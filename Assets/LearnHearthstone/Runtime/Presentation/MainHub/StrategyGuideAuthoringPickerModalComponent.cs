using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.MainHub
{
    public sealed class StrategyGuideAuthoringPickerItem
    {
        public string Id;
        public string Name;
        public string Detail;
        public string Group;
        public string ImagePath;
        public CardKind CardKind;
    }

    public sealed class StrategyGuideAuthoringPickerModalComponent : MonoBehaviour
    {
        private IReadOnlyList<StrategyGuideAuthoringPickerItem> items;
        private Action<StrategyGuideAuthoringPickerItem> selected;
        private Action close;
        private string title;
        private string help;
        private string currentId;
        private string searchText = string.Empty;
        private bool useEnglish;
        private UnityTavernLayoutContext? layoutContext;

        public static GameObject CreateModalHost(Transform parent)
        {
            var host = new GameObject(
                "StrategyGuideAuthoringPickerOverlay",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(StrategyGuideAuthoringPickerModalComponent));
            host.transform.SetParent(parent, false);
            host.GetComponent<LayoutElement>().ignoreLayout = true;
            UiFactory.Stretch(host.GetComponent<RectTransform>());
            host.transform.SetAsLastSibling();
            return host;
        }

        public void Build(
            IEnumerable<StrategyGuideAuthoringPickerItem> source,
            string selectedId,
            string modalTitle,
            string helpText,
            Action<StrategyGuideAuthoringPickerItem> onSelected,
            Action onClose,
            bool english = false,
            UnityTavernLayoutContext? requestedLayout = null)
        {
            items = (source ?? Enumerable.Empty<StrategyGuideAuthoringPickerItem>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(item => item.Group ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Name ?? item.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            currentId = selectedId;
            title = modalTitle ?? string.Empty;
            help = helpText ?? string.Empty;
            selected = onSelected;
            close = onClose;
            useEnglish = english;
            layoutContext = requestedLayout;
            Rebuild();
        }

        private void Rebuild()
        {
            ClearChildren(transform);
            ConfigureOverlay();
            var layout = layoutContext ?? UnityTavernLayoutContext.FromRoot(transform.parent);
            var panel = UiFactory.Panel(
                "StrategyGuideAuthoringPickerPanel",
                transform,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.99f));
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = layout.IsCompact ? new Vector2(0.035f, 0.04f) : new Vector2(0.12f, 0.09f);
            rect.anchorMax = layout.IsCompact ? new Vector2(0.965f, 0.96f) : new Vector2(0.88f, 0.91f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            UiFactory.Vertical(panel, layout.IsCompact ? 9 : 14, layout.IsCompact ? 8 : 10);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.72f),
                new Vector2(2f, -2f));
            UnityTavernUiStyle.AddStarLanternRail(
                panel.transform,
                "StrategyGuideAuthoringPickerStarLantern",
                UnityTavernUiStyle.ArcaneBlue);

            BuildHeader(panel.transform, layout);
            BuildHelp(panel.transform, layout);
            BuildResults(panel.transform, layout);
        }

        private void BuildHeader(Transform parent, UnityTavernLayoutContext layout)
        {
            var header = UiFactory.Panel("StrategyGuideAuthoringPickerHeader", parent, Color.clear);
            UiFactory.SetHeight(header, layout.IsCompact ? 106f : 54f);
            var row = UiFactory.Horizontal(header, 0, 8);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var heading = UiFactory.Label(
                "StrategyGuideAuthoringPickerTitle",
                header.transform,
                title,
                layout.IsCompact ? 18 : 21,
                FontStyle.Bold,
                layout);
            heading.color = UnityTavernUiStyle.Gold;
            UiFactory.SetFlexible(heading.gameObject, 1f, 0f);

            var count = UiFactory.Label(
                "StrategyGuideAuthoringPickerResultCount",
                header.transform,
                FilteredItems().Count() + T(" 项", " items"),
                14,
                FontStyle.Bold,
                layout);
            count.color = UnityTavernUiStyle.TextMuted;
            count.alignment = TextAnchor.MiddleRight;
            UiFactory.SetWidth(count.gameObject, 84f);

            var inputObject = new GameObject(
                "StrategyGuideAuthoringPickerSearchInput",
                typeof(RectTransform),
                typeof(Image),
                typeof(InputField));
            inputObject.transform.SetParent(header.transform, false);
            UiFactory.SetMinSize(inputObject, 150f, UnityTavernUiStyle.TouchHeight);
            UiFactory.SetWidth(inputObject, layout.IsCompact ? 156f : 230f);
            var inputLayout = inputObject.GetComponent<LayoutElement>();
            var inputHeight = layout.CanvasUnitsForPhysicalPixels(UnityTavernUiStyle.TouchHeight);
            inputLayout.minHeight = inputHeight;
            inputLayout.preferredHeight = inputHeight;
            inputLayout.flexibleHeight = 0f;
            inputLayout.layoutPriority = 2;
            var input = inputObject.GetComponent<InputField>();
            var placeholder = UiFactory.Label(
                "StrategyGuideAuthoringPickerSearchPlaceholder",
                inputObject.transform,
                T("搜索名称、文本或编号", "Search name, text, or id"),
                14,
                FontStyle.Normal,
                layout);
            placeholder.color = UnityTavernUiStyle.TextMuted;
            placeholder.alignment = TextAnchor.MiddleLeft;
            UiFactory.Stretch(placeholder.rectTransform);
            placeholder.rectTransform.offsetMin = new Vector2(10f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-10f, 0f);
            var text = UiFactory.Label(
                "StrategyGuideAuthoringPickerSearchText",
                inputObject.transform,
                searchText,
                14,
                FontStyle.Normal,
                layout);
            text.color = UnityTavernUiStyle.TextLight;
            text.alignment = TextAnchor.MiddleLeft;
            UiFactory.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(10f, 0f);
            text.rectTransform.offsetMax = new Vector2(-10f, 0f);
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = searchText;
            input.lineType = InputField.LineType.SingleLine;
            input.onEndEdit.AddListener(value =>
            {
                searchText = (value ?? string.Empty).Trim();
                Rebuild();
            });
            inputObject.AddComponent<UnitySelectableFocusRing>();
            UnityTavernUiStyle.ConfigureInputField(input, UnityTavernUiStyle.ArcaneBlue);

            var closeButton = UiFactory.Button(
                "StrategyGuideAuthoringPickerCloseButton",
                header.transform,
                T("关闭", "Close"),
                () => close?.Invoke(),
                layout);
            UnityTavernUiStyle.ConfigureButton(closeButton, UnityTavernUiStyle.ArcaneBlue);
            UiFactory.SetWidth(closeButton.gameObject, layout.IsCompact ? 72f : 88f);
        }

        private void BuildHelp(Transform parent, UnityTavernLayoutContext layout)
        {
            var label = UiFactory.Label(
                "StrategyGuideAuthoringPickerHelp",
                parent,
                help,
                14,
                FontStyle.Normal,
                layout);
            label.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(label.gameObject, layout.IsCompact ? 48f : 34f);
        }

        private void BuildResults(Transform parent, UnityTavernLayoutContext layout)
        {
            var content = UiFactory.ScrollView(
                "StrategyGuideAuthoringPickerScroll",
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.96f),
                out _,
                layout);
            var list = UiFactory.Vertical(content.gameObject, 8, 8);
            list.childControlWidth = true;
            list.childForceExpandWidth = true;
            var filtered = FilteredItems().ToList();
            if (filtered.Count == 0)
            {
                var empty = UiFactory.Label(
                    "StrategyGuideAuthoringPickerEmpty",
                    content,
                    T("没有匹配项目。请缩短关键词，或关闭后检查当前版本与筛选条件。", "No matches. Shorten the query or check version filters."),
                    14,
                    FontStyle.Bold,
                    layout);
                empty.color = UnityTavernUiStyle.TextMuted;
                empty.alignment = TextAnchor.MiddleCenter;
                UiFactory.SetHeight(empty.gameObject, 72f);
                return;
            }

            foreach (var item in filtered)
            {
                BuildItem(content, item, layout);
            }
        }

        private void BuildItem(Transform parent, StrategyGuideAuthoringPickerItem item, UnityTavernLayoutContext layout)
        {
            var safeId = SafeName(item.Id);
            var current = string.Equals(item.Id, currentId, StringComparison.OrdinalIgnoreCase);
            var rowObject = UiFactory.Panel(
                "StrategyGuideAuthoringPickerItem-" + safeId,
                parent,
                current
                    ? Color.Lerp(UnityTavernUiStyle.SurfaceRaised, UnityTavernUiStyle.Gold, 0.19f)
                    : UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.96f));
            UiFactory.SetHeight(rowObject, layout.IsCompact ? 76f : 84f);
            var row = UiFactory.Horizontal(rowObject, 8, 9);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandWidth = false;
            UnityTavernUiStyle.ConfigureOutline(
                rowObject,
                current
                    ? UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Gold, 0.72f)
                    : UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.ArcaneBlue, 0.28f),
                new Vector2(1f, -1f));

            BuildImage(rowObject.transform, item, layout.IsCompact ? 58f : 66f, layout);
            var copy = UiFactory.Panel("StrategyGuideAuthoringPickerCopy-" + safeId, rowObject.transform, Color.clear);
            UiFactory.SetFlexible(copy, 1f, 0f);
            UiFactory.Vertical(copy, 0, 2);
            var name = UiFactory.Label(
                "StrategyGuideAuthoringPickerName-" + safeId,
                copy.transform,
                item.Name ?? item.Id,
                15,
                FontStyle.Bold,
                layout);
            name.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetHeight(name.gameObject, 26f);
            var detail = UiFactory.Label(
                "StrategyGuideAuthoringPickerDetail-" + safeId,
                copy.transform,
                string.IsNullOrWhiteSpace(item.Detail) ? item.Id : item.Detail,
                14,
                FontStyle.Normal,
                layout);
            detail.color = UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(detail.gameObject, layout.IsCompact ? 34f : 38f);

            var choose = UiFactory.Button(
                "StrategyGuideAuthoringPickerChooseButton-" + safeId,
                rowObject.transform,
                current ? T("当前", "Current") : T("选择", "Choose"),
                () =>
                {
                    if (!current)
                    {
                        selected?.Invoke(item);
                    }
                },
                layout);
            choose.interactable = !current;
            UnityTavernUiStyle.ConfigureButton(
                choose,
                current ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Brass,
                current,
                current);
            UiFactory.SetWidth(choose.gameObject, layout.IsCompact ? 74f : 92f);
        }

        private static void BuildImage(
            Transform parent,
            StrategyGuideAuthoringPickerItem item,
            float size,
            UnityTavernLayoutContext layout)
        {
            var frame = UiFactory.Panel(
                "StrategyGuideAuthoringPickerImage-" + SafeName(item.Id),
                parent,
                UnityTavernUiStyle.SurfaceDark);
            UiFactory.SetMinSize(frame, size, size);
            UiFactory.SetWidth(frame, size);
            var sprite = CardImageProvider.LoadSprite(item.ImagePath, item.Id, item.CardKind);
            if (sprite == null)
            {
                var fallback = UiFactory.Label(
                    "StrategyGuideAuthoringPickerImageFallback",
                    frame.transform,
                    item.CardKind == CardKind.Hero ? "H" : item.CardKind == CardKind.Trinket ? "T" : "C",
                    18,
                    FontStyle.Bold,
                    layout);
                fallback.alignment = TextAnchor.MiddleCenter;
                fallback.color = UnityTavernUiStyle.Gold;
                UiFactory.Stretch(fallback.rectTransform);
                return;
            }
            var imageObject = new GameObject("Art", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(frame.transform, false);
            UiFactory.Stretch(imageObject.GetComponent<RectTransform>());
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private IEnumerable<StrategyGuideAuthoringPickerItem> FilteredItems()
        {
            var query = (searchText ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(query))
            {
                return items ?? Array.Empty<StrategyGuideAuthoringPickerItem>();
            }
            return (items ?? Array.Empty<StrategyGuideAuthoringPickerItem>()).Where(item =>
                Contains(item.Id, query) ||
                Contains(item.Name, query) ||
                Contains(item.Detail, query) ||
                Contains(item.Group, query));
        }

        private void ConfigureOverlay()
        {
            UiFactory.Stretch(GetComponent<RectTransform>());
            var image = GetComponent<Image>();
            image.color = UnityTavernUiStyle.WithAlpha(Color.black, 0.74f);
            image.raycastTarget = true;
        }

        private string T(string chinese, string english)
        {
            return useEnglish ? english : chinese;
        }

        private static bool Contains(string value, string query)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string SafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Unknown";
            }
            return new string(value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());
        }

        private static void ClearChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index -= 1)
            {
                var child = parent.GetChild(index).gameObject;
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }
    }
}
