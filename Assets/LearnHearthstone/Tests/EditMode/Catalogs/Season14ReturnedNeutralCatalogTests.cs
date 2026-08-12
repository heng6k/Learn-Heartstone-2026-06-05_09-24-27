using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14ReturnedNeutralCatalogTests
    {
        [Test]
        public void EmbeddedCatalog_DefinesMotleyPhalanxFromIydImage()
        {
            var definition = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha")
                .Chinese.Minions.All.Single(item => item.ResearchKey == "POOL-D27");

            Assert.AreEqual("BG27_080", definition.CardId);
            Assert.AreEqual("BG27_080@36.2-preview-v1", definition.RevisionId);
            Assert.AreEqual(106487, definition.DbfId);
            Assert.AreEqual(4, definition.TavernTier);
            Assert.AreEqual(2, definition.BaseAttack);
            Assert.AreEqual(2, definition.BaseHealth);
            Assert.Contains(Tribe.All, definition.Tribes);
            Assert.Contains(Keyword.Taunt, definition.Keywords);
            Assert.Contains(Keyword.Deathrattle, definition.Keywords);
            Assert.AreEqual("CommunityCrossChecked", definition.SourceLevel);
            Assert.AreEqual("Partial", definition.ImplementationStatus);
            Assert.IsFalse(definition.InPool);
            Assert.NotNull(definition.Golden);
            Assert.AreEqual("BG27_080_G", definition.Golden.CardId);
            Assert.AreEqual(106488, definition.Golden.DbfId);
            Assert.AreEqual(4, definition.Golden.BaseAttack);
            Assert.AreEqual(4, definition.Golden.BaseHealth);
        }

        [Test]
        public void PreviewContentSet_SelectsMotleyPhalanxOnlyForPreview()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);

            Assert.AreEqual(103, preview.EntityRevisions.Count(revision => revision.Kind == EntityKind.Minion));
            Assert.AreEqual(103, preview.ContentSet.MinionRevisionIds.Count);
            Assert.IsTrue(preview.EntityRevisions.Any(revision => revision.StableEntityId == "BG27_080"));
            Assert.IsFalse(legacy.EntityRevisions.Any(revision => revision.StableEntityId == "BG27_080"));
        }
    }
}
