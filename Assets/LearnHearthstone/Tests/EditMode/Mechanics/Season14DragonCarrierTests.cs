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
    public sealed class Season14DragonCarrierTests
    {
        private const string HiredMountKey = "MIN-R45";
        private const string BronzeTimewalkerKey = "MIN-R46";
        private const string HeavenbornEscapeDrakeKey = "MIN-R47";
        private const string DeathstriderKey = "MIN-R35";
        private const string ExpertAviatorCardId = "BG34_140";
        private const string HarmlessBoneheadCardId = "BG28_300";
        private const string RunicArcanistKey = "MIN-R48";
        private const string CrimsonGuardDragonKey = "MIN-R49";
        private const string HiredMountActionId = "activate:min-r45";
        private const string HeavenbornEscapeDrakeActionId = "activate:min-r47";

        [Test]
        public void EmbeddedCatalog_DefinesDragonGoldenCardsAndActivates()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;

            var hiredMount = minions.Single(item => item.ResearchKey == HiredMountKey);
            AssertPreviewCarrier(hiredMount, 6, 10, "2张多彩幼龙");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == BronzeTimewalkerKey), 4, 18, "2张多彩幼龙");
            var escapeDrake = minions.Single(item => item.ResearchKey == HeavenbornEscapeDrakeKey);
            AssertPreviewCarrier(escapeDrake, 10, 14, "触发两次");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == RunicArcanistKey), 6, 10, "触发两次");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == CrimsonGuardDragonKey), 16, 18, "触发两次");

            AssertActivate(hiredMount, HiredMountActionId, "season14.activate.min-r45@1", 2);
            AssertActivate(escapeDrake, HeavenbornEscapeDrakeActionId, "season14.activate.min-r47@1", 1);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void HiredMount_ActivateAddsChromawhelps(bool golden, int expectedCards)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, HiredMountKey, "hired-mount", golden);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Gold = 5;

            Activate(service, HiredMountActionId, source);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(IsChromawhelp));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void BronzeTimewalker_RallyAddsChromawhelps(bool golden, int expectedCards)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, BronzeTimewalkerKey, "bronze-timewalker", golden);
            source.Health = source.MaxHealth = 100;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(Minion("timewalker-filler", 0, 100, Tribe.None));
            service.State.Opponent.Board.Add(Minion("timewalker-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            RunOneAttack(service, 7201);

            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(IsChromawhelp));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void HeavenbornEscapeDrake_ActivateTriggersFriendlyRally(bool golden, int expectedCards)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, HeavenbornEscapeDrakeKey, "escape-drake", golden);
            var rallyTarget = CreateCatalogMinion(service, BronzeTimewalkerKey, "escape-rally-target", false);
            var deathstrider = CreateCatalogMinion(service, DeathstriderKey, "escape-deathstrider", false);
            var bonehead = CreateCatalogMinionByCardId(service, HarmlessBoneheadCardId, "escape-bonehead");
            var wall = Minion("escape-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(rallyTarget);
            service.State.Player.Board.Add(deathstrider);
            service.State.Player.Board.Add(bonehead);
            service.State.Opponent.Board.Add(wall);
            service.State.Player.Tavern.Gold = 5;

            Activate(service, HeavenbornEscapeDrakeActionId, source);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(IsChromawhelp));
            Assert.AreEqual(4, service.State.Player.Board.Count, "Replaying Rally is not an attack, so Deathstrider must not trigger the Bonehead Deathrattle.");
            Assert.AreEqual(0, rallyTarget.AttacksThisCombat);
            Assert.AreEqual(100, wall.Health);
        }

        [Test]
        public void HeavenbornEscapeDrake_ExpertAviatorSummonExistsOnlyForCombatAndDoesNotAttackDuringActivate()
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, HeavenbornEscapeDrakeKey, "escape-aviator-source", false);
            var aviator = CreateCatalogMinionByCardId(service, ExpertAviatorCardId, "escape-aviator");
            var low = Minion("escape-hand-low", 2, 4, Tribe.Beast);
            var high = Minion("escape-hand-high", 9, 9, Tribe.Beast);
            var wall = Minion("escape-aviator-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(aviator);
            service.State.Player.Tavern.Hand.Add(low);
            service.State.Player.Tavern.Hand.Add(high);
            service.State.Opponent.Board.Add(wall);
            service.State.Player.Tavern.Gold = 5;

            Activate(service, HeavenbornEscapeDrakeActionId, source);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(0, aviator.AttacksThisCombat, "Activate must replay only the Rally effect.");
            Assert.AreEqual(100, wall.Health, "Activate must not deal attack damage.");

            RunOneAttack(service, 7204);

            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Any(card => card.InstanceId.Contains(high.InstanceId)));
            Assert.IsFalse(service.State.LastResult.FinalPlayerBoard.Any(card => card.InstanceId.Contains(low.InstanceId)));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.InstanceId == high.InstanceId));
            Assert.IsFalse(
                service.State.Player.Board.Any(card => card.InstanceId.Contains(high.InstanceId)),
                "Expert Aviator says the summoned copy exists for this combat only and must not persist on the recruit board.");
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void RunicArcanist_StartOfCombatCastsShinyRing(bool golden, int expectedCasts)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, RunicArcanistKey, "runic-arcanist", golden);
            var other = Minion("runic-arcanist-other", 2, 100, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(other);
            service.State.Opponent.Board.Add(Minion("runic-arcanist-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            RunOneAttack(service, 7202);

            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, source, expectedCasts, expectedCasts);
            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, other, expectedCasts, expectedCasts);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void CrimsonGuardDragon_RallyCastsMightyDragonbreath(bool golden, int expectedCasts)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, CrimsonGuardDragonKey, "crimson-guard", golden);
            var dragon = Minion("crimson-dragon", 2, 100, Tribe.Dragon);
            var shield = Minion("crimson-shield", 2, 100, Tribe.Beast, BoardSide.Player, Keyword.DivineShield);
            var plain = Minion("crimson-plain", 2, 100, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(dragon);
            service.State.Player.Board.Add(shield);
            service.State.Player.Board.Add(plain);
            service.State.Opponent.Board.Add(Minion("crimson-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            RunOneAttack(service, 7203);

            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, source, 3 * expectedCasts, 3 * expectedCasts);
            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, dragon, 2 * expectedCasts, 2 * expectedCasts);
            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, shield, 2 * expectedCasts, 2 * expectedCasts);
            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, plain, expectedCasts, expectedCasts);
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
            service.State.ActiveTribes = new List<Tribe> { Tribe.Dragon, Tribe.Beast };
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Hand.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
            return service;
        }

        private static void Activate(MatchService service, string actionId, MinionInstance source)
        {
            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = actionId,
                SourceInstanceId = source.InstanceId,
                TargetZone = TargetZone.Unspecified
            }));
        }

        private static void RunOneAttack(MatchService service, int seed)
        {
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = seed, SafetyLimit = 1 }));
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

        private static MinionInstance CreateCatalogMinionByCardId(
            MatchService service,
            string cardId,
            string suffix)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.CardId == cardId);
            return MinionFactory.Create(definition, BoardSide.Player, suffix, false, PoolSource.Copy, 0);
        }

        private static MinionInstance Minion(
            string instanceId,
            int attack,
            int health,
            Tribe tribe,
            BoardSide owner = BoardSide.Player,
            Keyword keyword = Keyword.Trigger)
        {
            var keywords = keyword == Keyword.Trigger ? new List<Keyword>() : new List<Keyword> { keyword };
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

        private static bool IsChromawhelp(MinionInstance minion)
        {
            return minion != null && new[] { "BG34_634t", "BG34_635t", "BG34_636t", "BG34_637t", "BG34_638t" }.Contains(minion.CardId);
        }

        private static void AssertCombatBuff(
            IEnumerable<MinionInstance> board,
            MinionInstance original,
            int attack,
            int health)
        {
            var actual = board.Single(card => card.InstanceId == original.InstanceId);
            Assert.AreEqual(original.BaseAttack + attack, actual.Attack);
            Assert.AreEqual(original.BaseHealth + health, actual.MaxHealth);
        }

        private static void AssertActivate(
            MinionDefinition minion,
            string actionId,
            string resolverId,
            int gold)
        {
            var action = minion.RecruitActions.Single();
            Assert.AreEqual(actionId, action.ActionId);
            Assert.AreEqual(resolverId, action.ResolverId);
            Assert.AreEqual(gold, action.CostSpec.Gold);
            Assert.AreEqual(RecruitActionTargetSpec.None, action.TargetSpec);
            Assert.AreEqual(1, action.UsesPerTurn);
            Assert.AreEqual(MatchPhase.Tavern, action.AllowedPhase);
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
