using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.Common
{
    public static class UiFactory
    {
        private static Font uiFont;

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
            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            var label = Label(name + "Label", buttonObject.transform, text, 16, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform);
            return button;
        }

        public static void SetTextColor(Text text, Color color)
        {
            text.color = color;
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
            if (uiFont != null)
            {
                return uiFont;
            }

            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial" },
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
