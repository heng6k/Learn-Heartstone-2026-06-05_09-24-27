using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class HeroEffectImplementationRegistryTests
    {
        [Test]
        public void Registry_ExposesImplementedHeroPowerAndBuddyStatuses()
        {
            var cenarius = HeroEffectImplementationRegistry.FindByHeroPowerCardId("BG32_HERO_001p");
            var malorne = HeroEffectImplementationRegistry.FindByBuddyCardId("BG32_HERO_001_Buddy");

            Assert.AreEqual(HeroEffectImplementationStatus.Implemented, cenarius.Status);
            Assert.AreEqual("Forest Lord Cenarius", cenarius.HeroName);
            Assert.AreEqual(HeroEffectImplementationStatus.Implemented, malorne.Status);
            Assert.AreEqual("Malorne", malorne.BuddyName);
        }

        [Test]
        public void Registry_ExposesImplementedPhaseTwoAndTrinketStatuses()
        {
            var omu = HeroEffectImplementationRegistry.FindByHeroCardId("TB_BaconShop_HERO_74");
            var marin = HeroEffectImplementationRegistry.FindByHeroPowerCardId("BG30_HERO_304p");
            var buttons = HeroEffectImplementationRegistry.FindByHeroPowerCardId("BG32_HERO_002p");

            Assert.AreEqual(HeroEffectImplementationStatus.Implemented, omu.Status);
            Assert.AreEqual("Evergreen Botani", omu.BuddyName);
            Assert.AreEqual(HeroEffectImplementationStatus.Implemented, marin.Status);
            Assert.IsTrue(marin.Note.Contains("Trinket"));
            Assert.AreEqual(HeroEffectImplementationStatus.Implemented, buttons.Status);
            Assert.IsTrue(buttons.Note.Contains("Trinket"));
        }

        [Test]
        public void Registry_MarksA1CombatEventHeroPowersImplemented()
        {
            var implementedHeroPowers = new[]
            {
                "BG22_HERO_000p",
                "BG20_HERO_282p",
                "BG22_HERO_305p",
                "BG22_HERO_001p"
            };

            foreach (var heroPowerCardId in implementedHeroPowers)
            {
                Assert.AreEqual(
                    HeroEffectImplementationStatus.Implemented,
                    HeroEffectImplementationRegistry.FindByHeroPowerCardId(heroPowerCardId).Status,
                    heroPowerCardId);
            }
        }

        [Test]
        public void Registry_MarksMorchieAndMurozondImplemented()
        {
            var morchie = HeroEffectImplementationRegistry.FindByHeroPowerCardId("BG34_HERO_004p");
            var murozond = HeroEffectImplementationRegistry.FindByHeroPowerCardId("BG34_HERO_000p");

            Assert.AreEqual(HeroEffectImplementationStatus.Implemented, morchie.Status);
            Assert.That(morchie.Note, Does.Contain("Minor Timewarped Tavern"));
            Assert.AreEqual(HeroEffectImplementationStatus.Implemented, murozond.Status);
            Assert.That(murozond.Note, Does.Contain("Major Timewarped Tavern"));
            Assert.That(murozond.Note, Does.Contain("Timewarped Tavern data/effect"));
        }

        [Test]
        public void Registry_MarksAcceptedSinglePlayerOpponentProxyHeroesImplemented()
        {
            var scabbs = HeroEffectImplementationRegistry.FindByHeroPowerCardId("BG21_HERO_010p");
            var tess = HeroEffectImplementationRegistry.FindByHeroPowerCardId("TB_BaconShop_HP_077");
            var bigglesworth = HeroEffectImplementationRegistry.FindByHeroPowerCardId("TB_BaconShop_HP_080");

            Assert.AreEqual(HeroEffectImplementationStatus.Implemented, scabbs.Status);
            Assert.That(scabbs.Note, Does.Contain("single-player opponent proxy"));
            Assert.AreEqual(HeroEffectImplementationStatus.Implemented, tess.Status);
            Assert.That(tess.Note, Does.Contain("single-player opponent proxy"));
            Assert.AreEqual(HeroEffectImplementationStatus.FrameworkFirst, bigglesworth.Status);
        }

        [Test]
        public void Registry_ReturnsVisibleUnregisteredStatusForUnknownCards()
        {
            var unknown = HeroEffectImplementationRegistry.FindByHeroPowerCardId("UNKNOWN_HERO_POWER");

            Assert.AreEqual(HeroEffectImplementationStatus.Unregistered, unknown.Status);
            Assert.AreEqual("Unregistered", unknown.HeroName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(unknown.Note));
        }

        [Test]
        public void Registry_EntriesHaveSearchableNotesAndNoDuplicateKnownIds()
        {
            Assert.IsTrue(HeroEffectImplementationRegistry.All.All(entry => !string.IsNullOrWhiteSpace(entry.HeroCardId)));
            Assert.IsTrue(HeroEffectImplementationRegistry.All.All(entry => !string.IsNullOrWhiteSpace(entry.HeroName)));
            Assert.IsTrue(HeroEffectImplementationRegistry.All.All(entry => !string.IsNullOrWhiteSpace(entry.Phase)));
            Assert.IsTrue(HeroEffectImplementationRegistry.All.All(entry => !string.IsNullOrWhiteSpace(entry.Note)));
            Assert.AreEqual(
                HeroEffectImplementationRegistry.All.Count,
                HeroEffectImplementationRegistry.All.Select(entry => entry.HeroCardId).Distinct().Count());
            Assert.AreEqual(
                HeroEffectImplementationRegistry.All.Count,
                HeroEffectImplementationRegistry.All.Select(entry => entry.HeroPowerCardId).Distinct().Count());
            Assert.AreEqual(
                HeroEffectImplementationRegistry.All.Where(entry => !string.IsNullOrWhiteSpace(entry.BuddyCardId)).Count(),
                HeroEffectImplementationRegistry.All.Where(entry => !string.IsNullOrWhiteSpace(entry.BuddyCardId)).Select(entry => entry.BuddyCardId).Distinct().Count());
        }

        [Test]
        public void Registry_CurrentRemainingStatusCountsMatchP0Baseline()
        {
            var counts = HeroEffectImplementationRegistry.All
                .GroupBy(entry => entry.Status)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.AreEqual(113, counts[HeroEffectImplementationStatus.Implemented]);
            Assert.AreEqual(1, counts[HeroEffectImplementationStatus.FrameworkFirst]);
            Assert.IsFalse(counts.ContainsKey(HeroEffectImplementationStatus.Planned));
            Assert.IsFalse(counts.ContainsKey(HeroEffectImplementationStatus.Deferred));
        }

        [Test]
        public void Registry_MarksLargeSystemsImplementedButLeavesBigglesworthExcluded()
        {
            Assert.AreEqual(
                HeroEffectImplementationStatus.Implemented,
                HeroEffectImplementationRegistry.FindByHeroPowerCardId("BG31_HERO_801p").Status);
            Assert.AreEqual(
                HeroEffectImplementationStatus.Implemented,
                HeroEffectImplementationRegistry.FindByHeroPowerCardId("BG31_HERO_802p").Status);
            Assert.AreEqual(
                HeroEffectImplementationStatus.Implemented,
                HeroEffectImplementationRegistry.FindByHeroPowerCardId("BG31_HERO_811p").Status);

            var putricide = HeroEffectImplementationRegistry.FindByHeroPowerCardId("BG25_HERO_100p");
            Assert.AreEqual(HeroEffectImplementationStatus.Implemented, putricide.Status);
            Assert.That(putricide.Note, Does.Contain("two sequential 3-option component Discovers"));

            var bigglesworth = HeroEffectImplementationRegistry.FindByHeroPowerCardId("TB_BaconShop_HP_080");
            Assert.AreEqual(HeroEffectImplementationStatus.FrameworkFirst, bigglesworth.Status);
        }

        [Test]
        public void Registry_CoversEveryHeroFromSourceData()
        {
            var heroes = HeroCatalogLoader.LoadFromResources().AllHeroes
                .Where(hero => !string.IsNullOrWhiteSpace(hero.HeroCardId))
                .ToList();
            var missing = heroes
                .Where(hero => HeroEffectImplementationRegistry.FindByHeroCardId(hero.HeroCardId).Status == HeroEffectImplementationStatus.Unregistered)
                .Select(hero => hero.HeroCardId + " " + hero.Name)
                .ToList();

            Assert.IsEmpty(missing, string.Join(", ", missing));
            Assert.AreEqual(
                heroes.Count,
                HeroEffectImplementationRegistry.All.Count(entry =>
                    heroes.Any(hero => string.Equals(hero.HeroCardId, entry.HeroCardId, System.StringComparison.OrdinalIgnoreCase))));
        }

        [Test]
        public void MatchStart_LogsVisibleHeroImplementationStatusForFrameworkFirstPairs()
        {
            var service = MatchService.CreateWithDefaultCatalog(9001, setup: new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_70" });

            var statusLog = service.State.Player.Tavern.RecruitLog.LastOrDefault(entry => entry.Message.StartsWith("英雄效果状态:"));

            statusLog = statusLog ?? service.State.Player.Tavern.RecruitLog.LastOrDefault(entry => entry.Message.StartsWith("Hero effect status:"));
            Assert.IsNotNull(statusLog);
            Assert.That(statusLog.Message, Does.Contain("Mr. Bigglesworth"));
            Assert.That(statusLog.Message, Does.Contain("FrameworkFirst"));
            Assert.That(statusLog.Message, Does.Contain("Lil' K.T."));
        }
    }
}
