using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class ContentPackageProtocolTests
    {
        [Test]
        public void StandaloneContentManifestUrl_IsResolvedBesideThePlayerDataDirectory()
        {
            var dataPath = Path.Combine("D:\\Builds", "Learn Heartstone_Data");

            var manifestUrl = LearnHearthstoneBootstrap.ResolveStandaloneContentManifestUrl(dataPath);

            Assert.AreEqual(
                new Uri(Path.GetFullPath(Path.Combine("D:\\Builds", "content", "content-manifest.json"))).AbsoluteUri,
                manifestUrl);
        }

        private const string ClientVersion = "0.1.0-alpha";
        private const string ContentVersion = "20260727";
        private const string ValidSha256 = "44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a";
        private static readonly byte[] ValidContent = Encoding.UTF8.GetBytes("{}");

        [Test]
        public void ValidV1Package_ReturnsStrictUtf8Content()
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
            var wrongProtocol = ContentPackageValidator.ParseManifest(Manifest(protocolVersion: 99));
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

        [Test]
        public void ValidV2Package_ValidatesAllFilesAndStableFingerprint()
        {
            var files = ContentPackageV2TestData.CreateMinimalFiles(ContentVersion);
            var manifest = ContentPackageValidator.ParseManifest(
                ContentPackageV2TestData.CreateManifestBytes(files, ContentVersion, "snapshot-20260727"));

            var jsonFiles = ContentPackageValidator.Validate(manifest, files, ClientVersion);

            Assert.AreEqual(ContentPackageValidator.SupportedProtocolVersion, manifest.ProtocolVersion);
            Assert.AreEqual("snapshot-20260727", manifest.SnapshotId);
            Assert.AreEqual(GameVersionIds.LegacyCompositeSandbox, manifest.GameVersionId);
            Assert.AreEqual(RulesetIds.LegacyCompositeSandbox, manifest.RulesetId);
            Assert.AreEqual(files.Count, manifest.Files.Count);
            Assert.AreEqual(files.Count, jsonFiles.Count);
            Assert.AreEqual(
                ContentPackageValidator.ComputePackageFingerprint(manifest.Files),
                manifest.PackageFingerprint);
        }

        [Test]
        public void V2Validation_RejectsMissingFileSchemaMismatchAndWrongReference()
        {
            var files = ContentPackageV2TestData.CreateMinimalFiles(ContentVersion);
            var manifest = ContentPackageValidator.ParseManifest(
                ContentPackageV2TestData.CreateManifestBytes(files, ContentVersion, "snapshot-20260727"));
            var missing = ContentPackageV2TestData.Clone(files);
            missing.Remove("heroes.v20260727.json");

            var wrongSchema = ContentPackageV2TestData.Clone(files);
            wrongSchema["heroes.v20260727.json"] = Encoding.UTF8.GetBytes("{\"schemaVersion\":2,\"heroes\":[]}");
            var wrongSchemaManifest = ContentPackageValidator.ParseManifest(
                ContentPackageV2TestData.CreateManifestBytes(wrongSchema, ContentVersion, "snapshot-20260727", schemaVersionOverrides: new Dictionary<string, int>
                {
                    ["heroes.v20260727.json"] = 1
                }));

            var wrongReference = ContentPackageV2TestData.Clone(files);
            wrongReference["versions.v20260727.json"] = Encoding.UTF8.GetBytes(
                "{\"schemaVersion\":1,\"versions\":[{\"id\":\"legacy-composite-sandbox-v1\",\"rulesetId\":\"wrong-ruleset\",\"contentSetId\":\"content-legacy-composite-v1\"}]}");
            var wrongReferenceManifest = ContentPackageValidator.ParseManifest(
                ContentPackageV2TestData.CreateManifestBytes(wrongReference, ContentVersion, "snapshot-20260727"));

            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.Validate(manifest, missing, ClientVersion));
            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.Validate(wrongSchemaManifest, wrongSchema, ClientVersion));
            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.Validate(wrongReferenceManifest, wrongReference, ClientVersion));
        }

        [Test]
        public void V2Validation_RejectsWrongPackageFingerprintAndIgnoresManifestFileOrder()
        {
            var files = ContentPackageV2TestData.CreateMinimalFiles(ContentVersion);
            var first = ContentPackageValidator.ParseManifest(
                ContentPackageV2TestData.CreateManifestBytes(files, ContentVersion, "snapshot-20260727"));
            var reversed = ContentPackageValidator.ParseManifest(
                ContentPackageV2TestData.CreateManifestBytes(files, ContentVersion, "snapshot-20260727", reverseFileOrder: true));
            var wrongFingerprint = ContentPackageValidator.ParseManifest(
                ContentPackageV2TestData.CreateManifestBytes(files, ContentVersion, "snapshot-20260727", packageFingerprintOverride: new string('0', 64)));

            Assert.AreEqual(first.PackageFingerprint, reversed.PackageFingerprint);
            Assert.AreEqual(
                ContentPackageValidator.ComputePackageFingerprint(first.Files),
                ContentPackageValidator.ComputePackageFingerprint(reversed.Files));
            Assert.Throws<InvalidDataException>(() => ContentPackageValidator.Validate(wrongFingerprint, files, ClientVersion));
        }

        private static byte[] Manifest(
            int protocolVersion = ContentPackageValidator.LegacyProtocolVersion,
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

    internal static class ContentPackageV2TestData
    {
        private const string ClientVersion = "0.1.0-alpha";

        public static Dictionary<string, byte[]> CreateMinimalFiles(string contentVersion)
        {
            return new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["versions.v" + contentVersion + ".json"] = Encoding.UTF8.GetBytes(
                    "{\"schemaVersion\":1,\"versions\":[{\"id\":\"legacy-composite-sandbox-v1\",\"displayName\":\"Legacy Composite Sandbox\",\"releaseDateUtc\":\"1970-01-01T00:00:00Z\",\"officialStatus\":\"Unofficial\",\"implementationStatus\":\"Verified\",\"rulesetId\":\"ruleset-legacy-composite-v1\",\"contentSetId\":\"content-legacy-composite-v1\"}],\"contentSets\":[{\"id\":\"content-legacy-composite-v1\",\"includeAllDarkGiftRevisions\":false,\"poolMembership\":[]}]}"),
                ["rulesets.v" + contentVersion + ".json"] = Encoding.UTF8.GetBytes(
                    "{\"schemaVersion\":1,\"rulesets\":[{\"id\":\"ruleset-legacy-composite-v1\",\"schemaVersion\":1,\"venomousEffectRevision\":\"keyword.venomous@legacy-single-use\"}]}"),
                ["heroes.v" + contentVersion + ".json"] = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"heroes\":[]}"),
                ["minions.v" + contentVersion + ".json"] = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"minions\":[]}"),
                ["tavernSpells.v" + contentVersion + ".json"] = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"spells\":[]}"),
                ["trinkets.v" + contentVersion + ".json"] = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"trinkets\":[]}"),
                ["darkGifts.v" + contentVersion + ".json"] = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"darkGifts\":[]}"),
                ["localizations.v" + contentVersion + ".json"] = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"cards\":[]}"),
                ["assetMap.v" + contentVersion + ".json"] = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"assets\":[]}")
            };
        }

        public static Dictionary<string, byte[]> CreateBuiltInFiles(string contentVersion)
        {
            var files = CreateMinimalFiles(contentVersion);
            AddResource(files, "heroes", "battlegroundsHeroes", contentVersion);
            AddResource(files, "minions", "battlegroundsMinions", contentVersion);
            AddResource(files, "tavernSpells", "battlegroundsSpells", contentVersion);
            AddResource(files, "trinkets", "battlegroundsTrinkets", contentVersion);
            AddResource(files, "darkGifts", "battlegroundsDarkGifts", contentVersion);
            AddResource(files, "versions", "battlegroundsGameVersions", contentVersion);
            AddResource(files, "rulesets", "battlegroundsRulesets", contentVersion);
            AddResource(files, "quests", "battlegroundsQuests", contentVersion);
            AddResource(files, "anomalies", "battlegroundsAnomalies", contentVersion);
            AddResource(files, "timewarpedTavern", "timewarpedTavernCards", contentVersion);
            AddResource(files, "darkmoonPrizes", "darkmoonPrizes", contentVersion);
            AddResource(files, "heroLocalizationZhCN", "battlegroundsHeroLocalizationZhCN", contentVersion);
            AddResource(files, "questLocalizationZhCN", "battlegroundsQuestLocalizationZhCN", contentVersion);
            AddResource(files, "trinketLocalizationZhCN", "battlegroundsTrinketLocalizationZhCN", contentVersion);
            AddResource(files, "anomalyLocalizationZhCN", "battlegroundsAnomalyLocalizationZhCN", contentVersion);
            AddResource(files, "darkmoonPrizeLocalizationZhCN", "darkmoonPrizeLocalizationZhCN", contentVersion);
            AddResource(files, "darkGiftLocalizationZhCN", "battlegroundsDarkGiftLocalizationZhCN", contentVersion);
            files.Remove("localizations.v" + contentVersion + ".json");
            return files;
        }

        public static byte[] CreateManifestBytes(
            IReadOnlyDictionary<string, byte[]> files,
            string contentVersion,
            string snapshotId,
            IDictionary<string, int> schemaVersionOverrides = null,
            string packageFingerprintOverride = null,
            bool reverseFileOrder = false)
        {
            var entries = files.Select(pair => new TestFileEntry(
                    KindFor(pair.Key),
                    pair.Key,
                    schemaVersionOverrides != null && schemaVersionOverrides.TryGetValue(pair.Key, out var schemaVersion) ? schemaVersion : 1,
                    pair.Value.LongLength,
                    Sha256(pair.Value)))
                .OrderBy(entry => entry.Kind, StringComparer.Ordinal)
                .ThenBy(entry => entry.FileName, StringComparer.Ordinal)
                .ToList();
            var fingerprint = packageFingerprintOverride ?? Fingerprint(entries);
            if (reverseFileOrder)
            {
                entries.Reverse();
            }

            var builder = new StringBuilder();
            builder.Append("{\"protocolVersion\":2,");
            builder.Append("\"contentVersion\":\"").Append(contentVersion).Append("\",");
            builder.Append("\"snapshotId\":\"").Append(snapshotId).Append("\",");
            builder.Append("\"gameVersionId\":\"").Append(GameVersionIds.LegacyCompositeSandbox).Append("\",");
            builder.Append("\"rulesetId\":\"").Append(RulesetIds.LegacyCompositeSandbox).Append("\",");
            builder.Append("\"minClientVersion\":\"").Append(ClientVersion).Append("\",");
            builder.Append("\"maxClientVersion\":\"").Append(ClientVersion).Append("\",");
            builder.Append("\"generatedAtUtc\":\"2026-08-01T00:00:00.000Z\",");
            builder.Append("\"files\":[");
            for (var index = 0; index < entries.Count; index += 1)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }
                var entry = entries[index];
                builder.Append("{\"kind\":\"").Append(entry.Kind)
                    .Append("\",\"fileName\":\"").Append(entry.FileName)
                    .Append("\",\"schemaVersion\":").Append(entry.SchemaVersion)
                    .Append(",\"bytes\":").Append(entry.Bytes)
                    .Append(",\"sha256\":\"").Append(entry.Sha256).Append("\"}");
            }
            builder.Append("],\"packageFingerprint\":\"").Append(fingerprint).Append("\"}");
            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        public static Dictionary<string, byte[]> Clone(IReadOnlyDictionary<string, byte[]> files)
        {
            return files.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone(), StringComparer.Ordinal);
        }

        private static void AddResource(Dictionary<string, byte[]> files, string outputName, string resourceName, string contentVersion)
        {
            var asset = Resources.Load<TextAsset>("Data/" + resourceName);
            Assert.IsNotNull(asset, resourceName);
            files[outputName + ".v" + contentVersion + ".json"] = EnsureSchemaVersion(asset.bytes);
        }

        private static byte[] EnsureSchemaVersion(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);
            if (json.Contains("\"schemaVersion\""))
            {
                return bytes;
            }

            var objectStart = json.IndexOf('{');
            Assert.GreaterOrEqual(objectStart, 0);
            return Encoding.UTF8.GetBytes(json.Insert(objectStart + 1, "\"schemaVersion\":1,"));
        }

        private static string KindFor(string fileName)
        {
            if (fileName.StartsWith("versions.", StringComparison.Ordinal)) return "versions";
            if (fileName.StartsWith("rulesets.", StringComparison.Ordinal)) return "rulesets";
            if (fileName.StartsWith("heroes.", StringComparison.Ordinal)) return "heroes";
            if (fileName.StartsWith("minions.", StringComparison.Ordinal)) return "minions";
            if (fileName.StartsWith("tavernSpells.", StringComparison.Ordinal)) return "tavern-spells";
            if (fileName.StartsWith("trinkets.", StringComparison.Ordinal)) return "trinkets";
            if (fileName.StartsWith("darkGifts.", StringComparison.Ordinal)) return "dark-gifts";
            if (fileName.StartsWith("assetMap.", StringComparison.Ordinal)) return "asset-map";
            if (fileName.IndexOf("Localization", StringComparison.Ordinal) >= 0 || fileName.StartsWith("localizations.", StringComparison.Ordinal)) return "localizations";
            if (fileName.StartsWith("quests.", StringComparison.Ordinal)) return "quests";
            if (fileName.StartsWith("anomalies.", StringComparison.Ordinal)) return "anomalies";
            if (fileName.StartsWith("timewarpedTavern.", StringComparison.Ordinal)) return "timewarped-tavern";
            if (fileName.StartsWith("darkmoonPrizes.", StringComparison.Ordinal)) return "darkmoon-prizes";
            throw new InvalidOperationException("Unknown test content file: " + fileName);
        }

        private static string Fingerprint(IEnumerable<TestFileEntry> entries)
        {
            var canonical = string.Join("\n", entries
                .OrderBy(entry => entry.Kind, StringComparer.Ordinal)
                .ThenBy(entry => entry.FileName, StringComparer.Ordinal)
                .Select(entry => entry.Kind + "|" + entry.FileName + "|" + entry.SchemaVersion + "|" + entry.Bytes + "|" + entry.Sha256));
            return Sha256(Encoding.UTF8.GetBytes(canonical));
        }

        private static string Sha256(byte[] bytes)
        {
            using (var hash = SHA256.Create())
            {
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private sealed class TestFileEntry
        {
            public TestFileEntry(string kind, string fileName, int schemaVersion, long bytes, string sha256)
            {
                Kind = kind;
                FileName = fileName;
                SchemaVersion = schemaVersion;
                Bytes = bytes;
                Sha256 = sha256;
            }

            public string Kind { get; }
            public string FileName { get; }
            public int SchemaVersion { get; }
            public long Bytes { get; }
            public string Sha256 { get; }
        }
    }
}
