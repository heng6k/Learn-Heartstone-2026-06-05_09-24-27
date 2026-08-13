using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StrategyGuideUiTests
    {
        [Test]
        public void SelectionUsesCompactMasterDetailLayoutWithSevenSlotLineups()
        {
            var root = new GameObject("StrategyGuideMasterDetailRoot", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f)).Build();

                Assert.AreEqual(1, Find(root.transform, "StrategyGuideWorkspace").Count);
                var rail = Find(root.transform, "StrategyGuideRail").Single();
                var railElement = rail.GetComponent<LayoutElement>();
                var layoutContext = UnityTavernLayoutContext.ForSize(1280f, 720f);
                Assert.AreEqual(railElement.minWidth, railElement.preferredWidth);
                Assert.AreEqual(0f, railElement.flexibleWidth);
                Assert.AreEqual(2, railElement.layoutPriority);
                Assert.LessOrEqual(railElement.preferredWidth * layoutContext.CanvasScaleFactor, 330f);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideDetailScroll").Count);
                var modeSwitcher = Find(root.transform, "StrategyGuideModeSwitcher").Single();
                var browseMode = Find(root.transform, "StrategyGuideBrowseModeButton").Single();
                var createMode = Find(root.transform, "StrategyGuideAuthoringOpenButton").Single();
                Assert.GreaterOrEqual(
                    modeSwitcher.GetComponent<LayoutElement>().preferredHeight,
                    UnityTavernUiStyle.TouchHeight);
                Assert.AreEqual(1f, browseMode.GetComponent<LayoutElement>().flexibleWidth);
                Assert.AreEqual(1f, createMode.GetComponent<LayoutElement>().flexibleWidth);
                StringAssert.Contains("查看一图流", browseMode.GetComponentInChildren<Text>().text);
                StringAssert.Contains("创建一图流", createMode.GetComponentInChildren<Text>().text);
                Assert.AreEqual(
                    catalog.Guides.Count,
                    root.GetComponentsInChildren<Button>(true).Count(button =>
                        button.name.StartsWith("StrategyGuideCard-", StringComparison.Ordinal)));
                foreach (var guide in catalog.Guides)
                {
                    var lineup = Find(root.transform, "StrategyGuideLineup-" + guide.GuideId).Single();
                    Assert.IsInstanceOf<HorizontalLayoutGroup>(lineup.GetComponent<HorizontalOrVerticalLayoutGroup>());
                    Assert.AreEqual(
                        7,
                        lineup.GetComponentsInChildren<Transform>(true).Count(item =>
                            item.name.StartsWith("StrategyGuideLineupCard-", StringComparison.Ordinal)));
                }
                Assert.AreEqual(
                    1,
                    root.GetComponentsInChildren<Transform>(true).Count(item =>
                        item.name.StartsWith("StrategyGuideDetail-", StringComparison.Ordinal) &&
                        item.gameObject.activeInHierarchy));

                const string mechGuideId = "GUIDE-S14-MECH-SPELL-SATELLITE";
                Find(root.transform, "StrategyGuideCard-" + mechGuideId).Single().GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(Find(root.transform, "StrategyGuideDetail-" + mechGuideId).Single().gameObject.activeSelf);
                Assert.IsFalse(Find(root.transform, "StrategyGuideDetail-GUIDE-S14-BEAST-LOBSTER-RALLY").Single().gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MobileOnePageSelectionKeepsCreateAndImportWithoutOpeningMainHub()
        {
            var root = new GameObject("StrategyGuideMobilePreviewRoot", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var layoutContext = UnityTavernLayoutContext.ForSize(1920f, 1080f);
                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    null,
                    layoutContext: layoutContext,
                    resolvedVersion: ResolveSeason14(),
                    startImportedGuide: _ => { },
                    mobileOnePageOnly: true).Build();

                Assert.AreEqual(1, Find(root.transform, "StrategyGuideModeSwitcher").Count);
                Assert.Zero(Find(root.transform, "StrategyGuideBackButton").Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideImportButton").Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringOpenButton").Count);
                Assert.GreaterOrEqual(
                    Find(root.transform, "StrategyGuideModeSwitcher").Single().GetComponent<LayoutElement>().preferredHeight,
                    UnityTavernUiStyle.TouchHeight);
                StringAssert.Contains(
                    "一图流试玩",
                    Find(root.transform, "StrategyGuideHeaderTitle").Single().GetComponent<Text>().text);

                var rail = Find(root.transform, "StrategyGuideRail").Single().GetComponent<LayoutElement>();
                Assert.LessOrEqual(rail.preferredWidth * layoutContext.CanvasScaleFactor, 280f);
                Assert.AreEqual(
                    catalog.Guides.Count * 3,
                    root.GetComponentsInChildren<Button>(true).Count(button =>
                        button.name.StartsWith("StrategyGuideStartButton-", StringComparison.Ordinal)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CompactSelectionClipsGuideRailAndLeavesDetailScrollable()
        {
            var root = new GameObject("StrategyGuidePhoneRoot", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(844f, 390f)).Build();

                var rail = Find(root.transform, "StrategyGuideRail").Single().GetComponent<LayoutElement>();
                var railScroll = Find(root.transform, "StrategyGuideRailListScroll").Single().GetComponent<ScrollRect>();
                var selectors = root.GetComponentsInChildren<Button>(true)
                    .Where(button => button.name.StartsWith("StrategyGuideCard-", StringComparison.Ordinal))
                    .ToList();

                Assert.AreEqual(176f, rail.preferredHeight);
                Assert.NotNull(railScroll.viewport.GetComponent<Mask>());
                Assert.AreEqual(catalog.Guides.Count, selectors.Count);
                Assert.IsTrue(selectors.All(button => button.transform.IsChildOf(railScroll.content)));
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideDetailScroll").Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AuthoringStartPickerSeparatesSourcesAndDeletesLocalDraftWithConfirmation()
        {
            var root = new GameObject("StrategyGuideAuthoringPickerRoot", typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            var directory = Path.Combine(
                UnityEngine.Application.temporaryCachePath,
                "strategy-guide-picker-" + Guid.NewGuid().ToString("N"));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var repository = new FileStrategyGuideAuthoringRepository(directory);
                const string draftId = "draft-picker-delete";
                repository.SaveDraft(new StrategyGuideAuthoringDraft
                {
                    DraftId = draftId,
                    Guide = JsonUtility.FromJson<StrategyGuideDefinition>(JsonUtility.ToJson(catalog.Guides[0]))
                });

                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f),
                    resolvedVersion: ResolveSeason14(),
                    authoringRepository: repository).Build();

                Find(root.transform, "StrategyGuideAuthoringOpenButton")
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();

                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringDraftsTab").Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringTemplatesTab").Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringVerifiedTab").Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringBlankButton").Count);
                var tabStrip = Find(root.transform, "StrategyGuideAuthoringStartTabs").Single();
                var tabStripElement = tabStrip.GetComponent<LayoutElement>();
                Assert.AreEqual(56f, tabStripElement.minHeight);
                Assert.AreEqual(56f, tabStripElement.preferredHeight);
                Assert.Zero(tabStripElement.flexibleHeight);
                foreach (var tabName in new[]
                         {
                             "StrategyGuideAuthoringDraftsTab",
                             "StrategyGuideAuthoringTemplatesTab",
                             "StrategyGuideAuthoringVerifiedTab"
                         })
                {
                    var tabElement = Find(root.transform, tabName).Single().GetComponent<LayoutElement>();
                    Assert.Zero(tabElement.minHeight);
                    Assert.AreEqual(48f, tabElement.preferredHeight);
                    Assert.Zero(tabElement.flexibleHeight);
                }
                Assert.IsTrue(Find(root.transform, "StrategyGuideAuthoringDraftsPage").Single().gameObject.activeSelf);
                Assert.IsFalse(Find(root.transform, "StrategyGuideAuthoringTemplatesPage").Single().gameObject.activeSelf);
                Assert.IsFalse(Find(root.transform, "StrategyGuideAuthoringVerifiedPage").Single().gameObject.activeSelf);

                Find(root.transform, "StrategyGuideAuthoringDraftDeleteButton-" + draftId)
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringDeleteConfirmation").Count);
                Find(root.transform, "StrategyGuideAuthoringDeleteCancelButton")
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();
                CollectionAssert.Contains(repository.ListDraftIds(), draftId);

                Find(root.transform, "StrategyGuideAuthoringDraftDeleteButton-" + draftId)
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Find(root.transform, "StrategyGuideAuthoringDeleteConfirmButton")
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Assert.IsEmpty(repository.ListDraftIds());
                Assert.IsFalse(Find(root.transform, "StrategyGuideAuthoringDraft-" + draftId).Single().gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void AuthoringCanStartFromBlankWithoutSelectingTemplate()
        {
            var root = new GameObject("StrategyGuideBlankAuthoringRoot", typeof(RectTransform));
            var compact = UnityTavernLayoutContext.ForSize(390f, 844f);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(
                compact.Width / compact.CanvasScaleFactor,
                compact.Height / compact.CanvasScaleFactor);
            var directory = Path.Combine(
                UnityEngine.Application.temporaryCachePath,
                "strategy-guide-blank-" + Guid.NewGuid().ToString("N"));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var repository = new FileStrategyGuideAuthoringRepository(directory);
                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: compact,
                    resolvedVersion: ResolveSeason14(),
                    authoringRepository: repository).Build();

                Find(root.transform, "StrategyGuideAuthoringOpenButton")
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Find(root.transform, "StrategyGuideAuthoringBlankButton")
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();

                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringEditor").Count);
                var draft = repository.ListDraftIds().Select(repository.LoadDraft).Single();
                StringAssert.StartsWith("GUIDE-CUSTOM-", draft.Guide.GuideId);
                Assert.AreEqual(7, draft.Guide.FinalComposition.Count);
                Assert.AreEqual(1, draft.Guide.EntryProfiles.Count);
                Assert.IsEmpty(draft.Guide.RequiredTribes);
                CollectionAssert.Contains(draft.Guide.ActiveTribes, Tribe.Beast.ToString());
                CollectionAssert.AreEqual(
                    new[] { StrategyGuideShapingSpells.Battlecry },
                    draft.Guide.EntryProfiles[0].ShapingSpellCardIds);

                var title = Find(root.transform, "StrategyGuideAuthoringTitleInput")
                    .Single()
                    .GetComponent<InputField>();
                ExecuteEvents.Execute<ISelectHandler>(
                    title.gameObject,
                    new BaseEventData(null),
                    ExecuteEvents.selectHandler);
                title.text = "手机自定义阵容";
                title.onEndEdit.Invoke(title.text);

                Find(root.transform, "StrategyGuideAuthoringTribeButton-Beast")
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();
                draft = repository.ListDraftIds().Select(repository.LoadDraft).Single();
                Assert.IsFalse(draft.Guide.ActiveTribes.Contains(Tribe.Beast.ToString()));
                var replacement = TribeAvailabilityRules.PlayableTribes
                    .Select(tribe => tribe.ToString())
                    .First(tribe => tribe != Tribe.Beast.ToString() && !draft.Guide.ActiveTribes.Contains(tribe));
                Find(root.transform, "StrategyGuideAuthoringTribeButton-" + replacement)
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();

                Find(root.transform, "StrategyGuideAuthoringStepButton-3")
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Find(root.transform, "StrategyGuideAuthoringFreezeButton")
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();

                draft = repository.ListDraftIds().Select(repository.LoadDraft).Single();
                Assert.AreEqual("手机自定义阵容", draft.Guide.Title);
                Assert.AreEqual(5, draft.Guide.ActiveTribes.Count);
                Assert.IsEmpty(draft.Guide.RequiredTribes);
                Assert.IsFalse(draft.Guide.ActiveTribes.Contains(Tribe.Beast.ToString()));
                var directFreeze = StrategyGuideAuthoringFreezeService.Freeze(
                    draft,
                    catalog,
                    ResolveSeason14());
                Assert.IsTrue(directFreeze.Succeeded, string.Join(" | ", directFreeze.Diagnostics));
                var frozenDirectory = Path.Combine(directory, "Frozen");
                Assert.IsTrue(Directory.Exists(frozenDirectory), "The UI freeze action did not create its artifact directory.");
                var frozen = Directory.GetFiles(frozenDirectory, "*.json");
                Assert.AreEqual(1, frozen.Length, "A real blank-authoring journey should finish with one frozen revision.");
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringFrozenDelivery").Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void SelectionBuildsThreeReadableDataDrivenEntryRowsPerGuide()
        {
            var root = new GameObject("StrategyGuideSelectionRoot", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                string selected = null;
                string selectedProfile = null;
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (guideId, profileId) =>
                    {
                        selected = guideId;
                        selectedProfile = profileId;
                    },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f)).Build();

                var starts = root.GetComponentsInChildren<Button>(true)
                    .Where(button => button.name.StartsWith("StrategyGuideStartButton-", StringComparison.Ordinal))
                    .ToList();
                Assert.AreEqual(catalog.Guides.Count * 3, starts.Count);
                Assert.IsTrue(root.GetComponentsInChildren<Text>(true).All(text => text.fontSize >= 14));
                Assert.IsTrue(starts.All(button => button.GetComponent<LayoutElement>().minHeight >= UnityTavernUiStyle.TouchHeight));
                Assert.AreEqual(catalog.Guides.Count, starts.Count(button => button.GetComponentInChildren<Text>().text.Contains("受控找牌")));
                Assert.AreEqual(catalog.Guides.Count, starts.Count(button => button.GetComponentInChildren<Text>().text.Contains("1 次撤销")));
                Assert.AreEqual(catalog.Guides.Count, starts.Count(button => button.GetComponentInChildren<Text>().text.Contains("大饰品池教学")));

                Find(root.transform, "StrategyGuideStartButton-GUIDE-S14-MECH-SPELL-SATELLITE-showcase")
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Assert.AreEqual("GUIDE-S14-MECH-SPELL-SATELLITE", selected);
                Assert.AreEqual("showcase", selectedProfile);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SelectionRendersAdditionalEntryProfileWithoutGuideSpecificLayoutBranch()
        {
            var root = new GameObject("StrategyGuideProfileSelectionRoot", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = catalog.Guides[0];
                var guided = JsonUtility.FromJson<StrategyGuideEntryProfileDefinition>(
                    JsonUtility.ToJson(guide.EntryProfiles.Single(profile =>
                        profile.Difficulty == StrategyGuideDifficulties.Showcase)));
                guided.ProfileId = "guided-probe";
                guided.Difficulty = StrategyGuideDifficulties.GuidedDiscover;
                guided.Title = "初级模式";
                guided.EnglishTitle = "Guided";
                guide.EntryProfiles.Add(guided);
                string selectedProfile = null;

                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, profileId) => selectedProfile = profileId,
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f)).Build();

                var button = Find(root.transform, "StrategyGuideStartButton-" + guide.GuideId + "-guided-probe")
                    .Single()
                    .GetComponent<Button>();
                button.onClick.Invoke();

                Assert.AreEqual("guided-probe", selectedProfile);
                Assert.GreaterOrEqual(button.GetComponent<LayoutElement>().minHeight, UnityTavernUiStyle.TouchHeight);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SelectionCopiesOneGuideCodeAndChoosesProfileAfterValidation()
        {
            var root = new GameObject("StrategyGuidePortableUiRoot", typeof(RectTransform));
            var previousClipboard = GUIUtility.systemCopyBuffer;
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = catalog.Guides[0];
                var profile = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.GuidedDiscover);
                StrategyGuideImportResult accepted = null;

                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f),
                    resolvedVersion: version,
                    startImportedGuide: result => accepted = result).Build();

                Assert.AreEqual(
                    catalog.Guides.Count,
                    root.GetComponentsInChildren<Button>(true).Count(button =>
                        button.name.StartsWith("StrategyGuideCopyCodeButton-", StringComparison.Ordinal)));
                Assert.AreEqual(
                    catalog.Guides.Count * 2,
                    root.GetComponentsInChildren<Button>(true).Count(button =>
                        button.name.StartsWith("StrategyGuideSharePreviewButton-", StringComparison.Ordinal)));
                Find(root.transform, "StrategyGuideCopyCodeButton-" + guide.GuideId)
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();
                StringAssert.StartsWith(StrategyGuidePortableCodeService.CodePrefix + ".", GUIUtility.systemCopyBuffer);
                var imported = StrategyGuidePortableCodeService.Import(GUIUtility.systemCopyBuffer, version);
                Assert.IsTrue(imported.IsCompatible);
                Assert.IsNull(imported.Profile);

                Find(root.transform, "StrategyGuideImportButton").Single().GetComponent<Button>().onClick.Invoke();
                var input = Find(root.transform, "StrategyGuideImportCodeInput").Single().GetComponent<InputField>();
                input.text = GUIUtility.systemCopyBuffer;
                Find(root.transform, "StrategyGuideImportValidateButton").Single().GetComponent<Button>().onClick.Invoke();

                var start = Find(root.transform, "StrategyGuideImportStartButton").Single().GetComponent<Button>();
                Assert.IsFalse(start.gameObject.activeSelf);
                StringAssert.Contains(guide.Title, Find(root.transform, "StrategyGuideImportSummary").Single().GetComponent<Text>().text);
                var choices = root.GetComponentsInChildren<Button>(true)
                    .Where(button => button.name.StartsWith("StrategyGuideImportProfileButton-", StringComparison.Ordinal))
                    .ToList();
                Assert.AreEqual(guide.EntryProfiles.Count, choices.Count);
                Assert.IsTrue(choices.All(button =>
                    button.GetComponent<LayoutElement>().minHeight >= UnityTavernUiStyle.TouchHeight));
                Find(root.transform, "StrategyGuideImportProfileButton-" + profile.ProfileId)
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Assert.NotNull(accepted);
                Assert.IsTrue(accepted.IsCompatible);
                Assert.AreEqual(guide.GuideId, accepted.Guide.GuideId);
                Assert.AreEqual(profile.ProfileId, accepted.Profile.ProfileId);
            }
            finally
            {
                GUIUtility.systemCopyBuffer = previousClipboard;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ImportProfileChoicesRenderAdditionalProfileWithoutGuideSpecificBranch()
        {
            var root = new GameObject("StrategyGuidePortableFourthProfileUiRoot", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = catalog.Guides[0];
                var probe = JsonUtility.FromJson<StrategyGuideEntryProfileDefinition>(
                    JsonUtility.ToJson(guide.EntryProfiles.Single(item =>
                        item.Difficulty == StrategyGuideDifficulties.GuidedDiscover)));
                probe.ProfileId = "guided-probe";
                probe.Title = "额外教学入口";
                probe.EnglishTitle = "Additional lesson";
                guide.EntryProfiles.Add(probe);
                StrategyGuideImportResult accepted = null;

                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f),
                    resolvedVersion: version,
                    startImportedGuide: result => accepted = result).Build();

                Find(root.transform, "StrategyGuideImportButton").Single().GetComponent<Button>().onClick.Invoke();
                Find(root.transform, "StrategyGuideImportCodeInput").Single().GetComponent<InputField>().text =
                    StrategyGuidePortableCodeService.ExportGuide(catalog, guide.GuideId, version);
                Find(root.transform, "StrategyGuideImportValidateButton").Single().GetComponent<Button>().onClick.Invoke();

                var button = Find(root.transform, "StrategyGuideImportProfileButton-guided-probe")
                    .Single()
                    .GetComponent<Button>();
                Assert.GreaterOrEqual(button.GetComponent<LayoutElement>().minHeight, UnityTavernUiStyle.TouchHeight);
                StringAssert.Contains("额外教学入口", button.GetComponentInChildren<Text>().text);
                button.onClick.Invoke();
                Assert.NotNull(accepted);
                Assert.AreEqual("guided-probe", accepted.Profile.ProfileId);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ImportExactProfileDeepLinkKeepsSingleConfirmAction()
        {
            var root = new GameObject("StrategyGuidePortableDeepLinkUiRoot", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = catalog.Guides[0];
                var profile = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.OpenBuild);
                StrategyGuideImportResult accepted = null;

                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f),
                    resolvedVersion: version,
                    startImportedGuide: result => accepted = result).Build();

                Find(root.transform, "StrategyGuideImportButton").Single().GetComponent<Button>().onClick.Invoke();
                Find(root.transform, "StrategyGuideImportCodeInput").Single().GetComponent<InputField>().text =
                    StrategyGuidePortableCodeService.Export(catalog, guide.GuideId, profile.ProfileId, version);
                Find(root.transform, "StrategyGuideImportValidateButton").Single().GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(
                    0,
                    root.GetComponentsInChildren<Button>(true).Count(button =>
                        button.gameObject.activeInHierarchy &&
                        button.name.StartsWith("StrategyGuideImportProfileButton-", StringComparison.Ordinal)));
                var start = Find(root.transform, "StrategyGuideImportStartButton").Single().GetComponent<Button>();
                Assert.IsTrue(start.gameObject.activeSelf);
                Assert.IsTrue(start.interactable);
                start.onClick.Invoke();
                Assert.NotNull(accepted);
                Assert.AreEqual(profile.ProfileId, accepted.Profile.ProfileId);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SelectionKeepsStartDisabledWhenImportedCodeIsInvalid()
        {
            var root = new GameObject("StrategyGuideInvalidPortableUiRoot", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
                new StrategyGuideSelectionView(
                    root.transform,
                    StrategyGuideCatalogLoader.LoadFromResources(),
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f),
                    resolvedVersion: version,
                    startImportedGuide: _ => Assert.Fail("Rejected code must not start a session.")).Build();

                Find(root.transform, "StrategyGuideImportButton").Single().GetComponent<Button>().onClick.Invoke();
                Find(root.transform, "StrategyGuideImportCodeInput").Single().GetComponent<InputField>().text = "not-a-guide-code";
                Find(root.transform, "StrategyGuideImportValidateButton").Single().GetComponent<Button>().onClick.Invoke();

                Assert.IsFalse(Find(root.transform, "StrategyGuideImportStartButton").Single().GetComponent<Button>().interactable);
                Assert.AreEqual(
                    UnityTavernUiStyle.DangerRed,
                    Find(root.transform, "StrategyGuideImportSummary").Single().GetComponent<Text>().color);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TrainerBuildsCompactGuideHudAndKeepsCoreRecruitControlsVisible()
        {
            var root = new GameObject("StrategyGuideTrainerRoot", typeof(RectTransform));
            try
            {
                var session = Start("GUIDE-S14-BEAST-LOBSTER-RALLY");
                new UnityTavernTrainerView(
                    root.transform,
                    session.MatchService,
                    new LocalAdvisorService(),
                    () => { },
                    strategyGuideSession: session).Build();

                Assert.AreEqual(1, Find(root.transform, "UnityStrategyGuideHud").Count);
                Assert.IsNotEmpty(Find(root.transform, "UnityStrategyGuideInstruction").Single().GetComponent<Text>().text);
                Assert.AreEqual(0, Find(root.transform, "UnityQuickToolsButton").Count);
                var refresh = Find(root.transform, "UnityQuickRefreshButton").Single().GetComponent<Button>();
                Assert.IsFalse(refresh.interactable);
                StringAssert.Contains("模式锁定", refresh.GetComponentInChildren<Text>(true).text);
                Assert.IsFalse(Find(root.transform, "UnityHeroBadge").Single().GetComponent<Button>().interactable);

                var undo = Find(root.transform, "UnityStrategyGuideUndoButton").Single().GetComponent<Button>();
                Assert.IsFalse(undo.interactable);
                Assert.GreaterOrEqual(undo.GetComponent<LayoutElement>().minHeight, UnityTavernUiStyle.TouchHeight);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GuideHudUndoButtonConsumesTheSingleUndoAndRefreshesProgress()
        {
            var root = new GameObject("StrategyGuideUndoUiRoot", typeof(RectTransform));
            try
            {
                var session = Start("GUIDE-S14-BEAST-LOBSTER-RALLY");
                var shop = session.MatchService.State.Player.Tavern.Shop;
                session.Apply(new GameCommand(
                    GameCommandType.BuyMinion,
                    shop.FindIndex(card => card.InstanceId == "player-guide-beast-scarab")));
                new UnityTavernTrainerView(
                    root.transform,
                    session.MatchService,
                    new LocalAdvisorService(),
                    () => { },
                    strategyGuideSession: session).Build();

                var undo = Find(root.transform, "UnityStrategyGuideUndoButton").Single().GetComponent<Button>();
                Assert.IsTrue(undo.interactable);
                undo.onClick.Invoke();

                Assert.AreEqual(0, session.UndoUsesRemaining);
                Assert.IsFalse(session.ActionProgress.Single(item => item.ActionId == "buy-scarab").IsComplete);
                Assert.IsTrue(session.MatchService.State.Player.Tavern.Shop.Any(card => card.InstanceId == "player-guide-beast-scarab"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GuidedHudDisclosesControlledOfferAndOmitsUnavailableUndo()
        {
            var root = new GameObject("StrategyGuideControlledOfferUiRoot", typeof(RectTransform));
            try
            {
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var session = StrategyGuideSession.Start(
                    catalog,
                    "GUIDE-S14-DEMON-TAVERN-CONSUME",
                    ResolveSeason14(),
                    profileId: "guided");

                new UnityTavernTrainerView(
                    root.transform,
                    session.MatchService,
                    new LocalAdvisorService(),
                    () => { },
                    () => { },
                    strategyGuideSession: session).Build();

                var instruction = Find(root.transform, "UnityStrategyGuideInstruction").Single().GetComponent<Text>().text;
                StringAssert.Contains("受控发牌", instruction);
                StringAssert.Contains("必含目标", instruction);
                StringAssert.Contains("实际游戏以正常概率为准", instruction);
                Assert.AreEqual(0, Find(root.transform, "UnityStrategyGuideUndoButton").Count);
                Assert.AreEqual(1, Find(root.transform, "UnityStrategyGuideRestartButton").Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DifficultHudShowsGreaterTrinketGateAndProbabilityDisclosureWithoutUndo()
        {
            var root = new GameObject("StrategyGuideDifficultHudRoot", typeof(RectTransform));
            try
            {
                var session = StrategyGuideSession.Start(
                    StrategyGuideCatalogLoader.LoadFromResources(),
                    "GUIDE-S14-BEAST-LOBSTER-RALLY",
                    ResolveSeason14(),
                    profileId: "difficult");

                new UnityTavernTrainerView(
                    root.transform,
                    session.MatchService,
                    new LocalAdvisorService(),
                    () => { },
                    () => { },
                    strategyGuideSession: session).Build();

                var instruction = Find(root.transform, "UnityStrategyGuideInstruction").Single().GetComponent<Text>();
                StringAssert.Contains("大饰品", instruction.text);
                StringAssert.Contains("至少3只野兽", instruction.text);
                StringAssert.Contains("实际游戏以正常概率为准", instruction.text);
                Assert.GreaterOrEqual(instruction.fontSize, 14);
                Assert.AreEqual(0, Find(root.transform, "UnityStrategyGuideUndoButton").Count);
                Assert.AreEqual(1, Find(root.transform, "UnityStrategyGuideRestartButton").Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GuideShapingSpellsRenderInsideHandAndExecuteWithoutOrdinaryHandSideEffects()
        {
            var root = CreateTrainerRoot("StrategyGuideShapingSlotUiRoot");
            try
            {
                var session = Start("GUIDE-S14-MECH-SPELL-SATELLITE");
                var state = session.MatchService.State;
                var tavern = state.Player.Tavern;
                state.Player.Board.Clear();
                tavern.AdvancedMechanics = new AdvancedMechanicState();
                var handIds = tavern.Hand.Select(card => card.InstanceId).ToArray();
                var round = state.Round;
                var phase = state.Phase;
                var spellsThisTurn = tavern.TavernSpellsCastThisTurn;
                var spellsThisGame = tavern.TavernSpellsCastThisGame;
                var cardsPlayedThisTurn = tavern.CardsPlayedThisTurn;

                BuildTrainer(root, session);

                var hand = Find(root.transform, "UnityHandZone").Single();
                var currentSpells = session.MatchService.GetCurrentGuideShapingSpells();
                var shapingCards = hand.GetComponentsInChildren<UnityTavernCardComponent>(true)
                    .Where(card => StrategyGuideShapingSpells.Contains(card.Card?.CardId))
                    .ToList();
                Assert.AreEqual(0, Find(root.transform, "UnityStrategyGuideShapingSpellSlot").Count);
                Assert.AreEqual(
                    handIds.Length + currentSpells.Count,
                    hand.GetComponentsInChildren<Transform>(true).Count(item =>
                        item.name.StartsWith("UnityHandZoneSlot-", StringComparison.Ordinal)));
                Assert.AreEqual(currentSpells.Count, shapingCards.Count);
                CollectionAssert.AreEquivalent(
                    currentSpells.Select(spell => spell.CardId),
                    shapingCards.Select(card => card.Card.CardId));
                StringAssert.Contains("塑造法术 2", hand.GetComponentsInChildren<Text>(true)
                    .Single(text => text.name == "UnityZoneSubtitle").text);

                var directSpell = currentSpells.First(spell => !session.MatchService.RequiresPlayerTarget(spell));
                var directDrag = new UnityTavernDragContext(
                    directSpell,
                    UnityTavernDragSource.GuideShapingSpell,
                    currentSpells.IndexOf(directSpell),
                    false);
                Assert.IsTrue(UnityTavernDragController.TryBuildDropCommand(
                    directDrag,
                    UnityTavernDropTarget.CastZone,
                    -1,
                    out var directCommand));
                session.Apply(directCommand);

                Assert.AreEqual(round, state.Round);
                Assert.AreEqual(phase, state.Phase);
                CollectionAssert.AreEqual(handIds, tavern.Hand.Select(card => card.InstanceId).ToArray());
                Assert.AreEqual(spellsThisTurn, tavern.TavernSpellsCastThisTurn);
                Assert.AreEqual(spellsThisGame, tavern.TavernSpellsCastThisGame);
                Assert.AreEqual(cardsPlayedThisTurn, tavern.CardsPlayedThisTurn);
                Assert.AreEqual(currentSpells.Count - 1, tavern.GuideShapingSpellCardIds.Count);
                Assert.AreEqual(currentSpells.Count == 1, tavern.GuideShapingSpellConsumed);
                Assert.AreEqual(currentSpells.Count - 1, tavern.GuideShapingSpellCardIds.Count(cardId =>
                    string.Equals(cardId, directSpell.CardId, StringComparison.Ordinal)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GuideRecruitCommandsKeepStableVisibleButtonsWhenLocked()
        {
            var root = CreateTrainerRoot("StrategyGuideStableRecruitActionsRoot");
            try
            {
                BuildTrainer(root, Start("GUIDE-S14-BEAST-LOBSTER-RALLY", "showcase"));

                foreach (var name in new[]
                         {
                             "UnityQuickRefreshButton",
                             "UnityQuickFreezeButton",
                             "UnityQuickUpgradeButton"
                         })
                {
                    var button = Find(root.transform, name).Single().GetComponent<Button>();
                    Assert.IsFalse(button.interactable, name);
                    Assert.GreaterOrEqual(button.GetComponent<LayoutElement>().preferredHeight, 48f, name);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DifficultGuideKeepsAllowedRecruitCommandsVisibleAndUsable()
        {
            var root = CreateTrainerRoot("StrategyGuideAllowedRecruitActionsRoot");
            try
            {
                var session = Start("GUIDE-S14-BEAST-LOBSTER-RALLY", "difficult");
                session.MatchService.State.Player.Tavern.Gold = 10;
                BuildTrainer(root, session);

                foreach (var name in new[]
                         {
                             "UnityQuickRefreshButton",
                             "UnityQuickFreezeButton",
                             "UnityQuickUpgradeButton"
                         })
                {
                    var button = Find(root.transform, name).Single().GetComponent<Button>();
                    Assert.IsTrue(button.interactable, name);
                    StringAssert.DoesNotContain("模式锁定", button.GetComponentInChildren<Text>(true).text, name);
                }

                var frozen = session.MatchService.State.Player.Tavern.Frozen;
                Find(root.transform, "UnityQuickFreezeButton").Single().GetComponent<Button>().onClick.Invoke();
                Assert.AreNotEqual(frozen, session.MatchService.State.Player.Tavern.Frozen);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GuideBattlecryShapingKeepsSecondaryTargetActiveUntilSecondDrop()
        {
            var root = CreateTrainerRoot("StrategyGuideBattlecryShapingUiRoot");
            try
            {
                var session = Start("GUIDE-S14-BEAST-LOBSTER-RALLY");
                var state = session.MatchService.State;
                state.Player.Board.Clear();
                state.Player.Tavern.Hand.Clear();
                var source = CreateMinion(session, "BG28_303", "guide-ui-battlecry-source");
                source.Keywords.Add(Keyword.Battlecry);
                var victim = CreateMinion(session, "BG28_300", "guide-ui-battlecry-victim");
                victim.Tribes.Clear();
                victim.Tribes.Add(Tribe.Undead);
                state.Player.Board.Add(source);
                state.Player.Board.Add(victim);
                SetGuideShapingSpell(session, StrategyGuideShapingSpells.Battlecry);
                var battlecries = state.Player.Tavern.BattlecriesTriggeredThisGame;

                BuildTrainer(root, session);

                root.GetComponentsInChildren<UnityTavernCardComponent>(true)
                    .Single(card => card.Card?.CardId == StrategyGuideShapingSpells.Battlecry)
                    .GetComponent<Button>()
                    .onClick.Invoke();
                var controller = Find(root.transform, "UnityTavernTrainer")
                    .Single()
                    .GetComponent<UnityTavernTrainerController>();
                controller.HandleDrop(UnityTavernDropTarget.PlayerBoard, 0);

                Assert.IsFalse(state.Player.Tavern.GuideShapingSpellConsumed);
                Assert.AreEqual(StrategyGuideShapingSpells.Battlecry, state.Player.Tavern.GuideShapingSpellCardId);
                Assert.AreEqual(battlecries, state.Player.Tavern.BattlecriesTriggeredThisGame);
                Assert.IsTrue(state.Player.Board.Any(card => card.InstanceId == victim.InstanceId));
                Assert.AreEqual(0, Find(root.transform, "UnityTargetingCancelButton").Count);
                Assert.IsFalse(controller.CancelCurrentTargeting());
                Assert.IsFalse(state.Player.Tavern.GuideShapingSpellConsumed);
                Assert.AreEqual(StrategyGuideShapingSpells.Battlecry, state.Player.Tavern.GuideShapingSpellCardId);
                var cards = root.GetComponentsInChildren<UnityTavernCardComponent>(true);
                Assert.AreEqual(
                    UnityTavernTargetingState.ConfirmedTarget,
                    cards.Single(card => card.Card?.InstanceId == source.InstanceId).TargetingState);
                Assert.AreEqual(
                    UnityTavernTargetingState.Candidate,
                    cards.Single(card => card.Card?.InstanceId == victim.InstanceId).TargetingState);

                controller.HandleDrop(UnityTavernDropTarget.PlayerBoard, 1);

                Assert.IsTrue(state.Player.Tavern.GuideShapingSpellConsumed);
                Assert.IsNull(state.Player.Tavern.GuideShapingSpellCardId);
                Assert.AreEqual(battlecries + 1, state.Player.Tavern.BattlecriesTriggeredThisGame);
                Assert.IsFalse(state.Player.Board.Any(card => card.InstanceId == victim.InstanceId));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GuideShapingDragMapsPlayerBoardToGuideCommand()
        {
            var drag = new UnityTavernDragContext(
                new MinionInstance { CardKind = CardKind.TavernSpell },
                UnityTavernDragSource.GuideShapingSpell,
                0,
                true);

            Assert.IsTrue(UnityTavernDragController.TryBuildDropCommand(
                drag,
                UnityTavernDropTarget.PlayerBoard,
                3,
                out var command));
            Assert.AreEqual(GameCommandType.UseGuideShapingSpell, command.Type);
            Assert.AreEqual(3, command.TargetIndex);
            Assert.AreEqual(TargetZone.FriendlyBoard, command.TargetZone);
        }

        [Test]
        public void GuideDirectShapingDragMapsCastZoneToGuideCommand()
        {
            var drag = new UnityTavernDragContext(
                new MinionInstance
                {
                    CardId = StrategyGuideShapingSpells.EndOfTurn,
                    CardKind = CardKind.TavernSpell
                },
                UnityTavernDragSource.GuideShapingSpell,
                2,
                false);

            Assert.IsTrue(UnityTavernDragController.TryBuildDropCommand(
                drag,
                UnityTavernDropTarget.CastZone,
                -1,
                out var command));
            Assert.AreEqual(GameCommandType.UseGuideShapingSpell, command.Type);
            Assert.AreEqual(StrategyGuideShapingSpells.EndOfTurn, command.CardId);
            Assert.AreEqual(TargetZone.Unspecified, command.TargetZone);
            Assert.IsFalse(UnityTavernDragController.CanDrop(
                drag,
                UnityTavernDropTarget.PlayerBoard,
                0));
        }

        [Test]
        public void GuideDrawersOverlayPlaySurfaceAndRemainMutuallyExclusive()
        {
            var root = CreateTrainerRoot("StrategyGuideDrawerUiRoot");
            try
            {
                var session = Start("GUIDE-S14-BEAST-LOBSTER-RALLY");
                BuildTrainer(root, session);

                var playSurface = Find(root.transform, "UnityPlaySurface").Single().GetComponent<RectTransform>();
                var anchorMin = playSurface.anchorMin;
                var anchorMax = playSurface.anchorMax;
                var offsetMin = playSurface.offsetMin;
                var offsetMax = playSurface.offsetMax;
                var leftToggle = Find(root.transform, "UnityStrategyGuideGoalDrawerToggle").Single();
                var rightToggle = Find(root.transform, "UnityRightPanelDrawerToggle").Single();
                Assert.IsNotNull(leftToggle.GetComponent<UnitySelectableFocusRing>());
                Assert.IsNotNull(rightToggle.GetComponent<UnitySelectableFocusRing>());
                Assert.GreaterOrEqual(leftToggle.GetComponent<RectTransform>().sizeDelta.y, UnityTavernUiStyle.TouchHeight);
                Assert.GreaterOrEqual(rightToggle.GetComponent<RectTransform>().sizeDelta.y, UnityTavernUiStyle.TouchHeight);

                leftToggle.GetComponent<Button>().onClick.Invoke();

                var drawer = Find(root.transform, "UnityStrategyGuideGoalDrawer").Single();
                Assert.IsTrue(drawer.GetComponent<LayoutElement>().ignoreLayout);
                Assert.IsTrue(drawer.GetComponentsInChildren<Text>(true).All(text => text.fontSize >= 14));
                var rebuiltSurface = Find(root.transform, "UnityPlaySurface").Single().GetComponent<RectTransform>();
                Assert.AreEqual(anchorMin, rebuiltSurface.anchorMin);
                Assert.AreEqual(anchorMax, rebuiltSurface.anchorMax);
                Assert.AreEqual(offsetMin, rebuiltSurface.offsetMin);
                Assert.AreEqual(offsetMax, rebuiltSurface.offsetMax);
                Assert.AreEqual(0, Find(root.transform, "UnityRightPanel").Count);

                Find(root.transform, "UnityRightPanelDrawerToggle").Single().GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(0, Find(root.transform, "UnityStrategyGuideGoalDrawer").Count);
                Assert.AreEqual(1, Find(root.transform, "UnityRightPanel").Count);

                Find(root.transform, "UnityStrategyGuideGoalDrawerToggle").Single().GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(1, Find(root.transform, "UnityStrategyGuideGoalDrawer").Count);
                Assert.AreEqual(0, Find(root.transform, "UnityRightPanel").Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GuideRightActionsShowTasksAndGrowthWithoutSandboxActionPanel()
        {
            var root = CreateTrainerRoot("StrategyGuideRightActionsUiRoot");
            try
            {
                var session = Start("GUIDE-S14-BEAST-LOBSTER-RALLY", "guided");
                BuildTrainer(root, session);

                Find(root.transform, "UnityRightPanelDrawerToggle").Single().GetComponent<Button>().onClick.Invoke();

                var panel = Find(root.transform, "UnityRightPanel").Single();
                var rightActions = Find(panel, "UnityStrategyGuideRightActions").Single();
                Assert.AreEqual(0, Find(panel, "UnityActionPanel").Count);
                Assert.AreEqual(
                    session.ActionProgress.Count,
                    panel.GetComponentsInChildren<Transform>(true).Count(item =>
                        item.name.StartsWith("UnityStrategyGuideRightAction-", StringComparison.Ordinal)));
                Assert.Greater(session.GrowthProgress.Count, 0);
                Assert.AreEqual(
                    session.GrowthProgress.Count,
                    panel.GetComponentsInChildren<Transform>(true).Count(item =>
                        item.name.StartsWith("UnityStrategyGuideRightGrowth-", StringComparison.Ordinal)));
                foreach (var sandboxControl in new[]
                {
                    "UnityRefreshButton",
                    "UnityFreezeButton",
                    "UnityUpgradeButton",
                    "UnityReplayButton",
                    "UnityToolsButton"
                })
                {
                    Assert.AreEqual(0, Find(panel, sandboxControl).Count, sandboxControl);
                }
                var tabs = panel.GetComponentsInChildren<Button>(true)
                    .Where(button => button.name.StartsWith("UnityRightPanelTab-", StringComparison.Ordinal))
                    .ToList();
                Assert.AreEqual(4, tabs.Count);
                Assert.IsTrue(tabs.All(button => button.GetComponent<UnitySelectableFocusRing>() != null));
                Assert.IsTrue(rightActions.GetComponentsInChildren<Text>(true).All(text => text.fontSize >= 14));
                Assert.IsTrue(tabs.All(button => button.GetComponentInChildren<Text>(true).fontSize >= 14));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GuideGoalDrawerRendersSevenStatusRowsAndGrowthProgress()
        {
            var root = CreateTrainerRoot("StrategyGuideGoalStatusUiRoot");
            try
            {
                var session = Start("GUIDE-S14-BEAST-LOBSTER-RALLY", "guided");
                var targets = session.Guide.FinalComposition;
                var board = session.MatchService.State.Player.Board;
                board.Clear();
                board.Add(CreateFinalCard(session, targets[0], "complete", true));
                board.Add(CreateFinalCard(session, targets[2], "wrong-position-a", true));
                board.Add(CreateFinalCard(session, targets[1], "wrong-position-b", true));
                board.Add(CreateFinalCard(session, targets[3], "state-mismatch", false));
                session.Synchronize();
                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        StrategyGuideFinalSlotStatus.Complete,
                        StrategyGuideFinalSlotStatus.PositionWrong,
                        StrategyGuideFinalSlotStatus.StateMismatch,
                        StrategyGuideFinalSlotStatus.Missing
                    },
                    session.FinalSlotProgress.Select(slot => slot.Status).Distinct().ToArray());

                BuildTrainer(root, session);
                Find(root.transform, "UnityStrategyGuideGoalDrawerToggle").Single().GetComponent<Button>().onClick.Invoke();

                var drawer = Find(root.transform, "UnityStrategyGuideGoalDrawer").Single();
                var slotRows = drawer.GetComponentsInChildren<Transform>(true)
                    .Where(item => item.name.StartsWith("UnityStrategyGuideGoalSlot-", StringComparison.Ordinal))
                    .ToList();
                Assert.AreEqual(7, slotRows.Count);
                Assert.AreEqual(
                    4,
                    slotRows.Select(row => row.GetComponent<Image>().color).Distinct().Count());
                Assert.AreEqual(
                    4,
                    slotRows.Select(row => row.GetComponent<Outline>().effectColor).Distinct().Count());
                foreach (var slot in session.FinalSlotProgress)
                {
                    var status = Find(drawer, "UnityStrategyGuideGoalSlotStatus-" + slot.SlotIndex)
                        .Single()
                        .GetComponent<Text>();
                    Assert.IsNotEmpty(status.text);
                    Assert.AreEqual(UnityTavernUiStyle.Text, status.color);
                    Assert.GreaterOrEqual(status.fontSize, 14);
                }
                Assert.Greater(session.GrowthProgress.Count, 0);
                foreach (var growth in session.GrowthProgress)
                {
                    var growthText = Find(drawer, "UnityStrategyGuideGrowthText-" + growth.Key)
                        .Single()
                        .GetComponent<Text>();
                    StringAssert.Contains(growth.CurrentValue + "/" + growth.RequiredValue, growthText.text);
                    Assert.GreaterOrEqual(growthText.fontSize, 14);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FreeExploreKeepsFinalBoardDrawerAvailable()
        {
            var root = CreateTrainerRoot("StrategyGuideFreeExploreGoalUiRoot");
            try
            {
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = catalog.GetGuide("GUIDE-S14-MECH-SPELL-SATELLITE");
                guide.EntryProfiles.Single(profile =>
                    profile.Difficulty == StrategyGuideDifficulties.Showcase).RequiredActions.Clear();
                var session = StrategyGuideSession.Start(catalog, guide.GuideId, ResolveSeason14());
                FillFinalBoard(session);
                session.MatchService.State.LastResult = new CombatOutput { Winner = CombatWinner.Player };
                session.MatchService.State.Phase = MatchPhase.Result;
                session.Synchronize();
                BuildTrainer(root, session);

                Find(root.transform, "UnityStrategyGuideFreeExploreButton").Single().GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(StrategyGuideRunState.FreeExplore, session.RunState);
                Assert.AreEqual(0, Find(root.transform, "UnityStrategyGuideHud").Count);
                var toggle = Find(root.transform, "UnityStrategyGuideGoalDrawerToggle").Single();
                Assert.IsNotNull(toggle.GetComponent<UnitySelectableFocusRing>());
                toggle.GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(1, Find(root.transform, "UnityStrategyGuideGoalDrawer").Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CompletedGuideShowsOnlyTheThreeFrozenPostWinChoices()
        {
            var root = new GameObject("StrategyGuideCompletionUiRoot", typeof(RectTransform));
            try
            {
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = catalog.GetGuide("GUIDE-S14-MECH-SPELL-SATELLITE");
                guide.EntryProfiles.Single(profile =>
                    profile.Difficulty == StrategyGuideDifficulties.Showcase).RequiredActions.Clear();
                var session = StrategyGuideSession.Start(catalog, guide.GuideId, ResolveSeason14());
                FillFinalBoard(session);
                session.MatchService.State.LastResult = new CombatOutput { Winner = CombatWinner.Player };
                session.MatchService.State.Phase = MatchPhase.Result;
                session.Synchronize();

                new UnityTavernTrainerView(
                    root.transform,
                    session.MatchService,
                    new LocalAdvisorService(),
                    () => { },
                    strategyGuideSession: session).Build();

                Assert.AreEqual(1, Find(root.transform, "UnityStrategyGuideCompletionOverlay").Count);
                Assert.AreEqual(1, Find(root.transform, "UnityStrategyGuideFreeExploreButton").Count);
                Assert.AreEqual(1, Find(root.transform, "UnityStrategyGuideCompletionRestartButton").Count);
                Assert.AreEqual(1, Find(root.transform, "UnityStrategyGuideReturnButton").Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static StrategyGuideSession Start(string guideId)
        {
            return StrategyGuideSession.Start(StrategyGuideCatalogLoader.LoadFromResources(), guideId, ResolveSeason14());
        }

        private static StrategyGuideSession Start(string guideId, string profileId)
        {
            return StrategyGuideSession.Start(
                StrategyGuideCatalogLoader.LoadFromResources(),
                guideId,
                ResolveSeason14(),
                profileId: profileId);
        }

        private static GameObject CreateTrainerRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            return root;
        }

        private static void BuildTrainer(GameObject root, StrategyGuideSession session)
        {
            new UnityTavernTrainerView(
                root.transform,
                session.MatchService,
                new LocalAdvisorService(),
                () => { },
                strategyGuideSession: session).Build();
        }

        private static void SetGuideShapingSpell(StrategyGuideSession session, string cardId)
        {
            var tavern = session.MatchService.State.Player.Tavern;
            tavern.GuideShapingSpellCardId = cardId;
            tavern.GuideShapingSpellCardIds = new List<string> { cardId };
            tavern.GuideShapingSpellRound = session.MatchService.State.Round;
            tavern.GuideShapingSpellConsumed = false;
        }

        private static MinionInstance CreateMinion(StrategyGuideSession session, string cardId, string instanceId)
        {
            var card = MinionFactory.Create(
                session.MatchService.Catalogs.Minions.GetByCardId(cardId),
                BoardSide.Player,
                instanceId,
                false,
                PoolSource.Copy,
                0);
            card.InstanceId = instanceId;
            return card;
        }

        private static MinionInstance CreateFinalCard(
            StrategyGuideSession session,
            StrategyGuideCardDefinition target,
            string instanceId,
            bool meetsRequirements)
        {
            var card = MinionFactory.Create(
                session.MatchService.Catalogs.Minions.GetByCardId(target.CardId),
                BoardSide.Player,
                instanceId,
                meetsRequirements ? target.Golden : !target.Golden,
                PoolSource.Copy,
                0);
            card.InstanceId = instanceId;
            if (meetsRequirements)
            {
                card.Attack = Math.Max(card.Attack, target.MinimumAttack);
                card.Health = Math.Max(card.Health, target.MinimumHealth);
                card.MaxHealth = Math.Max(card.MaxHealth, card.Health);
            }
            else
            {
                card.Attack = 0;
                card.Health = 1;
                card.MaxHealth = 1;
            }
            return card;
        }

        private static ResolvedGameVersion ResolveSeason14()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            return snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
        }

        private static void FillFinalBoard(StrategyGuideSession session)
        {
            session.MatchService.State.Player.Board.Clear();
            foreach (var target in session.Guide.FinalComposition)
            {
                var card = MinionFactory.Create(
                    session.MatchService.Catalogs.Minions.GetByCardId(target.CardId),
                    BoardSide.Player,
                    target.PlacementId,
                    target.Golden,
                    PoolSource.Copy,
                    0);
                card.Attack = Math.Max(card.Attack, target.MinimumAttack);
                card.Health = Math.Max(card.Health, target.MinimumHealth);
                card.MaxHealth = Math.Max(card.MaxHealth, card.Health);
                session.MatchService.State.Player.Board.Add(card);
            }
        }

        private static List<Transform> Find(Transform root, string name)
        {
            var result = new List<Transform>();
            Collect(root, name, result);
            return result;
        }

        private static void Collect(Transform root, string name, ICollection<Transform> result)
        {
            if (root.name == name)
            {
                result.Add(root);
            }
            for (var index = 0; index < root.childCount; index += 1)
            {
                Collect(root.GetChild(index), name, result);
            }
        }
    }
}
