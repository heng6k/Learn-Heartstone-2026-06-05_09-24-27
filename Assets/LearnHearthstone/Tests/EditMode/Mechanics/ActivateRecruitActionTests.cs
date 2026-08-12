using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class ActivateRecruitActionTests
    {
        private const string ActionId = "activate:representative-buff";
        private const string ResolverId = "season14.activate.representative-buff@1";
        private const string SourceCardId = "TEST_ACTIVATE_SOURCE";

        [Test]
        public void Command_ResolvesByResolverIdPaysGoldBuffsTargetAndRecordsTurnUse()
        {
            var registry = new RecruitActionResolverRegistry();
            registry.Register(ResolverId, context => RecruitActionResolution.Success(
                state =>
                {
                    var target = state.Player.Board.First(item => item.InstanceId == context.Target.InstanceId);
                    target.Attack += 3;
                    target.Health += 3;
                    target.MaxHealth += 3;
                },
                new[] { "representative Activate target gained +3/+3" }));
            var service = CreateService(ResolverId, registry);
            var source = Minion("activate-source", SourceCardId, 1, 1);
            var target = Minion("activate-target", "TEST_ACTIVATE_TARGET", 2, 2);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 5;

            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = ActionId,
                SourceInstanceId = source.InstanceId,
                TargetInstanceId = target.InstanceId,
                TargetZone = TargetZone.FriendlyBoard
            }));

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(5, target.Health);
            Assert.AreEqual(1, service.State.RecruitActionStates.Single().UsesThisTurn);
            CollectionAssert.AreEqual(
                new[] { "representative Activate target gained +3/+3" },
                service.LastRecruitActionResult.Events);
        }

        [Test]
        public void Command_MissingResolverDoesNotPayOrMutateTarget()
        {
            var service = CreateService("season14.activate.missing@1", new RecruitActionResolverRegistry());
            var source = Minion("activate-source", SourceCardId, 1, 1);
            var target = Minion("activate-target", "TEST_ACTIVATE_TARGET", 2, 2);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 5;

            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = ActionId,
                SourceInstanceId = source.InstanceId,
                TargetInstanceId = target.InstanceId,
                TargetZone = TargetZone.FriendlyBoard
            }));

            Assert.IsFalse(service.LastRecruitActionResult.Succeeded);
            Assert.AreEqual("recruit-action.resolver.not-found", service.LastRecruitActionResult.Code);
            Assert.AreEqual(5, service.State.Player.Tavern.Gold);
            Assert.AreEqual(2, target.Attack);
            Assert.AreEqual(2, target.Health);
            Assert.IsEmpty(service.State.RecruitActionStates);
        }

        private static MatchService CreateService(string resolverId, RecruitActionResolverRegistry registry)
        {
            var baseline = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var definitions = new List<MinionDefinition>(baseline.Catalogs.Minions.All)
            {
                new MinionDefinition
                {
                    Id = SourceCardId,
                    CardId = SourceCardId,
                    RevisionId = SourceCardId + "@1",
                    EffectRevision = SourceCardId + "@1",
                    Name = "Representative Activate Source",
                    TavernTier = 1,
                    BaseAttack = 1,
                    BaseHealth = 1,
                    Tribes = new List<Tribe> { Tribe.None },
                    RecruitActions = new List<RecruitActionDefinition>
                    {
                        new RecruitActionDefinition
                        {
                            ActionId = ActionId,
                            ResolverId = resolverId,
                            CostSpec = new RecruitActionCostSpec { Gold = 1 },
                            TargetSpec = RecruitActionTargetSpec.OtherFriendlyBoardMinion,
                            UsesPerTurn = 1,
                            AllowedPhase = MatchPhase.Tavern
                        }
                    }
                }
            };
            var source = baseline.Catalogs;
            var catalogs = new GameCatalogSet(
                new MinionCatalog(definitions),
                source.Spells,
                source.Heroes,
                source.Trinkets,
                source.Quests,
                source.TimewarpedTavern,
                source.Anomalies,
                source.DarkmoonPrizes);
            var service = MatchService.CreateWithCatalogs(
                catalogs,
                12345,
                new InMemoryTestScenarioRepository(),
                recruitActionResolvers: registry);
            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Board.Clear();
            service.State.RecruitActionStates.Clear();
            return service;
        }

        private static MinionInstance Minion(string instanceId, string cardId, int attack, int health)
        {
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
                Tribes = new List<Tribe> { Tribe.None },
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
