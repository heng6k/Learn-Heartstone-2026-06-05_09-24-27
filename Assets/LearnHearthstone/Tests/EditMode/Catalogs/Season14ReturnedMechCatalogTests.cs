using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14ReturnedMechCatalogTests
    {
        [TestCase("POOL-D20A", "BG31_177", 115678, 2, 3, 1, "BG31_177_G", 115679, 6, 2, "Implemented")]
        [TestCase("POOL-D20B", "BG26_152", 98588, 6, 4, 6, "BG26_152_G", 98591, 8, 12, "Partial")]
        public void EmbeddedCatalog_DefinesReturnedMechsFromIydImages(
            string researchKey,
            string cardId,
            int dbfId,
            int tier,
            int attack,
            int health,
            string goldenCardId,
            int goldenDbfId,
            int goldenAttack,
            int goldenHealth,
            string implementationStatus)
        {
            var definition = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha")
                .Chinese.Minions.All.Single(item => item.ResearchKey == researchKey);

            Assert.AreEqual(cardId, definition.CardId);
            Assert.AreEqual(cardId + "@36.2-preview-v1", definition.RevisionId);
            Assert.AreEqual(dbfId, definition.DbfId);
            Assert.AreEqual(tier, definition.TavernTier);
            Assert.AreEqual(attack, definition.BaseAttack);
            Assert.AreEqual(health, definition.BaseHealth);
            Assert.Contains(Tribe.Mech, definition.Tribes);
            Assert.AreEqual("CommunityCrossChecked", definition.SourceLevel);
            Assert.AreEqual(implementationStatus, definition.ImplementationStatus);
            Assert.IsFalse(definition.InPool);
            Assert.NotNull(definition.Golden);
            Assert.AreEqual(goldenCardId, definition.Golden.CardId);
            Assert.AreEqual(goldenDbfId, definition.Golden.DbfId);
            Assert.AreEqual(goldenAttack, definition.Golden.BaseAttack);
            Assert.AreEqual(goldenHealth, definition.Golden.BaseHealth);
        }

        [Test]
        public void PreviewContentSet_SelectsReturnedMechsOnlyForPreview()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);
            var cardIds = new[] { "BG31_177", "BG26_152" };

            Assert.AreEqual(103, preview.EntityRevisions.Count(revision => revision.Kind == EntityKind.Minion));
            Assert.AreEqual(103, preview.ContentSet.MinionRevisionIds.Count);
            CollectionAssert.IsSubsetOf(cardIds, preview.EntityRevisions.Select(revision => revision.StableEntityId).ToList());
            Assert.IsFalse(legacy.EntityRevisions.Any(revision => cardIds.Contains(revision.StableEntityId)));
        }

        [Test]
        public void PreviewPool_ContainsTierFiveGlambot()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var glambot = preview.Snapshot.Chinese.Minions.All.Single(item => item.CardId == "BG36_853");

            Assert.IsTrue(glambot.InPool);
            Assert.AreEqual(5, glambot.TavernTier);
            Assert.Contains(Tribe.Mech, glambot.Tribes);
        }
    }
}
