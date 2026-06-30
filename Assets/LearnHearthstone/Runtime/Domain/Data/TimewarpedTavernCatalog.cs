using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class TimewarpedTavernCatalog
    {
        private readonly Dictionary<string, TimewarpedTavernCardDefinition> byCardId;

        public TimewarpedTavernCatalog(IEnumerable<TimewarpedTavernCardDefinition> definitions)
        {
            All = definitions?.ToList() ?? new List<TimewarpedTavernCardDefinition>();
            byCardId = All
                .Where(card => !string.IsNullOrEmpty(card.CardId))
                .GroupBy(card => card.CardId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        public List<TimewarpedTavernCardDefinition> All { get; }

        public List<TimewarpedTavernCardDefinition> Current =>
            All.Where(card => string.Equals(card.PoolStatus, "current", StringComparison.OrdinalIgnoreCase)).ToList();

        public List<TimewarpedTavernCardDefinition> Minor =>
            Current.Where(card => card.TimewarpKind == TimewarpKind.Minor).ToList();

        public List<TimewarpedTavernCardDefinition> Major =>
            Current.Where(card => card.TimewarpKind == TimewarpKind.Major).ToList();

        public List<TimewarpedTavernCardDefinition> HistoricalExtra =>
            All.Where(card => string.Equals(card.PoolStatus, "historical_extra", StringComparison.OrdinalIgnoreCase)).ToList();

        public List<TimewarpedTavernCardDefinition> NonMinions =>
            All.Where(card => card.CardKind != CardKind.Minion).ToList();

        public List<TimewarpedTavernCardDefinition> ImplementedNonMinions =>
            NonMinions.Where(card => string.Equals(card.PoolStatus, "implemented_non_minion", StringComparison.OrdinalIgnoreCase)).ToList();

        public List<TimewarpedTavernCardDefinition> BlockedNonMinions =>
            NonMinions.Where(TimewarpedCardBehavior.IsBlockedNonMinionSupport).ToList();

        public List<TimewarpedTavernCardDefinition> OfferableCurrentNonMinionsForKind(TimewarpKind kind)
        {
            return ImplementedNonMinions
                .Where(card =>
                    card.TimewarpKind == kind &&
                    TimewarpedCardBehavior.IsOfferableNonExit(card))
                .ToList();
        }

        public TimewarpedTavernCardDefinition GetByCardId(string cardId)
        {
            if (!byCardId.TryGetValue(cardId, out var definition))
            {
                throw new InvalidOperationException("Timewarped Tavern card does not exist: " + cardId);
            }

            return definition;
        }

        public bool TryGetByCardId(string cardId, out TimewarpedTavernCardDefinition definition)
        {
            return byCardId.TryGetValue(cardId, out definition);
        }
    }
}
