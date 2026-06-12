using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
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
        private static readonly float[] ReplayFrameDurations = { 0.65f, 0.36f, 0.18f };
        private static readonly string[] ReplaySpeedLabels = { "1x", "2x", "4x" };
        private static readonly Tribe[] ToolsAcquisitionTribes =
        {
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

        private MatchService service;
        private IAdvisorService advisor;
        private Action backToHub;
        private string selectedInstanceId;
        private string lastError;
        private string lastFeedback;
        private UnityTavernDragContext activeDrag;
        private GameObject dragGhost;
        private bool rightPanelOpen;
        private bool cardDetailOpen;
        private bool combatReplayOpen;
        private bool toolsOpen;
        private int activeReplayFrameIndex;
        private bool replayPlaying;
        private float replayPlaybackElapsed;
        private int replaySpeedIndex;
        private CardKind toolsAcquisitionKind = CardKind.Minion;
        private int toolsAcquisitionTierFilter;
        private Tribe toolsAcquisitionTribeFilter = Tribe.All;

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
            BuildBackground();
            BuildTopBar();
            BuildPlaySurface();
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

            if (service.State.Player.Tavern.Discover != null)
            {
                BuildDiscoverModal();
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
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

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

            ResourcePill(bar.transform, "回合", service.State.Round.ToString(), UnityTavernUiStyle.TableLit);
            ResourcePill(bar.transform, "金币", service.State.Player.Tavern.Gold + "/" + service.State.Player.Tavern.MaxGold, UnityTavernUiStyle.Gold);
            ResourcePill(bar.transform, "酒馆", service.State.Player.Tavern.Tier + " 本", UnityTavernUiStyle.Blue);
            ResourcePill(bar.transform, "生命", service.State.Player.Health.ToString(), UnityTavernUiStyle.Red);

            var spacer = new GameObject("UnityTopBarSpacer", typeof(RectTransform));
            spacer.transform.SetParent(bar.transform, false);
            UnityTavernUiStyle.SetFlexible(spacer, 1f, 1f);

            SmallButton("UnityBackButton", bar.transform, "返回大厅", () => backToHub?.Invoke(), 96f);
        }

        private void BuildPlaySurface()
        {
            var surface = Panel("UnityPlaySurface", transform, Color.clear);
            var rect = surface.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(18f, 18f);
            rect.offsetMax = new Vector2(-18f, -92f);

            var surfaceLayout = surface.AddComponent<HorizontalLayoutGroup>();
            surfaceLayout.spacing = 14;
            surfaceLayout.childControlWidth = true;
            surfaceLayout.childControlHeight = true;
            surfaceLayout.childForceExpandWidth = true;
            surfaceLayout.childForceExpandHeight = true;

            var center = Panel("UnityTableColumn", surface.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(center, 1f, 1f);
            var centerLayout = center.AddComponent<VerticalLayoutGroup>();
            centerLayout.spacing = 10;
            centerLayout.childControlWidth = true;
            centerLayout.childControlHeight = true;
            centerLayout.childForceExpandWidth = true;
            centerLayout.childForceExpandHeight = false;

            BuildOpponentBoard(center.transform);
            BuildShop(center.transform);
            BuildPlayerBoard(center.transform);
            BuildHand(center.transform);

        }

        private void BuildOpponentBoard(Transform parent)
        {
            var zone = Zone("UnityOpponentBoardZone", parent, 168f, UnityTavernZoneKind.OpponentBoard);
            zone.Build(
                "对手战场",
                service.State.Opponent.Board.Count + "/7",
                service.State.Opponent.Board,
                BoardLimit,
                UnityTavernCardMode.Board,
                card => null,
                SelectCard,
                null,
                (cardObject, card, index) => ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.OpponentBoard, index),
                (slot, index) => AddDropTarget(slot, UnityTavernDropTarget.OpponentBoard, index));
        }

        private void BuildShop(Transform parent)
        {
            var zone = Zone("UnityShopZone", parent, 236f, UnityTavernZoneKind.Shop);
            zone.Build(
                "鲍勃的酒馆",
                service.State.Player.Tavern.Frozen ? "已冻结" : "可刷新",
                service.State.Player.Tavern.Shop,
                0,
                UnityTavernCardMode.Shop,
                card => "购买",
                SelectCard,
                BuyCard,
                (cardObject, card, index) => ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.Shop, index));
        }

        private void BuildPlayerBoard(Transform parent)
        {
            var zone = Zone("UnityPlayerBoardZone", parent, 168f, UnityTavernZoneKind.PlayerBoard);
            zone.Build(
                "玩家战场",
                service.State.Player.Board.Count + "/7",
                service.State.Player.Board,
                BoardLimit,
                UnityTavernCardMode.Board,
                card => "出售",
                SelectCard,
                SellCard,
                (cardObject, card, index) => ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.PlayerBoard, index),
                (slot, index) => AddDropTarget(slot, UnityTavernDropTarget.PlayerBoard, index));
        }

        private void BuildHand(Transform parent)
        {
            var zone = Zone("UnityHandZone", parent, 208f, UnityTavernZoneKind.Hand);
            zone.Build(
                "手牌",
                service.State.Player.Tavern.Hand.Count + "/10",
                service.State.Player.Tavern.Hand,
                HandLimit,
                UnityTavernCardMode.Hand,
                HandActionLabel,
                SelectCard,
                PlayCard,
                (cardObject, card, index) => ConfigureDraggableCard(cardObject, card, UnityTavernDragSource.Hand, index),
                (slot, index) => AddDropTarget(slot, UnityTavernDropTarget.Hand));
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
            panel.GetComponent<UnityTavernRightPanelComponent>().Build(
                "功能面板",
                true,
                ToggleRightPanelDrawer,
                BuildActionStripPrefab,
                BuildSelectedCardPrefab,
                BuildAdvisorPrefab,
                BuildLogPrefab);
        }

        private void ConfigureFloatingRightPanel(GameObject panel)
        {
            var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(panel);
            element.ignoreLayout = true;

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(-450f, 18f);
            rect.offsetMax = new Vector2(-18f, -92f);

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

        private void BuildActionStripPrefab(Transform parent)
        {
            var panel = UnityTavernActionPanelComponent.CreatePanelHost(parent, "UnityActionPanel");
            UnityTavernUiStyle.SetPreferredHeight(panel, 190f);
            panel.GetComponent<UnityTavernActionPanelComponent>().Build(BuildActionButtonsPrefab);
        }

        private void BuildActionButtonsPrefab(Transform parent)
        {
            ActionButton("UnityRefreshButton", parent, "刷新", () => Apply(new GameCommand(GameCommandType.RerollShop)));
            ActionButton("UnityFreezeButton", parent, service.State.Player.Tavern.Frozen ? "解冻" : "冻结", () => Apply(new GameCommand(GameCommandType.FreezeShop, !service.State.Player.Tavern.Frozen)));
            ActionButton("UnityUpgradeButton", parent, "升本", () => Apply(new GameCommand(GameCommandType.UpgradeTavern)));
            ActionButton("UnityNextTurnButton", parent, "下回合", () => Apply(new GameCommand(GameCommandType.NextTurn)));
            ActionButton("UnityCombatButton", parent, "开战", () => ApplyAndOpenReplay(new GameCommand(GameCommandType.SimulateCombat)));
            ActionButton("UnityReplayButton", parent, "回放", OpenCombatReplay);
            ActionButton("UnityToolsButton", parent, "工具", OpenTools);
            BuildSellDropZone(parent);
        }

        private void BuildSelectedCardPrefab(Transform parent)
        {
            var card = FindSelectedCard();
            var detail = UnityTavernSelectedCardPanelComponent.CreatePanelHost(parent, "UnitySelectedCardPanel");
            UnityTavernUiStyle.SetPreferredHeight(detail, 354f);
            detail.GetComponent<UnityTavernSelectedCardPanelComponent>().Build(content => BuildSelectedCardPrefabContent(content, card));
        }

        private void BuildSelectedCardPrefabContent(Transform parent, MinionInstance card)
        {
            if (card == null)
            {
                var empty = UiFactory.Label("UnitySelectedCardEmpty", parent, "选择一张牌查看详情。", 14, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 80f);
                return;
            }

            var cardObject = UnityTavernCardComponent.CreateCardHost(UnityTavernCardMode.Detail, parent, "UnitySelectedCardDetail");
            cardObject.GetComponent<UnityTavernCardComponent>().Bind(card, UnityTavernCardMode.Detail, null, SelectCard, null, true);

            var text = UiFactory.Label("UnitySelectedCardText", parent, card.Text ?? string.Empty, 11, FontStyle.Normal);
            text.color = UnityTavernUiStyle.MutedText;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetFixedSize(text.gameObject, 286f, 58f);

            var details = ActionButton("UnitySelectedCardDetailsButton", parent, "详情", OpenCardDetail);
            UnityTavernUiStyle.SetFixedSize(details.gameObject, 140f, 32f);
        }

        private void BuildAdvisorPrefab(Transform parent)
        {
            var panel = UnityTavernAdvisorPanelComponent.CreatePanelHost(parent, "UnityAdvisorPanel");
            UnityTavernUiStyle.SetPreferredHeight(panel, 112f);
            panel.GetComponent<UnityTavernAdvisorPanelComponent>().Build("建议", BuildAdvisorPrefabLines);
        }

        private void BuildAdvisorPrefabLines(Transform parent)
        {
            foreach (var advice in advisor.GetAdvice(service.State).Take(3))
            {
                var line = UiFactory.Label("UnityAdvisorLine", parent, "- " + advice, 11, FontStyle.Normal);
                line.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(line.gameObject, 22f);
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
            foreach (var message in logs)
            {
                var line = UiFactory.Label("UnityLogLine", parent, message, 11, FontStyle.Normal);
                line.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(line.gameObject, 24f);
            }
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
            Rebuild();
        }

        private void CloseTools()
        {
            toolsOpen = false;
            Rebuild();
        }

        private void BuildToolsModal()
        {
            var modal = UnityTavernToolsModalComponent.CreateModalHost(transform, "UnityTrainerToolsOverlay");
            modal.transform.SetAsLastSibling();
            modal.GetComponent<UnityTavernToolsModalComponent>().Build("训练工具", BuildToolsContent, CloseTools);
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
            });

            BuildToolsCardLibrarySection(parent);

            BuildToolsSection(parent, "UnityToolsOpponentSection", "对手", 4, grid =>
            {
                ToolButton("UnityToolsAddOpponentButton", grid, "加对手", service.State.Opponent.Board.Count < BoardLimit, AddFirstOpponentMinion);
                ToolButton("UnityToolsRemoveOpponentButton", grid, "移除对手", SelectedOpponentCard() != null, RemoveSelectedOpponent);
                ToolButton("UnityToolsClearOpponentButton", grid, "清空对手", service.State.Opponent.Board.Count > 0, () => Apply(new GameCommand(GameCommandType.ClearOpponentBoard)));
                ToolButton("UnityToolsCopyOpponentButton", grid, "复制", service.State.Player.Board.Count > 0, () => Apply(new GameCommand(GameCommandType.CopyPlayerBoardToOpponent)));
                ToolButton("UnityToolsMirrorOpponentButton", grid, "镜像", service.State.Player.Board.Count > 0, () => Apply(new GameCommand(GameCommandType.MirrorPlayerBoardToOpponent)));
            });

            BuildToolsSection(parent, "UnityToolsSelectedSection", "选中卡牌", 4, grid =>
            {
                var selected = FindSelectedCard();
                var canPatch = selected != null && selected.CardKind != CardKind.TavernSpell;
                ToolButton("UnityToolsSelectedAttackPlusButton", grid, "攻+1", canPatch, () => PatchSelected(new MinionPatch { Attack = selected.Attack + 1 }));
                ToolButton("UnityToolsSelectedAttackMinusButton", grid, "攻-1", canPatch, () => PatchSelected(new MinionPatch { Attack = selected.Attack - 1 }));
                ToolButton("UnityToolsSelectedHealthPlusButton", grid, "血+1", canPatch, () => PatchSelected(new MinionPatch { Health = selected.Health + 1, MaxHealth = Math.Max(selected.MaxHealth, selected.Health + 1) }));
                ToolButton("UnityToolsSelectedGoldenButton", grid, "金色", canPatch, () => PatchSelected(new MinionPatch { Golden = !selected.Golden }));
            });

            BuildToolsSection(parent, "UnityToolsCombatSection", "战斗测试", 4, grid =>
            {
                ToolButton("UnityToolsRunCombatTestButton", grid, "运行测试", true, () => ApplyAndOpenReplay(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = DefaultCombatSeed(), SafetyLimit = 200 })));
                ToolButton("UnityToolsResetCombatSnapshotButton", grid, "重置快照", service.HasCombatTestSnapshot, () => Apply(new GameCommand(GameCommandType.ResetCombatTestSnapshot)));
                ToolButton("UnityToolsSaveScenarioButton", grid, "保存场景", true, () => Apply(new GameCommand(GameCommandType.SaveTestScenario, DefaultScenarioName(), new CombatTestOptions())));
                ToolButton("UnityToolsLoadScenarioButton", grid, "加载场景", service.TestScenarioNames.Count > 0, LoadFirstScenario);
            });
        }

        private static void BuildToolsSection(Transform parent, string name, string title, int rows, Action<Transform> buildGrid)
        {
            var section = Panel(name, parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(section, 32f + rows * 40f);
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 10);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var heading = UiFactory.Label(name + "Title", section.transform, title, 13, FontStyle.Bold);
            heading.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(heading.gameObject, 22f);

            var grid = new GameObject(name + "Grid", typeof(RectTransform));
            grid.transform.SetParent(section.transform, false);
            UnityTavernUiStyle.SetFlexible(grid, 1f, 1f);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(0, 0, 0, 0);
            gridLayout.spacing = new Vector2(8f, 6f);
            gridLayout.cellSize = new Vector2(138f, 34f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            buildGrid?.Invoke(grid.transform);
        }

        private static Button ToolButton(string name, Transform parent, string text, bool interactable, Action onClick)
        {
            var button = ActionButton(name, parent, text, onClick);
            button.interactable = interactable;
            return button;
        }

        private void BuildToolsCardLibrarySection(Transform parent)
        {
            var choices = FilteredToolsAcquisitionChoices().ToList();
            var tribeHeight = toolsAcquisitionKind == CardKind.Minion ? 104f : 34f;
            var listHeight = Mathf.Max(48f, choices.Count * 46f + 12f);

            var section = Panel("UnityToolsCardLibrarySection", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(section, 150f + tribeHeight + listHeight);
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 10);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var header = UiFactory.Label("UnityToolsCardLibraryTitle", section.transform, "卡牌库", 13, FontStyle.Bold);
            header.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(header.gameObject, 22f);

            BuildToolsAcquisitionModeRow(section.transform);
            BuildToolsAcquisitionTierRow(section.transform);
            BuildToolsAcquisitionTribeRow(section.transform);

            var count = UiFactory.Label("UnityToolsCardLibraryCountText", section.transform, ToolsAcquisitionSubtitle(choices.Count), 12, FontStyle.Bold);
            count.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(count.gameObject, 22f);

            var list = Panel("UnityToolsCardLibraryList", section.transform, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(list, listHeight);
            var listLayout = list.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(6, 6, 6, 6);
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
            var row = Panel("UnityToolsCardLibraryModeRow", parent, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, 34f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            LibraryFilterButton("UnityToolsCardLibraryMinionModeButton", row.transform, "随从", toolsAcquisitionKind == CardKind.Minion, 92f, () =>
            {
                toolsAcquisitionKind = CardKind.Minion;
                toolsAcquisitionTribeFilter = Tribe.All;
                Rebuild();
            });
            LibraryFilterButton("UnityToolsCardLibrarySpellModeButton", row.transform, "酒馆法术", toolsAcquisitionKind == CardKind.TavernSpell, 112f, () =>
            {
                toolsAcquisitionKind = CardKind.TavernSpell;
                toolsAcquisitionTribeFilter = Tribe.All;
                Rebuild();
            });
        }

        private void BuildToolsAcquisitionTierRow(Transform parent)
        {
            var row = Panel("UnityToolsCardLibraryTierRow", parent, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, 34f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
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

        private void BuildToolsAcquisitionTribeRow(Transform parent)
        {
            var grid = Panel("UnityToolsCardLibraryTribeGrid", parent, Color.clear);
            if (toolsAcquisitionKind != CardKind.Minion)
            {
                UnityTavernUiStyle.SetPreferredHeight(grid, 34f);
                var layout = grid.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 8;
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
                LibraryFilterButton("UnityToolsCardLibraryTribeAllButton", grid.transform, "全部", true, 84f, () => { });
                LibraryFilterButton("UnityToolsCardLibraryTavernSpellTypeButton", grid.transform, "酒馆法术", true, 112f, () => { });
                return;
            }

            UnityTavernUiStyle.SetPreferredHeight(grid, 104f);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.spacing = new Vector2(6f, 6f);
            gridLayout.cellSize = new Vector2(136f, 30f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;

            LibraryFilterButton("UnityToolsCardLibraryTribeAllButton", grid.transform, "全部", toolsAcquisitionTribeFilter == Tribe.All, 136f, () =>
            {
                toolsAcquisitionTribeFilter = Tribe.All;
                Rebuild();
            });

            foreach (var tribe in ToolsAcquisitionTribes)
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
            UnityTavernUiStyle.SetPreferredHeight(row, 40f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var name = UiFactory.Label("UnityToolsCardLibraryChoiceName", row.transform, card.Name, 12, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(name.gameObject, 1f, 0f);

            var meta = UiFactory.Label("UnityToolsCardLibraryChoiceMeta", row.transform, ToolsAcquisitionCardMeta(card), 11, FontStyle.Normal);
            meta.alignment = TextAnchor.MiddleRight;
            meta.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(meta.gameObject, 210f, 30f);

            var addButtonName = index == 0 ? "UnityToolsCardLibraryAddButton" : "UnityToolsCardLibraryAddButton-" + SafeObjectName(card.CardId);
            var add = ToolButton(addButtonName, row.transform, "加入", service.State.Player.Tavern.Hand.Count < HandLimit, () =>
            {
                Apply(new GameCommand(GameCommandType.AddCardToHand, card.CardId, card.CardKind));
            });
            UnityTavernUiStyle.SetFixedSize(add.gameObject, 64f, 30f);
        }

        private Button LibraryFilterButton(string name, Transform parent, string text, bool active, float width, Action onClick)
        {
            var button = ActionButton(name, parent, text, onClick);
            UnityTavernUiStyle.SetFixedSize(button.gameObject, width, 30f);
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? new Color(0.56f, 0.38f, 0.16f, 0.96f) : UnityTavernUiStyle.PanelRaised;
            }

            return button;
        }

        private string ToolsAcquisitionSubtitle(int visibleCount)
        {
            var kind = toolsAcquisitionKind == CardKind.TavernSpell ? "酒馆法术" : "随从";
            var tier = toolsAcquisitionTierFilter == 0 ? "全部等级" : toolsAcquisitionTierFilter + "本";
            var tribe = toolsAcquisitionKind == CardKind.Minion ? TribeName(toolsAcquisitionTribeFilter) : "酒馆法术";
            return kind + " / " + tier + " / " + tribe + " / " + visibleCount + "张 / 手牌 " + service.State.Player.Tavern.Hand.Count + "/" + HandLimit;
        }

        private IEnumerable<MinionInstance> FilteredToolsAcquisitionChoices()
        {
            var choices = toolsAcquisitionKind == CardKind.Minion
                ? BuildToolsAcquisitionMinionChoices()
                : BuildToolsAcquisitionSpellChoices();

            if (toolsAcquisitionTierFilter > 0)
            {
                choices = choices.Where(card => card.TavernTier == toolsAcquisitionTierFilter);
            }

            if (toolsAcquisitionKind == CardKind.Minion && toolsAcquisitionTribeFilter != Tribe.All)
            {
                choices = choices.Where(card => card.Tribes != null && card.Tribes.Contains(toolsAcquisitionTribeFilter));
            }

            return choices
                .OrderBy(card => card.TavernTier)
                .ThenBy(card => card.Name)
                .Take(80)
                .ToList();
        }

        private IEnumerable<MinionInstance> BuildToolsAcquisitionMinionChoices()
        {
            foreach (var definition in MinionCatalogLoader.LoadFromResources().All.Where(card => card.InPool && !card.CardId.StartsWith("BGDUO")))
            {
                yield return MinionFactory.Create(definition, BoardSide.Player, "unity-tools-library", false, PoolSource.Debug, 0);
            }
        }

        private IEnumerable<MinionInstance> BuildToolsAcquisitionSpellChoices()
        {
            foreach (var definition in SpellCatalogLoader.LoadFromResources().All.Where(spell => spell.InPool && spell.Category == "TavernSpell" && !spell.CardNumber.StartsWith("BGDUO")))
            {
                var spell = MinionFactory.Create(definition, BoardSide.Player, "unity-tools-library");
                spell.PoolSource = PoolSource.Debug;
                spell.OriginPoolSource = PoolSource.Debug;
                yield return spell;
            }
        }

        private static string ToolsAcquisitionCardMeta(MinionInstance card)
        {
            if (card.CardKind == CardKind.TavernSpell)
            {
                return card.TavernTier + "本 / " + card.Cost + "费 / 法术";
            }

            return card.TavernTier + "本 / " + card.Attack + "/" + card.Health + " / " + TribesText(card);
        }

        private static string TribesText(MinionInstance card)
        {
            if (card.Tribes == null || card.Tribes.Count == 0)
            {
                return "无种族";
            }

            var tribes = card.Tribes.Where(tribe => tribe != Tribe.None).Take(2).Select(TribeName).ToArray();
            return tribes.Length == 0 ? "无种族" : string.Join("/", tribes);
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
                default: return "无";
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
            var definition = MinionCatalogLoader.LoadFromResources().All.FirstOrDefault(card => card.InPool && card.TavernTier <= Math.Max(1, service.State.Player.Tavern.Tier));
            if (definition != null)
            {
                Apply(new GameCommand(GameCommandType.AddCardToHand, definition.CardId, CardKind.Minion));
            }
        }

        private void AddFirstSpellToHand()
        {
            var definition = SpellCatalogLoader.LoadFromResources().All.FirstOrDefault(spell => spell.InPool && spell.Category == "TavernSpell");
            if (definition != null)
            {
                Apply(new GameCommand(GameCommandType.AddCardToHand, definition.CardNumber, CardKind.TavernSpell));
            }
        }

        private void AddFirstOpponentMinion()
        {
            var definition = MinionCatalogLoader.LoadFromResources().All.FirstOrDefault(card => card.InPool && card.TavernTier <= Math.Max(1, service.State.Player.Tavern.Tier));
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

        private void BuildSellDropZone(Transform parent)
        {
            var zone = Panel("UnitySellDropZone", parent, new Color(0.32f, 0.09f, 0.08f, 0.88f));
            AddDropTarget(zone, UnityTavernDropTarget.SellZone);

            var label = UiFactory.Label("UnitySellDropZoneText", zone.transform, "出售", 13, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private void BuildDiscoverModal()
        {
            var modal = UnityTavernDiscoverModalComponent.CreateModalHost(transform, "UnityDiscoverOverlay");
            modal.GetComponent<UnityTavernDiscoverModalComponent>().Build("发现奖励", BuildDiscoverOptions);
        }

        private void BuildDiscoverOptions(Transform parent)
        {
            var options = service.State.Player.Tavern.Discover.Options;
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

        public void BeginDrag(MinionInstance card, UnityTavernDragSource source, int index, PointerEventData eventData = null)
        {
            if (card == null)
            {
                return;
            }

            activeDrag = new UnityTavernDragContext(card, source, index);
            selectedInstanceId = card.InstanceId;
            if (eventData != null)
            {
                CreateDragGhost(card, eventData);
            }
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

            selectedInstanceId = target == UnityTavernDropTarget.SellZone ? null : drag.Card.InstanceId;
            activeDrag = null;
            DestroyDragGhost();
            Apply(command);
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

        private void SelectCard(MinionInstance card)
        {
            if (card == null)
            {
                return;
            }

            selectedInstanceId = card.InstanceId;
            Rebuild();
        }

        private void BuyCard(MinionInstance card)
        {
            var index = service.State.Player.Tavern.Shop.FindIndex(item => item.InstanceId == card.InstanceId);
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
                Apply(new GameCommand(GameCommandType.PlayMinion, index));
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
                case GameCommandType.ChooseDiscover:
                    return "已选择发现奖励";
                case GameCommandType.NextTurn:
                    return "进入下一回合";
                case GameCommandType.DebugAddGold:
                    return "已增加金币";
                case GameCommandType.SimulateCombat:
                    return "战斗已开始";
                case GameCommandType.AddCardToHand:
                    return "已加入手牌";
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

        private UnityTavernZoneComponent Zone(string name, Transform parent, float height, UnityTavernZoneKind kind)
        {
            var zoneObject = UnityTavernZoneComponent.CreateZoneHost(kind, parent, name);
            UnityTavernUiStyle.SetPreferredHeight(zoneObject, height);
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

        private void ConfigureDraggableCard(GameObject target, MinionInstance card, UnityTavernDragSource source, int index)
        {
            ConfigureCardFeedback(target, card);
            AddDrag(target, card, source, index);
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

        private void AddDropTarget(GameObject target, UnityTavernDropTarget dropTarget, int targetIndex = -1)
        {
            if (target == null)
            {
                return;
            }

            var image = UnityTavernUiStyle.EnsureComponent<Image>(target);
            image.raycastTarget = true;

            var behaviour = UnityTavernUiStyle.EnsureComponent<UnityTavernDropTargetBehaviour>(target);
            behaviour.Initialize(this, dropTarget, targetIndex);
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
            UnityTavernUiStyle.SetFixedSize(buttonObject, width, 54f);
            buttonObject.GetComponent<Image>().color = UnityTavernUiStyle.PanelRaised;
            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                new Color(1f, 0.91f, 0.62f, 1f),
                new Color(0.72f, 0.62f, 0.42f, 1f));

            var label = UiFactory.Label(name + "Text", buttonObject.transform, text, 13, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return button;
        }

        private static Button ActionButton(string name, Transform parent, string text, Action onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = UnityTavernUiStyle.PanelRaised;
            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                new Color(1f, 0.91f, 0.62f, 1f),
                new Color(0.72f, 0.62f, 0.42f, 1f));

            var label = UiFactory.Label(name + "Text", buttonObject.transform, text, 13, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return button;
        }

        private static string HandActionLabel(MinionInstance card)
        {
            if (card == null)
            {
                return null;
            }

            return card.CardKind == CardKind.TavernSpell ? "施放" : "上场";
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
