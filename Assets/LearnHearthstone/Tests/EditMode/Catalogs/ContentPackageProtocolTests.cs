using System.IO;
using System.Text;
using LearnHearthstone.Adapters.Content;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class ContentPackageProtocolTests
    {
        private const string ClientVersion = "0.1.0-alpha";
        private const string ContentVersion = "20260727";
        private const string ValidSha256 = "44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a";
        private static readonly byte[] ValidContent = Encoding.UTF8.GetBytes("{}");

        [Test]
        public void ValidPackage_ReturnsStrictUtf8Content()
        {
            var manifest = ContentPackageValidator.ParseManifest(Manifest());

            var json = ContentPackageValidator.Validate(manifest, ValidContent, ClientVersion);

            Assert.AreEqual("{}", json);
            Assert.AreEqual(ContentVersion, manifest.ContentVersion);
            Assert.AreEqual("battlegroundsMinions.v20260727.json", manifest.Minions.FileName);
        }

        [Test]
        public void Validation_RejectsProtocolAndClientMismatch()
        {
            var wrongProtocol = ContentPackageValidator.ParseManifest(Manifest(protocolVersion: 2));
            var wrongClient = ContentPackageValidator.ParseManifest(Manifest(requiredClientVersion: "0.2.0"));

            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.Validate(wrongProtocol, ValidContent, ClientVersion));
            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.Validate(wrongClient, ValidContent, ClientVersion));
        }

        [Test]
        public void Validation_RejectsUnsafeVersionAndFileName()
        {
            var unsafeVersion = ContentPackageValidator.ParseManifest(Manifest(contentVersion: "../20260727"));
            var wrongFileName = ContentPackageValidator.ParseManifest(Manifest(fileName: "../battlegroundsMinions.v20260727.json"));

            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.Validate(unsafeVersion, ValidContent, ClientVersion));
            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.Validate(wrongFileName, ValidContent, ClientVersion));
        }

        [Test]
        public void Validation_RejectsByteAndHashMismatch()
        {
            var wrongBytes = ContentPackageValidator.ParseManifest(Manifest(bytes: 3));
            var wrongHash = ContentPackageValidator.ParseManifest(Manifest(sha256: new string('0', 64)));

            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.Validate(wrongBytes, ValidContent, ClientVersion));
            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.Validate(wrongHash, ValidContent, ClientVersion));
        }

        [Test]
        public void Parsing_RejectsOversizedAndInvalidUtf8Manifest()
        {
            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.ParseManifest(new byte[ContentPackageValidator.MaxManifestBytes + 1]));
            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.ParseManifest(new byte[] { 0xff }));
        }

        private static byte[] Manifest(
            int protocolVersion = ContentPackageValidator.SupportedProtocolVersion,
            string contentVersion = ContentVersion,
            string requiredClientVersion = ClientVersion,
            string fileName = "battlegroundsMinions.v20260727.json",
            long bytes = 2,
            string sha256 = ValidSha256)
        {
            var json = "{" +
                       "\"protocolVersion\":" + protocolVersion + "," +
                       "\"contentVersion\":\"" + contentVersion + "\"," +
                       "\"requiredClientVersion\":\"" + requiredClientVersion + "\"," +
                       "\"generatedAtUtc\":\"2026-07-28T00:00:00.000Z\"," +
                       "\"minions\":{" +
                       "\"fileName\":\"" + fileName + "\"," +
                       "\"bytes\":" + bytes + "," +
                       "\"sha256\":\"" + sha256 + "\"}}";
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
