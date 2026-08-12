using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public sealed class TripleResult
    {
        public List<MinionInstance> Remaining = new List<MinionInstance>();
        public List<string> ConsumedInstanceIds = new List<string>();
        public MinionInstance Golden;
    }

    public static class TripleEngine
    {
        public static string FindTripleCandidate(IEnumerable<MinionInstance> items, int requiredCopies = 3)
        {
            requiredCopies = Math.Max(2, requiredCopies);
            return items
                .Where(item => IsTripleMaterial(item) && !item.Golden)
                .GroupBy(item => item.DefinitionId)
                .Where(group => group.Count() >= requiredCopies)
                .Select(group => group.Key)
                .FirstOrDefault();
        }

        public static TripleResult ResolveTriple(IList<MinionInstance> items, string definitionId, BoardSide owner, string suffix = "triple", int requiredCopies = 3)
        {
            requiredCopies = Math.Max(2, requiredCopies);
            var materials = new List<MinionInstance>();
            var remaining = new List<MinionInstance>();

            foreach (var item in items)
            {
                if (IsTripleMaterial(item) && item.DefinitionId == definitionId && !item.Golden && materials.Count < requiredCopies)
                {
                    materials.Add(item);
                }
                else
                {
                    remaining.Add(item);
                }
            }

            if (materials.Count < requiredCopies)
            {
                throw new InvalidOperationException("Not enough minions to resolve triple.");
            }

            var golden = CreateGoldenFromMaterials(materials, definitionId, owner, suffix);

            return new TripleResult
            {
                Remaining = remaining,
                ConsumedInstanceIds = materials.Select(item => item.InstanceId).ToList(),
                Golden = golden
            };
        }

        public static MinionInstance CreateGoldenFromMaterials(IReadOnlyList<MinionInstance> materials, string definitionId, BoardSide owner, string suffix = "triple")
        {
            if (materials == null || materials.Count == 0)
            {
                throw new InvalidOperationException("At least one triple material is required.");
            }

            var baseItem = materials[0];
            var hasNormalBaseStats = baseItem.BaseHealth > 0;
            var normalBaseAttack = hasNormalBaseStats ? baseItem.BaseAttack : baseItem.Attack;
            var normalBaseHealth = hasNormalBaseStats ? baseItem.BaseHealth : baseItem.MaxHealth;
            var poolCopiesHeld = materials.Sum(item => item.PoolCopiesHeld);
            var golden = baseItem.Clone();
            golden.InstanceId = owner.ToString().ToLowerInvariant() + "-" + definitionId + "-golden-" + suffix;
            golden.Owner = owner;
            golden.Golden = true;
            golden.BaseAttack = StatMath.SaturatingMultiply(normalBaseAttack, 2, 0, StatMath.MaxStat);
            golden.BaseHealth = StatMath.SaturatingMultiply(normalBaseHealth, 2, 1, StatMath.MaxStat);
            golden.Attack = golden.BaseAttack;
            golden.Health = golden.BaseHealth;
            golden.MaxHealth = golden.BaseHealth;
            golden.Enchantments = new List<Enchantment>();
            golden.Keywords = materials
                .Where(item => item.Keywords != null)
                .SelectMany(item => item.Keywords)
                .Distinct()
                .ToList();

            foreach (var material in materials)
            {
                foreach (var enchantment in material.Enchantments ?? Enumerable.Empty<Enchantment>())
                {
                    StatMath.ApplyEnchantment(golden, enchantment?.Clone());
                }
            }

            StatMath.ClampCurrentHealthToMax(golden);
            golden.PoolSource = materials.Any(item => item.PoolSource == PoolSource.Buddy) && poolCopiesHeld > 0
                ? PoolSource.Buddy
                : poolCopiesHeld > 0 ? PoolSource.Pool : PoolSource.Copy;
            golden.PoolCopiesHeld = poolCopiesHeld;
            return golden;
        }

        private static bool IsTripleMaterial(MinionInstance item)
        {
            return item != null &&
                   (item.CardKind == CardKind.Minion || item.CardKind == CardKind.HeroBuddy) &&
                   !string.IsNullOrEmpty(item.DefinitionId);
        }
    }
}
