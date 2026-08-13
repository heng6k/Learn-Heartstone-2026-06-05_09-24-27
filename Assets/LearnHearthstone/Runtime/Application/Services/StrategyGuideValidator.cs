using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Services
{
    public static class StrategyGuideValidator
    {
        private const int StandardMaxTavernTier = 6;
        private static readonly HashSet<string> GrowthKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            StrategyGuideGrowthKeys.BeastLobsterGrowth,
            StrategyGuideGrowthKeys.TavernSpellsCastThisGame,
            StrategyGuideGrowthKeys.DemonTavernBonusAttack,
            StrategyGuideGrowthKeys.DemonTavernBonusHealth
        };

        private static readonly HashSet<string> ProvenanceValues = new HashSet<string>(StringComparer.Ordinal)
        {
            StrategyGuideProvenance.NormalPool,
            StrategyGuideProvenance.Generated,
            StrategyGuideProvenance.GuideTutorial
        };

        private static readonly HashSet<string> ActionKinds = new HashSet<string>(StringComparer.Ordinal)
        {
            StrategyGuideActionKinds.Buy,
            StrategyGuideActionKinds.Play,
            StrategyGuideActionKinds.Sell,
            StrategyGuideActionKinds.Cast,
            StrategyGuideActionKinds.Activate,
            StrategyGuideActionKinds.ChooseTrinket,
            StrategyGuideActionKinds.PlayFinalCards,
            StrategyGuideActionKinds.BoardOrder
        };

        private static readonly HashSet<string> Difficulties = new HashSet<string>(StringComparer.Ordinal)
        {
            StrategyGuideDifficulties.Showcase,
            StrategyGuideDifficulties.GuidedDiscover,
            StrategyGuideDifficulties.OpenBuild
        };

        private static readonly HashSet<string> OfferSources = new HashSet<string>(StringComparer.Ordinal)
        {
            StrategyGuideOfferSources.TripleRewardDiscover,
            StrategyGuideOfferSources.ShopRefresh,
            StrategyGuideOfferSources.TavernSpellDiscover,
            StrategyGuideOfferSources.GreaterTrinketChoice
        };

        private static readonly HashSet<string> OfferPolicies = new HashSet<string>(StringComparer.Ordinal)
        {
            StrategyGuideOfferPolicies.NaturalSeeded,
            StrategyGuideOfferPolicies.MustInclude,
            StrategyGuideOfferPolicies.MustIncludeAny,
            StrategyGuideOfferPolicies.Pinned
        };

        public static StrategyGuideValidationResult Validate(
            StrategyGuideCatalog source,
            StrategyGuideDefinition guide,
            ResolvedGameVersion version)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (guide == null)
            {
                throw new ArgumentNullException(nameof(guide));
            }
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            var result = new StrategyGuideValidationResult();
            var catalogs = version.Snapshot.Chinese;
            Require(result, !string.IsNullOrWhiteSpace(guide.GuideId), "guide.id.required");
            Require(result, !string.IsNullOrWhiteSpace(guide.RevisionId), "guide.revision.required");
            Require(
                result,
                string.Equals(guide.GameVersionId, version.GameVersion.Id, StringComparison.Ordinal),
                "guide.version.mismatch");

            var activeTribes = ParseTribes(result, guide.ActiveTribes, "guide.active-tribe.invalid");
            var requiredTribes = ParseTribes(result, guide.RequiredTribes, "guide.required-tribe.invalid");
            Require(result, activeTribes.Count == 5, "guide.active-tribe.count");
            Require(result, activeTribes.Distinct().Count() == activeTribes.Count, "guide.active-tribe.duplicate");
            Require(result, requiredTribes.All(activeTribes.Contains), "guide.required-tribe.missing");

            ValidateHeroAndMechanics(result, guide, catalogs);
            var rawProfiles = guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>();
            var profiles = rawProfiles
                .Where(item => item != null)
                .ToList();
            Require(result, profiles.Count == rawProfiles.Count, "guide.profile.null");
            EnsureUnique(result, profiles.Select(item => item.ProfileId), "guide.profile");
            Require(result, profiles.Count > 0, "guide.profile.empty");
            Require(
                result,
                profiles.Count(item => string.Equals(item.Difficulty, StrategyGuideDifficulties.Showcase, StringComparison.Ordinal)) == 1,
                "guide.profile.default-showcase");
            foreach (var profile in profiles)
            {
                Require(result, Difficulties.Contains(profile.Difficulty ?? string.Empty), "guide.profile.difficulty:" + profile.ProfileId);
                Require(result, !string.IsNullOrWhiteSpace(profile.Title), "guide.profile.title:" + profile.ProfileId);
                ValidateProfile(result, guide, profile, catalogs, source.Opponents);
            }
            return result;
        }

        private static void ValidateHeroAndMechanics(
            StrategyGuideValidationResult result,
            StrategyGuideDefinition guide,
            GameCatalogSet catalogs)
        {
            var hero = catalogs.Heroes.AllHeroes.FirstOrDefault(item =>
                string.Equals(item.HeroCardId, guide.HeroCardId, StringComparison.OrdinalIgnoreCase));
            Require(result, hero != null && hero.InPool, "guide.hero.unavailable");
            Require(
                result,
                hero != null && HeroEffectImplementationRegistry.FindByHeroCardId(hero.HeroCardId).Status == HeroEffectImplementationStatus.Implemented,
                "guide.hero.not-implemented");

            ValidateTrinket(result, catalogs.Trinkets, guide.LesserTrinketCardId, TrinketSlotKind.Lesser, "lesser");
            ValidateTrinket(result, catalogs.Trinkets, guide.GreaterTrinketCardId, TrinketSlotKind.Greater, "greater");
            ValidateRecommendedTrinkets(
                result,
                catalogs.Trinkets,
                guide.RecommendedLesserTrinketCardIds,
                guide.LesserTrinketCardId,
                TrinketSlotKind.Lesser,
                "lesser");
            ValidateRecommendedTrinkets(
                result,
                catalogs.Trinkets,
                guide.RecommendedGreaterTrinketCardIds,
                guide.GreaterTrinketCardId,
                TrinketSlotKind.Greater,
                "greater");
        }

        private static void ValidateRecommendedTrinkets(
            StrategyGuideValidationResult result,
            TrinketCatalog catalog,
            IEnumerable<string> recommendations,
            string defaultCardId,
            TrinketSlotKind slot,
            string label)
        {
            var effective = EffectiveRecommendations(recommendations, defaultCardId);
            EnsureUnique(result, effective, "guide.trinket.recommended." + label);
            Require(
                result,
                effective.Contains(defaultCardId, StringComparer.OrdinalIgnoreCase),
                "guide.trinket.recommended.default:" + label);
            foreach (var cardId in effective)
            {
                ValidateTrinket(result, catalog, cardId, slot, "recommended-" + label + ":" + cardId);
            }
        }

        private static void ValidateTrinket(
            StrategyGuideValidationResult result,
            TrinketCatalog catalog,
            string cardId,
            TrinketSlotKind slot,
            string label)
        {
            if (!catalog.TryGetByCardId(cardId, out var definition))
            {
                result.Errors.Add("guide.trinket." + label + ".missing");
                return;
            }

            Require(result, definition.SlotKind == slot, "guide.trinket." + label + ".slot");
            Require(
                result,
                definition.ImplementationStatus == TrinketImplementationStatus.Implemented,
                "guide.trinket." + label + ".not-implemented");
            Require(
                result,
                definition.OfferPoolStatus == TrinketOfferPoolStatus.Offerable,
                "guide.trinket." + label + ".not-offerable");
        }

        private static void ValidateProfile(
            StrategyGuideValidationResult result,
            StrategyGuideDefinition guide,
            StrategyGuideEntryProfileDefinition mode,
            GameCatalogSet catalogs,
            IReadOnlyList<StrategyGuideOpponentDefinition> opponents)
        {
            Require(result, mode.StartRound >= 1, "guide.start-round.invalid");
            Require(result, mode.TavernTier >= 1 && mode.TavernTier <= 6, "guide.tavern-tier.invalid");
            Require(result, mode.Gold >= 0 && mode.MaxGold >= mode.Gold, "guide.gold.invalid");
            var allowedCommands = mode.AllowedCommands ?? new List<string>();
            Require(result, allowedCommands.Count > 0, "guide.allowed-command.empty");
            EnsureUnique(result, allowedCommands, "guide.allowed-command");
            foreach (var command in allowedCommands)
            {
                Require(result, Enum.TryParse(command, false, out GameCommandType _), "guide.allowed-command.unknown:" + command);
            }
            var expectedUndoUses = string.Equals(mode.Difficulty, StrategyGuideDifficulties.Showcase, StringComparison.Ordinal)
                ? 1
                : 0;
            Require(result, mode.Undo != null && mode.Undo.UsesPerRun == expectedUndoUses, "guide.undo.uses");
            Require(
                result,
                mode.Undo != null && mode.Undo.RestoreRng && mode.Undo.LockAfterTurnEnd && mode.Undo.LockAfterCombat && mode.Undo.LockDuringFreeExplore,
                "guide.undo.boundary");
            Require(
                result,
                mode.Victory != null && mode.Victory.RequireFinalComposition && mode.Victory.RequireCombatWin,
                "guide.victory.contract");
            Require(
                result,
                mode.Victory != null && new HashSet<string>(mode.Victory.PostWinChoices ?? new List<string>(), StringComparer.Ordinal)
                    .SetEquals(new[] { "FreeExplore", "Restart", "Return" }),
                "guide.victory.post-win-choices");

            var placements = mode.Placements ?? new List<StrategyGuideCardDefinition>();
            EnsureUnique(result, placements.Select(item => item?.PlacementId), "guide.placement");
            foreach (var placement in placements)
            {
                ValidateCard(result, catalogs, placement, true);
                if (StrategyGuideShapingSpells.Contains(placement?.CardId))
                {
                    result.Errors.Add("guide.card.shaping-slot-only:" + placement.CardId);
                }
            }
            Require(result, placements.Count(item => item != null && item.Zone == StrategyGuideZones.Board) <= 7, "guide.board.capacity");
            Require(result, mode.InitialTripleRewardCount >= 0 && mode.InitialTripleRewardCount <= 2, "guide.triple-reward.count");
            Require(
                result,
                mode.InitialTripleRewardCount == 0 ||
                string.Equals(mode.Difficulty, StrategyGuideDifficulties.OpenBuild, StringComparison.Ordinal),
                "guide.triple-reward.difficulty");
            Require(
                result,
                placements.Count(item => item != null && item.Zone == StrategyGuideZones.Hand) + mode.InitialTripleRewardCount <= 10,
                "guide.hand.capacity");
            Require(result, placements.Count(item => item != null && item.Zone == StrategyGuideZones.Shop) <= 7, "guide.shop.capacity");
            var isOpenBuild = string.Equals(
                mode.Difficulty,
                StrategyGuideDifficulties.OpenBuild,
                StringComparison.Ordinal);
            if (isOpenBuild)
            {
                var shopMinions = placements
                    .Where(item => item != null &&
                        item.Zone == StrategyGuideZones.Shop &&
                        item.CardKind == StrategyGuideCardKinds.Minion)
                    .ToList();
                Require(result, mode.StartRound == 8, "guide.open-build.start-round");
                Require(result, mode.TavernTier == 4, "guide.open-build.tavern-tier");
                Require(result, mode.InitialTripleRewardCount == 2, "guide.open-build.triple-rewards");
                Require(
                    result,
                    placements.Count(item => item != null && item.Zone == StrategyGuideZones.Board) == 7,
                    "guide.open-build.board-count");
                Require(result, shopMinions.Count == 5, "guide.open-build.shop-minion-count");
                Require(
                    result,
                    shopMinions.All(item =>
                        catalogs.Minions.TryGetByCardId(item.CardId, out var minion) &&
                        minion.TavernTier <= mode.TavernTier),
                    "guide.open-build.shop-minion-tier");
                Require(
                    result,
                    (mode.AllowedCommands ?? new List<string>()).Contains(GameCommandType.RerollShop.ToString()) &&
                    (mode.AllowedCommands ?? new List<string>()).Contains(GameCommandType.UpgradeTavern.ToString()),
                    "guide.open-build.tavern-controls");
            }

            var unequippedTrinketSlots = mode.UnequippedTrinketSlots ?? new List<string>();
            EnsureUnique(result, unequippedTrinketSlots, "guide.unequipped-trinket-slot");
            foreach (var slot in unequippedTrinketSlots)
            {
                Require(
                    result,
                    Enum.TryParse(slot, false, out TrinketSlotKind _),
                    "guide.unequipped-trinket-slot.invalid:" + slot);
            }

            var finalComposition = guide.FinalComposition ?? new List<StrategyGuideCardDefinition>();
            Require(result, finalComposition.Count == 7, "guide.final-composition.count");
            foreach (var card in finalComposition)
            {
                ValidateCard(result, catalogs, card, false);
            }

            foreach (var core in guide.CoreMinionCardIds ?? new List<string>())
            {
                Require(result, catalogs.Minions.TryGetByCardId(core, out _), "guide.core-minion.missing:" + core);
            }
            foreach (var core in guide.CoreSpellCardNumbers ?? new List<string>())
            {
                Require(result, catalogs.Spells.All.Any(item => string.Equals(item.CardNumber, core, StringComparison.OrdinalIgnoreCase)), "guide.core-spell.missing:" + core);
            }

            var shapingSpellCardIds = mode.ShapingSpellCardIds ?? new List<string>();
            EnsureUnique(result, shapingSpellCardIds, "guide.shaping-spell");
            Require(
                result,
                shapingSpellCardIds.Count == 1,
                "guide.shaping-spell.single-category");
            foreach (var cardId in shapingSpellCardIds)
            {
                if (!StrategyGuideShapingSpells.Contains(cardId))
                {
                    result.Errors.Add("guide.shaping-spell.unknown:" + cardId);
                    continue;
                }

                Require(
                    result,
                    catalogs.Spells.All.Any(item => string.Equals(item.CardNumber, cardId, StringComparison.Ordinal)),
                    "guide.shaping-spell.missing:" + cardId);
            }

            ValidateDarkGifts(result, catalogs, mode, placements);
            ValidateGrowth(result, mode.GrowthQuality);
            ValidateActions(result, mode.RequiredActions, placements);
            ValidateAcquisitionPlan(result, guide, mode, catalogs);
            ValidateOpponent(result, guide.GameVersionId, mode.Opponent, opponents, catalogs);
        }

        private static void ValidateAcquisitionPlan(
            StrategyGuideValidationResult result,
            StrategyGuideDefinition guide,
            StrategyGuideEntryProfileDefinition profile,
            GameCatalogSet catalogs)
        {
            var plan = profile.AcquisitionPlan;
            var schedules = (plan?.OfferSchedules ?? new List<StrategyGuideOfferScheduleDefinition>())
                .Where(item => item != null)
                .ToList();
            if (string.Equals(profile.Difficulty, StrategyGuideDifficulties.GuidedDiscover, StringComparison.Ordinal) ||
                string.Equals(profile.Difficulty, StrategyGuideDifficulties.OpenBuild, StringComparison.Ordinal))
            {
                Require(result, plan != null && schedules.Count > 0, "guide.acquisition.required");
            }
            if (plan == null)
            {
                return;
            }

            if (string.Equals(profile.Difficulty, StrategyGuideDifficulties.OpenBuild, StringComparison.Ordinal))
            {
                var shopRefreshSchedules = schedules
                    .Where(item => string.Equals(
                        item.Source,
                        StrategyGuideOfferSources.ShopRefresh,
                        StringComparison.Ordinal))
                    .ToList();
                var shopRefreshSchedule = shopRefreshSchedules.FirstOrDefault();
                var shopRefreshTargets = shopRefreshSchedule?.TargetCardIds ?? new List<string>();
                Require(
                    result,
                    shopRefreshSchedules.Count == 1 &&
                    shopRefreshSchedule.TriggerTavernTier == 4 &&
                    shopRefreshSchedule.TriggerOccurrence == 2 &&
                    shopRefreshSchedule.OptionCount == TavernRules.GetShopSize(4) &&
                    string.Equals(shopRefreshSchedule.Policy, StrategyGuideOfferPolicies.MustInclude, StringComparison.Ordinal) &&
                    shopRefreshTargets.Count == 1 &&
                    (guide.CoreMinionCardIds ?? new List<string>()).Contains(shopRefreshTargets[0], StringComparer.OrdinalIgnoreCase) &&
                    catalogs.Minions.TryGetByCardId(shopRefreshTargets[0], out var shopRefreshCore) &&
                    shopRefreshCore.TavernTier <= 4,
                    "guide.open-build.tier-four-refresh-core");
                Require(
                    result,
                    schedules.Any(item =>
                        string.Equals(item.Source, StrategyGuideOfferSources.TripleRewardDiscover, StringComparison.Ordinal) &&
                        item.TriggerTavernTier == 4 &&
                        item.TavernTier == 5 &&
                        string.Equals(item.Policy, StrategyGuideOfferPolicies.MustInclude, StringComparison.Ordinal)),
                    "guide.open-build.tier-five-discover");
                Require(
                    result,
                    schedules.Any(item =>
                        string.Equals(item.Source, StrategyGuideOfferSources.TripleRewardDiscover, StringComparison.Ordinal) &&
                        item.TriggerTavernTier == 5 &&
                        item.TavernTier == 6 &&
                        string.Equals(item.Policy, StrategyGuideOfferPolicies.MustInclude, StringComparison.Ordinal)),
                    "guide.open-build.tier-six-discover");
                Require(
                    result,
                    schedules.Count(item => string.Equals(
                        item.Source,
                        StrategyGuideOfferSources.GreaterTrinketChoice,
                        StringComparison.Ordinal)) == 1,
                    "guide.open-build.greater-trinket-plan");
                var recommendedGreater = EffectiveRecommendations(
                    guide.RecommendedGreaterTrinketCardIds,
                    guide.GreaterTrinketCardId);
                var greaterSchedule = schedules.FirstOrDefault(item => string.Equals(
                    item.Source,
                    StrategyGuideOfferSources.GreaterTrinketChoice,
                    StringComparison.Ordinal));
                Require(
                    result,
                    greaterSchedule != null &&
                    (greaterSchedule.TargetCardIds ?? new List<string>()).Any(cardId =>
                        recommendedGreater.Contains(cardId, StringComparer.OrdinalIgnoreCase)),
                    "guide.open-build.greater-trinket-recommendation");
            }

            Require(result, schedules.Count == (plan.OfferSchedules?.Count ?? 0), "guide.acquisition.schedule.null");
            EnsureUnique(result, schedules.Select(item => item.ScheduleId), "guide.acquisition.schedule");
            EnsureUnique(
                result,
                schedules.Select(item =>
                    (item.Source ?? string.Empty) + "|" +
                    (item.TriggerCardId ?? string.Empty) + "|" +
                    item.TriggerTavernTier + "|" +
                    item.TriggerOccurrence),
                "guide.acquisition.route");

            var hasControlled = false;
            foreach (var schedule in schedules)
            {
                var controlled = string.Equals(schedule.Policy, StrategyGuideOfferPolicies.MustInclude, StringComparison.Ordinal) ||
                    string.Equals(schedule.Policy, StrategyGuideOfferPolicies.MustIncludeAny, StringComparison.Ordinal) ||
                    string.Equals(schedule.Policy, StrategyGuideOfferPolicies.Pinned, StringComparison.Ordinal);
                hasControlled |= controlled;
                Require(result, IsStableId(schedule.ScheduleId), "guide.acquisition.schedule.id:" + schedule.ScheduleId);
                Require(result, OfferSources.Contains(schedule.Source ?? string.Empty), "guide.acquisition.source:" + schedule.ScheduleId);
                Require(result, OfferPolicies.Contains(schedule.Policy ?? string.Empty), "guide.acquisition.policy:" + schedule.ScheduleId);
                Require(
                    result,
                    string.Equals(schedule.CardKind, StrategyGuideCardKinds.Minion, StringComparison.Ordinal) ||
                    string.Equals(schedule.CardKind, StrategyGuideCardKinds.TavernSpell, StringComparison.Ordinal) ||
                    string.Equals(schedule.CardKind, StrategyGuideCardKinds.Trinket, StringComparison.Ordinal),
                    "guide.acquisition.card-kind:" + schedule.ScheduleId);
                Require(result, schedule.TriggerOccurrence >= 1 && schedule.TriggerOccurrence <= 99, "guide.acquisition.occurrence:" + schedule.ScheduleId);
                Require(result, schedule.TriggerTavernTier >= 0 && schedule.TriggerTavernTier <= 7, "guide.acquisition.trigger-tier:" + schedule.ScheduleId);
                Require(result, schedule.TavernTier >= 0 && schedule.TavernTier <= 7, "guide.acquisition.tier:" + schedule.ScheduleId);
                Require(result, schedule.OptionCount >= 1 && schedule.OptionCount <= 7, "guide.acquisition.option-count:" + schedule.ScheduleId);
                Require(
                    result,
                    !string.Equals(schedule.Source, StrategyGuideOfferSources.TavernSpellDiscover, StringComparison.Ordinal) ||
                    !string.IsNullOrWhiteSpace(schedule.TriggerCardId),
                    "guide.acquisition.trigger-card:" + schedule.ScheduleId);
                Require(result, !controlled || !string.IsNullOrWhiteSpace(schedule.Label), "guide.acquisition.label:" + schedule.ScheduleId);
                var effectiveTargetTier = schedule.TavernTier;
                if (string.Equals(schedule.Source, StrategyGuideOfferSources.TripleRewardDiscover, StringComparison.Ordinal))
                {
                    var triggerTavernTier = schedule.TriggerTavernTier > 0
                        ? schedule.TriggerTavernTier
                        : profile.TavernTier;
                    var tripleRewardTier = Math.Min(StandardMaxTavernTier, triggerTavernTier + 1);
                    Require(
                        result,
                        schedule.TavernTier <= 0 || schedule.TavernTier == tripleRewardTier,
                        "guide.acquisition.triple-tier:" + schedule.ScheduleId);
                    effectiveTargetTier = tripleRewardTier;
                }

                var rawTargets = schedule.TargetCardIds ?? new List<string>();
                var targets = rawTargets
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToList();
                EnsureUnique(result, rawTargets, "guide.acquisition.target:" + schedule.ScheduleId);
                Require(result, rawTargets.Count == targets.Count, "guide.acquisition.target.empty:" + schedule.ScheduleId);
                Require(
                    result,
                    controlled ? targets.Count > 0 && targets.Count <= schedule.OptionCount : targets.Count == 0,
                    "guide.acquisition.target-count:" + schedule.ScheduleId);
                Require(
                    result,
                    !string.Equals(schedule.Policy, StrategyGuideOfferPolicies.Pinned, StringComparison.Ordinal) ||
                    targets.Count == schedule.OptionCount,
                    "guide.acquisition.pinned-count:" + schedule.ScheduleId);
                Require(
                    result,
                    !string.Equals(profile.Difficulty, StrategyGuideDifficulties.OpenBuild, StringComparison.Ordinal) ||
                    !string.Equals(schedule.Policy, StrategyGuideOfferPolicies.Pinned, StringComparison.Ordinal),
                    "guide.acquisition.open-build-pinned:" + schedule.ScheduleId);

                var isGreaterTrinketChoice = string.Equals(
                    schedule.Source,
                    StrategyGuideOfferSources.GreaterTrinketChoice,
                    StringComparison.Ordinal);
                var parsedRequiredTribe = Tribe.None;
                var hasValidRequiredTribe = !string.IsNullOrWhiteSpace(schedule.RequiredTribe) &&
                    Enum.TryParse(schedule.RequiredTribe, true, out parsedRequiredTribe) &&
                    parsedRequiredTribe != Tribe.None &&
                    (guide.ActiveTribes ?? new List<string>()).Any(value =>
                        Enum.TryParse(value, true, out Tribe activeTribe) && activeTribe == parsedRequiredTribe);
                Require(
                    result,
                    !isGreaterTrinketChoice || string.Equals(schedule.CardKind, StrategyGuideCardKinds.Trinket, StringComparison.Ordinal),
                    "guide.acquisition.trinket-card-kind:" + schedule.ScheduleId);
                Require(
                    result,
                    !isGreaterTrinketChoice || (profile.UnequippedTrinketSlots ?? new List<string>())
                        .Contains(TrinketSlotKind.Greater.ToString(), StringComparer.Ordinal),
                    "guide.acquisition.trinket-slot:" + schedule.ScheduleId);
                Require(
                    result,
                    !isGreaterTrinketChoice || hasValidRequiredTribe,
                    "guide.acquisition.required-tribe:" + schedule.ScheduleId);
                Require(
                    result,
                    !isGreaterTrinketChoice ||
                    (schedule.MinimumRequiredTribeMinions >= 1 && schedule.MinimumRequiredTribeMinions <= 7),
                    "guide.acquisition.required-tribe-count:" + schedule.ScheduleId);
                Require(
                    result,
                    isGreaterTrinketChoice ||
                    (string.IsNullOrWhiteSpace(schedule.RequiredTribe) && schedule.MinimumRequiredTribeMinions == 0),
                    "guide.acquisition.unexpected-tribe-gate:" + schedule.ScheduleId);

                foreach (var target in targets)
                {
                    ValidateAcquisitionTarget(result, catalogs, schedule, target, effectiveTargetTier);
                    if (string.Equals(schedule.Source, StrategyGuideOfferSources.ShopRefresh, StringComparison.Ordinal) &&
                        catalogs.Minions.TryGetByCardId(target, out var shopTarget))
                    {
                        var availableTier = schedule.TriggerTavernTier > 0
                            ? schedule.TriggerTavernTier
                            : profile.TavernTier;
                        Require(
                            result,
                            shopTarget.TavernTier <= availableTier,
                            "guide.acquisition.target.above-tavern:" + schedule.ScheduleId + ":" + target);
                    }
                }
            }

            foreach (var route in schedules.GroupBy(
                         item => (item.Source ?? string.Empty) + "|" + (item.TriggerCardId ?? string.Empty),
                         StringComparer.OrdinalIgnoreCase))
            {
                var hasAnyTier = route.Any(item => item.TriggerTavernTier <= 0);
                var hasExactTier = route.Any(item => item.TriggerTavernTier > 0);
                Require(result, !hasAnyTier || !hasExactTier, "guide.acquisition.trigger-tier.mixed:" + route.Key);
            }

            Require(result, !hasControlled || plan.DiscloseControlledOffers, "guide.acquisition.disclosure");
        }

        private static void ValidateAcquisitionTarget(
            StrategyGuideValidationResult result,
            GameCatalogSet catalogs,
            StrategyGuideOfferScheduleDefinition schedule,
            string cardId,
            int effectiveTargetTier)
        {
            if (string.Equals(schedule.CardKind, StrategyGuideCardKinds.Minion, StringComparison.Ordinal))
            {
                if (!catalogs.Minions.TryGetByCardId(cardId, out var minion))
                {
                    result.Errors.Add("guide.acquisition.target.missing:" + schedule.ScheduleId + ":" + cardId);
                    return;
                }

                Require(result, minion.InPool, "guide.acquisition.target.not-in-pool:" + schedule.ScheduleId + ":" + cardId);
                Require(
                    result,
                    effectiveTargetTier <= 0 || minion.TavernTier == effectiveTargetTier,
                    "guide.acquisition.target.tier:" + schedule.ScheduleId + ":" + cardId);
                return;
            }

            if (string.Equals(schedule.CardKind, StrategyGuideCardKinds.TavernSpell, StringComparison.Ordinal))
            {
                var spell = catalogs.Spells.All.FirstOrDefault(item =>
                    string.Equals(item.CardNumber, cardId, StringComparison.OrdinalIgnoreCase));
                if (spell == null)
                {
                    result.Errors.Add("guide.acquisition.target.missing:" + schedule.ScheduleId + ":" + cardId);
                    return;
                }

                Require(result, spell.InPool, "guide.acquisition.target.not-in-pool:" + schedule.ScheduleId + ":" + cardId);
                Require(
                    result,
                    effectiveTargetTier <= 0 || spell.TavernTier == effectiveTargetTier,
                    "guide.acquisition.target.tier:" + schedule.ScheduleId + ":" + cardId);
                return;
            }

            if (string.Equals(schedule.CardKind, StrategyGuideCardKinds.Trinket, StringComparison.Ordinal))
            {
                if (!catalogs.Trinkets.TryGetByCardId(cardId, out var trinket))
                {
                    result.Errors.Add("guide.acquisition.target.missing:" + schedule.ScheduleId + ":" + cardId);
                    return;
                }

                Require(
                    result,
                    trinket.SlotKind == TrinketSlotKind.Greater,
                    "guide.acquisition.target.trinket-slot:" + schedule.ScheduleId + ":" + cardId);
                Require(
                    result,
                    trinket.ImplementationStatus == TrinketImplementationStatus.Implemented &&
                    trinket.OfferPoolStatus == TrinketOfferPoolStatus.Offerable,
                    "guide.acquisition.target.not-in-pool:" + schedule.ScheduleId + ":" + cardId);
                if (Enum.TryParse(schedule.RequiredTribe, true, out Tribe requiredTribe))
                {
                    Require(
                        result,
                        TribeAvailabilityRules.TrinketTribes(trinket).Contains(requiredTribe),
                        "guide.acquisition.target.trinket-tribe:" + schedule.ScheduleId + ":" + cardId);
                }
                return;
            }

            result.Errors.Add("guide.acquisition.card-kind:" + schedule.ScheduleId);
        }

        private static bool IsStableId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Length <= 64 && value.All(character =>
                char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.');
        }

        private static void ValidateCard(
            StrategyGuideValidationResult result,
            GameCatalogSet catalogs,
            StrategyGuideCardDefinition card,
            bool requireZone)
        {
            if (card == null)
            {
                result.Errors.Add("guide.card.null");
                return;
            }
            if (requireZone && card.Zone != StrategyGuideZones.Board && card.Zone != StrategyGuideZones.Hand && card.Zone != StrategyGuideZones.Shop)
            {
                result.Errors.Add("guide.card.zone:" + card.PlacementId);
            }
            if (!ProvenanceValues.Contains(card.Provenance ?? string.Empty))
            {
                result.Errors.Add("guide.card.provenance:" + card.PlacementId);
            }

            var inPool = false;
            if (card.CardKind == StrategyGuideCardKinds.Minion)
            {
                if (!catalogs.Minions.TryGetByCardId(card.CardId, out var minion))
                {
                    result.Errors.Add("guide.card.minion-missing:" + card.CardId);
                    return;
                }
                inPool = minion.InPool;
                if (card.Golden && minion.Golden == null)
                {
                    result.Errors.Add("guide.card.golden-missing:" + card.CardId);
                }
            }
            else if (card.CardKind == StrategyGuideCardKinds.TavernSpell)
            {
                var spell = catalogs.Spells.All.FirstOrDefault(item =>
                    string.Equals(item.CardNumber, card.CardId, StringComparison.OrdinalIgnoreCase));
                if (spell == null)
                {
                    result.Errors.Add("guide.card.spell-missing:" + card.CardId);
                    return;
                }
                inPool = spell.InPool;
                if (card.Golden)
                {
                    result.Errors.Add("guide.card.spell-golden:" + card.CardId);
                }
            }
            else
            {
                result.Errors.Add("guide.card.kind:" + card.CardKind);
                return;
            }

            if (card.Provenance == StrategyGuideProvenance.NormalPool && !inPool)
            {
                result.Errors.Add("guide.card.not-in-pool:" + card.CardId);
            }
            if (requireZone &&
                card.Zone == StrategyGuideZones.Shop &&
                (card.Provenance == StrategyGuideProvenance.Generated ||
                 (!inPool && card.Provenance != StrategyGuideProvenance.GuideTutorial)))
            {
                result.Errors.Add("guide.card.shop-injection-unmarked:" + card.CardId);
            }
        }

        private static void ValidateDarkGifts(
            StrategyGuideValidationResult result,
            GameCatalogSet catalogs,
            StrategyGuideEntryProfileDefinition mode,
            IReadOnlyCollection<StrategyGuideCardDefinition> placements)
        {
            var attachments = mode.DarkGiftAttachments ?? new List<StrategyGuideDarkGiftAttachment>();
            EnsureUnique(result, attachments.Select(item => item?.AttachmentId), "guide.dark-gift-attachment");
            var byPlacement = placements
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.PlacementId))
                .GroupBy(item => item.PlacementId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            foreach (var attachment in attachments.Where(item => item != null))
            {
                if (!byPlacement.TryGetValue(attachment.TargetPlacementId ?? string.Empty, out var target) ||
                    target.CardKind != StrategyGuideCardKinds.Minion)
                {
                    result.Errors.Add("guide.dark-gift.target:" + attachment.AttachmentId);
                }

                DarkGiftDefinition gift = null;
                try
                {
                    gift = catalogs.DarkGifts.GetByResearchKey(attachment.GiftResearchKey);
                }
                catch (InvalidOperationException)
                {
                    result.Errors.Add("guide.dark-gift.missing:" + attachment.GiftResearchKey);
                }
                if (gift == null)
                {
                    continue;
                }
                Require(result, gift.ImplementationStatus == DarkGiftImplementationStatus.Implemented, "guide.dark-gift.not-implemented:" + attachment.GiftResearchKey);
                Require(result, attachment.AcquiredRound >= gift.EarliestOfferRound, "guide.dark-gift.too-early:" + attachment.AttachmentId);
                Require(result, gift.LatestOfferRound <= 0 || attachment.AcquiredRound <= gift.LatestOfferRound, "guide.dark-gift.too-late:" + attachment.AttachmentId);
                Require(result, attachment.AcquiredRound <= mode.StartRound, "guide.dark-gift.after-start:" + attachment.AttachmentId);
            }
        }

        private static void ValidateGrowth(StrategyGuideValidationResult result, IEnumerable<StrategyGuideGrowthValue> values)
        {
            var items = (values ?? Enumerable.Empty<StrategyGuideGrowthValue>()).Where(item => item != null).ToList();
            EnsureUnique(result, items.Select(item => item.Key), "guide.growth");
            foreach (var item in items)
            {
                Require(result, GrowthKeys.Contains(item.Key ?? string.Empty), "guide.growth.unknown:" + item.Key);
                Require(result, item.Value >= 0, "guide.growth.negative:" + item.Key);
            }
        }

        private static void ValidateActions(
            StrategyGuideValidationResult result,
            IEnumerable<StrategyGuideRequiredAction> actions,
            IReadOnlyCollection<StrategyGuideCardDefinition> placements)
        {
            var items = (actions ?? Enumerable.Empty<StrategyGuideRequiredAction>()).Where(item => item != null).ToList();
            EnsureUnique(result, items.Select(item => item.ActionId), "guide.action");
            var placementIds = new HashSet<string>(placements.Where(item => item != null).Select(item => item.PlacementId), StringComparer.OrdinalIgnoreCase);
            foreach (var action in items)
            {
                Require(result, ActionKinds.Contains(action.Kind ?? string.Empty), "guide.action.kind:" + action.ActionId);
                Require(result, action.Count > 0, "guide.action.count:" + action.ActionId);
                Require(result, !string.IsNullOrWhiteSpace(action.Instruction), "guide.action.instruction:" + action.ActionId);
                if (!string.IsNullOrWhiteSpace(action.SourcePlacementId))
                {
                    Require(result, placementIds.Contains(action.SourcePlacementId), "guide.action.source:" + action.ActionId);
                }
                foreach (var source in action.SourcePlacementIds ?? new List<string>())
                {
                    Require(result, placementIds.Contains(source), "guide.action.source:" + action.ActionId);
                }
                if (!string.IsNullOrWhiteSpace(action.TargetPlacementId))
                {
                    Require(result, placementIds.Contains(action.TargetPlacementId), "guide.action.target:" + action.ActionId);
                }
            }
        }

        private static void ValidateOpponent(
            StrategyGuideValidationResult result,
            string versionId,
            StrategyGuideOpponentSelector selector,
            IReadOnlyList<StrategyGuideOpponentDefinition> opponents,
            GameCatalogSet catalogs)
        {
            if (selector == null || selector.StrengthRound < 1 || string.IsNullOrWhiteSpace(selector.RequiredTag))
            {
                result.Errors.Add("guide.opponent.selector");
                return;
            }

            var eligible = opponents.Where(item =>
                    item != null &&
                    string.Equals(item.GameVersionId, versionId, StringComparison.Ordinal) &&
                    item.StrengthRound == selector.StrengthRound &&
                    (item.Tags ?? new List<string>()).Contains(selector.RequiredTag))
                .ToList();
            Require(result, eligible.Count > 0, "guide.opponent.none");
            foreach (var opponent in eligible)
            {
                Require(result, (opponent.Board ?? new List<StrategyGuideCardDefinition>()).Count == 7, "guide.opponent.board-count:" + opponent.OpponentId);
                foreach (var card in opponent.Board ?? new List<StrategyGuideCardDefinition>())
                {
                    ValidateCard(result, catalogs, card, false);
                }
                ValidateGrowth(result, opponent.GrowthQuality);
            }
        }

        private static List<string> EffectiveRecommendations(
            IEnumerable<string> recommendations,
            string fallbackCardId)
        {
            var values = (recommendations ?? Enumerable.Empty<string>())
                .Where(cardId => !string.IsNullOrWhiteSpace(cardId))
                .ToList();
            if (values.Count == 0 && !string.IsNullOrWhiteSpace(fallbackCardId))
            {
                values.Add(fallbackCardId);
            }
            return values;
        }

        private static List<Tribe> ParseTribes(StrategyGuideValidationResult result, IEnumerable<string> values, string error)
        {
            var tribes = new List<Tribe>();
            foreach (var value in values ?? Enumerable.Empty<string>())
            {
                if (!Enum.TryParse(value, true, out Tribe tribe) || tribe == Tribe.None || tribe == Tribe.All)
                {
                    result.Errors.Add(error + ":" + value);
                    continue;
                }
                tribes.Add(tribe);
            }
            return tribes;
        }

        private static void EnsureUnique(StrategyGuideValidationResult result, IEnumerable<string> values, string code)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
                {
                    result.Errors.Add(code + ".duplicate-or-empty:" + value);
                }
            }
        }

        private static void Require(StrategyGuideValidationResult result, bool condition, string error)
        {
            if (!condition)
            {
                result.Errors.Add(error);
            }
        }
    }
}
