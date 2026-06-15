using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernTribeSelectionView
    {
        private readonly Transform root;
        private readonly Action<List<Tribe>> start;
        private readonly Action backToHub;
        private readonly UnityTavernLayoutContext layout;
        private readonly HashSet<Tribe> selected = new HashSet<Tribe>();
        private GameObject shell;

        public UnityTavernTribeSelectionView(
            Transform root,
            Action<List<Tribe>> start,
            Action backToHub,
            UnityTavernLayoutContext? layoutContext = null)
        {
            this.root = root;
            this.start = start;
            this.backToHub = backToHub;
            layout = layoutContext ?? UnityTavernLayoutContext.FromRoot(root);
        }

        public void Build()
        {
            if (shell == null)
            {
                shell = UiFactory.Panel("UnityTavernTribeSelection", root, UnityTavernUiStyle.BackWall);
                UnityTavernUiStyle.Stretch(shell.GetComponent<RectTransform>());
            }
            else
            {
                ClearChildren(shell.transform);
            }

            var page = UiFactory.Panel("UnityTribeSelectionPage", shell.transform, UnityTavernUiStyle.Panel);
            var pageRect = page.GetComponent<RectTransform>();
            pageRect.anchorMin = new Vector2(0.08f, 0.08f);
            pageRect.anchorMax = new Vector2(0.92f, 0.92f);
            pageRect.offsetMin = Vector2.zero;
            pageRect.offsetMax = Vector2.zero;
            UnityTavernUiStyle.ConfigureOutline(page, new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.42f), new Vector2(2f, -2f));

            var vertical = page.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(layout.IsCompact ? 14 : 24, layout.IsCompact ? 14 : 24, layout.IsCompact ? 14 : 22, layout.IsCompact ? 14 : 22);
            vertical.spacing = layout.IsCompact ? 10 : 14;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            BuildHeader(page.transform);
            BuildTribeGrid(page.transform);
            BuildQuickActions(page.transform);
        }

        private void BuildHeader(Transform parent)
        {
            var header = UiFactory.Panel("UnityTribeSelectionHeader", parent, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(header, layout.IsCompact ? 92f : 108f);
            var headerLayout = header.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(12, 12, 8, 8);
            headerLayout.spacing = 6;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityTribeSelectionTitle", header.transform, "选择本局种族", layout.IsCompact ? 22 : 26, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 34f : 40f);

            var count = selected.Count + "/5";
            var names = selected.Count == 0 ? "尚未选择" : string.Join(" / ", TribeAvailabilityRules.PlayableTribes.Where(selected.Contains).Select(TribeName).ToArray());
            var summary = UiFactory.Label("UnityTribeSelectionSummary", header.transform, "已选 " + count + "  " + names, layout.IsCompact ? 13 : 15, FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleCenter;
            summary.color = selected.Count == 5 ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, layout.IsCompact ? 30f : 34f);
        }

        private void BuildTribeGrid(Transform parent)
        {
            var gridObject = UiFactory.Panel("UnityTribeSelectionGrid", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetFlexible(gridObject, 1f, 1f);
            var grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.spacing = layout.IsCompact ? new Vector2(8f, 8f) : new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = layout.IsCompact ? 2 : 5;
            grid.cellSize = layout.IsCompact ? new Vector2(220f, 58f) : new Vector2(170f, 72f);

            foreach (var tribe in TribeAvailabilityRules.PlayableTribes)
            {
                BuildTribeButton(gridObject.transform, tribe);
            }
        }

        private void BuildTribeButton(Transform parent, Tribe tribe)
        {
            var isSelected = selected.Contains(tribe);
            var canSelect = isSelected || selected.Count < 5;
            var buttonObject = new GameObject("UnityTribeSelection" + tribe + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = isSelected
                ? Color.Lerp(UnityTavernUiStyle.PanelRaised, TribeAccent(tribe), 0.55f)
                : UnityTavernUiStyle.Panel;

            var button = buttonObject.GetComponent<Button>();
            button.interactable = canSelect;
            button.onClick.AddListener(() =>
            {
                ToggleTribe(tribe);
                Build();
            });
            UnityTavernUiStyle.TintSelectable(button, image.color, Color.Lerp(image.color, Color.white, 0.16f), Color.Lerp(image.color, Color.black, 0.16f));
            UnityTavernUiStyle.ConfigureOutline(
                buttonObject,
                isSelected ? new Color(TribeAccent(tribe).r, TribeAccent(tribe).g, TribeAccent(tribe).b, 0.86f) : new Color(0f, 0f, 0f, 0.24f),
                isSelected ? new Vector2(2f, -2f) : new Vector2(1f, -1f));

            var label = UiFactory.Label(buttonObject.name + "Text", buttonObject.transform, TribeName(tribe), layout.IsCompact ? 16 : 18, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private void BuildQuickActions(Transform parent)
        {
            var row = UiFactory.Panel("UnityTribeSelectionActions", parent, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 58f : 64f);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(10, 10, 8, 8);
            rowLayout.spacing = 10;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            ActionButton("UnityTribeSelectionBackButton", row.transform, "返回", true, backToHub);
            ActionButton("UnityTribeSelectionRandomButton", row.transform, "随机5个", true, () =>
            {
                SelectRandomFive();
                Build();
            });
            ActionButton("UnityTribeSelectionAllButton", row.transform, "全部10个种族", true, () => start?.Invoke(TribeAvailabilityRules.AllPlayableTribes()));
            ActionButton("UnityTribeSelectionEnterButton", row.transform, "进入酒馆", selected.Count == 5, () => start?.Invoke(TribeAvailabilityRules.PlayableTribes.Where(selected.Contains).ToList()));
        }

        private void ToggleTribe(Tribe tribe)
        {
            if (selected.Contains(tribe))
            {
                selected.Remove(tribe);
                return;
            }

            if (selected.Count < 5)
            {
                selected.Add(tribe);
            }
        }

        private void SelectRandomFive()
        {
            selected.Clear();
            var rng = new System.Random(Environment.TickCount);
            foreach (var tribe in TribeAvailabilityRules.PlayableTribes.OrderBy(_ => rng.Next()).Take(5))
            {
                selected.Add(tribe);
            }
        }

        private static Button ActionButton(string name, Transform parent, string text, bool interactable, Action onClick)
        {
            var button = UiFactory.Button(name, parent, text, () => onClick?.Invoke());
            button.interactable = interactable;
            UnityTavernUiStyle.SetPreferredHeight(button.gameObject, UnityTavernUiStyle.TouchHeight);
            var normal = interactable ? UnityTavernUiStyle.Panel : UnityTavernUiStyle.PanelQuiet;
            UnityTavernUiStyle.EnsureComponent<Image>(button.gameObject).color = normal;
            UnityTavernUiStyle.TintSelectable(button, normal, Color.Lerp(normal, UnityTavernUiStyle.Gold, 0.20f), Color.Lerp(normal, Color.black, 0.16f));
            return button;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index -= 1)
            {
                var child = parent.GetChild(index).gameObject;
                if (UnityEngine.Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child);
                }
            }
        }

        private static Color TribeAccent(Tribe tribe)
        {
            switch (tribe)
            {
                case Tribe.Beast: return UnityTavernUiStyle.ColorFromHex(0x7A5C2E);
                case Tribe.Murloc: return UnityTavernUiStyle.ColorFromHex(0x2B7A78);
                case Tribe.Mech: return UnityTavernUiStyle.ColorFromHex(0x6A7480);
                case Tribe.Demon: return UnityTavernUiStyle.ColorFromHex(0x7A2F5A);
                case Tribe.Dragon: return UnityTavernUiStyle.ColorFromHex(0x8A3D31);
                case Tribe.Pirate: return UnityTavernUiStyle.ColorFromHex(0x3E5F8A);
                case Tribe.Elemental: return UnityTavernUiStyle.ColorFromHex(0xB26B2A);
                case Tribe.Quilboar: return UnityTavernUiStyle.ColorFromHex(0x9B4C36);
                case Tribe.Undead: return UnityTavernUiStyle.ColorFromHex(0x5D6580);
                case Tribe.Naga: return UnityTavernUiStyle.ColorFromHex(0x3D788A);
                default: return UnityTavernUiStyle.Green;
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
                default: return "中立";
            }
        }
    }
}
