using LearnHearthstone.Adapters.Data;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MinionCatalogTests
    {
        [Test]
        public void LoadFromJson_LoadsOriginalBattlegroundsMinionPayload()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            var minion = catalog.GetByCardId("BG35_801");

            Assert.AreEqual(279, catalog.All.Count);
            Assert.AreEqual("贪吃的穴居人", minion.Name);
            Assert.AreEqual(1, minion.TavernTier);
            Assert.AreEqual(2, minion.BaseAttack);
            Assert.AreEqual(3, minion.BaseHealth);
        }
    }
}
