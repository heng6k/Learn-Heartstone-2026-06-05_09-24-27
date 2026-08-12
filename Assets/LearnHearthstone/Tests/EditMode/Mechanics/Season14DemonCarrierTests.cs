using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14DemonCarrierTests
    {
        private const string TrappedDemonKey = "MIN-R37";
        private const string DevilishDistractorKey = "MIN-R38";
        private const string GhostlyIllusionistKey = "MIN-R39";
        private const string NimbleEscapeeKey = "MIN-R40";
        private const string EredarEscapeMasterKey = "MIN-R41";
        private const string NimbleEscapeeActionId = "activate:min-r40";
        private const string DemonFodderCardId = "DEMON_FODDER";
        private const string MethodicalMadnessCardNumber = "132903";
        private const string HastyExcavationCardId = "104559";
        private const string SoulRewinderCardId = "BG26_174";

        [Test]
        public void EmbeddedCatalog_DefinesDemonGoldenCardsAndActivate()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;

            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == TrappedDemonKey), 4, 4, "两个恶魔饲料");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == DevilishDistractorKey), 6, 12, "+4/+4");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == GhostlyIllusionistKey), 12, 6, "4 张理性癫狂");
            var escapee = minions.Single(item => item.ResearchKey == NimbleEscapeeKey);
            AssertPreviewCarrier(escapee, 16, 16, "+16/+16");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == EredarEscapeMasterKey), 10, 10, "两张");

            var action = escapee.RecruitActions.Single();
            Assert.AreEqual(NimbleEscapeeActionId, action.ActionId);
            Assert.AreEqual("season14.activate.min-r40@1", action.ResolverId);
            Assert.AreEqual(1, action.CostSpec.Gold);
            Assert.AreEqual(RecruitActionTargetSpec.None, action.TargetSpec);
            Assert.AreEqual(1, action.UsesPerTurn);
            Assert.AreEqual(MatchPhase.Tavern, action.AllowedPhase);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void TrappedDemon_QueuesFodderForExactlyTheNextThreeRefreshes(bool golden, int expectedPerRefresh)
        {
            var service = CreateService();
            ConfigureDeath(service, TrappedDemonKey, "trapped-demon", golden);

            RunOneAttack(service, golden ? 6702 : 6701);

            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Tavern.FreeRefreshes = 4;
            for (var refresh = 0; refresh < 4; refresh += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
                var actual = service.State.Player.Tavern.Shop.Count(card => card?.CardId == DemonFodderCardId);
                Assert.AreEqual(refresh < 3 ? expectedPerRefresh : 0, actual, "refresh " + (refresh + 1));
            }
        }

        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void DevilishDistractor_TargetedSpellBuffsCurrentTavernMinionsForTheGame(bool golden, int expectedBuff)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, DevilishDistractorKey, "devilish-distractor", golden);
            var first = Minion("distractor-shop-first", "DISTRACTOR_SHOP_FIRST", 2, 3, Tribe.Demon);
            var second = Minion("distractor-shop-second", "DISTRACTOR_SHOP_SECOND", 5, 7, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Shop = new List<MinionInstance> { first, second };
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "100596", CardKind.TavernSpell));

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified));

            Assert.AreEqual(first.BaseAttack + expectedBuff, first.Attack);
            Assert.AreEqual(first.BaseHealth + expectedBuff, first.MaxHealth);
            Assert.AreEqual(second.BaseAttack + expectedBuff, second.Attack);
            Assert.AreEqual(second.BaseHealth + expectedBuff, second.MaxHealth);
        }

        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void GhostlyIllusionist_DeathrattleAddsMethodicalMadness(bool golden, int expectedCards)
        {
            var service = CreateService();
            ConfigureDeath(service, GhostlyIllusionistKey, "ghostly-illusionist", golden);

            RunOneAttack(service, golden ? 6704 : 6703);

            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count(card => card.CardId == MethodicalMadnessCardNumber));
        }

        [TestCase(false, 8)]
        [TestCase(true, 16)]
        public void NimbleEscapee_ActivateBuffsEveryTavernMinionAndAddsAnAllowedKeyword(bool golden, int expectedBuff)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, NimbleEscapeeKey, "nimble-escapee", golden);
            var first = Minion("escapee-shop-first", "ESCAPEE_SHOP_FIRST", 2, 3, Tribe.Demon);
            var second = Minion("escapee-shop-second", "ESCAPEE_SHOP_SECOND", 5, 7, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Shop = new List<MinionInstance> { first, second };
            service.State.Player.Tavern.Gold = 5;

            Activate(service, NimbleEscapeeActionId, source);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
            AssertEscapeeBuff(first, expectedBuff);
            AssertEscapeeBuff(second, expectedBuff);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void EredarEscapeMaster_AccumulatesActualHeroDamageAndAddsStatTavernSpells(bool golden, int expectedCards)
        {
            var service = CreateService();
            service.State.Player.Health = 30;
            service.State.Player.Board.Add(CreateCatalogMinion(service, EredarEscapeMasterKey, "eredar-escape-master", golden));

            BuyHastyExcavation(service);
            Assert.AreEqual(27, service.State.Player.Health);
            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count(card => card.CardId != HastyExcavationCardId));
            service.State.Player.Tavern.Hand.Clear();

            BuyHastyExcavation(service);

            Assert.AreEqual(24, service.State.Player.Health);
            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count(card => card.CardId != HastyExcavationCardId));
        }

        [Test]
        public void EredarEscapeMaster_DoesNotCountDamagePreventedBySoulRewinder()
        {
            var service = CreateService();
            service.State.Player.Health = 30;
            service.State.Player.Board.Add(CreateCatalogMinion(service, EredarEscapeMasterKey, "eredar-prevented", false));
            service.State.Player.Board.Add(Minion("soul-rewinder", SoulRewinderCardId, 3, 1, Tribe.Demon));

            BuyHastyExcavation(service);

            Assert.AreEqual(30, service.State.Player.Health);
            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count(card => card.CardId != HastyExcavationCardId));
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
            service.State.ActiveTribes = new List<Tribe> { Tribe.Demon, Tribe.Beast };
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.DelayedObjectStates.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
            return service;
        }

        private static void ConfigureDeath(
            MatchService service,
            string researchKey,
            string suffix,
            bool golden)
        {
            var source = CreateCatalogMinion(service, researchKey, suffix, golden);
            source.Attack = 0;
            source.Health = source.MaxHealth = 1;
            service.State.Player.Board.Add(source);
            service.State.Opponent.Board.Add(Minion(suffix + "-killer", suffix + "-killer", 20, 100, Tribe.None, BoardSide.Opponent));
            service.State.Opponent.Board.Add(Minion(suffix + "-filler", suffix + "-filler", 0, 100, Tribe.None, BoardSide.Opponent));
        }

        private static void RunOneAttack(MatchService service, int seed)
        {
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = seed, SafetyLimit = 1 }));
        }

        private static void BuyHastyExcavation(MatchService service)
        {
            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, HastyExcavationCardId, CardKind.TavernSpell));
            var hasty = service.State.Player.Tavern.Hand.Single();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop = new List<MinionInstance> { hasty };
            service.State.Player.Tavern.Gold = 0;
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
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
            BoardSide owner = BoardSide.Player)
        {
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
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>(),
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
        }

        private static void AssertEscapeeBuff(MinionInstance minion, int expectedBuff)
        {
            Assert.AreEqual(minion.BaseAttack + expectedBuff, minion.Attack);
            Assert.AreEqual(minion.BaseHealth + expectedBuff, minion.MaxHealth);
            var allowed = new[] { Keyword.Taunt, Keyword.DivineShield, Keyword.Windfury };
            Assert.AreEqual(1, allowed.Count(minion.Keywords.Contains));
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
