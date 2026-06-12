using LearnHearthstone.Application.Commands;
using LearnHearthstone.Domain.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public enum UnityTavernDragSource
    {
        Shop,
        Discover,
        Hand,
        PlayerBoard,
        OpponentBoard
    }

    public enum UnityTavernDropTarget
    {
        Hand,
        PlayerBoard,
        OpponentBoard,
        SellZone
    }

    public sealed class UnityTavernDragContext
    {
        public UnityTavernDragContext(MinionInstance card, UnityTavernDragSource source, int index)
        {
            Card = card;
            Source = source;
            Index = index;
        }

        public MinionInstance Card { get; }
        public UnityTavernDragSource Source { get; }
        public int Index { get; }
    }

    public static class UnityTavernDragController
    {
        public static bool TryBuildDropCommand(
            UnityTavernDragContext drag,
            UnityTavernDropTarget target,
            int targetIndex,
            out GameCommand command)
        {
            command = null;
            if (drag == null || drag.Card == null)
            {
                return false;
            }

            if (drag.Source == UnityTavernDragSource.Shop && target == UnityTavernDropTarget.Hand)
            {
                command = new GameCommand(GameCommandType.BuyMinion, drag.Index);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.Discover && target == UnityTavernDropTarget.Hand)
            {
                command = new GameCommand(GameCommandType.ChooseDiscover, drag.Index);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.Hand && target == UnityTavernDropTarget.PlayerBoard)
            {
                command = new GameCommand(GameCommandType.PlayMinion, drag.Index, targetIndex);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.PlayerBoard && target == UnityTavernDropTarget.PlayerBoard)
            {
                command = new GameCommand(GameCommandType.MoveBoardMinion, drag.Card.InstanceId, targetIndex);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.PlayerBoard && target == UnityTavernDropTarget.SellZone)
            {
                command = new GameCommand(GameCommandType.SellMinion, drag.Card.InstanceId);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.OpponentBoard && target == UnityTavernDropTarget.OpponentBoard)
            {
                command = new GameCommand(GameCommandType.MoveOpponentMinion, drag.Card.InstanceId, targetIndex);
                return true;
            }

            return false;
        }
    }

    public sealed class UnityTavernCardDragBehaviour : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private UnityTavernTrainerController owner;
        private MinionInstance card;
        private UnityTavernDragSource source;
        private int index;

        public void Initialize(UnityTavernTrainerController controller, MinionInstance value, UnityTavernDragSource dragSource, int cardIndex)
        {
            owner = controller;
            card = value;
            source = dragSource;
            index = cardIndex;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner == null || card == null || eventData == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            owner.BeginDrag(card, source, index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner?.MoveDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.EndDrag();
        }
    }

    public sealed class UnityTavernDropTargetBehaviour : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private UnityTavernTrainerController owner;
        private UnityTavernDropTarget target;
        private int targetIndex;
        private Image image;
        private Outline outline;
        private Color normalColor;
        private bool highlighted;

        public bool IsHighlighted => highlighted;
        public Color HighlightColor => Highlight(target);

        public void Initialize(UnityTavernTrainerController controller, UnityTavernDropTarget dropTarget, int index)
        {
            owner = controller;
            target = dropTarget;
            targetIndex = index;
            image = GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                normalColor = image.color;
            }

            outline = UnityTavernUiStyle.EnsureComponent<Outline>(gameObject);
            outline.enabled = false;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
        }

        public void OnDrop(PointerEventData eventData)
        {
            Restore();
            owner?.HandleDrop(target, targetIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (image == null)
            {
                return;
            }

            highlighted = true;
            var color = Highlight(target);
            image.color = Color.Lerp(normalColor, color, 0.55f);
            if (outline != null)
            {
                outline.enabled = true;
                outline.effectColor = new Color(color.r, color.g, color.b, 0.9f);
            }
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

            highlighted = false;
            if (outline != null)
            {
                outline.enabled = false;
            }
        }

        private static Color Highlight(UnityTavernDropTarget dropTarget)
        {
            switch (dropTarget)
            {
                case UnityTavernDropTarget.Hand:
                    return UnityTavernUiStyle.Blue;
                case UnityTavernDropTarget.PlayerBoard:
                    return UnityTavernUiStyle.Green;
                case UnityTavernDropTarget.OpponentBoard:
                    return UnityTavernUiStyle.ColorFromHex(0x455D83);
                case UnityTavernDropTarget.SellZone:
                    return UnityTavernUiStyle.Red;
                default:
                    return Color.white;
            }
        }
    }
}
