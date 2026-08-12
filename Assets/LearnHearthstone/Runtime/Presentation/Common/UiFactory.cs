using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;

namespace LearnHearthstone.Presentation.Common
{
    public static class UiFactory
    {
        public const float MinimumButtonHeight = 48f;
        private const string BundledFontResourcePath = "Fonts/NotoSansSC-Regular";
        private static readonly string[] DefaultUiFontNames =
        {
            "Noto Sans SC",
            "Noto Sans CJK SC",
            "Source Han Sans SC",
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "SimHei",
            "PingFang SC",
            "Hiragino Sans GB",
            "Arial Unicode MS",
            "Arial"
        };

        private static readonly Dictionary<string, Font> UiFonts = new Dictionary<string, Font>();
        private static Font fontOverride;

        public static GameObject Panel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return panel;
        }

        public static Text Label(
            string name,
            Transform parent,
            string text,
            int size = 18,
            FontStyle style = FontStyle.Normal,
            UnityTavernLayoutContext? layoutContext = null)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text));
            label.transform.SetParent(parent, false);
            var textComponent = label.GetComponent<Text>();
            var layout = layoutContext ?? UnityTavernLayoutContext.Current();
            var minimumReadableSize = Mathf.CeilToInt(layout.CanvasUnitsForPhysicalPixels(14f));
            var resolvedSize = Mathf.Max(minimumReadableSize, size);
            textComponent.text = text;
            textComponent.font = GetUiFont(resolvedSize, style);
            textComponent.fontSize = resolvedSize;
            textComponent.fontStyle = style;
            textComponent.color = UnityTavernUiStyle.TextLight;
            textComponent.alignment = TextAnchor.MiddleLeft;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
            textComponent.alignByGeometry = true;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        public static Button Button(
            string name,
            Transform parent,
            string text,
            UnityAction onClick,
            UnityTavernLayoutContext? layoutContext = null)
        {
            var layout = layoutContext ?? UnityTavernLayoutContext.Current();
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = UnityTavernUiStyle.SurfaceRaised;
            var minimumTouchSize = layout.CanvasUnitsForPhysicalPixels(MinimumButtonHeight);
            SetMinSize(buttonObject, minimumTouchSize, minimumTouchSize);
            var button = buttonObject.GetComponent<Button>();
            buttonObject.AddComponent<UnitySelectableFocusRing>();
            button.onClick.AddListener(onClick);
            var colors = button.colors;
            colors.normalColor = UnityTavernUiStyle.SurfaceRaised;
            colors.highlightedColor = Color.Lerp(UnityTavernUiStyle.SurfaceRaised, UnityTavernUiStyle.ArcaneBlue, 0.28f);
            colors.pressedColor = Color.Lerp(UnityTavernUiStyle.SurfaceRaised, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Disabled, 0.48f);
            colors.fadeDuration = UnityUiMotionSettings.Duration(0.08f);
            button.colors = colors;

            var label = Label(name + "Label", buttonObject.transform, text, 16, FontStyle.Bold, layout);
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.CeilToInt(layout.CanvasUnitsForPhysicalPixels(14f));
            label.resizeTextMaxSize = label.fontSize;
            Stretch(label.rectTransform);
            return button;
        }

        public static Transform ScrollView(
            string name,
            Transform parent,
            Color background,
            out ScrollRect scrollRect,
            UnityTavernLayoutContext? layoutContext = null,
            bool horizontal = false)
        {
            var layout = layoutContext ?? UnityTavernLayoutContext.Current();
            var scrollbarWidth = layout.CanvasUnitsForPhysicalPixels(20f);
            var scrollbarGap = layout.CanvasUnitsForPhysicalPixels(4f);
            var root = Panel(name, parent, background);
            SetFlexible(root, 1, 1);
            scrollRect = root.AddComponent<ScrollRect>();
            scrollRect.horizontal = horizontal;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 32f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject(name + "Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(root.transform, false);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = background.a > 0f ? background : Color.white;
            viewportImage.raycastTarget = true;
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            var viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewportRect.offsetMin = new Vector2(0f, horizontal ? scrollbarWidth + scrollbarGap : 0f);
            viewportRect.offsetMax = new Vector2(-(scrollbarWidth + scrollbarGap), 0f);

            var content = new GameObject(name + "Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = horizontal
                ? ContentSizeFitter.FitMode.PreferredSize
                : ContentSizeFitter.FitMode.Unconstrained;

            var scrollbarObject = new GameObject(name + "Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(root.transform, false);
            var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(
                -scrollbarWidth,
                horizontal ? scrollbarWidth + scrollbarGap : 0f);
            scrollbarRect.offsetMax = Vector2.zero;
            scrollbarObject.GetComponent<Image>().color = UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.12f);

            var slidingArea = new GameObject(name + "ScrollbarSlidingArea", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarObject.transform, false);
            Stretch(slidingArea.GetComponent<RectTransform>());

            var handle = new GameObject(name + "ScrollbarHandle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);
            handle.GetComponent<Image>().color = UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.ArcaneBlue, 0.62f);
            Stretch(handle.GetComponent<RectTransform>());

            var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle.GetComponent<RectTransform>();
            scrollbar.targetGraphic = handle.GetComponent<Image>();

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            scrollRect.verticalScrollbar = scrollbar;

            if (horizontal)
            {
                var horizontalScrollbarObject = new GameObject(
                    name + "HorizontalScrollbar",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Scrollbar));
                horizontalScrollbarObject.transform.SetParent(root.transform, false);
                var horizontalScrollbarRect = horizontalScrollbarObject.GetComponent<RectTransform>();
                horizontalScrollbarRect.anchorMin = Vector2.zero;
                horizontalScrollbarRect.anchorMax = new Vector2(1f, 0f);
                horizontalScrollbarRect.pivot = new Vector2(0.5f, 0f);
                horizontalScrollbarRect.offsetMin = Vector2.zero;
                horizontalScrollbarRect.offsetMax = new Vector2(
                    -(scrollbarWidth + scrollbarGap),
                    scrollbarWidth);
                horizontalScrollbarObject.GetComponent<Image>().color =
                    UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.12f);

                var horizontalSlidingArea = new GameObject(
                    name + "HorizontalScrollbarSlidingArea",
                    typeof(RectTransform));
                horizontalSlidingArea.transform.SetParent(horizontalScrollbarObject.transform, false);
                Stretch(horizontalSlidingArea.GetComponent<RectTransform>());

                var horizontalHandle = new GameObject(
                    name + "HorizontalScrollbarHandle",
                    typeof(RectTransform),
                    typeof(Image));
                horizontalHandle.transform.SetParent(horizontalSlidingArea.transform, false);
                horizontalHandle.GetComponent<Image>().color =
                    UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.ArcaneBlue, 0.62f);
                Stretch(horizontalHandle.GetComponent<RectTransform>());

                var horizontalScrollbar = horizontalScrollbarObject.GetComponent<Scrollbar>();
                horizontalScrollbar.direction = Scrollbar.Direction.LeftToRight;
                horizontalScrollbar.handleRect = horizontalHandle.GetComponent<RectTransform>();
                horizontalScrollbar.targetGraphic = horizontalHandle.GetComponent<Image>();
                scrollRect.horizontalScrollbar = horizontalScrollbar;
            }
            return content.transform;
        }

        public static void SetTextColor(Text text, Color color)
        {
            text.color = color;
        }

        public static void EnsureFont(Text text)
        {
            if (text != null && text.font == null)
            {
                text.font = GetUiFont(text.fontSize, text.fontStyle);
            }
        }

        public static void SetFontOverride(Font font)
        {
            fontOverride = font;
        }

        public static void SetImageColor(GameObject target, Color color)
        {
            var image = target.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private static Font GetUiFont(int size, FontStyle style)
        {
            var resolvedSize = Mathf.Max(1, size);
            if (fontOverride != null)
            {
                return fontOverride;
            }

            var cacheKey = BundledFontResourcePath + ":" + resolvedSize + ":" + (int)style;
            if (UiFonts.TryGetValue(cacheKey, out var cached) && HasUsableAtlas(cached))
            {
                return cached;
            }

            UiFonts.Remove(cacheKey);

            var font = Font.CreateDynamicFontFromOSFont(DefaultUiFontNames, Mathf.Max(16, resolvedSize));
            font.name = "UiFont-" + resolvedSize + "-" + style;
            font.hideFlags = HideFlags.HideAndDontSave;
            UiFonts[cacheKey] = font;
            return font;
        }

        private static bool HasUsableAtlas(Font font)
        {
            return font != null &&
                   font.material != null &&
                   font.material.mainTexture != null;
        }

        public static VerticalLayoutGroup Vertical(GameObject target, int padding = 10, int spacing = 8)
        {
            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup Horizontal(GameObject target, int padding = 10, int spacing = 8)
        {
            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return layout;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetHeight(GameObject target, float height)
        {
            var element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
            element.preferredHeight = height;
        }

        public static void SetWidth(GameObject target, float width)
        {
            var element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
            element.preferredWidth = width;
        }

        public static void SetMinSize(GameObject target, float width, float height)
        {
            var element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
            element.minWidth = width;
            element.minHeight = height;
        }

        public static void SetFlexible(GameObject target, float width, float height)
        {
            var element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
            element.flexibleWidth = width;
            element.flexibleHeight = height;
        }
    }
}
