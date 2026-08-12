using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Content
{
    public sealed class GameVersionCatalog
    {
        private readonly ReadOnlyCollection<GameVersionDefinition> versions;
        private readonly ReadOnlyCollection<GameVersionSummaryViewModel> summaries;
        private readonly Dictionary<string, GameVersionDefinition> byId;

        public GameVersionCatalog(IEnumerable<GameVersionDefinition> versions)
        {
            var items = (versions ?? throw new ArgumentNullException(nameof(versions))).ToArray();
            if (items.Any(item => item == null))
            {
                throw new ArgumentException("Game versions cannot contain null entries.", nameof(versions));
            }

            var duplicate = items
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                throw new ArgumentException("Duplicate game version id: " + duplicate.Key, nameof(versions));
            }

            this.versions = Array.AsReadOnly(items);
            summaries = Array.AsReadOnly(items.Select(item => new GameVersionSummaryViewModel(item)).ToArray());
            byId = items.ToDictionary(item => item.Id, item => item, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<GameVersionDefinition> Versions => versions;
        public IReadOnlyList<GameVersionSummaryViewModel> Summaries => summaries;

        public GameVersionDefinition Default => versions
            .Where(version => version.IsDefaultCandidate)
            .OrderByDescending(version => version.ReleaseDateUtc)
            .ThenBy(version => version.Id, StringComparer.Ordinal)
            .FirstOrDefault() ?? throw new InvalidOperationException("No verified game version is available.");

        public GameVersionDefinition Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !byId.TryGetValue(id, out var version))
            {
                throw new InvalidOperationException("Game version does not exist: " + id);
            }

            return version;
        }

        public static GameVersionCatalog CreateBuiltIn()
        {
            return new GameVersionCatalog(new[]
            {
                new GameVersionDefinition(
                    GameVersionIds.LegacyCompositeSandbox,
                    "综合沙盒（旧行为）",
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    GameVersionOfficialStatus.Unofficial,
                    GameVersionImplementationStatus.Verified,
                    RulesetIds.LegacyCompositeSandbox,
                    ContentSetIds.LegacyCompositeSandbox,
                    "保留当前综合训练器行为。"),
                new GameVersionDefinition(
                    GameVersionIds.Season14Preview,
                    "36.2",
                    new DateTime(2026, 8, 4, 17, 0, 0, DateTimeKind.Utc),
                    GameVersionOfficialStatus.Released,
                    GameVersionImplementationStatus.Partial,
                    RulesetIds.Season14Preview,
                    ContentSetIds.Season14Preview,
                    "第 14 赛季已发布；训练器仍为部分支持，不会自动成为默认版本。")
            });
        }
    }
}
