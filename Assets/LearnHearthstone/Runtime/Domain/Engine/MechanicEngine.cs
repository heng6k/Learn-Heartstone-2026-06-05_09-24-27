using System;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class MechanicEngine
    {
        private const string ScarletSurvivorCardId = "BG35_814";

        public static void ApplyToMinion(MinionInstance minion, MechanicAction action)
        {
            if (minion == null || action == null)
            {
                return;
            }

            switch (action.Type)
            {
                case MechanicActionType.BuffStats:
                    ApplyStatBuff(minion, action);
                    break;
                case MechanicActionType.SetStats:
                    StatMath.SetStats(minion, action.Attack, action.Health, action.SourceId);
                    break;
                case MechanicActionType.AddKeyword:
                    if (!minion.Keywords.Contains(action.Keyword))
                    {
                        minion.Keywords.Add(action.Keyword);
                    }
                    break;
                case MechanicActionType.RemoveKeyword:
                    minion.Keywords.Remove(action.Keyword);
                    break;
            }
        }

        public static void ApplyToTavern(TavernState tavern, MechanicAction action)
        {
            if (tavern == null || action == null)
            {
                return;
            }

            switch (action.Type)
            {
                case MechanicActionType.GainGold:
                    TavernRules.GainGold(tavern, action.Gold);
                    break;
                case MechanicActionType.ModifyShopGrowth:
                    tavern.Growth.ShopModifiers.Add(new TavernGrowthModifier
                    {
                        Scope = action.Scope,
                        Tribe = action.Tribe,
                        Attack = action.Attack,
                        Health = action.Health,
                        EnchantmentKind = action.EnchantmentKind,
                        SourceId = action.SourceId
                    });
                    break;
            }
        }

        private static void ApplyStatBuff(MinionInstance minion, MechanicAction action)
        {
            StatMath.ApplyEnchantment(minion, new Enchantment
            {
                Id = action.SourceId,
                SourceId = action.SourceId,
                Kind = action.EnchantmentKind,
                AttackBonus = action.Attack,
                HealthBonus = action.Health
            });
            if (minion.CardId == ScarletSurvivorCardId && minion.Attack >= 6 && !minion.Keywords.Contains(Keyword.DivineShield))
            {
                minion.Keywords.Add(Keyword.DivineShield);
            }
        }
    }
}
