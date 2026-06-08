using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class CombatEngine
    {
        private const int BoardLimit = 7;
        private const string CordPullerCardId = "BG29_611";
        private const string HarmlessBoneheadCardId = "BG28_300";
        private const string ManasaberCardId = "BG26_800";
        private const string TwilightHatchlingCardId = "BG34_630";
        private const string ForestRoverCardId = "BG31_801";
        private const string GlowgulletWarlordCardId = "BG32_430";
        private const string ScarletSkullCardId = "BG25_022";
        private const string HummingBirdCardId = "BG26_805";
        private const string AlertAlarmistCardId = "BG35_340";
        private const string BristlebackBullyCardId = "BG35_432";
        private const string MetallicHunterCardId = "BG32_170";
        private const string TideRaiserCardId = "BG34_920";
        private const string SleepySupporterCardId = "BG33_241";
        private const string ExpertAviatorCardId = "BG34_140";
        private const string EternalKnightCardId = "BG25_008";
        private const string VeryHungryWinterfinnerCardId = "BG29_300";
        private const string BristlebackBloodGemCardId = "BRISTLEBACK_BLOOD_GEM";
        private const string PointyArrowCardId = "100596";

        public static CombatOutput SimulateBasicCombat(
            IEnumerable<MinionInstance> playerBoard,
            IEnumerable<MinionInstance> opponentBoard,
            int seed,
            int safetyLimit = 200,
            TavernState playerTavern = null,
            TavernState opponentTavern = null,
            IEnumerable<MinionInstance> playerHand = null,
            IEnumerable<MinionInstance> opponentHand = null)
        {
            var context = new CombatContext(
                playerBoard.Select(minion => minion.Clone()).Where(IsAlive).ToList(),
                opponentBoard.Select(minion => minion.Clone()).Where(IsAlive).ToList(),
                playerTavern,
                opponentTavern,
                playerHand?.Select(card => card.Clone()).ToList() ?? new List<MinionInstance>(),
                opponentHand?.Select(card => card.Clone()).ToList() ?? new List<MinionInstance>(),
                seed);
            ApplyStartOfCombatAuras(context.Player);
            ApplyStartOfCombatAuras(context.Opponent);

            var attackerSide = context.Player.Board.Count >= context.Opponent.Board.Count ? BoardSide.Player : BoardSide.Opponent;
            var steps = 0;
            AddLog(context.Log, "CombatStarted", "seed " + seed + " player " + context.Player.Board.Count + " opponent " + context.Opponent.Board.Count, null, null, LogSeverity.Normal);

            while (context.Player.Board.Any(IsAlive) && context.Opponent.Board.Any(IsAlive) && steps < safetyLimit)
            {
                var attackers = context.Get(attackerSide);
                var attackerIndex = FindNextAttackerIndex(attackers.Board, attackers.AttackIndex);
                if (attackerIndex < 0)
                {
                    break;
                }

                steps += 1;
                var attackResult = PerformAttack(context, attackerSide, attackerIndex, steps, false);
                ResolveExtraAttacks(context, attackResult, ref steps, safetyLimit);
                ResolveImmediateAttacks(context, ref steps, safetyLimit);

                if (attackerSide == BoardSide.Player)
                {
                    context.Player.AttackIndex = NormalizeAttackIndex(context.Player.Board, attackerIndex + 1);
                    context.Opponent.AttackIndex = NormalizeAttackIndex(context.Opponent.Board, context.Opponent.AttackIndex);
                }
                else
                {
                    context.Opponent.AttackIndex = NormalizeAttackIndex(context.Opponent.Board, attackerIndex + 1);
                    context.Player.AttackIndex = NormalizeAttackIndex(context.Player.Board, context.Player.AttackIndex);
                }

                attackerSide = attackerSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
            }

            var winner = context.Player.Board.Count == context.Opponent.Board.Count
                ? CombatWinner.Draw
                : context.Player.Board.Count > context.Opponent.Board.Count ? CombatWinner.Player : CombatWinner.Opponent;
            AddLog(context.Log, "CombatEnded", "winner " + winner + " steps " + steps + " safety " + (steps >= safetyLimit), null, null, LogSeverity.Normal);
            return new CombatOutput
            {
                Winner = winner,
                FinalPlayerBoard = context.Player.Board,
                FinalOpponentBoard = context.Opponent.Board,
                Log = context.Log,
                PlayerRewards = context.Player.Rewards,
                OpponentRewards = context.Opponent.Rewards,
                Steps = steps,
                SafetyStopped = steps >= safetyLimit
            };
        }

        private static void ApplyStartOfCombatAuras(CombatSideState side)
        {
            side.BeastAttackAura = side.Board
                .Where(minion => IsAlive(minion) && minion.CardId == HummingBirdCardId)
                .Sum(minion => minion.Golden ? 2 : 1);
            if (side.BeastAttackAura <= 0)
            {
                return;
            }

            foreach (var beast in side.Board.Where(minion => IsAlive(minion) && minion.Tribes.Contains(Tribe.Beast)))
            {
                BuffMinion(beast, side.BeastAttackAura, 0, "Humming Bird");
            }
        }

        private static void ApplySummonAuras(CombatSideState side, MinionInstance minion)
        {
            if (minion == null || side.BeastAttackAura <= 0 || !minion.Tribes.Contains(Tribe.Beast))
            {
                return;
            }

            BuffMinion(minion, side.BeastAttackAura, 0, "Humming Bird");
        }

        private static void ResolveExtraAttacks(CombatContext context, AttackResult attackResult, ref int steps, int safetyLimit)
        {
            if (!attackResult.AttackerSurvived || !attackResult.AttackerHadWindfury)
            {
                return;
            }

            if (steps >= safetyLimit || !context.Get(attackResult.DefenderSide).Board.Any(IsAlive))
            {
                return;
            }

            var attackers = context.Get(attackResult.AttackerSide);
            var attackerIndex = attackers.Board.FindIndex(minion => minion.InstanceId == attackResult.AttackerId);
            if (attackerIndex < 0)
            {
                return;
            }

            steps += 1;
            var windfuryResult = PerformAttack(context, attackResult.AttackerSide, attackerIndex, steps, true);
            ResolveImmediateAttacks(context, ref steps, safetyLimit);
            AddLog(context.Log, "WindfuryResolved", windfuryResult.AttackerId + " extra attack", windfuryResult.AttackerId, null, LogSeverity.Good);
        }

        private static void ResolveImmediateAttacks(CombatContext context, ref int steps, int safetyLimit)
        {
            while (context.ImmediateAttacks.Count > 0 && steps < safetyLimit && context.Player.Board.Any(IsAlive) && context.Opponent.Board.Any(IsAlive))
            {
                var request = context.ImmediateAttacks.Dequeue();
                var attackers = context.Get(request.Side);
                var attackerIndex = attackers.Board.FindIndex(minion => minion.InstanceId == request.InstanceId && IsAlive(minion));
                if (attackerIndex < 0)
                {
                    continue;
                }

                steps += 1;
                PerformAttack(context, request.Side, attackerIndex, steps, true);
            }
        }

        private static AttackResult PerformAttack(CombatContext context, BoardSide attackerSide, int attackerIndex, int step, bool triggeredAttack)
        {
            var attackers = context.Get(attackerSide);
            var defenders = context.Get(attackerSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            if (attackerIndex < 0 || attackerIndex >= attackers.Board.Count || !defenders.Board.Any(IsAlive))
            {
                return AttackResult.Empty(attackerSide);
            }

            var attacker = attackers.Board[attackerIndex];
            var defender = ChooseDefender(defenders.Board.Where(IsAlive).ToList(), context.Seed + step + context.AttackSequence);
            var attackerVenomous = attacker.Keywords.Contains(Keyword.Venomous);
            var defenderVenomous = defender.Keywords.Contains(Keyword.Venomous);
            var attackerPoison = attacker.Keywords.Contains(Keyword.Poisonous) || attackerVenomous;
            var defenderPoison = defender.Keywords.Contains(Keyword.Poisonous) || defenderVenomous;
            var defenderDamage = DealDamage(defender, attacker.Attack, attackerPoison);
            var attackerDamage = DealDamage(attacker, defender.Attack, defenderPoison);
            var damagedDefender = defenderDamage.Minion;
            var damagedAttacker = attackerDamage.Minion;
            damagedAttacker.Keywords.Remove(Keyword.Stealth);
            damagedAttacker.AttacksThisCombat += 1;

            if (attackerVenomous && defenderDamage.CombatDamageDealt)
            {
                damagedAttacker.Keywords.Remove(Keyword.Venomous);
            }

            if (defenderVenomous && attackerDamage.CombatDamageDealt)
            {
                damagedDefender.Keywords.Remove(Keyword.Venomous);
            }

            ReplaceByInstanceId(attackers.Board, damagedAttacker);
            ReplaceByInstanceId(defenders.Board, damagedDefender);
            QueueDamagedMinionRewards(context.Log, attackers, attacker, attackerDamage.CombatDamageDealt);
            QueueDamagedMinionRewards(context.Log, defenders, defender, defenderDamage.CombatDamageDealt);
            context.AttackSequence += 1;
            AddLog(
                context.Log,
                triggeredAttack ? "TriggeredAttackResolved" : "AttackResolved",
                attacker.InstanceId + " attacked " + defender.InstanceId,
                attacker.InstanceId,
                defender.InstanceId,
                LogSeverity.Normal);

            ResolveDeaths(context, attackers.Side);
            ResolveDeaths(context, defenders.Side);
            ResolveRally(context, attackers.Side, attacker.InstanceId);

            var attackerSurvived = attackers.Board.Any(minion => minion.InstanceId == attacker.InstanceId && IsAlive(minion));
            return new AttackResult(
                attacker.InstanceId,
                attackers.Side,
                defenders.Side,
                attackerSurvived,
                attacker.Keywords.Contains(Keyword.Windfury) && !triggeredAttack);
        }

        private static bool IsAlive(MinionInstance minion)
        {
            return minion.Health > 0;
        }

        private static void ResolveDeaths(CombatContext context, BoardSide side)
        {
            var owner = context.Get(side);
            var index = 0;
            while (index < owner.Board.Count)
            {
                var minion = owner.Board[index];
                if (minion.Health > 0)
                {
                    index += 1;
                    continue;
                }

                owner.Board.RemoveAt(index);
                var inserted = 0;
                AddReward(context.Log, owner, CombatRewardType.FriendlyMinionDied, minion.CardId, null, 1);
                if (minion.CardId == EternalKnightCardId)
                {
                    AddReward(context.Log, owner, CombatRewardType.EternalKnightDied, minion.CardId, null, 1);
                }

                if (minion.Keywords.Contains(Keyword.Deathrattle))
                {
                    AddLog(context.Log, "DeathrattleResolved", minion.InstanceId + " deathrattle", minion.InstanceId, null, LogSeverity.Normal);
                    inserted += ResolveDeathrattleSummons(context, owner, minion, index);
                }

                if (minion.Keywords.Contains(Keyword.Reborn))
                {
                    var reborn = minion.Clone();
                    reborn.Health = 1;
                    reborn.MaxHealth = Math.Max(1, reborn.MaxHealth);
                    reborn.Keywords.Remove(Keyword.Reborn);
                    ApplySummonAuras(owner, reborn);
                    owner.Board.Insert(Math.Min(index + inserted, owner.Board.Count), reborn);
                    inserted += 1;
                    AddLog(context.Log, "RebornResolved", minion.InstanceId + " reborn", minion.InstanceId, null, LogSeverity.Good);
                }

                index += inserted;
            }
        }

        private static int ResolveDeathrattleSummons(CombatContext context, CombatSideState owner, MinionInstance minion, int insertIndex)
        {
            var inserted = 0;
            switch (minion.CardId)
            {
                case CordPullerCardId:
                    inserted += AddToken(owner, context.Log, minion, insertIndex + inserted, "microbot", "Microbot", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Mech) == null ? 0 : 1;
                    break;
                case HarmlessBoneheadCardId:
                    inserted += AddToken(owner, context.Log, minion, insertIndex + inserted, "skeleton", "Skeleton", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Undead) == null ? 0 : 1;
                    inserted += AddToken(owner, context.Log, minion, insertIndex + inserted, "skeleton", "Skeleton", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Undead) == null ? 0 : 1;
                    break;
                case ManasaberCardId:
                    inserted += AddToken(owner, context.Log, minion, insertIndex + inserted, "cubling", "Cubling", 0, minion.Golden ? 2 : 1, Tribe.Beast, Keyword.Taunt) == null ? 0 : 1;
                    inserted += AddToken(owner, context.Log, minion, insertIndex + inserted, "cubling", "Cubling", 0, minion.Golden ? 2 : 1, Tribe.Beast, Keyword.Taunt) == null ? 0 : 1;
                    break;
                case TwilightHatchlingCardId:
                    inserted += AddImmediateAttackHatchling(context, owner, minion, insertIndex + inserted) == null ? 0 : 1;
                    if (minion.Golden)
                    {
                        inserted += AddImmediateAttackHatchling(context, owner, minion, insertIndex + inserted) == null ? 0 : 1;
                    }

                    break;
                case ForestRoverCardId:
                    inserted += AddToken(
                        owner,
                        context.Log,
                        minion,
                        insertIndex + inserted,
                        "beetle",
                        "Beetle",
                        (minion.Golden ? 4 : 2) + (owner.Tavern?.BeetleAttackBonus ?? 0),
                        (minion.Golden ? 4 : 2) + (owner.Tavern?.BeetleHealthBonus ?? 0),
                        Tribe.Beast) == null ? 0 : 1;
                    break;
                case GlowgulletWarlordCardId:
                    inserted += AddBloodGemToken(owner, context.Log, minion, insertIndex + inserted);
                    inserted += AddBloodGemToken(owner, context.Log, minion, insertIndex + inserted);
                    if (minion.Golden)
                    {
                        inserted += AddBloodGemToken(owner, context.Log, minion, insertIndex + inserted);
                        inserted += AddBloodGemToken(owner, context.Log, minion, insertIndex + inserted);
                    }

                    break;
                case ScarletSkullCardId:
                    BuffFirstFriendly(owner.Board.Where(candidate => candidate.Tribes.Contains(Tribe.Undead)), minion.Golden ? 2 : 1, minion.Golden ? 4 : 2, "Scarlet Skull");
                    break;
                case AlertAlarmistCardId:
                    AddReward(context.Log, owner, CombatRewardType.TavernSpellCostReduction, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case BristlebackBullyCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, minion.CardId, BristlebackBloodGemCardId, minion.Golden ? 2 : 1);
                    break;
                case MetallicHunterCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, minion.CardId, PointyArrowCardId, minion.Golden ? 2 : 1);
                    break;
                case TideRaiserCardId:
                    ResolveTideRaiser(context, owner, insertIndex, minion.Golden);
                    break;
            }

            if (minion.Tags.Contains("surf_n_surf_crab"))
            {
                var attack = minion.Counters.TryGetValue("surf_crab_attack", out var storedAttack) ? storedAttack : 3;
                var health = minion.Counters.TryGetValue("surf_crab_health", out var storedHealth) ? storedHealth : 2;
                inserted += AddToken(owner, context.Log, minion, insertIndex + inserted, "crab", "Crab", attack, health, Tribe.Beast) == null ? 0 : 1;
            }

            return inserted;
        }

        private static void ResolveTideRaiser(CombatContext context, CombatSideState owner, int deadIndex, bool golden)
        {
            var candidates = new List<MinionInstance>();
            if (deadIndex - 1 >= 0 && deadIndex - 1 < owner.Board.Count)
            {
                candidates.Add(owner.Board[deadIndex - 1]);
            }

            if (deadIndex >= 0 && deadIndex < owner.Board.Count)
            {
                candidates.Add(owner.Board[deadIndex]);
            }

            if (candidates.Count == 0)
            {
                return;
            }

            var targets = golden ? candidates : new List<MinionInstance> { new SeededRng(context.Seed + context.AttackSequence + deadIndex).Pick(candidates) };
            foreach (var target in targets)
            {
                var amount = target.Tribes.Contains(Tribe.Naga) ? 4 : 2;
                BuffMinion(target, amount, amount, "Shifting Tide");
                AddLog(context.Log, "CombatSpellCast", "Shifting Tide on " + target.InstanceId, TideRaiserCardId, target.InstanceId, LogSeverity.Good);
            }
        }

        private static void ResolveRally(CombatContext context, BoardSide side, string attackerId)
        {
            var owner = context.Get(side);
            var attackerIndex = owner.Board.FindIndex(minion => minion.InstanceId == attackerId && IsAlive(minion));
            if (attackerIndex < 0)
            {
                return;
            }

            var attacker = owner.Board[attackerIndex];
            if (attacker.CardId != SleepySupporterCardId || attackerIndex + 1 >= owner.Board.Count)
            {
                if (attacker.CardId == ExpertAviatorCardId)
                {
                    ResolveExpertAviatorRally(context, owner, attacker, attackerIndex);
                }

                return;
            }

            if (attacker.CardId != SleepySupporterCardId)
            {
                return;
            }

            var target = owner.Board[attackerIndex + 1];
            var amount = attacker.Golden ? 4 : 2;
            BuffMinion(target, amount, amount, "Sleepy Supporter");
            AddLog(context.Log, "RallyResolved", attacker.InstanceId + " rallied " + target.InstanceId, attacker.InstanceId, target.InstanceId, LogSeverity.Good);
        }

        private static void ResolveExpertAviatorRally(CombatContext context, CombatSideState owner, MinionInstance attacker, int attackerIndex)
        {
            var count = attacker.Golden ? 2 : 1;
            var candidates = owner.Hand
                .Where(card => card.CardKind == CardKind.Minion)
                .OrderByDescending(card => card.Attack)
                .ThenBy(card => card.InstanceId)
                .Take(count)
                .ToList();
            var insertIndex = attackerIndex + 1;
            foreach (var candidate in candidates)
            {
                if (owner.Board.Count >= BoardLimit)
                {
                    return;
                }

                var copy = candidate.Clone();
                copy.InstanceId = "combat-aviator-" + attacker.InstanceId + "-" + copy.InstanceId;
                copy.Owner = owner.Side;
                copy.PoolSource = PoolSource.Summon;
                copy.PoolCopiesHeld = 0;
                copy.CanAttack = true;
                ApplySummonAuras(owner, copy);
                owner.Board.Insert(Math.Min(insertIndex, owner.Board.Count), copy);
                insertIndex += 1;
                AddLog(context.Log, "RallyResolved", attacker.InstanceId + " summoned " + copy.InstanceId, attacker.InstanceId, copy.InstanceId, LogSeverity.Good);
            }
        }

        private static void AddReward(List<CombatLogEntry> log, CombatSideState owner, CombatRewardType type, string sourceCardId, string cardId, int amount)
        {
            AddReward(log, owner, type, sourceCardId, cardId, amount, 0, 0);
        }

        private static void AddReward(List<CombatLogEntry> log, CombatSideState owner, CombatRewardType type, string sourceCardId, string cardId, int amount, int attack, int health)
        {
            if (amount <= 0)
            {
                return;
            }

            owner.Rewards.Add(new CombatReward
            {
                Type = type,
                Side = owner.Side,
                SourceCardId = sourceCardId,
                CardId = cardId,
                Amount = amount,
                Attack = attack,
                Health = health
            });
            AddLog(log, "CombatRewardQueued", type + " x" + amount + " from " + sourceCardId, sourceCardId, cardId, LogSeverity.Good);
        }

        private static void QueueDamagedMinionRewards(List<CombatLogEntry> log, CombatSideState owner, MinionInstance damaged, bool tookDamage)
        {
            if (!tookDamage || damaged.CardId != VeryHungryWinterfinnerCardId)
            {
                return;
            }

            AddReward(
                log,
                owner,
                CombatRewardType.BuffHandMinion,
                damaged.CardId,
                null,
                1,
                damaged.Golden ? 4 : 2,
                damaged.Golden ? 2 : 1);
        }

        private static MinionInstance AddImmediateAttackHatchling(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex)
        {
            var token = AddToken(owner, context.Log, source, insertIndex, "hatchling", "Hatchling", 3, 3, Tribe.Dragon);
            if (token != null)
            {
                context.ImmediateAttacks.Enqueue(new ImmediateAttackRequest(owner.Side, token.InstanceId));
                AddLog(context.Log, "ImmediateAttackQueued", token.InstanceId + " queued", token.InstanceId, null, LogSeverity.Good);
            }

            return token;
        }

        private static void BuffFirstFriendly(IEnumerable<MinionInstance> candidates, int attack, int health, string sourceId)
        {
            var target = candidates.FirstOrDefault();
            if (target == null)
            {
                return;
            }

            BuffMinion(target, attack, health, sourceId);
        }

        private static int AddBloodGemToken(CombatSideState owner, List<CombatLogEntry> log, MinionInstance source, int insertIndex)
        {
            var token = AddToken(owner, log, source, insertIndex, "quilboar", "Quilboar", 1, 1, Tribe.Quilboar, Keyword.Taunt);
            ApplyBloodGem(token);
            return token == null ? 0 : 1;
        }

        private static MinionInstance AddToken(CombatSideState owner, List<CombatLogEntry> log, MinionInstance source, int insertIndex, string tokenId, string name, int attack, int health, Tribe tribe, Keyword? keyword = null)
        {
            if (owner.Board.Count >= BoardLimit)
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
                InstanceId = "token-" + source.InstanceId + "-" + tokenId + "-" + owner.Board.Count,
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
                Owner = owner.Side,
                CanAttack = true,
                PoolSource = PoolSource.Summon,
                PoolCopiesHeld = 0
            };
            ApplySummonAuras(owner, token);
            owner.Board.Insert(Math.Min(Math.Max(0, insertIndex), owner.Board.Count), token);
            AddLog(log, "MinionSummoned", source.InstanceId + " summoned " + name, source.InstanceId, token.InstanceId, LogSeverity.Good);
            return token;
        }

        private static void ApplyBloodGem(MinionInstance target)
        {
            if (target == null)
            {
                return;
            }

            BuffMinion(target, 1, 1, "Blood Gem");
        }

        private static void BuffMinion(MinionInstance target, int attack, int health, string sourceId)
        {
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

        private sealed class CombatContext
        {
            public CombatContext(List<MinionInstance> player, List<MinionInstance> opponent, TavernState playerTavern, TavernState opponentTavern, List<MinionInstance> playerHand, List<MinionInstance> opponentHand, int seed)
            {
                Player = new CombatSideState(BoardSide.Player, player, playerTavern, playerHand);
                Opponent = new CombatSideState(BoardSide.Opponent, opponent, opponentTavern, opponentHand);
                Seed = seed;
            }

            public CombatSideState Player { get; }
            public CombatSideState Opponent { get; }
            public int Seed { get; }
            public int AttackSequence { get; set; }
            public List<CombatLogEntry> Log { get; } = new List<CombatLogEntry>();
            public Queue<ImmediateAttackRequest> ImmediateAttacks { get; } = new Queue<ImmediateAttackRequest>();

            public CombatSideState Get(BoardSide side)
            {
                return side == BoardSide.Player ? Player : Opponent;
            }
        }

        private sealed class CombatSideState
        {
            public CombatSideState(BoardSide side, List<MinionInstance> board, TavernState tavern, List<MinionInstance> hand)
            {
                Side = side;
                Board = board;
                Tavern = tavern;
                Hand = hand;
            }

            public BoardSide Side { get; }
            public List<MinionInstance> Board { get; }
            public List<MinionInstance> Hand { get; }
            public TavernState Tavern { get; }
            public List<CombatReward> Rewards { get; } = new List<CombatReward>();
            public int AttackIndex { get; set; }
            public int BeastAttackAura { get; set; }
        }

        private readonly struct ImmediateAttackRequest
        {
            public ImmediateAttackRequest(BoardSide side, string instanceId)
            {
                Side = side;
                InstanceId = instanceId;
            }

            public BoardSide Side { get; }
            public string InstanceId { get; }
        }

        private readonly struct AttackResult
        {
            public AttackResult(string attackerId, BoardSide attackerSide, BoardSide defenderSide, bool attackerSurvived, bool attackerHadWindfury)
            {
                AttackerId = attackerId;
                AttackerSide = attackerSide;
                DefenderSide = defenderSide;
                AttackerSurvived = attackerSurvived;
                AttackerHadWindfury = attackerHadWindfury;
            }

            public string AttackerId { get; }
            public BoardSide AttackerSide { get; }
            public BoardSide DefenderSide { get; }
            public bool AttackerSurvived { get; }
            public bool AttackerHadWindfury { get; }

            public static AttackResult Empty(BoardSide attackerSide)
            {
                var defenderSide = attackerSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
                return new AttackResult(null, attackerSide, defenderSide, false, false);
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
