using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using LearnHearthstone.Application.Content;
using UnityEngine;

namespace LearnHearthstone.Adapters.Content
{
    public static class ContentPackageValidator
    {
        public const int SupportedProtocolVersion = 1;
        public const int MaxManifestBytes = 64 * 1024;
        public const int MaxContentBytes = 16 * 1024 * 1024;

        private const string MinionFilePrefix = "battlegroundsMinions.v";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static ContentPackageManifest ParseManifest(byte[] manifestBytes)
        {
            if (manifestBytes == null || manifestBytes.Length == 0 || manifestBytes.Length > MaxManifestBytes)
            {
                throw Invalid("manifest size is outside the supported range");
            }

            string json;
            try
            {
                json = StrictUtf8.GetString(manifestBytes);
            }
            catch (DecoderFallbackException exception)
            {
                throw Invalid("manifest is not valid UTF-8", exception);
            }

            if (json.Length > 0 && json[0] == '\uFEFF')
            {
                throw Invalid("manifest must not contain a UTF-8 BOM");
            }

            RawManifest raw;
            try
            {
                raw = JsonUtility.FromJson<RawManifest>(json);
            }
            catch (ArgumentException exception)
            {
                throw Invalid("manifest JSON is invalid", exception);
            }

            if (raw == null || raw.minions == null)
            {
                throw Invalid("manifest is missing minions metadata");
            }

            return new ContentPackageManifest(
                raw.protocolVersion,
                raw.contentVersion,
                raw.requiredClientVersion,
                raw.generatedAtUtc,
                new ContentPackageFile(raw.minions.fileName, raw.minions.bytes, raw.minions.sha256));
        }

        public static string Validate(ContentPackageManifest manifest, byte[] contentBytes, string clientVersion)
        {
            if (manifest == null)
            {
                throw Invalid("manifest is required");
            }
            if (manifest.ProtocolVersion != SupportedProtocolVersion)
            {
                throw Invalid("protocol version is not supported");
            }
            if (!IsSafeContentVersion(manifest.ContentVersion))
            {
                throw Invalid("content version is unsafe");
            }
            if (string.IsNullOrWhiteSpace(clientVersion) ||
                !string.Equals(manifest.RequiredClientVersion, clientVersion.Trim(), StringComparison.Ordinal))
            {
                throw Invalid("required client version does not match");
            }
            if (string.IsNullOrWhiteSpace(manifest.GeneratedAtUtc) ||
                !manifest.GeneratedAtUtc.EndsWith("Z", StringComparison.Ordinal) ||
                !DateTimeOffset.TryParse(
                    manifest.GeneratedAtUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var generatedAt) ||
                generatedAt.Offset != TimeSpan.Zero)
            {
                throw Invalid("generatedAtUtc must be a UTC timestamp");
            }
            if (manifest.Minions == null)
            {
                throw Invalid("minions metadata is required");
            }

            var expectedFileName = MinionFilePrefix + manifest.ContentVersion + ".json";
            if (!string.Equals(manifest.Minions.FileName, expectedFileName, StringComparison.Ordinal))
            {
                throw Invalid("minions file name does not match the content version");
            }
            if (manifest.Minions.Bytes <= 0 || manifest.Minions.Bytes > MaxContentBytes)
            {
                throw Invalid("content byte count is outside the supported range");
            }
            if (contentBytes == null || contentBytes.Length != manifest.Minions.Bytes)
            {
                throw Invalid("content byte count does not match");
            }
            if (!IsLowerHexSha256(manifest.Minions.Sha256))
            {
                throw Invalid("content SHA-256 is malformed");
            }

            string actualSha256;
            using (var sha256 = SHA256.Create())
            {
                actualSha256 = BitConverter.ToString(sha256.ComputeHash(contentBytes)).Replace("-", string.Empty).ToLowerInvariant();
            }
            if (!string.Equals(actualSha256, manifest.Minions.Sha256, StringComparison.Ordinal))
            {
                throw Invalid("content SHA-256 does not match");
            }

            try
            {
                var json = StrictUtf8.GetString(contentBytes);
                if (json.Length > 0 && json[0] == '\uFEFF')
                {
                    throw Invalid("content must not contain a UTF-8 BOM");
                }
                return json;
            }
            catch (DecoderFallbackException exception)
            {
                throw Invalid("content is not valid UTF-8", exception);
            }
        }

        private static bool IsSafeContentVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64 || value.Contains(".."))
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
            public string generatedAtUtc;
            public RawFile minions;
        }

        [Serializable]
        private sealed class RawFile
        {
            public string fileName;
            public long bytes;
            public string sha256;
        }
    }
}
