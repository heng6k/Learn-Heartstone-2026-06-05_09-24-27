using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Services
{
    public static class StrategyGuideScenarioCompiler
    {
        private const string LobsterGrowthCounter = "season14_min_r30_lobster_growth";

        public static CompiledStrategyGuide Compile(
            StrategyGuideCatalog source,
            StrategyGuideDefinition guide,
            ResolvedGameVersion version,
            bool useEnglish = false,
            string profileId = null)
        {
            var validation = StrategyGuideValidator.Validate(source, guide, version);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException("Strategy guide validation failed: " + string.Join(" | ", validation.Errors));
            }

            var profile = ResolveProfile(guide, profileId);
            var resolverRegistry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(resolverRegistry, version.Snapshot.Chinese.Minions);
            var service = CreateRuntimeService(version, guide, profile, useEnglish, resolverRegistry);

            ConfigurePlayer(service, guide, profile, resolverRegistry);
            var opponent = SelectOpponent(source, guide, profile);
            ConfigureOpponent(service, opponent);
            var scenarioIdentity = guide.RevisionId + "#" + profile.ProfileId;
            var scenario = TestScenarioMapper.Capture(service.State, scenarioIdentity);
            scenario.Name = scenarioIdentity;
            return new CompiledStrategyGuide
            {
                Guide = guide,
                Profile = profile,
                Opponent = opponent,
                Scenario = scenario
            };
        }

        public static MatchService CreateRuntimeService(
            ResolvedGameVersion version,
            StrategyGuideDefinition guide,
            bool useEnglish = false,
            string profileId = null)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }
            if (guide == null)
            {
                throw new ArgumentNullException(nameof(guide));
            }

            var resolverRegistry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(resolverRegistry, version.Snapshot.Chinese.Minions);
            return CreateRuntimeService(version, guide, ResolveProfile(guide, profileId), useEnglish, resolverRegistry);
        }

        private static MatchService CreateRuntimeService(
            ResolvedGameVersion version,
            StrategyGuideDefinition guide,
            StrategyGuideEntryProfileDefinition profile,
            bool useEnglish,
            DarkGiftResolverRegistry resolverRegistry)
        {
            return MatchService.CreateWithResolvedVersion(
                version,
                profile.Seed,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    UseEnglish = useEnglish,
                    ActiveTribes = guide.ActiveTribes.Select(value => (Tribe)Enum.Parse(typeof(Tribe), value, true)).ToList(),
                    SelectedHeroCardId = guide.HeroCardId,
                    AdvancedMechanicMode = AdvancedMechanicMode.Trinkets,
                    EnableQuests = false,
                    EnableTrinkets = true,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                },
                darkGiftResolvers: resolverRegistry);
        }

        private static void ConfigurePlayer(
            MatchService service,
            StrategyGuideDefinition guide,
            StrategyGuideEntryProfileDefinition mode,
            DarkGiftResolverRegistry resolverRegistry)
        {
            var state = service.State;
            state.Phase = MatchPhase.Tavern;
            state.Round = mode.StartRound;
            state.PendingTurnStartRound = 0;
            state.PendingTurnResolvedCombat = false;
            state.Player.HeroId = guide.HeroCardId;
            var hero = service.Catalogs.Heroes.GetHeroByCardId(guide.HeroCardId);
            state.Player.Health = hero.Health;
            state.Player.MaxHealth = hero.Health;
            state.Player.Armor = hero.Armor;
            state.ActiveTribes = guide.ActiveTribes.Select(value => (Tribe)Enum.Parse(typeof(Tribe), value, true)).ToList();

            var tavern = state.Player.Tavern;
            tavern.Tier = mode.TavernTier;
            tavern.Gold = mode.Gold;
            tavern.MaxGold = mode.MaxGold;
            tavern.Shop.Clear();
            tavern.Hand.Clear();
            state.Player.Board.Clear();
            state.PlayerDarkGifts = new PlayerDarkGiftState();
            state.ChoiceQueue = new ChoiceQueueState();
            state.RecruitActionStates.Clear();
            state.DelayedObjectStates.Clear();
            state.MechanicEvents.Clear();
            tavern.RecruitLog.Clear();

            if (ShouldPreEquipTrinket(mode, TrinketSlotKind.Lesser))
            {
                EquipTrinket(state, service.Catalogs.Trinkets.GetByCardId(guide.LesserTrinketCardId), mode.StartRound);
            }
            if (ShouldPreEquipTrinket(mode, TrinketSlotKind.Greater))
            {
                EquipTrinket(state, service.Catalogs.Trinkets.GetByCardId(guide.GreaterTrinketCardId), mode.StartRound);
            }

            var byPlacement = new Dictionary<string, MinionInstance>(StringComparer.OrdinalIgnoreCase);
            foreach (var placement in mode.Placements)
            {
                var instance = CreateCard(service.Catalogs, placement, BoardSide.Player, placement.PlacementId);
                byPlacement.Add(placement.PlacementId, instance);
                AddToZone(state, placement.Zone, instance);
            }

            for (var index = 0; index < mode.InitialTripleRewardCount; index++)
            {
                tavern.Hand.Add(MatchService.CreateTripleRewardCard(
                    "strategy-guide-" + mode.ProfileId + "-" + index));
            }

            foreach (var attachment in mode.DarkGiftAttachments)
            {
                var gift = service.Catalogs.DarkGifts.GetByResearchKey(attachment.GiftResearchKey);
                var originalRound = state.Round;
                state.Round = attachment.AcquiredRound;
                var result = DarkGiftStateMachine.Acquire(
                    state,
                    byPlacement[attachment.TargetPlacementId],
                    gift,
                    string.IsNullOrWhiteSpace(attachment.Source) ? "strategy-guide" : attachment.Source,
                    attachment.AttachmentId,
                    resolverRegistry);
                state.Round = originalRound;
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException("Dark Gift attachment failed [" + attachment.AttachmentId + "]: " + result.Message);
                }
            }
        }

        private static StrategyGuideOpponentDefinition SelectOpponent(
            StrategyGuideCatalog source,
            StrategyGuideDefinition guide,
            StrategyGuideEntryProfileDefinition profile)
        {
            var selector = profile.Opponent;
            var eligible = source.Opponents
                .Where(item =>
                    string.Equals(item.GameVersionId, guide.GameVersionId, StringComparison.Ordinal) &&
                    item.StrengthRound == selector.StrengthRound &&
                    item.Tags.Contains(selector.RequiredTag))
                .OrderBy(item => item.OpponentId, StringComparer.Ordinal)
                .ToList();
            var index = (int)((uint)profile.Seed % (uint)eligible.Count);
            return eligible[index];
        }

        private static StrategyGuideEntryProfileDefinition ResolveProfile(
            StrategyGuideDefinition guide,
            string profileId)
        {
            var profiles = (guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>())
                .Where(item => item != null)
                .ToList();
            if (!string.IsNullOrWhiteSpace(profileId))
            {
                var selected = profiles.FirstOrDefault(item =>
                    string.Equals(item.ProfileId, profileId, StringComparison.OrdinalIgnoreCase));
                if (selected == null)
                {
                    throw new InvalidOperationException(
                        "Strategy guide entry profile does not exist: " + guide.GuideId + "/" + profileId + ".");
                }
                return selected;
            }

            var defaults = profiles
                .Where(item => string.Equals(item.Difficulty, StrategyGuideDifficulties.Showcase, StringComparison.Ordinal))
                .ToList();
            if (defaults.Count != 1)
            {
                throw new InvalidOperationException(
                    "Strategy guide must have exactly one default Showcase profile: " + guide.GuideId + ".");
            }
            return defaults[0];
        }

        private static void ConfigureOpponent(MatchService service, StrategyGuideOpponentDefinition opponent)
        {
            var state = service.State;
            state.Opponent.Name = opponent.OpponentId;
            state.Opponent.HeroId = opponent.HeroCardId;
            state.Opponent.TavernTier = opponent.TavernTier;
            state.Opponent.Editable = false;
            state.Opponent.Board.Clear();
            state.Opponent.Hand.Clear();
            state.Opponent.AdvancedMechanics = new AdvancedMechanicState();
            state.Opponent.CombatModifiers = new SideCombatModifierState();
            ApplyGrowth(state, opponent.GrowthQuality, true);
            foreach (var card in opponent.Board)
            {
                state.Opponent.Board.Add(CreateCard(service.Catalogs, card, BoardSide.Opponent, opponent.OpponentId + "-" + card.PlacementId));
            }
        }

        private static MinionInstance CreateCard(
            GameCatalogSet catalogs,
            StrategyGuideCardDefinition card,
            BoardSide owner,
            string suffix)
        {
            MinionInstance instance;
            if (card.CardKind == StrategyGuideCardKinds.Minion)
            {
                instance = MinionFactory.Create(
                    catalogs.Minions.GetByCardId(card.CardId),
                    owner,
                    suffix,
                    card.Golden,
                    PoolSource.Copy,
                    0);
            }
            else
            {
                instance = MinionFactory.Create(catalogs.Spells.GetByCardNumber(card.CardId), owner, suffix);
            }

            instance.InstanceId = owner.ToString().ToLowerInvariant() + "-guide-" + suffix;
            if (card.AttackOverride > 0 && instance.CardKind == CardKind.Minion)
            {
                instance.Attack = card.AttackOverride;
            }
            if (card.HealthOverride > 0 && instance.CardKind == CardKind.Minion)
            {
                instance.Health = card.HealthOverride;
                instance.MaxHealth = card.HealthOverride;
            }
            if (!instance.Tags.Contains("strategy-guide:" + card.Provenance))
            {
                instance.Tags.Add("strategy-guide:" + card.Provenance);
            }
            if (!instance.Tags.Contains("strategy-guide-placement:" + card.PlacementId))
            {
                instance.Tags.Add("strategy-guide-placement:" + card.PlacementId);
            }
            return instance;
        }

        private static void AddToZone(MatchState state, string zone, MinionInstance instance)
        {
            switch (zone)
            {
                case StrategyGuideZones.Board:
                    state.Player.Board.Add(instance);
                    break;
                case StrategyGuideZones.Hand:
                    state.Player.Tavern.Hand.Add(instance);
                    break;
                case StrategyGuideZones.Shop:
                    state.Player.Tavern.Shop.Add(instance);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported strategy guide zone: " + zone + ".");
            }
        }

        private static void EquipTrinket(MatchState state, TrinketDefinition definition, int round)
        {
            var advanced = state.Player.Tavern.AdvancedMechanics;
            var trinkets = advanced.Trinkets;
            if (definition.SlotKind == TrinketSlotKind.Lesser)
            {
                trinkets.LesserTrinketId = definition.CardId;
            }
            else
            {
                trinkets.GreaterTrinketId = definition.CardId;
            }
            trinkets.Equipped.Add(new EquippedTrinketState
            {
                TrinketId = definition.CardId,
                Name = definition.Name,
                SlotKind = definition.SlotKind,
                EquippedRound = round,
                CostPaid = 0,
                ImplementationStatus = definition.ImplementationStatus
            });
            advanced.Equipped.Add(new EquippedAdvancedMechanic
            {
                Kind = AdvancedMechanicKind.Trinket,
                SourceId = definition.CardId,
                DisplayName = definition.Name,
                Slot = definition.SlotKind.ToString(),
                EquippedRound = round,
                CostPaid = 0,
                ImplementationStatus = definition.ImplementationStatus.ToString()
            });
        }

        private static bool ShouldPreEquipTrinket(
            StrategyGuideEntryProfileDefinition profile,
            TrinketSlotKind slotKind)
        {
            return !(profile?.UnequippedTrinketSlots ?? new List<string>())
                .Contains(slotKind.ToString(), StringComparer.Ordinal);
        }

        private static void ApplyGrowth(MatchState state, IEnumerable<StrategyGuideGrowthValue> values, bool opponent)
        {
            foreach (var item in values ?? Enumerable.Empty<StrategyGuideGrowthValue>())
            {
                var value = Math.Max(0, item.Value);
                var modifiers = opponent ? state.Opponent.CombatModifiers : state.Player.CombatModifiers;
                var advanced = opponent ? state.Opponent.AdvancedMechanics : state.Player.Tavern.AdvancedMechanics;
                switch (item.Key)
                {
                    case StrategyGuideGrowthKeys.BeastLobsterGrowth:
                        advanced.Counters[LobsterGrowthCounter] = value;
                        break;
                    case StrategyGuideGrowthKeys.TavernSpellsCastThisGame:
                        modifiers.SpellsCastThisGame = value;
                        if (!opponent)
                        {
                            state.Player.Tavern.TavernSpellsCastThisGame = value;
                        }
                        break;
                    case StrategyGuideGrowthKeys.DemonTavernBonusAttack:
                        modifiers.TavernSpellBonusAttack = value;
                        if (!opponent)
                        {
                            state.Player.Tavern.TavernSpellBonusAttack = value;
                        }
                        break;
                    case StrategyGuideGrowthKeys.DemonTavernBonusHealth:
                        modifiers.TavernSpellBonusHealth = value;
                        if (!opponent)
                        {
                            state.Player.Tavern.TavernSpellBonusHealth = value;
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported strategy guide growth key: " + item.Key + ".");
                }
            }
        }
    }
}
