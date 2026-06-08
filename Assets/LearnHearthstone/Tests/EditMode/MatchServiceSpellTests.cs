using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MatchServiceSpellTests
    {
        [Test]
        public void Apply_PlayPointyArrowBuffsFirstAvailableMinionAttack()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            var beforeAttack = target.Attack;
            AddSpellToHand(service, "100596");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(beforeAttack + 4, service.State.Player.Board[0].Attack);
            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Last().Message.Contains("尖利箭矢"));
        }

        [Test]
        public void Apply_PlayTavernCoinGainsGoldWithoutAddingBoardCard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 0;
            service.State.Player.Tavern.MaxGold = 3;
            AddSpellToHand(service, "104436");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Gold);
            Assert.AreEqual(0, service.State.Player.Board.Count);
            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
        }

        [Test]
        public void Apply_PlayPerfectImageSetsTargetStatsToTwentyTwenty()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            AddBoardTarget(service);
            AddSpellToHand(service, "104601");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(20, service.State.Player.Board[0].Attack);
            Assert.AreEqual(20, service.State.Player.Board[0].Health);
            Assert.AreEqual(20, service.State.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Apply_PlayUnexpectedFruitBuffsShopMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var shopTarget = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            var beforeAttack = shopTarget.Attack;
            var beforeHealth = shopTarget.Health;
            AddSpellToHand(service, "105903");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(beforeAttack + 1, shopTarget.Attack);
            Assert.AreEqual(beforeHealth + 2, shopTarget.Health);
        }

        [Test]
        public void Apply_PlayNewSaplingStartsTierOneDiscover()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            AddSpellToHand(service, "122864");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(1, service.State.Player.Tavern.Discover.RewardTier);
            Assert.AreEqual(3, service.State.Player.Tavern.Discover.Options.Count);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(option => option.TavernTier == 1));
        }

        [Test]
        public void Apply_PlayMischievousPackageAddsThreeSpellCards()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 4;
            service.State.Player.Tavern.Hand.Clear();
            AddSpellToHand(service, "110407");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.TavernSpell));
        }

        private static MinionInstance AddBoardTarget(MatchService service)
        {
            service.State.Player.Board.Clear();
            var target = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            target.InstanceId = "spell-target";
            target.Owner = BoardSide.Player;
            service.State.Player.Board.Add(target);
            return target;
        }

        private static void AddSpellToHand(MatchService service, string cardNumber)
        {
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardNumber, CardKind.TavernSpell));
        }
    }
}
