using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class OpponentCustomizationTests
    {
        [Test]
        public void Apply_AddOpponentMinionCreatesOpponentBoardInstance()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Opponent.Board.Clear();

            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, source.CardId));

            Assert.AreEqual(1, service.State.Opponent.Board.Count);
            var added = service.State.Opponent.Board[0];
            Assert.AreEqual(source.CardId, added.CardId);
            Assert.AreEqual(BoardSide.Opponent, added.Owner);
            Assert.AreEqual(PoolSource.Debug, added.PoolSource);
            Assert.AreEqual(0, added.PoolCopiesHeld);
        }

        [Test]
        public void Apply_AddOpponentMinionThrowsWhenOpponentBoardIsFull()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Opponent.Board.Clear();
            for (var index = 0; index < 7; index += 1)
            {
                service.State.Opponent.Board.Add(source.Clone());
            }

            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, source.CardId)));
        }

        [Test]
        public void Apply_RemoveOpponentMinionDeletesTarget()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Opponent.Board.Clear();
            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, source.CardId));
            var target = service.State.Opponent.Board[0];

            service.Apply(new GameCommand(GameCommandType.RemoveOpponentMinion, target.InstanceId));

            Assert.AreEqual(0, service.State.Opponent.Board.Count);
        }

        [Test]
        public void Apply_MoveOpponentMinionReordersOpponentBoard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Opponent.Board.Clear();
            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, source.CardId));
            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, source.CardId));
            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, source.CardId));
            var first = service.State.Opponent.Board[0];
            var second = service.State.Opponent.Board[1];
            var third = service.State.Opponent.Board[2];

            service.Apply(new GameCommand(GameCommandType.MoveOpponentMinion, first.InstanceId, 2));

            Assert.AreEqual(second.InstanceId, service.State.Opponent.Board[0].InstanceId);
            Assert.AreEqual(third.InstanceId, service.State.Opponent.Board[1].InstanceId);
            Assert.AreEqual(first.InstanceId, service.State.Opponent.Board[2].InstanceId);
        }

        [Test]
        public void Apply_UpdateOpponentMinionChangesStatsAndKeywords()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Opponent.Board.Clear();
            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, source.CardId));
            var target = service.State.Opponent.Board[0];

            service.Apply(new GameCommand(
                GameCommandType.UpdateOpponentMinion,
                target.InstanceId,
                new MinionPatch
                {
                    Attack = 12,
                    MaxHealth = 9,
                    Health = 7,
                    Keywords = new List<Keyword> { Keyword.Taunt, Keyword.DivineShield }
                }));

            var updated = service.State.Opponent.Board[0];
            Assert.AreEqual(12, updated.Attack);
            Assert.AreEqual(9, updated.MaxHealth);
            Assert.AreEqual(7, updated.Health);
            Assert.AreEqual(new[] { Keyword.Taunt, Keyword.DivineShield }, updated.Keywords);
        }

        [Test]
        public void Apply_AddsAndRemovesOpponentHandCard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, BoardSide.Opponent, source.CardId, source.CardKind));

            Assert.AreEqual(1, service.State.Opponent.Hand.Count);
            Assert.AreEqual(source.CardId, service.State.Opponent.Hand[0].CardId);
            Assert.AreEqual(BoardSide.Opponent, service.State.Opponent.Hand[0].Owner);

            service.Apply(new GameCommand(GameCommandType.RemoveHandCard, BoardSide.Opponent, 0));

            Assert.AreEqual(0, service.State.Opponent.Hand.Count);
        }

        [Test]
        public void Apply_SideCombatModifiersAreIndependent()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);

            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Player, SideCombatModifierKind.BloodGemAttackBonus, 3));
            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Opponent, SideCombatModifierKind.BloodGemAttackBonus, 1));
            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Opponent, SideCombatModifierKind.UndeadAttackBonus, 4));

            Assert.AreEqual(3, service.State.Player.CombatModifiers.BloodGemAttackBonus);
            Assert.AreEqual(3, service.State.Player.Tavern.BloodGemBonusAttack);
            Assert.AreEqual(1, service.State.Opponent.CombatModifiers.BloodGemAttackBonus);
            Assert.AreEqual(4, service.State.Opponent.CombatModifiers.UndeadAttackBonus);
            Assert.AreEqual(0, service.State.Player.CombatModifiers.UndeadAttackBonus);
        }

        [Test]
        public void RunCombatTest_UsesOpponentHandForOpponentEffects()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var handSource = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Opponent.Hand.Clear();

            var player = handSource.Clone();
            player.InstanceId = "player-target";
            player.Owner = BoardSide.Player;
            player.Attack = 1;
            player.Health = 1;
            player.MaxHealth = 1;
            service.State.Player.Board.Add(player);

            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, "BG26_354"));
            var choral = service.State.Opponent.Board[0];
            choral.InstanceId = "opponent-choral";
            choral.Attack = 1;
            choral.Health = 50;
            choral.MaxHealth = 50;

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, BoardSide.Opponent, handSource.CardId, handSource.CardKind));
            service.State.Opponent.Hand[0].Attack = 5;
            service.State.Opponent.Hand[0].Health = 6;
            service.State.Opponent.Hand[0].MaxHealth = 6;

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 777, SafetyLimit = 20 }));

            var finalChoral = service.State.LastResult.FinalOpponentBoard.First(card => card.InstanceId == "opponent-choral");
            Assert.AreEqual(6, finalChoral.Attack);
            Assert.AreEqual(56, finalChoral.MaxHealth);
        }

        [Test]
        public void RunCombatTest_ConsumesOpponentSideHistoryModifiers()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();

            service.State.Player.Board.Add(CreateBoardMinion("player-wall", "player-wall-card", BoardSide.Player, 1, 100));
            service.State.Opponent.Board.Add(CreateBoardMinion("opponent-undead", "opponent-undead-card", BoardSide.Opponent, 1, 50, Tribe.Undead));
            service.State.Opponent.Board.Add(CreateBoardMinion("opponent-eternal", "BG25_008", BoardSide.Opponent, 1, 50, Tribe.Undead));
            service.State.Opponent.Board.Add(CreateBoardMinion("opponent-automaton", "BG_TTN_401", BoardSide.Opponent, 1, 50, Tribe.Mech));
            service.State.Opponent.Hand.Add(CreateBoardMinion("opponent-hand-eternal", "BG25_008", BoardSide.Opponent, 2, 30, Tribe.Undead));
            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Opponent, SideCombatModifierKind.UndeadAttackBonus, 3));
            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Opponent, SideCombatModifierKind.EternalKnightDeaths, 2));
            service.Apply(new GameCommand(GameCommandType.SetSideCombatModifier, BoardSide.Opponent, SideCombatModifierKind.AstralAutomatonSummons, 4));

            Assert.AreEqual(4, service.State.Opponent.Board[0].Attack);
            Assert.AreEqual(12, service.State.Opponent.Board[1].Attack);
            Assert.AreEqual(54, service.State.Opponent.Board[1].MaxHealth);
            Assert.AreEqual(10, service.State.Opponent.Board[2].Attack);
            Assert.AreEqual(56, service.State.Opponent.Board[2].MaxHealth);
            Assert.AreEqual(13, service.State.Opponent.Hand[0].Attack);
            Assert.AreEqual(34, service.State.Opponent.Hand[0].MaxHealth);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 777, SafetyLimit = 0 }));

            var undead = service.State.LastResult.FinalOpponentBoard.First(card => card.InstanceId == "opponent-undead");
            var eternal = service.State.LastResult.FinalOpponentBoard.First(card => card.InstanceId == "opponent-eternal");
            var automaton = service.State.LastResult.FinalOpponentBoard.First(card => card.InstanceId == "opponent-automaton");
            Assert.AreEqual(4, undead.Attack);
            Assert.AreEqual(12, eternal.Attack);
            Assert.AreEqual(54, eternal.MaxHealth);
            Assert.AreEqual(10, automaton.Attack);
            Assert.AreEqual(56, automaton.MaxHealth);
        }

        [Test]
        public void CombatEngine_UsesOpponentSpellPowerForCombatSpellDamage()
        {
            var player = CreateBoardMinion("player-target", "player-target-card", BoardSide.Player, 1, 20);
            var opponent = CreateBoardMinion("opponent-caster", "opponent-caster-card", BoardSide.Opponent, 1, 20);
            var opponentTavern = new TavernState
            {
                HeroBrukanElementActive = true,
                HeroBrukanElement = "lightning",
                SpellPower = 2
            };

            var result = CombatEngine.SimulateBasicCombat(
                new[] { player },
                new[] { opponent },
                777,
                0,
                null,
                opponentTavern);

            var finalPlayer = result.FinalPlayerBoard.First(card => card.InstanceId == "player-target");
            Assert.AreEqual(5, finalPlayer.Health);
        }

        [Test]
        public void CombatEngine_UsesPlayerSpellPowerForTavishDeadeyeDamage()
        {
            var player = CreateBoardMinion("player-caster", "player-caster-card", BoardSide.Player, 1, 20);
            var opponent = CreateBoardMinion("opponent-target", "opponent-target-card", BoardSide.Opponent, 1, 10);
            var playerTavern = new TavernState
            {
                HeroTavishDeadeyeActive = true,
                SpellPower = 3
            };

            var result = CombatEngine.SimulateBasicCombat(
                new[] { player },
                new[] { opponent },
                777,
                0,
                playerTavern);

            var finalOpponent = result.FinalOpponentBoard.First(card => card.InstanceId == "opponent-target");
            Assert.AreEqual(6, finalOpponent.Health);
            Assert.IsTrue(result.Log.Any(entry => entry.Detail.Contains("Deadeye dealt 4 damage")));
        }

        [Test]
        public void NextTurn_RunsCombatBeforeStartingNextRecruitTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { UseEnglish = true, EnableTimewarpedTavern = false });
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();

            var player = source.Clone();
            player.InstanceId = "player-next-turn";
            player.Owner = BoardSide.Player;
            player.Attack = 4;
            player.Health = 4;
            player.MaxHealth = 4;
            service.State.Player.Board.Add(player);

            var opponent = source.Clone();
            opponent.InstanceId = "opponent-next-turn";
            opponent.Owner = BoardSide.Opponent;
            opponent.Attack = 1;
            opponent.Health = 2;
            opponent.MaxHealth = 2;
            service.State.Opponent.Board.Add(opponent);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Round);
            Assert.AreEqual(MatchPhase.Tavern, service.State.Phase);
            Assert.IsNotNull(service.State.LastResult);
            Assert.AreEqual("CombatStarted", service.State.CombatLog.First().Title);
            Assert.AreEqual("CombatEnded", service.State.CombatLog.Last().Title);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(log => log.Message == "Combat resolved before turn 2."));
        }

        [Test]
        public void SimulateCombat_CompletesNextTurnAndPaysQueuedCombatGold()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { UseEnglish = true, EnableTimewarpedTavern = false });
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(CreateBoardMinion("player-sim-turn", "player-sim-turn-card", BoardSide.Player, 10, 10));
            service.State.Opponent.Board.Add(CreateBoardMinion("opponent-sim-turn", "opponent-sim-turn-card", BoardSide.Opponent, 1, 1));
            service.State.Player.Tavern.PendingCombatWinGold = 2;

            service.Apply(new GameCommand(GameCommandType.SimulateCombat, new CombatTestOptions { Seed = 777, SafetyLimit = 20 }));

            Assert.AreEqual(2, service.State.Round);
            Assert.AreEqual(MatchPhase.Tavern, service.State.Phase);
            Assert.IsNotNull(service.State.LastResult);
            Assert.AreEqual(0, service.State.Player.Tavern.PendingCombatWinGold);
            Assert.AreEqual(0, service.State.Player.Tavern.NextTurnBonusGold);
            Assert.AreEqual(service.State.Player.Tavern.MaxGold + 2, service.State.Player.Tavern.Gold);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(log => log.Message == "Combat resolved before turn 2."));
        }

        [Test]
        public void DebugSkipToNextTurn_RunsTurnTransitionWithoutCombatResult()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { UseEnglish = true, EnableTimewarpedTavern = false });
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(CreateBoardMinion("player-skip-turn", source.CardId, BoardSide.Player, 2, 2));
            service.State.Opponent.Board.Add(CreateBoardMinion("opponent-skip-turn", source.CardId, BoardSide.Opponent, 1, 2));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest));

            Assert.IsNotNull(service.State.LastResult);

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(2, service.State.Round);
            Assert.AreEqual(MatchPhase.Tavern, service.State.Phase);
            Assert.IsNull(service.State.LastResult);
            Assert.IsNull(service.State.LastReplay);
            Assert.AreEqual(0, service.State.CombatLog.Count);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(log => log.Message == "Turn 1 ended."));
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(log => log.Message == "Turn 2 started."));
            Assert.IsFalse(service.State.Player.Tavern.RecruitLog.Any(log => log.Message == "Combat resolved before turn 2."));
        }

        private static MinionInstance CreateBoardMinion(string instanceId, string cardId, BoardSide owner, int attack, int health, params Tribe[] tribes)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                CardId = cardId,
                DefinitionId = cardId,
                Name = cardId,
                Owner = owner,
                CardKind = CardKind.Minion,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                BaseAttack = attack,
                BaseHealth = health,
                CanAttack = true,
                Tribes = tribes?.ToList() ?? new List<Tribe>()
            };
        }
    }
}
