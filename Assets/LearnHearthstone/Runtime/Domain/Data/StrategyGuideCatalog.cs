using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class StrategyGuideCatalog
    {
        private readonly Dictionary<string, StrategyGuideDefinition> guidesById;

        public StrategyGuideCatalog(StrategyGuideCatalogDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (definition.SchemaVersion != 2)
            {
                throw new ArgumentException("Unsupported strategy guide schema version: " + definition.SchemaVersion + ".");
            }

            Guides = (definition.Guides ?? new List<StrategyGuideDefinition>())
                .Where(item => item != null)
                .ToList();
            Opponents = (definition.Opponents ?? new List<StrategyGuideOpponentDefinition>())
                .Where(item => item != null)
                .ToList();
            EnsureUnique(Guides.Select(item => item.GuideId), "guide id");
            EnsureUnique(Guides.Select(item => item.RevisionId), "guide revision");
            EnsureUnique(Opponents.Select(item => item.OpponentId), "opponent id");
            EnsureUnique(Opponents.Select(item => item.RevisionId), "opponent revision");
            foreach (var guide in Guides)
            {
                EnsureUnique(
                    (guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>())
                        .Where(item => item != null)
                        .Select(item => item.ProfileId),
                    "entry profile id for " + guide.GuideId);
            }
            guidesById = Guides.ToDictionary(item => item.GuideId, item => item, StringComparer.OrdinalIgnoreCase);
        }

        public StrategyGuideCatalogDefinition Definition { get; }
        public IReadOnlyList<StrategyGuideDefinition> Guides { get; }
        public IReadOnlyList<StrategyGuideOpponentDefinition> Opponents { get; }

        public StrategyGuideDefinition GetGuide(string guideId)
        {
            if (string.IsNullOrWhiteSpace(guideId) || !guidesById.TryGetValue(guideId, out var guide))
            {
                throw new InvalidOperationException("Strategy guide does not exist: " + guideId + ".");
            }

            return guide;
        }

        public StrategyGuideEntryProfileDefinition GetProfile(string guideId, string profileId)
        {
            var guide = GetGuide(guideId);
            var profiles = guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>();
            var profile = profiles.FirstOrDefault(item =>
                item != null && string.Equals(item.ProfileId, profileId, StringComparison.OrdinalIgnoreCase));
            if (profile == null)
            {
                throw new InvalidOperationException(
                    "Strategy guide entry profile does not exist: " + guideId + "/" + profileId + ".");
            }
            return profile;
        }

        public StrategyGuideEntryProfileDefinition GetDefaultProfile(string guideId)
        {
            var guide = GetGuide(guideId);
            var profiles = (guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>())
                .Where(item => item != null && string.Equals(item.Difficulty, StrategyGuideDifficulties.Showcase, StringComparison.Ordinal))
                .ToList();
            if (profiles.Count != 1)
            {
                throw new InvalidOperationException(
                    "Strategy guide must have exactly one default Showcase profile: " + guideId + ".");
            }
            return profiles[0];
        }

        private static void EnsureUnique(IEnumerable<string> values, string label)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Strategy guide " + label + " is required.");
                }
                if (!seen.Add(value))
                {
                    throw new ArgumentException("Duplicate strategy guide " + label + ": " + value + ".");
                }
            }
        }
    }
}
