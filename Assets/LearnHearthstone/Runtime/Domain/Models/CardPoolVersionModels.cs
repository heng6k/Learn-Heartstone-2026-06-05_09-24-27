using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class CardPoolVersionProfile
    {
        public string Id;
        public string Name;
        public long CreatedAtUnixSeconds;
        public long UpdatedAtUnixSeconds;
        public List<string> EnabledMinionCardIds = new List<string>();
        public List<string> EnabledTavernSpellCardNumbers = new List<string>();
        public List<string> EnabledQuestCardIds = new List<string>();
        public List<string> EnabledQuestRewardCardIds = new List<string>();
        public List<string> EnabledLesserTrinketCardIds = new List<string>();
        public List<string> EnabledGreaterTrinketCardIds = new List<string>();
        public List<string> EnabledAnomalyCardIds = new List<string>();
    }

    [Serializable]
    public sealed class CardPoolVersionStore
    {
        public string SelectedVersionId;
        public List<CardPoolVersionProfile> Versions = new List<CardPoolVersionProfile>();
    }

    public sealed class CardPoolVersionSelection
    {
        public string VersionId;
        public string VersionName;
        public bool IsDefault;
        public HashSet<string> EnabledMinionCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> EnabledTavernSpellCardNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> EnabledQuestCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> EnabledQuestRewardCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> EnabledLesserTrinketCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> EnabledGreaterTrinketCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> EnabledAnomalyCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
