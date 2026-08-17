using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.MainHub
{
    public sealed class GameVersionCenterView
    {
        private enum VersionTab
        {
            Overview,
            Heroes,
            Cards,
            Mechanics,
            Compare
        }

        private enum DifferenceKind
        {
            Added,
            Changed,
            Removed,
            Returned
        }

        private readonly Transform root;
        private readonly VersionedContentCatalog content;
        private readonly IReadOnlyList<GameVersionDefinition> versions;
        private readonly Action backToHub;
        private readonly UnityTavernLayoutContext layout;
        private readonly bool useEnglish;
        private string selectedVersionId;
        private VersionTab activeTab;
        private bool compactShowingList;
        private GameObject shell;

        public GameVersionCenterView(
            Transform root,
            VersionedContentCatalog content,
            string currentVersionId,
            Action backToHub,
            UnityTavernLayoutContext? layoutContext = null,
            bool useEnglish = false)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            this.backToHub = backToHub;
            layout = layoutContext ?? UnityTavernLayoutContext.FromRoot(root);
            this.useEnglish = useEnglish;
            versions = content.Versions.Versions
                .OrderByDescending(version => version.ReleaseDateUtc)
                .ThenBy(version => version.Id, StringComparer.Ordinal)
                .ToArray();
            if (versions.Count == 0)
            {
                throw new ArgumentException("Version center requires at least one game version.", nameof(content));
            }

            selectedVersionId = versions.Any(version => string.Equals(version.Id, currentVersionId, StringComparison.OrdinalIgnoreCase))
                ? currentVersionId
                : content.Versions.Default.Id;
        }

        public void Build()
        {
            if (shell == null)
            {
                shell = UiFactory.Panel("GameVersionCenter", root, UnityTavernUiStyle.BackWall);
                UiFactory.Stretch(shell.GetComponent<RectTransform>());
                UiFactory.Vertical(shell, CompactInt(8f, 16f), CompactInt(8f, 12f));
            }
            else
            {
                ClearChildren(shell.transform);
            }

            BuildHeader(shell.transform);
            if (layout.IsCompact)
            {
                if (compactShowingList)
                {
                    BuildCompactList(shell.transform);
                }
                else
                {
                    BuildCompactDetails(shell.transform);
                }
                return;
            }

            BuildDesktop(shell.transform);
        }

        private string T(string chinese, string english)
        {
            return useEnglish ? english : chinese;
        }

        private void BuildHeader(Transform parent)
        {
            var header = UiFactory.Panel("GameVersionCenterHeader", parent, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.98f));
            UiFactory.SetHeight(header, CompactUnits(58f, 72f));
            UnityTavernUiStyle.ConfigureOutline(header, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.52f), new Vector2(1f, -1f));
            UnityTavernUiStyle.AddStarLanternRail(header.transform, "GameVersionCenterStarLantern", UnityTavernUiStyle.ArcaneBlue);
            var row = UiFactory.Horizontal(header, CompactInt(4f, 8f), CompactInt(8f, 12f));
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;

            var back = UiFactory.Button("GameVersionCenterBackButton", header.transform, T("返回大厅", "Back to Hub"), () => backToHub?.Invoke(), layout);
            back.interactable = backToHub != null;
            UnityTavernUiStyle.ConfigureButton(back, UnityTavernUiStyle.Brass);
            if (layout.IsShortLandscape)
            {
                UnityTavernUiStyle.ApplyTavernButtonSkin(back, true);
            }
            UiFactory.SetWidth(back.gameObject, CompactUnits(142f, 170f));

            var title = UiFactory.Label("GameVersionCenterTitle", header.transform, T("版本中心", "Version Center"), layout.IsCompact ? 22 : 28, FontStyle.Bold, layout);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetFlexible(title.gameObject, 1f, 0f);

            var subtitle = UiFactory.Label(
                "GameVersionCenterSubtitle",
                header.transform,
                layout.IsCompact ? T("官方与训练器状态分开显示", "Official and trainer status") : T("浏览正式游戏版本、实现状态与逐项变化", "Browse versions, implementation status, and changes"),
                14,
                FontStyle.Bold,
                layout);
            subtitle.alignment = TextAnchor.MiddleRight;
            subtitle.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetWidth(subtitle.gameObject, CompactUnits(300f, 430f));
        }

        private void BuildDesktop(Transform parent)
        {
            var body = UiFactory.Panel("GameVersionCenterDesktopBody", parent, Color.clear);
            UiFactory.SetFlexible(body, 1f, 1f);
            var row = UiFactory.Horizontal(body, 0, 12);
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            BuildVersionList(body.transform, false);
            BuildDetails(body.transform, "GameVersionCenterDesktopDetailPage");
        }

        private void BuildCompactList(Transform parent)
        {
            var page = UiFactory.Panel("GameVersionCenterCompactListPage", parent, UnityTavernUiStyle.Panel);
            UiFactory.SetFlexible(page, 1f, 1f);
            UiFactory.Vertical(page, CompactInt(8f), CompactInt(8f));

            var title = UiFactory.Label("GameVersionCenterCompactListTitle", page.transform, T("选择要查看的版本", "Choose a version"), 18, FontStyle.Bold, layout);
            title.alignment = TextAnchor.MiddleCenter;
            UiFactory.SetHeight(title.gameObject, CompactUnits(38f));
            BuildVersionList(page.transform, true);
        }

        private void BuildCompactDetails(Transform parent)
        {
            var page = UiFactory.Panel("GameVersionCenterCompactDetailPage", parent, UnityTavernUiStyle.Panel);
            UiFactory.SetFlexible(page, 1f, 1f);
            UiFactory.Vertical(page, CompactInt(6f), CompactInt(6f));

            var list = UiFactory.Button("GameVersionCenterCompactListButton", page.transform, T("查看版本列表", "View Version List"), () =>
            {
                compactShowingList = true;
                Build();
            }, layout);
            UnityTavernUiStyle.ConfigureButton(list, UnityTavernUiStyle.ArcaneBlue);
            if (layout.IsShortLandscape)
            {
                UnityTavernUiStyle.ApplyTavernButtonSkin(list, true);
            }
            UiFactory.SetHeight(list.gameObject, CompactUnits(UnityTavernUiStyle.CompactTouchHeight));
            BuildDetails(page.transform, "GameVersionCenterCompactDetails");
        }

        private void BuildVersionList(Transform parent, bool compact)
        {
            var panel = UiFactory.Panel(compact ? "GameVersionCenterCompactVersionList" : "GameVersionCenterVersionList", parent, UnityTavernUiStyle.PanelRaised);
            UiFactory.SetFlexible(panel, compact ? 1f : 0f, 1f);
            if (!compact)
            {
                UiFactory.SetWidth(panel, 320f);
            }
            UiFactory.Vertical(panel, compact ? CompactInt(6f) : 10, compact ? CompactInt(8f) : 8);

            if (!compact)
            {
                var title = UiFactory.Label("GameVersionCenterVersionListTitle", panel.transform, T("游戏版本", "Game Versions"), 18, FontStyle.Bold, layout);
                title.color = UnityTavernUiStyle.Gold;
                UiFactory.SetHeight(title.gameObject, 34f);
            }

            var listContent = UiFactory.ScrollView("GameVersionCenterVersionListScroll", panel.transform, UnityTavernUiStyle.PanelQuiet, out _, layout);
            UiFactory.Vertical(listContent.gameObject, 8, 8);
            foreach (var version in versions)
            {
                var capturedId = version.Id;
                var selected = string.Equals(version.Id, selectedVersionId, StringComparison.OrdinalIgnoreCase);
                var label = GameVersionUiText.DisplayName(version, useEnglish) + "\n" + GameVersionUiText.Category(version, IsLatest(version), useEnglish);
                var button = UiFactory.Button("GameVersionCenterVersionButton-" + version.Id, listContent, label, () =>
                {
                    selectedVersionId = capturedId;
                    activeTab = VersionTab.Overview;
                    compactShowingList = false;
                    Build();
                }, layout);
                UnityTavernUiStyle.ConfigureButton(button, selected ? UnityTavernUiStyle.FocusRing : UnityTavernUiStyle.Brass, selected, selected);
                UiFactory.SetHeight(button.gameObject, compact ? CompactUnits(74f) : 78f);
            }
        }

        private void BuildDetails(Transform parent, string objectName)
        {
            var panel = UiFactory.Panel(objectName, parent, UnityTavernUiStyle.PanelRaised);
            UiFactory.SetFlexible(panel, 1f, 1f);
            UiFactory.Vertical(panel, CompactInt(6f, 12f), CompactInt(6f, 10f));

            var version = SelectedVersion();
            BuildDetailHeader(panel.transform, version);
            BuildTabs(panel.transform);

            var legend = UiFactory.Label(
                "GameVersionCenterDifferenceLegend",
                panel.transform,
                T("差异标记：+ 新增　~ 调整　- 移除　↩ 回归", "Change legend: + Added   ~ Changed   - Removed   ↩ Returned"),
                14,
                FontStyle.Bold,
                layout);
            legend.alignment = TextAnchor.MiddleCenter;
            legend.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(legend.gameObject, CompactUnits(34f, 38f));

            var contentRoot = UiFactory.ScrollView("GameVersionCenterDetailScroll", panel.transform, UnityTavernUiStyle.PanelQuiet, out _, layout);
            UiFactory.Vertical(contentRoot.gameObject, CompactInt(8f, 12f), CompactInt(8f, 10f));
            switch (activeTab)
            {
                case VersionTab.Heroes:
                    BuildRevisionList(contentRoot, version, revision => revision.Kind == EntityKind.Hero, T("此版本没有英雄差异。", "No hero changes in this version."));
                    break;
                case VersionTab.Cards:
                    BuildRevisionList(contentRoot, version, IsCardRevision, T("此版本没有卡牌差异。", "No card changes in this version."));
                    break;
                case VersionTab.Mechanics:
                    BuildMechanics(contentRoot, version);
                    break;
                case VersionTab.Compare:
                    BuildComparison(contentRoot, version);
                    break;
                default:
                    BuildOverview(contentRoot, version);
                    break;
            }
        }

        private void BuildDetailHeader(Transform parent, GameVersionDefinition version)
        {
            var header = UiFactory.Panel("GameVersionCenterDetailHeader", parent, UnityTavernUiStyle.Panel);
            UiFactory.SetHeight(header, CompactUnits(76f, 92f));
            var row = UiFactory.Horizontal(header, CompactInt(4f, 8f), CompactInt(6f, 10f));
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;

            var name = UiFactory.Label("GameVersionCenterVersionName", header.transform, GameVersionUiText.DisplayName(version, useEnglish), layout.IsCompact ? 18 : 24, FontStyle.Bold, layout);
            name.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetFlexible(name.gameObject, 1f, 0f);

            var tier = UiFactory.Label("GameVersionCenterVersionTier", header.transform, GameVersionUiText.Category(version, IsLatest(version), useEnglish), 14, FontStyle.Bold, layout);
            tier.alignment = TextAnchor.MiddleCenter;
            tier.color = UnityTavernUiStyle.Gold;
            UiFactory.SetWidth(tier.gameObject, CompactUnits(190f, 230f));

            BuildStatusLabel(header.transform, "GameVersionCenterOfficialStatus", GameVersionUiText.OfficialStatus(version.OfficialStatus, useEnglish));
            BuildStatusLabel(header.transform, "GameVersionCenterImplementationStatus", GameVersionUiText.ImplementationStatus(version.ImplementationStatus, useEnglish));
        }

        private void BuildStatusLabel(Transform parent, string name, string text)
        {
            var label = UiFactory.Label(name, parent, text, 14, FontStyle.Bold, layout);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetWidth(label.gameObject, CompactUnits(220f, 250f));
        }

        private void BuildTabs(Transform parent)
        {
            var tabs = UiFactory.Panel("GameVersionCenterTabs", parent, Color.clear);
            UiFactory.SetHeight(tabs, CompactUnits(54f, 58f));
            var row = UiFactory.Horizontal(tabs, 0, CompactInt(5f, 8f));
            row.childForceExpandWidth = true;
            BuildTab(tabs.transform, "GameVersionCenterTabOverview", VersionTab.Overview, T("概览", "Overview"));
            BuildTab(tabs.transform, "GameVersionCenterTabHeroes", VersionTab.Heroes, T("英雄", "Heroes"));
            BuildTab(tabs.transform, "GameVersionCenterTabCards", VersionTab.Cards, T("卡牌", "Cards"));
            BuildTab(tabs.transform, "GameVersionCenterTabMechanics", VersionTab.Mechanics, T("机制", "Mechanics"));
            BuildTab(tabs.transform, "GameVersionCenterTabCompare", VersionTab.Compare, T("与上一版对比", "Compare"));
        }

        private void BuildTab(Transform parent, string name, VersionTab tab, string text)
        {
            var active = activeTab == tab;
            var button = UiFactory.Button(name, parent, active ? "✓ " + text : text, () =>
            {
                activeTab = tab;
                Build();
            }, layout);
            UnityTavernUiStyle.ConfigureButton(button, active ? UnityTavernUiStyle.FocusRing : UnityTavernUiStyle.Brass, active, active);
            UiFactory.SetFlexible(button.gameObject, 1f, 0f);
        }

        private void BuildOverview(Transform parent, GameVersionDefinition version)
        {
            var revisions = RevisionsFor(version);
            var ruleset = RulesetFor(version);
            BuildSectionTitle(parent, T("版本概览", "Version Overview"));
            BuildParagraph(parent, "GameVersionCenterOverviewSummary", GameVersionUiText.ChangeSummary(version.Id, version.ChangeSummary, useEnglish));
            BuildParagraph(
                parent,
                "GameVersionCenterOverviewContentSummary",
                T(
                    "版本差异：英雄 " + revisions.Count(item => item.Kind == EntityKind.Hero) + " 项 · 卡牌 " + revisions.Count(IsCardRevision) + " 项 · 机制 " + (ruleset?.MechanicProfiles.Count ?? 0) + " 项",
                    "Changes: " + revisions.Count(item => item.Kind == EntityKind.Hero) + " heroes · " + revisions.Count(IsCardRevision) + " cards · " + (ruleset?.MechanicProfiles.Count ?? 0) + " mechanics"));
            var contentSet = ContentSetFor(version);
            BuildParagraph(
                parent,
                "GameVersionCenterOverviewPoolSummary",
                T(
                    "原子卡池成员：" + (contentSet?.PoolMembership.Count ?? 0) + " · 发布日期：" + version.ReleaseDateUtc.ToString("yyyy-MM-dd"),
                    "Atomic pool members: " + (contentSet?.PoolMembership.Count ?? 0) + " · Released: " + version.ReleaseDateUtc.ToString("yyyy-MM-dd")));
        }

        private void BuildRevisionList(Transform parent, GameVersionDefinition version, Func<EntityRevisionDefinition, bool> predicate, string emptyText)
        {
            var revisions = RevisionsFor(version).Where(predicate).ToArray();
            if (revisions.Length == 0)
            {
                BuildParagraph(parent, "GameVersionCenterEmptyState", emptyText);
                return;
            }

            foreach (var revision in revisions)
            {
                BuildDifferenceRow(parent, DifferenceFor(revision), revision.StableEntityId, RevisionDetail(revision));
            }
        }

        private void BuildMechanics(Transform parent, GameVersionDefinition version)
        {
            var ruleset = RulesetFor(version);
            if (ruleset == null || ruleset.MechanicProfiles.Count == 0)
            {
                BuildParagraph(parent, "GameVersionCenterMechanicsEmptyState", T("此版本没有独立机制差异。", "No mechanic changes in this version."));
                return;
            }

            foreach (var mechanic in ruleset.MechanicProfiles)
            {
                var display = mechanic.IndexOf("dark-gift", StringComparison.OrdinalIgnoreCase) >= 0
                    ? T("黑暗之赐", "Dark Gifts")
                    : mechanic;
                BuildDifferenceRow(parent, DifferenceKind.Added, display, T("版本规则机制", "Version rules mechanic"));
            }
        }

        private void BuildComparison(Transform parent, GameVersionDefinition version)
        {
            BuildSectionTitle(parent, T("与上一版对比", "Compared with Previous Version"));
            var revisions = RevisionsFor(version);
            foreach (var revision in revisions)
            {
                BuildDifferenceRow(parent, DifferenceFor(revision), revision.StableEntityId, RevisionDetail(revision));
            }

            var ruleset = RulesetFor(version);
            foreach (var mechanic in ruleset?.MechanicProfiles ?? Array.Empty<string>())
            {
                var display = mechanic.IndexOf("dark-gift", StringComparison.OrdinalIgnoreCase) >= 0
                    ? T("黑暗之赐", "Dark Gifts")
                    : mechanic;
                BuildDifferenceRow(parent, DifferenceKind.Added, display, T("版本规则机制", "Version rules mechanic"));
            }

            if (revisions.Count == 0 && (ruleset == null || ruleset.MechanicProfiles.Count == 0))
            {
                BuildParagraph(parent, "GameVersionCenterCompareEmptyState", T("这是当前综合基线，没有可列出的上一版差异。", "This is the composite baseline; no previous-version diff is listed."));
            }
        }

        private void BuildDifferenceRow(Transform parent, DifferenceKind kind, string title, string detail)
        {
            var row = UiFactory.Panel("GameVersionCenterDifference-" + title, parent, UnityTavernUiStyle.SurfaceRaised);
            UnityTavernUiStyle.ConfigureOutline(row, UnityTavernUiStyle.WithAlpha(DifferenceColor(kind), 0.68f), new Vector2(1f, -1f));
            UiFactory.Vertical(row, CompactInt(8f, 10f), CompactInt(4f));
            UiFactory.SetHeight(row, CompactUnits(78f, 82f));

            var heading = UiFactory.Label("GameVersionCenterDifferenceTitle", row.transform, DifferenceText(kind) + " · " + title, 16, FontStyle.Bold, layout);
            heading.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetHeight(heading.gameObject, 30f);
            var body = UiFactory.Label("GameVersionCenterDifferenceBody", row.transform, detail, 14, FontStyle.Normal, layout);
            body.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(body.gameObject, 28f);
        }

        private void BuildSectionTitle(Transform parent, string text)
        {
            var label = UiFactory.Label("GameVersionCenterSectionTitle", parent, text, 18, FontStyle.Bold, layout);
            label.color = UnityTavernUiStyle.Gold;
            UiFactory.SetHeight(label.gameObject, 34f);
        }

        private void BuildParagraph(Transform parent, string name, string text)
        {
            var label = UiFactory.Label(name, parent, text ?? string.Empty, 14, FontStyle.Normal, layout);
            label.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetHeight(label.gameObject, CompactUnits(58f, 64f));
        }

        private GameVersionDefinition SelectedVersion()
        {
            return versions.First(version => string.Equals(version.Id, selectedVersionId, StringComparison.OrdinalIgnoreCase));
        }

        private ContentSetDefinition ContentSetFor(GameVersionDefinition version)
        {
            return content.ContentSets.FirstOrDefault(item => string.Equals(item.Id, version.ContentSetId, StringComparison.OrdinalIgnoreCase));
        }

        private RulesetDefinition RulesetFor(GameVersionDefinition version)
        {
            return content.Rulesets.FirstOrDefault(item => string.Equals(item.Id, version.RulesetId, StringComparison.OrdinalIgnoreCase));
        }

        private IReadOnlyList<EntityRevisionDefinition> RevisionsFor(GameVersionDefinition version)
        {
            var contentSet = ContentSetFor(version);
            if (contentSet == null)
            {
                return Array.Empty<EntityRevisionDefinition>();
            }

            var selected = new HashSet<string>(contentSet.AllRevisionIds, StringComparer.OrdinalIgnoreCase);
            return content.EntityRevisions
                .Where(revision => selected.Contains(revision.RevisionId))
                .OrderBy(revision => revision.Kind)
                .ThenBy(revision => revision.StableEntityId, StringComparer.Ordinal)
                .ToArray();
        }

        private bool IsLatest(GameVersionDefinition version)
        {
            return string.Equals(version.Id, versions[0].Id, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCardRevision(EntityRevisionDefinition revision)
        {
            return revision.Kind == EntityKind.Minion ||
                   revision.Kind == EntityKind.TavernSpell ||
                   revision.Kind == EntityKind.Trinket;
        }

        private static DifferenceKind DifferenceFor(EntityRevisionDefinition revision)
        {
            if (revision.Tags.Any(tag => string.Equals(tag, "pool-action:return", StringComparison.OrdinalIgnoreCase)))
            {
                return DifferenceKind.Returned;
            }
            if (revision.Tags.Any(tag => string.Equals(tag, "pool-action:remove", StringComparison.OrdinalIgnoreCase)))
            {
                return DifferenceKind.Removed;
            }
            if (revision.Kind == EntityKind.Hero || revision.Tags.Any(tag => string.Equals(tag, "pool-action:add", StringComparison.OrdinalIgnoreCase)))
            {
                return DifferenceKind.Added;
            }

            return DifferenceKind.Changed;
        }

        private string RevisionDetail(EntityRevisionDefinition revision)
        {
            var text = useEnglish ? revision.EnglishText : revision.LocalizedText;
            if (string.IsNullOrWhiteSpace(text))
            {
                text = revision.Text;
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                text = revision.Stats;
            }

            return string.IsNullOrWhiteSpace(text)
                ? GameVersionUiText.EntityKind(revision.Kind, useEnglish)
                : GameVersionUiText.EntityKind(revision.Kind, useEnglish) + " · " + text;
        }

        private string DifferenceText(DifferenceKind kind)
        {
            switch (kind)
            {
                case DifferenceKind.Added:
                    return T("+ 新增", "+ Added");
                case DifferenceKind.Removed:
                    return T("- 移除", "- Removed");
                case DifferenceKind.Returned:
                    return T("↩ 回归", "↩ Returned");
                default:
                    return T("~ 调整", "~ Changed");
            }
        }

        private static Color DifferenceColor(DifferenceKind kind)
        {
            switch (kind)
            {
                case DifferenceKind.Added:
                    return UnityTavernUiStyle.SuccessGreen;
                case DifferenceKind.Removed:
                    return UnityTavernUiStyle.DangerRed;
                case DifferenceKind.Returned:
                    return UnityTavernUiStyle.ArcaneBlue;
                default:
                    return UnityTavernUiStyle.Gold;
            }
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

        private float CompactUnits(float compactPhysicalSize, float regularSize = -1f)
        {
            if (layout.IsCompact)
            {
                return layout.CanvasUnitsForPhysicalPixels(compactPhysicalSize);
            }

            return regularSize >= 0f ? regularSize : compactPhysicalSize;
        }

        private int CompactInt(float compactPhysicalSize, float regularSize = -1f)
        {
            return Mathf.CeilToInt(CompactUnits(compactPhysicalSize, regularSize));
        }
    }

    internal static class GameVersionUiText
    {
        public static string DisplayName(GameVersionDefinition version, bool useEnglish)
        {
            if (!useEnglish && string.Equals(version.Id, GameVersionIds.LegacyCompositeSandbox, StringComparison.OrdinalIgnoreCase))
            {
                return "综合沙盒（旧行为）";
            }

            return version.DisplayName;
        }

        public static string DisplayName(GameVersionSummaryViewModel version, bool useEnglish)
        {
            if (!useEnglish && string.Equals(version.Id, GameVersionIds.LegacyCompositeSandbox, StringComparison.OrdinalIgnoreCase))
            {
                return "综合沙盒（旧行为）";
            }

            return version.DisplayName;
        }

        public static string Category(GameVersionDefinition version, bool isLatest, bool useEnglish)
        {
            var category = Category(version.Id, version.OfficialStatus, version.IsDefaultCandidate, useEnglish);
            if (!isLatest)
            {
                return category;
            }

            return (useEnglish ? "● Latest · " : "● 最新 · ") + category;
        }

        public static string Category(GameVersionSummaryViewModel version, bool useEnglish)
        {
            return Category(version.Id, version.OfficialStatus, version.IsDefaultCandidate, useEnglish);
        }

        public static string ChangeSummary(string versionId, string summary, bool useEnglish)
        {
            if (useEnglish)
            {
                return summary ?? string.Empty;
            }
            if (string.Equals(versionId, GameVersionIds.LegacyCompositeSandbox, StringComparison.OrdinalIgnoreCase))
            {
                return "保留版本化前的综合训练器行为。";
            }
            if (string.Equals(versionId, GameVersionIds.Season14Preview, StringComparison.OrdinalIgnoreCase))
            {
                return "第 14 赛季：黑暗之赐、发动、上锁宝箱、鱼饵、英雄、卡牌与卡池变化；训练器仍为部分支持。";
            }

            return summary ?? string.Empty;
        }

        public static string OfficialStatus(GameVersionOfficialStatus status, bool useEnglish)
        {
            string value;
            switch (status)
            {
                case GameVersionOfficialStatus.Announced:
                    value = useEnglish ? "Announced" : "已公布";
                    break;
                case GameVersionOfficialStatus.Released:
                    value = useEnglish ? "Released" : "已发布";
                    break;
                case GameVersionOfficialStatus.Archived:
                    value = useEnglish ? "Archived" : "已归档";
                    break;
                default:
                    value = useEnglish ? "Unofficial" : "非官方";
                    break;
            }

            return (useEnglish ? "Official: " : "官方状态：") + value;
        }

        public static string ImplementationStatus(GameVersionImplementationStatus status, bool useEnglish)
        {
            string value;
            switch (status)
            {
                case GameVersionImplementationStatus.Verified:
                    value = useEnglish ? "Verified" : "已验证";
                    break;
                case GameVersionImplementationStatus.Complete:
                    value = useEnglish ? "Complete" : "已完成";
                    break;
                case GameVersionImplementationStatus.Partial:
                    value = useEnglish ? "Partial" : "部分支持";
                    break;
                case GameVersionImplementationStatus.ContentOnly:
                    value = useEnglish ? "Content only" : "仅内容";
                    break;
                default:
                    value = useEnglish ? "Planned" : "计划中";
                    break;
            }

            return (useEnglish ? "Trainer: " : "训练器实现：") + value;
        }

        public static string EntityKind(LearnHearthstone.Domain.Models.EntityKind kind, bool useEnglish)
        {
            switch (kind)
            {
                case LearnHearthstone.Domain.Models.EntityKind.Hero:
                    return useEnglish ? "Hero" : "英雄";
                case LearnHearthstone.Domain.Models.EntityKind.Minion:
                    return useEnglish ? "Minion" : "随从";
                case LearnHearthstone.Domain.Models.EntityKind.TavernSpell:
                    return useEnglish ? "Tavern Spell" : "酒馆法术";
                case LearnHearthstone.Domain.Models.EntityKind.Trinket:
                    return useEnglish ? "Trinket" : "饰品";
                case LearnHearthstone.Domain.Models.EntityKind.DarkGift:
                    return useEnglish ? "Dark Gift" : "黑暗之赐";
                default:
                    return useEnglish ? "Mechanic" : "机制";
            }
        }

        private static string Category(string id, GameVersionOfficialStatus status, bool isDefaultCandidate, bool useEnglish)
        {
            if (string.Equals(id, GameVersionIds.LegacyCompositeSandbox, StringComparison.OrdinalIgnoreCase))
            {
                return useEnglish ? "◇ Sandbox" : "◇ 综合沙盒";
            }
            if (status == GameVersionOfficialStatus.Archived)
            {
                return useEnglish ? "◷ Historical" : "◷ 历史";
            }
            if (status == GameVersionOfficialStatus.Announced)
            {
                return useEnglish ? "◈ Preview" : "◈ 预览";
            }
            if (isDefaultCandidate)
            {
                return useEnglish ? "✓ Stable" : "✓ 稳定";
            }

            return useEnglish ? "● Current" : "● 当前";
        }
    }
}
