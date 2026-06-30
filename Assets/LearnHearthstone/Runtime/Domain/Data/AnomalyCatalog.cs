using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class AnomalyCatalog
    {
        private readonly Dictionary<string, AnomalyDefinition> byCardId;

        public AnomalyCatalog(IEnumerable<AnomalyDefinition> definitions)
        {
            All = definitions == null ? new List<AnomalyDefinition>() : definitions.ToList();
            byCardId = All
                .Where(definition => !string.IsNullOrEmpty(definition.CardId))
                .GroupBy(definition => definition.CardId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        public List<AnomalyDefinition> All { get; private set; }

        public List<AnomalyDefinition> GetByPool(AnomalyPoolVersion version)
        {
            return All
                .Where(definition =>
                    definition.SourcePools != null &&
                    definition.SourcePools.Contains(version))
                .ToList();
        }

        public AnomalyDefinition GetByCardId(string cardId)
        {
            if (!byCardId.TryGetValue(cardId, out var definition))
            {
                throw new InvalidOperationException("Anomaly card id does not exist: " + cardId);
            }

            return definition;
        }

        public bool TryGetByCardId(string cardId, out AnomalyDefinition definition)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                definition = null;
                return false;
            }

            return byCardId.TryGetValue(cardId, out definition);
        }
    }
}
