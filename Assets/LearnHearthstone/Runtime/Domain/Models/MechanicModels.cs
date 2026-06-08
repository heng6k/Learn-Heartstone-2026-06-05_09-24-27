using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum MechanicEventType
    {
        TurnStarted,
        ShopRefreshed,
        CardBought,
        CardPlayed,
        BattlecryRequested,
        MinionSold,
        TavernSpellBought,
        TavernSpellCast,
        DiscoverStarted,
        TurnEnded,
        CombatStarted,
        BeforeAttack,
        AfterAttack,
        DamageDealt,
        DivineShieldPopped,
        MinionDied,
        DeathrattleQueued,
        DeathrattleResolved,
        RebornResolved,
        AvengeCounterChanged,
        MinionSummoned,
        CombatEnded
    }

    public enum BuffScope
    {
        Instance,
        Board,
        Hand,
        ShopCurrent,
        ShopGlobal,
        FutureShopTyped,
        GeneratedCard,
        CombatOnly
    }

    public enum MechanicActionType
    {
        BuffStats,
        SetStats,
        AddKeyword,
        RemoveKeyword,
        ModifyShopGrowth,
        ModifyGeneratedCardBuff,
        GainGold,
        SummonToken
    }

    public enum MechanicAuraKind
    {
        StatAura,
        EffectAura
    }

    [Serializable]
    public sealed class MechanicAction
    {
        public MechanicActionType Type;
        public BuffScope Scope = BuffScope.Instance;
        public int Attack;
        public int Health;
        public Keyword Keyword;
        public Tribe Tribe = Tribe.None;
        public string SourceId;
        public int Gold;
        public string TokenDefinitionId;
    }

    [Serializable]
    public sealed class TavernGrowthModifier
    {
        public BuffScope Scope;
        public Tribe Tribe = Tribe.None;
        public int Attack;
        public int Health;
        public string SourceId;
    }

    [Serializable]
    public sealed class GeneratedCardBuffState
    {
        public string CardId;
        public int AttackBonus;
        public int HealthBonus;
    }
}
