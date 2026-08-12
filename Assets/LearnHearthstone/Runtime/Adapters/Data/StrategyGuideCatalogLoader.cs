using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Data
{
    public static class StrategyGuideCatalogLoader
    {
        private const string ResourcePath = "Data/battlegroundsStrategyGuides";
        private const string ExpansionResourcePath = "Data/battlegroundsStrategyGuidesExpandedTribes";

        public static StrategyGuideCatalog LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ResourcePath + ".json");
            }

            var expansionAsset = Resources.Load<TextAsset>(ExpansionResourcePath);
            if (expansionAsset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ExpansionResourcePath + ".json");
            }

            var payload = ParseDefinition(asset.text);
            var expansion = ParseDefinition(expansionAsset.text);
            if (payload.SchemaVersion != expansion.SchemaVersion)
            {
                throw new InvalidOperationException("Strategy guide resource schema versions do not match.");
            }

            payload.Guides = payload.Guides ?? new List<StrategyGuideDefinition>();
            payload.Opponents = payload.Opponents ?? new List<StrategyGuideOpponentDefinition>();
            payload.Guides.AddRange(expansion.Guides ?? new List<StrategyGuideDefinition>());
            payload.Opponents.AddRange(expansion.Opponents ?? new List<StrategyGuideOpponentDefinition>());
            payload.CatalogRevisionId = payload.CatalogRevisionId + "+" + expansion.CatalogRevisionId;
            return new StrategyGuideCatalog(payload);
        }

        public static StrategyGuideCatalog LoadFromJson(string json)
        {
            return new StrategyGuideCatalog(ParseDefinition(json));
        }

        private static StrategyGuideCatalogDefinition ParseDefinition(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Strategy guide JSON is required.", nameof(json));
            }

            var payload = JsonUtility.FromJson<StrategyGuideCatalogDefinition>(json);
            if (payload == null)
            {
                throw new InvalidOperationException("Invalid strategy guide payload.");
            }

            return payload;
        }
    }
}
