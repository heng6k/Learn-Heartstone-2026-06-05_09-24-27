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
