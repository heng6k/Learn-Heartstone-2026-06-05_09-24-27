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
    public sealed class Season14PirateCarrierTests
    {
        private const string ShipwreckPirateKey = "MIN-R17";
        private const string TreasureParrotKey = "MIN-R18";
        private const string AmbitiousDeserterKey = "MIN-R19";
        private const string MaritimeExtortionistKey = "MIN-R20";
        private const string CaptainCookieKey = "MIN-R21";
        private const string QuietCourierKey = "MIN-R22";
        private const string PlunderMasterHooktuskKey = "MIN-R23";
        private const string ShipwreckPirateActionId = "activate:min-r17";
        private const string GoldenTouchCardNumber = "104448";
        private const string ChefsChoiceCardNumber = "105664";

        [Test]
        public void EmbeddedCatalog_DefinesPirateActionAndGoldenCards()
        {
            var minions = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").Chinese.Minions.All;
            var shipwreckPirate = minions.Single(item => item.ResearchKey == ShipwreckPirateKey);
            var action = shipwreckPirate.RecruitActions.Single();

            Assert.AreEqual(ShipwreckPirateActionId, action.ActionId);
            Assert.AreEqual("season14.activate.min-r17@1", action.ResolverId);
            Assert.AreEqual(2, action.CostSpec.Gold);
            Assert.AreEqual(RecruitActionTargetSpec.None, action.TargetSpec);
            Assert.AreEqual(1, action.UsesPerTurn);
            Assert.AreEqual(MatchPhase.Tavern, action.AllowedPhase);

            AssertPreviewCarrier(shipwreckPirate, 4, 6, "发现2张");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == TreasureParrotKey), 10, 10, "两张点金之触");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == AmbitiousDeserterKey), 12, 12, "提前2回合");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == MaritimeExtortionistKey), 16, 16, "+16/+16");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == CaptainCookieKey), 10, 6, "获取2张");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == QuietCourierKey), 14, 14, "随机获取两张");
            AssertPreviewCarrier(minions.Single(item => item.ResearchKey == PlunderMasterHooktuskKey), 8, 8, "+2/+2");
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void ShipwreckPirate_ActivateDiscoversTavernSpells(bool golden, int expectedPicks)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, ShipwreckPirateKey, "shipwreck", golden);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Gold = 10;

            ActivateShipwreckPirate(service, source);

            Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
            Assert.AreEqual(8, service.State.Player.Tavern.Gold);
            for (var pick = 0; pick < expectedPicks; pick += 1)
            {
                Assert.NotNull(service.State.Player.Tavern.Discover);
                service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            }

            Assert.AreEqual(expectedPicks, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.TavernSpell));
            Assert.IsNull(service.State.Player.Tavern.Discover);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void TreasureParrot_AfterFortyDamageAddsGoldenTouch(bool golden, int expectedCards)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, TreasureParrotKey, "treasure-parrot", golden);
            source.Attack = 20;
            source.Health = source.MaxHealth = 100;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(Minion("parrot-filler", 0, 100, Tribe.None));
            service.State.Opponent.Board.Add(Minion("parrot-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            RunOneAttackCombat(service, 6401);
            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.CardId == GoldenTouchCardNumber));

            service.State.Phase = MatchPhase.Tavern;
            RunOneAttackCombat(service, 6402);

            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count(card => card.CardId == GoldenTouchCardNumber));
        }

        [Test]
        public void TreasureParrot_DivineShieldDoesNotCountAsDamage()
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, TreasureParrotKey, "treasure-parrot-shield", false);
            source.Attack = 40;
            source.Health = source.MaxHealth = 100;
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(Minion("parrot-shield-filler", 0, 100, Tribe.None));
            service.State.Opponent.Board.Add(Minion("parrot-shield-wall", 0, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt, Keyword.DivineShield));

            RunOneAttackCombat(service, 6403);

            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.CardId == GoldenTouchCardNumber));
        }

        [TestCase(false, 4)]
        [TestCase(true, 3)]
        public void AmbitiousDeserter_EveryFiveGoldCreatesThenAcceleratesLockbox(bool golden, int expectedTurns)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, AmbitiousDeserterKey, "ambitious-deserter", golden);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Gold = 20;

            SpendGoldOnRefreshes(service, 5);
            Assert.AreEqual(5, service.State.DelayedObjectStates.Single().RemainingTurns);

            SpendGoldOnRefreshes(service, 5);

            Assert.AreEqual(1, service.State.DelayedObjectStates.Count);
            Assert.AreEqual(expectedTurns, service.State.DelayedObjectStates.Single().RemainingTurns);
        }

        [Test]
        public void MaritimeExtortionist_GoldenPlayUpdatesCopiesOnBoardInHandAndInShop()
        {
            var service = CreateService();
            var boardCopy = CreateCatalogMinion(service, MaritimeExtortionistKey, "extortion-board", false);
            var handCopy = CreateCatalogMinion(service, MaritimeExtortionistKey, "extortion-hand", false);
            var shopCopy = CreateCatalogMinion(service, MaritimeExtortionistKey, "extortion-shop", false);
            service.State.Player.Board.Add(boardCopy);
            service.State.Player.Tavern.Hand.Add(handCopy);
            service.State.Player.Tavern.Shop.Add(shopCopy);
            service.State.Player.Tavern.Hand.Add(Minion("played-golden", 2, 2, Tribe.Beast, golden: true));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 1));

            Assert.AreEqual(boardCopy.BaseAttack + 8, boardCopy.Attack);
            Assert.AreEqual(handCopy.BaseAttack + 8, handCopy.Attack);
            Assert.AreEqual(shopCopy.BaseAttack + 8, shopCopy.Attack);
            Assert.AreEqual(boardCopy.BaseHealth + 8, boardCopy.MaxHealth);
            Assert.AreEqual(handCopy.BaseHealth + 8, handCopy.MaxHealth);
            Assert.AreEqual(shopCopy.BaseHealth + 8, shopCopy.MaxHealth);
        }

        [Test]
        public void GoldenMaritimeExtortionist_CountsItselfAndGainsSixteenStats()
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, MaritimeExtortionistKey, "golden-extortionist", true);
            service.State.Player.Tavern.Hand.Add(source);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(source.BaseAttack + 16, source.Attack);
            Assert.AreEqual(source.BaseHealth + 16, source.MaxHealth);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void CaptainCookie_DeathrattleAddsChefsChoice(bool golden, int expectedCards)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, CaptainCookieKey, "captain-cookie", golden);
            source.Attack = 0;
            source.Health = source.MaxHealth = 1;
            service.State.Player.Board.Add(source);
            service.State.Opponent.Board.Add(Minion("cookie-opponent", 20, 20, Tribe.None, BoardSide.Opponent));

            service.Apply(new GameCommand(GameCommandType.SimulateCombat, new CombatTestOptions { Seed = 777, SafetyLimit = 20 }));

            Assert.AreEqual(expectedCards, service.State.Player.Tavern.Hand.Count(card => card.CardId == ChefsChoiceCardNumber));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void QuietCourier_BattlecryAddsGoldenTierFourCardsWithoutTripleRewards(bool golden, int expectedCards)
        {
            var service = CreateService();
            PlayCatalogMinion(service, QuietCourierKey, "quiet-courier", golden);

            var rewards = service.State.Player.Tavern.Hand
                .Where(card => card.CardKind == CardKind.Minion && card.Golden && card.TavernTier == 4)
                .ToList();
            Assert.AreEqual(expectedCards, rewards.Count);
            Assert.IsTrue(rewards.All(card => card.Counters.TryGetValue("triple-reward-granted", out var granted) && granted == 1));

            var playedReward = rewards[0];
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Hand.Add(playedReward);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.CardId == "TRIPLE_REWARD"));
        }

        [TestCase(false, 3)]
        [TestCase(true, 6)]
        public void PlunderMasterHooktusk_DiscoverBuffsOtherPiratesUsingGoldenCardsPlayed(bool golden, int expectedBuff)
        {
            var service = CreateService();
            var source = CreateCatalogMinion(service, PlunderMasterHooktuskKey, "hooktusk", golden);
            var otherPirate = Minion("hooktusk-pirate", 2, 3, Tribe.Pirate);
            var nonPirate = Minion("hooktusk-beast", 4, 5, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(otherPirate);
            service.State.Player.Board.Add(nonPirate);
            for (var index = 0; index < 2; index += 1)
            {
                service.State.Player.Tavern.Hand.Add(Minion("hooktusk-golden-" + index, 1, 1, Tribe.Beast, golden: true));
                service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            }

            service.State.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = "season14-pirate-test",
                RewardTier = 1,
                RemainingPicks = 1,
                Options = new List<MinionInstance> { Minion("hooktusk-discover", 1, 1, Tribe.Mech) }
            });
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(otherPirate.BaseAttack + expectedBuff, otherPirate.Attack);
            Assert.AreEqual(otherPirate.BaseHealth + expectedBuff, otherPirate.MaxHealth);
            Assert.AreEqual(source.BaseAttack, source.Attack);
            Assert.AreEqual(source.BaseHealth, source.MaxHealth);
            Assert.AreEqual(nonPirate.BaseAttack, nonPirate.Attack);
            Assert.AreEqual(nonPirate.BaseHealth, nonPirate.MaxHealth);
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
            service.State.DelayedObjectStates.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
            return service;
        }

        private static void ActivateShipwreckPirate(MatchService service, MinionInstance source)
        {
            service.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = ShipwreckPirateActionId,
                SourceInstanceId = source.InstanceId
            }));
        }

        private static void RunOneAttackCombat(MatchService service, int seed)
        {
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = seed, SafetyLimit = 1 }));
        }

        private static void SpendGoldOnRefreshes(MatchService service, int count)
        {
            for (var index = 0; index < count; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
            }
        }

        private static MinionInstance PlayCatalogMinion(MatchService service, string researchKey, string suffix, bool golden)
        {
            var card = CreateCatalogMinion(service, researchKey, suffix, golden);
            service.State.Player.Tavern.Hand.Add(card);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            return service.State.Player.Board.Single(item => item.InstanceId == card.InstanceId);
        }

        private static MinionInstance CreateCatalogMinion(MatchService service, string researchKey, string suffix, bool golden)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == researchKey);
            return MinionFactory.Create(definition, BoardSide.Player, suffix, golden, PoolSource.Copy, 0);
        }

        private static MinionInstance Minion(
            string instanceId,
            int attack,
            int health,
            Tribe tribe,
            BoardSide owner = BoardSide.Player,
            Keyword keyword = Keyword.Trigger,
            Keyword secondKeyword = Keyword.Trigger,
            bool golden = false)
        {
            var keywords = new[] { keyword, secondKeyword }
                .Where(item => item != Keyword.Trigger)
                .ToList();
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
                Owner = owner,
                CanAttack = true,
                TavernTier = 1,
                Golden = golden,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords,
                OfficialKeywords = new List<Keyword>(keywords),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>(),
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
        }

        private static void AssertPreviewCarrier(
            MinionDefinition minion,
            int goldenAttack,
            int goldenHealth,
            string goldenText)
        {
            Assert.AreEqual("Implemented", minion.ImplementationStatus);
            Assert.IsFalse(minion.InPool);
            Assert.NotNull(minion.Golden);
            Assert.AreEqual(goldenAttack, minion.Golden.BaseAttack);
            Assert.AreEqual(goldenHealth, minion.Golden.BaseHealth);
            StringAssert.Contains(goldenText, minion.Golden.Text);
        }
    }
}
