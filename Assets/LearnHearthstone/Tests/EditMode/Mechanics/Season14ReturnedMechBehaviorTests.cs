using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14ReturnedMechBehaviorTests
    {
        [TestCase(false, 3, 1)]
        [TestCase(true, 6, 2)]
        public void MechagnomeInterpreter_BuffsMechPlayedFromHand(bool golden, int bonusAttack, int bonusHealth)
        {
            var service = CreateService();
            service.State.Player.Board.Add(CreateCatalogMinion(service, "POOL-D20A", "interpreter", golden));
            var target = Minion("played-mech", 2, 3);
            service.State.Player.Tavern.Hand.Add(target);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(2 + bonusAttack, target.Attack);
            Assert.AreEqual(3 + bonusHealth, target.MaxHealth);
        }

        [TestCase(false, 3, 1)]
        [TestCase(true, 6, 2)]
        public void MechagnomeInterpreter_BuffsMagnetizedMech(bool golden, int bonusAttack, int bonusHealth)
        {
            var service = CreateService();
            service.State.Player.Board.Add(CreateCatalogMinion(service, "POOL-D20A", "interpreter", golden));
            var target = Minion("magnetized-mech", 5, 7);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Hand.Add(Magnetic("magnetic-source", 2, 3));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));

            Assert.AreEqual(5 + 2 + bonusAttack, target.Attack);
            Assert.AreEqual(7 + 3 + bonusHealth, target.MaxHealth);
        }

        [TestCase(false, 4)]
        [TestCase(true, 8)]
        public void UtilityDrone_EndOfTurnBuffsEachRecordedMagnetization(bool golden, int bonusPerMagnetization)
        {
            var service = CreateService();
            service.State.Player.Board.Add(CreateCatalogMinion(service, "POOL-D20B", "utility-drone", golden));
            var target = Minion("twice-magnetized", 2, 3);
            var untouched = Minion("not-magnetized", 7, 9);
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(untouched);

            service.State.Player.Tavern.Hand.Add(Magnetic("first-magnetic", 1, 2));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));
            service.State.Player.Tavern.Hand.Add(Magnetic("second-magnetic", 2, 1));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));
            var attackBeforeTurnEnd = target.Attack;
            var healthBeforeTurnEnd = target.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(attackBeforeTurnEnd + (2 * bonusPerMagnetization), target.Attack);
            Assert.AreEqual(healthBeforeTurnEnd + (2 * bonusPerMagnetization), target.MaxHealth);
            Assert.AreEqual(7, untouched.Attack);
            Assert.AreEqual(9, untouched.MaxHealth);
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
            service.State.ActiveTribes = new List<Tribe> { Tribe.Mech };
            service.State.Player.Tavern.Tier = 6;
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

        private static MinionInstance Magnetic(string instanceId, int attack, int health)
        {
            var minion = Minion(instanceId, attack, health);
            minion.Keywords.Add(Keyword.Magnetic);
            minion.OfficialKeywords.Add(Keyword.Magnetic);
            return minion;
        }

        private static MinionInstance Minion(string instanceId, int attack, int health)
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
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.Mech },
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
