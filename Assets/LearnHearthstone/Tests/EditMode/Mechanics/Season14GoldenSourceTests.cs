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
    public sealed class Season14GoldenSourceTests
    {
        private const string AureateLaureateCardId = "BG32_236";
        private const string AlwaysGoldenNoTripleRewardEffectId = "minion.always-golden-no-triple-reward";
        private const string TripleRewardGrantedCounter = "triple-reward-granted";

        [Test]
        public void Season14Catalog_AureateLaureateUsesCurrentTextKeywordsAndEffect()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            var definition = AureateDefinition(service);

            Assert.AreEqual(2, definition.BaseAttack);
            Assert.AreEqual(2, definition.BaseHealth);
            CollectionAssert.Contains(definition.Keywords, Keyword.DivineShield);
            CollectionAssert.DoesNotContain(definition.Keywords, Keyword.Battlecry);
            CollectionAssert.DoesNotContain(definition.Golden.Keywords, Keyword.Battlecry);
            StringAssert.Contains("始终为金色", definition.Text);
            StringAssert.Contains("三连奖励", definition.Text);
            CollectionAssert.Contains(definition.EffectIds, AlwaysGoldenNoTripleRewardEffectId);
        }

        [Test]
        public void Season14Factory_AureateLaureateIsGoldenAndRewardProcessedAtAcquisition()
        {
            var card = CreateAureate(CreateService(GameVersionIds.Season14Preview), "factory");

            Assert.IsTrue(card.Golden);
            Assert.AreEqual(2, card.Attack);
            Assert.AreEqual(2, card.MaxHealth);
            CollectionAssert.DoesNotContain(card.Keywords, Keyword.Battlecry);
            Assert.AreEqual(1, card.Counters[TripleRewardGrantedCounter]);
        }

        [Test]
        public void Season14AureateLaureate_PlayDoesNotGrantTripleReward()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            service.State.Player.Tavern.Hand.Add(CreateAureate(service, "play"));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsTrue(service.State.Player.Board.Single().Golden);
            Assert.IsFalse(HasTripleReward(service));
        }

        [Test]
        public void Season14AureateLaureate_ClonePreservesNoRewardMarker()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            var copy = CreateAureate(service, "original").Clone();
            copy.InstanceId = "player-aureate-copy";
            Assert.IsTrue(copy.Golden);
            Assert.AreEqual(1, copy.Counters[TripleRewardGrantedCounter]);
            service.State.Player.Tavern.Hand.Add(copy);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(1, service.State.Player.Board.Single().Counters[TripleRewardGrantedCounter]);
            Assert.IsFalse(HasTripleReward(service));
        }

        [Test]
        public void Season14AureateLaureate_SaveLoadPreservesNoRewardMarker()
        {
            var repository = new InMemoryTestScenarioRepository();
            var service = CreateService(GameVersionIds.Season14Preview, repository);
            service.State.Player.Tavern.Hand.Add(CreateAureate(service, "save"));

            service.Apply(new GameCommand(GameCommandType.SaveTestScenario, "p1d-aureate", new CombatTestOptions()));
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.LoadTestScenario, "p1d-aureate", new CombatTestOptions()));

            var restored = service.State.Player.Tavern.Hand.Single();
            Assert.IsTrue(restored.Golden);
            Assert.AreEqual(1, restored.Counters[TripleRewardGrantedCounter]);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.IsFalse(HasTripleReward(service));
        }

        [Test]
        public void Season14AureateLaureate_ScenarioImportCanUseEffectIdWhenCounterIsMissing()
        {
            var source = CreateService(GameVersionIds.Season14Preview);
            var importedCard = CreateAureate(source, "scenario-import");
            importedCard.Counters.Clear();
            source.State.Player.Tavern.Hand.Add(importedCard);
            var scenario = TestScenarioMapper.Capture(source.State, "p1d-aureate-import");
            var target = CreateService(GameVersionIds.Season14Preview);

            TestScenarioMapper.ApplyTo(target.State, scenario);

            CollectionAssert.Contains(target.State.Player.Tavern.Hand.Single().EffectIds, AlwaysGoldenNoTripleRewardEffectId);
            target.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.IsFalse(HasTripleReward(target));
        }

        [Test]
        public void Season14AureateLaureate_SellAndReacquireRestoresNoRewardSemantics()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            BuyOnlyShopCard(service, "first-buy");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.Apply(new GameCommand(GameCommandType.SellMinion, service.State.Player.Board.Single().InstanceId));

            BuyOnlyShopCard(service, "second-buy");
            var reacquired = service.State.Player.Tavern.Hand.Single();

            Assert.IsTrue(reacquired.Golden);
            Assert.AreEqual(1, reacquired.Counters[TripleRewardGrantedCounter]);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.IsFalse(HasTripleReward(service));
        }

        [Test]
        public void Season14AureateLaureate_FullHandBlocksPurchaseAtomically()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            FillHand(service, 10);
            service.State.Player.Tavern.Shop.Add(CreateAureate(service, "full-hand-shop", PoolSource.Pool, 1));
            var goldBefore = service.State.Player.Tavern.Gold;

            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(GameCommandType.BuyMinion, 0)));

            Assert.AreEqual(10, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(1, service.State.Player.Tavern.Shop.Count);
            Assert.AreEqual(goldBefore, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void Season14AureateLaureate_FullHandBlocksDiscoverAtomically()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            FillHand(service, 10);
            var option = CreateAureate(service, "full-hand-discover", PoolSource.Copy, 0);
            service.State.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = "p1d-aureate",
                RewardTier = 1,
                RemainingPicks = 1,
                Options = new List<MinionInstance> { option }
            });

            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0)));

            Assert.AreEqual(10, service.State.Player.Tavern.Hand.Count);
            Assert.NotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(1, service.State.Player.Tavern.Discover.Options.Single().Counters[TripleRewardGrantedCounter]);
        }

        [Test]
        public void LegacyAureateLaureate_RemainsOneOneBattlecrySelfGolden()
        {
            var service = CreateService(GameVersionIds.LegacyCompositeSandbox);
            var definition = AureateDefinition(service);
            var card = MinionFactory.Create(definition, BoardSide.Player, "legacy", false, PoolSource.Copy, 0);

            Assert.AreEqual(1, card.Attack);
            Assert.AreEqual(1, card.MaxHealth);
            Assert.IsFalse(card.Golden);
            CollectionAssert.Contains(card.Keywords, Keyword.Battlecry);
            service.State.Player.Tavern.Hand.Add(card);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsTrue(service.State.Player.Board.Single().Golden);
            Assert.AreEqual(2, service.State.Player.Board.Single().Attack);
            Assert.IsFalse(HasTripleReward(service));
        }

        [Test]
        public void LegalNormalTriple_StillGrantsTripleReward()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            var definition = service.Catalogs.Minions.All.First(item =>
                item.Golden != null &&
                !item.EffectIds.Contains(AlwaysGoldenNoTripleRewardEffectId));
            for (var index = 0; index < 3; index += 1)
            {
                service.State.Player.Tavern.Hand.Add(MinionFactory.Create(
                    definition,
                    BoardSide.Player,
                    "legal-triple-" + index,
                    false,
                    PoolSource.Copy,
                    0));
            }

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var goldenIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.Golden);
            Assert.GreaterOrEqual(goldenIndex, 0);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, goldenIndex));

            Assert.IsTrue(HasTripleReward(service));
        }

        [Test]
        public void ExplicitRewardEligibleGolden_StillGrantsTripleReward()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            var definition = service.Catalogs.Minions.All.First(item =>
                item.Golden != null &&
                !item.EffectIds.Contains(AlwaysGoldenNoTripleRewardEffectId));
            service.State.Player.Tavern.Hand.Add(MinionFactory.Create(
                definition,
                BoardSide.Player,
                "eligible-golden",
                true,
                PoolSource.Pool,
                1));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsTrue(HasTripleReward(service));
        }

        private static MatchService CreateService(string versionId, ITestScenarioRepository repository = null)
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(versionId, snapshot);
            var service = MatchService.CreateWithResolvedVersion(
                resolved,
                12345,
                repository ?? new InMemoryTestScenarioRepository(),
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
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;
            return service;
        }

        private static MinionDefinition AureateDefinition(MatchService service)
        {
            return service.Catalogs.Minions.GetByCardId(AureateLaureateCardId);
        }

        private static MinionInstance CreateAureate(
            MatchService service,
            string suffix,
            PoolSource source = PoolSource.Copy,
            int poolCopiesHeld = 0)
        {
            return MinionFactory.Create(AureateDefinition(service), BoardSide.Player, suffix, false, source, poolCopiesHeld);
        }

        private static void BuyOnlyShopCard(MatchService service, string suffix)
        {
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(CreateAureate(service, suffix, PoolSource.Pool, 1));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
        }

        private static void FillHand(MatchService service, int count)
        {
            for (var index = 0; index < count; index += 1)
            {
                service.State.Player.Tavern.Hand.Add(new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = "p1d-filler-" + index,
                    DefinitionId = "p1d-filler-" + index,
                    CardId = "P1D_FILLER_" + index,
                    Name = "P1-D Filler",
                    BaseAttack = 1,
                    BaseHealth = 1,
                    Attack = 1,
                    Health = 1,
                    MaxHealth = 1,
                    TavernTier = 1,
                    Owner = BoardSide.Player,
                    PoolSource = PoolSource.Copy,
                    PoolCopiesHeld = 0
                });
            }
        }

        private static bool HasTripleReward(MatchService service)
        {
            return service.State.Player.Tavern.Hand.Any(card => card.CardId == "TRIPLE_REWARD");
        }
    }
}
