using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Data
{
    public static class VersionedContentCatalogLoader
    {
        private const string VersionsResourcePath = "Data/battlegroundsGameVersions";
        private const string RulesetsResourcePath = "Data/battlegroundsRulesets";

        public static VersionedContentCatalog LoadFromResources(
            IEnumerable<MinionDefinition> minions,
            IEnumerable<HeroDefinition> heroes,
            IEnumerable<TavernSpellDefinition> tavernSpells,
            IEnumerable<DarkGiftDefinition> darkGifts)
        {
            var versions = Resources.Load<TextAsset>(VersionsResourcePath);
            var rulesets = Resources.Load<TextAsset>(RulesetsResourcePath);
            if (versions == null)
            {
                throw new InvalidOperationException("Missing Resources/" + VersionsResourcePath + ".json");
            }
            if (rulesets == null)
            {
                throw new InvalidOperationException("Missing Resources/" + RulesetsResourcePath + ".json");
            }
            return LoadFromJson(versions.text, rulesets.text, minions, heroes, tavernSpells, darkGifts);
        }

        public static VersionedContentCatalog LoadFromJson(
            string versionsJson,
            string rulesetsJson,
            IEnumerable<MinionDefinition> minions,
            IEnumerable<HeroDefinition> heroes,
            IEnumerable<TavernSpellDefinition> tavernSpells,
            IEnumerable<DarkGiftDefinition> darkGifts)
        {
            var versionPayload = JsonUtility.FromJson<RawVersionsPayload>(versionsJson);
            var rulesetPayload = JsonUtility.FromJson<RawRulesetsPayload>(rulesetsJson);
            if (versionPayload == null || versionPayload.versions == null || versionPayload.contentSets == null)
            {
                throw new InvalidOperationException("Invalid game versions payload.");
            }
            if (rulesetPayload == null || rulesetPayload.rulesets == null)
            {
                throw new InvalidOperationException("Invalid rulesets payload.");
            }

            var gifts = (darkGifts ?? Enumerable.Empty<DarkGiftDefinition>())
                .Where(item => item != null)
                .Select(item => item.Clone())
                .ToArray();
            var heroDefinitions = (heroes ?? Enumerable.Empty<HeroDefinition>())
                .Where(item => item != null)
                .ToArray();
            var minionDefinitions = (minions ?? Enumerable.Empty<MinionDefinition>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.RevisionId))
                .ToArray();
            var spellDefinitions = (tavernSpells ?? Enumerable.Empty<TavernSpellDefinition>())
                .Where(item => item != null)
                .ToArray();
            var versions = versionPayload.versions.Select(ToVersion).ToArray();
            var rulesets = rulesetPayload.rulesets.Select(ToRuleset).ToArray();
            var contentSets = versionPayload.contentSets
                .Select(raw => ToContentSet(raw, minionDefinitions, heroDefinitions, spellDefinitions, gifts))
                .ToArray();
            var revisions = (versionPayload.entityRevisions ?? new List<RawEntityRevision>())
                .Select(ToRevision)
                .Concat(minionDefinitions.Select(ToRevision))
                .Concat(gifts.Select(ToRevision))
                .ToArray();
            return new VersionedContentCatalog(
                new GameVersionCatalog(versions),
                rulesets,
                contentSets,
                revisions);
        }

        private static GameVersionDefinition ToVersion(RawVersion raw)
        {
            if (raw == null || !DateTime.TryParse(
                    raw.releaseDateUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var releaseDateUtc))
            {
                throw new InvalidOperationException("Game version release date is invalid.");
            }
            return new GameVersionDefinition(
                raw.id,
                raw.displayName,
                releaseDateUtc,
                ParseEnum(raw.officialStatus, GameVersionOfficialStatus.Unofficial),
                ParseEnum(raw.implementationStatus, GameVersionImplementationStatus.Planned),
                raw.rulesetId,
                raw.contentSetId,
                raw.changeSummary);
        }

        private static RulesetDefinition ToRuleset(RawRuleset raw)
        {
            if (raw == null)
            {
                throw new InvalidOperationException("Ruleset payload contains a null definition.");
            }
            return new RulesetDefinition(
                raw.id,
                raw.schemaVersion,
                raw.ruleFlags,
                raw.turnSchedule,
                raw.mechanicProfiles,
                raw.compatibilityPolicy,
                ToDarkGiftProfile(raw.darkGiftProfile),
                raw.venomousEffectRevision,
                raw.allowedSetupMechanicIds,
                raw.defaultSetupMechanicIds);
        }

        private static DarkGiftProfile ToDarkGiftProfile(RawDarkGiftProfile raw)
        {
            if (raw == null)
            {
                return null;
            }
            return new DarkGiftProfile
            {
                Id = raw.id,
                Enabled = raw.enabled,
                NormalEntryStartRound = raw.normalEntryStartRound,
                GoldCost = raw.goldCost,
                UsesPerTurn = raw.usesPerTurn,
                UsesPerGame = raw.usesPerGame,
                OfferCount = raw.offerCount,
                PickCount = raw.pickCount,
                TierRanges = (raw.tierRanges ?? new List<RawTierRange>())
                    .Select(item => new DarkGiftTierRangeRule
                    {
                        FromRound = item.fromRound,
                        MinTier = item.minTier,
                        MaxTier = item.maxTier
                    })
                    .ToList(),
                CandidateFilter = new DarkGiftCandidateFilter
                {
                    BattlecryAllowedFromRound = raw.candidateFilter?.battlecryAllowedFromRound ?? 0,
                    ChooseOneAllowedFromRound = raw.candidateFilter?.chooseOneAllowedFromRound ?? 0,
                    RequiredTags = raw.candidateFilter?.requiredTags ?? new List<string>(),
                    ExcludedTags = raw.candidateFilter?.excludedTags ?? new List<string>(),
                    ExcludedMechanics = raw.candidateFilter?.excludedMechanics ?? new List<string>()
                },
                DeduplicationPolicy = raw.deduplicationPolicy,
                CommonTribeGuarantee = new DarkGiftCommonTribeGuarantee
                {
                    Enabled = raw.commonTribeGuarantee != null && raw.commonTribeGuarantee.enabled,
                    StartRound = raw.commonTribeGuarantee?.startRound ?? 0,
                    MinimumOfferCount = raw.commonTribeGuarantee?.minimumOfferCount ?? 0
                },
                ChoiceQueuePriority = raw.choiceQueuePriority,
                ChoiceQueuePriorityFactStatus = ParseEnum(
                    raw.choiceQueuePriorityFactStatus,
                    DarkGiftOfficialFactStatus.BlockedByOfficialFact),
                AutoChoicePolicy = ParseEnum(raw.autoChoicePolicy, DarkGiftAutoChoicePolicy.PlayerChoice),
                ImplementationStatus = ParseEnum(raw.implementationStatus, DarkGiftImplementationStatus.Planned)
            };
        }

        private static ContentSetDefinition ToContentSet(
            RawContentSet raw,
            IReadOnlyCollection<MinionDefinition> minions,
            IReadOnlyCollection<HeroDefinition> heroes,
            IReadOnlyCollection<TavernSpellDefinition> tavernSpells,
            IReadOnlyCollection<DarkGiftDefinition> gifts)
        {
            if (raw == null)
            {
                throw new InvalidOperationException("Content set payload contains a null definition.");
            }
            var darkGiftRevisionIds = raw.includeAllDarkGiftRevisions
                ? gifts.Select(item => item.RevisionId)
                : raw.darkGiftRevisionIds;
            var explicitMinionRevisionIds = raw.minionRevisionIds ?? new List<string>();
            var minionRevisionIds = raw.includeAllVersionedMinionRevisions
                ? minions.Select(item => item.RevisionId)
                    .Concat(explicitMinionRevisionIds)
                    .Distinct(StringComparer.Ordinal)
                : explicitMinionRevisionIds;
            var memberships = (raw.poolMembership ?? new List<RawPoolMembership>())
                .Select(item => new PoolMembershipEntry(ParseEnum(item.kind, EntityKind.Minion), item.stableEntityId))
                .ToList();
            if (raw.includeAllDarkGiftRevisions)
            {
                memberships.AddRange(gifts.Select(item => new PoolMembershipEntry(EntityKind.DarkGift, item.Id)));
            }
            if (raw.includeAllHeroDefinitions)
            {
                memberships.AddRange(heroes
                    .Where(item => !string.IsNullOrWhiteSpace(item.HeroCardId))
                    .Select(item => new PoolMembershipEntry(EntityKind.Hero, item.HeroCardId)));
            }
            if (raw.includeAllTavernSpellDefinitions)
            {
                memberships.AddRange(tavernSpells
                    .Where(item => !string.IsNullOrWhiteSpace(item.CardNumber) &&
                                   string.Equals(item.Category, "TavernSpell", StringComparison.OrdinalIgnoreCase))
                    .Select(item => new PoolMembershipEntry(EntityKind.TavernSpell, item.CardNumber)));
            }
            return new ContentSetDefinition(
                raw.id,
                raw.heroRevisionIds,
                minionRevisionIds,
                raw.tavernSpellRevisionIds,
                raw.trinketRevisionIds,
                darkGiftRevisionIds,
                memberships);
        }

        private static EntityRevisionDefinition ToRevision(DarkGiftDefinition gift)
        {
            var tags = (gift.AvailabilityTags ?? new List<string>())
                .Concat(gift.CompatibilityTags ?? new List<string>())
                .Concat(new[] { "research-key:" + gift.ResearchKey, "source-level:" + gift.SourceLevel });
            return new EntityRevisionDefinition(
                EntityKind.DarkGift,
                gift.Id,
                gift.RevisionId,
                gift.EffectRevision,
                GameVersionIds.Season14Preview,
                "rounds:" + gift.EarliestOfferRound + "-" + (gift.LatestOfferRound <= 0 ? "infinity" : gift.LatestOfferRound.ToString()),
                gift.Text,
                gift.ImagePath,
                tags,
                gift.EffectIds);
        }

        private static EntityRevisionDefinition ToRevision(MinionDefinition minion)
        {
            var tags = (minion.Tags ?? new List<string>())
                .Concat(new[]
                {
                    "research-key:" + minion.ResearchKey,
                    "source-level:" + minion.SourceLevel,
                    "implementation-status:" + minion.ImplementationStatus,
                    "image-source:" + minion.ImageSource
                });
            return new EntityRevisionDefinition(
                EntityKind.Minion,
                string.IsNullOrWhiteSpace(minion.CardId) ? minion.Id : minion.CardId,
                minion.RevisionId,
                minion.EffectRevision,
                GameVersionIds.Season14Preview,
                "tier:" + minion.TavernTier + ",attack:" + minion.BaseAttack + ",health:" + minion.BaseHealth,
                minion.Text,
                minion.ImagePath,
                tags,
                minion.EffectIds);
        }

        private static EntityRevisionDefinition ToRevision(RawEntityRevision raw)
        {
            if (raw == null)
            {
                throw new InvalidOperationException("Entity revision payload contains a null definition.");
            }
            return new EntityRevisionDefinition(
                ParseEnum(raw.kind, EntityKind.Minion),
                raw.stableEntityId,
                raw.revisionId,
                raw.effectRevision,
                raw.effectiveVersionId,
                raw.stats,
                raw.text,
                raw.art,
                raw.tags,
                raw.effectIds,
                raw.localizedText,
                raw.englishText);
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse(value, true, out TEnum parsed) ? parsed : fallback;
        }

        [Serializable]
        private sealed class RawVersionsPayload
        {
            public int schemaVersion;
            public List<RawVersion> versions;
            public List<RawContentSet> contentSets;
            public List<RawEntityRevision> entityRevisions;
        }

        [Serializable]
        private sealed class RawEntityRevision
        {
            public string kind;
            public string stableEntityId;
            public string revisionId;
            public string effectRevision;
            public string effectiveVersionId;
            public string stats;
            public string text;
            public string art;
            public List<string> tags;
            public List<string> effectIds;
            public string localizedText;
            public string englishText;
        }

        [Serializable]
        private sealed class RawVersion
        {
            public string id;
            public string displayName;
            public string releaseDateUtc;
            public string officialStatus;
            public string implementationStatus;
            public string rulesetId;
            public string contentSetId;
            public string changeSummary;
        }

        [Serializable]
        private sealed class RawContentSet
        {
            public string id;
            public List<string> heroRevisionIds;
            public List<string> minionRevisionIds;
            public List<string> tavernSpellRevisionIds;
            public List<string> trinketRevisionIds;
            public List<string> darkGiftRevisionIds;
            public bool includeAllDarkGiftRevisions;
            public bool includeAllVersionedMinionRevisions;
            public bool includeAllHeroDefinitions;
            public bool includeAllTavernSpellDefinitions;
            public List<RawPoolMembership> poolMembership;
        }

        [Serializable]
        private sealed class RawPoolMembership
        {
            public string kind;
            public string stableEntityId;
        }

        [Serializable]
        private sealed class RawRulesetsPayload
        {
            public int schemaVersion;
            public List<RawRuleset> rulesets;
        }

        [Serializable]
        private sealed class RawRuleset
        {
            public string id;
            public int schemaVersion;
            public List<string> ruleFlags;
            public string turnSchedule;
            public List<string> mechanicProfiles;
            public List<string> allowedSetupMechanicIds;
            public List<string> defaultSetupMechanicIds;
            public string compatibilityPolicy;
            public string venomousEffectRevision;
            public RawDarkGiftProfile darkGiftProfile;
        }

        [Serializable]
        private sealed class RawDarkGiftProfile
        {
            public string id;
            public bool enabled;
            public int normalEntryStartRound;
            public int goldCost;
            public int usesPerTurn;
            public int usesPerGame;
            public int offerCount;
            public int pickCount;
            public List<RawTierRange> tierRanges;
            public RawCandidateFilter candidateFilter;
            public string deduplicationPolicy;
            public RawCommonTribeGuarantee commonTribeGuarantee;
            public int choiceQueuePriority;
            public string choiceQueuePriorityFactStatus;
            public string autoChoicePolicy;
            public string implementationStatus;
        }

        [Serializable]
        private sealed class RawTierRange
        {
            public int fromRound;
            public int minTier;
            public int maxTier;
        }

        [Serializable]
        private sealed class RawCandidateFilter
        {
            public int battlecryAllowedFromRound;
            public int chooseOneAllowedFromRound;
            public List<string> requiredTags;
            public List<string> excludedTags;
            public List<string> excludedMechanics;
        }

        [Serializable]
        private sealed class RawCommonTribeGuarantee
        {
            public bool enabled;
            public int startRound;
            public int minimumOfferCount;
        }
    }
}
