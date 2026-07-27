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

            Assert.AreEqual(284, catalog.All.Count);
            Assert.AreEqual("贪吃的穴居人", minion.Name);
            Assert.AreEqual(1, minion.TavernTier);
            Assert.AreEqual(2, minion.BaseAttack);
            Assert.AreEqual(3, minion.BaseHealth);
            CollectionAssert.AreEqual(new[] { Tribe.None }, brann.Tribes);
        }

        [Test]
        public void LoadFromResources_EnglishCatalogHasCompleteNormalAndGoldenText()
        {
            var chinese = MinionCatalogLoader.LoadFromResources();
            var english = MinionCatalogLoader.LoadFromResources(true);
            var trogg = english.GetByCardId("BG35_801");

            Assert.AreEqual(284, english.All.Count);
            Assert.AreEqual("Gluttonous Trogg", trogg.Name);
            Assert.IsTrue(trogg.Text.Contains("Once you buy 4 cards"));
            Assert.IsTrue(english.All.All(definition =>
                !string.IsNullOrWhiteSpace(definition.Name) &&
                !string.IsNullOrWhiteSpace(definition.Text) &&
                definition.Golden != null &&
                !string.IsNullOrWhiteSpace(definition.Golden.Text) &&
                !definition.Name.StartsWith("[Missing en-US:") &&
                !definition.Text.StartsWith("[Missing en-US:") &&
                !definition.Golden.Text.StartsWith("[Missing en-US:") &&
                !ContainsChinese(definition.Name) &&
                !ContainsChinese(definition.Text) &&
                !ContainsChinese(definition.Golden.Text)));

            var chineseTrogg = chinese.GetByCardId("BG35_801");
            CollectionAssert.AreEqual(chineseTrogg.Tribes, trogg.Tribes);
            CollectionAssert.AreEqual(chineseTrogg.Keywords, trogg.Keywords);
            CollectionAssert.AreEqual(chineseTrogg.EffectIds, trogg.EffectIds);
            CollectionAssert.AreEqual(chineseTrogg.Tags, trogg.Tags);
            Assert.AreEqual(chineseTrogg.BaseAttack, trogg.BaseAttack);
            Assert.AreEqual(chineseTrogg.BaseHealth, trogg.BaseHealth);
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
        public void InPoolMinions_UseCurrentTierCopyCounts()
        {
            var expectedByTier = new[] { 0, 15, 15, 13, 11, 9, 7, 5 };
            var catalog = MinionCatalogLoader.LoadFromResources();

            foreach (var definition in catalog.All.Where(definition => definition.InPool))
            {
                Assert.That(definition.TavernTier, Is.InRange(1, 7), definition.CardId);
                Assert.AreEqual(expectedByTier[definition.TavernTier], definition.PoolCount, definition.CardId);
            }
        }

        [Test]
        public void HeroDerivatives_AreCatalogCardsButNeverNormalPoolCards()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            var derivativeIds = new[]
            {
                "TB_BaconShop_HP_105t",
                "BG21_HERO_030t",
                "TB_BaconShop_HP_033t",
                "BG31_HERO_811t"
            };

            foreach (var cardId in derivativeIds)
            {
                var definition = catalog.GetByCardId(cardId);
                Assert.IsFalse(definition.InPool, cardId);
                Assert.AreEqual(0, definition.PoolCount, cardId);
                Assert.Contains("hero_derivative", definition.Tags, cardId);
                Assert.IsNotNull(definition.Golden, cardId);
            }

            Assert.IsFalse(catalog.All.Any(card => card.CardId == "BG31_HERO_801pt"));
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

        private static bool ContainsChinese(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Any(character => character >= '\u4e00' && character <= '\u9fff');
        }
    }
}
