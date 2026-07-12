using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LearnHearthstone.Tests.PlayMode
{
    public sealed class CorePlayerJourneyInputTests
    {
        [UnityTest]
        public IEnumerator PlayMode_PJ01_MainHubToTavernCompletesThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                MatchService service = null;
                Action openSetup = null;
                openSetup = () =>
                {
                    ClearChildren(scene.Root);
                    new UnityTavernTribeSelectionView(
                        scene.Root,
                        setup =>
                        {
                            ClearChildren(scene.Root);
                            service = MatchService.CreateWithDefaultCatalog(24680, new InMemoryTestScenarioRepository(), setup);
                            new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), openSetup).Build();
                        },
                        () => { },
                        UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();
                };

                new MainHubView(
                    scene.Root,
                    () => { },
                    () => { },
                    openSetup,
                    UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();

                yield return WaitForChild(scene.Root, "酒馆训练器Button");
                Click(scene, FindChild(scene.Root, "酒馆训练器Button"));
                yield return WaitForChild(scene.Root, "UnityTribeSelectionAllButton");

                Assert.AreEqual("选择本局种族", FindChild(scene.Root, "UnityTribeSelectionTitle").GetComponent<Text>().text);
                Click(scene, FindChild(scene.Root, "UnityTribeSelectionAllButton"));
                yield return WaitForChild(scene.Root, "UnityAdvancedMechanicsStartButton");

                Assert.IsNotNull(FindChild(scene.Root, "UnityAdvancedMechanicsSetupOverlay"));
                Click(scene, FindChild(scene.Root, "UnityAdvancedMechanicsStartButton"));
                yield return WaitForChild(scene.Root, "UnityQuickRefreshButton");

                Assert.IsNotNull(service);
                Assert.IsNotNull(FindChild(scene.Root, "UnityPlayerBoardZone"));
                Assert.IsNotNull(FindChild(scene.Root, "UnityHandZone"));
                Assert.IsNotNull(FindChild(scene.Root, "UnityShopZone"));
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_PJ02_BasicRecruitActionsCompleteThroughRealInput()
        {
            using (var scene = new JourneyScene())
            {
                var service = MatchService.CreateWithDefaultCatalog(13579, new InMemoryTestScenarioRepository());
                service.State.Player.Tavern.Gold = 20;
                service.State.Player.Tavern.MaxGold = 20;
                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(scene.Root, "UnityQuickRefreshButton");

                Click(scene, FindChild(scene.Root, "UnityQuickFreezeButton"));
                yield return WaitForState(() => service.State.Player.Tavern.Frozen, "freeze");
                yield return WaitForChild(scene.Root, "UnityQuickFreezeButton");
                Click(scene, FindChild(scene.Root, "UnityQuickFreezeButton"));
                yield return WaitForState(() => !service.State.Player.Tavern.Frozen, "unfreeze");

                var shopBefore = service.State.Player.Tavern.Shop.Select(card => card.InstanceId).ToArray();
                Click(scene, FindChild(scene.Root, "UnityQuickRefreshButton"));
                yield return WaitForState(
                    () => !shopBefore.SequenceEqual(service.State.Player.Tavern.Shop.Select(card => card.InstanceId)),
                    "shop refresh");

                var firstShopCard = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
                Click(scene, FindChild(scene.Root, "UnityCardAction-" + firstShopCard.InstanceId));
                yield return WaitForState(() => service.State.Player.Tavern.Hand.Any(card => card.InstanceId == firstShopCard.InstanceId), "first purchase");
                yield return WaitForChild(scene.Root, "UnityCard-" + firstShopCard.InstanceId);
                yield return Drag(scene, FindChild(scene.Root, "UnityCard-" + firstShopCard.InstanceId), DropTarget(scene.Root, UnityTavernDropTarget.PlayerBoard, 0));
                yield return WaitForState(() => service.State.Player.Board.Any(card => card.InstanceId == firstShopCard.InstanceId), "first play");

                var secondShopCard = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
                yield return WaitForChild(scene.Root, "UnityCardAction-" + secondShopCard.InstanceId);
                Click(scene, FindChild(scene.Root, "UnityCardAction-" + secondShopCard.InstanceId));
                yield return WaitForState(() => service.State.Player.Tavern.Hand.Any(card => card.InstanceId == secondShopCard.InstanceId), "second purchase");
                yield return WaitForChild(scene.Root, "UnityCard-" + secondShopCard.InstanceId);
                yield return Drag(scene, FindChild(scene.Root, "UnityCard-" + secondShopCard.InstanceId), DropTarget(scene.Root, UnityTavernDropTarget.PlayerBoard, 1));
                yield return WaitForState(() => service.State.Player.Board.Count >= 2, "second play");

                var movedId = service.State.Player.Board[1].InstanceId;
                yield return WaitForChild(scene.Root, "UnityCard-" + movedId);
                yield return Drag(scene, FindChild(scene.Root, "UnityCard-" + movedId), DropTarget(scene.Root, UnityTavernDropTarget.PlayerBoard, 0));
                yield return WaitForState(() => service.State.Player.Board[0].InstanceId == movedId, "board reorder");

                var soldId = service.State.Player.Board[0].InstanceId;
                yield return WaitForChild(scene.Root, "UnityCard-" + soldId);
                yield return Drag(scene, FindChild(scene.Root, "UnityCard-" + soldId), DropTarget(scene.Root, UnityTavernDropTarget.SellZone, -1));
                yield return WaitForState(() => service.State.Player.Board.All(card => card.InstanceId != soldId), "sell");

                var tierBefore = service.State.Player.Tavern.Tier;
                yield return WaitForChild(scene.Root, "UnityQuickUpgradeButton");
                Click(scene, FindChild(scene.Root, "UnityQuickUpgradeButton"));
                yield return WaitForState(() => service.State.Player.Tavern.Tier == tierBefore + 1, "upgrade");

                Assert.IsTrue(FindChild(scene.Root, "UnityQuickUpgradeButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(scene.Root, "UnityFeedbackToast").GetComponentsInChildren<Text>(true).Any(text => text.text == "酒馆已升级"));
                var recruitMessages = service.State.Player.Tavern.RecruitLog.Select(entry => entry.Message).ToList();
                Assert.IsTrue(recruitMessages.Any(message => message == "刷新酒馆"));
                Assert.IsTrue(recruitMessages.Any(message => message.StartsWith("购买 ", StringComparison.Ordinal)));
                Assert.IsTrue(recruitMessages.Any(message => message.StartsWith("打出 ", StringComparison.Ordinal)));
                Assert.IsTrue(recruitMessages.Any(message => message.StartsWith("调整站位 ", StringComparison.Ordinal)));
                Assert.IsTrue(recruitMessages.Any(message => message.StartsWith("出售 ", StringComparison.Ordinal)));
                Assert.IsTrue(recruitMessages.Any(message => message.StartsWith("升级到酒馆等级 ", StringComparison.Ordinal)));
                Assert.IsFalse(recruitMessages.Any(message => message.Contains("璐") || message.Contains("鎵") || message.Contains("鍑") || message.Contains("閰")));
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_PJ06_PJ07_PJ08_ToolsReplayAndReturnCompleteThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                var returned = false;
                var service = MatchService.CreateWithDefaultCatalog(97531, new InMemoryTestScenarioRepository());
                var visibleCards = service.State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion).Take(2).ToList();
                Assert.AreEqual(2, visibleCards.Count);
                var player = visibleCards[0].Clone();
                player.InstanceId = "pj07-player";
                player.Owner = BoardSide.Player;
                var opponent = visibleCards[1].Clone();
                opponent.InstanceId = "pj07-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Player.Board.Add(player);
                service.State.Opponent.Board.Add(opponent);

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => returned = true).Build();
                yield return WaitForChild(scene.Root, "UnityOpponentEntryButton");

                Click(scene, FindChild(scene.Root, "UnityOpponentEntryButton"));
                yield return WaitForChild(scene.Root, "UnityOpponentPanelCloseButton");
                Assert.IsNotNull(FindChild(scene.Root, "UnityOpponentPanel"));
                Click(scene, FindChild(scene.Root, "UnityOpponentPanelCloseButton"));
                yield return WaitForMissing(scene.Root, "UnityOpponentPanel");

                Click(scene, FindChild(scene.Root, "UnityQuickToolsButton"));
                yield return WaitForChild(scene.Root, "UnityTrainerToolsCloseButton");
                Assert.IsNotNull(FindChild(scene.Root, "UnityTrainerToolsOverlay"));
                Click(scene, FindChild(scene.Root, "UnityTrainerToolsCloseButton"));
                yield return WaitForMissing(scene.Root, "UnityTrainerToolsOverlay");

                Click(scene, FindChild(scene.Root, "UnityQuickNextTurnButton"));
                yield return WaitForChild(scene.Root, "UnityCombatBattlefieldRoot");
                Assert.IsNotNull(service.State.LastReplay);
                Assert.Greater(service.State.LastReplay.Frames.Count, 1);

                Click(scene, FindChild(scene.Root, "UnityReplayNextButton"));
                yield return WaitForChild(scene.Root, "UnityReplaySpeedButton");
                Click(scene, FindChild(scene.Root, "UnityReplaySpeedButton"));
                yield return WaitForChild(scene.Root, "UnityReplayPlayPauseButton");
                Click(scene, FindChild(scene.Root, "UnityReplayPlayPauseButton"));
                yield return null;
                yield return WaitForChild(scene.Root, "UnityReplayLastButton");
                Click(scene, FindChild(scene.Root, "UnityReplayLastButton"));
                yield return WaitForChild(scene.Root, "UnityCombatCloseButton");
                Click(scene, FindChild(scene.Root, "UnityCombatCloseButton"));
                yield return WaitForChild(scene.Root, "UnityBackButton");
                Assert.IsNotNull(service.State.LastReplay);

                Click(scene, FindChild(scene.Root, "UnityBackButton"));
                yield return WaitForChild(scene.Root, "UnityReturnConfirmNoButton");
                Click(scene, FindChild(scene.Root, "UnityReturnConfirmNoButton"));
                yield return WaitForMissing(scene.Root, "UnityReturnConfirmOverlay");
                Assert.IsFalse(returned);

                Click(scene, FindChild(scene.Root, "UnityBackButton"));
                yield return WaitForChild(scene.Root, "UnityReturnConfirmYesButton");
                Click(scene, FindChild(scene.Root, "UnityReturnConfirmYesButton"));
                yield return WaitForState(() => returned, "confirmed return");
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_PJ03_PJ05_GeorgeTargetingShieldAndReplayCompleteThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                var service = MatchService.CreateWithDefaultCatalog(
                    86420,
                    new InMemoryTestScenarioRepository(),
                    new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_15" });
                service.State.Player.Tavern.Gold = 5;
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var source = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
                var target = source.Clone();
                target.InstanceId = "pj03-george-target";
                target.Owner = BoardSide.Player;
                target.Attack = target.BaseAttack = 1;
                target.Health = target.MaxHealth = target.BaseHealth = 100;
                target.Keywords.Remove(Keyword.DivineShield);
                var enemy = source.Clone();
                enemy.InstanceId = "pj03-george-enemy";
                enemy.Owner = BoardSide.Opponent;
                enemy.Attack = enemy.BaseAttack = 1;
                enemy.Health = enemy.MaxHealth = enemy.BaseHealth = 100;
                enemy.Keywords.Clear();
                service.State.Player.Board.Add(target);
                service.State.Opponent.Board.Add(enemy);

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(scene.Root, "UnityQuickHeroPowerButton");

                var goldBefore = service.State.Player.Tavern.Gold;
                Click(scene, FindChild(scene.Root, "UnityQuickHeroPowerButton"));
                yield return WaitForChild(scene.Root, "UnityTargetingCancelButton");
                Assert.AreEqual("可选", FindChild(FindChild(scene.Root, "UnityCard-" + target.InstanceId), "UnityTargetingLabelText").GetComponent<Text>().text);
                Click(scene, FindChild(scene.Root, "UnityTargetingCancelButton"));
                yield return WaitForMissing(scene.Root, "UnityTargetingCancelButton");
                Assert.AreEqual(goldBefore, service.State.Player.Tavern.Gold);

                Click(scene, FindChild(scene.Root, "UnityOpponentEntryButton"));
                yield return WaitForChild(scene.Root, "UnityCard-" + enemy.InstanceId);
                yield return Drag(scene, FindChild(scene.Root, "UnityQuickHeroPowerButton"), DropTarget(scene.Root, UnityTavernDropTarget.OpponentBoard, 0));
                yield return WaitForChild(scene.Root, "UnityErrorToast");
                Assert.AreEqual(goldBefore, service.State.Player.Tavern.Gold);
                Assert.IsFalse(enemy.Keywords.Contains(Keyword.DivineShield));

                Click(scene, FindChild(scene.Root, "UnityOpponentPanelCloseButton"));
                yield return WaitForChild(scene.Root, "UnityQuickHeroPowerButton");
                yield return Drag(scene, FindChild(scene.Root, "UnityQuickHeroPowerButton"), DropTarget(scene.Root, UnityTavernDropTarget.PlayerBoard, 0));
                yield return WaitForState(() => target.Keywords.Contains(Keyword.DivineShield), "George Divine Shield");
                Assert.AreEqual(goldBefore - 1, service.State.Player.Tavern.Gold);

                yield return WaitForChild(scene.Root, "UnityQuickNextTurnButton");
                Click(scene, FindChild(scene.Root, "UnityQuickNextTurnButton"));
                yield return WaitForChild(scene.Root, "UnityCombatCurrentEventText");
                var shieldFrame = service.State.LastReplay.Frames.FindIndex(frame => frame.EventType == CombatEventType.DivineShieldBroken);
                Assert.GreaterOrEqual(shieldFrame, 0);
                for (var index = 0; index < shieldFrame; index += 1)
                {
                    yield return WaitForChild(scene.Root, "UnityReplayNextButton");
                    Click(scene, FindChild(scene.Root, "UnityReplayNextButton"));
                    yield return null;
                }

                yield return WaitForChild(scene.Root, "UnityCombatCurrentEventText");
                StringAssert.Contains("圣盾破裂", FindChild(scene.Root, "UnityCombatCurrentEventText").GetComponent<Text>().text);
            }
        }

        private sealed class JourneyScene : IDisposable
        {
            private readonly GameObject canvasObject;
            private readonly GameObject eventSystemObject;

            public JourneyScene()
            {
                canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                Canvas = canvasObject.GetComponent<Canvas>();
                Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);

                var rootObject = new GameObject("Root", typeof(RectTransform));
                rootObject.transform.SetParent(canvasObject.transform, false);
                Root = rootObject.transform;
                UnityTavernUiStyle.Stretch(rootObject.GetComponent<RectTransform>());
            }

            public Transform Root { get; }
            public Canvas Canvas { get; }
            public EventSystem EventSystem => eventSystemObject.GetComponent<EventSystem>();
            public GraphicRaycaster Raycaster => canvasObject.GetComponent<GraphicRaycaster>();

            public void Dispose()
            {
                UnityEngine.Object.Destroy(canvasObject);
                UnityEngine.Object.Destroy(eventSystemObject);
            }
        }

        private static void Click(JourneyScene scene, Transform target)
        {
            var pointer = PointerAt(scene, target.GetComponent<RectTransform>());
            var hit = Raycast(scene, pointer, target);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static void ClickAtNormalized(JourneyScene scene, Transform target, Vector2 normalized)
        {
            var rect = target.GetComponent<RectTransform>();
            var local = new Vector3(
                Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, normalized.x),
                Mathf.Lerp(rect.rect.yMin, rect.rect.yMax, normalized.y));
            var pointer = new PointerEventData(scene.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local))
            };
            var hit = Raycast(scene, pointer, target);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static IEnumerator Drag(JourneyScene scene, Transform source, UnityTavernDropTargetBehaviour target)
        {
            var sourcePointer = PointerAt(scene, source.GetComponent<RectTransform>());
            var sourceHit = Raycast(scene, sourcePointer, source);
            ExecuteEvents.ExecuteHierarchy(sourceHit, sourcePointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(sourceHit, sourcePointer, ExecuteEvents.beginDragHandler);
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.IsTrue(target.gameObject.activeInHierarchy, target.name + " did not become visible after drag began.");

            var targetPointer = PointerAt(scene, target.GetComponent<RectTransform>());
            var targetHit = Raycast(scene, targetPointer, target.transform);
            ExecuteEvents.ExecuteHierarchy(sourceHit, targetPointer, ExecuteEvents.dragHandler);
            ExecuteEvents.ExecuteHierarchy(targetHit, targetPointer, ExecuteEvents.pointerEnterHandler);
            yield return null;
            ExecuteEvents.ExecuteHierarchy(targetHit, targetPointer, ExecuteEvents.dropHandler);
            ExecuteEvents.ExecuteHierarchy(sourceHit, targetPointer, ExecuteEvents.endDragHandler);
            ExecuteEvents.ExecuteHierarchy(sourceHit, targetPointer, ExecuteEvents.pointerUpHandler);
        }

        private static PointerEventData PointerAt(JourneyScene scene, RectTransform target)
        {
            return new PointerEventData(scene.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center))
            };
        }

        private static GameObject Raycast(JourneyScene scene, PointerEventData pointer, Transform expected)
        {
            var hits = new List<RaycastResult>();
            scene.Raycaster.Raycast(pointer, hits);
            var hit = hits.FirstOrDefault(result => result.gameObject == expected.gameObject || result.gameObject.transform.IsChildOf(expected));
            Assert.IsNotNull(hit.gameObject, expected.name + " was not reachable through GraphicRaycaster.");
            return hit.gameObject;
        }

        private static UnityTavernDropTargetBehaviour DropTarget(Transform root, UnityTavernDropTarget target, int index)
        {
            var result = root.GetComponentsInChildren<UnityTavernDropTargetBehaviour>(true)
                .FirstOrDefault(candidate => candidate.Target == target && candidate.TargetIndex == index);
            Assert.IsNotNull(result, "Missing drop target " + target + " at " + index + ".");
            return result;
        }

        private static Transform FindChild(Transform root, string name)
        {
            var child = FindChildOrNull(root, name);
            Assert.IsNotNull(child, "Missing child: " + name);
            return child;
        }

        private static Transform FindChildOrNull(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (var index = 0; index < root.childCount; index += 1)
            {
                var child = FindChildOrNull(root.GetChild(index), name);
                if (child != null) return child;
            }

            return null;
        }

        private static IEnumerator WaitForChild(Transform root, string name)
        {
            Transform stableChild = null;
            var stableFrames = 0;
            for (var frame = 0; frame < 90; frame += 1)
            {
                Canvas.ForceUpdateCanvases();
                var child = FindChildOrNull(root, name);
                if (child != null && child == stableChild)
                {
                    if (++stableFrames >= 2) yield break;
                }
                else
                {
                    stableChild = child;
                    stableFrames = child == null ? 0 : 1;
                }

                yield return null;
            }

            Assert.Fail("Timed out waiting for child: " + name);
        }

        private static IEnumerator WaitForState(Func<bool> condition, string operation)
        {
            for (var frame = 0; frame < 90; frame += 1)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                if (condition()) yield break;
            }

            Assert.Fail("Timed out waiting for " + operation + ".");
        }

        private static IEnumerator WaitForMissing(Transform root, string name)
        {
            for (var frame = 0; frame < 90; frame += 1)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                if (FindChildOrNull(root, name) == null) yield break;
            }

            Assert.Fail("Timed out waiting for removal: " + name);
        }

        private static void ClearChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index -= 1)
            {
                UnityEngine.Object.Destroy(root.GetChild(index).gameObject);
            }
        }
    }
}
