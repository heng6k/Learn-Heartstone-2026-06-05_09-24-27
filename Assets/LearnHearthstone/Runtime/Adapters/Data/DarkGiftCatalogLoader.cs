using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Data
{
    public static class DarkGiftCatalogLoader
    {
        private const string ResourcePath = "Data/battlegroundsDarkGifts";
        private const string LocalizationZhCnResourcePath = "Data/battlegroundsDarkGiftLocalizationZhCN";

        public static DarkGiftCatalog LoadFromResources(bool useEnglish = true)
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

            var localization = Resources.Load<TextAsset>(LocalizationZhCnResourcePath);
            if (localization == null)
            {
                throw new InvalidOperationException("Missing Resources/" + LocalizationZhCnResourcePath + ".json");
            }
            return LoadFromJson(asset.text, localization.text);
        }

        public static DarkGiftCatalog LoadFromJson(string json, string zhCnJson = null)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.darkGifts == null)
            {
                throw new InvalidOperationException("Invalid battlegrounds Dark Gift payload.");
            }
            if (payload.count != payload.darkGifts.Count)
            {
                throw new InvalidOperationException("Dark Gift payload count does not match its definitions.");
            }

            var localization = ParseLocalization(zhCnJson);
            var definitions = new List<DarkGiftDefinition>();
            foreach (var raw in payload.darkGifts)
            {
                if (raw == null)
                {
                    throw new InvalidOperationException("Dark Gift payload contains a null definition.");
                }
                var definition = ToDefinition(raw);
                if (localization != null)
                {
                    if (!localization.TryGetValue(definition.Id, out var localized) ||
                        string.IsNullOrWhiteSpace(localized.name) ||
                        string.IsNullOrWhiteSpace(localized.text))
                    {
                        throw new InvalidOperationException("Missing zh-CN Dark Gift localization: " + definition.Id);
                    }
                    definition.DisplayName = localized.name;
                    definition.Text = localized.text;
                }
                definitions.Add(definition);
            }
            return new DarkGiftCatalog(definitions);
        }

        private static DarkGiftDefinition ToDefinition(RawDarkGift raw)
        {
            return new DarkGiftDefinition
            {
                Id = raw.id,
                CardId = raw.cardId,
                DbfId = raw.dbfId,
                ResearchKey = raw.researchKey,
                RevisionId = raw.revisionId,
                EffectRevision = raw.effectRevision,
                SourceLevel = raw.sourceLevel,
                DisplayName = raw.name,
                Text = raw.text,
                ImagePath = raw.imagePath,
                ImageSource = raw.imageSource,
                EarliestOfferRound = raw.earliestOfferRound,
                LatestOfferRound = raw.latestOfferRound,
                AvailabilityTags = raw.availabilityTags ?? new List<string>(),
                CompatibilityTags = raw.compatibilityTags ?? new List<string>(),
                RequiredMinionTags = raw.requiredMinionTags ?? new List<string>(),
                ExcludedMinionTags = raw.excludedMinionTags ?? new List<string>(),
                TriggerSpec = raw.triggerSpec,
                TriggerDelayRounds = Math.Max(0, raw.triggerDelayRounds),
                ChoiceSpec = raw.choiceSpec,
                StackPolicy = raw.stackPolicy,
                MaxStacks = raw.maxStacks > 0 ? raw.maxStacks : 1,
                DurationPolicy = raw.durationPolicy,
                DurationRounds = Math.Max(0, raw.durationRounds),
                InitialUses = Math.Max(0, raw.initialUses),
                CooldownRounds = Math.Max(0, raw.cooldownRounds),
                EffectIds = raw.effectIds ?? new List<string>(),
                ImplementationStatus = ParseEnum(raw.implementationStatus, DarkGiftImplementationStatus.Planned)
            };
        }

        private static Dictionary<string, RawLocalizedGift> ParseLocalization(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }
            var payload = JsonUtility.FromJson<RawLocalizationPayload>(json);
            if (payload == null || payload.gifts == null || payload.count != payload.gifts.Count)
            {
                throw new InvalidOperationException("Invalid zh-CN Dark Gift localization payload.");
            }
            var result = new Dictionary<string, RawLocalizedGift>(StringComparer.OrdinalIgnoreCase);
            foreach (var gift in payload.gifts)
            {
                if (gift == null || string.IsNullOrWhiteSpace(gift.id) || !result.TryAdd(gift.id, gift))
                {
                    throw new InvalidOperationException("Duplicate or missing zh-CN Dark Gift localization id.");
                }
            }
            return result;
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse(value, true, out TEnum parsed) ? parsed : fallback;
        }

        [Serializable]
        private sealed class RawPayload
        {
            public int schemaVersion;
            public int count;
            public List<RawDarkGift> darkGifts;
        }

        [Serializable]
        private sealed class RawDarkGift
        {
            public string id;
            public string cardId;
            public int dbfId;
            public string researchKey;
            public string revisionId;
            public string effectRevision;
            public string sourceLevel;
            public string name;
            public string text;
            public string imagePath;
            public string imageSource;
            public int earliestOfferRound;
            public int latestOfferRound;
            public List<string> availabilityTags;
            public List<string> compatibilityTags;
            public List<string> requiredMinionTags;
            public List<string> excludedMinionTags;
            public string triggerSpec;
            public int triggerDelayRounds;
            public string choiceSpec;
            public string stackPolicy;
            public int maxStacks;
            public string durationPolicy;
            public int durationRounds;
            public int initialUses;
            public int cooldownRounds;
            public List<string> effectIds;
            public string implementationStatus;
        }

        [Serializable]
        private sealed class RawLocalizationPayload
        {
            public int schemaVersion;
            public string locale;
            public int count;
            public List<RawLocalizedGift> gifts;
        }

        [Serializable]
        private sealed class RawLocalizedGift
        {
            public string id;
            public string name;
            public string text;
        }
    }
}
