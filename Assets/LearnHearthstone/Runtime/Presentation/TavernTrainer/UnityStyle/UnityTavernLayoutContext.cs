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
            if (UnityEngine.Application.isPlaying && Screen.width > 0 && Screen.height > 0)
            {
                return ForSize(Screen.width, Screen.height);
            }

            var rectTransform = root as RectTransform;
            if (rectTransform != null && rectTransform.rect.width > 0f && rectTransform.rect.height > 0f)
            {
                return ForSize(rectTransform.rect.width, rectTransform.rect.height);
            }

            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            if (screenWidth > 0 && screenHeight > 0)
            {
                return ForSize(screenWidth, screenHeight);
            }

            return ForSize(1366f, 768f);
        }

        public UnityTavernZoneMetrics ZoneMetrics(UnityTavernZoneKind kind, UnityTavernCardMode cardMode)
        {
            if (IsCompact)
            {
                return CompactZoneMetrics(kind, cardMode);
            }

            return IsWide ? WideZoneMetrics(kind, cardMode) : StandardZoneMetrics(kind, cardMode);
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

        private static UnityTavernZoneMetrics StandardZoneMetrics(UnityTavernZoneKind kind, UnityTavernCardMode cardMode)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.Hand:
                    return new UnityTavernZoneMetrics(176f, new Vector2(104f, 144f), 0f, 0.9f);
                case UnityTavernZoneKind.PlayerBoard:
                    return new UnityTavernZoneMetrics(158f, new Vector2(108f, 120f), 10f, 0.9f);
                case UnityTavernZoneKind.OpponentBoard:
                    return new UnityTavernZoneMetrics(150f, new Vector2(102f, 114f), 8f, 0.86f);
                default:
                    return new UnityTavernZoneMetrics(212f, new Vector2(118f, 164f), 12f, 0.88f);
            }
        }

        private static UnityTavernZoneMetrics WideZoneMetrics(UnityTavernZoneKind kind, UnityTavernCardMode cardMode)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.Hand:
                    return new UnityTavernZoneMetrics(196f, new Vector2(118f, 164f), 0f, 1f);
                case UnityTavernZoneKind.PlayerBoard:
                case UnityTavernZoneKind.OpponentBoard:
                    return new UnityTavernZoneMetrics(172f, new Vector2(118f, 132f), 12f, 1f);
                default:
                    return new UnityTavernZoneMetrics(232f, new Vector2(136f, 190f), 14f, 1f);
            }
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
