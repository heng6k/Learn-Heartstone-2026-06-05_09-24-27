using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Domain.Models;
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
        public void TierOnePool_UsesCurrentSoloPoolAndInferredTags()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();

            var tierOne = catalog.All.Where(definition => definition.InPool && definition.TavernTier == 1).ToList();

            Assert.AreEqual(20, tierOne.Count);
            Assert.IsTrue(tierOne.Any(definition => definition.CardId == "BG26_800" && definition.Name == "魔刃豹"));
            Assert.IsFalse(tierOne.Any(definition => definition.CardId == "BGDUO_114"));
            Assert.IsFalse(tierOne.Any(definition => definition.CardId == "BG26_529"));
            Assert.IsFalse(tierOne.Any(definition => definition.CardId == "BG25_013"));
            Assert.Contains("spell_discount", catalog.GetByCardId("BG31_330").Tags);
            Assert.Contains("spellcraft_generator", catalog.GetByCardId("BG27_004").Tags);
            Assert.Contains("buy_counter", catalog.GetByCardId("BG35_801").Tags);
            Assert.Contains("blood_gem_generator", catalog.GetByCardId("BG20_100").Tags);
        }

        [Test]
        public void MinionCatalog_RepresentativeMechanicSliceHasAtLeastFifteenTaggedMechanicMinions()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();

            var effectMinions = catalog.All.Where(definition => definition.Tags != null && definition.Tags.Any(tag => tag != "minion" && !tag.StartsWith("tier_"))).ToList();

            Assert.GreaterOrEqual(effectMinions.Count, 15);
        }

        [Test]
        public void MinionCatalog_DoesNotImportDuosPassMechanic()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();

            Assert.IsFalse(catalog.All.Any(definition => definition.Tags != null && definition.Tags.Contains("duos_pass")));
            Assert.IsFalse(catalog.All.Any(definition => definition.Keywords != null && definition.Keywords.Contains(Keyword.Pass)));
        }
    }
}
