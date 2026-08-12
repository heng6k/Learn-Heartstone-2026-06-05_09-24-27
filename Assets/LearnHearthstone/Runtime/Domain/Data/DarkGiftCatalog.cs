using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class DarkGiftCatalog
    {
        private readonly Dictionary<string, DarkGiftDefinition> byId;
        private readonly Dictionary<string, DarkGiftDefinition> byResearchKey;
        private readonly Dictionary<string, DarkGiftDefinition> byRevisionId;

        public DarkGiftCatalog(IEnumerable<DarkGiftDefinition> definitions)
        {
            var items = (definitions ?? Enumerable.Empty<DarkGiftDefinition>())
                .Where(item => item != null)
                .Select(item => item.Clone())
                .ToArray();
            byId = Index(items, item => item.Id, "id");
            byResearchKey = Index(items, item => item.ResearchKey, "research key");
            byRevisionId = Index(items, item => item.RevisionId, "revision id");
            All = Array.AsReadOnly(items);
        }

        public ReadOnlyCollection<DarkGiftDefinition> All { get; }

        public DarkGiftDefinition GetById(string id)
        {
            return Get(byId, id, "id");
        }

        public DarkGiftDefinition GetByResearchKey(string researchKey)
        {
            return Get(byResearchKey, researchKey, "research key");
        }

        public DarkGiftDefinition GetByRevisionId(string revisionId)
        {
            return Get(byRevisionId, revisionId, "revision id");
        }

        private static Dictionary<string, DarkGiftDefinition> Index(
            IEnumerable<DarkGiftDefinition> definitions,
            Func<DarkGiftDefinition, string> key,
            string label)
        {
            var result = new Dictionary<string, DarkGiftDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitions)
            {
                var value = key(definition);
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Dark Gift " + label + " is required.");
                }
                if (!result.TryAdd(value, definition))
                {
                    throw new ArgumentException("Duplicate Dark Gift " + label + ": " + value + ".");
                }
            }
            return result;
        }

        private static DarkGiftDefinition Get(
            IReadOnlyDictionary<string, DarkGiftDefinition> definitions,
            string value,
            string label)
        {
            if (string.IsNullOrWhiteSpace(value) || !definitions.TryGetValue(value, out var definition))
            {
                throw new InvalidOperationException("Dark Gift " + label + " does not exist: " + value);
            }
            return definition;
        }
    }
}
