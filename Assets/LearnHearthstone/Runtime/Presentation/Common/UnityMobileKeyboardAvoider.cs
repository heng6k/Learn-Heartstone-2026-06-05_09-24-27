using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.Common
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InputField))]
    public sealed class UnityMobileKeyboardAvoider : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private InputField input;
        private RectTransform safeAreaRect;
        private Vector2 baseAnchorMin;
        private Vector2 baseAnchorMax;
        private bool captured;

        public static float CalculateKeyboardTopAnchor(int screenHeight, Rect keyboardArea, float currentMinimum)
        {
            if (screenHeight <= 0 || keyboardArea.height <= 0f)
            {
                return Mathf.Clamp01(currentMinimum);
            }

            return Mathf.Clamp01(Mathf.Max(currentMinimum, keyboardArea.yMax / screenHeight));
        }

        private void Awake()
        {
            input = GetComponent<InputField>();
            var safeArea = GetComponentInParent<UnitySafeAreaPanel>();
            safeAreaRect = safeArea == null ? null : safeArea.transform as RectTransform;
        }

        public void OnSelect(BaseEventData eventData)
        {
            CaptureBaseAnchors();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            Restore();
        }

        private void OnDisable()
        {
            Restore();
        }

        private void Update()
        {
            if (!UnityEngine.Application.isMobilePlatform || input == null || safeAreaRect == null || !input.isFocused)
            {
                return;
            }

            if (!TouchScreenKeyboard.visible || TouchScreenKeyboard.area.height <= 0f)
            {
                Restore();
                return;
            }

            CaptureBaseAnchors();
            var anchorMin = baseAnchorMin;
            anchorMin.y = Mathf.Min(
                baseAnchorMax.y - 0.1f,
                CalculateKeyboardTopAnchor(Screen.height, TouchScreenKeyboard.area, baseAnchorMin.y));
            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = baseAnchorMax;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
        }

        private void CaptureBaseAnchors()
        {
            if (captured || safeAreaRect == null)
            {
                return;
            }

            baseAnchorMin = safeAreaRect.anchorMin;
            baseAnchorMax = safeAreaRect.anchorMax;
            captured = true;
        }

        private void Restore()
        {
            if (!captured || safeAreaRect == null)
            {
                return;
            }

            safeAreaRect.anchorMin = baseAnchorMin;
            safeAreaRect.anchorMax = baseAnchorMax;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
            captured = false;
        }
    }
}
