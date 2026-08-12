using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Data
{
    public static class TrinketCatalogLoader
    {
        private const string ResourcePath = "Data/battlegroundsTrinkets";
        private const string LocalizationZhCnResourcePath = "Data/battlegroundsTrinketLocalizationZhCN";

        public static TrinketCatalog LoadFromResources(bool useEnglish = true)
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ResourcePath + ".json");
            }

            if (useEnglish)
            {
                return LoadFromJson(asset.text);
            }

            var localizationAsset = Resources.Load<TextAsset>(LocalizationZhCnResourcePath);
            if (localizationAsset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + LocalizationZhCnResourcePath + ".json");
            }

            return LoadFromJson(asset.text, localizationAsset.text);
        }

        public static TrinketCatalog LoadFromJson(string json)
        {
            return LoadFromJson(json, null);
        }

        public static TrinketCatalog LoadFromJson(string json, string zhCnJson)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.trinkets == null)
            {
                throw new InvalidOperationException("Invalid battlegrounds Trinket payload.");
            }

            var localizedCards = ParseLocalization(zhCnJson);
            var definitions = new List<TrinketDefinition>();
            foreach (var raw in payload.trinkets)
            {
                var definition = ToDefinition(raw);
                ApplyLocalization(definition, localizedCards);
                definitions.Add(definition);
            }

            return new TrinketCatalog(definitions);
        }

        private static Dictionary<string, RawLocalizedCard> ParseLocalization(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var payload = JsonUtility.FromJson<RawLocalizationPayload>(json);
            if (payload == null || payload.cards == null)
            {
                throw new InvalidOperationException("Invalid zh-CN Trinket localization payload.");
            }

            var cards = new Dictionary<string, RawLocalizedCard>(StringComparer.OrdinalIgnoreCase);
            foreach (var card in payload.cards)
            {
                if (card != null && !string.IsNullOrWhiteSpace(card.cardId))
                {
                    cards[card.cardId] = card;
                }
            }

            return cards;
        }

        private static void ApplyLocalization(
            TrinketDefinition definition,
            Dictionary<string, RawLocalizedCard> localizedCards)
        {
            if (localizedCards == null)
            {
                return;
            }

            if (!localizedCards.TryGetValue(definition.CardId ?? string.Empty, out var localized) ||
                string.IsNullOrWhiteSpace(localized.name) ||
                string.IsNullOrWhiteSpace(localized.text))
            {
                throw new InvalidOperationException("Missing zh-CN Trinket localization: " + definition.CardId);
            }

            definition.Name = localized.name;
            definition.Text = localized.text;
        }

        private static TrinketDefinition ToDefinition(RawTrinket raw)
        {
            var implementationStatus = ParseEnum(raw.implementationStatus, TrinketImplementationStatus.FrameworkFirst);
            var offerPoolStatus = ParseOfferPoolStatus(raw.offerPoolStatus, implementationStatus);
            var definition = new TrinketDefinition
            {
                Id = raw.id,
                CardId = string.IsNullOrEmpty(raw.cardId) ? raw.id : raw.cardId,
                ResearchKey = raw.researchKey,
                DbfId = raw.dbfId,
                SourceName = raw.name,
                Name = raw.name,
                SlotKind = ParseEnum(raw.slotKind, TrinketSlotKind.Lesser),
                Cost = Math.Max(0, raw.cost),
                Text = raw.text,
                ImagePath = raw.imagePath,
                ImageUrl = raw.imageUrl,
                Mechanics = raw.mechanics ?? new List<string>(),
                ReferencedTags = raw.referencedTags ?? new List<string>(),
                AssociatedRaces = raw.associatedRaces ?? new List<string>(),
                RelatedDbfId = raw.relatedDbfId,
                Tags = raw.tags ?? new List<string>(),
                EffectIds = raw.effectIds ?? new List<string>(),
                ImplementationStatus = implementationStatus,
                OfferPoolStatus = offerPoolStatus,
                PowerLevel = ParsePowerLevel(raw.powerLevel, offerPoolStatus, implementationStatus),
                EffectFamily = string.IsNullOrWhiteSpace(raw.effectFamily) ? "pending" : raw.effectFamily,
                TriggerTemplate = ParseEnum(raw.triggerTemplate, TrinketTriggerTemplate.Auto),
                EffectTemplate = ParseEnum(raw.effectTemplate, TrinketEffectTemplate.Auto),
                Requires = raw.requires ?? new List<string>(),
                ProxyLevel = string.IsNullOrWhiteSpace(raw.proxyLevel) ? "Blocked" : raw.proxyLevel,
                Notes = raw.notes
            };

            definition.TriggerTemplate = TrinketBehaviorTemplate.ResolveTriggerTemplate(definition);
            definition.EffectTemplate = TrinketBehaviorTemplate.ResolveEffectTemplate(definition);
            return definition;
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse(value, true, out TEnum parsed) ? parsed : fallback;
        }

        private static TrinketOfferPoolStatus ParseOfferPoolStatus(string value, TrinketImplementationStatus implementationStatus)
        {
            if (Enum.TryParse(value, true, out TrinketOfferPoolStatus parsed))
            {
                return parsed;
            }

            return implementationStatus == TrinketImplementationStatus.Implemented
                ? TrinketOfferPoolStatus.Offerable
                : TrinketOfferPoolStatus.DebugOnly;
        }

        private static TrinketPowerLevel ParsePowerLevel(
            string value,
            TrinketOfferPoolStatus offerPoolStatus,
            TrinketImplementationStatus implementationStatus)
        {
            if (Enum.TryParse(value, true, out TrinketPowerLevel parsed))
            {
                return parsed;
            }

            if (implementationStatus != TrinketImplementationStatus.Implemented)
            {
                return TrinketPowerLevel.Pending;
            }

            return offerPoolStatus == TrinketOfferPoolStatus.HiddenEffectOnly
                ? TrinketPowerLevel.Weak
                : TrinketPowerLevel.Medium;
        }

        [Serializable]
        private sealed class RawPayload
        {
            public List<string> sourcePages;
            public string cardsSource;
            public string generatedAt;
            public int count;
            public List<RawTrinket> trinkets;
        }

        [Serializable]
        private sealed class RawTrinket
        {
            public string id;
            public string cardId;
            public string researchKey;
            public int dbfId;
            public string name;
            public string slotKind;
            public int cost;
            public string text;
            public string imagePath;
            public string imageUrl;
            public List<string> mechanics;
            public List<string> referencedTags;
            public List<string> associatedRaces;
            public int relatedDbfId;
            public List<string> tags;
            public List<string> effectIds;
            public string implementationStatus;
            public string offerPoolStatus;
            public string powerLevel;
            public string effectFamily;
            public string triggerTemplate;
            public string effectTemplate;
            public List<string> requires;
            public string proxyLevel;
            public string notes;
        }

        [Serializable]
        private sealed class RawLocalizationPayload
        {
            public string source;
            public string generatedAt;
            public int count;
            public List<RawLocalizedCard> cards;
        }

        [Serializable]
        private sealed class RawLocalizedCard
        {
            public string cardId;
            public string name;
            public string text;
        }
    }
}
