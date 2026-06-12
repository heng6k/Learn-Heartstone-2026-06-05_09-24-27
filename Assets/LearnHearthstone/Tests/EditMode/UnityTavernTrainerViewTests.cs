using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class UnityTavernTrainerViewTests
    {
        [Test]
        public void MainHub_BuildCreatesUnityComponentTavernEntry()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var legacyOpened = false;
                var opened = false;
                new MainHubView(rootObject.transform, () => legacyOpened = true, () => { }, () => opened = true).Build();

                FindChild(rootObject.transform, "酒馆训练器Button").GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(opened);
                Assert.IsFalse(legacyOpened);
                Assert.IsNull(FindChild(rootObject.transform, "Unity 组件酒馆 UIButton"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CreatesUnityComponentZonesAndStableSlots()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var shell = FindChild(rootObject.transform, "UnityTavernTrainer");
                Assert.IsNotNull(shell);
                Assert.IsNotNull(shell.GetComponent<UnityTavernTrainerController>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTopBar"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityLegacyButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityShopZone").GetComponent<UnityTavernZoneComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityPlayerBoardZone").GetComponent<UnityTavernZoneComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentBoardZone").GetComponent<UnityTavernZoneComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHandZone").GetComponent<UnityTavernZoneComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanelDrawerToggle"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityRightPanel"));
                Assert.AreEqual(7, FindChildren(rootObject.transform, "UnityPlayerBoardZoneSlot-").Count);
                Assert.AreEqual(10, FindChildren(rootObject.transform, "UnityHandZoneSlot-").Count);
                Assert.GreaterOrEqual(FindComponentsNamed(rootObject.transform, "UnityTavernCardComponent").Count, service.State.Player.Tavern.Shop.Count);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_ShopCardActionBuysIntoHand()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var firstCard = service.State.Player.Tavern.Shop.First(card => card != null);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                FindChild(rootObject.transform, "UnityCardAction-" + firstCard.InstanceId).GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanelDrawerToggle"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void UserJourney_FromMainHubBuysPlaysAddsOpponentAndOpensCombatReplay()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var advisor = new LocalAdvisorService();
                var legacyOpened = false;
                var realisticOpened = false;

                void OpenUnityTrainer()
                {
                    ClearChildren(rootObject.transform);
                    new UnityTavernTrainerView(rootObject.transform, service, advisor, () => { }).Build();
                }

                new MainHubView(rootObject.transform, () => legacyOpened = true, () => realisticOpened = true, OpenUnityTrainer).Build();
                FindChild(rootObject.transform, "酒馆训练器Button").GetComponent<Button>().onClick.Invoke();

                Assert.IsFalse(legacyOpened);
                Assert.IsFalse(realisticOpened);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTavernTrainer"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityLegacyButton"));

                var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
                Assert.GreaterOrEqual(shopIndex, 0);
                var boughtCard = service.State.Player.Tavern.Shop[shopIndex];
                FindChild(rootObject.transform, "UnityCardAction-" + boughtCard.InstanceId).GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
                Assert.AreEqual(boughtCard.InstanceId, service.State.Player.Tavern.Hand[0].InstanceId);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityFeedbackToast"));

                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();
                var handCard = service.State.Player.Tavern.Hand[0];
                controller.BeginDrag(handCard, UnityTavernDragSource.Hand, 0);
                controller.HandleDrop(UnityTavernDropTarget.PlayerBoard, 0);

                Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
                Assert.AreEqual(1, service.State.Player.Board.Count);
                Assert.AreEqual(handCard.InstanceId, service.State.Player.Board[0].InstanceId);

                OpenRightPanelDrawer(rootObject.transform);
                Assert.AreEqual("功能面板", FindChild(rootObject.transform, "UnityRightPanelTitle").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay"));
                FindChild(rootObject.transform, "UnityToolsAddOpponentButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(1, service.State.Opponent.Board.Count);
                FindChild(rootObject.transform, "UnityTrainerToolsCloseButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay"));

                FindChild(rootObject.transform, "UnityCombatButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(MatchPhase.Result, service.State.Phase);
                Assert.IsNotNull(service.State.LastReplay);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatReplayPanel").GetComponent<UnityTavernCombatReplayPanelComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityReplayPlayPauseButton"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CommandSuccessShowsChineseFeedbackToast()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityRefreshButton").GetComponent<Button>().onClick.Invoke();

                var toast = FindChild(rootObject.transform, "UnityFeedbackToast");
                Assert.IsNotNull(toast);
                Assert.IsNotNull(toast.GetComponent<UnityTavernToastComponent>());
                Assert.AreEqual("已刷新酒馆", toast.GetComponentInChildren<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityErrorToast"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void DragCommandMapper_BuildsExpectedCommands()
        {
            var card = new MinionInstance { InstanceId = "drag-card" };

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.Shop, 2),
                UnityTavernDropTarget.Hand,
                -1,
                GameCommandType.BuyMinion,
                2,
                -1,
                null);

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.Discover, 1),
                UnityTavernDropTarget.Hand,
                -1,
                GameCommandType.ChooseDiscover,
                1,
                -1,
                null);

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.Hand, 0),
                UnityTavernDropTarget.PlayerBoard,
                3,
                GameCommandType.PlayMinion,
                0,
                3,
                null);

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.PlayerBoard, 0),
                UnityTavernDropTarget.PlayerBoard,
                4,
                GameCommandType.MoveBoardMinion,
                0,
                4,
                "drag-card");

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.PlayerBoard, 0),
                UnityTavernDropTarget.SellZone,
                -1,
                GameCommandType.SellMinion,
                0,
                -1,
                "drag-card");

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.OpponentBoard, 0),
                UnityTavernDropTarget.OpponentBoard,
                2,
                GameCommandType.MoveOpponentMinion,
                0,
                2,
                "drag-card");

            Assert.IsFalse(UnityTavernDragController.TryBuildDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.Shop, 0),
                UnityTavernDropTarget.PlayerBoard,
                0,
                out _));
        }

        [Test]
        public void Build_AddsDragSourcesDropTargetsAndSellZone()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var firstShopCard = service.State.Player.Tavern.Shop.First(card => card != null);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCard-" + firstShopCard.InstanceId).GetComponent<UnityTavernCardDragBehaviour>());
                Assert.AreEqual(10, FindChildren(rootObject.transform, "UnityHandZoneSlot-").Count(slot => slot.GetComponent<UnityTavernDropTargetBehaviour>() != null));
                Assert.AreEqual(7, FindChildren(rootObject.transform, "UnityPlayerBoardZoneSlot-").Count(slot => slot.GetComponent<UnityTavernDropTargetBehaviour>() != null));

                OpenRightPanelDrawer(rootObject.transform);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySellDropZone").GetComponent<UnityTavernDropTargetBehaviour>());

                var handDrop = FindChildren(rootObject.transform, "UnityHandZoneSlot-")
                    .First(slot => slot.GetComponent<UnityTavernDropTargetBehaviour>() != null)
                    .GetComponent<UnityTavernDropTargetBehaviour>();
                var handImage = handDrop.GetComponent<Image>();
                var normalColor = handImage.color;
                handDrop.OnPointerEnter(null);
                Assert.IsTrue(handDrop.IsHighlighted);
                Assert.AreNotEqual(normalColor, handImage.color);
                Assert.IsTrue(handDrop.GetComponent<Outline>().enabled);
                handDrop.OnPointerExit(null);
                Assert.IsFalse(handDrop.IsHighlighted);
                Assert.AreEqual(normalColor, handImage.color);
                Assert.IsFalse(handDrop.GetComponent<Outline>().enabled);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void BeginDrag_WithPointerEventCreatesNonBlockingDragGhost()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null);
                var firstShopCard = service.State.Player.Tavern.Shop[shopIndex];

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();
                var eventData = new PointerEventData(eventSystemObject.GetComponent<EventSystem>())
                {
                    position = new Vector2(420f, 260f)
                };

                Assert.DoesNotThrow(() => controller.BeginDrag(firstShopCard, UnityTavernDragSource.Shop, shopIndex, eventData));

                var ghost = FindChildren(rootObject.transform, "UnityCard-" + firstShopCard.InstanceId)
                    .FirstOrDefault(card => card.GetComponent<CanvasGroup>() != null);
                Assert.IsNotNull(ghost);
                Assert.IsFalse(ghost.GetComponent<CanvasGroup>().blocksRaycasts);
                Assert.IsFalse(ghost.GetComponent<CanvasGroup>().interactable);
                Assert.AreEqual(0.92f, ghost.GetComponent<CanvasGroup>().alpha, 0.001f);
                Assert.AreEqual(5000, ghost.GetComponent<Canvas>().sortingOrder);

                controller.EndDrag();
                Assert.IsFalse(FindChildren(rootObject.transform, "UnityCard-" + firstShopCard.InstanceId)
                    .Any(card => card.GetComponent<CanvasGroup>() != null));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                Object.DestroyImmediate(eventSystemObject);
            }
        }

        [Test]
        public void Bootstrap_ConfiguresExistingCanvasForResponsiveScaling()
        {
            var canvasObject = new GameObject("ExistingCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();

                LearnHearthstoneBootstrap.ConfigureCanvas(canvas);

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                Assert.IsNotNull(scaler);
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
                Assert.IsFalse(canvas.pixelPerfect);
                Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
                Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
                Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, scaler.screenMatchMode);
                Assert.AreEqual(0.5f, scaler.matchWidthOrHeight, 0.001f);
                Assert.IsNotNull(canvasObject.GetComponent<GraphicRaycaster>());
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void CardComponent_FeedbackTracksSelectedHoverAndPress()
        {
            var cardObject = new GameObject("FeedbackCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                var component = cardObject.GetComponent<UnityTavernCardComponent>();
                var card = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "feedback-card",
                    Name = "Feedback Minion",
                    Attack = 3,
                    Health = 4,
                    TavernTier = 2
                };

                component.Bind(card, UnityTavernCardMode.Shop, "Buy", null, null, true);

                var outline = cardObject.GetComponent<Outline>();
                Assert.IsTrue(component.IsSelected);
                Assert.IsNotNull(outline);
                Assert.IsTrue(outline.enabled);
                Assert.Greater(cardObject.transform.localScale.x, 1f);

                var selectedScale = cardObject.transform.localScale.x;
                component.OnPointerEnter(null);
                Assert.IsTrue(component.IsHovered);
                Assert.Greater(cardObject.transform.localScale.x, selectedScale);

                var hoverScale = cardObject.transform.localScale.x;
                component.OnPointerDown(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
                Assert.Less(cardObject.transform.localScale.x, hoverScale);

                component.OnPointerUp(null);
                Assert.Greater(cardObject.transform.localScale.x, 1f);

                component.OnPointerExit(null);
                Assert.IsFalse(component.IsHovered);
                Assert.IsTrue(outline.enabled);

                component.SetSelected(false);
                Assert.IsFalse(component.IsSelected);
                Assert.IsFalse(outline.enabled);
                Assert.AreEqual(1f, cardObject.transform.localScale.x, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void HandleDrop_AppliesBuyPlayMoveAndSellCommands()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
                Assert.GreaterOrEqual(shopIndex, 0);
                var shopCard = service.State.Player.Tavern.Shop[shopIndex];

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();

                controller.BeginDrag(shopCard, UnityTavernDragSource.Shop, shopIndex);
                controller.HandleDrop(UnityTavernDropTarget.Hand);

                Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
                Assert.IsNull(service.State.Player.Tavern.Shop[shopIndex]);

                var handCard = service.State.Player.Tavern.Hand[0];
                controller.BeginDrag(handCard, UnityTavernDragSource.Hand, 0);
                controller.HandleDrop(UnityTavernDropTarget.PlayerBoard, 0);

                Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
                Assert.AreEqual(1, service.State.Player.Board.Count);

                var secondBoardCard = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
                secondBoardCard.InstanceId = "unity-drag-second";
                secondBoardCard.Owner = BoardSide.Player;
                service.State.Player.Board.Add(secondBoardCard);

                var firstBoardCard = service.State.Player.Board[0];
                controller.BeginDrag(firstBoardCard, UnityTavernDragSource.PlayerBoard, 0);
                controller.HandleDrop(UnityTavernDropTarget.PlayerBoard, 1);

                Assert.AreEqual(firstBoardCard.InstanceId, service.State.Player.Board[1].InstanceId);

                controller.BeginDrag(firstBoardCard, UnityTavernDragSource.PlayerBoard, 1);
                controller.HandleDrop(UnityTavernDropTarget.SellZone);

                Assert.IsFalse(service.State.Player.Board.Any(card => card.InstanceId == firstBoardCard.InstanceId));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_UsesConfiguredRootPrefabWhenAvailable()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var rootPrefab = new GameObject("ConfiguredRootPrefab", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(UnityTavernTrainerController));
            try
            {
                rootPrefab.GetComponent<CanvasGroup>().alpha = 0.73f;
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(
                    rootObject.transform,
                    service,
                    new LocalAdvisorService(),
                    () => { },
                    rootPrefab: rootPrefab).Build();

                var shell = FindChild(rootObject.transform, "UnityTavernTrainer");
                Assert.IsNotNull(shell);
                Assert.AreEqual(0.73f, shell.GetComponent<CanvasGroup>().alpha);
                Assert.IsNotNull(shell.GetComponent<UnityTavernTrainerController>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTopBar"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                Object.DestroyImmediate(rootPrefab);
            }
        }

        [Test]
        public void CardComponent_BindUsesConfiguredPrefabReferencesWithoutGeneratedChildren()
        {
            var cardObject = new GameObject("CardPrefab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                var art = ImageChild(cardObject.transform, "PrefabArt");
                var name = TextChild(cardObject.transform, "PrefabName");
                var subtitle = TextChild(cardObject.transform, "PrefabSubtitle");
                var kind = TextChild(cardObject.transform, "PrefabKind");
                var tierBadge = new GameObject("PrefabTierBadge", typeof(RectTransform));
                tierBadge.transform.SetParent(cardObject.transform, false);
                var tier = TextChild(tierBadge.transform, "PrefabTierText");
                var attackBadge = new GameObject("PrefabAttackBadge", typeof(RectTransform));
                attackBadge.transform.SetParent(cardObject.transform, false);
                var attack = TextChild(attackBadge.transform, "PrefabAttackText");
                var healthBadge = new GameObject("PrefabHealthBadge", typeof(RectTransform));
                healthBadge.transform.SetParent(cardObject.transform, false);
                var health = TextChild(healthBadge.transform, "PrefabHealthText");
                var costBadge = new GameObject("PrefabCostBadge", typeof(RectTransform));
                costBadge.transform.SetParent(cardObject.transform, false);
                var cost = TextChild(costBadge.transform, "PrefabCostText");
                var action = ButtonChild(cardObject.transform, "PrefabPrimaryAction");
                var actionText = TextChild(action.transform, "PrefabPrimaryActionText");

                var component = cardObject.GetComponent<UnityTavernCardComponent>();
                component.ConfigureReferences(
                    frame: cardObject.GetComponent<Image>(),
                    art: art,
                    name: name,
                    subtitle: subtitle,
                    kind: kind,
                    tier: tier,
                    attack: attack,
                    health: health,
                    cost: cost,
                    rootButton: cardObject.GetComponent<Button>(),
                    primaryButton: action.GetComponent<Button>(),
                    primaryText: actionText,
                    tierBadgeObject: tierBadge,
                    attackBadgeObject: attackBadge,
                    healthBadgeObject: healthBadge,
                    costBadgeObject: costBadge);

                var selected = false;
                var acted = false;
                var card = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "prefab-card",
                    Name = "测试随从",
                    Attack = 3,
                    Health = 4,
                    Cost = 3,
                    TavernTier = 2,
                    Tribes = new List<Tribe> { Tribe.Mech },
                    OfficialKeywords = new List<Keyword> { Keyword.Taunt }
                };
                var childCount = cardObject.transform.childCount;

                component.Bind(card, UnityTavernCardMode.Shop, "购买", _ => selected = true, _ => acted = true);

                Assert.AreEqual(childCount, cardObject.transform.childCount);
                Assert.IsNull(FindChild(cardObject.transform, "UnityCardArt"));
                Assert.AreEqual("测试随从", name.text);
                Assert.AreEqual("嘲讽", subtitle.text);
                Assert.AreEqual("机械", kind.text);
                Assert.AreEqual("2", tier.text);
                Assert.AreEqual("3", attack.text);
                Assert.AreEqual("4", health.text);
                Assert.AreEqual("购买", actionText.text);
                Assert.IsTrue(attackBadge.activeSelf);
                Assert.IsTrue(healthBadge.activeSelf);
                Assert.IsFalse(costBadge.activeSelf);

                action.GetComponent<Button>().onClick.Invoke();
                cardObject.GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(acted);
                Assert.IsTrue(selected);
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void TavernCardPrefab_BindsThroughSerializedReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCardComponent.TavernCardPrefabAssetPath);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<Outline>());
            Assert.GreaterOrEqual(prefab.GetComponents<Shadow>().Length, 2);

            var cardObject = Object.Instantiate(prefab);
            try
            {
                var component = cardObject.GetComponent<UnityTavernCardComponent>();
                Assert.IsNotNull(component);

                var acted = false;
                var card = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "prefab-asset-card",
                    Name = "资产随从",
                    Attack = 5,
                    Health = 6,
                    TavernTier = 3,
                    Tribes = new List<Tribe> { Tribe.Beast },
                    OfficialKeywords = new List<Keyword> { Keyword.DivineShield }
                };
                var childCount = cardObject.transform.childCount;

                component.Bind(card, UnityTavernCardMode.Shop, "购买", null, _ => acted = true);

                Assert.AreEqual(childCount, cardObject.transform.childCount);
                Assert.AreEqual("资产随从", FindChild(cardObject.transform, "UnityCardName").GetComponent<Text>().text);
                Assert.AreEqual("圣盾", FindChild(cardObject.transform, "UnityCardSubtitle").GetComponent<Text>().text);
                Assert.AreEqual("5", FindChild(cardObject.transform, "UnityAttackBadgeText").GetComponent<Text>().text);
                Assert.AreEqual("6", FindChild(cardObject.transform, "UnityHealthBadgeText").GetComponent<Text>().text);

                FindChild(cardObject.transform, "UnityCardAction-prefab-asset-card").GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(acted);
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void ZonePrefabs_HaveRootComponents()
        {
            AssertZonePrefab(UnityTavernZoneComponent.ShopZonePrefabAssetPath);
            AssertZonePrefab(UnityTavernZoneComponent.HandZonePrefabAssetPath);
            AssertZonePrefab(UnityTavernZoneComponent.PlayerBoardZonePrefabAssetPath);
            AssertZonePrefab(UnityTavernZoneComponent.OpponentBoardZonePrefabAssetPath);
        }

        [Test]
        public void ZonePrefab_BindsSerializedHeaderAndSlotParent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernZoneComponent.ShopZonePrefabAssetPath);
            Assert.IsNotNull(prefab);

            var zoneObject = Object.Instantiate(prefab);
            try
            {
                var zone = zoneObject.GetComponent<UnityTavernZoneComponent>();
                Assert.IsNotNull(zone);

                var acted = false;
                var card = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "zone-prefab-card",
                    Name = "区域随从",
                    Attack = 2,
                    Health = 3,
                    TavernTier = 1,
                    Tribes = new List<Tribe> { Tribe.Murloc }
                };
                var rootChildCount = zoneObject.transform.childCount;

                zone.Build(
                    "测试商店",
                    "可刷新",
                    new List<MinionInstance> { card },
                    2,
                    UnityTavernCardMode.Shop,
                    _ => "购买",
                    null,
                    _ => acted = true);

                Assert.AreEqual(rootChildCount, zoneObject.transform.childCount);
                Assert.AreEqual("测试商店", FindChild(zoneObject.transform, "UnityZoneTitle").GetComponent<Text>().text);
                Assert.AreEqual("可刷新", FindChild(zoneObject.transform, "UnityZoneSubtitle").GetComponent<Text>().text);

                var row = FindChild(zoneObject.transform, "UnityZoneCardRow");
                Assert.AreEqual(2, row.childCount);
                Assert.IsNotNull(FindChild(row, "UnityCard-zone-prefab-card").GetComponent<UnityTavernCardComponent>());

                FindChild(row, "UnityCardAction-zone-prefab-card").GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(acted);
            }
            finally
            {
                Object.DestroyImmediate(zoneObject);
            }
        }

        [Test]
        public void PanelAndModalPrefabs_HaveRootComponents()
        {
            var rightPanel = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernRightPanelComponent.RightPanelPrefabAssetPath);
            Assert.IsNotNull(rightPanel);
            Assert.IsNotNull(rightPanel.GetComponent<UnityTavernRightPanelComponent>());
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelHeader"));
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelFloatToggle"));
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelFloatToggleText"));
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelActionHost"));
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelDetailHost"));
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelAdvisorHost"));
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelLogHost"));

            var actionPanel = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernActionPanelComponent.ActionPanelPrefabAssetPath);
            Assert.IsNotNull(actionPanel);
            Assert.IsNotNull(actionPanel.GetComponent<UnityTavernActionPanelComponent>());
            Assert.IsNotNull(FindChild(actionPanel.transform, "UnityActionButtonGrid"));

            var selectedPanel = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernSelectedCardPanelComponent.SelectedCardPanelPrefabAssetPath);
            Assert.IsNotNull(selectedPanel);
            Assert.IsNotNull(selectedPanel.GetComponent<UnityTavernSelectedCardPanelComponent>());
            Assert.IsNotNull(FindChild(selectedPanel.transform, "UnitySelectedCardContent"));

            var advisorPanel = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernAdvisorPanelComponent.AdvisorPanelPrefabAssetPath);
            Assert.IsNotNull(advisorPanel);
            Assert.IsNotNull(advisorPanel.GetComponent<UnityTavernAdvisorPanelComponent>());
            Assert.IsNotNull(FindChild(advisorPanel.transform, "UnityAdvisorTitle"));
            Assert.IsNotNull(FindChild(advisorPanel.transform, "UnityAdvisorContent"));

            var recruitLog = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernLogPanelComponent.RecruitLogPanelPrefabAssetPath);
            Assert.IsNotNull(recruitLog);
            Assert.IsNotNull(recruitLog.GetComponent<UnityTavernLogPanelComponent>());
            Assert.IsNotNull(FindChild(recruitLog.transform, "UnityLogTitle"));
            Assert.IsNotNull(FindChild(recruitLog.transform, "UnityLogScrollViewContent"));

            var combatLog = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernLogPanelComponent.CombatLogPanelPrefabAssetPath);
            Assert.IsNotNull(combatLog);
            Assert.IsNotNull(combatLog.GetComponent<UnityTavernLogPanelComponent>());
            Assert.IsNotNull(FindChild(combatLog.transform, "UnityLogTitle"));
            Assert.IsNotNull(FindChild(combatLog.transform, "UnityLogScrollViewContent"));

            var discover = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernDiscoverModalComponent.DiscoverModalPrefabAssetPath);
            Assert.IsNotNull(discover);
            Assert.IsNotNull(discover.GetComponent<UnityTavernDiscoverModalComponent>());
            Assert.IsNotNull(FindChild(discover.transform, "UnityDiscoverTitle"));
            Assert.IsNotNull(FindChild(discover.transform, "UnityDiscoverOptions"));

            var cardDetail = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCardDetailModalComponent.CardDetailModalPrefabAssetPath);
            Assert.IsNotNull(cardDetail);
            Assert.IsNotNull(cardDetail.GetComponent<UnityTavernCardDetailModalComponent>());
            Assert.IsNotNull(FindChild(cardDetail.transform, "UnityCardDetailTitle"));
            Assert.IsNotNull(FindChild(cardDetail.transform, "UnityCardDetailCardHost"));
            Assert.IsNotNull(FindChild(cardDetail.transform, "UnityCardDetailInfo"));

            var tools = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernToolsModalComponent.ToolsModalPrefabAssetPath);
            Assert.IsNotNull(tools);
            Assert.IsNotNull(tools.GetComponent<UnityTavernToolsModalComponent>());
            Assert.IsNotNull(FindChild(tools.transform, "UnityTrainerToolsTitle"));
            Assert.IsNotNull(FindChild(tools.transform, "UnityTrainerToolsScrollContent"));

            var replay = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCombatReplayPanelComponent.CombatReplayPanelPrefabAssetPath);
            Assert.IsNotNull(replay);
            Assert.IsNotNull(replay.GetComponent<UnityTavernCombatReplayPanelComponent>());
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayTitle"));
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayControls"));
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayEventHighlights"));
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayPlayerBoard"));
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayOpponentBoard"));
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayTimelineContent"));

            var toast = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernToastComponent.ErrorToastPrefabAssetPath);
            Assert.IsNotNull(toast);
            Assert.IsNotNull(toast.GetComponent<UnityTavernToastComponent>());
            Assert.IsNotNull(FindChild(toast.transform, "UnityErrorToastText"));
        }

        [Test]
        public void RightPanelChildPrefabs_BuildThroughSerializedContainers()
        {
            var actionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernActionPanelComponent.ActionPanelPrefabAssetPath);
            var selectedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernSelectedCardPanelComponent.SelectedCardPanelPrefabAssetPath);
            var advisorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernAdvisorPanelComponent.AdvisorPanelPrefabAssetPath);
            var logPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernLogPanelComponent.RecruitLogPanelPrefabAssetPath);
            Assert.IsNotNull(actionPrefab);
            Assert.IsNotNull(selectedPrefab);
            Assert.IsNotNull(advisorPrefab);
            Assert.IsNotNull(logPrefab);

            var actionObject = Object.Instantiate(actionPrefab);
            var selectedObject = Object.Instantiate(selectedPrefab);
            var advisorObject = Object.Instantiate(advisorPrefab);
            var logObject = Object.Instantiate(logPrefab);
            try
            {
                actionObject.GetComponent<UnityTavernActionPanelComponent>().Build(
                    parent => new GameObject("BuiltActionButton", typeof(RectTransform)).transform.SetParent(parent, false));
                Assert.IsNotNull(FindChild(actionObject.transform, "BuiltActionButton"));

                selectedObject.GetComponent<UnityTavernSelectedCardPanelComponent>().Build(
                    parent => new GameObject("BuiltSelectedDetail", typeof(RectTransform)).transform.SetParent(parent, false));
                Assert.IsNotNull(FindChild(selectedObject.transform, "BuiltSelectedDetail"));

                advisorObject.GetComponent<UnityTavernAdvisorPanelComponent>().Build(
                    "Test Advice",
                    parent => new GameObject("BuiltAdvisorLine", typeof(RectTransform)).transform.SetParent(parent, false));
                Assert.AreEqual("Test Advice", FindChild(advisorObject.transform, "UnityAdvisorTitle").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(advisorObject.transform, "BuiltAdvisorLine"));

                logObject.GetComponent<UnityTavernLogPanelComponent>().Build(
                    "Test Log",
                    parent => new GameObject("BuiltLogLine", typeof(RectTransform)).transform.SetParent(parent, false));
                Assert.AreEqual("Test Log", FindChild(logObject.transform, "UnityLogTitle").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(logObject.transform, "BuiltLogLine"));
            }
            finally
            {
                Object.DestroyImmediate(actionObject);
                Object.DestroyImmediate(selectedObject);
                Object.DestroyImmediate(advisorObject);
                Object.DestroyImmediate(logObject);
            }
        }

        [Test]
        public void CardDetailModalPrefab_BuildsCardInfoAndClose()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCardDetailModalComponent.CardDetailModalPrefabAssetPath);
            Assert.IsNotNull(prefab);

            var modalObject = Object.Instantiate(prefab);
            try
            {
                var closed = false;
                var card = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "detail-card",
                    CardId = "detail-card-id",
                    Name = "Detail Minion",
                    Attack = 2,
                    Health = 3,
                    MaxHealth = 3,
                    TavernTier = 1,
                    Text = "Detail text",
                    Tribes = new List<Tribe> { Tribe.Beast },
                    Keywords = new List<Keyword> { Keyword.Taunt }
                };

                modalObject.GetComponent<UnityTavernCardDetailModalComponent>().Build(card, () => closed = true);

                Assert.AreEqual("Detail Minion", FindChild(modalObject.transform, "UnityCardDetailTitle").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(modalObject.transform, "UnityCard-detail-card"));
                FindChild(modalObject.transform, "UnityCardDetailCloseButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(closed);
            }
            finally
            {
                Object.DestroyImmediate(modalObject);
            }
        }

        [Test]
        public void CombatReplayPanelPrefab_BuildsFramesAndNavigation()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCombatReplayPanelComponent.CombatReplayPanelPrefabAssetPath);
            Assert.IsNotNull(prefab);

            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var player = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
            player.InstanceId = "replay-player";
            player.Owner = BoardSide.Player;
            var opponent = service.State.Player.Tavern.Shop.Last(card => card != null && card.CardKind == CardKind.Minion).Clone();
            opponent.InstanceId = "replay-opponent";
            opponent.Owner = BoardSide.Opponent;
            service.State.Player.Board.Add(player);
            service.State.Opponent.Board.Add(opponent);
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 7, SafetyLimit = 20 }));

            var panelObject = Object.Instantiate(prefab);
            try
            {
                var targetIndex = -1;
                var closed = false;
                var playbackToggled = false;
                var speedCycled = false;
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(
                    service.State.LastReplay,
                    0,
                    false,
                    "1x",
                    index => targetIndex = index,
                    () => playbackToggled = true,
                    () => speedCycled = true,
                    () => closed = true);

                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayPlayerTitle"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayOpponentTitle"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayFirstButton"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayPlayPauseButton"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplaySpeedButton"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayEventChip-Event"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayEventLine"));

                FindChild(panelObject.transform, "UnityReplayPlayPauseButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(playbackToggled);

                FindChild(panelObject.transform, "UnityReplaySpeedButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(speedCycled);

                if (service.State.LastReplay.Frames.Count > 1)
                {
                    FindChild(panelObject.transform, "UnityReplayNextButton").GetComponent<Button>().onClick.Invoke();
                    Assert.AreEqual(1, targetIndex);
                }

                FindChild(panelObject.transform, "UnityCombatReplayCloseButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(closed);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void Build_CombatReplayPlaybackControlsToggleAndCycleSpeed()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var player = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
                player.InstanceId = "unity-replay-player";
                player.Owner = BoardSide.Player;
                var opponent = service.State.Player.Tavern.Shop.Last(card => card != null && card.CardKind == CardKind.Minion).Clone();
                opponent.InstanceId = "unity-replay-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Player.Board.Add(player);
                service.State.Opponent.Board.Add(opponent);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityCombatButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatReplayPanel").GetComponent<UnityTavernCombatReplayPanelComponent>());
                Assert.AreEqual("暂停", FindChild(rootObject.transform, "UnityReplayPlayPauseButtonText").GetComponent<Text>().text);
                Assert.AreEqual("速度 1x", FindChild(rootObject.transform, "UnityReplaySpeedButtonText").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityReplayEventChip-Event"));

                FindChild(rootObject.transform, "UnityReplayPlayPauseButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual("播放", FindChild(rootObject.transform, "UnityReplayPlayPauseButtonText").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityReplaySpeedButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual("速度 2x", FindChild(rootObject.transform, "UnityReplaySpeedButtonText").GetComponent<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void CombatReplayPanel_BuildsAnimatedStrikeHitAndDeathTiles()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var replay = new CombatReplay
                {
                    Seed = 7,
                    Result = CombatWinner.Player
                };

                replay.Frames.Add(new CombatFrame
                {
                    Index = 0,
                    EventType = CombatEventType.CombatStarted,
                    PlayerBoardSnapshot = BoardSnapshot(BoardSide.Player, MinionSnapshot("anim-player", "Animated Player", 0, 3, 4)),
                    OpponentBoardSnapshot = BoardSnapshot(BoardSide.Opponent, MinionSnapshot("anim-opponent", "Animated Opponent", 0, 2, 2)),
                    LogText = "start"
                });
                replay.Frames.Add(new CombatFrame
                {
                    Index = 1,
                    EventType = CombatEventType.AttackDeclared,
                    ActorSide = BoardSide.Player,
                    ActorId = "anim-player",
                    TargetSide = BoardSide.Opponent,
                    TargetId = "anim-opponent",
                    DamagedEntityIds = new List<string> { "anim-opponent" },
                    PlayerBoardSnapshot = BoardSnapshot(BoardSide.Player, MinionSnapshot("anim-player", "Animated Player", 0, 3, 4)),
                    OpponentBoardSnapshot = BoardSnapshot(BoardSide.Opponent, MinionSnapshot("anim-opponent", "Animated Opponent", 0, 2, 1)),
                    LogText = "attack"
                });
                replay.Frames.Add(new CombatFrame
                {
                    Index = 2,
                    EventType = CombatEventType.DeathQueued,
                    TargetSide = BoardSide.Opponent,
                    TargetId = "anim-opponent",
                    DeadEntityIds = new List<string> { "anim-opponent" },
                    PlayerBoardSnapshot = BoardSnapshot(BoardSide.Player, MinionSnapshot("anim-player", "Animated Player", 0, 3, 4)),
                    OpponentBoardSnapshot = BoardSnapshot(BoardSide.Opponent),
                    LogText = "dead"
                });

                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(replay, 1, true, "1x", _ => { }, () => { }, () => { }, () => { });

                var actor = FindChild(panelObject.transform, "UnityReplayMinion-anim-player");
                var target = FindChild(panelObject.transform, "UnityReplayMinion-anim-opponent");
                var actorAnimator = actor.GetComponent<UnityTavernReplayTileAnimator>();
                var targetAnimator = target.GetComponent<UnityTavernReplayTileAnimator>();
                Assert.AreEqual(UnityTavernReplayTileMotion.Strike, actorAnimator.Motion);
                Assert.AreEqual(UnityTavernReplayTileMotion.Hit, targetAnimator.Motion);
                actorAnimator.ApplyPreview(0.5f);
                Assert.Greater(actor.localScale.x, 1f);
                Assert.IsNotNull(FindChild(actor, "UnityReplayMotionFlash"));

                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(replay, 2, true, "1x", _ => { }, () => { }, () => { }, () => { });

                var death = FindChild(panelObject.transform, "UnityReplayDeathMarker-anim-opponent");
                var deathAnimator = death.GetComponent<UnityTavernReplayTileAnimator>();
                Assert.AreEqual(UnityTavernReplayTileMotion.Death, deathAnimator.Motion);
                deathAnimator.ApplyPreview(1f);
                Assert.Less(death.GetComponent<CanvasGroup>().alpha, 0.5f);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void RightPanelPrefab_BuildConfiguresFloatingToggle()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernRightPanelComponent.RightPanelPrefabAssetPath);
            Assert.IsNotNull(prefab);

            var panelObject = Object.Instantiate(prefab);
            try
            {
                var panel = panelObject.GetComponent<UnityTavernRightPanelComponent>();
                Assert.IsNotNull(panel);

                var toggled = false;
                panel.Build(
                    "Test Control",
                    false,
                    () => toggled = true,
                    parent => { },
                    parent => { },
                    parent => { },
                    parent => { });

                Assert.AreEqual("展开", FindChild(panelObject.transform, "UnityRightPanelFloatToggleText").GetComponent<Text>().text);
                FindChild(panelObject.transform, "UnityRightPanelFloatToggle").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(toggled);

                panel.Build(
                    "Test Control",
                    true,
                    () => { },
                    parent => { },
                    parent => { },
                    parent => { },
                    parent => { });

                Assert.AreEqual("收起", FindChild(panelObject.transform, "UnityRightPanelFloatToggleText").GetComponent<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void RightPanelPrefab_BuildUsesSerializedSectionHosts()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernRightPanelComponent.RightPanelPrefabAssetPath);
            Assert.IsNotNull(prefab);

            var panelObject = Object.Instantiate(prefab);
            try
            {
                var panel = panelObject.GetComponent<UnityTavernRightPanelComponent>();
                Assert.IsNotNull(panel);
                var rootChildCount = panelObject.transform.childCount;

                panel.Build(
                    "Test Control",
                    parent => new GameObject("BuiltActions", typeof(RectTransform)).transform.SetParent(parent, false),
                    parent => new GameObject("BuiltDetail", typeof(RectTransform)).transform.SetParent(parent, false),
                    parent => new GameObject("BuiltAdvisor", typeof(RectTransform)).transform.SetParent(parent, false),
                    parent => new GameObject("BuiltLog", typeof(RectTransform)).transform.SetParent(parent, false));

                Assert.AreEqual(rootChildCount, panelObject.transform.childCount);
                Assert.AreEqual("Test Control", FindChild(panelObject.transform, "UnityRightPanelTitle").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(panelObject.transform, "BuiltActions"));
                Assert.IsNotNull(FindChild(panelObject.transform, "BuiltDetail"));
                Assert.IsNotNull(FindChild(panelObject.transform, "BuiltAdvisor"));
                Assert.IsNotNull(FindChild(panelObject.transform, "BuiltLog"));
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void Build_RightPanelToggleExpandsAndCollapsesDrawer()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNull(FindChild(rootObject.transform, "UnityRightPanel"));
                var drawerToggle = FindChild(rootObject.transform, "UnityRightPanelDrawerToggle");
                Assert.IsNotNull(drawerToggle);
                Assert.AreEqual("功能", FindChild(rootObject.transform, "UnityRightPanelDrawerToggleText").GetComponent<Text>().text);

                drawerToggle.GetComponent<Button>().onClick.Invoke();

                var drawerPanel = FindChild(rootObject.transform, "UnityRightPanel");
                Assert.IsNotNull(drawerPanel);
                Assert.AreEqual("UnityTavernTrainer", drawerPanel.parent.name);
                Assert.IsNull(FindChild(rootObject.transform, "UnityRightPanelDrawerToggle"));
                var drawerRect = drawerPanel.GetComponent<RectTransform>();
                Assert.AreEqual(1f, drawerRect.anchorMin.x);
                Assert.AreEqual(0f, drawerRect.anchorMin.y);
                Assert.AreEqual(1f, drawerRect.anchorMax.x);
                Assert.AreEqual(1f, drawerRect.anchorMax.y);
                Assert.AreEqual("功能面板", FindChild(rootObject.transform, "UnityRightPanelTitle").GetComponent<Text>().text);
                Assert.AreEqual("收起", FindChild(rootObject.transform, "UnityRightPanelFloatToggleText").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityRightPanelFloatToggle").GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(FindChild(rootObject.transform, "UnityRightPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanelDrawerToggle"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CardDetailReplayAndToolsExposeMissingCommands()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var player = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
                player.InstanceId = "unity-tools-player";
                player.Owner = BoardSide.Player;
                var opponent = service.State.Player.Tavern.Shop.Last(card => card != null && card.CardKind == CardKind.Minion).Clone();
                opponent.InstanceId = "unity-tools-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Player.Board.Add(player);
                service.State.Opponent.Board.Add(opponent);
                var startingGold = service.State.Player.Tavern.Gold;

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);

                FindChild(rootObject.transform, "UnitySelectedCardDetailsButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardDetailOverlay").GetComponent<UnityTavernCardDetailModalComponent>());
                FindChild(rootObject.transform, "UnityCardDetailCloseButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNull(FindChild(rootObject.transform, "UnityCardDetailOverlay"));

                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay").GetComponent<UnityTavernToolsModalComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsAddGoldButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsAddMinionButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsAddSpellButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsAddOpponentButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsClearOpponentButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCopyOpponentButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsMirrorOpponentButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsRunCombatTestButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsSaveScenarioButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsLoadScenarioButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsResetCombatSnapshotButton"));

                FindChild(rootObject.transform, "UnityToolsAddGoldButton").GetComponent<Button>().onClick.Invoke();
                Assert.Greater(service.State.Player.Tavern.Gold, startingGold);

                var handBefore = service.State.Player.Tavern.Hand.Count;
                FindChild(rootObject.transform, "UnityToolsAddMinionButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(handBefore + 1, service.State.Player.Tavern.Hand.Count);

                FindChild(rootObject.transform, "UnityToolsRunCombatTestButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(service.State.LastReplay);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatReplayPanel").GetComponent<UnityTavernCombatReplayPanelComponent>());
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Tools_CardLibraryFiltersAndAddsCardsToHand()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibrarySection"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryMinionModeButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibrarySpellModeButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryTier1Button"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryTribeBeastButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryAddButton"));

                FindChild(rootObject.transform, "UnityToolsCardLibraryTier1Button").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsCardLibraryCountText").GetComponent<Text>().text.Contains("1本"));
                FindChild(rootObject.transform, "UnityToolsCardLibraryTribeBeastButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsCardLibraryCountText").GetComponent<Text>().text.Contains("野兽"));
                FindChild(rootObject.transform, "UnityToolsCardLibraryTierAllButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityToolsCardLibraryTribeAllButton").GetComponent<Button>().onClick.Invoke();

                var handBeforeMinion = service.State.Player.Tavern.Hand.Count;
                FindChild(rootObject.transform, "UnityToolsCardLibraryAddButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(handBeforeMinion + 1, service.State.Player.Tavern.Hand.Count);
                Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Minion));

                FindChild(rootObject.transform, "UnityToolsCardLibrarySpellModeButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryTavernSpellTypeButton"));

                var handBeforeSpell = service.State.Player.Tavern.Hand.Count;
                FindChild(rootObject.transform, "UnityToolsCardLibraryAddButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(handBeforeSpell + 1, service.State.Player.Tavern.Hand.Count);
                Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_UsesPanelModalAndToastPrefabs()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Tavern.Discover = new DiscoverState
                {
                    Options = service.State.Player.Tavern.Shop.Where(card => card != null).Take(2).Select(card => card.Clone()).ToList()
                };

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanel").GetComponent<UnityTavernRightPanelComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityActionPanel").GetComponent<UnityTavernActionPanelComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySelectedCardPanel").GetComponent<UnityTavernSelectedCardPanelComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvisorPanel").GetComponent<UnityTavernAdvisorPanelComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityLogScroll").GetComponent<UnityTavernLogPanelComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityReplayButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityDiscoverOverlay").GetComponent<UnityTavernDiscoverModalComponent>());
                Assert.AreEqual(2, FindChild(rootObject.transform, "UnityDiscoverOptions").childCount);

                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();
                var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null);
                controller.BeginDrag(service.State.Player.Tavern.Shop[shopIndex], UnityTavernDragSource.Shop, shopIndex);
                controller.HandleDrop(UnityTavernDropTarget.PlayerBoard);

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityErrorToast").GetComponent<UnityTavernToastComponent>());
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CombatButtonRunsExistingMatchCommand()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var player = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
                player.InstanceId = "unity-player";
                player.Owner = BoardSide.Player;
                var opponent = service.State.Player.Tavern.Shop.Last(card => card.CardKind == CardKind.Minion).Clone();
                opponent.InstanceId = "unity-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Player.Board.Add(player);
                service.State.Opponent.Board.Add(opponent);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityCombatButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(MatchPhase.Result, service.State.Phase);
                Assert.IsNotNull(service.State.LastResult);
                Assert.IsNotNull(service.State.LastReplay);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        private static CombatBoardSnapshot BoardSnapshot(BoardSide side, params CombatMinionSnapshot[] minions)
        {
            var snapshot = new CombatBoardSnapshot { Side = side };
            snapshot.Minions.AddRange(minions);
            return snapshot;
        }

        private static CombatMinionSnapshot MinionSnapshot(string instanceId, string name, int position, int attack, int health)
        {
            return new CombatMinionSnapshot
            {
                InstanceId = instanceId,
                CardId = instanceId + "-card",
                Name = name,
                Position = position,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                CanAttack = true,
                Keywords = new List<Keyword>(),
                Tribes = new List<Tribe> { Tribe.Beast }
            };
        }

        private static void AssertDropCommand(
            UnityTavernDragContext drag,
            UnityTavernDropTarget target,
            int targetIndex,
            GameCommandType expectedType,
            int expectedIndex,
            int expectedTargetIndex,
            string expectedInstanceId)
        {
            Assert.IsTrue(UnityTavernDragController.TryBuildDropCommand(drag, target, targetIndex, out var command));
            Assert.AreEqual(expectedType, command.Type);
            Assert.AreEqual(expectedIndex, command.Index);
            Assert.AreEqual(expectedTargetIndex, command.TargetIndex);
            Assert.AreEqual(expectedInstanceId, command.InstanceId);
        }

        private static Transform OpenRightPanelDrawer(Transform root)
        {
            var toggle = FindChild(root, "UnityRightPanelDrawerToggle");
            Assert.IsNotNull(toggle);
            toggle.GetComponent<Button>().onClick.Invoke();
            var panel = FindChild(root, "UnityRightPanel");
            Assert.IsNotNull(panel);
            return panel;
        }

        private static void ClearChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index -= 1)
            {
                Object.DestroyImmediate(root.GetChild(index).gameObject);
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

        private static List<MonoBehaviour> FindComponentsNamed(Transform root, string typeName)
        {
            var results = root.GetComponents<MonoBehaviour>()
                .Where(component => component != null && component.GetType().Name == typeName)
                .ToList();
            for (var index = 0; index < root.childCount; index += 1)
            {
                results.AddRange(FindComponentsNamed(root.GetChild(index), typeName));
            }

            return results;
        }

        private static Image ImageChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Image>();
        }

        private static Text TextChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Text));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Text>();
        }

        private static GameObject ButtonChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void AssertZonePrefab(string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Assert.IsNotNull(prefab, assetPath);
            Assert.IsNotNull(prefab.GetComponent<Image>(), assetPath);
            Assert.IsNotNull(prefab.GetComponent<UnityTavernZoneComponent>(), assetPath);
            Assert.IsNotNull(FindChild(prefab.transform, "UnityZoneHeader"), assetPath);
            Assert.IsNotNull(FindChild(prefab.transform, "UnityZoneTitle"), assetPath);
            Assert.IsNotNull(FindChild(prefab.transform, "UnityZoneSubtitle"), assetPath);
            Assert.IsNotNull(FindChild(prefab.transform, "UnityZoneCardRow"), assetPath);
        }
    }
}
