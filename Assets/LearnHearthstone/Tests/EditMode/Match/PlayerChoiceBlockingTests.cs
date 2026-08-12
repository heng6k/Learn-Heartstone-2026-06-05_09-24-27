using System;
using System.Collections.Generic;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class PlayerChoiceBlockingTests
    {
        [Test]
        public void NextTurn_WithDiscoverPending_BlocksAndKeepsCurrentRound()
        {
            var service = CreateService();
            var discover = CreatePendingDiscover();
            service.State.Player.Tavern.Discover = discover;
            var round = service.State.Round;

            var error = Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.NextTurn)));

            StringAssert.Contains("\u53d1\u73b0", error.Message);
            Assert.AreEqual(round, service.State.Round);
            Assert.AreSame(discover, service.State.Player.Tavern.Discover);
        }

        [Test]
        public void SimulateCombat_WithDiscoverPending_BlocksAndKeepsCurrentRound()
        {
            var service = CreateService();
            var discover = CreatePendingDiscover();
            service.State.Player.Tavern.Discover = discover;
            var round = service.State.Round;

            var error = Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.SimulateCombat)));

            StringAssert.Contains("\u53d1\u73b0", error.Message);
            Assert.AreEqual(round, service.State.Round);
            Assert.AreSame(discover, service.State.Player.Tavern.Discover);
        }

        [Test]
        public void NextTurn_WithQuestPending_BlocksAndKeepsChoice()
        {
            var service = CreateService();
            service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));
            var request = service.State.ChoiceQueue.ActiveChoice;
            var round = service.State.Round;

            var error = Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.NextTurn)));

            StringAssert.Contains("\u4efb\u52a1", error.Message);
            Assert.AreEqual(round, service.State.Round);
            Assert.AreSame(request, service.State.ChoiceQueue.ActiveChoice);
        }

        [Test]
        public void NextTurn_WithTrinketPending_BlocksAndKeepsChoice()
        {
            var service = CreateService();
            service.State.Player.Tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));
            var request = service.State.ChoiceQueue.ActiveChoice;
            var round = service.State.Round;

            var error = Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.NextTurn)));

            StringAssert.Contains("\u9970\u54c1", error.Message);
            Assert.AreEqual(round, service.State.Round);
            Assert.AreSame(request, service.State.ChoiceQueue.ActiveChoice);
        }

        [Test]
        public void NextTurn_WithAnomalyPending_BlocksAndKeepsChoice()
        {
            var service = CreateService();
            var request = CreatePendingMechanicChoice(AdvancedMechanicKind.Anomaly);
            service.State.Player.Tavern.AdvancedMechanics.PendingChoice = request;
            var round = service.State.Round;

            var error = Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.NextTurn)));

            StringAssert.Contains("\u7578\u53d8", error.Message);
            Assert.AreEqual(round, service.State.Round);
            Assert.AreSame(request, service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
        }

        [Test]
        public void DebugSkipToNextTurn_WithPendingChoice_BypassesNormalGuard()
        {
            var service = CreateService();
            var request = CreatePendingMechanicChoice(AdvancedMechanicKind.Quest);
            service.State.Player.Tavern.AdvancedMechanics.PendingChoice = request;
            var round = service.State.Round;

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(round + 1, service.State.Round);
            Assert.AreSame(request, service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
        }

        private static MatchService CreateService()
        {
            return MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
        }

        private static DiscoverState CreatePendingDiscover()
        {
            return new DiscoverState
            {
                Source = "test-pending-discover",
                RewardTier = 1,
                RemainingPicks = 1,
                Options = new List<MinionInstance>
                {
                    new MinionInstance
                    {
                        InstanceId = "test-discover-option",
                        CardId = "TEST_DISCOVER_OPTION",
                        Name = "Test Discover Option",
                        TavernTier = 1
                    }
                }
            };
        }

        private static MechanicChoiceRequest CreatePendingMechanicChoice(AdvancedMechanicKind kind)
        {
            return new MechanicChoiceRequest
            {
                RequestId = "test-" + kind.ToString().ToLowerInvariant() + "-choice",
                Kind = kind,
                Source = "test-choice",
                Slot = "Main",
                Round = 1,
                RemainingPicks = 1,
                Options = new List<MechanicChoiceOption>
                {
                    new MechanicChoiceOption
                    {
                        OptionId = "test-option",
                        Kind = kind,
                        DisplayName = "Test Option"
                    }
                }
            };
        }
    }
}
