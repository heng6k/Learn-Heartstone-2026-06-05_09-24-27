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
    public sealed class Season14LockboxFishbaitCarrierTests
    {
        private const string BilgewaterResearchKey = "LOCK-R02N";
        private const string MutineerResearchKey = "LOCK-R03N";
        private const string FishbaitResearchKey = "FISH-R01N";
        private const string LionfishResearchKey = "FISH-R02N";
        private const string SnarkySharkResearchKey = "FISH-R03N";
        private const string LionfishActionId = "activate:lurking-lionfish";

        [Test]
        public void EmbeddedCatalog_DefinesOfficialCarrierKeywordsAndLionfishActivate()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").English.Minions.All;
            var bilgewater = minions.Single(item => item.ResearchKey == BilgewaterResearchKey);
            var mutineer = minions.Single(item => item.ResearchKey == MutineerResearchKey);
            var fishbait = minions.Single(item => item.ResearchKey == FishbaitResearchKey);
            var lionfish = minions.Single(item => item.ResearchKey == LionfishResearchKey);

            Assert.Contains(Keyword.Battlecry, bilgewater.Keywords);
            Assert.Contains(Keyword.Deathrattle, mutineer.Keywords);
            Assert.Contains(Keyword.Deathrattle, fishbait.Keywords);
            var action = lionfish.RecruitActions.Single();
            Assert.AreEqual(LionfishActionId, action.ActionId);
            Assert.AreEqual("season14.activate.lurking-lionfish@1", action.ResolverId);
            Assert.AreEqual(2, action.CostSpec.Gold);
            Assert.AreEqual(RecruitActionTargetSpec.TavernMinion, action.TargetSpec);
        }

        [TestCase(false, 4)]
        [TestCase(true, 3)]
        public void BilgewaterBreakout_CreatesThenAcceleratesTheSharedLockbox(bool goldenSecondCopy, int expectedTurns)
        {
            var service = CreateService();
            PlayCatalogMinion(service, BilgewaterResearchKey, false, "first");

            Assert.AreEqual(5, service.State.DelayedObjectStates.Single().RemainingTurns);

            PlayCatalogMinion(service, BilgewaterResearchKey, goldenSecondCopy, "second");

            Assert.AreEqual(1, service.State.DelayedObjectStates.Count);
            Assert.AreEqual(expectedTurns, service.State.DelayedObjectStates.Single().RemainingTurns);
            var acceleration = service.State.MechanicEvents.Single(item =>
                item.Type == "delayed-object.battlecry-accelerated");
            Assert.AreEqual(service.State.Player.Board.Last().InstanceId, acceleration.Source);
        }

        [Test]
        public void Lockbox_AfterFiveTurnsAddsOneRandomGoldenTypedMinionToHand()
        {
            var service = CreateService();
            PlayCatalogMinion(service, BilgewaterResearchKey, false, "open");

            for (var index = 0; index < 5; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.NextTurn));
            }

            var lockbox = service.State.DelayedObjectStates.Single();
            var reward = service.State.Player.Tavern.Hand.Single();
            Assert.IsTrue(lockbox.Opened);
            Assert.IsTrue(reward.Golden);
            Assert.IsTrue(reward.Tribes.Any(tribe => tribe != Tribe.None && tribe != Tribe.All));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void Lockbox_GoldenRewardNeverExceedsTheCurrentTavernTier(int tavernTier)
        {
            for (var seed = 1; seed <= 12; seed += 1)
            {
                var service = CreateService(seed);
                service.State.Player.Tavern.Tier = tavernTier;
                PlayCatalogMinion(service, BilgewaterResearchKey, false, "tier-cap-" + seed);

                for (var turn = 0; turn < 5; turn += 1)
                {
                    service.Apply(new GameCommand(GameCommandType.NextTurn));
                }

                var reward = service.State.Player.Tavern.Hand.Single();
                var definition = service.Catalogs.Minions.All.Single(item => item.Id == reward.DefinitionId);
                Assert.IsTrue(reward.Golden, "seed " + seed + " did not create a Golden reward");
                Assert.IsTrue(definition.InPool, "seed " + seed + " selected a non-pool minion");
                Assert.LessOrEqual(
                    definition.TavernTier,
                    tavernTier,
                    "seed " + seed + " selected " + reward.CardId + " above Tavern Tier " + tavernTier);
            }
        }

        [Test]
        public void LockedUpMutineer_DeathrattleCreatesLockboxThroughCombatRewardPipeline()
        {
            var service = CreateService();
            var mutineer = CreateCatalogMinion(service, MutineerResearchKey, "combat", false);
            service.State.Player.Board.Add(mutineer);
            service.State.Opponent.Board.Clear();
            service.State.Opponent.Board.Add(Minion("opponent", "TEST_OPPONENT", 20, 20, Tribe.None));

            service.Apply(new GameCommand(
                GameCommandType.SimulateCombat,
                new CombatTestOptions { Seed = 777, SafetyLimit = 20 }));

            Assert.AreEqual(1, service.State.DelayedObjectStates.Count);
            Assert.AreEqual(5, service.State.DelayedObjectStates.Single().RemainingTurns);
        }

        [TestCase(false, 5)]
        [TestCase(true, 10)]
        public void LurkingLionfish_ReplacesSelectedShopCardAndAttacksItsFishbait(bool golden, int expectedBuff)
        {
            var service = CreateService();
            var lionfish = CreateCatalogMinion(service, LionfishResearchKey, "activate", golden);
            service.State.Player.Board.Add(lionfish);
            var target = Minion("replace-me", "TEST_REPLACE", 4, 4, Tribe.None);
            service.State.Player.Tavern.Shop.Add(target);
            service.State.Player.Tavern.Gold = 5;
            var attackBefore = lionfish.Attack;
            var healthBefore = lionfish.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = LionfishActionId,
                SourceInstanceId = lionfish.InstanceId,
                TargetInstanceId = target.InstanceId,
                TargetZone = TargetZone.TavernShop
            }));

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            var liveLionfish = service.State.Player.Board.Single(item => item.InstanceId == lionfish.InstanceId);
            Assert.AreEqual(attackBefore + expectedBuff, liveLionfish.Attack);
            Assert.AreEqual(healthBefore + expectedBuff, liveLionfish.MaxHealth);
            Assert.IsEmpty(service.State.Player.Tavern.Shop);
        }

        [TestCase(false, 5)]
        [TestCase(true, 10)]
        public void SnarkyShark_OnSellRefreshesWithFishbaitAndMakesLeftmostRemainingBeastAttack(bool golden, int expectedBuff)
        {
            var service = CreateService();
            var shark = CreateCatalogMinion(service, SnarkySharkResearchKey, "sell", golden);
            var attacker = Minion("leftmost-beast", "TEST_LEFTMOST_BEAST", 2, 3, Tribe.Beast);
            service.State.Player.Board.Add(shark);
            service.State.Player.Board.Add(attacker);
            service.State.Player.Tavern.Shop.Add(Minion("old-shop", "TEST_OLD_SHOP", 1, 1, Tribe.None));
            var attackBefore = attacker.Attack;
            var healthBefore = attacker.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.SellMinion, shark.InstanceId));

            var liveAttacker = service.State.Player.Board.Single(item => item.InstanceId == attacker.InstanceId);
            Assert.AreEqual(attackBefore + expectedBuff, liveAttacker.Attack);
            Assert.AreEqual(healthBefore + expectedBuff, liveAttacker.MaxHealth);
            Assert.IsFalse(service.State.Player.Board.Any(item => item.InstanceId == shark.InstanceId));
            Assert.IsTrue(service.State.MechanicEvents.Any(item => item.Type == "fishbait.refreshed"));
            Assert.IsTrue(service.State.MechanicEvents.Any(item => item.Type == "fishbait.reward.resolved"));
        }

        [Test]
        public void Fishbait_CannotGainStatsFromCentralStatOperations()
        {
            var service = CreateService();
            var fishbait = CreateCatalogMinion(service, FishbaitResearchKey, "immune", false);

            StatMath.ApplyEnchantment(fishbait, new Enchantment
            {
                Id = "test-buff",
                SourceId = "test-buff",
                AttackBonus = 9,
                HealthBonus = 9
            });
            StatMath.ApplyStatDelta(fishbait, 4, 4);

            Assert.AreEqual(0, fishbait.Attack);
            Assert.AreEqual(1, fishbait.MaxHealth);
            Assert.AreEqual(1, fishbait.Health);
            Assert.IsEmpty(fishbait.Enchantments);
        }

        private static MatchService CreateService(int seed = 12345)
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var service = MatchService.CreateWithResolvedVersion(
                resolved,
                seed,
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
            service.State.DelayedObjectStates.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
            return service;
        }

        private static void PlayCatalogMinion(MatchService service, string researchKey, bool golden, string suffix)
        {
            service.State.Player.Tavern.Hand.Add(CreateCatalogMinion(service, researchKey, suffix, golden));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
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
            string cardId,
            int attack,
            int health,
            Tribe tribe)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = instanceId,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Owner = BoardSide.Player,
                CanAttack = true,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
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
