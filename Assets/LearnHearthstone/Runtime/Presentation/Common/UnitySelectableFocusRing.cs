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

        public Outline FocusOutline => EnsureFocusOutline();

        public void OnSelect(BaseEventData eventData)
        {
            EnsureFocusOutline().enabled = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (focusOutline != null)
            {
                focusOutline.enabled = false;
            }
        }

        private void Awake()
        {
            EnsureFocusOutline().enabled = false;
        }

        private Outline EnsureFocusOutline()
        {
            if (focusOutline != null)
            {
                return focusOutline;
            }

            var ringObject = new GameObject(
                name + "FocusRing",
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline),
                typeof(LayoutElement));
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.SetAsLastSibling();
            var rect = ringObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-2f, -2f);
            rect.offsetMax = new Vector2(2f, 2f);
            var image = ringObject.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            ringObject.GetComponent<LayoutElement>().ignoreLayout = true;
            focusOutline = ringObject.GetComponent<Outline>();
            focusOutline.effectColor = UnityTavernUiStyle.FocusRing;
            focusOutline.effectDistance = new Vector2(3f, -3f);
            focusOutline.useGraphicAlpha = false;
            focusOutline.enabled = false;
            return focusOutline;
        }
    }
}
