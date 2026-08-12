using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14ReturnedPirateBehaviorTests
    {
        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void AzsharanCutlassier_BattlecryImprovesTavernSpellAttackForTheGame(
            bool golden,
            int expectedBonus)
        {
            var service = CreateService();
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == "POOL-D18");
            var source = MinionFactory.Create(definition, BoardSide.Player, "azsharan-cutlassier", golden, PoolSource.Copy, 0);
            service.State.Player.Tavern.Hand.Add(source);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(expectedBonus, service.State.Player.Tavern.TavernSpellBonusAttack);

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(expectedBonus, service.State.Player.Tavern.TavernSpellBonusAttack);
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
            return service;
        }
    }
}
