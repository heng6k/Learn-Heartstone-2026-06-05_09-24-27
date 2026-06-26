using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class QuestImplementation
    {
        public string CardId;
        public string Name;
        public QuestImplementationStatus Status;
        public string Note;
    }

    public static class QuestImplementationRegistry
    {
        public static List<QuestImplementation> Quests(QuestCatalog catalog)
        {
            return catalog == null
                ? new List<QuestImplementation>()
                : catalog.Quests.Select(FromQuest).ToList();
        }

        public static List<QuestImplementation> Rewards(QuestCatalog catalog)
        {
            return catalog == null
                ? new List<QuestImplementation>()
                : catalog.Rewards.Select(FromReward).ToList();
        }

        private static QuestImplementation FromQuest(QuestDefinition definition)
        {
            return new QuestImplementation
            {
                CardId = definition.CardId,
                Name = definition.Name,
                Status = definition.ImplementationStatus,
                Note = string.IsNullOrWhiteSpace(definition.Notes)
                    ? "No Quest implementation note has been registered."
                    : definition.Notes
            };
        }

        private static QuestImplementation FromReward(QuestRewardDefinition definition)
        {
            return new QuestImplementation
            {
                CardId = definition.CardId,
                Name = definition.Name,
                Status = definition.ImplementationStatus,
                Note = string.IsNullOrWhiteSpace(definition.Notes)
                    ? "No Quest Reward implementation note has been registered."
                    : definition.Notes
            };
        }
    }
}
