using System;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StrategyGuideAuthoringEditorUiTests
    {
        [Test]
        public void EditorUsesFourReadableStepsAndRendersTemplateDataWithoutGuideBranches()
        {
            WithEditor((root, view, guide, repository) =>
            {
                Assert.AreEqual(4, root.GetComponentsInChildren<Button>(true).Count(button =>
                    button.name.StartsWith("StrategyGuideAuthoringStepButton-", StringComparison.Ordinal)));
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringBasicStep").Count);
                Assert.AreEqual(0, Find(root.transform, "StrategyGuideAuthoringCompositionStep").Count);

                Click(root, "StrategyGuideAuthoringStepButton-1");

                Assert.AreEqual(
                    guide.FinalComposition.Count,
                    root.GetComponentsInChildren<Button>(true).Count(button =>
                        button.name.StartsWith("StrategyGuideAuthoringGoldenButton-", StringComparison.Ordinal)));
                Assert.IsTrue(root.GetComponentsInChildren<Text>(true).All(text => text.fontSize >= 14));
                Assert.IsTrue(root.GetComponentsInChildren<Button>(true).All(button =>
                    button.GetComponent<LayoutElement>().minHeight >= UnityTavernUiStyle.TouchHeight));

                Click(root, "StrategyGuideAuthoringStepButton-2");
                Assert.AreEqual(
                    guide.EntryProfiles.Count,
                    root.GetComponentsInChildren<Transform>(true).Count(item =>
                        item.name.StartsWith("StrategyGuideAuthoringProfile-", StringComparison.Ordinal)));
            });
        }

        [Test]
        public void SelectionOpensDataDrivenTemplatePickerThenLocalEditor()
        {
            var root = new GameObject("StrategyGuideAuthoringSelectionRoot", typeof(RectTransform));
            var repositoryRoot = Path.Combine(Path.GetTempPath(), "learn-hearthstone-authoring-entry-" + Guid.NewGuid().ToString("N"));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var repository = new FileStrategyGuideAuthoringRepository(repositoryRoot);
                var version = snapshot.VersionedContent.CreateResolver().Resolve(
                    GameVersionIds.Season14Preview,
                    snapshot);
                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f),
                    resolvedVersion: version,
                    authoringRepository: repository).Build();

                Click(root, "StrategyGuideAuthoringOpenButton");
                Assert.AreEqual(
                    catalog.Guides.Count,
                    root.GetComponentsInChildren<Button>(true).Count(button =>
                        button.name.StartsWith("StrategyGuideAuthoringTemplateSelectButton-", StringComparison.Ordinal)));

                Click(root, "StrategyGuideAuthoringTemplateSelectButton-" + catalog.Guides[0].GuideId);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringEditor").Count);
                Assert.AreEqual(1, repository.ListDraftIds().Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (Directory.Exists(repositoryRoot))
                {
                    Directory.Delete(repositoryRoot, true);
                }
            }
        }

        [Test]
        public void FourthProfileRendersWithoutChangingTheEditorLayout()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var template = Clone(catalog.Guides[0]);
            var extra = JsonUtility.FromJson<StrategyGuideEntryProfileDefinition>(
                JsonUtility.ToJson(template.EntryProfiles[0]));
            extra.ProfileId = "custom-fourth-entry";
            extra.Title = "第四入口";
            template.EntryProfiles.Add(extra);

            WithEditor((root, view, guide, repository) =>
            {
                Click(root, "StrategyGuideAuthoringStepButton-2");
                Assert.AreEqual(
                    4,
                    root.GetComponentsInChildren<Transform>(true).Count(item =>
                        item.name.StartsWith("StrategyGuideAuthoringProfile-", StringComparison.Ordinal)));
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringProfile-custom-fourth-entry").Count);
            }, template);
        }

        [Test]
        public void TemplatePickerReopensAutosavedDraftWithoutCreatingAnotherIdentity()
        {
            var root = new GameObject("StrategyGuideAuthoringResumeRoot", typeof(RectTransform));
            var repositoryRoot = Path.Combine(Path.GetTempPath(), "learn-hearthstone-authoring-resume-" + Guid.NewGuid().ToString("N"));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = Clone(catalog.Guides[0]);
                guide.Title = "待继续的草稿";
                var repository = new FileStrategyGuideAuthoringRepository(repositoryRoot);
                repository.SaveDraft(new StrategyGuideAuthoringDraft
                {
                    DraftId = "draft-existing-ui",
                    Guide = guide
                });
                var version = snapshot.VersionedContent.CreateResolver().Resolve(
                    GameVersionIds.Season14Preview,
                    snapshot);
                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f),
                    resolvedVersion: version,
                    authoringRepository: repository).Build();

                Click(root, "StrategyGuideAuthoringOpenButton");
                Click(root, "StrategyGuideAuthoringDraftOpenButton-draft-existing-ui");
                var input = Find(root.transform, "StrategyGuideAuthoringTitleInput").Single().GetComponent<InputField>();
                Assert.AreEqual("待继续的草稿", input.text);
                input.text = "继续后的草稿";
                input.onEndEdit.Invoke(input.text);

                Assert.AreEqual(1, repository.ListDraftIds().Count);
                Assert.AreEqual("继续后的草稿", repository.LoadDraft("draft-existing-ui").Guide.Title);
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (Directory.Exists(repositoryRoot))
                {
                    Directory.Delete(repositoryRoot, true);
                }
            }
        }

        [Test]
        public void CompactEditorUsesOneScrollRegionAndKeepsPhysicalTouchTargets()
        {
            var compact = UnityTavernLayoutContext.ForSize(390f, 844f);
            WithEditor((root, view, guide, repository) =>
            {
                Assert.AreEqual(1, root.GetComponentsInChildren<ScrollRect>(true).Length);
                Assert.IsTrue(root.GetComponentsInChildren<Button>(true).All(button =>
                    button.GetComponent<LayoutElement>().minHeight >=
                    compact.CanvasUnitsForPhysicalPixels(UiFactory.MinimumButtonHeight)));
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringHeader").Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringFooter").Count);
            }, layoutContext: compact);
        }

        [Test]
        public void WideEditorUsesContextRailSevenCardTrackAndCompactFooterActions()
        {
            WithEditor((root, view, guide, repository) =>
            {
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringContextRail").Count);
                var railWidth = Find(root.transform, "StrategyGuideAuthoringContextRail")
                    .Single()
                    .GetComponent<LayoutElement>()
                    .preferredWidth;
                var physicalRailWidth = railWidth * UnityTavernLayoutContext.ForSize(1280f, 720f).CanvasScaleFactor;
                Assert.GreaterOrEqual(physicalRailWidth, 240f);
                Assert.LessOrEqual(physicalRailWidth, 320f);
                Assert.AreEqual(4, root.GetComponentsInChildren<Transform>(true).Count(item =>
                    item.name.StartsWith("StrategyGuideAuthoringContextStep-", StringComparison.Ordinal)));
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringAutosaveHint").Count);
                var footerButtons = Find(root.transform, "StrategyGuideAuthoringFooter")
                    .Single()
                    .GetComponentsInChildren<Button>(true);
                Assert.IsTrue(footerButtons.All(button =>
                    button.GetComponent<LayoutElement>().preferredWidth > 0f &&
                    button.GetComponent<LayoutElement>().preferredWidth <= 148f));

                Click(root, "StrategyGuideAuthoringStepButton-1");
                var lineup = Find(root.transform, "StrategyGuideAuthoringLineup").Single();
                Assert.IsInstanceOf<HorizontalLayoutGroup>(lineup.GetComponent<HorizontalOrVerticalLayoutGroup>());
                Assert.AreEqual(7, guide.FinalComposition.Count);
                Assert.AreEqual(guide.FinalComposition.Count, root.GetComponentsInChildren<Image>(true).Count(image =>
                    image.name.StartsWith("StrategyGuideAuthoringCardArt-", StringComparison.Ordinal)));
                Assert.AreEqual(guide.FinalComposition.Count, root.GetComponentsInChildren<Transform>(true).Count(item =>
                    item.name.StartsWith("StrategyGuideAuthoringCardActions-", StringComparison.Ordinal)));
                Assert.AreEqual(
                    "✓",
                    Find(Find(root.transform, "StrategyGuideAuthoringContextStep-0").Single(), "StrategyGuideAuthoringContextState")
                        .Single()
                        .GetComponent<Text>()
                        .text);
                StringAssert.Contains(
                    "当前",
                    Find(Find(root.transform, "StrategyGuideAuthoringContextStep-1").Single(), "StrategyGuideAuthoringContextStatus")
                        .Single()
                        .GetComponent<Text>()
                        .text);
                Assert.AreEqual(1, root.GetComponentsInChildren<ScrollRect>(true).Length);
            });
        }

        [Test]
        public void WideEditorMainScrollUsesFullWidthVerticalScrolling()
        {
            WithEditor((root, view, guide, repository) =>
            {
                var scroll = Find(root.transform, "StrategyGuideAuthoringStepScroll")
                    .Single()
                    .GetComponent<ScrollRect>();

                Assert.IsFalse(scroll.horizontal);
                Assert.IsTrue(scroll.vertical);
                Assert.IsNull(scroll.horizontalScrollbar);
                Assert.NotNull(scroll.verticalScrollbar);
                Assert.AreEqual(
                    ContentSizeFitter.FitMode.Unconstrained,
                    scroll.content.GetComponent<ContentSizeFitter>().horizontalFit);

                var rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(1280f, 720f);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                Assert.AreEqual(scroll.viewport.rect.width, scroll.content.rect.width, 1f);
            });
        }

        [Test]
        public void SameStepSelectionPreservesVerticalScrollPositionAfterRebuild()
        {
            WithEditor((root, view, guide, repository) =>
            {
                var scroll = Find(root.transform, "StrategyGuideAuthoringStepScroll")
                    .Single()
                    .GetComponent<ScrollRect>();
                var removableTribe = guide.ActiveTribes.First(tribe =>
                    !guide.RequiredTribes.Contains(tribe, StringComparer.OrdinalIgnoreCase));
                const float expectedPosition = 0.37f;
                scroll.verticalNormalizedPosition = expectedPosition;

                Click(root, "StrategyGuideAuthoringTribeButton-" + removableTribe);

                Assert.AreEqual(expectedPosition, scroll.verticalNormalizedPosition, 0.001f);
                Assert.IsFalse(repository.LoadDraft("draft-ui-test").Guide.ActiveTribes.Contains(
                    removableTribe,
                    StringComparer.OrdinalIgnoreCase));
            });
        }

        [Test]
        public void TitleEndEditAutosavesAndSuccessfulFreezeStoresImmutableRevision()
        {
            WithEditor((root, view, guide, repository) =>
            {
                var input = Find(root.transform, "StrategyGuideAuthoringTitleInput")
                    .Single()
                    .GetComponent<InputField>();
                input.text = "我的第一套一图流";
                input.onEndEdit.Invoke(input.text);

                Assert.AreEqual(
                    "我的第一套一图流",
                    repository.LoadDraft("draft-ui-test").Guide.Title);

                Click(root, "StrategyGuideAuthoringStepButton-3");
                Click(root, "StrategyGuideAuthoringFreezeButton");

                Assert.NotNull(view.LastFreezeResult);
                Assert.IsTrue(view.LastFreezeResult.Succeeded, string.Join(" | ", view.LastFreezeResult.Diagnostics));
                Assert.IsTrue(repository.ContainsFrozen(view.LastFreezeResult.ContentHash));
                StringAssert.Contains(
                    view.LastFreezeResult.Guide.RevisionId,
                    Find(root.transform, "StrategyGuideAuthoringStatus").Single().GetComponent<Text>().text);
            });
        }

        [Test]
        public void GoldenToggleKeepsFinalGoalAndShowcaseSetupInSync()
        {
            WithEditor((root, view, guide, repository) =>
            {
                Click(root, "StrategyGuideAuthoringStepButton-1");
                var target = guide.FinalComposition[0];
                Click(root, "StrategyGuideAuthoringGoldenButton-" + target.PlacementId);

                var saved = repository.LoadDraft("draft-ui-test").Guide;
                var expected = !target.Golden;
                Assert.IsTrue(saved.FinalComposition
                    .Where(card => card.CardId == target.CardId)
                    .All(card => card.Golden == expected));
                Assert.IsTrue(saved.EntryProfiles
                    .Where(profile => profile.Difficulty == StrategyGuideDifficulties.Showcase)
                    .SelectMany(profile => profile.Placements)
                    .Where(card => card.CardId == target.CardId)
                    .All(card => card.Golden == expected));
            });
        }

        [Test]
        public void FreezeFailureShowsPlayerFacingRecoveryWithoutDiscardingDraft()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var invalid = Clone(catalog.Guides[0]);
            invalid.ActiveTribes.RemoveAt(0);
            WithEditor((root, view, guide, repository) =>
            {
                Click(root, "StrategyGuideAuthoringStepButton-3");
                Click(root, "StrategyGuideAuthoringFreezeButton");

                Assert.NotNull(view.LastFreezeResult);
                Assert.IsFalse(view.LastFreezeResult.Succeeded);
                Assert.AreEqual(1, repository.ListDraftIds().Count);
                var status = Find(root.transform, "StrategyGuideAuthoringStatus").Single().GetComponent<Text>().text;
                StringAssert.Contains("5", status);
                StringAssert.Contains("种族", status);
            }, invalid);
        }

        [Test]
        public void BasicsExposeVersionFilteredHeroTrinketAndTenTribeSelectors()
        {
            WithEditor((root, view, guide, repository) =>
            {
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringHeroPickerButton").Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringLesserTrinketPickerButton").Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringGreaterTrinketPickerButton").Count);
                Assert.AreEqual(10, root.GetComponentsInChildren<Button>(true).Count(button =>
                    button.name.StartsWith("StrategyGuideAuthoringTribeButton-", StringComparison.Ordinal)));

                var originalHero = guide.HeroCardId;
                Click(root, "StrategyGuideAuthoringHeroPickerButton");
                Assert.AreEqual(1, Find(root.transform, "UnityHeroSelectionOverlay").Count);
                var heroChoice = root.GetComponentsInChildren<Button>(true).First(button =>
                    button.interactable &&
                    button.name.StartsWith("UnityHeroSelectionHeroChooseButton-", StringComparison.Ordinal));
                heroChoice.onClick.Invoke();

                var saved = repository.LoadDraft("draft-ui-test").Guide;
                Assert.AreNotEqual(originalHero, saved.HeroCardId);
                Assert.AreEqual(5, saved.ActiveTribes.Count);
            });
        }

        [Test]
        public void TrinketPickerShowsOnlyRequestedSlotAndAutosavesSelection()
        {
            WithEditor((root, view, guide, repository) =>
            {
                var original = guide.GreaterTrinketCardId;
                Click(root, "StrategyGuideAuthoringGreaterTrinketPickerButton");
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringPickerOverlay").Count);
                var choice = root.GetComponentsInChildren<Button>(true).First(button =>
                    button.interactable &&
                    button.name.StartsWith("StrategyGuideAuthoringPickerChooseButton-", StringComparison.Ordinal));
                choice.onClick.Invoke();

                var saved = repository.LoadDraft("draft-ui-test").Guide;
                Assert.AreNotEqual(original, saved.GreaterTrinketCardId);
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
                var selected = version.Snapshot.ForLanguage(false).Trinkets.GetByCardId(saved.GreaterTrinketCardId);
                Assert.AreEqual(TrinketSlotKind.Greater, selected.SlotKind);
                Assert.AreEqual(TrinketOfferPoolStatus.Offerable, selected.OfferPoolStatus);
            });
        }

        [TestCase(844f, 390f)]
        [TestCase(1280f, 720f)]
        public void CoreCardPickersUseAFullScreenOverlayOutsideTheEditorLayout(float width, float height)
        {
            WithEditor((root, view, guide, repository) =>
            {
                Click(root, "StrategyGuideAuthoringStepButton-1");
                foreach (var buttonName in new[]
                         {
                             "StrategyGuideAuthoringCoreMinionAddButton",
                             "StrategyGuideAuthoringCoreSpellAddButton"
                         })
                {
                    Click(root, buttonName);
                    var overlay = Find(root.transform, "StrategyGuideAuthoringPickerOverlay").Single();
                    var editor = Find(root.transform, "StrategyGuideAuthoringEditor").Single();
                    var overlayRect = overlay.GetComponent<RectTransform>();
                    var overlayLayout = overlay.GetComponent<LayoutElement>();

                    Assert.AreSame(editor, overlay.parent);
                    Assert.IsNotNull(overlayLayout);
                    Assert.IsTrue(overlayLayout.ignoreLayout, buttonName);
                    Assert.AreEqual(editor.childCount - 1, overlay.GetSiblingIndex(), buttonName);
                    Assert.AreEqual(Vector2.zero, overlayRect.anchorMin, buttonName);
                    Assert.AreEqual(Vector2.one, overlayRect.anchorMax, buttonName);
                    Assert.AreEqual(Vector2.zero, overlayRect.offsetMin, buttonName);
                    Assert.AreEqual(Vector2.zero, overlayRect.offsetMax, buttonName);
                    Assert.AreEqual(0, overlay.GetComponentsInParent<RectMask2D>(true).Length, buttonName);

                    Click(root, "StrategyGuideAuthoringPickerCloseButton");
                    Assert.AreEqual(0, Find(root.transform, "StrategyGuideAuthoringPickerOverlay").Count);
                }
            }, layoutContext: UnityTavernLayoutContext.ForSize(width, height));
        }

        [TestCase(844f, 390f)]
        [TestCase(1280f, 720f)]
        public void PickerSearchFieldCannotExpandTheModalHeader(float width, float height)
        {
            var layout = UnityTavernLayoutContext.ForSize(width, height);
            var root = new GameObject("StrategyGuideAuthoringPickerGeometryRoot", typeof(RectTransform));
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(
                    width / layout.CanvasScaleFactor,
                    height / layout.CanvasScaleFactor);
                var shell = UiFactory.Panel("StrategyGuideAuthoringPickerGeometryShell", root.transform, Color.black);
                UiFactory.Stretch(shell.GetComponent<RectTransform>());
                UiFactory.Vertical(shell, 8, 8);
                var overlay = StrategyGuideAuthoringPickerModalComponent.CreateModalHost(shell.transform);
                overlay.GetComponent<StrategyGuideAuthoringPickerModalComponent>().Build(
                    new[]
                    {
                        new StrategyGuideAuthoringPickerItem
                        {
                            Id = "BGS_041",
                            Name = "Geometry card",
                            Detail = "Layout check",
                            Group = "Test",
                            CardKind = CardKind.Minion
                        }
                    },
                    null,
                    "Core cards",
                    "Duplicate entries are ignored.",
                    _ => { },
                    () => { },
                    false,
                    layout);

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
                var header = Find(root.transform, "StrategyGuideAuthoringPickerHeader").Single().GetComponent<RectTransform>();
                var scroll = Find(root.transform, "StrategyGuideAuthoringPickerScroll").Single().GetComponent<RectTransform>();
                var input = Find(root.transform, "StrategyGuideAuthoringPickerSearchInput").Single().GetComponent<LayoutElement>();
                var headerPhysicalHeight = header.rect.height * layout.CanvasScaleFactor;
                var scrollPhysicalHeight = scroll.rect.height * layout.CanvasScaleFactor;

                Assert.That(headerPhysicalHeight, Is.InRange(47.5f, 55f));
                Assert.Greater(scrollPhysicalHeight, height * 0.55f);
                Assert.AreEqual(2, input.layoutPriority);
                Assert.AreEqual(
                    UnityTavernUiStyle.TouchHeight,
                    input.preferredHeight * layout.CanvasScaleFactor,
                    0.5f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AdvancedProfileEditorProgressivelyRevealsCardsGiftsOffersAndOpponent()
        {
            WithEditor((root, view, guide, repository) =>
            {
                var profile = guide.EntryProfiles[0];
                Click(root, "StrategyGuideAuthoringStepButton-2");
                Assert.AreEqual(0, Find(root.transform, "StrategyGuideAuthoringPlacements-" + profile.ProfileId).Count);

                Click(root, "StrategyGuideAuthoringAdvancedButton-" + profile.ProfileId);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringPlacements-" + profile.ProfileId).Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringDarkGifts-" + profile.ProfileId).Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringOffers-" + profile.ProfileId).Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringOpponent-" + profile.ProfileId).Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringOpponentPickerButton-" + profile.ProfileId).Count);
            });
        }

        [Test]
        public void ProfileCardPickerExcludesShapingSpellsAndDedicatedSchedulePersists()
        {
            WithEditor((root, view, guide, repository) =>
            {
                var profile = guide.EntryProfiles[0];
                Click(root, "StrategyGuideAuthoringStepButton-2");
                Click(root, "StrategyGuideAuthoringAdvancedButton-" + profile.ProfileId);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringShapingSpells-" + profile.ProfileId).Count);
                Click(root, "StrategyGuideAuthoringPlacementAddButton-" + profile.ProfileId);

                Assert.AreEqual(0, Find(root.transform, "StrategyGuideAuthoringPickerChooseButton-GUIDE_SHAPING_DEATHRATTLE").Count);
                Assert.AreEqual(0, Find(root.transform, "StrategyGuideAuthoringPickerChooseButton-GUIDE_SHAPING_BATTLECRY").Count);
                Assert.AreEqual(0, Find(root.transform, "StrategyGuideAuthoringPickerChooseButton-GUIDE_SHAPING_END_OF_TURN").Count);

                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
                var excluded = version.Snapshot.ForLanguage(false).Spells.All.FirstOrDefault(spell =>
                    !spell.InPool && !string.Equals(spell.Category, "GuideTutorial", StringComparison.OrdinalIgnoreCase));
                if (excluded != null)
                {
                    Assert.AreEqual(
                        0,
                        Find(root.transform, "StrategyGuideAuthoringPickerChooseButton-" +
                            StrategyGuideAuthoringPickerModalComponent.SafeName(excluded.CardNumber)).Count);
                }

                var scheduleCount = profile.ShapingSpellCardIds.Count;
                Click(root, "StrategyGuideAuthoringShapingSpellAddButton-" + profile.ProfileId);
                Click(root, "StrategyGuideAuthoringPickerChooseButton-GUIDE_SHAPING_DEATHRATTLE");
                var saved = repository.LoadDraft("draft-ui-test").Guide.EntryProfiles
                    .Single(item => item.ProfileId == profile.ProfileId);
                Assert.AreEqual(scheduleCount + 1, saved.ShapingSpellCardIds.Count);
                Assert.AreEqual("GUIDE_SHAPING_DEATHRATTLE", saved.ShapingSpellCardIds.Last());

                var goal = Find(root.transform, "StrategyGuideAuthoringLearningGoalInput-" + profile.ProfileId)
                    .Single()
                    .GetComponent<InputField>();
                goal.text = "学会用塑造法术推进核心成长";
                goal.onEndEdit.Invoke(goal.text);
                Assert.AreEqual(
                    "学会用塑造法术推进核心成长",
                    repository.LoadDraft("draft-ui-test").Guide.EntryProfiles
                        .Single(item => item.ProfileId == profile.ProfileId)
                        .LearningGoal);
            });
        }

        [Test]
        public void AddingControlledOfferEnablesDisclosureAndPersistsGenericSchedule()
        {
            WithEditor((root, view, guide, repository) =>
            {
                var profile = guide.EntryProfiles[0];
                var before = profile.AcquisitionPlan?.OfferSchedules?.Count ?? 0;
                Click(root, "StrategyGuideAuthoringStepButton-2");
                Click(root, "StrategyGuideAuthoringAdvancedButton-" + profile.ProfileId);
                Click(root, "StrategyGuideAuthoringOfferAddButton-" + profile.ProfileId);

                var saved = repository.LoadDraft("draft-ui-test").Guide.EntryProfiles
                    .Single(item => item.ProfileId == profile.ProfileId);
                Assert.IsTrue(saved.AcquisitionPlan.DiscloseControlledOffers);
                Assert.AreEqual(before + 1, saved.AcquisitionPlan.OfferSchedules.Count);
                Assert.AreEqual(StrategyGuideOfferSources.ShopRefresh, saved.AcquisitionPlan.OfferSchedules.Last().Source);
                Assert.AreEqual(StrategyGuideOfferPolicies.MustInclude, saved.AcquisitionPlan.OfferSchedules.Last().Policy);
            });
        }

        [Test]
        public void SuccessfulFreezeRevealsCopyShareAndProfileDeliveryActions()
        {
            WithEditor((root, view, guide, repository) =>
            {
                GUIUtility.systemCopyBuffer = string.Empty;
                Click(root, "StrategyGuideAuthoringStepButton-3");
                Click(root, "StrategyGuideAuthoringFreezeButton");

                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringFrozenDelivery").Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringCopyFrozenCodeButton").Count);
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideAuthoringPreviewFrozenShareButton").Count);
                Assert.AreEqual(
                    guide.EntryProfiles.Count,
                    root.GetComponentsInChildren<Button>(true).Count(button =>
                        button.name.StartsWith("StrategyGuideAuthoringStartFrozenButton-", StringComparison.Ordinal)));

                Click(root, "StrategyGuideAuthoringCopyFrozenCodeButton");
                StringAssert.StartsWith(StrategyGuidePortableCodeService.CodePrefix + ".", GUIUtility.systemCopyBuffer);
                Assert.AreEqual(64, GUIUtility.systemCopyBuffer.Split('.')[2].Length);

                Click(root, "StrategyGuideAuthoringPreviewFrozenShareButton");
                Assert.AreEqual(1, Find(root.transform, "StrategyGuideShareOverlay").Count);
                Click(root, "StrategyGuideShareCloseButton");
                Assert.AreEqual(0, Find(root.transform, "StrategyGuideShareOverlay").Count);
            });
        }

        [Test]
        public void FrozenProfileStartsThroughTheExistingImportedGuideCallback()
        {
            var root = new GameObject("StrategyGuideAuthoringHandoffRoot", typeof(RectTransform));
            var repositoryRoot = Path.Combine(Path.GetTempPath(), "learn-hearthstone-authoring-handoff-" + Guid.NewGuid().ToString("N"));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var repository = new FileStrategyGuideAuthoringRepository(repositoryRoot);
                var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
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
                    startImportedGuide: result => accepted = result,
                    authoringRepository: repository).Build();

                Click(root, "StrategyGuideAuthoringOpenButton");
                var guide = catalog.Guides[0];
                Click(root, "StrategyGuideAuthoringTemplateSelectButton-" + guide.GuideId);
                Click(root, "StrategyGuideAuthoringStepButton-3");
                Click(root, "StrategyGuideAuthoringFreezeButton");
                var profile = guide.EntryProfiles[1];
                Click(root, "StrategyGuideAuthoringStartFrozenButton-" + profile.ProfileId);

                Assert.NotNull(accepted);
                Assert.IsTrue(accepted.IsCompatible);
                Assert.AreEqual(profile.ProfileId, accepted.Profile.ProfileId);
                Assert.AreEqual(guide.GuideId, accepted.Guide.GuideId);
                Assert.AreEqual(accepted.Payload.Guide.RevisionId, accepted.Guide.RevisionId);
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (Directory.Exists(repositoryRoot))
                {
                    Directory.Delete(repositoryRoot, true);
                }
            }
        }

        private static void WithEditor(
            Action<GameObject, StrategyGuideAuthoringEditorView, StrategyGuideDefinition, FileStrategyGuideAuthoringRepository> assertion,
            StrategyGuideDefinition template = null,
            UnityTavernLayoutContext? layoutContext = null)
        {
            var root = new GameObject("StrategyGuideAuthoringUiRoot", typeof(RectTransform));
            var repositoryRoot = Path.Combine(Path.GetTempPath(), "learn-hearthstone-authoring-ui-" + Guid.NewGuid().ToString("N"));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = template ?? catalog.Guides[0];
                var repository = new FileStrategyGuideAuthoringRepository(repositoryRoot);
                var version = snapshot.VersionedContent.CreateResolver().Resolve(
                    GameVersionIds.Season14Preview,
                    snapshot);
                var view = new StrategyGuideAuthoringEditorView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    version,
                    guide,
                    repository,
                    () => { },
                    false,
                    layoutContext ?? UnityTavernLayoutContext.ForSize(1280f, 720f),
                    "draft-ui-test");
                view.Build();
                assertion(root, view, guide, repository);
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (Directory.Exists(repositoryRoot))
                {
                    Directory.Delete(repositoryRoot, true);
                }
            }
        }

        private static StrategyGuideDefinition Clone(StrategyGuideDefinition value)
        {
            return JsonUtility.FromJson<StrategyGuideDefinition>(JsonUtility.ToJson(value));
        }

        private static void Click(GameObject root, string name)
        {
            Find(root.transform, name).Single().GetComponent<Button>().onClick.Invoke();
        }

        private static System.Collections.Generic.List<Transform> Find(Transform root, string name)
        {
            var result = new System.Collections.Generic.List<Transform>();
            Collect(root, name, result);
            return result;
        }

        private static void Collect(
            Transform root,
            string name,
            System.Collections.Generic.ICollection<Transform> result)
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
