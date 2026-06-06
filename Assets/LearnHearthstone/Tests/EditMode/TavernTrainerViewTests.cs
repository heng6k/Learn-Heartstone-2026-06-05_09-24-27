using System;
using System.Linq;
using System.Reflection;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.TavernTrainer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TavernTrainerViewTests
    {
        [Test]
        public void Build_AfterBuyingMinion_ShowsMinionNameInHandPanel()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);
                service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
                var boughtName = service.State.Player.Tavern.Hand[0].Name;

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var handPanel = FindChild(rootObject.transform, "HandPanel");
                Assert.IsNotNull(handPanel);
                var labels = handPanel.GetComponentsInChildren<Text>();
                Assert.IsTrue(labels.Any(label => label.text == boughtName), "Expected hand panel to show " + boughtName);
                Assert.IsFalse(labels.Any(label => label.text == "打出"), "Hand cards should use drag drop instead of action buttons.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_AfterPlayingMinion_DoesNotShowSellButtonOnBoardCard()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);
                service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
                service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
                var playedName = service.State.Player.Board[0].Name;

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var boardPanel = FindChild(rootObject.transform, "玩家战场Panel");
                Assert.IsNotNull(boardPanel);
                var labels = boardPanel.GetComponentsInChildren<Text>();
                Assert.IsTrue(labels.Any(label => label.text == playedName), "Expected player board to show " + playedName);
                Assert.IsFalse(labels.Any(label => label.text == "出售"), "Board cards should use drag drop instead of action buttons.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_UsesDragCardsAndDropZonesForRecruitActions()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var shopCard = FindChild(rootObject.transform, "Card-" + service.State.Player.Tavern.Shop[0].InstanceId);
                var handPanel = FindChild(rootObject.transform, "HandPanel");
                var sellDropZone = FindChild(rootObject.transform, "SellDropZone");
                Assert.IsNotNull(shopCard);
                Assert.IsNotNull(handPanel);
                Assert.IsNotNull(sellDropZone);
                Assert.IsTrue(HasBehaviourNamed(shopCard, "DragCardBehaviour"), "Shop cards should be draggable.");
                Assert.IsTrue(HasBehaviourNamed(handPanel, "DropTargetBehaviour"), "Hand panel should accept bought cards.");
                Assert.IsTrue(HasBehaviourNamed(sellDropZone, "DropTargetBehaviour"), "Sell zone should accept board minions.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_PlacesSellDropZoneInShopStageAsLargeTarget()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var shopStage = FindChild(rootObject.transform, "ShopStage");
                var rightInspector = FindChild(rootObject.transform, "RightInspector");
                var sellDropZone = FindChild(rootObject.transform, "SellDropZone");

                Assert.IsNotNull(shopStage);
                Assert.IsNotNull(rightInspector);
                Assert.IsNotNull(sellDropZone);
                Assert.IsTrue(IsDescendantOf(sellDropZone, shopStage), "Sell drop zone should sit in the wide shop-stage area.");
                Assert.IsFalse(IsDescendantOf(sellDropZone, rightInspector), "Sell drop zone should no longer live in the narrow right inspector.");
                Assert.GreaterOrEqual(sellDropZone.GetComponent<RectTransform>().sizeDelta.y, 72f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void DropCommand_PlayerBoardToHandReturnsMoveMinionCommand()
        {
            var minion = new MinionInstance
            {
                InstanceId = "player-golden",
                DefinitionId = "m1",
                Name = "m1",
                Owner = BoardSide.Player
            };

            var command = BuildDropCommandByReflection(minion, "PlayerBoard", "Hand");

            Assert.IsNotNull(command);
            Assert.AreEqual(GameCommandType.MoveMinion, command.Type);
            Assert.AreEqual(minion.InstanceId, command.InstanceId);
        }

        [Test]
        public void DropCommand_HandToPlayerBoardSlotUsesTargetIndex()
        {
            var minion = new MinionInstance
            {
                InstanceId = "hand-minion",
                DefinitionId = "m1",
                Name = "m1",
                Owner = BoardSide.Player
            };

            var command = BuildDropCommandByReflection(minion, "Hand", "PlayerBoard", 3);

            Assert.IsNotNull(command);
            Assert.AreEqual(GameCommandType.PlayMinion, command.Type);
            Assert.AreEqual(0, command.Index);
            Assert.AreEqual(3, command.TargetIndex);
        }

        [Test]
        public void DropCommand_PlayerBoardToPlayerBoardSlotUsesReorderCommand()
        {
            var minion = new MinionInstance
            {
                InstanceId = "board-minion",
                DefinitionId = "m1",
                Name = "m1",
                Owner = BoardSide.Player
            };

            var command = BuildDropCommandByReflection(minion, "PlayerBoard", "PlayerBoard", 1);

            Assert.IsNotNull(command);
            Assert.AreEqual(GameCommandType.MoveBoardMinion, command.Type);
            Assert.AreEqual(minion.InstanceId, command.InstanceId);
            Assert.AreEqual(1, command.TargetIndex);
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var match = FindChild(child, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool HasBehaviourNamed(Transform target, string typeName)
        {
            return target.GetComponents<MonoBehaviour>().Any(component => component != null && component.GetType().Name == typeName);
        }

        private static bool IsDescendantOf(Transform child, Transform ancestor)
        {
            var current = child;
            while (current != null)
            {
                if (current == ancestor)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static GameCommand BuildDropCommandByReflection(MinionInstance minion, string sourceName, string targetName, int targetIndex = -1)
        {
            var viewType = typeof(TavernTrainerView);
            var assembly = viewType.Assembly;
            var dragContextType = viewType.GetNestedType("DragContext", BindingFlags.NonPublic);
            var dragSourceType = assembly.GetType("LearnHearthstone.Presentation.TavernTrainer.DragSource");
            var dropTargetType = assembly.GetType("LearnHearthstone.Presentation.TavernTrainer.DropTarget");
            var method = viewType.GetMethod("TryBuildDropCommand", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(dragContextType);
            Assert.IsNotNull(dragSourceType);
            Assert.IsNotNull(dropTargetType);
            Assert.IsNotNull(method);

            var dragContext = Activator.CreateInstance(dragContextType, true);
            dragContextType.GetField("Minion").SetValue(dragContext, minion);
            dragContextType.GetField("Source").SetValue(dragContext, Enum.Parse(dragSourceType, sourceName));
            dragContextType.GetField("Index").SetValue(dragContext, 0);

            var args = new object[]
            {
                dragContext,
                Enum.Parse(dropTargetType, targetName),
                targetIndex,
                null
            };

            var handled = (bool)method.Invoke(null, args);
            return handled ? (GameCommand)args[3] : null;
        }
    }
}
