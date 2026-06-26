using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class HeroCatalog
    {
        private readonly Dictionary<string, HeroDefinition> heroesByCardId;
        private readonly Dictionary<string, HeroPowerDefinition> heroPowersByCardId;
        private readonly Dictionary<string, HeroBuddyDefinition> buddiesByCardId;

        public HeroCatalog(IEnumerable<HeroDefinition> heroes)
        {
            AllHeroes = (heroes ?? Enumerable.Empty<HeroDefinition>()).ToList();
            heroesByCardId = AllHeroes
                .Where(hero => !string.IsNullOrEmpty(hero.HeroCardId))
                .GroupBy(hero => hero.HeroCardId)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            AllHeroPowers = AllHeroes
                .Select(hero => hero.HeroPower)
                .Where(power => power != null && !string.IsNullOrEmpty(power.CardId))
                .GroupBy(power => power.CardId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            heroPowersByCardId = AllHeroPowers.ToDictionary(power => power.CardId, power => power, StringComparer.OrdinalIgnoreCase);

            AllBuddies = AllHeroes
                .Select(hero => hero.Buddy)
                .Where(buddy => buddy != null && !string.IsNullOrEmpty(buddy.CardId))
                .GroupBy(buddy => buddy.CardId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            buddiesByCardId = AllBuddies.ToDictionary(buddy => buddy.CardId, buddy => buddy, StringComparer.OrdinalIgnoreCase);
        }

        public List<HeroDefinition> AllHeroes { get; }

        public List<HeroPowerDefinition> AllHeroPowers { get; }

        public List<HeroBuddyDefinition> AllBuddies { get; }

        public HeroDefinition GetHeroByCardId(string heroCardId)
        {
            if (!heroesByCardId.TryGetValue(heroCardId ?? string.Empty, out var hero))
            {
                throw new InvalidOperationException("Hero card id does not exist: " + heroCardId);
            }

            return hero;
        }

        public HeroPowerDefinition GetHeroPowerByCardId(string heroPowerCardId)
        {
            if (!heroPowersByCardId.TryGetValue(heroPowerCardId ?? string.Empty, out var heroPower))
            {
                throw new InvalidOperationException("Hero power card id does not exist: " + heroPowerCardId);
            }

            return heroPower;
        }

        public HeroBuddyDefinition GetBuddyByCardId(string buddyCardId)
        {
            if (!buddiesByCardId.TryGetValue(buddyCardId ?? string.Empty, out var buddy))
            {
                throw new InvalidOperationException("Hero buddy card id does not exist: " + buddyCardId);
            }

            return buddy;
        }

        public List<HeroPowerDefinition> GetDiscoverableHeroPowers(string currentHeroPowerCardId)
        {
            return AllHeroPowers
                .Where(power => power.ReplacementEligibility == HeroPowerReplacementEligibility.DiscoverableAfterStart)
                .Where(power => !string.Equals(power.CardId, currentHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<HeroDefinition> GetInitialSelectableHeroes()
        {
            return AllHeroes
                .Where(hero => !string.IsNullOrEmpty(hero.HeroCardId))
                .Where(hero => hero.Health > 0)
                .ToList();
        }
    }
}
