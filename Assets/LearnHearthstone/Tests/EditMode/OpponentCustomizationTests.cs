using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
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
    }
}
