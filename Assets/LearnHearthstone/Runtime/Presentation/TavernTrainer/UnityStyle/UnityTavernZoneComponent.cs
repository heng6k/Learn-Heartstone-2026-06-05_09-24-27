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

            if (zoneObject.GetComponent<UnityTavernZoneComponent>() == null)
            {
                zoneObject.AddComponent<UnityTavernZoneComponent>();
            }

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
            Action<GameObject, int> configureSlot = null)
        {
            var image = UnityTavernUiStyle.EnsureComponent<Image>(gameObject);
            image.color = UnityTavernUiStyle.Panel;
            image.raycastTarget = false;

            if (HasPrefabReferences())
            {
                BuildPrefabZone(title, subtitle, cards, stableSlotCount, cardMode, actionLabel, onSelect, onPrimaryAction, configureCard, configureSlot);
                return;
            }

            ClearChildren();

            var vertical = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(gameObject);
            vertical.padding = new RectOffset(12, 12, 10, 12);
            vertical.spacing = 8;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            BuildHeader(title, subtitle);
            BuildGeneratedRow(cards, stableSlotCount, cardMode, actionLabel, onSelect, onPrimaryAction, configureCard, configureSlot);
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
            Action<GameObject, int> configureSlot)
        {
            SetText(titleText, title);
            SetText(subtitleText, subtitle);

            var parent = slotParent != null ? slotParent : transform;
            ClearChildren(parent);
            BuildSlots(parent, cards, stableSlotCount, cardMode, actionLabel, onSelect, onPrimaryAction, configureCard, configureSlot);
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
        }

        private void BuildGeneratedRow(
            IReadOnlyList<MinionInstance> cards,
            int stableSlotCount,
            UnityTavernCardMode cardMode,
            Func<MinionInstance, string> actionLabel,
            Action<MinionInstance> onSelect,
            Action<MinionInstance> onPrimaryAction,
            Action<GameObject, MinionInstance, int> configureCard,
            Action<GameObject, int> configureSlot)
        {
            var rowObject = new GameObject("UnityZoneCardRow", typeof(RectTransform));
            rowObject.transform.SetParent(transform, false);
            UnityTavernUiStyle.SetFlexible(rowObject, 1f, 1f);
            row = rowObject.transform;

            var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            BuildSlots(row, cards, stableSlotCount, cardMode, actionLabel, onSelect, onPrimaryAction, configureCard, configureSlot);
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
            Action<GameObject, int> configureSlot)
        {
            var totalSlots = stableSlotCount > 0 ? stableSlotCount : cards.Count;
            for (var index = 0; index < totalSlots; index += 1)
            {
                var card = index < cards.Count ? cards[index] : null;
                var slot = CreateSlot(parent, gameObject.name + "Slot-" + index, cardMode, card == null);
                UnityTavernUiStyle.SetFixedSize(slot, cardMode == UnityTavernCardMode.Board ? 118f : 136f, cardMode == UnityTavernCardMode.Board ? 132f : 190f);
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
            image.color = empty ? new Color(0.05f, 0.065f, 0.065f, 0.62f) : Color.clear;
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
