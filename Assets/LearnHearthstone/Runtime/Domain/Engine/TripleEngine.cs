using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public sealed class TripleResult
    {
        public List<MinionInstance> Remaining = new List<MinionInstance>();
        public MinionInstance Golden;
    }

    public static class TripleEngine
    {
        public static string FindTripleCandidate(IEnumerable<MinionInstance> items, int requiredCopies = 3)
        {
            requiredCopies = Math.Max(2, requiredCopies);
            return items
                .Where(item => !item.Golden)
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
                if (item.DefinitionId == definitionId && !item.Golden && materials.Count < requiredCopies)
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

            var baseItem = materials[0];
            var poolCopiesHeld = materials.Sum(item => item.PoolCopiesHeld);
            var golden = baseItem.Clone();
            golden.InstanceId = owner.ToString().ToLowerInvariant() + "-" + definitionId + "-golden-" + suffix;
            golden.Owner = owner;
            golden.Golden = true;
            golden.Attack = StatMath.SaturatingMultiply(baseItem.Attack, 2, 0, StatMath.MaxStat);
            golden.Health = StatMath.SaturatingMultiply(baseItem.Health, 2, int.MinValue, StatMath.MaxStat);
            golden.MaxHealth = StatMath.SaturatingMultiply(baseItem.MaxHealth, 2, 1, StatMath.MaxStat);
            StatMath.ClampCurrentHealthToMax(golden);
            golden.PoolSource = poolCopiesHeld > 0 ? PoolSource.Pool : PoolSource.Copy;
            golden.PoolCopiesHeld = poolCopiesHeld;

            return new TripleResult { Remaining = remaining, Golden = golden };
        }
    }
}
