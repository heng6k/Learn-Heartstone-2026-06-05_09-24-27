using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StressTests
    {
        private const int BoardLimit = 7;
        private const int HandLimit = 10;
        private const string HastyExcavationCardId = "104559";
        private const string LockedTurnsCounter = "locked-turns";
        private const string StressTargetId = "stress-target";

        [Test]
        [Category("Stress")]
        public void LongRecruitSessionsAcrossSeeds_MaintainCoreStateLimits()
        {
            for (var seedIndex = 0; seedIndex < 16; seedIndex += 1)
            {
                var service = MatchService.CreateWithDefaultCatalog(870000 + seedIndex, new InMemoryTestScenarioRepository());
                EnsureStressTarget(service);

                for (var turn = 0; turn < 18; turn += 1)
                {
                    AssertStateWithinLimits(service.State, "seed " + seedIndex + " turn " + turn + " start");

                    service.Apply(new GameCommand(GameCommandType.DebugAddGold, 20));
                    if (service.State.Player.Tavern.UpgradeCost > 0 && turn % 2 == 1)
                    {
                        service.Apply(new GameCommand(GameCommandType.UpgradeTavern));
                    }

                    for (var refresh = 0; refresh < 2; refresh += 1)
                    {
                        service.Apply(new GameCommand(GameCommandType.DebugAddGold, 5));
                        service.Apply(new GameCommand(GameCommandType.RerollShop));
                        AssertStateWithinLimits(service.State, "seed " + seedIndex + " turn " + turn + " refresh " + refresh);
                    }

                    for (var buy = 0; buy < 3; buy += 1)
                    {
                        MakeHandRoom(service);
                        var buyIndex = FindBuyableShopIndex(service.State);
                        if (buyIndex >= 0)
                        {
                            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 10));
                            service.Apply(new GameCommand(GameCommandType.BuyMinion, buyIndex));
                            ResolveDiscoverChoices(service);
                        }

                        PlayCards(service, 2);
                        AssertStateWithinLimits(service.State, "seed " + seedIndex + " turn " + turn + " buy " + buy);
                    }

                    ResolveDiscoverChoices(service);
                    service.Apply(new GameCommand(GameCommandType.NextTurn));
                    ResolveDiscoverChoices(service);
                    EnsureStressTarget(service);
                    AssertStateWithinLimits(service.State, "seed " + seedIndex + " turn " + turn + " end");
                }
            }
        }

        [Test]
        [Category("Stress")]
        public void HighTriggerCombatAcrossSeeds_StaysInsideSafetyLimitAndBoardCaps()
        {
            const int safetyLimit = 300;

            for (var seedIndex = 0; seedIndex < 80; seedIndex += 1)
            {
                var tavern = new TavernState
                {
                    BeetleAttackBonus = 2 + seedIndex % 3,
                    BeetleHealthBonus = 1 + seedIndex % 2,
                    BloodGemBonusAttack = seedIndex % 2,
                    BloodGemBonusHealth = seedIndex % 3
                };

                var result = CombatEngine.SimulateBasicCombat(
                    CreateHighTriggerPlayerBoard(seedIndex),
                    CreateHighTriggerOpponentBoard(seedIndex),
                    880000 + seedIndex,
                    safetyLimit,
                    tavern);

                Assert.IsFalse(result.SafetyStopped, "combat seed " + seedIndex + " should finish before the safety limit");
                Assert.LessOrEqual(result.Steps, safetyLimit, "combat seed " + seedIndex + " exceeded safety limit");
                AssertCombatBoard(result.FinalPlayerBoard, "player final board seed " + seedIndex);
                AssertCombatBoard(result.FinalOpponentBoard, "opponent final board seed " + seedIndex);
                Assert.IsNotNull(result.Replay, "combat seed " + seedIndex + " replay");
                Assert.LessOrEqual(result.Replay.Frames.Count, safetyLimit + 2, "combat seed " + seedIndex + " replay frames");
            }
        }

        [Test]
        [Category("Stress")]
        public void ExtremeStatGainBeyondThirtyTwoBitTotal_SaturatesWithoutWrapping()
        {
            var target = StressCard("overflow-target", BoardSide.Player, "STRESS_OVERFLOW_TARGET", int.MaxValue - 2, int.MaxValue - 2, 1);
            var overflowBuff = new MechanicAction
            {
                Type = MechanicActionType.BuffStats,
                Attack = int.MaxValue,
                Health = int.MaxValue,
                SourceId = "overflow-stress"
            };

            Assert.DoesNotThrow(() =>
            {
                MechanicEngine.ApplyToMinion(target, overflowBuff);
                MechanicEngine.ApplyToMinion(target, overflowBuff);
            });

            Assert.AreEqual(int.MaxValue, target.Attack);
            Assert.AreEqual(int.MaxValue, target.MaxHealth);
            Assert.AreEqual(int.MaxValue, target.Health);
            Assert.LessOrEqual(target.Health, target.MaxHealth);
            Assert.GreaterOrEqual(target.Attack, 0);
        }

        [Test]
        [Category("Stress")]
        public void ExtremeStatCombat_DamageAtStatCapDoesNotWrap()
        {
            var result = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    Card("cap-attacker", BoardSide.Player, "STRESS_CAP_ATTACKER", int.MaxValue, int.MaxValue, Tribe.Mech, 6)
                },
                new[]
                {
                    Card("cap-defender", BoardSide.Opponent, "STRESS_CAP_DEFENDER", 1, int.MaxValue, Tribe.Demon, 6)
                },
                900001,
                10);

            Assert.IsFalse(result.SafetyStopped);
            Assert.AreEqual(1, result.Steps);
            Assert.AreEqual(1, result.FinalPlayerBoard.Count);
            Assert.AreEqual(0, result.FinalOpponentBoard.Count);
            Assert.AreEqual(int.MaxValue - 1, result.FinalPlayerBoard[0].Health);
            AssertCombatBoard(result.FinalPlayerBoard, "extreme stat cap player board");
        }

        [Test]
        [Category("Stress")]
        public void LowAttackHighHealthCombat_StopsAtSafetyLimit()
        {
            const int safetyLimit = 25;

            var result = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    Card("slow-player", BoardSide.Player, "STRESS_SLOW_PLAYER", 1, int.MaxValue, Tribe.Beast, 1)
                },
                new[]
                {
                    Card("slow-opponent", BoardSide.Opponent, "STRESS_SLOW_OPPONENT", 1, int.MaxValue, Tribe.Demon, 1)
                },
                900002,
                safetyLimit);

            Assert.IsTrue(result.SafetyStopped);
            Assert.AreEqual(safetyLimit, result.Steps);
            Assert.AreEqual(CombatWinner.Draw, result.Winner);
            Assert.AreEqual(1, result.FinalPlayerBoard.Count);
            Assert.AreEqual(1, result.FinalOpponentBoard.Count);
            AssertCombatBoard(result.FinalPlayerBoard, "slow combat player board");
            AssertCombatBoard(result.FinalOpponentBoard, "slow combat opponent board");
            Assert.Greater(result.FinalPlayerBoard[0].Health, int.MaxValue - safetyLimit - 1);
            Assert.Greater(result.FinalOpponentBoard[0].Health, int.MaxValue - safetyLimit - 1);
            Assert.LessOrEqual(result.Replay.Frames.Count, safetyLimit * 4 + 4);
        }

        [Test]
        [Category("Stress")]
        public void SoloMinionCatalog_AllInPoolTierOneToSevenMinionsCanEnterRecruitFlow()
        {
            var definitions = MinionCatalogLoader.LoadFromResources().All
                .Where(minion => minion.InPool)
                .Where(minion => minion.TavernTier >= TavernRules.MinTavernTier && minion.TavernTier <= TavernRules.MaxTavernTier)
                .Where(minion => !minion.CardId.StartsWith("BGDUO"))
                .OrderBy(minion => minion.TavernTier)
                .ThenBy(minion => minion.CardId)
                .ToList();

            Assert.Greater(definitions.Count, 200);

            for (var index = 0; index < definitions.Count; index += 1)
            {
                var definition = definitions[index];
                var service = MatchService.CreateWithDefaultCatalog(890000 + index, new InMemoryTestScenarioRepository());
                service.State.Player.Tavern.Tier = TavernRules.MaxTavernTier;
                service.State.Player.Tavern.Gold = 30;
                service.State.Player.Tavern.MaxGold = 30;
                service.State.Player.Tavern.Hand.Clear();
                service.State.Player.Tavern.Shop.Clear();
                service.State.Player.Board.Clear();
                EnsureStressTarget(service);
                SeedStressShop(service);

                Assert.DoesNotThrow(
                    () => service.Apply(new GameCommand(GameCommandType.AddCardToHand, definition.CardId, CardKind.Minion)),
                    definition.CardId + " add to hand");

                var handIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == definition.CardId);
                Assert.GreaterOrEqual(handIndex, 0, definition.CardId + " should be in hand");
                if (IsLocked(service.State.Player.Tavern.Hand[handIndex]))
                {
                    AssertStateWithinLimits(service.State, definition.CardId + " locked hand state");
                    continue;
                }

                Assert.DoesNotThrow(
                    () => service.Apply(new GameCommand(GameCommandType.PlayMinion, handIndex)),
                    definition.CardId + " " + definition.Name + " play");
                ResolveDiscoverChoices(service);
                AssertStateWithinLimits(service.State, definition.CardId + " " + definition.Name + " recruit flow");
            }
        }

        private static void PlayCards(MatchService service, int attempts)
        {
            for (var attempt = 0; attempt < attempts; attempt += 1)
            {
                ResolveDiscoverChoices(service);
                MakeBoardRoom(service);
                var handIndex = FindPlayableHandIndex(service.State);
                if (handIndex < 0)
                {
                    return;
                }

                var card = service.State.Player.Tavern.Hand[handIndex];
                var targetIndex = card.CardKind == CardKind.Minion ? -1 : 0;
                if (targetIndex == 0)
                {
                    EnsureStressTarget(service);
                }

                service.Apply(new GameCommand(GameCommandType.PlayMinion, handIndex, targetIndex));
            }
        }

        private static int FindPlayableHandIndex(MatchState state)
        {
            for (var index = 0; index < state.Player.Tavern.Hand.Count; index += 1)
            {
                var card = state.Player.Tavern.Hand[index];
                if (card == null || IsLocked(card))
                {
                    continue;
                }

                if (card.CardId == "TRIPLE_REWARD")
                {
                    return index;
                }

                if (card.CardKind != CardKind.Minion && state.Player.Board.Count > 0)
                {
                    return index;
                }
            }

            for (var index = 0; index < state.Player.Tavern.Hand.Count; index += 1)
            {
                var card = state.Player.Tavern.Hand[index];
                if (card != null && card.CardKind == CardKind.Minion && !IsLocked(card) && state.Player.Board.Count < BoardLimit)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int FindBuyableShopIndex(MatchState state)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return -1;
            }

            for (var index = 0; index < state.Player.Tavern.Shop.Count; index += 1)
            {
                var card = state.Player.Tavern.Shop[index];
                if (card != null && card.CardKind == CardKind.Minion)
                {
                    return index;
                }
            }

            for (var index = 0; index < state.Player.Tavern.Shop.Count; index += 1)
            {
                var card = state.Player.Tavern.Shop[index];
                if (card != null && card.CardKind == CardKind.TavernSpell && card.CardId != HastyExcavationCardId)
                {
                    return index;
                }
            }

            return -1;
        }

        private static void ResolveDiscoverChoices(MatchService service)
        {
            for (var guard = 0; service.State.Player.Tavern.Discover != null && guard < 8; guard += 1)
            {
                var discover = service.State.Player.Tavern.Discover;
                Assert.IsNotNull(discover.Options, "discover options");
                Assert.Greater(discover.Options.Count, 0, "discover from " + discover.Source + " should have options");
                TrimHandToRoom(service, HandLimit - 1);
                service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            }

            Assert.IsNull(service.State.Player.Tavern.Discover, "discover chain should resolve within guard");
        }

        private static void MakeHandRoom(MatchService service)
        {
            PlayCards(service, 3);
            TrimHandToRoom(service, HandLimit - 1);
        }

        private static void TrimHandToRoom(MatchService service, int maxCount)
        {
            var hand = service.State.Player.Tavern.Hand;
            while (hand.Count > maxCount)
            {
                hand.RemoveAt(hand.Count - 1);
            }
        }

        private static void MakeBoardRoom(MatchService service)
        {
            EnsureStressTarget(service);
            TrimHandToRoom(service, HandLimit - 2);
            while (service.State.Player.Board.Count >= BoardLimit)
            {
                var candidate = service.State.Player.Board.LastOrDefault(card => card.InstanceId != StressTargetId);
                if (candidate == null)
                {
                    return;
                }

                service.Apply(new GameCommand(GameCommandType.SellMinion, candidate.InstanceId));
            }
        }

        private static void EnsureStressTarget(MatchService service)
        {
            if (service.State.Player.Board.Any(card => card.InstanceId == StressTargetId))
            {
                return;
            }

            if (service.State.Player.Board.Count >= BoardLimit)
            {
                service.State.Player.Board.RemoveAt(service.State.Player.Board.Count - 1);
            }

            service.State.Player.Board.Insert(0, StressCard(StressTargetId, BoardSide.Player, "STRESS_TARGET", 3, 8, 1));
        }

        private static void SeedStressShop(MatchService service)
        {
            service.State.Player.Tavern.Shop.Add(StressCard("stress-shop-a", BoardSide.Player, "STRESS_SHOP_A", 2, 4, 2));
            service.State.Player.Tavern.Shop.Add(StressCard("stress-shop-b", BoardSide.Player, "STRESS_SHOP_B", 4, 6, 4));
            service.State.Player.Tavern.Shop.Add(StressCard("stress-shop-c", BoardSide.Player, "STRESS_SHOP_C", 6, 8, 6));
        }

        private static List<MinionInstance> CreateHighTriggerPlayerBoard(int seedIndex)
        {
            return new List<MinionInstance>
            {
                Card("p-manasaber-" + seedIndex, BoardSide.Player, "BG26_800", 0, 1, Tribe.Beast, 1, Keyword.Taunt, Keyword.Deathrattle),
                Card("p-rover-" + seedIndex, BoardSide.Player, "BG31_801", 0, 1, Tribe.Beast, 2, Keyword.Taunt, Keyword.Deathrattle),
                Card("p-glow-" + seedIndex, BoardSide.Player, "BG32_430", 1, 1, Tribe.Quilboar, 2, Keyword.Deathrattle),
                Card("p-titus-" + seedIndex, BoardSide.Player, "BG25_354", 1, 7, Tribe.None, 5),
                Card("p-charlga-" + seedIndex, BoardSide.Player, "BG26_157", 4, 12, Tribe.Quilboar, 6, Keyword.Avenge),
                Card("p-sporebat-" + seedIndex, BoardSide.Player, "BG31_835", 2, 12, Tribe.Undead, 6, Keyword.Avenge),
                Card("p-trailblazer-" + seedIndex, BoardSide.Player, "BG35_437", 2, 10, Tribe.Quilboar, 6)
            };
        }

        private static List<MinionInstance> CreateHighTriggerOpponentBoard(int seedIndex)
        {
            return new List<MinionInstance>
            {
                Card("o-killer-a-" + seedIndex, BoardSide.Opponent, "STRESS_KILLER_A", 12, 35, Tribe.Demon, 6, Keyword.Taunt),
                Card("o-killer-b-" + seedIndex, BoardSide.Opponent, "STRESS_KILLER_B", 10, 35, Tribe.Mech, 6, Keyword.Taunt),
                Card("o-killer-c-" + seedIndex, BoardSide.Opponent, "STRESS_KILLER_C", 12, 35, Tribe.Dragon, 6),
                Card("o-killer-d-" + seedIndex, BoardSide.Opponent, "STRESS_KILLER_D", 10, 35, Tribe.Elemental, 6),
                Card("o-killer-e-" + seedIndex, BoardSide.Opponent, "STRESS_KILLER_E", 12, 35, Tribe.Pirate, 6),
                Card("o-killer-f-" + seedIndex, BoardSide.Opponent, "STRESS_KILLER_F", 10, 35, Tribe.Murloc, 6),
                Card("o-killer-g-" + seedIndex, BoardSide.Opponent, "STRESS_KILLER_G", 12, 35, Tribe.Naga, 6)
            };
        }

        private static void AssertStateWithinLimits(MatchState state, string context)
        {
            Assert.IsNotNull(state, context + " state");
            Assert.IsNotNull(state.Player, context + " player");
            Assert.IsNotNull(state.Player.Tavern, context + " tavern");
            Assert.IsNotNull(state.Player.Tavern.Shop, context + " shop");
            Assert.IsNotNull(state.Player.Tavern.Hand, context + " hand");
            Assert.IsNotNull(state.Player.Board, context + " board");
            Assert.GreaterOrEqual(state.Player.Health, 0, context + " player health");
            Assert.GreaterOrEqual(state.Player.Tavern.Tier, TavernRules.MinTavernTier, context + " tavern tier lower bound");
            Assert.LessOrEqual(state.Player.Tavern.Tier, TavernRules.MaxTavernTier, context + " tavern tier upper bound");
            Assert.GreaterOrEqual(state.Player.Tavern.Gold, 0, context + " gold");
            Assert.LessOrEqual(state.Player.Tavern.Hand.Count, HandLimit, context + " hand size");
            Assert.LessOrEqual(state.Player.Board.Count, BoardLimit, context + " board size");
            Assert.LessOrEqual(state.Opponent.Board.Count, BoardLimit, context + " opponent board size");
            Assert.LessOrEqual(state.Player.Tavern.Shop.Count, 16, context + " shop size");
            Assert.IsFalse(state.Player.Tavern.Hand.Any(card => card == null), context + " hand null card");
            Assert.IsFalse(state.Player.Board.Any(card => card == null), context + " board null card");

            foreach (var minion in state.Player.Board.Concat(state.Opponent.Board).Concat(state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion)))
            {
                Assert.GreaterOrEqual(minion.Attack, 0, context + " " + minion.InstanceId + " attack");
                Assert.GreaterOrEqual(minion.MaxHealth, 1, context + " " + minion.InstanceId + " max health");
                Assert.LessOrEqual(minion.Health, minion.MaxHealth, context + " " + minion.InstanceId + " health over max");
            }
        }

        private static void AssertCombatBoard(IReadOnlyCollection<MinionInstance> board, string context)
        {
            Assert.IsNotNull(board, context);
            Assert.LessOrEqual(board.Count, BoardLimit, context + " board size");
            Assert.IsFalse(board.Any(card => card == null), context + " null card");
            foreach (var minion in board)
            {
                Assert.GreaterOrEqual(minion.Attack, 0, context + " " + minion.InstanceId + " attack");
                Assert.GreaterOrEqual(minion.MaxHealth, 1, context + " " + minion.InstanceId + " max health");
                Assert.Greater(minion.Health, 0, context + " " + minion.InstanceId + " health");
                Assert.LessOrEqual(minion.Health, minion.MaxHealth, context + " " + minion.InstanceId + " health over max");
            }
        }

        private static bool IsLocked(MinionInstance card)
        {
            return card.Counters != null && card.Counters.TryGetValue(LockedTurnsCounter, out var turns) && turns > 0;
        }

        private static MinionInstance StressCard(string id, BoardSide owner, string cardId, int attack, int health, int tavernTier)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = id,
                DefinitionId = id,
                CardId = cardId,
                Name = id,
                Attack = attack,
                BaseAttack = attack,
                Health = health,
                MaxHealth = health,
                BaseHealth = health,
                TavernTier = tavernTier,
                Owner = owner,
                Tribes = StressTribes(),
                Keywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>(),
                CanAttack = true
            };
        }

        private static MinionInstance Card(string id, BoardSide owner, string cardId, int attack, int health, Tribe tribe, int tavernTier, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = id,
                DefinitionId = id,
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

        private static List<Tribe> StressTribes()
        {
            return new List<Tribe>
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
        }
    }
}
