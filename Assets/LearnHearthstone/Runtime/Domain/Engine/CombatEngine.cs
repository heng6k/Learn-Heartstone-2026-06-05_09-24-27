using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class CombatEngine
    {
        public static CombatOutput SimulateBasicCombat(IEnumerable<MinionInstance> playerBoard, IEnumerable<MinionInstance> opponentBoard, int seed, int safetyLimit = 200)
        {
            var player = playerBoard.Select(minion => minion.Clone()).Where(IsAlive).ToList();
            var opponent = opponentBoard.Select(minion => minion.Clone()).Where(IsAlive).ToList();
            var log = new List<CombatLogEntry>();
            var attackerSide = player.Count >= opponent.Count ? BoardSide.Player : BoardSide.Opponent;
            var steps = 0;

            while (player.Any(IsAlive) && opponent.Any(IsAlive) && steps < safetyLimit)
            {
                steps += 1;
                var attackers = attackerSide == BoardSide.Player ? player : opponent;
                var defenders = attackerSide == BoardSide.Player ? opponent : player;
                var attacker = attackers.FirstOrDefault(IsAlive);
                if (attacker == null)
                {
                    break;
                }

                var defender = ChooseDefender(defenders.Where(IsAlive).ToList(), seed + steps);
                var attackerPoison = attacker.Keywords.Contains(Keyword.Poisonous) || attacker.Keywords.Contains(Keyword.Venomous);
                var defenderPoison = defender.Keywords.Contains(Keyword.Poisonous) || defender.Keywords.Contains(Keyword.Venomous);
                var damagedDefender = DealDamage(defender, attacker.Attack, attackerPoison);
                var damagedAttacker = DealDamage(attacker, defender.Attack, defenderPoison);

                ReplaceByInstanceId(attackers, damagedAttacker);
                ReplaceByInstanceId(defenders, damagedDefender);
                player = player.Where(IsAlive).ToList();
                opponent = opponent.Where(IsAlive).ToList();

                log.Add(new CombatLogEntry
                {
                    Seq = steps,
                    Title = "攻击",
                    Detail = attacker.InstanceId + " 攻击 " + defender.InstanceId,
                    ActorId = attacker.InstanceId,
                    TargetId = defender.InstanceId,
                    Severity = LogSeverity.Normal
                });

                attackerSide = attackerSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
            }

            var winner = player.Count == opponent.Count ? CombatWinner.Draw : player.Count > opponent.Count ? CombatWinner.Player : CombatWinner.Opponent;
            return new CombatOutput
            {
                Winner = winner,
                FinalPlayerBoard = player,
                FinalOpponentBoard = opponent,
                Log = log,
                Steps = steps,
                SafetyStopped = steps >= safetyLimit
            };
        }

        private static bool IsAlive(MinionInstance minion)
        {
            return minion.Health > 0;
        }

        private static MinionInstance ChooseDefender(IList<MinionInstance> defenders, int seed)
        {
            var taunts = defenders.Where(minion => minion.Keywords.Contains(Keyword.Taunt)).ToList();
            var candidates = taunts.Count > 0 ? taunts : defenders;
            return new SeededRng(seed).Pick(candidates);
        }

        private static MinionInstance DealDamage(MinionInstance target, int amount, bool poison)
        {
            var next = target.Clone();
            if (amount <= 0)
            {
                return next;
            }

            if (next.Keywords.Contains(Keyword.DivineShield))
            {
                next.Keywords.Remove(Keyword.DivineShield);
                return next;
            }

            next.Health = poison ? 0 : next.Health - amount;
            return next;
        }

        private static void ReplaceByInstanceId(IList<MinionInstance> items, MinionInstance next)
        {
            for (var index = 0; index < items.Count; index += 1)
            {
                if (items[index].InstanceId == next.InstanceId)
                {
                    items[index] = next;
                    return;
                }
            }
        }
    }
}
