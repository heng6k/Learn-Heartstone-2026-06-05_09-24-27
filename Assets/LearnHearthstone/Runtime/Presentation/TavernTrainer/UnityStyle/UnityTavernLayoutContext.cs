using UnityEngine;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public enum UnityTavernLayoutMode
    {
        Wide,
        Standard,
        Compact
    }

    public readonly struct UnityTavernLayoutContext
    {
        public const float CompactWidth = 1000f;
        public const float CompactHeight = 650f;
        public const float WideWidth = 1400f;
        public const float WideHeight = 800f;

        public UnityTavernLayoutContext(float width, float height)
        {
            Width = width;
            Height = height;
            Mode = ResolveMode(width, height);
        }

        public float Width { get; }

        public float Height { get; }

        public UnityTavernLayoutMode Mode { get; }

        public bool IsCompact => Mode == UnityTavernLayoutMode.Compact;

        public bool IsWide => Mode == UnityTavernLayoutMode.Wide;

        public float ZoneStackSpacing => IsCompact ? 6f : 10f;

        public static UnityTavernLayoutContext Current()
        {
            return ForSize(Screen.width, Screen.height);
        }

        public static UnityTavernLayoutContext ForSize(float width, float height)
        {
            if (width <= 0f)
            {
                width = 1366f;
            }

            if (height <= 0f)
            {
                height = 768f;
            }

            return new UnityTavernLayoutContext(width, height);
        }

        public static UnityTavernLayoutContext FromRoot(Transform root)
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            if (screenWidth > 0 && screenHeight > 0)
            {
                return ForSize(screenWidth, screenHeight);
            }

            var rectTransform = root as RectTransform;
            if (rectTransform != null && rectTransform.rect.width > 0f && rectTransform.rect.height > 0f)
            {
                return ForSize(rectTransform.rect.width, rectTransform.rect.height);
            }

            return ForSize(1366f, 768f);
        }

        public UnityTavernZoneMetrics ZoneMetrics(UnityTavernZoneKind kind, UnityTavernCardMode cardMode)
        {
            if (IsCompact)
            {
                return CompactZoneMetrics(kind, cardMode);
            }

            return new UnityTavernZoneMetrics(StandardZoneHeight(kind), StandardSlotSize(cardMode), 8f, 1f);
        }

        private static UnityTavernLayoutMode ResolveMode(float width, float height)
        {
            if (width < CompactWidth || height < CompactHeight)
            {
                return UnityTavernLayoutMode.Compact;
            }

            if (width >= WideWidth && height >= WideHeight)
            {
                return UnityTavernLayoutMode.Wide;
            }

            return UnityTavernLayoutMode.Standard;
        }

        private static UnityTavernZoneMetrics CompactZoneMetrics(UnityTavernZoneKind kind, UnityTavernCardMode cardMode)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.OpponentBoard:
                    return new UnityTavernZoneMetrics(112f, new Vector2(86f, 96f), 6f, 0.72f);
                case UnityTavernZoneKind.PlayerBoard:
                    return new UnityTavernZoneMetrics(136f, new Vector2(98f, 110f), 6f, 0.78f);
                case UnityTavernZoneKind.Hand:
                    return new UnityTavernZoneMetrics(144f, new Vector2(92f, 130f), 6f, 0.84f);
                default:
                    return new UnityTavernZoneMetrics(176f, new Vector2(108f, 150f), 6f, 0.8f);
            }
        }

        private static float StandardZoneHeight(UnityTavernZoneKind kind)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.Hand:
                    return 208f;
                case UnityTavernZoneKind.PlayerBoard:
                case UnityTavernZoneKind.OpponentBoard:
                    return 168f;
                default:
                    return 236f;
            }
        }

        private static Vector2 StandardSlotSize(UnityTavernCardMode cardMode)
        {
            return cardMode == UnityTavernCardMode.Board
                ? new Vector2(118f, 132f)
                : new Vector2(136f, 190f);
        }
    }

    public readonly struct UnityTavernZoneMetrics
    {
        public UnityTavernZoneMetrics(float height, Vector2 slotSize, float slotSpacing, float cardScale)
        {
            Height = height;
            SlotSize = slotSize;
            SlotSpacing = slotSpacing;
            CardScale = cardScale;
        }

        public float Height { get; }

        public Vector2 SlotSize { get; }

        public float SlotSpacing { get; }

        public float CardScale { get; }
    }
}
