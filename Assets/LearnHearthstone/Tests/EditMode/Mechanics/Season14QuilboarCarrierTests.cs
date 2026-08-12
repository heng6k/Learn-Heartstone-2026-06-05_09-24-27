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
    public sealed class Season14QuilboarCarrierTests
    {
        private const string InfiltratorKey = "MIN-R07";
        private const string ExcavatorKey = "MIN-R08";
        private const string TrapperKey = "MIN-R09";
        private const string BristlebackKey = "MIN-R10";
        private const string BullyKey = "MIN-R11";
        private const string RuffianKey = "MIN-R12";

        [Test]
        public void EmbeddedCatalog_DefinesQuilboarGoldenCards()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;

            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == InfiltratorKey), 8, 10, "4次免费的刷新");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == ExcavatorKey), 6, 12, "2张抉择牌");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == TrapperKey), 8, 8, "上限提高2枚");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == BristlebackKey), 6, 10, "各使用2张");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == BullyKey), 4, 14, "鲜血宝石两倍");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == RuffianKey), 16, 16, "施放6次");
        }

        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void CunningInfiltrator_FreeRefreshChoice(bool golden, int expectedRefreshes)
        {
            var service = CreateService();
            PlayCatalogMinion(service, InfiltratorKey, "infiltrator-refresh", golden);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(expectedRefreshes, service.State.Player.Tavern.FreeRefreshes);
        }

        [TestCase(false, 3)]
        [TestCase(true, 6)]
        public void CunningInfiltrator_BloodGemChoice(bool golden, int expectedGems)
        {
            var service = CreateService();
            PlayCatalogMinion(service, InfiltratorKey, "infiltrator-gems", golden);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 1));

            Assert.AreEqual(expectedGems, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void SnareTrapper_QuilboarChoice(bool golden, int expectedCards)
        {
            var service = CreateService();
            service.State.Player.Tavern.Tier = 6;
            PlayCatalogMinion(service, TrapperKey, "trapper-card", golden);
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => BoardTribeAnalyzer.HasTribe(card, Tribe.Quilboar)));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void SnareTrapper_CoinCapChoice(bool golden, int expectedIncrease)
        {
            var service = CreateService();
            service.State.Player.Tavern.MaxGold = 10;
            service.State.Player.Tavern.PersistentMaxGoldBonus = 0;
            PlayCatalogMinion(service, TrapperKey, "trapper-cap", golden);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 1));

            Assert.AreEqual(10 + expectedIncrease, service.State.Player.Tavern.MaxGold);
            Assert.AreEqual(expectedIncrease, service.State.Player.Tavern.PersistentMaxGoldBonus);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void VigilantBristleback_WhenTargetedAppliesBloodGemsToNeighbors(bool golden, int expectedGems)
        {
            var service = CreateService();
            var left = Minion("bristle-left", BoardSide.Player, 2, 3, Tribe.Beast);
            var source = CreateCatalogMinion(service, BristlebackKey, "bristle-source", golden);
            var right = Minion("bristle-right", BoardSide.Player, 4, 5, Tribe.Demon);
            service.State.Player.Board.Add(left);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(right);
            AddBananaPlatter(service);

            PlayHandSpell(service, 1);

            Assert.AreEqual(2 + expectedGems, left.Attack);
            Assert.AreEqual(3 + expectedGems, left.MaxHealth);
            Assert.AreEqual(4 + expectedGems, right.Attack);
            Assert.AreEqual(5 + expectedGems, right.MaxHealth);
            Assert.AreEqual(expectedGems, left.Enchantments.Count(StatMath.IsBloodGemEnchantment));
            Assert.AreEqual(expectedGems, right.Enchantments.Count(StatMath.IsBloodGemEnchantment));
        }

        [TestCase(false, 3)]
        [TestCase(true, 6)]
        public void VeteranRuffian_AllMinionsChoice(bool golden, int expectedGems)
        {
            var service = CreateService();
            var filler = Minion("ruffian-filler", BoardSide.Player, 2, 2, Tribe.Mech);
            service.State.Player.Board.Add(filler);
            var source = PlayCatalogMinion(service, RuffianKey, "ruffian-gems", golden);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(2 + expectedGems, filler.Attack);
            Assert.AreEqual(2 + expectedGems, filler.MaxHealth);
            Assert.AreEqual(source.BaseAttack + expectedGems, source.Attack);
            Assert.AreEqual(source.BaseHealth + expectedGems, source.MaxHealth);
        }

        [TestCase(false, 3)]
        [TestCase(true, 6)]
        public void VeteranRuffian_BarrageChoice(bool golden, int expectedCasts)
        {
            var service = CreateService();
            service.State.Player.Board.Add(Minion("ruffian-barrage-filler", BoardSide.Player, 2, 20, Tribe.Mech));
            PlayCatalogMinion(service, RuffianKey, "ruffian-barrage", golden);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 1));

            Assert.AreEqual(expectedCasts, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(expectedCasts, service.State.Player.Tavern.TavernSpellsCastThisGame);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void SpinyExcavator_RallyAddsRandomChooseOneCardsToHand(bool golden, int expectedCards)
        {
            var service = CreateService();
            service.State.Player.Tavern.Tier = 6;
            var source = CreateCatalogMinion(service, ExcavatorKey, "excavator", golden);
            source.Attack = 1;
            source.Health = source.MaxHealth = 100;
            service.State.Player.Board.Add(source);
            service.State.Opponent.Board.Add(Minion("excavator-wall", BoardSide.Opponent, 0, 100, Tribe.None, Keyword.Taunt));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 6201, SafetyLimit = 1 }));

            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(TavernSpellEngine.IsChooseOneCard));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card =>
                card.CardKind == CardKind.Minion || card.CardKind == CardKind.TavernSpell));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void ImprisonedBully_RallySummonsAndImmediatelyAttacksWithBloodGemGolem(bool golden, int multiplier)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, BullyKey, "bully", golden);
            StatMath.ApplyEnchantment(source, new Enchantment
            {
                Id = "bully-blood-gem",
                SourceId = "Blood Gem",
                Kind = EnchantmentKind.BloodGem,
                AttackBonus = 3,
                HealthBonus = 4
            });
            var filler = Minion("bully-filler", BoardSide.Player, 0, 100, Tribe.None);
            var wall = Minion("bully-wall", BoardSide.Opponent, 0, 100, Tribe.None, Keyword.Taunt);

            var result = CombatEngine.SimulateBasicCombat(
                new[] { source, filler },
                new[] { wall },
                6202,
                safetyLimit: 2,
                playerTavern: service.State.Player.Tavern);

            var golem = result.FinalPlayerBoard.Single(card => card.DefinitionId == "blood-golem");
            Assert.AreEqual(3 * multiplier, golem.Attack);
            Assert.AreEqual(4 * multiplier, golem.MaxHealth);
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "ImmediateAttackQueued" && entry.ActorId == golem.InstanceId));
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "TriggeredAttackResolved" && entry.ActorId == golem.InstanceId));
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
            service.State.Player.Tavern.TavernSpellsCastThisTurn = 0;
            service.State.Player.Tavern.TavernSpellsCastThisGame = 0;
            return service;
        }

        private static MinionInstance PlayCatalogMinion(
            MatchService service,
            string researchKey,
            string suffix,
            bool golden)
        {
            var card = CreateCatalogMinion(service, researchKey, suffix, golden);
            service.State.Player.Tavern.Hand.Add(card);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            return service.State.Player.Board.Single(item => item.InstanceId == card.InstanceId);
        }

        private static MinionInstance CreateCatalogMinion(MatchService service, string researchKey, string suffix, bool golden)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == researchKey);
            return MinionFactory.Create(definition, BoardSide.Player, suffix, golden, PoolSource.Copy, 0);
        }

        private static MinionInstance Minion(
            string instanceId,
            BoardSide owner,
            int attack,
            int health,
            Tribe tribe,
            params Keyword[] keywords)
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
                Owner = owner,
                CanAttack = true,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                OfficialKeywords = keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>()
            };
        }

        private static void AddBananaPlatter(MatchService service)
        {
            service.State.Player.Tavern.Hand.Add(MinionFactory.Create(
                service.Catalogs.Spells.GetByCardNumber("105752"),
                BoardSide.Player,
                "quilboar-test-banana"));
        }

        private static void PlayHandSpell(MatchService service, int targetIndex)
        {
            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                targetIndex,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified));
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
            StringAssert.Contains(goldenText, minion.Golden.Text.Replace(" ", string.Empty));
        }
    }
}
