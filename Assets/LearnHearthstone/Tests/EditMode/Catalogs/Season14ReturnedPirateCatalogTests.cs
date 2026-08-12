using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14ReturnedPirateCatalogTests
    {
        [Test]
        public void EmbeddedCatalog_DefinesAzsharanCutlassierPreviewCarrier()
        {
            var definition = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha")
                .Chinese.Minions.All.Single(item => item.ResearchKey == "POOL-D18");

            Assert.AreEqual("BG33_830", definition.CardId);
            Assert.AreEqual("BG33_830@36.2-preview-v1", definition.RevisionId);
            Assert.AreEqual(3, definition.TavernTier);
            Assert.AreEqual(6, definition.BaseAttack);
            Assert.AreEqual(4, definition.BaseHealth);
            Assert.IsTrue(definition.Tribes.Contains(Tribe.Pirate));
            Assert.IsTrue(definition.Keywords.Contains(Keyword.Battlecry));
            Assert.AreEqual("CommunityCrossChecked", definition.SourceLevel);
            Assert.AreEqual("Partial", definition.ImplementationStatus);
            Assert.IsFalse(definition.InPool);
            Assert.NotNull(definition.Golden);
            Assert.AreEqual("BG33_830_G", definition.Golden.CardId);
            Assert.AreEqual(12, definition.Golden.BaseAttack);
            Assert.AreEqual(8, definition.Golden.BaseHealth);
        }

        [Test]
        public void PreviewContentSet_SelectsAzsharanCutlassierOnlyForPreview()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);

            Assert.AreEqual(103, preview.EntityRevisions.Count(revision => revision.Kind == EntityKind.Minion));
            Assert.AreEqual(103, preview.ContentSet.MinionRevisionIds.Count);
            Assert.IsTrue(preview.EntityRevisions.Any(revision => revision.StableEntityId == "BG33_830"));
            Assert.IsFalse(legacy.EntityRevisions.Any(revision => revision.StableEntityId == "BG33_830"));
        }
    }
}
