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
    public sealed class Season14UndeadCarrierTests
    {
        private const string DeathbellKey = "MIN-R01";
        private const string BansheeKey = "MIN-R02";
        private const string PhantomKey = "MIN-R03";

        [Test]
        public void EmbeddedCatalog_DefinesUndeadActionAndGoldenCards()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;
            var deathbell = minions.Single(item => item.ResearchKey == DeathbellKey);
            var banshee = minions.Single(item => item.ResearchKey == BansheeKey);
            var phantom = minions.Single(item => item.ResearchKey == PhantomKey);

            var action = deathbell.RecruitActions.Single();
            Assert.AreEqual("activate:deathbell-necromancer", action.ActionId);
            Assert.AreEqual("season14.activate.deathbell-necromancer@1", action.ResolverId);
            Assert.AreEqual(1, action.CostSpec.Gold);
            Assert.AreEqual(RecruitActionTargetSpec.OtherFriendlyBoardMinion, action.TargetSpec);
            Assert.AreEqual(1, action.UsesPerTurn);
            Assert.AreEqual(MatchPhase.Tavern, action.AllowedPhase);

            AssertPreviewCarrier(deathbell, 6, 12, "+8/+8");
            AssertPreviewCarrier(banshee, 14, 14, "+14/+14");
            AssertPreviewCarrier(phantom, 12, 16, "等同于复生随从攻击力");
        }

        [TestCase(false, 4)]
        [TestCase(true, 8)]
        public void DeathbellNecromancer_GivesRebornDestroysTargetAndBuffsItself(bool golden, int expectedBuff)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, DeathbellKey, "deathbell", golden);
            var target = Minion("deathbell-target", BoardSide.Player, 5, 2, Tribe.Undead);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 3;
            var sourceAttack = source.Attack;
            var sourceHealth = source.MaxHealth;

            Activate(service, source, target);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(2, service.State.Player.Tavern.Gold);
            var liveSource = service.State.Player.Board.Single(item => item.InstanceId == source.InstanceId);
            Assert.AreEqual(sourceAttack + expectedBuff, liveSource.Attack);
            Assert.AreEqual(sourceHealth + expectedBuff, liveSource.MaxHealth);
            Assert.IsFalse(service.State.Player.Board.Any(item => item.InstanceId == target.InstanceId));
            var reborn = service.State.Player.Board.Single(item => item.InstanceId.StartsWith(target.InstanceId + "-reborn-"));
            Assert.AreEqual(5, reborn.Attack);
            Assert.AreEqual(1, reborn.Health);
            Assert.IsFalse(reborn.Keywords.Contains(Keyword.Reborn));
        }

        [Test]
        public void DeathbellNecromancer_RejectsNonUndeadTargetWithoutPayment()
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, DeathbellKey, "deathbell-invalid", false);
            var target = Minion("deathbell-beast", BoardSide.Player, 5, 5, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 3;

            Activate(service, source, target);

            Assert.IsFalse(service.LastRecruitActionResult.Succeeded);
            Assert.AreEqual("season14.activate.deathbell-necromancer.target.invalid", service.LastRecruitActionResult.Code);
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            Assert.AreEqual(2, service.State.Player.Board.Count);
            Assert.IsFalse(target.Keywords.Contains(Keyword.Reborn));
        }

        [TestCase(false, 7)]
        [TestCase(true, 14)]
        public void RecruitPhaseReborn_TriggersBansheeAndPhantom(bool golden, int bansheeBuff)
        {
            var service = CreateService();
            var target = Minion("recruit-reborn-target", BoardSide.Player, 5, 2, Tribe.Undead, Keyword.Reborn);
            var banshee = CreateCatalogMinion(service, BansheeKey, "recruit-banshee", golden);
            var phantom = CreateCatalogMinion(service, PhantomKey, "recruit-phantom", golden);
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(banshee);
            service.State.Player.Board.Add(phantom);
            var bansheeAttack = banshee.Attack;
            var bansheeHealth = banshee.MaxHealth;
            var phantomAttack = phantom.Attack;
            var phantomHealth = phantom.MaxHealth;

            CombatEngine.ResolveRecruitPhaseDeath(
                service.State.Player.Board,
                target,
                service.State.Player.Tavern,
                service.State.Player.Tavern.Hand,
                5150,
                "Season14UndeadCarrierTests");

            var liveBanshee = service.State.Player.Board.Single(item => item.InstanceId == banshee.InstanceId);
            var livePhantom = service.State.Player.Board.Single(item => item.InstanceId == phantom.InstanceId);
            Assert.IsTrue(liveBanshee.Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(bansheeAttack + bansheeBuff, liveBanshee.Attack);
            Assert.AreEqual(bansheeHealth + bansheeBuff, liveBanshee.MaxHealth);
            Assert.AreEqual(phantomAttack + 5, livePhantom.Attack);
            Assert.AreEqual(phantomHealth + 5, livePhantom.MaxHealth);
        }

        [Test]
        public void CombatReborn_InvokesBothUndeadTriggers()
        {
            var service = CreateService();
            var target = Minion("combat-reborn-target", BoardSide.Player, 5, 1, Tribe.Undead, Keyword.Taunt, Keyword.Reborn);
            var banshee = CreateCatalogMinion(service, BansheeKey, "combat-banshee", false);
            var phantom = CreateCatalogMinion(service, PhantomKey, "combat-phantom", false);
            var opponent = Minion("combat-opponent", BoardSide.Opponent, 1, 100, Tribe.None);

            var result = CombatEngine.SimulateBasicCombat(
                new[] { target, banshee, phantom },
                new[] { opponent },
                5151,
                safetyLimit: 20);

            Assert.AreEqual(1, result.Log.Count(item => item.Title == "Season14BansheeRebornTriggered"));
            Assert.AreEqual(1, result.Log.Count(item => item.Title == "Season14PhantomRebornTriggered"));
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
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
            return service;
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

        private static void Activate(MatchService service, MinionInstance source, MinionInstance target)
        {
            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = "activate:deathbell-necromancer",
                SourceInstanceId = source.InstanceId,
                TargetInstanceId = target.InstanceId,
                TargetZone = TargetZone.FriendlyBoard
            }));
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
