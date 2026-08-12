using System;

namespace LearnHearthstone.Application.Content
{
    public enum ContentSnapshotSource
    {
        Remote,
        LastKnownGood,
        Embedded
    }

    public sealed class ContentSnapshotInfo
    {
        public ContentSnapshotInfo(
            string contentVersion,
            string requiredClientVersion,
            ContentSnapshotSource source,
            string sourceCommit,
            DateTime loadedAtUtc)
            : this(
                contentVersion,
                requiredClientVersion,
                source,
                sourceCommit,
                loadedAtUtc,
                contentVersion,
                string.Empty,
                string.Empty,
                string.Empty)
        {
        }

        public ContentSnapshotInfo(
            string contentVersion,
            string requiredClientVersion,
            ContentSnapshotSource source,
            string sourceCommit,
            DateTime loadedAtUtc,
            string snapshotId,
            string gameVersionId,
            string rulesetId,
            string contentFingerprint)
        {
            ContentVersion = string.IsNullOrWhiteSpace(contentVersion)
                ? throw new ArgumentException("Content version is required.", nameof(contentVersion))
                : contentVersion;
            RequiredClientVersion = requiredClientVersion ?? string.Empty;
            Source = source;
            SourceCommit = sourceCommit ?? string.Empty;
            LoadedAtUtc = loadedAtUtc.Kind == DateTimeKind.Utc ? loadedAtUtc : loadedAtUtc.ToUniversalTime();
            SnapshotId = string.IsNullOrWhiteSpace(snapshotId) ? ContentVersion : snapshotId;
            GameVersionId = gameVersionId ?? string.Empty;
            RulesetId = rulesetId ?? string.Empty;
            ContentFingerprint = contentFingerprint ?? string.Empty;
        }

        public string ContentVersion { get; }
        public string RequiredClientVersion { get; }
        public ContentSnapshotSource Source { get; }
        public string SourceCommit { get; }
        public DateTime LoadedAtUtc { get; }
        public string SnapshotId { get; }
        public string GameVersionId { get; }
        public string RulesetId { get; }
        public string ContentFingerprint { get; }
    }

    public sealed class GameCatalogSnapshot
    {
        public GameCatalogSnapshot(
            ContentSnapshotInfo info,
            GameCatalogSet chinese,
            GameCatalogSet english,
            VersionedContentCatalog versionedContent = null)
        {
            Info = info ?? throw new ArgumentNullException(nameof(info));
            Chinese = chinese ?? throw new ArgumentNullException(nameof(chinese));
            English = english ?? throw new ArgumentNullException(nameof(english));
            VersionedContent = versionedContent;
        }

        public ContentSnapshotInfo Info { get; }
        public GameCatalogSet Chinese { get; }
        public GameCatalogSet English { get; }
        public VersionedContentCatalog VersionedContent { get; }

        public GameCatalogSet ForLanguage(bool useEnglish)
        {
            return useEnglish ? English : Chinese;
        }

        public GameCatalogSnapshot AsVersionResolutionSource()
        {
            if (string.IsNullOrWhiteSpace(Info.GameVersionId) &&
                string.IsNullOrWhiteSpace(Info.RulesetId) &&
                string.IsNullOrWhiteSpace(Info.ContentFingerprint))
            {
                return this;
            }

            return new GameCatalogSnapshot(
                new ContentSnapshotInfo(
                    Info.ContentVersion,
                    Info.RequiredClientVersion,
                    Info.Source,
                    Info.SourceCommit,
                    Info.LoadedAtUtc,
                    Info.SnapshotId,
                    string.Empty,
                    string.Empty,
                    string.Empty),
                Chinese,
                English,
                VersionedContent);
        }
    }
}
