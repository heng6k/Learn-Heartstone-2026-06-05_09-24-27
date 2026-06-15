using System;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.MainHub
{
    public sealed class MainHubView
    {
        private readonly Transform root;
        private readonly Action openTrainer;
        private readonly Action openUnityTrainer;
        private readonly UnityTavernLayoutContext layout;

        public MainHubView(
            Transform root,
            Action openTrainer,
            Action openRealisticTrainer,
            Action openUnityTrainer = null,
            UnityTavernLayoutContext? layoutContext = null)
        {
            this.root = root;
            this.openTrainer = openTrainer;
            this.openUnityTrainer = openUnityTrainer;
            layout = layoutContext ?? UnityTavernLayoutContext.FromRoot(root);
        }

        public void Build()
        {
            var shell = UiFactory.Panel("MainHub", root, new Color(0.08f, 0.10f, 0.12f));
            UiFactory.Stretch(shell.GetComponent<RectTransform>());
            UiFactory.Vertical(shell, layout.IsCompact ? 14 : 22, layout.IsCompact ? 10 : 16);

            var title = UiFactory.Label("Title", shell.transform, "Learn Heartstone", layout.IsCompact ? 28 : 34, FontStyle.Bold);
            UiFactory.SetHeight(title.gameObject, layout.IsCompact ? 42 : 54);

            var subtitle = UiFactory.Label("Subtitle", shell.transform, "训练功能大厅", layout.IsCompact ? 16 : 20);
            UiFactory.SetHeight(subtitle.gameObject, layout.IsCompact ? 26 : 34);

            var primaryTrainer = openUnityTrainer ?? openTrainer;
            ModuleButton(
                shell.transform,
                "酒馆训练器",
                "进入单人酒馆训练、拖拽摆位、战斗测试与回放",
                primaryTrainer != null,
                primaryTrainer,
                true,
                layout);

            var grid = UiFactory.Panel("ModuleGrid", shell.transform, new Color(0.10f, 0.13f, 0.16f));
            UiFactory.SetFlexible(grid, 1, 1);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            ConfigureModuleGrid(gridLayout, layout);

            ModuleButton(grid.transform, "英雄训练", "后续扩展入口", false, null, false, layout);
            ModuleButton(grid.transform, "阵容库", "后续扩展入口", false, null, false, layout);
            ModuleButton(grid.transform, "教学场景", "后续扩展入口", false, null, false, layout);
            ModuleButton(grid.transform, "数据浏览", "后续扩展入口", false, null, false, layout);
        }

        private static void ConfigureModuleGrid(GridLayoutGroup gridLayout, UnityTavernLayoutContext context)
        {
            var padding = context.IsCompact ? 12 : 18;
            var spacing = context.IsCompact ? 10 : 14;
            gridLayout.padding = new RectOffset(padding, padding, padding, padding);
            gridLayout.spacing = new Vector2(spacing, spacing);
            gridLayout.cellSize = context.IsCompact ? new Vector2(220, 96) : new Vector2(260, 116);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = context.IsWide ? 4 : 2;
        }

        private static void ModuleButton(
            Transform parent,
            string title,
            string body,
            bool enabled,
            Action action,
            bool primary,
            UnityTavernLayoutContext context)
        {
            var tile = UiFactory.Panel(title, parent, enabled ? new Color(0.16f, 0.24f, 0.31f) : new Color(0.14f, 0.14f, 0.14f));
            var padding = primary ? (context.IsCompact ? 14 : 18) : 12;
            UiFactory.Vertical(tile, padding, context.IsCompact ? 6 : 8);
            if (primary)
            {
                UiFactory.SetHeight(tile, context.IsCompact ? 148 : 168);
            }

            var heading = UiFactory.Label(title + "Title", tile.transform, title, primary ? (context.IsCompact ? 22 : 26) : 18, FontStyle.Bold);
            UiFactory.SetHeight(heading.gameObject, primary ? (context.IsCompact ? 32 : 38) : 28);
            var desc = UiFactory.Label(title + "Body", tile.transform, body, primary ? 15 : 13);
            UiFactory.SetHeight(desc.gameObject, primary ? (context.IsCompact ? 42 : 48) : 32);
            var button = UiFactory.Button(title + "Button", tile.transform, enabled ? "进入" : "预留", () => action?.Invoke());
            button.interactable = enabled;
            UiFactory.SetHeight(button.gameObject, primary || context.IsCompact ? UnityTavernUiStyle.CompactTouchHeight : UnityTavernUiStyle.TouchHeight);
        }
    }
}
