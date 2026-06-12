using System.Collections.Generic;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Commands
{
    public enum GameCommandType
    {
        BuyMinion,
        SellMinion,
        RerollShop,
        FreezeShop,
        UpgradeTavern,
        MoveMinion,
        MoveBoardMinion,
        UpdateMinion,
        PlayMinion,
        ChooseDiscover,
        NextTurn,
        DebugAddGold,
        SimulateCombat,
        AddCardToHand,
        AddOpponentMinion,
        RemoveOpponentMinion,
        MoveOpponentMinion,
        UpdateOpponentMinion,
        ClearOpponentBoard,
        CopyPlayerBoardToOpponent,
        MirrorPlayerBoardToOpponent,
        SaveTestScenario,
        LoadTestScenario,
        RunCombatTest,
        ResetCombatTestSnapshot
    }

    public sealed class GameCommand
    {
        public GameCommand(GameCommandType type, int index)
        {
            Type = type;
            Index = index;
        }

        public GameCommand(GameCommandType type, int index, int targetIndex)
        {
            Type = type;
            Index = index;
            TargetIndex = targetIndex;
        }

        public GameCommand(GameCommandType type, string instanceId)
        {
            Type = type;
            InstanceId = instanceId;
        }

        public GameCommand(GameCommandType type, string instanceId, int targetIndex)
        {
            Type = type;
            InstanceId = instanceId;
            TargetIndex = targetIndex;
        }

        public GameCommand(GameCommandType type, string instanceId, MinionPatch minionPatch)
        {
            Type = type;
            InstanceId = instanceId;
            MinionPatch = minionPatch;
        }

        public GameCommand(GameCommandType type, string cardId, CardKind cardKind)
        {
            Type = type;
            CardId = cardId;
            CardKind = cardKind;
        }

        public GameCommand(GameCommandType type, string scenarioName, CombatTestOptions combatTestOptions)
        {
            Type = type;
            ScenarioName = scenarioName;
            CombatTestOptions = combatTestOptions;
        }

        public GameCommand(GameCommandType type, CombatTestOptions combatTestOptions)
        {
            Type = type;
            CombatTestOptions = combatTestOptions;
        }

        public GameCommand(GameCommandType type, bool flag)
        {
            Type = type;
            Flag = flag;
        }

        public GameCommand(GameCommandType type)
        {
            Type = type;
        }

        public GameCommandType Type { get; }
        public int Index { get; }
        public int TargetIndex { get; } = -1;
        public string InstanceId { get; }
        public string CardId { get; }
        public CardKind CardKind { get; }
        public string ScenarioName { get; }
        public CombatTestOptions CombatTestOptions { get; }
        public bool Flag { get; }
        public MinionPatch MinionPatch { get; }
    }

    public sealed class MinionPatch
    {
        public int? Attack;
        public int? Health;
        public int? MaxHealth;
        public bool? Golden;
        public List<Keyword> Keywords;
        public List<Tribe> Tribes;
    }
}
