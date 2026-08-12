using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LearnHearthstone.Domain.Models
{
    public static class GameVersionIds
    {
        public const string LegacyCompositeSandbox = "legacy-composite-sandbox-v1";
        public const string Season14Preview = "36.2-preview";
    }

    public static class RulesetIds
    {
        public const string LegacyCompositeSandbox = "ruleset-legacy-composite-v1";
        public const string Season14Preview = "ruleset-36.2-preview-v1";
    }

    public static class ContentSetIds
    {
        public const string LegacyCompositeSandbox = "content-legacy-composite-v1";
        public const string Season14Preview = "content-36.2-preview-v1";
    }

    public static class VenomousEffectRevisions
    {
        public const string LegacySingleUse = "keyword.venomous@legacy-single-use";
        public const string PerCombat = "keyword.venomous@36.2-per-combat";
    }

    public enum GameVersionOfficialStatus
    {
        Unofficial,
        Announced,
        Released,
        Archived
    }

    public enum GameVersionImplementationStatus
    {
        Planned,
        ContentOnly,
        Partial,
        Complete,
        Verified
    }

    public enum EntityKind
    {
        Hero,
        Minion,
        TavernSpell,
        Trinket,
        DarkGift,
        TimewarpedTavern
    }

    public sealed class GameVersionDefinition
    {
        public GameVersionDefinition(
            string id,
            string displayName,
            DateTime releaseDateUtc,
            GameVersionOfficialStatus officialStatus,
            GameVersionImplementationStatus implementationStatus,
            string rulesetId,
            string contentSetId,
            string changeSummary)
        {
            Id = Required(id, nameof(id));
            DisplayName = Required(displayName, nameof(displayName));
            ReleaseDateUtc = releaseDateUtc.Kind == DateTimeKind.Utc ? releaseDateUtc : releaseDateUtc.ToUniversalTime();
            OfficialStatus = officialStatus;
            ImplementationStatus = implementationStatus;
            RulesetId = Required(rulesetId, nameof(rulesetId));
            ContentSetId = Required(contentSetId, nameof(contentSetId));
            ChangeSummary = changeSummary ?? string.Empty;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public DateTime ReleaseDateUtc { get; }
        public GameVersionOfficialStatus OfficialStatus { get; }
        public GameVersionImplementationStatus ImplementationStatus { get; }
        public string RulesetId { get; }
        public string ContentSetId { get; }
        public string ChangeSummary { get; }
        public bool IsDefaultCandidate => ImplementationStatus == GameVersionImplementationStatus.Verified;

        private static string Required(string value, string parameterName)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Value is required.", parameterName)
                : value;
        }
    }

    public sealed class GameVersionSummaryViewModel
    {
        public GameVersionSummaryViewModel(GameVersionDefinition version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            Id = version.Id;
            DisplayName = version.DisplayName;
            ReleaseDateUtc = version.ReleaseDateUtc;
            OfficialStatus = version.OfficialStatus;
            ImplementationStatus = version.ImplementationStatus;
            ChangeSummary = version.ChangeSummary;
            IsDefaultCandidate = version.IsDefaultCandidate;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public DateTime ReleaseDateUtc { get; }
        public GameVersionOfficialStatus OfficialStatus { get; }
        public GameVersionImplementationStatus ImplementationStatus { get; }
        public string ChangeSummary { get; }
        public bool IsDefaultCandidate { get; }
    }

    public sealed class RulesetDefinition
    {
        public RulesetDefinition(
            string id,
            int schemaVersion,
            IEnumerable<string> ruleFlags = null,
            string turnSchedule = null,
            IEnumerable<string> mechanicProfiles = null,
            string compatibilityPolicy = null,
            DarkGiftProfile darkGiftProfile = null,
            string venomousEffectRevision = VenomousEffectRevisions.LegacySingleUse,
            IEnumerable<string> allowedSetupMechanicIds = null,
            IEnumerable<string> defaultSetupMechanicIds = null)
        {
            Id = Required(id, nameof(id));
            SchemaVersion = schemaVersion > 0
                ? schemaVersion
                : throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            RuleFlags = ReadOnly(ruleFlags);
            TurnSchedule = turnSchedule ?? string.Empty;
            MechanicProfiles = ReadOnly(mechanicProfiles);
            AllowedSetupMechanicIds = ReadOnly(allowedSetupMechanicIds);
            DefaultSetupMechanicIds = ReadOnly(defaultSetupMechanicIds);
            if (DefaultSetupMechanicIds.Except(AllowedSetupMechanicIds, StringComparer.OrdinalIgnoreCase).Any())
            {
                throw new ArgumentException("Default setup mechanics must be allowed by the ruleset.", nameof(defaultSetupMechanicIds));
            }

            CompatibilityPolicy = compatibilityPolicy ?? string.Empty;
            this.darkGiftProfile = darkGiftProfile?.Clone();
            VenomousEffectRevision = string.IsNullOrWhiteSpace(venomousEffectRevision)
                ? VenomousEffectRevisions.LegacySingleUse
                : venomousEffectRevision;
        }

        public string Id { get; }
        public int SchemaVersion { get; }
        public IReadOnlyList<string> RuleFlags { get; }
        public string TurnSchedule { get; }
        public IReadOnlyList<string> MechanicProfiles { get; }
        public IReadOnlyList<string> AllowedSetupMechanicIds { get; }
        public IReadOnlyList<string> DefaultSetupMechanicIds { get; }
        public string CompatibilityPolicy { get; }
        public string VenomousEffectRevision { get; }
        public DarkGiftProfile DarkGiftProfile => darkGiftProfile?.Clone();

        private readonly DarkGiftProfile darkGiftProfile;

        private static string Required(string value, string parameterName)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Value is required.", parameterName)
                : value;
        }

        private static ReadOnlyCollection<string> ReadOnly(IEnumerable<string> values)
        {
            return Array.AsReadOnly((values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
        }
    }

    public sealed class PoolMembershipEntry
    {
        public PoolMembershipEntry(EntityKind kind, string stableEntityId)
        {
            Kind = kind;
            StableEntityId = string.IsNullOrWhiteSpace(stableEntityId)
                ? throw new ArgumentException("Stable entity id is required.", nameof(stableEntityId))
                : stableEntityId;
        }

        public EntityKind Kind { get; }
        public string StableEntityId { get; }
    }

    public sealed class ContentSetDefinition
    {
        public ContentSetDefinition(
            string id,
            IEnumerable<string> heroRevisionIds = null,
            IEnumerable<string> minionRevisionIds = null,
            IEnumerable<string> tavernSpellRevisionIds = null,
            IEnumerable<string> trinketRevisionIds = null,
            IEnumerable<string> darkGiftRevisionIds = null,
            IEnumerable<PoolMembershipEntry> poolMembership = null)
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException("Content set id is required.", nameof(id))
                : id;
            HeroRevisionIds = ReadOnly(heroRevisionIds);
            MinionRevisionIds = ReadOnly(minionRevisionIds);
            TavernSpellRevisionIds = ReadOnly(tavernSpellRevisionIds);
            TrinketRevisionIds = ReadOnly(trinketRevisionIds);
            DarkGiftRevisionIds = ReadOnly(darkGiftRevisionIds);
            PoolMembership = Array.AsReadOnly((poolMembership ?? Enumerable.Empty<PoolMembershipEntry>())
                .Where(entry => entry != null)
                .GroupBy(
                    entry => entry.Kind + "|" + entry.StableEntityId,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(entry => entry.Kind)
                .ThenBy(entry => entry.StableEntityId, StringComparer.Ordinal)
                .ToArray());
        }

        public string Id { get; }
        public IReadOnlyList<string> HeroRevisionIds { get; }
        public IReadOnlyList<string> MinionRevisionIds { get; }
        public IReadOnlyList<string> TavernSpellRevisionIds { get; }
        public IReadOnlyList<string> TrinketRevisionIds { get; }
        public IReadOnlyList<string> DarkGiftRevisionIds { get; }
        public IReadOnlyList<PoolMembershipEntry> PoolMembership { get; }

        public IEnumerable<string> AllRevisionIds =>
            HeroRevisionIds
                .Concat(MinionRevisionIds)
                .Concat(TavernSpellRevisionIds)
                .Concat(TrinketRevisionIds)
                .Concat(DarkGiftRevisionIds);

        private static ReadOnlyCollection<string> ReadOnly(IEnumerable<string> values)
        {
            return Array.AsReadOnly((values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
        }
    }

    public sealed class EntityRevisionDefinition
    {
        public EntityRevisionDefinition(
            EntityKind kind,
            string stableEntityId,
            string revisionId,
            string effectRevision,
            string effectiveVersionId,
            string stats = null,
            string text = null,
            string art = null,
            IEnumerable<string> tags = null,
            IEnumerable<string> effectIds = null,
            string localizedText = null,
            string englishText = null)
        {
            Kind = kind;
            StableEntityId = Required(stableEntityId, nameof(stableEntityId));
            RevisionId = Required(revisionId, nameof(revisionId));
            EffectRevision = Required(effectRevision, nameof(effectRevision));
            EffectiveVersionId = Required(effectiveVersionId, nameof(effectiveVersionId));
            Stats = stats ?? string.Empty;
            Text = text ?? string.Empty;
            Art = art ?? string.Empty;
            Tags = ReadOnly(tags);
            EffectIds = ReadOnly(effectIds);
            LocalizedText = localizedText ?? string.Empty;
            EnglishText = englishText ?? string.Empty;
        }

        public EntityKind Kind { get; }
        public string StableEntityId { get; }
        public string RevisionId { get; }
        public string EffectRevision { get; }
        public string EffectiveVersionId { get; }
        public string Stats { get; }
        public string Text { get; }
        public string Art { get; }
        public IReadOnlyList<string> Tags { get; }
        public IReadOnlyList<string> EffectIds { get; }
        public string LocalizedText { get; }
        public string EnglishText { get; }

        private static string Required(string value, string parameterName)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Value is required.", parameterName)
                : value;
        }

        private static ReadOnlyCollection<string> ReadOnly(IEnumerable<string> values)
        {
            return Array.AsReadOnly((values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
        }
    }
}
