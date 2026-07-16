using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public enum UnityTavernZoneKind
    {
        Shop,
        Hand,
        PlayerBoard,
        OpponentBoard
    }

    public sealed class UnityTavernZoneComponent : MonoBehaviour
    {
        private const float HandMinimumSpacingRatio = 0.48f;
        private const float HandMaximumSpacingRatio = 0.82f;
        private const float HandArcHeight = 12f;
        private const float HandMaximumRotation = 6f;

        public const string ShopZonePrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Zones/ShopZone.prefab";
        public const string HandZonePrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Zones/HandZone.prefab";
        public const string PlayerBoardZonePrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Zones/PlayerBoardZone.prefab";
        public const string OpponentBoardZonePrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Zones/OpponentBoardZone.prefab";
        public const string ShopZonePrefabResourcePath = "TavernTrainer/UnityStyle/Zones/ShopZone";
        public const string HandZonePrefabResourcePath = "TavernTrainer/UnityStyle/Zones/HandZone";
        public const string PlayerBoardZonePrefabResourcePath = "TavernTrainer/UnityStyle/Zones/PlayerBoardZone";
        public const string OpponentBoardZonePrefabResourcePath = "TavernTrainer/UnityStyle/Zones/OpponentBoardZone";

        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Transform slotParent;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private GameObject tavernCardPrefab;
        [SerializeField] private GameObject boardMinionPrefab;
        [SerializeField] private UnityTavernZoneKind zoneKind;

        private Transform row;

        public static GameObject CreateZoneHost(UnityTavernZoneKind kind, Transform parent, string fallbackName)
        {
            var prefab = ResolveZonePrefab(kind);
            var zoneObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityTavernZoneComponent));

            zoneObject.name = fallbackName;
            zoneObject.transform.SetParent(parent, false);
            if (zoneObject.GetComponent<Image>() == null)
            {
                zoneObject.AddComponent<Image>();
            }

            var component = zoneObject.GetComponent<UnityTavernZoneComponent>();
            if (component == null)
            {
                component = zoneObject.AddComponent<UnityTavernZoneComponent>();
            }

            component.zoneKind = kind;
            return zoneObject;
        }

        public void ConfigureReferences(
            Text title = null,
            Text subtitle = null,
            Transform slots = null,
            GameObject slotPrefabAsset = null,
            GameObject tavernCardPrefabAsset = null,
            GameObject boardMinionPrefabAsset = null)
        {
            titleText = title;
            subtitleText = subtitle;
            slotParent = slots;
            slotPrefab = slotPrefabAsset;
            tavernCardPrefab = tavernCardPrefabAsset;
            boardMinionPrefab = boardMinionPrefabAsset;
        }

        public void Build(
            string title,
            string subtitle,
            IReadOnlyList<MinionInstance> cards,
            int stableSlotCount,
            UnityTavernCardMode cardMode,
            Func<MinionInstance, string> actionLabel,
            Action<MinionInstance> onSelect,
            Action<MinionInstance> onPrimaryAction,
            Action<GameObject, MinionInstance, int> configureCard = null,
            Action<GameObject, int> configureSlot = null,
            UnityTavernLayoutContext? layoutContext = null)
        {
            var resolvedLayout = layoutContext ?? UnityTavernLayoutContext.Current();
            var image = UnityTavernUiStyle.EnsureComponent<Image>(gameObject);
            image.color = ZoneSurfaceColor(zoneKind);
            image.raycastTarget = false;
            ConfigureZoneFrame();

            if (HasPrefabReferences())
            {
                var rootLayout = GetComponent<VerticalLayoutGroup>();
                if (rootLayout != null)
                {
                    ConfigureRootLayout(rootLayout, resolvedLayout);
                }

                BuildPrefabZone(title, subtitle, cards, stableSlotCount, cardMode, actionLabel, onSelect, onPrimaryAction, configureCard, configureSlot, resolvedLayout);
                return;
            }

            ClearChildren();

            var vertical = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(gameObject);
            ConfigureRootLayout(vertical, resolvedLayout);
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            BuildHeader(title, subtitle);
            BuildGeneratedRow(cards, stableSlotCount, cardMode, actionLabel, onSelect, onPrimaryAction, configureCard, configureSlot, resolvedLayout);
        }

        private void BuildPrefabZone(
            string title,
            string subtitle,
            IReadOnlyList<MinionInstance> cards,
            int stableSlotCount,
            UnityTavernCardMode cardMode,
            Func<MinionInstance, string> actionLabel,
            Action<MinionInstance> onSelect,
            Action<MinionInstance> onPrimaryAction,
            Action<GameObject, MinionInstance, int> configureCard,
            Action<GameObject, int> configureSlot,
            UnityTavernLayoutContext layout)
        {
            SetText(titleText, title);
            SetText(subtitleText, subtitle);
            ConfigureHeaderVisuals(ResolveHeader(), titleText, subtitleText);

            var parent = slotParent != null ? slotParent : transform;
            ClearChildren(parent);
            BuildSlots(parent, cards, stableSlotCount, cardMode, actionLabel, onSelect, onPrimaryAction, configureCard, configureSlot, layout);
        }

        private void BuildHeader(string title, string subtitle)
        {
            var header = new GameObject("UnityZoneHeader", typeof(RectTransform));
            header.transform.SetParent(transform, false);
            UnityTavernUiStyle.SetPreferredHeight(header, 32f);
            var layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var titleLabel = UiFactory.Label("UnityZoneTitle", header.transform, title, 16, FontStyle.Bold);
            titleLabel.color = UnityTavernUiStyle.Text;
            titleLabel.alignment = TextAnchor.MiddleLeft;

            var subtitleLabel = UiFactory.Label("UnityZoneSubtitle", header.transform, subtitle, 14, FontStyle.Bold);
            subtitleLabel.color = UnityTavernUiStyle.MutedText;
            subtitleLabel.alignment = TextAnchor.MiddleRight;
            ConfigureHeaderVisuals(header.transform, titleLabel, subtitleLabel);
        }

        private void BuildGeneratedRow(
            IReadOnlyList<MinionInstance> cards,
            int stableSlotCount,
            UnityTavernCardMode cardMode,
            Func<MinionInstance, string> actionLabel,
            Action<MinionInstance> onSelect,
            Action<MinionInstance> onPrimaryAction,
            Action<GameObject, MinionInstance, int> configureCard,
            Action<GameObject, int> configureSlot,
            UnityTavernLayoutContext layoutContext)
        {
            var rowObject = new GameObject("UnityZoneCardRow", typeof(RectTransform));
            rowObject.transform.SetParent(transform, false);
            UnityTavernUiStyle.SetFlexible(rowObject, 1f, 1f);
            row = rowObject.transform;

            var metrics = layoutContext.ZoneMetrics(zoneKind, cardMode);
            var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = metrics.SlotSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            BuildSlots(row, cards, stableSlotCount, cardMode, actionLabel, onSelect, onPrimaryAction, configureCard, configureSlot, layoutContext);
        }

        private void BuildSlots(
            Transform parent,
            IReadOnlyList<MinionInstance> cards,
            int stableSlotCount,
            UnityTavernCardMode cardMode,
            Func<MinionInstance, string> actionLabel,
            Action<MinionInstance> onSelect,
            Action<MinionInstance> onPrimaryAction,
            Action<GameObject, MinionInstance, int> configureCard,
            Action<GameObject, int> configureSlot,
            UnityTavernLayoutContext layout)
        {
            var metrics = layout.ZoneMetrics(zoneKind, cardMode);
            var handCardCount = cards != null ? cards.Count : 0;
            var layoutHand = cardMode == UnityTavernCardMode.Hand;
            ConfigureSlotParent(parent, metrics, layoutHand);
            ConfigureRowVisuals(parent);

            var totalSlots = layoutHand
                ? handCardCount
                : stableSlotCount > 0 ? stableSlotCount : handCardCount;
            for (var index = 0; index < totalSlots; index += 1)
            {
                var card = index < handCardCount ? cards[index] : null;
                var slot = CreateSlot(parent, gameObject.name + "Slot-" + index, cardMode, card == null);
                UnityTavernUiStyle.SetFixedSize(slot, metrics.SlotSize.x, metrics.SlotSize.y);
                if (layoutHand)
                {
                    ConfigureHandSlot(slot, index, totalSlots, metrics);
                }

                configureSlot?.Invoke(slot, index);

                var fallbackName = card == null ? "UnityEmptySlotCard" : "UnityCardHost-" + card.InstanceId;
                var cardObject = UnityTavernCardComponent.CreateCardHost(cardMode, slot.transform, fallbackName, CardPrefabFor(cardMode));
                var cardRect = cardObject.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;

                var cardComponent = cardObject.GetComponent<UnityTavernCardComponent>();
                cardComponent.Bind(
                    card,
                    cardMode,
                    card == null ? null : actionLabel?.Invoke(card),
                    onSelect,
                    onPrimaryAction);
                cardComponent.SetLayoutScale(metrics.CardScale);
                if (zoneKind == UnityTavernZoneKind.Hand && cardMode == UnityTavernCardMode.Hand && card != null)
                {
                    cardComponent.SetHandFocusLiftEnabled(true);
                }

                configureCard?.Invoke(cardObject, card, index);
            }
        }

        private GameObject CreateSlot(Transform parent, string name, UnityTavernCardMode cardMode, bool empty)
        {
            var prefab = slotPrefab != null ? slotPrefab : ResolveSlotPrefab();
            var slot = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(name, typeof(RectTransform), typeof(Image));

            slot.name = name;
            slot.transform.SetParent(parent, false);
            var image = UnityTavernUiStyle.EnsureComponent<Image>(slot);
            image.color = SlotColor(zoneKind, empty);
            image.raycastTarget = false;
            if (slot.GetComponent<RectTransform>() == null)
            {
                slot.AddComponent<RectTransform>();
            }

            return slot;
        }

        private GameObject CardPrefabFor(UnityTavernCardMode cardMode)
        {
            return cardMode == UnityTavernCardMode.Board ? boardMinionPrefab : tavernCardPrefab;
        }

        private static void ConfigureRootLayout(VerticalLayoutGroup layout, UnityTavernLayoutContext context)
        {
            if (context.IsCompact)
            {
                layout.padding = new RectOffset(8, 8, 6, 8);
                layout.spacing = 4;
                return;
            }

            layout.padding = new RectOffset(12, 12, 10, 12);
            layout.spacing = 8;
        }

        private static void ConfigureHandSlot(GameObject slot, int index, int totalSlots, UnityTavernZoneMetrics metrics)
        {
            var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(slot);
            element.ignoreLayout = true;

            var rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var center = (totalSlots - 1) * 0.5f;
            var density = totalSlots <= 1 ? 0f : Mathf.Clamp01((totalSlots - 1f) / 9f);
            var spacingRatio = Mathf.Lerp(HandMaximumSpacingRatio, HandMinimumSpacingRatio, density);
            var normalized = center <= 0f ? 0f : (index - center) / center;
            rect.anchoredPosition = new Vector2(
                (index - center) * metrics.SlotSize.x * spacingRatio,
                (1f - normalized * normalized) * HandArcHeight);
            rect.localRotation = Quaternion.Euler(0f, 0f, -normalized * HandMaximumRotation);
        }

        private static void ConfigureSlotParent(Transform parent, UnityTavernZoneMetrics metrics, bool layoutHand)
        {
            var layout = parent.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                return;
            }

            layout.enabled = !layoutHand;
            if (layoutHand)
            {
                return;
            }

            layout.spacing = metrics.SlotSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void ConfigureZoneFrame()
        {
            var outline = UnityTavernUiStyle.EnsureComponent<Outline>(gameObject);
            var accent = ZoneAccentColor(zoneKind);
            outline.enabled = true;
            outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.62f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;
        }

        private void ConfigureHeaderVisuals(Transform header, Text title, Text subtitle)
        {
            if (header == null)
            {
                return;
            }

            var headerImage = UnityTavernUiStyle.EnsureComponent<Image>(header.gameObject);
            headerImage.color = ZoneHeaderColor(zoneKind);
            headerImage.raycastTarget = false;
            UnityTavernUiStyle.SetPreferredHeight(header.gameObject, 32f);

            var layout = header.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(24, 8, 0, 0);
            }

            var mark = header.Find("UnityZoneAccentMark") as RectTransform;
            if (mark == null)
            {
                var markObject = new GameObject("UnityZoneAccentMark", typeof(RectTransform), typeof(Image));
                markObject.transform.SetParent(header, false);
                mark = markObject.GetComponent<RectTransform>();
            }

            mark.SetSiblingIndex(0);
            UnityTavernUiStyle.SetFixedSize(mark.gameObject, 10f, 10f);
            var markElement = UnityTavernUiStyle.EnsureComponent<LayoutElement>(mark.gameObject);
            markElement.ignoreLayout = true;
            mark.anchorMin = new Vector2(0f, 0.5f);
            mark.anchorMax = new Vector2(0f, 0.5f);
            mark.pivot = new Vector2(0.5f, 0.5f);
            mark.sizeDelta = new Vector2(10f, 10f);
            mark.anchoredPosition = new Vector2(10f, 0f);
            mark.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var markImage = UnityTavernUiStyle.EnsureComponent<Image>(mark.gameObject);
            markImage.color = ZoneAccentColor(zoneKind);
            markImage.raycastTarget = false;

            if (title != null)
            {
                UiFactory.EnsureFont(title);
                title.fontSize = Mathf.Max(16, title.fontSize);
                title.color = UnityTavernUiStyle.Text;
            }

            if (subtitle != null)
            {
                UiFactory.EnsureFont(subtitle);
                subtitle.fontSize = Mathf.Max(14, subtitle.fontSize);
                var accent = ZoneAccentColor(zoneKind);
                subtitle.color = new Color(accent.r, accent.g, accent.b, 0.95f);
            }
        }

        private void ConfigureRowVisuals(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            var image = UnityTavernUiStyle.EnsureComponent<Image>(parent.gameObject);
            image.color = ZoneRowColor(zoneKind);
            image.raycastTarget = false;
        }

        private Transform ResolveHeader()
        {
            if (titleText != null && titleText.transform.parent != null)
            {
                return titleText.transform.parent;
            }

            return subtitleText != null ? subtitleText.transform.parent : null;
        }

        private static Color ZoneSurfaceColor(UnityTavernZoneKind kind)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.Shop:
                    return Color.Lerp(UnityTavernUiStyle.TableDark, UnityTavernUiStyle.TableLit, 0.18f);
                case UnityTavernZoneKind.PlayerBoard:
                    return Color.Lerp(UnityTavernUiStyle.SurfaceDark, UnityTavernUiStyle.ArcaneBlue, 0.08f);
                case UnityTavernZoneKind.OpponentBoard:
                    return Color.Lerp(UnityTavernUiStyle.SurfaceDark, UnityTavernUiStyle.CombatRed, 0.12f);
                case UnityTavernZoneKind.Hand:
                    return Color.Lerp(UnityTavernUiStyle.SurfaceDark, UnityTavernUiStyle.ArcaneBlue, 0.16f);
                default:
                    return UnityTavernUiStyle.Panel;
            }
        }

        private static Color ZoneAccentColor(UnityTavernZoneKind kind)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.Shop:
                    return UnityTavernUiStyle.Brass;
                case UnityTavernZoneKind.PlayerBoard:
                    return UnityTavernUiStyle.ArcaneBlue;
                case UnityTavernZoneKind.OpponentBoard:
                    return UnityTavernUiStyle.CombatRed;
                case UnityTavernZoneKind.Hand:
                    return UnityTavernUiStyle.Blue;
                default:
                    return UnityTavernUiStyle.MutedText;
            }
        }

        private static Color ZoneHeaderColor(UnityTavernZoneKind kind)
        {
            var color = Color.Lerp(ZoneSurfaceColor(kind), ZoneAccentColor(kind), 0.16f);
            color.a = 0.88f;
            return color;
        }

        private static Color ZoneRowColor(UnityTavernZoneKind kind)
        {
            var color = Color.Lerp(ZoneSurfaceColor(kind), Color.black, 0.18f);
            color.a = kind == UnityTavernZoneKind.PlayerBoard ? 0.28f : 0.42f;
            return color;
        }

        private static Color SlotColor(UnityTavernZoneKind kind, bool empty)
        {
            var color = Color.Lerp(ZoneSurfaceColor(kind), ZoneAccentColor(kind), empty ? 0.08f : 0.14f);
            color.a = empty ? 0.38f : 0.46f;
            return color;
        }

        private bool HasPrefabReferences()
        {
            return titleText != null
                || subtitleText != null
                || slotParent != null
                || slotPrefab != null
                || tavernCardPrefab != null
                || boardMinionPrefab != null;
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static GameObject ResolveSlotPrefab()
        {
#if UNITY_EDITOR
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCardComponent.CardSlotPrefabAssetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(UnityTavernCardComponent.CardSlotPrefabResourcePath);
        }

        private static GameObject ResolveZonePrefab(UnityTavernZoneKind kind)
        {
            var assetPath = ZoneAssetPath(kind);
            var resourcePath = ZoneResourcePath(kind);

#if UNITY_EDITOR
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(resourcePath);
        }

        private static string ZoneAssetPath(UnityTavernZoneKind kind)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.Hand:
                    return HandZonePrefabAssetPath;
                case UnityTavernZoneKind.PlayerBoard:
                    return PlayerBoardZonePrefabAssetPath;
                case UnityTavernZoneKind.OpponentBoard:
                    return OpponentBoardZonePrefabAssetPath;
                default:
                    return ShopZonePrefabAssetPath;
            }
        }

        private static string ZoneResourcePath(UnityTavernZoneKind kind)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.Hand:
                    return HandZonePrefabResourcePath;
                case UnityTavernZoneKind.PlayerBoard:
                    return PlayerBoardZonePrefabResourcePath;
                case UnityTavernZoneKind.OpponentBoard:
                    return OpponentBoardZonePrefabResourcePath;
                default:
                    return ShopZonePrefabResourcePath;
            }
        }

        private void ClearChildren()
        {
            ClearChildren(transform);
        }

        private static void ClearChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index -= 1)
            {
                var child = parent.GetChild(index).gameObject;
                if (UnityEngine.Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child);
                }
            }
        }
    }
}
