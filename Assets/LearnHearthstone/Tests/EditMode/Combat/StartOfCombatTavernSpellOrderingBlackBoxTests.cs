using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StartOfCombatTavernSpellOrderingBlackBoxTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void RunCombatTest_TwoSidedShareTheLoveUsesEstablishedSeededSideOrder(bool playerFirst)
        {
            var seed = FindSeed(playerFirst);
            var service = CreateServiceWithPassiveBoards(2, 20, 3, 30);
            service.Apply(new GameCommand(GameCommandType.DebugCastCard, "119599", CardKind.TavernSpell, -1));
            service.Apply(new GameCommand(GameCommandType.SetOpponentStartOfCombatSpell, "119599", CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = seed, SafetyLimit = 1 }));

            var player = service.State.LastResult.FinalPlayerBoard.Single(minion => minion.InstanceId == "order-player");
            var opponent = service.State.LastResult.FinalOpponentBoard.Single(minion => minion.InstanceId == "order-opponent");
            if (playerFirst)
            {
                Assert.AreEqual(5, player.Attack);
                Assert.AreEqual(50, player.MaxHealth);
                Assert.AreEqual(8, opponent.Attack);
                Assert.AreEqual(80, opponent.MaxHealth);
            }
            else
            {
                Assert.AreEqual(7, player.Attack);
                Assert.AreEqual(70, player.MaxHealth);
                Assert.AreEqual(5, opponent.Attack);
                Assert.AreEqual(50, opponent.MaxHealth);
            }
        }

        [Test]
        public void RunCombatTest_QueuedSpellsKeepTheirEstablishedWithinSidePhaseOrder()
        {
            var service = CreateServiceWithPassiveBoards(2, 20, 0, 30);
            foreach (var cardId in new[] { "105665", "110401", "119599", "127503" })
            {
                service.Apply(new GameCommand(GameCommandType.DebugCastCard, cardId, CardKind.TavernSpell, -1));
            }

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 17, SafetyLimit = 1 }));

            var player = service.State.LastResult.FinalPlayerBoard.Single(minion => minion.InstanceId == "order-player");
            Assert.AreEqual(8, player.Attack, "Board buff, stat copy, then Attack doubling must remain ordered.");
            Assert.AreEqual(51, player.MaxHealth);
            var beetles = service.State.LastResult.FinalPlayerBoard.Where(minion => minion.InstanceId != "order-player").ToList();
            Assert.AreEqual(2, beetles.Count);
            Assert.IsTrue(beetles.All(beetle => beetle.Attack == 2 && beetle.MaxHealth == 2), "Beetles are summoned after the board-wide spell buff.");
        }

        [Test]
        public void RunCombatTest_HeroStartOfCombatResolvesBeforeQueuedTavernSpell()
        {
            var service = CreateServiceWithPassiveBoards(2, 2, 1, 30);
            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, "BG22_HERO_000p", CardKind.HeroPower));
            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPowerTarget, BoardSide.Player, 0));
            service.Apply(new GameCommand(GameCommandType.SetOpponentStartOfCombatSpell, "104560", CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 23, SafetyLimit = 1 }));

            var player = service.State.LastResult.FinalPlayerBoard.Single(minion => minion.InstanceId == "order-player");
            Assert.AreEqual(1, player.Health, "Tavish must deal damage before Upper Hand sets the surviving minion to 1 Health.");
            var heroIndex = service.State.CombatLog.FindIndex(entry => entry.Title == "HeroStartOfCombat" && entry.Detail.Contains("Deadeye"));
            var spellIndex = service.State.CombatLog.FindIndex(entry => entry.Title == "CombatSpellCast");
            Assert.GreaterOrEqual(heroIndex, 0);
            Assert.Greater(spellIndex, heroIndex);
        }

        private static MatchService CreateServiceWithPassiveBoards(int playerAttack, int playerHealth, int opponentAttack, int opponentHealth)
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var template = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(PassiveMinion(template, "order-player", BoardSide.Player, playerAttack, playerHealth));
            service.State.Opponent.Board.Add(PassiveMinion(template, "order-opponent", BoardSide.Opponent, opponentAttack, opponentHealth));
            return service;
        }

        private static MinionInstance PassiveMinion(MinionInstance template, string instanceId, BoardSide side, int attack, int health)
        {
            var minion = template.Clone();
            minion.InstanceId = instanceId;
            minion.Owner = side;
            minion.Attack = attack;
            minion.Health = health;
            minion.MaxHealth = health;
            minion.CanAttack = false;
            minion.Keywords.Clear();
            minion.Tags.Clear();
            minion.Enchantments.Clear();
            return minion;
        }

        private static int FindSeed(bool playerFirst)
        {
            for (var seed = 1; seed <= 1000; seed += 1)
            {
                if ((new SeededRng(seed + 7919).NextInt(2) == 0) == playerFirst)
                {
                    return seed;
                }
            }

            Assert.Fail("No seed found for requested start-of-combat side order.");
            return 0;
        }
    }
}
