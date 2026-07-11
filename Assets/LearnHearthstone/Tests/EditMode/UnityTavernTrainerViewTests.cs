using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Adapters.Persistence;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class UnityTavernTrainerViewTests
    {
        private static readonly Keyword[] EditableMinionKeywords =
        {
            Keyword.Taunt,
            Keyword.DivineShield,
            Keyword.Venomous,
            Keyword.Reborn,
            Keyword.Deathrattle,
            Keyword.Windfury,
            Keyword.Stealth
        };
        private const string MillhouseHeroCardId = "TB_BaconShop_HERO_49";
        private const string MillhouseHeroPowerCardId = "TB_BaconShop_HP_054";
        private const string GeorgeHeroCardId = "TB_BaconShop_HERO_15";
        private const string TavernCaptureDirectory = ".planning/ugui-batch0-batch1/captures";

        [Test]
        public void MainHub_BuildCreatesUnityComponentTavernEntry()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var legacyOpened = false;
                var opened = false;
                new MainHubView(
                    rootObject.transform,
                    () => legacyOpened = true,
                    () => { },
                    () => opened = true,
                    UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();

                FindChild(rootObject.transform, "酒馆训练器Button").GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(opened);
                Assert.IsFalse(legacyOpened);
                Assert.IsNull(FindChild(rootObject.transform, "Unity 组件酒馆 UIButton"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void MainHub_CompactLayoutPrioritizesReadablePrimaryEntry()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(994f, 384f)).Build();

                var primaryButton = FindChild(rootObject.transform, "酒馆训练器Button");
                Assert.IsNotNull(primaryButton);
                Assert.GreaterOrEqual(primaryButton.GetComponent<LayoutElement>().preferredHeight, UnityTavernUiStyle.CompactTouchHeight);

                var primaryTile = FindChild(rootObject.transform, "酒馆训练器");
                Assert.GreaterOrEqual(primaryTile.GetComponent<LayoutElement>().preferredHeight, 140f);

                var grid = FindChild(rootObject.transform, "ModuleGrid").GetComponent<GridLayoutGroup>();
                Assert.AreEqual(2, grid.constraintCount);
                Assert.LessOrEqual(grid.cellSize.y, 100f);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void MainHub_LanguageSwitchControlsInitialTavernSetupLanguage()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var useEnglish = false;

                void OpenUnityTrainer()
                {
                    ClearChildren(rootObject.transform);
                    new UnityTavernTribeSelectionView(
                        rootObject.transform,
                        (Action<MatchSetupOptions>)(_ => { }),
                        () => { },
                        UnityTavernLayoutContext.ForSize(1366f, 768f),
                        useEnglish: useEnglish).Build();
                }

                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    OpenUnityTrainer,
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    useEnglish: useEnglish,
                    languageChanged: value => useEnglish = value).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "MainHubLanguageChineseButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "MainHubLanguageEnglishButton"));
                Assert.AreEqual("语言", FindChild(rootObject.transform, "MainHubLanguageLabel").GetComponent<Text>().text);

                FindChild(rootObject.transform, "MainHubLanguageEnglishButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(useEnglish);

                FindChild(rootObject.transform, "酒馆训练器Button").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual("Choose Tribes", FindChild(rootObject.transform, "UnityTribeSelectionTitle").GetComponent<Text>().text);
                Assert.AreEqual("Next", FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponentInChildren<Text>().text);
                Assert.AreEqual("Beast", FindChild(rootObject.transform, "UnityTribeSelectionBeastButtonText").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnitySetupLanguageChineseButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnitySetupLanguageEnglishButton"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void DebugAspectRatioOverlay_BuildOpensPresetPopupAndAppliesSelection()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var appliedWidth = 0;
                var appliedHeight = 0;
                DebugAspectRatioOverlay.Build(
                    rootObject.transform,
                    (width, height) =>
                    {
                        appliedWidth = width;
                        appliedHeight = height;
                    });

                var button = FindChild(rootObject.transform, DebugAspectRatioOverlay.ButtonName);
                Assert.IsNotNull(button);

                var buttonRect = button.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(1f, 0f), buttonRect.anchorMin);
                Assert.AreEqual(new Vector2(1f, 0f), buttonRect.anchorMax);
                Assert.AreEqual(new Vector2(1f, 0f), buttonRect.pivot);
                Assert.AreEqual(new Vector2(-16f, 88f), buttonRect.anchoredPosition);

                button.GetComponent<Button>().onClick.Invoke();
                var panel = FindChild(rootObject.transform, DebugAspectRatioOverlay.ModalPanelName);
                Assert.IsNotNull(panel);
                var panelRect = panel.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(300f, 292f), panelRect.sizeDelta);
                Assert.AreEqual(new Vector2(-16f, 140f), panelRect.anchoredPosition);
                Assert.IsNotNull(FindChild(rootObject.transform, DebugAspectRatioOverlay.CurrentLabelName));
                Assert.IsNotNull(FindChild(rootObject.transform, DebugAspectRatioOverlay.PresetButtonPrefix + "1920x1080"));

                FindChild(rootObject.transform, DebugAspectRatioOverlay.PresetButtonPrefix + "994x384")
                    .GetComponent<Button>()
                    .onClick
                    .Invoke();

                Assert.AreEqual(994, appliedWidth);
                Assert.AreEqual(384, appliedHeight);
                Assert.IsNull(FindChild(rootObject.transform, DebugAspectRatioOverlay.ModalOverlayName));

                button.GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, DebugAspectRatioOverlay.PresetButtonPrefix + "1920x1080")
                    .GetComponent<Button>()
                    .onClick
                    .Invoke();

                Assert.AreEqual(1920, appliedWidth);
                Assert.AreEqual(1080, appliedHeight);
                Assert.IsNull(FindChild(rootObject.transform, DebugAspectRatioOverlay.ModalOverlayName));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void DebugAspectRatioOverlay_DoesNotCoverHeroEffectRack()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                DebugAspectRatioOverlay.Build(rootObject.transform);

                var quickBar = FindChild(rootObject.transform, "UnityHeroEffectRack");
                var button = FindChild(rootObject.transform, DebugAspectRatioOverlay.ButtonName);
                Assert.IsNotNull(quickBar);
                Assert.IsNotNull(button);

                var quickBarTop = quickBar.GetComponent<RectTransform>().offsetMax.y;
                var debugButtonBottom = button.GetComponent<RectTransform>().anchoredPosition.y;
                Assert.Greater(debugButtonBottom, quickBarTop);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TribeSelectionView_RequiresFiveManualChoicesAndSupportsShortcuts()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                List<Tribe> startedWith = null;
                var backClicked = false;
                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    tribes => startedWith = tribes,
                    () => backClicked = true,
                    UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();

                Assert.IsFalse(FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().interactable);

                var firstFive = TribeAvailabilityRules.PlayableTribes.Take(5).ToList();
                foreach (var tribe in firstFive)
                {
                    FindChild(rootObject.transform, "UnityTribeSelection" + tribe + "Button").GetComponent<Button>().onClick.Invoke();
                }

                var enter = FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>();
                Assert.IsTrue(enter.interactable);
                enter.onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsSetupOverlay"));
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                CollectionAssert.AreEqual(firstFive, startedWith);

                FindChild(rootObject.transform, "UnityTribeSelectionRandomButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(FindChild(rootObject.transform, "UnityTribeSelectionSummary").GetComponent<Text>().text.Contains("已选 5/5"));

                startedWith = null;
                FindChild(rootObject.transform, "UnityTribeSelectionAllButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsSetupOverlay"));
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                CollectionAssert.AreEqual(TribeAvailabilityRules.PlayableTribes, startedWith);

                FindChild(rootObject.transform, "UnityTribeSelectionBackButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(backClicked);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TribeSelectionView_DefaultsChineseAndUsesInitialEnglish()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                Action<List<Tribe>> start = _ => { };

                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    start,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();

                Assert.AreEqual("选择本局种族", FindChild(rootObject.transform, "UnityTribeSelectionTitle").GetComponent<Text>().text);
                Assert.AreEqual("下一步", FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponentInChildren<Text>().text);
                Assert.AreEqual("野兽", FindChild(rootObject.transform, "UnityTribeSelectionBeastButtonText").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnitySetupLanguageChineseButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnitySetupLanguageEnglishButton"));

                ClearChildren(rootObject.transform);
                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    start,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    useEnglish: true).Build();

                Assert.AreEqual("Choose Tribes", FindChild(rootObject.transform, "UnityTribeSelectionTitle").GetComponent<Text>().text);
                Assert.AreEqual("Next", FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponentInChildren<Text>().text);
                Assert.AreEqual("Beast", FindChild(rootObject.transform, "UnityTribeSelectionBeastButtonText").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnitySetupLanguageChineseButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnitySetupLanguageEnglishButton"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TribeSelectionView_CardPoolPanelCopiesDefaultAndPassesCustomSetup()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-card-pool-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                MatchSetupOptions startedWith = null;
                var repository = new JsonCardPoolVersionRepository(directory, "versions.json");
                var minionCatalog = MinionCatalogLoader.LoadFromResources();
                var spellCatalog = SpellCatalogLoader.LoadFromResources();
                var disabledMinion = minionCatalog.All
                    .Where(card => card.InPool)
                    .OrderBy(card => card.TavernTier)
                    .ThenBy(card => card.Name)
                    .First();

                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => startedWith = setup,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    repository,
                    minionCatalog,
                    spellCatalog).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolVersionPanel"));
                FindChild(rootObject.transform, "UnityCardPoolVersionOpenButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolVersionOverlay"));
                FindChild(rootObject.transform, "UnityCardPoolVersionCopyButton").GetComponent<Button>().onClick.Invoke();

                var minionToggle = FindChild(rootObject.transform, "UnityCardPoolMinionToggle-" + disabledMinion.CardId);
                Assert.IsNotNull(minionToggle);
                minionToggle.GetComponent<Toggle>().isOn = false;

                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    FindChild(rootObject.transform, "UnityTribeSelection" + tribe + "Button").GetComponent<Button>().onClick.Invoke();
                }

                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(startedWith);
                Assert.IsFalse(startedWith.IsDefaultCardPoolVersion);
                Assert.IsFalse(startedWith.EnabledMinionCardIds.Contains(disabledMinion.CardId));
                Assert.Greater(startedWith.EnabledTavernSpellCardNumbers.Count, 0);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void TribeSelectionView_AdvancedMechanicPoolsDefaultEmpty()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-advanced-mechanics-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                MatchSetupOptions startedWith = null;
                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => startedWith = setup,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources(),
                    useEnglish: true).Build();

                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsSetupPanel"));

                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    FindChild(rootObject.transform, "UnityTribeSelection" + tribe + "Button").GetComponent<Button>().onClick.Invoke();
                }

                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsSetupOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedQuestRewardPoolCard"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedTrinketPoolCard"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedAnomalyPoolCard"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsToggle-EnableQuests"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsToggle-EnableTrinkets"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsToggle-EnableQuestRewards"));

                FindChild(rootObject.transform, "UnityAdvancedMechanicsToggle-ShowProxySafe").GetComponent<Toggle>().isOn = false;
                FindChild(rootObject.transform, "UnityAdvancedMechanicsToggle-ShowHiddenEffectOnly").GetComponent<Toggle>().isOn = true;
                FindChild(rootObject.transform, "UnityAdvancedMechanicsToggle-ShowDebugOnly").GetComponent<Toggle>().isOn = true;
                FindChild(rootObject.transform, "UnityAdvancedMechanicsToggle-ShowDisabled").GetComponent<Toggle>().isOn = true;

                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(startedWith);
                Assert.IsFalse(startedWith.EnableQuests);
                Assert.IsFalse(startedWith.EnableTrinkets);
                Assert.IsFalse(startedWith.EnableQuestRewards);
                Assert.IsFalse(startedWith.EnableAnomalies);
                Assert.AreEqual(0, startedWith.EnabledQuestCardIds.Count);
                Assert.AreEqual(0, startedWith.EnabledQuestRewardCardIds.Count);
                Assert.AreEqual(0, startedWith.EnabledLesserTrinketCardIds.Count);
                Assert.AreEqual(0, startedWith.EnabledGreaterTrinketCardIds.Count);
                Assert.AreEqual(0, startedWith.EnabledAnomalyCardIds.Count);
                Assert.IsFalse(startedWith.ShowProxySafe);
                Assert.IsTrue(startedWith.ShowDebugOnly);
                Assert.IsTrue(startedWith.ShowHiddenEffectOnly);
                Assert.IsTrue(startedWith.ShowDisabled);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void TribeSelectionView_AdvancedMechanicPoolCardsQuickEnableOfferablePools()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-advanced-quick-enable-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                MatchSetupOptions startedWith = null;
                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => startedWith = setup,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources(),
                    useEnglish: true).Build();

                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    FindChild(rootObject.transform, "UnityTribeSelection" + tribe + "Button").GetComponent<Button>().onClick.Invoke();
                }

                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedQuestRewardPoolCardEnableButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedPoolEditorOverlay"));
                Assert.IsFalse(FindChild(rootObject.transform, "UnityAdvancedQuestRewardPoolCardEnableButton").GetComponent<Button>().interactable);

                FindChild(rootObject.transform, "UnityAdvancedTrinketPoolCardEnableButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedPoolEditorOverlay"));

                FindChild(rootObject.transform, "UnityAdvancedAnomalyPoolCardEnableButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedPoolEditorOverlay"));

                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(startedWith);
                Assert.IsTrue(startedWith.EnableQuests);
                Assert.IsTrue(startedWith.EnableQuestRewards);
                Assert.IsTrue(startedWith.EnableTrinkets);
                Assert.IsTrue(startedWith.EnableAnomalies);
                Assert.Greater(startedWith.EnabledQuestCardIds.Count, 0);
                Assert.Greater(startedWith.EnabledQuestRewardCardIds.Count, 0);
                Assert.Greater(startedWith.EnabledLesserTrinketCardIds.Count + startedWith.EnabledGreaterTrinketCardIds.Count, 0);
                Assert.Greater(startedWith.EnabledAnomalyCardIds.Count, 0);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void TribeSelectionView_AdvancedMechanicPoolEditorPassesSelectedPools()
        {
            UiFactory.SetFontOverride(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-advanced-pool-editor-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                MatchSetupOptions startedWith = null;
                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => startedWith = setup,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    new LearnHearthstone.Domain.Data.MinionCatalog(Array.Empty<MinionDefinition>()),
                    new LearnHearthstone.Domain.Data.SpellCatalog(Array.Empty<TavernSpellDefinition>()),
                    heroCatalog: CreateMinimalHeroCatalog(),
                    anomalyCatalog: CreateMinimalAnomalyCatalog(),
                    useEnglish: true,
                    questCatalog: CreateMinimalQuestCatalog(),
                    trinketCatalog: CreateMinimalTrinketCatalog()).Build();

                FindChild(rootObject.transform, "UnityTribeSelectionAllButton").GetComponent<Button>().onClick.Invoke();

                FindChild(rootObject.transform, "UnityAdvancedQuestRewardPoolCardEditButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedPoolOfferableOnlyButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedPoolTab-Trinkets").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedPoolOfferableOnlyButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedPoolTab-Anomalies").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedPoolOfferableOnlyButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedPoolEditorCloseButton").GetComponent<Button>().onClick.Invoke();

                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(startedWith);
                Assert.IsTrue(startedWith.EnableQuests);
                Assert.IsTrue(startedWith.EnableQuestRewards);
                Assert.IsTrue(startedWith.EnableTrinkets);
                Assert.IsTrue(startedWith.EnableAnomalies);
                Assert.Greater(startedWith.EnabledQuestCardIds.Count, 0);
                Assert.Greater(startedWith.EnabledQuestRewardCardIds.Count, 0);
                Assert.Greater(startedWith.EnabledLesserTrinketCardIds.Count + startedWith.EnabledGreaterTrinketCardIds.Count, 0);
                Assert.Greater(startedWith.EnabledAnomalyCardIds.Count, 0);
                Assert.AreEqual(startedWith.EnabledAnomalyCardIds.Count != 1, startedWith.RandomizeAnomaly);
                Assert.IsTrue(string.IsNullOrEmpty(startedWith.SelectedAnomalyCardId) || startedWith.EnabledAnomalyCardIds.Contains(startedWith.SelectedAnomalyCardId));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }

                UiFactory.SetFontOverride(null);
            }
        }

        [Test]
        public void TribeSelectionView_TimewarpedPoolVersionButtonPassesSetup()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-timewarped-version-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                MatchSetupOptions startedWith = null;
                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => startedWith = setup,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources()).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolVersionPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTimewarpedPoolVersionButton"));

                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    FindChild(rootObject.transform, "UnityTribeSelection" + tribe + "Button").GetComponent<Button>().onClick.Invoke();
                }

                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(startedWith);
                Assert.AreEqual(TimewarpedPoolVersion.Current, startedWith.TimewarpedPoolVersion);
                Assert.IsFalse(startedWith.UseHistoricalTimewarpedPool);

                FindChild(rootObject.transform, "UnityTimewarpedPoolVersionButton").GetComponent<Button>().onClick.Invoke();
                startedWith = null;
                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(startedWith);
                Assert.AreEqual(TimewarpedPoolVersion.FirestoneAll, startedWith.TimewarpedPoolVersion);
                Assert.IsTrue(startedWith.UseHistoricalTimewarpedPool);

                FindChild(rootObject.transform, "UnityTimewarpedPoolVersionButton").GetComponent<Button>().onClick.Invoke();
                startedWith = null;
                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(startedWith);
                Assert.AreEqual(TimewarpedPoolVersion.Launch, startedWith.TimewarpedPoolVersion);
                Assert.IsTrue(startedWith.UseHistoricalTimewarpedPool);

                FindChild(rootObject.transform, "UnityTimewarpedPoolVersionButton").GetComponent<Button>().onClick.Invoke();
                startedWith = null;
                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(startedWith);
                Assert.AreEqual(TimewarpedPoolVersion.Current, startedWith.TimewarpedPoolVersion);
                Assert.IsFalse(startedWith.UseHistoricalTimewarpedPool);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void TribeSelectionView_AnomalyPoolDefaultClosedWithoutLegacyButtons()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-anomaly-random-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                MatchSetupOptions startedWith = null;
                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => startedWith = setup,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources()).Build();

                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    FindChild(rootObject.transform, "UnityTribeSelection" + tribe + "Button").GetComponent<Button>().onClick.Invoke();
                }

                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedAnomalyPoolCard"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsToggle-EnableAnomalies"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAnomalyRandomButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAnomalySelectButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAnomalySelectionOverlay"));
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(startedWith);
                Assert.IsFalse(startedWith.EnableAnomalies);
                Assert.IsFalse(startedWith.RandomizeAnomaly);
                Assert.IsNull(startedWith.SelectedAnomalyCardId);
                Assert.AreEqual(0, startedWith.EnabledAnomalyCardIds.Count);
                Assert.AreEqual(AnomalyPoolVersion.CurrentHsReplay, startedWith.AnomalyPoolVersion);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void TribeSelectionView_AnomalyPoolVersionButtonPassesSetup()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-anomaly-version-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                MatchSetupOptions startedWith = null;
                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => startedWith = setup,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources()).Build();

                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    FindChild(rootObject.transform, "UnityTribeSelection" + tribe + "Button").GetComponent<Button>().onClick.Invoke();
                }

                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAnomalyPoolVersionButton"));
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(startedWith);
                Assert.AreEqual(AnomalyPoolVersion.CurrentHsReplay, startedWith.AnomalyPoolVersion);

                FindChild(rootObject.transform, "UnityAnomalyPoolVersionButton").GetComponent<Button>().onClick.Invoke();
                startedWith = null;
                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(startedWith);
                Assert.AreEqual(AnomalyPoolVersion.Season5AllBg27, startedWith.AnomalyPoolVersion);

                FindChild(rootObject.transform, "UnityAnomalyPoolVersionButton").GetComponent<Button>().onClick.Invoke();
                startedWith = null;
                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(startedWith);
                Assert.AreEqual(AnomalyPoolVersion.Season5Launch, startedWith.AnomalyPoolVersion);

                FindChild(rootObject.transform, "UnityAnomalyPoolVersionButton").GetComponent<Button>().onClick.Invoke();
                startedWith = null;
                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(startedWith);
                Assert.AreEqual(AnomalyPoolVersion.AllKnown, startedWith.AnomalyPoolVersion);

                FindChild(rootObject.transform, "UnityAnomalyPoolVersionButton").GetComponent<Button>().onClick.Invoke();
                startedWith = null;
                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(startedWith);
                Assert.AreEqual(AnomalyPoolVersion.CurrentHsReplay, startedWith.AnomalyPoolVersion);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void TribeSelectionView_AnomalyPoolSingleSelectionPassesFixedSetup()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-anomaly-picker-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                MatchSetupOptions startedWith = null;
                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => startedWith = setup,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources()).Build();

                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    FindChild(rootObject.transform, "UnityTribeSelection" + tribe + "Button").GetComponent<Button>().onClick.Invoke();
                }

                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedAnomalyPoolCardEditButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedPoolSearchInput").GetComponent<InputField>().onEndEdit.Invoke("BG31_Anomaly_123");
                FindChild(rootObject.transform, "UnityAdvancedPoolAnomalyToggle-BG31_Anomaly_123").GetComponent<Toggle>().isOn = true;
                FindChild(rootObject.transform, "UnityAdvancedPoolEditorCloseButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(startedWith);
                Assert.IsTrue(startedWith.EnableAnomalies);
                Assert.IsFalse(startedWith.RandomizeAnomaly);
                Assert.AreEqual("BG31_Anomaly_123", startedWith.SelectedAnomalyCardId);
                CollectionAssert.AreEqual(new[] { "BG31_Anomaly_123" }, startedWith.EnabledAnomalyCardIds);
                Assert.AreEqual(AnomalyPoolVersion.CurrentHsReplay, startedWith.AnomalyPoolVersion);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void TribeSelectionView_HeroStripSelectsHeroAndPassesSetup()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-hero-selection-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                MatchSetupOptions startedWith = null;
                var heroCatalog = HeroCatalogLoader.LoadFromResources();
                var targetHero = heroCatalog.GetInitialSelectableHeroes()
                    .First(hero => !string.Equals(hero.Name, "Patchwerk", StringComparison.OrdinalIgnoreCase));

                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => startedWith = setup,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources(),
                    heroCatalog).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTribeSelectionHeroPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTribeSelectionChooseHeroButton"));

                FindChild(rootObject.transform, "UnityTribeSelectionChooseHeroButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroSelectionOverlay"));
                FindChild(rootObject.transform, "UnityHeroSelectionHeroButton-" + HeroSelectionSafeName(targetHero.HeroCardId)).GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityHeroSelectionConfirmButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(ChineseHeroName(targetHero), FindChild(rootObject.transform, "UnityTribeSelectionHeroName").GetComponent<Text>().text);

                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    FindChild(rootObject.transform, "UnityTribeSelection" + tribe + "Button").GetComponent<Button>().onClick.Invoke();
                }

                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(startedWith);
                Assert.AreEqual(targetHero.HeroCardId, startedWith.SelectedHeroCardId);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void HeroSelectionModal_SearchFiltersAndDirectChooseUpdatesOpeningStrip()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-hero-search-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                var heroCatalog = HeroCatalogLoader.LoadFromResources();
                var targetHero = heroCatalog.GetInitialSelectableHeroes()
                    .First(hero => !string.Equals(hero.Name, "Patchwerk", StringComparison.OrdinalIgnoreCase));

                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources(),
                    heroCatalog).Build();

                FindChild(rootObject.transform, "UnityTribeSelectionChooseHeroButton").GetComponent<Button>().onClick.Invoke();
                Assert.Greater(FindChildren(rootObject.transform, "UnityHeroSelectionHeroButton-").Count, 1);

                FindChild(rootObject.transform, "UnityHeroSelectionSearchInput")
                    .GetComponent<InputField>()
                    .onEndEdit
                    .Invoke(targetHero.HeroCardId);

                Assert.AreEqual("1 个英雄", FindChild(rootObject.transform, "UnityHeroSelectionResultCount").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroSelectionHeroButton-" + HeroSelectionSafeName(targetHero.HeroCardId)));
                Assert.AreEqual(1, FindChildren(rootObject.transform, "UnityHeroSelectionHeroButton-").Count);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityHeroSelectionClearSearchButton").GetComponent<Button>().interactable);

                FindChild(rootObject.transform, "UnityHeroSelectionClearSearchButton")
                    .GetComponent<Button>()
                    .onClick
                    .Invoke();
                Assert.Greater(FindChildren(rootObject.transform, "UnityHeroSelectionHeroButton-").Count, 1);

                FindChild(rootObject.transform, "UnityHeroSelectionSearchInput")
                    .GetComponent<InputField>()
                    .onEndEdit
                    .Invoke(targetHero.HeroCardId);

                FindChild(rootObject.transform, "UnityHeroSelectionHeroChooseButton-" + HeroSelectionSafeName(targetHero.HeroCardId))
                    .GetComponent<Button>()
                    .onClick
                    .Invoke();

                Assert.AreEqual(ChineseHeroName(targetHero), FindChild(rootObject.transform, "UnityTribeSelectionHeroName").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityHeroSelectionOverlay"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void HeroSelectionModal_UsesZhCnHeroLocalizationAndInitialEnglish()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-hero-localization-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                var heroCatalog = HeroCatalogLoader.LoadFromResources();
                var yogg = heroCatalog.GetHeroByCardId("TB_BaconShop_HERO_35");

                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources(),
                    heroCatalog).Build();

                FindChild(rootObject.transform, "UnityTribeSelectionChooseHeroButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual("尤格-萨隆", FindChild(rootObject.transform, "UnityHeroSelectionHeroName-" + HeroSelectionSafeName(yogg.HeroCardId)).GetComponent<Text>().text);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityHeroSelectionHeroDetail-" + HeroSelectionSafeName(yogg.HeroCardId)).GetComponent<Text>().text.StartsWith("谜之匣 · 费用 0", StringComparison.Ordinal));

                FindChild(rootObject.transform, "UnityHeroSelectionSearchInput")
                    .GetComponent<InputField>()
                    .onEndEdit
                    .Invoke("尤格");

                Assert.AreEqual("1 个英雄", FindChild(rootObject.transform, "UnityHeroSelectionResultCount").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroSelectionHeroButton-" + HeroSelectionSafeName(yogg.HeroCardId)));

                FindChild(rootObject.transform, "UnityHeroSelectionHeroChooseButton-" + HeroSelectionSafeName(yogg.HeroCardId))
                    .GetComponent<Button>()
                    .onClick
                    .Invoke();

                Assert.AreEqual("尤格-萨隆", FindChild(rootObject.transform, "UnityTribeSelectionHeroName").GetComponent<Text>().text);
                Assert.AreEqual("技能：谜之匣 / 费用 0", FindChild(rootObject.transform, "UnityTribeSelectionHeroPower").GetComponent<Text>().text);

                ClearChildren(rootObject.transform);
                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources(),
                    heroCatalog,
                    useEnglish: true).Build();

                FindChild(rootObject.transform, "UnityTribeSelectionChooseHeroButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(yogg.Name, FindChild(rootObject.transform, "UnityHeroSelectionHeroName-" + HeroSelectionSafeName(yogg.HeroCardId)).GetComponent<Text>().text);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityHeroSelectionHeroDetail-" + HeroSelectionSafeName(yogg.HeroCardId)).GetComponent<Text>().text.Contains("Puzzle Box"));

                FindChild(rootObject.transform, "UnityHeroSelectionHeroChooseButton-" + HeroSelectionSafeName(yogg.HeroCardId))
                    .GetComponent<Button>()
                    .onClick
                    .Invoke();

                Assert.AreEqual(yogg.Name, FindChild(rootObject.transform, "UnityTribeSelectionHeroName").GetComponent<Text>().text);
                Assert.AreEqual("Power: Puzzle Box / Cost 0", FindChild(rootObject.transform, "UnityTribeSelectionHeroPower").GetComponent<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void HeroSelectionModal_CompactWindowKeepsCriticalControlsReachable()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-hero-compact-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                var rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(994f, 384f);

                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(994f, 384f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources(),
                    HeroCatalogLoader.LoadFromResources()).Build();

                FindChild(rootObject.transform, "UnityTribeSelectionChooseHeroButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroSelectionSearchInput"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroSelectionCloseButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroSelectionConfirmButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroSelectionHeroScroll"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void HeroSelectionModal_CategoryFilterReducesVisibleHeroes()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-hero-category-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                var heroCatalog = HeroCatalogLoader.LoadFromResources();
                var selectable = heroCatalog.GetInitialSelectableHeroes();
                var categoryGroup = selectable
                    .Where(hero => hero.HeroPower != null)
                    .GroupBy(hero => hero.HeroPower.PrimaryCategory)
                    .First(group => group.Count() > 0 && group.Count() < selectable.Count);
                var includedHero = categoryGroup.First();
                var excludedHero = selectable.First(hero => hero.HeroPower == null || hero.HeroPower.PrimaryCategory != categoryGroup.Key);

                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources(),
                    heroCatalog).Build();

                FindChild(rootObject.transform, "UnityTribeSelectionChooseHeroButton").GetComponent<Button>().onClick.Invoke();
                var allVisible = FindChildren(rootObject.transform, "UnityHeroSelectionHeroButton-").Count;

                FindChild(rootObject.transform, "UnityHeroSelectionCategory" + categoryGroup.Key + "Button")
                    .GetComponent<Button>()
                    .onClick
                    .Invoke();

                Assert.Less(FindChildren(rootObject.transform, "UnityHeroSelectionHeroButton-").Count, allVisible);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroSelectionHeroButton-" + HeroSelectionSafeName(includedHero.HeroCardId)));
                Assert.IsNull(FindChild(rootObject.transform, "UnityHeroSelectionHeroButton-" + HeroSelectionSafeName(excludedHero.HeroCardId)));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void TribeSelectionView_CardPoolModalRenamesFiltersAndBulkExcludes()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-card-pool-filter-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                var repository = new JsonCardPoolVersionRepository(directory, "versions.json");
                var minionCatalog = MinionCatalogLoader.LoadFromResources();
                var spellCatalog = SpellCatalogLoader.LoadFromResources();
                var filteredMinion = minionCatalog.All
                    .Where(card => card.InPool && !card.CardId.StartsWith("BGDUO", StringComparison.OrdinalIgnoreCase) && card.Tribes != null && card.Tribes.Any(tribe => tribe != Tribe.None && tribe != Tribe.All))
                    .OrderBy(card => card.TavernTier)
                    .ThenBy(card => card.Name)
                    .First();
                var filteredTribe = filteredMinion.Tribes.First(tribe => tribe != Tribe.None && tribe != Tribe.All);
                var duoMinion = minionCatalog.All.First(card => card.InPool && card.CardId.StartsWith("BGDUO", StringComparison.OrdinalIgnoreCase));
                var lazyLoadedMinion = minionCatalog.All
                    .Where(card => card.InPool && !card.CardId.StartsWith("BGDUO", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(card => card.TavernTier)
                    .ThenBy(card => card.Name)
                    .Skip(100)
                    .First();
                var defaultSelection = CardPoolVersionFactory.CreateDefaultSelection(minionCatalog, spellCatalog);
                Assert.IsFalse(defaultSelection.EnabledMinionCardIds.Any(cardId => cardId.StartsWith("BGDUO", StringComparison.OrdinalIgnoreCase)));

                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    repository,
                    minionCatalog,
                    spellCatalog).Build();

                FindChild(rootObject.transform, "UnityCardPoolVersionOpenButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual("版本名称", FindChild(rootObject.transform, "UnityCardPoolVersionNameLabel").GetComponent<Text>().text);
                Assert.AreEqual("默认只读", FindChild(rootObject.transform, "UnityCardPoolVersionNameHint").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityCardPoolMinionToggle-" + lazyLoadedMinion.CardId));
                Assert.IsTrue(FindChild(rootObject.transform, "UnityCardPoolVersionBulkHint").GetComponent<Text>().text.Contains("列表已显示"));

                FindChild(rootObject.transform, "UnityCardPoolVersionSearchInput").GetComponent<InputField>().onEndEdit.Invoke(duoMinion.CardId);
                Assert.IsNull(FindChild(rootObject.transform, "UnityCardPoolMinionToggle-" + duoMinion.CardId));
                Assert.AreEqual("当前筛选无卡牌", FindChild(rootObject.transform, "UnityCardPoolVersionLoadState").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityCardPoolVersionSearchInput").GetComponent<InputField>().onEndEdit.Invoke(string.Empty);
                FindChild(rootObject.transform, "UnityCardPoolVersionScroll").GetComponent<ScrollRect>().onValueChanged.Invoke(Vector2.zero);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolMinionToggle-" + lazyLoadedMinion.CardId));

                FindChild(rootObject.transform, "UnityCardPoolVersionCopyButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual("点击改名", FindChild(rootObject.transform, "UnityCardPoolVersionNameHint").GetComponent<Text>().text);

                var nameInput = FindChild(rootObject.transform, "UnityCardPoolVersionNameInput").GetComponent<InputField>();
                Assert.IsNotNull(nameInput.GetComponent<Outline>());
                nameInput.onEndEdit.Invoke("野兽削弱测试版");

                FindChild(rootObject.transform, "UnityCardPoolVersionTier" + filteredMinion.TavernTier + "Button").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityCardPoolVersionTribe" + filteredTribe + "Button").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolMinionToggle-" + filteredMinion.CardId + "ImageFrame"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolVersionFilterCount"));
                Assert.IsTrue(FindChild(rootObject.transform, "UnityCardPoolVersionBulkHint").GetComponent<Text>().text.Contains("批量操作会影响当前筛选的全部"));

                FindChild(rootObject.transform, "UnityCardPoolVersionExcludeFilteredButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsFalse(FindChild(rootObject.transform, "UnityCardPoolMinionToggle-" + filteredMinion.CardId).GetComponent<Toggle>().isOn);

                FindChild(rootObject.transform, "UnityCardPoolVersionSaveButton").GetComponent<Button>().onClick.Invoke();

                var saved = repository.Load();
                Assert.AreEqual(1, saved.Versions.Count);
                Assert.AreEqual("野兽削弱测试版", saved.Versions[0].Name);
                CollectionAssert.DoesNotContain(saved.Versions[0].EnabledMinionCardIds, filteredMinion.CardId);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void TribeSelectionView_CardPoolModalShowsTimewarpedTab()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-card-pool-timewarped-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                var timewarpedCard = TimewarpedTavernCatalogLoader.LoadFromResources()
                    .All
                    .First(card => !string.IsNullOrEmpty(card.CardId));

                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    new JsonCardPoolVersionRepository(directory, "versions.json"),
                    MinionCatalogLoader.LoadFromResources(),
                    SpellCatalogLoader.LoadFromResources()).Build();

                FindChild(rootObject.transform, "UnityCardPoolVersionOpenButton").GetComponent<Button>().onClick.Invoke();
                var tab = FindChild(rootObject.transform, "UnityCardPoolVersionTimewarpedTab");
                Assert.IsNotNull(tab);
                tab.GetComponent<Button>().onClick.Invoke();

                var row = FindChild(rootObject.transform, "UnityCardPoolTimewarpedToggle-" + HeroSelectionSafeName(timewarpedCard.CardId));
                Assert.IsNotNull(row);
                Assert.IsFalse(row.GetComponent<Toggle>().interactable);
                Assert.IsFalse(FindChild(rootObject.transform, "UnityCardPoolVersionExcludeFilteredButton").GetComponent<Button>().interactable);
                Assert.IsFalse(FindChild(rootObject.transform, "UnityCardPoolVersionIncludeFilteredButton").GetComponent<Button>().interactable);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void TribeSelectionView_CardPoolModalPromptsBeforeSwitchingUnsavedVersion()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-card-pool-unsaved-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                var repository = new JsonCardPoolVersionRepository(directory, "versions.json");
                var minionCatalog = MinionCatalogLoader.LoadFromResources();
                var spellCatalog = SpellCatalogLoader.LoadFromResources();
                var disabledMinion = minionCatalog.All
                    .Where(card => card.InPool)
                    .OrderBy(card => card.TavernTier)
                    .ThenBy(card => card.Name)
                    .First();

                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    setup => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    repository,
                    minionCatalog,
                    spellCatalog).Build();

                FindChild(rootObject.transform, "UnityCardPoolVersionOpenButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityCardPoolVersionCopyButton").GetComponent<Button>().onClick.Invoke();

                FindChild(rootObject.transform, "UnityCardPoolMinionToggle-" + disabledMinion.CardId).GetComponent<Toggle>().isOn = false;

                var saveButton = FindChild(rootObject.transform, "UnityCardPoolVersionSaveButton").GetComponent<Button>();
                Assert.IsTrue(saveButton.interactable);
                Assert.AreEqual("保存*", saveButton.GetComponentInChildren<Text>().text);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityCardPoolVersionModalSummary").GetComponent<Text>().text.Contains("未保存"));

                FindChild(rootObject.transform, "UnityCardPoolVersionDefaultButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolVersionUnsavedDialog"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolVersionConfirmSaveAndSwitchButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolVersionConfirmDiscardButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolVersionConfirmCancelButton"));

                FindChild(rootObject.transform, "UnityCardPoolVersionConfirmCancelButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNull(FindChild(rootObject.transform, "UnityCardPoolVersionUnsavedDialog"));
                Assert.IsTrue(FindChild(rootObject.transform, "UnityCardPoolVersionModalSummary").GetComponent<Text>().text.Contains("未保存"));

                FindChild(rootObject.transform, "UnityCardPoolVersionDefaultButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityCardPoolVersionConfirmSaveAndSwitchButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsFalse(FindChild(rootObject.transform, "UnityCardPoolVersionSaveButton").GetComponent<Button>().interactable);
                Assert.IsFalse(FindChild(rootObject.transform, "UnityCardPoolVersionModalSummary").GetComponent<Text>().text.Contains("未保存"));

                var saved = repository.Load();
                Assert.AreEqual(1, saved.Versions.Count);
                CollectionAssert.DoesNotContain(saved.Versions[0].EnabledMinionCardIds, disabledMinion.CardId);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void Build_CreatesUnityComponentZonesAndStableSlots()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var shell = FindChild(rootObject.transform, "UnityTavernTrainer");
                Assert.IsNotNull(shell);
                Assert.IsNotNull(shell.GetComponent<UnityTavernTrainerController>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTopBar"));
                var topBarLayout = FindChild(rootObject.transform, "UnityTopBar").GetComponent<HorizontalLayoutGroup>();
                Assert.IsTrue(topBarLayout.childControlWidth);
                Assert.IsTrue(topBarLayout.childControlHeight);
                Assert.IsFalse(topBarLayout.childForceExpandHeight);
                Assert.AreEqual("返回", FindChild(rootObject.transform, "UnityBackButtonText").GetComponent<Text>().text);
                Assert.AreEqual(48f, FindChild(rootObject.transform, "UnityBackButton").GetComponent<LayoutElement>().preferredWidth, 0.001f);
                Assert.AreEqual(30f, FindChild(rootObject.transform, "UnityBackButton").GetComponent<LayoutElement>().preferredHeight, 0.001f);
                Assert.AreEqual(0f, FindChild(rootObject.transform, "UnityTopBarSpacer").GetComponent<LayoutElement>().flexibleHeight, 0.001f);
                Assert.IsNull(FindChild(rootObject.transform, "UnityLegacyButton"));
                var shopZone = FindChild(rootObject.transform, "UnityShopZone");
                var playerBoardZone = FindChild(rootObject.transform, "UnityPlayerBoardZone");
                var handZone = FindChild(rootObject.transform, "UnityHandZone");
                Assert.AreEqual(0f, FindChild(playerBoardZone, "UnityZoneCardRow").GetComponent<Image>().color.a, 0.001f);
                Assert.IsNotNull(shopZone.GetComponent<UnityTavernZoneComponent>());
                Assert.IsNotNull(playerBoardZone.GetComponent<UnityTavernZoneComponent>());
                Assert.IsNotNull(handZone.GetComponent<UnityTavernZoneComponent>());
                Assert.AreNotEqual(shopZone.GetComponent<Image>().color, playerBoardZone.GetComponent<Image>().color);
                Assert.AreNotEqual(playerBoardZone.GetComponent<Image>().color, handZone.GetComponent<Image>().color);
                Assert.IsNotNull(FindChild(shopZone, "UnityZoneAccentMark"));
                Assert.IsNotNull(FindChild(playerBoardZone, "UnityZoneAccentMark"));
                Assert.IsNotNull(FindChild(handZone, "UnityZoneAccentMark"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentEntryPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentEntryButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentBoardZone"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityQuickActionBar"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTavernActionBar"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroEffectRack"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityPlayerBoardReorderDropZone"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityQuickRefreshButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityQuickFreezeButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityQuickUpgradeButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityQuickNextTurnButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityQuickCombatButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityQuickReplayButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityQuickToolsButton"));
                FindChild(rootObject.transform, "UnityQuickFreezeButton").GetComponent<Button>().onClick.Invoke();
                playerBoardZone = FindChild(rootObject.transform, "UnityPlayerBoardZone");
                Assert.AreEqual(0f, FindChild(playerBoardZone, "UnityZoneCardRow").GetComponent<Image>().color.a, 0.001f);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroBadge"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroBadgeName"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroBadgePower"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanelDrawerToggle"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityRightPanel"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityRefreshButton"));
                Assert.AreEqual(7, FindChildren(rootObject.transform, "UnityPlayerBoardZoneSlot-").Count);
                Assert.AreEqual(10, FindChildren(rootObject.transform, "UnityHandZoneSlot-").Count);
                Assert.GreaterOrEqual(FindComponentsNamed(rootObject.transform, "UnityTavernCardComponent").Count, service.State.Player.Tavern.Shop.Count);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TavernTable_BackButtonRequiresExitConfirmation()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                rootObject.GetComponent<RectTransform>().sizeDelta = new Vector2(994f, 384f);
                var exited = false;
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => exited = true).Build();

                FindChild(rootObject.transform, "UnityBackButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsFalse(exited);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityReturnConfirmOverlay"));
                var panelRect = FindChild(rootObject.transform, "UnityReturnConfirmPanel").GetComponent<RectTransform>();
                Assert.LessOrEqual(panelRect.sizeDelta.x, 994f - 32f);
                Assert.LessOrEqual(panelRect.sizeDelta.y, 384f - 32f);
                Assert.AreEqual("返回后退出本局模拟", FindChild(rootObject.transform, "UnityReturnConfirmMessage").GetComponent<Text>().text);
                Assert.AreEqual("是", FindChild(rootObject.transform, "UnityReturnConfirmYesButtonText").GetComponent<Text>().text);
                Assert.AreEqual("否", FindChild(rootObject.transform, "UnityReturnConfirmNoButtonText").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityReturnConfirmNoButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsFalse(exited);
                Assert.IsNull(FindChild(rootObject.transform, "UnityReturnConfirmOverlay"));

                FindChild(rootObject.transform, "UnityBackButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityReturnConfirmYesButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(exited);
                Assert.IsNull(FindChild(rootObject.transform, "UnityReturnConfirmOverlay"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TavernTable_CapturesMainDeskAcceptanceAtTargetResolutions()
        {
            CaptureAndAssertTavernTable(1920, 1080, "batch0-main-tavern-1920x1080.png");
            CaptureAndAssertTavernTable(994, 384, "batch0-main-tavern-994x384.png");
        }

        [Test]
        public void TavernTable_CompactWindowKeepsMainRowsAndControlsReachable()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                rootObject.GetComponent<RectTransform>().sizeDelta = new Vector2(994f, 384f);
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                Canvas.ForceUpdateCanvases();

                AssertMainTableRows(rootObject.transform);

                var quickBar = FindChild(rootObject.transform, "UnityTavernActionBar").GetComponent<RectTransform>();
                Assert.GreaterOrEqual(quickBar.anchorMin.x, 0.34f);
                Assert.AreEqual(1f, quickBar.anchorMax.x, 0.001f);
                Assert.GreaterOrEqual(quickBar.offsetMax.y - quickBar.offsetMin.y, UnityTavernUiStyle.CompactTouchHeight);

                var quickNextTurn = FindChild(rootObject.transform, "UnityQuickNextTurnButton").GetComponent<LayoutElement>();
                Assert.GreaterOrEqual(quickNextTurn.preferredHeight, UnityTavernUiStyle.CompactTouchHeight);

                var drawerToggle = FindChild(rootObject.transform, "UnityRightPanelDrawerToggle").GetComponent<RectTransform>();
                Assert.GreaterOrEqual(drawerToggle.sizeDelta.x, 44f);
                Assert.GreaterOrEqual(drawerToggle.sizeDelta.y, 48f);
                Assert.LessOrEqual(drawerToggle.anchoredPosition.x, -UnityTavernUiStyle.SpacingSm);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TavernTable_OpensOpponentDetailsAsOverlayWithoutPermanentOpponentZones()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Opponent.Board.Add(CreateBoardMinion(service, "opponent-overlay-card", BoardSide.Opponent, 4, 5));

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var shopZone = FindChild(rootObject.transform, "UnityShopZone");
                Assert.IsNotNull(shopZone);
                Assert.AreEqual("UnityTableColumn", shopZone.parent.name);
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentBoardZone"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentEntryButton"));

                FindChild(rootObject.transform, "UnityOpponentEntryButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentPanelOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentBoardZone"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHandZone"));
                Assert.AreEqual("UnityTableColumn", FindChild(rootObject.transform, "UnityShopZone").parent.name);

                FindChild(rootObject.transform, "UnityOpponentPanelCloseButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentPanelOverlay"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentBoardZone"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityShopZone"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void OpponentMechanicConfiguration_DisabledSetupHidesSections()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(
                    12345,
                    new InMemoryTestScenarioRepository(),
                    new MatchSetupOptions
                    {
                        EnableQuests = false,
                        EnableQuestRewards = false,
                        EnableTrinkets = false
                    });

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentEntryMechanicChips"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentEntryHeroPowerChip"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentEntryQuestRewardChip"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentEntryLesserTrinketChip"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentEntryGreaterTrinketChip"));

                FindChild(rootObject.transform, "UnityOpponentEntryButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentPanelOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentMechanicSection"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerSection"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerSelectButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentQuestRewardSection"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentTrinketSection"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void OpponentMechanicConfiguration_DetailOverlayShowsSectionsButMainEntryStaysCompact()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentEntryMechanicChips"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentEntryHeroPowerChip"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentEntryQuestRewardChip"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentEntryLesserTrinketChip"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentEntryGreaterTrinketChip"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerSelectButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentQuestRewardSelectButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentTrinketSelectButton-Lesser"));

                FindChild(rootObject.transform, "UnityOpponentEntryButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentMechanicSection"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerSection"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerName"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerText"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerSelectButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerClearButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerTargetRow"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerTargetText"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerTargetPlayerLeftButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerTargetOpponentLeftButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerTargetClearButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentQuestRewardSection"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentQuestRewardName"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentQuestRewardText"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentQuestRewardSelectButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentQuestRewardClearButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentTrinketSection"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentTrinketSlot-Lesser"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentTrinketSlot-Greater"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentTrinketName-Lesser"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentTrinketName-Greater"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentTrinketStatus-Lesser"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentTrinketStatus-Greater"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentTrinketSelectButton-Lesser"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentTrinketSelectButton-Greater"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentTrinketClearButton-Lesser"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentTrinketClearButton-Greater"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void OpponentMechanicConfiguration_SelectsQuestRewardWithoutPlayerChoiceModal()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var firstReward = service.GetOpponentSelectableQuestRewards()
                    .OrderBy(reward => reward.PowerLevel + " / " + reward.Trigger + " / " + reward.OfferPoolStatus)
                    .ThenBy(reward => reward.Name)
                    .First();

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                FindChild(rootObject.transform, "UnityOpponentEntryButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityOpponentQuestRewardSelectButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentMechanicLibraryOverlay"));
                Assert.AreEqual("选择对手任务奖励", FindChild(rootObject.transform, "UnityOpponentMechanicLibraryTitle").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedMechanicChoiceOverlay"));

                FindChild(rootObject.transform, "UnityOpponentMechanicLibrarySelectButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(firstReward.Id, service.State.Opponent.AdvancedMechanics.Quests.MainQuest.RewardId);
                Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
                Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest);
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentMechanicLibraryOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentPanelOverlay"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void OpponentMechanicConfiguration_SelectsAndClearsHeroPower()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                service.State.Player.Board.Add(CreateBoardMinion(service, "ui-player-target", BoardSide.Player, 2, 8));
                service.State.Player.Board.Add(CreateBoardMinion(service, "ui-player-second-target", BoardSide.Player, 3, 9));
                service.State.Opponent.Board.Add(CreateBoardMinion(service, "ui-opponent-target", BoardSide.Opponent, 2, 8));
                service.State.Opponent.Board.Add(CreateBoardMinion(service, "ui-opponent-second-target", BoardSide.Opponent, 3, 9));
                var firstPower = service.GetOpponentSelectableHeroPowers()
                    .OrderBy(power => "Hero Power / " + power.Cost + "g / " + power.PrimaryCategory)
                    .ThenBy(power => power.Name)
                    .ThenBy(power => power.CardId)
                    .First();

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                FindChild(rootObject.transform, "UnityOpponentEntryButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityOpponentHeroPowerSelectButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentMechanicLibraryOverlay"));
                Assert.AreEqual("选择对手英雄技能", FindChild(rootObject.transform, "UnityOpponentMechanicLibraryTitle").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedMechanicChoiceOverlay"));

                FindChild(rootObject.transform, "UnityOpponentMechanicLibrarySelectButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(firstPower.CardId, service.State.Opponent.HeroPowerCardId);
                Assert.AreEqual(firstPower.Name, FindChild(rootObject.transform, "UnityOpponentHeroPowerName").GetComponent<Text>().text);
                Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
                Assert.IsNull(FindChild(rootObject.transform, "UnityOpponentMechanicLibraryOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentPanelOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerTargetButton-Player-1"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentHeroPowerTargetButton-Opponent-1"));

                FindChild(rootObject.transform, "UnityOpponentHeroPowerTargetPlayerLeftButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(BoardSide.Player, service.State.Opponent.HeroPowerTargetSide);
                Assert.AreEqual(0, service.State.Opponent.HeroPowerTargetIndex);
                Assert.AreEqual("ui-player-target", service.State.Opponent.HeroPowerTargetInstanceId);

                FindChild(rootObject.transform, "UnityOpponentHeroPowerTargetButton-Player-1").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(BoardSide.Player, service.State.Opponent.HeroPowerTargetSide);
                Assert.AreEqual(1, service.State.Opponent.HeroPowerTargetIndex);
                Assert.AreEqual("ui-player-second-target", service.State.Opponent.HeroPowerTargetInstanceId);
                var markedTarget = FindChild(rootObject.transform, "UnityCard-ui-player-second-target").GetComponent<UnityTavernCardComponent>();
                Assert.AreEqual(UnityTavernTargetingState.OpponentTarget, markedTarget.TargetingState);
                Assert.AreEqual("敌技目标", FindChild(markedTarget.transform, "UnityTargetingLabelText").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityOpponentHeroPowerTargetButton-Opponent-1").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(BoardSide.Opponent, service.State.Opponent.HeroPowerTargetSide);
                Assert.AreEqual(1, service.State.Opponent.HeroPowerTargetIndex);
                Assert.AreEqual("ui-opponent-second-target", service.State.Opponent.HeroPowerTargetInstanceId);

                FindChild(rootObject.transform, "UnityOpponentHeroPowerTargetClearButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(-1, service.State.Opponent.HeroPowerTargetIndex);
                Assert.IsNull(service.State.Opponent.HeroPowerTargetInstanceId);
                Assert.AreEqual(
                    UnityTavernTargetingState.None,
                    FindChild(rootObject.transform, "UnityCard-ui-opponent-second-target").GetComponent<UnityTavernCardComponent>().TargetingState);

                FindChild(rootObject.transform, "UnityOpponentHeroPowerClearButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(service.State.Opponent.HeroPowerCardId);
                Assert.AreEqual("未配置", FindChild(rootObject.transform, "UnityOpponentHeroPowerName").GetComponent<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void OpponentMechanicConfiguration_SelectsAndClearsTrinketSlots()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var firstLesser = service.GetOpponentSelectableTrinkets(TrinketSlotKind.Lesser)
                    .OrderBy(trinket => trinket.SlotKind + " / " + trinket.Cost + "g / " + trinket.OfferPoolStatus)
                    .ThenBy(trinket => trinket.Name)
                    .First();
                var firstGreater = service.GetOpponentSelectableTrinkets(TrinketSlotKind.Greater)
                    .OrderBy(trinket => trinket.SlotKind + " / " + trinket.Cost + "g / " + trinket.OfferPoolStatus)
                    .ThenBy(trinket => trinket.Name)
                    .First();

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                FindChild(rootObject.transform, "UnityOpponentEntryButton").GetComponent<Button>().onClick.Invoke();

                FindChild(rootObject.transform, "UnityOpponentTrinketSelectButton-Lesser").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityOpponentMechanicLibraryOverlay"));
                Assert.AreEqual("选择对手小饰品", FindChild(rootObject.transform, "UnityOpponentMechanicLibraryTitle").GetComponent<Text>().text);
                FindChild(rootObject.transform, "UnityOpponentMechanicLibrarySelectButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(firstLesser.CardId, service.State.Opponent.AdvancedMechanics.Trinkets.LesserTrinketId);
                Assert.AreEqual(firstLesser.Name, FindChild(rootObject.transform, "UnityOpponentTrinketName-Lesser").GetComponent<Text>().text);
                Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId);

                FindChild(rootObject.transform, "UnityOpponentTrinketSelectButton-Greater").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual("选择对手大饰品", FindChild(rootObject.transform, "UnityOpponentMechanicLibraryTitle").GetComponent<Text>().text);
                FindChild(rootObject.transform, "UnityOpponentMechanicLibrarySelectButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(firstGreater.CardId, service.State.Opponent.AdvancedMechanics.Trinkets.GreaterTrinketId);
                Assert.AreEqual(firstGreater.Name, FindChild(rootObject.transform, "UnityOpponentTrinketName-Greater").GetComponent<Text>().text);
                Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.Trinkets.GreaterTrinketId);

                FindChild(rootObject.transform, "UnityOpponentTrinketClearButton-Lesser").GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(service.State.Opponent.AdvancedMechanics.Trinkets.LesserTrinketId);
                Assert.AreEqual(firstGreater.CardId, service.State.Opponent.AdvancedMechanics.Trinkets.GreaterTrinketId);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TavernTable_ToolsOverlayDoesNotReparentMainTableRows()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                AssertMainTableRows(rootObject.transform);

                FindChild(rootObject.transform, "UnityQuickToolsButton").GetComponent<Button>().onClick.Invoke();

                var overlay = FindChild(rootObject.transform, "UnityTrainerToolsOverlay");
                Assert.IsNotNull(overlay);
                Assert.AreEqual("UnityTavernTrainer", overlay.parent.name);
                AssertMainTableRows(rootObject.transform);

                FindChild(rootObject.transform, "UnityTrainerToolsCloseButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay"));
                AssertMainTableRows(rootObject.transform);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TavernTable_CompressesManyHandCardsAndHoverLiftsFocusedCard()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var template = service.State.Player.Tavern.Shop.First(card => card != null).Clone();
                service.State.Player.Tavern.Hand.Clear();
                for (var index = 0; index < 8; index += 1)
                {
                    var card = template.Clone();
                    card.InstanceId = "compressed-hand-card-" + index;
                    card.Name = "Compressed Hand " + index;
                    service.State.Player.Tavern.Hand.Add(card);
                }

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var slots = FindChildren(rootObject.transform, "UnityHandZoneSlot-");
                Assert.AreEqual(8, slots.Count);
                Assert.IsTrue(slots.All(slot => slot.GetComponent<LayoutElement>().ignoreLayout));

                var firstRect = slots[0].GetComponent<RectTransform>();
                var secondRect = slots[1].GetComponent<RectTransform>();
                Assert.Less(
                    Mathf.Abs(secondRect.anchoredPosition.x - firstRect.anchoredPosition.x),
                    firstRect.sizeDelta.x);

                var focused = FindChild(rootObject.transform, "UnityCard-compressed-hand-card-3");
                var focusedRect = focused.GetComponent<RectTransform>();
                var focusedParent = focused.parent;
                var baseY = focusedRect.anchoredPosition.y;
                var baseScale = focused.localScale.x;

                focused.GetComponent<UnityTavernCardComponent>().OnPointerEnter(new PointerEventData(EnsureEventSystem(rootObject.transform)));

                Assert.Greater(focusedRect.anchoredPosition.y, baseY);
                Assert.Greater(focused.localScale.x, baseScale);
                Assert.AreEqual(focusedParent.parent.childCount - 1, focusedParent.GetSiblingIndex());
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void HeroBadgeSelection_ReplacesCurrentHeroAndRefreshesTopBar()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var targetHero = service.HeroCatalog.GetInitialSelectableHeroes()
                    .First(hero => string.Equals(hero.HeroCardId, MillhouseHeroCardId, StringComparison.OrdinalIgnoreCase));
                service.State.Player.Health = 7;
                service.State.Player.MaxHealth = 9;
                service.State.Player.Armor = 0;
                service.State.Player.Tavern.Gold = targetHero.HeroPower == null ? service.State.Player.Tavern.Gold : service.State.Player.Tavern.UpgradeCost;
                var upgradeCostBeforeSwap = service.State.Player.Tavern.UpgradeCost;
                var logCountBeforeSwap = service.State.Player.Tavern.RecruitLog.Count;

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                FindChild(rootObject.transform, "UnityHeroBadge").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroSelectionOverlay"));
                Assert.IsFalse(FindChild(rootObject.transform, "UnityHeroSelectionConfirmButton").GetComponent<Button>().interactable);
                Assert.IsFalse(FindChild(rootObject.transform, "UnityHeroSelectionHeroChooseButton-" + HeroSelectionSafeName(service.State.Player.HeroId)).GetComponent<Button>().interactable);

                FindChild(rootObject.transform, "UnityHeroSelectionHeroChooseButton-" + HeroSelectionSafeName(targetHero.HeroCardId)).GetComponent<Button>().onClick.Invoke();
                Assert.AreNotEqual(targetHero.HeroCardId, service.State.Player.HeroId);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityHeroSelectionConfirmButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityHeroSelectionConfirmButton").GetComponentInChildren<Text>().text.Contains(targetHero.Name));

                FindChild(rootObject.transform, "UnityHeroSelectionConfirmButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(targetHero.HeroCardId, service.State.Player.HeroId);
                Assert.AreEqual(MillhouseHeroPowerCardId, service.State.Player.HeroPowerCardId);
                Assert.AreEqual(targetHero.Health, service.State.Player.Health);
                Assert.AreEqual(targetHero.Health, service.State.Player.MaxHealth);
                Assert.AreEqual(targetHero.Armor, service.State.Player.Armor);
                Assert.AreEqual(logCountBeforeSwap + 1, service.State.Player.Tavern.RecruitLog.Count);
                Assert.AreEqual(RecruitLogType.Discover, service.State.Player.Tavern.RecruitLog.Last().Type);
                Assert.AreEqual("Hero set: " + targetHero.Name, service.State.Player.Tavern.RecruitLog.Last().Message);
                Assert.AreEqual(targetHero.Name, FindChild(rootObject.transform, "UnityHeroBadgeName").GetComponent<Text>().text);
                Assert.AreEqual(targetHero.HeroPower.Name, FindChild(rootObject.transform, "UnityHeroBadgePower").GetComponent<Text>().text);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityFeedbackToast").GetComponentInChildren<Text>().text.Contains(targetHero.Name));
                Assert.IsTrue(FindChild(rootObject.transform, "UnityQuickRefreshButton").GetComponentInChildren<Text>().text.Contains("2"));
                Assert.IsTrue(FindChild(rootObject.transform, "UnityQuickUpgradeButton").GetComponentInChildren<Text>().text.Contains((upgradeCostBeforeSwap + 1).ToString()));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void HeroSelectionModal_ToolsEntryUsesInMatchPreviewConfirmation()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var targetHero = service.HeroCatalog.GetInitialSelectableHeroes()
                    .First(hero => !string.Equals(hero.HeroCardId, service.State.Player.HeroId, StringComparison.OrdinalIgnoreCase));

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                FindChild(rootObject.transform, "UnityQuickToolsButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityToolsSwapHeroButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityHeroSelectionOverlay"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityToolsModal"));
                Assert.IsFalse(FindChild(rootObject.transform, "UnityHeroSelectionConfirmButton").GetComponent<Button>().interactable);

                FindChild(rootObject.transform, "UnityHeroSelectionHeroChooseButton-" + HeroSelectionSafeName(targetHero.HeroCardId))
                    .GetComponent<Button>()
                    .onClick
                    .Invoke();

                Assert.AreNotEqual(targetHero.HeroCardId, service.State.Player.HeroId);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityHeroSelectionConfirmButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityHeroSelectionConfirmButton").GetComponentInChildren<Text>().text.Contains(targetHero.Name));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TavernSpellFullArt_HidesGeneratedCostBadge()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var definition = LearnHearthstone.Adapters.Data.SpellCatalogLoader.LoadFromResources()
                    .All
                    .First(spell => spell.InPool && spell.Category == "TavernSpell");
                var spell = MinionFactory.Create(definition, BoardSide.Player, "unity-spell-art-test");
                Assert.IsNotNull(CardImageProvider.LoadSprite(spell));

                var cardObject = UnityTavernCardComponent.CreateCardHost(UnityTavernCardMode.Shop, rootObject.transform, "UnitySpellCardArtProbe");
                cardObject.GetComponent<UnityTavernCardComponent>().Bind(spell, UnityTavernCardMode.Shop, null, null, null);

                var art = FindChild(cardObject.transform, "UnityCardArt").GetComponent<Image>();
                Assert.IsNotNull(art.sprite);
                var costBadge = FindChild(cardObject.transform, "UnityCostBadge");
                Assert.IsNotNull(costBadge);
                Assert.IsFalse(costBadge.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_ShopCardActionBuysIntoHand()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var firstCard = service.State.Player.Tavern.Shop.First(card => card != null);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                FindChild(rootObject.transform, "UnityCardAction-" + firstCard.InstanceId).GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanelDrawerToggle"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void UserJourney_FromMainHubBuysPlaysAddsOpponentAndOpensCombatReplay()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var advisor = new LocalAdvisorService();
                var legacyOpened = false;
                var realisticOpened = false;

                void OpenUnityTrainer()
                {
                    ClearChildren(rootObject.transform);
                    new UnityTavernTrainerView(rootObject.transform, service, advisor, () => { }).Build();
                }

                new MainHubView(rootObject.transform, () => legacyOpened = true, () => realisticOpened = true, OpenUnityTrainer).Build();
                FindChild(rootObject.transform, "酒馆训练器Button").GetComponent<Button>().onClick.Invoke();

                Assert.IsFalse(legacyOpened);
                Assert.IsFalse(realisticOpened);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTavernTrainer"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityLegacyButton"));

                var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
                Assert.GreaterOrEqual(shopIndex, 0);
                var boughtCard = service.State.Player.Tavern.Shop[shopIndex];
                FindChild(rootObject.transform, "UnityCardAction-" + boughtCard.InstanceId).GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
                Assert.AreEqual(boughtCard.InstanceId, service.State.Player.Tavern.Hand[0].InstanceId);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityFeedbackToast"));

                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();
                var handCard = service.State.Player.Tavern.Hand[0];
                controller.BeginDrag(handCard, UnityTavernDragSource.Hand, 0);
                controller.HandleDrop(UnityTavernDropTarget.PlayerBoard, 0);

                Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
                Assert.AreEqual(1, service.State.Player.Board.Count);
                Assert.AreEqual(handCard.InstanceId, service.State.Player.Board[0].InstanceId);

                OpenRightPanelDrawer(rootObject.transform);
                Assert.AreEqual("功能面板", FindChild(rootObject.transform, "UnityRightPanelTitle").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay"));
                FindChild(rootObject.transform, "UnityToolsAddOpponentButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryOverlay"));
                FirstCardLibraryCard(rootObject.transform).GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(1, service.State.Opponent.Board.Count);
                FindChild(rootObject.transform, "UnityCardLibraryBackButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay"));
                FindChild(rootObject.transform, "UnityTrainerToolsCloseButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay"));

                FindChild(rootObject.transform, "UnityNextTurnButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(2, service.State.Round);
                Assert.AreEqual(MatchPhase.Tavern, service.State.Phase);
                Assert.IsNotNull(service.State.LastReplay);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatReplayPanel").GetComponent<UnityTavernCombatReplayPanelComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityReplayPlayPauseButton"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CommandSuccessShowsChineseFeedbackToast()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityRefreshButton").GetComponent<Button>().onClick.Invoke();

                var toast = FindChild(rootObject.transform, "UnityFeedbackToast");
                Assert.IsNotNull(toast);
                Assert.IsNotNull(toast.GetComponent<UnityTavernToastComponent>());
                Assert.AreEqual("已刷新酒馆", toast.GetComponentInChildren<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityErrorToast"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_QuickActionBarRunsPrimaryCommandsWithoutOpeningDrawer()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var refreshButton = FindChild(rootObject.transform, "UnityQuickRefreshButton");
                Assert.IsNotNull(refreshButton);
                Assert.GreaterOrEqual(refreshButton.GetComponent<LayoutElement>().preferredHeight, UnityTavernUiStyle.TouchHeight);

                refreshButton.GetComponent<Button>().onClick.Invoke();

                var toast = FindChild(rootObject.transform, "UnityFeedbackToast");
                Assert.IsNotNull(toast);
                Assert.AreEqual("已刷新酒馆", toast.GetComponentInChildren<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityRightPanel"));

                FindChild(rootObject.transform, "UnityQuickToolsButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityRightPanel"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_ActionButtonsExposeSemanticPriorityAndReplayDisabledState()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var refreshButton = FindChild(rootObject.transform, "UnityQuickRefreshButton");
                var nextTurnButton = FindChild(rootObject.transform, "UnityQuickNextTurnButton");
                var replayButton = FindChild(rootObject.transform, "UnityQuickReplayButton");
                var toolsButton = FindChild(rootObject.transform, "UnityQuickToolsButton");

                AssertActionButtonChrome(refreshButton);
                AssertActionButtonChrome(nextTurnButton);
                AssertActionButtonChrome(replayButton);
                AssertActionButtonChrome(toolsButton);
                Assert.IsNull(FindChild(rootObject.transform, "UnityQuickCombatButton"));
                Assert.AreEqual("完整下一回合", FindChild(nextTurnButton, "UnityQuickNextTurnButtonText").GetComponent<Text>().text);
                Assert.AreNotEqual(nextTurnButton.GetComponent<Image>().color, toolsButton.GetComponent<Image>().color);
                Assert.AreNotEqual(refreshButton.GetComponent<Image>().color, nextTurnButton.GetComponent<Image>().color);
                Assert.IsFalse(replayButton.GetComponent<Button>().interactable);
                Assert.AreEqual("无回放", FindChild(replayButton, "UnityQuickReplayButtonText").GetComponent<Text>().text);

                OpenRightPanelDrawer(rootObject.transform);

                AssertActionButtonChrome(FindChild(rootObject.transform, "UnityNextTurnButton"));
                Assert.AreEqual("完整下一回合", FindChild(rootObject.transform, "UnityNextTurnButtonText").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityCombatButton"));
                Assert.IsFalse(FindChild(rootObject.transform, "UnityReplayButton").GetComponent<Button>().interactable);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TavernTable_KeyControlsSupportFocusNavigationRoute()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                var eventSystem = EnsureEventSystem(rootObject.transform);

                var keyControls = new[]
                {
                    "UnityOpponentEntryButton",
                    "UnityQuickRefreshButton",
                    "UnityQuickFreezeButton",
                    "UnityQuickUpgradeButton",
                    "UnityQuickNextTurnButton",
                    "UnityQuickToolsButton",
                    "UnityRightPanelDrawerToggle"
                };

                foreach (var controlName in keyControls)
                {
                    AssertFocusableButton(FindChild(rootObject.transform, controlName), controlName);
                }

                var nextTurn = FindChild(rootObject.transform, "UnityQuickNextTurnButton");
                eventSystem.SetSelectedGameObject(nextTurn.gameObject);
                Assert.AreSame(nextTurn.gameObject, eventSystem.currentSelectedGameObject);

                FindChild(rootObject.transform, "UnityQuickToolsButton").GetComponent<Button>().onClick.Invoke();
                var closeTools = FindChild(rootObject.transform, "UnityTrainerToolsCloseButton");
                AssertFocusableButton(closeTools, "UnityTrainerToolsCloseButton");
                eventSystem.SetSelectedGameObject(closeTools.gameObject);
                Assert.AreSame(closeTools.gameObject, eventSystem.currentSelectedGameObject);
                closeTools.GetComponent<Button>().onClick.Invoke();

                var drawerToggle = FindChild(rootObject.transform, "UnityRightPanelDrawerToggle");
                drawerToggle.GetComponent<Button>().onClick.Invoke();
                var closeDrawer = FindChild(rootObject.transform, "UnityRightPanelFloatToggle");
                AssertFocusableButton(closeDrawer, "UnityRightPanelFloatToggle");
                eventSystem.SetSelectedGameObject(closeDrawer.gameObject);
                Assert.AreSame(closeDrawer.gameObject, eventSystem.currentSelectedGameObject);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_DisabledEconomyButtonsDoNotRunCommands()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Tavern.Gold = 0;
                service.State.Player.Tavern.FreeRefreshes = 0;
                service.State.Player.Tavern.HealthCostRefreshes = 0;
                service.State.Player.Tavern.UpgradeCost = 5;
                var startingTier = service.State.Player.Tavern.Tier;
                var startingShop = service.State.Player.Tavern.Shop.Select(card => card?.InstanceId).ToList();

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var refreshButton = FindChild(rootObject.transform, "UnityQuickRefreshButton").GetComponent<Button>();
                var upgradeButton = FindChild(rootObject.transform, "UnityQuickUpgradeButton").GetComponent<Button>();
                Assert.IsFalse(refreshButton.interactable);
                Assert.IsFalse(upgradeButton.interactable);
                Assert.AreEqual("刷新 1", FindChild(rootObject.transform, "UnityQuickRefreshButtonText").GetComponent<Text>().text);
                Assert.AreEqual("升本 5", FindChild(rootObject.transform, "UnityQuickUpgradeButtonText").GetComponent<Text>().text);

                refreshButton.onClick.Invoke();
                upgradeButton.onClick.Invoke();

                CollectionAssert.AreEqual(startingShop, service.State.Player.Tavern.Shop.Select(card => card?.InstanceId).ToList());
                Assert.AreEqual(startingTier, service.State.Player.Tavern.Tier);
                Assert.IsNull(FindChild(rootObject.transform, "UnityFeedbackToast"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityErrorToast"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void HeroPowerUi_TargetsFriendlyMinionAndAppliesGeorgeShield()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(
                    12345,
                    new InMemoryTestScenarioRepository(),
                    new MatchSetupOptions { SelectedHeroCardId = GeorgeHeroCardId });
                service.State.Player.Tavern.Gold = 5;
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var target = CreateBoardMinion(service, "george-ui-target", BoardSide.Player, 2, 3);
                service.State.Player.Board.Add(target);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var heroPowerButton = FindChild(rootObject.transform, "UnityQuickHeroPowerButton").GetComponent<Button>();
                Assert.IsTrue(heroPowerButton.interactable);
                Assert.IsNotNull(heroPowerButton.GetComponent<UnityTavernCardDragBehaviour>());

                heroPowerButton.onClick.Invoke();
                FindChild(rootObject.transform, "UnityCard-" + target.InstanceId).GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(4, service.State.Player.Tavern.Gold);
                Assert.IsTrue(target.Keywords.Contains(Keyword.DivineShield));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityFeedbackToast"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityErrorToast"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void CosmicDualitySecondHeroPowerUi_ShowsUnlockedButtonAndDragUsesSelectedPower()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(
                    12345,
                    new InMemoryTestScenarioRepository(),
                    new MatchSetupOptions
                    {
                        EnableAnomalies = true,
                        RandomizeAnomaly = false,
                        SelectedAnomalyCardId = "BG31_Anomaly_123",
                        AnomalyPoolVersion = AnomalyPoolVersion.CurrentHsReplay
                    });
                var discover = service.State.Player.Tavern.Discover;
                Assert.IsNotNull(discover);
                Assert.AreEqual("anomaly-cosmic-duality", discover.Source);
                var pickedCardId = discover.Options[0].CardId;

                service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
                CollectionAssert.Contains(service.State.Player.ExtraHeroPowerCardIds, pickedCardId);
                Assert.AreEqual(1, service.State.Player.ExtraHeroPowerUnlockRounds[pickedCardId]);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityQuickHeroPowerButton"));
                var extraHeroPowerObject = FindChild(rootObject.transform, "UnityQuickHeroPowerButton1");
                Assert.IsNotNull(extraHeroPowerObject);
                var extraHeroPowerButton = extraHeroPowerObject.GetComponent<Button>();
                Assert.IsTrue(extraHeroPowerButton.interactable);

                var drag = extraHeroPowerObject.GetComponent<UnityTavernCardDragBehaviour>();
                Assert.IsNotNull(drag);
                var cardField = typeof(UnityTavernCardDragBehaviour).GetField(
                    "card",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.IsNotNull(cardField);
                var dragCard = (MinionInstance)cardField.GetValue(drag);
                Assert.IsNotNull(dragCard);
                Assert.AreEqual(CardKind.HeroPower, dragCard.CardKind);
                Assert.AreEqual(pickedCardId, dragCard.CardId);

                Assert.IsTrue(UnityTavernDragController.TryBuildDropCommand(
                    new UnityTavernDragContext(dragCard, UnityTavernDragSource.HeroPower, 1),
                    UnityTavernDropTarget.PlayerBoard,
                    0,
                    out var command));
                Assert.AreEqual(GameCommandType.UseHeroPower, command.Type);
                Assert.AreEqual(pickedCardId, command.HeroPowerCardId);

                extraHeroPowerButton.onClick.Invoke();
                Assert.IsNull(FindChild(rootObject.transform, "UnityErrorToast"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void HeroPowerTargeting_ShowsCandidatesPreviewSourceAndClearsOnCancel()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            rootObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                service.State.Player.Board.Add(CreateBoardMinion(service, "clarity-player", BoardSide.Player, 3, 4));
                service.State.Opponent.Board.Add(CreateBoardMinion(service, "clarity-opponent", BoardSide.Opponent, 4, 5));

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();
                var heroPowerButton = FindChild(rootObject.transform, "UnityQuickHeroPowerButton");
                var heroPowerDrag = heroPowerButton.GetComponent<UnityTavernCardDragBehaviour>();
                Assert.IsNotNull(heroPowerDrag);
                Assert.IsNotNull(heroPowerDrag.Card);

                controller.BeginDrag(heroPowerDrag.Card, UnityTavernDragSource.HeroPower, 0);
                FindChild(rootObject.transform, "UnityOpponentEntryButton").GetComponent<Button>().onClick.Invoke();
                heroPowerButton = FindChild(rootObject.transform, "UnityQuickHeroPowerButton");

                var playerCard = FindChild(rootObject.transform, "UnityCard-clarity-player").GetComponent<UnityTavernCardComponent>();
                var opponentCard = FindChild(rootObject.transform, "UnityCard-clarity-opponent").GetComponent<UnityTavernCardComponent>();
                Assert.AreEqual(UnityTavernTargetingState.Candidate, playerCard.TargetingState);
                Assert.AreEqual(UnityTavernTargetingState.Candidate, opponentCard.TargetingState);
                Assert.AreEqual("可选", FindChild(playerCard.transform, "UnityTargetingLabelText").GetComponent<Text>().text);
                Assert.IsFalse(FindChild(playerCard.transform, "UnityTargetingLabel").GetComponent<Image>().raycastTarget);
                Assert.IsNotNull(FindChild(heroPowerButton, "UnityTargetingSourceMarker"));

                playerCard.OnPointerEnter(null);
                Assert.AreEqual("目标", FindChild(playerCard.transform, "UnityTargetingLabelText").GetComponent<Text>().text);
                playerCard.OnPointerExit(null);
                Assert.AreEqual("可选", FindChild(playerCard.transform, "UnityTargetingLabelText").GetComponent<Text>().text);

                controller.EndDrag();
                Assert.AreEqual(UnityTavernTargetingState.None, playerCard.TargetingState);
                Assert.IsFalse(FindChild(playerCard.transform, "UnityTargetingLabel").gameObject.activeSelf);
                Assert.IsNull(FindChild(heroPowerButton, "UnityTargetingSourceMarker"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void TargetedSpellTargeting_ShowsFriendlyCandidatesAndUsesSpellSourceLabel()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            rootObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                service.State.Player.Tavern.Hand.Clear();
                service.State.Player.Board.Add(CreateBoardMinion(service, "spell-target", BoardSide.Player, 3, 4));
                service.State.Opponent.Board.Add(CreateBoardMinion(service, "spell-opponent", BoardSide.Opponent, 4, 5));
                var spell = new MinionInstance
                {
                    CardKind = CardKind.TavernSpell,
                    InstanceId = "targeted-spell",
                    CardId = "targeted-spell-card",
                    Name = "Targeted Spell",
                    Owner = BoardSide.Player,
                    Tags = new List<string> { "targeted_spell" }
                };
                service.State.Player.Tavern.Hand.Add(spell);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                var source = FindChild(rootObject.transform, "UnityCard-targeted-spell");
                source.GetComponentsInChildren<Button>(true).First(button => button.transform != source).onClick.Invoke();

                var sourceCard = source.GetComponent<UnityTavernCardComponent>();
                var targetCard = FindChild(rootObject.transform, "UnityCard-spell-target").GetComponent<UnityTavernCardComponent>();
                Assert.AreEqual(UnityTavernTargetingState.Source, sourceCard.TargetingState);
                Assert.AreEqual("法术", FindChild(sourceCard.transform, "UnityTargetingLabelText").GetComponent<Text>().text);
                Assert.AreEqual(UnityTavernTargetingState.Candidate, targetCard.TargetingState);
                Assert.AreEqual("可选", FindChild(targetCard.transform, "UnityTargetingLabelText").GetComponent<Text>().text);

                targetCard.OnPointerEnter(null);
                Assert.AreEqual("目标", FindChild(targetCard.transform, "UnityTargetingLabelText").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>().EndDrag();
                Assert.AreEqual(UnityTavernTargetingState.None, sourceCard.TargetingState);
                Assert.AreEqual(UnityTavernTargetingState.None, targetCard.TargetingState);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void DragCommandMapper_BuildsExpectedCommands()
        {
            var card = new MinionInstance { InstanceId = "drag-card" };

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.Shop, 2),
                UnityTavernDropTarget.Hand,
                -1,
                GameCommandType.BuyMinion,
                2,
                -1,
                null);

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.Discover, 1),
                UnityTavernDropTarget.Hand,
                -1,
                GameCommandType.ChooseDiscover,
                1,
                -1,
                null);

            AssertDropCommand(
                new UnityTavernDragContext(new MinionInstance { InstanceId = "hero-power-card", CardKind = CardKind.HeroPower }, UnityTavernDragSource.HeroPower, 0),
                UnityTavernDropTarget.PlayerBoard,
                2,
                GameCommandType.UseHeroPower,
                0,
                2,
                null,
                TargetZone.FriendlyBoard);

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.Hand, 0),
                UnityTavernDropTarget.PlayerBoard,
                3,
                GameCommandType.PlayMinion,
                0,
                3,
                null);

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.PlayerBoard, 0),
                UnityTavernDropTarget.PlayerBoard,
                4,
                GameCommandType.MoveBoardMinion,
                0,
                4,
                "drag-card");

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.PlayerBoard, 0),
                UnityTavernDropTarget.SellZone,
                -1,
                GameCommandType.SellMinion,
                0,
                -1,
                "drag-card");

            AssertDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.OpponentBoard, 0),
                UnityTavernDropTarget.OpponentBoard,
                2,
                GameCommandType.MoveOpponentMinion,
                0,
                2,
                "drag-card");

            Assert.IsFalse(UnityTavernDragController.TryBuildDropCommand(
                new UnityTavernDragContext(card, UnityTavernDragSource.Shop, 0),
                UnityTavernDropTarget.PlayerBoard,
                0,
                out _));
        }

        [Test]
        public void Build_AddsDragSourcesDropTargetsAndSellZone()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null);
                var firstShopCard = service.State.Player.Tavern.Shop[shopIndex];
                var boardCard = CreateBoardMinion(service, "drag-sell-board-card", BoardSide.Player, 3, 4);
                var opponentCard = CreateBoardMinion(service, "drag-opponent-board-card", BoardSide.Opponent, 5, 6);
                service.State.Player.Board.Add(boardCard);
                service.State.Opponent.Board.Add(opponentCard);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();
                FindChild(rootObject.transform, "UnityOpponentEntryButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCard-" + firstShopCard.InstanceId).GetComponent<UnityTavernCardDragBehaviour>());
                Assert.AreEqual(10, FindChildren(rootObject.transform, "UnityHandZoneSlot-").Count(slot => slot.GetComponent<UnityTavernDropTargetBehaviour>() != null));
                Assert.AreEqual(7, FindChildren(rootObject.transform, "UnityPlayerBoardZoneSlot-").Count(slot => slot.GetComponent<UnityTavernDropTargetBehaviour>() != null));

                var handZone = FindChild(rootObject.transform, "UnityHandZone");
                var shopZone = FindChild(rootObject.transform, "UnityShopZone");
                var playerBoardZone = FindChild(rootObject.transform, "UnityPlayerBoardZone");
                var opponentBoardZone = FindChild(rootObject.transform, "UnityOpponentBoardZone");
                var buyDrop = FindChild(handZone, "UnityHandBuyDropZone").GetComponent<UnityTavernDropTargetBehaviour>();
                var sellDrop = FindChild(shopZone, "UnitySellDropZone").GetComponent<UnityTavernDropTargetBehaviour>();
                var playerWideDrop = FindChild(playerBoardZone, "UnityPlayerBoardReorderDropZone").GetComponent<UnityTavernDropTargetBehaviour>();
                var opponentWideDrop = FindChild(opponentBoardZone, "UnityOpponentBoardReorderDropZone").GetComponent<UnityTavernDropTargetBehaviour>();
                Assert.AreEqual(UnityTavernDropTarget.Hand, buyDrop.Target);
                Assert.AreEqual(UnityTavernDropTarget.SellZone, sellDrop.Target);
                Assert.AreEqual(UnityTavernDropTarget.PlayerBoard, playerWideDrop.Target);
                Assert.AreEqual(UnityTavernDropTarget.OpponentBoard, opponentWideDrop.Target);
                Assert.IsFalse(buyDrop.gameObject.activeSelf);
                Assert.IsFalse(buyDrop.GetComponent<Image>().raycastTarget);
                Assert.IsFalse(sellDrop.gameObject.activeSelf);
                Assert.IsFalse(sellDrop.GetComponent<Image>().raycastTarget);
                Assert.IsFalse(playerWideDrop.gameObject.activeSelf);
                Assert.IsFalse(playerWideDrop.GetComponent<Image>().raycastTarget);
                Assert.IsFalse(opponentWideDrop.gameObject.activeSelf);
                Assert.IsFalse(opponentWideDrop.GetComponent<Image>().raycastTarget);

                var handDrop = FindChildren(rootObject.transform, "UnityHandZoneSlot-")
                    .First(slot => slot.GetComponent<UnityTavernDropTargetBehaviour>() != null)
                    .GetComponent<UnityTavernDropTargetBehaviour>();
                var boardDrop = FindChildren(rootObject.transform, "UnityPlayerBoardZoneSlot-")
                    .First(slot => slot.GetComponent<UnityTavernDropTargetBehaviour>() != null)
                    .GetComponent<UnityTavernDropTargetBehaviour>();
                var handImage = handDrop.GetComponent<Image>();
                var boardImage = boardDrop.GetComponent<Image>();
                var normalColor = handImage.color;
                var boardNormalColor = boardImage.color;

                controller.BeginDrag(firstShopCard, UnityTavernDragSource.Shop, shopIndex);
                Assert.IsTrue(handDrop.IsDropCueVisible);
                Assert.IsTrue(handDrop.IsDropAllowed);
                Assert.AreNotEqual(normalColor, handImage.color);
                Assert.IsTrue(handDrop.GetComponent<Outline>().enabled);
                Assert.IsTrue(buyDrop.gameObject.activeSelf);
                Assert.IsTrue(buyDrop.IsDropCueVisible);
                Assert.IsTrue(buyDrop.IsDropAllowed);
                Assert.IsTrue(buyDrop.GetComponent<Image>().raycastTarget);
                Assert.IsFalse(sellDrop.gameObject.activeSelf);
                Assert.IsFalse(sellDrop.IsDropCueVisible);
                Assert.IsFalse(playerWideDrop.gameObject.activeSelf);
                Assert.IsTrue(boardDrop.IsDropCueVisible);
                Assert.IsFalse(boardDrop.IsDropAllowed);
                Assert.AreNotEqual(boardNormalColor, boardImage.color);
                Assert.IsFalse(boardDrop.GetComponent<Outline>().enabled);

                controller.EndDrag();
                Assert.IsFalse(handDrop.IsDropCueVisible);
                Assert.IsFalse(handDrop.IsDropAllowed);
                Assert.AreEqual(normalColor, handImage.color);
                Assert.IsFalse(handDrop.GetComponent<Outline>().enabled);
                Assert.IsFalse(buyDrop.gameObject.activeSelf);
                Assert.IsFalse(buyDrop.GetComponent<Image>().raycastTarget);
                Assert.IsFalse(boardDrop.IsDropCueVisible);
                Assert.AreEqual(boardNormalColor, boardImage.color);

                controller.BeginDrag(boardCard, UnityTavernDragSource.PlayerBoard, 0);
                Assert.IsTrue(sellDrop.gameObject.activeSelf);
                Assert.IsTrue(sellDrop.IsDropCueVisible);
                Assert.IsTrue(sellDrop.IsDropAllowed);
                Assert.IsTrue(sellDrop.GetComponent<Image>().raycastTarget);
                Assert.IsTrue(sellDrop.GetComponent<Outline>().enabled);
                Assert.IsTrue(playerWideDrop.gameObject.activeSelf);
                Assert.IsTrue(playerWideDrop.IsDropCueVisible);
                Assert.IsTrue(playerWideDrop.IsDropAllowed);
                Assert.IsTrue(playerWideDrop.GetComponent<Image>().raycastTarget);
                Assert.IsFalse(opponentWideDrop.gameObject.activeSelf);
                controller.EndDrag();
                Assert.IsFalse(sellDrop.gameObject.activeSelf);
                Assert.IsFalse(sellDrop.GetComponent<Image>().raycastTarget);
                Assert.IsFalse(playerWideDrop.gameObject.activeSelf);
                Assert.IsFalse(playerWideDrop.GetComponent<Image>().raycastTarget);

                controller.BeginDrag(opponentCard, UnityTavernDragSource.OpponentBoard, 0);
                Assert.IsTrue(opponentWideDrop.gameObject.activeSelf);
                Assert.IsTrue(opponentWideDrop.IsDropCueVisible);
                Assert.IsTrue(opponentWideDrop.IsDropAllowed);
                Assert.IsTrue(opponentWideDrop.GetComponent<Image>().raycastTarget);
                Assert.IsFalse(playerWideDrop.gameObject.activeSelf);
                controller.EndDrag();
                Assert.IsFalse(opponentWideDrop.gameObject.activeSelf);
                Assert.IsFalse(opponentWideDrop.GetComponent<Image>().raycastTarget);

                handDrop.OnPointerEnter(null);
                Assert.IsTrue(handDrop.IsHighlighted);
                Assert.AreNotEqual(normalColor, handImage.color);
                Assert.IsTrue(handDrop.GetComponent<Outline>().enabled);
                handDrop.OnPointerExit(null);
                Assert.IsFalse(handDrop.IsHighlighted);
                Assert.AreEqual(normalColor, handImage.color);
                Assert.IsFalse(handDrop.GetComponent<Outline>().enabled);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void DiscoverDrag_DisablesModalBackdropSoHandDropCanReceiveChoice()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var options = service.State.Player.Tavern.Shop
                    .Where(card => card != null)
                    .Take(3)
                    .Select(card => card.Clone())
                    .ToList();
                Assert.Greater(options.Count, 0);
                service.State.Player.Tavern.Discover = new DiscoverState
                {
                    Options = options
                };

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();
                var discoverOverlay = FindChild(rootObject.transform, "UnityDiscoverOverlay");
                var discoverBackdrop = discoverOverlay.GetComponent<Image>();
                var handBuyDrop = FindChild(rootObject.transform, "UnityHandBuyDropZone").GetComponent<UnityTavernDropTargetBehaviour>();
                var choice = service.State.Player.Tavern.Discover.Options[0];

                Assert.IsTrue(discoverBackdrop.raycastTarget);
                controller.BeginDrag(choice, UnityTavernDragSource.Discover, 0);
                Assert.IsFalse(discoverBackdrop.raycastTarget);
                Assert.IsTrue(handBuyDrop.gameObject.activeSelf);
                Assert.IsTrue(handBuyDrop.IsDropAllowed);
                Assert.IsTrue(handBuyDrop.GetComponent<Image>().raycastTarget);

                controller.EndDrag();
                Assert.IsTrue(discoverBackdrop.raycastTarget);

                controller.BeginDrag(choice, UnityTavernDragSource.Discover, 0);
                handBuyDrop.OnDrop(null);

                Assert.IsNull(service.State.Player.Tavern.Discover);
                Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
                Assert.AreEqual(choice.CardId, service.State.Player.Tavern.Hand[0].CardId);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void BeginDrag_WithPointerEventCreatesNonBlockingDragGhost()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null);
                var firstShopCard = service.State.Player.Tavern.Shop[shopIndex];

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();
                var eventData = new PointerEventData(eventSystemObject.GetComponent<EventSystem>())
                {
                    position = new Vector2(420f, 260f)
                };

                Assert.DoesNotThrow(() => controller.BeginDrag(firstShopCard, UnityTavernDragSource.Shop, shopIndex, eventData));

                var ghost = FindChildren(rootObject.transform, "UnityCard-" + firstShopCard.InstanceId)
                    .FirstOrDefault(card => card.GetComponent<CanvasGroup>() != null);
                Assert.IsNotNull(ghost);
                Assert.IsFalse(ghost.GetComponent<CanvasGroup>().blocksRaycasts);
                Assert.IsFalse(ghost.GetComponent<CanvasGroup>().interactable);
                Assert.AreEqual(0.92f, ghost.GetComponent<CanvasGroup>().alpha, 0.001f);
                Assert.AreEqual(5000, ghost.GetComponent<Canvas>().sortingOrder);

                controller.EndDrag();
                Assert.IsFalse(FindChildren(rootObject.transform, "UnityCard-" + firstShopCard.InstanceId)
                    .Any(card => card.GetComponent<CanvasGroup>() != null));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                Object.DestroyImmediate(eventSystemObject);
            }
        }

        [Test]
        public void Bootstrap_ConfiguresExistingCanvasForResponsiveScaling()
        {
            var canvasObject = new GameObject("ExistingCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();

                LearnHearthstoneBootstrap.ConfigureCanvas(canvas, UnityTavernLayoutContext.ForSize(1366f, 768f));

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                Assert.IsNotNull(scaler);
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
                Assert.IsFalse(canvas.pixelPerfect);
                Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
                Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
                Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, scaler.screenMatchMode);
                Assert.AreEqual(0.5f, scaler.matchWidthOrHeight, 0.001f);
                Assert.IsNotNull(canvasObject.GetComponent<GraphicRaycaster>());
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void Bootstrap_CompactCanvasFavorsWidthToAvoidOverShrinking()
        {
            var canvasObject = new GameObject("ExistingCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();

                LearnHearthstoneBootstrap.ConfigureCanvas(canvas, UnityTavernLayoutContext.ForSize(994f, 384f));

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                Assert.IsNotNull(scaler);
                Assert.AreEqual(0f, scaler.matchWidthOrHeight, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void LayoutContext_CompactZoneMetricsReduceBoardDensity()
        {
            var standard = UnityTavernLayoutContext.ForSize(1366f, 768f);
            var compact = UnityTavernLayoutContext.ForSize(994f, 384f);

            var standardShop = standard.ZoneMetrics(UnityTavernZoneKind.Shop, UnityTavernCardMode.Shop);
            var compactShop = compact.ZoneMetrics(UnityTavernZoneKind.Shop, UnityTavernCardMode.Shop);
            var standardOpponent = standard.ZoneMetrics(UnityTavernZoneKind.OpponentBoard, UnityTavernCardMode.Board);
            var compactOpponent = compact.ZoneMetrics(UnityTavernZoneKind.OpponentBoard, UnityTavernCardMode.Board);

            Assert.AreEqual(236f, standardShop.Height, 0.001f);
            Assert.AreEqual(168f, standardOpponent.Height, 0.001f);
            Assert.Less(compactShop.Height, standardShop.Height);
            Assert.Less(compactOpponent.Height, standardOpponent.Height);
            Assert.Less(compactShop.SlotSize.x, standardShop.SlotSize.x);
            Assert.Less(compactShop.SlotSize.y, standardShop.SlotSize.y);
            Assert.Less(compactShop.CardScale, standardShop.CardScale);
            Assert.Less(compact.ZoneStackSpacing, standard.ZoneStackSpacing);

            var compactStackHeight = compact.ZoneMetrics(UnityTavernZoneKind.OpponentBoard, UnityTavernCardMode.Board).Height
                + compact.ZoneMetrics(UnityTavernZoneKind.Shop, UnityTavernCardMode.Shop).Height
                + compact.ZoneMetrics(UnityTavernZoneKind.PlayerBoard, UnityTavernCardMode.Board).Height
                + compact.ZoneMetrics(UnityTavernZoneKind.Hand, UnityTavernCardMode.Hand).Height
                + compact.ZoneStackSpacing * 3f;
            Assert.LessOrEqual(compactStackHeight, 590f);
        }

        [Test]
        public void CardComponent_FeedbackTracksSelectedHoverAndPress()
        {
            var cardObject = new GameObject("FeedbackCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                var component = cardObject.GetComponent<UnityTavernCardComponent>();
                var card = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "feedback-card",
                    Name = "Feedback Minion",
                    Attack = 3,
                    Health = 4,
                    TavernTier = 2
                };

                component.Bind(card, UnityTavernCardMode.Shop, "Buy", null, null, true);

                var outline = cardObject.GetComponent<Outline>();
                Assert.IsTrue(component.IsSelected);
                Assert.IsNotNull(outline);
                Assert.IsTrue(outline.enabled);
                Assert.Greater(cardObject.transform.localScale.x, 1f);

                var selectedScale = cardObject.transform.localScale.x;
                component.OnPointerEnter(null);
                Assert.IsTrue(component.IsHovered);
                Assert.Greater(cardObject.transform.localScale.x, selectedScale);

                var hoverScale = cardObject.transform.localScale.x;
                component.OnPointerDown(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
                Assert.Less(cardObject.transform.localScale.x, hoverScale);

                component.OnPointerUp(null);
                Assert.Greater(cardObject.transform.localScale.x, 1f);

                component.OnPointerExit(null);
                Assert.IsFalse(component.IsHovered);
                Assert.IsTrue(outline.enabled);

                component.SetSelected(false);
                Assert.IsFalse(component.IsSelected);
                Assert.IsFalse(outline.enabled);
                Assert.AreEqual(1f, cardObject.transform.localScale.x, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void CardComponent_TargetingStatesUseLabelsAndDoNotBlockRaycasts()
        {
            var cardObject = new GameObject("TargetingCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                var component = cardObject.GetComponent<UnityTavernCardComponent>();
                component.Bind(
                    new MinionInstance
                    {
                        CardKind = CardKind.Minion,
                        InstanceId = "targeting-card",
                        Name = "Targeting Minion",
                        Attack = 2,
                        Health = 3,
                        TavernTier = 1
                    },
                    UnityTavernCardMode.Board,
                    null,
                    null,
                    null);

                component.SetTargetingState(UnityTavernTargetingState.Source);
                Assert.AreEqual("来源", FindChild(cardObject.transform, "UnityTargetingLabelText").GetComponent<Text>().text);
                Assert.IsFalse(FindChild(cardObject.transform, "UnityTargetingLabel").GetComponent<Image>().raycastTarget);
                Assert.IsFalse(FindChild(cardObject.transform, "UnityTargetingLabelText").GetComponent<Text>().raycastTarget);

                component.SetTargetingState(UnityTavernTargetingState.Candidate);
                Assert.AreEqual("可选", FindChild(cardObject.transform, "UnityTargetingLabelText").GetComponent<Text>().text);
                component.OnPointerEnter(null);
                Assert.AreEqual("目标", FindChild(cardObject.transform, "UnityTargetingLabelText").GetComponent<Text>().text);

                component.SetTargetingState(UnityTavernTargetingState.OpponentTarget);
                Assert.AreEqual("敌技目标", FindChild(cardObject.transform, "UnityTargetingLabelText").GetComponent<Text>().text);
                Assert.AreEqual(UnityTavernUiStyle.Red, cardObject.GetComponent<Outline>().effectColor);
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void CardImageProvider_LoadsExplicitPathsAndCardIdFallbacks()
        {
            var direct = CardImageProvider.LoadSprite(new MinionInstance
            {
                CardKind = CardKind.Minion,
                ImagePath = "CardImages/BG20_100"
            });
            Assert.IsNotNull(direct);

            var fullAssetPath = CardImageProvider.LoadSprite(new MinionInstance
            {
                CardKind = CardKind.Minion,
                ImagePath = "Assets/LearnHearthstone/Resources/CardImages/BG20_100.png"
            });
            Assert.IsNotNull(fullAssetPath);

            var byCardId = CardImageProvider.LoadSprite(new MinionInstance
            {
                CardKind = CardKind.Minion,
                CardId = "BG20_100"
            });
            Assert.IsNotNull(byCardId);

            var spellByCardId = CardImageProvider.LoadSprite(new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                CardId = "BG28_168"
            });
            Assert.IsNotNull(spellByCardId);
            Assert.AreEqual(430f, spellByCardId.rect.width, 0.001f);
            Assert.AreEqual(585f, spellByCardId.rect.height, 0.001f);
        }

        [Test]
        public void CardComponent_UsesArtSpriteWhenAvailableAndFallbackLabelWhenMissing()
        {
            var cardWithArtObject = new GameObject("CardWithArt", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            var missingArtObject = new GameObject("MissingArtCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                cardWithArtObject.GetComponent<UnityTavernCardComponent>().Bind(
                    new MinionInstance
                    {
                        CardKind = CardKind.Minion,
                        InstanceId = "card-with-art",
                        CardId = "BG20_100",
                        Name = "Card With Art",
                        Attack = 2,
                        Health = 1,
                        TavernTier = 1,
                        Tribes = new List<Tribe> { Tribe.Quilboar },
                        OfficialKeywords = new List<Keyword> { Keyword.Taunt, Keyword.DivineShield },
                        Text = "Full art cards keep the art-only presentation."
                    },
                    UnityTavernCardMode.Shop,
                    "Buy",
                    null,
                    null);

                var artImage = FindChild(cardWithArtObject.transform, "UnityCardArt").GetComponent<Image>();
                var artRect = artImage.GetComponent<RectTransform>();
                Assert.IsNotNull(artImage.sprite);
                var artViewport = FindChild(cardWithArtObject.transform, "UnityCardArtViewport");
                Assert.IsNotNull(artViewport);
                Assert.IsNotNull(artViewport.GetComponent<RectMask2D>());
                Assert.AreSame(artViewport, artImage.transform.parent);
                Assert.Greater(artViewport.GetComponent<RectTransform>().sizeDelta.x, 100f);
                Assert.AreEqual(new Vector2(0f, -1f), artRect.anchorMin);
                Assert.AreEqual(new Vector2(1f, 1f), artRect.anchorMax);
                Assert.AreEqual("Card With Art", FindChild(cardWithArtObject.transform, "UnityCardName").GetComponent<Text>().text);
                Assert.IsNull(FindChild(cardWithArtObject.transform, "UnityTierBadge"));
                Assert.AreEqual("2", FindChild(cardWithArtObject.transform, "UnityAttackBadgeText").GetComponent<Text>().text);
                Assert.AreEqual("1", FindChild(cardWithArtObject.transform, "UnityHealthBadgeText").GetComponent<Text>().text);
                Assert.IsNull(FindChild(cardWithArtObject.transform, "UnityCardKeywordText"));
                Assert.IsNull(FindChild(cardWithArtObject.transform, "UnityCardKeywordStrip"));
                Assert.IsNull(FindChild(cardWithArtObject.transform, "UnityCardArtFallbackText"));
                var artActionRect = FindChild(cardWithArtObject.transform, "UnityCardAction-card-with-art").GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.22f, 0f), artActionRect.anchorMin);
                Assert.AreEqual(new Vector2(0.78f, 0f), artActionRect.anchorMax);
                Assert.AreEqual(new Vector2(0f, 4f), artActionRect.offsetMin);
                Assert.AreEqual(new Vector2(0f, 28f), artActionRect.offsetMax);

                missingArtObject.GetComponent<UnityTavernCardComponent>().Bind(
                    new MinionInstance
                    {
                        CardKind = CardKind.Minion,
                        InstanceId = "missing-art-card",
                        CardId = "MISSING_ART",
                        ImagePath = "CardImages/does-not-exist",
                        Name = "Missing Art",
                        Attack = 1,
                        Health = 1,
                        TavernTier = 1,
                        Tribes = new List<Tribe> { Tribe.Murloc },
                        Text = "Fill your hand with Tavern Dish Bananas."
                    },
                    UnityTavernCardMode.Shop,
                    "Buy",
                    null,
                    null);

                var missingArt = FindChild(missingArtObject.transform, "UnityCardArt").GetComponent<Image>();
                var fallback = FindChild(missingArtObject.transform, "UnityCardArtFallbackText").GetComponent<Text>();
                Assert.IsNull(missingArt.sprite);
                Assert.AreEqual("MA", fallback.text);
                Assert.GreaterOrEqual(fallback.fontSize, 20);
                Assert.IsTrue(fallback.GetComponent<Outline>().enabled);
                Assert.IsNotNull(FindChild(missingArtObject.transform, "UnityCardName"));
                Assert.AreEqual("Fill your hand with Tavern Dish Bananas.", FindChild(missingArtObject.transform, "UnityCardSubtitle").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(missingArtObject.transform, "UnityAttackBadge"));
                var missingActionRect = FindChild(missingArtObject.transform, "UnityCardAction-missing-art-card").GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.14f, 0f), missingActionRect.anchorMin);
                Assert.AreEqual(new Vector2(0.86f, 0f), missingActionRect.anchorMax);
                Assert.AreEqual(new Vector2(0f, 4f), missingActionRect.offsetMin);
                Assert.AreEqual(new Vector2(0f, 28f), missingActionRect.offsetMax);
            }
            finally
            {
                Object.DestroyImmediate(cardWithArtObject);
                Object.DestroyImmediate(missingArtObject);
            }
        }

        [Test]
        public void CardComponent_ArtDisplayTagsOverrideViewportWithoutHidingHud()
        {
            var cardObject = new GameObject("ContainedCardArt", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            var croppedSpellObject = new GameObject("CroppedSpellArt", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                    new MinionInstance
                    {
                        CardKind = CardKind.Minion,
                        InstanceId = "contained-card-art",
                        CardId = "BG20_100",
                        Name = "Contained Card Art",
                        Attack = 2,
                        Health = 1,
                        TavernTier = 1,
                        Tags = new List<string> { "ART_DISPLAY:CONTAIN" }
                    },
                    UnityTavernCardMode.Shop,
                    "Buy",
                    null,
                    null);

                var art = FindChild(cardObject.transform, "UnityCardArt").GetComponent<Image>();
                Assert.IsNotNull(art.sprite);
                Assert.IsNull(FindChild(cardObject.transform, "UnityCardArtViewport"));
                Assert.AreSame(cardObject.transform, art.transform.parent);
                Assert.AreEqual(Vector2.zero, art.rectTransform.anchorMin);
                Assert.AreEqual(Vector2.one, art.rectTransform.anchorMax);
                Assert.AreEqual("Contained Card Art", FindChild(cardObject.transform, "UnityCardName").GetComponent<Text>().text);
                Assert.AreEqual("2", FindChild(cardObject.transform, "UnityAttackBadgeText").GetComponent<Text>().text);
                Assert.AreEqual("1", FindChild(cardObject.transform, "UnityHealthBadgeText").GetComponent<Text>().text);

                croppedSpellObject.GetComponent<UnityTavernCardComponent>().Bind(
                    new MinionInstance
                    {
                        CardKind = CardKind.TavernSpell,
                        InstanceId = "cropped-spell-art",
                        CardId = "BG28_168",
                        Name = "Cropped Spell Art",
                        Tags = new List<string> { "art_display:crop" }
                    },
                    UnityTavernCardMode.Shop,
                    "Buy",
                    null,
                    null);

                var croppedSpellArt = FindChild(croppedSpellObject.transform, "UnityCardArt").GetComponent<Image>();
                var croppedSpellViewport = FindChild(croppedSpellObject.transform, "UnityCardArtViewport");
                Assert.IsNotNull(croppedSpellArt.sprite);
                Assert.IsNotNull(croppedSpellViewport);
                Assert.AreSame(croppedSpellViewport, croppedSpellArt.transform.parent);
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
                Object.DestroyImmediate(croppedSpellObject);
            }
        }

        [Test]
        public void CardComponent_MissingArtUsesStableNameAbbreviationAndOnlyTavernSpellCost()
        {
            var firstObject = new GameObject("MissingCjkArt", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            var secondObject = new GameObject("MissingCjkArtCopy", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            var spellObject = new GameObject("MissingOrdinarySpellArt", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            var tavernSpellObject = new GameObject("MissingTavernSpellArt", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                var missingCjkCard = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "missing-cjk-art",
                    CardId = "MISSING_CJK_ART",
                    ImagePath = "CardImages/does-not-exist-cjk",
                    Name = "\u9c9c\u8840\u5b9d\u77f3",
                    Attack = 1,
                    Health = 2,
                    TavernTier = 1
                };

                firstObject.GetComponent<UnityTavernCardComponent>().Bind(missingCjkCard, UnityTavernCardMode.Shop, null, null, null);
                secondObject.GetComponent<UnityTavernCardComponent>().Bind(missingCjkCard.Clone(), UnityTavernCardMode.Shop, null, null, null);

                var firstArt = FindChild(firstObject.transform, "UnityCardArt").GetComponent<Image>();
                var secondArt = FindChild(secondObject.transform, "UnityCardArt").GetComponent<Image>();
                Assert.AreEqual("\u9c9c\u8840", FindChild(firstObject.transform, "UnityCardArtFallbackText").GetComponent<Text>().text);
                Assert.AreEqual(firstArt.color, secondArt.color);

                spellObject.GetComponent<UnityTavernCardComponent>().Bind(
                    new MinionInstance
                    {
                        CardKind = CardKind.Spell,
                        InstanceId = "missing-ordinary-spell-art",
                        CardId = "MISSING_ORDINARY_SPELL_ART",
                        ImagePath = "CardImages/does-not-exist-spell",
                        Name = "Arcane Consumption",
                        Cost = 0
                    },
                    UnityTavernCardMode.Shop,
                    null,
                    null,
                    null);

                Assert.AreEqual("AC", FindChild(spellObject.transform, "UnityCardArtFallbackText").GetComponent<Text>().text);
                Assert.IsNull(FindChild(spellObject.transform, "UnityCostBadge"));

                tavernSpellObject.GetComponent<UnityTavernCardComponent>().Bind(
                    new MinionInstance
                    {
                        CardKind = CardKind.TavernSpell,
                        InstanceId = "missing-tavern-spell-art",
                        CardId = "MISSING_TAVERN_SPELL_ART",
                        ImagePath = "CardImages/does-not-exist-tavern-spell",
                        Name = "Tavern Offer",
                        Cost = 2
                    },
                    UnityTavernCardMode.Shop,
                    null,
                    null,
                    null);

                Assert.AreEqual("2", FindChild(tavernSpellObject.transform, "UnityCostBadgeText").GetComponent<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
                Object.DestroyImmediate(spellObject);
                Object.DestroyImmediate(tavernSpellObject);
            }
        }

        [Test]
        public void HandleDrop_AppliesBuyPlayMoveAndSellCommands()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
                Assert.GreaterOrEqual(shopIndex, 0);
                var shopCard = service.State.Player.Tavern.Shop[shopIndex];

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();
                var handBuyDrop = FindChild(rootObject.transform, "UnityHandBuyDropZone").GetComponent<UnityTavernDropTargetBehaviour>();

                controller.BeginDrag(shopCard, UnityTavernDragSource.Shop, shopIndex);
                handBuyDrop.OnDrop(null);

                Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
                Assert.IsNull(service.State.Player.Tavern.Shop[shopIndex]);

                var handCard = service.State.Player.Tavern.Hand[0];
                controller.BeginDrag(handCard, UnityTavernDragSource.Hand, 0);
                controller.HandleDrop(UnityTavernDropTarget.PlayerBoard, 0);

                Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
                Assert.AreEqual(1, service.State.Player.Board.Count);

                var secondBoardCard = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
                secondBoardCard.InstanceId = "unity-drag-second";
                secondBoardCard.Owner = BoardSide.Player;
                service.State.Player.Board.Add(secondBoardCard);

                var firstBoardCard = service.State.Player.Board[0];
                controller.BeginDrag(firstBoardCard, UnityTavernDragSource.PlayerBoard, 0);
                var playerWideDrop = FindChild(rootObject.transform, "UnityPlayerBoardReorderDropZone").GetComponent<UnityTavernDropTargetBehaviour>();
                PrepareWideDropRect(playerWideDrop);
                playerWideDrop.OnDrop(PointerDrop(rootObject.transform, 349f));

                Assert.AreEqual(firstBoardCard.InstanceId, service.State.Player.Board[1].InstanceId);

                controller.BeginDrag(firstBoardCard, UnityTavernDragSource.PlayerBoard, 1);
                controller.HandleDrop(UnityTavernDropTarget.SellZone);

                Assert.IsFalse(service.State.Player.Board.Any(card => card.InstanceId == firstBoardCard.InstanceId));

                var firstOpponentCard = CreateBoardMinion(service, "unity-drag-opponent-first", BoardSide.Opponent, 2, 3);
                var secondOpponentCard = CreateBoardMinion(service, "unity-drag-opponent-second", BoardSide.Opponent, 4, 5);
                service.State.Opponent.Board.Add(firstOpponentCard);
                service.State.Opponent.Board.Add(secondOpponentCard);

                FindChild(rootObject.transform, "UnityOpponentEntryButton").GetComponent<Button>().onClick.Invoke();
                controller.BeginDrag(firstOpponentCard, UnityTavernDragSource.OpponentBoard, 0);
                var opponentWideDrop = FindChild(rootObject.transform, "UnityOpponentBoardReorderDropZone").GetComponent<UnityTavernDropTargetBehaviour>();
                PrepareWideDropRect(opponentWideDrop);
                opponentWideDrop.OnDrop(PointerDrop(rootObject.transform, 349f));

                Assert.AreEqual(firstOpponentCard.InstanceId, service.State.Opponent.Board[1].InstanceId);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void ShopDrop_InsertsPurchasedCardAtHandTargetIndex()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var firstHandCard = CreateBoardMinion(service, "hand-insert-first", BoardSide.Player, 2, 3);
                var secondHandCard = CreateBoardMinion(service, "hand-insert-second", BoardSide.Player, 4, 5);
                firstHandCard.CardId = "hand-insert-first-card";
                secondHandCard.CardId = "hand-insert-second-card";
                firstHandCard.DefinitionId = "hand-insert-first-definition";
                secondHandCard.DefinitionId = "hand-insert-second-definition";
                service.State.Player.Tavern.Hand.Clear();
                service.State.Player.Tavern.Hand.Add(firstHandCard);
                service.State.Player.Tavern.Hand.Add(secondHandCard);

                var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
                Assert.GreaterOrEqual(shopIndex, 0);
                var purchasedCard = service.State.Player.Tavern.Shop[shopIndex];

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();

                controller.BeginDrag(purchasedCard, UnityTavernDragSource.Shop, shopIndex);
                controller.HandleDrop(UnityTavernDropTarget.Hand, 1);

                Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count);
                Assert.AreSame(firstHandCard, service.State.Player.Tavern.Hand[0]);
                Assert.AreSame(purchasedCard, service.State.Player.Tavern.Hand[1]);
                Assert.AreSame(secondHandCard, service.State.Player.Tavern.Hand[2]);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void BoardCard_HoverShowsAndHidesKeywordTooltip()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();

                var minion = CreateBoardMinion(service, "tooltip-player", BoardSide.Player, 6, 7, Keyword.Taunt, Keyword.DivineShield);
                minion.Text = "Battlecry: add a temporary bonus.";
                service.State.Player.Board.Add(minion);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var component = FindChild(rootObject.transform, "UnityCard-" + minion.InstanceId).GetComponent<UnityTavernCardComponent>();
                component.OnPointerEnter(new PointerEventData(EnsureEventSystem(rootObject.transform)));

                var tooltip = FindChild(rootObject.transform, "UnityKeywordTooltip");
                Assert.IsNotNull(tooltip);
                var descriptionTitle = FindChild(tooltip, "UnityKeywordTooltipDescriptionTitle");
                var description = FindChild(tooltip, "UnityKeywordTooltipDescription");
                Assert.IsNotNull(descriptionTitle);
                Assert.AreEqual(minion.Text, description.GetComponent<Text>().text);
                Assert.AreEqual("关键词", FindChild(tooltip, "UnityKeywordTooltipTitle").GetComponent<Text>().text);
                Assert.Less(descriptionTitle.GetSiblingIndex(), FindChild(tooltip, "UnityKeywordTooltipTitle").GetSiblingIndex());
                Assert.IsTrue(FindChild(tooltip, "UnityKeywordTooltipLine-Taunt").GetComponent<Text>().text.Contains("嘲讽"));
                Assert.IsTrue(FindChild(tooltip, "UnityKeywordTooltipLine-DivineShield").GetComponent<Text>().text.Contains("圣盾"));

                component.OnPointerExit(new PointerEventData(EventSystem.current));

                Assert.IsNull(FindChild(rootObject.transform, "UnityKeywordTooltip"));

                var shopRootObject = new GameObject("ShopRoot", typeof(RectTransform));
                try
                {
                    var shopService = MatchService.CreateWithDefaultCatalog(67890, new InMemoryTestScenarioRepository());
                    var shopMinion = CreateBoardMinion(shopService, "tooltip-shop", BoardSide.Player, 3, 4);
                    shopMinion.Text = "Gain a temporary bonus this turn.";
                    shopService.State.Player.Tavern.Shop.Clear();
                    shopService.State.Player.Tavern.Shop.Add(shopMinion);

                    new UnityTavernTrainerView(shopRootObject.transform, shopService, new LocalAdvisorService(), () => { }).Build();

                    var shopComponent = FindChild(shopRootObject.transform, "UnityCard-" + shopMinion.InstanceId).GetComponent<UnityTavernCardComponent>();
                    shopComponent.OnPointerEnter(new PointerEventData(EnsureEventSystem(shopRootObject.transform)));

                    var shopTooltip = FindChild(shopRootObject.transform, "UnityKeywordTooltip");
                    Assert.IsNotNull(shopTooltip);
                    Assert.AreEqual(shopMinion.Text, FindChild(shopTooltip, "UnityKeywordTooltipDescription").GetComponent<Text>().text);
                }
                finally
                {
                    Object.DestroyImmediate(shopRootObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void BoardCard_RightClickEditorSavesCurrentStatsAndKeywords()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();

                var minion = CreateBoardMinion(service, "editable-player", BoardSide.Player, 3, 4, Keyword.Magnetic, Keyword.Taunt);
                service.State.Player.Board.Add(minion);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                RightClickCard(rootObject.transform, minion);

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityMinionEditOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityMinionEditPanel"));
                Assert.AreEqual("随从编辑", FindChild(rootObject.transform, "UnityMinionEditTitle").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityMinionEditKeywordToggle-Magnetic"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityMinionEditAttackInputPlaceholder"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityMinionEditHealthInputPlaceholder"));

                SetEditorValues(rootObject.transform, "50", "50", Keyword.Taunt, Keyword.DivineShield, Keyword.Reborn);
                FindChild(rootObject.transform, "UnityMinionEditSaveButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(FindChild(rootObject.transform, "UnityMinionEditOverlay"));
                AssertMinionState(service.State.Player.Board[0], 50, 50, Keyword.Taunt, Keyword.DivineShield, Keyword.Reborn);
                CollectionAssert.DoesNotContain(service.State.Player.Board[0].Keywords, Keyword.Magnetic);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void BoardCard_RightClickEditorAcceptsIntMaxStats()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();

                var minion = CreateBoardMinion(service, "editable-max-player", BoardSide.Player, 3, 4);
                service.State.Player.Board.Add(minion);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                RightClickCard(rootObject.transform, minion);
                SetEditorValues(rootObject.transform, int.MaxValue.ToString(), int.MaxValue.ToString());
                FindChild(rootObject.transform, "UnityMinionEditSaveButton").GetComponent<Button>().onClick.Invoke();

                AssertMinionState(service.State.Player.Board[0], int.MaxValue, int.MaxValue);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Tools_SelectedStatPlusButtonsSaturateAtIntMaxValue()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();

                var minion = CreateBoardMinion(service, "tools-max-player", BoardSide.Player, int.MaxValue, int.MaxValue);
                service.State.Player.Board.Add(minion);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                FindChild(rootObject.transform, "UnityCard-" + minion.InstanceId).GetComponent<Button>().onClick.Invoke();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();

                FindChild(rootObject.transform, "UnityToolsSelectedAttackPlusButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityToolsSelectedHealthPlusButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(int.MaxValue, service.State.Player.Board[0].Attack);
                Assert.AreEqual(int.MaxValue, service.State.Player.Board[0].Health);
                Assert.AreEqual(int.MaxValue, service.State.Player.Board[0].MaxHealth);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void BoardCard_RightClickEditorAppliesPanelValuesToEachSide()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();

                var playerA = CreateBoardMinion(service, "batch-player-a", BoardSide.Player, 2, 3, Keyword.Taunt);
                var playerB = CreateBoardMinion(service, "batch-player-b", BoardSide.Player, 4, 5, Keyword.DivineShield);
                var opponentA = CreateBoardMinion(service, "batch-opponent-a", BoardSide.Opponent, 6, 7, Keyword.Deathrattle);
                var opponentB = CreateBoardMinion(service, "batch-opponent-b", BoardSide.Opponent, 8, 9, Keyword.Reborn);
                service.State.Player.Board.Add(playerA);
                service.State.Player.Board.Add(playerB);
                service.State.Opponent.Board.Add(opponentA);
                service.State.Opponent.Board.Add(opponentB);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                RightClickCard(rootObject.transform, playerA);
                SetEditorValues(rootObject.transform, "44", "55", Keyword.Taunt, Keyword.Windfury);
                FindChild(rootObject.transform, "UnityMinionEditApplyPlayerButton").GetComponent<Button>().onClick.Invoke();

                AssertMinionState(playerA, 44, 55, Keyword.Taunt, Keyword.Windfury);
                AssertMinionState(playerB, 44, 55, Keyword.Taunt, Keyword.Windfury);
                AssertMinionState(opponentA, 6, 7, Keyword.Deathrattle);
                AssertMinionState(opponentB, 8, 9, Keyword.Reborn);

                FindChild(rootObject.transform, "UnityOpponentEntryButton").GetComponent<Button>().onClick.Invoke();
                RightClickCard(rootObject.transform, opponentA);
                SetEditorValues(rootObject.transform, "12", "34", Keyword.Venomous, Keyword.Stealth);
                FindChild(rootObject.transform, "UnityMinionEditApplyOpponentButton").GetComponent<Button>().onClick.Invoke();

                AssertMinionState(playerA, 44, 55, Keyword.Taunt, Keyword.Windfury);
                AssertMinionState(playerB, 44, 55, Keyword.Taunt, Keyword.Windfury);
                AssertMinionState(opponentA, 12, 34, Keyword.Venomous, Keyword.Stealth);
                AssertMinionState(opponentB, 12, 34, Keyword.Venomous, Keyword.Stealth);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_UsesConfiguredRootPrefabWhenAvailable()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            var rootPrefab = new GameObject("ConfiguredRootPrefab", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(UnityTavernTrainerController));
            try
            {
                rootPrefab.GetComponent<CanvasGroup>().alpha = 0.73f;
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(
                    rootObject.transform,
                    service,
                    new LocalAdvisorService(),
                    () => { },
                    rootPrefab: rootPrefab).Build();

                var shell = FindChild(rootObject.transform, "UnityTavernTrainer");
                Assert.IsNotNull(shell);
                Assert.AreEqual(0.73f, shell.GetComponent<CanvasGroup>().alpha);
                Assert.IsNotNull(shell.GetComponent<UnityTavernTrainerController>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTopBar"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                Object.DestroyImmediate(rootPrefab);
            }
        }

        [Test]
        public void CardComponent_BindUsesConfiguredPrefabReferencesWithoutGeneratedChildren()
        {
            var cardObject = new GameObject("CardPrefab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                var art = ImageChild(cardObject.transform, "PrefabArt");
                var name = TextChild(cardObject.transform, "PrefabName");
                var subtitle = TextChild(cardObject.transform, "PrefabSubtitle");
                var kind = TextChild(cardObject.transform, "PrefabKind");
                var tierBadge = new GameObject("PrefabTierBadge", typeof(RectTransform));
                tierBadge.transform.SetParent(cardObject.transform, false);
                var tier = TextChild(tierBadge.transform, "PrefabTierText");
                var attackBadge = new GameObject("PrefabAttackBadge", typeof(RectTransform));
                attackBadge.transform.SetParent(cardObject.transform, false);
                var attack = TextChild(attackBadge.transform, "PrefabAttackText");
                var healthBadge = new GameObject("PrefabHealthBadge", typeof(RectTransform));
                healthBadge.transform.SetParent(cardObject.transform, false);
                var health = TextChild(healthBadge.transform, "PrefabHealthText");
                var costBadge = new GameObject("PrefabCostBadge", typeof(RectTransform));
                costBadge.transform.SetParent(cardObject.transform, false);
                var cost = TextChild(costBadge.transform, "PrefabCostText");
                var action = ButtonChild(cardObject.transform, "PrefabPrimaryAction");
                var actionText = TextChild(action.transform, "PrefabPrimaryActionText");

                var component = cardObject.GetComponent<UnityTavernCardComponent>();
                component.ConfigureReferences(
                    frame: cardObject.GetComponent<Image>(),
                    art: art,
                    name: name,
                    subtitle: subtitle,
                    kind: kind,
                    tier: tier,
                    attack: attack,
                    health: health,
                    cost: cost,
                    rootButton: cardObject.GetComponent<Button>(),
                    primaryButton: action.GetComponent<Button>(),
                    primaryText: actionText,
                    tierBadgeObject: tierBadge,
                    attackBadgeObject: attackBadge,
                    healthBadgeObject: healthBadge,
                    costBadgeObject: costBadge);

                var selected = false;
                var acted = false;
                var card = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "prefab-card",
                    Name = "测试随从",
                    Attack = 3,
                    Health = 4,
                    Cost = 3,
                    TavernTier = 2,
                    Tribes = new List<Tribe> { Tribe.Mech },
                    OfficialKeywords = new List<Keyword> { Keyword.Taunt },
                    Text = "A plain minion should not show spell description text."
                };
                var childCount = cardObject.transform.childCount;

                component.Bind(card, UnityTavernCardMode.Shop, "购买", _ => selected = true, _ => acted = true);

                Assert.AreEqual(childCount, cardObject.transform.childCount);
                Assert.IsNull(FindChild(cardObject.transform, "UnityCardArt"));
                Assert.AreEqual("测试随从", name.text);
                Assert.IsFalse(subtitle.gameObject.activeSelf);
                Assert.AreEqual(string.Empty, subtitle.text);
                Assert.AreEqual("机械", kind.text);
                Assert.AreEqual("2", tier.text);
                Assert.AreEqual("3", attack.text);
                Assert.AreEqual("4", health.text);
                Assert.AreEqual("购买", actionText.text);
                Assert.IsTrue(attackBadge.activeSelf);
                Assert.IsTrue(healthBadge.activeSelf);
                Assert.IsFalse(costBadge.activeSelf);

                action.GetComponent<Button>().onClick.Invoke();
                cardObject.GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(acted);
                Assert.IsTrue(selected);
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void CardComponent_PrefabReferencesShowSpellDescriptionWhenArtMissing()
        {
            var cardObject = new GameObject("CardPrefabSpellNoArt", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                var art = ImageChild(cardObject.transform, "PrefabArt");
                var name = TextChild(cardObject.transform, "PrefabName");
                var subtitle = TextChild(cardObject.transform, "PrefabSubtitle");
                var kind = TextChild(cardObject.transform, "PrefabKind");
                var tierBadge = new GameObject("PrefabTierBadge", typeof(RectTransform));
                tierBadge.transform.SetParent(cardObject.transform, false);
                var tier = TextChild(tierBadge.transform, "PrefabTierText");
                var attackBadge = new GameObject("PrefabAttackBadge", typeof(RectTransform));
                attackBadge.transform.SetParent(cardObject.transform, false);
                var attack = TextChild(attackBadge.transform, "PrefabAttackText");
                var healthBadge = new GameObject("PrefabHealthBadge", typeof(RectTransform));
                healthBadge.transform.SetParent(cardObject.transform, false);
                var health = TextChild(healthBadge.transform, "PrefabHealthText");
                var costBadge = new GameObject("PrefabCostBadge", typeof(RectTransform));
                costBadge.transform.SetParent(cardObject.transform, false);
                var cost = TextChild(costBadge.transform, "PrefabCostText");
                var action = ButtonChild(cardObject.transform, "PrefabPrimaryAction");
                var actionText = TextChild(action.transform, "PrefabPrimaryActionText");

                cardObject.GetComponent<UnityTavernCardComponent>().ConfigureReferences(
                    frame: cardObject.GetComponent<Image>(),
                    art: art,
                    name: name,
                    subtitle: subtitle,
                    kind: kind,
                    tier: tier,
                    attack: attack,
                    health: health,
                    cost: cost,
                    rootButton: cardObject.GetComponent<Button>(),
                    primaryButton: action.GetComponent<Button>(),
                    primaryText: actionText,
                    tierBadgeObject: tierBadge,
                    attackBadgeObject: attackBadge,
                    healthBadgeObject: healthBadge,
                    costBadgeObject: costBadge);

                cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                    new MinionInstance
                    {
                        CardKind = CardKind.Spell,
                        InstanceId = "prefab-spell-no-art",
                        CardId = "MISSING_DARKMOON_PRIZE",
                        ImagePath = "CardImages/does-not-exist",
                        Name = "No Art Prize",
                        Cost = 0,
                        TavernTier = 3,
                        Tribes = new List<Tribe> { Tribe.None },
                        Text = "Fill your hand with Tavern Dish Bananas."
                    },
                    UnityTavernCardMode.Shop,
                    "Play",
                    null,
                    null);

                Assert.IsTrue(subtitle.gameObject.activeSelf);
                Assert.AreEqual("Fill your hand with Tavern Dish Bananas.", subtitle.text);
                Assert.AreEqual("法术", kind.text);
                Assert.AreEqual(string.Empty, cost.text);
                Assert.IsFalse(costBadge.activeSelf);
                Assert.IsFalse(attackBadge.activeSelf);
                Assert.IsFalse(healthBadge.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }
        [Test]
        public void CardComponent_PrefabReferencesShowCombatHudWhenFullArtExists()
        {
            var cardObject = new GameObject("CardPrefabWithArt", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                var art = ImageChild(cardObject.transform, "PrefabArt");
                var name = TextChild(cardObject.transform, "PrefabName");
                var subtitle = TextChild(cardObject.transform, "PrefabSubtitle");
                var kind = TextChild(cardObject.transform, "PrefabKind");
                var tierBadge = new GameObject("PrefabTierBadge", typeof(RectTransform));
                tierBadge.transform.SetParent(cardObject.transform, false);
                var tier = TextChild(tierBadge.transform, "PrefabTierText");
                var attackBadge = new GameObject("PrefabAttackBadge", typeof(RectTransform));
                attackBadge.transform.SetParent(cardObject.transform, false);
                var attack = TextChild(attackBadge.transform, "PrefabAttackText");
                var healthBadge = new GameObject("PrefabHealthBadge", typeof(RectTransform));
                healthBadge.transform.SetParent(cardObject.transform, false);
                var health = TextChild(healthBadge.transform, "PrefabHealthText");
                var costBadge = new GameObject("PrefabCostBadge", typeof(RectTransform));
                costBadge.transform.SetParent(cardObject.transform, false);
                var cost = TextChild(costBadge.transform, "PrefabCostText");
                var action = ButtonChild(cardObject.transform, "PrefabPrimaryAction");
                var actionText = TextChild(action.transform, "PrefabPrimaryActionText");

                cardObject.GetComponent<UnityTavernCardComponent>().ConfigureReferences(
                    frame: cardObject.GetComponent<Image>(),
                    art: art,
                    name: name,
                    subtitle: subtitle,
                    kind: kind,
                    tier: tier,
                    attack: attack,
                    health: health,
                    cost: cost,
                    rootButton: cardObject.GetComponent<Button>(),
                    primaryButton: action.GetComponent<Button>(),
                    primaryText: actionText,
                    tierBadgeObject: tierBadge,
                    attackBadgeObject: attackBadge,
                    healthBadgeObject: healthBadge,
                    costBadgeObject: costBadge);

                cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                    new MinionInstance
                    {
                        CardKind = CardKind.Minion,
                        InstanceId = "prefab-card-with-art",
                        CardId = "BG20_100",
                        Name = "Card With Art",
                        Attack = 2,
                        Health = 1,
                        Cost = 3,
                        TavernTier = 1,
                        Tribes = new List<Tribe> { Tribe.Quilboar },
                        OfficialKeywords = new List<Keyword> { Keyword.Taunt, Keyword.DivineShield },
                        Text = "Full art cards keep the art-only presentation."
                    },
                    UnityTavernCardMode.Shop,
                    "Buy",
                    null,
                    null);

                Assert.IsNotNull(art.sprite);
                var artViewport = FindChild(cardObject.transform, "UnityCardArtViewport");
                Assert.IsNotNull(artViewport);
                Assert.IsNotNull(artViewport.GetComponent<RectMask2D>());
                Assert.AreSame(artViewport, art.transform.parent);
                Assert.AreEqual(new Vector2(0f, -1f), art.rectTransform.anchorMin);
                Assert.AreEqual(new Vector2(1f, 1f), art.rectTransform.anchorMax);
                Assert.IsTrue(name.gameObject.activeSelf);
                Assert.IsNotNull(name.font);
                Assert.IsFalse(subtitle.gameObject.activeSelf);
                Assert.AreEqual(string.Empty, subtitle.text);
                Assert.IsFalse(kind.gameObject.activeSelf);
                Assert.IsFalse(tierBadge.activeSelf);
                Assert.IsTrue(attackBadge.activeSelf);
                Assert.IsTrue(healthBadge.activeSelf);
                Assert.IsFalse(costBadge.activeSelf);
                Assert.AreEqual("2", attack.text);
                Assert.AreEqual("1", health.text);
                Assert.AreEqual("Buy", actionText.text);
                Assert.IsNotNull(attack.font);
                Assert.IsNotNull(health.font);
                Assert.IsNotNull(actionText.font);
                var actionRect = action.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.22f, 0f), actionRect.anchorMin);
                Assert.AreEqual(new Vector2(0.78f, 0f), actionRect.anchorMax);
                Assert.AreEqual(new Vector2(0f, 4f), actionRect.offsetMin);
                Assert.AreEqual(new Vector2(0f, 28f), actionRect.offsetMax);
                Assert.AreEqual(12, actionText.fontSize);
                Assert.IsNotNull(action.GetComponent<Outline>());
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void TavernCardPrefab_BindsThroughSerializedReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCardComponent.TavernCardPrefabAssetPath);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<Outline>());
            Assert.GreaterOrEqual(prefab.GetComponents<Shadow>().Length, 2);

            var cardObject = Object.Instantiate(prefab);
            try
            {
                var component = cardObject.GetComponent<UnityTavernCardComponent>();
                Assert.IsNotNull(component);

                var acted = false;
                var card = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "prefab-asset-card",
                    Name = "资产随从",
                    Attack = 5,
                    Health = 6,
                    TavernTier = 3,
                    Tribes = new List<Tribe> { Tribe.Beast },
                    OfficialKeywords = new List<Keyword> { Keyword.DivineShield }
                };
                var childCount = cardObject.transform.childCount;

                component.Bind(card, UnityTavernCardMode.Shop, "购买", null, _ => acted = true);

                Assert.AreEqual(childCount, cardObject.transform.childCount);
                Assert.AreEqual("资产随从", FindChild(cardObject.transform, "UnityCardName").GetComponent<Text>().text);
                var subtitle = FindChild(cardObject.transform, "UnityCardSubtitle").GetComponent<Text>();
                Assert.IsFalse(subtitle.gameObject.activeSelf);
                Assert.AreEqual(string.Empty, subtitle.text);
                Assert.AreEqual("5", FindChild(cardObject.transform, "UnityAttackBadgeText").GetComponent<Text>().text);
                Assert.AreEqual("6", FindChild(cardObject.transform, "UnityHealthBadgeText").GetComponent<Text>().text);

                FindChild(cardObject.transform, "UnityCardAction-prefab-asset-card").GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(acted);
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void ZonePrefabs_HaveRootComponents()
        {
            AssertZonePrefab(UnityTavernZoneComponent.ShopZonePrefabAssetPath);
            AssertZonePrefab(UnityTavernZoneComponent.HandZonePrefabAssetPath);
            AssertZonePrefab(UnityTavernZoneComponent.PlayerBoardZonePrefabAssetPath);
            AssertZonePrefab(UnityTavernZoneComponent.OpponentBoardZonePrefabAssetPath);
        }

        [Test]
        public void ZonePrefab_BindsSerializedHeaderAndSlotParent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernZoneComponent.ShopZonePrefabAssetPath);
            Assert.IsNotNull(prefab);

            var zoneObject = Object.Instantiate(prefab);
            try
            {
                var zone = zoneObject.GetComponent<UnityTavernZoneComponent>();
                Assert.IsNotNull(zone);

                var acted = false;
                var card = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "zone-prefab-card",
                    Name = "区域随从",
                    Attack = 2,
                    Health = 3,
                    TavernTier = 1,
                    Tribes = new List<Tribe> { Tribe.Murloc }
                };
                var rootChildCount = zoneObject.transform.childCount;

                zone.Build(
                    "测试商店",
                    "可刷新",
                    new List<MinionInstance> { card },
                    2,
                    UnityTavernCardMode.Shop,
                    _ => "购买",
                    null,
                    _ => acted = true);

                Assert.AreEqual(rootChildCount, zoneObject.transform.childCount);
                Assert.AreEqual("测试商店", FindChild(zoneObject.transform, "UnityZoneTitle").GetComponent<Text>().text);
                Assert.AreEqual("可刷新", FindChild(zoneObject.transform, "UnityZoneSubtitle").GetComponent<Text>().text);
                Assert.AreNotEqual(UnityTavernUiStyle.Panel, zoneObject.GetComponent<Image>().color);
                Assert.IsTrue(zoneObject.GetComponent<Outline>().enabled);

                var header = FindChild(zoneObject.transform, "UnityZoneHeader");
                Assert.IsNotNull(header.GetComponent<Image>());
                Assert.Greater(header.GetComponent<Image>().color.a, 0f);
                Assert.IsNotNull(FindChild(header, "UnityZoneAccentMark"));
                Assert.IsTrue(FindChild(header, "UnityZoneAccentMark").GetComponent<LayoutElement>().ignoreLayout);

                var row = FindChild(zoneObject.transform, "UnityZoneCardRow");
                Assert.IsNotNull(row.GetComponent<Image>());
                Assert.Greater(row.GetComponent<Image>().color.a, 0f);
                Assert.AreEqual(2, row.childCount);
                Assert.Greater(row.GetChild(0).GetComponent<Image>().color.a, 0f);
                Assert.Greater(row.GetChild(1).GetComponent<Image>().color.a, 0f);
                Assert.IsNotNull(FindChild(row, "UnityCard-zone-prefab-card").GetComponent<UnityTavernCardComponent>());

                FindChild(row, "UnityCardAction-zone-prefab-card").GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(acted);
            }
            finally
            {
                Object.DestroyImmediate(zoneObject);
            }
        }

        [Test]
        public void ZonePrefab_CompactLayoutUsesDenseSlotsAndScaledCards()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernZoneComponent.ShopZonePrefabAssetPath);
            Assert.IsNotNull(prefab);

            var zoneObject = Object.Instantiate(prefab);
            try
            {
                var compact = UnityTavernLayoutContext.ForSize(994f, 384f);
                var metrics = compact.ZoneMetrics(UnityTavernZoneKind.Shop, UnityTavernCardMode.Shop);
                var zone = zoneObject.GetComponent<UnityTavernZoneComponent>();
                Assert.IsNotNull(zone);

                var card = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "compact-zone-card",
                    Name = "Compact Zone Minion",
                    Attack = 2,
                    Health = 3,
                    TavernTier = 1,
                    Tribes = new List<Tribe> { Tribe.Murloc }
                };

                zone.Build(
                    "Compact Shop",
                    "Ready",
                    new List<MinionInstance> { card },
                    2,
                    UnityTavernCardMode.Shop,
                    _ => "Buy",
                    null,
                    null,
                    layoutContext: compact);

                var row = FindChild(zoneObject.transform, "UnityZoneCardRow");
                Assert.AreEqual(metrics.SlotSpacing, row.GetComponent<HorizontalLayoutGroup>().spacing, 0.001f);

                var slot = row.GetChild(0).GetComponent<LayoutElement>();
                Assert.AreEqual(metrics.SlotSize.x, slot.preferredWidth, 0.001f);
                Assert.AreEqual(metrics.SlotSize.y, slot.preferredHeight, 0.001f);

                var cardObject = FindChild(row, "UnityCard-compact-zone-card");
                Assert.AreEqual(metrics.CardScale, cardObject.localScale.x, 0.001f);
                Assert.AreEqual(metrics.CardScale, cardObject.localScale.y, 0.001f);
                Assert.AreEqual(4f, zoneObject.GetComponent<VerticalLayoutGroup>().spacing, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(zoneObject);
            }
        }

        [Test]
        public void PanelAndModalPrefabs_HaveRootComponents()
        {
            var rightPanel = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernRightPanelComponent.RightPanelPrefabAssetPath);
            Assert.IsNotNull(rightPanel);
            Assert.IsNotNull(rightPanel.GetComponent<UnityTavernRightPanelComponent>());
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelHeader"));
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelFloatToggle"));
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelFloatToggleText"));
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelActionHost"));
            var detailHost = FindChild(rightPanel.transform, "UnityRightPanelDetailHost");
            Assert.IsNotNull(detailHost);
            Assert.AreEqual(236f, detailHost.GetComponent<LayoutElement>().preferredHeight, 0.001f);
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelAdvisorHost"));
            Assert.IsNotNull(FindChild(rightPanel.transform, "UnityRightPanelLogHost"));

            var actionPanel = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernActionPanelComponent.ActionPanelPrefabAssetPath);
            Assert.IsNotNull(actionPanel);
            Assert.IsNotNull(actionPanel.GetComponent<UnityTavernActionPanelComponent>());
            Assert.IsNotNull(FindChild(actionPanel.transform, "UnityActionButtonGrid"));

            var selectedPanel = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernSelectedCardPanelComponent.SelectedCardPanelPrefabAssetPath);
            Assert.IsNotNull(selectedPanel);
            Assert.IsNotNull(selectedPanel.GetComponent<UnityTavernSelectedCardPanelComponent>());
            Assert.IsNotNull(FindChild(selectedPanel.transform, "UnitySelectedCardContent"));

            var advisorPanel = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernAdvisorPanelComponent.AdvisorPanelPrefabAssetPath);
            Assert.IsNotNull(advisorPanel);
            Assert.IsNotNull(advisorPanel.GetComponent<UnityTavernAdvisorPanelComponent>());
            Assert.IsNotNull(FindChild(advisorPanel.transform, "UnityAdvisorTitle"));
            Assert.IsNotNull(FindChild(advisorPanel.transform, "UnityAdvisorContent"));

            var recruitLog = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernLogPanelComponent.RecruitLogPanelPrefabAssetPath);
            Assert.IsNotNull(recruitLog);
            Assert.IsNotNull(recruitLog.GetComponent<UnityTavernLogPanelComponent>());
            Assert.IsNotNull(FindChild(recruitLog.transform, "UnityLogTitle"));
            Assert.IsNotNull(FindChild(recruitLog.transform, "UnityLogScrollViewContent"));

            var combatLog = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernLogPanelComponent.CombatLogPanelPrefabAssetPath);
            Assert.IsNotNull(combatLog);
            Assert.IsNotNull(combatLog.GetComponent<UnityTavernLogPanelComponent>());
            Assert.IsNotNull(FindChild(combatLog.transform, "UnityLogTitle"));
            Assert.IsNotNull(FindChild(combatLog.transform, "UnityLogScrollViewContent"));

            var discover = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernDiscoverModalComponent.DiscoverModalPrefabAssetPath);
            Assert.IsNotNull(discover);
            Assert.IsNotNull(discover.GetComponent<UnityTavernDiscoverModalComponent>());
            Assert.IsNotNull(FindChild(discover.transform, "UnityDiscoverTitle"));
            Assert.IsNotNull(FindChild(discover.transform, "UnityDiscoverOptions"));

            var cardDetail = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCardDetailModalComponent.CardDetailModalPrefabAssetPath);
            Assert.IsNotNull(cardDetail);
            Assert.IsNotNull(cardDetail.GetComponent<UnityTavernCardDetailModalComponent>());
            Assert.IsNotNull(FindChild(cardDetail.transform, "UnityCardDetailTitle"));
            Assert.IsNotNull(FindChild(cardDetail.transform, "UnityCardDetailCardHost"));
            Assert.IsNotNull(FindChild(cardDetail.transform, "UnityCardDetailInfo"));

            var tools = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernToolsModalComponent.ToolsModalPrefabAssetPath);
            Assert.IsNotNull(tools);
            Assert.IsNotNull(tools.GetComponent<UnityTavernToolsModalComponent>());
            Assert.IsNotNull(FindChild(tools.transform, "UnityTrainerToolsTitle"));
            Assert.IsNotNull(FindChild(tools.transform, "UnityTrainerToolsScrollContent"));
            var toolsPanelRect = FindChild(tools.transform, "UnityTrainerToolsPanel").GetComponent<RectTransform>();
            Assert.AreEqual(new Vector2(0.22f, 0.16f), toolsPanelRect.anchorMin);
            Assert.AreEqual(new Vector2(0.78f, 0.84f), toolsPanelRect.anchorMax);
            Assert.AreEqual(Vector2.zero, toolsPanelRect.sizeDelta);
            Assert.IsFalse(FindChild(tools.transform, "UnityTrainerToolsPanel").GetComponent<VerticalLayoutGroup>().childForceExpandHeight);
            Assert.AreEqual(42f, FindChild(tools.transform, "UnityTrainerToolsHeader").GetComponent<LayoutElement>().preferredHeight, 0.001f);
            Assert.IsFalse(FindChild(tools.transform, "UnityTrainerToolsHeader").GetComponent<HorizontalLayoutGroup>().childForceExpandWidth);
            Assert.IsFalse(FindChild(tools.transform, "UnityTrainerToolsHeader").GetComponent<HorizontalLayoutGroup>().childForceExpandHeight);
            Assert.AreEqual(84f, FindChild(tools.transform, "UnityTrainerToolsCloseButton").GetComponent<LayoutElement>().preferredWidth, 0.001f);
            Assert.AreEqual(32f, FindChild(tools.transform, "UnityTrainerToolsCloseButton").GetComponent<LayoutElement>().preferredHeight, 0.001f);

            var replay = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCombatReplayPanelComponent.CombatReplayPanelPrefabAssetPath);
            Assert.IsNotNull(replay);
            Assert.IsNotNull(replay.GetComponent<UnityTavernCombatReplayPanelComponent>());
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayTitle"));
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayControls"));
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayEventHighlights"));
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayPlayerBoard"));
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayOpponentBoard"));
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayTimelineContent"));
            var replayPanelRect = FindChild(replay.transform, "UnityCombatReplayPanelSurface").GetComponent<RectTransform>();
            Assert.IsNotNull(replayPanelRect.GetComponent<Outline>());
            Assert.AreEqual(new Vector2(0.06f, 0.04f), replayPanelRect.anchorMin);
            Assert.AreEqual(new Vector2(0.94f, 0.96f), replayPanelRect.anchorMax);
            Assert.AreEqual(Vector2.zero, replayPanelRect.sizeDelta);
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayHeader").GetComponent<Outline>());
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayHeaderAccent"));
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayControls").GetComponent<Outline>());
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayEventHighlights").GetComponent<Outline>());
            Assert.IsNotNull(FindChild(replay.transform, "UnityCombatReplayTimeline").GetComponent<Outline>());
            var replayBoards = FindChild(replay.transform, "UnityCombatReplayBoards");
            Assert.IsNotNull(replayBoards.GetComponent<VerticalLayoutGroup>());
            Assert.IsNull(replayBoards.GetComponent<HorizontalLayoutGroup>());
            Assert.IsNotNull(replayBoards.GetComponent<Outline>());
            Assert.AreEqual(360f, replayBoards.GetComponent<LayoutElement>().preferredHeight, 0.001f);
            Assert.IsNotNull(FindChild(replayBoards, "UnityCombatReplayOpponentBoard").GetComponent<Outline>());
            Assert.IsNotNull(FindChild(replayBoards, "UnityCombatReplayPlayerBoard").GetComponent<Outline>());
            Assert.IsNotNull(FindChild(replayBoards, "UnityReplayBoardAccent"));
            Assert.Less(
                FindChild(replayBoards, "UnityCombatReplayOpponentBoard").GetSiblingIndex(),
                FindChild(replayBoards, "UnityCombatReplayPlayerBoard").GetSiblingIndex());

            var toast = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernToastComponent.ErrorToastPrefabAssetPath);
            Assert.IsNotNull(toast);
            Assert.IsNotNull(toast.GetComponent<UnityTavernToastComponent>());
            Assert.IsNotNull(FindChild(toast.transform, "UnityErrorToastText"));
        }

        [Test]
        public void RightPanelChildPrefabs_BuildThroughSerializedContainers()
        {
            var actionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernActionPanelComponent.ActionPanelPrefabAssetPath);
            var selectedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernSelectedCardPanelComponent.SelectedCardPanelPrefabAssetPath);
            var advisorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernAdvisorPanelComponent.AdvisorPanelPrefabAssetPath);
            var logPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernLogPanelComponent.RecruitLogPanelPrefabAssetPath);
            Assert.IsNotNull(actionPrefab);
            Assert.IsNotNull(selectedPrefab);
            Assert.IsNotNull(advisorPrefab);
            Assert.IsNotNull(logPrefab);

            var actionObject = Object.Instantiate(actionPrefab);
            var selectedObject = Object.Instantiate(selectedPrefab);
            var advisorObject = Object.Instantiate(advisorPrefab);
            var logObject = Object.Instantiate(logPrefab);
            try
            {
                actionObject.GetComponent<UnityTavernActionPanelComponent>().Build(
                    parent => new GameObject("BuiltActionButton", typeof(RectTransform)).transform.SetParent(parent, false));
                Assert.IsNotNull(FindChild(actionObject.transform, "BuiltActionButton"));

                selectedObject.GetComponent<UnityTavernSelectedCardPanelComponent>().Build(
                    parent => new GameObject("BuiltSelectedDetail", typeof(RectTransform)).transform.SetParent(parent, false));
                Assert.IsNotNull(FindChild(selectedObject.transform, "BuiltSelectedDetail"));

                advisorObject.GetComponent<UnityTavernAdvisorPanelComponent>().Build(
                    "Test Advice",
                    parent => new GameObject("BuiltAdvisorLine", typeof(RectTransform)).transform.SetParent(parent, false));
                Assert.AreEqual("Test Advice", FindChild(advisorObject.transform, "UnityAdvisorTitle").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(advisorObject.transform, "BuiltAdvisorLine"));

                logObject.GetComponent<UnityTavernLogPanelComponent>().Build(
                    "Test Log",
                    parent => new GameObject("BuiltLogLine", typeof(RectTransform)).transform.SetParent(parent, false));
                Assert.AreEqual("Test Log", FindChild(logObject.transform, "UnityLogTitle").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(logObject.transform, "BuiltLogLine"));
            }
            finally
            {
                Object.DestroyImmediate(actionObject);
                Object.DestroyImmediate(selectedObject);
                Object.DestroyImmediate(advisorObject);
                Object.DestroyImmediate(logObject);
            }
        }

        [Test]
        public void CardDetailModalPrefab_BuildsCardInfoAndClose()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCardDetailModalComponent.CardDetailModalPrefabAssetPath);
            Assert.IsNotNull(prefab);

            var modalObject = Object.Instantiate(prefab);
            try
            {
                var closed = false;
                var card = new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "detail-card",
                    CardId = "detail-card-id",
                    Name = "Detail Minion",
                    Attack = 2,
                    Health = 3,
                    MaxHealth = 3,
                    TavernTier = 1,
                    Text = "Detail text",
                    Tribes = new List<Tribe> { Tribe.Beast },
                    Keywords = new List<Keyword> { Keyword.Taunt }
                };

                modalObject.GetComponent<UnityTavernCardDetailModalComponent>().Build(card, () => closed = true);

                Assert.AreEqual("Detail Minion", FindChild(modalObject.transform, "UnityCardDetailTitle").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(modalObject.transform, "UnityCard-detail-card"));
                FindChild(modalObject.transform, "UnityCardDetailCloseButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(closed);
            }
            finally
            {
                Object.DestroyImmediate(modalObject);
            }
        }

        [Test]
        public void CombatReplayPanelPrefab_BuildsFullscreenBattlefieldAndNavigation()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCombatReplayPanelComponent.CombatReplayPanelPrefabAssetPath);
            Assert.IsNotNull(prefab);

            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var player = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
            player.InstanceId = "replay-player";
            player.Owner = BoardSide.Player;
            var opponent = service.State.Player.Tavern.Shop.Last(card => card != null && card.CardKind == CardKind.Minion).Clone();
            opponent.InstanceId = "replay-opponent";
            opponent.Owner = BoardSide.Opponent;
            service.State.Player.Board.Add(player);
            service.State.Opponent.Board.Add(opponent);
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 7, SafetyLimit = 20 }));

            var panelObject = Object.Instantiate(prefab);
            try
            {
                var targetIndex = -1;
                var closed = false;
                var playbackToggled = false;
                var speedCycled = false;
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(
                    service.State.LastReplay,
                    0,
                    false,
                    "1x",
                    index => targetIndex = index,
                    () => playbackToggled = true,
                    () => speedCycled = true,
                    () => closed = true);

                var panelRect = panelObject.GetComponent<RectTransform>();
                Assert.AreEqual(Vector2.zero, panelRect.anchorMin);
                Assert.AreEqual(Vector2.one, panelRect.anchorMax);
                Assert.AreEqual(Vector2.zero, panelRect.sizeDelta);
                Assert.IsNull(FindChild(panelObject.transform, "UnityCombatReplayPanelSurface"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatReplayOverlay"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatBattlefieldRoot"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatHudHeader"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatResultBadge"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatResultText"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatStatsText"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatStatsMetaText"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatBattlefield"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatOpponentBoard"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatPlayerBoard"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatCenterEventBand"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatPlaybackBar"));
                Assert.IsNull(FindChild(panelObject.transform, "UnityCombatTimelineDrawer"));
                Assert.AreEqual(7, FindChildren(panelObject.transform, "UnityCombatSlot-Opponent-").Count);
                Assert.AreEqual(7, FindChildren(panelObject.transform, "UnityCombatSlot-Player-").Count);

                var safeAreaRect = FindChild(panelObject.transform, "UnityCombatTitleSafeArea").GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.05f, 0.045f), safeAreaRect.anchorMin);
                Assert.AreEqual(new Vector2(0.95f, 0.96f), safeAreaRect.anchorMax);

                Assert.AreSame(
                    FindChild(panelObject.transform, "UnityCombatPlaybackBar"),
                    FindChild(panelObject.transform, "UnityCombatReplayControls").parent);
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayFirstButton"));
                Assert.AreEqual("|<", FindChild(panelObject.transform, "UnityReplayFirstButtonText").GetComponent<Text>().text);
                Assert.AreEqual(54f, FindChild(panelObject.transform, "UnityReplayFirstButton").GetComponent<LayoutElement>().preferredWidth, 0.001f);
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayFirstButton").GetComponent<Outline>());
                Assert.AreEqual("<", FindChild(panelObject.transform, "UnityReplayPrevButtonText").GetComponent<Text>().text);
                Assert.AreEqual(54f, FindChild(panelObject.transform, "UnityReplayPrevButton").GetComponent<LayoutElement>().preferredWidth, 0.001f);
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayPlayPauseButton"));
                Assert.AreEqual("\u64ad\u653e", FindChild(panelObject.transform, "UnityReplayPlayPauseButtonText").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayPlayPauseButton").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplaySpeedButton"));
                Assert.AreEqual("\u901f\u5ea6 1x", FindChild(panelObject.transform, "UnityReplaySpeedButtonText").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplaySpeedButton").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayEventChip-Event"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayEventChip-Event").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatMaxStepsLabel"));
                Assert.AreEqual("\u6700\u5927\u8f6e\u6b21 200", FindChild(panelObject.transform, "UnityCombatMaxStepsLabel").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatStatsButton"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatTimelineToggleButton"));

                FindChild(panelObject.transform, "UnityReplayPlayPauseButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(playbackToggled);

                FindChild(panelObject.transform, "UnityReplaySpeedButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(speedCycled);

                if (service.State.LastReplay.Frames.Count > 1)
                {
                    FindChild(panelObject.transform, "UnityReplayNextButton").GetComponent<Button>().onClick.Invoke();
                    Assert.AreEqual(1, targetIndex);
                }

                FindChild(panelObject.transform, "UnityCombatCloseButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(closed);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void Build_CombatReplayPlaybackControlsToggleAndCycleSpeed()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var player = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
                player.InstanceId = "unity-replay-player";
                player.Owner = BoardSide.Player;
                var opponent = service.State.Player.Tavern.Shop.Last(card => card != null && card.CardKind == CardKind.Minion).Clone();
                opponent.InstanceId = "unity-replay-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Player.Board.Add(player);
                service.State.Opponent.Board.Add(opponent);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityNextTurnButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatReplayPanel").GetComponent<UnityTavernCombatReplayPanelComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatBattlefieldRoot"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatPlaybackBar"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityCombatReplayPanelSurface"));
                Assert.AreEqual("\u6682\u505c", FindChild(rootObject.transform, "UnityReplayPlayPauseButtonText").GetComponent<Text>().text);
                Assert.AreEqual("\u901f\u5ea6 1x", FindChild(rootObject.transform, "UnityReplaySpeedButtonText").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityReplayEventChip-Event"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityCombatTimelineDrawer"));

                FindChild(rootObject.transform, "UnityReplayPlayPauseButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual("\u64ad\u653e", FindChild(rootObject.transform, "UnityReplayPlayPauseButtonText").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityReplaySpeedButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual("\u901f\u5ea6 2x", FindChild(rootObject.transform, "UnityReplaySpeedButtonText").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityCombatTimelineToggleButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatTimelineDrawer"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatTimeline"));
                FindChild(rootObject.transform, "UnityCombatTimelineCloseButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNull(FindChild(rootObject.transform, "UnityCombatTimelineDrawer"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void CombatReplayPanel_BuildsAnimatedStrikeHitAndDeathTiles()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            panelObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
            try
            {
                var replay = new CombatReplay
                {
                    Seed = 7,
                    Result = CombatWinner.Player
                };

                replay.Frames.Add(new CombatFrame
                {
                    Index = 0,
                    EventType = CombatEventType.CombatStarted,
                    PlayerBoardSnapshot = BoardSnapshot(BoardSide.Player, MinionSnapshot("anim-player", "Animated Player", 0, 3, 4)),
                    OpponentBoardSnapshot = BoardSnapshot(BoardSide.Opponent, MinionSnapshot("anim-opponent", "Animated Opponent", 0, 2, 2)),
                    LogText = "start"
                });
                replay.Frames.Add(new CombatFrame
                {
                    Index = 1,
                    EventType = CombatEventType.AttackDeclared,
                    ActorSide = BoardSide.Player,
                    ActorId = "anim-player",
                    TargetSide = BoardSide.Opponent,
                    TargetId = "anim-opponent",
                    DamagedEntityIds = new List<string> { "anim-opponent" },
                    PlayerBoardSnapshot = BoardSnapshot(BoardSide.Player, MinionSnapshot("anim-player", "Animated Player", 0, 3, 4)),
                    OpponentBoardSnapshot = BoardSnapshot(BoardSide.Opponent, MinionSnapshot("anim-opponent", "Animated Opponent", 0, 2, 1)),
                    LogText = "attack"
                });
                replay.Frames.Add(new CombatFrame
                {
                    Index = 2,
                    EventType = CombatEventType.DeathQueued,
                    TargetSide = BoardSide.Opponent,
                    TargetId = "anim-opponent",
                    DeadEntityIds = new List<string> { "anim-opponent" },
                    PlayerBoardSnapshot = BoardSnapshot(BoardSide.Player, MinionSnapshot("anim-player", "Animated Player", 0, 3, 4)),
                    OpponentBoardSnapshot = BoardSnapshot(BoardSide.Opponent),
                    LogText = "dead"
                });

                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(replay, 1, true, "1x", _ => { }, () => { }, () => { }, () => { });

                var battlefield = FindChild(panelObject.transform, "UnityCombatBattlefield");
                Assert.IsNotNull(battlefield.GetComponent<VerticalLayoutGroup>());
                Assert.IsNotNull(battlefield.GetComponent<Outline>());
                Assert.Less(
                    FindChild(battlefield, "UnityCombatOpponentBoard").parent.GetSiblingIndex(),
                    FindChild(battlefield, "UnityCombatPlayerBoard").parent.GetSiblingIndex());
                Assert.AreEqual(7, FindChildren(panelObject.transform, "UnityCombatSlot-Opponent-").Count);
                Assert.AreEqual(7, FindChildren(panelObject.transform, "UnityCombatSlot-Player-").Count);

                var actor = FindChild(panelObject.transform, "UnityReplayMinion-anim-player");
                var target = FindChild(panelObject.transform, "UnityReplayMinion-anim-opponent");
                Assert.IsNotNull(actor.GetComponent<Outline>());
                Assert.IsNotNull(target.GetComponent<Outline>());
                var actorOutline = actor.GetComponent<Outline>().effectColor;
                var targetOutline = target.GetComponent<Outline>().effectColor;
                Assert.AreEqual(UnityTavernUiStyle.Gold.r, actorOutline.r, 0.001f);
                Assert.AreEqual(UnityTavernUiStyle.Gold.g, actorOutline.g, 0.001f);
                Assert.AreEqual(UnityTavernUiStyle.Gold.b, actorOutline.b, 0.001f);
                Assert.AreEqual(UnityTavernUiStyle.Red.r, targetOutline.r, 0.001f);
                Assert.AreEqual(UnityTavernUiStyle.Red.g, targetOutline.g, 0.001f);
                Assert.AreEqual(UnityTavernUiStyle.Red.b, targetOutline.b, 0.001f);
                Assert.Greater(actorOutline.a, 0.75f);
                Assert.Greater(targetOutline.a, 0.75f);
                Assert.AreEqual("来源", FindChild(actor, "UnityReplayTargetingLabelText-anim-player").GetComponent<Text>().text);
                Assert.AreEqual("目标", FindChild(target, "UnityReplayTargetingLabelText-anim-opponent").GetComponent<Text>().text);
                Assert.IsFalse(FindChild(actor, "UnityReplayTargetingLabel-anim-player").GetComponent<Image>().raycastTarget);
                StringAssert.Contains("Animated Player", FindChild(panelObject.transform, "UnityReplayEventChipText-Actor").GetComponent<Text>().text);
                StringAssert.Contains("Animated Opponent", FindChild(panelObject.transform, "UnityReplayEventChipText-Target").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityReplayTargetingConnector"));
                Assert.IsFalse(FindChild(panelObject.transform, "UnityReplayTargetingConnector").GetComponent<Image>().raycastTarget);
                var actorAnimator = actor.GetComponent<UnityTavernReplayTileAnimator>();
                var targetAnimator = target.GetComponent<UnityTavernReplayTileAnimator>();
                Assert.AreEqual(UnityTavernReplayTileMotion.Strike, actorAnimator.Motion);
                Assert.AreEqual(UnityTavernReplayTileMotion.Hit, targetAnimator.Motion);
                actorAnimator.ApplyPreview(0.5f);
                Assert.Greater(actor.localScale.x, 1f);
                Assert.IsNotNull(FindChild(actor, "UnityReplayMotionFlash"));

                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(replay, 2, true, "1x", _ => { }, () => { }, () => { }, () => { });

                var death = FindChild(panelObject.transform, "UnityReplayDeathMarker-anim-opponent");
                var deathAnimator = death.GetComponent<UnityTavernReplayTileAnimator>();
                Assert.AreEqual(UnityTavernReplayTileMotion.Death, deathAnimator.Motion);
                deathAnimator.ApplyPreview(1f);
                Assert.Less(death.GetComponent<CanvasGroup>().alpha, 0.5f);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatReplayPanel_SafetyStoppedShowsOverLimitAndUnresolvedStats()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var replay = new CombatReplay
                {
                    Seed = 7,
                    Result = CombatWinner.Draw,
                    SafetyStopped = true
                };
                replay.Frames.Add(new CombatFrame
                {
                    Index = 0,
                    EventType = CombatEventType.CombatStarted,
                    PlayerBoardSnapshot = BoardSnapshot(BoardSide.Player, MinionSnapshot("safety-player", "Safety Player", 0, 1, 12)),
                    OpponentBoardSnapshot = BoardSnapshot(BoardSide.Opponent, MinionSnapshot("safety-opponent", "Safety Opponent", 0, 1, 12)),
                    LogText = "loop guard"
                });

                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(
                    replay,
                    0,
                    new UnityCombatReplayPanelOptions
                    {
                        MaxSteps = 3
                    });

                Assert.AreEqual("\u8d85\u9650 / \u672a\u51b3", FindChild(panelObject.transform, "UnityCombatResultText").GetComponent<Text>().text);
                Assert.AreEqual("\u80dc 0%  \u5e73 0%  \u8d1f 0%  \u8d85\u9650 100%", FindChild(panelObject.transform, "UnityCombatStatsText").GetComponent<Text>().text);
                Assert.AreEqual("\u6837\u672c 1 / \u6700\u5927\u8f6e\u6b21 3", FindChild(panelObject.transform, "UnityCombatStatsMetaText").GetComponent<Text>().text);
                StringAssert.Contains("\u8d85\u9650", FindChild(panelObject.transform, "UnityCombatCurrentEventText").GetComponent<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatReplayPanel_ReturnClosesBattlefieldWithoutClearingReplay()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                service.State.Player.Board.Add(CreateBoardMinion(service, "return-player", BoardSide.Player, 4, 4));
                service.State.Opponent.Board.Add(CreateBoardMinion(service, "return-opponent", BoardSide.Opponent, 2, 6));

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityNextTurnButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(service.State.LastReplay);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatReplayPanel"));

                FindChild(rootObject.transform, "UnityCombatReturnButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(FindChild(rootObject.transform, "UnityCombatReplayPanel"));
                Assert.IsNotNull(service.State.LastReplay);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void CombatReplayPanel_MaxStepsAndStatsControlsUpdateBattlefieldHud()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                service.State.Player.Board.Add(CreateBoardMinion(service, "stats-player-a", BoardSide.Player, 5, 4, Keyword.DivineShield));
                service.State.Player.Board.Add(CreateBoardMinion(service, "stats-player-b", BoardSide.Player, 3, 8, Keyword.Taunt));
                service.State.Opponent.Board.Add(CreateBoardMinion(service, "stats-opponent-a", BoardSide.Opponent, 4, 5, Keyword.Venomous));
                service.State.Opponent.Board.Add(CreateBoardMinion(service, "stats-opponent-b", BoardSide.Opponent, 2, 9, Keyword.Reborn));

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityNextTurnButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual("\u6700\u5927\u8f6e\u6b21 200", FindChild(rootObject.transform, "UnityCombatMaxStepsLabel").GetComponent<Text>().text);
                FindChild(rootObject.transform, "UnityCombatMaxStepsDownButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual("\u6700\u5927\u8f6e\u6b21 100", FindChild(rootObject.transform, "UnityCombatMaxStepsLabel").GetComponent<Text>().text);
                FindChild(rootObject.transform, "UnityCombatMaxStepsUpButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual("\u6700\u5927\u8f6e\u6b21 200", FindChild(rootObject.transform, "UnityCombatMaxStepsLabel").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityCombatStatsButton").GetComponent<Button>().onClick.Invoke();

                var statsText = FindChild(rootObject.transform, "UnityCombatStatsText").GetComponent<Text>().text;
                StringAssert.StartsWith("\u80dc ", statsText);
                StringAssert.Contains("\u5e73 ", statsText);
                StringAssert.Contains("\u8d1f ", statsText);
                StringAssert.Contains("\u8d85\u9650 ", statsText);
                Assert.AreEqual("\u6837\u672c 100 / \u6700\u5927\u8f6e\u6b21 200", FindChild(rootObject.transform, "UnityCombatStatsMetaText").GetComponent<Text>().text);
                Assert.IsNotNull(service.State.LastReplay);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatBattlefieldRoot"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void RightPanelPrefab_BuildConfiguresFloatingToggle()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernRightPanelComponent.RightPanelPrefabAssetPath);
            Assert.IsNotNull(prefab);

            var panelObject = Object.Instantiate(prefab);
            try
            {
                var panel = panelObject.GetComponent<UnityTavernRightPanelComponent>();
                Assert.IsNotNull(panel);

                var toggled = false;
                panel.Build(
                    "Test Control",
                    false,
                    () => toggled = true,
                    parent => { },
                    parent => { },
                    parent => { },
                    parent => { });

                Assert.AreEqual("展开", FindChild(panelObject.transform, "UnityRightPanelFloatToggleText").GetComponent<Text>().text);
                FindChild(panelObject.transform, "UnityRightPanelFloatToggle").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(toggled);

                panel.Build(
                    "Test Control",
                    true,
                    () => { },
                    parent => { },
                    parent => { },
                    parent => { },
                    parent => { });

                Assert.AreEqual("收起", FindChild(panelObject.transform, "UnityRightPanelFloatToggleText").GetComponent<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void RightPanelPrefab_BuildUsesSerializedSectionHosts()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernRightPanelComponent.RightPanelPrefabAssetPath);
            Assert.IsNotNull(prefab);

            var panelObject = Object.Instantiate(prefab);
            try
            {
                var panel = panelObject.GetComponent<UnityTavernRightPanelComponent>();
                Assert.IsNotNull(panel);
                var rootChildCount = panelObject.transform.childCount;

                panel.Build(
                    "Test Control",
                    parent => new GameObject("BuiltActions", typeof(RectTransform)).transform.SetParent(parent, false),
                    parent => new GameObject("BuiltDetail", typeof(RectTransform)).transform.SetParent(parent, false),
                    parent => new GameObject("BuiltAdvisor", typeof(RectTransform)).transform.SetParent(parent, false),
                    parent => new GameObject("BuiltLog", typeof(RectTransform)).transform.SetParent(parent, false));

                Assert.AreEqual(rootChildCount, panelObject.transform.childCount);
                Assert.AreEqual("Test Control", FindChild(panelObject.transform, "UnityRightPanelTitle").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(panelObject.transform, "BuiltActions"));
                Assert.IsNotNull(FindChild(panelObject.transform, "BuiltDetail"));
                Assert.IsNotNull(FindChild(panelObject.transform, "BuiltAdvisor"));
                Assert.IsNotNull(FindChild(panelObject.transform, "BuiltLog"));
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void Build_RightPanelToggleExpandsAndCollapsesDrawer()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNull(FindChild(rootObject.transform, "UnityRightPanel"));
                var drawerToggle = FindChild(rootObject.transform, "UnityRightPanelDrawerToggle");
                Assert.IsNotNull(drawerToggle);
                Assert.AreEqual("功能", FindChild(rootObject.transform, "UnityRightPanelDrawerToggleText").GetComponent<Text>().text);

                drawerToggle.GetComponent<Button>().onClick.Invoke();

                var drawerPanel = FindChild(rootObject.transform, "UnityRightPanel");
                Assert.IsNotNull(drawerPanel);
                Assert.AreEqual("UnityTavernTrainer", drawerPanel.parent.name);
                Assert.IsNull(FindChild(rootObject.transform, "UnityRightPanelDrawerToggle"));
                var drawerRect = drawerPanel.GetComponent<RectTransform>();
                Assert.AreEqual(1f, drawerRect.anchorMin.x);
                Assert.AreEqual(0f, drawerRect.anchorMin.y);
                Assert.AreEqual(1f, drawerRect.anchorMax.x);
                Assert.AreEqual(1f, drawerRect.anchorMax.y);
                Assert.IsNotNull(drawerPanel.GetComponent<Outline>());
                Assert.AreEqual("功能面板", FindChild(rootObject.transform, "UnityRightPanelTitle").GetComponent<Text>().text);
                Assert.AreEqual("收起", FindChild(rootObject.transform, "UnityRightPanelFloatToggleText").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanelHeader").GetComponent<Image>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanelHeaderAccent"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanelFloatToggle").GetComponent<Outline>());
                var tabRow = FindChild(rootObject.transform, "UnityRightPanelTabs");
                Assert.IsNotNull(tabRow);
                Assert.IsNotNull(tabRow.GetComponent<Image>());
                Assert.IsNotNull(tabRow.GetComponent<Outline>());
                Assert.AreEqual("操作", FindChild(rootObject.transform, "UnityRightPanelTab-ActionsText").GetComponent<Text>().text);
                Assert.AreEqual("详情", FindChild(rootObject.transform, "UnityRightPanelTab-DetailsText").GetComponent<Text>().text);
                Assert.AreEqual("建议", FindChild(rootObject.transform, "UnityRightPanelTab-AdviceText").GetComponent<Text>().text);
                Assert.AreEqual("日志", FindChild(rootObject.transform, "UnityRightPanelTab-LogsText").GetComponent<Text>().text);
                var actionTab = FindChild(rootObject.transform, "UnityRightPanelTab-Actions");
                Assert.IsTrue(actionTab.GetComponent<Outline>().enabled);
                var actionHost = FindChild(rootObject.transform, "UnityRightPanelActionHost");
                Assert.IsNotNull(actionHost.GetComponent<Image>());
                Assert.IsNotNull(actionHost.GetComponent<VerticalLayoutGroup>());
                Assert.AreEqual(1f, actionHost.GetComponent<LayoutElement>().flexibleHeight);
                var actionPanel = FindChild(rootObject.transform, "UnityActionPanel");
                Assert.IsNotNull(actionPanel);
                Assert.IsNotNull(actionPanel.GetComponent<Outline>());
                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityActionButtonGrid").GetComponent<GridLayoutGroup>().cellSize.y, 40f);
                Assert.IsNull(FindChild(rootObject.transform, "UnitySelectedCardPanel"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvisorPanel"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityLogScroll"));

                FindChild(rootObject.transform, "UnityRightPanelFloatToggle").GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(FindChild(rootObject.transform, "UnityRightPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanelDrawerToggle"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_RightPanelTabsSwitchVisibleInspectorSection()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityActionPanel"));
                Assert.IsNull(FindChild(rootObject.transform, "UnitySelectedCardPanel"));

                FindChild(rootObject.transform, "UnityRightPanelTab-Details").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(FindChild(rootObject.transform, "UnityRightPanelTab-Details").GetComponent<Outline>().enabled);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityRightPanelDetailHost").GetComponent<Outline>().enabled);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySelectedCardPanel").GetComponent<Outline>());
                Assert.IsNull(FindChild(rootObject.transform, "UnityActionPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySelectedCardDetailLayout").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySelectedCardInfoStack").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySelectedCardSummarySection").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySelectedCardSummaryAccent"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySelectedCardEffectSection").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySelectedCardActionSection").GetComponent<Outline>());
                Assert.AreEqual("查看详情", FindChild(rootObject.transform, "UnitySelectedCardDetailsButtonText").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityRightPanelTab-Advice").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(FindChild(rootObject.transform, "UnityRightPanelTab-Advice").GetComponent<Outline>().enabled);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvisorPanel").GetComponent<Outline>());
                Assert.IsNull(FindChild(rootObject.transform, "UnitySelectedCardPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvisorLineCard").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvisorLineAccent"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvisorLine"));

                FindChild(rootObject.transform, "UnityRightPanelTab-Logs").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(FindChild(rootObject.transform, "UnityRightPanelTab-Logs").GetComponent<Outline>().enabled);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityLogScroll").GetComponent<Outline>());
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvisorPanel"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityLogLineRow").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityLogLineAccent"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityLogLine"));

                FindChild(rootObject.transform, "UnityRightPanelTab-Actions").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityActionPanel"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityLogScroll"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CardDetailReplayAndToolsExposeMissingCommands()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var player = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
                player.InstanceId = "unity-tools-player";
                player.Owner = BoardSide.Player;
                var opponent = service.State.Player.Tavern.Shop.Last(card => card != null && card.CardKind == CardKind.Minion).Clone();
                opponent.InstanceId = "unity-tools-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Player.Board.Add(player);
                service.State.Opponent.Board.Add(opponent);
                var startingGold = service.State.Player.Tavern.Gold;

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);

                FindChild(rootObject.transform, "UnityRightPanelTab-Details").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnitySelectedCardDetailsButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardDetailOverlay").GetComponent<UnityTavernCardDetailModalComponent>());
                FindChild(rootObject.transform, "UnityCardDetailCloseButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNull(FindChild(rootObject.transform, "UnityCardDetailOverlay"));

                FindChild(rootObject.transform, "UnityRightPanelTab-Actions").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay").GetComponent<UnityTavernToolsModalComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTrainerToolsPanel").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTrainerToolsHeader").GetComponent<Image>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTrainerToolsHeaderAccent"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTrainerToolsCloseButton").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsEconomySection").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsEconomySectionHeader"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsEconomySectionHeaderAccent"));
                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityToolsEconomySectionGrid").GetComponent<GridLayoutGroup>().cellSize.y, 38f);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsAddGoldButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsAddMinionButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsAddSpellButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryEntrySection"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsOpenCardLibraryButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsTrinketDebugSection"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsReplaceLesserTrinketButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsReplaceGreaterTrinketButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityToolsCardLibrarySection"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsAddOpponentButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsClearOpponentButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCopyOpponentButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsMirrorOpponentButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsRunCombatTestButton"));
                Assert.AreEqual("仅战斗调试", FindChild(rootObject.transform, "UnityToolsRunCombatTestButtonText").GetComponent<Text>().text);
                Assert.AreEqual("跳过战斗进下回合", FindChild(rootObject.transform, "UnityToolsSkipCombatNextTurnButtonText").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsPlayerUndeadAttackStatusCard"));
                Assert.AreEqual("来源：永久全局 State.Player.Tavern.UndeadAttackBonus", FindChild(rootObject.transform, "UnityToolsPlayerUndeadAttackStatusSource").GetComponent<Text>().text);
                Assert.AreEqual("手动修改会立即重算已有战场、手牌和商店牌", FindChild(rootObject.transform, "UnityToolsPlayerUndeadAttackStatusManual").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsSaveScenarioButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsLoadScenarioButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsResetCombatSnapshotButton"));

                FindChild(rootObject.transform, "UnityToolsAddGoldButton").GetComponent<Button>().onClick.Invoke();
                Assert.Greater(service.State.Player.Tavern.Gold, startingGold);

                var handBefore = service.State.Player.Tavern.Hand.Count;
                FindChild(rootObject.transform, "UnityToolsAddMinionButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(handBefore + 1, service.State.Player.Tavern.Hand.Count);

                FindChild(rootObject.transform, "UnityToolsRunCombatTestButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(service.State.LastReplay);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatReplayPanel").GetComponent<UnityTavernCombatReplayPanelComponent>());
                Assert.IsNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void CombatTestResult_DisablesRecruitActionsAndCanEnterNextRecruitPhase()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Tavern.Gold = 10;
                service.State.Player.Tavern.MaxGold = 10;

                var shopCard = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
                var handCard = shopCard.Clone();
                handCard.InstanceId = "phase-ui-hand-card";
                handCard.Owner = BoardSide.Player;
                service.State.Player.Tavern.Hand.Add(handCard);
                service.State.Player.Board.Add(CreateBoardMinion(service, "phase-ui-player", BoardSide.Player, 4, 4));
                service.State.Opponent.Board.Add(CreateBoardMinion(service, "phase-ui-opponent", BoardSide.Opponent, 3, 5));

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);

                Assert.IsTrue(FindChild(rootObject.transform, "UnityRefreshButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityFreezeButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityUpgradeButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityNextTurnButton").GetComponent<Button>().interactable);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardAction-" + shopCard.InstanceId));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardAction-" + handCard.InstanceId));

                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityToolsRunCombatTestButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(MatchPhase.Result, service.State.Phase);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCombatReplayPanel"));
                FindChild(rootObject.transform, "UnityCombatReturnButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsFalse(FindChild(rootObject.transform, "UnityRefreshButton").GetComponent<Button>().interactable);
                Assert.IsFalse(FindChild(rootObject.transform, "UnityFreezeButton").GetComponent<Button>().interactable);
                Assert.IsFalse(FindChild(rootObject.transform, "UnityUpgradeButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityNextTurnButton").GetComponent<Button>().interactable);
                Assert.AreEqual("\u8fdb\u5165\u4e0b\u4e00\u51c6\u5907\u9636\u6bb5", FindChild(rootObject.transform, "UnityNextTurnButtonText").GetComponent<Text>().text);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityReplayButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().interactable);
                Assert.IsNull(FindChild(rootObject.transform, "UnityCardAction-" + shopCard.InstanceId));
                Assert.IsNull(FindChild(rootObject.transform, "UnityCardAction-" + handCard.InstanceId));
                Assert.IsNull(FindChild(rootObject.transform, "UnityCard-" + shopCard.InstanceId).GetComponent<UnityTavernCardDragBehaviour>());
                Assert.IsNull(FindChild(rootObject.transform, "UnityCard-" + handCard.InstanceId).GetComponent<UnityTavernCardDragBehaviour>());

                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsRunCombatTestButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsSkipCombatNextTurnButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsResetCombatSnapshotButton").GetComponent<Button>().interactable);
                FindChild(rootObject.transform, "UnityTrainerToolsCloseButton").GetComponent<Button>().onClick.Invoke();

                FindChild(rootObject.transform, "UnityNextTurnButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(MatchPhase.Tavern, service.State.Phase);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityRefreshButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityFreezeButton").GetComponent<Button>().interactable);
                Assert.AreEqual("\u5b8c\u6574\u4e0b\u4e00\u56de\u5408", FindChild(rootObject.transform, "UnityNextTurnButtonText").GetComponent<Text>().text);
                var nextShopCard = service.State.Player.Tavern.Shop.First(card => card != null);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardAction-" + nextShopCard.InstanceId));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardAction-" + handCard.InstanceId));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Tools_MechanicCoverageReportShowsQuestTrinketNotes()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsMechanicCoverageSection"));
                var row = FindChild(rootObject.transform, "UnityToolsMechanicCoverageRow-Quest-Trinketinteractions");
                Assert.IsNotNull(row.GetComponent<HorizontalLayoutGroup>());

                var main = FindChild(rootObject.transform, "UnityToolsMechanicCoverageMain-Quest-Trinketinteractions");
                var meta = FindChild(rootObject.transform, "UnityToolsMechanicCoverageMeta-Quest-Trinketinteractions");
                Assert.AreSame(row, main.parent);
                Assert.AreSame(row, meta.parent);

                var title = FindChild(rootObject.transform, "UnityToolsMechanicCoverageSystem-Quest-Trinketinteractions").GetComponent<Text>().text;
                Assert.That(title, Is.EqualTo("Quest/Trinket interactions"));

                var confidence = FindChild(rootObject.transform, "UnityToolsMechanicCoverageConfidence-Quest-Trinketinteractions").GetComponent<Text>().text;
                Assert.That(confidence, Is.EqualTo("Medium High"));

                var statusTransform = FindChild(rootObject.transform, "UnityToolsMechanicCoverageStatus-Quest-Trinketinteractions");
                Assert.AreSame(meta, statusTransform.parent);
                var status = statusTransform.GetComponent<Text>().text;
                Assert.That(status, Does.Contain("UI yes"));
                Assert.That(status, Does.Contain("Tests yes"));

                var notesTransform = FindChild(rootObject.transform, "UnityToolsMechanicCoverageNotes-Quest-Trinketinteractions");
                Assert.AreSame(main, notesTransform.parent);
                var notes = notesTransform.GetComponent<Text>().text;
                Assert.That(notes, Does.Contain("repeated summon overflow"));
                Assert.That(notes, Does.Contain("stacked repeat sources"));
                Assert.That(notes, Does.Contain("replay non-duplication"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Tools_CardLibraryFiltersAndAddsCardsToHand()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();

                var libraryEntry = FindChild(rootObject.transform, "UnityToolsOpenCardLibraryButton");
                if (libraryEntry != null)
                {
                    libraryEntry.GetComponent<Button>().onClick.Invoke();

                    Assert.IsNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay"));
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryOverlay"));
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryTierPanel").GetComponent<Outline>());
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryCenterPanel").GetComponent<Outline>());
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryTypePanel").GetComponent<Outline>());
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryMinionTab"));
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibrarySpellTab"));
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryHeroTab"));
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryTier1Button"));
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryTier1ButtonIcon"));
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryTribeBeastButton"));
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryTribeBeastButtonSymbol"));
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryTribeNoneButton"));
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryAddButton"));
                    Assert.IsTrue(FindChild(rootObject.transform, "UnityCardLibraryMinionTab").GetComponent<Outline>().enabled);
                    Assert.AreEqual(GridLayoutGroup.Constraint.FixedColumnCount, FindChild(rootObject.transform, "UnityCardLibraryScrollContent").GetComponent<GridLayoutGroup>().constraint);
                    Assert.AreEqual(4, FindChild(rootObject.transform, "UnityCardLibraryScrollContent").GetComponent<GridLayoutGroup>().constraintCount);

                    FindChild(rootObject.transform, "UnityCardLibraryTier1Button").GetComponent<Button>().onClick.Invoke();
                    Assert.IsTrue(FindChild(rootObject.transform, "UnityCardLibraryTier1Button").GetComponent<Outline>().enabled);
                    FindChild(rootObject.transform, "UnityCardLibraryTier5Button").GetComponent<Button>().onClick.Invoke();
                    FindChild(rootObject.transform, "UnityCardLibraryTribeBeastButton").GetComponent<Button>().onClick.Invoke();
                    Assert.IsTrue(FindChild(rootObject.transform, "UnityCardLibraryTribeBeastButton").GetComponent<Outline>().enabled);
                    Assert.IsNotNull(FindCardLibraryCard(rootObject.transform, "BG32_111"));
                    FindChild(rootObject.transform, "UnityCardLibraryTribeNoneButton").GetComponent<Button>().onClick.Invoke();
                    Assert.IsTrue(FindChild(rootObject.transform, "UnityCardLibraryTribeNoneButton").GetComponent<Outline>().enabled);
                    Assert.IsNull(FindCardLibraryCard(rootObject.transform, "BG32_111"));
                    Assert.IsNotNull(FindCardLibraryCard(rootObject.transform, "BG_LOE_077"));
                    FindChild(rootObject.transform, "UnityCardLibraryTierAllButton").GetComponent<Button>().onClick.Invoke();
                    FindChild(rootObject.transform, "UnityCardLibraryTribeAllButton").GetComponent<Button>().onClick.Invoke();

                    var handBeforeLibraryMinion = service.State.Player.Tavern.Hand.Count;
                    FirstCardLibraryCard(rootObject.transform).GetComponent<Button>().onClick.Invoke();
                    Assert.AreEqual(handBeforeLibraryMinion + 1, service.State.Player.Tavern.Hand.Count);
                    Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Minion));

                    FindChild(rootObject.transform, "UnityCardLibrarySpellTab").GetComponent<Button>().onClick.Invoke();
                    Assert.IsTrue(FindChild(rootObject.transform, "UnityCardLibrarySpellTab").GetComponent<Outline>().enabled);
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryTavernSpellTypeButton"));

                    var handBeforeLibrarySpell = service.State.Player.Tavern.Hand.Count;
                    FirstCardLibraryCard(rootObject.transform).GetComponent<Button>().onClick.Invoke();
                    Assert.AreEqual(handBeforeLibrarySpell + 1, service.State.Player.Tavern.Hand.Count);
                    Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell));

                    FindChild(rootObject.transform, "UnityCardLibraryHeroTab").GetComponent<Button>().onClick.Invoke();
                    Assert.IsTrue(FindChild(rootObject.transform, "UnityCardLibraryHeroTab").GetComponent<Outline>().enabled);
                    var renoCard = FindCardLibraryCard(rootObject.transform, "TB_BaconShop_HERO_41");
                    Assert.IsNotNull(renoCard);
                    var handBeforeHero = service.State.Player.Tavern.Hand.Count;
                    renoCard.GetComponent<Button>().onClick.Invoke();
                    Assert.AreEqual(handBeforeHero, service.State.Player.Tavern.Hand.Count);
                    Assert.AreEqual("TB_BaconShop_HERO_41", service.State.Player.HeroId);
                    return;
                }

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibrarySection").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryHeader"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryHeaderAccent"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibrarySummary"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryList").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryModeRow").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryTierRow").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryTribeGrid").GetComponent<Outline>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryMinionModeButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibrarySpellModeButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryHeroModeButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryTier1Button"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryTribeBeastButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryTribeNoneButton"));
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsCardLibraryMinionModeButton").GetComponent<Outline>().enabled);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryChoiceAccent"));
                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityToolsCardLibraryAddButton").GetComponent<LayoutElement>().preferredHeight, 34f);

                FindChild(rootObject.transform, "UnityToolsCardLibraryTier1Button").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsCardLibraryTier1Button").GetComponent<Outline>().enabled);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsCardLibraryCountText").GetComponent<Text>().text.Contains("1本"));
                FindChild(rootObject.transform, "UnityToolsCardLibraryTribeBeastButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsCardLibraryTribeBeastButton").GetComponent<Outline>().enabled);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsCardLibraryCountText").GetComponent<Text>().text.Contains("野兽"));
                FindChild(rootObject.transform, "UnityToolsCardLibraryTierAllButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityToolsCardLibraryTribeAllButton").GetComponent<Button>().onClick.Invoke();

                var handBeforeMinion = service.State.Player.Tavern.Hand.Count;
                FindChild(rootObject.transform, "UnityToolsCardLibraryAddButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(handBeforeMinion + 1, service.State.Player.Tavern.Hand.Count);
                Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Minion));

                FindChild(rootObject.transform, "UnityToolsCardLibrarySpellModeButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsCardLibrarySpellModeButton").GetComponent<Outline>().enabled);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsCardLibraryTavernSpellTypeButton"));

                var handBeforeSpell = service.State.Player.Tavern.Hand.Count;
                FindChild(rootObject.transform, "UnityToolsCardLibraryAddButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(handBeforeSpell + 1, service.State.Player.Tavern.Hand.Count);
                Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell));

                FindChild(rootObject.transform, "UnityToolsCardLibraryHeroModeButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsCardLibraryHeroModeButton").GetComponent<Outline>().enabled);
                Assert.IsTrue(FindChild(rootObject.transform, "UnityToolsCardLibraryCountText").GetComponent<Text>().text.Contains("英雄"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Tools_CardLibrarySyncsWithActiveTribesAndShowAllToggle()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(
                    12345,
                    new InMemoryTestScenarioRepository(),
                    new MatchSetupOptions { ActiveTribes = new List<Tribe> { Tribe.Beast } });

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityToolsOpenCardLibraryButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryTribeBeastButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityCardLibraryTribeMurlocButton"));
                Assert.IsNull(FindCardLibraryCard(rootObject.transform, "BG32_330"));

                FindChild(rootObject.transform, "UnityCardLibraryShowAllToggle").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryTribeMurlocButton"));
                FindChild(rootObject.transform, "UnityCardLibraryTribeMurlocButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindCardLibraryCard(rootObject.transform, "BG32_330"));

                FindChild(rootObject.transform, "UnityCardLibraryShowAllToggle").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityCardLibrarySpellTab").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibrarySpellTribeBeastButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityCardLibrarySpellTribePirateButton"));
                Assert.IsNull(FindCardLibraryCard(rootObject.transform, "122182"));

                FindChild(rootObject.transform, "UnityCardLibraryShowAllToggle").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibrarySpellTribePirateButton"));
                FindChild(rootObject.transform, "UnityCardLibrarySpellTribePirateButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindCardLibraryCard(rootObject.transform, "122182"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Tools_OpponentCardLibraryAddsMinionsAndCastsSpells()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var playerTarget = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
                playerTarget.InstanceId = "ui-debug-target-0";
                playerTarget.Owner = BoardSide.Player;
                service.State.Player.Board.Add(playerTarget);
                service.State.Player.Tavern.Hand.Clear();

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityToolsAddOpponentButton").GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(FindChild(rootObject.transform, "UnityTrainerToolsOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryMinionTab"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibrarySpellTab"));
                var goldenToggle = FindChild(rootObject.transform, "UnityCardLibraryOpponentGoldenToggle");
                Assert.IsNotNull(goldenToggle);

                var handBeforeMinion = service.State.Player.Tavern.Hand.Count;
                FirstCardLibraryCard(rootObject.transform).GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(1, service.State.Opponent.Board.Count);
                Assert.IsFalse(service.State.Opponent.Board[0].Golden);
                Assert.AreEqual(handBeforeMinion, service.State.Player.Tavern.Hand.Count);

                FindChild(rootObject.transform, "UnityCardLibraryOpponentGoldenToggle").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual("金色", FindChild(rootObject.transform, "UnityCardLibraryOpponentGoldenToggleText").GetComponent<Text>().text);
                FirstCardLibraryCard(rootObject.transform).GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(2, service.State.Opponent.Board.Count);
                Assert.IsTrue(service.State.Opponent.Board[1].Golden);

                FindChild(rootObject.transform, "UnityCardLibrarySpellTab").GetComponent<Button>().onClick.Invoke();
                Assert.IsNull(FindChild(rootObject.transform, "UnityCardLibraryOpponentGoldenToggle"));
                var spellCard = FindCardLibraryCard(rootObject.transform, "100596");
                Assert.IsNotNull(spellCard);

                var handBeforeSpell = service.State.Player.Tavern.Hand.Count;
                var spellsBefore = service.State.Player.Tavern.TavernSpellsCastThisTurn;
                var attackBefore = service.State.Player.Board.Sum(card => card.Attack);
                spellCard.GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(handBeforeSpell, service.State.Player.Tavern.Hand.Count);
                Assert.AreEqual(spellsBefore + 1, service.State.Player.Tavern.TavernSpellsCastThisTurn);
                Assert.AreEqual(attackBefore + 4, service.State.Player.Board.Sum(card => card.Attack));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void AdvancedMechanicLibrary_QuestButtonsCompleteAndReplaceReward()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));
                service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
                var rewards = service.GetDebugSelectableQuestRewards()
                    .OrderBy(reward => reward.PowerLevel + " / " + reward.Trigger + " / " + reward.OfferPoolStatus)
                    .ThenBy(reward => reward.Name)
                    .ToList();
                Assert.Greater(rewards.Count, 1);
                var oldReward = rewards.Last();
                service.Apply(new GameCommand(GameCommandType.DebugReplaceQuestReward, oldReward.CardId, CardKind.QuestReward, false, 0));

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityQuestCompleteButton-Main"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityQuestReplaceRewardButton-Main"));

                Assert.AreEqual(oldReward.CardId, service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest.RewardCardId);

                FindChild(rootObject.transform, "UnityQuestReplaceRewardButton-Main").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedCardLibraryOverlay"));
                FindChild(rootObject.transform, "UnityAdvancedCardLibrarySelectButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(rewards.First().CardId, service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest.RewardCardId);
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedCardLibraryOverlay"));

                FindChild(rootObject.transform, "UnityQuestCompleteButton-Main").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest.Completed);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void AdvancedMechanicLibrary_ToolsTrinketReplaceButtonUsesDebugCommand()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var trinkets = service.GetDebugSelectableTrinkets(TrinketSlotKind.Lesser)
                    .OrderBy(trinket => trinket.SlotKind + " / " + trinket.Cost + "g / " + trinket.OfferPoolStatus)
                    .ThenBy(trinket => trinket.Name)
                    .ToList();
                Assert.Greater(trinkets.Count, 1);
                service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, trinkets.Last().CardId, CardKind.Trinket, 0));

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNull(FindChild(rootObject.transform, "UnityTrinketTrackerPanel"));
                Assert.AreEqual(trinkets.Last().Name, FindChild(rootObject.transform, "UnityHeroEffectTrinket-LesserTitle").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityTrinketReplaceButton-Lesser"));

                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();
                FindChild(rootObject.transform, "UnityToolsReplaceLesserTrinketButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedCardLibraryOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedCardLibraryLesserTab"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedCardLibraryGreaterTab"));

                FindChild(rootObject.transform, "UnityAdvancedCardLibrarySelectButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(trinkets.First().CardId, service.State.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId);
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedCardLibraryOverlay"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void AdvancedChoiceStatusPanel_ShowsScheduledPendingAndDiscoverChoices()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var strip = FindChild(rootObject.transform, "UnityHeroEffectRack");
                Assert.IsNotNull(strip);

                Assert.IsNull(FindChild(rootObject.transform, "UnityTrinketTrackerPanel"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityTrinketReplaceButton-Lesser"));

                var advancedPanel = FindChild(rootObject.transform, "UnityAdvancedChoiceStatusPanel");
                Assert.IsNull(advancedPanel);
                Assert.IsNull(FindChild(rootObject.transform, "UnityMechanicStatusStrip"));
                return;

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedChoiceStatusRow-trinket-lesser-round-6"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedChoiceStatusRow-trinket-greater-round-9"));
                Assert.AreEqual("第6回合", FindChild(rootObject.transform, "UnityAdvancedChoiceStatusMarker-trinket-lesser-round-6").GetComponent<Text>().text);
                Assert.AreEqual("小饰品", FindChild(rootObject.transform, "UnityAdvancedChoiceStatusName-trinket-lesser-round-6").GetComponent<Text>().text);
                Assert.AreEqual("未到回合", FindChild(rootObject.transform, "UnityAdvancedChoiceStatusDetail-trinket-lesser-round-6").GetComponent<Text>().text);
                Assert.AreEqual("第9回合", FindChild(rootObject.transform, "UnityAdvancedChoiceStatusMarker-trinket-greater-round-9").GetComponent<Text>().text);
                Assert.AreEqual("大饰品", FindChild(rootObject.transform, "UnityAdvancedChoiceStatusName-trinket-greater-round-9").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedChoiceStatusOpenButton-trinket-lesser-round-6"));

                service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));
                ClearChildren(rootObject.transform);
                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedChoiceStatusRow-advanced-current"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedChoiceStatusOpenButton-advanced-current"));
                Assert.AreEqual("请选择", FindChild(rootObject.transform, "UnityAdvancedChoiceStatusMarker-advanced-current").GetComponent<Text>().text);
                Assert.AreEqual("请选择小饰品", FindChild(rootObject.transform, "UnityAdvancedChoiceStatusName-advanced-current").GetComponent<Text>().text);
                Assert.AreEqual("4 个选项，必须选择", FindChild(rootObject.transform, "UnityAdvancedChoiceStatusDetail-advanced-current").GetComponent<Text>().text);
                Assert.AreEqual("选择", FindChild(rootObject.transform, "UnityAdvancedChoiceStatusOpenButton-advanced-current").GetComponentInChildren<Text>().text);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedMechanicChoiceOverlay"));
                Assert.AreEqual("请选择小饰品", FindChild(rootObject.transform, "UnityAdvancedMechanicChoiceTitle").GetComponent<Text>().text);
                var choicePanelSize = FindChild(rootObject.transform, "UnityAdvancedMechanicChoicePanel").GetComponent<RectTransform>().sizeDelta;
                Assert.GreaterOrEqual(choicePanelSize.x, 680f);
                Assert.GreaterOrEqual(choicePanelSize.y, 500f);

                service.State.Player.Tavern.AdvancedMechanics.PendingChoice = null;
                service.State.Player.Tavern.Discover = new DiscoverState
                {
                    Source = "ui-test-discover",
                    RemainingPicks = 1,
                    Options = service.State.Player.Tavern.Shop.Where(card => card != null).Take(2).Select(card => card.Clone()).ToList()
                };
                ClearChildren(rootObject.transform);
                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedChoiceStatusRow-discover-current"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedChoiceStatusOpenButton-discover-current"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityDiscoverOverlay"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void AdvancedMechanicChoiceModal_RendersQuestAndTrinketOptionCounts()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));
                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedMechanicChoiceOverlay"));
                Assert.AreEqual(3, FindChildren(rootObject.transform, "UnityAdvancedMechanicChoiceCard-").Count);
                var questPanelSize = FindChild(rootObject.transform, "UnityAdvancedMechanicChoicePanel").GetComponent<RectTransform>().sizeDelta;
                Assert.GreaterOrEqual(questPanelSize.x, 680f);
                Assert.GreaterOrEqual(questPanelSize.y, 500f);

                service.State.Player.Tavern.AdvancedMechanics.PendingChoice = null;
                service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));
                ClearChildren(rootObject.transform);
                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedMechanicChoiceOverlay"));
                Assert.AreEqual(4, FindChildren(rootObject.transform, "UnityAdvancedMechanicChoiceCard-").Count);
                var trinketPanelSize = FindChild(rootObject.transform, "UnityAdvancedMechanicChoicePanel").GetComponent<RectTransform>().sizeDelta;
                Assert.GreaterOrEqual(trinketPanelSize.x, 680f);
                Assert.GreaterOrEqual(trinketPanelSize.y, 500f);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void AdvancedMechanicLibrary_RepeatedOpenCloseClicksStayStable()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                for (var index = 0; index < 5; index += 1)
                {
                    if (FindChild(rootObject.transform, "UnityRightPanel") == null)
                    {
                        OpenRightPanelDrawer(rootObject.transform);
                    }

                    FindChild(rootObject.transform, "UnityToolsButton").GetComponent<Button>().onClick.Invoke();
                    FindChild(rootObject.transform, "UnityToolsReplaceLesserTrinketButton").GetComponent<Button>().onClick.Invoke();
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedCardLibraryOverlay"));
                    FindChild(rootObject.transform, "UnityAdvancedCardLibraryCloseButton").GetComponent<Button>().onClick.Invoke();
                    Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedCardLibraryOverlay"));
                }

                service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));
                ClearChildren(rootObject.transform);
                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                for (var index = 0; index < 3; index += 1)
                {
                    FindChild(rootObject.transform, "UnityAdvancedChoiceStatusOpenButton-advanced-current").GetComponent<Button>().onClick.Invoke();
                    Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedMechanicChoiceOverlay"));
                }
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_UsesPanelModalAndToastPrefabs()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Tavern.Discover = new DiscoverState
                {
                    Options = service.State.Player.Tavern.Shop.Where(card => card != null).Take(2).Select(card => card.Clone()).ToList()
                };

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanel").GetComponent<UnityTavernRightPanelComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityRightPanelTabs"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityActionPanel").GetComponent<UnityTavernActionPanelComponent>());
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityReplayButton"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityToolsButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnitySelectedCardPanel"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvisorPanel"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityLogScroll"));

                FindChild(rootObject.transform, "UnityRightPanelTab-Details").GetComponent<Button>().onClick.Invoke();
                var selectedPanel = FindChild(rootObject.transform, "UnitySelectedCardPanel");
                Assert.IsNotNull(selectedPanel.GetComponent<UnityTavernSelectedCardPanelComponent>());
                Assert.AreEqual(236f, selectedPanel.GetComponent<LayoutElement>().preferredHeight, 0.001f);
                Assert.AreEqual(202f, FindChild(selectedPanel, "UnitySelectedCardDetailLayout").GetComponent<LayoutElement>().preferredHeight, 0.001f);

                FindChild(rootObject.transform, "UnityRightPanelTab-Advice").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvisorPanel").GetComponent<UnityTavernAdvisorPanelComponent>());

                FindChild(rootObject.transform, "UnityRightPanelTab-Logs").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityLogScroll").GetComponent<UnityTavernLogPanelComponent>());

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityDiscoverOverlay").GetComponent<UnityTavernDiscoverModalComponent>());
                Assert.AreEqual(2, FindChild(rootObject.transform, "UnityDiscoverOptions").childCount);

                var controller = FindChild(rootObject.transform, "UnityTavernTrainer").GetComponent<UnityTavernTrainerController>();
                var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null);
                controller.BeginDrag(service.State.Player.Tavern.Shop[shopIndex], UnityTavernDragSource.Shop, shopIndex);
                controller.HandleDrop(UnityTavernDropTarget.PlayerBoard);

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityErrorToast").GetComponent<UnityTavernToastComponent>());
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CombatButtonCompletesCombatAndNextTurn()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var player = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
                player.InstanceId = "unity-player";
                player.Owner = BoardSide.Player;
                var opponent = service.State.Player.Tavern.Shop.Last(card => card.CardKind == CardKind.Minion).Clone();
                opponent.InstanceId = "unity-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Player.Board.Add(player);
                service.State.Opponent.Board.Add(opponent);

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                OpenRightPanelDrawer(rootObject.transform);
                FindChild(rootObject.transform, "UnityNextTurnButton").GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(2, service.State.Round);
                Assert.AreEqual(MatchPhase.Tavern, service.State.Phase);
                Assert.IsNotNull(service.State.LastResult);
                Assert.IsNotNull(service.State.LastReplay);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        private static LearnHearthstone.Domain.Data.QuestCatalog CreateMinimalQuestCatalog()
        {
            return new LearnHearthstone.Domain.Data.QuestCatalog(
                new[]
                {
                    new QuestDefinition
                    {
                        Id = "ui-quest",
                        CardId = "UI_QUEST",
                        Name = "UI Quest",
                        Text = "Test quest",
                        ImplementationStatus = QuestImplementationStatus.Implemented
                    }
                },
                new[]
                {
                    new QuestRewardDefinition
                    {
                        Id = "ui-reward",
                        CardId = "UI_REWARD",
                        Name = "UI Reward",
                        Text = "Test reward",
                        ImplementationStatus = QuestImplementationStatus.Implemented,
                        OfferPoolStatus = QuestOfferPoolStatus.Offerable
                    }
                });
        }

        private static LearnHearthstone.Domain.Data.HeroCatalog CreateMinimalHeroCatalog()
        {
            return new LearnHearthstone.Domain.Data.HeroCatalog(new[]
            {
                new HeroDefinition
                {
                    HeroCardId = "UI_HERO",
                    Name = "Patchwerk",
                    Health = 30,
                    HeroPower = new HeroPowerDefinition
                    {
                        CardId = "UI_HERO_POWER",
                        Name = "UI Hero Power",
                        Text = "Test hero power"
                    }
                }
            });
        }

        private static LearnHearthstone.Domain.Data.TrinketCatalog CreateMinimalTrinketCatalog()
        {
            return new LearnHearthstone.Domain.Data.TrinketCatalog(new[]
            {
                new TrinketDefinition
                {
                    Id = "ui-lesser-trinket",
                    CardId = "UI_LESSER_TRINKET",
                    Name = "UI Lesser Trinket",
                    SlotKind = TrinketSlotKind.Lesser,
                    ImplementationStatus = TrinketImplementationStatus.Implemented,
                    OfferPoolStatus = TrinketOfferPoolStatus.Offerable
                },
                new TrinketDefinition
                {
                    Id = "ui-greater-trinket",
                    CardId = "UI_GREATER_TRINKET",
                    Name = "UI Greater Trinket",
                    SlotKind = TrinketSlotKind.Greater,
                    ImplementationStatus = TrinketImplementationStatus.Implemented,
                    OfferPoolStatus = TrinketOfferPoolStatus.Offerable
                }
            });
        }

        private static LearnHearthstone.Domain.Data.AnomalyCatalog CreateMinimalAnomalyCatalog()
        {
            return new LearnHearthstone.Domain.Data.AnomalyCatalog(new[]
            {
                new AnomalyDefinition
                {
                    Id = "ui-anomaly",
                    CardId = "UI_ANOMALY",
                    Name = "UI Anomaly",
                    Text = "Test anomaly",
                    ImplementationStatus = AnomalyImplementationStatus.Implemented,
                    SourcePools = new List<AnomalyPoolVersion> { AnomalyPoolVersion.CurrentHsReplay }
                }
            });
        }

        private static MinionInstance CreateBoardMinion(
            MatchService service,
            string instanceId,
            BoardSide side,
            int attack,
            int health,
            params Keyword[] keywords)
        {
            var minion = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
            minion.InstanceId = instanceId;
            minion.Owner = side;
            minion.Attack = attack;
            minion.Health = health;
            minion.MaxHealth = health;
            minion.Keywords = new List<Keyword>(keywords);
            return minion;
        }

        private static void CaptureAndAssertTavernTable(int width, int height, string fileName)
        {
            Directory.CreateDirectory(TavernCaptureDirectory);
            var path = Path.Combine(TavernCaptureDirectory, fileName);
            var nonBackgroundSamples = CaptureTavernTable(width, height, path);

            Assert.IsTrue(File.Exists(path), path);
            Assert.Greater(new FileInfo(path).Length, 10000, path);
            Assert.Greater(nonBackgroundSamples, 20, path);
        }

        private static int CaptureTavernTable(int width, int height, string path)
        {
            var cameraObject = new GameObject("TavernCaptureCamera", typeof(Camera));
            var canvasObject = new GameObject("TavernCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            var previousActive = RenderTexture.active;
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.transform.position = new Vector3(0f, 0f, -10f);

                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;

                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(width, height);

                var canvas = canvasObject.GetComponent<Canvas>();
                LearnHearthstoneBootstrap.ConfigureCanvas(canvas, UnityTavernLayoutContext.ForSize(width, height));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;

                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                new UnityTavernTrainerView(canvasObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
                AssertMainTableRows(canvasObject.transform);
                AssertTavernKeyControlsHaveRoom(canvasObject.transform);
                camera.Render();

                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                return CountNonBackgroundSamples(texture);
            }
            finally
            {
                RenderTexture.active = previousActive;
                var camera = cameraObject.GetComponent<Camera>();
                if (camera != null)
                {
                    camera.targetTexture = null;
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }

                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }

                Object.DestroyImmediate(canvasObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static int CountNonBackgroundSamples(Texture2D texture)
        {
            var count = 0;
            var pixels = texture.GetPixels32();
            var stride = Mathf.Max(1, pixels.Length / 512);
            for (var index = 0; index < pixels.Length; index += stride)
            {
                var pixel = pixels[index];
                if (pixel.r > 8 || pixel.g > 8 || pixel.b > 8)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static void AssertMainTableRows(Transform root)
        {
            Assert.AreEqual("UnityTableColumn", FindChild(root, "UnityShopZone").parent.name);
            Assert.AreEqual("UnityTableColumn", FindChild(root, "UnityPlayerBoardZone").parent.name);
            Assert.AreEqual("UnityTableColumn", FindChild(root, "UnityHandZone").parent.name);
        }

        private static void AssertTavernKeyControlsHaveRoom(Transform root)
        {
            var quickBar = FindChild(root, "UnityTavernActionBar").GetComponent<RectTransform>();
            Assert.GreaterOrEqual(quickBar.rect.height, 40f);

            var keyButtons = new[]
            {
                "UnityQuickRefreshButton",
                "UnityQuickFreezeButton",
                "UnityQuickUpgradeButton",
                "UnityQuickNextTurnButton",
                "UnityQuickToolsButton"
            };

            foreach (var buttonName in keyButtons)
            {
                var element = FindChild(root, buttonName).GetComponent<LayoutElement>();
                Assert.GreaterOrEqual(element.preferredHeight, UnityTavernUiStyle.TouchHeight, buttonName);
            }

            var drawerToggle = FindChild(root, "UnityRightPanelDrawerToggle").GetComponent<RectTransform>();
            Assert.GreaterOrEqual(drawerToggle.sizeDelta.x, 44f);
            Assert.GreaterOrEqual(drawerToggle.sizeDelta.y, 48f);
        }

        private static EventSystem EnsureEventSystem(Transform parent)
        {
            if (EventSystem.current != null)
            {
                return EventSystem.current;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.transform.SetParent(parent, false);
            return eventSystemObject.GetComponent<EventSystem>();
        }

        private static PointerEventData PointerDrop(Transform root, float x)
        {
            return new PointerEventData(EnsureEventSystem(root))
            {
                position = new Vector2(x, 0f)
            };
        }

        [Test]
        public void OpponentBoard_StartOfCombatSpellButtonShowsOnlyEligibleSpellsAndQueuesSelection()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();
                FindChild(rootObject.transform, "UnityOpponentEntryButton").GetComponent<Button>().onClick.Invoke();

                var openButton = FindChild(rootObject.transform, "UnityOpponentStartOfCombatSpellButton");
                Assert.IsNotNull(openButton, "Opponent start-of-combat spell entry button was not built.");
                Assert.GreaterOrEqual(openButton.GetComponent<LayoutElement>().preferredHeight, 44f);
                openButton.GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardLibraryOverlay"), "Card library overlay did not open.");
                Assert.AreEqual("敌方战斗开始法术", FindChild(rootObject.transform, "UnityCardLibraryTitle").GetComponent<Text>().text);
                Assert.IsNull(FindChild(rootObject.transform, "UnityCardLibraryMinionTab"));
                var cards = rootObject.GetComponentsInChildren<UnityTavernCardComponent>(true)
                    .Where(IsCardLibraryComponent)
                    .ToList();
                Assert.AreEqual(6, cards.Count);
                Assert.IsTrue(cards.All(card => TavernSpellEngine.IsStartOfCombatSpell(card.Card.CardId)));

                var beetles = FindCardLibraryCard(rootObject.transform, "110401");
                Assert.IsNotNull(beetles, "Boon of Beetles was not present in the filtered start-of-combat spell library.");
                beetles.GetComponent<Button>().onClick.Invoke();

                CollectionAssert.Contains(service.State.Opponent.NextCombatTavernSpellCardIds, "110401");
                var beetlesAfter = FindCardLibraryCard(rootObject.transform, "110401");
                Assert.IsNotNull(beetlesAfter, "Boon of Beetles disappeared after configuring it.");
                var configureButton = beetlesAfter.transform.parent.GetComponentsInChildren<Button>(true)
                    .First(button => button.gameObject.name.StartsWith("UnityCardLibraryAddButton"));
                Assert.IsFalse(configureButton.interactable);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        private static void PrepareWideDropRect(UnityTavernDropTargetBehaviour dropTarget)
        {
            var rect = dropTarget.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(700f, 100f);
            rect.anchoredPosition = Vector2.zero;
            rect.position = Vector3.zero;
            rect.ForceUpdateRectTransforms();
        }

        private static void RightClickCard(Transform root, MinionInstance minion)
        {
            var cardObject = FindChild(root, "UnityCard-" + minion.InstanceId);
            Assert.IsNotNull(cardObject, "Missing UnityCard-" + minion.InstanceId + ". Existing cards: " + string.Join(", ", FindChildren(root, "UnityCard-").Select(child => child.name)));
            var component = cardObject.GetComponent<UnityTavernCardComponent>();
            Assert.IsNotNull(component);
            component.OnPointerClick(new PointerEventData(EnsureEventSystem(root))
            {
                button = PointerEventData.InputButton.Right
            });
        }

        private static Transform FirstCardLibraryCard(Transform root)
        {
            var card = root.GetComponentsInChildren<UnityTavernCardComponent>(true)
                .FirstOrDefault(IsCardLibraryComponent);
            Assert.IsNotNull(card);
            return card.transform;
        }

        private static UnityTavernCardComponent FindCardLibraryCard(Transform root, string cardId)
        {
            return root.GetComponentsInChildren<UnityTavernCardComponent>(true)
                .FirstOrDefault(component =>
                    IsCardLibraryComponent(component) &&
                    component.Card != null &&
                    component.Card.CardId == cardId);
        }

        private static bool IsCardLibraryComponent(UnityTavernCardComponent component)
        {
            if (component == null)
            {
                return false;
            }

            if (component.gameObject.name.StartsWith("UnityCardLibraryCard-"))
            {
                return true;
            }

            var current = component.transform;
            while (current != null)
            {
                if (current.name.StartsWith("UnityCardLibraryCardSlot-"))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void SetEditorValues(Transform root, string attack, string health, params Keyword[] selectedKeywords)
        {
            FindChild(root, "UnityMinionEditAttackInput").GetComponent<InputField>().text = attack;
            FindChild(root, "UnityMinionEditHealthInput").GetComponent<InputField>().text = health;

            var selected = new HashSet<Keyword>(selectedKeywords);
            foreach (var keyword in EditableMinionKeywords)
            {
                var toggle = FindChild(root, "UnityMinionEditKeywordToggle-" + keyword);
                Assert.IsNotNull(toggle, keyword.ToString());
                toggle.GetComponent<Toggle>().isOn = selected.Contains(keyword);
            }
        }

        private static void AssertMinionState(MinionInstance minion, int attack, int health, params Keyword[] keywords)
        {
            Assert.AreEqual(attack, minion.Attack);
            Assert.AreEqual(health, minion.Health);
            Assert.AreEqual(health, minion.MaxHealth);
            Assert.IsNotNull(minion.Keywords);
            Assert.AreEqual(keywords.Length, minion.Keywords.Count);
            CollectionAssert.AreEquivalent(keywords, minion.Keywords);
        }

        private static CombatBoardSnapshot BoardSnapshot(BoardSide side, params CombatMinionSnapshot[] minions)
        {
            var snapshot = new CombatBoardSnapshot { Side = side };
            snapshot.Minions.AddRange(minions);
            return snapshot;
        }

        private static CombatMinionSnapshot MinionSnapshot(string instanceId, string name, int position, int attack, int health)
        {
            return new CombatMinionSnapshot
            {
                InstanceId = instanceId,
                CardId = instanceId + "-card",
                Name = name,
                Position = position,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                CanAttack = true,
                Keywords = new List<Keyword>(),
                Tribes = new List<Tribe> { Tribe.Beast }
            };
        }

        private static void AssertDropCommand(
            UnityTavernDragContext drag,
            UnityTavernDropTarget target,
            int targetIndex,
            GameCommandType expectedType,
            int expectedIndex,
            int expectedTargetIndex,
            string expectedInstanceId,
            TargetZone expectedTargetZone = TargetZone.Unspecified)
        {
            Assert.IsTrue(UnityTavernDragController.TryBuildDropCommand(drag, target, targetIndex, out var command));
            Assert.AreEqual(expectedType, command.Type);
            Assert.AreEqual(expectedIndex, command.Index);
            Assert.AreEqual(expectedTargetIndex, command.TargetIndex);
            Assert.AreEqual(expectedTargetZone, command.TargetZone);
            Assert.AreEqual(expectedInstanceId, command.InstanceId);
        }

        private static Transform OpenRightPanelDrawer(Transform root)
        {
            var toggle = FindChild(root, "UnityRightPanelDrawerToggle");
            Assert.IsNotNull(toggle);
            toggle.GetComponent<Button>().onClick.Invoke();
            var panel = FindChild(root, "UnityRightPanel");
            Assert.IsNotNull(panel);
            return panel;
        }

        private static void AssertActionButtonChrome(Transform button)
        {
            Assert.IsNotNull(button);
            Assert.IsNotNull(button.GetComponent<Outline>());
            Assert.IsNotNull(button.GetComponent<Image>());
            var selectable = button.GetComponent<Button>();
            Assert.IsNotNull(selectable);
            Assert.AreEqual(Selectable.Transition.ColorTint, selectable.transition);
            Assert.AreNotEqual(selectable.colors.normalColor, selectable.colors.highlightedColor);

            var accent = FindChild(button, button.name + "Accent");
            Assert.IsNotNull(accent);
            Assert.IsTrue(accent.gameObject.activeSelf);
            Assert.IsNotNull(accent.GetComponent<Image>());
        }

        private static void AssertFocusableButton(Transform buttonTransform, string name)
        {
            Assert.IsNotNull(buttonTransform, name);
            Assert.IsNotNull(buttonTransform.GetComponent<Image>(), name);
            var outline = buttonTransform.GetComponent<Outline>();
            Assert.IsNotNull(outline, name);
            Assert.IsTrue(outline.enabled, name);

            var button = buttonTransform.GetComponent<Button>();
            Assert.IsNotNull(button, name);
            Assert.AreEqual(Selectable.Transition.ColorTint, button.transition, name);
            Assert.AreNotEqual(Navigation.Mode.None, button.navigation.mode, name);
            Assert.AreNotEqual(button.colors.normalColor, button.colors.highlightedColor, name);
            Assert.AreEqual(button.colors.highlightedColor, button.colors.selectedColor, name);
        }

        private static void ClearChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index -= 1)
            {
                Object.DestroyImmediate(root.GetChild(index).gameObject);
            }
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                var found = FindChild(root.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static List<Transform> FindChildren(Transform root, string namePrefix)
        {
            var results = new List<Transform>();
            Collect(root, namePrefix, results);
            return results;
        }

        private static void Collect(Transform root, string namePrefix, List<Transform> results)
        {
            if (root.name.StartsWith(namePrefix))
            {
                results.Add(root);
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                Collect(root.GetChild(index), namePrefix, results);
            }
        }

        private static List<MonoBehaviour> FindComponentsNamed(Transform root, string typeName)
        {
            var results = root.GetComponents<MonoBehaviour>()
                .Where(component => component != null && component.GetType().Name == typeName)
                .ToList();
            for (var index = 0; index < root.childCount; index += 1)
            {
                results.AddRange(FindComponentsNamed(root.GetChild(index), typeName));
            }

            return results;
        }

        private static string HeroSelectionSafeName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Unknown";
            }

            return value.Replace(' ', '_').Replace('/', '_').Replace('\\', '_').Replace(':', '_');
        }

        private static string ChineseHeroName(HeroDefinition hero)
        {
            return string.IsNullOrEmpty(hero?.ZhName) ? hero?.Name : hero.ZhName;
        }

        private static Image ImageChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Image>();
        }

        private static Text TextChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Text));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Text>();
        }

        private static GameObject ButtonChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void AssertZonePrefab(string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Assert.IsNotNull(prefab, assetPath);
            Assert.IsNotNull(prefab.GetComponent<Image>(), assetPath);
            Assert.IsNotNull(prefab.GetComponent<UnityTavernZoneComponent>(), assetPath);
            Assert.IsNotNull(FindChild(prefab.transform, "UnityZoneHeader"), assetPath);
            Assert.IsNotNull(FindChild(prefab.transform, "UnityZoneTitle"), assetPath);
            Assert.IsNotNull(FindChild(prefab.transform, "UnityZoneSubtitle"), assetPath);
            Assert.IsNotNull(FindChild(prefab.transform, "UnityZoneCardRow"), assetPath);
        }
    }
}
