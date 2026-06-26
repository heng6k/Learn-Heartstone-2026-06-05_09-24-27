using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class TrinketCatalog
    {
        private readonly Dictionary<string, TrinketDefinition> byId;
        private readonly Dictionary<string, TrinketDefinition> byCardId;

        public TrinketCatalog(IEnumerable<TrinketDefinition> definitions)
        {
            All = (definitions ?? Enumerable.Empty<TrinketDefinition>()).ToList();
            byId = All
                .Where(trinket => !string.IsNullOrEmpty(trinket.Id))
                .GroupBy(trinket => trinket.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            byCardId = All
                .Where(trinket => !string.IsNullOrEmpty(trinket.CardId))
                .GroupBy(trinket => trinket.CardId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        public List<TrinketDefinition> All { get; }

        public List<TrinketDefinition> Lesser => All.Where(trinket => trinket.SlotKind == TrinketSlotKind.Lesser).ToList();

        public List<TrinketDefinition> Greater => All.Where(trinket => trinket.SlotKind == TrinketSlotKind.Greater).ToList();

        public List<TrinketDefinition> Implemented =>
            All.Where(trinket => trinket.ImplementationStatus == TrinketImplementationStatus.Implemented).ToList();

        public List<TrinketDefinition> Offerable =>
            All.Where(IsOfferable).ToList();

        public List<TrinketDefinition> HiddenEffectOnly =>
            All.Where(trinket =>
                trinket.ImplementationStatus == TrinketImplementationStatus.Implemented &&
                trinket.OfferPoolStatus == TrinketOfferPoolStatus.HiddenEffectOnly).ToList();

        public TrinketDefinition GetById(string id)
        {
            if (!TryGetById(id, out var definition))
            {
                throw new InvalidOperationException("Trinket id does not exist: " + id);
            }

            return definition;
        }

        public TrinketDefinition GetByCardId(string cardId)
        {
            if (!TryGetByCardId(cardId, out var definition))
            {
                throw new InvalidOperationException("Trinket card id does not exist: " + cardId);
            }

            return definition;
        }

        public bool TryGetById(string id, out TrinketDefinition definition)
        {
            return byId.TryGetValue(id ?? string.Empty, out definition);
        }

        public bool TryGetByCardId(string cardId, out TrinketDefinition definition)
        {
            return byCardId.TryGetValue(cardId ?? string.Empty, out definition);
        }

        public List<TrinketDefinition> GetBySlot(TrinketSlotKind slotKind)
        {
            return All.Where(trinket => trinket.SlotKind == slotKind).ToList();
        }

        public List<TrinketDefinition> GetOfferableBySlot(TrinketSlotKind slotKind)
        {
            return Offerable.Where(trinket => trinket.SlotKind == slotKind).ToList();
        }

        private static bool IsOfferable(TrinketDefinition definition)
        {
            return definition != null &&
                definition.ImplementationStatus == TrinketImplementationStatus.Implemented &&
                definition.OfferPoolStatus == TrinketOfferPoolStatus.Offerable;
        }
    }
}
