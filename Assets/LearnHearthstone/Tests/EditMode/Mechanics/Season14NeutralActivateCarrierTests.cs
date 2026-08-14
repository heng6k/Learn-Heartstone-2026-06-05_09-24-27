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
    public sealed class Season14NeutralActivateCarrierTests
    {
        private const string PrisonGuardKey = "MIN-R50";
        private const string SpellcasterKey = "MIN-R51";
        private const string FruitVendorKey = "MIN-R52";
        private const string TyraelKey = "MIN-R55";

        [Test]
        public void EmbeddedCatalog_DefinesNeutralActivateActionsAndGoldenCards()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;

            AssertAction(minions.Single(item => item.ResearchKey == PrisonGuardKey),
                "activate:suspicious-prison-guard", "season14.activate.suspicious-prison-guard@1", 1,
                RecruitActionTargetSpec.OtherFriendlyBoardMinion, 6, 6, "+6/+6");
            AssertAction(minions.Single(item => item.ResearchKey == SpellcasterKey),
                "activate:alluring-spellcaster", "season14.activate.alluring-spellcaster@1", 2,
                RecruitActionTargetSpec.None, 6, 8, "2张");
            AssertAction(minions.Single(item => item.ResearchKey == FruitVendorKey),
                "activate:fruit-vendor", "season14.activate.fruit-vendor@1", 1,
                RecruitActionTargetSpec.None, 6, 12, "4 张");
            AssertAction(minions.Single(item => item.ResearchKey == TyraelKey),
                "activate:tyrael", "season14.activate.tyrael@1", 2,
                RecruitActionTargetSpec.OtherFriendlyBoardMinion, 16, 16, "80/80");
        }

        [TestCase(false, 3)]
        [TestCase(true, 6)]
        public void SuspiciousPrisonGuard_BuffsAnotherFriendlyMinion(bool golden, int expectedBuff)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, PrisonGuardKey, "guard", golden);
            var target = Minion("guard-target", 2, 4);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 5;

            Activate(service, "activate:suspicious-prison-guard", source, target);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
            Assert.AreEqual(2 + expectedBuff, target.Attack);
            Assert.AreEqual(4 + expectedBuff, target.MaxHealth);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void AlluringSpellcaster_StealsTheHighestAttackTavernCards(bool golden, int expectedCount)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, SpellcasterKey, "spellcaster", golden);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Shop.Add(Minion("shop-low", 2, 2));
            service.State.Player.Tavern.Shop.Add(Minion("shop-high", 8, 2));
            service.State.Player.Tavern.Shop.Add(Minion("shop-middle", 5, 2));
            service.State.Player.Tavern.Gold = 5;

            Activate(service, "activate:alluring-spellcaster", source);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            Assert.AreEqual(expectedCount, service.State.Player.Tavern.Hand.Count);
            CollectionAssert.AreEqual(
                golden ? new[] { 8, 5 } : new[] { 8 },
                service.State.Player.Tavern.Hand.Select(item => item.Attack).ToArray());
        }

        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void FruitVendor_AddsOfficialBananaPlatters(bool golden, int expectedCount)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, FruitVendorKey, "vendor", golden);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Gold = 5;

            Activate(service, "activate:fruit-vendor", source);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
            Assert.AreEqual(expectedCount, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(item => item.CardId == "105752"));
        }

        [TestCase(false, 40)]
        [TestCase(true, 80)]
        public void Tyrael_SetsAnotherFriendlyMinionsStats(bool golden, int expectedStats)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, TyraelKey, "tyrael", golden);
            var target = Minion("tyrael-target", 3, 7);
            target.Health = 2;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 5;

            Activate(service, "activate:tyrael", source, target);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            Assert.AreEqual(expectedStats, target.Attack);
            Assert.AreEqual(expectedStats, target.MaxHealth);
            Assert.AreEqual(expectedStats, target.Health);
        }

        [Test]
        public void Tyrael_AndPrisonGuardRespectSetThenAddOrdering()
        {
            var setThenAdd = CreateService();
            var tyrael = CreateCatalogMinion(setThenAdd, TyraelKey, "order-tyrael", false);
            var guard = CreateCatalogMinion(setThenAdd, PrisonGuardKey, "order-guard", false);
            var target = Minion("order-target", 2, 4);
            setThenAdd.State.Player.Board.Add(tyrael);
            setThenAdd.State.Player.Board.Add(guard);
            setThenAdd.State.Player.Board.Add(target);
            setThenAdd.State.Player.Tavern.Gold = 5;

            Activate(setThenAdd, "activate:tyrael", tyrael, target);
            Activate(setThenAdd, "activate:suspicious-prison-guard", guard, target);

            Assert.AreEqual(43, target.Attack);
            Assert.AreEqual(43, target.MaxHealth);

            var addThenSet = CreateService();
            tyrael = CreateCatalogMinion(addThenSet, TyraelKey, "reverse-tyrael", false);
            guard = CreateCatalogMinion(addThenSet, PrisonGuardKey, "reverse-guard", false);
            target = Minion("reverse-target", 2, 4);
            addThenSet.State.Player.Board.Add(tyrael);
            addThenSet.State.Player.Board.Add(guard);
            addThenSet.State.Player.Board.Add(target);
            addThenSet.State.Player.Tavern.Gold = 5;

            Activate(addThenSet, "activate:suspicious-prison-guard", guard, target);
            Activate(addThenSet, "activate:tyrael", tyrael, target);

            Assert.AreEqual(40, target.Attack);
            Assert.AreEqual(40, target.MaxHealth);
            Assert.AreEqual(40, target.Health);
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
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
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

        private static MinionInstance Minion(string instanceId, int attack, int health)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>()
            };
        }

        private static void Activate(
            MatchService service,
            string actionId,
            MinionInstance source,
            MinionInstance target = null)
        {
            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = actionId,
                SourceInstanceId = source.InstanceId,
                TargetInstanceId = target?.InstanceId,
                TargetZone = target == null ? TargetZone.Unspecified : TargetZone.FriendlyBoard
            }));
        }

        private static void AssertAction(
            MinionDefinition minion,
            string actionId,
            string resolverId,
            int gold,
            RecruitActionTargetSpec targetSpec,
            int goldenAttack,
            int goldenHealth,
            string goldenText)
        {
            var action = minion.RecruitActions.Single();
            Assert.AreEqual(actionId, action.ActionId);
            Assert.AreEqual(resolverId, action.ResolverId);
            Assert.AreEqual(gold, action.CostSpec.Gold);
            Assert.AreEqual(targetSpec, action.TargetSpec);
            Assert.AreEqual(1, action.UsesPerTurn);
            Assert.AreEqual(MatchPhase.Tavern, action.AllowedPhase);
            Assert.AreEqual("Implemented", minion.ImplementationStatus);
            Assert.IsFalse(minion.InPool);
            Assert.NotNull(minion.Golden);
            Assert.AreEqual(goldenAttack, minion.Golden.BaseAttack);
            Assert.AreEqual(goldenHealth, minion.Golden.BaseHealth);
            StringAssert.Contains(goldenText, minion.Golden.Text);
        }
    }
}
