using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class DomainEngineTests
    {
        [Test]
        public void SeededRng_RepeatsSequenceForSameSeed()
        {
            var first = new SeededRng(12345);
            var second = new SeededRng(12345);

            Assert.AreEqual(first.NextInt(1000), second.NextInt(1000));
            Assert.AreEqual(first.NextInt(1000), second.NextInt(1000));
        }

        [Test]
        public void MinionPool_DrawsAndReleasesCopies()
        {
            var definition = TestMinion("m1", 1, 2, 3, 2);
            var pool = new MinionPool(new[] { definition });

            var drawn = pool.DrawShop(1, 1, new SeededRng(1));

            Assert.AreEqual("m1", drawn[0].Id);
            Assert.AreEqual(1, pool.Remaining("m1"));

            pool.Release("m1", 1);
            Assert.AreEqual(2, pool.Remaining("m1"));
        }

        [Test]
        public void TripleEngine_CombinesThreeCopiesIntoGolden()
        {
            var items = new List<MinionInstance>
            {
                TestInstance("a", "m1", 1),
                TestInstance("b", "m1", 1),
                TestInstance("c", "m1", 1),
                TestInstance("d", "m2", 0)
            };

            Assert.AreEqual("m1", TripleEngine.FindTripleCandidate(items));

            var result = TripleEngine.ResolveTriple(items, "m1", BoardSide.Player, "unit");

            Assert.AreEqual(1, result.Remaining.Count);
            Assert.IsTrue(result.Golden.Golden);
            Assert.AreEqual(3, result.Golden.PoolCopiesHeld);
        }

        [Test]
        public void CombatEngine_PrioritizesTauntAndRemovesDivineShield()
        {
            var attacker = TestInstance("p1", "attacker", 0);
            attacker.Attack = 2;
            attacker.Health = 2;
            var support = TestInstance("p2", "support", 0);
            var taunt = TestInstance("o1", "taunt", 0);
            taunt.Attack = 1;
            taunt.Health = 3;
            taunt.Keywords.Add(Keyword.Taunt);
            taunt.Keywords.Add(Keyword.DivineShield);
            var other = TestInstance("o2", "other", 0);
            other.Attack = 1;
            other.Health = 3;

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker, support }, new[] { other, taunt }, 42, 1);

            Assert.AreEqual(1, result.Steps);
            Assert.AreEqual("o1", result.Log.First(entry => entry.Title == "AttackResolved").TargetId);
            Assert.IsFalse(result.FinalOpponentBoard.Find(m => m.InstanceId == "o1").Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void CombatEngine_VenomousKillsDamagedTargetAndIsConsumed()
        {
            var attacker = TestInstance("p1", "venom", 0);
            attacker.Attack = 1;
            attacker.Keywords.Add(Keyword.Venomous);
            var defender = TestInstance("o1", "defender", 0);
            defender.Attack = 0;
            defender.Health = 10;
            defender.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker }, new[] { defender }, 1, 1);

            Assert.AreEqual(0, result.FinalOpponentBoard.Count);
            Assert.IsFalse(result.FinalPlayerBoard[0].Keywords.Contains(Keyword.Venomous));
        }

        [Test]
        public void CombatEngine_StealthDefendersAreSkippedWhenOtherTargetsExist()
        {
            var attacker = TestInstance("p1", "attacker", 0);
            var support = TestInstance("p2", "support", 0);
            var stealth = TestInstance("o1", "stealth", 0);
            stealth.Keywords.Add(Keyword.Stealth);
            var visible = TestInstance("o2", "visible", 0);

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker, support }, new[] { stealth, visible }, 5, 1);

            Assert.AreEqual("o2", result.Log.First(entry => entry.Title == "AttackResolved").TargetId);
        }

        [Test]
        public void CombatEngine_RotatesAttackersInsteadOfAlwaysUsingFirstMinion()
        {
            var first = TestInstance("p1", "first", 0);
            first.Attack = 1;
            first.Health = 10;
            first.MaxHealth = 10;
            var second = TestInstance("p2", "second", 0);
            second.Attack = 1;
            second.Health = 10;
            second.MaxHealth = 10;
            var defender = TestInstance("o1", "defender", 0);
            defender.Attack = 0;
            defender.Health = 10;
            defender.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { first, second }, new[] { defender }, 1, 3);
            var playerAttacks = result.Log.Where(entry => entry.Title == "AttackResolved" && entry.ActorId.StartsWith("p")).ToList();

            Assert.AreEqual("p1", playerAttacks[0].ActorId);
            Assert.AreEqual("p2", playerAttacks[1].ActorId);
        }

        [Test]
        public void CombatEngine_TierOneDeathrattlesSummonTokens()
        {
            var manasaber = TestInstance("p1", "manasaber", 0);
            manasaber.CardId = "BG26_800";
            manasaber.Attack = 0;
            manasaber.Health = 1;
            manasaber.MaxHealth = 1;
            manasaber.Keywords.Add(Keyword.Deathrattle);
            var attacker = TestInstance("o1", "attacker", 0);
            attacker.Attack = 1;
            attacker.Health = 1;

            var result = CombatEngine.SimulateBasicCombat(new[] { manasaber }, new[] { attacker }, 1, 1);

            Assert.AreEqual(2, result.FinalPlayerBoard.Count(card => card.CardId == "CUBLING"));
            Assert.IsTrue(result.FinalPlayerBoard.Where(card => card.CardId == "CUBLING").All(card => card.Keywords.Contains(Keyword.Taunt)));
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "MinionSummoned"));
        }

        [Test]
        public void CombatEngine_DeathrattleTokensUseDeadMinionsBoardPosition()
        {
            var rover = TestInstance("p-rover", "forest-rover", 0);
            rover.CardId = "BG31_801";
            rover.Attack = 0;
            rover.Health = 1;
            rover.MaxHealth = 1;
            rover.Tribes = new List<Tribe> { Tribe.Beast };
            rover.Keywords.Add(Keyword.Deathrattle);
            var right = TestInstance("p-right", "right", 0);
            right.Health = 10;
            right.MaxHealth = 10;
            var attacker = TestInstance("o1", "attacker", 0);
            attacker.Attack = 1;
            attacker.Health = 1;
            attacker.MaxHealth = 1;

            var result = CombatEngine.SimulateBasicCombat(new[] { rover, right }, new[] { attacker }, 1, 1);

            Assert.AreEqual("beetle", result.FinalPlayerBoard[0].DefinitionId);
            Assert.AreEqual("p-right", result.FinalPlayerBoard[1].InstanceId);
        }

        [Test]
        public void CombatEngine_TierTwoDeathrattlesSummonBasicTokensAndBuffUndead()
        {
            var forestRover = TestInstance("p-forest", "forest-rover", 0);
            forestRover.CardId = "BG31_801";
            forestRover.Attack = 0;
            forestRover.Health = 1;
            forestRover.MaxHealth = 1;
            forestRover.Tribes = new List<Tribe> { Tribe.Beast };
            forestRover.Keywords.Add(Keyword.Deathrattle);
            var smallAttacker = TestInstance("o-small", "small-attacker", 0);
            smallAttacker.Attack = 1;
            smallAttacker.Health = 1;
            smallAttacker.MaxHealth = 1;

            var forestResult = CombatEngine.SimulateBasicCombat(
                new[] { forestRover },
                new[] { smallAttacker },
                1,
                1,
                new TavernState { BeetleAttackBonus = 2, BeetleHealthBonus = 1 });
            var beetle = forestResult.FinalPlayerBoard.First(card => card.DefinitionId == "beetle");
            Assert.AreEqual(4, beetle.Attack);
            Assert.AreEqual(3, beetle.MaxHealth);
            Assert.IsTrue(beetle.Tribes.Contains(Tribe.Beast));

            var glowgullet = TestInstance("p-glow", "glowgullet", 0);
            glowgullet.CardId = "BG32_430";
            glowgullet.Attack = 0;
            glowgullet.Health = 1;
            glowgullet.MaxHealth = 1;
            glowgullet.Tribes = new List<Tribe> { Tribe.Quilboar };
            glowgullet.Keywords.Add(Keyword.Deathrattle);
            var glowAttacker = TestInstance("o-glow", "glow-attacker", 0);
            glowAttacker.Attack = 1;
            glowAttacker.Health = 1;
            glowAttacker.MaxHealth = 1;

            var glowResult = CombatEngine.SimulateBasicCombat(new[] { glowgullet }, new[] { glowAttacker }, 1, 1);
            var quilboars = glowResult.FinalPlayerBoard.Where(card => card.DefinitionId == "quilboar").ToList();
            Assert.AreEqual(2, quilboars.Count);
            Assert.IsTrue(quilboars.All(card => card.Attack == 2 && card.MaxHealth == 2 && card.Keywords.Contains(Keyword.Taunt)));
            Assert.IsTrue(quilboars.All(card => card.Enchantments.Any(enchantment => enchantment.SourceId == "Blood Gem")));

            var undeadTarget = TestInstance("p-undead", "undead-target", 0);
            undeadTarget.Attack = 0;
            undeadTarget.Health = 3;
            undeadTarget.MaxHealth = 3;
            undeadTarget.Tribes = new List<Tribe> { Tribe.Undead };
            undeadTarget.Keywords.Add(Keyword.Stealth);
            var scarletSkull = TestInstance("p-skull", "scarlet-skull", 0);
            scarletSkull.CardId = "BG25_022";
            scarletSkull.Attack = 0;
            scarletSkull.Health = 1;
            scarletSkull.MaxHealth = 1;
            scarletSkull.Tribes = new List<Tribe> { Tribe.Undead };
            scarletSkull.Keywords.Add(Keyword.Deathrattle);
            var skullAttacker = TestInstance("o-skull", "skull-attacker", 0);
            skullAttacker.Attack = 1;
            skullAttacker.Health = 3;
            skullAttacker.MaxHealth = 3;

            var skullResult = CombatEngine.SimulateBasicCombat(new[] { undeadTarget, scarletSkull }, new[] { skullAttacker }, 1, 2);
            var buffed = skullResult.FinalPlayerBoard.First(card => card.InstanceId == "p-undead");
            Assert.AreEqual(undeadTarget.Attack + 1, buffed.Attack);
            Assert.AreEqual(undeadTarget.MaxHealth + 2, buffed.MaxHealth);
        }

        [Test]
        public void CombatEngine_ScarletSkullBuffsUndeadBehindIt()
        {
            var scarletSkull = TestInstance("p-skull", "scarlet-skull", 0);
            scarletSkull.CardId = "BG25_022";
            scarletSkull.Attack = 0;
            scarletSkull.Health = 1;
            scarletSkull.MaxHealth = 1;
            scarletSkull.Tribes = new List<Tribe> { Tribe.Undead };
            scarletSkull.Keywords.Add(Keyword.Deathrattle);
            var undeadTarget = TestInstance("p-undead", "undead-target", 0);
            undeadTarget.Attack = 2;
            undeadTarget.Health = 4;
            undeadTarget.MaxHealth = 4;
            undeadTarget.Tribes = new List<Tribe> { Tribe.Undead };
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 1;
            opponent.Health = 10;
            opponent.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { scarletSkull, undeadTarget }, new[] { opponent }, 7, 1);
            var buffed = result.FinalPlayerBoard.First(card => card.InstanceId == "p-undead");

            Assert.AreEqual(3, buffed.Attack);
            Assert.AreEqual(6, buffed.MaxHealth);
        }

        [Test]
        public void CombatEngine_HummingBirdAuraBuffsFutureBeastSummons()
        {
            var forestRover = TestInstance("p-forest", "forest-rover", 0);
            forestRover.CardId = "BG31_801";
            forestRover.Attack = 0;
            forestRover.Health = 1;
            forestRover.MaxHealth = 1;
            forestRover.Tribes = new List<Tribe> { Tribe.Beast };
            forestRover.Keywords.Add(Keyword.Deathrattle);
            var hummingBird = TestInstance("p-humming", "humming-bird", 0);
            hummingBird.CardId = "BG26_805";
            hummingBird.Attack = 0;
            hummingBird.Health = 5;
            hummingBird.MaxHealth = 5;
            hummingBird.Tribes = new List<Tribe> { Tribe.Beast };
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 1;
            opponent.Health = 10;
            opponent.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { forestRover, hummingBird }, new[] { opponent }, 3, 1);
            var beetle = result.FinalPlayerBoard.First(card => card.DefinitionId == "beetle");

            Assert.AreEqual(3, beetle.Attack);
            Assert.IsTrue(beetle.Enchantments.Any(enchantment => enchantment.SourceId == "Humming Bird"));
        }

        [Test]
        public void CombatEngine_TwilightHatchlingSummonImmediatelyAttacks()
        {
            var hatchling = TestInstance("p-hatchling", "twilight-hatchling", 0);
            hatchling.CardId = "BG34_630";
            hatchling.Attack = 0;
            hatchling.Health = 1;
            hatchling.MaxHealth = 1;
            hatchling.Tribes = new List<Tribe> { Tribe.Dragon };
            hatchling.Keywords.Add(Keyword.Deathrattle);
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 1;
            opponent.Health = 5;
            opponent.MaxHealth = 5;

            var result = CombatEngine.SimulateBasicCombat(new[] { hatchling }, new[] { opponent }, 11, 2);

            Assert.IsTrue(result.Log.Any(entry => entry.Title == "ImmediateAttackQueued"));
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "TriggeredAttackResolved" && entry.ActorId.StartsWith("token-p-hatchling-hatchling")));
            Assert.AreEqual(2, result.FinalOpponentBoard.First().Health);
        }

        [Test]
        public void CombatEngine_WindfuryAttacksTwiceInOneTurn()
        {
            var windfury = TestInstance("p-windfury", "windfury", 0);
            windfury.Attack = 1;
            windfury.Health = 10;
            windfury.MaxHealth = 10;
            windfury.Keywords.Add(Keyword.Windfury);
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 0;
            opponent.Health = 10;
            opponent.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { windfury }, new[] { opponent }, 13, 2);
            var attacks = result.Log.Count(entry =>
                (entry.Title == "AttackResolved" || entry.Title == "TriggeredAttackResolved") &&
                entry.ActorId == "p-windfury");

            Assert.AreEqual(2, attacks);
            Assert.AreEqual(8, result.FinalOpponentBoard.First().Health);
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "WindfuryResolved"));
        }

        [Test]
        public void CombatEngine_TierTwoDeathrattlesQueueRecruitRewards()
        {
            var alert = DeathrattleRewardSource("p-alert", "BG35_340");
            var alertResult = CombatEngine.SimulateBasicCombat(new[] { alert }, new[] { LethalOpponent("o-alert") }, 21, 1);
            var discountReward = alertResult.PlayerRewards.First(reward => reward.Type == CombatRewardType.TavernSpellCostReduction);
            Assert.AreEqual(1, discountReward.Amount);

            var bully = DeathrattleRewardSource("p-bully", "BG35_432");
            var bullyResult = CombatEngine.SimulateBasicCombat(new[] { bully }, new[] { LethalOpponent("o-bully") }, 22, 1);
            Assert.AreEqual("BRISTLEBACK_BLOOD_GEM", bullyResult.PlayerRewards.First(reward => reward.Type == CombatRewardType.AddGeneratedSpellToHand).CardId);

            var hunter = DeathrattleRewardSource("p-hunter", "BG32_170");
            var hunterResult = CombatEngine.SimulateBasicCombat(new[] { hunter }, new[] { LethalOpponent("o-hunter") }, 23, 1);
            Assert.AreEqual("100596", hunterResult.PlayerRewards.First(reward => reward.Type == CombatRewardType.AddGeneratedSpellToHand).CardId);
            Assert.IsTrue(hunterResult.Log.Any(entry => entry.Title == "CombatRewardQueued"));
        }

        [Test]
        public void CombatEngine_TideRaiserCastsShiftingTideOnAdjacentMinion()
        {
            var tideRaiser = DeathrattleRewardSource("p-tide", "BG34_920");
            var adjacentNaga = TestInstance("p-naga", "naga", 0);
            adjacentNaga.Attack = 2;
            adjacentNaga.Health = 3;
            adjacentNaga.MaxHealth = 3;
            adjacentNaga.Tribes = new List<Tribe> { Tribe.Naga };
            var opponent = LethalOpponent("o-tide");

            var result = CombatEngine.SimulateBasicCombat(new[] { tideRaiser, adjacentNaga }, new[] { opponent }, 41, 1);
            var buffed = result.FinalPlayerBoard.First(card => card.InstanceId == "p-naga");

            Assert.AreEqual(6, buffed.Attack);
            Assert.AreEqual(7, buffed.MaxHealth);
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "CombatSpellCast" && entry.TargetId == "p-naga"));
        }

        [Test]
        public void CombatEngine_SleepySupporterRallyBuffsRightMinion()
        {
            var supporter = TestInstance("p-supporter", "sleepy-supporter", 0);
            supporter.CardId = "BG33_241";
            supporter.Attack = 1;
            supporter.Health = 5;
            supporter.MaxHealth = 5;
            supporter.Keywords.Add(Keyword.Rally);
            var right = TestInstance("p-right", "right", 0);
            right.Attack = 2;
            right.Health = 2;
            right.MaxHealth = 2;
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 0;
            opponent.Health = 10;
            opponent.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { supporter, right }, new[] { opponent }, 42, 1);
            var buffed = result.FinalPlayerBoard.First(card => card.InstanceId == "p-right");

            Assert.AreEqual(4, buffed.Attack);
            Assert.AreEqual(4, buffed.MaxHealth);
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "RallyResolved"));
        }

        [Test]
        public void CombatEngine_ExpertAviatorRallySummonsHighestAttackMinionFromHand()
        {
            var aviator = TestInstance("p-aviator", "expert-aviator", 0);
            aviator.CardId = "BG34_140";
            aviator.Attack = 1;
            aviator.Health = 5;
            aviator.MaxHealth = 5;
            aviator.Keywords.Add(Keyword.Rally);
            var low = TestInstance("hand-low", "low", 0);
            low.Attack = 2;
            var high = TestInstance("hand-high", "high", 0);
            high.Attack = 7;
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 0;
            opponent.Health = 10;
            opponent.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(
                new[] { aviator },
                new[] { opponent },
                43,
                1,
                null,
                null,
                new[] { low, high });

            Assert.IsTrue(result.FinalPlayerBoard.Any(card => card.InstanceId.Contains("hand-high")));
            Assert.IsFalse(result.FinalPlayerBoard.Any(card => card.InstanceId.Contains("hand-low")));
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "RallyResolved" && entry.TargetId.Contains("hand-high")));
        }

        [Test]
        public void BoardTribeAnalyzer_CountsDualTribeAndAllMinions()
        {
            var dragonMurloc = TestInstance("dual", "dual", 0);
            dragonMurloc.Tribes = new List<Tribe> { Tribe.Dragon, Tribe.Murloc };
            var all = TestInstance("all", "all", 0);
            all.Tribes = new List<Tribe> { Tribe.All };

            var distribution = BoardTribeAnalyzer.Build(new[] { dragonMurloc, all });

            Assert.AreEqual(2, distribution[Tribe.Dragon]);
            Assert.AreEqual(2, distribution[Tribe.Murloc]);
            Assert.AreEqual(1, distribution[Tribe.Beast]);
            Assert.AreEqual(10, distribution.Count);
        }

        [Test]
        public void BoardTribeAnalyzer_HasTribeExpandsAllMinions()
        {
            var all = TestInstance("all", "all", 0);
            all.Tribes = new List<Tribe> { Tribe.All };
            var none = TestInstance("none", "none", 0);

            Assert.IsTrue(BoardTribeAnalyzer.HasTribe(all, Tribe.Beast));
            Assert.IsTrue(BoardTribeAnalyzer.HasTribe(all, Tribe.Naga));
            Assert.IsTrue(BoardTribeAnalyzer.HasTribe(all, Tribe.All));
            Assert.IsFalse(BoardTribeAnalyzer.HasTribe(all, Tribe.None));
            Assert.IsFalse(BoardTribeAnalyzer.HasTribe(none, Tribe.Beast));
        }

        [Test]
        public void BoardTribeAnalyzer_SelectByTribeAndCountTribeIncludeAllMinions()
        {
            var naga = TestInstance("naga", "naga", 0);
            naga.Tribes = new List<Tribe> { Tribe.Naga };
            var demonNaga = TestInstance("demon-naga", "demon-naga", 0);
            demonNaga.Tribes = new List<Tribe> { Tribe.Demon, Tribe.Naga };
            var all = TestInstance("all", "all", 0);
            all.Tribes = new List<Tribe> { Tribe.All };
            var beast = TestInstance("beast", "beast", 0);
            beast.Tribes = new List<Tribe> { Tribe.Beast };

            var board = new[] { naga, demonNaga, all, beast };
            var nagaTargets = BoardTribeAnalyzer.SelectByTribe(board, Tribe.Naga);

            CollectionAssert.AreEqual(new[] { "naga", "demon-naga", "all" }, nagaTargets.Select(minion => minion.InstanceId).ToArray());
            Assert.AreEqual(3, BoardTribeAnalyzer.CountTribe(board, Tribe.Naga));
            Assert.AreEqual(2, BoardTribeAnalyzer.CountTribe(board, Tribe.Demon));
        }

        [Test]
        public void BoardTribeAnalyzer_MostCommonUsesNoneForEmptyBoardAndStableTieOrder()
        {
            var player = new LocalPlayerState();
            Assert.AreEqual(Tribe.None, BoardTribeAnalyzer.GetMostCommonTribe(player));

            var dragon = TestInstance("dragon", "dragon", 0);
            dragon.Tribes = new List<Tribe> { Tribe.Dragon };
            var murloc = TestInstance("murloc", "murloc", 0);
            murloc.Tribes = new List<Tribe> { Tribe.Murloc };
            player.Board.Add(dragon);
            player.Board.Add(murloc);

            Assert.AreEqual(Tribe.Murloc, BoardTribeAnalyzer.GetMostCommonTribe(player));
        }

        [Test]
        public void BoardTribeAnalyzer_SelectOneOfEachTribeDoesNotSelectSameAllMinionMoreThanOnce()
        {
            var board = new List<MinionInstance>();
            for (var index = 0; index < 6; index += 1)
            {
                var minion = TestInstance("typed-" + index, "typed-" + index, 0);
                minion.Tribes = new List<Tribe> { (Tribe)index };
                board.Add(minion);
            }

            var all = TestInstance("all", "all", 0);
            all.Tribes = new List<Tribe> { Tribe.All };
            board.Add(all);

            var selected = BoardTribeAnalyzer.SelectOneOfEachTribe(board);

            Assert.LessOrEqual(selected.Count, 7);
            Assert.AreEqual(selected.Count, selected.Select(minion => minion.InstanceId).Distinct().Count());
            Assert.AreEqual(7, selected.Count);
        }

        [Test]
        public void BoardTribeAnalyzer_SumStatsFromDifferentTribesExcludesSourceAndRespectsMaxCount()
        {
            var source = TestInstance("source", "source", 0);
            source.Tribes = new List<Tribe> { Tribe.Naga };
            source.Attack = 20;
            source.MaxHealth = 20;
            var beast = TestInstance("beast", "beast", 0);
            beast.Tribes = new List<Tribe> { Tribe.Beast };
            beast.Attack = 3;
            beast.MaxHealth = 5;
            var all = TestInstance("all", "all", 0);
            all.Tribes = new List<Tribe> { Tribe.All };
            all.Attack = 7;
            all.MaxHealth = 11;
            var demon = TestInstance("demon", "demon", 0);
            demon.Tribes = new List<Tribe> { Tribe.Demon };
            demon.Attack = 13;
            demon.MaxHealth = 17;

            var stats = BoardTribeAnalyzer.SumStatsFromDifferentTribes(new[] { source, beast, all, demon }, source, 2);

            Assert.AreEqual(10, stats.Attack);
            Assert.AreEqual(16, stats.Health);
        }

        [Test]
        public void BoardTribeAnalyzer_RefreshRecomputesAfterSummonAndRebornBoardChanges()
        {
            var player = new LocalPlayerState();
            var dragon = TestInstance("dragon", "dragon", 0);
            dragon.Tribes = new List<Tribe> { Tribe.Dragon };
            player.Board.Add(dragon);

            BoardTribeAnalyzer.Refresh(player);
            Assert.AreEqual(1, player.BoardTribeDistribution[Tribe.Dragon]);

            var summoned = TestInstance("summoned", "summoned", 0);
            summoned.Tribes = new List<Tribe> { Tribe.Beast };
            player.Board.Add(summoned);
            BoardTribeAnalyzer.Refresh(player);
            Assert.AreEqual(1, player.BoardTribeDistribution[Tribe.Beast]);

            var reborn = TestInstance("reborn", "reborn", 0);
            reborn.Tribes = new List<Tribe> { Tribe.Undead };
            player.Board[1] = reborn;
            BoardTribeAnalyzer.Refresh(player);
            Assert.IsFalse(player.BoardTribeDistribution.ContainsKey(Tribe.Beast));
            Assert.AreEqual(1, player.BoardTribeDistribution[Tribe.Undead]);
        }

        private static MinionDefinition TestMinion(string id, int tier, int attack, int health, int poolCount)
        {
            return new MinionDefinition
            {
                Id = id,
                CardId = id.ToUpperInvariant(),
                DbfId = 1,
                Name = id,
                TavernTier = tier,
                BaseAttack = attack,
                BaseHealth = health,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                InPool = true,
                PoolCount = poolCount
            };
        }

        private static MinionInstance TestInstance(string instanceId, string definitionId, int poolCopiesHeld)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = definitionId,
                CardId = definitionId.ToUpperInvariant(),
                Name = definitionId,
                Attack = 2,
                Health = 2,
                MaxHealth = 2,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Golden = false,
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true,
                AttacksThisCombat = 0,
                PoolSource = PoolSource.Pool,
                PoolCopiesHeld = poolCopiesHeld
            };
        }

        private static MinionInstance DeathrattleRewardSource(string instanceId, string cardId)
        {
            var source = TestInstance(instanceId, cardId.ToLowerInvariant(), 0);
            source.CardId = cardId;
            source.Attack = 0;
            source.Health = 1;
            source.MaxHealth = 1;
            source.Keywords.Add(Keyword.Deathrattle);
            return source;
        }

        private static MinionInstance LethalOpponent(string instanceId)
        {
            var opponent = TestInstance(instanceId, "opponent", 0);
            opponent.Attack = 1;
            opponent.Health = 10;
            opponent.MaxHealth = 10;
            return opponent;
        }
    }
}
