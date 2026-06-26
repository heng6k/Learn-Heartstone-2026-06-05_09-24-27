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

        public static QuestCatalog LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ResourcePath + ".json");
            }

            return LoadFromJson(asset.text);
        }

        public static QuestCatalog LoadFromJson(string json)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.quests == null || payload.rewards == null)
            {
                throw new InvalidOperationException("Invalid battlegrounds Quest payload.");
            }

            var quests = new List<QuestDefinition>();
            foreach (var raw in payload.quests)
            {
                quests.Add(ToQuest(raw));
            }

            var rewards = new List<QuestRewardDefinition>();
            foreach (var raw in payload.rewards)
            {
                rewards.Add(ToReward(raw));
            }

            return new QuestCatalog(quests, rewards);
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
    }
}
