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
    public sealed class Season14ReturnedUndeadBehaviorTests
    {
        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void MawCaster_DestroysSelectedFriendlyUndeadAndDiscoversUndead(
            bool golden,
            int expectedPicks)
        {
            var service = CreateService();
            var target = Minion("maw-caster-target", Tribe.Undead);
            service.State.Player.Board.Add(target);
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == "POOL-D02");
            var source = MinionFactory.Create(definition, BoardSide.Player, "maw-caster", golden, PoolSource.Copy, 0);
            service.State.Player.Tavern.Hand.Add(source);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.IsFalse(service.State.Player.Board.Any(card => card.InstanceId == target.InstanceId));
            Assert.AreEqual(
                0,
                service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.Minion),
                "The Battlecry must not add an Undead before a Discover choice is made.");
            for (var pick = 0; pick < expectedPicks; pick += 1)
            {
                Assert.NotNull(service.State.Player.Tavern.Discover);
                Assert.IsTrue(
                    service.State.Player.Tavern.Discover.Options.All(card =>
                        card.Tribes.Contains(Tribe.Undead) || card.Tribes.Contains(Tribe.All)),
                    "Maw Caster candidates: " + string.Join(
                        "; ",
                        service.State.Player.Tavern.Discover.Options.Select(card =>
                            card.CardId + ":" + string.Join(",", card.Tribes))));
                service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
                Assert.AreEqual(
                    pick + 1,
                    service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.Minion),
                    "Each Discover choice must add exactly one Undead. Hand: " +
                    string.Join(", ", service.State.Player.Tavern.Hand.Select(card => card.CardId)));
            }

            Assert.IsNull(service.State.Player.Tavern.Discover);
            var discovered = service.State.Player.Tavern.Hand
                .Where(card => card.CardKind == CardKind.Minion)
                .ToList();
            Assert.AreEqual(expectedPicks, discovered.Count);
            Assert.IsTrue(discovered.All(card =>
                card.Tribes.Contains(Tribe.Undead) || card.Tribes.Contains(Tribe.All)));
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
            service.State.ActiveTribes = new List<Tribe> { Tribe.Undead };
            service.State.Player.Tavern.Tier = 6;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            return service;
        }

        private static MinionInstance Minion(string instanceId, Tribe tribe)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                BaseAttack = 1,
                BaseHealth = 1,
                Attack = 1,
                Health = 1,
                MaxHealth = 1,
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
