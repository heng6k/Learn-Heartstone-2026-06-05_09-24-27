using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StrategyGuideShareCardTests
    {
        [Test]
        public void Create_BeginnerAndHardProfilesBuildCompilerBackedOneSheetModels()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var testedProfiles = 0;

            foreach (var guide in catalog.Guides)
            {
                var profiles = guide.EntryProfiles
                    .Where(IsOneSheetProfile)
                    .ToList();
                Assert.AreEqual(2, profiles.Count, guide.GuideId);

                foreach (var profile in profiles)
                {
                    var compiled = StrategyGuideScenarioCompiler.Compile(
                        catalog,
                        guide,
                        version,
                        false,
                        profile.ProfileId);
                    var card = StrategyGuideShareCardService.Create(
                        catalog,
                        guide.GuideId,
                        profile.ProfileId,
                        version,
                        snapshot.ForLanguage(false),
                        false);
                    var expectedCode = StrategyGuidePortableCodeService.Export(
                        catalog,
                        guide.GuideId,
                        profile.ProfileId,
                        version);

                    Assert.AreEqual(guide.GuideId, card.GuideId);
                    Assert.AreEqual(profile.ProfileId, card.ProfileId);
                    Assert.AreEqual(profile.Difficulty, card.Difficulty);
                    Assert.AreEqual(profile.Title, card.DifficultyTitle);
                    Assert.AreEqual(profile.LearningGoal, card.LearningGoal);
                    Assert.AreEqual(compiled.Scenario.SavedAtRound, card.StartRound);
                    Assert.AreEqual(compiled.Scenario.Tavern.Tier, card.TavernTier);
                    Assert.AreEqual(compiled.Scenario.Tavern.Gold, card.Gold);
                    Assert.AreEqual(expectedCode, card.PublicCode);
                    Assert.AreEqual(expectedCode.Split('.')[2], card.ContentHash);
                    CollectionAssert.AreEquivalent(
                        EffectiveRecommendations(
                            guide.RecommendedLesserTrinketCardIds,
                            guide.LesserTrinketCardId),
                        card.RecommendedLesserTrinkets.Select(item => item.StableId));
                    CollectionAssert.AreEquivalent(
                        EffectiveRecommendations(
                            guide.RecommendedGreaterTrinketCardIds,
                            guide.GreaterTrinketCardId),
                        card.RecommendedGreaterTrinkets.Select(item => item.StableId));
                    Assert.AreEqual(1, card.Entries.Count);
                    Assert.AreEqual(profile.ProfileId, card.Entries[0].ProfileId);
                    CollectionAssert.AreEqual(
                        profile.KeyDecisions.Where(item => !string.IsNullOrWhiteSpace(item)).Take(3),
                        card.KeyDecisions);
                    CollectionAssert.AreEqual(
                        profile.ShapingSpellCardIds,
                        card.ShapingTurns.Select(item => item.Spell.StableId));
                    CollectionAssert.AreEqual(
                        Enumerable.Range(1, profile.ShapingSpellCardIds.Count),
                        card.ShapingTurns.Select(item => item.LocalTurn));
                    CollectionAssert.AreEqual(
                        profile.GrowthQuality.Select(item => item.Key),
                        card.GrowthTargets.Select(item => item.Key));
                    CollectionAssert.AreEqual(
                        profile.GrowthQuality.Select(item => item.Value),
                        card.GrowthTargets.Select(item => item.MinimumValue));
                    Assert.IsNotEmpty(card.CompletionCondition);
                    AssertZoneMatches(compiled.Scenario.Shop, card.StartingShop);
                    AssertZoneMatches(compiled.Scenario.PlayerBoard, card.StartingBoard);
                    AssertZoneMatches(compiled.Scenario.Hand, card.StartingHand);
                    testedProfiles += 1;
                }
            }

            Assert.AreEqual(catalog.Guides.Count * 2, testedProfiles);
        }

        [Test]
        public void Create_BeginnerAndHardProfilesHaveDistinctPortableIdentity()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);

            foreach (var guide in catalog.Guides)
            {
                var beginner = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.GuidedDiscover);
                var hard = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.OpenBuild);
                var beginnerCard = StrategyGuideShareCardService.Create(
                    catalog,
                    guide.GuideId,
                    beginner.ProfileId,
                    version,
                    snapshot.ForLanguage(false),
                    false);
                var hardCard = StrategyGuideShareCardService.Create(
                    catalog,
                    guide.GuideId,
                    hard.ProfileId,
                    version,
                    snapshot.ForLanguage(false),
                    false);

                Assert.AreNotEqual(beginnerCard.PublicCode, hardCard.PublicCode, guide.GuideId);
                Assert.AreNotEqual(beginnerCard.ContentHash, hardCard.ContentHash, guide.GuideId);
                Assert.AreEqual(beginner.ProfileId, beginnerCard.ProfileId);
                Assert.AreEqual(hard.ProfileId, hardCard.ProfileId);
            }
        }

        [Test]
        public void Create_LocalizationDoesNotChangeRequestedProfileIdentity()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var guide = catalog.Guides[0];
            var profile = guide.EntryProfiles.Single(item =>
                item.Difficulty == StrategyGuideDifficulties.GuidedDiscover);

            var chinese = StrategyGuideShareCardService.Create(
                catalog,
                guide.GuideId,
                profile.ProfileId,
                version,
                snapshot.ForLanguage(false),
                false);
            var english = StrategyGuideShareCardService.Create(
                catalog,
                guide.GuideId,
                profile.ProfileId,
                version,
                snapshot.ForLanguage(true),
                true);

            Assert.AreEqual(guide.EnglishTitle, english.Title);
            Assert.AreEqual(profile.EnglishTitle, english.DifficultyTitle);
            Assert.AreEqual(profile.EnglishLearningGoal, english.LearningGoal);
            Assert.AreEqual(profile.ProfileId, english.ProfileId);
            Assert.AreEqual(chinese.ContentHash, english.ContentHash);
            Assert.AreEqual(chinese.PublicCode, english.PublicCode);
        }

        [Test]
        public void Create_DefaultOverloadKeepsShowcaseCompatibility()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var guide = catalog.Guides[0];

            var card = StrategyGuideShareCardService.Create(
                catalog,
                guide.GuideId,
                version,
                snapshot.ForLanguage(false),
                false);

            Assert.AreEqual(catalog.GetDefaultProfile(guide.GuideId).ProfileId, card.ProfileId);
            Assert.AreEqual(StrategyGuideDifficulties.Showcase, card.Difficulty);
        }

        private static List<string> EffectiveRecommendations(
            IEnumerable<string> recommendations,
            string fallbackCardId)
        {
            var values = (recommendations ?? Enumerable.Empty<string>())
                .Where(cardId => !string.IsNullOrWhiteSpace(cardId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (values.Count == 0)
            {
                values.Add(fallbackCardId);
            }
            return values;
        }

        private static bool IsOneSheetProfile(StrategyGuideEntryProfileDefinition profile)
        {
            return profile != null &&
                (profile.Difficulty == StrategyGuideDifficulties.GuidedDiscover ||
                    profile.Difficulty == StrategyGuideDifficulties.OpenBuild);
        }

        private static void AssertZoneMatches(
            IReadOnlyList<ScenarioCardState> expected,
            IReadOnlyList<StrategyGuideShareCardAsset> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            for (var index = 0; index < expected.Count; index += 1)
            {
                var expectedCard = expected[index];
                var actualCard = actual[index];
                Assert.AreEqual(
                    string.IsNullOrWhiteSpace(expectedCard.InstanceId) ? expectedCard.CardId : expectedCard.InstanceId,
                    actualCard.StableId);
                Assert.AreEqual(expectedCard.CardKind, actualCard.CardKind);
                Assert.AreEqual(expectedCard.Name, actualCard.Name);
                Assert.AreEqual(expectedCard.ImagePath, actualCard.ImagePath);
                Assert.AreEqual(expectedCard.Golden, actualCard.Golden);
                Assert.AreEqual(expectedCard.Attack, actualCard.Attack);
                Assert.AreEqual(expectedCard.Health, actualCard.Health);
                Assert.AreEqual(expectedCard.TavernTier, actualCard.TavernTier);
                Assert.AreEqual(expectedCard.Cost, actualCard.Cost);
            }
        }
    }
}
