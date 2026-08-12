using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Content;
using UnityEngine;

namespace LearnHearthstone.Adapters.Content
{
    public sealed class GameCatalogSnapshotResolver
    {
        private readonly string clientVersion;
        private readonly LastKnownGoodContentRepository repository;
        private readonly bool preferEmbeddedFallback;

        public GameCatalogSnapshotResolver(
            string clientVersion,
            LastKnownGoodContentRepository repository = null,
            bool preferEmbeddedFallback = false)
        {
            this.clientVersion = string.IsNullOrWhiteSpace(clientVersion) ? "unknown" : clientVersion.Trim();
            this.repository = repository ?? new LastKnownGoodContentRepository();
            this.preferEmbeddedFallback = preferEmbeddedFallback;
        }

        public GameCatalogSnapshot Resolve(
            byte[] remoteManifestBytes = null,
            byte[] remoteContentBytes = null,
            string remoteFailureReason = null)
        {
            ContentPackageDownload remotePackage = null;
            if (remoteManifestBytes != null || remoteContentBytes != null)
            {
                try
                {
                    if (remoteManifestBytes == null || remoteContentBytes == null)
                    {
                        throw new InvalidDataException("Remote content package is incomplete.");
                    }
                    var manifest = ContentPackageValidator.ParseManifest(remoteManifestBytes);
                    if (manifest.Minions == null)
                    {
                        throw new InvalidDataException("Remote v1 content package is missing minions metadata.");
                    }
                    remotePackage = new ContentPackageDownload(
                        remoteManifestBytes,
                        new Dictionary<string, byte[]>(StringComparer.Ordinal)
                        {
                            [manifest.Minions.FileName] = remoteContentBytes
                        });
                }
                catch (Exception exception)
                {
                    remoteFailureReason = exception.Message;
                }
            }
            return ResolveInternal(remotePackage, remoteFailureReason);
        }

        public GameCatalogSnapshot Resolve(
            ContentPackageDownload remotePackage,
            string remoteFailureReason = null)
        {
            return ResolveInternal(remotePackage, remoteFailureReason);
        }

        private GameCatalogSnapshot ResolveInternal(
            ContentPackageDownload remotePackage,
            string remoteFailureReason)
        {
            if (remotePackage != null)
            {
                try
                {
                    var remote = LoadPackage(remotePackage.ManifestBytes, remotePackage.Files, ContentSnapshotSource.Remote);
                    repository.Promote(remotePackage.ManifestBytes, remotePackage.Files, clientVersion);
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

            if (preferEmbeddedFallback)
            {
                var developmentEmbedded = EmbeddedGameCatalogSnapshotLoader.Load(clientVersion);
                LogSelected(developmentEmbedded);
                return developmentEmbedded;
            }

            if (repository.TryReadPackage(clientVersion, out var manifestBytes, out var files, out var lkgFailureReason))
            {
                try
                {
                    var lastKnownGood = LoadPackage(manifestBytes, files, ContentSnapshotSource.LastKnownGood);
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
            IReadOnlyDictionary<string, byte[]> files,
            ContentSnapshotSource source)
        {
            var manifest = ContentPackageValidator.ParseManifest(manifestBytes);
            var jsonFiles = ContentPackageValidator.Validate(manifest, files, clientVersion);
            if (manifest.ProtocolVersion == ContentPackageValidator.LegacyProtocolVersion)
            {
                var minionJson = jsonFiles[manifest.Minions.FileName];
                return EmbeddedGameCatalogSnapshotLoader.LoadWithMinionJson(
                    new ContentSnapshotInfo(
                        manifest.ContentVersion,
                        manifest.RequiredClientVersion,
                        source,
                        string.Empty,
                        DateTime.UtcNow),
                    minionJson);
            }

            return LoadV2Snapshot(manifest, jsonFiles, source);
        }

        private GameCatalogSnapshot LoadV2Snapshot(
            ContentPackageManifest manifest,
            IReadOnlyDictionary<string, string> jsonFiles,
            ContentSnapshotSource source)
        {
            var heroJson = JsonForKind(manifest, jsonFiles, "heroes");
            var minionJson = JsonForKind(manifest, jsonFiles, "minions");
            var spellJson = JsonForKind(manifest, jsonFiles, "tavern-spells");
            var trinketJson = JsonForKind(manifest, jsonFiles, "trinkets");
            var darkGiftJson = JsonForKind(manifest, jsonFiles, "dark-gifts");
            var versionsJson = JsonForKind(manifest, jsonFiles, "versions");
            var rulesetsJson = JsonForKind(manifest, jsonFiles, "rulesets");
            var questJson = JsonForKind(manifest, jsonFiles, "quests");
            var anomalyJson = JsonForKind(manifest, jsonFiles, "anomalies");
            var timewarpedJson = JsonForKind(manifest, jsonFiles, "timewarped-tavern");
            var darkmoonJson = JsonForKind(manifest, jsonFiles, "darkmoon-prizes");
            var heroLocalization = LocalizationJson(manifest, jsonFiles, "heroLocalizationZhCN");
            var questLocalization = LocalizationJson(manifest, jsonFiles, "questLocalizationZhCN");
            var trinketLocalization = LocalizationJson(manifest, jsonFiles, "trinketLocalizationZhCN");
            var anomalyLocalization = LocalizationJson(manifest, jsonFiles, "anomalyLocalizationZhCN");
            var darkmoonLocalization = LocalizationJson(manifest, jsonFiles, "darkmoonPrizeLocalizationZhCN");
            var darkGiftLocalization = LocalizationJson(manifest, jsonFiles, "darkGiftLocalizationZhCN");
            var chineseDarkGifts = DarkGiftCatalogLoader.LoadFromJson(darkGiftJson, darkGiftLocalization);
            var englishDarkGifts = DarkGiftCatalogLoader.LoadFromJson(darkGiftJson);
            var chineseHeroes = HeroCatalogLoader.LoadFromJson(heroJson, heroLocalization);
            var englishHeroes = HeroCatalogLoader.LoadFromJson(heroJson);
            var chineseSpells = SpellCatalogLoader.LoadFromJson(spellJson);
            var englishSpells = SpellCatalogLoader.LoadFromJson(spellJson, true);
            var chineseMinions = MinionCatalogLoader.LoadFromJson(minionJson);
            var englishMinions = MinionCatalogLoader.LoadFromJson(minionJson, true);
            var versionedContent = VersionedContentCatalogLoader.LoadFromJson(
                versionsJson,
                rulesetsJson,
                englishMinions.All,
                englishHeroes.AllHeroes,
                englishSpells.All,
                englishDarkGifts.All);

            var info = new ContentSnapshotInfo(
                manifest.ContentVersion,
                string.IsNullOrEmpty(manifest.RequiredClientVersion)
                    ? manifest.MinClientVersion + ".." + manifest.MaxClientVersion
                    : manifest.RequiredClientVersion,
                source,
                string.Empty,
                DateTime.UtcNow,
                manifest.SnapshotId,
                manifest.GameVersionId,
                manifest.RulesetId,
                manifest.PackageFingerprint);
            var chinese = new GameCatalogSet(
                chineseMinions,
                chineseSpells,
                chineseHeroes,
                TrinketCatalogLoader.LoadFromJson(trinketJson, trinketLocalization),
                QuestCatalogLoader.LoadFromJson(questJson, questLocalization),
                TimewarpedTavernCatalogLoader.LoadFromJson(timewarpedJson),
                AnomalyCatalogLoader.LoadFromJson(anomalyJson, anomalyLocalization),
                DarkmoonPrizeCatalogLoader.LoadFromJson(darkmoonJson, darkmoonLocalization),
                chineseDarkGifts);
            var english = new GameCatalogSet(
                englishMinions,
                englishSpells,
                englishHeroes,
                TrinketCatalogLoader.LoadFromJson(trinketJson),
                QuestCatalogLoader.LoadFromJson(questJson),
                TimewarpedTavernCatalogLoader.LoadFromJson(timewarpedJson),
                AnomalyCatalogLoader.LoadFromJson(anomalyJson),
                DarkmoonPrizeCatalogLoader.LoadFromJson(darkmoonJson),
                englishDarkGifts);
            var snapshot = new GameCatalogSnapshot(info, chinese, english, versionedContent);
            return versionedContent.CreateResolver().Resolve(manifest.GameVersionId, snapshot).Snapshot;
        }

        private static string JsonForKind(
            ContentPackageManifest manifest,
            IReadOnlyDictionary<string, string> jsonFiles,
            string kind)
        {
            var matches = manifest.Files
                .Where(file => string.Equals(file.Kind, kind, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidDataException("Complete content snapshot requires exactly one " + kind + " file.");
            }
            return jsonFiles[matches[0].FileName];
        }

        private static string LocalizationJson(
            ContentPackageManifest manifest,
            IReadOnlyDictionary<string, string> jsonFiles,
            string nameToken)
        {
            var matches = manifest.Files
                .Where(file => string.Equals(file.Kind, "localizations", StringComparison.Ordinal) &&
                               file.FileName.IndexOf(nameToken, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidDataException("Complete content snapshot requires localization file " + nameToken + ".");
            }
            return jsonFiles[matches[0].FileName];
        }

        private static void LogSelected(GameCatalogSnapshot snapshot)
        {
            Debug.Log(
                "Content snapshot selected: source=" + snapshot.Info.Source +
                ", snapshot=" + snapshot.Info.SnapshotId +
                ", gameVersion=" + snapshot.Info.GameVersionId +
                ", fingerprint=" + snapshot.Info.ContentFingerprint + ".");
        }
    }
}
