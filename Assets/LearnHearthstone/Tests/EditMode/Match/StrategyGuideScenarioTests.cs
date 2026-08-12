using System;
using System.Collections.Generic;
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
    public sealed class StrategyGuideScenarioTests
    {
        [Test]
        public void Catalog_LoadsEightVersionedGuidesAndGlobalOpponents()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();

            Assert.AreEqual(2, catalog.Definition.SchemaVersion);
            Assert.AreEqual(8, catalog.Guides.Count);
            Assert.AreEqual(3, catalog.Opponents.Count);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "DeathrattleSummonChain",
                    "SpellEconomyGrowth",
                    "StatGrowth",
                    "DragonBattlecryScaling",
                    "MurlocMagicfin",
                    "PirateBountyApm",
                    "QuilboarCombatScaling",
                    "UndeadOverflow"
                },
                catalog.Guides.Select(item => item.Archetype));
            Assert.IsTrue(catalog.Guides.All(item => item.GameVersionId == GameVersionIds.Season14Preview));
            Assert.IsTrue(catalog.Guides.All(item => item.FinalComposition.Count == 7));
            Assert.IsTrue(catalog.Guides.All(item => item.ActiveTribes.Count == 5));
            Assert.IsTrue(catalog.Guides.All(item => item.EntryProfiles.Count == 3));
            Assert.IsTrue(catalog.Guides.All(item => Showcase(item).Undo.UsesPerRun == 1));
            Assert.IsTrue(catalog.Guides.All(item => Guided(item).Undo.UsesPerRun == 0));
            Assert.IsTrue(catalog.Guides.All(item => item.EntryProfiles.Count(profile =>
                profile.Difficulty == StrategyGuideDifficulties.Showcase) == 1));
            Assert.IsTrue(catalog.Guides.All(item => item.EntryProfiles.Count(profile =>
                profile.Difficulty == StrategyGuideDifficulties.GuidedDiscover) == 1));
            Assert.IsTrue(catalog.Guides.All(item => item.EntryProfiles.Count(profile =>
                profile.Difficulty == StrategyGuideDifficulties.OpenBuild) == 1));
            CollectionAssert.AreEquivalent(
                new[]
                {
                    StrategyGuideOfferSources.TripleRewardDiscover,
                    StrategyGuideOfferSources.TavernSpellDiscover,
                    StrategyGuideOfferSources.ShopRefresh
                },
                catalog.Guides
                    .Select(item => Guided(item).AcquisitionPlan.OfferSchedules.Single().Source)
                    .Distinct());
        }

        [Test]
        public void Catalog_RejectsDuplicateProfileIdsWithinOneGuide()
        {
            var loaded = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = loaded.Guides[0];
            guide.EntryProfiles.Add(guide.EntryProfiles[0]);

            var exception = Assert.Throws<ArgumentException>(() => new StrategyGuideCatalog(loaded.Definition));

            StringAssert.Contains("entry profile id", exception.Message);
        }

        [Test]
        public void Validator_RejectsUnknownDifficultyAndMissingDefaultShowcase()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            Showcase(guide).Difficulty = "UnknownDifficulty";

            var result = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());

            AssertContains(result, "guide.profile.default-showcase");
            AssertContains(result, "guide.profile.difficulty:showcase");
        }

        [Test]
        public void Compiler_SelectsEntryProfileByDataIdentityAndRejectsUnknownProfile()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var guided = JsonUtility.FromJson<StrategyGuideEntryProfileDefinition>(
                JsonUtility.ToJson(Showcase(guide)));
            guided.ProfileId = "guided-probe";
            guided.Difficulty = StrategyGuideDifficulties.GuidedDiscover;
            guided.Title = "初级模式探针";
            guided.EnglishTitle = "Guided Probe";
            guided.StartRound = 8;
            guided.Seed += 1;
            guided.Undo.UsesPerRun = 0;
            guided.AcquisitionPlan = new StrategyGuideAcquisitionPlanDefinition
            {
                DiscloseControlledOffers = true,
                OfferSchedules = new System.Collections.Generic.List<StrategyGuideOfferScheduleDefinition>
                {
                    new StrategyGuideOfferScheduleDefinition
                    {
                        ScheduleId = "guided-probe-triple",
                        Source = StrategyGuideOfferSources.TripleRewardDiscover,
                        TriggerOccurrence = 1,
                        Policy = StrategyGuideOfferPolicies.MustInclude,
                        CardKind = StrategyGuideCardKinds.Minion,
                        OptionCount = 3,
                        TargetCardIds = new System.Collections.Generic.List<string> { guide.CoreMinionCardIds[2] },
                        Label = "引导发现"
                    }
                }
            };
            guide.EntryProfiles.Add(guided);

            var compiled = StrategyGuideScenarioCompiler.Compile(
                catalog,
                guide,
                ResolveSeason14(),
                profileId: guided.ProfileId);

            Assert.AreEqual(guided.ProfileId, compiled.Profile.ProfileId);
            Assert.AreEqual(8, compiled.Scenario.SavedAtRound);
            Assert.Throws<InvalidOperationException>(() => StrategyGuideScenarioCompiler.Compile(
                catalog,
                guide,
                ResolveSeason14(),
                profileId: "missing-profile"));
        }

        [Test]
        public void Validator_AcceptsAllFrozenGuidesAgainstResolved362Content()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var version = ResolveSeason14();

            foreach (var guide in catalog.Guides)
            {
                var result = StrategyGuideValidator.Validate(catalog, guide, version);

                Assert.IsTrue(result.IsValid, guide.GuideId + ": " + string.Join(" | ", result.Errors));
            }
        }

        [Test]
        public void Validator_RejectsWrongVersionMissingRequiredTribeUnknownCardAndUnknownGrowth()
        {
            var version = ResolveSeason14();

            var wrongVersionCatalog = StrategyGuideCatalogLoader.LoadFromResources();
            var wrongVersion = wrongVersionCatalog.Guides[0];
            wrongVersion.GameVersionId = GameVersionIds.LegacyCompositeSandbox;
            AssertContains(
                StrategyGuideValidator.Validate(wrongVersionCatalog, wrongVersion, version),
                "guide.version.mismatch");

            var missingTribeCatalog = StrategyGuideCatalogLoader.LoadFromResources();
            var missingTribe = missingTribeCatalog.Guides[0];
            missingTribe.ActiveTribes.Remove("Beast");
            missingTribe.ActiveTribes.Add("Dragon");
            AssertContains(
                StrategyGuideValidator.Validate(missingTribeCatalog, missingTribe, version),
                "guide.required-tribe.missing");

            var unknownCardCatalog = StrategyGuideCatalogLoader.LoadFromResources();
            var unknownCard = unknownCardCatalog.Guides[0];
            Showcase(unknownCard).Placements[0].CardId = "UNKNOWN_GUIDE_CARD";
            AssertContains(
                StrategyGuideValidator.Validate(unknownCardCatalog, unknownCard, version),
                "guide.card.minion-missing:UNKNOWN_GUIDE_CARD");

            var unknownGrowthCatalog = StrategyGuideCatalogLoader.LoadFromResources();
            var unknownGrowth = unknownGrowthCatalog.Guides[0];
            Showcase(unknownGrowth).GrowthQuality.Add(new StrategyGuideGrowthValue
            {
                Key = "unknown.growth",
                Value = 1
            });
            AssertContains(
                StrategyGuideValidator.Validate(unknownGrowthCatalog, unknownGrowth, version),
                "guide.growth.unknown:unknown.growth");
        }

        [Test]
        public void Validator_RejectsCapacityOverflowAndUnmarkedGeneratedShopInjection()
        {
            var version = ResolveSeason14();
            var overflowCatalog = StrategyGuideCatalogLoader.LoadFromResources();
            var overflow = overflowCatalog.Guides[0];
            Showcase(overflow).Placements.Add(new StrategyGuideCardDefinition
            {
                PlacementId = "overflow-board-a",
                Zone = StrategyGuideZones.Board,
                CardKind = StrategyGuideCardKinds.Minion,
                CardId = "BG36_200",
                Provenance = StrategyGuideProvenance.NormalPool
            });
            Showcase(overflow).Placements.Add(new StrategyGuideCardDefinition
            {
                PlacementId = "overflow-board-b",
                Zone = StrategyGuideZones.Board,
                CardKind = StrategyGuideCardKinds.Minion,
                CardId = "BG36_200",
                Provenance = StrategyGuideProvenance.NormalPool
            });
            Showcase(overflow).Placements.Add(new StrategyGuideCardDefinition
            {
                PlacementId = "overflow-board-c",
                Zone = StrategyGuideZones.Board,
                CardKind = StrategyGuideCardKinds.Minion,
                CardId = "BG36_200",
                Provenance = StrategyGuideProvenance.NormalPool
            });
            Showcase(overflow).Placements.Add(new StrategyGuideCardDefinition
            {
                PlacementId = "overflow-board-d",
                Zone = StrategyGuideZones.Board,
                CardKind = StrategyGuideCardKinds.Minion,
                CardId = "BG36_200",
                Provenance = StrategyGuideProvenance.NormalPool
            });

            AssertContains(
                StrategyGuideValidator.Validate(overflowCatalog, overflow, version),
                "guide.board.capacity");

            var injectionCatalog = StrategyGuideCatalogLoader.LoadFromResources();
            var injection = injectionCatalog.GetGuide("GUIDE-S14-DEMON-TAVERN-CONSUME");
            Showcase(injection).Placements.Single(item => item.PlacementId == "demon-methodical-madness").Provenance =
                StrategyGuideProvenance.Generated;

            AssertContains(
                StrategyGuideValidator.Validate(injectionCatalog, injection, version),
                "guide.card.shop-injection-unmarked:132903");
        }

        [Test]
        public void Compiler_CanonicalizesAllGuidesIntoDeterministicScenarioV3()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var version = ResolveSeason14();

            foreach (var guide in catalog.Guides)
            {
                var first = StrategyGuideScenarioCompiler.Compile(catalog, guide, version);
                var second = StrategyGuideScenarioCompiler.Compile(catalog, guide, version);
                var scenario = first.Scenario;

                Assert.AreEqual(TestScenarioMigration.CurrentSchemaVersion, scenario.SchemaVersion);
                Assert.AreEqual(TestScenarioMigration.CurrentVersion, scenario.Version);
                Assert.IsFalse(scenario.IsStateTemplate);
                Assert.AreEqual(GameVersionIds.Season14Preview, scenario.GameVersionId);
                Assert.AreEqual(version.ContentSnapshotId, scenario.ContentSnapshotId);
                Assert.AreEqual(version.ContentFingerprint, scenario.ContentFingerprint);
                Assert.AreEqual(Showcase(guide).StartRound, scenario.SavedAtRound);
                Assert.AreEqual(5, scenario.ResolvedCardPool.ActiveTribes.Count);
                Assert.AreEqual(7, scenario.OpponentBoard.Count);
                Assert.AreEqual(first.Opponent.OpponentId, second.Opponent.OpponentId);
                CollectionAssert.AreEqual(
                    first.Scenario.OpponentBoard.Select(item => item.CardId),
                    second.Scenario.OpponentBoard.Select(item => item.CardId));
                Assert.IsTrue(scenario.PlayerBoard.Concat(scenario.Hand).Concat(scenario.Shop)
                    .All(item => !string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(item.ImagePath)));
                Assert.AreEqual(guide.LesserTrinketCardId, scenario.PlayerAdvancedMechanics.State.Trinkets.LesserTrinketId);
                Assert.AreEqual(guide.GreaterTrinketCardId, scenario.PlayerAdvancedMechanics.State.Trinkets.GreaterTrinketId);
                Assert.AreEqual(Showcase(guide).DarkGiftAttachments.Count, scenario.PlayerDarkGiftState.AcquiredGiftInstances.Count);
            }
        }

        [Test]
        public void Compiler_AllowsSameGiftOnPlainAndGoldenCopiesWithoutDeduplicatingThem()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.GetGuide("GUIDE-S14-BEAST-LOBSTER-RALLY");
            Showcase(guide).Placements.Add(new StrategyGuideCardDefinition
            {
                PlacementId = "beast-lobster-golden-copy",
                Zone = StrategyGuideZones.Hand,
                CardKind = StrategyGuideCardKinds.Minion,
                CardId = "BG36_202",
                Golden = true,
                Provenance = StrategyGuideProvenance.NormalPool
            });
            Showcase(guide).DarkGiftAttachments.Add(new StrategyGuideDarkGiftAttachment
            {
                AttachmentId = "beast-jaws-golden-copy-v1",
                TargetPlacementId = "beast-lobster-golden-copy",
                GiftResearchKey = "DG-R03",
                AcquiredRound = 3,
                Source = "strategy-guide-golden-copy"
            });

            var compiled = StrategyGuideScenarioCompiler.Compile(catalog, guide, ResolveSeason14());

            Assert.AreEqual(2, compiled.Scenario.PlayerDarkGiftState.AcquiredGiftInstances.Count);
            Assert.AreEqual(2, compiled.Scenario.PlayerDarkGiftState.AcquiredGiftInstances
                .Select(item => item.InstanceId)
                .Distinct(StringComparer.Ordinal)
                .Count());
            Assert.AreEqual(1, compiled.Scenario.Hand.Count(item => item.CardId == "BG36_202" && item.Golden));
        }

        [Test]
        public void CompiledScenario_RoundTripsThroughExistingMapperAndExactVersionLock()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var version = ResolveSeason14();
            var compiled = StrategyGuideScenarioCompiler.Compile(catalog, catalog.Guides[1], version);
            var target = MatchService.CreateWithResolvedVersion(
                version,
                1,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    ActiveTribes = compiled.Scenario.ResolvedCardPool.ActiveTribes,
                    SelectedHeroCardId = compiled.Guide.HeroCardId,
                    AdvancedMechanicMode = AdvancedMechanicMode.Trinkets,
                    EnableQuests = false,
                    EnableTrinkets = true,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                });

            var result = TestScenarioMapper.TryApplyTo(target.State, compiled.Scenario);

            Assert.AreEqual(TestScenarioRestoreStatus.Applied, result.Status, result.Message);
            Assert.AreEqual(compiled.Scenario.SavedAtRound, target.State.Round);
            Assert.AreEqual(compiled.Scenario.PlayerBoard.Count, target.State.Player.Board.Count);
            Assert.AreEqual(compiled.Scenario.Hand.Count, target.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(compiled.Scenario.Shop.Count, target.State.Player.Tavern.Shop.Count);
            Assert.AreEqual(compiled.Scenario.OpponentBoard.Count, target.State.Opponent.Board.Count);
        }

        private static ResolvedGameVersion ResolveSeason14()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            return snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
        }

        private static StrategyGuideEntryProfileDefinition Showcase(StrategyGuideDefinition guide)
        {
            return guide.EntryProfiles.Single(profile =>
                profile.Difficulty == StrategyGuideDifficulties.Showcase);
        }

        private static StrategyGuideEntryProfileDefinition Guided(StrategyGuideDefinition guide)
        {
            return guide.EntryProfiles.Single(profile =>
                profile.Difficulty == StrategyGuideDifficulties.GuidedDiscover);
        }

        private static void AssertContains(StrategyGuideValidationResult result, string error)
        {
            Assert.IsFalse(result.IsValid);
            CollectionAssert.Contains(result.Errors, error, string.Join(" | ", result.Errors));
        }
    }
}
