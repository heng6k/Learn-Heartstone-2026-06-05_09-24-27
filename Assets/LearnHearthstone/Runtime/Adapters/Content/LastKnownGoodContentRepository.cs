using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using LearnHearthstone.Application.Content;
using UnityEngine;

namespace LearnHearthstone.Adapters.Content
{
    public sealed class LastKnownGoodContentRepository
    {
        private const string ActiveManifestFileName = "content-manifest.json";
        private const string ActivePointerFileName = "active.json";
        private const string SnapshotsDirectoryName = "Snapshots";
        private readonly string rootDirectory;
        private readonly string legacyDirectory;

        public LastKnownGoodContentRepository(string directory = null)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                rootDirectory = Path.Combine(UnityEngine.Application.persistentDataPath, "Content");
                legacyDirectory = Path.Combine(rootDirectory, "LKG");
            }
            else
            {
                rootDirectory = directory;
                legacyDirectory = directory;
            }
        }

        public bool TryRead(
            string clientVersion,
            out byte[] manifestBytes,
            out byte[] contentBytes,
            out string failureReason)
        {
            manifestBytes = null;
            contentBytes = null;
            failureReason = null;

            var manifestPath = Path.Combine(legacyDirectory, ActiveManifestFileName);
            if (!File.Exists(manifestPath))
            {
                return false;
            }

            try
            {
                manifestBytes = File.ReadAllBytes(manifestPath);
                var manifest = ContentPackageValidator.ParseManifest(manifestBytes);
                ContentPackageValidator.ValidateManifest(manifest, clientVersion);
                if (manifest.ProtocolVersion != ContentPackageValidator.LegacyProtocolVersion)
                {
                    throw new InvalidDataException("Legacy LKG manifest is not protocol v1.");
                }

                var contentPath = Path.Combine(legacyDirectory, manifest.Minions.FileName);
                if (!File.Exists(contentPath))
                {
                    throw new InvalidDataException("LKG content file is missing.");
                }

                contentBytes = File.ReadAllBytes(contentPath);
                ContentPackageValidator.Validate(manifest, contentBytes, clientVersion);
                return true;
            }
            catch (Exception exception)
            {
                manifestBytes = null;
                contentBytes = null;
                failureReason = exception.Message;
                return false;
            }
        }

        public bool TryReadPackage(
            string clientVersion,
            out byte[] manifestBytes,
            out IReadOnlyDictionary<string, byte[]> files,
            out string failureReason)
        {
            manifestBytes = null;
            files = null;
            failureReason = null;
            var activePath = Path.Combine(rootDirectory, ActivePointerFileName);
            if (!File.Exists(activePath))
            {
                if (!TryRead(clientVersion, out manifestBytes, out var legacyBytes, out failureReason))
                {
                    return false;
                }

                var legacyManifest = ContentPackageValidator.ParseManifest(manifestBytes);
                files = new ReadOnlyDictionary<string, byte[]>(new Dictionary<string, byte[]>(StringComparer.Ordinal)
                {
                    [legacyManifest.Minions.FileName] = legacyBytes
                });
                return true;
            }

            try
            {
                var pointer = JsonUtility.FromJson<ActivePointer>(File.ReadAllText(activePath, Encoding.UTF8));
                if (pointer == null || !IsSafeSegment(pointer.snapshotId))
                {
                    throw new InvalidDataException("LKG active snapshot pointer is invalid.");
                }

                var snapshotDirectory = Path.Combine(rootDirectory, SnapshotsDirectoryName, pointer.snapshotId);
                ReadSnapshotDirectory(
                    snapshotDirectory,
                    pointer.snapshotId,
                    clientVersion,
                    out manifestBytes,
                    out files);
                var manifest = ContentPackageValidator.ParseManifest(manifestBytes);
                if (!string.IsNullOrWhiteSpace(pointer.packageFingerprint) &&
                    !string.Equals(pointer.packageFingerprint, manifest.PackageFingerprint, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("LKG active snapshot fingerprint does not match.");
                }
                return true;
            }
            catch (Exception exception)
            {
                manifestBytes = null;
                files = null;
                failureReason = exception.Message;
                return false;
            }
        }

        public void Promote(byte[] manifestBytes, byte[] contentBytes, string clientVersion)
        {
            var manifest = ContentPackageValidator.ParseManifest(manifestBytes);
            ContentPackageValidator.Validate(manifest, contentBytes, clientVersion);
            Directory.CreateDirectory(legacyDirectory);

            var contentPath = Path.Combine(legacyDirectory, manifest.Minions.FileName);
            if (File.Exists(contentPath))
            {
                ContentPackageValidator.Validate(manifest, File.ReadAllBytes(contentPath), clientVersion);
            }
            else
            {
                WriteAtomic(contentPath, contentBytes, false);
            }

            WriteAtomic(Path.Combine(legacyDirectory, ActiveManifestFileName), manifestBytes, true);
        }

        public void Promote(
            byte[] manifestBytes,
            IReadOnlyDictionary<string, byte[]> files,
            string clientVersion)
        {
            var manifest = ContentPackageValidator.ParseManifest(manifestBytes);
            ContentPackageValidator.Validate(manifest, files, clientVersion);
            if (manifest.ProtocolVersion == ContentPackageValidator.LegacyProtocolVersion)
            {
                Promote(manifestBytes, files[manifest.Minions.FileName], clientVersion);
                return;
            }

            var snapshotsDirectory = Path.Combine(rootDirectory, SnapshotsDirectoryName);
            var snapshotDirectory = Path.Combine(snapshotsDirectory, manifest.SnapshotId);
            Directory.CreateDirectory(snapshotsDirectory);
            if (Directory.Exists(snapshotDirectory))
            {
                ReadSnapshotDirectory(
                    snapshotDirectory,
                    manifest.SnapshotId,
                    clientVersion,
                    out var existingManifestBytes,
                    out _);
                var existing = ContentPackageValidator.ParseManifest(existingManifestBytes);
                if (!string.Equals(existing.PackageFingerprint, manifest.PackageFingerprint, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Immutable content snapshot already exists with different bytes.");
                }
            }
            else
            {
                var stagingDirectory = Path.Combine(
                    snapshotsDirectory,
                    "." + manifest.SnapshotId + "." + Guid.NewGuid().ToString("N") + ".staging");
                try
                {
                    Directory.CreateDirectory(stagingDirectory);
                    foreach (var file in manifest.Files)
                    {
                        File.WriteAllBytes(Path.Combine(stagingDirectory, file.FileName), files[file.FileName]);
                    }
                    File.WriteAllBytes(Path.Combine(stagingDirectory, ActiveManifestFileName), manifestBytes);
                    ReadSnapshotDirectory(
                        stagingDirectory,
                        manifest.SnapshotId,
                        clientVersion,
                        out _,
                        out _);
                    Directory.Move(stagingDirectory, snapshotDirectory);
                }
                finally
                {
                    if (Directory.Exists(stagingDirectory))
                    {
                        Directory.Delete(stagingDirectory, true);
                    }
                }
            }

            var pointerBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new ActivePointer
            {
                snapshotId = manifest.SnapshotId,
                gameVersionId = manifest.GameVersionId,
                packageFingerprint = manifest.PackageFingerprint
            }));
            Directory.CreateDirectory(rootDirectory);
            WriteAtomic(Path.Combine(rootDirectory, ActivePointerFileName), pointerBytes, true);
        }

        private static void ReadSnapshotDirectory(
            string snapshotDirectory,
            string expectedSnapshotId,
            string clientVersion,
            out byte[] manifestBytes,
            out IReadOnlyDictionary<string, byte[]> files)
        {
            var manifestPath = Path.Combine(snapshotDirectory, ActiveManifestFileName);
            if (!File.Exists(manifestPath))
            {
                throw new InvalidDataException("LKG snapshot manifest is missing.");
            }

            manifestBytes = File.ReadAllBytes(manifestPath);
            var manifest = ContentPackageValidator.ParseManifest(manifestBytes);
            if (manifest.ProtocolVersion != ContentPackageValidator.SupportedProtocolVersion ||
                !string.Equals(manifest.SnapshotId, expectedSnapshotId, StringComparison.Ordinal))
            {
                throw new InvalidDataException("LKG snapshot identity does not match its directory.");
            }

            var loaded = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var file in manifest.Files)
            {
                var filePath = Path.Combine(snapshotDirectory, file.FileName);
                if (!File.Exists(filePath))
                {
                    throw new InvalidDataException("LKG snapshot file is missing: " + file.FileName + ".");
                }
                loaded.Add(file.FileName, File.ReadAllBytes(filePath));
            }
            ContentPackageValidator.Validate(manifest, loaded, clientVersion);
            files = new ReadOnlyDictionary<string, byte[]>(loaded);
        }

        private static bool IsSafeSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Contains("..") || value.Length > 128)
            {
                return false;
            }
            return string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal);
        }

        private static void WriteAtomic(string destination, byte[] bytes, bool overwrite)
        {
            var temporaryPath = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, bytes);
                if (overwrite && File.Exists(destination))
                {
                    File.Replace(temporaryPath, destination, null);
                }
                else
                {
                    File.Move(temporaryPath, destination);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        [Serializable]
        private sealed class ActivePointer
        {
            public string snapshotId;
            public string gameVersionId;
            public string packageFingerprint;
        }
    }
}
