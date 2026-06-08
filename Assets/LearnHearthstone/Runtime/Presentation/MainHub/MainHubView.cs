using System;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.MainHub
{
    public sealed class MainHubView
    {
        private readonly Transform root;
        private readonly Action openTrainer;

        public MainHubView(Transform root, Action openTrainer)
        {
            this.root = root;
            this.openTrainer = openTrainer;
        }

        public void Build()
        {
            var shell = UiFactory.Panel("MainHub", root, new Color(0.08f, 0.10f, 0.12f));
            UiFactory.Stretch(shell.GetComponent<RectTransform>());
            UiFactory.Vertical(shell, 22, 16);

            var title = UiFactory.Label("Title", shell.transform, "Learn Heartstone", 34, FontStyle.Bold);
            UiFactory.SetHeight(title.gameObject, 54);

            var subtitle = UiFactory.Label("Subtitle", shell.transform, "训练功能大厅", 20);
            UiFactory.SetHeight(subtitle.gameObject, 34);

            var grid = UiFactory.Panel("ModuleGrid", shell.transform, new Color(0.10f, 0.13f, 0.16f));
            UiFactory.SetFlexible(grid, 1, 1);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(18, 18, 18, 18);
            gridLayout.spacing = new Vector2(14, 14);
            gridLayout.cellSize = new Vector2(260, 128);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            ModuleButton(grid.transform, "酒馆训练器", "商店、手牌、战场、日志与回放", true, openTrainer);
            ModuleButton(grid.transform, "英雄训练", "后续扩展入口", false, null);
            ModuleButton(grid.transform, "阵容库", "后续扩展入口", false, null);
            ModuleButton(grid.transform, "教学场景", "后续扩展入口", false, null);
            ModuleButton(grid.transform, "数据浏览", "后续扩展入口", false, null);
            ModuleButton(grid.transform, "设置", "后续扩展入口", false, null);
        }

        private static void ModuleButton(Transform parent, string title, string body, bool enabled, Action action)
        {
            var tile = UiFactory.Panel(title, parent, enabled ? new Color(0.16f, 0.24f, 0.31f) : new Color(0.14f, 0.14f, 0.14f));
            UiFactory.Vertical(tile, 12, 6);
            var heading = UiFactory.Label(title + "Title", tile.transform, title, 20, FontStyle.Bold);
            UiFactory.SetHeight(heading.gameObject, 34);
            var desc = UiFactory.Label(title + "Body", tile.transform, body, 14);
            UiFactory.SetHeight(desc.gameObject, 42);
            var button = UiFactory.Button(title + "Button", tile.transform, enabled ? "进入" : "预留", () => action?.Invoke());
            button.interactable = enabled;
            UiFactory.SetHeight(button.gameObject, 34);
        }
    }
}
