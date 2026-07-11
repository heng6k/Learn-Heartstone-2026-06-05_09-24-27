using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Services
{
    public static class SideModifierService
    {
        private const string EternalKnightCardId = "BG25_008";
        private const string AstralAutomatonCardId = "BG_TTN_401";
        private const string ScarletSurvivorCardId = "BG35_814";
        private const string EternalKnightSourceId = "Eternal Knight";
        private const string AstralAutomatonSourceId = "Ancestral Automaton";
        private const string UndeadAttackSourceId = "Undead Attack Bonus";

        public static int GetValue(SideCombatModifierState modifiers, SideCombatModifierKind kind)
        {
            if (modifiers == null)
            {
                return 0;
            }

            switch (kind)
            {
                case SideCombatModifierKind.SpellsCastThisGame: return modifiers.SpellsCastThisGame;
                case SideCombatModifierKind.SpellPower: return modifiers.SpellPower;
                case SideCombatModifierKind.TavernSpellBonusAttack: return modifiers.TavernSpellBonusAttack;
                case SideCombatModifierKind.TavernSpellBonusHealth: return modifiers.TavernSpellBonusHealth;
                case SideCombatModifierKind.BloodGemAttackBonus: return modifiers.BloodGemAttackBonus;
                case SideCombatModifierKind.BloodGemHealthBonus: return modifiers.BloodGemHealthBonus;
                case SideCombatModifierKind.UndeadAttackBonus: return modifiers.UndeadAttackBonus;
                case SideCombatModifierKind.EternalKnightDeaths: return modifiers.EternalKnightDeaths;
                case SideCombatModifierKind.AstralAutomatonSummons: return modifiers.AstralAutomatonSummons;
                case SideCombatModifierKind.FriendlyMinionDeathsThisGame: return modifiers.FriendlyMinionDeathsThisGame;
                default: return 0;
            }
        }

        public static void SetValue(SideCombatModifierState modifiers, SideCombatModifierKind kind, int value)
        {
            if (modifiers == null)
            {
                return;
            }

            value = Math.Max(0, value);
            switch (kind)
            {
                case SideCombatModifierKind.SpellsCastThisGame: modifiers.SpellsCastThisGame = value; break;
                case SideCombatModifierKind.SpellPower: modifiers.SpellPower = value; break;
                case SideCombatModifierKind.TavernSpellBonusAttack: modifiers.TavernSpellBonusAttack = value; break;
                case SideCombatModifierKind.TavernSpellBonusHealth: modifiers.TavernSpellBonusHealth = value; break;
                case SideCombatModifierKind.BloodGemAttackBonus: modifiers.BloodGemAttackBonus = value; break;
                case SideCombatModifierKind.BloodGemHealthBonus: modifiers.BloodGemHealthBonus = value; break;
                case SideCombatModifierKind.UndeadAttackBonus: modifiers.UndeadAttackBonus = value; break;
                case SideCombatModifierKind.EternalKnightDeaths: modifiers.EternalKnightDeaths = value; break;
                case SideCombatModifierKind.AstralAutomatonSummons: modifiers.AstralAutomatonSummons = value; break;
                case SideCombatModifierKind.FriendlyMinionDeathsThisGame: modifiers.FriendlyMinionDeathsThisGame = value; break;
            }
        }

        public static void CopyFromTavern(TavernState tavern, SideCombatModifierState modifiers)
        {
            if (tavern == null || modifiers == null)
            {
                return;
            }

            modifiers.SpellsCastThisGame = Math.Max(0, tavern.TavernSpellsCastThisGame);
            modifiers.SpellPower = Math.Max(0, tavern.SpellPower);
            modifiers.TavernSpellBonusAttack = Math.Max(0, tavern.TavernSpellBonusAttack);
            modifiers.TavernSpellBonusHealth = Math.Max(0, tavern.TavernSpellBonusHealth);
            modifiers.BloodGemAttackBonus = Math.Max(0, tavern.BloodGemBonusAttack);
            modifiers.BloodGemHealthBonus = Math.Max(0, tavern.BloodGemBonusHealth);
            modifiers.UndeadAttackBonus = Math.Max(0, tavern.UndeadAttackBonus);
            modifiers.EternalKnightDeaths = Math.Max(0, tavern.EternalKnightDeaths);
            modifiers.AstralAutomatonSummons = Math.Max(0, tavern.AncestralAutomatonSummons);
            modifiers.FriendlyMinionDeathsThisGame = Math.Max(0, tavern.FriendlyMinionDeathsThisGame);
        }

        public static void ApplyToTavern(SideCombatModifierState modifiers, TavernState tavern)
        {
            if (tavern == null || modifiers == null)
            {
                return;
            }

            tavern.TavernSpellsCastThisGame = Math.Max(0, modifiers.SpellsCastThisGame);
            tavern.SpellPower = Math.Max(0, modifiers.SpellPower);
            tavern.TavernSpellBonusAttack = Math.Max(0, modifiers.TavernSpellBonusAttack);
            tavern.TavernSpellBonusHealth = Math.Max(0, modifiers.TavernSpellBonusHealth);
            tavern.BloodGemBonusAttack = Math.Max(0, modifiers.BloodGemAttackBonus);
            tavern.BloodGemBonusHealth = Math.Max(0, modifiers.BloodGemHealthBonus);
            tavern.UndeadAttackBonus = Math.Max(0, modifiers.UndeadAttackBonus);
            tavern.EternalKnightDeaths = Math.Max(0, modifiers.EternalKnightDeaths);
            tavern.AncestralAutomatonSummons = Math.Max(0, modifiers.AstralAutomatonSummons);
            tavern.FriendlyMinionDeathsThisGame = Math.Max(0, modifiers.FriendlyMinionDeathsThisGame);
        }

        public static void ApplyToRetainedCards(IEnumerable<MinionInstance> cards, SideCombatModifierState modifiers)
        {
            foreach (var minion in cards ?? Enumerable.Empty<MinionInstance>())
            {
                ApplyToRetainedCard(minion, modifiers);
            }
        }

        public static bool ApplyCombatRewards(SideCombatModifierState modifiers, IEnumerable<CombatReward> rewards)
        {
            if (modifiers == null)
            {
                return false;
            }

            var changed = false;
            foreach (var reward in rewards ?? Enumerable.Empty<CombatReward>())
            {
                if (reward == null)
                {
                    continue;
                }

                var amount = Math.Max(0, reward.Amount);
                switch (reward.Type)
                {
                    case CombatRewardType.EternalKnightDied:
                        modifiers.EternalKnightDeaths = StatMath.SaturatingAdd(modifiers.EternalKnightDeaths, amount, 0, StatMath.MaxStat);
                        changed = true;
                        break;
                    case CombatRewardType.FriendlyMinionDied:
                        modifiers.FriendlyMinionDeathsThisGame = StatMath.SaturatingAdd(modifiers.FriendlyMinionDeathsThisGame, amount, 0, StatMath.MaxStat);
                        changed = true;
                        break;
                    case CombatRewardType.ImproveBloodGemAttack:
                        modifiers.BloodGemAttackBonus = StatMath.SaturatingAdd(modifiers.BloodGemAttackBonus, amount, 0, StatMath.MaxStat);
                        changed = true;
                        break;
                    case CombatRewardType.ImproveBloodGemHealth:
                        modifiers.BloodGemHealthBonus = StatMath.SaturatingAdd(modifiers.BloodGemHealthBonus, amount, 0, StatMath.MaxStat);
                        changed = true;
                        break;
                    case CombatRewardType.ImproveBloodGemStats:
                        modifiers.BloodGemAttackBonus = StatMath.SaturatingAdd(modifiers.BloodGemAttackBonus, StatMath.SaturatingMultiply(reward.Attack, amount, 0, StatMath.MaxStat), 0, StatMath.MaxStat);
                        modifiers.BloodGemHealthBonus = StatMath.SaturatingAdd(modifiers.BloodGemHealthBonus, StatMath.SaturatingMultiply(reward.Health, amount, 0, StatMath.MaxStat), 0, StatMath.MaxStat);
                        changed = true;
                        break;
                    case CombatRewardType.ImproveTavernSpellStats:
                        modifiers.TavernSpellBonusAttack = StatMath.SaturatingAdd(modifiers.TavernSpellBonusAttack, StatMath.SaturatingMultiply(reward.Attack, amount, 0, StatMath.MaxStat), 0, StatMath.MaxStat);
                        modifiers.TavernSpellBonusHealth = StatMath.SaturatingAdd(modifiers.TavernSpellBonusHealth, StatMath.SaturatingMultiply(reward.Health, amount, 0, StatMath.MaxStat), 0, StatMath.MaxStat);
                        changed = true;
                        break;
                    case CombatRewardType.ImproveUndeadAttack:
                        modifiers.UndeadAttackBonus = StatMath.SaturatingAdd(modifiers.UndeadAttackBonus, amount, 0, StatMath.MaxStat);
                        changed = true;
                        break;
                    case CombatRewardType.ImproveTavernSpellAttack:
                        modifiers.TavernSpellBonusAttack = StatMath.SaturatingAdd(modifiers.TavernSpellBonusAttack, amount, 0, StatMath.MaxStat);
                        changed = true;
                        break;
                }
            }

            return changed;
        }

        private static void ApplyToRetainedCard(MinionInstance minion, SideCombatModifierState modifiers)
        {
            if (minion == null || modifiers == null || minion.CardKind != CardKind.Minion)
            {
                return;
            }

            ApplyOrRemoveTrackedBuff(minion, UndeadAttackSourceId, Math.Max(0, modifiers.UndeadAttackBonus), 0, modifiers.UndeadAttackBonus > 0 && minion.Tribes.Contains(Tribe.Undead));

            var eternalDeaths = Math.Max(0, modifiers.EternalKnightDeaths);
            ApplyOrRemoveTrackedBuff(
                minion,
                EternalKnightSourceId,
                StatMath.SaturatingMultiply(eternalDeaths, minion.Golden ? 8 : 4, 0, StatMath.MaxStat),
                StatMath.SaturatingMultiply(eternalDeaths, minion.Golden ? 4 : 2, 0, StatMath.MaxStat),
                eternalDeaths > 0 && minion.CardId == EternalKnightCardId);

            var otherAutomatonSummons = Math.Max(0, modifiers.AstralAutomatonSummons - 1);
            ApplyOrRemoveTrackedBuff(
                minion,
                AstralAutomatonSourceId,
                StatMath.SaturatingMultiply(otherAutomatonSummons, minion.Golden ? 6 : 3, 0, StatMath.MaxStat),
                StatMath.SaturatingMultiply(otherAutomatonSummons, minion.Golden ? 4 : 2, 0, StatMath.MaxStat),
                otherAutomatonSummons > 0 && minion.CardId == AstralAutomatonCardId);
        }

        private static void ApplyOrRemoveTrackedBuff(MinionInstance target, string sourceId, int attack, int health, bool shouldApply)
        {
            if (!shouldApply)
            {
                RemoveTrackedBuff(target, sourceId);
                return;
            }

            var existing = target.Enchantments.FirstOrDefault(enchantment => enchantment.SourceId == sourceId);
            var currentAttack = existing?.AttackBonus ?? 0;
            var currentHealth = existing?.HealthBonus ?? 0;
            StatMath.ApplyStatDelta(target, StatMath.SaturatingDelta(attack, currentAttack), StatMath.SaturatingDelta(health, currentHealth));
            if (existing == null)
            {
                target.Enchantments.Add(new Enchantment
                {
                    Id = sourceId,
                    SourceId = sourceId,
                    AttackBonus = attack,
                    HealthBonus = health
                });
            }
            else
            {
                existing.AttackBonus = attack;
                existing.HealthBonus = health;
            }

            RefreshScarletSurvivor(target);
        }

        private static void RemoveTrackedBuff(MinionInstance target, string sourceId)
        {
            if (target?.Enchantments == null)
            {
                return;
            }

            foreach (var enchantment in target.Enchantments.Where(value => value.SourceId == sourceId).ToList())
            {
                StatMath.ApplyStatDeltaPreservingDamage(target, StatMath.SaturatingSubtract(0, enchantment.AttackBonus), StatMath.SaturatingSubtract(0, enchantment.HealthBonus));
                target.Enchantments.Remove(enchantment);
            }

            RefreshScarletSurvivor(target);
        }

        private static void RefreshScarletSurvivor(MinionInstance target)
        {
            if (target != null && target.CardId == ScarletSurvivorCardId && target.Attack >= 6 && !target.Keywords.Contains(Keyword.DivineShield))
            {
                target.Keywords.Add(Keyword.DivineShield);
            }
        }
    }
}
