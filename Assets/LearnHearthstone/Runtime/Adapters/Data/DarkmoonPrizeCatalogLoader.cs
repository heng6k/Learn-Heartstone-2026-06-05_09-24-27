using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Data
{
    public static class DarkmoonPrizeCatalogLoader
    {
        private const string ResourcePath = "Data/darkmoonPrizes";

        public static DarkmoonPrizeCatalog LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ResourcePath + ".json");
            }

            return LoadFromJson(asset.text);
        }

        public static DarkmoonPrizeCatalog LoadFromJson(string json)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.prizes == null)
            {
                throw new InvalidOperationException("Invalid Darkmoon Prize payload.");
            }

            var definitions = new List<DarkmoonPrizeDefinition>();
            foreach (var raw in payload.prizes)
            {
                definitions.Add(ToDefinition(raw));
            }

            return new DarkmoonPrizeCatalog(definitions);
        }

        private static DarkmoonPrizeDefinition ToDefinition(RawPrize raw)
        {
            return new DarkmoonPrizeDefinition
            {
                CardId = raw.cardId,
                DbfId = raw.dbfId,
                Name = raw.name,
                Text = raw.text,
                Tier = raw.tier,
                ImagePath = raw.imagePath,
                ImageUrl = raw.imageUrl,
                ImplementationStatus = MapImplementationStatus(raw.implementationStatus),
                Keywords = MapKeywords(raw.keywords),
                EffectIds = raw.effectIds == null ? new List<string>() : new List<string>(raw.effectIds),
                Tags = raw.tags == null ? new List<string>() : new List<string>(raw.tags),
                SourcePool = raw.sourcePool
            };
        }

        private static DarkmoonPrizeImplementationStatus MapImplementationStatus(string value)
        {
            switch (value)
            {
                case "Implemented":
                case "implemented":
                    return DarkmoonPrizeImplementationStatus.Implemented;
                default:
                    return DarkmoonPrizeImplementationStatus.Proxy;
            }
        }

        private static List<Keyword> MapKeywords(List<string> raw)
        {
            var keywords = new List<Keyword>();
            foreach (var value in raw ?? new List<string>())
            {
                if (!TryMapKeyword(value, out var keyword) || keywords.Contains(keyword))
                {
                    continue;
                }

                keywords.Add(keyword);
            }

            return keywords;
        }

        private static bool TryMapKeyword(string value, out Keyword keyword)
        {
            switch (value)
            {
                case "Discover":
                case "DISCOVER":
                    keyword = Keyword.Discover;
                    return true;
                case "DivineShield":
                case "DIVINE_SHIELD":
                    keyword = Keyword.DivineShield;
                    return true;
                case "Windfury":
                case "WINDFURY":
                    keyword = Keyword.Windfury;
                    return true;
                case "Taunt":
                case "TAUNT":
                    keyword = Keyword.Taunt;
                    return true;
                default:
                    keyword = Keyword.Trigger;
                    return false;
            }
        }

        [Serializable]
        private sealed class RawPayload
        {
            public int count;
            public string sourceUrl;
            public string snapshotDate;
            public List<RawPrize> prizes;
        }

        [Serializable]
        private sealed class RawPrize
        {
            public string cardId;
            public int dbfId;
            public string name;
            public string text;
            public int tier;
            public string imagePath;
            public string imageUrl;
            public string implementationStatus;
            public List<string> keywords;
            public List<string> effectIds;
            public List<string> tags;
            public string sourcePool;
        }
    }
}
