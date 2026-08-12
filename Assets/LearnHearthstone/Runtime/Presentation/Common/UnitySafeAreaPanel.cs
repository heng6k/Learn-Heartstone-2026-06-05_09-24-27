using UnityEngine;

namespace LearnHearthstone.Presentation.Common
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UnitySafeAreaPanel : MonoBehaviour
    {
        public const float MinimumPhysicalMargin = 16f;
        public const float TitleSafeFraction = 0.9f;

        private bool includeTitleSafe = true;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private Rect lastSafeArea;

        public static Transform Create(
            Transform parent,
            bool includeTitleSafe = true,
            string name = "LearnHearthstoneSafeArea")
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(UnitySafeAreaPanel));
            panelObject.transform.SetParent(parent, false);
            var panel = panelObject.GetComponent<UnitySafeAreaPanel>();
            panel.includeTitleSafe = includeTitleSafe;
            panel.Refresh(Screen.width, Screen.height, Screen.safeArea);
            return panelObject.transform;
        }

        public static Rect CalculateSafeRect(
            int screenWidth,
            int screenHeight,
            Rect actualSafeArea,
            bool includeTitleSafe)
        {
            var width = Mathf.Max(1f, screenWidth);
            var height = Mathf.Max(1f, screenHeight);
            if (actualSafeArea.width <= 0f || actualSafeArea.height <= 0f)
            {
                actualSafeArea = new Rect(0f, 0f, width, height);
            }

            var titleMarginX = includeTitleSafe ? width * (1f - TitleSafeFraction) * 0.5f : 0f;
            var titleMarginY = includeTitleSafe ? height * (1f - TitleSafeFraction) * 0.5f : 0f;
            var left = Mathf.Max(MinimumPhysicalMargin, titleMarginX, Mathf.Clamp(actualSafeArea.xMin, 0f, width));
            var right = Mathf.Max(MinimumPhysicalMargin, titleMarginX, Mathf.Clamp(width - actualSafeArea.xMax, 0f, width));
            var bottom = Mathf.Max(MinimumPhysicalMargin, titleMarginY, Mathf.Clamp(actualSafeArea.yMin, 0f, height));
            var top = Mathf.Max(MinimumPhysicalMargin, titleMarginY, Mathf.Clamp(height - actualSafeArea.yMax, 0f, height));

            left = Mathf.Min(left, width * 0.5f);
            right = Mathf.Min(right, width - left);
            bottom = Mathf.Min(bottom, height * 0.5f);
            top = Mathf.Min(top, height - bottom);
            return Rect.MinMaxRect(left, bottom, width - right, height - top);
        }

        public void Configure(bool shouldIncludeTitleSafe)
        {
            includeTitleSafe = shouldIncludeTitleSafe;
            lastScreenWidth = -1;
            Refresh(Screen.width, Screen.height, Screen.safeArea);
        }

        public void Refresh(int screenWidth, int screenHeight, Rect safeArea)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            var safeRect = CalculateSafeRect(screenWidth, screenHeight, safeArea, includeTitleSafe);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safeRect.xMin / screenWidth, safeRect.yMin / screenHeight);
            rect.anchorMax = new Vector2(safeRect.xMax / screenWidth, safeRect.yMax / screenHeight);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;
            lastSafeArea = safeArea;
        }

        private void Update()
        {
            if (lastScreenWidth == Screen.width &&
                lastScreenHeight == Screen.height &&
                lastSafeArea == Screen.safeArea)
            {
                return;
            }

            Refresh(Screen.width, Screen.height, Screen.safeArea);
        }
    }
}
