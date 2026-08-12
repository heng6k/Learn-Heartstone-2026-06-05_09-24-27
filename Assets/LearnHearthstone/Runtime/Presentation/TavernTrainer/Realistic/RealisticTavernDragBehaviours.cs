using LearnHearthstone.Domain.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.Realistic
{
    internal enum RealisticDragSource
    {
        Shop,
        Discover,
        Hand,
        PlayerBoard,
        OpponentBoard
    }

    internal enum RealisticDropTarget
    {
        Hand,
        PlayerBoard,
        TavernShop,
        OpponentBoard,
        SellZone
    }

    internal enum RealisticDrawerTab
    {
        Info,
        Opponent,
        Battle,
        Logs,
        Debug
    }

    internal sealed class RealisticCardDragBehaviour : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RealisticTavernTrainerView view;
        private MinionInstance card;
        private RealisticDragSource source;
        private int index;

        public void Initialize(RealisticTavernTrainerView owner, MinionInstance minion, RealisticDragSource dragSource, int cardIndex)
        {
            view = owner;
            card = minion;
            source = dragSource;
            index = cardIndex;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (view == null || card == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            view.BeginDrag(card, source, index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            view?.MoveDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            view?.EndDrag();
        }
    }

    internal sealed class RealisticDropTargetBehaviour : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private RealisticTavernTrainerView view;
        private RealisticDropTarget target;
        private int targetIndex;
        private Image image;
        private Color normalColor;

        public void Initialize(RealisticTavernTrainerView owner, RealisticDropTarget dropTarget, int index)
        {
            view = owner;
            target = dropTarget;
            targetIndex = index;
            image = GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                normalColor = image.color;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            Restore();
            view?.HandleDrop(target, targetIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (image == null)
            {
                return;
            }

            image.color = Color.Lerp(normalColor, Highlight(target), 0.62f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Restore();
        }

        private void Restore()
        {
            if (image != null)
            {
                image.color = normalColor;
            }
        }

        private static Color Highlight(RealisticDropTarget dropTarget)
        {
            switch (dropTarget)
            {
                case RealisticDropTarget.Hand:
                    return RealisticTavernTrainerView.ColorFromHex(0x2D6C7D);
                case RealisticDropTarget.PlayerBoard:
                    return RealisticTavernTrainerView.ColorFromHex(0x436B31);
                case RealisticDropTarget.TavernShop:
                    return RealisticTavernTrainerView.ColorFromHex(0xD9A63A);
                case RealisticDropTarget.OpponentBoard:
                    return RealisticTavernTrainerView.ColorFromHex(0x455D83);
                case RealisticDropTarget.SellZone:
                    return RealisticTavernTrainerView.ColorFromHex(0x8A2E2A);
                default:
                    return Color.white;
            }
        }
    }

    internal sealed class RealisticCardHoverMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private float hoverScale = 1.035f;
        private float targetScale = 1f;

        public void Initialize(float scale)
        {
            hoverScale = scale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = hoverScale;
            ApplyReducedMotionTarget();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = 1f;
            ApplyReducedMotionTarget();
        }

        private void Update()
        {
            transform.localScale = LearnHearthstone.Presentation.Common.UnityUiMotionSettings.ReduceMotion
                ? Vector3.one * targetScale
                : Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * 12f);
        }

        private void ApplyReducedMotionTarget()
        {
            if (LearnHearthstone.Presentation.Common.UnityUiMotionSettings.ReduceMotion)
            {
                transform.localScale = Vector3.one * targetScale;
            }
        }
    }
}
