using LearnHearthstone.Adapters.Data;
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

            Assert.IsTrue(result.Contains("鲜血宝石弹幕"));
            Assert.AreEqual(1, state.Player.Tavern.Growth.ShopModifiers.Count);
            Assert.AreEqual(BuffScope.ShopGlobal, state.Player.Tavern.Growth.ShopModifiers[0].Scope);
            Assert.AreEqual(1, state.Player.Tavern.Growth.ShopModifiers[0].Attack);
            Assert.AreEqual(1, state.Player.Tavern.Growth.ShopModifiers[0].Health);
        }
    }
}
