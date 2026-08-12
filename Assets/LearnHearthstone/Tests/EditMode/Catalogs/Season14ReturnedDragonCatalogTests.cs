using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14ReturnedDragonCatalogTests
    {
        [TestCase("POOL-D25", "BG26_963", 100026, 2, 3, 4, "BG26_963_G", 100029, 6, 8)]
        [TestCase("POOL-D26A", "BG29_888", 113346, 1, 1, 4, "BG29_888_G", 113347, 2, 8)]
        [TestCase("POOL-D26B", "BG29_810", 108116, 2, 2, 3, "BG29_810_G", 108279, 4, 6)]
        [TestCase("POOL-D26D", "BG24_004", 92413, 6, 12, 4, "BG24_004_G", 92422, 24, 8)]
        public void EmbeddedCatalog_DefinesReturnedDragonsFromIydImages(
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
            Assert.Contains(Tribe.Dragon, definition.Tribes);
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
        public void FirescaleHoarder_ReusesCompletedNagaEntity()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;
            var firescale = minions.Single(item => item.ResearchKey == "POOL-D06C");

            Assert.AreEqual("BG32_820", firescale.CardId);
            Assert.AreEqual(5, firescale.TavernTier);
            Assert.AreEqual(5, firescale.BaseAttack);
            Assert.AreEqual(5, firescale.BaseHealth);
            Assert.Contains(Tribe.Naga, firescale.Tribes);
            Assert.Contains(Tribe.Dragon, firescale.Tribes);
            Assert.AreEqual(1, minions.Count(item => item.CardId == "BG32_820"));
        }

        [Test]
        public void PreviewContentSet_SelectsReturnedDragonsOnlyForPreview()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);
            var cardIds = new[] { "BG26_963", "BG29_888", "BG29_810", "BG24_004" };

            Assert.AreEqual(103, preview.EntityRevisions.Count(revision => revision.Kind == EntityKind.Minion));
            Assert.AreEqual(103, preview.ContentSet.MinionRevisionIds.Count);
            CollectionAssert.IsSubsetOf(cardIds, preview.EntityRevisions.Select(revision => revision.StableEntityId).ToList());
            Assert.IsFalse(legacy.EntityRevisions.Any(revision => cardIds.Contains(revision.StableEntityId)));
        }
    }
}
