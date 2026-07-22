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
        TavernShop,
        OpponentBoard,
        SellZone
    }

    public enum UnityTavernTargetingFailureReason
    {
        None,
        MissingSource,
        MissingTarget,
        UnsupportedTarget,
        InvalidTarget
    }

    public readonly struct UnityTavernTargetingEvaluation
    {
        public UnityTavernTargetingEvaluation(bool allowed, UnityTavernTargetingFailureReason reason)
        {
            Allowed = allowed;
            Reason = reason;
        }

        public bool Allowed { get; }
        public UnityTavernTargetingFailureReason Reason { get; }
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
        private const string AkazamzarakHeroPowerCardId = "TB_BaconShop_HP_020";

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
            return TryBuildDropCommand(drag, target, targetIndex, out command, out _);
        }

        public static bool TryBuildDropCommand(
            UnityTavernDragContext drag,
            UnityTavernDropTarget target,
            int targetIndex,
            out GameCommand command,
            out UnityTavernTargetingFailureReason failureReason)
        {
            command = null;
            failureReason = UnityTavernTargetingFailureReason.None;
            if (drag == null || drag.Card == null)
            {
                failureReason = UnityTavernTargetingFailureReason.MissingSource;
                return false;
            }

            if (drag.Source == UnityTavernDragSource.HeroPower && IsDirectUseHeroPower(drag.Card))
            {
                failureReason = UnityTavernTargetingFailureReason.UnsupportedTarget;
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
                    failureReason = UnityTavernTargetingFailureReason.MissingTarget;
                    return false;
                }

                if (TargetsTavernOnly(drag.Card))
                {
                    failureReason = UnityTavernTargetingFailureReason.UnsupportedTarget;
                    return false;
                }

                command = new GameCommand(GameCommandType.UseHeroPower, targetIndex, TargetZone.FriendlyBoard, heroPowerCardId: drag.Card.CardId);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.HeroPower && target == UnityTavernDropTarget.OpponentBoard)
            {
                if (targetIndex < 0)
                {
                    failureReason = UnityTavernTargetingFailureReason.MissingTarget;
                    return false;
                }

                if (RequiresTwoTargets(drag.Card))
                {
                    failureReason = UnityTavernTargetingFailureReason.UnsupportedTarget;
                    return false;
                }

                if (TargetsTavernOnly(drag.Card))
                {
                    failureReason = UnityTavernTargetingFailureReason.UnsupportedTarget;
                    return false;
                }

                command = new GameCommand(GameCommandType.UseHeroPower, targetIndex, TargetZone.OpponentBoard, heroPowerCardId: drag.Card.CardId);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.HeroPower &&
                target == UnityTavernDropTarget.TavernShop &&
                CanHeroPowerTargetTavern(drag.Card))
            {
                if (targetIndex < 0)
                {
                    failureReason = UnityTavernTargetingFailureReason.MissingTarget;
                    return false;
                }

                command = new GameCommand(GameCommandType.UseHeroPower, targetIndex, TargetZone.TavernShop, heroPowerCardId: drag.Card.CardId);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.Hand && target == UnityTavernDropTarget.PlayerBoard)
            {
                var targetedSpell = IsTargetedSpell(drag.Card);
                if (targetedSpell && targetIndex < 0)
                {
                    failureReason = UnityTavernTargetingFailureReason.MissingTarget;
                    return false;
                }

                command = targetedSpell
                    ? new GameCommand(
                        GameCommandType.PlayMinion,
                        drag.Index,
                        targetIndex,
                        TargetZone.FriendlyBoard,
                        -1,
                        TargetZone.Unspecified)
                    : new GameCommand(GameCommandType.PlayMinion, drag.Index, targetIndex);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.Hand &&
                target == UnityTavernDropTarget.TavernShop &&
                targetIndex >= 0 &&
                IsTargetedSpell(drag.Card))
            {
                command = new GameCommand(
                    GameCommandType.PlayMinion,
                    drag.Index,
                    targetIndex,
                    TargetZone.TavernShop,
                    -1,
                    TargetZone.Unspecified);
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

            failureReason = targetIndex < 0
                ? UnityTavernTargetingFailureReason.MissingTarget
                : UnityTavernTargetingFailureReason.UnsupportedTarget;
            return false;
        }

        public static UnityTavernTargetingEvaluation Evaluate(
            UnityTavernDragContext drag,
            UnityTavernDropTarget target,
            int targetIndex)
        {
            var allowed = TryBuildDropCommand(drag, target, targetIndex, out _, out var reason);
            return new UnityTavernTargetingEvaluation(allowed, reason);
        }

        private static bool IsTargetedSpell(MinionInstance card)
        {
            return card != null &&
                   (card.CardKind == CardKind.TavernSpell || card.CardKind == CardKind.Spell) &&
                   card.Tags != null &&
                   card.Tags.Exists(tag => string.Equals(tag, "targeted_spell", System.StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsBloodGemSpell(MinionInstance card)
        {
            return card != null &&
                   ((card.Keywords != null && card.Keywords.Contains(Keyword.BloodGem)) ||
                    (card.Tags != null && card.Tags.Exists(tag => string.Equals(tag, "blood_gem", System.StringComparison.OrdinalIgnoreCase))));
        }

        private static bool CanHeroPowerTargetTavern(MinionInstance card)
        {
            if (card == null || card.CardKind != CardKind.HeroPower)
            {
                return false;
            }

            if (string.Equals(card.CardId, "BG20_HERO_201p", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var text = card.Text ?? string.Empty;
            var chooses = text.IndexOf("Choose", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                          text.IndexOf("选择", System.StringComparison.OrdinalIgnoreCase) >= 0;
            var tavern = text.IndexOf("Tavern", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         text.IndexOf("酒馆", System.StringComparison.OrdinalIgnoreCase) >= 0;
            return chooses && tavern;
        }

        public static bool RequiresTwoTargets(MinionInstance card)
        {
            if (card == null || card.CardKind != CardKind.HeroPower)
            {
                return false;
            }

            if (string.Equals(card.CardId, "BG20_HERO_201p", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var text = card.Text ?? string.Empty;
            return text.IndexOf("Choose 2 minions", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("选择2个随从", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("选择两个随从", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool TargetsTavernOnly(MinionInstance card)
        {
            return CanHeroPowerTargetTavern(card) && !RequiresTwoTargets(card);
        }

        public static bool IsDirectUseHeroPower(MinionInstance card)
        {
            return card != null &&
                   card.CardKind == CardKind.HeroPower &&
                   string.Equals(card.CardId, AkazamzarakHeroPowerCardId, System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool RequiresBattlecryTarget(MinionInstance card)
        {
            return card != null &&
                   card.CardKind == CardKind.Minion &&
                   (string.Equals(card.CardId, "BG29_503", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(card.CardId, "BG28_303", System.StringComparison.OrdinalIgnoreCase));
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
                case UnityTavernDropTarget.TavernShop:
                    return UnityTavernUiStyle.Gold;
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
