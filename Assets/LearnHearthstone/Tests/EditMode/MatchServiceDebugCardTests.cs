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
    }
}
