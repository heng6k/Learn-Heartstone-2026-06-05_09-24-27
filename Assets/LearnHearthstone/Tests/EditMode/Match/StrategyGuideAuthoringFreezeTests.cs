using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StrategyGuideAuthoringFreezeTests
    {
        [Test]
        public void FreezeIsDeterministicAndDoesNotMutateTheDraft()
        {
            var context = Context();
            foreach (var guide in context.Catalog.Guides)
            {
                var draft = Draft(guide);
                draft.Guide.RevisionId = "editor-working-copy";
                var before = JsonUtility.ToJson(draft);

                var first = StrategyGuideAuthoringFreezeService.Freeze(
                    draft,
                    context.Catalog,
                    context.Version);
                var second = StrategyGuideAuthoringFreezeService.Freeze(
                    draft,
                    context.Catalog,
                    context.Version);

                Assert.IsTrue(first.Succeeded, string.Join(" | ", first.Diagnostics));
                Assert.IsTrue(second.Succeeded, string.Join(" | ", second.Diagnostics));
                Assert.AreEqual(first.Guide.RevisionId, second.Guide.RevisionId);
                Assert.AreEqual(first.ContentHash, second.ContentHash);
                Assert.AreEqual(before, JsonUtility.ToJson(draft));
                Assert.AreNotEqual("editor-working-copy", first.Guide.RevisionId);
                StringAssert.StartsWith(guide.GuideId + "@", first.Guide.RevisionId);
            }
        }

        [Test]
        public void FreezeRejectsMissingTribesUnknownCardsDuplicatePlacementsAndUndisclosedOffers()
        {
            var context = Context();
            var baseGuide = context.Catalog.Guides[0];

            var missingTribe = Draft(baseGuide);
            missingTribe.Guide.ActiveTribes.RemoveAt(0);
            AssertRejected(missingTribe, context, "guide.active-tribe.count");

            var unknownCard = Draft(baseGuide);
            unknownCard.Guide.CoreMinionCardIds.Add("UNKNOWN_AUTHORING_CARD");
            AssertRejected(unknownCard, context, "guide.core-minion.missing:UNKNOWN_AUTHORING_CARD");

            var duplicatePlacement = Draft(baseGuide);
            var profile = duplicatePlacement.Guide.EntryProfiles[0];
            profile.Placements.Add(Clone(profile.Placements[0]));
            AssertRejected(duplicatePlacement, context, "guide.placement.duplicate");

            var undisclosed = Draft(baseGuide);
            var controlled = undisclosed.Guide.EntryProfiles.First(item =>
                item.AcquisitionPlan?.OfferSchedules?.Any(schedule =>
                    schedule.Policy == StrategyGuideOfferPolicies.MustInclude ||
                    schedule.Policy == StrategyGuideOfferPolicies.Pinned) == true);
            controlled.AcquisitionPlan.DiscloseControlledOffers = false;
            AssertRejected(undisclosed, context, "guide.acquisition.disclosure");
        }

        [Test]
        public void FreezeRejectsAuthoringIntegerOverflowBeforeRuntimeCompilation()
        {
            var context = Context();
            var draft = Draft(context.Catalog.Guides[0]);
            draft.Guide.EntryProfiles[0].StartRound = int.MaxValue;
            draft.Guide.EntryProfiles[0].MaxGold = int.MaxValue;

            var result = StrategyGuideAuthoringFreezeService.Freeze(
                draft,
                context.Catalog,
                context.Version);

            Assert.IsFalse(result.Succeeded);
            CollectionAssert.Contains(result.Diagnostics, "authoring.start-round.range");
            CollectionAssert.Contains(result.Diagnostics, "authoring.max-gold.range");
        }

        [Test]
        public void FreezeSupportsAdditionalProfileWithoutChangingTheProtocol()
        {
            var context = Context();
            var draft = Draft(context.Catalog.Guides[0]);
            var additional = Clone(draft.Guide.EntryProfiles.Single(item =>
                item.Difficulty == StrategyGuideDifficulties.GuidedDiscover));
            additional.ProfileId = "guided-extra";
            additional.Title = "额外教学入口";
            additional.EnglishTitle = "Extra lesson";
            draft.Guide.EntryProfiles.Add(additional);

            var result = StrategyGuideAuthoringFreezeService.Freeze(
                draft,
                context.Catalog,
                context.Version);

            Assert.IsTrue(result.Succeeded, string.Join(" | ", result.Diagnostics));
            Assert.AreEqual(4, result.Guide.EntryProfiles.Count);
            Assert.AreEqual("guided-extra", result.Guide.EntryProfiles[3].ProfileId);
        }

        [Test]
        public void FreezeRoundTripThroughLhsg1KeepsTheSameRevision()
        {
            var context = Context();
            var first = StrategyGuideAuthoringFreezeService.Freeze(
                Draft(context.Catalog.Guides[0]),
                context.Catalog,
                context.Version);
            Assert.IsTrue(first.Succeeded, string.Join(" | ", first.Diagnostics));
            var frozenCatalog = new StrategyGuideCatalog(new StrategyGuideCatalogDefinition
            {
                SchemaVersion = 2,
                CatalogRevisionId = "authoring-round-trip",
                Guides = new List<StrategyGuideDefinition> { first.Guide },
                Opponents = context.Catalog.Opponents.ToList()
            });
            var code = StrategyGuidePortableCodeService.ExportGuide(
                frozenCatalog,
                first.Guide.GuideId,
                context.Version);
            var imported = StrategyGuidePortableCodeService.Import(code, context.Version);
            Assert.IsTrue(imported.IsCompatible);

            var second = StrategyGuideAuthoringFreezeService.Freeze(
                Draft(imported.Guide),
                imported.Catalog,
                context.Version);

            Assert.IsTrue(second.Succeeded, string.Join(" | ", second.Diagnostics));
            Assert.AreEqual(first.Guide.RevisionId, second.Guide.RevisionId);
            Assert.AreEqual(first.ContentHash, second.ContentHash);
        }

        [Test]
        public void CatalogRevisionsFreezeAndDeliverAllEightShowcaseGoldenSamples()
        {
            var context = Context();
            var directory = Path.Combine(
                Path.GetTempPath(),
                "learn-hearthstone-showcase-golden-" + Guid.NewGuid().ToString("N"));
            try
            {
                var repository = new FileStrategyGuideAuthoringRepository(directory);
                var frozenGuides = context.Catalog.Guides
                    .Select(guide => new
                    {
                        Source = guide,
                        Frozen = StrategyGuideAuthoringFreezeService.Freeze(
                            Draft(guide),
                            context.Catalog,
                            context.Version)
                    })
                    .ToList();

                Assert.AreEqual(8, frozenGuides.Count);
                var revisionMismatches = frozenGuides
                    .Where(item => !string.Equals(
                        item.Source.RevisionId,
                        item.Frozen.Guide?.RevisionId,
                        StringComparison.Ordinal))
                    .Select(item => item.Source.GuideId + ": " + item.Source.RevisionId + " -> " +
                                    (item.Frozen.Guide?.RevisionId ?? "<freeze failed>") +
                                    " [" + (item.Frozen.ContentHash ?? "no hash") + "]")
                    .ToList();
                Assert.AreEqual(0, revisionMismatches.Count, string.Join(" | ", revisionMismatches));
                foreach (var item in frozenGuides)
                {
                    var showcase = context.Catalog.GetDefaultProfile(item.Source.GuideId);
                    Assert.IsTrue(item.Frozen.Succeeded,
                        item.Source.GuideId + ": " + string.Join(" | ", item.Frozen.Diagnostics));
                    Assert.AreEqual(5, item.Source.ActiveTribes.Count, item.Source.GuideId);
                    Assert.IsTrue(item.Source.RequiredTribes.All(tribe => item.Source.ActiveTribes.Contains(tribe)),
                        item.Source.GuideId);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(item.Source.HeroCardId), item.Source.GuideId);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(item.Source.LesserTrinketCardId), item.Source.GuideId);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(item.Source.GreaterTrinketCardId), item.Source.GuideId);
                    Assert.AreEqual(7, item.Source.FinalComposition.Count, item.Source.GuideId);
                    Assert.IsTrue(item.Source.FinalComposition.Any(card => card.Golden), item.Source.GuideId);
                    Assert.IsTrue(item.Source.FinalComposition.Any(card => !card.Golden), item.Source.GuideId);
                    Assert.AreEqual(10, showcase.StartRound, item.Source.GuideId);
                    Assert.IsTrue(showcase.Placements.Any(card => card.Zone == StrategyGuideZones.Board), item.Source.GuideId);
                    Assert.IsTrue(showcase.Placements.Any(card => card.Zone == StrategyGuideZones.Hand), item.Source.GuideId);
                    Assert.IsTrue(showcase.Placements.Any(card => card.Zone == StrategyGuideZones.Shop), item.Source.GuideId);
                    Assert.IsNotEmpty(showcase.DarkGiftAttachments, item.Source.GuideId);
                    Assert.IsNotEmpty(showcase.RequiredActions, item.Source.GuideId);
                    Assert.AreEqual("SimpleBalanced", showcase.Opponent.RequiredTag, item.Source.GuideId);
                    Assert.AreEqual(10, showcase.Opponent.StrengthRound, item.Source.GuideId);
                    Assert.IsTrue(showcase.Victory.RequireFinalComposition, item.Source.GuideId);
                    Assert.IsTrue(showcase.Victory.RequireCombatWin, item.Source.GuideId);
                    Assert.AreEqual(1, showcase.Undo.UsesPerRun, item.Source.GuideId);
                }

                foreach (var item in frozenGuides)
                {
                    repository.SaveFrozen(item.Frozen);
                    Assert.IsTrue(repository.ContainsFrozen(item.Frozen.ContentHash), item.Source.GuideId);
                    var stored = repository.LoadFrozen(item.Frozen.ContentHash);
                    var frozenCatalog = StrategyGuideAuthoringFreezeService.CreateFrozenCatalog(stored, context.Catalog);
                    var code = StrategyGuidePortableCodeService.ExportGuide(
                        frozenCatalog,
                        stored.Guide.GuideId,
                        context.Version);
                    var imported = StrategyGuidePortableCodeService.Import(code, context.Version);
                    Assert.IsTrue(imported.IsCompatible,
                        item.Source.GuideId + ": " + string.Join(" | ", imported.Diagnostics.Select(diagnostic => diagnostic.Message)));
                    Assert.AreEqual(stored.Guide.RevisionId, imported.Guide.RevisionId, item.Source.GuideId);

                    var direct = StrategyGuideSession.Start(
                        frozenCatalog,
                        stored.Guide.GuideId,
                        context.Version,
                        profileId: "showcase");
                    var fromCode = StrategyGuideSession.Start(
                        imported.Catalog,
                        imported.Guide.GuideId,
                        context.Version,
                        profileId: "showcase");
                    Assert.AreEqual(
                        JsonUtility.ToJson(TestScenarioMapper.Capture(direct.MatchService.State, "showcase-initial")),
                        JsonUtility.ToJson(TestScenarioMapper.Capture(fromCode.MatchService.State, "showcase-initial")),
                        item.Source.GuideId);
                }
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void StoredFrozenRevisionImportsAndStartsEveryProfileWithTheSameInitialScenario()
        {
            var context = Context();
            var directory = Path.Combine(
                Path.GetTempPath(),
                "learn-hearthstone-authoring-e2e-" + Guid.NewGuid().ToString("N"));
            try
            {
                var frozen = StrategyGuideAuthoringFreezeService.Freeze(
                    Draft(context.Catalog.Guides[0]),
                    context.Catalog,
                    context.Version);
                Assert.IsTrue(frozen.Succeeded, string.Join(" | ", frozen.Diagnostics));

                var repository = new FileStrategyGuideAuthoringRepository(directory);
                repository.SaveFrozen(frozen);
                var stored = repository.LoadFrozen(frozen.ContentHash);
                var frozenCatalog = StrategyGuideAuthoringFreezeService.CreateFrozenCatalog(stored, context.Catalog);
                var code = StrategyGuidePortableCodeService.ExportGuide(
                    frozenCatalog,
                    stored.Guide.GuideId,
                    context.Version);
                var imported = StrategyGuidePortableCodeService.Import(code, context.Version);

                Assert.IsTrue(imported.IsCompatible, string.Join(" | ", imported.Diagnostics.Select(item => item.Message)));
                Assert.AreEqual(frozen.ContentHash, stored.ContentHash);
                Assert.AreEqual(stored.Guide.RevisionId, imported.Guide.RevisionId);
                foreach (var profile in stored.Guide.EntryProfiles)
                {
                    var direct = StrategyGuideSession.Start(
                        frozenCatalog,
                        stored.Guide.GuideId,
                        context.Version,
                        profileId: profile.ProfileId);
                    var fromCode = StrategyGuideSession.Start(
                        imported.Catalog,
                        imported.Guide.GuideId,
                        context.Version,
                        profileId: profile.ProfileId);

                    Assert.AreEqual(profile.ProfileId, fromCode.Profile.ProfileId);
                    Assert.AreEqual(
                        JsonUtility.ToJson(TestScenarioMapper.Capture(direct.MatchService.State, "initial")),
                        JsonUtility.ToJson(TestScenarioMapper.Capture(fromCode.MatchService.State, "initial")),
                        profile.ProfileId);
                }
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        private static void AssertRejected(
            StrategyGuideAuthoringDraft draft,
            TestContextData context,
            string diagnostic)
        {
            var result = StrategyGuideAuthoringFreezeService.Freeze(
                draft,
                context.Catalog,
                context.Version);
            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Diagnostics.Any(item => item.Contains(diagnostic)),
                string.Join(" | ", result.Diagnostics));
        }

        private static StrategyGuideAuthoringDraft Draft(StrategyGuideDefinition guide)
        {
            return new StrategyGuideAuthoringDraft
            {
                DraftId = "draft-" + guide.GuideId.ToLowerInvariant(),
                Guide = Clone(guide)
            };
        }

        private static T Clone<T>(T value)
        {
            return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        }

        private static TestContextData Context()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            return new TestContextData
            {
                Catalog = StrategyGuideCatalogLoader.LoadFromResources(),
                Version = snapshot.VersionedContent.CreateResolver().Resolve(
                    GameVersionIds.Season14Preview,
                    snapshot)
            };
        }

        private sealed class TestContextData
        {
            public StrategyGuideCatalog Catalog;
            public ResolvedGameVersion Version;
        }
    }
}
