using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class HeroCatalog
    {
        private static readonly HashSet<string> AlwaysFilteredDiscoverHeroPowerIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "BG34_HERO_002p",
                "TB_BaconShop_HP_080",
                "TB_BaconShop_HP_081",
                "BG23_HERO_303p2"
            };

        private readonly Dictionary<string, HeroDefinition> heroesByCardId;
        private readonly Dictionary<string, HeroPowerDefinition> heroPowersByCardId;
        private readonly Dictionary<string, HeroDefinition> heroesByHeroPowerCardId;
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
            heroesByHeroPowerCardId = AllHeroes
                .Where(hero => hero.HeroPower != null && !string.IsNullOrEmpty(hero.HeroPower.CardId))
                .GroupBy(hero => hero.HeroPower.CardId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

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

        public bool TryGetHeroByHeroPowerCardId(string heroPowerCardId, out HeroDefinition hero)
        {
            return heroesByHeroPowerCardId.TryGetValue(heroPowerCardId ?? string.Empty, out hero);
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

        public List<HeroPowerDefinition> GetOfferableDiscoverableHeroPowers(string currentHeroPowerCardId)
        {
            return GetDiscoverableHeroPowers(currentHeroPowerCardId)
                .Where(IsOfferableDiscoverHeroPower)
                .ToList();
        }

        public MinionInstance CreateDiscoverableHeroPowerOption(HeroPowerDefinition definition, BoardSide owner, string suffix)
        {
            var option = MinionFactory.Create(definition, owner, suffix);
            AddHeroPowerImplementationTags(option, definition?.CardId);
            return option;
        }

        public static bool IsOfferableDiscoverHeroPower(HeroPowerDefinition power)
        {
            if (power == null || string.IsNullOrEmpty(power.CardId))
            {
                return false;
            }

            if (AlwaysFilteredDiscoverHeroPowerIds.Contains(power.CardId))
            {
                return false;
            }

            var status = HeroEffectImplementationRegistry.GetStatusByHeroPowerCardId(power.CardId);
            return status == HeroEffectImplementationStatus.Implemented ||
                   status == HeroEffectImplementationStatus.FrameworkFirst;
        }

        public static void AddHeroPowerImplementationTags(MinionInstance option, string heroPowerCardId)
        {
            if (option == null)
            {
                return;
            }

            var status = HeroEffectImplementationRegistry.GetStatusByHeroPowerCardId(heroPowerCardId ?? option.CardId);
            AddTag(option.Tags, "implementation_status:" + status);
            if (status == HeroEffectImplementationStatus.FrameworkFirst)
            {
                AddTag(option.Tags, "hero_power_proxy");
                AddTag(option.Tags, "framework_first");
                AddTag(option.Tags, "incomplete_hero_power");
            }
        }

        private static void AddTag(List<string> tags, string tag)
        {
            if (tags == null || string.IsNullOrEmpty(tag) || tags.Contains(tag))
            {
                return;
            }

            tags.Add(tag);
        }

        public List<HeroDefinition> GetInitialSelectableHeroes()
        {
            return AllHeroes
                .Where(hero => !string.IsNullOrEmpty(hero.HeroCardId))
                .Where(hero => hero.Health > 0)
                .Where(hero => hero.InPool)
                .ToList();
        }
    }
}
