using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public sealed class ChooseOneCardPool
    {
        private ChooseOneCardPool(
            IReadOnlyList<MinionDefinition> minions,
            IReadOnlyList<TavernSpellDefinition> tavernSpells)
        {
            Minions = minions;
            TavernSpells = tavernSpells;
        }

        public IReadOnlyList<MinionDefinition> Minions { get; }
        public IReadOnlyList<TavernSpellDefinition> TavernSpells { get; }

        public static ChooseOneCardPool Create(
            MinionCatalog minions,
            SpellCatalog spells,
            CardPoolAvailability availability = null)
        {
            var minionChoices = minions?.All
                .Where(definition =>
                    definition != null &&
                    definition.Keywords != null &&
                    definition.Keywords.Contains(Keyword.ChooseOne) &&
                    (availability?.AllowsMinion(definition) ?? definition.InPool))
                .OrderBy(definition => definition.TavernTier)
                .ThenBy(definition => definition.CardId, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<MinionDefinition>();
            var spellChoices = spells?.All
                .Where(definition =>
                    IsChooseOneSpell(definition) &&
                    (availability?.AllowsTavernSpell(definition) ?? definition.InPool))
                .OrderBy(definition => definition.TavernTier)
                .ThenBy(definition => definition.CardNumber, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<TavernSpellDefinition>();
            return new ChooseOneCardPool(minionChoices, spellChoices);
        }

        public static bool IsChooseOneSpell(TavernSpellDefinition definition)
        {
            return definition != null &&
                   string.Equals(definition.Category, "TavernSpell", StringComparison.OrdinalIgnoreCase) &&
                   ((definition.Tags ?? new List<string>()).Any(tag =>
                        string.Equals(tag, "choose_one", StringComparison.OrdinalIgnoreCase)) ||
                    (definition.EnglishText ?? string.Empty).IndexOf("Choose One", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (definition.Text ?? string.Empty).Contains("抉择"));
        }
    }
}
