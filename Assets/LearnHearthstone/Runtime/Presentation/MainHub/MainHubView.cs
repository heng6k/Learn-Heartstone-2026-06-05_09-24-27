using System;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.MainHub
{
    public sealed class MainHubView
    {
        private const string AtmosphereResource = "UI/Combat/BattlegroundsBattlefieldAtmosphere-v2";

        private readonly Transform root;
        private readonly Action openTrainer;
        private readonly Action openUnityTrainer;
        private readonly UnityTavernLayoutContext layout;
        private readonly bool useEnglish;
        private readonly Action<bool> languageChanged;
        private readonly GameVersionSummaryViewModel currentGameVersion;
        private readonly Action openVersionCenter;
        private readonly Action openStrategyGuides;

        public MainHubView(
            Transform root,
            Action openTrainer,
            Action openRealisticTrainer,
            Action openUnityTrainer = null,
            UnityTavernLayoutContext? layoutContext = null,
            bool useEnglish = false,
            Action<bool> languageChanged = null,
            GameVersionSummaryViewModel currentGameVersion = null,
            Action openVersionCenter = null,
            Action openStrategyGuides = null)
        {
            this.root = root;
            this.openTrainer = openTrainer;
            this.openUnityTrainer = openUnityTrainer;
            layout = layoutContext ?? UnityTavernLayoutContext.FromRoot(root);
            this.useEnglish = useEnglish;
            this.languageChanged = languageChanged;
            this.currentGameVersion = currentGameVersion;
            this.openVersionCenter = openVersionCenter;
            this.openStrategyGuides = openStrategyGuides;
            _ = openRealisticTrainer;
        }

        public void Build()
        {
            var shell = UiFactory.Panel("MainHub", root, UnityTavernUiStyle.BackWall);
            UiFactory.Stretch(shell.GetComponent<RectTransform>());
            UiFactory.Vertical(
                shell,
                CompactInt(layout.IsCompact ? 12f : 24f),
                CompactInt(layout.IsCompact ? 10f : 16f));

            BuildAtmosphere(shell.transform);
            BuildHeader(shell.transform);
            BuildEntryDeck(shell.transform);
            BuildVersionContext(shell.transform);
        }

        private static void BuildAtmosphere(Transform parent)
        {
            var backdrop = new GameObject(
                "MainHubAtmosphere",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            backdrop.transform.SetParent(parent, false);
            backdrop.GetComponent<LayoutElement>().ignoreLayout = true;
            UiFactory.Stretch(backdrop.GetComponent<RectTransform>());
            var image = backdrop.GetComponent<Image>();
            image.sprite = Resources.Load<Sprite>(AtmosphereResource);
            image.color = UnityTavernUiStyle.WithAlpha(Color.white, 0.58f);
            image.preserveAspect = false;
            image.raycastTarget = false;
            backdrop.transform.SetAsFirstSibling();

            var veil = UiFactory.Panel(
                "MainHubAtmosphereVeil",
                backdrop.transform,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.BackWall, 0.52f));
            UiFactory.Stretch(veil.GetComponent<RectTransform>());
            veil.GetComponent<Image>().raycastTarget = false;
        }

        private void BuildHeader(Transform parent)
        {
            var header = UiFactory.Panel(
                "MainHubHeader",
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.96f));
            UiFactory.SetHeight(
                header,
                layout.CanvasUnitsForPhysicalPixels(layout.IsCompact ? 64f : 76f));
            UiFactory.SetFlexible(header, 0f, 0f);
            UnityTavernUiStyle.ConfigureOutline(
                header,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.46f),
                new Vector2(1f, -1f));
            UnityTavernUiStyle.AddStarLanternRail(
                header.transform,
                "MainHubStarLantern",
                UnityTavernUiStyle.ArcaneBlue);

            var headerLayout = UiFactory.Horizontal(
                header,
                CompactInt(8f),
                CompactInt(layout.IsCompact ? 8f : 14f));
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            var titleStack = UiFactory.Panel("MainHubTitleStack", header.transform, Color.clear);
            UiFactory.SetFlexible(titleStack, 1f, 0f);
            UiFactory.Vertical(titleStack, 0, CompactInt(2f)).childAlignment = TextAnchor.MiddleLeft;

            var title = UiFactory.Label(
                "Title",
                titleStack.transform,
                "Learn Heartstone",
                CompactFont(28f, 36),
                FontStyle.Bold,
                layout);
            title.color = UnityTavernUiStyle.TextLight;
            UiFactory.SetHeight(title.gameObject, CompactUnits(layout.IsCompact ? 32f : 36f));

            if (!layout.IsCompact)
            {
                var subtitle = UiFactory.Label(
                    "MainHubSubtitle",
                    titleStack.transform,
                    T("酒馆战棋 · 交互训练工坊", "Battlegrounds interactive training lab"),
                    14,
                    FontStyle.Bold,
                    layout);
                subtitle.color = UnityTavernUiStyle.TextMuted;
                UiFactory.SetHeight(subtitle.gameObject, 18f);
            }

            BuildLanguageSwitch(header.transform);
        }

        private void BuildLanguageSwitch(Transform parent)
        {
            var switcher = UiFactory.Panel(
                "MainHubLanguageSwitch",
                parent,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceRaised, 0.82f));
            UnityTavernUiStyle.SetFixedSize(
                switcher,
                CompactUnits(layout.IsCompact ? 226f : 250f),
                layout.CanvasUnitsForPhysicalPixels(48f));
            var row = UiFactory.Horizontal(
                switcher,
                CompactInt(4f),
                CompactInt(6f));
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            BuildLanguageButton(
                "MainHubLanguageChineseButton",
                switcher.transform,
                useEnglish ? "中文" : "中文 · 当前",
                !useEnglish,
                UnityTavernUiStyle.Gold,
                () => RequestLanguage(false));
            BuildLanguageButton(
                "MainHubLanguageEnglishButton",
                switcher.transform,
                useEnglish ? "English · Current" : "English",
                useEnglish,
                UnityTavernUiStyle.ArcaneBlue,
                () => RequestLanguage(true));
        }

        private void BuildLanguageButton(
            string name,
            Transform parent,
            string text,
            bool active,
            Color accent,
            Action onClick)
        {
            var button = UiFactory.Button(name, parent, text, () => onClick?.Invoke(), layout);
            UiFactory.SetFlexible(button.gameObject, 1f, 1f);
            UnityTavernUiStyle.ConfigureButton(button, accent, active, active);
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = CompactFont(14f, 14);
                label.resizeTextMaxSize = label.fontSize;
            }
        }

        private void BuildEntryDeck(Transform parent)
        {
            var deck = UiFactory.Panel("MainHubEntryDeck", parent, Color.clear);
            UiFactory.SetFlexible(deck, 1f, 1f);
            var portrait = layout.Height > layout.Width;
            HorizontalOrVerticalLayoutGroup deckLayout = portrait
                ? (HorizontalOrVerticalLayoutGroup)UiFactory.Vertical(deck, 0, CompactInt(12f))
                : UiFactory.Horizontal(deck, 0, CompactInt(layout.IsCompact ? 12f : 18f));
            deckLayout.childControlWidth = true;
            deckLayout.childControlHeight = true;
            deckLayout.childForceExpandWidth = true;
            deckLayout.childForceExpandHeight = true;

            BuildEntryCard(
                deck.transform,
                "MainHubPrimaryStartButton",
                "MainHubTrainingEntryContent",
                T("模拟对局", "Tavern Simulator"),
                layout.IsCompact
                    ? T("购买、打出、移位、战斗", "Buy · Play · Reposition · Fight")
                    : T("像真实酒馆一样购买、打出、移位并测试战斗", "Buy, play, reposition, and test combat like the real Tavern"),
                T("进入模拟", "Start Simulation"),
                UnityTavernUiStyle.Gold,
                openUnityTrainer ?? openTrainer,
                true);

            BuildEntryCard(
                deck.transform,
                "MainHubStrategyGuideButton",
                "MainHubGuideEntryContent",
                T("一图流训练", "One-Page Training"),
                layout.IsCompact
                    ? T("创建、查看、跟练阵容路线", "Create · Review · Practice Routes")
                    : T("创建、查看并按初级或困难路线练习阵容", "Create, review, and practice beginner or hard lineup routes"),
                openStrategyGuides != null
                    ? T("进入一图流", "Open One-Page Training")
                    : T("暂不可用", "Unavailable"),
                UnityTavernUiStyle.ArcaneBlue,
                openStrategyGuides,
                false);
        }

        private void BuildEntryCard(
            Transform parent,
            string buttonName,
            string contentName,
            string titleText,
            string descriptionText,
            string actionText,
            Color accent,
            Action action,
            bool emphasized)
        {
            var button = UiFactory.Button(buttonName, parent, string.Empty, () => action?.Invoke(), layout);
            button.interactable = action != null;
            UiFactory.SetFlexible(button.gameObject, 1f, 1f);
            UnityTavernUiStyle.ConfigureButton(button, accent, emphasized, false);
            var buttonSurface = button.GetComponent<Image>();
            var resting = UnityTavernUiStyle.WithAlpha(
                emphasized
                    ? Color.Lerp(UnityTavernUiStyle.SurfaceRaised, accent, 0.24f)
                    : Color.Lerp(UnityTavernUiStyle.SurfaceDark, UnityTavernUiStyle.SurfaceRaised, 0.72f),
                0.92f);
            UnityTavernUiStyle.TintSelectable(
                button,
                resting,
                Color.Lerp(resting, accent, 0.24f),
                Color.Lerp(resting, Color.black, 0.16f));
            buttonSurface.color = resting;
            var outerOutline = UnityTavernUiStyle.ConfigureOutline(
                button.gameObject,
                UnityTavernUiStyle.WithAlpha(accent, emphasized ? 0.76f : 0.54f),
                new Vector2(emphasized ? 2f : 1f, emphasized ? -2f : -1f));
            outerOutline.enabled = !layout.IsCompact;
            UnityTavernUiStyle.AddStarLanternRail(
                button.transform,
                buttonName + "StarLantern",
                accent);

            var defaultLabel = button.GetComponentInChildren<Text>();
            if (defaultLabel != null)
            {
                defaultLabel.gameObject.SetActive(false);
            }

            BuildEntryArtwork(button.transform, buttonName, accent, emphasized);

            var content = UiFactory.Panel(
                contentName,
                button.transform,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, layout.IsCompact ? 0.72f : 0.86f));
            content.GetComponent<Image>().raycastTarget = false;
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.035f, 0.07f);
            contentRect.anchorMax = new Vector2(layout.IsCompact ? 0.58f : 0.64f, 0.93f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var contentLayout = UiFactory.Vertical(
                content,
                CompactInt(layout.IsCompact ? 10f : 18f),
                CompactInt(layout.IsCompact ? 6f : 8f));
            contentLayout.childAlignment = TextAnchor.MiddleLeft;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandHeight = false;

            var badge = UiFactory.Label(
                buttonName + "ModeBadge",
                content.transform,
                emphasized
                    ? T("自由沙盒 · 真实操作", "FREE PLAY · REAL INTERACTIONS")
                    : T("路线学习 · 阵容复现", "GUIDED ROUTES · LINEUP PRACTICE"),
                14,
                FontStyle.Bold,
                layout);
            badge.color = button.interactable ? accent : UnityTavernUiStyle.TextMuted;
            UiFactory.SetHeight(badge.gameObject, CompactUnits(layout.IsCompact ? 20f : 24f));

            var title = UiFactory.Label(
                buttonName + "Title",
                content.transform,
                titleText,
                CompactFont(26f, 34),
                FontStyle.Bold,
                layout);
            title.color = UnityTavernUiStyle.TextLight;
            title.alignment = TextAnchor.MiddleLeft;
            UiFactory.SetHeight(title.gameObject, CompactUnits(layout.IsCompact ? 34f : 46f));

            var description = UiFactory.Label(
                buttonName + "Description",
                content.transform,
                descriptionText,
                CompactFont(16f, 18),
                FontStyle.Normal,
                layout);
            description.color = UnityTavernUiStyle.TextMuted;
            description.alignment = TextAnchor.UpperLeft;
            UiFactory.SetHeight(description.gameObject, CompactUnits(layout.IsCompact ? 40f : 58f));

            var spacer = UiFactory.Panel(buttonName + "Spacer", content.transform, Color.clear);
            UiFactory.SetFlexible(spacer, 1f, 1f);

            var actionPlate = UiFactory.Panel(
                buttonName + "ActionPlate",
                content.transform,
                UnityTavernUiStyle.WithAlpha(
                    button.interactable ? accent : UnityTavernUiStyle.SurfaceRaised,
                    0.28f));
            if (button.interactable)
            {
                UnityTavernUiStyle.ApplyTavernActionSkin(actionPlate, !emphasized);
            }
            actionPlate.GetComponent<Image>().raycastTarget = false;
            var actionHeight = Mathf.Max(
                UiFactory.MinimumButtonHeight,
                layout.CanvasUnitsForPhysicalPixels(UiFactory.MinimumButtonHeight));
            UiFactory.SetMinSize(actionPlate, 0f, actionHeight);
            UiFactory.SetHeight(actionPlate, actionHeight);
            var actionOutline = UnityTavernUiStyle.ConfigureOutline(
                actionPlate,
                UnityTavernUiStyle.WithAlpha(
                    button.interactable ? accent : UnityTavernUiStyle.TextMuted,
                    0.62f),
                new Vector2(1f, -1f));
            actionOutline.enabled = !layout.IsCompact || !button.interactable;

            var actionLabel = UiFactory.Label(
                buttonName + "ActionLabel",
                actionPlate.transform,
                actionText + "   →",
                CompactFont(16f, 18),
                FontStyle.Bold,
                layout);
            actionLabel.color = button.interactable
                ? emphasized ? UnityTavernUiStyle.TextDark : UnityTavernUiStyle.TextLight
                : UnityTavernUiStyle.TextMuted;
            actionLabel.alignment = TextAnchor.MiddleCenter;
            actionLabel.raycastTarget = false;
            UiFactory.Stretch(actionLabel.rectTransform);
            actionLabel.rectTransform.offsetMin = new Vector2(8f, 0f);
            actionLabel.rectTransform.offsetMax = new Vector2(-8f, 0f);

            if (layout.IsCompact)
            {
                UnityTavernUiStyle.AddTavernPanelFrame(button.transform, buttonName + "TavernFrame");
            }

            var focusRing = button.GetComponent<UnitySelectableFocusRing>();
            if (focusRing != null)
            {
                focusRing.FocusOutline.transform.parent.SetAsLastSibling();
            }
        }

        private void BuildEntryArtwork(Transform parent, string prefix, Color accent, bool trainingMode)
        {
            var artwork = UiFactory.Panel(prefix + "Artwork", parent, Color.clear);
            artwork.GetComponent<Image>().raycastTarget = false;
            var artworkRect = artwork.GetComponent<RectTransform>();
            artworkRect.anchorMin = new Vector2(layout.IsCompact ? 0.55f : 0.48f, 0.04f);
            artworkRect.anchorMax = new Vector2(0.98f, 0.97f);
            artworkRect.offsetMin = Vector2.zero;
            artworkRect.offsetMax = Vector2.zero;

            var glow = UiFactory.Panel(
                prefix + "ArtworkGlow",
                artwork.transform,
                UnityTavernUiStyle.WithAlpha(accent, 0.10f));
            UiFactory.Stretch(glow.GetComponent<RectTransform>());
            glow.GetComponent<Image>().raycastTarget = false;

            if (trainingMode)
            {
                BuildCardArt(artwork.transform, prefix, 0, "BGS_041", CardKind.Minion, -8f, 0.00f, 0.16f, 0.72f, 0.97f);
                BuildCardArt(artwork.transform, prefix, 1, "BG32_822", CardKind.Minion, 7f, 0.28f, 0.05f, 1.00f, 0.92f);
            }
            else
            {
                BuildCardArt(artwork.transform, prefix, 0, "BG33_825", CardKind.Minion, -7f, 0.00f, 0.14f, 0.70f, 0.96f);
                BuildCardArt(artwork.transform, prefix, 1, "133711", CardKind.TavernSpell, 6f, 0.28f, 0.04f, 1.00f, 0.92f);
            }
        }

        private static void BuildCardArt(
            Transform parent,
            string prefix,
            int index,
            string cardId,
            CardKind cardKind,
            float rotation,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            var card = new GameObject(prefix + "CardArt-" + index, typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = card.GetComponent<Image>();
            image.sprite = CardImageProvider.LoadSprite(null, cardId, cardKind);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void BuildVersionContext(Transform parent)
        {
            GameObject footer;
            if (openVersionCenter != null)
            {
                var button = UiFactory.Button(
                    "MainHubVersionCenterButton",
                    parent,
                    string.Empty,
                    () => openVersionCenter.Invoke(),
                    layout);
                footer = button.gameObject;
                UnityTavernUiStyle.ConfigureButton(button, UnityTavernUiStyle.ArcaneBlue, false, false);
                var defaultLabel = button.GetComponentInChildren<Text>();
                if (defaultLabel != null)
                {
                    defaultLabel.gameObject.SetActive(false);
                }
            }
            else
            {
                footer = UiFactory.Panel(
                    "MainHubFooter",
                    parent,
                    UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.SurfaceDark, 0.78f));
            }

            var footerHeight = layout.CanvasUnitsForPhysicalPixels(openVersionCenter != null ? 48f : 40f);
            UiFactory.SetHeight(footer, footerHeight);
            UiFactory.SetMinSize(footer, 0f, footerHeight);
            UiFactory.SetFlexible(footer, 0f, 0f);
            UiFactory.Horizontal(
                footer,
                CompactInt(layout.IsCompact ? 10f : 14f),
                0).childAlignment = TextAnchor.MiddleLeft;

            var versionName = currentGameVersion == null
                ? T("当前训练版本", "Current training version")
                : GameVersionUiText.DisplayName(currentGameVersion, useEnglish);
            var version = UiFactory.Label(
                "MainHubVersionContext",
                footer.transform,
                versionName + (openVersionCenter != null
                    ? T(" · 查看版本与机制　→", " · View versions and mechanics  →")
                    : T(" · 版本与机制可在训练内调整", " · Change version and mechanics inside training")),
                Mathf.CeilToInt(layout.CanvasUnitsForPhysicalPixels(14f)),
                FontStyle.Normal,
                layout);
            version.color = UnityTavernUiStyle.TextMuted;
            version.alignment = TextAnchor.MiddleLeft;
            UiFactory.SetFlexible(version.gameObject, 1f, 0f);
            UiFactory.SetMinSize(version.gameObject, 0f, layout.CanvasUnitsForPhysicalPixels(18f));
        }

        private string T(string chinese, string english)
        {
            return useEnglish ? english : chinese;
        }

        private void RequestLanguage(bool nextUseEnglish)
        {
            if (useEnglish != nextUseEnglish)
            {
                languageChanged?.Invoke(nextUseEnglish);
            }
        }

        private float CompactUnits(float value)
        {
            return layout.IsCompact ? layout.CanvasUnitsForPhysicalPixels(value) : value;
        }

        private int CompactInt(float value)
        {
            return Mathf.CeilToInt(CompactUnits(value));
        }

        private int CompactFont(float compactPhysicalSize, int regularSize)
        {
            return layout.IsCompact
                ? Mathf.CeilToInt(layout.CanvasUnitsForPhysicalPixels(compactPhysicalSize))
                : regularSize;
        }
    }
}
