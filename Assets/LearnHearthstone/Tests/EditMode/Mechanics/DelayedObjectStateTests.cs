using System.Collections.Generic;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class DelayedObjectStateTests
    {
        [Test]
        public void Advance_AcceleratesAndOpensExactlyOnce()
        {
            var state = CreateState();
            var resolverCalls = 0;
            Assert.IsTrue(DelayedObjectService.Add(state, new DelayedObjectState
            {
                InstanceId = "lockbox-1",
                DefinitionRevisionId = "lockbox@1",
                CreatedRound = state.Round,
                RemainingTurns = 2,
                OpenResolverId = "test-open",
                Source = "test"
            }));

            var first = DelayedObjectService.Advance(
                state,
                "lockbox-1",
                1,
                context =>
                {
                    resolverCalls += 1;
                    return DelayedObjectResolution.Success();
                });
            var second = DelayedObjectService.Advance(
                state,
                "lockbox-1",
                1,
                context =>
                {
                    resolverCalls += 1;
                    Assert.AreNotSame(state.DelayedObjectStates[0], context.DelayedObject);
                    return DelayedObjectResolution.Success(live => live.Player.Tavern.Gold += 2);
                });
            var third = DelayedObjectService.TryOpen(
                state,
                "lockbox-1",
                context =>
                {
                    resolverCalls += 1;
                    return DelayedObjectResolution.Success();
                });

            Assert.IsTrue(first.Succeeded, first.Message);
            Assert.IsFalse(first.Opened);
            Assert.AreEqual(1, first.RemainingTurns);
            Assert.IsTrue(second.Succeeded, second.Message);
            Assert.IsTrue(second.Opened);
            Assert.AreEqual(0, second.RemainingTurns);
            Assert.IsFalse(third.Succeeded);
            Assert.AreEqual("delayed-object.already-opened", third.Code);
            Assert.AreEqual(1, resolverCalls);
            Assert.AreEqual(2, state.Player.Tavern.Gold);
            Assert.IsTrue(state.DelayedObjectStates[0].Opened);
            CollectionAssert.AreEqual(
                new[] { "delayed-object.created", "delayed-object.accelerated", "delayed-object.accelerated", "delayed-object.opened" },
                state.MechanicEvents.ConvertAll(item => item.Type));
        }

        [Test]
        public void ScenarioRoundTrip_RestoresDelayedObjectsEventsAndNextSequence()
        {
            var source = CreateState();
            source.DelayedObjectStates.Add(new DelayedObjectState
            {
                InstanceId = "saved-lockbox",
                DefinitionRevisionId = "lockbox@saved",
                CreatedRound = source.Round,
                RemainingTurns = 3,
                OpenResolverId = "saved-resolver",
                Source = "saved-source",
                Opened = false
            });
            MechanicEventLog.Append(
                source,
                "test.saved",
                "saved-source",
                new[] { "target-a", "target-b" },
                "saved-result",
                "request-1");
            var scenario = TestScenarioMapper.Clone(TestScenarioMapper.Capture(source, "delayed-event-round-trip"));
            var target = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository()).State;

            var restore = TestScenarioMapper.TryApplyTo(target, scenario);
            var appended = MechanicEventLog.Append(target, "test.after-restore", "source-2");

            Assert.AreEqual(TestScenarioRestoreStatus.Applied, restore.Status, restore.Message);
            Assert.AreEqual(1, target.DelayedObjectStates.Count);
            Assert.AreEqual("saved-lockbox", target.DelayedObjectStates[0].InstanceId);
            Assert.AreEqual(3, target.DelayedObjectStates[0].RemainingTurns);
            Assert.AreEqual(1, scenario.RngState.MechanicEventCursor);
            Assert.AreEqual(2, target.MechanicEvents.Count);
            Assert.AreEqual(1, target.MechanicEvents[0].Sequence);
            CollectionAssert.AreEqual(new[] { "target-a", "target-b" }, target.MechanicEvents[0].Targets);
            Assert.AreEqual(2, appended.Sequence);
        }

        [Test]
        public void Advance_WhenOpenCommitThrows_RollsBackCountdownOpenFlagAndOccurrenceEvent()
        {
            var state = CreateState();
            Assert.IsTrue(DelayedObjectService.Add(state, new DelayedObjectState
            {
                InstanceId = "lockbox-commit-failure",
                DefinitionRevisionId = "lockbox@atomic",
                CreatedRound = state.Round,
                RemainingTurns = 1,
                OpenResolverId = "throwing-open",
                Source = "test"
            }));

            var result = DelayedObjectService.Advance(
                state,
                "lockbox-commit-failure",
                1,
                context => DelayedObjectResolution.Success(live =>
                    throw new System.InvalidOperationException("commit failed")),
                "turn-end:1:transition-atomic:lockbox:lockbox-commit-failure:occurrence:0",
                "turn-end:1:transition-atomic",
                "delayed-object.turn-ended");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("delayed-object.commit.failed", result.Code);
            Assert.AreEqual(1, state.DelayedObjectStates[0].RemainingTurns);
            Assert.IsFalse(state.DelayedObjectStates[0].Opened);
            Assert.AreEqual(1, state.MechanicEvents.Count);
            Assert.IsFalse(state.MechanicEvents.Exists(item => item.Type == "delayed-object.turn-ended"));
        }

        private static MatchState CreateState()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Phase = MatchPhase.Tavern;
            state.Player.Tavern.Gold = 0;
            state.DelayedObjectStates.Clear();
            state.MechanicEvents.Clear();
            return state;
        }
    }
}
