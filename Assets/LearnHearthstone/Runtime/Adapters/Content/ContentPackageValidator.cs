using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LearnHearthstone.Application.Content;
using UnityEngine;

namespace LearnHearthstone.Adapters.Content
{
    public static class ContentPackageValidator
    {
        public const int LegacyProtocolVersion = 1;
        public const int SupportedProtocolVersion = 2;
        public const int MaxManifestBytes = 64 * 1024;
        public const int MaxContentBytes = 16 * 1024 * 1024;

        private const string MinionFilePrefix = "battlegroundsMinions.v";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly string[] RequiredV2Kinds =
        {
            "versions",
            "rulesets",
            "heroes",
            "minions",
            "tavern-spells",
            "trinkets",
            "dark-gifts",
            "localizations",
            "asset-map"
        };

        public static ContentPackageManifest ParseManifest(byte[] manifestBytes)
        {
            if (manifestBytes == null || manifestBytes.Length == 0 || manifestBytes.Length > MaxManifestBytes)
            {
                throw Invalid("manifest size is outside the supported range");
            }

            var json = DecodeStrictJson(manifestBytes, "manifest");
            RawManifest raw;
            try
            {
                raw = JsonUtility.FromJson<RawManifest>(json);
            }
            catch (ArgumentException exception)
            {
                throw Invalid("manifest JSON is invalid", exception);
            }

            if (raw == null)
            {
                throw Invalid("manifest JSON is invalid");
            }

            if (raw.files != null && raw.files.Length > 0)
            {
                return new ContentPackageManifest(
                    raw.protocolVersion,
                    raw.contentVersion,
                    raw.snapshotId,
                    raw.gameVersionId,
                    raw.rulesetId,
                    raw.minClientVersion,
                    raw.maxClientVersion,
                    raw.generatedAtUtc,
                    raw.files.Select(file => new ContentPackageFile(
                        file == null ? null : file.kind,
                        file == null ? null : file.fileName,
                        file == null ? 0 : file.schemaVersion,
                        file == null ? 0 : file.bytes,
                        file == null ? null : file.sha256)),
                    raw.packageFingerprint);
            }

            if (raw.minions == null)
            {
                throw Invalid("manifest is missing content file metadata");
            }

            return new ContentPackageManifest(
                raw.protocolVersion,
                raw.contentVersion,
                raw.requiredClientVersion,
                raw.generatedAtUtc,
                new ContentPackageFile(raw.minions.fileName, raw.minions.bytes, raw.minions.sha256));
        }

        public static void ValidateManifest(ContentPackageManifest manifest, string clientVersion)
        {
            if (manifest == null)
            {
                throw Invalid("manifest is required");
            }
            if (manifest.ProtocolVersion == LegacyProtocolVersion)
            {
                ValidateLegacyManifest(manifest, clientVersion);
                return;
            }
            if (manifest.ProtocolVersion != SupportedProtocolVersion)
            {
                throw Invalid("protocol version is not supported");
            }

            ValidateV2Manifest(manifest, clientVersion);
        }

        public static string Validate(ContentPackageManifest manifest, byte[] contentBytes, string clientVersion)
        {
            ValidateManifest(manifest, clientVersion);
            if (manifest.ProtocolVersion != LegacyProtocolVersion || manifest.Minions == null || manifest.Files.Count != 1)
            {
                throw Invalid("single-file validation only supports protocol v1");
            }

            var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [manifest.Minions.FileName] = contentBytes
            };
            return Validate(manifest, files, clientVersion)[manifest.Minions.FileName];
        }

        public static IReadOnlyDictionary<string, string> Validate(
            ContentPackageManifest manifest,
            IReadOnlyDictionary<string, byte[]> files,
            string clientVersion)
        {
            ValidateManifest(manifest, clientVersion);
            if (files == null)
            {
                throw Invalid("content files are required");
            }
            if (files.Count != manifest.Files.Count)
            {
                throw Invalid("content file set does not match the manifest");
            }

            var jsonFiles = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var file in manifest.Files)
            {
                if (!files.TryGetValue(file.FileName, out var bytes) || bytes == null)
                {
                    throw Invalid("content file is missing: " + file.FileName);
                }
                if (bytes.LongLength != file.Bytes)
                {
                    throw Invalid("content byte count does not match: " + file.FileName);
                }
                if (!string.Equals(Sha256(bytes), file.Sha256, StringComparison.Ordinal))
                {
                    throw Invalid("content SHA-256 does not match: " + file.FileName);
                }

                var json = DecodeStrictJson(bytes, "content file " + file.FileName);
                if (manifest.ProtocolVersion == SupportedProtocolVersion)
                {
                    ValidateSchemaVersion(file, json);
                }
                jsonFiles.Add(file.FileName, json);
            }

            if (manifest.ProtocolVersion == SupportedProtocolVersion)
            {
                ValidateCrossFileReferences(manifest, jsonFiles);
            }
            return new ReadOnlyDictionary<string, string>(jsonFiles);
        }

        public static string ComputePackageFingerprint(IEnumerable<ContentPackageFile> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            var canonical = string.Join("\n", files
                .OrderBy(file => file.Kind, StringComparer.Ordinal)
                .ThenBy(file => file.FileName, StringComparer.Ordinal)
                .Select(file => string.Join("|", new[]
                {
                    file.Kind,
                    file.FileName,
                    file.SchemaVersion.ToString(CultureInfo.InvariantCulture),
                    file.Bytes.ToString(CultureInfo.InvariantCulture),
                    file.Sha256
                })));
            return Sha256(Encoding.UTF8.GetBytes(canonical));
        }

        private static void ValidateLegacyManifest(ContentPackageManifest manifest, string clientVersion)
        {
            if (!IsSafeToken(manifest.ContentVersion))
            {
                throw Invalid("content version is unsafe");
            }
            if (string.IsNullOrWhiteSpace(clientVersion) ||
                !string.Equals(manifest.RequiredClientVersion, clientVersion.Trim(), StringComparison.Ordinal))
            {
                throw Invalid("required client version does not match");
            }
            ValidateGeneratedAt(manifest.GeneratedAtUtc);
            if (manifest.Minions == null)
            {
                throw Invalid("minions metadata is required");
            }

            var expectedFileName = MinionFilePrefix + manifest.ContentVersion + ".json";
            if (!string.Equals(manifest.Minions.FileName, expectedFileName, StringComparison.Ordinal))
            {
                throw Invalid("minions file name does not match the content version");
            }
            ValidateFileMetadata(manifest.Minions, requireSchema: false);
        }

        private static void ValidateV2Manifest(ContentPackageManifest manifest, string clientVersion)
        {
            if (!IsSafeToken(manifest.ContentVersion))
            {
                throw Invalid("content version is unsafe");
            }
            if (!IsSafeToken(manifest.SnapshotId))
            {
                throw Invalid("snapshot id is unsafe");
            }
            if (!IsSafeToken(manifest.GameVersionId))
            {
                throw Invalid("game version id is unsafe");
            }
            if (!IsSafeToken(manifest.RulesetId))
            {
                throw Invalid("ruleset id is unsafe");
            }
            if (!IsClientVersionSupported(clientVersion, manifest.MinClientVersion, manifest.MaxClientVersion))
            {
                throw Invalid("client version is outside the supported range");
            }
            ValidateGeneratedAt(manifest.GeneratedAtUtc);
            if (manifest.Files == null || manifest.Files.Count == 0)
            {
                throw Invalid("content file table is required");
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in manifest.Files)
            {
                ValidateFileMetadata(file, requireSchema: true);
                if (!names.Add(file.FileName))
                {
                    throw Invalid("duplicate content file name: " + file.FileName);
                }
            }

            foreach (var kind in RequiredV2Kinds)
            {
                var count = manifest.Files.Count(file => string.Equals(file.Kind, kind, StringComparison.Ordinal));
                if (count == 0)
                {
                    throw Invalid("content file kind is missing: " + kind);
                }
                if (!string.Equals(kind, "localizations", StringComparison.Ordinal) && count != 1)
                {
                    throw Invalid("content file kind must appear exactly once: " + kind);
                }
            }

            if (!IsLowerHexSha256(manifest.PackageFingerprint))
            {
                throw Invalid("package fingerprint is malformed");
            }
            if (!string.Equals(
                    ComputePackageFingerprint(manifest.Files),
                    manifest.PackageFingerprint,
                    StringComparison.Ordinal))
            {
                throw Invalid("package fingerprint does not match");
            }
        }

        private static void ValidateFileMetadata(ContentPackageFile file, bool requireSchema)
        {
            if (file == null)
            {
                throw Invalid("content file metadata is required");
            }
            if (!IsSafeToken(file.Kind))
            {
                throw Invalid("content file kind is unsafe");
            }
            if (!IsSafeFileName(file.FileName))
            {
                throw Invalid("content file name is unsafe");
            }
            if (requireSchema && file.SchemaVersion <= 0)
            {
                throw Invalid("content schema version is invalid: " + file.FileName);
            }
            if (file.Bytes <= 0 || file.Bytes > MaxContentBytes)
            {
                throw Invalid("content byte count is outside the supported range: " + file.FileName);
            }
            if (!IsLowerHexSha256(file.Sha256))
            {
                throw Invalid("content SHA-256 is malformed: " + file.FileName);
            }
        }

        private static void ValidateSchemaVersion(ContentPackageFile file, string json)
        {
            RawSchemaEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<RawSchemaEnvelope>(json);
            }
            catch (ArgumentException exception)
            {
                throw Invalid("content JSON is invalid: " + file.FileName, exception);
            }
            if (envelope == null || envelope.schemaVersion != file.SchemaVersion)
            {
                throw Invalid("content schema version does not match: " + file.FileName);
            }
        }

        private static void ValidateCrossFileReferences(
            ContentPackageManifest manifest,
            IReadOnlyDictionary<string, string> jsonFiles)
        {
            var versionsJson = JsonForKind(manifest, jsonFiles, "versions");
            var rulesetsJson = JsonForKind(manifest, jsonFiles, "rulesets");
            RawVersionsPayload versions;
            RawRulesetsPayload rulesets;
            try
            {
                versions = JsonUtility.FromJson<RawVersionsPayload>(versionsJson);
                rulesets = JsonUtility.FromJson<RawRulesetsPayload>(rulesetsJson);
            }
            catch (ArgumentException exception)
            {
                throw Invalid("versions or rulesets JSON is invalid", exception);
            }

            var selectedVersion = versions?.versions?.FirstOrDefault(version =>
                version != null && string.Equals(version.id, manifest.GameVersionId, StringComparison.Ordinal));
            if (selectedVersion == null)
            {
                throw Invalid("game version is missing from versions file");
            }
            if (!string.Equals(selectedVersion.rulesetId, manifest.RulesetId, StringComparison.Ordinal))
            {
                throw Invalid("game version ruleset reference does not match the manifest");
            }
            if (string.IsNullOrWhiteSpace(selectedVersion.contentSetId))
            {
                throw Invalid("game version content set reference is missing");
            }
            if (rulesets?.rulesets == null || !rulesets.rulesets.Any(ruleset =>
                    ruleset != null && string.Equals(ruleset.id, manifest.RulesetId, StringComparison.Ordinal)))
            {
                throw Invalid("ruleset is missing from rulesets file");
            }
        }

        private static string JsonForKind(
            ContentPackageManifest manifest,
            IReadOnlyDictionary<string, string> jsonFiles,
            string kind)
        {
            var file = manifest.Files.Single(item => string.Equals(item.Kind, kind, StringComparison.Ordinal));
            return jsonFiles[file.FileName];
        }

        private static string DecodeStrictJson(byte[] bytes, string label)
        {
            string json;
            try
            {
                json = StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException exception)
            {
                throw Invalid(label + " is not valid UTF-8", exception);
            }
            if (json.Length > 0 && json[0] == '\uFEFF')
            {
                throw Invalid(label + " must not contain a UTF-8 BOM");
            }
            return json;
        }

        private static void ValidateGeneratedAt(string generatedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(generatedAtUtc) ||
                !generatedAtUtc.EndsWith("Z", StringComparison.Ordinal) ||
                !DateTimeOffset.TryParse(
                    generatedAtUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var generatedAt) ||
                generatedAt.Offset != TimeSpan.Zero)
            {
                throw Invalid("generatedAtUtc must be a UTC timestamp");
            }
        }

        private static bool IsClientVersionSupported(string clientVersion, string minimum, string maximum)
        {
            if (string.IsNullOrWhiteSpace(clientVersion) ||
                string.IsNullOrWhiteSpace(minimum) ||
                string.IsNullOrWhiteSpace(maximum))
            {
                return false;
            }
            var normalized = clientVersion.Trim();
            return CompareClientVersions(normalized, minimum) >= 0 &&
                   CompareClientVersions(normalized, maximum) <= 0;
        }

        private static int CompareClientVersions(string left, string right)
        {
            if (!TryParseClientVersion(left, out var leftCore, out var leftPrerelease) ||
                !TryParseClientVersion(right, out var rightCore, out var rightPrerelease))
            {
                return string.Compare(left, right, StringComparison.Ordinal);
            }

            var count = Math.Max(leftCore.Length, rightCore.Length);
            for (var index = 0; index < count; index += 1)
            {
                var leftPart = index < leftCore.Length ? leftCore[index] : 0;
                var rightPart = index < rightCore.Length ? rightCore[index] : 0;
                var comparison = leftPart.CompareTo(rightPart);
                if (comparison != 0)
                {
                    return comparison;
                }
            }
            if (string.IsNullOrEmpty(leftPrerelease) && string.IsNullOrEmpty(rightPrerelease))
            {
                return 0;
            }
            if (string.IsNullOrEmpty(leftPrerelease))
            {
                return 1;
            }
            if (string.IsNullOrEmpty(rightPrerelease))
            {
                return -1;
            }
            return string.Compare(leftPrerelease, rightPrerelease, StringComparison.Ordinal);
        }

        private static bool TryParseClientVersion(string value, out int[] core, out string prerelease)
        {
            core = null;
            prerelease = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            var parts = value.Split(new[] { '-' }, 2);
            var coreParts = parts[0].Split('.');
            core = new int[coreParts.Length];
            for (var index = 0; index < coreParts.Length; index += 1)
            {
                if (!int.TryParse(coreParts[index], NumberStyles.None, CultureInfo.InvariantCulture, out core[index]))
                {
                    core = null;
                    return false;
                }
            }
            prerelease = parts.Length == 2 ? parts[1] : string.Empty;
            return true;
        }

        private static bool IsSafeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Contains(".."))
            {
                return false;
            }
            for (var index = 0; index < value.Length; index += 1)
            {
                var character = value[index];
                var alphaNumeric = (character >= 'A' && character <= 'Z') ||
                                   (character >= 'a' && character <= 'z') ||
                                   (character >= '0' && character <= '9');
                var safe = alphaNumeric || character == '.' || character == '_' || character == '-';
                if (!safe || (index == 0 && !alphaNumeric))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 192 || value.Contains(".."))
            {
                return false;
            }
            if (!string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal) ||
                !value.EndsWith(".json", StringComparison.Ordinal))
            {
                return false;
            }
            return IsSafeToken(value);
        }

        private static bool IsLowerHexSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
            {
                return false;
            }
            foreach (var character in value)
            {
                if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
                {
                    return false;
                }
            }
            return true;
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static InvalidDataException Invalid(string reason, Exception innerException = null)
        {
            return new InvalidDataException("Invalid content package: " + reason + ".", innerException);
        }

        [Serializable]
        private sealed class RawManifest
        {
            public int protocolVersion;
            public string contentVersion;
            public string requiredClientVersion;
            public string snapshotId;
            public string gameVersionId;
            public string rulesetId;
            public string minClientVersion;
            public string maxClientVersion;
            public string generatedAtUtc;
            public RawFile minions;
            public RawFile[] files;
            public string packageFingerprint;
        }

        [Serializable]
        private sealed class RawFile
        {
            public string kind;
            public string fileName;
            public int schemaVersion;
            public long bytes;
            public string sha256;
        }

        [Serializable]
        private sealed class RawSchemaEnvelope
        {
            public int schemaVersion;
        }

        [Serializable]
        private sealed class RawVersionsPayload
        {
            public RawVersion[] versions;
        }

        [Serializable]
        private sealed class RawVersion
        {
            public string id;
            public string rulesetId;
            public string contentSetId;
        }

        [Serializable]
        private sealed class RawRulesetsPayload
        {
            public RawRuleset[] rulesets;
        }

        [Serializable]
        private sealed class RawRuleset
        {
            public string id;
        }
    }
}
