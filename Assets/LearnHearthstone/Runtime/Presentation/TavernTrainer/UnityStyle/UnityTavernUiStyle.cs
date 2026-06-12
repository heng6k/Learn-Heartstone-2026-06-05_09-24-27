using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public static class UnityTavernUiStyle
    {
        public static readonly Color BackWall = ColorFromHex(0x151915);
        public static readonly Color TableDark = ColorFromHex(0x2A2118);
        public static readonly Color TableLit = ColorFromHex(0x5A3B21);
        public static readonly Color Panel = ColorFromHex(0x202B2C);
        public static readonly Color PanelRaised = ColorFromHex(0x2B3937);
        public static readonly Color PanelQuiet = ColorFromHex(0x182022);
        public static readonly Color Gold = ColorFromHex(0xD9A63A);
        public static readonly Color Red = ColorFromHex(0x8D3A36);
        public static readonly Color Blue = ColorFromHex(0x2E6788);
        public static readonly Color Green = ColorFromHex(0x496D43);
        public static readonly Color Text = ColorFromHex(0xF4E8C9);
        public static readonly Color MutedText = ColorFromHex(0xB9AB8A);

        public static Color ColorFromHex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f);
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetFixedSize(GameObject target, float width, float height)
        {
            var element = EnsureComponent<LayoutElement>(target);
            element.minWidth = width;
            element.preferredWidth = width;
            element.minHeight = height;
            element.preferredHeight = height;
        }

        public static void SetPreferredHeight(GameObject target, float height)
        {
            var element = EnsureComponent<LayoutElement>(target);
            element.preferredHeight = height;
        }

        public static void SetFlexible(GameObject target, float width, float height)
        {
            var element = EnsureComponent<LayoutElement>(target);
            element.flexibleWidth = width;
            element.flexibleHeight = height;
        }

        public static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        public static void TintSelectable(Button button, Color normal, Color highlighted, Color pressed)
        {
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = highlighted;
            colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.42f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
        }
    }
}
