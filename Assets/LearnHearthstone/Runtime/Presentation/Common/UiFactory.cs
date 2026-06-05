using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.Common
{
    public static class UiFactory
    {
        public static GameObject Panel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        public static Text Label(string name, Transform parent, string text, int size = 18, FontStyle style = FontStyle.Normal)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text));
            label.transform.SetParent(parent, false);
            var textComponent = label.GetComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = size;
            textComponent.fontStyle = style;
            textComponent.color = new Color(0.94f, 0.92f, 0.86f);
            textComponent.alignment = TextAnchor.MiddleLeft;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
            return textComponent;
        }

        public static Button Button(string name, Transform parent, string text, UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.36f, 0.48f);
            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            var label = Label(name + "Label", buttonObject.transform, text, 16, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform);
            return button;
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
            layout.childForceExpandWidth = true;
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

        public static void SetFlexible(GameObject target, float width, float height)
        {
            var element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
            element.flexibleWidth = width;
            element.flexibleHeight = height;
        }
    }
}
