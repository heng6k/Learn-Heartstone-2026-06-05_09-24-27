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
        UpdateMinion,
        PlayMinion,
        ChooseDiscover,
        NextTurn,
        DebugAddGold,
        SimulateCombat
    }

    public sealed class GameCommand
    {
        public GameCommand(GameCommandType type, int index)
        {
            Type = type;
            Index = index;
        }

        public GameCommand(GameCommandType type, string instanceId)
        {
            Type = type;
            InstanceId = instanceId;
        }

        public GameCommand(GameCommandType type, string instanceId, MinionPatch minionPatch)
        {
            Type = type;
            InstanceId = instanceId;
            MinionPatch = minionPatch;
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
        public string InstanceId { get; }
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
