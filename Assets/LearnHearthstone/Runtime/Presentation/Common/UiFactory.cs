using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.Common
{
    public static class UiFactory
    {
        public const float MinimumButtonHeight = 44f;

        private static Font uiFont;
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

        public static Text Label(string name, Transform parent, string text, int size = 18, FontStyle style = FontStyle.Normal)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text));
            label.transform.SetParent(parent, false);
            var textComponent = label.GetComponent<Text>();
            textComponent.text = text;
            textComponent.font = GetUiFont(size);
            textComponent.fontSize = size;
            textComponent.fontStyle = style;
            textComponent.color = new Color(0.94f, 0.92f, 0.86f);
            textComponent.alignment = TextAnchor.MiddleLeft;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
            textComponent.alignByGeometry = true;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        public static Button Button(string name, Transform parent, string text, UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = new Color(0.13f, 0.18f, 0.23f);
            SetMinSize(buttonObject, 0f, MinimumButtonHeight);
            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            var colors = button.colors;
            colors.highlightedColor = new Color(0.22f, 0.30f, 0.36f, 1f);
            colors.pressedColor = new Color(0.10f, 0.14f, 0.18f, 1f);
            colors.disabledColor = new Color(0.18f, 0.18f, 0.18f, 0.48f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var label = Label(name + "Label", buttonObject.transform, text, 16, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform);
            return button;
        }

        public static Transform ScrollView(string name, Transform parent, Color background, out ScrollRect scrollRect)
        {
            var root = Panel(name, parent, background);
            SetFlexible(root, 1, 1);
            scrollRect = root.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 32f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject(name + "Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(root.transform, false);
            viewport.GetComponent<Image>().color = background;
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            var viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewportRect.offsetMax = new Vector2(-12f, 0f);

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
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scrollbarObject = new GameObject(name + "Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(root.transform, false);
            var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(8f, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            var slidingArea = new GameObject(name + "ScrollbarSlidingArea", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarObject.transform, false);
            Stretch(slidingArea.GetComponent<RectTransform>());

            var handle = new GameObject(name + "ScrollbarHandle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);
            handle.GetComponent<Image>().color = new Color(0.74f, 0.8f, 0.86f, 0.42f);
            Stretch(handle.GetComponent<RectTransform>());

            var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle.GetComponent<RectTransform>();
            scrollbar.targetGraphic = handle.GetComponent<Image>();

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            scrollRect.verticalScrollbar = scrollbar;
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
                text.font = GetUiFont(text.fontSize);
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

        private static Font GetUiFont(int size)
        {
            if (fontOverride != null)
            {
                return fontOverride;
            }

            if (uiFont != null)
            {
                return uiFont;
            }

            uiFont = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "Microsoft YaHei UI",
                    "Microsoft YaHei",
                    "SimHei",
                    "Noto Sans CJK SC",
                    "Noto Sans SC",
                    "Source Han Sans SC",
                    "PingFang SC",
                    "Hiragino Sans GB",
                    "Arial Unicode MS",
                    "Arial"
                },
                Mathf.Max(16, size));
            return uiFont;
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
