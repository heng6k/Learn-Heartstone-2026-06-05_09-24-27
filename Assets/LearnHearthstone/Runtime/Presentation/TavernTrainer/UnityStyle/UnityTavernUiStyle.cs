using System;
using System.Linq;
using LearnHearthstone.Presentation.Common;
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
        public const float SpacingXl = 24f;
        public const float Spacing2Xl = 32f;
        public const float TouchHeight = 48f;
        public const float CompactTouchHeight = 52f;

        public static readonly Color BackWall = ColorFromHex(0x111512);
        public static readonly Color TableDark = ColorFromHex(0x332319);
        public static readonly Color TableLit = ColorFromHex(0x684326);
        public static readonly Color SurfaceDark = ColorFromHex(0x192322);
        public static readonly Color SurfaceRaised = ColorFromHex(0x263534);
        public static readonly Color Parchment = ColorFromHex(0xD8C499);
        public static readonly Color ParchmentDark = ColorFromHex(0xA98B60);
        public static readonly Color Brass = ColorFromHex(0xC6923E);
        public static readonly Color Gold = ColorFromHex(0xE0B858);
        public static readonly Color ArcaneBlue = ColorFromHex(0x4B94C2);
        public static readonly Color SuccessGreen = ColorFromHex(0x5D8653);
        public static readonly Color CombatRed = ColorFromHex(0x9F4438);
        public static readonly Color DangerRed = ColorFromHex(0xB34E47);
        public static readonly Color TextLight = ColorFromHex(0xF5E8CD);
        public static readonly Color TextDark = ColorFromHex(0x2B2118);
        public static readonly Color TextMuted = ColorFromHex(0xBCAE8C);
        public static readonly Color FocusRing = ColorFromHex(0x79CCFF);
        public static readonly Color Disabled = ColorFromHex(0x666A64);

        // Compatibility aliases keep the existing UI on one shared token source while windows migrate by slice.
        public static readonly Color Panel = SurfaceDark;
        public static readonly Color PanelRaised = SurfaceRaised;
        public static readonly Color PanelQuiet = ColorFromHex(0x141C1C);
        public static readonly Color Red = CombatRed;
        public static readonly Color Blue = ArcaneBlue;
        public static readonly Color Green = SuccessGreen;
        public static readonly Color Text = TextLight;
        public static readonly Color MutedText = TextMuted;

        public static Color ColorFromHex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static void ConfigureLabel(Text label, Color color, int minimumSize = 14)
        {
            if (label == null)
            {
                return;
            }

            UiFactory.EnsureFont(label);
            label.fontSize = Mathf.Max(minimumSize, label.fontSize);
            label.color = color;
        }

        public static void ConfigureButton(Button button, Color accent, bool emphasized = false, bool selected = false)
        {
            if (button == null)
            {
                return;
            }

            var normal = emphasized
                ? Color.Lerp(SurfaceRaised, accent, 0.28f)
                : Color.Lerp(SurfaceDark, SurfaceRaised, 0.72f);
            ConfigureSurface(button.gameObject, normal, true);
            ConfigureOutline(
                button.gameObject,
                WithAlpha(selected ? FocusRing : accent, selected ? 0.92f : 0.58f),
                selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f));
            TintSelectable(
                button,
                normal,
                Color.Lerp(normal, accent, 0.28f),
                Color.Lerp(normal, Color.black, 0.18f));

            foreach (var label in button.GetComponentsInChildren<Text>(true))
            {
                ConfigureLabel(label, button.interactable ? TextLight : TextMuted);
            }
        }

        public static void ConfigureInputField(InputField input, Color accent)
        {
            if (input == null)
            {
                return;
            }

            ConfigureSurface(input.gameObject, Color.Lerp(SurfaceDark, SurfaceRaised, 0.42f), true);
            ConfigureOutline(input.gameObject, WithAlpha(accent, 0.46f), new Vector2(1f, -1f));
            // InputField inherits Unity's bright default selected tint. It obscures text on
            // mobile; focus is already represented by UnitySelectableFocusRing.
            input.transition = Selectable.Transition.None;
            ConfigureLabel(input.textComponent, TextLight);
            ConfigureLabel(input.placeholder as Text, TextMuted);
        }

        public static void AddStarLanternRail(Transform parent, string name, Color accent)
        {
            if (parent == null || parent.Find(name + "Rail") != null)
            {
                return;
            }

            var rail = new GameObject(name + "Rail", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rail.transform.SetParent(parent, false);
            rail.GetComponent<LayoutElement>().ignoreLayout = true;
            var railRect = rail.GetComponent<RectTransform>();
            railRect.anchorMin = Vector2.zero;
            railRect.anchorMax = new Vector2(1f, 0f);
            railRect.pivot = new Vector2(0.5f, 0f);
            railRect.sizeDelta = new Vector2(0f, 2f);
            railRect.anchoredPosition = Vector2.zero;
            var railImage = rail.GetComponent<Image>();
            railImage.color = WithAlpha(Brass, 0.64f);
            railImage.raycastTarget = false;

            var facet = new GameObject(name + "Facet", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            facet.transform.SetParent(parent, false);
            facet.GetComponent<LayoutElement>().ignoreLayout = true;
            var facetRect = facet.GetComponent<RectTransform>();
            facetRect.anchorMin = new Vector2(0.5f, 0f);
            facetRect.anchorMax = new Vector2(0.5f, 0f);
            facetRect.pivot = new Vector2(0.5f, 0.5f);
            facetRect.sizeDelta = new Vector2(10f, 10f);
            facetRect.anchoredPosition = new Vector2(0f, 1f);
            facetRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var facetImage = facet.GetComponent<Image>();
            facetImage.color = accent;
            facetImage.raycastTarget = false;
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
            var selectable = target.GetComponent<Selectable>() != null;
            var minimumTouchSize = selectable
                ? UnityTavernLayoutContext.Current().CanvasUnitsForPhysicalPixels(TouchHeight)
                : 0f;
            var resolvedWidth = selectable && width > 0f ? Mathf.Max(minimumTouchSize, width) : width;
            var resolvedHeight = selectable ? Mathf.Max(minimumTouchSize, height) : height;
            var element = EnsureComponent<LayoutElement>(target);
            element.minWidth = Mathf.Max(element.minWidth, resolvedWidth);
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
            element.minHeight = Mathf.Max(element.minHeight, resolvedHeight);
            element.preferredHeight = selectable ? Mathf.Max(TouchHeight, height) : height;
            element.flexibleHeight = 0f;

            var rect = target.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(resolvedWidth, resolvedHeight);
            }
        }

        public static void SetPreferredHeight(GameObject target, float height)
        {
            var element = EnsureComponent<LayoutElement>(target);
            var selectable = target.GetComponent<Selectable>();
            var minimumTouchSize = selectable != null
                ? UnityTavernLayoutContext.Current().CanvasUnitsForPhysicalPixels(TouchHeight)
                : 0f;
            var resolvedHeight = selectable != null ? Mathf.Max(minimumTouchSize, height) : height;
            if (selectable != null)
            {
                element.minHeight = Mathf.Max(element.minHeight, resolvedHeight);
            }

            element.preferredHeight = selectable != null ? Mathf.Max(TouchHeight, height) : height;
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
            colors.fadeDuration = UnityUiMotionSettings.Duration(0.08f);
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
        }
    }
}
