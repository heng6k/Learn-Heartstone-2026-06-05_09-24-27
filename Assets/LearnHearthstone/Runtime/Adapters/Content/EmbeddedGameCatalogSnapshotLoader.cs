using System;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Data;

namespace LearnHearthstone.Adapters.Content
{
    public static class EmbeddedGameCatalogSnapshotLoader
    {
        public static GameCatalogSnapshot Load(string clientVersion)
        {
            var normalizedClientVersion = string.IsNullOrWhiteSpace(clientVersion) ? "unknown" : clientVersion.Trim();
            var heroes = HeroCatalogLoader.LoadFromResources();
            var timewarpedTavern = TimewarpedTavernCatalogLoader.LoadFromResources();

            return new GameCatalogSnapshot(
                new ContentSnapshotInfo(
                    "embedded:" + normalizedClientVersion,
                    normalizedClientVersion,
                    ContentSnapshotSource.Embedded,
                    string.Empty,
                    DateTime.UtcNow),
                LoadLanguage(false, heroes, timewarpedTavern),
                LoadLanguage(true, heroes, timewarpedTavern));
        }

        private static GameCatalogSet LoadLanguage(
            bool useEnglish,
            HeroCatalog heroes,
            TimewarpedTavernCatalog timewarpedTavern)
        {
            return new GameCatalogSet(
                MinionCatalogLoader.LoadFromResources(useEnglish),
                SpellCatalogLoader.LoadFromResources(useEnglish),
                heroes,
                TrinketCatalogLoader.LoadFromResources(useEnglish),
                QuestCatalogLoader.LoadFromResources(useEnglish),
                timewarpedTavern,
                AnomalyCatalogLoader.LoadFromResources(useEnglish),
                DarkmoonPrizeCatalogLoader.LoadFromResources(useEnglish));
        }
    }
}
