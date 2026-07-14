using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class OpponentCombatMechanicBlackBoxTests
    {
        private const string AlAkirHeroPowerCardId = "TB_BaconShop_HP_086";
        private const string DeathwingHeroPowerCardId = "TB_BaconShop_HP_061";
        private const string YshaarjHeroPowerCardId = "TB_BaconShop_HP_103";
        private const string OnyxiaHeroPowerCardId = "BG22_HERO_305p";
        private const string BrukanHeroPowerCardId = "BG22_HERO_001p";
        private const string StaffOfOriginationRewardId = "BG24_Reward_312";
        private const string TurbulentTombsRewardId = "BG27_Reward_803";
        private const string LesserValorousMedallionCardId = "BG30_MagicItem_970";
        private const string GreaterValorousMedallionCardId = "BG30_MagicItem_970t";
        private const string LesserBirdFeederCardId = "BG32_MagicItem_864";
        private const string GreaterAllPurposeKibbleCardId = "BG32_MagicItem_200";
        private const string LesserShipInABottleCardId = "BG30_MagicItem_407";
        private const string ColdlightSeerCardId = "BG33_894";
        private const string TitusRivendareCardId = "BG25_354";
        private const string EternalKnightCardId = "BG25_008";
        private const string TavishHeroPowerCardId = "BG22_HERO_000p";
        private const string TamsinHeroPowerCardId = "BG20_HERO_282p";
        private const string IllidanHeroPowerCardId = "TB_BaconShop_HP_069";
        private const string QueenWagtoggleHeroPowerCardId = "TB_BaconShop_HP_037a";
        private const string VanndarHeroPowerCardId = "BG22_HERO_003p";
        private const string DrektharHeroPowerCardId = "BG22_HERO_002p";
        private const string TeronHeroPowerCardId = "BG25_HERO_103p";
        private const string OzumatHeroPowerCardId = "BG23_HERO_201p";
        private const string ZerglingCardId = "BG31_HERO_811t2";
        private const string HarmlessBoneheadCardId = "BG28_300";
        private const string ForestRoverCardId = "BG31_801";
        private const string BassgillCardId = "BG26_350";
        private const string OperaticBelcherCardId = "BG33_318";
        private const string BristlebachPortraitMinionCardId = "BG26_157";
        private const string CordPullerCardId = "BG29_611";
        private const string ManasaberCardId = "BG26_800";
        private const string ThornedTrailblazerCardId = "BG35_437";
        private const string BristlebackScrapSmithCardId = "BG24_707";
        private const string TideRaiserCardId = "BG34_920";
        private const string HeavyMetalWyrmCardId = "BG26_801";
        private const string ImpulsiveTricksterCardId = "BG21_006";
        private const string KaboomBotCardId = "BG_BOT_606";
        private const string TarecgosaCardId = "BG21_015";
        private const string SkyPirateFlagbearerCardId = "BG30_119";

        public enum MatrixCaseKind
        {
            HeroPower,
            Trinket
        }

        public sealed class MatrixCase
        {
            public int Ordinal;
            public string CaseId;
            public MatrixCaseKind Kind;
            public TrinketSlotKind SlotKind;
            public string CardId;
            public string Name;
            public string EffectId;
            public string Template;

            public override string ToString()
            {
                return CaseId + " " + Name;
            }
        }

        public static IEnumerable<TestCaseData> FullDocumentedOpponentCombatMatrixCases()
        {
            var rows = LoadFullDocumentedOpponentCombatMatrixRows().ToList();
            if (rows.Count != 102)
            {
                throw new InvalidOperationException("Expected 102 opponent combat matrix rows, but found " + rows.Count + ".");
            }

            foreach (var row in rows)
            {
                yield return new TestCaseData(row).SetName("RunCombatTest_" + SanitizeTestName(row.CaseId + "_" + row.Name));
            }
        }

        [TestCaseSource(nameof(FullDocumentedOpponentCombatMatrixCases))]
        public void RunCombatTest_FullDocumentedOpponentCombatMatrixCaseRunsBlackBox(MatrixCase row)
        {
            var service = CreateFullMatrixService();
            ConfigureMatrixCombat(service, row);
            var pending = PendingChoice(AdvancedMechanicKind.Trinket);
            service.State.Player.Tavern.AdvancedMechanics.PendingChoice = pending;

            if (row.Kind == MatrixCaseKind.HeroPower)
            {
                service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, row.CardId, CardKind.HeroPower));
                ConfigureMatrixHeroPowerTarget(service, row);
            }
            else
            {
                if (string.Equals(row.Template, "TR-START-EXTRA", StringComparison.OrdinalIgnoreCase))
                {
                    service.Apply(new GameCommand(
                        GameCommandType.SetOpponentTrinket,
                        LesserShipInABottleCardId,
                        CardKind.Trinket,
                        0));
                }

                service.Apply(new GameCommand(
                    GameCommandType.SetOpponentTrinket,
                    row.CardId,
                    CardKind.Trinket,
                    row.SlotKind == TrinketSlotKind.Greater ? 1 : 0));
            }

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions
                {
                    Seed = 5000 + row.Ordinal,
                    SafetyLimit = MatrixSafetyLimit(row)
                }));

            AssertMatrixCombatCompleted(service, row);
            Assert.AreSame(pending, service.State.Player.Tavern.AdvancedMechanics.PendingChoice);

            if (row.Kind == MatrixCaseKind.HeroPower)
            {
                Assert.AreEqual(row.CardId, service.State.Opponent.HeroPowerCardId);
                AssertHeroPowerMatrixObservation(service, row);
            }
            else
            {
                Assert.AreEqual(row.CardId, service.GetOpponentTrinketDefinition(row.SlotKind)?.CardId);
                Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId);
                Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.Trinkets.GreaterTrinketId);
                Assert.IsFalse(service.State.LastResult.PlayerRewards.Any(reward =>
                    string.Equals(reward.SourceCardId, row.CardId, StringComparison.OrdinalIgnoreCase)));
                AssertTrinketMatrixObservation(service, row);
            }
        }

        [Test]
        public void RunCombatTest_OpponentQuestTrinketsAndHeroPowerStackOnOpponentOnly()
        {
            var service = CreateService();
            service.State.Round = 9;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Gold = 7;
            var pending = PendingChoice(AdvancedMechanicKind.Trinket);
            service.State.Player.Tavern.AdvancedMechanics.PendingChoice = pending;
            service.State.Player.Board.Add(TestMinion("player-control", BoardSide.Player, 0, 100));
            service.State.Opponent.Board.Add(TestMinion("opponent-stacked", BoardSide.Opponent, 2, 3));

            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, AlAkirHeroPowerCardId, CardKind.HeroPower));
            service.Apply(new GameCommand(GameCommandType.SetOpponentQuestReward, StaffOfOriginationRewardId, CardKind.QuestReward));
            service.Apply(new GameCommand(GameCommandType.SetOpponentTrinket, LesserValorousMedallionCardId, CardKind.Trinket, 0));
            service.Apply(new GameCommand(GameCommandType.SetOpponentTrinket, GreaterValorousMedallionCardId, CardKind.Trinket, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 4401, SafetyLimit = 1 }));

            Assert.IsNotNull(service.State.LastReplay);
            Assert.IsNotNull(service.State.LastResult);

            var opponentInitial = service.State.LastReplay.InitialSnapshot.Opponent.Minions.Single(card => card.InstanceId == "opponent-stacked");
            Assert.IsTrue(opponentInitial.Keywords.Contains(Keyword.Windfury));
            Assert.IsTrue(opponentInitial.Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(opponentInitial.Keywords.Contains(Keyword.Taunt));

            var opponentFinal = service.State.LastResult.FinalOpponentBoard.Single(card => card.InstanceId == "opponent-stacked");
            Assert.AreEqual(22, opponentFinal.Attack);
            Assert.AreEqual(23, opponentFinal.MaxHealth);
            Assert.AreEqual(23, opponentFinal.Health);

            var playerFinal = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == "player-control");
            Assert.AreEqual(0, playerFinal.Attack);
            Assert.AreEqual(100, playerFinal.MaxHealth);
            Assert.AreSame(pending, service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            Assert.AreEqual(7, service.State.Player.Tavern.Gold);
            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest);
            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId);
            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.Trinkets.GreaterTrinketId);
        }

        [TestCase(5, 2, 3)]
        [TestCase(6, 4, 5)]
        [TestCase(8, 4, 5)]
        [TestCase(9, 10, 11)]
        public void RunCombatTest_OpponentTrinketRoundGatesAffectActualCombat(int round, int expectedAttack, int expectedHealth)
        {
            var service = CreateService();
            service.State.Round = round;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Opponent.Board.Add(TestMinion("opponent-gated", BoardSide.Opponent, 2, 3));
            service.Apply(new GameCommand(GameCommandType.SetOpponentTrinket, LesserValorousMedallionCardId, CardKind.Trinket, 0));
            service.Apply(new GameCommand(GameCommandType.SetOpponentTrinket, GreaterValorousMedallionCardId, CardKind.Trinket, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 4402 + round, SafetyLimit = 1 }));

            var opponent = service.State.LastResult.FinalOpponentBoard.Single(card => card.InstanceId == "opponent-gated");
            Assert.AreEqual(expectedAttack, opponent.Attack);
            Assert.AreEqual(expectedHealth, opponent.MaxHealth);
            Assert.IsEmpty(service.State.LastResult.FinalPlayerBoard);
        }

        [Test]
        public void RunCombatTest_OpponentDeathwingHeroPowerAppliesGlobalCombatBuffOnce()
        {
            var service = CreateService();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(TestMinion("player-global", BoardSide.Player, 1, 30));
            service.State.Opponent.Board.Add(TestMinion("opponent-global", BoardSide.Opponent, 2, 30));
            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, DeathwingHeroPowerCardId, CardKind.HeroPower));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 4410, SafetyLimit = 1 }));

            var playerInitial = service.State.LastReplay.InitialSnapshot.Player.Minions.Single(card => card.InstanceId == "player-global");
            var opponentInitial = service.State.LastReplay.InitialSnapshot.Opponent.Minions.Single(card => card.InstanceId == "opponent-global");
            Assert.AreEqual(3, playerInitial.Attack);
            Assert.AreEqual(4, opponentInitial.Attack);
            Assert.AreEqual(30, playerInitial.MaxHealth);
            Assert.AreEqual(30, opponentInitial.MaxHealth);
        }

        [Test]
        public void RunCombatTest_OpponentYshaarjHeroPowerSummonsForOpponentOnly()
        {
            var service = CreateService();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Hand.Clear();
            service.State.Opponent.TavernTier = 1;
            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, YshaarjHeroPowerCardId, CardKind.HeroPower));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 4411, SafetyLimit = 1 }));

            Assert.IsEmpty(service.State.LastReplay.InitialSnapshot.Player.Minions);
            Assert.AreEqual(1, service.State.LastReplay.InitialSnapshot.Opponent.Minions.Count);
            Assert.IsEmpty(service.State.Player.Tavern.Hand);
            Assert.AreEqual(1, service.State.Opponent.Hand.Count);
            Assert.AreEqual(BoardSide.Opponent, service.State.Opponent.Hand[0].Owner);
        }

        [Test]
        public void RunCombatTest_OpponentBrukanElementUsesOpponentChoiceOnly()
        {
            var service = CreateService();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(TestMinion("player-brukan-control", BoardSide.Player, 0, 30));
            service.State.Opponent.Board.Add(TestMinion("opponent-brukan-target", BoardSide.Opponent, 2, 30));
            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, BrukanHeroPowerCardId, CardKind.HeroPower));
            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPowerElement, "fire"));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 4412, SafetyLimit = 1 }));

            var player = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == "player-brukan-control");
            var opponent = service.State.LastResult.FinalOpponentBoard.Single(card => card.InstanceId == "opponent-brukan-target");
            Assert.AreEqual(0, player.Attack);
            Assert.AreEqual(4, opponent.Attack);
            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            Assert.IsTrue(service.State.CombatLog.Any(entry =>
                entry.Title == "HeroStartOfCombat" &&
                entry.Detail.Contains("Embrace the Elements called fire")));
        }

        [Test]
        public void RunCombatTest_OpponentOnyxiaAvengeSummonsFromOpponentSide()
        {
            var service = CreateService();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(TestMinion("player-onyxia-killer", BoardSide.Player, 10, 50));
            for (var index = 0; index < 4; index += 1)
            {
                service.State.Opponent.Board.Add(TestMinion("opponent-onyxia-fodder-" + index, BoardSide.Opponent, 1, 1, Tribe.None, Keyword.Taunt));
            }

            service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPower, OnyxiaHeroPowerCardId, CardKind.HeroPower));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 4413, SafetyLimit = 8 }));

            Assert.IsTrue(service.State.CombatLog.Any(entry =>
                entry.Title == "ImmediateAttackQueued" &&
                entry.Detail.Contains("queued by Broodmother")));
            Assert.IsTrue(service.State.LastReplay.Frames.Any(frame =>
                frame.EventType == CombatEventType.MinionSummoned &&
                frame.ActorSide == BoardSide.Opponent &&
                frame.TargetId != null &&
                frame.TargetId.Contains("BG22_HERO_305t")));
            Assert.IsFalse(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.SourceCardId == OnyxiaHeroPowerCardId));
        }

        [Test]
        public void RunCombatTest_OpponentBirdFeederAvengeBuffsOpponentBoardOnly()
        {
            var service = CreateService();
            service.State.Round = 6;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(TestMinion("player-bird-feeder-killer", BoardSide.Player, 10, 100));
            service.State.Opponent.Board.Add(TestMinion("opponent-bird-feeder-fodder-0", BoardSide.Opponent, 1, 1, Tribe.None, Keyword.Taunt));
            service.State.Opponent.Board.Add(TestMinion("opponent-bird-feeder-fodder-1", BoardSide.Opponent, 1, 1, Tribe.None, Keyword.Taunt));
            service.State.Opponent.Board.Add(TestMinion("opponent-bird-feeder-survivor", BoardSide.Opponent, 1, 30));
            service.Apply(new GameCommand(GameCommandType.SetOpponentTrinket, LesserBirdFeederCardId, CardKind.Trinket, 0));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 4414, SafetyLimit = 2 }));

            var survivor = service.State.LastResult.FinalOpponentBoard.Single(card => card.InstanceId == "opponent-bird-feeder-survivor");
            var player = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == "player-bird-feeder-killer");
            Assert.AreEqual(2, survivor.Attack);
            Assert.AreEqual(31, survivor.MaxHealth);
            Assert.AreEqual(10, player.Attack);
            Assert.IsTrue(service.State.LastReplay.Frames.Any(frame =>
                frame.EventType == CombatEventType.AvengeCounterUpdated &&
                frame.ActorSide == BoardSide.Opponent &&
                frame.MechanicCounter == 2 &&
                frame.MechanicThreshold == 2));
        }

        [Test]
        public void RunCombatTest_OpponentAllPurposeKibbleAttackTriggerBuffsOpponentBeastOnly()
        {
            var service = CreateService();
            service.State.Round = 9;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(TestMinion("player-kibble-control", BoardSide.Player, 0, 40));
            service.State.Opponent.Board.Add(TestMinion("opponent-kibble-beast", BoardSide.Opponent, 2, 40, Tribe.Beast));
            service.Apply(new GameCommand(GameCommandType.SetOpponentTrinket, GreaterAllPurposeKibbleCardId, CardKind.Trinket, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 4415, SafetyLimit = 2 }));

            var opponent = service.State.LastResult.FinalOpponentBoard.Single(card => card.InstanceId == "opponent-kibble-beast");
            var player = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == "player-kibble-control");
            Assert.AreEqual(4, opponent.Attack);
            Assert.AreEqual(0, player.Attack);
            Assert.IsTrue(service.State.LastReplay.Frames.Any(frame =>
                frame.EventType == CombatEventType.AttackTriggered &&
                frame.ActorSide == BoardSide.Opponent &&
                frame.TargetId == "opponent-kibble-beast"));
        }

        [Test]
        public void RunCombatTest_OpponentQuestDeathrattleRewardStaysOpponentSide()
        {
            var service = CreateService();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestMinion("player-killer", BoardSide.Player, 10, 30));
            service.State.Opponent.Board.Add(TestCardMinion(
                "opponent-coldlight",
                ColdlightSeerCardId,
                BoardSide.Opponent,
                1,
                1,
                Tribe.Murloc,
                Keyword.Taunt,
                Keyword.Deathrattle));
            service.Apply(new GameCommand(GameCommandType.SetOpponentQuestReward, TurbulentTombsRewardId, CardKind.QuestReward));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 4403, SafetyLimit = 1 }));

            var deathrattleReward = service.State.LastResult.OpponentRewards.Single(reward =>
                reward.Type == CombatRewardType.FriendlyDeathrattleTriggered &&
                reward.SourceInstanceId == "opponent-coldlight");
            Assert.AreEqual(BoardSide.Opponent, deathrattleReward.Side);
            Assert.AreEqual(2, deathrattleReward.Amount);
            Assert.IsFalse(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.SourceInstanceId == "opponent-coldlight"));
            Assert.IsEmpty(service.State.Player.Tavern.Hand);
        }

        private static IReadOnlyList<MatrixCase> LoadFullDocumentedOpponentCombatMatrixRows()
        {
            var rows = new List<MatrixCase>();
            var trinketIndex = 0;
            foreach (var rawLine in File.ReadAllLines(MatrixDocumentPath()))
            {
                var line = rawLine.Trim();
                var parts = SplitMarkdownRow(line);
                if (parts.Length == 0)
                {
                    continue;
                }

                if (parts[0].StartsWith("OCM-BB-HP-", StringComparison.Ordinal))
                {
                    rows.Add(new MatrixCase
                    {
                        Ordinal = rows.Count + 1,
                        CaseId = parts[0],
                        Kind = MatrixCaseKind.HeroPower,
                        CardId = StripCode(parts[2]),
                        Name = parts[1],
                        Template = "HP"
                    });
                    continue;
                }

                if ((string.Equals(parts[0], "Greater", StringComparison.Ordinal) ||
                     string.Equals(parts[0], "Lesser", StringComparison.Ordinal)) &&
                    parts.Length >= 5 &&
                    parts[1].StartsWith("`", StringComparison.Ordinal))
                {
                    trinketIndex += 1;
                    rows.Add(new MatrixCase
                    {
                        Ordinal = rows.Count + 1,
                        CaseId = "OCM-BB-TR-" + trinketIndex.ToString("000"),
                        Kind = MatrixCaseKind.Trinket,
                        SlotKind = string.Equals(parts[0], "Greater", StringComparison.Ordinal)
                            ? TrinketSlotKind.Greater
                            : TrinketSlotKind.Lesser,
                        CardId = StripCode(parts[1]),
                        Name = parts[2],
                        EffectId = StripCode(parts[3]),
                        Template = parts[4]
                    });
                }
            }

            return rows;
        }

        private static string MatrixDocumentPath()
        {
            var directory = Environment.CurrentDirectory;
            for (var depth = 0; depth < 5; depth += 1)
            {
                var candidate = Path.Combine(
                    directory,
                    ".planning",
                    "tavern-ui-screenshot-requirements",
                    "step-4-opponent-combat-mechanics-blackbox-cases.md");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                var parent = Directory.GetParent(directory);
                if (parent == null)
                {
                    break;
                }

                directory = parent.FullName;
            }

            return Path.Combine(
                Environment.CurrentDirectory,
                ".planning",
                "tavern-ui-screenshot-requirements",
                "step-4-opponent-combat-mechanics-blackbox-cases.md");
        }

        private static string[] SplitMarkdownRow(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("|", StringComparison.Ordinal))
            {
                return new string[0];
            }

            return line.Trim('|')
                .Split('|')
                .Select(part => part.Trim())
                .ToArray();
        }

        private static string StripCode(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? value : value.Trim().Trim('`');
        }

        private static string SanitizeTestName(string value)
        {
            var chars = value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            var result = new string(chars);
            while (result.Contains("__"))
            {
                result = result.Replace("__", "_");
            }

            return result.Trim('_');
        }

        private static void ConfigureMatrixCombat(MatchService service, MatrixCase row)
        {
            service.State.Round = row.Kind == MatrixCaseKind.Trinket && row.SlotKind == TrinketSlotKind.Lesser ? 6 : 9;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Hand.Clear();
            service.State.Player.Tavern.Gold = 7;
            service.State.Opponent.TavernTier = 4;

            if (row.Kind == MatrixCaseKind.HeroPower)
            {
                ConfigureHeroPowerMatrixBoard(service, row);
            }
            else
            {
                ConfigureTrinketMatrixBoard(service, row);
            }
        }

        private static void ConfigureHeroPowerMatrixBoard(MatchService service, MatrixCase row)
        {
            if (string.Equals(row.CardId, AlAkirHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-control", BoardSide.Player, 0, 100));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-alakir-left", BoardSide.Opponent, 2, 30, Tribe.Beast));
                return;
            }

            if (string.Equals(row.CardId, TavishHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-tavish-target", BoardSide.Player, 1, 20));
                service.State.Player.Board.Add(TestMinion("matrix-player-tavish-spare", BoardSide.Player, 1, 20));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-tavish-source", BoardSide.Opponent, 2, 40));
                return;
            }

            if (string.Equals(row.CardId, DeathwingHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-deathwing", BoardSide.Player, 1, 40));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-deathwing", BoardSide.Opponent, 2, 40));
                return;
            }

            if (string.Equals(row.CardId, YshaarjHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Opponent.TavernTier = 1;
                return;
            }

            if (string.Equals(row.CardId, OnyxiaHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-onyxia-killer", BoardSide.Player, 10, 80));
                for (var index = 0; index < 4; index += 1)
                {
                    service.State.Opponent.Board.Add(TestMinion("matrix-opponent-onyxia-fodder-" + index, BoardSide.Opponent, 1, 1, Tribe.None, Keyword.Taunt));
                }

                return;
            }

            if (string.Equals(row.CardId, BrukanHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-brukan-control", BoardSide.Player, 0, 40));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-brukan-target", BoardSide.Opponent, 2, 40, Tribe.Elemental));
                return;
            }

            if (string.Equals(row.CardId, TamsinHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-tamsin-killer", BoardSide.Player, 8, 80));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-tamsin-low", HarmlessBoneheadCardId, BoardSide.Opponent, 1, 1, Tribe.Undead, Keyword.Taunt, Keyword.Deathrattle));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-tamsin-survivor", BoardSide.Opponent, 4, 60, Tribe.Undead));
                return;
            }

            if (string.Equals(row.CardId, IllidanHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-illidan-defender", BoardSide.Player, 0, 100));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-illidan-left", BoardSide.Opponent, 2, 30, Tribe.Demon));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-illidan-middle", BoardSide.Opponent, 1, 30, Tribe.Pirate));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-illidan-right", BoardSide.Opponent, 3, 30, Tribe.Dragon));
                return;
            }

            if (string.Equals(row.CardId, QueenWagtoggleHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-wagtoggle-control", BoardSide.Player, 0, 100));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-wagtoggle-beast", BoardSide.Opponent, 2, 30, Tribe.Beast));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-wagtoggle-murloc", BoardSide.Opponent, 2, 30, Tribe.Murloc));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-wagtoggle-dragon", BoardSide.Opponent, 2, 30, Tribe.Dragon));
                return;
            }

            if (string.Equals(row.CardId, VanndarHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Round = 7;
                service.State.Player.Board.Add(TestMinion("matrix-player-vanndar-control", BoardSide.Player, 0, 100));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-vanndar-small", BoardSide.Opponent, 1, 20, Tribe.Pirate));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-vanndar-high-health", BoardSide.Opponent, 2, 50, Tribe.Mech));
                return;
            }

            if (string.Equals(row.CardId, DrektharHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Round = 7;
                service.State.Player.Board.Add(TestMinion("matrix-player-drekthar-control", BoardSide.Player, 0, 100));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-drekthar-low", BoardSide.Opponent, 1, 50, Tribe.Pirate));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-drekthar-high-attack", BoardSide.Opponent, 8, 20, Tribe.Mech));
                return;
            }

            if (string.Equals(row.CardId, TeronHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-teron-control", BoardSide.Player, 0, 100));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-teron-target", BoardSide.Opponent, 3, 30, Tribe.Undead));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-teron-spare", BoardSide.Opponent, 2, 30, Tribe.Beast));
                return;
            }

            if (string.Equals(row.CardId, OzumatHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-ozumat-control", BoardSide.Player, 0, 100));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-ozumat-anchor", BoardSide.Opponent, 2, 30, Tribe.Naga));
                return;
            }

            service.State.Player.Board.Add(TestMinion("matrix-player-default-control", BoardSide.Player, 0, 100));
            service.State.Opponent.Board.Add(TestMultiTribeMinion("matrix-opponent-default-all", BoardSide.Opponent, 2, 60, AllPlayableTribes()));
        }

        private static void ConfigureMatrixHeroPowerTarget(MatchService service, MatrixCase row)
        {
            if (string.Equals(row.CardId, TavishHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPowerTarget, BoardSide.Player, 0));
            }
            else if (string.Equals(row.CardId, TeronHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPowerTarget, BoardSide.Opponent, 0));
            }
            else if (string.Equals(row.CardId, BrukanHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                service.Apply(new GameCommand(GameCommandType.SetOpponentHeroPowerElement, "fire"));
            }
        }

        private static void ConfigureTrinketMatrixBoard(MatchService service, MatrixCase row)
        {
            if (ConfigureRemainingTriggerMatrixPreconditions(service, row))
            {
                return;
            }

            if (string.Equals(row.Template, "TR-ATTACK", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-attack-control", BoardSide.Player, 0, 300));
                service.State.Opponent.Board.Add(TestMultiTribeMinion("matrix-opponent-attack-all", BoardSide.Opponent, 4, 120, AllPlayableTribes()));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-attack-quilboar", BoardSide.Opponent, 3, 80, Tribe.Quilboar));
                return;
            }

            if (string.Equals(row.Template, "TR-START-EXTRA", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-start-extra-control", BoardSide.Player, 0, 300));
                service.State.Opponent.Board.Add(TestCardMultiTribeMinion("matrix-opponent-zergling", ZerglingCardId, BoardSide.Opponent, 1, 40, new[] { Tribe.Beast }));
                service.State.Opponent.Board.Add(TestMultiTribeMinion("matrix-opponent-start-extra-all", BoardSide.Opponent, 2, 60, AllPlayableTribes()));
                return;
            }

            if (string.Equals(row.Template, "TR-START-STATS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.Template, "TR-ROUND-GATE", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-start-control", BoardSide.Player, 0, 300));
                service.State.Opponent.Board.Add(TestMultiTribeMinion("matrix-opponent-start-all", BoardSide.Opponent, 2, 60, AllPlayableTribes()));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-start-beast", BoardSide.Opponent, 2, 60, Tribe.Beast));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-start-dragon", BoardSide.Opponent, 2, 60, Tribe.Dragon));
                ConfigureStartStatsMatrixPreconditions(service, row);
                return;
            }

            if (string.Equals(row.Template, "TR-OVERFLOW", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-overflow-killer", BoardSide.Player, 30, 300));
                AddMatrixFodderBoard(service, 7, false);
                return;
            }

            if (string.Equals(row.Template, "TR-SUMMON", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.Template, "TR-COMBAT-REWARD", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-summon-killer", BoardSide.Player, 30, 300));
                AddMatrixFodderBoard(service, 4, true);
                return;
            }

            service.State.Player.Board.Add(TestMinion("matrix-player-death-killer", BoardSide.Player, 30, 300));
            AddMatrixFodderBoard(service, 6, true);
        }

        private static bool ConfigureRemainingTriggerMatrixPreconditions(MatchService service, MatrixCase row)
        {
            if (string.Equals(row.EffectId, "bassgill_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-bassgill-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-bassgill", BassgillCardId, BoardSide.Opponent, 1, 1, Tribe.Murloc, Keyword.Taunt, Keyword.Deathrattle));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-bassgill-survivor", BoardSide.Opponent, 2, 80, Tribe.Murloc));
                service.State.Opponent.Hand.Add(TestMinion("matrix-opponent-hand-murloc", BoardSide.Opponent, 5, 20, Tribe.Murloc));
                return true;
            }

            if (string.Equals(row.EffectId, "divine_signet", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.EffectId, "mechagon_adapter", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-shield-breaker", BoardSide.Player, 1, 300));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-shielded-mech", BoardSide.Opponent, 0, 50, Tribe.Mech, Keyword.DivineShield, Keyword.Taunt));
                return true;
            }

            if (string.Equals(row.EffectId, "blingtrons_sunglasses", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.EffectId, "reinforced_shield", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-mech-summon-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-cord-puller", CordPullerCardId, BoardSide.Opponent, 1, 1, Tribe.Mech, Keyword.Taunt, Keyword.Deathrattle));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-mech-survivor", BoardSide.Opponent, 2, 80, Tribe.Mech));
                return true;
            }

            if (string.Equals(row.EffectId, "slamma_sticker", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.EffectId, "mama_bear_sticker", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-beast-summon-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-forest-rover", ForestRoverCardId, BoardSide.Opponent, 1, 1, Tribe.Beast, Keyword.Taunt, Keyword.Deathrattle));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-beast-survivor", BoardSide.Opponent, 2, 80, Tribe.Beast));
                return true;
            }

            if (string.Equals(row.EffectId, "tiger_carving", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-tiger-defender", BoardSide.Player, 2, 300));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-tiger-damaged", BoardSide.Opponent, 4, 80, Tribe.Beast));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-tiger-buff-target", BoardSide.Opponent, 3, 80, Tribe.Quilboar));
                return true;
            }

            if (string.Equals(row.EffectId, "belcher_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-belcher-defender", BoardSide.Player, 1, 300));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-operatic-belcher", OperaticBelcherCardId, BoardSide.Opponent, 1, 40, Tribe.Murloc, Keyword.Venomous, Keyword.Taunt));
                return true;
            }

            if (string.Equals(row.EffectId, "deathtouch_apple", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-apple-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-reborn-undead", BoardSide.Opponent, 1, 1, Tribe.Undead, Keyword.Reborn, Keyword.Taunt));
                return true;
            }

            if (string.Equals(row.EffectId, "fishy_sticker", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-fishy-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-fishy-bonehead", HarmlessBoneheadCardId, BoardSide.Opponent, 1, 1, Tribe.Undead, Keyword.Taunt, Keyword.Deathrattle));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-fishy-survivor", BoardSide.Opponent, 3, 80, Tribe.Beast));
                return true;
            }

            if (string.Equals(row.EffectId, "flagbearer_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-flagbearer-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-sky-pirate-flagbearer", SkyPirateFlagbearerCardId, BoardSide.Opponent, 1, 1, Tribe.Pirate, Keyword.Taunt, Keyword.Deathrattle));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-flagbearer-survivor", BoardSide.Opponent, 2, 80, Tribe.Pirate));
                return true;
            }

            if (string.Equals(row.EffectId, "vinespeaker_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-vinespeaker-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-vinespeaker-bonehead", HarmlessBoneheadCardId, BoardSide.Opponent, 1, 1, Tribe.Undead, Keyword.Taunt, Keyword.Deathrattle));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-thorned-trailblazer", ThornedTrailblazerCardId, BoardSide.Opponent, 2, 80, Tribe.Quilboar));
                return true;
            }

            if (string.Equals(row.EffectId, "wildfeather_duster", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-wildfeather-killer", BoardSide.Player, 30, 300));
                for (var index = 0; index < 3; index += 1)
                {
                    service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-manasaber-" + index, ManasaberCardId, BoardSide.Opponent, 1, 1, Tribe.Beast, Keyword.Taunt, Keyword.Deathrattle));
                }

                return true;
            }

            if (string.Equals(row.EffectId, "deathly_phylactery", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-phylactery-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-phylactery-bonehead", HarmlessBoneheadCardId, BoardSide.Opponent, 1, 1, Tribe.Undead, Keyword.Taunt, Keyword.Deathrattle));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-phylactery-survivor", BoardSide.Opponent, 2, 80, Tribe.Undead));
                return true;
            }

            if (string.Equals(row.EffectId, "bristlebach_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-bristlebach-defender", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-bristlebach-fodder", BoardSide.Opponent, 1, 1, Tribe.Quilboar, Keyword.Taunt));
                var source = TestCardMinion("matrix-opponent-bristlebach-portrait", BristlebachPortraitMinionCardId, BoardSide.Opponent, 2, 80, Tribe.Quilboar, Keyword.Avenge);
                source.Counters["avenge_threshold"] = 1;
                service.State.Opponent.Board.Add(source);
                service.State.Opponent.Board.Add(TestMultiTribeMinion("matrix-opponent-survivor-all", BoardSide.Opponent, 3, 120, AllPlayableTribes()));
                return true;
            }

            if (string.Equals(row.EffectId, "impulsive_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-impulsive-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-impulsive-trickster", ImpulsiveTricksterCardId, BoardSide.Opponent, 1, 5, Tribe.Demon, Keyword.Taunt, Keyword.Deathrattle));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-impulsive-neighbor", BoardSide.Opponent, 2, 80, Tribe.Demon));
                return true;
            }

            if (string.Equals(row.EffectId, "kaboom_bot_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-kaboom-target", BoardSide.Player, 1, 50));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-kaboom-bot", KaboomBotCardId, BoardSide.Opponent, 1, 1, Tribe.Mech, Keyword.Taunt, Keyword.Deathrattle));
                return true;
            }

            if (string.Equals(row.EffectId, "rylak_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-rylak-control", BoardSide.Player, 0, 300));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-heavy-metal-wyrm", HeavyMetalWyrmCardId, BoardSide.Opponent, 2, 80, Tribe.Dragon, Keyword.Deathrattle));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-rylak-neighbor", BoardSide.Opponent, 2, 80, Tribe.Dragon));
                return true;
            }

            if (string.Equals(row.EffectId, "scrapsmith_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-scrapsmith-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-taunt-for-scrapsmith", BoardSide.Opponent, 1, 1, Tribe.Quilboar, Keyword.Taunt));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-bristleback-scrapsmith", BristlebackScrapSmithCardId, BoardSide.Opponent, 2, 80, Tribe.Quilboar));
                return true;
            }

            if (string.Equals(row.EffectId, "tarecgosa_sticker", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-tarecgosa-sticker-defender", BoardSide.Player, 1, 300));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-tarecgosa-fodder", BoardSide.Opponent, 1, 1, Tribe.Quilboar, Keyword.Taunt));
                var source = TestCardMinion("matrix-opponent-tarecgosa-bristlebach", BristlebachPortraitMinionCardId, BoardSide.Opponent, 2, 80, Tribe.Quilboar, Keyword.Avenge);
                source.Counters["avenge_threshold"] = 1;
                service.State.Opponent.Board.Add(source);
                service.State.Opponent.Board.Add(TestMultiTribeMinion("matrix-opponent-tarecgosa-sticker-dragon", BoardSide.Opponent, 2, 100, new[] { Tribe.Dragon, Tribe.Quilboar }));
                return true;
            }

            if (string.Equals(row.EffectId, "tide_raiser_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-tide-raiser-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-tide-raiser", TideRaiserCardId, BoardSide.Opponent, 1, 1, Tribe.Naga, Keyword.Taunt, Keyword.Deathrattle));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-tide-raiser-neighbor", BoardSide.Opponent, 2, 80, Tribe.Naga));
                return true;
            }

            if (string.Equals(row.EffectId, "eye_of_dalaran", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Player.Board.Add(TestMinion("matrix-player-eye-killer", BoardSide.Player, 30, 300));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-typeless-eye-target", BoardSide.Opponent, 1, 1, Tribe.None, Keyword.Taunt));
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-eye-survivor", BoardSide.Opponent, 2, 80, Tribe.Dragon));
                return true;
            }

            return false;
        }

        private static void ConfigureStartStatsMatrixPreconditions(MatchService service, MatrixCase row)
        {
            if (string.Equals(row.EffectId, "dramaloc_sticker", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.EffectId, "tinyfin_onesie", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.EffectId, "crocheted_sungill", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Opponent.Hand.Add(TestMinion("matrix-opponent-hand-heavy", BoardSide.Opponent, 9, 90, Tribe.Murloc));
            }

            if (string.Equals(row.EffectId, "emerald_dreamcatcher", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-start-high-pirate", BoardSide.Opponent, 8, 60, Tribe.Pirate));
            }

            if (string.Equals(row.EffectId, "ironforge_anvil", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Opponent.Board.Add(TestMinion("matrix-opponent-start-typeless", BoardSide.Opponent, 2, 60));
            }

            if (string.Equals(row.EffectId, "rivendare_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-start-titus", TitusRivendareCardId, BoardSide.Opponent, 2, 60));
            }

            if (string.Equals(row.EffectId, "eternal_portrait", StringComparison.OrdinalIgnoreCase))
            {
                service.State.Opponent.Board.Add(TestCardMinion("matrix-opponent-start-eternal", EternalKnightCardId, BoardSide.Opponent, 2, 60, Tribe.Undead));
            }
        }

        private static void AddMatrixFodderBoard(MatchService service, int fodderCount, bool addSurvivor)
        {
            var tribes = AllPlayableTribes();
            for (var index = 0; index < fodderCount && service.State.Opponent.Board.Count < 7; index += 1)
            {
                var cardId = index % 2 == 0 ? HarmlessBoneheadCardId : ForestRoverCardId;
                var tribe = tribes[index % tribes.Count];
                service.State.Opponent.Board.Add(TestCardMultiTribeMinion(
                    "matrix-opponent-fodder-" + index,
                    cardId,
                    BoardSide.Opponent,
                    1,
                    1,
                    new[] { tribe },
                    Keyword.Taunt,
                    Keyword.Deathrattle));
            }

            if (addSurvivor && service.State.Opponent.Board.Count < 7)
            {
                service.State.Opponent.Board.Add(TestMultiTribeMinion(
                    "matrix-opponent-survivor-all",
                    BoardSide.Opponent,
                    3,
                    120,
                    tribes));
            }
        }

        private static int MatrixSafetyLimit(MatrixCase row)
        {
            if (row.Kind == MatrixCaseKind.HeroPower &&
                string.Equals(row.CardId, OnyxiaHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                return 10;
            }

            if (row.Kind == MatrixCaseKind.Trinket &&
                (string.Equals(row.Template, "TR-AVENGE-BUFF", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(row.Template, "TR-AVENGE-SUMMON", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(row.Template, "TR-AVENGE-REWARD", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(row.Template, "TR-DEATH", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(row.Template, "TR-SUMMON", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(row.Template, "TR-COMBAT-REWARD", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(row.Template, "TR-OVERFLOW", StringComparison.OrdinalIgnoreCase)))
            {
                return 24;
            }

            return 6;
        }

        private static void AssertMatrixCombatCompleted(MatchService service, MatrixCase row)
        {
            Assert.IsNotNull(service.State.LastReplay, row.CaseId + " did not produce LastReplay.");
            Assert.IsNotNull(service.State.LastResult, row.CaseId + " did not produce LastResult.");
            Assert.IsTrue(
                service.State.LastReplay.Frames.Any(frame => frame.EventType == CombatEventType.CombatStarted),
                row.CaseId + " did not record CombatStarted.");
            Assert.IsTrue(
                service.State.LastReplay.Frames.Any(frame => frame.EventType == CombatEventType.CombatEnded),
                row.CaseId + " did not record CombatEnded.");
        }

        private static void AssertHeroPowerMatrixObservation(MatchService service, MatrixCase row)
        {
            if (string.Equals(row.CardId, AlAkirHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                var minion = InitialOpponent(service, "matrix-opponent-alakir-left");
                Assert.IsTrue(minion.Keywords.Contains(Keyword.Windfury));
                Assert.IsTrue(minion.Keywords.Contains(Keyword.DivineShield));
                Assert.IsTrue(minion.Keywords.Contains(Keyword.Taunt));
                return;
            }

            if (string.Equals(row.CardId, TavishHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                var target = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == "matrix-player-tavish-target");
                Assert.Less(target.Health, target.MaxHealth);
                Assert.IsTrue(service.State.CombatLog.Any(entry => entry.Title == "HeroStartOfCombat" && ContainsText(entry.Detail, "Deadeye")));
                return;
            }

            if (string.Equals(row.CardId, DeathwingHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(3, InitialPlayer(service, "matrix-player-deathwing").Attack);
                Assert.AreEqual(4, InitialOpponent(service, "matrix-opponent-deathwing").Attack);
                return;
            }

            if (string.Equals(row.CardId, YshaarjHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(1, service.State.LastReplay.InitialSnapshot.Opponent.Minions.Count);
                Assert.AreEqual(1, service.State.Opponent.Hand.Count);
                Assert.IsEmpty(service.State.Player.Tavern.Hand);
                return;
            }

            if (string.Equals(row.CardId, OnyxiaHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(service.State.CombatLog.Any(entry =>
                    entry.Title == "ImmediateAttackQueued" &&
                    ContainsText(entry.Detail, "Broodmother")));
                Assert.IsTrue(service.State.LastReplay.Frames.Any(frame =>
                    frame.EventType == CombatEventType.MinionSummoned &&
                    frame.ActorSide == BoardSide.Opponent &&
                    ContainsText(frame.TargetId, "BG22_HERO_305t")));
                return;
            }

            if (string.Equals(row.CardId, BrukanHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(4, service.State.LastResult.FinalOpponentBoard.Single(card => card.InstanceId == "matrix-opponent-brukan-target").Attack);
                Assert.IsTrue(service.State.CombatLog.Any(entry =>
                    entry.Title == "HeroStartOfCombat" &&
                    ContainsText(entry.Detail, "Embrace the Elements called fire")));
                return;
            }

            if (string.Equals(row.CardId, TamsinHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                var initial = InitialOpponent(service, "matrix-opponent-tamsin-low");
                Assert.IsTrue(initial.Keywords.Contains(Keyword.Deathrattle));
                Assert.IsTrue(initial.Tags.Any(tag => ContainsText(tag, "tamsin")));
                return;
            }

            if (string.Equals(row.CardId, IllidanHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                Assert.Greater(InitialOpponent(service, "matrix-opponent-illidan-left").Attack, 2);
                Assert.Greater(InitialOpponent(service, "matrix-opponent-illidan-right").Attack, 3);
                Assert.IsTrue(service.State.LastReplay.Frames.Any(frame =>
                    frame.EventType == CombatEventType.ImmediateAttackQueued &&
                    frame.ActorSide == BoardSide.Opponent &&
                    ContainsText(frame.LogText, "Wingmen")));
                return;
            }

            if (string.Equals(row.CardId, QueenWagtoggleHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(service.State.LastReplay.InitialSnapshot.Opponent.Minions.Any(minion =>
                    minion.InstanceId.StartsWith("matrix-opponent-wagtoggle", StringComparison.Ordinal) &&
                    minion.Attack > minion.BaseAttack &&
                    minion.MaxHealth > minion.BaseHealth));
                return;
            }

            if (string.Equals(row.CardId, VanndarHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(service.State.LastReplay.InitialSnapshot.Opponent.Minions.Any(minion =>
                    ContainsText(minion.InstanceId, "opponent-vanndar-combat-copy")));
                return;
            }

            if (string.Equals(row.CardId, DrektharHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(service.State.LastReplay.InitialSnapshot.Opponent.Minions.Any(minion =>
                    ContainsText(minion.InstanceId, "opponent-drekthar-combat-copy")));
                return;
            }

            if (string.Equals(row.CardId, TeronHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(service.State.LastReplay.InitialSnapshot.Opponent.Minions.Any(minion =>
                    ContainsText(minion.InstanceId, "teron-reanimated")));
                return;
            }

            if (string.Equals(row.CardId, OzumatHeroPowerCardId, StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(service.State.LastReplay.InitialSnapshot.Opponent.Minions.Any(minion =>
                    string.Equals(minion.CardId, "OZUMAT_TENTACLE", StringComparison.OrdinalIgnoreCase) &&
                    minion.Keywords.Contains(Keyword.Taunt)));
                return;
            }

            Assert.IsTrue(HasOpponentPublicSignal(service, row), row.CaseId + " did not expose a public opponent-side Hero Power signal.");
        }

        private static void AssertTrinketMatrixObservation(MatchService service, MatrixCase row)
        {
            if (string.Equals(row.EffectId, "tide_raiser_portrait", StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(
                    service.State.LastReplay.Frames.Any(frame =>
                        frame.EventType == CombatEventType.CombatSpellCast &&
                        frame.ActorSide == BoardSide.Opponent &&
                        string.Equals(frame.ActorId, TideRaiserCardId, StringComparison.OrdinalIgnoreCase)),
                    row.CaseId + " did not expose opponent Tide Raiser combat spell casting.");
                return;
            }

            if (string.Equals(row.EffectId, "tarecgosa_sticker", StringComparison.OrdinalIgnoreCase))
            {
                var dragon = service.State.Opponent.Board.FirstOrDefault(minion =>
                    minion.InstanceId == "matrix-opponent-tarecgosa-sticker-dragon");
                Assert.IsNotNull(dragon, row.CaseId + " lost the configured opponent edge Dragon.");
                Assert.IsTrue(
                    dragon.Attack > 2 || dragon.MaxHealth > 100,
                    row.CaseId + " did not persist combat buffs onto the opponent edge Dragon.");
                return;
            }

            if (string.Equals(row.Template, "TR-START-STATS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.Template, "TR-ROUND-GATE", StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(
                    OpponentStatsChanged(service) ||
                    OpponentInitialBoardExpanded(service) ||
                    OpponentInitialKeywordsChanged(service) ||
                    HasOpponentPublicSignal(service, row),
                    row.CaseId + " did not expose opponent start-of-combat stats or a row-specific public signal.");
                return;
            }

            if (string.Equals(row.Template, "TR-START-EXTRA", StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(
                    service.State.LastReplay.InitialSnapshot.Opponent.Minions.Count >= 4 ||
                    HasOpponentPublicSignal(service, row),
                    row.CaseId + " did not expose an extra opponent Start of Combat trigger.");
                return;
            }

            if (string.Equals(row.Template, "TR-AVENGE-BUFF", StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(
                    HasOpponentPublicSignal(service, row) ||
                    HasOpponentAvengeFrame(service) && OpponentFinalSurvivorBuffed(service),
                    row.CaseId + " did not expose an opponent Avenge buff.");
                return;
            }

            if (string.Equals(row.Template, "TR-AVENGE-SUMMON", StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(
                    HasOpponentPublicSignal(service, row),
                    row.CaseId + " did not expose a row-specific opponent Avenge summon signal.");
                return;
            }

            if (string.Equals(row.Template, "TR-AVENGE-REWARD", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.Template, "TR-COMBAT-REWARD", StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(
                    service.State.LastResult.OpponentRewards.Any(reward =>
                        string.Equals(reward.SourceCardId, row.CardId, StringComparison.OrdinalIgnoreCase)) ||
                    HasOpponentPublicSignal(service, row),
                    row.CaseId + " did not expose an opponent reward queued from this Trinket.");
                return;
            }

            if (string.Equals(row.Template, "TR-ATTACK", StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(
                    HasOpponentPublicSignal(service, row),
                    row.CaseId + " did not expose a row-specific opponent attack-trigger signal.");
                return;
            }

            if (string.Equals(row.Template, "TR-SUMMON", StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(
                    HasOpponentPublicSignal(service, row),
                    row.CaseId + " did not expose a row-specific opponent summon-trigger signal.");
                return;
            }

            if (string.Equals(row.Template, "TR-DEATH", StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(
                    HasOpponentPublicSignal(service, row),
                    row.CaseId + " did not expose a row-specific opponent death/deathrattle signal.");
                return;
            }

            if (string.Equals(row.Template, "TR-OVERFLOW", StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(
                    service.State.LastReplay.Frames.Any(frame =>
                        frame.EventType == CombatEventType.SummonOverflowed &&
                        frame.ActorSide == BoardSide.Opponent) &&
                    HasOpponentPublicSignal(service, row),
                    row.CaseId + " did not expose an opponent overflow Trinket signal.");
                return;
            }

            Assert.IsTrue(HasOpponentPublicSignal(service, row), row.CaseId + " did not expose a row-specific public Trinket signal.");
        }

        private static bool HasOpponentPublicSignal(MatchService service, MatrixCase row)
        {
            return service.State.CombatLog.Any(entry =>
                    ContainsMechanicText(entry.Title, row) ||
                    ContainsMechanicText(entry.Detail, row) ||
                    ContainsMechanicText(entry.ActorId, row) ||
                    ContainsMechanicText(entry.TargetId, row)) ||
                service.State.LastReplay.Frames.Any(frame =>
                    frame.ActorSide == BoardSide.Opponent &&
                    (ContainsMechanicText(frame.ActorId, row) ||
                     ContainsMechanicText(frame.TargetId, row) ||
                     ContainsMechanicText(frame.LogText, row) ||
                     ContainsMechanicText(frame.RelatedEntityIds, row) ||
                     ContainsMechanicText(frame.TriggerSourceIds, row) ||
                     ContainsMechanicText(frame.SummonedEntityIds, row) ||
                     ContainsMechanicText(frame.OverflowedEntityIds, row))) ||
                service.State.LastResult.OpponentRewards.Any(reward =>
                    ContainsMechanicText(reward.SourceCardId, row) ||
                    ContainsMechanicText(reward.SourceInstanceId, row) ||
                    ContainsMechanicText(reward.TargetInstanceId, row) ||
                    ContainsMechanicText(reward.CardId, row));
        }

        private static bool OpponentStatsChanged(MatchService service)
        {
            return service.State.LastReplay.InitialSnapshot.Opponent.Minions.Any(StatsChanged) ||
                service.State.LastResult.FinalOpponentBoard.Any(card =>
                    card.InstanceId.StartsWith("matrix-opponent", StringComparison.Ordinal) &&
                    (card.Attack != card.BaseAttack || card.MaxHealth != card.BaseHealth));
        }

        private static bool StatsChanged(CombatMinionSnapshot minion)
        {
            return minion.InstanceId.StartsWith("matrix-opponent", StringComparison.Ordinal) &&
                (minion.Attack != minion.BaseAttack || minion.MaxHealth != minion.BaseHealth);
        }

        private static bool OpponentInitialBoardExpanded(MatchService service)
        {
            return service.State.LastReplay.InitialSnapshot.Opponent.Minions.Count > service.State.Opponent.Board.Count;
        }

        private static bool OpponentInitialKeywordsChanged(MatchService service)
        {
            var before = service.State.Opponent.Board
                .Where(minion => minion != null)
                .ToDictionary(minion => minion.InstanceId, minion => minion.Keywords ?? new List<Keyword>(), StringComparer.Ordinal);
            return service.State.LastReplay.InitialSnapshot.Opponent.Minions.Any(minion =>
                minion.InstanceId.StartsWith("matrix-opponent", StringComparison.Ordinal) &&
                before.TryGetValue(minion.InstanceId, out var originalKeywords) &&
                minion.Keywords.Any(keyword => !originalKeywords.Contains(keyword)));
        }

        private static bool HasOpponentAvengeFrame(MatchService service)
        {
            return service.State.LastReplay.Frames.Any(frame =>
                frame.EventType == CombatEventType.AvengeCounterUpdated &&
                frame.ActorSide == BoardSide.Opponent);
        }

        private static bool OpponentFinalSurvivorBuffed(MatchService service)
        {
            var survivor = service.State.LastResult.FinalOpponentBoard.FirstOrDefault(card =>
                card.InstanceId == "matrix-opponent-survivor-all");
            return survivor != null && (survivor.Attack > 3 || survivor.MaxHealth > 120);
        }

        private static CombatMinionSnapshot InitialPlayer(MatchService service, string instanceId)
        {
            return service.State.LastReplay.InitialSnapshot.Player.Minions.Single(minion => minion.InstanceId == instanceId);
        }

        private static CombatMinionSnapshot InitialOpponent(MatchService service, string instanceId)
        {
            return service.State.LastReplay.InitialSnapshot.Opponent.Minions.Single(minion => minion.InstanceId == instanceId);
        }

        private static bool ContainsMechanicText(IEnumerable<string> values, MatrixCase row)
        {
            return values != null && values.Any(value => ContainsMechanicText(value, row));
        }

        private static bool ContainsMechanicText(string value, MatrixCase row)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return ContainsText(value, row.CardId) ||
                ContainsText(value, row.EffectId) ||
                ContainsText(value, row.Name) ||
                ContainsText(NormalizeToken(value), NormalizeToken(row.EffectId)) ||
                ContainsText(NormalizeToken(value), NormalizeToken(row.Name));
        }

        private static bool ContainsText(string value, string expected)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                !string.IsNullOrWhiteSpace(expected) &&
                value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return new string(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static MatchService CreateFullMatrixService()
        {
            return MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    ActiveTribes = AllPlayableTribes(),
                    ShowDebugOnly = true,
                    ShowHiddenEffectOnly = true,
                    ShowDisabled = true
                });
        }

        private static MatchService CreateService()
        {
            return MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    ActiveTribes = new List<Tribe> { Tribe.Beast, Tribe.Dragon, Tribe.Murloc },
                    ShowDebugOnly = true,
                    ShowHiddenEffectOnly = true
                });
        }

        private static MechanicChoiceRequest PendingChoice(AdvancedMechanicKind kind)
        {
            return new MechanicChoiceRequest
            {
                RequestId = "blackbox-pending",
                Kind = kind,
                Source = "blackbox",
                Slot = "Main",
                Round = 1
            };
        }

        private static MinionInstance TestCardMinion(
            string instanceId,
            string cardId,
            BoardSide owner,
            int attack,
            int health,
            Tribe tribe = Tribe.None,
            params Keyword[] keywords)
        {
            var minion = TestMinion(instanceId, owner, attack, health, tribe, keywords);
            minion.DefinitionId = cardId;
            minion.CardId = cardId;
            return minion;
        }

        private static MinionInstance TestCardMultiTribeMinion(
            string instanceId,
            string cardId,
            BoardSide owner,
            int attack,
            int health,
            IEnumerable<Tribe> tribes,
            params Keyword[] keywords)
        {
            var minion = TestMultiTribeMinion(instanceId, owner, attack, health, tribes, keywords);
            minion.DefinitionId = cardId;
            minion.CardId = cardId;
            return minion;
        }

        private static MinionInstance TestMultiTribeMinion(
            string instanceId,
            BoardSide owner,
            int attack,
            int health,
            IEnumerable<Tribe> tribes,
            params Keyword[] keywords)
        {
            var minion = TestMinion(instanceId, owner, attack, health, Tribe.None, keywords);
            minion.Tribes = tribes == null ? new List<Tribe> { Tribe.None } : tribes.Distinct().ToList();
            if (minion.Tribes.Count == 0)
            {
                minion.Tribes.Add(Tribe.None);
            }

            return minion;
        }

        private static MinionInstance TestMinion(
            string instanceId,
            BoardSide owner,
            int attack,
            int health,
            Tribe tribe = Tribe.None,
            params Keyword[] keywords)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                Cost = 3,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Owner = owner,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                OfficialKeywords = keywords.ToList(),
                PoolSource = PoolSource.Debug,
                PoolCopiesHeld = 0,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>()
            };
        }

        private static List<Tribe> AllPlayableTribes()
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
                Tribe.Naga
            };
        }
    }
}
