using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Adapters.Persistence;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernTribeSelectionView
    {
        private const int CardPoolLoadStep = 100;
        private const float CardPoolLoadMoreThreshold = 0.02f;

        private enum CardPoolTab
        {
            Minions,
            TavernSpells,
            TimewarpedTavern
        }

        private enum AdvancedPoolTab
        {
            QuestRewards,
            Trinkets,
            Anomalies
        }

        private enum AdvancedPoolTypeFilter
        {
            All,
            Primary,
            Secondary
        }

        private enum AdvancedPoolStatusFilter
        {
            All,
            Implemented,
            Offerable
        }

        private enum SetupLanguage
        {
            Chinese,
            English
        }

        private readonly Transform root;
        private readonly Action<MatchSetupOptions> start;
        private readonly Action backToHub;
        private UnityTavernLayoutContext layout;
        private readonly ICardPoolVersionRepository repository;
        private readonly MinionCatalog minionCatalog;
        private readonly SpellCatalog spellCatalog;
        private readonly HeroCatalog heroCatalog;
        private readonly AnomalyCatalog anomalyCatalog;
        private readonly QuestCatalog questCatalog;
        private readonly TrinketCatalog trinketCatalog;
        private readonly TimewarpedTavernCatalog timewarpedTavernCatalog;
        private readonly HashSet<Tribe> selected = new HashSet<Tribe>();
        private readonly HashSet<string> enabledMinionCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> enabledTavernSpellCardNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> enabledQuestCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> enabledQuestRewardCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> enabledLesserTrinketCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> enabledGreaterTrinketCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> enabledAnomalyCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> enabledTimewarpedCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Transform cardPoolListContent;
        private CardPoolVersionStore store;
        private string selectedVersionId;
        private CardPoolTab activeTab = CardPoolTab.Minions;
        private AdvancedPoolTab activeAdvancedPoolTab = AdvancedPoolTab.QuestRewards;
        private AdvancedPoolTypeFilter advancedPoolTypeFilter = AdvancedPoolTypeFilter.All;
        private AdvancedPoolStatusFilter advancedPoolStatusFilter = AdvancedPoolStatusFilter.All;
        private string searchText = string.Empty;
        private string advancedPoolSearchText = string.Empty;
        private int versionTierFilter;
        private Tribe versionTribeFilter = Tribe.All;
        private int visibleCardPoolItemCount = CardPoolLoadStep;
        private bool keepVersionListAtBottom;
        private bool versionModalOpen;
        private bool heroSelectionOpen;
        private bool advancedMechanicsOpen;
        private bool advancedPoolEditorOpen;
        private GameObject advancedPoolEditorOverlay;
        private List<Tribe> pendingStartTribes = new List<Tribe>();
        private bool hasUnsavedCardPoolChanges;
        private bool versionSwitchConfirmOpen;
        private string pendingVersionSwitchId;
        private string selectedHeroCardId;
        private bool enableQuests;
        private bool enableTrinkets;
        private bool enableQuestRewards;
        private bool enableAnomalies;
        private AnomalyPoolVersion anomalyPoolVersion = AnomalyPoolVersion.CurrentHsReplay;
        private bool showProxySafe = true;
        private bool showDebugOnly;
        private bool showHiddenEffectOnly;
        private bool showDisabled;
        private bool enablePlayerDirectedChoices = true;
        private bool enableTimewarpedTavern = true;
        private TimewarpedPoolVersion timewarpedPoolVersion = TimewarpedPoolVersion.Current;
        private SetupLanguage setupLanguage = SetupLanguage.Chinese;
        private GameObject shell;

        public UnityTavernTribeSelectionView(
            Transform root,
            Action<List<Tribe>> start,
            Action backToHub,
            UnityTavernLayoutContext? layoutContext = null,
            bool useEnglish = false)
            : this(
                root,
                setup => start?.Invoke(setup?.ActiveTribes ?? new List<Tribe>()),
                backToHub,
                layoutContext,
                null,
                null,
                null,
                null,
                null,
                useEnglish)
        {
        }

        public UnityTavernTribeSelectionView(
            Transform root,
            Action<MatchSetupOptions> start,
            Action backToHub,
            UnityTavernLayoutContext? layoutContext = null,
            ICardPoolVersionRepository repository = null,
            MinionCatalog minionCatalog = null,
            SpellCatalog spellCatalog = null,
            HeroCatalog heroCatalog = null,
            AnomalyCatalog anomalyCatalog = null,
            bool useEnglish = false,
            QuestCatalog questCatalog = null,
            TrinketCatalog trinketCatalog = null)
        {
            this.root = root;
            this.start = start;
            this.backToHub = backToHub;
            layout = layoutContext ?? UnityTavernLayoutContext.FromRoot(root);
            this.repository = repository ?? new JsonCardPoolVersionRepository();
            this.minionCatalog = minionCatalog ?? MinionCatalogLoader.LoadFromResources();
            this.spellCatalog = spellCatalog ?? SpellCatalogLoader.LoadFromResources();
            this.heroCatalog = heroCatalog ?? HeroCatalogLoader.LoadFromResources();
            this.anomalyCatalog = anomalyCatalog ?? AnomalyCatalogLoader.LoadFromResources(useEnglish);
            this.questCatalog = questCatalog ?? QuestCatalogLoader.LoadFromResources(useEnglish);
            this.trinketCatalog = trinketCatalog ?? TrinketCatalogLoader.LoadFromResources(useEnglish);
            timewarpedTavernCatalog = TimewarpedTavernCatalogLoader.LoadFromResources();
            foreach (var card in timewarpedTavernCatalog.All.Where(card => !string.IsNullOrEmpty(card.CardId)))
            {
                enabledTimewarpedCardIds.Add(card.CardId);
            }
            setupLanguage = useEnglish ? SetupLanguage.English : SetupLanguage.Chinese;
            selectedHeroCardId = ResolveDefaultHero()?.HeroCardId;
            store = CardPoolVersionFactory.NormalizeStore(this.repository.Load());
            SelectVersion(store.SelectedVersionId, false);
        }

        private bool UseEnglish => setupLanguage == SetupLanguage.English;

        private string T(string chinese, string english)
        {
            return UseEnglish ? english : chinese;
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
            pageRect.anchorMin = new Vector2(0.04f, 0.06f);
            pageRect.anchorMax = new Vector2(0.96f, 0.94f);
            pageRect.offsetMin = Vector2.zero;
            pageRect.offsetMax = Vector2.zero;
            UnityTavernUiStyle.ConfigureOutline(page, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.52f), new Vector2(2f, -2f));
            UnityTavernUiStyle.AddStarLanternRail(page.transform, "UnitySetupStarLantern", UnityTavernUiStyle.ArcaneBlue);

            var pageLayout = page.AddComponent<VerticalLayoutGroup>();
            pageLayout.padding = new RectOffset(layout.IsCompact ? 12 : 18, layout.IsCompact ? 12 : 18, layout.IsCompact ? 12 : 18, layout.IsCompact ? 12 : 18);
            pageLayout.spacing = layout.IsCompact ? 10 : 14;
            pageLayout.childControlWidth = true;
            pageLayout.childControlHeight = true;
            pageLayout.childForceExpandWidth = true;
            pageLayout.childForceExpandHeight = false;

            var left = UiFactory.Panel("UnityTribeSelectionLeftPanel", page.transform, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetFlexible(left, 1f, 1f);
            var leftLayout = left.AddComponent<VerticalLayoutGroup>();
            leftLayout.padding = new RectOffset(layout.IsCompact ? 10 : 14, layout.IsCompact ? 10 : 14, layout.IsCompact ? 10 : 14, layout.IsCompact ? 10 : 14);
            leftLayout.spacing = layout.IsCompact ? 8 : 12;
            leftLayout.childControlWidth = true;
            leftLayout.childControlHeight = true;
            leftLayout.childForceExpandWidth = true;
            leftLayout.childForceExpandHeight = false;

            BuildHeader(left.transform);
            BuildHeroSummaryStrip(left.transform);
            BuildTribeGrid(left.transform);
            BuildVersionSummaryStrip(left.transform);
            BuildQuickActions(left.transform);
            if (heroSelectionOpen)
            {
                BuildHeroSelectionOverlay();
            }

            if (versionModalOpen)
            {
                BuildVersionEditorOverlay();
            }

            if (advancedMechanicsOpen)
            {
                BuildAdvancedMechanicsOverlay();
            }

            if (advancedPoolEditorOpen)
            {
                BuildAdvancedPoolEditorOverlay();
            }
        }

        public void RebuildForLayout(UnityTavernLayoutContext nextLayout)
        {
            layout = nextLayout;
            Build();
        }

        private void BuildHeader(Transform parent)
        {
            var header = UiFactory.Panel("UnityTribeSelectionHeader", parent, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(header, layout.IsCompact ? 108f : 126f);
            UnityTavernUiStyle.ConfigureOutline(header, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.42f), new Vector2(1f, -1f));
            UnityTavernUiStyle.AddStarLanternRail(header.transform, "UnitySetupHeaderStarLantern", UnityTavernUiStyle.ArcaneBlue);
            var headerLayout = header.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(12, 12, 8, 8);
            headerLayout.spacing = 6;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityTribeSelectionTitle", header.transform, T("选择本局种族", "Choose Tribes"), layout.IsCompact ? 20 : 26, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 30f : 40f);

            var count = selected.Count + "/5";
            var names = selected.Count == 0 ? T("尚未选择", "None selected") : string.Join(" / ", TribeAvailabilityRules.PlayableTribes.Where(selected.Contains).Select(TribeName).ToArray());
            var summary = UiFactory.Label("UnityTribeSelectionSummary", header.transform, T("已选 ", "Selected ") + count + "  " + names, layout.IsCompact ? 14 : 16, FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleCenter;
            summary.color = selected.Count == 5 ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, layout.IsCompact ? 28f : 34f);

            var excluded = TribeAvailabilityRules.PlayableTribes.Where(tribe => !selected.Contains(tribe)).Select(TribeName).ToArray();
            var exclusionText = selected.Count == 5
                ? T("本局排除：", "Excluded: ") + string.Join(" / ", excluded)
                : T("还需选择 " + (5 - selected.Count) + " 个；选满后其余种族将排除", "Choose " + (5 - selected.Count) + " more; remaining tribes will be excluded");
            var exclusion = UiFactory.Label("UnityTribeSelectionExclusionSummary", header.transform, exclusionText, 14, FontStyle.Bold);
            exclusion.alignment = TextAnchor.MiddleCenter;
            exclusion.color = selected.Count == 5 ? UnityTavernUiStyle.MutedText : UnityTavernUiStyle.Blue;
            exclusion.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetPreferredHeight(exclusion.gameObject, layout.IsCompact ? 24f : 28f);
        }

        private void BuildTribeGrid(Transform parent)
        {
            var gridObject = UiFactory.Panel("UnityTribeSelectionGrid", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetFlexible(gridObject, 1f, 1f);
            var grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.spacing = layout.IsCompact ? new Vector2(8f, 8f) : new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = layout.IsCompact ? 2 : 5;
            grid.cellSize = layout.IsCompact ? new Vector2(210f, 54f) : new Vector2(145f, 66f);

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
            var button = buttonObject.GetComponent<Button>();
            button.interactable = canSelect;
            button.onClick.AddListener(() =>
            {
                ToggleTribe(tribe);
                Build();
            });
            UnityTavernUiStyle.ConfigureButton(button, TribeAccent(tribe), isSelected, isSelected);

            var stateText = isSelected
                ? "\n" + T("已选", "Selected")
                : selected.Count == 5
                    ? "\n" + T("排除", "Excluded")
                    : string.Empty;
            var label = UiFactory.Label(buttonObject.name + "Text", buttonObject.transform, TribeName(tribe) + stateText, stateText.Length == 0 ? (layout.IsCompact ? 15 : 17) : 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private void BuildQuickActions(Transform parent)
        {
            var remainingSelections = Math.Max(0, 5 - selected.Count);
            var row = UiFactory.Panel("UnityTribeSelectionActions", parent, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 56f : 62f);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 8, 7, 7);
            rowLayout.spacing = 8;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            var back = ActionButton("UnityTribeSelectionBackButton", row.transform, T("返回", "Back"), true, backToHub);
            UnityTavernUiStyle.ConfigureButton(back, UnityTavernUiStyle.Brass);
            var random = ActionButton(
                "UnityTribeSelectionRandomButton",
                row.transform,
                selected.Count == 0 ? T("随机选择5个", "Random 5") : T("重新随机5个", "Reroll 5"),
                true,
                () =>
            {
                SelectRandomFive();
                Build();
            });
            UnityTavernUiStyle.ConfigureButton(random, UnityTavernUiStyle.ArcaneBlue);
            var all = ActionButton("UnityTribeSelectionAllButton", row.transform, T("快速配置：全部种族", "Quick Setup: All Tribes"), true, () => OpenAdvancedMechanicsPage(TribeAvailabilityRules.AllPlayableTribes()));
            UnityTavernUiStyle.ConfigureButton(all, UnityTavernUiStyle.Brass);
            var enter = ActionButton(
                "UnityTribeSelectionEnterButton",
                row.transform,
                selected.Count == 5
                    ? T("自定义下一步", "Continue Custom Setup")
                    : T("自定义：还需选择 " + remainingSelections + " 个", "Custom: choose " + remainingSelections + " more"),
                selected.Count == 5,
                () => OpenAdvancedMechanicsPage(TribeAvailabilityRules.PlayableTribes.Where(selected.Contains).ToList()));
            UnityTavernUiStyle.ConfigureButton(enter, UnityTavernUiStyle.Gold, true);
        }

        private void BuildAdvancedMechanicsOverlay()
        {
            var overlay = UiFactory.Panel("UnityAdvancedMechanicsSetupOverlay", shell.transform, UnityTavernUiStyle.WithAlpha(Color.black, 0.68f));
            overlay.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;

            var panel = UiFactory.Panel("UnityAdvancedMechanicsSetupPage", overlay.transform, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.ConfigureOutline(panel, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.56f), new Vector2(2f, -2f));
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityAdvancedMechanicsStarLantern", UnityTavernUiStyle.ArcaneBlue);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = layout.IsCompact ? new Vector2(0.05f, 0.08f) : new Vector2(0.10f, 0.12f);
            rect.anchorMax = layout.IsCompact ? new Vector2(0.95f, 0.92f) : new Vector2(0.90f, 0.88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(layout.IsCompact ? 12 : 18, layout.IsCompact ? 12 : 18, layout.IsCompact ? 12 : 16, layout.IsCompact ? 12 : 18);
            panelLayout.spacing = layout.IsCompact ? 10 : 14;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            BuildAdvancedMechanicsHeader(panel.transform);
            BuildAdvancedMechanicsStrip(panel.transform);
            BuildAdvancedMechanicsActions(panel.transform);
        }

        private void BuildAdvancedMechanicsHeader(Transform parent)
        {
            var header = UiFactory.Panel("UnityAdvancedMechanicsSetupHeader", parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(header, layout.IsCompact ? 76f : 86f);
            UnityTavernUiStyle.AddStarLanternRail(header.transform, "UnityAdvancedMechanicsHeaderStarLantern", UnityTavernUiStyle.Gold);
            var headerLayout = header.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(12, 12, 8, 8);
            headerLayout.spacing = 5;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityAdvancedMechanicsSetupPageTitle", header.transform, T("高级机制配置", "Advanced Mechanics"), layout.IsCompact ? 20 : 24, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 30f : 36f);

            var tribeText = pendingStartTribes == null || pendingStartTribes.Count == 0
                ? T("全部可用种族", "All available tribes")
                : string.Join(" / ", pendingStartTribes.Select(TribeName).ToArray());
            var summary = UiFactory.Label("UnityAdvancedMechanicsSetupPageSummary", header.transform, tribeText + "  " + AdvancedMechanicsSummaryText(), 14, FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleCenter;
            summary.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, layout.IsCompact ? 24f : 28f);
        }

        private void BuildAdvancedMechanicsActions(Transform parent)
        {
            var row = UiFactory.Panel("UnityAdvancedMechanicsSetupActions", parent, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 52f : 58f);
            ConfigureButtonRow(row, 8, 8);

            var back = ActionButton("UnityAdvancedMechanicsBackButton", row.transform, T("返回上一步", "Back"), true, () =>
            {
                advancedMechanicsOpen = false;
                Build();
            });
            UnityTavernUiStyle.ConfigureButton(back, UnityTavernUiStyle.Brass);
            var reset = ActionButton("UnityAdvancedMechanicsResetFiltersButton", row.transform, T("重置筛选", "Reset Filters"), true, () =>
            {
                ResetAdvancedFilters();
                Build();
            });
            UnityTavernUiStyle.ConfigureButton(reset, UnityTavernUiStyle.ArcaneBlue);
            var start = ActionButton("UnityAdvancedMechanicsStartButton", row.transform, T("进入酒馆", "Enter Tavern"), true, () => StartTrainer(pendingStartTribes));
            UnityTavernUiStyle.ConfigureButton(start, UnityTavernUiStyle.Gold, true);
        }

        private void ResetAdvancedFilters()
        {
            showProxySafe = true;
            showDebugOnly = false;
            showHiddenEffectOnly = false;
            showDisabled = false;
            enablePlayerDirectedChoices = true;
        }

        private void OpenAdvancedMechanicsPage(List<Tribe> activeTribes)
        {
            pendingStartTribes = activeTribes == null ? new List<Tribe>() : activeTribes.ToList();
            heroSelectionOpen = false;
            versionModalOpen = false;
            advancedPoolEditorOpen = false;
            advancedMechanicsOpen = true;
            Build();
        }

        private void BuildAdvancedMechanicsStrip(Transform parent)
        {
            var strip = UiFactory.Panel("UnityAdvancedMechanicsSetupPanel", parent, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(strip, layout.IsCompact ? 500f : 330f);
            UnityTavernUiStyle.SetFlexible(strip, 1f, 1f);
            UnityTavernUiStyle.ConfigureOutline(strip, new Color(UnityTavernUiStyle.Blue.r, UnityTavernUiStyle.Blue.g, UnityTavernUiStyle.Blue.b, 0.28f), new Vector2(1f, -1f));
            var stripLayout = strip.AddComponent<HorizontalLayoutGroup>();
            stripLayout.padding = new RectOffset(10, 10, 8, 8);
            stripLayout.spacing = 10;
            stripLayout.childControlWidth = true;
            stripLayout.childControlHeight = true;
            stripLayout.childForceExpandWidth = false;
            stripLayout.childForceExpandHeight = true;

            var titleBlock = UiFactory.Panel("UnityAdvancedMechanicsSetupTitleBlock", strip.transform, Color.clear);
            UnityTavernUiStyle.SetFixedSize(titleBlock, layout.IsCompact ? 96f : 130f, layout.IsCompact ? 148f : 96f);
            var titleLayout = titleBlock.AddComponent<VerticalLayoutGroup>();
            titleLayout.spacing = 2;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = true;
            titleLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityAdvancedMechanicsSetupTitle", titleBlock.transform, T("高级机制", "Mechanics"), layout.IsCompact ? 15 : 17, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 24f : 28f);

            var summary = UiFactory.Label("UnityAdvancedMechanicsSetupSummary", titleBlock.transform, AdvancedMechanicsSummaryText(), 14, FontStyle.Bold);
            summary.color = UnityTavernUiStyle.MutedText;
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetFlexible(summary.gameObject, 1f, 0f);

            var gridObject = UiFactory.Panel("UnityAdvancedMechanicsSetupGrid", strip.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(gridObject, 1f, 0f);
            var grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.spacing = layout.IsCompact ? new Vector2(7f, 7f) : new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = layout.IsCompact ? 2 : 4;
            grid.cellSize = layout.IsCompact ? new Vector2(150f, 112f) : new Vector2(176f, 112f);

            BuildAdvancedPoolSummaryCard(gridObject.transform, "UnityAdvancedQuestRewardPoolCard", T("任务/奖励池", "Quest / Reward Pool"), QuestRewardPoolSummaryText(), QuestPoolsEnabled(), () => QuickEnableAdvancedPool(AdvancedPoolTab.QuestRewards), () => OpenAdvancedPoolEditor(AdvancedPoolTab.QuestRewards));
            BuildAdvancedPoolSummaryCard(gridObject.transform, "UnityAdvancedTrinketPoolCard", T("饰品池", "Trinket Pool"), TrinketPoolSummaryText(), TrinketPoolsEnabled(), () => QuickEnableAdvancedPool(AdvancedPoolTab.Trinkets), () => OpenAdvancedPoolEditor(AdvancedPoolTab.Trinkets));
            BuildAdvancedPoolSummaryCard(gridObject.transform, "UnityAdvancedAnomalyPoolCard", T("畸变池", "Anomaly Pool"), AnomalyPoolSummaryText(), AnomalyPoolEnabled(), () => QuickEnableAdvancedPool(AdvancedPoolTab.Anomalies), () => OpenAdvancedPoolEditor(AdvancedPoolTab.Anomalies));
            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-ShowProxySafe", T("代理实现", "Proxy-safe"), showProxySafe, true, value =>
            {
                showProxySafe = value;
                Build();
            });
            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-ShowDebugOnly", T("调试池", "Debug Pool"), showDebugOnly, true, value =>
            {
                showDebugOnly = value;
                if (!showDebugOnly)
                {
                    showDisabled = false;
                }

                Build();
            });
            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-ShowHiddenEffectOnly", T("隐藏效果池", "Hidden Effects"), showHiddenEffectOnly, true, value =>
            {
                showHiddenEffectOnly = value;
                Build();
            });
            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-ShowDisabled", T("禁用池", "Disabled Pool"), showDisabled, showDebugOnly, value =>
            {
                showDisabled = showDebugOnly && value;
                Build();
            });

            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-EnablePlayerDirectedChoices", T("自由选择", "Free Choice"), enablePlayerDirectedChoices, true, value =>
            {
                enablePlayerDirectedChoices = value;
                Build();
            });
        }

        private void BuildAdvancedPoolSummaryCard(Transform parent, string name, string titleText, string detailText, bool active, Action quickEnable, Action edit)
        {
            var card = UiFactory.Panel(name, parent, active ? Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.12f) : UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.ConfigureOutline(
                card,
                active
                    ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.42f)
                    : new Color(1f, 1f, 1f, 0.08f),
                new Vector2(1f, -1f));
            var layoutGroup = card.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(6, 6, 5, 6);
            layoutGroup.spacing = 3;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            var title = UiFactory.Label(name + "Title", card.transform, titleText, layout.IsCompact ? 10 : 12, FontStyle.Bold);
            title.color = active ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Text;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 18f : 20f);

            var detail = UiFactory.Label(name + "Summary", card.transform, detailText, layout.IsCompact ? 9 : 10, FontStyle.Bold);
            detail.color = UnityTavernUiStyle.MutedText;
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetFlexible(detail.gameObject, 1f, 0f);

            var actions = UiFactory.Panel(name + "Actions", card.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(actions, UnityTavernUiStyle.TouchHeight);
            ConfigureButtonRow(actions, 0, 4);
            ActionButton(name + "EnableButton", actions.transform, active ? T("已开启", "On") : T("一键开启", "Enable"), !active, quickEnable);
            ActionButton(name + "EditButton", actions.transform, T("编辑", "Edit"), true, edit);
        }

        private void QuickEnableAdvancedPool(AdvancedPoolTab tab)
        {
            activeAdvancedPoolTab = tab;
            advancedPoolSearchText = string.Empty;
            advancedPoolTypeFilter = AdvancedPoolTypeFilter.All;
            advancedPoolStatusFilter = AdvancedPoolStatusFilter.All;
            SelectAdvancedOfferableOnly();
        }

        private void OpenAdvancedPoolEditor(AdvancedPoolTab tab)
        {
            activeAdvancedPoolTab = tab;
            advancedPoolTypeFilter = AdvancedPoolTypeFilter.All;
            advancedPoolStatusFilter = AdvancedPoolStatusFilter.All;
            advancedPoolSearchText = string.Empty;
            heroSelectionOpen = false;
            versionModalOpen = false;
            advancedPoolEditorOpen = true;
            Build();
        }

        private void BuildAdvancedPoolEditorOverlay()
        {
            var overlay = UiFactory.Panel("UnityAdvancedPoolEditorOverlay", shell.transform, UnityTavernUiStyle.WithAlpha(Color.black, 0.68f));
            advancedPoolEditorOverlay = overlay;
            overlay.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;

            var panel = UiFactory.Panel("UnityAdvancedPoolEditorPanel", overlay.transform, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.ConfigureOutline(panel, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.56f), new Vector2(2f, -2f));
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityAdvancedPoolStarLantern", UnityTavernUiStyle.ArcaneBlue);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.045f, 0.06f);
            rect.anchorMax = new Vector2(0.955f, 0.93f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(14, 14, 12, 14);
            panelLayout.spacing = 9;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            BuildAdvancedPoolHeader(panel.transform);
            BuildAdvancedPoolSearch(panel.transform);
            BuildAdvancedPoolFilters(panel.transform);
            BuildAdvancedPoolBulkActions(panel.transform);
            BuildAdvancedPoolList(panel.transform);
        }

        private void RebuildAdvancedPoolEditorOverlay()
        {
            if (!advancedPoolEditorOpen || shell == null)
            {
                Build();
                return;
            }

            if (advancedPoolEditorOverlay != null)
            {
                advancedPoolEditorOverlay.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(advancedPoolEditorOverlay);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(advancedPoolEditorOverlay);
                }

                advancedPoolEditorOverlay = null;
            }

            BuildAdvancedPoolEditorOverlay();
        }

        private void BuildAdvancedPoolHeader(Transform parent)
        {
            var header = UiFactory.Panel("UnityAdvancedPoolEditorHeader", parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(header, layout.IsCompact ? 58f : 64f);
            UnityTavernUiStyle.AddStarLanternRail(header.transform, "UnityAdvancedPoolHeaderStarLantern", UnityTavernUiStyle.Gold);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 10, 8, 8);
            headerLayout.spacing = 8;
            headerLayout.childControlWidth = false;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            BuildAdvancedPoolTabButton(header.transform, AdvancedPoolTab.QuestRewards, T("任务/奖励", "Quest / Reward"));
            BuildAdvancedPoolTabButton(header.transform, AdvancedPoolTab.Trinkets, T("饰品", "Trinkets"));
            BuildAdvancedPoolTabButton(header.transform, AdvancedPoolTab.Anomalies, T("畸变", "Anomalies"));

            var title = UiFactory.Label("UnityAdvancedPoolEditorTitle", header.transform, ActiveAdvancedPoolTitle(), layout.IsCompact ? 18 : 22, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

            var summary = UiFactory.Label("UnityAdvancedPoolEditorSummary", header.transform, ActiveAdvancedPoolSummaryText(), 14, FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleRight;
            summary.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(summary.gameObject, layout.IsCompact ? 190f : 260f, 34f);

            var close = ActionButton("UnityAdvancedPoolEditorCloseButton", header.transform, T("关闭", "Close"), true, () =>
            {
                advancedPoolEditorOpen = false;
                Build();
            });
            UnityTavernUiStyle.SetFixedSize(close.gameObject, 72f, UnityTavernUiStyle.TouchHeight);
        }

        private void BuildAdvancedPoolTabButton(Transform parent, AdvancedPoolTab tab, string label)
        {
            var button = ActionButton("UnityAdvancedPoolTab-" + tab, parent, label, true, () =>
            {
                activeAdvancedPoolTab = tab;
                advancedPoolTypeFilter = AdvancedPoolTypeFilter.All;
                advancedPoolStatusFilter = AdvancedPoolStatusFilter.All;
                RebuildAdvancedPoolEditorOverlay();
            });
            UnityTavernUiStyle.SetFixedSize(button.gameObject, layout.IsCompact ? 92f : 112f, UnityTavernUiStyle.TouchHeight);
            UnityTavernUiStyle.ConfigureButton(button, UnityTavernUiStyle.Gold, activeAdvancedPoolTab == tab, activeAdvancedPoolTab == tab);
        }

        private void BuildAdvancedPoolSearch(Transform parent)
        {
            var inputObject = new GameObject("UnityAdvancedPoolSearchInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetPreferredHeight(inputObject, 40f);
            UnityTavernUiStyle.ConfigureSurface(inputObject, UnityTavernUiStyle.PanelQuiet, true);
            UnityTavernUiStyle.ConfigureOutline(inputObject, new Color(1f, 1f, 1f, 0.12f), new Vector2(1f, -1f));

            var input = inputObject.GetComponent<InputField>();
            input.caretColor = UnityTavernUiStyle.Text;
            input.selectionColor = new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.35f);
            input.textComponent = UiFactory.Label("UnityAdvancedPoolSearchText", inputObject.transform, string.Empty, 14, FontStyle.Bold);
            UnityTavernUiStyle.Stretch(input.textComponent.rectTransform);
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            input.textComponent.color = UnityTavernUiStyle.Text;
            input.textComponent.rectTransform.offsetMin = new Vector2(12f, 0f);
            input.textComponent.rectTransform.offsetMax = new Vector2(-12f, 0f);
            input.placeholder = UiFactory.Label("UnityAdvancedPoolSearchPlaceholder", inputObject.transform, T("搜索名称、CardId、文本、标签", "Search name, CardId, text, tags"), 14);
            UnityTavernUiStyle.Stretch(input.placeholder.rectTransform);
            input.placeholder.color = UnityTavernUiStyle.MutedText;
            input.placeholder.rectTransform.offsetMin = new Vector2(12f, 0f);
            input.placeholder.rectTransform.offsetMax = new Vector2(-12f, 0f);
            input.text = advancedPoolSearchText;
            input.onEndEdit.AddListener(value =>
            {
                advancedPoolSearchText = value ?? string.Empty;
                RebuildAdvancedPoolEditorOverlay();
            });
        }

        private void BuildAdvancedPoolFilters(Transform parent)
        {
            var row = UiFactory.Panel("UnityAdvancedPoolFilters", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 102f : UnityTavernUiStyle.TouchHeight);
            var grid = row.AddComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(6f, 6f);
            grid.cellSize = new Vector2(layout.IsCompact ? 112f : 128f, UnityTavernUiStyle.TouchHeight);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = layout.IsCompact ? 3 : 6;

            BuildAdvancedPoolTypeFilter(row.transform, AdvancedPoolTypeFilter.All, T("全部", "All"));
            BuildAdvancedPoolTypeFilter(row.transform, AdvancedPoolTypeFilter.Primary, AdvancedPrimaryFilterText());
            BuildAdvancedPoolTypeFilter(row.transform, AdvancedPoolTypeFilter.Secondary, AdvancedSecondaryFilterText());
            BuildAdvancedPoolStatusFilter(row.transform, AdvancedPoolStatusFilter.All, T("全部状态", "All Status"));
            BuildAdvancedPoolStatusFilter(row.transform, AdvancedPoolStatusFilter.Implemented, T("已实现", "Implemented"));
            BuildAdvancedPoolStatusFilter(row.transform, AdvancedPoolStatusFilter.Offerable, T("可提供", "Offerable"));
        }

        private void BuildAdvancedPoolTypeFilter(Transform parent, AdvancedPoolTypeFilter filter, string label)
        {
            var button = FilterButton("UnityAdvancedPoolTypeFilter-" + filter, parent, label, advancedPoolTypeFilter == filter, UnityTavernUiStyle.Blue, () =>
            {
                advancedPoolTypeFilter = filter;
                RebuildAdvancedPoolEditorOverlay();
            });
            UnityTavernUiStyle.SetPreferredHeight(button.gameObject, 30f);
        }

        private void BuildAdvancedPoolStatusFilter(Transform parent, AdvancedPoolStatusFilter filter, string label)
        {
            var button = FilterButton("UnityAdvancedPoolStatusFilter-" + filter, parent, label, advancedPoolStatusFilter == filter, UnityTavernUiStyle.Green, () =>
            {
                advancedPoolStatusFilter = filter;
                RebuildAdvancedPoolEditorOverlay();
            });
            UnityTavernUiStyle.SetPreferredHeight(button.gameObject, 30f);
        }

        private void BuildAdvancedPoolBulkActions(Transform parent)
        {
            var row = UiFactory.Panel("UnityAdvancedPoolBulkActions", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 102f : UnityTavernUiStyle.TouchHeight);
            if (layout.IsCompact)
            {
                var grid = row.AddComponent<GridLayoutGroup>();
                grid.spacing = new Vector2(6f, 6f);
                grid.cellSize = new Vector2(116f, UnityTavernUiStyle.TouchHeight);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
            }
            else
            {
                ConfigureButtonRow(row, 4, 8);
            }

            ActionButton("UnityAdvancedPoolIncludeFilteredButton", row.transform, T("全选当前", "Select Filtered"), true, () => SetAdvancedFilteredEnabled(true));
            ActionButton("UnityAdvancedPoolExcludeFilteredButton", row.transform, T("清空当前", "Clear Filtered"), true, () => SetAdvancedFilteredEnabled(false));
            ActionButton("UnityAdvancedPoolImplementedOnlyButton", row.transform, T("只选已实现", "Implemented Only"), true, SelectAdvancedImplementedOnly);
            ActionButton("UnityAdvancedPoolOfferableOnlyButton", row.transform, T("只选可提供", "Offerable Only"), true, SelectAdvancedOfferableOnly);
            ActionButton("UnityAdvancedPoolInvertButton", row.transform, T("反选当前", "Invert Filtered"), true, InvertAdvancedFiltered);
            ActionButton("UnityAdvancedPoolResetFiltersButton", row.transform, T("重置筛选", "Reset Filters"), true, () =>
            {
                advancedPoolSearchText = string.Empty;
                advancedPoolTypeFilter = AdvancedPoolTypeFilter.All;
                advancedPoolStatusFilter = AdvancedPoolStatusFilter.All;
                RebuildAdvancedPoolEditorOverlay();
            });
        }

        private void BuildAdvancedPoolList(Transform parent)
        {
            var content = UiFactory.ScrollView("UnityAdvancedPoolScroll", parent, UnityTavernUiStyle.PanelQuiet, out _);
            var listLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(6, 10, 6, 6);
            listLayout.spacing = 5;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            var count = 0;
            if (activeAdvancedPoolTab == AdvancedPoolTab.QuestRewards)
            {
                if (advancedPoolTypeFilter != AdvancedPoolTypeFilter.Secondary)
                {
                    foreach (var quest in FilteredAdvancedQuests())
                    {
                        count += 1;
                        BuildPoolToggleRow(
                            content,
                            "UnityAdvancedPoolQuestToggle-" + SafeCardName(quest.CardId),
                            CardImageProvider.LoadSprite(quest.ImagePath, quest.CardId, CardKind.Quest),
                            T("任务  ", "Quest  ") + quest.Name,
                            quest.Objective.Kind + "  " + quest.CardId,
                            enabledQuestCardIds.Contains(quest.CardId),
                            true,
                            value => SetAdvancedPoolEnabled(enabledQuestCardIds, quest.CardId, value));
                    }
                }

                if (advancedPoolTypeFilter != AdvancedPoolTypeFilter.Primary)
                {
                    foreach (var reward in FilteredAdvancedQuestRewards())
                    {
                        count += 1;
                        BuildPoolToggleRow(
                            content,
                            "UnityAdvancedPoolRewardToggle-" + SafeCardName(reward.CardId),
                            CardImageProvider.LoadSprite(reward.ImagePath, reward.CardId, CardKind.QuestReward),
                            T("奖励  ", "Reward  ") + reward.Name,
                            reward.Trigger + " / " + reward.OfferPoolStatus + "  " + reward.CardId,
                            enabledQuestRewardCardIds.Contains(reward.CardId),
                            true,
                            value => SetAdvancedPoolEnabled(enabledQuestRewardCardIds, reward.CardId, value));
                    }
                }
            }
            else if (activeAdvancedPoolTab == AdvancedPoolTab.Trinkets)
            {
                foreach (var trinket in FilteredAdvancedTrinkets())
                {
                    var target = trinket.SlotKind == TrinketSlotKind.Greater ? enabledGreaterTrinketCardIds : enabledLesserTrinketCardIds;
                    count += 1;
                    BuildPoolToggleRow(
                        content,
                        "UnityAdvancedPoolTrinketToggle-" + SafeCardName(trinket.CardId),
                        CardImageProvider.LoadSprite(trinket.ImagePath, trinket.CardId, CardKind.Trinket),
                            T(trinket.SlotKind == TrinketSlotKind.Lesser ? "小型" : "大型", trinket.SlotKind.ToString()) + "  " + trinket.Name,
                        trinket.TriggerTemplate + " / " + trinket.OfferPoolStatus + "  " + trinket.CardId,
                        target.Contains(trinket.CardId),
                        true,
                        value => SetAdvancedPoolEnabled(target, trinket.CardId, value));
                }
            }
            else
            {
                foreach (var anomaly in FilteredAdvancedAnomalies())
                {
                    count += 1;
                    BuildPoolToggleRow(
                        content,
                        "UnityAdvancedPoolAnomalyToggle-" + SafeCardName(anomaly.CardId),
                        CardImageProvider.LoadSprite(null, anomaly.CardId, CardKind.Spell),
                        anomaly.Name,
                        anomaly.EffectFamily + " / " + anomaly.ImplementationStatus + "  " + anomaly.CardId,
                        enabledAnomalyCardIds.Contains(anomaly.CardId),
                        true,
                        value => SetAdvancedPoolEnabled(enabledAnomalyCardIds, anomaly.CardId, value));
                }
            }

            var state = UiFactory.Label("UnityAdvancedPoolLoadState", content, count == 0 ? T("当前筛选无条目", "No entries match the current filters.") : T("已显示 ", "Showing ") + count, 14, FontStyle.Bold);
            state.alignment = TextAnchor.MiddleCenter;
            state.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(state.gameObject, 24f);
        }

        private IEnumerable<QuestDefinition> FilteredAdvancedQuests()
        {
            var query = (advancedPoolSearchText ?? string.Empty).Trim();
            return (questCatalog?.Quests ?? new List<QuestDefinition>())
                .Where(quest => MatchesAdvancedQuery(quest.Name, quest.Text, quest.CardId, quest.Id, quest.Tags))
                .Where(quest => advancedPoolStatusFilter == AdvancedPoolStatusFilter.All ||
                    quest.ImplementationStatus == QuestImplementationStatus.Implemented)
                .OrderBy(quest => quest.Name)
                .ThenBy(quest => quest.CardId);
        }

        private IEnumerable<QuestRewardDefinition> FilteredAdvancedQuestRewards()
        {
            return (questCatalog?.Rewards ?? new List<QuestRewardDefinition>())
                .Where(reward => MatchesAdvancedQuery(reward.Name, reward.Text, reward.CardId, reward.Id, reward.Tags))
                .Where(reward =>
                    advancedPoolStatusFilter == AdvancedPoolStatusFilter.All ||
                    (reward.ImplementationStatus == QuestImplementationStatus.Implemented &&
                     (advancedPoolStatusFilter != AdvancedPoolStatusFilter.Offerable ||
                      reward.OfferPoolStatus == QuestOfferPoolStatus.Offerable)))
                .OrderBy(reward => reward.Name)
                .ThenBy(reward => reward.CardId);
        }

        private IEnumerable<TrinketDefinition> FilteredAdvancedTrinkets()
        {
            return (trinketCatalog?.All ?? new List<TrinketDefinition>())
                .Where(trinket =>
                    advancedPoolTypeFilter == AdvancedPoolTypeFilter.All ||
                    (advancedPoolTypeFilter == AdvancedPoolTypeFilter.Primary && trinket.SlotKind == TrinketSlotKind.Lesser) ||
                    (advancedPoolTypeFilter == AdvancedPoolTypeFilter.Secondary && trinket.SlotKind == TrinketSlotKind.Greater))
                .Where(trinket => MatchesAdvancedQuery(
                    trinket.Name,
                    trinket.Text,
                    trinket.CardId,
                    trinket.Id,
                    trinket.Tags,
                    trinket.Mechanics,
                    trinket.AssociatedRaces,
                    trinket.ReferencedTags))
                .Where(trinket =>
                    advancedPoolStatusFilter == AdvancedPoolStatusFilter.All ||
                    (trinket.ImplementationStatus == TrinketImplementationStatus.Implemented &&
                     (advancedPoolStatusFilter != AdvancedPoolStatusFilter.Offerable ||
                      trinket.OfferPoolStatus == TrinketOfferPoolStatus.Offerable)))
                .OrderBy(trinket => trinket.SlotKind)
                .ThenBy(trinket => trinket.Name)
                .ThenBy(trinket => trinket.CardId);
        }

        private IEnumerable<AnomalyDefinition> FilteredAdvancedAnomalies()
        {
            var anomalies = anomalyCatalog == null
                ? new List<AnomalyDefinition>()
                : advancedPoolTypeFilter == AdvancedPoolTypeFilter.Secondary
                    ? anomalyCatalog.All
                    : anomalyCatalog.GetByPool(anomalyPoolVersion);
            return anomalies
                .Where(anomaly => MatchesAdvancedQuery(anomaly.Name, anomaly.Text, anomaly.CardId, anomaly.Id, anomaly.Tags))
                .Where(anomaly =>
                    advancedPoolStatusFilter == AdvancedPoolStatusFilter.All ||
                    ((anomaly.ImplementationStatus == AnomalyImplementationStatus.Implemented ||
                      anomaly.ImplementationStatus == AnomalyImplementationStatus.OfferableWithExactProxy) &&
                     (advancedPoolStatusFilter != AdvancedPoolStatusFilter.Offerable ||
                      IsAnomalySelectable(anomaly))))
                .OrderBy(anomaly => anomaly.Name)
                .ThenBy(anomaly => anomaly.CardId);
        }

        private bool MatchesAdvancedQuery(params object[] values)
        {
            var query = (advancedPoolSearchText ?? string.Empty).Trim();
            if (query.Length == 0)
            {
                return true;
            }

            foreach (var value in values)
            {
                if (value == null)
                {
                    continue;
                }

                if (value is IEnumerable<string> strings)
                {
                    if (strings.Any(item => !string.IsNullOrEmpty(item) && item.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return true;
                    }

                    continue;
                }

                if (value.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetAdvancedFilteredEnabled(bool enabled)
        {
            var changed = false;
            if (activeAdvancedPoolTab == AdvancedPoolTab.QuestRewards)
            {
                if (advancedPoolTypeFilter != AdvancedPoolTypeFilter.Secondary)
                {
                    foreach (var quest in FilteredAdvancedQuests())
                    {
                        changed |= SetEnabled(enabledQuestCardIds, quest.CardId, enabled);
                    }
                }

                if (advancedPoolTypeFilter != AdvancedPoolTypeFilter.Primary)
                {
                    foreach (var reward in FilteredAdvancedQuestRewards())
                    {
                        changed |= SetEnabled(enabledQuestRewardCardIds, reward.CardId, enabled);
                    }
                }
            }
            else if (activeAdvancedPoolTab == AdvancedPoolTab.Trinkets)
            {
                foreach (var trinket in FilteredAdvancedTrinkets())
                {
                    changed |= SetEnabled(
                        trinket.SlotKind == TrinketSlotKind.Greater ? enabledGreaterTrinketCardIds : enabledLesserTrinketCardIds,
                        trinket.CardId,
                        enabled);
                }
            }
            else
            {
                foreach (var anomaly in FilteredAdvancedAnomalies())
                {
                    changed |= SetEnabled(enabledAnomalyCardIds, anomaly.CardId, enabled);
                }
            }

            if (changed)
            {
                MarkCardPoolDirty();
                SyncAdvancedMechanicFlagsFromPools();
            }

            RebuildAdvancedPoolEditorOverlay();
        }

        private void SelectAdvancedImplementedOnly()
        {
            ClearActiveAdvancedPool();
            advancedPoolStatusFilter = AdvancedPoolStatusFilter.Implemented;
            SetAdvancedFilteredEnabled(true);
        }

        private void SelectAdvancedOfferableOnly()
        {
            ClearActiveAdvancedPool();
            advancedPoolStatusFilter = AdvancedPoolStatusFilter.Offerable;
            SetAdvancedFilteredEnabled(true);
        }

        private void InvertAdvancedFiltered()
        {
            if (activeAdvancedPoolTab == AdvancedPoolTab.QuestRewards)
            {
                foreach (var quest in FilteredAdvancedQuests())
                {
                    SetEnabled(enabledQuestCardIds, quest.CardId, !enabledQuestCardIds.Contains(quest.CardId));
                }

                foreach (var reward in FilteredAdvancedQuestRewards())
                {
                    SetEnabled(enabledQuestRewardCardIds, reward.CardId, !enabledQuestRewardCardIds.Contains(reward.CardId));
                }
            }
            else if (activeAdvancedPoolTab == AdvancedPoolTab.Trinkets)
            {
                foreach (var trinket in FilteredAdvancedTrinkets())
                {
                    var target = trinket.SlotKind == TrinketSlotKind.Greater ? enabledGreaterTrinketCardIds : enabledLesserTrinketCardIds;
                    SetEnabled(target, trinket.CardId, !target.Contains(trinket.CardId));
                }
            }
            else
            {
                foreach (var anomaly in FilteredAdvancedAnomalies())
                {
                    SetEnabled(enabledAnomalyCardIds, anomaly.CardId, !enabledAnomalyCardIds.Contains(anomaly.CardId));
                }
            }

            MarkCardPoolDirty();
            SyncAdvancedMechanicFlagsFromPools();
            RebuildAdvancedPoolEditorOverlay();
        }

        private void ClearActiveAdvancedPool()
        {
            if (activeAdvancedPoolTab == AdvancedPoolTab.QuestRewards)
            {
                enabledQuestCardIds.Clear();
                enabledQuestRewardCardIds.Clear();
            }
            else if (activeAdvancedPoolTab == AdvancedPoolTab.Trinkets)
            {
                enabledLesserTrinketCardIds.Clear();
                enabledGreaterTrinketCardIds.Clear();
            }
            else
            {
                enabledAnomalyCardIds.Clear();
            }

            MarkCardPoolDirty();
            SyncAdvancedMechanicFlagsFromPools();
        }

        private void SetAdvancedPoolEnabled(HashSet<string> pool, string cardId, bool enabled)
        {
            if (SetEnabled(pool, cardId, enabled))
            {
                MarkCardPoolDirty();
                SyncAdvancedMechanicFlagsFromPools();
                RebuildAdvancedPoolEditorOverlay();
            }
        }

        private void SyncAdvancedMechanicFlagsFromPools()
        {
            enableQuests = QuestPoolsEnabled();
            enableQuestRewards = enableQuests;
            enableTrinkets = TrinketPoolsEnabled();
            enableAnomalies = AnomalyPoolEnabled();
        }

        private void RemoveAnomaliesOutsideActivePool()
        {
            if (anomalyCatalog == null || enabledAnomalyCardIds.Count == 0)
            {
                SyncAdvancedMechanicFlagsFromPools();
                return;
            }

            var allowed = new HashSet<string>(
                anomalyCatalog.GetByPool(anomalyPoolVersion)
                    .Where(IsAnomalySelectable)
                    .Select(anomaly => anomaly.CardId),
                StringComparer.OrdinalIgnoreCase);
            enabledAnomalyCardIds.RemoveWhere(cardId => !allowed.Contains(cardId));
            SyncAdvancedMechanicFlagsFromPools();
        }

        private bool QuestPoolsEnabled()
        {
            return enabledQuestCardIds.Count > 0 && enabledQuestRewardCardIds.Count > 0;
        }

        private bool TrinketPoolsEnabled()
        {
            return enabledLesserTrinketCardIds.Count > 0 || enabledGreaterTrinketCardIds.Count > 0;
        }

        private bool AnomalyPoolEnabled()
        {
            return enabledAnomalyCardIds.Count > 0;
        }

        private string ActiveAdvancedPoolTitle()
        {
            switch (activeAdvancedPoolTab)
            {
                case AdvancedPoolTab.Trinkets:
                    return T("饰品池", "Trinket Pool");
                case AdvancedPoolTab.Anomalies:
                    return T("畸变池", "Anomaly Pool");
                default:
                    return T("任务/奖励池", "Quest / Reward Pool");
            }
        }

        private string ActiveAdvancedPoolSummaryText()
        {
            switch (activeAdvancedPoolTab)
            {
                case AdvancedPoolTab.Trinkets:
                    return TrinketPoolSummaryText();
                case AdvancedPoolTab.Anomalies:
                    return AnomalyPoolSummaryText();
                default:
                    return QuestRewardPoolSummaryText();
            }
        }

        private string AdvancedPrimaryFilterText()
        {
            switch (activeAdvancedPoolTab)
            {
                case AdvancedPoolTab.Trinkets:
                    return T("小饰品", "Lesser");
                case AdvancedPoolTab.Anomalies:
                    return T("当前池", "Current Pool");
                default:
                    return T("任务", "Quests");
            }
        }

        private string AdvancedSecondaryFilterText()
        {
            switch (activeAdvancedPoolTab)
            {
                case AdvancedPoolTab.Trinkets:
                    return T("大饰品", "Greater");
                case AdvancedPoolTab.Anomalies:
                    return T("全部畸变", "All Anomalies");
                default:
                    return T("奖励", "Rewards");
            }
        }

        private string QuestRewardPoolSummaryText()
        {
            var questTotal = questCatalog?.Quests.Count ?? 0;
            var rewardTotal = questCatalog?.Rewards.Count ?? 0;
            return T("任务 ", "Quests ") + enabledQuestCardIds.Count + "/" + questTotal +
                T("  奖励 ", "  Rewards ") + enabledQuestRewardCardIds.Count + "/" + rewardTotal +
                "  " + (QuestPoolsEnabled() ? T("启用", "On") : T("关闭", "Off"));
        }

        private string TrinketPoolSummaryText()
        {
            var lesserTotal = trinketCatalog?.Lesser.Count ?? 0;
            var greaterTotal = trinketCatalog?.Greater.Count ?? 0;
            return T("小 ", "Lesser ") + enabledLesserTrinketCardIds.Count + "/" + lesserTotal +
                T("  大 ", "  Greater ") + enabledGreaterTrinketCardIds.Count + "/" + greaterTotal +
                "  " + (TrinketPoolsEnabled() ? T("启用", "On") : T("关闭", "Off"));
        }

        private string AnomalyPoolSummaryText()
        {
            var total = anomalyCatalog == null ? 0 : anomalyCatalog.GetByPool(anomalyPoolVersion).Count;
            var mode = enabledAnomalyCardIds.Count == 0
                ? T("关闭", "Off")
                : enabledAnomalyCardIds.Count == 1
                    ? T("固定", "Fixed")
                    : T("随机", "Random");
            return T("畸变 ", "Anomalies ") + enabledAnomalyCardIds.Count + "/" + total + "  " + mode;
        }

        private void BuildVersionSummaryStrip(Transform parent)
        {
            var selection = CurrentSelection();
            var strip = UiFactory.Panel("UnityCardPoolVersionPanel", parent, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(strip, layout.IsCompact ? 58f : 64f);
            var stripLayout = strip.AddComponent<HorizontalLayoutGroup>();
            stripLayout.padding = new RectOffset(10, 10, 8, 8);
            stripLayout.spacing = 10;
            stripLayout.childControlWidth = true;
            stripLayout.childControlHeight = true;
            stripLayout.childForceExpandWidth = false;
            stripLayout.childForceExpandHeight = true;

            var titleBlock = UiFactory.Panel("UnityCardPoolVersionSummaryBlock", strip.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(titleBlock, 1f, 0f);
            var titleLayout = titleBlock.AddComponent<VerticalLayoutGroup>();
            titleLayout.spacing = 2;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = true;
            titleLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityCardPoolVersionTitle", titleBlock.transform, T("卡池版本", "Card Pool"), layout.IsCompact ? 16 : 18, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 24f : 28f);

            var summary = UiFactory.Label("UnityCardPoolVersionSummary", titleBlock.transform, VersionSummaryText(selection), layout.IsCompact ? 12 : 13, FontStyle.Bold);
            summary.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, layout.IsCompact ? 20f : 22f);

            if (enableTimewarpedTavern)
            {
                var timewarped = ActionButton("UnityTimewarpedPoolVersionButton", strip.transform, TimewarpedPoolVersionButtonText(), true, () =>
                {
                    AdvanceTimewarpedPoolVersion();
                    Build();
                });
                UnityTavernUiStyle.SetFixedSize(timewarped.gameObject, layout.IsCompact ? (UseEnglish ? 128f : 112f) : (UseEnglish ? 154f : 132f), layout.IsCompact ? 42f : 46f);
                if (timewarpedPoolVersion != TimewarpedPoolVersion.Current)
                {
                    UnityTavernUiStyle.EnsureComponent<Image>(timewarped.gameObject).color = Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.24f);
                }
            }

            var timewarpedToggle = ActionButton(
                "UnityTimewarpedTavernToggleButton",
                strip.transform,
                enableTimewarpedTavern ? T("时空酒馆：开启", "Timewarp: On") : T("时空酒馆：关闭", "Timewarp: Off"),
                true,
                () =>
                {
                    enableTimewarpedTavern = !enableTimewarpedTavern;
                    if (!enableTimewarpedTavern && activeTab == CardPoolTab.TimewarpedTavern)
                    {
                        activeTab = CardPoolTab.Minions;
                    }
                    Build();
                });
            UnityTavernUiStyle.SetFixedSize(timewarpedToggle.gameObject, layout.IsCompact ? 118f : 142f, layout.IsCompact ? 42f : 46f);
            if (enableTimewarpedTavern)
            {
                UnityTavernUiStyle.EnsureComponent<Image>(timewarpedToggle.gameObject).color = Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.24f);
            }

            var anomalyPool = ActionButton("UnityAnomalyPoolVersionButton", strip.transform, AnomalyPoolVersionButtonText(), true, () =>
            {
                AdvanceAnomalyPoolVersion();
                RemoveAnomaliesOutsideActivePool();
                Build();
            });
            UnityTavernUiStyle.SetFixedSize(anomalyPool.gameObject, layout.IsCompact ? (UseEnglish ? 128f : 112f) : (UseEnglish ? 154f : 132f), layout.IsCompact ? 42f : 46f);
            if (anomalyPoolVersion != AnomalyPoolVersion.CurrentHsReplay)
            {
                UnityTavernUiStyle.EnsureComponent<Image>(anomalyPool.gameObject).color = Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.24f);
            }

            var open = ActionButton("UnityCardPoolVersionOpenButton", strip.transform, T("编辑卡池", "Edit Pool"), true, () =>
            {
                versionModalOpen = true;
                Build();
            });
            UnityTavernUiStyle.SetFixedSize(open.gameObject, layout.IsCompact ? 108f : 128f, layout.IsCompact ? 42f : 46f);
        }

        private void AdvanceTimewarpedPoolVersion()
        {
            switch (timewarpedPoolVersion)
            {
                case TimewarpedPoolVersion.Current:
                    timewarpedPoolVersion = TimewarpedPoolVersion.FirestoneAll;
                    break;
                case TimewarpedPoolVersion.FirestoneAll:
                    timewarpedPoolVersion = TimewarpedPoolVersion.Launch;
                    break;
                default:
                    timewarpedPoolVersion = TimewarpedPoolVersion.Current;
                    break;
            }
        }

        private void AdvanceAnomalyPoolVersion()
        {
            switch (anomalyPoolVersion)
            {
                case AnomalyPoolVersion.CurrentHsReplay:
                    anomalyPoolVersion = AnomalyPoolVersion.Season5AllBg27;
                    break;
                case AnomalyPoolVersion.Season5AllBg27:
                    anomalyPoolVersion = AnomalyPoolVersion.Season5Launch;
                    break;
                case AnomalyPoolVersion.Season5Launch:
                    anomalyPoolVersion = AnomalyPoolVersion.AllKnown;
                    break;
                default:
                    anomalyPoolVersion = AnomalyPoolVersion.CurrentHsReplay;
                    break;
            }
        }

        private string AnomalyPoolVersionButtonText()
        {
            switch (anomalyPoolVersion)
            {
                case AnomalyPoolVersion.Season5AllBg27:
                    return T("畸变: S5全", "Anomaly: S5 All");
                case AnomalyPoolVersion.Season5Launch:
                    return T("畸变: S5初", "Anomaly: S5 Launch");
                case AnomalyPoolVersion.AllKnown:
                    return T("畸变: 全池", "Anomaly: All");
                default:
                    return T("畸变: 当前", "Anomaly: Current");
            }
        }

        private string TimewarpedPoolVersionButtonText()
        {
            switch (timewarpedPoolVersion)
            {
                case TimewarpedPoolVersion.FirestoneAll:
                    return T("时空: 全池", "Timewarp: All");
                case TimewarpedPoolVersion.Launch:
                    return T("时空: 上线", "Timewarp: Launch");
                default:
                    return T("时空: 当前", "Timewarp: Current");
            }
        }

        private void BuildHeroSummaryStrip(Transform parent)
        {
            var hero = CurrentHero();
            var strip = UiFactory.Panel("UnityTribeSelectionHeroPanel", parent, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(strip, layout.IsCompact ? 56f : 74f);
            UnityTavernUiStyle.ConfigureOutline(strip, new Color(UnityTavernUiStyle.Blue.r, UnityTavernUiStyle.Blue.g, UnityTavernUiStyle.Blue.b, 0.30f), new Vector2(1f, -1f));
            var stripLayout = strip.AddComponent<HorizontalLayoutGroup>();
            stripLayout.padding = new RectOffset(10, 10, layout.IsCompact ? 6 : 7, layout.IsCompact ? 6 : 7);
            stripLayout.spacing = 10;
            stripLayout.childControlWidth = true;
            stripLayout.childControlHeight = true;
            stripLayout.childForceExpandWidth = false;
            stripLayout.childForceExpandHeight = true;

            BuildHeroIcon(strip.transform, "UnityTribeSelectionHeroImage", hero, layout.IsCompact ? 44f : 60f);

            var textBlock = UiFactory.Panel("UnityTribeSelectionHeroTextBlock", strip.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(textBlock, 1f, 0f);
            var textLayout = textBlock.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = layout.IsCompact ? 1 : 2;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityTribeSelectionHeroName", textBlock.transform, hero == null ? T("未设置英雄", "No hero set") : HeroName(hero), layout.IsCompact ? 15 : 17, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 22f : 25f);

            var power = hero?.HeroPower;
            var powerText = power == null ? T("技能：未设置", "Power: not set") : T("技能：", "Power: ") + HeroPowerName(power) + T(" / 费用 ", " / Cost ") + power.Cost;
            var detail = UiFactory.Label("UnityTribeSelectionHeroPower", textBlock.transform, powerText, layout.IsCompact ? 11 : 12, FontStyle.Bold);
            detail.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(detail.gameObject, layout.IsCompact ? 18f : 21f);

            if (!layout.IsCompact)
            {
                var stats = UiFactory.Label("UnityTribeSelectionHeroStats", textBlock.transform, hero == null ? T("进入酒馆时由对局兜底", "Match will use fallback hero") : T("生命 ", "Health ") + hero.Health + T(" / 护甲 ", " / Armor ") + hero.Armor, 14, FontStyle.Normal);
                stats.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(stats.gameObject, 18f);
            }

            var choose = ActionButton("UnityTribeSelectionChooseHeroButton", strip.transform, layout.IsCompact ? T("选择", "Choose") : T("选择英雄", "Choose Hero"), true, () =>
            {
                heroSelectionOpen = true;
                Build();
            });
            UnityTavernUiStyle.SetFixedSize(choose.gameObject, layout.IsCompact ? 74f : 104f, layout.IsCompact ? 40f : 46f);
        }

        private void BuildHeroSelectionOverlay()
        {
            var modal = UnityHeroSelectionModalComponent.CreateModalHost(shell.transform, "UnityHeroSelectionOverlay");
            modal.transform.SetAsLastSibling();
            modal.GetComponent<UnityHeroSelectionModalComponent>().Build(
                heroCatalog,
                selectedHeroCardId,
                false,
                hero =>
                {
                    selectedHeroCardId = hero?.HeroCardId;
                    heroSelectionOpen = false;
                    Build();
                },
                () =>
                {
                    heroSelectionOpen = false;
                    Build();
                },
                T("选择英雄", "Choose Hero"),
                UseEnglish,
                !UseEnglish);
        }

        private void BuildVersionEditorOverlay()
        {
            var overlay = UiFactory.Panel("UnityCardPoolVersionOverlay", shell.transform, UnityTavernUiStyle.WithAlpha(Color.black, 0.68f));
            overlay.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;

            var panel = UiFactory.Panel("UnityCardPoolVersionModalPanel", overlay.transform, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.ConfigureOutline(panel, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.56f), new Vector2(2f, -2f));
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityVersionEditorStarLantern", UnityTavernUiStyle.ArcaneBlue);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.035f, 0.055f);
            rect.anchorMax = new Vector2(0.965f, 0.94f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(14, 14, 12, 14);
            panelLayout.spacing = 10;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var selection = CurrentSelection();
            BuildVersionModalHeader(panel.transform, selection);

            var body = UiFactory.Panel("UnityCardPoolVersionModalBody", panel.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(body, 1f, 1f);
            var bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 12;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;

            BuildVersionModalSidePanel(body.transform, selection);
            BuildVersionModalCardPanel(body.transform, selection);
            if (versionSwitchConfirmOpen)
            {
                BuildVersionSwitchConfirmDialog(overlay.transform);
            }
        }

        private void BuildVersionModalHeader(Transform parent, CardPoolVersionSelection selection)
        {
            var header = UiFactory.Panel("UnityCardPoolVersionModalHeader", parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(header, layout.IsCompact ? 58f : 64f);
            UnityTavernUiStyle.AddStarLanternRail(header.transform, "UnityVersionHeaderStarLantern", UnityTavernUiStyle.Gold);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 10, 8, 8);
            headerLayout.spacing = 8;
            headerLayout.childControlWidth = false;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            var minionTab = ActionButton("UnityCardPoolVersionMinionTab", header.transform, T("随从", "Minions"), true, () =>
            {
                SwitchCardPoolTab(CardPoolTab.Minions);
            });
            UnityTavernUiStyle.SetFixedSize(minionTab.gameObject, UseEnglish ? 86f : 72f, UnityTavernUiStyle.TouchHeight);

            var spellTab = ActionButton("UnityCardPoolVersionSpellTab", header.transform, T("酒馆法术", "Tavern Spells"), true, () =>
            {
                SwitchCardPoolTab(CardPoolTab.TavernSpells);
            });
            UnityTavernUiStyle.SetFixedSize(spellTab.gameObject, UseEnglish ? 112f : 72f, UnityTavernUiStyle.TouchHeight);

            if (enableTimewarpedTavern)
            {
                var timewarpedTab = ActionButton("UnityCardPoolVersionTimewarpedTab", header.transform, T("时空", "Timewarp"), true, () =>
                {
                    SwitchCardPoolTab(CardPoolTab.TimewarpedTavern);
                });
                UnityTavernUiStyle.SetFixedSize(timewarpedTab.gameObject, UseEnglish ? 96f : 72f, UnityTavernUiStyle.TouchHeight);
            }

            var title = UiFactory.Label("UnityCardPoolVersionModalTitle", header.transform, T("卡池版本", "Card Pool"), layout.IsCompact ? 18 : 22, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

            var summary = UiFactory.Label("UnityCardPoolVersionModalSummary", header.transform, VersionSummaryText(selection), 14, FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleRight;
            summary.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(summary.gameObject, layout.IsCompact ? 220f : 280f, 32f);

            var close = ActionButton("UnityCardPoolVersionCloseButton", header.transform, T("关闭", "Close"), true, () =>
            {
                versionModalOpen = false;
                Build();
            });
            UnityTavernUiStyle.SetFixedSize(close.gameObject, 72f, UnityTavernUiStyle.TouchHeight);
        }

        private void BuildVersionSwitchConfirmDialog(Transform parent)
        {
            var blocker = UiFactory.Panel("UnityCardPoolVersionUnsavedDialog", parent, new Color(0f, 0f, 0f, 0.58f));
            blocker.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(blocker.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(blocker).raycastTarget = true;

            var dialog = UiFactory.Panel("UnityCardPoolVersionUnsavedDialogPanel", blocker.transform, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.ConfigureOutline(dialog, new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.70f), new Vector2(2f, -2f));
            var rect = dialog.GetComponent<RectTransform>();
            rect.anchorMin = layout.IsCompact ? new Vector2(0.12f, 0.26f) : new Vector2(0.28f, 0.32f);
            rect.anchorMax = layout.IsCompact ? new Vector2(0.88f, 0.76f) : new Vector2(0.72f, 0.68f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var dialogLayout = dialog.AddComponent<VerticalLayoutGroup>();
            dialogLayout.padding = new RectOffset(16, 16, 14, 14);
            dialogLayout.spacing = 10;
            dialogLayout.childControlWidth = true;
            dialogLayout.childControlHeight = true;
            dialogLayout.childForceExpandWidth = true;
            dialogLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityCardPoolVersionUnsavedTitle", dialog.transform, T("有未保存的卡池改动", "Unsaved card pool changes"), layout.IsCompact ? 18 : 20, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 30f);

            var body = UiFactory.Label(
                "UnityCardPoolVersionUnsavedBody",
                dialog.transform,
                UseEnglish
                    ? "Before switching to \"" + VersionNameFor(pendingVersionSwitchId) + "\", choose what to do with the current version changes."
                    : "切换到“" + VersionNameFor(pendingVersionSwitchId) + "”前，请选择如何处理当前版本的勾选改动。",
                14,
                FontStyle.Bold);
            body.alignment = TextAnchor.MiddleCenter;
            body.color = UnityTavernUiStyle.Text;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetPreferredHeight(body.gameObject, 52f);

            var row = UiFactory.Panel("UnityCardPoolVersionUnsavedActions", dialog.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, UnityTavernUiStyle.TouchHeight);
            ConfigureButtonRow(row, 0, 8);
            ActionButton("UnityCardPoolVersionConfirmSaveAndSwitchButton", row.transform, T("保存并切换", "Save and Switch"), true, SaveAndSwitchVersion);
            ActionButton("UnityCardPoolVersionConfirmDiscardButton", row.transform, T("放弃修改", "Discard"), true, DiscardAndSwitchVersion);
            ActionButton("UnityCardPoolVersionConfirmCancelButton", row.transform, T("取消", "Cancel"), true, CancelVersionSwitch);
        }

        private void BuildVersionModalSidePanel(Transform parent, CardPoolVersionSelection selection)
        {
            var side = UiFactory.Panel("UnityCardPoolVersionSidePanel", parent, UnityTavernUiStyle.PanelQuiet);
            var sideElement = UnityTavernUiStyle.EnsureComponent<LayoutElement>(side);
            sideElement.minWidth = layout.IsCompact ? 210f : 240f;
            sideElement.preferredWidth = layout.IsCompact ? 210f : 240f;
            sideElement.flexibleWidth = 0f;
            var sideLayout = side.AddComponent<VerticalLayoutGroup>();
            sideLayout.padding = new RectOffset(10, 10, 10, 10);
            sideLayout.spacing = 8;
            sideLayout.childControlWidth = true;
            sideLayout.childControlHeight = true;
            sideLayout.childForceExpandWidth = true;
            sideLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityCardPoolVersionSideTitle", side.transform, T("版本", "Versions"), 16, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 26f);

            BuildVersionNameInput(side.transform, selection);
            BuildVersionPicker(side.transform);
            BuildVersionActions(side.transform, selection);
        }

        private void BuildVersionModalCardPanel(Transform parent, CardPoolVersionSelection selection)
        {
            var cards = UiFactory.Panel("UnityCardPoolVersionCardPanel", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetFlexible(cards, 1f, 1f);
            var cardLayout = cards.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(10, 10, 10, 10);
            cardLayout.spacing = 8;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            BuildSearch(cards.transform);
            BuildVersionFilters(cards.transform, selection);
            BuildVersionBulkActions(cards.transform, selection);
            BuildCardList(cards.transform, selection);
        }

        private string VersionSummaryText(CardPoolVersionSelection selection)
        {
            var savedState = hasUnsavedCardPoolChanges ? T("  未保存", "  Unsaved") : string.Empty;
            var timewarpedCount = timewarpedTavernCatalog == null ? 0 : timewarpedTavernCatalog.All.Count;
            return selection.VersionName +
                "  " +
                (selection.IsDefault ? T("默认", "Default") : T("自定义", "Custom")) +
                T("  随从 ", "  Minions ") +
                enabledMinionCardIds.Count +
                T(" / 法术 ", " / Spells ") +
                enabledTavernSpellCardNumbers.Count +
                T(" / 时空 ", " / Timewarp ") +
                timewarpedCount +
                savedState;
        }

        private void BuildVersionNameInput(Transform parent, CardPoolVersionSelection selection)
        {
            var panel = UiFactory.Panel("UnityCardPoolVersionNamePanel", parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(panel, 76f);
            UnityTavernUiStyle.ConfigureOutline(panel, new Color(1f, 1f, 1f, 0.08f), new Vector2(1f, -1f));
            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(6, 6, 5, 6);
            panelLayout.spacing = 5;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var header = UiFactory.Panel("UnityCardPoolVersionNameHeader", panel.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(header, 20f);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 6;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            var label = UiFactory.Label("UnityCardPoolVersionNameLabel", header.transform, T("版本名称", "Version Name"), 14, FontStyle.Bold);
            label.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(label.gameObject).flexibleWidth = 1f;

            var hint = UiFactory.Label(
                "UnityCardPoolVersionNameHint",
                header.transform,
                selection.IsDefault ? T("默认只读", "Read only") : T("点击改名", "Rename"),
                14,
                FontStyle.Bold);
            hint.alignment = TextAnchor.MiddleRight;
            hint.color = selection.IsDefault ? UnityTavernUiStyle.MutedText : UnityTavernUiStyle.Gold;
            var hintElement = UnityTavernUiStyle.EnsureComponent<LayoutElement>(hint.gameObject);
            hintElement.minWidth = 64f;
            hintElement.preferredWidth = 64f;

            var inputObject = new GameObject("UnityCardPoolVersionNameInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetPreferredHeight(inputObject, 38f);
            UnityTavernUiStyle.ConfigureSurface(
                inputObject,
                selection.IsDefault ? UnityTavernUiStyle.PanelQuiet : Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.10f),
                true);
            UnityTavernUiStyle.ConfigureOutline(
                inputObject,
                selection.IsDefault
                    ? new Color(1f, 1f, 1f, 0.10f)
                    : new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.55f),
                new Vector2(1f, -1f));

            var input = inputObject.GetComponent<InputField>();
            input.interactable = !selection.IsDefault;
            input.caretColor = UnityTavernUiStyle.Text;
            input.selectionColor = new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.35f);
            input.textComponent = UiFactory.Label("UnityCardPoolVersionNameText", inputObject.transform, string.Empty, 14, FontStyle.Bold);
            UnityTavernUiStyle.Stretch(input.textComponent.rectTransform);
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            input.textComponent.color = selection.IsDefault ? UnityTavernUiStyle.MutedText : UnityTavernUiStyle.Text;
            input.textComponent.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.textComponent.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.placeholder = UiFactory.Label(
                "UnityCardPoolVersionNamePlaceholder",
                inputObject.transform,
                selection.IsDefault ? T("默认版本不可改名", "Default version cannot be renamed") : T("输入版本名称", "Enter version name"),
                14);
            UnityTavernUiStyle.Stretch(input.placeholder.rectTransform);
            input.placeholder.color = UnityTavernUiStyle.MutedText;
            input.placeholder.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.placeholder.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.text = selection.VersionName;
            input.onEndEdit.AddListener(value =>
            {
                RenameCurrentVersion(value);
                Build();
            });
        }

        private void BuildVersionPicker(Transform parent)
        {
            var row = UiFactory.Panel("UnityCardPoolVersionPicker", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(row, store.Versions.Count > 4 ? 118f : 64f);
            var rowLayout = row.AddComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset(6, 6, 6, 6);
            rowLayout.spacing = 5;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            var top = UiFactory.Panel("UnityCardPoolVersionPickerTop", row.transform, UnityTavernUiStyle.PanelQuiet);
            ConfigureButtonRow(top, 0, 5);
            ActionButton("UnityCardPoolVersionDefaultButton", top.transform, T("默认", "Default"), true, () =>
            {
                RequestVersionSwitch(null);
            });

            foreach (var version in store.Versions.Take(4))
            {
                var capturedId = version.Id;
                ActionButton("UnityCardPoolVersionSelect-" + version.Id, top.transform, ShortLabel(version.Name), true, () =>
                {
                    RequestVersionSwitch(capturedId);
                });
            }

            if (store.Versions.Count > 4)
            {
                var bottom = UiFactory.Panel("UnityCardPoolVersionPickerMore", row.transform, UnityTavernUiStyle.PanelQuiet);
                ConfigureButtonRow(bottom, 0, 5);
                foreach (var version in store.Versions.Skip(4).Take(6))
                {
                    var capturedId = version.Id;
                    ActionButton("UnityCardPoolVersionSelect-" + version.Id, bottom.transform, ShortLabel(version.Name), true, () =>
                    {
                        RequestVersionSwitch(capturedId);
                    });
                }
            }
        }

        private void BuildVersionActions(Transform parent, CardPoolVersionSelection selection)
        {
            var row = UiFactory.Panel("UnityCardPoolVersionActions", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(row, UnityTavernUiStyle.TouchHeight);
            ConfigureButtonRow(row, 6, 6);

            var canCreate = store.Versions.Count < CardPoolVersionFactory.MaxCustomVersions;
            ActionButton("UnityCardPoolVersionNewButton", row.transform, T("新建", "New"), canCreate, () => CreateVersionFromDefault());
            ActionButton("UnityCardPoolVersionCopyButton", row.transform, T("复制", "Copy"), canCreate, () => CopyCurrentVersion());
            var save = ActionButton("UnityCardPoolVersionSaveButton", row.transform, hasUnsavedCardPoolChanges ? T("保存*", "Save*") : T("保存", "Save"), !selection.IsDefault && hasUnsavedCardPoolChanges, () =>
            {
                SaveCurrentVersion();
                Build();
            });
            if (hasUnsavedCardPoolChanges && !selection.IsDefault)
            {
                UnityTavernUiStyle.EnsureComponent<Image>(save.gameObject).color = Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.35f);
            }

            ActionButton("UnityCardPoolVersionDeleteButton", row.transform, T("删除", "Delete"), !selection.IsDefault, () => DeleteCurrentVersion());
        }

        private void BuildSearch(Transform parent)
        {
            var row = UiFactory.Panel("UnityCardPoolVersionSearchRow", parent, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, UnityTavernUiStyle.TouchHeight);
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(row).flexibleHeight = 0f;
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var inputObject = new GameObject("UnityCardPoolVersionSearchInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(row.transform, false);
            var inputElement = UnityTavernUiStyle.EnsureComponent<LayoutElement>(inputObject);
            inputElement.flexibleWidth = 1f;
            inputElement.minHeight = UnityTavernUiStyle.TouchHeight;
            inputElement.preferredHeight = UnityTavernUiStyle.TouchHeight;
            inputObject.GetComponent<Image>().color = UnityTavernUiStyle.PanelQuiet;

            var input = inputObject.GetComponent<InputField>();
            input.textComponent = UiFactory.Label("UnityCardPoolVersionSearchText", inputObject.transform, string.Empty, 14);
            UnityTavernUiStyle.Stretch(input.textComponent.rectTransform);
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            input.textComponent.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.textComponent.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.placeholder = UiFactory.Label("UnityCardPoolVersionSearchPlaceholder", inputObject.transform, T("搜索名称或编号", "Search name or id"), 14);
            UnityTavernUiStyle.Stretch(input.placeholder.rectTransform);
            input.placeholder.color = UnityTavernUiStyle.MutedText;
            input.placeholder.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.placeholder.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.text = searchText;
            input.onEndEdit.AddListener(value =>
            {
                var nextSearch = value ?? string.Empty;
                if (!string.Equals(searchText, nextSearch, StringComparison.Ordinal))
                {
                    ResetVisibleCardPoolItems();
                }

                searchText = nextSearch;
                Build();
            });

            var hasActiveFilters = !string.IsNullOrWhiteSpace(searchText) || versionTierFilter != 0 || versionTribeFilter != Tribe.All;
            var reset = ActionButton(
                "UnityCardPoolVersionResetFiltersButton",
                row.transform,
                T("清空筛选", "Clear Filters"),
                hasActiveFilters,
                () =>
                {
                    searchText = string.Empty;
                    versionTierFilter = 0;
                    versionTribeFilter = Tribe.All;
                    ResetVisibleCardPoolItems();
                    Build();
                });
            var resetElement = UnityTavernUiStyle.EnsureComponent<LayoutElement>(reset.gameObject);
            resetElement.minWidth = layout.IsCompact ? 96f : 112f;
            resetElement.preferredWidth = layout.IsCompact ? 96f : 112f;
            resetElement.flexibleWidth = 0f;
        }

        private void BuildVersionFilters(Transform parent, CardPoolVersionSelection selection)
        {
            var filteredCount = FilteredCardPoolCount();
            var typeFilterColumns = layout.IsCompact ? 5 : 6;
            var typeFilterCount = 2 + TribeAvailabilityRules.PlayableTribes.Count();
            var typeFilterRows = Mathf.CeilToInt(typeFilterCount / (float)typeFilterColumns);
            var typeFilterHeight = typeFilterRows * UnityTavernUiStyle.TouchHeight + Mathf.Max(0, typeFilterRows - 1) * 5f;
            var panel = UiFactory.Panel("UnityCardPoolVersionFilters", parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(panel, 96f + typeFilterHeight);
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(panel).flexibleHeight = 0f;
            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(8, 8, 7, 7);
            panelLayout.spacing = 6;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var tierRow = UiFactory.Panel("UnityCardPoolVersionTierFilters", panel.transform, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(tierRow, UnityTavernUiStyle.TouchHeight);
            ConfigureButtonRow(tierRow, 0, 5);
            FilterButton("UnityCardPoolVersionTierAllButton", tierRow.transform, T("全部", "All"), versionTierFilter == 0, UnityTavernUiStyle.Gold, () =>
            {
                versionTierFilter = 0;
                ResetVisibleCardPoolItems();
                Build();
            });
            for (var tier = 1; tier <= 7; tier += 1)
            {
                var capturedTier = tier;
                FilterButton("UnityCardPoolVersionTier" + tier + "Button", tierRow.transform, T(tier + "本", "T" + tier), versionTierFilter == tier, UnityTavernUiStyle.Gold, () =>
                {
                    versionTierFilter = capturedTier;
                    ResetVisibleCardPoolItems();
                    Build();
                });
            }

            var typeRow = UiFactory.Panel("UnityCardPoolVersionTypeFilters", panel.transform, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(typeRow, typeFilterHeight);
            var grid = typeRow.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.spacing = new Vector2(5f, 5f);
            grid.cellSize = new Vector2(layout.IsCompact ? 62f : 70f, UnityTavernUiStyle.TouchHeight);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = typeFilterColumns;

            FilterButton("UnityCardPoolVersionTribeAllButton", typeRow.transform, T("全部", "All"), versionTribeFilter == Tribe.All, UnityTavernUiStyle.Green, () =>
            {
                versionTribeFilter = Tribe.All;
                ResetVisibleCardPoolItems();
                Build();
            });
            FilterButton("UnityCardPoolVersionTribeNoneButton", typeRow.transform, activeTab == CardPoolTab.TavernSpells ? T("通用", "General") : TribeName(Tribe.None), versionTribeFilter == Tribe.None, UnityTavernUiStyle.Blue, () =>
            {
                versionTribeFilter = Tribe.None;
                ResetVisibleCardPoolItems();
                Build();
            });
            foreach (var tribe in TribeAvailabilityRules.PlayableTribes)
            {
                var capturedTribe = tribe;
                FilterButton("UnityCardPoolVersionTribe" + tribe + "Button", typeRow.transform, TribeName(tribe), versionTribeFilter == tribe, TribeAccent(tribe), () =>
                {
                    versionTribeFilter = capturedTribe;
                    ResetVisibleCardPoolItems();
                    Build();
                });
            }

            var tierSummary = versionTierFilter == 0
                ? T("全部星级", "All tiers")
                : T(versionTierFilter + "本", "T" + versionTierFilter);
            var tribeSummary = versionTribeFilter == Tribe.All
                ? T("全部类型", "All types")
                : activeTab == CardPoolTab.TavernSpells && versionTribeFilter == Tribe.None
                    ? T("通用", "General")
                    : TribeName(versionTribeFilter);
            var query = (searchText ?? string.Empty).Trim();
            var shortQuery = query.Length <= 12 ? query : query.Substring(0, 12) + "…";
            var searchSummary = string.IsNullOrEmpty(shortQuery)
                ? string.Empty
                : T(" · 搜索“" + shortQuery + "”", " · Search \"" + shortQuery + "\"");
            var count = UiFactory.Label(
                "UnityCardPoolVersionFilterCount",
                panel.transform,
                UseEnglish
                    ? "Results " + filteredCount + " cards · " + tierSummary + " · " + tribeSummary + searchSummary + (selection.IsDefault ? " · Read only" : string.Empty)
                    : "结果 " + filteredCount + " 张 · " + tierSummary + " · " + tribeSummary + searchSummary + (selection.IsDefault ? " · 默认版本只读" : string.Empty),
                14,
                FontStyle.Bold);
            count.alignment = TextAnchor.MiddleLeft;
            count.color = selection.IsDefault ? UnityTavernUiStyle.MutedText : UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(count.gameObject, 22f);
        }

        private void BuildVersionBulkActions(Transform parent, CardPoolVersionSelection selection)
        {
            var panel = UiFactory.Panel("UnityCardPoolVersionBulkActions", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(panel, 55f);
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(panel).flexibleHeight = 0f;
            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(4, 4, 3, 4);
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var row = UiFactory.Panel("UnityCardPoolVersionBulkActionButtons", panel.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, UnityTavernUiStyle.TouchHeight);
            ConfigureButtonRow(row, 4, 8);

            var canEditActiveTab = !selection.IsDefault && activeTab != CardPoolTab.TimewarpedTavern;
            ActionButton("UnityCardPoolVersionExcludeFilteredButton", row.transform, T("剔除当前筛选", "Exclude Filtered"), canEditActiveTab, () =>
            {
                SetFilteredEnabled(false);
                Build();
            });
            ActionButton("UnityCardPoolVersionIncludeFilteredButton", row.transform, T("加入当前筛选", "Include Filtered"), canEditActiveTab, () =>
            {
                SetFilteredEnabled(true);
                Build();
            });
        }

        private void BuildCardList(Transform parent, CardPoolVersionSelection selection)
        {
            var content = UiFactory.ScrollView("UnityCardPoolVersionScroll", parent, UnityTavernUiStyle.PanelQuiet, out var scrollRect);
            cardPoolListContent = content;
            var listLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(6, 10, 6, 6);
            listLayout.spacing = 5;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            var totalCount = 0;
            if (activeTab == CardPoolTab.Minions)
            {
                var minions = FilteredMinions().ToList();
                totalCount = minions.Count;
                foreach (var minion in minions.Take(visibleCardPoolItemCount))
                {
                    BuildPoolToggleRow(
                        content,
                        "UnityCardPoolMinionToggle-" + minion.CardId,
                        CardImageProvider.LoadSprite(minion),
                        "T" + minion.TavernTier + "  " + minion.Name,
                        TribeListText(minion.Tribes) + "  " + minion.CardId,
                        enabledMinionCardIds.Contains(minion.CardId),
                        !selection.IsDefault,
                        value =>
                        {
                            if (SetEnabled(enabledMinionCardIds, minion.CardId, value))
                            {
                                MarkCardPoolDirty();
                                Build();
                            }
                        });
                }
            }
            else if (activeTab == CardPoolTab.TavernSpells)
            {
                var spells = FilteredSpells().ToList();
                totalCount = spells.Count;
                foreach (var spell in spells.Take(visibleCardPoolItemCount))
                {
                    BuildPoolToggleRow(
                        content,
                        "UnityCardPoolSpellToggle-" + spell.CardNumber,
                        CardImageProvider.LoadSprite(spell.ImagePath, spell.CardNumber, CardKind.TavernSpell),
                        "T" + spell.TavernTier + "  " + spell.Name,
                        SpellTribesText(spell) + "  " + spell.CardNumber,
                        enabledTavernSpellCardNumbers.Contains(spell.CardNumber),
                        !selection.IsDefault,
                        value =>
                        {
                            if (SetEnabled(enabledTavernSpellCardNumbers, spell.CardNumber, value))
                            {
                                MarkCardPoolDirty();
                                Build();
                            }
                        });
                }
            }
            else
            {
                var cards = FilteredTimewarpedCards().ToList();
                totalCount = cards.Count;
                foreach (var card in cards.Take(visibleCardPoolItemCount))
                {
                    BuildPoolToggleRow(
                        content,
                        "UnityCardPoolTimewarpedToggle-" + SafeCardName(card.CardId),
                        CardImageProvider.LoadSprite(card.ImagePath, card.CardId, card.CardKind),
                        TimewarpedCardTitle(card),
                        TimewarpedCardDetail(card),
                        enabledTimewarpedCardIds.Contains(card.CardId),
                        !selection.IsDefault,
                        value =>
                        {
                            if (SetEnabled(enabledTimewarpedCardIds, card.CardId, value))
                            {
                                Build();
                            }
                        });
                }
            }

            BuildCardPoolLoadState(content, totalCount);
            ConfigureCardPoolScrollLoading(scrollRect, totalCount);
        }

        private void BuildCardPoolLoadState(Transform parent, int totalCount)
        {
            var visibleCount = Math.Min(visibleCardPoolItemCount, totalCount);
            var label = UiFactory.Label(
                "UnityCardPoolVersionLoadState",
                parent,
                totalCount == 0
                    ? T("当前筛选无卡牌", "No cards match the current filters.")
                    : T("已显示 ", "Showing ") + visibleCount + " / " + totalCount,
                14,
                FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, 24f);
        }

        private void ConfigureCardPoolScrollLoading(ScrollRect scrollRect, int totalCount)
        {
            if (scrollRect == null)
            {
                return;
            }

            var anchorAtBottom = keepVersionListAtBottom;
            keepVersionListAtBottom = false;
            if (anchorAtBottom)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }

            scrollRect.onValueChanged.AddListener(position =>
            {
                if (position.y <= CardPoolLoadMoreThreshold)
                {
                    LoadMoreVisibleCardPoolItems(totalCount);
                }
            });
        }

        private void LoadMoreVisibleCardPoolItems(int totalCount)
        {
            if (visibleCardPoolItemCount >= totalCount || cardPoolListContent == null)
            {
                return;
            }

            var previousVisibleCount = visibleCardPoolItemCount;
            visibleCardPoolItemCount = Math.Min(visibleCardPoolItemCount + CardPoolLoadStep, totalCount);
            AppendVisibleCardPoolRows(cardPoolListContent, CurrentSelection(), previousVisibleCount, visibleCardPoolItemCount);

            var loadState = FindDirectChild(cardPoolListContent, "UnityCardPoolVersionLoadState");
            if (loadState != null)
            {
                loadState.SetAsLastSibling();
            }
        }

        private void AppendVisibleCardPoolRows(Transform content, CardPoolVersionSelection selection, int startIndex, int endIndex)
        {
            if (activeTab == CardPoolTab.Minions)
            {
                var minions = FilteredMinions().ToList();
                foreach (var minion in minions.Skip(startIndex).Take(Math.Max(0, Math.Min(endIndex, minions.Count) - startIndex)))
                {
                    BuildPoolToggleRow(
                        content,
                        "UnityCardPoolMinionToggle-" + minion.CardId,
                        CardImageProvider.LoadSprite(minion),
                        "T" + minion.TavernTier + "  " + minion.Name,
                        TribeListText(minion.Tribes) + "  " + minion.CardId,
                        enabledMinionCardIds.Contains(minion.CardId),
                        !selection.IsDefault,
                        value =>
                        {
                            if (SetEnabled(enabledMinionCardIds, minion.CardId, value))
                            {
                                MarkCardPoolDirty();
                                Build();
                            }
                        });
                }

                return;
            }

            if (activeTab == CardPoolTab.TavernSpells)
            {
                var spells = FilteredSpells().ToList();
                foreach (var spell in spells.Skip(startIndex).Take(Math.Max(0, Math.Min(endIndex, spells.Count) - startIndex)))
                {
                    BuildPoolToggleRow(
                        content,
                        "UnityCardPoolSpellToggle-" + spell.CardNumber,
                        CardImageProvider.LoadSprite(spell.ImagePath, spell.CardNumber, CardKind.TavernSpell),
                        "T" + spell.TavernTier + "  " + spell.Name,
                        SpellTribesText(spell) + "  " + spell.CardNumber,
                        enabledTavernSpellCardNumbers.Contains(spell.CardNumber),
                        !selection.IsDefault,
                        value =>
                        {
                            if (SetEnabled(enabledTavernSpellCardNumbers, spell.CardNumber, value))
                            {
                                MarkCardPoolDirty();
                                Build();
                            }
                        });
                }

                return;
            }

            var cards = FilteredTimewarpedCards().ToList();
            foreach (var card in cards.Skip(startIndex).Take(Math.Max(0, Math.Min(endIndex, cards.Count) - startIndex)))
            {
                BuildPoolToggleRow(
                    content,
                    "UnityCardPoolTimewarpedToggle-" + SafeCardName(card.CardId),
                    CardImageProvider.LoadSprite(card.ImagePath, card.CardId, card.CardKind),
                    TimewarpedCardTitle(card),
                    TimewarpedCardDetail(card),
                    enabledTimewarpedCardIds.Contains(card.CardId),
                    !selection.IsDefault,
                    value =>
                    {
                        if (SetEnabled(enabledTimewarpedCardIds, card.CardId, value))
                        {
                            Build();
                        }
                    });
            }
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = parent.GetChild(index);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private IEnumerable<MinionDefinition> FilteredMinions()
        {
            var query = (searchText ?? string.Empty).Trim();
            var cards = minionCatalog.All.Where(minion => minion.InPool && !IsDuoMinion(minion));
            if (versionTierFilter > 0)
            {
                cards = cards.Where(minion => minion.TavernTier == versionTierFilter);
            }

            if (versionTribeFilter != Tribe.All)
            {
                cards = cards.Where(minion => MatchesMinionTribe(minion, versionTribeFilter));
            }

            if (!string.IsNullOrEmpty(query))
            {
                cards = cards.Where(minion =>
                    Contains(minion.Name, query) ||
                    Contains(minion.CardId, query) ||
                    Contains(minion.Id, query));
            }

            return cards.OrderBy(minion => minion.TavernTier).ThenBy(minion => minion.Name);
        }

        private IEnumerable<TavernSpellDefinition> FilteredSpells()
        {
            var query = (searchText ?? string.Empty).Trim();
            var spells = spellCatalog.All.Where(spell => spell.InPool && spell.Category == "TavernSpell");
            if (versionTierFilter > 0)
            {
                spells = spells.Where(spell => spell.TavernTier == versionTierFilter);
            }

            if (versionTribeFilter != Tribe.All)
            {
                spells = spells.Where(spell => MatchesSpellTribe(spell, versionTribeFilter));
            }

            if (!string.IsNullOrEmpty(query))
            {
                spells = spells.Where(spell =>
                    Contains(spell.Name, query) ||
                    Contains(spell.EnglishName, query) ||
                    Contains(spell.CardNumber, query) ||
                    Contains(spell.Id, query));
            }

            return spells.OrderBy(spell => spell.TavernTier).ThenBy(spell => spell.Name);
        }

        private IEnumerable<TimewarpedTavernCardDefinition> FilteredTimewarpedCards()
        {
            var query = (searchText ?? string.Empty).Trim();
            var cards = timewarpedTavernCatalog == null
                ? Enumerable.Empty<TimewarpedTavernCardDefinition>()
                : timewarpedTavernCatalog.All.Where(card => !string.IsNullOrEmpty(card.CardId));
            if (versionTierFilter > 0)
            {
                cards = cards.Where(card => card.TechLevel == versionTierFilter);
            }

            if (versionTribeFilter != Tribe.All)
            {
                cards = cards.Where(card => MatchesTimewarpedTribe(card, versionTribeFilter));
            }

            if (!string.IsNullOrEmpty(query))
            {
                cards = cards.Where(card =>
                    Contains(card.Name, query) ||
                    Contains(card.ZhName, query) ||
                    Contains(card.CardId, query) ||
                    Contains(card.PoolStatus, query));
            }

            return cards.OrderBy(card => card.TechLevel).ThenBy(card => TimewarpedCardDisplayName(card));
        }

        private int FilteredCardPoolCount()
        {
            switch (activeTab)
            {
                case CardPoolTab.Minions:
                    return FilteredMinions().Count();
                case CardPoolTab.TavernSpells:
                    return FilteredSpells().Count();
                default:
                    return FilteredTimewarpedCards().Count();
            }
        }

        private static bool MatchesTimewarpedTribe(TimewarpedTavernCardDefinition card, Tribe tribe)
        {
            if (tribe == Tribe.All)
            {
                return true;
            }

            if (tribe == Tribe.None)
            {
                return card == null || card.Tribes == null || card.Tribes.Count == 0 || card.Tribes.All(value => value == Tribe.None);
            }

            return card != null && card.Tribes != null && (card.Tribes.Contains(tribe) || card.Tribes.Contains(Tribe.All));
        }

        private string TimewarpedCardTitle(TimewarpedTavernCardDefinition card)
        {
            if (card == null)
            {
                return T("时空酒馆", "Timewarped Tavern");
            }

            return "T" + card.TechLevel + "  " + TimewarpedCardDisplayName(card);
        }

        private string TimewarpedCardDetail(TimewarpedTavernCardDefinition card)
        {
            if (card == null)
            {
                return string.Empty;
            }

            return TimewarpKindName(card.TimewarpKind) + " / " + CardKindName(card.CardKind) + " / " + TimewarpedTribesText(card.Tribes) + "  " + card.CardId;
        }

        private string TimewarpedCardDisplayName(TimewarpedTavernCardDefinition card)
        {
            if (!UseEnglish && !string.IsNullOrEmpty(card?.ZhName))
            {
                return card.ZhName;
            }

            return string.IsNullOrEmpty(card?.Name) ? card?.CardId ?? T("未命名", "Unnamed") : card.Name;
        }

        private string TimewarpedTribesText(IEnumerable<Tribe> tribes)
        {
            return TribeListText(tribes);
        }

        private string TimewarpKindName(TimewarpKind kind)
        {
            switch (kind)
            {
                case TimewarpKind.Minor: return T("小型时空", "Minor Timewarp");
                case TimewarpKind.Major: return T("大型时空", "Major Timewarp");
                case TimewarpKind.Historical: return T("历史时空", "Historical Timewarp");
                default: return T("时空", "Timewarp");
            }
        }

        private string CardKindName(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Minion: return T("随从", "Minion");
                case CardKind.TavernSpell: return T("酒馆法术", "Tavern Spell");
                case CardKind.Spell: return T("法术", "Spell");
                case CardKind.Trinket: return T("饰品", "Trinket");
                case CardKind.Quest: return T("任务", "Quest");
                case CardKind.QuestReward: return T("任务奖励", "Quest Reward");
                default: return kind.ToString();
            }
        }

        private void BuildPoolToggleRow(Transform parent, string name, Sprite sprite, string titleText, string detailText, bool isOn, bool interactable, Action<bool> changed)
        {
            var row = UiFactory.Panel(name + "Row", parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(row, 76f);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 8, 6, 6);
            rowLayout.spacing = 8;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var toggleObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggleObject.transform.SetParent(row.transform, false);
            UnityTavernUiStyle.SetFixedSize(toggleObject, UnityTavernUiStyle.TouchHeight, UnityTavernUiStyle.TouchHeight);
            var background = toggleObject.GetComponent<Image>();
            background.color = UnityTavernUiStyle.PanelQuiet;
            UnityTavernUiStyle.ConfigureOutline(toggleObject, UnityTavernUiStyle.WithAlpha(isOn ? UnityTavernUiStyle.FocusRing : UnityTavernUiStyle.Brass, isOn ? 0.78f : 0.34f), new Vector2(1f, -1f));

            var check = new GameObject(name + "Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(toggleObject.transform, false);
            UnityTavernUiStyle.Stretch(check.GetComponent<RectTransform>());
            check.GetComponent<RectTransform>().offsetMin = new Vector2(14f, 14f);
            check.GetComponent<RectTransform>().offsetMax = new Vector2(-14f, -14f);
            check.GetComponent<Image>().color = UnityTavernUiStyle.Gold;

            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check.GetComponent<Image>();
            toggle.SetIsOnWithoutNotify(isOn);
            toggle.interactable = interactable;
            toggle.onValueChanged.AddListener(value => changed?.Invoke(value));

            BuildCardThumbnail(row.transform, name, sprite, titleText);

            var labelBlock = UiFactory.Panel(name + "LabelBlock", row.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(labelBlock, 1f, 0f);
            var labelLayout = labelBlock.AddComponent<VerticalLayoutGroup>();
            labelLayout.padding = new RectOffset(0, 0, 4, 4);
            labelLayout.spacing = 2;
            labelLayout.childControlWidth = true;
            labelLayout.childControlHeight = true;
            labelLayout.childForceExpandWidth = true;
            labelLayout.childForceExpandHeight = false;

            var title = UiFactory.Label(name + "Label", labelBlock.transform, titleText, 14, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleLeft;
            title.color = interactable ? UnityTavernUiStyle.Text : UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 30f);

            var detail = UiFactory.Label(name + "Detail", labelBlock.transform, detailText, 14);
            detail.alignment = TextAnchor.MiddleLeft;
            detail.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(detail.gameObject, 22f);
        }

        private void BuildSetupToggle(Transform parent, string name, string text, bool isOn, bool interactable, Action<bool> changed)
        {
            var toggleObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetPreferredHeight(toggleObject, UnityTavernUiStyle.TouchHeight);
            var surface = toggleObject.GetComponent<Image>();
            surface.color = isOn
                ? Color.Lerp(UnityTavernUiStyle.Panel, UnityTavernUiStyle.Blue, 0.28f)
                : UnityTavernUiStyle.PanelQuiet;

            var rowLayout = toggleObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(7, 7, 5, 5);
            rowLayout.spacing = 6;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var check = new GameObject(name + "Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(toggleObject.transform, false);
            UnityTavernUiStyle.SetFixedSize(check, 16f, 16f);
            check.GetComponent<Image>().color = interactable ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.MutedText;

            var displayText = !interactable && name == "UnityAdvancedMechanicsToggle-ShowDisabled"
                ? T("禁用池（需先开启调试池）", "Disabled Pool (enable Debug Pool first)")
                : text;
            var label = UiFactory.Label(name + "Label", toggleObject.transform, displayText, 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = interactable ? UnityTavernUiStyle.Text : UnityTavernUiStyle.MutedText;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetFlexible(label.gameObject, 1f, 0f);

            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = surface;
            toggle.graphic = check.GetComponent<Image>();
            toggle.SetIsOnWithoutNotify(isOn);
            toggle.interactable = interactable;
            toggle.onValueChanged.AddListener(value => changed?.Invoke(value));
            var colors = toggle.colors;
            colors.normalColor = surface.color;
            colors.highlightedColor = Color.Lerp(surface.color, UnityTavernUiStyle.Gold, 0.18f);
            colors.pressedColor = Color.Lerp(surface.color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(surface.color.r, surface.color.g, surface.color.b, 0.42f);
            colors.fadeDuration = 0.08f;
            toggle.colors = colors;
            UnityTavernUiStyle.ConfigureOutline(
                toggleObject,
                isOn ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.52f) : new Color(0f, 0f, 0f, 0.16f),
                new Vector2(1f, -1f));
        }

        private void BuildCardThumbnail(Transform parent, string name, Sprite sprite, string displayName)
        {
            var frame = UiFactory.Panel(name + "ImageFrame", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetFixedSize(frame, 46f, 64f);

            if (sprite == null)
            {
                var empty = UiFactory.Label(
                    name + "ImageFallbackText",
                    frame.transform,
                    UnityTavernUiStyle.ArtFallbackText(displayName, T("无图", "NA")),
                    20,
                    FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.Text;
                empty.raycastTarget = false;
                UnityTavernUiStyle.ConfigureOutline(empty.gameObject, new Color(0f, 0f, 0f, 0.78f), new Vector2(1f, -1f));
                UnityTavernUiStyle.Stretch(empty.rectTransform);
                return;
            }

            var imageObject = new GameObject(name + "Image", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(frame.transform, false);
            UnityTavernUiStyle.Stretch(imageObject.GetComponent<RectTransform>());
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private CardPoolVersionSelection CurrentSelection()
        {
            if (string.IsNullOrEmpty(selectedVersionId))
            {
                return CardPoolVersionFactory.CreateDefaultSelection(minionCatalog, spellCatalog);
            }

            var profile = store.Versions.FirstOrDefault(version => string.Equals(version.Id, selectedVersionId, StringComparison.OrdinalIgnoreCase));
            return CardPoolVersionFactory.CreateSelection(profile) ?? CardPoolVersionFactory.CreateDefaultSelection(minionCatalog, spellCatalog);
        }

        private void SelectVersion(string versionId, bool persist = true)
        {
            var profile = string.IsNullOrEmpty(versionId)
                ? null
                : store.Versions.FirstOrDefault(version => string.Equals(version.Id, versionId, StringComparison.OrdinalIgnoreCase));
            var selection = profile == null
                ? CardPoolVersionFactory.CreateDefaultSelection(minionCatalog, spellCatalog)
                : CardPoolVersionFactory.CreateSelection(profile);

            selectedVersionId = selection.IsDefault ? null : selection.VersionId;
            enabledMinionCardIds.Clear();
            enabledTavernSpellCardNumbers.Clear();
            enabledQuestCardIds.Clear();
            enabledQuestRewardCardIds.Clear();
            enabledLesserTrinketCardIds.Clear();
            enabledGreaterTrinketCardIds.Clear();
            enabledAnomalyCardIds.Clear();
            foreach (var cardId in selection.EnabledMinionCardIds.Where(value => !IsDuoCardId(value)))
            {
                enabledMinionCardIds.Add(cardId);
            }

            foreach (var cardNumber in selection.EnabledTavernSpellCardNumbers)
            {
                enabledTavernSpellCardNumbers.Add(cardNumber);
            }

            foreach (var cardId in selection.EnabledQuestCardIds)
            {
                enabledQuestCardIds.Add(cardId);
            }

            foreach (var cardId in selection.EnabledQuestRewardCardIds)
            {
                enabledQuestRewardCardIds.Add(cardId);
            }

            foreach (var cardId in selection.EnabledLesserTrinketCardIds)
            {
                enabledLesserTrinketCardIds.Add(cardId);
            }

            foreach (var cardId in selection.EnabledGreaterTrinketCardIds)
            {
                enabledGreaterTrinketCardIds.Add(cardId);
            }

            foreach (var cardId in selection.EnabledAnomalyCardIds)
            {
                enabledAnomalyCardIds.Add(cardId);
            }

            SyncAdvancedMechanicFlagsFromPools();

            hasUnsavedCardPoolChanges = false;
            versionSwitchConfirmOpen = false;
            pendingVersionSwitchId = null;

            if (persist)
            {
                store.SelectedVersionId = selectedVersionId;
                repository.Save(store);
            }
        }

        private void RequestVersionSwitch(string versionId)
        {
            if (IsCurrentVersion(versionId))
            {
                return;
            }

            if (hasUnsavedCardPoolChanges && !CurrentSelection().IsDefault)
            {
                pendingVersionSwitchId = versionId;
                versionSwitchConfirmOpen = true;
                Build();
                return;
            }

            CompleteVersionSwitch(versionId);
        }

        private void CompleteVersionSwitch(string versionId)
        {
            SelectVersion(versionId);
            Build();
        }

        private void SaveAndSwitchVersion()
        {
            var target = pendingVersionSwitchId;
            SaveCurrentVersion();
            CompleteVersionSwitch(target);
        }

        private void DiscardAndSwitchVersion()
        {
            var target = pendingVersionSwitchId;
            hasUnsavedCardPoolChanges = false;
            CompleteVersionSwitch(target);
        }

        private void CancelVersionSwitch()
        {
            pendingVersionSwitchId = null;
            versionSwitchConfirmOpen = false;
            Build();
        }

        private bool IsCurrentVersion(string versionId)
        {
            return string.Equals(selectedVersionId ?? string.Empty, versionId ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private void RenameCurrentVersion(string name)
        {
            var profile = store.Versions.FirstOrDefault(version => string.Equals(version.Id, selectedVersionId, StringComparison.OrdinalIgnoreCase));
            if (profile == null)
            {
                return;
            }

            var trimmed = string.IsNullOrWhiteSpace(name) ? T("自定义版本", "Custom Version") : name.Trim();
            profile.Name = trimmed.Length > 18 ? trimmed.Substring(0, 18) : trimmed;
            profile.UpdatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            store.SelectedVersionId = selectedVersionId;
            repository.Save(store);
        }

        private void CreateVersionFromDefault()
        {
            SelectVersion(null, false);
            CreateProfile(T("新版本", "New Version"));
        }

        private void CopyCurrentVersion()
        {
            CreateProfile(CurrentSelection().VersionName + T(" 副本", " Copy"));
        }

        private void CreateProfile(string name)
        {
            if (store.Versions.Count >= CardPoolVersionFactory.MaxCustomVersions)
            {
                return;
            }

            var profile = CardPoolVersionFactory.CreateProfileFromSelection(
                new CardPoolVersionSelection
                {
                    EnabledMinionCardIds = new HashSet<string>(enabledMinionCardIds, StringComparer.OrdinalIgnoreCase),
                    EnabledTavernSpellCardNumbers = new HashSet<string>(enabledTavernSpellCardNumbers, StringComparer.OrdinalIgnoreCase),
                    EnabledQuestCardIds = new HashSet<string>(enabledQuestCardIds, StringComparer.OrdinalIgnoreCase),
                    EnabledQuestRewardCardIds = new HashSet<string>(enabledQuestRewardCardIds, StringComparer.OrdinalIgnoreCase),
                    EnabledLesserTrinketCardIds = new HashSet<string>(enabledLesserTrinketCardIds, StringComparer.OrdinalIgnoreCase),
                    EnabledGreaterTrinketCardIds = new HashSet<string>(enabledGreaterTrinketCardIds, StringComparer.OrdinalIgnoreCase),
                    EnabledAnomalyCardIds = new HashSet<string>(enabledAnomalyCardIds, StringComparer.OrdinalIgnoreCase)
                },
                Guid.NewGuid().ToString("N"),
                name);
            store.Versions.Add(profile);
            selectedVersionId = profile.Id;
            SaveCurrentVersion();
            Build();
        }

        private void SaveCurrentVersion()
        {
            var profile = store.Versions.FirstOrDefault(version => string.Equals(version.Id, selectedVersionId, StringComparison.OrdinalIgnoreCase));
            if (profile == null)
            {
                return;
            }

            profile.EnabledMinionCardIds = enabledMinionCardIds
                .Where(value => !IsDuoCardId(value))
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
            profile.EnabledTavernSpellCardNumbers = enabledTavernSpellCardNumbers.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
            profile.EnabledQuestCardIds = enabledQuestCardIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
            profile.EnabledQuestRewardCardIds = enabledQuestRewardCardIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
            profile.EnabledLesserTrinketCardIds = enabledLesserTrinketCardIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
            profile.EnabledGreaterTrinketCardIds = enabledGreaterTrinketCardIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
            profile.EnabledAnomalyCardIds = enabledAnomalyCardIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
            profile.UpdatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            store.SelectedVersionId = selectedVersionId;
            repository.Save(store);
            hasUnsavedCardPoolChanges = false;
        }

        private bool SetFilteredEnabled(bool enabled)
        {
            var changed = false;
            if (activeTab == CardPoolTab.Minions)
            {
                foreach (var cardId in FilteredMinions().Select(minion => minion.CardId).Where(value => !string.IsNullOrEmpty(value)))
                {
                    changed |= SetEnabled(enabledMinionCardIds, cardId, enabled);
                }
            }
            else if (activeTab == CardPoolTab.TavernSpells)
            {
                foreach (var cardNumber in FilteredSpells().Select(spell => spell.CardNumber).Where(value => !string.IsNullOrEmpty(value)))
                {
                    changed |= SetEnabled(enabledTavernSpellCardNumbers, cardNumber, enabled);
                }
            }

            if (changed)
            {
                MarkCardPoolDirty();
            }

            return changed;
        }

        private void DeleteCurrentVersion()
        {
            store.Versions.RemoveAll(version => string.Equals(version.Id, selectedVersionId, StringComparison.OrdinalIgnoreCase));
            selectedVersionId = null;
            store.SelectedVersionId = null;
            repository.Save(store);
            SelectVersion(null, false);
            Build();
        }

        private void StartTrainer(List<Tribe> activeTribes)
        {
            SyncAdvancedMechanicFlagsFromPools();
            var selection = CurrentSelection();
            if (!selection.IsDefault)
            {
                SaveCurrentVersion();
            }

            var resolvedSelectedAnomalyCardId = enableAnomalies && enabledAnomalyCardIds.Count == 1
                ? enabledAnomalyCardIds.First()
                : null;
            var shouldRandomizeAnomaly = enableAnomalies && string.IsNullOrEmpty(resolvedSelectedAnomalyCardId);

            start?.Invoke(new MatchSetupOptions
            {
                UseEnglish = UseEnglish,
                ActiveTribes = activeTribes == null ? new List<Tribe>() : activeTribes.ToList(),
                SelectedHeroCardId = selectedHeroCardId,
                CardPoolVersionId = selection.VersionId,
                CardPoolVersionName = selection.VersionName,
                IsDefaultCardPoolVersion = selection.IsDefault,
                EnableQuests = enableQuests,
                EnableTrinkets = enableTrinkets,
                EnableQuestRewards = enableQuestRewards,
                EnableAnomalies = enableAnomalies,
                RandomizeAnomaly = shouldRandomizeAnomaly,
                SelectedAnomalyCardId = resolvedSelectedAnomalyCardId,
                AnomalyPoolVersion = anomalyPoolVersion,
                ShowProxySafe = showProxySafe,
                ShowDebugOnly = showDebugOnly,
                ShowHiddenEffectOnly = showHiddenEffectOnly,
                ShowDisabled = showDebugOnly && showDisabled,
                EnablePlayerDirectedChoices = enablePlayerDirectedChoices,
                EnableTimewarpedTavern = enableTimewarpedTavern,
                TimewarpedPoolVersion = timewarpedPoolVersion,
                UseHistoricalTimewarpedPool = timewarpedPoolVersion != TimewarpedPoolVersion.Current,
                UseExplicitTimewarpedPool = true,
                EnabledTimewarpedCardIds = enabledTimewarpedCardIds.ToList(),
                EnabledMinionCardIds = enabledMinionCardIds.Where(value => !IsDuoCardId(value)).ToList(),
                EnabledTavernSpellCardNumbers = enabledTavernSpellCardNumbers.ToList(),
                EnabledQuestCardIds = enabledQuestCardIds.ToList(),
                EnabledQuestRewardCardIds = enabledQuestRewardCardIds.ToList(),
                EnabledLesserTrinketCardIds = enabledLesserTrinketCardIds.ToList(),
                EnabledGreaterTrinketCardIds = enabledGreaterTrinketCardIds.ToList(),
                EnabledAnomalyCardIds = enabledAnomalyCardIds.ToList()
            });
        }

        private HeroDefinition CurrentHero()
        {
            if (heroCatalog == null || string.IsNullOrEmpty(selectedHeroCardId))
            {
                return null;
            }

            return heroCatalog.AllHeroes.FirstOrDefault(hero => string.Equals(hero.HeroCardId, selectedHeroCardId, StringComparison.OrdinalIgnoreCase));
        }

        private string HeroName(HeroDefinition hero)
        {
            if (!UseEnglish && !string.IsNullOrEmpty(hero?.ZhName))
            {
                return hero.ZhName;
            }

            return hero?.Name ?? string.Empty;
        }

        private string HeroPowerName(HeroPowerDefinition power)
        {
            if (!UseEnglish && !string.IsNullOrEmpty(power?.ZhName))
            {
                return power.ZhName;
            }

            return power?.Name ?? string.Empty;
        }

        private bool IsAnomalySelectable(AnomalyDefinition definition)
        {
            if (definition == null ||
                string.IsNullOrEmpty(definition.CardId) ||
                definition.SourcePools == null ||
                !definition.SourcePools.Contains(anomalyPoolVersion))
            {
                return false;
            }

            if (definition.ImplementationStatus != AnomalyImplementationStatus.Implemented &&
                definition.ImplementationStatus != AnomalyImplementationStatus.OfferableWithExactProxy)
            {
                return false;
            }

            return definition.AvailabilityReasons == null ||
                definition.AvailabilityReasons.All(IsAnomalyAvailabilitySatisfied);
        }

        private bool IsAnomalyAvailabilitySatisfied(AnomalyAvailabilityReason reason)
        {
            switch (reason)
            {
                case AnomalyAvailabilityReason.None:
                case AnomalyAvailabilityReason.RequiresTimewarpPool:
                case AnomalyAvailabilityReason.RequiresDarkmoonPrizeBackend:
                    return true;
                case AnomalyAvailabilityReason.RequiresBuddyMode:
                    return heroCatalog != null &&
                        heroCatalog.AllHeroes.Any(hero => hero.Buddy != null && !hero.Buddy.ExcludedFromBuddyDiscover);
                case AnomalyAvailabilityReason.RequiresTier7Pool:
                    return minionCatalog != null &&
                        minionCatalog.All.Any(minion => minion.InPool && minion.TavernTier == 7);
                default:
                    return false;
            }
        }

        private HeroDefinition ResolveDefaultHero()
        {
            if (heroCatalog == null || heroCatalog.AllHeroes.Count == 0)
            {
                return null;
            }

            return heroCatalog.AllHeroes.FirstOrDefault(hero => hero.Name == "Patchwerk")
                ?? heroCatalog.GetInitialSelectableHeroes().FirstOrDefault();
        }

        private void BuildHeroIcon(Transform parent, string name, HeroDefinition hero, float size)
        {
            var frame = UiFactory.Panel(name, parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetFixedSize(frame, size, size);
            UnityTavernUiStyle.ConfigureOutline(frame, new Color(0f, 0f, 0f, 0.25f), new Vector2(1f, -1f));

            var sprite = hero == null ? null : CardImageProvider.LoadSprite(hero.ImagePath, hero.HeroCardId, CardKind.Hero);
            if (sprite == null)
            {
                var missing = UiFactory.Label(name + "Missing", frame.transform, T("无图", "No art"), 14, FontStyle.Bold);
                missing.alignment = TextAnchor.MiddleCenter;
                missing.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.Stretch(missing.rectTransform);
                return;
            }

            var imageObject = new GameObject(name + "Sprite", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(frame.transform, false);
            UnityTavernUiStyle.Stretch(imageObject.GetComponent<RectTransform>());
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
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

        private static bool Contains(string source, string query)
        {
            return !string.IsNullOrEmpty(source) &&
                source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SwitchCardPoolTab(CardPoolTab tab)
        {
            if (activeTab == tab)
            {
                return;
            }

            activeTab = tab;
            ResetVisibleCardPoolItems();
            Build();
        }

        private void ResetVisibleCardPoolItems()
        {
            visibleCardPoolItemCount = CardPoolLoadStep;
            keepVersionListAtBottom = false;
        }

        private void MarkCardPoolDirty()
        {
            var selection = CurrentSelection();
            if (!selection.IsDefault)
            {
                hasUnsavedCardPoolChanges = true;
            }
        }

        private static bool SetEnabled(HashSet<string> target, string value, bool enabled)
        {
            if (enabled)
            {
                return target.Add(value);
            }
            else
            {
                return target.Remove(value);
            }
        }

        private static bool IsDuoMinion(MinionDefinition minion)
        {
            return minion != null && (IsDuoCardId(minion.CardId) || IsDuoCardId(minion.Id));
        }

        private static bool IsDuoCardId(string value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith("BGDUO", StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesMinionTribe(MinionDefinition minion, Tribe tribe)
        {
            if (tribe == Tribe.All)
            {
                return true;
            }

            if (tribe == Tribe.None)
            {
                return minion == null || minion.Tribes == null || minion.Tribes.Count == 0 || minion.Tribes.All(value => value == Tribe.None);
            }

            return minion != null && minion.Tribes != null && (minion.Tribes.Contains(tribe) || minion.Tribes.Contains(Tribe.All));
        }

        private static bool MatchesSpellTribe(TavernSpellDefinition spell, Tribe tribe)
        {
            if (tribe == Tribe.All)
            {
                return true;
            }

            var tribes = TribeAvailabilityRules.SpellTribes(spell);
            if (tribe == Tribe.None)
            {
                return tribes.Count == 0 || tribes.All(value => value == Tribe.None);
            }

            return tribes.Contains(tribe);
        }

        private string TribeListText(IEnumerable<Tribe> tribes)
        {
            var names = (tribes ?? Enumerable.Empty<Tribe>())
                .Where(tribe => tribe != Tribe.None)
                .Select(TribeName)
                .Distinct()
                .ToArray();
            return names.Length == 0 ? TribeName(Tribe.None) : string.Join("/", names);
        }

        private string SpellTribesText(TavernSpellDefinition spell)
        {
            var tribes = TribeAvailabilityRules.SpellTribes(spell);
            return tribes.Count == 0 ? T("通用法术", "General Spell") : string.Join("/", tribes.Select(TribeName).ToArray());
        }

        private static Button FilterButton(string name, Transform parent, string text, bool active, Color accentColor, Action onClick)
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

        private static Button ActionButton(string name, Transform parent, string text, bool interactable, Action onClick)
        {
            var button = UiFactory.Button(name, parent, text, () => onClick?.Invoke());
            button.interactable = interactable;
            UnityTavernUiStyle.SetPreferredHeight(button.gameObject, UnityTavernUiStyle.TouchHeight);
            UnityTavernUiStyle.ConfigureButton(button, UnityTavernUiStyle.Brass);
            return button;
        }

        private static void ConfigureButtonRow(GameObject row, int padding, int spacing)
        {
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(padding, padding, padding, padding);
            rowLayout.spacing = spacing;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
        }

        private string ShortLabel(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return T("未命名", "Unnamed");
            }

            return value.Length > 6 ? value.Substring(0, 6) : value;
        }

        private static string SafeCardName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Unknown";
            }

            return value.Replace(' ', '_').Replace('/', '_').Replace('\\', '_').Replace(':', '_');
        }

        private string VersionNameFor(string versionId)
        {
            if (string.IsNullOrEmpty(versionId))
            {
                return CardPoolVersionFactory.DefaultVersionName;
            }

            var profile = store.Versions.FirstOrDefault(version => string.Equals(version.Id, versionId, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(profile?.Name) ? T("自定义版本", "Custom Version") : profile.Name;
        }

        private string AnomalyChoiceSummaryText()
        {
            if (enabledAnomalyCardIds.Count == 0)
            {
                return T("畸变池：关闭", "Anomaly Pool: Off");
            }

            if (enabledAnomalyCardIds.Count > 1)
            {
                return T("畸变池：随机池", "Anomaly Pool: Random pool");
            }

            var cardId = enabledAnomalyCardIds.FirstOrDefault();
            var anomaly = anomalyCatalog != null && !string.IsNullOrEmpty(cardId) && anomalyCatalog.TryGetByCardId(cardId, out var definition)
                ? definition
                : null;
            return T("畸变池：固定 ", "Anomaly Pool: Fixed ") + ShortLabel(string.IsNullOrEmpty(anomaly?.Name) ? cardId : anomaly.Name);
        }

        private string AdvancedMechanicsSummaryText()
        {
            var enabled = new List<string>();
            if (showDebugOnly)
            {
                enabled.Add(T("调试池", "Debug Pool"));
            }

            if (showHiddenEffectOnly)
            {
                enabled.Add(T("隐藏效果池", "Hidden Effects"));
            }

            if (showDisabled)
            {
                enabled.Add(T("含禁用项", "Disabled Included"));
            }

            if (!showProxySafe)
            {
                enabled.Add(T("代理实现关闭", "Proxy-safe Off"));
            }

            if (!enablePlayerDirectedChoices)
            {
                enabled.Add(T("自由选择关闭", "Free Choice Off"));
            }

            if (QuestPoolsEnabled())
            {
                enabled.Add(T("任务/奖励池", "Quest / Reward Pool"));
            }

            if (TrinketPoolsEnabled())
            {
                enabled.Add(T("饰品池", "Trinket Pool"));
            }

            if (AnomalyPoolEnabled())
            {
                enabled.Add(enabledAnomalyCardIds.Count == 1
                    ? T("畸变池:固定", "Anomaly Pool: fixed")
                    : T("畸变池:随机池", "Anomaly Pool: random pool"));
            }

            return enabled.Count == 0 ? T("本局关闭", "All off") : string.Join(" / ", enabled.ToArray());
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

        private string TribeName(Tribe tribe)
        {
            switch (tribe)
            {
                case Tribe.Beast: return T("野兽", "Beast");
                case Tribe.Murloc: return T("鱼人", "Murloc");
                case Tribe.Mech: return T("机械", "Mech");
                case Tribe.Demon: return T("恶魔", "Demon");
                case Tribe.Dragon: return T("龙", "Dragon");
                case Tribe.Pirate: return T("海盗", "Pirate");
                case Tribe.Elemental: return T("元素", "Elemental");
                case Tribe.Quilboar: return T("野猪人", "Quilboar");
                case Tribe.Undead: return T("亡灵", "Undead");
                case Tribe.Naga: return T("纳迦", "Naga");
                default: return T("中立", "Neutral");
            }
        }
    }
}
