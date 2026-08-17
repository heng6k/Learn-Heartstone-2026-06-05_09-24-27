using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class StrategyGuideAuthoringEditorView
    {
        private const int BasicStep = 0;
        private const int CompositionStep = 1;
        private const int ProfilesStep = 2;
        private const int FreezeStep = 3;

        private readonly Transform root;
        private readonly StrategyGuideCatalog catalog;
        private readonly GameCatalogSet catalogs;
        private readonly ResolvedGameVersion version;
        private readonly FileStrategyGuideAuthoringRepository repository;
        private readonly Action close;
        private readonly Action<StrategyGuideImportResult> startImportedGuide;
        private readonly bool useEnglish;
        private readonly UnityTavernLayoutContext layout;
        private readonly StrategyGuideAuthoringDraft draft;
        private readonly List<Button> stepButtons = new List<Button>();
        private readonly List<GameObject> contextRows = new List<GameObject>();
        private readonly List<Text> contextStateLabels = new List<Text>();
        private readonly List<Text> contextStatusLabels = new List<Text>();
        private readonly HashSet<string> expandedProfileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private GameObject shell;
        private GameObject pickerOverlay;
        private GameObject shareOverlay;
        private Transform stepContent;
        private ScrollRect stepScroll;
        private Text status;
        private Button previousButton;
        private Button nextButton;
        private Button freezeButton;
        private StrategyGuideAuthoringOperationRunner operationRunner;
        private bool freezeInProgress;
        private int activeStep;

        public StrategyGuideAuthoringEditorView(
            Transform root,
            StrategyGuideCatalog catalog,
            GameCatalogSet catalogs,
            ResolvedGameVersion version,
            StrategyGuideDefinition template,
            FileStrategyGuideAuthoringRepository repository,
            Action close,
            bool useEnglish = false,
            UnityTavernLayoutContext? layoutContext = null,
            string draftId = null,
            StrategyGuideAuthoringDraft existingDraft = null,
            Action<StrategyGuideImportResult> startImportedGuide = null)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.version = version ?? throw new ArgumentNullException(nameof(version));
            this.catalogs = version.Snapshot?.ForLanguage(useEnglish)
                ?? catalogs
                ?? throw new ArgumentNullException(nameof(catalogs));
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.close = close;
            this.startImportedGuide = startImportedGuide;
            this.useEnglish = useEnglish;
            layout = layoutContext ?? UnityTavernLayoutContext.FromRoot(root);
            if (template == null && existingDraft?.Guide == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            draft = existingDraft == null
                ? new StrategyGuideAuthoringDraft
                {
                    DraftId = string.IsNullOrWhiteSpace(draftId) ? CreateDraftId(template.GuideId) : draftId,
                    Guide = Clone(template)
                }
                : Clone(existingDraft);
            if (existingDraft == null)
            {
                draft.Guide.RevisionId = "authoring-working-copy";
            }
        }

        public StrategyGuideAuthoringFreezeResult LastFreezeResult { get; private set; }

        public GameObject Build()
        {
            shell = UiFactory.Panel(
                "StrategyGuideAuthoringEditor",
                root,
                StrategyGuideUiTheme.Background);
            operationRunner = shell.AddComponent<StrategyGuideAuthoringOperationRunner>();
            UiFactory.Stretch(shell.GetComponent<RectTransform>());
            UiFactory.Vertical(
                shell,
                layout.IsShortLandscape ? ShortInt(4f) : layout.IsCompact ? 8 : 12,
                layout.IsShortLandscape ? ShortInt(4f) : 8);

            BuildHeader(shell.transform);
            BuildStepNavigation(shell.transform);
            var workspace = UiFactory.Panel(
                "StrategyGuideAuthoringWorkspace",
                shell.transform,
                StrategyGuideUiTheme.Workspace);
            UiFactory.SetFlexible(workspace, 1f, 1f);
            var workspaceLayout = UiFactory.Horizontal(workspace, layout.IsCompact ? 0 : 10, layout.IsCompact ? 0 : 12);
            workspaceLayout.childControlWidth = true;
            workspaceLayout.childControlHeight = true;
            workspaceLayout.childForceExpandWidth = layout.IsCompact;
            workspaceLayout.childForceExpandHeight = true;
            if (!layout.IsCompact)
            {
                BuildContextRail(workspace.transform);
            }
            stepContent = UiFactory.ScrollView(
                "StrategyGuideAuthoringStepScroll",
                workspace.transform,
                StrategyGuideUiTheme.Workspace,
                out stepScroll,
                layout,
                horizontal: false);
            var contentLayout = UiFactory.Vertical(
                stepContent.gameObject,
                layout.IsShortLandscape ? ShortInt(4f) : layout.IsCompact ? 10 : 16,
                layout.IsShortLandscape ? ShortInt(4f) : layout.IsCompact ? 8 : 12);
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            BuildFooter(shell.transform);
            ShowStep(BasicStep);
            SaveDraft(T("已创建本地草稿，编辑字段后会自动保存。", "Local draft created. Fields autosave after editing."));
            return shell;
        }

        private void BuildContextRail(Transform parent)
        {
            var rail = UiFactory.Panel(
                "StrategyGuideAuthoringContextRail",
                parent,
                StrategyGuideUiTheme.Surface);
            var railWidth = layout.CanvasUnitsForPhysicalPixels(Mathf.Clamp(layout.Width * 0.2f, 240f, 320f));
            var railElement = rail.GetComponent<LayoutElement>() ?? rail.AddComponent<LayoutElement>();
            railElement.minWidth = railWidth;
            railElement.preferredWidth = railWidth;
            railElement.flexibleWidth = 0f;
            railElement.layoutPriority = 2;
            StrategyGuideUiTheme.ApplySurface(rail, StrategyGuideUiTheme.Surface, "panel_strategy_rail");
            StrategyGuideUiTheme.Outline(rail, StrategyGuideUiTheme.BorderStrong);
            UiFactory.Vertical(rail, 14, 10);

            var title = UiFactory.Label(
                "StrategyGuideAuthoringContextTitle",
                rail.transform,
                useEnglish && !string.IsNullOrWhiteSpace(draft.Guide.EnglishTitle)
                    ? draft.Guide.EnglishTitle
                    : draft.Guide.Title,
                20,
                FontStyle.Bold,
                layout);
            title.color = StrategyGuideUiTheme.WarmText;
            UiFactory.SetHeight(title.gameObject, 54f);
            var versionBadge = UiFactory.Label(
                "StrategyGuideAuthoringContextVersion",
                rail.transform,
                draft.Guide.GameVersionId + " · " + T("本地草稿", "Local draft"),
                14,
                FontStyle.Bold,
                layout);
            versionBadge.color = StrategyGuideUiTheme.FocusSoft;
            UiFactory.SetHeight(versionBadge.gameObject, 28f);

            var labels = new[]
            {
                T("身份与版本", "Identity and version"),
                T("7 格阵容", "Seven-slot lineup"),
                T("进入方式", "Entry profiles"),
                T("发布校验", "Freeze checks")
            };
            foreach (var labelText in labels)
            {
                var row = UiFactory.Panel("StrategyGuideAuthoringContextStep-" + contextRows.Count, rail.transform, StrategyGuideUiTheme.SurfaceSoft);
                UiFactory.SetHeight(row, 58f);
                var rowLayout = UiFactory.Horizontal(row, 8, 8);
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.childForceExpandWidth = false;
                var state = UiFactory.Label("StrategyGuideAuthoringContextState", row.transform, (contextRows.Count + 1).ToString(), 16, FontStyle.Bold, layout);
                state.alignment = TextAnchor.MiddleCenter;
                state.color = StrategyGuideUiTheme.MutedText;
                UnityTavernUiStyle.SetFixedSize(state.gameObject, 34f, 34f);
                var copy = UiFactory.Panel("StrategyGuideAuthoringContextCopy", row.transform, Color.clear);
                UiFactory.SetFlexible(copy, 1f, 0f);
                UiFactory.Vertical(copy, 0, 0);
                var label = UiFactory.Label("StrategyGuideAuthoringContextLabel", copy.transform, labelText, 14, FontStyle.Bold, layout);
                label.color = StrategyGuideUiTheme.Text;
                var stateCopy = UiFactory.Label("StrategyGuideAuthoringContextStatus", copy.transform, T("未开始", "Not started"), 14, FontStyle.Normal, layout);
                stateCopy.color = StrategyGuideUiTheme.MutedText;
                contextRows.Add(row);
                contextStateLabels.Add(state);
                contextStatusLabels.Add(stateCopy);
            }
        }

        private void BuildHeader(Transform parent)
        {
            var header = UiFactory.Panel(
                "StrategyGuideAuthoringHeader",
                parent,
                StrategyGuideUiTheme.Workspace);
            UiFactory.SetHeight(header, layout.IsShortLandscape ? ShortUnits(48f) : layout.IsCompact ? 76f : 84f);
            StrategyGuideUiTheme.ApplySurface(header, StrategyGuideUiTheme.Workspace, "panel_workspace");
            StrategyGuideUiTheme.Outline(header, StrategyGuideUiTheme.BorderStrong);
            var row = UiFactory.Horizontal(
                header,
                layout.IsShortLandscape ? ShortInt(4f) : layout.IsCompact ? 8 : 12,
                layout.IsShortLandscape ? ShortInt(4f) : 10);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;

            var copy = UiFactory.Panel("StrategyGuideAuthoringHeaderCopy", header.transform, Color.clear);
            UiFactory.SetFlexible(copy, 1f, 0f);
            UiFactory.Vertical(copy, 0, 2);
            var title = UiFactory.Label(
                "StrategyGuideAuthoringTitle",
                copy.transform,
                T("本地一图流创作", "Local lineup authoring"),
                layout.IsShortLandscape ? 18 : layout.IsCompact ? 22 : 28,
                FontStyle.Bold,
                layout);
            title.color = StrategyGuideUiTheme.WarmText;
            status = UiFactory.Label(
                "StrategyGuideAuthoringStatus",
                copy.transform,
                T("正在准备草稿…", "Preparing draft..."),
                14,
                FontStyle.Bold,
                layout);
            status.color = StrategyGuideUiTheme.MutedText;
            status.gameObject.SetActive(!layout.IsShortLandscape);

            var closeButton = UiFactory.Button(
                "StrategyGuideAuthoringCloseButton",
                header.transform,
                T("返回攻略", "Back to guides"),
                () => close?.Invoke(),
                layout);
            StrategyGuideUiTheme.QuietButton(closeButton);
            UiFactory.SetWidth(closeButton.gameObject, layout.IsShortLandscape ? ShortUnits(112f) : layout.IsCompact ? 112f : 148f);
        }

        private void BuildStepNavigation(Transform parent)
        {
            var navigation = UiFactory.Panel(
                "StrategyGuideAuthoringSteps",
                parent,
                StrategyGuideUiTheme.SurfaceSoft);
            UiFactory.SetHeight(navigation, layout.IsShortLandscape ? ShortUnits(48f) : layout.IsCompact ? 56f : 64f);
            StrategyGuideUiTheme.Outline(navigation, StrategyGuideUiTheme.Border);
            var row = UiFactory.Horizontal(navigation, 0, layout.IsShortLandscape ? ShortInt(4f) : layout.IsCompact ? 4 : 8);
            row.childControlWidth = true;
            row.childForceExpandWidth = true;

            var labels = new[]
            {
                T("1 基本", "1 Basics"),
                T("2 阵容", "2 Lineup"),
                T("3 难度", "3 Entries"),
                T("4 冻结", "4 Freeze")
            };
            for (var index = 0; index < labels.Length; index += 1)
            {
                var captured = index;
                var button = UiFactory.Button(
                    "StrategyGuideAuthoringStepButton-" + index,
                    navigation.transform,
                    labels[index],
                    () => ShowStep(captured),
                    layout);
                StrategyGuideUiTheme.SecondaryButton(button);
                stepButtons.Add(button);
            }
        }

        private void BuildFooter(Transform parent)
        {
            var footer = UiFactory.Panel(
                "StrategyGuideAuthoringFooter",
                parent,
                StrategyGuideUiTheme.Background);
            UiFactory.SetHeight(footer, layout.IsShortLandscape ? ShortUnits(58f) : layout.IsCompact ? 56f : 64f);
            StrategyGuideUiTheme.Outline(footer, StrategyGuideUiTheme.Border);
            var row = UiFactory.Horizontal(
                footer,
                layout.IsShortLandscape ? ShortInt(4f) : layout.IsCompact ? 0 : 8,
                layout.IsShortLandscape ? ShortInt(4f) : 8);
            row.childControlWidth = true;
            row.childForceExpandWidth = layout.IsCompact;

            if (!layout.IsCompact)
            {
                var autosave = UiFactory.Label(
                    "StrategyGuideAuthoringAutosaveHint",
                    footer.transform,
                    T("自动保存已开启 · 本地草稿不会覆盖冻结 revision", "Autosave is on · local drafts never overwrite frozen revisions"),
                    14,
                    FontStyle.Normal,
                    layout);
                autosave.color = StrategyGuideUiTheme.MutedText;
                UiFactory.SetFlexible(autosave.gameObject, 1f, 0f);
            }

            previousButton = UiFactory.Button(
                "StrategyGuideAuthoringPreviousButton",
                footer.transform,
                T("上一步", "Previous"),
                () => ShowStep(Mathf.Max(BasicStep, activeStep - 1)),
                layout);
            StrategyGuideUiTheme.QuietButton(previousButton);
            if (layout.IsShortLandscape)
            {
                UnityTavernUiStyle.ApplyTavernButtonSkin(previousButton, true);
            }
            UiFactory.SetWidth(previousButton.gameObject, layout.IsShortLandscape ? ShortUnits(112f) : 112f);

            var save = UiFactory.Button(
                "StrategyGuideAuthoringSaveButton",
                footer.transform,
                T("保存草稿", "Save draft"),
                () => SaveDraft(T("草稿已保存。", "Draft saved.")),
                layout);
            StrategyGuideUiTheme.SecondaryButton(save);
            if (layout.IsShortLandscape)
            {
                UnityTavernUiStyle.ApplyTavernButtonSkin(save, true);
            }
            UiFactory.SetWidth(save.gameObject, layout.IsShortLandscape ? ShortUnits(126f) : 126f);

            nextButton = UiFactory.Button(
                "StrategyGuideAuthoringNextButton",
                footer.transform,
                T("下一步", "Next"),
                () => ShowStep(Mathf.Min(FreezeStep, activeStep + 1)),
                layout);
            StrategyGuideUiTheme.PrimaryButton(nextButton);
            if (layout.IsShortLandscape)
            {
                UnityTavernUiStyle.ApplyTavernButtonSkin(nextButton, false);
            }
            UiFactory.SetWidth(nextButton.gameObject, layout.IsShortLandscape ? ShortUnits(112f) : 112f);

            freezeButton = UiFactory.Button(
                "StrategyGuideAuthoringFreezeButton",
                footer.transform,
                T("校验并冻结", "Validate and freeze"),
                Freeze,
                layout);
            StrategyGuideUiTheme.SuccessButton(freezeButton);
            if (layout.IsShortLandscape)
            {
                UnityTavernUiStyle.ApplyTavernButtonSkin(freezeButton, false);
            }
            UiFactory.SetWidth(freezeButton.gameObject, layout.IsShortLandscape ? ShortUnits(148f) : 148f);
        }

        private void ShowStep(int step)
        {
            var nextStep = Mathf.Clamp(step, BasicStep, FreezeStep);
            var preserveScrollPosition = stepContent.childCount > 0 && nextStep == activeStep;
            var scrollPosition = preserveScrollPosition
                ? stepScroll.normalizedPosition
                : new Vector2(0f, 1f);
            stepScroll.StopMovement();
            activeStep = nextStep;
            ClearChildren(stepContent);
            switch (activeStep)
            {
                case BasicStep:
                    BuildBasicStep(stepContent);
                    break;
                case CompositionStep:
                    BuildCompositionStep(stepContent);
                    break;
                case ProfilesStep:
                    BuildProfilesStep(stepContent);
                    break;
                default:
                    BuildFreezeStep(stepContent);
                    break;
            }

            for (var index = 0; index < stepButtons.Count; index += 1)
            {
                StrategyGuideUiTheme.SecondaryButton(stepButtons[index], index == activeStep);
            }
            UpdateContextRail();
            previousButton.gameObject.SetActive(activeStep > BasicStep);
            nextButton.gameObject.SetActive(activeStep < FreezeStep);
            freezeButton.gameObject.SetActive(activeStep == FreezeStep);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(stepScroll.content);
            stepScroll.normalizedPosition = scrollPosition;

            var selectables = stepContent.GetComponentsInChildren<Selectable>(true);
            var first = selectables.FirstOrDefault(item => !(item is InputField)) ?? selectables.FirstOrDefault();
            if (first != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }

        private void UpdateContextRail()
        {
            for (var index = 0; index < contextRows.Count; index += 1)
            {
                var completed = index < activeStep;
                var current = index == activeStep;
                StrategyGuideUiTheme.ApplySurface(
                    contextRows[index],
                    current ? StrategyGuideUiTheme.SurfaceSelected : StrategyGuideUiTheme.SurfaceSoft,
                    "panel_content_card");
                StrategyGuideUiTheme.Outline(
                    contextRows[index],
                    current ? StrategyGuideUiTheme.Focus : StrategyGuideUiTheme.Border,
                    current);
                contextStateLabels[index].text = completed ? "✓" : (index + 1).ToString();
                contextStateLabels[index].color = completed
                    ? StrategyGuideUiTheme.Success
                    : current
                        ? StrategyGuideUiTheme.FocusSoft
                        : StrategyGuideUiTheme.MutedText;
                contextStatusLabels[index].text = completed
                    ? T("已完成", "Complete")
                    : current
                        ? T("当前正在编辑", "Editing now")
                        : T("未开始", "Not started");
            }
        }

        private void BuildBasicStep(Transform parent)
        {
            var panel = StepPanel("StrategyGuideAuthoringBasicStep", parent);
            SectionTitle(panel.transform, T("基本信息", "Basics"), T(
                "标题和简介会出现在攻略列表与分享图；内部身份继承模板，避免误改版本。",
                "Title and summary appear in the guide list and share card. Internal identity stays tied to the template."));
            BuildTextField(
                panel.transform,
                "StrategyGuideAuthoringTitleInput",
                T("攻略标题", "Guide title"),
                useEnglish ? draft.Guide.EnglishTitle : draft.Guide.Title,
                false,
                value =>
                {
                    if (useEnglish)
                    {
                        draft.Guide.EnglishTitle = value;
                    }
                    else
                    {
                        draft.Guide.Title = value;
                    }
                    SaveDraft(T("标题已自动保存。", "Title autosaved."));
                });
            BuildTextField(
                panel.transform,
                "StrategyGuideAuthoringSummaryInput",
                T("一句话说明", "Summary"),
                useEnglish ? draft.Guide.EnglishSummary : draft.Guide.Summary,
                true,
                value =>
                {
                    if (useEnglish)
                    {
                        draft.Guide.EnglishSummary = value;
                    }
                    else
                    {
                        draft.Guide.Summary = value;
                    }
                    SaveDraft(T("简介已自动保存。", "Summary autosaved."));
                });
            ReadOnlyRow(panel.transform, T("版本", "Version"), draft.Guide.GameVersionId);
            BuildBasicSelectors(panel.transform);
        }

        private void BuildCompositionStep(Transform parent)
        {
            var panel = StepPanel("StrategyGuideAuthoringCompositionStep", parent);
            SectionTitle(panel.transform, T("成型阵容", "Final lineup"), T(
                "可直接替换随从并调整普通 / 金色状态；位置编号和底层场景协议保持稳定。",
                "Replace minions and adjust normal/golden states while placement ids and the scene protocol stay stable."));
            var lineup = UiFactory.Panel(
                "StrategyGuideAuthoringLineup",
                panel.transform,
                StrategyGuideUiTheme.SurfaceSoft);
            UiFactory.SetHeight(lineup, layout.IsShortLandscape ? ShortUnits(516f) : layout.IsCompact ? 516f : 228f);
            var lineupLayout = layout.IsCompact
                ? (HorizontalOrVerticalLayoutGroup)UiFactory.Vertical(lineup, 8, 6)
                : UiFactory.Horizontal(lineup, 8, 8);
            lineupLayout.childControlWidth = true;
            lineupLayout.childForceExpandWidth = true;
            foreach (var card in draft.Guide.FinalComposition ?? new List<StrategyGuideCardDefinition>())
            {
                if (card != null)
                {
                    BuildCompositionCardEditor(lineup.transform, card);
                }
            }

            BuildCoreCardList(panel.transform, true);
            BuildCoreCardList(panel.transform, false);
        }

        private void BuildCompositionCardEditor(Transform parent, StrategyGuideCardDefinition card)
        {
            var cardPanel = UiFactory.Panel(
                "StrategyGuideAuthoringCard-" + card.PlacementId,
                parent,
                StrategyGuideUiTheme.SurfaceSoft);
            StrategyGuideUiTheme.ApplySurface(cardPanel, StrategyGuideUiTheme.SurfaceSoft, "slot_lineup");
            StrategyGuideUiTheme.Outline(
                cardPanel,
                card.Golden ? StrategyGuideUiTheme.Primary : StrategyGuideUiTheme.BorderStrong,
                card.Golden);
            if (layout.IsCompact)
            {
                UiFactory.SetHeight(cardPanel, layout.IsShortLandscape ? ShortUnits(66f) : 66f);
                var compactRow = UiFactory.Horizontal(
                    cardPanel,
                    layout.IsShortLandscape ? ShortInt(6f) : 8,
                    layout.IsShortLandscape ? ShortInt(6f) : 8);
                compactRow.childAlignment = TextAnchor.MiddleCenter;
                compactRow.childForceExpandWidth = false;
                var compactLabel = UiFactory.Label(
                    "StrategyGuideAuthoringCardLabel-" + card.PlacementId,
                    cardPanel.transform,
                    DisplayCard(card),
                    14,
                    FontStyle.Bold,
                    layout);
                compactLabel.color = StrategyGuideUiTheme.Text;
                UiFactory.SetFlexible(compactLabel.gameObject, 1f, 0f);
                BuildCompositionCardActions(cardPanel.transform, card, layout.IsShortLandscape ? ShortUnits(82f) : 82f);
                return;
            }

            UiFactory.SetFlexible(cardPanel, 1f, 0f);
            UiFactory.Vertical(cardPanel, 4, 3);
            var definition = catalogs.Minions.All.FirstOrDefault(item =>
                string.Equals(item.CardId, card.CardId, StringComparison.OrdinalIgnoreCase));
            var art = new GameObject(
                "StrategyGuideAuthoringCardArt-" + card.PlacementId,
                typeof(RectTransform),
                typeof(Image));
            art.transform.SetParent(cardPanel.transform, false);
            UiFactory.SetHeight(art, 126f);
            var image = art.GetComponent<Image>();
            image.sprite = definition == null ? null : CardImageProvider.LoadSprite(definition.ImagePath, definition.CardId, CardKind.Minion);
            image.color = image.sprite == null ? StrategyGuideUiTheme.Felt : Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var label = UiFactory.Label(
                "StrategyGuideAuthoringCardLabel-" + card.PlacementId,
                cardPanel.transform,
                DisplayCard(card),
                14,
                FontStyle.Bold,
                layout);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = StrategyGuideUiTheme.Text;
            UiFactory.SetHeight(label.gameObject, 38f);
            var actions = UiFactory.Panel("StrategyGuideAuthoringCardActions-" + card.PlacementId, cardPanel.transform, Color.clear);
            UiFactory.SetHeight(actions, UnityTavernUiStyle.TouchHeight);
            var actionsLayout = UiFactory.Horizontal(actions, 0, 4);
            actionsLayout.childControlWidth = true;
            actionsLayout.childForceExpandWidth = true;
            BuildCompositionCardActions(actions.transform, card, 0f);
        }

        private void BuildCompositionCardActions(Transform parent, StrategyGuideCardDefinition card, float width)
        {
            var replace = UiFactory.Button(
                "StrategyGuideAuthoringReplaceCardButton-" + card.PlacementId,
                parent,
                T("换牌", "Replace"),
                () => OpenFinalCompositionPicker(card),
                layout);
            StrategyGuideUiTheme.SecondaryButton(replace);
            if (width > 0f)
            {
                UiFactory.SetWidth(replace.gameObject, width);
            }
            var toggle = UiFactory.Button(
                "StrategyGuideAuthoringGoldenButton-" + card.PlacementId,
                parent,
                card.Golden ? T("金色", "Golden") : T("普通", "Normal"),
                () =>
                {
                    ToggleFinalGolden(card.CardId, !card.Golden);
                    SaveDraft(T("成型阵容与简单模式起手已同步保存。", "Final lineup and Showcase setup saved together."));
                    ShowStep(CompositionStep);
                },
                layout);
            if (card.Golden)
            {
                StrategyGuideUiTheme.PrimaryButton(toggle);
            }
            else
            {
                StrategyGuideUiTheme.SecondaryButton(toggle);
            }
            if (width > 0f)
            {
                UiFactory.SetWidth(toggle.gameObject, width + 8f);
            }
        }

        private void BuildProfilesStep(Transform parent)
        {
            var rootPanel = StepPanel("StrategyGuideAuthoringProfilesStep", parent);
            SectionTitle(rootPanel.transform, T("难度入口", "Difficulty entries"), T(
                "每个入口独立保存起始状态；数量完全由数据决定，可继续新增第四种玩法。",
                "Each entry keeps its own starting state. Entry count is data-driven and can grow beyond three."));
            foreach (var profile in draft.Guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>())
            {
                if (profile == null)
                {
                    continue;
                }
                BuildProfileEditor(rootPanel.transform, profile);
            }
        }

        private void BuildProfileEditor(Transform parent, StrategyGuideEntryProfileDefinition profile)
        {
            var panel = UiFactory.Panel(
                "StrategyGuideAuthoringProfile-" + profile.ProfileId,
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.96f));
            UiFactory.Vertical(panel, 10, 8);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.ArcaneBlue, 0.46f),
                new Vector2(1f, -1f));
            var title = UiFactory.Label(
                "StrategyGuideAuthoringProfileTitle-" + profile.ProfileId,
                panel.transform,
                (useEnglish && !string.IsNullOrWhiteSpace(profile.EnglishTitle) ? profile.EnglishTitle : profile.Title) +
                "  ·  " + DifficultyLabel(profile.Difficulty),
                18,
                FontStyle.Bold,
                layout);
            title.color = UnityTavernUiStyle.Gold;
            UiFactory.SetHeight(title.gameObject, layout.IsShortLandscape ? ShortUnits(34f) : 34f);

            var expanded = expandedProfileIds.Contains(profile.ProfileId ?? string.Empty);
            var advanced = UiFactory.Button(
                "StrategyGuideAuthoringAdvancedButton-" + profile.ProfileId,
                panel.transform,
                expanded ? T("收起高级配置", "Hide advanced setup") : T("编辑起始卡牌、黑赐、发牌与对手", "Edit cards, gifts, offers, and opponent"),
                () =>
                {
                    if (!expandedProfileIds.Add(profile.ProfileId ?? string.Empty))
                    {
                        expandedProfileIds.Remove(profile.ProfileId ?? string.Empty);
                    }
                    ShowStep(ProfilesStep);
                },
                layout);
            UnityTavernUiStyle.ConfigureButton(
                advanced,
                expanded ? UnityTavernUiStyle.FocusRing : UnityTavernUiStyle.ArcaneBlue,
                expanded,
                expanded);

            BuildNumberField(panel.transform, profile, "StartRound", T("起始回合", "Start round"), profile.StartRound, 1,
                StrategyGuideAuthoringFreezeService.MaximumAuthoringRound, value => profile.StartRound = value);
            BuildNumberField(panel.transform, profile, "TavernTier", T("酒馆等级", "Tavern tier"), profile.TavernTier, 1, 6,
                value => profile.TavernTier = value);
            BuildNumberField(panel.transform, profile, "Gold", T("起始金币", "Starting gold"), profile.Gold, 0,
                StrategyGuideAuthoringFreezeService.MaximumAuthoringGold, value => profile.Gold = value);
            BuildNumberField(panel.transform, profile, "MaxGold", T("金币上限", "Maximum gold"), profile.MaxGold, 0,
                StrategyGuideAuthoringFreezeService.MaximumAuthoringGold, value => profile.MaxGold = value);

            BuildShapingSpellEditor(panel.transform, profile);

            BuildTextField(
                panel.transform,
                "StrategyGuideAuthoringLearningGoalInput-" + profile.ProfileId,
                T("学习目标", "Learning goal"),
                useEnglish ? profile.EnglishLearningGoal : profile.LearningGoal,
                true,
                value =>
                {
                    if (useEnglish)
                    {
                        profile.EnglishLearningGoal = value;
                    }
                    else
                    {
                        profile.LearningGoal = value;
                    }
                    SaveDraft(T("学习目标已保存。", "Learning goal saved."));
                });
            BuildTextField(
                panel.transform,
                "StrategyGuideAuthoringKeyDecisionsInput-" + profile.ProfileId,
                T("关键判断（每行一条）", "Key decisions (one per line)"),
                string.Join("\n", useEnglish
                    ? profile.EnglishKeyDecisions ?? new List<string>()
                    : profile.KeyDecisions ?? new List<string>()),
                true,
                value =>
                {
                    var decisions = SplitNonEmptyLines(value);
                    if (useEnglish)
                    {
                        profile.EnglishKeyDecisions = decisions;
                    }
                    else
                    {
                        profile.KeyDecisions = decisions;
                    }
                    SaveDraft(T("关键判断已保存。", "Key decisions saved."));
                });

            var acquisition = profile.AcquisitionPlan;
            var schedules = acquisition?.OfferSchedules?.Count ?? 0;
            var disclosure = acquisition?.DiscloseControlledOffers == true
                ? T("受控发牌会向玩家明确标识", "Controlled offers are disclosed to the player")
                : T("仅使用自然固定种子", "Natural seeded offers only");
            SummaryCard(panel.transform, "StrategyGuideAuthoringProfilePlan-" + profile.ProfileId,
                T("找牌与对手摘要", "Acquisition and opponent summary"),
                T("受控计划 ", "Schedules ") + schedules + "  ·  " + disclosure + "\n" +
                T("对手强度回合 ", "Opponent strength round ") + (profile.Opponent?.StrengthRound ?? 0) + "  ·  " +
                T("胜利后可自由探索", "Free exploration is available after victory"));
            if (expanded)
            {
                BuildAdvancedProfileEditor(panel.transform, profile);
            }
        }

        private void BuildFreezeStep(Transform parent)
        {
            var panel = StepPanel("StrategyGuideAuthoringFreezeStep", parent);
            SectionTitle(panel.transform, T("校验与冻结", "Validate and freeze"), T(
                "冻结会生成不可覆盖的 revision；失败时草稿仍会保留，并给出可修复原因。",
                "Freezing creates an immutable revision. If validation fails, the draft remains and recovery guidance is shown."));
            SummaryCard(panel.transform, "StrategyGuideAuthoringFreezeSummary", T("待冻结内容", "Freeze summary"),
                T("阵容：", "Lineup: ") + (draft.Guide.FinalComposition?.Count ?? 0) +
                T(" 张  ·  难度入口：", " cards  ·  Entries: ") + (draft.Guide.EntryProfiles?.Count ?? 0) +
                T(" 个  ·  种族：", "  ·  Tribes: ") + (draft.Guide.ActiveTribes?.Count ?? 0) + "/5");
            SummaryCard(panel.transform, "StrategyGuideAuthoringFreezeRules", T("发布前检查", "Pre-freeze checks"), T(
                "版本、英雄、饰品、黑赐与卡牌引用完整；起始属性不越界；受控发牌已披露；对手与胜利条件可编译。",
                "Version, hero, trinket, dark-gift and card references; starting ranges; controlled-offer disclosure; opponent and victory compilation."));
            var note = UiFactory.Label(
                "StrategyGuideAuthoringFreezeHint",
                panel.transform,
                T("点击下方“校验并冻结”。成功后会显示 revision 与内容指纹。", "Choose Validate and freeze below. The revision and content fingerprint appear on success."),
                14,
                FontStyle.Bold,
                layout);
            note.color = UnityTavernUiStyle.MutedText;
            UiFactory.SetHeight(note.gameObject, layout.IsShortLandscape ? ShortUnits(44f) : 44f);
            if (LastFreezeResult?.Succeeded == true)
            {
                BuildFrozenDelivery(panel.transform);
            }
            else if (LastFreezeResult != null && LastFreezeResult.Diagnostics.Count > 0)
            {
                BuildValidationFailure(panel.transform);
            }
        }

        private void BuildValidationFailure(Transform parent)
        {
            var panel = AdvancedPanel(
                "StrategyGuideAuthoringValidationResults",
                parent,
                T("校验未通过，请按下面项目修改", "Validation needs attention"));
            foreach (var diagnostic in LastFreezeResult.Diagnostics.Take(8))
            {
                var row = UiFactory.Label(
                    "StrategyGuideAuthoringValidationItem",
                    panel.transform,
                    "• " + DiagnosticText(diagnostic),
                    14,
                    FontStyle.Bold,
                    layout);
                row.color = UnityTavernUiStyle.DangerRed;
                UiFactory.SetHeight(row.gameObject, layout.IsShortLandscape ? ShortUnits(44f) : 44f);
            }
        }

        private void BuildFrozenDelivery(Transform parent)
        {
            var panel = AdvancedPanel(
                "StrategyGuideAuthoringFrozenDelivery",
                parent,
                T("冻结完成，可以交付", "Frozen and ready"));
            var identity = UiFactory.Label(
                "StrategyGuideAuthoringFrozenIdentity",
                panel.transform,
                LastFreezeResult.Guide.RevisionId + "\nSHA-256 " + LastFreezeResult.ContentHash,
                14,
                FontStyle.Bold,
                layout);
            identity.color = UnityTavernUiStyle.SuccessGreen;
            UiFactory.SetHeight(identity.gameObject, layout.IsShortLandscape ? ShortUnits(46f) : 46f);

            var actions = UiFactory.Panel("StrategyGuideAuthoringFrozenActions", panel.transform, Color.clear);
            UiFactory.SetHeight(actions, layout.IsShortLandscape ? ShortUnits(48f) : UnityTavernUiStyle.TouchHeight);
            var row = UiFactory.Horizontal(actions, 0, 8);
            row.childControlWidth = true;
            row.childForceExpandWidth = true;

            var copy = UiFactory.Button(
                "StrategyGuideAuthoringCopyFrozenCodeButton",
                actions.transform,
                T("复制一图流代码", "Copy lineup code"),
                CopyFrozenCode,
                layout);
            UnityTavernUiStyle.ConfigureButton(copy, UnityTavernUiStyle.Gold, true);

            var preview = UiFactory.Button(
                "StrategyGuideAuthoringPreviewFrozenShareButton",
                actions.transform,
                T("预览与导出分享图", "Preview and export share card"),
                OpenFrozenShare,
                layout);
            UnityTavernUiStyle.ConfigureButton(preview, UnityTavernUiStyle.ArcaneBlue, true);

            var profileTitle = UiFactory.Label(
                "StrategyGuideAuthoringFrozenProfileTitle",
                panel.transform,
                T("立即试玩一个难度", "Play a difficulty now"),
                15,
                FontStyle.Bold,
                layout);
            profileTitle.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetHeight(profileTitle.gameObject, layout.IsShortLandscape ? ShortUnits(30f) : 30f);
            foreach (var profile in LastFreezeResult.Guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>())
            {
                if (profile == null)
                {
                    continue;
                }
                var captured = profile;
                var start = UiFactory.Button(
                    "StrategyGuideAuthoringStartFrozenButton-" + profile.ProfileId,
                    panel.transform,
                    (useEnglish && !string.IsNullOrWhiteSpace(profile.EnglishTitle) ? profile.EnglishTitle : profile.Title) +
                    " · " + DifficultyLabel(profile.Difficulty),
                    () => StartFrozenProfile(captured.ProfileId),
                    layout);
                start.interactable = startImportedGuide != null;
                UnityTavernUiStyle.ConfigureButton(
                    start,
                    profile.Difficulty == StrategyGuideDifficulties.Showcase
                        ? UnityTavernUiStyle.SuccessGreen
                        : profile.Difficulty == StrategyGuideDifficulties.GuidedDiscover
                            ? UnityTavernUiStyle.Gold
                            : UnityTavernUiStyle.ArcaneBlue,
                    true);
            }
        }

        private void CopyFrozenCode()
        {
            try
            {
                GUIUtility.systemCopyBuffer = FrozenPortableCode();
                SetStatus(T("一图流代码已复制。", "Lineup code copied."), true);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException)
            {
                SetStatus(T("代码生成失败：", "Code generation failed: ") + exception.Message, false);
            }
        }

        private void OpenFrozenShare()
        {
            if (shareOverlay != null)
            {
                return;
            }
            try
            {
                var frozenCatalog = StrategyGuideAuthoringFreezeService.CreateFrozenCatalog(LastFreezeResult, catalog);
                var model = StrategyGuideShareCardService.Create(
                    frozenCatalog,
                    LastFreezeResult.Guide.GuideId,
                    version,
                    catalogs,
                    useEnglish);
                if (!string.Equals(model.RevisionId, LastFreezeResult.Guide.RevisionId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Frozen share-card identity does not match the stored revision.");
                }
                shareOverlay = new StrategyGuideShareCardView(
                    shell.transform,
                    model,
                    layout,
                    useEnglish,
                    CloseFrozenShare).Build();
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException)
            {
                SetStatus(T("分享图生成失败：", "Share card failed: ") + exception.Message, false);
            }
        }

        private void CloseFrozenShare()
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

        private void StartFrozenProfile(string profileId)
        {
            if (startImportedGuide == null)
            {
                return;
            }
            try
            {
                var imported = StrategyGuidePortableCodeService.Import(FrozenPortableCode(), version);
                if (!imported.IsCompatible)
                {
                    throw new InvalidOperationException(string.Join(" | ", imported.Diagnostics.Select(item => item.Message)));
                }
                imported.Profile = imported.Catalog.GetProfile(imported.Guide.GuideId, profileId);
                startImportedGuide(imported);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException)
            {
                SetStatus(T("无法开始试玩：", "Unable to start: ") + exception.Message, false);
            }
        }

        private string FrozenPortableCode()
        {
            var frozenCatalog = StrategyGuideAuthoringFreezeService.CreateFrozenCatalog(LastFreezeResult, catalog);
            var code = StrategyGuidePortableCodeService.ExportGuide(
                frozenCatalog,
                LastFreezeResult.Guide.GuideId,
                version);
            return code;
        }

        private void BuildBasicSelectors(Transform parent)
        {
            var panel = UiFactory.Panel(
                "StrategyGuideAuthoringBasicSelectors",
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.88f));
            UiFactory.Vertical(panel, layout.IsCompact ? 9 : 12, 8);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.ArcaneBlue, 0.42f),
                new Vector2(1f, -1f));
            SectionTitle(
                panel.transform,
                T("英雄、饰品与本局种族", "Hero, trinkets, and tribes"),
                T("选择器只列出当前版本可用内容。自定义阵容没有默认必选种族；模板标记的核心种族仍需保留。", "Pick from current-version content. Blank custom lineups have no required tribe; template-defined core tribes still stay selected."));
            SelectionRow(
                panel.transform,
                "StrategyGuideAuthoringHeroPickerButton",
                T("英雄", "Hero"),
                DisplayHero(draft.Guide.HeroCardId),
                T("更换", "Change"),
                OpenHeroPicker);
            SelectionRow(
                panel.transform,
                "StrategyGuideAuthoringLesserTrinketPickerButton",
                T("小型饰品", "Lesser Trinket"),
                DisplayTrinket(draft.Guide.LesserTrinketCardId),
                T("选择", "Choose"),
                () => OpenTrinketPicker(TrinketSlotKind.Lesser));
            SelectionRow(
                panel.transform,
                "StrategyGuideAuthoringGreaterTrinketPickerButton",
                T("大型饰品", "Greater Trinket"),
                DisplayTrinket(draft.Guide.GreaterTrinketCardId),
                T("选择", "Choose"),
                () => OpenTrinketPicker(TrinketSlotKind.Greater));
            BuildTribeSelector(panel.transform);
        }

        private void BuildTribeSelector(Transform parent)
        {
            var selected = draft.Guide.ActiveTribes ?? (draft.Guide.ActiveTribes = new List<string>());
            var required = new HashSet<string>(
                draft.Guide.RequiredTribes ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);
            var heading = UiFactory.Label(
                "StrategyGuideAuthoringTribeCount",
                parent,
                T("酒馆种族 ", "Tavern tribes ") + selected.Count + "/5" +
                (required.Count > 0 ? T(" · 必须包含：", " · Required: ") + string.Join(", ", required.Select(TribeDisplayName)) : string.Empty),
                14,
                FontStyle.Bold,
                layout);
            heading.color = selected.Count == 5 ? UnityTavernUiStyle.SuccessGreen : UnityTavernUiStyle.DangerRed;
            UiFactory.SetHeight(heading.gameObject, layout.IsShortLandscape ? ShortUnits(30f) : 30f);

            var gridObject = UiFactory.Panel("StrategyGuideAuthoringTribeGrid", parent, Color.clear);
            var columns = layout.IsCompact ? 2 : 5;
            var grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.spacing = layout.IsShortLandscape
                ? new Vector2(ShortUnits(6f), ShortUnits(6f))
                : new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            var tribeCellHeight = layout.IsShortLandscape ? ShortUnits(48f) : UnityTavernUiStyle.TouchHeight;
            grid.cellSize = new Vector2(
                layout.IsShortLandscape ? ShortUnits(152f) : layout.IsCompact ? 152f : 178f,
                tribeCellHeight);
            var rows = Mathf.CeilToInt(TribeAvailabilityRules.PlayableTribes.Length / (float)columns);
            UiFactory.SetHeight(
                gridObject,
                rows * tribeCellHeight + Mathf.Max(0, rows - 1) * (layout.IsShortLandscape ? ShortUnits(6f) : 8f));

            foreach (var tribe in TribeAvailabilityRules.PlayableTribes)
            {
                var id = tribe.ToString();
                var isSelected = selected.Contains(id, StringComparer.OrdinalIgnoreCase);
                var isRequired = required.Contains(id);
                var captured = id;
                var button = UiFactory.Button(
                    "StrategyGuideAuthoringTribeButton-" + id,
                    gridObject.transform,
                    (isSelected ? "✓ " : string.Empty) + TribeDisplayName(id) + (isRequired ? T(" · 必选", " · Required") : string.Empty),
                    () => ToggleTribe(captured),
                    layout);
                UnityTavernUiStyle.ConfigureButton(
                    button,
                    isSelected ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.ArcaneBlue,
                    isSelected,
                    isSelected);
            }
        }

        private void ToggleTribe(string tribe)
        {
            var selected = draft.Guide.ActiveTribes ?? (draft.Guide.ActiveTribes = new List<string>());
            var existing = selected.FirstOrDefault(item => string.Equals(item, tribe, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if ((draft.Guide.RequiredTribes ?? new List<string>()).Contains(tribe, StringComparer.OrdinalIgnoreCase))
                {
                    SetStatus(T("该种族是阵容必需种族，不能移除。", "This tribe is required by the lineup."), false);
                    return;
                }
                selected.Remove(existing);
            }
            else
            {
                if (selected.Count >= 5)
                {
                    SetStatus(T("已经选择 5 个种族；请先移除一个非必选种族。", "Five tribes are already selected; remove a non-required tribe first."), false);
                    return;
                }
                selected.Add(tribe);
            }
            SaveDraft(T("种族配置已保存。", "Tribe setup saved."));
            ShowStep(BasicStep);
        }

        private void SelectionRow(
            Transform parent,
            string buttonName,
            string labelText,
            string value,
            string buttonText,
            Action onClick)
        {
            var rowPanel = UiFactory.Panel(
                buttonName + "Row",
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.92f));
            UiFactory.SetHeight(rowPanel, layout.IsShortLandscape ? ShortUnits(58f) : 58f);
            var row = UiFactory.Horizontal(
                rowPanel,
                layout.IsShortLandscape ? ShortInt(6f) : 8,
                layout.IsShortLandscape ? ShortInt(6f) : 8);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;
            var label = UiFactory.Label(buttonName + "Label", rowPanel.transform, labelText, 14, FontStyle.Bold, layout);
            label.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetWidth(label.gameObject, layout.IsShortLandscape ? ShortUnits(96f) : layout.IsCompact ? 96f : 142f);
            var selected = UiFactory.Label(buttonName + "Value", rowPanel.transform, value ?? T("未选择", "Not selected"), 14, FontStyle.Bold, layout);
            selected.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetFlexible(selected.gameObject, 1f, 0f);
            var button = UiFactory.Button(buttonName, rowPanel.transform, buttonText, () => onClick?.Invoke(), layout);
            UnityTavernUiStyle.ConfigureButton(button, UnityTavernUiStyle.Brass);
            UiFactory.SetWidth(button.gameObject, layout.IsShortLandscape ? ShortUnits(78f) : layout.IsCompact ? 78f : 104f);
        }

        private void OpenHeroPicker()
        {
            ClosePicker();
            pickerOverlay = UnityHeroSelectionModalComponent.CreateModalHost(shell.transform, "UnityHeroSelectionOverlay");
            pickerOverlay.GetComponent<UnityHeroSelectionModalComponent>().Build(
                catalogs.Heroes,
                draft.Guide.HeroCardId,
                false,
                hero =>
                {
                    draft.Guide.HeroCardId = hero.HeroCardId;
                    SaveDraft(T("英雄已保存。", "Hero saved."));
                    ClosePicker();
                    ShowStep(BasicStep);
                },
                ClosePicker,
                T("选择攻略英雄", "Choose guide hero"),
                useEnglish,
                !useEnglish,
                layout);
        }

        private void OpenTrinketPicker(TrinketSlotKind slot)
        {
            var current = slot == TrinketSlotKind.Lesser
                ? draft.Guide.LesserTrinketCardId
                : draft.Guide.GreaterTrinketCardId;
            OpenPicker(
                catalogs.Trinkets.GetOfferableBySlot(slot).Select(TrinketItem),
                current,
                slot == TrinketSlotKind.Lesser ? T("选择小型饰品", "Choose Lesser Trinket") : T("选择大型饰品", "Choose Greater Trinket"),
                T("仅显示当前版本中已实现且可进入候选池的饰品。", "Only implemented, offerable trinkets from this version are shown."),
                item =>
                {
                    if (slot == TrinketSlotKind.Lesser)
                    {
                        draft.Guide.LesserTrinketCardId = item.Id;
                    }
                    else
                    {
                        draft.Guide.GreaterTrinketCardId = item.Id;
                    }
                    SaveDraft(T("饰品已保存。", "Trinket saved."));
                    ShowStep(BasicStep);
                });
        }

        private void OpenFinalCompositionPicker(StrategyGuideCardDefinition card)
        {
            if (card == null)
            {
                return;
            }
            OpenPicker(
                MinionItems(),
                card.CardId,
                T("替换成型阵容随从", "Replace final-lineup minion"),
                T("只显示当前版本酒馆池内随从；位置编号与金色状态保持不变。", "Only current-version pool minions are shown. Placement and golden state stay unchanged."),
                item =>
                {
                    var oldCardId = card.CardId;
                    card.CardKind = StrategyGuideCardKinds.Minion;
                    card.CardId = item.Id;
                    card.Provenance = StrategyGuideProvenance.NormalPool;
                    ReplaceShowcaseCardReference(oldCardId, item.Id);
                    SaveDraft(T("成型阵容已更新。", "Final lineup updated."));
                    ShowStep(CompositionStep);
                });
        }

        private void BuildCoreCardList(Transform parent, bool minions)
        {
            var values = minions
                ? draft.Guide.CoreMinionCardIds ?? (draft.Guide.CoreMinionCardIds = new List<string>())
                : draft.Guide.CoreSpellCardNumbers ?? (draft.Guide.CoreSpellCardNumbers = new List<string>());
            var panel = UiFactory.Panel(
                minions ? "StrategyGuideAuthoringCoreMinionEditor" : "StrategyGuideAuthoringCoreSpellEditor",
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.86f));
            UiFactory.Vertical(panel, 9, 6);
            var heading = UiFactory.Label(
                minions ? "StrategyGuideAuthoringCoreMinionTitle" : "StrategyGuideAuthoringCoreSpellTitle",
                panel.transform,
                minions ? T("核心随从", "Core minions") : T("核心法术", "Core spells"),
                16,
                FontStyle.Bold,
                layout);
            heading.color = UnityTavernUiStyle.Gold;
            UiFactory.SetHeight(heading.gameObject, layout.IsShortLandscape ? ShortUnits(30f) : 30f);
            foreach (var value in values.ToList())
            {
                var captured = value;
                SelectionRow(
                    panel.transform,
                    (minions ? "StrategyGuideAuthoringCoreMinionRemoveButton-" : "StrategyGuideAuthoringCoreSpellRemoveButton-") + StrategyGuideAuthoringPickerModalComponent.SafeName(value),
                    minions ? T("随从", "Minion") : T("法术", "Spell"),
                    minions ? DisplayMinion(value) : DisplaySpell(value),
                    T("移除", "Remove"),
                    () =>
                    {
                        values.Remove(captured);
                        SaveDraft(T("核心列表已保存。", "Core list saved."));
                        ShowStep(CompositionStep);
                    });
            }
            var add = UiFactory.Button(
                minions ? "StrategyGuideAuthoringCoreMinionAddButton" : "StrategyGuideAuthoringCoreSpellAddButton",
                panel.transform,
                minions ? T("＋ 添加核心随从", "+ Add core minion") : T("＋ 添加核心法术", "+ Add core spell"),
                () => OpenPicker(
                    minions ? MinionItems() : SpellItems(false),
                    null,
                    minions ? T("添加核心随从", "Add core minion") : T("添加核心法术", "Add core spell"),
                    T("可按等级或关键词查找；已添加的卡牌会保留标记。", "Filter by tier or keyword. Added cards stay marked."),
                    item =>
                    {
                        if (!values.Contains(item.Id, StringComparer.OrdinalIgnoreCase))
                        {
                            values.Add(item.Id);
                        }
                        SaveDraft(T("核心列表已保存。", "Core list saved."));
                        ShowStep(CompositionStep);
                    },
                    true,
                    values),
                layout);
            UnityTavernUiStyle.ConfigureButton(add, UnityTavernUiStyle.ArcaneBlue);
        }

        private void BuildAdvancedProfileEditor(Transform parent, StrategyGuideEntryProfileDefinition profile)
        {
            BuildPlacementEditor(parent, profile);
            BuildDarkGiftEditor(parent, profile);
            BuildOfferEditor(parent, profile);
            BuildOpponentEditor(parent, profile);
        }

        private void BuildShapingSpellEditor(Transform parent, StrategyGuideEntryProfileDefinition profile)
        {
            var panel = AdvancedPanel(
                "StrategyGuideAuthoringShapingSpells-" + profile.ProfileId,
                parent,
                T("塑造法术类别", "Shaping spell category"));
            var selected = profile.ShapingSpellCardIds?.FirstOrDefault(StrategyGuideShapingSpells.Contains);
            var hint = UiFactory.Label(
                "StrategyGuideAuthoringShapingSpellHint-" + profile.ProfileId,
                panel.transform,
                T(
                    "为这套阵容指定战吼、亡语或回合结束中的一个类别。进入关卡时发两张，之后每个酒馆回合发一张同类法术。",
                    "Choose one category for this lineup: Battlecry, Deathrattle, or end of turn. Two copies are granted initially, then one copy each Tavern turn."),
                14,
                FontStyle.Normal,
                layout);
            hint.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(hint.gameObject, layout.IsShortLandscape ? ShortUnits(72f) : layout.IsCompact ? 72f : 54f);

            SelectionRow(
                panel.transform,
                "StrategyGuideAuthoringShapingSpellButton-" + profile.ProfileId,
                T("阵容专属类别", "Lineup category"),
                selected == null ? T("未选择", "Not selected") : DisplaySpell(selected),
                T("选择", "Choose"),
                () => OpenShapingSpellPicker(profile));
        }

        private void OpenShapingSpellPicker(StrategyGuideEntryProfileDefinition profile)
        {
            var selected = profile.ShapingSpellCardIds ?? (profile.ShapingSpellCardIds = new List<string>());
            var currentId = selected.FirstOrDefault(StrategyGuideShapingSpells.Contains);
            OpenPicker(
                ShapingSpellItems(),
                currentId,
                T("选择阵容专属塑造类别", "Choose the lineup's shaping category"),
                T(
                    "每套阵容只能选择一种。进入关卡先获得两张，之后每个酒馆回合获得一张。",
                    "Each lineup selects exactly one category. You receive two copies initially and one copy each later Tavern turn."),
                item =>
                {
                    selected.Clear();
                    selected.Add(item.Id);
                    SaveAndRefreshProfiles(T("阵容塑造类别已保存。", "Lineup shaping category saved."));
                });
        }

        private void BuildPlacementEditor(Transform parent, StrategyGuideEntryProfileDefinition profile)
        {
            var panel = AdvancedPanel("StrategyGuideAuthoringPlacements-" + profile.ProfileId, parent, T("起始战队、手牌与教学酒馆", "Starting board, hand, and teaching shop"));
            var placements = profile.Placements ?? (profile.Placements = new List<StrategyGuideCardDefinition>());
            foreach (var placement in placements.ToList())
            {
                if (placement == null)
                {
                    continue;
                }
                var id = placement.PlacementId;
                var rowPanel = UiFactory.Panel(
                    "StrategyGuideAuthoringPlacement-" + id,
                    panel.transform,
                    UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.92f));
                UiFactory.SetHeight(rowPanel, layout.IsShortLandscape ? ShortUnits(112f) : layout.IsCompact ? 112f : 70f);
                var row = UiFactory.Horizontal(
                    rowPanel,
                    layout.IsShortLandscape ? ShortInt(6f) : 8,
                    layout.IsShortLandscape ? ShortInt(5f) : 7);
                row.childAlignment = TextAnchor.MiddleCenter;
                row.childForceExpandWidth = false;
                var label = UiFactory.Label(
                    "StrategyGuideAuthoringPlacementLabel-" + id,
                    rowPanel.transform,
                    DisplayCard(placement) + "\n" + placement.Zone + " · " + placement.PlacementId,
                    14,
                    FontStyle.Bold,
                    layout);
                label.color = UnityTavernUiStyle.TextLight;
                UiFactory.SetFlexible(label.gameObject, 1f, 0f);
                SmallActionButton(rowPanel.transform, "StrategyGuideAuthoringPlacementReplaceButton-" + id, T("换牌", "Card"), () => OpenProfileCardPicker(profile, placement));
                SmallActionButton(rowPanel.transform, "StrategyGuideAuthoringPlacementZoneButton-" + id, ZoneLabel(placement.Zone), () =>
                {
                    placement.Zone = NextValue(placement.Zone, StrategyGuideZones.Board, StrategyGuideZones.Hand, StrategyGuideZones.Shop);
                    SaveAndRefreshProfiles(T("起始区域已保存。", "Starting zone saved."));
                });
                SmallActionButton(rowPanel.transform, "StrategyGuideAuthoringPlacementGoldenButton-" + id, placement.Golden ? T("金色", "Golden") : T("普通", "Normal"), () =>
                {
                    placement.Golden = !placement.Golden;
                    SaveAndRefreshProfiles(T("金色状态已保存。", "Golden state saved."));
                });
                SmallActionButton(rowPanel.transform, "StrategyGuideAuthoringPlacementRemoveButton-" + id, T("删除", "Remove"), () =>
                {
                    placements.Remove(placement);
                    profile.DarkGiftAttachments?.RemoveAll(item => item != null && string.Equals(item.TargetPlacementId, id, StringComparison.OrdinalIgnoreCase));
                    SaveAndRefreshProfiles(T("起始卡牌已移除。", "Starting card removed."));
                }, true);
            }
            var add = UiFactory.Button(
                "StrategyGuideAuthoringPlacementAddButton-" + profile.ProfileId,
                panel.transform,
                T("＋ 添加随从或池内法术", "+ Add minion or pool spell"),
                () => OpenPicker(
                    ProfileCardItems(),
                    null,
                    T("添加起始卡牌", "Add starting card"),
                    T("这里只显示当前版本的池内卡牌；三张塑造法术请在上方按本地回合单独配置。", "Only in-pool cards from this version appear here. Configure the three shaping spells separately by local turn above."),
                    item =>
                    {
                        var spell = FindSpell(item.Id);
                        placements.Add(new StrategyGuideCardDefinition
                        {
                            PlacementId = UniqueId(profile.ProfileId + "-card", placements.Select(value => value?.PlacementId)),
                            Zone = StrategyGuideZones.Hand,
                            CardKind = spell == null ? StrategyGuideCardKinds.Minion : StrategyGuideCardKinds.TavernSpell,
                            CardId = item.Id,
                            Provenance = spell != null && IsGuideTutorialSpell(spell)
                                ? StrategyGuideProvenance.GuideTutorial
                                : StrategyGuideProvenance.NormalPool
                        });
                        SaveAndRefreshProfiles(T("起始卡牌已添加。", "Starting card added."));
                    }),
                layout);
            UnityTavernUiStyle.ConfigureButton(add, UnityTavernUiStyle.ArcaneBlue);
        }

        private void OpenProfileCardPicker(StrategyGuideEntryProfileDefinition profile, StrategyGuideCardDefinition placement)
        {
            OpenPicker(
                ProfileCardItems(),
                placement.CardId,
                T("替换起始卡牌", "Replace starting card"),
                T("替换会保留区域与金色状态，并自动更新来源标记。", "Replacement keeps zone and golden state and refreshes provenance."),
                item =>
                {
                    var spell = FindSpell(item.Id);
                    placement.CardId = item.Id;
                    placement.CardKind = spell == null ? StrategyGuideCardKinds.Minion : StrategyGuideCardKinds.TavernSpell;
                    placement.Provenance = spell != null && IsGuideTutorialSpell(spell)
                        ? StrategyGuideProvenance.GuideTutorial
                        : StrategyGuideProvenance.NormalPool;
                    SaveAndRefreshProfiles(T("起始卡牌已替换。", "Starting card replaced."));
                });
        }

        private void BuildDarkGiftEditor(Transform parent, StrategyGuideEntryProfileDefinition profile)
        {
            var panel = AdvancedPanel("StrategyGuideAuthoringDarkGifts-" + profile.ProfileId, parent, T("黑暗之赐附着", "Dark Gift attachments"));
            var attachments = profile.DarkGiftAttachments ?? (profile.DarkGiftAttachments = new List<StrategyGuideDarkGiftAttachment>());
            foreach (var attachment in attachments.ToList())
            {
                if (attachment == null)
                {
                    continue;
                }
                var id = attachment.AttachmentId;
                SelectionRow(
                    panel.transform,
                    "StrategyGuideAuthoringDarkGiftChangeButton-" + id,
                    T("黑赐", "Gift"),
                    DisplayDarkGift(attachment.GiftResearchKey) + " → " + DisplayPlacement(profile, attachment.TargetPlacementId),
                    T("更换", "Change"),
                    () => OpenDarkGiftPicker(profile, attachment));
                SmallActionButton(panel.transform, "StrategyGuideAuthoringDarkGiftTargetButton-" + id, T("切换附着目标", "Next target"), () =>
                {
                    attachment.TargetPlacementId = NextTargetPlacement(profile, attachment.TargetPlacementId);
                    SaveAndRefreshProfiles(T("黑赐目标已保存。", "Dark Gift target saved."));
                });
                SmallActionButton(panel.transform, "StrategyGuideAuthoringDarkGiftRemoveButton-" + id, T("移除此黑赐", "Remove gift"), () =>
                {
                    attachments.Remove(attachment);
                    SaveAndRefreshProfiles(T("黑赐已移除。", "Dark Gift removed."));
                }, true);
            }
            var add = UiFactory.Button(
                "StrategyGuideAuthoringDarkGiftAddButton-" + profile.ProfileId,
                panel.transform,
                T("＋ 添加黑赐", "+ Add Dark Gift"),
                () =>
                {
                    var target = FirstMinionPlacement(profile);
                    if (target == null)
                    {
                        SetStatus(T("请先添加一个可附着黑赐的随从。", "Add a minion before attaching a Dark Gift."), false);
                        return;
                    }
                    OpenPicker(
                        DarkGiftItems(),
                        null,
                        T("选择黑暗之赐", "Choose Dark Gift"),
                        T("黑赐会附着到起始随从；可在添加后切换目标。", "The gift attaches to a starting minion; its target can be changed afterward."),
                        item =>
                        {
                            attachments.Add(new StrategyGuideDarkGiftAttachment
                            {
                                AttachmentId = UniqueId(profile.ProfileId + "-gift", attachments.Select(value => value?.AttachmentId)),
                                TargetPlacementId = target.PlacementId,
                                GiftResearchKey = item.Id,
                                AcquiredRound = profile.StartRound,
                                Source = "strategy-guide"
                            });
                            SaveAndRefreshProfiles(T("黑赐已添加。", "Dark Gift added."));
                        });
                },
                layout);
            UnityTavernUiStyle.ConfigureButton(add, UnityTavernUiStyle.ArcaneBlue);
        }

        private void OpenDarkGiftPicker(StrategyGuideEntryProfileDefinition profile, StrategyGuideDarkGiftAttachment attachment)
        {
            OpenPicker(
                DarkGiftItems(),
                attachment.GiftResearchKey,
                T("更换黑暗之赐", "Replace Dark Gift"),
                T("附着目标和获得回合保持不变。", "Target and acquired round stay unchanged."),
                item =>
                {
                    attachment.GiftResearchKey = item.Id;
                    SaveAndRefreshProfiles(T("黑赐已替换。", "Dark Gift replaced."));
                });
        }

        private void BuildOfferEditor(Transform parent, StrategyGuideEntryProfileDefinition profile)
        {
            var plan = profile.AcquisitionPlan ?? (profile.AcquisitionPlan = new StrategyGuideAcquisitionPlanDefinition());
            var schedules = plan.OfferSchedules ?? (plan.OfferSchedules = new List<StrategyGuideOfferScheduleDefinition>());
            var panel = AdvancedPanel("StrategyGuideAuthoringOffers-" + profile.ProfileId, parent, T("高级受控发牌", "Advanced controlled offers"));
            var disclosure = UiFactory.Button(
                "StrategyGuideAuthoringOfferDisclosureButton-" + profile.ProfileId,
                panel.transform,
                plan.DiscloseControlledOffers
                    ? T("✓ 已向玩家标明受控发牌", "✓ Controlled offers disclosed")
                    : T("必须向玩家标明受控发牌", "Disclose controlled offers"),
                () =>
                {
                    plan.DiscloseControlledOffers = !plan.DiscloseControlledOffers;
                    SaveAndRefreshProfiles(T("发牌披露设置已保存。", "Offer disclosure saved."));
                },
                layout);
            UnityTavernUiStyle.ConfigureButton(
                disclosure,
                plan.DiscloseControlledOffers ? UnityTavernUiStyle.SuccessGreen : UnityTavernUiStyle.DangerRed,
                plan.DiscloseControlledOffers,
                plan.DiscloseControlledOffers);

            foreach (var schedule in schedules.ToList())
            {
                if (schedule == null)
                {
                    continue;
                }
                var id = schedule.ScheduleId;
                var schedulePanel = AdvancedPanel(
                    "StrategyGuideAuthoringOffer-" + id,
                    panel.transform,
                    OfferSourceLabel(schedule.Source) + " · " + OfferPolicyLabel(schedule.Policy));
                SmallActionButton(schedulePanel.transform, "StrategyGuideAuthoringOfferSourceButton-" + id, T("来源：", "Source: ") + OfferSourceLabel(schedule.Source), () =>
                {
                    schedule.Source = NextValue(schedule.Source,
                        StrategyGuideOfferSources.TripleRewardDiscover,
                        StrategyGuideOfferSources.ShopRefresh,
                        StrategyGuideOfferSources.TavernSpellDiscover,
                        StrategyGuideOfferSources.GreaterTrinketChoice);
                    schedule.CardKind = schedule.Source == StrategyGuideOfferSources.GreaterTrinketChoice
                        ? StrategyGuideCardKinds.Trinket
                        : schedule.Source == StrategyGuideOfferSources.TavernSpellDiscover
                            ? StrategyGuideCardKinds.TavernSpell
                            : StrategyGuideCardKinds.Minion;
                    schedule.TargetCardIds = schedule.TargetCardIds ?? new List<string>();
                    schedule.TargetCardIds.Clear();
                    SaveAndRefreshProfiles(T("发牌来源已保存。", "Offer source saved."));
                });
                SmallActionButton(schedulePanel.transform, "StrategyGuideAuthoringOfferPolicyButton-" + id, T("策略：", "Policy: ") + OfferPolicyLabel(schedule.Policy), () =>
                {
                    schedule.Policy = NextValue(schedule.Policy,
                    StrategyGuideOfferPolicies.NaturalSeeded,
                    StrategyGuideOfferPolicies.MustInclude,
                    StrategyGuideOfferPolicies.MustIncludeAny,
                    StrategyGuideOfferPolicies.Pinned);
                    SaveAndRefreshProfiles(T("发现策略已保存。", "Offer policy saved."));
                });
                BuildScheduleNumberField(schedulePanel.transform, profile, schedule, "Occurrence", T("第几次触发", "Trigger occurrence"), schedule.TriggerOccurrence, 1, 20, value => schedule.TriggerOccurrence = value);
                BuildScheduleNumberField(schedulePanel.transform, profile, schedule, "Tier", T("目标等级", "Target tier"), schedule.TavernTier, 0, 6, value => schedule.TavernTier = value);
                BuildScheduleNumberField(schedulePanel.transform, profile, schedule, "Options", T("候选数量", "Option count"), schedule.OptionCount, 1, 8, value => schedule.OptionCount = value);
                SelectionRow(
                    schedulePanel.transform,
                    "StrategyGuideAuthoringOfferTargetButton-" + id,
                    T("必含牌", "Target"),
                    schedule.TargetCardIds == null || schedule.TargetCardIds.Count == 0
                        ? T("未指定", "Not set")
                        : string.Join(", ", schedule.TargetCardIds.Select(DisplayAnyCard)),
                    T("选择", "Choose"),
                    () => OpenOfferTargetPicker(profile, schedule));
                SmallActionButton(schedulePanel.transform, "StrategyGuideAuthoringOfferRemoveButton-" + id, T("删除此发牌计划", "Remove schedule"), () =>
                {
                    schedules.Remove(schedule);
                    SaveAndRefreshProfiles(T("发牌计划已移除。", "Offer schedule removed."));
                }, true);
            }

            var add = UiFactory.Button(
                "StrategyGuideAuthoringOfferAddButton-" + profile.ProfileId,
                panel.transform,
                T("＋ 添加受控发牌", "+ Add controlled offer"),
                () =>
                {
                    plan.DiscloseControlledOffers = true;
                    schedules.Add(new StrategyGuideOfferScheduleDefinition
                    {
                        ScheduleId = UniqueId(profile.ProfileId + "-offer", schedules.Select(value => value?.ScheduleId)),
                        Source = StrategyGuideOfferSources.ShopRefresh,
                        TriggerOccurrence = 1,
                        Policy = StrategyGuideOfferPolicies.MustInclude,
                        CardKind = StrategyGuideCardKinds.Minion,
                        TavernTier = Mathf.Clamp(profile.TavernTier, 1, 6),
                        OptionCount = 3,
                        TargetCardIds = new List<string>(),
                        Label = "受控发牌（实际游戏以正常概率为准）",
                        EnglishLabel = "Controlled offer (normal games use normal odds)"
                    });
                    SaveAndRefreshProfiles(T("已添加受控发牌，请继续选择目标牌。", "Controlled offer added; choose its target card."));
                },
                layout);
            UnityTavernUiStyle.ConfigureButton(add, UnityTavernUiStyle.ArcaneBlue);
        }

        private void OpenOfferTargetPicker(StrategyGuideEntryProfileDefinition profile, StrategyGuideOfferScheduleDefinition schedule)
        {
            IEnumerable<StrategyGuideAuthoringPickerItem> items;
            if (schedule.CardKind == StrategyGuideCardKinds.Trinket)
            {
                items = catalogs.Trinkets.GetOfferableBySlot(TrinketSlotKind.Greater).Select(TrinketItem);
            }
            else if (schedule.CardKind == StrategyGuideCardKinds.TavernSpell)
            {
                items = SpellItems(false);
            }
            else
            {
                items = MinionItems();
            }
            OpenPicker(
                items,
                schedule.TargetCardIds?.FirstOrDefault(),
                T("选择受控发牌目标", "Choose controlled-offer target"),
                T("第一版每个计划选择一张必含牌；可继续添加多个计划表达多次刷新。", "The first version chooses one required card per schedule; add schedules for later refreshes."),
                item =>
                {
                    schedule.TargetCardIds = new List<string> { item.Id };
                    SaveAndRefreshProfiles(T("受控目标牌已保存。", "Controlled target saved."));
                });
        }

        private void BuildOpponentEditor(Transform parent, StrategyGuideEntryProfileDefinition profile)
        {
            var opponent = profile.Opponent ?? (profile.Opponent = new StrategyGuideOpponentSelector());
            var panel = AdvancedPanel("StrategyGuideAuthoringOpponent-" + profile.ProfileId, parent, T("验收对手", "Validation opponent"));
            var selectedOpponent = EligibleOpponents().FirstOrDefault(item =>
                item.StrengthRound == opponent.StrengthRound &&
                (string.IsNullOrWhiteSpace(opponent.RequiredTag) || (item.Tags ?? new List<string>()).Contains(opponent.RequiredTag)));
            SelectionRow(
                panel.transform,
                "StrategyGuideAuthoringOpponentPickerButton-" + profile.ProfileId,
                T("对手池", "Opponent pool"),
                selectedOpponent == null
                    ? T("未匹配当前选择", "No matching opponent")
                    : OpponentDisplay(selectedOpponent),
                T("选择", "Choose"),
                () => OpenPicker(
                    OpponentItems(),
                    selectedOpponent?.OpponentId,
                    T("选择验收对手池", "Choose validation opponent pool"),
                    T("只选择版本、强度回合和标签；每次试玩仍从匹配池随机对手。", "Choose version, strength round, and tag; each run still randomizes within the matching pool."),
                    item =>
                    {
                        var definition = EligibleOpponents().First(value => string.Equals(value.OpponentId, item.Id, StringComparison.OrdinalIgnoreCase));
                        opponent.StrengthRound = definition.StrengthRound;
                        opponent.RequiredTag = (definition.Tags ?? new List<string>()).FirstOrDefault() ?? string.Empty;
                        SaveAndRefreshProfiles(T("对手池已保存。", "Opponent pool saved."));
                    }));
        }

        private GameObject AdvancedPanel(string name, Transform parent, string titleText)
        {
            var panel = UiFactory.Panel(name, parent, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.88f));
            UiFactory.Vertical(
                panel,
                layout.IsShortLandscape ? ShortInt(6f) : layout.IsCompact ? 8 : 10,
                layout.IsShortLandscape ? ShortInt(5f) : 7);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.30f),
                new Vector2(1f, -1f));
            var title = UiFactory.Label(name + "Title", panel.transform, titleText, 16, FontStyle.Bold, layout);
            title.color = UnityTavernUiStyle.Gold;
            UiFactory.SetHeight(title.gameObject, layout.IsShortLandscape ? ShortUnits(30f) : 30f);
            return panel;
        }

        private void SmallActionButton(Transform parent, string name, string text, Action action, bool danger = false)
        {
            var button = UiFactory.Button(name, parent, text, () => action?.Invoke(), layout);
            UnityTavernUiStyle.ConfigureButton(button, danger ? UnityTavernUiStyle.DangerRed : UnityTavernUiStyle.ArcaneBlue);
            UiFactory.SetWidth(button.gameObject, layout.IsShortLandscape ? ShortUnits(80f) : layout.IsCompact ? 94f : 118f);
        }

        private void BuildScheduleNumberField(
            Transform parent,
            StrategyGuideEntryProfileDefinition profile,
            StrategyGuideOfferScheduleDefinition schedule,
            string fieldName,
            string labelText,
            int value,
            int minimum,
            int maximum,
            Action<int> commit)
        {
            var row = UiFactory.Panel("StrategyGuideAuthoringOffer" + fieldName + "Row-" + schedule.ScheduleId, parent, Color.clear);
            UiFactory.SetHeight(row, layout.IsShortLandscape ? ShortUnits(52f) : 52f);
            var horizontal = UiFactory.Horizontal(row, 0, layout.IsShortLandscape ? ShortInt(6f) : 8);
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childForceExpandWidth = false;
            var label = UiFactory.Label("StrategyGuideAuthoringOffer" + fieldName + "Label-" + schedule.ScheduleId, row.transform, labelText, 14, FontStyle.Bold, layout);
            label.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetFlexible(label.gameObject, 1f, 0f);
            var inputObject = new GameObject("StrategyGuideAuthoringOffer" + fieldName + "Input-" + schedule.ScheduleId, typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(row.transform, false);
            UiFactory.SetMinSize(inputObject, 92f, UnityTavernUiStyle.TouchHeight);
            UiFactory.SetWidth(inputObject, layout.IsShortLandscape ? ShortUnits(100f) : 100f);
            var input = inputObject.GetComponent<InputField>();
            input.contentType = InputField.ContentType.IntegerNumber;
            var text = UiFactory.Label(inputObject.name + "Text", inputObject.transform, string.Empty, 16, FontStyle.Bold, layout);
            text.alignment = TextAnchor.MiddleCenter;
            UiFactory.Stretch(text.rectTransform);
            input.textComponent = text;
            input.text = value.ToString();
            input.ForceLabelUpdate();
            inputObject.AddComponent<UnitySelectableFocusRing>();
            UnityTavernUiStyle.ConfigureInputField(input, StrategyGuideUiTheme.Focus);
            input.onEndEdit.AddListener(raw =>
            {
                if (!int.TryParse(raw, out var parsed) || parsed < minimum || parsed > maximum)
                {
                    input.text = value.ToString();
                    SetStatus(T("数值超出允许范围。", "Value is outside the allowed range."), false);
                    return;
                }
                commit(parsed);
                SaveDraft(labelText + T("已保存。", " saved."));
            });
        }

        private void OpenPicker(
            IEnumerable<StrategyGuideAuthoringPickerItem> items,
            string currentId,
            string titleText,
            string helpText,
            Action<StrategyGuideAuthoringPickerItem> select,
            bool cardLibraryMode = false,
            IEnumerable<string> selectedIds = null)
        {
            ClosePicker();
            pickerOverlay = StrategyGuideAuthoringPickerModalComponent.CreateModalHost(shell.transform);
            pickerOverlay.GetComponent<StrategyGuideAuthoringPickerModalComponent>().Build(
                items,
                currentId,
                titleText,
                helpText,
                item =>
                {
                    select?.Invoke(item);
                    ClosePicker();
                },
                ClosePicker,
                useEnglish,
                layout,
                cardLibraryMode,
                selectedIds);
        }

        private void ClosePicker()
        {
            if (pickerOverlay == null)
            {
                return;
            }
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(pickerOverlay);
            }
            else
            {
                UnityEngine.Object.Destroy(pickerOverlay);
            }
#else
            UnityEngine.Object.Destroy(pickerOverlay);
#endif
            pickerOverlay = null;
        }

        private IEnumerable<StrategyGuideAuthoringPickerItem> MinionItems()
        {
            return catalogs.Minions.All
                .Where(item => item.InPool && !IsDuosCard(item.CardId))
                .Select(item => new StrategyGuideAuthoringPickerItem
                {
                    Id = item.CardId,
                    Name = item.Name,
                    Detail = T("等级 ", "Tier ") + item.TavernTier + " · " +
                        MinionTribesText(item.Tribes) + " · " + item.Text,
                    Group = T("等级 ", "Tier ") + item.TavernTier,
                    SearchTerms = string.Join(" ", (item.Tribes ?? new List<Tribe>())
                        .Select(tribe => TribeDisplayName(tribe.ToString()))),
                    ImagePath = item.ImagePath,
                    CardKind = CardKind.Minion,
                    TavernTier = item.TavernTier
                });
        }

        private IEnumerable<StrategyGuideAuthoringPickerItem> SpellItems(bool includeGuideTutorial)
        {
            return catalogs.Spells.All
                .Where(item => !IsDuosCard(item.CardNumber) &&
                    (item.InPool && string.Equals(item.Category, "TavernSpell", StringComparison.OrdinalIgnoreCase) ||
                     includeGuideTutorial && IsGuideTutorialSpell(item)))
                .Select(item => new StrategyGuideAuthoringPickerItem
                {
                    Id = item.CardNumber,
                    Name = useEnglish && !string.IsNullOrWhiteSpace(item.EnglishName) ? item.EnglishName : item.Name,
                    Detail = (IsGuideTutorialSpell(item) ? T("一图流教学专用 · ", "Guide tutorial only · ") : T("等级 ", "Tier ") + item.TavernTier + " · ") +
                        (useEnglish && !string.IsNullOrWhiteSpace(item.EnglishText) ? item.EnglishText : item.Text),
                    Group = IsGuideTutorialSpell(item) ? T("一图流教学", "Guide tutorial") : T("等级 ", "Tier ") + item.TavernTier,
                    SearchTerms = string.Join(" ", (item.Keywords ?? new List<string>()).Concat(item.Tags ?? new List<string>())),
                    ImagePath = item.ImagePath,
                    CardKind = CardKind.TavernSpell,
                    TavernTier = item.TavernTier
                });
        }

        private string MinionTribesText(IEnumerable<Tribe> tribes)
        {
            var names = (tribes ?? Enumerable.Empty<Tribe>())
                .Where(tribe => tribe != Tribe.None)
                .Select(tribe => TribeDisplayName(tribe.ToString()))
                .Distinct()
                .ToArray();
            return names.Length == 0 ? T("中立", "Neutral") : string.Join("/", names);
        }

        private static bool IsDuosCard(string cardId)
        {
            return !string.IsNullOrWhiteSpace(cardId) &&
                cardId.StartsWith("BGDUO", StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<StrategyGuideAuthoringPickerItem> ProfileCardItems()
        {
            return MinionItems().Concat(SpellItems(false));
        }

        private IEnumerable<StrategyGuideAuthoringPickerItem> ShapingSpellItems()
        {
            return SpellItems(true).Where(item =>
                string.Equals(item.Id, "GUIDE_SHAPING_DEATHRATTLE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Id, "GUIDE_SHAPING_BATTLECRY", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Id, "GUIDE_SHAPING_END_OF_TURN", StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<StrategyGuideAuthoringPickerItem> DarkGiftItems()
        {
            return catalogs.DarkGifts.All
                .Where(item => item.ImplementationStatus == DarkGiftImplementationStatus.Implemented ||
                    item.ImplementationStatus == DarkGiftImplementationStatus.Verified)
                .Select(item => new StrategyGuideAuthoringPickerItem
                {
                    Id = item.ResearchKey,
                    Name = item.DisplayName,
                    Detail = item.Text,
                    Group = "Level " + item.SourceLevel,
                    ImagePath = item.ImagePath,
                    CardKind = CardKind.Spell
                });
        }

        private IEnumerable<StrategyGuideAuthoringPickerItem> OpponentItems()
        {
            return EligibleOpponents().Select(item => new StrategyGuideAuthoringPickerItem
            {
                Id = item.OpponentId,
                Name = OpponentDisplay(item),
                Detail = T("战队 ", "Board ") + (item.Board?.Count ?? 0) + " · " + string.Join(", ", item.Tags ?? new List<string>()),
                Group = "Round " + item.StrengthRound,
                CardKind = CardKind.Hero
            });
        }

        private IEnumerable<StrategyGuideOpponentDefinition> EligibleOpponents()
        {
            return (catalog.Opponents ?? new List<StrategyGuideOpponentDefinition>())
                .Where(item => item != null && string.Equals(item.GameVersionId, draft.Guide.GameVersionId, StringComparison.OrdinalIgnoreCase));
        }

        private StrategyGuideAuthoringPickerItem TrinketItem(TrinketDefinition item)
        {
            return new StrategyGuideAuthoringPickerItem
            {
                Id = item.CardId,
                Name = item.Name,
                Detail = T("费用 ", "Cost ") + item.Cost + " · " + item.Text,
                Group = item.SlotKind.ToString(),
                ImagePath = item.ImagePath,
                CardKind = CardKind.Trinket
            };
        }

        private void SaveAndRefreshProfiles(string message)
        {
            SaveDraft(message);
            ShowStep(ProfilesStep);
        }

        private void ReplaceShowcaseCardReference(string oldCardId, string newCardId)
        {
            foreach (var profile in draft.Guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>())
            {
                if (profile?.Difficulty != StrategyGuideDifficulties.Showcase)
                {
                    continue;
                }
                foreach (var placement in profile.Placements ?? new List<StrategyGuideCardDefinition>())
                {
                    if (placement != null && string.Equals(placement.CardId, oldCardId, StringComparison.OrdinalIgnoreCase))
                    {
                        placement.CardId = newCardId;
                    }
                }
            }
        }

        private TavernSpellDefinition FindSpell(string cardNumber)
        {
            return catalogs.Spells.All.FirstOrDefault(item => string.Equals(item.CardNumber, cardNumber, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsGuideTutorialSpell(TavernSpellDefinition spell)
        {
            return spell != null && string.Equals(spell.Category, "GuideTutorial", StringComparison.OrdinalIgnoreCase);
        }

        private StrategyGuideCardDefinition FirstMinionPlacement(StrategyGuideEntryProfileDefinition profile)
        {
            return (profile.Placements ?? new List<StrategyGuideCardDefinition>()).FirstOrDefault(item =>
                item != null && item.CardKind == StrategyGuideCardKinds.Minion);
        }

        private string NextTargetPlacement(StrategyGuideEntryProfileDefinition profile, string current)
        {
            var targets = (profile.Placements ?? new List<StrategyGuideCardDefinition>())
                .Where(item => item != null && item.CardKind == StrategyGuideCardKinds.Minion)
                .Select(item => item.PlacementId)
                .ToArray();
            return targets.Length == 0 ? current : NextValue(current, targets);
        }

        private string DisplayPlacement(StrategyGuideEntryProfileDefinition profile, string placementId)
        {
            var placement = (profile.Placements ?? new List<StrategyGuideCardDefinition>()).FirstOrDefault(item =>
                item != null && string.Equals(item.PlacementId, placementId, StringComparison.OrdinalIgnoreCase));
            return placement == null ? placementId : DisplayCard(placement);
        }

        private string DisplayDarkGift(string researchKey)
        {
            var gift = catalogs.DarkGifts.All.FirstOrDefault(item => string.Equals(item.ResearchKey, researchKey, StringComparison.OrdinalIgnoreCase));
            return gift?.DisplayName ?? researchKey;
        }

        private string DisplayAnyCard(string id)
        {
            var spell = FindSpell(id);
            if (spell != null)
            {
                return DisplaySpell(id);
            }
            var trinket = catalogs.Trinkets.All.FirstOrDefault(item => string.Equals(item.CardId, id, StringComparison.OrdinalIgnoreCase));
            return trinket?.Name ?? DisplayMinion(id);
        }

        private string OpponentDisplay(StrategyGuideOpponentDefinition opponent)
        {
            return T("第 ", "Round ") + opponent.StrengthRound + T(" 回合", string.Empty) + " · " +
                string.Join("/", opponent.Tags ?? new List<string>());
        }

        private string TribeDisplayName(string tribe)
        {
            if (useEnglish)
            {
                return tribe;
            }
            switch (tribe)
            {
                case "Beast": return "野兽";
                case "Murloc": return "鱼人";
                case "Mech": return "机械";
                case "Demon": return "恶魔";
                case "Dragon": return "龙";
                case "Pirate": return "海盗";
                case "Elemental": return "元素";
                case "Quilboar": return "野猪人";
                case "Undead": return "亡灵";
                case "Naga": return "纳迦";
                default: return tribe;
            }
        }

        private string ZoneLabel(string zone)
        {
            if (zone == StrategyGuideZones.Board) return T("战队", "Board");
            if (zone == StrategyGuideZones.Shop) return T("酒馆", "Shop");
            return T("手牌", "Hand");
        }

        private string OfferSourceLabel(string source)
        {
            if (source == StrategyGuideOfferSources.TripleRewardDiscover) return T("三连奖励", "Triple reward");
            if (source == StrategyGuideOfferSources.TavernSpellDiscover) return T("法术发现", "Spell discover");
            if (source == StrategyGuideOfferSources.GreaterTrinketChoice) return T("大型饰品池", "Greater Trinket pool");
            return T("酒馆刷新", "Shop refresh");
        }

        private string OfferPolicyLabel(string policy)
        {
            if (policy == StrategyGuideOfferPolicies.MustInclude) return T("必定包含", "Must include");
            if (policy == StrategyGuideOfferPolicies.MustIncludeAny) return T("推荐中至少一个", "Include one recommendation");
            if (policy == StrategyGuideOfferPolicies.Pinned) return T("完全固定", "Pinned");
            return T("固定种子自然结果", "Natural seeded");
        }

        private static string NextValue(string current, params string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return current;
            }
            var index = Array.FindIndex(values, item => string.Equals(item, current, StringComparison.OrdinalIgnoreCase));
            return values[(index + 1 + values.Length) % values.Length];
        }

        private static List<string> SplitNonEmptyLines(string value)
        {
            return (value ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Take(6)
                .ToList();
        }

        private static string UniqueId(string prefix, IEnumerable<string> existing)
        {
            var used = new HashSet<string>(existing ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            for (var index = 1; index < 1000; index += 1)
            {
                var candidate = prefix + "-" + index;
                if (!used.Contains(candidate))
                {
                    return candidate;
                }
            }
            return prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private void BuildTextField(
            Transform parent,
            string objectName,
            string labelText,
            string value,
            bool multiline,
            Action<string> commit)
        {
            var group = UiFactory.Panel(objectName + "Group", parent, Color.clear);
            UiFactory.Vertical(group, 0, layout.IsShortLandscape ? ShortInt(4f) : 4);
            var label = UiFactory.Label(objectName + "Label", group.transform, labelText, 14, FontStyle.Bold, layout);
            label.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(label.gameObject, layout.IsShortLandscape ? ShortUnits(28f) : 28f);

            var inputObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(group.transform, false);
            var touchHeight = layout.CanvasUnitsForPhysicalPixels(UiFactory.MinimumButtonHeight);
            UiFactory.SetMinSize(inputObject, touchHeight, touchHeight);
            UiFactory.SetHeight(
                inputObject,
                multiline
                    ? Mathf.Max(layout.IsShortLandscape ? ShortUnits(104f) : 104f, touchHeight * 2f)
                    : touchHeight);
            var input = inputObject.GetComponent<InputField>();
            input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            input.characterLimit = multiline ? 320 : 80;
            input.caretColor = UnityTavernUiStyle.TextLight;
            input.selectionColor = UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.36f);

            var text = UiFactory.Label(objectName + "Text", inputObject.transform, string.Empty, 14, FontStyle.Normal, layout);
            text.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            UiFactory.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(12f, 8f);
            text.rectTransform.offsetMax = new Vector2(-12f, -8f);
            input.textComponent = text;
            input.text = value ?? string.Empty;
            input.ForceLabelUpdate();
            inputObject.AddComponent<StrategyGuideAuthoringInputVisibilityHandler>().Selected =
                () => KeepFocusedInputVisible(inputObject.GetComponent<RectTransform>());
            input.onEndEdit.AddListener(committed => commit?.Invoke(committed.Trim()));
            inputObject.AddComponent<UnitySelectableFocusRing>();
            UnityTavernUiStyle.ConfigureInputField(input, StrategyGuideUiTheme.Focus);
        }

        private void BuildNumberField(
            Transform parent,
            StrategyGuideEntryProfileDefinition profile,
            string fieldName,
            string labelText,
            int value,
            int minimum,
            int maximum,
            Action<int> commit)
        {
            var rowPanel = UiFactory.Panel(
                "StrategyGuideAuthoring" + fieldName + "Row-" + profile.ProfileId,
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.74f));
            var touchHeight = layout.CanvasUnitsForPhysicalPixels(UiFactory.MinimumButtonHeight);
            UiFactory.SetHeight(rowPanel, Mathf.Max(56f, touchHeight));
            var row = UiFactory.Horizontal(
                rowPanel,
                layout.IsShortLandscape ? ShortInt(6f) : 8,
                layout.IsShortLandscape ? ShortInt(6f) : 8);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;
            var label = UiFactory.Label(
                "StrategyGuideAuthoring" + fieldName + "Label-" + profile.ProfileId,
                rowPanel.transform,
                labelText,
                14,
                FontStyle.Bold,
                layout);
            label.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetFlexible(label.gameObject, 1f, 0f);

            var objectName = "StrategyGuideAuthoring" + fieldName + "Input-" + profile.ProfileId;
            var inputObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(rowPanel.transform, false);
            var inputWidth = layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(92f) : 112f;
            UiFactory.SetMinSize(inputObject, inputWidth, touchHeight);
            UiFactory.SetWidth(inputObject, inputWidth);
            var input = inputObject.GetComponent<InputField>();
            input.contentType = InputField.ContentType.IntegerNumber;
            input.lineType = InputField.LineType.SingleLine;
            var text = UiFactory.Label(objectName + "Text", inputObject.transform, string.Empty, 16, FontStyle.Bold, layout);
            text.alignment = TextAnchor.MiddleCenter;
            UiFactory.Stretch(text.rectTransform);
            input.textComponent = text;
            input.text = value.ToString();
            input.ForceLabelUpdate();
            inputObject.AddComponent<UnitySelectableFocusRing>();
            UnityTavernUiStyle.ConfigureInputField(input, StrategyGuideUiTheme.Focus);
            inputObject.AddComponent<StrategyGuideAuthoringInputVisibilityHandler>().Selected =
                () => KeepFocusedInputVisible(inputObject.GetComponent<RectTransform>());
            input.onEndEdit.AddListener(raw =>
            {
                if (!int.TryParse(raw, out var parsed) || parsed < minimum || parsed > maximum)
                {
                    input.text = value.ToString();
                    SetStatus(T("请输入 ", "Enter a value from ") + minimum + T(" 到 ", " to ") + maximum + "。", false);
                    return;
                }
                commit(parsed);
                SaveDraft(labelText + T("已自动保存。", " autosaved."));
            });
        }

        private void KeepFocusedInputVisible(RectTransform target)
        {
            EnsureFocusedInputVisible(target);
            if (UnityEngine.Application.isPlaying && operationRunner != null)
            {
                operationRunner.Run(KeepFocusedInputVisibleOverFrames(target));
            }
        }

        private IEnumerator KeepFocusedInputVisibleOverFrames(RectTransform target)
        {
            yield return null;
            EnsureFocusedInputVisible(target);
            yield return null;
            EnsureFocusedInputVisible(target);
        }

        private void EnsureFocusedInputVisible(RectTransform target)
        {
            if (target == null || stepScroll == null || stepScroll.content == null || stepScroll.viewport == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(stepScroll.content);
            var viewportHeight = stepScroll.viewport.rect.height;
            var hiddenHeight = Mathf.Max(0f, stepScroll.content.rect.height - viewportHeight);
            if (viewportHeight <= 0f || hiddenHeight <= 0f)
            {
                return;
            }

            var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(stepScroll.content, target);
            var targetTopFromContent = stepScroll.content.rect.yMax - targetBounds.max.y;
            var safeTop = viewportHeight * (layout.IsCompact ? 0.18f : 0.12f);
            var desiredY = Mathf.Clamp(targetTopFromContent - safeTop, 0f, hiddenHeight);
            stepScroll.StopMovement();
            stepScroll.content.anchoredPosition = new Vector2(stepScroll.content.anchoredPosition.x, desiredY);
        }

        private GameObject StepPanel(string name, Transform parent)
        {
            var panel = UiFactory.Panel(
                name,
                parent,
                StrategyGuideUiTheme.Surface);
            StrategyGuideUiTheme.ApplySurface(panel, StrategyGuideUiTheme.Surface, "panel_workspace");
            UiFactory.Vertical(
                panel,
                layout.IsShortLandscape ? ShortInt(6f) : layout.IsCompact ? 10 : 14,
                layout.IsShortLandscape ? ShortInt(6f) : 10);
            StrategyGuideUiTheme.Outline(panel, StrategyGuideUiTheme.BorderStrong);
            return panel;
        }

        private void SectionTitle(Transform parent, string titleText, string helpText)
        {
            var title = UiFactory.Label("StrategyGuideAuthoringSectionTitle", parent, titleText, layout.IsCompact ? 20 : 24, FontStyle.Bold, layout);
            title.color = StrategyGuideUiTheme.WarmText;
            UiFactory.SetHeight(title.gameObject, layout.IsShortLandscape ? ShortUnits(38f) : 38f);
            var help = UiFactory.Label("StrategyGuideAuthoringSectionHelp", parent, helpText, 14, FontStyle.Normal, layout);
            help.color = StrategyGuideUiTheme.MutedText;
            UiFactory.SetHeight(help.gameObject, layout.IsShortLandscape ? ShortUnits(54f) : layout.IsCompact ? 54f : 46f);
        }

        private void ReadOnlyRow(Transform parent, string labelText, string value)
        {
            var rowPanel = UiFactory.Panel("StrategyGuideAuthoringReadOnly-" + labelText, parent,
                StrategyGuideUiTheme.SurfaceSoft);
            UiFactory.SetHeight(rowPanel, layout.IsShortLandscape ? ShortUnits(52f) : 52f);
            var row = UiFactory.Horizontal(
                rowPanel,
                layout.IsShortLandscape ? ShortInt(6f) : 8,
                layout.IsShortLandscape ? ShortInt(6f) : 8);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;
            var label = UiFactory.Label("StrategyGuideAuthoringReadOnlyLabel", rowPanel.transform, labelText, 14, FontStyle.Bold, layout);
            label.color = StrategyGuideUiTheme.MutedText;
            UiFactory.SetWidth(label.gameObject, layout.IsShortLandscape ? ShortUnits(112f) : layout.IsCompact ? 112f : 150f);
            var text = UiFactory.Label("StrategyGuideAuthoringReadOnlyValue", rowPanel.transform, value, 14, FontStyle.Bold, layout);
            text.color = StrategyGuideUiTheme.Text;
            UiFactory.SetFlexible(text.gameObject, 1f, 0f);
        }

        private void SummaryCard(Transform parent, string name, string titleText, string bodyText)
        {
            var panel = UiFactory.Panel(name, parent, StrategyGuideUiTheme.SurfaceSoft);
            UiFactory.Vertical(
                panel,
                layout.IsShortLandscape ? ShortInt(8f) : 10,
                layout.IsShortLandscape ? ShortInt(4f) : 4);
            StrategyGuideUiTheme.Outline(panel, StrategyGuideUiTheme.Border);
            var title = UiFactory.Label(name + "Title", panel.transform, titleText, 16, FontStyle.Bold, layout);
            title.color = StrategyGuideUiTheme.WarmText;
            UiFactory.SetHeight(title.gameObject, layout.IsShortLandscape ? ShortUnits(30f) : 30f);
            var body = UiFactory.Label(name + "Body", panel.transform,
                string.IsNullOrWhiteSpace(bodyText) ? T("无", "None") : bodyText,
                14,
                FontStyle.Normal,
                layout);
            body.color = StrategyGuideUiTheme.Text;
            UiFactory.SetHeight(body.gameObject, layout.IsShortLandscape ? ShortUnits(48f) : 48f);
        }

        private void SaveDraft(string successMessage)
        {
            try
            {
                repository.SaveDraft(draft);
                SetStatus(successMessage, true);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is System.IO.IOException)
            {
                SetStatus(T("草稿保存失败：", "Draft save failed: ") + exception.Message, false);
            }
        }

        private void Freeze()
        {
            if (freezeInProgress)
            {
                return;
            }

            freezeInProgress = true;
            if (freezeButton != null)
            {
                freezeButton.interactable = false;
            }

            var routine = FreezeOverFrames();
            if (UnityEngine.Application.isPlaying)
            {
                operationRunner.Run(routine);
                return;
            }

            while (routine.MoveNext())
            {
            }
        }

        private IEnumerator FreezeOverFrames()
        {
            SetProgressStatus(T("1/3 正在保存草稿…", "1/3 Saving draft..."));
            yield return null;
            try
            {
                repository.SaveDraft(draft);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is System.IO.IOException)
            {
                FinishFreeze(T("草稿保存失败：", "Draft save failed: ") + exception.Message, false);
                yield break;
            }

            SetProgressStatus(T("2/3 正在校验并生成指纹…", "2/3 Validating and creating fingerprint..."));
            yield return null;
            LastFreezeResult = StrategyGuideAuthoringFreezeService.Freeze(draft, catalog, version);
            if (!LastFreezeResult.Succeeded)
            {
                var recovery = LastFreezeResult.Diagnostics
                    .Take(3)
                    .Select(DiagnosticText);
                FinishFreeze(T("无法冻结：", "Cannot freeze: ") + string.Join("；", recovery), false);
                ShowStep(FreezeStep);
                yield break;
            }

            SetProgressStatus(T("3/3 正在保存冻结版本…", "3/3 Saving frozen version..."));
            yield return null;
            try
            {
                repository.SaveFrozen(LastFreezeResult);
                FinishFreeze(
                    T("已冻结：", "Frozen: ") + LastFreezeResult.Guide.RevisionId +
                    "  ·  SHA-256 " + LastFreezeResult.ContentHash.Substring(0, 12) + "…",
                    true);
                ShowStep(FreezeStep);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is System.IO.IOException)
            {
                FinishFreeze(T("冻结产物保存失败：", "Frozen artifact save failed: ") + exception.Message, false);
            }
        }

        private void SetProgressStatus(string message)
        {
            if (status == null)
            {
                return;
            }

            status.text = message;
            status.color = StrategyGuideUiTheme.FocusSoft;
        }

        private void FinishFreeze(string message, bool succeeded)
        {
            freezeInProgress = false;
            if (freezeButton != null)
            {
                freezeButton.interactable = true;
            }
            SetStatus(message, succeeded);
        }

        private void ToggleFinalGolden(string cardId, bool golden)
        {
            foreach (var target in draft.Guide.FinalComposition ?? new List<StrategyGuideCardDefinition>())
            {
                if (target != null && string.Equals(target.CardId, cardId, StringComparison.OrdinalIgnoreCase))
                {
                    target.Golden = golden;
                }
            }

            foreach (var profile in draft.Guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>())
            {
                if (profile == null || profile.Difficulty != StrategyGuideDifficulties.Showcase)
                {
                    continue;
                }
                foreach (var placement in profile.Placements ?? new List<StrategyGuideCardDefinition>())
                {
                    if (placement != null && string.Equals(placement.CardId, cardId, StringComparison.OrdinalIgnoreCase))
                    {
                        placement.Golden = golden;
                    }
                }
            }
        }

        private string DiagnosticText(string diagnostic)
        {
            if (diagnostic.StartsWith("guide.active-tribe.count", StringComparison.Ordinal))
            {
                return T("需要恰好选择 5 个种族后再冻结", "Choose exactly five tribes before freezing");
            }
            if (diagnostic.Contains("card") || diagnostic.Contains("minion") || diagnostic.Contains("spell"))
            {
                return T("有卡牌引用不可用于当前版本，请回到阵容检查", "A card is unavailable in this version; review the lineup");
            }
            if (diagnostic.Contains("disclosure"))
            {
                return T("受控发牌必须向玩家明确标识", "Controlled offers must be disclosed to the player");
            }
            if (diagnostic.Contains("range"))
            {
                return T("起始回合、等级或金币超出允许范围", "A starting round, tier or gold value is out of range");
            }
            return T("请检查基本信息、阵容和难度入口（", "Review basics, lineup and entries (") + diagnostic + "）";
        }

        private void SetStatus(string message, bool success)
        {
            if (status == null)
            {
                return;
            }
            status.text = message;
            status.color = success ? UnityTavernUiStyle.SuccessGreen : UnityTavernUiStyle.DangerRed;
        }

        private string DisplayCard(StrategyGuideCardDefinition card)
        {
            if (card.CardKind == StrategyGuideCardKinds.TavernSpell)
            {
                return DisplaySpell(card.CardId);
            }
            return DisplayMinion(card.CardId);
        }

        private string DisplayMinion(string cardId)
        {
            return catalogs.Minions.TryGetByCardId(cardId, out var minion) ? minion.Name : cardId;
        }

        private string DisplaySpell(string cardNumber)
        {
            var spell = catalogs.Spells.All.FirstOrDefault(item =>
                string.Equals(item.CardNumber, cardNumber, StringComparison.OrdinalIgnoreCase));
            return spell == null ? cardNumber : useEnglish && !string.IsNullOrWhiteSpace(spell.EnglishName) ? spell.EnglishName : spell.Name;
        }

        private string DisplayHero(string cardId)
        {
            var hero = catalogs.Heroes.AllHeroes.FirstOrDefault(item =>
                string.Equals(item.HeroCardId, cardId, StringComparison.OrdinalIgnoreCase));
            return hero?.Name ?? cardId;
        }

        private string DisplayTrinket(string cardId)
        {
            var trinket = catalogs.Trinkets.All.FirstOrDefault(item =>
                string.Equals(item.CardId, cardId, StringComparison.OrdinalIgnoreCase));
            return trinket?.Name ?? cardId;
        }

        private string DifficultyLabel(string difficulty)
        {
            if (difficulty == StrategyGuideDifficulties.Showcase)
            {
                return T("简单模式", "Showcase");
            }
            if (difficulty == StrategyGuideDifficulties.GuidedDiscover)
            {
                return T("初级模式", "Guided Discover");
            }
            if (difficulty == StrategyGuideDifficulties.OpenBuild)
            {
                return T("困难模式", "Open Build");
            }
            return difficulty;
        }

        private static string JoinNames(IEnumerable<string> values, Func<string, string> display)
        {
            return string.Join("  ·  ", (values ?? Array.Empty<string>()).Select(display));
        }

        private float ShortUnits(float physicalSize, float regularSize = -1f)
        {
            if (layout.IsShortLandscape)
            {
                return layout.CanvasUnitsForPhysicalPixels(physicalSize);
            }

            return regularSize >= 0f ? regularSize : physicalSize;
        }

        private int ShortInt(float physicalSize, float regularSize = -1f)
        {
            return Mathf.CeilToInt(ShortUnits(physicalSize, regularSize));
        }

        private string T(string chinese, string english)
        {
            return useEnglish ? english : chinese;
        }

        private static string CreateDraftId(string guideId)
        {
            var safeGuide = new string((guideId ?? "guide").ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '-')
                .ToArray());
            var value = "draft-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + safeGuide;
            return value.Length <= 96 ? value : value.Substring(0, 96);
        }

        private static T Clone<T>(T value)
        {
            return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        }

        private static void ClearChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index -= 1)
            {
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(parent.GetChild(index).gameObject);
                }
                else
                {
                    UnityEngine.Object.Destroy(parent.GetChild(index).gameObject);
                }
#else
                UnityEngine.Object.Destroy(parent.GetChild(index).gameObject);
#endif
            }
        }
    }

    internal sealed class StrategyGuideAuthoringOperationRunner : MonoBehaviour
    {
        public void Run(IEnumerator routine)
        {
            StartCoroutine(routine);
        }
    }

    internal sealed class StrategyGuideAuthoringInputVisibilityHandler : MonoBehaviour, ISelectHandler
    {
        public Action Selected { get; set; }

        public void OnSelect(BaseEventData eventData)
        {
            Selected?.Invoke();
        }
    }
}
