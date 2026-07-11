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
        OpponentBoard,
        HeroPower
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
        public static bool CanDrop(UnityTavernDragContext drag, UnityTavernDropTarget target, int targetIndex)
        {
            return TryBuildDropCommand(drag, target, targetIndex, out _);
        }

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
                command = new GameCommand(GameCommandType.BuyMinion, drag.Index, targetIndex);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.Discover && target == UnityTavernDropTarget.Hand)
            {
                command = new GameCommand(GameCommandType.ChooseDiscover, drag.Index);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.HeroPower && target == UnityTavernDropTarget.PlayerBoard)
            {
                if (targetIndex < 0)
                {
                    return false;
                }

                command = new GameCommand(GameCommandType.UseHeroPower, targetIndex, TargetZone.FriendlyBoard, heroPowerCardId: drag.Card.CardId);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.HeroPower && target == UnityTavernDropTarget.OpponentBoard)
            {
                if (targetIndex < 0)
                {
                    return false;
                }

                command = new GameCommand(GameCommandType.UseHeroPower, targetIndex, TargetZone.OpponentBoard, heroPowerCardId: drag.Card.CardId);
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

        public MinionInstance Card => card;
        public UnityTavernDragSource Source => source;

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
        private bool cueVisible;
        private bool cueAllowed;
        private bool raycastOnlyWhenAllowed;
        private bool activeOnlyWhenAllowed;
        private bool cueOnlyWhenAllowed;
        private bool resolveTargetIndexFromPointer;
        private int pointerIndexSlotCount;

        public bool IsHighlighted => highlighted;
        public bool IsDropCueVisible => cueVisible;
        public bool IsDropAllowed => cueAllowed;
        public UnityTavernDropTarget Target => target;
        public int TargetIndex => targetIndex;
        public Color HighlightColor => Highlight(target);

        public void Initialize(
            UnityTavernTrainerController controller,
            UnityTavernDropTarget dropTarget,
            int index,
            bool raycastOnlyWhenAllowed = false,
            bool activeOnlyWhenAllowed = false,
            bool cueOnlyWhenAllowed = false,
            bool resolveIndexFromPointer = false,
            int indexSlotCount = 0)
        {
            owner = controller;
            target = dropTarget;
            targetIndex = index;
            this.raycastOnlyWhenAllowed = raycastOnlyWhenAllowed;
            this.activeOnlyWhenAllowed = activeOnlyWhenAllowed;
            this.cueOnlyWhenAllowed = cueOnlyWhenAllowed;
            resolveTargetIndexFromPointer = resolveIndexFromPointer;
            pointerIndexSlotCount = indexSlotCount;
            image = GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = !raycastOnlyWhenAllowed;
                normalColor = image.color;
            }

            outline = UnityTavernUiStyle.EnsureComponent<Outline>(gameObject);
            outline.enabled = false;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
            cueVisible = false;
            cueAllowed = false;
            highlighted = false;
            ApplyVisuals();
        }

        public void OnDrop(PointerEventData eventData)
        {
            ClearDropCue();
            owner?.HandleDrop(target, ResolveTargetIndex(eventData));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            highlighted = true;
            ApplyVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            highlighted = false;
            ApplyVisuals();
        }

        public void SetDropCue(UnityTavernDragContext drag, bool commandAllowed = true)
        {
            var allowed = commandAllowed && UnityTavernDragController.CanDrop(drag, target, targetIndex);
            cueVisible = cueOnlyWhenAllowed ? allowed : drag != null;
            cueAllowed = allowed;

            if (image != null && raycastOnlyWhenAllowed)
            {
                image.raycastTarget = allowed;
            }

            if (activeOnlyWhenAllowed)
            {
                gameObject.SetActive(allowed);
            }

            ApplyVisuals();
        }

        public void ClearDropCue()
        {
            cueVisible = false;
            cueAllowed = false;
            highlighted = false;
            ApplyVisuals();

            if (image != null && raycastOnlyWhenAllowed)
            {
                image.raycastTarget = false;
            }

            if (activeOnlyWhenAllowed)
            {
                gameObject.SetActive(false);
            }
        }

        private int ResolveTargetIndex(PointerEventData eventData)
        {
            if (!resolveTargetIndexFromPointer || eventData == null || pointerIndexSlotCount <= 0)
            {
                return targetIndex;
            }

            var rect = transform as RectTransform;
            if (rect == null || rect.rect.width <= 0f)
            {
                return targetIndex;
            }

            var camera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, camera, out var localPoint))
            {
                return targetIndex;
            }

            var normalized = Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, localPoint.x);
            var index = Mathf.FloorToInt(Mathf.Clamp01(normalized) * pointerIndexSlotCount);
            return Mathf.Clamp(index, 0, pointerIndexSlotCount - 1);
        }

        private void ApplyVisuals()
        {
            if (image != null)
            {
                image.color = ResolveColor();
            }

            if (outline != null)
            {
                var showOutline = highlighted || cueVisible && cueAllowed;
                outline.enabled = showOutline;
                if (showOutline)
                {
                    var color = cueVisible && !cueAllowed ? UnityTavernUiStyle.Red : Highlight(target);
                    outline.effectColor = new Color(color.r, color.g, color.b, highlighted ? 0.95f : 0.72f);
                    outline.effectDistance = cueVisible ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
                }
            }
        }

        private Color ResolveColor()
        {
            if (!cueVisible)
            {
                return highlighted
                    ? Color.Lerp(normalColor, Highlight(target), 0.55f)
                    : normalColor;
            }

            if (cueAllowed)
            {
                return Color.Lerp(normalColor, Highlight(target), highlighted ? 0.72f : 0.38f);
            }

            var dimmed = Color.Lerp(normalColor, Color.black, 0.34f);
            dimmed.a = Mathf.Max(normalColor.a * 0.72f, 0.2f);
            return highlighted ? Color.Lerp(dimmed, UnityTavernUiStyle.Red, 0.28f) : dimmed;
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
