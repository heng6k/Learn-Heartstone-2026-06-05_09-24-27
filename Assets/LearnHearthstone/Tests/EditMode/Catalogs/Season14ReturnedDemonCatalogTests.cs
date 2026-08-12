using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14ReturnedDemonCatalogTests
    {
        [TestCase("POOL-D23A", "BG23_357", 93321, 2, 3, 2, "BG23_357_G", 93323, 6, 4)]
        [TestCase("POOL-D23B", "BG21_004", 72060, 5, 4, 6, "BG21_004_G", 72821, 8, 12)]
        [TestCase("POOL-D23C", "BG26_523", 99228, 5, 3, 6, "BG26_523_G", 99229, 6, 12)]
        public void EmbeddedCatalog_DefinesReturnedDemonsFromIydImages(
            string researchKey,
            string cardId,
            int dbfId,
            int tier,
            int attack,
            int health,
            string goldenCardId,
            int goldenDbfId,
            int goldenAttack,
            int goldenHealth)
        {
            var definition = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha")
                .Chinese.Minions.All.Single(item => item.ResearchKey == researchKey);

            Assert.AreEqual(cardId, definition.CardId);
            Assert.AreEqual(cardId + "@36.2-preview-v1", definition.RevisionId);
            Assert.AreEqual(dbfId, definition.DbfId);
            Assert.AreEqual(tier, definition.TavernTier);
            Assert.AreEqual(attack, definition.BaseAttack);
            Assert.AreEqual(health, definition.BaseHealth);
            Assert.Contains(Tribe.Demon, definition.Tribes);
            Assert.AreEqual("CommunityCrossChecked", definition.SourceLevel);
            Assert.AreEqual("Partial", definition.ImplementationStatus);
            Assert.IsFalse(definition.InPool);
            Assert.NotNull(definition.Golden);
            Assert.AreEqual(goldenCardId, definition.Golden.CardId);
            Assert.AreEqual(goldenDbfId, definition.Golden.DbfId);
            Assert.AreEqual(goldenAttack, definition.Golden.BaseAttack);
            Assert.AreEqual(goldenHealth, definition.Golden.BaseHealth);
        }

        [Test]
        public void PreviewContentSet_SelectsReturnedDemonsOnlyForPreview()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);
            var cardIds = new[] { "BG23_357", "BG21_004", "BG26_523" };

            Assert.AreEqual(103, preview.EntityRevisions.Count(revision => revision.Kind == EntityKind.Minion));
            Assert.AreEqual(103, preview.ContentSet.MinionRevisionIds.Count);
            CollectionAssert.IsSubsetOf(cardIds, preview.EntityRevisions.Select(revision => revision.StableEntityId).ToList());
            Assert.IsFalse(legacy.EntityRevisions.Any(revision => cardIds.Contains(revision.StableEntityId)));
        }
    }
}
