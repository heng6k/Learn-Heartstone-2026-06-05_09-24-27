using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14ReturnedElementalBehaviorTests
    {
        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void MoltenRock_PlayElementalGainsHealth(bool golden, int expectedHealthGain)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D15A", "molten-rock", golden);
            service.State.Player.Board.Add(source);

            PlayElemental(service, "molten-rock-trigger");

            Assert.AreEqual(source.BaseAttack, source.Attack);
            Assert.AreEqual(source.BaseHealth + expectedHealthGain, source.MaxHealth);
        }

        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void MeteoriteCrasher_SellElementalGainsStats(bool golden, int expectedGain)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D15B", "meteorite-crasher", golden);
            var sold = Minion("sold-elemental", 1, 1, Tribe.Elemental);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(sold);

            service.Apply(new GameCommand(GameCommandType.SellMinion, sold.InstanceId));

            Assert.AreEqual(source.BaseAttack + expectedGain, source.Attack);
            Assert.AreEqual(source.BaseHealth + expectedGain, source.MaxHealth);
        }

        [TestCase(false, 6, 3)]
        [TestCase(true, 12, 6)]
        public void FlourishingFrostling_ElementalsPlayedPersistAcrossTurnsAndApplyToLaterCopies(
            bool golden,
            int expectedAttack,
            int expectedHealth)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D15C", "frostling-board", golden);
            service.State.Player.Board.Add(source);

            PlayElemental(service, "frostling-trigger-one");
            PlayElemental(service, "frostling-trigger-two");

            Assert.AreEqual(expectedAttack, source.Attack);
            Assert.AreEqual(expectedHealth, source.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));
            service.State.Player.Tavern.Shop.Clear();
            var laterCopy = CreateCatalogMinion(service, "POOL-D15C", "frostling-later", golden);
            service.State.Player.Tavern.Shop.Add(laterCopy);
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(expectedAttack, laterCopy.Attack);
            Assert.AreEqual(expectedHealth, laterCopy.MaxHealth);
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
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            return service;
        }

        private static void PlayElemental(MatchService service, string suffix)
        {
            var elemental = Minion(suffix, 1, 1, Tribe.Elemental);
            service.State.Player.Tavern.Hand.Add(elemental);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
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

        private static MinionInstance Minion(string instanceId, int attack, int health, Tribe tribe)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Owner = BoardSide.Player,
                CanAttack = true,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>()
            };
        }
    }
}
