using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Data
{
    public static class QuestCatalogLoader
    {
        private const string ResourcePath = "Data/battlegroundsQuests";
        private const string LocalizationZhCnResourcePath = "Data/battlegroundsQuestLocalizationZhCN";

        public static QuestCatalog LoadFromResources(bool useEnglish = true)
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

        public static QuestCatalog LoadFromJson(string json)
        {
            return LoadFromJson(json, null);
        }

        public static QuestCatalog LoadFromJson(string json, string zhCnJson)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.quests == null || payload.rewards == null)
            {
                throw new InvalidOperationException("Invalid battlegrounds Quest payload.");
            }

            var localizedCards = ParseLocalization(zhCnJson);
            var quests = new List<QuestDefinition>();
            foreach (var raw in payload.quests)
            {
                var definition = ToQuest(raw);
                ApplyLocalization(definition.CardId, localizedCards, out var name, out var text);
                if (localizedCards != null)
                {
                    definition.Name = name;
                    definition.Text = text;
                }

                quests.Add(definition);
            }

            var rewards = new List<QuestRewardDefinition>();
            foreach (var raw in payload.rewards)
            {
                var definition = ToReward(raw);
                ApplyLocalization(definition.CardId, localizedCards, out var name, out var text);
                if (localizedCards != null)
                {
                    definition.Name = name;
                    definition.Text = text;
                }

                rewards.Add(definition);
            }

            return new QuestCatalog(quests, rewards);
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
                throw new InvalidOperationException("Invalid zh-CN Quest/Reward localization payload.");
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
            string cardId,
            Dictionary<string, RawLocalizedCard> localizedCards,
            out string name,
            out string text)
        {
            name = null;
            text = null;
            if (localizedCards == null)
            {
                return;
            }

            if (!localizedCards.TryGetValue(cardId ?? string.Empty, out var localized) ||
                string.IsNullOrWhiteSpace(localized.name) ||
                string.IsNullOrWhiteSpace(localized.text))
            {
                throw new InvalidOperationException("Missing zh-CN Quest/Reward localization: " + cardId);
            }

            name = localized.name;
            text = localized.text;
        }

        private static QuestDefinition ToQuest(RawQuest raw)
        {
            return new QuestDefinition
            {
                Id = raw.id,
                CardId = string.IsNullOrEmpty(raw.cardId) ? raw.id : raw.cardId,
                DbfId = raw.dbfId,
                Name = raw.name,
                Text = raw.text,
                ImagePath = raw.imagePath,
                ImageUrl = raw.imageUrl,
                Objective = new QuestObjectiveDefinition
                {
                    Kind = ParseEnum(raw.objectiveKind, QuestObjectiveKind.BuyCards),
                    RequiredAmount = Math.Max(1, raw.requiredAmount),
                    RequiredTag = raw.requiredTag
                },
                DefaultRewardId = raw.defaultRewardId,
                Tags = raw.tags ?? new List<string>(),
                ImplementationStatus = ParseEnum(raw.implementationStatus, QuestImplementationStatus.FrameworkFirst),
                Notes = raw.notes
            };
        }

        private static QuestRewardDefinition ToReward(RawReward raw)
        {
            return new QuestRewardDefinition
            {
                Id = raw.id,
                CardId = string.IsNullOrEmpty(raw.cardId) ? raw.id : raw.cardId,
                DbfId = raw.dbfId,
                Name = raw.name,
                Text = raw.text,
                ImagePath = raw.imagePath,
                ImageUrl = raw.imageUrl,
                Trigger = ParseEnum(raw.trigger, QuestRewardTrigger.OnComplete),
                EffectKind = ParseEnum(raw.effectKind, QuestRewardEffectKind.None),
                GoldAmount = Math.Max(0, raw.goldAmount),
                MaxGoldAmount = Math.Max(0, raw.maxGoldAmount),
                AttackBonus = Math.Max(0, raw.attackBonus),
                HealthBonus = Math.Max(0, raw.healthBonus),
                TargetCount = Math.Max(0, raw.targetCount),
                Improves = raw.improves,
                PowerLevel = ParsePowerLevel(raw.powerLevel, raw.offerPoolStatus),
                OfferPoolStatus = ParseEnum(raw.offerPoolStatus, QuestOfferPoolStatus.Offerable),
                Tags = raw.tags ?? new List<string>(),
                ImplementationStatus = ParseEnum(raw.implementationStatus, QuestImplementationStatus.FrameworkFirst),
                Notes = raw.notes
            };
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse(value, true, out TEnum parsed) ? parsed : fallback;
        }

        private static QuestRewardPowerLevel ParsePowerLevel(string value, string offerPoolStatus)
        {
            if (Enum.TryParse(value, true, out QuestRewardPowerLevel parsed))
            {
                return parsed;
            }

            return ParseEnum(offerPoolStatus, QuestOfferPoolStatus.Offerable) == QuestOfferPoolStatus.HiddenEffectOnly
                ? QuestRewardPowerLevel.Weak
                : QuestRewardPowerLevel.Medium;
        }

        [Serializable]
        private sealed class RawPayload
        {
            public List<string> sourcePages;
            public string cardsSource;
            public string generatedAt;
            public List<RawQuest> quests;
            public List<RawReward> rewards;
        }

        [Serializable]
        private sealed class RawQuest
        {
            public string id;
            public string cardId;
            public int dbfId;
            public string name;
            public string text;
            public string imagePath;
            public string imageUrl;
            public string objectiveKind;
            public int requiredAmount;
            public string requiredTag;
            public string defaultRewardId;
            public List<string> tags;
            public string implementationStatus;
            public string notes;
        }

        [Serializable]
        private sealed class RawReward
        {
            public string id;
            public string cardId;
            public int dbfId;
            public string name;
            public string text;
            public string imagePath;
            public string imageUrl;
            public string trigger;
            public string effectKind;
            public int goldAmount;
            public int maxGoldAmount;
            public int attackBonus;
            public int healthBonus;
            public int targetCount;
            public bool improves;
            public string powerLevel;
            public string offerPoolStatus;
            public List<string> tags;
            public string implementationStatus;
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
