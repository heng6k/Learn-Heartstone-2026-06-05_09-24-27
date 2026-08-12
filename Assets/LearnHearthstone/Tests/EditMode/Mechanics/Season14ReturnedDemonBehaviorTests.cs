using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14ReturnedDemonBehaviorTests
    {
        private const string WrathWeaverCardId = "BGS_004";
        private const string SoulRewinderCardId = "BG26_174";

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void MindMuck_SelectedDemonConsumesOneTavernMinion(bool golden, int multiplier)
        {
            var service = CreateService();
            var selected = Minion("mind-muck-target", "MIND_MUCK_TARGET", 5, 7, Tribe.Demon);
            service.State.Player.Board.Add(selected);
            service.State.Player.Tavern.Shop.Add(Minion("mind-muck-food", "MIND_MUCK_FOOD", 2, 3, Tribe.Beast));
            service.State.Player.Tavern.Hand.Add(CreateCatalogMinion(service, "POOL-D23A", "mind-muck", golden));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(5 + (2 * multiplier), selected.Attack);
            Assert.AreEqual(7 + (3 * multiplier), selected.MaxHealth);
            Assert.IsNull(service.State.Player.Tavern.Shop[0]);
        }

        [Test]
        public void MindMuck_RejectsNonDemonTarget()
        {
            var service = CreateService();
            service.State.Player.Board.Add(Minion("mind-muck-invalid", "MIND_MUCK_INVALID", 5, 7, Tribe.Beast));
            service.State.Player.Tavern.Shop.Add(Minion("mind-muck-food", "MIND_MUCK_FOOD", 2, 3, Tribe.Beast));
            service.State.Player.Tavern.Hand.Add(CreateCatalogMinion(service, "POOL-D23A", "mind-muck", false));

            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0)));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void InsatiableUrzul_AfterDemonPlayedConsumesOneTavernMinion(bool golden, int multiplier)
        {
            var service = CreateService();
            var urzul = CreateCatalogMinion(service, "POOL-D23B", "urzul", golden);
            service.State.Player.Board.Add(urzul);
            service.State.Player.Tavern.Shop.Add(Minion("urzul-food", "URZUL_FOOD", 2, 3, Tribe.Beast));
            service.State.Player.Tavern.Hand.Add(Minion("played-demon", "PLAYED_DEMON", 1, 1, Tribe.Demon));
            var attackBefore = urzul.Attack;
            var healthBefore = urzul.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(attackBefore + (2 * multiplier), urzul.Attack);
            Assert.AreEqual(healthBefore + (3 * multiplier), urzul.MaxHealth);
            Assert.IsNull(service.State.Player.Tavern.Shop[0]);
        }

        [TestCase(false, 3, 2)]
        [TestCase(true, 6, 4)]
        public void Tichondrius_WhenHeroTakesDamageBuffsFriendlyDemons(
            bool golden,
            int bonusAttack,
            int bonusHealth)
        {
            var service = CreateService();
            var tichondrius = CreateCatalogMinion(service, "POOL-D23C", "tichondrius", golden);
            var bystander = Minion("tichondrius-demon", "TICHONDRIUS_DEMON", 5, 7, Tribe.Demon);
            var neutral = Minion("tichondrius-neutral", "TICHONDRIUS_NEUTRAL", 4, 6, Tribe.Beast);
            service.State.Player.Board.Add(tichondrius);
            service.State.Player.Board.Add(Minion("wrath-weaver", WrathWeaverCardId, 1, 3, Tribe.Demon));
            service.State.Player.Board.Add(bystander);
            service.State.Player.Board.Add(neutral);
            service.State.Player.Tavern.Hand.Add(Minion("damage-trigger", "DAMAGE_TRIGGER", 1, 1, Tribe.Demon));
            var tichondriusAttack = tichondrius.Attack;
            var tichondriusHealth = tichondrius.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(29, service.State.Player.Health);
            Assert.AreEqual(tichondriusAttack + bonusAttack, tichondrius.Attack);
            Assert.AreEqual(tichondriusHealth + bonusHealth, tichondrius.MaxHealth);
            Assert.AreEqual(5 + bonusAttack, bystander.Attack);
            Assert.AreEqual(7 + bonusHealth, bystander.MaxHealth);
            Assert.AreEqual(4, neutral.Attack);
            Assert.AreEqual(6, neutral.MaxHealth);
        }

        [Test]
        public void Tichondrius_DoesNotTriggerWhenSoulRewinderPreventsDamage()
        {
            var service = CreateService();
            var tichondrius = CreateCatalogMinion(service, "POOL-D23C", "tichondrius-prevented", false);
            var bystander = Minion("prevented-demon", "PREVENTED_DEMON", 5, 7, Tribe.Demon);
            service.State.Player.Board.Add(tichondrius);
            service.State.Player.Board.Add(Minion("wrath-weaver", WrathWeaverCardId, 1, 3, Tribe.Demon));
            service.State.Player.Board.Add(Minion("soul-rewinder", SoulRewinderCardId, 3, 1, Tribe.Demon));
            service.State.Player.Board.Add(bystander);
            service.State.Player.Tavern.Hand.Add(Minion("prevented-trigger", "PREVENTED_TRIGGER", 1, 1, Tribe.Demon));
            var tichondriusAttack = tichondrius.Attack;
            var tichondriusHealth = tichondrius.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(30, service.State.Player.Health);
            Assert.AreEqual(tichondriusAttack, tichondrius.Attack);
            Assert.AreEqual(tichondriusHealth, tichondrius.MaxHealth);
            Assert.AreEqual(5, bystander.Attack);
            Assert.AreEqual(7, bystander.MaxHealth);
        }

        private static MatchService CreateService()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var service = MatchService.CreateWithResolvedVersion(
                resolved,
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    EnableQuests = false,
                    EnableTrinkets = false,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                });
            service.State.Phase = MatchPhase.Tavern;
            service.State.ActiveTribes = new List<Tribe> { Tribe.Demon, Tribe.Beast };
            service.State.Player.Tavern.Tier = 6;
            service.State.Player.Health = 30;
            service.State.Player.Armor = 0;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.DelayedObjectStates.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
            return service;
        }

        private static MinionInstance CreateCatalogMinion(
            MatchService service,
            string researchKey,
            string suffix,
            bool golden)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == researchKey);
            return MinionFactory.Create(definition, BoardSide.Player, suffix, golden, PoolSource.Copy, 0);
        }

        private static MinionInstance Minion(
            string instanceId,
            string cardId,
            int attack,
            int health,
            Tribe tribe)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = instanceId,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Owner = BoardSide.Player,
                CanAttack = true,
                TavernTier = 1,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>(),
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
        }
    }
}
