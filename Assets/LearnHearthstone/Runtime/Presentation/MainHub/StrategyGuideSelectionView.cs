using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.MainHub
{
    public sealed class StrategyGuideSelectionView
    {
        private readonly Transform root;
        private readonly StrategyGuideCatalog catalog;
        private readonly GameCatalogSet catalogs;
        private readonly string gameVersionId;
        private readonly Action<string, string> startGuide;
        private readonly Action back;
        private readonly bool useEnglish;
        private readonly UnityTavernLayoutContext layout;
        private readonly ResolvedGameVersion resolvedVersion;
        private readonly Action<StrategyGuideImportResult> startImportedGuide;
        private readonly FileStrategyGuideAuthoringRepository authoringRepository;
        private readonly bool mobileOnePageOnly;
        private GameObject importOverlay;
        private GameObject shareOverlay;
        private GameObject authoringOverlay;
        private GameObject deleteDraftConfirmation;
        private readonly Dictionary<string, GameObject> guideDetails = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, Button> guideSelectors = new Dictionary<string, Button>(StringComparer.Ordinal);

        public StrategyGuideSelectionView(
            Transform root,
            StrategyGuideCatalog catalog,
            GameCatalogSet catalogs,
            string gameVersionId,
            Action<string, string> startGuide,
            Action back,
            bool useEnglish = false,
            UnityTavernLayoutContext? layoutContext = null,
            ResolvedGameVersion resolvedVersion = null,
            Action<StrategyGuideImportResult> startImportedGuide = null,
            FileStrategyGuideAuthoringRepository authoringRepository = null,
            bool mobileOnePageOnly = false)
        {
            this.root = root;
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.catalogs = catalogs ?? throw new ArgumentNullException(nameof(catalogs));
            this.gameVersionId = gameVersionId;
            this.startGuide = startGuide;
            this.back = back;
            this.useEnglish = useEnglish;
            this.resolvedVersion = resolvedVersion;
            this.startImportedGuide = startImportedGuide;
            this.authoringRepository = authoringRepository ?? new FileStrategyGuideAuthoringRepository();
            this.mobileOnePageOnly = mobileOnePageOnly;
            layout = layoutContext ?? UnityTavernLayoutContext.FromRoot(root);
        }

        public void Build()
        {
            var shell = UiFactory.Panel("StrategyGuideSelection", root, StrategyGuideUiTheme.Background);
            UiFactory.Stretch(shell.GetComponent<RectTransform>());
            UiFactory.Vertical(shell, layout.IsCompact ? 8 : 14, layout.IsCompact ? 8 : 10);
            BuildHeader(shell.transform);
            BuildGuideModeSwitcher(shell.transform);

            var guides = catalog.Guides
                .Where(item => string.Equals(item.GameVersionId, gameVersionId, StringComparison.Ordinal))
                .ToList();
            var workspace = UiFactory.Panel("StrategyGuideWorkspace", shell.transform, StrategyGuideUiTheme.Workspace);
            UiFactory.SetFlexible(workspace, 1f, 1f);
            var workspaceLayout = layout.IsCompact
                ? (HorizontalOrVerticalLayoutGroup)UiFactory.Vertical(workspace, 8, 8)
                : UiFactory.Horizontal(workspace, 10, 12);
            workspaceLayout.childControlWidth = true;
            workspaceLayout.childControlHeight = true;
            workspaceLayout.childForceExpandWidth = layout.IsCompact;
            workspaceLayout.childForceExpandHeight = true;

            var rail = UiFactory.Panel("StrategyGuideRail", workspace.transform, StrategyGuideUiTheme.Surface);
            StrategyGuideUiTheme.ApplySurface(rail, StrategyGuideUiTheme.Surface, "panel_strategy_rail");
            StrategyGuideUiTheme.Outline(rail, StrategyGuideUiTheme.BorderStrong);
            if (layout.IsCompact)
            {
                UiFactory.SetHeight(rail, 176f);
            }
            else
            {
                var railPhysicalWidth = mobileOnePageOnly
                    ? Mathf.Clamp(layout.Width * 0.21f, 220f, 280f)
                    : Mathf.Clamp(layout.Width * 0.24f, 250f, 330f);
                var railWidth = layout.CanvasUnitsForPhysicalPixels(railPhysicalWidth);
                var railElement = rail.GetComponent<LayoutElement>() ?? rail.AddComponent<LayoutElement>();
                railElement.minWidth = railWidth;
                railElement.preferredWidth = railWidth;
                railElement.flexibleWidth = 0f;
                railElement.layoutPriority = 2;
            }
            var railLayout = UiFactory.Vertical(rail, 10, 8);
            railLayout.childControlWidth = true;
            railLayout.childForceExpandWidth = true;
            var railTitle = UiFactory.Label("StrategyGuideRailTitle", rail.transform, T("首发阵容", "Launch lineups"), 20, FontStyle.Bold, layout);
            railTitle.color = StrategyGuideUiTheme.WarmText;
            UiFactory.SetHeight(railTitle.gameObject, 30f);
            var railHint = UiFactory.Label("StrategyGuideRailHint", rail.transform, T("3 套简单模式黄金样例", "Three Showcase golden samples"), 14, FontStyle.Normal, layout);
            railHint.color = StrategyGuideUiTheme.MutedText;
            UiFactory.SetHeight(railHint.gameObject, 24f);

            var selectorParent = rail.transform;
            if (layout.IsCompact)
            {
                selectorParent = UiFactory.ScrollView(
                    "StrategyGuideRailListScroll",
                    rail.transform,
                    Color.clear,
                    out _,
                    layout);
                var selectorList = UiFactory.Vertical(selectorParent.gameObject, 4, 2);
                selectorList.childControlWidth = true;
                selectorList.childForceExpandWidth = true;
            }

            var detailContent = UiFactory.ScrollView(
                "StrategyGuideDetailScroll",
                workspace.transform,
                StrategyGuideUiTheme.Workspace,
                out _,
                layout);
            var detailLayout = UiFactory.Vertical(detailContent.gameObject, 0, 0);
            detailLayout.childControlWidth = true;
            detailLayout.childForceExpandWidth = true;

            Button first = null;
            foreach (var guide in guides)
            {
                var selector = BuildGuideSelector(selectorParent, guide);
                guideSelectors.Add(guide.GuideId, selector);
                first ??= selector;
                var detail = BuildGuideDetail(detailContent, guide);
                guideDetails.Add(guide.GuideId, detail);
                detail.SetActive(false);
            }

            if (guides.Count > 0)
            {
                SelectGuide(guides[0].GuideId);
            }
            if (first != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }

        private void BuildHeader(Transform parent)
        {
            var header = UiFactory.Panel(
                "StrategyGuideHeader",
                parent,
                StrategyGuideUiTheme.Workspace);
            UiFactory.SetHeight(header, layout.IsCompact ? 76f : 84f);
            StrategyGuideUiTheme.ApplySurface(header, StrategyGuideUiTheme.Workspace, "panel_workspace");
            StrategyGuideUiTheme.Outline(header, StrategyGuideUiTheme.BorderStrong);
            var row = UiFactory.Horizontal(header, 0, 12);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;

            var stack = UiFactory.Panel("StrategyGuideHeaderText", header.transform, Color.clear);
            UiFactory.SetFlexible(stack, 1f, 0f);
            UiFactory.Vertical(stack, 0, 2);
            var titleText = mobileOnePageOnly
                ? T("酒馆战棋 · 一图流试玩", "Battlegrounds · One-Page Training")
                : T("36.2 一图流试玩", "36.2 Lineup Challenges");
            var title = UiFactory.Label("StrategyGuideHeaderTitle", stack.transform, titleText, layout.IsCompact ? 22 : 28, FontStyle.Bold, layout);
            title.color = StrategyGuideUiTheme.WarmText;
            var profileTitles = catalog.Guides
                .Where(item => string.Equals(item.GameVersionId, gameVersionId, StringComparison.Ordinal))
                .SelectMany(item => item.EntryProfiles ?? new System.Collections.Generic.List<StrategyGuideEntryProfileDefinition>())
                .Where(item => item != null)
                .Select(item => useEnglish && !string.IsNullOrWhiteSpace(item.EnglishTitle) ? item.EnglishTitle : item.Title)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var subtitleText = T("同一阵容共享目标 · 可用入口：", "Shared lineup objective · Entries: ") +
                string.Join(" · ", profileTitles);
            var subtitle = UiFactory.Label("StrategyGuideHeaderSubtitle", stack.transform, subtitleText, 14, FontStyle.Bold, layout);
            subtitle.color = StrategyGuideUiTheme.MutedText;

            if (resolvedVersion != null && startImportedGuide != null)
            {
                var import = UiFactory.Button(
                    "StrategyGuideImportButton",
                    header.transform,
                    T("导入代码", "Import code"),
                    OpenImportModal,
                    layout);
                StrategyGuideUiTheme.SecondaryButton(import);
                UiFactory.SetWidth(import.gameObject, layout.IsCompact ? 104f : 132f);
            }

            if (back != null)
            {
                var backButton = UiFactory.Button("StrategyGuideBackButton", header.transform, T("返回", "Back"), () => back.Invoke(), layout);
                StrategyGuideUiTheme.QuietButton(backButton);
                UiFactory.SetWidth(backButton.gameObject, layout.IsCompact ? 88f : 116f);
            }
        }

        private void BuildGuideModeSwitcher(Transform parent)
        {
            var modes = UiFactory.Panel(
                "StrategyGuideModeSwitcher",
                parent,
                StrategyGuideUiTheme.SurfaceSoft);
            UiFactory.SetHeight(modes, mobileOnePageOnly || layout.IsCompact ? 58f : 72f);
            StrategyGuideUiTheme.ApplySurface(modes, StrategyGuideUiTheme.SurfaceSoft, "panel_workspace");
            StrategyGuideUiTheme.Outline(modes, StrategyGuideUiTheme.BorderStrong);
            var row = UiFactory.Horizontal(
                modes,
                mobileOnePageOnly || layout.IsCompact ? 6 : 8,
                mobileOnePageOnly || layout.IsCompact ? 8 : 12);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = true;

            var browse = UiFactory.Button(
                "StrategyGuideBrowseModeButton",
                modes.transform,
                T("查看一图流", "View one-page guides"),
                () => { },
                layout);
            UiFactory.SetFlexible(browse.gameObject, 1f, 1f);
            UnityTavernUiStyle.ConfigureButton(browse, UnityTavernUiStyle.ArcaneBlue, true, true);

            var create = UiFactory.Button(
                "StrategyGuideAuthoringOpenButton",
                modes.transform,
                resolvedVersion == null
                    ? T("创建一图流（不可用）", "Create guide (unavailable)")
                    : T("创建一图流", "Create a one-page guide"),
                OpenAuthoringTemplates,
                layout);
            create.interactable = resolvedVersion != null;
            UiFactory.SetFlexible(create.gameObject, 1f, 1f);
            UnityTavernUiStyle.ConfigureButton(create, UnityTavernUiStyle.Gold, false, true);
        }

        private void OpenAuthoringTemplates()
        {
            if (authoringOverlay != null || resolvedVersion == null)
            {
                return;
            }

            authoringOverlay = UiFactory.Panel(
                "StrategyGuideAuthoringTemplateOverlay",
                root,
                UnityTavernUiStyle.WithAlpha(Color.black, 0.82f));
            authoringOverlay.GetComponent<Image>().raycastTarget = true;
            UiFactory.Stretch(authoringOverlay.GetComponent<RectTransform>());

            var card = UiFactory.Panel(
                "StrategyGuideAuthoringTemplateCard",
                authoringOverlay.transform,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.995f));
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(
                Mathf.Clamp(layout.Width - 32f, 320f, layout.IsCompact ? 560f : 820f),
                Mathf.Clamp(layout.Height - 32f, 420f, layout.IsCompact ? 520f : 620f));
            UnityTavernUiStyle.ConfigureOutline(
                card,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.72f),
                new Vector2(2f, -2f));
            UiFactory.Vertical(card, layout.IsCompact ? 12 : 18, 10);

            var title = UiFactory.Label(
                "StrategyGuideAuthoringTemplateTitle",
                card.transform,
                T("选择一个起点", "Choose a starting point"),
                layout.IsCompact ? 22 : 26,
                FontStyle.Bold,
                layout);
            title.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetHeight(title.gameObject, 40f);
            var hint = UiFactory.Label(
                "StrategyGuideAuthoringTemplateHint",
                card.transform,
                T("模板只用于提供合法初值；你的草稿会单独保存，不会改动已发布攻略。", "The template only provides valid defaults. Your draft is saved separately and never changes the published guide."),
                14,
                FontStyle.Normal,
                layout);
            hint.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(hint.gameObject, layout.IsCompact ? 54f : 42f);

            var blank = UiFactory.Button(
                "StrategyGuideAuthoringBlankButton",
                card.transform,
                T("空白创建", "Start blank"),
                () => OpenAuthoringEditor(CreateBlankGuide()),
                layout);
            UnityTavernUiStyle.ConfigureButton(blank, UnityTavernUiStyle.Gold, true);
            UiFactory.SetHeight(blank.gameObject, UnityTavernUiStyle.TouchHeight);

            IReadOnlyList<string> draftIds = Array.Empty<string>();
            string draftReadError = null;
            try
            {
                draftIds = authoringRepository.ListDraftIds();
            }
            catch (InvalidOperationException exception)
            {
                draftReadError = exception.Message;
            }
            var tabs = UiFactory.Panel(
                "StrategyGuideAuthoringStartTabs",
                card.transform,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.TableDark, 0.9f));
            var tabStripHeight = layout.IsCompact ? 52f : 56f;
            UiFactory.SetHeight(tabs, tabStripHeight);
            var tabStripElement = tabs.GetComponent<LayoutElement>();
            tabStripElement.minHeight = tabStripHeight;
            tabStripElement.flexibleHeight = 0f;
            var tabRow = UiFactory.Horizontal(tabs, 4, 6);
            tabRow.childControlWidth = true;
            tabRow.childForceExpandWidth = true;

            var pages = new List<GameObject>();
            var tabButtons = new List<Button>();
            tabButtons.Add(UiFactory.Button(
                "StrategyGuideAuthoringDraftsTab",
                tabs.transform,
                T("继续草稿", "Drafts") + " (" + draftIds.Count + ")",
                () => SelectAuthoringStartPage(pages, tabButtons, 0),
                layout));
            tabButtons.Add(UiFactory.Button(
                "StrategyGuideAuthoringTemplatesTab",
                tabs.transform,
                T("使用模板", "Templates"),
                () => SelectAuthoringStartPage(pages, tabButtons, 1),
                layout));
            tabButtons.Add(UiFactory.Button(
                "StrategyGuideAuthoringVerifiedTab",
                tabs.transform,
                T("已验证阵容", "Verified"),
                () => SelectAuthoringStartPage(pages, tabButtons, 2),
                layout));
            foreach (var tabButton in tabButtons)
            {
                var tabElement = tabButton.GetComponent<LayoutElement>();
                tabElement.minHeight = 0f;
                tabElement.preferredHeight = tabStripHeight - 8f;
                tabElement.flexibleHeight = 0f;
            }

            var draftContent = BuildAuthoringStartPage(card.transform, "StrategyGuideAuthoringDraftsPage", pages);
            if (draftIds.Count > 0)
            {
                foreach (var draftId in draftIds)
                {
                    BuildAuthoringDraftRow(draftContent, authoringRepository.LoadDraft(draftId));
                }
            }
            else
            {
                BuildAuthoringEmptyState(
                    draftContent,
                    "StrategyGuideAuthoringDraftsEmpty",
                    !string.IsNullOrWhiteSpace(draftReadError)
                        ? T("草稿读取失败：", "Could not read drafts: ") + draftReadError
                        : T("暂无本地草稿。可以空白创建，也可以使用模板。", "No local drafts. Start blank or use a template."),
                    !string.IsNullOrWhiteSpace(draftReadError));
            }

            var guides = catalog.Guides.Where(item =>
                    string.Equals(item.GameVersionId, gameVersionId, StringComparison.Ordinal))
                .ToList();
            var templateContent = BuildAuthoringStartPage(card.transform, "StrategyGuideAuthoringTemplatesPage", pages);
            foreach (var guide in guides)
            {
                BuildAuthoringTemplateRow(templateContent, guide, false);
            }

            var verifiedContent = BuildAuthoringStartPage(card.transform, "StrategyGuideAuthoringVerifiedPage", pages);
            foreach (var guide in guides)
            {
                BuildAuthoringTemplateRow(verifiedContent, guide, true);
            }

            SelectAuthoringStartPage(pages, tabButtons, 0);

            var cancel = UiFactory.Button(
                "StrategyGuideAuthoringTemplateCancelButton",
                card.transform,
                T("取消", "Cancel"),
                CloseAuthoringOverlay,
                layout);
            UnityTavernUiStyle.ConfigureButton(cancel, UnityTavernUiStyle.ArcaneBlue);
            UiFactory.SetHeight(cancel.gameObject, UnityTavernUiStyle.TouchHeight);
            authoringOverlay.AddComponent<UnityFocusTrap>().Activate(
                blank.gameObject);
        }

        private StrategyGuideDefinition CreateBlankGuide()
        {
            var authoringCatalogs = resolvedVersion?.Snapshot?.ForLanguage(useEnglish) ?? catalogs;
            var hero = authoringCatalogs.Heroes.AllHeroes.FirstOrDefault(item =>
                item.InPool &&
                HeroEffectImplementationRegistry.FindByHeroCardId(item.HeroCardId).Status == HeroEffectImplementationStatus.Implemented);
            var lesserTrinket = authoringCatalogs.Trinkets.GetOfferableBySlot(TrinketSlotKind.Lesser).FirstOrDefault();
            var greaterTrinket = authoringCatalogs.Trinkets.GetOfferableBySlot(TrinketSlotKind.Greater).FirstOrDefault();
            var minions = authoringCatalogs.Minions.All.Where(item => item.InPool).Take(7).ToList();
            var activeTribes = TribeAvailabilityRules.PlayableTribes.Take(5).Select(item => item.ToString()).ToList();

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var guide = new StrategyGuideDefinition
            {
                GuideId = "GUIDE-CUSTOM-" + timestamp,
                RevisionId = "authoring-working-copy",
                GameVersionId = gameVersionId,
                Title = "未命名一图流",
                EnglishTitle = "Untitled one-page guide",
                Summary = "从空白阵容开始制作。",
                EnglishSummary = "Created from a blank lineup.",
                Archetype = "Custom",
                HeroCardId = hero?.HeroCardId,
                LesserTrinketCardId = lesserTrinket?.CardId,
                GreaterTrinketCardId = greaterTrinket?.CardId,
                RecommendedLesserTrinketCardIds = lesserTrinket == null
                    ? new List<string>()
                    : new List<string> { lesserTrinket.CardId },
                RecommendedGreaterTrinketCardIds = greaterTrinket == null
                    ? new List<string>()
                    : new List<string> { greaterTrinket.CardId },
                ActiveTribes = activeTribes,
                RequiredTribes = new List<string>()
            };

            for (var index = 0; index < 7; index += 1)
            {
                var cardId = minions.Count == 0 ? null : minions[index % minions.Count].CardId;
                guide.FinalComposition.Add(new StrategyGuideCardDefinition
                {
                    PlacementId = "custom-final-" + (index + 1),
                    CardKind = StrategyGuideCardKinds.Minion,
                    CardId = cardId,
                    Provenance = StrategyGuideProvenance.NormalPool
                });
            }

            var opponent = (catalog.Opponents ?? new List<StrategyGuideOpponentDefinition>()).FirstOrDefault(item =>
                item != null && string.Equals(item.GameVersionId, gameVersionId, StringComparison.Ordinal));
            var profile = new StrategyGuideEntryProfileDefinition
            {
                ProfileId = "showcase",
                Difficulty = StrategyGuideDifficulties.Showcase,
                Title = "简单模式",
                EnglishTitle = "Showcase",
                LearningGoal = "完成这套阵容的一图流教学。",
                EnglishLearningGoal = "Complete this lineup's one-page lesson.",
                StartRound = 10,
                TavernTier = 6,
                Gold = 10,
                MaxGold = 10,
                Seed = timestamp.GetHashCode(),
                AllowedCommands = new List<string>
                {
                    "BuyMinion", "SellMinion", "RerollShop", "FreezeShop", "UpgradeTavern",
                    "PlayMinion", "MoveMinion", "MoveBoardMinion", "UseHeroPower",
                    "ChooseDiscover", "ChooseMechanicOption", "UseGuideShapingSpell",
                    "BeginNextTurnTransition", "ContinueNextTurnTransition"
                },
                ShapingSpellCardIds = new List<string> { StrategyGuideShapingSpells.Battlecry },
                Opponent = new StrategyGuideOpponentSelector
                {
                    StrengthRound = opponent?.StrengthRound ?? 10,
                    RequiredTag = opponent?.Tags?.FirstOrDefault()
                },
                Victory = new StrategyGuideVictoryCondition
                {
                    RequireFinalComposition = true,
                    RequireCombatWin = true,
                    PostWinChoices = new List<string> { "FreeExplore", "Restart", "Return" }
                },
                Undo = new StrategyGuideUndoPolicy
                {
                    UsesPerRun = 1,
                    RestoreRng = true,
                    LockAfterTurnEnd = true,
                    LockAfterCombat = true,
                    LockDuringFreeExplore = true
                }
            };
            foreach (var card in guide.FinalComposition)
            {
                profile.Placements.Add(new StrategyGuideCardDefinition
                {
                    PlacementId = "custom-board-" + profile.Placements.Count,
                    Zone = StrategyGuideZones.Board,
                    CardKind = card.CardKind,
                    CardId = card.CardId,
                    Provenance = card.Provenance
                });
            }
            guide.EntryProfiles.Add(profile);
            return guide;
        }

        private Transform BuildAuthoringStartPage(Transform parent, string name, ICollection<GameObject> pages)
        {
            var content = UiFactory.ScrollView(
                name,
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.TableDark, 0.9f),
                out var scroll,
                layout);
            UiFactory.SetFlexible(scroll.gameObject, 1f, 1f);
            var list = UiFactory.Vertical(content.gameObject, 10, 8);
            list.childControlWidth = true;
            list.childForceExpandWidth = true;
            pages.Add(scroll.gameObject);
            return content;
        }

        private void BuildAuthoringEmptyState(Transform parent, string name, string message, bool isError = false)
        {
            var empty = UiFactory.Label(name, parent, message, 16, FontStyle.Bold, layout);
            empty.alignment = TextAnchor.MiddleCenter;
            empty.color = isError ? UnityTavernUiStyle.DangerRed : UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(empty.gameObject, 88f);
        }

        private static void SelectAuthoringStartPage(
            IReadOnlyList<GameObject> pages,
            IReadOnlyList<Button> buttons,
            int selectedIndex)
        {
            for (var index = 0; index < pages.Count; index += 1)
            {
                pages[index].SetActive(index == selectedIndex);
                UnityTavernUiStyle.ConfigureButton(
                    buttons[index],
                    index == selectedIndex ? UnityTavernUiStyle.ArcaneBlue : UnityTavernUiStyle.SurfaceRaised,
                    index == selectedIndex,
                    true);
            }
        }

        private void BuildAuthoringListHeading(Transform parent, string text)
        {
            var heading = UiFactory.Label(
                "StrategyGuideAuthoringListHeading",
                parent,
                text,
                16,
                FontStyle.Bold,
                layout);
            heading.color = UnityTavernUiStyle.Gold;
            UiFactory.SetHeight(heading.gameObject, 34f);
        }

        private void BuildAuthoringDraftRow(Transform parent, StrategyGuideAuthoringDraft draft)
        {
            var savedAt = authoringRepository.GetDraftLastSavedUtc(draft.DraftId).ToLocalTime();
            var titleText = useEnglish && !string.IsNullOrWhiteSpace(draft.Guide.EnglishTitle)
                ? draft.Guide.EnglishTitle.Trim()
                : draft.Guide.Title?.Trim();
            if (string.IsNullOrWhiteSpace(titleText) ||
                string.Equals(titleText, "未命名一图流", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(titleText, "Untitled one-page guide", StringComparison.OrdinalIgnoreCase))
            {
                titleText = T("未命名草稿", "Untitled draft");
            }
            var dateText = savedAt.ToString(
                useEnglish ? "MMM dd" : "MM月dd日",
                CultureInfo.InvariantCulture);
            var panel = UiFactory.Panel(
                "StrategyGuideAuthoringDraft-" + draft.DraftId,
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.98f));
            UiFactory.SetHeight(panel, layout.IsCompact ? 78f : 86f);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.ArcaneBlue, 0.44f),
                new Vector2(1f, -1f));
            var row = UiFactory.Horizontal(panel, 10, 10);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;
            var copy = UiFactory.Panel("StrategyGuideAuthoringDraftCopy-" + draft.DraftId, panel.transform, Color.clear);
            var copyElement = copy.AddComponent<LayoutElement>();
            copyElement.minWidth = 120f;
            copyElement.minHeight = 26f;
            copyElement.flexibleWidth = 1f;
            var copyLayout = UiFactory.Vertical(copy, 0, 0);
            copyLayout.childForceExpandHeight = false;
            var titleRow = UiFactory.Panel(
                "StrategyGuideAuthoringDraftTitleRow-" + draft.DraftId,
                copy.transform,
                Color.clear);
            UiFactory.SetHeight(titleRow, 26f);
            var titleLayout = UiFactory.Horizontal(titleRow, 0, 6);
            titleLayout.childControlWidth = true;
            var title = UiFactory.Label(
                "StrategyGuideAuthoringDraftName-" + draft.DraftId,
                titleRow.transform,
                titleText,
                18,
                FontStyle.Bold,
                layout);
            title.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetFlexible(title.gameObject, 1f, 0f);
            var date = UiFactory.Label(
                "StrategyGuideAuthoringDraftDate-" + draft.DraftId,
                titleRow.transform,
                dateText,
                14,
                FontStyle.Bold,
                layout);
            date.alignment = TextAnchor.MiddleRight;
            date.color = UnityTavernUiStyle.Gold;
            date.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.SetWidth(date.gameObject, useEnglish ? 84f : 150f);
            var open = UiFactory.Button(
                "StrategyGuideAuthoringDraftOpenButton-" + draft.DraftId,
                panel.transform,
                T("继续编辑", "Continue"),
                () => OpenAuthoringEditor(draft.Guide, draft),
                layout);
            UnityTavernUiStyle.ConfigureButton(open, UnityTavernUiStyle.ArcaneBlue, true);
            UiFactory.SetWidth(open.gameObject, layout.IsCompact ? 112f : 140f);
            var delete = UiFactory.Button(
                "StrategyGuideAuthoringDraftDeleteButton-" + draft.DraftId,
                panel.transform,
                T("删除", "Delete"),
                () => ConfirmDeleteDraft(draft, panel),
                layout);
            UnityTavernUiStyle.ConfigureButton(delete, UnityTavernUiStyle.DangerRed, false);
            UiFactory.SetWidth(delete.gameObject, layout.IsCompact ? 76f : 88f);
        }

        private void BuildAuthoringTemplateRow(Transform parent, StrategyGuideDefinition guide, bool viewOnly)
        {
            var panel = UiFactory.Panel(
                (viewOnly ? "StrategyGuideAuthoringVerified-" : "StrategyGuideAuthoringTemplate-") + guide.GuideId,
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.98f));
            UiFactory.SetHeight(panel, layout.IsCompact ? 82f : 90f);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.36f),
                new Vector2(1f, -1f));
            var row = UiFactory.Horizontal(panel, 10, 10);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;
            var copy = UiFactory.Panel("StrategyGuideAuthoringTemplateCopy-" + guide.GuideId, panel.transform, Color.clear);
            var copyElement = copy.AddComponent<LayoutElement>();
            copyElement.minWidth = 120f;
            copyElement.minHeight = 50f;
            copyElement.flexibleWidth = 1f;
            var copyLayout = UiFactory.Vertical(copy, 0, 2);
            copyLayout.childForceExpandHeight = false;
            var title = UiFactory.Label(
                "StrategyGuideAuthoringTemplateName-" + guide.GuideId,
                copy.transform,
                useEnglish && !string.IsNullOrWhiteSpace(guide.EnglishTitle) ? guide.EnglishTitle : guide.Title,
                18,
                FontStyle.Bold,
                layout);
            title.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetHeight(title.gameObject, 26f);
            var summary = UiFactory.Label(
                "StrategyGuideAuthoringTemplateSummary-" + guide.GuideId,
                copy.transform,
                useEnglish && !string.IsNullOrWhiteSpace(guide.EnglishSummary) ? guide.EnglishSummary : guide.Summary,
                14,
                FontStyle.Normal,
                layout);
            summary.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(summary.gameObject, 22f);
            var select = UiFactory.Button(
                (viewOnly ? "StrategyGuideAuthoringVerifiedSelectButton-" : "StrategyGuideAuthoringTemplateSelectButton-") + guide.GuideId,
                panel.transform,
                viewOnly ? T("查看阵容", "View lineup") : T("从此开始", "Use template"),
                () =>
                {
                    if (viewOnly)
                    {
                        CloseAuthoringOverlay();
                        SelectGuide(guide.GuideId);
                    }
                    else
                    {
                        OpenAuthoringEditor(guide);
                    }
                },
                layout);
            UnityTavernUiStyle.ConfigureButton(select, UnityTavernUiStyle.Gold, true);
            UiFactory.SetWidth(select.gameObject, layout.IsCompact ? 112f : 140f);
        }

        private void ConfirmDeleteDraft(StrategyGuideAuthoringDraft draft, GameObject row)
        {
            if (deleteDraftConfirmation != null || authoringOverlay == null)
            {
                return;
            }

            deleteDraftConfirmation = UiFactory.Panel(
                "StrategyGuideAuthoringDeleteConfirmation",
                authoringOverlay.transform,
                UnityTavernUiStyle.WithAlpha(Color.black, 0.78f));
            deleteDraftConfirmation.GetComponent<Image>().raycastTarget = true;
            UiFactory.Stretch(deleteDraftConfirmation.GetComponent<RectTransform>());
            var card = UiFactory.Panel(
                "StrategyGuideAuthoringDeleteConfirmationCard",
                deleteDraftConfirmation.transform,
                UnityTavernUiStyle.SurfaceDark);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(layout.IsCompact ? 300f : 420f, 170f);
            UiFactory.Vertical(card, 14, 10);
            var message = UiFactory.Label(
                "StrategyGuideAuthoringDeleteConfirmationText",
                card.transform,
                T("删除这个本地草稿？冻结版本不会受影响。", "Delete this local draft? Frozen versions are unaffected."),
                16,
                FontStyle.Bold,
                layout);
            message.alignment = TextAnchor.MiddleCenter;
            message.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetFlexible(message.gameObject, 1f, 1f);
            var actions = UiFactory.Panel("StrategyGuideAuthoringDeleteConfirmationActions", card.transform, Color.clear);
            UiFactory.SetHeight(actions, UnityTavernUiStyle.TouchHeight);
            var actionRow = UiFactory.Horizontal(actions, 0, 10);
            actionRow.childControlWidth = true;
            actionRow.childForceExpandWidth = true;
            var cancel = UiFactory.Button(
                "StrategyGuideAuthoringDeleteCancelButton",
                actions.transform,
                T("保留", "Keep"),
                CloseDeleteDraftConfirmation,
                layout);
            UnityTavernUiStyle.ConfigureButton(cancel, UnityTavernUiStyle.ArcaneBlue, true);
            var confirm = UiFactory.Button(
                "StrategyGuideAuthoringDeleteConfirmButton",
                actions.transform,
                T("删除草稿", "Delete draft"),
                () =>
                {
                    authoringRepository.DeleteDraft(draft.DraftId);
                    row.SetActive(false);
                    CloseDeleteDraftConfirmation();
                },
                layout);
            UnityTavernUiStyle.ConfigureButton(confirm, UnityTavernUiStyle.DangerRed, true);
            deleteDraftConfirmation.AddComponent<UnityFocusTrap>().Activate(cancel.gameObject);
        }

        private void CloseDeleteDraftConfirmation()
        {
            if (deleteDraftConfirmation == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(deleteDraftConfirmation);
            }
            else
            {
                UnityEngine.Object.Destroy(deleteDraftConfirmation);
            }
#else
            UnityEngine.Object.Destroy(deleteDraftConfirmation);
#endif
            deleteDraftConfirmation = null;
        }

        private void OpenAuthoringEditor(
            StrategyGuideDefinition guide,
            StrategyGuideAuthoringDraft existingDraft = null)
        {
            CloseAuthoringOverlay();
            var editor = new StrategyGuideAuthoringEditorView(
                root,
                catalog,
                catalogs,
                resolvedVersion,
                guide,
                authoringRepository,
                CloseAuthoringOverlay,
                useEnglish,
                layout,
                existingDraft: existingDraft,
                startImportedGuide: startImportedGuide);
            authoringOverlay = editor.Build();
        }

        private void CloseAuthoringOverlay()
        {
            if (authoringOverlay == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(authoringOverlay);
            }
            else
            {
                UnityEngine.Object.Destroy(authoringOverlay);
            }
#else
            UnityEngine.Object.Destroy(authoringOverlay);
#endif
            authoringOverlay = null;
            deleteDraftConfirmation = null;
        }

        private Button BuildGuideSelector(Transform parent, StrategyGuideDefinition guide)
        {
            var showcase = PreferredProfile(guide);
            var title = useEnglish && !string.IsNullOrWhiteSpace(guide.EnglishTitle)
                ? guide.EnglishTitle
                : guide.Title;
            var subtitle = showcase == null
                ? T("一图流阵容", "Lineup guide")
                : T("第 ", "Round ") + showcase.StartRound + T(" 回合 · ", " · Tier ") + showcase.TavernTier + T(" 级", string.Empty);
            var button = UiFactory.Button(
                "StrategyGuideCard-" + guide.GuideId,
                parent,
                title + "\n" + subtitle,
                () => SelectGuide(guide.GuideId),
                layout);
            UnityTavernUiStyle.SetPreferredHeight(button.gameObject, layout.IsCompact ? 48f : 76f);
            StrategyGuideUiTheme.SecondaryButton(button);
            var label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin = new Vector2(14f, 6f);
            label.rectTransform.offsetMax = new Vector2(-10f, -6f);
            return button;
        }

        private void SelectGuide(string guideId)
        {
            foreach (var pair in guideDetails)
            {
                pair.Value.SetActive(string.Equals(pair.Key, guideId, StringComparison.Ordinal));
            }
            foreach (var pair in guideSelectors)
            {
                StrategyGuideUiTheme.SecondaryButton(
                    pair.Value,
                    string.Equals(pair.Key, guideId, StringComparison.Ordinal));
            }
        }

        private GameObject BuildGuideDetail(Transform parent, StrategyGuideDefinition guide)
        {
            var panel = UiFactory.Panel("StrategyGuideDetail-" + guide.GuideId, parent, StrategyGuideUiTheme.Surface);
            StrategyGuideUiTheme.ApplySurface(panel, StrategyGuideUiTheme.Surface, "panel_workspace");
            StrategyGuideUiTheme.Outline(panel, StrategyGuideUiTheme.BorderStrong);
            var column = UiFactory.Vertical(panel, layout.IsCompact ? 10 : 14, layout.IsCompact ? 8 : 10);
            column.childControlWidth = true;
            column.childForceExpandWidth = true;

            var identity = UiFactory.Panel("StrategyGuideIdentity-" + guide.GuideId, panel.transform, StrategyGuideUiTheme.SurfaceSelected);
            UiFactory.SetHeight(identity, layout.IsCompact ? 112f : 94f);
            var identityRow = UiFactory.Horizontal(identity, 10, 10);
            identityRow.childAlignment = TextAnchor.MiddleCenter;
            identityRow.childForceExpandWidth = false;
            if (!layout.IsCompact)
            {
                BuildHeroPortrait(identity.transform, guide, 62f, 72f);
            }
            var copy = UiFactory.Panel("StrategyGuideCopy-" + guide.GuideId, identity.transform, Color.clear);
            UiFactory.SetFlexible(copy, 1f, 0f);
            UiFactory.Vertical(copy, 0, 3);
            var title = UiFactory.Label(
                "StrategyGuideTitle-" + guide.GuideId,
                copy.transform,
                useEnglish && !string.IsNullOrWhiteSpace(guide.EnglishTitle) ? guide.EnglishTitle : guide.Title,
                layout.IsCompact ? 20 : 24,
                FontStyle.Bold,
                layout);
            title.color = StrategyGuideUiTheme.WarmText;
            var summary = UiFactory.Label(
                "StrategyGuideSummary-" + guide.GuideId,
                copy.transform,
                useEnglish && !string.IsNullOrWhiteSpace(guide.EnglishSummary) ? guide.EnglishSummary : guide.Summary,
                14,
                FontStyle.Normal,
                layout);
            summary.color = StrategyGuideUiTheme.Text;
            var preferred = PreferredProfile(guide);
            if (preferred != null)
            {
                var startState = UiFactory.Panel("StrategyGuideStartState-" + guide.GuideId, identity.transform, StrategyGuideUiTheme.SurfaceSoft);
                UiFactory.SetWidth(startState, layout.IsCompact ? 128f : 174f);
                UiFactory.Vertical(startState, 10, 2);
                var startLabel = UiFactory.Label("StrategyGuideStartStateLabel-" + guide.GuideId, startState.transform, T("开始状态", "Starting state"), 14, FontStyle.Normal, layout);
                startLabel.color = StrategyGuideUiTheme.MutedText;
                var startValue = UiFactory.Label("StrategyGuideStartStateValue-" + guide.GuideId, startState.transform, T("第 ", "Round ") + preferred.StartRound + T(" 回合 · ", " · Tier ") + preferred.TavernTier + T(" 级", string.Empty), 18, FontStyle.Bold, layout);
                startValue.color = StrategyGuideUiTheme.WarmText;
            }

            var lineupHeading = UiFactory.Label("StrategyGuideLineupHeading-" + guide.GuideId, panel.transform, T("最终成型阵容 · 从左到右即战斗站位", "Final lineup · left-to-right combat order"), 16, FontStyle.Bold, layout);
            lineupHeading.color = StrategyGuideUiTheme.WarmText;
            UiFactory.SetHeight(lineupHeading.gameObject, 26f);
            BuildFinalComposition(panel.transform, guide);
            BuildMechanics(panel.transform, guide);
            BuildGuideActions(panel.transform, guide);
            return panel;
        }

        private void BuildFinalComposition(Transform parent, StrategyGuideDefinition guide)
        {
            var lineup = UiFactory.Panel("StrategyGuideLineup-" + guide.GuideId, parent, StrategyGuideUiTheme.SurfaceSoft);
            UiFactory.SetHeight(lineup, layout.IsCompact ? 430f : 196f);
            if (layout.IsCompact)
            {
                var compact = UiFactory.Vertical(lineup, 8, 6);
                compact.childControlWidth = true;
                compact.childForceExpandWidth = true;
                foreach (var card in guide.FinalComposition ?? new List<StrategyGuideCardDefinition>())
                {
                    BuildCompactLineupCard(lineup.transform, card);
                }
                return;
            }

            var row = UiFactory.Horizontal(lineup, 8, 8);
            row.childControlWidth = true;
            row.childForceExpandWidth = true;
            foreach (var card in guide.FinalComposition ?? new List<StrategyGuideCardDefinition>())
            {
                BuildLineupCard(lineup.transform, card);
            }
        }

        private void BuildLineupCard(Transform parent, StrategyGuideCardDefinition card)
        {
            var definition = catalogs.Minions.All.FirstOrDefault(item =>
                string.Equals(item.CardId, card.CardId, StringComparison.OrdinalIgnoreCase));
            var slot = UiFactory.Panel("StrategyGuideLineupCard-" + card.PlacementId, parent, StrategyGuideUiTheme.SurfaceSoft);
            UiFactory.SetFlexible(slot, 1f, 0f);
            StrategyGuideUiTheme.ApplySurface(slot, StrategyGuideUiTheme.SurfaceSoft, "slot_lineup");
            StrategyGuideUiTheme.Outline(slot, card.Golden ? StrategyGuideUiTheme.Primary : StrategyGuideUiTheme.BorderStrong, card.Golden);
            UiFactory.Vertical(slot, 4, 3);
            var art = new GameObject("StrategyGuideLineupArt-" + card.PlacementId, typeof(RectTransform), typeof(Image));
            art.transform.SetParent(slot.transform, false);
            UiFactory.SetHeight(art, 132f);
            var image = art.GetComponent<Image>();
            image.sprite = definition == null ? null : CardImageProvider.LoadSprite(definition.ImagePath, definition.CardId, CardKind.Minion);
            image.color = image.sprite == null ? StrategyGuideUiTheme.Felt : Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var name = UiFactory.Label(
                "StrategyGuideLineupName-" + card.PlacementId,
                slot.transform,
                (card.Golden ? T("金色 · ", "Golden · ") : string.Empty) + (definition?.Name ?? card.CardId),
                14,
                FontStyle.Bold,
                layout);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = card.Golden ? StrategyGuideUiTheme.WarmText : StrategyGuideUiTheme.Text;
            UiFactory.SetHeight(name.gameObject, 38f);
        }

        private void BuildCompactLineupCard(Transform parent, StrategyGuideCardDefinition card)
        {
            var definition = catalogs.Minions.All.FirstOrDefault(item =>
                string.Equals(item.CardId, card.CardId, StringComparison.OrdinalIgnoreCase));
            var row = UiFactory.Panel("StrategyGuideLineupCard-" + card.PlacementId, parent, StrategyGuideUiTheme.SurfaceSoft);
            UiFactory.SetHeight(row, 48f);
            StrategyGuideUiTheme.Outline(row, card.Golden ? StrategyGuideUiTheme.Primary : StrategyGuideUiTheme.BorderStrong, card.Golden);
            var label = UiFactory.Label(
                "StrategyGuideLineupName-" + card.PlacementId,
                row.transform,
                (card.Golden ? T("金色 · ", "Golden · ") : string.Empty) + (definition?.Name ?? card.CardId),
                14,
                FontStyle.Bold,
                layout);
            label.color = StrategyGuideUiTheme.Text;
            UiFactory.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(12f, 0f);
        }

        private void BuildMechanics(Transform parent, StrategyGuideDefinition guide)
        {
            var mechanics = UiFactory.Panel("StrategyGuideMechanics-" + guide.GuideId, parent, Color.clear);
            UiFactory.SetHeight(mechanics, layout.IsCompact ? 236f : 88f);
            var values = new[]
            {
                new[] { T("固定英雄", "Hero"), HeroName(guide.HeroCardId) },
                new[] { T("本局种族", "Tribes"), string.Join(" · ", guide.ActiveTribes ?? new List<string>()) },
                new[] { T("大小饰品", "Trinkets"), TrinketRecommendationSummary(guide) },
                new[] { T("黑暗之赐", "Dark Gift"), DarkGiftName(guide) }
            };
            var group = layout.IsCompact
                ? (HorizontalOrVerticalLayoutGroup)UiFactory.Vertical(mechanics, 0, 6)
                : UiFactory.Horizontal(mechanics, 0, 8);
            group.childControlWidth = true;
            group.childForceExpandWidth = true;
            foreach (var value in values)
            {
                var card = UiFactory.Panel("StrategyGuideMechanic", mechanics.transform, StrategyGuideUiTheme.SurfaceSoft);
                UiFactory.SetFlexible(card, 1f, 0f);
                UiFactory.Vertical(card, 10, 2);
                var label = UiFactory.Label("StrategyGuideMechanicLabel", card.transform, value[0], 14, FontStyle.Normal, layout);
                label.color = StrategyGuideUiTheme.MutedText;
                var detail = UiFactory.Label("StrategyGuideMechanicValue", card.transform, value[1], 14, FontStyle.Bold, layout);
                detail.color = StrategyGuideUiTheme.Text;
            }
        }

        private void BuildGuideActions(Transform parent, StrategyGuideDefinition guide)
        {
            var actions = UiFactory.Panel("StrategyGuideProfiles-" + guide.GuideId, parent, Color.clear);
            var profiles = (guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>())
                .Where(item => item != null)
                .ToList();
            var shareProfiles = new[]
                {
                    profiles.FirstOrDefault(item => item.Difficulty == StrategyGuideDifficulties.GuidedDiscover),
                    profiles.FirstOrDefault(item => item.Difficulty == StrategyGuideDifficulties.OpenBuild)
                }
                .Where(item => item != null)
                .ToList();
            var utilityActionCount = shareProfiles.Count + 1;
            var utilityHeight = resolvedVersion == null
                ? 0f
                : layout.IsCompact
                    ? utilityActionCount * UnityTavernUiStyle.TouchHeight +
                        Math.Max(0, utilityActionCount - 1) * 6f
                    : UnityTavernUiStyle.TouchHeight;
            var utilityGap = resolvedVersion == null ? 0f : 6f;
            UiFactory.SetHeight(
                actions,
                utilityHeight + utilityGap + (layout.IsCompact ? profiles.Count * 54f : 62f));
            var column = UiFactory.Vertical(actions, 0, 6);
            column.childControlWidth = true;
            column.childForceExpandWidth = true;

            if (resolvedVersion != null)
            {
                var utility = UiFactory.Panel("StrategyGuideUtilityActions-" + guide.GuideId, actions.transform, Color.clear);
                UiFactory.SetHeight(utility, utilityHeight);
                var utilityLayout = layout.IsCompact
                    ? (HorizontalOrVerticalLayoutGroup)UiFactory.Vertical(utility, 0, 6)
                    : UiFactory.Horizontal(utility, 0, 8);
                utilityLayout.childControlWidth = true;
                utilityLayout.childForceExpandWidth = true;
                foreach (var profile in shareProfiles)
                {
                    var captured = profile;
                    var preview = UiFactory.Button(
                        "StrategyGuideSharePreviewButton-" + guide.GuideId + "-" + profile.ProfileId,
                        utility.transform,
                        profile.Difficulty == StrategyGuideDifficulties.GuidedDiscover
                            ? T("初级一图流", "Beginner one-sheet")
                            : T("困难一图流", "Hard one-sheet"),
                        () => OpenShareModal(guide.GuideId, captured.ProfileId),
                        layout);
                    StrategyGuideUiTheme.SecondaryButton(preview);
                    UiFactory.SetFlexible(preview.gameObject, 1f, 0f);
                    UnityTavernUiStyle.SetPreferredHeight(preview.gameObject, UnityTavernUiStyle.TouchHeight);
                }
                Button copyCode = null;
                copyCode = UiFactory.Button(
                    "StrategyGuideCopyCodeButton-" + guide.GuideId,
                    utility.transform,
                    T("复制攻略代码", "Copy guide code"),
                    () => CopyPortableCode(guide.GuideId, copyCode),
                    layout);
                StrategyGuideUiTheme.QuietButton(copyCode);
                UiFactory.SetFlexible(copyCode.gameObject, 1f, 0f);
                UnityTavernUiStyle.SetPreferredHeight(copyCode.gameObject, UnityTavernUiStyle.TouchHeight);
            }

            var starts = UiFactory.Panel("StrategyGuideStartActions-" + guide.GuideId, actions.transform, Color.clear);
            var startsLayout = layout.IsCompact
                ? (HorizontalOrVerticalLayoutGroup)UiFactory.Vertical(starts, 0, 6)
                : UiFactory.Horizontal(starts, 0, 8);
            startsLayout.childControlWidth = true;
            startsLayout.childForceExpandWidth = true;
            foreach (var profile in profiles)
            {
                var captured = profile;
                var start = UiFactory.Button(
                    "StrategyGuideStartButton-" + guide.GuideId + "-" + profile.ProfileId,
                    starts.transform,
                    BuildProfileButtonLabel(profile),
                    () => startGuide?.Invoke(guide.GuideId, captured.ProfileId),
                    layout);
                if (profile.Difficulty == StrategyGuideDifficulties.Showcase)
                {
                    StrategyGuideUiTheme.PrimaryButton(start);
                }
                else
                {
                    StrategyGuideUiTheme.SecondaryButton(start);
                }
                UiFactory.SetFlexible(start.gameObject, 1f, 0f);
                UnityTavernUiStyle.SetPreferredHeight(start.gameObject, UnityTavernUiStyle.TouchHeight);
            }
        }

        private StrategyGuideEntryProfileDefinition PreferredProfile(StrategyGuideDefinition guide)
        {
            return (guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>())
                .FirstOrDefault(item => item != null && item.Difficulty == StrategyGuideDifficulties.Showcase)
                ?? guide.EntryProfiles?.FirstOrDefault(item => item != null);
        }

        private string HeroName(string heroCardId)
        {
            var hero = catalogs.Heroes.AllHeroes.FirstOrDefault(item =>
                string.Equals(item.HeroCardId, heroCardId, StringComparison.OrdinalIgnoreCase));
            return hero?.Name ?? heroCardId ?? T("未选择", "Not selected");
        }

        private string TrinketRecommendationSummary(StrategyGuideDefinition guide)
        {
            var lesser = RecommendationIds(
                    guide?.RecommendedLesserTrinketCardIds,
                    guide?.LesserTrinketCardId)
                .Select(TrinketName);
            var greater = RecommendationIds(
                    guide?.RecommendedGreaterTrinketCardIds,
                    guide?.GreaterTrinketCardId)
                .Select(TrinketName);
            return T("小：", "Lesser: ") + string.Join(" / ", lesser) +
                " · " + T("大：", "Greater: ") + string.Join(" / ", greater);
        }

        private static List<string> RecommendationIds(IEnumerable<string> recommendations, string fallbackCardId)
        {
            var values = (recommendations ?? Enumerable.Empty<string>())
                .Where(cardId => !string.IsNullOrWhiteSpace(cardId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (values.Count == 0 && !string.IsNullOrWhiteSpace(fallbackCardId))
            {
                values.Add(fallbackCardId);
            }
            return values;
        }

        private string TrinketName(string cardId)
        {
            var trinket = catalogs.Trinkets.All.FirstOrDefault(item =>
                string.Equals(item.CardId, cardId, StringComparison.OrdinalIgnoreCase));
            return trinket?.Name ?? cardId ?? T("未选择", "Not selected");
        }

        private string DarkGiftName(StrategyGuideDefinition guide)
        {
            var key = (guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>())
                .Where(item => item != null)
                .SelectMany(item => item.DarkGiftAttachments ?? new List<StrategyGuideDarkGiftAttachment>())
                .FirstOrDefault(item => item != null)?.GiftResearchKey;
            var gift = catalogs.DarkGifts.All.FirstOrDefault(item =>
                string.Equals(item.ResearchKey, key, StringComparison.OrdinalIgnoreCase));
            return gift?.DisplayName ?? key ?? T("无", "None");
        }

        private string BuildProfileButtonLabel(StrategyGuideEntryProfileDefinition profile)
        {
            var title = useEnglish && !string.IsNullOrWhiteSpace(profile.EnglishTitle)
                ? profile.EnglishTitle
                : profile.Title;
            if (profile.Difficulty == StrategyGuideDifficulties.GuidedDiscover)
            {
                return title + "\n" + T("受控找牌 · 无撤销", "Controlled offers · No undo");
            }

            if (profile.Difficulty == StrategyGuideDifficulties.Showcase)
            {
                return title + "\n" + T("成型教学 · 1 次撤销", "Finished board · 1 undo");
            }

            if (profile.Difficulty == StrategyGuideDifficulties.OpenBuild)
            {
                return title + "\n" + T("大饰品池教学 · 无撤销", "Greater Trinket lesson · No undo");
            }

            return title;
        }

        private void CopyPortableCode(string guideId, Button source)
        {
            if (resolvedVersion == null)
            {
                return;
            }

            GUIUtility.systemCopyBuffer = StrategyGuidePortableCodeService.ExportGuide(
                catalog,
                guideId,
                resolvedVersion);
            var label = source == null ? null : source.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = T("已复制 ✓", "Copied ✓");
            }
        }

        private void OpenShareModal(string guideId, string profileId)
        {
            if (shareOverlay != null || resolvedVersion == null)
            {
                return;
            }

            var model = StrategyGuideShareCardService.Create(
                catalog,
                guideId,
                profileId,
                resolvedVersion,
                catalogs,
                useEnglish);
            shareOverlay = new StrategyGuideShareCardView(
                root,
                model,
                layout,
                useEnglish,
                CloseShareModal).Build();
        }

        private void CloseShareModal()
        {
            if (shareOverlay == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(shareOverlay);
            }
            else
            {
                UnityEngine.Object.Destroy(shareOverlay);
            }
#else
            UnityEngine.Object.Destroy(shareOverlay);
#endif
            shareOverlay = null;
        }

        private void OpenImportModal()
        {
            if (importOverlay != null || resolvedVersion == null || startImportedGuide == null)
            {
                return;
            }

            importOverlay = UiFactory.Panel(
                "StrategyGuideImportOverlay",
                root,
                UnityTavernUiStyle.WithAlpha(Color.black, 0.78f));
            importOverlay.GetComponent<Image>().raycastTarget = true;
            UiFactory.Stretch(importOverlay.GetComponent<RectTransform>());

            var card = UiFactory.Panel(
                "StrategyGuideImportCard",
                importOverlay.transform,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.99f));
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(
                Mathf.Clamp(layout.Width - 32f, 320f, layout.IsCompact ? 520f : 720f),
                Mathf.Clamp(layout.Height - 32f, 390f, layout.IsCompact ? 430f : 470f));
            UnityTavernUiStyle.ConfigureOutline(card, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.72f), new Vector2(2f, -2f));
            UiFactory.Vertical(card, layout.IsCompact ? 14 : 20, 10);

            var title = UiFactory.Label(
                "StrategyGuideImportTitle",
                card.transform,
                T("导入一图流代码", "Import lineup code"),
                layout.IsCompact ? 22 : 26,
                FontStyle.Bold,
                layout);
            title.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetHeight(title.gameObject, 40f);

            var hint = UiFactory.Label(
                "StrategyGuideImportHint",
                card.transform,
                T("粘贴 LHSG1 代码。验证版本与阵容后，再选择可用难度。", "Paste an LHSG1 code. Validate its version and lineup, then choose an available entry."),
                14,
                FontStyle.Normal,
                layout);
            hint.color = UnityTavernUiStyle.MutedText;
            UiFactory.SetHeight(hint.gameObject, 42f);

            var input = BuildImportInput(card.transform);
            var profileChoiceContent = UiFactory.ScrollView(
                "StrategyGuideImportProfileChoices",
                card.transform,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.TableDark, 0.86f),
                out var profileChoiceScroll,
                layout);
            UnityTavernUiStyle.SetPreferredHeight(
                profileChoiceScroll.gameObject,
                layout.IsCompact ? 150f : 180f);
            var profileChoiceLayout = UiFactory.Vertical(profileChoiceContent.gameObject, 8, 6);
            profileChoiceLayout.childControlWidth = true;
            profileChoiceLayout.childForceExpandWidth = true;
            profileChoiceLayout.childControlHeight = true;
            profileChoiceLayout.childForceExpandHeight = false;
            profileChoiceScroll.gameObject.SetActive(false);

            var summary = UiFactory.Label(
                "StrategyGuideImportSummary",
                card.transform,
                T("尚未验证", "Not validated"),
                14,
                FontStyle.Bold,
                layout);
            summary.color = UnityTavernUiStyle.MutedText;
            UiFactory.SetHeight(summary.gameObject, 64f);

            var buttons = UiFactory.Panel("StrategyGuideImportActions", card.transform, Color.clear);
            UiFactory.SetHeight(buttons, UnityTavernUiStyle.TouchHeight);
            var buttonRow = UiFactory.Horizontal(buttons, 0, 8);
            buttonRow.childControlWidth = true;
            buttonRow.childForceExpandWidth = true;

            var cancel = UiFactory.Button(
                "StrategyGuideImportCancelButton",
                buttons.transform,
                T("取消", "Cancel"),
                CloseImportModal,
                layout);
            UnityTavernUiStyle.ConfigureButton(cancel, UnityTavernUiStyle.ArcaneBlue, false);

            StrategyGuideImportResult validated = null;
            Button start = null;
            Button validate = null;
            validate = UiFactory.Button(
                "StrategyGuideImportValidateButton",
                buttons.transform,
                T("验证代码", "Validate"),
                () =>
                {
                    validated = StrategyGuidePortableCodeService.Import(input.text, resolvedVersion);
                    start.gameObject.SetActive(true);
                    start.interactable = validated.IsCompatible && validated.Profile != null;
                    input.gameObject.SetActive(true);
                    profileChoiceScroll.gameObject.SetActive(false);
                    if (validated.IsCompatible)
                    {
                        var guideTitle = useEnglish && !string.IsNullOrWhiteSpace(validated.Guide.EnglishTitle)
                            ? validated.Guide.EnglishTitle
                            : validated.Guide.Title;
                        if (validated.Profile == null)
                        {
                            input.gameObject.SetActive(false);
                            profileChoiceScroll.gameObject.SetActive(true);
                            start.gameObject.SetActive(false);
                            summary.text = T("验证通过，请选择进入难度：", "Validated. Choose an entry: ") +
                                guideTitle + " · " + validated.Payload.GameVersionId;
                            Button firstChoice = null;
                            foreach (var profile in validated.Guide.EntryProfiles.Where(item => item != null))
                            {
                                var captured = profile;
                                var choice = UiFactory.Button(
                                    "StrategyGuideImportProfileButton-" + profile.ProfileId,
                                    profileChoiceContent,
                                    BuildProfileButtonLabel(profile),
                                    () =>
                                    {
                                        validated.Profile = captured;
                                        var accepted = validated;
                                        CloseImportModal();
                                        startImportedGuide(accepted);
                                    },
                                    layout);
                                UnityTavernUiStyle.ConfigureButton(
                                    choice,
                                    profile.Difficulty == StrategyGuideDifficulties.GuidedDiscover
                                        ? UnityTavernUiStyle.Gold
                                        : profile.Difficulty == StrategyGuideDifficulties.OpenBuild
                                            ? UnityTavernUiStyle.ArcaneBlue
                                            : UnityTavernUiStyle.SuccessGreen,
                                    true);
                                UnityTavernUiStyle.SetPreferredHeight(choice.gameObject, UnityTavernUiStyle.TouchHeight);
                                firstChoice ??= choice;
                            }
                            if (firstChoice != null && EventSystem.current != null)
                            {
                                EventSystem.current.SetSelectedGameObject(firstChoice.gameObject);
                            }
                        }
                        else
                        {
                            var profileTitle = useEnglish && !string.IsNullOrWhiteSpace(validated.Profile.EnglishTitle)
                                ? validated.Profile.EnglishTitle
                                : validated.Profile.Title;
                            summary.text = T("验证通过：", "Validated: ") + guideTitle + " · " + profileTitle + " · " + validated.Payload.GameVersionId;
                        }
                        summary.color = UnityTavernUiStyle.SuccessGreen;
                        validate.interactable = false;
                    }
                    else
                    {
                        summary.text = string.Join("\n", validated.Diagnostics.Select(item => item.Message));
                        summary.color = UnityTavernUiStyle.DangerRed;
                    }
                },
                layout);
            UnityTavernUiStyle.ConfigureButton(validate, UnityTavernUiStyle.Gold, true);

            start = UiFactory.Button(
                "StrategyGuideImportStartButton",
                buttons.transform,
                T("开始试玩", "Start challenge"),
                () =>
                {
                    if (validated == null || !validated.IsCompatible)
                    {
                        return;
                    }

                    var accepted = validated;
                    CloseImportModal();
                    startImportedGuide(accepted);
                },
                layout);
            start.interactable = false;
            UnityTavernUiStyle.ConfigureButton(start, UnityTavernUiStyle.SuccessGreen, true);

            importOverlay.AddComponent<UnityFocusTrap>().Activate(input.gameObject);
        }

        private InputField BuildImportInput(Transform parent)
        {
            var inputObject = new GameObject(
                "StrategyGuideImportCodeInput",
                typeof(RectTransform),
                typeof(Image),
                typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            UiFactory.SetHeight(inputObject, layout.IsCompact ? 150f : 180f);
            var input = inputObject.GetComponent<InputField>();
            input.lineType = InputField.LineType.MultiLineNewline;
            input.characterLimit = StrategyGuidePortableCodeService.MaxCodeCharacters;
            input.caretColor = UnityTavernUiStyle.TextLight;
            input.selectionColor = UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.36f);

            var text = UiFactory.Label("StrategyGuideImportCodeText", inputObject.transform, string.Empty, 14, FontStyle.Normal, layout);
            text.alignment = TextAnchor.UpperLeft;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            UiFactory.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(12f, 10f);
            text.rectTransform.offsetMax = new Vector2(-12f, -10f);
            input.textComponent = text;

            var placeholder = UiFactory.Label(
                "StrategyGuideImportCodePlaceholder",
                inputObject.transform,
                T("在此粘贴 LHSG1...", "Paste LHSG1... here"),
                14,
                FontStyle.Normal,
                layout);
            placeholder.color = UnityTavernUiStyle.MutedText;
            placeholder.alignment = TextAnchor.UpperLeft;
            UiFactory.Stretch(placeholder.rectTransform);
            placeholder.rectTransform.offsetMin = new Vector2(12f, 10f);
            placeholder.rectTransform.offsetMax = new Vector2(-12f, -10f);
            input.placeholder = placeholder;
            UnityTavernUiStyle.ConfigureInputField(input, UnityTavernUiStyle.ArcaneBlue);
            return input;
        }

        private void CloseImportModal()
        {
            if (importOverlay == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(importOverlay);
            }
            else
            {
                UnityEngine.Object.Destroy(importOverlay);
            }
#else
            UnityEngine.Object.Destroy(importOverlay);
#endif
            importOverlay = null;
        }

        private void BuildHeroPortrait(
            Transform parent,
            StrategyGuideDefinition guide,
            float width = 96f,
            float height = 126f)
        {
            var frame = UiFactory.Panel("StrategyGuideHero-" + guide.GuideId, parent, StrategyGuideUiTheme.SurfaceSoft);
            StrategyGuideUiTheme.Outline(frame, StrategyGuideUiTheme.BorderStrong);
            UnityTavernUiStyle.SetFixedSize(frame, width, height);
            var hero = catalogs.Heroes.AllHeroes.FirstOrDefault(item =>
                string.Equals(item.HeroCardId, guide.HeroCardId, StringComparison.OrdinalIgnoreCase));
            var sprite = hero == null ? null : CardImageProvider.LoadSprite(hero.ImagePath, hero.HeroCardId, CardKind.Hero);
            if (sprite == null)
            {
                var fallback = UiFactory.Label("StrategyGuideHeroFallback", frame.transform, T("英雄", "Hero"), 14, FontStyle.Bold, layout);
                fallback.alignment = TextAnchor.MiddleCenter;
                fallback.color = StrategyGuideUiTheme.MutedText;
                UiFactory.Stretch(fallback.rectTransform);
                return;
            }

            var imageObject = new GameObject("StrategyGuideHeroImage", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(frame.transform, false);
            UiFactory.Stretch(imageObject.GetComponent<RectTransform>());
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private string T(string chinese, string english)
        {
            return useEnglish ? english : chinese;
        }
    }
}
