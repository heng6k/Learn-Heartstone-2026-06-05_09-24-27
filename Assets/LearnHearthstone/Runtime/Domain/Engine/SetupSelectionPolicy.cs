using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class SetupMechanicIds
    {
        public const string DarkGifts = "dark-gifts";
        public const string Trinkets = "trinkets";
        public const string Quests = "quests";
        public const string QuestRewards = "quest-rewards";
        public const string Anomalies = "anomalies";
        public const string TimewarpedTavern = "timewarped-tavern";

        public static readonly IReadOnlyList<string> Season14 = Array.AsReadOnly(new[]
        {
            DarkGifts,
            Trinkets
        });

        public static readonly IReadOnlyList<string> LegacyComposite = Array.AsReadOnly(new[]
        {
            Quests,
            QuestRewards,
            Trinkets,
            Anomalies,
            TimewarpedTavern
        });
    }

    public sealed class SetupSelectionPolicy
    {
        public const int DefaultRandomTribeCount = 5;
        public const int MinCustomTribeCount = 5;
        public const int SelectionCap = 10;

        private readonly HashSet<string> allowedMechanicIds;

        private SetupSelectionPolicy(
            int playableTribeCount,
            IEnumerable<string> allowedSetupMechanicIds,
            IEnumerable<string> defaultSetupMechanicIds)
        {
            if (playableTribeCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playableTribeCount));
            }

            PlayableTribeCount = playableTribeCount;
            MaxCustomTribeCount = Math.Min(SelectionCap, playableTribeCount);
            AllowedMechanicIds = ReadOnly(allowedSetupMechanicIds);
            DefaultMechanicIds = ReadOnly(defaultSetupMechanicIds);
            allowedMechanicIds = new HashSet<string>(AllowedMechanicIds, StringComparer.OrdinalIgnoreCase);

            var invalidDefaults = DefaultMechanicIds.Where(id => !allowedMechanicIds.Contains(id)).ToArray();
            if (invalidDefaults.Length > 0)
            {
                throw new ArgumentException(
                    "Default setup mechanics must be allowed: " + string.Join(", ", invalidDefaults),
                    nameof(defaultSetupMechanicIds));
            }
        }

        public int PlayableTribeCount { get; }
        public int MaxCustomTribeCount { get; }
        public IReadOnlyList<string> AllowedMechanicIds { get; }
        public IReadOnlyList<string> DefaultMechanicIds { get; }
        public bool HasCompletePlayableTribeCatalog => PlayableTribeCount >= MinCustomTribeCount;
        public bool CanSelectAllPlayableTribes =>
            HasCompletePlayableTribeCatalog && PlayableTribeCount <= SelectionCap;

        public static SetupSelectionPolicy FromRuleset(RulesetDefinition ruleset, int playableTribeCount)
        {
            if (ruleset == null)
            {
                throw new ArgumentNullException(nameof(ruleset));
            }

            return new SetupSelectionPolicy(
                playableTribeCount,
                ruleset.AllowedSetupMechanicIds,
                ruleset.DefaultSetupMechanicIds);
        }

        public static SetupSelectionPolicy CreateLegacyCompatible(int playableTribeCount)
        {
            return new SetupSelectionPolicy(
                playableTribeCount,
                SetupMechanicIds.LegacyComposite,
                Array.Empty<string>());
        }

        public bool IsCustomTribeCountValid(int selectedTribeCount)
        {
            return HasCompletePlayableTribeCatalog &&
                selectedTribeCount >= MinCustomTribeCount &&
                selectedTribeCount <= MaxCustomTribeCount;
        }

        public bool CanSelectAnotherTribe(int selectedTribeCount)
        {
            return HasCompletePlayableTribeCatalog && selectedTribeCount < MaxCustomTribeCount;
        }

        public bool AllowsMechanic(string mechanicId)
        {
            return !string.IsNullOrWhiteSpace(mechanicId) && allowedMechanicIds.Contains(mechanicId);
        }

        private static ReadOnlyCollection<string> ReadOnly(IEnumerable<string> values)
        {
            return Array.AsReadOnly((values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
        }
    }
}
