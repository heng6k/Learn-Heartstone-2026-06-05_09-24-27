using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class DesignValidationToolingTests
    {
        [Test]
        public void ScenarioCatalog_ExposesFiveDesignerValidationScenarios()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

            var scenarios = service.GetDesignValidationScenarios();

            Assert.AreEqual(5, scenarios.Count);
            CollectionAssert.Contains(scenarios.Select(scenario => scenario.Name).ToList(), DesignValidationScenarioCatalog.UndeadRevengeGrowth);
            CollectionAssert.Contains(scenarios.Select(scenario => scenario.Name).ToList(), DesignValidationScenarioCatalog.SpellFlow);
            CollectionAssert.Contains(scenarios.Select(scenario => scenario.Name).ToList(), DesignValidationScenarioCatalog.HistoricalStats);
            CollectionAssert.Contains(scenarios.Select(scenario => scenario.Name).ToList(), DesignValidationScenarioCatalog.EconomyToPower);
            CollectionAssert.Contains(scenarios.Select(scenario => scenario.Name).ToList(), DesignValidationScenarioCatalog.FullNextTurnFlow);
        }

        [Test]
        public void UndeadScenario_IncludesOpponentHandAndSidePressure()
        {
            Assert.IsTrue(DesignValidationScenarioCatalog.TryGetScenario(DesignValidationScenarioCatalog.UndeadRevengeGrowth, out var scenario));

            Assert.IsTrue(scenario.OpponentHand.Any(card => card.CardKind == CardKind.Minion));
            Assert.IsTrue(scenario.OpponentBoard.Any(card => card.CardId == "BG26_354"));
            Assert.IsTrue(scenario.OpponentBoard.Any(card => card.CardId == "BG26_350"));
            Assert.Greater(scenario.OpponentCombatModifiers.UndeadAttackBonus, 0);
            Assert.Greater(scenario.OpponentCombatModifiers.FriendlyMinionDeathsThisGame, 0);
        }

        [Test]
        public void LoadAndRunDesignValidationScenario_StoresCombatExplanation()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

            service.LoadDesignValidationScenario(DesignValidationScenarioCatalog.UndeadRevengeGrowth);
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 444, SafetyLimit = 80 }));

            Assert.IsNotNull(service.State.LastResult);
            Assert.IsNotNull(service.LastCombatExplanation);
            Assert.IsTrue(service.LastCombatExplanation.VariableSignals.Any(signal => signal.Title == "Undead attack" && signal.Side == BoardSide.Opponent));
            Assert.IsTrue(service.LastCombatExplanation.MainFactors.Count > 0);
        }

        [Test]
        public void CombatResultExplainer_DetectsVariablesTriggersAndContributors()
        {
            var output = new CombatOutput
            {
                Winner = CombatWinner.Player,
                Steps = 3,
                Replay = new CombatReplay()
            };
            output.Replay.Frames.Add(new CombatFrame
            {
                EventType = CombatEventType.DamageResolved,
                ActorSide = BoardSide.Player,
                ActorId = "player-attacker",
                TargetSide = BoardSide.Opponent,
                TargetId = "opponent-target",
                ActualDamageCount = 1
            });
            output.Replay.Frames.Add(new CombatFrame
            {
                EventType = CombatEventType.DeathrattleResolved,
                ActorSide = BoardSide.Opponent,
                ActorId = "opponent-bassgill",
                SummonedEntityIds = { "summoned-murloc" }
            });
            output.OpponentRewards.Add(new CombatReward { Type = CombatRewardType.FriendlyAvengeTriggered, Side = BoardSide.Opponent, Amount = 1 });
            var scenario = new TestScenarioDefinition
            {
                OpponentCombatModifiers = new SideCombatModifierState
                {
                    SpellPower = 3,
                    SpellsCastThisGame = 6,
                    UndeadAttackBonus = 5
                }
            };

            var explanation = CombatResultExplainer.Analyze(output, scenario);

            Assert.AreEqual(CombatWinner.Player, explanation.Winner);
            Assert.IsTrue(explanation.VariableSignals.Any(signal => signal.Title == "Spell power" && signal.Side == BoardSide.Opponent));
            Assert.IsTrue(explanation.TriggerSignals.Any(signal => signal.Title == "Deathrattle"));
            Assert.IsTrue(explanation.TopContributors.Any(contribution => contribution.EntityId == "player-attacker"));
            Assert.IsTrue(explanation.KeySwingCandidates.Count > 0);
        }

        [Test]
        public void MechanicCoverageReport_ContainsDesignTrustRows()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

            var report = service.GetMechanicCoverageReport();

            Assert.IsTrue(report.Rows.Any(row => row.System == "Opponent hand" && row.Configurable && row.CombatConsumed));
            Assert.IsTrue(report.Rows.Any(row => row.System == "Spell power" && row.Configurable && row.CombatConsumed));
            Assert.IsTrue(report.Rows.Any(row => row.System == "Full next-turn flow" && row.TestCovered));
            var questTrinketRow = report.Rows.Single(row => row.System == "Quest/Trinket interactions");
            Assert.That(questTrinketRow.Notes, Does.Contain("repeated summon overflow"));
            Assert.That(questTrinketRow.Notes, Does.Contain("stacked repeat sources"));
            Assert.That(questTrinketRow.Notes, Does.Contain("non-Beast filtering"));
            Assert.That(questTrinketRow.Notes, Does.Contain("counter remainders"));
            Assert.That(questTrinketRow.Notes, Does.Contain("replay non-duplication"));
            Assert.IsTrue(report.Rows.All(row => !string.IsNullOrEmpty(row.DesignConfidence)));
        }

        [Test]
        public void MechanicCoverageRegistry_HasStableUniqueCompleteEntriesAndDrivesReport()
        {
            var entries = MechanicCoverageRegistry.All;
            var report = MechanicCoverageReportService.CreateDefaultReport();

            Assert.IsNotEmpty(entries);
            Assert.AreEqual(entries.Count, entries.Select(entry => entry.Key).Distinct().Count());
            Assert.AreEqual(entries.Count, entries.Select(entry => entry.System).Distinct().Count());
            Assert.IsTrue(entries.All(entry => !string.IsNullOrWhiteSpace(entry.Key)));
            Assert.IsTrue(entries.All(entry => !string.IsNullOrWhiteSpace(entry.System)));
            Assert.IsTrue(entries.All(entry => !string.IsNullOrWhiteSpace(entry.DesignConfidence)));
            Assert.IsTrue(entries.All(entry => !string.IsNullOrWhiteSpace(entry.Notes)));
            Assert.AreEqual(entries.Count, report.Rows.Count);
            Assert.AreEqual("Spell power", MechanicCoverageRegistry.Find("SPELL-POWER").System);

            report.Rows[0].Notes = "mutated report row";
            Assert.AreNotEqual(report.Rows[0].Notes, entries[0].Notes);
        }

        [Test]
        public void FullNextTurnScenario_LogsEndCombatThenStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.LoadDesignValidationScenario(DesignValidationScenarioCatalog.FullNextTurnFlow);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var messages = service.State.Player.Tavern.RecruitLog.Select(entry => entry.Message).ToList();
            var endIndex = messages.FindIndex(message => message != null && message.Contains("Turn 6 ended."));
            var combatIndex = messages.FindIndex(message => message != null && message.Contains("Combat resolved before turn 7."));
            var startIndex = messages.FindIndex(message => message != null && message.Contains("Turn 7 started."));
            Assert.GreaterOrEqual(endIndex, 0);
            Assert.Greater(combatIndex, endIndex);
            Assert.Greater(startIndex, combatIndex);
            Assert.IsNotNull(service.LastCombatExplanation);
        }
    }
}
