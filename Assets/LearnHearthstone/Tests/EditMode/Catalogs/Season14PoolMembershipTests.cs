using System;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14PoolMembershipTests
    {
        private static readonly int[] RemovedMinionDbfIds =
        {
            60247, 61049, 62230, 70147, 70153, 72061, 72067, 72073, 80756, 87060,
            92400, 92880, 97115, 97535, 97549, 97553, 98829, 98880, 98886, 98930,
            95271, 98946, 100997, 101092, 101114, 103674, 104468, 104540, 104766, 105518,
            107930, 107937, 108393, 108397, 108439, 113152, 114099, 114379, 114391,
            114469, 114486, 115593, 115666, 115674, 115680, 116734, 119202, 119992,
            120031, 120299, 120662, 121135, 122086, 122094, 122116, 122175, 122283,
            122434, 122605, 122672, 122737, 123644, 126629, 126631, 126633, 126741,
            126745, 126802, 126830, 126849, 126918, 126955, 126711, 126713, 126715,
            126717, 126718, 126848, 127111, 127236, 127240,
            128168, 129263, 129745, 130076, 130079, 130149, 130153, 130157, 130294,
            130296, 130552, 130703, 130705, 130709, 130794, 131606, 131608
        };

        private static readonly int[] GeneratedOnlyChromadrakeDbfIds =
        {
            126711, 126713, 126715, 126717, 126718
        };

        private static readonly string[] RemovedSpellIds =
        {
            "130310", "130311", "100596", "105665", "113902", "131153"
        };

        private static readonly string[] ReturnedSpellIds =
        {
            "116221", "117567", "117584", "115910"
        };

        private static readonly string[] RemovedTimewarpedIds =
        {
            "BG34_Giant_031", "BG34_PreMadeChamp_090"
        };

        [Test]
        public void PreviewContentSet_UsesCompleteUniquePoolWhitelists()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var contentSet = snapshot.VersionedContent.ContentSets.Single(item =>
                item.Id == ContentSetIds.Season14Preview);

            AssertMembership(contentSet, EntityKind.Minion, 274);
            AssertMembership(contentSet, EntityKind.TavernSpell, 76);
            AssertMembership(contentSet, EntityKind.Trinket, 242);
            AssertMembership(contentSet, EntityKind.TimewarpedTavern, 123);
            Assert.AreEqual(
                contentSet.PoolMembership.Count,
                contentSet.PoolMembership
                    .Select(item => item.Kind + "|" + item.StableEntityId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count());
        }

        [Test]
        public void PreviewVersion_AtomicallyEnablesReturnsAndRemovesDepartures()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var minions = resolved.Snapshot.Chinese.Minions.All;
            var spells = resolved.Snapshot.Chinese.Spells.All;
            var heroes = resolved.Snapshot.Chinese.Heroes;

            Assert.AreEqual(274, minions.Count(item => item.InPool));
            Assert.IsTrue(minions
                .Where(item => !string.IsNullOrWhiteSpace(item.ResearchKey))
                .All(item => item.InPool));
            Assert.IsTrue(minions
                .Where(item => RemovedMinionDbfIds.Contains(item.DbfId))
                .All(item => !item.InPool));
            Assert.AreEqual(RemovedMinionDbfIds.Length, minions.Count(item => RemovedMinionDbfIds.Contains(item.DbfId)));
            Assert.IsTrue(GeneratedOnlyChromadrakeDbfIds.All(dbfId =>
                minions.Single(item => item.DbfId == dbfId).InPool == false));
            Assert.IsFalse(minions.Single(item => item.CardId == "BG25_013").InPool);
            Assert.IsFalse(minions.Single(item => item.CardId == "BG34_639").InPool);
            Assert.IsTrue(minions.Single(item => item.CardId == "BG36_853").InPool);
            Assert.AreEqual(5, minions.Single(item => item.CardId == "BG36_853").TavernTier);
            Assert.AreEqual(4, minions.Single(item => item.CardId == "BG27_080").TavernTier);

            Assert.AreEqual(76, spells.Count(item => item.Category == "TavernSpell" && item.InPool));
            Assert.IsTrue(RemovedSpellIds.All(id => !spells.Single(item => item.CardNumber == id).InPool));
            Assert.IsTrue(ReturnedSpellIds.All(id => spells.Single(item => item.CardNumber == id).InPool));
            Assert.IsFalse(spells.Single(item => item.CardNumber == "116596").InPool);

            var xavius = heroes.GetHeroByCardId("BG36_HERO_105");
            var trastath = heroes.GetHeroByCardId("BG36_HERO_101");
            Assert.IsTrue(xavius.InPool);
            Assert.IsTrue(trastath.InPool);
            Assert.AreEqual(12, xavius.Armor);
            Assert.AreEqual(10, trastath.Armor);

            Assert.AreEqual(123, resolved.Snapshot.Chinese.TimewarpedTavern.Current.Count);
            Assert.IsTrue(RemovedTimewarpedIds.All(id =>
                !resolved.Snapshot.Chinese.TimewarpedTavern.Current.Any(item => item.CardId == id)));
            Assert.IsTrue(RemovedTimewarpedIds.All(id =>
                resolved.Snapshot.Chinese.TimewarpedTavern.GetByCardId(id).PoolStatus == "removed"));
            Assert.AreEqual(242, resolved.Snapshot.Chinese.Trinkets.Offerable.Count);
        }

        [Test]
        public void LegacyVersion_KeepsHistoricalPools()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);

            Assert.AreEqual(277, resolved.Snapshot.Chinese.Minions.All.Count(item => item.InPool));
            Assert.AreEqual(73, resolved.Snapshot.Chinese.Spells.All.Count(item =>
                item.Category == "TavernSpell" && item.InPool));
            Assert.AreEqual(125, resolved.Snapshot.Chinese.TimewarpedTavern.Current.Count);
            Assert.AreEqual(329, resolved.Snapshot.Chinese.Trinkets.Offerable.Count);
            Assert.IsTrue(GeneratedOnlyChromadrakeDbfIds.All(dbfId =>
                resolved.Snapshot.Chinese.Minions.All.Single(item => item.DbfId == dbfId).InPool));
            Assert.IsTrue(RemovedTimewarpedIds.All(id =>
                resolved.Snapshot.Chinese.TimewarpedTavern.Current.Any(item => item.CardId == id)));
            Assert.IsFalse(resolved.Snapshot.Chinese.Heroes
                .GetHeroByCardId("BG36_HERO_105").InPool);
            Assert.IsFalse(resolved.Snapshot.Chinese.Heroes
                .GetHeroByCardId("BG36_HERO_101").InPool);
        }

        [Test]
        public void ContentSet_DeduplicatesMembershipByKindAndStableId()
        {
            var contentSet = new ContentSetDefinition(
                "deduplicated-content",
                poolMembership: new[]
                {
                    new PoolMembershipEntry(EntityKind.Minion, "BG_TEST"),
                    new PoolMembershipEntry(EntityKind.Minion, "bg_test"),
                    new PoolMembershipEntry(EntityKind.TavernSpell, "BG_TEST")
                });

            Assert.AreEqual(2, contentSet.PoolMembership.Count);
        }

        [Test]
        public void Resolve_RejectsUnknownMembershipBeforeProducingSnapshot()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var version = new GameVersionDefinition(
                "invalid-membership-version",
                "Invalid Membership Version",
                new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc),
                GameVersionOfficialStatus.Announced,
                GameVersionImplementationStatus.Partial,
                "invalid-membership-ruleset",
                "invalid-membership-content",
                string.Empty);
            var contentSet = new ContentSetDefinition(
                version.ContentSetId,
                poolMembership: new[]
                {
                    new PoolMembershipEntry(EntityKind.Minion, "BG_DOES_NOT_EXIST")
                });
            var resolver = new GameVersionResolver(
                new GameVersionCatalog(new[] { version }),
                new[] { new RulesetDefinition(version.RulesetId, 1) },
                new[] { contentSet },
                Array.Empty<EntityRevisionDefinition>());

            Assert.Throws<InvalidDataException>(() => resolver.Resolve(version.Id, snapshot));
        }

        private static void AssertMembership(ContentSetDefinition contentSet, EntityKind kind, int expected)
        {
            var ids = contentSet.PoolMembership
                .Where(item => item.Kind == kind)
                .Select(item => item.StableEntityId)
                .ToArray();
            Assert.AreEqual(expected, ids.Length);
            Assert.AreEqual(expected, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
    }
}
