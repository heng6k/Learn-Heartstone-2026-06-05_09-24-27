using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class StrategyGuideShareCardModel
    {
        public string GuideId;
        public string ProfileId;
        public string RevisionId;
        public string GameVersionId;
        public string ContentSnapshotId;
        public string Title;
        public string Summary;
        public string Archetype;
        public string Difficulty;
        public string DifficultyTitle;
        public string LearningGoal;
        public int StartRound;
        public int TavernTier;
        public int Gold;
        public int MaxGold;
        public bool AllowsUndo;
        public string PublicCode;
        public string ContentHash;
        public string ContentHashShort;
        public StrategyGuideShareCardAsset Hero;
        public StrategyGuideShareCardAsset LesserTrinket;
        public StrategyGuideShareCardAsset GreaterTrinket;
        public List<StrategyGuideShareCardAsset> RecommendedLesserTrinkets = new List<StrategyGuideShareCardAsset>();
        public List<StrategyGuideShareCardAsset> RecommendedGreaterTrinkets = new List<StrategyGuideShareCardAsset>();
        public List<string> ActiveTribes = new List<string>();
        public List<StrategyGuideShareCardAsset> FinalComposition = new List<StrategyGuideShareCardAsset>();
        public List<StrategyGuideShareCardAsset> CoreCards = new List<StrategyGuideShareCardAsset>();
        public List<StrategyGuideShareCardAsset> DarkGifts = new List<StrategyGuideShareCardAsset>();
        public List<StrategyGuideShareCardEntry> Entries = new List<StrategyGuideShareCardEntry>();
        public List<StrategyGuideShareCardAsset> StartingShop = new List<StrategyGuideShareCardAsset>();
        public List<StrategyGuideShareCardAsset> StartingBoard = new List<StrategyGuideShareCardAsset>();
        public List<StrategyGuideShareCardAsset> StartingHand = new List<StrategyGuideShareCardAsset>();
        public List<string> KeyDecisions = new List<string>();
        public List<StrategyGuideShareCardShapingTurn> ShapingTurns = new List<StrategyGuideShareCardShapingTurn>();
        public List<StrategyGuideShareCardGrowthTarget> GrowthTargets = new List<StrategyGuideShareCardGrowthTarget>();
        public string CompletionCondition;
        public bool HasControlledOffers;
        public string ProbabilityNotice;
        public string Disclaimer;
    }

    [Serializable]
    public sealed class StrategyGuideShareCardAsset
    {
        public string StableId;
        public CardKind CardKind;
        public string Name;
        public string ImagePath;
        public bool Golden;
        public string Badge;
        public int Attack;
        public int Health;
        public int TavernTier;
        public int Cost;
    }

    [Serializable]
    public sealed class StrategyGuideShareCardEntry
    {
        public string ProfileId;
        public string Difficulty;
        public string Title;
        public string StrategyLabel;
        public bool AllowsUndo;
    }

    [Serializable]
    public sealed class StrategyGuideShareCardShapingTurn
    {
        public int LocalTurn;
        public StrategyGuideShareCardAsset Spell;
    }

    [Serializable]
    public sealed class StrategyGuideShareCardGrowthTarget
    {
        public string Key;
        public string Label;
        public int MinimumValue;
    }
}
