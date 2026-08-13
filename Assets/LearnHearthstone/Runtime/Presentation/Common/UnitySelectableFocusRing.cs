using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.Common
{
    [DisallowMultipleComponent]
    public sealed class UnitySelectableFocusRing : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private Outline focusOutline;
        private GameObject focusRing;

        public Outline FocusOutline => EnsureFocusOutline();

        public void OnSelect(BaseEventData eventData)
        {
            EnsureFocusOutline().enabled = true;
            focusRing.SetActive(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (focusOutline != null)
            {
                focusOutline.enabled = false;
            }
            if (focusRing != null)
            {
                focusRing.SetActive(false);
            }
        }

        private void Awake()
        {
            EnsureFocusOutline().enabled = false;
            focusRing.SetActive(false);
        }

        private Outline EnsureFocusOutline()
        {
            if (focusOutline != null)
            {
                return focusOutline;
            }

            focusRing = new GameObject(
                name + "FocusRing",
                typeof(RectTransform),
                typeof(LayoutElement));
            focusRing.transform.SetParent(transform, false);
            focusRing.transform.SetAsLastSibling();
            var rect = focusRing.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-2f, -2f);
            rect.offsetMax = new Vector2(2f, 2f);
            focusRing.GetComponent<LayoutElement>().ignoreLayout = true;

            var topBorder = CreateBorder("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 3f));
            CreateBorder("Bottom", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 3f));
            CreateBorder("Left", Vector2.zero, new Vector2(0f, 1f), new Vector2(3f, 0f));
            CreateBorder("Right", new Vector2(1f, 0f), Vector2.one, new Vector2(3f, 0f));

            focusOutline = topBorder.AddComponent<Outline>();
            focusOutline.effectColor = UnityTavernUiStyle.FocusRing;
            focusOutline.effectDistance = new Vector2(2f, -2f);
            focusOutline.useGraphicAlpha = true;
            focusOutline.enabled = false;
            return focusOutline;
        }

        private GameObject CreateBorder(string suffix, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
        {
            var border = new GameObject(name + "FocusRing" + suffix, typeof(RectTransform), typeof(Image));
            border.transform.SetParent(focusRing.transform, false);
            var rect = border.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin + anchorMax) * 0.5f;
            rect.sizeDelta = sizeDelta;
            var image = border.GetComponent<Image>();
            image.color = UnityTavernUiStyle.FocusRing;
            image.raycastTarget = false;
            return border;
        }
    }
}
