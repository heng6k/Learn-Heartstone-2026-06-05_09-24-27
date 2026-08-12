using System;
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
    public sealed class Season14ReturnedQuilboarBehaviorTests
    {
        private const string GemTrainingCardId = "116596";

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void SanguineRefiner_RallyImprovesPlayerBloodGemsForTheGame(bool golden, int expectedBonus)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D08", "sanguine-refiner", golden);
            source.Health = source.MaxHealth = 100;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(Minion("sanguine-filler", BoardSide.Player, 0, 100, Tribe.None));
            service.State.Opponent.Board.Add(Minion("sanguine-wall", BoardSide.Opponent, 0, 100, Tribe.None, Keyword.Taunt));

            RunOneAttack(service, 7301);

            Assert.AreEqual(expectedBonus, service.State.Player.Tavern.BloodGemBonusAttack);
            Assert.AreEqual(expectedBonus, service.State.Player.Tavern.BloodGemBonusHealth);
        }

        [Test]
        public void SanguineRefiner_OpponentGrowthStaysIndependentAndLoadsIntoLaterSimulation()
        {
            var service = CreateService();
            ConfigureOpponentRally(service, "sanguine-opponent-first");

            RunOneAttack(service, 7302);

            Assert.AreEqual(0, service.State.Player.Tavern.BloodGemBonusAttack);
            Assert.AreEqual(1, service.GetOpponentCombatTavernStatePreview().BloodGemBonusAttack);
            Assert.AreEqual(1, service.GetOpponentCombatTavernStatePreview().BloodGemBonusHealth);

            ResetCombat(service);
            ConfigureOpponentRally(service, "sanguine-opponent-second");
            RunOneAttack(service, 7303);

            Assert.AreEqual(2, service.GetOpponentCombatTavernStatePreview().BloodGemBonusAttack);
            Assert.AreEqual(2, service.GetOpponentCombatTavernStatePreview().BloodGemBonusHealth);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void Roadboar_RallyAddsBloodGemsToHand(bool golden, int expectedGems)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D09", "roadboar", golden);
            source.Health = source.MaxHealth = 100;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(Minion("roadboar-filler", BoardSide.Player, 0, 100, Tribe.None));
            service.State.Opponent.Board.Add(Minion("roadboar-wall", BoardSide.Opponent, 0, 100, Tribe.None, Keyword.Taunt));

            RunOneAttack(service, 7304);

            Assert.AreEqual(expectedGems, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
        }

        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void CraterMiner_BloodGemChoiceAddsExpectedCards(bool golden, int expectedGems)
        {
            var service = CreateService();
            PlayCatalogMinion(service, "POOL-D10", "crater-gems", golden);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(expectedGems, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void CraterMiner_GemTrainingChoiceAddsZeroCostDerivedCards(bool golden, int expectedCards)
        {
            var service = CreateService();
            PlayCatalogMinion(service, "POOL-D10", "crater-training", golden);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 1));

            var cards = service.State.Player.Tavern.Hand.Where(card => card.CardId == GemTrainingCardId).ToList();
            Assert.AreEqual(expectedCards, cards.Count);
            Assert.IsTrue(cards.All(card => card.Cost == 0));
            Assert.IsTrue(cards.All(card => card.Keywords.Contains(Keyword.ChooseOne)));
        }

        [TestCase(0, 1, 0)]
        [TestCase(1, 0, 1)]
        public void GemTraining_ChooseOneImprovesBloodGemAttackOrHealth(int option, int expectedAttack, int expectedHealth)
        {
            var service = CreateService();
            PlayCatalogMinion(service, "POOL-D10", "crater-training-effect", false);
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 1));
            var trainingIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == GemTrainingCardId);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, trainingIndex));
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, option));

            Assert.AreEqual(expectedAttack, service.State.Player.Tavern.BloodGemBonusAttack);
            Assert.AreEqual(expectedHealth, service.State.Player.Tavern.BloodGemBonusHealth);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void ProdigiousTusker_OtherFriendlyAttackPlaysBloodGemsOnAttacker(bool golden, int expectedGems)
        {
            var service = CreateService();
            var attacker = Minion("tusker-attacker", BoardSide.Player, 1, 100, Tribe.Beast);
            var source = CreateCatalogMinion(service, "POOL-D11", "prodigious-tusker", golden);
            source.CanAttack = false;
            service.State.Player.Board.Add(attacker);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(Minion("tusker-filler", BoardSide.Player, 0, 100, Tribe.None));
            service.State.Opponent.Board.Add(Minion("tusker-wall", BoardSide.Opponent, 0, 100, Tribe.None, Keyword.Taunt));

            RunOneAttack(service, 7305);

            var finalAttacker = FinalPlayer(service, attacker);
            Assert.AreEqual(expectedGems, finalAttacker.Enchantments.Count(StatMath.IsBloodGemEnchantment));
            Assert.AreEqual(attacker.Attack + expectedGems, finalAttacker.Attack);
            Assert.AreEqual(attacker.MaxHealth + expectedGems, finalAttacker.MaxHealth);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void Bonker_RallyPlaysBloodGemsOnAllOtherFriendlyMinions(bool golden, int expectedGems)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D12", "bonker", golden);
            source.Health = source.MaxHealth = 100;
            var first = Minion("bonker-first", BoardSide.Player, 2, 100, Tribe.Quilboar);
            var second = Minion("bonker-second", BoardSide.Player, 3, 100, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);
            service.State.Opponent.Board.Add(Minion("bonker-wall", BoardSide.Opponent, 0, 100, Tribe.None, Keyword.Taunt));

            RunOneAttack(service, 7306);

            Assert.AreEqual(0, FinalPlayer(service, source).Enchantments.Count(StatMath.IsBloodGemEnchantment));
            Assert.AreEqual(expectedGems, FinalPlayer(service, first).Enchantments.Count(StatMath.IsBloodGemEnchantment));
            Assert.AreEqual(expectedGems, FinalPlayer(service, second).Enchantments.Count(StatMath.IsBloodGemEnchantment));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void TuskedCamper_RallyPlaysBloodGemsOnItself(bool golden, int expectedGems)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D14A", "tusked-camper", golden);
            source.Health = source.MaxHealth = 100;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(Minion("camper-filler", BoardSide.Player, 0, 100, Tribe.None));
            service.State.Opponent.Board.Add(Minion("camper-wall", BoardSide.Opponent, 0, 100, Tribe.None, Keyword.Taunt));

            RunOneAttack(service, 7307);

            Assert.AreEqual(expectedGems, FinalPlayer(service, source).Enchantments.Count(StatMath.IsBloodGemEnchantment));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void GemRat_EndTurnAddsGemTraining(bool golden, int expectedCards)
        {
            var service = CreateService();
            service.State.Player.Board.Add(CreateCatalogMinion(service, "POOL-D14B", "gem-rat", golden));

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count(card => card.CardId == GemTrainingCardId));
        }

        [TestCase(false, 3)]
        [TestCase(true, 6)]
        public void RazorfenVineweaver_RallyPermanentlyPlaysBloodGemsOnItself(bool golden, int expectedGems)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D14C", "razorfen-vineweaver", golden);
            source.Health = source.MaxHealth = 100;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(Minion("vineweaver-filler", BoardSide.Player, 0, 100, Tribe.None));
            service.State.Opponent.Board.Add(Minion("vineweaver-wall", BoardSide.Opponent, 0, 100, Tribe.None, Keyword.Taunt));

            RunOneAttack(service, 7308);

            Assert.AreEqual(expectedGems, FinalPlayer(service, source).Enchantments.Count(StatMath.IsBloodGemEnchantment));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void TurboHogrider_PlayChooseOneCardGemsAllOtherQuilboar(bool golden, int expectedGems)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D14D", "turbo-hogrider", golden);
            var filler = Minion("turbo-filler", BoardSide.Player, 2, 10, Tribe.Quilboar);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(filler);
            var crater = PlayCatalogMinion(service, "POOL-D10", "turbo-crater", false);

            Assert.AreEqual(0, source.Enchantments.Count(StatMath.IsBloodGemEnchantment));
            Assert.AreEqual(expectedGems, filler.Enchantments.Count(StatMath.IsBloodGemEnchantment));
            Assert.AreEqual(expectedGems, crater.Enchantments.Count(StatMath.IsBloodGemEnchantment));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void ThornedTrailblazer_FirstChooseOneCardsUseBothEffectsAndResetEachTurn(bool golden, int bothEffectCards)
        {
            var service = CreateService();
            service.State.Player.Board.Add(CreateCatalogMinion(service, "POOL-D13", "thorned-trailblazer", golden));

            for (var index = 0; index < bothEffectCards + 1; index += 1)
            {
                PlayCatalogMinion(service, "POOL-D10", "trailblazer-crater-" + index, false);
                service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            }

            Assert.AreEqual(bothEffectCards, service.State.Player.Tavern.Hand.Count(card => card.CardId == GemTrainingCardId));

            service.State.Player.Tavern.Hand.RemoveAll(card => card.CardId == "BLOOD_GEM");
            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));
            PlayCatalogMinion(service, "POOL-D10", "trailblazer-next-turn", false);
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(bothEffectCards + 1, service.State.Player.Tavern.Hand.Count(card => card.CardId == GemTrainingCardId));
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
            ResetCombat(service);
            return service;
        }

        private static void ConfigureOpponentRally(MatchService service, string suffix)
        {
            var source = CreateCatalogMinion(service, "POOL-D08", suffix, false, BoardSide.Opponent);
            source.Health = source.MaxHealth = 100;
            service.State.Opponent.Board.Add(source);
            service.State.Opponent.Board.Add(Minion(suffix + "-filler", BoardSide.Opponent, 0, 100, Tribe.None));
            service.State.Player.Board.Add(Minion(suffix + "-wall", BoardSide.Player, 0, 100, Tribe.None, Keyword.Taunt));
        }

        private static MinionInstance PlayCatalogMinion(
            MatchService service,
            string researchKey,
            string suffix,
            bool golden)
        {
            var card = CreateCatalogMinion(service, researchKey, suffix, golden);
            service.State.Player.Tavern.Hand.Add(card);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            return card;
        }

        private static MinionInstance CreateCatalogMinion(
            MatchService service,
            string researchKey,
            string suffix,
            bool golden,
            BoardSide owner = BoardSide.Player)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == researchKey);
            return MinionFactory.Create(definition, owner, suffix, golden, PoolSource.Copy, 0);
        }

        private static MinionInstance Minion(
            string instanceId,
            BoardSide owner,
            int attack,
            int health,
            Tribe tribe,
            params Keyword[] keywords)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Owner = owner,
                CanAttack = true,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                OfficialKeywords = keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>()
            };
        }

        private static MinionInstance FinalPlayer(MatchService service, MinionInstance original)
        {
            return service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == original.InstanceId);
        }

        private static void RunOneAttack(MatchService service, int seed)
        {
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = seed, SafetyLimit = 1 }));
        }

        private static void ResetCombat(MatchService service)
        {
            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Hand.Clear();
            service.State.DelayedObjectStates.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
        }
    }
}
