using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14NeutralTriggeredCarrierTests
    {
        private const string BoomBoxKey = "MIN-R53";
        private const string GatekeeperKey = "MIN-R54";

        [Test]
        public void EmbeddedCatalog_KeepsOfficialGoldenTriggerCounts()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;
            var boomBox = minions.Single(item => item.ResearchKey == BoomBoxKey);
            var gatekeeper = minions.Single(item => item.ResearchKey == GatekeeperKey);

            Assert.AreEqual("Implemented", boomBox.ImplementationStatus);
            Assert.AreEqual("Implemented", gatekeeper.ImplementationStatus);
            Assert.IsFalse(boomBox.InPool);
            Assert.IsFalse(gatekeeper.InPool);
            StringAssert.Contains("3 点伤害，触发两次", boomBox.Golden.Text);
            StringAssert.Contains("乱放的茶具，触发两次", gatekeeper.Golden.Text);
        }

        [TestCase(false, 17)]
        [TestCase(true, 14)]
        public void BoomBox_AtCombatStartDealsThreeToEveryOtherMinionPerTrigger(bool golden, int expectedHealth)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, BoomBoxKey, "boom-box", golden);
            var friendly = Minion("friendly-control", BoardSide.Player, 20, Tribe.Beast);
            var opponent = Minion("opponent-control", BoardSide.Opponent, 20, Tribe.Pirate);

            var result = CombatEngine.SimulateBasicCombat(
                new[] { source, friendly },
                new[] { opponent },
                12345,
                safetyLimit: 0);

            Assert.AreEqual(source.MaxHealth, result.FinalPlayerBoard.Single(item => item.InstanceId == source.InstanceId).Health);
            Assert.AreEqual(expectedHealth, result.FinalPlayerBoard.Single(item => item.InstanceId == friendly.InstanceId).Health);
            Assert.AreEqual(expectedHealth, result.FinalOpponentBoard.Single(item => item.InstanceId == opponent.InstanceId).Health);
        }

        [TestCase(false, 2)]
        [TestCase(true, 3)]
        public void GatekeeperAmalgam_WhenTargetedBySpellCastsMisplacedTeaSetPerTrigger(bool golden, int expectedSpellCasts)
        {
            var service = CreateService();
            var gatekeeper = CreateCatalogMinion(service, GatekeeperKey, "gatekeeper", golden);
            service.State.Player.Board.Add(gatekeeper);
            var banana = MinionFactory.Create(
                service.Catalogs.Spells.GetByCardNumber("105752"),
                BoardSide.Player,
                "gatekeeper-test");
            service.State.Player.Tavern.Hand.Add(banana);

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified));

            Assert.AreEqual(expectedSpellCasts, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(expectedSpellCasts, service.State.Player.Tavern.TavernSpellsCastThisGame);
            Assert.AreEqual("105271", service.State.Player.Tavern.LastTavernSpellCardId);
        }

        private static MatchService CreateService()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var service = MatchService.CreateWithResolvedVersion(
                resolved,
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    EnableQuests = false,
                    EnableTrinkets = false,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                });
            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.TavernSpellsCastThisTurn = 0;
            service.State.Player.Tavern.TavernSpellsCastThisGame = 0;
            return service;
        }

        private static MinionInstance CreateCatalogMinion(
            MatchService service,
            string researchKey,
            string suffix,
            bool golden)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == researchKey);
            return MinionFactory.Create(definition, BoardSide.Player, suffix, golden, PoolSource.Copy, 0);
        }

        private static MinionInstance Minion(string instanceId, BoardSide owner, int health, Tribe tribe)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                BaseAttack = 0,
                BaseHealth = health,
                Attack = 0,
                Health = health,
                MaxHealth = health,
                Owner = owner,
                CanAttack = false,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>()
            };
        }
    }
}
