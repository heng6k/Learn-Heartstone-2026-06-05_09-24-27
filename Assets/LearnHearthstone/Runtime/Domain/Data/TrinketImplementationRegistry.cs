using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class TrinketImplementation
    {
        public string CardId;
        public string Name;
        public TrinketSlotKind SlotKind;
        public TrinketImplementationStatus Status;
        public TrinketOfferPoolStatus OfferPoolStatus;
        public TrinketPowerLevel PowerLevel;
        public string EffectFamily;
        public string Note;
        public List<string> EffectIds = new List<string>();
    }

    public static class TrinketImplementationRegistry
    {
        public static List<TrinketImplementation> All(TrinketCatalog catalog)
        {
            if (catalog == null)
            {
                return new List<TrinketImplementation>();
            }

            return catalog.All.Select(FromDefinition).ToList();
        }

        public static TrinketImplementation FindByCardId(TrinketCatalog catalog, string cardId)
        {
            if (catalog != null && catalog.TryGetByCardId(cardId, out var definition))
            {
                return FromDefinition(definition);
            }

            return Unregistered(cardId);
        }

        public static TrinketImplementation FromDefinition(TrinketDefinition definition)
        {
            if (definition == null)
            {
                return Unregistered(null);
            }

            return new TrinketImplementation
            {
                CardId = definition.CardId,
                Name = definition.Name,
                SlotKind = definition.SlotKind,
                Status = definition.ImplementationStatus,
                OfferPoolStatus = definition.OfferPoolStatus,
                PowerLevel = definition.PowerLevel,
                EffectFamily = definition.EffectFamily,
                Note = string.IsNullOrWhiteSpace(definition.Notes)
                    ? "No Trinket implementation note has been registered."
                    : definition.Notes,
                EffectIds = definition.EffectIds == null ? new List<string>() : new List<string>(definition.EffectIds)
            };
        }

        private static TrinketImplementation Unregistered(string cardId)
        {
            return new TrinketImplementation
            {
                CardId = cardId,
                Name = "Unregistered",
                Status = TrinketImplementationStatus.Unregistered,
                OfferPoolStatus = TrinketOfferPoolStatus.Disabled,
                PowerLevel = TrinketPowerLevel.Pending,
                EffectFamily = "unregistered",
                Note = "No Trinket implementation status has been registered for this card id."
            };
        }
    }
}
