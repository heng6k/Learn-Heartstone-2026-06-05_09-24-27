using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class GlobalSideModifierConsistencyTests
    {
        [Test]
        public void ScenarioMapper_LegacyTavernValuesWinOverConflictingModifierCopies()
        {
            var scenario = new TestScenarioDefinition
            {
                Version = TestScenarioMigration.LegacyVersion,
                PlayerCombatModifiersAreAuthoritative = false,
                Tavern = new ScenarioTavernState
                {
                    Tier = 1,
                    TavernSpellBonusAttack = 3,
                    TavernSpellBonusHealth = 2
                },
                PlayerCombatModifiers = new SideCombatModifierState
                {
                    TavernSpellBonusAttack = 0,
                    TavernSpellBonusHealth = 0
                }
            };
            var target = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository()).State;

            TestScenarioMapper.ApplyTo(target, scenario);

            Assert.AreEqual(3, target.Player.Tavern.TavernSpellBonusAttack);
            Assert.AreEqual(2, target.Player.Tavern.TavernSpellBonusHealth);
            Assert.AreEqual(3, target.Player.CombatModifiers.TavernSpellBonusAttack);
            Assert.AreEqual(2, target.Player.CombatModifiers.TavernSpellBonusHealth);
        }

        [Test]
        public void DesignScenario_SpellFlowLoadsOneAuthoritativePlayerModifierSnapshot()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

            service.LoadDesignValidationScenario(DesignValidationScenarioCatalog.SpellFlow);

            Assert.AreEqual(3, service.State.Player.Tavern.TavernSpellBonusAttack);
            Assert.AreEqual(2, service.State.Player.Tavern.TavernSpellBonusHealth);
            Assert.AreEqual(3, service.State.Player.CombatModifiers.TavernSpellBonusAttack);
            Assert.AreEqual(2, service.State.Player.CombatModifiers.TavernSpellBonusHealth);
        }

        [Test]
        public void DesignScenario_HistoricalCardsAreRecalculatedForBothSidesOnLoad()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

            service.LoadDesignValidationScenario(DesignValidationScenarioCatalog.HistoricalStats);

            var playerEternal = service.State.Player.Board.Single(card => card.InstanceId == "player-eternal");
            var playerAutomaton = service.State.Player.Board.Single(card => card.InstanceId == "player-automaton");
            var opponentEternal = service.State.Opponent.Board.Single(card => card.InstanceId == "opponent-eternal-history");
            var opponentAutomaton = service.State.Opponent.Board.Single(card => card.InstanceId == "opponent-automaton-history");
            Assert.AreEqual(16, playerEternal.Attack);
            Assert.AreEqual(7, playerEternal.MaxHealth);
            Assert.AreEqual(12, playerAutomaton.Attack);
            Assert.AreEqual(10, playerAutomaton.MaxHealth);
            Assert.AreEqual(20, opponentEternal.Attack);
            Assert.AreEqual(9, opponentEternal.MaxHealth);
            Assert.AreEqual(15, opponentAutomaton.Attack);
            Assert.AreEqual(12, opponentAutomaton.MaxHealth);
        }

        [Test]
        public void PlayerToolModifiers_RecalculateExistingBoardHandAndShopCards()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Board.Add(Minion("player-eternal", "BG25_008", BoardSide.Player, 4, 1, Tribe.Undead));
            service.State.Player.Tavern.Hand.Add(Minion("player-automaton", "BG_TTN_401", BoardSide.Player, 3, 4, Tribe.Mech));
            service.State.Player.Tavern.Shop.Add(Minion("player-undead", "TEST_UNDEAD", BoardSide.Player, 2, 3, Tribe.Undead));

            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Player, SideCombatModifierKind.UndeadAttackBonus, 3));
            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Player, SideCombatModifierKind.EternalKnightDeaths, 2));
            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Player, SideCombatModifierKind.AstralAutomatonSummons, 4));

            Assert.AreEqual(15, service.State.Player.Board[0].Attack);
            Assert.AreEqual(5, service.State.Player.Board[0].MaxHealth);
            Assert.AreEqual(12, service.State.Player.Tavern.Hand[0].Attack);
            Assert.AreEqual(10, service.State.Player.Tavern.Hand[0].MaxHealth);
            Assert.AreEqual(5, service.State.Player.Tavern.Shop[0].Attack);

            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Player, SideCombatModifierKind.UndeadAttackBonus, 0));
            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Player, SideCombatModifierKind.EternalKnightDeaths, 0));
            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Player, SideCombatModifierKind.AstralAutomatonSummons, 0));

            Assert.AreEqual(4, service.State.Player.Board[0].Attack);
            Assert.AreEqual(1, service.State.Player.Board[0].MaxHealth);
            Assert.AreEqual(3, service.State.Player.Tavern.Hand[0].Attack);
            Assert.AreEqual(4, service.State.Player.Tavern.Hand[0].MaxHealth);
            Assert.AreEqual(2, service.State.Player.Tavern.Shop[0].Attack);
        }

        [Test]
        public void CopyPlayerBoard_ReplacesPlayerHistoryEnchantmentsWithOpponentValues()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var eternal = Minion("player-eternal-copy", "BG25_008", BoardSide.Player, 4, 1, Tribe.Undead);
            eternal.Attack = 24;
            eternal.Health = 11;
            eternal.MaxHealth = 11;
            eternal.Enchantments.Add(new Enchantment
            {
                Id = "Eternal Knight",
                SourceId = "Eternal Knight",
                AttackBonus = 20,
                HealthBonus = 10
            });
            service.State.Player.Board.Add(eternal);
            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Opponent, SideCombatModifierKind.EternalKnightDeaths, 1));

            service.Apply(new GameCommand(GameCommandType.CopyPlayerBoardToOpponent));

            var copy = service.State.Opponent.Board.Single();
            Assert.AreEqual(8, copy.Attack);
            Assert.AreEqual(3, copy.MaxHealth);
            var tracked = copy.Enchantments.Single(enchantment => enchantment.SourceId == "Eternal Knight");
            Assert.AreEqual(4, tracked.AttackBonus);
            Assert.AreEqual(2, tracked.HealthBonus);
        }

        [Test]
        public void Combat_PersistsSupportedOpponentHistoryAndQualityRewards()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var killer = Minion("player-killer", "PLAYER_KILLER", BoardSide.Player, 50, 50, Tribe.None);
            var eternalDeathrattle = Minion("opponent-eternal-rattle", "BG25_008", BoardSide.Opponent, 1, 1, Tribe.Undead, Keyword.Taunt, Keyword.Deathrattle);
            var trailblazer = Minion("opponent-trailblazer", "BG35_437", BoardSide.Opponent, 2, 50, Tribe.Quilboar);
            service.State.Player.Board.Add(killer);
            service.State.Opponent.Board.Add(eternalDeathrattle);
            service.State.Opponent.Board.Add(trailblazer);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 9617, SafetyLimit = 1 }));

            Assert.AreEqual(1, service.State.Opponent.CombatModifiers.EternalKnightDeaths);
            Assert.AreEqual(1, service.State.Opponent.CombatModifiers.FriendlyMinionDeathsThisGame);
            Assert.AreEqual(1, service.State.Opponent.CombatModifiers.BloodGemAttackBonus);
            Assert.AreEqual(5, eternalDeathrattle.Attack);
            Assert.AreEqual(3, eternalDeathrattle.MaxHealth);

            var saved = TestScenarioMapper.Capture(service.State, "opponent-growth-roundtrip");
            var restored = MatchService.CreateWithDefaultCatalog(7, new InMemoryTestScenarioRepository()).State;
            TestScenarioMapper.ApplyTo(restored, saved);
            Assert.AreEqual(1, restored.Opponent.CombatModifiers.EternalKnightDeaths);
            Assert.AreEqual(1, restored.Opponent.CombatModifiers.FriendlyMinionDeathsThisGame);
            Assert.AreEqual(1, restored.Opponent.CombatModifiers.BloodGemAttackBonus);
        }

        [Test]
        public void ToolLabels_DescribeModifierScopeInsteadOfImplyingUniversalAuras()
        {
            var labelMethod = typeof(UnityTavernTrainerController).GetMethod(
                "SideModifierSemanticLabel",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(labelMethod);
            Assert.AreEqual("本局酒馆法术数", labelMethod.Invoke(null, new object[] { SideCombatModifierKind.SpellsCastThisGame }));
            Assert.AreEqual("战斗法强", labelMethod.Invoke(null, new object[] { SideCombatModifierKind.SpellPower }));
            Assert.AreEqual("宝石攻(打出时)", labelMethod.Invoke(null, new object[] { SideCombatModifierKind.BloodGemAttackBonus }));
            Assert.AreEqual("本局友方死亡", labelMethod.Invoke(null, new object[] { SideCombatModifierKind.FriendlyMinionDeathsThisGame }));
        }

        [Test]
        public void SideModifierService_RoundTripsTavernValuesAndClampsNegatives()
        {
            var tavern = new TavernState
            {
                SpellPower = 3,
                BloodGemBonusAttack = 4,
                EternalKnightDeaths = -2
            };
            var modifiers = new SideCombatModifierState();

            SideModifierService.CopyFromTavern(tavern, modifiers);
            SideModifierService.SetValue(modifiers, SideCombatModifierKind.BloodGemAttackBonus, -9);
            SideModifierService.ApplyToTavern(modifiers, tavern);

            Assert.AreEqual(3, modifiers.SpellPower);
            Assert.AreEqual(0, modifiers.EternalKnightDeaths);
            Assert.AreEqual(0, tavern.BloodGemBonusAttack);
            Assert.AreEqual(3, tavern.SpellPower);
        }

        [Test]
        public void SideModifierService_CombatRewardsRecalculateTrackedHistory()
        {
            var modifiers = new SideCombatModifierState { EternalKnightDeaths = 1 };
            var eternal = Minion("service-eternal", "BG25_008", BoardSide.Opponent, 4, 1, Tribe.Undead);
            var changed = SideModifierService.ApplyCombatRewards(
                modifiers,
                new[]
                {
                    new CombatReward { Type = CombatRewardType.EternalKnightDied, Amount = 2 },
                    new CombatReward { Type = CombatRewardType.ImproveUndeadAttack, Amount = 3 }
                });

            SideModifierService.ApplyToRetainedCards(new[] { eternal }, modifiers);

            Assert.IsTrue(changed);
            Assert.AreEqual(3, modifiers.EternalKnightDeaths);
            Assert.AreEqual(3, modifiers.UndeadAttackBonus);
            Assert.AreEqual(19, eternal.Attack);
            Assert.AreEqual(7, eternal.MaxHealth);
            Assert.AreEqual(2, eternal.Enchantments.Count);
        }

        [Test]
        public void ScenarioMigration_V1ToV2IsOrderedAndIdempotent()
        {
            var scenario = new TestScenarioDefinition
            {
                Version = TestScenarioMigration.LegacyVersion,
                PlayerCombatModifiersAreAuthoritative = false,
                Tavern = new ScenarioTavernState
                {
                    TavernSpellBonusAttack = 5,
                    UndeadAttackBonus = 3,
                    EternalKnightDeaths = 2
                },
                PlayerCombatModifiers = new SideCombatModifierState()
            };

            var first = TestScenarioMigration.MigrateToCurrent(scenario);
            var second = TestScenarioMigration.MigrateToCurrent(first);

            Assert.AreSame(scenario, first);
            Assert.AreSame(first, second);
            Assert.AreEqual(TestScenarioMigration.CurrentVersion, scenario.Version);
            Assert.IsTrue(scenario.PlayerCombatModifiersAreAuthoritative);
            Assert.AreEqual(5, scenario.PlayerCombatModifiers.TavernSpellBonusAttack);
            Assert.AreEqual(3, scenario.PlayerCombatModifiers.UndeadAttackBonus);
            Assert.AreEqual(2, scenario.PlayerCombatModifiers.EternalKnightDeaths);
        }

        [Test]
        public void ScenarioCapture_AlwaysEmitsCurrentVersion()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;

            var scenario = TestScenarioMapper.Capture(state, "current-version");

            Assert.AreEqual(TestScenarioMigration.CurrentVersion, scenario.Version);
            Assert.IsTrue(scenario.PlayerCombatModifiersAreAuthoritative);
        }

        [Test]
        public void ScenarioMigration_RejectsUnknownFutureVersion()
        {
            var scenario = new TestScenarioDefinition { Version = "battle-test-loop-v99" };

            var exception = Assert.Throws<System.InvalidOperationException>(() => TestScenarioMigration.MigrateToCurrent(scenario));

            StringAssert.Contains("Unsupported test scenario version", exception.Message);
        }

        [Test]
        public void FileScenarioRepository_SavesAndLoadsCurrentMigratedVersion()
        {
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-scenario-migration-" + Guid.NewGuid().ToString("N"));
            try
            {
                var repository = new FileTestScenarioRepository(directory);
                repository.Save(new TestScenarioDefinition
                {
                    Version = TestScenarioMigration.LegacyVersion,
                    Name = "legacy-file",
                    PlayerCombatModifiersAreAuthoritative = false,
                    Tavern = new ScenarioTavernState { UndeadAttackBonus = 4 }
                });

                var loaded = repository.Load("legacy-file");

                Assert.AreEqual(TestScenarioMigration.CurrentVersion, loaded.Version);
                Assert.IsTrue(loaded.PlayerCombatModifiersAreAuthoritative);
                Assert.AreEqual(4, loaded.PlayerCombatModifiers.UndeadAttackBonus);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        private static MinionInstance Minion(string instanceId, string cardId, BoardSide owner, int attack, int health, Tribe tribe, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = cardId,
                Owner = owner,
                CardKind = CardKind.Minion,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                CanAttack = true,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords?.ToList() ?? new List<Keyword>()
            };
        }
    }
}
