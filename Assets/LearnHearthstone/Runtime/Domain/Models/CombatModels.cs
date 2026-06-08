using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class CombatLogEntry
    {
        public int Seq;
        public string Title;
        public string Detail;
        public string ActorId;
        public string TargetId;
        public LogSeverity Severity;
    }

    [Serializable]
    public sealed class CombatOutput
    {
        public CombatWinner Winner;
        public List<MinionInstance> FinalPlayerBoard = new List<MinionInstance>();
        public List<MinionInstance> FinalOpponentBoard = new List<MinionInstance>();
        public List<CombatLogEntry> Log = new List<CombatLogEntry>();
        public List<CombatReward> PlayerRewards = new List<CombatReward>();
        public List<CombatReward> OpponentRewards = new List<CombatReward>();
        public int Steps;
        public bool SafetyStopped;
    }

    [Serializable]
    public sealed class CombatReward
    {
        public CombatRewardType Type;
        public BoardSide Side;
        public string SourceCardId;
        public string CardId;
        public int Amount;
        public int Attack;
        public int Health;
    }

    public enum CombatRewardType
    {
        TavernSpellCostReduction,
        AddGeneratedSpellToHand,
        EternalKnightDied,
        FriendlyMinionDied,
        BuffHandMinion
    }
}
