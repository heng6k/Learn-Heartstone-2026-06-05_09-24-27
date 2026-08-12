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
    public sealed class Season14MurlocCarrierTests
    {
        private const string JailbreakMastermindKey = "MIN-R42";
        private const string TwilightTidehunterKey = "MIN-R43";
        private const string ShamanTidecallerKey = "MIN-R44";
        private const string JailbreakMastermindActionId = "activate:min-r42";

        [Test]
        public void EmbeddedCatalog_DefinesMurlocGoldenCardsAndActivate()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;

            var mastermind = minions.Single(item => item.ResearchKey == JailbreakMastermindKey);
            AssertPreviewCarrier(mastermind, 4, 10, "2张鱼人牌");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == TwilightTidehunterKey), 6, 12, "+12/+12");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == ShamanTidecallerKey), 10, 14, "+6/+6");

            var action = mastermind.RecruitActions.Single();
            Assert.AreEqual(JailbreakMastermindActionId, action.ActionId);
            Assert.AreEqual("season14.activate.min-r42@1", action.ResolverId);
            Assert.AreEqual(2, action.CostSpec.Gold);
            Assert.AreEqual(RecruitActionTargetSpec.None, action.TargetSpec);
            Assert.AreEqual(1, action.UsesPerTurn);
            Assert.AreEqual(MatchPhase.Tavern, action.AllowedPhase);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void JailbreakMastermind_ActivateAddsRandomMurlocs(bool golden, int expectedCards)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, JailbreakMastermindKey, "jailbreak-mastermind", golden);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Gold = 5;

            Activate(service, JailbreakMastermindActionId, source);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.Minion && HasTribe(card, Tribe.Murloc)));
        }

        [TestCase(false, 6)]
        [TestCase(true, 12)]
        public void TwilightTidehunter_TargetedSpellBuffsLeftmostHandMinion(bool golden, int expectedBuff)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, TwilightTidehunterKey, "twilight-tidehunter", golden);
            var left = Minion("tidehunter-left", "TIDEHUNTER_LEFT", 2, 3, Tribe.Beast);
            var right = Minion("tidehunter-right", "TIDEHUNTER_RIGHT", 5, 7, Tribe.Murloc);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Hand.Add(left);
            service.State.Player.Tavern.Hand.Add(right);
            var spellIndex = AddPointyArrow(service);

            CastPointyArrow(service, spellIndex, 0);

            Assert.AreEqual(left.BaseAttack + expectedBuff, left.Attack);
            Assert.AreEqual(left.BaseHealth + expectedBuff, left.MaxHealth);
            Assert.AreEqual(right.BaseAttack, right.Attack);
            Assert.AreEqual(right.BaseHealth, right.MaxHealth);
        }

        [TestCase(false, 3)]
        [TestCase(true, 6)]
        public void ShamanTidecaller_SpellOnMurlocBuffsMurlocsInHandAndWarband(bool golden, int expectedBuff)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, ShamanTidecallerKey, "shaman-tidecaller", golden);
            var boardMurloc = Minion("tidecaller-board-murloc", "TIDECALLER_BOARD_MURLOC", 2, 3, Tribe.Murloc);
            var boardBeast = Minion("tidecaller-board-beast", "TIDECALLER_BOARD_BEAST", 5, 7, Tribe.Beast);
            var handMurloc = Minion("tidecaller-hand-murloc", "TIDECALLER_HAND_MURLOC", 4, 6, Tribe.Murloc);
            var handBeast = Minion("tidecaller-hand-beast", "TIDECALLER_HAND_BEAST", 8, 9, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(boardMurloc);
            service.State.Player.Board.Add(boardBeast);
            service.State.Player.Tavern.Hand.Add(handMurloc);
            service.State.Player.Tavern.Hand.Add(handBeast);
            var spellIndex = AddPointyArrow(service);

            CastPointyArrow(service, spellIndex, 1);

            AssertStats(source, expectedBuff, expectedBuff);
            AssertStats(boardMurloc, expectedBuff + 4, expectedBuff);
            AssertStats(handMurloc, expectedBuff, expectedBuff);
            AssertStats(boardBeast, 0, 0);
            AssertStats(handBeast, 0, 0);
        }

        [Test]
        public void ShamanTidecaller_SpellOnNonMurlocDoesNotTrigger()
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, ShamanTidecallerKey, "shaman-tidecaller-negative", false);
            var boardBeast = Minion("tidecaller-negative-beast", "TIDECALLER_NEGATIVE_BEAST", 5, 7, Tribe.Beast);
            var handMurloc = Minion("tidecaller-negative-hand", "TIDECALLER_NEGATIVE_HAND", 4, 6, Tribe.Murloc);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(boardBeast);
            service.State.Player.Tavern.Hand.Add(handMurloc);
            var spellIndex = AddPointyArrow(service);

            CastPointyArrow(service, spellIndex, 1);

            AssertStats(source, 0, 0);
            AssertStats(boardBeast, 4, 0);
            AssertStats(handMurloc, 0, 0);
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
            service.State.ActiveTribes = new List<Tribe> { Tribe.Murloc, Tribe.Beast };
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
            return service;
        }

        private static int AddPointyArrow(MatchService service)
        {
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "100596", CardKind.TavernSpell));
            return service.State.Player.Tavern.Hand.Count - 1;
        }

        private static void CastPointyArrow(MatchService service, int spellIndex, int targetIndex)
        {
            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                spellIndex,
                targetIndex,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified));
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
            Tribe tribe)
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
                Owner = BoardSide.Player,
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

        private static bool HasTribe(MinionInstance minion, Tribe tribe)
        {
            return BoardTribeAnalyzer.HasTribe(minion, tribe);
        }

        private static void AssertStats(MinionInstance minion, int attack, int health)
        {
            Assert.AreEqual(minion.BaseAttack + attack, minion.Attack);
            Assert.AreEqual(minion.BaseHealth + health, minion.MaxHealth);
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
