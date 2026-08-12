using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14MechCarrierTests
    {
        private const string RescueBotKey = "MIN-R24";
        private const string ReflectionDroneKey = "MIN-R25";
        private const string SparkWreckerKey = "MIN-R26";
        private const string MechfinKey = "MIN-R27";
        private const string GlambotKey = "MIN-R28";
        private const string ReflectionDroneActionId = "activate:min-r25";
        private const string RepairJobCardNumber = "133711";
        private const string BaseBuyCostCounter = "base_buy_cost";

        [Test]
        public void EmbeddedCatalog_DefinesMechActionAndGoldenCards()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;
            var reflectionDrone = minions.Single(item => item.ResearchKey == ReflectionDroneKey);
            var action = reflectionDrone.RecruitActions.Single();

            Assert.AreEqual(ReflectionDroneActionId, action.ActionId);
            Assert.AreEqual("season14.activate.min-r25@1", action.ResolverId);
            Assert.AreEqual(1, action.CostSpec.Gold);
            Assert.AreEqual(RecruitActionTargetSpec.None, action.TargetSpec);
            Assert.AreEqual(1, action.UsesPerTurn);
            Assert.AreEqual(MatchPhase.Tavern, action.AllowedPhase);

            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == RescueBotKey), 10, 2, "2张维修作业");
            AssertPreviewCarrier(reflectionDrone, 8, 8, "三倍");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == SparkWreckerKey), 6, 6, "6/6");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == MechfinKey), 12, 10, "四张");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == GlambotKey), 12, 12, "触发两次");
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void RescueBot_DeathrattleAddsRepairJobs(bool golden, int expectedCards)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, RescueBotKey, "rescue-bot", golden);
            source.Attack = 0;
            source.Health = source.MaxHealth = 1;
            service.State.Player.Board.Add(source);
            service.State.Opponent.Board.Add(Minion("rescue-opponent", 20, 20, Tribe.None, BoardSide.Opponent));

            service.Apply(new GameCommand(GameCommandType.SimulateCombat, new CombatTestOptions { Seed = 777, SafetyLimit = 20 }));

            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count(card => card.CardId == RepairJobCardNumber));
        }

        [TestCase(false, 2)]
        [TestCase(true, 3)]
        public void ReflectionDrone_ActivateMultipliesOnlyTheNextMagneticAttachment(bool golden, int expectedMultiplier)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, ReflectionDroneKey, "reflection-drone", golden);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = ReflectionDroneActionId,
                SourceInstanceId = source.InstanceId
            }));

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(9, service.State.Player.Tavern.Gold);

            var attackBeforeFirst = source.Attack;
            var healthBeforeFirst = source.MaxHealth;
            service.State.Player.Tavern.Hand.Add(Magnetic("first-magnetic", 2, 3));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(attackBeforeFirst + (2 * expectedMultiplier), source.Attack);
            Assert.AreEqual(healthBeforeFirst + (3 * expectedMultiplier), source.MaxHealth);

            var attackBeforeSecond = source.Attack;
            var healthBeforeSecond = source.MaxHealth;
            service.State.Player.Tavern.Hand.Add(Magnetic("second-magnetic", 2, 3));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(attackBeforeSecond + 2, source.Attack);
            Assert.AreEqual(healthBeforeSecond + 3, source.MaxHealth);
        }

        [TestCase(false, 3, 4)]
        [TestCase(true, 6, 8)]
        public void SparkWrecker_PlayedMechsReceiveImprovingSatellites(bool golden, int firstSatellite, int secondSatellite)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, SparkWreckerKey, "spark-wrecker", golden);
            service.State.Player.Board.Add(source);

            var first = Minion("first-mech", 2, 3, Tribe.Mech);
            service.State.Player.Tavern.Hand.Add(first);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(first.BaseAttack + firstSatellite, first.Attack);
            Assert.AreEqual(first.BaseHealth + firstSatellite, first.MaxHealth);

            var second = Minion("second-mech", 4, 5, Tribe.Mech);
            service.State.Player.Tavern.Hand.Add(second);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(second.BaseAttack + secondSatellite, second.Attack);
            Assert.AreEqual(second.BaseHealth + secondSatellite, second.MaxHealth);
        }

        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void Mechfin_EndOfTurnAddsOneCostTavernSpells(bool golden, int expectedCards)
        {
            var service = CreateService();
            service.State.Player.Board.Add(CreateCatalogMinion(service, MechfinKey, "mechfin", golden));

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            var spells = service.State.Player.Tavern.Hand.Where(card => card.CardKind == CardKind.TavernSpell).ToList();
            Assert.AreEqual(expectedCards, spells.Count);
            Assert.IsTrue(spells.All(card => card.Cost == 1));
            Assert.IsTrue(spells.All(card => card.Counters.TryGetValue(BaseBuyCostCounter, out var cost) && cost == 1));
        }

        [TestCase(false, 12, 17)]
        [TestCase(true, 18, 23)]
        public void Glambot_TargetedSpellAttachesSatelliteOnceOrTwice(bool golden, int expectedAttack, int expectedHealth)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, GlambotKey, "glambot", golden);
            var target = Minion("glambot-target", 2, 3, Tribe.Mech);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(target);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, RepairJobCardNumber, CardKind.TavernSpell));

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                1,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.AreEqual(expectedAttack, target.Attack);
            Assert.AreEqual(expectedHealth, target.MaxHealth);
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
            service.State.DelayedObjectStates.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
            return service;
        }

        private static MinionInstance CreateCatalogMinion(MatchService service, string researchKey, string suffix, bool golden)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == researchKey);
            return MinionFactory.Create(definition, BoardSide.Player, suffix, golden, PoolSource.Copy, 0);
        }

        private static MinionInstance Magnetic(string instanceId, int attack, int health)
        {
            return Minion(instanceId, attack, health, Tribe.Mech, BoardSide.Player, Keyword.Magnetic);
        }

        private static MinionInstance Minion(
            string instanceId,
            int attack,
            int health,
            Tribe tribe,
            BoardSide owner = BoardSide.Player,
            Keyword keyword = Keyword.Trigger)
        {
            var keywords = keyword == Keyword.Trigger
                ? new List<Keyword>()
                : new List<Keyword> { keyword };
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
                Keywords = keywords,
                OfficialKeywords = new List<Keyword>(keywords),
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
