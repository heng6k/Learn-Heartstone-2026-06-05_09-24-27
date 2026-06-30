using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Data
{
    public static class AnomalyCatalogLoader
    {
        private const string ResourcePath = "Data/battlegroundsAnomalies";

        public static AnomalyCatalog LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ResourcePath + ".json");
            }

            return LoadFromJson(asset.text);
        }

        public static AnomalyCatalog LoadFromJson(string json)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.anomalies == null)
            {
                throw new InvalidOperationException("Invalid Battlegrounds anomaly payload.");
            }

            var definitions = new List<AnomalyDefinition>();
            foreach (var raw in payload.anomalies)
            {
                definitions.Add(ToDefinition(raw, payload.snapshotDate, payload.sourceUrl));
            }

            return new AnomalyCatalog(definitions);
        }

        private static AnomalyDefinition ToDefinition(RawAnomaly raw, string snapshotDate, string sourceUrl)
        {
            var definition = new AnomalyDefinition
            {
                Id = string.IsNullOrEmpty(raw.id) ? raw.cardId : raw.id,
                CardId = raw.cardId,
                DbfId = raw.dbfId,
                Name = raw.name,
                Text = raw.text,
                SourcePools = MapPoolVersions(raw.sourcePools),
                EffectFamily = MapEffectFamily(raw.effectFamily),
                ImplementationStatus = MapImplementationStatus(raw.implementationStatus),
                AvailabilityReasons = MapAvailabilityReasons(raw.availabilityReasons),
                Tags = raw.tags == null ? new List<string>() : new List<string>(raw.tags),
                SourceUrls = raw.sourceUrls == null ? new List<string>() : new List<string>(raw.sourceUrls),
                SnapshotDate = string.IsNullOrEmpty(raw.snapshotDate) ? snapshotDate : raw.snapshotDate,
                Notes = raw.notes
            };

            if (!string.IsNullOrEmpty(sourceUrl) && definition.SourceUrls.Count == 0)
            {
                definition.SourceUrls.Add(sourceUrl);
            }

            return definition;
        }

        private static List<AnomalyPoolVersion> MapPoolVersions(List<string> raw)
        {
            var versions = new List<AnomalyPoolVersion>();
            foreach (var value in raw ?? new List<string>())
            {
                switch (value)
                {
                    case "CurrentHsReplay":
                        versions.Add(AnomalyPoolVersion.CurrentHsReplay);
                        break;
                    case "Season5Launch":
                        versions.Add(AnomalyPoolVersion.Season5Launch);
                        break;
                    case "Season5AllBg27":
                        versions.Add(AnomalyPoolVersion.Season5AllBg27);
                        break;
                    case "AllKnown":
                        versions.Add(AnomalyPoolVersion.AllKnown);
                        break;
                }
            }

            if (versions.Count == 0)
            {
                versions.Add(AnomalyPoolVersion.AllKnown);
            }

            return versions;
        }

        private static List<AnomalyAvailabilityReason> MapAvailabilityReasons(List<string> raw)
        {
            var reasons = new List<AnomalyAvailabilityReason>();
            foreach (var value in raw ?? new List<string>())
            {
                reasons.Add(MapAvailabilityReason(value));
            }

            return reasons;
        }

        private static AnomalyAvailabilityReason MapAvailabilityReason(string value)
        {
            switch (value)
            {
                case "RequiresBuddyMode": return AnomalyAvailabilityReason.RequiresBuddyMode;
                case "RequiresDarkmoonPrizeBackend": return AnomalyAvailabilityReason.RequiresDarkmoonPrizeBackend;
                case "RequiresSecondHeroPowerUi": return AnomalyAvailabilityReason.RequiresSecondHeroPowerUi;
                case "RequiresTier7Pool": return AnomalyAvailabilityReason.RequiresTier7Pool;
                case "RequiresTimewarpPool": return AnomalyAvailabilityReason.RequiresTimewarpPool;
                case "RequiresSharedLobbyChoice": return AnomalyAvailabilityReason.RequiresSharedLobbyChoice;
                case "RequiresYoggWheel": return AnomalyAvailabilityReason.RequiresYoggWheel;
                case "RequiresDuos": return AnomalyAvailabilityReason.RequiresDuos;
                case "RequiresCombatRewrite": return AnomalyAvailabilityReason.RequiresCombatRewrite;
                case "RequiresOfficialDataReview": return AnomalyAvailabilityReason.RequiresOfficialDataReview;
                default: return AnomalyAvailabilityReason.None;
            }
        }

        private static AnomalyImplementationStatus MapImplementationStatus(string value)
        {
            switch (value)
            {
                case "Implemented": return AnomalyImplementationStatus.Implemented;
                case "OfferableWithExactProxy": return AnomalyImplementationStatus.OfferableWithExactProxy;
                case "FrameworkOnly": return AnomalyImplementationStatus.FrameworkOnly;
                case "BlockedByDependency": return AnomalyImplementationStatus.BlockedByDependency;
                case "DebugOnly": return AnomalyImplementationStatus.DebugOnly;
                case "Unsupported": return AnomalyImplementationStatus.Unsupported;
                default: return AnomalyImplementationStatus.Planned;
            }
        }

        private static AnomalyEffectFamily MapEffectFamily(string value)
        {
            switch (value)
            {
                case "Economy": return AnomalyEffectFamily.Economy;
                case "TavernRefresh": return AnomalyEffectFamily.TavernRefresh;
                case "MinionPool": return AnomalyEffectFamily.MinionPool;
                case "Buddy": return AnomalyEffectFamily.Buddy;
                case "DarkmoonPrize": return AnomalyEffectFamily.DarkmoonPrize;
                case "SecondHeroPower": return AnomalyEffectFamily.SecondHeroPower;
                case "Timewarp": return AnomalyEffectFamily.Timewarp;
                case "GeneratedSpell": return AnomalyEffectFamily.GeneratedSpell;
                case "GeneratedMinion": return AnomalyEffectFamily.GeneratedMinion;
                case "DelayedReward": return AnomalyEffectFamily.DelayedReward;
                case "TripleRule": return AnomalyEffectFamily.TripleRule;
                case "CombatRule": return AnomalyEffectFamily.CombatRule;
                case "SharedLobbyChoice": return AnomalyEffectFamily.SharedLobbyChoice;
                case "HeroReplacement": return AnomalyEffectFamily.HeroReplacement;
                default: return AnomalyEffectFamily.Economy;
            }
        }

        [Serializable]
        private sealed class RawPayload
        {
            public string snapshotDate;
            public string sourceUrl;
            public int count;
            public List<RawAnomaly> anomalies;
        }

        [Serializable]
        private sealed class RawAnomaly
        {
            public string id;
            public string cardId;
            public int dbfId;
            public string name;
            public string text;
            public List<string> sourcePools;
            public string effectFamily;
            public string implementationStatus;
            public List<string> availabilityReasons;
            public List<string> tags;
            public List<string> sourceUrls;
            public string snapshotDate;
            public string notes;
        }
    }
}
