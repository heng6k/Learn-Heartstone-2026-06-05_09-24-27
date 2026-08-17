using UnityEngine;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public enum UnityTavernLayoutMode
    {
        Wide,
        Standard,
        Compact,
        ShortLandscape
    }

    public readonly struct UnityTavernLayoutContext
    {
        public const float CompactWidth = 1000f;
        public const float CompactHeight = 650f;
        public const float WideWidth = 1400f;
        public const float WideHeight = 800f;
        public const float CanvasReferenceWidth = 1920f;
        public const float CanvasReferenceHeight = 1080f;
        public const float MobileReferenceDpi = 160f;
        public const float ShortLandscapeMaxHeight = 480f;
        public const float ShortLandscapeMinAspect = 1.6f;

        public UnityTavernLayoutContext(float width, float height)
            : this(width, height, width, height, 1f)
        {
        }

        private UnityTavernLayoutContext(
            float width,
            float height,
            float pixelWidth,
            float pixelHeight,
            float densityScale)
        {
            Width = width;
            Height = height;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
            DensityScale = densityScale;
            Mode = ResolveMode(width, height);
        }

        public float Width { get; }

        public float Height { get; }

        public float PixelWidth { get; }

        public float PixelHeight { get; }

        public float DensityScale { get; }

        public UnityTavernLayoutMode Mode { get; }

        public bool IsCompact => Mode == UnityTavernLayoutMode.Compact || IsShortLandscape;

        public bool IsShortLandscape => Mode == UnityTavernLayoutMode.ShortLandscape;

        public bool IsWide => Mode == UnityTavernLayoutMode.Wide;

        public float ZoneStackSpacing => IsShortLandscape ? 4f : IsCompact ? 6f : 10f;

        public float CanvasMatchWidthOrHeight => IsCompact ? 0f : 0.5f;

        public float CanvasScaleFactor
        {
            get
            {
                var widthScale = Mathf.Max(PixelWidth / CanvasReferenceWidth, 0.0001f);
                var heightScale = Mathf.Max(PixelHeight / CanvasReferenceHeight, 0.0001f);
                var logWidth = Mathf.Log(widthScale, 2f);
                var logHeight = Mathf.Log(heightScale, 2f);
                return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, CanvasMatchWidthOrHeight));
            }
        }

        public float CanvasUnitsForPhysicalPixels(float physicalPixels)
        {
            return Mathf.Max(0f, physicalPixels) / Mathf.Max(0.01f, CanvasScaleFactor);
        }

        public float CanvasUnitsForDensityIndependentPixels(float densityIndependentPixels)
        {
            return CanvasUnitsForPhysicalPixels(Mathf.Max(0f, densityIndependentPixels) * DensityScale);
        }

        public float CanvasUnitsForTouchTarget(float densityIndependentPixels = 48f)
        {
            return CanvasUnitsForDensityIndependentPixels(densityIndependentPixels);
        }

        public static UnityTavernLayoutContext Current()
        {
            return ForScreen(
                Screen.width,
                Screen.height,
                Screen.safeArea,
                Screen.dpi,
                UnityEngine.Application.isMobilePlatform);
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

        public static UnityTavernLayoutContext ForScreen(
            float pixelWidth,
            float pixelHeight,
            Rect safeArea,
            float dpi,
            bool handheld)
        {
            pixelWidth = pixelWidth > 0f ? pixelWidth : 1366f;
            pixelHeight = pixelHeight > 0f ? pixelHeight : 768f;
            var safeWidth = safeArea.width > 0f ? Mathf.Min(safeArea.width, pixelWidth) : pixelWidth;
            var safeHeight = safeArea.height > 0f ? Mathf.Min(safeArea.height, pixelHeight) : pixelHeight;
            var densityScale = ResolveDensityScale(pixelWidth, pixelHeight, dpi, handheld);
            return new UnityTavernLayoutContext(
                safeWidth / densityScale,
                safeHeight / densityScale,
                pixelWidth,
                pixelHeight,
                densityScale);
        }

        public static UnityTavernLayoutContext FromRoot(Transform root)
        {
            if (UnityEngine.Application.isPlaying && Screen.width > 0 && Screen.height > 0)
            {
                return Current();
            }

            var rectTransform = root as RectTransform;
            if (rectTransform != null && rectTransform.rect.width > 0f && rectTransform.rect.height > 0f)
            {
                return ForSize(rectTransform.rect.width, rectTransform.rect.height);
            }

            // EditMode view construction must not inherit the last GameView resolution.
            // Tests and prefab tooling often use a zero-sized temporary root; treating the
            // current 844x390 GameView as that root makes unrelated desktop views switch shells.
            return ForSize(1366f, 768f);
        }

        public UnityTavernZoneMetrics ZoneMetrics(UnityTavernZoneKind kind, UnityTavernCardMode cardMode)
        {
            if (IsShortLandscape)
            {
                return ShortLandscapeZoneMetrics(kind);
            }

            if (IsCompact)
            {
                return CompactZoneMetrics(kind, cardMode);
            }

            return IsWide ? WideZoneMetrics(kind, cardMode) : StandardZoneMetrics(kind, cardMode);
        }

        public float HandZoneHeight(int handCount)
        {
            if (IsShortLandscape)
            {
                return CanvasUnitsForDensityIndependentPixels(UnityTavernUiStyle.ShortLandscapeHandPeekHeight);
            }

            var expandedHeight = ZoneMetrics(UnityTavernZoneKind.Hand, UnityTavernCardMode.Hand).Height;
            var collapsedPhysicalHeight = IsCompact ? 48f : 56f;
            var collapsedHeight = Mathf.Min(
                expandedHeight - 12f,
                CanvasUnitsForPhysicalPixels(collapsedPhysicalHeight));
            if (handCount <= 0)
            {
                return collapsedHeight;
            }

            var firstCardHeight = Mathf.Max(collapsedHeight, expandedHeight * (IsCompact ? 0.70f : 0.68f));
            var density = Mathf.Clamp01((Mathf.Clamp(handCount, 1, 10) - 1f) / 9f);
            return Mathf.Lerp(firstCardHeight, expandedHeight, density);
        }

        private static UnityTavernLayoutMode ResolveMode(float width, float height)
        {
            if (width > height &&
                width / Mathf.Max(1f, height) >= ShortLandscapeMinAspect &&
                height <= ShortLandscapeMaxHeight)
            {
                return UnityTavernLayoutMode.ShortLandscape;
            }

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

        private static float ResolveDensityScale(float pixelWidth, float pixelHeight, float dpi, bool handheld)
        {
            if (!handheld)
            {
                return 1f;
            }

            if (dpi >= MobileReferenceDpi)
            {
                return Mathf.Clamp(dpi / MobileReferenceDpi, 1f, 4f);
            }

            return Mathf.Min(pixelWidth, pixelHeight) >= 720f ? 2f : 1f;
        }

        private static UnityTavernZoneMetrics CompactZoneMetrics(UnityTavernZoneKind kind, UnityTavernCardMode cardMode)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.OpponentBoard:
                    return new UnityTavernZoneMetrics(118f, new Vector2(110f, 122f), 5f, 1.05f);
                case UnityTavernZoneKind.PlayerBoard:
                    return new UnityTavernZoneMetrics(200f, new Vector2(168f, 184f), 5f, 1.25f);
                case UnityTavernZoneKind.Hand:
                    return new UnityTavernZoneMetrics(140f, new Vector2(100f, 142f), 4f, 1.05f);
                default:
                    return new UnityTavernZoneMetrics(240f, new Vector2(188f, 252f), 8f, 1.35f);
            }
        }

        private UnityTavernZoneMetrics ShortLandscapeZoneMetrics(UnityTavernZoneKind kind)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.OpponentBoard:
                    return ShortLandscapeMetrics(116f, 82f, 92f, 4f, 1f);
                case UnityTavernZoneKind.PlayerBoard:
                    return ShortLandscapeMetrics(
                        UnityTavernUiStyle.ShortLandscapeBoardHeight,
                        82f,
                        92f,
                        4f,
                        1f);
                case UnityTavernZoneKind.Hand:
                    return ShortLandscapeMetrics(
                        UnityTavernUiStyle.ShortLandscapeHandPeekHeight,
                        58f,
                        78f,
                        4f,
                        1f);
                default:
                    return ShortLandscapeMetrics(
                        UnityTavernUiStyle.ShortLandscapeShopHeight,
                        64f,
                        72f,
                        4f,
                        1f);
            }
        }

        private UnityTavernZoneMetrics ShortLandscapeMetrics(
            float height,
            float slotWidth,
            float slotHeight,
            float spacing,
            float cardScale)
        {
            return new UnityTavernZoneMetrics(
                CanvasUnitsForDensityIndependentPixels(height),
                new Vector2(
                    CanvasUnitsForDensityIndependentPixels(slotWidth),
                    CanvasUnitsForDensityIndependentPixels(slotHeight)),
                CanvasUnitsForDensityIndependentPixels(spacing),
                cardScale);
        }

        private static UnityTavernZoneMetrics StandardZoneMetrics(UnityTavernZoneKind kind, UnityTavernCardMode cardMode)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.Hand:
                    return new UnityTavernZoneMetrics(156f, new Vector2(112f, 154f), 0f, 1f);
                case UnityTavernZoneKind.PlayerBoard:
                    return new UnityTavernZoneMetrics(218f, new Vector2(154f, 170f), 10f, 1.2f);
                case UnityTavernZoneKind.OpponentBoard:
                    return new UnityTavernZoneMetrics(170f, new Vector2(130f, 145f), 8f, 1.08f);
                default:
                    return new UnityTavernZoneMetrics(250f, new Vector2(168f, 236f), 14f, 1.25f);
            }
        }

        private static UnityTavernZoneMetrics WideZoneMetrics(UnityTavernZoneKind kind, UnityTavernCardMode cardMode)
        {
            switch (kind)
            {
                case UnityTavernZoneKind.Hand:
                    return new UnityTavernZoneMetrics(198f, new Vector2(120f, 166f), 0f, 1f);
                case UnityTavernZoneKind.PlayerBoard:
                case UnityTavernZoneKind.OpponentBoard:
                    return new UnityTavernZoneMetrics(206f, new Vector2(142f, 158f), 12f, 1.1f);
                default:
                    return new UnityTavernZoneMetrics(250f, new Vector2(154f, 216f), 14f, 1.12f);
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
