using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.MainHub
{
    internal static class StrategyGuideUiTheme
    {
        private const string SpriteRoot = "UI/StrategyGuide/";

        public static readonly Color Background = UnityTavernUiStyle.ColorFromHex(0x0F172A);
        public static readonly Color Workspace = UnityTavernUiStyle.ColorFromHex(0x111F2C);
        public static readonly Color Surface = UnityTavernUiStyle.ColorFromHex(0x192134);
        public static readonly Color SurfaceSelected = UnityTavernUiStyle.ColorFromHex(0x203246);
        public static readonly Color SurfaceSoft = UnityTavernUiStyle.ColorFromHex(0x142536);
        public static readonly Color Felt = UnityTavernUiStyle.ColorFromHex(0x153C32);
        public static readonly Color Primary = UnityTavernUiStyle.ColorFromHex(0xD97706);
        public static readonly Color PrimaryHover = UnityTavernUiStyle.ColorFromHex(0xE68A0A);
        public static readonly Color Focus = UnityTavernUiStyle.ColorFromHex(0x38A9CF);
        public static readonly Color FocusSoft = UnityTavernUiStyle.ColorFromHex(0x8BD5EB);
        public static readonly Color Success = UnityTavernUiStyle.ColorFromHex(0x32B86B);
        public static readonly Color Text = UnityTavernUiStyle.ColorFromHex(0xF8FAFC);
        public static readonly Color WarmText = UnityTavernUiStyle.ColorFromHex(0xF6E7C4);
        public static readonly Color MutedText = UnityTavernUiStyle.ColorFromHex(0xA7B5C7);
        public static readonly Color Border = UnityTavernUiStyle.WithAlpha(Color.white, 0.10f);
        public static readonly Color BorderStrong = UnityTavernUiStyle.WithAlpha(Color.white, 0.18f);

        public static void ApplySurface(GameObject target, Color fallback, string spriteName = null, bool raycastTarget = false)
        {
            var image = UnityTavernUiStyle.EnsureComponent<Image>(target);
            var sprite = string.IsNullOrWhiteSpace(spriteName)
                ? null
                : Resources.Load<Sprite>(SpriteRoot + spriteName);
            image.sprite = sprite;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0f
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.color = sprite == null ? fallback : Color.white;
            image.raycastTarget = raycastTarget;
        }

        public static void Outline(GameObject target, Color color, bool selected = false)
        {
            UnityTavernUiStyle.ConfigureOutline(
                target,
                color,
                selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f));
        }

        public static void PrimaryButton(Button button)
        {
            ConfigureButton(button, Primary, PrimaryHover, "button_primary", true);
        }

        public static void SecondaryButton(Button button, bool selected = false)
        {
            ConfigureButton(
                button,
                selected ? SurfaceSelected : Surface,
                Color.Lerp(selected ? SurfaceSelected : Surface, Focus, 0.24f),
                "button_secondary",
                selected);
        }

        public static void QuietButton(Button button)
        {
            ConfigureButton(button, Workspace, SurfaceSelected, "button_quiet", false);
        }

        public static void SuccessButton(Button button)
        {
            ConfigureButton(button, Color.Lerp(Surface, Success, 0.30f), Color.Lerp(Surface, Success, 0.46f), "button_secondary", true);
        }

        private static void ConfigureButton(
            Button button,
            Color normal,
            Color highlighted,
            string spriteName,
            bool emphasized)
        {
            if (button == null)
            {
                return;
            }

            ApplySurface(button.gameObject, normal, spriteName, true);
            Outline(
                button.gameObject,
                UnityTavernUiStyle.WithAlpha(emphasized ? Focus : BorderStrong, emphasized ? 0.72f : 1f),
                emphasized);
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.selectedColor = highlighted;
            colors.pressedColor = Color.Lerp(normal, Color.black, 0.22f);
            colors.disabledColor = UnityTavernUiStyle.WithAlpha(SurfaceSoft, 0.52f);
            colors.fadeDuration = UnityUiMotionSettings.Duration(0.08f);
            button.colors = colors;
            foreach (var label in button.GetComponentsInChildren<Text>(true))
            {
                UiFactory.SetTextColor(label, button.interactable ? (emphasized ? WarmText : Text) : MutedText);
            }
        }
    }
}
