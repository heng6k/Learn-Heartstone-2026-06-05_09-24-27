using System;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.MainHub
{
    public sealed class MainHubView
    {
        private const string TavernTitle = "酒馆训练器";
        private const string TavernDescription = "进入单人酒馆训练、拖拽摆位、战斗测试与回放";

        private readonly Transform root;
        private readonly Action openTrainer;
        private readonly Action openUnityTrainer;
        private readonly UnityTavernLayoutContext layout;
        private readonly bool useEnglish;
        private readonly Action<bool> languageChanged;

        public MainHubView(
            Transform root,
            Action openTrainer,
            Action openRealisticTrainer,
            Action openUnityTrainer = null,
            UnityTavernLayoutContext? layoutContext = null,
            bool useEnglish = false,
            Action<bool> languageChanged = null)
        {
            this.root = root;
            this.openTrainer = openTrainer;
            this.openUnityTrainer = openUnityTrainer;
            layout = layoutContext ?? UnityTavernLayoutContext.FromRoot(root);
            this.useEnglish = useEnglish;
            this.languageChanged = languageChanged;
        }

        public void Build()
        {
            var shell = UiFactory.Panel("MainHub", root, UnityTavernUiStyle.BackWall);
            UiFactory.Stretch(shell.GetComponent<RectTransform>());
            UiFactory.Vertical(shell, layout.IsCompact ? 14 : 22, layout.IsCompact ? 10 : 16);

            BuildHeader(shell.transform);

            var primaryTrainer = openUnityTrainer ?? openTrainer;
            ModuleButton(
                shell.transform,
                TavernTitle,
                T(TavernTitle, "Tavern Trainer"),
                T(TavernDescription, "Practice solo Battlegrounds, board positioning, combat tests, and replays"),
                primaryTrainer != null,
                primaryTrainer,
                true,
                layout);

            var grid = UiFactory.Panel("ModuleGrid", shell.transform, UnityTavernUiStyle.TableDark);
            UnityTavernUiStyle.ConfigureOutline(grid, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.32f), new Vector2(1f, -1f));
            UiFactory.SetFlexible(grid, 1, 1);
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            ConfigureModuleGrid(gridLayout, layout);

            ModuleButton(grid.transform, "英雄训练", T("英雄训练", "Hero Training"), T("后续扩展入口", "Coming later"), false, null, false, layout);
            ModuleButton(grid.transform, "阵容库", T("阵容库", "Lineup Library"), T("后续扩展入口", "Coming later"), false, null, false, layout);
            ModuleButton(grid.transform, "教学场景", T("教学场景", "Tutorials"), T("后续扩展入口", "Coming later"), false, null, false, layout);
            ModuleButton(grid.transform, "数据浏览", T("数据浏览", "Data Browser"), T("后续扩展入口", "Coming later"), false, null, false, layout);
        }

        private string T(string chinese, string english)
        {
            return useEnglish ? english : chinese;
        }

        private void BuildHeader(Transform parent)
        {
            var header = UiFactory.Panel("MainHubHeader", parent, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.96f));
            UiFactory.SetHeight(header, layout.IsCompact ? 74f : 92f);
            UnityTavernUiStyle.ConfigureOutline(header, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.42f), new Vector2(1f, -1f));
            UnityTavernUiStyle.AddStarLanternRail(header.transform, "MainHubStarLantern", UnityTavernUiStyle.ArcaneBlue);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(0, 0, 0, 0);
            headerLayout.spacing = layout.IsCompact ? 10 : 16;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            var titleStack = UiFactory.Panel("MainHubTitleStack", header.transform, Color.clear);
            UiFactory.SetFlexible(titleStack, 1, 0);
            var titleLayout = titleStack.AddComponent<VerticalLayoutGroup>();
            titleLayout.padding = new RectOffset(0, 0, 0, 0);
            titleLayout.spacing = layout.IsCompact ? 4 : 6;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = true;
            titleLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("Title", titleStack.transform, "Learn Heartstone", layout.IsCompact ? 28 : 34, FontStyle.Bold);
            title.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetHeight(title.gameObject, layout.IsCompact ? 40 : 52);

            var subtitle = UiFactory.Label("Subtitle", titleStack.transform, T("训练功能大厅", "Training Hub"), layout.IsCompact ? 16 : 20);
            subtitle.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(subtitle.gameObject, layout.IsCompact ? 24 : 30);

            BuildLanguageSwitch(header.transform);
        }

        private void BuildLanguageSwitch(Transform parent)
        {
            var row = UiFactory.Panel("MainHubLanguageSwitch", parent, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.96f));
            UnityTavernUiStyle.SetFixedSize(row, layout.IsCompact ? 230f : 260f, 52f);
            UnityTavernUiStyle.ConfigureOutline(row, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.42f), new Vector2(1f, -1f));

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(6, 6, 3, 3);
            rowLayout.spacing = 6;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var label = UiFactory.Label("MainHubLanguageLabel", row.transform, T("语言", "Language"), 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleRight;
            label.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(label.gameObject, layout.IsCompact ? 48f : 68f, UnityTavernUiStyle.TouchHeight);

            var chinese = LanguageButton("MainHubLanguageChineseButton", row.transform, "中文", !useEnglish, UnityTavernUiStyle.Gold, () => RequestLanguage(false));
            UnityTavernUiStyle.SetFixedSize(chinese.gameObject, layout.IsCompact ? 72f : 78f, UnityTavernUiStyle.TouchHeight);

            var english = LanguageButton("MainHubLanguageEnglishButton", row.transform, "English", useEnglish, UnityTavernUiStyle.Blue, () => RequestLanguage(true));
            UnityTavernUiStyle.SetFixedSize(english.gameObject, layout.IsCompact ? 88f : 96f, UnityTavernUiStyle.TouchHeight);
        }

        private void RequestLanguage(bool nextUseEnglish)
        {
            if (useEnglish == nextUseEnglish)
            {
                return;
            }

            languageChanged?.Invoke(nextUseEnglish);
        }

        private static Button LanguageButton(string name, Transform parent, string text, bool active, Color accentColor, Action onClick)
        {
            var button = UiFactory.Button(name, parent, text, () => onClick?.Invoke());
            UnityTavernUiStyle.ConfigureButton(button, accentColor, active, active);

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 14;
                label.fontStyle = FontStyle.Bold;
                label.color = active ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Text;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.text = active ? "✓ " + text : text;
            }

            return button;
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

        private void ModuleButton(
            Transform parent,
            string title,
            string body,
            bool enabled,
            Action action,
            bool primary,
            UnityTavernLayoutContext context)
        {
            ModuleButton(parent, title, title, body, enabled, action, primary, context);
        }

        private void ModuleButton(
            Transform parent,
            string objectName,
            string title,
            string body,
            bool enabled,
            Action action,
            bool primary,
            UnityTavernLayoutContext context)
        {
            var tile = UiFactory.Panel(
                objectName,
                parent,
                enabled
                    ? Color.Lerp(UnityTavernUiStyle.TableDark, UnityTavernUiStyle.TableLit, primary ? 0.36f : 0.16f)
                    : Color.Lerp(UnityTavernUiStyle.SurfaceDark, UnityTavernUiStyle.Disabled, 0.18f));
            UnityTavernUiStyle.ConfigureOutline(
                tile,
                UnityTavernUiStyle.WithAlpha(primary ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.ParchmentDark, primary ? 0.68f : 0.32f),
                new Vector2(1f, -1f));
            if (primary)
            {
                UnityTavernUiStyle.AddStarLanternRail(tile.transform, "MainHubPrimaryStarLantern", UnityTavernUiStyle.Gold);
            }
            var padding = primary ? (context.IsCompact ? 14 : 18) : 12;
            UiFactory.Vertical(tile, padding, context.IsCompact ? 6 : 8);
            if (primary)
            {
                UiFactory.SetHeight(tile, context.IsCompact ? 148 : 168);
            }

            var heading = UiFactory.Label(objectName + "Title", tile.transform, title, primary ? (context.IsCompact ? 22 : 26) : 18, FontStyle.Bold);
            heading.color = enabled ? UnityTavernUiStyle.TextLight : UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(heading.gameObject, primary ? (context.IsCompact ? 32 : 38) : 28);
            var desc = UiFactory.Label(objectName + "Body", tile.transform, body, primary ? 16 : 14);
            desc.color = enabled ? UnityTavernUiStyle.TextMuted : UnityTavernUiStyle.Disabled;
            UiFactory.SetHeight(desc.gameObject, primary ? (context.IsCompact ? 42 : 48) : 32);
            var button = UiFactory.Button(objectName + "Button", tile.transform, enabled ? T("进入", "Enter") : T("预留", "Reserved"), () => action?.Invoke());
            button.interactable = enabled;
            UnityTavernUiStyle.ConfigureButton(button, primary ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.ParchmentDark, primary, false);
            UiFactory.SetHeight(button.gameObject, primary || context.IsCompact ? UnityTavernUiStyle.CompactTouchHeight : UnityTavernUiStyle.TouchHeight);
        }
    }
}
