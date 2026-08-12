using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class ContentSnapshotFallbackTests
    {
        private const string ClientVersion = "0.1.0-alpha";
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "LearnHearthstone-M4-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void Resolve_ValidRemotePromotesAndRestoresLastKnownGood()
        {
            var content = BuiltInMinionBytes();
            var repository = new LastKnownGoodContentRepository(directory);
            var resolver = new GameCatalogSnapshotResolver(ClientVersion, repository);

            var firstRemote = resolver.Resolve(Manifest("20260727", content), content);
            var secondRemote = resolver.Resolve(Manifest("20260728", content), content);
            var restored = new GameCatalogSnapshotResolver(ClientVersion, repository).Resolve();

            Assert.AreEqual(ContentSnapshotSource.Remote, firstRemote.Info.Source);
            Assert.AreEqual("20260727", firstRemote.Info.ContentVersion);
            Assert.AreEqual(ContentSnapshotSource.Remote, secondRemote.Info.Source);
            Assert.AreEqual("20260728", secondRemote.Info.ContentVersion);
            Assert.AreEqual(ContentSnapshotSource.LastKnownGood, restored.Info.Source);
            Assert.AreEqual(secondRemote.Info.ContentVersion, restored.Info.ContentVersion);
            Assert.AreEqual(secondRemote.Chinese.Minions.All.Count, restored.Chinese.Minions.All.Count);
        }

        [Test]
        public void Resolve_InvalidRemoteFallsBackToEmbeddedWithoutCreatingLkg()
        {
            var content = BuiltInMinionBytes();
            var manifest = Manifest("20260727", content);
            var corrupted = (byte[])content.Clone();
            corrupted[corrupted.Length - 1] ^= 1;
            var repository = new LastKnownGoodContentRepository(directory);

            var snapshot = new GameCatalogSnapshotResolver(ClientVersion, repository).Resolve(manifest, corrupted);

            Assert.AreEqual(ContentSnapshotSource.Embedded, snapshot.Info.Source);
            Assert.IsFalse(repository.TryRead(ClientVersion, out _, out _, out _));
        }

        [Test]
        public void Resolve_SameVersionDifferentBytesKeepsPreviousLkg()
        {
            var content = BuiltInMinionBytes();
            var repository = new LastKnownGoodContentRepository(directory);
            var resolver = new GameCatalogSnapshotResolver(ClientVersion, repository);
            resolver.Resolve(Manifest("20260727", content), content);

            var changed = new byte[content.Length + 1];
            Buffer.BlockCopy(content, 0, changed, 0, content.Length);
            changed[changed.Length - 1] = (byte)'\n';
            var snapshot = resolver.Resolve(Manifest("20260727", changed), changed);

            Assert.AreEqual(ContentSnapshotSource.LastKnownGood, snapshot.Info.Source);
            Assert.IsTrue(repository.TryRead(ClientVersion, out _, out var savedContent, out _));
            CollectionAssert.AreEqual(content, savedContent);
        }

        [Test]
        public void Resolve_UnreadableLkgFallsBackToEmbedded()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "content-manifest.json"), "not json");

            var snapshot = new GameCatalogSnapshotResolver(
                ClientVersion,
                new LastKnownGoodContentRepository(directory)).Resolve(remoteFailureReason: "offline");

            Assert.AreEqual(ContentSnapshotSource.Embedded, snapshot.Info.Source);
        }

        [Test]
        public void Resolve_ValidV2RemotePromotesImmutableSnapshotAndRestoresActive()
        {
            var repository = new LastKnownGoodContentRepository(directory);
            var resolver = new GameCatalogSnapshotResolver(ClientVersion, repository);
            var package = V2Package("snapshot-one", "20260801");

            var remote = resolver.Resolve(package);
            var restored = new GameCatalogSnapshotResolver(ClientVersion, repository).Resolve();

            Assert.AreEqual(ContentSnapshotSource.Remote, remote.Info.Source);
            Assert.AreEqual("snapshot-one", remote.Info.SnapshotId);
            Assert.AreEqual(GameVersionIds.LegacyCompositeSandbox, remote.Info.GameVersionId);
            Assert.AreEqual(ContentSnapshotSource.LastKnownGood, restored.Info.Source);
            Assert.AreEqual(remote.Info.SnapshotId, restored.Info.SnapshotId);
            Assert.AreEqual(remote.Info.ContentFingerprint, restored.Info.ContentFingerprint);
            Assert.IsTrue(File.Exists(Path.Combine(directory, "Snapshots", "snapshot-one", "content-manifest.json")));
            Assert.IsTrue(File.Exists(Path.Combine(directory, "active.json")));
        }

        [Test]
        public void Resolve_DevelopmentFallbackPrefersEmbeddedOverPersistedLkg()
        {
            var repository = new LastKnownGoodContentRepository(directory);
            var package = V2Package("p2-stale", "20260807");
            new GameCatalogSnapshotResolver(ClientVersion, repository).Resolve(package);

            var snapshot = new GameCatalogSnapshotResolver(
                ClientVersion,
                repository,
                preferEmbeddedFallback: true).Resolve(remoteFailureReason: "development uses embedded resources");

            Assert.AreEqual(ContentSnapshotSource.Embedded, snapshot.Info.Source);
            Assert.IsTrue(snapshot.VersionedContent.Versions.Versions.Any(
                version => version.Id == GameVersionIds.Season14Preview));
            Assert.IsTrue(snapshot.Chinese.Minions.All.Any(item => item.CardId == "BG36_851"));

            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(
                GameVersionIds.Season14Preview,
                snapshot);
            var guideCatalog = StrategyGuideCatalogLoader.LoadFromResources();
            foreach (var guide in guideCatalog.Guides)
            {
                var validation = StrategyGuideValidator.Validate(guideCatalog, guide, resolved);
                Assert.IsTrue(validation.IsValid, guide.GuideId + ": " + string.Join(" | ", validation.Errors));
            }
        }

        [Test]
        public void Resolve_InvalidV2ActivationKeepsPreviousActiveSnapshot()
        {
            var repository = new LastKnownGoodContentRepository(directory);
            var resolver = new GameCatalogSnapshotResolver(ClientVersion, repository);
            var first = resolver.Resolve(V2Package("snapshot-one", "20260801"));
            var second = V2Package("snapshot-two", "20260802");
            var corruptedFiles = ContentPackageV2TestData.Clone(second.Files);
            corruptedFiles["heroes.v20260802.json"][0] ^= 1;

            var fallback = resolver.Resolve(new ContentPackageDownload(second.ManifestBytes, corruptedFiles));

            Assert.AreEqual(ContentSnapshotSource.LastKnownGood, fallback.Info.Source);
            Assert.AreEqual(first.Info.SnapshotId, fallback.Info.SnapshotId);
            Assert.IsFalse(Directory.Exists(Path.Combine(directory, "Snapshots", "snapshot-two")));
            Assert.IsTrue(repository.TryReadPackage(ClientVersion, out var activeManifestBytes, out _, out _));
            Assert.AreEqual("snapshot-one", ContentPackageValidator.ParseManifest(activeManifestBytes).SnapshotId);
        }

        private static ContentPackageDownload V2Package(string snapshotId, string contentVersion)
        {
            var files = ContentPackageV2TestData.CreateBuiltInFiles(contentVersion);
            return new ContentPackageDownload(
                ContentPackageV2TestData.CreateManifestBytes(files, contentVersion, snapshotId),
                files);
        }

        private static byte[] BuiltInMinionBytes()
        {
            var asset = Resources.Load<TextAsset>("Data/battlegroundsMinions");
            Assert.IsNotNull(asset);
            return asset.bytes;
        }

        private static byte[] Manifest(string contentVersion, byte[] content)
        {
            string sha256;
            using (var hash = SHA256.Create())
            {
                sha256 = BitConverter.ToString(hash.ComputeHash(content)).Replace("-", string.Empty).ToLowerInvariant();
            }

            var json = "{" +
                       "\"protocolVersion\":1," +
                       "\"contentVersion\":\"" + contentVersion + "\"," +
                       "\"requiredClientVersion\":\"" + ClientVersion + "\"," +
                       "\"generatedAtUtc\":\"2026-07-28T00:00:00.000Z\"," +
                       "\"minions\":{" +
                       "\"fileName\":\"battlegroundsMinions.v" + contentVersion + ".json\"," +
                       "\"bytes\":" + content.Length + "," +
                       "\"sha256\":\"" + sha256 + "\"}}";
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
