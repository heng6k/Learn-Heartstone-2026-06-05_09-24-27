using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Content;
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
