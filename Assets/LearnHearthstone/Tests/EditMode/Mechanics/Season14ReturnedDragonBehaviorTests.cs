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
    public sealed class Season14ReturnedDragonBehaviorTests
    {
        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void ElectricSynthesizer_BattlecryBuffsOtherDragons(bool golden, int bonus)
        {
            var service = CreateService();
            var dragon = Minion("electric-dragon", "ELECTRIC_DRAGON", 5, 7, Tribe.Dragon);
            var beast = Minion("electric-beast", "ELECTRIC_BEAST", 4, 6, Tribe.Beast);
            var source = CreateCatalogMinion(service, "POOL-D25", "electric-synthesizer", golden);
            service.State.Player.Board.Add(dragon);
            service.State.Player.Board.Add(beast);
            service.State.Player.Tavern.Hand.Add(source);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(5 + bonus, dragon.Attack);
            Assert.AreEqual(7 + bonus, dragon.MaxHealth);
            Assert.AreEqual(4, beast.Attack);
            Assert.AreEqual(6, beast.MaxHealth);
            Assert.AreEqual(source.BaseAttack, source.Attack);
            Assert.AreEqual(source.BaseHealth, source.MaxHealth);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void ElectricSynthesizer_StartOfCombatBuffsOtherDragons(bool golden, int bonus)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D25", "electric-combat", golden);
            var dragon = Minion("electric-combat-dragon", "ELECTRIC_COMBAT_DRAGON", 5, 20, Tribe.Dragon);
            var beast = Minion("electric-combat-beast", "ELECTRIC_COMBAT_BEAST", 4, 20, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(dragon);
            service.State.Player.Board.Add(beast);
            AddCombatWall(service, "electric-wall", 0, 100);

            RunOneAttack(service, golden ? 8302 : 8301);

            AssertCombatStats(service, source, source.BaseAttack, source.BaseHealth);
            AssertCombatStats(service, dragon, 5 + bonus, 20 + bonus);
            AssertCombatStats(service, beast, 4, 20);
        }

        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void GlimGuardian_RallyGainsAttack(bool golden, int bonusAttack)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D26A", "glim-guardian", golden);
            source.Health = source.MaxHealth = 20;
            service.State.Player.Board.Add(source);
            AddCombatWall(service, "glim-wall", 0, 100);

            RunOneAttack(service, golden ? 8304 : 8303);

            AssertCombatStats(service, source, source.BaseAttack + bonusAttack, 20);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void ThousandthPaperDrake_BuffsLeftmostDragons(bool golden, int expectedTargets)
        {
            var service = CreateService();
            var first = Minion("paper-first", "PAPER_FIRST", 3, 20, Tribe.Dragon);
            var source = CreateCatalogMinion(service, "POOL-D26B", "paper-source", golden);
            var third = Minion("paper-third", "PAPER_THIRD", 5, 20, Tribe.Dragon);
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(third);
            AddCombatWall(service, "paper-wall", 0, 100);

            RunOneAttack(service, golden ? 8306 : 8305);

            AssertPaperTarget(service, first, true);
            AssertPaperTarget(service, source, expectedTargets == 2);
            AssertPaperTarget(service, third, false);
        }

        [TestCase("BG31_HERO_811t8")]
        [TestCase("BG34_Treasure_990")]
        [TestCase("BG24_004")]
        public void ImmuneWhileAttackingSources_IgnoreDefenderDamage(string cardId)
        {
            var service = CreateService();
            var source = Minion("immune-attacker", cardId, 6, 6, Tribe.Dragon);
            service.State.Player.Board.Add(source);
            AddCombatWall(service, "immune-wall", 50, 100);

            RunOneAttack(service, 8307);

            var final = service.State.LastResult.FinalPlayerBoard.SingleOrDefault(card => card.InstanceId == source.InstanceId);
            Assert.NotNull(final, cardId + " should survive while attacking.");
            Assert.AreEqual(6, final.Health);
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

        private static void AddCombatWall(MatchService service, string instanceId, int attack, int health)
        {
            service.State.Opponent.Board.Add(Minion(instanceId, instanceId, attack, health, Tribe.None, BoardSide.Opponent, Keyword.Taunt));
        }

        private static void RunOneAttack(MatchService service, int seed)
        {
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = seed, SafetyLimit = 1 }));
        }

        private static void AssertCombatStats(MatchService service, MinionInstance original, int attack, int health)
        {
            var actual = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == original.InstanceId);
            Assert.AreEqual(attack, actual.Attack);
            Assert.AreEqual(health, actual.MaxHealth);
        }

        private static void AssertPaperTarget(MatchService service, MinionInstance original, bool expectedBuff)
        {
            var actual = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == original.InstanceId);
            Assert.AreEqual(original.BaseAttack + (expectedBuff ? 1 : 0), actual.Attack);
            Assert.AreEqual(original.BaseHealth + (expectedBuff ? 2 : 0), actual.MaxHealth);
            Assert.AreEqual(expectedBuff, actual.Keywords.Contains(Keyword.Windfury));
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
            Tribe tribe,
            BoardSide owner = BoardSide.Player,
            Keyword keyword = Keyword.Trigger)
        {
            var keywords = keyword == Keyword.Trigger ? new List<Keyword>() : new List<Keyword> { keyword };
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = cardId,
                CardId = cardId,
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
    }
}
