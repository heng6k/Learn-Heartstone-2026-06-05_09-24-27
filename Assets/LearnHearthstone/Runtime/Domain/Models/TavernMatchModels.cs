using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class SearchTarget
    {
        public string DefinitionId;
        public string Priority = "MEDIUM";
        public int DesiredCopies = 1;
    }

    [Serializable]
    public sealed class SearchPlanState
    {
        public List<SearchTarget> Targets = new List<SearchTarget>();
        public int GoldSpentOnRerollThisTurn;
        public List<string> HitsThisTurn = new List<string>();
    }

    [Serializable]
    public sealed class RecruitLogEntry
    {
        public int Seq;
        public int Round;
        public RecruitLogType Type;
        public string Message;
        public int GoldBefore;
        public int GoldAfter;
    }

    [Serializable]
    public sealed class DiscoverState
    {
        public string Source = "TRIPLE";
        public int RewardTier;
        public List<MinionInstance> Options = new List<MinionInstance>();
    }

    [Serializable]
    public sealed class TavernState
    {
        public int Tier;
        public int Gold;
        public int MaxGold;
        public int UpgradeCost;
        public bool Frozen;
        public List<MinionInstance> Shop = new List<MinionInstance>();
        public List<MinionInstance> Hand = new List<MinionInstance>();
        public Dictionary<string, int> Pool = new Dictionary<string, int>();
        public DiscoverState Discover;
        public SearchPlanState SearchPlan = new SearchPlanState();
        public List<RecruitLogEntry> RecruitLog = new List<RecruitLogEntry>();
    }

    [Serializable]
    public sealed class SearchHint
    {
        public SearchHintType Type;
        public string Message;
        public SearchHintSeverity Severity;
    }

    [Serializable]
    public sealed class LocalPlayerState
    {
        public string HeroId;
        public int Health;
        public int Armor;
        public TavernState Tavern = new TavernState();
        public List<MinionInstance> Board = new List<MinionInstance>();
    }

    [Serializable]
    public sealed class LocalOpponentState
    {
        public string Name;
        public string HeroId;
        public int Health;
        public int Armor;
        public int TavernTier;
        public List<MinionInstance> Board = new List<MinionInstance>();
        public bool Editable;
    }

    [Serializable]
    public sealed class MatchState
    {
        public MatchMode Mode;
        public MatchPhase Phase;
        public int Round;
        public int Seed;
        public LocalPlayerState Player = new LocalPlayerState();
        public LocalOpponentState Opponent = new LocalOpponentState();
        public List<SearchHint> RecruitHints = new List<SearchHint>();
        public List<CombatLogEntry> CombatLog = new List<CombatLogEntry>();
        public CombatOutput LastResult;
    }
}
