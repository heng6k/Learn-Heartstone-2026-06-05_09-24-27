using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class CombatRulesConformanceTests
    {
        private const string SnakeTrap = "TB_Bacon_Secrets_02";
        private const string SplittingImage = "TB_Bacon_Secrets_04";
        private const string Redemption = "TB_Bacon_Secrets_10";
        private const string Avenge = "TB_Bacon_Secrets_08";

        [Test]
        public void EqualCountsRandomlyChooseEitherSideAcrossSeeds()
        {
            var sides = new HashSet<BoardSide>();
            for (var seed = 0; seed < 64; seed += 1)
            {
                var result = CombatEngine.SimulateBasicCombat(
                    new[] { Minion("p", BoardSide.Player, 1, 100) },
                    new[] { Minion("o", BoardSide.Opponent, 1, 100) },
                    seed,
                    1);
                sides.Add(FirstAttack(result).ActorSide);
            }

            CollectionAssert.AreEquivalent(new[] { BoardSide.Player, BoardSide.Opponent }, sides);
        }

        [Test]
        public void ZeroAttackAndDisabledMinionsYieldToOpponent()
        {
            var result = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    Minion("p-zero", BoardSide.Player, 0, 5),
                    Minion("p-disabled", BoardSide.Player, 5, 5, false)
                },
                new[] { Minion("o-attacker", BoardSide.Opponent, 5, 5) },
                1,
                1);

            Assert.AreEqual(BoardSide.Opponent, FirstAttack(result).ActorSide);
            Assert.AreEqual("o-attacker", FirstAttack(result).ActorId);
        }

        [Test]
        public void SideWithoutAttackerYieldsInsteadOfEndingCombat()
        {
            var result = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    Minion("p-disabled-a", BoardSide.Player, 5, 5, false),
                    Minion("p-disabled-b", BoardSide.Player, 5, 5, false)
                },
                new[] { Minion("o-attacker", BoardSide.Opponent, 5, 5) },
                2,
                1);

            Assert.AreEqual(BoardSide.Opponent, FirstAttack(result).ActorSide);
        }

        [Test]
        public void BothSidesWithoutAttackersEndInDrawRegardlessOfBoardCount()
        {
            var result = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    Minion("p-disabled-a", BoardSide.Player, 5, 5, false),
                    Minion("p-disabled-b", BoardSide.Player, 5, 5, false)
                },
                new[] { Minion("o-disabled", BoardSide.Opponent, 5, 5, false) },
                3,
                10);

            Assert.AreEqual(CombatWinner.Draw, result.Winner);
            Assert.AreEqual(0, result.Steps);
        }

        [Test]
        public void AllStealthedDefendersForceYieldUntilTheyAttack()
        {
            var result = CombatEngine.SimulateBasicCombat(
                new[] { Minion("p-attacker", BoardSide.Player, 5, 20) },
                new[] { Minion("o-stealth", BoardSide.Opponent, 1, 20, true, Keyword.Stealth) },
                4,
                1);

            Assert.AreEqual(BoardSide.Opponent, FirstAttack(result).ActorSide);
            Assert.AreEqual("o-stealth", FirstAttack(result).ActorId);
        }

        [Test]
        public void ZeroAttackStealthedMinionLosesStealthAndCanBeAttacked()
        {
            var result = CombatEngine.SimulateBasicCombat(
                new[] { Minion("p-attacker", BoardSide.Player, 1, 20) },
                new[] { Minion("o-zero-stealth", BoardSide.Opponent, 0, 20, true, Keyword.Stealth) },
                41,
                1);

            Assert.AreEqual("o-zero-stealth", FirstAttack(result).TargetId);
            Assert.IsFalse(result.FinalOpponentBoard.Single().Keywords.Contains(Keyword.Stealth));
        }

        [Test]
        public void AllMinionsStealthedOnBothSidesLoseStealthInsteadOfDeadlocking()
        {
            var result = CombatEngine.SimulateBasicCombat(
                new[] { Minion("p-stealth", BoardSide.Player, 1, 20, true, Keyword.Stealth) },
                new[] { Minion("o-stealth", BoardSide.Opponent, 1, 20, true, Keyword.Stealth) },
                42,
                1);

            Assert.AreEqual(1, result.Replay.Frames.Count(frame => frame.EventType == CombatEventType.AttackDeclared));
            Assert.IsFalse(result.FinalPlayerBoard.Single().Keywords.Contains(Keyword.Stealth));
            Assert.IsFalse(result.FinalOpponentBoard.Single().Keywords.Contains(Keyword.Stealth));
        }

        [Test]
        public void StartOfCombatSummonRecomputesFirstAttacker()
        {
            var opponentTavern = new TavernState { HeroOzumatActive = true };
            var result = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    Minion("p-a", BoardSide.Player, 1, 100),
                    Minion("p-b", BoardSide.Player, 0, 100, false)
                },
                new[]
                {
                    Minion("o-a", BoardSide.Opponent, 1, 100),
                    Minion("o-b", BoardSide.Opponent, 0, 100, false)
                },
                5,
                1,
                opponentTavern: opponentTavern);

            Assert.IsTrue(result.Replay.Frames.Any(frame =>
                frame.EventType == CombatEventType.MinionSummoned &&
                frame.ActorSide == BoardSide.Opponent));
            Assert.AreEqual(BoardSide.Opponent, FirstAttack(result).ActorSide);
        }

        [TestCase(BoardSide.Player, true)]
        [TestCase(BoardSide.Opponent, false)]
        public void HeroStartOfCombatEffectsUseConfiguredFirstSide(BoardSide firstSide, bool teronReanimates)
        {
            var target = Minion("p-hero-order-target", BoardSide.Player, 0, 1, false);
            var playerTavern = new TavernState
            {
                HeroTeronGorefiendActive = true,
                HeroTeronTargetInstanceId = target.InstanceId
            };
            var opponentTavern = new TavernState
            {
                HeroTavishDeadeyeActive = true,
                HeroTavishTargetInstanceId = target.InstanceId
            };
            var result = CombatEngine.SimulateBasicCombat(
                new[] { target },
                new[] { Minion("o-hero-order-control", BoardSide.Opponent, 0, 100, false) },
                43,
                1,
                playerTavern: playerTavern,
                opponentTavern: opponentTavern,
                startOfCombatFirstSide: firstSide);

            Assert.AreEqual(
                teronReanimates,
                result.FinalPlayerBoard.Any(minion => minion.InstanceId.Contains("teron-reanimated")));
        }

        [Test]
        public void PrecombatImmediateAttackDoesNotConsumeNaturalFirstAttack()
        {
            var wingman = Minion("p-wingman", BoardSide.Player, 1, 100);
            wingman.Tags.Add("wingmen_immediate_attack_pending");
            var result = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    wingman,
                    Minion("p-natural", BoardSide.Player, 1, 100)
                },
                new[] { Minion("o-target", BoardSide.Opponent, 1, 100) },
                6,
                2);
            var attacks = result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.AttackDeclared)
                .ToList();

            Assert.AreEqual(2, attacks.Count);
            Assert.IsTrue(attacks[0].TriggeredAttack);
            Assert.AreEqual(BoardSide.Player, attacks[1].ActorSide);
            Assert.IsFalse(attacks[1].TriggeredAttack);
        }

        [Test]
        public void VenomousCleaveDestroysOnlyFirstMinionActuallyDamaged()
        {
            var cleaver = Minion("p-cleaver", BoardSide.Player, 1, 100, true, Keyword.Venomous);
            cleaver.CardId = "BG26_817";
            var result = CombatEngine.SimulateBasicCombat(
                WithDisabledFillers(cleaver, BoardSide.Player, 4),
                new[]
                {
                    Minion("o-left", BoardSide.Opponent, 0, 100),
                    Minion("o-middle", BoardSide.Opponent, 0, 100, true, Keyword.Taunt),
                    Minion("o-right", BoardSide.Opponent, 0, 100)
                },
                7,
                1);

            Assert.AreEqual(2, result.FinalOpponentBoard.Count);
            Assert.AreEqual(2, result.FinalOpponentBoard.Count(minion => minion.Health == 99));
        }

        [Test]
        public void CleaveAdjacentDamageRunsDamageObservers()
        {
            var cleaver = Minion("p-cleaver", BoardSide.Player, 1, 100);
            cleaver.CardId = "BG26_817";
            var orca = Minion("o-orca", BoardSide.Opponent, 1, 10);
            orca.CardId = "BG34_312";
            var result = CombatEngine.SimulateBasicCombat(
                WithDisabledFillers(cleaver, BoardSide.Player, 4),
                new[]
                {
                    orca,
                    Minion("o-primary", BoardSide.Opponent, 1, 10, true, Keyword.Taunt),
                    Minion("o-support", BoardSide.Opponent, 1, 10)
                },
                8,
                1);

            Assert.AreEqual(2, result.FinalOpponentBoard.Single(minion => minion.InstanceId == "o-primary").Attack);
            Assert.AreEqual(2, result.FinalOpponentBoard.Single(minion => minion.InstanceId == "o-support").Attack);
        }

        [Test]
        public void StartOfCombatDamageRunsDamageObservers()
        {
            var tavish = new TavernState
            {
                HeroTavishDeadeyeActive = true,
                HeroTavishTargetInstanceId = "o-orca"
            };
            var orca = Minion("o-orca", BoardSide.Opponent, 1, 10);
            orca.CardId = "BG34_312";
            var result = CombatEngine.SimulateBasicCombat(
                new[] { Minion("p-source", BoardSide.Player, 1, 100) },
                new[] { orca, Minion("o-support", BoardSide.Opponent, 1, 10) },
                81,
                0,
                playerTavern: tavish);

            Assert.AreEqual(9, result.FinalOpponentBoard.Single(minion => minion.InstanceId == "o-orca").Health);
            Assert.AreEqual(2, result.FinalOpponentBoard.Single(minion => minion.InstanceId == "o-support").Attack);
            Assert.AreEqual(11, result.FinalOpponentBoard.Single(minion => minion.InstanceId == "o-support").Health);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void WildfireDamagesOneAdjacentNormallyAndBothWhenGolden(bool golden, int expectedDamagedAdjacent)
        {
            var wildfire = Minion("p-wildfire", BoardSide.Player, 5, 100);
            wildfire.CardId = "BGS_126";
            wildfire.Golden = golden;
            var result = CombatEngine.SimulateBasicCombat(
                WithDisabledFillers(wildfire, BoardSide.Player, 4),
                new[]
                {
                    Minion("o-left", BoardSide.Opponent, 0, 100),
                    Minion("o-primary", BoardSide.Opponent, 0, 1, true, Keyword.Taunt),
                    Minion("o-right", BoardSide.Opponent, 0, 100)
                },
                9,
                1);

            Assert.AreEqual(
                expectedDamagedAdjacent,
                result.FinalOpponentBoard.Count(minion => minion.InstanceId != "o-primary" && minion.Health == 96));
        }

        [Test]
        public void SimultaneousDeathsReleaseAllBoardSpaceBeforeDeathrattles()
        {
            var cleaver = Minion("p-cleaver", BoardSide.Player, 1, 100);
            cleaver.CardId = "BG26_817";
            var manasaber = Minion("o-manasaber", BoardSide.Opponent, 0, 1, true, Keyword.Deathrattle);
            manasaber.CardId = "BG26_800";
            var result = CombatEngine.SimulateBasicCombat(
                WithDisabledFillers(cleaver, BoardSide.Player, 7),
                new[]
                {
                    manasaber,
                    Minion("o-primary", BoardSide.Opponent, 0, 1, true, Keyword.Taunt),
                    Minion("o-filler-0", BoardSide.Opponent, 0, 100, false),
                    Minion("o-filler-1", BoardSide.Opponent, 0, 100, false),
                    Minion("o-filler-2", BoardSide.Opponent, 0, 100, false),
                    Minion("o-filler-3", BoardSide.Opponent, 0, 100, false),
                    Minion("o-filler-4", BoardSide.Opponent, 0, 100, false)
                },
                10,
                1);

            Assert.AreEqual(2, result.FinalOpponentBoard.Count(minion => minion.InstanceId.Contains("cubling")));
            Assert.AreEqual(0, result.Replay.Frames.Count(frame => frame.EventType == CombatEventType.SummonOverflowed));
            Assert.AreEqual(7, result.FinalOpponentBoard.Count);
        }

        [Test]
        public void OrdinaryRebornRebuildsPrintedStateAndRestoresNativeKeywords()
        {
            var reborn = Minion("p-reborn", BoardSide.Player, 10, 10, true, Keyword.Reborn, Keyword.Windfury);
            reborn.BaseAttack = 2;
            reborn.BaseHealth = 3;
            reborn.OfficialKeywords = new List<Keyword> { Keyword.DivineShield, Keyword.Reborn };
            reborn.Enchantments.Add(new Enchantment { Id = "buff", SourceId = "buff", AttackBonus = 8, HealthBonus = 7 });
            reborn.Counters["combat-counter"] = 4;
            var result = CombatEngine.SimulateBasicCombat(
                new[] { reborn },
                new[]
                {
                    Minion("o-killer", BoardSide.Opponent, 20, 100),
                    Minion("o-filler", BoardSide.Opponent, 0, 100, false)
                },
                11,
                1);
            var returned = result.FinalPlayerBoard.Single();

            Assert.AreEqual(2, returned.Attack);
            Assert.AreEqual(1, returned.Health);
            Assert.AreEqual(3, returned.MaxHealth);
            Assert.IsEmpty(returned.Enchantments);
            Assert.IsEmpty(returned.Counters);
            Assert.IsTrue(returned.Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(returned.OfficialKeywords.Contains(Keyword.Reborn));
            Assert.IsFalse(returned.Keywords.Contains(Keyword.Reborn));
            Assert.IsFalse(returned.Keywords.Contains(Keyword.Windfury));
        }

        [Test]
        public void FullEnchantmentDarkGiftRebornRemainsExplicitException()
        {
            var reborn = Minion("p-gifted", BoardSide.Player, 10, 10, true, Keyword.Reborn);
            reborn.BaseAttack = 2;
            reborn.BaseHealth = 3;
            reborn.OfficialKeywords = new List<Keyword> { Keyword.Reborn };
            reborn.Enchantments.Add(new Enchantment { Id = "gift-buff", SourceId = "gift-buff", AttackBonus = 8, HealthBonus = 7 });
            reborn.Tags.Add("dark-gift.dg-r41");
            var result = CombatEngine.SimulateBasicCombat(
                new[] { reborn },
                new[]
                {
                    Minion("o-killer", BoardSide.Opponent, 20, 100),
                    Minion("o-filler", BoardSide.Opponent, 0, 100, false)
                },
                12,
                1);
            var returned = result.FinalPlayerBoard.Single();

            Assert.AreEqual(10, returned.Attack);
            Assert.AreEqual(10, returned.Health);
            Assert.AreEqual(10, returned.MaxHealth);
            Assert.AreEqual(1, returned.Enchantments.Count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AttackSecretsResolveInAcquisitionOrderAndUnusableSecretRemains(bool splittingFirst)
        {
            var opponentTavern = new TavernState
            {
                Secrets = splittingFirst
                    ? new List<SecretState> { Secret(SplittingImage, 1), Secret(SnakeTrap, 2) }
                    : new List<SecretState> { Secret(SnakeTrap, 1), Secret(SplittingImage, 2) }
            };
            var result = CombatEngine.SimulateBasicCombat(
                WithDisabledFillers(Minion("p-attacker", BoardSide.Player, 1, 100), BoardSide.Player, 7),
                new[]
                {
                    Minion("o-defender", BoardSide.Opponent, 1, 100, true, Keyword.Taunt),
                    Minion("o-filler-0", BoardSide.Opponent, 0, 100, false),
                    Minion("o-filler-1", BoardSide.Opponent, 0, 100, false),
                    Minion("o-filler-2", BoardSide.Opponent, 0, 100, false),
                    Minion("o-filler-3", BoardSide.Opponent, 0, 100, false),
                    Minion("o-filler-4", BoardSide.Opponent, 0, 100, false)
                },
                13,
                1,
                opponentTavern: opponentTavern);

            Assert.AreEqual(splittingFirst ? 1 : 0, result.FinalOpponentBoard.Count(minion => minion.InstanceId.StartsWith("splitting-image-")));
            Assert.AreEqual(splittingFirst ? 0 : 1, result.FinalOpponentBoard.Count(minion => minion.Name == "Snake"));
            Assert.AreEqual(1, opponentTavern.Secrets.Count);
            Assert.AreEqual(splittingFirst ? SnakeTrap : SplittingImage, opponentTavern.Secrets[0].SecretCardId);
        }

        [Test]
        public void RedemptionCreatesUnenchantedFreshCopyWithNativeKeywords()
        {
            var target = Minion("p-redemption", BoardSide.Player, 10, 10, true, Keyword.DivineShield);
            target.BaseAttack = 2;
            target.BaseHealth = 3;
            target.OfficialKeywords = new List<Keyword> { Keyword.DivineShield };
            target.Enchantments.Add(new Enchantment { Id = "buff", SourceId = "buff", AttackBonus = 8, HealthBonus = 7 });
            var tavern = new TavernState { Secrets = new List<SecretState> { Secret(Redemption, 1, BoardSide.Player) } };
            var result = CombatEngine.SimulateBasicCombat(
                new[] { target },
                new[]
                {
                    Minion("o-killer", BoardSide.Opponent, 20, 100, true, Keyword.Windfury),
                    Minion("o-filler", BoardSide.Opponent, 0, 100, false)
                },
                14,
                2,
                playerTavern: tavern);
            var returned = result.FinalPlayerBoard.Single();

            Assert.AreEqual(2, returned.Attack);
            Assert.AreEqual(1, returned.Health);
            Assert.AreEqual(3, returned.MaxHealth);
            Assert.IsEmpty(returned.Enchantments);
            Assert.IsTrue(returned.Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void DeathrattleFillsBoardBeforeRedemptionAndLeavesUnusableSecretArmed()
        {
            var manasaber = Minion("p-manasaber", BoardSide.Player, 0, 1, false, Keyword.Deathrattle, Keyword.Taunt);
            manasaber.CardId = "BG26_800";
            var tavern = new TavernState { Secrets = new List<SecretState> { Secret(Redemption, 1, BoardSide.Player) } };
            var result = CombatEngine.SimulateBasicCombat(
                WithDisabledFillers(manasaber, BoardSide.Player, 7),
                new[] { Minion("o-killer", BoardSide.Opponent, 1, 100) },
                15,
                1,
                playerTavern: tavern);

            Assert.AreEqual(7, result.FinalPlayerBoard.Count);
            Assert.AreEqual(1, result.FinalPlayerBoard.Count(minion => minion.InstanceId.Contains("cubling")));
            Assert.AreEqual(1, tavern.Secrets.Count);
            Assert.AreEqual(Redemption, tavern.Secrets[0].SecretCardId);
            Assert.IsFalse(result.Replay.Frames.Any(frame =>
                frame.EventType == CombatEventType.MinionSummoned && frame.ActorId == Redemption));
        }

        [Test]
        public void RebornResolvesBeforeRedemption()
        {
            var target = Minion("p-reborn-redemption", BoardSide.Player, 0, 1, false, Keyword.Reborn, Keyword.Taunt);
            target.OfficialKeywords = new List<Keyword> { Keyword.Reborn };
            var tavern = new TavernState { Secrets = new List<SecretState> { Secret(Redemption, 1, BoardSide.Player) } };
            var result = CombatEngine.SimulateBasicCombat(
                new[] { target },
                new[] { Minion("o-killer", BoardSide.Opponent, 1, 100) },
                16,
                1,
                playerTavern: tavern);
            var rebornFrame = result.Replay.Frames.FindIndex(frame => frame.EventType == CombatEventType.RebornResolved);
            var redemptionFrame = result.Replay.Frames.FindIndex(frame =>
                frame.EventType == CombatEventType.MinionSummoned && frame.ActorId == Redemption);

            Assert.GreaterOrEqual(rebornFrame, 0);
            Assert.Greater(redemptionFrame, rebornFrame);
        }

        [Test]
        public void AvengeWithoutAnotherFriendlyMinionRemainsArmed()
        {
            var tavern = new TavernState { Secrets = new List<SecretState> { Secret(Avenge, 1, BoardSide.Player) } };
            CombatEngine.SimulateBasicCombat(
                new[] { Minion("p-alone", BoardSide.Player, 0, 1, false, Keyword.Taunt) },
                new[] { Minion("o-killer", BoardSide.Opponent, 1, 100) },
                17,
                1,
                playerTavern: tavern);

            Assert.AreEqual(1, tavern.Secrets.Count);
            Assert.AreEqual(Avenge, tavern.Secrets[0].SecretCardId);
        }

        [TestCase(true, 2, 4)]
        [TestCase(false, 4, 8)]
        public void BeastSummonObserversUseOrderOfPlay(bool slammaFirst, int firstExpectedAttack, int secondExpectedAttack)
        {
            var slamma = Minion("p-slamma", BoardSide.Player, 0, 100, false);
            slamma.CardId = "BG26_802";
            slamma.OrderOfPlay = slammaFirst ? 1 : 2;
            var moonRider = Minion("p-moon-rider", BoardSide.Player, 0, 100, false);
            moonRider.CardId = "BG35_602";
            moonRider.OrderOfPlay = slammaFirst ? 2 : 1;
            var manasaber = Minion("p-order-manasaber", BoardSide.Player, 0, 1, false, Keyword.Deathrattle, Keyword.Taunt);
            manasaber.CardId = "BG26_800";
            manasaber.OrderOfPlay = 3;
            var result = CombatEngine.SimulateBasicCombat(
                new[] { slamma, moonRider, manasaber },
                new[] { Minion("o-order-killer", BoardSide.Opponent, 1, 100) },
                18,
                1);
            var cublingAttacks = result.FinalPlayerBoard
                .Where(minion => minion.InstanceId.Contains("cubling"))
                .Select(minion => minion.Attack)
                .OrderBy(attack => attack)
                .ToList();

            CollectionAssert.AreEqual(new[] { firstExpectedAttack, secondExpectedAttack }, cublingAttacks);
        }

        [TestCase(true, "BG25_008")]
        [TestCase(false, "CUBLING")]
        public void AvengeAndDeathrattleUseSourceOrderOfPlay(bool avengeFirst, string expectedSummonCardId)
        {
            var eternalSummoner = Minion("p-eternal-summoner", BoardSide.Player, 0, 100, false, Keyword.Avenge);
            eternalSummoner.CardId = "BG34_403";
            eternalSummoner.OrderOfPlay = avengeFirst ? 1 : 2;
            eternalSummoner.Counters["avenge_threshold"] = 1;
            var manasaber = Minion("p-avenge-manasaber", BoardSide.Player, 0, 1, false, Keyword.Deathrattle, Keyword.Taunt);
            manasaber.CardId = "BG26_800";
            manasaber.OrderOfPlay = avengeFirst ? 2 : 1;
            var board = new List<MinionInstance> { eternalSummoner, manasaber };
            while (board.Count < 7)
            {
                board.Add(Minion("p-avenge-filler-" + board.Count, BoardSide.Player, 0, 100, false));
            }

            var result = CombatEngine.SimulateBasicCombat(
                board,
                new[] { Minion("o-avenge-killer", BoardSide.Opponent, 1, 100) },
                19,
                1);

            Assert.IsTrue(result.FinalPlayerBoard.Any(minion =>
                string.Equals(minion.CardId, expectedSummonCardId, System.StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(result.FinalPlayerBoard.Any(minion =>
                string.Equals(
                    minion.CardId,
                    avengeFirst ? "CUBLING" : "BG25_008",
                    System.StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public void RepeatedDeathrattleSummonsAlwaysUseUniqueInstanceIds()
        {
            var macaw = Minion("p-macaw", BoardSide.Player, 1, 100);
            macaw.CardId = "BGS_078";
            var manasaber = Minion("p-manasaber", BoardSide.Player, 0, 1, true, Keyword.Deathrattle, Keyword.Taunt);
            manasaber.CardId = "BG26_800";
            var cleaver = Minion("o-cleaver", BoardSide.Opponent, 1, 100, true, Keyword.Taunt);
            cleaver.CardId = "BG26_817";
            var result = CombatEngine.SimulateBasicCombat(
                new[] { macaw, manasaber, Minion("p-filler", BoardSide.Player, 0, 100, false) },
                new[] { cleaver, Minion("o-filler", BoardSide.Opponent, 0, 100, false) },
                3,
                2);

            Assert.AreEqual(
                result.FinalPlayerBoard.Count,
                result.FinalPlayerBoard.Select(minion => minion.InstanceId).Distinct().Count());
            var summonedIds = result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.MinionSummoned && !string.IsNullOrEmpty(frame.TargetId))
                .Select(frame => frame.TargetId)
                .ToList();
            Assert.AreEqual(summonedIds.Count, summonedIds.Distinct().Count());
        }

        private static CombatFrame FirstAttack(CombatOutput result)
        {
            return result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackDeclared);
        }

        private static IEnumerable<MinionInstance> WithDisabledFillers(MinionInstance first, BoardSide side, int count)
        {
            var board = new List<MinionInstance> { first };
            while (board.Count < count)
            {
                board.Add(Minion(side + "-filler-" + board.Count, side, 0, 100, false));
            }

            return board;
        }

        private static SecretState Secret(string cardId, int round, BoardSide owner = BoardSide.Opponent)
        {
            return new SecretState
            {
                SecretCardId = cardId,
                Name = cardId,
                Owner = owner,
                CreatedRound = round
            };
        }

        private static MinionInstance Minion(
            string id,
            BoardSide side,
            int attack,
            int health,
            bool canAttack = true,
            params Keyword[] keywords)
        {
            return new MinionInstance
            {
                InstanceId = id,
                DefinitionId = id,
                CardId = id.ToUpperInvariant(),
                Name = id,
                Owner = side,
                CardKind = CardKind.Minion,
                Attack = attack,
                BaseAttack = attack,
                Health = health,
                MaxHealth = health,
                BaseHealth = health,
                TavernTier = 1,
                CanAttack = canAttack,
                Keywords = keywords.ToList(),
                OfficialKeywords = keywords.ToList(),
                Tribes = new List<Tribe> { Tribe.None },
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>()
            };
        }
    }
}
