using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Data
{
    public static class MinionCatalogLoader
    {
        private const string ResourcePath = "Data/battlegroundsMinions";

        public static MinionCatalog LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ResourcePath + ".json");
            }

            return LoadFromJson(asset.text);
        }

        public static MinionCatalog LoadFromJson(string json)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.minions == null)
            {
                throw new InvalidOperationException("Invalid battlegrounds minion payload.");
            }

            var definitions = new List<MinionDefinition>();
            foreach (var raw in payload.minions)
            {
                definitions.Add(ToDefinition(raw));
            }

            return new MinionCatalog(definitions);
        }

        private static MinionDefinition ToDefinition(RawMinion raw)
        {
            var definition = new MinionDefinition
            {
                Id = raw.id,
                CardId = raw.cardId,
                DbfId = raw.dbfId,
                Name = raw.name,
                TavernTier = raw.tavernTier,
                BaseAttack = raw.attack,
                BaseHealth = raw.health,
                Tribes = MapTribes(raw.tribes),
                Keywords = MapKeywords(raw.keywords),
                Text = raw.text,
                InPool = raw.inPool == 1,
                PoolCount = raw.poolCount,
                ImagePath = "CardImages/" + raw.cardId,
                EffectIds = raw.effectIds == null ? new List<string>() : new List<string>(raw.effectIds)
            };

            if (raw.golden != null && !string.IsNullOrEmpty(raw.golden.cardId))
            {
                definition.Golden = new GoldenMinionDefinition
                {
                    CardId = raw.golden.cardId,
                    DbfId = raw.golden.dbfId,
                    BaseAttack = raw.golden.attack,
                    BaseHealth = raw.golden.health,
                    Text = raw.golden.text,
                    Keywords = MapKeywords(raw.golden.keywords)
                };
            }

            return definition;
        }

        private static List<Tribe> MapTribes(List<string> raw)
        {
            var tribes = new List<Tribe>();
            if (raw == null)
            {
                tribes.Add(Tribe.None);
                return tribes;
            }

            foreach (var tribe in raw)
            {
                tribes.Add(MapTribe(tribe));
            }

            if (tribes.Count == 0)
            {
                tribes.Add(Tribe.None);
            }

            return tribes;
        }

        private static Tribe MapTribe(string value)
        {
            switch (value)
            {
                case "野兽": return Tribe.Beast;
                case "鱼人": return Tribe.Murloc;
                case "机械": return Tribe.Mech;
                case "恶魔": return Tribe.Demon;
                case "龙": return Tribe.Dragon;
                case "海盗": return Tribe.Pirate;
                case "元素": return Tribe.Elemental;
                case "野猪人": return Tribe.Quilboar;
                case "亡灵": return Tribe.Undead;
                case "纳迦": return Tribe.Naga;
                case "全部种族": return Tribe.All;
                default: return Tribe.None;
            }
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
                var mapped = MapKeyword(keyword);
                if (!keywords.Contains(mapped))
                {
                    keywords.Add(mapped);
                }
            }

            return keywords;
        }

        private static Keyword MapKeyword(string value)
        {
            switch (value)
            {
                case "嘲讽": return Keyword.Taunt;
                case "圣盾": return Keyword.DivineShield;
                case "剧毒": return Keyword.Poisonous;
                case "烈毒": return Keyword.Venomous;
                case "复生": return Keyword.Reborn;
                case "亡语": return Keyword.Deathrattle;
                case "战吼": return Keyword.Battlecry;
                case "风怒": return Keyword.Windfury;
                case "顺劈": return Keyword.Cleave;
                case "磁力": return Keyword.Magnetic;
                case "复仇": return Keyword.Avenge;
                case "战斗开始时": return Keyword.StartOfCombat;
                case "回合结束时": return Keyword.EndOfTurn;
                case "进击": return Keyword.Rally;
                case "塑造法术": return Keyword.Spellcraft;
                case "鲜血宝石": return Keyword.BloodGem;
                case "发现": return Keyword.Discover;
                case "刷新": return Keyword.Refresh;
                case "传递": return Keyword.Pass;
                case "光环": return Keyword.Aura;
                case "吞食": return Keyword.Devour;
                case "酒馆法术": return Keyword.TavernSpell;
                case "抉择": return Keyword.ChooseOne;
                case "隐藏亡语": return Keyword.HiddenDeathrattle;
                case "潜行": return Keyword.Stealth;
                default: return Keyword.Trigger;
            }
        }

        [Serializable]
        private sealed class RawPayload
        {
            public int count;
            public List<RawMinion> minions;
        }

        [Serializable]
        private sealed class RawMinion
        {
            public string id;
            public string cardId;
            public int dbfId;
            public string name;
            public int tavernTier;
            public int attack;
            public int health;
            public List<string> tribes;
            public List<string> keywords;
            public string text;
            public int inPool;
            public int poolCount;
            public List<string> effectIds;
            public RawGolden golden;
        }

        [Serializable]
        private sealed class RawGolden
        {
            public string cardId;
            public int dbfId;
            public int attack;
            public int health;
            public string text;
            public List<string> keywords;
        }
    }
}
