using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer
{
    public sealed class TavernTrainerView
    {
        private readonly Transform root;
        private readonly MatchService service;
        private readonly IAdvisorService advisor;
        private readonly System.Action backToHub;
        private GameObject shell;

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
                Object.Destroy(shell);
            }

            shell = UiFactory.Panel("TavernTrainer", root, new Color(0.07f, 0.08f, 0.09f));
            UiFactory.Stretch(shell.GetComponent<RectTransform>());
            UiFactory.Vertical(shell, 10, 8);

            BuildTopBar(shell.transform);

            var main = UiFactory.Panel("TrainerMain", shell.transform, new Color(0.08f, 0.09f, 0.10f));
            UiFactory.SetFlexible(main, 1, 1);
            UiFactory.Horizontal(main, 8, 8);

            var center = UiFactory.Panel("CenterWorkspace", main.transform, new Color(0.09f, 0.11f, 0.13f));
            UiFactory.SetFlexible(center, 3, 1);
            UiFactory.Vertical(center, 8, 8);

            BuildShop(center.transform);
            BuildBoard(center.transform, "玩家战场", service.State.Player.Board, BoardSide.Player);
            BuildHand(center.transform);

            var side = UiFactory.Panel("RightWorkspace", main.transform, new Color(0.10f, 0.10f, 0.12f));
            UiFactory.SetFlexible(side, 1, 1);
            UiFactory.Vertical(side, 8, 8);

            BuildBoard(side.transform, service.State.Opponent.Name, service.State.Opponent.Board, BoardSide.Opponent);
            BuildEditor(side.transform);
            BuildHints(side.transform);
            BuildLogs(side.transform);
        }

        private void Rebuild()
        {
            Build();
        }

        private void BuildTopBar(Transform parent)
        {
            var bar = UiFactory.Panel("TopBar", parent, new Color(0.12f, 0.14f, 0.16f));
            UiFactory.SetHeight(bar, 58);
            UiFactory.Horizontal(bar, 8, 8);
            UiFactory.Button("BackButton", bar.transform, "返回大厅", () => backToHub());
            UiFactory.Button("RerollButton", bar.transform, "刷新(1)", () => Apply(new GameCommand(GameCommandType.RerollShop)));
            UiFactory.Button("FreezeButton", bar.transform, service.State.Player.Tavern.Frozen ? "解冻" : "冻结", () => Apply(new GameCommand(GameCommandType.FreezeShop, !service.State.Player.Tavern.Frozen)));
            UiFactory.Button("UpgradeButton", bar.transform, "升本(" + service.State.Player.Tavern.UpgradeCost + ")", () => Apply(new GameCommand(GameCommandType.UpgradeTavern)));
            UiFactory.Button("NextTurnButton", bar.transform, "下一回合", () => Apply(new GameCommand(GameCommandType.NextTurn)));
            UiFactory.Button("CombatButton", bar.transform, "模拟战斗", () => Apply(new GameCommand(GameCommandType.SimulateCombat)));
            UiFactory.Button("GoldButton", bar.transform, "+10 金币", () => Apply(new GameCommand(GameCommandType.DebugAddGold, 10)));

            var info = UiFactory.Label("MatchInfo", bar.transform, "回合 " + service.State.Round + " | " + service.State.Player.Tavern.Tier + " 本 | 金币 " + service.State.Player.Tavern.Gold + "/" + service.State.Player.Tavern.MaxGold, 17, FontStyle.Bold);
            UiFactory.SetFlexible(info.gameObject, 2, 1);
        }

        private void BuildShop(Transform parent)
        {
            var panel = Section(parent, "商店");
            var row = UiFactory.Panel("ShopRow", panel.transform, new Color(0.11f, 0.13f, 0.15f));
            UiFactory.SetHeight(row, 132);
            UiFactory.Horizontal(row, 6, 6);
            for (var i = 0; i < service.State.Player.Tavern.Shop.Count; i += 1)
            {
                var index = i;
                CardButton(row.transform, service.State.Player.Tavern.Shop[i], "购买", () => Apply(new GameCommand(GameCommandType.BuyMinion, index)));
            }

            if (service.State.Player.Tavern.Discover != null)
            {
                var discover = UiFactory.Panel("Discover", panel.transform, new Color(0.15f, 0.12f, 0.18f));
                UiFactory.SetHeight(discover, 126);
                UiFactory.Horizontal(discover, 6, 6);
                for (var i = 0; i < service.State.Player.Tavern.Discover.Options.Count; i += 1)
                {
                    var index = i;
                    CardButton(discover.transform, service.State.Player.Tavern.Discover.Options[i], "发现", () => Apply(new GameCommand(GameCommandType.ChooseDiscover, index)));
                }
            }
        }

        private void BuildHand(Transform parent)
        {
            var panel = Section(parent, "手牌");
            var row = UiFactory.Panel("HandRow", panel.transform, new Color(0.11f, 0.13f, 0.15f));
            UiFactory.SetHeight(row, 116);
            UiFactory.Horizontal(row, 6, 6);
            for (var i = 0; i < service.State.Player.Tavern.Hand.Count; i += 1)
            {
                var index = i;
                CardButton(row.transform, service.State.Player.Tavern.Hand[i], "打出", () => Apply(new GameCommand(GameCommandType.PlayMinion, index)));
            }
        }

        private void BuildBoard(Transform parent, string title, System.Collections.Generic.List<MinionInstance> board, BoardSide side)
        {
            var panel = Section(parent, title);
            var row = UiFactory.Panel(title + "Row", panel.transform, new Color(0.11f, 0.13f, 0.15f));
            UiFactory.SetHeight(row, 126);
            UiFactory.Horizontal(row, 6, 6);
            foreach (var minion in board)
            {
                CardButton(row.transform, minion, side == BoardSide.Player ? "出售" : "查看", () =>
                {
                    if (side == BoardSide.Player)
                    {
                        Apply(new GameCommand(GameCommandType.SellMinion, minion.InstanceId));
                    }
                });
            }
        }

        private void BuildEditor(Transform parent)
        {
            var panel = Section(parent, "编辑器");
            var text = "当前简版编辑器：可通过买入、打出、出售和调试金币修改局面。后续可在此扩展数值、关键词、双方阵容编辑。";
            var label = UiFactory.Label("EditorText", panel.transform, text, 14);
            UiFactory.SetHeight(label.gameObject, 72);
        }

        private void BuildHints(Transform parent)
        {
            var panel = Section(parent, "搜索/提示面板");
            var lines = advisor.GetAdvice(service.State).Concat(service.State.RecruitHints.Select(hint => hint.Message));
            foreach (var line in lines.Take(4))
            {
                var label = UiFactory.Label("Hint", panel.transform, "• " + line, 14);
                UiFactory.SetHeight(label.gameObject, 28);
            }
        }

        private void BuildLogs(Transform parent)
        {
            var panel = Section(parent, "日志 / 回放控制");
            foreach (var entry in service.State.Player.Tavern.RecruitLog.Skip(System.Math.Max(0, service.State.Player.Tavern.RecruitLog.Count - 5)))
            {
                var label = UiFactory.Label("RecruitLog", panel.transform, entry.Seq + ". " + entry.Message + " (" + entry.GoldBefore + "->" + entry.GoldAfter + ")", 13);
                UiFactory.SetHeight(label.gameObject, 24);
            }

            foreach (var entry in service.State.CombatLog.Take(5))
            {
                var label = UiFactory.Label("CombatLog", panel.transform, entry.Seq + ". " + entry.Detail, 13);
                UiFactory.SetHeight(label.gameObject, 24);
            }
        }

        private GameObject Section(Transform parent, string title)
        {
            var panel = UiFactory.Panel(title + "Section", parent, new Color(0.10f, 0.12f, 0.14f));
            UiFactory.SetFlexible(panel, 1, 1);
            UiFactory.Vertical(panel, 8, 6);
            var heading = UiFactory.Label(title + "Heading", panel.transform, title, 18, FontStyle.Bold);
            UiFactory.SetHeight(heading.gameObject, 28);
            return panel;
        }

        private static void CardButton(Transform parent, MinionInstance minion, string action, UnityEngine.Events.UnityAction onClick)
        {
            if (minion == null)
            {
                var empty = UiFactory.Panel("EmptySlot", parent, new Color(0.07f, 0.08f, 0.09f));
                UiFactory.Vertical(empty, 6, 4);
                var text = UiFactory.Label("EmptySlotText", empty.transform, "空", 14);
                text.alignment = TextAnchor.MiddleCenter;
                return;
            }

            var card = UiFactory.Panel(minion.InstanceId, parent, minion.Golden ? new Color(0.40f, 0.31f, 0.12f) : new Color(0.16f, 0.20f, 0.24f));
            UiFactory.Vertical(card, 6, 4);
            UiFactory.Label("Name", card.transform, minion.Name, 13, FontStyle.Bold);
            UiFactory.Label("Stats", card.transform, minion.Attack + "/" + minion.Health + " | " + minion.TavernTier + " 本", 13);
            UiFactory.Label("Keywords", card.transform, string.Join(", ", minion.Keywords.Select(keyword => keyword.ToString()).ToArray()), 11);
            var button = UiFactory.Button("Action", card.transform, action, onClick);
            UiFactory.SetHeight(button.gameObject, 28);
        }

        private void Apply(GameCommand command)
        {
            try
            {
                service.Apply(command);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(exception.Message);
            }

            Rebuild();
        }
    }
}
