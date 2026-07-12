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
        private const string LocalizationZhCnResourcePath = "Data/battlegroundsAnomalyLocalizationZhCN";

        public static AnomalyCatalog LoadFromResources(bool useEnglish = true)
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ResourcePath + ".json");
            }

            if (useEnglish)
            {
                return LoadFromJson(asset.text);
            }

            var localizationAsset = Resources.Load<TextAsset>(LocalizationZhCnResourcePath);
            if (localizationAsset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + LocalizationZhCnResourcePath + ".json");
            }

            return LoadFromJson(asset.text, localizationAsset.text);
        }

        public static AnomalyCatalog LoadFromJson(string json)
        {
            return LoadFromJson(json, null);
        }

        public static AnomalyCatalog LoadFromJson(string json, string zhCnJson)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.anomalies == null)
            {
                throw new InvalidOperationException("Invalid Battlegrounds anomaly payload.");
            }

            var localizedCards = ParseLocalization(zhCnJson);
            var definitions = new List<AnomalyDefinition>();
            foreach (var raw in payload.anomalies)
            {
                var definition = ToDefinition(raw, payload.snapshotDate, payload.sourceUrl);
                if (localizedCards != null)
                {
                    var cardId = string.IsNullOrEmpty(definition.CardId) ? definition.Id : definition.CardId;
                    if (!localizedCards.TryGetValue(cardId, out var localized) ||
                        string.IsNullOrWhiteSpace(localized.name) ||
                        string.IsNullOrWhiteSpace(localized.text))
                    {
                        throw new InvalidOperationException("Missing zh-CN anomaly localization: " + cardId);
                    }

                    definition.Name = localized.name;
                    definition.Text = localized.text;
                }

                definitions.Add(definition);
            }

            return new AnomalyCatalog(definitions);
        }

        private static Dictionary<string, RawLocalizedCard> ParseLocalization(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var payload = JsonUtility.FromJson<RawLocalizationPayload>(json);
            if (payload == null || payload.cards == null)
            {
                throw new InvalidOperationException("Invalid zh-CN anomaly localization payload.");
            }

            var cards = new Dictionary<string, RawLocalizedCard>(StringComparer.OrdinalIgnoreCase);
            foreach (var card in payload.cards)
            {
                if (card != null && !string.IsNullOrWhiteSpace(card.cardId))
                {
                    cards[card.cardId] = card;
                }
            }

            return cards;
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
                case "Unknown": return AnomalyEffectFamily.Unknown;
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
                case "SinglePlayerChoice": return AnomalyEffectFamily.SinglePlayerChoice;
                case "HeroReplacement": return AnomalyEffectFamily.HeroReplacement;
                default: throw new InvalidOperationException("Unknown anomaly effectFamily: " + value);
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

        [Serializable]
        private sealed class RawLocalizationPayload
        {
            public string source;
            public string generatedAt;
            public int count;
            public List<RawLocalizedCard> cards;
        }

        [Serializable]
        private sealed class RawLocalizedCard
        {
            public string cardId;
            public string name;
            public string text;
        }
    }
}
