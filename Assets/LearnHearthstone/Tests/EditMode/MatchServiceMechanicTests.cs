using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MatchServiceMechanicTests
    {
        [Test]
        public void Apply_PlayMinionDispatchesCardPlayedEffects()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            source.EffectIds = new List<string> { "battlecry_self_buff_2_2" };
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Add(source);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(source.BaseAttack + 2, service.State.Player.Board[0].Attack);
            Assert.AreEqual(source.BaseHealth + 2, service.State.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Apply_SellMinionDispatchesMinionSoldBeforeRemovingSource()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            source.InstanceId = "sell-source";
            source.EffectIds = new List<string> { "minion_sold_gain_gold_1" };
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Gold = 0;
            service.State.Player.Tavern.MaxGold = 10;

            service.Apply(new GameCommand(GameCommandType.SellMinion, source.InstanceId));

            Assert.AreEqual(2, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void Apply_NextTurnDispatchesTurnEndedForBoardMinionsBeforeNewShop()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var minion = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            minion.InstanceId = "turn-end-source";
            minion.EffectIds = new List<string> { "turn_ended_self_buff_1_1" };
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(minion);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(minion.BaseAttack + 1, service.State.Player.Board[0].Attack);
            Assert.AreEqual(minion.BaseHealth + 1, service.State.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Apply_BuyMinionDispatchesCardBoughtEffects()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            source.InstanceId = "buy-source";
            source.EffectIds = new List<string> { "card_bought_buff_self_1_1" };
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(source);

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(source.BaseAttack + 1, service.State.Player.Board[0].Attack);
            Assert.AreEqual(source.BaseHealth + 1, service.State.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Apply_RerollShopDispatchesShopRefreshedEffects()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            source.InstanceId = "refresh-source";
            source.EffectIds = new List<string> { "shop_refreshed_buff_shop_1_1" };
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(source);
            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 10));

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var minions = service.State.Player.Tavern.Shop.Where(card => card.CardKind == CardKind.Minion).ToList();
            Assert.IsTrue(minions.All(card => card.Attack >= card.BaseAttack + 1));
            Assert.IsTrue(minions.All(card => card.MaxHealth >= card.BaseHealth + 1));
        }

        [Test]
        public void Apply_NextTurnDispatchesTurnStartedAfterNewRoundSetup()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var minion = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            minion.InstanceId = "turn-start-source";
            minion.EffectIds = new List<string> { "turn_started_self_buff_1_1" };
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(minion);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(minion.BaseAttack + 1, service.State.Player.Board[0].Attack);
            Assert.AreEqual(minion.BaseHealth + 1, service.State.Player.Board[0].MaxHealth);
            Assert.AreEqual(2, service.State.Round);
            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void Apply_PlayTavernSpellDispatchesTavernSpellCastEffects()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            var spell = new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "test-spell",
                DefinitionId = "test-spell",
                CardId = "TEST_SPELL",
                Name = "Test Spell",
                Owner = BoardSide.Player
            };
            source.InstanceId = "spell-source";
            source.EffectIds = new List<string> { "tavern_spell_cast_buff_self_1_1" };
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Hand.Add(spell);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(1, service.State.Player.Board.Count);
            Assert.AreEqual(source.BaseAttack + 1, service.State.Player.Board[0].Attack);
            Assert.AreEqual(source.BaseHealth + 1, service.State.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Apply_PlayNormalSpellDoesNotDispatchTavernSpellCastEffects()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            source.InstanceId = "normal-spell-source";
            source.EffectIds = new List<string> { "tavern_spell_cast_buff_self_1_1" };
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BLOOD_GEM", CardKind.Spell));
            Assert.AreEqual(CardKind.Spell, service.State.Player.Tavern.Hand[0].CardKind);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(source.BaseAttack + 1, service.State.Player.Board[0].Attack);
            Assert.AreEqual(source.BaseHealth + 1, service.State.Player.Board[0].MaxHealth);
        }
    }
}
