using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public static class UnityTavernUiStyle
    {
        public const float SpacingXs = 4f;
        public const float SpacingSm = 8f;
        public const float SpacingMd = 12f;
        public const float SpacingLg = 18f;
        public const float TouchHeight = 44f;
        public const float CompactTouchHeight = 52f;

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
            element.flexibleWidth = 0f;
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;

            var rect = target.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(width, height);
            }
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

        public static Image ConfigureSurface(GameObject target, Color color, bool raycastTarget = false)
        {
            var image = EnsureComponent<Image>(target);
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        public static Outline ConfigureOutline(GameObject target, Color color, Vector2 distance)
        {
            var outline = EnsureComponent<Outline>(target);
            outline.enabled = true;
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = false;
            return outline;
        }

        public static string ArtFallbackText(string displayName, string fallback)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return fallback ?? string.Empty;
            }

            var trimmed = displayName.Trim();
            var firstVisible = trimmed.FirstOrDefault(char.IsLetterOrDigit);
            if (firstVisible >= '\u3400' && firstVisible <= '\u9fff')
            {
                return new string(trimmed.Where(char.IsLetterOrDigit).Take(2).ToArray());
            }

            var words = trimmed.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                var initials = words
                    .Select(word => word.FirstOrDefault(char.IsLetterOrDigit))
                    .Where(character => character != default(char))
                    .Take(2)
                    .ToArray();
                if (initials.Length > 1)
                {
                    return new string(initials).ToUpperInvariant();
                }
            }

            return new string(trimmed.Where(char.IsLetterOrDigit).Take(2).ToArray()).ToUpperInvariant();
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
