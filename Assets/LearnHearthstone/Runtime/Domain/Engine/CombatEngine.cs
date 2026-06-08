using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class CombatEngine
    {
        private const string CordPullerCardId = "BG29_611";
        private const string HarmlessBoneheadCardId = "BG28_300";
        private const string ManasaberCardId = "BG26_800";
        private const string TwilightHatchlingCardId = "BG34_630";
        private const string ForestRoverCardId = "BG31_801";
        private const string GlowgulletWarlordCardId = "BG32_430";
        private const string ScarletSkullCardId = "BG25_022";

        public static CombatOutput SimulateBasicCombat(IEnumerable<MinionInstance> playerBoard, IEnumerable<MinionInstance> opponentBoard, int seed, int safetyLimit = 200)
        {
            var player = playerBoard.Select(minion => minion.Clone()).Where(IsAlive).ToList();
            var opponent = opponentBoard.Select(minion => minion.Clone()).Where(IsAlive).ToList();
            var log = new List<CombatLogEntry>();
            var attackerSide = player.Count >= opponent.Count ? BoardSide.Player : BoardSide.Opponent;
            var steps = 0;
            AddLog(log, "CombatStarted", "seed " + seed + " player " + player.Count + " opponent " + opponent.Count, null, null, LogSeverity.Normal);

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

                AddLog(log, "攻击", attacker.InstanceId + " 攻击 " + defender.InstanceId, attacker.InstanceId, defender.InstanceId, LogSeverity.Normal);

                player = ResolveDeaths(player, log);
                opponent = ResolveDeaths(opponent, log);

                attackerSide = attackerSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
            }

            var winner = player.Count == opponent.Count ? CombatWinner.Draw : player.Count > opponent.Count ? CombatWinner.Player : CombatWinner.Opponent;
            AddLog(log, "CombatEnded", "winner " + winner + " steps " + steps + " safety " + (steps >= safetyLimit), null, null, LogSeverity.Normal);
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
                    AddLog(log, "DeathrattleResolved", minion.InstanceId + " deathrattle", minion.InstanceId, null, LogSeverity.Normal);
                    ResolveDeathrattleSummons(minion, result, log);
                }

                if (minion.Keywords.Contains(Keyword.Reborn))
                {
                    var reborn = minion.Clone();
                    reborn.Health = 1;
                    reborn.MaxHealth = Math.Max(1, reborn.MaxHealth);
                    reborn.Keywords.Remove(Keyword.Reborn);
                    result.Add(reborn);
                    AddLog(log, "RebornResolved", minion.InstanceId + " reborn", minion.InstanceId, null, LogSeverity.Good);
                }
            }

            return result;
        }

        private static void ResolveDeathrattleSummons(MinionInstance minion, List<MinionInstance> board, List<CombatLogEntry> log)
        {
            switch (minion.CardId)
            {
                case CordPullerCardId:
                    AddToken(board, log, minion, "microbot", "微型机器人", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Mech);
                    break;
                case HarmlessBoneheadCardId:
                    AddToken(board, log, minion, "skeleton", "骷髅", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Undead);
                    AddToken(board, log, minion, "skeleton", "骷髅", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Undead);
                    break;
                case ManasaberCardId:
                    AddToken(board, log, minion, "cubling", "豹宝宝", 0, minion.Golden ? 2 : 1, Tribe.Beast, Keyword.Taunt);
                    AddToken(board, log, minion, "cubling", "豹宝宝", 0, minion.Golden ? 2 : 1, Tribe.Beast, Keyword.Taunt);
                    break;
                case TwilightHatchlingCardId:
                    AddToken(board, log, minion, "hatchling", "雏龙", 3, 3, Tribe.Dragon);
                    if (minion.Golden)
                    {
                        AddToken(board, log, minion, "hatchling", "雏龙", 3, 3, Tribe.Dragon);
                    }
                    break;
                case ForestRoverCardId:
                    AddToken(board, log, minion, "beetle", "甲虫", minion.Golden ? 4 : 2, minion.Golden ? 4 : 2, Tribe.Beast);
                    break;
                case GlowgulletWarlordCardId:
                    AddToken(board, log, minion, "quilboar", "野猪人", 1, 1, Tribe.Quilboar, Keyword.Taunt);
                    AddToken(board, log, minion, "quilboar", "野猪人", 1, 1, Tribe.Quilboar, Keyword.Taunt);
                    if (minion.Golden)
                    {
                        AddToken(board, log, minion, "quilboar", "野猪人", 1, 1, Tribe.Quilboar, Keyword.Taunt);
                        AddToken(board, log, minion, "quilboar", "野猪人", 1, 1, Tribe.Quilboar, Keyword.Taunt);
                    }
                    break;
                case ScarletSkullCardId:
                    BuffFirstFriendly(board.Where(candidate => candidate.Tribes.Contains(Tribe.Undead)), minion.Golden ? 2 : 1, minion.Golden ? 4 : 2, "血色骷髅");
                    break;
            }
        }

        private static void BuffFirstFriendly(IEnumerable<MinionInstance> candidates, int attack, int health, string sourceId)
        {
            var target = candidates.FirstOrDefault();
            if (target == null)
            {
                return;
            }

            target.Attack += attack;
            target.MaxHealth += health;
            target.Health += health;
            target.Enchantments.Add(new Enchantment
            {
                Id = sourceId,
                SourceId = sourceId,
                AttackBonus = attack,
                HealthBonus = health
            });
        }

        private static void AddToken(List<MinionInstance> board, List<CombatLogEntry> log, MinionInstance source, string tokenId, string name, int attack, int health, Tribe tribe, Keyword? keyword = null)
        {
            if (board.Count >= 7)
            {
                return;
            }

            var keywords = new List<Keyword>();
            if (keyword.HasValue)
            {
                keywords.Add(keyword.Value);
            }

            board.Add(new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "token-" + source.InstanceId + "-" + tokenId + "-" + board.Count,
                DefinitionId = tokenId,
                CardId = tokenId.ToUpperInvariant(),
                Name = name,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                PoolSource = PoolSource.Summon,
                PoolCopiesHeld = 0
            });
            AddLog(log, "MinionSummoned", source.InstanceId + " summoned " + name, source.InstanceId, null, LogSeverity.Good);
        }

        private static void AddLog(List<CombatLogEntry> log, string title, string detail, string actorId, string targetId, LogSeverity severity)
        {
            log.Add(new CombatLogEntry
            {
                Seq = log.Count + 1,
                Title = title,
                Detail = detail,
                ActorId = actorId,
                TargetId = targetId,
                Severity = severity
            });
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
