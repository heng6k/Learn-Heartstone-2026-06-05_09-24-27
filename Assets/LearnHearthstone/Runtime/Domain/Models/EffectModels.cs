using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum EffectTargetType
    {
        Source,
        FriendlyBoard,
        FriendlyHand,
        FriendlyShop,
        RandomFriendlyBoard,
        RandomEnemyBoard,
        AdjacentFriendlyBoard,
        AllFriendlyBoard,
        AllFriendlyHand,
        AllFriendlyShop
    }

    [Serializable]
    public sealed class EffectTrigger
    {
        public MechanicEventType EventType;
    }

    [Serializable]
    public sealed class EffectTarget
    {
        public EffectTargetType Type;
        public Tribe Tribe = Tribe.All;
        public int Count = 1;
    }

    [Serializable]
    public sealed class TargetedMechanicAction
    {
        public EffectTarget Target = new EffectTarget { Type = EffectTargetType.Source };
        public MechanicAction Action = new MechanicAction();
    }

    [Serializable]
    public sealed class MinionEffectDefinition
    {
        public string Id;
        public List<EffectTrigger> Triggers = new List<EffectTrigger>();
        public List<TargetedMechanicAction> Actions = new List<TargetedMechanicAction>();
    }
}
