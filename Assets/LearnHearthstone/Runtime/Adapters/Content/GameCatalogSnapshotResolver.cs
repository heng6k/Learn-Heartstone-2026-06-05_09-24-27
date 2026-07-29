using System;
using System.IO;
using LearnHearthstone.Application.Content;
using UnityEngine;

namespace LearnHearthstone.Adapters.Content
{
    public sealed class GameCatalogSnapshotResolver
    {
        private readonly string clientVersion;
        private readonly LastKnownGoodContentRepository repository;

        public GameCatalogSnapshotResolver(string clientVersion, LastKnownGoodContentRepository repository = null)
        {
            this.clientVersion = string.IsNullOrWhiteSpace(clientVersion) ? "unknown" : clientVersion.Trim();
            this.repository = repository ?? new LastKnownGoodContentRepository();
        }

        public GameCatalogSnapshot Resolve(
            byte[] remoteManifestBytes = null,
            byte[] remoteContentBytes = null,
            string remoteFailureReason = null)
        {
            if (remoteManifestBytes != null || remoteContentBytes != null)
            {
                try
                {
                    if (remoteManifestBytes == null || remoteContentBytes == null)
                    {
                        throw new InvalidDataException("Remote content package is incomplete.");
                    }

                    var remote = LoadPackage(remoteManifestBytes, remoteContentBytes, ContentSnapshotSource.Remote);
                    repository.Promote(remoteManifestBytes, remoteContentBytes, clientVersion);
                    LogSelected(remote);
                    return remote;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Remote content rejected: " + exception.Message);
                }
            }
            else if (!string.IsNullOrWhiteSpace(remoteFailureReason))
            {
                Debug.LogWarning("Remote content unavailable: " + remoteFailureReason);
            }

            if (repository.TryRead(clientVersion, out var manifestBytes, out var contentBytes, out var lkgFailureReason))
            {
                try
                {
                    var lastKnownGood = LoadPackage(manifestBytes, contentBytes, ContentSnapshotSource.LastKnownGood);
                    LogSelected(lastKnownGood);
                    return lastKnownGood;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("LKG content rejected: " + exception.Message);
                }
            }
            else if (!string.IsNullOrWhiteSpace(lkgFailureReason))
            {
                Debug.LogWarning("LKG content unavailable: " + lkgFailureReason);
            }

            var embedded = EmbeddedGameCatalogSnapshotLoader.Load(clientVersion);
            LogSelected(embedded);
            return embedded;
        }

        private GameCatalogSnapshot LoadPackage(
            byte[] manifestBytes,
            byte[] contentBytes,
            ContentSnapshotSource source)
        {
            var manifest = ContentPackageValidator.ParseManifest(manifestBytes);
            var minionJson = ContentPackageValidator.Validate(manifest, contentBytes, clientVersion);
            return EmbeddedGameCatalogSnapshotLoader.LoadWithMinionJson(
                new ContentSnapshotInfo(
                    manifest.ContentVersion,
                    manifest.RequiredClientVersion,
                    source,
                    string.Empty,
                    DateTime.UtcNow),
                minionJson);
        }

        private static void LogSelected(GameCatalogSnapshot snapshot)
        {
            Debug.Log("Content snapshot selected: source=" + snapshot.Info.Source + ", version=" + snapshot.Info.ContentVersion + ".");
        }
    }
}
