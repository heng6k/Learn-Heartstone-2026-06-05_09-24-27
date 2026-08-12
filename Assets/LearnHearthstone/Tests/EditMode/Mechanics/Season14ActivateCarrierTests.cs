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
    public sealed class Season14ActivateCarrierTests
    {
        private const string KelpKeeperResearchKey = "ACT-R01N";
        private const string PrivateInvestigatorResearchKey = "ACT-R02N";
        private const string SoulkeepingJailerResearchKey = "ACT-R03N";
        private const string KelpKeeperActionId = "activate:kelp-keeper";
        private const string PrivateInvestigatorActionId = "activate:private-investigator";
        private const string SoulkeepingJailerActionId = "activate:soulkeeping-jailer";
        private const string PrivateInvestigatorResolverId = "season14.activate.private-investigator@1";

        [Test]
        public void EmbeddedCatalog_DefinesOfficialActivateCostsTargetsAndResolvers()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").English.Minions.All;

            AssertAction(minions.Single(item => item.ResearchKey == KelpKeeperResearchKey),
                KelpKeeperActionId, "season14.activate.kelp-keeper@1", 1, RecruitActionTargetSpec.OtherFriendlyBoardMinion);
            AssertAction(minions.Single(item => item.ResearchKey == PrivateInvestigatorResearchKey),
                PrivateInvestigatorActionId, PrivateInvestigatorResolverId, 1, RecruitActionTargetSpec.None);
            AssertAction(minions.Single(item => item.ResearchKey == SoulkeepingJailerResearchKey),
                SoulkeepingJailerActionId, "season14.activate.soulkeeping-jailer@1", 2, RecruitActionTargetSpec.None);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void KelpKeeper_TriggersAnotherFriendlyMinionsBattlecry(bool golden, int expectedBonusGold)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, KelpKeeperResearchKey, "kelp", golden);
            var target = Minion("busker", "BG26_135", 1, 1, Tribe.Pirate, Keyword.Battlecry);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 5;

            Activate(service, KelpKeeperActionId, source, target);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
            Assert.AreEqual(expectedBonusGold, service.State.Player.Tavern.NextTurnBonusGold);
        }

        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void PrivateInvestigator_BanksOfficialNextTurnGold(bool golden, int expectedBonusGold)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, PrivateInvestigatorResearchKey, "investigator", golden);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Gold = 5;

            Activate(service, PrivateInvestigatorActionId, source);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
            Assert.AreEqual(expectedBonusGold, service.State.Player.Tavern.NextTurnBonusGold);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void SoulkeepingJailer_EachFriendlyDemonConsumesOneRandomTavernMinion(bool golden, int statMultiplier)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, SoulkeepingJailerResearchKey, "jailer", golden);
            var otherDemon = Minion("other-demon", "TEST_OTHER_DEMON", 2, 3, Tribe.Demon);
            var nonDemon = Minion("non-demon", "TEST_NON_DEMON", 7, 7, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(otherDemon);
            service.State.Player.Board.Add(nonDemon);
            service.State.Player.Tavern.Shop.Add(Minion("shop-a", "TEST_SHOP_A", 2, 3, Tribe.None));
            service.State.Player.Tavern.Shop.Add(Minion("shop-b", "TEST_SHOP_B", 5, 7, Tribe.None));
            service.State.Player.Tavern.Gold = 5;
            var demonAttackBefore = source.Attack + otherDemon.Attack;
            var demonHealthBefore = source.MaxHealth + otherDemon.MaxHealth;

            Activate(service, SoulkeepingJailerActionId, source);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            Assert.AreEqual(demonAttackBefore + 7 * statMultiplier, source.Attack + otherDemon.Attack);
            Assert.AreEqual(demonHealthBefore + 10 * statMultiplier, source.MaxHealth + otherDemon.MaxHealth);
            Assert.AreEqual(7, nonDemon.Attack);
            Assert.AreEqual(7, nonDemon.MaxHealth);
            Assert.AreEqual(2, service.State.Player.Tavern.Shop.Count(item => item == null));
        }

        [Test]
        public void PreviewBuiltIns_DoNotOverwriteInjectedResolverWithTheSameId()
        {
            var registry = new RecruitActionResolverRegistry();
            registry.Register(PrivateInvestigatorResolverId, context => RecruitActionResolution.Success(
                state => state.Player.Tavern.NextTurnBonusGold += 99));
            var service = CreateService(registry);
            var source = CreateCatalogMinion(service, PrivateInvestigatorResearchKey, "custom", false);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Gold = 5;

            Activate(service, PrivateInvestigatorActionId, source);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(99, service.State.Player.Tavern.NextTurnBonusGold);
        }

        private static MatchService CreateService(RecruitActionResolverRegistry registry = null)
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
                },
                recruitActionResolvers: registry);
            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
            service.State.Player.Tavern.NextTurnBonusGold = 0;
            return service;
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

        private static void Activate(
            MatchService service,
            string actionId,
            MinionInstance source,
            MinionInstance target = null)
        {
            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = actionId,
                SourceInstanceId = source.InstanceId,
                TargetInstanceId = target?.InstanceId,
                TargetZone = target == null ? TargetZone.Unspecified : TargetZone.FriendlyBoard
            }));
        }

        private static void AssertAction(
            MinionDefinition minion,
            string actionId,
            string resolverId,
            int gold,
            RecruitActionTargetSpec targetSpec)
        {
            var action = minion.RecruitActions.Single();
            Assert.AreEqual(actionId, action.ActionId);
            Assert.AreEqual(resolverId, action.ResolverId);
            Assert.AreEqual(gold, action.CostSpec.Gold);
            Assert.AreEqual(targetSpec, action.TargetSpec);
            Assert.AreEqual(1, action.UsesPerTurn);
            Assert.AreEqual(MatchPhase.Tavern, action.AllowedPhase);
        }

        private static MinionInstance Minion(
            string instanceId,
            string cardId,
            int attack,
            int health,
            Tribe tribe,
            Keyword keyword = Keyword.Trigger)
        {
            var keywords = keyword == Keyword.Trigger
                ? new List<Keyword>()
                : new List<Keyword> { keyword };
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = instanceId,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords,
                OfficialKeywords = new List<Keyword>(keywords),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>()
            };
        }
    }
}
