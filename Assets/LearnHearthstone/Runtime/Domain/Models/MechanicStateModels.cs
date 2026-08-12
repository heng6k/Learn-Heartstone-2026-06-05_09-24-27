using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class DelayedObjectState
    {
        public string InstanceId;
        public string DefinitionRevisionId;
        public int CreatedRound;
        public int RemainingTurns;
        public string OpenResolverId;
        public string Source;
        public bool Opened;

        public DelayedObjectState Clone()
        {
            return (DelayedObjectState)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class MechanicEventRecord
    {
        public int Sequence;
        public int Round;
        public MatchPhase Phase;
        public string Type;
        public string Source;
        public List<string> Targets = new List<string>();
        public string Result;
        public string RequestId;

        public MechanicEventRecord Clone()
        {
            var clone = (MechanicEventRecord)MemberwiseClone();
            clone.Targets = new List<string>(Targets ?? new List<string>());
            return clone;
        }
    }

    [Serializable]
    public sealed class RecruitPhaseAttackContext
    {
        public string AttackerInstanceId;
        public string TavernTargetInstanceId;
        public string DamageContext;
        public string DeathContext;
        public string RewardSource;
        public int Sequence;

        public RecruitPhaseAttackContext Clone()
        {
            return (RecruitPhaseAttackContext)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class RecruitPhaseAttackResult
    {
        public bool Succeeded;
        public string Code;
        public string Message;
        public string AttackerInstanceId;
        public string TavernTargetInstanceId;
        public int AttackerDamage;
        public int TargetDamage;
        public bool AttackerDied;
        public bool TargetDied;
        public List<CombatReward> Rewards = new List<CombatReward>();
    }
}
