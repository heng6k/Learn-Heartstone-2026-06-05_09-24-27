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
            var brann = catalog.GetByCardId("BG_LOE_077");

            Assert.AreEqual(280, catalog.All.Count);
            Assert.AreEqual("贪吃的穴居人", minion.Name);
            Assert.AreEqual(1, minion.TavernTier);
            Assert.AreEqual(2, minion.BaseAttack);
            Assert.AreEqual(3, minion.BaseHealth);
            CollectionAssert.AreEqual(new[] { Tribe.None }, brann.Tribes);
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
        public void LoadFromJson_SeparatesOfficialKeywordsFromMechanicKeywords()
        {
            var json = "{\"count\":1,\"minions\":[{\"id\":\"m1\",\"cardId\":\"M1\",\"dbfId\":1,\"name\":\"m1\",\"tavernTier\":1,\"attack\":1,\"health\":1,\"tribes\":[],\"keywords\":[\"战吼\",\"触发效果\"],\"officialKeywords\":[\"Taunt\"],\"text\":\"\",\"inPool\":1,\"poolCount\":12}]}";

            var catalog = MinionCatalogLoader.LoadFromJson(json);
            var minion = catalog.GetByCardId("M1");

            Assert.Contains(Keyword.Battlecry, minion.Keywords);
            Assert.Contains(Keyword.Taunt, minion.OfficialKeywords);
            Assert.IsFalse(minion.OfficialKeywords.Contains(Keyword.Battlecry));
            Assert.Contains("battlecry", minion.Tags);
        }

        [Test]
        public void TierOnePool_UsesCurrentSoloPoolAndInferredTags()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();

            var tierOne = catalog.All.Where(definition => definition.InPool && definition.TavernTier == 1).ToList();

            Assert.AreEqual(22, tierOne.Count);
            Assert.IsFalse(tierOne.Any(definition => definition.CardId == "BG26_800"));
            Assert.IsTrue(tierOne.Any(definition => definition.CardId == "BG31_803"));
            Assert.IsFalse(tierOne.Any(definition => definition.CardId == "BGDUO_114"));
            Assert.IsTrue(tierOne.Any(definition => definition.CardId == "BG26_529"));
            Assert.IsTrue(tierOne.Any(definition => definition.CardId == "BG25_013"));
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

        [Test]
        public void TrySyncGoldenText_UsesGoldenAndNormalCatalogDescriptions()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            var definition = catalog.GetByCardId("BG28_300");
            var card = new MinionInstance
            {
                DefinitionId = definition.Id,
                CardId = definition.CardId,
                Text = definition.Text
            };

            card.Golden = true;
            Assert.IsTrue(catalog.TrySyncGoldenText(card));
            Assert.AreEqual(definition.Golden.Text, card.Text);

            card.Golden = false;
            Assert.IsTrue(catalog.TrySyncGoldenText(card));
            Assert.AreEqual(definition.Text, card.Text);
        }

        [Test]
        public void TrySyncGoldenText_UnknownProxyPreservesExistingDescription()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            var card = new MinionInstance
            {
                DefinitionId = "unknown-proxy",
                CardId = "UNKNOWN_PROXY",
                Text = "proxy text",
                Golden = true
            };

            Assert.IsFalse(catalog.TrySyncGoldenText(card));
            Assert.AreEqual("proxy text", card.Text);
        }
    }
}
