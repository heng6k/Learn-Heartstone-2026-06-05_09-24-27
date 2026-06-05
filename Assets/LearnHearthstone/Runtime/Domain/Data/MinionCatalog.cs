using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class MinionCatalog
    {
        private readonly Dictionary<string, MinionDefinition> byId;
        private readonly Dictionary<string, MinionDefinition> byCardId;

        public MinionCatalog(IEnumerable<MinionDefinition> definitions)
        {
            All = definitions.ToList();
            byId = All.ToDictionary(minion => minion.Id, minion => minion);
            byCardId = All.ToDictionary(minion => minion.CardId, minion => minion);
        }

        public List<MinionDefinition> All { get; }

        public MinionDefinition GetById(string id)
        {
            if (!byId.TryGetValue(id, out var definition))
            {
                throw new InvalidOperationException("Minion definition does not exist: " + id);
            }

            return definition;
        }

        public MinionDefinition GetByCardId(string cardId)
        {
            if (!byCardId.TryGetValue(cardId, out var definition))
            {
                throw new InvalidOperationException("Minion card id does not exist: " + cardId);
            }

            return definition;
        }

        public List<MinionDefinition> GetMinionsForTier(int tier)
        {
            return All.Where(minion => minion.InPool && minion.TavernTier <= tier).ToList();
        }
    }
}
