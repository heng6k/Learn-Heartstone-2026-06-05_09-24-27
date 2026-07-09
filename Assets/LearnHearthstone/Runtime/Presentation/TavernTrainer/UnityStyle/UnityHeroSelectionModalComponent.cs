using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityHeroSelectionModalComponent : MonoBehaviour
    {
        private enum ImplementationFilter
        {
            All,
            Implemented,
            Incomplete
        }

        private HeroCatalog heroCatalog;
        private string currentHeroCardId;
        private bool inMatch;
        private Action<HeroDefinition> onHeroSelected;
        private Action onClose;
        private string title;
        private string searchText = string.Empty;
        private ImplementationFilter implementationFilter = ImplementationFilter.All;
        private HeroPowerCategory? categoryFilter;
        private string previewHeroCardId;
        private bool useEnglish;
        private bool useLocalizedCardText;

        public static GameObject CreateModalHost(Transform parent, string fallbackName)
        {
            var modalObject = new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityHeroSelectionModalComponent));
            modalObject.transform.SetParent(parent, false);
            return modalObject;
        }

        public void Build(
            HeroCatalog catalog,
            string selectedHeroCardId,
            bool matchMode,
            Action<HeroDefinition> selected,
            Action close,
            string modalTitle = null,
            bool useEnglish = false,
            bool useLocalizedCardText = false)
        {
            heroCatalog = catalog;
            currentHeroCardId = selectedHeroCardId;
            inMatch = matchMode;
            onHeroSelected = selected;
            onClose = close;
            this.useEnglish = useEnglish;
            this.useLocalizedCardText = useLocalizedCardText;
            title = string.IsNullOrWhiteSpace(modalTitle) ? (matchMode ? T("更换英雄", "Change Hero") : T("选择英雄", "Choose Hero")) : modalTitle;

            if (string.IsNullOrEmpty(previewHeroCardId))
            {
                previewHeroCardId = ResolveInitialPreview()?.HeroCardId;
            }

            Rebuild();
        }

        private string T(string chinese, string english)
        {
            return useEnglish ? english : chinese;
        }

        private void Rebuild()
        {
            ClearChildren(transform);
            ConfigureOverlay(gameObject);

            var layout = UnityTavernLayoutContext.FromRoot(transform.parent);
            var panel = UiFactory.Panel("UnityHeroSelectionPanel", transform, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.ConfigureOutline(panel, new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.50f), new Vector2(2f, -2f));
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = layout.IsCompact ? new Vector2(0.035f, 0.045f) : new Vector2(0.08f, 0.10f);
            rect.anchorMax = layout.IsCompact ? new Vector2(0.965f, 0.955f) : new Vector2(0.92f, 0.90f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(layout.IsCompact ? 10 : 14, layout.IsCompact ? 10 : 14, 10, layout.IsCompact ? 10 : 14);
            panelLayout.spacing = layout.IsCompact ? 8 : 10;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            BuildHeader(panel.transform, layout);
            BuildFilters(panel.transform, layout);
            BuildBody(panel.transform, layout);
        }

        private void BuildHeader(Transform parent, UnityTavernLayoutContext layout)
        {
            var header = UiFactory.Panel("UnityHeroSelectionHeader", parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetPreferredHeight(header, layout.IsCompact ? 42f : 48f);
            var row = header.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(9, 7, 6, 6);
            row.spacing = 8;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var heading = UiFactory.Label("UnityHeroSelectionTitle", header.transform, title, layout.IsCompact ? 17 : 20, FontStyle.Bold);
            heading.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(heading.gameObject, 1f, 0f);

            var count = UiFactory.Label(
                "UnityHeroSelectionResultCount",
                header.transform,
                useEnglish ? FilteredHeroes().Count() + " heroes" : FilteredHeroes().Count() + " 个英雄",
                layout.IsCompact ? 11 : 12,
                FontStyle.Bold);
            count.alignment = TextAnchor.MiddleRight;
            count.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetFixedSize(count.gameObject, layout.IsCompact ? 78f : 106f, 30f);

            BuildSearchInput(header.transform, layout);

            var clearSearch = ModalButton("UnityHeroSelectionClearSearchButton", header.transform, T("清空", "Clear"), !string.IsNullOrWhiteSpace(searchText), () =>
            {
                searchText = string.Empty;
                ResetPreviewToFiltered();
            });
            UnityTavernUiStyle.SetFixedSize(clearSearch.gameObject, 52f, 32f);

            var close = ModalButton("UnityHeroSelectionCloseButton", header.transform, T("关闭", "Close"), true, () => onClose?.Invoke());
            UnityTavernUiStyle.SetFixedSize(close.gameObject, 72f, 32f);
        }

        private void BuildSearchInput(Transform parent, UnityTavernLayoutContext layout)
        {
            var inputObject = new GameObject("UnityHeroSelectionSearchInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(inputObject, layout.IsCompact ? 150f : 230f, 32f);
            UnityTavernUiStyle.ConfigureSurface(inputObject, UnityTavernUiStyle.PanelQuiet, true);
            UnityTavernUiStyle.ConfigureOutline(inputObject, new Color(0f, 0f, 0f, 0.24f), new Vector2(1f, -1f));

            var placeholder = UiFactory.Label("UnityHeroSelectionSearchPlaceholder", inputObject.transform, T("搜索英雄...", "Search heroes..."), 12);
            placeholder.color = UnityTavernUiStyle.MutedText;
            placeholder.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.Stretch(placeholder.rectTransform);
            placeholder.rectTransform.offsetMin = new Vector2(9f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-9f, 0f);

            var text = UiFactory.Label("UnityHeroSelectionSearchText", inputObject.transform, searchText, 12);
            text.color = UnityTavernUiStyle.Text;
            text.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(9f, 0f);
            text.rectTransform.offsetMax = new Vector2(-9f, 0f);

            var input = inputObject.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = searchText;
            input.lineType = InputField.LineType.SingleLine;
            input.onEndEdit.AddListener(value =>
            {
                searchText = value ?? string.Empty;
                previewHeroCardId = FilteredHeroes().FirstOrDefault()?.HeroCardId;
                Rebuild();
            });
        }

        private void BuildFilters(Transform parent, UnityTavernLayoutContext layout)
        {
            var row = UiFactory.Panel("UnityHeroSelectionFilters", parent, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(row, layout.IsCompact ? 82f : 38f);
            if (layout.IsCompact)
            {
                var grid = row.AddComponent<GridLayoutGroup>();
                grid.padding = new RectOffset(0, 0, 0, 0);
                grid.spacing = new Vector2(7f, 6f);
                grid.cellSize = new Vector2(78f, 34f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 6;
            }
            else
            {
                var group = row.AddComponent<HorizontalLayoutGroup>();
                group.padding = new RectOffset(0, 0, 0, 0);
                group.spacing = 7;
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandWidth = false;
                group.childForceExpandHeight = true;
            }

            FilterButton("UnityHeroSelectionStatusAllButton", row.transform, T("全部", "All"), implementationFilter == ImplementationFilter.All, () =>
            {
                implementationFilter = ImplementationFilter.All;
                ResetPreviewToFiltered();
            });
            FilterButton("UnityHeroSelectionImplementedButton", row.transform, T("已实现", "Implemented"), implementationFilter == ImplementationFilter.Implemented, () =>
            {
                implementationFilter = ImplementationFilter.Implemented;
                ResetPreviewToFiltered();
            });
            FilterButton("UnityHeroSelectionIncompleteButton", row.transform, T("未完成", "Incomplete"), implementationFilter == ImplementationFilter.Incomplete, () =>
            {
                implementationFilter = ImplementationFilter.Incomplete;
                ResetPreviewToFiltered();
            });

            foreach (var category in Enum.GetValues(typeof(HeroPowerCategory)).Cast<HeroPowerCategory>())
            {
                var captured = category;
                FilterButton(
                    "UnityHeroSelectionCategory" + captured + "Button",
                    row.transform,
                    CategoryName(captured),
                    categoryFilter == captured,
                    () =>
                    {
                        categoryFilter = categoryFilter == captured ? (HeroPowerCategory?)null : captured;
                        ResetPreviewToFiltered();
                    });
            }
        }

        private void BuildBody(Transform parent, UnityTavernLayoutContext layout)
        {
            var body = UiFactory.Panel("UnityHeroSelectionBody", parent, Color.clear);
            UnityTavernUiStyle.SetFlexible(body, 1f, 1f);
            if (layout.IsCompact)
            {
                var compactLayout = body.AddComponent<VerticalLayoutGroup>();
                compactLayout.spacing = 8;
                compactLayout.childControlWidth = true;
                compactLayout.childControlHeight = true;
                compactLayout.childForceExpandWidth = true;
                compactLayout.childForceExpandHeight = false;
                BuildPreview(body.transform, layout, true);
                BuildHeroList(body.transform, layout);
                return;
            }

            var bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 12;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;
            BuildPreview(body.transform, layout, false);
            BuildHeroList(body.transform, layout);
        }

        private void BuildPreview(Transform parent, UnityTavernLayoutContext layout, bool compact)
        {
            var hero = PreviewHero();
            var preview = UiFactory.Panel("UnityHeroSelectionPreview", parent, UnityTavernUiStyle.PanelQuiet);
            if (compact)
            {
                UnityTavernUiStyle.SetPreferredHeight(preview, 112f);
            }
            else
            {
                UnityTavernUiStyle.SetFixedSize(preview, 236f, 0f);
            }

            UnityTavernUiStyle.ConfigureOutline(preview, new Color(UnityTavernUiStyle.Blue.r, UnityTavernUiStyle.Blue.g, UnityTavernUiStyle.Blue.b, 0.26f), new Vector2(1f, -1f));
            var previewLayout = preview.AddComponent<VerticalLayoutGroup>();
            previewLayout.padding = new RectOffset(10, 10, 10, 10);
            previewLayout.spacing = compact ? 4 : 7;
            previewLayout.childControlWidth = true;
            previewLayout.childControlHeight = true;
            previewLayout.childForceExpandWidth = true;
            previewLayout.childForceExpandHeight = false;

            var top = UiFactory.Panel("UnityHeroSelectionPreviewTop", preview.transform, Color.clear);
            UnityTavernUiStyle.SetPreferredHeight(top, compact ? 54f : 80f);
            var topLayout = top.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 8;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = true;

            BuildHeroImage(top.transform, "UnityHeroSelectionPreviewImage", hero, compact ? 48f : 72f);

            var textStack = UiFactory.Panel("UnityHeroSelectionPreviewTextStack", top.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(textStack, 1f, 0f);
            var textLayout = textStack.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            var name = UiFactory.Label("UnityHeroSelectionPreviewName", textStack.transform, hero == null ? T("未设置英雄", "No hero set") : DisplayHeroName(hero), compact ? 14 : 16, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, compact ? 20f : 24f);

            var stats = UiFactory.Label("UnityHeroSelectionPreviewStats", textStack.transform, hero == null ? T("由对局兜底", "Match fallback") : T("生命 ", "Health ") + hero.Health + T(" / 护甲 ", " / Armor ") + hero.Armor, 12, FontStyle.Bold);
            stats.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(stats.gameObject, 18f);

            var power = hero?.HeroPower;
            AddPreviewLine(preview.transform, "UnityHeroSelectionPreviewPower", power == null ? T("技能：未设置", "Power: not set") : T("技能：", "Power: ") + DisplayHeroPowerName(power) + T("  费用 ", "  Cost ") + power.Cost, compact ? 12 : 13, UnityTavernUiStyle.Text);
            if (!compact)
            {
                AddPreviewLine(preview.transform, "UnityHeroSelectionPreviewText", string.IsNullOrWhiteSpace(DisplayHeroPowerText(power)) ? T("暂无技能描述。", "No hero power text.") : DisplayHeroPowerText(power), 11, UnityTavernUiStyle.MutedText, 54f);
                AddPreviewLine(preview.transform, "UnityHeroSelectionPreviewCategory", power == null ? T("分类：其他", "Category: Other") : T("分类：", "Category: ") + CategoryName(power.PrimaryCategory) + " / " + EligibilityName(power.ReplacementEligibility), 11, UnityTavernUiStyle.MutedText);
                AddPreviewLine(preview.transform, "UnityHeroSelectionPreviewStatus", StatusLabel(hero), 11, UnityTavernUiStyle.Gold);
            }

            var isCurrent = IsCurrentHero(hero);
            if (inMatch)
            {
                AddPreviewLine(
                    preview.transform,
                    "UnityHeroSelectionPreviewRisk",
                    isCurrent ? T("这是你当前正在使用的英雄。", "This is your current hero.") : T("确认后会刷新英雄、技能、生命和护甲。", "Confirming refreshes hero, power, health, and armor."),
                    11,
                    isCurrent ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Red,
                    compact ? 22f : 34f);
            }

            var confirm = ModalButton("UnityHeroSelectionConfirmButton", preview.transform, ConfirmButtonText(hero, isCurrent), hero != null && !isCurrent, () =>
            {
                if (hero != null)
                {
                    SelectHero(hero);
                }
            });
            UnityTavernUiStyle.SetPreferredHeight(confirm.gameObject, compact ? 32f : 38f);
        }

        private void BuildHeroList(Transform parent, UnityTavernLayoutContext layout)
        {
            var scrollContent = UiFactory.ScrollView("UnityHeroSelectionHeroScroll", parent, UnityTavernUiStyle.Panel, out _);
            var listLayout = scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(8, 8, 8, 8);
            listLayout.spacing = 7;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            var heroes = FilteredHeroes().ToList();
            if (heroes.Count == 0)
            {
                var empty = UiFactory.Label("UnityHeroSelectionEmpty", scrollContent, T("没有符合条件的英雄。", "No heroes match the filters."), 14, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.SetPreferredHeight(empty.gameObject, 56f);
                return;
            }

            foreach (var hero in heroes)
            {
                BuildHeroRow(scrollContent, hero, layout);
            }
        }

        private void BuildHeroRow(Transform parent, HeroDefinition hero, UnityTavernLayoutContext layout)
        {
            var rowObject = new GameObject("UnityHeroSelectionHeroButton-" + SafeName(hero.HeroCardId), typeof(RectTransform), typeof(Image), typeof(Button));
            rowObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetPreferredHeight(rowObject, layout.IsCompact ? 58f : 64f);
            var selected = string.Equals(hero.HeroCardId, previewHeroCardId, StringComparison.OrdinalIgnoreCase);
            var current = string.Equals(hero.HeroCardId, currentHeroCardId, StringComparison.OrdinalIgnoreCase);
            var surface = current || selected
                ? Color.Lerp(UnityTavernUiStyle.PanelRaised, current ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Blue, 0.32f)
                : UnityTavernUiStyle.PanelQuiet;
            UnityTavernUiStyle.ConfigureSurface(rowObject, surface, true);
            UnityTavernUiStyle.ConfigureOutline(rowObject, current ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.76f) : new Color(0f, 0f, 0f, 0.18f), new Vector2(1f, -1f));

            var button = rowObject.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                previewHeroCardId = hero.HeroCardId;
                Rebuild();
            });
            UnityTavernUiStyle.TintSelectable(button, surface, Color.Lerp(surface, UnityTavernUiStyle.Gold, 0.18f), Color.Lerp(surface, Color.black, 0.16f));

            var row = rowObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(7, 9, 6, 6);
            row.spacing = 8;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            BuildHeroImage(rowObject.transform, "UnityHeroSelectionHeroImage-" + SafeName(hero.HeroCardId), hero, layout.IsCompact ? 46f : 52f);

            var textStack = UiFactory.Panel("UnityHeroSelectionHeroText-" + SafeName(hero.HeroCardId), rowObject.transform, Color.clear);
            UnityTavernUiStyle.SetFlexible(textStack, 1f, 0f);
            var textLayout = textStack.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 1;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            var name = UiFactory.Label("UnityHeroSelectionHeroName-" + SafeName(hero.HeroCardId), textStack.transform, DisplayHeroName(hero), layout.IsCompact ? 13 : 14, FontStyle.Bold);
            name.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetPreferredHeight(name.gameObject, layout.IsCompact ? 20f : 24f);

            var detailText = (hero.HeroPower == null ? T("无技能", "No power") : DisplayHeroPowerName(hero.HeroPower) + T(" · 费用 ", " · Cost ") + hero.HeroPower.Cost) + " · " + (current ? T("当前英雄", "Current hero") : ShortStatusLabel(hero));
            var detail = UiFactory.Label("UnityHeroSelectionHeroDetail-" + SafeName(hero.HeroCardId), textStack.transform, detailText, 11, FontStyle.Bold);
            detail.color = current ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(detail.gameObject, layout.IsCompact ? 18f : 20f);

            var choose = ModalButton(
                "UnityHeroSelectionHeroChooseButton-" + SafeName(hero.HeroCardId),
                rowObject.transform,
                current ? T("当前", "Current") : inMatch ? T("预览", "Preview") : T("选择", "Choose"),
                !current,
                () =>
                {
                    if (inMatch)
                    {
                        previewHeroCardId = hero.HeroCardId;
                        Rebuild();
                    }
                    else
                    {
                        SelectHero(hero);
                    }
                });
            UnityTavernUiStyle.SetFixedSize(choose.gameObject, layout.IsCompact ? 58f : 66f, layout.IsCompact ? 34f : 38f);
        }

        private void BuildHeroImage(Transform parent, string name, HeroDefinition hero, float size)
        {
            var frame = UiFactory.Panel(name, parent, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.SetFixedSize(frame, size, size);
            UnityTavernUiStyle.ConfigureOutline(frame, new Color(0f, 0f, 0f, 0.25f), new Vector2(1f, -1f));

            var sprite = hero == null ? null : CardImageProvider.LoadSprite(hero.ImagePath, hero.HeroCardId, CardKind.Hero);
            if (sprite == null)
            {
                var missing = UiFactory.Label(name + "Missing", frame.transform, T("无图", "No art"), 10, FontStyle.Bold);
                missing.alignment = TextAnchor.MiddleCenter;
                missing.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.Stretch(missing.rectTransform);
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

        private IEnumerable<HeroDefinition> FilteredHeroes()
        {
            var heroes = heroCatalog == null ? Enumerable.Empty<HeroDefinition>() : heroCatalog.GetInitialSelectableHeroes();
            var query = (searchText ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(query))
            {
                heroes = heroes.Where(hero =>
                    Contains(hero.Name, query) ||
                    Contains(hero.ZhName, query) ||
                    Contains(hero.HeroCardId, query) ||
                    Contains(hero.HeroPower?.Name, query) ||
                    Contains(hero.HeroPower?.ZhName, query) ||
                    Contains(hero.HeroPower?.Text, query) ||
                    Contains(hero.HeroPower?.ZhText, query));
            }

            if (implementationFilter == ImplementationFilter.Implemented)
            {
                heroes = heroes.Where(hero => HeroEffectImplementationRegistry.FindByHeroCardId(hero.HeroCardId).Status == HeroEffectImplementationStatus.Implemented);
            }
            else if (implementationFilter == ImplementationFilter.Incomplete)
            {
                heroes = heroes.Where(hero => HeroEffectImplementationRegistry.FindByHeroCardId(hero.HeroCardId).Status != HeroEffectImplementationStatus.Implemented);
            }

            if (categoryFilter.HasValue)
            {
                heroes = heroes.Where(hero => hero.HeroPower != null && hero.HeroPower.PrimaryCategory == categoryFilter.Value);
            }

            return heroes.OrderBy(DisplayHeroName, StringComparer.OrdinalIgnoreCase);
        }

        private HeroDefinition PreviewHero()
        {
            if (heroCatalog == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(previewHeroCardId))
            {
                var preview = heroCatalog.AllHeroes.FirstOrDefault(hero => string.Equals(hero.HeroCardId, previewHeroCardId, StringComparison.OrdinalIgnoreCase));
                if (preview != null)
                {
                    return preview;
                }
            }

            return ResolveInitialPreview();
        }

        private HeroDefinition ResolveInitialPreview()
        {
            if (heroCatalog == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(currentHeroCardId))
            {
                var current = heroCatalog.AllHeroes.FirstOrDefault(hero => string.Equals(hero.HeroCardId, currentHeroCardId, StringComparison.OrdinalIgnoreCase));
                if (current != null)
                {
                    return current;
                }
            }

            return heroCatalog.AllHeroes.FirstOrDefault(hero => hero.Name == "Patchwerk")
                ?? heroCatalog.GetInitialSelectableHeroes().FirstOrDefault();
        }

        private void ResetPreviewToFiltered()
        {
            previewHeroCardId = FilteredHeroes().FirstOrDefault()?.HeroCardId;
            Rebuild();
        }

        private void SelectHero(HeroDefinition hero)
        {
            if (hero == null || IsCurrentHero(hero))
            {
                return;
            }

            onHeroSelected?.Invoke(hero);
        }

        private bool IsCurrentHero(HeroDefinition hero)
        {
            return hero != null &&
                !string.IsNullOrEmpty(currentHeroCardId) &&
                string.Equals(hero.HeroCardId, currentHeroCardId, StringComparison.OrdinalIgnoreCase);
        }

        private string ConfirmButtonText(HeroDefinition hero, bool isCurrent)
        {
            if (hero == null)
            {
                return T("没有可选英雄", "No selectable hero");
            }

            if (isCurrent)
            {
                return T("当前英雄", "Current hero");
            }

            return inMatch ? T("确认更换：", "Confirm: ") + DisplayHeroName(hero) : T("选择此英雄", "Choose this hero");
        }

        private string DisplayHeroName(HeroDefinition hero)
        {
            if (useLocalizedCardText && !string.IsNullOrEmpty(hero?.ZhName))
            {
                return hero.ZhName;
            }

            return hero?.Name ?? string.Empty;
        }

        private string DisplayHeroPowerName(HeroPowerDefinition power)
        {
            if (useLocalizedCardText && !string.IsNullOrEmpty(power?.ZhName))
            {
                return power.ZhName;
            }

            return power?.Name ?? string.Empty;
        }

        private string DisplayHeroPowerText(HeroPowerDefinition power)
        {
            if (useLocalizedCardText && !string.IsNullOrEmpty(power?.ZhText))
            {
                return power.ZhText;
            }

            return power?.Text ?? string.Empty;
        }

        private void AddPreviewLine(Transform parent, string name, string text, int size, Color color, float height = 22f)
        {
            var label = UiFactory.Label(name, parent, text, size, FontStyle.Bold);
            label.color = color;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, height);
        }

        private Button ModalButton(string name, Transform parent, string text, bool interactable, Action onClick)
        {
            var button = UiFactory.Button(name, parent, text, () =>
            {
                if (interactable)
                {
                    onClick?.Invoke();
                }
            });
            button.interactable = interactable;
            var normal = interactable ? UnityTavernUiStyle.PanelRaised : UnityTavernUiStyle.PanelQuiet;
            UnityTavernUiStyle.ConfigureSurface(button.gameObject, normal, true);
            UnityTavernUiStyle.TintSelectable(button, normal, Color.Lerp(normal, UnityTavernUiStyle.Gold, 0.22f), Color.Lerp(normal, Color.black, 0.18f));
            return button;
        }

        private void FilterButton(string name, Transform parent, string text, bool active, Action onClick)
        {
            var button = ModalButton(name, parent, text, true, onClick);
            UnityTavernUiStyle.SetFixedSize(button.gameObject, 78f, 32f);
            var color = active ? Color.Lerp(UnityTavernUiStyle.PanelRaised, UnityTavernUiStyle.Gold, 0.34f) : UnityTavernUiStyle.Panel;
            UnityTavernUiStyle.ConfigureSurface(button.gameObject, color, true);
            UnityTavernUiStyle.ConfigureOutline(button.gameObject, active ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.72f) : new Color(0f, 0f, 0f, 0.20f), new Vector2(1f, -1f));
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 11;
                label.color = active ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Text;
            }
        }

        private string StatusLabel(HeroDefinition hero)
        {
            if (hero == null)
            {
                return T("实现状态：未注册", "Status: Unregistered");
            }

            var implementation = HeroEffectImplementationRegistry.FindByHeroCardId(hero.HeroCardId);
            return T("实现状态：", "Status: ") + ShortStatusLabel(hero) + " / " + implementation.Phase;
        }

        private string ShortStatusLabel(HeroDefinition hero)
        {
            var status = HeroEffectImplementationRegistry.FindByHeroCardId(hero?.HeroCardId).Status;
            switch (status)
            {
                case HeroEffectImplementationStatus.Implemented: return T("已实现", "Implemented");
                case HeroEffectImplementationStatus.FrameworkFirst: return T("代理", "Proxy");
                case HeroEffectImplementationStatus.Deferred:
                case HeroEffectImplementationStatus.Unregistered: return T("禁用", "Disabled");
                default: return T("未完成", "Incomplete");
            }
        }

        private string CategoryName(HeroPowerCategory category)
        {
            switch (category)
            {
                case HeroPowerCategory.Economy: return T("经济", "Economy");
                case HeroPowerCategory.Buff: return T("增益", "Buff");
                case HeroPowerCategory.Combat: return T("战斗", "Combat");
                case HeroPowerCategory.Minion: return T("随从", "Minion");
                case HeroPowerCategory.Discover: return T("发现", "Discover");
                case HeroPowerCategory.Health: return T("生命", "Health");
                case HeroPowerCategory.Passive: return T("被动", "Passive");
                case HeroPowerCategory.HeroSwap: return T("换技能", "Swap Power");
                default: return T("其他", "Other");
            }
        }

        private string EligibilityName(HeroPowerReplacementEligibility eligibility)
        {
            switch (eligibility)
            {
                case HeroPowerReplacementEligibility.DiscoverableAfterStart: return T("可替换", "Discoverable");
                case HeroPowerReplacementEligibility.InitialOnly: return T("开局限定", "Opening only");
                case HeroPowerReplacementEligibility.NonSelectable: return T("不可选", "Not selectable");
                default: return T("禁用", "Disabled");
            }
        }

        private static bool Contains(string source, string query)
        {
            return !string.IsNullOrEmpty(source)
                && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string SafeName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Unknown";
            }

            return value.Replace(' ', '_').Replace('/', '_').Replace('\\', '_').Replace(':', '_');
        }

        private static void ConfigureOverlay(GameObject target)
        {
            target.name = "UnityHeroSelectionOverlay";
            UnityTavernUiStyle.Stretch(target.GetComponent<RectTransform>());
            var image = UnityTavernUiStyle.EnsureComponent<Image>(target);
            image.color = new Color(0f, 0f, 0f, 0.60f);
            image.raycastTarget = true;
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
    }
}
