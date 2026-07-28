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
        {
            ContentVersion = string.IsNullOrWhiteSpace(contentVersion)
                ? throw new ArgumentException("Content version is required.", nameof(contentVersion))
                : contentVersion;
            RequiredClientVersion = requiredClientVersion ?? string.Empty;
            Source = source;
            SourceCommit = sourceCommit ?? string.Empty;
            LoadedAtUtc = loadedAtUtc.Kind == DateTimeKind.Utc ? loadedAtUtc : loadedAtUtc.ToUniversalTime();
        }

        public string ContentVersion { get; }
        public string RequiredClientVersion { get; }
        public ContentSnapshotSource Source { get; }
        public string SourceCommit { get; }
        public DateTime LoadedAtUtc { get; }
    }

    public sealed class GameCatalogSnapshot
    {
        public GameCatalogSnapshot(ContentSnapshotInfo info, GameCatalogSet chinese, GameCatalogSet english)
        {
            Info = info ?? throw new ArgumentNullException(nameof(info));
            Chinese = chinese ?? throw new ArgumentNullException(nameof(chinese));
            English = english ?? throw new ArgumentNullException(nameof(english));
        }

        public ContentSnapshotInfo Info { get; }
        public GameCatalogSet Chinese { get; }
        public GameCatalogSet English { get; }

        public GameCatalogSet ForLanguage(bool useEnglish)
        {
            return useEnglish ? English : Chinese;
        }
    }
}
