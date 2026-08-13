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
        private readonly HashSet<string> versionMinionCardIds;
        private readonly HashSet<string> versionTavernSpellCardNumbers;
        private readonly bool hasVersionMinionPool;
        private readonly bool hasVersionTavernSpellPool;

        public CardPoolAvailability(CardPoolVersionSelection selection, ContentSetDefinition contentSet = null)
        {
            useDefault = selection == null || selection.IsDefault;
            enabledMinionCardIds = selection?.EnabledMinionCardIds ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            enabledTavernSpellCardNumbers = selection?.EnabledTavernSpellCardNumbers ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            versionMinionCardIds = Membership(contentSet, EntityKind.Minion);
            versionTavernSpellCardNumbers = Membership(contentSet, EntityKind.TavernSpell);
            hasVersionMinionPool = versionMinionCardIds.Count > 0;
            hasVersionTavernSpellPool = versionTavernSpellCardNumbers.Count > 0;
        }

        public bool AllowsMinion(MinionDefinition minion)
        {
            if (minion == null || IsDuoCardId(minion.CardId))
            {
                return false;
            }

            var inVersionPool = hasVersionMinionPool
                ? versionMinionCardIds.Contains(minion.CardId)
                : minion.InPool;
            return inVersionPool && (useDefault || enabledMinionCardIds.Contains(minion.CardId));
        }

        public bool AllowsTavernSpell(TavernSpellDefinition spell)
        {
            if (spell == null || spell.Category != "TavernSpell" || IsDuoCardId(spell.CardNumber))
            {
                return false;
            }

            var inVersionPool = hasVersionTavernSpellPool
                ? versionTavernSpellCardNumbers.Contains(spell.CardNumber)
                : spell.InPool;
            return inVersionPool && (useDefault || enabledTavernSpellCardNumbers.Contains(spell.CardNumber));
        }

        private static HashSet<string> Membership(ContentSetDefinition contentSet, EntityKind kind)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (contentSet == null)
            {
                return result;
            }

            foreach (var entry in contentSet.PoolMembership)
            {
                if (entry.Kind == kind)
                {
                    result.Add(entry.StableEntityId);
                }
            }

            return result;
        }

        private static bool IsDuoCardId(string value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith("BGDUO", StringComparison.OrdinalIgnoreCase);
        }
    }
}
