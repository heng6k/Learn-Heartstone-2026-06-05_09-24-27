using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14PoolRevisionTests
    {
        [TestCase("BG_DEEP_015", 3, 3, 1, 6, 2)]
        [TestCase("BG30_123", 4, 2, 4, 4, 8)]
        [TestCase("BG33_155", 3, 2, 2, 4, 4)]
        [TestCase("BG27_556", 3, 4, 5, 8, 10)]
        [TestCase("BG35_921", 5, 2, 1, 4, 2)]
        [TestCase("BG26_810", 4, 2, 6, 4, 12)]
        [TestCase("BG31_824", 5, 4, 5, 8, 10)]
        public void PreviewVersion_AppliesSeason14MinionStatRevision(
            string cardId,
            int tier,
            int attack,
            int health,
            int goldenAttack,
            int goldenHealth)
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var minion = resolved.Snapshot.Chinese.Minions.GetByCardId(cardId);

            Assert.AreEqual(tier, minion.TavernTier);
            Assert.AreEqual(attack, minion.BaseAttack);
            Assert.AreEqual(health, minion.BaseHealth);
            Assert.AreEqual(goldenAttack, minion.Golden.BaseAttack);
            Assert.AreEqual(goldenHealth, minion.Golden.BaseHealth);
            Assert.AreEqual(1, resolved.EntityRevisions.Count(revision =>
                revision.Kind == EntityKind.Minion && revision.StableEntityId == cardId));
        }

        [TestCase("BG_DEEP_015", 4, 3, 1, 6, 2)]
        [TestCase("BG30_123", 3, 2, 4, 4, 8)]
        [TestCase("BG33_155", 4, 2, 2, 4, 4)]
        [TestCase("BG27_556", 4, 5, 6, 10, 12)]
        [TestCase("BG35_921", 4, 1, 1, 2, 2)]
        [TestCase("BG26_810", 3, 3, 6, 6, 12)]
        [TestCase("BG31_824", 4, 3, 4, 6, 8)]
        public void LegacyVersion_KeepsHistoricalMinionStats(
            string cardId,
            int tier,
            int attack,
            int health,
            int goldenAttack,
            int goldenHealth)
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);
            var minion = resolved.Snapshot.Chinese.Minions.GetByCardId(cardId);

            Assert.AreEqual(tier, minion.TavernTier);
            Assert.AreEqual(attack, minion.BaseAttack);
            Assert.AreEqual(health, minion.BaseHealth);
            Assert.AreEqual(goldenAttack, minion.Golden.BaseAttack);
            Assert.AreEqual(goldenHealth, minion.Golden.BaseHealth);
            Assert.IsFalse(resolved.EntityRevisions.Any(revision =>
                revision.Kind == EntityKind.Minion && revision.StableEntityId == cardId));
        }

        [Test]
        public void PreviewContentSet_MergesExplicitStatRevisionsWithPreviewCarrierRevisions()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var adjustedIds = new[]
            {
                "BG_DEEP_015", "BG30_123", "BG33_155", "BG27_556",
                "BG35_921", "BG26_810", "BG31_824", "BGS_018"
            };

            Assert.AreEqual(103, resolved.ContentSet.MinionRevisionIds.Count);
            CollectionAssert.IsSubsetOf(
                adjustedIds,
                resolved.EntityRevisions
                    .Where(revision => revision.Kind == EntityKind.Minion)
                    .Select(revision => revision.StableEntityId)
                    .ToArray());
        }
    }
}
