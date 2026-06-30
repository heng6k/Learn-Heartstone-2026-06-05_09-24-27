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
            TavernSpells
        }

        private readonly Transform root;
        private readonly Action<MatchSetupOptions> start;
        private readonly Action backToHub;
        private readonly UnityTavernLayoutContext layout;
        private readonly ICardPoolVersionRepository repository;
        private readonly MinionCatalog minionCatalog;
        private readonly SpellCatalog spellCatalog;
        private readonly HeroCatalog heroCatalog;
        private readonly AnomalyCatalog anomalyCatalog;
        private readonly HashSet<Tribe> selected = new HashSet<Tribe>();
        private readonly HashSet<string> enabledMinionCardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> enabledTavernSpellCardNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Transform cardPoolListContent;
        private CardPoolVersionStore store;
        private string selectedVersionId;
        private CardPoolTab activeTab = CardPoolTab.Minions;
        private string searchText = string.Empty;
        private int versionTierFilter;
        private Tribe versionTribeFilter = Tribe.All;
        private int visibleCardPoolItemCount = CardPoolLoadStep;
        private bool keepVersionListAtBottom;
        private bool versionModalOpen;
        private bool heroSelectionOpen;
        private bool anomalySelectionOpen;
        private bool hasUnsavedCardPoolChanges;
        private bool versionSwitchConfirmOpen;
        private string pendingVersionSwitchId;
        private string selectedHeroCardId;
        private bool enableQuests = true;
        private bool enableTrinkets = true;
        private bool enableQuestRewards = true;
        private bool enableAnomalies;
        private bool randomizeAnomaly = true;
        private string selectedAnomalyCardId;
        private bool showProxySafe = true;
        private bool showDebugOnly;
        private bool showHiddenEffectOnly;
        private bool showDisabled;
        private GameObject shell;

        public UnityTavernTribeSelectionView(
            Transform root,
            Action<List<Tribe>> start,
            Action backToHub,
            UnityTavernLayoutContext? layoutContext = null)
            : this(
                root,
                setup => start?.Invoke(setup?.ActiveTribes ?? new List<Tribe>()),
                backToHub,
                layoutContext,
                null,
                null,
                null,
                null)
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
            AnomalyCatalog anomalyCatalog = null)
        {
            this.root = root;
            this.start = start;
            this.backToHub = backToHub;
            layout = layoutContext ?? UnityTavernLayoutContext.FromRoot(root);
            this.repository = repository ?? new JsonCardPoolVersionRepository();
            this.minionCatalog = minionCatalog ?? MinionCatalogLoader.LoadFromResources();
            this.spellCatalog = spellCatalog ?? SpellCatalogLoader.LoadFromResources();
            this.heroCatalog = heroCatalog ?? HeroCatalogLoader.LoadFromResources();
            this.anomalyCatalog = anomalyCatalog ?? AnomalyCatalogLoader.LoadFromResources();
            selectedHeroCardId = ResolveDefaultHero()?.HeroCardId;
            store = CardPoolVersionFactory.NormalizeStore(this.repository.Load());
            SelectVersion(store.SelectedVersionId, false);
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
            UnityTavernUiStyle.ConfigureOutline(page, new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.42f), new Vector2(2f, -2f));

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
            BuildAdvancedMechanicsStrip(left.transform);
            BuildQuickActions(left.transform);
            if (heroSelectionOpen)
            {
                BuildHeroSelectionOverlay();
            }

            if (versionModalOpen)
            {
                BuildVersionEditorOverlay();
            }

            if (anomalySelectionOpen)
            {
                BuildAnomalySelectionOverlay();
            }
        }

        private void BuildHeader(Transform parent)
        {
            var header = UiFactory.Panel("UnityTribeSelectionHeader", parent, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(header, layout.IsCompact ? 84f : 104f);
            var headerLayout = header.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(12, 12, 8, 8);
            headerLayout.spacing = 6;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            var title = UiFactory.Label("UnityTribeSelectionTitle", header.transform, "选择本局种族", layout.IsCompact ? 20 : 26, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 30f : 40f);

            var count = selected.Count + "/5";
            var names = selected.Count == 0 ? "尚未选择" : string.Join(" / ", TribeAvailabilityRules.PlayableTribes.Where(selected.Contains).Select(TribeName).ToArray());
            var summary = UiFactory.Label("UnityTribeSelectionSummary", header.transform, "已选 " + count + "  " + names, layout.IsCompact ? 13 : 15, FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleCenter;
            summary.color = selected.Count == 5 ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, layout.IsCompact ? 28f : 34f);
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

            var label = UiFactory.Label(buttonObject.name + "Text", buttonObject.transform, TribeName(tribe), layout.IsCompact ? 15 : 17, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private void BuildQuickActions(Transform parent)
        {
            var row = UiFactory.Panel("UnityTribeSelectionActions", parent, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 56f : 62f);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 8, 7, 7);
            rowLayout.spacing = 8;
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
            ActionButton("UnityTribeSelectionAllButton", row.transform, "全部10个种族", true, () => StartTrainer(TribeAvailabilityRules.AllPlayableTribes()));
            ActionButton("UnityTribeSelectionEnterButton", row.transform, "进入酒馆", selected.Count == 5, () => StartTrainer(TribeAvailabilityRules.PlayableTribes.Where(selected.Contains).ToList()));
        }

        private void BuildAdvancedMechanicsStrip(Transform parent)
        {
            var strip = UiFactory.Panel("UnityAdvancedMechanicsSetupPanel", parent, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(strip, layout.IsCompact ? 170f : 118f);
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

            var title = UiFactory.Label("UnityAdvancedMechanicsSetupTitle", titleBlock.transform, "高级机制", layout.IsCompact ? 15 : 17, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 24f : 28f);

            var summary = UiFactory.Label("UnityAdvancedMechanicsSetupSummary", titleBlock.transform, AdvancedMechanicsSummaryText(), layout.IsCompact ? 10 : 11, FontStyle.Bold);
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
            grid.cellSize = layout.IsCompact ? new Vector2(150f, 28f) : new Vector2(138f, 30f);

            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-EnableQuests", "任务", enableQuests, true, value =>
            {
                enableQuests = value;
                Build();
            });
            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-EnableTrinkets", "饰品", enableTrinkets, true, value =>
            {
                enableTrinkets = value;
                Build();
            });
            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-EnableQuestRewards", "任务奖励", enableQuestRewards, true, value =>
            {
                enableQuestRewards = value;
                Build();
            });
            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-EnableAnomalies", "畸变", enableAnomalies, true, value =>
            {
                enableAnomalies = value;
                if (enableAnomalies && string.IsNullOrEmpty(selectedAnomalyCardId))
                {
                    randomizeAnomaly = true;
                }

                Build();
            });
            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-ShowProxySafe", "代理实现", showProxySafe, true, value =>
            {
                showProxySafe = value;
                Build();
            });
            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-ShowDebugOnly", "调试池", showDebugOnly, true, value =>
            {
                showDebugOnly = value;
                if (!showDebugOnly)
                {
                    showDisabled = false;
                }

                Build();
            });
            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-ShowHiddenEffectOnly", "隐藏效果池", showHiddenEffectOnly, true, value =>
            {
                showHiddenEffectOnly = value;
                Build();
            });
            BuildSetupToggle(gridObject.transform, "UnityAdvancedMechanicsToggle-ShowDisabled", "禁用池", showDisabled, showDebugOnly, value =>
            {
                showDisabled = showDebugOnly && value;
                Build();
            });

            BuildAnomalySetupControls(strip.transform);
        }

        private void BuildAnomalySetupControls(Transform parent)
        {
            var panel = UiFactory.Panel("UnityAnomalySetupPanel", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetFixedSize(panel, layout.IsCompact ? 180f : 226f, layout.IsCompact ? 148f : 96f);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                enableAnomalies ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.42f) : new Color(0f, 0f, 0f, 0.18f),
                new Vector2(1f, -1f));

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(7, 7, 6, 7);
            panelLayout.spacing = 6;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var label = UiFactory.Label("UnityAnomalySetupChoiceLabel", panel.transform, AnomalyChoiceSummaryText(), layout.IsCompact ? 10 : 11, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = enableAnomalies ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.MutedText;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, layout.IsCompact ? 44f : 24f);

            var row = UiFactory.Panel("UnityAnomalySetupButtons", panel.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 76f : 38f);
            if (layout.IsCompact)
            {
                var vertical = row.AddComponent<VerticalLayoutGroup>();
                vertical.spacing = 5;
                vertical.childControlWidth = true;
                vertical.childControlHeight = true;
                vertical.childForceExpandWidth = true;
                vertical.childForceExpandHeight = true;
            }
            else
            {
                ConfigureButtonRow(row, 0, 6);
            }

            var randomButton = ActionButton("UnityAnomalyRandomButton", row.transform, "随机", true, () =>
            {
                enableAnomalies = true;
                randomizeAnomaly = true;
                selectedAnomalyCardId = null;
                Build();
            });
            if (enableAnomalies && randomizeAnomaly)
            {
                UnityTavernUiStyle.EnsureComponent<Image>(randomButton.gameObject).color = Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.28f);
            }

            var selectButton = ActionButton("UnityAnomalySelectButton", row.transform, "选择", true, () =>
            {
                enableAnomalies = true;
                heroSelectionOpen = false;
                versionModalOpen = false;
                anomalySelectionOpen = true;
                Build();
            });
            if (enableAnomalies && !randomizeAnomaly && !string.IsNullOrEmpty(selectedAnomalyCardId))
            {
                UnityTavernUiStyle.EnsureComponent<Image>(selectButton.gameObject).color = Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.28f);
            }
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

            var title = UiFactory.Label("UnityCardPoolVersionTitle", titleBlock.transform, "卡池版本", layout.IsCompact ? 16 : 18, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 24f : 28f);

            var summary = UiFactory.Label("UnityCardPoolVersionSummary", titleBlock.transform, VersionSummaryText(selection), layout.IsCompact ? 12 : 13, FontStyle.Bold);
            summary.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, layout.IsCompact ? 20f : 22f);

            var open = ActionButton("UnityCardPoolVersionOpenButton", strip.transform, "编辑卡池", true, () =>
            {
                versionModalOpen = true;
                Build();
            });
            UnityTavernUiStyle.SetFixedSize(open.gameObject, layout.IsCompact ? 108f : 128f, layout.IsCompact ? 42f : 46f);
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

            var title = UiFactory.Label("UnityTribeSelectionHeroName", textBlock.transform, hero == null ? "未设置英雄" : hero.Name, layout.IsCompact ? 15 : 17, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 22f : 25f);

            var power = hero?.HeroPower;
            var powerText = power == null ? "技能：未设置" : "技能：" + power.Name + " / 费用 " + power.Cost;
            var detail = UiFactory.Label("UnityTribeSelectionHeroPower", textBlock.transform, powerText, layout.IsCompact ? 11 : 12, FontStyle.Bold);
            detail.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(detail.gameObject, layout.IsCompact ? 18f : 21f);

            if (!layout.IsCompact)
            {
                var stats = UiFactory.Label("UnityTribeSelectionHeroStats", textBlock.transform, hero == null ? "进入酒馆时由对局兜底" : "生命 " + hero.Health + " / 护甲 " + hero.Armor, 11, FontStyle.Normal);
                stats.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(stats.gameObject, 18f);
            }

            var choose = ActionButton("UnityTribeSelectionChooseHeroButton", strip.transform, layout.IsCompact ? "选择" : "选择英雄", true, () =>
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
                "选择英雄");
        }

        private void BuildAnomalySelectionOverlay()
        {
            var overlay = UiFactory.Panel("UnityAnomalySelectionOverlay", shell.transform, new Color(0f, 0f, 0f, 0.62f));
            overlay.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;

            var panel = UiFactory.Panel("UnityAnomalySelectionPanel", overlay.transform, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.ConfigureOutline(panel, new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.50f), new Vector2(2f, -2f));
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = layout.IsCompact ? new Vector2(0.06f, 0.06f) : new Vector2(0.16f, 0.10f);
            rect.anchorMax = layout.IsCompact ? new Vector2(0.94f, 0.94f) : new Vector2(0.84f, 0.90f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(14, 14, 12, 14);
            panelLayout.spacing = 10;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var anomalies = SelectableAnomalies().ToList();
            var header = UiFactory.Panel("UnityAnomalySelectionHeader", panel.transform, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(header, layout.IsCompact ? 58f : 64f);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 10, 8, 8);
            headerLayout.spacing = 8;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            var title = UiFactory.Label("UnityAnomalySelectionTitle", header.transform, "选择畸变", layout.IsCompact ? 18 : 22, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleLeft;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

            var count = UiFactory.Label("UnityAnomalySelectionCount", header.transform, anomalies.Count + " 个可用", 12, FontStyle.Bold);
            count.alignment = TextAnchor.MiddleRight;
            count.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(count.gameObject, layout.IsCompact ? 86f : 112f, 32f);

            var close = ActionButton("UnityAnomalySelectionCloseButton", header.transform, "关闭", true, () =>
            {
                anomalySelectionOpen = false;
                Build();
            });
            UnityTavernUiStyle.SetFixedSize(close.gameObject, 72f, 36f);

            var content = UiFactory.ScrollView("UnityAnomalySelectionScroll", panel.transform, UnityTavernUiStyle.PanelQuiet, out _);
            var listLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(8, 12, 8, 8);
            listLayout.spacing = 6;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            BuildAnomalyOptionButton(
                content,
                "UnityAnomalySelectionRandomButton",
                "随机畸变",
                "从当前默认可用畸变池中随机选择。",
                enableAnomalies && randomizeAnomaly,
                () =>
                {
                    enableAnomalies = true;
                    randomizeAnomaly = true;
                    selectedAnomalyCardId = null;
                    anomalySelectionOpen = false;
                    Build();
                });

            foreach (var anomaly in anomalies)
            {
                var captured = anomaly;
                BuildAnomalyOptionButton(
                    content,
                    "UnityAnomalySelectionButton-" + anomaly.CardId,
                    string.IsNullOrEmpty(anomaly.Name) ? anomaly.CardId : anomaly.Name,
                    AnomalyDetailText(anomaly),
                    enableAnomalies &&
                        !randomizeAnomaly &&
                        string.Equals(selectedAnomalyCardId, anomaly.CardId, StringComparison.OrdinalIgnoreCase),
                    () =>
                    {
                        enableAnomalies = true;
                        randomizeAnomaly = false;
                        selectedAnomalyCardId = captured.CardId;
                        anomalySelectionOpen = false;
                        Build();
                    });
            }

            if (anomalies.Count == 0)
            {
                var empty = UiFactory.Label("UnityAnomalySelectionEmpty", content, "当前没有可用畸变", 14, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 46f);
            }
        }

        private void BuildAnomalyOptionButton(Transform parent, string name, string titleText, string detailText, bool selectedOption, Action onClick)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 74f : 82f);
            var image = row.GetComponent<Image>();
            image.color = selectedOption
                ? Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.26f)
                : UnityTavernUiStyle.Panel;
            UnityTavernUiStyle.ConfigureOutline(
                row,
                selectedOption ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.58f) : new Color(0f, 0f, 0f, 0.16f),
                new Vector2(1f, -1f));

            var button = row.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            UnityTavernUiStyle.TintSelectable(button, image.color, Color.Lerp(image.color, UnityTavernUiStyle.Gold, 0.18f), Color.Lerp(image.color, Color.black, 0.16f));

            var rowLayout = row.AddComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset(10, 10, 7, 7);
            rowLayout.spacing = 3;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            var title = UiFactory.Label(name + "Title", row.transform, titleText, layout.IsCompact ? 13 : 15, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleLeft;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, layout.IsCompact ? 24f : 28f);

            var detail = UiFactory.Label(name + "Detail", row.transform, detailText, layout.IsCompact ? 10 : 11, FontStyle.Normal);
            detail.alignment = TextAnchor.MiddleLeft;
            detail.color = UnityTavernUiStyle.MutedText;
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetFlexible(detail.gameObject, 1f, 0f);
        }

        private void BuildVersionEditorOverlay()
        {
            var overlay = UiFactory.Panel("UnityCardPoolVersionOverlay", shell.transform, new Color(0f, 0f, 0f, 0.62f));
            overlay.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());
            UnityTavernUiStyle.EnsureComponent<Image>(overlay).raycastTarget = true;

            var panel = UiFactory.Panel("UnityCardPoolVersionModalPanel", overlay.transform, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.ConfigureOutline(panel, new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.50f), new Vector2(2f, -2f));
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
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 10, 8, 8);
            headerLayout.spacing = 8;
            headerLayout.childControlWidth = false;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            var minionTab = ActionButton("UnityCardPoolVersionMinionTab", header.transform, "随从", true, () =>
            {
                SwitchCardPoolTab(CardPoolTab.Minions);
            });
            UnityTavernUiStyle.SetFixedSize(minionTab.gameObject, 72f, 42f);

            var spellTab = ActionButton("UnityCardPoolVersionSpellTab", header.transform, "法术", true, () =>
            {
                SwitchCardPoolTab(CardPoolTab.TavernSpells);
            });
            UnityTavernUiStyle.SetFixedSize(spellTab.gameObject, 72f, 42f);

            var title = UiFactory.Label("UnityCardPoolVersionModalTitle", header.transform, "卡池版本", layout.IsCompact ? 18 : 22, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

            var summary = UiFactory.Label("UnityCardPoolVersionModalSummary", header.transform, VersionSummaryText(selection), 12, FontStyle.Bold);
            summary.alignment = TextAnchor.MiddleRight;
            summary.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(summary.gameObject, layout.IsCompact ? 220f : 280f, 32f);

            var close = ActionButton("UnityCardPoolVersionCloseButton", header.transform, "关闭", true, () =>
            {
                versionModalOpen = false;
                Build();
            });
            UnityTavernUiStyle.SetFixedSize(close.gameObject, 72f, 36f);
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

            var title = UiFactory.Label("UnityCardPoolVersionUnsavedTitle", dialog.transform, "有未保存的卡池改动", layout.IsCompact ? 18 : 20, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 30f);

            var body = UiFactory.Label(
                "UnityCardPoolVersionUnsavedBody",
                dialog.transform,
                "切换到“" + VersionNameFor(pendingVersionSwitchId) + "”前，请选择如何处理当前版本的勾选改动。",
                14,
                FontStyle.Bold);
            body.alignment = TextAnchor.MiddleCenter;
            body.color = UnityTavernUiStyle.Text;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetPreferredHeight(body.gameObject, 52f);

            var row = UiFactory.Panel("UnityCardPoolVersionUnsavedActions", dialog.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, 42f);
            ConfigureButtonRow(row, 0, 8);
            ActionButton("UnityCardPoolVersionConfirmSaveAndSwitchButton", row.transform, "保存并切换", true, SaveAndSwitchVersion);
            ActionButton("UnityCardPoolVersionConfirmDiscardButton", row.transform, "放弃修改", true, DiscardAndSwitchVersion);
            ActionButton("UnityCardPoolVersionConfirmCancelButton", row.transform, "取消", true, CancelVersionSwitch);
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

            var title = UiFactory.Label("UnityCardPoolVersionSideTitle", side.transform, "版本", 16, FontStyle.Bold);
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
            var savedState = hasUnsavedCardPoolChanges ? "  未保存" : string.Empty;
            return selection.VersionName + "  " + (selection.IsDefault ? "默认" : "自定义") + "  随从 " + enabledMinionCardIds.Count + " / 法术 " + enabledTavernSpellCardNumbers.Count + savedState;
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

            var label = UiFactory.Label("UnityCardPoolVersionNameLabel", header.transform, "版本名称", 12, FontStyle.Bold);
            label.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.EnsureComponent<LayoutElement>(label.gameObject).flexibleWidth = 1f;

            var hint = UiFactory.Label(
                "UnityCardPoolVersionNameHint",
                header.transform,
                selection.IsDefault ? "默认只读" : "点击改名",
                11,
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
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            input.textComponent.color = selection.IsDefault ? UnityTavernUiStyle.MutedText : UnityTavernUiStyle.Text;
            input.textComponent.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.textComponent.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.placeholder = UiFactory.Label(
                "UnityCardPoolVersionNamePlaceholder",
                inputObject.transform,
                selection.IsDefault ? "默认版本不可改名" : "输入版本名称",
                14);
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
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 86f : 92f);
            var rowLayout = row.AddComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset(6, 6, 6, 6);
            rowLayout.spacing = 5;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            var top = UiFactory.Panel("UnityCardPoolVersionPickerTop", row.transform, UnityTavernUiStyle.PanelQuiet);
            ConfigureButtonRow(top, 0, 5);
            ActionButton("UnityCardPoolVersionDefaultButton", top.transform, "默认", true, () =>
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
            UnityTavernUiStyle.SetPreferredHeight(row, 46f);
            ConfigureButtonRow(row, 6, 6);

            var canCreate = store.Versions.Count < CardPoolVersionFactory.MaxCustomVersions;
            ActionButton("UnityCardPoolVersionNewButton", row.transform, "新建", canCreate, () => CreateVersionFromDefault());
            ActionButton("UnityCardPoolVersionCopyButton", row.transform, "复制", canCreate, () => CopyCurrentVersion());
            var save = ActionButton("UnityCardPoolVersionSaveButton", row.transform, hasUnsavedCardPoolChanges ? "保存*" : "保存", !selection.IsDefault && hasUnsavedCardPoolChanges, () =>
            {
                SaveCurrentVersion();
                Build();
            });
            if (hasUnsavedCardPoolChanges && !selection.IsDefault)
            {
                UnityTavernUiStyle.EnsureComponent<Image>(save.gameObject).color = Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.35f);
            }

            ActionButton("UnityCardPoolVersionDeleteButton", row.transform, "删除", !selection.IsDefault, () => DeleteCurrentVersion());
        }

        private void BuildSearch(Transform parent)
        {
            var inputObject = new GameObject("UnityCardPoolVersionSearchInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetPreferredHeight(inputObject, 38f);
            inputObject.GetComponent<Image>().color = UnityTavernUiStyle.PanelQuiet;

            var input = inputObject.GetComponent<InputField>();
            input.textComponent = UiFactory.Label("UnityCardPoolVersionSearchText", inputObject.transform, string.Empty, 14);
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            input.textComponent.rectTransform.offsetMin = new Vector2(10f, 0f);
            input.textComponent.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.placeholder = UiFactory.Label("UnityCardPoolVersionSearchPlaceholder", inputObject.transform, "搜索名称或编号", 14);
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
        }

        private void BuildVersionFilters(Transform parent, CardPoolVersionSelection selection)
        {
            var filteredCount = activeTab == CardPoolTab.Minions
                ? FilteredMinions().Count()
                : FilteredSpells().Count();
            var panel = UiFactory.Panel("UnityCardPoolVersionFilters", parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(panel, layout.IsCompact ? 138f : 146f);
            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(8, 8, 7, 7);
            panelLayout.spacing = 6;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var tierRow = UiFactory.Panel("UnityCardPoolVersionTierFilters", panel.transform, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(tierRow, 34f);
            ConfigureButtonRow(tierRow, 0, 5);
            FilterButton("UnityCardPoolVersionTierAllButton", tierRow.transform, "全部", versionTierFilter == 0, UnityTavernUiStyle.Gold, () =>
            {
                versionTierFilter = 0;
                ResetVisibleCardPoolItems();
                Build();
            });
            for (var tier = 1; tier <= 7; tier += 1)
            {
                var capturedTier = tier;
                FilterButton("UnityCardPoolVersionTier" + tier + "Button", tierRow.transform, tier + "本", versionTierFilter == tier, UnityTavernUiStyle.Gold, () =>
                {
                    versionTierFilter = capturedTier;
                    ResetVisibleCardPoolItems();
                    Build();
                });
            }

            var typeRow = UiFactory.Panel("UnityCardPoolVersionTypeFilters", panel.transform, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(typeRow, 64f);
            var grid = typeRow.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.spacing = new Vector2(5f, 5f);
            grid.cellSize = new Vector2(layout.IsCompact ? 62f : 70f, 28f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = layout.IsCompact ? 5 : 6;

            FilterButton("UnityCardPoolVersionTribeAllButton", typeRow.transform, "全部", versionTribeFilter == Tribe.All, UnityTavernUiStyle.Green, () =>
            {
                versionTribeFilter = Tribe.All;
                ResetVisibleCardPoolItems();
                Build();
            });
            FilterButton("UnityCardPoolVersionTribeNoneButton", typeRow.transform, activeTab == CardPoolTab.TavernSpells ? "通用" : "中立", versionTribeFilter == Tribe.None, UnityTavernUiStyle.Blue, () =>
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

            var count = UiFactory.Label("UnityCardPoolVersionFilterCount", panel.transform, "当前筛选 " + filteredCount + " 张" + (selection.IsDefault ? "  默认版本只读" : string.Empty), 12, FontStyle.Bold);
            count.alignment = TextAnchor.MiddleRight;
            count.color = selection.IsDefault ? UnityTavernUiStyle.MutedText : UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(count.gameObject, 18f);
        }

        private void BuildVersionBulkActions(Transform parent, CardPoolVersionSelection selection)
        {
            var panel = UiFactory.Panel("UnityCardPoolVersionBulkActions", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetPreferredHeight(panel, 64f);
            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(4, 4, 3, 4);
            panelLayout.spacing = 3;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var filteredCount = activeTab == CardPoolTab.Minions
                ? FilteredMinions().Count()
                : FilteredSpells().Count();
            var visibleCount = Math.Min(visibleCardPoolItemCount, filteredCount);
            var hint = UiFactory.Label("UnityCardPoolVersionBulkHint", panel.transform, "批量操作会影响当前筛选的全部 " + filteredCount + " 张；列表已显示 " + visibleCount + " 张。", 11, FontStyle.Bold);
            hint.alignment = TextAnchor.MiddleLeft;
            hint.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(hint.gameObject, 18f);

            var row = UiFactory.Panel("UnityCardPoolVersionBulkActionButtons", panel.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, 36f);
            ConfigureButtonRow(row, 4, 8);

            ActionButton("UnityCardPoolVersionExcludeFilteredButton", row.transform, "剔除当前筛选", !selection.IsDefault, () =>
            {
                SetFilteredEnabled(false);
                Build();
            });
            ActionButton("UnityCardPoolVersionIncludeFilteredButton", row.transform, "加入当前筛选", !selection.IsDefault, () =>
            {
                SetFilteredEnabled(true);
                Build();
            });
            ActionButton("UnityCardPoolVersionResetFiltersButton", row.transform, "重置筛选", true, () =>
            {
                searchText = string.Empty;
                versionTierFilter = 0;
                versionTribeFilter = Tribe.All;
                ResetVisibleCardPoolItems();
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
            else
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

            BuildCardPoolLoadState(content, totalCount);
            ConfigureCardPoolScrollLoading(scrollRect, totalCount);
        }

        private void BuildCardPoolLoadState(Transform parent, int totalCount)
        {
            var visibleCount = Math.Min(visibleCardPoolItemCount, totalCount);
            var label = UiFactory.Label("UnityCardPoolVersionLoadState", parent, totalCount == 0 ? "当前筛选无卡牌" : "已显示 " + visibleCount + " / " + totalCount, 11, FontStyle.Bold);
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
            UnityTavernUiStyle.SetFixedSize(toggleObject, 24f, 24f);
            var background = toggleObject.GetComponent<Image>();
            background.color = UnityTavernUiStyle.PanelQuiet;

            var check = new GameObject(name + "Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(toggleObject.transform, false);
            UnityTavernUiStyle.Stretch(check.GetComponent<RectTransform>());
            check.GetComponent<RectTransform>().offsetMin = new Vector2(5f, 5f);
            check.GetComponent<RectTransform>().offsetMax = new Vector2(-5f, -5f);
            check.GetComponent<Image>().color = UnityTavernUiStyle.Gold;

            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check.GetComponent<Image>();
            toggle.SetIsOnWithoutNotify(isOn);
            toggle.interactable = interactable;
            toggle.onValueChanged.AddListener(value => changed?.Invoke(value));

            BuildCardThumbnail(row.transform, name, sprite);

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

            var detail = UiFactory.Label(name + "Detail", labelBlock.transform, detailText, 12);
            detail.alignment = TextAnchor.MiddleLeft;
            detail.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(detail.gameObject, 22f);
        }

        private void BuildSetupToggle(Transform parent, string name, string text, bool isOn, bool interactable, Action<bool> changed)
        {
            var toggleObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);
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

            var label = UiFactory.Label(name + "Label", toggleObject.transform, text, layout.IsCompact ? 10 : 11, FontStyle.Bold);
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

        private void BuildCardThumbnail(Transform parent, string name, Sprite sprite)
        {
            var frame = UiFactory.Panel(name + "ImageFrame", parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetFixedSize(frame, 46f, 64f);

            if (sprite == null)
            {
                var empty = UiFactory.Label(name + "ImageMissing", frame.transform, "无图", 11, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
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
            foreach (var cardId in selection.EnabledMinionCardIds.Where(value => !IsDuoCardId(value)))
            {
                enabledMinionCardIds.Add(cardId);
            }

            foreach (var cardNumber in selection.EnabledTavernSpellCardNumbers)
            {
                enabledTavernSpellCardNumbers.Add(cardNumber);
            }

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

            var trimmed = string.IsNullOrWhiteSpace(name) ? "自定义版本" : name.Trim();
            profile.Name = trimmed.Length > 18 ? trimmed.Substring(0, 18) : trimmed;
            profile.UpdatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            store.SelectedVersionId = selectedVersionId;
            repository.Save(store);
        }

        private void CreateVersionFromDefault()
        {
            SelectVersion(null, false);
            CreateProfile("新版本");
        }

        private void CopyCurrentVersion()
        {
            CreateProfile(CurrentSelection().VersionName + " 副本");
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
                    EnabledTavernSpellCardNumbers = new HashSet<string>(enabledTavernSpellCardNumbers, StringComparer.OrdinalIgnoreCase)
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
            else
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
            var selection = CurrentSelection();
            if (!selection.IsDefault)
            {
                SaveCurrentVersion();
            }

            var resolvedSelectedAnomalyCardId = enableAnomalies && !randomizeAnomaly
                ? selectedAnomalyCardId
                : null;
            var shouldRandomizeAnomaly = enableAnomalies && (randomizeAnomaly || string.IsNullOrEmpty(resolvedSelectedAnomalyCardId));

            start?.Invoke(new MatchSetupOptions
            {
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
                AnomalyPoolVersion = AnomalyPoolVersion.CurrentHsReplay,
                ShowProxySafe = showProxySafe,
                ShowDebugOnly = showDebugOnly,
                ShowHiddenEffectOnly = showHiddenEffectOnly,
                ShowDisabled = showDebugOnly && showDisabled,
                EnabledMinionCardIds = enabledMinionCardIds.Where(value => !IsDuoCardId(value)).ToList(),
                EnabledTavernSpellCardNumbers = enabledTavernSpellCardNumbers.ToList()
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

        private AnomalyDefinition CurrentAnomaly()
        {
            if (anomalyCatalog == null || string.IsNullOrEmpty(selectedAnomalyCardId))
            {
                return null;
            }

            return anomalyCatalog.TryGetByCardId(selectedAnomalyCardId, out var anomaly)
                ? anomaly
                : null;
        }

        private IEnumerable<AnomalyDefinition> SelectableAnomalies()
        {
            if (anomalyCatalog == null)
            {
                return Enumerable.Empty<AnomalyDefinition>();
            }

            return anomalyCatalog
                .GetByPool(AnomalyPoolVersion.CurrentHsReplay)
                .Where(IsAnomalySelectable)
                .OrderBy(anomaly => anomaly.Name)
                .ThenBy(anomaly => anomaly.CardId);
        }

        private bool IsAnomalySelectable(AnomalyDefinition definition)
        {
            if (definition == null ||
                string.IsNullOrEmpty(definition.CardId) ||
                definition.SourcePools == null ||
                !definition.SourcePools.Contains(AnomalyPoolVersion.CurrentHsReplay))
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
                var missing = UiFactory.Label(name + "Missing", frame.transform, "无图", 10, FontStyle.Bold);
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

        private static string TribeListText(IEnumerable<Tribe> tribes)
        {
            var names = (tribes ?? Enumerable.Empty<Tribe>())
                .Where(tribe => tribe != Tribe.None)
                .Select(TribeName)
                .Distinct()
                .ToArray();
            return names.Length == 0 ? "中立" : string.Join("/", names);
        }

        private static string SpellTribesText(TavernSpellDefinition spell)
        {
            var tribes = TribeAvailabilityRules.SpellTribes(spell);
            return tribes.Count == 0 ? "通用法术" : string.Join("/", tribes.Select(TribeName).ToArray());
        }

        private static Button FilterButton(string name, Transform parent, string text, bool active, Color accentColor, Action onClick)
        {
            var button = UiFactory.Button(name, parent, text, () => onClick?.Invoke());
            var normal = active
                ? Color.Lerp(UnityTavernUiStyle.PanelRaised, accentColor, 0.45f)
                : UnityTavernUiStyle.Panel;
            var image = UnityTavernUiStyle.EnsureComponent<Image>(button.gameObject);
            image.color = normal;
            UnityTavernUiStyle.TintSelectable(
                button,
                normal,
                Color.Lerp(normal, accentColor, 0.28f),
                Color.Lerp(normal, Color.black, 0.16f));
            UnityTavernUiStyle.ConfigureOutline(
                button.gameObject,
                active ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.82f) : new Color(0f, 0f, 0f, 0.18f),
                active ? new Vector2(2f, -2f) : new Vector2(1f, -1f));
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 12;
                label.fontStyle = FontStyle.Bold;
                label.color = active ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Text;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            return button;
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

        private static string ShortLabel(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "未命名";
            }

            return value.Length > 6 ? value.Substring(0, 6) : value;
        }

        private string VersionNameFor(string versionId)
        {
            if (string.IsNullOrEmpty(versionId))
            {
                return CardPoolVersionFactory.DefaultVersionName;
            }

            var profile = store.Versions.FirstOrDefault(version => string.Equals(version.Id, versionId, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(profile?.Name) ? "自定义版本" : profile.Name;
        }

        private string AnomalyChoiceSummaryText()
        {
            if (!enableAnomalies)
            {
                return "畸变：关闭";
            }

            if (randomizeAnomaly || string.IsNullOrEmpty(selectedAnomalyCardId))
            {
                return "畸变：随机";
            }

            var anomaly = CurrentAnomaly();
            return "畸变：" + ShortLabel(string.IsNullOrEmpty(anomaly?.Name) ? selectedAnomalyCardId : anomaly.Name);
        }

        private static string AnomalyDetailText(AnomalyDefinition anomaly)
        {
            if (anomaly == null)
            {
                return string.Empty;
            }

            var text = string.IsNullOrEmpty(anomaly.Text) ? anomaly.CardId : anomaly.Text;
            return text + "  " + anomaly.CardId;
        }

        private string AdvancedMechanicsSummaryText()
        {
            var enabled = new List<string>();
            if (enableQuests)
            {
                enabled.Add("任务");
            }

            if (enableTrinkets)
            {
                enabled.Add("饰品");
            }

            if (enableQuestRewards)
            {
                enabled.Add("奖励");
            }

            if (enableAnomalies)
            {
                enabled.Add(randomizeAnomaly || string.IsNullOrEmpty(selectedAnomalyCardId)
                    ? "畸变:随机"
                    : "畸变:" + ShortLabel(CurrentAnomaly()?.Name ?? selectedAnomalyCardId));
            }

            return enabled.Count == 0 ? "本局关闭" : string.Join(" / ", enabled.ToArray());
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
