using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class StatMath
    {
        public const int MaxStat = int.MaxValue;

        public static int SaturatingAdd(int value, int delta, int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            return ClampToInt((long)value + delta, minValue, maxValue);
        }

        public static int SaturatingSubtract(int value, int delta, int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            return ClampToInt((long)value - delta, minValue, maxValue);
        }

        public static int SaturatingMultiply(int value, int multiplier, int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            return ClampToInt((long)value * multiplier, minValue, maxValue);
        }

        public static int SaturatingDelta(int nextValue, int currentValue)
        {
            return ClampToInt((long)nextValue - currentValue, int.MinValue, int.MaxValue);
        }

        public static int SaturatingSum(IEnumerable<int> values, int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            if (values == null)
            {
                return 0;
            }

            long total = 0;
            foreach (var value in values)
            {
                total += value;
                if (total >= maxValue)
                {
                    return maxValue;
                }

                if (total <= minValue)
                {
                    return minValue;
                }
            }

            return ClampToInt(total, minValue, maxValue);
        }

        public static int ClampAttack(long value)
        {
            return ClampToInt(value, 0, MaxStat);
        }

        public static int ClampMaxHealth(long value)
        {
            return ClampToInt(value, 1, MaxStat);
        }

        public static int ClampHealth(long value)
        {
            return ClampToInt(value, int.MinValue, MaxStat);
        }

        public static int DamageHealth(int health, int amount)
        {
            return amount <= 0 ? health : ClampHealth((long)health - amount);
        }

        public static void ApplyStatDelta(MinionInstance target, int attackDelta, int healthDelta)
        {
            if (target == null)
            {
                return;
            }

            target.Attack = ClampAttack((long)target.Attack + attackDelta);
            target.MaxHealth = ClampMaxHealth((long)target.MaxHealth + healthDelta);
            target.Health = ClampHealth((long)target.Health + healthDelta);
            ClampCurrentHealthToMax(target);
        }

        public static void ApplyStatDeltaPreservingDamage(MinionInstance target, int attackDelta, int maxHealthDelta)
        {
            if (target == null)
            {
                return;
            }

            target.Attack = ClampAttack((long)target.Attack + attackDelta);
            target.MaxHealth = ClampMaxHealth((long)target.MaxHealth + maxHealthDelta);
            ClampCurrentHealthToMax(target);
        }

        public static void ApplyEnchantment(MinionInstance target, Enchantment enchantment)
        {
            if (target == null || enchantment == null)
            {
                return;
            }

            if (target.Enchantments == null)
            {
                target.Enchantments = new List<Enchantment>();
            }

            target.Enchantments.Add(enchantment);
            if (enchantment.Kind == EnchantmentKind.SetStats)
            {
                target.Attack = ClampAttack(enchantment.AttackBonus);
                target.MaxHealth = ClampMaxHealth(enchantment.HealthBonus);
                target.Health = target.MaxHealth;
                return;
            }

            ApplyStatDelta(target, enchantment.AttackBonus, enchantment.HealthBonus);
            if (enchantment.AttackBonus > 0 &&
                target.CardId == "BG21_018" &&
                !string.Equals(enchantment.SourceId, "Defiant Shipwright", StringComparison.Ordinal))
            {
                var shipwrightBonus = new Enchantment
                {
                    Id = "Defiant Shipwright",
                    SourceId = "Defiant Shipwright",
                    AttackBonus = 0,
                    HealthBonus = target.Golden ? 2 : 1
                };
                target.Enchantments.Add(shipwrightBonus);
                ApplyStatDelta(target, 0, shipwrightBonus.HealthBonus);
            }
        }

        public static void SetStats(MinionInstance target, int attack, int health, string sourceId)
        {
            ApplyEnchantment(target, new Enchantment
            {
                Id = sourceId,
                SourceId = sourceId,
                Kind = EnchantmentKind.SetStats,
                AttackBonus = ClampAttack(attack),
                HealthBonus = ClampMaxHealth(health)
            });
        }

        public static void RecalculateStatsPreservingDamage(MinionInstance target)
        {
            if (target == null)
            {
                return;
            }

            var damage = (long)target.MaxHealth - target.Health;
            RecalculateAttackAndMaxHealth(target);
            target.Health = ClampHealth((long)target.MaxHealth - damage);
            ClampCurrentHealthToMax(target);
        }

        public static void RecalculateStatsPreservingCurrentHealth(MinionInstance target)
        {
            if (target == null)
            {
                return;
            }

            var currentHealth = target.Health;
            RecalculateAttackAndMaxHealth(target);
            target.Health = ClampHealth(currentHealth);
            ClampCurrentHealthToMax(target);
        }

        public static bool IsBloodGemEnchantment(Enchantment enchantment)
        {
            if (enchantment == null)
            {
                return false;
            }

            if (enchantment.Kind == EnchantmentKind.BloodGem)
            {
                return true;
            }

            return enchantment.Kind == EnchantmentKind.Unspecified &&
                   (ContainsBloodGemText(enchantment.SourceId) || ContainsBloodGemText(enchantment.Id));
        }

        public static void DoubleCurrentStats(MinionInstance target, bool healToMaxHealth)
        {
            if (target == null)
            {
                return;
            }

            target.Attack = SaturatingMultiply(target.Attack, 2, 0, MaxStat);
            target.MaxHealth = SaturatingMultiply(target.MaxHealth, 2, 1, MaxStat);
            target.Health = healToMaxHealth
                ? target.MaxHealth
                : SaturatingMultiply(target.Health, 2, int.MinValue, MaxStat);
            ClampCurrentHealthToMax(target);
        }

        public static void ClampCurrentHealthToMax(MinionInstance target)
        {
            if (target != null && target.Health > target.MaxHealth)
            {
                target.Health = target.MaxHealth;
            }
        }

        private static void RecalculateAttackAndMaxHealth(MinionInstance target)
        {
            var attack = ClampAttack(target.BaseAttack);
            var maxHealth = ClampMaxHealth(target.BaseHealth);
            foreach (var enchantment in target.Enchantments ?? new List<Enchantment>())
            {
                if (enchantment == null)
                {
                    continue;
                }

                if (enchantment.Kind == EnchantmentKind.SetStats)
                {
                    attack = ClampAttack(enchantment.AttackBonus);
                    maxHealth = ClampMaxHealth(enchantment.HealthBonus);
                    continue;
                }

                attack = ClampAttack((long)attack + enchantment.AttackBonus);
                maxHealth = ClampMaxHealth((long)maxHealth + enchantment.HealthBonus);
            }

            target.Attack = attack;
            target.MaxHealth = maxHealth;
        }

        private static bool ContainsBloodGemText(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf("Blood Gem", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int ClampToInt(long value, int minValue, int maxValue)
        {
            if (value > maxValue)
            {
                return maxValue;
            }

            if (value < minValue)
            {
                return minValue;
            }

            return (int)value;
        }
    }
}
