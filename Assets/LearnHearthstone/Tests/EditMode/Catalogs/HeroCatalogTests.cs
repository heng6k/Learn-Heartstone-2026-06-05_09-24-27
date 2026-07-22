using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class HeroCatalogTests
    {
        [Test]
        public void LoadFromResources_LoadsHeroPowerAndBuddyCatalogs()
        {
            var catalog = HeroCatalogLoader.LoadFromResources();

            Assert.AreEqual(117, catalog.AllHeroes.Count);
            Assert.AreEqual(117, catalog.AllHeroPowers.Count);
            Assert.AreEqual(108, catalog.AllBuddies.Count);
            Assert.IsTrue(catalog.AllHeroPowers.All(power => !string.IsNullOrEmpty(power.CardId)));
            Assert.IsTrue(catalog.AllBuddies.All(buddy => !string.IsNullOrEmpty(buddy.CardId)));
            Assert.AreEqual(HeroPowerReplacementEligibility.Disabled, catalog.GetHeroPowerByCardId("BG35_Anomaly_002t").ReplacementEligibility);
            Assert.AreEqual(HeroPowerReplacementEligibility.Disabled, catalog.GetHeroPowerByCardId("BG35_Anomaly_007t").ReplacementEligibility);
            Assert.AreEqual(HeroPowerReplacementEligibility.Disabled, catalog.GetHeroPowerByCardId("BG35_Anomaly_008t").ReplacementEligibility);
            Assert.IsFalse(catalog.GetInitialSelectableHeroes().Any(hero => hero.HeroCardId == "BG35_Anomaly_002t_PROXY"));
            Assert.IsFalse(catalog.GetInitialSelectableHeroes().Any(hero => hero.HeroCardId == "BG35_Anomaly_007t_PROXY"));
            Assert.IsFalse(catalog.GetInitialSelectableHeroes().Any(hero => hero.HeroCardId == "BG35_Anomaly_008t_PROXY"));
        }

        [Test]
        public void LoadFromResources_KeepsPatchwerkHealthAndInitialOnlyPower()
        {
            var catalog = HeroCatalogLoader.LoadFromResources();

            var patchwerk = catalog.GetHeroByCardId("TB_BaconShop_HERO_34");

            Assert.AreEqual("Patchwerk", patchwerk.Name);
            Assert.AreEqual(60, patchwerk.Health);
            Assert.AreEqual(0, patchwerk.Armor);
            Assert.IsNotNull(patchwerk.Buddy);
            Assert.IsFalse(patchwerk.MissingHeroPowerMapping);
            Assert.IsNotNull(patchwerk.HeroPower);
            Assert.AreEqual("TB_BaconShop_HP_035", patchwerk.HeroPower.CardId);
            Assert.AreEqual(HeroPowerReplacementEligibility.InitialOnly, patchwerk.HeroPower.ReplacementEligibility);
        }

        [Test]
        public void LoadFromResources_MergesZhCnHeroAndHeroPowerLocalization()
        {
            var catalog = HeroCatalogLoader.LoadFromResources();

            var yogg = catalog.GetHeroByCardId("TB_BaconShop_HERO_35");

            Assert.AreEqual("Yogg-Saron, Hope's End", yogg.Name);
            Assert.AreEqual("尤格-萨隆", yogg.ZhName);
            Assert.IsNotNull(yogg.HeroPower);
            Assert.AreEqual("Puzzle Box", yogg.HeroPower.Name);
            Assert.AreEqual("谜之匣", yogg.HeroPower.ZhName);
            Assert.IsTrue(yogg.HeroPower.ZhText.Contains("酒馆法术"));

            var lesserCrystalBall = catalog.GetHeroPowerByCardId("BG35_Anomaly_007t");
            Assert.AreEqual("小型水晶球", lesserCrystalBall.ZhName);
            Assert.IsTrue(lesserCrystalBall.ZhText.Contains("饰品"));
        }

        [Test]
        public void LoadFromResources_ExplicitlyListsMissingBuddyMappings()
        {
            var catalog = HeroCatalogLoader.LoadFromResources();

            var missingBuddyHeroes = catalog.AllHeroes
                .Where(hero => hero.MissingBuddyMapping || hero.Buddy == null)
                .Select(hero => hero.Name)
                .OrderBy(name => name)
                .ToList();

            CollectionAssert.AreEqual(
                new[]
                {
                    "Farseer Nobundo",
                    "Genn, Worgen King",
                    "Greater Crystal Ball",
                    "Lesser Crystal Ball",
                    "Mister Clocksworth",
                    "Morchie",
                    "Murozond, Unbounded",
                    "Mystery Cube",
                    "Time Twister Chromie"
                },
                missingBuddyHeroes);
        }

        [Test]
        public void DiscoverableHeroPowers_ExcludeCurrentAndNonDiscoverableEligibility()
        {
            var catalog = HeroCatalogLoader.LoadFromResources();
            var current = catalog.AllHeroPowers.First(power =>
                power.ReplacementEligibility == HeroPowerReplacementEligibility.DiscoverableAfterStart);

            var options = catalog.GetDiscoverableHeroPowers(current.CardId);

            Assert.IsFalse(options.Any(power => power.CardId == current.CardId));
            Assert.IsTrue(options.All(power =>
                power.ReplacementEligibility == HeroPowerReplacementEligibility.DiscoverableAfterStart));
        }

        [Test]
        public void OfferableDiscoverableHeroPowers_FilterUnsafeCandidatesAndTagFrameworkFirstOptions()
        {
            var catalog = HeroCatalogLoader.LoadFromResources();

            var options = catalog.GetOfferableDiscoverableHeroPowers("TB_BaconShop_HP_035");
            var ids = options.Select(power => power.CardId).ToList();

            CollectionAssert.DoesNotContain(ids, "TB_BaconShop_HP_080");
            CollectionAssert.DoesNotContain(ids, "BG34_HERO_002p");
            CollectionAssert.DoesNotContain(ids, "TB_BaconShop_HP_081");
            CollectionAssert.DoesNotContain(ids, "BG23_HERO_303p2");
            CollectionAssert.Contains(ids, "BG23_HERO_304p");
            CollectionAssert.Contains(ids, "BG22_HERO_007p");
            CollectionAssert.Contains(ids, "TB_BaconShop_HP_041");
            CollectionAssert.Contains(ids, "TB_BaconShop_HP_086");
            CollectionAssert.Contains(ids, "TB_BaconShop_HP_053");
            CollectionAssert.Contains(ids, "BG21_HERO_010p");
            CollectionAssert.Contains(ids, "TB_BaconShop_HP_077");

            var proxyOption = catalog.CreateDiscoverableHeroPowerOption(
                catalog.GetHeroPowerByCardId("TB_BaconShop_HP_080"),
                BoardSide.Player,
                "proxy-test");
            CollectionAssert.Contains(proxyOption.Tags, "implementation_status:FrameworkFirst");
            CollectionAssert.Contains(proxyOption.Tags, "hero_power_proxy");
            CollectionAssert.Contains(proxyOption.Tags, "framework_first");
            CollectionAssert.Contains(proxyOption.Tags, "incomplete_hero_power");

            var implementedOption = catalog.CreateDiscoverableHeroPowerOption(
                catalog.GetHeroPowerByCardId("TB_BaconShop_HP_010"),
                BoardSide.Player,
                "implemented-test");
            CollectionAssert.Contains(implementedOption.Tags, "implementation_status:Implemented");
            CollectionAssert.DoesNotContain(implementedOption.Tags, "hero_power_proxy");

            var a1ImplementedOption = catalog.CreateDiscoverableHeroPowerOption(
                catalog.GetHeroPowerByCardId("BG22_HERO_000p"),
                BoardSide.Player,
                "a1-implemented-test");
            CollectionAssert.Contains(a1ImplementedOption.Tags, "implementation_status:Implemented");
            CollectionAssert.DoesNotContain(a1ImplementedOption.Tags, "hero_power_proxy");
            CollectionAssert.DoesNotContain(a1ImplementedOption.Tags, "framework_first");
        }

        [Test]
        public void OfferableDiscoverableHeroPowers_ExcludeAllPlannedDeferredAndUnregisteredCandidates()
        {
            var catalog = HeroCatalogLoader.LoadFromResources();

            var options = catalog.GetOfferableDiscoverableHeroPowers("TB_BaconShop_HP_035");
            var unsafeOptions = options
                .Select(power => new
                {
                    Power = power,
                    Status = HeroEffectImplementationRegistry.GetStatusByHeroPowerCardId(power.CardId)
                })
                .Where(item =>
                    item.Status == HeroEffectImplementationStatus.Planned ||
                    item.Status == HeroEffectImplementationStatus.Deferred ||
                    item.Status == HeroEffectImplementationStatus.Unregistered)
                .Select(item => item.Power.CardId + ":" + item.Status)
                .ToList();

            Assert.IsEmpty(unsafeOptions, string.Join(", ", unsafeOptions));
        }

        [Test]
        public void HeroBuddies_DoNotEnterNormalMinionPool()
        {
            var heroes = HeroCatalogLoader.LoadFromResources();
            var minions = MinionCatalogLoader.LoadFromResources();
            var buddyIds = new HashSet<string>(heroes.AllBuddies.Select(buddy => buddy.CardId));

            Assert.IsFalse(minions.All.Any(minion => buddyIds.Contains(minion.CardId)));
        }

        [Test]
        public void HeroPowerCategories_DistinguishDerivativesRecruitActionsAndCombatEffects()
        {
            var catalog = HeroCatalogLoader.LoadFromResources();

            Assert.AreEqual(HeroPowerCategory.Minion, catalog.GetHeroPowerByCardId("TB_BaconShop_HP_105").PrimaryCategory);
            Assert.AreEqual(HeroPowerCategory.Minion, catalog.GetHeroPowerByCardId("BG31_HERO_005p").PrimaryCategory);
            Assert.AreEqual(HeroPowerCategory.Discover, catalog.GetHeroPowerByCardId("BG23_HERO_306p").PrimaryCategory);
            Assert.AreEqual(HeroPowerCategory.Discover, catalog.GetHeroByCardId("TB_BaconShop_HERO_16").HeroPower.PrimaryCategory);
            Assert.AreEqual(HeroPowerCategory.Combat, catalog.GetHeroPowerByCardId("TB_BaconShop_HP_103").PrimaryCategory);
        }
    }
}
