using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class RobustnessEdgeTests
    {
        private const int BoardLimit = 7;
        private const int HandLimit = 10;
        private static readonly int[] AttackEdges =
        {
            0,
            1,
            2,
            7,
            31,
            1024,
            int.MaxValue / 2,
            int.MaxValue - 3,
            int.MaxValue
        };

        private static readonly int[] HealthEdges =
        {
            1,
            2,
            7,
            4096,
            int.MaxValue / 2,
            int.MaxValue - 3,
            int.MaxValue
        };

        [Test]
        [Category("Stress")]
        public void StatMathBoundaryCases_ClampWithoutWrapping()
        {
            var target = Card("stat-edge", BoardSide.Player, "STAT_EDGE", int.MaxValue - 1, int.MaxValue - 1, Tribe.Mech, 6);

            StatMath.ApplyStatDelta(target, 200, 200);
            Assert.AreEqual(int.MaxValue, target.Attack);
            Assert.AreEqual(int.MaxValue, target.MaxHealth);
            Assert.AreEqual(int.MaxValue, target.Health);

            StatMath.ApplyStatDeltaPreservingDamage(target, int.MinValue, int.MinValue);
            Assert.AreEqual(0, target.Attack);
            Assert.AreEqual(1, target.MaxHealth);
            Assert.AreEqual(1, target.Health);

            target.Attack = int.MaxValue - 10;
            target.MaxHealth = int.MaxValue - 10;
            target.Health = int.MaxValue - 20;
            StatMath.DoubleCurrentStats(target, false);
            Assert.AreEqual(int.MaxValue, target.Attack);
            Assert.AreEqual(int.MaxValue, target.MaxHealth);
            Assert.AreEqual(int.MaxValue, target.Health);

            Assert.AreEqual(int.MaxValue, StatMath.SaturatingSum(new[] { int.MaxValue, 1 }, 0, int.MaxValue));
            Assert.AreEqual(int.MinValue, StatMath.DamageHealth(int.MinValue + 4, int.MaxValue));
        }

        [Test]
        [Category("Stress")]
        public void TripleEngineNearStatCap_SaturatesGoldenStats()
        {
            var materials = new List<MinionInstance>
            {
                Card("triple-a", BoardSide.Player, "TRIPLE_EDGE", int.MaxValue - 1, int.MaxValue - 1, Tribe.Elemental, 6),
                Card("triple-b", BoardSide.Player, "TRIPLE_EDGE", int.MaxValue - 2, int.MaxValue - 2, Tribe.Elemental, 6),
                Card("triple-c", BoardSide.Player, "TRIPLE_EDGE", int.MaxValue - 3, int.MaxValue - 3, Tribe.Elemental, 6)
            };
            foreach (var material in materials)
            {
                material.DefinitionId = "TRIPLE_EDGE_DEF";
            }

            var result = TripleEngine.ResolveTriple(materials, "TRIPLE_EDGE_DEF", BoardSide.Player, "stat-cap");

            Assert.AreEqual(int.MaxValue, result.Golden.Attack);
            Assert.AreEqual(int.MaxValue, result.Golden.MaxHealth);
            Assert.AreEqual(int.MaxValue, result.Golden.Health);
            Assert.AreEqual(0, result.Remaining.Count);
        }

        [Test]
        [Category("Stress")]
        public void RandomizedExtremeCombatAcrossManySeeds_MaintainsBounds()
        {
            const int safetyLimit = 96;

            for (var seedIndex = 0; seedIndex < 512; seedIndex += 1)
            {
                var seed = 910000 + seedIndex;
                var result = RunExtremeCombatScenario(seed, safetyLimit);
                Assert.LessOrEqual(result.Steps, safetyLimit, "seed " + seed + " steps");
                AssertCombatBoard(result.FinalPlayerBoard, "seed " + seed + " player");
                AssertCombatBoard(result.FinalOpponentBoard, "seed " + seed + " opponent");
                Assert.IsNotNull(result.Replay, "seed " + seed + " replay");
                Assert.LessOrEqual(result.Replay.Frames.Count, safetyLimit * 8 + 64, "seed " + seed + " replay frames");
            }
        }

        [Test]
        [Category("Stress")]
        public void RecruitAndStatMutationAcrossManySeeds_MaintainsBounds()
        {
            for (var seedIndex = 0; seedIndex < 96; seedIndex += 1)
            {
                RunRecruitScenario(920000 + seedIndex, 10);
            }
        }

        [Test]
        [Explicit("Runs for at least 30 minutes; invoke by exact test filter when doing robustness soak validation.")]
        [Category("Stress")]
        [Category("Marathon")]
        [Timeout(35 * 60 * 1000)]
        public void ThirtyMinuteExtremeCombatAndRecruitSoak_MaintainsBounds()
        {
            var stopwatch = Stopwatch.StartNew();
            var duration = TimeSpan.FromMinutes(30);
            var nextProgress = TimeSpan.FromSeconds(30);
            var iterations = 0;
            var combatRuns = 0;
            var recruitRuns = 0;

            while (stopwatch.Elapsed < duration)
            {
                var seed = 930000 + iterations;
                RunExtremeCombatScenario(seed, 128);
                combatRuns += 1;

                if (iterations % 8 == 0)
                {
                    RunRecruitScenario(seed, 6);
                    recruitRuns += 1;
                }

                if (iterations % 13 == 0)
                {
                    RunStatPipelineScenario(seed);
                }

                iterations += 1;
                if (stopwatch.Elapsed >= nextProgress)
                {
                    TestContext.Progress.WriteLine(
                        "marathon elapsed=" + (int)stopwatch.Elapsed.TotalSeconds +
                        "s combatRuns=" + combatRuns +
                        " recruitRuns=" + recruitRuns);
                    nextProgress = stopwatch.Elapsed + TimeSpan.FromSeconds(30);
                }
            }

            Assert.GreaterOrEqual(stopwatch.Elapsed, duration);
            TestContext.Progress.WriteLine(
                "marathon complete elapsed=" + (int)stopwatch.Elapsed.TotalSeconds +
                "s combatRuns=" + combatRuns +
                " recruitRuns=" + recruitRuns);
        }

        private static CombatOutput RunExtremeCombatScenario(int seed, int safetyLimit)
        {
            var result = CombatEngine.SimulateBasicCombat(
                CreateExtremeBoard(seed, BoardSide.Player),
                CreateExtremeBoard(seed + 997, BoardSide.Opponent),
                seed,
                safetyLimit,
                CreateExtremeTavern(seed),
                CreateExtremeTavern(seed + 31),
                CreateExtremeHand(seed, BoardSide.Player),
                CreateExtremeHand(seed + 17, BoardSide.Opponent));

            Assert.LessOrEqual(result.Steps, safetyLimit, "combat seed " + seed + " steps");
            AssertCombatBoard(result.FinalPlayerBoard, "combat seed " + seed + " player");
            AssertCombatBoard(result.FinalOpponentBoard, "combat seed " + seed + " opponent");
            return result;
        }

        private static void RunRecruitScenario(int seed, int turns)
        {
            var service = MatchService.CreateWithDefaultCatalog(
                seed,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { EnableTimewarpedTavern = false });
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Tier = TavernRules.MaxTavernTier;
            service.State.Player.Tavern.Gold = 30;
            service.State.Player.Tavern.MaxGold = 30;
            service.State.Player.Board.Add(Card("recruit-edge-target-" + seed, BoardSide.Player, "RECRUIT_EDGE", int.MaxValue - 11, int.MaxValue - 11, Tribe.All, 6));

            for (var turn = 0; turn < turns; turn += 1)
            {
                var target = service.State.Player.Board.FirstOrDefault(minion => minion.CardId == "RECRUIT_EDGE");
                if (target == null)
                {
                    target = Card("recruit-edge-target-" + seed + "-" + turn, BoardSide.Player, "RECRUIT_EDGE", int.MaxValue - 11, int.MaxValue - 11, Tribe.All, 6);
                    if (service.State.Player.Board.Count >= BoardLimit)
                    {
                        service.State.Player.Board[0] = target;
                    }
                    else
                    {
                        service.State.Player.Board.Add(target);
                    }
                }

                MechanicEngine.ApplyToMinion(target, new MechanicAction
                {
                    Type = MechanicActionType.BuffStats,
                    Attack = turn % 2 == 0 ? int.MaxValue : int.MaxValue / 2,
                    Health = turn % 3 == 0 ? int.MaxValue : int.MaxValue / 2,
                    SourceId = "recruit-edge-" + turn
                });

                service.Apply(new GameCommand(GameCommandType.DebugAddGold, 30));
                service.Apply(new GameCommand(GameCommandType.RerollShop));
                BuyFirstAvailableCard(service);
                ResolveRequiredChoices(service);
                AssertStateWithinLimits(service.State, "seed " + seed + " turn " + turn);

                service.Apply(new GameCommand(GameCommandType.NextTurn));
                ResolveRequiredChoices(service);
                AssertStateWithinLimits(service.State, "seed " + seed + " turn " + turn + " next");
            }
        }

        private static void RunStatPipelineScenario(int seed)
        {
            var target = Card("pipeline-target-" + seed, BoardSide.Player, "PIPELINE_EDGE", int.MaxValue - 8, int.MaxValue - 8, Tribe.Naga, 6);
            MechanicEngine.ApplyToMinion(target, new MechanicAction
            {
                Type = MechanicActionType.BuffStats,
                Attack = int.MaxValue,
                Health = int.MaxValue,
                SourceId = "pipeline-mechanic"
            });
            StatMath.DoubleCurrentStats(target, false);
            StatMath.ApplyStatDeltaPreservingDamage(target, int.MinValue, -17);
            StatMath.ApplyStatDelta(target, int.MaxValue, int.MaxValue);

            Assert.GreaterOrEqual(target.Attack, 0);
            Assert.GreaterOrEqual(target.MaxHealth, 1);
            Assert.LessOrEqual(target.Health, target.MaxHealth);
        }

        private static List<MinionInstance> CreateExtremeBoard(int seed, BoardSide side)
        {
            var rng = new SeededRng(seed);
            var count = 1 + rng.NextInt(BoardLimit);
            var board = new List<MinionInstance>();
            for (var index = 0; index < count; index += 1)
            {
                var attack = AttackEdges[rng.NextInt(AttackEdges.Length)];
                var health = HealthEdges[rng.NextInt(HealthEdges.Length)];
                var minion = Card(
                    side.ToString().ToLowerInvariant() + "-edge-" + seed + "-" + index,
                    side,
                    "EDGE_" + index,
                    attack,
                    health,
                    PickTribe(rng),
                    1 + rng.NextInt(TavernRules.MaxTavernTier));

                if (rng.NextInt(3) == 0)
                {
                    minion.Keywords.Add(Keyword.Taunt);
                }

                if (rng.NextInt(5) == 0)
                {
                    minion.Keywords.Add(Keyword.DivineShield);
                }

                if (rng.NextInt(8) == 0)
                {
                    minion.Keywords.Add(Keyword.Windfury);
                }

                if (rng.NextInt(11) == 0)
                {
                    minion.Keywords.Add(Keyword.Venomous);
                }

                board.Add(minion);
            }

            return board;
        }

        private static List<MinionInstance> CreateExtremeHand(int seed, BoardSide side)
        {
            var rng = new SeededRng(seed);
            var hand = new List<MinionInstance>();
            for (var index = 0; index < HandLimit; index += 1)
            {
                hand.Add(Card(
                    side.ToString().ToLowerInvariant() + "-hand-edge-" + seed + "-" + index,
                    side,
                    "HAND_EDGE_" + index,
                    AttackEdges[rng.NextInt(AttackEdges.Length)],
                    HealthEdges[rng.NextInt(HealthEdges.Length)],
                    PickTribe(rng),
                    1 + rng.NextInt(TavernRules.MaxTavernTier)));
            }

            return hand;
        }

        private static TavernState CreateExtremeTavern(int seed)
        {
            var rng = new SeededRng(seed);
            return new TavernState
            {
                BloodGemBonusAttack = AttackEdges[rng.NextInt(AttackEdges.Length)],
                BloodGemBonusHealth = HealthEdges[rng.NextInt(HealthEdges.Length)],
                BeetleAttackBonus = AttackEdges[rng.NextInt(AttackEdges.Length)],
                BeetleHealthBonus = HealthEdges[rng.NextInt(HealthEdges.Length)],
                NextCombatBoardAttack = AttackEdges[rng.NextInt(AttackEdges.Length)],
                NextCombatBoardHealth = HealthEdges[rng.NextInt(HealthEdges.Length)],
                TavernSpellBonusAttack = AttackEdges[rng.NextInt(AttackEdges.Length)],
                TavernSpellBonusHealth = HealthEdges[rng.NextInt(HealthEdges.Length)]
            };
        }

        private static Tribe PickTribe(SeededRng rng)
        {
            var tribes = new[]
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
                Tribe.Naga,
                Tribe.All
            };

            return tribes[rng.NextInt(tribes.Length)];
        }

        private static void BuyFirstAvailableCard(MatchService service)
        {
            if (service.State.Player.Tavern.Hand.Count >= HandLimit)
            {
                service.State.Player.Tavern.Hand.RemoveAt(service.State.Player.Tavern.Hand.Count - 1);
            }

            for (var index = 0; index < service.State.Player.Tavern.Shop.Count; index += 1)
            {
                var card = service.State.Player.Tavern.Shop[index];
                if (card == null)
                {
                    continue;
                }

                service.Apply(new GameCommand(GameCommandType.DebugAddGold, 30));
                service.Apply(new GameCommand(GameCommandType.BuyMinion, index));
                return;
            }
        }

        private static void ResolveRequiredChoices(MatchService service)
        {
            for (var guard = 0; guard < 32; guard += 1)
            {
                var tavern = service.State.Player.Tavern;
                if (tavern.Discover != null)
                {
                    if (tavern.Hand.Count >= HandLimit)
                    {
                        tavern.Hand.RemoveAt(tavern.Hand.Count - 1);
                    }

                    service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
                    continue;
                }

                if (tavern.AdvancedMechanics?.PendingChoice != null)
                {
                    service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
                    continue;
                }

                break;
            }

            Assert.IsNull(service.State.Player.Tavern.Discover, "discover chain should resolve within guard");
            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics?.PendingChoice, "advanced choice chain should resolve within guard");
        }

        private static void AssertStateWithinLimits(MatchState state, string context)
        {
            Assert.IsNotNull(state, context + " state");
            Assert.IsNotNull(state.Player, context + " player");
            Assert.IsNotNull(state.Player.Tavern, context + " tavern");
            Assert.LessOrEqual(state.Player.Board.Count, BoardLimit, context + " board size");
            Assert.LessOrEqual(state.Player.Tavern.Hand.Count, HandLimit, context + " hand size");
            Assert.LessOrEqual(state.Opponent.Board.Count, BoardLimit, context + " opponent board size");

            foreach (var minion in state.Player.Board.Concat(state.Opponent.Board).Concat(state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion)))
            {
                AssertMinionStats(minion, context + " " + minion.InstanceId, false);
            }
        }

        private static void AssertCombatBoard(IReadOnlyCollection<MinionInstance> board, string context)
        {
            Assert.IsNotNull(board, context);
            Assert.LessOrEqual(board.Count, BoardLimit, context + " board size");
            Assert.IsFalse(board.Any(card => card == null), context + " null card");
            foreach (var minion in board)
            {
                AssertMinionStats(minion, context + " " + minion.InstanceId, true);
            }
        }

        private static void AssertMinionStats(MinionInstance minion, string context, bool requireAlive)
        {
            Assert.IsNotNull(minion, context + " minion");
            Assert.GreaterOrEqual(minion.Attack, 0, context + " attack");
            Assert.GreaterOrEqual(minion.MaxHealth, 1, context + " max health");
            Assert.LessOrEqual(minion.Health, minion.MaxHealth, context + " health over max");
            if (requireAlive)
            {
                Assert.Greater(minion.Health, 0, context + " health");
            }
        }

        private static MinionInstance Card(string id, BoardSide owner, string cardId, int attack, int health, Tribe tribe, int tavernTier, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = id,
                DefinitionId = cardId,
                CardId = cardId,
                Name = id,
                Attack = attack,
                BaseAttack = attack,
                Health = health,
                MaxHealth = health,
                BaseHealth = health,
                TavernTier = tavernTier,
                Owner = owner,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>(),
                CanAttack = true
            };
        }
    }
}
