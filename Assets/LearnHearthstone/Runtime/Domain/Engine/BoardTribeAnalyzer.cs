using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class BoardTribeAnalyzer
    {
        private static readonly Tribe[] PlayableTribes =
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

        public static Dictionary<Tribe, int> Build(IEnumerable<MinionInstance> board)
        {
            var distribution = new Dictionary<Tribe, int>();
            if (board == null)
            {
                return distribution;
            }

            foreach (var minion in board.Where(minion => minion != null && minion.CardKind == CardKind.Minion))
            {
                foreach (var tribe in GetCountedTribes(minion))
                {
                    distribution.TryGetValue(tribe, out var count);
                    distribution[tribe] = count + 1;
                }
            }

            return distribution;
        }

        public static int CountDistinctTribes(IEnumerable<MinionInstance> board)
        {
            return Build(board).Count;
        }

        public static void Refresh(LocalPlayerState player)
        {
            if (player == null)
            {
                return;
            }

            player.BoardTribeDistribution = Build(player.Board);
        }

        public static Tribe GetMostCommonTribe(LocalPlayerState player)
        {
            if (player == null)
            {
                return Tribe.None;
            }

            Refresh(player);
            return player.BoardTribeDistribution
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => TribeSortIndex(pair.Key))
                .Select(pair => pair.Key)
                .DefaultIfEmpty(Tribe.None)
                .FirstOrDefault();
        }

        public static List<MinionInstance> SelectOneOfEachTribe(IEnumerable<MinionInstance> board)
        {
            var selected = new List<MinionInstance>();
            var seen = new HashSet<Tribe>();
            var usedInstances = new HashSet<MinionInstance>();
            if (board == null)
            {
                return selected;
            }

            foreach (var minion in board.Where(minion => minion != null && minion.CardKind == CardKind.Minion))
            {
                if (usedInstances.Contains(minion))
                {
                    continue;
                }

                var tribe = GetCountedTribes(minion)
                    .Where(candidate => !seen.Contains(candidate))
                    .DefaultIfEmpty(Tribe.None)
                    .FirstOrDefault();
                if (tribe == Tribe.None)
                {
                    continue;
                }

                seen.Add(tribe);
                usedInstances.Add(minion);
                selected.Add(minion);
            }

            return selected;
        }

        public static List<Tribe> GetCountedTribes(MinionInstance minion)
        {
            if (minion == null || minion.CardKind != CardKind.Minion)
            {
                return new List<Tribe>();
            }

            var tribes = minion.Tribes == null ? new List<Tribe>() : minion.Tribes;
            if (tribes.Contains(Tribe.All))
            {
                return PlayableTribes.ToList();
            }

            return tribes.Where(tribe => tribe != Tribe.None && tribe != Tribe.All).Distinct().ToList();
        }

        private static int TribeSortIndex(Tribe tribe)
        {
            var index = System.Array.IndexOf(PlayableTribes, tribe);
            return index < 0 ? int.MaxValue : index;
        }
    }
}
