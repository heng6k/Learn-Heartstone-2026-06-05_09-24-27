using System.Collections.Generic;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TavernSpellEngineTests
    {
        [Test]
        public void Cast_BloodGemBarrageAddsFutureShopGrowthModifier()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            var spell = new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                CardId = "126676",
                Name = "鲜血宝石弹幕"
            };

            var result = TavernSpellEngine.Cast(
                spell,
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            Assert.IsTrue(result.Contains("Blood Gem Barrage"));
            Assert.AreEqual(1, state.Player.Tavern.Growth.ShopModifiers.Count);
            Assert.AreEqual(BuffScope.ShopGlobal, state.Player.Tavern.Growth.ShopModifiers[0].Scope);
            Assert.AreEqual(1, state.Player.Tavern.Growth.ShopModifiers[0].Attack);
            Assert.AreEqual(1, state.Player.Tavern.Growth.ShopModifiers[0].Health);
        }

        [Test]
        public void Cast_TierOneGeneratedAndEconomySpellsResolve()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            var minion = state.Player.Tavern.Shop[0].Clone();
            state.Player.Board.Add(minion);

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "BLOOD_GEM", Name = "鲜血宝石" },
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            Assert.AreEqual(minion.BaseAttack + 1, state.Player.Board[0].Attack);
            Assert.AreEqual(minion.BaseHealth + 1, state.Player.Board[0].MaxHealth);

            state.Player.Tavern.Gold = state.Player.Tavern.MaxGold;
            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "104436", Name = "酒馆币" },
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            Assert.AreEqual(state.Player.Tavern.MaxGold + 1, state.Player.Tavern.Gold);
        }

        [Test]
        public void Cast_MenagerieTablewareUsesAnalyzerDistinctTribeCount()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Player.Board.Clear();
            state.Player.Board.Add(TestBoardMinion("all", "All", 1, 1, Tribe.All));

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "130527", Name = "Menagerie Tableware" },
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            Assert.AreEqual(31, state.Player.Board[0].Attack);
            Assert.AreEqual(31, state.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Cast_MisplacedTeaSetSelectsEachMinionAtMostOnce()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Player.Board.Clear();
            state.Player.Board.Add(TestBoardMinion("all", "All", 1, 1, Tribe.All));
            state.Player.Board.Add(TestBoardMinion("dragon", "Dragon", 1, 1, Tribe.Dragon));

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "105271", Name = "乱放的茶具" },
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            Assert.AreEqual(3, state.Player.Board[0].Attack);
            Assert.AreEqual(3, state.Player.Board[0].MaxHealth);
            Assert.AreEqual(3, state.Player.Board[1].Attack);
            Assert.AreEqual(3, state.Player.Board[1].MaxHealth);
        }

        [Test]
        public void StartOfCombatSpellQueue_PreventsDuplicateAndConsumesAllEffects()
        {
            var tavern = new TavernState();

            Assert.IsTrue(TavernSpellEngine.TryQueueStartOfCombatSpell("110401", tavern));
            Assert.IsFalse(TavernSpellEngine.TryQueueStartOfCombatSpell("110401", tavern));
            Assert.AreEqual(2, tavern.NextCombatBeetles);
            CollectionAssert.AreEqual(new[] { "110401" }, tavern.NextCombatTavernSpellCardIds);

            TavernSpellEngine.ConsumeStartOfCombatSpells(tavern);

            Assert.AreEqual(0, tavern.NextCombatBeetles);
            Assert.IsEmpty(tavern.NextCombatTavernSpellCardIds);
            Assert.IsTrue(TavernSpellEngine.TryQueueStartOfCombatSpell("110401", tavern));
        }

        [Test]
        public void MatchService_OpponentStartOfCombatSpellsMaterializeAndExpireAfterCombat()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();

            service.Apply(new GameCommand(GameCommandType.SetOpponentStartOfCombatSpell, "110401", CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.SetOpponentStartOfCombatSpell, "104560", CardKind.TavernSpell));

            var preview = service.GetOpponentCombatTavernStatePreview();
            Assert.AreEqual(2, preview.NextCombatBeetles);
            Assert.AreEqual(1, preview.NextCombatEnemyHealthToOne);
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.SetOpponentStartOfCombatSpell, "110401", CardKind.TavernSpell)));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 91, SafetyLimit = 4 }));

            Assert.IsEmpty(service.State.Opponent.NextCombatTavernSpellCardIds);
            Assert.AreEqual(0, service.GetOpponentCombatTavernStatePreview().NextCombatBeetles);
            Assert.DoesNotThrow(() =>
                service.Apply(new GameCommand(GameCommandType.SetOpponentStartOfCombatSpell, "110401", CardKind.TavernSpell)));
        }

        [Test]
        public void MatchService_UntriggeredPlayerStartOfCombatSpellsStillExpire()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var template = service.State.Player.Tavern.Shop[0];
            for (var index = 0; index < 7; index += 1)
            {
                var minion = template.Clone();
                minion.InstanceId = "full-board-" + index;
                service.State.Player.Board.Add(minion);
            }

            Assert.IsTrue(TavernSpellEngine.TryQueueStartOfCombatSpell("110401", service.State.Player.Tavern));
            Assert.IsTrue(TavernSpellEngine.TryQueueStartOfCombatSpell("104560", service.State.Player.Tavern));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 92, SafetyLimit = 4 }));

            Assert.AreEqual(0, service.State.Player.Tavern.NextCombatBeetles);
            Assert.AreEqual(0, service.State.Player.Tavern.NextCombatEnemyHealthToOne);
            Assert.IsEmpty(service.State.Player.Tavern.NextCombatTavernSpellCardIds);
        }

        private static MinionInstance TestBoardMinion(string id, string name, int attack, int health, params Tribe[] tribes)
        {
            return new MinionInstance
            {
                InstanceId = id,
                DefinitionId = id,
                CardId = id,
                Name = name,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Tribes = new List<Tribe>(tribes),
                Owner = BoardSide.Player
            };
        }
    }
}
