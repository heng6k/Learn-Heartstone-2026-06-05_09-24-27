using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class StrategyGuideCatalogDefinition
    {
        public int SchemaVersion = 2;
        public string CatalogRevisionId;
        public List<StrategyGuideDefinition> Guides = new List<StrategyGuideDefinition>();
        public List<StrategyGuideOpponentDefinition> Opponents = new List<StrategyGuideOpponentDefinition>();
    }

    [Serializable]
    public sealed class StrategyGuideDefinition
    {
        public string GuideId;
        public string RevisionId;
        public string GameVersionId;
        public string Title;
        public string EnglishTitle;
        public string Summary;
        public string EnglishSummary;
        public string Archetype;
        public string HeroCardId;
        public string LesserTrinketCardId;
        public string GreaterTrinketCardId;
        public List<string> RecommendedLesserTrinketCardIds = new List<string>();
        public List<string> RecommendedGreaterTrinketCardIds = new List<string>();
        public List<string> RequiredTribes = new List<string>();
        public List<string> ActiveTribes = new List<string>();
        public List<string> CoreMinionCardIds = new List<string>();
        public List<string> CoreSpellCardNumbers = new List<string>();
        public List<StrategyGuideCardDefinition> FinalComposition = new List<StrategyGuideCardDefinition>();
        public List<StrategyGuideEntryProfileDefinition> EntryProfiles = new List<StrategyGuideEntryProfileDefinition>();
    }

    [Serializable]
    public sealed class StrategyGuideEntryProfileDefinition
    {
        public string ProfileId;
        public string Difficulty;
        public string Title;
        public string EnglishTitle;
        public string LearningGoal;
        public string EnglishLearningGoal;
        public List<string> KeyDecisions = new List<string>();
        public List<string> EnglishKeyDecisions = new List<string>();
        public int StartRound;
        public int TavernTier;
        public int Gold;
        public int MaxGold;
        public int Seed;
        public int InitialTripleRewardCount;
        public List<string> AllowedCommands = new List<string>();
        public List<StrategyGuideCardDefinition> Placements = new List<StrategyGuideCardDefinition>();
        public List<StrategyGuideDarkGiftAttachment> DarkGiftAttachments = new List<StrategyGuideDarkGiftAttachment>();
        public List<string> ShapingSpellCardIds = new List<string>();
        public List<StrategyGuideGrowthValue> GrowthQuality = new List<StrategyGuideGrowthValue>();
        public List<StrategyGuideRequiredAction> RequiredActions = new List<StrategyGuideRequiredAction>();
        public List<string> UnequippedTrinketSlots = new List<string>();
        public StrategyGuideAcquisitionPlanDefinition AcquisitionPlan;
        public StrategyGuideOpponentSelector Opponent = new StrategyGuideOpponentSelector();
        public StrategyGuideVictoryCondition Victory = new StrategyGuideVictoryCondition();
        public StrategyGuideUndoPolicy Undo = new StrategyGuideUndoPolicy();
    }

    [Serializable]
    public sealed class StrategyGuideCardDefinition
    {
        public string PlacementId;
        public string Zone;
        public string CardKind;
        public string CardId;
        public bool Golden;
        public int AttackOverride;
        public int HealthOverride;
        public int MinimumAttack;
        public int MinimumHealth;
        public string Provenance = StrategyGuideProvenance.NormalPool;
    }

    [Serializable]
    public sealed class StrategyGuideDarkGiftAttachment
    {
        public string AttachmentId;
        public string TargetPlacementId;
        public string GiftResearchKey;
        public int AcquiredRound;
        public string Source;
    }

    [Serializable]
    public sealed class StrategyGuideGrowthValue
    {
        public string Key;
        public int Value;
    }

    [Serializable]
    public sealed class StrategyGuideRequiredAction
    {
        public string ActionId;
        public string Kind;
        public string SourcePlacementId;
        public List<string> SourcePlacementIds = new List<string>();
        public string TargetPlacementId;
        public string ChoiceId;
        public int Count = 1;
        public string Instruction;
        public string EnglishInstruction;
    }

    [Serializable]
    public sealed class StrategyGuideOpponentSelector
    {
        public int StrengthRound;
        public string RequiredTag;
    }

    [Serializable]
    public sealed class StrategyGuideVictoryCondition
    {
        public bool RequireFinalComposition = true;
        public bool RequireCombatWin = true;
        public List<string> PostWinChoices = new List<string>();
    }

    [Serializable]
    public sealed class StrategyGuideUndoPolicy
    {
        public int UsesPerRun = 1;
        public bool RestoreRng = true;
        public bool LockAfterTurnEnd = true;
        public bool LockAfterCombat = true;
        public bool LockDuringFreeExplore = true;
    }

    [Serializable]
    public sealed class StrategyGuideAcquisitionPlanDefinition
    {
        public bool DiscloseControlledOffers;
        public List<StrategyGuideOfferScheduleDefinition> OfferSchedules = new List<StrategyGuideOfferScheduleDefinition>();
    }

    [Serializable]
    public sealed class StrategyGuideOfferScheduleDefinition
    {
        public string ScheduleId;
        public string Source;
        public string TriggerCardId;
        public int TriggerTavernTier;
        public int TriggerOccurrence = 1;
        public string Policy = StrategyGuideOfferPolicies.NaturalSeeded;
        public string CardKind = StrategyGuideCardKinds.Minion;
        public int TavernTier;
        public int OptionCount = 3;
        public List<string> TargetCardIds = new List<string>();
        public string RequiredTribe;
        public int MinimumRequiredTribeMinions;
        public string Label;
        public string EnglishLabel;
    }

    [Serializable]
    public sealed class StrategyGuideOpponentDefinition
    {
        public string OpponentId;
        public string RevisionId;
        public string GameVersionId;
        public int StrengthRound;
        public List<string> Tags = new List<string>();
        public string HeroCardId;
        public int TavernTier;
        public List<StrategyGuideCardDefinition> Board = new List<StrategyGuideCardDefinition>();
        public List<StrategyGuideGrowthValue> GrowthQuality = new List<StrategyGuideGrowthValue>();
    }

    public static class StrategyGuideCardKinds
    {
        public const string Minion = "Minion";
        public const string TavernSpell = "TavernSpell";
        public const string Trinket = "Trinket";
    }

    public static class StrategyGuideZones
    {
        public const string Board = "Board";
        public const string Hand = "Hand";
        public const string Shop = "Shop";
    }

    public static class StrategyGuideProvenance
    {
        public const string NormalPool = "NormalPool";
        public const string Generated = "Generated";
        public const string GuideTutorial = "GuideTutorial";
    }

    public static class StrategyGuideGrowthKeys
    {
        public const string BeastLobsterGrowth = "beast.lobsterGrowth";
        public const string TavernSpellsCastThisGame = "tavern.spellsCastThisGame";
        public const string DemonTavernBonusAttack = "demon.tavernBonusAttack";
        public const string DemonTavernBonusHealth = "demon.tavernBonusHealth";
    }

    public static class StrategyGuideShapingSpells
    {
        public const string Deathrattle = "GUIDE_SHAPING_DEATHRATTLE";
        public const string Battlecry = "GUIDE_SHAPING_BATTLECRY";
        public const string EndOfTurn = "GUIDE_SHAPING_END_OF_TURN";

        public static bool Contains(string cardId)
        {
            return string.Equals(cardId, Deathrattle, StringComparison.Ordinal) ||
                string.Equals(cardId, Battlecry, StringComparison.Ordinal) ||
                string.Equals(cardId, EndOfTurn, StringComparison.Ordinal);
        }
    }

    public static class StrategyGuideActionKinds
    {
        public const string Buy = "Buy";
        public const string Play = "Play";
        public const string Sell = "Sell";
        public const string Cast = "Cast";
        public const string Activate = "Activate";
        public const string ChooseTrinket = "ChooseTrinket";
        public const string PlayFinalCards = "PlayFinalCards";
        public const string BoardOrder = "BoardOrder";
    }

    public static class StrategyGuideDifficulties
    {
        public const string Showcase = "Showcase";
        public const string GuidedDiscover = "GuidedDiscover";
        public const string OpenBuild = "OpenBuild";
    }

    public static class StrategyGuideOfferSources
    {
        public const string TripleRewardDiscover = "TripleRewardDiscover";
        public const string ShopRefresh = "ShopRefresh";
        public const string TavernSpellDiscover = "TavernSpellDiscover";
        public const string GreaterTrinketChoice = "GreaterTrinketChoice";
    }

    public static class StrategyGuideOfferPolicies
    {
        public const string NaturalSeeded = "NaturalSeeded";
        public const string MustInclude = "MustInclude";
        public const string MustIncludeAny = "MustIncludeAny";
        public const string Pinned = "Pinned";
    }

    public sealed class StrategyGuideValidationResult
    {
        public List<string> Errors = new List<string>();

        public bool IsValid => Errors.Count == 0;
    }

    public sealed class CompiledStrategyGuide
    {
        public StrategyGuideDefinition Guide;
        public StrategyGuideEntryProfileDefinition Profile;
        public StrategyGuideOpponentDefinition Opponent;
        public TestScenarioDefinition Scenario;
    }
}
