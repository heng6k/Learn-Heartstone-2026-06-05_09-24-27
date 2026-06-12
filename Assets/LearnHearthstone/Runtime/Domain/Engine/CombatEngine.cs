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
        private const string RotHideGnollCardId = "BG25_013";
        private const string ManasaberCardId = "BG26_800";
        private const string BuzzingVerminCardId = "BG31_803";
        private const string TwilightHatchlingCardId = "BG34_630";
        private const string ForestRoverCardId = "BG31_801";
        private const string GlowgulletWarlordCardId = "BG32_430";
        private const string ScarletSkullCardId = "BG25_022";
        private const string HummingBirdCardId = "BG26_805";
        private const string AlertAlarmistCardId = "BG35_340";
        private const string BristlebackBullyCardId = "BG35_432";
        private const string MetallicHunterCardId = "BG32_170";
        private const string TideRaiserCardId = "BG34_920";
        private const string HandlessForsakenCardId = "BG25_010";
        private const string BoneWatcherCardId = "BG30_125";
        private const string SlyRaptorCardId = "BG25_806";
        private const string AmberGuardianCardId = "BG24_500";
        private const string BristlebackScrapSmithCardId = "BG24_707";
        private const string GemDayPiperCardId = "BG26_160";
        private const string TreasureSeekerCardId = "BG26_360";
        private const string MummifierCardId = "BG28_309";
        private const string UtilityDroneCardId = "BG31_859";
        private const string BuriedBristlebackCardId = "BG32_434";
        private const string RadiantEmberCardId = "BG32_842";
        private const string DustboneDestroyerCardId = "BG33_323";
        private const string ColdlightDiverCardId = "BG33_894";
        private const string ToughOrcaCardId = "BG34_312";
        private const string JuvenileWaveCardId = "BG34_856";
        private const string RoaringRecruiterCardId = "BG29_816";
        private const string DeflectOBotCardId = "BGS_071";
        private const string WildfireElementalCardId = "BGS_126";
        private const string SleepySupporterCardId = "BG33_241";
        private const string ExpertAviatorCardId = "BG34_140";
        private const string EternalKnightCardId = "BG25_008";
        private const string VeryHungryWinterfinnerCardId = "BG29_300";
        private const string BristlebackBloodGemCardId = "BRISTLEBACK_BLOOD_GEM";
        private const string PointyArrowCardId = "100596";
        private const string PersistentPoetCardId = "BG29_813";
        private const string PersistentPoetSourceId = "Persistent Poet";
        private const string PrizedPromoDrakeCardId = "BG21_014";
        private const string HungrySnapjawCardId = "BG27_556";
        private const string SindoreiStraightShotCardId = "BG25_016";
        private const string BladeCollectorCardId = "BG26_817";
        private const string MonstrousMacawCardId = "BGS_078";
        private const string TopperTheThiefCardId = "BG33_822";
        private const string ValiantRebelCardId = "BG34_604";
        private const string RecruiterOfTheDeepCardId = "BG34_925";
        private const string BananaSlammaCardId = "BG26_802";
        private const string TrigoreTheLasherCardId = "BG29_807";
        private const string DevoutSatyressCardId = "BG33_155";
        private const string SilentSwarmguardCardId = "BG33_156";
        private const string TunnelBlasterCardId = "BG_DAL_775";
        private const string HeavyMetalWyrmCardId = "BG26_801";
        private const string TwilightNestmatronCardId = "BG34_731";
        private const string AutoAssemblerCardId = "BG32_172";
        private const string PlaguedGhoulCardId = "BG34_690";
        private const string DeepwaterChieftainCardId = "BG35_143";
        private const string ManasparkCardId = "BG35_881";
        private const string RefreshingAnomalyCardId = "BGS_116";
        private const string TavernTempestCardId = "BGS_123";
        private const string KingBagurgleCardId = "BGS_030";
        private const string PricklyPiperCardId = "BG26_525";
        private const string BalladistCardId = "BG26_814";
        private const string FeedingTigerSharkCardId = "BG34_523";
        private const string ScrapperCardId = "BG29_503";
        private const string BrannosaurCardId = "BG34_865";
        private const string SaloonDancerCardId = "BG35_702";
        private const string DustyCycloneCardId = "BG32_841";
        private const string FriendlyFelboarCardId = "BG32_880";
        private const string MobileProjectionistCardId = "BG31_175";
        private const string HatchingResearcherCardId = "BG34_632";
        private const string DreamingThornweaverCardId = "BG32_433";
        private const string RedChromawhelpCardId = "BG34_638t";
        private const string WyvernFrontmanCardId = "BG35_601";
        private const string KangorsApprenticeCardId = "BGS_012";
        private const string TitusRivendareCardId = "BG25_354";
        private const string LeeroyTheRecklessCardId = "BG23_318";
        private const string BarrensConjurerCardId = "BG29_862";
        private const string WintergraspGhoulCardId = "BG34_694";
        private const string CatacombCrasherCardId = "BG30_129";
        private const string DrustfallenButcherCardId = "BG32_324";
        private const string EternalSummonerCardId = "BG34_403";
        private const string ScreamingBansheeCardId = "BG35_334";
        private const string FarmhandWhirlOMatronCardId = "BG26_162";
        private const string NightmareParlorGuestCardId = "BG32_111";
        private const string ShadowdancerCardId = "BG32_891";
        private const string ScrapScraperCardId = "BG26_148";
        private const string ClunkerJunkerCardId = "BG35_890";
        private const string HolyMecherelCardId = "BG33_809";
        private const string ShipwreckedCaptainCardId = "BG33_821";
        private const string SewerRatPackCardId = "BG35_604";
        private const string BristlebachCardId = "BG29_808";
        private const string MoonRiderCardId = "BG35_602";
        private const string SkyfinRaptorCardId = "BG29_806";
        private const string TurquoiseSkittererCardId = "BG31_809";
        private const string ThreeLilQuilboarCardId = "BG26_867";
        private const string OperaticBelcherCardId = "BG33_318";
        private const string DragonCaretakerCardId = "BG34_633";
        private const string EternalSummonerHighCardId = "BG25_009";
        private const string CharlgaCardId = "BG26_157";
        private const string ChoralMrrrglrCardId = "BG26_354";
        private const string DeadlySporebatCardId = "BG31_835";
        private const string SilkyShimmermothCardId = "BG32_204";
        private const string FireforgedEvokerCardId = "BG32_822";
        private const string RuinsLordCardId = "BG33_154";
        private const string CharmingWingCardId = "BG33_240";
        private const string DeadseaSmasherCardId = "BG34_765";
        private const string RingingNagaCardId = "BG34_921";
        private const string QueenGuardCardId = "BG34_926";
        private const string FallenSkyGolemCardId = "BG35_342";
        private const string ThornedTrailblazerCardId = "BG35_437";
        private const string SkyPirateCardId = "SKY_PIRATE";
        private const string BloodChampionCardId = "BG23_017";
        private const string SargerasChampionCardId = "BG27_016";
        private const string ObsidianRavagerDragonCardId = "BG27_017";
        private const string StitchedReclaimerCardId = "BG31_999";
        private const string RheaSupremeWardenCardId = "BG34_319";
        private const string LastOfItsKindCardId = "BG34_320";
        private const string TenaciousKodoCardId = "BG34_322";
        private const string GoldrinnCardId = "BGS_018";
        private const string DisturbedGraveCardNumber = "126957";
        private const string ButcheringCardNumber = "110412";
        private const string MenagerieTablewareCardNumber = "105271";
        private const string StaffOfEnrichmentCardNumber = "105276";
        private const string SacredGiftCardNumber = "122899";
        private const string DeepwaterSchoolCardId = "131218";
        private const string ArcaneConsumptionCardId = "130311";
        private const string HealthyBountyCardId = "BG33_811";

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
            ApplyStartOfCombatAuras(context, context.Player);
            ApplyStartOfCombatAuras(context, context.Opponent);

            var attackerSide = context.Player.Board.Count >= context.Opponent.Board.Count ? BoardSide.Player : BoardSide.Opponent;
            var steps = 0;
            context.Replay.InitialSnapshot = CreateBoardPairSnapshot(context);
            AddLog(context.Log, "CombatStarted", "seed " + seed + " player " + context.Player.Board.Count + " opponent " + context.Opponent.Board.Count, null, null, LogSeverity.Normal);
            RecordFrame(context, CombatEventType.CombatStarted, "seed " + seed + " player " + context.Player.Board.Count + " opponent " + context.Opponent.Board.Count);

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

                AdvanceNaturalAttackPointers(context, attackerSide, attackerIndex);

                attackerSide = attackerSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
            }

            var winner = context.Player.Board.Count == context.Opponent.Board.Count
                ? CombatWinner.Draw
                : context.Player.Board.Count > context.Opponent.Board.Count ? CombatWinner.Player : CombatWinner.Opponent;
            AddLog(context.Log, "CombatEnded", "winner " + winner + " steps " + steps + " safety " + (steps >= safetyLimit), null, null, LogSeverity.Normal);
            context.Replay.Result = winner;
            context.Replay.Steps = steps;
            context.Replay.SafetyStopped = steps >= safetyLimit;
            context.Replay.PlayerRewards = CloneRewards(context.Player.Rewards);
            context.Replay.OpponentRewards = CloneRewards(context.Opponent.Rewards);
            RecordFrame(context, CombatEventType.CombatEnded, "winner " + winner + " steps " + steps + " safety " + (steps >= safetyLimit));
            return new CombatOutput
            {
                Winner = winner,
                FinalPlayerBoard = context.Player.Board,
                FinalOpponentBoard = context.Opponent.Board,
                Log = context.Log,
                Replay = context.Replay,
                PlayerRewards = context.Player.Rewards,
                OpponentRewards = context.Opponent.Rewards,
                Steps = steps,
                SafetyStopped = steps >= safetyLimit
            };
        }

        private static void ApplyStartOfCombatAuras(CombatContext context, CombatSideState side)
        {
            if (side.Tavern != null && (side.Tavern.NextCombatBoardAttack != 0 || side.Tavern.NextCombatBoardHealth != 0))
            {
                foreach (var minion in side.Board.Where(IsAlive))
                {
                    BuffMinion(minion, side.Tavern.NextCombatBoardAttack, side.Tavern.NextCombatBoardHealth, "Next Combat Tavern Spell");
                }
            }

            if (side.Tavern != null && side.Tavern.NextCombatBeetles > 0)
            {
                SummonStartOfCombatBeetles(context, side, side.Tavern.NextCombatBeetles);
            }

            if (side.Tavern != null)
            {
                ApplyDelayedTavernSpellCombatAuras(context, side);
            }

            foreach (var mrrrglr in side.Board.Where(minion => IsAlive(minion) && minion.CardId == ChoralMrrrglrCardId).ToList())
            {
                var multiplier = mrrrglr.Golden ? 2 : 1;
                var attack = side.Hand.Where(card => card.CardKind == CardKind.Minion).Sum(card => card.Attack) * multiplier;
                var health = side.Hand.Where(card => card.CardKind == CardKind.Minion).Sum(card => card.MaxHealth) * multiplier;
                BuffMinion(mrrrglr, attack, health, "Choral Mrrrglr");
            }

            foreach (var evoker in side.Board.Where(minion => IsAlive(minion) && minion.CardId == FireforgedEvokerCardId).ToList())
            {
                var attack = evoker.Golden ? 4 : 2;
                var health = evoker.Golden ? 2 : 1;
                evoker.Counters.TryGetValue("dragon_spell_attack", out var attackBonus);
                evoker.Counters.TryGetValue("dragon_spell_health", out var healthBonus);
                BuffAll(side.Board.Where(minion => IsAlive(minion) && minion.Tribes.Contains(Tribe.Dragon)), attack + attackBonus, health + healthBonus, "Fireforged Evoker");
            }

            ResolveStitchedReclaimersAtCombatStart(context, side);

            foreach (var promo in side.Board.Where(minion => IsAlive(minion) && minion.CardId == PrizedPromoDrakeCardId).ToList())
            {
                var amount = promo.Golden ? 8 : 4;
                foreach (var dragon in side.Board.Where(minion => IsAlive(minion) && minion.Tribes.Contains(Tribe.Dragon)))
                {
                    BuffMinion(dragon, amount, amount, "Prized Promo-Drake");
                }
            }

            foreach (var snapjaw in side.Board.Where(minion => IsAlive(minion) && minion.CardId == HungrySnapjawCardId).ToList())
            {
                SummonHighestAttackMurlocFromHand(context, side, snapjaw);
            }

            if (side.Tavern != null && side.Tavern.TemporaryAvengeBeastRewards > 0)
            {
                side.TemporaryAvengeBeastRewards = side.Tavern.TemporaryAvengeBeastRewards;
            }

            side.BeastAttackAura = side.Board
                .Where(minion => IsAlive(minion) && minion.CardId == HummingBirdCardId)
                .Sum(minion => minion.Golden ? 2 : 1);
            foreach (var guardian in side.Board.Where(minion => IsAlive(minion) && minion.CardId == AmberGuardianCardId).ToList())
            {
                var target = side.Board.FirstOrDefault(minion => minion.InstanceId != guardian.InstanceId && IsAlive(minion) && minion.Tribes.Contains(Tribe.Dragon));
                if (target == null)
                {
                    continue;
                }

                BuffMinion(target, guardian.Golden ? 4 : 2, guardian.Golden ? 4 : 2, "Amber Guardian");
                if (!target.Keywords.Contains(Keyword.DivineShield))
                {
                    target.Keywords.Add(Keyword.DivineShield);
                }
            }

            if (side.BeastAttackAura <= 0)
            {
                return;
            }

            foreach (var beast in side.Board.Where(minion => IsAlive(minion) && minion.Tribes.Contains(Tribe.Beast)))
            {
                BuffMinion(beast, side.BeastAttackAura, 0, "Humming Bird");
            }
        }

        private static void ApplyDelayedTavernSpellCombatAuras(CombatContext context, CombatSideState side)
        {
            var tavern = side.Tavern;
            var opponent = context.Get(side.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            for (var index = 0; index < tavern.NextCombatEnemyHealthToOne; index += 1)
            {
                var target = opponent.Board.Where(IsAlive).OrderByDescending(minion => minion.Health).ThenBy(minion => minion.InstanceId).FirstOrDefault();
                if (target != null)
                {
                    target.Health = 1;
                    AddLog(context.Log, "CombatSpellCast", "Tavern spell set " + target.InstanceId + " Health to 1", null, target.InstanceId, LogSeverity.Good);
                }
            }

            if (tavern.NextCombatLeftmostCopiesNearestEnemyStats)
            {
                var target = side.Board.FirstOrDefault(IsAlive);
                var enemy = opponent.Board.FirstOrDefault(IsAlive);
                if (target != null && enemy != null)
                {
                    BuffMinion(target, enemy.Attack, enemy.MaxHealth, "Share the Love");
                }
            }

            if (tavern.NextCombatLeftmostDoubleAttack)
            {
                var target = side.Board.FirstOrDefault(IsAlive);
                if (target != null)
                {
                    BuffMinion(target, target.Attack, 0, "Nozdormu's Offspring");
                }
            }

            if (tavern.NextCombatTriggerMixedMechanics)
            {
                AddReward(context.Log, side, CombatRewardType.AddGeneratedSpellToHand, "127642", ArcaneConsumptionCardId, 1);
                AddReward(context.Log, side, CombatRewardType.ImproveUndeadAttack, "127642", null, 2);
                AddReward(context.Log, side, CombatRewardType.AddRandomMagneticMechToHand, "127642", null, 1);
            }
        }

        private static void ResolveStitchedReclaimersAtCombatStart(CombatContext context, CombatSideState side)
        {
            foreach (var reclaimer in side.Board.Where(minion => IsAlive(minion) && minion.CardId == StitchedReclaimerCardId).ToList())
            {
                var index = side.Board.FindIndex(minion => minion.InstanceId == reclaimer.InstanceId);
                if (index < 0)
                {
                    continue;
                }

                var targetIndexes = reclaimer.Golden ? new[] { index - 1, index + 1 } : new[] { index - 1 };
                foreach (var targetIndex in targetIndexes.OrderByDescending(value => value))
                {
                    if (targetIndex < 0 || targetIndex >= side.Board.Count)
                    {
                        continue;
                    }

                    var target = side.Board[targetIndex];
                    if (target.CardId == StitchedReclaimerCardId)
                    {
                        continue;
                    }

                    var copy = target.Clone();
                    copy.InstanceId = "stitched-copy-" + reclaimer.InstanceId + "-" + target.InstanceId;
                    copy.Owner = side.Side;
                    copy.PoolSource = PoolSource.Summon;
                    copy.PoolCopiesHeld = 0;
                    side.StitchedCopies[reclaimer.InstanceId + ":" + target.InstanceId] = copy;
                    side.Board.RemoveAt(targetIndex);
                    AddLog(context.Log, "CombatStarted", reclaimer.InstanceId + " stitched " + target.InstanceId, reclaimer.InstanceId, target.InstanceId, LogSeverity.Good);
                }
            }
        }

        private static void SummonStartOfCombatBeetles(CombatContext context, CombatSideState side, int count)
        {
            var source = new MinionInstance
            {
                InstanceId = "start-of-combat-beetles-" + side.Side,
                DefinitionId = "boon-of-beetles",
                CardId = "110401",
                Name = "Boon of Beetles",
                Owner = side.Side,
                CardKind = CardKind.TavernSpell,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Tags = new List<string>()
            };

            for (var index = 0; index < count; index += 1)
            {
                AddToken(context, side, source, side.Board.Count, "boon-beetle", "Beetle", 1, 1, Tribe.Beast, Keyword.Taunt);
            }
        }

        private static void ApplySummonAuras(CombatSideState side, MinionInstance minion)
        {
            if (minion == null)
            {
                return;
            }

            if (side.BeastAttackAura > 0 && minion.Tribes.Contains(Tribe.Beast))
            {
                BuffMinion(minion, side.BeastAttackAura, 0, "Humming Bird");
            }

            foreach (var kodo in side.Board.Where(source => IsAlive(source) && source.CardId == TenaciousKodoCardId).ToList())
            {
                side.SummonAuraUses.TryGetValue(kodo.InstanceId, out var uses);
                if (uses >= 3 || minion.InstanceId == kodo.InstanceId)
                {
                    continue;
                }

                var amount = Math.Max(kodo.Attack, kodo.MaxHealth) * (kodo.Golden ? 2 : 1);
                BuffMinion(minion, amount, amount, "Tenacious Kodo");
                side.SummonAuraUses[kodo.InstanceId] = uses + 1;
            }
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
            RecordFrame(
                context,
                CombatEventType.WindfuryResolved,
                windfuryResult.AttackerId + " extra attack",
                windfuryResult.AttackerSide,
                windfuryResult.AttackerId,
                windfuryResult.DefenderSide,
                null,
                new[] { windfuryResult.AttackerId });
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
            var defenderIndex = defenders.Board.FindIndex(minion => minion.InstanceId == defender.InstanceId);
            var defenderHealthBeforeDamage = defender.Health;
            RecordFrame(
                context,
                CombatEventType.AttackDeclared,
                attacker.InstanceId + " attacks " + defender.InstanceId,
                attackers.Side,
                attacker.InstanceId,
                defenders.Side,
                defender.InstanceId,
                new[] { attacker.InstanceId, defender.InstanceId },
                null,
                null,
                null,
                null,
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                0,
                0,
                triggeredAttack);
            ResolveAttackDeclarationTriggers(context, attackers, attacker, defenders, defender, triggeredAttack);
            var attackerVenomous = attacker.Keywords.Contains(Keyword.Venomous);
            var defenderVenomous = defender.Keywords.Contains(Keyword.Venomous);
            var attackerPoison = attacker.Keywords.Contains(Keyword.Poisonous) || attackerVenomous;
            var defenderPoison = defender.Keywords.Contains(Keyword.Poisonous) || defenderVenomous;
            var defenderDamage = DealDamage(defender, attacker.Attack, attackerPoison);
            var attackerDamage = DealDamage(attacker, defender.Attack, defenderPoison);
            var damagedDefender = defenderDamage.Minion;
            var damagedAttacker = attackerDamage.Minion;
            ResolveCleaveDamage(context, attackers, attacker, defenders, defenderIndex);
            damagedAttacker.Keywords.Remove(Keyword.Stealth);
            damagedAttacker.AttacksThisCombat += 1;
            var damagedIds = new List<string>();
            if (attackerDamage.CombatDamageDealt || attackerDamage.DivineShieldBroken)
            {
                damagedIds.Add(attacker.InstanceId);
            }

            if (defenderDamage.CombatDamageDealt || defenderDamage.DivineShieldBroken)
            {
                damagedIds.Add(defender.InstanceId);
            }

            if (attackerVenomous && defenderDamage.CombatDamageDealt)
            {
                damagedAttacker.Keywords.Remove(Keyword.Venomous);
            }

            if (defenderVenomous && attackerDamage.CombatDamageDealt)
            {
                damagedDefender.Keywords.Remove(Keyword.Venomous);
            }

            if (damagedDefender.Health <= 0)
            {
                MarkKilledBy(damagedDefender, damagedAttacker.InstanceId);
            }

            if (damagedAttacker.Health <= 0)
            {
                MarkKilledBy(damagedAttacker, damagedDefender.InstanceId);
            }

            ReplaceByInstanceId(attackers.Board, damagedAttacker);
            ReplaceByInstanceId(defenders.Board, damagedDefender);
            ResolveOverkillTriggers(context, attackers, damagedAttacker, defenders, defenderIndex, defenderHealthBeforeDamage, defenderDamage);
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
            if (attackerDamage.DivineShieldBroken || defenderDamage.DivineShieldBroken)
            {
                RecordFrame(
                    context,
                    CombatEventType.DivineShieldBroken,
                    attacker.InstanceId + " / " + defender.InstanceId + " shield check",
                    attackers.Side,
                    attacker.InstanceId,
                    defenders.Side,
                    defender.InstanceId,
                    new[] { attacker.InstanceId, defender.InstanceId },
                    damagedIds);
            }

            if ((attackerVenomous && defenderDamage.CombatDamageDealt) || (defenderVenomous && attackerDamage.CombatDamageDealt))
            {
                RecordFrame(
                    context,
                    CombatEventType.VenomousResolved,
                    attacker.InstanceId + " / " + defender.InstanceId + " venomous resolved",
                    attackers.Side,
                    attacker.InstanceId,
                    defenders.Side,
                    defender.InstanceId,
                    new[] { attacker.InstanceId, defender.InstanceId },
                    damagedIds);
            }

            RecordFrame(
                context,
                CombatEventType.DamageResolved,
                attacker.InstanceId + " attacked " + defender.InstanceId,
                attackers.Side,
                attacker.InstanceId,
                defenders.Side,
                defender.InstanceId,
                new[] { attacker.InstanceId, defender.InstanceId },
                damagedIds,
                null,
                null,
                null,
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                (attackerDamage.CombatDamageDealt ? 1 : 0) + (defenderDamage.CombatDamageDealt ? 1 : 0),
                (attackerDamage.DivineShieldBroken ? 1 : 0) + (defenderDamage.DivineShieldBroken ? 1 : 0),
                triggeredAttack);
            ResolveDamageTriggers(
                context,
                attackers,
                attacker.InstanceId,
                attackerDamage.CombatDamageDealt,
                attackerDamage.DivineShieldBroken,
                defenders,
                defender.InstanceId,
                defenderDamage.CombatDamageDealt,
                defenderDamage.DivineShieldBroken);

            var deadIds = attackers.Board.Concat(defenders.Board).Where(minion => minion.Health <= 0).Select(minion => minion.InstanceId).ToList();
            if (deadIds.Count > 0)
            {
                RecordFrame(
                    context,
                    CombatEventType.DeathQueued,
                    "death queue " + string.Join(",", deadIds.ToArray()),
                    attackers.Side,
                    attacker.InstanceId,
                    defenders.Side,
                    defender.InstanceId,
                    deadIds,
                    null,
                    deadIds);
            }

            ResolveDeaths(context, attackers.Side);
            ResolveDeaths(context, defenders.Side);
            ResolveRally(context, attackers.Side, attacker.InstanceId, triggeredAttack);

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
            var newEntityIds = new List<string>();
            var retargetSourceIds = new List<string>();
            while (index < owner.Board.Count)
            {
                var minion = owner.Board[index];
                if (minion.Health > 0)
                {
                    index += 1;
                    continue;
                }

                TrackDeadMech(owner, minion);
                owner.Board.RemoveAt(index);
                var inserted = 0;
                var newEntityCountBeforeDeathEffects = newEntityIds.Count;
                AddReward(context.Log, owner, CombatRewardType.FriendlyMinionDied, minion.CardId, null, 1);
                ResolveRotHideGnollDeathAura(owner);
                if (minion.Keywords.Contains(Keyword.Taunt))
                {
                    QueueTauntDeathRewards(context, owner, minion);
                }

                if (minion.CardId == EternalKnightCardId)
                {
                    AddReward(context.Log, owner, CombatRewardType.EternalKnightDied, minion.CardId, null, 1);
                }

                ResolveAvenge(context, owner, minion.InstanceId);
                if (minion.Keywords.Contains(Keyword.Deathrattle))
                {
                    AddLog(context.Log, "DeathrattleResolved", minion.InstanceId + " deathrattle", minion.InstanceId, null, LogSeverity.Normal);
                    RecordFrame(
                        context,
                        CombatEventType.DeathrattleResolved,
                        minion.InstanceId + " deathrattle",
                        owner.Side,
                        minion.InstanceId,
                        owner.Side,
                        null,
                        new[] { minion.InstanceId },
                        null,
                        new[] { minion.InstanceId },
                        null,
                        new[] { minion.InstanceId });
                    var deathrattleRepeats = GetDeathrattleRepeats(owner);
                    AddReward(context.Log, owner, CombatRewardType.FriendlyDeathrattleTriggered, minion.CardId, null, deathrattleRepeats);
                    var thornedTrailblazerBonus = owner.Board
                        .Where(candidate => IsAlive(candidate) && candidate.CardId == ThornedTrailblazerCardId)
                        .Sum(candidate => candidate.Golden ? 2 : 1);
                    if (thornedTrailblazerBonus > 0)
                    {
                        AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemAttack, ThornedTrailblazerCardId, null, thornedTrailblazerBonus);
                    }

                    for (var repeat = 0; repeat < deathrattleRepeats; repeat += 1)
                    {
                        inserted += ResolveDeathrattleSummons(context, owner, minion, index + inserted, newEntityIds);
                    }
                }

                if (minion.Keywords.Contains(Keyword.Reborn))
                {
                    var reborn = minion.Clone();
                    reborn.Health = 1;
                    reborn.MaxHealth = Math.Max(1, reborn.MaxHealth);
                    reborn.Keywords.Remove(Keyword.Reborn);
                    if (owner.Board.Count >= BoardLimit)
                    {
                        RecordRebornOverflow(context, owner, minion);
                    }
                    else
                    {
                        ApplySummonAuras(owner, reborn);
                        owner.Board.Insert(Math.Min(index + inserted, owner.Board.Count), reborn);
                        inserted += 1;
                        newEntityIds.Add(reborn.InstanceId);
                        AddLog(context.Log, "RebornResolved", minion.InstanceId + " reborn", minion.InstanceId, null, LogSeverity.Good);
                        RecordFrame(
                            context,
                            CombatEventType.RebornResolved,
                            minion.InstanceId + " reborn",
                            owner.Side,
                            minion.InstanceId,
                            owner.Side,
                            reborn.InstanceId,
                            new[] { minion.InstanceId, reborn.InstanceId },
                            null,
                            null,
                            new[] { reborn.InstanceId },
                            new[] { minion.InstanceId });
                    }
                }

                if (newEntityIds.Count > newEntityCountBeforeDeathEffects)
                {
                    retargetSourceIds.Add(minion.InstanceId);
                }

                index += inserted;
            }

            RetargetAttackPointerToNewUnits(context, owner, newEntityIds, retargetSourceIds);
        }

        private static int ResolveDeathrattleSummons(CombatContext context, CombatSideState owner, MinionInstance minion, int insertIndex, List<string> newEntityIds)
        {
            var inserted = 0;
            switch (minion.CardId)
            {
                case CordPullerCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "microbot", "Microbot", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Mech);
                    break;
                case HarmlessBoneheadCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "skeleton", "Skeleton", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Undead);
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "skeleton", "Skeleton", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Undead);
                    break;
                case ManasaberCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "cubling", "Cubling", 0, minion.Golden ? 2 : 1, Tribe.Beast, Keyword.Taunt);
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "cubling", "Cubling", 0, minion.Golden ? 2 : 1, Tribe.Beast, Keyword.Taunt);
                    break;
                case BuzzingVerminCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "beetle", "Beetle", 2 + (owner.Tavern?.BeetleAttackBonus ?? 0), 2 + (owner.Tavern?.BeetleHealthBonus ?? 0), Tribe.Beast);
                    if (minion.Golden)
                    {
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "beetle", "Beetle", 2 + (owner.Tavern?.BeetleAttackBonus ?? 0), 2 + (owner.Tavern?.BeetleHealthBonus ?? 0), Tribe.Beast);
                    }

                    break;
                case TwilightHatchlingCardId:
                    inserted += AddImmediateAttackHatchling(context, owner, minion, insertIndex + inserted, newEntityIds) == null ? 0 : 1;
                    if (minion.Golden)
                    {
                        inserted += AddImmediateAttackHatchling(context, owner, minion, insertIndex + inserted, newEntityIds) == null ? 0 : 1;
                    }

                    break;
                case ForestRoverCardId:
                    inserted += AddTokenAndTrack(
                        context,
                        owner,
                        minion,
                        insertIndex + inserted,
                        newEntityIds,
                        "beetle",
                        "Beetle",
                        (minion.Golden ? 4 : 2) + (owner.Tavern?.BeetleAttackBonus ?? 0),
                        (minion.Golden ? 4 : 2) + (owner.Tavern?.BeetleHealthBonus ?? 0),
                        Tribe.Beast);
                    break;
                case GlowgulletWarlordCardId:
                    inserted += AddBloodGemToken(context, owner, minion, insertIndex + inserted, newEntityIds);
                    inserted += AddBloodGemToken(context, owner, minion, insertIndex + inserted, newEntityIds);
                    if (minion.Golden)
                    {
                        inserted += AddBloodGemToken(context, owner, minion, insertIndex + inserted, newEntityIds);
                        inserted += AddBloodGemToken(context, owner, minion, insertIndex + inserted, newEntityIds);
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
                case HandlessForsakenCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "reborn-hand", "Reborn Hand", 2, 1, Tribe.Undead, Keyword.Reborn);
                    if (minion.Golden)
                    {
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "reborn-hand", "Reborn Hand", 2, 1, Tribe.Undead, Keyword.Reborn);
                    }

                    break;
                case BoneWatcherCardId:
                    var skeletonCount = minion.Golden ? 6 : 3;
                    for (var summonIndex = 0; summonIndex < skeletonCount; summonIndex += 1)
                    {
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "skeleton", "Skeleton", 1, 1, Tribe.Undead);
                    }

                    break;
                case SlyRaptorCardId:
                    inserted += AddTokenAndTrack(
                        context,
                        owner,
                        minion,
                        insertIndex + inserted,
                        newEntityIds,
                        "summoned-beast",
                        "Summoned Beast",
                        minion.Golden ? 12 : 6,
                        minion.Golden ? 12 : 6,
                        Tribe.Beast);
                    break;
                case GemDayPiperCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemAttack, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case TreasureSeekerCardId:
                    AddReward(context.Log, owner, CombatRewardType.BuffHandMinion, minion.CardId, null, 1, minion.Golden ? 14 : 7, minion.Golden ? 14 : 7);
                    break;
                case MummifierCardId:
                    GiveDifferentUndeadReborn(context, owner, minion);
                    break;
                case BuriedBristlebackCardId:
                    ApplyBloodGemsToAdjacent(owner, insertIndex, minion.Golden ? 2 : 1);
                    break;
                case RadiantEmberCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveElementalHealth, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case ColdlightDiverCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, minion.CardId, "104436", minion.Golden ? 2 : 1);
                    break;
                case JuvenileWaveCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveRefreshBuff, minion.CardId, null, 1, minion.Golden ? 6 : 3, minion.Golden ? 6 : 3);
                    break;
                case HeavyMetalWyrmCardId:
                    TriggerAdjacentBattlecryResources(context, owner, minion, insertIndex);
                    break;
                case SilentSwarmguardCardId:
                    DealAreaDamage(context, owner, minion, minion.Golden ? 4 : 2, candidate => !(candidate.Owner == owner.Side && candidate.Tribes.Contains(Tribe.Demon)));
                    break;
                case TunnelBlasterCardId:
                    DealAreaDamage(context, owner, minion, minion.Golden ? 6 : 3, candidate => true);
                    break;
                case TwilightNestmatronCardId:
                    var twilightCount = minion.Golden ? 4 : 2;
                    for (var summonIndex = 0; summonIndex < twilightCount; summonIndex += 1)
                    {
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "twilight-hatchling", "Twilight Hatchling", 1, 1, Tribe.Dragon, Keyword.Taunt);
                    }

                    break;
                case AutoAssemblerCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "ancestral-automaton", "Ancestral Automaton", minion.Golden ? 6 : 3, minion.Golden ? 8 : 4, Tribe.Mech);
                    break;
                case PlaguedGhoulCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveUndeadAttack, minion.CardId, null, minion.Golden ? 4 : 2);
                    break;
                case FriendlyFelboarCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveTavernSpellAttack, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case DeepwaterChieftainCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, minion.CardId, DeepwaterSchoolCardId, minion.Golden ? 2 : 1);
                    break;
                case ManasparkCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, minion.CardId, ArcaneConsumptionCardId, minion.Golden ? 2 : 1);
                    break;
                case KangorsApprenticeCardId:
                    inserted += SummonDeadMechPlainCopies(context, owner, minion, insertIndex + inserted, newEntityIds, minion.Golden ? 4 : 2);
                    break;
                case LeeroyTheRecklessCardId:
                    DestroyKiller(context, owner, minion);
                    break;
                case BarrensConjurerCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomBattlecryMinionToHand, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case WintergraspGhoulCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, minion.CardId, DisturbedGraveCardNumber, minion.Golden ? 2 : 1);
                    break;
                case FarmhandWhirlOMatronCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveElementalShopStats, minion.CardId, null, minion.Golden ? 16 : 8);
                    break;
                case NightmareParlorGuestCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, minion.CardId, MenagerieTablewareCardNumber, minion.Golden ? 2 : 1);
                    break;
                case ShadowdancerCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, minion.CardId, StaffOfEnrichmentCardNumber, minion.Golden ? 2 : 1);
                    break;
                case ScrapScraperCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomMagneticMechToHand, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case ClunkerJunkerCardId:
                    BuffAll(owner.Board.Where(candidate => candidate.Tribes.Contains(Tribe.Mech)), minion.Golden ? 4 : 2, 0, "Clunker Junker");
                    break;
                case HolyMecherelCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, minion.CardId, SacredGiftCardNumber, minion.Golden ? 2 : 1);
                    break;
                case ShipwreckedCaptainCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddBountyToHand, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case SewerRatPackCardId:
                    var ratCount = minion.Golden ? 4 : 2;
                    for (var summonIndex = 0; summonIndex < ratCount; summonIndex += 1)
                    {
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "sewer-rat", "Sewer Rat", 2, 3, Tribe.Beast, Keyword.Taunt);
                    }

                    break;
                case BristlebachCardId:
                    BuffAll(owner.Board, 0, minion.Golden ? 2 : 1, "Bristlebach");
                    DealAreaDamage(context, owner, minion, 1, candidate => candidate.Owner == owner.Side);
                    break;
                case TurquoiseSkittererCardId:
                    if (owner.Tavern != null)
                    {
                        owner.Tavern.BeetleAttackBonus += minion.Golden ? 10 : 5;
                        owner.Tavern.BeetleHealthBonus += minion.Golden ? 10 : 5;
                    }

                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "beetle", "Beetle", 2 + (owner.Tavern?.BeetleAttackBonus ?? 0), 2 + (owner.Tavern?.BeetleHealthBonus ?? 0), Tribe.Beast);
                    break;
                case ThreeLilQuilboarCardId:
                    foreach (var quilboar in owner.Board.Where(candidate => candidate.Tribes.Contains(Tribe.Quilboar)).ToList())
                    {
                        BuffMinion(quilboar, minion.Golden ? 6 : 3, minion.Golden ? 6 : 3, "Three Lil' Quilboar");
                    }

                    break;
                case DragonCaretakerCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomChromawhelpToHand, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case EternalSummonerHighCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "eternal-knight", "Eternal Knight", minion.Golden ? 8 : 4, minion.Golden ? 2 : 1, Tribe.Undead, Keyword.Taunt);
                    break;
                case SilkyShimmermothCardId:
                    if (owner.Tavern != null)
                    {
                        owner.Tavern.BeetleAttackBonus += minion.Golden ? 4 : 2;
                        owner.Tavern.BeetleHealthBonus += minion.Golden ? 2 : 1;
                    }

                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "beetle", "Beetle", 2 + (owner.Tavern?.BeetleAttackBonus ?? 0), 2 + (owner.Tavern?.BeetleHealthBonus ?? 0), Tribe.Beast);
                    break;
                case DeadlySporebatCardId:
                    inserted += SummonLeftmostHandMinionForCombat(context, owner, minion, insertIndex + inserted, newEntityIds);
                    break;
                case QueenGuardCardId:
                    BuffAll(owner.Board, minion.Golden ? 4 : 2, minion.Golden ? 4 : 2, "Queen's Command");
                    BuffAll(owner.Board.Where(candidate => candidate.Tribes.Contains(Tribe.Naga)), minion.Golden ? 4 : 2, minion.Golden ? 4 : 2, "Queen's Command Naga");
                    break;
                case BloodChampionCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemAttack, minion.CardId, null, minion.Golden ? 2 : 1);
                    AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemHealth, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case SargerasChampionCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveTavernMinionStats, minion.CardId, null, minion.Golden ? 10 : 5);
                    break;
                case RheaSupremeWardenCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomTierSixMinionToHand, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case GoldrinnCardId:
                    BuffAll(owner.Board.Where(candidate => candidate.Tribes.Contains(Tribe.Beast)), minion.Golden ? 16 : 8, minion.Golden ? 16 : 8, "Goldrinn");
                    break;
                case StitchedReclaimerCardId:
                    foreach (var stored in owner.StitchedCopies.Where(pair => pair.Key.StartsWith(minion.InstanceId + ":", StringComparison.Ordinal)).Select(pair => pair.Value).ToList())
                    {
                        if (owner.Board.Count >= BoardLimit)
                        {
                            break;
                        }

                        var copy = stored.Clone();
                        copy.InstanceId = "stitched-summon-" + minion.InstanceId + "-" + newEntityIds.Count;
                        ApplySummonAuras(owner, copy);
                        owner.Board.Insert(Math.Min(insertIndex + inserted, owner.Board.Count), copy);
                        newEntityIds.Add(copy.InstanceId);
                        inserted += 1;
                    }

                    break;
            }

            if (minion.Tags.Contains("surf_n_surf_crab"))
            {
                var attack = minion.Counters.TryGetValue("surf_crab_attack", out var storedAttack) ? storedAttack : 3;
                var health = minion.Counters.TryGetValue("surf_crab_health", out var storedHealth) ? storedHealth : 2;
                inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "crab", "Crab", attack, health, Tribe.Beast);
            }

            return inserted;
        }

        private static void ResolveRotHideGnollDeathAura(CombatSideState owner)
        {
            foreach (var gnoll in owner.Board.Where(candidate => candidate.CardId == RotHideGnollCardId && IsAlive(candidate)).ToList())
            {
                BuffMinion(gnoll, gnoll.Golden ? 2 : 1, 0, "Rot Hide Gnoll");
            }
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
                RecordFrame(
                    context,
                    CombatEventType.CombatSpellCast,
                    "Shifting Tide on " + target.InstanceId,
                    owner.Side,
                    TideRaiserCardId,
                    owner.Side,
                    target.InstanceId,
                    new[] { TideRaiserCardId, target.InstanceId },
                    new[] { target.InstanceId },
                    null,
                    null,
                    new[] { TideRaiserCardId });
            }
        }

        private static void QueueTauntDeathRewards(CombatContext context, CombatSideState owner, MinionInstance deadMinion)
        {
            foreach (var source in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == BristlebackScrapSmithCardId))
            {
                AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, source.CardId, BristlebackBloodGemCardId, source.Golden ? 2 : 1);
            }
        }

        private static void DealAreaDamage(CombatContext context, CombatSideState owner, MinionInstance source, int amount, Func<MinionInstance, bool> canDamage)
        {
            var damagedIds = new List<string>();
            foreach (var side in new[] { context.Player, context.Opponent })
            {
                foreach (var target in side.Board.Where(minion => IsAlive(minion) && canDamage(minion)).ToList())
                {
                    var result = DealDamage(target, amount, false);
                    ReplaceByInstanceId(side.Board, result.Minion);
                    if (result.CombatDamageDealt || result.DivineShieldBroken)
                    {
                        damagedIds.Add(target.InstanceId);
                    }
                }
            }

            if (damagedIds.Count == 0)
            {
                return;
            }

            AddLog(context.Log, "DamageTriggered", source.InstanceId + " dealt area damage", source.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                source.InstanceId + " dealt area damage",
                owner.Side,
                source.InstanceId,
                owner.Side,
                null,
                damagedIds.Concat(new[] { source.InstanceId }),
                damagedIds,
                null,
                null,
                new[] { source.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                damagedIds.Count);
        }

        private static void TriggerAdjacentBattlecryResources(CombatContext context, CombatSideState owner, MinionInstance source, int deadIndex)
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

            var targets = candidates.Where(IsAlive).ToList();
            if (targets.Count == 0)
            {
                return;
            }

            if (!source.Golden && targets.Count > 1)
            {
                targets = new List<MinionInstance> { new SeededRng(context.Seed + context.AttackSequence + deadIndex).Pick(targets) };
            }

            foreach (var target in targets)
            {
                TriggerBattlecryResource(context, owner, source, target);
            }
        }

        private static void TriggerBattlecryResource(CombatContext context, CombatSideState owner, MinionInstance source, MinionInstance target)
        {
            var multiplier = target.Golden ? 2 : 1;
            switch (target.CardId)
            {
                case FeedingTigerSharkCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomBeastToHand, source.CardId, null, multiplier);
                    break;
                case PricklyPiperCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomDemonToHand, source.CardId, null, multiplier);
                    break;
                case BalladistCardId:
                    BuffFirstFriendly(owner.Board.Where(minion => minion.InstanceId != target.InstanceId && minion.Tribes.Contains(Tribe.Pirate)), 0, multiplier, "Balladist");
                    break;
                case KingBagurgleCardId:
                    foreach (var murloc in owner.Board.Where(minion => minion.InstanceId != target.InstanceId && minion.Tribes.Contains(Tribe.Murloc)))
                    {
                        BuffMinion(murloc, 4 * multiplier, 4 * multiplier, "King Bagurgle");
                    }

                    break;
                case ScrapperCardId:
                    MagnetizeRandomMechOntoFriendlyMech(context, owner, target, multiplier);
                    break;
                case DeepwaterChieftainCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, source.CardId, DeepwaterSchoolCardId, multiplier);
                    break;
                case ManasparkCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, source.CardId, ArcaneConsumptionCardId, multiplier);
                    break;
                case RefreshingAnomalyCardId:
                    AddReward(context.Log, owner, CombatRewardType.GainFreeRefresh, source.CardId, null, 2 * multiplier);
                    break;
                case TavernTempestCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomElementalToHand, source.CardId, null, multiplier);
                    break;
                case BrannosaurCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveRefreshBuff, source.CardId, null, 1, 7 * multiplier, 7 * multiplier);
                    break;
                case SaloonDancerCardId:
                    BuffFirstFriendly(owner.Board.Where(minion => minion.InstanceId != target.InstanceId), 2 * multiplier, 2 * multiplier, "Saloon Dancer");
                    break;
                case DustyCycloneCardId:
                    foreach (var elemental in owner.Board.Where(minion => minion.Tribes.Contains(Tribe.Elemental)))
                    {
                        BuffMinion(elemental, multiplier, 0, "Dusty Cyclone");
                    }

                    break;
            }
        }

        private static void MagnetizeRandomMechOntoFriendlyMech(CombatContext context, CombatSideState owner, MinionInstance source, int count)
        {
            var target = owner.Board.FirstOrDefault(minion => minion.InstanceId != source.InstanceId && IsAlive(minion) && (minion.Tribes.Contains(Tribe.Mech) || minion.Tribes.Contains(Tribe.All)))
                ?? owner.Board.FirstOrDefault(minion => IsAlive(minion) && (minion.Tribes.Contains(Tribe.Mech) || minion.Tribes.Contains(Tribe.All)));
            if (target == null)
            {
                return;
            }

            var amount = Math.Max(1, count);
            for (var index = 0; index < amount; index += 1)
            {
                BuffMinion(target, 2, 2, "Scrapper Magnetic");
            }
        }

        private static void GiveDifferentUndeadReborn(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            var target = owner.Board.FirstOrDefault(minion => minion.InstanceId != source.InstanceId && IsAlive(minion) && minion.Tribes.Contains(Tribe.Undead) && !minion.Keywords.Contains(Keyword.Reborn));
            if (target == null)
            {
                return;
            }

            target.Keywords.Add(Keyword.Reborn);
            AddLog(context.Log, "DeathrattleResolved", source.InstanceId + " gave Reborn to " + target.InstanceId, source.InstanceId, target.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DeathrattleResolved,
                source.InstanceId + " gave Reborn to " + target.InstanceId,
                owner.Side,
                source.InstanceId,
                owner.Side,
                target.InstanceId,
                new[] { source.InstanceId, target.InstanceId },
                null,
                null,
                null,
                new[] { source.InstanceId });
        }

        private static void ApplyBloodGemsToAdjacent(CombatSideState owner, int deadIndex, int count)
        {
            var targets = new List<MinionInstance>();
            if (deadIndex - 1 >= 0 && deadIndex - 1 < owner.Board.Count)
            {
                targets.Add(owner.Board[deadIndex - 1]);
            }

            if (deadIndex >= 0 && deadIndex < owner.Board.Count)
            {
                targets.Add(owner.Board[deadIndex]);
            }

            foreach (var target in targets.Where(IsAlive))
            {
                for (var index = 0; index < count; index += 1)
                {
                    ApplyBloodGem(target, owner.Tavern);
                }
            }
        }

        private static void ResolveRally(CombatContext context, BoardSide side, string attackerId, bool triggeredAttack)
        {
            var owner = context.Get(side);
            var attackerIndex = owner.Board.FindIndex(minion => minion.InstanceId == attackerId && IsAlive(minion));
            if (attackerIndex < 0)
            {
                return;
            }

            var attacker = owner.Board[attackerIndex];
            if (attacker.CardId == DustboneDestroyerCardId)
            {
                ResolveDustboneDestroyerRally(context, owner, attacker, triggeredAttack);
            }

            ResolveHighTierRally(context, owner, attacker, attackerIndex);

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
            RecordFrame(
                context,
                CombatEventType.RallyResolved,
                attacker.InstanceId + " rallied " + target.InstanceId,
                owner.Side,
                attacker.InstanceId,
                owner.Side,
                target.InstanceId,
                new[] { attacker.InstanceId, target.InstanceId },
                new[] { target.InstanceId },
                null,
                null,
                new[] { attacker.InstanceId });
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
                RecordFrame(
                    context,
                    CombatEventType.RallyResolved,
                    attacker.InstanceId + " summoned " + copy.InstanceId,
                    owner.Side,
                    attacker.InstanceId,
                    owner.Side,
                    copy.InstanceId,
                    new[] { attacker.InstanceId, copy.InstanceId },
                    null,
                    null,
                    new[] { copy.InstanceId },
                    new[] { attacker.InstanceId });
            }
        }

        private static void ResolveDustboneDestroyerRally(CombatContext context, CombatSideState owner, MinionInstance attacker, bool triggeredAttack)
        {
            var amount = attacker.Golden ? 2 : 1;
            var targets = owner.Board
                .Where(minion => minion.InstanceId != attacker.InstanceId && IsAlive(minion) && minion.Tribes.Contains(Tribe.Undead))
                .ToList();
            if (targets.Count == 0)
            {
                return;
            }

            foreach (var target in targets)
            {
                BuffMinion(target, amount, 0, "Dustbone Destroyer");
            }

            AddLog(context.Log, "AttackTriggered", attacker.InstanceId + " rallied undead attack +" + amount, attacker.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.AttackTriggered,
                attacker.InstanceId + " rallied undead attack +" + amount,
                owner.Side,
                attacker.InstanceId,
                owner.Side,
                null,
                targets.Select(target => target.InstanceId).Concat(new[] { attacker.InstanceId }),
                null,
                null,
                null,
                new[] { attacker.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                0,
                0,
                triggeredAttack);
        }

        private static void ResolveAttackDeclarationTriggers(CombatContext context, CombatSideState owner, MinionInstance attacker, CombatSideState defenderOwner, MinionInstance defender, bool triggeredAttack)
        {
            if (!IsAlive(attacker))
            {
                return;
            }

            if (attacker.CardId == SindoreiStraightShotCardId && defender != null)
            {
                defender.Keywords.Remove(Keyword.Reborn);
                defender.Keywords.Remove(Keyword.Taunt);
                AddLog(context.Log, "AttackTriggered", attacker.InstanceId + " purged Reborn/Taunt from " + defender.InstanceId, attacker.InstanceId, defender.InstanceId, LogSeverity.Good);
            }

            if (attacker.CardId == ValiantRebelCardId && defender != null)
            {
                BuffMinion(attacker, defender.Attack, 0, "Valiant Rebel");
                AddLog(context.Log, "AttackTriggered", attacker.InstanceId + " gained target attack", attacker.InstanceId, defender.InstanceId, LogSeverity.Good);
            }

            if (attacker.CardId == OperaticBelcherCardId)
            {
                var target = owner.Board.FirstOrDefault(minion => minion.InstanceId != attacker.InstanceId && minion.Tribes.Contains(Tribe.Murloc));
                if (target != null && !target.Keywords.Contains(Keyword.Venomous))
                {
                    target.Keywords.Add(Keyword.Venomous);
                    AddLog(context.Log, "AttackTriggered", attacker.InstanceId + " gave Venomous to " + target.InstanceId, attacker.InstanceId, target.InstanceId, LogSeverity.Good);
                }
            }

            if (attacker.CardId == MonstrousMacawCardId)
            {
                TriggerLeftmostOtherDeathrattle(context, owner, attacker);
            }

            if (attacker.CardId == TopperTheThiefCardId)
            {
                AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, attacker.CardId, HealthyBountyCardId, attacker.Golden ? 2 : 1);
            }

            if (attacker.CardId == MobileProjectionistCardId)
            {
                AddReward(context.Log, owner, CombatRewardType.AddRandomMagneticMechToHand, attacker.CardId, null, attacker.Golden ? 2 : 1);
            }

            if (attacker.CardId == RecruiterOfTheDeepCardId)
            {
                CastChefChoiceOnRightNeighbor(context, owner, attacker);
                if (attacker.Golden)
                {
                    CastChefChoiceOnRightNeighbor(context, owner, attacker);
                }
            }

            if (!attacker.Tribes.Contains(Tribe.Dragon))
            {
                return;
            }

            // The attacking Roaring Recruiter is not "another" friendly Dragon for its own trigger.
            var sources = owner.Board
                .Where(minion => minion.CardId == RoaringRecruiterCardId && minion.InstanceId != attacker.InstanceId && IsAlive(minion))
                .ToList();
            foreach (var source in sources)
            {
                var attack = source.Golden ? 6 : 3;
                var health = source.Golden ? 2 : 1;
                BuffMinion(attacker, attack, health, "Roaring Recruiter");
                AddLog(context.Log, "AttackTriggered", source.InstanceId + " buffed attacking dragon " + attacker.InstanceId, source.InstanceId, attacker.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.AttackTriggered,
                    source.InstanceId + " buffed attacking dragon " + attacker.InstanceId,
                    owner.Side,
                    source.InstanceId,
                    owner.Side,
                    attacker.InstanceId,
                    new[] { source.InstanceId, attacker.InstanceId },
                    null,
                    null,
                    null,
                    new[] { source.InstanceId },
                    null,
                    BoardSide.Player,
                    -1,
                    0,
                    0,
                    0,
                    0,
                    triggeredAttack);
            }
        }

        private static void ResolveDamageTriggers(
            CombatContext context,
            CombatSideState attackerOwner,
            string attackerId,
            bool attackerTookDamage,
            bool attackerShieldBroken,
            CombatSideState defenderOwner,
            string defenderId,
            bool defenderTookDamage,
            bool defenderShieldBroken)
        {
            ResolveToughOrcaDamageTrigger(context, attackerOwner, attackerId, attackerTookDamage, attackerShieldBroken);
            ResolveToughOrcaDamageTrigger(context, defenderOwner, defenderId, defenderTookDamage, defenderShieldBroken);
            ResolveTrigoreDamageTrigger(context, attackerOwner, attackerId, attackerTookDamage);
            ResolveTrigoreDamageTrigger(context, defenderOwner, defenderId, defenderTookDamage);
            ResolveSkyfinDamageTrigger(context, attackerOwner, attackerId, attackerTookDamage);
            ResolveSkyfinDamageTrigger(context, defenderOwner, defenderId, defenderTookDamage);
            ResolveDevoutSatyressDamageTrigger(context, attackerOwner, attackerId, defenderTookDamage || defenderShieldBroken);
            ResolveDevoutSatyressDamageTrigger(context, defenderOwner, defenderId, attackerTookDamage || attackerShieldBroken);
            ResolveRuinsLordDamageTrigger(context, attackerOwner, attackerId, defenderTookDamage || defenderShieldBroken);
            ResolveRuinsLordDamageTrigger(context, defenderOwner, defenderId, attackerTookDamage || attackerShieldBroken);
            ResolveWyvernDamageRefreshTrigger(context, attackerOwner, attackerId, attackerTookDamage);
            ResolveWyvernDamageRefreshTrigger(context, defenderOwner, defenderId, defenderTookDamage);
            ResolveSilkyShimmermothDamageTrigger(context, attackerOwner, attackerId, attackerTookDamage);
            ResolveSilkyShimmermothDamageTrigger(context, defenderOwner, defenderId, defenderTookDamage);
        }

        private static void ResolveWyvernDamageRefreshTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage)
        {
            if (!tookDamage)
            {
                return;
            }

            var source = owner.Board.FirstOrDefault(minion => minion.InstanceId == damagedId && minion.CardId == WyvernFrontmanCardId);
            if (source == null)
            {
                return;
            }

            source.Counters.TryGetValue("wyvern_refreshes", out var triggers);
            if (triggers >= 3)
            {
                return;
            }

            source.Counters["wyvern_refreshes"] = triggers + 1;
            AddReward(context.Log, owner, CombatRewardType.GainFreeRefresh, source.CardId, null, source.Golden ? 2 : 1);
            AddLog(context.Log, "DamageTriggered", source.InstanceId + " gained free refresh", source.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                source.InstanceId + " gained free refresh",
                owner.Side,
                source.InstanceId,
                owner.Side,
                source.InstanceId,
                new[] { source.InstanceId },
                new[] { source.InstanceId },
                null,
                null,
                new[] { source.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                1,
                0,
                false,
                triggers + 1,
                3);
        }

        private static void ResolveSilkyShimmermothDamageTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage)
        {
            if (!tookDamage || owner.Tavern == null)
            {
                return;
            }

            var source = owner.Board.FirstOrDefault(minion => minion.InstanceId == damagedId && minion.CardId == SilkyShimmermothCardId);
            if (source == null)
            {
                return;
            }

            owner.Tavern.BeetleAttackBonus += source.Golden ? 4 : 2;
            owner.Tavern.BeetleHealthBonus += source.Golden ? 2 : 1;
            AddLog(context.Log, "DamageTriggered", source.InstanceId + " improved Beetles", source.InstanceId, null, LogSeverity.Good);
        }

        private static void ResolveOverkillTriggers(
            CombatContext context,
            CombatSideState attackerOwner,
            MinionInstance attacker,
            CombatSideState defenderOwner,
            int defenderIndex,
            int defenderHealthBeforeDamage,
            DamageResult defenderDamage)
        {
            if (attacker.CardId != WildfireElementalCardId || !defenderDamage.CombatDamageDealt || defenderDamage.Minion.Health > 0)
            {
                return;
            }

            var excess = Math.Max(0, attacker.Attack - defenderHealthBeforeDamage);
            if (excess <= 0)
            {
                return;
            }

            var targets = new List<MinionInstance>();
            if (defenderIndex - 1 >= 0 && defenderIndex - 1 < defenderOwner.Board.Count)
            {
                targets.Add(defenderOwner.Board[defenderIndex - 1]);
            }

            if (defenderIndex + 1 >= 0 && defenderIndex + 1 < defenderOwner.Board.Count)
            {
                targets.Add(defenderOwner.Board[defenderIndex + 1]);
            }

            var damagedIds = new List<string>();
            foreach (var target in targets.Where(IsAlive))
            {
                var result = DealDamage(target, excess * (attacker.Golden ? 2 : 1), false);
                ReplaceByInstanceId(defenderOwner.Board, result.Minion);
                if (result.CombatDamageDealt || result.DivineShieldBroken)
                {
                    damagedIds.Add(target.InstanceId);
                }

                ResolveWyvernDamageRefreshTrigger(context, defenderOwner, target.InstanceId, result.CombatDamageDealt);
            }

            if (damagedIds.Count == 0)
            {
                return;
            }

            AddLog(context.Log, "DamageTriggered", attacker.InstanceId + " dealt excess damage", attacker.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                attacker.InstanceId + " dealt excess damage",
                attackerOwner.Side,
                attacker.InstanceId,
                defenderOwner.Side,
                null,
                damagedIds.Concat(new[] { attacker.InstanceId }),
                damagedIds,
                null,
                null,
                new[] { attacker.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                damagedIds.Count);
        }

        private static void ResolveCleaveDamage(CombatContext context, CombatSideState attackerOwner, MinionInstance attacker, CombatSideState defenderOwner, int defenderIndex)
        {
            if (attacker.CardId != BladeCollectorCardId || attacker.Attack <= 0)
            {
                return;
            }

            var damagedIds = new List<string>();
            foreach (var index in new[] { defenderIndex - 1, defenderIndex + 1 })
            {
                if (index < 0 || index >= defenderOwner.Board.Count)
                {
                    continue;
                }

                var target = defenderOwner.Board[index];
                if (!IsAlive(target))
                {
                    continue;
                }

                var result = DealDamage(target, attacker.Attack, attacker.Keywords.Contains(Keyword.Poisonous) || attacker.Keywords.Contains(Keyword.Venomous));
                ReplaceByInstanceId(defenderOwner.Board, result.Minion);
                if (result.CombatDamageDealt || result.DivineShieldBroken)
                {
                    damagedIds.Add(target.InstanceId);
                }

                ResolveWyvernDamageRefreshTrigger(context, defenderOwner, target.InstanceId, result.CombatDamageDealt);
            }

            if (damagedIds.Count == 0)
            {
                return;
            }

            AddLog(context.Log, "DamageTriggered", attacker.InstanceId + " cleaved adjacent targets", attacker.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                attacker.InstanceId + " cleaved adjacent targets",
                attackerOwner.Side,
                attacker.InstanceId,
                defenderOwner.Side,
                null,
                damagedIds.Concat(new[] { attacker.InstanceId }),
                damagedIds,
                null,
                null,
                new[] { attacker.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                damagedIds.Count);
        }

        private static void TriggerLeftmostOtherDeathrattle(CombatContext context, CombatSideState owner, MinionInstance attacker)
        {
            var target = owner.Board.FirstOrDefault(minion => minion.InstanceId != attacker.InstanceId && IsAlive(minion) && minion.Keywords.Contains(Keyword.Deathrattle));
            if (target == null)
            {
                return;
            }

            var index = owner.Board.FindIndex(minion => minion.InstanceId == target.InstanceId);
            var newEntityIds = new List<string>();
            ResolveDeathrattleSummons(context, owner, target, Math.Max(0, index + 1), newEntityIds);
            AddLog(context.Log, "AttackTriggered", attacker.InstanceId + " triggered " + target.InstanceId + " deathrattle", attacker.InstanceId, target.InstanceId, LogSeverity.Good);
        }

        private static void CastChefChoiceOnRightNeighbor(CombatContext context, CombatSideState owner, MinionInstance attacker)
        {
            var attackerIndex = owner.Board.FindIndex(minion => minion.InstanceId == attacker.InstanceId);
            if (attackerIndex < 0 || attackerIndex + 1 >= owner.Board.Count)
            {
                return;
            }

            var target = owner.Board[attackerIndex + 1];
            var tribes = BoardTribeAnalyzer.GetCountedTribes(target);
            if (tribes.Count == 0)
            {
                return;
            }

            var tribe = tribes.Count == 1 ? tribes[0] : new SeededRng(context.Seed + context.AttackSequence + attackerIndex).Pick(tribes);
            AddReward(context.Log, owner, CombatRewardType.AddRandomSameTribeMinionToHand, attacker.CardId, tribe.ToString(), 1);
            AddLog(context.Log, "CombatSpellCast", attacker.InstanceId + " cast Chef's Choice on " + target.InstanceId + " (" + tribe + ")", attacker.InstanceId, target.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.CombatSpellCast,
                attacker.InstanceId + " cast Chef's Choice on " + target.InstanceId,
                owner.Side,
                attacker.InstanceId,
                owner.Side,
                target.InstanceId,
                new[] { attacker.InstanceId, target.InstanceId },
                new[] { target.InstanceId },
                null,
                null,
                new[] { attacker.InstanceId });
        }

        private static void ResolveHighTierRally(CombatContext context, CombatSideState owner, MinionInstance attacker, int attackerIndex)
        {
            switch (attacker.CardId)
            {
                case CharmingWingCardId:
                    BuffAll(owner.Board.Where(minion => minion.InstanceId != attacker.InstanceId && minion.Tribes.Contains(Tribe.Dragon)).Take(2), 0, attacker.MaxHealth * (attacker.Golden ? 2 : 1), "Charming Wing");
                    break;
                case DeadseaSmasherCardId:
                    BuffAll(owner.Board.Where(minion => minion.InstanceId != attacker.InstanceId).Take(attacker.Golden ? 6 : 3), attacker.Attack, 0, "Deadsea Smasher");
                    break;
                case QueenGuardCardId:
                    BuffAll(owner.Board, attacker.Golden ? 4 : 2, attacker.Golden ? 4 : 2, "Queen's Command");
                    BuffAll(owner.Board.Where(minion => minion.Tribes.Contains(Tribe.Naga)), attacker.Golden ? 4 : 2, attacker.Golden ? 4 : 2, "Queen's Command Naga");
                    break;
                case RheaSupremeWardenCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomTierSixMinionToHand, attacker.CardId, null, attacker.Golden ? 2 : 1);
                    break;
                case LastOfItsKindCardId:
                    var repeats = attacker.Golden ? 2 : 1;
                    for (var repeat = 0; repeat < repeats; repeat += 1)
                    {
                        BuffOneOfEachTribe(owner.Board, 12, 12, "The Last of Its Kind");
                    }

                    break;
                case ObsidianRavagerDragonCardId:
                    DamageRallyTargetAndNeighbor(context, owner, attacker, attackerIndex);
                    break;
                case RingingNagaCardId:
                    BuffMinion(attacker, attacker.Golden ? 4 : 2, attacker.Golden ? 4 : 2, "Shiny Ring");
                    break;
                case "BG35_700":
                    SummonSkyPirate(context, owner, attacker, attackerIndex);
                    break;
            }
        }

        private static void BuffOneOfEachTribe(IEnumerable<MinionInstance> board, int attack, int health, string sourceId)
        {
            var seen = new HashSet<Tribe>();
            foreach (var minion in board.Where(IsAlive))
            {
                var tribe = minion.Tribes.FirstOrDefault(candidate => candidate != Tribe.None && candidate != Tribe.All && !seen.Contains(candidate));
                if (tribe == Tribe.None)
                {
                    continue;
                }

                seen.Add(tribe);
                BuffMinion(minion, attack, health, sourceId);
            }
        }

        private static void DamageRallyTargetAndNeighbor(CombatContext context, CombatSideState owner, MinionInstance attacker, int attackerIndex)
        {
            var defenderOwner = context.Get(owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            var targetIndex = defenderOwner.Board.FindIndex(IsAlive);
            if (targetIndex < 0)
            {
                return;
            }

            var indexes = attacker.Golden
                ? new[] { targetIndex - 1, targetIndex, targetIndex + 1 }
                : new[] { targetIndex, targetIndex + 1 };
            var damagedIds = new List<string>();
            foreach (var index in indexes.Distinct())
            {
                if (index < 0 || index >= defenderOwner.Board.Count)
                {
                    continue;
                }

                var target = defenderOwner.Board[index];
                if (!IsAlive(target))
                {
                    continue;
                }

                var result = DealDamage(target, attacker.Attack, false);
                ReplaceByInstanceId(defenderOwner.Board, result.Minion);
                if (result.CombatDamageDealt || result.DivineShieldBroken)
                {
                    damagedIds.Add(target.InstanceId);
                }
            }

            if (damagedIds.Count > 0)
            {
                AddLog(context.Log, "RallyResolved", attacker.InstanceId + " ravaged adjacent targets", attacker.InstanceId, null, LogSeverity.Good);
            }
        }

        private static void SummonSkyPirate(CombatContext context, CombatSideState owner, MinionInstance attacker, int attackerIndex)
        {
            if (owner.Board.Count >= BoardLimit)
            {
                return;
            }

            var token = AddToken(context, owner, attacker, Math.Min(attackerIndex + 1, owner.Board.Count), "sky-pirate", "Sky Pirate", attacker.Attack, 1, Tribe.Pirate);
            if (token == null)
            {
                return;
            }

            token.CardId = SkyPirateCardId;
            context.ImmediateAttacks.Enqueue(new ImmediateAttackRequest(owner.Side, token.InstanceId));
        }

        private static void ResolveToughOrcaDamageTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage, bool shieldBroken)
        {
            if (!tookDamage)
            {
                return;
            }

            var source = owner.Board.FirstOrDefault(minion => minion.InstanceId == damagedId && minion.CardId == ToughOrcaCardId && IsAlive(minion));
            if (source == null)
            {
                return;
            }

            var amount = source.Golden ? 2 : 1;
            var targets = owner.Board
                .Where(minion => minion.InstanceId != source.InstanceId && IsAlive(minion))
                .ToList();
            if (targets.Count == 0)
            {
                return;
            }

            foreach (var target in targets)
            {
                BuffMinion(target, amount, amount, "Tough Orca");
            }

            AddLog(context.Log, "DamageTriggered", source.InstanceId + " buffed other friendly minions", source.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                source.InstanceId + " buffed other friendly minions",
                owner.Side,
                source.InstanceId,
                owner.Side,
                null,
                targets.Select(target => target.InstanceId).Concat(new[] { source.InstanceId }),
                new[] { source.InstanceId },
                null,
                null,
                new[] { source.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                1,
                shieldBroken ? 1 : 0,
                false);
        }

        private static void ResolveTrigoreDamageTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage)
        {
            if (!tookDamage)
            {
                return;
            }

            var damaged = owner.Board.FirstOrDefault(minion => minion.InstanceId == damagedId && minion.Tribes.Contains(Tribe.Beast));
            if (damaged == null)
            {
                return;
            }

            foreach (var trigore in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == TrigoreTheLasherCardId && minion.InstanceId != damagedId))
            {
                BuffMinion(trigore, 0, trigore.Golden ? 4 : 2, "Trigore the Lasher");
                AddLog(context.Log, "DamageTriggered", trigore.InstanceId + " gained Health from damaged Beast", trigore.InstanceId, damagedId, LogSeverity.Good);
            }
        }

        private static void ResolveSkyfinDamageTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage)
        {
            if (!tookDamage)
            {
                return;
            }

            var damaged = owner.Board.FirstOrDefault(minion => minion.InstanceId == damagedId && minion.Tribes.Contains(Tribe.Beast));
            if (damaged == null)
            {
                return;
            }

            foreach (var skyfin in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == SkyfinRaptorCardId))
            {
                var target = owner.Board.FirstOrDefault(minion => IsAlive(minion) && minion.Tribes.Contains(Tribe.Beast) && minion.InstanceId != damagedId);
                if (target == null)
                {
                    continue;
                }

                BuffMinion(target, skyfin.Golden ? 6 : 3, skyfin.Golden ? 4 : 2, "Skyfin Raptor");
                AddLog(context.Log, "DamageTriggered", skyfin.InstanceId + " buffed " + target.InstanceId, skyfin.InstanceId, target.InstanceId, LogSeverity.Good);
            }
        }

        private static void ResolveDevoutSatyressDamageTrigger(CombatContext context, CombatSideState owner, string sourceId, bool dealtDamage)
        {
            if (!dealtDamage)
            {
                return;
            }

            var source = owner.Board.FirstOrDefault(minion => minion.InstanceId == sourceId && minion.Tribes.Contains(Tribe.Demon));
            if (source == null)
            {
                return;
            }

            foreach (var satyress in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == DevoutSatyressCardId && minion.InstanceId != source.InstanceId))
            {
                BuffMinion(satyress, satyress.Golden ? 2 : 1, satyress.Golden ? 4 : 2, "Devout Satyress");
                AddLog(context.Log, "DamageTriggered", satyress.InstanceId + " grew from Demon damage", satyress.InstanceId, sourceId, LogSeverity.Good);
            }
        }

        private static void ResolveRuinsLordDamageTrigger(CombatContext context, CombatSideState owner, string sourceId, bool dealtDamage)
        {
            if (!dealtDamage)
            {
                return;
            }

            var source = owner.Board.FirstOrDefault(minion => minion.InstanceId == sourceId && minion.Tribes.Contains(Tribe.Demon));
            if (source == null)
            {
                return;
            }

            foreach (var ruinsLord in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == RuinsLordCardId).ToList())
            {
                var attack = ruinsLord.Golden ? 4 : 2;
                var health = ruinsLord.Golden ? 2 : 1;
                BuffAll(owner.Board.Where(minion => minion.InstanceId != source.InstanceId), attack, health, "Ruins Lord");
                AddLog(context.Log, "DamageTriggered", ruinsLord.InstanceId + " rewarded Demon damage", ruinsLord.InstanceId, source.InstanceId, LogSeverity.Good);
            }
        }

        private static void ResolveAvenge(CombatContext context, CombatSideState owner, string deadId)
        {
            if (owner.TemporaryAvengeBeastRewards > 0)
            {
                owner.AvengeCounters.TryGetValue("temporary-beast-revenge", out var temporaryCount);
                temporaryCount += 1;
                if (temporaryCount >= 4)
                {
                    temporaryCount = 0;
                    AddReward(context.Log, owner, CombatRewardType.AddRandomBeastToHand, "123553", null, owner.TemporaryAvengeBeastRewards);
                }

                owner.AvengeCounters["temporary-beast-revenge"] = temporaryCount;
            }

            var sources = owner.Board
                .Where(minion => IsAlive(minion) && (minion.Keywords.Contains(Keyword.Avenge) || minion.EffectIds.Contains("avenge_2_buff_self_2_2")))
                .ToList();
            foreach (var source in sources)
            {
                var threshold = source.Counters.TryGetValue("avenge_threshold", out var storedThreshold) ? Math.Max(1, storedThreshold) : GetDefaultAvengeThreshold(source);
                var count = source.Counters.TryGetValue("avenge_count", out var storedCount) ? storedCount + 1 : 1;
                var triggered = count >= threshold;
                if (triggered)
                {
                    source.Counters["avenge_count"] = 0;
                    if (source.CardId == HatchingResearcherCardId)
                    {
                        AddReward(context.Log, owner, CombatRewardType.AddRandomChromawhelpToHand, source.CardId, null, source.Golden ? 2 : 1);
                    }
                    else if (source.CardId == DreamingThornweaverCardId)
                    {
                        AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemHealth, source.CardId, null, source.Golden ? 2 : 1);
                    }
                    else if (source.CardId == DrustfallenButcherCardId)
                    {
                        AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, source.CardId, ButcheringCardNumber, source.Golden ? 2 : 1);
                    }
                    else if (source.CardId == EternalSummonerCardId)
                    {
                        var token = AddToken(context, owner, source, owner.Board.Count, "eternal-knight", "Eternal Knight", 4, 1, Tribe.Undead);
                        if (token != null)
                        {
                            token.CardId = EternalKnightCardId;
                            token.Keywords.Add(Keyword.Windfury);
                            AddLog(context.Log, "Avenge", source.InstanceId + " summoned Eternal Knight", source.InstanceId, token.InstanceId, LogSeverity.Good);
                        }
                    }
                    else if (source.CardId == CharlgaCardId)
                    {
                        var gems = source.Golden ? 4 : 2;
                        foreach (var quilboar in owner.Board.Where(minion => IsAlive(minion) && minion.Tribes.Contains(Tribe.Quilboar)).ToList())
                        {
                            for (var gem = 0; gem < gems; gem += 1)
                            {
                                ApplyBloodGem(quilboar, owner.Tavern);
                            }
                        }
                    }
                    else if (source.CardId == DeadlySporebatCardId)
                    {
                        AddReward(context.Log, owner, CombatRewardType.AddRandomSameTribeMinionToHand, source.CardId, Tribe.Undead.ToString(), source.Golden ? 2 : 1);
                    }
                    else if (source.CardId == ScreamingBansheeCardId)
                    {
                        source.Counters.TryGetValue("banshee_bonus", out var bonus);
                        bonus += source.Golden ? 2 : 1;
                        source.Counters["banshee_bonus"] = bonus;
                    }
                    else
                    {
                        BuffMinion(source, source.Golden ? 4 : 2, source.Golden ? 4 : 2, "Avenge");
                    }
                }
                else
                {
                    source.Counters["avenge_count"] = count;
                }

                AddLog(context.Log, "AvengeCounterUpdated", source.InstanceId + " avenge " + count + "/" + threshold, source.InstanceId, deadId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.AvengeCounterUpdated,
                    source.InstanceId + " avenge " + count + "/" + threshold,
                    owner.Side,
                    source.InstanceId,
                    owner.Side,
                    source.InstanceId,
                    new[] { source.InstanceId, deadId },
                    null,
                    new[] { deadId },
                    null,
                    new[] { source.InstanceId },
                    null,
                    BoardSide.Player,
                    -1,
                    0,
                    0,
                    0,
                    0,
                    false,
                    count,
                    threshold);
            }
        }

        private static int GetDefaultAvengeThreshold(MinionInstance source)
        {
            if (source.CardId == DrustfallenButcherCardId)
            {
                return 3;
            }

            if (source.CardId == EternalSummonerCardId)
            {
                return 5;
            }

            if (source.CardId == ScreamingBansheeCardId)
            {
                return 2;
            }

            if (source.CardId == HatchingResearcherCardId || source.CardId == DreamingThornweaverCardId)
            {
                return 3;
            }

            if (source.CardId == DeadlySporebatCardId)
            {
                return 4;
            }

            return 2;
        }

        private static int GetDeathrattleRepeats(CombatSideState owner)
        {
            var extra = owner.Board
                .Where(minion => IsAlive(minion) && minion.CardId == TitusRivendareCardId)
                .Sum(minion => minion.Golden ? 2 : 1);
            return 1 + extra;
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

        private static MinionInstance AddImmediateAttackHatchling(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds = null)
        {
            var token = AddToken(context, owner, source, insertIndex, "hatchling", "Hatchling", 3, 3, Tribe.Dragon);
            if (token != null)
            {
                newEntityIds?.Add(token.InstanceId);
                context.ImmediateAttacks.Enqueue(new ImmediateAttackRequest(owner.Side, token.InstanceId));
                AddLog(context.Log, "ImmediateAttackQueued", token.InstanceId + " queued", token.InstanceId, null, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.ImmediateAttackQueued,
                    token.InstanceId + " queued",
                    owner.Side,
                    token.InstanceId,
                    owner.Side,
                    null,
                    new[] { source.InstanceId, token.InstanceId },
                    null,
                    null,
                    null,
                    new[] { source.InstanceId });
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

        private static void BuffAll(IEnumerable<MinionInstance> candidates, int attack, int health, string sourceId)
        {
            foreach (var target in candidates.Where(IsAlive).ToList())
            {
                BuffMinion(target, attack, health, sourceId);
            }
        }

        private static void DestroyLargestEnemy(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            var opponent = context.Get(owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            var target = opponent.Board
                .Where(IsAlive)
                .OrderByDescending(minion => minion.Attack + minion.MaxHealth)
                .FirstOrDefault();
            if (target == null)
            {
                return;
            }

            target.Health = 0;
            AddLog(context.Log, "DeathrattleResolved", source.InstanceId + " destroyed " + target.InstanceId, source.InstanceId, target.InstanceId, LogSeverity.Good);
        }

        private static void DestroyKiller(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            var killerId = GetKillerId(source);
            if (string.IsNullOrEmpty(killerId))
            {
                return;
            }

            var killerOwner = context.Player.Board.Any(minion => minion.InstanceId == killerId) ? context.Player : context.Opponent;
            var target = killerOwner.Board.FirstOrDefault(minion => minion.InstanceId == killerId && IsAlive(minion));
            if (target == null)
            {
                return;
            }

            target.Health = 0;
            AddLog(context.Log, "DeathrattleResolved", source.InstanceId + " destroyed killer " + target.InstanceId, source.InstanceId, target.InstanceId, LogSeverity.Good);
            if (killerOwner.Side != owner.Side)
            {
                ResolveDeaths(context, killerOwner.Side);
            }
        }

        private static void TrackDeadMech(CombatSideState owner, MinionInstance minion)
        {
            if (minion == null || !minion.Tribes.Contains(Tribe.Mech))
            {
                return;
            }

            owner.DeadMechPlainCopies.Add(CreatePlainCombatCopy(minion, "dead-mech-" + owner.DeadMechPlainCopies.Count));
        }

        private static int SummonDeadMechPlainCopies(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds, int count)
        {
            var inserted = 0;
            foreach (var deadMech in owner.DeadMechPlainCopies.Take(count).ToList())
            {
                if (owner.Board.Count >= BoardLimit)
                {
                    RecordSummonOverflow(context, owner, source, deadMech.CardId, deadMech.Name);
                    continue;
                }

                var copy = CreatePlainCombatCopy(deadMech, "kangor-" + source.InstanceId + "-" + inserted);
                copy.Owner = owner.Side;
                ApplySummonAuras(owner, copy);
                owner.Board.Insert(Math.Min(insertIndex + inserted, owner.Board.Count), copy);
                ResolveFriendlySummonTriggers(context, owner, copy, source);
                newEntityIds?.Add(copy.InstanceId);
                inserted += 1;
                AddLog(context.Log, "MinionSummoned", source.InstanceId + " rebuilt " + copy.InstanceId, source.InstanceId, copy.InstanceId, LogSeverity.Good);
            }

            return inserted;
        }

        private static MinionInstance CreatePlainCombatCopy(MinionInstance source, string instanceId)
        {
            var attack = source.BaseAttack > 0 ? source.BaseAttack : source.Attack;
            var health = source.BaseHealth > 0 ? source.BaseHealth : source.MaxHealth;
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = source.DefinitionId,
                CardId = source.CardId,
                Name = source.Name,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = source.TavernTier,
                Golden = source.Golden,
                CanAttack = true,
                Owner = source.Owner,
                Tribes = source.Tribes.ToList(),
                Keywords = source.Keywords.Where(keyword => keyword != Keyword.Reborn).ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>(),
                PoolSource = PoolSource.Summon,
                PoolCopiesHeld = 0
            };
        }

        private static void MarkKilledBy(MinionInstance target, string killerId)
        {
            if (target == null || string.IsNullOrEmpty(killerId))
            {
                return;
            }

            if (target.Tags == null)
            {
                target.Tags = new List<string>();
            }

            target.Tags.RemoveAll(tag => tag.StartsWith("killed_by:", StringComparison.Ordinal));
            target.Tags.Add("killed_by:" + killerId);
        }

        private static string GetKillerId(MinionInstance target)
        {
            var tag = target?.Tags?.FirstOrDefault(value => value.StartsWith("killed_by:", StringComparison.Ordinal));
            return string.IsNullOrEmpty(tag) ? null : tag.Substring("killed_by:".Length);
        }

        private static int AddBloodGemToken(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds = null)
        {
            var token = AddToken(context, owner, source, insertIndex, "quilboar", "Quilboar", 1, 1, Tribe.Quilboar, Keyword.Taunt);
            ApplyBloodGem(token, owner.Tavern);
            if (token != null)
            {
                newEntityIds?.Add(token.InstanceId);
            }

            return token == null ? 0 : 1;
        }

        private static int AddTokenAndTrack(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds, string tokenId, string name, int attack, int health, Tribe tribe, Keyword? keyword = null)
        {
            var token = AddToken(context, owner, source, insertIndex, tokenId, name, attack, health, tribe, keyword);
            if (token == null)
            {
                return 0;
            }

            newEntityIds?.Add(token.InstanceId);
            return 1;
        }

        private static void SummonHighestAttackMurlocFromHand(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            if (owner.Board.Count >= BoardLimit)
            {
                return;
            }

            var candidate = owner.Hand
                .Where(card => card.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Murloc))
                .OrderByDescending(card => card.Attack)
                .ThenBy(card => card.InstanceId)
                .FirstOrDefault();
            if (candidate == null)
            {
                return;
            }

            var copy = candidate.Clone();
            copy.InstanceId = "combat-snapjaw-" + source.InstanceId + "-" + copy.InstanceId;
            copy.Owner = owner.Side;
            copy.PoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            ApplySummonAuras(owner, copy);
            owner.Board.Add(copy);
            ResolveFriendlySummonTriggers(context, owner, copy, source);
            AddLog(context.Log, "MinionSummoned", source.InstanceId + " summoned hand Murloc " + copy.InstanceId, source.InstanceId, copy.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                source.InstanceId + " summoned hand Murloc " + copy.InstanceId,
                owner.Side,
                source.InstanceId,
                owner.Side,
                copy.InstanceId,
                new[] { source.InstanceId, copy.InstanceId },
                null,
                null,
                new[] { copy.InstanceId },
                new[] { source.InstanceId });
        }

        private static int SummonLeftmostHandMinionForCombat(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds)
        {
            if (owner.Board.Count >= BoardLimit)
            {
                return 0;
            }

            var candidate = owner.Hand.FirstOrDefault(card => card.CardKind == CardKind.Minion);
            if (candidate == null)
            {
                return 0;
            }

            var copy = candidate.Clone();
            copy.InstanceId = "combat-hand-" + source.InstanceId + "-" + candidate.InstanceId;
            copy.Owner = owner.Side;
            copy.PoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            ApplySummonAuras(owner, copy);
            owner.Board.Insert(Math.Min(insertIndex, owner.Board.Count), copy);
            newEntityIds.Add(copy.InstanceId);
            ResolveFriendlySummonTriggers(context, owner, copy, source);
            AddLog(context.Log, "MinionSummoned", source.InstanceId + " summoned hand minion " + copy.InstanceId, source.InstanceId, copy.InstanceId, LogSeverity.Good);
            return 1;
        }

        private static MinionInstance AddToken(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, string tokenId, string name, int attack, int health, Tribe tribe, Keyword? keyword = null)
        {
            if (owner.Board.Count >= BoardLimit)
            {
                foreach (var crasher in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == CatacombCrasherCardId))
                {
                    BuffAll(owner.Board, crasher.Golden ? 4 : 2, crasher.Golden ? 4 : 2, "Catacomb Crasher");
                }

                RecordSummonOverflow(context, owner, source, tokenId, name);
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
            ResolveFriendlySummonTriggers(context, owner, token, source);
            AddLog(context.Log, "MinionSummoned", source.InstanceId + " summoned " + name, source.InstanceId, token.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                source.InstanceId + " summoned " + name,
                owner.Side,
                source.InstanceId,
                owner.Side,
                token.InstanceId,
                new[] { source.InstanceId, token.InstanceId },
                null,
                null,
                new[] { token.InstanceId },
                new[] { source.InstanceId });
            return token;
        }

        private static void RecordSummonOverflow(CombatContext context, CombatSideState owner, MinionInstance source, string tokenId, string name)
        {
            var overflowId = "overflow-" + source.InstanceId + "-" + tokenId + "-" + context.Replay.Frames.Count;
            AddLog(context.Log, "SummonOverflowed", source.InstanceId + " overflowed " + name, source.InstanceId, tokenId, LogSeverity.Warning);
            RecordFrame(
                context,
                CombatEventType.SummonOverflowed,
                source.InstanceId + " overflowed " + name,
                owner.Side,
                source.InstanceId,
                owner.Side,
                tokenId,
                new[] { source.InstanceId },
                null,
                null,
                null,
                new[] { source.InstanceId },
                new[] { overflowId },
                owner.Side,
                -1,
                1,
                0);
        }

        private static void ResolveFriendlySummonTriggers(CombatContext context, CombatSideState owner, MinionInstance summoned, MinionInstance source)
        {
            if (summoned == null)
            {
                return;
            }

            if (summoned.Tribes.Contains(Tribe.Beast))
            {
                foreach (var slamma in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == BananaSlammaCardId))
                {
                    BuffMinion(summoned, summoned.Attack * (slamma.Golden ? 2 : 1), 0, "Banana Slamma");
                }

                foreach (var rider in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == MoonRiderCardId))
                {
                    rider.Counters.TryGetValue("beast_summon_attack", out var bonus);
                    bonus += rider.Golden ? 4 : 2;
                    rider.Counters["beast_summon_attack"] = bonus;
                    BuffMinion(summoned, bonus, 0, "Moon-Rider");
                }
            }

            if (!summoned.Tribes.Contains(Tribe.Mech))
            {
                return;
            }

            foreach (var deflecto in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == DeflectOBotCardId))
            {
                BuffMinion(deflecto, deflecto.Golden ? 4 : 2, 0, "Deflect-o-Bot");
                if (!deflecto.Keywords.Contains(Keyword.DivineShield))
                {
                    deflecto.Keywords.Add(Keyword.DivineShield);
                }

                RecordFrame(
                    context,
                    CombatEventType.AttackTriggered,
                    deflecto.InstanceId + " reacted to Mech summon",
                    owner.Side,
                    deflecto.InstanceId,
                    owner.Side,
                    summoned.InstanceId,
                    new[] { deflecto.InstanceId, summoned.InstanceId, source?.InstanceId },
                    null,
                    null,
                    null,
                    new[] { deflecto.InstanceId });
            }
        }

        private static void RecordRebornOverflow(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            var overflowId = "reborn-overflow-" + source.InstanceId + "-" + context.Replay.Frames.Count;
            AddLog(context.Log, "RebornOverflowed", source.InstanceId + " reborn overflowed", source.InstanceId, null, LogSeverity.Warning);
            RecordFrame(
                context,
                CombatEventType.RebornOverflowed,
                source.InstanceId + " reborn overflowed",
                owner.Side,
                source.InstanceId,
                owner.Side,
                source.InstanceId,
                new[] { source.InstanceId },
                null,
                null,
                null,
                new[] { source.InstanceId },
                new[] { overflowId },
                owner.Side,
                -1,
                0,
                1);
        }

        private static void RetargetAttackPointerToNewUnits(CombatContext context, CombatSideState owner, List<string> newEntityIds, List<string> sourceIds)
        {
            if (newEntityIds == null || newEntityIds.Count == 0)
            {
                return;
            }

            var candidates = newEntityIds
                .Select(id => new { Id = id, Index = owner.Board.FindIndex(minion => minion.InstanceId == id && IsAlive(minion) && minion.CanAttack) })
                .Where(candidate => candidate.Index >= 0)
                .OrderBy(candidate => candidate.Index)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var target = candidates[0];
            owner.PendingAttackIndexOverride = target.Index;
            AddLog(context.Log, "AttackPointerRetargeted", owner.Side + " attack pointer -> " + target.Id + " @" + target.Index, target.Id, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.AttackPointerRetargeted,
                owner.Side + " attack pointer -> " + target.Id + " @" + target.Index,
                owner.Side,
                target.Id,
                owner.Side,
                target.Id,
                candidates.Select(candidate => candidate.Id).Concat(sourceIds ?? Enumerable.Empty<string>()),
                null,
                null,
                null,
                candidates.Select(candidate => candidate.Id),
                null,
                owner.Side,
                target.Index,
                0,
                0);
        }

        private static void ApplyBloodGem(MinionInstance target, TavernState tavern = null)
        {
            if (target == null)
            {
                return;
            }

            BuffMinion(target, 1 + (tavern?.BloodGemBonusAttack ?? 0), 1 + (tavern?.BloodGemBonusHealth ?? 0), "Blood Gem");
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

        private static void RecordFrame(
            CombatContext context,
            CombatEventType eventType,
            string logText,
            BoardSide actorSide = BoardSide.Player,
            string actorId = null,
            BoardSide targetSide = BoardSide.Opponent,
            string targetId = null,
            IEnumerable<string> relatedEntityIds = null,
            IEnumerable<string> damagedEntityIds = null,
            IEnumerable<string> deadEntityIds = null,
            IEnumerable<string> summonedEntityIds = null,
            IEnumerable<string> triggerSourceIds = null,
            IEnumerable<string> overflowedEntityIds = null,
            BoardSide attackPointerSide = BoardSide.Player,
            int attackPointerIndex = -1,
            int summonOverflowCount = 0,
            int rebornOverflowCount = 0,
            int actualDamageCount = 0,
            int divineShieldBreakCount = 0,
            bool triggeredAttack = false,
            int mechanicCounter = 0,
            int mechanicThreshold = 0)
        {
            context.Replay.Frames.Add(new CombatFrame
            {
                Index = context.Replay.Frames.Count,
                EventType = eventType,
                ActorSide = actorSide,
                ActorId = actorId,
                TargetSide = targetSide,
                TargetId = targetId,
                PlayerBoardSnapshot = CreateBoardSnapshot(BoardSide.Player, context.Player.Board),
                OpponentBoardSnapshot = CreateBoardSnapshot(BoardSide.Opponent, context.Opponent.Board),
                LogText = logText,
                RelatedEntityIds = DistinctIds(relatedEntityIds),
                DamagedEntityIds = DistinctIds(damagedEntityIds),
                DeadEntityIds = DistinctIds(deadEntityIds),
                SummonedEntityIds = DistinctIds(summonedEntityIds),
                TriggerSourceIds = DistinctIds(triggerSourceIds),
                OverflowedEntityIds = DistinctIds(overflowedEntityIds),
                AttackPointerSide = attackPointerSide,
                AttackPointerIndex = attackPointerIndex,
                SummonOverflowCount = summonOverflowCount,
                RebornOverflowCount = rebornOverflowCount,
                ActualDamageCount = actualDamageCount,
                DivineShieldBreakCount = divineShieldBreakCount,
                TriggeredAttack = triggeredAttack,
                MechanicCounter = mechanicCounter,
                MechanicThreshold = mechanicThreshold
            });
        }

        private static CombatBoardPairSnapshot CreateBoardPairSnapshot(CombatContext context)
        {
            return new CombatBoardPairSnapshot
            {
                Player = CreateBoardSnapshot(BoardSide.Player, context.Player.Board),
                Opponent = CreateBoardSnapshot(BoardSide.Opponent, context.Opponent.Board)
            };
        }

        private static CombatBoardSnapshot CreateBoardSnapshot(BoardSide side, IList<MinionInstance> board)
        {
            var snapshot = new CombatBoardSnapshot { Side = side };
            for (var index = 0; index < board.Count; index += 1)
            {
                var minion = board[index];
                if (minion == null)
                {
                    continue;
                }

                snapshot.Minions.Add(new CombatMinionSnapshot
                {
                    Position = index,
                    InstanceId = minion.InstanceId,
                    CardId = minion.CardId,
                    Name = minion.Name,
                    Attack = minion.Attack,
                    Health = minion.Health,
                    MaxHealth = minion.MaxHealth,
                    BaseAttack = minion.BaseAttack,
                    BaseHealth = minion.BaseHealth,
                    TavernTier = minion.TavernTier,
                    Golden = minion.Golden,
                    CanAttack = minion.CanAttack,
                    AttacksThisCombat = minion.AttacksThisCombat,
                    Keywords = minion.Keywords == null ? new List<Keyword>() : new List<Keyword>(minion.Keywords),
                    Tribes = minion.Tribes == null ? new List<Tribe>() : new List<Tribe>(minion.Tribes),
                    EnchantmentSourceIds = minion.Enchantments == null
                        ? new List<string>()
                        : minion.Enchantments.Where(enchantment => enchantment != null && !string.IsNullOrEmpty(enchantment.SourceId)).Select(enchantment => enchantment.SourceId).Distinct().ToList(),
                    Tags = minion.Tags == null ? new List<string>() : new List<string>(minion.Tags)
                });
            }

            return snapshot;
        }

        private static List<string> DistinctIds(IEnumerable<string> ids)
        {
            return ids == null
                ? new List<string>()
                : ids.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        }

        private static List<CombatReward> CloneRewards(IEnumerable<CombatReward> rewards)
        {
            return rewards == null
                ? new List<CombatReward>()
                : rewards.Select(reward => new CombatReward
                {
                    Type = reward.Type,
                    Side = reward.Side,
                    SourceCardId = reward.SourceCardId,
                    CardId = reward.CardId,
                    Amount = reward.Amount,
                    Attack = reward.Attack,
                    Health = reward.Health
                }).ToList();
        }

        private static void AdvanceNaturalAttackPointers(CombatContext context, BoardSide attackerSide, int attackerIndex)
        {
            var attackers = context.Get(attackerSide);
            var defenders = context.Get(attackerSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);

            if (!ApplyPendingAttackPointer(attackers))
            {
                attackers.AttackIndex = NormalizeAttackIndex(attackers.Board, attackerIndex + 1);
            }

            if (!ApplyPendingAttackPointer(defenders))
            {
                defenders.AttackIndex = NormalizeAttackIndex(defenders.Board, defenders.AttackIndex);
            }
        }

        private static bool ApplyPendingAttackPointer(CombatSideState side)
        {
            if (!side.PendingAttackIndexOverride.HasValue)
            {
                return false;
            }

            side.AttackIndex = NormalizeAttackIndex(side.Board, side.PendingAttackIndexOverride.Value);
            side.PendingAttackIndexOverride = null;
            return true;
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
                return new DamageResult(next, false, true);
            }

            next.Health = poison ? 0 : next.Health - amount;
            return new DamageResult(next, true, false);
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
                Replay = new CombatReplay { Seed = seed };
            }

            public CombatSideState Player { get; }
            public CombatSideState Opponent { get; }
            public int Seed { get; }
            public int AttackSequence { get; set; }
            public List<CombatLogEntry> Log { get; } = new List<CombatLogEntry>();
            public CombatReplay Replay { get; }
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
            public int? PendingAttackIndexOverride { get; set; }
            public int BeastAttackAura { get; set; }
            public int TemporaryAvengeBeastRewards { get; set; }
            public Dictionary<string, int> AvengeCounters { get; } = new Dictionary<string, int>();
            public List<MinionInstance> DeadMechPlainCopies { get; } = new List<MinionInstance>();
            public Dictionary<string, MinionInstance> StitchedCopies { get; } = new Dictionary<string, MinionInstance>();
            public Dictionary<string, int> SummonAuraUses { get; } = new Dictionary<string, int>();
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
            public DamageResult(MinionInstance minion, bool combatDamageDealt, bool divineShieldBroken = false)
            {
                Minion = minion;
                CombatDamageDealt = combatDamageDealt;
                DivineShieldBroken = divineShieldBroken;
            }

            public MinionInstance Minion { get; }
            public bool CombatDamageDealt { get; }
            public bool DivineShieldBroken { get; }
        }
    }
}
