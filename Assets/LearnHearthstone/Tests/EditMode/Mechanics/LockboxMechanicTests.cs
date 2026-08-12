using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class LockboxMechanicTests
    {
        private const string DefinitionRevisionId = "NEUTRAL_ROGUE_BG36_520t@36.2";
        private const string OpenResolverId = "season14.lockbox.open@1";

        [Test]
        public void CreateOrAccelerate_ReusesActiveLockboxAndPersistsAccelerationCountInEvents()
        {
            var state = CreateState();
            var registry = new DelayedObjectResolverRegistry();

            var created = LockboxMechanicService.CreateOrAccelerate(
                state,
                Request("lockbox-1", "bilgewater-1", "lockbox-request-1"),
                registry);
            var accelerated = LockboxMechanicService.CreateOrAccelerate(
                state,
                Request("ignored-new-lockbox", "mutineer-1", "lockbox-request-2"),
                registry);

            Assert.IsTrue(created.Succeeded, created.Message);
            Assert.AreEqual("lockbox.created", created.Code);
            Assert.AreEqual(5, created.RemainingTurns);
            Assert.IsTrue(accelerated.Succeeded, accelerated.Message);
            Assert.AreEqual("lockbox.accelerated", accelerated.Code);
            Assert.AreEqual(4, accelerated.RemainingTurns);
            Assert.AreEqual(1, accelerated.AccelerationCount);
            Assert.AreEqual(1, state.DelayedObjectStates.Count);
            Assert.AreEqual("lockbox-1", state.DelayedObjectStates[0].InstanceId);
            Assert.AreEqual("mutineer-1", state.MechanicEvents.Last().Source);
            Assert.AreEqual("lockbox-request-2", state.MechanicEvents.Last().RequestId);

            var scenario = TestScenarioMapper.Clone(TestScenarioMapper.Capture(state, "lockbox-count-round-trip"));
            var restored = CreateState();
            var restore = TestScenarioMapper.TryApplyTo(restored, scenario);

            Assert.AreEqual(TestScenarioRestoreStatus.Applied, restore.Status, restore.Message);
            Assert.AreEqual(1, LockboxMechanicService.GetAccelerationCount(restored, "lockbox-1"));
            Assert.AreEqual(4, restored.DelayedObjectStates.Single().RemainingTurns);
        }

        [Test]
        public void CreateOrAccelerate_DuplicateRequestAndOpenedLockboxDoNotResolveTwice()
        {
            var state = CreateState();
            var resolverCalls = 0;
            var registry = new DelayedObjectResolverRegistry();
            registry.Register(OpenResolverId, context =>
            {
                resolverCalls += 1;
                return DelayedObjectResolution.Success(live => live.Player.Tavern.Gold += 7);
            });
            LockboxMechanicService.CreateOrAccelerate(
                state,
                Request("lockbox-1", "bilgewater-1", "lockbox-create"),
                registry);

            LockboxMechanicResult opened = null;
            for (var index = 1; index <= 5; index += 1)
            {
                opened = LockboxMechanicService.CreateOrAccelerate(
                    state,
                    Request("ignored-" + index, "pirate-" + index, "lockbox-accelerate-" + index),
                    registry);
            }
            var duplicate = LockboxMechanicService.CreateOrAccelerate(
                state,
                Request("ignored-5", "pirate-5", "lockbox-accelerate-5"),
                registry);

            Assert.IsNotNull(opened);
            Assert.IsTrue(opened.Succeeded, opened.Message);
            Assert.IsTrue(opened.Opened);
            Assert.AreEqual("lockbox.opened", opened.Code);
            Assert.AreEqual(1, resolverCalls);
            Assert.AreEqual(7, state.Player.Tavern.Gold);
            Assert.AreEqual(5, LockboxMechanicService.GetAccelerationCount(state, "lockbox-1"));
            Assert.IsTrue(duplicate.Succeeded, duplicate.Message);
            Assert.AreEqual("lockbox.already-applied", duplicate.Code);
            Assert.AreEqual(1, resolverCalls);
            Assert.AreEqual(1, state.MechanicEvents.Count(item => item.Type == "delayed-object.opened"));
        }

        [Test]
        public void AdvanceTurnEnded_AdvancesCreationRoundAndIsIdempotentPerOccurrence()
        {
            var state = CreateState();
            var registry = new DelayedObjectResolverRegistry();
            LockboxMechanicService.CreateOrAccelerate(
                state,
                Request("lockbox-1", "bilgewater-1", "lockbox-create"),
                registry);

            var first = LockboxMechanicService.AdvanceTurnEnded(
                state,
                registry,
                state.Round,
                "transition-1",
                1);
            var duplicate = LockboxMechanicService.AdvanceTurnEnded(
                state,
                registry,
                state.Round,
                "transition-1",
                1);

            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(4, first[0].RemainingTurns);
            Assert.AreEqual(1, duplicate.Count);
            Assert.AreEqual(4, duplicate[0].RemainingTurns);
            Assert.AreEqual(0, LockboxMechanicService.GetAccelerationCount(state, "lockbox-1"));
            Assert.AreEqual(1, state.MechanicEvents.Count(item => item.Type == "delayed-object.turn-ended"));
            Assert.AreEqual(
                "turn-end:3:transition-1:lockbox:lockbox-1:occurrence:0",
                state.MechanicEvents.Single(item => item.Type == "delayed-object.turn-ended").RequestId);
        }

        [Test]
        public void AdvanceTurnEnded_UsesDistinctUnitOccurrencesAndOpensOnce()
        {
            var state = CreateState();
            var resolverCalls = 0;
            var registry = new DelayedObjectResolverRegistry();
            registry.Register(OpenResolverId, context =>
            {
                resolverCalls += 1;
                return DelayedObjectResolution.Success(live => live.Player.Tavern.Gold += 7);
            });
            LockboxMechanicService.CreateOrAccelerate(
                state,
                Request("lockbox-1", "bilgewater-1", "lockbox-create"),
                registry);
            state.DelayedObjectStates.Single().RemainingTurns = 1;

            var results = LockboxMechanicService.AdvanceTurnEnded(
                state,
                registry,
                state.Round,
                "transition-open",
                3);
            var replay = LockboxMechanicService.AdvanceTurnEnded(
                state,
                registry,
                state.Round,
                "transition-open",
                3);

            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0].Opened);
            Assert.IsEmpty(replay);
            Assert.AreEqual(1, resolverCalls);
            Assert.AreEqual(7, state.Player.Tavern.Gold);
            Assert.AreEqual(1, state.MechanicEvents.Count(item => item.Type == "delayed-object.turn-ended"));
            Assert.AreEqual(1, state.MechanicEvents.Count(item => item.Type == "delayed-object.opened"));
        }

        [Test]
        public void TurnStartTrinket_AcceleratesExistingOrCreatesAfterPriorTurnEndOpened()
        {
            var state = CreateState();
            var registry = new DelayedObjectResolverRegistry();
            registry.Register(OpenResolverId, context => DelayedObjectResolution.Success());
            LockboxMechanicService.CreateOrAccelerate(
                state,
                Request("lockbox-1", "bilgewater-1", "lockbox-create"),
                registry);

            var accelerated = LockboxMechanicService.CreateOrAccelerate(
                state,
                Request(
                    "unused-lockbox",
                    "trinket-turn-start",
                    "trinket-turn-start-1",
                    "delayed-object.trinket-turn-start-accelerated",
                    2),
                registry);

            Assert.AreEqual(1, state.DelayedObjectStates.Count);
            Assert.AreEqual(3, accelerated.RemainingTurns);
            Assert.AreEqual(1, accelerated.AccelerationCount);
            Assert.AreEqual("trinket-turn-start", state.MechanicEvents.Last().Source);
            Assert.AreEqual("delayed-object.trinket-turn-start-accelerated", state.MechanicEvents.Last().Type);

            state.DelayedObjectStates.Single().RemainingTurns = 1;
            LockboxMechanicService.AdvanceTurnEnded(
                state,
                registry,
                state.Round,
                "transition-open",
                1);
            var created = LockboxMechanicService.CreateOrAccelerate(
                state,
                Request(
                    "lockbox-2",
                    "trinket-turn-start",
                    "trinket-turn-start-2",
                    "delayed-object.trinket-turn-start-accelerated",
                    2),
                registry);

            Assert.AreEqual(2, state.DelayedObjectStates.Count);
            Assert.AreEqual("lockbox.created", created.Code);
            Assert.AreEqual("lockbox-2", created.InstanceId);
            Assert.AreEqual(5, created.RemainingTurns);
        }

        [Test]
        public void Advance_WhenOpenResolverIsMissing_DoesNotPartiallyCommitCountdownOrEvent()
        {
            var state = CreateState();
            Assert.IsTrue(DelayedObjectService.Add(state, new DelayedObjectState
            {
                InstanceId = "lockbox-1",
                DefinitionRevisionId = DefinitionRevisionId,
                CreatedRound = state.Round,
                RemainingTurns = 1,
                OpenResolverId = OpenResolverId,
                Source = "bilgewater-1"
            }));

            var result = DelayedObjectService.Advance(
                state,
                "lockbox-1",
                1,
                null,
                "lockbox-open-without-resolver");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("delayed-object.resolver.not-found", result.Code);
            Assert.AreEqual(1, state.DelayedObjectStates.Single().RemainingTurns);
            Assert.AreEqual(1, state.MechanicEvents.Count);
            Assert.IsFalse(state.MechanicEvents.Any(item => item.RequestId == "lockbox-open-without-resolver"));
        }

        [Test]
        public void MatchServiceNextTurn_AdvancesLockboxThroughSharedDelayedObjectRegistry()
        {
            var registry = new DelayedObjectResolverRegistry();
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                delayedObjectResolvers: registry);
            var state = service.State;
            state.DelayedObjectStates.Clear();
            state.MechanicEvents.Clear();
            Assert.IsTrue(LockboxMechanicService.CreateOrAccelerate(
                state,
                Request("lockbox-1", "bilgewater-1", "lockbox-create"),
                registry).Succeeded);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, state.Round);
            Assert.AreEqual(4, state.DelayedObjectStates.Single().RemainingTurns);
            Assert.AreEqual(1, state.MechanicEvents.Count(item => item.Type == "delayed-object.turn-ended"));
        }

        [Test]
        public void MatchServiceBeginTurnTransition_ReentryUsesOneStableTransition()
        {
            var registry = new DelayedObjectResolverRegistry();
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                delayedObjectResolvers: registry);
            var state = service.State;
            state.DelayedObjectStates.Clear();
            state.MechanicEvents.Clear();
            Assert.IsTrue(LockboxMechanicService.CreateOrAccelerate(
                state,
                Request("lockbox-1", "bilgewater-1", "lockbox-create"),
                registry).Succeeded);

            service.Apply(new GameCommand(GameCommandType.BeginNextTurnTransition));
            var pendingTransitionId = state.PendingTurnEndTransitionId;
            var turnEndEventCount = state.MechanicEvents.Count(item => item.Type == "delayed-object.turn-ended");
            service.Apply(new GameCommand(GameCommandType.BeginNextTurnTransition));

            Assert.IsFalse(string.IsNullOrWhiteSpace(pendingTransitionId));
            Assert.AreEqual(pendingTransitionId, state.PendingTurnEndTransitionId);
            Assert.AreEqual(2, state.PendingTurnStartRound);
            Assert.AreEqual(4, state.DelayedObjectStates.Single().RemainingTurns);
            Assert.AreEqual(turnEndEventCount, state.MechanicEvents.Count(item => item.Type == "delayed-object.turn-ended"));

            service.Apply(new GameCommand(GameCommandType.ContinueNextTurnTransition));

            Assert.AreEqual(2, state.Round);
            Assert.AreEqual(0, state.PendingTurnStartRound);
            Assert.IsTrue(string.IsNullOrEmpty(state.PendingTurnEndTransitionId));
            Assert.AreEqual(1, state.TurnEndTransitionSequence);
        }

        [Test]
        public void ScenarioRoundTrip_BeforeAndAfterTurnEndRestoresIdentically()
        {
            var beforeRegistry = new DelayedObjectResolverRegistry();
            var uninterrupted = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                delayedObjectResolvers: beforeRegistry);
            uninterrupted.State.DelayedObjectStates.Clear();
            uninterrupted.State.MechanicEvents.Clear();
            Assert.IsTrue(LockboxMechanicService.CreateOrAccelerate(
                uninterrupted.State,
                Request("lockbox-before", "bilgewater-1", "lockbox-before-create"),
                beforeRegistry).Succeeded);
            var beforeScenario = TestScenarioMapper.Clone(
                TestScenarioMapper.Capture(uninterrupted.State, "lockbox-before-turn-end"));
            var beforeRestored = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                delayedObjectResolvers: new DelayedObjectResolverRegistry());
            Assert.AreEqual(
                TestScenarioRestoreStatus.Applied,
                TestScenarioMapper.TryApplyTo(beforeRestored.State, beforeScenario).Status);

            uninterrupted.Apply(new GameCommand(GameCommandType.NextTurn));
            beforeRestored.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(uninterrupted.State.Round, beforeRestored.State.Round);
            Assert.AreEqual(
                uninterrupted.State.DelayedObjectStates.Single().RemainingTurns,
                beforeRestored.State.DelayedObjectStates.Single().RemainingTurns);
            Assert.AreEqual(
                uninterrupted.State.MechanicEvents.Count(item => item.Type == "delayed-object.turn-ended"),
                beforeRestored.State.MechanicEvents.Count(item => item.Type == "delayed-object.turn-ended"));

            var afterRegistry = new DelayedObjectResolverRegistry();
            var staged = MatchService.CreateWithDefaultCatalog(
                54321,
                new InMemoryTestScenarioRepository(),
                delayedObjectResolvers: afterRegistry);
            staged.State.DelayedObjectStates.Clear();
            staged.State.MechanicEvents.Clear();
            Assert.IsTrue(LockboxMechanicService.CreateOrAccelerate(
                staged.State,
                Request("lockbox-after", "bilgewater-2", "lockbox-after-create"),
                afterRegistry).Succeeded);
            staged.Apply(new GameCommand(GameCommandType.BeginNextTurnTransition));
            var afterScenario = TestScenarioMapper.Clone(
                TestScenarioMapper.Capture(staged.State, "lockbox-after-turn-end"));
            var afterRestored = MatchService.CreateWithDefaultCatalog(
                54321,
                new InMemoryTestScenarioRepository(),
                delayedObjectResolvers: new DelayedObjectResolverRegistry());
            var afterRestore = TestScenarioMapper.TryApplyTo(afterRestored.State, afterScenario);

            Assert.AreEqual(TestScenarioRestoreStatus.Applied, afterRestore.Status, afterRestore.Message);
            Assert.AreEqual(staged.State.PendingTurnEndTransitionId, afterRestored.State.PendingTurnEndTransitionId);
            Assert.AreEqual(staged.State.PendingTurnEndOccurrenceCount, afterRestored.State.PendingTurnEndOccurrenceCount);

            staged.Apply(new GameCommand(GameCommandType.ContinueNextTurnTransition));
            afterRestored.Apply(new GameCommand(GameCommandType.ContinueNextTurnTransition));

            Assert.AreEqual(staged.State.Round, afterRestored.State.Round);
            Assert.AreEqual(staged.State.TurnEndTransitionSequence, afterRestored.State.TurnEndTransitionSequence);
            Assert.AreEqual(
                staged.State.DelayedObjectStates.Single().RemainingTurns,
                afterRestored.State.DelayedObjectStates.Single().RemainingTurns);
            Assert.AreEqual(
                staged.State.MechanicEvents.Count(item => item.Type == "delayed-object.turn-ended"),
                afterRestored.State.MechanicEvents.Count(item => item.Type == "delayed-object.turn-ended"));
        }

        private static LockboxMechanicRequest Request(
            string instanceId,
            string source,
            string requestId,
            string eventType = null,
            int accelerationTurns = 1)
        {
            return new LockboxMechanicRequest
            {
                InstanceId = instanceId,
                DefinitionRevisionId = DefinitionRevisionId,
                OpenResolverId = OpenResolverId,
                Source = source,
                RequestId = requestId,
                EventType = eventType,
                AccelerationTurns = accelerationTurns
            };
        }

        private static MatchState CreateState()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Phase = MatchPhase.Tavern;
            state.Round = 3;
            state.Player.Tavern.Gold = 0;
            state.DelayedObjectStates.Clear();
            state.MechanicEvents.Clear();
            return state;
        }
    }
}
