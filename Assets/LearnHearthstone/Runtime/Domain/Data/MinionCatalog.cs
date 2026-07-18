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
            if (!TryGetById(id, out var definition))
            {
                throw new InvalidOperationException("Minion definition does not exist: " + id);
            }

            return definition;
        }

        public MinionDefinition GetByCardId(string cardId)
        {
            if (!TryGetByCardId(cardId, out var definition))
            {
                throw new InvalidOperationException("Minion card id does not exist: " + cardId);
            }

            return definition;
        }

        public bool TryGetById(string id, out MinionDefinition definition)
        {
            definition = null;
            return !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out definition);
        }

        public bool TryGetByCardId(string cardId, out MinionDefinition definition)
        {
            definition = null;
            return !string.IsNullOrEmpty(cardId) && byCardId.TryGetValue(cardId, out definition);
        }

        public bool TrySyncGoldenText(MinionInstance target)
        {
            if (target == null ||
                (!TryGetById(target.DefinitionId, out var definition) &&
                 !TryGetByCardId(target.CardId, out definition)))
            {
                return false;
            }

            var text = target.Golden && !string.IsNullOrWhiteSpace(definition.Golden?.Text)
                ? definition.Golden.Text
                : definition.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            target.Text = text;
            return true;
        }

        public List<MinionDefinition> GetMinionsForTier(int tier)
        {
            return All.Where(minion => minion.InPool && minion.TavernTier <= tier).ToList();
        }
    }
}
