using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Content
{
    public sealed class ResolvedGameVersion
    {
        internal ResolvedGameVersion(
            GameVersionDefinition gameVersion,
            RulesetDefinition ruleset,
            ContentSetDefinition contentSet,
            IEnumerable<EntityRevisionDefinition> entityRevisions,
            GameCatalogSnapshot snapshot,
            string contentFingerprint)
        {
            GameVersion = gameVersion ?? throw new ArgumentNullException(nameof(gameVersion));
            Ruleset = ruleset ?? throw new ArgumentNullException(nameof(ruleset));
            ContentSet = contentSet ?? throw new ArgumentNullException(nameof(contentSet));
            EntityRevisions = Array.AsReadOnly((entityRevisions ?? Enumerable.Empty<EntityRevisionDefinition>()).ToArray());
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            ContentSnapshotId = snapshot.Info.SnapshotId;
            ContentFingerprint = string.IsNullOrWhiteSpace(contentFingerprint)
                ? throw new ArgumentException("Content fingerprint is required.", nameof(contentFingerprint))
                : contentFingerprint;
        }

        public GameVersionDefinition GameVersion { get; }
        public RulesetDefinition Ruleset { get; }
        public ContentSetDefinition ContentSet { get; }
        public IReadOnlyList<EntityRevisionDefinition> EntityRevisions { get; }
        public GameCatalogSnapshot Snapshot { get; }
        public string ContentSnapshotId { get; }
        public string ContentFingerprint { get; }
    }

    public sealed class GameVersionResolver
    {
        private readonly Dictionary<string, RulesetDefinition> rulesets;
        private readonly Dictionary<string, ContentSetDefinition> contentSets;
        private readonly Dictionary<string, EntityRevisionDefinition> revisions;

        public GameVersionResolver(
            GameVersionCatalog versions,
            IEnumerable<RulesetDefinition> rulesets,
            IEnumerable<ContentSetDefinition> contentSets,
            IEnumerable<EntityRevisionDefinition> revisions)
        {
            Versions = versions ?? throw new ArgumentNullException(nameof(versions));
            this.rulesets = Index(rulesets, item => item.Id, "ruleset");
            this.contentSets = Index(contentSets, item => item.Id, "content set");
            this.revisions = Index(revisions, item => item.RevisionId, "entity revision");
        }

        public GameVersionCatalog Versions { get; }

        public ResolvedGameVersion Resolve(string versionId, GameCatalogSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var version = Versions.Get(versionId);
            var ruleset = Get(rulesets, version.RulesetId, "ruleset");
            var contentSet = Get(contentSets, version.ContentSetId, "content set");
            ValidateSnapshotIdentity(snapshot.Info, version);
            var selectedRevisions = contentSet.AllRevisionIds.Select(id => Get(revisions, id, "entity revision")).ToArray();
            var resolvedSnapshot = ApplyContentSet(snapshot, contentSet, selectedRevisions);
            var fingerprint = string.IsNullOrWhiteSpace(snapshot.Info.ContentFingerprint)
                ? ComputeFingerprint(version, ruleset, contentSet, selectedRevisions, snapshot.Info.ContentVersion)
                : snapshot.Info.ContentFingerprint;
            return new ResolvedGameVersion(version, ruleset, contentSet, selectedRevisions, resolvedSnapshot, fingerprint);
        }

        public static GameVersionResolver CreateBuiltIn()
        {
            return new GameVersionResolver(
                GameVersionCatalog.CreateBuiltIn(),
                new[]
                {
                    new RulesetDefinition(
                        RulesetIds.LegacyCompositeSandbox,
                        1,
                        compatibilityPolicy: "legacy-composite",
                        allowedSetupMechanicIds: SetupMechanicIds.LegacyComposite,
                        defaultSetupMechanicIds: Array.Empty<string>()),
                    new RulesetDefinition(
                        RulesetIds.Season14Preview,
                        1,
                        mechanicProfiles: new[] { DarkGiftProfiles.Season14PreviewId },
                        compatibilityPolicy: "preview-explicit-selection",
                        darkGiftProfile: DarkGiftProfiles.CreateSeason14Preview(),
                        venomousEffectRevision: VenomousEffectRevisions.PerCombat,
                        allowedSetupMechanicIds: SetupMechanicIds.Season14,
                        defaultSetupMechanicIds: SetupMechanicIds.Season14)
                },
                new[]
                {
                    new ContentSetDefinition(ContentSetIds.LegacyCompositeSandbox),
                    new ContentSetDefinition(ContentSetIds.Season14Preview)
                },
                Array.Empty<EntityRevisionDefinition>());
        }

        private static void ValidateSnapshotIdentity(ContentSnapshotInfo info, GameVersionDefinition version)
        {
            if (!string.IsNullOrWhiteSpace(info.GameVersionId) &&
                !string.Equals(info.GameVersionId, version.Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Content snapshot game version does not match the requested version.");
            }
            if (!string.IsNullOrWhiteSpace(info.RulesetId) &&
                !string.Equals(info.RulesetId, version.RulesetId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Content snapshot ruleset does not match the requested version.");
            }
        }

        private static GameCatalogSnapshot ApplyContentSet(
            GameCatalogSnapshot snapshot,
            ContentSetDefinition contentSet,
            IReadOnlyCollection<EntityRevisionDefinition> selectedRevisions)
        {
            ValidatePoolMembership(snapshot.Chinese, contentSet);
            var revisionsByEntity = selectedRevisions
                .GroupBy(revision => EntityKey(revision.Kind, revision.StableEntityId), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count() == 1
                        ? group.Single()
                        : throw new InvalidDataException("Content set selects multiple revisions for " + group.Key + "."),
                    StringComparer.OrdinalIgnoreCase);
            var minionPool = Membership(contentSet, EntityKind.Minion);
            var spellPool = Membership(contentSet, EntityKind.TavernSpell);
            var heroPool = Membership(contentSet, EntityKind.Hero);
            var trinketPool = Membership(contentSet, EntityKind.Trinket);
            var timewarpedPool = Membership(contentSet, EntityKind.TimewarpedTavern);
            return new GameCatalogSnapshot(
                snapshot.Info,
                Apply(snapshot.Chinese, revisionsByEntity, heroPool, minionPool, spellPool, trinketPool, timewarpedPool, false),
                Apply(snapshot.English, revisionsByEntity, heroPool, minionPool, spellPool, trinketPool, timewarpedPool, true),
                snapshot.VersionedContent);
        }

        private static GameCatalogSet Apply(
            GameCatalogSet source,
            IReadOnlyDictionary<string, EntityRevisionDefinition> revisionsByEntity,
            HashSet<string> heroPool,
            HashSet<string> minionPool,
            HashSet<string> spellPool,
            HashSet<string> trinketPool,
            HashSet<string> timewarpedPool,
            bool useEnglish)
        {
            var hasHeroPool = heroPool.Count > 0;
            var hasMinionPool = minionPool.Count > 0;
            var hasSpellPool = spellPool.Count > 0;
            var hasTrinketPool = trinketPool.Count > 0;
            var hasTimewarpedPool = timewarpedPool.Count > 0;
            var heroes = source.Heroes.AllHeroes.Select(definition =>
            {
                var clone = Clone(definition);
                var stableId = definition.HeroCardId;
                if (hasHeroPool)
                {
                    clone.InPool = heroPool.Contains(stableId);
                }
                if (revisionsByEntity.TryGetValue(EntityKey(EntityKind.Hero, stableId), out var revision))
                {
                    clone.RevisionId = revision.RevisionId;
                    clone.EffectRevision = revision.EffectRevision;
                    ApplyHeroStats(clone, revision.Stats);
                    ApplyHeroRevision(clone, revision, useEnglish);
                }
                return clone;
            });
            var minions = source.Minions.All.Select(definition =>
            {
                var clone = Clone(definition);
                var stableId = StableMinionId(definition);
                if (hasMinionPool)
                {
                    clone.InPool = minionPool.Contains(stableId);
                }
                if (revisionsByEntity.TryGetValue(EntityKey(EntityKind.Minion, stableId), out var revision))
                {
                    clone.RevisionId = revision.RevisionId;
                    clone.EffectRevision = revision.EffectRevision;
                    ApplyMinionStats(clone, revision.Stats);
                    ApplyMinionRevision(clone, revision, useEnglish);
                }
                return clone;
            });
            var spells = source.Spells.All.Select(definition =>
            {
                var clone = Clone(definition);
                var stableId = StableSpellId(definition);
                if (hasSpellPool)
                {
                    clone.InPool = spellPool.Contains(stableId);
                }
                if (revisionsByEntity.TryGetValue(EntityKey(EntityKind.TavernSpell, stableId), out var revision))
                {
                    clone.RevisionId = revision.RevisionId;
                    clone.EffectRevision = revision.EffectRevision;
                    ApplySpellStats(clone, revision.Stats);
                    ApplySpellText(clone, revision, useEnglish);
                }
                return clone;
            });
            var trinkets = source.Trinkets.All.Select(definition =>
            {
                var clone = Clone(definition);
                var stableId = StableTrinketId(definition);
                if (hasTrinketPool)
                {
                    if (!trinketPool.Contains(stableId))
                    {
                        clone.OfferPoolStatus = TrinketOfferPoolStatus.Disabled;
                    }
                    else if (clone.ImplementationStatus == TrinketImplementationStatus.Implemented &&
                             clone.OfferPoolStatus != TrinketOfferPoolStatus.HiddenEffectOnly)
                    {
                        clone.OfferPoolStatus = TrinketOfferPoolStatus.Offerable;
                    }
                }
                if (revisionsByEntity.TryGetValue(EntityKey(EntityKind.Trinket, stableId), out var revision))
                {
                    ApplyTrinketStats(clone, revision.Stats);
                    ApplyTrinketText(clone, revision, useEnglish);
                    if (revision.EffectIds.Count > 0)
                    {
                        clone.EffectIds = new List<string>(revision.EffectIds);
                    }
                    if (!string.IsNullOrWhiteSpace(revision.Art))
                    {
                        clone.ImagePath = revision.Art;
                    }
                }
                return clone;
            });
            var timewarpedCards = source.TimewarpedTavern.All.Select(definition =>
            {
                var clone = Clone(definition);
                if (hasTimewarpedPool)
                {
                    if (timewarpedPool.Contains(definition.CardId))
                    {
                        clone.PoolStatus = "current";
                    }
                    else if (string.Equals(definition.PoolStatus, "current", StringComparison.OrdinalIgnoreCase))
                    {
                        clone.PoolStatus = "removed";
                    }
                }
                return clone;
            });

            return new GameCatalogSet(
                new MinionCatalog(minions),
                new SpellCatalog(spells),
                new HeroCatalog(heroes),
                new TrinketCatalog(trinkets),
                source.Quests,
                new TimewarpedTavernCatalog(timewarpedCards),
                source.Anomalies,
                source.DarkmoonPrizes,
                source.DarkGifts);
        }

        private static void ValidatePoolMembership(GameCatalogSet source, ContentSetDefinition contentSet)
        {
            foreach (var entry in contentSet.PoolMembership)
            {
                var stableId = entry.StableEntityId;
                var exists = entry.Kind switch
                {
                    EntityKind.Hero => source.Heroes.AllHeroes.Any(item =>
                        string.Equals(item.HeroCardId, stableId, StringComparison.OrdinalIgnoreCase)),
                    EntityKind.Minion => source.Minions.All.Any(item =>
                        string.Equals(StableMinionId(item), stableId, StringComparison.OrdinalIgnoreCase)),
                    EntityKind.TavernSpell => source.Spells.All.Any(item =>
                        string.Equals(StableSpellId(item), stableId, StringComparison.OrdinalIgnoreCase)),
                    EntityKind.TimewarpedTavern => source.TimewarpedTavern.All.Any(item =>
                        string.Equals(item.CardId, stableId, StringComparison.OrdinalIgnoreCase)),
                    EntityKind.Trinket => source.Trinkets.All.Any(item =>
                        string.Equals(item.CardId, stableId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(item.Id, stableId, StringComparison.OrdinalIgnoreCase)),
                    EntityKind.DarkGift => source.DarkGifts.All.Any(item =>
                        string.Equals(item.Id, stableId, StringComparison.OrdinalIgnoreCase)),
                    _ => false
                };
                if (!exists)
                {
                    throw new InvalidDataException(
                        "Content set " + contentSet.Id + " references unknown pool member " +
                        entry.Kind + "|" + stableId + ".");
                }
            }
        }

        private static HeroDefinition Clone(HeroDefinition source)
        {
            return new HeroDefinition
            {
                HeroCardId = source.HeroCardId,
                ResearchKey = source.ResearchKey,
                RevisionId = source.RevisionId,
                EffectRevision = source.EffectRevision,
                SourceLevel = source.SourceLevel,
                ImplementationStatus = source.ImplementationStatus,
                HeroDbfId = source.HeroDbfId,
                Name = source.Name,
                ZhName = source.ZhName,
                Health = source.Health,
                Armor = source.Armor,
                InPool = source.InPool,
                ImagePath = source.ImagePath,
                ImageSource = source.ImageSource,
                ImageSha256 = source.ImageSha256,
                HeroPower = source.HeroPower == null ? null : new HeroPowerDefinition
                {
                    CardId = source.HeroPower.CardId,
                    DbfId = source.HeroPower.DbfId,
                    Name = source.HeroPower.Name,
                    ZhName = source.HeroPower.ZhName,
                    Cost = source.HeroPower.Cost,
                    Text = source.HeroPower.Text,
                    ZhText = source.HeroPower.ZhText,
                    ImagePath = source.HeroPower.ImagePath,
                    PrimaryCategory = source.HeroPower.PrimaryCategory,
                    Tags = new List<string>(source.HeroPower.Tags ?? new List<string>()),
                    ReplacementEligibility = source.HeroPower.ReplacementEligibility
                },
                Buddy = source.Buddy == null ? null : new HeroBuddyDefinition
                {
                    CardId = source.Buddy.CardId,
                    DbfId = source.Buddy.DbfId,
                    Name = source.Buddy.Name,
                    ZhName = source.Buddy.ZhName,
                    TavernTier = source.Buddy.TavernTier,
                    Attack = source.Buddy.Attack,
                    Health = source.Buddy.Health,
                    Text = source.Buddy.Text,
                    ZhText = source.Buddy.ZhText,
                    ImagePath = source.Buddy.ImagePath,
                    Tribes = new List<Tribe>(source.Buddy.Tribes ?? new List<Tribe>()),
                    Keywords = new List<Keyword>(source.Buddy.Keywords ?? new List<Keyword>()),
                    ExcludedFromBuddyDiscover = source.Buddy.ExcludedFromBuddyDiscover
                },
                MissingBuddyMapping = source.MissingBuddyMapping,
                MissingHeroPowerMapping = source.MissingHeroPowerMapping
            };
        }

        private static MinionDefinition Clone(MinionDefinition source)
        {
            return new MinionDefinition
            {
                Id = source.Id,
                CardId = source.CardId,
                ResearchKey = source.ResearchKey,
                RevisionId = source.RevisionId,
                EffectRevision = source.EffectRevision,
                SourceLevel = source.SourceLevel,
                ImplementationStatus = source.ImplementationStatus,
                DbfId = source.DbfId,
                Name = source.Name,
                TavernTier = source.TavernTier,
                BaseAttack = source.BaseAttack,
                BaseHealth = source.BaseHealth,
                Tribes = new List<Tribe>(source.Tribes ?? new List<Tribe>()),
                Keywords = new List<Keyword>(source.Keywords ?? new List<Keyword>()),
                OfficialKeywords = new List<Keyword>(source.OfficialKeywords ?? new List<Keyword>()),
                Text = source.Text,
                InPool = source.InPool,
                PoolCount = source.PoolCount,
                Golden = source.Golden == null ? null : new GoldenMinionDefinition
                {
                    CardId = source.Golden.CardId,
                    DbfId = source.Golden.DbfId,
                    BaseAttack = source.Golden.BaseAttack,
                    BaseHealth = source.Golden.BaseHealth,
                    Keywords = new List<Keyword>(source.Golden.Keywords ?? new List<Keyword>()),
                    OfficialKeywords = new List<Keyword>(source.Golden.OfficialKeywords ?? new List<Keyword>()),
                    Text = source.Golden.Text
                },
                ImagePath = source.ImagePath,
                ImageSource = source.ImageSource,
                EffectIds = new List<string>(source.EffectIds ?? new List<string>()),
                Tags = new List<string>(source.Tags ?? new List<string>()),
                RecruitActions = (source.RecruitActions ?? new List<RecruitActionDefinition>())
                    .Where(action => action != null)
                    .Select(action => action.Clone())
                    .ToList(),
                TokenId = source.TokenId
            };
        }

        private static TavernSpellDefinition Clone(TavernSpellDefinition source)
        {
            return new TavernSpellDefinition
            {
                Id = source.Id,
                RevisionId = source.RevisionId,
                EffectRevision = source.EffectRevision,
                SourceId = source.SourceId,
                CardNumber = source.CardNumber,
                Name = source.Name,
                EnglishName = source.EnglishName,
                Type = source.Type,
                SpecialType = source.SpecialType,
                Category = source.Category,
                Faction = source.Faction,
                AvailableModes = new List<string>(source.AvailableModes ?? new List<string>()),
                Cost = source.Cost,
                TavernTier = source.TavernTier,
                InPool = source.InPool,
                Keywords = new List<string>(source.Keywords ?? new List<string>()),
                Text = source.Text,
                EnglishText = source.EnglishText,
                Description = source.Description,
                ImageUrl = source.ImageUrl,
                ImagePath = source.ImagePath,
                EffectIds = new List<string>(source.EffectIds ?? new List<string>()),
                Tags = new List<string>(source.Tags ?? new List<string>()),
                CardTemplate = source.CardTemplate,
                TargetTemplate = source.TargetTemplate,
                EffectTemplate = source.EffectTemplate,
                ImplementationStatus = source.ImplementationStatus,
                Notes = source.Notes
            };
        }

        private static TrinketDefinition Clone(TrinketDefinition source)
        {
            return new TrinketDefinition
            {
                Id = source.Id,
                CardId = source.CardId,
                ResearchKey = source.ResearchKey,
                DbfId = source.DbfId,
                SourceName = source.SourceName,
                Name = source.Name,
                SlotKind = source.SlotKind,
                Cost = source.Cost,
                Text = source.Text,
                ImagePath = source.ImagePath,
                ImageUrl = source.ImageUrl,
                Mechanics = new List<string>(source.Mechanics ?? new List<string>()),
                ReferencedTags = new List<string>(source.ReferencedTags ?? new List<string>()),
                AssociatedRaces = new List<string>(source.AssociatedRaces ?? new List<string>()),
                RelatedDbfId = source.RelatedDbfId,
                Tags = new List<string>(source.Tags ?? new List<string>()),
                EffectIds = new List<string>(source.EffectIds ?? new List<string>()),
                ImplementationStatus = source.ImplementationStatus,
                OfferPoolStatus = source.OfferPoolStatus,
                PowerLevel = source.PowerLevel,
                EffectFamily = source.EffectFamily,
                TriggerTemplate = source.TriggerTemplate,
                EffectTemplate = source.EffectTemplate,
                Requires = new List<string>(source.Requires ?? new List<string>()),
                ProxyLevel = source.ProxyLevel,
                Notes = source.Notes
            };
        }

        private static TimewarpedTavernCardDefinition Clone(TimewarpedTavernCardDefinition source)
        {
            return new TimewarpedTavernCardDefinition
            {
                CardId = source.CardId,
                DbfId = source.DbfId,
                Name = source.Name,
                ZhName = source.ZhName,
                CardKind = source.CardKind,
                TimewarpKind = source.TimewarpKind,
                Cost = source.Cost,
                TechLevel = source.TechLevel,
                Attack = source.Attack,
                Health = source.Health,
                Tribes = new List<Tribe>(source.Tribes ?? new List<Tribe>()),
                Keywords = new List<Keyword>(source.Keywords ?? new List<Keyword>()),
                Text = source.Text,
                ZhText = source.ZhText,
                ImagePath = source.ImagePath,
                EffectIds = new List<string>(source.EffectIds ?? new List<string>()),
                Tags = new List<string>(source.Tags ?? new List<string>()),
                PoolStatus = source.PoolStatus,
                PurchaseBehavior = source.PurchaseBehavior,
                PrimaryMechanicTemplate = source.PrimaryMechanicTemplate,
                MechanicTemplates = new List<TimewarpedMechanicTemplate>(
                    source.MechanicTemplates ?? new List<TimewarpedMechanicTemplate>()),
                GoldenCardId = source.GoldenCardId,
                GoldenDbfId = source.GoldenDbfId
            };
        }

        private static HashSet<string> Membership(ContentSetDefinition contentSet, EntityKind kind)
        {
            return new HashSet<string>(
                contentSet.PoolMembership
                    .Where(entry => entry.Kind == kind)
                    .Select(entry => entry.StableEntityId),
                StringComparer.OrdinalIgnoreCase);
        }

        private static string StableMinionId(MinionDefinition definition)
        {
            return string.IsNullOrWhiteSpace(definition.CardId) ? definition.Id : definition.CardId;
        }

        private static string StableSpellId(TavernSpellDefinition definition)
        {
            return string.IsNullOrWhiteSpace(definition.CardNumber) ? definition.Id : definition.CardNumber;
        }

        private static string StableTrinketId(TrinketDefinition definition)
        {
            return string.IsNullOrWhiteSpace(definition.CardId) ? definition.Id : definition.CardId;
        }

        private static void ApplyHeroStats(HeroDefinition definition, string stats)
        {
            if (string.IsNullOrWhiteSpace(stats))
            {
                return;
            }

            foreach (var entry in stats.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = entry.IndexOf(':');
                if (separator <= 0 || !int.TryParse(
                        entry.Substring(separator + 1).Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var value) || value < 0)
                {
                    throw new InvalidDataException("Invalid hero revision stat: " + entry.Trim());
                }

                switch (entry.Substring(0, separator).Trim().ToLowerInvariant())
                {
                    case "health":
                        definition.Health = value;
                        break;
                    case "armor":
                        definition.Armor = value;
                        break;
                    case "powercost" when definition.HeroPower != null:
                        definition.HeroPower.Cost = value;
                        break;
                    default:
                        throw new InvalidDataException("Unsupported hero revision stat: " + entry.Trim());
                }
            }
        }

        private static void ApplyHeroRevision(
            HeroDefinition definition,
            EntityRevisionDefinition revision,
            bool useEnglish)
        {
            if (definition.HeroPower == null)
            {
                return;
            }

            var text = useEnglish
                ? (string.IsNullOrWhiteSpace(revision.EnglishText) ? revision.Text : revision.EnglishText)
                : revision.LocalizedText;
            if (!string.IsNullOrWhiteSpace(text))
            {
                definition.HeroPower.Text = text;
                if (!useEnglish)
                {
                    definition.HeroPower.ZhText = text;
                }
            }

            if (!string.IsNullOrWhiteSpace(revision.Art))
            {
                definition.HeroPower.ImagePath = revision.Art;
            }

            foreach (var tag in revision.Tags)
            {
                if (TryRevisionTagValue(tag, "source-level:", out var sourceLevel))
                {
                    definition.SourceLevel = sourceLevel;
                }
                else if (TryRevisionTagValue(tag, "implementation:", out var implementationStatus))
                {
                    definition.ImplementationStatus = implementationStatus;
                }
                else if (TryRevisionTagValue(tag, "hero-power-category:", out var category) &&
                         Enum.TryParse(category, true, out HeroPowerCategory parsedCategory))
                {
                    definition.HeroPower.PrimaryCategory = parsedCategory;
                }
                else if (TryRevisionTagValue(tag, "hero-power-replacement:", out var eligibility) &&
                         Enum.TryParse(eligibility, true, out HeroPowerReplacementEligibility parsedEligibility))
                {
                    definition.HeroPower.ReplacementEligibility = parsedEligibility;
                }
                else if (TryRevisionTagValue(tag, "hero-power-tag:", out var heroPowerTag))
                {
                    AddTag(definition.HeroPower.Tags, heroPowerTag);
                }
            }
        }

        private static bool TryRevisionTagValue(string tag, string prefix, out string value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(tag) ||
                !tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                tag.Length <= prefix.Length)
            {
                return false;
            }

            value = tag.Substring(prefix.Length).Trim();
            return value.Length > 0;
        }

        private static void ApplyMinionStats(MinionDefinition definition, string stats)
        {
            if (string.IsNullOrWhiteSpace(stats))
            {
                return;
            }

            foreach (var entry in stats.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = entry.IndexOf(':');
                if (separator <= 0 || !int.TryParse(
                        entry.Substring(separator + 1).Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var value) || value < 0)
                {
                    throw new InvalidDataException("Invalid minion revision stat: " + entry.Trim());
                }

                switch (entry.Substring(0, separator).Trim().ToLowerInvariant())
                {
                    case "tier":
                        definition.TavernTier = value;
                        break;
                    case "attack":
                        definition.BaseAttack = value;
                        break;
                    case "health":
                        definition.BaseHealth = value;
                        break;
                    case "goldenattack" when definition.Golden != null:
                        definition.Golden.BaseAttack = value;
                        break;
                    case "goldenhealth" when definition.Golden != null:
                        definition.Golden.BaseHealth = value;
                        break;
                }
            }
        }

        private static void ApplyMinionRevision(
            MinionDefinition definition,
            EntityRevisionDefinition revision,
            bool useEnglish)
        {
            var text = useEnglish
                ? (string.IsNullOrWhiteSpace(revision.EnglishText) ? revision.Text : revision.EnglishText)
                : (string.IsNullOrWhiteSpace(revision.LocalizedText) ? revision.Text : revision.LocalizedText);
            if (!string.IsNullOrWhiteSpace(text))
            {
                definition.Text = text;
                if (definition.Golden != null)
                {
                    definition.Golden.Text = text;
                }
            }

            foreach (var effectId in revision.EffectIds)
            {
                if (!definition.EffectIds.Contains(effectId))
                {
                    definition.EffectIds.Add(effectId);
                }
            }

            if (!string.IsNullOrWhiteSpace(revision.Art))
            {
                definition.ImagePath = revision.Art;
            }

            if (!definition.EffectIds.Contains(MinionEffectIds.AlwaysGoldenNoTripleReward))
            {
                return;
            }

            definition.Keywords.RemoveAll(keyword => keyword == Keyword.Battlecry);
            definition.OfficialKeywords.RemoveAll(keyword => keyword == Keyword.Battlecry);
            definition.Tags.RemoveAll(tag =>
                string.Equals(tag, "battlecry", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tag, "self_golden", StringComparison.OrdinalIgnoreCase));
            AddTag(definition.Tags, "always_golden");
            AddTag(definition.Tags, "no_triple_reward");
            if (definition.Golden != null)
            {
                definition.Golden.Keywords.RemoveAll(keyword => keyword == Keyword.Battlecry);
                definition.Golden.OfficialKeywords.RemoveAll(keyword => keyword == Keyword.Battlecry);
            }
        }

        private static void AddTag(List<string> tags, string tag)
        {
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        private static void ApplySpellStats(TavernSpellDefinition definition, string stats)
        {
            if (string.IsNullOrWhiteSpace(stats))
            {
                return;
            }

            foreach (var entry in stats.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = entry.IndexOf(':');
                if (separator <= 0 || !int.TryParse(
                        entry.Substring(separator + 1).Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var value) || value < 0)
                {
                    throw new InvalidDataException("Invalid Tavern spell revision stat: " + entry.Trim());
                }

                switch (entry.Substring(0, separator).Trim().ToLowerInvariant())
                {
                    case "tier":
                        definition.TavernTier = value;
                        break;
                    case "cost":
                        definition.Cost = value;
                        break;
                }
            }
        }

        private static void ApplyTrinketStats(TrinketDefinition definition, string stats)
        {
            if (string.IsNullOrWhiteSpace(stats))
            {
                return;
            }

            foreach (var entry in stats.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = entry.IndexOf(':');
                if (separator <= 0 || !int.TryParse(
                        entry.Substring(separator + 1).Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var value) || value < 0)
                {
                    throw new InvalidDataException("Invalid Trinket revision stat: " + entry.Trim());
                }

                if (string.Equals(entry.Substring(0, separator).Trim(), "cost", StringComparison.OrdinalIgnoreCase))
                {
                    definition.Cost = value;
                }
            }
        }

        private static void ApplyTrinketText(
            TrinketDefinition definition,
            EntityRevisionDefinition revision,
            bool useEnglish)
        {
            var text = useEnglish
                ? (string.IsNullOrWhiteSpace(revision.EnglishText) ? revision.Text : revision.EnglishText)
                : (string.IsNullOrWhiteSpace(revision.LocalizedText) ? revision.Text : revision.LocalizedText);
            if (!string.IsNullOrWhiteSpace(text))
            {
                definition.Text = text;
            }
        }

        private static void ApplySpellText(
            TavernSpellDefinition definition,
            EntityRevisionDefinition revision,
            bool useEnglish)
        {
            if (useEnglish)
            {
                var english = string.IsNullOrWhiteSpace(revision.EnglishText)
                    ? revision.Text
                    : revision.EnglishText;
                if (!string.IsNullOrWhiteSpace(english))
                {
                    definition.Text = english;
                    definition.EnglishText = english;
                }
                return;
            }

            if (!string.IsNullOrWhiteSpace(revision.LocalizedText))
            {
                definition.Text = revision.LocalizedText;
            }
            if (!string.IsNullOrWhiteSpace(revision.EnglishText))
            {
                definition.EnglishText = revision.EnglishText;
            }
        }

        private static string EntityKey(EntityKind kind, string stableEntityId)
        {
            return kind + ":" + stableEntityId;
        }

        private static string ComputeFingerprint(
            GameVersionDefinition version,
            RulesetDefinition ruleset,
            ContentSetDefinition contentSet,
            IEnumerable<EntityRevisionDefinition> selectedRevisions,
            string contentVersion)
        {
            var parts = new List<string>
            {
                version.Id,
                version.RulesetId,
                version.ContentSetId,
                ruleset.SchemaVersion.ToString(),
                ruleset.VenomousEffectRevision,
                contentSet.Id,
                contentVersion ?? string.Empty
            };
            parts.AddRange(ruleset.AllowedSetupMechanicIds.Select(id => "setup-allowed|" + id));
            parts.AddRange(ruleset.DefaultSetupMechanicIds.Select(id => "setup-default|" + id));
            parts.AddRange(selectedRevisions
                .OrderBy(revision => revision.RevisionId, StringComparer.Ordinal)
                .Select(revision => revision.RevisionId + "|" + revision.EffectRevision + "|" + revision.Stats + "|" + revision.LocalizedText + "|" + revision.EnglishText));
            parts.AddRange(contentSet.PoolMembership.Select(entry => entry.Kind + "|" + entry.StableEntityId));
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(string.Join("\n", parts));
                return BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static Dictionary<string, T> Index<T>(IEnumerable<T> items, Func<T, string> key, string label)
        {
            var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items ?? Enumerable.Empty<T>())
            {
                if (item == null)
                {
                    throw new ArgumentException(label + " collection cannot contain null entries.");
                }
                var itemKey = key(item);
                if (!result.TryAdd(itemKey, item))
                {
                    throw new ArgumentException("Duplicate " + label + " id: " + itemKey + ".");
                }
            }
            return result;
        }

        private static T Get<T>(IReadOnlyDictionary<string, T> items, string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id) || !items.TryGetValue(id, out var item))
            {
                throw new InvalidDataException("Referenced " + label + " does not exist: " + id + ".");
            }
            return item;
        }
    }
}
