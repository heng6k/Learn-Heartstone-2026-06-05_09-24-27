using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class TestScenarioDefinition
    {
        public int SchemaVersion = 3;
        public string Version = "battle-test-loop-v3";
        public bool IsStateTemplate = true;
        public int MechanicStateSchemaVersion = 2;
        public string Name;
        public int SavedAtRound;
        public int Seed;
        public MatchPhase Phase;
        public int PendingTurnStartRound;
        public bool PendingTurnResolvedCombat;
        public string PendingTurnEndTransitionId;
        public int PendingTurnEndOccurrenceCount;
        public int TurnEndTransitionSequence;
        public string GameVersionId;
        public string RulesetId;
        public int RulesetRevision;
        public string ContentSnapshotId;
        public string ContentFingerprint;
        public string CardPoolPresetId;
        public string CardPoolPresetName;
        public bool IsDefaultCardPoolPreset = true;
        public ScenarioResolvedCardPoolState ResolvedCardPool = new ScenarioResolvedCardPoolState();
        public PlayerScenarioState Player = new PlayerScenarioState();
        public OpponentScenarioState Opponent = new OpponentScenarioState();
        public ScenarioTavernState Tavern = new ScenarioTavernState();
        public ScenarioAdvancedMechanicState PlayerAdvancedMechanics = new ScenarioAdvancedMechanicState();
        public ScenarioAdvancedMechanicState OpponentAdvancedMechanics = new ScenarioAdvancedMechanicState();
        public ScenarioPlayerDarkGiftState PlayerDarkGiftState = new ScenarioPlayerDarkGiftState();
        public ScenarioChoiceQueueState ChoiceQueueState = new ScenarioChoiceQueueState();
        public List<ScenarioRecruitActionState> RecruitActionStates = new List<ScenarioRecruitActionState>();
        public List<ScenarioDelayedObjectState> DelayedObjectStates = new List<ScenarioDelayedObjectState>();
        public List<ScenarioMechanicEventRecord> MechanicEvents = new List<ScenarioMechanicEventRecord>();
        public ScenarioDeterministicRngState RngState = new ScenarioDeterministicRngState();
        public List<RecruitLogEntry> RecruitLog = new List<RecruitLogEntry>();
        public bool PlayerCombatModifiersAreAuthoritative;
        public SideCombatModifierState PlayerCombatModifiers = new SideCombatModifierState();
        public SideCombatModifierState OpponentCombatModifiers = new SideCombatModifierState();
        public List<ScenarioCardState> Shop = new List<ScenarioCardState>();
        public List<ScenarioCardState> Hand = new List<ScenarioCardState>();
        public List<ScenarioCardState> OpponentHand = new List<ScenarioCardState>();
        public List<ScenarioCardState> PlayerBoard = new List<ScenarioCardState>();
        public List<ScenarioCardState> OpponentBoard = new List<ScenarioCardState>();
    }

    [Serializable]
    public sealed class PlayerScenarioState
    {
        public string HeroId;
        public int Health;
        public int Armor;
    }

    [Serializable]
    public sealed class OpponentScenarioState
    {
        public string Name;
        public string HeroId;
        public int Health;
        public int Armor;
        public int TavernTier;
        public bool Editable;
    }

    [Serializable]
    public sealed class ScenarioTavernState
    {
        public int Tier;
        public int Gold;
        public int MaxGold;
        public int UpgradeCost;
        public bool Frozen;
        public int NextTurnBonusGold;
        public int NextTavernSpellCostReduction;
        public int FreeRefreshes;
        public int DemonFodderRefreshes;
        public int TavernSpellBonusAttack;
        public int TavernSpellBonusHealth;
        public string GuideShapingSpellCardId;
        public List<string> GuideShapingSpellCardIds = new List<string>();
        public List<string> GuideCoreSpellCardNumbers = new List<string>();
        public int GuideShapingSpellRound;
        public bool GuideShapingSpellConsumed;
        public int BeetleAttackBonus = 2;
        public int BeetleHealthBonus = 2;
        public int FutureBallerAttackBonus;
        public int FutureBallerHealthBonus;
        public int UndeadAttackBonus;
        public int EternalKnightDeaths;
        public int AncestralAutomatonSummons;
        public int FriendlyMinionDeathsThisGame;
    }

    [Serializable]
    public sealed class ScenarioResolvedCardPoolState
    {
        public bool IsComplete;
        public List<Tribe> ActiveTribes = new List<Tribe>();
        public bool TimewarpedTavernEnabled = true;
        public bool UseHistoricalTimewarpedPool;
        public TimewarpedPoolVersion TimewarpedPoolVersion = TimewarpedPoolVersion.Current;
        public bool UseExplicitTimewarpedPool;
        public List<string> EnabledTimewarpedCardIds = new List<string>();
        public List<string> EnabledMinionCardIds = new List<string>();
        public List<string> EnabledTavernSpellCardNumbers = new List<string>();
        public List<string> EnabledQuestCardIds = new List<string>();
        public List<string> EnabledQuestRewardCardIds = new List<string>();
        public List<string> EnabledLesserTrinketCardIds = new List<string>();
        public List<string> EnabledGreaterTrinketCardIds = new List<string>();
        public List<string> EnabledAnomalyCardIds = new List<string>();
    }

    [Serializable]
    public sealed class ScenarioAdvancedMechanicState
    {
        public AdvancedMechanicState State = new AdvancedMechanicState();
        public List<ScenarioCounterState> Counters = new List<ScenarioCounterState>();
        public List<ScenarioStringState> Selections = new List<ScenarioStringState>();
        public List<ScenarioCounterState> QuestRewardCounters = new List<ScenarioCounterState>();
        public List<ScenarioBoolState> QuestRewardFlags = new List<ScenarioBoolState>();
        public List<ScenarioCounterState> AnomalyCounters = new List<ScenarioCounterState>();
        public List<ScenarioStringState> AnomalyFlags = new List<ScenarioStringState>();
    }

    [Serializable]
    public sealed class ScenarioPlayerDarkGiftState
    {
        public List<ScenarioDarkGiftInstanceState> AcquiredGiftInstances = new List<ScenarioDarkGiftInstanceState>();
        public List<ScenarioCounterState> Counters = new List<ScenarioCounterState>();
        public List<ScenarioCounterState> Cooldowns = new List<ScenarioCounterState>();
        public List<ScenarioMechanicEventRecord> TriggerHistory = new List<ScenarioMechanicEventRecord>();
    }

    [Serializable]
    public sealed class ScenarioDarkGiftInstanceState
    {
        public string InstanceId;
        public string DefinitionRevisionId;
        public int AcquiredRound;
        public string Source;
        public int StackCount;
        public int RemainingUses;
        public int Cooldown;
        public bool Active;
        public bool Suppressed;
        public bool Expired;
    }

    [Serializable]
    public sealed class ScenarioChoiceQueueState
    {
        public bool HasActiveChoice;
        public ScenarioChoiceQueueItem ActiveChoice;
        public List<ScenarioChoiceQueueItem> PendingChoices = new List<ScenarioChoiceQueueItem>();
        public List<string> CompletedRequestIds = new List<string>();
        public int NextSequence = 1;
    }

    [Serializable]
    public sealed class ScenarioChoiceQueueItem
    {
        public string RequestId;
        public string Kind;
        public string Source;
        public int CreatedRound;
        public int Sequence;
        public int Priority;
        public bool Blocking;
        public int RemainingPicks;
        public List<MechanicChoiceOption> Options = new List<MechanicChoiceOption>();
        public List<ScenarioStringState> ResolutionMetadata = new List<ScenarioStringState>();
        public ScenarioDiscoverState Discover;
    }

    [Serializable]
    public sealed class ScenarioDiscoverState
    {
        public string Source;
        public int RewardTier;
        public string TargetInstanceId;
        public int RemainingPicks;
        public bool AutoResolveRandomly;
        public int AutoResolveSeed;
        public bool ResolveAllOptions;
        public List<string> OptionTags = new List<string>();
        public List<ScenarioCounterState> OptionCounters = new List<ScenarioCounterState>();
        public List<ScenarioCardState> Options = new List<ScenarioCardState>();
    }

    [Serializable]
    public sealed class ScenarioRecruitActionState
    {
        public string SourceInstanceId;
        public string ActionId;
        public int UsesThisTurn;
        public int LastUsedRound;
        public int Cooldown;
        public string LockedReason;
    }

    [Serializable]
    public sealed class ScenarioDelayedObjectState
    {
        public string InstanceId;
        public string DefinitionRevisionId;
        public int CreatedRound;
        public int RemainingTurns;
        public string OpenResolverId;
        public string Source;
        public bool Opened;
    }

    [Serializable]
    public sealed class ScenarioMechanicEventRecord
    {
        public int Sequence;
        public int Round;
        public MatchPhase Phase;
        public string Type;
        public string Source;
        public List<string> Targets = new List<string>();
        public string Result;
        public string RequestId;
    }

    [Serializable]
    public sealed class ScenarioDeterministicRngState
    {
        public string Algorithm;
        public int Seed;
        public int Round;
        public int RecruitLogCursor;
        public int MechanicEventCursor;
    }

    [Serializable]
    public sealed class ScenarioCardState
    {
        public CardKind CardKind;
        public string InstanceId;
        public string DefinitionId;
        public string CardId;
        public string Name;
        public int Cost;
        public int BaseAttack;
        public int BaseHealth;
        public int Attack;
        public int Health;
        public int MaxHealth;
        public int TavernTier;
        public List<Tribe> Tribes = new List<Tribe>();
        public List<Keyword> Keywords = new List<Keyword>();
        public List<Keyword> OfficialKeywords = new List<Keyword>();
        public string Text;
        public bool Golden;
        public BoardSide Owner;
        public List<ScenarioEnchantmentState> Enchantments = new List<ScenarioEnchantmentState>();
        public List<ScenarioCounterState> Counters = new List<ScenarioCounterState>();
        public bool CanAttack;
        public int AttacksThisCombat;
        public PoolSource OriginPoolSource;
        public bool CanReturnToPoolAfterAttach;
        public PoolSource PoolSource;
        public int PoolCopiesHeld;
        public string ImagePath;
        public List<string> EffectIds = new List<string>();
        public List<string> Tags = new List<string>();
    }

    [Serializable]
    public sealed class ScenarioEnchantmentState
    {
        public string Id;
        public string SourceId;
        public int AttackBonus;
        public int HealthBonus;
        public List<Keyword> AddedKeywords = new List<Keyword>();
        public string Duration = "PERMANENT";
    }

    [Serializable]
    public sealed class ScenarioCounterState
    {
        public string Key;
        public int Value;
    }

    [Serializable]
    public sealed class ScenarioStringState
    {
        public string Key;
        public string Value;
    }

    [Serializable]
    public sealed class ScenarioBoolState
    {
        public string Key;
        public bool Value;
    }

    public enum TestScenarioRestoreStatus
    {
        Applied,
        MissingContentSnapshot,
        MissingCardPoolSnapshot,
        ContentSnapshotMismatch,
        InvalidRngState,
        InvalidScenario
    }

    public sealed class TestScenarioRestoreResult
    {
        public TestScenarioRestoreStatus Status;
        public string Message;

        public bool IsApplied => Status == TestScenarioRestoreStatus.Applied;
    }

    public sealed class TestScenarioSummary
    {
        public string Name;
        public int SavedAtRound;
        public string GameVersionId;
        public string ContentSnapshotId;
        public TestScenarioRestoreStatus RestoreStatus;
        public string RestoreMessage;

        public bool CanLoad => RestoreStatus == TestScenarioRestoreStatus.Applied;
    }

    [Serializable]
    public sealed class CombatTestOptions
    {
        public int Seed;
        public bool ResetBeforeRun;
        public int SafetyLimit = 200;
        public bool ApplyHeroDamage;
        public HeroDamageCapPolicy DamageCapPolicy = HeroDamageCapPolicy.TrainingRound12Approximation;
        public bool IsTopFour;
    }

    [Serializable]
    public sealed class CombatTestSnapshot
    {
        public TestScenarioDefinition BeforeCombat;
        public CombatTestOptions Options;
        public CombatOutput Result;
    }
}
