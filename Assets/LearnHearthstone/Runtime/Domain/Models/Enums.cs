namespace LearnHearthstone.Domain.Models
{
    public enum Keyword
    {
        Taunt,
        DivineShield,
        Poisonous,
        Venomous,
        Reborn,
        Deathrattle,
        Battlecry,
        Windfury,
        Cleave,
        Magnetic,
        Avenge,
        StartOfCombat,
        EndOfTurn,
        Rally,
        Spellcraft,
        Trigger,
        BloodGem,
        Discover,
        Refresh,
        Pass,
        Aura,
        Devour,
        TavernSpell,
        ChooseOne,
        HiddenDeathrattle,
        Stealth,
        Bounty
    }

    public enum EnchantmentKind
    {
        Unspecified,
        StatBuff,
        BloodGem,
        SetStats
    }

    public enum Tribe
    {
        Beast,
        Murloc,
        Mech,
        Demon,
        Dragon,
        Pirate,
        Elemental,
        Quilboar,
        Undead,
        Naga,
        All,
        None
    }

    public enum BoardSide
    {
        Player,
        Opponent
    }

    public enum TargetZone
    {
        Unspecified,
        FriendlyBoard,
        TavernShop,
        OpponentBoard,
        Hand
    }

    public enum PoolSource
    {
        Pool,
        Copy,
        Discover,
        Summon,
        Debug,
        Timewarped,
        Buddy
    }

    public enum CardKind
    {
        Minion,
        TavernSpell,
        Spell,
        Hero,
        HeroPower,
        HeroBuddy,
        Trinket,
        Quest,
        QuestReward
    }

    public enum MatchMode
    {
        TavernPractice,
        CombatSandbox,
        Scenario
    }

    public enum MatchPhase
    {
        Editing,
        Tavern,
        Combat,
        Result
    }

    public enum CombatWinner
    {
        Player,
        Opponent,
        Draw
    }

    public enum LogSeverity
    {
        Normal,
        Good,
        Warning
    }

    public enum RecruitLogType
    {
        TurnStart,
        Buy,
        Sell,
        Reroll,
        Freeze,
        LevelUp,
        Play,
        Triple,
        Discover
    }

    public enum SearchHintType
    {
        CanHit,
        CannotHit,
        LowGold,
        LevelUpFirst,
        FreezeValue,
        StopRolling
    }

    public enum SearchHintSeverity
    {
        Info,
        Warning,
        Good
    }
}
