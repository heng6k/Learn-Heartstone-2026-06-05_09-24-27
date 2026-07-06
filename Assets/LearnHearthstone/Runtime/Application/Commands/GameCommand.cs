using System.Collections.Generic;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Commands
{
    public enum GameCommandType
    {
        BuyMinion,
        BuyTimewarpedTavernCard,
        ExitTimewarpedTavern,
        SellMinion,
        RerollShop,
        FreezeShop,
        UpgradeTavern,
        MoveMinion,
        MoveBoardMinion,
        UpdateMinion,
        PlayMinion,
        DiscardCardFromHand,
        UseHeroPower,
        ChooseDiscover,
        ChooseMechanicOption,
        NextTurn,
        DebugAddGold,
        DebugOfferLesserTrinkets,
        DebugOfferGreaterTrinkets,
        DebugOfferQuests,
        DebugCompleteQuest,
        DebugReplaceQuestReward,
        DebugReplaceTrinket,
        SimulateCombat,
        AddCardToHand,
        RemoveHandCard,
        SetSideCombatModifier,
        AdjustSideCombatModifier,
        DebugSkipToNextTurn,
        DebugCastCard,
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

        public GameCommand(GameCommandType type, int index, int targetIndex, int secondaryTargetIndex)
        {
            Type = type;
            Index = index;
            TargetIndex = targetIndex;
            SecondaryTargetIndex = secondaryTargetIndex;
        }

        public GameCommand(
            GameCommandType type,
            int index,
            int targetIndex,
            TargetZone targetZone,
            int secondaryTargetIndex,
            TargetZone secondaryTargetZone,
            string targetInstanceId = null,
            string secondaryTargetInstanceId = null,
            string choiceId = null,
            string heroPowerCardId = null)
        {
            Type = type;
            Index = index;
            TargetIndex = targetIndex;
            TargetZone = targetZone;
            SecondaryTargetIndex = secondaryTargetIndex;
            SecondaryTargetZone = secondaryTargetZone;
            TargetInstanceId = targetInstanceId;
            SecondaryTargetInstanceId = secondaryTargetInstanceId;
            ChoiceId = choiceId;
            HeroPowerCardId = heroPowerCardId;
        }

        public GameCommand(
            GameCommandType type,
            int targetIndex,
            TargetZone targetZone,
            int secondaryTargetIndex = -1,
            TargetZone secondaryTargetZone = TargetZone.Unspecified,
            string targetInstanceId = null,
            string secondaryTargetInstanceId = null,
            string choiceId = null,
            string heroPowerCardId = null)
        {
            Type = type;
            TargetIndex = targetIndex;
            TargetZone = targetZone;
            SecondaryTargetIndex = secondaryTargetIndex;
            SecondaryTargetZone = secondaryTargetZone;
            TargetInstanceId = targetInstanceId;
            SecondaryTargetInstanceId = secondaryTargetInstanceId;
            ChoiceId = choiceId;
            HeroPowerCardId = heroPowerCardId;
        }

        public GameCommand(GameCommandType type, string instanceId)
        {
            Type = type;
            InstanceId = instanceId;
        }

        public GameCommand(GameCommandType type, string instanceId, bool flag)
        {
            Type = type;
            InstanceId = instanceId;
            Flag = flag;
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

        public GameCommand(GameCommandType type, BoardSide side, string cardId, CardKind cardKind)
        {
            Type = type;
            Side = side;
            CardId = cardId;
            CardKind = cardKind;
        }

        public GameCommand(GameCommandType type, BoardSide side, int index)
        {
            Type = type;
            Side = side;
            Index = index;
        }

        public GameCommand(GameCommandType type, BoardSide side, SideCombatModifierKind modifierKind, int value)
        {
            Type = type;
            Side = side;
            SideCombatModifierKind = modifierKind;
            Value = value;
        }

        public GameCommand(GameCommandType type, string cardId, CardKind cardKind, int targetIndex)
        {
            Type = type;
            CardId = cardId;
            CardKind = cardKind;
            TargetIndex = targetIndex;
        }

        public GameCommand(GameCommandType type, string cardId, CardKind cardKind, bool flag)
        {
            Type = type;
            CardId = cardId;
            CardKind = cardKind;
            Flag = flag;
        }

        public GameCommand(GameCommandType type, string cardId, CardKind cardKind, bool flag, int index)
        {
            Type = type;
            CardId = cardId;
            CardKind = cardKind;
            Flag = flag;
            Index = index;
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
        public TargetZone TargetZone { get; } = TargetZone.Unspecified;
        public int SecondaryTargetIndex { get; } = -1;
        public TargetZone SecondaryTargetZone { get; } = TargetZone.Unspecified;
        public string TargetInstanceId { get; }
        public string SecondaryTargetInstanceId { get; }
        public string ChoiceId { get; }
        public string HeroPowerCardId { get; }
        public string InstanceId { get; }
        public string CardId { get; }
        public CardKind CardKind { get; }
        public string ScenarioName { get; }
        public CombatTestOptions CombatTestOptions { get; }
        public bool Flag { get; }
        public MinionPatch MinionPatch { get; }
        public BoardSide Side { get; } = BoardSide.Player;
        public SideCombatModifierKind SideCombatModifierKind { get; }
        public int Value { get; }
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
