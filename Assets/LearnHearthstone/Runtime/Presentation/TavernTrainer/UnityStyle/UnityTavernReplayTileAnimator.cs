using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public enum UnityTavernReplayTileMotion
    {
        None,
        Strike,
        Hit,
        Death,
        Summon,
        Trigger,
        Related
    }

    public sealed class UnityTavernReplayTileAnimator : MonoBehaviour
    {
        private const float Duration = 0.58f;

        private RectTransform rect;
        private Image image;
        private CanvasGroup canvasGroup;
        private Image flashImage;
        private Color baseColor;
        private float startTime;
        private float direction;

        public UnityTavernReplayTileMotion Motion { get; private set; }
        public float Direction { get { return direction; } }
        public bool HasMotion { get { return Motion != UnityTavernReplayTileMotion.None; } }

        public void Configure(UnityTavernReplayTileMotion motion, Color tileColor, float lungeDirection)
        {
            rect = UnityTavernUiStyle.EnsureComponent<RectTransform>(gameObject);
            image = UnityTavernUiStyle.EnsureComponent<Image>(gameObject);
            canvasGroup = UnityTavernUiStyle.EnsureComponent<CanvasGroup>(gameObject);
            baseColor = tileColor;
            Motion = motion;
            direction = Mathf.Approximately(lungeDirection, 0f) ? 1f : Mathf.Sign(lungeDirection);
            startTime = Time.unscaledTime;

            ConfigureOutline(motion);
            ConfigureFlash(motion);
            ApplyPreview(0f);
        }

        public void ApplyPreview(float phase)
        {
            phase = Mathf.Clamp01(phase);
            var pulse = Mathf.Sin(phase * Mathf.PI);
            var scale = 1f;
            var rotation = 0f;
            var alpha = 1f;
            var tint = baseColor;
            var flashAlpha = 0f;

            switch (Motion)
            {
                case UnityTavernReplayTileMotion.Strike:
                    scale = 1f + 0.11f * pulse;
                    rotation = -direction * 5.5f * pulse;
                    tint = Color.Lerp(baseColor, UnityTavernUiStyle.Gold, 0.52f * pulse);
                    flashAlpha = 0.32f * pulse;
                    break;
                case UnityTavernReplayTileMotion.Hit:
                    scale = 1f + 0.05f * pulse;
                    rotation = direction * 4.5f * Mathf.Sin(phase * Mathf.PI * 6f) * (1f - phase);
                    tint = Color.Lerp(baseColor, UnityTavernUiStyle.Red, 0.62f * pulse);
                    flashAlpha = 0.44f * pulse;
                    break;
                case UnityTavernReplayTileMotion.Death:
                    scale = Mathf.Lerp(1f, 0.82f, phase);
                    rotation = direction * Mathf.Lerp(0f, 7f, phase);
                    alpha = Mathf.Lerp(1f, 0.38f, phase);
                    tint = Color.Lerp(baseColor, new Color(0.16f, 0.05f, 0.05f, 1f), phase);
                    flashAlpha = Mathf.Lerp(0.36f, 0.08f, phase);
                    break;
                case UnityTavernReplayTileMotion.Summon:
                    scale = Mathf.Lerp(0.82f, 1f, phase) + 0.12f * pulse;
                    tint = Color.Lerp(baseColor, UnityTavernUiStyle.Blue, 0.45f * pulse);
                    flashAlpha = 0.35f * pulse;
                    break;
                case UnityTavernReplayTileMotion.Trigger:
                    scale = 1f + 0.08f * pulse;
                    tint = Color.Lerp(baseColor, UnityTavernUiStyle.Gold, 0.46f * pulse);
                    flashAlpha = 0.28f * pulse;
                    break;
                case UnityTavernReplayTileMotion.Related:
                    scale = 1f + 0.035f * pulse;
                    tint = Color.Lerp(baseColor, UnityTavernUiStyle.TableLit, 0.35f * pulse);
                    flashAlpha = 0.18f * pulse;
                    break;
            }

            transform.localScale = new Vector3(scale, scale, 1f);
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            image.color = tint;
            canvasGroup.alpha = alpha;

            if (flashImage != null)
            {
                var color = flashImage.color;
                color.a = flashAlpha;
                flashImage.color = color;
            }
        }

        private void Update()
        {
            if (Motion == UnityTavernReplayTileMotion.None)
            {
                return;
            }

            ApplyPreview((Time.unscaledTime - startTime) / Duration);
        }

        private void ConfigureOutline(UnityTavernReplayTileMotion motion)
        {
            var outline = UnityTavernUiStyle.EnsureComponent<Outline>(gameObject);
            outline.enabled = motion != UnityTavernReplayTileMotion.None;
            outline.useGraphicAlpha = false;
            outline.effectDistance = motion == UnityTavernReplayTileMotion.Hit
                ? new Vector2(3f, -3f)
                : new Vector2(2f, -2f);
            outline.effectColor = MotionColor(motion);
        }

        private void ConfigureFlash(UnityTavernReplayTileMotion motion)
        {
            var flash = transform.Find("UnityReplayMotionFlash");
            var flashObject = flash == null
                ? new GameObject("UnityReplayMotionFlash", typeof(RectTransform), typeof(Image), typeof(LayoutElement))
                : flash.gameObject;

            flashObject.transform.SetParent(transform, false);
            flashObject.transform.SetAsFirstSibling();
            var layout = UnityTavernUiStyle.EnsureComponent<LayoutElement>(flashObject);
            layout.ignoreLayout = true;

            var flashRect = flashObject.GetComponent<RectTransform>();
            UnityTavernUiStyle.Stretch(flashRect);

            flashImage = flashObject.GetComponent<Image>();
            flashImage.raycastTarget = false;
            var color = MotionColor(motion);
            color.a = 0f;
            flashImage.color = color;
        }

        private static Color MotionColor(UnityTavernReplayTileMotion motion)
        {
            switch (motion)
            {
                case UnityTavernReplayTileMotion.Strike:
                case UnityTavernReplayTileMotion.Trigger:
                    return new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.82f);
                case UnityTavernReplayTileMotion.Hit:
                case UnityTavernReplayTileMotion.Death:
                    return new Color(UnityTavernUiStyle.Red.r, UnityTavernUiStyle.Red.g, UnityTavernUiStyle.Red.b, 0.88f);
                case UnityTavernReplayTileMotion.Summon:
                    return new Color(UnityTavernUiStyle.Blue.r, UnityTavernUiStyle.Blue.g, UnityTavernUiStyle.Blue.b, 0.76f);
                case UnityTavernReplayTileMotion.Related:
                    return new Color(UnityTavernUiStyle.TableLit.r, UnityTavernUiStyle.TableLit.g, UnityTavernUiStyle.TableLit.b, 0.58f);
                default:
                    return Color.clear;
            }
        }
    }
}
