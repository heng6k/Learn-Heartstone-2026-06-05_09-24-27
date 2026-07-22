using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTimewarpedTavernModalComponent : MonoBehaviour
    {
        private const int OfferSlotCount = TimewarpTavernRules.OfficialOfferCount;

        public static GameObject CreateModalHost(Transform parent)
        {
            var modal = new GameObject(
                "UnityTimewarpedTavernModal",
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTimewarpedTavernModalComponent));
            modal.transform.SetParent(parent, false);
            UnityTavernUiStyle.Stretch(modal.GetComponent<RectTransform>());
            var blocker = modal.GetComponent<Image>();
            blocker.color = UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.BackWall, 0.96f);
            blocker.raycastTarget = true;
            return modal;
        }

        public void Build(
            string title,
            int chronum,
            IReadOnlyList<MinionInstance> cards,
            IReadOnlyList<TimewarpedOfferSlot> offers,
            bool useEnglish,
            Action<int> buy,
            Action exit)
        {
            var panel = new GameObject("UnityTimewarpedTavernPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            ConfigureSafePanel(panel.GetComponent<RectTransform>());
            UnityTavernUiStyle.ConfigureSurface(panel, UnityTavernUiStyle.SurfaceDark, true);
            UnityTavernUiStyle.ConfigureOutline(panel, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.72f), new Vector2(2f, -2f));
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityTimewarpedStarLantern", UnityTavernUiStyle.ArcaneBlue);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildHeader(panel.transform, title, chronum, useEnglish);
            var buttons = BuildOffers(panel.transform, chronum, cards, offers, useEnglish, buy);
            var exitButton = BuildFooter(panel.transform, useEnglish, exit);
            ConfigureNavigation(buttons, exitButton);
        }

        private static void ConfigureSafePanel(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.05f, 0.05f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void BuildHeader(Transform parent, string title, int chronum, bool useEnglish)
        {
            var header = new GameObject("UnityTimewarpedTavernHeader", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(header, 0f, 56f);
            UnityTavernUiStyle.ConfigureSurface(header, UnityTavernUiStyle.SurfaceRaised);
            UnityTavernUiStyle.ConfigureOutline(header, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.34f), new Vector2(1f, -1f));
            var layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 6, 6);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var titleText = UiFactory.Label("UnityTimewarpedTavernTitle", header.transform, title, 22, FontStyle.Bold);
            titleText.color = UnityTavernUiStyle.Gold;
            titleText.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetFlexible(titleText.gameObject, 1f, 0f);
            UnityTavernUiStyle.ConfigureOutline(titleText.gameObject, new Color(0f, 0f, 0f, 0.8f), new Vector2(1f, -1f));

            var chronumText = UiFactory.Label(
                "UnityTimewarpedTavernChronum",
                header.transform,
                (useEnglish ? "Chronum: " : "时空资源：") + Math.Max(0, chronum),
                20,
                FontStyle.Bold);
            chronumText.color = UnityTavernUiStyle.FocusRing;
            chronumText.alignment = TextAnchor.MiddleRight;
            UnityTavernUiStyle.SetFixedSize(chronumText.gameObject, 190f, 42f);
        }

        private static List<Button> BuildOffers(
            Transform parent,
            int chronum,
            IReadOnlyList<MinionInstance> cards,
            IReadOnlyList<TimewarpedOfferSlot> offers,
            bool useEnglish,
            Action<int> buy)
        {
            var row = new GameObject("UnityTimewarpedOfferArea", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(row, 0f, 200f);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 8, 8, 8);
            rowLayout.spacing = 12f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var buttons = new List<Button>();
            for (var index = 0; index < OfferSlotCount; index += 1)
            {
                var slot = new GameObject("UnityTimewarpedOfferSlot" + index, typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(row.transform, false);
                UnityTavernUiStyle.SetFixedSize(slot, 144f, 192f);
                UnityTavernUiStyle.ConfigureSurface(slot, UnityTavernUiStyle.TableDark);
                UnityTavernUiStyle.ConfigureOutline(slot, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.ArcaneBlue, 0.44f), new Vector2(1f, -1f));

                var card = cards != null && index < cards.Count ? cards[index] : null;
                var offer = offers != null && index < offers.Count ? offers[index] : null;
                if (card == null || offer == null || offer.Purchased)
                {
                    var empty = UiFactory.Label(
                        "UnityTimewarpedOfferSlot" + index + "State",
                        slot.transform,
                        offer?.Purchased == true ? (useEnglish ? "Purchased" : "已购买") : (useEnglish ? "Unavailable" : "不可用"),
                        16,
                        FontStyle.Bold);
                    empty.alignment = TextAnchor.MiddleCenter;
                    empty.color = UnityTavernUiStyle.MutedText;
                    UnityTavernUiStyle.Stretch(empty.rectTransform);
                    continue;
                }

                var offerIndex = index;
                var affordable = chronum >= Math.Max(0, offer.Cost);
                var cardObject = UnityTavernCardComponent.CreateCardHost(
                    UnityTavernCardMode.Shop,
                    slot.transform,
                    "UnityTimewarpedOfferCard" + index);
                cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                    card,
                    UnityTavernCardMode.Shop,
                    affordable ? (useEnglish ? "Buy " : "购买 ") + Math.Max(0, offer.Cost) : null,
                    _ => buy?.Invoke(offerIndex),
                    _ => buy?.Invoke(offerIndex),
                    useEnglish: useEnglish);
                var rect = cardObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;

                var button = cardObject.GetComponent<Button>();
                button.interactable = affordable;
                buttons.Add(button);

                if (!affordable)
                {
                    var reason = UiFactory.Label(
                        "UnityTimewarpedOfferSlot" + index + "DisabledReason",
                        slot.transform,
                        useEnglish ? "Not enough Chronum" : "时空资源不足",
                        14,
                        FontStyle.Bold);
                    reason.alignment = TextAnchor.LowerCenter;
                    reason.color = UnityTavernUiStyle.DangerRed;
                    reason.rectTransform.anchorMin = new Vector2(0f, 0f);
                    reason.rectTransform.anchorMax = new Vector2(1f, 0f);
                    reason.rectTransform.pivot = new Vector2(0.5f, 0f);
                    reason.rectTransform.sizeDelta = new Vector2(0f, 28f);
                    reason.rectTransform.anchoredPosition = new Vector2(0f, 4f);
                }
            }

            return buttons;
        }

        private static Button BuildFooter(Transform parent, bool useEnglish, Action exit)
        {
            var footer = new GameObject("UnityTimewarpedTavernFooter", typeof(RectTransform));
            footer.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(footer, 0f, 56f);
            var layout = footer.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var button = UiFactory.Button(
                "UnityTimewarpedTavernExitButton",
                footer.transform,
                useEnglish ? "Exit Timewarped Tavern" : "退出时空酒馆",
                () => exit?.Invoke());
            UnityTavernUiStyle.SetFixedSize(button.gameObject, 220f, UnityTavernUiStyle.TouchHeight);
            UnityTavernUiStyle.ConfigureButton(button, UnityTavernUiStyle.Brass);
            return button;
        }

        private static void ConfigureNavigation(List<Button> offerButtons, Button exitButton)
        {
            var selectable = offerButtons.FindAll(button => button != null && button.interactable);
            for (var index = 0; index < selectable.Count; index += 1)
            {
                selectable[index].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = index > 0 ? selectable[index - 1] : exitButton,
                    selectOnRight = index + 1 < selectable.Count ? selectable[index + 1] : exitButton,
                    selectOnDown = exitButton,
                    selectOnUp = exitButton
                };
            }

            exitButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = selectable.Count > 0 ? selectable[selectable.Count - 1] : exitButton,
                selectOnRight = selectable.Count > 0 ? selectable[0] : exitButton,
                selectOnUp = selectable.Count > 0 ? selectable[0] : exitButton,
                selectOnDown = selectable.Count > 0 ? selectable[0] : exitButton
            };

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(
                    selectable.Count > 0 ? selectable[0].gameObject : exitButton.gameObject);
            }
        }
    }
}
