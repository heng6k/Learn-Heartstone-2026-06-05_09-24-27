using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Engine
{
    public static class TavernRules
    {
        public const int MinTavernTier = 1;
        public const int MaxTavernTier = 7;

        private static readonly IReadOnlyDictionary<int, int> ShopSizeByTier = new Dictionary<int, int>
        {
            { 1, 3 },
            { 2, 4 },
            { 3, 4 },
            { 4, 5 },
            { 5, 5 },
            { 6, 6 },
            { 7, 6 }
        };

        private static readonly IReadOnlyDictionary<int, int> UpgradeCostByTier = new Dictionary<int, int>
        {
            { 1, 5 },
            { 2, 7 },
            { 3, 8 },
            { 4, 9 },
            { 5, 10 },
            { 6, 11 }
        };

        public static int GetShopSize(int tier)
        {
            return ShopSizeByTier.TryGetValue(tier, out var size) ? size : ShopSizeByTier[MinTavernTier];
        }

        public static int GetUpgradeCost(int tier)
        {
            if (!UpgradeCostByTier.TryGetValue(tier, out var cost))
            {
                throw new InvalidOperationException("Tavern tier is already maxed.");
            }

            return cost;
        }

        public static int GetMaxGoldForRound(int round)
        {
            return Math.Min(10, 2 + Math.Max(1, round));
        }

        public static int DecrementUpgradeCost(int cost)
        {
            return Math.Max(0, cost - 1);
        }
    }
}
