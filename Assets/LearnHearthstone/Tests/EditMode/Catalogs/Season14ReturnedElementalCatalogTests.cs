using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14ReturnedElementalCatalogTests
    {
        [TestCase("POOL-D15A", "BGS_127", "TB_Baconups_202", 1, 3, 3, 6, 6)]
        [TestCase("POOL-D15B", "BG31_843", "BG31_843_G", 3, 2, 4, 4, 8)]
        [TestCase("POOL-D15C", "BG26_537", "BG26_537_G", 5, 2, 1, 4, 2)]
        public void EmbeddedCatalog_DefinesReturnedElementalPreviewCarrier(
            string researchKey,
            string cardId,
            string goldenCardId,
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
            Assert.IsTrue(definition.Tribes.Contains(Tribe.Elemental));
            Assert.AreEqual("CommunityCrossChecked", definition.SourceLevel);
            Assert.AreEqual("Partial", definition.ImplementationStatus);
            Assert.IsFalse(definition.InPool);
            Assert.NotNull(definition.Golden);
            Assert.AreEqual(goldenCardId, definition.Golden.CardId);
            Assert.AreEqual(goldenAttack, definition.Golden.BaseAttack);
            Assert.AreEqual(goldenHealth, definition.Golden.BaseHealth);
        }

        [Test]
        public void PreviewContentSet_SelectsReturnedElementalRevisionsOnlyForPreview()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);
            var cardIds = new[] { "BGS_127", "BG31_843", "BG26_537" };

            Assert.AreEqual(103, preview.EntityRevisions.Count(revision => revision.Kind == EntityKind.Minion));
            Assert.AreEqual(103, preview.ContentSet.MinionRevisionIds.Count);
            CollectionAssert.IsSubsetOf(cardIds, preview.EntityRevisions.Select(revision => revision.StableEntityId).ToList());
            Assert.IsFalse(legacy.EntityRevisions.Any(revision => cardIds.Contains(revision.StableEntityId)));
        }
    }
}
