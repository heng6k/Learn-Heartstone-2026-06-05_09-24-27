using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14BeastCarrierTests
    {
        private const string FlutteringBatKey = "MIN-R29";
        private const string DeliciousLobsterKey = "MIN-R30";
        private const string WolfPupKey = "MIN-R31";
        private const string HeadhunterGryphonKey = "MIN-R32";
        private const string CageGnawerKey = "MIN-R33";
        private const string FoodHoardingHyenaKey = "MIN-R34";
        private const string DeathChasingRoadrunnerKey = "MIN-R35";
        private const string TyrantScorpionKey = "MIN-R36";
        private const string RescueBotKey = "MIN-R24";
        private const string RepairJobCardNumber = "133711";
        private const string DeliciousLobsterGrowthCounter = "season14_min_r30_lobster_growth";

        [Test]
        public void EmbeddedCatalog_DefinesBeastGoldenCards()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;

            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == FlutteringBatKey), 2, 6, "两只1/1");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == DeliciousLobsterKey), 2, 2, "+2/+2");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == WolfPupKey), 6, 10, "+8/+4");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == HeadhunterGryphonKey), 6, 10, "2张野兽");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == CageGnawerKey), 4, 14, "+4/+2");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == FoodHoardingHyenaKey), 10, 12, "金色美味龙虾");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == DeathChasingRoadrunnerKey), 20, 22, "触发两次");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == TyrantScorpionKey), 14, 16, "+6/+6");
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void FlutteringBat_RallySummonsOneOrTwoBeasts(bool golden, int expectedSummons)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, FlutteringBatKey, "fluttering-bat", golden);
            source.Health = source.MaxHealth = 100;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(Minion("bat-filler", 0, 100, Tribe.None));
            service.State.Opponent.Board.Add(Minion("bat-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            RunOneAttack(service, 6501);

            var summons = service.State.LastResult.FinalPlayerBoard
                .Where(card => card.InstanceId.StartsWith("token-" + source.InstanceId, StringComparison.Ordinal))
                .ToList();
            Assert.AreEqual(expectedSummons, summons.Count);
            Assert.IsTrue(summons.All(card => card.Attack == 1 && card.MaxHealth == 1 && card.Tribes.Contains(Tribe.Beast)));
        }

        [TestCase(false, 1, 2)]
        [TestCase(true, 2, 3)]
        public void DeliciousLobster_PlayerGrowthPersistsIntoTheNextCombat(bool golden, int firstBuff, int secondBuff)
        {
            var service = CreateService();
            var firstA = Minion("lobster-first-a", 3, 20, Tribe.Beast);
            var firstB = Minion("lobster-first-b", 4, 20, Tribe.Beast);
            ConfigurePlayerLobsterDeath(service, golden, "lobster-first", firstA, firstB);

            RunOneAttack(service, 6502);

            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, firstA, firstBuff, firstBuff);
            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, firstB, firstBuff, firstBuff);
            Assert.AreEqual(golden ? 2 : 1, PlayerGrowth(service));

            ResetCombat(service);
            var secondA = Minion("lobster-second-a", 5, 20, Tribe.Beast);
            var secondB = Minion("lobster-second-b", 6, 20, Tribe.Beast);
            ConfigurePlayerLobsterDeath(service, false, "lobster-second", secondA, secondB);

            RunOneAttack(service, 6503);

            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, secondA, secondBuff, secondBuff);
            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, secondB, secondBuff, secondBuff);
        }

        [Test]
        public void DeliciousLobster_OpponentGrowthPersistsAndLoadsIntoTheNextSimulation()
        {
            var service = CreateService();
            var firstA = Minion("opponent-lobster-first-a", 3, 20, Tribe.Beast, BoardSide.Opponent);
            var firstB = Minion("opponent-lobster-first-b", 4, 20, Tribe.Beast, BoardSide.Opponent);
            ConfigureOpponentLobsterDeath(service, "opponent-lobster-first", firstA, firstB);

            RunOneAttack(service, 6504);

            AssertCombatBuff(service.State.LastResult.FinalOpponentBoard, firstA, 1, 1);
            AssertCombatBuff(service.State.LastResult.FinalOpponentBoard, firstB, 1, 1);
            Assert.AreEqual(1, OpponentGrowth(service));
            Assert.AreEqual(1, service.GetOpponentCombatTavernStatePreview().AdvancedMechanics.Counters[DeliciousLobsterGrowthCounter]);

            ResetCombat(service);
            var secondA = Minion("opponent-lobster-second-a", 5, 20, Tribe.Beast, BoardSide.Opponent);
            var secondB = Minion("opponent-lobster-second-b", 6, 20, Tribe.Beast, BoardSide.Opponent);
            ConfigureOpponentLobsterDeath(service, "opponent-lobster-second", secondA, secondB);

            RunOneAttack(service, 6505);

            AssertCombatBuff(service.State.LastResult.FinalOpponentBoard, secondA, 2, 2);
            AssertCombatBuff(service.State.LastResult.FinalOpponentBoard, secondB, 2, 2);
            Assert.AreEqual(2, OpponentGrowth(service));
        }

        [TestCase(false, 4, 2)]
        [TestCase(true, 8, 4)]
        public void WolfPup_RallyBuffsOtherMinions(bool golden, int attack, int health)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, WolfPupKey, "wolf-pup", golden);
            source.Health = source.MaxHealth = 100;
            var target = Minion("wolf-pup-target", 2, 20, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(target);
            service.State.Opponent.Board.Add(Minion("wolf-pup-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            RunOneAttack(service, 6506);

            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, target, attack, health);
            var finalSource = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == source.InstanceId);
            Assert.AreEqual(source.BaseAttack, finalSource.Attack);
            Assert.AreEqual(source.MaxHealth, finalSource.MaxHealth);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void HeadhunterGryphon_RallyAddsRandomBeasts(bool golden, int expectedCards)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, HeadhunterGryphonKey, "headhunter-gryphon", golden);
            source.Health = source.MaxHealth = 100;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(Minion("gryphon-filler", 0, 100, Tribe.None));
            service.State.Opponent.Board.Add(Minion("gryphon-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            RunOneAttack(service, 6507);

            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Beast)));
        }

        [TestCase(false, 2, 1)]
        [TestCase(true, 4, 2)]
        public void CageGnawer_FriendlyBeastAttackBuffsAllBeasts(bool golden, int attack, int health)
        {
            var service = CreateService();
            var attacker = Minion("cage-gnawer-attacker", 1, 100, Tribe.Beast);
            var source = CreateCatalogMinion(service, CageGnawerKey, "cage-gnawer", golden);
            var nonBeast = Minion("cage-gnawer-non-beast", 3, 20, Tribe.Demon);
            service.State.Player.Board.Add(attacker);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(nonBeast);
            service.State.Opponent.Board.Add(Minion("cage-gnawer-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            RunOneAttack(service, 6508);

            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, attacker, attack, health);
            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, source, attack, health);
            AssertCombatBuff(service.State.LastResult.FinalPlayerBoard, nonBeast, 0, 0);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FoodHoardingHyena_RallySummonsDeliciousLobster(bool golden)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, FoodHoardingHyenaKey, "food-hoarding-hyena", golden);
            source.Health = source.MaxHealth = 100;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(Minion("hyena-filler", 0, 100, Tribe.None));
            service.State.Opponent.Board.Add(Minion("hyena-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            RunOneAttack(service, 6509);

            var lobster = service.State.LastResult.FinalPlayerBoard.Single(card => card.CardId == "BG36_202");
            Assert.AreEqual(golden, lobster.Golden);
            Assert.AreEqual(golden ? 2 : 1, lobster.Attack);
            Assert.AreEqual(golden ? 2 : 1, lobster.MaxHealth);
            Assert.IsTrue(lobster.Keywords.Contains(Keyword.Taunt));
            Assert.IsTrue(lobster.Keywords.Contains(Keyword.Deathrattle));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void DeathChasingRoadrunner_RallyAttackTriggersLeftmostDeathrattle(bool golden, int expectedRepairJobs)
        {
            var service = CreateService();
            var attacker = CreateCatalogMinion(service, FlutteringBatKey, "roadrunner-attacker", false);
            attacker.Health = attacker.MaxHealth = 100;
            var deathrattle = CreateCatalogMinion(service, RescueBotKey, "roadrunner-deathrattle", false);
            var source = CreateCatalogMinion(service, DeathChasingRoadrunnerKey, "death-chasing-roadrunner", golden);
            service.State.Player.Board.Add(attacker);
            service.State.Player.Board.Add(deathrattle);
            service.State.Player.Board.Add(source);
            service.State.Opponent.Board.Add(Minion("roadrunner-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            RunOneAttack(service, 6510);

            Assert.AreEqual(expectedRepairJobs, service.State.Player.Tavern.Hand.Count(card => card.CardId == RepairJobCardNumber));
        }

        [TestCase(false, 5)]
        [TestCase(true, 8)]
        public void TyrantScorpion_FriendlyAttackImprovesBeetlesForTheGame(bool golden, int expectedStats)
        {
            var service = CreateService();
            service.State.Player.Board.Add(Minion("scorpion-attacker", 1, 100, Tribe.Beast));
            service.State.Player.Board.Add(CreateCatalogMinion(service, TyrantScorpionKey, "tyrant-scorpion", golden));
            service.State.Opponent.Board.Add(Minion("scorpion-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            RunOneAttack(service, 6511);

            Assert.AreEqual(expectedStats, service.State.Player.Tavern.BeetleAttackBonus);
            Assert.AreEqual(expectedStats, service.State.Player.Tavern.BeetleHealthBonus);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void TyrantScorpion_DeathrattleSummonsBeetles(bool golden, int expectedBeetles)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, TyrantScorpionKey, "tyrant-scorpion-deathrattle", golden);
            source.Attack = 0;
            source.Health = source.MaxHealth = 1;
            service.State.Player.Board.Add(source);
            service.State.Opponent.Board.Add(Minion("scorpion-killer", 20, 100, Tribe.None, BoardSide.Opponent));
            service.State.Opponent.Board.Add(Minion("scorpion-killer-filler", 0, 100, Tribe.None, BoardSide.Opponent));

            RunOneAttack(service, 6512);

            var beetles = service.State.LastResult.FinalPlayerBoard.Where(card => card.Name == "Beetle").ToList();
            Assert.AreEqual(expectedBeetles, beetles.Count);
            Assert.IsTrue(beetles.All(card => card.Attack == 2 && card.MaxHealth == 2));
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
            service.State.ActiveTribes = new List<Tribe> { Tribe.Beast, Tribe.Mech, Tribe.Demon };
            ResetCombat(service);
            return service;
        }

        private static void ConfigurePlayerLobsterDeath(
            MatchService service,
            bool golden,
            string suffix,
            MinionInstance first,
            MinionInstance second)
        {
            var source = CreateCatalogMinion(service, DeliciousLobsterKey, suffix, golden);
            source.Attack = 0;
            source.Health = source.MaxHealth = 1;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);
            AddKillerSide(service.State.Opponent.Board, BoardSide.Opponent, suffix);
        }

        private static void ConfigureOpponentLobsterDeath(
            MatchService service,
            string suffix,
            MinionInstance first,
            MinionInstance second)
        {
            var source = CreateCatalogMinion(service, DeliciousLobsterKey, suffix, false, BoardSide.Opponent);
            source.Attack = 0;
            source.Health = source.MaxHealth = 1;
            service.State.Opponent.Board.Add(source);
            service.State.Opponent.Board.Add(first);
            service.State.Opponent.Board.Add(second);
            AddKillerSide(service.State.Player.Board, BoardSide.Player, suffix);
        }

        private static void AddKillerSide(List<MinionInstance> board, BoardSide owner, string suffix)
        {
            board.Add(Minion(suffix + "-killer", 20, 100, Tribe.None, owner));
            board.Add(Minion(suffix + "-killer-filler-1", 0, 100, Tribe.None, owner));
            board.Add(Minion(suffix + "-killer-filler-2", 0, 100, Tribe.None, owner));
            board.Add(Minion(suffix + "-killer-filler-3", 0, 100, Tribe.None, owner));
        }

        private static void ResetCombat(MatchService service)
        {
            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Hand.Clear();
            service.State.DelayedObjectStates.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
        }

        private static int PlayerGrowth(MatchService service)
        {
            return service.State.Player.Tavern.AdvancedMechanics.Counters.TryGetValue(DeliciousLobsterGrowthCounter, out var value) ? value : 0;
        }

        private static int OpponentGrowth(MatchService service)
        {
            return service.State.Opponent.AdvancedMechanics.Counters.TryGetValue(DeliciousLobsterGrowthCounter, out var value) ? value : 0;
        }

        private static void RunOneAttack(MatchService service, int seed)
        {
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = seed, SafetyLimit = 1 }));
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

        private static MinionInstance CreateCatalogMinion(
            MatchService service,
            string researchKey,
            string suffix,
            bool golden,
            BoardSide owner = BoardSide.Player)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == researchKey);
            return MinionFactory.Create(definition, owner, suffix, golden, PoolSource.Copy, 0);
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
