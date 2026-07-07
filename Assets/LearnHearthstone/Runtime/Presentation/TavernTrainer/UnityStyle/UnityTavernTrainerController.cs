using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernTrainerController : MonoBehaviour
    {
        private const int BoardLimit = 7;
        private const int HandLimit = 10;
        private const string HeroPowerDragInstanceId = "unity-current-hero-power";
        private static readonly float[] ReplayFrameDurations = { 0.65f, 0.36f, 0.18f };
        private static readonly string[] ReplaySpeedLabels = { "1x", "2x", "4x" };
        private static readonly Regex RichTextTagPattern = new Regex("<.*?>", RegexOptions.Compiled);
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
            OpponentBoard
        }

        private enum AdvancedCardLibrarySelectionKind
        {
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

        private MatchService service;
        private IAdvisorService advisor;
        private Action backToHub;
        private string selectedInstanceId;
        private string lastError;
        private string lastFeedback;
        private UnityTavernDragContext activeDrag;
        private GameObject dragGhost;
        private bool rightPanelOpen;
        private UnityTavernInspectorTab activeInspectorTab = UnityTavernInspectorTab.Actions;
        private bool cardDetailOpen;
        private bool combatReplayOpen;
        private bool toolsOpen;
        private bool cardLibraryOpen;
        private bool heroSelectionOpen;
        private string minionEditorInstanceId;
        private BoardSide minionEditorSide;
        private GameObject keywordTooltip;
        private int activeReplayFrameIndex;
        private bool replayPlaying;
        private float replayPlaybackElapsed;
        private int replaySpeedIndex;
        private CardKind toolsAcquisitionKind = CardKind.Minion;
        private int toolsAcquisitionTierFilter;
        private Tribe toolsAcquisitionTribeFilter = Tribe.All;
        private UnityCardLibraryDestination cardLibraryDestination = UnityCardLibraryDestination.PlayerHand;
        private bool opponentCardLibraryGolden;
        private bool toolsShowAllCards;
        private HeroPowerCategory? toolsHeroPowerCategoryFilter;
        private HeroPowerReplacementEligibility? toolsHeroPowerEligibilityFilter;
        private bool advancedCardLibraryOpen;
        private AdvancedCardLibrarySelectionKind advancedCardLibraryKind = AdvancedCardLibrarySelectionKind.QuestReward;
        private int advancedCardLibraryQuestIndex;
        private bool playerDirectedChoiceOpen;
        private PlayerDirectedChoiceKind playerDirectedChoiceKind = PlayerDirectedChoiceKind.QuestPair;
        private TrinketSlotKind playerDirectedTrinketSlotKind = TrinketSlotKind.Lesser;
        private string playerDirectedSearchText = string.Empty;
        private int playerDirectedSelectableFilter;
        private int playerDirectedCostFilter;
        private string playerDirectedSlotFilter = string.Empty;
        private string playerDirectedTagFilter = string.Empty;

        public void Initialize(MatchService matchService, IAdvisorService advisorService, Action backAction, Action legacyAction)
        {
            service = matchService;
            advisor = advisorService;
            backToHub = backAction;
            selectedInstanceId = service.State.Player.Tavern.Shop.FirstOrDefault(card => card != null)?.InstanceId;
            Rebuild();
        }

        private void Update()
        {
            TickReplayPlayback(UnityEngine.Time.unscaledDeltaTime);
        }

        public void Rebuild()
        {
            ClearChildren();
            keywordTooltip = null;
            BuildBackground();
            BuildTopBar();
            BuildPlaySurface();
            BuildQuestTrackerOverlay();
            BuildTrinketTrackerOverlay();
            BuildAdvancedChoiceStatusPanel();
            BuildQuickActionBar();
            BuildRightPanelDrawerToggle();

            if (rightPanelOpen)
            {
                BuildFloatingRightPanel();
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
            }

            if (advancedCardLibraryOpen)
            {
                BuildAdvancedCardLibraryOverlay();
            }

            if (heroSelectionOpen)
            {
                BuildHeroSelectionOverlay();
            }

            if (!string.IsNullOrEmpty(minionEditorInstanceId))
            {
                BuildMinionEditModal();
            }

            if (service.State.Player.Tavern.Discover != null)
            {
                BuildDiscoverModal();
            }

            if (service.State.Player.Tavern.AdvancedMechanics?.PendingChoice != null)
            {
                BuildAdvancedMechanicChoiceModal();
            }

            if (playerDirectedChoiceOpen)
            {
                BuildPlayerDirectedChoiceModal();
            }

            if (!string.IsNullOrEmpty(lastError))
            {
                BuildErrorToast(lastError);
            }
            else if (!string.IsNullOrEmpty(lastFeedback))
            {
                BuildFeedbackToast(lastFeedback);
            }
        }

        private void BuildBackground()
        {
            var back = Panel("UnityTavernBackWall", transform, UnityTavernUiStyle.BackWall);
            UnityTavernUiStyle.Stretch(back.GetComponent<RectTransform>());

            var table = Panel("UnityTavernTableGlow", transform, new Color(0.45f, 0.26f, 0.12f, 0.28f));
            var tableRect = table.GetComponent<RectTransform>();
            tableRect.anchorMin = new Vector2(0.02f, 0.05f);
            tableRect.anchorMax = new Vector2(0.98f, 0.88f);
            tableRect.offsetMin = Vector2.zero;
            tableRect.offsetMax = Vector2.zero;
        }

        private void BuildTopBar()
        {
            var bar = Panel("UnityTopBar", transform, new Color(0.08f, 0.09f, 0.08f, 0.96f));
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

            var titleBlock = new GameObject("UnityTitleBlock", typeof(RectTransform));
            titleBlock.transform.SetParent(bar.transform, false);
            UnityTavernUiStyle.SetFixedSize(titleBlock, 270f, 54f);
            var titleLayout = titleBlock.AddComponent<VerticalLayoutGroup>();
            titleLayout.spacing = 0;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;

            var title = UiFactory.Label("UnityTitle", titleBlock.transform, "Unity 酒馆桌面", 22, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            UiFactory.Label("UnitySubtitle", titleBlock.transform, "组件式 UGUI / 可继续 prefab 化", 12, FontStyle.Normal).color = UnityTavernUiStyle.MutedText;

            BuildHeroBadge(bar.transform);
            ResourcePill(bar.transform, "回合", service.State.Round.ToString(), UnityTavernUiStyle.TableLit);
            ResourcePill(bar.transform, "金币", service.State.Player.Tavern.Gold + "/" + service.State.Player.Tavern.MaxGold, UnityTavernUiStyle.Gold);
            ResourcePill(bar.transform, "酒馆", service.State.Player.Tavern.Tier + " 本", UnityTavernUiStyle.Blue);
            ResourcePill(bar.transform, "生命", service.State.Player.Health.ToString(), UnityTavernUiStyle.Red);
            ResourcePill(bar.transform, "种族", ActiveLibraryTribes().Count + "/10", UnityTavernUiStyle.Green);

            var spacer = new GameObject("UnityTopBarSpacer", typeof(RectTransform));
            spacer.transform.SetParent(bar.transform, false);
            UnityTavernUiStyle.SetFlexible(spacer, 1f, 0f);

            SmallButton("UnityBackButton", bar.transform, "返回", () => backToHub?.Invoke(), 48f);
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
            UnityTavernUiStyle.ConfigureOutline(badge, new Color(UnityTavernUiStyle.Blue.r, UnityTavernUiStyle.Blue.g, UnityTavernUiStyle.Blue.b, 0.42f), new Vector2(1f, -1f));

            var button = badge.GetComponent<Button>();
            button.onClick.AddListener(OpenHeroSelection);
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

            var name = UiFactory.Label("UnityHeroBadgeName", stack.transform, hero == null ? "未设置" : hero.Name, 12, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 21f);

            var power = UiFactory.Label("UnityHeroBadgePower", stack.transform, CurrentHeroPowerName(), 11, FontStyle.Bold);
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
                var missing = UiFactory.Label("UnityHeroBadgeImageMissing", frame.transform, "无图", 9, FontStyle.Bold);
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
            var layout = UnityTavernLayoutContext.Current();
            var horizontalInset = layout.IsCompact ? UnityTavernUiStyle.SpacingSm : UnityTavernUiStyle.SpacingLg;
            var bottomInset = QuickActionBarHeight(layout) + (layout.IsCompact ? UnityTavernUiStyle.SpacingSm : UnityTavernUiStyle.SpacingLg);
            var topInset = layout.IsCompact ? 84f : 92f;

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

            BuildOpponentBoard(center.transform, layout);
            BuildOpponentHand(center.transform, layout);
            BuildShop(center.transform, layout);
            BuildPlayerBoard(center.transform, layout);
            BuildHand(center.transform, layout);

        }

        private void BuildQuickActionBar()
        {
            var layout = UnityTavernLayoutContext.Current();
            var bar = Panel("UnityQuickActionBar", transform, new Color(0.08f, 0.09f, 0.08f, 0.94f));
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

        private void BuildOpponentBoard(Transform parent, UnityTavernLayoutContext layout)
        {
            var zone = Zone("UnityOpponentBoardZone", parent, layout, UnityTavernZoneKind.OpponentBoard, UnityTavernCardMode.Board);
            zone.Build(
                "对手战场",
                service.State.Opponent.Board.Count + "/7",
                service.State.Opponent.Board,
                BoardLimit,
                UnityTavernCardMode.Board,
                card => null,
                SelectCard,
                null,
                configureCard: (cardObject, card, index) =>
                {
                    ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.OpponentBoard, index);
                    ConfigureBoardCardInteractions(cardObject, card);
                },
                configureSlot: (slot, index) => AddDropTarget(slot, UnityTavernDropTarget.OpponentBoard, index),
                layoutContext: layout);
            BuildBoardReorderDropZone(
                zone.transform,
                "UnityOpponentBoardReorderDropZone",
                UnityTavernDropTarget.OpponentBoard,
                new Color(0.12f, 0.18f, 0.34f, 0.58f),
                "拖到这里调整敌方站位",
                "按左右位置决定落点");
        }

        private void BuildShop(Transform parent, UnityTavernLayoutContext layout)
        {
            var timewarp = service.State.Player.Tavern.Timewarp;
            if (timewarp?.VisitOpen == true)
            {
                var timewarpedZone = Zone("UnityTimewarpedTavernZone", parent, layout, UnityTavernZoneKind.Shop, UnityTavernCardMode.Shop);
                timewarpedZone.Build(
                    TimewarpedTavernTitle(timewarp.PendingKind),
                    "Chronum " + timewarp.Chronum,
                    service.GetTimewarpedOfferCards(),
                    0,
                    UnityTavernCardMode.Shop,
                    TimewarpedOfferActionLabel,
                    SelectCard,
                    BuyTimewarpedOffer,
                    layoutContext: layout);
                ActionButton(
                    "UnityTimewarpedTavernExitButton",
                    timewarpedZone.transform,
                    "退出时空酒馆",
                    () => Apply(new GameCommand(GameCommandType.ExitTimewarpedTavern)),
                    role: UnityTavernActionButtonRole.Utility);
                return;
            }

            var zone = Zone("UnityShopZone", parent, layout, UnityTavernZoneKind.Shop, UnityTavernCardMode.Shop);
            zone.Build(
                "鲍勃的酒馆",
                service.State.Player.Tavern.Frozen ? "已冻结" : "可刷新",
                service.State.Player.Tavern.Shop,
                0,
                UnityTavernCardMode.Shop,
                card => "购买",
                SelectCard,
                BuyCard,
                configureCard: (cardObject, card, index) => ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.Shop, index),
                layoutContext: layout);
            BuildShopSellDropZone(zone.transform);
        }

        private void BuildOpponentHand(Transform parent, UnityTavernLayoutContext layout)
        {
            var hand = service.State.Opponent.Hand ?? new List<MinionInstance>();
            var zone = Zone("UnityOpponentHandZone", parent, layout, UnityTavernZoneKind.Hand, UnityTavernCardMode.Hand);
            zone.Build(
                "对手手牌",
                hand.Count + "/10",
                hand,
                HandLimit,
                UnityTavernCardMode.Hand,
                OpponentHandActionLabel,
                SelectCard,
                RemoveOpponentHandCard,
                layoutContext: layout);
        }

        private static string TimewarpedTavernTitle(TimewarpKind kind)
        {
            return kind == TimewarpKind.Major ? "Major Timewarped Tavern" : "Minor Timewarped Tavern";
        }

        private static string TimewarpedOfferActionLabel(MinionInstance card)
        {
            if (card == null)
            {
                return null;
            }

            if (TimewarpedCardBehavior.IsExitCardInstance(card))
            {
                return "Exit";
            }

            return "购买 " + card.Cost;
        }

        private void BuildPlayerBoard(Transform parent, UnityTavernLayoutContext layout)
        {
            var zone = Zone("UnityPlayerBoardZone", parent, layout, UnityTavernZoneKind.PlayerBoard, UnityTavernCardMode.Board);
            zone.Build(
                "玩家战场",
                service.State.Player.Board.Count + "/7",
                service.State.Player.Board,
                BoardLimit,
                UnityTavernCardMode.Board,
                card => "出售",
                SelectCard,
                SellCard,
                configureCard: (cardObject, card, index) =>
                {
                    ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.PlayerBoard, index);
                    ConfigureBoardCardInteractions(cardObject, card);
                },
                configureSlot: (slot, index) => AddDropTarget(slot, UnityTavernDropTarget.PlayerBoard, index),
                layoutContext: layout);
            BuildBoardReorderDropZone(
                zone.transform,
                "UnityPlayerBoardReorderDropZone",
                UnityTavernDropTarget.PlayerBoard,
                new Color(0.08f, 0.30f, 0.18f, 0.58f),
                "拖到这里调整己方站位",
                "按左右位置决定落点");
        }

        private void BuildHand(Transform parent, UnityTavernLayoutContext layout)
        {
            var zone = Zone("UnityHandZone", parent, layout, UnityTavernZoneKind.Hand, UnityTavernCardMode.Hand);
            zone.Build(
                "手牌",
                service.State.Player.Tavern.Hand.Count + "/10",
                service.State.Player.Tavern.Hand,
                HandLimit,
                UnityTavernCardMode.Hand,
                HandActionLabel,
                SelectCard,
                PlayCard,
                configureCard: (cardObject, card, index) => ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.Hand, index),
                configureSlot: (slot, index) => AddDropTarget(slot, UnityTavernDropTarget.Hand),
                layoutContext: layout);
            BuildHandBuyDropZone(zone.transform);
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
            rect.sizeDelta = new Vector2(52f, 112f);
            rect.anchoredPosition = new Vector2(-10f, -4f);

            var image = buttonObject.GetComponent<Image>();
            image.color = UnityTavernUiStyle.PanelRaised;
            image.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(ToggleRightPanelDrawer);
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                new Color(1f, 0.91f, 0.62f, 1f),
                new Color(0.72f, 0.62f, 0.42f, 1f));

            var label = UiFactory.Label("UnityRightPanelDrawerToggleText", buttonObject.transform, "功能", 15, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private void BuildFloatingRightPanel()
        {
            var panel = UnityTavernRightPanelComponent.CreatePanelHost(transform, "UnityRightPanel");
            ConfigureFloatingRightPanel(panel);
            panel.transform.SetAsLastSibling();
            panel.GetComponent<UnityTavernRightPanelComponent>().BuildTabbed(
                "功能面板",
                true,
                ToggleRightPanelDrawer,
                activeInspectorTab,
                SetInspectorTab,
                BuildActionStripPrefab,
                BuildSelectedCardPrefab,
                BuildAdvisorPrefab,
                BuildLogPrefab);
        }

        private void ConfigureFloatingRightPanel(GameObject panel)
        {
            var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(panel);
            element.ignoreLayout = true;

            var layout = UnityTavernLayoutContext.Current();
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
            Rebuild();
        }

        private void SetInspectorTab(UnityTavernInspectorTab tab)
        {
            activeInspectorTab = tab;
            Rebuild();
        }

        private void BuildActionStripPrefab(Transform parent)
        {
            var panel = UnityTavernActionPanelComponent.CreatePanelHost(parent, "UnityActionPanel");
            UnityTavernUiStyle.SetPreferredHeight(panel, 214f);
            panel.GetComponent<UnityTavernActionPanelComponent>().Build(BuildActionButtonsPrefab);
        }

        private void BuildActionButtonsPrefab(Transform parent)
        {
            BuildActionButtons(parent, "Unity", false, UnityTavernLayoutContext.Current());
        }

        private void BuildActionButtons(Transform parent, string namePrefix, bool quickBar, UnityTavernLayoutContext layout)
        {
            var height = quickBar ? Mathf.Max(UnityTavernUiStyle.TouchHeight, layout.IsCompact ? UnityTavernUiStyle.CompactTouchHeight : UnityTavernUiStyle.TouchHeight) : 0f;
            var flexibleWidth = quickBar;
            var minWidth = quickBar ? (layout.IsCompact ? 84f : 108f) : 0f;

            var heroPowers = CurrentHeroPowers();
            for (var heroPowerIndex = 0; heroPowerIndex < heroPowers.Count; heroPowerIndex += 1)
            {
                var heroPower = heroPowers[heroPowerIndex];
                var capturedHeroPower = heroPower;
                var unlocked = IsHeroPowerUnlocked(heroPower);
                var canUseHeroPower = heroPower != null && unlocked && service.CanUseHeroPower(heroPower.CardId);
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
            ActionButton(namePrefix + "FreezeButton", parent, service.State.Player.Tavern.Frozen ? "解冻" : "冻结", () => Apply(new GameCommand(GameCommandType.FreezeShop, !service.State.Player.Tavern.Frozen)), minWidth, height, flexibleWidth, UnityTavernActionButtonRole.Economy);
            ActionButton(namePrefix + "UpgradeButton", parent, UpgradeActionLabel(), () => Apply(new GameCommand(GameCommandType.UpgradeTavern)), minWidth, height, flexibleWidth, UnityTavernActionButtonRole.Economy, CanUpgradeTavern());
            ActionButton(namePrefix + "NextTurnButton", parent, "完整下回合", () => Apply(new GameCommand(GameCommandType.NextTurn)), minWidth, height, flexibleWidth, UnityTavernActionButtonRole.Primary);
            ActionButton(namePrefix + "CombatButton", parent, "开战", () => ApplyAndOpenReplay(new GameCommand(GameCommandType.SimulateCombat)), minWidth, height, flexibleWidth, UnityTavernActionButtonRole.Combat);
            ActionButton(namePrefix + "ReplayButton", parent, ReplayActionLabel(), OpenCombatReplay, minWidth, height, flexibleWidth, UnityTavernActionButtonRole.Utility, HasCombatReplay());
            ActionButton(namePrefix + "ToolsButton", parent, "工具", OpenTools, minWidth, height, flexibleWidth, UnityTavernActionButtonRole.Utility);
        }

        private bool CanRefreshShop()
        {
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
            var tavern = service.State.Player.Tavern;
            var upgradeCost = CurrentUpgradeCost();
            return tavern.UpgradeCost > 0 && tavern.Gold >= upgradeCost;
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
                return "免费刷新";
            }

            return tavern.HealthCostRefreshes > 0 ? "刷新 1血" : "刷新 " + CurrentRefreshCost();
        }

        private string UpgradeActionLabel()
        {
            var tavern = service.State.Player.Tavern;
            return tavern.UpgradeCost > 0 ? "升本 " + CurrentUpgradeCost() : "满本";
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
            return HasCombatReplay() ? "回放" : "无回放";
        }

        private static string HeroPowerActionLabel(HeroPowerDefinition heroPower)
        {
            return heroPower == null ? "无技能" : "技能 " + Math.Max(0, heroPower.Cost);
        }

        private void BuildSelectedCardPrefab(Transform parent)
        {
            var card = FindSelectedCard();
            var detail = UnityTavernSelectedCardPanelComponent.CreatePanelHost(parent, "UnitySelectedCardPanel");
            UnityTavernUiStyle.SetPreferredHeight(detail, 236f);
            detail.GetComponent<UnityTavernSelectedCardPanelComponent>().Build(content => BuildSelectedCardPrefabContent(content, card));
        }

        private void BuildSelectedCardPrefabContent(Transform parent, MinionInstance card)
        {
            if (card == null)
            {
                var emptyPanel = BuildInspectorSection(parent, "UnitySelectedCardEmptyPanel", "当前选择", UnityTavernUiStyle.Gold, 286f, 96f);
                var empty = UiFactory.Label("UnitySelectedCardEmpty", emptyPanel.transform, "选择一张牌查看详情。", 14, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 48f);
                return;
            }

            var detailLayout = Panel("UnitySelectedCardDetailLayout", parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(detailLayout, UnityTavernUiStyle.Gold, 0.2f);
            UnityTavernUiStyle.SetFixedSize(detailLayout, 386f, 202f);
            var rowLayout = detailLayout.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 8, 9, 9);
            rowLayout.spacing = 8;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var cardObject = UnityTavernCardComponent.CreateCardHost(UnityTavernCardMode.Hand, detailLayout.transform, "UnitySelectedCardDetail");
            cardObject.GetComponent<UnityTavernCardComponent>().Bind(card, UnityTavernCardMode.Hand, null, SelectCard, null, true);

            var infoStack = Panel("UnitySelectedCardInfoStack", detailLayout.transform, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(infoStack, card.CardKind == CardKind.TavernSpell ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.Gold, 0.18f);
            UnityTavernUiStyle.SetFixedSize(infoStack, 226f, 184f);
            var stackLayout = infoStack.AddComponent<VerticalLayoutGroup>();
            stackLayout.padding = new RectOffset(0, 0, 0, 0);
            stackLayout.spacing = 5;
            stackLayout.childControlWidth = true;
            stackLayout.childControlHeight = true;
            stackLayout.childForceExpandWidth = true;
            stackLayout.childForceExpandHeight = false;

            BuildSelectedCardSummary(infoStack.transform, card);

            var effectSection = BuildInspectorSection(infoStack.transform, "UnitySelectedCardEffectSection", "效果", UnityTavernUiStyle.Blue, 226f, 74f);
            var text = UiFactory.Label("UnitySelectedCardText", effectSection.transform, string.IsNullOrEmpty(card.Text) ? "无额外效果。" : card.Text, 11, FontStyle.Normal);
            text.color = UnityTavernUiStyle.MutedText;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, 28f);

            var actionSection = Panel("UnitySelectedCardActionSection", infoStack.transform, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(actionSection, UnityTavernUiStyle.Green, 0.18f);
            UnityTavernUiStyle.SetFixedSize(actionSection, 226f, 36f);
            var actionLayout = actionSection.AddComponent<HorizontalLayoutGroup>();
            actionLayout.padding = new RectOffset(10, 10, 4, 4);
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = true;
            var details = ActionButton("UnitySelectedCardDetailsButton", actionSection.transform, "查看详情", OpenCardDetail);
            UnityTavernUiStyle.SetFixedSize(details.gameObject, 186f, 28f);
            UnityTavernUiStyle.ConfigureOutline(details.gameObject, new Color(UnityTavernUiStyle.Green.r, UnityTavernUiStyle.Green.g, UnityTavernUiStyle.Green.b, 0.42f), new Vector2(1f, -1f));
        }

        private void BuildAdvisorPrefab(Transform parent)
        {
            var panel = UnityTavernAdvisorPanelComponent.CreatePanelHost(parent, "UnityAdvisorPanel");
            UnityTavernUiStyle.SetPreferredHeight(panel, 132f);
            panel.GetComponent<UnityTavernAdvisorPanelComponent>().Build("建议", BuildAdvisorPrefabLines);
        }

        private void BuildAdvisorPrefabLines(Transform parent)
        {
            var adviceLines = advisor.GetAdvice(service.State).Take(3).ToList();
            if (adviceLines.Count == 0)
            {
                adviceLines.Add("暂无建议。先进行一次操作。");
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
                logs.Add("暂无日志。先购买、刷新或战斗。");
            }

            panel.GetComponent<UnityTavernLogPanelComponent>().Build(hasCombatLog ? "战斗日志" : "招募日志", content => BuildLogPrefabLines(content, logs));
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

        private static void BuildSelectedCardSummary(Transform parent, MinionInstance card)
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

            var name = UiFactory.Label("UnitySelectedCardNameText", textStack.transform, card.Name, 12, FontStyle.Bold);
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

        private static string SelectedCardMeta(MinionInstance card)
        {
            if (card.CardKind == CardKind.TavernSpell)
            {
                return "酒馆法术 / " + Math.Max(0, card.Cost) + "费";
            }

            return card.TavernTier + "本 / " + TribesText(card) + (card.Golden ? " / 金色" : string.Empty);
        }

        private static string SelectedCardStats(MinionInstance card)
        {
            if (card.CardKind == CardKind.TavernSpell)
            {
                return "手牌 " + Math.Max(0, card.Cost) + "费";
            }

            return "攻 " + card.Attack + " / 血 " + card.Health;
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
            modal.GetComponent<UnityTavernCardDetailModalComponent>().Build(card, CloseCardDetail);
        }

        private void OpenCombatReplay()
        {
            combatReplayOpen = true;
            replayPlaying = false;
            replayPlaybackElapsed = 0f;
            Rebuild();
        }

        private void CloseCombatReplay()
        {
            combatReplayOpen = false;
            replayPlaying = false;
            replayPlaybackElapsed = 0f;
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

        private void BuildCombatReplayPanel()
        {
            ClampReplayFrameIndex();
            var panel = UnityTavernCombatReplayPanelComponent.CreatePanelHost(transform, "UnityCombatReplayPanel");
            panel.transform.SetAsLastSibling();
            panel.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(
                service.State.LastReplay,
                activeReplayFrameIndex,
                replayPlaying,
                ReplaySpeedLabels[Mathf.Clamp(replaySpeedIndex, 0, ReplaySpeedLabels.Length - 1)],
                SetReplayFrameIndex,
                ToggleReplayPlayback,
                CycleReplaySpeed,
                CloseCombatReplay);
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

        private void OpenTools()
        {
            toolsOpen = true;
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
            cardLibraryOpen = true;
            heroSelectionOpen = false;
            cardLibraryDestination = UnityCardLibraryDestination.PlayerHand;
            Rebuild();
        }

        private void OpenOpponentCardLibrary()
        {
            toolsOpen = false;
            cardLibraryOpen = true;
            heroSelectionOpen = false;
            cardLibraryDestination = UnityCardLibraryDestination.OpponentBoard;
            opponentCardLibraryGolden = false;
            Rebuild();
        }

        private void OpenOpponentHandCardLibrary()
        {
            toolsOpen = false;
            cardLibraryOpen = true;
            heroSelectionOpen = false;
            cardLibraryDestination = UnityCardLibraryDestination.OpponentHand;
            opponentCardLibraryGolden = false;
            Rebuild();
        }

        private void CloseCardLibrary()
        {
            cardLibraryOpen = false;
            toolsOpen = true;
            cardLibraryDestination = UnityCardLibraryDestination.PlayerHand;
            opponentCardLibraryGolden = false;
            Rebuild();
        }

        private void DismissCardLibrary()
        {
            cardLibraryOpen = false;
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
            BuildToolsSection(parent, "UnityToolsEconomySection", "经济", 2, grid =>
            {
                ToolButton("UnityToolsAddGoldButton", grid, "+10金币", true, () => Apply(new GameCommand(GameCommandType.DebugAddGold, 10)));
                ToolButton("UnityToolsReturnSelectedButton", grid, "回手", SelectedPlayerBoardCard() != null, ReturnSelectedToHand);
            });

            BuildToolsSection(parent, "UnityToolsCardSection", "卡牌来源", 2, grid =>
            {
                ToolButton("UnityToolsAddMinionButton", grid, "加随从", service.State.Player.Tavern.Hand.Count < HandLimit, AddFirstMinionToHand);
                ToolButton("UnityToolsAddSpellButton", grid, "加法术", service.State.Player.Tavern.Hand.Count < HandLimit, AddFirstSpellToHand);
                ToolButton("UnityToolsSwapHeroButton", grid, "换英雄", true, OpenHeroSelection);
            });

            BuildToolsSection(parent, "UnityToolsCardLibraryEntrySection", "卡牌库", 1, grid =>
            {
                ToolButton("UnityToolsOpenCardLibraryButton", grid, "打开卡牌库", true, OpenCardLibrary);
            });

            BuildToolsSection(parent, "UnityToolsOpponentSection", "对手", 6, grid =>
            {
                ToolButton("UnityToolsAddOpponentButton", grid, "加对手", true, OpenOpponentCardLibrary);
                ToolButton("UnityToolsAddOpponentHandButton", grid, "加敌方手牌", service.State.Opponent.Hand.Count < HandLimit, OpenOpponentHandCardLibrary);
                ToolButton("UnityToolsRemoveOpponentButton", grid, "移除对手", SelectedOpponentCard() != null, RemoveSelectedOpponent);
                ToolButton("UnityToolsClearOpponentButton", grid, "清空对手", service.State.Opponent.Board.Count > 0, () => Apply(new GameCommand(GameCommandType.ClearOpponentBoard)));
                ToolButton("UnityToolsCopyOpponentButton", grid, "复制", service.State.Player.Board.Count > 0, () => Apply(new GameCommand(GameCommandType.CopyPlayerBoardToOpponent)));
                ToolButton("UnityToolsMirrorOpponentButton", grid, "镜像", service.State.Player.Board.Count > 0, () => Apply(new GameCommand(GameCommandType.MirrorPlayerBoardToOpponent)));
            });

            BuildSideModifierTools(parent, BoardSide.Player, "UnityToolsPlayerModifierSection", "己方变量");
            BuildSideModifierTools(parent, BoardSide.Opponent, "UnityToolsOpponentModifierSection", "对手变量");

            BuildToolsSection(parent, "UnityToolsSelectedSection", "选中卡牌", 4, grid =>
            {
                var selected = FindSelectedCard();
                var canPatch = selected != null && selected.CardKind != CardKind.TavernSpell;
                ToolButton("UnityToolsSelectedAttackPlusButton", grid, "攻+1", canPatch, () => PatchSelected(new MinionPatch { Attack = IncrementStat(selected.Attack) }));
                ToolButton("UnityToolsSelectedAttackMinusButton", grid, "攻-1", canPatch, () => PatchSelected(new MinionPatch { Attack = selected.Attack - 1 }));
                ToolButton("UnityToolsSelectedHealthPlusButton", grid, "血+1", canPatch, () =>
                {
                    var nextHealth = IncrementStat(selected.Health);
                    PatchSelected(new MinionPatch { Health = nextHealth, MaxHealth = Math.Max(selected.MaxHealth, nextHealth) });
                });
                ToolButton("UnityToolsSelectedGoldenButton", grid, "金色", canPatch, () => PatchSelected(new MinionPatch { Golden = !selected.Golden }));
            });

            BuildToolsSection(parent, "UnityToolsCombatSection", "战斗测试", 5, grid =>
            {
                ToolButton("UnityToolsRunCombatTestButton", grid, "运行测试", true, () => ApplyAndOpenReplay(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = DefaultCombatSeed(), SafetyLimit = 200 })));
                ToolButton("UnityToolsSkipCombatNextTurnButton", grid, "跳过战斗下回合", true, () => Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn)));
                ToolButton("UnityToolsResetCombatSnapshotButton", grid, "重置快照", service.HasCombatTestSnapshot, () => Apply(new GameCommand(GameCommandType.ResetCombatTestSnapshot)));
                ToolButton("UnityToolsSaveScenarioButton", grid, "保存场景", true, () => Apply(new GameCommand(GameCommandType.SaveTestScenario, DefaultScenarioName(), new CombatTestOptions())));
                ToolButton("UnityToolsLoadScenarioButton", grid, "加载场景", service.TestScenarioNames.Count > 0, LoadFirstScenario);
            });

            BuildMechanicCoverageTools(parent);
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
            BuildToolsSection(parent, sectionName, title, 10, grid =>
            {
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

        private void SideModifierStepper(Transform grid, BoardSide side, SideCombatModifierKind kind, string label)
        {
            var value = SideModifierValue(side, kind);
            var prefix = side == BoardSide.Player ? "Player" : "Opponent";
            ToolButton(
                "UnityTools" + prefix + kind + "PlusButton",
                grid,
                label + " " + value + " +",
                true,
                () => Apply(new GameCommand(GameCommandType.AdjustSideCombatModifier, side, kind, 1)));
            ToolButton(
                "UnityTools" + prefix + kind + "MinusButton",
                grid,
                label + " " + value + " -",
                value > 0,
                () => Apply(new GameCommand(GameCommandType.AdjustSideCombatModifier, side, kind, -1)));
        }

        private int SideModifierValue(BoardSide side, SideCombatModifierKind kind)
        {
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

        private static void BuildToolsSection(Transform parent, string name, string title, int rows, Action<Transform> buildGrid)
        {
            var section = Panel(name, parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureToolsSurface(section, UnityTavernUiStyle.Gold, 0.22f);
            UnityTavernUiStyle.SetPreferredHeight(section, 42f + rows * 42f);
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
            gridLayout.cellSize = new Vector2(138f, 38f);
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
            UnityTavernUiStyle.SetPreferredHeight(header, 28f);
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

            var heading = UiFactory.Label(titleName, header.transform, title, 13, FontStyle.Bold);
            heading.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(heading.gameObject, 1f, 0f);
            return header.transform;
        }

        private static Button ToolButton(string name, Transform parent, string text, bool interactable, Action onClick)
        {
            var button = ActionButton(name, parent, text, onClick, role: UnityTavernActionButtonRole.Utility, interactable: interactable);
            return button;
        }

        private void BuildCardLibraryOverlay()
        {
            var overlay = Panel("UnityCardLibraryOverlay", transform, new Color(0f, 0f, 0f, 0.62f));
            overlay.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;

            var panel = Panel("UnityCardLibraryPanel", overlay.transform, UnityTavernUiStyle.PanelRaised);
            ConfigureToolsSurface(panel, UnityTavernUiStyle.Gold, 0.30f);
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
            bodyLayout.childForceExpandWidth = true;
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
            toolsHeroPowerCategoryFilter = null;
            toolsHeroPowerEligibilityFilter = null;
            opponentCardLibraryGolden = false;
        }

        private string CardLibraryKindTitle()
        {
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
            var header = Panel("UnityCardLibraryHeader", parent, UnityTavernUiStyle.Panel);
            ConfigureToolsSurface(header, CardLibraryAccent(), 0.22f);
            UnityTavernUiStyle.SetPreferredHeight(header, 64f);
            var layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 8;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

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

            var title = UiFactory.Label("UnityCardLibraryTitle", header.transform, CardLibraryKindTitle(), 20, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

            var summary = UiFactory.Label("UnityCardLibraryCountText", header.transform, ToolsAcquisitionSubtitle(FilteredToolsAcquisitionChoices().Count()), 12, FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleRight;
            summary.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(summary.gameObject, 270f, 32f);

            var showAll = ToolButton("UnityCardLibraryShowAllToggle", header.transform, toolsShowAllCards ? "显示全部" : "当前局", true, () =>
            {
                toolsShowAllCards = !toolsShowAllCards;
                NormalizeToolsAcquisitionTribeFilter();
                Rebuild();
            });
            UnityTavernUiStyle.SetFixedSize(showAll.gameObject, 82f, 32f);
            UnityTavernUiStyle.EnsureComponent<Image>(showAll.gameObject).color = toolsShowAllCards
                ? Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Blue, 0.42f)
                : UnityTavernUiStyle.PanelQuiet;

            var back = ToolButton("UnityCardLibraryBackButton", header.transform, "返回工具", true, CloseCardLibrary);
            UnityTavernUiStyle.SetFixedSize(back.gameObject, 92f, 32f);
            var close = ToolButton("UnityCardLibraryCloseButton", header.transform, "关闭", true, DismissCardLibrary);
            UnityTavernUiStyle.SetFixedSize(close.gameObject, 68f, 32f);
            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentBoard && toolsAcquisitionKind == CardKind.Minion)
            {
                var golden = ToolButton("UnityCardLibraryOpponentGoldenToggle", header.transform, opponentCardLibraryGolden ? "金色" : "普通", true, () =>
                {
                    opponentCardLibraryGolden = !opponentCardLibraryGolden;
                    Rebuild();
                });
                UnityTavernUiStyle.SetFixedSize(golden.gameObject, 76f, 32f);
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
            var choices = FilteredToolsAcquisitionChoices().ToList();
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

            BuildCardLibraryPanelTitle(
                center.transform,
                "UnityCardLibraryCenterTitle",
                CardLibraryKindTitle(),
                CardLibraryAccent());

            var content = UiFactory.ScrollView("UnityCardLibraryScroll", center.transform, UnityTavernUiStyle.PanelQuiet, out _);
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
                ? new Vector2(150f, 230f)
                : new Vector2(148f, 226f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;

            for (var index = 0; index < choices.Count; index += 1)
            {
                BuildCardLibraryCard(content, choices[index], index);
            }
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
                ApplyCardLibraryChoice);

            var addButtonName = index == 0 ? "UnityCardLibraryAddButton" : "UnityCardLibraryAddButton-" + SafeObjectName(card.CardId);
            var add = ToolButton(addButtonName, holder.transform, CardLibraryActionText(card), CanApplyCardLibraryChoice(card), () => ApplyCardLibraryChoice(card));
            UnityTavernUiStyle.SetFixedSize(add.gameObject, 84f, 30f);
        }

        private void ApplyCardLibraryChoice(MinionInstance card)
        {
            if (card == null || !CanApplyCardLibraryChoice(card))
            {
                return;
            }

            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentBoard)
            {
                if (card.CardKind == CardKind.Minion)
                {
                    Apply(new GameCommand(GameCommandType.AddOpponentMinion, card.CardId, opponentCardLibraryGolden));
                    return;
                }

                Apply(new GameCommand(GameCommandType.DebugCastCard, card.CardId, card.CardKind, -1));
                return;
            }

            AddLibraryCardToHand(card, cardLibraryDestination == UnityCardLibraryDestination.OpponentHand ? BoardSide.Opponent : BoardSide.Player);
        }

        private bool CanApplyCardLibraryChoice(MinionInstance card)
        {
            if (card == null)
            {
                return false;
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
            if (card != null && card.CardKind == CardKind.Hero)
            {
                return cardLibraryDestination == UnityCardLibraryDestination.PlayerHand ? "设为英雄" : "不可用";
            }

            if (card != null && card.CardKind == CardKind.HeroPower)
            {
                return "设为技能";
            }

            if (cardLibraryDestination == UnityCardLibraryDestination.PlayerHand)
            {
                return "加入";
            }

            if (cardLibraryDestination == UnityCardLibraryDestination.OpponentHand)
            {
                return "加入敌方手牌";
            }

            return card != null && card.CardKind == CardKind.Minion
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

            Apply(new GameCommand(GameCommandType.AddCardToHand, side, card.CardId, card.CardKind));
        }

        private void OpenQuestRewardLibrary(int questIndex)
        {
            advancedCardLibraryKind = AdvancedCardLibrarySelectionKind.QuestReward;
            advancedCardLibraryQuestIndex = questIndex;
            advancedCardLibraryOpen = true;
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
            cardLibraryOpen = false;
            toolsOpen = false;
            Rebuild();
        }

        private void DismissAdvancedCardLibrary()
        {
            advancedCardLibraryOpen = false;
            Rebuild();
        }

        private void BuildAdvancedCardLibraryOverlay()
        {
            var choices = AdvancedCardLibraryChoices().ToList();
            var overlay = Panel("UnityAdvancedCardLibraryOverlay", transform, new Color(0f, 0f, 0f, 0.62f));
            overlay.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;

            var panel = Panel("UnityAdvancedCardLibraryPanel", overlay.transform, UnityTavernUiStyle.PanelRaised);
            ConfigureToolsSurface(panel, AdvancedCardLibraryAccent(), 0.30f);
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

            var content = UiFactory.ScrollView("UnityAdvancedCardLibraryScroll", panel.transform, UnityTavernUiStyle.PanelQuiet, out _);
            UnityTavernUiStyle.SetFlexible(content.gameObject, 1f, 1f);

            if (choices.Count == 0)
            {
                var empty = UiFactory.Label("UnityAdvancedCardLibraryEmpty", content, "No eligible cards under the current setup.", 14, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 90f);
                return;
            }

            var gridLayout = content.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(12, 12, 12, 18);
            gridLayout.spacing = new Vector2(14f, 16f);
            gridLayout.cellSize = new Vector2(188f, 308f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;

            for (var index = 0; index < choices.Count; index += 1)
            {
                BuildAdvancedCardLibraryCard(content, choices[index], index);
            }
        }

        private void BuildAdvancedCardLibraryHeader(Transform parent, int visibleCount)
        {
            var header = Panel("UnityAdvancedCardLibraryHeader", parent, UnityTavernUiStyle.Panel);
            ConfigureToolsSurface(header, AdvancedCardLibraryAccent(), 0.22f);
            UnityTavernUiStyle.SetPreferredHeight(header, 58f);
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

            var summary = UiFactory.Label("UnityAdvancedCardLibraryCountText", header.transform, visibleCount + " selectable", 12, FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleRight;
            summary.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(summary.gameObject, 160f, 32f);

            if (advancedCardLibraryKind == AdvancedCardLibrarySelectionKind.LesserTrinket ||
                advancedCardLibraryKind == AdvancedCardLibrarySelectionKind.GreaterTrinket)
            {
                var lesser = ToolButton("UnityAdvancedCardLibraryLesserTab", header.transform, "Lesser", true, () =>
                {
                    advancedCardLibraryKind = AdvancedCardLibrarySelectionKind.LesserTrinket;
                    Rebuild();
                });
                UnityTavernUiStyle.SetFixedSize(lesser.gameObject, 76f, 32f);
                var greater = ToolButton("UnityAdvancedCardLibraryGreaterTab", header.transform, "Greater", true, () =>
                {
                    advancedCardLibraryKind = AdvancedCardLibrarySelectionKind.GreaterTrinket;
                    Rebuild();
                });
                UnityTavernUiStyle.SetFixedSize(greater.gameObject, 82f, 32f);
            }

            var close = ToolButton("UnityAdvancedCardLibraryCloseButton", header.transform, "Close", true, DismissAdvancedCardLibrary);
            UnityTavernUiStyle.SetFixedSize(close.gameObject, 68f, 32f);
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

            BuildMechanicChoiceImage(card.transform, item.ImagePath, item.CardId, item.CardKind, 92f, 126f);

            var name = UiFactory.Label("UnityAdvancedCardLibraryCardName", card.transform, item.DisplayName, 13, FontStyle.Bold);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = UnityTavernUiStyle.Text;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 34f);

            var meta = UiFactory.Label("UnityAdvancedCardLibraryCardMeta", card.transform, item.Meta, 10, FontStyle.Bold);
            meta.alignment = TextAnchor.MiddleCenter;
            meta.color = UnityTavernUiStyle.Gold;
            meta.horizontalOverflow = HorizontalWrapMode.Wrap;
            meta.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(meta.gameObject, 30f);

            var text = UiFactory.Label("UnityAdvancedCardLibraryCardText", card.transform, CleanCardText(item.Text), 10, FontStyle.Normal);
            text.color = UnityTavernUiStyle.MutedText;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, 44f);

            var notes = UiFactory.Label("UnityAdvancedCardLibraryCardNotes", card.transform, item.Notes, 9, FontStyle.Normal);
            notes.color = UnityTavernUiStyle.MutedText;
            notes.alignment = TextAnchor.UpperCenter;
            notes.horizontalOverflow = HorizontalWrapMode.Wrap;
            notes.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(notes.gameObject, 28f);

            var buttonName = index == 0 ? "UnityAdvancedCardLibrarySelectButton" : "UnityAdvancedCardLibrarySelectButton-" + SafeObjectName(item.CardId);
            var select = ActionButton(
                buttonName,
                card.transform,
                "Select",
                () => ApplyAdvancedCardLibraryChoice(item),
                0f,
                32f,
                true,
                UnityTavernActionButtonRole.Primary);
            UnityTavernUiStyle.SetPreferredHeight(select.gameObject, 32f);
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

        private static AdvancedCardLibraryItem ToAdvancedCardLibraryItem(QuestRewardDefinition reward)
        {
            return new AdvancedCardLibraryItem
            {
                CardKind = CardKind.QuestReward,
                CardId = reward.CardId,
                DisplayName = reward.Name,
                Text = reward.Text,
                ImagePath = reward.ImagePath,
                Meta = reward.PowerLevel + " / " + reward.Trigger + " / " + reward.OfferPoolStatus,
                Notes = reward.EffectKind + (string.IsNullOrWhiteSpace(reward.Notes) ? string.Empty : " / " + reward.Notes),
                TargetIndex = -1
            };
        }

        private static AdvancedCardLibraryItem ToAdvancedCardLibraryItem(TrinketDefinition trinket, int targetIndex)
        {
            var races = trinket.AssociatedRaces == null || trinket.AssociatedRaces.Count == 0
                ? "neutral"
                : string.Join("/", trinket.AssociatedRaces.Take(3).ToArray());
            var requires = trinket.Requires == null || trinket.Requires.Count == 0
                ? string.Empty
                : " / req " + string.Join("/", trinket.Requires.Take(2).ToArray());
            return new AdvancedCardLibraryItem
            {
                CardKind = CardKind.Trinket,
                CardId = trinket.CardId,
                DisplayName = trinket.Name,
                Text = trinket.Text,
                ImagePath = trinket.ImagePath,
                Meta = trinket.SlotKind + " / " + trinket.Cost + "g / " + trinket.OfferPoolStatus,
                Notes = trinket.ProxyLevel + " / " + trinket.EffectFamily + " / " + races + requires,
                TargetIndex = targetIndex
            };
        }

        private void ApplyAdvancedCardLibraryChoice(AdvancedCardLibraryItem item)
        {
            if (item == null)
            {
                return;
            }

            advancedCardLibraryOpen = false;
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
                    return "Replace Quest Reward";
                case AdvancedCardLibrarySelectionKind.GreaterTrinket:
                    return "Replace Greater Trinket";
                default:
                    return "Replace Lesser Trinket";
            }
        }

        private void BuildToolsCardLibrarySection(Transform parent)
        {
            var choices = FilteredToolsAcquisitionChoices().ToList();
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
        }

        private void BuildToolsAcquisitionModeRow(Transform parent)
        {
            var row = Panel("UnityToolsCardLibraryModeRow", parent, UnityTavernUiStyle.Panel);
            ConfigureToolsSurface(row, UnityTavernUiStyle.Blue, 0.14f);
            UnityTavernUiStyle.SetPreferredHeight(row, 40f);
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
            UnityTavernUiStyle.SetPreferredHeight(row, 40f);
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
            UnityTavernUiStyle.SetPreferredHeight(row, 40f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var label = UiFactory.Label("UnityToolsCardLibraryHeroInfoLabel", row.transform, title, 12, FontStyle.Bold);
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFixedSize(label.gameObject, 86f, 32f);

            var text = UiFactory.Label("UnityToolsCardLibraryHeroInfoValue", row.transform, value, 12, FontStyle.Bold);
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
            UnityTavernUiStyle.SetPreferredHeight(grid, 84f);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(8, 8, 6, 6);
            gridLayout.spacing = new Vector2(6f, 6f);
            gridLayout.cellSize = new Vector2(132f, 32f);
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
                UnityTavernUiStyle.SetPreferredHeight(grid, 40f);
                var rowLayout = grid.AddComponent<HorizontalLayoutGroup>();
                rowLayout.padding = new RectOffset(8, 8, 4, 4);
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = true;
                var info = UiFactory.Label("UnityToolsCardLibraryHeroInfoText", grid.transform, "点击条目可设为当前英雄。", 12, FontStyle.Bold);
                info.alignment = TextAnchor.MiddleCenter;
                info.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetFlexible(info.gameObject, 1f, 0f);
                return;
            }

            if (toolsAcquisitionKind == CardKind.HeroPower)
            {
                UnityTavernUiStyle.SetPreferredHeight(grid, 84f);
                var eligibilityGridLayout = grid.AddComponent<GridLayoutGroup>();
                eligibilityGridLayout.padding = new RectOffset(8, 8, 6, 6);
                eligibilityGridLayout.spacing = new Vector2(6f, 6f);
                eligibilityGridLayout.cellSize = new Vector2(132f, 32f);
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
                UnityTavernUiStyle.SetPreferredHeight(grid, 120f);
                var spellGridLayout = grid.AddComponent<GridLayoutGroup>();
                spellGridLayout.padding = new RectOffset(8, 8, 6, 6);
                spellGridLayout.spacing = new Vector2(6f, 6f);
                spellGridLayout.cellSize = new Vector2(132f, 32f);
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

            UnityTavernUiStyle.SetPreferredHeight(grid, 120f);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(8, 8, 6, 6);
            gridLayout.spacing = new Vector2(6f, 6f);
            gridLayout.cellSize = new Vector2(132f, 32f);
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
            UnityTavernUiStyle.SetPreferredHeight(row, 46f);
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

            var name = UiFactory.Label("UnityToolsCardLibraryChoiceName", row.transform, card.Name, 12, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFlexible(name.gameObject, 1f, 0f);

            var meta = UiFactory.Label("UnityToolsCardLibraryChoiceMeta", row.transform, ToolsAcquisitionCardMeta(card), 11, FontStyle.Normal);
            meta.alignment = TextAnchor.MiddleRight;
            meta.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(meta.gameObject, 190f, 34f);

            var addButtonName = index == 0 ? "UnityToolsCardLibraryAddButton" : "UnityToolsCardLibraryAddButton-" + SafeObjectName(card.CardId);
            var add = ToolButton(addButtonName, row.transform, CardLibraryActionText(card), CanApplyCardLibraryChoice(card), () =>
            {
                ApplyCardLibraryChoice(card);
            });
            UnityTavernUiStyle.SetFixedSize(add.gameObject, 72f, 34f);
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
            UnityTavernUiStyle.SetFixedSize(button.gameObject, width, 32f);
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? new Color(0.56f, 0.38f, 0.16f, 0.96f) : UnityTavernUiStyle.PanelRaised;
            }

            var outline = UnityTavernUiStyle.ConfigureOutline(
                button.gameObject,
                active ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.70f) : new Color(0f, 0f, 0f, 0.22f),
                new Vector2(1f, -1f));
            outline.enabled = active;

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = active ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Text;
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
                return "通用法术";
            }

            return TribeName(toolsAcquisitionTribeFilter);
        }

        private IEnumerable<MinionInstance> FilteredToolsAcquisitionChoices()
        {
            NormalizeToolsAcquisitionTribeFilter();
            var choices = BuildToolsAcquisitionChoices();

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

            return choices
                .OrderBy(card => card.TavernTier)
                .ThenBy(card => card.Name)
                .Take(toolsAcquisitionKind == CardKind.Hero ? 160 : 80)
                .ToList();
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
            var definitions = MinionCatalogLoader.LoadFromResources().All
                .Where(card => service.IsMinionAllowedByCardPool(card) && !card.CardId.StartsWith("BGDUO"));
            if (!toolsShowAllCards)
            {
                var active = ActiveLibraryTribes();
                definitions = definitions.Where(card => TribeAvailabilityRules.IsMinionAvailable(card, active));
            }

            foreach (var definition in definitions)
            {
                yield return MinionFactory.Create(definition, BoardSide.Player, "unity-tools-library", false, PoolSource.Debug, 0);
            }
        }

        private IEnumerable<MinionInstance> BuildToolsAcquisitionSpellChoices()
        {
            var definitions = SpellCatalogLoader.LoadFromResources().All
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

        private static string ToolsAcquisitionCardMeta(MinionInstance card)
        {
            if (card.CardKind == CardKind.TavernSpell)
            {
                return card.TavernTier + "本 / " + card.Cost + "费 / " + SpellTribesText(card);
            }

            if (card.CardKind == CardKind.HeroPower)
            {
                return Math.Max(0, card.Cost) + "费 / " + HeroPowerTagValue(card, "category") + " / " + HeroPowerTagValue(card, "eligibility");
            }

            if (card.CardKind == CardKind.Hero)
            {
                return card.Health + "生命 / " + card.Attack + "护甲" + HeroPowerSuffix(card);
            }

            if (card.CardKind == CardKind.HeroBuddy)
            {
                return card.TavernTier + "本 / " + card.Attack + "/" + card.Health + " / " + TribesText(card) + HeroBuddyHeroSuffix(card);
            }

            return card.TavernTier + "本 / " + card.Attack + "/" + card.Health + " / " + TribesText(card);
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

        private static string TribesText(MinionInstance card)
        {
            if (IsNeutralLibraryMinion(card))
            {
                return "中立";
            }

            if (card.Tribes.Contains(Tribe.All))
            {
                return "全部种族";
            }

            var tribes = card.Tribes.Where(tribe => tribe != Tribe.None).Take(2).Select(TribeName).ToArray();
            return tribes.Length == 0 ? "中立" : string.Join("/", tribes);
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

        private static string SpellTribesText(MinionInstance card)
        {
            if (card == null || card.Tribes == null || card.Tribes.Count == 0 || card.Tribes.All(tribe => tribe == Tribe.None))
            {
                return "通用法术";
            }

            return string.Join("/", card.Tribes.Where(tribe => tribe != Tribe.None).Select(TribeName).ToArray());
        }

        private static string TribeName(Tribe tribe)
        {
            switch (tribe)
            {
                case Tribe.Beast: return "野兽";
                case Tribe.Murloc: return "鱼人";
                case Tribe.Mech: return "机械";
                case Tribe.Demon: return "恶魔";
                case Tribe.Dragon: return "龙";
                case Tribe.Pirate: return "海盗";
                case Tribe.Elemental: return "元素";
                case Tribe.Quilboar: return "野猪人";
                case Tribe.Undead: return "亡灵";
                case Tribe.Naga: return "纳迦";
                case Tribe.All: return "全部";
                case Tribe.None: return "中立";
                default: return "中立";
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
            var definition = MinionCatalogLoader.LoadFromResources().All.FirstOrDefault(card =>
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
            var definition = SpellCatalogLoader.LoadFromResources().All.FirstOrDefault(spell =>
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
            var definition = MinionCatalogLoader.LoadFromResources().All.FirstOrDefault(card =>
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
                    service.Apply(command);
                }

                lastFeedback = feedback;
                selectedInstanceId = FindSelectedCard()?.InstanceId ?? service.State.Player.Tavern.Shop.FirstOrDefault(card => card != null)?.InstanceId;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                lastFeedback = null;
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
            if (!string.IsNullOrEmpty(scenarioName))
            {
                Apply(new GameCommand(GameCommandType.LoadTestScenario, scenarioName, new CombatTestOptions()));
            }
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
            var zone = BuildDragDropOverlay(
                "UnityHandBuyDropZone",
                parent,
                new Color(0.06f, 0.28f, 0.42f, 0.62f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0.48f),
                new Vector2(12f, 8f),
                new Vector2(-12f, -4f),
                "拖到这里购买",
                "整个手牌下方都可放置");
            AddDropTarget(
                zone,
                UnityTavernDropTarget.Hand,
                -1,
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
            var zone = BuildDragDropOverlay(
                "UnitySellDropZone",
                parent,
                new Color(0.40f, 0.08f, 0.06f, 0.92f),
                new Vector2(0f, 0.50f),
                new Vector2(1f, 1f),
                new Vector2(10f, 6f),
                new Vector2(-10f, -8f),
                "拖到这里出售",
                "出售己方战场随从");
            UnityTavernUiStyle.ConfigureOutline(zone, new Color(1f, 0.36f, 0.24f, 0.78f), new Vector2(3f, -3f));
            AddDropTarget(
                zone,
                UnityTavernDropTarget.SellZone,
                -1,
                raycastOnlyWhenAllowed: true,
                activeOnlyWhenAllowed: true,
                cueOnlyWhenAllowed: true);
            zone.SetActive(false);
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
            label.color = Color.white;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, 28f);

            var hint = UiFactory.Label(name + "Hint", zone.transform, hintText, 12, FontStyle.Bold);
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = new Color(1f, 0.86f, 0.74f, 0.96f);
            UnityTavernUiStyle.SetPreferredHeight(hint.gameObject, 20f);

            return zone;
        }

        private void BuildDiscoverModal()
        {
            var modal = UnityTavernDiscoverModalComponent.CreateModalHost(transform, "UnityDiscoverOverlay");
            modal.GetComponent<UnityTavernDiscoverModalComponent>().Build("发现奖励", BuildDiscoverOptions);
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

            var panel = Panel("UnityQuestTrackerPanel", transform, new Color(UnityTavernUiStyle.Panel.r, UnityTavernUiStyle.Panel.g, UnityTavernUiStyle.Panel.b, 0.94f));
            ConfigureInspectorSurface(panel, UnityTavernUiStyle.Blue, 0.28f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(18f, -86f);
            rect.sizeDelta = new Vector2(360f, QuestTrackerHeight(activeCount));

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 9, 10);
            layout.spacing = 7;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityQuestTrackerTitle", panel.transform, "Quests", 14, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 20f);

            if (questState?.MainQuest != null)
            {
                BuildQuestTrackerRow(panel.transform, questState.MainQuest, "Main", 0);
            }

            if (questState?.BonusQuest != null)
            {
                BuildQuestTrackerRow(panel.transform, questState.BonusQuest, "Bonus", 1);
            }
        }

        private void BuildQuestTrackerRow(Transform parent, ActiveQuestState quest, string slot, int questIndex)
        {
            var row = Panel("UnityQuestTrackerRow-" + slot, parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(row, quest.Completed ? UnityTavernUiStyle.Green : UnityTavernUiStyle.Gold, 0.18f);
            UnityTavernUiStyle.SetPreferredHeight(row, 76f);
            var layout = row.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 5, 6);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var heading = UiFactory.Label("UnityQuestTrackerHeading", row.transform, slot + ": " + quest.QuestName + "  " + quest.Progress + "/" + quest.RequiredAmount, 11, FontStyle.Bold);
            heading.color = quest.Completed ? UnityTavernUiStyle.Green : UnityTavernUiStyle.Text;
            heading.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(heading.gameObject, 17f);

            var reward = UiFactory.Label("UnityQuestTrackerReward", row.transform, (quest.RewardActive ? "Active: " : "Reward: ") + quest.RewardName, 10, FontStyle.Normal);
            reward.color = UnityTavernUiStyle.MutedText;
            reward.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(reward.gameObject, 15f);

            var actions = Panel("UnityQuestTrackerActions-" + slot, row.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(actions, 28f);
            var actionLayout = actions.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 6;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = true;

            var barBack = Panel("UnityQuestProgressBack", actions.transform, new Color(0f, 0f, 0f, 0.28f));
            UnityTavernUiStyle.SetFlexible(barBack, 1f, 0f);
            var fill = Panel("UnityQuestProgressFill", barBack.transform, quest.Completed ? UnityTavernUiStyle.Green : UnityTavernUiStyle.Gold);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(quest.RequiredAmount <= 0 ? 1f : (float)quest.Progress / quest.RequiredAmount), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            ActionButton(
                "UnityQuestCompleteButton-" + slot,
                actions.transform,
                "Complete",
                () => Apply(new GameCommand(GameCommandType.DebugCompleteQuest, questIndex)),
                76f,
                26f,
                false,
                UnityTavernActionButtonRole.Primary,
                !quest.Completed);
            ActionButton(
                "UnityQuestReplaceRewardButton-" + slot,
                actions.transform,
                "Reward",
                () => OpenQuestRewardLibrary(questIndex),
                72f,
                26f,
                false,
                UnityTavernActionButtonRole.Utility);
        }

        private void BuildTrinketTrackerOverlay()
        {
            var state = service.State.Player.Tavern.AdvancedMechanics?.Trinkets;
            var lesser = ResolveTrinketDefinition(state?.LesserTrinketId);
            var greater = ResolveTrinketDefinition(state?.GreaterTrinketId);
            var hasCandidates = service.GetDebugSelectableTrinkets(TrinketSlotKind.Lesser).Count > 0 ||
                service.GetDebugSelectableTrinkets(TrinketSlotKind.Greater).Count > 0;
            if (lesser == null && greater == null && !hasCandidates)
            {
                return;
            }

            var questCount = ActiveQuestCount();
            var panel = Panel("UnityTrinketTrackerPanel", transform, new Color(UnityTavernUiStyle.Panel.r, UnityTavernUiStyle.Panel.g, UnityTavernUiStyle.Panel.b, 0.94f));
            ConfigureInspectorSurface(panel, UnityTavernUiStyle.Gold, 0.24f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(18f, -86f - (questCount > 0 ? QuestTrackerHeight(questCount) + 8f : 0f));
            rect.sizeDelta = new Vector2(360f, 132f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 9, 10);
            layout.spacing = 7;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityTrinketTrackerTitle", panel.transform, "Trinkets", 14, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 20f);

            BuildTrinketTrackerRow(panel.transform, "Lesser", TrinketSlotKind.Lesser, lesser);
            BuildTrinketTrackerRow(panel.transform, "Greater", TrinketSlotKind.Greater, greater);
        }

        private void BuildTrinketTrackerRow(Transform parent, string slotName, TrinketSlotKind slotKind, TrinketDefinition definition)
        {
            var row = Panel("UnityTrinketTrackerRow-" + slotName, parent, UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(row, slotKind == TrinketSlotKind.Greater ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Blue, 0.16f);
            UnityTavernUiStyle.SetPreferredHeight(row, 42f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 5, 5);
            layout.spacing = 7;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var slot = UiFactory.Label("UnityTrinketSlotLabel-" + slotName, row.transform, slotName, 11, FontStyle.Bold);
            slot.color = UnityTavernUiStyle.Gold;
            slot.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetFixedSize(slot.gameObject, 54f, 30f);

            var name = UiFactory.Label("UnityTrinketName-" + slotName, row.transform, definition == null ? "None equipped" : definition.Name, 11, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFlexible(name.gameObject, 1f, 0f);

            var meta = UiFactory.Label("UnityTrinketMeta-" + slotName, row.transform, definition == null ? string.Empty : definition.Cost + "g / " + definition.OfferPoolStatus, 10, FontStyle.Normal);
            meta.color = UnityTavernUiStyle.MutedText;
            meta.alignment = TextAnchor.MiddleRight;
            meta.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFixedSize(meta.gameObject, 86f, 30f);

            ActionButton(
                "UnityTrinketReplaceButton-" + slotName,
                row.transform,
                "Replace",
                () => OpenTrinketLibrary(slotKind),
                70f,
                30f,
                false,
                UnityTavernActionButtonRole.Utility);
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
            return 46f + activeCount * 82f;
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

            var panel = Panel("UnityAdvancedChoiceStatusPanel", transform, new Color(UnityTavernUiStyle.Panel.r, UnityTavernUiStyle.Panel.g, UnityTavernUiStyle.Panel.b, 0.94f));
            ConfigureInspectorSurface(panel, UnityTavernUiStyle.Green, 0.22f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -86f);
            rect.sizeDelta = new Vector2(430f, 42f + visible.Count * 42f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 9, 10);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityAdvancedChoiceStatusTitle", panel.transform, "Advanced choices", 14, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 20f);

            foreach (var status in visible)
            {
                BuildAdvancedChoiceStatusRow(panel.transform, status);
            }
        }

        private void BuildAdvancedChoiceStatusRow(Transform parent, AdvancedChoiceStatus status)
        {
            var safeId = SafeObjectName(status.Id);
            var row = Panel("UnityAdvancedChoiceStatusRow-" + safeId, parent, status.IsCurrent ? UnityTavernUiStyle.PanelRaised : UnityTavernUiStyle.PanelQuiet);
            ConfigureInspectorSurface(row, status.IsCurrent ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Green, 0.16f);
            UnityTavernUiStyle.SetPreferredHeight(row, 36f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 7;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var markerText = status.IsCurrent ? "Now" : "R" + status.DueRound;
            var marker = UiFactory.Label("UnityAdvancedChoiceStatusMarker-" + safeId, row.transform, markerText, 11, FontStyle.Bold);
            marker.color = status.IsCurrent ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Green;
            marker.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetFixedSize(marker.gameObject, 38f, 28f);

            var title = UiFactory.Label("UnityAdvancedChoiceStatusName-" + safeId, row.transform, status.Title, 11, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFixedSize(title.gameObject, 130f, 28f);

            var detail = UiFactory.Label("UnityAdvancedChoiceStatusDetail-" + safeId, row.transform, status.Detail, 10, FontStyle.Normal);
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
                    "Open",
                    Rebuild,
                    58f,
                    28f,
                    false,
                    UnityTavernActionButtonRole.Primary);
            }
        }

        private void BuildAdvancedMechanicChoiceModal()
        {
            var request = service.State.Player.Tavern.AdvancedMechanics?.PendingChoice;
            if (request == null)
            {
                return;
            }

            var overlay = Panel("UnityAdvancedMechanicChoiceOverlay", transform, new Color(0f, 0f, 0f, 0.62f));
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().raycastTarget = true;
            overlay.transform.SetAsLastSibling();

            var layoutContext = UnityTavernLayoutContext.Current();
            var panel = Panel("UnityAdvancedMechanicChoicePanel", overlay.transform, UnityTavernUiStyle.PanelRaised);
            ConfigureInspectorSurface(panel, request.Kind == AdvancedMechanicKind.Quest ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.Gold, 0.32f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(layoutContext.IsCompact ? 620f : 820f, layoutContext.IsCompact ? 430f : 470f);
            panelRect.anchoredPosition = Vector2.zero;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(18, 18, 16, 18);
            panelLayout.spacing = 12;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var header = Panel("UnityAdvancedMechanicChoiceHeader", panel.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(header, 40f);
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
                    "自由选择",
                    () => OpenPlayerDirectedChoice(request),
                    92f,
                    34f,
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
                BuildAdvancedMechanicChoiceCard(options.transform, request, request.Options[index], index);
            }
        }

        private void BuildAdvancedMechanicChoiceCard(Transform parent, MechanicChoiceRequest request, MechanicChoiceOption option, int index)
        {
            var card = Panel("UnityAdvancedMechanicChoiceCard-" + index, parent, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(card, request.Kind == AdvancedMechanicKind.Quest ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.Gold, 0.22f);
            UnityTavernUiStyle.SetFixedSize(card, request.Kind == AdvancedMechanicKind.Quest ? 250f : 190f, 366f);

            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 7;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            if (request.Kind == AdvancedMechanicKind.Quest)
            {
                BuildQuestChoiceImages(card.transform, option);
            }
            else
            {
                BuildMechanicChoiceImage(card.transform, option.ImagePath, option.SourceId, CardKind.Trinket, 128f, 184f);
            }

            var name = UiFactory.Label("UnityAdvancedMechanicChoiceName", card.transform, option.DisplayName, 14, FontStyle.Bold);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = UnityTavernUiStyle.Text;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 36f);

            var text = UiFactory.Label("UnityAdvancedMechanicChoiceText", card.transform, CleanCardText(option.Text), 11, FontStyle.Normal);
            text.color = UnityTavernUiStyle.MutedText;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, request.Kind == AdvancedMechanicKind.Quest ? 42f : 48f);

            if (request.Kind == AdvancedMechanicKind.Quest)
            {
                var reward = UiFactory.Label("UnityAdvancedMechanicChoiceReward", card.transform, option.RewardName + "\n" + CleanCardText(option.RewardText), 10, FontStyle.Bold);
                reward.color = UnityTavernUiStyle.Gold;
                reward.alignment = TextAnchor.UpperCenter;
                reward.horizontalOverflow = HorizontalWrapMode.Wrap;
                reward.verticalOverflow = VerticalWrapMode.Truncate;
                UnityTavernUiStyle.SetPreferredHeight(reward.gameObject, 50f);
            }

            var label = request.Kind == AdvancedMechanicKind.Trinket && option.Cost > 0 ? "Choose (" + option.Cost + ")" : "Choose";
            ActionButton(
                "UnityAdvancedMechanicChoiceButton-" + index,
                card.transform,
                label,
                () => Apply(new GameCommand(GameCommandType.ChooseMechanicOption, index)),
                0f,
                36f,
                true,
                UnityTavernActionButtonRole.Primary);
        }

        private void BuildQuestChoiceImages(Transform parent, MechanicChoiceOption option)
        {
            var row = Panel("UnityQuestChoiceImageRow", parent, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, 132f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            BuildMechanicChoiceImage(row.transform, option.ImagePath, option.SourceId, CardKind.Quest, 88f, 124f);
            BuildMechanicChoiceImage(row.transform, option.RewardImagePath, option.RewardId, CardKind.QuestReward, 88f, 124f);
        }

        private static void BuildMechanicChoiceImage(Transform parent, string imagePath, string cardId, CardKind kind, float width, float height)
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
        }

        private static string AdvancedMechanicChoiceTitle(MechanicChoiceRequest request)
        {
            if (request.Kind == AdvancedMechanicKind.Quest)
            {
                return string.Equals(request.Slot, "Bonus", StringComparison.OrdinalIgnoreCase)
                    ? "Choose a Bonus Quest + Reward"
                    : "Choose a Quest + Reward";
            }

            return string.Equals(request.Slot, "Greater", StringComparison.OrdinalIgnoreCase)
                ? "Choose a Greater Trinket"
                : "Choose a Lesser Trinket";
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

            var layoutContext = UnityTavernLayoutContext.Current();
            var panel = Panel("UnityPlayerDirectedChoicePanel", overlay.transform, UnityTavernUiStyle.PanelRaised);
            ConfigureInspectorSurface(panel, playerDirectedChoiceKind == PlayerDirectedChoiceKind.Trinket ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Blue, 0.32f);
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

            var list = UiFactory.ScrollView("UnityPlayerDirectedChoiceScroll", panel.transform, UnityTavernUiStyle.PanelQuiet, out _);
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
                var empty = UiFactory.Label("UnityPlayerDirectedChoiceEmpty", list, "No selectable options under the current filters.", 14, FontStyle.Bold);
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
            var header = Panel("UnityPlayerDirectedChoiceHeader", parent, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(header, playerDirectedChoiceKind == PlayerDirectedChoiceKind.Trinket ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Blue, 0.22f);
            UnityTavernUiStyle.SetPreferredHeight(header, 176f);
            var layout = header.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var top = Panel("UnityPlayerDirectedChoiceHeaderTop", header.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(top, 34f);
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

            var count = UiFactory.Label("UnityPlayerDirectedChoiceCount", top.transform, visibleCount + " / " + totalCount, 12, FontStyle.Bold);
            count.color = UnityTavernUiStyle.MutedText;
            count.alignment = TextAnchor.MiddleRight;
            UnityTavernUiStyle.SetFixedSize(count.gameObject, 90f, 30f);

            ActionButton(
                "UnityPlayerDirectedChoiceCloseButton",
                top.transform,
                "Close",
                ClosePlayerDirectedChoice,
                68f,
                30f,
                false,
                UnityTavernActionButtonRole.Neutral);

            var searchObject = new GameObject("UnityPlayerDirectedChoiceSearchInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            searchObject.transform.SetParent(header.transform, false);
            UnityTavernUiStyle.SetPreferredHeight(searchObject, 34f);
            searchObject.GetComponent<Image>().color = UnityTavernUiStyle.PanelQuiet;
            var input = searchObject.GetComponent<InputField>();
            input.textComponent = UiFactory.Label("UnityPlayerDirectedChoiceSearchText", searchObject.transform, string.Empty, 13);
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            input.textComponent.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.textComponent.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.placeholder = UiFactory.Label("UnityPlayerDirectedChoiceSearchPlaceholder", searchObject.transform, "Search name or CardId", 13);
            input.placeholder.color = UnityTavernUiStyle.MutedText;
            input.placeholder.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.placeholder.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.text = playerDirectedSearchText;
            input.onEndEdit.AddListener(value =>
            {
                playerDirectedSearchText = value ?? string.Empty;
                Rebuild();
            });

            BuildPlayerDirectedChoiceFilters(header.transform, allOptions);
        }

        private void BuildPlayerDirectedChoiceFilters(Transform parent, IReadOnlyList<PlayerDirectedChoiceOption> allOptions)
        {
            var filters = Panel("UnityPlayerDirectedChoiceFilters", parent, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(filters, 70f);
            var layout = filters.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var row = Panel("UnityPlayerDirectedChoiceFilterRow", filters.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, 32f);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 6;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterStatusAll", row.transform, "全部", playerDirectedSelectableFilter == 0, 54f, () =>
            {
                playerDirectedSelectableFilter = 0;
                Rebuild();
            });
            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterStatusSelectable", row.transform, "可选", playerDirectedSelectableFilter == 1, 54f, () =>
            {
                playerDirectedSelectableFilter = 1;
                Rebuild();
            });
            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterStatusBlocked", row.transform, "不可选", playerDirectedSelectableFilter == 2, 64f, () =>
            {
                playerDirectedSelectableFilter = 2;
                Rebuild();
            });

            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterCostAll", row.transform, "费用", playerDirectedCostFilter == 0, 54f, () =>
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
                PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterSlotAll", row.transform, "槽位", string.IsNullOrEmpty(playerDirectedSlotFilter), 54f, () =>
                {
                    playerDirectedSlotFilter = string.Empty;
                    Rebuild();
                });

                foreach (var slot in slots.Take(3))
                {
                    var capturedSlot = slot;
                    PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterSlot" + SafeObjectName(slot), row.transform, slot, string.Equals(playerDirectedSlotFilter, slot, StringComparison.OrdinalIgnoreCase), 68f, () =>
                    {
                        playerDirectedSlotFilter = capturedSlot;
                        Rebuild();
                    });
                }
            }

            var tagRow = Panel("UnityPlayerDirectedChoiceTagFilterRow", filters.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(tagRow, 32f);
            var tagLayout = tagRow.AddComponent<HorizontalLayoutGroup>();
            tagLayout.spacing = 6;
            tagLayout.childControlWidth = false;
            tagLayout.childControlHeight = true;
            tagLayout.childForceExpandWidth = false;
            tagLayout.childForceExpandHeight = true;

            PlayerDirectedFilterButton("UnityPlayerDirectedChoiceFilterTagAll", tagRow.transform, "全部标签", string.IsNullOrEmpty(playerDirectedTagFilter), 78f, () =>
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
            return LibraryFilterButton(name, parent, text, active, width, onClick);
        }

        private void BuildPlayerDirectedChoiceRow(Transform parent, PlayerDirectedChoiceOption option, int index)
        {
            var row = Panel("UnityPlayerDirectedChoiceOption-" + index + "-" + SafeObjectName(option.CardId + "-" + option.SecondaryCardId), parent, UnityTavernUiStyle.Panel);
            ConfigureInspectorSurface(row, option.IsSelectable ? UnityTavernUiStyle.Green : UnityTavernUiStyle.Red, 0.16f);
            UnityTavernUiStyle.SetPreferredHeight(row, 86f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            BuildMechanicChoiceImage(row.transform, option.ImagePath, option.CardId, PlayerDirectedCardKind(option), 52f, 72f);

            var details = Panel("UnityPlayerDirectedChoiceDetails", row.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(details, 1f, 0f);
            var detailsLayout = details.AddComponent<VerticalLayoutGroup>();
            detailsLayout.spacing = 3;
            detailsLayout.childControlWidth = true;
            detailsLayout.childControlHeight = true;
            detailsLayout.childForceExpandWidth = true;
            detailsLayout.childForceExpandHeight = false;

            var nameText = option.DisplayName + (string.IsNullOrWhiteSpace(option.SecondaryDisplayName) ? string.Empty : " + " + option.SecondaryDisplayName);
            var name = UiFactory.Label("UnityPlayerDirectedChoiceName", details.transform, nameText, 13, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, 20f);

            var metaText = option.CardId + (string.IsNullOrWhiteSpace(option.SecondaryCardId) ? string.Empty : " / " + option.SecondaryCardId);
            var meta = UiFactory.Label("UnityPlayerDirectedChoiceMeta", details.transform, metaText + "  " + option.Status, 10, FontStyle.Normal);
            meta.color = UnityTavernUiStyle.MutedText;
            meta.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(meta.gameObject, 18f);

            var text = UiFactory.Label(
                "UnityPlayerDirectedChoiceText",
                details.transform,
                string.IsNullOrWhiteSpace(option.DisabledReason) ? CleanCardText(option.Text) : option.DisabledReason,
                10,
                option.IsSelectable ? FontStyle.Normal : FontStyle.Bold);
            text.color = option.IsSelectable ? UnityTavernUiStyle.MutedText : UnityTavernUiStyle.Red;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, 34f);

            var buttonName = index == 0 ? "UnityPlayerDirectedChoiceSelectButton" : "UnityPlayerDirectedChoiceSelectButton-" + SafeObjectName(option.CardId + "-" + option.SecondaryCardId);
            ActionButton(
                buttonName,
                row.transform,
                option.IsSelectable ? "Choose" : "Blocked",
                () => ApplyPlayerDirectedChoice(option),
                82f,
                32f,
                false,
                option.IsSelectable ? UnityTavernActionButtonRole.Primary : UnityTavernActionButtonRole.Danger,
                option.IsSelectable);
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
                    var request = service.State.Player.Tavern.AdvancedMechanics?.PendingChoice;
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

        private static List<string> PlayerDirectedVisibleFilterTags(IReadOnlyList<PlayerDirectedChoiceOption> options)
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

        private static string PlayerDirectedFilterTagLabel(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return string.Empty;
            }

            var value = tag;
            var separator = tag.IndexOf(':');
            if (separator >= 0 && separator + 1 < tag.Length)
            {
                value = tag.Substring(separator + 1);
            }

            return value.Replace("_", " ");
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
                    return "自由选择 " + playerDirectedTrinketSlotKind + " Trinket";
                case PlayerDirectedChoiceKind.SecondHeroPower:
                    return "自由选择 Second Hero Power";
                default:
                    return "自由选择 Quest + Reward";
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
                var cardObject = UnityTavernCardComponent.CreateCardHost(UnityTavernCardMode.Shop, parent, "UnityDiscoverCard-" + optionIndex);
                cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                    options[index],
                    UnityTavernCardMode.Shop,
                    "选择",
                    SelectCard,
                    card => Apply(new GameCommand(GameCommandType.ChooseDiscover, optionIndex)));
                ConfigureCardFeedback(cardObject, options[index]);
                AddDrag(cardObject, options[index], UnityTavernDragSource.Discover, optionIndex);
            }
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

            BeginDrag(card, UnityTavernDragSource.HeroPower, 0);
        }

        public void BeginDrag(MinionInstance card, UnityTavernDragSource source, int index, PointerEventData eventData = null)
        {
            if (card == null)
            {
                return;
            }

            activeDrag = new UnityTavernDragContext(card, source, index);
            selectedInstanceId = card.InstanceId;
            SetDiscoverBackdropRaycastBlocking(source != UnityTavernDragSource.Discover);
            if (eventData != null)
            {
                CreateDragGhost(card, eventData);
            }

            RefreshCardSelection();
            RefreshDropTargetCues();
        }

        public void MoveDrag(PointerEventData eventData)
        {
            if (dragGhost == null || eventData == null)
            {
                return;
            }

            MoveDragGhost(eventData);
        }

        public void EndDrag()
        {
            SetDiscoverBackdropRaycastBlocking(true);
            ClearDropTargetCues();
            activeDrag = null;
            DestroyDragGhost();
        }

        public void HandleDrop(UnityTavernDropTarget target, int targetIndex = -1)
        {
            if (activeDrag == null)
            {
                return;
            }

            var drag = activeDrag;
            if (!UnityTavernDragController.TryBuildDropCommand(drag, target, targetIndex, out var command))
            {
                lastError = "拖放目标无效。";
                lastFeedback = null;
                EndDrag();
                Rebuild();
                return;
            }

            selectedInstanceId = target == UnityTavernDropTarget.SellZone ? null : ResolveDropTargetInstanceId(target, targetIndex) ?? drag.Card.InstanceId;
            activeDrag = null;
            SetDiscoverBackdropRaycastBlocking(true);
            DestroyDragGhost();
            Apply(command);
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

            return null;
        }

        private void CreateDragGhost(MinionInstance card, PointerEventData eventData)
        {
            DestroyDragGhost();
            dragGhost = UnityTavernCardComponent.CreateCardHost(UnityTavernCardMode.Hand, transform, "UnityDragGhost-" + card.InstanceId);
            dragGhost.GetComponent<UnityTavernCardComponent>().Bind(card, UnityTavernCardMode.Hand, null, null, null);
            dragGhost.transform.SetAsLastSibling();

            var canvas = UnityTavernUiStyle.EnsureComponent<Canvas>(dragGhost);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5000;

            var group = UnityTavernUiStyle.EnsureComponent<CanvasGroup>(dragGhost);
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0.92f;

            MoveDragGhost(eventData);
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
            ghostRect.anchoredPosition = point;
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

            if (TryHandleTargetedHeroPowerClick(card))
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

        private bool TryHandleTargetedHeroPowerClick(MinionInstance card)
        {
            if (activeDrag == null || activeDrag.Source != UnityTavernDragSource.HeroPower)
            {
                return false;
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

            return false;
        }

        private void BuyCard(MinionInstance card)
        {
            var index = service.State.Player.Tavern.Shop.FindIndex(item => item.InstanceId == card.InstanceId);
            if (index >= 0)
            {
                Apply(new GameCommand(GameCommandType.BuyMinion, index));
            }
        }

        private void BuyTimewarpedOffer(MinionInstance card)
        {
            if (card == null)
            {
                return;
            }

            var offers = service.State.Player.Tavern.Timewarp?.Offers;
            if (offers == null)
            {
                return;
            }

            var index = offers.FindIndex(offer => offer != null && !offer.Purchased && offer.CardId == card.CardId);
            if (index >= 0)
            {
                Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, index));
            }
        }

        private void PlayCard(MinionInstance card)
        {
            var index = service.State.Player.Tavern.Hand.FindIndex(item => item.InstanceId == card.InstanceId);
            if (index >= 0)
            {
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
            try
            {
                lastError = null;
                service.Apply(command);
                lastFeedback = FeedbackForCommand(command);
                selectedInstanceId = FindSelectedCard()?.InstanceId ?? service.State.Player.Tavern.Shop.FirstOrDefault(card => card != null)?.InstanceId;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                lastFeedback = null;
            }

            Rebuild();
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
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, hero.HeroCardId, CardKind.Hero));
                lastFeedback = "已更换为 " + hero.Name;
                selectedInstanceId = FindSelectedCard()?.InstanceId ?? service.State.Player.Tavern.Shop.FirstOrDefault(card => card != null)?.InstanceId;
                heroSelectionOpen = false;
                toolsOpen = false;
                cardLibraryOpen = false;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                lastFeedback = null;
            }

            Rebuild();
        }

        private void ApplyAndOpenReplay(GameCommand command)
        {
            try
            {
                lastError = null;
                service.Apply(command);
                lastFeedback = FeedbackForCommand(command);
                selectedInstanceId = FindSelectedCard()?.InstanceId ?? service.State.Player.Tavern.Shop.FirstOrDefault(card => card != null)?.InstanceId;
                if (service.State.LastReplay != null)
                {
                    combatReplayOpen = true;
                    toolsOpen = false;
                    cardDetailOpen = false;
                    activeReplayFrameIndex = 0;
                    replayPlaying = service.State.LastReplay.Frames != null && service.State.LastReplay.Frames.Count > 1;
                    replayPlaybackElapsed = 0f;
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                lastFeedback = null;
            }

            Rebuild();
        }

        private static string FeedbackForCommand(GameCommand command)
        {
            switch (command.Type)
            {
                case GameCommandType.BuyMinion:
                    return "已购买一张牌";
                case GameCommandType.BuyTimewarpedTavernCard:
                    return "已购买时空酒馆卡牌";
                case GameCommandType.ExitTimewarpedTavern:
                    return "已退出时空酒馆";
                case GameCommandType.SellMinion:
                    return "已出售随从";
                case GameCommandType.RerollShop:
                    return "已刷新酒馆";
                case GameCommandType.FreezeShop:
                    return command.Flag ? "已冻结酒馆" : "已解冻酒馆";
                case GameCommandType.UpgradeTavern:
                    return "酒馆已升级";
                case GameCommandType.MoveMinion:
                    return "已回手";
                case GameCommandType.MoveBoardMinion:
                    return "已调整站位";
                case GameCommandType.UpdateMinion:
                case GameCommandType.UpdateOpponentMinion:
                    return "已更新随从";
                case GameCommandType.PlayMinion:
                    return "已打出手牌";
                case GameCommandType.UseHeroPower:
                    return "英雄技能已使用";
                case GameCommandType.ChooseDiscover:
                    return "已选择发现奖励";
                case GameCommandType.ChooseMechanicOption:
                    return "Advanced mechanic selected";
                case GameCommandType.DebugCompleteQuest:
                    return "Quest completed";
                case GameCommandType.DebugReplaceQuestReward:
                    return "Quest reward replaced";
                case GameCommandType.DebugReplaceTrinket:
                    return "Trinket replaced";
                case GameCommandType.NextTurn:
                    return "进入下一回合";
                case GameCommandType.DebugAddGold:
                    return "已增加金币";
                case GameCommandType.SimulateCombat:
                    return "战斗已开始";
                case GameCommandType.AddCardToHand:
                    return "已加入手牌";
                case GameCommandType.DebugCastCard:
                    return "已施放法术";
                case GameCommandType.AddOpponentMinion:
                    return "已加入对手随从";
                case GameCommandType.RemoveOpponentMinion:
                    return "已移除对手随从";
                case GameCommandType.MoveOpponentMinion:
                    return "已调整对手站位";
                case GameCommandType.ClearOpponentBoard:
                    return "已清空对手战场";
                case GameCommandType.CopyPlayerBoardToOpponent:
                    return "已复制到对手战场";
                case GameCommandType.MirrorPlayerBoardToOpponent:
                    return "已镜像到对手战场";
                case GameCommandType.SaveTestScenario:
                    return "已保存测试场景";
                case GameCommandType.LoadTestScenario:
                    return "已加载测试场景";
                case GameCommandType.RunCombatTest:
                    return "战斗测试已运行";
                case GameCommandType.ResetCombatTestSnapshot:
                    return "已重置战斗快照";
                default:
                    return "操作已完成";
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
            UnityTavernUiStyle.SetPreferredHeight(zoneObject, layout.ZoneMetrics(kind, cardMode).Height);
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

            AddDrag(target, CurrentHeroPowerDragCard(heroPower), UnityTavernDragSource.HeroPower, 0);
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
            AddDrag(target, card, source, index);
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

            component.ConfigureInteractionCallbacks(OpenMinionEditor, ShowKeywordTooltip, HideKeywordTooltip);
        }

        private void ShowKeywordTooltip(MinionInstance card, RectTransform anchor)
        {
            HideKeywordTooltip(card);
            if (card == null || card.CardKind == CardKind.TavernSpell)
            {
                return;
            }

            var keywords = EffectiveKeywords(card);
            if (keywords.Count == 0)
            {
                return;
            }

            keywordTooltip = Panel("UnityKeywordTooltip", transform, new Color(0.055f, 0.07f, 0.07f, 0.96f));
            keywordTooltip.transform.SetAsLastSibling();
            var image = keywordTooltip.GetComponent<Image>();
            image.raycastTarget = false;
            UnityTavernUiStyle.ConfigureOutline(keywordTooltip, new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.44f), new Vector2(1f, -1f));

            var rect = keywordTooltip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(292f, Mathf.Min(232f, 38f + keywords.Count * 34f));
            rect.anchoredPosition = TooltipPosition(anchor);

            var layout = keywordTooltip.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 5;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityKeywordTooltipTitle", keywordTooltip.transform, "关键词", 13, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 20f);

            foreach (var keyword in keywords)
            {
                var line = UiFactory.Label("UnityKeywordTooltipLine-" + keyword, keywordTooltip.transform, KeywordName(keyword) + "：" + KeywordDescription(keyword), 11, FontStyle.Normal);
                line.color = UnityTavernUiStyle.Text;
                line.alignment = TextAnchor.MiddleLeft;
                UnityTavernUiStyle.SetPreferredHeight(line.gameObject, 30f);
            }
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

        private void RefreshDropTargetCues()
        {
            var targets = GetComponentsInChildren<UnityTavernDropTargetBehaviour>(true);
            for (var index = 0; index < targets.Length; index += 1)
            {
                targets[index].SetDropCue(activeDrag);
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

        private static void ResourcePill(Transform parent, string label, string value, Color color)
        {
            var pill = Panel("UnityResourcePill-" + label, parent, new Color(color.r, color.g, color.b, 0.86f));
            UnityTavernUiStyle.SetFixedSize(pill, 96f, 54f);
            var layout = pill.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 5, 5);
            layout.spacing = 0;

            var labelText = UiFactory.Label("UnityResourceLabel", pill.transform, label, 10, FontStyle.Bold);
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            var valueText = UiFactory.Label("UnityResourceValue", pill.transform, value, 16, FontStyle.Bold);
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.color = Color.white;
        }

        private static Button SmallButton(string name, Transform parent, string text, Action onClick, float width)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(buttonObject, width, 30f);
            buttonObject.GetComponent<Image>().color = UnityTavernUiStyle.PanelRaised;
            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                new Color(1f, 0.91f, 0.62f, 1f),
                new Color(0.72f, 0.62f, 0.42f, 1f));

            var label = UiFactory.Label(name + "Text", buttonObject.transform, text, 11, FontStyle.Bold);
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
            if (minWidth > 0f || minHeight > 0f || flexibleWidth)
            {
                var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(buttonObject);
                if (minWidth > 0f)
                {
                    element.minWidth = minWidth;
                    element.preferredWidth = minWidth;
                }

                if (minHeight > 0f)
                {
                    element.minHeight = minHeight;
                    element.preferredHeight = minHeight;
                }

                element.flexibleWidth = flexibleWidth ? 1f : 0f;
            }

            var button = buttonObject.GetComponent<Button>();
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

            var label = UiFactory.Label(name + "Text", buttonObject.transform, text, 13, FontStyle.Bold);
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
            rect.sizeDelta = new Vector2(4f, 0f);
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
                return new Color(0.13f, 0.16f, 0.16f, 0.78f);
            }

            switch (role)
            {
                case UnityTavernActionButtonRole.Economy:
                    return new Color(0.34f, 0.25f, 0.12f, 0.98f);
                case UnityTavernActionButtonRole.Primary:
                    return new Color(0.24f, 0.38f, 0.22f, 0.98f);
                case UnityTavernActionButtonRole.Combat:
                    return new Color(0.38f, 0.15f, 0.14f, 0.98f);
                case UnityTavernActionButtonRole.Utility:
                    return new Color(0.16f, 0.30f, 0.39f, 0.96f);
                case UnityTavernActionButtonRole.Danger:
                    return new Color(0.40f, 0.10f, 0.09f, 0.98f);
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

        private static string HandActionLabel(MinionInstance card)
        {
            if (card == null)
            {
                return null;
            }

            return card.CardKind == CardKind.TavernSpell ? "施放" : "上场";
        }

        private static string OpponentHandActionLabel(MinionInstance card)
        {
            return card == null ? null : "删除";
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
