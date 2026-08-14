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
    public sealed class Season14ReturnedNagaBehaviorTests
    {
        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void MiniMyrmidon_SpellcraftAttackExpiresNextTurn(bool golden, int expectedAttack)
        {
            var service = CreateService();
            service.State.Player.Board.Add(CreateCatalogMinion(service, "POOL-D04", "mini-myrmidon", golden));
            var target = Minion("mini-target", 3, 7, Tribe.Beast, BoardSide.Player);
            service.State.Player.Board.Add(target);
            var baseAttack = target.Attack;

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));
            PlayGeneratedSpell(service, "MINI_MYRMIDON_SPELL", target);

            Assert.AreEqual(baseAttack + expectedAttack, target.Attack);
            Assert.AreEqual(7, target.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(baseAttack, target.Attack);
            Assert.AreEqual(7, target.MaxHealth);
        }

        [TestCase(false, 2)]
        [TestCase(true, 4)]
        public void Waverider_SpellcraftBuffsNagaAndTemporaryWindfury(bool golden, int expectedStats)
        {
            var service = CreateService();
            service.State.Player.Board.Add(CreateCatalogMinion(service, "POOL-D05", "waverider", golden));
            var target = Minion("waverider-target", 3, 7, Tribe.Naga, BoardSide.Player);
            service.State.Player.Board.Add(target);
            var baseAttack = target.Attack;
            var baseHealth = target.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));
            PlayGeneratedSpell(service, "WAVERIDER_SPELL", target);

            Assert.AreEqual(baseAttack + expectedStats, target.Attack);
            Assert.AreEqual(baseHealth + expectedStats, target.MaxHealth);
            CollectionAssert.Contains(target.Keywords, Keyword.Windfury);

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(baseAttack, target.Attack);
            Assert.AreEqual(baseHealth, target.MaxHealth);
            CollectionAssert.DoesNotContain(target.Keywords, Keyword.Windfury);
        }

        [TestCase(false, 3)]
        [TestCase(true, 6)]
        public void Thaumaturgist_UsesSpellsCastThisGameGrowth(bool golden, int expectedStats)
        {
            var service = CreateService();
            service.State.Player.Tavern.TavernSpellsCastThisGame = 8;
            service.State.Player.Tavern.AdvancedMechanics.Counters["all_spells_cast_this_game"] = 8;
            service.State.Player.Board.Add(CreateCatalogMinion(service, "POOL-D06A", "thaumaturgist", golden));
            var target = Minion("thaumaturgist-target", 3, 7, Tribe.Beast, BoardSide.Player);
            service.State.Player.Board.Add(target);
            var baseAttack = target.Attack;
            var baseHealth = target.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));
            PlayGeneratedSpell(service, "THAUMATURGIST_SPELL", target);

            Assert.AreEqual(baseAttack + expectedStats, target.Attack);
            Assert.AreEqual(baseHealth + expectedStats, target.MaxHealth);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void FirescaleHoarder_BattlecryAddsShinyRings(bool golden, int expectedRings)
        {
            var service = CreateService();
            service.State.Player.Tavern.Hand.Add(CreateCatalogMinion(service, "POOL-D06C", "firescale-battlecry", golden));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(expectedRings, service.State.Player.Tavern.Hand.Count(card => card.CardId == "109230"));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void FirescaleHoarder_DeathrattleAddsShinyRingsAfterCombat(bool golden, int expectedRings)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, "POOL-D06C", "firescale-deathrattle", golden);
            source.Attack = 0;
            source.Health = 1;
            source.MaxHealth = 1;
            service.State.Player.Board.Add(source);
            service.State.Opponent.Board.Add(Minion("firescale-enemy", 1, 10, Tribe.Pirate, BoardSide.Opponent));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 31, SafetyLimit = 1 }));

            Assert.AreEqual(expectedRings, service.State.Player.Tavern.Hand.Count(card => card.CardId == "109230"));
        }

        [TestCase(false, 4)]
        [TestCase(true, 8)]
        public void ShowyCyclist_DeathrattleBuffsAllNagaUsingSpellGrowth(bool golden, int expectedStats)
        {
            var service = CreateService();
            service.State.Player.Tavern.TavernSpellsCastThisGame = 8;
            service.State.Player.Tavern.AdvancedMechanics.Counters["all_spells_cast_this_game"] = 8;
            var source = CreateCatalogMinion(service, "POOL-D06B", "showy-cyclist", golden);
            source.Attack = 0;
            source.Health = 1;
            source.MaxHealth = 1;
            source.Keywords.Add(Keyword.Taunt);
            var firstNaga = Minion("showy-naga-1", 3, 7, Tribe.Naga, BoardSide.Player);
            var secondNaga = Minion("showy-naga-2", 5, 9, Tribe.Naga, BoardSide.Player);
            var beast = Minion("showy-beast", 4, 8, Tribe.Beast, BoardSide.Player);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(firstNaga);
            service.State.Player.Board.Add(secondNaga);
            service.State.Player.Board.Add(beast);
            service.State.Opponent.Board.Add(Minion("showy-enemy", 100, 100, Tribe.Pirate, BoardSide.Opponent));
            for (var index = 0; index < 4; index += 1)
            {
                var reserve = Minion("showy-enemy-reserve-" + index, 0, 100, Tribe.None, BoardSide.Opponent);
                reserve.CanAttack = false;
                service.State.Opponent.Board.Add(reserve);
            }

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 31, SafetyLimit = 1 }));

            var finalSource = service.State.LastResult.FinalPlayerBoard.FirstOrDefault(card => card.InstanceId == source.InstanceId);
            Assert.IsNull(
                finalSource,
                "Showy Cyclist must die before its Deathrattle can be verified.");
            Assert.IsTrue(
                service.State.LastResult.Replay.Frames.Any(frame =>
                    frame.EventType == CombatEventType.DeathrattleResolved &&
                    frame.ActorId == source.InstanceId),
                "Showy Cyclist death must emit its DeathrattleResolved frame.");
            var finalFirst = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == firstNaga.InstanceId);
            var finalSecond = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == secondNaga.InstanceId);
            var finalBeast = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == beast.InstanceId);
            Assert.AreEqual(firstNaga.Attack + expectedStats, finalFirst.Attack);
            Assert.AreEqual(firstNaga.MaxHealth + expectedStats, finalFirst.MaxHealth);
            Assert.AreEqual(secondNaga.Attack + expectedStats, finalSecond.Attack);
            Assert.AreEqual(secondNaga.MaxHealth + expectedStats, finalSecond.MaxHealth);
            Assert.AreEqual(beast.Attack, finalBeast.Attack);
            Assert.AreEqual(beast.MaxHealth, finalBeast.MaxHealth);
        }

        [TestCase(false, 3)]
        [TestCase(true, 6)]
        public void FaunaWhisperer_EndTurnCastsNaturalBlessingOnBothAdjacentMinions(bool golden, int expectedStats)
        {
            var service = CreateService();
            var left = Minion("fauna-left", 3, 7, Tribe.Beast, BoardSide.Player);
            var source = CreateCatalogMinion(service, "POOL-D06D", "fauna-whisperer", golden);
            var right = Minion("fauna-right", 5, 9, Tribe.Dragon, BoardSide.Player);
            service.State.Player.Board.Add(left);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(right);
            var leftAttack = left.Attack;
            var leftHealth = left.MaxHealth;
            var rightAttack = right.Attack;
            var rightHealth = right.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(leftAttack + expectedStats, left.Attack);
            Assert.AreEqual(leftHealth + expectedStats, left.MaxHealth);
            Assert.AreEqual(rightAttack + expectedStats, right.Attack);
            Assert.AreEqual(rightHealth + expectedStats, right.MaxHealth);
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
            service.State.Player.Tavern.TavernSpellsCastThisTurn = 0;
            service.State.Player.Tavern.TavernSpellsCastThisGame = 0;
            return service;
        }

        private static MinionInstance CreateCatalogMinion(MatchService service, string researchKey, string suffix, bool golden)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == researchKey);
            return MinionFactory.Create(definition, BoardSide.Player, suffix, golden, PoolSource.Copy, 0);
        }

        private static MinionInstance Minion(string instanceId, int attack, int health, Tribe tribe, BoardSide owner)
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
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>()
            };
        }

        private static void PlayGeneratedSpell(MatchService service, string cardId, MinionInstance target)
        {
            var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == cardId);
            Assert.AreNotEqual(-1, spellIndex, "Expected generated Spellcraft card " + cardId + ".");
            var targetIndex = service.State.Player.Board.FindIndex(card => card.InstanceId == target.InstanceId);
            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                spellIndex,
                targetIndex,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified));
        }
    }
}
