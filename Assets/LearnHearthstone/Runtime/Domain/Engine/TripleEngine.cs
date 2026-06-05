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
        public static string FindTripleCandidate(IEnumerable<MinionInstance> items)
        {
            return items
                .Where(item => !item.Golden)
                .GroupBy(item => item.DefinitionId)
                .Where(group => group.Count() >= 3)
                .Select(group => group.Key)
                .FirstOrDefault();
        }

        public static TripleResult ResolveTriple(IList<MinionInstance> items, string definitionId, BoardSide owner, string suffix = "triple")
        {
            var materials = new List<MinionInstance>();
            var remaining = new List<MinionInstance>();

            foreach (var item in items)
            {
                if (item.DefinitionId == definitionId && !item.Golden && materials.Count < 3)
                {
                    materials.Add(item);
                }
                else
                {
                    remaining.Add(item);
                }
            }

            if (materials.Count < 3)
            {
                throw new InvalidOperationException("Not enough minions to resolve triple.");
            }

            var baseItem = materials[0];
            var poolCopiesHeld = materials.Sum(item => item.PoolCopiesHeld);
            var golden = baseItem.Clone();
            golden.InstanceId = owner.ToString().ToLowerInvariant() + "-" + definitionId + "-golden-" + suffix;
            golden.Owner = owner;
            golden.Golden = true;
            golden.Attack = baseItem.Attack * 2;
            golden.Health = baseItem.Health * 2;
            golden.MaxHealth = baseItem.MaxHealth * 2;
            golden.PoolSource = poolCopiesHeld > 0 ? PoolSource.Pool : PoolSource.Copy;
            golden.PoolCopiesHeld = poolCopiesHeld;

            return new TripleResult { Remaining = remaining, Golden = golden };
        }
    }
}
