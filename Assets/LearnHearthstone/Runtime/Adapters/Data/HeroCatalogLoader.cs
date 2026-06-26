using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Data
{
    public static class HeroCatalogLoader
    {
        private const string ResourcePath = "Data/battlegroundsHeroes";

        public static HeroCatalog LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ResourcePath + ".json");
            }

            return LoadFromJson(asset.text);
        }

        public static HeroCatalog LoadFromJson(string json)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.heroes == null)
            {
                throw new InvalidOperationException("Invalid battlegrounds hero payload.");
            }

            var definitions = new List<HeroDefinition>();
            foreach (var raw in payload.heroes)
            {
                definitions.Add(ToDefinition(raw));
            }

            return new HeroCatalog(definitions);
        }

        private static HeroDefinition ToDefinition(RawHero raw)
        {
            return new HeroDefinition
            {
                HeroCardId = raw.heroCardId,
                HeroDbfId = raw.heroDbfId,
                Name = raw.name,
                Health = raw.health > 0 ? raw.health : 30,
                Armor = Math.Max(0, raw.armor),
                ImagePath = raw.imagePath,
                HeroPower = ToHeroPower(raw.heroPower),
                Buddy = ToBuddy(raw.buddy),
                MissingBuddyMapping = raw.missingBuddyMapping,
                MissingHeroPowerMapping = raw.missingHeroPowerMapping
            };
        }

        private static HeroPowerDefinition ToHeroPower(RawHeroPower raw)
        {
            if (raw == null)
            {
                return null;
            }

            return new HeroPowerDefinition
            {
                CardId = raw.cardId,
                DbfId = raw.dbfId,
                Name = raw.name,
                Cost = Math.Max(0, raw.cost),
                Text = raw.text,
                ImagePath = raw.imagePath,
                PrimaryCategory = ParseEnum(raw.primaryCategory, HeroPowerCategory.Other),
                Tags = raw.tags ?? new List<string>(),
                ReplacementEligibility = ParseEnum(raw.replacementEligibility, HeroPowerReplacementEligibility.Disabled)
            };
        }

        private static HeroBuddyDefinition ToBuddy(RawBuddy raw)
        {
            if (raw == null)
            {
                return null;
            }

            return new HeroBuddyDefinition
            {
                CardId = raw.cardId,
                DbfId = raw.dbfId,
                Name = raw.name,
                TavernTier = Math.Max(0, raw.tavernTier),
                Attack = Math.Max(0, raw.attack),
                Health = Math.Max(0, raw.health),
                Text = raw.text,
                ImagePath = raw.imagePath,
                Tribes = MapTribes(raw.tribes),
                Keywords = MapKeywords(raw.keywords),
                ExcludedFromBuddyDiscover = raw.excludedFromBuddyDiscover
            };
        }

        private static List<Tribe> MapTribes(List<string> raw)
        {
            var tribes = new List<Tribe>();
            if (raw != null)
            {
                foreach (var tribe in raw)
                {
                    if (Enum.TryParse(tribe, true, out Tribe mapped) && !tribes.Contains(mapped))
                    {
                        tribes.Add(mapped);
                    }
                }
            }

            if (tribes.Count == 0)
            {
                tribes.Add(Tribe.None);
            }

            return tribes;
        }

        private static List<Keyword> MapKeywords(List<string> raw)
        {
            var keywords = new List<Keyword>();
            if (raw == null)
            {
                return keywords;
            }

            foreach (var keyword in raw)
            {
                if (Enum.TryParse(keyword, true, out Keyword mapped) && !keywords.Contains(mapped))
                {
                    keywords.Add(mapped);
                }
            }

            return keywords;
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse(value, true, out TEnum parsed) ? parsed : fallback;
        }

        [Serializable]
        private sealed class RawPayload
        {
            public string sourcePage;
            public string cardsSource;
            public string heroStatsSource;
            public string generatedAt;
            public List<RawHero> heroes;
        }

        [Serializable]
        private sealed class RawHero
        {
            public string heroCardId;
            public int heroDbfId;
            public string name;
            public int health;
            public int armor;
            public string imagePath;
            public RawHeroPower heroPower;
            public RawBuddy buddy;
            public bool missingBuddyMapping;
            public bool missingHeroPowerMapping;
        }

        [Serializable]
        private sealed class RawHeroPower
        {
            public string cardId;
            public int dbfId;
            public string name;
            public int cost;
            public string text;
            public string imagePath;
            public string primaryCategory;
            public List<string> tags;
            public string replacementEligibility;
        }

        [Serializable]
        private sealed class RawBuddy
        {
            public string cardId;
            public int dbfId;
            public string name;
            public int tavernTier;
            public int attack;
            public int health;
            public string text;
            public string imagePath;
            public List<string> tribes;
            public List<string> keywords;
            public bool excludedFromBuddyDiscover;
        }
    }
}
