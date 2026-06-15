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
            UnityTavernUiStyle.SetPreferredHeight(header, 28f);
            var layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var titleLabel = UiFactory.Label("UnityZoneTitle", header.transform, title, 15, FontStyle.Bold);
            titleLabel.color = UnityTavernUiStyle.Text;
            titleLabel.alignment = TextAnchor.MiddleLeft;

            var subtitleLabel = UiFactory.Label("UnityZoneSubtitle", header.transform, subtitle, 11, FontStyle.Bold);
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
            ConfigureSlotParent(parent, metrics);
            ConfigureRowVisuals(parent);

            var totalSlots = stableSlotCount > 0 ? stableSlotCount : cards.Count;
            for (var index = 0; index < totalSlots; index += 1)
            {
                var card = index < cards.Count ? cards[index] : null;
                var slot = CreateSlot(parent, gameObject.name + "Slot-" + index, cardMode, card == null);
                UnityTavernUiStyle.SetFixedSize(slot, metrics.SlotSize.x, metrics.SlotSize.y);
                configureSlot?.Invoke(slot, index);

                var fallbackName = card == null ? "UnityEmptySlotCard" : "UnityCardHost-" + card.InstanceId;
                var cardObject = UnityTavernCardComponent.CreateCardHost(cardMode, slot.transform, fallbackName, CardPrefabFor(cardMode));
                var cardRect = cardObject.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;

                cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                    card,
                    cardMode,
                    card == null ? null : actionLabel?.Invoke(card),
                    onSelect,
                    onPrimaryAction);
                cardObject.transform.localScale = new Vector3(metrics.CardScale, metrics.CardScale, 1f);
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

        private static void ConfigureSlotParent(Transform parent, UnityTavernZoneMetrics metrics)
        {
            var layout = parent.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                return;
            }

            layout.spacing = metrics.SlotSpacing;
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
            outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.48f);
            outline.effectDistance = new Vector2(2f, -2f);
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

            var layout = header.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(10, 0, 0, 0);
            }

            var mark = header.Find("UnityZoneAccentMark") as RectTransform;
            if (mark == null)
            {
                var markObject = new GameObject("UnityZoneAccentMark", typeof(RectTransform), typeof(Image));
                markObject.transform.SetParent(header, false);
                mark = markObject.GetComponent<RectTransform>();
            }

            mark.SetSiblingIndex(0);
            UnityTavernUiStyle.SetFixedSize(mark.gameObject, 5f, 20f);
            var markElement = UnityTavernUiStyle.EnsureComponent<LayoutElement>(mark.gameObject);
            markElement.ignoreLayout = true;
            mark.anchorMin = new Vector2(0f, 0.5f);
            mark.anchorMax = new Vector2(0f, 0.5f);
            mark.pivot = new Vector2(0f, 0.5f);
            mark.sizeDelta = new Vector2(5f, 20f);
            mark.anchoredPosition = new Vector2(0f, 0f);
            var markImage = UnityTavernUiStyle.EnsureComponent<Image>(mark.gameObject);
            markImage.color = ZoneAccentColor(zoneKind);
            markImage.raycastTarget = false;

            if (title != null)
            {
                title.color = UnityTavernUiStyle.Text;
            }

            if (subtitle != null)
            {
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
                    return UnityTavernUiStyle.ColorFromHex(0x2E2619);
                case UnityTavernZoneKind.PlayerBoard:
                    return UnityTavernUiStyle.ColorFromHex(0x1C2D23);
                case UnityTavernZoneKind.OpponentBoard:
                    return UnityTavernUiStyle.ColorFromHex(0x232A38);
                case UnityTavernZoneKind.Hand:
                    return UnityTavernUiStyle.ColorFromHex(0x182A34);
                default:
                    return UnityTavernUiStyle.Panel;
            }
        }

        private static Color ZoneAccentColor(UnityTavernZoneKind kind)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.Shop:
                    return UnityTavernUiStyle.Gold;
                case UnityTavernZoneKind.PlayerBoard:
                    return UnityTavernUiStyle.Green;
                case UnityTavernZoneKind.OpponentBoard:
                    return UnityTavernUiStyle.ColorFromHex(0x6D7FA8);
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
            color.a = 0.38f;
            return color;
        }

        private static Color SlotColor(UnityTavernZoneKind kind, bool empty)
        {
            var color = Color.Lerp(ZoneSurfaceColor(kind), ZoneAccentColor(kind), empty ? 0.18f : 0.08f);
            color.a = empty ? 0.66f : 0.28f;
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
