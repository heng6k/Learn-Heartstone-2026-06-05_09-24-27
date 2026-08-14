using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernTrainerController : MonoBehaviour
    {
        private const int BoardLimit = 7;
        private const int HandLimit = 10;
        private const int CardLibraryPageSize = 32;
        private const string HeroPowerDragInstanceId = "unity-current-hero-power";
        private const int CombatStatsSampleCount = 100;
        private static readonly float[] ReplayFrameDurations = { 0.65f, 0.36f, 0.18f };
        private static readonly string[] ReplaySpeedLabels = { "1x", "2x", "4x" };
        private static readonly int[] CombatMaxStepChoices = { 50, 100, 200, 400, 800 };
        private static readonly Regex RichTextTagPattern = new Regex("<.*?>", RegexOptions.Compiled);
        private static readonly Regex OptionCountPattern = new Regex(@"(\d+)\s+option", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Tribe[] ToolsAcquisitionTribes =
        {
            Tribe.None,
            Tribe.Beast,
            Tribe.Murloc,
            Tribe.Mech,
            Tribe.Demon,
            Tribe.Dragon,
            Tribe.Pirate,
            Tribe.Elemental,
            Tribe.Quilboar,
            Tribe.Undead,
            Tribe.Naga
        };
        private static readonly string[] CardLibraryTierIconResourcePaths =
        {
            null,
            "CardImages/1.dee103e08f71abf6b31e",
            "CardImages/2.44901fe70968648d1c8c",
            "CardImages/3.51641e5957a78d91bcbe",
            "CardImages/4.5ef126193a0e435de08e",
            "CardImages/5.7c89576b8a39923564e8",
            "CardImages/6.1191535bba19ee35692e",
            "CardImages/7.977c3d29cd6965ac0dd1"
        };

        private enum UnityTavernActionButtonRole
        {
            Neutral,
            Economy,
            Primary,
            Combat,
            Utility,
            Danger
        }

        private enum UnityCardLibraryDestination
        {
            PlayerHand,
            OpponentHand,
            OpponentBoard,
            OpponentStartOfCombatSpell
        }

        private enum AdvancedCardLibrarySelectionKind
        {
            QuestReward,
            LesserTrinket,
            GreaterTrinket
        }

        private enum OpponentMechanicLibraryKind
        {
            HeroPower,
            QuestReward,
            LesserTrinket,
            GreaterTrinket
        }

        private sealed class AdvancedCardLibraryItem
        {
            public CardKind CardKind;
            public string CardId;
            public string DisplayName;
            public string Text;
            public string ImagePath;
            public string Meta;
            public string Notes;
            public int TargetIndex;
        }

        private sealed class EffectDisplayItem
        {
            public string Id;
            public string ObjectName;
            public string Type;
            public string Name;
            public string Description;
            public string Source;
            public string Status;
            public string Badge;
            public string CardId;
            public string ImagePath;
            public CardKind CardKind;
            public int SortOrder;
            public Color Accent;
            public Action OnClick;
            public HeroPowerDefinition HeroPower;
            public bool Interactable = true;
        }

        private MatchService service;
        private StrategyGuideSession strategyGuideSession;
        private IAdvisorService advisor;
        private Action backToHub;
        private string selectedInstanceId;
        private string lastError;
        private string lastFeedback;
        private UnityTavernDragContext activeDrag;
        private GameObject dragGhost;
        private RectTransform physicalDragSourceRect;
        private CanvasGroup physicalDragSourceGroup;
        private float physicalDragSourceAlpha = 1f;
        private bool physicalDragSourceBlocksRaycasts = true;
        private bool physicalDragSourceInteractable = true;
        private RectTransform physicalDragSourceSlotRect;
        private LayoutElement physicalDragSourceSlotLayout;
        private bool physicalDragSourceSlotIgnoredLayout;
        private readonly Dictionary<RectTransform, Vector2> physicalCardHomePositions = new Dictionary<RectTransform, Vector2>();
        private readonly Dictionary<RectTransform, Vector2> physicalCardTargetPositions = new Dictionary<RectTransform, Vector2>();
        private readonly List<float> physicalBoardCardCenterXs = new List<float>();
        private float physicalBoardCardWidth;
        private readonly List<float> physicalShopCardCenterXs = new List<float>();
        private float physicalShopCardWidth;
        private UnityTavernDropTarget physicalPreviewTarget;
        private int physicalPreviewIndex = -1;
        private Vector2 dragGhostTargetLocalPosition;
        private Vector2 dragGhostLastTargetLocalPosition;
        private bool dragGhostTargetAvailable;
        private float dragGhostTargetRotation;
        private Coroutine physicalDragTransition;
        private bool physicalCommitInProgress;
        private string confirmedTargetInstanceId;
        private string confirmedSecondaryTargetInstanceId;
        private float confirmedTargetUntil;
        private int pendingPrimaryTargetIndex = -1;
        private TargetZone pendingPrimaryTargetZone = TargetZone.Unspecified;
        private string pendingPrimaryTargetInstanceId;
        private int pendingSecondaryTargetIndex = -1;
        private TargetZone pendingSecondaryTargetZone = TargetZone.Unspecified;
        private string pendingSecondaryTargetInstanceId;
        private RectTransform targetingHoverAnchor;
        private bool targetingHoverAllowed;
        private Vector2 targetingPointerLocalPosition;
        private bool targetingPointerAvailable;
        private int lastTargetingCancelFrame = -1;
        private float nextTargetLockedHintTime;
        private readonly Dictionary<string, RectTransform> guideShapingSpellSourceAnchors =
            new Dictionary<string, RectTransform>(StringComparer.Ordinal);
        private GameObject targetingConnector;
        private bool rightPanelOpen;
        private bool strategyGuideGoalDrawerOpen;
        private UnityTavernInspectorTab activeInspectorTab = UnityTavernInspectorTab.Actions;
        private bool cardDetailOpen;
        private bool combatReplayOpen;
        private bool toolsOpen;
        private bool toolsAdvancedMode;
        private bool opponentPanelOpen;
        private bool cardLibraryOpen;
        private MinionInstance cardLibraryDetailCard;
        private bool heroSelectionOpen;
        private string minionEditorInstanceId;
        private BoardSide minionEditorSide;
        private GameObject keywordTooltip;
        private int activeReplayFrameIndex;
        private bool replayPlaying;
        private bool replayWasPlayingBeforeTimeline;
        private float replayPlaybackElapsed;
        private int replaySpeedIndex;
        private int combatMaxSteps = 200;
        private bool combatTimelineOpen;
        private UnityReplayTimelineFilter replayTimelineFilter = UnityReplayTimelineFilter.All;
        private CombatRunStats combatRunStats;
        private CardKind toolsAcquisitionKind = CardKind.Minion;
        private int toolsAcquisitionTierFilter;
        private Tribe toolsAcquisitionTribeFilter = Tribe.All;
        private string toolsAcquisitionSearchText = string.Empty;
        private ScrollRect cardLibraryScrollRect;
        private readonly Dictionary<Button, MinionInstance> cardLibraryAddButtons = new Dictionary<Button, MinionInstance>();
        private float cardLibraryScrollPosition = 1f;
        private bool restoreCardLibraryScrollPosition;
        private UnityCardLibraryDestination cardLibraryDestination = UnityCardLibraryDestination.PlayerHand;
        private bool opponentCardLibraryGolden;
        private bool toolsShowAllCards;
        private int cardLibraryVisibleLimit = CardLibraryPageSize;
        private HeroPowerCategory? toolsHeroPowerCategoryFilter;
        private HeroPowerReplacementEligibility? toolsHeroPowerEligibilityFilter;
        private bool advancedCardLibraryOpen;
        private AdvancedCardLibrarySelectionKind advancedCardLibraryKind = AdvancedCardLibrarySelectionKind.QuestReward;
        private int advancedCardLibraryQuestIndex;
        private string advancedCardLibrarySearchText = string.Empty;
        private bool opponentMechanicLibraryOpen;
        private OpponentMechanicLibraryKind opponentMechanicLibraryKind = OpponentMechanicLibraryKind.QuestReward;
        private string opponentMechanicLibrarySearchText = string.Empty;
        private AdvancedCardLibraryItem mechanicLibraryDetailItem;
        private bool playerDirectedChoiceOpen;
        private bool returnConfirmOpen;
        private int selectedDarkGiftChoiceIndex = -1;
        private int darkGiftChoicePageIndex;
        private string darkGiftChoiceRequestId;
        private bool darkGiftBlockingExplanationVisible;
        private bool darkGiftDetailsOpen;
        private string pendingRecruitActionSourceInstanceId;
        private RecruitActionDefinition pendingRecruitActionDefinition;
        private int pendingDiscoverTargetOptionIndex = -1;
        private string pendingDiscoverTargetSourceInstanceId;
        private MinionInstance pendingDiscoverTargetOptionCard;
        private MinionInstance pendingHandChooseOneCard;
        private int pendingHandChooseOneIndex = -1;
        private ChooseOneOptionDefinition pendingHandChooseOneOption;
        private PlayerDirectedChoiceKind playerDirectedChoiceKind = PlayerDirectedChoiceKind.QuestPair;
        private TrinketSlotKind playerDirectedTrinketSlotKind = TrinketSlotKind.Lesser;
        private string playerDirectedSearchText = string.Empty;
        private int playerDirectedSelectableFilter;
        private int playerDirectedCostFilter;
        private string playerDirectedSlotFilter = string.Empty;
        private string playerDirectedTagFilter = string.Empty;
        private bool rebuildQueued;

        public bool IsPhysicalDragActive => IsPhysicalCardDrag(activeDrag);
        public bool IsPhysicalCommitInProgress => physicalCommitInProgress;
        public int PhysicalPreviewIndex => physicalPreviewIndex;
        public UnityTavernDropTarget PhysicalPreviewTarget => physicalPreviewTarget;

        private sealed class CombatRunStats
        {
            public int Samples;
            public int Wins;
            public int Draws;
            public int Losses;
            public int OverLimits;
            public int MaxSteps;
        }

        public void Initialize(
            MatchService matchService,
            IAdvisorService advisorService,
            Action backAction,
            Action legacyAction,
            StrategyGuideSession guideSession = null)
        {
            service = matchService;
            strategyGuideSession = guideSession;
            advisor = advisorService;
            backToHub = backAction;
            selectedInstanceId = service.State.Player.Tavern.Shop.FirstOrDefault(card => card != null)?.InstanceId;
            Rebuild();
        }

        private void Update()
        {
            TickReplayPlayback(UnityEngine.Time.unscaledDeltaTime);
            UpdateTargetingPointerFromMouse();
            UpdatePhysicalDragFromMouse();
            TickPhysicalDragVisuals(UnityEngine.Time.unscaledDeltaTime);

            if ((!string.IsNullOrEmpty(confirmedTargetInstanceId) || !string.IsNullOrEmpty(confirmedSecondaryTargetInstanceId)) &&
                UnityEngine.Time.unscaledTime >= confirmedTargetUntil)
            {
                confirmedTargetInstanceId = null;
                confirmedSecondaryTargetInstanceId = null;
                RefreshTargetingClarity();
            }

            if (rebuildQueued)
            {
                rebuildQueued = false;
                Rebuild();
            }
        }

        private void UpdateTargetingPointerFromMouse()
        {
            if (!HasActiveTargetingSession())
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            UpdateTargetingPointer(mouse.position.ReadValue(), camera);
            if (mouse.rightButton.wasPressedThisFrame)
            {
                CancelCurrentTargeting();
            }
        }

        private void UpdatePhysicalDragFromMouse()
        {
            if (!IsPhysicalCardDrag(activeDrag) || physicalCommitInProgress)
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                CancelCurrentPhysicalDrag();
            }
        }

        private void TickPhysicalDragVisuals(float deltaTime)
        {
            var blend = UnityUiMotionSettings.ReduceMotion
                ? 1f
                : 1f - Mathf.Exp(-Mathf.Max(0f, deltaTime) * 18f);
            foreach (var pair in physicalCardTargetPositions)
            {
                if (pair.Key != null)
                {
                    pair.Key.anchoredPosition = Vector2.Lerp(pair.Key.anchoredPosition, pair.Value, blend);
                }
            }

            if (dragGhost == null || !dragGhostTargetAvailable || physicalDragTransition != null)
            {
                return;
            }

            var ghostRect = dragGhost.GetComponent<RectTransform>();
            if (ghostRect == null)
            {
                return;
            }

            ghostRect.anchoredPosition = Vector2.Lerp(ghostRect.anchoredPosition, dragGhostTargetLocalPosition, blend);
            var rotation = Quaternion.Euler(0f, 0f, dragGhostTargetRotation);
            ghostRect.localRotation = Quaternion.Slerp(ghostRect.localRotation, rotation, blend);
        }

        public void UpdateTargetingPointer(Vector2 screenPosition, Camera eventCamera = null)
        {
            var root = transform as RectTransform;
            if (root == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, eventCamera, out var localPosition))
            {
                return;
            }

            if (targetingPointerAvailable && (targetingPointerLocalPosition - localPosition).sqrMagnitude < 0.04f)
            {
                return;
            }

            targetingPointerLocalPosition = localPosition;
            targetingPointerAvailable = true;
            RefreshTargetingConnector();
        }

        public void Rebuild()
        {
            if (UnityEngine.Application.isPlaying &&
                (CanvasUpdateRegistry.IsRebuildingLayout() || CanvasUpdateRegistry.IsRebuildingGraphics()))
            {
                rebuildQueued = true;
                return;
            }

            rebuildQueued = false;
            EnsureRecruitActionUiStateIsLive();
            cardLibraryScrollRect = null;
            ClearChildren();
            keywordTooltip = null;
            targetingHoverAnchor = null;
            guideShapingSpellSourceAnchors.Clear();
            targetingConnector = null;
            BuildBackground();
            BuildTopBar();
            BuildStrategyGuideHud();
            BuildPlaySurface();
            BuildTavernActionBar();
            BuildHeroEffectRack();
            BuildQuestTrackerOverlay();
            BuildAdvancedChoiceStatusPanel();
            BuildRecruitActionTargetStatus();
            BuildStrategyGuideGoalDrawerToggle();
            BuildRightPanelDrawerToggle();

            if (rightPanelOpen)
            {
                BuildFloatingRightPanel();
            }

            if (strategyGuideGoalDrawerOpen)
            {
                BuildStrategyGuideGoalDrawer();
            }

            if (opponentPanelOpen)
            {
                BuildOpponentPanelOverlay();
            }

            if (combatReplayOpen)
            {
                BuildCombatReplayPanel();
            }

            if (cardDetailOpen)
            {
                BuildCardDetailModal();
            }

            if (toolsOpen)
            {
                BuildToolsModal();
            }

            if (cardLibraryOpen)
            {
                BuildCardLibraryOverlay();
                if (cardLibraryDetailCard != null)
                {
                    BuildCardLibraryDetailModal();
                }
            }

            if (advancedCardLibraryOpen)
            {
                BuildAdvancedCardLibraryOverlay();
            }

            if (opponentMechanicLibraryOpen)
            {
                BuildOpponentMechanicLibraryOverlay();
            }

            if (mechanicLibraryDetailItem != null && (advancedCardLibraryOpen || opponentMechanicLibraryOpen))
            {
                BuildMechanicLibraryDetailModal();
            }

            if (heroSelectionOpen)
            {
                BuildHeroSelectionOverlay();
            }

            if (!string.IsNullOrEmpty(minionEditorInstanceId))
            {
                BuildMinionEditModal();
            }

            if (pendingHandChooseOneCard != null && pendingHandChooseOneOption == null)
            {
                BuildHandChooseOneModal();
            }
            else if (service.State.Player.Tavern.Discover != null && !IsDiscoverOptionTargeting())
            {
                BuildDiscoverModal();
            }

            if (service.GetActiveMechanicChoice() != null)
            {
                BuildAdvancedMechanicChoiceModal();
            }

            if (playerDirectedChoiceOpen)
            {
                BuildPlayerDirectedChoiceModal();
            }

            if (darkGiftDetailsOpen && service.GetActiveMechanicChoice()?.Kind != AdvancedMechanicKind.DarkGift)
            {
                BuildDarkGiftDetailsOverlay();
            }

            if (strategyGuideSession?.RunState == StrategyGuideRunState.Completed && !combatReplayOpen)
            {
                BuildStrategyGuideCompletionOverlay();
            }

            if (returnConfirmOpen)
            {
                BuildReturnConfirmOverlay();
            }

            if (service.State.Player.Tavern.Timewarp?.VisitOpen == true)
            {
                BuildTimewarpedTavernModal();
            }

            if (!string.IsNullOrEmpty(lastError))
            {
                BuildErrorToast(lastError);
            }
            else if (!string.IsNullOrEmpty(lastFeedback))
            {
                BuildFeedbackToast(lastFeedback);
            }

            if (IsDiscoverOptionTargeting())
            {
                EnsureDiscoverTargetingSourceAnchor();
            }

            if (IsHandChooseOneTargeting())
            {
                EnsureHandChooseOneTargetingSourceAnchor();
            }

            RefreshTargetingClarity();
            RestoreCardLibraryScrollPosition();
        }

        private UnityTavernLayoutContext LayoutContext()
        {
            return UnityTavernLayoutContext.FromRoot(transform);
        }

        private string T(string chinese, string english)
        {
            return service.UseEnglish ? english : chinese;
        }

        private string DisplayCardName(MinionInstance card)
        {
            return !service.UseEnglish && !string.IsNullOrEmpty(card?.ZhName) ? card.ZhName : card?.Name ?? string.Empty;
        }

        private string DisplayCardText(MinionInstance card)
        {
            return !service.UseEnglish && !string.IsNullOrEmpty(card?.ZhText) ? card.ZhText : card?.Text ?? string.Empty;
        }

        private string DisplayStrategyGuideGrowthLabel(string key)
        {
            if (string.Equals(key, StrategyGuideGrowthKeys.BeastLobsterGrowth, StringComparison.Ordinal))
            {
                return T("美味龙虾成长", "Tasty Lobster growth");
            }
            if (string.Equals(key, StrategyGuideGrowthKeys.TavernSpellsCastThisGame, StringComparison.Ordinal))
            {
                return T("本局施放酒馆法术", "Tavern spells cast");
            }
            if (string.Equals(key, StrategyGuideGrowthKeys.DemonTavernBonusAttack, StringComparison.Ordinal))
            {
                return T("酒馆永久攻击成长", "Permanent Tavern Attack");
            }
            if (string.Equals(key, StrategyGuideGrowthKeys.DemonTavernBonusHealth, StringComparison.Ordinal))
            {
                return T("酒馆永久生命成长", "Permanent Tavern Health");
            }
            return key ?? string.Empty;
        }

        private void BuildBackground()
        {
            var back = Panel("UnityTavernBackWall", transform, UnityTavernUiStyle.BackWall);
            UnityTavernUiStyle.Stretch(back.GetComponent<RectTransform>());

            var table = Panel(
                "UnityTavernTableGlow",
                transform,
                new Color(UnityTavernUiStyle.TableLit.r, UnityTavernUiStyle.TableLit.g, UnityTavernUiStyle.TableLit.b, 0.32f));
            var tableRect = table.GetComponent<RectTransform>();
            tableRect.anchorMin = new Vector2(0.02f, 0.05f);
            tableRect.anchorMax = new Vector2(0.98f, 0.88f);
            tableRect.offsetMin = Vector2.zero;
            tableRect.offsetMax = Vector2.zero;
        }

        private void BuildTopBar()
        {
            var bar = Panel(
                "UnityTopBar",
                transform,
                new Color(UnityTavernUiStyle.SurfaceDark.r, UnityTavernUiStyle.SurfaceDark.g, UnityTavernUiStyle.SurfaceDark.b, 0.98f));
            UnityTavernUiStyle.ConfigureOutline(
                bar,
                new Color(UnityTavernUiStyle.Brass.r, UnityTavernUiStyle.Brass.g, UnityTavernUiStyle.Brass.b, 0.42f),
                new Vector2(1f, -1f));
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0f, -78f);
            rect.offsetMax = Vector2.zero;

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 12);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var rail = Panel(
                "UnityStarLanternRail",
                bar.transform,
                new Color(UnityTavernUiStyle.Brass.r, UnityTavernUiStyle.Brass.g, UnityTavernUiStyle.Brass.b, 0.64f));
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(rail).ignoreLayout = true;
            var railRect = rail.GetComponent<RectTransform>();
            railRect.anchorMin = Vector2.zero;
            railRect.anchorMax = new Vector2(1f, 0f);
            railRect.pivot = new Vector2(0.5f, 0f);
            railRect.sizeDelta = new Vector2(0f, 2f);
            railRect.anchoredPosition = Vector2.zero;

            var facet = Panel("UnityStarLanternFacet", bar.transform, UnityTavernUiStyle.ArcaneBlue);
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(facet).ignoreLayout = true;
            var facetRect = facet.GetComponent<RectTransform>();
            facetRect.anchorMin = new Vector2(0.5f, 0f);
            facetRect.anchorMax = new Vector2(0.5f, 0f);
            facetRect.pivot = new Vector2(0.5f, 0.5f);
            facetRect.sizeDelta = new Vector2(10f, 10f);
            facetRect.anchoredPosition = new Vector2(0f, 1f);
            facetRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

            var titleBlock = new GameObject("UnityTitleBlock", typeof(RectTransform));
            titleBlock.transform.SetParent(bar.transform, false);
            UnityTavernUiStyle.SetFixedSize(titleBlock, 280f, 54f);
            var titleLayout = titleBlock.AddComponent<VerticalLayoutGroup>();
            titleLayout.spacing = 0;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;

            var title = UiFactory.Label("UnityTitle", titleBlock.transform, T("星灯秘法酒馆", "Starlight Arcane Tavern"), 24, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            UiFactory.Label("UnityMatchVersionBadge", titleBlock.transform, LockedVersionBadgeText(), 14, FontStyle.Bold).color = UnityTavernUiStyle.MutedText;

            BuildHeroBadge(bar.transform);
            ResourcePill(bar.transform, "Round", service.UseEnglish ? "Round" : "回合", service.State.Round.ToString(), UnityTavernUiStyle.TableLit);
            ResourcePill(bar.transform, "Gold", service.UseEnglish ? "Gold" : "金币", service.State.Player.Tavern.Gold + "/" + service.State.Player.Tavern.MaxGold, UnityTavernUiStyle.Gold);
            ResourcePill(bar.transform, "Tavern", service.UseEnglish ? "Tavern" : "酒馆", service.State.Player.Tavern.Tier + (service.UseEnglish ? " Stars" : " 星"), UnityTavernUiStyle.Blue);
            ResourcePill(bar.transform, "Health", service.UseEnglish ? "Health" : "生命", service.State.Player.Health.ToString(), UnityTavernUiStyle.Red);
            ResourcePill(bar.transform, "Tribes", service.UseEnglish ? "Tribes" : "种族", ActiveLibraryTribes().Count + "/10", UnityTavernUiStyle.Green);

            var spacer = new GameObject("UnityTopBarSpacer", typeof(RectTransform));
            spacer.transform.SetParent(bar.transform, false);
            UnityTavernUiStyle.SetFlexible(spacer, 1f, 0f);

            SmallButton("UnityBackButton", bar.transform, T("返回", "Back"), OpenReturnConfirmation, 48f);
        }

        private void BuildStrategyGuideHud()
        {
            if (strategyGuideSession == null || strategyGuideSession.RunState == StrategyGuideRunState.FreeExplore)
            {
                return;
            }

            var layoutContext = LayoutContext();
            var panel = Panel(
                "UnityStrategyGuideHud",
                transform,
                new Color(UnityTavernUiStyle.SurfaceDark.r, UnityTavernUiStyle.SurfaceDark.g, UnityTavernUiStyle.SurfaceDark.b, 0.97f));
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(layoutContext.IsCompact ? 8f : 18f, layoutContext.IsCompact ? -150f : -138f);
            rect.offsetMax = new Vector2(layoutContext.IsCompact ? -8f : -18f, -84f);
            panel.GetComponent<Image>().raycastTarget = false;
            UnityTavernUiStyle.ConfigureOutline(panel, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.ArcaneBlue, 0.62f), new Vector2(1f, -1f));

            var row = panel.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(10, 10, 6, 6);
            row.spacing = layoutContext.IsCompact ? 6f : 10f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var title = UiFactory.Label(
                "UnityStrategyGuideTitle",
                panel.transform,
                service.UseEnglish && !string.IsNullOrWhiteSpace(strategyGuideSession.Guide.EnglishTitle)
                    ? strategyGuideSession.Guide.EnglishTitle + " · " + strategyGuideSession.Profile.EnglishTitle
                    : strategyGuideSession.Guide.Title + " · " + strategyGuideSession.Profile.Title,
                layoutContext.IsCompact ? 16 : 18,
                FontStyle.Bold);
            title.color = UnityTavernUiStyle.Gold;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFixedSize(title.gameObject, layoutContext.IsCompact ? 150f : 230f, UnityTavernUiStyle.TouchHeight);

            var instructionText = "▶ " + strategyGuideSession.NextInstruction(service.UseEnglish);
            var acquisitionStatus = strategyGuideSession.AcquisitionStatus(service.UseEnglish);
            var greaterTrinketGate = StrategyGuideGreaterTrinketGateText();
            if (!string.IsNullOrWhiteSpace(greaterTrinketGate) || !string.IsNullOrWhiteSpace(acquisitionStatus))
            {
                instructionText += "\n◆ " + string.Join(
                    " · ",
                    new[] { greaterTrinketGate, acquisitionStatus }.Where(value => !string.IsNullOrWhiteSpace(value)));
            }
            var instruction = UiFactory.Label(
                "UnityStrategyGuideInstruction",
                panel.transform,
                instructionText,
                14,
                FontStyle.Bold);
            instruction.color = UnityTavernUiStyle.Text;
            instruction.alignment = TextAnchor.MiddleLeft;
            instruction.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetFlexible(instruction.gameObject, 1f, 0f);
            UnityTavernUiStyle.SetPreferredHeight(instruction.gameObject, UnityTavernUiStyle.TouchHeight);

            var evaluation = strategyGuideSession.Evaluation;
            var progress = UiFactory.Label(
                "UnityStrategyGuideProgress",
                panel.transform,
                T("步骤 ", "Steps ") + evaluation.CompletedActionCount + "/" + evaluation.RequiredActionCount +
                T(" · 阵容 ", " · Board ") + evaluation.MatchedFinalSlots + "/" + evaluation.FinalSlotCount,
                14,
                FontStyle.Bold);
            progress.color = UnityTavernUiStyle.ArcaneBlue;
            progress.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetFixedSize(progress.gameObject, layoutContext.IsCompact ? 124f : 174f, UnityTavernUiStyle.TouchHeight);

            if (strategyGuideSession.Profile.Undo.UsesPerRun > 0)
            {
                ActionButton(
                    "UnityStrategyGuideUndoButton",
                    panel.transform,
                    strategyGuideSession.UndoUsesRemaining > 0 ? T("撤销一步", "Undo") : T("撤销已用", "Undo Used"),
                    UndoStrategyGuideAction,
                    layoutContext.IsCompact ? 88f : 108f,
                    UnityTavernUiStyle.TouchHeight,
                    false,
                    UnityTavernActionButtonRole.Utility,
                    strategyGuideSession.CanUndo);
            }
            ActionButton(
                "UnityStrategyGuideRestartButton",
                panel.transform,
                T("重开", "Restart"),
                RestartStrategyGuide,
                layoutContext.IsCompact ? 72f : 92f,
                UnityTavernUiStyle.TouchHeight,
                false,
                UnityTavernActionButtonRole.Danger);
        }

        private string StrategyGuideGreaterTrinketGateText()
        {
            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            if (!string.IsNullOrWhiteSpace(trinkets?.GreaterTrinketId))
            {
                return null;
            }

            var schedule = strategyGuideSession.Profile.AcquisitionPlan?.OfferSchedules?.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.Source, StrategyGuideOfferSources.GreaterTrinketChoice, StringComparison.Ordinal) &&
                item.MinimumRequiredTribeMinions > 0);
            if (schedule == null ||
                !Enum.TryParse(schedule.RequiredTribe, true, out Tribe requiredTribe) ||
                requiredTribe == Tribe.None)
            {
                return null;
            }

            var current = service.State.Player.Board.Count(card => BoardTribeAnalyzer.HasTribe(card, requiredTribe));
            return service.UseEnglish
                ? "Greater Trinket gate: at least " + schedule.MinimumRequiredTribeMinions + " " +
                  TribeName(requiredTribe) + " (current " + current + ")"
                : "大饰品门槛：至少" + schedule.MinimumRequiredTribeMinions + "只" +
                  TribeName(requiredTribe) + "（当前" + current + "）";
        }

        private void BeginGuideShapingSpell(MinionInstance spell)
        {
            if (spell == null || strategyGuideSession?.CanApply(GameCommandType.UseGuideShapingSpell) != true)
            {
                return;
            }

            if (service.RequiresPlayerTarget(spell))
            {
                BeginDrag(spell, UnityTavernDragSource.GuideShapingSpell, 0);
                return;
            }

            lastError = null;
            lastFeedback = T("请把这张塑造法术拖到中央施放区。", "Drag this shaping spell to the center cast area.");
            BuildFeedbackToast(lastFeedback);
        }

        private void UndoStrategyGuideAction()
        {
            var result = strategyGuideSession?.Undo();
            lastError = result != null && !result.Succeeded ? T("当前不能撤销。", "Undo is unavailable now.") : null;
            lastFeedback = result?.Succeeded == true ? T("已撤销上一步；本局撤销次数已用完。", "Last action undone; undo is now used.") : null;
            ClearPendingRecruitAction();
            Rebuild();
        }

        private void RestartStrategyGuide()
        {
            strategyGuideSession?.Restart();
            lastError = null;
            lastFeedback = T("已从攻略起点重新开始。", "Guide restarted from its initial state.");
            combatReplayOpen = false;
            ClearPendingRecruitAction();
            Rebuild();
        }

        private void EnterStrategyGuideFreeExplore()
        {
            var result = strategyGuideSession?.EnterFreeExplore();
            if (result?.Succeeded != true)
            {
                lastError = T("尚未满足自由探索条件。", "Free Explore is still locked.");
            }
            else
            {
                strategyGuideGoalDrawerOpen = false;
                lastError = null;
                lastFeedback = T("已进入自由探索；攻略限制已解除。", "Free Explore started; guide restrictions are disabled.");
            }
            Rebuild();
        }

        private void BuildStrategyGuideCompletionOverlay()
        {
            var overlay = Panel("UnityStrategyGuideCompletionOverlay", transform, new Color(0f, 0f, 0f, 0.76f));
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().raycastTarget = true;
            overlay.transform.SetAsLastSibling();

            var layoutContext = LayoutContext();
            var card = Panel("UnityStrategyGuideCompletionCard", overlay.transform, UnityTavernUiStyle.SurfaceDark);
            UnityTavernUiStyle.ConfigureOutline(card, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SuccessGreen, 0.82f), new Vector2(2f, -2f));
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(
                Mathf.Min(560f, Mathf.Max(300f, layoutContext.Width - 32f)),
                Mathf.Min(286f, Mathf.Max(236f, layoutContext.Height - 32f)));

            var stack = card.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(20, 20, 18, 18);
            stack.spacing = 12f;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityStrategyGuideCompletionTitle", card.transform, T("挑战完成", "Challenge Complete"), 24, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 38f);
            var message = UiFactory.Label(
                "UnityStrategyGuideCompletionMessage",
                card.transform,
                T("成型阵容、关键步骤和验收战斗均已通过。接下来想怎么继续？", "The final warband, required steps, and validation combat all passed. What next?"),
                16,
                FontStyle.Bold);
            message.alignment = TextAnchor.MiddleCenter;
            message.color = UnityTavernUiStyle.Text;
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetPreferredHeight(message.gameObject, 58f);

            var actions = Panel("UnityStrategyGuideCompletionActions", card.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(actions, 58f);
            var row = actions.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            Button first = null;
            foreach (var choice in strategyGuideSession.Profile.Victory.PostWinChoices)
            {
                Button button = null;
                switch (choice)
                {
                    case "FreeExplore":
                        button = ActionButton("UnityStrategyGuideFreeExploreButton", actions.transform, T("自由探索", "Free Explore"), EnterStrategyGuideFreeExplore, 130f, UnityTavernUiStyle.TouchHeight, true, UnityTavernActionButtonRole.Primary);
                        break;
                    case "Restart":
                        button = ActionButton("UnityStrategyGuideCompletionRestartButton", actions.transform, T("重新挑战", "Restart"), RestartStrategyGuide, 130f, UnityTavernUiStyle.TouchHeight, true, UnityTavernActionButtonRole.Utility);
                        break;
                    case "Return":
                        button = ActionButton("UnityStrategyGuideReturnButton", actions.transform, T("返回大厅", "Return"), () => backToHub?.Invoke(), 130f, UnityTavernUiStyle.TouchHeight, true, UnityTavernActionButtonRole.Neutral);
                        break;
                }

                if (first == null && button != null)
                {
                    first = button;
                }
            }

            if (first != null)
            {
                UnityTavernUiStyle.EnsureComponent<UnityFocusTrap>(overlay).Activate(first.gameObject);
            }
        }

        private void OpenReturnConfirmation()
        {
            returnConfirmOpen = true;
            Rebuild();
        }

        private void CancelReturnConfirmation()
        {
            returnConfirmOpen = false;
            Rebuild();
        }

        private void ConfirmReturnToHub()
        {
            returnConfirmOpen = false;
            Rebuild();
            backToHub?.Invoke();
        }

        private void BuildReturnConfirmOverlay()
        {
            var overlay = Panel("UnityReturnConfirmOverlay", transform, new Color(0f, 0f, 0f, 0.68f));
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;
            overlay.transform.SetAsLastSibling();

            var layoutContext = LayoutContext();
            var panel = Panel("UnityReturnConfirmPanel", overlay.transform, UnityTavernUiStyle.SurfaceDark);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.DangerRed, 0.72f),
                new Vector2(2f, -2f));
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityReturnConfirmStarLantern", UnityTavernUiStyle.DangerRed);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(
                Mathf.Min(480f, Mathf.Max(280f, layoutContext.Width - 32f)),
                Mathf.Min(220f, Mathf.Max(180f, layoutContext.Height - 32f)));

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(18, 18, 16, 16);
            panelLayout.spacing = 12;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityReturnConfirmTitle", panel.transform, T("退出本局模拟？", "Exit this simulation?"), 20, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 32f);

            var message = UiFactory.Label("UnityReturnConfirmMessage", panel.transform, T("返回后退出本局模拟", "Returning will end this simulation."), 15, FontStyle.Bold);
            message.alignment = TextAnchor.MiddleCenter;
            message.color = UnityTavernUiStyle.Text;
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetFlexible(message.gameObject, 1f, 0f);

            var actions = Panel("UnityReturnConfirmActions", panel.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(actions, 56f);
            var actionLayout = actions.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 12;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = true;

            ActionButton(
                "UnityReturnConfirmYesButton",
                actions.transform,
                T("是", "Yes"),
                ConfirmReturnToHub,
                120f,
                UnityTavernUiStyle.TouchHeight,
                true,
                UnityTavernActionButtonRole.Danger);
            var no = ActionButton(
                "UnityReturnConfirmNoButton",
                actions.transform,
                T("否", "No"),
                CancelReturnConfirmation,
                120f,
                UnityTavernUiStyle.TouchHeight,
                true,
                UnityTavernActionButtonRole.Utility);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(no.gameObject);
            }
        }

        private void BuildHeroBadge(Transform parent)
        {
            var hero = CurrentHero();
            var badge = new GameObject("UnityHeroBadge", typeof(RectTransform), typeof(Image), typeof(Button));
            badge.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(badge, 210f, 54f);
            var image = badge.GetComponent<Image>();
            image.color = UnityTavernUiStyle.PanelRaised;
            image.raycastTarget = true;
            UnityTavernUiStyle.ConfigureOutline(badge, new Color(UnityTavernUiStyle.ArcaneBlue.r, UnityTavernUiStyle.ArcaneBlue.g, UnityTavernUiStyle.ArcaneBlue.b, 0.52f), new Vector2(1f, -1f));

            var button = badge.GetComponent<Button>();
            var heroLockedByGuide = strategyGuideSession != null && strategyGuideSession.RunState != StrategyGuideRunState.FreeExplore;
            button.interactable = !heroLockedByGuide;
            if (!heroLockedByGuide)
            {
                button.onClick.AddListener(OpenHeroSelection);
            }
            UnityTavernUiStyle.TintSelectable(button, UnityTavernUiStyle.PanelRaised, Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.16f), Color.Lerp(UnityTavernUiStyle.PanelRaised, Color.black, 0.16f));

            var row = badge.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(6, 8, 5, 5);
            row.spacing = 7;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            BuildHeroBadgeImage(badge.transform, hero);

            var stack = Panel("UnityHeroBadgeTextStack", badge.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(stack, 1f, 0f);
            var stackLayout = stack.AddComponent<VerticalLayoutGroup>();
            stackLayout.spacing = 1;
            stackLayout.childControlWidth = true;
            stackLayout.childControlHeight = true;
            stackLayout.childForceExpandWidth = true;
            stackLayout.childForceExpandHeight = false;

            var name = UiFactory.Label("UnityHeroBadgeName", stack.transform, hero == null ? T("未设置", "Not Set") : hero.Name, 14, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 21f);

            var power = UiFactory.Label("UnityHeroBadgePower", stack.transform, CurrentHeroPowerName(), 14, FontStyle.Bold);
            power.color = UnityTavernUiStyle.Gold;
            power.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(power.gameObject, 19f);
        }

        private void BuildHeroBadgeImage(Transform parent, HeroDefinition hero)
        {
            var frame = Panel("UnityHeroBadgeImage", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetFixedSize(frame, 44f, 44f);
            UnityTavernUiStyle.ConfigureOutline(frame, new Color(0f, 0f, 0f, 0.32f), new Vector2(1f, -1f));

            var sprite = hero == null ? null : CardImageProvider.LoadSprite(hero.ImagePath, hero.HeroCardId, CardKind.Hero);
            if (sprite == null)
            {
                var missing = UiFactory.Label("UnityHeroBadgeImageMissing", frame.transform, T("无图", "No Art"), 14, FontStyle.Bold);
                missing.alignment = TextAnchor.MiddleCenter;
                missing.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.Stretch(missing.rectTransform);
                return;
            }

            var spriteObject = new GameObject("UnityHeroBadgeImageSprite", typeof(RectTransform), typeof(Image));
            spriteObject.transform.SetParent(frame.transform, false);
            UnityTavernUiStyle.Stretch(spriteObject.GetComponent<RectTransform>());
            var image = spriteObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void BuildPlaySurface()
        {
            var layout = LayoutContext();
            var horizontalInset = layout.IsCompact ? UnityTavernUiStyle.SpacingSm : UnityTavernUiStyle.SpacingLg;
            var bottomInset = layout.IsCompact ? 76f : 62f;
            var guideActive = strategyGuideSession != null && strategyGuideSession.RunState != StrategyGuideRunState.FreeExplore;
            var topInset = guideActive ? (layout.IsCompact ? 218f : 214f) : (layout.IsCompact ? 146f : 152f);

            var surface = Panel("UnityPlaySurface", transform, Color.clear);
            var rect = surface.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(horizontalInset, bottomInset);
            rect.offsetMax = new Vector2(-horizontalInset, -topInset);

            var surfaceLayout = surface.AddComponent<HorizontalLayoutGroup>();
            surfaceLayout.spacing = layout.IsCompact ? UnityTavernUiStyle.SpacingSm : 14f;
            surfaceLayout.childControlWidth = true;
            surfaceLayout.childControlHeight = true;
            surfaceLayout.childForceExpandWidth = true;
            surfaceLayout.childForceExpandHeight = true;

            var center = Panel("UnityTableColumn", surface.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(center, 1f, 1f);
            var centerLayout = center.AddComponent<VerticalLayoutGroup>();
            centerLayout.spacing = layout.ZoneStackSpacing;
            centerLayout.childControlWidth = true;
            centerLayout.childControlHeight = true;
            centerLayout.childForceExpandWidth = true;
            centerLayout.childForceExpandHeight = false;

            var mainTable = Panel(
                "UnityMainTable",
                center.transform,
                new Color(UnityTavernUiStyle.TableDark.r, UnityTavernUiStyle.TableDark.g, UnityTavernUiStyle.TableDark.b, 0.36f));
            UnityTavernUiStyle.SetFlexible(mainTable, 1f, 1f);
            var mainTableLayout = mainTable.AddComponent<VerticalLayoutGroup>();
            mainTableLayout.padding = layout.IsCompact
                ? new RectOffset(4, 4, 4, 4)
                : new RectOffset(8, 8, 6, 8);
            mainTableLayout.spacing = layout.ZoneStackSpacing;
            mainTableLayout.childControlWidth = true;
            mainTableLayout.childControlHeight = true;
            mainTableLayout.childForceExpandWidth = true;
            mainTableLayout.childForceExpandHeight = false;

            BuildOpponentEntry(mainTable.transform, layout);
            BuildShop(mainTable.transform, layout);
            BuildPlayerBoard(mainTable.transform, layout);
            BuildHand(center.transform, layout);

        }

        private void BuildQuickActionBar()
        {
            var layout = LayoutContext();
            var bar = Panel(
                "UnityQuickActionBar",
                transform,
                new Color(UnityTavernUiStyle.SurfaceDark.r, UnityTavernUiStyle.SurfaceDark.g, UnityTavernUiStyle.SurfaceDark.b, 0.96f));
            var rect = bar.GetComponent<RectTransform>();
            var bottom = layout.IsCompact ? UnityTavernUiStyle.SpacingSm : 12f;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(layout.IsCompact ? UnityTavernUiStyle.SpacingSm : UnityTavernUiStyle.SpacingLg, bottom);
            rect.offsetMax = new Vector2(layout.IsCompact ? -UnityTavernUiStyle.SpacingSm : -UnityTavernUiStyle.SpacingLg, bottom + QuickActionBarHeight(layout));

            var image = bar.GetComponent<Image>();
            image.raycastTarget = true;

            var group = bar.AddComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(
                Mathf.RoundToInt(UnityTavernUiStyle.SpacingSm),
                Mathf.RoundToInt(UnityTavernUiStyle.SpacingSm),
                Mathf.RoundToInt(UnityTavernUiStyle.SpacingSm),
                Mathf.RoundToInt(UnityTavernUiStyle.SpacingSm));
            group.spacing = layout.IsCompact ? UnityTavernUiStyle.SpacingSm : UnityTavernUiStyle.SpacingMd;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            BuildActionButtons(bar.transform, "UnityQuick", true, layout);
        }

        private void BuildTavernActionBar()
        {
            var layout = LayoutContext();
            var bar = Panel(
                "UnityTavernActionBar",
                transform,
                new Color(UnityTavernUiStyle.SurfaceDark.r, UnityTavernUiStyle.SurfaceDark.g, UnityTavernUiStyle.SurfaceDark.b, 0.96f));
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            var guideActive = strategyGuideSession != null && strategyGuideSession.RunState != StrategyGuideRunState.FreeExplore;
            var top = guideActive ? (layout.IsCompact ? -158f : -146f) : -86f;
            var height = layout.IsCompact ? UnityTavernUiStyle.CompactTouchHeight : 56f;
            rect.offsetMin = new Vector2(layout.IsCompact ? 8f : 18f, top - height);
            rect.offsetMax = new Vector2(layout.IsCompact ? -8f : -18f, top);
            bar.GetComponent<Image>().raycastTarget = true;
            UnityTavernUiStyle.ConfigureOutline(
                bar,
                new Color(UnityTavernUiStyle.Brass.r, UnityTavernUiStyle.Brass.g, UnityTavernUiStyle.Brass.b, 0.52f),
                new Vector2(1f, -1f));

            var group = bar.AddComponent<HorizontalLayoutGroup>();
            group.padding = layout.IsCompact ? new RectOffset(8, 8, 0, 0) : new RectOffset(8, 8, 6, 6);
            group.spacing = 8f;
            group.childAlignment = TextAnchor.MiddleRight;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            BuildTavernActionButtons(bar.transform, layout);
        }

        private void BuildTavernActionButtons(Transform parent, UnityTavernLayoutContext layout)
        {
            var height = layout.IsCompact ? UnityTavernUiStyle.CompactTouchHeight : UnityTavernUiStyle.TouchHeight;
            var minWidth = layout.IsCompact ? 64f : 88f;
            BuildSelectedRecruitActionButtons(parent, "UnityQuick", minWidth, height, false);
            var guideRestricted = strategyGuideSession != null && strategyGuideSession.RunState != StrategyGuideRunState.FreeExplore;
            var canRefresh = CanRefreshShop();
            var canFreeze = CanExecute(GameCommandType.FreezeShop);
            var canUpgrade = CanUpgradeTavern();
            ActionButton("UnityQuickRefreshButton", parent, RecruitActionLabel(RefreshActionLabel(), GameCommandType.RerollShop, canRefresh), () => Apply(new GameCommand(GameCommandType.RerollShop)), minWidth, height, false, UnityTavernActionButtonRole.Economy, canRefresh);
            ActionButton("UnityQuickFreezeButton", parent, RecruitActionLabel(service.State.Player.Tavern.Frozen ? T("解冻", "Unfreeze") : T("冻结", "Freeze"), GameCommandType.FreezeShop, canFreeze), () => Apply(new GameCommand(GameCommandType.FreezeShop, !service.State.Player.Tavern.Frozen)), minWidth, height, false, UnityTavernActionButtonRole.Economy, canFreeze);
            ActionButton("UnityQuickUpgradeButton", parent, RecruitActionLabel(UpgradeActionLabel(), GameCommandType.UpgradeTavern, canUpgrade), () => Apply(new GameCommand(GameCommandType.UpgradeTavern)), minWidth, height, false, UnityTavernActionButtonRole.Economy, canUpgrade);
            var advanceFromResult = service.State.Phase == MatchPhase.Result;
            var turnCommand = advanceFromResult ? GameCommandType.DebugSkipToNextTurn : GameCommandType.BeginNextTurnTransition;
            ActionButton("UnityQuickNextTurnButton", parent, NextTurnActionLabel(advanceFromResult, T("完整下一回合", "Complete Next Turn")), () =>
            {
                if (advanceFromResult) Apply(new GameCommand(turnCommand));
                else ApplyAndOpenReplay(new GameCommand(turnCommand));
            }, minWidth + 18f, height, false, UnityTavernActionButtonRole.Combat, CanExecute(turnCommand));
            ActionButton("UnityQuickReplayButton", parent, ReplayActionLabel(), OpenCombatReplay, minWidth, height, false, UnityTavernActionButtonRole.Utility, HasCombatReplay());
            if (!guideRestricted)
            {
                ActionButton("UnityQuickToolsButton", parent, T("工具", "Tools"), OpenTools, minWidth, height, false, UnityTavernActionButtonRole.Utility);
            }
        }

        private string RecruitActionLabel(string label, GameCommandType commandType, bool available)
        {
            if (available || strategyGuideSession == null ||
                strategyGuideSession.RunState == StrategyGuideRunState.FreeExplore ||
                strategyGuideSession.Profile.AllowedCommands?.Contains(commandType.ToString()) == true)
            {
                return label;
            }

            return label + T("（模式锁定）", " (Locked)");
        }

        private void BuildHeroEffectRack()
        {
            var layout = LayoutContext();
            var rack = Panel("UnityHeroEffectRack", transform, Color.clear);
            var rect = rack.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(layout.IsCompact ? 8f : 18f, layout.IsCompact ? 8f : 12f);
            rect.offsetMax = new Vector2(layout.IsCompact ? -8f : -18f, layout.IsCompact ? 68f : 76f);

            var row = rack.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            foreach (var item in CurrentEffectDisplayItems().OrderBy(item => item.SortOrder))
            {
                BuildEffectItem(rack.transform, item, layout);
            }
        }

        private IReadOnlyList<EffectDisplayItem> CurrentEffectDisplayItems()
        {
            var items = new List<EffectDisplayItem>();
            var powers = CurrentHeroPowers();
            for (var index = 0; index < powers.Count; index += 1)
            {
                var power = powers[index];
                if (power == null || !IsHeroPowerUnlocked(power)) continue;
                var captured = power;
                items.Add(new EffectDisplayItem
                {
                    Id = "HeroPower-" + power.CardId,
                    ObjectName = index == 0 ? "UnityQuickHeroPowerButton" : "UnityQuickHeroPowerButton" + index,
                    Type = T("英雄技能", "Hero Power"),
                    Name = power.Name,
                    Description = power.Text,
                    Source = CurrentHero()?.Name ?? T("当前英雄", "Current Hero"),
                    Status = service.CanUseHeroPower(power.CardId) ? T("本回合可用", "Ready This Turn") : T("本回合不可用", "Unavailable This Turn"),
                    Badge = Math.Max(0, power.Cost).ToString(),
                    CardId = power.CardId,
                    ImagePath = power.ImagePath,
                    CardKind = CardKind.HeroPower,
                    SortOrder = index,
                    Accent = UnityTavernUiStyle.Green,
                    OnClick = () => BeginHeroPowerTargeting(captured),
                    HeroPower = power,
                    Interactable = CanExecute(GameCommandType.UseHeroPower) && service.CanUseHeroPower(power.CardId)
                });
            }

            var secrets = service.State.Player.Tavern.Secrets;
            if (secrets != null)
            {
                var activeSecretIndex = 0;
                foreach (var secret in secrets)
                {
                    if (secret == null || secret.Triggered || string.IsNullOrWhiteSpace(secret.SecretCardId)) continue;
                    AddSecretEffect(items, secret, activeSecretIndex);
                    activeSecretIndex += 1;
                }
            }

            var quests = service.State.Player.Tavern.AdvancedMechanics?.Quests;
            AddQuestEffect(items, quests?.MainQuest, "Main", 20);
            AddQuestEffect(items, quests?.BonusQuest, "Bonus", 21);

            var trinkets = service.State.Player.Tavern.AdvancedMechanics?.Trinkets;
            AddTrinketEffect(items, TrinketSlotKind.Lesser, ResolveTrinketDefinition(trinkets?.LesserTrinketId), 30);
            AddTrinketEffect(items, TrinketSlotKind.Greater, ResolveTrinketDefinition(trinkets?.GreaterTrinketId), 31);

            var anomaly = service.State.Player.Tavern.AdvancedMechanics?.Anomalies;
            if (anomaly != null && anomaly.Enabled && !string.IsNullOrWhiteSpace(anomaly.ActiveName))
            {
                items.Add(new EffectDisplayItem
                {
                    Id = "Anomaly-" + anomaly.ActiveAnomalyId,
                    Type = T("畸变", "Anomaly"),
                    Name = anomaly.ActiveName,
                    Description = anomaly.ActiveText,
                    Source = T("本局规则", "Match Rule"),
                    Status = T("持续生效", "Active"),
                    CardId = anomaly.ActiveCardId,
                    CardKind = CardKind.Quest,
                    SortOrder = 40,
                    Accent = UnityTavernUiStyle.Red
                });
            }

            AddNormalDarkGiftEntry(items);
            AddDarkGiftEffects(items, LayoutContext().IsCompact);
            AddLockboxEffects(items);

            return items;
        }

        private void AddNormalDarkGiftEntry(List<EffectDisplayItem> items)
        {
            var profile = service.DarkGiftProfile;
            if (profile?.Enabled != true)
            {
                return;
            }

            var unavailableReason = service.GetNormalDarkGiftUnavailableReason();
            var remaining = service.NormalDarkGiftUsesRemaining();
            var available = string.IsNullOrWhiteSpace(unavailableReason) && CanExecute(GameCommandType.UseNormalDarkGift);
            items.Add(new EffectDisplayItem
            {
                Id = "DarkGift-NormalEntry",
                ObjectName = "UnityDarkGiftButton",
                Type = NormalDarkGiftButtonType(profile, remaining),
                Name = T("黑暗之赐", "Dark Gift"),
                Description = T(
                    "支付 " + profile.GoldCost + " 枚铸币，三选一获得带有黑暗之赐的随从。" + NormalDarkGiftTierText(profile),
                    "Pay " + profile.GoldCost + " Gold to choose one of three minions carrying a Dark Gift. " + NormalDarkGiftTierText(profile)),
                Source = T("36.2 版本规则", "36.2 Version Rule"),
                Status = (available ? T("本回合可用", "Ready this turn") : unavailableReason) +
                         NormalDarkGiftUnlockCountdown(profile) +
                         T(" · 候选等级 ", " · Offer tier ") + NormalDarkGiftTierValue(profile) +
                         T(" · 本局剩余 ", " · Match uses ") + remaining + "/" + profile.UsesPerGame,
                Badge = Math.Max(0, profile.GoldCost).ToString(),
                CardKind = CardKind.Trinket,
                SortOrder = -10,
                Accent = available ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.MutedText,
                OnClick = () => Apply(new GameCommand(GameCommandType.UseNormalDarkGift)),
                Interactable = available
            });
        }

        private string NormalDarkGiftTierText(DarkGiftProfile profile)
        {
            var effectiveRound = Math.Max(profile.NormalEntryStartRound, service.State.Round);
            var range = (profile.TierRanges ?? new List<DarkGiftTierRangeRule>())
                .Where(item => item != null && item.FromRound <= effectiveRound)
                .OrderByDescending(item => item.FromRound)
                .FirstOrDefault();
            if (range == null)
            {
                return string.Empty;
            }

            var tier = TierValue(range);
            return T(" 当前候选等级：", " Current offer tier: ") + tier + T(" 级。", ".");
        }

        private string NormalDarkGiftTierValue(DarkGiftProfile profile)
        {
            var effectiveRound = Math.Max(profile.NormalEntryStartRound, service.State.Round);
            var range = (profile.TierRanges ?? new List<DarkGiftTierRangeRule>())
                .Where(item => item != null && item.FromRound <= effectiveRound)
                .OrderByDescending(item => item.FromRound)
                .FirstOrDefault();
            return range == null ? "-" : TierValue(range);
        }

        private string NormalDarkGiftUnlockCountdown(DarkGiftProfile profile)
        {
            var rounds = Math.Max(0, profile.NormalEntryStartRound - service.State.Round);
            return rounds <= 0
                ? string.Empty
                : T(" · 距离解锁 " + rounds + " 回合", " · Unlocks in " + rounds + (rounds == 1 ? " round" : " rounds"));
        }

        private string NormalDarkGiftButtonType(DarkGiftProfile profile, int remaining)
        {
            var rounds = Math.Max(0, profile.NormalEntryStartRound - service.State.Round);
            var firstLine = rounds > 0
                ? T("黑赐·" + rounds + "回合", "Gift · " + rounds + "r")
                : T("黑暗之赐", "Dark Gift");
            return firstLine + "\n" + NormalDarkGiftTierValue(profile) + "★ · " + remaining + "/" + profile.UsesPerGame;
        }

        private static string TierValue(DarkGiftTierRangeRule range)
        {
            return range.MinTier == range.MaxTier
                ? range.MinTier.ToString()
                : range.MinTier + "–" + range.MaxTier;
        }

        private void AddSecretEffect(List<EffectDisplayItem> items, SecretState secret, int index)
        {
            if (secret == null || secret.Triggered || string.IsNullOrWhiteSpace(secret.SecretCardId)) return;
            var better = secret.Better;
            items.Add(new EffectDisplayItem
            {
                Id = "Secret-" + secret.SecretCardId,
                ObjectName = "UnityHeroEffectSecret-" + secret.SecretCardId,
                Type = better ? T("强化奥秘", "Better Secret") : T("奥秘", "Secret"),
                Name = !service.UseEnglish && !string.IsNullOrWhiteSpace(secret.ZhName) ? secret.ZhName : secret.Name,
                Description = !service.UseEnglish && !string.IsNullOrWhiteSpace(secret.ZhText) ? secret.ZhText : secret.Text,
                Source = better ? T("街头魔术师", "Street Magician") : T("神奇魔术", "Prestidigitation"),
                Status = T("等待触发", "Armed"),
                CardId = secret.SecretCardId,
                CardKind = CardKind.Spell,
                SortOrder = 10 + index,
                Accent = better ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Blue
            });
        }

        private void AddQuestEffect(List<EffectDisplayItem> items, ActiveQuestState quest, string slot, int sortOrder)
        {
            if (quest == null) return;
            var rewardActive = quest.RewardActive;
            items.Add(new EffectDisplayItem
            {
                Id = "Quest-" + slot,
                ObjectName = "UnityHeroEffectQuest-" + slot,
                Type = rewardActive ? T("任务奖励", "Quest Reward") : T("任务", "Quest"),
                Name = rewardActive ? quest.RewardName : quest.QuestName,
                Description = rewardActive ? quest.RewardText : quest.QuestText,
                Source = string.IsNullOrWhiteSpace(quest.Source) ? T("本局任务", "Match Quest") : quest.Source,
                Status = rewardActive ? T("奖励已生效", "Reward Active") : quest.Completed ? T("已完成", "Completed") : T("进行中", "In Progress"),
                Badge = rewardActive || quest.RequiredAmount <= 0 ? string.Empty : quest.Progress + "/" + quest.RequiredAmount,
                CardId = rewardActive ? quest.RewardCardId : quest.QuestCardId,
                ImagePath = rewardActive ? quest.RewardImagePath : quest.QuestImagePath,
                CardKind = rewardActive ? CardKind.QuestReward : CardKind.Quest,
                SortOrder = sortOrder,
                Accent = rewardActive || quest.Completed ? UnityTavernUiStyle.Green : UnityTavernUiStyle.Blue
            });
        }

        private void AddTrinketEffect(List<EffectDisplayItem> items, TrinketSlotKind slot, TrinketDefinition definition, int sortOrder)
        {
            if (definition == null) return;
            items.Add(new EffectDisplayItem
            {
                Id = "Trinket-" + slot,
                ObjectName = "UnityHeroEffectTrinket-" + slot,
                Type = slot == TrinketSlotKind.Greater ? T("大饰品", "Greater Trinket") : T("小饰品", "Lesser Trinket"),
                Name = definition.Name,
                Description = definition.Text,
                Source = T("已装备饰品", "Equipped Trinket"),
                Status = T("持续生效", "Active"),
                CardId = definition.CardId,
                ImagePath = definition.ImagePath,
                CardKind = CardKind.Trinket,
                SortOrder = sortOrder,
                Accent = UnityTavernUiStyle.Gold
            });
        }

        private void AddDarkGiftEffects(List<EffectDisplayItem> items, bool compact)
        {
            var active = (service.State.PlayerDarkGifts?.AcquiredGiftInstances ?? new List<PlayerDarkGiftInstance>())
                .Where(instance => instance != null && instance.Active && !instance.Expired)
                .OrderBy(instance => instance.AcquiredRound)
                .ThenBy(instance => instance.DefinitionRevisionId, StringComparer.Ordinal)
                .ToList();
            if (active.Count == 0)
            {
                return;
            }

            if (compact)
            {
                items.Add(new EffectDisplayItem
                {
                    Id = "DarkGift-Aggregate",
                    ObjectName = "UnityDarkGiftEffectAggregate",
                    Type = T("黑暗之赐 ×" + active.Count, "Dark Gifts ×" + active.Count),
                    Name = T("当前黑暗之赐", "Current Dark Gifts"),
                    Description = T("打开查看当前效果与触发时间线。", "Open current effects and trigger timeline."),
                    Source = T("本局机制", "Match Mechanic"),
                    Status = T(active.Count + " 项正在生效", active.Count + " active"),
                    Badge = active.Count.ToString(),
                    CardKind = CardKind.Trinket,
                    SortOrder = 50,
                    Accent = UnityTavernUiStyle.Gold,
                    OnClick = OpenDarkGiftDetails
                });
                return;
            }

            var visibleCount = Math.Min(3, active.Count);
            for (var index = 0; index < visibleCount; index += 1)
            {
                var instance = active[index];
                var definition = ResolveDarkGiftDefinition(instance.DefinitionRevisionId);
                items.Add(new EffectDisplayItem
                {
                    Id = "DarkGift-" + index,
                    ObjectName = "UnityDarkGiftEffect-" + index,
                    Type = T("黑暗之赐", "Dark Gift"),
                    Name = definition?.DisplayName ?? instance.DefinitionRevisionId,
                    Description = definition?.Text ?? string.Empty,
                    Source = string.IsNullOrWhiteSpace(instance.Source) ? T("未知来源", "Unknown Source") : instance.Source,
                    Status = DarkGiftInstanceStatus(instance),
                    Badge = instance.StackCount > 1 ? "×" + instance.StackCount : string.Empty,
                    CardId = definition?.Id,
                    ImagePath = definition?.ImagePath,
                    CardKind = CardKind.Trinket,
                    SortOrder = 50 + index,
                    Accent = instance.Suppressed ? UnityTavernUiStyle.MutedText : UnityTavernUiStyle.Gold,
                    OnClick = OpenDarkGiftDetails
                });
            }

            if (active.Count > visibleCount)
            {
                var overflow = active.Count - visibleCount;
                items.Add(new EffectDisplayItem
                {
                    Id = "DarkGift-Overflow",
                    ObjectName = "UnityDarkGiftEffectOverflow",
                    Type = "+" + overflow,
                    Name = T("更多黑暗之赐", "More Dark Gifts"),
                    Description = T("打开查看全部当前效果与触发时间线。", "Open all current effects and trigger timeline."),
                    Source = T("本局机制", "Match Mechanic"),
                    Status = T("另有 " + overflow + " 项", overflow + " more"),
                    Badge = overflow.ToString(),
                    CardKind = CardKind.Trinket,
                    SortOrder = 59,
                    Accent = UnityTavernUiStyle.Gold,
                    OnClick = OpenDarkGiftDetails
                });
            }
        }

        private void AddLockboxEffects(List<EffectDisplayItem> items)
        {
            var lockboxes = (service.State.DelayedObjectStates ?? new List<DelayedObjectState>())
                .Where(item => item != null && !item.Opened &&
                               (!string.IsNullOrWhiteSpace(item.OpenResolverId) &&
                                item.OpenResolverId.IndexOf("lockbox", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                !string.IsNullOrWhiteSpace(item.DefinitionRevisionId) &&
                                item.DefinitionRevisionId.IndexOf("ROGUE_BG36_520t", StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(item => item.CreatedRound)
                .ToList();
            for (var index = 0; index < lockboxes.Count; index += 1)
            {
                var lockbox = lockboxes[index];
                var acceleration = LastLockboxAcceleration(lockbox.InstanceId);
                items.Add(new EffectDisplayItem
                {
                    Id = "Lockbox-" + lockbox.InstanceId,
                    ObjectName = index == 0 ? "UnityLockboxEffect" : "UnityLockboxEffect-" + index,
                    Type = T("锁盒", "Lockbox"),
                    Name = T("共享锁盒", "Shared Lockbox"),
                    Description = T("倒计时结束时开启。来源：", "Opens when its countdown ends. Source: ") + lockbox.Source,
                    Source = lockbox.Source,
                    Status = T("剩余 " + lockbox.RemainingTurns + " 回合", lockbox.RemainingTurns + " turns remaining") +
                             (acceleration == null
                                 ? T(" · 尚未加速", " · Not accelerated")
                                 : T(" · 上次加速：第 " + acceleration.Round + " 回合", " · Last accelerated: Round " + acceleration.Round)),
                    Badge = Math.Max(0, lockbox.RemainingTurns).ToString(),
                    CardKind = CardKind.Trinket,
                    SortOrder = 60 + index,
                    Accent = UnityTavernUiStyle.ArcaneBlue
                });
            }
        }

        private DarkGiftDefinition ResolveDarkGiftDefinition(string revisionId)
        {
            return service.Catalogs?.DarkGifts?.All?.FirstOrDefault(item =>
                item != null && string.Equals(item.RevisionId, revisionId, StringComparison.Ordinal));
        }

        private string DarkGiftInstanceStatus(PlayerDarkGiftInstance instance)
        {
            var state = instance.Suppressed
                ? T("已压制", "Suppressed")
                : instance.Active
                    ? T("生效中", "Active")
                    : T("未生效", "Inactive");
            return T("第 ", "Round ") + instance.AcquiredRound +
                   T(" 回合获得", " acquired") +
                   T(" · 剩余次数 ", " · Uses ") + instance.RemainingUses +
                   T(" · 冷却 ", " · Cooldown ") + instance.Cooldown +
                   T(" · 叠加 ", " · Stacks ") + Math.Max(1, instance.StackCount) +
                   " · " + state;
        }

        private MechanicEventRecord LastLockboxAcceleration(string instanceId)
        {
            return (service.State.MechanicEvents ?? new List<MechanicEventRecord>())
                .Where(item => item != null &&
                               !string.IsNullOrWhiteSpace(item.Type) &&
                               item.Type.IndexOf("lockbox", StringComparison.OrdinalIgnoreCase) >= 0 &&
                               item.Type.IndexOf("accelerat", StringComparison.OrdinalIgnoreCase) >= 0 &&
                               ((item.Targets ?? new List<string>()).Any(target =>
                                    string.Equals(target, instanceId, StringComparison.Ordinal)) ||
                                string.Equals(item.Source, instanceId, StringComparison.Ordinal)))
                .OrderByDescending(item => item.Sequence)
                .FirstOrDefault();
        }

        private void BuildEffectItem(Transform parent, EffectDisplayItem item, UnityTavernLayoutContext layout)
        {
            var root = Panel(string.IsNullOrWhiteSpace(item.ObjectName) ? "UnityHeroEffect-" + SafeObjectName(item.Id) : item.ObjectName, parent, new Color(UnityTavernUiStyle.PanelQuiet.r, UnityTavernUiStyle.PanelQuiet.g, UnityTavernUiStyle.PanelQuiet.b, 0.96f));
            var isNormalDarkGift = string.Equals(item.Id, "DarkGift-NormalEntry", StringComparison.Ordinal);
            UnityTavernUiStyle.SetFixedSize(
                root,
                isNormalDarkGift ? (layout.IsCompact ? 88f : 100f) : (layout.IsCompact ? 72f : 84f),
                layout.IsCompact ? 52f : 60f);
            UnityTavernUiStyle.ConfigureOutline(root, new Color(item.Accent.r, item.Accent.g, item.Accent.b, 0.72f), new Vector2(1f, -1f));
            var image = root.GetComponent<Image>();
            var sprite = CardImageProvider.LoadSprite(item.ImagePath, item.CardId, item.CardKind);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            root.GetComponent<Image>().raycastTarget = true;

            var type = UiFactory.Label("UnityHeroEffectType-" + SafeObjectName(item.Id), root.transform, item.Type, 14, FontStyle.Bold);
            type.color = item.Accent;
            type.alignment = TextAnchor.LowerCenter;
            type.rectTransform.anchorMin = new Vector2(0f, 0f);
            type.rectTransform.anchorMax = new Vector2(1f, 0f);
            type.rectTransform.offsetMin = new Vector2(3f, 2f);
            type.rectTransform.offsetMax = new Vector2(-3f, 18f);
            if (isNormalDarkGift)
            {
                type.fontSize = 13;
                type.alignment = TextAnchor.MiddleCenter;
                type.horizontalOverflow = HorizontalWrapMode.Wrap;
                type.verticalOverflow = VerticalWrapMode.Truncate;
                type.rectTransform.offsetMax = new Vector2(-3f, 38f);
            }
            if (!string.IsNullOrWhiteSpace(item.Badge))
            {
                var badge = UiFactory.Label("UnityHeroEffectBadge-" + SafeObjectName(item.Id), root.transform, item.Badge, 14, FontStyle.Bold);
                badge.color = Color.white;
                badge.alignment = TextAnchor.MiddleCenter;
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(1f, 1f);
                badge.rectTransform.pivot = new Vector2(1f, 1f);
                badge.rectTransform.sizeDelta = new Vector2(34f, 18f);
                badge.rectTransform.anchoredPosition = new Vector2(-2f, -2f);
            }

            var button = root.AddComponent<Button>();
            root.AddComponent<UnitySelectableFocusRing>();
            if (item.OnClick != null)
            {
                button.interactable = item.Interactable;
                button.onClick.AddListener(() => item.OnClick());
                if (item.HeroPower != null && service.CanUseHeroPower(item.HeroPower.CardId)) AddHeroPowerDrag(root, item.HeroPower);
            }
            else
            {
                button.onClick.AddListener(() => ShowEffectTooltip(root.GetComponent<RectTransform>(), item));
            }

            UnityTavernUiStyle.TintSelectable(button, Color.white, new Color(1f, 0.94f, 0.72f, 1f), new Color(0.72f, 0.72f, 0.72f, 1f));

            var trigger = root.AddComponent<EventTrigger>();
            AddEventTrigger(trigger, EventTriggerType.PointerEnter, _ => ShowEffectTooltip(root.GetComponent<RectTransform>(), item));
            AddEventTrigger(trigger, EventTriggerType.PointerExit, _ => HideEffectTooltip());
            AddEventTrigger(trigger, EventTriggerType.Select, _ => ShowEffectTooltip(root.GetComponent<RectTransform>(), item));
            AddEventTrigger(trigger, EventTriggerType.Deselect, _ => HideEffectTooltip());
        }

        private void ShowEffectTooltip(RectTransform anchor, EffectDisplayItem item)
        {
            HideEffectTooltip();
            keywordTooltip = Panel("UnityHeroEffectTooltip", transform, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.98f));
            keywordTooltip.transform.SetAsLastSibling();
            keywordTooltip.GetComponent<Image>().raycastTarget = false;
            UnityTavernUiStyle.ConfigureOutline(keywordTooltip, new Color(item.Accent.r, item.Accent.g, item.Accent.b, 0.72f), new Vector2(1f, -1f));
            var rect = keywordTooltip.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(320f, 184f);
            rect.anchoredPosition = EffectTooltipPosition(anchor, rect.sizeDelta);
            var layout = keywordTooltip.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var kindLabel = UiFactory.Label("UnityHeroEffectTooltipKind", keywordTooltip.transform, item.Type + (string.IsNullOrWhiteSpace(item.Badge) ? string.Empty : " · " + item.Badge), 14, FontStyle.Bold);
            kindLabel.color = item.Accent;
            UnityTavernUiStyle.SetPreferredHeight(kindLabel.gameObject, 20f);
            var titleLabel = UiFactory.Label("UnityHeroEffectTooltipTitle", keywordTooltip.transform, item.Name ?? string.Empty, 15, FontStyle.Bold);
            titleLabel.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetPreferredHeight(titleLabel.gameObject, 24f);
            AddEffectTooltipLine("UnityHeroEffectTooltipDescription", keywordTooltip.transform, item.Description, 70f, UnityTavernUiStyle.Text);
            AddEffectTooltipLine("UnityHeroEffectTooltipSource", keywordTooltip.transform, T("来源：", "Source: ") + item.Source, 22f, UnityTavernUiStyle.MutedText);
            AddEffectTooltipLine("UnityHeroEffectTooltipStatus", keywordTooltip.transform, T("状态：", "Status: ") + item.Status, 22f, item.Accent);
        }

        private static void AddEffectTooltipLine(string name, Transform parent, string text, float height, Color color)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var label = UiFactory.Label(name, parent, text, 14, FontStyle.Normal);
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, height);
        }

        private Vector2 EffectTooltipPosition(RectTransform anchor, Vector2 tooltipSize)
        {
            var root = transform as RectTransform;
            if (root == null || anchor == null) return new Vector2(16f, 80f);
            var local = root.InverseTransformPoint(anchor.position);
            var x = Mathf.Clamp(local.x - tooltipSize.x * 0.5f, root.rect.xMin + 8f, root.rect.xMax - tooltipSize.x - 8f);
            var y = Mathf.Clamp(local.y + 42f, root.rect.yMin + 8f, root.rect.yMax - tooltipSize.y - 8f);
            return new Vector2(x, y);
        }

        private void HideEffectTooltip()
        {
            if (keywordTooltip == null) return;
            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(keywordTooltip);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(keywordTooltip);
            }
            keywordTooltip = null;
        }

        private void OpenDarkGiftDetails()
        {
            darkGiftDetailsOpen = true;
            HideEffectTooltip();
            Rebuild();
        }

        private void CloseDarkGiftDetails()
        {
            darkGiftDetailsOpen = false;
            Rebuild();
        }

        private void BuildDarkGiftDetailsOverlay()
        {
            var overlay = Panel("UnityDarkGiftDetailsOverlay", transform, new Color(0f, 0f, 0f, 0.72f));
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().raycastTarget = true;
            overlay.transform.SetAsLastSibling();

            var layoutContext = LayoutContext();
            var panel = Panel("UnityDarkGiftDetailsPanel", overlay.transform, UnityTavernUiStyle.SurfaceDark);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.8f),
                new Vector2(2f, -2f));
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityDarkGiftDetailsStarLantern", UnityTavernUiStyle.Gold);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(
                Mathf.Min(layoutContext.Width - 24f, layoutContext.IsCompact ? 820f : 920f),
                Mathf.Min(layoutContext.Height - 24f, layoutContext.IsCompact ? 366f : 650f));
            rect.anchoredPosition = Vector2.zero;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(14, 14, 12, 12);
            panelLayout.spacing = 8f;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var title = UiFactory.Label(
                "UnityDarkGiftDetailsTitle",
                panel.transform,
                T("黑暗之赐详情", "Dark Gift Details"),
                20,
                FontStyle.Bold,
                layoutContext);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Gold;

            var content = UiFactory.ScrollView(
                "UnityDarkGiftDetailsScroll",
                panel.transform,
                UnityTavernUiStyle.PanelQuiet,
                out _,
                layoutContext);
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 10f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            BuildDarkGiftCurrentEffects(content, layoutContext);
            BuildDarkGiftTriggerTimeline(content, layoutContext);

            var close = UiFactory.Button(
                "UnityDarkGiftDetailsCloseButton",
                panel.transform,
                T("关闭", "Close"),
                CloseDarkGiftDetails,
                layoutContext);
            panel.AddComponent<UnityFocusTrap>().Activate(close.gameObject);
        }

        private void BuildDarkGiftCurrentEffects(Transform parent, UnityTavernLayoutContext layoutContext)
        {
            var section = Panel("UnityDarkGiftCurrentEffects", parent, UnityTavernUiStyle.Panel);
            var sectionLayout = section.AddComponent<VerticalLayoutGroup>();
            sectionLayout.padding = new RectOffset(10, 10, 8, 8);
            sectionLayout.spacing = 6f;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            var heading = UiFactory.Label(
                "UnityDarkGiftCurrentEffectsTitle",
                section.transform,
                T("当前生效", "Current Effects"),
                16,
                FontStyle.Bold,
                layoutContext);
            heading.color = UnityTavernUiStyle.Gold;

            var active = (service.State.PlayerDarkGifts?.AcquiredGiftInstances ?? new List<PlayerDarkGiftInstance>())
                .Where(instance => instance != null && instance.Active && !instance.Expired)
                .OrderBy(instance => instance.AcquiredRound)
                .ToList();
            if (active.Count == 0)
            {
                UiFactory.Label(
                    "UnityDarkGiftCurrentEffectsEmpty",
                    section.transform,
                    T("当前没有生效中的黑暗之赐。", "No Dark Gifts are currently active."),
                    14,
                    FontStyle.Normal,
                    layoutContext).color = UnityTavernUiStyle.MutedText;
                return;
            }

            for (var index = 0; index < active.Count; index += 1)
            {
                var instance = active[index];
                var definition = ResolveDarkGiftDefinition(instance.DefinitionRevisionId);
                var target = AllCards().FirstOrDefault(card =>
                    card != null && string.Equals(card.InstanceId, instance.InstanceId, StringComparison.Ordinal));
                var line = UiFactory.Label(
                    "UnityDarkGiftCurrentEffect-" + index,
                    section.transform,
                    (definition?.DisplayName ?? instance.DefinitionRevisionId) +
                    (target == null ? string.Empty : " · " + DisplayCardName(target)) + "\n" +
                    (definition?.Text ?? string.Empty) + "\n" +
                    T("来源：", "Source: ") + instance.Source +
                    T(" · 第 ", " · Round ") + instance.AcquiredRound + T(" 回合获得", " acquired") +
                    T(" · 剩余次数 ", " · Uses ") + instance.RemainingUses +
                    T(" · 冷却 ", " · Cooldown ") + instance.Cooldown +
                    T(" · 叠加 ", " · Stacks ") + Math.Max(1, instance.StackCount),
                    14,
                    FontStyle.Normal,
                    layoutContext);
                line.color = instance.Suppressed ? UnityTavernUiStyle.MutedText : UnityTavernUiStyle.Text;
                line.horizontalOverflow = HorizontalWrapMode.Wrap;
                line.verticalOverflow = VerticalWrapMode.Overflow;
                UnityTavernUiStyle.SetPreferredHeight(
                    line.gameObject,
                    layoutContext.CanvasUnitsForPhysicalPixels(layoutContext.IsCompact ? 74f : 62f));
            }
        }

        private void BuildDarkGiftTriggerTimeline(Transform parent, UnityTavernLayoutContext layoutContext)
        {
            var section = Panel("UnityDarkGiftTriggerTimeline", parent, UnityTavernUiStyle.Panel);
            var sectionLayout = section.AddComponent<VerticalLayoutGroup>();
            sectionLayout.padding = new RectOffset(10, 10, 8, 8);
            sectionLayout.spacing = 5f;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            var heading = UiFactory.Label(
                "UnityDarkGiftTriggerTimelineTitle",
                section.transform,
                T("触发时间线", "Trigger Timeline"),
                16,
                FontStyle.Bold,
                layoutContext);
            heading.color = UnityTavernUiStyle.ArcaneBlue;

            var events = (service.State.PlayerDarkGifts?.TriggerHistory?.Events ?? new List<MechanicEventRecord>())
                .Where(item => item != null)
                .OrderByDescending(item => item.Sequence)
                .ToList();
            if (events.Count == 0)
            {
                UiFactory.Label(
                    "UnityDarkGiftTriggerTimelineEmpty",
                    section.transform,
                    T("尚无触发记录。", "No trigger history yet."),
                    14,
                    FontStyle.Normal,
                    layoutContext).color = UnityTavernUiStyle.MutedText;
                return;
            }

            for (var index = 0; index < events.Count; index += 1)
            {
                var entry = events[index];
                var line = UiFactory.Label(
                    "UnityDarkGiftTriggerTimelineEntry-" + index,
                    section.transform,
                    T("第 ", "Round ") + entry.Round + T(" 回合", string.Empty) +
                    " · " + entry.Type +
                    (string.IsNullOrWhiteSpace(entry.Result) ? string.Empty : " · " + entry.Result) +
                    (string.IsNullOrWhiteSpace(entry.Source) ? string.Empty : T(" · 来源：", " · Source: ") + entry.Source),
                    14,
                    FontStyle.Normal,
                    layoutContext);
                line.color = UnityTavernUiStyle.Text;
                line.horizontalOverflow = HorizontalWrapMode.Wrap;
                line.verticalOverflow = VerticalWrapMode.Overflow;
                UnityTavernUiStyle.SetPreferredHeight(
                    line.gameObject,
                    layoutContext.CanvasUnitsForPhysicalPixels(34f));
            }
        }

        private static void AddEventTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => callback(data));
            trigger.triggers.Add(entry);
        }

        private void BuildOpponentEntry(Transform parent, UnityTavernLayoutContext layout)
        {
            var entry = Panel("UnityOpponentEntryPanel", parent, new Color(0.12f, 0.15f, 0.19f, 0.92f));
            UnityTavernUiStyle.SetPreferredHeight(entry, OpponentEntryShowsMechanicChips() ? (layout.IsCompact ? 74f : 80f) : (layout.IsCompact ? 48f : 56f));
            UnityTavernUiStyle.ConfigureOutline(
                entry,
                new Color(UnityTavernUiStyle.Blue.r, UnityTavernUiStyle.Blue.g, UnityTavernUiStyle.Blue.b, 0.42f),
                new Vector2(1.5f, -1.5f));

            var row = entry.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(14, 12, 8, 8);
            row.spacing = 12;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var summary = Panel("UnityOpponentEntrySummary", entry.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(summary, 1f, 0f);
            var stack = summary.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 1;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityOpponentEntryTitle", summary.transform, T("对手", "Opponent"), 14, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 20f);

            var meta = UiFactory.Label("UnityOpponentEntrySummaryText", summary.transform, OpponentSummaryText(), 11, FontStyle.Bold);
            meta.color = UnityTavernUiStyle.MutedText;
            meta.alignment = TextAnchor.MiddleLeft;
            meta.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(meta.gameObject, 17f);

            BuildOpponentEntryMechanicChips(summary.transform);

            ActionButton(
                "UnityOpponentEntryButton",
                entry.transform,
                T("查看对手", "View Opponent"),
                OpenOpponentPanel,
                layout.IsCompact ? 104f : 128f,
                layout.IsCompact ? 32f : 36f,
                false,
                UnityTavernActionButtonRole.Utility);
        }

        private bool OpponentEntryShowsMechanicChips()
        {
            return service.OpponentHeroPowerConfigurationEnabled ||
                service.OpponentQuestRewardConfigurationEnabled ||
                service.OpponentTrinketConfigurationEnabled;
        }

        private void BuildOpponentEntryMechanicChips(Transform parent)
        {
            if (!OpponentEntryShowsMechanicChips())
            {
                return;
            }

            var chips = Panel("UnityOpponentEntryMechanicChips", parent, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(chips, 24f);
            var layout = chips.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            if (service.OpponentHeroPowerConfigurationEnabled)
            {
                BuildOpponentEntryChip(
                    chips.transform,
                    "UnityOpponentEntryHeroPowerChip",
                T("技能:", "Power: ") + (OpponentHeroPowerConfigured() ? T("已配置", "Set") : T("未配置", "Not Set")),
                    OpponentHeroPowerConfigured() ? UnityTavernUiStyle.Green : UnityTavernUiStyle.PanelRaised);
            }

            if (service.OpponentQuestRewardConfigurationEnabled)
            {
                BuildOpponentEntryChip(
                    chips.transform,
                    "UnityOpponentEntryQuestRewardChip",
                T("任务：", "Quest: ") + (OpponentQuestRewardConfigured() ? T("已配置", "Set") : T("未配置", "Not Set")),
                    OpponentQuestRewardConfigured() ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.PanelRaised);
            }

            if (service.OpponentTrinketConfigurationEnabled)
            {
                BuildOpponentEntryChip(
                    chips.transform,
                    "UnityOpponentEntryLesserTrinketChip",
                T("小饰品：", "Lesser: ") + OpponentTrinketShortStatus(TrinketSlotKind.Lesser),
                    UnityTavernUiStyle.Gold);
                BuildOpponentEntryChip(
                    chips.transform,
                    "UnityOpponentEntryGreaterTrinketChip",
                T("大饰品：", "Greater: ") + OpponentTrinketShortStatus(TrinketSlotKind.Greater),
                    UnityTavernUiStyle.Gold);
            }
        }

        private static void BuildOpponentEntryChip(Transform parent, string name, string text, Color accent)
        {
            var chip = Panel(name, parent, new Color(0.08f, 0.10f, 0.12f, 0.86f));
            ConfigureInspectorSurface(chip, accent, 0.16f);
            UnityTavernUiStyle.SetFixedSize(chip, Mathf.Max(86f, text.Length * 9.5f), 22f);
            var label = UiFactory.Label(name + "Text", chip.transform, text, 10, FontStyle.Bold);
            label.color = UnityTavernUiStyle.Text;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(5f, 1f);
            label.rectTransform.offsetMax = new Vector2(-5f, -1f);
        }

        private string OpponentSummaryText()
        {
            var boardCount = service.State.Opponent.Board != null ? service.State.Opponent.Board.Count : 0;
            var handCount = service.State.Opponent.Hand != null ? service.State.Opponent.Hand.Count : 0;
            return T("战场 ", "Board ") + boardCount + "/7 · " + T("手牌 ", "Hand ") + handCount + "/10";
        }

        private bool OpponentQuestRewardConfigured()
        {
            return service.State.Opponent.AdvancedMechanics?.Quests?.MainQuest?.RewardActive == true;
        }

        private bool OpponentHeroPowerConfigured()
        {
            return !string.IsNullOrWhiteSpace(service.State.Opponent.HeroPowerCardId);
        }

        private bool OpponentTrinketConfigured(TrinketSlotKind slotKind)
        {
            var trinkets = service.State.Opponent.AdvancedMechanics?.Trinkets;
            var trinketId = slotKind == TrinketSlotKind.Greater ? trinkets?.GreaterTrinketId : trinkets?.LesserTrinketId;
            return !string.IsNullOrWhiteSpace(trinketId);
        }

        private string OpponentTrinketShortStatus(TrinketSlotKind slotKind)
        {
            if (!OpponentTrinketConfigured(slotKind))
            {
                return T("未配置", "Not Set");
            }

            return service.State.Round >= OpponentTrinketActiveRound(slotKind) ? T("已装备", "Equipped") : T("未生效", "Inactive");
        }

        private string OpponentTrinketName(TrinketSlotKind slotKind, TrinketDefinition definition)
        {
            if (definition != null)
            {
                return definition.Name;
            }

            var equipped = service.State.Opponent.AdvancedMechanics?.Trinkets?.Equipped?
                .FirstOrDefault(item => item != null && item.SlotKind == slotKind);
            return equipped == null ? T("未配置", "Not Set") : equipped.Name;
        }

        private string OpponentTrinketStatus(TrinketSlotKind slotKind, TrinketDefinition definition)
        {
            if (!OpponentTrinketConfigured(slotKind))
            {
                return T("未配置。用于模拟对手状态，不消耗玩家金币。", "Not set. This simulates opponent state and does not spend player Gold.");
            }

            var dueRound = OpponentTrinketActiveRound(slotKind);
            var status = service.State.Round >= dueRound ? T("已装备", "Equipped") : T("已预设，未生效", "Scheduled, Inactive");
            if (definition != null && definition.ImplementationStatus != TrinketImplementationStatus.Implemented)
            {
                status += " / " + BattlegroundsLocalizedText.TrinketImplementation(definition.ImplementationStatus, service.UseEnglish);
            }

            return status + T(" / 第 ", " / Active on Turn ") + dueRound + (service.UseEnglish ? string.Empty : " 回合生效");
        }

        private static int OpponentTrinketActiveRound(TrinketSlotKind slotKind)
        {
            return slotKind == TrinketSlotKind.Greater ? 9 : 6;
        }

        private void OpenOpponentPanel()
        {
            opponentPanelOpen = true;
            rightPanelOpen = false;
            toolsOpen = false;
            cardLibraryOpen = false;
            heroSelectionOpen = false;
            advancedCardLibraryOpen = false;
            Rebuild();
        }

        private void CloseOpponentPanel()
        {
            opponentPanelOpen = false;
            opponentMechanicLibraryOpen = false;
            Rebuild();
        }

        private void OpenOpponentMechanicLibrary(OpponentMechanicLibraryKind kind)
        {
            opponentMechanicLibraryKind = kind;
            opponentMechanicLibraryOpen = true;
            mechanicLibraryDetailItem = null;
            cardLibraryOpen = false;
            advancedCardLibraryOpen = false;
            toolsOpen = false;
            heroSelectionOpen = false;
            Rebuild();
        }

        private void DismissOpponentMechanicLibrary()
        {
            opponentMechanicLibraryOpen = false;
            mechanicLibraryDetailItem = null;
            Rebuild();
        }

        private void BuildOpponentPanelOverlay()
        {
            var overlay = Panel("UnityOpponentPanelOverlay", transform, new Color(0f, 0f, 0f, 0.68f));
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;
            overlay.transform.SetAsLastSibling();

            var layoutContext = LayoutContext();
            var panel = Panel("UnityOpponentPanel", overlay.transform, UnityTavernUiStyle.SurfaceDark);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.01f, 0.01f);
            rect.anchorMax = new Vector2(0.99f, 0.99f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.58f),
                new Vector2(2f, -2f));
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityOpponentPanelStarLantern", UnityTavernUiStyle.ArcaneBlue);
            var shadow = UnityTavernUiStyle.EnsureComponent<Shadow>(panel);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(6f, -6f);
            shadow.useGraphicAlpha = true;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(16, 16, 14, 16);
            panelLayout.spacing = 10;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var header = Panel("UnityOpponentPanelHeader", panel.transform, UnityTavernUiStyle.SurfaceRaised);
            UnityTavernUiStyle.ConfigureOutline(header, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.32f), new Vector2(1f, -1f));
            UnityTavernUiStyle.SetPreferredHeight(header, 60f);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(12, 8, 6, 6);
            headerLayout.spacing = 10;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            var titleStack = Panel("UnityOpponentPanelTitleStack", header.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(titleStack, 1f, 0f);
            var titleLayout = titleStack.AddComponent<VerticalLayoutGroup>();
            titleLayout.spacing = 0;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = true;
            titleLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityOpponentPanelTitle", titleStack.transform, T("敌方战场 · 编辑", "Opponent Board · Edit"), 18, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 22f);

            var summary = UiFactory.Label("UnityOpponentPanelSummary", titleStack.transform, OpponentSummaryText(), 14, FontStyle.Bold);
            summary.color = UnityTavernUiStyle.MutedText;
            summary.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, 16f);

            var close = ActionButton(
                "UnityOpponentPanelCloseButton",
                header.transform,
                "关闭",
                CloseOpponentPanel,
                80f,
                UnityTavernUiStyle.TouchHeight,
                false,
                UnityTavernActionButtonRole.Utility);
            close.GetComponentInChildren<Text>(true).fontSize = 14;

            ActionButton(
                "UnityOpponentStartOfCombatSpellButton",
                header.transform,
                T("战斗法术 ", "Combat Spells ") + (service.State.Opponent.NextCombatTavernSpellCardIds?.Count ?? 0) + "/6",
                OpenOpponentStartOfCombatSpellLibrary,
                layoutContext.IsCompact ? 108f : 148f,
                UnityTavernUiStyle.TouchHeight,
                false,
                UnityTavernActionButtonRole.Utility);

            BuildOpponentBoard(panel.transform, layoutContext);
            BuildOpponentSelectionActions(panel.transform, layoutContext);

            var body = UiFactory.ScrollView("UnityOpponentPanelScroll", panel.transform, UnityTavernUiStyle.SurfaceRaised, out _);
            UnityTavernUiStyle.SetFlexible(body.gameObject, 1f, 1f);
            var bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = layoutContext.ZoneStackSpacing;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = false;
            bodyLayout.childForceExpandHeight = false;

            BuildOpponentMechanicSection(body, layoutContext);
            BuildOpponentHand(body, layoutContext);
        }

        private void BuildOpponentSelectionActions(Transform parent, UnityTavernLayoutContext layout)
        {
            var selected = SelectedOpponentCard();
            var row = Panel("UnityOpponentSelectionActions", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 52f : 58f);
            var group = row.AddComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(10, 10, 4, 4);
            group.spacing = 8f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            var label = UiFactory.Label(
                "UnityOpponentSelectedCardLabel",
                row.transform,
                selected == null ? T("选择随从后可调整站位或删除", "Select a minion to move or remove it") :
                    T("已选：", "Selected: ") + selected.Name + "  " + selected.Attack + "/" + selected.Health,
                14,
                FontStyle.Bold);
            label.color = selected == null ? UnityTavernUiStyle.MutedText : UnityTavernUiStyle.Text;
            label.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetFlexible(label.gameObject, 1f, 0f);

            var selectedIndex = selected == null ? -1 : service.State.Opponent.Board.IndexOf(selected);
            ActionButton("UnityOpponentMoveLeftButton", row.transform, T("左移", "Left"), () => MoveSelectedOpponent(-1), 72f, UnityTavernUiStyle.TouchHeight, false, UnityTavernActionButtonRole.Utility, selectedIndex > 0);
            ActionButton("UnityOpponentMoveRightButton", row.transform, T("右移", "Right"), () => MoveSelectedOpponent(1), 72f, UnityTavernUiStyle.TouchHeight, false, UnityTavernActionButtonRole.Utility, selectedIndex >= 0 && selectedIndex < service.State.Opponent.Board.Count - 1);
            ActionButton("UnityOpponentRemoveSelectedButton", row.transform, T("删除", "Remove"), RemoveSelectedOpponent, 80f, UnityTavernUiStyle.TouchHeight, false, UnityTavernActionButtonRole.Danger, selected != null);
        }

        private void BuildOpponentMechanicSection(Transform parent, UnityTavernLayoutContext layoutContext)
        {
            if (!service.OpponentHeroPowerConfigurationEnabled &&
                !service.OpponentQuestRewardConfigurationEnabled &&
                !service.OpponentTrinketConfigurationEnabled)
            {
                return;
            }

            var section = Panel("UnityOpponentMechanicSection", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(section, UnityTavernUiStyle.Gold, 0.18f);
            UnityTavernUiStyle.SetPreferredHeight(section, OpponentMechanicSectionHeight(layoutContext));
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 9, 10);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityOpponentMechanicTitle", section.transform, "对手机制", 14, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 22f);

            if (service.OpponentHeroPowerConfigurationEnabled)
            {
                BuildOpponentHeroPowerSection(section.transform);
            }

            if (service.OpponentQuestRewardConfigurationEnabled)
            {
                BuildOpponentQuestRewardSection(section.transform);
            }

            if (service.OpponentTrinketConfigurationEnabled)
            {
                BuildOpponentTrinketSection(section.transform);
            }
        }

        private float OpponentMechanicSectionHeight(UnityTavernLayoutContext layoutContext)
        {
            var height = 44f;
            if (service.OpponentHeroPowerConfigurationEnabled)
            {
                height += OpponentHeroPowerSectionHeight(layoutContext);
            }

            if (service.OpponentQuestRewardConfigurationEnabled)
            {
                height += layoutContext.IsCompact ? 78f : 86f;
            }

            if (service.OpponentTrinketConfigurationEnabled)
            {
                height += 152f;
            }

            return height;
        }

        private float OpponentHeroPowerSectionHeight(UnityTavernLayoutContext layoutContext)
        {
            if (!OpponentHeroPowerConfigured())
            {
                return 138f;
            }

            return 252f;
        }

        private void BuildOpponentHeroPowerSection(Transform parent)
        {
            var power = service.GetOpponentHeroPowerDefinition();
            var configured = power != null && OpponentHeroPowerConfigured();
            var section = Panel("UnityOpponentHeroPowerSection", parent, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(section, UnityTavernUiStyle.Green, configured ? 0.22f : 0.12f);
            UnityTavernUiStyle.SetPreferredHeight(section, OpponentHeroPowerSectionHeight(LayoutContext()));
            var sectionLayout = section.AddComponent<VerticalLayoutGroup>();
            sectionLayout.padding = new RectOffset(8, 8, 6, 6);
            sectionLayout.spacing = 6;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            var row = Panel("UnityOpponentHeroPowerMainRow", section.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, 62f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var details = Panel("UnityOpponentHeroPowerDetails", row.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(details, 1f, 0f);
            var detailsLayout = details.AddComponent<VerticalLayoutGroup>();
            detailsLayout.spacing = 3;
            detailsLayout.childControlWidth = true;
            detailsLayout.childControlHeight = true;
            detailsLayout.childForceExpandWidth = true;
            detailsLayout.childForceExpandHeight = false;

            var name = UiFactory.Label("UnityOpponentHeroPowerName", details.transform, configured ? DisplayHeroPowerName(power) : "未配置", 14, FontStyle.Bold);
            name.color = configured ? UnityTavernUiStyle.Text : UnityTavernUiStyle.MutedText;
            name.alignment = TextAnchor.MiddleLeft;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 22f);

            var text = UiFactory.Label(
                "UnityOpponentHeroPowerText",
                details.transform,
                configured ? CleanCardText(DisplayHeroPowerText(power)) : "用于模拟对手战斗触发技能，不占用玩家选择流程。",
                14,
                FontStyle.Normal);
            text.color = UnityTavernUiStyle.MutedText;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, 32f);

            var select = ActionButton(
                "UnityOpponentHeroPowerSelectButton",
                row.transform,
                configured ? "更换" : "选择",
                () => OpenOpponentMechanicLibrary(OpponentMechanicLibraryKind.HeroPower),
                72f,
                44f,
                false,
                UnityTavernActionButtonRole.Primary);
            UnityTavernUiStyle.SetPreferredHeight(select.gameObject, 44f);
            select.GetComponentInChildren<Text>(true).fontSize = 14;

            var clear = ActionButton(
                "UnityOpponentHeroPowerClearButton",
                row.transform,
                configured ? "清除" : "未配置",
                () => Apply(new GameCommand(GameCommandType.ClearOpponentHeroPower)),
                72f,
                44f,
                false,
                UnityTavernActionButtonRole.Utility,
                configured);
            UnityTavernUiStyle.SetPreferredHeight(clear.gameObject, 44f);
            clear.GetComponentInChildren<Text>(true).fontSize = 14;

            BuildOpponentHeroPowerTargetRow(section.transform, configured);
            if (configured)
            {
                BuildOpponentHeroPowerTargetPickerRow(section.transform, BoardSide.Player);
                BuildOpponentHeroPowerTargetPickerRow(section.transform, BoardSide.Opponent);
            }
        }

        private void BuildOpponentHeroPowerTargetRow(Transform parent, bool configured)
        {
            var row = Panel("UnityOpponentHeroPowerTargetRow", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(row, UnityTavernUiStyle.Green, configured ? 0.12f : 0.06f);
            UnityTavernUiStyle.SetPreferredHeight(row, 56f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var label = UiFactory.Label("UnityOpponentHeroPowerTargetText", row.transform, OpponentHeroPowerTargetText(), 14, FontStyle.Bold);
            label.color = UnityTavernUiStyle.MutedText;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFlexible(label.gameObject, 1f, 0f);

            var playerLeft = ActionButton(
                "UnityOpponentHeroPowerTargetPlayerLeftButton",
                row.transform,
                service.State.Player.Board.Count > 0 ? "玩家最左" : "玩家战场为空",
                () => Apply(new GameCommand(GameCommandType.SetOpponentHeroPowerTarget, BoardSide.Player, 0)),
                104f,
                44f,
                false,
                UnityTavernActionButtonRole.Utility,
                configured && service.State.Player.Board.Count > 0);

            playerLeft.GetComponentInChildren<Text>(true).fontSize = 14;

            var opponentLeft = ActionButton(
                "UnityOpponentHeroPowerTargetOpponentLeftButton",
                row.transform,
                service.State.Opponent.Board.Count > 0 ? "对手最左" : "对手战场为空",
                () => Apply(new GameCommand(GameCommandType.SetOpponentHeroPowerTarget, BoardSide.Opponent, 0)),
                104f,
                44f,
                false,
                UnityTavernActionButtonRole.Utility,
                configured && service.State.Opponent.Board.Count > 0);

            opponentLeft.GetComponentInChildren<Text>(true).fontSize = 14;

            var clearTarget = ActionButton(
                "UnityOpponentHeroPowerTargetClearButton",
                row.transform,
                service.State.Opponent.HeroPowerTargetIndex >= 0 ? "清目标" : "暂无目标",
                () => Apply(new GameCommand(GameCommandType.ClearOpponentHeroPowerTarget)),
                82f,
                44f,
                false,
                UnityTavernActionButtonRole.Utility,
                configured && service.State.Opponent.HeroPowerTargetIndex >= 0);
            clearTarget.GetComponentInChildren<Text>(true).fontSize = 14;
        }

        private void BuildOpponentHeroPowerTargetPickerRow(Transform parent, BoardSide targetSide)
        {
            var board = targetSide == BoardSide.Opponent ? service.State.Opponent.Board : service.State.Player.Board;
            var rowName = targetSide == BoardSide.Opponent
                ? "UnityOpponentHeroPowerTargetOpponentBoardRow"
                : "UnityOpponentHeroPowerTargetPlayerBoardRow";
            var row = Panel(rowName, parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(row, UnityTavernUiStyle.Green, 0.10f);
            UnityTavernUiStyle.SetPreferredHeight(row, 52f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 2, 2);
            layout.spacing = 5;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var label = UiFactory.Label(rowName + "Label", row.transform, targetSide == BoardSide.Opponent ? "对手目标" : "玩家目标", 14, FontStyle.Bold);
            label.color = UnityTavernUiStyle.MutedText;
            label.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetFixedSize(label.gameObject, 72f, 44f);

            if (board.Count == 0)
            {
                var empty = UiFactory.Label(rowName + "Empty", row.transform, "无随从", 14, FontStyle.Normal);
                empty.color = UnityTavernUiStyle.MutedText;
                empty.alignment = TextAnchor.MiddleLeft;
                UnityTavernUiStyle.SetFlexible(empty.gameObject, 1f, 0f);
                return;
            }

            for (var index = 0; index < board.Count; index += 1)
            {
                var capturedIndex = index;
                var minion = board[index];
                var selected = IsOpponentHeroPowerTarget(targetSide, index, minion);
                var target = ActionButton(
                    "UnityOpponentHeroPowerTargetButton-" + targetSide + "-" + index,
                    row.transform,
                    (index + 1).ToString(),
                    () => Apply(new GameCommand(GameCommandType.SetOpponentHeroPowerTarget, targetSide, capturedIndex)),
                    44f,
                    44f,
                    false,
                    selected ? UnityTavernActionButtonRole.Primary : UnityTavernActionButtonRole.Utility);
                target.GetComponentInChildren<Text>(true).fontSize = 14;
            }
        }

        private bool IsOpponentHeroPowerTarget(BoardSide targetSide, int targetIndex, MinionInstance minion)
        {
            var opponent = service.State.Opponent;
            if (opponent == null || opponent.HeroPowerTargetSide != targetSide || opponent.HeroPowerTargetIndex < 0)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(opponent.HeroPowerTargetInstanceId) && minion != null)
            {
                return string.Equals(opponent.HeroPowerTargetInstanceId, minion.InstanceId, StringComparison.OrdinalIgnoreCase);
            }

            return opponent.HeroPowerTargetIndex == targetIndex;
        }

        private string OpponentHeroPowerTargetText()
        {
            var opponent = service.State.Opponent;
            if (opponent == null || opponent.HeroPowerTargetIndex < 0)
            {
                return "目标: 未设置";
            }

            var board = opponent.HeroPowerTargetSide == BoardSide.Opponent
                ? service.State.Opponent.Board
                : service.State.Player.Board;
            var sideLabel = opponent.HeroPowerTargetSide == BoardSide.Opponent ? "对手战场" : "玩家战场";
            var target = !string.IsNullOrEmpty(opponent.HeroPowerTargetInstanceId)
                ? board.FirstOrDefault(minion => string.Equals(minion.InstanceId, opponent.HeroPowerTargetInstanceId, StringComparison.OrdinalIgnoreCase))
                : null;
            if (target == null && opponent.HeroPowerTargetIndex >= 0 && opponent.HeroPowerTargetIndex < board.Count)
            {
                target = board[opponent.HeroPowerTargetIndex];
            }

            return "目标: " + sideLabel + " " + (target?.Name ?? "#" + opponent.HeroPowerTargetIndex);
        }

        private void BuildOpponentQuestRewardSection(Transform parent)
        {
            var reward = service.GetOpponentQuestRewardDefinition();
            var active = service.State.Opponent.AdvancedMechanics?.Quests?.MainQuest;
            var configured = active?.RewardActive == true;
            var row = Panel("UnityOpponentQuestRewardSection", parent, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(row, UnityTavernUiStyle.Blue, configured ? 0.22f : 0.12f);
            UnityTavernUiStyle.SetPreferredHeight(row, 76f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var details = Panel("UnityOpponentQuestRewardDetails", row.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(details, 1f, 0f);
            var detailsLayout = details.AddComponent<VerticalLayoutGroup>();
            detailsLayout.spacing = 3;
            detailsLayout.childControlWidth = true;
            detailsLayout.childControlHeight = true;
            detailsLayout.childForceExpandWidth = true;
            detailsLayout.childForceExpandHeight = false;

            var name = UiFactory.Label("UnityOpponentQuestRewardName", details.transform, configured ? active.RewardName : "未配置", 14, FontStyle.Bold);
            name.color = configured ? UnityTavernUiStyle.Text : UnityTavernUiStyle.MutedText;
            name.alignment = TextAnchor.MiddleLeft;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 22f);

            var text = UiFactory.Label(
                "UnityOpponentQuestRewardText",
                details.transform,
                configured ? CleanCardText(reward?.Text ?? active.RewardText) : "对手战斗模拟用任务奖励，不会占用玩家选择流程。",
                14,
                FontStyle.Normal);
            text.color = UnityTavernUiStyle.MutedText;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, 32f);

            var select = ActionButton(
                "UnityOpponentQuestRewardSelectButton",
                row.transform,
                configured ? "更换" : "选择",
                () => OpenOpponentMechanicLibrary(OpponentMechanicLibraryKind.QuestReward),
                76f,
                44f,
                false,
                UnityTavernActionButtonRole.Primary);
            UnityTavernUiStyle.SetPreferredHeight(select.gameObject, 44f);
            select.GetComponentInChildren<Text>(true).fontSize = 14;

            var clear = ActionButton(
                "UnityOpponentQuestRewardClearButton",
                row.transform,
                configured ? "清除" : "未配置",
                () => Apply(new GameCommand(GameCommandType.ClearOpponentQuestReward)),
                76f,
                44f,
                false,
                UnityTavernActionButtonRole.Utility,
                configured);
            UnityTavernUiStyle.SetPreferredHeight(clear.gameObject, 44f);
            clear.GetComponentInChildren<Text>(true).fontSize = 14;
        }

        private void BuildOpponentTrinketSection(Transform parent)
        {
            var section = Panel("UnityOpponentTrinketSection", parent, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(section, UnityTavernUiStyle.Gold, 0.16f);
            UnityTavernUiStyle.SetPreferredHeight(section, 152f);
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 8);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityOpponentTrinketTitle", section.transform, "对手饰品", 14, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 20f);

            BuildOpponentTrinketSlot(section.transform, TrinketSlotKind.Lesser);
            BuildOpponentTrinketSlot(section.transform, TrinketSlotKind.Greater);
        }

        private void BuildOpponentTrinketSlot(Transform parent, TrinketSlotKind slotKind)
        {
            var configured = OpponentTrinketConfigured(slotKind);
            var definition = service.GetOpponentTrinketDefinition(slotKind);
            var suffix = slotKind == TrinketSlotKind.Greater ? "Greater" : "Lesser";
            var row = Panel("UnityOpponentTrinketSlot-" + suffix, parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(row, UnityTavernUiStyle.Gold, configured ? 0.20f : 0.10f);
            UnityTavernUiStyle.SetPreferredHeight(row, 56f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var details = Panel("UnityOpponentTrinketDetails-" + suffix, row.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(details, 1f, 0f);
            var detailsLayout = details.AddComponent<VerticalLayoutGroup>();
            detailsLayout.spacing = 1;
            detailsLayout.childControlWidth = true;
            detailsLayout.childControlHeight = true;
            detailsLayout.childForceExpandWidth = true;
            detailsLayout.childForceExpandHeight = false;

            var name = UiFactory.Label("UnityOpponentTrinketName-" + suffix, details.transform, OpponentTrinketName(slotKind, definition), 14, FontStyle.Bold);
            name.color = configured ? UnityTavernUiStyle.Text : UnityTavernUiStyle.MutedText;
            name.alignment = TextAnchor.MiddleLeft;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 20f);

            var status = UiFactory.Label("UnityOpponentTrinketStatus-" + suffix, details.transform, OpponentTrinketStatus(slotKind, definition), 14, FontStyle.Normal);
            status.color = UnityTavernUiStyle.MutedText;
            status.alignment = TextAnchor.MiddleLeft;
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            status.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(status.gameObject, 17f);

            var select = ActionButton(
                "UnityOpponentTrinketSelectButton-" + suffix,
                row.transform,
                configured ? "更换" : "选择",
                () => OpenOpponentMechanicLibrary(slotKind == TrinketSlotKind.Greater ? OpponentMechanicLibraryKind.GreaterTrinket : OpponentMechanicLibraryKind.LesserTrinket),
                76f,
                44f,
                false,
                UnityTavernActionButtonRole.Primary);
            select.GetComponentInChildren<Text>(true).fontSize = 14;

            var clear = ActionButton(
                "UnityOpponentTrinketClearButton-" + suffix,
                row.transform,
                configured ? "清除" : "未配置",
                () => Apply(new GameCommand(GameCommandType.ClearOpponentTrinket, slotKind == TrinketSlotKind.Greater ? 1 : 0)),
                76f,
                44f,
                false,
                UnityTavernActionButtonRole.Utility,
                configured);
            clear.GetComponentInChildren<Text>(true).fontSize = 14;
        }

        private void BuildOpponentBoard(Transform parent, UnityTavernLayoutContext layout)
        {
            var zone = Zone("UnityOpponentBoardZone", parent, layout, UnityTavernZoneKind.OpponentBoard, UnityTavernCardMode.Board);
            BindOpponentBoardZone(zone, layout);
            BuildBoardReorderDropZone(
                zone.transform,
                "UnityOpponentBoardReorderDropZone",
                UnityTavernDropTarget.OpponentBoard,
                new Color(0.12f, 0.18f, 0.34f, 0.58f),
                "拖到这里调整敌方站位",
                "按左右位置决定落点");
        }

        private void BindOpponentBoardZone(UnityTavernZoneComponent zone, UnityTavernLayoutContext layout)
        {
            zone.Build(
                T("对手战场", "Opponent Board"),
                service.State.Opponent.Board.Count + "/7",
                service.State.Opponent.Board,
                BoardLimit,
                UnityTavernCardMode.Board,
                card => selectedInstanceId == card?.InstanceId ? T("已选择", "Selected") : T("选择", "Select"),
                SelectCard,
                SelectCard,
                configureCard: (cardObject, card, index) =>
                {
                    ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.OpponentBoard, index);
                    ConfigureBoardCardInteractions(cardObject, card);
                },
                configureSlot: (slot, index) => AddDropTarget(slot, UnityTavernDropTarget.OpponentBoard, index),
                layoutContext: layout,
                useEnglish: service.UseEnglish);
        }

        private void BuildShop(Transform parent, UnityTavernLayoutContext layout)
        {
            var zone = Zone("UnityShopZone", parent, layout, UnityTavernZoneKind.Shop, UnityTavernCardMode.Shop);
            zone.Build(
                T("鲍勃的酒馆", "Bob's Tavern"),
                service.State.Player.Tavern.Frozen ? T("已冻结", "Frozen") : T("可刷新", "Ready to Refresh"),
                service.State.Player.Tavern.Shop,
                0,
                UnityTavernCardMode.Shop,
                card => CanExecute(GameCommandType.BuyMinion) ? T("购买", "Buy") : null,
                SelectCard,
                BuyCard,
                configureCard: (cardObject, card, index) =>
                {
                    ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.Shop, index);
                    ConfigureCardHoverTooltip(cardObject, card);
                    AddDropTarget(cardObject, UnityTavernDropTarget.TavernShop, index);
                },
                layoutContext: layout,
                useEnglish: service.UseEnglish);
            BuildShopSellDropZone(zone.transform);
            BuildShopPhysicalDropZone(zone.transform);
        }

        private void BuildTimewarpedTavernModal()
        {
            var timewarp = service.State.Player.Tavern.Timewarp;
            if (timewarp?.VisitOpen != true)
            {
                return;
            }

            var modalObject = UnityTimewarpedTavernModalComponent.CreateModalHost(transform);
            modalObject.GetComponent<UnityTimewarpedTavernModalComponent>().Build(
                TimewarpedTavernTitle(timewarp.PendingKind),
                timewarp.Chronum,
                service.GetTimewarpedOfferCards(),
                timewarp.Offers,
                service.UseEnglish,
                index => Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, index)),
                () => Apply(new GameCommand(GameCommandType.ExitTimewarpedTavern)));
        }

        private void BuildOpponentHand(Transform parent, UnityTavernLayoutContext layout)
        {
            var zone = Zone("UnityOpponentHandZone", parent, layout, UnityTavernZoneKind.Hand, UnityTavernCardMode.Hand);
            BindOpponentHandZone(zone, layout);
        }

        private void BindOpponentHandZone(UnityTavernZoneComponent zone, UnityTavernLayoutContext layout)
        {
            var hand = service.State.Opponent.Hand ?? new List<MinionInstance>();
            zone.Build(
                T("对手手牌", "Opponent Hand"),
                hand.Count + "/10",
                hand,
                HandLimit,
                UnityTavernCardMode.Hand,
                OpponentHandActionLabel,
                SelectCard,
                RemoveOpponentHandCard,
                configureCard: (cardObject, card, index) => ConfigureCardHoverTooltip(cardObject, card),
                layoutContext: layout,
                useEnglish: service.UseEnglish);
        }

        private string TimewarpedTavernTitle(TimewarpKind kind)
        {
            if (service.UseEnglish)
            {
                return kind == TimewarpKind.Major ? "Major Timewarped Tavern" : "Minor Timewarped Tavern";
            }

            return kind == TimewarpKind.Major ? "大型时空酒馆" : "小型时空酒馆";
        }

        private void BuildPlayerBoard(Transform parent, UnityTavernLayoutContext layout)
        {
            var zone = Zone("UnityPlayerBoardZone", parent, layout, UnityTavernZoneKind.PlayerBoard, UnityTavernCardMode.Board);
            zone.Build(
                T("玩家战场", "Your Board"),
                service.State.Player.Board.Count + "/7",
                service.State.Player.Board,
                BoardLimit,
                UnityTavernCardMode.Board,
                card => CanExecute(GameCommandType.SellMinion) ? T("出售", "Sell") : null,
                SelectCard,
                SellCard,
                configureCard: (cardObject, card, index) =>
                {
                    ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.PlayerBoard, index);
                    ConfigureBoardCardInteractions(cardObject, card);
                    BuildEmbeddedRecruitActionButtons(cardObject, card);
                    if (card != null)
                    {
                        AddDropTarget(
                            cardObject,
                            UnityTavernDropTarget.PlayerBoard,
                            index,
                            cueOnlyWhenAllowed: true);
                    }
                },
                layoutContext: layout,
                useEnglish: service.UseEnglish);
            BuildPlayerBoardPhysicalDropZone(zone.transform);
            BuildSpellCastDropZone(zone.transform);
            BuildPurchaseDropZone(zone.transform, "UnityBoardBuyDropZone");
            /* removed full-board green reorder overlay
                "拖到这里调整己方站位",
                "按左右位置决定落点");
            */
        }

        private void BuildHand(Transform parent, UnityTavernLayoutContext layout)
        {
            var zone = Zone("UnityHandZone", parent, layout, UnityTavernZoneKind.Hand, UnityTavernCardMode.Hand);
            BindPlayerHandZone(zone, layout);
            BuildHandBuyDropZone(zone.transform);
        }

        private void BindPlayerHandZone(UnityTavernZoneComponent zone, UnityTavernLayoutContext layout)
        {
            var ordinaryHand = service.State.Player.Tavern.Hand;
            var shapingSpells = service.GetCurrentGuideShapingSpells();
            var visibleHand = ordinaryHand.Concat(shapingSpells).ToList();
            var shapingCount = shapingSpells.Count;
            zone.Build(
                T("手牌", "Hand"),
                ordinaryHand.Count + "/10" + (shapingCount > 0 ? T(" · 塑造法术 ", " · Shaping ") + shapingCount : string.Empty),
                visibleHand,
                HandLimit,
                UnityTavernCardMode.Hand,
                card => StrategyGuideShapingSpells.Contains(card?.CardId)
                    ? strategyGuideSession?.CanApply(GameCommandType.UseGuideShapingSpell) == true ? T("施放", "Cast") : null
                    : CanExecute(GameCommandType.PlayMinion) && card != null &&
                      card.CardKind != CardKind.TavernSpell && card.CardKind != CardKind.Spell
                        ? HandActionLabel(card)
                        : null,
                card =>
                {
                    if (StrategyGuideShapingSpells.Contains(card?.CardId)) BeginGuideShapingSpell(card);
                    else SelectCard(card);
                },
                card =>
                {
                    if (StrategyGuideShapingSpells.Contains(card?.CardId)) BeginGuideShapingSpell(card);
                    else PlayCard(card);
                },
                configureCard: (cardObject, card, index) =>
                {
                    if (StrategyGuideShapingSpells.Contains(card?.CardId))
                    {
                        var shapingIndex = shapingSpells.FindIndex(spell => spell.CardId == card.CardId);
                        ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.GuideShapingSpell, shapingIndex);
                        guideShapingSpellSourceAnchors[card.CardId] = cardObject.GetComponent<RectTransform>();
                        BuildGuideShapingSpellBadge(cardObject.transform);
                    }
                    else
                    {
                        ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.Hand, ordinaryHand.FindIndex(item => item.InstanceId == card.InstanceId));
                    }
                    ConfigureCardHoverTooltip(cardObject, card);
                },
                configureSlot: (slot, index) => AddDropTarget(
                    slot,
                    UnityTavernDropTarget.Hand,
                    Mathf.Min(index, ordinaryHand.Count)),
                layoutContext: layout,
                useEnglish: service.UseEnglish);
        }

        private void BuildGuideShapingSpellBadge(Transform parent)
        {
            var badge = Panel("UnityGuideShapingSpellBadge", parent, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.92f));
            var rect = badge.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.82f);
            rect.anchorMax = new Vector2(0.54f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            badge.GetComponent<Image>().raycastTarget = false;
            var text = UiFactory.Label("UnityGuideShapingSpellBadgeText", badge.transform, T("一图流", "GUIDE"), 14, FontStyle.Bold);
            text.color = UnityTavernUiStyle.SurfaceDark;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            UnityTavernUiStyle.Stretch(text.rectTransform);
        }

        private void BuildStrategyGuideGoalDrawerToggle()
        {
            if (strategyGuideSession == null ||
                strategyGuideGoalDrawerOpen)
            {
                return;
            }

            var buttonObject = new GameObject(
                "UnityStrategyGuideGoalDrawerToggle",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            buttonObject.transform.SetAsLastSibling();

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(48f, 112f);
            rect.anchoredPosition = new Vector2(LayoutContext().IsCompact ? 10f : 18f, -6f);

            var image = buttonObject.GetComponent<Image>();
            image.color = UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.PanelQuiet, 0.94f);
            image.raycastTarget = true;
            UnityTavernUiStyle.ConfigureOutline(
                buttonObject,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.74f),
                new Vector2(1f, -1f));

            var button = buttonObject.GetComponent<Button>();
            buttonObject.AddComponent<UnitySelectableFocusRing>();
            button.targetGraphic = image;
            button.onClick.AddListener(ToggleStrategyGuideGoalDrawer);
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.92f),
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.MutedText, 0.72f));

            var label = UiFactory.Label(
                "UnityStrategyGuideGoalDrawerToggleText",
                buttonObject.transform,
                T("目标\n阵容", "Goal\nBoard"),
                14,
                FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private void BuildStrategyGuideGoalDrawer()
        {
            if (strategyGuideSession == null)
            {
                strategyGuideGoalDrawerOpen = false;
                return;
            }

            var layout = LayoutContext();
            var panel = Panel(
                "UnityStrategyGuideGoalDrawer",
                transform,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.98f));
            panel.transform.SetAsLastSibling();
            var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(panel);
            element.ignoreLayout = true;
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.72f),
                new Vector2(2f, -2f));

            var width = Mathf.Clamp(layout.Width * (layout.IsCompact ? 0.78f : 0.34f), 340f, layout.IsCompact ? 390f : 450f);
            var guideHudVisible = strategyGuideSession.RunState != StrategyGuideRunState.FreeExplore;
            var topOffset = guideHudVisible
                ? (layout.IsCompact ? 158f : 146f)
                : (layout.IsCompact ? 82f : 92f);
            var sideOffset = layout.IsCompact ? 10f : 18f;
            var bottomOffset = layout.IsCompact ? 10f : 18f;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(sideOffset, bottomOffset);
            rect.offsetMax = new Vector2(width + sideOffset, -topOffset);

            var shadow = UnityTavernUiStyle.EnsureComponent<Shadow>(panel);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
            shadow.effectDistance = new Vector2(8f, -8f);
            shadow.useGraphicAlpha = true;

            var stack = panel.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(12, 12, 12, 12);
            stack.spacing = 8f;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            var header = Panel("UnityStrategyGuideGoalDrawerHeader", panel.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(header, UnityTavernUiStyle.TouchHeight);
            var headerRow = header.AddComponent<HorizontalLayoutGroup>();
            headerRow.spacing = 8f;
            headerRow.childAlignment = TextAnchor.MiddleCenter;
            headerRow.childControlWidth = true;
            headerRow.childControlHeight = true;
            headerRow.childForceExpandWidth = false;
            headerRow.childForceExpandHeight = false;

            var title = UiFactory.Label(
                "UnityStrategyGuideGoalDrawerTitle",
                header.transform,
                T("最终阵容目标", "Final Board Goal"),
                20,
                FontStyle.Bold);
            title.color = UnityTavernUiStyle.Gold;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, UnityTavernUiStyle.TouchHeight);

            ActionButton(
                "UnityStrategyGuideGoalDrawerCloseButton",
                header.transform,
                T("关闭", "Close"),
                ToggleStrategyGuideGoalDrawer,
                76f,
                UnityTavernUiStyle.TouchHeight,
                false,
                UnityTavernActionButtonRole.Utility);

            var evaluation = strategyGuideSession.Evaluation;
            var summary = UiFactory.Label(
                "UnityStrategyGuideGoalDrawerSummary",
                panel.transform,
                T("阵容完成 ", "Board ") + evaluation.MatchedFinalSlots + "/" + evaluation.FinalSlotCount +
                T(" · 成长完成 ", " · Growth ") + evaluation.CompletedGrowthCount + "/" + evaluation.RequiredGrowthCount,
                14,
                FontStyle.Bold);
            summary.color = UnityTavernUiStyle.ArcaneBlue;
            summary.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, 30f);

            var content = UiFactory.ScrollView(
                "UnityStrategyGuideGoalDrawerScroll",
                panel.transform,
                Color.clear,
                out _,
                layout);
            UnityTavernUiStyle.SetFlexible(content.parent.gameObject, 1f, 1f);
            var contentStack = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentStack.padding = new RectOffset(2, 4, 2, 4);
            contentStack.spacing = 8f;
            contentStack.childControlWidth = true;
            contentStack.childControlHeight = true;
            contentStack.childForceExpandWidth = true;
            contentStack.childForceExpandHeight = false;

            foreach (var slot in strategyGuideSession.FinalSlotProgress)
            {
                BuildStrategyGuideGoalSlot(content, slot, layout);
            }

            var growthTitle = UiFactory.Label(
                "UnityStrategyGuideGrowthTitle",
                content,
                T("全局成长", "Global Growth"),
                16,
                FontStyle.Bold);
            growthTitle.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(growthTitle.gameObject, 30f);

            var growthItems = strategyGuideSession.GrowthProgress;
            if (growthItems.Count == 0)
            {
                var empty = UiFactory.Label(
                    "UnityStrategyGuideGrowthEmpty",
                    content,
                    T("本关没有额外成长门槛。", "This level has no extra growth gate."),
                    14,
                    FontStyle.Normal);
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 34f);
            }
            else
            {
                foreach (var growth in growthItems)
                {
                    var row = Panel(
                        "UnityStrategyGuideGrowth-" + growth.Key,
                        content,
                        UnityTavernUiStyle.WithAlpha(
                            growth.IsComplete ? UnityTavernUiStyle.SuccessGreen : UnityTavernUiStyle.PanelQuiet,
                            growth.IsComplete ? 0.18f : 0.72f));
                    UnityTavernUiStyle.SetPreferredHeight(row, 42f);
                    UnityTavernUiStyle.ConfigureOutline(
                        row,
                        UnityTavernUiStyle.WithAlpha(
                            growth.IsComplete ? UnityTavernUiStyle.SuccessGreen : UnityTavernUiStyle.ArcaneBlue,
                            0.62f),
                        new Vector2(1f, -1f));
                    var text = UiFactory.Label(
                        "UnityStrategyGuideGrowthText-" + growth.Key,
                        row.transform,
                        (growth.IsComplete ? T("[完成] ", "[Complete] ") : T("[待完成] ", "[Pending] ")) +
                        DisplayStrategyGuideGrowthLabel(growth.Key) + "  " + growth.CurrentValue + "/" + growth.RequiredValue,
                        14,
                        FontStyle.Bold);
                    text.color = UnityTavernUiStyle.Text;
                    text.alignment = TextAnchor.MiddleLeft;
                    UnityTavernUiStyle.Stretch(text.rectTransform);
                    text.rectTransform.offsetMin = new Vector2(10f, 0f);
                    text.rectTransform.offsetMax = new Vector2(-10f, 0f);
                }
            }
        }

        private void BuildStrategyGuideGoalSlot(
            Transform parent,
            StrategyGuideFinalSlotProgress slot,
            UnityTavernLayoutContext layout)
        {
            var statusColor = StrategyGuideFinalSlotColor(slot.Status);
            var row = Panel(
                "UnityStrategyGuideGoalSlot-" + slot.SlotIndex,
                parent,
                UnityTavernUiStyle.WithAlpha(statusColor, 0.16f));
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 72f : 78f);
            UnityTavernUiStyle.ConfigureOutline(row, UnityTavernUiStyle.WithAlpha(statusColor, 0.72f), new Vector2(1f, -1f));
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 8, 6, 6);
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var indexLabel = UiFactory.Label(
                "UnityStrategyGuideGoalSlotIndex-" + slot.SlotIndex,
                row.transform,
                (slot.SlotIndex + 1).ToString(),
                18,
                FontStyle.Bold);
            indexLabel.color = UnityTavernUiStyle.Gold;
            indexLabel.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetFixedSize(indexLabel.gameObject, 26f, 48f);

            service.Catalogs.Minions.TryGetByCardId(slot.Target?.CardId, out var definition);
            var artObject = new GameObject(
                "UnityStrategyGuideGoalSlotArt-" + slot.SlotIndex,
                typeof(RectTransform),
                typeof(Image));
            artObject.transform.SetParent(row.transform, false);
            UnityTavernUiStyle.SetFixedSize(artObject, 48f, 48f);
            var art = artObject.GetComponent<Image>();
            art.raycastTarget = false;
            art.preserveAspect = true;
            art.sprite = definition == null
                ? null
                : CardImageProvider.LoadSprite(definition.ImagePath, definition.CardId, CardKind.Minion);
            art.color = art.sprite == null ? UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.ArcaneBlue, 0.48f) : Color.white;

            var textStack = new GameObject(
                "UnityStrategyGuideGoalSlotText-" + slot.SlotIndex,
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            textStack.transform.SetParent(row.transform, false);
            UnityTavernUiStyle.SetFlexible(textStack, 1f, 0f);
            var textLayout = textStack.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2f;
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            var targetName = definition == null
                ? slot.Target?.CardId ?? T("未知随从", "Unknown minion")
                : definition.Name;
            var name = UiFactory.Label(
                "UnityStrategyGuideGoalSlotName-" + slot.SlotIndex,
                textStack.transform,
                targetName,
                15,
                FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 24f);

            var requirement = UiFactory.Label(
                "UnityStrategyGuideGoalSlotRequirement-" + slot.SlotIndex,
                textStack.transform,
                (slot.Target?.Golden == true ? T("金色", "Golden") : T("普通", "Normal")) +
                " · " + T("至少 ", "At least ") +
                (slot.Target?.MinimumAttack ?? 0) + "/" + (slot.Target?.MinimumHealth ?? 0),
                14,
                FontStyle.Normal);
            requirement.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(requirement.gameObject, 22f);

            var status = UiFactory.Label(
                "UnityStrategyGuideGoalSlotStatus-" + slot.SlotIndex,
                row.transform,
                StrategyGuideFinalSlotStatusText(slot),
                14,
                FontStyle.Bold);
            status.color = UnityTavernUiStyle.Text;
            status.alignment = TextAnchor.MiddleCenter;
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetFixedSize(status.gameObject, layout.IsCompact ? 96f : 112f, 54f);
        }

        private string StrategyGuideFinalSlotStatusText(StrategyGuideFinalSlotProgress slot)
        {
            switch (slot.Status)
            {
                case StrategyGuideFinalSlotStatus.Complete:
                    return T("完成", "Complete");
                case StrategyGuideFinalSlotStatus.PositionWrong:
                    return T("位置错误\n当前第", "Wrong position\nCurrently ") + (slot.MatchedBoardIndex + 1) + T("格", "");
                case StrategyGuideFinalSlotStatus.StateMismatch:
                    return slot.Actual == null
                        ? T("状态不符", "State mismatch")
                        : T("状态不符\n", "State mismatch\n") +
                          (slot.MatchedBoardIndex >= 0 && slot.MatchedBoardIndex != slot.SlotIndex
                              ? T("第", "Slot ") + (slot.MatchedBoardIndex + 1) + T("格 · ", " · ")
                              : T("当前 ", "Current ")) +
                          (slot.Actual.Golden ? T("金色 ", "Golden ") : string.Empty) +
                          slot.Actual.Attack + "/" + slot.Actual.Health;
                default:
                    return T("缺失", "Missing");
            }
        }

        private static Color StrategyGuideFinalSlotColor(StrategyGuideFinalSlotStatus status)
        {
            switch (status)
            {
                case StrategyGuideFinalSlotStatus.Complete:
                    return UnityTavernUiStyle.SuccessGreen;
                case StrategyGuideFinalSlotStatus.PositionWrong:
                    return UnityTavernUiStyle.Gold;
                case StrategyGuideFinalSlotStatus.StateMismatch:
                    return UnityTavernUiStyle.ArcaneBlue;
                default:
                    return UnityTavernUiStyle.DangerRed;
            }
        }

        private void ToggleStrategyGuideGoalDrawer()
        {
            strategyGuideGoalDrawerOpen = !strategyGuideGoalDrawerOpen;
            if (strategyGuideGoalDrawerOpen)
            {
                rightPanelOpen = false;
            }
            Rebuild();
        }

        private void BuildRightPanelDrawerToggle()
        {
            if (rightPanelOpen)
            {
                return;
            }

            var buttonObject = new GameObject("UnityRightPanelDrawerToggle", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(48f, 88f);
            rect.anchoredPosition = new Vector2(-18f, -6f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(UnityTavernUiStyle.PanelQuiet.r, UnityTavernUiStyle.PanelQuiet.g, UnityTavernUiStyle.PanelQuiet.b, 0.84f);
            image.raycastTarget = true;
            UnityTavernUiStyle.ConfigureOutline(
                buttonObject,
                new Color(UnityTavernUiStyle.Blue.r, UnityTavernUiStyle.Blue.g, UnityTavernUiStyle.Blue.b, 0.24f),
                new Vector2(1f, -1f));

            var button = buttonObject.GetComponent<Button>();
            buttonObject.AddComponent<UnitySelectableFocusRing>();
            button.targetGraphic = image;
            button.onClick.AddListener(ToggleRightPanelDrawer);
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                new Color(0.82f, 0.92f, 1f, 1f),
                new Color(0.48f, 0.60f, 0.68f, 1f));

            var label = UiFactory.Label("UnityRightPanelDrawerToggleText", buttonObject.transform, T("功能", "Panel"), 13, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            var guideActive = strategyGuideSession != null && strategyGuideSession.RunState != StrategyGuideRunState.FreeExplore;
            label.text = guideActive ? T("任务", "Tasks") : T("功能", "Panel");
            label.fontSize = 14;
            label.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private void BuildFloatingRightPanel()
        {
            var panel = UnityTavernRightPanelComponent.CreatePanelHost(transform, "UnityRightPanel");
            ConfigureFloatingRightPanel(panel);
            panel.transform.SetAsLastSibling();
            var guideActive = strategyGuideSession != null && strategyGuideSession.RunState != StrategyGuideRunState.FreeExplore;
            panel.GetComponent<UnityTavernRightPanelComponent>().BuildTabbed(
                guideActive ? T("攻略任务", "Guide Tasks") : T("功能面板", "Utility Panel"),
                true,
                ToggleRightPanelDrawer,
                activeInspectorTab,
                SetInspectorTab,
                guideActive ? BuildStrategyGuideRightActions : BuildActionStripPrefab,
                BuildSelectedCardPrefab,
                BuildAdvisorPrefab,
                BuildLogPrefab,
                service.UseEnglish);
        }

        private void ConfigureFloatingRightPanel(GameObject panel)
        {
            var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(panel);
            element.ignoreLayout = true;

            var layout = LayoutContext();
            var width = Mathf.Clamp(layout.Width * (layout.IsCompact ? 0.78f : 0.34f), 340f, layout.IsCompact ? 390f : 450f);
            var topOffset = layout.IsCompact ? 82f : 92f;
            var sideOffset = layout.IsCompact ? 10f : 18f;
            var bottomOffset = layout.IsCompact ? 10f : 18f;

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(-(width + sideOffset), bottomOffset);
            rect.offsetMax = new Vector2(-sideOffset, -topOffset);

            var shadow = UnityTavernUiStyle.EnsureComponent<Shadow>(panel);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(-8f, -8f);
            shadow.useGraphicAlpha = true;
        }

        private void ToggleRightPanelDrawer()
        {
            rightPanelOpen = !rightPanelOpen;
            if (rightPanelOpen)
            {
                strategyGuideGoalDrawerOpen = false;
            }
            Rebuild();
        }

        private void SetInspectorTab(UnityTavernInspectorTab tab)
        {
            activeInspectorTab = tab;
            Rebuild();
        }

        private void BuildStrategyGuideRightActions(Transform parent)
        {
            var host = Panel("UnityStrategyGuideRightActions", parent, Color.clear);
            UnityTavernUiStyle.SetFlexible(host, 1f, 1f);
            var content = UiFactory.ScrollView(
                "UnityStrategyGuideRightActionsScroll",
                host.transform,
                Color.clear,
                out _,
                LayoutContext());
            var stack = content.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(8, 8, 8, 8);
            stack.spacing = 8f;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            var evaluation = strategyGuideSession.Evaluation;
            var summary = UiFactory.Label(
                "UnityStrategyGuideRightSummary",
                content,
                T("当前进度", "Current Progress") + "\n" +
                T("步骤 ", "Steps ") + evaluation.CompletedActionCount + "/" + evaluation.RequiredActionCount +
                T(" · 阵容 ", " · Board ") + evaluation.MatchedFinalSlots + "/" + evaluation.FinalSlotCount +
                T(" · 成长 ", " · Growth ") + evaluation.CompletedGrowthCount + "/" + evaluation.RequiredGrowthCount,
                16,
                FontStyle.Bold);
            summary.color = UnityTavernUiStyle.Gold;
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, 54f);

            var instruction = UiFactory.Label(
                "UnityStrategyGuideRightInstruction",
                content,
                T("下一步：", "Next: ") + strategyGuideSession.NextInstruction(service.UseEnglish),
                14,
                FontStyle.Bold);
            instruction.color = UnityTavernUiStyle.Text;
            instruction.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetPreferredHeight(instruction.gameObject, 52f);

            foreach (var action in strategyGuideSession.ActionProgress)
            {
                var text = (service.UseEnglish && !string.IsNullOrWhiteSpace(action.EnglishInstruction)
                    ? action.EnglishInstruction
                    : action.Instruction) + "  " + action.CompletedCount + "/" + action.RequiredCount;
                var row = UiFactory.Label(
                    "UnityStrategyGuideRightAction-" + action.ActionId,
                    content,
                    (action.IsComplete ? T("[完成] ", "[Complete] ") : T("[待完成] ", "[Pending] ")) + text,
                    14,
                    FontStyle.Bold);
                row.color = UnityTavernUiStyle.Text;
                row.horizontalOverflow = HorizontalWrapMode.Wrap;
                UnityTavernUiStyle.SetPreferredHeight(row.gameObject, 46f);
            }

            foreach (var growth in strategyGuideSession.GrowthProgress)
            {
                var row = UiFactory.Label(
                    "UnityStrategyGuideRightGrowth-" + growth.Key,
                    content,
                    (growth.IsComplete ? T("[完成] ", "[Complete] ") : T("[待完成] ", "[Pending] ")) +
                    DisplayStrategyGuideGrowthLabel(growth.Key) + "  " + growth.CurrentValue + "/" + growth.RequiredValue,
                    14,
                    FontStyle.Bold);
                row.color = UnityTavernUiStyle.Text;
                UnityTavernUiStyle.SetPreferredHeight(row.gameObject, 36f);
            }
        }

        private void BuildActionStripPrefab(Transform parent)
        {
            var panel = UnityTavernActionPanelComponent.CreatePanelHost(parent, "UnityActionPanel");
            UnityTavernUiStyle.SetPreferredHeight(panel, 214f);
            panel.GetComponent<UnityTavernActionPanelComponent>().Build(BuildActionButtonsPrefab);
        }

        private void BuildActionButtonsPrefab(Transform parent)
        {
            BuildActionButtons(parent, "Unity", false, LayoutContext());
        }

        private void BuildActionButtons(Transform parent, string namePrefix, bool quickBar, UnityTavernLayoutContext layout)
        {
            var height = quickBar ? Mathf.Max(UnityTavernUiStyle.TouchHeight, layout.IsCompact ? UnityTavernUiStyle.CompactTouchHeight : UnityTavernUiStyle.TouchHeight) : 0f;
            var flexibleWidth = quickBar;
            var minWidth = quickBar ? (layout.IsCompact ? 84f : 108f) : 0f;

            if (!quickBar)
            {
                BuildSelectedRecruitActionButtons(parent, namePrefix, minWidth, height, flexibleWidth);
            }

            var heroPowers = CurrentHeroPowers();
            for (var heroPowerIndex = 0; heroPowerIndex < heroPowers.Count; heroPowerIndex += 1)
            {
                var heroPower = heroPowers[heroPowerIndex];
                var capturedHeroPower = heroPower;
                var unlocked = IsHeroPowerUnlocked(heroPower);
                var canUseHeroPower = CanExecute(GameCommandType.UseHeroPower) &&
                                      heroPower != null &&
                                      unlocked &&
                                      service.CanUseHeroPower(heroPower.CardId);
                var heroPowerButton = ActionButton(
                    heroPowerIndex == 0 ? namePrefix + "HeroPowerButton" : namePrefix + "HeroPowerButton" + heroPowerIndex,
                    parent,
                    HeroPowerActionLabel(heroPower),
                    () => BeginHeroPowerTargeting(capturedHeroPower),
                    minWidth,
                    height,
                    flexibleWidth,
                    UnityTavernActionButtonRole.Primary,
                    canUseHeroPower);
                if (canUseHeroPower)
                {
                    AddHeroPowerDrag(heroPowerButton.gameObject, heroPower);
                }
            }
            ActionButton(namePrefix + "RefreshButton", parent, RefreshActionLabel(), () => Apply(new GameCommand(GameCommandType.RerollShop)), minWidth, height, flexibleWidth, UnityTavernActionButtonRole.Economy, CanRefreshShop());
            ActionButton(namePrefix + "FreezeButton", parent, service.State.Player.Tavern.Frozen ? T("解冻", "Unfreeze") : T("冻结", "Freeze"), () => Apply(new GameCommand(GameCommandType.FreezeShop, !service.State.Player.Tavern.Frozen)), minWidth, height, flexibleWidth, UnityTavernActionButtonRole.Economy, CanExecute(GameCommandType.FreezeShop));
            ActionButton(namePrefix + "UpgradeButton", parent, UpgradeActionLabel(), () => Apply(new GameCommand(GameCommandType.UpgradeTavern)), minWidth, height, flexibleWidth, UnityTavernActionButtonRole.Economy, CanUpgradeTavern());
            var advanceFromResult = service.State.Phase == MatchPhase.Result;
            var turnCommand = advanceFromResult ? GameCommandType.DebugSkipToNextTurn : GameCommandType.BeginNextTurnTransition;
            var turnLabel = NextTurnActionLabel(advanceFromResult, T("完整下一回合", "Complete Next Turn"));
            ActionButton(
                namePrefix + "NextTurnButton",
                parent,
                turnLabel,
                () =>
                {
                    if (advanceFromResult)
                    {
                        Apply(new GameCommand(turnCommand));
                    }
                    else
                    {
                        ApplyAndOpenReplay(new GameCommand(turnCommand));
                    }
                },
                minWidth,
                height,
                flexibleWidth,
                UnityTavernActionButtonRole.Combat,
                CanExecute(turnCommand));
            ActionButton(namePrefix + "ReplayButton", parent, ReplayActionLabel(), OpenCombatReplay, minWidth, height, flexibleWidth, UnityTavernActionButtonRole.Utility, HasCombatReplay());
            ActionButton(namePrefix + "ToolsButton", parent, T("工具", "Tools"), OpenTools, minWidth, height, flexibleWidth, UnityTavernActionButtonRole.Utility);
        }

        private void BuildSelectedRecruitActionButtons(
            Transform parent,
            string namePrefix,
            float minWidth,
            float height,
            bool flexibleWidth)
        {
            if (HasActiveTargetingSession())
            {
                return;
            }

            var source = SelectedPlayerBoardCard();
            if (source == null)
            {
                return;
            }

            var actions = RecruitActionsFor(source);
            for (var index = 0; index < actions.Count; index += 1)
            {
                var action = actions[index];
                if (action == null)
                {
                    continue;
                }

                var captured = action.Clone();
                var reason = RecruitActionUnavailableReason(source, action);
                var label = RecruitActionLabel(source, action, reason);
                ActionButton(
                    "UnityRecruitActionButton-" + SafeObjectName(action.ActionId),
                    parent,
                    label,
                    () => BeginRecruitAction(source, captured),
                    minWidth,
                    height,
                    flexibleWidth,
                    UnityTavernActionButtonRole.Primary,
                    string.IsNullOrWhiteSpace(reason));
            }
        }

        private void BuildEmbeddedRecruitActionButtons(GameObject cardObject, MinionInstance source)
        {
            if (cardObject == null || source == null || HasActiveTargetingSession())
            {
                return;
            }

            var actions = RecruitActionsFor(source)
                .Where(action => action != null)
                .ToList();
            if (actions.Count == 0)
            {
                return;
            }

            var rackObject = new GameObject(
                "UnityCardRecruitActionRack-" + source.InstanceId,
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            rackObject.transform.SetParent(cardObject.transform, false);
            rackObject.transform.SetAsLastSibling();
            var rackRect = rackObject.GetComponent<RectTransform>();
            var rowHeight = Mathf.Max(44f, UnityTavernUiStyle.TouchHeight);
            var rackHeight = actions.Count * rowHeight + Math.Max(0, actions.Count - 1) * 3f;
            rackRect.anchorMin = new Vector2(0.08f, 1f);
            rackRect.anchorMax = new Vector2(0.92f, 1f);
            rackRect.pivot = new Vector2(0.5f, 1f);
            rackRect.offsetMin = new Vector2(0f, -rackHeight - 4f);
            rackRect.offsetMax = new Vector2(0f, -4f);

            var layout = rackObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 3f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            foreach (var action in actions)
            {
                var captured = action.Clone();
                var reason = RecruitActionUnavailableReason(source, action);
                var button = ActionButton(
                    "UnityCardRecruitActionButton-" + source.InstanceId + "-" + SafeObjectName(action.ActionId),
                    rackObject.transform,
                    RecruitActionCardLabel(source, action, reason),
                    () => BeginRecruitAction(source, captured),
                    0f,
                    rowHeight,
                    true,
                    UnityTavernActionButtonRole.Primary,
                    string.IsNullOrWhiteSpace(reason));
                var label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.fontSize = 12;
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 9;
                    label.resizeTextMaxSize = 12;
                    label.horizontalOverflow = HorizontalWrapMode.Wrap;
                    label.verticalOverflow = VerticalWrapMode.Truncate;
                }
            }
        }

        private IReadOnlyList<RecruitActionDefinition> RecruitActionsFor(MinionInstance source)
        {
            if (source == null || service.Catalogs?.Minions == null)
            {
                return new List<RecruitActionDefinition>();
            }

            if (!service.Catalogs.Minions.TryGetByCardId(source.CardId, out var definition) &&
                !service.Catalogs.Minions.TryGetById(source.DefinitionId, out definition))
            {
                return new List<RecruitActionDefinition>();
            }

            return definition.RecruitActions ?? new List<RecruitActionDefinition>();
        }

        private string RecruitActionLabel(
            MinionInstance source,
            RecruitActionDefinition definition,
            string unavailableReason)
        {
            var state = RecruitActionStateFor(source, definition);
            var used = state != null && state.LastUsedRound == service.State.Round
                ? Math.Max(0, state.UsesThisTurn)
                : 0;
            var remaining = Math.Max(0, definition.UsesPerTurn - used);
            var cost = Math.Max(0, definition.CostSpec?.Gold ?? 0);
            var label = T("发动", "Activate") +
                        T(" · ", " · ") + cost + T(" 金币", " Gold") +
                        " · " + remaining + "/" + Math.Max(1, definition.UsesPerTurn) +
                        " · " + RecruitActionTargetLabel(definition.TargetSpec);
            return string.IsNullOrWhiteSpace(unavailableReason)
                ? label
                : label + T("\n⛔ 不可用：", "\n⛔ Unavailable: ") + unavailableReason;
        }

        private string RecruitActionCardLabel(
            MinionInstance source,
            RecruitActionDefinition definition,
            string unavailableReason)
        {
            var state = RecruitActionStateFor(source, definition);
            var used = state != null && state.LastUsedRound == service.State.Round
                ? Math.Max(0, state.UsesThisTurn)
                : 0;
            var maximum = Math.Max(1, definition.UsesPerTurn);
            var remaining = Math.Max(0, maximum - used);
            var cost = Math.Max(0, definition.CostSpec?.Gold ?? 0);
            var status = string.IsNullOrWhiteSpace(unavailableReason)
                ? T("可用", "Ready")
                : used >= maximum
                    ? T("已用", "Used")
                    : unavailableReason;
            return T("发动", "Activate") + " · " + cost + T("金", "G") +
                   "\n" + remaining + "/" + maximum + " · " + status;
        }

        private string RecruitActionUnavailableReason(MinionInstance source, RecruitActionDefinition definition)
        {
            if (service.State.Phase != definition.AllowedPhase)
            {
                return T("当前阶段不可用", "Wrong phase");
            }

            var actionState = RecruitActionStateFor(source, definition);
            if (!string.IsNullOrWhiteSpace(actionState?.LockedReason))
            {
                return actionState.LockedReason;
            }

            if ((actionState?.Cooldown ?? 0) > 0)
            {
                return T("冷却剩余 " + actionState.Cooldown + " 回合", "Cooldown: " + actionState.Cooldown);
            }

            var uses = actionState != null && actionState.LastUsedRound == service.State.Round
                ? Math.Max(0, actionState.UsesThisTurn)
                : 0;
            if (uses >= Math.Max(1, definition.UsesPerTurn))
            {
                return T("本回合次数已用完", "No uses remaining this turn");
            }

            var cost = Math.Max(0, definition.CostSpec?.Gold ?? 0);
            if (service.State.Player.Tavern.Gold < cost)
            {
                return T("金币不足", "Not enough Gold");
            }

            return string.Empty;
        }

        private RecruitActionState RecruitActionStateFor(
            MinionInstance source,
            RecruitActionDefinition definition)
        {
            if (source == null || definition == null)
            {
                return null;
            }

            var states = service.State.RecruitActionStates ?? new List<RecruitActionState>();
            var exact = states.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.SourceInstanceId, source.InstanceId, StringComparison.Ordinal) &&
                string.Equals(item.ActionId, definition.ActionId, StringComparison.Ordinal));
            if (exact != null)
            {
                return exact;
            }

            return RecruitActionsFor(source).Count == 1
                ? states.FirstOrDefault(item =>
                    item != null &&
                    string.Equals(item.SourceInstanceId, source.InstanceId, StringComparison.Ordinal) &&
                    string.IsNullOrWhiteSpace(item.ActionId))
                : null;
        }

        private string RecruitActionTargetLabel(RecruitActionTargetSpec targetSpec)
        {
            switch (targetSpec)
            {
                case RecruitActionTargetSpec.TavernMinion:
                    return T("选择酒馆随从", "Target a Tavern minion");
                case RecruitActionTargetSpec.OtherFriendlyBoardMinion:
                    return T("选择另一名己方随从", "Target another friendly minion");
                case RecruitActionTargetSpec.FriendlyBoardMinion:
                    return T("选择己方随从", "Target a friendly minion");
                default:
                    return T("无需目标", "No target");
            }
        }

        private void BeginRecruitAction(MinionInstance source, RecruitActionDefinition definition)
        {
            if (source == null || definition == null ||
                !string.IsNullOrWhiteSpace(RecruitActionUnavailableReason(source, definition)))
            {
                return;
            }

            selectedInstanceId = source.InstanceId;
            pendingRecruitActionSourceInstanceId = source.InstanceId;
            pendingRecruitActionDefinition = definition.Clone();
            if (definition.TargetSpec == RecruitActionTargetSpec.None)
            {
                ExecuteRecruitAction(new RecruitActionRequest
                {
                    ActionId = definition.ActionId,
                    SourceInstanceId = source.InstanceId
                });
                return;
            }

            lastError = null;
            lastFeedback = T("请选择一个合法目标。", "Choose a legal target.");
            Rebuild();
        }

        private bool CanRefreshShop()
        {
            if (!CanExecute(GameCommandType.RerollShop))
            {
                return false;
            }

            var tavern = service.State.Player.Tavern;
            if (tavern.FreeRefreshes > 0)
            {
                return true;
            }

            if (tavern.HealthCostRefreshes > 0)
            {
                return service.State.Player.Health > 1;
            }

            return tavern.Gold >= CurrentRefreshCost();
        }

        private bool CanUpgradeTavern()
        {
            if (!CanExecute(GameCommandType.UpgradeTavern))
            {
                return false;
            }

            var tavern = service.State.Player.Tavern;
            var upgradeCost = CurrentUpgradeCost();
            return tavern.UpgradeCost > 0 && tavern.Gold >= upgradeCost;
        }

        private bool CanExecute(GameCommandType commandType)
        {
            if (service?.State == null ||
                (strategyGuideSession != null
                    ? !strategyGuideSession.CanApply(commandType)
                    : !service.CanApply(commandType)))
            {
                return false;
            }

            return (commandType != GameCommandType.NextTurn && commandType != GameCommandType.BeginNextTurnTransition) ||
                string.IsNullOrEmpty(service.GetNextTurnBlockedReason());
        }

        private string NextTurnActionLabel(bool advanceFromResult, string defaultLabel)
        {
            if (advanceFromResult)
            {
                return T("进入下一准备阶段", "Enter Next Recruit Phase");
            }

            var reason = service.GetNextTurnBlockedReason();
            return string.IsNullOrEmpty(reason)
                ? defaultLabel
                : reason.TrimEnd('。').Replace("请先", "先").Replace("当前", string.Empty);
        }

        private bool HasCombatReplay()
        {
            return service.State.LastReplay != null
                && service.State.LastReplay.Frames != null
                && service.State.LastReplay.Frames.Count > 0;
        }

        private string RefreshActionLabel()
        {
            var tavern = service.State.Player.Tavern;
            if (tavern.FreeRefreshes > 0)
            {
                return T("免费刷新", "Free Refresh");
            }

            return tavern.HealthCostRefreshes > 0
                ? T("刷新 1血", "Refresh 1 Health")
                : T("刷新 ", "Refresh ") + CurrentRefreshCost();
        }

        private string UpgradeActionLabel()
        {
            var tavern = service.State.Player.Tavern;
            return tavern.UpgradeCost > 0 ? T("升本 ", "Upgrade ") + CurrentUpgradeCost() : T("满本", "Max Tier");
        }

        private int CurrentRefreshCost()
        {
            return HeroEffectEngine.ModifyRefreshCost(service.State, service.State.Player.HeroPowerCardId, 1);
        }

        private int CurrentUpgradeCost()
        {
            return HeroEffectEngine.ModifyUpgradeCost(service.State, service.State.Player.HeroPowerCardId, service.State.Player.Tavern.UpgradeCost);
        }

        private string ReplayActionLabel()
        {
            return HasCombatReplay() ? T("回放", "Replay") : T("无回放", "No Replay");
        }

        private string HeroPowerActionLabel(HeroPowerDefinition heroPower)
        {
            return heroPower == null ? T("无技能", "No Power") : T("技能 ", "Power ") + Math.Max(0, heroPower.Cost);
        }

        private void BuildSelectedCardPrefab(Transform parent)
        {
            var card = FindSelectedCard();
            var detail = UnityTavernSelectedCardPanelComponent.CreatePanelHost(parent, "UnitySelectedCardPanel");
            UnityTavernUiStyle.SetPreferredHeight(detail, 250f);
            detail.GetComponent<UnityTavernSelectedCardPanelComponent>().Build(content => BuildSelectedCardPrefabContent(content, card));
        }

        private void BuildSelectedCardPrefabContent(Transform parent, MinionInstance card)
        {
            if (card == null)
            {
                var emptyPanel = BuildInspectorSection(parent, "UnitySelectedCardEmptyPanel", T("当前选择", "Current Selection"), UnityTavernUiStyle.Gold, 286f, 96f);
                var empty = UiFactory.Label("UnitySelectedCardEmpty", emptyPanel.transform, T("选择一张牌查看详情。", "Select a card to view details."), 14, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 48f);
                return;
            }

            var detailLayout = Panel("UnitySelectedCardDetailLayout", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(detailLayout, UnityTavernUiStyle.Gold, 0.2f);
            UnityTavernUiStyle.SetFixedSize(detailLayout, 386f, 216f);
            var rowLayout = detailLayout.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 8, 9, 9);
            rowLayout.spacing = 8;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var cardObject = UnityTavernCardComponent.CreateCardHost(UnityTavernCardMode.Hand, detailLayout.transform, "UnitySelectedCardDetail");
            cardObject.GetComponent<UnityTavernCardComponent>().Bind(card, UnityTavernCardMode.Hand, null, SelectCard, null, true, service.UseEnglish);

            var infoStack = Panel("UnitySelectedCardInfoStack", detailLayout.transform, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(infoStack, card.CardKind == CardKind.TavernSpell ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.Gold, 0.18f);
            UnityTavernUiStyle.SetFixedSize(infoStack, 226f, 198f);
            var stackLayout = infoStack.AddComponent<VerticalLayoutGroup>();
            stackLayout.padding = new RectOffset(0, 0, 0, 0);
            stackLayout.spacing = 5;
            stackLayout.childControlWidth = true;
            stackLayout.childControlHeight = true;
            stackLayout.childForceExpandWidth = true;
            stackLayout.childForceExpandHeight = false;

            BuildSelectedCardSummary(infoStack.transform, card);

            var effectSection = BuildInspectorSection(infoStack.transform, "UnitySelectedCardEffectSection", T("效果", "Effect"), UnityTavernUiStyle.Blue, 226f, 74f);
            var displayText = DisplayCardText(card);
            var text = UiFactory.Label("UnitySelectedCardText", effectSection.transform, string.IsNullOrEmpty(displayText) ? T("无额外效果。", "No additional effect.") : displayText, 11, FontStyle.Normal);
            text.color = UnityTavernUiStyle.MutedText;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, 28f);

            var actionSection = Panel("UnitySelectedCardActionSection", infoStack.transform, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(actionSection, UnityTavernUiStyle.Green, 0.18f);
            UnityTavernUiStyle.SetFixedSize(actionSection, 226f, 56f);
            var actionLayout = actionSection.AddComponent<HorizontalLayoutGroup>();
            actionLayout.padding = new RectOffset(10, 10, 4, 4);
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = true;
            var details = ActionButton("UnitySelectedCardDetailsButton", actionSection.transform, T("查看详情", "View Details"), OpenCardDetail);
            UnityTavernUiStyle.SetFixedSize(details.gameObject, 186f, UnityTavernUiStyle.TouchHeight);
            UnityTavernUiStyle.ConfigureOutline(details.gameObject, new Color(UnityTavernUiStyle.Green.r, UnityTavernUiStyle.Green.g, UnityTavernUiStyle.Green.b, 0.42f), new Vector2(1f, -1f));
        }

        private void BuildAdvisorPrefab(Transform parent)
        {
            var panel = UnityTavernAdvisorPanelComponent.CreatePanelHost(parent, "UnityAdvisorPanel");
            UnityTavernUiStyle.SetPreferredHeight(panel, 132f);
            panel.GetComponent<UnityTavernAdvisorPanelComponent>().Build(T("建议", "Advice"), BuildAdvisorPrefabLines);
        }

        private void BuildAdvisorPrefabLines(Transform parent)
        {
            var adviceLines = advisor.GetAdvice(service.State, service.UseEnglish).Take(3).ToList();
            if (adviceLines.Count == 0)
            {
                adviceLines.Add(T("暂无建议。先进行一次操作。", "No advice yet. Make an action first."));
            }

            for (var index = 0; index < adviceLines.Count; index += 1)
            {
                BuildInspectorTextRow(
                    parent,
                    "UnityAdvisorLineCard",
                    "UnityAdvisorLine",
                    adviceLines[index],
                    UnityTavernUiStyle.Green,
                    30f,
                    index % 2 == 0 ? UnityTavernUiStyle.PanelQuiet : UnityTavernUiStyle.PanelRaised);
            }
        }

        private void BuildLogPrefab(Transform parent)
        {
            var hasCombatLog = service.State.CombatLog.Count > 0;
            var panel = UnityTavernLogPanelComponent.CreatePanelHost(parent, "UnityLogScroll", hasCombatLog);
            UnityTavernUiStyle.SetFlexible(panel, 1f, 1f);

            var logs = hasCombatLog
                ? service.State.CombatLog.Select(log => log.Title + " - " + log.Detail).Take(12).ToList()
                : service.State.Player.Tavern.RecruitLog.Select(log => log.Message).Reverse().Take(12).ToList();
            if (logs.Count == 0)
            {
                logs.Add(T("暂无日志。先购买、刷新或战斗。", "No logs yet. Buy, refresh, or start combat."));
            }

            panel.GetComponent<UnityTavernLogPanelComponent>().Build(hasCombatLog ? T("战斗日志", "Combat Log") : T("招募日志", "Recruit Log"), content => BuildLogPrefabLines(content, logs));
        }

        private static void BuildLogPrefabLines(Transform parent, IReadOnlyList<string> logs)
        {
            for (var index = 0; index < logs.Count; index += 1)
            {
                BuildInspectorTextRow(
                    parent,
                    "UnityLogLineRow",
                    "UnityLogLine",
                    logs[index],
                    LogAccentColor(logs[index]),
                    30f,
                    index % 2 == 0 ? UnityTavernUiStyle.PanelQuiet : UnityTavernUiStyle.PanelRaised);
            }
        }

        private void BuildSelectedCardSummary(Transform parent, MinionInstance card)
        {
            var summary = Panel("UnitySelectedCardSummarySection", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(summary, card.CardKind == CardKind.TavernSpell ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.Gold, 0.24f);
            UnityTavernUiStyle.SetFixedSize(summary, 226f, 58f);
            var layout = summary.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 7, 5, 5);
            layout.spacing = 7;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var accent = new GameObject("UnitySelectedCardSummaryAccent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(summary.transform, false);
            UnityTavernUiStyle.SetFixedSize(accent, 4f, 42f);
            UnityTavernUiStyle.ConfigureSurface(accent, card.CardKind == CardKind.TavernSpell ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.Gold);

            var textStack = new GameObject("UnitySelectedCardSummaryTextStack", typeof(RectTransform));
            textStack.transform.SetParent(summary.transform, false);
            UnityTavernUiStyle.SetFlexible(textStack, 1f, 0f);
            var textLayout = textStack.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 1;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            var name = UiFactory.Label("UnitySelectedCardNameText", textStack.transform, DisplayCardName(card), 12, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 18f);

            var meta = UiFactory.Label("UnitySelectedCardMetaText", textStack.transform, SelectedCardMeta(card), 10, FontStyle.Bold);
            meta.color = UnityTavernUiStyle.Gold;
            meta.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(meta.gameObject, 15f);

            var stats = UiFactory.Label("UnitySelectedCardStatsText", textStack.transform, SelectedCardStats(card), 10, FontStyle.Normal);
            stats.color = UnityTavernUiStyle.MutedText;
            stats.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(stats.gameObject, 15f);
        }

        private static GameObject BuildInspectorSection(Transform parent, string name, string title, Color accentColor, float width, float height)
        {
            var section = Panel(name, parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(section, accentColor, 0.18f);
            UnityTavernUiStyle.SetFixedSize(section, width, height);
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 5;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildToolsSectionHeader(section.transform, name + "Header", name + "Title", title, accentColor);
            return section;
        }

        private static void BuildInspectorTextRow(Transform parent, string rowName, string labelName, string message, Color accentColor, float height, Color surfaceColor)
        {
            var row = Panel(rowName, parent, surfaceColor);
            ConfigureInspectorSurface(row, accentColor, 0.16f);
            UnityTavernUiStyle.SetPreferredHeight(row, height);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 7, 4, 4);
            layout.spacing = 7;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var accent = new GameObject(labelName + "Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(row.transform, false);
            UnityTavernUiStyle.SetFixedSize(accent, 4f, Mathf.Max(18f, height - 10f));
            UnityTavernUiStyle.ConfigureSurface(accent, accentColor);

            var line = UiFactory.Label(labelName, row.transform, message, 11, FontStyle.Normal);
            line.color = UnityTavernUiStyle.MutedText;
            line.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFlexible(line.gameObject, 1f, 0f);
        }

        private static void ConfigureInspectorSurface(GameObject target, Color accentColor, float accentAlpha)
        {
            var current = UnityTavernUiStyle.EnsureComponent<Image>(target).color;
            UnityTavernUiStyle.ConfigureSurface(target, current.a > 0f ? current : UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.ConfigureOutline(
                target,
                new Color(accentColor.r, accentColor.g, accentColor.b, accentAlpha),
                new Vector2(1f, -1f));
        }

        private static Color LogAccentColor(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return UnityTavernUiStyle.Blue;
            }

            if (message.Contains("伤害") || message.Contains("死亡") || message.Contains("战斗") || message.Contains("Attack") || message.Contains("Damage"))
            {
                return UnityTavernUiStyle.Red;
            }

            if (message.Contains("购买") || message.Contains("刷新") || message.Contains("升本") || message.Contains("金币"))
            {
                return UnityTavernUiStyle.Gold;
            }

            return UnityTavernUiStyle.Blue;
        }

        private string SelectedCardMeta(MinionInstance card)
        {
            if (card.CardKind == CardKind.TavernSpell)
            {
                return T("酒馆法术 / ", "Tavern Spell / ") + Math.Max(0, card.Cost) + T("费", " Cost");
            }

            return (service.UseEnglish ? "Tier " : string.Empty) + card.TavernTier + T("本 / ", " / ") + TribesText(card) + (card.Golden ? T(" / 金色", " / Golden") : string.Empty);
        }

        private string SelectedCardStats(MinionInstance card)
        {
            if (card.CardKind == CardKind.TavernSpell)
            {
                return T("手牌 ", "Hand · ") + Math.Max(0, card.Cost) + T("费", " Cost");
            }

            return T("攻 ", "Attack ") + TavernNumberFormatter.FullNumber(card.Attack) + T(" / 血 ", " / Health ") + TavernNumberFormatter.FullNumber(card.Health);
        }

        private void OpenCardDetail()
        {
            cardDetailOpen = true;
            Rebuild();
        }

        private void CloseCardDetail()
        {
            cardDetailOpen = false;
            Rebuild();
        }

        private void BuildCardDetailModal()
        {
            var card = FindSelectedCard();
            if (card == null)
            {
                cardDetailOpen = false;
                return;
            }

            var modal = UnityTavernCardDetailModalComponent.CreateModalHost(transform, "UnityCardDetailOverlay");
            modal.transform.SetAsLastSibling();
            modal.GetComponent<UnityTavernCardDetailModalComponent>().Build(card, CloseCardDetail, useEnglish: service.UseEnglish);
        }

        private void OpenCombatReplay()
        {
            combatReplayOpen = true;
            combatTimelineOpen = false;
            replayWasPlayingBeforeTimeline = false;
            replayTimelineFilter = UnityReplayTimelineFilter.All;
            replayPlaying = false;
            replayPlaybackElapsed = 0f;
            Rebuild();
        }

        private void CloseCombatReplay()
        {
            combatReplayOpen = false;
            replayPlaying = false;
            combatTimelineOpen = false;
            replayWasPlayingBeforeTimeline = false;
            replayPlaybackElapsed = 0f;
            if (service.State.PendingTurnStartRound > 0)
            {
                Apply(new GameCommand(GameCommandType.ContinueNextTurnTransition));
                return;
            }

            Rebuild();
        }

        private void SetReplayFrameIndex(int index)
        {
            var replay = service.State.LastReplay;
            activeReplayFrameIndex = replay == null || replay.Frames.Count == 0
                ? 0
                : Mathf.Clamp(index, 0, replay.Frames.Count - 1);
            replayPlaybackElapsed = 0f;
            Rebuild();
        }

        private void SetReplayTimelineFilter(UnityReplayTimelineFilter filter)
        {
            replayTimelineFilter = filter;
            Rebuild();
        }

        private void ToggleReplayPlayback()
        {
            var replay = service.State.LastReplay;
            if (replay == null || replay.Frames == null || replay.Frames.Count == 0)
            {
                replayPlaying = false;
                return;
            }

            if (!replayPlaying && activeReplayFrameIndex >= replay.Frames.Count - 1)
            {
                activeReplayFrameIndex = 0;
            }

            replayPlaying = !replayPlaying;
            replayPlaybackElapsed = 0f;
            Rebuild();
        }

        private void CycleReplaySpeed()
        {
            replaySpeedIndex = (replaySpeedIndex + 1) % ReplaySpeedLabels.Length;
            replayPlaybackElapsed = 0f;
            Rebuild();
        }

        private void TickReplayPlayback(float deltaTime)
        {
            if (!combatReplayOpen || !replayPlaying)
            {
                return;
            }

            var replay = service.State.LastReplay;
            if (replay == null || replay.Frames == null || replay.Frames.Count == 0)
            {
                replayPlaying = false;
                replayPlaybackElapsed = 0f;
                return;
            }

            if (activeReplayFrameIndex >= replay.Frames.Count - 1)
            {
                replayPlaying = false;
                replayPlaybackElapsed = 0f;
                Rebuild();
                return;
            }

            replayPlaybackElapsed += Mathf.Max(0f, deltaTime);
            var frameDuration = ReplayFrameDurations[Mathf.Clamp(replaySpeedIndex, 0, ReplayFrameDurations.Length - 1)];
            if (replayPlaybackElapsed < frameDuration)
            {
                return;
            }

            while (replayPlaybackElapsed >= frameDuration && activeReplayFrameIndex < replay.Frames.Count - 1)
            {
                activeReplayFrameIndex += 1;
                replayPlaybackElapsed -= frameDuration;
            }

            if (activeReplayFrameIndex >= replay.Frames.Count - 1)
            {
                replayPlaying = false;
                replayPlaybackElapsed = 0f;
            }

            Rebuild();
        }

        private IReadOnlyList<UnityCombatTrinketDisplay> CombatPlayerTrinkets()
        {
            var displays = new List<UnityCombatTrinketDisplay>();
            var state = service.State.Player.Tavern.AdvancedMechanics?.Trinkets;
            AddPlayerCombatTrinket(displays, state, TrinketSlotKind.Lesser, state?.LesserTrinketId);
            AddPlayerCombatTrinket(displays, state, TrinketSlotKind.Greater, state?.GreaterTrinketId);
            return displays;
        }

        private void AddPlayerCombatTrinket(
            List<UnityCombatTrinketDisplay> displays,
            PlayerTrinketState state,
            TrinketSlotKind slotKind,
            string idOrCardId)
        {
            if (string.IsNullOrWhiteSpace(idOrCardId))
            {
                return;
            }

            var definition = ResolveTrinketDefinition(idOrCardId);
            var equipped = state?.Equipped?
                .FirstOrDefault(item => item != null && item.SlotKind == slotKind);
            var status = T("\u6301\u7eed\u751f\u6548", "Active");
            if (definition != null && definition.ImplementationStatus != TrinketImplementationStatus.Implemented)
            {
                status += " / " + BattlegroundsLocalizedText.TrinketImplementation(definition.ImplementationStatus, service.UseEnglish);
            }

            displays.Add(CreateCombatTrinketDisplay(
                slotKind,
                definition,
                equipped?.Name ?? idOrCardId,
                status,
                true));
        }

        private IReadOnlyList<UnityCombatTrinketDisplay> CombatOpponentTrinkets()
        {
            var displays = new List<UnityCombatTrinketDisplay>();
            var state = service.State.Opponent.AdvancedMechanics?.Trinkets;
            AddOpponentCombatTrinket(displays, state, TrinketSlotKind.Lesser, state?.LesserTrinketId);
            AddOpponentCombatTrinket(displays, state, TrinketSlotKind.Greater, state?.GreaterTrinketId);
            return displays;
        }

        private void AddOpponentCombatTrinket(
            List<UnityCombatTrinketDisplay> displays,
            PlayerTrinketState state,
            TrinketSlotKind slotKind,
            string idOrCardId)
        {
            if (string.IsNullOrWhiteSpace(idOrCardId))
            {
                return;
            }

            var definition = service.GetOpponentTrinketDefinition(slotKind);
            var fallbackName = state?.Equipped?
                .FirstOrDefault(item => item != null && item.SlotKind == slotKind)?.Name;
            displays.Add(CreateCombatTrinketDisplay(
                slotKind,
                definition,
                fallbackName ?? idOrCardId,
                OpponentTrinketStatus(slotKind, definition),
                service.State.Round >= OpponentTrinketActiveRound(slotKind)));
        }

        private static UnityCombatTrinketDisplay CreateCombatTrinketDisplay(
            TrinketSlotKind slotKind,
            TrinketDefinition definition,
            string fallbackName,
            string status,
            bool active)
        {
            return new UnityCombatTrinketDisplay
            {
                SlotKind = slotKind,
                Name = definition?.Name ?? fallbackName,
                Description = definition?.Text ?? string.Empty,
                CardId = definition?.CardId,
                ImagePath = definition?.ImagePath,
                Status = status,
                Active = active
            };
        }

        private void BuildCombatReplayPanel()
        {
            ClampReplayFrameIndex();
            var layout = LayoutContext();
            var playerHero = CurrentHero();
            var opponentHero = HeroDefinitionFor(service?.State?.Opponent?.HeroId);
            var panel = UnityTavernCombatReplayPanelComponent.CreatePanelHost(transform, "UnityCombatReplayPanel");
            panel.transform.SetAsLastSibling();
            var replayPanel = panel.GetComponent<UnityTavernCombatReplayPanelComponent>();
            replayPanel.ConfigureCatalogs(service.Catalogs);
            replayPanel.Build(
                service.State.LastReplay,
                activeReplayFrameIndex,
                new UnityCombatReplayPanelOptions
                {
                    ReplayPlaying = replayPlaying,
                    SpeedLabel = ReplaySpeedLabels[Mathf.Clamp(replaySpeedIndex, 0, ReplaySpeedLabels.Length - 1)],
                    MaxSteps = combatMaxSteps,
                    StatsText = CombatStatsText(),
                    StatsMetaText = CombatStatsMetaText(),
                    TimelineOpen = combatTimelineOpen,
                    ViewportWidth = layout.Width,
                    ViewportHeight = layout.Height,
                    PlayerHeroName = playerHero?.Name ?? T("我方英雄", "Friendly Hero"),
                    PlayerHeroCardId = playerHero?.HeroCardId ?? service?.State?.Player?.HeroId,
                    PlayerHeroImagePath = playerHero?.ImagePath,
                    PlayerHealth = service?.State?.Player?.Health ?? 0,
                    PlayerArmor = service?.State?.Player?.Armor ?? 0,
                    OpponentHeroName = opponentHero?.Name ?? service?.State?.Opponent?.Name ?? T("敌方英雄", "Enemy Hero"),
                    OpponentHeroCardId = opponentHero?.HeroCardId ?? service?.State?.Opponent?.HeroId,
                    OpponentHeroImagePath = opponentHero?.ImagePath,
                    OpponentHealth = service?.State?.Opponent?.Health ?? 0,
                    OpponentArmor = service?.State?.Opponent?.Armor ?? 0,
                    PlayerTrinkets = CombatPlayerTrinkets(),
                    OpponentTrinkets = CombatOpponentTrinkets(),
                    GameVersionId = service.State.GameVersionId,
                    ContentSnapshotId = service.State.ContentSnapshotId,
                    MechanicEvents = service.State.MechanicEvents,
                    TimelineFilter = replayTimelineFilter,
                    SetFrame = SetReplayFrameIndex,
                    SetTimelineFilter = SetReplayTimelineFilter,
                    TogglePlayback = ToggleReplayPlayback,
                    CycleSpeed = CycleReplaySpeed,
                    ToggleTimeline = ToggleCombatTimeline,
                    DecreaseMaxSteps = () => AdjustCombatMaxSteps(-1),
                    IncreaseMaxSteps = () => AdjustCombatMaxSteps(1),
                    RunStatistics = RunCombatStatistics,
                    Close = CloseCombatReplay
                });
        }

        private void ClampReplayFrameIndex()
        {
            var replay = service.State.LastReplay;
            if (replay == null || replay.Frames == null || replay.Frames.Count == 0)
            {
                activeReplayFrameIndex = 0;
                replayPlaying = false;
                return;
            }

            activeReplayFrameIndex = Mathf.Clamp(activeReplayFrameIndex, 0, replay.Frames.Count - 1);
        }

        private CombatTestOptions CurrentCombatOptions(bool resetBeforeRun = false, int seed = 0)
        {
            return new CombatTestOptions
            {
                Seed = seed == 0 ? DefaultCombatSeed() : seed,
                ResetBeforeRun = resetBeforeRun,
                SafetyLimit = Mathf.Max(1, combatMaxSteps)
            };
        }

        private void ToggleCombatTimeline()
        {
            if (!combatTimelineOpen)
            {
                replayWasPlayingBeforeTimeline = replayPlaying;
                replayPlaying = false;
                combatTimelineOpen = true;
            }
            else
            {
                combatTimelineOpen = false;
                replayPlaying = replayWasPlayingBeforeTimeline;
                replayWasPlayingBeforeTimeline = false;
            }

            replayPlaybackElapsed = 0f;
            Rebuild();
        }

        private void AdjustCombatMaxSteps(int direction)
        {
            var index = Array.IndexOf(CombatMaxStepChoices, combatMaxSteps);
            if (index < 0)
            {
                index = Array.FindIndex(CombatMaxStepChoices, value => value >= combatMaxSteps);
                if (index < 0)
                {
                    index = CombatMaxStepChoices.Length - 1;
                }
            }

            index = Mathf.Clamp(index + Math.Sign(direction), 0, CombatMaxStepChoices.Length - 1);
            combatMaxSteps = CombatMaxStepChoices[index];
            combatRunStats = null;
            Rebuild();
        }

        private void RunCombatStatistics()
        {
            try
            {
                lastError = null;
                var stats = new CombatRunStats
                {
                    Samples = CombatStatsSampleCount,
                    MaxSteps = Mathf.Max(1, combatMaxSteps)
                };

                var hasExistingSnapshot = service.LastCombatTestSnapshot?.BeforeCombat != null;
                for (var sample = 0; sample < CombatStatsSampleCount; sample += 1)
                {
                    var options = CurrentCombatOptions(hasExistingSnapshot || sample > 0, DefaultCombatSeed() + sample);
                    ExecuteCommand(new GameCommand(GameCommandType.RunCombatTest, options));
                    AccumulateCombatStats(stats, service.State.LastResult);
                    hasExistingSnapshot = true;
                }

                combatRunStats = stats;
                combatReplayOpen = service.State.LastReplay != null;
                toolsOpen = false;
                cardDetailOpen = false;
                activeReplayFrameIndex = 0;
                replayPlaying = false;
                replayPlaybackElapsed = 0f;
                lastFeedback = "\u5df2\u7edf\u8ba1" + CombatStatsSampleCount + "\u573a\u6218\u6597";
            }
            catch (Exception ex)
            {
                lastError = service.LocalizePlayerMessage(ex.Message);
                replayPlaying = false;
            }

            Rebuild();
        }

        private static void AccumulateCombatStats(CombatRunStats stats, CombatOutput result)
        {
            if (stats == null || result == null)
            {
                return;
            }

            if (result.SafetyStopped)
            {
                stats.OverLimits += 1;
                return;
            }

            if (result.Winner == CombatWinner.Player)
            {
                stats.Wins += 1;
            }
            else if (result.Winner == CombatWinner.Draw)
            {
                stats.Draws += 1;
            }
            else
            {
                stats.Losses += 1;
            }
        }

        private string CombatStatsText()
        {
            if (combatRunStats == null || combatRunStats.Samples <= 0)
            {
                return null;
            }

            return "\u80dc " + Percent(combatRunStats.Wins, combatRunStats.Samples)
                + "  \u5e73 " + Percent(combatRunStats.Draws, combatRunStats.Samples)
                + "  \u8d1f " + Percent(combatRunStats.Losses, combatRunStats.Samples)
                + "  \u8d85\u9650 " + Percent(combatRunStats.OverLimits, combatRunStats.Samples);
        }

        private string CombatStatsMetaText()
        {
            if (combatRunStats == null || combatRunStats.Samples <= 0)
            {
                return null;
            }

            return "\u6837\u672c " + combatRunStats.Samples + " / \u6700\u5927\u8f6e\u6b21 " + combatRunStats.MaxSteps;
        }

        private static string Percent(int count, int total)
        {
            return total <= 0 ? "0%" : Mathf.RoundToInt(count * 100f / total) + "%";
        }

        private void OpenTools()
        {
            toolsOpen = true;
            opponentPanelOpen = false;
            opponentMechanicLibraryOpen = false;
            cardLibraryOpen = false;
            heroSelectionOpen = false;
            Rebuild();
        }

        private void CloseTools()
        {
            toolsOpen = false;
            Rebuild();
        }

        private void OpenHeroSelection()
        {
            heroSelectionOpen = true;
            opponentPanelOpen = false;
            opponentMechanicLibraryOpen = false;
            cardLibraryOpen = false;
            toolsOpen = false;
            Rebuild();
        }

        private void CloseHeroSelection()
        {
            heroSelectionOpen = false;
            Rebuild();
        }

        private void OpenCardLibrary()
        {
            toolsOpen = false;
            opponentPanelOpen = false;
            opponentMechanicLibraryOpen = false;
            cardLibraryOpen = true;
            cardLibraryDetailCard = null;
            heroSelectionOpen = false;
            cardLibraryDestination = UnityCardLibraryDestination.PlayerHand;
            ResetCardLibraryFilters(CardKind.Minion);
            Rebuild();
        }

        private void OpenOpponentCardLibrary()
        {
            toolsOpen = false;
            opponentPanelOpen = false;
            opponentMechanicLibraryOpen = false;
            cardLibraryOpen = true;
            cardLibraryDetailCard = null;
            heroSelectionOpen = false;
            cardLibraryDestination = UnityCardLibraryDestination.OpponentBoard;
            opponentCardLibraryGolden = false;
            ResetCardLibraryFilters(CardKind.Minion);
            Rebuild();
        }

        private void OpenOpponentHandCardLibrary()
        {
            toolsOpen = false;
            opponentPanelOpen = false;
            opponentMechanicLibraryOpen = false;
            cardLibraryOpen = true;
            cardLibraryDetailCard = null;
            heroSelectionOpen = false;
            cardLibraryDestination = UnityCardLibraryDestination.OpponentHand;
            opponentCardLibraryGolden = false;
            ResetCardLibraryFilters(CardKind.Minion);
            Rebuild();
        }

        private void OpenOpponentStartOfCombatSpellLibrary()
        {
            toolsOpen = false;
            opponentPanelOpen = false;
            opponentMechanicLibraryOpen = false;
            cardLibraryOpen = true;
            cardLibraryDetailCard = null;
            heroSelectionOpen = false;
            cardLibraryDestination = UnityCardLibraryDestination.OpponentStartOfCombatSpell;
            ResetCardLibraryFilters(CardKind.TavernSpell);
            Rebuild();
        }

        private void CloseCardLibrary()
        {
            cardLibraryOpen = false;
            cardLibraryDetailCard = null;
            toolsOpen = true;
            cardLibraryDestination = UnityCardLibraryDestination.PlayerHand;
            opponentCardLibraryGolden = false;
            Rebuild();
        }

        private void DismissCardLibrary()
        {
            cardLibraryOpen = false;
            cardLibraryDetailCard = null;
            cardLibraryDestination = UnityCardLibraryDestination.PlayerHand;
            opponentCardLibraryGolden = false;
            Rebuild();
        }

        private void BuildToolsModal()
        {
            var modal = UnityTavernToolsModalComponent.CreateModalHost(transform, "UnityTrainerToolsOverlay");
            modal.transform.SetAsLastSibling();
            modal.GetComponent<UnityTavernToolsModalComponent>().Build("训练工具", BuildToolsContent, CloseTools);
        }

        private void BuildHeroSelectionOverlay()
        {
            var modal = UnityHeroSelectionModalComponent.CreateModalHost(transform, "UnityHeroSelectionOverlay");
            modal.transform.SetAsLastSibling();
            modal.GetComponent<UnityHeroSelectionModalComponent>().Build(
                service.HeroCatalog,
                service.State.Player.HeroId,
                true,
                ApplyHeroSelection,
                CloseHeroSelection,
                "更换英雄");
        }

        private void BuildMinionEditModal()
        {
            var target = MinionEditorTarget();
            if (target == null)
            {
                minionEditorInstanceId = null;
                return;
            }

            var modal = UnityTavernMinionEditModalComponent.CreateModalHost(transform, "UnityMinionEditOverlay");
            modal.transform.SetAsLastSibling();
            modal.GetComponent<UnityTavernMinionEditModalComponent>().Build(
                target,
                minionEditorSide,
                ApplyMinionEditorPatch,
                ApplyPatchToPlayerBoard,
                ApplyPatchToOpponentBoard,
                CloseMinionEditor);
        }

        private void BuildToolsContent(Transform parent)
        {
            if (toolsAdvancedMode)
            {
                BuildToolsAdvancedContent(parent);
                return;
            }

            BuildToolsSection(parent, "UnityToolsEconomySection", "经济", 2, grid =>
            {
                ToolButton("UnityToolsAddGoldButton", grid, "+10金币", true, () => Apply(new GameCommand(GameCommandType.DebugAddGold, 10)));
                ToolButton("UnityToolsReturnSelectedButton", grid, "回手", SelectedPlayerBoardCard() != null, ReturnSelectedToHand, "先选己方随从");
            });

            BuildToolsSection(parent, "UnityToolsCardSection", "卡牌来源", 3, grid =>
            {
                ToolButton("UnityToolsAddMinionButton", grid, "加随从", service.State.Player.Tavern.Hand.Count < HandLimit, AddFirstMinionToHand, "手牌已满");
                ToolButton("UnityToolsAddSpellButton", grid, "加法术", service.State.Player.Tavern.Hand.Count < HandLimit, AddFirstSpellToHand, "手牌已满");
                ToolButton("UnityToolsSwapHeroButton", grid, "换英雄", true, OpenHeroSelection);
            });

            BuildToolsSection(parent, "UnityToolsCardLibraryEntrySection", "卡牌库", 1, grid =>
            {
                ToolButton("UnityToolsOpenCardLibraryButton", grid, "打开卡牌库", true, OpenCardLibrary);
            });

            BuildToolsSection(parent, "UnityToolsOpponentSection", "对手", 6, grid =>
            {
                ToolButton("UnityToolsAddOpponentButton", grid, "加对手", true, OpenOpponentCardLibrary);
                ToolButton("UnityToolsAddOpponentHandButton", grid, "加敌方手牌", service.State.Opponent.Hand.Count < HandLimit, OpenOpponentHandCardLibrary, "对手手牌已满");
                ToolButton("UnityToolsRemoveOpponentButton", grid, "移除对手", SelectedOpponentCard() != null, RemoveSelectedOpponent, "先选对手随从");
                ToolButton("UnityToolsClearOpponentButton", grid, "清空对手", service.State.Opponent.Board.Count > 0, () => Apply(new GameCommand(GameCommandType.ClearOpponentBoard)), "对手战场为空");
                ToolButton("UnityToolsCopyOpponentButton", grid, "复制", service.State.Player.Board.Count > 0, () => Apply(new GameCommand(GameCommandType.CopyPlayerBoardToOpponent)), "己方战场为空");
                ToolButton("UnityToolsMirrorOpponentButton", grid, "镜像", service.State.Player.Board.Count > 0, () => Apply(new GameCommand(GameCommandType.MirrorPlayerBoardToOpponent)), "己方战场为空");
            });

            BuildToolsSection(parent, "UnityToolsAdvancedEntrySection", service.UseEnglish ? "Advanced" : "高级工具", 1, grid =>
            {
                ToolButton(
                    "UnityToolsOpenAdvancedButton",
                    grid,
                    service.UseEnglish ? "Open Advanced Tools" : "打开高级工具",
                    true,
                    () => SetToolsAdvancedMode(true));
            });
        }

        private void BuildToolsAdvancedContent(Transform parent)
        {
            BuildToolsSection(parent, "UnityToolsAdvancedBackSection", service.UseEnglish ? "Advanced" : "高级工具", 1, grid =>
            {
                ToolButton(
                    "UnityToolsBackToCommonButton",
                    grid,
                    service.UseEnglish ? "Back to Common Tools" : "返回常用工具",
                    true,
                    () => SetToolsAdvancedMode(false));
            });

            BuildToolsSection(parent, "UnityToolsTrinketDebugSection", "饰品调试", 2, grid =>
            {
                ToolButton(
                    "UnityToolsReplaceLesserTrinketButton",
                    grid,
                    "替换小饰品",
                    service.GetDebugSelectableTrinkets(TrinketSlotKind.Lesser).Count > 0,
                    () => OpenTrinketLibrary(TrinketSlotKind.Lesser),
                    "无可用小饰品");
                ToolButton(
                    "UnityToolsReplaceGreaterTrinketButton",
                    grid,
                    "替换大饰品",
                    service.GetDebugSelectableTrinkets(TrinketSlotKind.Greater).Count > 0,
                    () => OpenTrinketLibrary(TrinketSlotKind.Greater),
                    "无可用大饰品");
            });

            BuildSideModifierTools(parent, BoardSide.Player, "UnityToolsPlayerModifierSection", "己方变量");
            BuildSideModifierTools(parent, BoardSide.Opponent, "UnityToolsOpponentModifierSection", "对手变量");

            BuildToolsSection(parent, "UnityToolsSelectedSection", "选中卡牌", 4, grid =>
            {
                var selected = FindSelectedCard();
                var canPatch = selected != null && selected.CardKind != CardKind.TavernSpell;
                var unavailable = selected == null ? "先选择随从" : "法术不可修改";
                ToolButton("UnityToolsSelectedAttackPlusButton", grid, "攻+1", canPatch, () => PatchSelected(new MinionPatch { Attack = IncrementStat(selected.Attack) }), unavailable);
                ToolButton("UnityToolsSelectedAttackMinusButton", grid, "攻-1", canPatch, () => PatchSelected(new MinionPatch { Attack = selected.Attack - 1 }), unavailable);
                ToolButton("UnityToolsSelectedHealthPlusButton", grid, "血+1", canPatch, () =>
                {
                    var nextHealth = IncrementStat(selected.Health);
                    PatchSelected(new MinionPatch { Health = nextHealth, MaxHealth = Math.Max(selected.MaxHealth, nextHealth) });
                }, unavailable);
                ToolButton("UnityToolsSelectedGoldenButton", grid, "金色", canPatch, () => PatchSelected(new MinionPatch { Golden = !selected.Golden }), unavailable);
            });

            BuildToolsSection(parent, "UnityToolsCombatSection", "战斗测试", 3, grid =>
            {
                ToolButton("UnityToolsRunCombatTestButton", grid, "仅战斗调试", true, () => ApplyAndOpenReplay(new GameCommand(GameCommandType.RunCombatTest, CurrentCombatOptions())));
                ToolButton("UnityToolsSkipCombatNextTurnButton", grid, "跳过战斗进下回合", true, () => Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn)));
                ToolButton("UnityToolsResetCombatSnapshotButton", grid, "重置快照", service.HasCombatTestSnapshot, () => Apply(new GameCommand(GameCommandType.ResetCombatTestSnapshot)), "暂无战斗快照");
            });

            BuildSaveReplayTools(parent);

            BuildMechanicCoverageTools(parent);
        }

        private void BuildSaveReplayTools(Transform parent)
        {
            var summaries = service.TestScenarioSummaries;
            BuildToolsSection(parent, "UnityToolsSaveReplaySection", T("存档与回放", "Saves & Replay"), 3 + Math.Max(1, summaries.Count), grid =>
            {
                ToolButton("UnityToolsSaveVersionBadge", grid, LockedVersionBadgeText(), false, null, LockedVersionBadgeText());
                ToolButton("UnityToolsSaveScenarioButton", grid, T("保存当前场景", "Save Current"), true, () => Apply(new GameCommand(GameCommandType.SaveTestScenario, DefaultScenarioName(), new CombatTestOptions())));
                ToolButton("UnityToolsOpenReplayButton", grid, ReplayActionLabel(), HasCombatReplay(), OpenCombatReplay, T("暂无战斗回放", "No combat replay"));

                if (summaries.Count == 0)
                {
                    ToolButton("UnityToolsLoadScenarioButton", grid, T("加载场景", "Load Scenario"), false, null, "暂无已保存场景");
                    return;
                }

                for (var index = 0; index < summaries.Count; index += 1)
                {
                    var summary = summaries[index];
                    var scenarioName = summary.Name;
                    var buttonName = index == 0
                        ? "UnityToolsLoadScenarioButton"
                        : "UnityToolsLoadScenarioButton-" + SafeObjectName(scenarioName);
                    var label = ScenarioLoadText(summary);
                    ToolButton(
                        buttonName,
                        grid,
                        label,
                        summary.CanLoad,
                        () => LoadScenario(scenarioName),
                        label);
                }
            });
        }

        private string ScenarioLoadText(TestScenarioSummary summary)
        {
            if (summary == null)
            {
                return T("阻塞：存档无效", "Blocked: invalid save");
            }

            if (summary.CanLoad)
            {
                return T("可恢复 · ", "Restorable · ") + summary.Name;
            }

            switch (summary.RestoreStatus)
            {
                case TestScenarioRestoreStatus.MissingContentSnapshot:
                    return T("阻塞：缺少精确快照 · ", "Blocked: exact snapshot missing · ") + summary.Name;
                case TestScenarioRestoreStatus.MissingCardPoolSnapshot:
                    return T("阻塞：卡池快照不完整 · ", "Blocked: card-pool snapshot incomplete · ") + summary.Name;
                case TestScenarioRestoreStatus.ContentSnapshotMismatch:
                    return T("阻塞：版本或快照不匹配 · ", "Blocked: version/snapshot mismatch · ") + summary.Name;
                case TestScenarioRestoreStatus.InvalidRngState:
                    return T("阻塞：随机状态无效 · ", "Blocked: invalid RNG state · ") + summary.Name;
                default:
                    return T("阻塞：存档无效 · ", "Blocked: invalid save · ") + summary.Name;
            }
        }

        private string LockedVersionBadgeText()
        {
            return T("🔒 只读 · ", "🔒 Read-only · ") + (string.IsNullOrEmpty(service.State.GameVersionId) ? T("未知版本", "Unknown version") : service.State.GameVersionId);
        }

        private void SetToolsAdvancedMode(bool advanced)
        {
            toolsAdvancedMode = advanced;
            Rebuild();
        }

        private void BuildMechanicCoverageTools(Transform parent)
        {
            var report = service.GetMechanicCoverageReport();
            if (report == null || report.Rows == null || report.Rows.Count == 0)
            {
                return;
            }

            var section = Panel("UnityToolsMechanicCoverageSection", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureToolsSurface(section, UnityTavernUiStyle.Blue, 0.22f);
            UnityTavernUiStyle.SetPreferredHeight(section, 48f + report.Rows.Count * 76f);
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildToolsSectionHeader(section.transform, "UnityToolsMechanicCoverageSectionHeader", "UnityToolsMechanicCoverageSectionTitle", "Mechanic Coverage", UnityTavernUiStyle.Blue);
            for (var index = 0; index < report.Rows.Count; index += 1)
            {
                BuildMechanicCoverageRow(section.transform, report.Rows[index], index);
            }
        }

        private static void BuildMechanicCoverageRow(Transform parent, MechanicCoverageRow row, int index)
        {
            if (row == null)
            {
                return;
            }

            var safeName = SafeObjectName(row.System);
            var rowObject = Panel("UnityToolsMechanicCoverageRow-" + safeName, parent, index % 2 == 0 ? UnityTavernUiStyle.Panel : UnityTavernUiStyle.PanelRaised);
            ConfigureToolsSurface(rowObject, MechanicCoverageAccent(row), 0.14f);
            UnityTavernUiStyle.SetPreferredHeight(rowObject, 68f);
            var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var main = Panel("UnityToolsMechanicCoverageMain-" + safeName, rowObject.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(main, 1f, 0f);
            var mainLayout = main.AddComponent<VerticalLayoutGroup>();
            mainLayout.spacing = 3;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = true;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;

            var meta = Panel("UnityToolsMechanicCoverageMeta-" + safeName, rowObject.transform, Color.clear);
            UnityTavernUiStyle.SetFixedSize(meta, 156f, 56f);
            var metaLayout = meta.AddComponent<VerticalLayoutGroup>();
            metaLayout.spacing = 3;
            metaLayout.childControlWidth = true;
            metaLayout.childControlHeight = true;
            metaLayout.childForceExpandWidth = true;
            metaLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityToolsMechanicCoverageSystem-" + safeName, main.transform, row.System, 12, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 17f);

            var notes = UiFactory.Label("UnityToolsMechanicCoverageNotes-" + safeName, main.transform, row.Notes, 10, FontStyle.Normal);
            notes.color = UnityTavernUiStyle.MutedText;
            notes.horizontalOverflow = HorizontalWrapMode.Wrap;
            notes.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(notes.gameObject, 34f);

            var confidence = UiFactory.Label("UnityToolsMechanicCoverageConfidence-" + safeName, meta.transform, row.DesignConfidence, 10, FontStyle.Bold);
            confidence.alignment = TextAnchor.MiddleRight;
            confidence.color = UnityTavernUiStyle.Text;
            confidence.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(confidence.gameObject, 16f);

            var status = UiFactory.Label("UnityToolsMechanicCoverageStatus-" + safeName, meta.transform, MechanicCoverageStatus(row).Replace(" / UI", "\nUI"), 9, FontStyle.Normal);
            status.alignment = TextAnchor.UpperRight;
            status.color = UnityTavernUiStyle.Gold;
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            status.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(status.gameObject, 34f);
        }

        private static Color MechanicCoverageAccent(MechanicCoverageRow row)
        {
            return row.TestCovered && row.UiVisible ? UnityTavernUiStyle.Green : UnityTavernUiStyle.Gold;
        }

        private static string MechanicCoverageStatus(MechanicCoverageRow row)
        {
            return "Config " + YesNo(row.Configurable) +
                " / Combat " + YesNo(row.CombatConsumed) +
                " / UI " + YesNo(row.UiVisible) +
                " / Tests " + YesNo(row.TestCovered);
        }

        private static string YesNo(bool value)
        {
            return value ? "yes" : "no";
        }

        private void BuildSideModifierTools(Transform parent, BoardSide side, string sectionName, string title)
        {
            if (side == BoardSide.Player)
            {
                BuildPlayerUndeadAttackStatusCard(parent);
            }

            BuildToolsSection(parent, sectionName, title, 12, grid =>
            {
                SideModifierStepper(grid, side, SideCombatModifierKind.BeetleAttackBonus, "Beetle Attack");
                SideModifierStepper(grid, side, SideCombatModifierKind.BeetleHealthBonus, "Beetle Health");
                SideModifierStepper(grid, side, SideCombatModifierKind.SpellsCastThisGame, "法术");
                SideModifierStepper(grid, side, SideCombatModifierKind.SpellPower, "法强");
                SideModifierStepper(grid, side, SideCombatModifierKind.TavernSpellBonusAttack, "酒法攻");
                SideModifierStepper(grid, side, SideCombatModifierKind.TavernSpellBonusHealth, "酒法血");
                SideModifierStepper(grid, side, SideCombatModifierKind.BloodGemAttackBonus, "宝石攻");
                SideModifierStepper(grid, side, SideCombatModifierKind.BloodGemHealthBonus, "宝石血");
                SideModifierStepper(grid, side, SideCombatModifierKind.UndeadAttackBonus, "亡灵攻");
                SideModifierStepper(grid, side, SideCombatModifierKind.EternalKnightDeaths, "永恒死");
                SideModifierStepper(grid, side, SideCombatModifierKind.AstralAutomatonSummons, "星元机");
                SideModifierStepper(grid, side, SideCombatModifierKind.FriendlyMinionDeathsThisGame, "复仇死");
            });
        }

        private void BuildPlayerUndeadAttackStatusCard(Transform parent)
        {
            var card = Panel("UnityToolsPlayerUndeadAttackStatusCard", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureToolsSurface(card, UnityTavernUiStyle.Gold, 0.30f);
            UnityTavernUiStyle.SetPreferredHeight(card, 126f);

            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var value = service.State.Player.Tavern == null ? 0 : Math.Max(0, service.State.Player.Tavern.UndeadAttackBonus);
            AddStatusCardLine(card.transform, "UnityToolsPlayerUndeadAttackStatusValue", T("亡灵攻击加成：+", "Undead Attack Bonus: +") + value, 14, FontStyle.Bold, UnityTavernUiStyle.Gold);
            AddStatusCardLine(card.transform, "UnityToolsPlayerUndeadAttackStatusSource", T("来源：本局永久生效", "Source: permanent for this match"), 12, FontStyle.Normal, UnityTavernUiStyle.Text);
            AddStatusCardLine(card.transform, "UnityToolsPlayerUndeadAttackStatusRecent", RecentUndeadAttackRewardText(), 12, FontStyle.Normal, UnityTavernUiStyle.Text);
            AddStatusCardLine(card.transform, "UnityToolsPlayerUndeadAttackStatusImpact", T("影响：后续亡灵/酒馆成长", "Affects future Undead and Tavern growth"), 12, FontStyle.Normal, UnityTavernUiStyle.MutedText);
            AddStatusCardLine(card.transform, "UnityToolsPlayerUndeadAttackStatusManual", T("手动修改会立即重算已有战场、手牌和商店牌", "Manual changes immediately recalculate existing board, hand, and shop cards"), 12, FontStyle.Normal, UnityTavernUiStyle.MutedText);
        }

        private string RecentUndeadAttackRewardText()
        {
            var rewards = service.State.LastReplay?.PlayerRewards;
            var amount = rewards == null
                ? 0
                : rewards
                    .Where(reward => reward != null && reward.Type == CombatRewardType.ImproveUndeadAttack)
                    .Sum(reward => Math.Max(0, reward.Amount));
            return amount > 0
                ? T("最近变化：战斗奖励使亡灵攻击 +", "Latest change: combat reward increased Undead Attack by ") + amount
                : T("最近变化：无", "Latest change: none");
        }

        private static void AddStatusCardLine(Transform parent, string name, string text, int size, FontStyle style, Color color)
        {
            var label = UiFactory.Label(name, parent, text, size, style);
            label.color = color;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, style == FontStyle.Bold ? 20f : 17f);
        }

        private void SideModifierStepper(Transform grid, BoardSide side, SideCombatModifierKind kind, string label)
        {
            label = SideModifierSemanticLabel(kind);
            var value = SideModifierValue(side, kind);
            var prefix = side == BoardSide.Player ? "Player" : "Opponent";
            var row = Panel("UnityTools" + prefix + kind + "Editor", grid, UnityTavernUiStyle.PanelQuiet);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(6, 6, 4, 4);
            rowLayout.spacing = 4;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var title = UiFactory.Label("UnityTools" + prefix + kind + "Label", row.transform, label, 11, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

            var inputObject = new GameObject("UnityTools" + prefix + kind + "Input", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(row.transform, false);
            UnityTavernUiStyle.SetFixedSize(inputObject, 54f, 32f);
            UnityTavernUiStyle.ConfigureSurface(inputObject, UnityTavernUiStyle.PanelRaised, true);
            var input = inputObject.GetComponent<InputField>();
            input.contentType = InputField.ContentType.IntegerNumber;
            input.caretColor = UnityTavernUiStyle.Text;
            input.textComponent = UiFactory.Label("UnityTools" + prefix + kind + "InputText", inputObject.transform, value.ToString(), 13, FontStyle.Bold);
            UnityTavernUiStyle.Stretch(input.textComponent.rectTransform);
            input.textComponent.alignment = TextAnchor.MiddleCenter;
            input.text = value.ToString();
            input.onEndEdit.AddListener(raw =>
            {
                if (int.TryParse(raw, out var parsed))
                {
                    Apply(new GameCommand(GameCommandType.SetSideCombatModifier, side, kind, parsed));
                }
                else
                {
                    Rebuild();
                }
            });
            ToolButton(
                "UnityTools" + prefix + kind + "PlusButton",
                row.transform,
                "+",
                true,
                () => Apply(new GameCommand(GameCommandType.AdjustSideCombatModifier, side, kind, 1)));
            ToolButton(
                "UnityTools" + prefix + kind + "MinusButton",
                row.transform,
                "-",
                value > SideModifierService.MinimumValue(kind),
                () => Apply(new GameCommand(GameCommandType.AdjustSideCombatModifier, side, kind, -1)),
                "已到最低值");
        }

        private static string SideModifierSemanticLabel(SideCombatModifierKind kind)
        {
            switch (kind)
            {
                case SideCombatModifierKind.SpellsCastThisGame:
                    return "\u672c\u5c40\u9152\u9986\u6cd5\u672f\u6570";
                case SideCombatModifierKind.SpellPower:
                    return "\u6218\u6597\u6cd5\u5f3a";
                case SideCombatModifierKind.TavernSpellBonusAttack:
                    return "\u9152\u9986\u6cd5\u672f\u653b";
                case SideCombatModifierKind.TavernSpellBonusHealth:
                    return "\u9152\u9986\u6cd5\u672f\u8840";
                case SideCombatModifierKind.BloodGemAttackBonus:
                    return "\u5b9d\u77f3\u653b(\u6253\u51fa\u65f6)";
                case SideCombatModifierKind.BloodGemHealthBonus:
                    return "\u5b9d\u77f3\u8840(\u6253\u51fa\u65f6)";
                case SideCombatModifierKind.BeetleAttackBonus:
                    return "\u7532\u866b\u8d28\u91cf\u653b";
                case SideCombatModifierKind.BeetleHealthBonus:
                    return "\u7532\u866b\u8d28\u91cf\u8840";
                case SideCombatModifierKind.UndeadAttackBonus:
                    return "\u4ea1\u7075\u653b\u51fb\u6210\u957f";
                case SideCombatModifierKind.EternalKnightDeaths:
                    return "\u6c38\u6052\u9a91\u58eb\u6b7b\u4ea1";
                case SideCombatModifierKind.AstralAutomatonSummons:
                    return "\u661f\u5143\u673a\u53ec\u5524";
                case SideCombatModifierKind.FriendlyMinionDeathsThisGame:
                    return "\u672c\u5c40\u53cb\u65b9\u6b7b\u4ea1";
                default:
                    return kind.ToString();
            }
        }

        private int SideModifierValue(BoardSide side, SideCombatModifierKind kind)
        {
            if (side == BoardSide.Player && kind == SideCombatModifierKind.UndeadAttackBonus)
            {
                return service.State.Player.Tavern == null
                    ? 0
                    : Math.Max(0, service.State.Player.Tavern.UndeadAttackBonus);
            }

            var modifiers = side == BoardSide.Player
                ? service.State.Player.CombatModifiers
                : service.State.Opponent.CombatModifiers;
            if (modifiers == null)
            {
                return 0;
            }

            switch (kind)
            {
                case SideCombatModifierKind.SpellsCastThisGame:
                    return modifiers.SpellsCastThisGame;
                case SideCombatModifierKind.SpellPower:
                    return modifiers.SpellPower;
                case SideCombatModifierKind.BloodGemAttackBonus:
                    return modifiers.BloodGemAttackBonus;
                case SideCombatModifierKind.BloodGemHealthBonus:
                    return modifiers.BloodGemHealthBonus;
                case SideCombatModifierKind.BeetleAttackBonus:
                    return Math.Max(2, modifiers.BeetleAttackBonus);
                case SideCombatModifierKind.BeetleHealthBonus:
                    return Math.Max(2, modifiers.BeetleHealthBonus);
                case SideCombatModifierKind.UndeadAttackBonus:
                    return modifiers.UndeadAttackBonus;
                case SideCombatModifierKind.EternalKnightDeaths:
                    return modifiers.EternalKnightDeaths;
                case SideCombatModifierKind.AstralAutomatonSummons:
                    return modifiers.AstralAutomatonSummons;
                case SideCombatModifierKind.FriendlyMinionDeathsThisGame:
                    return modifiers.FriendlyMinionDeathsThisGame;
                case SideCombatModifierKind.TavernSpellBonusAttack:
                    return modifiers.TavernSpellBonusAttack;
                case SideCombatModifierKind.TavernSpellBonusHealth:
                    return modifiers.TavernSpellBonusHealth;
                default:
                    return 0;
            }
        }

        private static void BuildToolsSection(Transform parent, string name, string title, int itemCount, Action<Transform> buildGrid)
        {
            var section = Panel(name, parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureToolsSurface(section, UnityTavernUiStyle.Gold, 0.22f);
            var rowCount = Mathf.Max(1, Mathf.CeilToInt(itemCount / 2f));
            UnityTavernUiStyle.SetPreferredHeight(section, 54f + rowCount * 54f);
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            BuildToolsSectionHeader(section.transform, name + "Header", name + "Title", title, UnityTavernUiStyle.Gold);

            var grid = new GameObject(name + "Grid", typeof(RectTransform));
            grid.transform.SetParent(section.transform, false);
            UnityTavernUiStyle.SetFlexible(grid, 1f, 1f);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(0, 0, 0, 0);
            gridLayout.spacing = new Vector2(8f, 6f);
            gridLayout.cellSize = new Vector2(138f, UnityTavernUiStyle.TouchHeight);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            buildGrid?.Invoke(grid.transform);
        }

        private static void ConfigureToolsSurface(GameObject target, Color accentColor, float accentAlpha)
        {
            var current = UnityTavernUiStyle.EnsureComponent<Image>(target).color;
            UnityTavernUiStyle.ConfigureSurface(target, current.a > 0f ? current : UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.ConfigureOutline(
                target,
                new Color(accentColor.r, accentColor.g, accentColor.b, accentAlpha),
                new Vector2(1f, -1f));
        }

        private static Transform BuildToolsSectionHeader(Transform parent, string headerName, string titleName, string title, Color accentColor)
        {
            var header = Panel(headerName, parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(header, 32f);
            var layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 8, 4, 4);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var accent = new GameObject(headerName + "Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(header.transform, false);
            UnityTavernUiStyle.SetFixedSize(accent, 4f, 18f);
            UnityTavernUiStyle.ConfigureSurface(accent, accentColor);

            var heading = UiFactory.Label(titleName, header.transform, title, 14, FontStyle.Bold);
            heading.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(heading.gameObject, 1f, 0f);
            return header.transform;
        }

        private static Button ToolButton(string name, Transform parent, string text, bool interactable, Action onClick, string disabledText = null)
        {
            var displayText = !interactable && !string.IsNullOrWhiteSpace(disabledText) ? disabledText : text;
            var button = ActionButton(name, parent, displayText, onClick, role: UnityTavernActionButtonRole.Utility, interactable: interactable);
            button.GetComponentInChildren<Text>(true).fontSize = 14;
            return button;
        }

        private void BuildCardLibraryOverlay()
        {
            cardLibraryAddButtons.Clear();
            var overlay = Panel("UnityCardLibraryOverlay", transform, new Color(0f, 0f, 0f, 0.68f));
            overlay.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;

            var panel = Panel("UnityCardLibraryPanel", overlay.transform, UnityTavernUiStyle.SurfaceDark);
            ConfigureToolsSurface(panel, UnityTavernUiStyle.Gold, 0.30f);
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityCardLibraryStarLantern", CardLibraryAccent());
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.035f, 0.055f);
            rect.anchorMax = new Vector2(0.965f, 0.94f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 14);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildCardLibraryHeader(panel.transform);

            var body = Panel("UnityCardLibraryBody", panel.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(body, 1f, 1f);
            var bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 12;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = false;
            bodyLayout.childForceExpandHeight = true;

            BuildCardLibraryTierPanel(body.transform);
            BuildCardLibraryCenterPanel(body.transform);
            BuildCardLibraryTypePanel(body.transform);
        }

        private void SelectToolsAcquisitionKind(CardKind kind)
        {
            toolsAcquisitionKind = kind;
            toolsAcquisitionTierFilter = 0;
            toolsAcquisitionTribeFilter = Tribe.All;
            toolsAcquisitionSearchText = string.Empty;
            toolsHeroPowerCategoryFilter = null;
            toolsHeroPowerEligibilityFilter = null;
            opponentCardLibraryGolden = false;
            cardLibraryVisibleLimit = CardLibraryPageSize;
        }

        private void ResetCardLibraryFilters(CardKind kind)
        {
            SelectToolsAcquisitionKind(kind);
            toolsShowAllCards = false;
            cardLibraryScrollPosition = 1f;
            restoreCardLibraryScrollPosition = false;
        }

        private string CardLibraryKindTitle()
        {
            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentStartOfCombatSpell)
            {
                return "敌方战斗开始法术";
            }

            switch (toolsAcquisitionKind)
            {
                case CardKind.TavernSpell: return "酒馆法术";
                case CardKind.Hero: return "英雄";
                case CardKind.HeroPower: return "英雄技能";
                case CardKind.HeroBuddy: return "英雄宝宝";
                default: return "随从";
            }
        }

        private Color CardLibraryAccent()
        {
            return CardKindAccent(toolsAcquisitionKind);
        }

        private static Color CardKindAccent(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Hero:
                    return UnityTavernUiStyle.Red;
                case CardKind.TavernSpell:
                case CardKind.HeroPower:
                    return UnityTavernUiStyle.Blue;
                case CardKind.HeroBuddy:
                    return UnityTavernUiStyle.Green;
                default:
                    return UnityTavernUiStyle.Gold;
            }
        }

        private void BuildCardLibraryHeader(Transform parent)
        {
            var header = Panel("UnityCardLibraryHeader", parent, UnityTavernUiStyle.SurfaceRaised);
            ConfigureToolsSurface(header, CardLibraryAccent(), 0.22f);
            UnityTavernUiStyle.SetPreferredHeight(header, 64f);
            var layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 8;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (cardLibraryDestination != UnityCardLibraryDestination.OpponentStartOfCombatSpell)
            {
                CardLibraryBadgeButton("UnityCardLibraryMinionTab", header.transform, "随从", "随", null, toolsAcquisitionKind == CardKind.Minion, new Vector2(74f, 48f), UnityTavernUiStyle.Gold, () =>
                {
                    SelectToolsAcquisitionKind(CardKind.Minion);
                    Rebuild();
                });

                CardLibraryBadgeButton("UnityCardLibrarySpellTab", header.transform, "酒馆法术", "法", null, toolsAcquisitionKind == CardKind.TavernSpell, new Vector2(86f, 48f), UnityTavernUiStyle.Blue, () =>
                {
                    SelectToolsAcquisitionKind(CardKind.TavernSpell);
                    Rebuild();
                });

                CardLibraryBadgeButton("UnityCardLibraryHeroTab", header.transform, "英雄", "英", null, toolsAcquisitionKind == CardKind.Hero, new Vector2(74f, 48f), UnityTavernUiStyle.Red, () =>
                {
                    SelectToolsAcquisitionKind(CardKind.Hero);
                    Rebuild();
                });

                CardLibraryBadgeButton("UnityCardLibraryHeroPowerTab", header.transform, "英雄技能", "技", null, toolsAcquisitionKind == CardKind.HeroPower, new Vector2(86f, 48f), UnityTavernUiStyle.Blue, () =>
                {
                    SelectToolsAcquisitionKind(CardKind.HeroPower);
                    Rebuild();
                });

                CardLibraryBadgeButton("UnityCardLibraryHeroBuddyTab", header.transform, "英雄宝宝", "宝", null, toolsAcquisitionKind == CardKind.HeroBuddy, new Vector2(86f, 48f), UnityTavernUiStyle.Green, () =>
                {
                    SelectToolsAcquisitionKind(CardKind.HeroBuddy);
                    Rebuild();
                });
            }

            var title = UiFactory.Label("UnityCardLibraryTitle", header.transform, CardLibraryKindTitle(), 20, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

            var summary = UiFactory.Label("UnityCardLibraryCountText", header.transform, ToolsAcquisitionSubtitle(FilteredToolsAcquisitionChoices().Count()), 14, FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleRight;
            summary.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(summary.gameObject, 270f, 32f);

            var showAll = ToolButton("UnityCardLibraryShowAllToggle", header.transform, toolsShowAllCards ? "显示全部" : "当前局", true, () =>
            {
                toolsShowAllCards = !toolsShowAllCards;
                NormalizeToolsAcquisitionTribeFilter();
                Rebuild();
            });
            UnityTavernUiStyle.SetFixedSize(showAll.gameObject, 92f, UnityTavernUiStyle.TouchHeight);
            UnityTavernUiStyle.EnsureComponent<Image>(showAll.gameObject).color = toolsShowAllCards
                ? Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Blue, 0.42f)
                : UnityTavernUiStyle.PanelQuiet;

            var back = ToolButton("UnityCardLibraryBackButton", header.transform, "返回工具", true, CloseCardLibrary);
            UnityTavernUiStyle.SetFixedSize(back.gameObject, 104f, UnityTavernUiStyle.TouchHeight);
            var close = ToolButton("UnityCardLibraryCloseButton", header.transform, "关闭", true, DismissCardLibrary);
            UnityTavernUiStyle.SetFixedSize(close.gameObject, 76f, UnityTavernUiStyle.TouchHeight);
            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentBoard && toolsAcquisitionKind == CardKind.Minion)
            {
                var golden = ToolButton("UnityCardLibraryOpponentGoldenToggle", header.transform, opponentCardLibraryGolden ? "金色" : "普通", true, () =>
                {
                    opponentCardLibraryGolden = !opponentCardLibraryGolden;
                    Rebuild();
                });
                UnityTavernUiStyle.SetFixedSize(golden.gameObject, 84f, UnityTavernUiStyle.TouchHeight);
                UnityTavernUiStyle.EnsureComponent<Image>(golden.gameObject).color = opponentCardLibraryGolden
                    ? Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.42f)
                    : UnityTavernUiStyle.PanelQuiet;
                UnityTavernUiStyle.ConfigureOutline(
                    golden.gameObject,
                    opponentCardLibraryGolden ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.86f) : new Color(0f, 0f, 0f, 0.24f),
                    opponentCardLibraryGolden ? new Vector2(2f, -2f) : new Vector2(1f, -1f));
            }
        }

        private void BuildCardLibraryTierPanel(Transform parent)
        {
            if (toolsAcquisitionKind == CardKind.Hero)
            {
                BuildCardLibraryHeroInfoPanel(parent, "当前英雄", CurrentHeroName());
                return;
            }

            if (toolsAcquisitionKind == CardKind.HeroPower)
            {
                BuildCardLibraryHeroPowerCategoryPanel(parent);
                return;
            }

            var panel = Panel("UnityCardLibraryTierPanel", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureCardLibrarySidePanel(panel, 172f, UnityTavernUiStyle.Gold);
            BuildCardLibraryPanelTitle(panel.transform, "UnityCardLibraryTierTitle", "等级", UnityTavernUiStyle.Gold);

            var grid = new GameObject("UnityCardLibraryTierGrid", typeof(RectTransform));
            grid.transform.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetFlexible(grid, 1f, 1f);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(8, 8, 8, 8);
            gridLayout.spacing = new Vector2(8f, 8f);
            gridLayout.cellSize = new Vector2(68f, 68f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            CardLibraryBadgeButton(
                "UnityCardLibraryTierAllButton",
                grid.transform,
                "全部",
                "全",
                null,
                toolsAcquisitionTierFilter == 0,
                new Vector2(68f, 68f),
                UnityTavernUiStyle.Gold,
                () =>
            {
                toolsAcquisitionTierFilter = 0;
                Rebuild();
            });

            for (var tier = 1; tier <= 7; tier += 1)
            {
                var capturedTier = tier;
                CardLibraryBadgeButton(
                    "UnityCardLibraryTier" + tier + "Button",
                    grid.transform,
                    tier + "本",
                    string.Empty,
                    CardLibraryTierIcon(tier),
                    toolsAcquisitionTierFilter == tier,
                    new Vector2(68f, 68f),
                    UnityTavernUiStyle.Gold,
                    () =>
                {
                    toolsAcquisitionTierFilter = capturedTier;
                    Rebuild();
                });
            }
        }

        private void BuildCardLibraryHeroPowerCategoryPanel(Transform parent)
        {
            var panel = Panel("UnityCardLibraryHeroPowerCategoryPanel", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureCardLibrarySidePanel(panel, 172f, UnityTavernUiStyle.Blue);
            BuildCardLibraryPanelTitle(panel.transform, "UnityCardLibraryHeroPowerCategoryTitle", "分类", UnityTavernUiStyle.Blue);

            var grid = new GameObject("UnityCardLibraryHeroPowerCategoryGrid", typeof(RectTransform));
            grid.transform.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetFlexible(grid, 1f, 1f);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(8, 8, 8, 8);
            gridLayout.spacing = new Vector2(8f, 8f);
            gridLayout.cellSize = new Vector2(68f, 58f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            CardLibraryBadgeButton("UnityCardLibraryHeroPowerCategoryAllButton", grid.transform, "全部", "全", null, !toolsHeroPowerCategoryFilter.HasValue, new Vector2(68f, 58f), UnityTavernUiStyle.Blue, () =>
            {
                toolsHeroPowerCategoryFilter = null;
                Rebuild();
            });

            foreach (HeroPowerCategory category in Enum.GetValues(typeof(HeroPowerCategory)))
            {
                var capturedCategory = category;
                CardLibraryBadgeButton(
                    "UnityCardLibraryHeroPowerCategory" + category + "Button",
                    grid.transform,
                    HeroPowerCategoryName(category),
                    HeroPowerCategorySymbol(category),
                    null,
                    toolsHeroPowerCategoryFilter == category,
                    new Vector2(68f, 58f),
                    UnityTavernUiStyle.Blue,
                    () =>
                {
                    toolsHeroPowerCategoryFilter = capturedCategory;
                    Rebuild();
                });
            }
        }

        private void BuildCardLibraryHeroInfoPanel(Transform parent, string title, string value)
        {
            var panel = Panel("UnityCardLibraryHeroInfoPanel", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureCardLibrarySidePanel(panel, 172f, UnityTavernUiStyle.Red);
            BuildCardLibraryPanelTitle(panel.transform, "UnityCardLibraryHeroInfoTitle", title, UnityTavernUiStyle.Red);

            var info = UiFactory.Label("UnityCardLibraryHeroInfoText", panel.transform, value, 13, FontStyle.Bold);
            info.alignment = TextAnchor.MiddleCenter;
            info.color = UnityTavernUiStyle.MutedText;
            info.horizontalOverflow = HorizontalWrapMode.Wrap;
            info.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFlexible(info.gameObject, 1f, 1f);
        }

        private void BuildCardLibraryCenterPanel(Transform parent)
        {
            var allChoices = FilteredToolsAcquisitionChoices().ToList();
            var visibleLimit = toolsAcquisitionTierFilter > 0 || !string.IsNullOrWhiteSpace(toolsAcquisitionSearchText)
                ? allChoices.Count
                : cardLibraryVisibleLimit;
            var choices = allChoices.Take(visibleLimit).ToList();
            var center = Panel("UnityCardLibraryCenterPanel", parent, UnityTavernUiStyle.Panel);
            ConfigureToolsSurface(center, CardLibraryAccent(), 0.20f);
            UnityTavernUiStyle.SetFlexible(center, 1f, 1f);
            var layout = center.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 12);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildCardLibrarySearch(center.transform);

            var content = UiFactory.ScrollView("UnityCardLibraryScroll", center.transform, UnityTavernUiStyle.PanelQuiet, out var scrollRect);
            cardLibraryScrollRect = scrollRect;
            if (!restoreCardLibraryScrollPosition)
            {
                cardLibraryScrollPosition = 1f;
            }
            scrollRect.onValueChanged.AddListener(position => cardLibraryScrollPosition = position.y);
            UnityTavernUiStyle.SetFlexible(content.gameObject, 1f, 1f);

            if (choices.Count == 0)
            {
                var empty = UiFactory.Label("UnityCardLibraryEmpty", content, "没有符合筛选条件的卡牌。", 14, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 80f);
                return;
            }

            var gridLayout = content.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(12, 12, 12, 18);
            gridLayout.spacing = new Vector2(16f, 18f);
            gridLayout.cellSize = toolsAcquisitionKind == CardKind.TavernSpell || toolsAcquisitionKind == CardKind.HeroPower
                ? new Vector2(150f, 244f)
                : new Vector2(148f, 240f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = CardLibraryColumnCount();

            for (var index = 0; index < choices.Count; index += 1)
            {
                BuildCardLibraryCard(content, choices[index], index);
            }

            if (choices.Count < allChoices.Count)
            {
                var loadMore = ToolButton(
                    "UnityCardLibraryLoadMoreButton",
                    content,
                    "加载更多（" + choices.Count + "/" + allChoices.Count + "）",
                    true,
                    LoadMoreCardLibraryChoices);
                UnityTavernUiStyle.SetFixedSize(loadMore.gameObject, 148f, UnityTavernUiStyle.TouchHeight);
            }
        }

        private int CardLibraryColumnCount()
        {
            var layout = LayoutContext();
            if (layout.IsCompact)
            {
                return 3;
            }

            return layout.Width >= 1800f ? 5 : layout.Width >= 1500f ? 4 : 3;
        }

        private void LoadMoreCardLibraryChoices()
        {
            PreserveCardLibraryScrollForNextRebuild();
            cardLibraryVisibleLimit = toolsAcquisitionKind == CardKind.HeroPower ||
                                      toolsAcquisitionKind == CardKind.HeroBuddy
                ? FilteredToolsAcquisitionChoices().Count()
                : cardLibraryVisibleLimit + CardLibraryPageSize;
            Rebuild();
        }

        private void BuildCardLibrarySearch(Transform parent)
        {
            var row = Panel("UnityCardLibrarySearchRow", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(row, 56f);
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(row).flexibleHeight = 0f;
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(4, 4, 4, 4);
            rowLayout.spacing = 8;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var inputObject = new GameObject("UnityCardLibrarySearchInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(row.transform, false);
            var inputElement = UnityTavernUiStyle.EnsureComponent<LayoutElement>(inputObject);
            inputElement.flexibleWidth = 1f;
            inputElement.minHeight = UnityTavernUiStyle.TouchHeight;
            inputElement.preferredHeight = UnityTavernUiStyle.TouchHeight;

            var input = inputObject.GetComponent<InputField>();
            UnityTavernUiStyle.ConfigureInputField(input, UnityTavernUiStyle.ArcaneBlue);
            input.caretColor = UnityTavernUiStyle.Text;
            input.selectionColor = new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.35f);
            input.textComponent = UiFactory.Label("UnityCardLibrarySearchText", inputObject.transform, string.Empty, 14);
            UnityTavernUiStyle.Stretch(input.textComponent.rectTransform);
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            input.textComponent.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.textComponent.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.placeholder = UiFactory.Label("UnityCardLibrarySearchPlaceholder", inputObject.transform, "搜索名称、描述或关键词", 14);
            UnityTavernUiStyle.Stretch(input.placeholder.rectTransform);
            input.placeholder.color = UnityTavernUiStyle.MutedText;
            input.placeholder.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.placeholder.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.text = toolsAcquisitionSearchText;
            input.onEndEdit.AddListener(value =>
            {
                toolsAcquisitionSearchText = value ?? string.Empty;
                Rebuild();
            });

            var clear = ToolButton(
                "UnityCardLibraryClearSearchButton",
                row.transform,
                "清空搜索",
                !string.IsNullOrWhiteSpace(toolsAcquisitionSearchText),
                () =>
                {
                    toolsAcquisitionSearchText = string.Empty;
                    Rebuild();
                });
            UnityTavernUiStyle.SetFixedSize(clear.gameObject, 96f, UnityTavernUiStyle.TouchHeight);
        }

        private void BuildCardLibraryTypePanel(Transform parent)
        {
            var panel = Panel("UnityCardLibraryTypePanel", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureCardLibrarySidePanel(panel, 210f, UnityTavernUiStyle.Green);
            BuildCardLibraryPanelTitle(panel.transform, "UnityCardLibraryTypeTitle", toolsAcquisitionKind == CardKind.HeroPower ? "替换" : toolsAcquisitionKind == CardKind.Hero ? "说明" : "类型", UnityTavernUiStyle.Green);

            var grid = new GameObject("UnityCardLibraryTypeGrid", typeof(RectTransform));
            grid.transform.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetFlexible(grid, 1f, 1f);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(10, 10, 10, 10);
            gridLayout.spacing = new Vector2(8f, 8f);
            gridLayout.cellSize = new Vector2(88f, 58f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            if (toolsAcquisitionKind == CardKind.Hero)
            {
                CardLibraryBadgeButton("UnityCardLibraryHeroAllButton", grid.transform, "全部英雄", "英", null, true, new Vector2(88f, 58f), UnityTavernUiStyle.Red, () => { });
                return;
            }

            if (toolsAcquisitionKind == CardKind.HeroPower)
            {
                CardLibraryBadgeButton("UnityCardLibraryEligibilityAllButton", grid.transform, "全部", "全", null, !toolsHeroPowerEligibilityFilter.HasValue, new Vector2(88f, 58f), UnityTavernUiStyle.Green, () =>
                {
                    toolsHeroPowerEligibilityFilter = null;
                    Rebuild();
                });

                foreach (HeroPowerReplacementEligibility eligibility in Enum.GetValues(typeof(HeroPowerReplacementEligibility)))
                {
                    var capturedEligibility = eligibility;
                    CardLibraryBadgeButton(
                        "UnityCardLibraryEligibility" + eligibility + "Button",
                        grid.transform,
                        HeroPowerEligibilityName(eligibility),
                        HeroPowerEligibilitySymbol(eligibility),
                        null,
                        toolsHeroPowerEligibilityFilter == eligibility,
                        new Vector2(88f, 58f),
                        UnityTavernUiStyle.Green,
                        () =>
                    {
                        toolsHeroPowerEligibilityFilter = capturedEligibility;
                        Rebuild();
                    });
                }
                return;
            }

            if (toolsAcquisitionKind == CardKind.TavernSpell)
            {
                CardLibraryBadgeButton("UnityCardLibraryTypeAllButton", grid.transform, "全部", "全", null, toolsAcquisitionTribeFilter == Tribe.All, new Vector2(88f, 58f), UnityTavernUiStyle.Blue, () =>
                {
                    toolsAcquisitionTribeFilter = Tribe.All;
                    Rebuild();
                });
                CardLibraryBadgeButton("UnityCardLibraryTavernSpellTypeButton", grid.transform, "通用法术", "法", null, toolsAcquisitionTribeFilter == Tribe.None, new Vector2(88f, 58f), UnityTavernUiStyle.Blue, () =>
                {
                    toolsAcquisitionTribeFilter = Tribe.None;
                    Rebuild();
                });
                foreach (var tribe in VisibleLibraryTribes().Where(tribe => tribe != Tribe.None))
                {
                    var capturedTribe = tribe;
                    CardLibraryBadgeButton(
                        "UnityCardLibrarySpellTribe" + tribe + "Button",
                        grid.transform,
                        TribeName(tribe),
                        TribeSymbol(tribe),
                        null,
                        toolsAcquisitionTribeFilter == tribe,
                        new Vector2(88f, 58f),
                        TribeAccent(tribe),
                        () =>
                    {
                        toolsAcquisitionTribeFilter = capturedTribe;
                        Rebuild();
                    });
                }
                return;
            }

            CardLibraryBadgeButton(
                "UnityCardLibraryTribeAllButton",
                grid.transform,
                "全部",
                "全",
                null,
                toolsAcquisitionTribeFilter == Tribe.All,
                new Vector2(88f, 58f),
                UnityTavernUiStyle.Green,
                () =>
            {
                toolsAcquisitionTribeFilter = Tribe.All;
                Rebuild();
            });

            foreach (var tribe in VisibleLibraryTribes())
            {
                var capturedTribe = tribe;
                CardLibraryBadgeButton(
                    "UnityCardLibraryTribe" + tribe + "Button",
                    grid.transform,
                    TribeName(tribe),
                    TribeSymbol(tribe),
                    null,
                    toolsAcquisitionTribeFilter == tribe,
                    new Vector2(88f, 58f),
                    TribeAccent(tribe),
                    () =>
                {
                    toolsAcquisitionTribeFilter = capturedTribe;
                    Rebuild();
                });
            }
        }

        private static void ConfigureCardLibrarySidePanel(GameObject panel, float width, Color accentColor)
        {
            ConfigureToolsSurface(panel, accentColor, 0.20f);
            var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(panel);
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 1f;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void BuildCardLibraryPanelTitle(Transform parent, string name, string text, Color accentColor)
        {
            var header = Panel(name + "Header", parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(header, 38f);
            var layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 5, 5);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var accent = new GameObject(name + "Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(header.transform, false);
            UnityTavernUiStyle.SetFixedSize(accent, 4f, 24f);
            UnityTavernUiStyle.ConfigureSurface(accent, accentColor);

            var label = UiFactory.Label(name, header.transform, text, 15, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(label.gameObject, 1f, 0f);
        }

        private void BuildCardLibraryCard(Transform parent, MinionInstance card, int index)
        {
            var holder = Panel("UnityCardLibraryCardSlot-" + index + "-" + SafeObjectName(card.CardId), parent, Color.clear);
            var holderLayout = holder.AddComponent<VerticalLayoutGroup>();
            holderLayout.padding = new RectOffset(6, 6, 0, 0);
            holderLayout.spacing = 6;
            holderLayout.childAlignment = TextAnchor.UpperCenter;
            holderLayout.childControlWidth = false;
            holderLayout.childControlHeight = false;
            holderLayout.childForceExpandWidth = false;
            holderLayout.childForceExpandHeight = false;

            var cardObject = UnityTavernCardComponent.CreateCardHost(UnityTavernCardMode.Shop, holder.transform, "UnityCardLibraryCard-" + index + "-" + SafeObjectName(card.CardId));
            cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                card,
                UnityTavernCardMode.Shop,
                string.Empty,
                ApplyCardLibraryChoice,
                ApplyCardLibraryChoice,
                useEnglish: service.UseEnglish);
            ConfigureCardHoverTooltip(cardObject, card);

            var actions = Panel("UnityCardLibraryCardActions-" + index + "-" + SafeObjectName(card.CardId), holder.transform, Color.clear);
            UnityTavernUiStyle.SetFixedSize(actions, 140f, UnityTavernUiStyle.TouchHeight);
            var actionsLayout = actions.AddComponent<HorizontalLayoutGroup>();
            actionsLayout.spacing = 4;
            actionsLayout.childControlWidth = false;
            actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = false;
            actionsLayout.childForceExpandHeight = true;

            var detailButtonName = index == 0 ? "UnityCardLibraryDetailButton" : "UnityCardLibraryDetailButton-" + SafeObjectName(card.CardId);
            var detail = ToolButton(detailButtonName, actions.transform, "详情", true, () => OpenCardLibraryDetail(card));
            UnityTavernUiStyle.SetFixedSize(detail.gameObject, 62f, UnityTavernUiStyle.TouchHeight);
            detail.GetComponentInChildren<Text>(true).fontSize = 14;

            var addButtonName = index == 0 ? "UnityCardLibraryAddButton" : "UnityCardLibraryAddButton-" + SafeObjectName(card.CardId);
            var add = ToolButton(addButtonName, actions.transform, CardLibraryActionText(card), CanApplyCardLibraryChoice(card), () => ApplyCardLibraryChoice(card));
            cardLibraryAddButtons[add] = card;
            UnityTavernUiStyle.SetFixedSize(add.gameObject, 74f, UnityTavernUiStyle.TouchHeight);
            add.GetComponentInChildren<Text>(true).fontSize = 14;
        }

        private void OpenCardLibraryDetail(MinionInstance card)
        {
            if (card == null)
            {
                return;
            }

            cardLibraryDetailCard = card;
            HideKeywordTooltip(card);
            Rebuild();
        }

        private void CloseCardLibraryDetail()
        {
            cardLibraryDetailCard = null;
            Rebuild();
        }

        private void BuildCardLibraryDetailModal()
        {
            var modal = UnityTavernCardDetailModalComponent.CreateModalHost(transform, "UnityCardLibraryDetailOverlay");
            modal.transform.SetAsLastSibling();
            modal.GetComponent<UnityTavernCardDetailModalComponent>().Build(cardLibraryDetailCard, CloseCardLibraryDetail, showCardId: false, useEnglish: service.UseEnglish);
        }

        private void ApplyCardLibraryChoice(MinionInstance card)
        {
            if (card == null || !CanApplyCardLibraryChoice(card))
            {
                return;
            }

            PreserveCardLibraryScrollForNextRebuild();

            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentStartOfCombatSpell)
            {
                ApplyCardLibraryCommand(new GameCommand(GameCommandType.SetOpponentStartOfCombatSpell, card.CardId, CardKind.TavernSpell));
                return;
            }

            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentBoard)
            {
                if (card.CardKind == CardKind.Minion)
                {
                    ApplyCardLibraryCommand(new GameCommand(GameCommandType.AddOpponentMinion, card.CardId, opponentCardLibraryGolden));
                    return;
                }

                ApplyCardLibraryCommand(new GameCommand(GameCommandType.DebugCastCard, card.CardId, card.CardKind, -1));
                return;
            }

            AddLibraryCardToHand(card, cardLibraryDestination == UnityCardLibraryDestination.OpponentHand ? BoardSide.Opponent : BoardSide.Player);
        }

        private void PreserveCardLibraryScrollForNextRebuild()
        {
            if (!cardLibraryOpen || cardLibraryScrollRect == null)
            {
                return;
            }

            cardLibraryScrollPosition = cardLibraryScrollRect.verticalNormalizedPosition;
            restoreCardLibraryScrollPosition = true;
        }

        private void RestoreCardLibraryScrollPosition()
        {
            if (cardLibraryScrollRect == null)
            {
                restoreCardLibraryScrollPosition = false;
                return;
            }

            Canvas.ForceUpdateCanvases();
            cardLibraryScrollRect.verticalNormalizedPosition = Mathf.Clamp01(cardLibraryScrollPosition);
            restoreCardLibraryScrollPosition = false;
        }

        private void ApplyCardLibraryCommand(GameCommand command)
        {
            if (!cardLibraryOpen)
            {
                Apply(command);
                return;
            }

            if (!TryApply(command))
            {
                Rebuild();
                return;
            }

            restoreCardLibraryScrollPosition = false;
            RefreshCardLibraryDestinationZone();
            RefreshCardLibraryAddButtons();
        }

        private void RefreshCardLibraryDestinationZone()
        {
            var zoneName = cardLibraryDestination == UnityCardLibraryDestination.PlayerHand
                ? "UnityHandZone"
                : cardLibraryDestination == UnityCardLibraryDestination.OpponentHand
                    ? "UnityOpponentHandZone"
                    : cardLibraryDestination == UnityCardLibraryDestination.OpponentBoard
                        ? "UnityOpponentBoardZone"
                        : null;
            if (string.IsNullOrEmpty(zoneName))
            {
                return;
            }

            var zone = GetComponentsInChildren<UnityTavernZoneComponent>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.gameObject.name == zoneName);
            if (zone == null)
            {
                return;
            }

            var layout = LayoutContext();
            if (cardLibraryDestination == UnityCardLibraryDestination.PlayerHand)
            {
                BindPlayerHandZone(zone, layout);
            }
            else if (cardLibraryDestination == UnityCardLibraryDestination.OpponentHand)
            {
                BindOpponentHandZone(zone, layout);
            }
            else
            {
                BindOpponentBoardZone(zone, layout);
            }
        }

        private void RefreshCardLibraryAddButtons()
        {
            foreach (var pair in cardLibraryAddButtons)
            {
                var button = pair.Key;
                if (button == null)
                {
                    continue;
                }

                button.interactable = CanApplyCardLibraryChoice(pair.Value);
                var label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = CardLibraryActionText(pair.Value);
                }
            }
        }

        private bool CanApplyCardLibraryChoice(MinionInstance card)
        {
            if (card == null)
            {
                return false;
            }

            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentStartOfCombatSpell)
            {
                return TavernSpellEngine.IsStartOfCombatSpell(card.CardId) &&
                       !(service.State.Opponent.NextCombatTavernSpellCardIds?.Any(queued => string.Equals(queued, card.CardId, StringComparison.OrdinalIgnoreCase)) ?? false);
            }

            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentBoard)
            {
                return card.CardKind == CardKind.Minion
                    ? service.State.Opponent.Board.Count < BoardLimit
                    : card.CardKind == CardKind.TavernSpell || card.CardKind == CardKind.Spell;
            }

            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentHand)
            {
                return card.CardKind != CardKind.Hero &&
                       card.CardKind != CardKind.HeroPower &&
                       service.State.Opponent.Hand.Count < HandLimit;
            }

            if (card.CardKind == CardKind.Hero || card.CardKind == CardKind.HeroPower)
            {
                return true;
            }

            return service.State.Player.Tavern.Hand.Count < HandLimit;
        }

        private string CardLibraryActionText(MinionInstance card)
        {
            if (card == null)
            {
                return "不可用";
            }

            if (!CanApplyCardLibraryChoice(card))
            {
                if (cardLibraryDestination == UnityCardLibraryDestination.OpponentStartOfCombatSpell)
                {
                    return service.State.Opponent.NextCombatTavernSpellCardIds?.Any(queued => string.Equals(queued, card.CardId, StringComparison.OrdinalIgnoreCase)) == true
                        ? "已配置"
                        : "不可配置";
                }

                if (cardLibraryDestination == UnityCardLibraryDestination.OpponentBoard)
                {
                    return card.CardKind == CardKind.Minion && service.State.Opponent.Board.Count >= BoardLimit
                        ? "战场已满"
                        : "不可放置";
                }

                if (cardLibraryDestination == UnityCardLibraryDestination.OpponentHand)
                {
                    return card.CardKind == CardKind.Hero || card.CardKind == CardKind.HeroPower
                        ? "不可加入"
                        : "手牌已满";
                }

                return "手牌已满";
            }

            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentStartOfCombatSpell)
            {
                return "配置";
            }

            if (card.CardKind == CardKind.Hero)
            {
                return "设为英雄";
            }

            if (card.CardKind == CardKind.HeroPower)
            {
                return "设为技能";
            }

            if (cardLibraryDestination == UnityCardLibraryDestination.PlayerHand)
            {
                return "加入";
            }

            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentHand)
            {
                return "加入敌方";
            }

            return card.CardKind == CardKind.Minion
                ? (opponentCardLibraryGolden ? "加入金色" : "加入敌方")
                : "施放";
        }

        private void AddLibraryCardToHand(MinionInstance card)
        {
            AddLibraryCardToHand(card, BoardSide.Player);
        }

        private void AddLibraryCardToHand(MinionInstance card, BoardSide side)
        {
            if (card == null)
            {
                return;
            }

            ApplyCardLibraryCommand(new GameCommand(GameCommandType.AddCardToHand, side, card.CardId, card.CardKind));
        }

        private void OpenQuestRewardLibrary(int questIndex)
        {
            advancedCardLibraryKind = AdvancedCardLibrarySelectionKind.QuestReward;
            advancedCardLibraryQuestIndex = questIndex;
            advancedCardLibraryOpen = true;
            mechanicLibraryDetailItem = null;
            opponentMechanicLibraryOpen = false;
            cardLibraryOpen = false;
            toolsOpen = false;
            Rebuild();
        }

        private void OpenTrinketLibrary(TrinketSlotKind slotKind)
        {
            advancedCardLibraryKind = slotKind == TrinketSlotKind.Greater
                ? AdvancedCardLibrarySelectionKind.GreaterTrinket
                : AdvancedCardLibrarySelectionKind.LesserTrinket;
            advancedCardLibraryQuestIndex = 0;
            advancedCardLibraryOpen = true;
            mechanicLibraryDetailItem = null;
            opponentMechanicLibraryOpen = false;
            cardLibraryOpen = false;
            toolsOpen = false;
            Rebuild();
        }

        private void DismissAdvancedCardLibrary()
        {
            advancedCardLibraryOpen = false;
            mechanicLibraryDetailItem = null;
            Rebuild();
        }

        private void BuildAdvancedCardLibraryOverlay()
        {
            var choices = AdvancedCardLibraryChoices()
                .Where(item => MechanicLibraryItemMatchesSearch(item, advancedCardLibrarySearchText))
                .ToList();
            var overlay = Panel("UnityAdvancedCardLibraryOverlay", transform, new Color(0f, 0f, 0f, 0.68f));
            overlay.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;

            var panel = Panel("UnityAdvancedCardLibraryPanel", overlay.transform, UnityTavernUiStyle.SurfaceDark);
            ConfigureToolsSurface(panel, AdvancedCardLibraryAccent(), 0.30f);
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityAdvancedCardLibraryStarLantern", AdvancedCardLibraryAccent());
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.08f);
            rect.anchorMax = new Vector2(0.92f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 14);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildAdvancedCardLibraryHeader(panel.transform, choices.Count);
            BuildMechanicLibrarySearch(
                panel.transform,
                "UnityAdvancedCardLibrary",
                advancedCardLibrarySearchText,
                value => advancedCardLibrarySearchText = value,
                AdvancedCardLibraryAccent());

            var content = UiFactory.ScrollView("UnityAdvancedCardLibraryScroll", panel.transform, UnityTavernUiStyle.SurfaceRaised, out _);
            UnityTavernUiStyle.SetFlexible(content.gameObject, 1f, 1f);

            if (choices.Count == 0)
            {
                var empty = UiFactory.Label(
                    "UnityAdvancedCardLibraryEmpty",
                    content,
                    service.UseEnglish ? "No selectable items match the current search." : "没有符合当前搜索条件的可选项。",
                    14,
                    FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 90f);
                return;
            }

            var gridLayout = content.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(12, 12, 12, 18);
            gridLayout.spacing = new Vector2(14f, 16f);
            gridLayout.cellSize = new Vector2(188f, 366f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = MechanicLibraryColumnCount();

            for (var index = 0; index < choices.Count; index += 1)
            {
                BuildAdvancedCardLibraryCard(content, choices[index], index);
            }
        }

        private void BuildAdvancedCardLibraryHeader(Transform parent, int visibleCount)
        {
            var header = Panel("UnityAdvancedCardLibraryHeader", parent, UnityTavernUiStyle.SurfaceRaised);
            ConfigureToolsSurface(header, AdvancedCardLibraryAccent(), 0.22f);
            UnityTavernUiStyle.SetPreferredHeight(header, 64f);
            var layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityAdvancedCardLibraryTitle", header.transform, AdvancedCardLibraryTitle(), 18, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleLeft;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

            var summary = UiFactory.Label(
                "UnityAdvancedCardLibraryCountText",
                header.transform,
                service.UseEnglish ? visibleCount + " selectable" : visibleCount + " 项可选",
                14,
                FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleRight;
            summary.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(summary.gameObject, 160f, 32f);

            if (advancedCardLibraryKind == AdvancedCardLibrarySelectionKind.LesserTrinket ||
                advancedCardLibraryKind == AdvancedCardLibrarySelectionKind.GreaterTrinket)
            {
                var lesser = ToolButton("UnityAdvancedCardLibraryLesserTab", header.transform, service.UseEnglish ? "Lesser" : "小饰品", true, () =>
                {
                    advancedCardLibraryKind = AdvancedCardLibrarySelectionKind.LesserTrinket;
                    Rebuild();
                });
                UnityTavernUiStyle.SetFixedSize(lesser.gameObject, 84f, UnityTavernUiStyle.TouchHeight);
                var greater = ToolButton("UnityAdvancedCardLibraryGreaterTab", header.transform, service.UseEnglish ? "Greater" : "大饰品", true, () =>
                {
                    advancedCardLibraryKind = AdvancedCardLibrarySelectionKind.GreaterTrinket;
                    Rebuild();
                });
                UnityTavernUiStyle.SetFixedSize(greater.gameObject, 90f, UnityTavernUiStyle.TouchHeight);
            }

            var close = ToolButton("UnityAdvancedCardLibraryCloseButton", header.transform, service.UseEnglish ? "Close" : "关闭", true, DismissAdvancedCardLibrary);
            UnityTavernUiStyle.SetFixedSize(close.gameObject, 76f, UnityTavernUiStyle.TouchHeight);
        }

        private void BuildAdvancedCardLibraryCard(Transform parent, AdvancedCardLibraryItem item, int index)
        {
            var card = Panel("UnityAdvancedCardLibraryCard-" + index + "-" + SafeObjectName(item.CardId), parent, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(card, AdvancedCardLibraryAccent(), 0.18f);
            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildMechanicChoiceImage(card.transform, item.ImagePath, item.CardId, item.DisplayName, item.CardKind, 92f, 126f);

            var name = UiFactory.Label("UnityAdvancedCardLibraryCardName", card.transform, item.DisplayName, 14, FontStyle.Bold);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = UnityTavernUiStyle.Text;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 34f);

            var meta = UiFactory.Label("UnityAdvancedCardLibraryCardMeta", card.transform, item.Meta, 14, FontStyle.Bold);
            meta.alignment = TextAnchor.MiddleCenter;
            meta.color = UnityTavernUiStyle.Gold;
            meta.horizontalOverflow = HorizontalWrapMode.Wrap;
            meta.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(meta.gameObject, 30f);

            var text = UiFactory.Label("UnityAdvancedCardLibraryCardText", card.transform, CleanCardText(item.Text), 14, FontStyle.Normal);
            text.color = UnityTavernUiStyle.MutedText;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, 48f);

            var notes = UiFactory.Label("UnityAdvancedCardLibraryCardNotes", card.transform, item.Notes, 14, FontStyle.Normal);
            notes.color = UnityTavernUiStyle.MutedText;
            notes.alignment = TextAnchor.UpperCenter;
            notes.horizontalOverflow = HorizontalWrapMode.Wrap;
            notes.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(notes.gameObject, 34f);

            var actions = Panel("UnityAdvancedCardLibraryActions-" + index, card.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(actions, 56f);
            var actionsLayout = actions.AddComponent<HorizontalLayoutGroup>();
            actionsLayout.spacing = 8f;
            actionsLayout.childControlWidth = true;
            actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.childForceExpandHeight = true;

            var detailButtonName = index == 0 ? "UnityAdvancedCardLibraryDetailButton" : "UnityAdvancedCardLibraryDetailButton-" + SafeObjectName(item.CardId);
            var detail = ActionButton(
                detailButtonName,
                actions.transform,
                service.UseEnglish ? "Detail" : "详情",
                () => OpenMechanicLibraryDetail(item),
                0f,
                44f,
                true,
                UnityTavernActionButtonRole.Utility);
            UnityTavernUiStyle.SetFlexible(detail.gameObject, 1f, 0f);
            detail.GetComponentInChildren<Text>(true).fontSize = 14;

            var buttonName = index == 0 ? "UnityAdvancedCardLibrarySelectButton" : "UnityAdvancedCardLibrarySelectButton-" + SafeObjectName(item.CardId);
            var select = ActionButton(
                buttonName,
                actions.transform,
                service.UseEnglish ? "Select" : "选择",
                () => ApplyAdvancedCardLibraryChoice(item),
                0f,
                44f,
                true,
                UnityTavernActionButtonRole.Primary);
            UnityTavernUiStyle.SetFlexible(select.gameObject, 1f, 0f);
            select.GetComponentInChildren<Text>(true).fontSize = 14;
        }

        private IEnumerable<AdvancedCardLibraryItem> AdvancedCardLibraryChoices()
        {
            switch (advancedCardLibraryKind)
            {
                case AdvancedCardLibrarySelectionKind.QuestReward:
                    return service.GetDebugSelectableQuestRewards()
                        .Select(ToAdvancedCardLibraryItem)
                        .OrderBy(item => item.Meta)
                        .ThenBy(item => item.DisplayName)
                        .Take(96)
                        .ToList();
                case AdvancedCardLibrarySelectionKind.GreaterTrinket:
                    return service.GetDebugSelectableTrinkets(TrinketSlotKind.Greater)
                        .Select(definition => ToAdvancedCardLibraryItem(definition, 1))
                        .OrderBy(item => item.Meta)
                        .ThenBy(item => item.DisplayName)
                        .Take(96)
                        .ToList();
                default:
                    return service.GetDebugSelectableTrinkets(TrinketSlotKind.Lesser)
                        .Select(definition => ToAdvancedCardLibraryItem(definition, 0))
                        .OrderBy(item => item.Meta)
                        .ThenBy(item => item.DisplayName)
                        .Take(96)
                        .ToList();
            }
        }

        private AdvancedCardLibraryItem ToAdvancedCardLibraryItem(QuestRewardDefinition reward)
        {
            return new AdvancedCardLibraryItem
            {
                CardKind = CardKind.QuestReward,
                CardId = reward.CardId,
                DisplayName = reward.Name,
                Text = reward.Text,
                ImagePath = reward.ImagePath,
                Meta = BattlegroundsLocalizedText.Power(reward.PowerLevel, service.UseEnglish) + " / " +
                    BattlegroundsLocalizedText.QuestTrigger(reward.Trigger, service.UseEnglish) + " / " +
                    BattlegroundsLocalizedText.OfferPool(reward.OfferPoolStatus, service.UseEnglish),
                Notes = BattlegroundsLocalizedText.QuestEffect(reward.EffectKind, service.UseEnglish) +
                    (service.UseEnglish && !string.IsNullOrWhiteSpace(reward.Notes) ? " / " + reward.Notes : string.Empty),
                TargetIndex = -1
            };
        }

        private AdvancedCardLibraryItem ToAdvancedCardLibraryItem(TrinketDefinition trinket, int targetIndex)
        {
            var races = trinket.AssociatedRaces == null || trinket.AssociatedRaces.Count == 0
                ? T("中立", "neutral")
                : string.Join("/", trinket.AssociatedRaces.Take(3).Select(race => BattlegroundsLocalizedText.Tribe(race, service.UseEnglish)).ToArray());
            var requires = trinket.Requires == null || trinket.Requires.Count == 0
                ? string.Empty
                : T(" / 条件：", " / req ") + string.Join("/", trinket.Requires.Take(2).Select(requirement => BattlegroundsLocalizedText.MechanicTag(requirement, service.UseEnglish)).ToArray());
            return new AdvancedCardLibraryItem
            {
                CardKind = CardKind.Trinket,
                CardId = trinket.CardId,
                DisplayName = trinket.Name,
                Text = trinket.Text,
                ImagePath = trinket.ImagePath,
                Meta = BattlegroundsLocalizedText.TrinketSlot(trinket.SlotKind, service.UseEnglish) + " / " +
                    trinket.Cost + T("铸币", "g") + " / " + BattlegroundsLocalizedText.OfferPool(trinket.OfferPoolStatus, service.UseEnglish),
                Notes = BattlegroundsLocalizedText.ProxyLevel(trinket.ProxyLevel, service.UseEnglish) + " / " +
                    BattlegroundsLocalizedText.MechanicTag(trinket.EffectFamily, service.UseEnglish) + " / " + races + requires,
                TargetIndex = targetIndex
            };
        }

        private AdvancedCardLibraryItem ToAdvancedCardLibraryItem(HeroPowerDefinition power)
        {
            var status = BattlegroundsLocalizedText.HeroImplementation(
                HeroEffectImplementationRegistry.GetStatusByHeroPowerCardId(power?.CardId),
                service.UseEnglish);
            var tags = power?.Tags == null || power.Tags.Count == 0
                ? string.Empty
                : " / " + string.Join("/", power.Tags.Take(2).Select(tag => BattlegroundsLocalizedText.FilterTag(tag, service.UseEnglish)).ToArray());
            return new AdvancedCardLibraryItem
            {
                CardKind = CardKind.HeroPower,
                CardId = power?.CardId,
                DisplayName = DisplayHeroPowerName(power),
                Text = DisplayHeroPowerText(power),
                ImagePath = power?.ImagePath,
                Meta = T("英雄技能 / ", "Hero Power / ") + (power?.Cost ?? 0) + T("铸币 / ", "g / ") +
                    BattlegroundsLocalizedText.HeroPowerCategory(power?.PrimaryCategory ?? HeroPowerCategory.Other, service.UseEnglish),
                Notes = status + " / " + BattlegroundsLocalizedText.HeroPowerEligibility(
                    power?.ReplacementEligibility ?? HeroPowerReplacementEligibility.Disabled,
                    service.UseEnglish) + tags,
                TargetIndex = -1
            };
        }

        private string DisplayHeroPowerName(HeroPowerDefinition power)
        {
            if (!service.UseEnglish && !string.IsNullOrEmpty(power?.ZhName))
            {
                return power.ZhName;
            }

            return power?.Name ?? string.Empty;
        }

        private string DisplayHeroPowerText(HeroPowerDefinition power)
        {
            if (!service.UseEnglish && !string.IsNullOrEmpty(power?.ZhText))
            {
                return power.ZhText;
            }

            return power?.Text ?? string.Empty;
        }

        private void ApplyAdvancedCardLibraryChoice(AdvancedCardLibraryItem item)
        {
            if (item == null)
            {
                return;
            }

            advancedCardLibraryOpen = false;
            mechanicLibraryDetailItem = null;
            if (item.CardKind == CardKind.QuestReward)
            {
                var quest = ActiveQuestByIndex(advancedCardLibraryQuestIndex);
                Apply(new GameCommand(GameCommandType.DebugReplaceQuestReward, item.CardId, CardKind.QuestReward, quest != null && quest.Completed, advancedCardLibraryQuestIndex));
                return;
            }

            Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, item.CardId, CardKind.Trinket, item.TargetIndex));
        }

        private Color AdvancedCardLibraryAccent()
        {
            return advancedCardLibraryKind == AdvancedCardLibrarySelectionKind.QuestReward
                ? UnityTavernUiStyle.Blue
                : UnityTavernUiStyle.Gold;
        }

        private string AdvancedCardLibraryTitle()
        {
            switch (advancedCardLibraryKind)
            {
                case AdvancedCardLibrarySelectionKind.QuestReward:
                    return service.UseEnglish ? "Replace Quest Reward" : "替换任务奖励";
                case AdvancedCardLibrarySelectionKind.GreaterTrinket:
                    return service.UseEnglish ? "Replace Greater Trinket" : "替换大饰品";
                default:
                    return service.UseEnglish ? "Replace Lesser Trinket" : "替换小饰品";
            }
        }

        private void BuildOpponentMechanicLibraryOverlay()
        {
            var choices = OpponentMechanicLibraryChoices()
                .Where(item => MechanicLibraryItemMatchesSearch(item, opponentMechanicLibrarySearchText))
                .ToList();
            var overlay = Panel("UnityOpponentMechanicLibraryOverlay", transform, new Color(0f, 0f, 0f, 0.68f));
            overlay.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;

            var panel = Panel("UnityOpponentMechanicLibraryPanel", overlay.transform, UnityTavernUiStyle.SurfaceDark);
            ConfigureToolsSurface(panel, OpponentMechanicLibraryAccent(), 0.30f);
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityOpponentMechanicLibraryStarLantern", OpponentMechanicLibraryAccent());
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.08f);
            rect.anchorMax = new Vector2(0.92f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 14);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildOpponentMechanicLibraryHeader(panel.transform, choices.Count);
            BuildMechanicLibrarySearch(
                panel.transform,
                "UnityOpponentMechanicLibrary",
                opponentMechanicLibrarySearchText,
                value => opponentMechanicLibrarySearchText = value,
                OpponentMechanicLibraryAccent());

            var content = UiFactory.ScrollView("UnityOpponentMechanicLibraryScroll", panel.transform, UnityTavernUiStyle.SurfaceRaised, out _);
            UnityTavernUiStyle.SetFlexible(content.gameObject, 1f, 1f);

            if (choices.Count == 0)
            {
                var empty = UiFactory.Label("UnityOpponentMechanicLibraryEmpty", content, "当前设置下没有可选机制。", 14, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 90f);
                return;
            }

            var gridLayout = content.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(12, 12, 12, 18);
            gridLayout.spacing = new Vector2(14f, 16f);
            gridLayout.cellSize = new Vector2(188f, 366f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = MechanicLibraryColumnCount();

            for (var index = 0; index < choices.Count; index += 1)
            {
                BuildOpponentMechanicLibraryCard(content, choices[index], index);
            }
        }

        private void BuildOpponentMechanicLibraryHeader(Transform parent, int visibleCount)
        {
            var header = Panel("UnityOpponentMechanicLibraryHeader", parent, UnityTavernUiStyle.SurfaceRaised);
            ConfigureToolsSurface(header, OpponentMechanicLibraryAccent(), 0.22f);
            UnityTavernUiStyle.SetPreferredHeight(header, 64f);
            var layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityOpponentMechanicLibraryTitle", header.transform, OpponentMechanicLibraryTitle(), 18, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleLeft;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

            var summary = UiFactory.Label("UnityOpponentMechanicLibraryCountText", header.transform, service.UseEnglish ? visibleCount + " selectable" : visibleCount + " 项可选", 14, FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleRight;
            summary.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(summary.gameObject, 120f, 32f);

            if (opponentMechanicLibraryKind == OpponentMechanicLibraryKind.LesserTrinket ||
                opponentMechanicLibraryKind == OpponentMechanicLibraryKind.GreaterTrinket)
            {
                var lesser = ToolButton("UnityOpponentMechanicLibraryLesserTab", header.transform, "小饰品", true, () =>
                {
                    opponentMechanicLibraryKind = OpponentMechanicLibraryKind.LesserTrinket;
                    Rebuild();
                });
                UnityTavernUiStyle.SetFixedSize(lesser.gameObject, 84f, UnityTavernUiStyle.TouchHeight);
                var greater = ToolButton("UnityOpponentMechanicLibraryGreaterTab", header.transform, "大饰品", true, () =>
                {
                    opponentMechanicLibraryKind = OpponentMechanicLibraryKind.GreaterTrinket;
                    Rebuild();
                });
                UnityTavernUiStyle.SetFixedSize(greater.gameObject, 90f, UnityTavernUiStyle.TouchHeight);
            }

            var close = ToolButton("UnityOpponentMechanicLibraryCloseButton", header.transform, "关闭", true, DismissOpponentMechanicLibrary);
            UnityTavernUiStyle.SetFixedSize(close.gameObject, 76f, UnityTavernUiStyle.TouchHeight);
        }

        private void BuildOpponentMechanicLibraryCard(Transform parent, AdvancedCardLibraryItem item, int index)
        {
            var card = Panel("UnityOpponentMechanicLibraryCard-" + index + "-" + SafeObjectName(item.CardId), parent, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(card, OpponentMechanicLibraryAccent(), 0.18f);
            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildMechanicChoiceImage(card.transform, item.ImagePath, item.CardId, item.DisplayName, item.CardKind, 92f, 126f);

            var name = UiFactory.Label("UnityOpponentMechanicLibraryCardName", card.transform, item.DisplayName, 14, FontStyle.Bold);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = UnityTavernUiStyle.Text;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 34f);

            var meta = UiFactory.Label("UnityOpponentMechanicLibraryCardMeta", card.transform, item.Meta, 14, FontStyle.Bold);
            meta.alignment = TextAnchor.MiddleCenter;
            meta.color = UnityTavernUiStyle.Gold;
            meta.horizontalOverflow = HorizontalWrapMode.Wrap;
            meta.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(meta.gameObject, 30f);

            var text = UiFactory.Label("UnityOpponentMechanicLibraryCardText", card.transform, CleanCardText(item.Text), 14, FontStyle.Normal);
            text.color = UnityTavernUiStyle.MutedText;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, 48f);

            var notes = UiFactory.Label("UnityOpponentMechanicLibraryCardNotes", card.transform, item.Notes, 14, FontStyle.Normal);
            notes.color = UnityTavernUiStyle.MutedText;
            notes.alignment = TextAnchor.UpperCenter;
            notes.horizontalOverflow = HorizontalWrapMode.Wrap;
            notes.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(notes.gameObject, 34f);

            var actions = Panel("UnityOpponentMechanicLibraryActions-" + index, card.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(actions, 56f);
            var actionsLayout = actions.AddComponent<HorizontalLayoutGroup>();
            actionsLayout.spacing = 8f;
            actionsLayout.childControlWidth = true;
            actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.childForceExpandHeight = true;

            var detailButtonName = index == 0 ? "UnityOpponentMechanicLibraryDetailButton" : "UnityOpponentMechanicLibraryDetailButton-" + SafeObjectName(item.CardId);
            var detail = ActionButton(
                detailButtonName,
                actions.transform,
                service.UseEnglish ? "Detail" : "详情",
                () => OpenMechanicLibraryDetail(item),
                0f,
                44f,
                true,
                UnityTavernActionButtonRole.Utility);
            UnityTavernUiStyle.SetFlexible(detail.gameObject, 1f, 0f);
            detail.GetComponentInChildren<Text>(true).fontSize = 14;

            var buttonName = index == 0 ? "UnityOpponentMechanicLibrarySelectButton" : "UnityOpponentMechanicLibrarySelectButton-" + SafeObjectName(item.CardId);
            var select = ActionButton(
                buttonName,
                actions.transform,
                service.UseEnglish ? "Select" : "设为对手",
                () => ApplyOpponentMechanicLibraryChoice(item),
                0f,
                44f,
                true,
                UnityTavernActionButtonRole.Primary);
            UnityTavernUiStyle.SetFlexible(select.gameObject, 1f, 0f);
            select.GetComponentInChildren<Text>(true).fontSize = 14;
        }

        private void OpenMechanicLibraryDetail(AdvancedCardLibraryItem item)
        {
            mechanicLibraryDetailItem = item;
            Rebuild();
        }

        private void CloseMechanicLibraryDetail()
        {
            mechanicLibraryDetailItem = null;
            Rebuild();
        }

        private void BuildMechanicLibraryDetailModal()
        {
            var item = mechanicLibraryDetailItem;
            if (item == null)
            {
                return;
            }

            var overlay = Panel("UnityMechanicLibraryDetailOverlay", transform, new Color(0f, 0f, 0f, 0.72f));
            overlay.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;

            var panel = Panel("UnityMechanicLibraryDetailPanel", overlay.transform, UnityTavernUiStyle.SurfaceDark);
            ConfigureToolsSurface(panel, item.CardKind == CardKind.Trinket ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Blue, 0.30f);
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityMechanicLibraryDetailStarLantern", item.CardKind == CardKind.Trinket ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.ArcaneBlue);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.20f, 0.12f);
            rect.anchorMax = new Vector2(0.80f, 0.88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 16);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityMechanicLibraryDetailTitle", panel.transform, item.DisplayName, 22, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Text;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 48f);

            var meta = UiFactory.Label("UnityMechanicLibraryDetailMeta", panel.transform, item.Meta, 14, FontStyle.Bold);
            meta.alignment = TextAnchor.MiddleCenter;
            meta.color = UnityTavernUiStyle.Gold;
            meta.horizontalOverflow = HorizontalWrapMode.Wrap;
            meta.verticalOverflow = VerticalWrapMode.Overflow;
            UnityTavernUiStyle.SetPreferredHeight(meta.gameObject, 38f);

            var scroll = UiFactory.ScrollView("UnityMechanicLibraryDetailScroll", panel.transform, UnityTavernUiStyle.SurfaceRaised, out _);
            UnityTavernUiStyle.SetFlexible(scroll.gameObject, 1f, 1f);
            var bodyLayout = scroll.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(14, 14, 12, 12);
            bodyLayout.spacing = 12f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            BuildMechanicLibraryDetailLabel("UnityMechanicLibraryDetailText", scroll, CleanCardText(item.Text), UnityTavernUiStyle.Text);
            BuildMechanicLibraryDetailLabel("UnityMechanicLibraryDetailNotes", scroll, item.Notes, UnityTavernUiStyle.MutedText);
            BuildMechanicLibraryDetailLabel("UnityMechanicLibraryDetailCardId", scroll, item.CardId, UnityTavernUiStyle.MutedText);

            var close = ActionButton(
                "UnityMechanicLibraryDetailCloseButton",
                panel.transform,
                service.UseEnglish ? "Close" : "关闭",
                CloseMechanicLibraryDetail,
                0f,
                UnityTavernUiStyle.TouchHeight,
                true,
                UnityTavernActionButtonRole.Utility);
            UnityTavernUiStyle.SetPreferredHeight(close.gameObject, UnityTavernUiStyle.TouchHeight);
            close.GetComponentInChildren<Text>(true).fontSize = 14;
        }

        private static void BuildMechanicLibraryDetailLabel(string objectName, Transform parent, string value, Color color)
        {
            var label = UiFactory.Label(objectName, parent, string.IsNullOrWhiteSpace(value) ? "-" : value, 14, FontStyle.Normal);
            label.alignment = TextAnchor.UpperLeft;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void BuildMechanicLibrarySearch(
            Transform parent,
            string namePrefix,
            string value,
            Action<string> changed,
            Color accent)
        {
            var row = Panel(namePrefix + "SearchRow", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(row, 56f);
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(row).flexibleHeight = 0f;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var inputObject = new GameObject(namePrefix + "SearchInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(row.transform, false);
            var inputElement = UnityTavernUiStyle.EnsureComponent<LayoutElement>(inputObject);
            inputElement.flexibleWidth = 1f;
            inputElement.minHeight = UnityTavernUiStyle.TouchHeight;
            inputElement.preferredHeight = UnityTavernUiStyle.TouchHeight;

            var input = inputObject.GetComponent<InputField>();
            UnityTavernUiStyle.ConfigureInputField(input, accent);
            input.caretColor = UnityTavernUiStyle.Text;
            input.selectionColor = new Color(accent.r, accent.g, accent.b, 0.35f);
            input.textComponent = UiFactory.Label(namePrefix + "SearchText", inputObject.transform, string.Empty, 14);
            UnityTavernUiStyle.Stretch(input.textComponent.rectTransform);
            input.textComponent.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.textComponent.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.placeholder = UiFactory.Label(
                namePrefix + "SearchPlaceholder",
                inputObject.transform,
                service.UseEnglish ? "Search name, text, tag, or CardId" : "搜索名称、文本、标签或卡牌ID",
                14);
            UnityTavernUiStyle.Stretch(input.placeholder.rectTransform);
            input.placeholder.color = UnityTavernUiStyle.MutedText;
            input.placeholder.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.placeholder.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.text = value ?? string.Empty;
            input.onEndEdit.AddListener(nextValue =>
            {
                changed?.Invoke(nextValue ?? string.Empty);
                Rebuild();
            });

            var clear = ToolButton(
                namePrefix + "ClearSearchButton",
                row.transform,
                service.UseEnglish ? "Clear" : "清空",
                !string.IsNullOrWhiteSpace(value),
                () =>
                {
                    changed?.Invoke(string.Empty);
                    Rebuild();
                });
            UnityTavernUiStyle.SetFixedSize(clear.gameObject, 80f, UnityTavernUiStyle.TouchHeight);
            clear.GetComponentInChildren<Text>(true).fontSize = 14;
        }

        private int MechanicLibraryColumnCount()
        {
            var layout = LayoutContext();
            return layout.IsCompact ? 3 : layout.Width >= 1700f ? 6 : layout.Width >= 1300f ? 5 : 4;
        }

        private static bool MechanicLibraryItemMatchesSearch(AdvancedCardLibraryItem item, string searchText)
        {
            if (item == null || string.IsNullOrWhiteSpace(searchText))
            {
                return item != null;
            }

            var query = searchText.Trim();
            return ContainsIgnoreCase(item.DisplayName, query) ||
                   ContainsIgnoreCase(item.Text, query) ||
                   ContainsIgnoreCase(item.Meta, query) ||
                   ContainsIgnoreCase(item.Notes, query) ||
                   ContainsIgnoreCase(item.CardId, query);
        }

        private IEnumerable<AdvancedCardLibraryItem> OpponentMechanicLibraryChoices()
        {
            switch (opponentMechanicLibraryKind)
            {
                case OpponentMechanicLibraryKind.HeroPower:
                    return service.GetOpponentSelectableHeroPowers()
                        .Select(ToAdvancedCardLibraryItem)
                        .OrderBy(item => item.Meta)
                        .ThenBy(item => item.DisplayName)
                        .Take(96)
                        .ToList();
                case OpponentMechanicLibraryKind.QuestReward:
                    return service.GetOpponentSelectableQuestRewards()
                        .Select(ToAdvancedCardLibraryItem)
                        .OrderBy(item => item.Meta)
                        .ThenBy(item => item.DisplayName)
                        .Take(96)
                        .ToList();
                case OpponentMechanicLibraryKind.GreaterTrinket:
                    return service.GetOpponentSelectableTrinkets(TrinketSlotKind.Greater)
                        .Select(definition => ToAdvancedCardLibraryItem(definition, 1))
                        .OrderBy(item => item.Meta)
                        .ThenBy(item => item.DisplayName)
                        .Take(96)
                        .ToList();
                default:
                    return service.GetOpponentSelectableTrinkets(TrinketSlotKind.Lesser)
                        .Select(definition => ToAdvancedCardLibraryItem(definition, 0))
                        .OrderBy(item => item.Meta)
                        .ThenBy(item => item.DisplayName)
                        .Take(96)
                        .ToList();
            }
        }

        private void ApplyOpponentMechanicLibraryChoice(AdvancedCardLibraryItem item)
        {
            if (item == null)
            {
                return;
            }

            opponentMechanicLibraryOpen = false;
            mechanicLibraryDetailItem = null;
            opponentPanelOpen = true;
            if (item.CardKind == CardKind.HeroPower)
            {
                Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, item.CardId, CardKind.HeroPower));
                return;
            }

            if (item.CardKind == CardKind.QuestReward)
            {
                Apply(new GameCommand(GameCommandType.SetOpponentQuestReward, item.CardId, CardKind.QuestReward));
                return;
            }

            Apply(new GameCommand(GameCommandType.SetOpponentTrinket, item.CardId, CardKind.Trinket, item.TargetIndex));
        }

        private Color OpponentMechanicLibraryAccent()
        {
            switch (opponentMechanicLibraryKind)
            {
                case OpponentMechanicLibraryKind.HeroPower:
                    return UnityTavernUiStyle.Green;
                case OpponentMechanicLibraryKind.QuestReward:
                    return UnityTavernUiStyle.Blue;
                default:
                    return UnityTavernUiStyle.Gold;
            }
        }

        private string OpponentMechanicLibraryTitle()
        {
            switch (opponentMechanicLibraryKind)
            {
                case OpponentMechanicLibraryKind.HeroPower:
                    return "选择对手英雄技能";
                case OpponentMechanicLibraryKind.QuestReward:
                    return "选择对手任务奖励";
                case OpponentMechanicLibraryKind.GreaterTrinket:
                    return "选择对手大饰品";
                default:
                    return "选择对手小饰品";
            }
        }

        private void BuildToolsCardLibrarySection(Transform parent)
        {
            var allChoices = FilteredToolsAcquisitionChoices().ToList();
            var choices = allChoices.Take(cardLibraryVisibleLimit).ToList();
            var tribeHeight = 120f;
            var listHeight = Mathf.Max(56f, choices.Count * 50f + 14f);

            var section = Panel("UnityToolsCardLibrarySection", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureToolsSurface(section, UnityTavernUiStyle.Blue, 0.28f);
            UnityTavernUiStyle.SetPreferredHeight(section, 190f + tribeHeight + listHeight);
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 12);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildToolsSectionHeader(section.transform, "UnityToolsCardLibraryHeader", "UnityToolsCardLibraryTitle", "卡牌库", UnityTavernUiStyle.Blue);

            BuildToolsAcquisitionModeRow(section.transform);
            BuildToolsAcquisitionTierRow(section.transform);
            BuildToolsAcquisitionTribeRow(section.transform);

            var summary = Panel("UnityToolsCardLibrarySummary", section.transform, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(summary, 30f);
            var summaryLayout = summary.AddComponent<HorizontalLayoutGroup>();
            summaryLayout.padding = new RectOffset(8, 8, 4, 4);
            summaryLayout.childControlWidth = true;
            summaryLayout.childControlHeight = true;
            summaryLayout.childForceExpandWidth = true;
            summaryLayout.childForceExpandHeight = true;

            var count = UiFactory.Label("UnityToolsCardLibraryCountText", summary.transform, ToolsAcquisitionSubtitle(choices.Count), 12, FontStyle.Bold);
            count.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFlexible(count.gameObject, 1f, 0f);

            var list = Panel("UnityToolsCardLibraryList", section.transform, UnityTavernUiStyle.Panel);
            ConfigureToolsSurface(list, UnityTavernUiStyle.Blue, 0.18f);
            UnityTavernUiStyle.SetPreferredHeight(list, listHeight);
            var listLayout = list.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(8, 8, 8, 8);
            listLayout.spacing = 6;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            if (choices.Count == 0)
            {
                var empty = UiFactory.Label("UnityToolsCardLibraryEmpty", list.transform, "没有符合筛选条件的卡牌。", 12, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 36f);
                return;
            }

            for (var index = 0; index < choices.Count; index += 1)
            {
                BuildToolsAcquisitionChoiceRow(list.transform, choices[index], index);
            }

            if (choices.Count < allChoices.Count)
            {
                var loadMore = ToolButton(
                    "UnityToolsCardLibraryLoadMoreButton",
                    list.transform,
                    "加载更多（" + choices.Count + "/" + allChoices.Count + "）",
                    true,
                    LoadMoreCardLibraryChoices);
                UnityTavernUiStyle.SetPreferredHeight(loadMore.gameObject, UnityTavernUiStyle.TouchHeight);
            }
        }

        private void BuildToolsAcquisitionModeRow(Transform parent)
        {
            var row = Panel("UnityToolsCardLibraryModeRow", parent, UnityTavernUiStyle.Panel);
            ConfigureToolsSurface(row, UnityTavernUiStyle.Blue, 0.14f);
            UnityTavernUiStyle.SetPreferredHeight(row, 56f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            LibraryFilterButton("UnityToolsCardLibraryMinionModeButton", row.transform, "随从", toolsAcquisitionKind == CardKind.Minion, 92f, () =>
            {
                SelectToolsAcquisitionKind(CardKind.Minion);
                Rebuild();
            });
            LibraryFilterButton("UnityToolsCardLibrarySpellModeButton", row.transform, "酒馆法术", toolsAcquisitionKind == CardKind.TavernSpell, 112f, () =>
            {
                SelectToolsAcquisitionKind(CardKind.TavernSpell);
                Rebuild();
            });
            LibraryFilterButton("UnityToolsCardLibraryHeroModeButton", row.transform, "英雄", toolsAcquisitionKind == CardKind.Hero, 82f, () =>
            {
                SelectToolsAcquisitionKind(CardKind.Hero);
                Rebuild();
            });
            LibraryFilterButton("UnityToolsCardLibraryHeroPowerModeButton", row.transform, "英雄技能", toolsAcquisitionKind == CardKind.HeroPower, 112f, () =>
            {
                SelectToolsAcquisitionKind(CardKind.HeroPower);
                Rebuild();
            });
            LibraryFilterButton("UnityToolsCardLibraryHeroBuddyModeButton", row.transform, "英雄宝宝", toolsAcquisitionKind == CardKind.HeroBuddy, 112f, () =>
            {
                SelectToolsAcquisitionKind(CardKind.HeroBuddy);
                Rebuild();
            });
            LibraryFilterButton("UnityToolsCardLibraryShowAllToggle", row.transform, toolsShowAllCards ? "显示全部" : "当前局", toolsShowAllCards, 92f, () =>
            {
                toolsShowAllCards = !toolsShowAllCards;
                NormalizeToolsAcquisitionTribeFilter();
                Rebuild();
            });
        }

        private void BuildToolsAcquisitionTierRow(Transform parent)
        {
            if (toolsAcquisitionKind == CardKind.Hero)
            {
                BuildToolsHeroInfoRow(parent, "当前英雄", CurrentHeroName(), UnityTavernUiStyle.Red);
                return;
            }

            if (toolsAcquisitionKind == CardKind.HeroPower)
            {
                BuildToolsHeroPowerCategoryRow(parent);
                return;
            }

            var row = Panel("UnityToolsCardLibraryTierRow", parent, UnityTavernUiStyle.Panel);
            ConfigureToolsSurface(row, UnityTavernUiStyle.Gold, 0.14f);
            UnityTavernUiStyle.SetPreferredHeight(row, 56f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 6;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            LibraryFilterButton("UnityToolsCardLibraryTierAllButton", row.transform, "全部", toolsAcquisitionTierFilter == 0, 58f, () =>
            {
                toolsAcquisitionTierFilter = 0;
                Rebuild();
            });

            for (var tier = 1; tier <= 7; tier += 1)
            {
                var capturedTier = tier;
                LibraryFilterButton("UnityToolsCardLibraryTier" + tier + "Button", row.transform, tier + "本", toolsAcquisitionTierFilter == tier, 52f, () =>
                {
                    toolsAcquisitionTierFilter = capturedTier;
                    Rebuild();
                });
            }
        }

        private void BuildToolsHeroInfoRow(Transform parent, string title, string value, Color accent)
        {
            var row = Panel("UnityToolsCardLibraryHeroInfoRow", parent, UnityTavernUiStyle.Panel);
            ConfigureToolsSurface(row, accent, 0.14f);
            UnityTavernUiStyle.SetPreferredHeight(row, 56f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var label = UiFactory.Label("UnityToolsCardLibraryHeroInfoLabel", row.transform, title, 14, FontStyle.Bold);
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFixedSize(label.gameObject, 86f, 32f);

            var text = UiFactory.Label("UnityToolsCardLibraryHeroInfoValue", row.transform, value, 14, FontStyle.Bold);
            text.color = UnityTavernUiStyle.MutedText;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFlexible(text.gameObject, 1f, 0f);
        }

        private void BuildToolsHeroPowerCategoryRow(Transform parent)
        {
            var grid = Panel("UnityToolsCardLibraryHeroPowerCategoryGrid", parent, UnityTavernUiStyle.Panel);
            ConfigureToolsSurface(grid, UnityTavernUiStyle.Blue, 0.14f);
            UnityTavernUiStyle.SetPreferredHeight(grid, 114f);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(8, 8, 6, 6);
            gridLayout.spacing = new Vector2(6f, 6f);
            gridLayout.cellSize = new Vector2(132f, UnityTavernUiStyle.TouchHeight);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;

            LibraryFilterButton("UnityToolsCardLibraryHeroPowerCategoryAllButton", grid.transform, "全部分类", !toolsHeroPowerCategoryFilter.HasValue, 136f, () =>
            {
                toolsHeroPowerCategoryFilter = null;
                Rebuild();
            });

            foreach (HeroPowerCategory category in Enum.GetValues(typeof(HeroPowerCategory)))
            {
                var capturedCategory = category;
                LibraryFilterButton("UnityToolsCardLibraryHeroPowerCategory" + category + "Button", grid.transform, HeroPowerCategoryName(category), toolsHeroPowerCategoryFilter == category, 136f, () =>
                {
                    toolsHeroPowerCategoryFilter = capturedCategory;
                    Rebuild();
                });
            }
        }

        private void BuildToolsAcquisitionTribeRow(Transform parent)
        {
            var grid = Panel("UnityToolsCardLibraryTribeGrid", parent, UnityTavernUiStyle.Panel);
            ConfigureToolsSurface(grid, UnityTavernUiStyle.Green, 0.14f);
            if (toolsAcquisitionKind == CardKind.Hero)
            {
                UnityTavernUiStyle.SetPreferredHeight(grid, 56f);
                var rowLayout = grid.AddComponent<HorizontalLayoutGroup>();
                rowLayout.padding = new RectOffset(8, 8, 4, 4);
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = true;
                var info = UiFactory.Label("UnityToolsCardLibraryHeroInfoText", grid.transform, "点击条目可设为当前英雄。", 14, FontStyle.Bold);
                info.alignment = TextAnchor.MiddleCenter;
                info.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetFlexible(info.gameObject, 1f, 0f);
                return;
            }

            if (toolsAcquisitionKind == CardKind.HeroPower)
            {
                UnityTavernUiStyle.SetPreferredHeight(grid, 60f);
                var eligibilityGridLayout = grid.AddComponent<GridLayoutGroup>();
                eligibilityGridLayout.padding = new RectOffset(8, 8, 6, 6);
                eligibilityGridLayout.spacing = new Vector2(6f, 6f);
                eligibilityGridLayout.cellSize = new Vector2(132f, UnityTavernUiStyle.TouchHeight);
                eligibilityGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                eligibilityGridLayout.constraintCount = 4;
                LibraryFilterButton("UnityToolsCardLibraryHeroPowerEligibilityAllButton", grid.transform, "全部资格", !toolsHeroPowerEligibilityFilter.HasValue, 136f, () =>
                {
                    toolsHeroPowerEligibilityFilter = null;
                    Rebuild();
                });
                foreach (HeroPowerReplacementEligibility eligibility in Enum.GetValues(typeof(HeroPowerReplacementEligibility)))
                {
                    var capturedEligibility = eligibility;
                    LibraryFilterButton("UnityToolsCardLibraryHeroPowerEligibility" + eligibility + "Button", grid.transform, HeroPowerEligibilityName(eligibility), toolsHeroPowerEligibilityFilter == eligibility, 136f, () =>
                    {
                        toolsHeroPowerEligibilityFilter = capturedEligibility;
                        Rebuild();
                    });
                }
                return;
            }

            if (toolsAcquisitionKind == CardKind.TavernSpell)
            {
                UnityTavernUiStyle.SetPreferredHeight(grid, 174f);
                var spellGridLayout = grid.AddComponent<GridLayoutGroup>();
                spellGridLayout.padding = new RectOffset(8, 8, 6, 6);
                spellGridLayout.spacing = new Vector2(6f, 6f);
                spellGridLayout.cellSize = new Vector2(132f, UnityTavernUiStyle.TouchHeight);
                spellGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                spellGridLayout.constraintCount = 4;
                LibraryFilterButton("UnityToolsCardLibraryTribeAllButton", grid.transform, "全部", toolsAcquisitionTribeFilter == Tribe.All, 136f, () =>
                {
                    toolsAcquisitionTribeFilter = Tribe.All;
                    Rebuild();
                });
                LibraryFilterButton("UnityToolsCardLibraryTavernSpellTypeButton", grid.transform, "通用法术", toolsAcquisitionTribeFilter == Tribe.None, 136f, () =>
                {
                    toolsAcquisitionTribeFilter = Tribe.None;
                    Rebuild();
                });
                foreach (var tribe in VisibleLibraryTribes().Where(tribe => tribe != Tribe.None))
                {
                    var capturedTribe = tribe;
                    LibraryFilterButton("UnityToolsCardLibrarySpellTribe" + tribe + "Button", grid.transform, TribeName(tribe), toolsAcquisitionTribeFilter == tribe, 136f, () =>
                    {
                        toolsAcquisitionTribeFilter = capturedTribe;
                        Rebuild();
                    });
                }
                return;
            }

            UnityTavernUiStyle.SetPreferredHeight(grid, 174f);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(8, 8, 6, 6);
            gridLayout.spacing = new Vector2(6f, 6f);
            gridLayout.cellSize = new Vector2(132f, UnityTavernUiStyle.TouchHeight);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;

            LibraryFilterButton("UnityToolsCardLibraryTribeAllButton", grid.transform, "全部", toolsAcquisitionTribeFilter == Tribe.All, 136f, () =>
            {
                toolsAcquisitionTribeFilter = Tribe.All;
                Rebuild();
            });

            foreach (var tribe in VisibleLibraryTribes())
            {
                var capturedTribe = tribe;
                LibraryFilterButton("UnityToolsCardLibraryTribe" + tribe + "Button", grid.transform, TribeName(tribe), toolsAcquisitionTribeFilter == tribe, 136f, () =>
                {
                    toolsAcquisitionTribeFilter = capturedTribe;
                    Rebuild();
                });
            }
        }

        private void BuildToolsAcquisitionChoiceRow(Transform parent, MinionInstance card, int index)
        {
            var row = Panel("UnityToolsCardLibraryChoice-" + index + "-" + SafeObjectName(card.CardId), parent, index % 2 == 0 ? UnityTavernUiStyle.PanelQuiet : UnityTavernUiStyle.PanelRaised);
            ConfigureToolsSurface(row, CardKindAccent(card.CardKind), 0.12f);
            UnityTavernUiStyle.SetPreferredHeight(row, 56f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 8, 5, 5);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var accent = new GameObject("UnityToolsCardLibraryChoiceAccent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(row.transform, false);
            UnityTavernUiStyle.SetFixedSize(accent, 4f, 32f);
            UnityTavernUiStyle.ConfigureSurface(accent, CardKindAccent(card.CardKind));

            var name = UiFactory.Label("UnityToolsCardLibraryChoiceName", row.transform, DisplayCardName(card), 14, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFlexible(name.gameObject, 1f, 0f);

            var meta = UiFactory.Label("UnityToolsCardLibraryChoiceMeta", row.transform, ToolsAcquisitionCardMeta(card), 14, FontStyle.Normal);
            meta.alignment = TextAnchor.MiddleRight;
            meta.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(meta.gameObject, 190f, 34f);

            var addButtonName = index == 0 ? "UnityToolsCardLibraryAddButton" : "UnityToolsCardLibraryAddButton-" + SafeObjectName(card.CardId);
            var add = ToolButton(addButtonName, row.transform, CardLibraryActionText(card), CanApplyCardLibraryChoice(card), () =>
            {
                ApplyCardLibraryChoice(card);
            });
            UnityTavernUiStyle.SetFixedSize(add.gameObject, 80f, UnityTavernUiStyle.TouchHeight);
        }

        private static Button CardLibraryBadgeButton(
            string name,
            Transform parent,
            string caption,
            string symbol,
            Sprite icon,
            bool active,
            Vector2 size,
            Color accentColor,
            Action onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(buttonObject, size.x, size.y);

            var image = buttonObject.GetComponent<Image>();
            image.color = active
                ? Color.Lerp(UnityTavernUiStyle.PanelRaised, accentColor, 0.42f)
                : new Color(0.12f, 0.14f, 0.13f, 0.96f);
            image.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                Color.Lerp(UnityTavernUiStyle.PanelRaised, accentColor, 0.52f),
                Color.Lerp(UnityTavernUiStyle.PanelQuiet, accentColor, 0.44f));

            var outline = UnityTavernUiStyle.ConfigureOutline(
                buttonObject,
                active ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.86f) : new Color(0f, 0f, 0f, 0.24f),
                active ? new Vector2(2f, -2f) : new Vector2(1f, -1f));
            outline.enabled = active;

            if (icon != null)
            {
                var iconObject = new GameObject(name + "Icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(buttonObject.transform, false);
                var iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.08f, 0.25f);
                iconRect.anchorMax = new Vector2(0.92f, 0.96f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;

                var iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
                iconImage.raycastTarget = false;
            }
            else
            {
                var symbolText = UiFactory.Label(name + "Symbol", buttonObject.transform, string.IsNullOrEmpty(symbol) ? caption : symbol, 18, FontStyle.Bold);
                symbolText.alignment = TextAnchor.MiddleCenter;
                symbolText.color = active ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Text;
                symbolText.raycastTarget = false;
                var symbolRect = symbolText.rectTransform;
                symbolRect.anchorMin = new Vector2(0.12f, 0.24f);
                symbolRect.anchorMax = new Vector2(0.88f, 0.96f);
                symbolRect.offsetMin = Vector2.zero;
                symbolRect.offsetMax = Vector2.zero;
            }

            var captionText = UiFactory.Label(name + "Text", buttonObject.transform, caption, icon != null ? 10 : 9, FontStyle.Bold);
            captionText.alignment = TextAnchor.MiddleCenter;
            captionText.color = active ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.MutedText;
            captionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            captionText.verticalOverflow = VerticalWrapMode.Truncate;
            captionText.raycastTarget = false;
            var captionRect = captionText.rectTransform;
            captionRect.anchorMin = new Vector2(0f, 0f);
            captionRect.anchorMax = new Vector2(1f, 0f);
            captionRect.pivot = new Vector2(0.5f, 0f);
            captionRect.offsetMin = new Vector2(2f, 2f);
            captionRect.offsetMax = new Vector2(-2f, 20f);
            return button;
        }

        private static Sprite CardLibraryTierIcon(int tier)
        {
            if (tier <= 0 || tier >= CardLibraryTierIconResourcePaths.Length)
            {
                return null;
            }

            var path = CardLibraryTierIconResourcePaths[tier];
            var sprite = Resources.Load<Sprite>(path);
            return sprite != null ? sprite : Resources.LoadAll<Sprite>(path).FirstOrDefault();
        }

        private static string TribeSymbol(Tribe tribe)
        {
            switch (tribe)
            {
                case Tribe.Beast: return "野";
                case Tribe.Murloc: return "鱼";
                case Tribe.Mech: return "机";
                case Tribe.Demon: return "魔";
                case Tribe.Dragon: return "龙";
                case Tribe.Pirate: return "海";
                case Tribe.Elemental: return "元";
                case Tribe.Quilboar: return "猪";
                case Tribe.Undead: return "亡";
                case Tribe.Naga: return "纳";
                case Tribe.All: return "全";
                case Tribe.None: return "中";
                default: return "中";
            }
        }

        private static Color TribeAccent(Tribe tribe)
        {
            switch (tribe)
            {
                case Tribe.Beast: return UnityTavernUiStyle.ColorFromHex(0xB56A36);
                case Tribe.Murloc: return UnityTavernUiStyle.ColorFromHex(0x2E8A89);
                case Tribe.Mech: return UnityTavernUiStyle.ColorFromHex(0x8B8F95);
                case Tribe.Demon: return UnityTavernUiStyle.ColorFromHex(0x7B3F78);
                case Tribe.Dragon: return UnityTavernUiStyle.ColorFromHex(0xB34842);
                case Tribe.Pirate: return UnityTavernUiStyle.ColorFromHex(0xB08A3C);
                case Tribe.Elemental: return UnityTavernUiStyle.ColorFromHex(0x3B70A2);
                case Tribe.Quilboar: return UnityTavernUiStyle.ColorFromHex(0x9B5C47);
                case Tribe.Undead: return UnityTavernUiStyle.ColorFromHex(0x6D7283);
                case Tribe.Naga: return UnityTavernUiStyle.ColorFromHex(0x457B66);
                case Tribe.None: return UnityTavernUiStyle.ColorFromHex(0x8A8172);
                default: return UnityTavernUiStyle.Green;
            }
        }

        private Button LibraryFilterButton(string name, Transform parent, string text, bool active, float width, Action onClick)
        {
            var button = ActionButton(name, parent, text, onClick);
            UnityTavernUiStyle.SetFixedSize(button.gameObject, width, UnityTavernUiStyle.TouchHeight);
            UnityTavernUiStyle.ConfigureButton(button, UnityTavernUiStyle.ArcaneBlue, selected: active);

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = (active ? "✓ " : string.Empty) + text;
                label.fontSize = Mathf.Max(14, label.fontSize);
                label.color = active ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.TextLight;
            }

            return button;
        }

        private string ToolsAcquisitionSubtitle(int visibleCount)
        {
            if (toolsAcquisitionKind == CardKind.Hero)
            {
                return "英雄 / " + visibleCount + "张 / 当前英雄 " + CurrentHeroName();
            }

            if (toolsAcquisitionKind == CardKind.HeroPower)
            {
                var category = toolsHeroPowerCategoryFilter.HasValue ? HeroPowerCategoryName(toolsHeroPowerCategoryFilter.Value) : "全部分类";
                var eligibility = toolsHeroPowerEligibilityFilter.HasValue ? HeroPowerEligibilityName(toolsHeroPowerEligibilityFilter.Value) : "全部资格";
                return "英雄技能 / " + category + " / " + eligibility + " / " + visibleCount + "张 / 当前技能 " + CurrentHeroPowerName();
            }

            var kind = CardLibraryKindTitle();
            var tier = toolsAcquisitionTierFilter == 0 ? "全部等级" : toolsAcquisitionTierFilter + "本";
            var tribe = ToolsAcquisitionFilterName();
            var scope = toolsShowAllCards && toolsAcquisitionKind != CardKind.HeroBuddy ? "显示全部" : "当前局";
            return kind + " / " + tier + " / " + tribe + " / " + scope + " / " + visibleCount + "张 / 手牌 " + service.State.Player.Tavern.Hand.Count + "/" + HandLimit;
        }

        private string CurrentHeroPowerName()
        {
            var cardId = service?.State?.Player?.HeroPowerCardId;
            var power = CurrentHeroPower();
            return power == null ? string.IsNullOrEmpty(cardId) ? "未设置" : cardId : power.Name;
        }

        private HeroPowerDefinition CurrentHeroPower()
        {
            var cardId = service?.State?.Player?.HeroPowerCardId;
            if (string.IsNullOrEmpty(cardId) || service?.HeroCatalog == null)
            {
                return null;
            }

            return service.HeroCatalog.AllHeroPowers.FirstOrDefault(item =>
                string.Equals(item.CardId, cardId, StringComparison.OrdinalIgnoreCase));
        }

        private List<HeroPowerDefinition> CurrentHeroPowers()
        {
            var powers = new List<HeroPowerDefinition>();
            AddCurrentHeroPower(powers, service?.State?.Player?.HeroPowerCardId);
            var extras = service?.State?.Player?.ExtraHeroPowerCardIds;
            if (extras != null)
            {
                foreach (var cardId in extras)
                {
                    AddCurrentHeroPower(powers, cardId);
                }
            }

            if (powers.Count == 0)
            {
                powers.Add(null);
            }

            return powers;
        }

        private void AddCurrentHeroPower(List<HeroPowerDefinition> powers, string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || service?.HeroCatalog == null ||
                powers.Any(power => power != null && string.Equals(power.CardId, cardId, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var powerDefinition = service.HeroCatalog.AllHeroPowers.FirstOrDefault(item =>
                string.Equals(item.CardId, cardId, StringComparison.OrdinalIgnoreCase));
            if (powerDefinition != null)
            {
                powers.Add(powerDefinition);
            }
        }

        private bool IsHeroPowerUnlocked(HeroPowerDefinition heroPower)
        {
            if (heroPower == null || service?.State?.Player == null)
            {
                return false;
            }

            if (string.Equals(service.State.Player.HeroPowerCardId, heroPower.CardId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var unlocks = service.State.Player.ExtraHeroPowerUnlockRounds;
            return unlocks == null ||
                !unlocks.TryGetValue(heroPower.CardId, out var unlockRound) ||
                service.State.Round >= Math.Max(1, unlockRound);
        }

        private string CurrentHeroName()
        {
            var cardId = service?.State?.Player?.HeroId;
            if (string.IsNullOrEmpty(cardId) || service?.HeroCatalog == null)
            {
                return "未设置";
            }

            var hero = service.HeroCatalog.AllHeroes.FirstOrDefault(item =>
                string.Equals(item.HeroCardId, cardId, StringComparison.OrdinalIgnoreCase));
            return hero == null ? cardId : hero.Name;
        }

        private HeroDefinition CurrentHero()
        {
            var cardId = service?.State?.Player?.HeroId;
            return HeroDefinitionFor(cardId);
        }

        private HeroDefinition HeroDefinitionFor(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || service?.HeroCatalog == null)
            {
                return null;
            }

            return service.HeroCatalog.AllHeroes.FirstOrDefault(item =>
                string.Equals(item.HeroCardId, cardId, StringComparison.OrdinalIgnoreCase));
        }

        private List<Tribe> ActiveLibraryTribes()
        {
            return TribeAvailabilityRules.Normalize(service?.State?.ActiveTribes);
        }

        private IEnumerable<Tribe> VisibleLibraryTribes()
        {
            yield return Tribe.None;
            var active = toolsShowAllCards
                || toolsAcquisitionKind == CardKind.HeroBuddy
                ? TribeAvailabilityRules.AllPlayableTribes()
                : ActiveLibraryTribes();
            foreach (var tribe in active)
            {
                yield return tribe;
            }
        }

        private void NormalizeToolsAcquisitionTribeFilter()
        {
            if (toolsAcquisitionKind == CardKind.Hero || toolsAcquisitionKind == CardKind.HeroPower || toolsAcquisitionKind == CardKind.HeroBuddy)
            {
                return;
            }

            if (toolsAcquisitionTribeFilter == Tribe.All || toolsAcquisitionTribeFilter == Tribe.None || toolsShowAllCards)
            {
                return;
            }

            if (!ActiveLibraryTribes().Contains(toolsAcquisitionTribeFilter))
            {
                toolsAcquisitionTribeFilter = Tribe.All;
            }
        }

        private string ToolsAcquisitionFilterName()
        {
            if (toolsAcquisitionKind == CardKind.HeroPower)
            {
                return toolsHeroPowerCategoryFilter.HasValue ? HeroPowerCategoryName(toolsHeroPowerCategoryFilter.Value) : "全部分类";
            }

            if (toolsAcquisitionTribeFilter == Tribe.All)
            {
                return "全部";
            }

            if (toolsAcquisitionKind == CardKind.TavernSpell && toolsAcquisitionTribeFilter == Tribe.None)
            {
                return T("通用法术", "General Spell");
            }

            return TribeName(toolsAcquisitionTribeFilter);
        }

        private IEnumerable<MinionInstance> FilteredToolsAcquisitionChoices()
        {
            NormalizeToolsAcquisitionTribeFilter();
            var choices = cardLibraryDestination == UnityCardLibraryDestination.OpponentStartOfCombatSpell
                ? BuildToolsAcquisitionSpellChoices().Where(card => TavernSpellEngine.IsStartOfCombatSpell(card.CardId))
                : BuildToolsAcquisitionChoices();

            if (toolsAcquisitionKind != CardKind.Hero && toolsAcquisitionKind != CardKind.HeroPower && toolsAcquisitionTierFilter > 0)
            {
                choices = choices.Where(card => card.TavernTier == toolsAcquisitionTierFilter);
            }

            if ((toolsAcquisitionKind == CardKind.Minion || toolsAcquisitionKind == CardKind.HeroBuddy) && toolsAcquisitionTribeFilter != Tribe.All)
            {
                choices = choices.Where(card => MatchesToolsAcquisitionTribe(card, toolsAcquisitionTribeFilter));
            }
            else if (toolsAcquisitionKind == CardKind.TavernSpell && toolsAcquisitionTribeFilter != Tribe.All)
            {
                choices = choices.Where(card => MatchesToolsAcquisitionSpellTribe(card, toolsAcquisitionTribeFilter));
            }

            if (!string.IsNullOrWhiteSpace(toolsAcquisitionSearchText))
            {
                choices = choices.Where(CardLibraryMatchesSearch);
            }

            return choices
                .OrderBy(card => card.TavernTier)
                .ThenBy(card => card.Name)
                .ToList();
        }

        private bool CardLibraryMatchesSearch(MinionInstance card)
        {
            if (card == null)
            {
                return false;
            }

            var query = toolsAcquisitionSearchText.Trim();
            return ContainsIgnoreCase(card.Name, query) ||
                   ContainsIgnoreCase(card.Text, query) ||
                   (card.Keywords != null && card.Keywords.Any(keyword => ContainsIgnoreCase(KeywordName(keyword), query))) ||
                   (card.Tribes != null && card.Tribes.Any(tribe => ContainsIgnoreCase(TribeName(tribe), query)));
        }

        private IEnumerable<MinionInstance> BuildToolsAcquisitionChoices()
        {
            switch (toolsAcquisitionKind)
            {
                case CardKind.TavernSpell:
                    return BuildToolsAcquisitionSpellChoices();
                case CardKind.Hero:
                    return BuildToolsAcquisitionHeroChoices();
                case CardKind.HeroPower:
                    return BuildToolsAcquisitionHeroPowerChoices();
                case CardKind.HeroBuddy:
                    return BuildToolsAcquisitionHeroBuddyChoices();
                default:
                    return BuildToolsAcquisitionMinionChoices();
            }
        }

        private IEnumerable<MinionInstance> BuildToolsAcquisitionHeroChoices()
        {
            var definitions = service.HeroCatalog.AllHeroes
                .Where(hero => !string.IsNullOrEmpty(hero.HeroCardId))
                .GroupBy(hero => hero.HeroCardId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First());
            foreach (var definition in definitions)
            {
                yield return MinionFactory.Create(definition, BoardSide.Player, "unity-tools-library");
            }
        }

        private IEnumerable<MinionInstance> BuildToolsAcquisitionMinionChoices()
        {
            var definitions = service.Catalogs.Minions.All
                .Where(card =>
                    (service.IsMinionAllowedByCardPool(card) || card.Tags.Contains("hero_derivative")) &&
                    !card.CardId.StartsWith("BGDUO"));
            var timewarped = service.GetTimewarpedDebugMinionCards().AsEnumerable();
            if (!toolsShowAllCards)
            {
                var active = ActiveLibraryTribes();
                definitions = definitions.Where(card =>
                    card.Tags.Contains("hero_derivative") ||
                    TribeAvailabilityRules.IsMinionAvailable(card, active));
                timewarped = timewarped.Where(card => MatchesToolsAcquisitionTribeAvailability(card, active));
            }

            foreach (var definition in definitions)
            {
                yield return MinionFactory.Create(definition, BoardSide.Player, "unity-tools-library", false, PoolSource.Debug, 0);
            }

            foreach (var card in timewarped)
            {
                yield return card;
            }
        }

        private static bool MatchesToolsAcquisitionTribeAvailability(MinionInstance card, IReadOnlyCollection<Tribe> activeTribes)
        {
            if (card == null)
            {
                return false;
            }

            if (card.Tribes == null || card.Tribes.Count == 0 || card.Tribes.All(tribe => tribe == Tribe.None) || card.Tribes.Contains(Tribe.All))
            {
                return true;
            }

            var active = TribeAvailabilityRules.Normalize(activeTribes);
            return card.Tribes.Any(active.Contains);
        }

        private IEnumerable<MinionInstance> BuildToolsAcquisitionSpellChoices()
        {
            var definitions = service.Catalogs.Spells.All
                .Where(spell => service.IsTavernSpellAllowedByCardPool(spell) && !spell.CardNumber.StartsWith("BGDUO"));
            if (!toolsShowAllCards)
            {
                var active = ActiveLibraryTribes();
                definitions = definitions.Where(spell => TribeAvailabilityRules.IsTavernSpellAvailable(spell, active));
            }

            foreach (var definition in definitions)
            {
                var spell = MinionFactory.Create(definition, BoardSide.Player, "unity-tools-library");
                spell.PoolSource = PoolSource.Debug;
                spell.OriginPoolSource = PoolSource.Debug;
                var tribes = TribeAvailabilityRules.SpellTribes(definition);
                spell.Tribes = tribes.Count == 0 ? new List<Tribe> { Tribe.None } : tribes.ToList();
                yield return spell;
            }
        }

        private IEnumerable<MinionInstance> BuildToolsAcquisitionHeroPowerChoices()
        {
            var definitions = service.HeroCatalog.AllHeroPowers.AsEnumerable();
            if (toolsHeroPowerCategoryFilter.HasValue)
            {
                definitions = definitions.Where(power => power.PrimaryCategory == toolsHeroPowerCategoryFilter.Value);
            }

            if (toolsHeroPowerEligibilityFilter.HasValue)
            {
                definitions = definitions.Where(power => power.ReplacementEligibility == toolsHeroPowerEligibilityFilter.Value);
            }

            foreach (var definition in definitions)
            {
                yield return MinionFactory.Create(definition, BoardSide.Player, "unity-tools-library");
            }
        }

        private IEnumerable<MinionInstance> BuildToolsAcquisitionHeroBuddyChoices()
        {
            var heroes = service.HeroCatalog.AllHeroes
                .Where(hero => hero.Buddy != null && !string.IsNullOrEmpty(hero.Buddy.CardId))
                .GroupBy(hero => hero.Buddy.CardId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First());
            foreach (var hero in heroes)
            {
                var buddy = MinionFactory.Create(hero.Buddy, BoardSide.Player, "unity-tools-library", PoolSource.Debug);
                if (!string.IsNullOrEmpty(hero.Name))
                {
                    buddy.Tags.Add("hero:" + hero.Name);
                }

                yield return buddy;
            }
        }

        private string ToolsAcquisitionCardMeta(MinionInstance card)
        {
            if (card.CardKind == CardKind.TavernSpell)
            {
                return T("", "Tier ") + card.TavernTier + T("本 / ", " / ") + card.Cost + T("费 / ", " Cost / ") + SpellTribesText(card);
            }

            if (card.CardKind == CardKind.HeroPower)
            {
                return Math.Max(0, card.Cost) + T("费 / ", " Cost / ") + HeroPowerTagValue(card, "category") + " / " + HeroPowerTagValue(card, "eligibility");
            }

            if (card.CardKind == CardKind.Hero)
            {
                return TavernNumberFormatter.FullNumber(card.Health) + T("生命 / ", " Health / ") + TavernNumberFormatter.FullNumber(card.Attack) + T("护甲", " Armor") + HeroPowerSuffix(card);
            }

            if (card.CardKind == CardKind.HeroBuddy)
            {
                return T("", "Tier ") + card.TavernTier + T("本 / ", " / ") + TavernNumberFormatter.FullStats(card.Attack, card.Health) + " / " + TribesText(card) + HeroBuddyHeroSuffix(card);
            }

            return T("", "Tier ") + card.TavernTier + T("本 / ", " / ") + TavernNumberFormatter.FullStats(card.Attack, card.Health) + " / " + TribesText(card);
        }

        private static string HeroPowerTagValue(MinionInstance card, string prefix)
        {
            if (card?.Tags == null)
            {
                return "未分类";
            }

            var value = card.Tags.FirstOrDefault(tag => tag.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(value))
            {
                return "未分类";
            }

            var raw = value.Substring(prefix.Length + 1);
            if (prefix == "category" && Enum.TryParse(raw, out HeroPowerCategory category))
            {
                return HeroPowerCategoryName(category);
            }

            if (prefix == "eligibility" && Enum.TryParse(raw, out HeroPowerReplacementEligibility eligibility))
            {
                return HeroPowerEligibilityName(eligibility);
            }

            return raw;
        }

        private static string HeroBuddyHeroSuffix(MinionInstance card)
        {
            var hero = card?.Tags?.FirstOrDefault(tag => tag.StartsWith("hero:", StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(hero) ? string.Empty : " / " + hero.Substring("hero:".Length);
        }

        private static string HeroPowerSuffix(MinionInstance card)
        {
            var power = card?.Tags?.FirstOrDefault(tag => tag.StartsWith("hero_power:", StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(power) ? string.Empty : " / " + power.Substring("hero_power:".Length);
        }

        private static string HeroPowerCategoryName(HeroPowerCategory category)
        {
            switch (category)
            {
                case HeroPowerCategory.Economy: return "经济";
                case HeroPowerCategory.Buff: return "增益";
                case HeroPowerCategory.Combat: return "战斗";
                case HeroPowerCategory.Minion: return "随从";
                case HeroPowerCategory.Discover: return "发现";
                case HeroPowerCategory.Health: return "生命";
                case HeroPowerCategory.Passive: return "被动";
                case HeroPowerCategory.HeroSwap: return "换技能";
                default: return "其他";
            }
        }

        private static string HeroPowerCategorySymbol(HeroPowerCategory category)
        {
            switch (category)
            {
                case HeroPowerCategory.Economy: return "金";
                case HeroPowerCategory.Buff: return "增";
                case HeroPowerCategory.Combat: return "战";
                case HeroPowerCategory.Minion: return "随";
                case HeroPowerCategory.Discover: return "发";
                case HeroPowerCategory.Health: return "命";
                case HeroPowerCategory.Passive: return "被";
                case HeroPowerCategory.HeroSwap: return "换";
                default: return "其";
            }
        }

        private static string HeroPowerEligibilityName(HeroPowerReplacementEligibility eligibility)
        {
            switch (eligibility)
            {
                case HeroPowerReplacementEligibility.DiscoverableAfterStart: return "可替换";
                case HeroPowerReplacementEligibility.InitialOnly: return "开局限定";
                case HeroPowerReplacementEligibility.NonSelectable: return "不可选择";
                case HeroPowerReplacementEligibility.Disabled: return "未启用";
                default: return "未知";
            }
        }

        private static string HeroPowerEligibilitySymbol(HeroPowerReplacementEligibility eligibility)
        {
            switch (eligibility)
            {
                case HeroPowerReplacementEligibility.DiscoverableAfterStart: return "可";
                case HeroPowerReplacementEligibility.InitialOnly: return "初";
                case HeroPowerReplacementEligibility.NonSelectable: return "禁";
                case HeroPowerReplacementEligibility.Disabled: return "停";
                default: return "？";
            }
        }

        private string TribesText(MinionInstance card)
        {
            if (IsNeutralLibraryMinion(card))
            {
                return T("中立", "Neutral");
            }

            if (card.Tribes.Contains(Tribe.All))
            {
                return T("全部种族", "All Tribes");
            }

            var tribes = card.Tribes.Where(tribe => tribe != Tribe.None).Take(2).Select(TribeName).ToArray();
            return tribes.Length == 0 ? T("中立", "Neutral") : string.Join("/", tribes);
        }

        private static bool MatchesToolsAcquisitionTribe(MinionInstance card, Tribe tribe)
        {
            if (tribe == Tribe.All)
            {
                return true;
            }

            if (tribe == Tribe.None)
            {
                return IsNeutralLibraryMinion(card);
            }

            return card.Tribes != null && (card.Tribes.Contains(tribe) || card.Tribes.Contains(Tribe.All));
        }

        private static bool IsNeutralLibraryMinion(MinionInstance card)
        {
            return card == null || card.Tribes == null || card.Tribes.Count == 0 || card.Tribes.All(tribe => tribe == Tribe.None);
        }

        private static bool MatchesToolsAcquisitionSpellTribe(MinionInstance card, Tribe tribe)
        {
            if (tribe == Tribe.All)
            {
                return true;
            }

            if (tribe == Tribe.None)
            {
                return card == null || card.Tribes == null || card.Tribes.Count == 0 || card.Tribes.All(value => value == Tribe.None);
            }

            return card != null && card.Tribes != null && card.Tribes.Contains(tribe);
        }

        private string SpellTribesText(MinionInstance card)
        {
            if (card == null || card.Tribes == null || card.Tribes.Count == 0 || card.Tribes.All(tribe => tribe == Tribe.None))
            {
                return "通用法术";
            }

            return string.Join("/", card.Tribes.Where(tribe => tribe != Tribe.None).Select(TribeName).ToArray());
        }

        private string TribeName(Tribe tribe)
        {
            switch (tribe)
            {
                case Tribe.Beast: return T("野兽", "Beast");
                case Tribe.Murloc: return T("鱼人", "Murloc");
                case Tribe.Mech: return T("机械", "Mech");
                case Tribe.Demon: return T("恶魔", "Demon");
                case Tribe.Dragon: return T("龙", "Dragon");
                case Tribe.Pirate: return T("海盗", "Pirate");
                case Tribe.Elemental: return T("元素", "Elemental");
                case Tribe.Quilboar: return T("野猪人", "Quilboar");
                case Tribe.Undead: return T("亡灵", "Undead");
                case Tribe.Naga: return T("纳迦", "Naga");
                case Tribe.All: return T("全部", "All");
                case Tribe.None: return T("中立", "Neutral");
                default: return T("中立", "Neutral");
            }
        }

        private static string SafeObjectName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Unknown";
            }

            return value.Replace(" ", string.Empty)
                .Replace("/", "-")
                .Replace("\\", "-")
                .Replace(":", "-")
                .Replace(".", "-");
        }

        private void AddFirstMinionToHand()
        {
            var active = ActiveLibraryTribes();
            var definition = service.Catalogs.Minions.All.FirstOrDefault(card =>
                service.IsMinionAllowedByCardPool(card) &&
                card.TavernTier <= Math.Max(1, service.State.Player.Tavern.Tier) &&
                TribeAvailabilityRules.IsMinionAvailable(card, active));
            if (definition != null)
            {
                Apply(new GameCommand(GameCommandType.AddCardToHand, definition.CardId, CardKind.Minion));
            }
        }

        private void AddFirstSpellToHand()
        {
            var active = ActiveLibraryTribes();
            var definition = service.Catalogs.Spells.All.FirstOrDefault(spell =>
                service.IsTavernSpellAllowedByCardPool(spell) &&
                TribeAvailabilityRules.IsTavernSpellAvailable(spell, active));
            if (definition != null)
            {
                Apply(new GameCommand(GameCommandType.AddCardToHand, definition.CardNumber, CardKind.TavernSpell));
            }
        }

        private void AddFirstOpponentMinion()
        {
            var active = ActiveLibraryTribes();
            var definition = service.Catalogs.Minions.All.FirstOrDefault(card =>
                service.IsMinionAllowedByCardPool(card) &&
                card.TavernTier <= Math.Max(1, service.State.Player.Tavern.Tier) &&
                TribeAvailabilityRules.IsMinionAvailable(card, active));
            if (definition != null)
            {
                Apply(new GameCommand(GameCommandType.AddOpponentMinion, definition.CardId));
            }
        }

        private void ReturnSelectedToHand()
        {
            var selected = SelectedPlayerBoardCard();
            if (selected != null)
            {
                Apply(new GameCommand(GameCommandType.MoveMinion, selected.InstanceId));
            }
        }

        private void RemoveSelectedOpponent()
        {
            var selected = SelectedOpponentCard();
            if (selected != null)
            {
                Apply(new GameCommand(GameCommandType.RemoveOpponentMinion, selected.InstanceId));
            }
        }

        private void MoveSelectedOpponent(int delta)
        {
            var selected = SelectedOpponentCard();
            if (selected == null)
            {
                return;
            }

            var current = service.State.Opponent.Board.IndexOf(selected);
            var target = Mathf.Clamp(current + delta, 0, service.State.Opponent.Board.Count - 1);
            if (target != current)
            {
                Apply(new GameCommand(GameCommandType.MoveOpponentMinion, selected.InstanceId, target));
            }
        }

        private void PatchSelected(MinionPatch patch)
        {
            var selected = FindSelectedCard();
            if (selected == null)
            {
                return;
            }

            if (SelectedOpponentCard() != null)
            {
                Apply(new GameCommand(GameCommandType.UpdateOpponentMinion, selected.InstanceId, patch));
            }
            else
            {
                Apply(new GameCommand(GameCommandType.UpdateMinion, selected.InstanceId, patch));
            }
        }

        private static int IncrementStat(int value)
        {
            return value >= int.MaxValue ? int.MaxValue : value + 1;
        }

        private void OpenMinionEditor(MinionInstance card)
        {
            if (card == null || card.CardKind == CardKind.TavernSpell)
            {
                return;
            }

            if (service.State.Opponent.Board.Any(item => item != null && item.InstanceId == card.InstanceId))
            {
                minionEditorSide = BoardSide.Opponent;
            }
            else if (service.State.Player.Board.Any(item => item != null && item.InstanceId == card.InstanceId))
            {
                minionEditorSide = BoardSide.Player;
            }
            else
            {
                return;
            }

            selectedInstanceId = card.InstanceId;
            minionEditorInstanceId = card.InstanceId;
            toolsOpen = false;
            cardDetailOpen = false;
            HideKeywordTooltip(card);
            Rebuild();
        }

        private void CloseMinionEditor()
        {
            minionEditorInstanceId = null;
            Rebuild();
        }

        private void ApplyMinionEditorPatch(MinionPatch patch)
        {
            var target = MinionEditorTarget();
            if (target == null || patch == null)
            {
                return;
            }

            var side = minionEditorSide;
            minionEditorInstanceId = null;
            Apply(new GameCommand(side == BoardSide.Opponent ? GameCommandType.UpdateOpponentMinion : GameCommandType.UpdateMinion, target.InstanceId, patch));
        }

        private void ApplyPatchToPlayerBoard(MinionPatch patch)
        {
            if (patch == null)
            {
                return;
            }

            var commands = service.State.Player.Board
                .Where(card => card != null && card.CardKind != CardKind.TavernSpell)
                .Select(card => new GameCommand(GameCommandType.UpdateMinion, card.InstanceId, ClonePatch(patch)))
                .ToList();
            minionEditorInstanceId = null;
            ApplyBatch(commands, "已套用己方随从");
        }

        private void ApplyPatchToOpponentBoard(MinionPatch patch)
        {
            if (patch == null)
            {
                return;
            }

            var commands = service.State.Opponent.Board
                .Where(card => card != null && card.CardKind != CardKind.TavernSpell)
                .Select(card => new GameCommand(GameCommandType.UpdateOpponentMinion, card.InstanceId, ClonePatch(patch)))
                .ToList();
            minionEditorInstanceId = null;
            ApplyBatch(commands, "已套用敌方随从");
        }

        private void ApplyBatch(IReadOnlyList<GameCommand> commands, string feedback)
        {
            try
            {
                lastError = null;
                foreach (var command in commands)
                {
                    ExecuteCommand(command);
                }

                lastFeedback = feedback;
                selectedInstanceId = FindSelectedCard()?.InstanceId ?? service.State.Player.Tavern.Shop.FirstOrDefault(card => card != null)?.InstanceId;
            }
            catch (Exception ex)
            {
                lastError = service.LocalizePlayerMessage(ex.Message);
                lastFeedback = null;
                confirmedTargetInstanceId = null;
            }

            Rebuild();
        }

        private MinionInstance MinionEditorTarget()
        {
            return minionEditorSide == BoardSide.Opponent
                ? service.State.Opponent.Board.FirstOrDefault(card => card != null && card.InstanceId == minionEditorInstanceId)
                : service.State.Player.Board.FirstOrDefault(card => card != null && card.InstanceId == minionEditorInstanceId);
        }

        private static MinionPatch ClonePatch(MinionPatch patch)
        {
            return new MinionPatch
            {
                Attack = patch.Attack,
                Health = patch.Health,
                MaxHealth = patch.MaxHealth,
                Golden = patch.Golden,
                Keywords = patch.Keywords == null ? null : new List<Keyword>(patch.Keywords),
                Tribes = patch.Tribes == null ? null : new List<Tribe>(patch.Tribes)
            };
        }

        private void LoadFirstScenario()
        {
            var scenarioName = service.TestScenarioNames.FirstOrDefault();
            LoadScenario(scenarioName);
        }

        private void LoadScenario(string scenarioName)
        {
            if (string.IsNullOrEmpty(scenarioName))
            {
                return;
            }

            Apply(new GameCommand(GameCommandType.LoadTestScenario, scenarioName, new CombatTestOptions()));
        }

        private MinionInstance SelectedPlayerBoardCard()
        {
            return service.State.Player.Board.FirstOrDefault(card => card.InstanceId == selectedInstanceId);
        }

        private MinionInstance SelectedOpponentCard()
        {
            return service.State.Opponent.Board.FirstOrDefault(card => card.InstanceId == selectedInstanceId);
        }

        private int DefaultCombatSeed()
        {
            return service.State.Seed + service.State.Round;
        }

        private string DefaultScenarioName()
        {
            return "round-" + service.State.Round + "-battle-test";
        }

        private void BuildHandBuyDropZone(Transform parent)
        {
            BuildPlayerPurchaseHeroAnchor(parent);
            BuildWideHandDropZone(parent, "UnityHandBuyDropZone", UnityTavernDropTarget.PurchaseZone);
            BuildWideHandDropZone(parent, "UnityDiscoverHandDropZone", UnityTavernDropTarget.DiscoverZone);
        }

        private void BuildPurchaseDropZone(Transform parent, string name)
        {
            BuildWideHandDropZone(parent, name, UnityTavernDropTarget.PurchaseZone);
        }

        private void BuildWideHandDropZone(Transform parent, string name, UnityTavernDropTarget target)
        {
            var zone = Panel(name, parent, Color.clear);
            zone.transform.SetAsLastSibling();
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(zone).ignoreLayout = true;
            var rect = zone.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 6f);
            rect.offsetMax = new Vector2(-8f, -30f);
            BuildPurchaseDropZoneBorder(zone.transform);
            AddDropTarget(
                zone,
                target,
                service.State.Player.Tavern.Hand.Count,
                raycastOnlyWhenAllowed: true,
                activeOnlyWhenAllowed: true,
                cueOnlyWhenAllowed: true);
            zone.SetActive(false);
        }

        private static void BuildPurchaseDropZoneBorder(Transform parent)
        {
            var color = UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.FocusRing, 0.84f);
            BuildPurchaseDropZoneBorderEdge(parent, "Top", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -2f), Vector2.zero, color);
            BuildPurchaseDropZoneBorderEdge(parent, "Bottom", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 2f), color);
            BuildPurchaseDropZoneBorderEdge(parent, "Left", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(2f, 0f), color);
            BuildPurchaseDropZoneBorderEdge(parent, "Right", new Vector2(1f, 0f), Vector2.one, new Vector2(-2f, 0f), Vector2.zero, color);
        }

        private static void BuildPurchaseDropZoneBorderEdge(
            Transform parent,
            string edge,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            var border = Panel("UnityPurchaseDropBorder" + edge, parent, color);
            var rect = border.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private Transform BuildPlayerPurchaseHeroAnchor(Transform parent)
        {
            var layout = LayoutContext();
            var anchor = Panel(
                "UnityPlayerPurchaseHeroAnchor",
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.98f));
            anchor.transform.SetAsLastSibling();
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(anchor).ignoreLayout = true;
            UnityTavernUiStyle.ConfigureOutline(
                anchor,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.76f),
                new Vector2(2f, -2f));
            var rect = anchor.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(layout.IsCompact ? 56f : 84f, layout.IsCompact ? 58f : 86f);
            rect.sizeDelta = layout.IsCompact ? new Vector2(66f, 78f) : new Vector2(86f, 104f);

            var hero = CurrentHero();
            var portrait = Panel("UnityPlayerPurchaseHeroPortrait", anchor.transform, UnityTavernUiStyle.PanelQuiet);
            var portraitRect = portrait.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.08f, 0.24f);
            portraitRect.anchorMax = new Vector2(0.92f, 0.94f);
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;
            var portraitImage = portrait.GetComponent<Image>();
            var sprite = hero == null
                ? null
                : CardImageProvider.LoadSprite(hero.ImagePath, hero.HeroCardId, CardKind.Hero);
            if (sprite != null)
            {
                portraitImage.sprite = sprite;
                portraitImage.color = Color.white;
                portraitImage.preserveAspect = true;
            }

            var name = UiFactory.Label(
                "UnityPlayerPurchaseHeroName",
                anchor.transform,
                hero == null ? T("我方英雄", "Your Hero") : hero.Name,
                layout.IsCompact ? 10 : 12,
                FontStyle.Bold);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = UnityTavernUiStyle.TextLight;
            name.raycastTarget = false;
            var nameRect = name.rectTransform;
            nameRect.anchorMin = new Vector2(0.04f, 0.02f);
            nameRect.anchorMax = new Vector2(0.96f, 0.23f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            return anchor.transform;
        }

        private void BuildPlayerBoardPhysicalDropZone(Transform parent)
        {
            var zone = Panel("UnityPlayerBoardPhysicalDropZone", parent, Color.clear);
            zone.transform.SetAsLastSibling();
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(zone).ignoreLayout = true;
            var zoneRect = zone.GetComponent<RectTransform>();
            zoneRect.anchorMin = Vector2.zero;
            zoneRect.anchorMax = Vector2.one;
            zoneRect.offsetMin = new Vector2(8f, 6f);
            zoneRect.offsetMax = new Vector2(-8f, -30f);

            AddDropTarget(
                zone,
                UnityTavernDropTarget.PlayerBoardInsert,
                0,
                raycastOnlyWhenAllowed: true,
                activeOnlyWhenAllowed: true,
                cueOnlyWhenAllowed: true,
                suppressCueVisuals: true,
                resolveIndexFromPointer: true);
            zone.SetActive(false);
        }

        private void BuildShopPhysicalDropZone(Transform parent)
        {
            var zone = Panel("UnityShopPhysicalDropZone", parent, Color.clear);
            zone.transform.SetAsLastSibling();
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(zone).ignoreLayout = true;
            var zoneRect = zone.GetComponent<RectTransform>();
            zoneRect.anchorMin = Vector2.zero;
            zoneRect.anchorMax = Vector2.one;
            zoneRect.offsetMin = new Vector2(8f, 6f);
            zoneRect.offsetMax = new Vector2(-8f, -30f);

            AddDropTarget(
                zone,
                UnityTavernDropTarget.TavernShopInsert,
                0,
                raycastOnlyWhenAllowed: true,
                activeOnlyWhenAllowed: true,
                cueOnlyWhenAllowed: true,
                suppressCueVisuals: true,
                resolveIndexFromPointer: true);
            zone.SetActive(false);
        }

        private void BuildSpellCastDropZone(Transform parent)
        {
            var zone = BuildDragDropOverlay(
                "UnitySpellCastDropZone",
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.ArcaneBlue, 0.78f),
                new Vector2(0.20f, 0.12f),
                new Vector2(0.80f, 0.88f),
                Vector2.zero,
                Vector2.zero,
                T("释放施放", "Release to cast"),
                T("将法术拖出手牌后在这里松开", "Drag the spell out of your hand and release here"));
            UnityTavernUiStyle.ConfigureOutline(
                zone,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.FocusRing, 0.92f),
                new Vector2(3f, -3f));
            AddDropTarget(
                zone,
                UnityTavernDropTarget.CastZone,
                0,
                raycastOnlyWhenAllowed: true,
                activeOnlyWhenAllowed: true,
                cueOnlyWhenAllowed: true);
            zone.SetActive(false);
        }

        private void BuildBoardReorderDropZone(
            Transform parent,
            string name,
            UnityTavernDropTarget target,
            Color color,
            string labelText,
            string hintText)
        {
            var zone = BuildDragDropOverlay(
                name,
                parent,
                color,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(10f, 8f),
                new Vector2(-10f, -34f),
                labelText,
                hintText);
            AddDropTarget(
                zone,
                target,
                0,
                raycastOnlyWhenAllowed: true,
                activeOnlyWhenAllowed: true,
                cueOnlyWhenAllowed: true,
                resolveIndexFromPointer: true,
                indexSlotCount: BoardLimit);
            zone.SetActive(false);
        }

        private void BuildShopSellDropZone(Transform parent)
        {
            var zone = Panel("UnitySellDropZone", parent, Color.clear);
            zone.transform.SetAsLastSibling();
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(zone).ignoreLayout = true;
            var zoneRect = zone.GetComponent<RectTransform>();
            zoneRect.anchorMin = Vector2.zero;
            zoneRect.anchorMax = Vector2.one;
            zoneRect.offsetMin = new Vector2(8f, 6f);
            zoneRect.offsetMax = new Vector2(-8f, -30f);

            var railColor = UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Red, 0.52f);
            BuildDropZoneBorderEdge(
                zone.transform,
                "UnitySellDropBorderTop",
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, -2f),
                Vector2.zero,
                railColor);
            BuildDropZoneBorderEdge(
                zone.transform,
                "UnitySellDropBorderBottom",
                Vector2.zero,
                new Vector2(1f, 0f),
                Vector2.zero,
                new Vector2(0f, 2f),
                railColor);

            var hint = Panel(
                "UnitySellHintPill",
                zone.transform,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.78f));
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintRect.pivot = new Vector2(0.5f, 0.5f);
            hintRect.sizeDelta = new Vector2(LayoutContext().IsCompact ? 190f : 238f, 38f);
            UnityTavernUiStyle.ConfigureOutline(hint, railColor, new Vector2(1f, -1f));
            var hintLabel = UiFactory.Label(
                "UnitySellHintText",
                hint.transform,
                T("松手出售 · +1 金币", "Release to sell · +1 Gold"),
                14,
                FontStyle.Bold);
            hintLabel.alignment = TextAnchor.MiddleCenter;
            hintLabel.color = UnityTavernUiStyle.TextLight;
            hintLabel.raycastTarget = false;
            UnityTavernUiStyle.Stretch(hintLabel.rectTransform);

            var coinFeedback = UiFactory.Label(
                "UnitySellCoinFeedback",
                hint.transform,
                T("金币到账", "Gold received"),
                16,
                FontStyle.Bold);
            coinFeedback.alignment = TextAnchor.MiddleCenter;
            coinFeedback.color = UnityTavernUiStyle.Gold;
            coinFeedback.raycastTarget = false;
            UnityTavernUiStyle.Stretch(coinFeedback.rectTransform);
            coinFeedback.gameObject.SetActive(false);
            AddDropTarget(
                zone,
                UnityTavernDropTarget.SellZone,
                -1,
                raycastOnlyWhenAllowed: true,
                activeOnlyWhenAllowed: true,
                cueOnlyWhenAllowed: true);
            zone.SetActive(false);
        }

        private static void BuildDropZoneBorderEdge(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            var border = Panel(name, parent, color);
            var rect = border.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            border.GetComponent<Image>().raycastTarget = false;
        }

        private GameObject BuildDragDropOverlay(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            string labelText,
            string hintText)
        {
            var zone = Panel(name, parent, color);
            zone.transform.SetAsLastSibling();

            var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(zone);
            element.ignoreLayout = true;

            var rect = zone.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var layout = zone.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var label = UiFactory.Label(name + "Text", zone.transform, labelText, 18, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.TextLight;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, 28f);

            var hint = UiFactory.Label(name + "Hint", zone.transform, hintText, 14, FontStyle.Bold);
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = UnityTavernUiStyle.TextLight;
            UnityTavernUiStyle.SetPreferredHeight(hint.gameObject, 20f);

            return zone;
        }

        private void BuildDiscoverModal()
        {
            var modal = UnityTavernDiscoverModalComponent.CreateModalHost(transform, "UnityDiscoverOverlay");
            modal.GetComponent<UnityTavernDiscoverModalComponent>().Build(
                IsChooseOneDiscoverState() ? T("抉择：选择一种效果", "Choose One") : T("发现奖励", "Discover"),
                BuildDiscoverOptions);
        }

        private bool IsChooseOneDiscoverState()
        {
            var source = service.State.Player.Tavern.Discover?.Source ?? string.Empty;
            return source.StartsWith("sprightly-scarab:", StringComparison.Ordinal) ||
                   string.Equals(source, "fearless-foodie", StringComparison.Ordinal) ||
                   source.StartsWith("intrepid-botanist:", StringComparison.Ordinal) ||
                   source.StartsWith("season14-quilboar:", StringComparison.Ordinal);
        }

        private void BuildHandChooseOneModal()
        {
            var modal = UnityTavernDiscoverModalComponent.CreateModalHost(transform, "UnityHandChooseOneOverlay");
            modal.GetComponent<UnityTavernDiscoverModalComponent>().Build(
                T("抉择：选择一种效果", "Choose One"),
                BuildHandChooseOneOptions);
        }

        private void BuildHandChooseOneOptions(Transform parent)
        {
            if (pendingHandChooseOneCard == null ||
                !ChooseOneOptionRegistry.TryGetOptions(pendingHandChooseOneCard.CardId, out var options))
            {
                return;
            }

            foreach (var option in options)
            {
                var optionCard = CreateHandChooseOneOptionCard(pendingHandChooseOneCard, option);
                var cardObject = UnityTavernCardComponent.CreateCardHost(
                    UnityTavernCardMode.Detail,
                    parent,
                    "UnityHandChooseOneOption-" + option.ChoiceId);
                cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                    optionCard,
                    UnityTavernCardMode.Detail,
                    T("选择", "Choose"),
                    null,
                    _ => SelectHandChooseOneOption(option),
                    useEnglish: service.UseEnglish);
                cardObject.name = "UnityHandChooseOneOption-" + option.ChoiceId;
            }
        }

        private static MinionInstance CreateHandChooseOneOptionCard(
            MinionInstance source,
            ChooseOneOptionDefinition option)
        {
            var card = source.Clone();
            card.InstanceId = source.InstanceId + ":choice:" + option.ChoiceId;
            card.Name = option.EnglishName;
            card.ZhName = option.Name;
            card.Text = option.EnglishText;
            card.ZhText = option.Text;
            card.Cost = 0;
            return card;
        }

        private void BeginHandChooseOne(MinionInstance card, int handIndex)
        {
            if (card == null ||
                handIndex < 0 ||
                !ChooseOneOptionRegistry.TryGetOptions(card.CardId, out var options))
            {
                return;
            }

            ClearDropTargetCues();
            activeDrag = null;
            ClearPendingPrimaryTarget();
            FinalizePhysicalDragVisuals(true);
            ClearTargetingHover();
            pendingHandChooseOneCard = card;
            pendingHandChooseOneIndex = handIndex;
            pendingHandChooseOneOption = null;

            if (TavernSpellEngine.ShouldResolveChooseOneBothEffects(card))
            {
                if (options.Any(option => option.RequiresPlayerTarget))
                {
                    pendingHandChooseOneOption = new ChooseOneOptionDefinition(
                        "both",
                        "两种效果",
                        "Both Effects",
                        "本牌同时拥有两种效果。请选择效果目标。",
                        "This card has both effects. Choose an effect target.",
                        true);
                    activeDrag = new UnityTavernDragContext(card, UnityTavernDragSource.Hand, handIndex, true);
                    selectedInstanceId = card.InstanceId;
                    lastFeedback = T("两种效果同时生效，请指向目标。", "Both effects apply. Choose a target.");
                    Rebuild();
                    return;
                }

                ClearPendingHandChooseOne();
                Apply(new GameCommand(GameCommandType.PlayMinion, handIndex));
                return;
            }

            lastError = null;
            lastFeedback = T("请选择一种抉择效果。", "Choose one effect.");
            Rebuild();
        }

        private void SelectHandChooseOneOption(ChooseOneOptionDefinition option)
        {
            if (pendingHandChooseOneCard == null || option == null)
            {
                return;
            }

            pendingHandChooseOneOption = option;
            if (option.RequiresPlayerTarget)
            {
                activeDrag = new UnityTavernDragContext(
                    pendingHandChooseOneCard,
                    UnityTavernDragSource.Hand,
                    pendingHandChooseOneIndex,
                    true);
                selectedInstanceId = pendingHandChooseOneCard.InstanceId;
                lastError = null;
                lastFeedback = T("抉择已选定，请用箭头指向目标。", "Choice selected. Point the arrow at a target.");
                Rebuild();
                return;
            }

            var command = new GameCommand(
                GameCommandType.PlayMinion,
                pendingHandChooseOneIndex,
                PlayIntent.Unspecified,
                choiceId: option.ChoiceId);
            ClearPendingHandChooseOne();
            Apply(command);
        }

        private void ClearPendingHandChooseOne()
        {
            pendingHandChooseOneCard = null;
            pendingHandChooseOneIndex = -1;
            pendingHandChooseOneOption = null;
        }

        private void BuildQuestTrackerOverlay()
        {
            var questState = service.State.Player.Tavern.AdvancedMechanics?.Quests;
            var activeCount = 0;
            if (questState?.MainQuest != null)
            {
                activeCount += 1;
            }

            if (questState?.BonusQuest != null)
            {
                activeCount += 1;
            }

            if (activeCount == 0)
            {
                return;
            }

            var panel = Panel("UnityQuestTrackerPanel", MechanicStatusStripRoot(), UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.94f));
            ConfigureInspectorSurface(panel, UnityTavernUiStyle.ArcaneBlue, 0.34f);
            UnityTavernUiStyle.SetFixedSize(panel, 500f, QuestTrackerHeight(activeCount));

            var layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 7, 7);
            layout.spacing = 7;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityQuestTrackerTitle", panel.transform, T("任务", "Quests"), 14, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetFixedSize(title.gameObject, 64f, UnityTavernUiStyle.TouchHeight);

            var rows = Panel("UnityQuestTrackerRows", panel.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(rows, 1f, 0f);
            var rowsLayout = rows.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 4;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            if (questState?.MainQuest != null)
            {
                BuildQuestTrackerRow(rows.transform, questState.MainQuest, "Main", 0);
            }

            if (questState?.BonusQuest != null)
            {
                BuildQuestTrackerRow(rows.transform, questState.BonusQuest, "Bonus", 1);
            }
        }

        private void BuildQuestTrackerRow(Transform parent, ActiveQuestState quest, string slot, int questIndex)
        {
            var row = Panel("UnityQuestTrackerRow-" + slot, parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(row, quest.Completed ? UnityTavernUiStyle.Green : UnityTavernUiStyle.Gold, 0.18f);
            UnityTavernUiStyle.SetPreferredHeight(row, 56f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 3, 3);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var slotText = slot == "Main" ? T("主任务", "Main") : T("额外任务", "Bonus");
            var headingText = slotText + " " + quest.Progress + "/" + quest.RequiredAmount + "  " + quest.QuestName;
            var heading = UiFactory.Label("UnityQuestTrackerHeading", row.transform, headingText, 14, FontStyle.Bold);
            heading.color = quest.Completed ? UnityTavernUiStyle.Green : UnityTavernUiStyle.Text;
            heading.horizontalOverflow = HorizontalWrapMode.Wrap;
            heading.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFlexible(heading.gameObject, 1f, 0f);

            var reward = UiFactory.Label("UnityQuestTrackerReward", row.transform, quest.RewardActive ? T("生效", "Active") : T("奖励", "Reward"), 14, FontStyle.Bold);
            reward.color = UnityTavernUiStyle.MutedText;
            reward.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetFixedSize(reward.gameObject, 64f, 32f);

            var barBack = Panel("UnityQuestProgressBack", row.transform, new Color(0f, 0f, 0f, 0.28f));
            UnityTavernUiStyle.SetFixedSize(barBack, 46f, 6f);
            var fill = Panel("UnityQuestProgressFill", barBack.transform, quest.Completed ? UnityTavernUiStyle.Green : UnityTavernUiStyle.Gold);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(quest.RequiredAmount <= 0 ? 1f : (float)quest.Progress / quest.RequiredAmount), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            ActionButton(
                "UnityQuestCompleteButton-" + slot,
                row.transform,
                T("完成", "Done"),
                () => Apply(new GameCommand(GameCommandType.DebugCompleteQuest, questIndex)),
                58f,
                UnityTavernUiStyle.TouchHeight,
                false,
                UnityTavernActionButtonRole.Primary,
                !quest.Completed);
            ActionButton(
                "UnityQuestReplaceRewardButton-" + slot,
                row.transform,
                T("奖励", "Reward"),
                () => OpenQuestRewardLibrary(questIndex),
                76f,
                UnityTavernUiStyle.TouchHeight,
                false,
                UnityTavernActionButtonRole.Utility);
        }

        private void BuildTrinketTrackerOverlay()
        {
            var state = service.State.Player.Tavern.AdvancedMechanics?.Trinkets;
            var lesser = ResolveTrinketDefinition(state?.LesserTrinketId);
            var greater = ResolveTrinketDefinition(state?.GreaterTrinketId);
            if (lesser == null && greater == null)
            {
                return;
            }

            var panel = Panel("UnityTrinketTrackerPanel", MechanicStatusStripRoot(), UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.94f));
            ConfigureInspectorSurface(panel, UnityTavernUiStyle.Gold, 0.24f);
            UnityTavernUiStyle.SetFixedSize(panel, 420f, 80f);

            var layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 7;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityTrinketTrackerTitle", panel.transform, "饰品", 14, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetFixedSize(title.gameObject, 64f, UnityTavernUiStyle.TouchHeight);

            var rows = Panel("UnityTrinketTrackerRows", panel.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(rows, 1f, 0f);
            var rowsLayout = rows.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 3;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            BuildTrinketTrackerRow(rows.transform, "Lesser", TrinketSlotKind.Lesser, lesser);
            BuildTrinketTrackerRow(rows.transform, "Greater", TrinketSlotKind.Greater, greater);
        }

        private void BuildTrinketTrackerRow(Transform parent, string slotName, TrinketSlotKind slotKind, TrinketDefinition definition)
        {
            var row = Panel("UnityTrinketTrackerRow-" + slotName, parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(row, slotKind == TrinketSlotKind.Greater ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Blue, 0.16f);
            UnityTavernUiStyle.SetPreferredHeight(row, 30f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 1, 1);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var slot = UiFactory.Label("UnityTrinketSlotLabel-" + slotName, row.transform, TrinketSlotDisplayName(slotKind), 14, FontStyle.Bold);
            slot.color = UnityTavernUiStyle.Gold;
            slot.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetFixedSize(slot.gameObject, 70f, 26f);

            var name = UiFactory.Label("UnityTrinketName-" + slotName, row.transform, definition == null ? TrinketSlotScheduledText(slotKind) : definition.Name, 14, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFlexible(name.gameObject, 1f, 0f);

            var meta = UiFactory.Label("UnityTrinketMeta-" + slotName, row.transform, definition == null ? "待选" : definition.Cost + "g", 14, FontStyle.Normal);
            meta.color = UnityTavernUiStyle.MutedText;
            meta.alignment = TextAnchor.MiddleRight;
            meta.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFixedSize(meta.gameObject, 48f, 26f);
        }

        private static string TrinketSlotDisplayName(TrinketSlotKind slotKind)
        {
            return slotKind == TrinketSlotKind.Greater ? "大饰品" : "小饰品";
        }

        private static string TrinketSlotScheduledText(TrinketSlotKind slotKind)
        {
            return slotKind == TrinketSlotKind.Greater ? "第 9 回合选择" : "第 6 回合选择";
        }

        private int ActiveQuestCount()
        {
            var quests = service.State.Player.Tavern.AdvancedMechanics?.Quests;
            var count = 0;
            if (quests?.MainQuest != null)
            {
                count += 1;
            }

            if (quests?.BonusQuest != null)
            {
                count += 1;
            }

            return count;
        }

        private static float QuestTrackerHeight(int activeCount)
        {
            return 18f + activeCount * 56f;
        }

        private ActiveQuestState ActiveQuestByIndex(int questIndex)
        {
            var quests = service.State.Player.Tavern.AdvancedMechanics?.Quests;
            return questIndex == 1 ? quests?.BonusQuest : quests?.MainQuest;
        }

        private TrinketDefinition ResolveTrinketDefinition(string idOrCardId)
        {
            if (string.IsNullOrWhiteSpace(idOrCardId) || service.TrinketCatalog == null)
            {
                return null;
            }

            if (service.TrinketCatalog.TryGetByCardId(idOrCardId, out var byCardId))
            {
                return byCardId;
            }

            return service.TrinketCatalog.TryGetById(idOrCardId, out var byId) ? byId : null;
        }

        private void BuildAdvancedChoiceStatusPanel()
        {
            var statuses = service.GetAdvancedChoiceStatuses().ToList();
            if (statuses.Count == 0)
            {
                return;
            }

            var visible = statuses
                .OrderByDescending(status => status.IsCurrent)
                .ThenBy(status => status.DueRound <= 0 ? int.MaxValue : status.DueRound)
                .Take(4)
                .ToList();

            var layoutContext = LayoutContext();
            var panel = Panel("UnityAdvancedChoiceStatusPanel", MechanicStatusStripRoot(), UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.86f));
            ConfigureInspectorSurface(panel, UnityTavernUiStyle.Green, 0.12f);
            UnityTavernUiStyle.SetFixedSize(panel, layoutContext.IsCompact ? 520f : 560f, AdvancedChoiceStatusPanelHeight(visible.Count));

            var layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 5, 5);
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityAdvancedChoiceStatusTitle", panel.transform, "机制", 14, FontStyle.Bold);
            title.color = UnityTavernUiStyle.MutedText;
            title.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetFixedSize(title.gameObject, 64f, UnityTavernUiStyle.TouchHeight);

            var rows = Panel("UnityAdvancedChoiceStatusRows", panel.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(rows, 1f, 0f);
            var rowsLayout = rows.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 2;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            foreach (var status in visible)
            {
                BuildAdvancedChoiceStatusRow(rows.transform, status);
            }
        }

        private void BuildRecruitActionTargetStatus()
        {
            if (pendingRecruitActionDefinition == null)
            {
                return;
            }

            var layoutContext = LayoutContext();
            var panel = Panel(
                "UnityRecruitActionTargetPanel",
                MechanicStatusStripRoot(),
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.96f));
            ConfigureInspectorSurface(panel, UnityTavernUiStyle.Gold, 0.34f);
            UnityTavernUiStyle.SetFixedSize(
                panel,
                layoutContext.IsCompact ? Math.Min(520f, layoutContext.Width - 20f) : 560f,
                layoutContext.CanvasUnitsForPhysicalPixels(58f));
            var row = panel.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(8, 8, 5, 5);
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            var hintText = !string.IsNullOrWhiteSpace(pendingPrimaryTargetInstanceId)
                ? T("战吼来源已锁定；请选择它的效果目标。", "Battlecry source locked; choose its effect target.")
                : pendingRecruitActionDefinition.TargetSpec == RecruitActionTargetSpec.TavernMinion
                    ? T("选择酒馆中的随从作为目标；不是己方战队随从。", "Choose a Tavern minion; not a minion in your warband.")
                    : RecruitActionTargetLabel(pendingRecruitActionDefinition.TargetSpec);
            var hint = UiFactory.Label(
                "UnityRecruitActionTargetHint",
                panel.transform,
                T("目标：", "Target: ") + hintText,
                14,
                FontStyle.Bold,
                layoutContext);
            hint.color = UnityTavernUiStyle.Gold;
            hint.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetFlexible(hint.gameObject, 1f, 0f);

            UiFactory.Button(
                "UnityRecruitActionTargetCancelButton",
                panel.transform,
                T("取消", "Cancel"),
                () => CancelCurrentTargeting(),
                layoutContext);
        }

        private void ExecuteRecruitAction(RecruitActionRequest request)
        {
            var sourceInstanceId = request?.SourceInstanceId;
            ClearPendingRecruitAction();
            try
            {
                ExecuteCommand(new GameCommand(GameCommandType.UseRecruitAction, request));
                var result = service.LastRecruitActionResult;
                if (result == null || !result.Succeeded)
                {
                    lastError = result?.Message ?? T("激活失败。", "Activate failed.");
                    lastFeedback = null;
                }
                else
                {
                    lastError = null;
                    lastFeedback = string.IsNullOrWhiteSpace(result.Message)
                        ? T("激活已完成。", "Activate resolved.")
                        : result.Message;
                }

                selectedInstanceId = service.State.Player.Board.Any(item =>
                    item != null &&
                    string.Equals(item.InstanceId, sourceInstanceId, StringComparison.Ordinal))
                    ? sourceInstanceId
                    : null;
            }
            catch (Exception exception)
            {
                lastError = service.LocalizePlayerMessage(exception.Message);
                lastFeedback = null;
            }

            Rebuild();
        }

        private void EnsureRecruitActionUiStateIsLive()
        {
            if (string.IsNullOrWhiteSpace(pendingRecruitActionSourceInstanceId) &&
                pendingRecruitActionDefinition == null)
            {
                return;
            }

            var source = service.State.Player.Board.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.InstanceId, pendingRecruitActionSourceInstanceId, StringComparison.Ordinal));
            var sourceIsValid = source != null &&
                pendingRecruitActionDefinition != null &&
                RecruitActionsFor(source).Any(action =>
                    action != null &&
                    string.Equals(
                        action.ActionId,
                        pendingRecruitActionDefinition.ActionId,
                        StringComparison.Ordinal)) &&
                service.State.Phase == pendingRecruitActionDefinition.AllowedPhase;
            if (sourceIsValid)
            {
                return;
            }

            var sourceLeft = source == null;
            ClearPendingRecruitAction();
            lastError = null;
            lastFeedback = sourceLeft
                ? T("发动来源已离场，已取消目标选择。", "The Activate source left your warband; targeting was cancelled.")
                : T("发动能力或阶段已变化，已取消目标选择。", "The Activate ability or phase changed; targeting was cancelled.");
        }

        private void CancelPendingRecruitAction()
        {
            ClearPendingRecruitAction();
            lastError = null;
            lastFeedback = null;
            Rebuild();
        }

        private void ClearPendingRecruitAction()
        {
            pendingRecruitActionSourceInstanceId = null;
            pendingRecruitActionDefinition = null;
            ClearPendingPrimaryTarget();
        }

        private void BuildAdvancedChoiceStatusRow(Transform parent, AdvancedChoiceStatus status)
        {
            var safeId = SafeObjectName(status.Id);
            var baseColor = status.IsCurrent ? UnityTavernUiStyle.PanelRaised : UnityTavernUiStyle.PanelQuiet;
            var row = Panel("UnityAdvancedChoiceStatusRow-" + safeId, parent, new Color(baseColor.r, baseColor.g, baseColor.b, status.IsCurrent ? 0.74f : 0.56f));
            ConfigureInspectorSurface(row, status.IsCurrent ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Green, 0.1f);
            UnityTavernUiStyle.SetPreferredHeight(row, UnityTavernUiStyle.TouchHeight);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 1, 1);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var markerText = AdvancedChoiceStatusMarkerText(status);
            var marker = UiFactory.Label("UnityAdvancedChoiceStatusMarker-" + safeId, row.transform, markerText, 14, FontStyle.Bold);
            marker.color = status.IsCurrent ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Green;
            marker.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetFixedSize(marker.gameObject, 76f, 32f);

            var title = UiFactory.Label("UnityAdvancedChoiceStatusName-" + safeId, row.transform, AdvancedChoiceStatusTitleText(status), 14, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFixedSize(title.gameObject, 144f, 32f);

            var detail = UiFactory.Label("UnityAdvancedChoiceStatusDetail-" + safeId, row.transform, AdvancedChoiceStatusDetailText(status), 14, FontStyle.Normal);
            detail.color = status.IsBlocking ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.MutedText;
            detail.alignment = TextAnchor.MiddleLeft;
            detail.verticalOverflow = VerticalWrapMode.Truncate;
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetFlexible(detail.gameObject, 1f, 0f);

            if (status.IsCurrent)
            {
                ActionButton(
                    "UnityAdvancedChoiceStatusOpenButton-" + safeId,
                    row.transform,
                    "选择",
                    Rebuild,
                    68f,
                    UnityTavernUiStyle.TouchHeight,
                    false,
                    UnityTavernActionButtonRole.Primary);
            }
        }

        private static string AdvancedChoiceStatusMarkerText(AdvancedChoiceStatus status)
        {
            if (status == null)
            {
                return string.Empty;
            }

            if (status.IsCurrent)
            {
                return "请选择";
            }

            return status.DueRound > 0 ? "第" + status.DueRound + "回合" : "待定";
        }

        private static string AdvancedChoiceStatusTitleText(AdvancedChoiceStatus status)
        {
            if (status == null)
            {
                return string.Empty;
            }

            var title = status.Title ?? string.Empty;

            if (string.Equals(status.Id, "trinket-lesser-round-6", StringComparison.OrdinalIgnoreCase))
            {
                return "小饰品";
            }

            if (string.Equals(status.Id, "trinket-greater-round-9", StringComparison.OrdinalIgnoreCase))
            {
                return "大饰品";
            }

            if (string.Equals(status.Id, "discover-current", StringComparison.OrdinalIgnoreCase))
            {
                return "发现";
            }

            if (title.IndexOf("Lesser Trinket", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return status.IsCurrent ? "请选择小饰品" : "小饰品";
            }

            if (title.IndexOf("Greater Trinket", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return status.IsCurrent ? "请选择大饰品" : "大饰品";
            }

            if (title.IndexOf("Bonus Quest", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return status.IsCurrent ? "请选择额外任务" : "额外任务";
            }

            if (title.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return status.IsCurrent ? "请选择任务" : "任务";
            }

            if (title.IndexOf("Anomaly", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return status.IsCurrent ? "请选择畸变" : "畸变";
            }

            if (title.IndexOf("Flightpath", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "请选择飞行路线";
            }

            if (title.IndexOf("Hero", StringComparison.OrdinalIgnoreCase) >= 0 ||
                title.IndexOf("Conviction", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "请选择英雄机制";
            }

            return status.Title;
        }

        private static string AdvancedChoiceStatusDetailText(AdvancedChoiceStatus status)
        {
            if (status == null)
            {
                return string.Empty;
            }

            if (string.Equals(status.Id, "trinket-lesser-round-6", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status.Id, "trinket-greater-round-9", StringComparison.OrdinalIgnoreCase))
            {
                return "未到回合";
            }

            if (string.Equals(status.Id, "discover-current", StringComparison.OrdinalIgnoreCase))
            {
                return "请先完成发现选择";
            }

            if (status.IsCurrent)
            {
                var optionCount = OptionCount(status.Detail);
                return optionCount > 0 ? optionCount + " 个选项，必须选择" : "必须选择";
            }

            if (status.DueRound > 0)
            {
                return "第 " + status.DueRound + " 回合开放";
            }

            return status.Detail;
        }

        private static int OptionCount(string detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
            {
                return 0;
            }

            var match = OptionCountPattern.Match(detail);
            return match.Success && int.TryParse(match.Groups[1].Value, out var count) ? count : 0;
        }

        private Transform MechanicStatusStripRoot()
        {
            var existing = transform.Find("UnityMechanicStatusStrip");
            if (existing != null)
            {
                return existing;
            }

            var strip = Panel("UnityMechanicStatusStrip", transform, Color.clear);
            var rect = strip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(18f, -304f);
            rect.offsetMax = new Vector2(-18f, -84f);

            var layout = strip.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return strip.transform;
        }

        private static float AdvancedChoiceStatusPanelHeight(int visibleCount)
        {
            return 12f + Mathf.Min(visibleCount, 4) * 50f;
        }

        private void BuildAdvancedMechanicChoiceModal()
        {
            var request = service.GetActiveMechanicChoice();
            if (request == null)
            {
                return;
            }

            if (request.Kind == AdvancedMechanicKind.DarkGift)
            {
                BuildDarkGiftChoiceModal(request);
                return;
            }

            var overlay = Panel("UnityAdvancedMechanicChoiceOverlay", transform, new Color(0f, 0f, 0f, 0.68f));
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().raycastTarget = true;
            overlay.transform.SetAsLastSibling();

            var layoutContext = LayoutContext();
            var panel = Panel("UnityAdvancedMechanicChoicePanel", overlay.transform, UnityTavernUiStyle.SurfaceDark);
            ConfigureInspectorSurface(panel, request.Kind == AdvancedMechanicKind.Quest ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.Gold, 0.32f);
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityAdvancedMechanicChoiceStarLantern", request.Kind == AdvancedMechanicKind.Quest ? UnityTavernUiStyle.ArcaneBlue : UnityTavernUiStyle.Gold);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = AdvancedMechanicChoicePanelSize(layoutContext);
            panelRect.anchoredPosition = Vector2.zero;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(22, 22, 18, 22);
            panelLayout.spacing = 14;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var header = Panel("UnityAdvancedMechanicChoiceHeader", panel.transform, UnityTavernUiStyle.SurfaceRaised);
            UnityTavernUiStyle.ConfigureOutline(header, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.28f), new Vector2(1f, -1f));
            UnityTavernUiStyle.SetPreferredHeight(header, 56f);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 10;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            var title = UiFactory.Label("UnityAdvancedMechanicChoiceTitle", header.transform, AdvancedMechanicChoiceTitle(request), 20, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

            if (CanOpenPlayerDirectedChoice(request))
            {
                ActionButton(
                    "UnityPlayerDirectedChoiceButton-" + request.Kind,
                    header.transform,
                    T("自由选择", "Free Choice"),
                    () => OpenPlayerDirectedChoice(request),
                    92f,
                    UnityTavernUiStyle.TouchHeight,
                    false,
                    UnityTavernActionButtonRole.Utility);
            }

            var options = Panel("UnityAdvancedMechanicChoiceOptions", panel.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(options, 1f, 1f);
            var row = options.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12;
            row.childAlignment = TextAnchor.UpperCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            for (var index = 0; index < request.Options.Count; index += 1)
            {
                BuildAdvancedMechanicChoiceCard(options.transform, request, request.Options[index], index, layoutContext.IsCompact);
            }
        }

        private void BuildDarkGiftChoiceModal(MechanicChoiceRequest request)
        {
            var queued = service.State.ChoiceQueue?.ActiveChoice;
            var requestId = queued?.RequestId ?? request?.RequestId ?? string.Empty;
            if (!string.Equals(darkGiftChoiceRequestId, requestId, StringComparison.Ordinal))
            {
                darkGiftChoiceRequestId = requestId;
                selectedDarkGiftChoiceIndex = -1;
                darkGiftChoicePageIndex = 0;
                darkGiftBlockingExplanationVisible = false;
            }

            var optionCount = request?.Options?.Count ?? 0;
            darkGiftChoicePageIndex = optionCount == 0
                ? 0
                : Mathf.Clamp(darkGiftChoicePageIndex, 0, optionCount - 1);
            UnityTavernChoiceQueueModalComponent.Create(transform).Build(
                request,
                queued,
                LayoutContext(),
                service.UseEnglish,
                selectedDarkGiftChoiceIndex,
                darkGiftChoicePageIndex,
                darkGiftBlockingExplanationVisible,
                SelectDarkGiftChoice,
                ChangeDarkGiftChoicePage,
                ConfirmDarkGiftChoice,
                ExplainBlockingDarkGiftChoice);
        }

        private void SelectDarkGiftChoice(int index)
        {
            var request = service.GetActiveMechanicChoice();
            if (request?.Kind != AdvancedMechanicKind.DarkGift ||
                index < 0 ||
                index >= request.Options.Count)
            {
                return;
            }

            selectedDarkGiftChoiceIndex = index;
            darkGiftChoicePageIndex = index;
            darkGiftBlockingExplanationVisible = false;
            Rebuild();
        }

        private void ChangeDarkGiftChoicePage(int index)
        {
            var count = service.GetActiveMechanicChoice()?.Options?.Count ?? 0;
            darkGiftChoicePageIndex = count == 0 ? 0 : Mathf.Clamp(index, 0, count - 1);
            Rebuild();
        }

        private void ConfirmDarkGiftChoice()
        {
            var request = service.GetActiveMechanicChoice();
            if (request?.Kind != AdvancedMechanicKind.DarkGift ||
                selectedDarkGiftChoiceIndex < 0 ||
                selectedDarkGiftChoiceIndex >= request.Options.Count)
            {
                return;
            }

            var selected = selectedDarkGiftChoiceIndex;
            selectedDarkGiftChoiceIndex = -1;
            darkGiftChoicePageIndex = 0;
            darkGiftBlockingExplanationVisible = false;
            Apply(new GameCommand(GameCommandType.ChooseMechanicOption, selected));
        }

        private void ExplainBlockingDarkGiftChoice()
        {
            darkGiftBlockingExplanationVisible = true;
            Rebuild();
        }

        private static Vector2 AdvancedMechanicChoicePanelSize(UnityTavernLayoutContext layoutContext)
        {
            var targetWidth = layoutContext.IsCompact ? 720f : 1080f;
            var targetHeight = layoutContext.IsCompact ? 540f : 620f;
            var minimumWidth = layoutContext.IsCompact ? 680f : 980f;
            var minimumHeight = layoutContext.IsCompact ? 500f : 560f;
            var width = Mathf.Clamp(layoutContext.Width - 96f, minimumWidth, targetWidth);
            var height = Mathf.Clamp(layoutContext.Height - 96f, minimumHeight, targetHeight);
            return new Vector2(width, height);
        }

        private void BuildAdvancedMechanicChoiceCard(Transform parent, MechanicChoiceRequest request, MechanicChoiceOption option, int index, bool compact)
        {
            var card = Panel("UnityAdvancedMechanicChoiceCard-" + index, parent, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(card, request.Kind == AdvancedMechanicKind.Quest ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.Gold, 0.22f);
            var cardWidth = request.Kind == AdvancedMechanicKind.Quest
                ? (compact ? 250f : 292f)
                : (compact ? 190f : 224f);
            var cardHeight = compact ? 366f : 430f;
            UnityTavernUiStyle.SetFixedSize(card, cardWidth, cardHeight);

            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 7;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            if (request.Kind == AdvancedMechanicKind.Quest)
            {
                BuildQuestChoiceImages(card.transform, option, compact);
            }
            else
            {
                BuildMechanicChoiceImage(
                    card.transform,
                    option.ImagePath,
                    option.SourceId,
                    option.DisplayName,
                    CardKind.Trinket,
                    compact ? 128f : 156f,
                    compact ? 184f : 222f);
            }

            var name = UiFactory.Label("UnityAdvancedMechanicChoiceName", card.transform, option.DisplayName, 14, FontStyle.Bold);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = UnityTavernUiStyle.Text;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, compact ? 36f : 44f);

            var text = UiFactory.Label("UnityAdvancedMechanicChoiceText", card.transform, CleanCardText(option.Text), 11, FontStyle.Normal);
            text.color = UnityTavernUiStyle.MutedText;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, request.Kind == AdvancedMechanicKind.Quest ? (compact ? 42f : 54f) : (compact ? 48f : 58f));

            if (request.Kind == AdvancedMechanicKind.Quest)
            {
                var reward = UiFactory.Label("UnityAdvancedMechanicChoiceReward", card.transform, option.RewardName + "\n" + CleanCardText(option.RewardText), 10, FontStyle.Bold);
                reward.color = UnityTavernUiStyle.Gold;
                reward.alignment = TextAnchor.UpperCenter;
                reward.horizontalOverflow = HorizontalWrapMode.Wrap;
                reward.verticalOverflow = VerticalWrapMode.Truncate;
                UnityTavernUiStyle.SetPreferredHeight(reward.gameObject, compact ? 50f : 60f);
            }

            var label = request.Kind == AdvancedMechanicKind.Trinket && option.Cost > 0
                ? T("选择（" + option.Cost + "铸币）", "Choose (" + option.Cost + ")")
                : T("选择", "Choose");
            var canAfford = request.Kind != AdvancedMechanicKind.Trinket ||
                option.Cost <= service.State.Player.Tavern.Gold;
            ActionButton(
                "UnityAdvancedMechanicChoiceButton-" + index,
                card.transform,
                label,
                () => Apply(new GameCommand(GameCommandType.ChooseMechanicOption, index)),
                0f,
                36f,
                role: UnityTavernActionButtonRole.Primary,
                interactable: canAfford);
        }

        private void BuildQuestChoiceImages(Transform parent, MechanicChoiceOption option, bool compact)
        {
            var row = Panel("UnityQuestChoiceImageRow", parent, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, compact ? 132f : 162f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            BuildMechanicChoiceImage(row.transform, option.ImagePath, option.SourceId, option.DisplayName, CardKind.Quest, compact ? 88f : 108f, compact ? 124f : 152f);
            BuildMechanicChoiceImage(row.transform, option.RewardImagePath, option.RewardId, option.RewardName, CardKind.QuestReward, compact ? 88f : 108f, compact ? 124f : 152f);
        }

        private static void BuildMechanicChoiceImage(Transform parent, string imagePath, string cardId, string displayName, CardKind kind, float width, float height)
        {
            var frame = Panel("UnityMechanicChoiceImageFrame", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(frame, kind == CardKind.QuestReward ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Blue, 0.18f);
            UnityTavernUiStyle.SetFixedSize(frame, width, height);

            var image = new GameObject("UnityMechanicChoiceImage", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(frame.transform, false);
            UnityTavernUiStyle.Stretch(image.GetComponent<RectTransform>());
            var imageComponent = image.GetComponent<Image>();
            imageComponent.sprite = CardImageProvider.LoadSprite(imagePath, cardId, kind);
            imageComponent.preserveAspect = true;
            imageComponent.color = imageComponent.sprite == null ? UnityTavernUiStyle.PanelRaised : Color.white;
            imageComponent.raycastTarget = false;
            if (imageComponent.sprite == null)
            {
                var fallback = UiFactory.Label(
                    "UnityMechanicChoiceImageFallbackText",
                    frame.transform,
                    UnityTavernUiStyle.ArtFallbackText(displayName, kind == CardKind.QuestReward ? "R" : "Q"),
                    24,
                    FontStyle.Bold);
                fallback.alignment = TextAnchor.MiddleCenter;
                fallback.color = UnityTavernUiStyle.Text;
                fallback.raycastTarget = false;
                UnityTavernUiStyle.ConfigureOutline(fallback.gameObject, new Color(0f, 0f, 0f, 0.78f), new Vector2(1f, -1f));
                UnityTavernUiStyle.Stretch(fallback.rectTransform);
            }
        }

        private string AdvancedMechanicChoiceTitle(MechanicChoiceRequest request)
        {
            if (request.Kind == AdvancedMechanicKind.Quest)
            {
                return string.Equals(request.Slot, "Bonus", StringComparison.OrdinalIgnoreCase)
                    ? T("请选择额外任务和奖励", "Choose a Bonus Quest and Reward")
                    : T("请选择任务和奖励", "Choose a Quest and Reward");
            }

            if (request.Kind == AdvancedMechanicKind.Anomaly)
            {
                return T("请选择畸变", "Choose an Anomaly");
            }

            if (request.Kind == AdvancedMechanicKind.Distortion)
            {
                return T("请选择英雄机制", "Choose a Hero Mechanic");
            }

            return string.Equals(request.Slot, "Greater", StringComparison.OrdinalIgnoreCase)
                ? T("请选择大饰品", "Choose a Greater Trinket")
                : T("请选择小饰品", "Choose a Lesser Trinket");
        }

        private bool CanOpenPlayerDirectedChoice(MechanicChoiceRequest request)
        {
            return service.PlayerDirectedChoicesEnabled &&
                   request != null &&
                   (request.Kind == AdvancedMechanicKind.Quest || request.Kind == AdvancedMechanicKind.Trinket);
        }

        private void OpenPlayerDirectedChoice(MechanicChoiceRequest request)
        {
            if (!CanOpenPlayerDirectedChoice(request))
            {
                return;
            }

            ResetPlayerDirectedChoiceFilters();
            playerDirectedChoiceKind = request.Kind == AdvancedMechanicKind.Quest
                ? PlayerDirectedChoiceKind.QuestPair
                : PlayerDirectedChoiceKind.Trinket;
            playerDirectedTrinketSlotKind = ParseUiTrinketSlotKind(request.Slot);
            playerDirectedChoiceOpen = true;
            Rebuild();
        }

        private void OpenSecondHeroPowerDirectedChoice()
        {
            if (!service.HasPlayerDirectedSecondHeroPowerChoice())
            {
                return;
            }

            ResetPlayerDirectedChoiceFilters();
            playerDirectedChoiceKind = PlayerDirectedChoiceKind.SecondHeroPower;
            playerDirectedChoiceOpen = true;
            Rebuild();
        }

        private void ResetPlayerDirectedChoiceFilters()
        {
            playerDirectedSearchText = string.Empty;
            playerDirectedSelectableFilter = 0;
            playerDirectedCostFilter = 0;
            playerDirectedSlotFilter = string.Empty;
            playerDirectedTagFilter = string.Empty;
        }

        private void BuildPlayerDirectedChoiceModal()
        {
            var allOptions = PlayerDirectedChoiceOptions();
            var visible = allOptions
                .Where(PlayerDirectedChoiceMatchesFilters)
                .Where(PlayerDirectedChoiceMatchesSearch)
                .Take(160)
                .ToList();

            var overlay = Panel("UnityPlayerDirectedChoiceOverlay", transform, new Color(0f, 0f, 0f, 0.68f));
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().raycastTarget = true;
            overlay.transform.SetAsLastSibling();

            var layoutContext = LayoutContext();
            var panel = Panel("UnityPlayerDirectedChoicePanel", overlay.transform, UnityTavernUiStyle.SurfaceDark);
            ConfigureInspectorSurface(panel, playerDirectedChoiceKind == PlayerDirectedChoiceKind.Trinket ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Blue, 0.32f);
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityPlayerDirectedChoiceStarLantern", playerDirectedChoiceKind == PlayerDirectedChoiceKind.Trinket ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.ArcaneBlue);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(layoutContext.IsCompact ? 650f : 900f, layoutContext.IsCompact ? 520f : 610f);
            rect.anchoredPosition = Vector2.zero;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(16, 16, 14, 16);
            panelLayout.spacing = 10;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            BuildPlayerDirectedChoiceHeader(panel.transform, visible.Count, allOptions.Count, allOptions);

            var list = UiFactory.ScrollView("UnityPlayerDirectedChoiceScroll", panel.transform, UnityTavernUiStyle.SurfaceRaised, out _);
            UnityTavernUiStyle.SetFlexible(list.gameObject, 1f, 1f);
            var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(10, 10, 10, 10);
            listLayout.spacing = 8;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            if (visible.Count == 0)
            {
                var empty = UiFactory.Label(
                    "UnityPlayerDirectedChoiceEmpty",
                    list,
                    service.UseEnglish ? "No selectable options under the current filters." : "当前筛选条件下没有可选项。",
                    14,
                    FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 74f);
                return;
            }

            for (var index = 0; index < visible.Count; index += 1)
            {
                BuildPlayerDirectedChoiceRow(list, visible[index], index);
            }
        }

        private void BuildPlayerDirectedChoiceHeader(
            Transform parent,
            int visibleCount,
            int totalCount,
            IReadOnlyList<PlayerDirectedChoiceOption> allOptions)
        {
            var header = Panel("UnityPlayerDirectedChoiceHeader", parent, UnityTavernUiStyle.SurfaceRaised);
            ConfigureInspectorSurface(header, playerDirectedChoiceKind == PlayerDirectedChoiceKind.Trinket ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Blue, 0.22f);
            UnityTavernUiStyle.SetPreferredHeight(header, 260f);
            var layout = header.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var top = Panel("UnityPlayerDirectedChoiceHeaderTop", header.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(top, 56f);
            var topLayout = top.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 8;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = true;

            var title = UiFactory.Label("UnityPlayerDirectedChoiceTitle", top.transform, PlayerDirectedChoiceTitle(), 18, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

            var count = UiFactory.Label("UnityPlayerDirectedChoiceCount", top.transform, visibleCount + " / " + totalCount, 14, FontStyle.Bold);
            count.color = UnityTavernUiStyle.MutedText;
            count.alignment = TextAnchor.MiddleRight;
            UnityTavernUiStyle.SetFixedSize(count.gameObject, 90f, UnityTavernUiStyle.TouchHeight);

            var close = ActionButton(
                "UnityPlayerDirectedChoiceCloseButton",
                top.transform,
                service.UseEnglish ? "Close" : "关闭",
                ClosePlayerDirectedChoice,
                76f,
                UnityTavernUiStyle.TouchHeight,
                false,
                UnityTavernActionButtonRole.Neutral);
            close.GetComponentInChildren<Text>(true).fontSize = 14;

            var searchObject = new GameObject("UnityPlayerDirectedChoiceSearchInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            searchObject.transform.SetParent(header.transform, false);
            UnityTavernUiStyle.SetPreferredHeight(searchObject, UnityTavernUiStyle.TouchHeight);
            var input = searchObject.GetComponent<InputField>();
            UnityTavernUiStyle.ConfigureInputField(input, UnityTavernUiStyle.ArcaneBlue);
            input.textComponent = UiFactory.Label("UnityPlayerDirectedChoiceSearchText", searchObject.transform, string.Empty, 14);
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.Stretch(input.textComponent.rectTransform);
            input.textComponent.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.textComponent.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.placeholder = UiFactory.Label(
                "UnityPlayerDirectedChoiceSearchPlaceholder",
                searchObject.transform,
                service.UseEnglish ? "Search name or CardId" : "搜索名称或 CardId",
                14);
            input.placeholder.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.Stretch(input.placeholder.rectTransform);
            input.placeholder.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.placeholder.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.text = playerDirectedSearchText;
            input.onEndEdit.AddListener(value =>
            {
                playerDirectedSearchText = value ?? string.Empty;
                rebuildQueued = true;
            });

            BuildPlayerDirectedChoiceFilters(header.transform, allOptions);
            if (UnityEngine.Application.isPlaying && parent.parent != null)
            {
                StartCoroutine(ActivatePlayerDirectedFocusTrapNextFrame(parent.parent.gameObject, searchObject));
            }
        }

        private IEnumerator ActivatePlayerDirectedFocusTrapNextFrame(GameObject overlay, GameObject searchObject)
        {
            yield return null;
            if (playerDirectedChoiceOpen &&
                overlay != null &&
                overlay.activeInHierarchy &&
                searchObject != null &&
                searchObject.activeInHierarchy)
            {
                UnityTavernUiStyle.EnsureComponent<UnityFocusTrap>(overlay).Activate(searchObject);
            }
        }

        private void BuildPlayerDirectedChoiceFilters(Transform parent, IReadOnlyList<PlayerDirectedChoiceOption> allOptions)
        {
            var filters = Panel("UnityPlayerDirectedChoiceFilters", parent, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(filters, 118f);
            var layout = filters.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var row = Panel("UnityPlayerDirectedChoiceFilterRow", filters.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, 56f);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 6;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterStatusAll", row.transform, T("全部", "All"), playerDirectedSelectableFilter == 0, 54f, () =>
            {
                playerDirectedSelectableFilter = 0;
                Rebuild();
            });
            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterStatusSelectable", row.transform, T("可选", "Available"), playerDirectedSelectableFilter == 1, 54f, () =>
            {
                playerDirectedSelectableFilter = 1;
                Rebuild();
            });
            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterStatusBlocked", row.transform, T("不可选", "Blocked"), playerDirectedSelectableFilter == 2, 64f, () =>
            {
                playerDirectedSelectableFilter = 2;
                Rebuild();
            });

            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterCostAll", row.transform, T("费用", "Cost"), playerDirectedCostFilter == 0, 54f, () =>
            {
                playerDirectedCostFilter = 0;
                Rebuild();
            });
            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterCostFree", row.transform, "0", playerDirectedCostFilter == 1, 42f, () =>
            {
                playerDirectedCostFilter = 1;
                Rebuild();
            });
            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterCostLow", row.transform, "1-3", playerDirectedCostFilter == 2, 48f, () =>
            {
                playerDirectedCostFilter = 2;
                Rebuild();
            });
            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterCostHigh", row.transform, "4+", playerDirectedCostFilter == 3, 48f, () =>
            {
                playerDirectedCostFilter = 3;
                Rebuild();
            });

            var slots = allOptions == null
                ? new List<string>()
                : allOptions
                    .Select(option => option?.Slot)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value)
                    .ToList();
            if (slots.Count > 0)
            {
                PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterSlotAll", row.transform, T("槽位", "Slot"), string.IsNullOrEmpty(playerDirectedSlotFilter), 54f, () =>
                {
                    playerDirectedSlotFilter = string.Empty;
                    Rebuild();
                });

                foreach (var slot in slots.Take(3))
                {
                    var capturedSlot = slot;
                    PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterSlot" + SafeObjectName(slot), row.transform, BattlegroundsLocalizedText.Slot(slot, service.UseEnglish), string.Equals(playerDirectedSlotFilter, slot, StringComparison.OrdinalIgnoreCase), 68f, () =>
                    {
                        playerDirectedSlotFilter = capturedSlot;
                        Rebuild();
                    });
                }
            }

            var tagRow = Panel("UnityPlayerDirectedChoiceTagFilterRow", filters.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(tagRow, 56f);
            var tagLayout = tagRow.AddComponent<HorizontalLayoutGroup>();
            tagLayout.spacing = 6;
            tagLayout.childControlWidth = false;
            tagLayout.childControlHeight = true;
            tagLayout.childForceExpandWidth = false;
            tagLayout.childForceExpandHeight = true;

            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterTagAll", tagRow.transform, T("全部标签", "All Tags"), string.IsNullOrEmpty(playerDirectedTagFilter), 78f, () =>
            {
                playerDirectedTagFilter = string.Empty;
                Rebuild();
            });

            var tags = PlayerDirectedVisibleFilterTags(allOptions);
            foreach (var tag in tags.Take(7))
            {
                var capturedTag = tag;
                PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterTag" + SafeObjectName(tag), tagRow.transform, PlayerDirectedFilterTagLabel(tag), string.Equals(playerDirectedTagFilter, tag, StringComparison.OrdinalIgnoreCase), 92f, () =>
                {
                    playerDirectedTagFilter = capturedTag;
                    Rebuild();
                });
            }
        }

        private Button PlayerDirectedFilterButton(string name, Transform parent, string text, bool active, float width, Action onClick)
        {
            var button = LibraryFilterButton(name, parent, text, active, width, onClick);
            UnityTavernUiStyle.SetFixedSize(button.gameObject, width, UnityTavernUiStyle.TouchHeight);
            button.GetComponentInChildren<Text>(true).fontSize = 14;
            return button;
        }

        private void BuildPlayerDirectedChoiceRow(Transform parent, PlayerDirectedChoiceOption option, int index)
        {
            var row = Panel("UnityPlayerDirectedChoiceOption-" + index + "-" + SafeObjectName(option.CardId + "-" + option.SecondaryCardId), parent, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(row, option.IsSelectable ? UnityTavernUiStyle.Green : UnityTavernUiStyle.Red, 0.16f);
            UnityTavernUiStyle.SetPreferredHeight(row, 100f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            BuildMechanicChoiceImage(row.transform, option.ImagePath, option.CardId, option.DisplayName, PlayerDirectedCardKind(option), 52f, 72f);

            var details = Panel("UnityPlayerDirectedChoiceDetails", row.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(details, 1f, 0f);
            var detailsLayout = details.AddComponent<VerticalLayoutGroup>();
            detailsLayout.spacing = 3;
            detailsLayout.childControlWidth = true;
            detailsLayout.childControlHeight = true;
            detailsLayout.childForceExpandWidth = true;
            detailsLayout.childForceExpandHeight = false;

            var nameText = option.DisplayName + (string.IsNullOrWhiteSpace(option.SecondaryDisplayName) ? string.Empty : " + " + option.SecondaryDisplayName);
            var name = UiFactory.Label("UnityPlayerDirectedChoiceName", details.transform, nameText, 14, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 22f);

            var metaText = option.CardId + (string.IsNullOrWhiteSpace(option.SecondaryCardId) ? string.Empty : " / " + option.SecondaryCardId);
            var meta = UiFactory.Label("UnityPlayerDirectedChoiceMeta", details.transform, metaText + "  " + option.Status, 14, FontStyle.Normal);
            meta.color = UnityTavernUiStyle.MutedText;
            meta.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(meta.gameObject, 20f);

            var text = UiFactory.Label(
                "UnityPlayerDirectedChoiceText",
                details.transform,
                string.IsNullOrWhiteSpace(option.DisabledReason) ? CleanCardText(option.Text) : option.DisabledReason,
                14,
                option.IsSelectable ? FontStyle.Normal : FontStyle.Bold);
            text.color = option.IsSelectable ? UnityTavernUiStyle.MutedText : UnityTavernUiStyle.Red;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, 40f);

            var buttonName = index == 0 ? "UnityPlayerDirectedChoiceSelectButton" : "UnityPlayerDirectedChoiceSelectButton-" + SafeObjectName(option.CardId + "-" + option.SecondaryCardId);
            var select = ActionButton(
                buttonName,
                row.transform,
                option.IsSelectable
                    ? (service.UseEnglish ? "Choose" : "选择")
                    : (service.UseEnglish ? "Unavailable" : "不可选择"),
                () => ApplyPlayerDirectedChoice(option),
                92f,
                44f,
                false,
                option.IsSelectable ? UnityTavernActionButtonRole.Primary : UnityTavernActionButtonRole.Danger,
                option.IsSelectable);
            select.GetComponentInChildren<Text>(true).fontSize = 14;
        }

        private List<PlayerDirectedChoiceOption> PlayerDirectedChoiceOptions()
        {
            switch (playerDirectedChoiceKind)
            {
                case PlayerDirectedChoiceKind.Trinket:
                    return service.GetPlayerSelectableTrinkets(playerDirectedTrinketSlotKind).ToList();
                case PlayerDirectedChoiceKind.SecondHeroPower:
                    return service.GetPlayerSelectableSecondHeroPowers().ToList();
                default:
                    var request = service.GetActiveMechanicChoice();
                    return service.GetPlayerSelectableQuestPairs(new PlayerDirectedChoiceContext
                    {
                        Kind = PlayerDirectedChoiceKind.QuestPair,
                        Source = request?.Source,
                        Slot = request?.Slot,
                        Round = service.State.Round
                    }).ToList();
            }
        }

        private bool PlayerDirectedChoiceMatchesSearch(PlayerDirectedChoiceOption option)
        {
            if (option == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(playerDirectedSearchText))
            {
                return true;
            }

            var query = playerDirectedSearchText.Trim();
            return ContainsIgnoreCase(option.DisplayName, query) ||
                   ContainsIgnoreCase(option.SecondaryDisplayName, query) ||
                   ContainsIgnoreCase(option.CardId, query) ||
                   ContainsIgnoreCase(option.SecondaryCardId, query) ||
                   ContainsIgnoreCase(option.Type, query) ||
                   ContainsIgnoreCase(option.Status, query) ||
                   ContainsIgnoreCase(option.Slot, query) ||
                   ContainsIgnoreCase(option.PowerLevel, query) ||
                   ContainsIgnoreCase(option.Timing, query) ||
                   ContainsIgnoreCase(option.Text, query);
        }

        private bool PlayerDirectedChoiceMatchesFilters(PlayerDirectedChoiceOption option)
        {
            if (option == null)
            {
                return false;
            }

            if (playerDirectedSelectableFilter == 1 && !option.IsSelectable)
            {
                return false;
            }

            if (playerDirectedSelectableFilter == 2 && option.IsSelectable)
            {
                return false;
            }

            switch (playerDirectedCostFilter)
            {
                case 1:
                    if (option.Cost != 0)
                    {
                        return false;
                    }

                    break;
                case 2:
                    if (option.Cost < 1 || option.Cost > 3)
                    {
                        return false;
                    }

                    break;
                case 3:
                    if (option.Cost < 4)
                    {
                        return false;
                    }

                    break;
            }

            if (!string.IsNullOrWhiteSpace(playerDirectedSlotFilter) &&
                !string.Equals(option.Slot, playerDirectedSlotFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(playerDirectedTagFilter))
            {
                return option.FilterTags != null &&
                       option.FilterTags.Any(tag => string.Equals(tag, playerDirectedTagFilter, StringComparison.OrdinalIgnoreCase));
            }

            return true;
        }

        private List<string> PlayerDirectedVisibleFilterTags(IReadOnlyList<PlayerDirectedChoiceOption> options)
        {
            if (options == null)
            {
                return new List<string>();
            }

            return options
                .Where(option => option?.FilterTags != null)
                .SelectMany(option => option.FilterTags)
                .Where(PlayerDirectedFilterTagIsUseful)
                .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => PlayerDirectedFilterTagLabel(group.Key))
                .Select(group => group.Key)
                .ToList();
        }

        private static bool PlayerDirectedFilterTagIsUseful(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            if (Enum.TryParse(tag, true, out Tribe tribe) && tribe != Tribe.All)
            {
                return true;
            }

            return tag.StartsWith("power:", StringComparison.OrdinalIgnoreCase) ||
                   tag.StartsWith("timing:", StringComparison.OrdinalIgnoreCase) ||
                   tag.StartsWith("category:", StringComparison.OrdinalIgnoreCase) ||
                   tag.StartsWith("race:", StringComparison.OrdinalIgnoreCase) ||
                   tag.StartsWith("requires:", StringComparison.OrdinalIgnoreCase) ||
                   tag.IndexOf("trigger", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   tag.IndexOf("combat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   tag.IndexOf("economy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   tag.IndexOf("discover", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   tag.IndexOf("buff", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string PlayerDirectedFilterTagLabel(string tag)
        {
            return BattlegroundsLocalizedText.FilterTag(tag, service.UseEnglish);
        }

        private void ApplyPlayerDirectedChoice(PlayerDirectedChoiceOption option)
        {
            if (option == null || !option.IsSelectable)
            {
                return;
            }

            playerDirectedChoiceOpen = false;
            switch (option.Kind)
            {
                case PlayerDirectedChoiceKind.Trinket:
                    Apply(new GameCommand(
                        GameCommandType.ChoosePlayerDirectedTrinket,
                        option.CardId,
                        CardKind.Trinket,
                        string.Equals(option.Slot, "Greater", StringComparison.OrdinalIgnoreCase) ? 1 : 0));
                    break;
                case PlayerDirectedChoiceKind.SecondHeroPower:
                    Apply(new GameCommand(GameCommandType.ChoosePlayerDirectedSecondHeroPower, option.CardId, CardKind.HeroPower));
                    break;
                default:
                    Apply(new GameCommand(
                        GameCommandType.ChoosePlayerDirectedQuestPair,
                        option.CardId,
                        option.SecondaryCardId,
                        CardKind.Quest,
                        string.Equals(option.Slot, "Bonus", StringComparison.OrdinalIgnoreCase) ? 1 : 0));
                    break;
            }
        }

        private void ClosePlayerDirectedChoice()
        {
            playerDirectedChoiceOpen = false;
            playerDirectedSearchText = string.Empty;
            Rebuild();
        }

        private string PlayerDirectedChoiceTitle()
        {
            switch (playerDirectedChoiceKind)
            {
                case PlayerDirectedChoiceKind.Trinket:
                    return service.UseEnglish
                        ? "Free Choice " + playerDirectedTrinketSlotKind + " Trinket"
                        : "自由选择" + BattlegroundsLocalizedText.TrinketSlot(playerDirectedTrinketSlotKind, false);
                case PlayerDirectedChoiceKind.SecondHeroPower:
                    return T("自由选择第二英雄技能", "Free Choice Second Hero Power");
                default:
                    return T("自由选择任务与奖励", "Free Choice Quest + Reward");
            }
        }

        private static CardKind PlayerDirectedCardKind(PlayerDirectedChoiceOption option)
        {
            switch (option.Kind)
            {
                case PlayerDirectedChoiceKind.Trinket:
                    return CardKind.Trinket;
                case PlayerDirectedChoiceKind.SecondHeroPower:
                    return CardKind.HeroPower;
                default:
                    return CardKind.Quest;
            }
        }

        private static TrinketSlotKind ParseUiTrinketSlotKind(string slot)
        {
            return string.Equals(slot, "Greater", StringComparison.OrdinalIgnoreCase)
                ? TrinketSlotKind.Greater
                : TrinketSlotKind.Lesser;
        }

        private static bool ContainsIgnoreCase(string source, string query)
        {
            return !string.IsNullOrEmpty(source) &&
                   !string.IsNullOrEmpty(query) &&
                   source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string CleanCardText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var cleaned = value.Replace("[x]", string.Empty).Replace("\n", " ");
            cleaned = RichTextTagPattern.Replace(cleaned, string.Empty);
            return Regex.Replace(cleaned, "\\s+", " ").Trim();
        }

        private void BuildDiscoverOptions(Transform parent)
        {
            var options = service.State.Player.Tavern.Discover.Options;
            if (service.HasPlayerDirectedSecondHeroPowerChoice())
            {
                ActionButton(
                    "UnityPlayerDirectedChoiceButton-SecondHeroPower",
                    parent,
                    "自由选择",
                    OpenSecondHeroPowerDirectedChoice,
                    0f,
                    38f,
                    true,
                    UnityTavernActionButtonRole.Utility);
            }

            for (var index = 0; index < options.Count; index += 1)
            {
                var optionIndex = index;
                var cardObject = UnityTavernCardComponent.CreateCardHost(UnityTavernCardMode.Detail, parent, "UnityDiscoverCard-" + optionIndex);
                cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                    options[index],
                    UnityTavernCardMode.Detail,
                    T("选择", "Choose"),
                    SelectCard,
                    card => BeginDiscoverOptionSelection(optionIndex),
                    useEnglish: service.UseEnglish);
                ConfigureCardFeedback(cardObject, options[index]);
                if (!service.DiscoverOptionRequiresPlayerTarget(optionIndex))
                {
                    AddDrag(cardObject, options[index], UnityTavernDragSource.Discover, optionIndex);
                }
            }
        }

        private void BeginDiscoverOptionSelection(int optionIndex)
        {
            if (!service.DiscoverOptionRequiresPlayerTarget(optionIndex))
            {
                Apply(new GameCommand(GameCommandType.ChooseDiscover, optionIndex));
                return;
            }

            pendingDiscoverTargetOptionIndex = optionIndex;
            pendingDiscoverTargetSourceInstanceId = service.DiscoverOptionTargetSourceInstanceId(optionIndex);
            pendingDiscoverTargetOptionCard = service.State.Player.Tavern.Discover.Options[optionIndex];
            lastError = null;
            lastFeedback = T("抉择已选定，请指向一个友方野兽。", "Choice selected. Target a friendly Beast.");
            Rebuild();
        }

        private void BuildErrorToast(string message)
        {
            var toast = UnityTavernToastComponent.CreateToastHost(transform, "UnityErrorToast");
            toast.GetComponent<UnityTavernToastComponent>().Build(message);
        }

        private void BuildFeedbackToast(string message)
        {
            var toast = UnityTavernToastComponent.CreateToastHost(transform, "UnityFeedbackToast");
            toast.GetComponent<UnityTavernToastComponent>().Build(message, new Color(0.08f, 0.34f, 0.28f, 0.94f));
        }

        private void BeginHeroPowerTargeting()
        {
            BeginHeroPowerTargeting(null);
        }

        private void BeginHeroPowerTargeting(HeroPowerDefinition heroPower)
        {
            var card = CurrentHeroPowerDragCard(heroPower);
            if (card == null)
            {
                return;
            }

            if (UnityTavernDragController.IsDirectUseHeroPower(card))
            {
                Apply(new GameCommand(
                    GameCommandType.UseHeroPower,
                    -1,
                    TargetZone.Unspecified,
                    heroPowerCardId: card.CardId));
                return;
            }

            BeginDrag(card, UnityTavernDragSource.HeroPower, 0);
        }

        public void BeginDrag(
            MinionInstance card,
            UnityTavernDragSource source,
            int index,
            PointerEventData eventData = null,
            RectTransform sourceRect = null)
        {
            if (card == null || physicalCommitInProgress || !CanBeginDrag(source))
            {
                return;
            }

            if (activeDrag != null)
            {
                ClearActiveDragSession();
            }

            var opensChooseOne = source == UnityTavernDragSource.Hand &&
                                 pendingHandChooseOneOption == null &&
                                 ChooseOneOptionRegistry.TryGetOptions(card.CardId, out _);
            activeDrag = new UnityTavernDragContext(
                card,
                source,
                index,
                (source == UnityTavernDragSource.Hand || source == UnityTavernDragSource.GuideShapingSpell) &&
                !opensChooseOne &&
                service.RequiresPlayerTarget(card));
            ClearPendingPrimaryTarget();
            confirmedTargetInstanceId = null;
            confirmedSecondaryTargetInstanceId = null;
            selectedInstanceId = card.InstanceId;
            SetDiscoverBackdropRaycastBlocking(source != UnityTavernDragSource.Discover);
            if (IsPhysicalCardDrag(activeDrag))
            {
                BeginPhysicalDrag(sourceRect ?? FindCardRect(card.InstanceId));
            }

            if (eventData != null)
            {
                if (HasActiveTargetingSession())
                {
                    UpdateTargetingPointer(eventData.position, eventData.pressEventCamera);
                }
                else if (IsPhysicalCardDrag(activeDrag))
                {
                    CreateDragGhost(activeDrag, eventData);
                }
            }

            RefreshCardSelection();
            RefreshDropTargetCues();
            RefreshTargetingClarity();
        }

        private bool CanBeginDrag(UnityTavernDragSource source)
        {
            return !physicalCommitInProgress && CanConfigureDrag(source);
        }

        private bool CanConfigureDrag(UnityTavernDragSource source)
        {
            switch (source)
            {
                case UnityTavernDragSource.Shop:
                    return CanExecute(GameCommandType.BuyMinion) || CanExecute(GameCommandType.MoveShopCard);
                case UnityTavernDragSource.Discover:
                    return CanExecute(GameCommandType.ChooseDiscover);
                case UnityTavernDragSource.Hand:
                    return CanExecute(GameCommandType.PlayMinion);
                case UnityTavernDragSource.PlayerBoard:
                    return CanExecute(GameCommandType.MoveBoardMinion) || CanExecute(GameCommandType.SellMinion);
                case UnityTavernDragSource.OpponentBoard:
                    return CanExecute(GameCommandType.MoveOpponentMinion);
                case UnityTavernDragSource.HeroPower:
                    return CanExecute(GameCommandType.UseHeroPower);
                case UnityTavernDragSource.GuideShapingSpell:
                    return strategyGuideSession?.CanApply(GameCommandType.UseGuideShapingSpell) == true &&
                           service.GetCurrentGuideShapingSpell() != null;
                default:
                    return false;
            }
        }

        private bool CanDropInCurrentPhase(UnityTavernDragContext drag, UnityTavernDropTarget target, int targetIndex)
        {
            if (drag == null || drag.Card == null)
            {
                return false;
            }

            if (drag.Source == UnityTavernDragSource.GuideShapingSpell)
            {
                if (!drag.RequiresPlayerTarget)
                {
                    return target == UnityTavernDropTarget.CastZone;
                }

                if (target != UnityTavernDropTarget.PlayerBoard)
                {
                    return false;
                }

                var guideTargetInstanceId = ResolveDropTargetInstanceId(target, targetIndex);
                if (!string.IsNullOrEmpty(pendingPrimaryTargetInstanceId))
                {
                    return service.TryValidateGuideShapingSecondaryTarget(
                        drag.Card.CardId,
                        pendingPrimaryTargetIndex,
                        pendingPrimaryTargetInstanceId,
                        targetIndex,
                        TargetZone.FriendlyBoard,
                        guideTargetInstanceId,
                        out _);
                }

                return service.TryValidatePlayerTarget(
                    drag.Card,
                    targetIndex,
                    TargetZone.FriendlyBoard,
                    guideTargetInstanceId,
                    out _);
            }

            if (IsSelectingBoardInsertForTargetedMinion(drag))
            {
                return target == UnityTavernDropTarget.PlayerBoardInsert &&
                       CanExecute(GameCommandType.PlayMinion) &&
                       StateAllowsBoardInsert(targetIndex);
            }

            if (!UnityTavernDragController.TryBuildDropCommand(drag, target, targetIndex, out var command))
            {
                return false;
            }

            if (!CanExecute(command.Type))
            {
                return false;
            }

            if (UnityTavernDragController.RequiresTwoTargets(drag?.Card))
            {
                if (target != UnityTavernDropTarget.PlayerBoard && target != UnityTavernDropTarget.TavernShop)
                {
                    return false;
                }

                var instanceId = ResolveDropTargetInstanceId(target, targetIndex);
                if (!string.IsNullOrEmpty(pendingPrimaryTargetInstanceId) &&
                    string.Equals(pendingPrimaryTargetInstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (command.Type == GameCommandType.MoveBoardMinion)
            {
                return target == UnityTavernDropTarget.PlayerBoardInsert &&
                       targetIndex >= 0 &&
                       targetIndex <= service.State.Player.Board.Count;
            }

            if (command.Type == GameCommandType.MoveShopCard)
            {
                return target == UnityTavernDropTarget.TavernShopInsert &&
                       targetIndex >= 0 &&
                       targetIndex <= service.State.Player.Tavern.Shop.Count;
            }

            if (command.PlayIntent == PlayIntent.Place)
            {
                return target == UnityTavernDropTarget.PlayerBoardInsert && StateAllowsBoardInsert(targetIndex);
            }

            var targetInstanceId = ResolveDropTargetInstanceId(target, targetIndex);
            if (command.PlayIntent == PlayIntent.Magnetize)
            {
                return service.TryValidateMagnetize(
                    drag.Card,
                    targetIndex,
                    command.TargetZone,
                    targetInstanceId,
                    out _);
            }

            if (command.PlayIntent == PlayIntent.Target)
            {
                if (drag.Card.CardKind == CardKind.Minion && service.State.Player.Board.Count >= BoardLimit)
                {
                    return false;
                }

                if (!service.TryValidatePlayerTarget(
                    drag.Card,
                    targetIndex,
                    command.TargetZone,
                    targetInstanceId,
                    out _))
                {
                    return false;
                }

                return string.IsNullOrEmpty(pendingPrimaryTargetInstanceId) ||
                       !UnityTavernDragController.RequiresTwoTargets(drag.Card) ||
                       service.TryValidatePlayerSecondaryTarget(
                           drag.Card,
                           pendingPrimaryTargetIndex,
                           pendingPrimaryTargetInstanceId,
                           targetIndex,
                           command.TargetZone,
                           targetInstanceId,
                           out _);
            }

            return true;
        }

        private bool StateAllowsBoardInsert(int targetIndex)
        {
            return service.State.Player.Board.Count < BoardLimit &&
                   targetIndex >= 0 &&
                   targetIndex <= service.State.Player.Board.Count;
        }

        private bool IsSelectingBoardInsertForTargetedMinion(UnityTavernDragContext drag = null)
        {
            var resolvedDrag = drag ?? activeDrag;
            return resolvedDrag != null &&
                   resolvedDrag.Source == UnityTavernDragSource.Hand &&
                   resolvedDrag.RequiresPlayerTarget &&
                   resolvedDrag.Card != null &&
                   resolvedDrag.Card.CardKind == CardKind.Minion &&
                   !string.IsNullOrEmpty(pendingPrimaryTargetInstanceId) &&
                   (!UnityTavernDragController.RequiresTwoTargets(resolvedDrag.Card) ||
                    !string.IsNullOrEmpty(pendingSecondaryTargetInstanceId));
        }

        public void MoveDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            if (IsPhysicalCardDrag(activeDrag))
            {
                MoveDragGhost(eventData);
                var raycastObject = eventData.pointerCurrentRaycast.gameObject;
                var dropTarget = raycastObject == null
                    ? null
                    : raycastObject.GetComponentInParent<UnityTavernDropTargetBehaviour>();
                if (dropTarget != null)
                {
                    PreviewPhysicalDrop(dropTarget.Target, dropTarget.ResolvePointerTargetIndex(eventData));
                }

                return;
            }

            UpdateTargetingPointer(eventData.position, eventData.pressEventCamera);
            RefreshTargetingConnector();
        }

        public void EndDrag()
        {
            if (IsPhysicalCardDrag(activeDrag))
            {
                CancelCurrentPhysicalDrag();
                return;
            }

            if (activeDrag != null && !string.IsNullOrEmpty(pendingPrimaryTargetInstanceId))
            {
                DestroyDragGhost();
                RefreshTargetingConnector();
                return;
            }

            if (HasActiveTargetingSession())
            {
                CancelCurrentTargeting();
                return;
            }

            if (activeDrag != null)
            {
                ClearActiveDragSession();
            }
        }

        public bool CancelCurrentTargeting()
        {
            if (!HasActiveTargetingSession())
            {
                return false;
            }

            if (IsFirstTargetLocked())
            {
                if (UnityEngine.Time.unscaledTime >= nextTargetLockedHintTime)
                {
                    nextTargetLockedHintTime = UnityEngine.Time.unscaledTime + 0.35f;
                    BuildFeedbackToast(T("目标 1 已锁定，请继续选择目标 2。", "Target 1 is locked. Choose target 2."));
                }

                return false;
            }

            lastTargetingCancelFrame = UnityEngine.Time.frameCount;
            if (IsDiscoverOptionTargeting())
            {
                ClearPendingDiscoverTarget();
                Rebuild();
                return true;
            }

            if (IsHandChooseOneTargeting())
            {
                activeDrag = null;
                pendingHandChooseOneOption = null;
                ClearPendingPrimaryTarget();
                ClearTargetingHover();
                Rebuild();
                return true;
            }

            if (pendingRecruitActionDefinition != null)
            {
                CancelPendingRecruitAction();
                return true;
            }

            ClearActiveDragSession();
            return true;
        }

        private void ClearActiveDragSession()
        {
            SetDiscoverBackdropRaycastBlocking(true);
            ClearDropTargetCues();
            activeDrag = null;
            ClearPendingPrimaryTarget();
            FinalizePhysicalDragVisuals(true);
            ClearTargetingHover();
            RefreshTargetingClarity();
        }

        public bool CancelCurrentPhysicalDrag()
        {
            if (!IsPhysicalCardDrag(activeDrag) || physicalCommitInProgress)
            {
                return false;
            }

            ClearDropTargetCues();
            activeDrag = null;
            ClearPendingPrimaryTarget();
            SetDiscoverBackdropRaycastBlocking(true);
            ResetPhysicalCardTargets();
            ClearTargetingHover();
            RefreshCardSelection();
            RefreshTargetingClarity();

            if (UnityEngine.Application.isPlaying && dragGhost != null && physicalDragSourceRect != null)
            {
                physicalCommitInProgress = true;
                physicalDragTransition = StartCoroutine(AnimatePhysicalRollback());
            }
            else
            {
                FinalizePhysicalDragVisuals(true);
            }

            return true;
        }

        public void PreviewPhysicalDrop(UnityTavernDropTarget target, int targetIndex)
        {
            if (!IsPhysicalCardDrag(activeDrag) ||
                !CanDropInCurrentPhase(activeDrag, target, targetIndex))
            {
                return;
            }

            physicalPreviewTarget = target;
            physicalPreviewIndex = targetIndex;
            UpdatePhysicalDragHint(target);
            if (target == UnityTavernDropTarget.PlayerBoardInsert)
            {
                ApplyPhysicalBoardPreview(targetIndex);
            }
            else if (target == UnityTavernDropTarget.TavernShopInsert)
            {
                ApplyPhysicalShopPreview(targetIndex);
            }
            else if (target == UnityTavernDropTarget.SellZone && activeDrag.Source == UnityTavernDragSource.PlayerBoard)
            {
                ApplyPhysicalSellPreview();
            }
            else
            {
                ResetPhysicalCardTargets();
            }
        }

        public void ClearPhysicalDropPreview(UnityTavernDropTarget target)
        {
            if (!IsPhysicalCardDrag(activeDrag) || physicalPreviewTarget != target)
            {
                return;
            }

            physicalPreviewTarget = default;
            physicalPreviewIndex = -1;
            UpdatePhysicalDragHint(default);
            ResetPhysicalCardTargets();
        }

        private void BeginPhysicalDrag(RectTransform sourceRect)
        {
            FinalizePhysicalDragVisuals(true);
            physicalDragSourceRect = sourceRect;
            physicalPreviewTarget = default;
            physicalPreviewIndex = -1;
            CapturePhysicalDragRowCards();

            physicalDragSourceSlotRect = sourceRect != null ? sourceRect.parent as RectTransform : null;
            if (physicalDragSourceSlotRect == null)
            {
                return;
            }

            physicalDragSourceGroup = UnityTavernUiStyle.EnsureComponent<CanvasGroup>(physicalDragSourceSlotRect.gameObject);
            physicalDragSourceAlpha = physicalDragSourceGroup.alpha;
            physicalDragSourceBlocksRaycasts = physicalDragSourceGroup.blocksRaycasts;
            physicalDragSourceInteractable = physicalDragSourceGroup.interactable;
            physicalDragSourceGroup.alpha = 0f;
            physicalDragSourceGroup.blocksRaycasts = false;
            physicalDragSourceGroup.interactable = false;

            if (activeDrag?.Source != UnityTavernDragSource.Shop)
            {
                return;
            }

            physicalDragSourceSlotLayout = UnityTavernUiStyle.EnsureComponent<LayoutElement>(physicalDragSourceSlotRect.gameObject);
            physicalDragSourceSlotIgnoredLayout = physicalDragSourceSlotLayout.ignoreLayout;
            physicalDragSourceSlotLayout.ignoreLayout = true;
            if (physicalDragSourceSlotRect.parent is RectTransform row)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            }
        }

        private void CapturePhysicalDragRowCards()
        {
            physicalCardHomePositions.Clear();
            physicalCardTargetPositions.Clear();
            physicalBoardCardCenterXs.Clear();
            physicalBoardCardWidth = 0f;
            physicalShopCardCenterXs.Clear();
            physicalShopCardWidth = 0f;
            physicalShopCardCenterXs.Clear();
            physicalShopCardWidth = 0f;
            if (activeDrag?.Source == UnityTavernDragSource.Shop)
            {
                CapturePhysicalShopCards();
                return;
            }

            CapturePhysicalBoardCards();
        }

        private void CapturePhysicalBoardCards()
        {
            CapturePhysicalRowCards(
                "UnityPlayerBoardZone",
                service.State.Player.Board,
                physicalBoardCardCenterXs,
                out physicalBoardCardWidth);
        }

        private void CapturePhysicalShopCards()
        {
            CapturePhysicalRowCards(
                "UnityShopZone",
                service.State.Player.Tavern.Shop,
                physicalShopCardCenterXs,
                out physicalShopCardWidth);
        }

        private void CapturePhysicalRowCards(
            string zoneName,
            IReadOnlyList<MinionInstance> rowCards,
            ICollection<float> centerXs,
            out float averageWidth)
        {
            averageWidth = 0f;
            var rowZone = GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect.name == zoneName);
            if (rowZone == null)
            {
                return;
            }

            var cards = rowZone.GetComponentsInChildren<UnityTavernCardComponent>(true);
            var orderedCards = rowCards
                .Where(card => card != null)
                .Select(card => cards.FirstOrDefault(component =>
                    component.Card != null &&
                    string.Equals(component.Card.InstanceId, card.InstanceId, StringComparison.OrdinalIgnoreCase)))
                .Where(component => component != null)
                .ToArray();
            var widthTotal = 0f;
            var widthCount = 0;
            for (var index = 0; index < orderedCards.Length; index += 1)
            {
                var component = orderedCards[index];
                var rect = component.transform as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                physicalCardHomePositions[rect] = rect.anchoredPosition;
                physicalCardTargetPositions[rect] = rect.anchoredPosition;
                centerXs.Add(RootLocalCenter(rect).x);
                widthTotal += RootLocalWidth(rect);
                widthCount += 1;
            }

            averageWidth = widthCount == 0 ? 0f : widthTotal / widthCount;
        }

        private void ApplyPhysicalBoardPreview(int targetIndex)
        {
            ResetPhysicalCardTargets();
            if (activeDrag == null)
            {
                return;
            }

            var boardCount = service.State.Player.Board.Count;
            var shift = PhysicalBoardCardStep();
            var resolvedInsert = activeDrag.Source == UnityTavernDragSource.PlayerBoard && targetIndex > activeDrag.Index
                ? targetIndex - 1
                : targetIndex;
            resolvedInsert = Mathf.Clamp(resolvedInsert, 0, Mathf.Max(0, boardCount));

            foreach (var pair in physicalCardHomePositions)
            {
                var component = pair.Key == null ? null : pair.Key.GetComponent<UnityTavernCardComponent>();
                var cardIndex = component?.Card == null
                    ? -1
                    : service.State.Player.Board.FindIndex(card =>
                        card != null && string.Equals(card.InstanceId, component.Card.InstanceId, StringComparison.OrdinalIgnoreCase));
                if (cardIndex < 0 || cardIndex == activeDrag.Index && activeDrag.Source == UnityTavernDragSource.PlayerBoard)
                {
                    continue;
                }

                var offset = 0f;
                if (activeDrag.Source == UnityTavernDragSource.Hand)
                {
                    offset = cardIndex < resolvedInsert ? -shift * 0.5f : shift * 0.5f;
                }
                else if (activeDrag.Source == UnityTavernDragSource.PlayerBoard)
                {
                    if (resolvedInsert < activeDrag.Index && cardIndex >= resolvedInsert && cardIndex < activeDrag.Index)
                    {
                        offset = shift;
                    }
                    else if (resolvedInsert > activeDrag.Index && cardIndex > activeDrag.Index && cardIndex <= resolvedInsert)
                    {
                        offset = -shift;
                    }
                }

                physicalCardTargetPositions[pair.Key] = pair.Value + new Vector2(offset, Mathf.Abs(offset) > 0.01f ? 3f : 0f);
            }
        }

        private void ApplyPhysicalShopPreview(int targetIndex)
        {
            ResetPhysicalCardTargets();
            if (activeDrag?.Source != UnityTavernDragSource.Shop)
            {
                return;
            }

            var shop = service.State.Player.Tavern.Shop;
            var shift = BoardCardStep(physicalShopCardCenterXs, physicalShopCardWidth);
            var resolvedInsert = targetIndex > activeDrag.Index ? targetIndex - 1 : targetIndex;
            resolvedInsert = Mathf.Clamp(resolvedInsert, 0, Mathf.Max(0, shop.Count - 1));
            foreach (var pair in physicalCardHomePositions)
            {
                var component = pair.Key == null ? null : pair.Key.GetComponent<UnityTavernCardComponent>();
                var cardIndex = component?.Card == null
                    ? -1
                    : shop.FindIndex(card =>
                        card != null && string.Equals(card.InstanceId, component.Card.InstanceId, StringComparison.OrdinalIgnoreCase));
                if (cardIndex < 0 || cardIndex == activeDrag.Index)
                {
                    continue;
                }

                var offset = 0f;
                if (resolvedInsert < activeDrag.Index && cardIndex >= resolvedInsert && cardIndex < activeDrag.Index)
                {
                    offset = shift;
                }
                else if (resolvedInsert > activeDrag.Index && cardIndex > activeDrag.Index && cardIndex <= resolvedInsert)
                {
                    offset = -shift;
                }

                physicalCardTargetPositions[pair.Key] = pair.Value + new Vector2(offset, Mathf.Abs(offset) > 0.01f ? 3f : 0f);
            }
        }

        private void ResetPhysicalCardTargets()
        {
            foreach (var pair in physicalCardHomePositions)
            {
                if (pair.Key != null)
                {
                    physicalCardTargetPositions[pair.Key] = pair.Value;
                }
            }
        }

        private void ApplyPhysicalSellPreview()
        {
            ResetPhysicalCardTargets();
            if (activeDrag?.Source != UnityTavernDragSource.PlayerBoard)
            {
                return;
            }

            var shift = LayoutContext().IsCompact ? 40f : 54f;
            foreach (var pair in physicalCardHomePositions)
            {
                var component = pair.Key == null ? null : pair.Key.GetComponent<UnityTavernCardComponent>();
                var cardIndex = component?.Card == null
                    ? -1
                    : service.State.Player.Board.FindIndex(card =>
                        card != null && string.Equals(card.InstanceId, component.Card.InstanceId, StringComparison.OrdinalIgnoreCase));
                if (cardIndex < 0 || cardIndex == activeDrag.Index)
                {
                    continue;
                }

                physicalCardTargetPositions[pair.Key] = pair.Value + new Vector2(
                    cardIndex < activeDrag.Index ? shift : -shift,
                    3f);
            }
        }

        private IEnumerator AnimatePhysicalRollback()
        {
            var ghostRect = dragGhost == null ? null : dragGhost.GetComponent<RectTransform>();
            var destination = RootLocalCenter(physicalDragSourceRect);
            yield return AnimateDragGhostTo(ghostRect, destination, 1f, 0.16f, false, 24f);
            FinalizePhysicalDragVisuals(true);
            yield return null;
            physicalDragTransition = null;
            physicalCommitInProgress = false;
        }

        private void FinalizePhysicalDragVisuals(bool destroyGhost)
        {
            if (physicalDragSourceGroup != null)
            {
                physicalDragSourceGroup.alpha = physicalDragSourceAlpha;
                physicalDragSourceGroup.blocksRaycasts = physicalDragSourceBlocksRaycasts;
                physicalDragSourceGroup.interactable = physicalDragSourceInteractable;
            }

            if (physicalDragSourceSlotLayout != null)
            {
                physicalDragSourceSlotLayout.ignoreLayout = physicalDragSourceSlotIgnoredLayout;
                if (physicalDragSourceSlotRect != null && physicalDragSourceSlotRect.parent is RectTransform row)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(row);
                }
            }

            foreach (var pair in physicalCardHomePositions)
            {
                if (pair.Key != null)
                {
                    pair.Key.anchoredPosition = pair.Value;
                }
            }

            physicalCardHomePositions.Clear();
            physicalCardTargetPositions.Clear();
            physicalBoardCardCenterXs.Clear();
            physicalBoardCardWidth = 0f;
            physicalDragSourceRect = null;
            physicalDragSourceSlotRect = null;
            physicalDragSourceGroup = null;
            physicalDragSourceSlotLayout = null;
            physicalPreviewTarget = default;
            physicalPreviewIndex = -1;
            dragGhostTargetAvailable = false;
            dragGhostTargetRotation = 0f;
            if (destroyGhost)
            {
                DestroyDragGhost();
            }
        }

        public int ResolvePlayerBoardInsertIndex(Vector2 screenPosition, Camera eventCamera = null)
        {
            var root = transform as RectTransform;
            if (root == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, eventCamera, out var localPoint))
            {
                return Mathf.Clamp(physicalPreviewIndex, 0, service.State.Player.Board.Count);
            }

            var centers = physicalBoardCardCenterXs.Count > 0
                ? physicalBoardCardCenterXs
                : CurrentBoardCardCenterXs(out physicalBoardCardWidth);
            var previousIndex = physicalPreviewTarget == UnityTavernDropTarget.PlayerBoardInsert
                ? physicalPreviewIndex
                : -1;
            return UnityTavernDragController.ResolveBoardInsertIndex(
                centers,
                localPoint.x,
                previousIndex,
                Mathf.Max(0f, physicalBoardCardWidth) * 0.10f);
        }

        public int ResolveShopInsertIndex(Vector2 screenPosition, Camera eventCamera = null)
        {
            var root = transform as RectTransform;
            if (root == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, eventCamera, out var localPoint))
            {
                return Mathf.Clamp(physicalPreviewIndex, 0, service.State.Player.Tavern.Shop.Count);
            }

            var centers = physicalShopCardCenterXs.Count > 0
                ? physicalShopCardCenterXs
                : CurrentShopCardCenterXs(out physicalShopCardWidth);
            var previousIndex = physicalPreviewTarget == UnityTavernDropTarget.TavernShopInsert
                ? physicalPreviewIndex
                : -1;
            return UnityTavernDragController.ResolveBoardInsertIndex(
                centers,
                localPoint.x,
                previousIndex,
                Mathf.Max(0f, physicalShopCardWidth) * 0.10f);
        }

        private List<float> CurrentBoardCardCenterXs(out float averageWidth)
        {
            return CurrentRowCardCenterXs(
                "UnityPlayerBoardZone",
                service.State.Player.Board,
                out averageWidth);
        }

        private List<float> CurrentShopCardCenterXs(out float averageWidth)
        {
            return CurrentRowCardCenterXs(
                "UnityShopZone",
                service.State.Player.Tavern.Shop,
                out averageWidth);
        }

        private List<float> CurrentRowCardCenterXs(
            string zoneName,
            IReadOnlyList<MinionInstance> rowCards,
            out float averageWidth)
        {
            var centers = new List<float>();
            var rowZone = GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect.name == zoneName);
            if (rowZone == null)
            {
                averageWidth = 0f;
                return centers;
            }

            var cards = rowZone.GetComponentsInChildren<UnityTavernCardComponent>(true);
            var widthTotal = 0f;
            foreach (var rowCard in rowCards.Where(card => card != null))
            {
                var component = cards.FirstOrDefault(candidate =>
                    candidate.Card != null &&
                    string.Equals(candidate.Card.InstanceId, rowCard.InstanceId, StringComparison.OrdinalIgnoreCase));
                var rect = component == null ? null : component.transform as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                centers.Add(RootLocalCenter(rect).x);
                widthTotal += RootLocalWidth(rect);
            }

            averageWidth = centers.Count == 0 ? 0f : widthTotal / centers.Count;
            return centers;
        }

        private float PhysicalBoardCardStep()
        {
            return BoardCardStep(physicalBoardCardCenterXs, physicalBoardCardWidth);
        }

        private float BoardCardStep(IReadOnlyList<float> centers, float cardWidth)
        {
            if (centers != null && centers.Count > 1)
            {
                var distance = 0f;
                for (var index = 1; index < centers.Count; index += 1)
                {
                    distance += Mathf.Abs(centers[index] - centers[index - 1]);
                }

                var averageDistance = distance / (centers.Count - 1);
                if (averageDistance > 1f)
                {
                    return averageDistance;
                }
            }

            return Mathf.Max(cardWidth * 1.20f, LayoutContext().IsCompact ? 96f : 128f);
        }

        private float RootLocalWidth(RectTransform target)
        {
            var root = transform as RectTransform;
            if (root == null || target == null)
            {
                return 0f;
            }

            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            return Mathf.Abs(root.InverseTransformPoint(corners[2]).x - root.InverseTransformPoint(corners[1]).x);
        }

        public void HandleDrop(UnityTavernDropTarget target, int targetIndex = -1)
        {
            if (activeDrag == null)
            {
                return;
            }

            var drag = activeDrag;
            if (IsSelectingBoardInsertForTargetedMinion(drag))
            {
                if (target != UnityTavernDropTarget.PlayerBoardInsert || !StateAllowsBoardInsert(targetIndex))
                {
                    lastError = T("请选择一个有效的上场位置。", "Choose a valid board position.");
                    lastFeedback = null;
                    BuildErrorToast(lastError);
                    RefreshDropTargetCues();
                    RefreshTargetingClarity();
                    return;
                }

                if (!service.TryValidatePlayerTarget(
                        drag.Card,
                        pendingPrimaryTargetIndex,
                        pendingPrimaryTargetZone,
                        pendingPrimaryTargetInstanceId,
                        out var pendingTargetFailureReason))
                {
                    lastError = pendingTargetFailureReason;
                    lastFeedback = null;
                    BuildErrorToast(lastError);
                    RefreshDropTargetCues();
                    RefreshTargetingClarity();
                    return;
                }

                if (!service.TryValidatePlayerSecondaryTarget(
                        drag.Card,
                        pendingPrimaryTargetIndex,
                        pendingPrimaryTargetInstanceId,
                        pendingSecondaryTargetIndex,
                        pendingSecondaryTargetZone,
                        pendingSecondaryTargetInstanceId,
                        out var pendingSecondaryFailureReason))
                {
                    lastError = pendingSecondaryFailureReason;
                    lastFeedback = null;
                    BuildErrorToast(lastError);
                    RefreshDropTargetCues();
                    RefreshTargetingClarity();
                    return;
                }

                var targetedMinionCommand = new GameCommand(
                    GameCommandType.PlayMinion,
                    drag.Index,
                    PlayIntent.Target,
                    boardInsertIndex: targetIndex,
                    targetIndex: pendingPrimaryTargetIndex,
                    targetZone: pendingPrimaryTargetZone,
                    targetInstanceId: pendingPrimaryTargetInstanceId,
                    secondaryTargetIndex: pendingSecondaryTargetIndex,
                    secondaryTargetZone: pendingSecondaryTargetZone,
                    secondaryTargetInstanceId: pendingSecondaryTargetInstanceId);
                confirmedTargetInstanceId = pendingPrimaryTargetInstanceId;
                confirmedSecondaryTargetInstanceId = pendingSecondaryTargetInstanceId;
                confirmedTargetUntil = UnityEngine.Time.unscaledTime + 1.25f;
                CompleteDrop(drag, target, targetedMinionCommand, pendingPrimaryTargetInstanceId);
                return;
            }

            if (drag.Source == UnityTavernDragSource.GuideShapingSpell)
            {
                HandleGuideShapingSpellDrop(drag, target, targetIndex);
                return;
            }

            if (drag.Source == UnityTavernDragSource.Hand &&
                pendingHandChooseOneCard == null &&
                target == UnityTavernDropTarget.CastZone &&
                ChooseOneOptionRegistry.TryGetOptions(drag.Card.CardId, out _))
            {
                BeginHandChooseOne(drag.Card, drag.Index);
                return;
            }

            if (!UnityTavernDragController.TryBuildDropCommand(drag, target, targetIndex, out var command, out var failureReason))
            {
                lastError = failureReason == UnityTavernTargetingFailureReason.MissingTarget
                    ? "请选择一个目标。"
                    : "该目标不可选。";
                lastFeedback = null;
                BuildErrorToast(lastError);
                RefreshDropTargetCues();
                RefreshTargetingClarity();
                return;
            }

            var resolvedTargetInstanceId = ResolveDropTargetInstanceId(target, targetIndex);
            if (command.PlayIntent == PlayIntent.Target &&
                !service.TryValidatePlayerTarget(
                    drag.Card,
                    targetIndex,
                    command.TargetZone,
                    resolvedTargetInstanceId,
                    out var targetFailureReason))
            {
                lastError = targetFailureReason;
                lastFeedback = null;
                BuildErrorToast(lastError);
                RefreshDropTargetCues();
                RefreshTargetingClarity();
                return;
            }

            if (command.PlayIntent == PlayIntent.Magnetize &&
                !service.TryValidateMagnetize(
                    drag.Card,
                    targetIndex,
                    command.TargetZone,
                    resolvedTargetInstanceId,
                    out var magnetizeFailureReason))
            {
                lastError = magnetizeFailureReason;
                lastFeedback = null;
                BuildErrorToast(lastError);
                RefreshDropTargetCues();
                RefreshTargetingClarity();
                return;
            }

            if (UnityTavernDragController.RequiresTwoTargets(drag.Card) &&
                target != UnityTavernDropTarget.PlayerBoard &&
                target != UnityTavernDropTarget.TavernShop)
            {
                lastError = T("该效果只能选择合法随从。", "This effect can only target a legal minion.");
                lastFeedback = null;
                BuildErrorToast(lastError);
                RefreshDropTargetCues();
                RefreshTargetingClarity();
                return;
            }

            if (command.PlayIntent == PlayIntent.Target && drag.Card.CardKind == CardKind.Minion)
            {
                if (UnityTavernDragController.RequiresTwoTargets(drag.Card))
                {
                    if (string.IsNullOrEmpty(pendingPrimaryTargetInstanceId))
                    {
                        pendingPrimaryTargetIndex = targetIndex;
                        pendingPrimaryTargetZone = command.TargetZone;
                        pendingPrimaryTargetInstanceId = resolvedTargetInstanceId;
                        selectedInstanceId = resolvedTargetInstanceId;
                        BuildFeedbackToast(T(
                            "目标 1 已锁定，请选择另一个随从作为目标 2。",
                            "Target 1 is locked. Choose a different minion as target 2."));
                        ClearTargetingHover();
                        RefreshDropTargetCues();
                        RefreshTargetingClarity();
                        return;
                    }

                    if (!service.TryValidatePlayerSecondaryTarget(
                            drag.Card,
                            pendingPrimaryTargetIndex,
                            pendingPrimaryTargetInstanceId,
                            targetIndex,
                            command.TargetZone,
                            resolvedTargetInstanceId,
                            out var secondaryTargetFailureReason))
                    {
                        lastError = secondaryTargetFailureReason;
                        lastFeedback = null;
                        BuildErrorToast(lastError);
                        RefreshDropTargetCues();
                        RefreshTargetingClarity();
                        return;
                    }

                    pendingSecondaryTargetIndex = targetIndex;
                    pendingSecondaryTargetZone = command.TargetZone;
                    pendingSecondaryTargetInstanceId = resolvedTargetInstanceId;
                    confirmedSecondaryTargetInstanceId = resolvedTargetInstanceId;
                    selectedInstanceId = resolvedTargetInstanceId;
                    BuildFeedbackToast(T(
                        "两个效果目标已锁定，请选择桑德斯的上场位置。",
                        "Both effect targets are locked. Choose where to place Captain Sanders."));
                    ClearTargetingHover();
                    RefreshDropTargetCues();
                    RefreshTargetingClarity();
                    return;
                }

                pendingPrimaryTargetIndex = targetIndex;
                pendingPrimaryTargetZone = command.TargetZone;
                pendingPrimaryTargetInstanceId = resolvedTargetInstanceId;
                selectedInstanceId = resolvedTargetInstanceId;
                BuildFeedbackToast(T(
                    "效果目标已选择，请再选择随从的上场位置。",
                    "Effect target selected. Choose where to place the minion."));
                ClearTargetingHover();
                RefreshDropTargetCues();
                RefreshTargetingClarity();
                return;
            }

            if (UnityTavernDragController.RequiresTwoTargets(drag.Card))
            {
                var resolvedTargetZone = ToTargetZone(target);
                if (string.IsNullOrEmpty(pendingPrimaryTargetInstanceId))
                {
                    pendingPrimaryTargetIndex = targetIndex;
                    pendingPrimaryTargetZone = resolvedTargetZone;
                    pendingPrimaryTargetInstanceId = resolvedTargetInstanceId;
                    selectedInstanceId = resolvedTargetInstanceId;
                    BuildFeedbackToast("已选择目标 1/2，请选择另一个目标。");
                    RefreshDropTargetCues();
                    RefreshTargetingClarity();
                    return;
                }

                if (string.Equals(pendingPrimaryTargetInstanceId, resolvedTargetInstanceId, StringComparison.OrdinalIgnoreCase))
                {
                    lastError = "第二个目标必须与第一个目标不同。";
                    lastFeedback = null;
                    BuildErrorToast(lastError);
                    RefreshDropTargetCues();
                    RefreshTargetingClarity();
                    return;
                }

                command = new GameCommand(
                    GameCommandType.UseHeroPower,
                    pendingPrimaryTargetIndex,
                    pendingPrimaryTargetZone,
                    targetIndex,
                    resolvedTargetZone,
                    pendingPrimaryTargetInstanceId,
                    resolvedTargetInstanceId,
                    heroPowerCardId: drag.Card.CardId);
                confirmedTargetInstanceId = pendingPrimaryTargetInstanceId;
                confirmedSecondaryTargetInstanceId = resolvedTargetInstanceId;
            }
            else if (command.PlayIntent == PlayIntent.Target || command.PlayIntent == PlayIntent.Magnetize)
            {
                command = new GameCommand(
                    GameCommandType.PlayMinion,
                    drag.Index,
                    command.PlayIntent,
                    targetIndex: targetIndex,
                    targetZone: command.TargetZone,
                    targetInstanceId: resolvedTargetInstanceId,
                    choiceId: IsHandChooseOneTargeting() ? pendingHandChooseOneOption.ChoiceId : null);
            }

            CompleteDrop(drag, target, command, resolvedTargetInstanceId);
        }

        private void HandleGuideShapingSpellDrop(
            UnityTavernDragContext drag,
            UnityTavernDropTarget target,
            int targetIndex)
        {
            if (!drag.RequiresPlayerTarget && target == UnityTavernDropTarget.CastZone)
            {
                CompleteDrop(
                    drag,
                    target,
                    new GameCommand(
                        GameCommandType.UseGuideShapingSpell,
                        -1,
                        TargetZone.Unspecified,
                        cardId: drag.Card.CardId),
                    null);
                return;
            }

            if (target != UnityTavernDropTarget.PlayerBoard || targetIndex < 0)
            {
                lastError = T("请选择一个友方战场随从。", "Choose a friendly board minion.");
                lastFeedback = null;
                BuildErrorToast(lastError);
                RefreshDropTargetCues();
                RefreshTargetingClarity();
                return;
            }

            var targetInstanceId = ResolveDropTargetInstanceId(target, targetIndex);
            if (string.IsNullOrEmpty(pendingPrimaryTargetInstanceId))
            {
                if (!service.TryValidatePlayerTarget(
                        drag.Card,
                        targetIndex,
                        TargetZone.FriendlyBoard,
                        targetInstanceId,
                        out var primaryFailureReason))
                {
                    lastError = primaryFailureReason;
                    lastFeedback = null;
                    BuildErrorToast(lastError);
                    RefreshDropTargetCues();
                    RefreshTargetingClarity();
                    return;
                }

                if (!service.TryValidateGuideShapingSecondaryTarget(
                        drag.Card.CardId,
                        targetIndex,
                        targetInstanceId,
                        -1,
                        TargetZone.Unspecified,
                        null,
                        out _))
                {
                    pendingPrimaryTargetIndex = targetIndex;
                    pendingPrimaryTargetZone = TargetZone.FriendlyBoard;
                    pendingPrimaryTargetInstanceId = targetInstanceId;
                    selectedInstanceId = targetInstanceId;
                    BuildFeedbackToast(T(
                        "已选择要重触发战吼的随从，请再选择它的效果目标。",
                        "Battlecry source selected. Choose its effect target."));
                    ClearTargetingHover();
                    RefreshDropTargetCues();
                    RefreshTargetingClarity();
                    return;
                }

                confirmedTargetInstanceId = targetInstanceId;
                confirmedTargetUntil = UnityEngine.Time.unscaledTime + 1.25f;
                CompleteDrop(
                    drag,
                    target,
                    new GameCommand(
                        GameCommandType.UseGuideShapingSpell,
                        targetIndex,
                        TargetZone.FriendlyBoard,
                        targetInstanceId: targetInstanceId,
                        cardId: drag.Card.CardId),
                    targetInstanceId);
                return;
            }

            if (!service.TryValidateGuideShapingSecondaryTarget(
                    drag.Card.CardId,
                    pendingPrimaryTargetIndex,
                    pendingPrimaryTargetInstanceId,
                    targetIndex,
                    TargetZone.FriendlyBoard,
                    targetInstanceId,
                    out var secondaryFailureReason))
            {
                lastError = secondaryFailureReason;
                lastFeedback = null;
                BuildErrorToast(lastError);
                RefreshDropTargetCues();
                RefreshTargetingClarity();
                return;
            }

            confirmedTargetInstanceId = pendingPrimaryTargetInstanceId;
            confirmedSecondaryTargetInstanceId = targetInstanceId;
            confirmedTargetUntil = UnityEngine.Time.unscaledTime + 1.25f;
            CompleteDrop(
                drag,
                target,
                new GameCommand(
                    GameCommandType.UseGuideShapingSpell,
                    pendingPrimaryTargetIndex,
                    pendingPrimaryTargetZone,
                    targetIndex,
                    TargetZone.FriendlyBoard,
                    pendingPrimaryTargetInstanceId,
                    targetInstanceId,
                    cardId: drag.Card.CardId),
                targetInstanceId);
        }

        private void CompleteDrop(
            UnityTavernDragContext drag,
            UnityTavernDropTarget target,
            GameCommand command,
            string resolvedTargetInstanceId)
        {
            var completesHandChooseOne = IsHandChooseOneTargeting();
            var physicalDrop = IsPhysicalCardDrag(drag);
            var physicalDropIndex = physicalPreviewIndex >= 0
                ? physicalPreviewIndex
                : ResolvePhysicalCommandIndex(command);
            selectedInstanceId = target == UnityTavernDropTarget.SellZone
                ? null
                : resolvedTargetInstanceId ?? drag.Card.InstanceId;
            if ((drag.Source == UnityTavernDragSource.HeroPower ||
                 drag.Source == UnityTavernDragSource.GuideShapingSpell ||
                 command.PlayIntent == PlayIntent.Target ||
                 command.PlayIntent == PlayIntent.Magnetize) &&
                !string.IsNullOrEmpty(resolvedTargetInstanceId))
            {
                if (string.IsNullOrEmpty(confirmedTargetInstanceId))
                {
                    confirmedTargetInstanceId = resolvedTargetInstanceId;
                }

                confirmedTargetUntil = UnityEngine.Time.unscaledTime + 1.25f;
            }

            ClearDropTargetCues();
            activeDrag = null;
            ClearPendingPrimaryTarget();
            if (completesHandChooseOne)
            {
                ClearPendingHandChooseOne();
            }
            SetDiscoverBackdropRaycastBlocking(true);
            ClearTargetingHover();

            if (!physicalDrop)
            {
                DestroyDragGhost();
                Apply(command);
                return;
            }

            var goldBefore = service.State.Player.Tavern.Gold;
            if (!TryApply(command))
            {
                FinalizePhysicalDragVisuals(true);
                Rebuild();
                return;
            }

            var goldDelta = service.State.Player.Tavern.Gold - goldBefore;
            if (UnityEngine.Application.isPlaying && dragGhost != null)
            {
                physicalCommitInProgress = true;
                physicalDragTransition = StartCoroutine(AnimatePhysicalCommit(target, physicalDropIndex, goldDelta));
                return;
            }

            FinalizePhysicalDragVisuals(true);
            Rebuild();
        }

        private static int ResolvePhysicalCommandIndex(GameCommand command)
        {
            if (command == null)
            {
                return -1;
            }

            if (command.BoardInsertIndex >= 0)
            {
                return command.BoardInsertIndex;
            }

            return command.TargetIndex;
        }

        private string ResolveDropTargetInstanceId(UnityTavernDropTarget target, int targetIndex)
        {
            if (target == UnityTavernDropTarget.PlayerBoard && targetIndex >= 0 && targetIndex < service.State.Player.Board.Count)
            {
                return service.State.Player.Board[targetIndex]?.InstanceId;
            }

            if (target == UnityTavernDropTarget.OpponentBoard && targetIndex >= 0 && targetIndex < service.State.Opponent.Board.Count)
            {
                return service.State.Opponent.Board[targetIndex]?.InstanceId;
            }

            if (target == UnityTavernDropTarget.TavernShop && targetIndex >= 0 && targetIndex < service.State.Player.Tavern.Shop.Count)
            {
                return service.State.Player.Tavern.Shop[targetIndex]?.InstanceId;
            }

            return null;
        }

        private void CreateDragGhost(UnityTavernDragContext drag, PointerEventData eventData)
        {
            DestroyDragGhost();
            if (drag?.Card == null || eventData == null)
            {
                return;
            }

            var mode = PhysicalDragCardMode(drag.Source);
            dragGhost = UnityTavernCardComponent.CreateCardHost(mode, transform, "UnityDragGhost-" + drag.Card.InstanceId);
            dragGhost.GetComponent<UnityTavernCardComponent>().Bind(drag.Card, mode, null, null, null, useEnglish: service.UseEnglish);
            dragGhost.transform.SetAsLastSibling();

            var canvas = UnityTavernUiStyle.EnsureComponent<Canvas>(dragGhost);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5000;

            var group = UnityTavernUiStyle.EnsureComponent<CanvasGroup>(dragGhost);
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0.92f;

            var ghostRect = dragGhost.GetComponent<RectTransform>();
            ghostRect.localScale = Vector3.one * (mode == UnityTavernCardMode.Board ? 1.08f : 1.06f);
            UnityTavernUiStyle.ConfigureOutline(
                dragGhost,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.FocusRing, 0.88f),
                new Vector2(3f, -3f));

            var hint = UiFactory.Label(
                "UnityPhysicalDragHint",
                dragGhost.transform,
                PhysicalDragHint(drag.Source, default),
                13,
                FontStyle.Bold);
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = UnityTavernUiStyle.TextLight;
            hint.raycastTarget = false;
            var hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0.05f, 0f);
            hintRect.anchorMax = new Vector2(0.95f, 0f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -5f);
            hintRect.sizeDelta = new Vector2(0f, 24f);
            UnityTavernUiStyle.ConfigureOutline(hint.gameObject, new Color(0f, 0f, 0f, 0.86f), new Vector2(1f, -1f));

            MoveDragGhost(eventData);
            if (dragGhostTargetAvailable)
            {
                ghostRect.anchoredPosition = dragGhostTargetLocalPosition;
            }
        }

        private void MoveDragGhost(PointerEventData eventData)
        {
            var rootRect = transform as RectTransform;
            var ghostRect = dragGhost == null ? null : dragGhost.GetComponent<RectTransform>();
            if (rootRect == null || ghostRect == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, eventData.position, eventData.pressEventCamera, out var point);
            ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
            ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
            if (!dragGhostTargetAvailable)
            {
                ghostRect.anchoredPosition = point;
                dragGhostLastTargetLocalPosition = point;
            }

            var horizontalVelocity = point.x - dragGhostLastTargetLocalPosition.x;
            dragGhostTargetRotation = Mathf.Clamp(-horizontalVelocity * 0.22f, -7f, 7f);
            dragGhostLastTargetLocalPosition = point;
            dragGhostTargetLocalPosition = point;
            dragGhostTargetAvailable = true;
        }

        private static UnityTavernCardMode PhysicalDragCardMode(UnityTavernDragSource source)
        {
            switch (source)
            {
                case UnityTavernDragSource.Shop:
                    return UnityTavernCardMode.Shop;
                case UnityTavernDragSource.PlayerBoard:
                case UnityTavernDragSource.OpponentBoard:
                    return UnityTavernCardMode.Board;
                default:
                    return UnityTavernCardMode.Hand;
            }
        }

        private string PhysicalDragHint(UnityTavernDragSource source, UnityTavernDropTarget target)
        {
            switch (target)
            {
                case UnityTavernDropTarget.Hand:
                case UnityTavernDropTarget.PurchaseZone:
                case UnityTavernDropTarget.DiscoverZone:
                    return T("释放购买", "Release to buy");
                case UnityTavernDropTarget.PlayerBoardInsert:
                    return source == UnityTavernDragSource.PlayerBoard
                        ? T("释放调整站位", "Release to reorder")
                        : T("释放打出", "Release to play");
                case UnityTavernDropTarget.TavernShopInsert:
                    return T("释放调整酒馆位置", "Release to reorder the tavern");
                case UnityTavernDropTarget.SellZone:
                    return T("释放出售", "Release to sell");
                case UnityTavernDropTarget.CastZone:
                    return T("释放施放", "Release to cast");
            }

            switch (source)
            {
                case UnityTavernDragSource.Shop:
                    return T("酒馆内横移换位，拖到下方购买", "Reorder in the tavern or drag down to buy");
                case UnityTavernDragSource.Hand:
                    return activeDrag?.Card != null &&
                           (activeDrag.Card.CardKind == CardKind.TavernSpell || activeDrag.Card.CardKind == CardKind.Spell)
                        ? T("拖出手牌后在中央区域松开施放", "Drag out of your hand and release in the center to cast")
                        : T("拖到战场打出", "Drag to the board to play");
                case UnityTavernDragSource.GuideShapingSpell:
                    return activeDrag?.RequiresPlayerTarget == true
                        ? T("拖向友方随从选择目标", "Drag to a friendly minion to target it")
                        : T("拖到中央区域松开施放", "Drag to the center and release to cast");
                case UnityTavernDragSource.PlayerBoard:
                    return T("战场换位或拖给鲍勃出售", "Reorder or drag to Bob to sell");
                default:
                    return T("拖动卡牌", "Drag card");
            }
        }

        private void UpdatePhysicalDragHint(UnityTavernDropTarget target)
        {
            if (dragGhost == null || activeDrag == null)
            {
                return;
            }

            var hint = dragGhost.transform.Find("UnityPhysicalDragHint")?.GetComponent<Text>();
            if (hint != null)
            {
                hint.text = PhysicalDragHint(activeDrag.Source, target);
            }
        }

        private IEnumerator AnimatePhysicalCommit(UnityTavernDropTarget target, int targetIndex, int goldDelta)
        {
            var ghostRect = dragGhost == null ? null : dragGhost.GetComponent<RectTransform>();
            var destination = PhysicalCommitDestination(target, targetIndex);
            var purchaseTarget = target == UnityTavernDropTarget.Hand ||
                                 target == UnityTavernDropTarget.PurchaseZone ||
                                 target == UnityTavernDropTarget.DiscoverZone;
            var targetScale = target == UnityTavernDropTarget.SellZone
                ? 0.12f
                : purchaseTarget || target == UnityTavernDropTarget.CastZone ? 0.24f : 0.90f;
            var fade = target == UnityTavernDropTarget.SellZone ||
                       purchaseTarget ||
                       target == UnityTavernDropTarget.CastZone;
            var arcHeight = purchaseTarget
                ? 58f
                : target == UnityTavernDropTarget.SellZone ? 20f : target == UnityTavernDropTarget.CastZone ? 34f : 12f;
            if (target == UnityTavernDropTarget.SellZone)
            {
                ShowSellCoinFeedback(goldDelta);
            }

            yield return AnimateDragGhostTo(ghostRect, destination, targetScale, 0.20f, fade, arcHeight);
            FinalizePhysicalDragVisuals(true);
            Rebuild();
            yield return null;
            physicalDragTransition = null;
            physicalCommitInProgress = false;
        }

        private IEnumerator AnimateDragGhostTo(
            RectTransform ghostRect,
            Vector2 destination,
            float targetScale,
            float duration,
            bool fade,
            float arcHeight)
        {
            if (ghostRect == null)
            {
                yield break;
            }

            var group = ghostRect.GetComponent<CanvasGroup>();
            var start = ghostRect.anchoredPosition;
            var startScale = ghostRect.localScale;
            var startAlpha = group == null ? 1f : group.alpha;
            var resolvedDuration = UnityUiMotionSettings.ReduceMotion ? 0f : duration;
            if (resolvedDuration <= 0f)
            {
                ghostRect.anchoredPosition = destination;
                ghostRect.localScale = Vector3.one * targetScale;
                ghostRect.localRotation = Quaternion.identity;
                if (group != null && fade)
                {
                    group.alpha = 0f;
                }

                yield break;
            }

            var elapsed = 0f;
            while (elapsed < resolvedDuration && ghostRect != null)
            {
                elapsed += UnityEngine.Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / resolvedDuration);
                var eased = normalized * normalized * (3f - 2f * normalized);
                var position = Vector2.Lerp(start, destination, eased);
                position.y += Mathf.Sin(normalized * Mathf.PI) * arcHeight;
                ghostRect.anchoredPosition = position;
                ghostRect.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, eased);
                ghostRect.localRotation = Quaternion.Slerp(ghostRect.localRotation, Quaternion.identity, eased);
                if (group != null && fade)
                {
                    group.alpha = Mathf.Lerp(startAlpha, 0f, eased);
                }

                yield return null;
            }
        }

        private Vector2 PhysicalCommitDestination(UnityTavernDropTarget target, int targetIndex)
        {
            RectTransform destination;
            switch (target)
            {
                case UnityTavernDropTarget.Hand:
                case UnityTavernDropTarget.PurchaseZone:
                case UnityTavernDropTarget.DiscoverZone:
                    destination = FindNamedRect("UnityHandZone");
                    break;
                case UnityTavernDropTarget.PlayerBoardInsert:
                    return BoardInsertRootPosition(targetIndex);
                case UnityTavernDropTarget.TavernShopInsert:
                    return ShopInsertRootPosition(targetIndex);
                case UnityTavernDropTarget.SellZone:
                    destination = FindNamedRect("UnitySellDropZone");
                    break;
                case UnityTavernDropTarget.CastZone:
                    destination = FindNamedRect("UnitySpellCastDropZone");
                    break;
                case UnityTavernDropTarget.OpponentBoard:
                    destination = FindNamedRect("UnityOpponentBoardZone");
                    break;
                default:
                    destination = null;
                    break;
            }

            return destination == null ? dragGhostTargetLocalPosition : RootLocalCenter(destination);
        }

        private Vector2 BoardInsertRootPosition(int targetIndex)
        {
            var centers = physicalBoardCardCenterXs.Count > 0
                ? physicalBoardCardCenterXs
                : CurrentBoardCardCenterXs(out physicalBoardCardWidth);
            if (centers.Count == 0)
            {
                var boardZone = FindNamedRect("UnityPlayerBoardZone");
                return boardZone == null ? dragGhostTargetLocalPosition : RootLocalCenter(boardZone);
            }

            var resolvedIndex = Mathf.Clamp(targetIndex, 0, centers.Count);
            var step = BoardCardStep(centers, physicalBoardCardWidth);
            float x;
            if (resolvedIndex == 0)
            {
                x = centers[0] - step * 0.5f;
            }
            else if (resolvedIndex == centers.Count)
            {
                x = centers[centers.Count - 1] + step * 0.5f;
            }
            else
            {
                x = (centers[resolvedIndex - 1] + centers[resolvedIndex]) * 0.5f;
            }

            var cardRect = physicalCardHomePositions.Keys.FirstOrDefault(rect => rect != null) ??
                           FindCardRect(service.State.Player.Board.FirstOrDefault(card => card != null)?.InstanceId);
            var y = cardRect == null ? dragGhostTargetLocalPosition.y : RootLocalCenter(cardRect).y;
            return new Vector2(x, y);
        }

        private Vector2 ShopInsertRootPosition(int targetIndex)
        {
            var centers = physicalShopCardCenterXs.Count > 0
                ? physicalShopCardCenterXs
                : CurrentShopCardCenterXs(out physicalShopCardWidth);
            if (centers.Count == 0)
            {
                var shopZone = FindNamedRect("UnityShopZone");
                return shopZone == null ? dragGhostTargetLocalPosition : RootLocalCenter(shopZone);
            }

            var resolvedIndex = Mathf.Clamp(targetIndex, 0, centers.Count);
            var step = BoardCardStep(centers, physicalShopCardWidth);
            float x;
            if (resolvedIndex == 0)
            {
                x = centers[0] - step * 0.5f;
            }
            else if (resolvedIndex == centers.Count)
            {
                x = centers[centers.Count - 1] + step * 0.5f;
            }
            else
            {
                x = (centers[resolvedIndex - 1] + centers[resolvedIndex]) * 0.5f;
            }

            var cardRect = physicalCardHomePositions.Keys.FirstOrDefault(rect => rect != null) ??
                           FindCardRect(service.State.Player.Tavern.Shop.FirstOrDefault(card => card != null)?.InstanceId);
            var y = cardRect == null ? dragGhostTargetLocalPosition.y : RootLocalCenter(cardRect).y;
            return new Vector2(x, y);
        }

        private void ShowSellCoinFeedback(int goldDelta)
        {
            var label = GetComponentsInChildren<Text>(true)
                .FirstOrDefault(text => text.name == "UnitySellCoinFeedback");
            if (label == null)
            {
                return;
            }

            label.text = goldDelta > 0
                ? T("金币 +", "Gold +") + goldDelta
                : T("金币到账", "Gold received");
            label.gameObject.SetActive(true);
        }

        private RectTransform FindNamedRect(string objectName)
        {
            return GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect.name == objectName);
        }

        private Vector2 RootLocalCenter(RectTransform target)
        {
            var root = transform as RectTransform;
            if (root == null || target == null)
            {
                return dragGhostTargetLocalPosition;
            }

            var worldCenter = target.TransformPoint(target.rect.center);
            return root.InverseTransformPoint(worldCenter);
        }

        private void DestroyDragGhost()
        {
            if (dragGhost == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(dragGhost);
            }
            else
            {
                DestroyImmediate(dragGhost);
            }

            dragGhost = null;
        }

        private void SetDiscoverBackdropRaycastBlocking(bool blocksRaycasts)
        {
            var modals = GetComponentsInChildren<UnityTavernDiscoverModalComponent>(true);
            for (var index = 0; index < modals.Length; index += 1)
            {
                modals[index].SetBackdropRaycastBlocking(blocksRaycasts);
            }
        }

        private void SelectCard(MinionInstance card)
        {
            if (card == null)
            {
                return;
            }

            if (TryHandleDiscoverOptionTargetClick(card))
            {
                return;
            }

            if (TryHandleRecruitActionTargetClick(card))
            {
                return;
            }

            if (TryHandleTargetedCardClick(card))
            {
                return;
            }

            selectedInstanceId = card.InstanceId;
            if (rightPanelOpen)
            {
                activeInspectorTab = UnityTavernInspectorTab.Details;
            }

            Rebuild();
        }

        private bool TryHandleDiscoverOptionTargetClick(MinionInstance card)
        {
            if (!IsDiscoverOptionTargeting())
            {
                return false;
            }

            var targetIndex = service.State.Player.Board.FindIndex(item =>
                item != null && string.Equals(item.InstanceId, card.InstanceId, StringComparison.Ordinal));
            if (!service.TryValidateDiscoverOptionTarget(
                    pendingDiscoverTargetOptionIndex,
                    targetIndex,
                    TargetZone.FriendlyBoard,
                    card.InstanceId,
                    out var reason))
            {
                lastError = reason;
                lastFeedback = null;
                Rebuild();
                return true;
            }

            var optionIndex = pendingDiscoverTargetOptionIndex;
            ClearPendingDiscoverTarget();
            Apply(new GameCommand(
                GameCommandType.ChooseDiscover,
                optionIndex,
                targetIndex,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                targetInstanceId: card.InstanceId));
            return true;
        }

        private void ClearPendingDiscoverTarget()
        {
            pendingDiscoverTargetOptionIndex = -1;
            pendingDiscoverTargetSourceInstanceId = null;
            pendingDiscoverTargetOptionCard = null;
        }

        private bool TryHandleRecruitActionTargetClick(MinionInstance card)
        {
            if (pendingRecruitActionDefinition == null ||
                string.IsNullOrWhiteSpace(pendingRecruitActionSourceInstanceId))
            {
                return false;
            }

            if (!TryBuildRecruitActionTargetRequest(card, out var request))
            {
                lastError = RecruitActionTargetInvalidReason(pendingRecruitActionDefinition.TargetSpec);
                lastFeedback = null;
                Rebuild();
                return true;
            }

            if (string.IsNullOrWhiteSpace(pendingPrimaryTargetInstanceId) &&
                service.RecruitActionRequiresSecondaryTarget(pendingRecruitActionDefinition, card))
            {
                pendingPrimaryTargetIndex = request.TargetIndex;
                pendingPrimaryTargetZone = request.TargetZone;
                pendingPrimaryTargetInstanceId = request.TargetInstanceId;
                selectedInstanceId = request.TargetInstanceId;
                lastError = null;
                lastFeedback = T(
                    "战吼来源已锁定，请选择要接受该战吼效果的随从。",
                    "Battlecry source locked. Choose the minion that receives its effect.");
                Rebuild();
                return true;
            }

            ExecuteRecruitAction(request);
            return true;
        }

        private bool TryBuildRecruitActionTargetRequest(MinionInstance card, out RecruitActionRequest request)
        {
            request = null;
            if (card == null || pendingRecruitActionDefinition == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(pendingPrimaryTargetInstanceId))
            {
                var primaryTarget = service.State.Player.Board.FirstOrDefault(item =>
                    item != null && string.Equals(item.InstanceId, pendingPrimaryTargetInstanceId, StringComparison.Ordinal));
                var secondaryTargetIndex = service.State.Player.Board.FindIndex(item =>
                    item != null && string.Equals(item.InstanceId, card.InstanceId, StringComparison.Ordinal));
                if (primaryTarget == null ||
                    secondaryTargetIndex < 0 ||
                    string.Equals(primaryTarget.InstanceId, card.InstanceId, StringComparison.Ordinal) ||
                    !service.TryValidatePlayerTarget(
                        primaryTarget,
                        secondaryTargetIndex,
                        TargetZone.FriendlyBoard,
                        card.InstanceId,
                        out _))
                {
                    return false;
                }

                request = new RecruitActionRequest
                {
                    ActionId = pendingRecruitActionDefinition.ActionId,
                    SourceInstanceId = pendingRecruitActionSourceInstanceId,
                    TargetIndex = pendingPrimaryTargetIndex,
                    TargetZone = pendingPrimaryTargetZone,
                    TargetInstanceId = pendingPrimaryTargetInstanceId,
                    SecondaryTargetIndex = secondaryTargetIndex,
                    SecondaryTargetZone = TargetZone.FriendlyBoard,
                    SecondaryTargetInstanceId = card.InstanceId
                };
                return true;
            }

            if (!service.IsRecruitActionTargetCandidate(
                    pendingRecruitActionDefinition,
                    pendingRecruitActionSourceInstanceId,
                    card))
            {
                return false;
            }

            var targetSpec = pendingRecruitActionDefinition.TargetSpec;
            var targetIndex = -1;
            var targetZone = TargetZone.Unspecified;
            if (targetSpec == RecruitActionTargetSpec.TavernMinion)
            {
                targetIndex = service.State.Player.Tavern.Shop.FindIndex(item =>
                    item != null && string.Equals(item.InstanceId, card.InstanceId, StringComparison.Ordinal));
                targetZone = TargetZone.TavernShop;
            }
            else if (targetSpec == RecruitActionTargetSpec.FriendlyBoardMinion ||
                     targetSpec == RecruitActionTargetSpec.OtherFriendlyBoardMinion)
            {
                targetIndex = service.State.Player.Board.FindIndex(item =>
                    item != null && string.Equals(item.InstanceId, card.InstanceId, StringComparison.Ordinal));
                targetZone = TargetZone.FriendlyBoard;
                if (targetSpec == RecruitActionTargetSpec.OtherFriendlyBoardMinion &&
                    string.Equals(card.InstanceId, pendingRecruitActionSourceInstanceId, StringComparison.Ordinal))
                {
                    targetIndex = -1;
                }
            }

            if (targetIndex < 0)
            {
                return false;
            }

            request = new RecruitActionRequest
            {
                ActionId = pendingRecruitActionDefinition.ActionId,
                SourceInstanceId = pendingRecruitActionSourceInstanceId,
                TargetIndex = targetIndex,
                TargetZone = targetZone,
                TargetInstanceId = card.InstanceId
            };
            return true;
        }

        private string RecruitActionTargetInvalidReason(RecruitActionTargetSpec targetSpec)
        {
            return targetSpec == RecruitActionTargetSpec.TavernMinion
                ? T("该激活只能选择酒馆中的随从，不是己方战队随从。", "This Activate targets a Tavern minion, not your warband.")
                : T("该激活只能选择合法的己方战队随从。", "This Activate requires a legal friendly warband minion.");
        }

        private bool TryHandleTargetedCardClick(MinionInstance card)
        {
            if (!IsExplicitTargetingDrag())
            {
                return false;
            }

            if (IsSelectingBoardInsertForTargetedMinion())
            {
                return true;
            }

            if (TryResolveBoardDropTarget(card, out var target, out var targetIndex))
            {
                HandleDrop(target, targetIndex);
                return true;
            }

            return false;
        }

        private bool TryResolveBoardDropTarget(MinionInstance card, out UnityTavernDropTarget target, out int targetIndex)
        {
            target = UnityTavernDropTarget.PlayerBoard;
            targetIndex = -1;
            if (card == null)
            {
                return false;
            }

            targetIndex = service.State.Player.Board.FindIndex(item => string.Equals(item.InstanceId, card.InstanceId, StringComparison.OrdinalIgnoreCase));
            if (targetIndex >= 0)
            {
                target = UnityTavernDropTarget.PlayerBoard;
                return true;
            }

            targetIndex = service.State.Opponent.Board.FindIndex(item => string.Equals(item.InstanceId, card.InstanceId, StringComparison.OrdinalIgnoreCase));
            if (targetIndex >= 0)
            {
                target = UnityTavernDropTarget.OpponentBoard;
                return true;
            }

            targetIndex = service.State.Player.Tavern.Shop.FindIndex(item => item != null && string.Equals(item.InstanceId, card.InstanceId, StringComparison.OrdinalIgnoreCase));
            if (targetIndex >= 0)
            {
                target = UnityTavernDropTarget.TavernShop;
                return true;
            }

            return false;
        }

        private void BuyCard(MinionInstance card)
        {
            if (card == null)
            {
                return;
            }

            var index = service.State.Player.Tavern.Shop.FindIndex(item => item != null && item.InstanceId == card.InstanceId);
            if (index >= 0)
            {
                Apply(new GameCommand(GameCommandType.BuyMinion, index));
            }
        }

        private void PlayCard(MinionInstance card)
        {
            var index = service.State.Player.Tavern.Hand.FindIndex(item => item.InstanceId == card.InstanceId);
            if (index >= 0)
            {
                if (ChooseOneOptionRegistry.TryGetOptions(card.CardId, out _))
                {
                    BeginHandChooseOne(card, index);
                    return;
                }

                if (service.RequiresPlayerTarget(card))
                {
                    BeginDrag(card, UnityTavernDragSource.Hand, index);
                    return;
                }

                if (UnityTavernDragController.IsMagnetic(card))
                {
                    BeginDrag(card, UnityTavernDragSource.Hand, index);
                    return;
                }

                if (card.CardKind == CardKind.Minion)
                {
                    Apply(new GameCommand(
                        GameCommandType.PlayMinion,
                        index,
                        PlayIntent.Place,
                        boardInsertIndex: service.State.Player.Board.Count));
                    return;
                }

                Apply(new GameCommand(GameCommandType.PlayMinion, index));
            }
        }

        private void RemoveOpponentHandCard(MinionInstance card)
        {
            var hand = service.State.Opponent.Hand;
            var index = hand.FindIndex(item => item.InstanceId == card.InstanceId);
            if (index >= 0)
            {
                Apply(new GameCommand(GameCommandType.RemoveHandCard, BoardSide.Opponent, index));
            }
        }

        private void SellCard(MinionInstance card)
        {
            if (card != null)
            {
                Apply(new GameCommand(GameCommandType.SellMinion, card.InstanceId));
            }
        }

        private void Apply(GameCommand command)
        {
            TryApply(command);
            Rebuild();
        }

        private MatchState ExecuteCommand(GameCommand command)
        {
            return strategyGuideSession != null
                ? strategyGuideSession.Apply(command)
                : service.Apply(command);
        }

        private bool TryApply(GameCommand command)
        {
            try
            {
                lastError = null;
                ExecuteCommand(command);
                lastFeedback = FeedbackForCommand(command);
                selectedInstanceId = FindSelectedCard()?.InstanceId ?? service.State.Player.Tavern.Shop.FirstOrDefault(card => card != null)?.InstanceId;
                return true;
            }
            catch (Exception ex)
            {
                lastError = service.LocalizePlayerMessage(ex.Message);
                lastFeedback = null;
                return false;
            }
        }

        private void ApplyHeroSelection(HeroDefinition hero)
        {
            if (hero == null || string.IsNullOrEmpty(hero.HeroCardId))
            {
                return;
            }

            try
            {
                lastError = null;
                ExecuteCommand(new GameCommand(GameCommandType.AddCardToHand, hero.HeroCardId, CardKind.Hero));
                lastFeedback = "已更换为 " + hero.Name;
                selectedInstanceId = FindSelectedCard()?.InstanceId ?? service.State.Player.Tavern.Shop.FirstOrDefault(card => card != null)?.InstanceId;
                heroSelectionOpen = false;
                toolsOpen = false;
                cardLibraryOpen = false;
            }
            catch (Exception ex)
            {
                lastError = service.LocalizePlayerMessage(ex.Message);
                lastFeedback = null;
            }

            Rebuild();
        }

        private void ApplyAndOpenReplay(GameCommand command)
        {
            try
            {
                lastError = null;
                ExecuteCommand(command);
                lastFeedback = FeedbackForCommand(command);
                selectedInstanceId = FindSelectedCard()?.InstanceId ?? service.State.Player.Tavern.Shop.FirstOrDefault(card => card != null)?.InstanceId;
                if (service.State.LastReplay != null)
                {
                    combatReplayOpen = true;
                    combatTimelineOpen = false;
                    replayWasPlayingBeforeTimeline = false;
                    combatRunStats = null;
                    toolsOpen = false;
                    cardDetailOpen = false;
                    activeReplayFrameIndex = 0;
                    replayPlaying = service.State.LastReplay.Frames != null && service.State.LastReplay.Frames.Count > 1;
                    replayPlaybackElapsed = 0f;
                }
            }
            catch (Exception ex)
            {
                lastError = service.LocalizePlayerMessage(ex.Message);
                lastFeedback = null;
            }

            Rebuild();
        }

        private string FeedbackForCommand(GameCommand command)
        {
            switch (command.Type)
            {
                case GameCommandType.BuyMinion:
                    return T("已购买一张牌", "Card purchased");
                case GameCommandType.BuyTimewarpedTavernCard:
                    return T("已购买时空酒馆卡牌", "Timewarped Tavern card purchased");
                case GameCommandType.ExitTimewarpedTavern:
                    return T("已退出时空酒馆", "Exited the Timewarped Tavern");
                case GameCommandType.SellMinion:
                    return T("已出售随从", "Minion sold");
                case GameCommandType.RerollShop:
                    return T("已刷新酒馆", "Tavern refreshed");
                case GameCommandType.FreezeShop:
                    return command.Flag ? T("已冻结酒馆", "Tavern frozen") : T("已解冻酒馆", "Tavern unfrozen");
                case GameCommandType.UpgradeTavern:
                    return T("酒馆已升级", "Tavern upgraded");
                case GameCommandType.MoveMinion:
                    return T("已回手", "Minion returned to hand");
                case GameCommandType.MoveBoardMinion:
                    return T("已调整站位", "Board order updated");
                case GameCommandType.MoveShopCard:
                    return T("已调整酒馆位置", "Tavern order updated");
                case GameCommandType.UpdateMinion:
                case GameCommandType.UpdateOpponentMinion:
                    return T("已更新随从", "Minion updated");
                case GameCommandType.PlayMinion:
                    return T("已打出手牌", "Card played");
                case GameCommandType.UseHeroPower:
                    return T("英雄技能已使用", "Hero Power used");
                case GameCommandType.UseNormalDarkGift:
                    return T("已开启黑暗之赐选择", "Dark Gift choice opened");
                case GameCommandType.ChooseDiscover:
                    return T("已选择发现奖励", "Discover reward chosen");
                case GameCommandType.ChooseMechanicOption:
                    return T("已选择进阶机制", "Advanced mechanic selected");
                case GameCommandType.DebugCompleteQuest:
                    return T("任务已完成", "Quest completed");
                case GameCommandType.DebugReplaceQuestReward:
                    return T("任务奖励已替换", "Quest reward replaced");
                case GameCommandType.DebugReplaceTrinket:
                    return T("饰品已替换", "Trinket replaced");
                case GameCommandType.SetOpponentQuestReward:
                    return T("已配置对手任务奖励", "Opponent quest reward configured");
                case GameCommandType.ClearOpponentQuestReward:
                    return T("已清除对手任务奖励", "Opponent quest reward cleared");
                case GameCommandType.SetOpponentTrinket:
                    return T("已配置对手饰品", "Opponent trinket configured");
                case GameCommandType.ClearOpponentTrinket:
                    return T("已清除对手饰品", "Opponent trinket cleared");
                case GameCommandType.SetOpponentHeroPower:
                    return T("已配置对手英雄技能", "Opponent Hero Power configured");
                case GameCommandType.ClearOpponentHeroPower:
                    return T("已清除对手英雄技能", "Opponent Hero Power cleared");
                case GameCommandType.SetOpponentHeroPowerTarget:
                    return T("已配置对手英雄技能目标", "Opponent Hero Power target configured");
                case GameCommandType.ClearOpponentHeroPowerTarget:
                    return T("已清除对手英雄技能目标", "Opponent Hero Power target cleared");
                case GameCommandType.SetOpponentStartOfCombatSpell:
                    return T("已配置敌方战斗开始法术", "Opponent start-of-combat spell configured");
            case GameCommandType.NextTurn:
                return T("进入下一回合", "Entered the next turn");
            case GameCommandType.BeginNextTurnTransition:
                return T("进入战斗", "Entered combat");
            case GameCommandType.ContinueNextTurnTransition:
                return T("已离开战斗界面", "Left the combat view");
                case GameCommandType.DebugAddGold:
                    return T("已增加金币", "Gold added");
                case GameCommandType.SimulateCombat:
                    return T("已完成战斗并进入下一回合", "Combat completed and next turn started");
                case GameCommandType.AddCardToHand:
                    return T("已加入手牌", "Card added to hand");
                case GameCommandType.DebugCastCard:
                    return T("已施放法术", "Spell cast");
                case GameCommandType.UseGuideShapingSpell:
                    return T("塑造法术已触发；本回合次数已使用。", "Shaping spell triggered; this round's use is spent.");
                case GameCommandType.AddOpponentMinion:
                    return T("已加入对手随从", "Opponent minion added");
                case GameCommandType.RemoveOpponentMinion:
                    return T("已移除对手随从", "Opponent minion removed");
                case GameCommandType.MoveOpponentMinion:
                    return T("已调整对手站位", "Opponent board order updated");
                case GameCommandType.ClearOpponentBoard:
                    return T("已清空对手战场", "Opponent board cleared");
                case GameCommandType.CopyPlayerBoardToOpponent:
                    return T("已复制到对手战场", "Copied to opponent board");
                case GameCommandType.MirrorPlayerBoardToOpponent:
                    return T("已镜像到对手战场", "Mirrored to opponent board");
                case GameCommandType.SaveTestScenario:
                    return T("已保存测试场景", "Test scenario saved");
                case GameCommandType.LoadTestScenario:
                    return T("已加载测试场景", "Test scenario loaded");
                case GameCommandType.RunCombatTest:
                    return T("战斗测试已运行", "Combat test run");
                case GameCommandType.ResetCombatTestSnapshot:
                    return T("已重置战斗快照", "Combat snapshot reset");
                default:
                    return T("操作已完成", "Action completed");
            }
        }

        private MinionInstance FindSelectedCard()
        {
            return AllCards().FirstOrDefault(card => card.InstanceId == selectedInstanceId) ?? AllCards().FirstOrDefault();
        }

        private IEnumerable<MinionInstance> AllCards()
        {
            if (service.State.Player.Tavern.Timewarp?.VisitOpen == true)
            {
                foreach (var card in service.GetTimewarpedOfferCards())
                {
                    if (card != null)
                    {
                        yield return card;
                    }
                }
            }

            foreach (var card in service.State.Player.Tavern.Shop)
            {
                if (card != null)
                {
                    yield return card;
                }
            }

            foreach (var card in service.State.Player.Tavern.Hand)
            {
                if (card != null)
                {
                    yield return card;
                }
            }

            foreach (var card in service.State.Opponent.Hand)
            {
                if (card != null)
                {
                    yield return card;
                }
            }

            foreach (var card in service.State.Player.Board)
            {
                if (card != null)
                {
                    yield return card;
                }
            }

            foreach (var card in service.State.Opponent.Board)
            {
                if (card != null)
                {
                    yield return card;
                }
            }
        }

        private UnityTavernZoneComponent Zone(
            string name,
            Transform parent,
            UnityTavernLayoutContext layout,
            UnityTavernZoneKind kind,
            UnityTavernCardMode cardMode)
        {
            var zoneObject = UnityTavernZoneComponent.CreateZoneHost(kind, parent, name);
            var preferredHeight = kind == UnityTavernZoneKind.Hand &&
                                  string.Equals(name, "UnityHandZone", StringComparison.Ordinal)
                ? layout.HandZoneHeight(service.State.Player.Tavern.Hand.Count + service.GetCurrentGuideShapingSpells().Count)
                : layout.ZoneMetrics(kind, cardMode).Height;
            UnityTavernUiStyle.SetPreferredHeight(zoneObject, preferredHeight);
            UnityTavernUiStyle.SetFlexible(
                zoneObject,
                1f,
                kind == UnityTavernZoneKind.PlayerBoard ? 1f : kind == UnityTavernZoneKind.Shop ? 0.45f : 0f);
            return zoneObject.GetComponent<UnityTavernZoneComponent>();
        }

        private void AddDrag(GameObject target, MinionInstance card, UnityTavernDragSource source, int index)
        {
            if (target == null || card == null)
            {
                return;
            }

            var drag = UnityTavernUiStyle.EnsureComponent<UnityTavernCardDragBehaviour>(target);
            drag.Initialize(this, card, source, index);
        }

        private void AddHeroPowerDrag(GameObject target, HeroPowerDefinition heroPower)
        {
            if (target == null || heroPower == null)
            {
                return;
            }

            var card = CurrentHeroPowerDragCard(heroPower);
            if (!UnityTavernDragController.IsDirectUseHeroPower(card))
            {
                AddDrag(target, card, UnityTavernDragSource.HeroPower, 0);
            }
        }

        private MinionInstance CurrentHeroPowerDragCard(HeroPowerDefinition heroPower = null)
        {
            var power = heroPower ?? CurrentHeroPower();
            if (power == null)
            {
                return null;
            }

            return new MinionInstance
            {
                CardKind = CardKind.HeroPower,
                InstanceId = HeroPowerDragInstanceId + "-" + power.CardId,
                DefinitionId = power.CardId,
                CardId = power.CardId,
                Name = power.Name,
                Cost = Math.Max(0, power.Cost),
                Text = power.Text,
                Owner = BoardSide.Player
            };
        }

        private void ConfigureDraggableCard(GameObject target, MinionInstance card, UnityTavernDragSource source, int index)
        {
            ConfigureCardFeedback(target, card);
            if (CanConfigureDrag(source))
            {
                AddDrag(target, card, source, index);
            }
        }

        private void ConfigureBoardCardInteractions(GameObject target, MinionInstance card)
        {
            if (target == null || card == null || card.CardKind == CardKind.TavernSpell)
            {
                return;
            }

            var component = target.GetComponent<UnityTavernCardComponent>();
            if (component == null)
            {
                return;
            }

            component.ConfigureInteractionCallbacks(HandleBoardCardContextAction, HandleTargetingCardHoverStart, HandleTargetingCardHoverEnd);
        }

        private void HandleBoardCardContextAction(MinionInstance card)
        {
            if (HasActiveTargetingSession() || lastTargetingCancelFrame == UnityEngine.Time.frameCount)
            {
                if (HasActiveTargetingSession())
                {
                    CancelCurrentTargeting();
                }

                return;
            }

            OpenMinionEditor(card);
        }

        private void HandleTargetingCardHoverStart(MinionInstance card, RectTransform anchor)
        {
            if (!HasActiveTargetingSession())
            {
                ShowKeywordTooltip(card, anchor);
                return;
            }

            HideKeywordTooltip(card);
            if (!TryResolveBoardDropTarget(card, out _, out _))
            {
                return;
            }

            targetingHoverAnchor = anchor;
            targetingHoverAllowed = IsActiveTarget(card);
            if (!targetingPointerAvailable && anchor != null && transform is RectTransform root)
            {
                targetingPointerLocalPosition = RectTransformUtility.CalculateRelativeRectTransformBounds(root, anchor).center;
                targetingPointerAvailable = true;
            }

            RefreshTargetingConnector();
        }

        private void HandleTargetingCardHoverEnd(MinionInstance card)
        {
            HideKeywordTooltip(card);
            ClearTargetingHover();
        }

        private void ConfigureCardHoverTooltip(GameObject target, MinionInstance card)
        {
            if (target == null || card == null)
            {
                return;
            }

            var component = target.GetComponent<UnityTavernCardComponent>();
            if (component != null)
            {
                component.ConfigureInteractionCallbacks(null, HandleTargetingCardHoverStart, HandleTargetingCardHoverEnd);
            }
        }

        private void ShowKeywordTooltip(MinionInstance card, RectTransform anchor)
        {
            HideKeywordTooltip(card);
            if (card == null)
            {
                return;
            }

            var keywords = EffectiveKeywords(card);
            var description = TooltipDescription(card);
            if (keywords.Count == 0 && string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            keywordTooltip = Panel("UnityKeywordTooltip", transform, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.98f));
            keywordTooltip.transform.SetAsLastSibling();
            var image = keywordTooltip.GetComponent<Image>();
            image.raycastTarget = false;
            UnityTavernUiStyle.ConfigureOutline(keywordTooltip, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.58f), new Vector2(1f, -1f));

            var rect = keywordTooltip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            var descriptionHeight = string.IsNullOrWhiteSpace(description) ? 0f : 94f;
            var keywordHeight = keywords.Count == 0 ? 0f : 28f + keywords.Count * 40f;
            rect.sizeDelta = new Vector2(320f, Mathf.Min(300f, 24f + descriptionHeight + keywordHeight));
            rect.anchoredPosition = TooltipPosition(anchor);

            var layout = keywordTooltip.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 5;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            if (!string.IsNullOrWhiteSpace(description))
            {
                var descriptionTitle = UiFactory.Label(
                    "UnityKeywordTooltipDescriptionTitle",
                    keywordTooltip.transform,
                    card.CardKind == CardKind.TavernSpell || card.CardKind == CardKind.Spell ? "卡牌描述" : "随从描述",
                    14,
                    FontStyle.Bold);
                descriptionTitle.color = UnityTavernUiStyle.Gold;
                UnityTavernUiStyle.SetPreferredHeight(descriptionTitle.gameObject, 22f);

                var descriptionLine = UiFactory.Label("UnityKeywordTooltipDescription", keywordTooltip.transform, description, 14, FontStyle.Normal);
                descriptionLine.color = UnityTavernUiStyle.Text;
                descriptionLine.alignment = TextAnchor.UpperLeft;
                descriptionLine.horizontalOverflow = HorizontalWrapMode.Wrap;
                descriptionLine.verticalOverflow = VerticalWrapMode.Truncate;
                UnityTavernUiStyle.SetPreferredHeight(descriptionLine.gameObject, 66f);
            }

            if (keywords.Count > 0)
            {
                var title = UiFactory.Label("UnityKeywordTooltipTitle", keywordTooltip.transform, "关键词", 14, FontStyle.Bold);
                title.color = UnityTavernUiStyle.Gold;
                UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 22f);

                foreach (var keyword in keywords)
                {
                    var line = UiFactory.Label("UnityKeywordTooltipLine-" + keyword, keywordTooltip.transform, KeywordName(keyword) + "：" + KeywordDescription(keyword), 14, FontStyle.Normal);
                    line.color = UnityTavernUiStyle.Text;
                    line.alignment = TextAnchor.MiddleLeft;
                    UnityTavernUiStyle.SetPreferredHeight(line.gameObject, 36f);
                }
            }
        }

        private string TooltipDescription(MinionInstance card)
        {
            var text = DisplayCardText(card);
            return string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.Replace("[x]", string.Empty).Replace("\r", string.Empty).Trim();
        }

        private void HideKeywordTooltip(MinionInstance card)
        {
            if (keywordTooltip == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(keywordTooltip);
            }
            else
            {
                DestroyImmediate(keywordTooltip);
            }

            keywordTooltip = null;
        }

        private Vector2 TooltipPosition(RectTransform anchor)
        {
            var root = transform as RectTransform;
            if (root == null || anchor == null)
            {
                return new Vector2(32f, -92f);
            }

            var local = root.InverseTransformPoint(anchor.position);
            return new Vector2(local.x + 44f, local.y + 78f);
        }

        private void ConfigureCardFeedback(GameObject target, MinionInstance card)
        {
            if (target == null || card == null)
            {
                return;
            }

            var component = target.GetComponent<UnityTavernCardComponent>();
            if (component != null)
            {
                component.SetSelected(card.InstanceId == selectedInstanceId);
            }
        }

        private static List<Keyword> EffectiveKeywords(MinionInstance card)
        {
            if (card == null)
            {
                return new List<Keyword>();
            }

            if (card.Keywords != null && card.Keywords.Count > 0)
            {
                return card.Keywords.Distinct().ToList();
            }

            return card.OfficialKeywords == null ? new List<Keyword>() : card.OfficialKeywords.Distinct().ToList();
        }

        private static string KeywordName(Keyword keyword)
        {
            switch (keyword)
            {
                case Keyword.Taunt: return "嘲讽";
                case Keyword.DivineShield: return "圣盾";
                case Keyword.Poisonous: return "剧毒";
                case Keyword.Venomous: return "烈毒";
                case Keyword.Reborn: return "复生";
                case Keyword.Deathrattle: return "亡语";
                case Keyword.Battlecry: return "战吼";
                case Keyword.Windfury: return "风怒";
                case Keyword.Cleave: return "顺劈";
                case Keyword.Magnetic: return "磁力";
                case Keyword.Avenge: return "复仇";
                case Keyword.StartOfCombat: return "战斗开始";
                case Keyword.EndOfTurn: return "回合结束";
                case Keyword.Rally: return "进击";
                case Keyword.Spellcraft: return "塑造法术";
                case Keyword.BloodGem: return "鲜血宝石";
                case Keyword.Discover: return "发现";
                case Keyword.Stealth: return "潜行";
                default: return keyword.ToString();
            }
        }

        private static string KeywordDescription(Keyword keyword)
        {
            switch (keyword)
            {
                case Keyword.Taunt: return "敌人必须优先攻击该随从。";
                case Keyword.DivineShield: return "第一次受到伤害时免疫该伤害。";
                case Keyword.Poisonous: return "造成伤害时消灭目标。";
                case Keyword.Venomous: return "造成伤害后消灭目标，通常为一次性效果。";
                case Keyword.Reborn: return "死亡后以 1 点生命值复活一次。";
                case Keyword.Deathrattle: return "死亡时触发效果。";
                case Keyword.Battlecry: return "从手牌打出时触发效果。";
                case Keyword.Windfury: return "可以额外攻击一次。";
                case Keyword.Cleave: return "攻击时同时伤害目标相邻随从。";
                case Keyword.Magnetic: return "打出时可与机械合体。";
                case Keyword.Avenge: return "友方随从死亡达到次数后触发。";
                case Keyword.StartOfCombat: return "战斗开始时触发。";
                case Keyword.EndOfTurn: return "回合结束时触发。";
                case Keyword.Rally: return "满足进击条件时触发。";
                case Keyword.Spellcraft: return "每回合获得临时塑造法术。";
                case Keyword.BloodGem: return "可提供身材增益的鲜血宝石。";
                case Keyword.Discover: return "从多个选项中选择一个。";
                case Keyword.Stealth: return "未攻击前不易被普通攻击指定。";
                default: return "当前随从拥有该关键词。";
            }
        }

        private void RefreshCardSelection()
        {
            var cards = GetComponentsInChildren<UnityTavernCardComponent>(true);
            for (var index = 0; index < cards.Length; index += 1)
            {
                var component = cards[index];
                if (component.Card != null)
                {
                    component.SetSelected(component.Card.InstanceId == selectedInstanceId);
                }
            }
        }

        private void RefreshTargetingClarity()
        {
            var cards = GetComponentsInChildren<UnityTavernCardComponent>(true);
            for (var index = 0; index < cards.Length; index += 1)
            {
                var component = cards[index];
                var card = component.Card;
                var state = UnityTavernTargetingState.None;
                if (card != null && HasActiveTargetingSession())
                {
                    var sourceInstanceId = IsDiscoverOptionTargeting()
                        ? pendingDiscoverTargetSourceInstanceId
                        : IsRecruitActionTargeting()
                            ? pendingRecruitActionSourceInstanceId
                            : IsHandChooseOneTargeting()
                                ? HandChooseOneTargetingSourceInstanceId()
                                : activeDrag?.Card?.InstanceId;
                    if (string.Equals(card.InstanceId, sourceInstanceId, StringComparison.OrdinalIgnoreCase))
                    {
                        state = UnityTavernTargetingState.Source;
                    }
                    else if (string.Equals(card.InstanceId, pendingPrimaryTargetInstanceId, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(card.InstanceId, pendingSecondaryTargetInstanceId, StringComparison.OrdinalIgnoreCase))
                    {
                        state = UnityTavernTargetingState.ConfirmedTarget;
                    }
                    else if (IsActiveTarget(card))
                    {
                        state = UnityTavernTargetingState.Candidate;
                    }
                    else if (IsTargetingCandidateSurface(card))
                    {
                        state = UnityTavernTargetingState.InvalidTarget;
                    }
                }
                else if (card != null &&
                         (string.Equals(card.InstanceId, confirmedTargetInstanceId, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(card.InstanceId, confirmedSecondaryTargetInstanceId, StringComparison.OrdinalIgnoreCase)))
                {
                    state = UnityTavernTargetingState.ConfirmedTarget;
                }
                else if (card != null && IsSavedOpponentHeroPowerTarget(card))
                {
                    state = UnityTavernTargetingState.OpponentTarget;
                }

                string labelOverride = null;
                if (state == UnityTavernTargetingState.ConfirmedTarget &&
                    string.Equals(card?.InstanceId, pendingPrimaryTargetInstanceId, StringComparison.OrdinalIgnoreCase))
                {
                    labelOverride = IsRecruitActionTargeting()
                        ? T("战吼来源已锁定", "Battlecry source locked")
                        : UnityTavernDragController.RequiresTwoTargets(activeDrag?.Card)
                        ? T("目标 1 已锁定", "Target 1 locked")
                        : T("效果目标", "Effect target");
                }
                else if (state == UnityTavernTargetingState.ConfirmedTarget &&
                         string.Equals(card?.InstanceId, pendingSecondaryTargetInstanceId, StringComparison.OrdinalIgnoreCase))
                {
                    labelOverride = T("目标 2 已锁定", "Target 2 locked");
                }
                component.SetTargetingState(state, labelOverride);
            }

            RefreshHeroPowerSourceMarkers();
            RefreshTargetingConnector();
            RefreshTargetingCancelButton();
        }

        private void RefreshTargetingCancelButton()
        {
            var existing = transform.Find("UnityTargetingCancelButton");
            if (!ShouldShowDragCancelButton())
            {
                if (existing != null)
                {
                    if (UnityEngine.Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(existing.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    }
                }

                return;
            }

            if (existing != null)
            {
                existing.SetAsLastSibling();
                return;
            }

            var layout = LayoutContext();
            var button = ActionButton(
                "UnityTargetingCancelButton",
                transform,
                HasActiveTargetingSession()
                    ? T("取消选择目标", "Cancel target")
                    : T("取消操作", "Cancel action"),
                () => CancelCurrentTargeting(),
                layout.IsCompact ? 132f : 148f,
                layout.IsCompact ? UnityTavernUiStyle.CompactTouchHeight : UnityTavernUiStyle.TouchHeight,
                false,
                UnityTavernActionButtonRole.Utility);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -12f);
            rect.sizeDelta = new Vector2(layout.IsCompact ? 132f : 148f, layout.IsCompact ? UnityTavernUiStyle.CompactTouchHeight : UnityTavernUiStyle.TouchHeight);
            rect.SetAsLastSibling();
        }

        private bool ShouldShowDragCancelButton()
        {
            if (!HasActiveTargetingSession() || IsFirstTargetLocked())
            {
                return false;
            }

            return IsDiscoverOptionTargeting() ||
                   IsRecruitActionTargeting() ||
                   activeDrag != null &&
                   (activeDrag.Source == UnityTavernDragSource.HeroPower ||
                    activeDrag.Source == UnityTavernDragSource.GuideShapingSpell ||
                    activeDrag.Source == UnityTavernDragSource.Hand &&
                    (activeDrag.RequiresPlayerTarget || activeDrag.Card?.CardKind == CardKind.Minion));
        }

        private bool IsActiveTarget(MinionInstance card)
        {
            if (card == null)
            {
                return false;
            }

            if (IsDiscoverOptionTargeting())
            {
                var discoverTargetIndex = service.State.Player.Board.FindIndex(item =>
                    item != null && string.Equals(item.InstanceId, card.InstanceId, StringComparison.Ordinal));
                return service.TryValidateDiscoverOptionTarget(
                    pendingDiscoverTargetOptionIndex,
                    discoverTargetIndex,
                    TargetZone.FriendlyBoard,
                    card.InstanceId,
                    out _);
            }

            if (IsRecruitActionTargeting())
            {
                return TryBuildRecruitActionTargetRequest(card, out _);
            }

            if (!IsExplicitTargetingDrag() || IsSelectingBoardInsertForTargetedMinion())
            {
                return false;
            }

            if (!TryResolveBoardDropTarget(card, out var target, out var targetIndex))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(pendingPrimaryTargetInstanceId) &&
                string.Equals(card.InstanceId, pendingPrimaryTargetInstanceId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (activeDrag.RequiresPlayerTarget &&
                !TryValidatePlayerCardDrop(activeDrag.Card, target, targetIndex, out _))
            {
                return false;
            }

            return CanDropInCurrentPhase(activeDrag, target, targetIndex);
        }

        private bool TryValidatePlayerCardDrop(
            MinionInstance card,
            UnityTavernDropTarget target,
            int targetIndex,
            out string reason)
        {
            reason = null;
            if (card == null || targetIndex < 0)
            {
                reason = "请选择一个目标。";
                return false;
            }

            if (target != UnityTavernDropTarget.PlayerBoard && target != UnityTavernDropTarget.TavernShop)
            {
                reason = "该效果不能选择此区域。";
                return false;
            }

            var targetZone = target == UnityTavernDropTarget.PlayerBoard ? TargetZone.FriendlyBoard : TargetZone.TavernShop;
            var instanceId = ResolveDropTargetInstanceId(target, targetIndex);
            if (activeDrag?.Source == UnityTavernDragSource.GuideShapingSpell &&
                !string.IsNullOrEmpty(pendingPrimaryTargetInstanceId))
            {
                return service.TryValidateGuideShapingSecondaryTarget(
                    activeDrag.Card.CardId,
                    pendingPrimaryTargetIndex,
                    pendingPrimaryTargetInstanceId,
                    targetIndex,
                    targetZone,
                    instanceId,
                    out reason);
            }

            return service.TryValidatePlayerTarget(card, targetIndex, targetZone, instanceId, out reason);
        }

        private bool IsTargetingCandidateSurface(MinionInstance card)
        {
            if (!HasActiveTargetingSession() || IsSelectingBoardInsertForTargetedMinion() || card == null)
            {
                return false;
            }

            var sourceInstanceId = IsDiscoverOptionTargeting()
                ? pendingDiscoverTargetSourceInstanceId
                : IsRecruitActionTargeting()
                    ? pendingRecruitActionSourceInstanceId
                    : activeDrag?.Card?.InstanceId;
            return !string.Equals(card.InstanceId, sourceInstanceId, StringComparison.OrdinalIgnoreCase) &&
                   TryResolveBoardDropTarget(card, out _, out _);
        }

        private bool IsExplicitTargetingDrag()
        {
            return activeDrag != null &&
                   (activeDrag.Source == UnityTavernDragSource.HeroPower ||
                    activeDrag.Source == UnityTavernDragSource.GuideShapingSpell && activeDrag.RequiresPlayerTarget ||
                    activeDrag.Source == UnityTavernDragSource.Hand &&
                    (activeDrag.RequiresPlayerTarget || UnityTavernDragController.IsMagnetic(activeDrag.Card)));
        }

        private static bool IsPhysicalCardDrag(UnityTavernDragContext drag)
        {
            if (drag?.Card == null)
            {
                return false;
            }

            switch (drag.Source)
            {
                case UnityTavernDragSource.Shop:
                case UnityTavernDragSource.Discover:
                case UnityTavernDragSource.PlayerBoard:
                case UnityTavernDragSource.OpponentBoard:
                    return true;
                case UnityTavernDragSource.Hand:
                    return !drag.RequiresPlayerTarget &&
                           (drag.Card.CardKind == CardKind.TavernSpell ||
                            drag.Card.CardKind == CardKind.Spell ||
                            drag.Card.CardKind == CardKind.Minion && !UnityTavernDragController.IsMagnetic(drag.Card));
                case UnityTavernDragSource.GuideShapingSpell:
                    return !drag.RequiresPlayerTarget;
                default:
                    return false;
            }
        }

        private bool IsRecruitActionTargeting()
        {
            return pendingRecruitActionDefinition != null &&
                   !string.IsNullOrWhiteSpace(pendingRecruitActionSourceInstanceId);
        }

        private bool IsDiscoverOptionTargeting()
        {
            return pendingDiscoverTargetOptionIndex >= 0 &&
                   !string.IsNullOrWhiteSpace(pendingDiscoverTargetSourceInstanceId);
        }

        private bool IsHandChooseOneTargeting()
        {
            return pendingHandChooseOneCard != null &&
                   pendingHandChooseOneOption != null &&
                   pendingHandChooseOneOption.RequiresPlayerTarget &&
                   activeDrag != null;
        }

        private bool HasActiveTargetingSession()
        {
            return IsExplicitTargetingDrag() ||
                   IsRecruitActionTargeting() ||
                   IsDiscoverOptionTargeting();
        }

        private bool IsFirstTargetLocked()
        {
            if (activeDrag == null || string.IsNullOrEmpty(pendingPrimaryTargetInstanceId))
            {
                return false;
            }

            return UnityTavernDragController.RequiresTwoTargets(activeDrag.Card) ||
                   activeDrag.Source == UnityTavernDragSource.GuideShapingSpell;
        }

        private static TargetZone ToTargetZone(UnityTavernDropTarget target)
        {
            switch (target)
            {
                case UnityTavernDropTarget.PlayerBoard:
                    return TargetZone.FriendlyBoard;
                case UnityTavernDropTarget.OpponentBoard:
                    return TargetZone.OpponentBoard;
                case UnityTavernDropTarget.TavernShop:
                    return TargetZone.TavernShop;
                case UnityTavernDropTarget.Hand:
                    return TargetZone.Hand;
                default:
                    return TargetZone.Unspecified;
            }
        }

        private void ClearPendingPrimaryTarget()
        {
            pendingPrimaryTargetIndex = -1;
            pendingPrimaryTargetZone = TargetZone.Unspecified;
            pendingPrimaryTargetInstanceId = null;
            pendingSecondaryTargetIndex = -1;
            pendingSecondaryTargetZone = TargetZone.Unspecified;
            pendingSecondaryTargetInstanceId = null;
        }

        private bool IsSavedOpponentHeroPowerTarget(MinionInstance card)
        {
            if (card == null)
            {
                return false;
            }

            var playerIndex = service.State.Player.Board.FindIndex(item =>
                item != null && string.Equals(item.InstanceId, card.InstanceId, StringComparison.OrdinalIgnoreCase));
            if (playerIndex >= 0)
            {
                return IsOpponentHeroPowerTarget(BoardSide.Player, playerIndex, card);
            }

            var opponentIndex = service.State.Opponent.Board.FindIndex(item =>
                item != null && string.Equals(item.InstanceId, card.InstanceId, StringComparison.OrdinalIgnoreCase));
            return opponentIndex >= 0 && IsOpponentHeroPowerTarget(BoardSide.Opponent, opponentIndex, card);
        }

        private void RefreshHeroPowerSourceMarkers()
        {
            var drags = GetComponentsInChildren<UnityTavernCardDragBehaviour>(true);
            for (var index = 0; index < drags.Length; index += 1)
            {
                var drag = drags[index];
                ClearHeroPowerSourceMarker(drag.transform);
                if (activeDrag == null ||
                    activeDrag.Source != UnityTavernDragSource.HeroPower ||
                    drag.Source != UnityTavernDragSource.HeroPower ||
                    drag.Card == null ||
                    !string.Equals(drag.Card.CardId, activeDrag.Card.CardId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                BuildHeroPowerSourceMarker(drag.transform);
            }
        }

        private static void BuildHeroPowerSourceMarker(Transform parent)
        {
            var marker = new GameObject("UnityTargetingSourceMarker", typeof(RectTransform), typeof(Image));
            marker.transform.SetParent(parent, false);
            UnityTavernUiStyle.Stretch(marker.GetComponent<RectTransform>());
            var image = marker.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            var outline = UnityTavernUiStyle.EnsureComponent<Outline>(marker);
            outline.effectColor = UnityTavernUiStyle.Gold;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;

            var label = UiFactory.Label("UnityTargetingSourceLabel", marker.transform, "英雄技能", 14, FontStyle.Bold);
            label.alignment = TextAnchor.UpperCenter;
            label.color = UnityTavernUiStyle.Gold;
            label.raycastTarget = false;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            UnityTavernUiStyle.ConfigureOutline(label.gameObject, new Color(0f, 0f, 0f, 0.72f), new Vector2(1f, -1f));
        }

        private static void ClearHeroPowerSourceMarker(Transform parent)
        {
            var marker = parent == null ? null : parent.Find("UnityTargetingSourceMarker");
            if (marker == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(marker.gameObject);
            }
            else
            {
                DestroyImmediate(marker.gameObject);
            }
        }

        private void RefreshTargetingConnector()
        {
            if (!HasActiveTargetingSession())
            {
                DestroyTargetingConnector();
                return;
            }

            var root = transform as RectTransform;
            var source = TargetingSourceRect();
            if (root == null || source == null || !targetingPointerAvailable)
            {
                if (targetingConnector != null)
                {
                    targetingConnector.SetActive(false);
                }

                return;
            }

            var sourceBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, source);
            var direction = (targetingPointerLocalPosition - (Vector2)sourceBounds.center).normalized;
            var start = TargetingEdgePoint(sourceBounds, direction);
            var end = targetingPointerLocalPosition;
            var delta = end - start;
            if (delta.sqrMagnitude < 1f)
            {
                if (targetingConnector != null)
                {
                    targetingConnector.SetActive(false);
                }

                return;
            }

            if (targetingConnector == null)
            {
                targetingConnector = new GameObject(
                    "UnityTargetingConnector",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(UnityTavernTargetingRibbonGraphic));
                targetingConnector.transform.SetParent(transform, false);
                var connectorRect = targetingConnector.GetComponent<RectTransform>();
                connectorRect.anchorMin = Vector2.zero;
                connectorRect.anchorMax = Vector2.one;
                connectorRect.pivot = root.pivot;
                connectorRect.offsetMin = Vector2.zero;
                connectorRect.offsetMax = Vector2.zero;
            }

            targetingConnector.SetActive(true);
            targetingConnector.transform.SetAsLastSibling();
            var endpointState = targetingHoverAnchor == null
                ? UnityTavernTargetingEndpointState.Neutral
                : targetingHoverAllowed
                    ? UnityTavernTargetingEndpointState.Valid
                    : UnityTavernTargetingEndpointState.Invalid;
            var layout = LayoutContext();
            targetingConnector.GetComponent<UnityTavernTargetingRibbonGraphic>().SetGeometry(
                start,
                end,
                endpointState,
                layout.CanvasUnitsForPhysicalPixels(22f),
                layout.CanvasUnitsForPhysicalPixels(42f),
                layout.CanvasUnitsForPhysicalPixels(6f));
        }

        private static Vector2 TargetingEdgePoint(Bounds bounds, Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                return bounds.center;
            }

            var xDistance = Mathf.Abs(direction.x) < 0.001f ? float.PositiveInfinity : bounds.extents.x / Mathf.Abs(direction.x);
            var yDistance = Mathf.Abs(direction.y) < 0.001f ? float.PositiveInfinity : bounds.extents.y / Mathf.Abs(direction.y);
            return (Vector2)bounds.center + direction * Mathf.Min(xDistance, yDistance);
        }

        private RectTransform TargetingSourceRect()
        {
            if (IsDiscoverOptionTargeting())
            {
                return EnsureDiscoverTargetingSourceAnchor();
            }

            if (IsHandChooseOneTargeting())
            {
                return EnsureHandChooseOneTargetingSourceAnchor();
            }

            if (IsRecruitActionTargeting())
            {
                return FindCardRect(string.IsNullOrWhiteSpace(pendingPrimaryTargetInstanceId)
                    ? pendingRecruitActionSourceInstanceId
                    : pendingPrimaryTargetInstanceId);
            }

            if (activeDrag?.Source == UnityTavernDragSource.GuideShapingSpell &&
                activeDrag.Card != null &&
                guideShapingSpellSourceAnchors.TryGetValue(activeDrag.Card.CardId, out var shapingSourceAnchor))
            {
                return shapingSourceAnchor;
            }

            var drags = GetComponentsInChildren<UnityTavernCardDragBehaviour>(true);
            for (var index = 0; index < drags.Length; index += 1)
            {
                var drag = drags[index];
                if (drag.Source == UnityTavernDragSource.HeroPower &&
                    drag.Card != null &&
                    string.Equals(drag.Card.CardId, activeDrag.Card.CardId, StringComparison.OrdinalIgnoreCase))
                {
                    return drag.transform as RectTransform;
                }
            }

            var cards = GetComponentsInChildren<UnityTavernCardComponent>(true);
            for (var index = 0; index < cards.Length; index += 1)
            {
                var card = cards[index];
                if (card.Card != null &&
                    string.Equals(card.Card.InstanceId, activeDrag.Card.InstanceId, StringComparison.OrdinalIgnoreCase))
                {
                    return card.transform as RectTransform;
                }
            }

            return null;
        }

        private RectTransform EnsureDiscoverTargetingSourceAnchor()
        {
            var existing = transform.Find("UnityDiscoverTargetingSource");
            if (existing != null)
            {
                return existing as RectTransform;
            }

            if (pendingDiscoverTargetOptionCard == null)
            {
                return FindCardRect(pendingDiscoverTargetSourceInstanceId);
            }

            var cardObject = UnityTavernCardComponent.CreateCardHost(
                UnityTavernCardMode.Detail,
                transform,
                "UnityDiscoverTargetingSource");
            var rect = cardObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(132f, 186f);
            cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                pendingDiscoverTargetOptionCard,
                UnityTavernCardMode.Detail,
                string.Empty,
                null,
                null,
                useEnglish: service.UseEnglish);
            cardObject.name = "UnityDiscoverTargetingSource";
            rect.sizeDelta = new Vector2(132f, 186f);
            cardObject.GetComponent<UnityTavernCardComponent>().SetLayoutScale(0.62f);
            UnityTavernUiStyle.EnsureComponent<CanvasGroup>(cardObject).blocksRaycasts = false;
            return rect;
        }

        private string HandChooseOneTargetingSourceInstanceId()
        {
            return pendingHandChooseOneCard == null || pendingHandChooseOneOption == null
                ? null
                : pendingHandChooseOneCard.InstanceId + ":choice:" + pendingHandChooseOneOption.ChoiceId;
        }

        private RectTransform EnsureHandChooseOneTargetingSourceAnchor()
        {
            var existing = transform.Find("UnityHandChooseOneTargetingSource");
            if (existing != null)
            {
                return existing as RectTransform;
            }

            if (pendingHandChooseOneCard == null || pendingHandChooseOneOption == null)
            {
                return null;
            }

            var optionCard = CreateHandChooseOneOptionCard(pendingHandChooseOneCard, pendingHandChooseOneOption);
            var cardObject = UnityTavernCardComponent.CreateCardHost(
                UnityTavernCardMode.Detail,
                transform,
                "UnityHandChooseOneTargetingSource");
            var rect = cardObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(132f, 186f);
            cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                optionCard,
                UnityTavernCardMode.Detail,
                string.Empty,
                null,
                null,
                useEnglish: service.UseEnglish);
            cardObject.name = "UnityHandChooseOneTargetingSource";
            rect.sizeDelta = new Vector2(132f, 186f);
            cardObject.GetComponent<UnityTavernCardComponent>().SetLayoutScale(0.62f);
            UnityTavernUiStyle.EnsureComponent<CanvasGroup>(cardObject).blocksRaycasts = false;
            return rect;
        }

        private RectTransform FindCardRect(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return null;
            }

            var cards = GetComponentsInChildren<UnityTavernCardComponent>(true);
            for (var index = 0; index < cards.Length; index += 1)
            {
                var card = cards[index];
                if (card.Card != null &&
                    string.Equals(card.Card.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
                {
                    return card.transform as RectTransform;
                }
            }

            return null;
        }

        private void ClearTargetingHover()
        {
            targetingHoverAnchor = null;
            targetingHoverAllowed = false;
            RefreshTargetingConnector();
        }

        private void DestroyTargetingConnector()
        {
            if (targetingConnector == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(targetingConnector);
            }
            else
            {
                DestroyImmediate(targetingConnector);
            }

            targetingConnector = null;
        }

        private void RefreshDropTargetCues()
        {
            var targets = GetComponentsInChildren<UnityTavernDropTargetBehaviour>(true);
            for (var index = 0; index < targets.Length; index += 1)
            {
                var target = targets[index];
                var isPhysicalBoardSurface = string.Equals(
                    target.name,
                    "UnityPlayerBoardPhysicalDropZone",
                    StringComparison.Ordinal);
                var boardInsertSession = activeDrag != null &&
                                         CanDropInCurrentPhase(activeDrag, UnityTavernDropTarget.PlayerBoardInsert, 0);
                if (isPhysicalBoardSurface && !boardInsertSession)
                {
                    target.ClearDropCue();
                    continue;
                }

                target.SetDropCue(activeDrag, CanDropInCurrentPhase(activeDrag, target.Target, target.TargetIndex));
            }
        }

        private void ClearDropTargetCues()
        {
            var targets = GetComponentsInChildren<UnityTavernDropTargetBehaviour>(true);
            for (var index = 0; index < targets.Length; index += 1)
            {
                targets[index].ClearDropCue();
            }
        }

        private void AddDropTarget(
            GameObject target,
            UnityTavernDropTarget dropTarget,
            int targetIndex = -1,
            bool raycastOnlyWhenAllowed = false,
            bool activeOnlyWhenAllowed = false,
            bool cueOnlyWhenAllowed = false,
            bool suppressCueVisuals = false,
            bool resolveIndexFromPointer = false,
            int indexSlotCount = 0)
        {
            if (target == null)
            {
                return;
            }

            var image = UnityTavernUiStyle.EnsureComponent<Image>(target);
            image.raycastTarget = !raycastOnlyWhenAllowed;

            var behaviour = UnityTavernUiStyle.EnsureComponent<UnityTavernDropTargetBehaviour>(target);
            behaviour.Initialize(
                this,
                dropTarget,
                targetIndex,
                raycastOnlyWhenAllowed,
                activeOnlyWhenAllowed,
                cueOnlyWhenAllowed,
                suppressCueVisuals,
                resolveIndexFromPointer,
                indexSlotCount);
        }

        private static GameObject Panel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            panel.GetComponent<Image>().raycastTarget = false;
            return panel;
        }

        private static void ResourcePill(Transform parent, string id, string label, string value, Color color)
        {
            var pill = Panel("UnityResourcePill-" + id, parent, UnityTavernUiStyle.SurfaceRaised);
            UnityTavernUiStyle.SetFixedSize(pill, 96f, 54f);
            UnityTavernUiStyle.ConfigureOutline(pill, new Color(color.r, color.g, color.b, 0.54f), new Vector2(1f, -1f));
            var layout = pill.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 8, 2, 2);
            layout.spacing = 0;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var accent = Panel("UnityResourceAccent", pill.transform, color);
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(accent).ignoreLayout = true;
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = Vector2.zero;
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.sizeDelta = new Vector2(4f, 0f);
            accentRect.anchoredPosition = Vector2.zero;

            var labelText = UiFactory.Label("UnityResourceLabel", pill.transform, label, 14, FontStyle.Bold);
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.verticalOverflow = VerticalWrapMode.Overflow;
            labelText.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(labelText.gameObject, 22f);
            var valueText = UiFactory.Label("UnityResourceValue", pill.transform, value, 18, FontStyle.Bold);
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.verticalOverflow = VerticalWrapMode.Overflow;
            valueText.color = color;
            UnityTavernUiStyle.SetPreferredHeight(valueText.gameObject, 26f);
        }

        private static Button SmallButton(string name, Transform parent, string text, Action onClick, float width)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(buttonObject, width, UnityTavernUiStyle.TouchHeight);
            buttonObject.GetComponent<Image>().color = UnityTavernUiStyle.SurfaceRaised;
            UnityTavernUiStyle.ConfigureOutline(
                buttonObject,
                new Color(UnityTavernUiStyle.Brass.r, UnityTavernUiStyle.Brass.g, UnityTavernUiStyle.Brass.b, 0.42f),
                new Vector2(1f, -1f));
            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                new Color(1f, 0.91f, 0.62f, 1f),
                new Color(0.72f, 0.62f, 0.42f, 1f));

            var label = UiFactory.Label(name + "Text", buttonObject.transform, text, 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return button;
        }

        private static float QuickActionBarHeight(UnityTavernLayoutContext layout)
        {
            return (layout.IsCompact ? UnityTavernUiStyle.CompactTouchHeight : UnityTavernUiStyle.TouchHeight) + UnityTavernUiStyle.SpacingSm * 2f;
        }

        private static Button ActionButton(
            string name,
            Transform parent,
            string text,
            Action onClick,
            float minWidth = 0f,
            float minHeight = 0f,
            bool flexibleWidth = false,
            UnityTavernActionButtonRole role = UnityTavernActionButtonRole.Neutral,
            bool interactable = true)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = ActionButtonSurface(role, interactable);
            var effectiveMinHeight = Mathf.Max(UnityTavernUiStyle.TouchHeight, minHeight);
            if (minWidth > 0f || effectiveMinHeight > 0f || flexibleWidth)
            {
                var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(buttonObject);
                if (minWidth > 0f)
                {
                    element.minWidth = minWidth;
                    element.preferredWidth = minWidth;
                }

                element.minHeight = effectiveMinHeight;
                element.preferredHeight = effectiveMinHeight;

                element.flexibleWidth = flexibleWidth ? 1f : 0f;
            }

            var button = buttonObject.GetComponent<Button>();
            buttonObject.AddComponent<UnitySelectableFocusRing>();
            button.interactable = interactable;
            button.onClick.AddListener(() =>
            {
                if (button.interactable)
                {
                    onClick?.Invoke();
                }
            });
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                ActionButtonHighlight(role, interactable),
                ActionButtonPressed(role, interactable));

            var accent = ActionButtonAccent(role, interactable);
            AddActionButtonAccent(buttonObject, name + "Accent", accent, role != UnityTavernActionButtonRole.Neutral || !interactable);
            UnityTavernUiStyle.ConfigureOutline(
                buttonObject,
                ActionButtonOutline(role, interactable),
                new Vector2(1f, -1f));

            var label = UiFactory.Label(name + "Text", buttonObject.transform, text, 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = interactable ? ActionButtonText(role) : UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(role == UnityTavernActionButtonRole.Neutral && interactable ? 0f : 6f, 0f);
            return button;
        }

        private static void AddActionButtonAccent(GameObject buttonObject, string name, Color color, bool visible)
        {
            var accent = new GameObject(name, typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(buttonObject.transform, false);
            var rect = accent.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(5f, 0f);
            rect.anchoredPosition = Vector2.zero;

            var image = accent.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            accent.SetActive(visible);
        }

        private static Color ActionButtonSurface(UnityTavernActionButtonRole role, bool interactable)
        {
            if (!interactable)
            {
                return new Color(UnityTavernUiStyle.Disabled.r, UnityTavernUiStyle.Disabled.g, UnityTavernUiStyle.Disabled.b, 0.62f);
            }

            switch (role)
            {
                case UnityTavernActionButtonRole.Economy:
                    return Color.Lerp(UnityTavernUiStyle.TableDark, UnityTavernUiStyle.Brass, 0.16f);
                case UnityTavernActionButtonRole.Primary:
                    return Color.Lerp(UnityTavernUiStyle.SurfaceRaised, UnityTavernUiStyle.SuccessGreen, 0.18f);
                case UnityTavernActionButtonRole.Combat:
                    return Color.Lerp(UnityTavernUiStyle.SurfaceDark, UnityTavernUiStyle.CombatRed, 0.34f);
                case UnityTavernActionButtonRole.Utility:
                    return Color.Lerp(UnityTavernUiStyle.SurfaceDark, UnityTavernUiStyle.ArcaneBlue, 0.18f);
                case UnityTavernActionButtonRole.Danger:
                    return Color.Lerp(UnityTavernUiStyle.SurfaceDark, UnityTavernUiStyle.DangerRed, 0.42f);
                default:
                    return UnityTavernUiStyle.PanelRaised;
            }
        }

        private static Color ActionButtonAccent(UnityTavernActionButtonRole role, bool interactable)
        {
            var color = ActionButtonAccent(role);
            return interactable ? color : new Color(color.r, color.g, color.b, 0.34f);
        }

        private static Color ActionButtonAccent(UnityTavernActionButtonRole role)
        {
            switch (role)
            {
                case UnityTavernActionButtonRole.Economy:
                    return UnityTavernUiStyle.Gold;
                case UnityTavernActionButtonRole.Primary:
                    return UnityTavernUiStyle.Green;
                case UnityTavernActionButtonRole.Combat:
                case UnityTavernActionButtonRole.Danger:
                    return UnityTavernUiStyle.Red;
                case UnityTavernActionButtonRole.Utility:
                    return UnityTavernUiStyle.Blue;
                default:
                    return new Color(0f, 0f, 0f, 0.28f);
            }
        }

        private static Color ActionButtonOutline(UnityTavernActionButtonRole role, bool interactable)
        {
            var color = ActionButtonAccent(role);
            return interactable
                ? new Color(color.r, color.g, color.b, role == UnityTavernActionButtonRole.Neutral ? 0.24f : 0.54f)
                : new Color(color.r, color.g, color.b, 0.18f);
        }

        private static Color ActionButtonHighlight(UnityTavernActionButtonRole role, bool interactable)
        {
            var color = ActionButtonAccent(role);
            return interactable
                ? new Color(Mathf.Min(1f, color.r + 0.34f), Mathf.Min(1f, color.g + 0.28f), Mathf.Min(1f, color.b + 0.18f), 1f)
                : new Color(0.62f, 0.62f, 0.62f, 0.42f);
        }

        private static Color ActionButtonPressed(UnityTavernActionButtonRole role, bool interactable)
        {
            var color = ActionButtonAccent(role);
            return interactable
                ? new Color(Mathf.Max(0.12f, color.r * 0.72f), Mathf.Max(0.12f, color.g * 0.72f), Mathf.Max(0.12f, color.b * 0.72f), 1f)
                : new Color(0.42f, 0.42f, 0.42f, 0.42f);
        }

        private static Color ActionButtonText(UnityTavernActionButtonRole role)
        {
            return role == UnityTavernActionButtonRole.Economy ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Text;
        }

        private string HandActionLabel(MinionInstance card)
        {
            if (card == null)
            {
                return null;
            }

            return card.CardKind == CardKind.TavernSpell ? T("施放", "Cast") : T("上场", "Play");
        }

        private string OpponentHandActionLabel(MinionInstance card)
        {
            return card == null ? null : T("删除", "Remove");
        }

        private void ClearChildren()
        {
            for (var index = transform.childCount - 1; index >= 0; index -= 1)
            {
                var child = transform.GetChild(index).gameObject;
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }
    }
}
