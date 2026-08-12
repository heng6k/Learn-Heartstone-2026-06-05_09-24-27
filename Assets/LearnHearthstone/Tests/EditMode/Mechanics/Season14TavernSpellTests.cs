using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14TavernSpellTests
    {
        [Test]
        public void MethodicalMadness_ConsumesExactlyTwoShopMinionsAndTransfersStatsAndKeywords()
        {
            var service = CreateService();
            var target = Minion("demon", 2, 3, Tribe.Demon);
            service.State.Player.Board.Add(target);
            var shopA = Minion("shop-a", 3, 4, Tribe.Beast, Keyword.Taunt);
            var shopB = Minion("shop-b", 5, 6, Tribe.Dragon, Keyword.DivineShield);
            var shopC = Minion("shop-c", 7, 8, Tribe.Mech, Keyword.Reborn);
            shopA.OfficialKeywords.Clear();
            shopB.OfficialKeywords.Clear();
            shopC.OfficialKeywords.Clear();
            service.State.Player.Tavern.Shop = new List<MinionInstance> { shopA, shopB, shopC };
            AddSpell(service, "132903");

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            var survivors = service.State.Player.Tavern.Shop.Where(card => card != null).ToList();
            Assert.AreEqual(1, survivors.Count);
            var consumed = new[] { "shop-a", "shop-b", "shop-c" }
                .Where(id => survivors.All(card => card.InstanceId != id))
                .Select(id => id == "shop-a" ? (Attack: 3, Health: 4, Keyword: Keyword.Taunt) :
                              id == "shop-b" ? (Attack: 5, Health: 6, Keyword: Keyword.DivineShield) :
                                               (Attack: 7, Health: 8, Keyword: Keyword.Reborn))
                .ToList();
            Assert.AreEqual(2 + consumed.Sum(item => item.Attack), target.Attack);
            Assert.AreEqual(3 + consumed.Sum(item => item.Health), target.MaxHealth);
            Assert.IsTrue(consumed.All(item => target.Keywords.Contains(item.Keyword)));
        }

        [Test]
        public void MightyDragonbreath_RepeatsForDragonAndDivineShieldIndependently()
        {
            var service = CreateService();
            var both = Minion("both", 1, 1, Tribe.Dragon, Keyword.DivineShield);
            var dragon = Minion("dragon", 1, 1, Tribe.Dragon);
            var shield = Minion("shield", 1, 1, Tribe.Murloc, Keyword.DivineShield);
            var neither = Minion("neither", 1, 1, Tribe.Murloc);
            service.State.Player.Board.AddRange(new[] { both, dragon, shield, neither });
            AddSpell(service, "132995");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(4, both.Attack);
            Assert.AreEqual(3, dragon.Attack);
            Assert.AreEqual(3, shield.Attack);
            Assert.AreEqual(2, neither.Attack);
            Assert.AreEqual(4, both.MaxHealth);
        }

        [Test]
        public void RepairJobAndWeaponsForge_GeneratesAndPlaysLegacyPointyArrow()
        {
            var service = CreateService();
            var target = Minion("repair", 2, 3, Tribe.Mech);
            service.State.Player.Board.Add(target);
            AddSpell(service, "133711");

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.AreEqual(6, target.Attack);
            Assert.AreEqual(11, target.MaxHealth);

            AddSpell(service, "133371");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == "100596"));

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(10, target.Attack);
        }

        [Test]
        public void WinnersBread_AppliesImmediateBuffAndWinOnlyFollowUpAtNextTurnStart()
        {
            var service = CreateService();
            var target = Minion("winner", 20, 20, Tribe.Dragon);
            service.State.Player.Board.Add(target);
            service.State.Opponent.Board.Add(Minion("opponent", 1, 1, Tribe.Murloc));
            AddSpell(service, "133369");

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.AreEqual(22, target.Attack);
            Assert.AreEqual(23, target.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(26, target.Attack);
            Assert.AreEqual(29, target.MaxHealth);
            Assert.AreEqual(2, service.State.Round);
        }

        [TestCase(GameVersionIds.LegacyCompositeSandbox, 6)]
        [TestCase(GameVersionIds.Season14Preview, 7)]
        public void DefenderRites_UsesLockedVersionAmount(string versionId, int expectedAmount)
        {
            var service = CreateService(versionId);
            var target = Minion("defender", 2, 3, Tribe.Mech);
            service.State.Player.Board.Add(target);
            AddSpell(service, "104445");

            PlaySpell(service, target, "buff");

            Assert.AreEqual(2 + expectedAmount, target.Attack);
            Assert.AreEqual(3 + expectedAmount, target.MaxHealth);
            Assert.Contains(Keyword.Taunt, target.Keywords);
        }

        [TestCase("attack", 4, 2)]
        [TestCase("health", 2, 4)]
        public void AllianceFlag_ResolvesSelectedStatPair(string choiceId, int expectedAttack, int expectedHealth)
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            var target = Minion("alliance", 1, 1, Tribe.Murloc);
            service.State.Player.Board.Add(target);
            AddSpell(service, "117567");

            PlaySpell(service, target, choiceId);

            Assert.AreEqual(expectedAttack, target.Attack);
            Assert.AreEqual(expectedHealth, target.MaxHealth);
        }

        [Test]
        public void ForestsBounty_SingleTargetChoiceTriggersPlusSixPlusSixTwice()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            var target = Minion("forest-single", 1, 1, Tribe.Beast);
            service.State.Player.Board.Add(target);
            AddSpell(service, "117584");

            PlaySpell(service, target, "single");

            Assert.AreEqual(13, target.Attack);
            Assert.AreEqual(13, target.MaxHealth);
            Assert.AreEqual(2, target.Enchantments.Count(enchantment => enchantment.SourceId == "Forest's Bounty"));
        }

        [Test]
        public void ForestsBounty_BoardChoiceBuffsEveryFriendlyMinionOnce()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            var first = Minion("forest-board-a", 1, 1, Tribe.Beast);
            var second = Minion("forest-board-b", 2, 3, Tribe.Quilboar);
            service.State.Player.Board.AddRange(new[] { first, second });
            AddSpell(service, "117584");

            PlaySpell(service, first, "board");

            Assert.AreEqual(3, first.Attack);
            Assert.AreEqual(3, first.MaxHealth);
            Assert.AreEqual(4, second.Attack);
            Assert.AreEqual(5, second.MaxHealth);
        }

        [TestCase("minion", CardKind.Minion)]
        [TestCase("spell", CardKind.TavernSpell)]
        public void BoundlessPotential_DiscoversSelectedCurrentTierCardKind(string choiceId, CardKind expectedKind)
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            service.State.Player.Tavern.Tier = 4;
            AddSpell(service, "115910");

            PlaySpell(service, null, choiceId);

            Assert.NotNull(service.State.Player.Tavern.Discover);
            Assert.IsNotEmpty(service.State.Player.Tavern.Discover.Options);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(option => option.CardKind == expectedKind));
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(option => option.TavernTier == 4));
        }

        [Test]
        public void FandralsFortune_DiscoveredChooseOneCardKeepsBothEffectsTagInHand()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            AddSpell(service, "116221");

            PlaySpell(service, null, null);

            var discover = service.State.Player.Tavern.Discover;
            Assert.NotNull(discover);
            Assert.IsNotEmpty(discover.Options);
            Assert.IsTrue(discover.Options.All(option => option.Tags.Contains("choose_one")));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Single().Tags.Contains("choose_one_both_effects"));
        }

        [TestCase("attack", 1, 0)]
        [TestCase("health", 0, 1)]
        public void GemDay_OfficialStableIdUsesExistingGeneratedChooseOnePath(
            string choiceId,
            int expectedAttack,
            int expectedHealth)
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            AddSpell(service, "116596");

            PlaySpell(service, null, choiceId);
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, choiceId == "attack" ? 0 : 1));

            Assert.AreEqual(expectedAttack, service.State.Player.Tavern.BloodGemBonusAttack);
            Assert.AreEqual(expectedHealth, service.State.Player.Tavern.BloodGemBonusHealth);
        }

        private static MatchService CreateService()
        {
            var service = MatchService.CreateWithDefaultCatalog(24680, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            return service;
        }

        private static MatchService CreateService(string versionId)
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(versionId, snapshot);
            var service = MatchService.CreateWithResolvedVersion(
                resolved,
                24680,
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
            Assert.AreEqual(versionId, service.State.GameVersionId);
            return service;
        }

        private static void AddSpell(MatchService service, string cardNumber)
        {
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardNumber, CardKind.TavernSpell));
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(cardNumber, service.State.Player.Tavern.Hand[0].CardId);
            Assert.AreEqual(CardKind.TavernSpell, service.State.Player.Tavern.Hand[0].CardKind);
        }

        private static void PlaySpell(MatchService service, MinionInstance target, string choiceId)
        {
            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                target == null ? -1 : 0,
                target == null ? TargetZone.Unspecified : TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target?.InstanceId,
                choiceId: choiceId));
        }

        private static MinionInstance Minion(string id, int attack, int health, Tribe tribe, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                InstanceId = id,
                DefinitionId = id,
                CardId = id,
                Name = id,
                CardKind = CardKind.Minion,
                Owner = BoardSide.Player,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(keywords),
                OfficialKeywords = new List<Keyword>(keywords),
                PoolSource = PoolSource.Debug
            };
        }
    }
}
