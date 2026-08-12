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
            return Create(
                new ContentSnapshotInfo(
                    "embedded:" + normalizedClientVersion,
                    normalizedClientVersion,
                    ContentSnapshotSource.Embedded,
                    string.Empty,
                    DateTime.UtcNow),
                MinionCatalogLoader.LoadFromResources(),
                MinionCatalogLoader.LoadFromResources(true));
        }

        public static GameCatalogSnapshot LoadWithMinionJson(ContentSnapshotInfo info, string minionJson)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            if (string.IsNullOrWhiteSpace(minionJson))
            {
                throw new ArgumentException("Minion JSON is required.", nameof(minionJson));
            }

            return Create(
                info,
                MinionCatalogLoader.LoadFromJson(minionJson),
                MinionCatalogLoader.LoadFromJson(minionJson, true));
        }

        private static GameCatalogSnapshot Create(
            ContentSnapshotInfo info,
            MinionCatalog chineseMinions,
            MinionCatalog englishMinions)
        {
            var heroes = HeroCatalogLoader.LoadFromResources();
            var timewarpedTavern = TimewarpedTavernCatalogLoader.LoadFromResources();
            var chineseDarkGifts = DarkGiftCatalogLoader.LoadFromResources(false);
            var englishDarkGifts = DarkGiftCatalogLoader.LoadFromResources();
            var chineseSpells = SpellCatalogLoader.LoadFromResources();
            var englishSpells = SpellCatalogLoader.LoadFromResources(true);
            var versionedContent = VersionedContentCatalogLoader.LoadFromResources(
                englishMinions.All,
                heroes.AllHeroes,
                englishSpells.All,
                englishDarkGifts.All);
            return new GameCatalogSnapshot(
                info,
                LoadLanguage(false, chineseMinions, chineseSpells, heroes, timewarpedTavern, chineseDarkGifts),
                LoadLanguage(true, englishMinions, englishSpells, heroes, timewarpedTavern, englishDarkGifts),
                versionedContent);
        }

        private static GameCatalogSet LoadLanguage(
            bool useEnglish,
            MinionCatalog minions,
            SpellCatalog spells,
            HeroCatalog heroes,
            TimewarpedTavernCatalog timewarpedTavern,
            DarkGiftCatalog darkGifts)
        {
            return new GameCatalogSet(
                minions,
                spells,
                heroes,
                TrinketCatalogLoader.LoadFromResources(useEnglish),
                QuestCatalogLoader.LoadFromResources(useEnglish),
                timewarpedTavern,
                AnomalyCatalogLoader.LoadFromResources(useEnglish),
                DarkmoonPrizeCatalogLoader.LoadFromResources(useEnglish),
                darkGifts);
        }
    }
}
