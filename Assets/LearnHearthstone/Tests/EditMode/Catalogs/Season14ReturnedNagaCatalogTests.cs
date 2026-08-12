using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14ReturnedNagaCatalogTests
    {
        [TestCase("POOL-D04", "BG23_000", 1, 1, 4, 2, 8)]
        [TestCase("POOL-D05", "BG23_007", 3, 2, 6, 4, 12)]
        [TestCase("POOL-D06A", "BG31_924", 2, 2, 2, 4, 4)]
        [TestCase("POOL-D06B", "BG31_925", 5, 4, 3, 8, 6)]
        [TestCase("POOL-D06C", "BG32_820", 5, 5, 5, 10, 10)]
        [TestCase("POOL-D06D", "BG32_837", 6, 4, 9, 8, 18)]
        public void EmbeddedCatalog_DefinesReturnedNagaPreviewCarrier(
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
            Assert.IsTrue(definition.Tribes.Contains(Tribe.Naga));
            Assert.AreEqual("CommunityCrossChecked", definition.SourceLevel);
            Assert.AreEqual("Partial", definition.ImplementationStatus);
            Assert.IsFalse(definition.InPool);
            Assert.NotNull(definition.Golden);
            Assert.AreEqual(cardId + "_G", definition.Golden.CardId);
            Assert.AreEqual(goldenAttack, definition.Golden.BaseAttack);
            Assert.AreEqual(goldenHealth, definition.Golden.BaseHealth);
        }

        [Test]
        public void PreviewContentSet_SelectsReturnedNagaRevisionsOnlyForPreview()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);
            var cardIds = new[] { "BG23_000", "BG23_007", "BG31_924", "BG31_925", "BG32_820", "BG32_837" };

            Assert.AreEqual(103, preview.EntityRevisions.Count(revision => revision.Kind == EntityKind.Minion));
            Assert.AreEqual(103, preview.ContentSet.MinionRevisionIds.Count);
            CollectionAssert.IsSubsetOf(cardIds, preview.EntityRevisions.Select(revision => revision.StableEntityId).ToList());
            Assert.IsFalse(legacy.EntityRevisions.Any(revision => cardIds.Contains(revision.StableEntityId)));
        }
    }
}
