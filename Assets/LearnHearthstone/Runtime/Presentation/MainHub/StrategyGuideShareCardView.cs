using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.MainHub
{
    public sealed class StrategyGuideShareCardView
    {
        private readonly Transform root;
        private readonly StrategyGuideShareCardModel model;
        private readonly UnityTavernLayoutContext layout;
        private readonly bool useEnglish;
        private readonly Action close;
        private readonly bool includeActions;

        public StrategyGuideShareCardView(
            Transform root,
            StrategyGuideShareCardModel model,
            UnityTavernLayoutContext layout,
            bool useEnglish,
            Action close,
            bool includeActions = true)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.layout = layout;
            this.useEnglish = useEnglish;
            this.close = close;
            this.includeActions = includeActions;
        }

        public GameObject Build()
        {
            var overlay = UiFactory.Panel(
                "StrategyGuideShareOverlay",
                root,
                UnityTavernUiStyle.WithAlpha(Color.black, 0.86f));
            overlay.GetComponent<Image>().raycastTarget = true;
            UiFactory.Stretch(overlay.GetComponent<RectTransform>());

            var shell = UiFactory.Panel(
                "StrategyGuideShareShell",
                overlay.transform,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.99f));
            var shellRect = shell.GetComponent<RectTransform>();
            shellRect.anchorMin = shellRect.anchorMax = new Vector2(0.5f, 0.5f);
            shellRect.pivot = new Vector2(0.5f, 0.5f);
            var maximumWidth = includeActions ? 1180f : Mathf.Max(320f, layout.Width - 48f);
            var maximumHeight = includeActions ? 680f : Mathf.Max(480f, layout.Height - 48f);
            var shellWidth = Mathf.Clamp(layout.Width - 24f, layout.IsCompact ? 1f : 320f, maximumWidth);
            var shellHeight = Mathf.Clamp(layout.Height - 24f, layout.IsCompact ? 1f : 480f, maximumHeight);
            var compensateScaledPreview = includeActions && layout.CanvasScaleFactor < 1f;
            shellRect.sizeDelta = compensateScaledPreview
                ? new Vector2(
                    layout.CanvasUnitsForPhysicalPixels(shellWidth),
                    layout.CanvasUnitsForPhysicalPixels(shellHeight))
                : new Vector2(shellWidth, shellHeight);
            UnityTavernUiStyle.ConfigureOutline(
                shell,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.72f),
                new Vector2(2f, -2f));
            UiFactory.Vertical(shell, layout.IsCompact ? 10 : 14, 10);

            if (includeActions)
            {
                BuildActions(shell.transform);
            }
            if (layout.IsCompact && includeActions)
            {
                var content = UiFactory.ScrollView(
                    "StrategyGuideShareCardScroll",
                    shell.transform,
                    Color.clear,
                    out _,
                    layout);
                var contentLayout = UiFactory.Vertical(content.gameObject, 0, 0);
                contentLayout.childControlWidth = true;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childForceExpandHeight = false;
                var card = BuildCard(content);
                UiFactory.SetHeight(card, layout.CanvasUnitsForPhysicalPixels(600f));
            }
            else
            {
                BuildCard(shell.transform);
            }
            if (includeActions)
            {
                overlay.AddComponent<UnityFocusTrap>().Activate(
                    FindButton(overlay.transform, "StrategyGuideShareCopyButton")?.gameObject);
            }
            return overlay;
        }

        private void BuildActions(Transform parent)
        {
            var rowObject = UiFactory.Panel("StrategyGuideShareActions", parent, Color.clear);
            UnityTavernUiStyle.SetFixedSize(
                rowObject,
                0f,
                layout.CanvasUnitsForPhysicalPixels(UnityTavernUiStyle.TouchHeight));
            var row = UiFactory.Horizontal(rowObject, 0, 8);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;

            var heading = UiFactory.Label(
                "StrategyGuideSharePreviewHeading",
                rowObject.transform,
                T("一图流预览", "Flow preview"),
                layout.IsCompact ? 20 : 24,
                FontStyle.Bold,
                layout);
            heading.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetFlexible(heading.gameObject, 1f, 0f);

            if (StrategyGuideShareCardPngExporter.CanExport)
            {
                var export = UiFactory.Button(
                    "StrategyGuideShareExportButton",
                    rowObject.transform,
                    T("导出 PNG", "Export PNG"),
                    () => ExportPng(heading),
                    layout);
                UnityTavernUiStyle.ConfigureButton(export, UnityTavernUiStyle.SuccessGreen, true);
                UiFactory.SetWidth(export.gameObject, layout.IsCompact ? 106f : 132f);
            }

            var copy = UiFactory.Button(
                "StrategyGuideShareCopyButton",
                rowObject.transform,
                T("复制本难度代码", "Copy profile code"),
                () =>
                {
                    GUIUtility.systemCopyBuffer = model.PublicCode;
                    heading.text = T("已复制 ✓", "Copied ✓");
                    heading.color = UnityTavernUiStyle.SuccessGreen;
                },
                layout);
            UnityTavernUiStyle.ConfigureButton(copy, UnityTavernUiStyle.Gold, true);
            UiFactory.SetWidth(copy.gameObject, layout.IsCompact ? 132f : 172f);

            var closeButton = UiFactory.Button(
                "StrategyGuideShareCloseButton",
                rowObject.transform,
                T("关闭", "Close"),
                () => close?.Invoke(),
                layout);
            UnityTavernUiStyle.ConfigureButton(closeButton, UnityTavernUiStyle.ArcaneBlue, false);
            UiFactory.SetWidth(closeButton.gameObject, layout.IsCompact ? 76f : 96f);
        }

        private GameObject BuildCard(Transform parent)
        {
            var card = UiFactory.Panel(
                "StrategyGuideShareCard",
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.TableDark, 0.98f));
            UiFactory.SetFlexible(card, 1f, 1f);
            UnityTavernUiStyle.ConfigureOutline(
                card,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.46f),
                new Vector2(1f, -1f));
            UiFactory.Vertical(card, layout.IsCompact ? 8 : 12, layout.IsCompact ? 6 : 8);

            BuildHeader(card.transform);
            BuildFlow(card.transform);
            BuildFooter(card.transform);
            return card;
        }

        private void BuildHeader(Transform parent)
        {
            var header = UiFactory.Panel("StrategyGuideShareHeader", parent, Color.clear);
            UnityTavernUiStyle.SetFixedSize(
                header,
                0f,
                layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(72f) : 82f);
            var row = UiFactory.Horizontal(header, 0, 10);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;

            var titles = UiFactory.Panel("StrategyGuideShareTitles", header.transform, Color.clear);
            UiFactory.SetFlexible(titles, 1f, 0f);
            UiFactory.Vertical(titles, 0, 1);
            var title = UiFactory.Label(
                "StrategyGuideShareTitle",
                titles.transform,
                model.Title + " · " + model.DifficultyTitle,
                layout.IsCompact ? 20 : 27,
                FontStyle.Bold,
                layout);
            title.color = UnityTavernUiStyle.TextLight;
            var meta = UiFactory.Label(
                "StrategyGuideShareStartMeta",
                titles.transform,
                T("第 ", "Round ") + model.StartRound +
                T(" 回合 · 酒馆 ", " · Tavern Tier ") + model.TavernTier +
                T(" 级 · 金币 ", " · Gold ") + model.Gold + "/" + model.MaxGold +
                " · " + model.Hero.Name,
                14,
                FontStyle.Bold,
                layout);
            meta.color = UnityTavernUiStyle.MutedText;
            meta.horizontalOverflow = HorizontalWrapMode.Overflow;
            meta.verticalOverflow = VerticalWrapMode.Overflow;
            UnityTavernUiStyle.SetFixedSize(
                meta.gameObject,
                0f,
                layout.CanvasUnitsForPhysicalPixels(20f));

            var identity = UiFactory.Panel("StrategyGuideShareIdentity", header.transform, UnityTavernUiStyle.PanelQuiet);
            UiFactory.SetWidth(identity, layout.IsCompact ? 196f : 270f);
            UiFactory.Vertical(identity, 6, 0);
            var version = UiFactory.Label(
                "StrategyGuideShareVersion",
                identity.transform,
                T("版本 ", "Version ") + model.GameVersionId,
                14,
                FontStyle.Bold,
                layout);
            version.color = UnityTavernUiStyle.Gold;
            var hash = UiFactory.Label(
                "StrategyGuideShareHash",
                identity.transform,
                "LHSG1 · " + model.ProfileId + " · " + model.ContentHashShort,
                14,
                FontStyle.Bold,
                layout);
            hash.color = UnityTavernUiStyle.MutedText;
        }

        private void BuildFlow(Transform parent)
        {
            var flow = UiFactory.Panel("StrategyGuideShareFlow", parent, Color.clear);
            UiFactory.SetFlexible(flow, 1f, 1f);
            var row = UiFactory.Horizontal(flow, 0, layout.IsCompact ? 8 : 12);
            row.childControlWidth = true;
            row.childForceExpandWidth = true;

            BuildStartingState(flow.transform);
            BuildPlan(flow.transform);
        }

        private void BuildStartingState(Transform parent)
        {
            var state = UiFactory.Panel(
                "StrategyGuideShareStartingState",
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.5f));
            UiFactory.SetFlexible(state, 2.4f, 1f);
            var stateLayout = state.GetComponent<LayoutElement>();
            stateLayout.minWidth = 1f;
            stateLayout.preferredWidth = 0f;
            stateLayout.flexibleWidth = 2.4f;
            var column = UiFactory.Vertical(state, layout.IsCompact ? 6 : 8, 5);
            column.childControlHeight = true;

            var heading = UiFactory.Label(
                "StrategyGuideShareStartingStateHeading",
                state.transform,
                T("起手局面", "Starting state"),
                16,
                FontStyle.Bold,
                layout);
            heading.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetFixedSize(
                heading.gameObject,
                0f,
                layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(24f) : 24f);

            BuildZone(state.transform, "Shop", T("酒馆", "Tavern"), model.StartingShop);
            BuildZone(state.transform, "Board", T("战场", "Warband"), model.StartingBoard);
            BuildZone(state.transform, "Hand", T("手牌", "Hand"), model.StartingHand);
        }

        private void BuildZone(
            Transform parent,
            string zoneId,
            string heading,
            IReadOnlyList<StrategyGuideShareCardAsset> cards)
        {
            var zone = UiFactory.Panel(
                "StrategyGuideShareZone-" + zoneId,
                parent,
                UnityTavernUiStyle.PanelQuiet);
            UiFactory.SetFlexible(zone, 1f, 1f);
            var row = UiFactory.Horizontal(zone, 5, 5);
            row.childControlWidth = true;
            row.childForceExpandWidth = false;

            var label = UiFactory.Label(
                "StrategyGuideShareZoneHeading-" + zoneId,
                zone.transform,
                heading,
                14,
                FontStyle.Bold,
                layout);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Gold;
            UiFactory.SetWidth(label.gameObject, layout.IsCompact ? 52f : 64f);

            if (cards == null || cards.Count == 0)
            {
                var empty = UiFactory.Label(
                    "StrategyGuideShareZoneEmpty-" + zoneId,
                    zone.transform,
                    T("无", "Empty"),
                    14,
                    FontStyle.Normal,
                    layout);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UiFactory.SetFlexible(empty.gameObject, 1f, 1f);
                return;
            }

            for (var index = 0; index < cards.Count; index += 1)
            {
                BuildStartingCard(zone.transform, zoneId, cards[index], index);
            }
        }

        private void BuildStartingCard(
            Transform parent,
            string zoneId,
            StrategyGuideShareCardAsset asset,
            int index)
        {
            var tile = UiFactory.Panel(
                "StrategyGuideShareStartCard-" + zoneId + "-" + index,
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.92f));
            UiFactory.SetFlexible(tile, 1f, 1f);
            var tileLayout = tile.GetComponent<LayoutElement>();
            tileLayout.minWidth = 1f;
            tileLayout.preferredWidth = 0f;
            tileLayout.flexibleWidth = 1f;
            UnityTavernUiStyle.ConfigureOutline(
                tile,
                UnityTavernUiStyle.WithAlpha(asset.Golden ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Brass, 0.5f),
                new Vector2(1f, -1f));
            UiFactory.Vertical(tile, 3, 2);

            AddCardImage(tile.transform, "StrategyGuideShareStartImage-" + zoneId + "-" + index, asset);
            var name = UiFactory.Label(
                "StrategyGuideShareStartName-" + zoneId + "-" + index,
                tile.transform,
                asset.Name,
                14,
                FontStyle.Bold,
                layout);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = UnityTavernUiStyle.TextLight;
            UnityTavernUiStyle.SetFixedSize(
                name.gameObject,
                0f,
                layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(30f) : 34f);

            var stats = UiFactory.Label(
                "StrategyGuideShareStartStats-" + zoneId + "-" + index,
                tile.transform,
                CardStats(asset),
                14,
                FontStyle.Bold,
                layout);
            stats.alignment = TextAnchor.MiddleCenter;
            stats.color = asset.Golden ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(
                stats.gameObject,
                0f,
                layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(20f) : 22f);
        }

        private void BuildPlan(Transform parent)
        {
            var plan = UiFactory.Panel(
                "StrategyGuideSharePlan",
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.5f));
            UiFactory.SetFlexible(plan, 1f, 1f);
            var planLayout = plan.GetComponent<LayoutElement>();
            planLayout.minWidth = 1f;
            planLayout.preferredWidth = 0f;
            planLayout.flexibleWidth = 1f;
            var column = UiFactory.Vertical(plan, layout.IsCompact ? 6 : 8, 6);
            column.childControlHeight = true;

            BuildLearningGoal(plan.transform);
            BuildKeyDecisions(plan.transform);
            BuildShapingTimeline(plan.transform);
            BuildCompletion(plan.transform);
        }

        private void BuildLearningGoal(Transform parent)
        {
            var panel = FlowPanel(parent, "StrategyGuideShareLearningGoal", 1f);
            BuildFlowHeading(panel.transform, "StrategyGuideShareLearningGoalHeading", T("学习目标", "Learning goal"));
            var value = UiFactory.Label(
                "StrategyGuideShareLearningGoalValue",
                panel.transform,
                model.LearningGoal,
                14,
                FontStyle.Bold,
                layout);
            value.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetFlexible(value.gameObject, 1f, 1f);
        }

        private void BuildKeyDecisions(Transform parent)
        {
            var panel = FlowPanel(parent, "StrategyGuideShareKeyDecisions", 1.35f);
            BuildFlowHeading(panel.transform, "StrategyGuideShareKeyDecisionsHeading", T("关键判断", "Key decisions"));
            if (model.KeyDecisions.Count == 0)
            {
                BuildEmptyFlowValue(panel.transform, "StrategyGuideShareDecision-Empty");
                return;
            }

            for (var index = 0; index < model.KeyDecisions.Count; index += 1)
            {
                var decision = UiFactory.Label(
                    "StrategyGuideShareDecision-" + (index + 1),
                    panel.transform,
                    (index + 1) + ". " + model.KeyDecisions[index],
                    14,
                    FontStyle.Normal,
                    layout);
                decision.color = UnityTavernUiStyle.TextLight;
                UiFactory.SetFlexible(decision.gameObject, 1f, 1f);
            }
        }

        private void BuildShapingTimeline(Transform parent)
        {
            var panel = FlowPanel(parent, "StrategyGuideShareShapingTimeline", 1.15f);
            BuildFlowHeading(panel.transform, "StrategyGuideShareShapingHeading", T("逐回合塑造", "Shaping by turn"));
            if (model.ShapingTurns.Count == 0)
            {
                BuildEmptyFlowValue(panel.transform, "StrategyGuideShareShapingTurn-Empty");
                return;
            }

            var row = UiFactory.Panel("StrategyGuideShareShapingTurns", panel.transform, Color.clear);
            UiFactory.SetFlexible(row, 1f, 1f);
            var rowLayout = UiFactory.Horizontal(row, 0, 5);
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            foreach (var turn in model.ShapingTurns)
            {
                var item = UiFactory.Panel(
                    "StrategyGuideShareShapingTurn-" + turn.LocalTurn,
                    row.transform,
                    UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.9f));
                UiFactory.SetFlexible(item, 1f, 1f);
                UiFactory.Vertical(item, 4, 2);
                var turnLabel = UiFactory.Label(
                    "StrategyGuideShareShapingTurnLabel-" + turn.LocalTurn,
                    item.transform,
                    T("第 ", "Turn ") + turn.LocalTurn + T(" 回合", string.Empty),
                    14,
                    FontStyle.Bold,
                    layout);
                turnLabel.alignment = TextAnchor.MiddleCenter;
                turnLabel.color = UnityTavernUiStyle.Gold;
                AddCardImage(
                    item.transform,
                    "StrategyGuideShareShapingImage-" + turn.LocalTurn,
                    turn.Spell);
                var spell = UiFactory.Label(
                    "StrategyGuideShareShapingSpell-" + turn.LocalTurn,
                    item.transform,
                    turn.Spell?.Name ?? T("未配置", "Not configured"),
                    14,
                    FontStyle.Bold,
                    layout);
                spell.alignment = TextAnchor.MiddleCenter;
                spell.color = UnityTavernUiStyle.TextLight;
                UiFactory.SetFlexible(spell.gameObject, 1f, 1f);
            }
        }

        private void BuildCompletion(Transform parent)
        {
            var panel = FlowPanel(parent, "StrategyGuideShareCompletion", 1.1f);
            BuildFlowHeading(panel.transform, "StrategyGuideShareCompletionHeading", T("成长与完成", "Growth and finish"));
            var growthText = model.GrowthTargets.Count == 0
                ? T("无额外成长阈值", "No additional growth threshold")
                : string.Join(" · ", model.GrowthTargets.Select(item => item.Label + " ≥ " + item.MinimumValue));
            var growth = UiFactory.Label(
                "StrategyGuideShareGrowthTargets",
                panel.transform,
                growthText,
                14,
                FontStyle.Bold,
                layout);
            growth.color = model.GrowthTargets.Count == 0
                ? UnityTavernUiStyle.MutedText
                : UnityTavernUiStyle.Gold;
            var condition = UiFactory.Label(
                "StrategyGuideShareCompletionCondition",
                panel.transform,
                model.CompletionCondition,
                14,
                FontStyle.Normal,
                layout);
            condition.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetFlexible(condition.gameObject, 1f, 1f);
        }

        private GameObject FlowPanel(Transform parent, string name, float flexibleHeight)
        {
            var panel = UiFactory.Panel(name, parent, UnityTavernUiStyle.PanelQuiet);
            UiFactory.SetFlexible(panel, 1f, flexibleHeight);
            UiFactory.Vertical(panel, 6, 2);
            return panel;
        }

        private void BuildFlowHeading(Transform parent, string name, string text)
        {
            var heading = UiFactory.Label(name, parent, text, 14, FontStyle.Bold, layout);
            heading.color = UnityTavernUiStyle.Gold;
            UiFactory.SetHeight(heading.gameObject, 20f);
        }

        private void BuildEmptyFlowValue(Transform parent, string name)
        {
            var empty = UiFactory.Label(name, parent, T("未配置", "Not configured"), 14, FontStyle.Normal, layout);
            empty.color = UnityTavernUiStyle.MutedText;
            UiFactory.SetFlexible(empty.gameObject, 1f, 1f);
        }

        private void BuildFooter(Transform parent)
        {
            var footer = UiFactory.Panel("StrategyGuideShareFooter", parent, Color.clear);
            UnityTavernUiStyle.SetFixedSize(
                footer,
                0f,
                layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(50f) : 58f);
            UiFactory.Vertical(footer, 0, 2);
            var probability = UiFactory.Label(
                "StrategyGuideShareProbability",
                footer.transform,
                model.ProbabilityNotice,
                14,
                FontStyle.Bold,
                layout);
            probability.color = UnityTavernUiStyle.Gold;
            var disclaimer = UiFactory.Label(
                "StrategyGuideShareDisclaimer",
                footer.transform,
                model.Disclaimer,
                14,
                FontStyle.Normal,
                layout);
            disclaimer.color = UnityTavernUiStyle.MutedText;
        }

        private void AddCardImage(
            Transform parent,
            string name,
            StrategyGuideShareCardAsset asset)
        {
            var imageObject = UiFactory.Panel(
                name,
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.9f));
            UiFactory.SetFlexible(imageObject, 1f, 1f);
            var image = imageObject.GetComponent<Image>();
            image.sprite = asset == null
                ? null
                : CardImageProvider.LoadSprite(asset.ImagePath, asset.StableId, asset.CardKind);
            image.color = image.sprite == null ? UnityTavernUiStyle.PanelQuiet : Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (image.sprite == null)
            {
                UiFactory.SetMinSize(imageObject, 0f, layout.CanvasUnitsForPhysicalPixels(20f));
                var fallback = UiFactory.Label(
                    name + "Fallback",
                    imageObject.transform,
                    UnityTavernUiStyle.ArtFallbackText(asset?.Name, T("无图", "NA")),
                    14,
                    FontStyle.Bold,
                    layout);
                fallback.alignment = TextAnchor.MiddleCenter;
                fallback.color = UnityTavernUiStyle.MutedText;
                UiFactory.Stretch(fallback.rectTransform);
            }
        }

        private string CardStats(StrategyGuideShareCardAsset asset)
        {
            var prefix = asset.Golden ? T("金色 · ", "Golden · ") : string.Empty;
            if (asset.CardKind == CardKind.Minion)
            {
                return prefix + asset.Attack + "/" + asset.Health + " · " + asset.TavernTier + T(" 星", "★");
            }
            if (asset.CardKind == CardKind.TavernSpell || asset.CardKind == CardKind.Spell)
            {
                return prefix + asset.Cost + T(" 金币", " Gold");
            }
            return prefix + T("起手资源", "Starting resource");
        }

        private static Button FindButton(Transform parent, string name)
        {
            return parent.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(item => string.Equals(item.name, name, StringComparison.Ordinal));
        }

        private void ExportPng(Text status)
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                var result = StrategyGuideShareCardPngExporter.ExportToBrowser(model, useEnglish);
                status.text = T("浏览器下载已开始：", "Browser download started: ") + result.Path;
#else
                var directory = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "ShareCards");
                var result = StrategyGuideShareCardPngExporter.Export(model, useEnglish, directory);
                GUIUtility.systemCopyBuffer = result.Path;
                status.fontSize = 14;
                status.text = T("已导出，路径已复制：", "Exported; path copied: ") + result.Path;
#endif
                status.color = UnityTavernUiStyle.SuccessGreen;
            }
            catch (Exception exception)
            {
                status.text = T("导出失败：", "Export failed: ") + exception.Message;
                status.color = UnityTavernUiStyle.DangerRed;
            }
        }

        private string T(string chinese, string english)
        {
            return useEnglish ? english : chinese;
        }
    }
}
