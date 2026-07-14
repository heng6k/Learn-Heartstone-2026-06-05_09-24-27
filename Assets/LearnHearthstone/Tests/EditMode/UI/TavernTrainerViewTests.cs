using System;
using System.Collections.Generic;
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
        public void Build_PlayerBoardShowsTribeDistributionWithMostCommonHighlighted()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);
                service.State.Player.Board.Clear();
                service.State.Player.Board.Add(TestBoardMinion("dragon-1", "Dragon One", Tribe.Dragon));
                service.State.Player.Board.Add(TestBoardMinion("dragon-murloc", "Dragon Murloc", Tribe.Dragon, Tribe.Murloc));

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var row = FindChild(rootObject.transform, "PlayerBoardTribeDistribution");
                var textTransform = FindChild(rootObject.transform, "PlayerBoardTribeDistributionText");
                Assert.IsNotNull(row);
                Assert.IsNotNull(textTransform);
                var text = textTransform.GetComponent<Text>();
                Assert.IsNotNull(text);
                Assert.IsTrue(text.supportRichText);
                Assert.IsTrue(text.text.Contains("<color=#F1C968><b>龙 2</b></color>"), text.text);
                Assert.IsTrue(text.text.Contains("鱼人 1"), text.text);
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
        public void Build_PlayerBoardShowsTargetPositionAndEmptySlotState()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);
                service.State.Player.Board.Clear();
                service.State.Player.Board.Add(TestBoardMinion("board-a", "Board A", Tribe.Beast));

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var badge = FindChild(rootObject.transform, "DropIntentBadge");
                var emptySlot = FindChild(rootObject.transform, "EmptySlotText");
                Assert.IsNotNull(badge);
                Assert.IsNotNull(emptySlot);
                Assert.That(badge.GetComponent<Text>().text, Does.Contain("目标 / 位置 1"));
                Assert.AreEqual("位置", emptySlot.GetComponent<Text>().text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_DiscoverPanelShowsSourceAndOptionState()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);
                service.State.Player.Tavern.Discover = new DiscoverState
                {
                    Source = "arena-showman",
                    RewardTier = 4,
                    Options = new List<MinionInstance> { TestBoardMinion("discover-a", "Discover A", Tribe.Elemental) }
                };

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var strip = FindChild(rootObject.transform, "DiscoverStateStrip");
                var text = FindChild(rootObject.transform, "DiscoverStateText");
                Assert.IsNotNull(strip);
                Assert.IsNotNull(text);
                Assert.That(text.GetComponent<Text>().text, Does.Contain("arena-showman"));
                Assert.That(text.GetComponent<Text>().text, Does.Contain("选项 1"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CardUsesOfficialKeywordsForDisplay()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);
                service.State.Player.Tavern.Shop.Clear();
                service.State.Player.Tavern.Hand.Clear();
                service.State.Player.Board.Clear();
                var minion = TestBoardMinion("official-keyword-card", "Official Keyword Card", Tribe.Beast);
                minion.Keywords = new List<Keyword> { Keyword.Battlecry };
                minion.OfficialKeywords = new List<Keyword> { Keyword.Taunt };
                service.State.Player.Board.Add(minion);

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var card = FindChild(rootObject.transform, "Card-official-keyword-card");
                Assert.IsNotNull(card);
                var labels = card.GetComponentsInChildren<Text>();
                Assert.IsTrue(labels.Any(label => label.text == "嘲讽"));
                Assert.IsFalse(labels.Any(label => label.text == "战吼"));
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
        public void Build_RightInspectorCreatesTabButtons()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var pageScroll = FindChild(rootObject.transform, "TavernTrainerPageScroll");
                Assert.IsNotNull(pageScroll);
                var pageScrollRect = pageScroll.GetComponent<ScrollRect>();
                Assert.IsNotNull(pageScrollRect);
                Assert.IsNotNull(pageScrollRect.verticalScrollbar);
                Assert.IsNotNull(FindChild(rootObject.transform, "TopStatusRow"));
                Assert.IsNotNull(FindChild(rootObject.transform, "TopActionRow"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RightInspectorTabs"));
                Assert.IsNotNull(FindChild(rootObject.transform, "Tab-Info"));
                Assert.IsNotNull(FindChild(rootObject.transform, "Tab-CardAcquisition"));
                Assert.IsNotNull(FindChild(rootObject.transform, "Tab-OpponentCustomization"));
                Assert.IsNotNull(FindChild(rootObject.transform, "Tab-BattleTest"));
                Assert.IsNotNull(FindChild(rootObject.transform, "StartCombatButton"));
                var scroll = FindChild(rootObject.transform, "RightInspectorScroll");
                Assert.IsNotNull(scroll);
                var scrollRect = scroll.GetComponent<ScrollRect>();
                Assert.IsNotNull(scrollRect);
                Assert.IsNotNull(scrollRect.verticalScrollbar);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_LegacyPolishUsesTouchSizedControlsAndTintStates()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var accent = FindChild(rootObject.transform, "TopToolbarAccent");
                var tabs = FindChild(rootObject.transform, "RightInspectorTabs");
                var infoTab = FindChild(rootObject.transform, "Tab-Info").GetComponent<Button>();
                var sellDropZone = FindChild(rootObject.transform, "SellDropZone");

                Assert.IsNotNull(accent);
                Assert.GreaterOrEqual(accent.GetComponent<LayoutElement>().preferredHeight, 4f);
                Assert.GreaterOrEqual(tabs.GetComponent<LayoutElement>().preferredHeight, 44f);
                Assert.AreEqual(Selectable.Transition.ColorTint, infoTab.transition);
                Assert.AreNotEqual(infoTab.colors.normalColor, infoTab.colors.highlightedColor);
                Assert.GreaterOrEqual(sellDropZone.GetComponent<LayoutElement>().preferredHeight, 88f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_LegacyCardsUsePolishedSurfacesAndPointerStates()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var card = FindChild(rootObject.transform, "Card-" + service.State.Player.Tavern.Shop[0].InstanceId);
                var holder = card.parent;
                var button = card.GetComponent<Button>();

                Assert.IsNotNull(card);
                Assert.GreaterOrEqual(holder.GetComponent<LayoutElement>().minWidth, 124f);
                Assert.GreaterOrEqual(holder.GetComponent<LayoutElement>().minHeight, 150f);
                Assert.AreEqual(Selectable.Transition.ColorTint, button.transition);
                Assert.AreNotEqual(button.colors.normalColor, button.colors.highlightedColor);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CardAcquisitionTabOpensCenteredModal()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);
                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                FindChild(rootObject.transform, "Tab-CardAcquisition").GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(FindChild(rootObject.transform, "CardAcquisitionPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "CardAcquisitionModalOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "CardAcquisitionModal"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AcquisitionSpellModeButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AcquisitionMinionModeButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AcquisitionTierRail"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AcquisitionCenterPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AcquisitionTypeRail"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AcquisitionCardGridScroll"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AddCardToHandButton"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CardAcquisitionModalFiltersByKindTierAndType()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);
                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                FindChild(rootObject.transform, "Tab-CardAcquisition").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "AcquisitionMinionModeButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "AcquisitionTier1Button").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "AcquisitionTypeBeastButton").GetComponent<Button>().onClick.Invoke();

                var subtitle = FindChild(rootObject.transform, "AcquisitionSubtitle").GetComponent<Text>().text;
                Assert.That(subtitle, Does.Contain("1 本"));
                Assert.That(subtitle, Does.Contain("野兽"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AcquisitionTypeAllButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AcquisitionTypeNoneButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AcquisitionCardGridScroll"));

                FindChild(rootObject.transform, "AcquisitionTier5Button").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "AcquisitionTypeNoneButton").GetComponent<Button>().onClick.Invoke();

                subtitle = FindChild(rootObject.transform, "AcquisitionSubtitle").GetComponent<Text>().text;
                Assert.That(subtitle, Does.Contain("5 本"));
                Assert.That(subtitle, Does.Contain("中立"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AcquisitionCardCell-BG_LOE_077"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CardAcquisitionModalAddButtonAddsCardToHand()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);
                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                FindChild(rootObject.transform, "Tab-CardAcquisition").GetComponent<Button>().onClick.Invoke();
                var before = service.State.Player.Tavern.Hand.Count;
                FindChild(rootObject.transform, "AddCardToHandButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(before + 1, service.State.Player.Tavern.Hand.Count);
                Assert.IsNotNull(FindChild(rootObject.transform, "CardAcquisitionModal"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_OpponentCustomizationTabShowsEditingEntries()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345);
                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                FindChild(rootObject.transform, "Tab-OpponentCustomization").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "OpponentCustomizationPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "AddOpponentButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "MoveOpponentLeftButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "MoveOpponentRightButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RemoveOpponentButton"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_BattleTestTabShowsScenarioAndCombatControls()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                FindChild(rootObject.transform, "Tab-BattleTest").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "BattleTestPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "ScenarioNameInput"));
                Assert.IsNotNull(FindChild(rootObject.transform, "SaveScenarioButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "LoadScenarioButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "CombatSeedInput"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RunCombatTestButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "ResetCombatSnapshotButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "ScenarioList"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_StartCombatButtonRunsBattleFromCurrentBoards()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var player = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
                player.InstanceId = "ui-player";
                player.Owner = BoardSide.Player;
                var opponent = service.State.Player.Tavern.Shop.Last(card => card.CardKind == CardKind.Minion).Clone();
                opponent.InstanceId = "ui-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Player.Board.Add(player);
                service.State.Opponent.Board.Add(opponent);

                new TavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                FindChild(rootObject.transform, "StartCombatButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(MatchPhase.Result, service.State.Phase);
                Assert.IsNotNull(service.State.LastResult);
                Assert.IsTrue(service.State.CombatLog.Count > 0);
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
        public void DropCommand_BloodGemToTavernShopUsesTavernTargetZone()
        {
            var gem = new MinionInstance
            {
                InstanceId = "legacy-blood-gem",
                CardKind = CardKind.Spell,
                Keywords = new List<Keyword> { Keyword.BloodGem },
                Tags = new List<string> { "targeted_spell", "blood_gem" }
            };

            var command = BuildDropCommandByReflection(gem, "Hand", "TavernShop", 2);

            Assert.IsNotNull(command);
            Assert.AreEqual(GameCommandType.PlayMinion, command.Type);
            Assert.AreEqual(2, command.TargetIndex);
            Assert.AreEqual(TargetZone.TavernShop, command.TargetZone);
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

        private static MinionInstance TestBoardMinion(string id, string name, params Tribe[] tribes)
        {
            return new MinionInstance
            {
                InstanceId = id,
                DefinitionId = id,
                CardId = id,
                Name = name,
                Attack = 2,
                Health = 2,
                MaxHealth = 2,
                TavernTier = 1,
                Tribes = new List<Tribe>(tribes),
                Owner = BoardSide.Player
            };
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
