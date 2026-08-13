using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Persistence;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class GameVersionCatalogTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-game-version-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void LegacyCardPoolFile_LoadsWithoutLosingFields()
        {
            File.Copy(FixturePath("CardPoolVersionsV0.json"), Path.Combine(directory, "card-pool-versions.json"));

            var store = new JsonCardPoolVersionRepository(directory, "card-pool-versions.json").Load();
            var profile = store.Versions.Single();

            Assert.AreEqual(CardPoolVersionStore.CurrentSchemaVersion, store.SchemaVersion);
            Assert.AreEqual("legacy-user-preset", store.SelectedPresetId);
            Assert.AreEqual("旧卡池", profile.Name);
            Assert.AreEqual(GameVersionIds.LegacyCompositeSandbox, profile.BaseGameVersionId);
            Assert.AreEqual(CardPoolPresetValidationState.Valid, profile.ValidationState);
            CollectionAssert.AreEqual(new[] { "BG_TEST_MINION" }, profile.EnabledMinionCardIds);
            CollectionAssert.AreEqual(new[] { "100001" }, profile.EnabledTavernSpellCardNumbers);
            CollectionAssert.AreEqual(new[] { "BG_TEST_QUEST" }, profile.EnabledQuestCardIds);
            CollectionAssert.AreEqual(new[] { "BG_TEST_REWARD" }, profile.EnabledQuestRewardCardIds);
            CollectionAssert.AreEqual(new[] { "BG_TEST_LESSER" }, profile.EnabledLesserTrinketCardIds);
            CollectionAssert.AreEqual(new[] { "BG_TEST_GREATER" }, profile.EnabledGreaterTrinketCardIds);
            CollectionAssert.AreEqual(new[] { "BG_TEST_ANOMALY" }, profile.EnabledAnomalyCardIds);
        }

        [Test]
        public void CardPoolPreset_SaveThenLoad_RoundTripsNewSemantics()
        {
            File.Copy(FixturePath("CardPoolVersionsV0.json"), Path.Combine(directory, "card-pool-versions.json"));
            var repository = new JsonCardPoolVersionRepository(directory, "card-pool-versions.json");
            var store = repository.Load();
            var preset = CardPoolPresetAdapter.FromLegacy(store.Versions.Single());
            preset.BaseGameVersionId = GameVersionIds.Season14Preview;
            preset.CreatedAgainstContentFingerprint = "sha256:test-content";
            preset.ValidationState = CardPoolPresetValidationState.HasIncompatibleEntries;
            preset.IncompatibleEntityIds.Add("BG_REMOVED_CARD");

            repository.Save(store);
            var roundTrip = repository.Load();
            var loaded = CardPoolPresetAdapter.FromLegacy(roundTrip.Versions.Single());

            Assert.AreEqual(CardPoolVersionStore.CurrentSchemaVersion, roundTrip.SchemaVersion);
            Assert.AreEqual("legacy-user-preset", roundTrip.SelectedPresetId);
            Assert.AreEqual(GameVersionIds.Season14Preview, loaded.BaseGameVersionId);
            Assert.AreEqual("sha256:test-content", loaded.CreatedAgainstContentFingerprint);
            Assert.AreEqual(CardPoolPresetValidationState.HasIncompatibleEntries, loaded.ValidationState);
            CollectionAssert.AreEqual(new[] { "BG_REMOVED_CARD" }, loaded.IncompatibleEntityIds);
            CollectionAssert.AreEqual(new[] { "BG_TEST_MINION" }, loaded.EnabledMinionCardIds);
        }

        [Test]
        public void PreviewVersion_IsNotDefault_AndCatalogSummariesAreReadOnly()
        {
            var preview = new GameVersionDefinition(
                GameVersionIds.Season14Preview,
                "36.2 预览",
                new DateTime(2026, 8, 4, 17, 0, 0, DateTimeKind.Utc),
                GameVersionOfficialStatus.Announced,
                GameVersionImplementationStatus.Partial,
                "ruleset-36.2-preview",
                "content-36.2-preview",
                "第 14 赛季预览内容");
            var catalog = new GameVersionCatalog(new[] { preview });

            Assert.IsFalse(preview.IsDefaultCandidate);
            Assert.AreEqual(1, catalog.Versions.Count);
            Assert.AreSame(preview, catalog.Versions[0]);
            Assert.AreEqual(GameVersionImplementationStatus.Partial, catalog.Summaries[0].ImplementationStatus);
            Assert.AreEqual("第 14 赛季预览内容", catalog.Summaries[0].ChangeSummary);
            Assert.Throws<NotSupportedException>(() => ((IList<GameVersionDefinition>)catalog.Versions).Add(preview));
            Assert.Throws<NotSupportedException>(() => ((IList<GameVersionSummaryViewModel>)catalog.Summaries).Clear());
        }

        [Test]
        public void BuiltInVersions_RegisterLegacyAndPreview_AndDefaultToLatestVerified()
        {
            var catalog = GameVersionCatalog.CreateBuiltIn();

            CollectionAssert.AreEquivalent(
                new[] { GameVersionIds.LegacyCompositeSandbox, GameVersionIds.Season14Preview },
                catalog.Versions.Select(version => version.Id));
            Assert.AreEqual(GameVersionIds.LegacyCompositeSandbox, catalog.Default.Id);
            Assert.AreEqual(GameVersionImplementationStatus.Verified, catalog.Default.ImplementationStatus);
            var season14 = catalog.Get(GameVersionIds.Season14Preview);
            Assert.AreEqual("36.2", season14.DisplayName);
            Assert.AreEqual(GameVersionOfficialStatus.Released, season14.OfficialStatus);
            Assert.AreEqual(GameVersionImplementationStatus.Partial, season14.ImplementationStatus);
            Assert.IsFalse(season14.IsDefaultCandidate);
        }

        [Test]
        public void Resolve_SameVersionAndSnapshot_ReturnsStableFingerprint()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolver = GameVersionResolver.CreateBuiltIn();

            var first = resolver.Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);
            var second = resolver.Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);

            Assert.AreEqual(first.ContentFingerprint, second.ContentFingerprint);
            Assert.AreEqual(first.ContentSnapshotId, second.ContentSnapshotId);
            Assert.AreSame(first.GameVersion, resolver.Versions.Get(GameVersionIds.LegacyCompositeSandbox));
        }

        [Test]
        public void Resolve_HistoricalMembershipAndEffectRevision_OverrideCurrentGlobalFlags()
        {
            var minion = new MinionDefinition
            {
                Id = "historical-minion",
                CardId = "BG_HISTORICAL_MINION",
                Name = "Historical Minion",
                TavernTier = 2,
                BaseAttack = 2,
                BaseHealth = 2,
                InPool = false,
                RecruitActions = new List<RecruitActionDefinition>
                {
                    new RecruitActionDefinition
                    {
                        ActionId = "activate:historical",
                        ResolverId = "historical-resolver",
                        CostSpec = new RecruitActionCostSpec { Gold = 2 },
                        TargetSpec = RecruitActionTargetSpec.TavernMinion,
                        UsesPerTurn = 1,
                        AllowedPhase = MatchPhase.Tavern
                    }
                }
            };
            var embedded = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var snapshot = new GameCatalogSnapshot(
                new ContentSnapshotInfo("historical-content", "0.1.0-alpha", ContentSnapshotSource.Embedded, string.Empty, DateTime.UtcNow),
                ReplaceMinions(embedded.Chinese, minion),
                ReplaceMinions(embedded.English, minion));
            var version = new GameVersionDefinition(
                "historical-version",
                "Historical Version",
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                GameVersionOfficialStatus.Archived,
                GameVersionImplementationStatus.Verified,
                "historical-ruleset",
                "historical-content-set",
                string.Empty);
            var ruleset = new RulesetDefinition("historical-ruleset", 1);
            var oldRevision = new EntityRevisionDefinition(
                EntityKind.Minion,
                minion.CardId,
                "BG_HISTORICAL_MINION@old",
                "effect-old",
                version.Id);
            var newRevision = new EntityRevisionDefinition(
                EntityKind.Minion,
                minion.CardId,
                "BG_HISTORICAL_MINION@new",
                "effect-new",
                GameVersionIds.Season14Preview);
            var contentSet = new ContentSetDefinition(
                version.ContentSetId,
                minionRevisionIds: new[] { oldRevision.RevisionId },
                poolMembership: new[] { new PoolMembershipEntry(EntityKind.Minion, minion.CardId) });
            var resolver = new GameVersionResolver(
                new GameVersionCatalog(new[] { version }),
                new[] { ruleset },
                new[] { contentSet },
                new[] { oldRevision, newRevision });

            var resolved = resolver.Resolve(version.Id, snapshot);
            var resolvedMinion = resolved.Snapshot.Chinese.Minions.GetByCardId(minion.CardId);

            Assert.IsTrue(resolvedMinion.InPool);
            Assert.AreEqual(oldRevision.RevisionId, resolvedMinion.RevisionId);
            Assert.AreEqual("effect-old", resolvedMinion.EffectRevision);
            Assert.AreEqual("activate:historical", resolvedMinion.RecruitActions.Single().ActionId);
            Assert.AreEqual(2, resolvedMinion.RecruitActions.Single().CostSpec.Gold);
            Assert.AreNotSame(minion.RecruitActions.Single(), resolvedMinion.RecruitActions.Single());
            Assert.IsTrue(new CardPoolAvailability(null, resolved.ContentSet).AllowsMinion(resolvedMinion));
        }

        [Test]
        public void Resolve_TrinketMembershipAtomicallyEnablesDisablesAndAppliesRevision()
        {
            var active = new TrinketDefinition
            {
                Id = "active-trinket",
                CardId = "BG_ACTIVE_TRINKET",
                Name = "Active Trinket",
                Cost = 5,
                Text = "Old text",
                SlotKind = TrinketSlotKind.Lesser,
                ImplementationStatus = TrinketImplementationStatus.Implemented,
                OfferPoolStatus = TrinketOfferPoolStatus.Disabled
            };
            var removed = new TrinketDefinition
            {
                Id = "removed-trinket",
                CardId = "BG_REMOVED_TRINKET",
                Name = "Removed Trinket",
                SlotKind = TrinketSlotKind.Lesser,
                ImplementationStatus = TrinketImplementationStatus.Implemented,
                OfferPoolStatus = TrinketOfferPoolStatus.Offerable
            };
            var planned = new TrinketDefinition
            {
                Id = "planned-trinket",
                CardId = "BG_PLANNED_TRINKET",
                Name = "Planned Trinket",
                SlotKind = TrinketSlotKind.Greater,
                ImplementationStatus = TrinketImplementationStatus.Planned,
                OfferPoolStatus = TrinketOfferPoolStatus.Disabled
            };
            var embedded = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var snapshot = new GameCatalogSnapshot(
                new ContentSnapshotInfo("trinket-content", "0.1.0-alpha", ContentSnapshotSource.Embedded, string.Empty, DateTime.UtcNow),
                ReplaceTrinkets(embedded.Chinese, active, removed, planned),
                ReplaceTrinkets(embedded.English, active, removed, planned));
            var version = new GameVersionDefinition(
                "trinket-version",
                "Trinket Version",
                new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
                GameVersionOfficialStatus.Announced,
                GameVersionImplementationStatus.Partial,
                "trinket-ruleset",
                "trinket-content-set",
                string.Empty);
            var revision = new EntityRevisionDefinition(
                EntityKind.Trinket,
                active.CardId,
                "BG_ACTIVE_TRINKET@new",
                "active-effect@new",
                version.Id,
                stats: "cost:2",
                text: "New text",
                effectIds: new[] { "active_effect" },
                englishText: "New text");
            var contentSet = new ContentSetDefinition(
                version.ContentSetId,
                trinketRevisionIds: new[] { revision.RevisionId },
                poolMembership: new[]
                {
                    new PoolMembershipEntry(EntityKind.Trinket, active.CardId),
                    new PoolMembershipEntry(EntityKind.Trinket, planned.CardId)
                });
            var resolver = new GameVersionResolver(
                new GameVersionCatalog(new[] { version }),
                new[] { new RulesetDefinition(version.RulesetId, 1) },
                new[] { contentSet },
                new[] { revision });

            var resolved = resolver.Resolve(version.Id, snapshot).Snapshot.English.Trinkets;

            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, resolved.GetByCardId(active.CardId).OfferPoolStatus);
            Assert.AreEqual(2, resolved.GetByCardId(active.CardId).Cost);
            Assert.AreEqual("New text", resolved.GetByCardId(active.CardId).Text);
            CollectionAssert.AreEqual(new[] { "active_effect" }, resolved.GetByCardId(active.CardId).EffectIds);
            Assert.AreEqual(TrinketOfferPoolStatus.Disabled, resolved.GetByCardId(removed.CardId).OfferPoolStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Disabled, resolved.GetByCardId(planned.CardId).OfferPoolStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Disabled, active.OfferPoolStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, removed.OfferPoolStatus);
        }

        [Test]
        public void CardPoolAvailabilityRejectsDuosMinionsAndTavernSpells()
        {
            var availability = new CardPoolAvailability(null);

            Assert.IsFalse(availability.AllowsMinion(new MinionDefinition
            {
                CardId = "BGDUO_TEST_MINION",
                InPool = true
            }));
            Assert.IsFalse(availability.AllowsTavernSpell(new TavernSpellDefinition
            {
                CardNumber = "BGDUO_TEST_SPELL",
                Category = "TavernSpell",
                InPool = true
            }));
        }

        private static GameCatalogSet ReplaceMinions(GameCatalogSet source, MinionDefinition minion)
        {
            return new GameCatalogSet(
                new MinionCatalog(new[] { minion }),
                source.Spells,
                source.Heroes,
                source.Trinkets,
                source.Quests,
                source.TimewarpedTavern,
                source.Anomalies,
                source.DarkmoonPrizes);
        }

        private static GameCatalogSet ReplaceTrinkets(GameCatalogSet source, params TrinketDefinition[] trinkets)
        {
            return new GameCatalogSet(
                source.Minions,
                source.Spells,
                source.Heroes,
                new TrinketCatalog(trinkets),
                source.Quests,
                source.TimewarpedTavern,
                source.Anomalies,
                source.DarkmoonPrizes,
                source.DarkGifts);
        }

        private static string FixturePath(string fileName)
        {
            return Path.Combine(UnityEngine.Application.dataPath, "LearnHearthstone", "Tests", "EditMode", "Catalogs", "Fixtures", fileName);
        }
    }
}
