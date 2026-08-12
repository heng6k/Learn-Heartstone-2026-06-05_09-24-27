using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    [DisallowMultipleComponent]
    public sealed class UnityTavernChoiceQueueModalComponent : MonoBehaviour
    {
        public static UnityTavernChoiceQueueModalComponent Create(Transform parent)
        {
            var overlay = UiFactory.Panel(
                "UnityDarkGiftChoiceOverlay",
                parent,
                new Color(0f, 0f, 0f, 0.76f));
            UiFactory.Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().raycastTarget = true;
            overlay.transform.SetAsLastSibling();
            return overlay.AddComponent<UnityTavernChoiceQueueModalComponent>();
        }

        public void Build(
            MechanicChoiceRequest request,
            ChoiceQueueItem queued,
            UnityTavernLayoutContext layout,
            bool useEnglish,
            int selectedIndex,
            int compactPageIndex,
            bool showBlockingExplanation,
            Action<int> select,
            Action<int> changePage,
            Action confirm,
            Action explainBlocking)
        {
            if (request == null)
            {
                return;
            }

            var panel = UiFactory.Panel("UnityDarkGiftChoicePanel", transform, UnityTavernUiStyle.SurfaceDark);
            ConfigurePanel(panel, layout);
            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(
                CompactInt(layout, 14f),
                CompactInt(layout, 14f),
                CompactInt(layout, 12f),
                CompactInt(layout, 12f));
            panelLayout.spacing = CompactUnits(layout, 8f);
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            BuildHeader(panel.transform, request, queued, layout, useEnglish, selectedIndex);

            Button initialFocus;
            if (layout.IsCompact)
            {
                initialFocus = BuildCompactOptions(
                    panel.transform,
                    request,
                    layout,
                    useEnglish,
                    selectedIndex,
                    compactPageIndex,
                    select,
                    changePage);
            }
            else
            {
                initialFocus = BuildWideOptions(
                    panel.transform,
                    request,
                    layout,
                    useEnglish,
                    selectedIndex,
                    select);
            }

            if (showBlockingExplanation)
            {
                var explanation = UiFactory.Label(
                    "UnityDarkGiftChoiceBlockingExplanation",
                    panel.transform,
                    useEnglish
                        ? "This choice is blocking. Complete the required selection before returning to the match."
                        : "这是阻塞选择，必须完成所需选择后才能返回战局。",
                    14,
                    FontStyle.Bold,
                    layout);
                explanation.color = UnityTavernUiStyle.Gold;
                explanation.alignment = TextAnchor.MiddleCenter;
                UnityTavernUiStyle.SetPreferredHeight(
                    explanation.gameObject,
                    layout.CanvasUnitsForPhysicalPixels(layout.IsCompact ? 34f : 28f));
            }

            var actions = UiFactory.Panel("UnityDarkGiftChoiceActions", panel.transform, Color.clear);
            var actionLayout = actions.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = CompactUnits(layout, 10f);
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = false;
            UnityTavernUiStyle.SetPreferredHeight(
                actions,
                layout.CanvasUnitsForPhysicalPixels(layout.IsCompact ? 54f : 52f));

            var why = UiFactory.Button(
                "UnityDarkGiftChoiceWhyBlockedButton",
                actions.transform,
                useEnglish ? "Why can't I go back?" : "为什么不能返回？",
                () => explainBlocking?.Invoke(),
                layout);
            UnityTavernUiStyle.SetFixedSize(
                why.gameObject,
                layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(156f) : 220f,
                layout.CanvasUnitsForPhysicalPixels(UnityTavernUiStyle.TouchHeight));
            var confirmButton = UiFactory.Button(
                "UnityDarkGiftChoiceConfirmButton",
                actions.transform,
                useEnglish ? "Confirm selection" : "确认选择",
                () => confirm?.Invoke(),
                layout);
            UnityTavernUiStyle.SetFixedSize(
                confirmButton.gameObject,
                layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(190f) : 240f,
                layout.CanvasUnitsForPhysicalPixels(UnityTavernUiStyle.TouchHeight));
            confirmButton.interactable = selectedIndex >= 0 && selectedIndex < request.Options.Count;
            UnityTavernUiStyle.ConfigureOutline(
                confirmButton.gameObject,
                confirmButton.interactable
                    ? UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Green, 0.86f)
                    : UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Disabled, 0.72f),
                new Vector2(2f, -2f));

            panel.AddComponent<UnityFocusTrap>().Activate(
                selectedIndex >= 0 && confirmButton.interactable
                    ? confirmButton.gameObject
                    : initialFocus != null
                        ? initialFocus.gameObject
                        : why.gameObject);
        }

        private static void ConfigurePanel(GameObject panel, UnityTavernLayoutContext layout)
        {
            UnityTavernUiStyle.ConfigureSurface(panel, UnityTavernUiStyle.SurfaceDark);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.78f),
                new Vector2(2f, -2f));
            UnityTavernUiStyle.AddStarLanternRail(
                panel.transform,
                "UnityDarkGiftChoiceStarLantern",
                UnityTavernUiStyle.Gold);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            var inset = layout.IsCompact ? 12f : 48f;
            var physicalWidth = Mathf.Max(
                280f,
                Mathf.Min(layout.Width - inset * 2f, layout.IsCompact ? 820f : 1180f));
            var physicalHeight = Mathf.Max(
                260f,
                Mathf.Min(layout.Height - inset * 2f, layout.IsCompact ? 366f : 720f));
            rect.sizeDelta = new Vector2(
                layout.CanvasUnitsForPhysicalPixels(physicalWidth),
                layout.CanvasUnitsForPhysicalPixels(physicalHeight));
            rect.anchoredPosition = Vector2.zero;
        }

        private static void BuildHeader(
            Transform parent,
            MechanicChoiceRequest request,
            ChoiceQueueItem queued,
            UnityTavernLayoutContext layout,
            bool useEnglish,
            int selectedIndex)
        {
            var header = UiFactory.Panel("UnityDarkGiftChoiceHeader", parent, UnityTavernUiStyle.SurfaceRaised);
            UnityTavernUiStyle.ConfigureOutline(
                header,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.46f),
                new Vector2(1f, -1f));
            var headerLayout = header.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(
                CompactInt(layout, 8f),
                CompactInt(layout, 8f),
                CompactInt(layout, 5f),
                CompactInt(layout, 5f));
            headerLayout.spacing = CompactUnits(layout, 2f);
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;
            UnityTavernUiStyle.SetPreferredHeight(header, CompactUnits(layout, layout.IsCompact ? 56f : 64f));

            var title = UiFactory.Label(
                "UnityDarkGiftChoiceTitle",
                header.transform,
                useEnglish ? "Choose a Dark Gift" : "选择黑暗之赐",
                20,
                FontStyle.Bold,
                layout);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, CompactUnits(layout, layout.IsCompact ? 24f : 28f));

            var cost = CostText(request, queued, useEnglish);
            var rawSource = string.IsNullOrWhiteSpace(queued?.Source) ? request.Source : queued.Source;
            var source = DisplaySource(rawSource, useEnglish);
            var round = queued?.CreatedRound > 0 ? queued.CreatedRound : request.Round;
            var picked = selectedIndex >= 0 ? 1 : 0;
            var required = Math.Max(1, request.RemainingPicks);
            var blocking = queued?.Blocking == true;
            var stack = Metadata(queued, "stack-policy") ?? StackTag(request);
            var metadata = UiFactory.Label(
                "UnityDarkGiftChoiceMetadata",
                header.transform,
                useEnglish
                    ? "Source: " + source + " · Round " + round + " · " + cost + " · Selected " + picked + "/" + required +
                      (blocking ? " · Blocking" : string.Empty) + (string.IsNullOrWhiteSpace(stack) ? string.Empty : " · Stack: " + stack)
                    : "来源：" + source + " · 第 " + round + " 回合 · " + cost + " · 已选 " + picked + "/" + required +
                      (blocking ? " · 阻塞" : string.Empty) + (string.IsNullOrWhiteSpace(stack) ? string.Empty : " · 叠加：" + stack),
                14,
                FontStyle.Bold,
                layout);
            metadata.alignment = TextAnchor.MiddleCenter;
            metadata.color = blocking ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(metadata.gameObject, CompactUnits(layout, layout.IsCompact ? 20f : 22f));
        }

        private static string DisplaySource(string source, bool useEnglish)
        {
            if (string.Equals(source, Season14DarkGiftSourceService.NormalEntrySourceId, StringComparison.Ordinal))
            {
                return useEnglish ? "Dark Gift button" : "黑暗之赐按钮";
            }

            return source;
        }

        private static Button BuildWideOptions(
            Transform parent,
            MechanicChoiceRequest request,
            UnityTavernLayoutContext layout,
            bool useEnglish,
            int selectedIndex,
            Action<int> select)
        {
            var options = UiFactory.Panel("UnityDarkGiftChoiceGrid", parent, Color.clear);
            UnityTavernUiStyle.SetFlexible(options, 1f, 1f);
            var row = options.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12f;
            row.childAlignment = TextAnchor.UpperCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            Button first = null;
            for (var index = 0; index < request.Options.Count; index += 1)
            {
                var captured = index;
                var button = BuildChoiceCard(
                    options.transform,
                    request.Options[index],
                    index,
                    index == selectedIndex,
                    layout,
                    useEnglish,
                    () => select?.Invoke(captured));
                first = first ?? button;
            }

            return first;
        }

        private static Button BuildCompactOptions(
            Transform parent,
            MechanicChoiceRequest request,
            UnityTavernLayoutContext layout,
            bool useEnglish,
            int selectedIndex,
            int compactPageIndex,
            Action<int> select,
            Action<int> changePage)
        {
            var options = UiFactory.Panel("UnityDarkGiftChoicePager", parent, Color.clear);
            UnityTavernUiStyle.SetFlexible(options, 1f, 1f);
            var row = options.AddComponent<HorizontalLayoutGroup>();
            row.spacing = CompactUnits(layout, 6f);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            var count = request.Options.Count;
            var page = count == 0 ? 0 : Mathf.Clamp(compactPageIndex, 0, count - 1);
            var previous = UiFactory.Button(
                "UnityDarkGiftChoicePreviousButton",
                options.transform,
                "‹",
                () => changePage?.Invoke(count == 0 ? 0 : (page - 1 + count) % count),
                layout);
            UnityTavernUiStyle.SetFixedSize(
                previous.gameObject,
                layout.CanvasUnitsForPhysicalPixels(48f),
                layout.CanvasUnitsForPhysicalPixels(48f));

            Button selectButton = null;
            if (count > 0)
            {
                selectButton = BuildChoiceCard(
                    options.transform,
                    request.Options[page],
                    page,
                    page == selectedIndex,
                    layout,
                    useEnglish,
                    () => select?.Invoke(page));
            }

            var next = UiFactory.Button(
                "UnityDarkGiftChoiceNextButton",
                options.transform,
                "›",
                () => changePage?.Invoke(count == 0 ? 0 : (page + 1) % count),
                layout);
            UnityTavernUiStyle.SetFixedSize(
                next.gameObject,
                layout.CanvasUnitsForPhysicalPixels(48f),
                layout.CanvasUnitsForPhysicalPixels(48f));

            var pageLabel = UiFactory.Label(
                "UnityDarkGiftChoicePage",
                options.transform,
                count == 0 ? "0/0" : (page + 1) + "/" + count,
                14,
                FontStyle.Bold,
                layout);
            pageLabel.alignment = TextAnchor.MiddleCenter;
            pageLabel.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(
                pageLabel.gameObject,
                layout.CanvasUnitsForPhysicalPixels(52f),
                layout.CanvasUnitsForPhysicalPixels(24f));
            pageLabel.transform.SetSiblingIndex(next.transform.GetSiblingIndex());
            return selectButton ?? previous;
        }

        private static Button BuildChoiceCard(
            Transform parent,
            MechanicChoiceOption option,
            int index,
            bool selected,
            UnityTavernLayoutContext layout,
            bool useEnglish,
            Action select)
        {
            var card = UiFactory.Panel("UnityDarkGiftChoiceCard-" + index, parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.ConfigureOutline(
                card,
                selected
                    ? UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Green, 0.96f)
                    : UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.48f),
                selected ? new Vector2(3f, -3f) : new Vector2(1f, -1f));
            if (layout.IsCompact)
            {
                var compactLayout = card.AddComponent<HorizontalLayoutGroup>();
                compactLayout.padding = new RectOffset(
                    CompactInt(layout, 8f),
                    CompactInt(layout, 8f),
                    CompactInt(layout, 8f),
                    CompactInt(layout, 8f));
                compactLayout.spacing = CompactUnits(layout, 8f);
                compactLayout.childAlignment = TextAnchor.MiddleCenter;
                compactLayout.childControlWidth = true;
                compactLayout.childControlHeight = true;
                compactLayout.childForceExpandWidth = false;
                compactLayout.childForceExpandHeight = true;
            }
            else
            {
                var cardLayout = card.AddComponent<VerticalLayoutGroup>();
                cardLayout.padding = new RectOffset(8, 8, 7, 7);
                cardLayout.spacing = 4f;
                cardLayout.childControlWidth = true;
                cardLayout.childControlHeight = true;
                cardLayout.childForceExpandWidth = true;
                cardLayout.childForceExpandHeight = false;
            }

            UnityTavernUiStyle.SetFlexible(card, 1f, 1f);

            BuildCombinationArt(card.transform, option, layout);
            var detailsParent = card.transform;
            if (layout.IsCompact)
            {
                var details = UiFactory.Panel("UnityDarkGiftChoiceCompactDetails-" + index, card.transform, Color.clear);
                var detailsLayout = details.AddComponent<VerticalLayoutGroup>();
                detailsLayout.spacing = CompactUnits(layout, 4f);
                detailsLayout.childControlWidth = true;
                detailsLayout.childControlHeight = true;
                detailsLayout.childForceExpandWidth = true;
                detailsLayout.childForceExpandHeight = false;
                UnityTavernUiStyle.SetFlexible(details, 1f, 1f);
                detailsParent = details.transform;
            }

            BuildMinionSummary(detailsParent, option, index, useEnglish, layout);
            var name = UiFactory.Label(
                "UnityDarkGiftChoiceCombinationName-" + index,
                detailsParent,
                (option?.DisplayName ?? string.Empty) + " + " + (option?.RewardName ?? string.Empty),
                15,
                FontStyle.Bold,
                layout);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = UnityTavernUiStyle.Text;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetPreferredHeight(
                name.gameObject,
                CompactUnits(layout, layout.IsCompact ? 26f : 36f));

            BuildRuleSection(
                detailsParent,
                "UnityDarkGiftChoiceMinion",
                index,
                useEnglish ? "MINION" : "随从效果",
                option?.Text,
                UnityTavernUiStyle.ArcaneBlue,
                layout);
            BuildRuleSection(
                detailsParent,
                "UnityDarkGiftChoiceGift",
                index,
                useEnglish ? "DARK GIFT" : "黑赐效果",
                option?.RewardText,
                UnityTavernUiStyle.Gold,
                layout);

            if (selected && !layout.IsCompact)
            {
                var marker = UiFactory.Label(
                    "UnityDarkGiftChoiceSelectedMarker-" + index,
                    card.transform,
                    useEnglish ? "✓ Selected" : "✓ 已选择",
                    14,
                    FontStyle.Bold,
                    layout);
                marker.alignment = TextAnchor.MiddleCenter;
                marker.color = UnityTavernUiStyle.Green;
                UnityTavernUiStyle.SetPreferredHeight(marker.gameObject, 22f);
            }

            return UiFactory.Button(
                "UnityDarkGiftChoiceSelectButton-" + index,
                detailsParent,
                selected ? (useEnglish ? "Selected" : "已选择") : (useEnglish ? "Select" : "选择"),
                () => select?.Invoke(),
                layout);
        }

        private static void BuildRuleSection(
            Transform parent,
            string prefix,
            int index,
            string headingText,
            string bodyText,
            Color accent,
            UnityTavernLayoutContext layout)
        {
            var section = UiFactory.Panel(prefix + "Section-" + index, parent, UnityTavernUiStyle.PanelQuiet);
            if (layout.IsCompact)
            {
                var compactLayout = section.AddComponent<HorizontalLayoutGroup>();
                compactLayout.padding = new RectOffset(
                    CompactInt(layout, 7f),
                    CompactInt(layout, 7f),
                    CompactInt(layout, 4f),
                    CompactInt(layout, 4f));
                compactLayout.spacing = CompactUnits(layout, 6f);
                compactLayout.childControlWidth = true;
                compactLayout.childControlHeight = true;
                compactLayout.childForceExpandWidth = false;
                compactLayout.childForceExpandHeight = true;
            }
            else
            {
                var sectionLayout = section.AddComponent<VerticalLayoutGroup>();
                sectionLayout.padding = new RectOffset(7, 7, 4, 5);
                sectionLayout.spacing = 2f;
                sectionLayout.childControlWidth = true;
                sectionLayout.childControlHeight = true;
                sectionLayout.childForceExpandWidth = true;
                sectionLayout.childForceExpandHeight = false;
            }

            UnityTavernUiStyle.ConfigureOutline(
                section,
                UnityTavernUiStyle.WithAlpha(accent, 0.34f),
                new Vector2(1f, -1f));
            UnityTavernUiStyle.SetPreferredHeight(
                section,
                CompactUnits(layout, layout.IsCompact ? 34f : 80f));

            var heading = UiFactory.Label(
                prefix + "Heading-" + index,
                section.transform,
                headingText,
                14,
                FontStyle.Bold,
                layout);
            heading.alignment = TextAnchor.MiddleLeft;
            heading.color = accent;
            if (layout.IsCompact)
            {
                UnityTavernUiStyle.SetFixedSize(
                    heading.gameObject,
                    layout.CanvasUnitsForPhysicalPixels(86f),
                    layout.CanvasUnitsForPhysicalPixels(24f));
            }
            else
            {
                UnityTavernUiStyle.SetPreferredHeight(heading.gameObject, 18f);
            }

            var body = UiFactory.Label(
                prefix + "Text-" + index,
                section.transform,
                bodyText ?? string.Empty,
                14,
                FontStyle.Normal,
                layout);
            body.alignment = TextAnchor.UpperLeft;
            body.color = UnityTavernUiStyle.Text;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            UnityTavernUiStyle.SetFlexible(body.gameObject, 1f, layout.IsCompact ? 1f : 0f);
        }

        private static void BuildCombinationArt(Transform parent, MechanicChoiceOption option, UnityTavernLayoutContext layout)
        {
            var row = UiFactory.Panel("UnityDarkGiftChoiceArtRow", parent, Color.clear);
            var minionWidth = layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(90f) : 126f;
            var minionHeight = layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(124f) : 174f;
            var giftWidth = layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(58f) : 78f;
            var giftHeight = layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(80f) : 108f;
            var minionArt = BuildArt(
                row.transform,
                "UnityDarkGiftChoiceMinionArt",
                option?.ImagePath,
                option?.SourceId,
                CardKind.Minion,
                minionWidth,
                minionHeight,
                option?.DisplayName,
                UnityTavernUiStyle.ArcaneBlue,
                layout);
            PositionCentered(
                minionArt.GetComponent<RectTransform>(),
                new Vector2(layout.IsCompact ? -layout.CanvasUnitsForPhysicalPixels(12f) : -18f, 0f));

            var attachment = UiFactory.Panel("UnityDarkGiftChoiceGiftAttachment", row.transform, Color.clear);
            var attachmentRect = attachment.GetComponent<RectTransform>();
            attachmentRect.anchorMin = attachmentRect.anchorMax = new Vector2(0.5f, 0.5f);
            attachmentRect.pivot = new Vector2(0.5f, 0.5f);
            attachmentRect.sizeDelta = new Vector2(giftWidth, giftHeight);
            attachmentRect.anchoredPosition = new Vector2(
                layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(43f) : 66f,
                layout.IsCompact ? -layout.CanvasUnitsForPhysicalPixels(18f) : -18f);
            var giftArt = BuildArt(
                attachment.transform,
                "UnityDarkGiftChoiceGiftArt",
                option?.RewardImagePath,
                option?.RewardId,
                CardKind.Trinket,
                giftWidth,
                giftHeight,
                option?.RewardName,
                UnityTavernUiStyle.Gold,
                layout);
            PositionCentered(giftArt.GetComponent<RectTransform>(), Vector2.zero);
            if (layout.IsCompact)
            {
                UnityTavernUiStyle.SetFixedSize(
                    row,
                    layout.CanvasUnitsForPhysicalPixels(150f),
                    layout.CanvasUnitsForPhysicalPixels(184f));
            }
            else
            {
                UnityTavernUiStyle.SetPreferredHeight(row, minionHeight);
            }
        }

        private static GameObject BuildArt(
            Transform parent,
            string name,
            string imagePath,
            string cardId,
            CardKind kind,
            float width,
            float height,
            string fallbackText,
            Color accent,
            UnityTavernLayoutContext layout)
        {
            var art = UiFactory.Panel(name, parent, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.SetFixedSize(art, width, height);
            UnityTavernUiStyle.ConfigureOutline(
                art,
                UnityTavernUiStyle.WithAlpha(accent, 0.68f),
                new Vector2(1f, -1f));
            var image = art.GetComponent<Image>();
            image.raycastTarget = false;
            var sprite = CardImageProvider.LoadSprite(imagePath, cardId, kind);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
                image.color = Color.white;
                return art;
            }

            var fallbackSurface = kind == CardKind.Trinket
                ? Color.Lerp(UnityTavernUiStyle.PanelQuiet, UnityTavernUiStyle.Brass, 0.14f)
                : Color.Lerp(UnityTavernUiStyle.PanelQuiet, UnityTavernUiStyle.ArcaneBlue, 0.12f);
            image.color = UnityTavernUiStyle.WithAlpha(fallbackSurface, 0.96f);
            var fallback = UiFactory.Label(
                name + "Fallback",
                art.transform,
                string.IsNullOrWhiteSpace(fallbackText) ? cardId ?? string.Empty : fallbackText,
                14,
                FontStyle.Bold,
                layout);
            UiFactory.Stretch(fallback.rectTransform);
            fallback.alignment = TextAnchor.MiddleCenter;
            fallback.color = UnityTavernUiStyle.Text;
            fallback.horizontalOverflow = HorizontalWrapMode.Wrap;
            fallback.verticalOverflow = VerticalWrapMode.Truncate;
            fallback.raycastTarget = false;
            return art;
        }

        private static void PositionCentered(RectTransform rect, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
        }

        private static void BuildMinionSummary(
            Transform parent,
            MechanicChoiceOption option,
            int index,
            bool useEnglish,
            UnityTavernLayoutContext layout)
        {
            var parts = new List<string>();
            if (option?.DifficultyTier > 0)
            {
                parts.Add(useEnglish ? "Tier " + option.DifficultyTier : option.DifficultyTier + " 星");
            }

            var tribes = (option?.Tribes ?? new List<Tribe>())
                .Where(tribe => tribe != Tribe.None)
                .Take(2)
                .Select(tribe => TribeName(tribe, useEnglish))
                .ToArray();
            if (tribes.Length > 0)
            {
                parts.Add(string.Join("/", tribes));
            }

            if (option != null && option.Health > 0)
            {
                parts.Add(Math.Max(0, option.Attack) + "/" + option.Health);
            }

            var summary = UiFactory.Label(
                "UnityDarkGiftChoiceMinionSummary-" + index,
                parent,
                string.Join(" · ", parts),
                14,
                FontStyle.Bold,
                layout);
            summary.alignment = TextAnchor.MiddleCenter;
            summary.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(
                summary.gameObject,
                CompactUnits(layout, 22f));
        }

        private static float CompactUnits(UnityTavernLayoutContext layout, float value)
        {
            return layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(value) : value;
        }

        private static int CompactInt(UnityTavernLayoutContext layout, float value)
        {
            return Mathf.CeilToInt(CompactUnits(layout, value));
        }

        private static string TribeName(Tribe tribe, bool useEnglish)
        {
            if (useEnglish)
            {
                return tribe == Tribe.All ? "All" : tribe.ToString();
            }

            switch (tribe)
            {
                case Tribe.Beast: return "野兽";
                case Tribe.Mech: return "机械";
                case Tribe.Demon: return "恶魔";
                case Tribe.Dragon: return "龙";
                case Tribe.Pirate: return "海盗";
                case Tribe.Elemental: return "元素";
                case Tribe.Quilboar: return "野猪人";
                case Tribe.Undead: return "亡灵";
                case Tribe.Naga: return "纳迦";
                case Tribe.All: return "全部种族";
                default: return "无种族";
            }
        }

        private static string CostText(MechanicChoiceRequest request, ChoiceQueueItem queued, bool useEnglish)
        {
            var metadata = Metadata(queued, "gold-cost");
            if (!int.TryParse(metadata, out var cost))
            {
                cost = request.Options == null || request.Options.Count == 0
                    ? 0
                    : request.Options.Max(option => Math.Max(0, option?.Cost ?? 0));
            }

            return cost + (useEnglish ? " Gold" : " 金币");
        }

        private static string Metadata(ChoiceQueueItem queued, string key)
        {
            return queued?.ResolutionMetadata?.FirstOrDefault(item =>
                item != null && string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private static string StackTag(MechanicChoiceRequest request)
        {
            return request?.Options?.SelectMany(option => option?.Tags ?? Enumerable.Empty<string>())
                .FirstOrDefault(tag => tag != null && tag.StartsWith("stack:", StringComparison.OrdinalIgnoreCase))
                ?.Substring("stack:".Length);
        }
    }
}
