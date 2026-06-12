using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class TestScenarioDefinition
    {
        public string Version = "battle-test-loop-v1";
        public string Name;
        public int SavedAtRound;
        public int Seed;
        public MatchPhase Phase;
        public PlayerScenarioState Player = new PlayerScenarioState();
        public OpponentScenarioState Opponent = new OpponentScenarioState();
        public ScenarioTavernState Tavern = new ScenarioTavernState();
        public List<ScenarioCardState> Shop = new List<ScenarioCardState>();
        public List<ScenarioCardState> Hand = new List<ScenarioCardState>();
        public List<ScenarioCardState> PlayerBoard = new List<ScenarioCardState>();
        public List<ScenarioCardState> OpponentBoard = new List<ScenarioCardState>();
    }

    [Serializable]
    public sealed class PlayerScenarioState
    {
        public string HeroId;
        public int Health;
        public int Armor;
    }

    [Serializable]
    public sealed class OpponentScenarioState
    {
        public string Name;
        public string HeroId;
        public int Health;
        public int Armor;
        public int TavernTier;
        public bool Editable;
    }

    [Serializable]
    public sealed class ScenarioTavernState
    {
        public int Tier;
        public int Gold;
        public int MaxGold;
        public int UpgradeCost;
        public bool Frozen;
        public int NextTurnBonusGold;
        public int NextTavernSpellCostReduction;
        public int FreeRefreshes;
        public int DemonFodderRefreshes;
        public int TavernSpellBonusAttack;
        public int TavernSpellBonusHealth;
        public int BeetleAttackBonus;
        public int BeetleHealthBonus;
        public int FutureBallerAttackBonus;
        public int FutureBallerHealthBonus;
        public int UndeadAttackBonus;
        public int EternalKnightDeaths;
        public int AncestralAutomatonSummons;
        public int FriendlyMinionDeathsThisGame;
    }

    [Serializable]
    public sealed class ScenarioCardState
    {
        public CardKind CardKind;
        public string InstanceId;
        public string DefinitionId;
        public string CardId;
        public string Name;
        public int Cost;
        public int BaseAttack;
        public int BaseHealth;
        public int Attack;
        public int Health;
        public int MaxHealth;
        public int TavernTier;
        public List<Tribe> Tribes = new List<Tribe>();
        public List<Keyword> Keywords = new List<Keyword>();
        public List<Keyword> OfficialKeywords = new List<Keyword>();
        public string Text;
        public bool Golden;
        public BoardSide Owner;
        public List<ScenarioEnchantmentState> Enchantments = new List<ScenarioEnchantmentState>();
        public List<ScenarioCounterState> Counters = new List<ScenarioCounterState>();
        public bool CanAttack;
        public int AttacksThisCombat;
        public PoolSource OriginPoolSource;
        public bool CanReturnToPoolAfterAttach;
        public PoolSource PoolSource;
        public int PoolCopiesHeld;
        public string ImagePath;
        public List<string> EffectIds = new List<string>();
        public List<string> Tags = new List<string>();
    }

    [Serializable]
    public sealed class ScenarioEnchantmentState
    {
        public string Id;
        public string SourceId;
        public int AttackBonus;
        public int HealthBonus;
        public List<Keyword> AddedKeywords = new List<Keyword>();
        public string Duration = "PERMANENT";
    }

    [Serializable]
    public sealed class ScenarioCounterState
    {
        public string Key;
        public int Value;
    }

    [Serializable]
    public sealed class CombatTestOptions
    {
        public int Seed;
        public bool ResetBeforeRun;
        public int SafetyLimit = 200;
    }

    [Serializable]
    public sealed class CombatTestSnapshot
    {
        public TestScenarioDefinition BeforeCombat;
        public CombatTestOptions Options;
        public CombatOutput Result;
    }
}
