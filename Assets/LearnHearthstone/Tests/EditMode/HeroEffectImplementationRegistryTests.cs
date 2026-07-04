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
        public void Registry_ExposesImplementedPhaseTwoAndDeferredStatuses()
        {
            var omu = HeroEffectImplementationRegistry.FindByHeroCardId("TB_BaconShop_HERO_74");
            var marin = HeroEffectImplementationRegistry.FindByHeroPowerCardId("BG30_HERO_304p");

            Assert.AreEqual(HeroEffectImplementationStatus.Implemented, omu.Status);
            Assert.AreEqual("Evergreen Botani", omu.BuddyName);
            Assert.AreEqual(HeroEffectImplementationStatus.FrameworkFirst, marin.Status);
            Assert.IsTrue(marin.Note.Contains("Trinket"));
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
        public void MatchStart_LogsVisibleHeroImplementationStatusForDeferredPairs()
        {
            var service = MatchService.CreateWithDefaultCatalog(9001, setup: new MatchSetupOptions { SelectedHeroCardId = "BG30_HERO_304" });

            var statusLog = service.State.Player.Tavern.RecruitLog.LastOrDefault(entry => entry.Message.StartsWith("英雄效果状态:"));

            Assert.IsNotNull(statusLog);
            Assert.That(statusLog.Message, Does.Contain("Marin the Manager"));
            Assert.That(statusLog.Message, Does.Contain("FrameworkFirst"));
            Assert.That(statusLog.Message, Does.Contain("Fantastic Bellhop"));
        }
    }
}
