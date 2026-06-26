using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
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

            Assert.AreEqual(114, catalog.AllHeroes.Count);
            Assert.AreEqual(114, catalog.AllHeroPowers.Count);
            Assert.AreEqual(108, catalog.AllBuddies.Count);
            Assert.IsTrue(catalog.AllHeroPowers.All(power => !string.IsNullOrEmpty(power.CardId)));
            Assert.IsTrue(catalog.AllBuddies.All(buddy => !string.IsNullOrEmpty(buddy.CardId)));
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
                    "Mister Clocksworth",
                    "Morchie",
                    "Murozond, Unbounded",
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
        public void HeroBuddies_DoNotEnterNormalMinionPool()
        {
            var heroes = HeroCatalogLoader.LoadFromResources();
            var minions = MinionCatalogLoader.LoadFromResources();
            var buddyIds = new HashSet<string>(heroes.AllBuddies.Select(buddy => buddy.CardId));

            Assert.IsFalse(minions.All.Any(minion => buddyIds.Contains(minion.CardId)));
        }
    }
}
