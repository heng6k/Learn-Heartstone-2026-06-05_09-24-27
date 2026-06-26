using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class HeroSetupAndUnmaskedIdentityTests
    {
        [Test]
        public void CreateWithDefaultCatalog_DefaultsPatchwerkToSixtyHealth()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

            Assert.AreEqual("TB_BaconShop_HERO_34", service.State.Player.HeroId);
            Assert.AreEqual(60, service.State.Player.Health);
            Assert.AreEqual(60, service.State.Player.MaxHealth);
            Assert.AreEqual(0, service.State.Player.Armor);
        }

        [Test]
        public void CreateWithDefaultCatalog_SelectedHeroInitializesHealthArmorAndHeroPower()
        {
            var baseline = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var hero = baseline.HeroCatalog.AllHeroes.First(candidate => candidate.HeroPower != null);

            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { SelectedHeroCardId = hero.HeroCardId });

            Assert.AreEqual(hero.HeroCardId, service.State.Player.HeroId);
            Assert.AreEqual(hero.Health, service.State.Player.Health);
            Assert.AreEqual(hero.Health, service.State.Player.MaxHealth);
            Assert.AreEqual(hero.Armor, service.State.Player.Armor);
            Assert.AreEqual(hero.HeroPower.CardId, service.State.Player.HeroPowerCardId);
        }

        [Test]
        public void UnmaskedIdentity_StartsHeroPowerDiscoverAndChoiceUpdatesCurrentPower()
        {
            var service = CreateServiceWithHeroPower();
            var startingPower = service.State.Player.HeroPowerCardId;
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "100910", CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            var discover = service.State.Player.Tavern.Discover;
            Assert.IsNotNull(discover);
            Assert.AreEqual("hero-power:unmasked-identity", discover.Source);
            Assert.AreEqual(3, discover.Options.Count);
            Assert.IsTrue(discover.Options.All(option => option.CardKind == CardKind.HeroPower));
            Assert.IsFalse(discover.Options.Any(option => option.CardId == startingPower));
            Assert.IsTrue(discover.Options.All(option =>
                service.HeroCatalog.GetHeroPowerByCardId(option.CardId).ReplacementEligibility ==
                HeroPowerReplacementEligibility.DiscoverableAfterStart));

            var picked = discover.Options[0];
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(picked.CardId, service.State.Player.HeroPowerCardId);
            Assert.IsNull(service.State.Player.Tavern.Discover);
        }

        [Test]
        public void UnmaskedIdentity_DebugCastAcceptsOfficialCardIdAlias()
        {
            var service = CreateServiceWithHeroPower();

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, "EBG_Spell_037", CardKind.TavernSpell, -1));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual("hero-power:unmasked-identity", service.State.Player.Tavern.Discover.Source);
        }

        [Test]
        public void HeroBuddy_DebugAddCreatesHeroBuddyHandCard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var buddy = service.HeroCatalog.AllBuddies.First();
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, buddy.CardId, CardKind.HeroBuddy));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            var added = service.State.Player.Tavern.Hand[0];
            Assert.AreEqual(CardKind.HeroBuddy, added.CardKind);
            Assert.AreEqual(buddy.CardId, added.CardId);
            Assert.AreEqual(PoolSource.Debug, added.PoolSource);
            Assert.AreEqual(0, added.PoolCopiesHeld);
        }

        [Test]
        public void Hero_DebugAddSetsCurrentHeroWithoutUsingHandSpace()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var reno = service.HeroCatalog.GetHeroByCardId("TB_BaconShop_HERO_41");
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, reno.HeroCardId, CardKind.Hero));

            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(reno.HeroCardId, service.State.Player.HeroId);
            Assert.AreEqual(reno.Health, service.State.Player.Health);
            Assert.AreEqual(reno.Health, service.State.Player.MaxHealth);
            Assert.AreEqual(reno.Armor, service.State.Player.Armor);
            Assert.AreEqual(reno.HeroPower.CardId, service.State.Player.HeroPowerCardId);
        }

        private static MatchService CreateServiceWithHeroPower()
        {
            var baseline = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var hero = baseline.HeroCatalog.AllHeroes.First(candidate =>
                candidate.HeroPower != null &&
                candidate.HeroPower.ReplacementEligibility == HeroPowerReplacementEligibility.DiscoverableAfterStart);

            return MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { SelectedHeroCardId = hero.HeroCardId });
        }
    }
}
