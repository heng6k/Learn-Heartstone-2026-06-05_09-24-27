using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14ReturnedUndeadCatalogTests
    {
        [Test]
        public void EmbeddedCatalog_DefinesMawCasterPreviewCarrierFromIydImage()
        {
            var definition = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha")
                .Chinese.Minions.All.Single(item => item.ResearchKey == "POOL-D02");

            Assert.AreEqual("BG32_340", definition.CardId);
            Assert.AreEqual("BG32_340@36.2-preview-v1", definition.RevisionId);
            Assert.AreEqual(121508, definition.DbfId);
            Assert.AreEqual(5, definition.TavernTier);
            Assert.AreEqual(4, definition.BaseAttack);
            Assert.AreEqual(5, definition.BaseHealth);
            Assert.IsTrue(definition.Tribes.Contains(Tribe.Undead));
            Assert.IsTrue(definition.Keywords.Contains(Keyword.Battlecry));
            Assert.AreEqual("CommunityCrossChecked", definition.SourceLevel);
            Assert.AreEqual("Partial", definition.ImplementationStatus);
            Assert.IsFalse(definition.InPool);
            Assert.NotNull(definition.Golden);
            Assert.AreEqual("BG32_340_G", definition.Golden.CardId);
            Assert.AreEqual(121509, definition.Golden.DbfId);
            Assert.AreEqual(8, definition.Golden.BaseAttack);
            Assert.AreEqual(10, definition.Golden.BaseHealth);
        }

        [Test]
        public void PreviewContentSet_SelectsMawCasterOnlyForPreview()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);

            Assert.AreEqual(103, preview.EntityRevisions.Count(revision => revision.Kind == EntityKind.Minion));
            Assert.AreEqual(103, preview.ContentSet.MinionRevisionIds.Count);
            Assert.IsTrue(preview.EntityRevisions.Any(revision => revision.StableEntityId == "BG32_340"));
            Assert.IsFalse(legacy.EntityRevisions.Any(revision => revision.StableEntityId == "BG32_340"));
        }
    }
}
