using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LearnHearthstone.Tests.PlayMode
{
    public sealed class TimewarpedTavernInputTests
    {
        [UnityTest]
        public IEnumerator PlayMode_PJ04_TrinketFirstFullHandRapidClickShopRestoreAndCombatCompleteThroughRaycast()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);

                var rootObject = new GameObject("Root", typeof(RectTransform));
                rootObject.transform.SetParent(canvasObject.transform, false);
                var rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                var service = MatchService.CreateWithDefaultCatalog(
                    13579,
                    new InMemoryTestScenarioRepository(),
                    new MatchSetupOptions
                    {
                        AdvancedMechanicMode = AdvancedMechanicMode.Mixed,
                        EnableQuests = false,
                        EnableQuestRewards = false,
                        EnableTrinkets = true
                    });
                while (service.State.Round < 6)
                {
                    service.Apply(new GameCommand(GameCommandType.NextTurn));
                }

                var tavern = service.State.Player.Tavern;
                var pendingChoice = service.GetActiveMechanicChoice();
                Assert.IsNotNull(pendingChoice, "Round 6 must expose the pending Lesser Trinket choice.");
                Assert.AreEqual(AdvancedMechanicKind.Trinket, pendingChoice.Kind);
                Assert.AreEqual(TimewarpTavernPhase.BlockedByTrinketChoice, tavern.Timewarp.Phase);
                Assert.IsFalse(tavern.Timewarp.VisitOpen);

                TavernShopSlots.Ensure(tavern);
                for (var slotIndex = 0; slotIndex < tavern.ShopSlots.Count; slotIndex += 1)
                {
                    tavern.ShopSlots[slotIndex].Frozen = slotIndex % 2 == 0 && tavern.Shop[slotIndex] != null;
                }
                tavern.Frozen = tavern.ShopSlots.Any(slot => slot != null && slot.Frozen);
                var shopFingerprint = ShopFingerprint(tavern);
                var poolFingerprint = PoolFingerprint(tavern);

                tavern.Hand.Clear();
                for (var handIndex = 0; handIndex < 10; handIndex += 1)
                {
                    tavern.Hand.Add(new MinionInstance
                    {
                        CardKind = CardKind.Minion,
                        InstanceId = "pj04-full-hand-" + handIndex,
                        DefinitionId = "pj04-vanilla-" + handIndex,
                        CardId = "PJ04_VANILLA_" + handIndex,
                        Name = "旅程测试随从 " + (handIndex + 1),
                        Cost = 3,
                        BaseAttack = 1,
                        BaseHealth = 1,
                        Attack = 1,
                        Health = 1,
                        MaxHealth = 1,
                        TavernTier = 1,
                        Owner = BoardSide.Player,
                        OriginPoolSource = PoolSource.Debug,
                        PoolSource = PoolSource.Debug
                    });
                }
                var handSource = tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
                var opponent = handSource.Clone();
                opponent.InstanceId = "pj04-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Opponent.Board.Clear();
                service.State.Opponent.Board.Add(opponent);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(rootObject.transform, "UnityAdvancedMechanicChoiceButton-0");

                var eventSystem = eventSystemObject.GetComponent<EventSystem>();
                var raycaster = canvasObject.GetComponent<GraphicRaycaster>();
                ClickThroughRaycast(
                    eventSystem,
                    raycaster,
                    FindChild(rootObject.transform, "UnityAdvancedMechanicChoiceButton-0").GetComponent<RectTransform>());
                yield return WaitForChild(rootObject.transform, "UnityTimewarpedTavernModal");

                Assert.IsNull(tavern.AdvancedMechanics.PendingChoice);
                Assert.IsTrue(tavern.Timewarp.VisitOpen);
                Assert.AreEqual(TimewarpTavernPhase.Open, tavern.Timewarp.Phase);
                Assert.IsNotNull(tavern.AdvancedMechanics.Trinkets.LesserTrinketId, "Selecting the first offer must equip a Lesser Trinket.");

                var offerCard = service.GetTimewarpedOfferCards()
                    .First(card => card != null && card.CardKind == CardKind.Minion);
                var offer = tavern.Timewarp.Offers
                    .First(item => item != null && item.SlotId == offerCard.InstanceId.Substring("timewarp-offer-".Length));
                tavern.Timewarp.Chronum = offer.Cost + 2;
                var actionName = "UnityCardAction-" + offerCard.InstanceId;
                var chronumBeforePurchase = tavern.Timewarp.Chronum;
                var matchingCardsBefore = tavern.Hand.Count(card => card.CardId == offerCard.CardId);

                ClickThroughRaycast(
                    eventSystem,
                    raycaster,
                    FindChild(rootObject.transform, actionName).GetComponent<RectTransform>());
                yield return WaitForPurchasedOffer(rootObject.transform, service, offer, offerCard.CardId);

                Assert.IsTrue(offer.Purchased);
                Assert.AreEqual(11, tavern.Hand.Count);
                Assert.IsTrue(tavern.Timewarp.HasTemporaryHandExpansion);
                Assert.AreEqual(chronumBeforePurchase - offer.Cost, tavern.Timewarp.Chronum);
                Assert.AreEqual(matchingCardsBefore + 1, tavern.Hand.Count(card => card.CardId == offerCard.CardId));

                var handAction = FindChild(rootObject.transform, "UnityCard-pj04-full-hand-0");
                var blockedHandPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    handAction.GetComponent<RectTransform>().TransformPoint(handAction.GetComponent<RectTransform>().rect.center));
                ClickAtScreenPosition(eventSystem, raycaster, blockedHandPosition);
                yield return null;

                Assert.AreEqual(11, tavern.Hand.Count);
                Assert.IsTrue(tavern.Timewarp.VisitOpen);

                ClickThroughRaycast(
                    eventSystem,
                    raycaster,
                    FindChild(rootObject.transform, "UnityTimewarpedTavernExitButton").GetComponent<RectTransform>());
                yield return WaitForTimewarpedExitRebuild(rootObject.transform, service);

                Assert.AreEqual(shopFingerprint, ShopFingerprint(tavern));
                Assert.AreEqual(poolFingerprint, PoolFingerprint(tavern));

                var roundBeforeCombat = service.State.Round;
                ClickThroughRaycast(
                    eventSystem,
                    raycaster,
                    FindChild(rootObject.transform, "UnityQuickNextTurnButton").GetComponent<RectTransform>());
                yield return WaitForChild(rootObject.transform, "UnityCombatBattlefieldRoot");

                Assert.AreEqual(roundBeforeCombat, service.State.Round);
                Assert.AreEqual(roundBeforeCombat + 1, service.State.PendingTurnStartRound);
                Assert.AreEqual(MatchPhase.Result, service.State.Phase);
                Assert.IsNotNull(service.State.LastReplay, "Starting combat after the Timewarped visit must record a replay.");
                Assert.Greater(service.State.LastReplay.Frames.Count, 1);

                ClickThroughRaycast(
                    eventSystem,
                    raycaster,
                    FindChild(rootObject.transform, "UnityCombatReturnButton").GetComponent<RectTransform>());
                yield return null;

                Assert.Greater(service.State.Round, roundBeforeCombat);
                Assert.AreEqual(0, service.State.PendingTurnStartRound);
                Assert.AreEqual(MatchPhase.Tavern, service.State.Phase);
            }
            finally
            {
                Object.Destroy(canvasObject);
                Object.Destroy(eventSystemObject);
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_TimewarpedOpenVisitBlocksNextTurnAndExitWorksThroughRaycast()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);

                var rootObject = new GameObject("Root", typeof(RectTransform));
                rootObject.transform.SetParent(canvasObject.transform, false);
                var rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                var service = MatchService.CreateWithDefaultCatalog(
                    12345,
                    new InMemoryTestScenarioRepository(),
                    new MatchSetupOptions
                    {
                        AdvancedMechanicMode = AdvancedMechanicMode.Timewarp,
                        EnableTrinkets = false
                    });
                while (service.State.Round < 6)
                {
                    service.Apply(new GameCommand(GameCommandType.NextTurn));
                }

                service.State.Player.Tavern.Timewarp.Chronum = 0;

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(rootObject.transform, "UnityTimewarpedTavernModal");

                var eventSystem = eventSystemObject.GetComponent<EventSystem>();
                var raycaster = canvasObject.GetComponent<GraphicRaycaster>();
                var nextTurn = FindChild(rootObject.transform, "UnityQuickNextTurnButton");
                var roundBefore = service.State.Round;

                var nextTurnPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    nextTurn.GetComponent<RectTransform>().TransformPoint(nextTurn.GetComponent<RectTransform>().rect.center));
                ClickAtScreenPosition(eventSystem, raycaster, nextTurnPosition);
                yield return null;

                Assert.AreEqual(roundBefore, service.State.Round);
                Assert.IsTrue(service.State.Player.Tavern.Timewarp.VisitOpen);
                Assert.IsFalse(nextTurn.GetComponent<Button>().interactable);

                var timewarp = service.State.Player.Tavern.Timewarp;
                var offerCard = service.GetTimewarpedOfferCards()
                    .First(card => card != null && card.CardKind == CardKind.Minion);
                var offerDefinition = service.GetTimewarpedCandidateDefinitions(timewarp.PendingKind)
                    .First(definition => definition.CardId == offerCard.CardId);
                Assert.AreEqual(offerDefinition.ZhName, offerCard.Name);
                Assert.AreEqual(offerDefinition.ZhText, offerCard.Text);
                var timewarpedModal = FindChild(rootObject.transform, "UnityTimewarpedTavernModal");
                Assert.AreEqual("小型时空酒馆", FindChild(timewarpedModal, "UnityTimewarpedTavernTitle").GetComponent<Text>().text);
                Assert.AreEqual("时空资源：0", FindChild(timewarpedModal, "UnityTimewarpedTavernChronum").GetComponent<Text>().text);
                Assert.IsNotNull(FindChildOrNull(timewarpedModal, "UnityTimewarpedOfferSlot0DisabledReason"));
                Assert.AreEqual(5, timewarp.Offers.Count);

                var exit = FindChild(rootObject.transform, "UnityTimewarpedTavernExitButton");
                ClickThroughRaycast(eventSystem, raycaster, exit.GetComponent<RectTransform>());
                yield return WaitForTimewarpedExitRebuild(rootObject.transform, service);

                Assert.IsFalse(service.State.Player.Tavern.Timewarp.VisitOpen);
                Assert.IsNull(service.GetNextTurnBlockedReason());
                Assert.IsTrue(FindChild(rootObject.transform, "UnityQuickNextTurnButton").GetComponent<Button>().interactable);
            }
            finally
            {
                Object.Destroy(canvasObject);
                Object.Destroy(eventSystemObject);
            }
        }

        private static void ClickThroughRaycast(EventSystem eventSystem, GraphicRaycaster raycaster, RectTransform target)
        {
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center))
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            var hit = hits.FirstOrDefault(result =>
                result.gameObject == target.gameObject || result.gameObject.transform.IsChildOf(target));
            Assert.IsNotNull(hit.gameObject, target.name + " was not reachable through GraphicRaycaster.");

            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static void ClickAtScreenPosition(EventSystem eventSystem, GraphicRaycaster raycaster, Vector2 position)
        {
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = position
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            if (hits.Count == 0)
            {
                return;
            }

            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static Transform FindChild(Transform parent, string name)
        {
            var child = FindChildOrNull(parent, name);
            Assert.IsNotNull(child, "Missing child: " + name);
            return child;
        }

        private static Transform FindChildOrNull(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == name)
            {
                return parent;
            }

            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = FindChildOrNull(parent.GetChild(index), name);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static IEnumerator WaitForChild(Transform root, string name)
        {
            Transform stableChild = null;
            var stableFrames = 0;
            for (var frame = 0; frame < 60; frame += 1)
            {
                Canvas.ForceUpdateCanvases();
                var child = FindChildOrNull(root, name);
                if (child != null && child == stableChild)
                {
                    stableFrames += 1;
                    if (stableFrames >= 2)
                    {
                        yield break;
                    }
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

        private static IEnumerator WaitForToast(Transform root, string toastName, string expectedText)
        {
            for (var frame = 0; frame < 60; frame += 1)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                var toast = FindChildOrNull(root, toastName);
                if (toast != null && toast.GetComponentsInChildren<Text>(true).Any(text => text.text == expectedText))
                {
                    yield break;
                }
            }

            Assert.Fail("Timed out waiting for toast: " + expectedText);
        }

        private static IEnumerator WaitForPurchasedOffer(
            Transform root,
            MatchService service,
            TimewarpedOfferSlot offer,
            string cardId)
        {
            for (var frame = 0; frame < 60; frame += 1)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                if (offer.Purchased &&
                    service.State.Player.Tavern.Hand.Any(card => card.CardId == cardId) &&
                    FindChildOrNull(root, "UnityFeedbackToast") != null)
                {
                    yield break;
                }
            }

            Assert.Fail("Timewarped Tavern purchase did not complete through the visible action button.");
        }

        private static string ShopFingerprint(TavernState tavern)
        {
            TavernShopSlots.Ensure(tavern);
            var cards = string.Join("|", tavern.Shop.Select(card => card == null
                ? "null"
                : card.InstanceId + ":" + card.CardId + ":" + card.Attack + ":" + card.Health + ":" + card.Cost));
            var slots = string.Join("|", tavern.ShopSlots.Select(slot => slot == null
                ? "null"
                : slot.SlotId + ":" + slot.CardInstanceId + ":" + slot.Frozen));
            return tavern.Frozen + "#" + cards + "#" + slots;
        }

        private static string PoolFingerprint(TavernState tavern)
        {
            return string.Join("|", tavern.Pool.OrderBy(pair => pair.Key).Select(pair => pair.Key + ":" + pair.Value));
        }

        private static IEnumerator WaitForTimewarpedExitRebuild(Transform root, MatchService service)
        {
            for (var frame = 0; frame < 30; frame += 1)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                var nextTurn = FindChildOrNull(root, "UnityQuickNextTurnButton");
                if (!service.State.Player.Tavern.Timewarp.VisitOpen &&
                    nextTurn != null &&
                    nextTurn.GetComponent<Button>().interactable)
                {
                    yield break;
                }
            }

            Assert.Fail("Timewarped Tavern exit did not rebuild an interactable Next Turn button.");
        }
    }
}
