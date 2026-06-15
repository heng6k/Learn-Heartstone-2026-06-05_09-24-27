using System;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MatchServiceDebugCardTests
    {
        [Test]
        public void Apply_AddMinionCardToHandCreatesPlayerHandInstance()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, source.CardId, CardKind.Minion));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            var added = service.State.Player.Tavern.Hand[0];
            Assert.AreEqual(source.CardId, added.CardId);
            Assert.AreEqual(CardKind.Minion, added.CardKind);
            Assert.AreEqual(BoardSide.Player, added.Owner);
            Assert.AreEqual(PoolSource.Debug, added.PoolSource);
            Assert.AreEqual(0, added.PoolCopiesHeld);
        }

        [Test]
        public void Apply_AddTavernSpellCardToHandCreatesSpellInstance()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var spell = service.State.Player.Tavern.Shop.Last(card => card.CardKind == CardKind.TavernSpell);
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, spell.CardId, CardKind.TavernSpell));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            var added = service.State.Player.Tavern.Hand[0];
            Assert.AreEqual(spell.CardId, added.CardId);
            Assert.AreEqual(CardKind.TavernSpell, added.CardKind);
            Assert.AreEqual(BoardSide.Player, added.Owner);
            Assert.AreEqual(PoolSource.Debug, added.PoolSource);
        }

        [Test]
        public void Apply_AddCardToHandThrowsWhenHandIsFull()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Player.Tavern.Hand.Clear();
            for (var index = 0; index < 10; index += 1)
            {
                service.State.Player.Tavern.Hand.Add(source.Clone());
            }

            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, source.CardId, CardKind.Minion)));
        }

        [Test]
        public void Apply_AddMinionCardToHandDoesNotConsumePoolCopy()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            var before = service.State.Player.Tavern.Pool[source.DefinitionId];
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, source.CardId, CardKind.Minion));

            Assert.AreEqual(before, service.State.Player.Tavern.Pool[source.DefinitionId]);
        }

        [Test]
        public void Apply_AddOpponentMinionCanCreateGoldenMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);

            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, source.CardId, true));

            Assert.AreEqual(1, service.State.Opponent.Board.Count);
            var added = service.State.Opponent.Board[0];
            Assert.AreEqual(source.CardId, added.CardId);
            Assert.AreEqual(BoardSide.Opponent, added.Owner);
            Assert.IsTrue(added.Golden);
            Assert.AreEqual(PoolSource.Debug, added.PoolSource);
            Assert.AreEqual(0, added.PoolCopiesHeld);
        }

        [Test]
        public void Apply_DebugCastTavernSpellDoesNotAddCardToHand()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            target.InstanceId = "debug-target-0";
            target.Owner = BoardSide.Player;
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Hand.Clear();

            var attackBefore = target.Attack;
            var handBefore = service.State.Player.Tavern.Hand.Count;

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, "100596", CardKind.TavernSpell, -1));

            Assert.AreEqual(handBefore, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(1, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(attackBefore + 4, service.State.Player.Board[0].Attack);
        }

        [Test]
        public void Apply_DebugCastTargetedTavernSpellRandomlyTargetsFriendlyBoard()
        {
            var service = MatchService.CreateWithDefaultCatalog(54321);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Player.Board.Add(source.Clone());
            service.State.Player.Board.Add(source.Clone());
            service.State.Player.Board[0].InstanceId = "debug-target-0";
            service.State.Player.Board[1].InstanceId = "debug-target-1";
            service.State.Player.Board[0].Owner = BoardSide.Player;
            service.State.Player.Board[1].Owner = BoardSide.Player;
            service.State.Player.Tavern.Hand.Clear();

            var firstAttackBefore = service.State.Player.Board[0].Attack;
            var secondAttackBefore = service.State.Player.Board[1].Attack;

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, "100596", CardKind.TavernSpell, -1));

            var firstGained = service.State.Player.Board[0].Attack - firstAttackBefore;
            var secondGained = service.State.Player.Board[1].Attack - secondAttackBefore;
            Assert.AreEqual(4, firstGained + secondGained);
            Assert.IsTrue(firstGained == 4 || secondGained == 4);
            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
        }
    }
}
