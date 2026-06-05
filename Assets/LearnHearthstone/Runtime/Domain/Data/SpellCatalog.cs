using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class SpellCatalog
    {
        private readonly Dictionary<string, TavernSpellDefinition> byId;
        private readonly Dictionary<int, TavernSpellDefinition> bySourceId;
        private readonly Dictionary<string, TavernSpellDefinition> byCardNumber;

        public SpellCatalog(IEnumerable<TavernSpellDefinition> definitions)
        {
            All = definitions.ToList();
            byId = All.ToDictionary(spell => spell.Id, spell => spell);
            bySourceId = All.ToDictionary(spell => spell.SourceId, spell => spell);
            byCardNumber = All
                .Where(spell => !string.IsNullOrEmpty(spell.CardNumber))
                .ToDictionary(spell => spell.CardNumber, spell => spell);
        }

        public List<TavernSpellDefinition> All { get; }

        public TavernSpellDefinition GetById(string id)
        {
            if (!byId.TryGetValue(id, out var definition))
            {
                throw new InvalidOperationException("Spell definition does not exist: " + id);
            }

            return definition;
        }

        public TavernSpellDefinition GetBySourceId(int sourceId)
        {
            if (!bySourceId.TryGetValue(sourceId, out var definition))
            {
                throw new InvalidOperationException("Spell source id does not exist: " + sourceId);
            }

            return definition;
        }

        public TavernSpellDefinition GetByCardNumber(string cardNumber)
        {
            if (!byCardNumber.TryGetValue(cardNumber, out var definition))
            {
                throw new InvalidOperationException("Spell card number does not exist: " + cardNumber);
            }

            return definition;
        }

        public List<TavernSpellDefinition> GetSpellsForTier(int tier)
        {
            return All.Where(spell => spell.TavernTier <= tier).ToList();
        }
    }
}
