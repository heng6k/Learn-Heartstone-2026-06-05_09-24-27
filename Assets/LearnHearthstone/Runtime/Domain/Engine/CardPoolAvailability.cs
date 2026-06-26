using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public sealed class CardPoolAvailability
    {
        private readonly bool useDefault;
        private readonly HashSet<string> enabledMinionCardIds;
        private readonly HashSet<string> enabledTavernSpellCardNumbers;

        public CardPoolAvailability(CardPoolVersionSelection selection)
        {
            useDefault = selection == null || selection.IsDefault;
            enabledMinionCardIds = selection?.EnabledMinionCardIds ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            enabledTavernSpellCardNumbers = selection?.EnabledTavernSpellCardNumbers ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool AllowsMinion(MinionDefinition minion)
        {
            if (minion == null || !minion.InPool || IsDuoCardId(minion.CardId))
            {
                return false;
            }

            return useDefault || enabledMinionCardIds.Contains(minion.CardId);
        }

        public bool AllowsTavernSpell(TavernSpellDefinition spell)
        {
            if (spell == null || !spell.InPool || spell.Category != "TavernSpell")
            {
                return false;
            }

            return useDefault || enabledTavernSpellCardNumbers.Contains(spell.CardNumber);
        }

        private static bool IsDuoCardId(string value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith("BGDUO", StringComparison.OrdinalIgnoreCase);
        }
    }
}
