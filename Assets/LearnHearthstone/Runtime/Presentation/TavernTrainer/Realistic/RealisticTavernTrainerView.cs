using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.Realistic
{
    public sealed class RealisticTavernTrainerView
    {
        private const int BoardLimit = 7;
        private const int HandLimit = 10;
        private readonly Transform root;
        private readonly MatchService service;
        private readonly IAdvisorService advisor;
        private readonly Action backToHub;
        private readonly Action openLegacyTools;
        private GameObject shell;
        private string selectedCardId;
        private string lastError;
        private DragContext activeDrag;
        private GameObject dragGhost;
        private RealisticDrawerTab activeDrawerTab = RealisticDrawerTab.Info;
        private int activeReplayFrameIndex;

        public RealisticTavernTrainerView(
            Transform root,
            MatchService service,
            IAdvisorService advisor,
            Action backToHub,
            Action openLegacyTools = null)
        {
            this.root = root;
            this.service = service;
            this.advisor = advisor;
            this.backToHub = backToHub;
            this.openLegacyTools = openLegacyTools;
        }

        public void Build()
        {
            DestroyShell();
            shell = Panel("RealisticTavernTrainer", root, ColorFromHex(0x17100B));
            UiFactory.Stretch(shell.GetComponent<RectTransform>());

            BuildBackground(shell.transform);
            BuildTopStatusBar(shell.transform);
            BuildMainTable(shell.transform);
            BuildHandDock(shell.transform);
            BuildRightRail(shell.transform);

            if (service.State.Player.Tavern.Discover != null)
            {
                BuildDiscoverOverlay(shell.transform);
            }

            if (!string.IsNullOrEmpty(lastError))
            {
                BuildToast(shell.transform, lastError);
            }
        }

        internal void BeginDrag(MinionInstance card, RealisticDragSource source, int index, PointerEventData eventData)
        {
            activeDrag = new DragContext
            {
                Card = card,
                Source = source,
                Index = index
            };
            selectedCardId = card?.InstanceId;
            CreateDragGhost(card, eventData);
        }

        internal void MoveDrag(PointerEventData eventData)
        {
            if (dragGhost == null)
            {
                return;
            }

            MoveDragGhost(eventData);
        }

        internal void EndDrag()
        {
            activeDrag = null;
            DestroyDragGhost();
        }

        internal void HandleDrop(RealisticDropTarget target, int targetIndex)
        {
            if (activeDrag == null)
            {
                return;
            }

            if (!TryBuildDropCommand(activeDrag, target, targetIndex, out var command))
            {
                lastError = "请拖到正确区域。";
                EndDrag();
                Build();
                return;
            }

            selectedCardId = target == RealisticDropTarget.SellZone ? null : activeDrag.Card.InstanceId;
            activeDrag = null;
            DestroyDragGhost();
            Apply(command);
        }

        private void BuildBackground(Transform parent)
        {
            Panel("TavernBackWall", parent, ColorFromHex(0x241611), new Vector2(0f, 0.48f), Vector2.one, new Vector2(0f, 0f), Vector2.zero);
            Panel("TavernTableWood", parent, ColorFromHex(0x3A2417), Vector2.zero, new Vector2(1f, 0.76f), Vector2.zero, Vector2.zero);
            Panel("TavernTableInnerGlow", parent, new Color(0.75f, 0.42f, 0.18f, 0.16f), new Vector2(0.08f, 0.18f), new Vector2(0.82f, 0.68f), Vector2.zero, Vector2.zero);
            Panel("TavernBottomShadow", parent, new Color(0.02f, 0.012f, 0.008f, 0.42f), Vector2.zero, new Vector2(1f, 0.24f), Vector2.zero, Vector2.zero);
        }

        private void BuildTopStatusBar(Transform parent)
        {
            var bar = Panel("RealisticTopStatusBar", parent, new Color(0.07f, 0.055f, 0.045f, 0.96f), new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -86f), Vector2.zero);
            UiFactory.Horizontal(bar, 18, 10);

            var title = StackPanel("TavernTitleBlock", bar.transform, 240f);
            Label(title.transform, "酒馆战棋训练器", 22, FontStyle.Bold, ColorFromHex(0xF2D598), TextAnchor.MiddleLeft);
            Label(title.transform, "真实酒馆式界面 / 单人训练", 12, FontStyle.Normal, ColorFromHex(0xC8B38B), TextAnchor.MiddleLeft);

            ResourcePill(bar.transform, "回合", service.State.Round.ToString(), ColorFromHex(0x594025));
            ResourcePill(bar.transform, "金币", service.State.Player.Tavern.Gold + "/" + service.State.Player.Tavern.MaxGold, ColorFromHex(0x9B6A20));
            ResourcePill(bar.transform, "酒馆", service.State.Player.Tavern.Tier + " 本", ColorFromHex(0x2E5C7D));
            ResourcePill(bar.transform, "升级", service.State.Player.Tavern.UpgradeCost.ToString(), ColorFromHex(0x335B3F));
            ResourcePill(bar.transform, "生命", service.State.Player.Health.ToString(), ColorFromHex(0x763235));
            ResourcePill(bar.transform, "对手", service.State.Opponent.Board.Count + "/7", ColorFromHex(0x4A3B6C));

            var spacer = UiFactory.Panel("TopStatusSpacer", bar.transform, Color.clear);
            UiFactory.SetFlexible(spacer, 1f, 1f);

            SmallButton("RealisticBackButton", bar.transform, "返回", true, () => backToHub?.Invoke(), 84f);
        }

        private void BuildMainTable(Transform parent)
        {
            var table = Panel("RealisticMainTable", parent, Color.clear, new Vector2(0f, 0.22f), new Vector2(1f, 1f), new Vector2(18f, 0f), new Vector2(-404f, -96f));

            var keeper = Panel("TavernKeeperStage", table.transform, new Color(0.10f, 0.07f, 0.05f, 0.72f), new Vector2(0f, 0.77f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            UiFactory.Vertical(keeper, 18, 2);
            Label(keeper.transform, "鲍勃的酒馆", 22, FontStyle.Bold, ColorFromHex(0xF1C968), TextAnchor.MiddleCenter);
            Label(keeper.transform, "升级酒馆、冻结商店、挑选随从", 12, FontStyle.Normal, ColorFromHex(0xD7C7A3), TextAnchor.LowerCenter);

            BuildShopStage(table.transform);
            BuildPlayerBoard(table.transform);
        }

        private void BuildShopStage(Transform parent)
        {
            var stage = Panel("RealisticShopStage", parent, new Color(0.18f, 0.10f, 0.055f, 0.86f), new Vector2(0f, 0.48f), new Vector2(1f, 0.78f), Vector2.zero, Vector2.zero);
            UiFactory.Vertical(stage, 12, 8);

            var header = UiFactory.Panel("RealisticShopHeader", stage.transform, Color.clear);
            UiFactory.SetHeight(header, 28);
            UiFactory.Horizontal(header, 0, 8);
            Label(header.transform, "商店", 16, FontStyle.Bold, ColorFromHex(0xF2D598), TextAnchor.MiddleLeft);
            var frozenText = service.State.Player.Tavern.Frozen ? "已冻结" : "未冻结";
            Label(header.transform, frozenText, 13, FontStyle.Bold, service.State.Player.Tavern.Frozen ? ColorFromHex(0x82D5FF) : ColorFromHex(0xC8B38B), TextAnchor.MiddleRight);

            var row = UiFactory.Panel("RealisticShopRow", stage.transform, service.State.Player.Tavern.Frozen ? new Color(0.14f, 0.22f, 0.28f, 0.68f) : Color.clear);
            UiFactory.SetFlexible(row, 1f, 1f);
            UiFactory.Horizontal(row, 10, 8);
            AddDropTarget(row, RealisticDropTarget.Hand, -1);

            for (var index = 0; index < service.State.Player.Tavern.Shop.Count; index += 1)
            {
                var slot = Slot("RealisticShopSlot-" + index, row.transform, ColorFromHex(0x2E2017), 134f, 188f);
                var card = service.State.Player.Tavern.Shop[index];
                if (card != null)
                {
                    var cardObject = TavernCardView.Create(slot.transform, card, TavernCardVisualMode.Shop, SelectCard);
                    AddDrag(cardObject, card, RealisticDragSource.Shop, index);
                }
            }
        }

        private void BuildPlayerBoard(Transform parent)
        {
            var board = Panel("RealisticPlayerBoard", parent, new Color(0.12f, 0.075f, 0.045f, 0.84f), new Vector2(0f, 0.09f), new Vector2(1f, 0.43f), Vector2.zero, Vector2.zero);
            UiFactory.Vertical(board, 12, 8);

            var header = UiFactory.Panel("RealisticBoardHeader", board.transform, Color.clear);
            UiFactory.SetHeight(header, 26);
            UiFactory.Horizontal(header, 0, 8);
            Label(header.transform, "玩家战场", 16, FontStyle.Bold, ColorFromHex(0xF2D598), TextAnchor.MiddleLeft);
            Label(header.transform, "7 个槽位", 12, FontStyle.Bold, ColorFromHex(0xC8B38B), TextAnchor.MiddleRight);

            var row = UiFactory.Panel("RealisticBoardRow", board.transform, Color.clear);
            UiFactory.SetFlexible(row, 1f, 1f);
            UiFactory.Horizontal(row, 10, 8);

            for (var index = 0; index < BoardLimit; index += 1)
            {
                var slot = Slot("RealisticBoardSlot-" + index, row.transform, ColorFromHex(0x2B2118), 112f, 126f);
                AddDropTarget(slot, RealisticDropTarget.PlayerBoard, index);
                var card = index < service.State.Player.Board.Count ? service.State.Player.Board[index] : null;
                if (card == null)
                {
                    Label(slot.transform, (index + 1).ToString(), 14, FontStyle.Bold, ColorFromHex(0x8C7658), TextAnchor.MiddleCenter);
                    continue;
                }

                var cardObject = TavernCardView.Create(slot.transform, card, TavernCardVisualMode.Board, SelectCard);
                AddDrag(cardObject, card, RealisticDragSource.PlayerBoard, index);
                AddDropTarget(cardObject, RealisticDropTarget.PlayerBoard, index);
            }
        }

        private void BuildHandDock(Transform parent)
        {
            var dock = Panel("RealisticHandDock", parent, new Color(0.07f, 0.045f, 0.032f, 0.98f), Vector2.zero, new Vector2(1f, 0.22f), new Vector2(18f, 12f), new Vector2(-18f, 0f));
            UiFactory.Horizontal(dock, 12, 8);
            AddDropTarget(dock, RealisticDropTarget.Hand, -1);

            var labelBlock = StackPanel("RealisticHandLabel", dock.transform, 86f);
            Label(labelBlock.transform, "手牌", 18, FontStyle.Bold, ColorFromHex(0xF2D598), TextAnchor.MiddleLeft);
            Label(labelBlock.transform, service.State.Player.Tavern.Hand.Count + "/" + HandLimit, 13, FontStyle.Bold, ColorFromHex(0xC8B38B), TextAnchor.MiddleLeft);

            for (var index = 0; index < HandLimit; index += 1)
            {
                var slot = Slot("RealisticHandSlot-" + index, dock.transform, ColorFromHex(0x231810), 112f, 154f);
                var card = index < service.State.Player.Tavern.Hand.Count ? service.State.Player.Tavern.Hand[index] : null;
                if (card == null)
                {
                    Label(slot.transform, "空", 11, FontStyle.Bold, ColorFromHex(0x7F6B50), TextAnchor.MiddleCenter);
                    continue;
                }

                var cardObject = TavernCardView.Create(slot.transform, card, TavernCardVisualMode.Hand, SelectCard);
                AddDrag(cardObject, card, RealisticDragSource.Hand, index);
            }
        }

        private void BuildRightRail(Transform parent)
        {
            var rail = Panel("RealisticRightRail", parent, new Color(0.08f, 0.06f, 0.05f, 0.96f), new Vector2(1f, 0.22f), new Vector2(1f, 1f), new Vector2(-386f, 0f), new Vector2(-16f, -96f));
            UiFactory.Vertical(rail, 12, 10);

            BuildActionPanel(rail.transform);
            BuildSelectedPanel(rail.transform);
            BuildTrainerDrawer(rail.transform);
        }

        private void BuildActionPanel(Transform parent)
        {
            var panel = UiFactory.Panel("RealisticActionPanel", parent, ColorFromHex(0x2D2117));
            UiFactory.SetHeight(panel, 238f);
            UiFactory.Vertical(panel, 10, 8);
            Label(panel.transform, "主要操作", 15, FontStyle.Bold, ColorFromHex(0xF2D598), TextAnchor.MiddleLeft);

            var firstRow = UiFactory.Panel("RealisticActionRowA", panel.transform, Color.clear);
            UiFactory.SetHeight(firstRow, 38f);
            UiFactory.Horizontal(firstRow, 0, 6);
            SmallButton("RealisticRefreshButton", firstRow.transform, "刷新 1", service.State.Player.Tavern.Gold >= 1, () => Apply(new GameCommand(GameCommandType.RerollShop)));
            SmallButton("RealisticFreezeButton", firstRow.transform, service.State.Player.Tavern.Frozen ? "解冻" : "冻结", true, () => Apply(new GameCommand(GameCommandType.FreezeShop, !service.State.Player.Tavern.Frozen)));

            var secondRow = UiFactory.Panel("RealisticActionRowB", panel.transform, Color.clear);
            UiFactory.SetHeight(secondRow, 38f);
            UiFactory.Horizontal(secondRow, 0, 6);
            SmallButton("RealisticUpgradeButton", secondRow.transform, "升级 " + service.State.Player.Tavern.UpgradeCost, service.State.Player.Tavern.UpgradeCost > 0 && service.State.Player.Tavern.Gold >= service.State.Player.Tavern.UpgradeCost, () => Apply(new GameCommand(GameCommandType.UpgradeTavern)));
            SmallButton("RealisticNextTurnButton", secondRow.transform, "下回合", true, () => Apply(new GameCommand(GameCommandType.NextTurn)));

            var thirdRow = UiFactory.Panel("RealisticActionRowC", panel.transform, Color.clear);
            UiFactory.SetHeight(thirdRow, 36f);
            UiFactory.Horizontal(thirdRow, 0, 6);
            SmallButton("RealisticCombatButton", thirdRow.transform, "模拟战斗", true, () => Apply(new GameCommand(GameCommandType.SimulateCombat)), 148f);

            var sellZone = UiFactory.Panel("RealisticSellZone", panel.transform, ColorFromHex(0x4C1D1B));
            UiFactory.SetHeight(sellZone, 44f);
            AddDropTarget(sellZone, RealisticDropTarget.SellZone, -1);
            Label(sellZone.transform, "拖到这里出售 +1 金币", 13, FontStyle.Bold, ColorFromHex(0xF0C6C6), TextAnchor.MiddleCenter);
        }

        private void BuildSelectedPanel(Transform parent)
        {
            var panel = UiFactory.Panel("RealisticSelectedPanel", parent, ColorFromHex(0x211A14));
            UiFactory.SetHeight(panel, 218f);
            UiFactory.Vertical(panel, 10, 7);
            Label(panel.transform, "卡牌详情", 15, FontStyle.Bold, ColorFromHex(0xF2D598), TextAnchor.MiddleLeft);

            var selected = FindSelectedCard();
            if (selected == null)
            {
                Label(panel.transform, "选择或拖动一张牌查看详情。", 12, FontStyle.Normal, ColorFromHex(0xBDAA84), TextAnchor.MiddleLeft);
                return;
            }

            var row = UiFactory.Panel("RealisticSelectedRow", panel.transform, Color.clear);
            UiFactory.SetFlexible(row, 1f, 1f);
            UiFactory.Horizontal(row, 0, 8);
            TavernCardView.Create(row.transform, selected, TavernCardVisualMode.Board, SelectCard);

            var textBlock = UiFactory.Panel("RealisticSelectedText", row.transform, Color.clear);
            UiFactory.SetFlexible(textBlock, 1f, 1f);
            UiFactory.Vertical(textBlock, 0, 3);
            Label(textBlock.transform, selected.Name, 13, FontStyle.Bold, ColorFromHex(0xF4E4BC), TextAnchor.MiddleLeft);
            Label(textBlock.transform, selected.Text ?? string.Empty, 11, FontStyle.Normal, ColorFromHex(0xD7C7A3), TextAnchor.UpperLeft);
        }

        private void BuildTrainerDrawer(Transform parent)
        {
            var drawer = UiFactory.Panel("RealisticTrainerDrawer", parent, ColorFromHex(0x1C1713));
            UiFactory.SetFlexible(drawer, 1f, 1f);
            UiFactory.Vertical(drawer, 10, 8);

            var header = UiFactory.Panel("RealisticDrawerHeader", drawer.transform, Color.clear);
            UiFactory.SetHeight(header, 30f);
            UiFactory.Horizontal(header, 0, 8);
            Label(header.transform, "训练器抽屉", 15, FontStyle.Bold, ColorFromHex(0xF2D598), TextAnchor.MiddleLeft);
            SmallButton("RealisticLegacyTrainerButton", header.transform, "旧工具", openLegacyTools != null, () => openLegacyTools?.Invoke(), 82f);

            BuildDrawerTabs(drawer.transform);

            var content = UiFactory.ScrollView("RealisticDrawerScroll", drawer.transform, ColorFromHex(0x18130F), out _);
            UiFactory.Vertical(content.gameObject, 0, 8);
            switch (activeDrawerTab)
            {
                case RealisticDrawerTab.Opponent:
                    BuildOpponentDrawer(content);
                    break;
                case RealisticDrawerTab.Battle:
                    BuildBattleDrawer(content);
                    break;
                case RealisticDrawerTab.Logs:
                    BuildLogsDrawer(content);
                    break;
                case RealisticDrawerTab.Debug:
                    BuildDebugDrawer(content);
                    break;
                default:
                    BuildInfoDrawer(content);
                    break;
            }
        }

        private void BuildDrawerTabs(Transform parent)
        {
            var tabs = UiFactory.Panel("RealisticDrawerTabs", parent, ColorFromHex(0x120E0B));
            UiFactory.SetHeight(tabs, 38f);
            UiFactory.Horizontal(tabs, 3, 4);
            DrawerTabButton(tabs.transform, "RealisticDrawerTabInfo", "信息", RealisticDrawerTab.Info);
            DrawerTabButton(tabs.transform, "RealisticDrawerTabOpponent", "对手", RealisticDrawerTab.Opponent);
            DrawerTabButton(tabs.transform, "RealisticDrawerTabBattle", "战斗", RealisticDrawerTab.Battle);
            DrawerTabButton(tabs.transform, "RealisticDrawerTabLogs", "日志", RealisticDrawerTab.Logs);
            DrawerTabButton(tabs.transform, "RealisticDrawerTabDebug", "调试", RealisticDrawerTab.Debug);
        }

        private void DrawerTabButton(Transform parent, string name, string text, RealisticDrawerTab tab)
        {
            var button = SmallButton(name, parent, text, true, () =>
            {
                activeDrawerTab = tab;
                Build();
            }, 64f);
            UiFactory.SetFlexible(button.gameObject, 1f, 1f);
            UiFactory.SetImageColor(button.gameObject, activeDrawerTab == tab ? ColorFromHex(0x5A3C22) : ColorFromHex(0x2B2118));
        }

        private void BuildInfoDrawer(Transform parent)
        {
            var panel = UiFactory.Panel("RealisticInfoDrawerPanel", parent, ColorFromHex(0x211A14));
            UiFactory.SetHeight(panel, 630f);
            UiFactory.Vertical(panel, 10, 8);
            BuildPanelHeader(panel.transform, "当前信息", service.State.Player.Board.Count + "/7");
            BuildRealisticBoardTribeDistribution(panel.transform);
            BuildSelectedStatEditor(panel.transform);
            BuildOpponentPreview(panel.transform);
            BuildLogPreview(panel.transform);
        }

        private void BuildSelectedStatEditor(Transform parent)
        {
            var selected = FindSelectedCard();
            var editor = UiFactory.Panel("RealisticMinionEditor", parent, ColorFromHex(0x18130F));
            UiFactory.SetHeight(editor, selected == null ? 108f : 292f);
            UiFactory.Vertical(editor, 8, 6);
            BuildPanelHeader(editor.transform, "随从编辑", selected == null ? "未选择" : selected.CardKind.ToString());
            if (selected == null)
            {
                EmptyText(editor.transform, "选择商店、手牌或战场里的卡牌后编辑。");
                return;
            }

            var title = Label(editor.transform, selected.Name, 13, FontStyle.Bold, ColorFromHex(0xF4E4BC), TextAnchor.MiddleLeft);
            title.gameObject.name = "RealisticSelectedName";
            if (selected.CardKind != CardKind.Minion)
            {
                EmptyText(editor.transform, "法术会通过拖到战场目标或空位来施放。");
                return;
            }

            Stepper(editor.transform, "攻击", selected.Attack, value => UpdateSelected(new MinionPatch { Attack = value }));
            Stepper(editor.transform, "生命", selected.Health, value => UpdateSelected(new MinionPatch { Health = value }));
            Stepper(editor.transform, "最大生命", selected.MaxHealth, value => UpdateSelected(new MinionPatch { MaxHealth = value }));

            var toggles = UiFactory.Panel("RealisticEditorToggles", editor.transform, Color.clear);
            UiFactory.SetHeight(toggles, 34f);
            UiFactory.Horizontal(toggles, 0, 6);
            SmallButton("RealisticGoldenToggle", toggles.transform, selected.Golden ? "金色" : "普通", true, () => UpdateSelected(new MinionPatch { Golden = !selected.Golden }), 72f);
            KeywordToggle(toggles.transform, selected, Keyword.Taunt, "嘲讽");
            KeywordToggle(toggles.transform, selected, Keyword.DivineShield, "圣盾");

            var moreToggles = UiFactory.Panel("RealisticEditorMoreToggles", editor.transform, Color.clear);
            UiFactory.SetHeight(moreToggles, 34f);
            UiFactory.Horizontal(moreToggles, 0, 6);
            KeywordToggle(moreToggles.transform, selected, Keyword.Reborn, "复生");
            KeywordToggle(moreToggles.transform, selected, Keyword.Poisonous, "剧毒");
            KeywordToggle(moreToggles.transform, selected, Keyword.Venomous, "烈毒");
        }

        private void BuildOpponentDrawer(Transform parent)
        {
            var panel = UiFactory.Panel("RealisticOpponentCustomizationPanel", parent, ColorFromHex(0x211A14));
            UiFactory.SetHeight(panel, 790f);
            UiFactory.Vertical(panel, 10, 8);
            BuildPanelHeader(panel.transform, "自定义对手", service.State.Opponent.Board.Count + "/7");
            if (!string.IsNullOrEmpty(lastError))
            {
                LogLine(panel.transform, lastError);
            }

            BuildOpponentEditorBoard(panel.transform);
            BuildOpponentBulkActions(panel.transform);
            BuildOpponentCardSource(panel.transform);

            var selected = service.State.Opponent.Board.FirstOrDefault(card => card.InstanceId == selectedCardId);
            BuildOpponentToolbar(panel.transform, selected);
            BuildOpponentStatEditor(panel.transform, selected);
        }

        private void BuildOpponentEditorBoard(Transform parent)
        {
            var row = UiFactory.Panel("RealisticOpponentEditorSlots", parent, ColorFromHex(0x18130F));
            UiFactory.SetHeight(row, 88f);
            UiFactory.Horizontal(row, 6, 4);
            AddDropTarget(row, RealisticDropTarget.OpponentBoard, -1);
            for (var index = 0; index < BoardLimit; index += 1)
            {
                var slot = Slot("RealisticOpponentEditorSlot-" + index, row.transform, ColorFromHex(0x332821), 44f, 70f);
                AddDropTarget(slot, RealisticDropTarget.OpponentBoard, index);
                var card = index < service.State.Opponent.Board.Count ? service.State.Opponent.Board[index] : null;
                if (card == null)
                {
                    Label(slot.transform, (index + 1).ToString(), 11, FontStyle.Bold, ColorFromHex(0x8C7658), TextAnchor.MiddleCenter);
                    continue;
                }

                var token = MiniBoardToken("RealisticOpponentToken-" + index, slot.transform, card, true);
                AddDrag(token, card, RealisticDragSource.OpponentBoard, index);
                AddDropTarget(token, RealisticDropTarget.OpponentBoard, index);
            }
        }

        private void BuildOpponentBulkActions(Transform parent)
        {
            var actions = UiFactory.Panel("RealisticOpponentBulkActions", parent, Color.clear);
            UiFactory.SetHeight(actions, 36f);
            UiFactory.Horizontal(actions, 0, 6);
            SmallButton("RealisticClearOpponentButton", actions.transform, "清空", service.State.Opponent.Board.Count > 0, () =>
            {
                selectedCardId = null;
                Apply(new GameCommand(GameCommandType.ClearOpponentBoard));
            });
            SmallButton("RealisticCopyOpponentButton", actions.transform, "复制玩家", service.State.Player.Board.Count > 0, () => Apply(new GameCommand(GameCommandType.CopyPlayerBoardToOpponent)));
            SmallButton("RealisticMirrorOpponentButton", actions.transform, "镜像玩家", service.State.Player.Board.Count > 0, () => Apply(new GameCommand(GameCommandType.MirrorPlayerBoardToOpponent)));
        }

        private void BuildOpponentCardSource(Transform parent)
        {
            var source = UiFactory.Panel("RealisticOpponentCardSource", parent, ColorFromHex(0x18130F));
            UiFactory.SetHeight(source, 154f);
            UiFactory.Vertical(source, 8, 6);
            BuildPanelHeader(source.transform, "对手牌源", "1-2 本");

            var choices = MinionCatalogLoader.LoadFromResources().All
                .Where(card => card.InPool && !card.CardId.StartsWith("BGDUO") && card.TavernTier <= 2)
                .Take(6)
                .ToList();
            for (var rowIndex = 0; rowIndex < 2; rowIndex += 1)
            {
                var row = UiFactory.Panel("RealisticOpponentCardSourceRow-" + rowIndex, source.transform, Color.clear);
                UiFactory.SetHeight(row, 36f);
                UiFactory.Horizontal(row, 0, 6);
                foreach (var definition in choices.Skip(rowIndex * 3).Take(3))
                {
                    var captured = definition;
                    var name = rowIndex == 0 && choices.IndexOf(definition) == 0 ? "RealisticAddOpponentButton" : "RealisticAddOpponent-" + captured.CardId;
                    SmallButton(name, row.transform, ShortName(captured.Name), service.State.Opponent.Board.Count < BoardLimit, () => Apply(new GameCommand(GameCommandType.AddOpponentMinion, captured.CardId)), 104f);
                }
            }
        }

        private void BuildOpponentToolbar(Transform parent, MinionInstance selected)
        {
            var toolbar = UiFactory.Panel("RealisticOpponentToolbar", parent, Color.clear);
            UiFactory.SetHeight(toolbar, 36f);
            UiFactory.Horizontal(toolbar, 0, 6);
            var canEdit = selected != null;
            SmallButton("RealisticMoveOpponentLeftButton", toolbar.transform, "左移", canEdit, () => MoveOpponentSelected(-1));
            SmallButton("RealisticMoveOpponentRightButton", toolbar.transform, "右移", canEdit, () => MoveOpponentSelected(1));
            SmallButton("RealisticRemoveOpponentButton", toolbar.transform, "删除", canEdit, () =>
            {
                if (selected == null)
                {
                    return;
                }

                Apply(new GameCommand(GameCommandType.RemoveOpponentMinion, selected.InstanceId));
                selectedCardId = null;
            });
        }

        private void BuildOpponentStatEditor(Transform parent, MinionInstance selected)
        {
            var editor = UiFactory.Panel("RealisticOpponentStatEditor", parent, ColorFromHex(0x18130F));
            UiFactory.SetHeight(editor, selected == null ? 106f : 270f);
            UiFactory.Vertical(editor, 8, 6);
            if (selected == null)
            {
                EmptyText(editor.transform, "选择对手随从后编辑身材和关键词。");
                return;
            }

            Label(editor.transform, selected.Name, 13, FontStyle.Bold, ColorFromHex(0xF4E4BC), TextAnchor.MiddleLeft);
            Stepper(editor.transform, "对手攻击", selected.Attack, value => UpdateOpponentSelected(new MinionPatch { Attack = value }));
            Stepper(editor.transform, "对手生命", selected.Health, value => UpdateOpponentSelected(new MinionPatch { Health = value }));
            Stepper(editor.transform, "对手最大生命", selected.MaxHealth, value => UpdateOpponentSelected(new MinionPatch { MaxHealth = value }));

            var toggles = UiFactory.Panel("RealisticOpponentKeywordToggles", editor.transform, Color.clear);
            UiFactory.SetHeight(toggles, 34f);
            UiFactory.Horizontal(toggles, 0, 6);
            KeywordToggle(toggles.transform, selected, Keyword.Taunt, "嘲讽", UpdateOpponentSelected);
            KeywordToggle(toggles.transform, selected, Keyword.DivineShield, "圣盾", UpdateOpponentSelected);
            KeywordToggle(toggles.transform, selected, Keyword.Reborn, "复生", UpdateOpponentSelected);
        }

        private void BuildBattleDrawer(Transform parent)
        {
            var panel = UiFactory.Panel("RealisticBattleTestPanel", parent, ColorFromHex(0x211A14));
            UiFactory.SetHeight(panel, service.State.LastReplay == null || service.State.LastReplay.Frames.Count == 0 ? 560f : 880f);
            UiFactory.Vertical(panel, 10, 8);
            BuildPanelHeader(panel.transform, "战斗测试", "种子 " + DefaultCombatSeed());

            var scenario = UiFactory.Panel("RealisticScenarioControls", panel.transform, ColorFromHex(0x18130F));
            UiFactory.SetHeight(scenario, 110f);
            UiFactory.Vertical(scenario, 8, 6);
            Label(scenario.transform, "场景：" + DefaultScenarioName(), 12, FontStyle.Bold, ColorFromHex(0xD7C7A3), TextAnchor.MiddleLeft).gameObject.name = "RealisticScenarioNameInput";
            var scenarioButtons = UiFactory.Panel("RealisticScenarioButtons", scenario.transform, Color.clear);
            UiFactory.SetHeight(scenarioButtons, 36f);
            UiFactory.Horizontal(scenarioButtons, 0, 6);
            SmallButton("RealisticSaveScenarioButton", scenarioButtons.transform, "保存", true, () => Apply(new GameCommand(GameCommandType.SaveTestScenario, DefaultScenarioName(), new CombatTestOptions())));
            SmallButton("RealisticLoadScenarioButton", scenarioButtons.transform, "加载", true, () => Apply(new GameCommand(GameCommandType.LoadTestScenario, DefaultScenarioName(), new CombatTestOptions())));

            var combat = UiFactory.Panel("RealisticCombatControls", panel.transform, ColorFromHex(0x18130F));
            UiFactory.SetHeight(combat, 112f);
            UiFactory.Vertical(combat, 8, 6);
            Label(combat.transform, "固定种子：" + DefaultCombatSeed(), 12, FontStyle.Bold, ColorFromHex(0xD7C7A3), TextAnchor.MiddleLeft).gameObject.name = "RealisticCombatSeedInput";
            var combatButtons = UiFactory.Panel("RealisticCombatButtons", combat.transform, Color.clear);
            UiFactory.SetHeight(combatButtons, 36f);
            UiFactory.Horizontal(combatButtons, 0, 6);
            SmallButton("RealisticRunCombatTestButton", combatButtons.transform, "开始战斗", true, () => Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = DefaultCombatSeed(), SafetyLimit = 200 })));
            SmallButton("RealisticResetCombatSnapshotButton", combatButtons.transform, "重置战前", true, () => Apply(new GameCommand(GameCommandType.ResetCombatTestSnapshot)));

            var list = UiFactory.Panel("RealisticScenarioList", panel.transform, ColorFromHex(0x18130F));
            UiFactory.SetHeight(list, 104f);
            UiFactory.Vertical(list, 8, 4);
            BuildPanelHeader(list.transform, "最近场景", service.TestScenarioNames.Count + " 个");
            foreach (var name in service.TestScenarioNames.Take(3))
            {
                LogLine(list.transform, name);
            }

            BuildReplayDebugger(panel.transform);
        }

        private void BuildReplayDebugger(Transform parent)
        {
            var replay = service.State.LastReplay;
            var panel = UiFactory.Panel("RealisticCombatReplayDebugger", parent, ColorFromHex(0x18130F));
            UiFactory.SetHeight(panel, replay == null || replay.Frames.Count == 0 ? 126f : 430f);
            UiFactory.Vertical(panel, 8, 6);
            if (replay == null || replay.Frames.Count == 0)
            {
                BuildPanelHeader(panel.transform, "战斗回放", "无帧");
                EmptyText(panel.transform, "开始战斗后可以逐帧检查战斗过程。");
                return;
            }

            activeReplayFrameIndex = Mathf.Clamp(activeReplayFrameIndex, 0, replay.Frames.Count - 1);
            var frame = replay.Frames[activeReplayFrameIndex];
            BuildPanelHeader(panel.transform, "战斗回放", (activeReplayFrameIndex + 1) + "/" + replay.Frames.Count + "  " + replay.Result);

            var controls = UiFactory.Panel("RealisticReplayFrameControls", panel.transform, Color.clear);
            UiFactory.SetHeight(controls, 34f);
            UiFactory.Horizontal(controls, 0, 6);
            SmallButton("RealisticReplayFirstButton", controls.transform, "|<", activeReplayFrameIndex > 0, () => SetReplayFrameIndex(0), 54f);
            SmallButton("RealisticReplayPrevButton", controls.transform, "<", activeReplayFrameIndex > 0, () => SetReplayFrameIndex(activeReplayFrameIndex - 1), 54f);
            SmallButton("RealisticReplayNextButton", controls.transform, ">", activeReplayFrameIndex + 1 < replay.Frames.Count, () => SetReplayFrameIndex(activeReplayFrameIndex + 1), 54f);
            SmallButton("RealisticReplayLastButton", controls.transform, ">|", activeReplayFrameIndex + 1 < replay.Frames.Count, () => SetReplayFrameIndex(replay.Frames.Count - 1), 54f);

            Label(panel.transform, frame.EventType + "  " + frame.LogText, 11, FontStyle.Bold, ColorFromHex(0xF4E4BC), TextAnchor.MiddleLeft);
            BuildReplaySnapshot(panel.transform, "玩家", frame.PlayerBoardSnapshot);
            BuildReplaySnapshot(panel.transform, "对手", frame.OpponentBoardSnapshot);
        }

        private void BuildReplaySnapshot(Transform parent, string title, CombatBoardSnapshot snapshot)
        {
            var row = UiFactory.Panel("RealisticReplay" + title + "Board", parent, ColorFromHex(0x120E0B));
            UiFactory.SetHeight(row, 58f);
            UiFactory.Horizontal(row, 6, 4);
            Label(row.transform, title, 11, FontStyle.Bold, ColorFromHex(0xD7C7A3), TextAnchor.MiddleCenter);
            foreach (var minion in snapshot.Minions.Take(BoardLimit))
            {
                var slot = Slot("RealisticReplayMinion", row.transform, ColorFromHex(0x332821), 36f, 42f);
                Label(slot.transform, minion.Attack + "/" + minion.Health, 9, FontStyle.Bold, ColorFromHex(0xF4E4BC), TextAnchor.MiddleCenter);
            }
        }

        private void BuildLogsDrawer(Transform parent)
        {
            var panel = UiFactory.Panel("RealisticLogsPanel", parent, ColorFromHex(0x211A14));
            UiFactory.SetHeight(panel, 520f);
            UiFactory.Vertical(panel, 10, 6);
            BuildPanelHeader(panel.transform, "日志", lastError == null ? "招募 / 战斗" : lastError);
            foreach (var entry in service.State.Player.Tavern.RecruitLog.Skip(Math.Max(0, service.State.Player.Tavern.RecruitLog.Count - 10)))
            {
                LogLine(panel.transform, entry.Seq + ". " + entry.Message + " (" + entry.GoldBefore + "->" + entry.GoldAfter + ")");
            }

            foreach (var entry in service.State.CombatLog.Take(10))
            {
                LogLine(panel.transform, entry.Seq + ". " + entry.Detail);
            }
        }

        private void BuildDebugDrawer(Transform parent)
        {
            var panel = UiFactory.Panel("RealisticDebugPanel", parent, ColorFromHex(0x211A14));
            UiFactory.SetHeight(panel, 540f);
            UiFactory.Vertical(panel, 10, 8);
            BuildPanelHeader(panel.transform, "调试", service.State.Player.Tavern.Hand.Count + "/" + HandLimit);

            var quick = UiFactory.Panel("RealisticDebugQuickActions", panel.transform, Color.clear);
            UiFactory.SetHeight(quick, 36f);
            UiFactory.Horizontal(quick, 0, 6);
            SmallButton("RealisticAddCardButton", quick.transform, "加当前本随从", service.State.Player.Tavern.Hand.Count < HandLimit, AddDebugCard);
            SmallButton("RealisticDebugGoldButton", quick.transform, "+10 金币", true, () => Apply(new GameCommand(GameCommandType.DebugAddGold, 10)));

            var choices = UiFactory.Panel("RealisticCardAcquisitionPanel", panel.transform, ColorFromHex(0x18130F));
            UiFactory.SetHeight(choices, 398f);
            UiFactory.Vertical(choices, 8, 6);
            BuildPanelHeader(choices.transform, "获取卡牌", "随从 / 法术");
            var index = 0;
            foreach (var card in BuildDebugCardChoices().Take(10))
            {
                var row = UiFactory.Panel("RealisticCardAcquisitionRow", choices.transform, ColorFromHex(0x120E0B));
                UiFactory.SetHeight(row, 32f);
                UiFactory.Horizontal(row, 6, 6);
                Label(row.transform, ShortName(card.Name) + "  " + CardKindText(card), 10, FontStyle.Bold, ColorFromHex(0xD7C7A3), TextAnchor.MiddleLeft);
                var buttonName = index == 0 ? "RealisticAddCardToHandButton" : "RealisticAddCardToHandButton-" + card.CardId;
                SmallButton(buttonName, row.transform, "加入", service.State.Player.Tavern.Hand.Count < HandLimit, () => Apply(new GameCommand(GameCommandType.AddCardToHand, card.CardId, card.CardKind)), 64f);
                index += 1;
            }
        }

        private void BuildPanelHeader(Transform parent, string title, string meta)
        {
            var header = UiFactory.Panel("Realistic" + title + "Header", parent, Color.clear);
            UiFactory.SetHeight(header, 26f);
            UiFactory.Horizontal(header, 0, 8);
            var titleLabel = Label(header.transform, title, 13, FontStyle.Bold, ColorFromHex(0xF2D598), TextAnchor.MiddleLeft);
            UiFactory.SetFlexible(titleLabel.gameObject, 1f, 1f);
            var metaLabel = Label(header.transform, meta, 10, FontStyle.Bold, ColorFromHex(0xBDAA84), TextAnchor.MiddleRight);
            UiFactory.SetWidth(metaLabel.gameObject, 118f);
        }

        private void BuildRealisticBoardTribeDistribution(Transform parent)
        {
            var row = UiFactory.Panel("RealisticPlayerBoardTribeDistribution", parent, ColorFromHex(0x18130F));
            UiFactory.SetHeight(row, 34f);
            UiFactory.Horizontal(row, 8, 4);
            var distribution = BoardTribeAnalyzer.Build(service.State.Player.Board);
            var text = "种族分布：空战场";
            if (distribution.Count > 0)
            {
                var parts = distribution
                    .OrderByDescending(pair => pair.Value)
                    .ThenBy(pair => TribeDisplaySortIndex(pair.Key))
                    .Select(pair => TribeName(pair.Key) + " " + pair.Value);
                text = "种族分布：" + string.Join(" / ", parts.ToArray());
            }

            var label = Label(row.transform, text, 11, FontStyle.Bold, ColorFromHex(0xD7C7A3), TextAnchor.MiddleLeft);
            label.gameObject.name = "RealisticPlayerBoardTribeDistributionText";
        }

        private GameObject MiniBoardToken(string name, Transform parent, MinionInstance card, bool selectOnClick)
        {
            var token = UiFactory.Panel(name, parent, card.InstanceId == selectedCardId ? ColorFromHex(0x5A3C22) : ColorFromHex(0x463325));
            UiFactory.Stretch(token.GetComponent<RectTransform>());
            token.GetComponent<Image>().raycastTarget = true;
            var button = token.AddComponent<Button>();
            if (selectOnClick)
            {
                button.onClick.AddListener(() => SelectCard(card));
            }

            UiFactory.Vertical(token, 2, 1);
            Label(token.transform, card.TavernTier + " 本", 8, FontStyle.Bold, ColorFromHex(0xD8C08A), TextAnchor.MiddleCenter);
            Label(token.transform, card.Attack + "/" + card.Health, 10, FontStyle.Bold, ColorFromHex(0xF4E4BC), TextAnchor.MiddleCenter);
            return token;
        }

        private void Stepper(Transform parent, string label, int value, Action<int> onChange)
        {
            var row = UiFactory.Panel("Realistic" + label + "Stepper", parent, Color.clear);
            UiFactory.SetHeight(row, 32f);
            UiFactory.Horizontal(row, 0, 6);
            var text = Label(row.transform, label + "  " + value, 11, FontStyle.Bold, ColorFromHex(0xD7C7A3), TextAnchor.MiddleLeft);
            UiFactory.SetFlexible(text.gameObject, 1f, 1f);
            SmallButton("Realistic" + label + "MinusButton", row.transform, "-", true, () => onChange(value - 1), 42f);
            SmallButton("Realistic" + label + "PlusButton", row.transform, "+", true, () => onChange(value + 1), 42f);
        }

        private void KeywordToggle(Transform parent, MinionInstance card, Keyword keyword, string label)
        {
            KeywordToggle(parent, card, keyword, label, UpdateSelected);
        }

        private void KeywordToggle(Transform parent, MinionInstance card, Keyword keyword, string label, Action<MinionPatch> update)
        {
            var keywords = card.Keywords ?? new List<Keyword>();
            var hasKeyword = keywords.Contains(keyword);
            var button = SmallButton("RealisticKeyword-" + keyword, parent, label, true, () =>
            {
                var next = new List<Keyword>(keywords);
                if (hasKeyword)
                {
                    next.Remove(keyword);
                }
                else
                {
                    next.Add(keyword);
                }

                update(new MinionPatch { Keywords = next });
            }, 72f);
            UiFactory.SetImageColor(button.gameObject, hasKeyword ? ColorFromHex(0x5A3C22) : ColorFromHex(0x2B2118));
        }

        private void MoveOpponentSelected(int delta)
        {
            if (string.IsNullOrEmpty(selectedCardId))
            {
                return;
            }

            var index = service.State.Opponent.Board.FindIndex(card => card.InstanceId == selectedCardId);
            if (index < 0)
            {
                return;
            }

            Apply(new GameCommand(GameCommandType.MoveOpponentMinion, selectedCardId, index + delta));
        }

        private void UpdateOpponentSelected(MinionPatch patch)
        {
            if (string.IsNullOrEmpty(selectedCardId))
            {
                return;
            }

            Apply(new GameCommand(GameCommandType.UpdateOpponentMinion, selectedCardId, patch));
        }

        private void UpdateSelected(MinionPatch patch)
        {
            if (string.IsNullOrEmpty(selectedCardId))
            {
                return;
            }

            Apply(new GameCommand(GameCommandType.UpdateMinion, selectedCardId, patch));
        }

        private void SetReplayFrameIndex(int index)
        {
            var replay = service.State.LastReplay;
            activeReplayFrameIndex = replay == null || replay.Frames.Count == 0
                ? 0
                : Mathf.Clamp(index, 0, replay.Frames.Count - 1);
            Build();
        }

        private int DefaultCombatSeed()
        {
            return service.State.Seed + service.State.Round;
        }

        private string DefaultScenarioName()
        {
            return "round-" + service.State.Round + "-battle-test";
        }

        private IEnumerable<MinionInstance> BuildDebugCardChoices()
        {
            foreach (var definition in MinionCatalogLoader.LoadFromResources().All.Where(card => card.InPool && !card.CardId.StartsWith("BGDUO")).Take(6))
            {
                yield return MinionFactory.Create(definition, BoardSide.Player, "ui-choice", false, PoolSource.Debug, 0);
            }

            foreach (var definition in SpellCatalogLoader.LoadFromResources().All.Where(spell => spell.Category == "TavernSpell").Take(4))
            {
                var spell = MinionFactory.Create(definition, BoardSide.Player, "ui-choice");
                spell.PoolSource = PoolSource.Debug;
                spell.OriginPoolSource = PoolSource.Debug;
                yield return spell;
            }
        }

        private void EmptyText(Transform parent, string text)
        {
            var label = Label(parent, text, 11, FontStyle.Normal, ColorFromHex(0xBDAA84), TextAnchor.MiddleCenter);
            UiFactory.SetFlexible(label.gameObject, 1f, 1f);
        }

        private void LogLine(Transform parent, string text)
        {
            var label = Label(parent, text, 10, FontStyle.Normal, ColorFromHex(0xBDAA84), TextAnchor.MiddleLeft);
            UiFactory.SetHeight(label.gameObject, 22f);
        }

        private static string ShortName(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 7)
            {
                return value;
            }

            return value.Substring(0, 7);
        }

        private static string CardKindText(MinionInstance card)
        {
            return card.CardKind == CardKind.TavernSpell ? "酒馆法术" : "随从";
        }

        private static int TribeDisplaySortIndex(Tribe tribe)
        {
            switch (tribe)
            {
                case Tribe.Beast: return 0;
                case Tribe.Murloc: return 1;
                case Tribe.Mech: return 2;
                case Tribe.Demon: return 3;
                case Tribe.Dragon: return 4;
                case Tribe.Pirate: return 5;
                case Tribe.Elemental: return 6;
                case Tribe.Quilboar: return 7;
                case Tribe.Undead: return 8;
                case Tribe.Naga: return 9;
                default: return int.MaxValue;
            }
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

        private void BuildOpponentPreview(Transform parent)
        {
            var panel = UiFactory.Panel("RealisticOpponentPreview", parent, ColorFromHex(0x241C17));
            UiFactory.SetHeight(panel, 96f);
            UiFactory.Vertical(panel, 8, 5);
            Label(panel.transform, "对手：" + service.State.Opponent.Name + "  " + service.State.Opponent.Board.Count + "/7", 12, FontStyle.Bold, ColorFromHex(0xD7C7A3), TextAnchor.MiddleLeft);
            var row = UiFactory.Panel("RealisticOpponentMiniRow", panel.transform, Color.clear);
            UiFactory.SetFlexible(row, 1f, 1f);
            UiFactory.Horizontal(row, 0, 4);
            for (var index = 0; index < BoardLimit; index += 1)
            {
                var slot = Slot("RealisticOpponentSlot-" + index, row.transform, ColorFromHex(0x332821), 30f, 40f);
                AddDropTarget(slot, RealisticDropTarget.OpponentBoard, index);
                if (index < service.State.Opponent.Board.Count)
                {
                    Label(slot.transform, service.State.Opponent.Board[index].Attack + "/" + service.State.Opponent.Board[index].Health, 9, FontStyle.Bold, ColorFromHex(0xF4E4BC), TextAnchor.MiddleCenter);
                }
            }
        }

        private void BuildLogPreview(Transform parent)
        {
            var panel = UiFactory.Panel("RealisticLogPreview", parent, ColorFromHex(0x241C17));
            UiFactory.SetFlexible(panel, 1f, 1f);
            UiFactory.Vertical(panel, 8, 4);
            Label(panel.transform, "日志 / 提示", 12, FontStyle.Bold, ColorFromHex(0xD7C7A3), TextAnchor.MiddleLeft);

            var lines = service.State.Player.Tavern.RecruitLog
                .Select(entry => entry.Message)
                .Concat(service.State.CombatLog.Select(entry => entry.Detail))
                .Concat(advisor.GetAdvice(service.State))
                .Where(line => !string.IsNullOrEmpty(line))
                .Reverse()
                .Take(5)
                .Reverse()
                .ToList();

            if (lines.Count == 0)
            {
                lines.Add("准备招募。");
            }

            foreach (var line in lines)
            {
                Label(panel.transform, "- " + line, 10, FontStyle.Normal, ColorFromHex(0xBDAA84), TextAnchor.MiddleLeft);
            }
        }

        private void BuildDiscoverOverlay(Transform parent)
        {
            var overlay = Panel("RealisticDiscoverOverlay", parent, new Color(0f, 0f, 0f, 0.62f));
            UiFactory.Stretch(overlay.GetComponent<RectTransform>());

            var modal = Panel("RealisticDiscoverModal", overlay.transform, ColorFromHex(0x2B2118), new Vector2(0.18f, 0.24f), new Vector2(0.82f, 0.78f), Vector2.zero, Vector2.zero);
            UiFactory.Vertical(modal, 18, 12);
            Label(modal.transform, "发现奖励", 24, FontStyle.Bold, ColorFromHex(0xF2D598), TextAnchor.MiddleCenter);

            var row = UiFactory.Panel("RealisticDiscoverOptions", modal.transform, Color.clear);
            UiFactory.SetFlexible(row, 1f, 1f);
            UiFactory.Horizontal(row, 16, 16);
            AddDropTarget(row, RealisticDropTarget.Hand, -1);
            var options = service.State.Player.Tavern.Discover.Options;
            for (var index = 0; index < options.Count; index += 1)
            {
                var localIndex = index;
                var card = TavernCardView.Create(row.transform, options[index], TavernCardVisualMode.Shop, _ => Apply(new GameCommand(GameCommandType.ChooseDiscover, localIndex)));
                UiFactory.SetWidth(card, 150f);
                AddDrag(card, options[index], RealisticDragSource.Discover, localIndex);
            }
        }

        private void BuildToast(Transform parent, string message)
        {
            var toast = Panel("RealisticErrorToast", parent, new Color(0.50f, 0.12f, 0.10f, 0.94f), new Vector2(0.28f, 0.90f), new Vector2(0.72f, 0.965f), Vector2.zero, Vector2.zero);
            Label(toast.transform, message, 14, FontStyle.Bold, ColorFromHex(0xFFE4D2), TextAnchor.MiddleCenter);
        }

        private void SelectCard(MinionInstance card)
        {
            selectedCardId = card?.InstanceId;
            Build();
        }

        private void AddDebugCard()
        {
            var tier = service.State.Player.Tavern.Tier;
            var definition = MinionCatalogLoader.LoadFromResources().All
                .FirstOrDefault(minion => minion.InPool && !minion.CardId.StartsWith("BGDUO") && minion.TavernTier == tier);
            if (definition != null)
            {
                Apply(new GameCommand(GameCommandType.AddCardToHand, definition.CardId, CardKind.Minion));
            }
        }

        private void Apply(GameCommand command)
        {
            try
            {
                service.Apply(command);
                lastError = null;
            }
            catch (Exception exception)
            {
                lastError = exception.Message;
                Debug.LogWarning(exception.Message);
            }

            Build();
        }

        private MinionInstance FindSelectedCard()
        {
            if (string.IsNullOrEmpty(selectedCardId))
            {
                return null;
            }

            return AllVisibleCards().FirstOrDefault(card => card.InstanceId == selectedCardId);
        }

        private IEnumerable<MinionInstance> AllVisibleCards()
        {
            foreach (var card in service.State.Player.Tavern.Shop.Where(card => card != null))
            {
                yield return card;
            }

            foreach (var card in service.State.Player.Tavern.Hand)
            {
                yield return card;
            }

            foreach (var card in service.State.Player.Board)
            {
                yield return card;
            }

            foreach (var card in service.State.Opponent.Board)
            {
                yield return card;
            }

            if (service.State.Player.Tavern.Discover == null)
            {
                yield break;
            }

            foreach (var card in service.State.Player.Tavern.Discover.Options)
            {
                yield return card;
            }
        }

        private static bool TryBuildDropCommand(DragContext drag, RealisticDropTarget target, int targetIndex, out GameCommand command)
        {
            command = null;
            if (drag.Source == RealisticDragSource.Shop && target == RealisticDropTarget.Hand)
            {
                command = new GameCommand(GameCommandType.BuyMinion, drag.Index);
                return true;
            }

            if (drag.Source == RealisticDragSource.Discover && target == RealisticDropTarget.Hand)
            {
                command = new GameCommand(GameCommandType.ChooseDiscover, drag.Index);
                return true;
            }

            if (drag.Source == RealisticDragSource.Hand && target == RealisticDropTarget.PlayerBoard)
            {
                command = new GameCommand(GameCommandType.PlayMinion, drag.Index, targetIndex);
                return true;
            }

            if (drag.Source == RealisticDragSource.PlayerBoard && target == RealisticDropTarget.PlayerBoard)
            {
                command = new GameCommand(GameCommandType.MoveBoardMinion, drag.Card.InstanceId, targetIndex);
                return true;
            }

            if (drag.Source == RealisticDragSource.PlayerBoard && target == RealisticDropTarget.Hand)
            {
                command = new GameCommand(GameCommandType.MoveMinion, drag.Card.InstanceId);
                return true;
            }

            if (drag.Source == RealisticDragSource.PlayerBoard && target == RealisticDropTarget.SellZone)
            {
                command = new GameCommand(GameCommandType.SellMinion, drag.Card.InstanceId);
                return true;
            }

            if (drag.Source == RealisticDragSource.OpponentBoard && target == RealisticDropTarget.OpponentBoard)
            {
                command = new GameCommand(GameCommandType.MoveOpponentMinion, drag.Card.InstanceId, targetIndex);
                return true;
            }

            return false;
        }

        private void CreateDragGhost(MinionInstance card, PointerEventData eventData)
        {
            DestroyDragGhost();
            dragGhost = TavernCardView.Create(root, card, TavernCardVisualMode.Hand, null);
            dragGhost.name = "RealisticDragGhost-" + card.InstanceId;
            dragGhost.transform.SetAsLastSibling();
            var canvas = dragGhost.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5000;
            var group = dragGhost.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0.95f;
            MoveDragGhost(eventData);
        }

        private void MoveDragGhost(PointerEventData eventData)
        {
            var rootRect = root as RectTransform;
            var ghostRect = dragGhost.GetComponent<RectTransform>();
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
                UnityEngine.Object.Destroy(dragGhost);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(dragGhost);
            }

            dragGhost = null;
        }

        private void DestroyShell()
        {
            if (shell == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(shell);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(shell);
            }
        }

        private void AddDrag(GameObject target, MinionInstance card, RealisticDragSource source, int index)
        {
            var drag = target.GetComponent<RealisticCardDragBehaviour>() ?? target.AddComponent<RealisticCardDragBehaviour>();
            drag.Initialize(this, card, source, index);
        }

        private void AddDropTarget(GameObject target, RealisticDropTarget dropTarget, int targetIndex)
        {
            var behaviour = target.GetComponent<RealisticDropTargetBehaviour>() ?? target.AddComponent<RealisticDropTargetBehaviour>();
            behaviour.Initialize(this, dropTarget, targetIndex);
        }

        private static GameObject Panel(string name, Transform parent, Color color)
        {
            return UiFactory.Panel(name, parent, color);
        }

        private static GameObject Panel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var panel = UiFactory.Panel(name, parent, color);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return panel;
        }

        private static GameObject StackPanel(string name, Transform parent, float width)
        {
            var panel = UiFactory.Panel(name, parent, Color.clear);
            UiFactory.SetWidth(panel, width);
            UiFactory.Vertical(panel, 0, 1);
            return panel;
        }

        private static GameObject Slot(string name, Transform parent, Color color, float width, float height)
        {
            var slot = UiFactory.Panel(name, parent, color);
            slot.GetComponent<Image>().raycastTarget = true;
            UiFactory.SetMinSize(slot, width, height);
            UiFactory.SetWidth(slot, width);
            UiFactory.SetHeight(slot, height);
            return slot;
        }

        private static Text Label(Transform parent, string text, int size, FontStyle style, Color color, TextAnchor anchor)
        {
            var label = UiFactory.Label("RealisticLabel", parent, text, size, style);
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            UiFactory.SetTextColor(label, color);
            if (parent.GetComponent<LayoutGroup>() == null)
            {
                UiFactory.Stretch(label.rectTransform);
            }
            else
            {
                UiFactory.SetHeight(label.gameObject, Mathf.Max(18f, size + 8f));
            }

            return label;
        }

        private static void ResourcePill(Transform parent, string label, string value, Color color)
        {
            var pill = UiFactory.Panel("ResourcePill-" + label, parent, color);
            UiFactory.SetWidth(pill, 98f);
            UiFactory.Vertical(pill, 4, 0);
            Label(pill.transform, label, 10, FontStyle.Bold, ColorFromHex(0xE6D3A8), TextAnchor.MiddleCenter);
            Label(pill.transform, value, 17, FontStyle.Bold, ColorFromHex(0xFFF2C5), TextAnchor.MiddleCenter);
        }

        private static Button SmallButton(string name, Transform parent, string text, bool enabled, Action onClick, float width = 112f)
        {
            var button = UiFactory.Button(name, parent, text, () => onClick?.Invoke());
            button.interactable = enabled;
            UiFactory.SetWidth(button.gameObject, width);
            UiFactory.SetHeight(button.gameObject, 34f);
            UiFactory.SetImageColor(button.gameObject, enabled ? ColorFromHex(0x4C3320) : ColorFromHex(0x24201C));
            return button;
        }

        internal static Color ColorFromHex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f);
        }

        private sealed class DragContext
        {
            public MinionInstance Card;
            public RealisticDragSource Source;
            public int Index;
        }
    }
}
