using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class RecruitPhaseActionTests
    {
        [Test]
        public void Execute_ValidActionPaysGoldCommitsResolverAndRecordsUse()
        {
            var state = CreateState();
            var source = Minion("activate-source", 1, 1);
            var target = Minion("friendly-target", 2, 2);
            state.Player.Board.Add(source);
            state.Player.Board.Add(target);
            state.Player.Tavern.Gold = 5;

            var result = RecruitActionService.Execute(
                state,
                Definition(2, RecruitActionTargetSpec.FriendlyBoardMinion),
                Request(source.InstanceId, target.InstanceId, 1, TargetZone.FriendlyBoard),
                context =>
                {
                    Assert.AreNotSame(source, context.Source);
                    Assert.AreNotSame(target, context.Target);
                    Assert.AreEqual(5, context.GoldBefore);
                    return RecruitActionResolution.Success(
                        live => live.Player.Board[1].Attack += 3,
                        new[] { "friendly target gained +3 Attack" });
                });

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(5, result.GoldBefore);
            Assert.AreEqual(3, result.GoldAfter);
            Assert.AreEqual(2, result.GoldSpent);
            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(1, state.RecruitActionStates.Count);
            Assert.AreEqual(source.InstanceId, state.RecruitActionStates[0].SourceInstanceId);
            Assert.AreEqual("activate:test", state.RecruitActionStates[0].ActionId);
            Assert.AreEqual(1, state.RecruitActionStates[0].UsesThisTurn);
            Assert.AreEqual(state.Round, state.RecruitActionStates[0].LastUsedRound);
            CollectionAssert.AreEqual(new[] { "friendly target gained +3 Attack" }, result.Events);
            CollectionAssert.AreEqual(
                new[] { "recruit-action.validated", "recruit-action.paid", "recruit-action.resolved" },
                state.MechanicEvents.ConvertAll(item => item.Type));
        }

        [Test]
        public void Execute_InvalidTargetDoesNotPayInvokeResolverOrCreateState()
        {
            var state = CreateState();
            var source = Minion("activate-source", 1, 1);
            var friendly = Minion("friendly-target", 2, 2);
            state.Player.Board.Add(source);
            state.Player.Board.Add(friendly);
            state.Player.Tavern.Gold = 5;
            var resolverInvoked = false;

            var result = RecruitActionService.Execute(
                state,
                Definition(2, RecruitActionTargetSpec.TavernMinion),
                Request(source.InstanceId, friendly.InstanceId, 1, TargetZone.FriendlyBoard),
                context =>
                {
                    resolverInvoked = true;
                    return RecruitActionResolution.Success();
                });

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("recruit-action.target.invalid", result.Code);
            Assert.IsFalse(resolverInvoked);
            Assert.AreEqual(5, state.Player.Tavern.Gold);
            Assert.IsEmpty(state.RecruitActionStates);
        }

        [Test]
        public void Execute_InsufficientGoldDoesNotInvokeResolverOrCreateState()
        {
            var state = CreateState();
            var source = Minion("activate-source", 1, 1);
            state.Player.Board.Add(source);
            state.Player.Tavern.Gold = 1;
            var resolverInvoked = false;

            var result = RecruitActionService.Execute(
                state,
                Definition(2, RecruitActionTargetSpec.None),
                Request(source.InstanceId),
                context =>
                {
                    resolverInvoked = true;
                    return RecruitActionResolution.Success();
                });

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("recruit-action.cost.insufficient-gold", result.Code);
            Assert.IsFalse(resolverInvoked);
            Assert.AreEqual(1, state.Player.Tavern.Gold);
            Assert.IsEmpty(state.RecruitActionStates);
        }

        [Test]
        public void Execute_UsesExhaustedDoesNotPayOrChangeExistingState()
        {
            var state = CreateState();
            var source = Minion("activate-source", 1, 1);
            state.Player.Board.Add(source);
            state.Player.Tavern.Gold = 5;
            state.RecruitActionStates.Add(new RecruitActionState
            {
                SourceInstanceId = source.InstanceId,
                UsesThisTurn = 1,
                LastUsedRound = state.Round
            });

            var result = RecruitActionService.Execute(
                state,
                Definition(2, RecruitActionTargetSpec.None),
                Request(source.InstanceId),
                context => RecruitActionResolution.Success());

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("recruit-action.uses.exhausted", result.Code);
            Assert.AreEqual(5, state.Player.Tavern.Gold);
            Assert.AreEqual(1, state.RecruitActionStates[0].UsesThisTurn);
            Assert.AreEqual(state.Round, state.RecruitActionStates[0].LastUsedRound);
        }

        [Test]
        public void Execute_DifferentActionsOnSameSourceTrackUsesIndependently()
        {
            var state = CreateState();
            var source = Minion("multi-action-source", 1, 1);
            state.Player.Board.Add(source);
            state.Player.Tavern.Gold = 5;
            var firstDefinition = Definition(1, RecruitActionTargetSpec.None);
            firstDefinition.ActionId = "activate:first";
            var secondDefinition = Definition(1, RecruitActionTargetSpec.None);
            secondDefinition.ActionId = "activate:second";

            var first = RecruitActionService.Execute(
                state,
                firstDefinition,
                new RecruitActionRequest { ActionId = firstDefinition.ActionId, SourceInstanceId = source.InstanceId },
                _ => RecruitActionResolution.Success());
            var second = RecruitActionService.Execute(
                state,
                secondDefinition,
                new RecruitActionRequest { ActionId = secondDefinition.ActionId, SourceInstanceId = source.InstanceId },
                _ => RecruitActionResolution.Success());

            Assert.IsTrue(first.Succeeded, first.Message);
            Assert.IsTrue(second.Succeeded, second.Message);
            Assert.AreEqual(3, state.Player.Tavern.Gold);
            CollectionAssert.AreEquivalent(
                new[] { firstDefinition.ActionId, secondDefinition.ActionId },
                state.RecruitActionStates.ConvertAll(item => item.ActionId));
            Assert.IsTrue(state.RecruitActionStates.All(item => item.UsesThisTurn == 1));
        }

        [Test]
        public void Execute_LegacyStateWithoutActionIdIsClaimedByTheExecutedAction()
        {
            var state = CreateState();
            var source = Minion("legacy-action-source", 1, 1);
            state.Player.Board.Add(source);
            state.Player.Tavern.Gold = 5;
            state.RecruitActionStates.Add(new RecruitActionState
            {
                SourceInstanceId = source.InstanceId,
                UsesThisTurn = 1,
                LastUsedRound = state.Round - 1
            });

            var result = RecruitActionService.Execute(
                state,
                Definition(1, RecruitActionTargetSpec.None),
                Request(source.InstanceId),
                _ => RecruitActionResolution.Success());

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(1, state.RecruitActionStates.Count);
            Assert.AreEqual("activate:test", state.RecruitActionStates[0].ActionId);
            Assert.AreEqual(1, state.RecruitActionStates[0].UsesThisTurn);
            Assert.AreEqual(state.Round, state.RecruitActionStates[0].LastUsedRound);
        }

        [Test]
        public void Execute_CommitCanMigrateTheProvisionalUseDuringSourceReplacement()
        {
            var state = CreateState();
            var source = Minion("replacement-source", 1, 1);
            state.Player.Board.Add(source);
            state.Player.Tavern.Gold = 5;
            var replacementId = "replacement-source-golden";

            var result = RecruitActionService.Execute(
                state,
                Definition(1, RecruitActionTargetSpec.None),
                Request(source.InstanceId),
                _ => RecruitActionResolution.Success(live =>
                {
                    var provisional = live.RecruitActionStates.Single(item =>
                        item.SourceInstanceId == source.InstanceId && item.ActionId == "activate:test");
                    Assert.AreEqual(1, provisional.UsesThisTurn);
                    provisional.SourceInstanceId = replacementId;
                    var replacement = live.Player.Board.Single(item => item.InstanceId == source.InstanceId);
                    replacement.InstanceId = replacementId;
                }));

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(replacementId, state.RecruitActionStates.Single().SourceInstanceId);
            Assert.AreEqual("activate:test", state.RecruitActionStates.Single().ActionId);
            Assert.AreEqual(1, state.RecruitActionStates.Single().UsesThisTurn);
        }

        [Test]
        public void Execute_ResolverRejectionDoesNotPayOrCreateState()
        {
            var state = CreateState();
            var source = Minion("activate-source", 1, 1);
            state.Player.Board.Add(source);
            state.Player.Tavern.Gold = 5;

            var result = RecruitActionService.Execute(
                state,
                Definition(2, RecruitActionTargetSpec.None),
                Request(source.InstanceId),
                context => RecruitActionResolution.Failure("recruit-action.test.rejected", "Rejected by test resolver."));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("recruit-action.test.rejected", result.Code);
            Assert.AreEqual(5, state.Player.Tavern.Gold);
            Assert.IsEmpty(state.RecruitActionStates);
            Assert.AreEqual("recruit-action.rejected", state.MechanicEvents[0].Type);
        }

        [Test]
        public void ScenarioRoundTrip_RestoresRecruitActionUsageAndLockState()
        {
            var source = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            source.RecruitActionStates.Add(new RecruitActionState
            {
                SourceInstanceId = "saved-source",
                ActionId = "activate:saved",
                UsesThisTurn = 1,
                LastUsedRound = source.Round,
                Cooldown = 2,
                LockedReason = "saved lock"
            });
            var scenario = TestScenarioMapper.Clone(TestScenarioMapper.Capture(source, "recruit-action-round-trip"));
            var target = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository()).State;

            var restore = TestScenarioMapper.TryApplyTo(target, scenario);

            Assert.AreEqual(TestScenarioRestoreStatus.Applied, restore.Status, restore.Message);
            Assert.AreEqual(1, target.RecruitActionStates.Count);
            Assert.AreEqual("saved-source", target.RecruitActionStates[0].SourceInstanceId);
            Assert.AreEqual("activate:saved", target.RecruitActionStates[0].ActionId);
            Assert.AreEqual(1, target.RecruitActionStates[0].UsesThisTurn);
            Assert.AreEqual(source.Round, target.RecruitActionStates[0].LastUsedRound);
            Assert.AreEqual(2, target.RecruitActionStates[0].Cooldown);
            Assert.AreEqual("saved lock", target.RecruitActionStates[0].LockedReason);
        }

        [Test]
        public void UseRecruitActionCommand_UsesTypedRequestAndReturnsStableFailureWithoutMutation()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var source = Minion("unknown-action-source", 1, 1);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Gold = 5;
            var request = Request(source.InstanceId);

            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, request));

            Assert.IsFalse(service.LastRecruitActionResult.Succeeded);
            Assert.AreEqual("recruit-action.definition.not-found", service.LastRecruitActionResult.Code);
            Assert.AreEqual(5, service.State.Player.Tavern.Gold);
            Assert.IsEmpty(service.State.RecruitActionStates);
            Assert.IsTrue(MatchService.IsCommandAllowedInPhase(GameCommandType.UseRecruitAction, MatchPhase.Tavern));
            Assert.IsFalse(MatchService.IsCommandAllowedInPhase(GameCommandType.UseRecruitAction, MatchPhase.Combat));
        }

        private static MatchState CreateState()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Phase = MatchPhase.Tavern;
            state.Player.Board.Clear();
            state.Player.Tavern.Shop.Clear();
            state.RecruitActionStates.Clear();
            return state;
        }

        private static RecruitActionDefinition Definition(int gold, RecruitActionTargetSpec target)
        {
            return new RecruitActionDefinition
            {
                ActionId = "activate:test",
                ResolverId = "test-resolver",
                CostSpec = new RecruitActionCostSpec { Gold = gold },
                TargetSpec = target,
                UsesPerTurn = 1,
                AllowedPhase = MatchPhase.Tavern
            };
        }

        private static RecruitActionRequest Request(
            string sourceInstanceId,
            string targetInstanceId = null,
            int targetIndex = -1,
            TargetZone targetZone = TargetZone.Unspecified)
        {
            return new RecruitActionRequest
            {
                ActionId = "activate:test",
                SourceInstanceId = sourceInstanceId,
                TargetInstanceId = targetInstanceId,
                TargetIndex = targetIndex,
                TargetZone = targetZone
            };
        }

        private static MinionInstance Minion(string instanceId, int attack, int health)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
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
