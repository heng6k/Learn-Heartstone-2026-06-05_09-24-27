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
using UnityEngine.Events;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer
{
    public sealed class TavernTrainerView
    {
        private const int BoardLimit = 7;
        private readonly Transform root;
        private readonly MatchService service;
        private readonly IAdvisorService advisor;
        private readonly System.Action backToHub;
        private GameObject shell;
        private string selectedMinionId;
        private string lastError;
        private DragContext activeDrag;
        private GameObject dragGhost;
        private RightInspectorTab activeRightTab = RightInspectorTab.Info;

        public TavernTrainerView(Transform root, MatchService service, IAdvisorService advisor, System.Action backToHub)
        {
            this.root = root;
            this.service = service;
            this.advisor = advisor;
            this.backToHub = backToHub;
        }

        public void Build()
        {
            if (shell != null)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Object.Destroy(shell);
                }
                else
                {
                    Object.DestroyImmediate(shell);
                }
            }

            shell = UiFactory.Panel("TavernTrainer", root, ColorFromHex(0x101418));
            UiFactory.Stretch(shell.GetComponent<RectTransform>());
            UiFactory.Vertical(shell, 0, 0);

            BuildTopToolbar(shell.transform);
            BuildWorkspace(shell.transform);
            BuildBottomDock(shell.transform);
        }

        private void BuildTopToolbar(Transform parent)
        {
            var bar = UiFactory.Panel("TopToolbar", parent, ColorFromHex(0x111820));
            UiFactory.SetHeight(bar, 60);
            UiFactory.Horizontal(bar, 12, 10);

            var brand = UiFactory.Panel("BrandBlock", bar.transform, Color.clear);
            UiFactory.SetWidth(brand, 220);
            UiFactory.Vertical(brand, 0, 1);
            var title = UiFactory.Label("BrandTitle", brand.transform, "酒馆战棋训练器", 15, FontStyle.Bold);
            UiFactory.SetHeight(title.gameObject, 26);
            var subtitle = UiFactory.Label("BrandSubtitle", brand.transform, "本地单人教学 / Unity 版", 13);
            UiFactory.SetTextColor(subtitle, ColorFromHex(0x9AA7B4));
            UiFactory.SetHeight(subtitle.gameObject, 20);

            var modes = UiFactory.Panel("ModeTabs", bar.transform, ColorFromHex(0x181E24));
            UiFactory.SetWidth(modes, 276);
            UiFactory.Horizontal(modes, 3, 4);
            ToolbarButton(modes.transform, "酒馆练习", true, true, () => { });
            ToolbarButton(modes.transform, "战斗沙盒", false, false, () => { });
            ToolbarButton(modes.transform, "场景", false, false, () => { });

            var info = UiFactory.Label(
                "RoundInfo",
                bar.transform,
                "回合 " + service.State.Round + "  |  " + service.State.Player.Tavern.Tier + " 本  |  金币 " + service.State.Player.Tavern.Gold + "/" + service.State.Player.Tavern.MaxGold,
                15,
                FontStyle.Bold);
            UiFactory.SetFlexible(info.gameObject, 1, 1);

            ToolbarButton(bar.transform, "返回", false, true, () => backToHub());
            ToolbarButton(bar.transform, "刷新 1", false, service.State.Player.Tavern.Gold >= 1, () => Apply(new GameCommand(GameCommandType.RerollShop)));
            ToolbarButton(bar.transform, service.State.Player.Tavern.Frozen ? "解冻" : "冻结", service.State.Player.Tavern.Frozen, true, () => Apply(new GameCommand(GameCommandType.FreezeShop, !service.State.Player.Tavern.Frozen)));
            ToolbarButton(bar.transform, "升本 " + service.State.Player.Tavern.UpgradeCost, false, service.State.Player.Tavern.UpgradeCost > 0 && service.State.Player.Tavern.Gold >= service.State.Player.Tavern.UpgradeCost, () => Apply(new GameCommand(GameCommandType.UpgradeTavern)));
            ToolbarButton(bar.transform, "下回合", false, true, () => Apply(new GameCommand(GameCommandType.NextTurn)));
            ToolbarButton(bar.transform, "+10 金币", false, true, () => Apply(new GameCommand(GameCommandType.DebugAddGold, 10)));
        }

        private void BuildWorkspace(Transform parent)
        {
            var workspace = UiFactory.Panel("Workspace", parent, ColorFromHex(0x101418));
            UiFactory.SetFlexible(workspace, 1, 1);
            UiFactory.Horizontal(workspace, 0, 0);

            var mainStage = UiFactory.Panel("MainStage", workspace.transform, ColorFromHex(0x101418));
            UiFactory.SetFlexible(mainStage, 1, 1);
            UiFactory.Vertical(mainStage, 16, 14);

            BuildShopStage(mainStage.transform);
            BuildBoardPanel(mainStage.transform, "玩家战场", service.State.Player.Health, service.State.Player.Armor, service.State.Player.Board, BoardSide.Player, true);

            var inspector = UiFactory.Panel("RightInspector", workspace.transform, ColorFromHex(0x181E24));
            UiFactory.SetWidth(inspector, 380);
            UiFactory.Vertical(inspector, 12, 10);
            BuildRightInspector(inspector.transform);
        }

        private void BuildRightInspector(Transform parent)
        {
            BuildRightInspectorTabs(parent);
            if (activeRightTab == RightInspectorTab.CardAcquisition)
            {
                BuildCardAcquisitionPanel(parent);
                return;
            }

            if (activeRightTab == RightInspectorTab.OpponentCustomization)
            {
                BuildOpponentCustomizationPanel(parent);
                return;
            }

            if (activeRightTab == RightInspectorTab.BattleTest)
            {
                BuildBattleTestPanel(parent);
                return;
            }

            BuildBattleQuickControls(parent);
            BuildBoardPanel(parent, service.State.Opponent.Name, service.State.Opponent.Health, service.State.Opponent.Armor, service.State.Opponent.Board, BoardSide.Opponent, false);
            BuildMinionEditor(parent);
            BuildHints(parent);
            BuildLogs(parent);
        }

        private void BuildRightInspectorTabs(Transform parent)
        {
            var tabs = UiFactory.Panel("RightInspectorTabs", parent, ColorFromHex(0x141B22));
            UiFactory.SetHeight(tabs, 40);
            UiFactory.Horizontal(tabs, 4, 4);
            InspectorTabButton(tabs.transform, "Tab-Info", "对局", RightInspectorTab.Info);
            InspectorTabButton(tabs.transform, "Tab-CardAcquisition", "获取", RightInspectorTab.CardAcquisition);
            InspectorTabButton(tabs.transform, "Tab-OpponentCustomization", "对手", RightInspectorTab.OpponentCustomization);
            InspectorTabButton(tabs.transform, "Tab-BattleTest", "战斗", RightInspectorTab.BattleTest);
        }

        private void InspectorTabButton(Transform parent, string name, string text, RightInspectorTab tab)
        {
            var button = UiFactory.Button(name, parent, text, () =>
            {
                activeRightTab = tab;
                Rebuild();
            });
            UiFactory.SetFlexible(button.gameObject, 1, 1);
            UiFactory.SetImageColor(button.gameObject, activeRightTab == tab ? ColorFromHex(0x273F57) : ColorFromHex(0x202832));
        }

        private void BuildCardAcquisitionPanel(Transform parent)
        {
            var panel = UiFactory.Panel("CardAcquisitionPanel", parent, ColorFromHex(0x202832));
            UiFactory.SetFlexible(panel, 1, 1);
            UiFactory.Vertical(panel, 10, 7);
            BuildDockHeader(panel.transform, "获取卡牌", service.State.Player.Tavern.Hand.Count + "/10");
            if (lastError != null)
            {
                LogLine(panel.transform, lastError);
            }

            var index = 0;
            foreach (var card in BuildDebugCardChoices().Take(8))
            {
                var row = UiFactory.Panel("CardAcquisitionRow", panel.transform, ColorFromHex(0x181E24));
                UiFactory.SetHeight(row, 42);
                UiFactory.Horizontal(row, 6, 6);
                var label = UiFactory.Label("CardAcquisitionName", row.transform, card.Name + "  " + card.CardKind, 12, FontStyle.Bold);
                UiFactory.SetFlexible(label.gameObject, 1, 1);
                var buttonName = index == 0 ? "AddCardToHandButton" : "AddCardToHandButton-" + card.CardId;
                var button = UiFactory.Button(buttonName, row.transform, "加入手牌", () => Apply(new GameCommand(GameCommandType.AddCardToHand, card.CardId, card.CardKind)));
                UiFactory.SetWidth(button.gameObject, 94);
                index += 1;
            }
        }

        private IEnumerable<MinionInstance> BuildDebugCardChoices()
        {
            foreach (var definition in MinionCatalogLoader.LoadFromResources().All.Take(6))
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

        private void BuildBattleQuickControls(Transform parent)
        {
            var panel = UiFactory.Panel("BattleQuickControls", parent, ColorFromHex(0x202832));
            UiFactory.SetHeight(panel, 72);
            UiFactory.Vertical(panel, 8, 6);
            var header = UiFactory.Label("BattleQuickTitle", panel.transform, "战斗测试", 13, FontStyle.Bold);
            UiFactory.SetHeight(header.gameObject, 20);
            var start = UiFactory.Button("StartCombatButton", panel.transform, "开始战斗", () => Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = DefaultCombatSeed(), SafetyLimit = 200 })));
            UiFactory.SetHeight(start.gameObject, 34);
        }

        private void BuildBattleTestPanel(Transform parent)
        {
            var panel = UiFactory.Panel("BattleTestPanel", parent, ColorFromHex(0x202832));
            UiFactory.SetFlexible(panel, 1, 1);
            UiFactory.Vertical(panel, 10, 7);
            BuildDockHeader(panel.transform, "战斗测试", "种子 " + DefaultCombatSeed());

            var scenarioName = DefaultScenarioName();
            var scenario = UiFactory.Panel("ScenarioControls", panel.transform, ColorFromHex(0x181E24));
            UiFactory.SetHeight(scenario, 112);
            UiFactory.Vertical(scenario, 8, 6);
            UiFactory.Label("ScenarioNameInput", scenario.transform, "场景：" + scenarioName, 13, FontStyle.Bold);
            var scenarioButtons = UiFactory.Panel("ScenarioButtons", scenario.transform, Color.clear);
            UiFactory.SetHeight(scenarioButtons, 34);
            UiFactory.Horizontal(scenarioButtons, 0, 6);
            var save = UiFactory.Button("SaveScenarioButton", scenarioButtons.transform, "保存场景", () => Apply(new GameCommand(GameCommandType.SaveTestScenario, scenarioName, new CombatTestOptions())));
            var load = UiFactory.Button("LoadScenarioButton", scenarioButtons.transform, "加载场景", () => Apply(new GameCommand(GameCommandType.LoadTestScenario, scenarioName, new CombatTestOptions())));
            UiFactory.SetWidth(save.gameObject, 112);
            UiFactory.SetWidth(load.gameObject, 112);

            var combat = UiFactory.Panel("CombatTestControls", panel.transform, ColorFromHex(0x181E24));
            UiFactory.SetHeight(combat, 122);
            UiFactory.Vertical(combat, 8, 6);
            UiFactory.Label("CombatSeedInput", combat.transform, "固定种子：" + DefaultCombatSeed(), 13, FontStyle.Bold);
            var combatButtons = UiFactory.Panel("CombatButtons", combat.transform, Color.clear);
            UiFactory.SetHeight(combatButtons, 34);
            UiFactory.Horizontal(combatButtons, 0, 6);
            var run = UiFactory.Button("RunCombatTestButton", combatButtons.transform, "开始战斗", () => Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = DefaultCombatSeed(), SafetyLimit = 200 })));
            var reset = UiFactory.Button("ResetCombatSnapshotButton", combatButtons.transform, "重置战前", () => Apply(new GameCommand(GameCommandType.ResetCombatTestSnapshot)));
            UiFactory.SetWidth(run.gameObject, 112);
            UiFactory.SetWidth(reset.gameObject, 112);

            var list = UiFactory.Panel("ScenarioList", panel.transform, ColorFromHex(0x181E24));
            UiFactory.SetHeight(list, 112);
            UiFactory.Vertical(list, 8, 4);
            BuildDockHeader(list.transform, "最近场景", service.TestScenarioNames.Count + " 个");
            foreach (var name in service.TestScenarioNames.Take(3))
            {
                LogLine(list.transform, name);
            }

            var log = UiFactory.Panel("BattleTestLogPreview", panel.transform, ColorFromHex(0x181E24));
            UiFactory.SetFlexible(log, 1, 1);
            UiFactory.Vertical(log, 8, 4);
            var result = service.State.LastResult == null
                ? "尚未开始战斗"
                : "结果：" + service.State.LastResult.Winner + "，步数 " + service.State.LastResult.Steps;
            BuildDockHeader(log.transform, "战斗日志", result);
            foreach (var entry in service.State.CombatLog.Take(8))
            {
                LogLine(log.transform, entry.Seq + ". " + entry.Detail);
            }
        }

        private int DefaultCombatSeed()
        {
            return service.State.Seed + service.State.Round;
        }

        private string DefaultScenarioName()
        {
            return "round-" + service.State.Round + "-battle-test";
        }

        private void BuildOpponentCustomizationPanel(Transform parent)
        {
            var panel = UiFactory.Panel("OpponentCustomizationPanel", parent, ColorFromHex(0x202832));
            UiFactory.SetFlexible(panel, 1, 1);
            UiFactory.Vertical(panel, 10, 7);
            BuildDockHeader(panel.transform, "自定义对手", service.State.Opponent.Board.Count + "/7");
            if (lastError != null)
            {
                LogLine(panel.transform, lastError);
            }

            BuildCompactBoardGrid(panel.transform, "OpponentCustomizationSlots", service.State.Opponent.Board, BoardSide.Opponent);
            var firstChoice = MinionCatalogLoader.LoadFromResources().All.First();
            var addButton = UiFactory.Button("AddOpponentButton", panel.transform, "添加对手", () => Apply(new GameCommand(GameCommandType.AddOpponentMinion, firstChoice.CardId)));
            UiFactory.SetHeight(addButton.gameObject, 32);

            var selected = service.State.Opponent.Board.FirstOrDefault(minion => minion.InstanceId == selectedMinionId);
            BuildOpponentToolbar(panel.transform, selected);
            BuildOpponentStatEditor(panel.transform, selected);
        }

        private void BuildOpponentToolbar(Transform parent, MinionInstance selected)
        {
            var toolbar = UiFactory.Panel("OpponentCustomizationToolbar", parent, Color.clear);
            UiFactory.SetHeight(toolbar, 36);
            UiFactory.Horizontal(toolbar, 0, 6);
            var canEdit = selected != null;
            var left = UiFactory.Button("MoveOpponentLeftButton", toolbar.transform, "左移", () => MoveOpponentSelected(-1));
            left.interactable = canEdit;
            var right = UiFactory.Button("MoveOpponentRightButton", toolbar.transform, "右移", () => MoveOpponentSelected(1));
            right.interactable = canEdit;
            var remove = UiFactory.Button("RemoveOpponentButton", toolbar.transform, "删除", () =>
            {
                if (selected != null)
                {
                    Apply(new GameCommand(GameCommandType.RemoveOpponentMinion, selected.InstanceId));
                    selectedMinionId = null;
                }
            });
            remove.interactable = canEdit;
        }

        private void BuildOpponentStatEditor(Transform parent, MinionInstance selected)
        {
            var editor = UiFactory.Panel("OpponentStatEditor", parent, ColorFromHex(0x181E24));
            UiFactory.SetFlexible(editor, 1, 1);
            UiFactory.Vertical(editor, 8, 6);
            if (selected == null)
            {
                EmptyText(editor.transform, "选择对手随从后编辑身材和关键词。");
                return;
            }

            var name = UiFactory.Label("OpponentSelectedName", editor.transform, selected.Name, 14, FontStyle.Bold);
            UiFactory.SetHeight(name.gameObject, 24);
            Stepper(editor.transform, "对手攻击", selected.Attack, value => UpdateOpponentSelected(new MinionPatch { Attack = value }));
            Stepper(editor.transform, "对手生命", selected.Health, value => UpdateOpponentSelected(new MinionPatch { Health = value }));
            Stepper(editor.transform, "对手最大生命", selected.MaxHealth, value => UpdateOpponentSelected(new MinionPatch { MaxHealth = value }));

            var toggles = UiFactory.Panel("OpponentKeywordToggles", editor.transform, Color.clear);
            UiFactory.SetHeight(toggles, 72);
            UiFactory.Horizontal(toggles, 0, 6);
            OpponentKeywordToggle(toggles.transform, selected, Keyword.Taunt, "嘲讽");
            OpponentKeywordToggle(toggles.transform, selected, Keyword.DivineShield, "圣盾");
            OpponentKeywordToggle(toggles.transform, selected, Keyword.Poisonous, "剧毒");
            OpponentKeywordToggle(toggles.transform, selected, Keyword.Venomous, "烈毒");
        }

        private void MoveOpponentSelected(int delta)
        {
            if (string.IsNullOrEmpty(selectedMinionId))
            {
                return;
            }

            var index = service.State.Opponent.Board.FindIndex(minion => minion.InstanceId == selectedMinionId);
            if (index < 0)
            {
                return;
            }

            Apply(new GameCommand(GameCommandType.MoveOpponentMinion, selectedMinionId, index + delta));
        }

        private void UpdateOpponentSelected(MinionPatch patch)
        {
            if (string.IsNullOrEmpty(selectedMinionId))
            {
                return;
            }

            Apply(new GameCommand(GameCommandType.UpdateOpponentMinion, selectedMinionId, patch));
        }

        private void OpponentKeywordToggle(Transform parent, MinionInstance minion, Keyword keyword, string label)
        {
            var hasKeyword = minion.Keywords.Contains(keyword);
            ToolbarButton(parent, label, hasKeyword, true, () =>
            {
                var next = new List<Keyword>(minion.Keywords);
                if (hasKeyword)
                {
                    next.Remove(keyword);
                }
                else
                {
                    next.Add(keyword);
                }

                UpdateOpponentSelected(new MinionPatch { Keywords = next });
            });
        }

        private void BuildShopStage(Transform parent)
        {
            var stage = UiFactory.Panel("ShopStage", parent, ColorFromHex(0x181E24));
            UiFactory.SetHeight(stage, service.State.Player.Tavern.Discover == null ? 368 : 546);
            UiFactory.Vertical(stage, 12, 8);

            BuildDockHeader(stage.transform, "商店", "当前商店随从：" + service.State.Player.Tavern.Shop.Count);
            BuildSellDropZone(stage.transform);
            BuildTavernControls(stage.transform);
            BuildCardRow(stage.transform, "ShopRow", service.State.Player.Tavern.Shop, 172, (minion, index) =>
                CardWithDrag(minion, DragSource.Shop, index, null));

            if (service.State.Player.Tavern.Discover != null)
            {
                BuildDockHeader(stage.transform, "三连发现", "奖励 " + service.State.Player.Tavern.Discover.RewardTier + " 本");
                BuildCardRow(stage.transform, "DiscoverRow", service.State.Player.Tavern.Discover.Options, 162, (minion, index) =>
                    CardWithDrag(minion, DragSource.Discover, index, null));
            }
        }

        private void BuildTavernControls(Transform parent)
        {
            var controls = UiFactory.Panel("TavernControls", parent, Color.clear);
            UiFactory.SetHeight(controls, 36);
            UiFactory.Horizontal(controls, 0, 8);
            ResourcePill(controls.transform, "金币 " + service.State.Player.Tavern.Gold + "/" + service.State.Player.Tavern.MaxGold);
            ResourcePill(controls.transform, service.State.Player.Tavern.Tier + " 本酒馆");
            ResourcePill(controls.transform, "升级费用 " + service.State.Player.Tavern.UpgradeCost);
            var spacer = UiFactory.Panel("ControlsSpacer", controls.transform, Color.clear);
            UiFactory.SetFlexible(spacer, 1, 1);
            UiFactory.Button("CombatButton", controls.transform, "模拟战斗", () => Apply(new GameCommand(GameCommandType.SimulateCombat)));
        }

        private void BuildBottomDock(Transform parent)
        {
            var dock = UiFactory.Panel("BottomDock", parent, ColorFromHex(0x111820));
            UiFactory.SetHeight(dock, 220);
            UiFactory.Horizontal(dock, 12, 12);

            var hand = UiFactory.Panel("HandPanel", dock.transform, ColorFromHex(0x181E24));
            UiFactory.SetFlexible(hand, 1, 1);
            UiFactory.Vertical(hand, 10, 8);
            BuildDockHeader(hand.transform, "手牌", service.State.Player.Tavern.Hand.Count + "/10");
            AddDropTarget(hand, DropTarget.Hand);
            var handRow = BuildCardRow(hand.transform, "HandRow", service.State.Player.Tavern.Hand, 164, (minion, index) =>
                CardWithDrag(minion, DragSource.Hand, index, DropTarget.Hand));
            AddDropTarget(handRow, DropTarget.Hand);

            var replay = UiFactory.Panel("ReplayPanel", dock.transform, ColorFromHex(0x181E24));
            UiFactory.SetWidth(replay, 410);
            UiFactory.Vertical(replay, 10, 8);
            BuildDockHeader(replay.transform, "回放控制", service.State.CombatLog.Count + " 条战斗日志");
            var result = service.State.LastResult == null
                ? "尚未模拟战斗"
                : "结果：" + service.State.LastResult.Winner + "，步数 " + service.State.LastResult.Steps;
            var resultLabel = UiFactory.Label("CombatResult", replay.transform, result, 14, FontStyle.Bold);
            UiFactory.SetHeight(resultLabel.gameObject, 30);
            var hint = UiFactory.Label("ReplayHint", replay.transform, "点击“模拟战斗”后在右侧日志查看每一步。", 13);
            UiFactory.SetTextColor(hint, ColorFromHex(0x9AA7B4));
            UiFactory.SetHeight(hint.gameObject, 42);
        }

        private void BuildBoardPanel(Transform parent, string title, int health, int armor, List<MinionInstance> board, BoardSide side, bool flexible)
        {
            var panel = UiFactory.Panel(title + "Panel", parent, ColorFromHex(0x181E24));
            if (flexible)
            {
                UiFactory.SetFlexible(panel, 1, 1);
            }
            else
            {
                UiFactory.SetHeight(panel, 214);
            }

            UiFactory.Vertical(panel, 12, 8);
            BuildDockHeader(panel.transform, title, "生命 " + health + "  护甲 " + armor + "  随从 " + board.Count + "/7");

            if (!flexible)
            {
                BuildCompactBoardGrid(panel.transform, title + "CompactSlots", board, side);
                return;
            }

            var slots = new List<MinionInstance>(board);
            while (slots.Count < BoardLimit)
            {
                slots.Add(null);
            }

            var boardRow = BuildCardRow(panel.transform, title + "Slots", slots, 176, (minion, index) =>
            {
                if (minion == null)
                {
                    var empty = EmptySlot();
                    AddDropTarget(empty, DropTarget.PlayerBoard, index);
                    return empty;
                }

                return CardWithDrag(
                    minion,
                    side == BoardSide.Player ? DragSource.PlayerBoard : DragSource.OpponentBoard,
                    index,
                    side == BoardSide.Player ? DropTarget.PlayerBoard : (DropTarget?)null,
                    index);
            });
            AddDropTarget(boardRow, DropTarget.PlayerBoard);
        }

        private void BuildCompactBoardGrid(Transform parent, string name, List<MinionInstance> board, BoardSide side)
        {
            var grid = UiFactory.Panel(name, parent, ColorFromHex(0x141B22));
            UiFactory.SetFlexible(grid, 1, 1);
            UiFactory.Vertical(grid, 8, 6);

            var slots = new List<MinionInstance>(board);
            while (slots.Count < 4)
            {
                slots.Add(null);
            }

            for (var rowIndex = 0; rowIndex < 2; rowIndex += 1)
            {
                var row = UiFactory.Panel(name + "Row" + rowIndex, grid.transform, Color.clear);
                UiFactory.SetFlexible(row, 1, 1);
                UiFactory.Horizontal(row, 0, 6);
                for (var column = 0; column < 2; column += 1)
                {
                    var minion = slots[rowIndex * 2 + column];
                    var item = minion == null
                        ? EmptySlot()
                        : MiniCard(minion);
                    item.transform.SetParent(row.transform, false);
                    UiFactory.SetFlexible(item, 1, 1);
                }
            }
        }

        private void BuildMinionEditor(Transform parent)
        {
            var panel = UiFactory.Panel("MinionEditor", parent, ColorFromHex(0x202832));
            UiFactory.SetHeight(panel, 258);
            UiFactory.Vertical(panel, 10, 7);
            BuildDockHeader(panel.transform, "随从编辑", "选择卡牌后编辑");

            var selected = FindSelectedMinion();
            if (selected == null)
            {
                EmptyText(panel.transform, "选择商店、手牌或战场里的随从后编辑数值和关键词。");
                return;
            }

            var name = UiFactory.Label("SelectedName", panel.transform, selected.Name, 15, FontStyle.Bold);
            UiFactory.SetHeight(name.gameObject, 24);
            Stepper(panel.transform, "攻击", selected.Attack, value => UpdateSelected(new MinionPatch { Attack = value }));
            Stepper(panel.transform, "生命", selected.Health, value => UpdateSelected(new MinionPatch { Health = value }));
            Stepper(panel.transform, "最大生命", selected.MaxHealth, value => UpdateSelected(new MinionPatch { MaxHealth = value }));

            var toggles = UiFactory.Panel("EditorToggles", panel.transform, Color.clear);
            UiFactory.SetHeight(toggles, 34);
            UiFactory.Horizontal(toggles, 0, 6);
            ToolbarButton(toggles.transform, selected.Golden ? "金色：开" : "金色：关", selected.Golden, true, () => UpdateSelected(new MinionPatch { Golden = !selected.Golden }));
            KeywordToggle(toggles.transform, selected, Keyword.Taunt, "嘲讽");
            KeywordToggle(toggles.transform, selected, Keyword.DivineShield, "圣盾");
            KeywordToggle(toggles.transform, selected, Keyword.Reborn, "复生");
        }

        private void BuildSellDropZone(Transform parent)
        {
            var zone = UiFactory.Panel("SellDropZone", parent, ColorFromHex(0x2A1D20));
            UiFactory.SetHeight(zone, 78);
            UiFactory.Horizontal(zone, 12, 8);
            AddDropTarget(zone, DropTarget.SellZone);

            var label = UiFactory.Label("SellDropZoneText", zone.transform, "战场随从拖到这里出售", 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            UiFactory.SetTextColor(label, ColorFromHex(0xF0C6C6));
            UiFactory.SetFlexible(label.gameObject, 1, 1);
        }

        private void BuildHints(Transform parent)
        {
            var panel = UiFactory.Panel("AdvisorPanel", parent, ColorFromHex(0x202832));
            UiFactory.SetHeight(panel, 132);
            UiFactory.Vertical(panel, 10, 6);
            BuildDockHeader(panel.transform, "搜索计划 / AI 提示", "本地建议");
            var lines = advisor.GetAdvice(service.State).Concat(service.State.RecruitHints.Select(hint => hint.Message)).Take(3);
            foreach (var line in lines)
            {
                var label = UiFactory.Label("Hint", panel.transform, "- " + line, 13);
                UiFactory.SetTextColor(label, ColorFromHex(0xDCA94A));
                UiFactory.SetHeight(label.gameObject, 24);
            }
        }

        private void BuildLogs(Transform parent)
        {
            var panel = UiFactory.Panel("LogPanel", parent, ColorFromHex(0x202832));
            UiFactory.SetFlexible(panel, 1, 1);
            UiFactory.Vertical(panel, 10, 5);
            BuildDockHeader(panel.transform, "日志", lastError == null ? "招募 / 战斗" : lastError);

            foreach (var entry in service.State.Player.Tavern.RecruitLog.Skip(System.Math.Max(0, service.State.Player.Tavern.RecruitLog.Count - 5)))
            {
                LogLine(panel.transform, entry.Seq + ". " + entry.Message + " (" + entry.GoldBefore + "->" + entry.GoldAfter + ")");
            }

            foreach (var entry in service.State.CombatLog.Take(5))
            {
                LogLine(panel.transform, entry.Seq + ". " + entry.Detail);
            }
        }

        private void BuildDockHeader(Transform parent, string title, string meta)
        {
            var header = UiFactory.Panel(title + "Header", parent, Color.clear);
            UiFactory.SetHeight(header, 28);
            UiFactory.Horizontal(header, 0, 8);
            var titleLabel = UiFactory.Label(title + "Title", header.transform, title, 16, FontStyle.Bold);
            UiFactory.SetFlexible(titleLabel.gameObject, 1, 1);
            var metaLabel = UiFactory.Label(title + "Meta", header.transform, meta, 12);
            metaLabel.alignment = TextAnchor.MiddleRight;
            UiFactory.SetTextColor(metaLabel, ColorFromHex(0x9AA7B4));
            UiFactory.SetWidth(metaLabel.gameObject, 160);
        }

        private GameObject BuildCardRow(Transform parent, string name, List<MinionInstance> minions, float height, System.Func<MinionInstance, int, GameObject> builder)
        {
            var row = UiFactory.Panel(name, parent, ColorFromHex(0x141B22));
            UiFactory.SetHeight(row, height);
            UiFactory.Horizontal(row, 8, 8);

            if (minions.Count == 0)
            {
                EmptyText(row.transform, "暂无随从");
                return row;
            }

            for (var index = 0; index < minions.Count; index += 1)
            {
                var item = builder(minions[index], index);
                item.transform.SetParent(row.transform, false);
                UiFactory.SetWidth(item, 118);
            }

            return row;
        }

        private GameObject CardWithDrag(MinionInstance minion, DragSource source, int index, DropTarget? dropTarget, int targetIndex = -1)
        {
            if (minion == null)
            {
                return EmptySlot();
            }

            var holder = UiFactory.Panel("CardHolder", null, Color.clear);
            UiFactory.SetMinSize(holder, 118, 145);
            UiFactory.Vertical(holder, 0, 5);

            var card = new GameObject("Card-" + minion.InstanceId, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(holder.transform, false);
            UiFactory.SetHeight(card, 136);
            var image = card.GetComponent<Image>();
            image.color = minion.InstanceId == selectedMinionId
                ? ColorFromHex(0x273F57)
                : minion.Golden ? ColorFromHex(0x5B4718) : ColorFromHex(0x202832);
            var drag = card.AddComponent<DragCardBehaviour>();
            drag.Initialize(this, minion, source, index);
            if (dropTarget.HasValue)
            {
                AddDropTarget(card, dropTarget.Value, targetIndex);
                AddDropTarget(holder, dropTarget.Value, targetIndex);
            }

            UiFactory.Vertical(card, 6, 2);
            SmallLabel(card.transform, minion.TavernTier + " 本  " + TribesText(minion), ColorFromHex(0x9AA7B4), 11);
            SmallLabel(card.transform, minion.Name, ColorFromHex(0xEDF2F7), 13, FontStyle.Bold);
            var effect = SmallLabel(card.transform, EffectText(minion), ColorFromHex(0xD6DEE6), 11);
            UiFactory.SetHeight(effect.gameObject, 32);
            SmallLabel(card.transform, KeywordsText(minion), ColorFromHex(0xDCA94A), 10);
            SmallLabel(card.transform, minion.Attack + " / " + minion.Health, ColorFromHex(0xEDF2F7), 15, FontStyle.Bold);
            return holder;
        }

        private GameObject MiniCard(MinionInstance minion)
        {
            var holder = UiFactory.Panel("MiniCardHolder", null, Color.clear);
            UiFactory.Vertical(holder, 0, 3);

            var card = new GameObject("MiniCard-" + minion.InstanceId, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(holder.transform, false);
            UiFactory.SetFlexible(card, 1, 1);
            card.GetComponent<Image>().color = minion.InstanceId == selectedMinionId
                ? ColorFromHex(0x273F57)
                : minion.Golden ? ColorFromHex(0x5B4718) : ColorFromHex(0x202832);
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                Select(minion);
                Rebuild();
            });
            UiFactory.Vertical(card, 5, 1);
            SmallLabel(card.transform, minion.Name, ColorFromHex(0xEDF2F7), 11, FontStyle.Bold);
            SmallLabel(card.transform, minion.Attack + "/" + minion.Health + "  " + minion.TavernTier + " 本", ColorFromHex(0xDCA94A), 11);
            return holder;
        }

        private GameObject EmptySlot()
        {
            var empty = UiFactory.Panel("EmptySlot", null, ColorFromHex(0x101418));
            UiFactory.Vertical(empty, 6, 4);
            var label = UiFactory.Label("EmptySlotText", empty.transform, "空位", 13);
            label.alignment = TextAnchor.MiddleCenter;
            UiFactory.SetTextColor(label, ColorFromHex(0x9AA7B4));
            return empty;
        }

        private void AddDropTarget(GameObject target, DropTarget dropTarget, int targetIndex = -1)
        {
            var image = target.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            var behaviour = target.GetComponent<DropTargetBehaviour>() ?? target.AddComponent<DropTargetBehaviour>();
            behaviour.Initialize(this, dropTarget, targetIndex);
        }

        private void Stepper(Transform parent, string label, int value, System.Action<int> onChange)
        {
            var row = UiFactory.Panel(label + "Stepper", parent, Color.clear);
            UiFactory.SetHeight(row, 32);
            UiFactory.Horizontal(row, 0, 6);
            var text = UiFactory.Label(label + "Label", row.transform, label + "  " + value, 13);
            UiFactory.SetFlexible(text.gameObject, 1, 1);
            ToolbarButton(row.transform, "-", false, true, () => onChange(value - 1));
            ToolbarButton(row.transform, "+", false, true, () => onChange(value + 1));
        }

        private void KeywordToggle(Transform parent, MinionInstance minion, Keyword keyword, string label)
        {
            var hasKeyword = minion.Keywords.Contains(keyword);
            ToolbarButton(parent, label, hasKeyword, true, () =>
            {
                var next = new List<Keyword>(minion.Keywords);
                if (hasKeyword)
                {
                    next.Remove(keyword);
                }
                else
                {
                    next.Add(keyword);
                }

                UpdateSelected(new MinionPatch { Keywords = next });
            });
        }

        private void ResourcePill(Transform parent, string text)
        {
            var pill = UiFactory.Panel("ResourcePill", parent, ColorFromHex(0x202832));
            UiFactory.SetWidth(pill, 126);
            UiFactory.Horizontal(pill, 8, 0);
            var label = UiFactory.Label("ResourceText", pill.transform, text, 13, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
        }

        private Button ToolbarButton(Transform parent, string text, bool active, bool enabled, UnityAction onClick)
        {
            var button = UiFactory.Button(text + "Button", parent, text, onClick);
            button.interactable = enabled;
            UiFactory.SetHeight(button.gameObject, 32);
            UiFactory.SetWidth(button.gameObject, 84);
            UiFactory.SetImageColor(button.gameObject, active ? ColorFromHex(0x273F57) : ColorFromHex(0x202832));
            if (!enabled)
            {
                UiFactory.SetImageColor(button.gameObject, ColorFromHex(0x181E24));
            }

            return button;
        }

        private Text SmallLabel(Transform parent, string text, Color color, int size, FontStyle style = FontStyle.Normal)
        {
            var label = UiFactory.Label("CardLabel", parent, text, size, style);
            UiFactory.SetTextColor(label, color);
            UiFactory.SetHeight(label.gameObject, size + 6);
            return label;
        }

        private void EmptyText(Transform parent, string text)
        {
            var label = UiFactory.Label("EmptyText", parent, text, 13);
            label.alignment = TextAnchor.MiddleCenter;
            UiFactory.SetTextColor(label, ColorFromHex(0x9AA7B4));
            UiFactory.SetFlexible(label.gameObject, 1, 1);
        }

        private void LogLine(Transform parent, string text)
        {
            var label = UiFactory.Label("LogLine", parent, text, 12);
            UiFactory.SetHeight(label.gameObject, 22);
        }

        internal void BeginDrag(MinionInstance minion, DragSource source, int index, PointerEventData eventData)
        {
            activeDrag = new DragContext
            {
                Minion = minion,
                Source = source,
                Index = index
            };
            Select(minion);
            CreateDragGhost(minion, eventData);
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
            if (activeDrag != null)
            {
                activeDrag = null;
            }

            DestroyDragGhost();
        }

        internal void HandleDrop(DropTarget target, int targetIndex = -1)
        {
            if (activeDrag == null)
            {
                return;
            }

            var drag = activeDrag;
            if (!TryBuildDropCommand(drag, target, targetIndex, out var command))
            {
                lastError = "请拖到正确区域。";
                EndDrag();
                Rebuild();
                return;
            }

            selectedMinionId = drag.Minion.InstanceId;
            if (target == DropTarget.SellZone)
            {
                selectedMinionId = null;
            }

            activeDrag = null;
            DestroyDragGhost();
            Apply(command);
        }

        private static bool TryBuildDropCommand(DragContext drag, DropTarget target, int targetIndex, out GameCommand command)
        {
            command = null;
            if (drag.Source == DragSource.Shop && target == DropTarget.Hand)
            {
                command = new GameCommand(GameCommandType.BuyMinion, drag.Index);
                return true;
            }

            if (drag.Source == DragSource.Discover && target == DropTarget.Hand)
            {
                command = new GameCommand(GameCommandType.ChooseDiscover, drag.Index);
                return true;
            }

            if (drag.Source == DragSource.Hand && target == DropTarget.PlayerBoard)
            {
                command = new GameCommand(GameCommandType.PlayMinion, drag.Index, targetIndex);
                return true;
            }

            if (drag.Source == DragSource.PlayerBoard && target == DropTarget.PlayerBoard)
            {
                command = new GameCommand(GameCommandType.MoveBoardMinion, drag.Minion.InstanceId, targetIndex);
                return true;
            }

            if (drag.Source == DragSource.PlayerBoard && target == DropTarget.Hand)
            {
                command = new GameCommand(GameCommandType.MoveMinion, drag.Minion.InstanceId);
                return true;
            }

            if (drag.Source == DragSource.PlayerBoard && target == DropTarget.SellZone)
            {
                command = new GameCommand(GameCommandType.SellMinion, drag.Minion.InstanceId);
                return true;
            }

            return false;
        }

        private void CreateDragGhost(MinionInstance minion, PointerEventData eventData)
        {
            DestroyDragGhost();

            dragGhost = new GameObject("DragGhost-" + minion.InstanceId, typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(CanvasGroup));
            dragGhost.transform.SetParent(root, false);
            dragGhost.transform.SetAsLastSibling();

            var rect = dragGhost.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(132, 154);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = dragGhost.GetComponent<Image>();
            image.color = minion.Golden ? ColorFromHex(0x6F5519) : ColorFromHex(0x2C3945);
            image.raycastTarget = false;

            var canvas = dragGhost.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5000;

            var group = dragGhost.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0.96f;

            UiFactory.Vertical(dragGhost, 8, 3);
            SmallLabel(dragGhost.transform, minion.TavernTier + " 本  " + TribesText(minion), ColorFromHex(0xB8C7D4), 11);
            SmallLabel(dragGhost.transform, minion.Name, ColorFromHex(0xFFFFFF), 14, FontStyle.Bold);
            var effect = SmallLabel(dragGhost.transform, EffectText(minion), ColorFromHex(0xD6DEE6), 11);
            UiFactory.SetHeight(effect.gameObject, 36);
            SmallLabel(dragGhost.transform, KeywordsText(minion), ColorFromHex(0xF1C968), 10);
            SmallLabel(dragGhost.transform, minion.Attack + " / " + minion.Health, ColorFromHex(0xFFFFFF), 16, FontStyle.Bold);

            MoveDragGhost(eventData);
        }

        private void MoveDragGhost(PointerEventData eventData)
        {
            var rect = dragGhost.GetComponent<RectTransform>();
            var rootRect = root as RectTransform;
            if (rootRect == null)
            {
                dragGhost.transform.position = eventData.position;
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootRect,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);
            rect.anchoredPosition = localPoint;
        }

        private void DestroyDragGhost()
        {
            if (dragGhost == null)
            {
                return;
            }

            Object.Destroy(dragGhost);
            dragGhost = null;
        }

        internal void Select(MinionInstance minion)
        {
            if (minion != null)
            {
                selectedMinionId = minion.InstanceId;
            }
        }

        private void UpdateSelected(MinionPatch patch)
        {
            if (selectedMinionId == null)
            {
                return;
            }

            Apply(new GameCommand(GameCommandType.UpdateMinion, selectedMinionId, patch));
        }

        private MinionInstance FindSelectedMinion()
        {
            if (string.IsNullOrEmpty(selectedMinionId))
            {
                return null;
            }

            var selected = AllMinions().FirstOrDefault(minion => minion.InstanceId == selectedMinionId);
            if (selected == null)
            {
                selectedMinionId = null;
            }

            return selected;
        }

        private IEnumerable<MinionInstance> AllMinions()
        {
            foreach (var minion in service.State.Player.Board)
            {
                yield return minion;
            }

            foreach (var minion in service.State.Opponent.Board)
            {
                yield return minion;
            }

            foreach (var minion in service.State.Player.Tavern.Hand)
            {
                yield return minion;
            }

            foreach (var minion in service.State.Player.Tavern.Shop)
            {
                if (minion != null)
                {
                    yield return minion;
                }
            }

            if (service.State.Player.Tavern.Discover == null)
            {
                yield break;
            }

            foreach (var minion in service.State.Player.Tavern.Discover.Options)
            {
                yield return minion;
            }
        }

        private void Apply(GameCommand command)
        {
            try
            {
                service.Apply(command);
                lastError = null;
            }
            catch (System.Exception exception)
            {
                lastError = exception.Message;
                Debug.LogWarning(exception.Message);
            }

            Rebuild();
        }

        internal void Rebuild()
        {
            Build();
        }

        private static string TribesText(MinionInstance minion)
        {
            return minion.Tribes == null || minion.Tribes.Count == 0
                ? "无种族"
                : string.Join(" / ", minion.Tribes.Select(TribeName).ToArray());
        }

        private static string EffectText(MinionInstance minion)
        {
            return string.IsNullOrEmpty(minion.Text) ? "无额外效果" : minion.Text;
        }

        private static string KeywordsText(MinionInstance minion)
        {
            return minion.Keywords == null || minion.Keywords.Count == 0
                ? "无关键词"
                : string.Join(" ", minion.Keywords.Take(3).Select(KeywordName).ToArray());
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
                default: return keyword.ToString();
            }
        }

        private sealed class DragContext
        {
            public MinionInstance Minion;
            public DragSource Source;
            public int Index;
        }

        internal static Color ColorFromHex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f);
        }
    }

    internal enum DragSource
    {
        Shop,
        Discover,
        Hand,
        PlayerBoard,
        OpponentBoard
    }

    internal enum DropTarget
    {
        Hand,
        PlayerBoard,
        SellZone
    }

    internal enum RightInspectorTab
    {
        Info,
        CardAcquisition,
        OpponentCustomization,
        BattleTest
    }

    internal sealed class DragCardBehaviour : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private TavernTrainerView view;
        private MinionInstance minion;
        private DragSource source;
        private int index;

        public void Initialize(TavernTrainerView owner, MinionInstance cardMinion, DragSource cardSource, int cardIndex)
        {
            view = owner;
            minion = cardMinion;
            source = cardSource;
            index = cardIndex;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (view == null || minion == null)
            {
                return;
            }

            view.Select(minion);
            view.Rebuild();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (view == null || minion == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            view.BeginDrag(minion, source, index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (view == null)
            {
                return;
            }

            view.MoveDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (view == null)
            {
                return;
            }

            view.EndDrag();
        }
    }

    internal sealed class DropTargetBehaviour : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private TavernTrainerView view;
        private DropTarget target;
        private int targetIndex;
        private Image image;
        private Color normalColor;
        private bool hasImage;

        public void Initialize(TavernTrainerView owner, DropTarget dropTarget, int dropTargetIndex)
        {
            view = owner;
            target = dropTarget;
            targetIndex = dropTargetIndex;
            image = GetComponent<Image>();
            hasImage = image != null;
            if (hasImage)
            {
                normalColor = image.color;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            RestoreColor();
            if (view == null)
            {
                return;
            }

            view.HandleDrop(target, targetIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!hasImage)
            {
                return;
            }

            image.color = HighlightColor(target, normalColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RestoreColor();
        }

        private void RestoreColor()
        {
            if (hasImage)
            {
                image.color = normalColor;
            }
        }

        private static Color HighlightColor(DropTarget dropTarget, Color baseColor)
        {
            switch (dropTarget)
            {
                case DropTarget.Hand:
                    return Color.Lerp(baseColor, TavernTrainerView.ColorFromHex(0x2F5F6F), 0.65f);
                case DropTarget.PlayerBoard:
                    return Color.Lerp(baseColor, TavernTrainerView.ColorFromHex(0x3D5B38), 0.65f);
                case DropTarget.SellZone:
                    return Color.Lerp(baseColor, TavernTrainerView.ColorFromHex(0x7A2C32), 0.65f);
                default:
                    return baseColor;
            }
        }
    }
}
