using System;
using System.Collections.Generic;
using System.IO;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TestScenarioMapperTests
    {
        [Test]
        public void CaptureAndApply_RoundTripsEditedBoardsHandAndTavernState()
        {
            var service = MatchService.CreateWithDefaultCatalog(2468, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Tier = 4;
            service.State.Player.Tavern.Gold = 7;
            service.State.Player.Tavern.MaxGold = 9;
            service.State.Player.Tavern.UpgradeCost = 6;
            service.State.Player.Tavern.TavernSpellsCastThisGame = 6;
            service.State.Player.Tavern.BloodGemBonusAttack = 2;
            service.State.Player.Tavern.BloodGemBonusHealth = 1;
            service.State.Opponent.CombatModifiers.BloodGemAttackBonus = 4;
            service.State.Opponent.CombatModifiers.UndeadAttackBonus = 5;
            service.State.Opponent.CombatModifiers.AstralAutomatonSummons = 3;
            service.State.Round = 5;
            service.State.Phase = MatchPhase.Result;

            var player = service.State.Player.Tavern.Shop[0].Clone();
            player.InstanceId = "player-edited";
            player.Attack = 12;
            player.Health = 6;
            player.MaxHealth = 8;
            player.Golden = true;
            player.Keywords.Add(Keyword.DivineShield);
            player.Counters["test"] = 3;
            service.State.Player.Board.Add(player);

            var hand = service.State.Player.Tavern.Shop[1].Clone();
            hand.InstanceId = "hand-edited";
            service.State.Player.Tavern.Hand.Add(hand);

            var opponentHand = service.State.Player.Tavern.Shop[1].Clone();
            opponentHand.InstanceId = "opponent-hand-edited";
            opponentHand.Owner = BoardSide.Opponent;
            service.State.Opponent.Hand.Add(opponentHand);

            var opponent = service.State.Player.Tavern.Shop[2].Clone();
            opponent.InstanceId = "opponent-edited";
            opponent.Owner = BoardSide.Opponent;
            opponent.Keywords.Add(Keyword.Taunt);
            service.State.Opponent.Board.Add(opponent);

            var scenario = TestScenarioMapper.Capture(service.State, "edited");
            var target = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository()).State;

            TestScenarioMapper.ApplyTo(target, scenario);

            Assert.AreEqual(5, target.Round);
            Assert.AreEqual(2468, target.Seed);
            Assert.AreEqual(MatchPhase.Result, target.Phase);
            Assert.AreEqual(4, target.Player.Tavern.Tier);
            Assert.AreEqual(7, target.Player.Tavern.Gold);
            Assert.AreEqual(9, target.Player.Tavern.MaxGold);
            Assert.AreEqual(6, target.Player.Tavern.UpgradeCost);
            Assert.AreEqual(1, target.Player.Board.Count);
            Assert.AreEqual("player-edited", target.Player.Board[0].InstanceId);
            Assert.AreEqual(12, target.Player.Board[0].Attack);
            Assert.AreEqual(6, target.Player.Board[0].Health);
            Assert.IsTrue(target.Player.Board[0].Golden);
            Assert.IsTrue(target.Player.Board[0].Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(3, target.Player.Board[0].Counters["test"]);
            Assert.AreEqual(1, target.Player.Tavern.Hand.Count);
            Assert.AreEqual("hand-edited", target.Player.Tavern.Hand[0].InstanceId);
            Assert.AreEqual(1, target.Opponent.Hand.Count);
            Assert.AreEqual("opponent-hand-edited", target.Opponent.Hand[0].InstanceId);
            Assert.AreEqual(BoardSide.Opponent, target.Opponent.Hand[0].Owner);
            Assert.AreEqual(1, target.Opponent.Board.Count);
            Assert.AreEqual(BoardSide.Opponent, target.Opponent.Board[0].Owner);
            Assert.IsTrue(target.Opponent.Board[0].Keywords.Contains(Keyword.Taunt));
            Assert.AreEqual(6, target.Player.CombatModifiers.SpellsCastThisGame);
            Assert.AreEqual(2, target.Player.CombatModifiers.BloodGemAttackBonus);
            Assert.AreEqual(1, target.Player.CombatModifiers.BloodGemHealthBonus);
            Assert.AreEqual(4, target.Opponent.CombatModifiers.BloodGemAttackBonus);
            Assert.AreEqual(5, target.Opponent.CombatModifiers.UndeadAttackBonus);
            Assert.AreEqual(3, target.Opponent.CombatModifiers.AstralAutomatonSummons);
            Assert.AreNotSame(player, target.Player.Board[0]);
        }

        [Test]
        public void ApplyTo_ClampsInvalidHealthValues()
        {
            var scenario = new TestScenarioDefinition
            {
                Name = "clamp",
                SavedAtRound = 1,
                Seed = 1,
                Tavern = new ScenarioTavernState { Tier = 1, Gold = 3, MaxGold = 3, UpgradeCost = 5 },
                PlayerBoard = new List<ScenarioCardState>
                {
                    new ScenarioCardState
                    {
                        CardKind = CardKind.Minion,
                        InstanceId = "bad-health",
                        DefinitionId = "bad",
                        CardId = "BAD",
                        Name = "bad",
                        Attack = -2,
                        Health = 99,
                        MaxHealth = -5,
                        Tribes = new List<Tribe> { Tribe.None },
                        Keywords = new List<Keyword>()
                    }
                }
            };
            var target = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository()).State;

            TestScenarioMapper.ApplyTo(target, scenario);

            Assert.AreEqual(0, target.Player.Board[0].Attack);
            Assert.AreEqual(1, target.Player.Board[0].MaxHealth);
            Assert.AreEqual(1, target.Player.Board[0].Health);
        }

        [Test]
        public void Migration_V1Fixture_PassesThroughV2AndFreezesLegacyV3Defaults()
        {
            var scenario = LoadFixture("BattleTestLoopV1.json");

            var migrated = TestScenarioMigration.MigrateToCurrent(scenario);

            Assert.AreEqual(TestScenarioMigration.CurrentVersion, migrated.Version);
            Assert.AreEqual(TestScenarioMigration.CurrentSchemaVersion, migrated.SchemaVersion);
            Assert.AreEqual(GameVersionIds.LegacyCompositeSandbox, migrated.GameVersionId);
            Assert.AreEqual(RulesetIds.LegacyCompositeSandbox, migrated.RulesetId);
            Assert.AreEqual(1, migrated.RulesetRevision);
            Assert.IsEmpty(migrated.ContentSnapshotId);
            Assert.IsEmpty(migrated.ContentFingerprint);
            Assert.IsFalse(migrated.IsStateTemplate);
            Assert.IsTrue(migrated.PlayerCombatModifiersAreAuthoritative);
            Assert.AreEqual(5, migrated.PlayerCombatModifiers.TavernSpellBonusAttack);
            Assert.AreEqual(3, migrated.PlayerCombatModifiers.UndeadAttackBonus);
            Assert.AreEqual(2, migrated.PlayerCombatModifiers.EternalKnightDeaths);
            AssertDefaultMechanicSlots(migrated);
        }

        [Test]
        public void Migration_V2Fixture_BecomesUnprovenLegacyRatherThanSeason14()
        {
            var scenario = LoadFixture("BattleTestLoopV2.json");

            var migrated = TestScenarioMigration.MigrateToCurrent(scenario);

            Assert.AreEqual(TestScenarioMigration.CurrentVersion, migrated.Version);
            Assert.AreEqual(TestScenarioMigration.CurrentSchemaVersion, migrated.SchemaVersion);
            Assert.AreEqual(GameVersionIds.LegacyCompositeSandbox, migrated.GameVersionId);
            Assert.AreEqual(RulesetIds.LegacyCompositeSandbox, migrated.RulesetId);
            Assert.AreNotEqual(GameVersionIds.Season14Preview, migrated.GameVersionId);
            Assert.IsEmpty(migrated.ContentSnapshotId);
            Assert.IsEmpty(migrated.ContentFingerprint);
            Assert.IsFalse(migrated.ResolvedCardPool.IsComplete);
            AssertDefaultMechanicSlots(migrated);
        }

        [Test]
        public void CaptureJsonRepositoryApply_RoundTripsVersionPoolsAdvancedMechanicsAndRng()
        {
            var directory = TempDirectory("roundtrip");
            try
            {
                var source = MatchService.CreateWithDefaultCatalog(2468, new InMemoryTestScenarioRepository()).State;
                source.Player.Tavern.AdvancedMechanics.PendingChoice = new MechanicChoiceRequest
                {
                    RequestId = "quest-choice-1",
                    Kind = AdvancedMechanicKind.Quest,
                    Source = "test",
                    Slot = "main",
                    Round = 5,
                    RemainingPicks = 1,
                    Options = new List<MechanicChoiceOption>
                    {
                        new MechanicChoiceOption { OptionId = "option-1", Kind = AdvancedMechanicKind.Quest, SourceId = "quest-1" }
                    }
                };
                source.Player.Tavern.AdvancedMechanics.Equipped.Add(new EquippedAdvancedMechanic
                {
                    Kind = AdvancedMechanicKind.Trinket,
                    SourceId = "trinket-1",
                    DisplayName = "Test Trinket",
                    Slot = "lesser",
                    EquippedRound = 4,
                    CostPaid = 2,
                    ImplementationStatus = "Implemented"
                });
                source.Player.Tavern.AdvancedMechanics.Counters["player-counter"] = 7;
                source.Player.Tavern.AdvancedMechanics.Selections["player-selection"] = "selected";
                source.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId = "trinket-1";
                source.Player.Tavern.AdvancedMechanics.Quests.MainQuest = new ActiveQuestState
                {
                    QuestId = "quest-1",
                    QuestCardId = "QUEST_CARD_1",
                    RewardId = "reward-1",
                    Progress = 3,
                    RequiredAmount = 6
                };
                source.Player.Tavern.AdvancedMechanics.Quests.RewardCounters["quest-counter"] = 2;
                source.Player.Tavern.AdvancedMechanics.Quests.RewardFlags["quest-flag"] = true;
                source.Player.Tavern.AdvancedMechanics.Anomalies.Enabled = true;
                source.Player.Tavern.AdvancedMechanics.Anomalies.ActiveAnomalyId = "anomaly-1";
                source.Player.Tavern.AdvancedMechanics.Anomalies.Counters["anomaly-counter"] = 4;
                source.Player.Tavern.AdvancedMechanics.Anomalies.Flags["anomaly-flag"] = "active";
                source.Opponent.AdvancedMechanics.Counters["opponent-counter"] = 9;
                source.Player.Tavern.RecruitLog.Add(new RecruitLogEntry
                {
                    Seq = 1,
                    Round = source.Round,
                    Type = RecruitLogType.Play,
                    Message = "rng cursor",
                    GoldBefore = 3,
                    GoldAfter = 3
                });

                var repository = new FileTestScenarioRepository(directory);
                repository.Save(TestScenarioMapper.Capture(source, "round-trip"));
                var json = File.ReadAllText(Path.Combine(directory, "round-trip.json"));
                var loaded = repository.Load("round-trip");
                var target = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository()).State;

                var result = TestScenarioMapper.TryApplyTo(target, loaded);

                StringAssert.Contains("battle-test-loop-v3", json);
                Assert.AreEqual(TestScenarioRestoreStatus.Applied, result.Status, result.Message);
                Assert.AreEqual(source.GameVersionId, target.GameVersionId);
                Assert.AreEqual(source.RulesetId, target.RulesetId);
                Assert.AreEqual(source.ContentSnapshotId, target.ContentSnapshotId);
                Assert.AreEqual(source.ContentFingerprint, target.ContentFingerprint);
                Assert.AreEqual(source.CardPoolVersionId, target.CardPoolVersionId);
                CollectionAssert.AreEqual(source.ActiveTribes, target.ActiveTribes);
                CollectionAssert.AreEqual(source.EnabledMinionCardIds, target.EnabledMinionCardIds);
                CollectionAssert.AreEqual(source.EnabledTavernSpellCardNumbers, target.EnabledTavernSpellCardNumbers);
                Assert.AreEqual("quest-choice-1", target.Player.Tavern.AdvancedMechanics.PendingChoice.RequestId);
                Assert.AreEqual(7, target.Player.Tavern.AdvancedMechanics.Counters["player-counter"]);
                Assert.AreEqual("selected", target.Player.Tavern.AdvancedMechanics.Selections["player-selection"]);
                Assert.AreEqual("trinket-1", target.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId);
                Assert.AreEqual("quest-1", target.Player.Tavern.AdvancedMechanics.Quests.MainQuest.QuestId);
                Assert.AreEqual(2, target.Player.Tavern.AdvancedMechanics.Quests.RewardCounters["quest-counter"]);
                Assert.IsTrue(target.Player.Tavern.AdvancedMechanics.Quests.RewardFlags["quest-flag"]);
                Assert.AreEqual(4, target.Player.Tavern.AdvancedMechanics.Anomalies.Counters["anomaly-counter"]);
                Assert.AreEqual("active", target.Player.Tavern.AdvancedMechanics.Anomalies.Flags["anomaly-flag"]);
                Assert.AreEqual(9, target.Opponent.AdvancedMechanics.Counters["opponent-counter"]);
                Assert.AreEqual(1, target.Player.Tavern.RecruitLog.Count);
                Assert.AreEqual(1, loaded.RngState.RecruitLogCursor);
                AssertDefaultMechanicSlots(loaded);
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Test]
        public void TryApplyTo_UnprovenOrDifferentSnapshot_ReturnsBlockingResultWithoutMutation()
        {
            var target = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository()).State;
            var originalRound = target.Round;
            var migrated = TestScenarioMigration.MigrateToCurrent(LoadFixture("BattleTestLoopV2.json"));
            migrated.SavedAtRound = 9;

            var missing = TestScenarioMapper.TryApplyTo(target, migrated);

            Assert.AreEqual(TestScenarioRestoreStatus.MissingContentSnapshot, missing.Status);
            Assert.AreEqual(originalRound, target.Round);

            var exact = TestScenarioMapper.Capture(target, "fingerprint-mismatch");
            exact.SavedAtRound = 10;
            exact.ContentFingerprint = "different-fingerprint";

            var mismatch = TestScenarioMapper.TryApplyTo(target, exact);

            Assert.AreEqual(TestScenarioRestoreStatus.ContentSnapshotMismatch, mismatch.Status);
            Assert.AreEqual(originalRound, target.Round);
        }

        [Test]
        public void FileRepository_LoadingV2PreservesSourceAndWritesMigratedCopy()
        {
            var directory = TempDirectory("migration-copy");
            try
            {
                var sourcePath = Path.Combine(directory, "legacy-source.json");
                File.Copy(FixturePath("BattleTestLoopV2.json"), sourcePath);
                var original = File.ReadAllText(sourcePath);
                var repository = new FileTestScenarioRepository(directory);

                var loaded = repository.Load("legacy-source");

                var migratedPath = Path.Combine(directory, "legacy-source-migrated-v3.json");
                Assert.AreEqual(original, File.ReadAllText(sourcePath));
                Assert.IsTrue(File.Exists(migratedPath));
                Assert.AreEqual("legacy-source-migrated-v3", loaded.Name);
                Assert.AreEqual(TestScenarioMigration.CurrentVersion, loaded.Version);
                Assert.AreEqual(TestScenarioMigration.CurrentVersion, JsonUtility.FromJson<TestScenarioDefinition>(File.ReadAllText(migratedPath)).Version);
                CollectionAssert.Contains(repository.ListScenarioNames(), "legacy-source");
                CollectionAssert.Contains(repository.ListScenarioNames(), "legacy-source-migrated-v3");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static TestScenarioDefinition LoadFixture(string fileName)
        {
            return JsonUtility.FromJson<TestScenarioDefinition>(File.ReadAllText(FixturePath(fileName)));
        }

        private static string FixturePath(string fileName)
        {
            return Path.Combine(UnityEngine.Application.dataPath, "LearnHearthstone", "Tests", "EditMode", "Catalogs", "Fixtures", fileName);
        }

        private static string TempDirectory(string suffix)
        {
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-scenario-v3-" + suffix + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static void DeleteDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        private static void AssertDefaultMechanicSlots(TestScenarioDefinition scenario)
        {
            Assert.NotNull(scenario.PlayerDarkGiftState);
            Assert.NotNull(scenario.ChoiceQueueState);
            Assert.NotNull(scenario.RecruitActionStates);
            Assert.NotNull(scenario.DelayedObjectStates);
            Assert.NotNull(scenario.MechanicEvents);
            Assert.NotNull(scenario.RngState);
            Assert.IsEmpty(scenario.PlayerDarkGiftState.AcquiredGiftInstances);
            Assert.IsEmpty(scenario.ChoiceQueueState.PendingChoices);
            Assert.IsEmpty(scenario.RecruitActionStates);
            Assert.IsEmpty(scenario.DelayedObjectStates);
            Assert.IsEmpty(scenario.MechanicEvents);
        }
    }
}
