using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Services
{
    public static class StrategyGuideOfferScheduleResolver
    {
        public static StrategyGuideOfferScheduleDefinition FindSchedule(
            StrategyGuideAcquisitionPlanDefinition plan,
            string source,
            string triggerCardId,
            int occurrence,
            int tavernTier = 0)
        {
            if (plan == null || occurrence <= 0 || string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            return (plan.OfferSchedules ?? new List<StrategyGuideOfferScheduleDefinition>())
                .Where(item => item != null)
                .FirstOrDefault(item =>
                    string.Equals(item.Source, source, StringComparison.Ordinal) &&
                    item.TriggerOccurrence == occurrence &&
                    (item.TriggerTavernTier <= 0 || item.TriggerTavernTier == tavernTier) &&
                    TriggerMatches(item.TriggerCardId, triggerCardId));
        }

        public static bool ApplyToShop(
            StrategyGuideOfferScheduleDefinition schedule,
            MatchService matchService)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }
            if (matchService == null)
            {
                throw new ArgumentNullException(nameof(matchService));
            }
            if (!string.Equals(schedule.CardKind, StrategyGuideCardKinds.Minion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Shop refresh schedules currently support minion targets only.");
            }
            if (string.Equals(schedule.Policy, StrategyGuideOfferPolicies.NaturalSeeded, StringComparison.Ordinal))
            {
                return true;
            }
            if (!string.Equals(schedule.Policy, StrategyGuideOfferPolicies.MustInclude, StringComparison.Ordinal) &&
                !string.Equals(schedule.Policy, StrategyGuideOfferPolicies.MustIncludeAny, StringComparison.Ordinal) &&
                !string.Equals(schedule.Policy, StrategyGuideOfferPolicies.Pinned, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unknown strategy guide offer policy: " + schedule.Policy + ".");
            }

            return matchService.TryEnsureShopMinionOffers(
                DistinctIds(schedule.TargetCardIds),
                "strategy-guide-" + schedule.ScheduleId);
        }

        public static List<string> ResolveCandidateIds(
            StrategyGuideOfferScheduleDefinition schedule,
            IEnumerable<string> naturalCandidateIds,
            IEnumerable<string> eligibleCandidateIds,
            int seed)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            var optionCount = Math.Max(1, schedule.OptionCount);
            var natural = DistinctIds(naturalCandidateIds);
            var eligible = new HashSet<string>(DistinctIds(eligibleCandidateIds), StringComparer.OrdinalIgnoreCase);
            if (string.Equals(schedule.Policy, StrategyGuideOfferPolicies.NaturalSeeded, StringComparison.Ordinal))
            {
                return natural.Take(optionCount).ToList();
            }

            var targets = DistinctIds(schedule.TargetCardIds);
            if (targets.Any(target => !eligible.Contains(target)))
            {
                throw new InvalidOperationException("A controlled target is not eligible for this offer.");
            }

            if (string.Equals(schedule.Policy, StrategyGuideOfferPolicies.Pinned, StringComparison.Ordinal))
            {
                return targets.Take(optionCount).ToList();
            }
            var mustIncludeAll = string.Equals(
                schedule.Policy,
                StrategyGuideOfferPolicies.MustInclude,
                StringComparison.Ordinal);
            var mustIncludeAny = string.Equals(
                schedule.Policy,
                StrategyGuideOfferPolicies.MustIncludeAny,
                StringComparison.Ordinal);
            if (!mustIncludeAll && !mustIncludeAny)
            {
                throw new InvalidOperationException("Unknown strategy guide offer policy: " + schedule.Policy + ".");
            }

            var rng = new SeededRng(seed);
            var result = mustIncludeAny
                ? new List<string> { targets[rng.NextInt(targets.Count)] }
                : new List<string>(targets);
            result.AddRange(natural.Where(item => !result.Contains(item, StringComparer.OrdinalIgnoreCase)));
            if (result.Count < optionCount)
            {
                result.AddRange(eligible
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .Where(item => !result.Contains(item, StringComparer.OrdinalIgnoreCase)));
            }

            result = result.Take(optionCount).ToList();
            Shuffle(result, rng);
            return result;
        }

        public static bool ApplyToActiveDiscover(
            StrategyGuideOfferScheduleDefinition schedule,
            MatchState state,
            GameCatalogSet catalogs,
            int seed)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }
            if (state?.Player?.Tavern?.Discover == null || catalogs == null)
            {
                return false;
            }
            if (!string.Equals(schedule.CardKind, StrategyGuideCardKinds.Minion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The current strategy guide runtime only supports minion discover schedules.");
            }

            var discover = state.Player.Tavern.Discover;
            var tier = schedule.TavernTier > 0 ? schedule.TavernTier : discover.RewardTier;
            var activeTribes = new HashSet<Tribe>(state.ActiveTribes ?? new List<Tribe>());
            var eligible = catalogs.Minions.All
                .Where(item => item.InPool &&
                    (tier <= 0 || item.TavernTier == tier) &&
                    IsEligibleTribe(item.Tribes, activeTribes))
                .GroupBy(item => item.CardId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(item => item.CardId, StringComparer.Ordinal)
                .ToList();
            var natural = (discover.Options ?? new List<MinionInstance>())
                .Where(item => item != null)
                .Select(item => item.CardId)
                .ToList();
            var resolvedIds = ResolveCandidateIds(
                schedule,
                natural,
                eligible.Select(item => item.CardId),
                seed);
            if (resolvedIds.Count == 0)
            {
                return false;
            }

            var naturalById = (discover.Options ?? new List<MinionInstance>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.CardId))
                .GroupBy(item => item.CardId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var eligibleById = eligible.ToDictionary(item => item.CardId, item => item, StringComparer.OrdinalIgnoreCase);
            var options = new List<MinionInstance>();
            for (var index = 0; index < resolvedIds.Count; index += 1)
            {
                var cardId = resolvedIds[index];
                if (naturalById.TryGetValue(cardId, out var existing))
                {
                    options.Add(existing);
                    continue;
                }
                if (!eligibleById.TryGetValue(cardId, out var definition))
                {
                    throw new InvalidOperationException("A resolved strategy guide candidate is not available: " + cardId + ".");
                }

                var option = MinionFactory.Create(
                    definition,
                    BoardSide.Player,
                    "strategy-guide-" + schedule.ScheduleId + "-" + index,
                    false,
                    PoolSource.Discover,
                    0);
                discover.ApplyOptionMetadata(option);
                options.Add(option);
            }

            discover.Options = options;
            var active = state.ChoiceQueue?.ActiveChoice;
            if (active?.Kind == ChoiceRequestKind.Discover && ReferenceEquals(active.Discover, discover))
            {
                ChoiceQueueService.UpdateDiscoverChoice(active, discover);
            }
            return true;
        }

        public static bool ApplyToActiveTrinketChoice(
            StrategyGuideOfferScheduleDefinition schedule,
            MatchService matchService,
            int seed)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }
            if (matchService == null)
            {
                throw new ArgumentNullException(nameof(matchService));
            }
            if (!string.Equals(schedule.CardKind, StrategyGuideCardKinds.Trinket, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Trinket choice schedules require Trinket candidates.");
            }

            var state = matchService.State;
            var active = state?.ChoiceQueue?.ActiveChoice;
            if (active?.Kind != ChoiceRequestKind.Trinket || active.Options == null || active.Options.Count == 0)
            {
                return false;
            }
            if (!Enum.TryParse(schedule.RequiredTribe, true, out Tribe requiredTribe) || requiredTribe == Tribe.None)
            {
                throw new InvalidOperationException("Strategy guide Trinket schedule has no valid required tribe.");
            }

            var matchingBoardMinions = (state.Player?.Board ?? new List<MinionInstance>())
                .Count(card => card != null &&
                    card.CardKind == CardKind.Minion &&
                    (card.Tribes?.Contains(requiredTribe) == true || card.Tribes?.Contains(Tribe.All) == true));
            if (matchingBoardMinions < Math.Max(1, schedule.MinimumRequiredTribeMinions))
            {
                throw new InvalidOperationException(
                    "Strategy guide Greater Trinket required tribe gate was not met: " +
                    requiredTribe + " " + matchingBoardMinions + "/" + Math.Max(1, schedule.MinimumRequiredTribeMinions) + ".");
            }

            var naturalIds = active.Options
                .Where(option => option != null)
                .Select(option => option.SourceId)
                .ToList();
            var eligibleIds = matchService.GetDebugSelectableTrinkets(TrinketSlotKind.Greater)
                .Select(definition => definition.CardId)
                .ToList();
            var eligibleIdSet = new HashSet<string>(eligibleIds, StringComparer.OrdinalIgnoreCase);
            if (naturalIds.Any(cardId => !eligibleIdSet.Contains(cardId)))
            {
                return false;
            }
            var resolvedIds = ResolveCandidateIds(schedule, naturalIds, eligibleIds, seed);
            return resolvedIds.Count > 0 && matchService.TryReplaceActiveTrinketChoiceOptions(resolvedIds);
        }

        private static bool TriggerMatches(string expected, string actual)
        {
            return string.IsNullOrWhiteSpace(expected) ||
                string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEligibleTribe(IEnumerable<Tribe> tribes, ISet<Tribe> activeTribes)
        {
            var values = (tribes ?? Enumerable.Empty<Tribe>()).ToList();
            return values.Count == 0 ||
                values.Contains(Tribe.None) ||
                values.Contains(Tribe.All) ||
                values.Any(activeTribes.Contains);
        }

        private static List<string> DistinctIds(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void Shuffle<T>(IList<T> values, SeededRng rng)
        {
            for (var index = values.Count - 1; index > 0; index -= 1)
            {
                var other = rng.NextInt(index + 1);
                if (other == index)
                {
                    continue;
                }

                var item = values[index];
                values[index] = values[other];
                values[other] = item;
            }
        }
    }
}
