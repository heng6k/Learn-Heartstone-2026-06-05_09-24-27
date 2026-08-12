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
    public sealed class Season14ElementalCarrierTests
    {
        private const string LivingPrisonKey = "MIN-R13";
        private const string AirBallerKey = "MIN-R14";
        private const string GutterGuardianKey = "MIN-R15";
        private const string UnboundTempestKey = "MIN-R16";

        [Test]
        public void EmbeddedCatalog_DefinesElementalActionAndGoldenCards()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;
            var prison = minions.Single(item => item.ResearchKey == LivingPrisonKey);
            var action = prison.RecruitActions.Single();

            Assert.AreEqual("activate:min-r13", action.ActionId);
            Assert.AreEqual("season14.activate.min-r13@1", action.ResolverId);
            Assert.AreEqual(1, action.CostSpec.Gold);
            Assert.AreEqual(RecruitActionTargetSpec.None, action.TargetSpec);
            Assert.AreEqual(1, action.UsesPerTurn);
            Assert.AreEqual(MatchPhase.Tavern, action.AllowedPhase);

            AssertPreviewCarrier(prison, 8, 10, "双倍属性值");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == AirBallerKey), 14, 14, "+4/+4");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == GutterGuardianKey), 8, 20, "+2/+4");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == UnboundTempestKey), 6, 24, "还剩 3 张");
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void LivingPrison_ActivateGainsNextBoughtMinionsStats(bool golden, int multiplier)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, LivingPrisonKey, "living-prison", golden);
            var bought = Minion("prison-purchase", 5, 7, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Shop.Add(bought);
            service.State.Player.Tavern.Gold = 10;

            Activate(service, source);
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(source.BaseAttack + 5 * multiplier, source.Attack);
            Assert.AreEqual(source.BaseHealth + 7 * multiplier, source.MaxHealth);
        }

        [Test]
        public void LivingPrison_UnspentActivationExpiresAfterTheTurn()
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, LivingPrisonKey, "living-prison-expired", false);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Shop.Add(Minion("late-purchase", 5, 7, Tribe.Beast));
            service.State.Player.Tavern.Gold = 10;

            Activate(service, source);
            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            service.State.Round += 1;
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(source.BaseAttack, source.Attack);
            Assert.AreEqual(source.BaseHealth, source.MaxHealth);
        }

        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void AirBaller_SellBuffsWarbandAndImprovesFutureBallers(bool golden, int amount)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, AirBallerKey, "air-baller", golden);
            var filler = Minion("air-baller-filler", 3, 5, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(filler);

            service.Apply(new GameCommand(GameCommandType.SellMinion, source.InstanceId));

            Assert.AreEqual(3 + amount, filler.Attack);
            Assert.AreEqual(5 + amount, filler.MaxHealth);
            Assert.AreEqual(amount, service.State.Player.Tavern.FutureBallerAttackBonus);
            Assert.AreEqual(amount, service.State.Player.Tavern.FutureBallerHealthBonus);
        }

        [TestCase(false, 1, 2)]
        [TestCase(true, 2, 4)]
        public void GutterGuardian_RallyImprovesElementalStatGrants(bool golden, int extraAttack, int extraHealth)
        {
            var service = CreateService();
            var guardian = CreateCatalogMinion(service, GutterGuardianKey, "gutter-guardian", golden);
            guardian.Attack = 1;
            guardian.Health = guardian.MaxHealth = 100;
            service.State.Player.Board.Add(guardian);
            service.State.Opponent.Board.Add(Minion("guardian-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 6301, SafetyLimit = 1 }));
            service.State.Phase = MatchPhase.Tavern;

            var filler = Minion("guardian-filler", 3, 5, Tribe.Beast);
            var baller = CreateCatalogMinion(service, AirBallerKey, "guardian-air-baller", false);
            service.State.Player.Board.Add(filler);
            service.State.Player.Board.Add(baller);
            service.Apply(new GameCommand(GameCommandType.SellMinion, baller.InstanceId));

            Assert.AreEqual(3 + 2 + extraAttack, filler.Attack);
            Assert.AreEqual(5 + 2 + extraHealth, filler.MaxHealth);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnboundTempest_AfterThreeElementalsGainsHighestHealthShopStats(bool golden)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, UnboundTempestKey, "unbound-tempest", golden);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Shop.Add(Minion("shop-high-attack", 99, 11, Tribe.Beast));
            service.State.Player.Tavern.Shop.Add(Minion("shop-high-health", 5, 12, Tribe.Demon));

            for (var index = 0; index < 3; index += 1)
            {
                service.State.Player.Tavern.Hand.Add(Minion("played-elemental-" + index, 1, 1, Tribe.Elemental));
                service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            }

            Assert.AreEqual(source.BaseAttack + 5, source.Attack);
            Assert.AreEqual(source.BaseHealth + 12, source.MaxHealth);
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
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
            return service;
        }

        private static void Activate(MatchService service, MinionInstance source)
        {
            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = "activate:min-r13",
                SourceInstanceId = source.InstanceId
            }));
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

        private static MinionInstance Minion(
            string instanceId,
            int attack,
            int health,
            Tribe tribe,
            BoardSide owner = BoardSide.Player,
            params Keyword[] keywords)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Owner = owner,
                CanAttack = true,
                TavernTier = 1,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                OfficialKeywords = keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>(),
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
        }

        private static void AssertPreviewCarrier(
            MinionDefinition minion,
            int goldenAttack,
            int goldenHealth,
            string goldenText)
        {
            Assert.AreEqual("Implemented", minion.ImplementationStatus);
            Assert.IsFalse(minion.InPool);
            Assert.NotNull(minion.Golden);
            Assert.AreEqual(goldenAttack, minion.Golden.BaseAttack);
            Assert.AreEqual(goldenHealth, minion.Golden.BaseHealth);
            StringAssert.Contains(goldenText, minion.Golden.Text);
        }
    }
}
