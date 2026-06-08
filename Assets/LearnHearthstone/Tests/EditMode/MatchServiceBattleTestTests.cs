using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MatchServiceBattleTestTests
    {
        [Test]
        public void Apply_SaveAndLoadTestScenarioRestoresCustomBattleSetup()
        {
            var repository = new InMemoryTestScenarioRepository();
            var service = MatchService.CreateWithDefaultCatalog(12345, repository);
            BuildSimpleBattle(service);
            service.State.Player.Board[0].Attack = 9;

            service.Apply(new GameCommand(GameCommandType.SaveTestScenario, "simple", new CombatTestOptions()));
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Gold = 0;

            service.Apply(new GameCommand(GameCommandType.LoadTestScenario, "simple", new CombatTestOptions()));

            Assert.AreEqual(1, service.State.Player.Board.Count);
            Assert.AreEqual(1, service.State.Opponent.Board.Count);
            Assert.AreEqual(9, service.State.Player.Board[0].Attack);
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            Assert.IsTrue(repository.Exists("simple"));
        }

        [Test]
        public void Apply_RunCombatTestUsesCurrentBoardsAndStoresResetSnapshot()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            BuildSimpleBattle(service);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 777, SafetyLimit = 20 }));

            Assert.AreEqual(MatchPhase.Result, service.State.Phase);
            Assert.IsNotNull(service.State.LastResult);
            Assert.IsTrue(service.HasCombatTestSnapshot);
            Assert.AreEqual(777, service.LastCombatTestSnapshot.Options.Seed);
            Assert.AreEqual("CombatStarted", service.State.CombatLog.First().Title);
            Assert.AreEqual("CombatEnded", service.State.CombatLog.Last().Title);

            service.Apply(new GameCommand(GameCommandType.ResetCombatTestSnapshot));

            Assert.AreEqual(MatchPhase.Tavern, service.State.Phase);
            Assert.IsNull(service.State.LastResult);
            Assert.AreEqual(0, service.State.CombatLog.Count);
            Assert.AreEqual(1, service.State.Player.Board.Count);
            Assert.AreEqual("player-attacker", service.State.Player.Board[0].InstanceId);
            Assert.AreEqual(1, service.State.Opponent.Board.Count);
        }

        [Test]
        public void Apply_RunResetRunWithSameSeedProducesSameCombatLog()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            BuildSimpleBattle(service);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 888, SafetyLimit = 20 }));
            var first = service.State.CombatLog.Select(entry => entry.Seq + "|" + entry.Title + "|" + entry.ActorId + "|" + entry.TargetId + "|" + entry.Detail).ToList();
            var firstWinner = service.State.LastResult.Winner;
            service.Apply(new GameCommand(GameCommandType.ResetCombatTestSnapshot));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 888, SafetyLimit = 20 }));
            var second = service.State.CombatLog.Select(entry => entry.Seq + "|" + entry.Title + "|" + entry.ActorId + "|" + entry.TargetId + "|" + entry.Detail).ToList();

            Assert.AreEqual(firstWinner, service.State.LastResult.Winner);
            CollectionAssert.AreEqual(first, second);
        }

        private static void BuildSimpleBattle(MatchService service)
        {
            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 3;
            service.State.Player.Tavern.MaxGold = 3;

            var attacker = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            attacker.InstanceId = "player-attacker";
            attacker.Owner = BoardSide.Player;
            attacker.Attack = 4;
            attacker.Health = 4;
            attacker.MaxHealth = 4;
            service.State.Player.Board.Add(attacker);

            var defender = service.State.Player.Tavern.Shop.Last(card => card.CardKind == CardKind.Minion).Clone();
            defender.InstanceId = "opponent-defender";
            defender.Owner = BoardSide.Opponent;
            defender.Attack = 1;
            defender.Health = 2;
            defender.MaxHealth = 2;
            service.State.Opponent.Board.Add(defender);
        }
    }
}
