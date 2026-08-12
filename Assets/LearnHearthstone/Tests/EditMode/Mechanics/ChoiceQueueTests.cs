using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class ChoiceQueueTests
    {
        [Test]
        public void Enqueue_PreservesActiveChoiceAndActivatesPendingByPriorityThenSequence()
        {
            var queue = new ChoiceQueueState();
            var active = ChoiceQueueService.Enqueue(queue, Request(ChoiceRequestKind.Quest, "active", 0));
            var low = ChoiceQueueService.Enqueue(queue, Request(ChoiceRequestKind.Trinket, "low", 20));
            var highFirst = ChoiceQueueService.Enqueue(queue, Request(ChoiceRequestKind.Anomaly, "high-first", 10));
            var highSecond = ChoiceQueueService.Enqueue(queue, Request(ChoiceRequestKind.HeroChoice, "high-second", 10));

            Assert.AreSame(active, queue.ActiveChoice);
            CollectionAssert.AreEqual(
                new[] { low.RequestId, highFirst.RequestId, highSecond.RequestId },
                queue.PendingChoices.ConvertAll(item => item.RequestId));

            Assert.IsTrue(ChoiceQueueService.CompleteActive(queue, active.RequestId));
            Assert.AreEqual(highFirst.RequestId, queue.ActiveChoice.RequestId);
            Assert.IsTrue(ChoiceQueueService.CompleteActive(queue, highFirst.RequestId));
            Assert.AreEqual(highSecond.RequestId, queue.ActiveChoice.RequestId);
            Assert.IsTrue(ChoiceQueueService.CompleteActive(queue, highSecond.RequestId));
            Assert.AreEqual(low.RequestId, queue.ActiveChoice.RequestId);
        }

        [Test]
        public void Enqueue_CompletedRequestIdIsIgnoredWithoutDuplicatingRewardOrState()
        {
            var queue = new ChoiceQueueState();
            var active = ChoiceQueueService.Enqueue(queue, Request(ChoiceRequestKind.DarkGift, "gift", 5));

            Assert.IsTrue(ChoiceQueueService.CompleteActive(queue, active.RequestId));
            Assert.IsNull(ChoiceQueueService.Enqueue(queue, active.Clone()));
            Assert.IsFalse(ChoiceQueueService.CompleteActive(queue, active.RequestId));
            CollectionAssert.AreEqual(new[] { active.RequestId }, queue.CompletedRequestIds);
            Assert.IsNull(queue.ActiveChoice);
            Assert.IsEmpty(queue.PendingChoices);
        }

        [Test]
        public void ScenarioRoundTrip_RestoresActivePendingCompletedAndNextSequence()
        {
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-choice-queue-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var source = MatchService.CreateWithDefaultCatalog(321, new InMemoryTestScenarioRepository()).State;
                var completed = ChoiceQueueService.Enqueue(source.ChoiceQueue, Request(ChoiceRequestKind.Quest, "completed", 1));
                ChoiceQueueService.CompleteActive(source.ChoiceQueue, completed.RequestId);
                var active = ChoiceQueueService.Enqueue(source.ChoiceQueue, Request(ChoiceRequestKind.Trinket, "active", 5));
                var pendingHigh = ChoiceQueueService.Enqueue(source.ChoiceQueue, Request(ChoiceRequestKind.Anomaly, "pending-high", 10));
                var pendingLow = ChoiceQueueService.Enqueue(source.ChoiceQueue, Request(ChoiceRequestKind.HeroChoice, "pending-low", 20));
                var repository = new FileTestScenarioRepository(directory);

                repository.Save(TestScenarioMapper.Capture(source, "choice-queue"));
                var loaded = repository.Load("choice-queue");
                var target = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository()).State;

                var result = TestScenarioMapper.TryApplyTo(target, loaded);

                Assert.AreEqual(TestScenarioRestoreStatus.Applied, result.Status, result.Message);
                Assert.AreEqual(active.RequestId, target.ChoiceQueue.ActiveChoice.RequestId);
                CollectionAssert.AreEqual(
                    new[] { pendingHigh.RequestId, pendingLow.RequestId },
                    target.ChoiceQueue.PendingChoices.ConvertAll(item => item.RequestId));
                CollectionAssert.AreEqual(new[] { completed.RequestId }, target.ChoiceQueue.CompletedRequestIds);
                Assert.AreEqual(5, target.ChoiceQueue.NextSequence);
                Assert.AreEqual("option-active", target.ChoiceQueue.ActiveChoice.Options[0].OptionId);
                Assert.AreEqual("cost", target.ChoiceQueue.ActiveChoice.ResolutionMetadata[0].Key);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void GetNextTurnBlockState_ReturnsStableCodeAndKeepsLegacyMessageAdapter()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            ChoiceQueueService.Enqueue(service.State.ChoiceQueue, Request(ChoiceRequestKind.Quest, "blocking", 10));

            var block = service.GetNextTurnBlockState();

            Assert.AreEqual("choice.quest.pending", block.Code);
            StringAssert.Contains("任务", block.Message);
            Assert.AreEqual(block.Message, service.GetNextTurnBlockedReason());
        }

        [Test]
        public void DebugOfferQuests_EnqueuesQuestWithoutWritingLegacyPendingChoice()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

            service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));

            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            Assert.IsNotNull(service.State.ChoiceQueue.ActiveChoice);
            Assert.AreEqual(ChoiceRequestKind.Quest, service.State.ChoiceQueue.ActiveChoice.Kind);
            Assert.AreEqual("debug", service.State.ChoiceQueue.ActiveChoice.Source);
            Assert.AreEqual(3, service.State.ChoiceQueue.ActiveChoice.Options.Count);
        }

        [Test]
        public void GetActiveMechanicChoice_AdaptsQueuedQuestForLegacyUi()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));

            var request = service.GetActiveMechanicChoice();

            Assert.IsNotNull(request);
            Assert.AreEqual(service.State.ChoiceQueue.ActiveChoice.RequestId, request.RequestId);
            Assert.AreEqual(AdvancedMechanicKind.Quest, request.Kind);
            Assert.AreEqual("Main", request.Slot);
            Assert.AreEqual(3, request.Options.Count);
        }

        [Test]
        public void ChooseMechanicOption_CompletesQueuedQuestAndActivatesNextChoice()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));
            var quest = service.State.ChoiceQueue.ActiveChoice;
            var next = ChoiceQueueService.Enqueue(service.State.ChoiceQueue, Request(ChoiceRequestKind.DarkGift, "next", 100));

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            CollectionAssert.Contains(service.State.ChoiceQueue.CompletedRequestIds, quest.RequestId);
            Assert.AreEqual(next.RequestId, service.State.ChoiceQueue.ActiveChoice.RequestId);
            Assert.IsNotNull(service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest);
        }

        [Test]
        public void DebugOfferQuests_WithActiveQueuedChoice_AppendsWithoutOverwritingActive()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var active = ChoiceQueueService.Enqueue(service.State.ChoiceQueue, Request(ChoiceRequestKind.DarkGift, "active", 100));

            service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));

            Assert.AreEqual(active.RequestId, service.State.ChoiceQueue.ActiveChoice.RequestId);
            Assert.AreEqual(1, service.State.ChoiceQueue.PendingChoices.Count);
            Assert.AreEqual(ChoiceRequestKind.Quest, service.State.ChoiceQueue.PendingChoices[0].Kind);
        }

        [Test]
        public void DebugOfferLesserTrinkets_EnqueuesWithoutWritingLegacyPendingChoice()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 20;

            service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));

            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            Assert.AreEqual(ChoiceRequestKind.Trinket, service.State.ChoiceQueue.ActiveChoice.Kind);
            Assert.AreEqual(4, service.State.ChoiceQueue.ActiveChoice.Options.Count);
            Assert.AreEqual("Lesser", service.GetActiveMechanicChoice().Slot);
        }

        [Test]
        public void ChooseMechanicOption_CompletesQueuedTrinketAndActivatesNextChoice()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 20;
            service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));
            var trinket = service.State.ChoiceQueue.ActiveChoice;
            var next = ChoiceQueueService.Enqueue(service.State.ChoiceQueue, Request(ChoiceRequestKind.Quest, "next-quest", 100));

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            CollectionAssert.Contains(service.State.ChoiceQueue.CompletedRequestIds, trinket.RequestId);
            Assert.AreEqual(next.RequestId, service.State.ChoiceQueue.ActiveChoice.RequestId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(service.State.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId));
        }

        [Test]
        public void DebugOfferLesserTrinkets_WithActiveQuest_AppendsWithoutOverwritingActive()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 20;
            service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));
            var active = service.State.ChoiceQueue.ActiveChoice;

            service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));

            Assert.AreEqual(active.RequestId, service.State.ChoiceQueue.ActiveChoice.RequestId);
            Assert.AreEqual(1, service.State.ChoiceQueue.PendingChoices.Count);
            Assert.AreEqual(ChoiceRequestKind.Trinket, service.State.ChoiceQueue.PendingChoices[0].Kind);
        }

        [Test]
        public void ChooseMechanicOption_InsufficientGoldKeepsQueuedTrinketActive()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 20;
            service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));
            var active = service.State.ChoiceQueue.ActiveChoice;
            var paidOptionIndex = active.Options.FindIndex(option => option.Cost > 0);
            Assert.GreaterOrEqual(paidOptionIndex, 0);
            service.State.Player.Tavern.Gold = 0;

            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, paidOptionIndex)));

            Assert.AreSame(active, service.State.ChoiceQueue.ActiveChoice);
            CollectionAssert.DoesNotContain(service.State.ChoiceQueue.CompletedRequestIds, active.RequestId);
        }

        [Test]
        public void Cancel_RemovesActiveWithoutCompletingAndActivatesNextChoice()
        {
            var queue = new ChoiceQueueState();
            var active = ChoiceQueueService.Enqueue(queue, Request(ChoiceRequestKind.Anomaly, "expiring", 100));
            var next = ChoiceQueueService.Enqueue(queue, Request(ChoiceRequestKind.HeroChoice, "next", 100));

            Assert.IsTrue(ChoiceQueueService.Cancel(queue, active.RequestId));

            Assert.AreEqual(next.RequestId, queue.ActiveChoice.RequestId);
            CollectionAssert.DoesNotContain(queue.CompletedRequestIds, active.RequestId);
        }

        [Test]
        public void AudienceChoice_EnqueuesAnomalyWithoutWritingLegacyPendingChoice()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    EnableAnomalies = true,
                    EnableTrinkets = false,
                    SelectedAnomalyCardId = "BG27_Anomaly_580"
                });

            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            Assert.AreEqual(ChoiceRequestKind.Anomaly, service.State.ChoiceQueue.ActiveChoice.Kind);
            Assert.AreEqual(AdvancedMechanicKind.Anomaly, service.GetActiveMechanicChoice().Kind);
        }

        [Test]
        public void AudienceChoice_UnchosenRequestIsCancelledAndRecreatedForNextRound()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    EnableAnomalies = true,
                    EnableTrinkets = false,
                    SelectedAnomalyCardId = "BG27_Anomaly_580"
                });
            var first = service.State.ChoiceQueue.ActiveChoice;

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(2, service.State.Round);
            Assert.AreEqual(ChoiceRequestKind.Anomaly, service.State.ChoiceQueue.ActiveChoice.Kind);
            Assert.AreNotEqual(first.RequestId, service.State.ChoiceQueue.ActiveChoice.RequestId);
            CollectionAssert.DoesNotContain(service.State.ChoiceQueue.CompletedRequestIds, first.RequestId);
        }

        [Test]
        public void GalewingChoice_EnqueuesHeroChoiceAndCompletesThroughLegacyCommand()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    EnableTrinkets = false,
                    SelectedHeroCardId = "BG20_HERO_283"
                });

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));
            var active = service.State.ChoiceQueue.ActiveChoice;

            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            Assert.AreEqual(ChoiceRequestKind.HeroChoice, active.Kind);
            Assert.AreEqual(AdvancedMechanicKind.Distortion, service.GetActiveMechanicChoice().Kind);

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            CollectionAssert.Contains(service.State.ChoiceQueue.CompletedRequestIds, active.RequestId);
            Assert.IsTrue(service.State.Player.Tavern.AdvancedMechanics.Selections.ContainsKey("hero:galewing:route"));
        }

        [Test]
        public void DiscoverAdapter_QueuesLegacyDiscoverBehindActiveMechanic()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var active = ChoiceQueueService.Enqueue(
                service.State.ChoiceQueue,
                Request(ChoiceRequestKind.DarkGift, "active-gift", 100));
            service.State.Player.Tavern.QueueDiscover(Discover("legacy-discover", "discover-card"));

            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 0));

            Assert.AreEqual(active.RequestId, service.State.ChoiceQueue.ActiveChoice.RequestId);
            Assert.IsNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(1, service.State.ChoiceQueue.PendingChoices.Count);
            var queued = service.State.ChoiceQueue.PendingChoices[0];
            Assert.AreEqual(ChoiceRequestKind.Discover, queued.Kind);
            Assert.AreEqual("discover-card", queued.Discover.Options[0].CardId);
            Assert.AreSame(queued.Discover, service.State.Player.Tavern.DiscoverQueue[0]);
        }

        [Test]
        public void ChooseDiscover_CompletesQueuedDiscoverAndActivatesNextMechanic()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.QueueDiscover(Discover("first-discover", "picked-card"));
            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 0));
            var discover = service.State.ChoiceQueue.ActiveChoice;
            var next = ChoiceQueueService.Enqueue(
                service.State.ChoiceQueue,
                Request(ChoiceRequestKind.Quest, "next-quest", 100));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            CollectionAssert.Contains(service.State.ChoiceQueue.CompletedRequestIds, discover.RequestId);
            Assert.AreEqual(next.RequestId, service.State.ChoiceQueue.ActiveChoice.RequestId);
            Assert.IsNull(service.State.Player.Tavern.Discover);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Exists(card => card.CardId == "picked-card"));
        }

        [Test]
        public void ChooseDiscover_WithQueuedDiscover_PromotesNextDiscoverInFifoOrder()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.QueueDiscover(Discover("first-discover", "first-card"));
            service.State.Player.Tavern.QueueDiscover(Discover("second-discover", "second-card"));
            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 0));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(ChoiceRequestKind.Discover, service.State.ChoiceQueue.ActiveChoice.Kind);
            Assert.AreEqual("second-discover", service.State.ChoiceQueue.ActiveChoice.Source);
            Assert.AreSame(service.State.ChoiceQueue.ActiveChoice.Discover, service.State.Player.Tavern.Discover);
            Assert.IsEmpty(service.State.Player.Tavern.DiscoverQueue);
        }

        [Test]
        public void ChooseDiscover_WithContinuation_KeepsRequestAheadOfQueuedDiscover()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var continuing = Discover("clockwork-assistant", "first-card");
            continuing.RemainingPicks = 2;
            service.State.Player.Tavern.QueueDiscover(continuing);
            service.State.Player.Tavern.QueueDiscover(Discover("queued-after-continuation", "queued-card"));
            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 0));
            var requestId = service.State.ChoiceQueue.ActiveChoice.RequestId;

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(requestId, service.State.ChoiceQueue.ActiveChoice.RequestId);
            Assert.AreEqual("clockwork-assistant", service.State.Player.Tavern.Discover.Source);
            Assert.AreEqual(1, service.State.Player.Tavern.Discover.RemainingPicks);
            Assert.AreEqual("queued-after-continuation", service.State.Player.Tavern.DiscoverQueue[0].Source);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual("queued-after-continuation", service.State.ChoiceQueue.ActiveChoice.Source);
            Assert.AreSame(service.State.ChoiceQueue.ActiveChoice.Discover, service.State.Player.Tavern.Discover);
        }

        [Test]
        public void ScenarioRoundTrip_RestoresDiscoverSnapshotAndLegacyView()
        {
            var sourceService = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var discover = Discover("saved-discover", "saved-card");
            discover.TargetInstanceId = "target-1";
            discover.RemainingPicks = 2;
            discover.Options[0].Tags.Add("saved-tag");
            discover.Options[0].Counters["saved-counter"] = 7;
            sourceService.State.Player.Tavern.QueueDiscover(discover);
            sourceService.Apply(new GameCommand(GameCommandType.DebugAddGold, 0));
            var scenario = TestScenarioMapper.Clone(TestScenarioMapper.Capture(sourceService.State, "discover-round-trip"));
            var targetService = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository());

            var result = TestScenarioMapper.TryApplyTo(targetService.State, scenario);

            Assert.AreEqual(TestScenarioRestoreStatus.Applied, result.Status, result.Message);
            Assert.AreEqual(ChoiceRequestKind.Discover, targetService.State.ChoiceQueue.ActiveChoice.Kind);
            Assert.AreEqual("target-1", targetService.State.ChoiceQueue.ActiveChoice.Discover.TargetInstanceId);
            Assert.AreEqual(2, targetService.State.ChoiceQueue.ActiveChoice.Discover.RemainingPicks);
            Assert.AreEqual("saved-tag", targetService.State.ChoiceQueue.ActiveChoice.Discover.Options[0].Tags[0]);
            Assert.AreEqual(7, targetService.State.ChoiceQueue.ActiveChoice.Discover.Options[0].Counters["saved-counter"]);
            Assert.AreSame(targetService.State.ChoiceQueue.ActiveChoice.Discover, targetService.State.Player.Tavern.Discover);

            targetService.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsTrue(targetService.State.Player.Tavern.Hand.Exists(card => card.CardId == "saved-card"));
        }

        [Test]
        public void ScheduledChoiceStatus_UsesActiveQueueChoiceAsBlockingSource()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.AdvancedMechanics.Counters["trinket:mystery_cube:pending"] = 1;
            service.State.Player.Tavern.QueueDiscover(Discover("blocking-discover", "discover-card"));
            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 0));

            var status = service.GetAdvancedChoiceStatuses()
                .Single(item => item.Id == "trinket-mystery-cube");

            Assert.IsTrue(status.IsBlocking);
            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            Assert.AreEqual(ChoiceRequestKind.Discover, service.State.ChoiceQueue.ActiveChoice.Kind);
        }

        [Test]
        public void Apply_RecordsChoiceCreatedActivatedAndCompletedInStableSequence()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.MechanicEvents.Clear();

            service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));
            var requestId = service.State.ChoiceQueue.ActiveChoice.RequestId;
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            CollectionAssert.AreEqual(
                new[] { "choice.created", "choice.activated", "choice.completed" },
                service.State.MechanicEvents.Select(item => item.Type).ToArray());
            CollectionAssert.AreEqual(
                new[] { 1, 2, 3 },
                service.State.MechanicEvents.Select(item => item.Sequence).ToArray());
            Assert.IsTrue(service.State.MechanicEvents.All(item => item.RequestId == requestId));
        }

        private static DiscoverState Discover(string source, string cardId)
        {
            return new DiscoverState
            {
                Source = source,
                RewardTier = 1,
                RemainingPicks = 1,
                Options = new List<MinionInstance>
                {
                    new MinionInstance
                    {
                        InstanceId = source + "-option",
                        DefinitionId = cardId,
                        CardId = cardId,
                        Name = cardId,
                        Attack = 1,
                        Health = 1,
                        MaxHealth = 1,
                        TavernTier = 1,
                        Tribes = new List<Tribe> { Tribe.None },
                        Keywords = new List<Keyword>(),
                        Tags = new List<string>(),
                        Counters = new Dictionary<string, int>()
                    }
                }
            };
        }

        private static ChoiceQueueItem Request(ChoiceRequestKind kind, string source, int priority)
        {
            return new ChoiceQueueItem
            {
                Kind = kind,
                Source = source,
                CreatedRound = 1,
                Priority = priority,
                Blocking = true,
                RemainingPicks = 1,
                Options = new List<MechanicChoiceOption>
                {
                    new MechanicChoiceOption
                    {
                        OptionId = "option-" + source,
                        DisplayName = source
                    }
                },
                ResolutionMetadata = new List<ChoiceResolutionMetadataEntry>
                {
                    new ChoiceResolutionMetadataEntry { Key = "cost", Value = "3" }
                }
            };
        }
    }
}
