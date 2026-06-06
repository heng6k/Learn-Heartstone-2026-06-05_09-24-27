using LearnHearthstone.Adapters.Data;
using System.Linq;
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

        [Test]
        public void LoadFromJson_LeavesEffectIdsEmptyWhenPayloadOmitsThem()
        {
            var catalog = MinionCatalogLoader.LoadFromJson("{\"count\":1,\"minions\":[{\"id\":\"m1\",\"cardId\":\"M1\",\"dbfId\":1,\"name\":\"m1\",\"tavernTier\":1,\"attack\":1,\"health\":1,\"tribes\":[],\"keywords\":[],\"text\":\"\",\"inPool\":1,\"poolCount\":12}]}");

            Assert.AreEqual(0, catalog.GetByCardId("M1").EffectIds.Count);
        }

        [Test]
        public void LoadFromJson_ReadsEffectIdsFromPayload()
        {
            var catalog = MinionCatalogLoader.LoadFromJson("{\"count\":1,\"minions\":[{\"id\":\"m1\",\"cardId\":\"M1\",\"dbfId\":1,\"name\":\"m1\",\"tavernTier\":1,\"attack\":1,\"health\":1,\"tribes\":[],\"keywords\":[],\"text\":\"\",\"inPool\":1,\"poolCount\":12,\"effectIds\":[\"battlecry_self_buff_2_2\"]}]}");

            Assert.AreEqual("battlecry_self_buff_2_2", catalog.GetByCardId("M1").EffectIds[0]);
        }

        [Test]
        public void MinionCatalog_RepresentativeMechanicSliceHasAtLeastFifteenEffectMinions()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();

            var effectMinions = catalog.All.Where(definition => definition.EffectIds != null && definition.EffectIds.Count > 0).ToList();

            Assert.GreaterOrEqual(effectMinions.Count, 15);
        }
    }
}
