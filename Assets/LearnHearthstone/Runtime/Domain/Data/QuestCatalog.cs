using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class QuestCatalog
    {
        private readonly Dictionary<string, QuestDefinition> questsById;
        private readonly Dictionary<string, QuestDefinition> questsByCardId;
        private readonly Dictionary<string, QuestRewardDefinition> rewardsById;
        private readonly Dictionary<string, QuestRewardDefinition> rewardsByCardId;

        public QuestCatalog(IEnumerable<QuestDefinition> quests, IEnumerable<QuestRewardDefinition> rewards)
        {
            Quests = (quests ?? Enumerable.Empty<QuestDefinition>()).ToList();
            Rewards = (rewards ?? Enumerable.Empty<QuestRewardDefinition>()).ToList();
            questsById = Quests
                .Where(quest => !string.IsNullOrEmpty(quest.Id))
                .GroupBy(quest => quest.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            questsByCardId = Quests
                .Where(quest => !string.IsNullOrEmpty(quest.CardId))
                .GroupBy(quest => quest.CardId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            rewardsById = Rewards
                .Where(reward => !string.IsNullOrEmpty(reward.Id))
                .GroupBy(reward => reward.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            rewardsByCardId = Rewards
                .Where(reward => !string.IsNullOrEmpty(reward.CardId))
                .GroupBy(reward => reward.CardId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        public List<QuestDefinition> Quests { get; }

        public List<QuestRewardDefinition> Rewards { get; }

        public List<QuestDefinition> ImplementedQuests =>
            Quests.Where(quest =>
                quest.ImplementationStatus == QuestImplementationStatus.Implemented &&
                !IsDeletedQuest(quest)).ToList();

        public List<QuestRewardDefinition> ImplementedRewards =>
            Rewards.Where(reward => reward.ImplementationStatus == QuestImplementationStatus.Implemented).ToList();

        public List<QuestRewardDefinition> OfferableRewards =>
            Rewards.Where(reward =>
                reward.ImplementationStatus == QuestImplementationStatus.Implemented &&
                reward.OfferPoolStatus == QuestOfferPoolStatus.Offerable).ToList();

        public List<QuestRewardDefinition> HiddenEffectRewards =>
            Rewards.Where(reward =>
                reward.ImplementationStatus == QuestImplementationStatus.Implemented &&
                reward.OfferPoolStatus == QuestOfferPoolStatus.HiddenEffectOnly).ToList();

        public QuestDefinition GetQuestById(string id)
        {
            if (!TryGetQuestById(id, out var definition))
            {
                throw new InvalidOperationException("Quest id does not exist: " + id);
            }

            return definition;
        }

        public QuestDefinition GetQuestByCardId(string cardId)
        {
            if (!TryGetQuestByCardId(cardId, out var definition))
            {
                throw new InvalidOperationException("Quest card id does not exist: " + cardId);
            }

            return definition;
        }

        public QuestRewardDefinition GetRewardById(string id)
        {
            if (!TryGetRewardById(id, out var definition))
            {
                throw new InvalidOperationException("Quest reward id does not exist: " + id);
            }

            return definition;
        }

        public QuestRewardDefinition GetRewardByCardId(string cardId)
        {
            if (!TryGetRewardByCardId(cardId, out var definition))
            {
                throw new InvalidOperationException("Quest reward card id does not exist: " + cardId);
            }

            return definition;
        }

        public bool TryGetQuestById(string id, out QuestDefinition definition)
        {
            return questsById.TryGetValue(id ?? string.Empty, out definition);
        }

        public bool TryGetQuestByCardId(string cardId, out QuestDefinition definition)
        {
            return questsByCardId.TryGetValue(cardId ?? string.Empty, out definition);
        }

        public bool TryGetRewardById(string id, out QuestRewardDefinition definition)
        {
            return rewardsById.TryGetValue(id ?? string.Empty, out definition);
        }

        public bool TryGetRewardByCardId(string cardId, out QuestRewardDefinition definition)
        {
            return rewardsByCardId.TryGetValue(cardId ?? string.Empty, out definition);
        }

        private static bool IsDeletedQuest(QuestDefinition quest)
        {
            return quest?.Tags != null &&
                   quest.Tags.Any(tag =>
                       string.Equals(tag, "deleted", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(tag, "history_only", StringComparison.OrdinalIgnoreCase));
        }
    }
}
