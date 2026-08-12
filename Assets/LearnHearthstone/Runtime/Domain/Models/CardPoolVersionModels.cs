using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum CardPoolPresetValidationState
    {
        Unknown,
        Valid,
        HasIncompatibleEntries
    }

    [Serializable]
    public sealed class CardPoolVersionProfile
    {
        public string Id;
        public string Name;
        public string BaseGameVersionId;
        public string CreatedAgainstContentFingerprint;
        public CardPoolPresetValidationState ValidationState;
        public long CreatedAtUnixSeconds;
        public long UpdatedAtUnixSeconds;
        public List<string> IncompatibleEntityIds = new List<string>();
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
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion = CurrentSchemaVersion;
        public string SelectedVersionId;
        public List<CardPoolVersionProfile> Versions = new List<CardPoolVersionProfile>();

        public string SelectedPresetId
        {
            get => SelectedVersionId;
            set => SelectedVersionId = value;
        }
    }

    public sealed class CardPoolPresetProfile
    {
        internal CardPoolPresetProfile(CardPoolVersionProfile legacyProfile)
        {
            LegacyProfile = legacyProfile ?? throw new ArgumentNullException(nameof(legacyProfile));
        }

        internal CardPoolVersionProfile LegacyProfile { get; }

        public string Id
        {
            get => LegacyProfile.Id;
            set => LegacyProfile.Id = value;
        }

        public string Name
        {
            get => LegacyProfile.Name;
            set => LegacyProfile.Name = value;
        }

        public string BaseGameVersionId
        {
            get => LegacyProfile.BaseGameVersionId;
            set => LegacyProfile.BaseGameVersionId = value;
        }

        public string CreatedAgainstContentFingerprint
        {
            get => LegacyProfile.CreatedAgainstContentFingerprint;
            set => LegacyProfile.CreatedAgainstContentFingerprint = value;
        }

        public CardPoolPresetValidationState ValidationState
        {
            get => LegacyProfile.ValidationState;
            set => LegacyProfile.ValidationState = value;
        }

        public long CreatedAtUnixSeconds
        {
            get => LegacyProfile.CreatedAtUnixSeconds;
            set => LegacyProfile.CreatedAtUnixSeconds = value;
        }

        public long UpdatedAtUnixSeconds
        {
            get => LegacyProfile.UpdatedAtUnixSeconds;
            set => LegacyProfile.UpdatedAtUnixSeconds = value;
        }

        public List<string> IncompatibleEntityIds => LegacyProfile.IncompatibleEntityIds;
        public List<string> EnabledMinionCardIds => LegacyProfile.EnabledMinionCardIds;
        public List<string> EnabledTavernSpellCardNumbers => LegacyProfile.EnabledTavernSpellCardNumbers;
        public List<string> EnabledQuestCardIds => LegacyProfile.EnabledQuestCardIds;
        public List<string> EnabledQuestRewardCardIds => LegacyProfile.EnabledQuestRewardCardIds;
        public List<string> EnabledLesserTrinketCardIds => LegacyProfile.EnabledLesserTrinketCardIds;
        public List<string> EnabledGreaterTrinketCardIds => LegacyProfile.EnabledGreaterTrinketCardIds;
        public List<string> EnabledAnomalyCardIds => LegacyProfile.EnabledAnomalyCardIds;
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
