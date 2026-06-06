using System;
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
                var attackerVenomous = attacker.Keywords.Contains(Keyword.Venomous);
                var defenderVenomous = defender.Keywords.Contains(Keyword.Venomous);
                var attackerPoison = attacker.Keywords.Contains(Keyword.Poisonous) || attackerVenomous;
                var defenderPoison = defender.Keywords.Contains(Keyword.Poisonous) || defenderVenomous;
                var defenderDamage = DealDamage(defender, attacker.Attack, attackerPoison);
                var attackerDamage = DealDamage(attacker, defender.Attack, defenderPoison);
                var damagedDefender = defenderDamage.Minion;
                var damagedAttacker = attackerDamage.Minion;
                damagedAttacker.Keywords.Remove(Keyword.Stealth);

                if (attackerVenomous && defenderDamage.CombatDamageDealt)
                {
                    damagedAttacker.Keywords.Remove(Keyword.Venomous);
                }

                if (defenderVenomous && attackerDamage.CombatDamageDealt)
                {
                    damagedDefender.Keywords.Remove(Keyword.Venomous);
                }

                ReplaceByInstanceId(attackers, damagedAttacker);
                ReplaceByInstanceId(defenders, damagedDefender);

                log.Add(new CombatLogEntry
                {
                    Seq = steps,
                    Title = "攻击",
                    Detail = attacker.InstanceId + " 攻击 " + defender.InstanceId,
                    ActorId = attacker.InstanceId,
                    TargetId = defender.InstanceId,
                    Severity = LogSeverity.Normal
                });

                player = ResolveDeaths(player, log);
                opponent = ResolveDeaths(opponent, log);

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

        private static List<MinionInstance> ResolveDeaths(IEnumerable<MinionInstance> board, List<CombatLogEntry> log)
        {
            var result = new List<MinionInstance>();
            foreach (var minion in board)
            {
                if (minion.Health > 0)
                {
                    result.Add(minion);
                    continue;
                }

                if (minion.Keywords.Contains(Keyword.Deathrattle))
                {
                    log.Add(new CombatLogEntry
                    {
                        Seq = log.Count + 1,
                        Title = "DeathrattleResolved",
                        Detail = minion.InstanceId + " deathrattle",
                        ActorId = minion.InstanceId,
                        Severity = LogSeverity.Normal
                    });
                }

                if (minion.Keywords.Contains(Keyword.Reborn))
                {
                    var reborn = minion.Clone();
                    reborn.Health = 1;
                    reborn.MaxHealth = Math.Max(1, reborn.MaxHealth);
                    reborn.Keywords.Remove(Keyword.Reborn);
                    result.Add(reborn);
                    log.Add(new CombatLogEntry
                    {
                        Seq = log.Count + 1,
                        Title = "RebornResolved",
                        Detail = minion.InstanceId + " reborn",
                        ActorId = minion.InstanceId,
                        Severity = LogSeverity.Good
                    });
                }
            }

            return result;
        }

        private static MinionInstance ChooseDefender(IList<MinionInstance> defenders, int seed)
        {
            var visible = defenders.Where(minion => !minion.Keywords.Contains(Keyword.Stealth)).ToList();
            var targetPool = visible.Count > 0 ? visible : defenders;
            var taunts = targetPool.Where(minion => minion.Keywords.Contains(Keyword.Taunt)).ToList();
            var candidates = taunts.Count > 0 ? taunts : targetPool;
            return new SeededRng(seed).Pick(candidates);
        }

        private static DamageResult DealDamage(MinionInstance target, int amount, bool poison)
        {
            var next = target.Clone();
            if (amount <= 0)
            {
                return new DamageResult(next, false);
            }

            if (next.Keywords.Contains(Keyword.DivineShield))
            {
                next.Keywords.Remove(Keyword.DivineShield);
                return new DamageResult(next, false);
            }

            next.Health = poison ? 0 : next.Health - amount;
            return new DamageResult(next, true);
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

        private readonly struct DamageResult
        {
            public DamageResult(MinionInstance minion, bool combatDamageDealt)
            {
                Minion = minion;
                CombatDamageDealt = combatDamageDealt;
            }

            public MinionInstance Minion { get; }
            public bool CombatDamageDealt { get; }
        }
    }
}
