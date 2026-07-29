using System;
using System.IO;
using UnityEngine;

namespace LearnHearthstone.Adapters.Content
{
    public sealed class LastKnownGoodContentRepository
    {
        private const string ActiveManifestFileName = "content-manifest.json";
        private readonly string directory;

        public LastKnownGoodContentRepository(string directory = null)
        {
            this.directory = string.IsNullOrWhiteSpace(directory)
                ? Path.Combine(UnityEngine.Application.persistentDataPath, "Content", "LKG")
                : directory;
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

            var manifestPath = Path.Combine(directory, ActiveManifestFileName);
            if (!File.Exists(manifestPath))
            {
                return false;
            }

            try
            {
                manifestBytes = File.ReadAllBytes(manifestPath);
                var manifest = ContentPackageValidator.ParseManifest(manifestBytes);
                ContentPackageValidator.ValidateManifest(manifest, clientVersion);

                var contentPath = Path.Combine(directory, manifest.Minions.FileName);
                if (!File.Exists(contentPath))
                {
                    throw new InvalidDataException("LKG content file is missing.");
                }

                contentBytes = File.ReadAllBytes(contentPath);
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

        public void Promote(byte[] manifestBytes, byte[] contentBytes, string clientVersion)
        {
            var manifest = ContentPackageValidator.ParseManifest(manifestBytes);
            ContentPackageValidator.Validate(manifest, contentBytes, clientVersion);
            Directory.CreateDirectory(directory);

            var contentPath = Path.Combine(directory, manifest.Minions.FileName);
            if (File.Exists(contentPath))
            {
                ContentPackageValidator.Validate(manifest, File.ReadAllBytes(contentPath), clientVersion);
            }
            else
            {
                WriteAtomic(contentPath, contentBytes, false);
            }

            WriteAtomic(Path.Combine(directory, ActiveManifestFileName), manifestBytes, true);
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
    }
}
