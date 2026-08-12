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
    public sealed class Season14NagaCarrierTests
    {
        private const string EscapingNagaKey = "MIN-R04";
        private const string AlertSpellcasterKey = "MIN-R05";
        private const string SurgingDestroyerKey = "MIN-R06";

        [Test]
        public void EmbeddedCatalog_DefinesNagaActionAndGoldenCards()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;
            var escapingNaga = minions.Single(item => item.ResearchKey == EscapingNagaKey);
            var spellcaster = minions.Single(item => item.ResearchKey == AlertSpellcasterKey);
            var destroyer = minions.Single(item => item.ResearchKey == SurgingDestroyerKey);

            var action = spellcaster.RecruitActions.Single();
            Assert.AreEqual("activate:min-r05", action.ActionId);
            Assert.AreEqual("season14.activate.min-r05@1", action.ResolverId);
            Assert.AreEqual(1, action.CostSpec.Gold);
            Assert.AreEqual(RecruitActionTargetSpec.None, action.TargetSpec);
            Assert.AreEqual(1, action.UsesPerTurn);
            Assert.AreEqual(MatchPhase.Tavern, action.AllowedPhase);

            AssertPreviewCarrier(escapingNaga, 6, 4, "+2 生命值");
            AssertPreviewCarrier(spellcaster, 12, 8, "随机施放 2 个酒馆法术");
            AssertPreviewCarrier(destroyer, 6, 6, "+6/+6");
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void EscapingNaga_WhenTargetedBySpellGainsExtraHealth(bool golden, int expectedExtraHealth)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, EscapingNagaKey, "escaping-naga", golden);
            service.State.Player.Board.Add(source);
            AddBananaPlatter(service);
            var attack = source.Attack;
            var health = source.MaxHealth;

            PlayHandSpell(service, 0);

            Assert.AreEqual(attack + 2, source.Attack);
            Assert.AreEqual(health + 2 + expectedExtraHealth, source.MaxHealth);
            Assert.AreEqual(1, service.State.Player.Tavern.TavernSpellsCastThisTurn);
        }

        [TestCase(false, 3)]
        [TestCase(true, 6)]
        public void SurgingDestroyer_WhenSpellTargetsNagaBuffsWarband(bool golden, int expectedBuff)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, SurgingDestroyerKey, "surging-destroyer", golden);
            var target = Minion("naga-target", 2, 4, Tribe.Naga);
            var filler = Minion("beast-filler", 5, 7, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(filler);
            AddBananaPlatter(service);
            var sourceAttack = source.Attack;
            var sourceHealth = source.MaxHealth;
            var fillerAttack = filler.Attack;
            var fillerHealth = filler.MaxHealth;

            PlayHandSpell(service, 1);

            Assert.AreEqual(sourceAttack + expectedBuff, source.Attack);
            Assert.AreEqual(sourceHealth + expectedBuff, source.MaxHealth);
            Assert.AreEqual(fillerAttack + expectedBuff, filler.Attack);
            Assert.AreEqual(fillerHealth + expectedBuff, filler.MaxHealth);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void AlertSpellcaster_ActivateCastsRandomTavernSpells(bool golden, int expectedCasts)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, AlertSpellcasterKey, "alert-spellcaster", golden);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Tier = 1;
            service.State.Player.Tavern.Gold = 3;

            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = "activate:min-r05",
                SourceInstanceId = source.InstanceId
            }));

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(2, service.State.Player.Tavern.Gold);
            Assert.AreEqual(expectedCasts, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(expectedCasts, service.State.Player.Tavern.TavernSpellsCastThisGame);
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
            service.State.RecruitActionStates.Clear();
            return service;
        }

        private static MinionInstance CreateCatalogMinion(MatchService service, string researchKey, string suffix, bool golden)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == researchKey);
            return MinionFactory.Create(definition, BoardSide.Player, suffix, golden, PoolSource.Copy, 0);
        }

        private static MinionInstance Minion(string instanceId, int attack, int health, Tribe tribe)
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
                CanAttack = true,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
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
                "naga-test-banana"));
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
            StringAssert.Contains(goldenText, minion.Golden.Text);
        }
    }
}
