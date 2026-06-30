using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class DarkmoonPrizeCatalog
    {
        private readonly Dictionary<string, DarkmoonPrizeDefinition> byCardId;

        public DarkmoonPrizeCatalog(IEnumerable<DarkmoonPrizeDefinition> definitions)
        {
            All = definitions == null ? new List<DarkmoonPrizeDefinition>() : definitions.ToList();
            byCardId = All
                .Where(definition => !string.IsNullOrEmpty(definition.CardId))
                .GroupBy(definition => definition.CardId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        public List<DarkmoonPrizeDefinition> All { get; private set; }

        public List<DarkmoonPrizeDefinition> GetByTier(int tier)
        {
            return All.Where(definition => definition.Tier == tier).ToList();
        }

        public DarkmoonPrizeDefinition GetByCardId(string cardId)
        {
            if (!byCardId.TryGetValue(cardId, out var definition))
            {
                throw new InvalidOperationException("Darkmoon Prize card id does not exist: " + cardId);
            }

            return definition;
        }

        public bool TryGetByCardId(string cardId, out DarkmoonPrizeDefinition definition)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                definition = null;
                return false;
            }

            return byCardId.TryGetValue(cardId, out definition);
        }
    }
}
