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
        public CombatReplay Replay = new CombatReplay();
        public List<CombatReward> PlayerRewards = new List<CombatReward>();
        public List<CombatReward> OpponentRewards = new List<CombatReward>();
        public int Steps;
        public bool SafetyStopped;
    }

    [Serializable]
    public sealed class CombatReplay
    {
        public int Seed;
        public CombatBoardPairSnapshot InitialSnapshot = new CombatBoardPairSnapshot();
        public List<CombatFrame> Frames = new List<CombatFrame>();
        public CombatWinner Result;
        public List<CombatReward> PlayerRewards = new List<CombatReward>();
        public List<CombatReward> OpponentRewards = new List<CombatReward>();
        public int Steps;
        public bool SafetyStopped;
    }

    [Serializable]
    public sealed class CombatFrame
    {
        public int Index;
        public CombatEventType EventType;
        public BoardSide ActorSide;
        public string ActorId;
        public BoardSide TargetSide;
        public string TargetId;
        public CombatBoardSnapshot PlayerBoardSnapshot = new CombatBoardSnapshot();
        public CombatBoardSnapshot OpponentBoardSnapshot = new CombatBoardSnapshot();
        public string LogText;
        public List<string> RelatedEntityIds = new List<string>();
        public List<string> DamagedEntityIds = new List<string>();
        public List<string> DeadEntityIds = new List<string>();
        public List<string> SummonedEntityIds = new List<string>();
        public List<string> TriggerSourceIds = new List<string>();
        public List<string> OverflowedEntityIds = new List<string>();
        public BoardSide AttackPointerSide;
        public int AttackPointerIndex = -1;
        public int SummonOverflowCount;
        public int RebornOverflowCount;
        public int MechanicCounter;
        public int MechanicThreshold;
        public bool TriggeredAttack;
        public int ActualDamageCount;
        public int DivineShieldBreakCount;
    }

    [Serializable]
    public sealed class CombatBoardPairSnapshot
    {
        public CombatBoardSnapshot Player = new CombatBoardSnapshot();
        public CombatBoardSnapshot Opponent = new CombatBoardSnapshot();
    }

    [Serializable]
    public sealed class CombatBoardSnapshot
    {
        public BoardSide Side;
        public List<CombatMinionSnapshot> Minions = new List<CombatMinionSnapshot>();
    }

    [Serializable]
    public sealed class CombatMinionSnapshot
    {
        public int Position;
        public string InstanceId;
        public string CardId;
        public string Name;
        public int Attack;
        public int Health;
        public int MaxHealth;
        public int BaseAttack;
        public int BaseHealth;
        public int TavernTier;
        public bool Golden;
        public bool CanAttack;
        public int AttacksThisCombat;
        public List<Keyword> Keywords = new List<Keyword>();
        public List<Tribe> Tribes = new List<Tribe>();
        public List<string> EnchantmentSourceIds = new List<string>();
        public List<string> Tags = new List<string>();
    }

    public enum CombatEventType
    {
        CombatStarted,
        AttackDeclared,
        DamageResolved,
        DivineShieldBroken,
        VenomousResolved,
        DeathQueued,
        DeathrattleResolved,
        MinionSummoned,
        RebornResolved,
        RallyResolved,
        AvengeProgressed,
        AvengeCounterUpdated,
        DamageTriggered,
        AttackTriggered,
        SpellcraftTemporaryApplied,
        ImmediateAttackQueued,
        WindfuryResolved,
        AttackPointerRetargeted,
        SummonOverflowed,
        RebornOverflowed,
        CombatRewardQueued,
        CombatSpellCast,
        CombatEnded,
        TrinketTriggered
    }

    [Serializable]
    public sealed class CombatReward
    {
        public CombatRewardType Type;
        public BoardSide Side;
        public string SourceCardId;
        public string SourceInstanceId;
        public string TargetInstanceId;
        public string CardId;
        public int Amount;
        public int Attack;
        public int Health;
        public int TavernTier;
        public List<Tribe> Tribes = new List<Tribe>();
    }

    public enum CombatRewardType
    {
        TavernSpellCostReduction,
        AddGeneratedSpellToHand,
        EternalKnightDied,
        FriendlyMinionDied,
        FriendlyDeathrattleTriggered,
        FriendlyAvengeTriggered,
        FriendlyRallyTriggered,
        FriendlyMinionKilledEnemy,
        FriendlyMinionAttacked,
        FriendlyMinionSummoned,
        BuffHandMinion,
        ImproveBloodGemAttack,
        ImproveElementalHealth,
        ImproveRefreshBuff,
        AddTavernSpellToHand,
        AddRandomBeastToHand,
        AddRandomMagneticMechToHand,
        AddRandomChromawhelpToHand,
        ImproveUndeadAttack,
        ImproveTavernSpellAttack,
        ImproveBloodGemHealth,
        PersistAdjacentDragonCombatBuffs,
        GainFreeRefresh,
        AddRandomSameTribeMinionToHand,
        AddRandomElementalToHand,
        AddRandomDemonToHand,
        AddRandomBattlecryMinionToHand,
        AddBountyToHand,
        ImproveElementalShopStats,
        ImproveTavernMinionStats,
        AddRandomTierSixMinionToHand,
        FriendlyDeathrattleMinionDied,
        AddRandomTavernSpellToHand,
        BuffOriginalFriendlyMinion,
        ImproveAllPurposeKibble,
        TriggerFriendlyBattlecry,
        GainNextTurnGold,
        ImproveBloodGemsUntilNextCombat,
        AddRandomSpellcraftSpellToHand,
        ImproveBloodGemStats,
        ImproveTavernSpellStats,
        ImproveBeetleStats,
        BuffFriendlyTribe,
        BuffOneOfEachFriendlyType,
        BuffFriendlyBoard,
        BuffTargetHandMinion,
        AddTripleRewardToHand,
        AddCopyOfKillerToHand,
        AddPlainCopyOfKilledEnemyToHand,
        AddRandomProtossToHand,
        AddRandomGoldenBeastToHand,
        AddKeywordToOriginalFriendlyMinion
    }
}
