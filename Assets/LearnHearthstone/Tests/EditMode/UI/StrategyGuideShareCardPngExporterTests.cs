using System;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.MainHub;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StrategyGuideShareCardPngExporterTests
    {
        [Test]
        public void ExportPngWritesProfileSpecificFilenamesWithoutOverwriting()
        {
            var directory = Path.Combine(
                UnityEngine.Application.temporaryCachePath,
                "strategy-guide-share-card-" + Guid.NewGuid().ToString("N"));
            Texture2D decoded = null;
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = catalog.Guides[0];
                var beginner = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.GuidedDiscover);
                var hard = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.OpenBuild);
                var beginnerModel = StrategyGuideShareCardService.Create(
                    catalog,
                    guide.GuideId,
                    beginner.ProfileId,
                    version,
                    snapshot.ForLanguage(false),
                    false);
                var hardModel = StrategyGuideShareCardService.Create(
                    catalog,
                    guide.GuideId,
                    hard.ProfileId,
                    version,
                    snapshot.ForLanguage(false),
                    false);

                var beginnerResult = StrategyGuideShareCardPngExporter.Export(beginnerModel, false, directory);
                var hardResult = StrategyGuideShareCardPngExporter.Export(hardModel, false, directory);

                Assert.AreNotEqual(beginnerResult.Path, hardResult.Path);
                Assert.IsTrue(File.Exists(beginnerResult.Path));
                Assert.IsTrue(File.Exists(hardResult.Path));
                Assert.AreEqual(1600, beginnerResult.Width);
                Assert.AreEqual(900, beginnerResult.Height);
                Assert.AreEqual(beginnerModel.ContentHash, beginnerResult.ContentHash);
                StringAssert.Contains(beginnerModel.GuideId, Path.GetFileName(beginnerResult.Path));
                StringAssert.Contains("_" + beginnerModel.ProfileId + "_", Path.GetFileName(beginnerResult.Path));
                StringAssert.Contains(beginnerModel.RevisionId, Path.GetFileName(beginnerResult.Path));
                StringAssert.Contains(beginnerModel.ContentHashShort, Path.GetFileName(beginnerResult.Path));
                StringAssert.Contains("_" + hardModel.ProfileId + "_", Path.GetFileName(hardResult.Path));
                CollectionAssert.AreEqual(
                    new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a },
                    File.ReadAllBytes(beginnerResult.Path).Take(8).ToArray());

                decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.IsTrue(decoded.LoadImage(File.ReadAllBytes(beginnerResult.Path)));
                Assert.AreEqual(1600, decoded.width);
                Assert.AreEqual(900, decoded.height);
                Assert.Greater(new FileInfo(beginnerResult.Path).Length, 4096);
                Assert.Greater(new FileInfo(hardResult.Path).Length, 4096);
            }
            finally
            {
                if (decoded != null)
                {
                    UnityEngine.Object.DestroyImmediate(decoded);
                }
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void BuildFileNameRejectsMissingProfileIdentity()
        {
            var model = new StrategyGuideShareCardModel
            {
                GuideId = "guide",
                RevisionId = "revision",
                ContentHashShort = "1234567890ab"
            };

            Assert.Throws<InvalidOperationException>(() =>
                StrategyGuideShareCardPngExporter.BuildFileName(model));
        }
    }
}
