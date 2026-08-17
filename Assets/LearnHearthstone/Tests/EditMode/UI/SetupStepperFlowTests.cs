using System;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Persistence;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class SetupStepperFlowTests
    {
        [Test]
        public void Build_DesktopShowsFourStepProgressCurrentVersionAndSingleStepContent()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                BuildView(rootObject.transform, UnityTavernLayoutContext.ForSize(1280f, 720f));

                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySetupStepper"));
                Assert.AreEqual(
                    4,
                    rootObject.GetComponentsInChildren<Button>(true)
                        .Count(button => button.name.StartsWith("UnitySetupStepButton-", StringComparison.Ordinal)));
                StringAssert.Contains("综合沙盒", TextOf(rootObject.transform, "UnitySetupCurrentGameVersionSummary"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySetupGameVersionStep"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityTribeSelectionGrid"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySetupContinueButton"));
                Assert.IsNull(FindChild(rootObject.transform, "UnitySetupStartButton"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CompactKeepsOneScrollableStepAndFixedPhysicalCta()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var compact = UnityTavernLayoutContext.ForSize(844f, 390f);
                BuildView(rootObject.transform, compact);

                var scroll = FindChild(rootObject.transform, "UnitySetupStepScroll");
                var navigation = FindChild(rootObject.transform, "UnitySetupNavigation");
                var continueButton = FindChild(rootObject.transform, "UnitySetupContinueButton").GetComponent<Button>();
                Assert.IsNotNull(scroll.GetComponent<ScrollRect>());
                Assert.IsFalse(navigation.IsChildOf(scroll), "The bottom CTA must be a fixed sibling of the independently scrolling step.");
                Assert.GreaterOrEqual(
                    continueButton.GetComponent<LayoutElement>().minHeight * compact.CanvasScaleFactor,
                    48f - 0.01f);
                Assert.GreaterOrEqual(
                    continueButton.GetComponentInChildren<Text>().resizeTextMinSize * compact.CanvasScaleFactor,
                    14f - 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void SetupChrome_UsesCompactFixedRowsAndRightAlignedActions(int width, int height)
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var layout = UnityTavernLayoutContext.ForSize(width, height);
                var rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(width / layout.CanvasScaleFactor, height / layout.CanvasScaleFactor);
                BuildView(rootObject.transform, layout);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);

                var stepper = FindChild(rootObject.transform, "UnitySetupStepper");
                var stepperLayout = stepper.GetComponent<HorizontalLayoutGroup>();
                Assert.IsFalse(stepperLayout.childForceExpandWidth);
                Assert.IsFalse(stepperLayout.childForceExpandHeight);
                Assert.AreEqual(0f, LayoutUtility.GetFlexibleHeight((RectTransform)stepper), 0.001f);
                Assert.LessOrEqual(stepper.GetComponent<LayoutElement>().preferredHeight * layout.CanvasScaleFactor, 80f);
                Assert.LessOrEqual(((RectTransform)stepper).rect.height * layout.CanvasScaleFactor, 80f);

                var stepButtons = rootObject.GetComponentsInChildren<Button>(true)
                    .Where(button => button.name.StartsWith("UnitySetupStepButton-", StringComparison.Ordinal))
                    .ToArray();
                Assert.AreEqual(4, stepButtons.Length);
                Assert.IsTrue(stepButtons.All(button =>
                {
                    var element = button.GetComponent<LayoutElement>();
                    return element.preferredWidth * layout.CanvasScaleFactor <= 168f + 0.01f &&
                           element.minHeight * layout.CanvasScaleFactor >= 48f - 0.01f;
                }));

                var navigation = FindChild(rootObject.transform, "UnitySetupNavigation");
                var navigationLayout = navigation.GetComponent<HorizontalLayoutGroup>();
                Assert.AreEqual(TextAnchor.MiddleRight, navigationLayout.childAlignment);
                Assert.IsFalse(navigationLayout.childForceExpandWidth);
                Assert.IsFalse(navigationLayout.childForceExpandHeight);
                Assert.AreEqual(0f, LayoutUtility.GetFlexibleHeight((RectTransform)navigation), 0.001f);
                Assert.LessOrEqual(((RectTransform)navigation).rect.height * layout.CanvasScaleFactor, 80f);

                foreach (var buttonName in new[] { "UnitySetupBackButton", "UnitySetupContinueButton" })
                {
                    var element = FindChild(rootObject.transform, buttonName).GetComponent<LayoutElement>();
                    Assert.LessOrEqual(element.preferredWidth * layout.CanvasScaleFactor, 148f + 0.01f, buttonName);
                    Assert.GreaterOrEqual(element.minHeight * layout.CanvasScaleFactor, 48f - 0.01f, buttonName);
                }
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void HeroAndTribeSelection_PreservesStepScrollWhenTribeChanges()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                BuildView(rootObject.transform, UnityTavernLayoutContext.ForSize(844f, 390f));
                Click(rootObject.transform, "UnitySetupContinueButton");
                var before = FindChild(rootObject.transform, "UnitySetupStepScroll").GetComponent<ScrollRect>();
                before.verticalNormalizedPosition = 0.31f;

                Click(rootObject.transform, "UnityTribeSelectionBeastButton");

                var after = FindChild(rootObject.transform, "UnitySetupStepScroll").GetComponent<ScrollRect>();
                Assert.AreEqual(0.31f, after.verticalNormalizedPosition, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [TestCase(844, 390)]
        [TestCase(994, 384)]
        [TestCase(1000, 600)]
        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void Build_ReferenceResolutionsKeepSingleStepAndControllerFocus(int width, int height)
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                BuildView(rootObject.transform, UnityTavernLayoutContext.ForSize(width, height));

                Assert.AreEqual(1, CountNamed(rootObject.transform, "UnitySetupGameVersionStep"));
                Assert.AreEqual(0, CountNamed(rootObject.transform, "UnityTribeSelectionGrid"));
                var stepButtons = rootObject.GetComponentsInChildren<Button>(true)
                    .Where(button => button.name.StartsWith("UnitySetupStepButton-", StringComparison.Ordinal))
                    .ToArray();
                Assert.AreEqual(4, stepButtons.Length);
                Assert.IsTrue(stepButtons.All(button => button.GetComponent<UnitySelectableFocusRing>() != null));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [TestCase(740, 360)]
        [TestCase(844, 390)]
        [TestCase(932, 430)]
        public void ShortLandscape_EditorsKeepEveryControlReachableAcrossPhoneWidths(int width, int height)
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var layout = UnityTavernLayoutContext.ForSize(width, height);
                Assert.IsTrue(layout.IsShortLandscape, width + "x" + height + " must use the shared short-landscape shell.");
                var rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(width / layout.CanvasScaleFactor, height / layout.CanvasScaleFactor);
                BuildView(rootObject.transform, layout);

                Click(rootObject.transform, "UnitySetupContinueButton");
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
                AssertPhysicalSize(rootObject.transform, "UnityTribeSelectionHeroImage", layout, 76f, 76f);

                Click(rootObject.transform, "UnityTribeSelectionChooseHeroButton");
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
                AssertPhysicalHeight(rootObject.transform, "UnityHeroSelectionHeader", layout, 48f);
                AssertHorizontalToolbar(rootObject.transform, "UnityHeroSelectionFilterScroll");
                AssertPhysicalSize(rootObject.transform, "UnityHeroSelectionPreviewImage", layout, 64f, 64f);
                var listPortrait = rootObject.GetComponentsInChildren<LayoutElement>(true)
                    .First(item => item.name.StartsWith("UnityHeroSelectionHeroImage-", StringComparison.Ordinal));
                AssertPhysicalSize(listPortrait.transform, layout, 56f, 56f);
                Assert.IsNotNull(listPortrait.GetComponent<RectMask2D>());
                var portraitFitter = listPortrait.GetComponentInChildren<AspectRatioFitter>(true);
                Assert.IsNotNull(portraitFitter);
                var portraitArt = portraitFitter.GetComponent<Image>();
                Assert.IsNotNull(portraitArt.sprite);
                Assert.GreaterOrEqual(portraitArt.rectTransform.rect.width * layout.CanvasScaleFactor, 56f - 0.01f);
                Assert.GreaterOrEqual(portraitArt.rectTransform.rect.height * layout.CanvasScaleFactor, 56f - 0.01f);
                Click(rootObject.transform, "UnityHeroSelectionCloseButton");

                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    Click(rootObject.transform, "UnityTribeSelection" + tribe + "Button");
                }

                Click(rootObject.transform, "UnitySetupContinueButton");
                Click(rootObject.transform, "UnityAdvancedQuestRewardPoolCardEditButton");
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);

                AssertPhysicalHeight(rootObject.transform, "UnityAdvancedPoolEditorHeader", layout, 58f);
                AssertPhysicalHeight(rootObject.transform, "UnityAdvancedPoolSearchInput", layout, 48f);
                AssertHorizontalToolbar(rootObject.transform, "UnityAdvancedPoolFilters");
                AssertHorizontalToolbar(rootObject.transform, "UnityAdvancedPoolBulkActions");
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedPoolScroll").GetComponent<ScrollRect>());
                foreach (var control in new[]
                {
                    "UnityAdvancedPoolTab-QuestRewards",
                    "UnityAdvancedPoolTab-Trinkets",
                    "UnityAdvancedPoolTab-Anomalies",
                    "UnityAdvancedPoolIncludeFilteredButton",
                    "UnityAdvancedPoolExcludeFilteredButton",
                    "UnityAdvancedPoolImplementedOnlyButton",
                    "UnityAdvancedPoolOfferableOnlyButton",
                    "UnityAdvancedPoolInvertButton",
                    "UnityAdvancedPoolResetFiltersButton"
                })
                {
                    Assert.IsNotNull(FindChild(rootObject.transform, control), "Missing short-landscape control: " + control);
                }

                Click(rootObject.transform, "UnityAdvancedPoolEditorCloseButton");
                Click(rootObject.transform, "UnitySetupContinueButton");
                Click(rootObject.transform, "UnityCardPoolVersionOpenButton");
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);

                AssertPhysicalHeight(rootObject.transform, "UnityCardPoolVersionModalHeader", layout, 58f);
                AssertPhysicalHeight(rootObject.transform, "UnityCardPoolVersionSearchRow", layout, 48f);
                AssertHorizontalToolbar(rootObject.transform, "UnityCardPoolVersionPicker");
                AssertHorizontalToolbar(rootObject.transform, "UnityCardPoolVersionFilters");
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolVersionScroll").GetComponent<ScrollRect>());
                var cardThumbnail = FindChild(rootObject.transform, "UnityCardPoolVersionScroll")
                    .GetComponentsInChildren<LayoutElement>(true)
                    .First(item => item.name.EndsWith("ImageFrame", StringComparison.Ordinal));
                AssertPhysicalSize(cardThumbnail.transform, layout, 50f, 68f);
                foreach (var control in new[]
                {
                    "UnityCardPoolVersionMinionTab",
                    "UnityCardPoolVersionSpellTab",
                    "UnityCardPoolVersionDefaultButton",
                    "UnityCardPoolVersionNewButton",
                    "UnityCardPoolVersionCopyButton",
                    "UnityCardPoolVersionSaveButton",
                    "UnityCardPoolVersionDeleteButton",
                    "UnityCardPoolVersionExcludeFilteredButton",
                    "UnityCardPoolVersionIncludeFilteredButton",
                    "UnityCardPoolVersionCloseButton"
                })
                {
                    Assert.IsNotNull(FindChild(rootObject.transform, control), "Missing short-landscape control: " + control);
                }
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void FourSteps_PreserveSelectionsAndStartWithResolvedVersionLock()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                MatchSetupOptions startedWith = null;
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("ui3-stepper-test");
                BuildView(
                    rootObject.transform,
                    UnityTavernLayoutContext.ForSize(1280f, 720f),
                    snapshot,
                    setup => startedWith = setup);

                Click(rootObject.transform, "UnitySetupContinueButton");
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityTribeSelectionGrid"));
                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    Click(rootObject.transform, "UnityTribeSelection" + tribe + "Button");
                }

                Assert.IsTrue(FindChild(rootObject.transform, "UnitySetupContinueButton").GetComponent<Button>().interactable);
                Click(rootObject.transform, "UnitySetupContinueButton");
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsSetupPanel"));
                Click(rootObject.transform, "UnitySetupContinueButton");
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityCardPoolVersionPanel"));
                StringAssert.Contains("基础游戏版本", TextOf(rootObject.transform, "UnityCardPoolVersionSummary"));
                Click(rootObject.transform, "UnitySetupStartButton");

                Assert.IsNotNull(startedWith);
                Assert.AreEqual(GameVersionIds.LegacyCompositeSandbox, startedWith.GameVersionId);
                Assert.AreEqual(RulesetIds.LegacyCompositeSandbox, startedWith.RulesetId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(startedWith.ContentSnapshotId));
                Assert.IsFalse(string.IsNullOrWhiteSpace(startedWith.ContentFingerprint));
                CollectionAssert.AreEqual(TribeAvailabilityRules.PlayableTribes.Take(5), startedWith.ActiveTribes);
                Assert.IsFalse(startedWith.EnableTimewarpedTavern);
                Assert.IsFalse(startedWith.UseExplicitTimewarpedPool);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Season14_ShowsOnlyDarkGiftsAndTrinketsAndClampsLegacyMechanics()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                MatchSetupOptions startedWith = null;
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("ui3-season14-mechanics-test");
                BuildView(
                    rootObject.transform,
                    UnityTavernLayoutContext.ForSize(1280f, 720f),
                    snapshot,
                    setup => startedWith = setup);

                Click(rootObject.transform, "UnitySetupGameVersionButton-" + GameVersionIds.Season14Preview);
                Click(rootObject.transform, "UnitySetupContinueButton");

                Assert.IsFalse(string.IsNullOrWhiteSpace(TextOf(rootObject.transform, "UnityTribeSelectionHeroPowerText")));
                StringAssert.Contains("张可用", TextOf(rootObject.transform, "UnityTribeSelectionBeastButtonText"));
                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    Click(rootObject.transform, "UnityTribeSelection" + tribe + "Button");
                }

                Click(rootObject.transform, "UnitySetupContinueButton");
                Assert.IsNotNull(FindChild(rootObject.transform, "UnitySeason14DarkGiftCard"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedTrinketPoolCard"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedQuestRewardPoolCard"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedAnomalyPoolCard"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsToggle-ShowDebugOnly"));
                Assert.IsNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsToggle-EnablePlayerDirectedChoices"));

                Click(rootObject.transform, "UnitySetupContinueButton");
                StringAssert.Contains("黑暗之赐与饰品", TextOf(rootObject.transform, "UnitySetupCardPoolHint"));
                Click(rootObject.transform, "UnitySetupStartButton");

                Assert.IsNotNull(startedWith);
                Assert.AreEqual(GameVersionIds.Season14Preview, startedWith.GameVersionId);
                Assert.IsTrue(startedWith.EnableTrinkets);
                Assert.IsFalse(startedWith.EnableQuests);
                Assert.IsFalse(startedWith.EnableQuestRewards);
                Assert.IsFalse(startedWith.EnableAnomalies);
                Assert.IsFalse(startedWith.EnableTimewarpedTavern);
                Assert.IsFalse(startedWith.UseExplicitTimewarpedPool);
                CollectionAssert.IsEmpty(startedWith.EnabledQuestCardIds);
                CollectionAssert.IsEmpty(startedWith.EnabledQuestRewardCardIds);
                CollectionAssert.IsEmpty(startedWith.EnabledAnomalyCardIds);
                CollectionAssert.IsEmpty(startedWith.EnabledTimewarpedCardIds);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void ChangingVersion_ShowsConflictsBeforeExplicitCancelOrAutoRepair()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("ui3-stepper-test");
                BuildView(rootObject.transform, UnityTavernLayoutContext.ForSize(1280f, 720f), snapshot);

                Click(rootObject.transform, "UnitySetupContinueButton");
                foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                {
                    Click(rootObject.transform, "UnityTribeSelection" + tribe + "Button");
                }

                Click(rootObject.transform, "UnitySetupContinueButton");
                Click(rootObject.transform, "UnitySetupContinueButton");
                Click(rootObject.transform, "UnityCardPoolVersionOpenButton");
                Click(rootObject.transform, "UnityCardPoolVersionCopyButton");
                Click(rootObject.transform, "UnityCardPoolVersionCloseButton");
                Click(rootObject.transform, "UnitySetupStepButton-GameVersion");

                Click(rootObject.transform, "UnitySetupGameVersionButton-" + GameVersionIds.Season14Preview);
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityGameVersionConflictOverlay"));
                StringAssert.Contains("卡池方案", TextOf(rootObject.transform, "UnityGameVersionConflictSummary"));
                StringAssert.Contains("综合沙盒", TextOf(rootObject.transform, "UnitySetupCurrentGameVersionSummary"));

                Click(rootObject.transform, "UnityGameVersionConflictCancelButton");
                Assert.IsNull(FindChild(rootObject.transform, "UnityGameVersionConflictOverlay"));
                StringAssert.Contains("综合沙盒", TextOf(rootObject.transform, "UnitySetupCurrentGameVersionSummary"));

                Click(rootObject.transform, "UnitySetupGameVersionButton-" + GameVersionIds.Season14Preview);
                Click(rootObject.transform, "UnityGameVersionConflictApplyButton");
                Assert.IsNull(FindChild(rootObject.transform, "UnityGameVersionConflictOverlay"));
                StringAssert.Contains("36.2", TextOf(rootObject.transform, "UnitySetupCurrentGameVersionSummary"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void ChangingVersion_UsesUnboundResolutionSourceForPinnedSnapshots()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var source = EmbeddedGameCatalogSnapshotLoader.Load("ui3-pinned-snapshot-test");
                var pinned = new GameCatalogSnapshot(
                    new ContentSnapshotInfo(
                        source.Info.ContentVersion,
                        source.Info.RequiredClientVersion,
                        source.Info.Source,
                        source.Info.SourceCommit,
                        source.Info.LoadedAtUtc,
                        source.Info.SnapshotId,
                        GameVersionIds.LegacyCompositeSandbox,
                        RulesetIds.LegacyCompositeSandbox,
                        "pinned-fingerprint"),
                    source.Chinese,
                    source.English,
                    source.VersionedContent);
                BuildView(rootObject.transform, UnityTavernLayoutContext.ForSize(1280f, 720f), pinned);

                Click(rootObject.transform, "UnitySetupGameVersionButton-" + GameVersionIds.Season14Preview);

                StringAssert.Contains("36.2", TextOf(rootObject.transform, "UnitySetupCurrentGameVersionSummary"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        private static UnityTavernTribeSelectionView BuildView(
            Transform root,
            UnityTavernLayoutContext layout,
            GameCatalogSnapshot snapshot = null,
            Action<MatchSetupOptions> start = null)
        {
            snapshot = snapshot ?? EmbeddedGameCatalogSnapshotLoader.Load("ui3-stepper-test");
            var view = new UnityTavernTribeSelectionView(
                root,
                start ?? (_ => { }),
                () => { },
                layout,
                new MemoryCardPoolVersionRepository(),
                useEnglish: false,
                catalogs: snapshot.Chinese,
                catalogSnapshot: snapshot);
            view.Build();
            return view;
        }

        private static void Click(Transform root, string name)
        {
            var target = FindChild(root, name);
            Assert.IsNotNull(target, "Missing button: " + name);
            target.GetComponent<Button>().onClick.Invoke();
        }

        private static string TextOf(Transform root, string name)
        {
            var target = FindChild(root, name);
            Assert.IsNotNull(target, "Missing text: " + name);
            return target.GetComponent<Text>().text;
        }

        private static int CountNamed(Transform parent, string name)
        {
            var count = parent.name == name ? 1 : 0;
            for (var index = 0; index < parent.childCount; index++)
            {
                count += CountNamed(parent.GetChild(index), name);
            }

            return count;
        }

        private static void AssertPhysicalHeight(Transform root, string name, UnityTavernLayoutContext layout, float minimum)
        {
            var target = FindChild(root, name);
            Assert.IsNotNull(target, "Missing layout target: " + name);
            var element = target.GetComponent<LayoutElement>();
            Assert.IsNotNull(element, "Missing LayoutElement: " + name);
            Assert.GreaterOrEqual(element.preferredHeight * layout.CanvasScaleFactor, minimum - 0.01f, name);
        }

        private static void AssertPhysicalSize(
            Transform root,
            string name,
            UnityTavernLayoutContext layout,
            float minimumWidth,
            float minimumHeight)
        {
            var target = FindChild(root, name);
            Assert.IsNotNull(target, "Missing layout target: " + name);
            AssertPhysicalSize(target, layout, minimumWidth, minimumHeight);
        }

        private static void AssertPhysicalSize(
            Transform target,
            UnityTavernLayoutContext layout,
            float minimumWidth,
            float minimumHeight)
        {
            var element = target.GetComponent<LayoutElement>();
            Assert.IsNotNull(element, "Missing LayoutElement: " + target.name);
            Assert.GreaterOrEqual(element.preferredWidth * layout.CanvasScaleFactor, minimumWidth - 0.01f, target.name);
            Assert.GreaterOrEqual(element.preferredHeight * layout.CanvasScaleFactor, minimumHeight - 0.01f, target.name);
        }

        private static void AssertHorizontalToolbar(Transform root, string name)
        {
            var target = FindChild(root, name);
            Assert.IsNotNull(target, "Missing toolbar: " + name);
            var scroll = target.GetComponent<ScrollRect>();
            Assert.IsNotNull(scroll, "Toolbar must be swipeable: " + name);
            Assert.IsTrue(scroll.horizontal, name);
            Assert.IsFalse(scroll.vertical, name);
            Assert.IsNull(scroll.horizontalScrollbar, name + " uses touch scrolling without shrinking its viewport.");
            Assert.IsNull(scroll.verticalScrollbar, name + " uses touch scrolling without shrinking its viewport.");
            Assert.AreEqual(Vector2.zero, scroll.viewport.offsetMin, name);
            Assert.AreEqual(Vector2.zero, scroll.viewport.offsetMax, name);
        }

        private static Transform FindChild(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var found = FindChild(parent.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private sealed class MemoryCardPoolVersionRepository : ICardPoolVersionRepository
        {
            private CardPoolVersionStore store = new CardPoolVersionStore();

            public CardPoolVersionStore Load()
            {
                return store;
            }

            public void Save(CardPoolVersionStore nextStore)
            {
                store = nextStore;
            }
        }
    }
}
