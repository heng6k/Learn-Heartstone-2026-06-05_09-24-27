using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LearnHearthstone.Application.Services
{
    public static class StrategyGuidePortableCodeService
    {
        public const string CodePrefix = "LHSG1";
        public const int PayloadSchemaVersion = 1;
        public const int MaxJsonBytes = 1024 * 1024;
        public const int MaxCodeCharacters = 2 * 1024 * 1024;

        private const string PackageType = "StrategyGuide";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static string Export(
            StrategyGuideCatalog catalog,
            string guideId,
            string profileId,
            ResolvedGameVersion version)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            var guide = catalog.GetGuide(guideId);
            var profile = catalog.GetProfile(guideId, profileId);

            return ExportCore(catalog, guide, profile, version);
        }

        public static string ExportGuide(
            StrategyGuideCatalog catalog,
            string guideId,
            ResolvedGameVersion version)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            return ExportCore(catalog, catalog.GetGuide(guideId), null, version);
        }

        private static string ExportCore(
            StrategyGuideCatalog catalog,
            StrategyGuideDefinition guide,
            StrategyGuideEntryProfileDefinition profile,
            ResolvedGameVersion version)
        {

            var validation = StrategyGuideValidator.Validate(catalog, guide, version);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    "Strategy guide validation failed: " + string.Join(" | ", validation.Errors));
            }

            var opponents = EligibleOpponents(
                catalog,
                guide,
                guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>()).ToList();
            if (opponents.Count == 0)
            {
                throw new InvalidOperationException("Strategy guide has no eligible opponent revisions.");
            }

            var payload = new StrategyGuidePortablePayload
            {
                SchemaVersion = PayloadSchemaVersion,
                PackageType = PackageType,
                GameVersionId = version.GameVersion.Id,
                RulesetId = version.Ruleset.Id,
                ContentSnapshotId = version.ContentSnapshotId,
                ContentFingerprint = version.ContentFingerprint,
                CatalogRevisionId = catalog.Definition.CatalogRevisionId,
                ProfileId = profile?.ProfileId,
                Guide = guide,
                Opponents = opponents
            };
            return Encode(payload);
        }

        public static StrategyGuideImportResult Import(string code, ResolvedGameVersion version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            try
            {
                return ImportCore(code, version);
            }
            catch (PortableCodeException exception)
            {
                return Rejected(exception.Code, exception.Message);
            }
            catch (Exception exception) when (
                exception is JsonException ||
                exception is InvalidDataException ||
                exception is IOException ||
                exception is ArgumentException ||
                exception is InvalidOperationException)
            {
                return Rejected("portable.payload.invalid", exception.Message);
            }
        }

        private static StrategyGuideImportResult ImportCore(string code, ResolvedGameVersion version)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw Invalid("portable.code.required", "Strategy guide code is required.");
            }

            var trimmed = code.Trim();
            if (trimmed.Length > MaxCodeCharacters)
            {
                throw Invalid("portable.code.too-large", "Strategy guide code exceeds the supported size.");
            }

            var parts = trimmed.Split('.');
            if (parts.Length != 3)
            {
                throw Invalid("portable.code.segment-count", "Strategy guide code must contain three segments.");
            }
            if (!string.Equals(parts[0], CodePrefix, StringComparison.Ordinal))
            {
                throw Invalid("portable.code.prefix", "Strategy guide code prefix is not supported.");
            }
            if (!IsSha256(parts[2]))
            {
                throw Invalid("portable.code.hash-format", "Strategy guide code hash is invalid.");
            }

            var compressed = DecodeBase64Url(parts[1]);
            var jsonBytes = DecompressBounded(compressed);
            var actualHash = Sha256(jsonBytes);
            if (!string.Equals(actualHash, parts[2], StringComparison.Ordinal))
            {
                throw Invalid("portable.code.hash-mismatch", "Strategy guide code has been changed or corrupted.");
            }

            string json;
            try
            {
                json = StrictUtf8.GetString(jsonBytes);
            }
            catch (DecoderFallbackException exception)
            {
                throw Invalid("portable.payload.utf8", "Strategy guide payload is not valid UTF-8.", exception);
            }

            var token = ParseStrictJson(json);
            var canonical = CanonicalJson(token);
            if (!string.Equals(canonical, json, StringComparison.Ordinal))
            {
                throw Invalid("portable.payload.not-canonical", "Strategy guide payload is not canonical JSON.");
            }

            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                DateParseHandling = DateParseHandling.None,
                Culture = System.Globalization.CultureInfo.InvariantCulture
            });
            StrategyGuidePortablePayload payload;
            try
            {
                payload = token.ToObject<StrategyGuidePortablePayload>(serializer);
            }
            catch (JsonException exception)
            {
                throw Invalid("portable.payload.schema", "Strategy guide payload does not match schema v1.", exception);
            }
            if (payload == null)
            {
                throw Invalid("portable.payload.empty", "Strategy guide payload is empty.");
            }

            ValidateIdentity(payload, version);
            if (payload.Guide == null)
            {
                throw Invalid("portable.guide.required", "Strategy guide revision is required.");
            }
            if (!string.Equals(payload.Guide.GameVersionId, payload.GameVersionId, StringComparison.Ordinal))
            {
                throw Invalid("portable.guide.version", "Strategy guide revision does not match the payload version.");
            }

            StrategyGuideCatalog catalog;
            try
            {
                catalog = new StrategyGuideCatalog(new StrategyGuideCatalogDefinition
                {
                    SchemaVersion = 2,
                    CatalogRevisionId = payload.CatalogRevisionId,
                    Guides = new List<StrategyGuideDefinition> { payload.Guide },
                    Opponents = payload.Opponents ?? new List<StrategyGuideOpponentDefinition>()
                });
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                throw Invalid("portable.catalog.invalid", exception.Message, exception);
            }

            StrategyGuideEntryProfileDefinition profile = null;
            if (!string.IsNullOrWhiteSpace(payload.ProfileId))
            {
                try
                {
                    profile = catalog.GetProfile(payload.Guide.GuideId, payload.ProfileId);
                }
                catch (InvalidOperationException exception)
                {
                    throw Invalid("portable.profile.missing", exception.Message, exception);
                }
            }

            var validation = StrategyGuideValidator.Validate(catalog, payload.Guide, version);
            if (!validation.IsValid)
            {
                throw Invalid(
                    "portable.guide.invalid",
                    "Strategy guide validation failed: " + string.Join(" | ", validation.Errors));
            }

            return new StrategyGuideImportResult
            {
                Status = StrategyGuideImportStatus.Compatible,
                Payload = payload,
                Catalog = catalog,
                Guide = payload.Guide,
                Profile = profile
            };
        }

        private static string Encode(StrategyGuidePortablePayload payload)
        {
            var token = JObject.FromObject(payload, JsonSerializer.Create(new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                Culture = System.Globalization.CultureInfo.InvariantCulture
            }));
            var json = CanonicalJson(token);
            var bytes = StrictUtf8.GetBytes(json);
            if (bytes.Length == 0 || bytes.Length > MaxJsonBytes)
            {
                throw new InvalidOperationException("Strategy guide payload exceeds the supported size.");
            }

            var compressed = Compress(bytes);
            return CodePrefix + "." + EncodeBase64Url(compressed) + "." + Sha256(bytes);
        }

        private static IEnumerable<StrategyGuideOpponentDefinition> EligibleOpponents(
            StrategyGuideCatalog catalog,
            StrategyGuideDefinition guide,
            IEnumerable<StrategyGuideEntryProfileDefinition> profiles)
        {
            var selectors = (profiles ?? Enumerable.Empty<StrategyGuideEntryProfileDefinition>())
                .Where(item => item?.Opponent != null)
                .Select(item => item.Opponent)
                .ToList();
            return catalog.Opponents.Where(item =>
                    item != null &&
                    string.Equals(item.GameVersionId, guide.GameVersionId, StringComparison.Ordinal) &&
                    selectors.Any(selector =>
                        item.StrengthRound == selector.StrengthRound &&
                        (item.Tags ?? new List<string>()).Contains(selector.RequiredTag)))
                .GroupBy(item => item.OpponentId, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(item => item.OpponentId, StringComparer.Ordinal);
        }

        private static void ValidateIdentity(StrategyGuidePortablePayload payload, ResolvedGameVersion version)
        {
            if (payload.SchemaVersion != PayloadSchemaVersion)
            {
                throw Invalid("portable.schema.unsupported", "Strategy guide payload schema is not supported.");
            }
            if (!string.Equals(payload.PackageType, PackageType, StringComparison.Ordinal))
            {
                throw Invalid("portable.package-type", "Strategy guide payload type is not supported.");
            }
            RequireIdentity(payload.GameVersionId, version.GameVersion.Id, "portable.version.mismatch");
            RequireIdentity(payload.RulesetId, version.Ruleset.Id, "portable.ruleset.mismatch");
            RequireIdentity(payload.ContentSnapshotId, version.ContentSnapshotId, "portable.snapshot.mismatch");
            RequireIdentity(payload.ContentFingerprint, version.ContentFingerprint, "portable.fingerprint.mismatch");
            if (string.IsNullOrWhiteSpace(payload.CatalogRevisionId))
            {
                throw Invalid("portable.catalog-revision.required", "Strategy guide catalog revision is required.");
            }
            if (payload.ProfileId != null && string.IsNullOrWhiteSpace(payload.ProfileId))
            {
                throw Invalid("portable.profile.required", "Strategy guide profile identity must be null or non-empty.");
            }
        }

        private static void RequireIdentity(string actual, string expected, string code)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw Invalid(code, "Strategy guide content identity is incompatible with this game version.");
            }
        }

        private static JToken ParseStrictJson(string json)
        {
            using var textReader = new StringReader(json);
            using var reader = new JsonTextReader(textReader)
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Decimal,
                MaxDepth = 64
            };
            var token = JToken.ReadFrom(reader, new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Load,
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                LineInfoHandling = LineInfoHandling.Load
            });
            if (token.Type != JTokenType.Object || reader.Read())
            {
                throw Invalid("portable.payload.json", "Strategy guide payload must contain one JSON object.");
            }
            if (ContainsComment(token))
            {
                throw Invalid("portable.payload.comment", "Strategy guide payload cannot contain comments.");
            }
            return token;
        }

        private static bool ContainsComment(JToken token)
        {
            if (token.Type == JTokenType.Comment)
            {
                return true;
            }
            return token is JContainer container && container.Children().Any(ContainsComment);
        }

        private static string CanonicalJson(JToken token)
        {
            return Canonicalize(token).ToString(Formatting.None);
        }

        private static JToken Canonicalize(JToken token)
        {
            if (token is JObject obj)
            {
                var result = new JObject();
                foreach (var property in obj.Properties().OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    result.Add(property.Name, Canonicalize(property.Value));
                }
                return result;
            }
            if (token is JArray array)
            {
                return new JArray(array.Select(Canonicalize));
            }
            return token.DeepClone();
        }

        private static byte[] Compress(byte[] bytes)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
            return output.ToArray();
        }

        private static byte[] DecompressBounded(byte[] compressed)
        {
            try
            {
                using var input = new MemoryStream(compressed, false);
                using var gzip = new GZipStream(input, CompressionMode.Decompress, false);
                using var output = new MemoryStream();
                var buffer = new byte[8192];
                while (true)
                {
                    var read = gzip.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        break;
                    }
                    if (output.Length + read > MaxJsonBytes)
                    {
                        throw Invalid("portable.payload.too-large", "Strategy guide payload exceeds the supported size.");
                    }
                    output.Write(buffer, 0, read);
                }
                if (output.Length == 0)
                {
                    throw Invalid("portable.payload.empty", "Strategy guide payload is empty.");
                }
                return output.ToArray();
            }
            catch (InvalidDataException exception)
            {
                throw Invalid("portable.payload.compression", "Strategy guide payload compression is invalid.", exception);
            }
        }

        private static string EncodeBase64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] DecodeBase64Url(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Any(character =>
                    !(character >= 'A' && character <= 'Z') &&
                    !(character >= 'a' && character <= 'z') &&
                    !(character >= '0' && character <= '9') &&
                    character != '-' && character != '_'))
            {
                throw Invalid("portable.code.base64", "Strategy guide code payload is not valid Base64Url.");
            }

            var base64 = value.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
                case 1:
                    throw Invalid("portable.code.base64", "Strategy guide code payload is not valid Base64Url.");
            }
            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (FormatException exception)
            {
                throw Invalid("portable.code.base64", "Strategy guide code payload is not valid Base64Url.", exception);
            }
        }

        private static bool IsSha256(string value)
        {
            return value != null && value.Length == 64 && value.All(character =>
                (character >= '0' && character <= '9') ||
                (character >= 'a' && character <= 'f'));
        }

        private static string Sha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);
            var builder = new StringBuilder(hash.Length * 2);
            foreach (var item in hash)
            {
                builder.Append(item.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private static StrategyGuideImportResult Rejected(string code, string message)
        {
            return new StrategyGuideImportResult
            {
                Status = StrategyGuideImportStatus.Rejected,
                Diagnostics = new List<StrategyGuideImportDiagnostic>
                {
                    new StrategyGuideImportDiagnostic
                    {
                        Code = code,
                        Message = message,
                        IsWarning = false
                    }
                }
            };
        }

        private static PortableCodeException Invalid(string code, string message, Exception inner = null)
        {
            return new PortableCodeException(code, message, inner);
        }

        private sealed class PortableCodeException : Exception
        {
            public PortableCodeException(string code, string message, Exception inner)
                : base(message, inner)
            {
                Code = code;
            }

            public string Code { get; }
        }
    }
}
