using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer.Realistic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class RealisticTavernTrainerViewTests
    {
        [Test]
        public void MainHub_BuildRoutesPrimaryTavernEntryToUnityAndHidesOldTavernEntries()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var legacyOpened = false;
                var realisticOpened = false;
                var unityOpened = false;

                new MainHubView(rootObject.transform, () => legacyOpened = true, () => realisticOpened = true, () => unityOpened = true).Build();

                FindChild(rootObject.transform, "MainHubPrimaryStartButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(unityOpened);
                Assert.IsFalse(legacyOpened);
                Assert.IsFalse(realisticOpened);
                Assert.IsNull(FindChild(rootObject.transform, "真实酒馆 UIButton"));
                Assert.IsNull(FindChild(rootObject.transform, "Unity 组件酒馆 UIButton"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CreatesRealisticTavernZonesAndStableSlots()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new RealisticTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticTavernTrainer"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticTopStatusBar"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticShopStage"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticPlayerBoard"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticHandDock"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticActionPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticNextTurnButton"));
                Assert.IsNull(FindChild(rootObject.transform, "RealisticCombatButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticTrainerDrawer"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticDrawerTabs"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticDrawerScroll"));
                Assert.AreEqual(7, FindChildren(rootObject.transform, "RealisticBoardSlot-").Count);
                Assert.AreEqual(10, FindChildren(rootObject.transform, "RealisticHandSlot-").Count);
                Assert.GreaterOrEqual(FindChildren(rootObject.transform, "CardArtImage").Count, service.State.Player.Tavern.Shop.Count(card => card != null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_DrawerTabsExposeOldTrainerFeatureGroups()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new RealisticTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                FindChild(rootObject.transform, "RealisticDrawerTabOpponent").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticOpponentCustomizationPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticAddOpponentButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticMoveOpponentLeftButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticMoveOpponentRightButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticRemoveOpponentButton"));

                FindChild(rootObject.transform, "RealisticDrawerTabBattle").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticBattleTestPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticScenarioNameInput"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticSaveScenarioButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticLoadScenarioButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticCombatSeedInput"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticCombatDebugOnlyHint"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticRunCombatTestButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticResetCombatSnapshotButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticScenarioList"));

                FindChild(rootObject.transform, "RealisticDrawerTabDebug").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticDebugPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticAddCardButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticAddCardToHandButton"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_RefreshAndDebugButtonsApplyMatchCommands()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var startingGold = service.State.Player.Tavern.Gold;

                new RealisticTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                FindChild(rootObject.transform, "RealisticRefreshButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(startingGold - 1, service.State.Player.Tavern.Gold);

                FindChild(rootObject.transform, "RealisticDrawerTabDebug").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "RealisticAddCardButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_PrimaryNextTurnCompletesCombatAndNextTurn()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var player = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
                player.InstanceId = "realistic-quick-player";
                player.Owner = BoardSide.Player;
                var opponent = service.State.Player.Tavern.Shop.Last(card => card.CardKind == CardKind.Minion).Clone();
                opponent.InstanceId = "realistic-quick-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Player.Board.Add(player);
                service.State.Opponent.Board.Add(opponent);

                new RealisticTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                FindChild(rootObject.transform, "RealisticNextTurnButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(2, service.State.Round);
                Assert.AreEqual(MatchPhase.Tavern, service.State.Phase);
                Assert.IsNotNull(service.State.LastResult);
                Assert.IsNotNull(service.State.LastReplay);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_BattleDrawerRunsCombatFromCurrentBoards()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var player = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
                player.InstanceId = "realistic-player";
                player.Owner = BoardSide.Player;
                var opponent = service.State.Player.Tavern.Shop.Last(card => card.CardKind == CardKind.Minion).Clone();
                opponent.InstanceId = "realistic-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Player.Board.Add(player);
                service.State.Opponent.Board.Add(opponent);

                new RealisticTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                FindChild(rootObject.transform, "RealisticDrawerTabBattle").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "RealisticRunCombatTestButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(MatchPhase.Result, service.State.Phase);
                Assert.IsNotNull(service.State.LastResult);
                Assert.IsNotNull(FindChild(rootObject.transform, "RealisticCombatReplayDebugger"));
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
                InstanceId = "realistic-board-minion",
                DefinitionId = "m1",
                Name = "m1",
                Owner = BoardSide.Player
            };

            var command = BuildRealisticDropCommandByReflection(minion, "PlayerBoard", "Hand");

            Assert.IsNotNull(command);
            Assert.AreEqual(GameCommandType.MoveMinion, command.Type);
            Assert.AreEqual(minion.InstanceId, command.InstanceId);
        }

        [Test]
        public void DropCommand_DiscoverToHandReturnsChooseDiscoverCommand()
        {
            var minion = new MinionInstance
            {
                InstanceId = "realistic-discover",
                DefinitionId = "m1",
                Name = "m1",
                Owner = BoardSide.Player
            };

            var command = BuildRealisticDropCommandByReflection(minion, "Discover", "Hand", 2);

            Assert.IsNotNull(command);
            Assert.AreEqual(GameCommandType.ChooseDiscover, command.Type);
            Assert.AreEqual(2, command.Index);
        }

        [Test]
        public void DropCommand_BloodGemToTavernShopUsesTavernTargetZone()
        {
            var gem = new MinionInstance
            {
                InstanceId = "realistic-blood-gem",
                CardKind = CardKind.Spell,
                Keywords = new List<Keyword> { Keyword.BloodGem },
                Tags = new List<string> { "targeted_spell", "blood_gem" }
            };

            var command = BuildRealisticDropCommandByReflection(gem, "Hand", "TavernShop", 2);

            Assert.IsNotNull(command);
            Assert.AreEqual(GameCommandType.PlayMinion, command.Type);
            Assert.AreEqual(2, command.TargetIndex);
            Assert.AreEqual(TargetZone.TavernShop, command.TargetZone);
        }

        [Test]
        public void TavernCardView_OfficialFullCardUsesCenteredFullImageWithoutPortraitCrop()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var card = new MinionInstance
                {
                    InstanceId = "official-card-art",
                    DefinitionId = "BG20_100",
                    CardId = "BG20_100",
                    Name = "Official Card Art",
                    CardKind = CardKind.Minion,
                    TavernTier = 1,
                    Attack = 2,
                    Health = 1,
                    MaxHealth = 1,
                    ImagePath = "CardImages/BG20_100",
                    Tribes = new List<Tribe> { Tribe.Quilboar },
                    Keywords = new List<Keyword>()
                };

                TavernCardView.Create(rootObject.transform, card, TavernCardVisualMode.Shop, null);

                var art = FindChild(rootObject.transform, "CardArtImage").GetComponent<Image>();
                var rect = art.GetComponent<RectTransform>();
                Assert.IsNotNull(art.sprite);
                Assert.IsTrue(art.preserveAspect);
                Assert.AreEqual(Vector2.zero, rect.anchorMin);
                Assert.AreEqual(Vector2.one, rect.anchorMax);
                Assert.AreEqual(new Vector2(2f, 2f), rect.offsetMin);
                Assert.AreEqual(new Vector2(-2f, -2f), rect.offsetMax);
                Assert.IsNull(FindChild(rootObject.transform, "CardPortraitMask"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TavernCardView_MissingArtUsesFallbackColorWithoutThrowing()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var card = new MinionInstance
                {
                    InstanceId = "missing-art",
                    DefinitionId = "missing-art",
                    CardId = "MISSING_ART",
                    Name = "Missing Art",
                    CardKind = CardKind.Minion,
                    TavernTier = 1,
                    Attack = 1,
                    Health = 1,
                    MaxHealth = 1,
                    ImagePath = "CardImages/does-not-exist",
                    Tribes = new List<Tribe> { Tribe.Beast },
                    Keywords = new List<Keyword>()
                };

                Assert.DoesNotThrow(() => TavernCardView.Create(rootObject.transform, card, TavernCardVisualMode.Shop, null));
                var art = FindChild(rootObject.transform, "CardArtImage").GetComponent<Image>();
                Assert.IsNull(art.sprite);
                Assert.AreNotEqual(Color.white, art.color);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                var found = FindChild(root.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static List<Transform> FindChildren(Transform root, string namePrefix)
        {
            var results = new List<Transform>();
            Collect(root, namePrefix, results);
            return results;
        }

        private static void Collect(Transform root, string namePrefix, List<Transform> results)
        {
            if (root.name.StartsWith(namePrefix))
            {
                results.Add(root);
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                Collect(root.GetChild(index), namePrefix, results);
            }
        }

        private static GameCommand BuildRealisticDropCommandByReflection(MinionInstance card, string sourceName, string targetName, int targetIndex = -1)
        {
            var viewType = typeof(RealisticTavernTrainerView);
            var assembly = viewType.Assembly;
            var dragContextType = viewType.GetNestedType("DragContext", BindingFlags.NonPublic);
            var dragSourceType = assembly.GetType("LearnHearthstone.Presentation.TavernTrainer.Realistic.RealisticDragSource");
            var dropTargetType = assembly.GetType("LearnHearthstone.Presentation.TavernTrainer.Realistic.RealisticDropTarget");
            var method = viewType.GetMethod("TryBuildDropCommand", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(dragContextType);
            Assert.IsNotNull(dragSourceType);
            Assert.IsNotNull(dropTargetType);
            Assert.IsNotNull(method);

            var dragContext = Activator.CreateInstance(dragContextType, true);
            dragContextType.GetField("Card").SetValue(dragContext, card);
            dragContextType.GetField("Source").SetValue(dragContext, Enum.Parse(dragSourceType, sourceName));
            dragContextType.GetField("Index").SetValue(dragContext, targetIndex < 0 ? 0 : targetIndex);

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
