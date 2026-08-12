using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public sealed class LockboxMechanicRequest
    {
        public string InstanceId;
        public string DefinitionRevisionId;
        public string OpenResolverId;
        public string Source;
        public string RequestId;
        public string EventType;
        public int AccelerationTurns = 1;
    }

    public sealed class LockboxMechanicResult
    {
        public bool Succeeded;
        public string Code;
        public string Message;
        public string InstanceId;
        public int RemainingTurns;
        public int AccelerationCount;
        public bool Opened;
    }

    public static class LockboxMechanicService
    {
        private const int InitialTurns = 5;

        public static LockboxMechanicResult CreateOrAccelerate(
            MatchState state,
            LockboxMechanicRequest request,
            DelayedObjectResolverRegistry registry)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (request == null ||
                string.IsNullOrWhiteSpace(request.InstanceId) ||
                string.IsNullOrWhiteSpace(request.DefinitionRevisionId) ||
                string.IsNullOrWhiteSpace(request.OpenResolverId))
            {
                return Failure("lockbox.request.invalid", "Lockbox request is invalid.");
            }

            var priorEvent = FindRequestEvent(state, request.RequestId);
            if (priorEvent != null)
            {
                var prior = Find(state, priorEvent.Targets?.FirstOrDefault());
                return FromState(state, prior, "lockbox.already-applied", true);
            }

            var active = (state.DelayedObjectStates ?? new List<DelayedObjectState>())
                .FirstOrDefault(item =>
                    item != null &&
                    !item.Opened &&
                    string.Equals(item.DefinitionRevisionId, request.DefinitionRevisionId, StringComparison.Ordinal));
            if (active == null)
            {
                var added = DelayedObjectService.Add(state, new DelayedObjectState
                {
                    InstanceId = request.InstanceId,
                    DefinitionRevisionId = request.DefinitionRevisionId,
                    CreatedRound = Math.Max(1, state.Round),
                    RemainingTurns = InitialTurns,
                    OpenResolverId = request.OpenResolverId,
                    Source = request.Source,
                    Opened = false
                }, request.RequestId);
                return added
                    ? FromState(state, Find(state, request.InstanceId), "lockbox.created", true)
                    : Failure("lockbox.create.failed", "Lockbox could not be created.");
            }

            DelayedObjectResolver resolver = null;
            registry?.TryGet(active.OpenResolverId, out resolver);
            var advanced = DelayedObjectService.Advance(
                state,
                active.InstanceId,
                Math.Max(1, request.AccelerationTurns),
                resolver,
                request.RequestId,
                request.Source,
                string.IsNullOrWhiteSpace(request.EventType)
                    ? "delayed-object.accelerated"
                    : request.EventType);
            return FromDelayedResult(state, active, advanced);
        }

        public static List<LockboxMechanicResult> AdvanceTurnEnded(
            MatchState state,
            DelayedObjectResolverRegistry registry,
            int endingRound,
            string transitionId,
            int occurrenceCount)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (string.IsNullOrWhiteSpace(transitionId))
            {
                throw new ArgumentException("Turn-end transition ID is required.", nameof(transitionId));
            }

            var results = new List<LockboxMechanicResult>();
            var lockboxes = (state.DelayedObjectStates ?? new List<DelayedObjectState>())
                .Where(item => item != null && !item.Opened)
                .ToList();
            for (var occurrence = 0; occurrence < Math.Max(1, occurrenceCount); occurrence += 1)
            {
                foreach (var delayedObject in lockboxes)
                {
                    if (delayedObject.Opened)
                    {
                        continue;
                    }

                    var requestId =
                        "turn-end:" + Math.Max(1, endingRound) +
                        ":" + transitionId +
                        ":lockbox:" + delayedObject.InstanceId +
                        ":occurrence:" + occurrence;
                    DelayedObjectResolver resolver = null;
                    registry?.TryGet(delayedObject.OpenResolverId, out resolver);
                    var advanced = DelayedObjectService.Advance(
                        state,
                        delayedObject.InstanceId,
                        1,
                        resolver,
                        requestId,
                        "turn-end:" + Math.Max(1, endingRound) + ":" + transitionId,
                        "delayed-object.turn-ended");
                    results.Add(FromDelayedResult(state, delayedObject, advanced));
                }
            }

            return results;
        }

        public static int GetAccelerationCount(MatchState state, string instanceId)
        {
            return (state?.MechanicEvents ?? new List<MechanicEventRecord>()).Count(item =>
                item != null &&
                (string.Equals(item.Type, "delayed-object.accelerated", StringComparison.Ordinal) ||
                 (item.Type?.EndsWith("-accelerated", StringComparison.Ordinal) ?? false)) &&
                (item.Targets ?? new List<string>()).Contains(instanceId));
        }

        private static LockboxMechanicResult FromDelayedResult(
            MatchState state,
            DelayedObjectState delayedObject,
            DelayedObjectResult result)
        {
            if (result == null || !result.Succeeded)
            {
                return Failure(result?.Code ?? "lockbox.advance.failed", result?.Message ?? "Lockbox advance failed.");
            }

            return FromState(
                state,
                delayedObject,
                result.Opened ? "lockbox.opened" : "lockbox.accelerated",
                true);
        }

        private static LockboxMechanicResult FromState(
            MatchState state,
            DelayedObjectState delayedObject,
            string code,
            bool succeeded)
        {
            return new LockboxMechanicResult
            {
                Succeeded = succeeded,
                Code = code,
                Message = string.Empty,
                InstanceId = delayedObject?.InstanceId,
                RemainingTurns = delayedObject?.RemainingTurns ?? 0,
                AccelerationCount = GetAccelerationCount(state, delayedObject?.InstanceId),
                Opened = delayedObject?.Opened ?? false
            };
        }

        private static LockboxMechanicResult Failure(string code, string message)
        {
            return new LockboxMechanicResult
            {
                Succeeded = false,
                Code = code,
                Message = message
            };
        }

        private static DelayedObjectState Find(MatchState state, string instanceId)
        {
            return state?.DelayedObjectStates?.FirstOrDefault(item =>
                item != null && string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal));
        }

        private static MechanicEventRecord FindRequestEvent(MatchState state, string requestId)
        {
            return string.IsNullOrWhiteSpace(requestId)
                ? null
                : state?.MechanicEvents?.FirstOrDefault(item =>
                    item != null && string.Equals(item.RequestId, requestId, StringComparison.Ordinal));
        }
    }

    public delegate MinionInstance FishbaitRefreshResolver(MatchState state);

    public static class FishbaitRecruitAttackService
    {
        public const string FishbaitCardId = "BG36_205";
        public const string GoldenFishbaitCardId = "BG36_205_G";

        public static RecruitPhaseAttackResult ReplaceAndAttack(
            MatchState state,
            string tavernTargetInstanceId,
            MinionInstance fishbait,
            int seed)
        {
            if (!TryGetAttackInputs(state, out _, out var shop, out var failure))
            {
                return failure;
            }
            var targetIndex = shop.FindIndex(item =>
                item != null && string.Equals(item.InstanceId, tavernTargetInstanceId, StringComparison.Ordinal));
            if (targetIndex < 0 || !IsFishbait(fishbait))
            {
                return Failure("fishbait.replace.invalid", "Fishbait replacement target or card is invalid.");
            }

            var replacement = fishbait.Clone();
            replacement.Owner = BoardSide.Player;
            shop[targetIndex] = replacement;
            MechanicEventLog.Append(
                state,
                "fishbait.replaced",
                "fishbait:replace",
                new[] { tavernTargetInstanceId, replacement.InstanceId },
                "shop-index=" + targetIndex);
            return Attack(state, replacement, seed, "fishbait:replace");
        }

        public static RecruitPhaseAttackResult RefreshAndAttack(
            MatchState state,
            FishbaitRefreshResolver refresh,
            int seed,
            string source)
        {
            if (!TryGetAttackInputs(state, out _, out _, out var failure))
            {
                return failure;
            }
            if (refresh == null)
            {
                return Failure("fishbait.refresh.missing", "Fishbait refresh resolver is required.");
            }

            var fishbait = refresh(state);
            var liveFishbait = state.Player?.Tavern?.Shop?.FirstOrDefault(item =>
                item != null &&
                fishbait != null &&
                string.Equals(item.InstanceId, fishbait.InstanceId, StringComparison.Ordinal));
            if (!IsFishbait(liveFishbait))
            {
                return Failure("fishbait.refresh.invalid", "Refresh did not create a Fishbait in the Tavern.");
            }

            MechanicEventLog.Append(
                state,
                "fishbait.refreshed",
                source,
                new[] { liveFishbait.InstanceId },
                "fishbait-ready");
            return Attack(state, liveFishbait, seed, source);
        }

        private static RecruitPhaseAttackResult Attack(
            MatchState state,
            MinionInstance fishbait,
            int seed,
            string source)
        {
            var attacker = state.Player.Board.First(item =>
                item != null &&
                item.Health > 0 &&
                (item.Tribes ?? new List<Tribe>()).Contains(Tribe.Beast));
            var result = CombatEngine.ResolveRecruitPhaseAttack(
                state,
                new RecruitPhaseAttackContext
                {
                    AttackerInstanceId = attacker.InstanceId,
                    TavernTargetInstanceId = fishbait.InstanceId,
                    DamageContext = "fishbait-damage",
                    DeathContext = "fishbait-deathrattle",
                    RewardSource = source,
                    Sequence = state.MechanicEvents?.Count ?? 0
                },
                seed,
                venomousEffectRevision: VenomousEffectRevisions.PerCombat);
            if (result.Succeeded && result.Rewards.Any(item =>
                    item != null &&
                    item.Type == CombatRewardType.BuffOriginalFriendlyMinion &&
                    IsFishbaitCardId(item.SourceCardId)))
            {
                MechanicEventLog.Append(
                    state,
                    "fishbait.reward.resolved",
                    fishbait.InstanceId,
                    new[] { result.AttackerInstanceId },
                    "killer-buffed");
            }
            return result;
        }

        private static bool TryGetAttackInputs(
            MatchState state,
            out MinionInstance attacker,
            out List<MinionInstance> shop,
            out RecruitPhaseAttackResult failure)
        {
            attacker = state?.Player?.Board?.FirstOrDefault(item =>
                item != null &&
                item.Health > 0 &&
                (item.Tribes ?? new List<Tribe>()).Contains(Tribe.Beast));
            shop = state?.Player?.Tavern?.Shop;
            failure = null;
            if (state == null || state.Phase != MatchPhase.Tavern)
            {
                failure = Failure("fishbait.phase.invalid", "Fishbait attack is only allowed during the Tavern phase.");
                return false;
            }
            if (attacker == null)
            {
                failure = Failure("fishbait.attacker.missing", "No friendly Beast is available to attack.");
                return false;
            }
            if (shop == null)
            {
                failure = Failure("fishbait.shop.missing", "Tavern shop is unavailable.");
                return false;
            }
            return true;
        }

        private static RecruitPhaseAttackResult Failure(string code, string message)
        {
            return new RecruitPhaseAttackResult
            {
                Succeeded = false,
                Code = code,
                Message = message,
                Rewards = new List<CombatReward>()
            };
        }

        internal static bool IsFishbait(MinionInstance minion)
        {
            return minion != null && IsFishbaitCardId(minion.CardId);
        }

        internal static bool IsFishbaitCardId(string cardId)
        {
            return string.Equals(cardId, FishbaitCardId, StringComparison.Ordinal) ||
                   string.Equals(cardId, GoldenFishbaitCardId, StringComparison.Ordinal);
        }
    }

    public static class Season14DarkGiftSourceService
    {
        public const string XaviusSourceId = "hero-power:feel-devastation";
        public const string TrastathSourceId = "hero-power:void-power";
        public const string WaxLanceSourceId = "trinket:wax-lance";
        public const string OminousStoneSourceId = "trinket:ominous-stone";
        public const string NormalGiftPoolId = "dark-gift:normal";
        public const string TrastathGiftPoolId = "dark-gift:trastath-exclusive-21";
        public const string GiftPoolMetadataKey = "dark-gift.gift-pool";
        public const string UnlockRoundMetadataKey = "dark-gift.unlock-round";
        public const string GoldCostMetadataKey = "gold-cost";
        public const string NormalEntrySourceId = "normal-button";
        public const string NormalUsesTotalCounter = "dark-gift.normal.uses-total";
        public const string NormalLastUsedRoundCounter = "dark-gift.normal.last-used-round";
        public const string NormalUsesThisRoundCounter = "dark-gift.normal.uses-this-round";
        public const string WaxLanceEffectId = "season14_wax_lance";
        private const string LegacyWaxLanceEffectId = "wax_lance";

        private const string XaviusPowerName = "Feel Devastation";
        private const string TrastathPowerName = "Void Power";
        private const string WaxLanceName = "Wax Lance";
        private const string RngCursorCounter = "dark-gift.rng-cursor";
        private const string GiftPoolTagPrefix = "gift-pool:";
        private const string NormalPoolTag = "normal-pool";
        private const string TrastathPoolTag = "tras-pool";

        public static ChoiceQueueItem ScheduleNormalEntry(
            MatchState state,
            DarkGiftProfile profile,
            MinionCatalog minions,
            IEnumerable<DarkGiftDefinition> gifts,
            IEnumerable<Tribe> activeTribes,
            CardPoolAvailability minionAvailability = null)
        {
            var queued = Schedule(
                state,
                profile,
                minions,
                gifts,
                activeTribes,
                DarkGiftOfferSourceKind.NormalButton,
                NormalEntrySourceId,
                NormalGiftPoolId,
                requestedTier: 0,
                ignoreNormalRoundRestrictions: false,
                unlockRound: 0,
                minionAvailability: minionAvailability);
            if (queued != null && !(queued.ResolutionMetadata ?? new List<ChoiceResolutionMetadataEntry>()).Any(item =>
                    string.Equals(item?.Key, GoldCostMetadataKey, StringComparison.Ordinal)))
            {
                queued.ResolutionMetadata.Add(new ChoiceResolutionMetadataEntry
                {
                    Key = GoldCostMetadataKey,
                    Value = Math.Max(0, profile?.GoldCost ?? 0).ToString()
                });
            }

            return queued;
        }

        public static ChoiceQueueItem ScheduleOpeningHeroPower(
            MatchState state,
            string heroPowerName,
            DarkGiftProfile profile,
            MinionCatalog minions,
            IEnumerable<DarkGiftDefinition> gifts,
            IEnumerable<Tribe> activeTribes,
            CardPoolAvailability minionAvailability = null)
        {
            return string.Equals(heroPowerName, TrastathPowerName, StringComparison.Ordinal)
                ? Schedule(
                    state,
                    profile,
                    minions,
                    gifts,
                    activeTribes,
                    DarkGiftOfferSourceKind.HeroPower,
                    TrastathSourceId,
                    TrastathGiftPoolId,
                    requestedTier: 5,
                    ignoreNormalRoundRestrictions: true,
                    unlockRound: 7,
                    minionAvailability: minionAvailability)
                : null;
        }

        public static ChoiceQueueItem ScheduleTurnStartHeroPower(
            MatchState state,
            string heroPowerName,
            DarkGiftProfile profile,
            MinionCatalog minions,
            IEnumerable<DarkGiftDefinition> gifts,
            IEnumerable<Tribe> activeTribes,
            CardPoolAvailability minionAvailability = null)
        {
            return string.Equals(heroPowerName, XaviusPowerName, StringComparison.Ordinal) &&
                   state != null &&
                   state.Round > 0 &&
                   state.Round % 4 == 0
                ? Schedule(
                    state,
                    profile,
                    minions,
                    gifts,
                    activeTribes,
                    DarkGiftOfferSourceKind.HeroPower,
                    XaviusSourceId,
                    NormalGiftPoolId,
                    requestedTier: 0,
                    ignoreNormalRoundRestrictions: false,
                    unlockRound: 0,
                    minionAvailability: minionAvailability)
                : null;
        }

        public static ChoiceQueueItem ScheduleWaxLance(
            MatchState state,
            TrinketDefinition trinket,
            DarkGiftProfile profile,
            MinionCatalog minions,
            IEnumerable<DarkGiftDefinition> gifts,
            IEnumerable<Tribe> activeTribes,
            CardPoolAvailability minionAvailability = null)
        {
            if (trinket == null ||
                (!string.Equals(trinket.Name, WaxLanceName, StringComparison.Ordinal) &&
                 !(trinket.EffectIds ?? new List<string>()).Any(effectId =>
                     string.Equals(effectId, WaxLanceEffectId, StringComparison.Ordinal) ||
                     string.Equals(effectId, LegacyWaxLanceEffectId, StringComparison.Ordinal))))
            {
                return null;
            }

            return Schedule(
                state,
                profile,
                minions,
                gifts,
                activeTribes,
                DarkGiftOfferSourceKind.Trinket,
                WaxLanceSourceId,
                NormalGiftPoolId,
                requestedTier: 7,
                ignoreNormalRoundRestrictions: false,
                unlockRound: 0,
                minionAvailability: minionAvailability);
        }

        public static ChoiceQueueItem ScheduleOminousStone(
            MatchState state,
            DarkGiftProfile profile,
            MinionCatalog minions,
            IEnumerable<DarkGiftDefinition> gifts,
            IEnumerable<Tribe> activeTribes,
            CardPoolAvailability minionAvailability = null)
        {
            return Schedule(
                state,
                profile,
                minions,
                gifts,
                activeTribes,
                DarkGiftOfferSourceKind.Trinket,
                OminousStoneSourceId,
                NormalGiftPoolId,
                requestedTier: 4,
                ignoreNormalRoundRestrictions: true,
                unlockRound: 0,
                minionAvailability: minionAvailability);
        }

        private static ChoiceQueueItem Schedule(
            MatchState state,
            DarkGiftProfile profile,
            MinionCatalog minions,
            IEnumerable<DarkGiftDefinition> gifts,
            IEnumerable<Tribe> activeTribes,
            DarkGiftOfferSourceKind sourceKind,
            string sourceId,
            string giftPoolId,
            int requestedTier,
            bool ignoreNormalRoundRestrictions,
            int unlockRound,
            CardPoolAvailability minionAvailability)
        {
            if (state == null || state.ChoiceQueue == null || profile?.Enabled != true || minions == null)
            {
                return null;
            }

            var requestId = "dark-gift:" + sourceId + ":" + state.Round;
            if ((state.ChoiceQueue.CompletedRequestIds ?? new List<string>()).Contains(requestId))
            {
                return null;
            }
            if (string.Equals(state.ChoiceQueue.ActiveChoice?.RequestId, requestId, StringComparison.Ordinal))
            {
                return state.ChoiceQueue.ActiveChoice;
            }

            var pending = (state.ChoiceQueue.PendingChoices ?? new List<ChoiceQueueItem>()).FirstOrDefault(item =>
                string.Equals(item?.RequestId, requestId, StringComparison.Ordinal));
            if (pending != null)
            {
                return pending;
            }

            state.PlayerDarkGifts = state.PlayerDarkGifts ?? new PlayerDarkGiftState();
            state.PlayerDarkGifts.Counters = state.PlayerDarkGifts.Counters ?? new Dictionary<string, int>();
            state.PlayerDarkGifts.Counters.TryGetValue(RngCursorCounter, out var cursor);
            var definitions = SelectGiftPool(gifts, giftPoolId);
            var offer = DarkGiftOfferService.CreateOffer(
                new DarkGiftOfferRequest
                {
                    SourceKind = sourceKind,
                    SourceId = sourceId,
                    Round = state.Round,
                    RequestedTier = requestedTier,
                    OfferCount = profile.OfferCount,
                    PickCount = profile.PickCount,
                    PlayerTavernTier = state.Player?.Tavern?.Tier ?? 0,
                    BattlecriesTriggeredThisGame = state.Player?.Tavern?.BattlecriesTriggeredThisGame ?? 0,
                    DeathrattlesTriggeredThisGame = state.Player?.Tavern?.DeathrattlesTriggeredThisGame ?? 0,
                    TavernSpellsCastThisGame = state.Player?.Tavern?.TavernSpellsCastThisGame ?? 0,
                    ActiveTribes = new List<Tribe>(activeTribes ?? Enumerable.Empty<Tribe>()),
                    CurrentBoardTribeCounts = BoardTribeCounts(state),
                    GiftPoolProfileId = giftPoolId,
                    IgnoreNormalRoundRestrictions = ignoreNormalRoundRestrictions,
                    Seed = state.Seed,
                    RngCursor = cursor
                },
                profile,
                minions,
                definitions,
                minionAvailability);
            if (!offer.Succeeded)
            {
                return null;
            }

            var metadata = new List<ChoiceResolutionMetadataEntry>
            {
                new ChoiceResolutionMetadataEntry { Key = GiftPoolMetadataKey, Value = giftPoolId }
            };
            if (unlockRound > 0)
            {
                metadata.Add(new ChoiceResolutionMetadataEntry
                {
                    Key = UnlockRoundMetadataKey,
                    Value = unlockRound.ToString()
                });
            }

            var queued = ChoiceQueueService.Enqueue(state.ChoiceQueue, new ChoiceQueueItem
            {
                RequestId = requestId,
                Kind = ChoiceRequestKind.DarkGift,
                Source = sourceId,
                CreatedRound = state.Round,
                Priority = profile.ChoiceQueuePriority,
                Blocking = true,
                RemainingPicks = offer.PickCount,
                ResolutionMetadata = metadata,
                Options = offer.Options.ConvertAll(option => new MechanicChoiceOption
                {
                    OptionId = option.OptionId,
                    Kind = AdvancedMechanicKind.DarkGift,
                    SourceId = option.MinionCardId,
                    DisplayName = option.MinionName,
                    Text = option.MinionText,
                    ImagePath = option.MinionImagePath,
                    RewardId = option.GiftRevisionId,
                    RewardName = option.GiftName,
                    RewardText = option.GiftText,
                    RewardImagePath = option.GiftImagePath,
                    DifficultyTier = option.MinionTier,
                    Attack = option.MinionAttack,
                    Health = option.MinionHealth,
                    Tribes = new List<Tribe>(option.MinionTribes ?? new List<Tribe>()),
                    Slot = "DarkGift"
                })
            });
            if (queued != null)
            {
                state.PlayerDarkGifts.Counters[RngCursorCounter] = offer.NextRngCursor;
            }
            return queued;
        }

        private static List<DarkGiftDefinition> SelectGiftPool(
            IEnumerable<DarkGiftDefinition> gifts,
            string giftPoolId)
        {
            var all = (gifts ?? Enumerable.Empty<DarkGiftDefinition>())
                .Where(item => item != null)
                .ToList();
            var anyTaggedPool = all.Any(item => (item.AvailabilityTags ?? new List<string>())
                .Any(tag => tag != null &&
                            (tag.StartsWith(GiftPoolTagPrefix, StringComparison.Ordinal) ||
                             string.Equals(tag, NormalPoolTag, StringComparison.Ordinal) ||
                             string.Equals(tag, TrastathPoolTag, StringComparison.Ordinal))));
            var compactPoolTag = string.Equals(giftPoolId, TrastathGiftPoolId, StringComparison.Ordinal)
                ? TrastathPoolTag
                : NormalPoolTag;
            return anyTaggedPool
                ? all.Where(item => (item.AvailabilityTags ?? new List<string>())
                    .Any(tag => string.Equals(tag, GiftPoolTagPrefix + giftPoolId, StringComparison.Ordinal) ||
                                string.Equals(tag, compactPoolTag, StringComparison.Ordinal)))
                    .ToList()
                : all;
        }

        private static List<DarkGiftTribeCount> BoardTribeCounts(MatchState state)
        {
            return (state.Player?.Board ?? new List<MinionInstance>())
                .Where(item => item != null)
                .SelectMany(item => item.Tribes ?? new List<Tribe>())
                .Where(tribe => tribe != Tribe.None && tribe != Tribe.All)
                .GroupBy(tribe => tribe)
                .Select(group => new DarkGiftTribeCount { Tribe = group.Key, Count = group.Count() })
                .OrderBy(item => item.Tribe)
                .ToList();
        }
    }
}
