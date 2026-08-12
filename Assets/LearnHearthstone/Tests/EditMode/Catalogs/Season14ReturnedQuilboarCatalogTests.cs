using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14ReturnedQuilboarCatalogTests
    {
        [TestCase("POOL-D08", "BG33_885", 6, 3, 10, 6, 20)]
        [TestCase("POOL-D09", "BG20_101", 2, 2, 4, 4, 8)]
        [TestCase("POOL-D10", "BG31_320", 2, 2, 2, 4, 4)]
        [TestCase("POOL-D11", "BG33_430", 2, 1, 3, 2, 6)]
        [TestCase("POOL-D12", "BG20_104", 4, 2, 7, 4, 14)]
        [TestCase("POOL-D13", "BG31_327", 4, 4, 5, 8, 10)]
        [TestCase("POOL-D14A", "BG33_886", 1, 2, 3, 4, 6)]
        [TestCase("POOL-D14B", "BG31_326", 4, 4, 4, 8, 8)]
        [TestCase("POOL-D14C", "BG33_883", 5, 5, 5, 10, 10)]
        [TestCase("POOL-D14D", "BG31_323", 6, 5, 7, 10, 14)]
        public void EmbeddedCatalog_DefinesReturnedQuilboarPreviewCarrier(
            string researchKey,
            string cardId,
            int tier,
            int attack,
            int health,
            int goldenAttack,
            int goldenHealth)
        {
            var definition = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha")
                .Chinese.Minions.All.Single(item => item.ResearchKey == researchKey);

            Assert.AreEqual(cardId, definition.CardId);
            Assert.AreEqual(cardId + "@36.2-preview-v1", definition.RevisionId);
            Assert.AreEqual(tier, definition.TavernTier);
            Assert.AreEqual(attack, definition.BaseAttack);
            Assert.AreEqual(health, definition.BaseHealth);
            Assert.IsTrue(definition.Tribes.Contains(Tribe.Quilboar));
            Assert.AreEqual("CommunityCrossChecked", definition.SourceLevel);
            Assert.AreEqual("Partial", definition.ImplementationStatus);
            Assert.IsFalse(definition.InPool);
            Assert.NotNull(definition.Golden);
            Assert.AreEqual(cardId + "_G", definition.Golden.CardId);
            Assert.AreEqual(goldenAttack, definition.Golden.BaseAttack);
            Assert.AreEqual(goldenHealth, definition.Golden.BaseHealth);
        }

        [Test]
        public void PreviewContentSet_SelectsReturnedQuilboarRevisionsOnlyForPreview()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);
            var cardIds = new[]
            {
                "BG33_885", "BG20_101", "BG31_320", "BG33_430", "BG20_104",
                "BG31_327", "BG33_886", "BG31_326", "BG33_883", "BG31_323"
            };

            Assert.AreEqual(103, preview.EntityRevisions.Count(revision => revision.Kind == EntityKind.Minion));
            Assert.AreEqual(103, preview.ContentSet.MinionRevisionIds.Count);
            CollectionAssert.IsSubsetOf(cardIds, preview.EntityRevisions.Select(revision => revision.StableEntityId).ToList());
            Assert.IsFalse(legacy.EntityRevisions.Any(revision => cardIds.Contains(revision.StableEntityId)));
        }
    }
}
