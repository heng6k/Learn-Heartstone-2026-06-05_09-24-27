using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum RecruitActionTargetSpec
    {
        None,
        FriendlyBoardMinion,
        OtherFriendlyBoardMinion,
        TavernMinion
    }

    [Serializable]
    public sealed class RecruitActionCostSpec
    {
        public int Gold;

        public RecruitActionCostSpec Clone()
        {
            return (RecruitActionCostSpec)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class RecruitActionDefinition
    {
        public string ActionId;
        public string ResolverId;
        public RecruitActionCostSpec CostSpec = new RecruitActionCostSpec();
        public RecruitActionTargetSpec TargetSpec;
        public int UsesPerTurn = 1;
        public MatchPhase AllowedPhase = MatchPhase.Tavern;

        public RecruitActionDefinition Clone()
        {
            var clone = (RecruitActionDefinition)MemberwiseClone();
            clone.CostSpec = CostSpec?.Clone() ?? new RecruitActionCostSpec();
            return clone;
        }
    }

    [Serializable]
    public sealed class RecruitActionState
    {
        public string SourceInstanceId;
        public string ActionId;
        public int UsesThisTurn;
        public int LastUsedRound;
        public int Cooldown;
        public string LockedReason;

        public RecruitActionState Clone()
        {
            return (RecruitActionState)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class RecruitActionRequest
    {
        public string ActionId;
        public string SourceInstanceId;
        public int TargetIndex = -1;
        public TargetZone TargetZone = TargetZone.Unspecified;
        public string TargetInstanceId;
        public int SecondaryTargetIndex = -1;
        public TargetZone SecondaryTargetZone = TargetZone.Unspecified;
        public string SecondaryTargetInstanceId;
        public string ChoiceId;

        public RecruitActionRequest Clone()
        {
            return (RecruitActionRequest)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class RecruitActionResult
    {
        public bool Succeeded;
        public string Code;
        public string Message;
        public int GoldBefore;
        public int GoldAfter;
        public int GoldSpent;
        public int UsesThisTurn;
        public List<string> Events = new List<string>();
    }
}
