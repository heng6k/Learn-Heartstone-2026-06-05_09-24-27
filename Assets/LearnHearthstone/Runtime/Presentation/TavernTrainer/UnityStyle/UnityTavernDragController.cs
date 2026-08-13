using LearnHearthstone.Application.Commands;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public enum UnityTavernTargetingEndpointState
    {
        Neutral,
        Valid,
        Invalid
    }

    public enum UnityTavernDragSource
    {
        Shop,
        Discover,
        Hand,
        PlayerBoard,
        OpponentBoard,
        HeroPower,
        GuideShapingSpell
    }

    public enum UnityTavernDropTarget
    {
        Hand,
        PurchaseZone,
        DiscoverZone,
        PlayerBoard,
        PlayerBoardInsert,
        TavernShop,
        TavernShopInsert,
        OpponentBoard,
        SellZone,
        CastZone
    }

    public enum UnityTavernTargetingFailureReason
    {
        None,
        MissingSource,
        MissingTarget,
        UnsupportedTarget,
        InvalidTarget
    }

    public enum UnityTavernDropFeedbackKind
    {
        None,
        Generic,
        Purchase,
        Place,
        Reorder,
        Sell,
        Cast,
        Magnetize,
        Target
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
        public UnityTavernDragContext(
            MinionInstance card,
            UnityTavernDragSource source,
            int index,
            bool requiresPlayerTarget = false)
        {
            Card = card;
            Source = source;
            Index = index;
            RequiresPlayerTarget = requiresPlayerTarget;
        }

        public MinionInstance Card { get; }
        public UnityTavernDragSource Source { get; }
        public int Index { get; }
        public bool RequiresPlayerTarget { get; }
    }

    public static class UnityTavernDragController
    {
        private const string AkazamzarakHeroPowerCardId = "TB_BaconShop_HP_020";
        private const string CaptainSandersCardId = "BG25_034";

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

            if (drag.Source == UnityTavernDragSource.Shop &&
                (target == UnityTavernDropTarget.Hand || target == UnityTavernDropTarget.PurchaseZone))
            {
                command = new GameCommand(GameCommandType.BuyMinion, drag.Index);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.Shop && target == UnityTavernDropTarget.TavernShopInsert)
            {
                if (targetIndex < 0)
                {
                    failureReason = UnityTavernTargetingFailureReason.MissingTarget;
                    return false;
                }

                var resolvedInsertIndex = targetIndex > drag.Index ? targetIndex - 1 : targetIndex;
                command = new GameCommand(GameCommandType.MoveShopCard, drag.Card.InstanceId, resolvedInsertIndex);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.Discover &&
                (target == UnityTavernDropTarget.Hand || target == UnityTavernDropTarget.DiscoverZone))
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

            if (drag.Source == UnityTavernDragSource.GuideShapingSpell &&
                drag.RequiresPlayerTarget &&
                target == UnityTavernDropTarget.PlayerBoard)
            {
                if (targetIndex < 0)
                {
                    failureReason = UnityTavernTargetingFailureReason.MissingTarget;
                    return false;
                }

                command = new GameCommand(
                    GameCommandType.UseGuideShapingSpell,
                    targetIndex,
                    TargetZone.FriendlyBoard);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.GuideShapingSpell &&
                target == UnityTavernDropTarget.CastZone &&
                !drag.RequiresPlayerTarget)
            {
                command = new GameCommand(
                    GameCommandType.UseGuideShapingSpell,
                    -1,
                    TargetZone.Unspecified,
                    cardId: drag.Card.CardId);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.Hand && target == UnityTavernDropTarget.CastZone)
            {
                if ((drag.Card.CardKind != CardKind.TavernSpell && drag.Card.CardKind != CardKind.Spell) ||
                    drag.RequiresPlayerTarget)
                {
                    failureReason = UnityTavernTargetingFailureReason.UnsupportedTarget;
                    return false;
                }

                command = new GameCommand(GameCommandType.PlayMinion, drag.Index);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.Hand && target == UnityTavernDropTarget.PlayerBoardInsert)
            {
                if (targetIndex < 0)
                {
                    failureReason = UnityTavernTargetingFailureReason.MissingTarget;
                    return false;
                }

                if (drag.Card.CardKind != CardKind.Minion || drag.RequiresPlayerTarget)
                {
                    failureReason = UnityTavernTargetingFailureReason.UnsupportedTarget;
                    return false;
                }

                command = new GameCommand(
                    GameCommandType.PlayMinion,
                    drag.Index,
                    PlayIntent.Place,
                    boardInsertIndex: targetIndex);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.Hand &&
                (target == UnityTavernDropTarget.PlayerBoard || target == UnityTavernDropTarget.TavernShop) &&
                drag.RequiresPlayerTarget)
            {
                if (targetIndex < 0)
                {
                    failureReason = UnityTavernTargetingFailureReason.MissingTarget;
                    return false;
                }

                command = new GameCommand(
                    GameCommandType.PlayMinion,
                    drag.Index,
                    PlayIntent.Target,
                    targetIndex: targetIndex,
                    targetZone: target == UnityTavernDropTarget.PlayerBoard ? TargetZone.FriendlyBoard : TargetZone.TavernShop);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.Hand &&
                target == UnityTavernDropTarget.PlayerBoard &&
                IsMagnetic(drag.Card))
            {
                if (targetIndex < 0)
                {
                    failureReason = UnityTavernTargetingFailureReason.MissingTarget;
                    return false;
                }

                command = new GameCommand(
                    GameCommandType.PlayMinion,
                    drag.Index,
                    PlayIntent.Magnetize,
                    targetIndex: targetIndex,
                    targetZone: TargetZone.FriendlyBoard);
                return true;
            }

            if (drag.Source == UnityTavernDragSource.PlayerBoard && target == UnityTavernDropTarget.PlayerBoardInsert)
            {
                if (targetIndex < 0)
                {
                    failureReason = UnityTavernTargetingFailureReason.MissingTarget;
                    return false;
                }

                var resolvedInsertIndex = targetIndex > drag.Index ? targetIndex - 1 : targetIndex;
                command = new GameCommand(GameCommandType.MoveBoardMinion, drag.Card.InstanceId, resolvedInsertIndex);
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

        public static int ResolveBoardInsertIndex(
            System.Collections.Generic.IReadOnlyList<float> cardCenterXs,
            float pointerX,
            int previousIndex = -1,
            float hysteresis = 0f)
        {
            if (cardCenterXs == null || cardCenterXs.Count == 0)
            {
                return 0;
            }

            var proposed = 0;
            while (proposed < cardCenterXs.Count && pointerX > cardCenterXs[proposed])
            {
                proposed += 1;
            }

            if (previousIndex < 0 || previousIndex > cardCenterXs.Count ||
                Mathf.Abs(proposed - previousIndex) != 1 || hysteresis <= 0f)
            {
                return proposed;
            }

            if (proposed > previousIndex)
            {
                var boundary = cardCenterXs[Mathf.Min(previousIndex, cardCenterXs.Count - 1)];
                return pointerX >= boundary + hysteresis ? proposed : previousIndex;
            }

            var reverseBoundary = cardCenterXs[Mathf.Max(0, previousIndex - 1)];
            return pointerX <= reverseBoundary - hysteresis ? proposed : previousIndex;
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
            if (card == null)
            {
                return false;
            }

            if (card.CardKind == CardKind.Minion)
            {
                return card.Golden &&
                       string.Equals(card.CardId, CaptainSandersCardId, System.StringComparison.OrdinalIgnoreCase);
            }

            if (card.CardKind != CardKind.HeroPower)
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

        public static bool IsMagnetic(MinionInstance card)
        {
            return card != null &&
                   card.CardKind == CardKind.Minion &&
                   card.Keywords != null &&
                   card.Keywords.Contains(Keyword.Magnetic);
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UnityTavernTargetingRibbonGraphic : MaskableGraphic
    {
        private const int ForbiddenCircleSegments = 18;
        [SerializeField] private Vector2 startPoint;
        [SerializeField] private Vector2 endPoint;
        [SerializeField] private float ribbonThickness = 22f;
        [SerializeField] private float segmentLength = 42f;
        [SerializeField] private float segmentGap = 6f;
        [SerializeField] private UnityTavernTargetingEndpointState endpointState;

        public Vector2 StartPoint => startPoint;
        public Vector2 EndPoint => endPoint;
        public UnityTavernTargetingEndpointState EndpointState => endpointState;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void SetGeometry(
            Vector2 start,
            Vector2 end,
            UnityTavernTargetingEndpointState state,
            float thickness = 22f,
            float dashLength = 42f,
            float gap = 6f)
        {
            startPoint = start;
            endPoint = end;
            endpointState = state;
            ribbonThickness = Mathf.Max(8f, thickness);
            segmentLength = Mathf.Max(10f, dashLength);
            segmentGap = Mathf.Max(3f, gap);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var delta = endPoint - startPoint;
            var distance = delta.magnitude;
            if (distance < 2f)
            {
                return;
            }

            var direction = delta / distance;
            var perpendicular = new Vector2(-direction.y, direction.x);
            var shadowColor = new Color(0.08f, 0.01f, 0.01f, 0.82f);
            var ribbonColor = new Color(UnityTavernUiStyle.Red.r, UnityTavernUiStyle.Red.g, UnityTavernUiStyle.Red.b, 0.96f);
            var step = segmentLength + segmentGap;
            for (var offset = 0f; offset < distance - ribbonThickness; offset += step)
            {
                var segmentEnd = Mathf.Min(distance - ribbonThickness, offset + segmentLength);
                AddBand(vertexHelper, startPoint + direction * offset, startPoint + direction * segmentEnd, perpendicular, ribbonThickness + 5f, shadowColor);
                AddBand(vertexHelper, startPoint + direction * offset, startPoint + direction * segmentEnd, perpendicular, ribbonThickness, ribbonColor);
            }

            var arrowBase = endPoint - direction * (ribbonThickness * 1.7f);
            AddTriangle(
                vertexHelper,
                endPoint,
                arrowBase + perpendicular * ribbonThickness,
                arrowBase - perpendicular * ribbonThickness,
                shadowColor,
                3f);
            AddTriangle(
                vertexHelper,
                endPoint,
                arrowBase + perpendicular * ribbonThickness * 0.78f,
                arrowBase - perpendicular * ribbonThickness * 0.78f,
                ribbonColor);

            if (endpointState == UnityTavernTargetingEndpointState.Invalid)
            {
                DrawForbiddenMarker(vertexHelper, endPoint, ribbonThickness * 1.45f);
            }
            else if (endpointState == UnityTavernTargetingEndpointState.Valid)
            {
                DrawValidMarker(vertexHelper, endPoint, direction, perpendicular, ribbonThickness * 1.1f);
            }
        }

        private static void AddBand(
            VertexHelper vertexHelper,
            Vector2 start,
            Vector2 end,
            Vector2 perpendicular,
            float thickness,
            Color color)
        {
            var half = perpendicular * thickness * 0.5f;
            AddQuad(vertexHelper, start - half, start + half, end + half, end - half, color);
        }

        private static void AddQuad(
            VertexHelper vertexHelper,
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d,
            Color color)
        {
            var startIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, a, color);
            AddVertex(vertexHelper, b, color);
            AddVertex(vertexHelper, c, color);
            AddVertex(vertexHelper, d, color);
            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }

        private static void AddTriangle(
            VertexHelper vertexHelper,
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Color color,
            float offset = 0f)
        {
            if (offset > 0f)
            {
                var center = (a + b + c) / 3f;
                a = center + (a - center).normalized * ((a - center).magnitude + offset);
                b = center + (b - center).normalized * ((b - center).magnitude + offset);
                c = center + (c - center).normalized * ((c - center).magnitude + offset);
            }

            var startIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, a, color);
            AddVertex(vertexHelper, b, color);
            AddVertex(vertexHelper, c, color);
            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        }

        private static void AddVertex(VertexHelper vertexHelper, Vector2 point, Color color)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = point;
            vertex.color = color;
            vertexHelper.AddVert(vertex);
        }

        private static void DrawForbiddenMarker(VertexHelper vertexHelper, Vector2 center, float radius)
        {
            var markerColor = new Color(UnityTavernUiStyle.Red.r, UnityTavernUiStyle.Red.g, UnityTavernUiStyle.Red.b, 1f);
            var shadowColor = new Color(0.05f, 0f, 0f, 0.9f);
            DrawRing(vertexHelper, center, radius + 2.5f, 7f, shadowColor);
            DrawRing(vertexHelper, center, radius, 5f, markerColor);
            var diagonal = new Vector2(radius * 0.72f, radius * 0.72f);
            var perpendicular = new Vector2(-0.7071f, 0.7071f);
            AddBand(vertexHelper, center - diagonal, center + diagonal, perpendicular, 8f, shadowColor);
            AddBand(vertexHelper, center - diagonal, center + diagonal, perpendicular, 5f, markerColor);
        }

        private static void DrawRing(VertexHelper vertexHelper, Vector2 center, float radius, float thickness, Color color)
        {
            for (var index = 0; index < ForbiddenCircleSegments; index += 1)
            {
                var startAngle = Mathf.PI * 2f * index / ForbiddenCircleSegments;
                var endAngle = Mathf.PI * 2f * (index + 1) / ForbiddenCircleSegments;
                var startDirection = new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle));
                var endDirection = new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle));
                AddQuad(
                    vertexHelper,
                    center + startDirection * (radius - thickness),
                    center + startDirection * radius,
                    center + endDirection * radius,
                    center + endDirection * (radius - thickness),
                    color);
            }
        }

        private static void DrawValidMarker(
            VertexHelper vertexHelper,
            Vector2 center,
            Vector2 direction,
            Vector2 perpendicular,
            float radius)
        {
            var color = new Color(UnityTavernUiStyle.FocusRing.r, UnityTavernUiStyle.FocusRing.g, UnityTavernUiStyle.FocusRing.b, 1f);
            AddBand(vertexHelper, center - direction * radius, center, perpendicular, 5f, color);
            AddBand(vertexHelper, center, center + perpendicular * radius * 0.65f, direction, 5f, color);
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

            owner.BeginDrag(card, source, index, eventData, transform as RectTransform);
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

    public sealed class UnityTavernDropTargetBehaviour : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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
        private bool suppressCueVisuals;
        private bool resolveTargetIndexFromPointer;
        private int pointerIndexSlotCount;
        private Text cueLabel;
        private UnityTavernDropFeedbackKind feedbackKind;

        public bool IsHighlighted => highlighted;
        public bool IsDropCueVisible => cueVisible;
        public bool IsDropAllowed => cueAllowed;
        public UnityTavernDropTarget Target => target;
        public int TargetIndex => targetIndex;
        public Color HighlightColor => Highlight(target);
        public UnityTavernDropFeedbackKind FeedbackKind => feedbackKind;
        public string CueLabel => cueLabel == null ? string.Empty : cueLabel.text;

        public void Initialize(
            UnityTavernTrainerController controller,
            UnityTavernDropTarget dropTarget,
            int index,
            bool raycastOnlyWhenAllowed = false,
            bool activeOnlyWhenAllowed = false,
            bool cueOnlyWhenAllowed = false,
            bool suppressVisuals = false,
            bool resolveIndexFromPointer = false,
            int indexSlotCount = 0)
        {
            owner = controller;
            target = dropTarget;
            targetIndex = index;
            this.raycastOnlyWhenAllowed = raycastOnlyWhenAllowed;
            this.activeOnlyWhenAllowed = activeOnlyWhenAllowed;
            this.cueOnlyWhenAllowed = cueOnlyWhenAllowed;
            suppressCueVisuals = suppressVisuals;
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
            feedbackKind = UnityTavernDropFeedbackKind.None;
            EnsureCueLabel();
            ApplyVisuals();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var resolvedIndex = ResolveTargetIndex(eventData);
            owner?.PreviewPhysicalDrop(target, resolvedIndex);
            ClearDropCue();
            owner?.HandleDrop(target, resolvedIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            highlighted = true;
            owner?.PreviewPhysicalDrop(target, ResolveTargetIndex(eventData));
            ApplyVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            highlighted = false;
            owner?.ClearPhysicalDropPreview(target);
            ApplyVisuals();
        }

        public int ResolvePointerTargetIndex(PointerEventData eventData)
        {
            return ResolveTargetIndex(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (cueVisible && cueAllowed)
            {
                owner?.HandleDrop(target, ResolveTargetIndex(eventData));
            }
        }

        public void SetDropCue(UnityTavernDragContext drag, bool commandAllowed = true)
        {
            var allowed = drag != null && commandAllowed;
            cueVisible = cueOnlyWhenAllowed ? allowed : drag != null;
            cueAllowed = allowed;
            feedbackKind = ResolveFeedbackKind(drag);

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
            feedbackKind = UnityTavernDropFeedbackKind.None;
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
            if (resolveTargetIndexFromPointer && owner != null && eventData != null)
            {
                var geometryCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
                if (target == UnityTavernDropTarget.PlayerBoardInsert)
                {
                    return owner.ResolvePlayerBoardInsertIndex(eventData.position, geometryCamera);
                }

                if (target == UnityTavernDropTarget.TavernShopInsert)
                {
                    return owner.ResolveShopInsertIndex(eventData.position, geometryCamera);
                }
            }

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
            if (suppressCueVisuals)
            {
                ApplySuppressedCueVisuals();
                return;
            }

            if (image != null)
            {
                image.color = ResolveColor();
            }

            if (outline != null)
            {
                var showOutline = target != UnityTavernDropTarget.PurchaseZone &&
                                  target != UnityTavernDropTarget.DiscoverZone &&
                                  target != UnityTavernDropTarget.SellZone &&
                                  (highlighted || cueVisible && cueAllowed);
                outline.enabled = showOutline;
                if (showOutline)
                {
                    var color = cueVisible && !cueAllowed ? UnityTavernUiStyle.Red : FeedbackColor();
                    outline.effectColor = new Color(color.r, color.g, color.b, highlighted ? 0.95f : 0.72f);
                    outline.effectDistance = cueVisible ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
                }
            }

            if (cueLabel != null)
            {
                var showLabel = cueVisible &&
                                cueAllowed &&
                                feedbackKind != UnityTavernDropFeedbackKind.Generic &&
                                feedbackKind != UnityTavernDropFeedbackKind.Sell &&
                                (feedbackKind != UnityTavernDropFeedbackKind.Place || highlighted);
                cueLabel.gameObject.SetActive(showLabel);
                cueLabel.text = FeedbackLabel(feedbackKind);
                cueLabel.color = feedbackKind == UnityTavernDropFeedbackKind.Target
                    ? UnityTavernUiStyle.Text
                    : UnityTavernUiStyle.Gold;
            }
        }

        private void ApplySuppressedCueVisuals()
        {
            if (image != null)
            {
                image.color = Color.clear;
            }

            if (outline != null)
            {
                outline.enabled = false;
            }

            var marker = transform.Find("UnityPhysicalInsertMarker");
            if (marker != null)
            {
                marker.gameObject.SetActive(highlighted && cueAllowed);
            }

            if (cueLabel != null)
            {
                cueLabel.gameObject.SetActive(false);
            }
        }

        private Color ResolveColor()
        {
            if (target == UnityTavernDropTarget.PurchaseZone ||
                target == UnityTavernDropTarget.DiscoverZone ||
                target == UnityTavernDropTarget.SellZone)
            {
                return Color.clear;
            }

            if (!cueVisible)
            {
                return highlighted
                    ? Color.Lerp(normalColor, Highlight(target), 0.55f)
                    : normalColor;
            }

            if (cueAllowed)
            {
                return Color.Lerp(normalColor, FeedbackColor(), highlighted ? 0.72f : 0.38f);
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
                case UnityTavernDropTarget.PurchaseZone:
                case UnityTavernDropTarget.DiscoverZone:
                    return UnityTavernUiStyle.Blue;
                case UnityTavernDropTarget.PlayerBoard:
                case UnityTavernDropTarget.PlayerBoardInsert:
                    return UnityTavernUiStyle.Green;
                case UnityTavernDropTarget.TavernShop:
                case UnityTavernDropTarget.TavernShopInsert:
                    return UnityTavernUiStyle.Gold;
                case UnityTavernDropTarget.OpponentBoard:
                    return UnityTavernUiStyle.ColorFromHex(0x455D83);
                case UnityTavernDropTarget.SellZone:
                    return UnityTavernUiStyle.Red;
                case UnityTavernDropTarget.CastZone:
                    return UnityTavernUiStyle.FocusRing;
                default:
                    return Color.white;
            }
        }

        private void EnsureCueLabel()
        {
            var existing = transform.Find("UnityDropCueLabelText")?.GetComponent<Text>();
            cueLabel = existing ?? UiFactory.Label("UnityDropCueLabelText", transform, string.Empty, 14, FontStyle.Bold);
            cueLabel.alignment = TextAnchor.MiddleCenter;
            cueLabel.raycastTarget = false;
            var rect = cueLabel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.38f);
            rect.anchorMax = new Vector2(0.9f, 0.62f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var labelOutline = UnityTavernUiStyle.EnsureComponent<Outline>(cueLabel.gameObject);
            labelOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            labelOutline.effectDistance = new Vector2(1f, -1f);
            cueLabel.gameObject.SetActive(false);
        }

        private UnityTavernDropFeedbackKind ResolveFeedbackKind(UnityTavernDragContext drag)
        {
            if (drag == null)
            {
                return UnityTavernDropFeedbackKind.None;
            }

            if (target == UnityTavernDropTarget.PlayerBoardInsert)
            {
                return UnityTavernDropFeedbackKind.Place;
            }

            if (target == UnityTavernDropTarget.TavernShopInsert && drag.Source == UnityTavernDragSource.Shop)
            {
                return UnityTavernDropFeedbackKind.Reorder;
            }

            if ((target == UnityTavernDropTarget.Hand ||
                 target == UnityTavernDropTarget.PurchaseZone ||
                 target == UnityTavernDropTarget.DiscoverZone) &&
                (drag.Source == UnityTavernDragSource.Shop || drag.Source == UnityTavernDragSource.Discover))
            {
                return UnityTavernDropFeedbackKind.Purchase;
            }

            if (target == UnityTavernDropTarget.SellZone && drag.Source == UnityTavernDragSource.PlayerBoard)
            {
                return UnityTavernDropFeedbackKind.Sell;
            }

            if (target == UnityTavernDropTarget.CastZone && drag.Source == UnityTavernDragSource.Hand)
            {
                return UnityTavernDropFeedbackKind.Cast;
            }

            if ((target == UnityTavernDropTarget.PlayerBoard || target == UnityTavernDropTarget.TavernShop) && drag.RequiresPlayerTarget)
            {
                return UnityTavernDropFeedbackKind.Target;
            }

            if (target == UnityTavernDropTarget.PlayerBoard && UnityTavernDragController.IsMagnetic(drag.Card))
            {
                return UnityTavernDropFeedbackKind.Magnetize;
            }

            return UnityTavernDropFeedbackKind.Generic;
        }

        private Color FeedbackColor()
        {
            switch (feedbackKind)
            {
                case UnityTavernDropFeedbackKind.Purchase:
                case UnityTavernDropFeedbackKind.Place:
                case UnityTavernDropFeedbackKind.Reorder:
                case UnityTavernDropFeedbackKind.Sell:
                case UnityTavernDropFeedbackKind.Cast:
                    return UnityTavernUiStyle.FocusRing;
                case UnityTavernDropFeedbackKind.Magnetize:
                    return UnityTavernUiStyle.Blue;
                case UnityTavernDropFeedbackKind.Target:
                    return UnityTavernUiStyle.Red;
                default:
                    return Highlight(target);
            }
        }

        private static string FeedbackLabel(UnityTavernDropFeedbackKind kind)
        {
            switch (kind)
            {
                case UnityTavernDropFeedbackKind.Purchase:
                    return "购买";
                case UnityTavernDropFeedbackKind.Place:
                    return "插入";
                case UnityTavernDropFeedbackKind.Reorder:
                    return "换位";
                case UnityTavernDropFeedbackKind.Sell:
                    return "出售";
                case UnityTavernDropFeedbackKind.Cast:
                    return "施放";
                case UnityTavernDropFeedbackKind.Magnetize:
                    return "合体";
                case UnityTavernDropFeedbackKind.Target:
                    return "目标";
                default:
                    return string.Empty;
            }
        }
    }
}
