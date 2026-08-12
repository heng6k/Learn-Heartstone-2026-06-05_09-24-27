using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class CardPoolVersionFactory
    {
        public const int MaxCustomVersions = 10;
        public const string DefaultVersionId = "default";
        public const string DefaultVersionName = "默认方案";

        public static CardPoolVersionSelection CreateDefaultSelection(MinionCatalog minions, SpellCatalog spells)
        {
            return new CardPoolVersionSelection
            {
                VersionId = DefaultVersionId,
                VersionName = DefaultVersionName,
                IsDefault = true,
                EnabledMinionCardIds = new HashSet<string>(
                    (minions?.All ?? Enumerable.Empty<MinionDefinition>())
                    .Where(minion => minion.InPool && !string.IsNullOrEmpty(minion.CardId) && !IsDuoCardId(minion.CardId))
                    .Select(minion => minion.CardId),
                    StringComparer.OrdinalIgnoreCase),
                EnabledTavernSpellCardNumbers = new HashSet<string>(
                    (spells?.All ?? Enumerable.Empty<TavernSpellDefinition>())
                    .Where(spell => spell.InPool && spell.Category == "TavernSpell" && !string.IsNullOrEmpty(spell.CardNumber))
                    .Select(spell => spell.CardNumber),
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        public static CardPoolVersionSelection CreateSelection(CardPoolVersionProfile profile)
        {
            if (profile == null)
            {
                return null;
            }

            return new CardPoolVersionSelection
            {
                VersionId = profile.Id,
                VersionName = string.IsNullOrEmpty(profile.Name) ? "自定义方案" : profile.Name,
                IsDefault = false,
                EnabledMinionCardIds = ToMinionSet(profile.EnabledMinionCardIds),
                EnabledTavernSpellCardNumbers = ToSet(profile.EnabledTavernSpellCardNumbers),
                EnabledQuestCardIds = ToSet(profile.EnabledQuestCardIds),
                EnabledQuestRewardCardIds = ToSet(profile.EnabledQuestRewardCardIds),
                EnabledLesserTrinketCardIds = ToSet(profile.EnabledLesserTrinketCardIds),
                EnabledGreaterTrinketCardIds = ToSet(profile.EnabledGreaterTrinketCardIds),
                EnabledAnomalyCardIds = ToSet(profile.EnabledAnomalyCardIds)
            };
        }

        public static CardPoolVersionProfile CreateProfileFromSelection(CardPoolVersionSelection selection, string id, string name)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return new CardPoolVersionProfile
            {
                Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id,
                Name = string.IsNullOrEmpty(name) ? "自定义方案" : name,
                CreatedAtUnixSeconds = now,
                UpdatedAtUnixSeconds = now,
                EnabledMinionCardIds = OrderedMinions(selection?.EnabledMinionCardIds),
                EnabledTavernSpellCardNumbers = Ordered(selection?.EnabledTavernSpellCardNumbers),
                EnabledQuestCardIds = Ordered(selection?.EnabledQuestCardIds),
                EnabledQuestRewardCardIds = Ordered(selection?.EnabledQuestRewardCardIds),
                EnabledLesserTrinketCardIds = Ordered(selection?.EnabledLesserTrinketCardIds),
                EnabledGreaterTrinketCardIds = Ordered(selection?.EnabledGreaterTrinketCardIds),
                EnabledAnomalyCardIds = Ordered(selection?.EnabledAnomalyCardIds)
            };
        }

        public static CardPoolVersionStore NormalizeStore(CardPoolVersionStore store)
        {
            var normalized = store ?? new CardPoolVersionStore();
            normalized.SchemaVersion = CardPoolVersionStore.CurrentSchemaVersion;
            normalized.Versions = normalized.Versions ?? new List<CardPoolVersionProfile>();
            normalized.Versions = normalized.Versions
                .Where(version => version != null && !string.IsNullOrEmpty(version.Id))
                .GroupBy(version => version.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => NormalizeProfile(group.First()))
                .Take(MaxCustomVersions)
                .ToList();

            if (!string.IsNullOrEmpty(normalized.SelectedVersionId) &&
                normalized.Versions.All(version => !string.Equals(version.Id, normalized.SelectedVersionId, StringComparison.OrdinalIgnoreCase)))
            {
                normalized.SelectedVersionId = null;
            }

            return normalized;
        }

        public static CardPoolVersionProfile NormalizeProfile(CardPoolVersionProfile profile)
        {
            var legacyProfile = string.IsNullOrWhiteSpace(profile.BaseGameVersionId);
            if (legacyProfile)
            {
                profile.BaseGameVersionId = GameVersionIds.LegacyCompositeSandbox;
                if (profile.ValidationState == CardPoolPresetValidationState.Unknown)
                {
                    profile.ValidationState = CardPoolPresetValidationState.Valid;
                }
            }

            profile.CreatedAgainstContentFingerprint = profile.CreatedAgainstContentFingerprint ?? string.Empty;
            profile.IncompatibleEntityIds = Ordered(profile.IncompatibleEntityIds);
            profile.EnabledMinionCardIds = OrderedMinions(profile.EnabledMinionCardIds);
            profile.EnabledTavernSpellCardNumbers = Ordered(profile.EnabledTavernSpellCardNumbers);
            profile.EnabledQuestCardIds = Ordered(profile.EnabledQuestCardIds);
            profile.EnabledQuestRewardCardIds = Ordered(profile.EnabledQuestRewardCardIds);
            profile.EnabledLesserTrinketCardIds = Ordered(profile.EnabledLesserTrinketCardIds);
            profile.EnabledGreaterTrinketCardIds = Ordered(profile.EnabledGreaterTrinketCardIds);
            profile.EnabledAnomalyCardIds = Ordered(profile.EnabledAnomalyCardIds);
            if (string.IsNullOrEmpty(profile.Name))
            {
                profile.Name = "自定义方案";
            }

            return profile;
        }

        private static HashSet<string> ToMinionSet(IEnumerable<string> values)
        {
            return new HashSet<string>(
                (values ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrEmpty(value) && !IsDuoCardId(value)),
                StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<string> ToSet(IEnumerable<string> values)
        {
            return new HashSet<string>(
                (values ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrEmpty(value)),
                StringComparer.OrdinalIgnoreCase);
        }

        private static List<string> Ordered(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> OrderedMinions(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrEmpty(value) && !IsDuoCardId(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsDuoCardId(string value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith("BGDUO", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class CardPoolPresetAdapter
    {
        public static CardPoolPresetProfile FromLegacy(CardPoolVersionProfile profile)
        {
            return new CardPoolPresetProfile(CardPoolVersionFactory.NormalizeProfile(
                profile ?? throw new ArgumentNullException(nameof(profile))));
        }

        public static CardPoolVersionProfile ToLegacy(CardPoolPresetProfile preset)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            return CardPoolVersionFactory.NormalizeProfile(preset.LegacyProfile);
        }
    }
}
