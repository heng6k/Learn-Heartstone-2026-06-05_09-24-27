using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class TribeAvailabilityRules
    {
        public static readonly Tribe[] PlayableTribes =
        {
            Tribe.Beast,
            Tribe.Murloc,
            Tribe.Mech,
            Tribe.Demon,
            Tribe.Dragon,
            Tribe.Pirate,
            Tribe.Elemental,
            Tribe.Quilboar,
            Tribe.Undead,
            Tribe.Naga
        };

        private static readonly Dictionary<string, Tribe[]> TavernSpellTribeMap = new Dictionary<string, Tribe[]>
        {
            { "126676", new[] { Tribe.Quilboar } },
            { "110642", new[] { Tribe.Quilboar } },
            { "122182", new[] { Tribe.Pirate } },
            { "122183", new[] { Tribe.Pirate } },
            { "122184", new[] { Tribe.Pirate } },
            { "122185", new[] { Tribe.Pirate } },
            { "122186", new[] { Tribe.Pirate } },
            { "127506", new[] { Tribe.Pirate } },
            { "110400", new[] { Tribe.Murloc } },
            { "131218", new[] { Tribe.Murloc } },
            { "122862", new[] { Tribe.Elemental } },
            { "130311", new[] { Tribe.Elemental } },
            { "130310", new[] { Tribe.Elemental } },
            { "126909", new[] { Tribe.Elemental } },
            { "117670", new[] { Tribe.Elemental } },
            { "120900", new[] { Tribe.Naga } },
            { "110406", new[] { Tribe.Naga } },
            { "130713", new[] { Tribe.Naga } },
            { "126957", new[] { Tribe.Undead } },
            { "110412", new[] { Tribe.Undead } },
            { "122489", new[] { Tribe.Undead } },
            { "110407", new[] { Tribe.Demon } },
            { "127503", new[] { Tribe.Dragon } },
            { "123553", new[] { Tribe.Beast } },
            { "122899", new[] { Tribe.Mech } }
        };

        public static List<Tribe> AllPlayableTribes()
        {
            return new List<Tribe>(PlayableTribes);
        }

        public static List<Tribe> Normalize(IEnumerable<Tribe> tribes)
        {
            if (tribes == null)
            {
                return AllPlayableTribes();
            }

            var result = new List<Tribe>();
            foreach (var tribe in tribes)
            {
                if (!PlayableTribes.Contains(tribe) || result.Contains(tribe))
                {
                    continue;
                }

                result.Add(tribe);
            }

            return result.Count == 0 ? AllPlayableTribes() : result;
        }

        public static bool IsTribeActive(IReadOnlyCollection<Tribe> activeTribes, Tribe tribe)
        {
            if (tribe == Tribe.None || tribe == Tribe.All)
            {
                return true;
            }

            return Normalize(activeTribes).Contains(tribe);
        }

        public static bool IsMinionAvailable(MinionDefinition minion, IReadOnlyCollection<Tribe> activeTribes)
        {
            if (minion == null)
            {
                return false;
            }

            if (minion.Tribes == null || minion.Tribes.Count == 0 || minion.Tribes.All(tribe => tribe == Tribe.None))
            {
                return true;
            }

            if (minion.Tribes.Contains(Tribe.All))
            {
                return true;
            }

            var active = Normalize(activeTribes);
            return minion.Tribes.Any(active.Contains);
        }

        public static bool IsTavernSpellAvailable(TavernSpellDefinition spell, IReadOnlyCollection<Tribe> activeTribes)
        {
            var tribes = SpellTribes(spell);
            return tribes.Count == 0 || tribes.Any(tribe => IsTribeActive(activeTribes, tribe));
        }

        public static bool IsTrinketAvailable(TrinketDefinition trinket, IReadOnlyCollection<Tribe> activeTribes)
        {
            if (trinket == null)
            {
                return false;
            }

            if (trinket.AssociatedRaces == null || trinket.AssociatedRaces.Count == 0)
            {
                return true;
            }

            var tribes = TrinketTribes(trinket);
            return tribes.Count == 0 || tribes.Any(tribe => IsTribeActive(activeTribes, tribe));
        }

        public static IReadOnlyList<Tribe> TrinketTribes(TrinketDefinition trinket)
        {
            if (trinket?.AssociatedRaces == null || trinket.AssociatedRaces.Count == 0)
            {
                return Array.Empty<Tribe>();
            }

            var result = new List<Tribe>();
            foreach (var race in trinket.AssociatedRaces)
            {
                var tribe = MapFaction(race);
                if (!tribe.HasValue || result.Contains(tribe.Value))
                {
                    continue;
                }

                result.Add(tribe.Value);
            }

            return result;
        }

        public static IReadOnlyList<Tribe> SpellTribes(TavernSpellDefinition spell)
        {
            if (spell == null)
            {
                return Array.Empty<Tribe>();
            }

            if (!string.IsNullOrEmpty(spell.CardNumber) && TavernSpellTribeMap.TryGetValue(spell.CardNumber, out var mappedByCardNumber))
            {
                return mappedByCardNumber;
            }

            if (!string.IsNullOrEmpty(spell.Id) && TavernSpellTribeMap.TryGetValue(spell.Id, out var mappedById))
            {
                return mappedById;
            }

            var factionTribe = MapFaction(spell.Faction);
            return factionTribe.HasValue ? new[] { factionTribe.Value } : Array.Empty<Tribe>();
        }

        private static Tribe? MapFaction(string faction)
        {
            switch (faction)
            {
                case "野兽":
                case "Beast":
                    return Tribe.Beast;
                case "鱼人":
                case "Murloc":
                    return Tribe.Murloc;
                case "机械":
                case "Mech":
                    return Tribe.Mech;
                case "恶魔":
                case "Demon":
                    return Tribe.Demon;
                case "龙":
                case "Dragon":
                    return Tribe.Dragon;
                case "海盗":
                case "Pirate":
                    return Tribe.Pirate;
                case "元素":
                case "Elemental":
                    return Tribe.Elemental;
                case "野猪人":
                case "Quilboar":
                    return Tribe.Quilboar;
                case "亡灵":
                case "Undead":
                    return Tribe.Undead;
                case "纳迦":
                case "Naga":
                    return Tribe.Naga;
                default:
                    return null;
            }
        }
    }
}
