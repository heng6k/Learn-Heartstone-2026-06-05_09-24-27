using System;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14TrinketContentCatalogTests
    {
        private static readonly int[] RemovedDbfIds =
        {
            112073, 120152, 130876, 131312, 121062, 130899, 130900,
            115231, 110972, 130878, 114212, 131001, 130906, 130902, 117856, 130905, 131275
        };

        [Test]
        public void Catalog_ContainsAllSeason14TrinketsWithLocalizedTextAndImages()
        {
            var english = TrinketCatalogLoader.LoadFromResources(true);
            var chinese = TrinketCatalogLoader.LoadFromResources(false);
            var season14 = english.All
                .Where(item => !string.IsNullOrWhiteSpace(item.ResearchKey))
                .Where(item => item.ResearchKey.StartsWith("LT-R", StringComparison.Ordinal) ||
                               item.ResearchKey.StartsWith("GT-R", StringComparison.Ordinal))
                .ToArray();

            Assert.AreEqual(47, season14.Length);
            Assert.AreEqual(
                "光羽标签",
                chinese.GetByCardId("BG36_MagicItem_213").Name);
            Assert.AreEqual(23, season14.Count(item => item.SlotKind == TrinketSlotKind.Lesser));
            Assert.AreEqual(24, season14.Count(item => item.SlotKind == TrinketSlotKind.Greater));
            Assert.AreEqual(47, season14.Select(item => item.ResearchKey).Distinct(StringComparer.Ordinal).Count());
            Assert.IsTrue(season14.All(item => item.DbfId > 0));
            Assert.IsTrue(season14.All(item =>
                item.CardId.StartsWith("BG36_MagicItem_", StringComparison.Ordinal)));
            Assert.IsFalse(season14.Any(item =>
                item.CardId.StartsWith("preview-s14-trinket-", StringComparison.Ordinal)));
            Assert.IsTrue(season14.All(item => !string.IsNullOrWhiteSpace(item.Text)));
            Assert.IsTrue(season14.All(item => !string.IsNullOrWhiteSpace(item.ImagePath)));
            Assert.IsTrue(season14.All(item => item.OfferPoolStatus == TrinketOfferPoolStatus.Disabled));
            Assert.IsTrue(season14.All(item => Resources.Load<Texture2D>(item.ImagePath) != null));
            Assert.IsTrue(season14.All(item =>
            {
                var localized = chinese.GetByCardId(item.CardId);
                return !string.IsNullOrWhiteSpace(localized.Name) &&
                       !string.IsNullOrWhiteSpace(localized.Text);
            }));
            Assert.AreEqual(3, season14.Single(item => item.ResearchKey == "LT-R08").Cost);
            Assert.AreEqual("BG36_MagicItem_390", season14.Single(item => item.ResearchKey == "LT-R08").CardId);
            Assert.AreEqual("BG36_MagicItem_213", season14.Single(item => item.ResearchKey == "GT-R24").CardId);
        }

        [Test]
        public void Catalog_UsesCurrentLiveClientTextForChangedReturningTrinkets()
        {
            var english = TrinketCatalogLoader.LoadFromResources(true);

            Assert.AreEqual(
                "After you play a <b>Magnetic</b> minion, cast Repair Job on a random friendly Mech.",
                english.GetByCardId("BG32_MagicItem_170").Text);
            StringAssert.Contains("+4/+4", english.GetByCardId("BG35_MagicItem_151").Text);
            StringAssert.Contains("+15/+15", english.GetByCardId("BG35_MagicItem_151t").Text);
        }

        [Test]
        public void PreviewContentSet_UsesSoloTrinketWhitelistAndKeepsNewCardsOutOfLegacy()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var contentSet = snapshot.VersionedContent.ContentSets.Single(item =>
                item.Id == ContentSetIds.Season14Preview);
            var membership = contentSet.PoolMembership
                .Where(item => item.Kind == EntityKind.Trinket)
                .Select(item => item.StableEntityId)
                .ToArray();
            var preview = snapshot.VersionedContent.CreateResolver()
                .Resolve(GameVersionIds.Season14Preview, snapshot)
                .Snapshot.English.Trinkets;
            var legacy = snapshot.VersionedContent.CreateResolver()
                .Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot)
                .Snapshot.English.Trinkets;
            var newCards = preview.All.Where(item => item.ResearchKey?.EndsWith("-R01", StringComparison.Ordinal) == true ||
                                                     item.ResearchKey?.StartsWith("LT-R", StringComparison.Ordinal) == true ||
                                                     item.ResearchKey?.StartsWith("GT-R", StringComparison.Ordinal) == true)
                .ToArray();

            Assert.AreEqual(242, membership.Length);
            Assert.AreEqual(242, membership.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.AreEqual(47, contentSet.TrinketRevisionIds.Count);
            Assert.AreEqual(47, newCards.Length);
            Assert.IsTrue(newCards.All(item => membership.Contains(item.CardId, StringComparer.OrdinalIgnoreCase)));
            Assert.IsTrue(newCards.All(item => legacy.GetByCardId(item.CardId).OfferPoolStatus == TrinketOfferPoolStatus.Disabled));
            Assert.IsTrue(RemovedDbfIds.All(dbfId =>
                preview.All.Single(item => item.DbfId == dbfId).OfferPoolStatus == TrinketOfferPoolStatus.Disabled));
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, preview.GetByCardId("BG35_MagicItem_151").OfferPoolStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, preview.GetByCardId("BG35_MagicItem_151t").OfferPoolStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, preview.GetByCardId("BG30_MagicItem_944").OfferPoolStatus);
            Assert.AreEqual(329, legacy.Offerable.Count);
        }

        [TestCase(111664, 2)]
        [TestCase(120866, 6)]
        [TestCase(115253, 2)]
        [TestCase(117416, 2)]
        [TestCase(131278, 1)]
        [TestCase(131277, 1)]
        [TestCase(117858, 1)]
        public void PreviewContentSet_UsesCurrentLiveClientCosts(int dbfId, int expectedCost)
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver()
                .Resolve(GameVersionIds.Season14Preview, snapshot)
                .Snapshot.English.Trinkets;

            Assert.AreEqual(expectedCost, preview.All.Single(item => item.DbfId == dbfId).Cost);
        }
    }
}
