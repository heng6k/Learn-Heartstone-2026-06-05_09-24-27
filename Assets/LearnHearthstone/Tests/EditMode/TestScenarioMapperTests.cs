using System.Collections.Generic;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TestScenarioMapperTests
    {
        [Test]
        public void CaptureAndApply_RoundTripsEditedBoardsHandAndTavernState()
        {
            var service = MatchService.CreateWithDefaultCatalog(2468, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Tier = 4;
            service.State.Player.Tavern.Gold = 7;
            service.State.Player.Tavern.MaxGold = 9;
            service.State.Player.Tavern.UpgradeCost = 6;
            service.State.Player.Tavern.TavernSpellsCastThisGame = 6;
            service.State.Player.Tavern.BloodGemBonusAttack = 2;
            service.State.Player.Tavern.BloodGemBonusHealth = 1;
            service.State.Opponent.CombatModifiers.BloodGemAttackBonus = 4;
            service.State.Opponent.CombatModifiers.UndeadAttackBonus = 5;
            service.State.Opponent.CombatModifiers.AstralAutomatonSummons = 3;
            service.State.Round = 5;
            service.State.Phase = MatchPhase.Result;

            var player = service.State.Player.Tavern.Shop[0].Clone();
            player.InstanceId = "player-edited";
            player.Attack = 12;
            player.Health = 6;
            player.MaxHealth = 8;
            player.Golden = true;
            player.Keywords.Add(Keyword.DivineShield);
            player.Counters["test"] = 3;
            service.State.Player.Board.Add(player);

            var hand = service.State.Player.Tavern.Shop[1].Clone();
            hand.InstanceId = "hand-edited";
            service.State.Player.Tavern.Hand.Add(hand);

            var opponentHand = service.State.Player.Tavern.Shop[1].Clone();
            opponentHand.InstanceId = "opponent-hand-edited";
            opponentHand.Owner = BoardSide.Opponent;
            service.State.Opponent.Hand.Add(opponentHand);

            var opponent = service.State.Player.Tavern.Shop[2].Clone();
            opponent.InstanceId = "opponent-edited";
            opponent.Owner = BoardSide.Opponent;
            opponent.Keywords.Add(Keyword.Taunt);
            service.State.Opponent.Board.Add(opponent);

            var scenario = TestScenarioMapper.Capture(service.State, "edited");
            var target = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository()).State;

            TestScenarioMapper.ApplyTo(target, scenario);

            Assert.AreEqual(5, target.Round);
            Assert.AreEqual(2468, target.Seed);
            Assert.AreEqual(MatchPhase.Result, target.Phase);
            Assert.AreEqual(4, target.Player.Tavern.Tier);
            Assert.AreEqual(7, target.Player.Tavern.Gold);
            Assert.AreEqual(9, target.Player.Tavern.MaxGold);
            Assert.AreEqual(6, target.Player.Tavern.UpgradeCost);
            Assert.AreEqual(1, target.Player.Board.Count);
            Assert.AreEqual("player-edited", target.Player.Board[0].InstanceId);
            Assert.AreEqual(12, target.Player.Board[0].Attack);
            Assert.AreEqual(6, target.Player.Board[0].Health);
            Assert.IsTrue(target.Player.Board[0].Golden);
            Assert.IsTrue(target.Player.Board[0].Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(3, target.Player.Board[0].Counters["test"]);
            Assert.AreEqual(1, target.Player.Tavern.Hand.Count);
            Assert.AreEqual("hand-edited", target.Player.Tavern.Hand[0].InstanceId);
            Assert.AreEqual(1, target.Opponent.Hand.Count);
            Assert.AreEqual("opponent-hand-edited", target.Opponent.Hand[0].InstanceId);
            Assert.AreEqual(BoardSide.Opponent, target.Opponent.Hand[0].Owner);
            Assert.AreEqual(1, target.Opponent.Board.Count);
            Assert.AreEqual(BoardSide.Opponent, target.Opponent.Board[0].Owner);
            Assert.IsTrue(target.Opponent.Board[0].Keywords.Contains(Keyword.Taunt));
            Assert.AreEqual(6, target.Player.CombatModifiers.SpellsCastThisGame);
            Assert.AreEqual(2, target.Player.CombatModifiers.BloodGemAttackBonus);
            Assert.AreEqual(1, target.Player.CombatModifiers.BloodGemHealthBonus);
            Assert.AreEqual(4, target.Opponent.CombatModifiers.BloodGemAttackBonus);
            Assert.AreEqual(5, target.Opponent.CombatModifiers.UndeadAttackBonus);
            Assert.AreEqual(3, target.Opponent.CombatModifiers.AstralAutomatonSummons);
            Assert.AreNotSame(player, target.Player.Board[0]);
        }

        [Test]
        public void ApplyTo_ClampsInvalidHealthValues()
        {
            var scenario = new TestScenarioDefinition
            {
                Name = "clamp",
                SavedAtRound = 1,
                Seed = 1,
                Tavern = new ScenarioTavernState { Tier = 1, Gold = 3, MaxGold = 3, UpgradeCost = 5 },
                PlayerBoard = new List<ScenarioCardState>
                {
                    new ScenarioCardState
                    {
                        CardKind = CardKind.Minion,
                        InstanceId = "bad-health",
                        DefinitionId = "bad",
                        CardId = "BAD",
                        Name = "bad",
                        Attack = -2,
                        Health = 99,
                        MaxHealth = -5,
                        Tribes = new List<Tribe> { Tribe.None },
                        Keywords = new List<Keyword>()
                    }
                }
            };
            var target = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository()).State;

            TestScenarioMapper.ApplyTo(target, scenario);

            Assert.AreEqual(0, target.Player.Board[0].Attack);
            Assert.AreEqual(1, target.Player.Board[0].MaxHealth);
            Assert.AreEqual(1, target.Player.Board[0].Health);
        }
    }
}
