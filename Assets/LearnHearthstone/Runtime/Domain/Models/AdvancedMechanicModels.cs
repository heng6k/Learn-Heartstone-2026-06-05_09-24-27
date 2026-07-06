using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum AdvancedMechanicKind
    {
        Trinket,
        Quest,
        Anomaly,
        Timewarp,
        Distortion
    }

    public enum AdvancedMechanicMode
    {
        None,
        Trinkets,
        Quests,
        Anomalies,
        Timewarp,
        Distortion,
        Mixed
    }

    public enum AdvancedMechanicTrigger
    {
        MatchStarted,
        TurnStarted,
        TurnEnded,
        ShopRefreshed,
        CardBought,
        OptionChosen,
        Equipped,
        StartOfCombat
    }

    public enum PlayerDirectedChoiceKind
    {
        QuestPair,
        Trinket,
        SecondHeroPower
    }

    [Serializable]
    public sealed class PlayerDirectedChoiceContext
    {
        public PlayerDirectedChoiceKind Kind;
        public string Source;
        public string Slot;
        public int Round;
        public IReadOnlyList<Tribe> ActiveTribes;
        public bool IncludeDebugOnly;
        public bool IncludeHiddenEffectOnly;
        public bool IncludeDisabled;
    }

    [Serializable]
    public sealed class PlayerDirectedChoiceOption
    {
        public PlayerDirectedChoiceKind Kind;
        public string CardId;
        public string SecondaryCardId;
        public string DisplayName;
        public string SecondaryDisplayName;
        public string Text;
        public string ImagePath;
        public string Type;
        public string Status;
        public string Slot;
        public string PowerLevel;
        public string Timing;
        public string DisabledReason;
        public bool IsSelectable;
        public int Cost;
        public List<string> FilterTags = new List<string>();
    }

    [Serializable]
    public sealed class MechanicChoiceOption
    {
        public string OptionId;
        public AdvancedMechanicKind Kind;
        public string SourceId;
        public string DisplayName;
        public string Text;
        public string ImagePath;
        public string RewardId;
        public string RewardName;
        public string RewardText;
        public string RewardImagePath;
        public int RequiredAmount;
        public int DifficultyTier;
        public string RewardPowerLevel;
        public int Cost;
        public string Slot;
        public string ImplementationStatus;
        public List<string> Tags = new List<string>();
    }

    [Serializable]
    public sealed class MechanicChoiceRequest
    {
        public string RequestId;
        public AdvancedMechanicKind Kind;
        public string Source;
        public string Slot;
        public int Round;
        public int RemainingPicks = 1;
        public List<MechanicChoiceOption> Options = new List<MechanicChoiceOption>();
    }

    [Serializable]
    public sealed class EquippedAdvancedMechanic
    {
        public AdvancedMechanicKind Kind;
        public string SourceId;
        public string DisplayName;
        public string Slot;
        public int EquippedRound;
        public int CostPaid;
        public string ImplementationStatus;
    }

    [Serializable]
    public sealed class AdvancedMechanicState
    {
        public MechanicChoiceRequest PendingChoice;
        public List<EquippedAdvancedMechanic> Equipped = new List<EquippedAdvancedMechanic>();
        public Dictionary<string, int> Counters = new Dictionary<string, int>();
        public Dictionary<string, string> Selections = new Dictionary<string, string>();
        public PlayerTrinketState Trinkets = new PlayerTrinketState();
        public PlayerQuestState Quests = new PlayerQuestState();
        public AnomalyState Anomalies = new AnomalyState();
    }
}
