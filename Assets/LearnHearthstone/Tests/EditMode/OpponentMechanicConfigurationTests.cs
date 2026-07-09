using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class OpponentMechanicConfigurationTests
    {
        private const string AlAkirHeroPowerCardId = "TB_BaconShop_HP_086";
        private const string TavishHeroPowerCardId = "BG22_HERO_000p";

        [Test]
        public void DisabledMechanics_DoNotExposeOpponentConfiguration()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    EnableQuests = false,
                    EnableQuestRewards = false,
                    EnableTrinkets = false
                });

            Assert.IsFalse(service.OpponentQuestRewardConfigurationEnabled);
            Assert.IsFalse(service.OpponentTrinketConfigurationEnabled);
            Assert.IsTrue(service.OpponentHeroPowerConfigurationEnabled);
            Assert.IsEmpty(service.GetOpponentSelectableQuestRewards());
            Assert.IsEmpty(service.GetOpponentSelectableTrinkets(TrinketSlotKind.Lesser));
            Assert.IsNotEmpty(service.GetOpponentSelectableHeroPowers());

            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.SetOpponentQuestReward, "missing", CardKind.QuestReward)));
            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.SetOpponentTrinket, "missing", CardKind.Trinket, 0)));
        }

        [Test]
        public void SetAndClearOpponentHeroPower_DoNotSpendGoldOrTouchPlayerState()
        {
            var service = CreateService();
            var power = service.GetOpponentSelectableHeroPowers()
                .First(candidate => candidate.CardId == AlAkirHeroPowerCardId);
            var pending = PendingChoice(AdvancedMechanicKind.Trinket);
            var playerAdvanced = service.State.Player.Tavern.AdvancedMechanics;
            playerAdvanced.PendingChoice = pending;
            var playerHeroPower = service.State.Player.HeroPowerCardId;
            service.State.Player.Tavern.Gold = 6;

            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, power.CardId, CardKind.HeroPower));

            Assert.AreEqual(power.CardId, service.State.Opponent.HeroPowerCardId);
            Assert.AreEqual(power.CardId, service.GetOpponentHeroPowerDefinition().CardId);
            Assert.AreEqual(playerHeroPower, service.State.Player.HeroPowerCardId);
            Assert.AreSame(pending, playerAdvanced.PendingChoice);
            Assert.AreEqual(6, service.State.Player.Tavern.Gold);

            service.Apply(new GameCommand(GameCommandType.ClearOpponentHeroPower));

            Assert.IsNull(service.State.Opponent.HeroPowerCardId);
            Assert.IsNull(service.GetOpponentHeroPowerDefinition());
            Assert.AreEqual(-1, service.State.Opponent.HeroPowerTargetIndex);
            Assert.AreEqual(playerHeroPower, service.State.Player.HeroPowerCardId);
            Assert.AreSame(pending, playerAdvanced.PendingChoice);
            Assert.AreEqual(6, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void SetAndClearOpponentHeroPowerTarget_WritesTargetOnly()
        {
            var service = CreateService();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(TestMinion("player-target", BoardSide.Player, 1, 10));
            service.State.Opponent.Board.Add(TestMinion("opponent-target", BoardSide.Opponent, 1, 10));
            var pending = PendingChoice(AdvancedMechanicKind.Quest);
            service.State.Player.Tavern.AdvancedMechanics.PendingChoice = pending;
            service.State.Player.Tavern.Gold = 5;
            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, TavishHeroPowerCardId, CardKind.HeroPower));

            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPowerTarget, BoardSide.Player, 0));

            Assert.AreEqual(BoardSide.Player, service.State.Opponent.HeroPowerTargetSide);
            Assert.AreEqual(0, service.State.Opponent.HeroPowerTargetIndex);
            Assert.AreEqual("player-target", service.State.Opponent.HeroPowerTargetInstanceId);
            Assert.AreSame(pending, service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            Assert.AreEqual(5, service.State.Player.Tavern.Gold);

            service.Apply(new GameCommand(GameCommandType.ClearOpponentHeroPowerTarget));

            Assert.AreEqual(-1, service.State.Opponent.HeroPowerTargetIndex);
            Assert.IsNull(service.State.Opponent.HeroPowerTargetInstanceId);
            Assert.AreSame(pending, service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            Assert.AreEqual(5, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void SetOpponentQuestReward_WritesOpponentOnlyAndKeepsPlayerPendingChoice()
        {
            var service = CreateService();
            var reward = service.GetOpponentSelectableQuestRewards().First();
            var pending = PendingChoice(AdvancedMechanicKind.Trinket);
            var playerAdvanced = service.State.Player.Tavern.AdvancedMechanics;
            playerAdvanced.PendingChoice = pending;
            var playerGold = service.State.Player.Tavern.Gold;

            service.Apply(new GameCommand(GameCommandType.SetOpponentQuestReward, reward.CardId, CardKind.QuestReward));

            var opponentQuest = service.State.Opponent.AdvancedMechanics.Quests.MainQuest;
            Assert.IsNotNull(opponentQuest);
            Assert.AreEqual(reward.Id, opponentQuest.RewardId);
            Assert.IsTrue(opponentQuest.Completed);
            Assert.IsTrue(opponentQuest.RewardActive);
            Assert.AreSame(pending, playerAdvanced.PendingChoice);
            Assert.IsNull(playerAdvanced.Quests.MainQuest);
            Assert.AreEqual(playerGold, service.State.Player.Tavern.Gold);

            service.Apply(new GameCommand(GameCommandType.ClearOpponentQuestReward));

            Assert.IsNull(service.State.Opponent.AdvancedMechanics.Quests.MainQuest);
            Assert.AreSame(pending, playerAdvanced.PendingChoice);
            Assert.IsNull(playerAdvanced.Quests.MainQuest);
        }

        [Test]
        public void SetAndClearOpponentTrinkets_DoNotSpendGoldOrTouchPlayerState()
        {
            var service = CreateService();
            var lesser = service.GetOpponentSelectableTrinkets(TrinketSlotKind.Lesser).First();
            var greater = service.GetOpponentSelectableTrinkets(TrinketSlotKind.Greater).First();
            var pending = PendingChoice(AdvancedMechanicKind.Quest);
            var playerAdvanced = service.State.Player.Tavern.AdvancedMechanics;
            playerAdvanced.PendingChoice = pending;
            service.State.Player.Tavern.Gold = 7;

            service.Apply(new GameCommand(GameCommandType.SetOpponentTrinket, lesser.CardId, CardKind.Trinket, 0));
            service.Apply(new GameCommand(GameCommandType.SetOpponentTrinket, greater.CardId, CardKind.Trinket, 1));

            var opponentTrinkets = service.State.Opponent.AdvancedMechanics.Trinkets;
            Assert.AreEqual(lesser.CardId, opponentTrinkets.LesserTrinketId);
            Assert.AreEqual(greater.CardId, opponentTrinkets.GreaterTrinketId);
            Assert.AreEqual(2, opponentTrinkets.Equipped.Count);
            Assert.AreSame(pending, playerAdvanced.PendingChoice);
            Assert.IsNull(playerAdvanced.Trinkets.LesserTrinketId);
            Assert.IsNull(playerAdvanced.Trinkets.GreaterTrinketId);
            Assert.AreEqual(7, service.State.Player.Tavern.Gold);

            service.Apply(new GameCommand(GameCommandType.ClearOpponentTrinket, 0));

            Assert.IsNull(opponentTrinkets.LesserTrinketId);
            Assert.AreEqual(greater.CardId, opponentTrinkets.GreaterTrinketId);
            Assert.IsFalse(opponentTrinkets.Equipped.Any(item => item.SlotKind == TrinketSlotKind.Lesser));
            Assert.AreSame(pending, playerAdvanced.PendingChoice);
            Assert.AreEqual(7, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void OpponentCombatPreview_CarriesQuestAndGatesTrinketsByRound()
        {
            var service = CreateService();
            var reward = service.GetOpponentSelectableQuestRewards().First();
            var lesser = service.GetOpponentSelectableTrinkets(TrinketSlotKind.Lesser).First();
            var greater = service.GetOpponentSelectableTrinkets(TrinketSlotKind.Greater).First();
            service.Apply(new GameCommand(GameCommandType.SetOpponentQuestReward, reward.CardId, CardKind.QuestReward));
            service.Apply(new GameCommand(GameCommandType.SetOpponentTrinket, lesser.CardId, CardKind.Trinket, 0));
            service.Apply(new GameCommand(GameCommandType.SetOpponentTrinket, greater.CardId, CardKind.Trinket, 1));

            service.State.Round = 1;
            var roundOne = service.GetOpponentCombatTavernStatePreview();
            Assert.AreEqual(reward.Id, roundOne.AdvancedMechanics.Quests.MainQuest.RewardId);
            Assert.IsNull(roundOne.AdvancedMechanics.Trinkets.LesserTrinketId);
            Assert.IsNull(roundOne.AdvancedMechanics.Trinkets.GreaterTrinketId);
            Assert.IsNull(roundOne.AdvancedMechanics.PendingChoice);

            service.State.Round = 6;
            var roundSix = service.GetOpponentCombatTavernStatePreview();
            Assert.AreEqual(lesser.CardId, roundSix.AdvancedMechanics.Trinkets.LesserTrinketId);
            Assert.IsNull(roundSix.AdvancedMechanics.Trinkets.GreaterTrinketId);

            service.State.Round = 9;
            var roundNine = service.GetOpponentCombatTavernStatePreview();
            Assert.AreEqual(lesser.CardId, roundNine.AdvancedMechanics.Trinkets.LesserTrinketId);
            Assert.AreEqual(greater.CardId, roundNine.AdvancedMechanics.Trinkets.GreaterTrinketId);
        }

        [Test]
        public void RunCombatTest_OpponentAlAkirHeroPowerAppliesToOpponentInitialSnapshotOnly()
        {
            var service = CreateService();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(TestMinion("player-left", BoardSide.Player, 3, 30));
            service.State.Opponent.Board.Add(TestMinion("opponent-left", BoardSide.Opponent, 3, 30));
            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, AlAkirHeroPowerCardId, CardKind.HeroPower));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 17, SafetyLimit = 12 }));

            Assert.IsNotNull(service.State.LastReplay);
            var playerLeft = service.State.LastReplay.InitialSnapshot.Player.Minions.First(minion => minion.InstanceId == "player-left");
            var opponentLeft = service.State.LastReplay.InitialSnapshot.Opponent.Minions.First(minion => minion.InstanceId == "opponent-left");
            Assert.IsFalse(playerLeft.Keywords.Contains(Keyword.Windfury));
            Assert.IsFalse(playerLeft.Keywords.Contains(Keyword.DivineShield));
            Assert.IsFalse(playerLeft.Keywords.Contains(Keyword.Taunt));
            Assert.IsTrue(opponentLeft.Keywords.Contains(Keyword.Windfury));
            Assert.IsTrue(opponentLeft.Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(opponentLeft.Keywords.Contains(Keyword.Taunt));
        }

        [Test]
        public void RunCombatTest_OpponentTavishHeroPowerDamagesConfiguredPlayerTarget()
        {
            var service = CreateService();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(TestMinion("player-left", BoardSide.Player, 0, 30));
            service.State.Player.Board.Add(TestMinion("player-target", BoardSide.Player, 0, 30));
            service.State.Opponent.Board.Add(TestMinion("opponent-left", BoardSide.Opponent, 0, 30));
            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, TavishHeroPowerCardId, CardKind.HeroPower));
            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPowerTarget, BoardSide.Player, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 19, SafetyLimit = 1 }));

            Assert.IsNotNull(service.State.LastResult);
            var left = service.State.LastResult.FinalPlayerBoard.First(minion => minion.InstanceId == "player-left");
            var target = service.State.LastResult.FinalPlayerBoard.First(minion => minion.InstanceId == "player-target");
            Assert.AreEqual(30, left.Health);
            Assert.AreEqual(29, target.Health);
            Assert.IsTrue(service.State.CombatLog.Any(entry =>
                entry.Title == "HeroStartOfCombat" &&
                entry.TargetId == "player-target"));
        }

        private static MatchService CreateService()
        {
            return MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
        }

        private static MechanicChoiceRequest PendingChoice(AdvancedMechanicKind kind)
        {
            return new MechanicChoiceRequest
            {
                RequestId = "test-pending",
                Kind = kind,
                Source = "test",
                Slot = "Main",
                Round = 1
            };
        }

        private static MinionInstance TestMinion(string instanceId, BoardSide owner, int attack, int health)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId.ToUpperInvariant(),
                Name = instanceId,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Owner = owner,
                Keywords = new List<Keyword>(),
                Tribes = new List<Tribe> { Tribe.None }
            };
        }
    }
}
