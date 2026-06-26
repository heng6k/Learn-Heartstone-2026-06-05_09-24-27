using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public sealed class MinionPool
    {
        private readonly IReadOnlyDictionary<string, MinionDefinition> definitionsById;
        private readonly List<MinionDefinition> definitions;
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        public MinionPool(
            IEnumerable<MinionDefinition> definitions,
            IDictionary<string, int> initial = null,
            IReadOnlyCollection<Tribe> activeTribes = null,
            Func<MinionDefinition, bool> availability = null)
        {
            this.definitions = definitions
                .Where(definition =>
                    (availability == null || availability(definition)) &&
                    TribeAvailabilityRules.IsMinionAvailable(definition, activeTribes))
                .ToList();
            definitionsById = this.definitions.ToDictionary(definition => definition.Id, definition => definition);

            foreach (var definition in this.definitions)
            {
                if (!definition.InPool)
                {
                    continue;
                }

                var initialCount = initial != null && initial.TryGetValue(definition.Id, out var count)
                    ? Math.Min(definition.PoolCount, Math.Max(0, count))
                    : definition.PoolCount;
                counts[definition.Id] = initialCount;
            }
        }

        public int Remaining(string definitionId)
        {
            return counts.TryGetValue(definitionId, out var count) ? count : 0;
        }

        public void Occupy(string definitionId, int copies = 1)
        {
            var current = Remaining(definitionId);
            if (current < copies)
            {
                throw new InvalidOperationException("Insufficient minion copies: " + definitionId);
            }

            counts[definitionId] = current - copies;
        }

        public void Release(string definitionId, int copies = 1)
        {
            if (!definitionsById.TryGetValue(definitionId, out var definition) || !definition.InPool)
            {
                return;
            }

            counts[definitionId] = Math.Min(definition.PoolCount, Remaining(definitionId) + copies);
        }

        public List<MinionDefinition> DrawShop(int tier, int size, SeededRng rng, int minimumTier = TavernRules.MinTavernTier)
        {
            var drawn = new List<MinionDefinition>();
            var minTier = Math.Max(TavernRules.MinTavernTier, minimumTier);
            for (var index = 0; index < size; index += 1)
            {
                var candidates = definitions
                    .Where(definition =>
                        definition.InPool &&
                        definition.TavernTier >= minTier &&
                        definition.TavernTier <= tier &&
                        Remaining(definition.Id) > 0)
                    .ToList();
                if (candidates.Count == 0)
                {
                    break;
                }

                var picked = rng.Pick(candidates);
                Occupy(picked.Id);
                drawn.Add(picked);
            }

            return drawn;
        }

        public Dictionary<string, int> Snapshot()
        {
            return new Dictionary<string, int>(counts);
        }
    }
}
