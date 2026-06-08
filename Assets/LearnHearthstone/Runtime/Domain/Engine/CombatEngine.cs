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

        public static CombatOutput SimulateBasicCombat(
            IEnumerable<MinionInstance> playerBoard,
            IEnumerable<MinionInstance> opponentBoard,
            int seed,
            int safetyLimit = 200,
            TavernState playerTavern = null,
            TavernState opponentTavern = null)
        {
            var player = playerBoard.Select(minion => minion.Clone()).Where(IsAlive).ToList();
            var opponent = opponentBoard.Select(minion => minion.Clone()).Where(IsAlive).ToList();
            var log = new List<CombatLogEntry>();
            var attackerSide = player.Count >= opponent.Count ? BoardSide.Player : BoardSide.Opponent;
            var playerAttackIndex = 0;
            var opponentAttackIndex = 0;
            var steps = 0;
            AddLog(log, "CombatStarted", "seed " + seed + " player " + player.Count + " opponent " + opponent.Count, null, null, LogSeverity.Normal);

            while (player.Any(IsAlive) && opponent.Any(IsAlive) && steps < safetyLimit)
            {
                steps += 1;
                var attackers = attackerSide == BoardSide.Player ? player : opponent;
                var defenders = attackerSide == BoardSide.Player ? opponent : player;
                var attackerIndex = FindNextAttackerIndex(attackers, attackerSide == BoardSide.Player ? playerAttackIndex : opponentAttackIndex);
                if (attackerIndex < 0)
                {
                    break;
                }

                var attacker = attackers[attackerIndex];
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

                ResolveDeaths(player, log, playerTavern);
                ResolveDeaths(opponent, log, opponentTavern);

                if (attackerSide == BoardSide.Player)
                {
                    playerAttackIndex = NormalizeAttackIndex(player, attackerIndex + 1);
                    opponentAttackIndex = NormalizeAttackIndex(opponent, opponentAttackIndex);
                }
                else
                {
                    opponentAttackIndex = NormalizeAttackIndex(opponent, attackerIndex + 1);
                    playerAttackIndex = NormalizeAttackIndex(player, playerAttackIndex);
                }

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

        private static void ResolveDeaths(List<MinionInstance> board, List<CombatLogEntry> log, TavernState tavern)
        {
            var index = 0;
            while (index < board.Count)
            {
                var minion = board[index];
                if (minion.Health > 0)
                {
                    index += 1;
                    continue;
                }

                board.RemoveAt(index);
                var inserted = 0;
                if (minion.Keywords.Contains(Keyword.Deathrattle))
                {
                    AddLog(log, "DeathrattleResolved", minion.InstanceId + " deathrattle", minion.InstanceId, null, LogSeverity.Normal);
                    inserted += ResolveDeathrattleSummons(minion, board, log, tavern, index);
                }

                if (minion.Keywords.Contains(Keyword.Reborn))
                {
                    var reborn = minion.Clone();
                    reborn.Health = 1;
                    reborn.MaxHealth = Math.Max(1, reborn.MaxHealth);
                    reborn.Keywords.Remove(Keyword.Reborn);
                    board.Insert(Math.Min(index + inserted, board.Count), reborn);
                    inserted += 1;
                    AddLog(log, "RebornResolved", minion.InstanceId + " reborn", minion.InstanceId, null, LogSeverity.Good);
                }

                index += inserted;
            }
        }

        private static int ResolveDeathrattleSummons(MinionInstance minion, List<MinionInstance> board, List<CombatLogEntry> log, TavernState tavern, int insertIndex)
        {
            var inserted = 0;
            switch (minion.CardId)
            {
                case CordPullerCardId:
                    inserted += AddToken(board, log, minion, insertIndex + inserted, "microbot", "微型机器人", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Mech) == null ? 0 : 1;
                    break;
                case HarmlessBoneheadCardId:
                    inserted += AddToken(board, log, minion, insertIndex + inserted, "skeleton", "骷髅", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Undead) == null ? 0 : 1;
                    inserted += AddToken(board, log, minion, insertIndex + inserted, "skeleton", "骷髅", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Undead) == null ? 0 : 1;
                    break;
                case ManasaberCardId:
                    inserted += AddToken(board, log, minion, insertIndex + inserted, "cubling", "豹宝宝", 0, minion.Golden ? 2 : 1, Tribe.Beast, Keyword.Taunt) == null ? 0 : 1;
                    inserted += AddToken(board, log, minion, insertIndex + inserted, "cubling", "豹宝宝", 0, minion.Golden ? 2 : 1, Tribe.Beast, Keyword.Taunt) == null ? 0 : 1;
                    break;
                case TwilightHatchlingCardId:
                    inserted += AddToken(board, log, minion, insertIndex + inserted, "hatchling", "雏龙", 3, 3, Tribe.Dragon) == null ? 0 : 1;
                    if (minion.Golden)
                    {
                        inserted += AddToken(board, log, minion, insertIndex + inserted, "hatchling", "雏龙", 3, 3, Tribe.Dragon) == null ? 0 : 1;
                    }
                    break;
                case ForestRoverCardId:
                    inserted += AddToken(
                        board,
                        log,
                        minion,
                        insertIndex + inserted,
                        "beetle",
                        "甲虫",
                        (minion.Golden ? 4 : 2) + (tavern?.BeetleAttackBonus ?? 0),
                        (minion.Golden ? 4 : 2) + (tavern?.BeetleHealthBonus ?? 0),
                        Tribe.Beast) == null ? 0 : 1;
                    break;
                case GlowgulletWarlordCardId:
                    inserted += AddBloodGemToken(board, log, minion, insertIndex + inserted);
                    inserted += AddBloodGemToken(board, log, minion, insertIndex + inserted);
                    if (minion.Golden)
                    {
                        inserted += AddBloodGemToken(board, log, minion, insertIndex + inserted);
                        inserted += AddBloodGemToken(board, log, minion, insertIndex + inserted);
                    }
                    break;
                case ScarletSkullCardId:
                    BuffFirstFriendly(board.Where(candidate => candidate.Tribes.Contains(Tribe.Undead)), minion.Golden ? 2 : 1, minion.Golden ? 4 : 2, "血色骷髅");
                    break;
            }

            return inserted;
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

        private static int AddBloodGemToken(List<MinionInstance> board, List<CombatLogEntry> log, MinionInstance source, int insertIndex)
        {
            var token = AddToken(board, log, source, insertIndex, "quilboar", "野猪人", 1, 1, Tribe.Quilboar, Keyword.Taunt);
            ApplyBloodGem(token);
            return token == null ? 0 : 1;
        }

        private static MinionInstance AddToken(List<MinionInstance> board, List<CombatLogEntry> log, MinionInstance source, int insertIndex, string tokenId, string name, int attack, int health, Tribe tribe, Keyword? keyword = null)
        {
            if (board.Count >= 7)
            {
                return null;
            }

            var keywords = new List<Keyword>();
            if (keyword.HasValue)
            {
                keywords.Add(keyword.Value);
            }

            var token = new MinionInstance
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
            };
            board.Insert(Math.Min(Math.Max(0, insertIndex), board.Count), token);
            AddLog(log, "MinionSummoned", source.InstanceId + " summoned " + name, source.InstanceId, null, LogSeverity.Good);
            return token;
        }

        private static void ApplyBloodGem(MinionInstance target)
        {
            if (target == null)
            {
                return;
            }

            target.Attack += 1;
            target.MaxHealth += 1;
            target.Health += 1;
            target.Enchantments.Add(new Enchantment
            {
                Id = "鲜血宝石",
                SourceId = "鲜血宝石",
                AttackBonus = 1,
                HealthBonus = 1
            });
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

        private static int FindNextAttackerIndex(IList<MinionInstance> board, int startIndex)
        {
            if (board.Count == 0)
            {
                return -1;
            }

            var normalized = NormalizeAttackIndex(board, startIndex);
            for (var offset = 0; offset < board.Count; offset += 1)
            {
                var index = (normalized + offset) % board.Count;
                var candidate = board[index];
                if (candidate != null && IsAlive(candidate) && candidate.CanAttack)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int NormalizeAttackIndex(IList<MinionInstance> board, int index)
        {
            if (board.Count == 0)
            {
                return 0;
            }

            var normalized = index % board.Count;
            return normalized < 0 ? normalized + board.Count : normalized;
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
