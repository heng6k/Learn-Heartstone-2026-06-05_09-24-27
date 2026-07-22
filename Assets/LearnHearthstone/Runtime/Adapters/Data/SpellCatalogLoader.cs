using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Data
{
    public static class SpellCatalogLoader
    {
        private const string ResourcePath = "Data/battlegroundsSpells";

        public static SpellCatalog LoadFromResources(bool useEnglish = false)
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ResourcePath + ".json");
            }

            return LoadFromJson(asset.text, useEnglish);
        }

        public static SpellCatalog LoadFromJson(string json, bool useEnglish = false)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.spells == null)
            {
                throw new InvalidOperationException("Invalid battlegrounds spell payload.");
            }

            var definitions = new List<TavernSpellDefinition>();
            foreach (var raw in payload.spells)
            {
                definitions.Add(ToDefinition(raw, useEnglish));
            }

            return new SpellCatalog(definitions);
        }

        private static TavernSpellDefinition ToDefinition(RawSpell raw, bool useEnglish)
        {
            var definition = new TavernSpellDefinition
            {
                Id = raw.id,
                SourceId = raw.sourceId,
                CardNumber = raw.cardNumber,
                Name = Localized(raw.name, raw.englishName, useEnglish, raw.cardNumber + ".name"),
                EnglishName = raw.englishName,
                Type = raw.type,
                SpecialType = raw.specialType,
                Category = raw.category,
                Faction = raw.faction,
                AvailableModes = raw.availableModes ?? new List<string>(),
                Cost = raw.cost,
                TavernTier = raw.tavernTier,
                InPool = raw.inPool != 0,
                Keywords = raw.keywords ?? new List<string>(),
                Text = Localized(raw.text, raw.englishText, useEnglish, raw.cardNumber + ".text"),
                EnglishText = raw.englishText,
                Description = raw.description,
                ImageUrl = raw.imageUrl,
                ImagePath = raw.imagePath,
                EffectIds = raw.effectIds ?? new List<string>(),
                Tags = raw.tags == null || raw.tags.Count == 0 ? InferTags(raw) : raw.tags,
                CardTemplate = ParseEnum(raw.cardTemplate, SpellCardTemplate.Auto),
                TargetTemplate = ParseEnum(raw.targetTemplate, SpellTargetTemplate.Auto),
                EffectTemplate = ParseEnum(raw.effectTemplate, SpellEffectTemplate.Auto),
                ImplementationStatus = raw.implementationStatus,
                Notes = raw.notes
            };

            ApplyTargetingCorrections(definition);
            definition.CardTemplate = SpellBehaviorTemplate.ResolveCardTemplate(definition);
            definition.TargetTemplate = SpellBehaviorTemplate.ResolveTargetTemplate(definition);
            definition.EffectTemplate = SpellBehaviorTemplate.ResolveEffectTemplate(definition);
            return definition;
        }

        private static string Localized(string chinese, string english, bool useEnglish, string key)
        {
            if (!useEnglish)
            {
                return chinese;
            }

            return string.IsNullOrWhiteSpace(english) ? "[Missing en-US: " + key + "]" : english;
        }

        private static void ApplyTargetingCorrections(TavernSpellDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (definition.Tags == null)
            {
                definition.Tags = new List<string>();
            }

            switch (definition.CardNumber)
            {
                case "122184":
                    definition.Tags.RemoveAll(tag => string.Equals(tag, "targeted_spell", StringComparison.OrdinalIgnoreCase));
                    definition.TargetTemplate = SpellTargetTemplate.None;
                    break;
                case "130311":
                case "130312":
                case "131218":
                    Add(definition.Tags, "targeted_spell");
                    definition.TargetTemplate = SpellTargetTemplate.FriendlyMinion;
                    break;
            }
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return Enum.TryParse(value, true, out TEnum parsed) ? parsed : fallback;
        }

        private static List<string> InferTags(RawSpell raw)
        {
            var tags = new List<string> { "tavern_spell", "tier_" + raw.tavernTier };
            var text = raw.text ?? string.Empty;

            if (text.Contains("使一个随从"))
            {
                Add(tags, "targeted_spell");
            }

            if (text.Contains("获得+") || text.Contains("获得 +") || text.Contains("+"))
            {
                Add(tags, "buff_spell");
            }

            if (text.Contains("攻击力"))
            {
                Add(tags, "attack_buff");
            }

            if (text.Contains("生命值"))
            {
                Add(tags, "health_buff");
            }

            if (text.Contains("嘲讽") || text.Contains("圣盾"))
            {
                Add(tags, "keyword_grant");
            }

            if (text.Contains("发现"))
            {
                Add(tags, "discover_spell");
            }

            if (text.Contains("随机获取") || text.Contains("获取一张"))
            {
                Add(tags, "card_generator");
            }

            if (text.Contains("酒馆中的"))
            {
                Add(tags, "shop_spell");
            }

            if (text.Contains("偷取"))
            {
                Add(tags, "steal_spell");
            }

            if (text.Contains("铸币"))
            {
                Add(tags, "economy_spell");
            }

            switch (raw.cardNumber)
            {
                case "100596":
                    Add(tags, "targeted_attack_buff");
                    break;
                case "103791":
                    Add(tags, "targeted_health_buff");
                    Add(tags, "taunt_grant");
                    break;
                case "105752":
                    Add(tags, "targeted_stat_buff");
                    break;
                case "104436":
                    Add(tags, "gain_gold");
                    break;
                case "104029":
                    Add(tags, "max_gold_growth");
                    Add(tags, "economy_spell");
                    break;
                case "104446":
                    Add(tags, "free_refresh");
                    Add(tags, "refresh_spell");
                    break;
                case "104559":
                    Add(tags, "health_cost");
                    Add(tags, "gain_gold");
                    Add(tags, "economy_spell");
                    break;
                case "127288":
                    Add(tags, "discover_spell");
                    Add(tags, "current_tier_discover");
                    Add(tags, "hand_lock");
                    break;
                case "105664":
                    Add(tags, "targeted_spell");
                    Add(tags, "same_tribe_generator");
                    Add(tags, "card_generator");
                    break;
                case "103793":
                    Add(tags, "random_tier_1_minion");
                    break;
                case "122864":
                    Add(tags, "discover_tier_1_minion");
                    break;
                case "105903":
                    Add(tags, "shop_buff");
                    break;
                case "104502":
                    Add(tags, "shop_steal");
                    break;
            }

            return tags;
        }

        private static void Add(List<string> tags, string tag)
        {
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        [Serializable]
        private sealed class RawPayload
        {
            public int count;
            public List<RawSpell> spells;
        }

        [Serializable]
        private sealed class RawSpell
        {
            public string id;
            public int sourceId;
            public string cardNumber;
            public string name;
            public string englishName;
            public string type;
            public string specialType;
            public string category;
            public string faction;
            public List<string> availableModes;
            public int cost;
            public int tavernTier;
            public int inPool = 1;
            public List<string> keywords;
            public string text;
            public string englishText;
            public string description;
            public string imageUrl;
            public string imagePath;
            public List<string> effectIds;
            public List<string> tags;
            public string cardTemplate;
            public string targetTemplate;
            public string effectTemplate;
            public string implementationStatus;
            public string notes;
        }
    }
}
