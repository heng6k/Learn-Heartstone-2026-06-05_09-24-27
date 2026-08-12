using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LearnHearthstone.Application.Content
{
    public sealed class ContentPackageManifest
    {
        public ContentPackageManifest(
            int protocolVersion,
            string contentVersion,
            string requiredClientVersion,
            string generatedAtUtc,
            ContentPackageFile minions)
            : this(
                protocolVersion,
                contentVersion,
                contentVersion,
                string.Empty,
                string.Empty,
                requiredClientVersion,
                requiredClientVersion,
                generatedAtUtc,
                minions == null ? Array.Empty<ContentPackageFile>() : new[] { minions },
                string.Empty)
        {
        }

        public ContentPackageManifest(
            int protocolVersion,
            string contentVersion,
            string snapshotId,
            string gameVersionId,
            string rulesetId,
            string minClientVersion,
            string maxClientVersion,
            string generatedAtUtc,
            IEnumerable<ContentPackageFile> files,
            string packageFingerprint)
        {
            ProtocolVersion = protocolVersion;
            ContentVersion = contentVersion;
            SnapshotId = snapshotId;
            GameVersionId = gameVersionId;
            RulesetId = rulesetId;
            MinClientVersion = minClientVersion;
            MaxClientVersion = maxClientVersion;
            RequiredClientVersion = string.Equals(minClientVersion, maxClientVersion, StringComparison.Ordinal)
                ? minClientVersion
                : string.Empty;
            GeneratedAtUtc = generatedAtUtc;
            Files = Array.AsReadOnly((files ?? Enumerable.Empty<ContentPackageFile>()).ToArray());
            PackageFingerprint = packageFingerprint;
            Minions = Files.FirstOrDefault(file => string.Equals(file.Kind, "minions", StringComparison.Ordinal));
        }

        public int ProtocolVersion { get; }
        public string ContentVersion { get; }
        public string RequiredClientVersion { get; }
        public string GeneratedAtUtc { get; }
        public ContentPackageFile Minions { get; }
        public string SnapshotId { get; }
        public string GameVersionId { get; }
        public string RulesetId { get; }
        public string MinClientVersion { get; }
        public string MaxClientVersion { get; }
        public IReadOnlyList<ContentPackageFile> Files { get; }
        public string PackageFingerprint { get; }
        public bool IsLegacyV1 => ProtocolVersion == 1;
    }

    public sealed class ContentPackageFile
    {
        public ContentPackageFile(string fileName, long bytes, string sha256)
            : this("minions", fileName, 1, bytes, sha256)
        {
        }

        public ContentPackageFile(string kind, string fileName, int schemaVersion, long bytes, string sha256)
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

    public sealed class ContentPackageDownload
    {
        private readonly ReadOnlyDictionary<string, byte[]> files;

        public ContentPackageDownload(byte[] manifestBytes, IReadOnlyDictionary<string, byte[]> files)
        {
            ManifestBytes = manifestBytes == null
                ? throw new ArgumentNullException(nameof(manifestBytes))
                : (byte[])manifestBytes.Clone();
            var copy = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var pair in files ?? throw new ArgumentNullException(nameof(files)))
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                {
                    throw new ArgumentException("Content package files must have names and bytes.", nameof(files));
                }
                copy.Add(pair.Key, (byte[])pair.Value.Clone());
            }
            this.files = new ReadOnlyDictionary<string, byte[]>(copy);
        }

        public byte[] ManifestBytes { get; }
        public IReadOnlyDictionary<string, byte[]> Files => files;
    }
}
