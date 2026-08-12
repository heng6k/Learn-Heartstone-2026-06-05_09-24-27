using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14MinionContentCatalogTests
    {
        [Test]
        public void EmbeddedCatalog_LoadsUniquePreviewMinionsWithCompleteLiveSeason14Identities()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var chinese = snapshot.Chinese.Minions.All.Where(item => !string.IsNullOrWhiteSpace(item.ResearchKey)).ToList();
            var english = snapshot.English.Minions.All.Where(item => !string.IsNullOrWhiteSpace(item.ResearchKey)).ToList();

            Assert.AreEqual(94, chinese.Count);
            Assert.AreEqual(55, chinese.Count(item => item.ResearchKey.StartsWith("MIN-R")));
            Assert.AreEqual(39, chinese.Count(item => !item.ResearchKey.StartsWith("MIN-R")));
            Assert.AreEqual(94, chinese.Select(item => item.CardId).Distinct().Count());
            Assert.AreEqual(94, chinese.Select(item => item.RevisionId).Distinct().Count());
            Assert.IsTrue(chinese.All(item => !item.InPool));
            Assert.AreEqual(0, english.Count(item => item.Name.StartsWith("[Missing en-US:")));
            Assert.AreEqual("Air Baller", english.Single(item => item.ResearchKey == "MIN-R14").Name);
            Assert.AreEqual("空气投球手", chinese.Single(item => item.ResearchKey == "MIN-R14").Name);
            Assert.AreEqual(133455, chinese.Single(item => item.ResearchKey == "MIN-R14").DbfId);
            Assert.AreEqual("Tyrael", english.Single(item => item.ResearchKey == "MIN-R55").Name);

            var newSeason14Minions = english
                .Where(item => item.ResearchKey.StartsWith("MIN-R") ||
                               item.ResearchKey.StartsWith("ACT-R") ||
                               item.ResearchKey.StartsWith("LOCK-R") ||
                               item.ResearchKey.StartsWith("FISH-R"))
                .ToList();
            Assert.AreEqual(63, newSeason14Minions.Count);
            Assert.IsTrue(newSeason14Minions.All(item => item.DbfId > 0));
            Assert.IsTrue(newSeason14Minions.All(item => item.Golden?.DbfId > 0));
            Assert.IsTrue(newSeason14Minions.All(item => !item.CardId.StartsWith("preview-")));
            Assert.IsTrue(newSeason14Minions.All(item => !item.Golden.CardId.StartsWith("preview-")));
            Assert.AreEqual("BG36_511", newSeason14Minions.Single(item => item.ResearchKey == "MIN-R01").CardId);
            Assert.AreEqual("BG36_181", newSeason14Minions.Single(item => item.ResearchKey == "MIN-R14").CardId);
            Assert.AreEqual("BG36_853", newSeason14Minions.Single(item => item.ResearchKey == "MIN-R28").CardId);
            Assert.AreEqual("BG36_762", newSeason14Minions.Single(item => item.ResearchKey == "MIN-R38").CardId);
            Assert.AreEqual("BG36_356_G", newSeason14Minions.Single(item => item.ResearchKey == "MIN-R55").Golden.CardId);
            Assert.IsTrue(newSeason14Minions.All(item => item.ImplementationStatus == "Implemented"));
            Assert.IsTrue(newSeason14Minions.All(item => !string.IsNullOrWhiteSpace(item.EffectRevision)));
            Assert.IsTrue(chinese.All(item => !string.IsNullOrWhiteSpace(item.ImagePath)), "Every 36.2 new or returned minion must have a local image path.");
            Assert.IsTrue(chinese.All(item => Resources.Load<Texture2D>(item.ImagePath) != null), "Every 36.2 new or returned minion image must be imported as a local Unity resource.");
        }

        [Test]
        public void MechanicCarrierDefinitions_KeepOfficialNormalGoldenPairsAndPreviewIds()
        {
            var catalog = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").English.Minions;
            var kelpKeeper = catalog.All.Single(item => item.ResearchKey == "ACT-R01N");
            var bilgewater = catalog.All.Single(item => item.ResearchKey == "LOCK-R02N");
            var fishbait = catalog.All.Single(item => item.ResearchKey == "FISH-R01N");

            Assert.AreEqual("BG36_701", kelpKeeper.CardId);
            Assert.AreEqual(132883, kelpKeeper.DbfId);
            Assert.AreEqual(4, kelpKeeper.TavernTier);
            Assert.AreEqual("BG36_701_G", kelpKeeper.Golden.CardId);
            Assert.AreEqual(10, kelpKeeper.Golden.BaseAttack);
            Assert.AreEqual("BG36_520_G", bilgewater.Golden.CardId);
            Assert.AreEqual("BG36_205_G", fishbait.Golden.CardId);
            Assert.AreEqual(0, fishbait.BaseAttack);
            Assert.AreEqual(1, fishbait.BaseHealth);
            Assert.Contains(Keyword.Deathrattle, fishbait.Keywords);
        }

        [Test]
        public void PreviewVersion_IndexesAllMinionRevisionsAndEnablesCompletedDefinitions()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);

            Assert.AreEqual(103, resolved.EntityRevisions.Count(item => item.Kind == EntityKind.Minion));
            Assert.AreEqual(103, resolved.ContentSet.MinionRevisionIds.Count);
            Assert.IsTrue(resolved.Snapshot.English.Minions.All
                .Where(item => !string.IsNullOrWhiteSpace(item.ResearchKey))
                .All(item => item.InPool));
        }
    }
}
