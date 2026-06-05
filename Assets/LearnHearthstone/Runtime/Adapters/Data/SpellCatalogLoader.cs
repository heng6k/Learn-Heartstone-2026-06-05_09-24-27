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

        public static SpellCatalog LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ResourcePath + ".json");
            }

            return LoadFromJson(asset.text);
        }

        public static SpellCatalog LoadFromJson(string json)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.spells == null)
            {
                throw new InvalidOperationException("Invalid battlegrounds spell payload.");
            }

            var definitions = new List<TavernSpellDefinition>();
            foreach (var raw in payload.spells)
            {
                definitions.Add(ToDefinition(raw));
            }

            return new SpellCatalog(definitions);
        }

        private static TavernSpellDefinition ToDefinition(RawSpell raw)
        {
            return new TavernSpellDefinition
            {
                Id = raw.id,
                SourceId = raw.sourceId,
                CardNumber = raw.cardNumber,
                Name = raw.name,
                EnglishName = raw.englishName,
                Type = raw.type,
                SpecialType = raw.specialType,
                Category = raw.category,
                Faction = raw.faction,
                AvailableModes = raw.availableModes ?? new List<string>(),
                Cost = raw.cost,
                TavernTier = raw.tavernTier,
                Keywords = raw.keywords ?? new List<string>(),
                Text = raw.text,
                Description = raw.description,
                ImageUrl = raw.imageUrl,
                ImagePath = raw.imagePath,
                EffectIds = raw.effectIds ?? new List<string>(),
                Tags = raw.tags ?? new List<string>(),
                ImplementationStatus = raw.implementationStatus,
                Notes = raw.notes
            };
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
            public List<string> keywords;
            public string text;
            public string description;
            public string imageUrl;
            public string imagePath;
            public List<string> effectIds;
            public List<string> tags;
            public string implementationStatus;
            public string notes;
        }
    }
}
