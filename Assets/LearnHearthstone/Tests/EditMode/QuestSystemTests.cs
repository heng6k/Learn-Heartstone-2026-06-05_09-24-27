using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class QuestSystemTests
    {
        [Test]
        public void Catalog_LoadsImplementedQuestsAndRewardsWithImages()
        {
            var catalog = QuestCatalogLoader.LoadFromResources();

            Assert.AreEqual(6, catalog.Quests.Count);
            Assert.AreEqual(73, catalog.Rewards.Count);
            Assert.AreEqual(5, catalog.ImplementedQuests.Count);
            Assert.AreEqual(20, catalog.HiddenEffectRewards.Count);
            Assert.IsTrue(catalog.HiddenEffectRewards.All(reward => reward.OfferPoolStatus == QuestOfferPoolStatus.HiddenEffectOnly));
            Assert.IsTrue(catalog.OfferableRewards.All(reward => reward.OfferPoolStatus == QuestOfferPoolStatus.Offerable));
            Assert.IsFalse(catalog.OfferableRewards.Any(reward => HiddenRewardIds.Contains(reward.Id)));
            Assert.IsFalse(catalog.OfferableRewards.Any(reward => DebugOnlyRewardIds.Contains(reward.Id)));
            Assert.IsFalse(catalog.OfferableRewards.Any(reward => DisabledRewardIds.Contains(reward.Id)));
            Assert.IsTrue(catalog.Rewards.Where(reward => DisabledRewardIds.Contains(reward.Id)).All(reward => reward.OfferPoolStatus == QuestOfferPoolStatus.Disabled));
            Assert.IsTrue(catalog.Quests.All(quest => quest.ImplementationStatus == QuestImplementationStatus.Implemented));
            Assert.IsTrue(catalog.Rewards.All(reward => reward.ImplementationStatus == QuestImplementationStatus.Implemented));
            Assert.IsFalse(catalog.ImplementedQuests.Any(quest => quest.CardId == "BG27_Quest_800"));
            Assert.IsTrue(catalog.Quests.All(quest => Resources.Load<Texture2D>(quest.ImagePath) != null));
            Assert.IsTrue(catalog.Rewards.All(reward => Resources.Load<Texture2D>(reward.ImagePath) != null));
            Assert.IsTrue(catalog.Quests.All(quest => catalog.TryGetRewardById(quest.DefaultRewardId, out _)));

            var quest = catalog.GetQuestByCardId("BG27_Quest_800");
            Assert.AreEqual(QuestObjectiveKind.SellMinions, quest.Objective.Kind);
            Assert.AreEqual("CardImages/BG27_Quest_800", quest.ImagePath);
            Assert.IsNotNull(CardImageProvider.LoadSprite(quest.ImagePath, quest.CardId, CardKind.Quest));
        }

        [Test]
        public void DebugOfferQuests_ChoiceActivatesQuestAndRewardPair()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());

            service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));
            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Quest, request.Kind);
            Assert.AreEqual(3, request.Options.Count);
            Assert.IsFalse(request.Options.Any(option => option.SourceId == "BG27_Quest_800"));
            Assert.IsTrue(request.Options.All(option => !string.IsNullOrEmpty(option.RewardId)));
            Assert.IsTrue(request.Options.All(option => !string.IsNullOrEmpty(option.RewardImagePath)));
            Assert.IsTrue(request.Options.All(option => option.RequiredAmount > 0));
            Assert.IsTrue(request.Options.All(option => option.DifficultyTier >= 1 && option.DifficultyTier <= 4));
            Assert.IsFalse(request.Options.Any(option => HiddenRewardIds.Contains(option.RewardId)));

            var selected = request.Options[0];
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var quests = service.State.Player.Tavern.AdvancedMechanics.Quests;
            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            Assert.IsNotNull(quests.MainQuest);
            Assert.AreEqual(selected.SourceId, quests.MainQuest.QuestCardId);
            Assert.AreEqual(selected.RewardId, quests.MainQuest.RewardId);
            Assert.IsFalse(quests.MainQuest.Completed);
        }

        [Test]
        public void QuestDifficulty_UsesRewardPowerAndPatchwerkHighHealthOverride()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            QueueQuestChoice(service, "BG24_Quest_313", "BG24_Reward_306");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var active = service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest;
            Assert.AreEqual(4, active.BaseRequiredAmount);
            Assert.AreEqual(3, active.RequiredAmount);
            Assert.AreEqual(QuestRewardPowerLevel.Strong, active.RewardPowerLevel);
        }

        [Test]
        public void QuestModeOpening_OffersThreeQuestRewardPairs()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { AdvancedMechanicMode = AdvancedMechanicMode.Quests });

            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Quest, request.Kind);
            Assert.AreEqual("quest-mode-opening", request.Source);
            Assert.AreEqual("Main", request.Slot);
            Assert.AreEqual(3, request.Options.Count);
            Assert.IsTrue(request.Options.All(option => !string.IsNullOrEmpty(option.SourceId)));
            Assert.IsTrue(request.Options.All(option => !string.IsNullOrEmpty(option.ImagePath)));
            Assert.IsTrue(request.Options.All(option => !string.IsNullOrEmpty(option.RewardId)));
            Assert.IsTrue(request.Options.All(option => !string.IsNullOrEmpty(option.RewardImagePath)));
            Assert.IsFalse(request.Options.Any(option => HiddenRewardIds.Contains(option.RewardId)));
        }

        [Test]
        public void QuestModeOpening_DisabledQuestsDoesNotOfferChoice()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    AdvancedMechanicMode = AdvancedMechanicMode.Quests,
                    EnableQuests = false
                });

            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
        }

        [Test]
        public void QuestModeOpening_DisabledQuestRewardsDoesNotOfferChoice()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    AdvancedMechanicMode = AdvancedMechanicMode.Quests,
                    EnableQuestRewards = false
                });

            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
        }

        [Test]
        public void QuestModeOpening_UsesSelectedQuestAndRewardPools()
        {
            var catalog = QuestCatalogLoader.LoadFromResources();
            var quest = catalog.ImplementedQuests.First();
            var reward = catalog.OfferableRewards.First();
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    AdvancedMechanicMode = AdvancedMechanicMode.Quests,
                    EnableQuests = true,
                    EnableQuestRewards = true,
                    EnabledQuestCardIds = new List<string> { quest.CardId },
                    EnabledQuestRewardCardIds = new List<string> { reward.CardId }
                });

            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;

            Assert.IsNotNull(request);
            Assert.AreEqual(1, request.Options.Count);
            Assert.AreEqual(quest.CardId, request.Options[0].SourceId);
            Assert.AreEqual(reward.Id, request.Options[0].RewardId);
            CollectionAssert.AreEquivalent(new[] { quest.CardId }, service.State.EnabledQuestCardIds);
            CollectionAssert.AreEquivalent(new[] { reward.CardId }, service.State.EnabledQuestRewardCardIds);
        }

        [Test]
        public void QuestModeOpening_RequiresSelectedQuestAndRewardPoolsTogether()
        {
            var quest = QuestCatalogLoader.LoadFromResources().ImplementedQuests.First();
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    AdvancedMechanicMode = AdvancedMechanicMode.Quests,
                    EnableQuests = true,
                    EnableQuestRewards = true,
                    EnabledQuestCardIds = new List<string> { quest.CardId }
                });

            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            CollectionAssert.AreEquivalent(new[] { quest.CardId }, service.State.EnabledQuestCardIds);
            Assert.AreEqual(0, service.State.EnabledQuestRewardCardIds.Count);
        }

        [Test]
        public void FirstBatch_AnimaBribeSellingMinionBuffsTavernMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(TestMinion("shop-target"));
            var sold = TestMinion("sold");
            sold.Attack = 4;
            sold.MaxHealth = 6;
            sold.Health = 6;
            service.State.Player.Board.Add(sold);
            ActivateRewardDirectly(service, "BG24_Reward_305");

            service.Apply(new GameCommand(GameCommandType.SellMinion, sold.InstanceId));

            var target = service.State.Player.Tavern.Shop[0];
            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(7, target.MaxHealth);
        }

        [Test]
        public void FirstBatch_InvigoratingConchBuyingMinionBuffsFriendlyMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Shop.Clear();
            var boardTarget = TestMinion("friendly");
            service.State.Player.Board.Add(boardTarget);
            var bought = TestMinion("shop-buy");
            bought.Attack = 4;
            bought.MaxHealth = 6;
            bought.Health = 6;
            service.State.Player.Tavern.Shop.Add(bought);
            service.State.Player.Tavern.Gold = 10;
            ActivateRewardDirectly(service, "BG27_Reward_503");

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(5, boardTarget.Attack);
            Assert.AreEqual(7, boardTarget.MaxHealth);
        }

        [Test]
        public void FirstBatch_DoubleHeadedRewardCopiesOnlyFirstBoughtCardEachTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(TestMinion("first-buy"));
            service.State.Player.Tavern.Shop.Add(TestMinion("second-buy"));
            service.State.Player.Tavern.Gold = 10;
            ActivateRewardDirectly(service, "BG28_Reward_506");

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));

            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "first-buy"));
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardId == "second-buy"));
        }

        [Test]
        public void FirstBatch_StashOfTheScribeAddsThreeTavernSpellsAtTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            ActivateRewardDirectly(service, "BG28_Reward_515");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void FirstBatch_BeyondTheMirageDiscountsTavernSpellPurchases()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(TestSpell("spell-zero", 1));
            service.State.Player.Tavern.Gold = 0;
            ActivateRewardDirectly(service, "BG28_Reward_500");

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(0, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void FirstBatch_BloodsoakedTomeSetsTavernMinionCostToTwo()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(TestMinion("two-gold-minion"));
            service.State.Player.Tavern.Gold = 2;
            ActivateRewardDirectly(service, "BG27_Reward_811");

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(0, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void FirstBatch_SplittingScrollCopiesExpensiveBoughtTavernSpell()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(TestSpell("spell-three", 3));
            service.State.Player.Tavern.Gold = 3;
            ActivateRewardDirectly(service, "BG28_Reward_502");

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "spell-three"));
        }

        [Test]
        public void FirstBatch_GoldenForgeMakesHighestTierShopMinionGoldenAtTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Shop.Clear();
            var low = TestMinion("low-tier", 2);
            var high = TestMinion("high-tier", 5);
            service.State.Player.Tavern.Shop.Add(low);
            service.State.Player.Tavern.Shop.Add(high);
            TavernShopSlots.SetAllFrozen(service.State.Player.Tavern, true);
            ActivateRewardDirectly(service, "BG33_Reward_013");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsFalse(low.Golden);
            Assert.IsTrue(high.Golden);
            Assert.AreEqual(2, high.Attack);
            Assert.AreEqual(2, high.MaxHealth);
        }

        [Test]
        public void SecondBatch_SnickerSnacksTriggersTwoFriendlyBattlecriesAtTurnEnd()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestBattlecryMinion("shell-one", "BG23_002"));
            service.State.Player.Board.Add(TestBattlecryMinion("shell-two", "BG23_002"));
            ActivateRewardDirectly(service, "BG24_Reward_107");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void HiddenReward_ExquisiteConchOnlyRepeatsFirstBattlecryEachTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            QueueQuestChoice(service, "BG24_Quest_112", "BG24_Reward_123");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            CompleteQuestByRefreshing(service);
            Assert.AreEqual("BG24_Reward_123", service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest.RewardId);
            Assert.IsTrue(service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest.RewardActive);
            Assert.IsFalse(service.State.Player.Tavern.AdvancedMechanics.Quests.RewardCounters.ContainsKey("BG24_Reward_123:usedRound"));

            service.State.Player.Tavern.Hand.Add(TestBattlecryMinion("shell-conch-first", "BG23_002"));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));

            service.State.Player.Tavern.Hand.Add(TestBattlecryMinion("shell-conch-second", "BG23_002"));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.AreEqual(4, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.State.Player.Tavern.Hand.Add(TestBattlecryMinion("shell-conch-next-turn", "BG23_002"));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.AreEqual(7, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void DebugReward_GilneanWarHornAddsOneBattlecryRepeat()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            ActivateRewardDirectly(service, "BG27_Reward_802");

            service.State.Player.Tavern.Hand.Add(TestBattlecryMinion("shell-war-horn", "BG23_002"));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void SecondBatch_TealTigerSapphireUsesRefreshCountWithoutOverStackingFrozenMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Shop.Clear();
            var frozen = TestMinion("teal-frozen");
            service.State.Player.Tavern.Shop.Add(frozen);
            service.State.Player.Tavern.Shop.Add(TestMinion("teal-replaced"));
            TavernShopSlots.Ensure(service.State.Player.Tavern);
            TavernShopSlots.SetSlotFrozen(service.State.Player.Tavern, 0, true);
            service.State.Player.Tavern.Gold = 20;
            ActivateRewardDirectly(service, "BG24_Reward_308");

            service.Apply(new GameCommand(GameCommandType.RerollShop));
            service.State.Player.Tavern.Gold = 20;
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.AreEqual(3, frozen.Attack);
            Assert.AreEqual(3, frozen.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(1, frozen.Attack);
            Assert.AreEqual(1, frozen.MaxHealth);
        }

        [Test]
        public void SecondBatch_DevilsInTheDetailsConsumesShopMinionsForBothEdges()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Shop.Clear();
            var left = TestMinion("devils-left");
            var right = TestMinion("devils-right");
            var foodOne = TestMinion("devils-food-one");
            foodOne.Attack = 4;
            foodOne.MaxHealth = 6;
            foodOne.Health = 6;
            var foodTwo = TestMinion("devils-food-two");
            foodTwo.Attack = 2;
            foodTwo.MaxHealth = 3;
            foodTwo.Health = 3;
            service.State.Player.Board.Add(left);
            service.State.Player.Board.Add(right);
            service.State.Player.Tavern.Shop.Add(foodOne);
            service.State.Player.Tavern.Shop.Add(foodTwo);
            ActivateRewardDirectly(service, "BG24_Reward_309");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.Greater(left.Attack, 1);
            Assert.Greater(left.MaxHealth, 1);
            Assert.Greater(right.Attack, 1);
            Assert.Greater(right.MaxHealth, 1);
        }

        [Test]
        public void SecondBatch_GiftOfTheGoldenKoboldMakesShopMinionGoldenAfterFiveRefreshes()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 100;
            service.State.Player.Tavern.MaxGold = 100;
            ActivateRewardDirectly(service, "BG28_Reward_508");

            for (var refresh = 0; refresh < 5; refresh += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
                service.State.Player.Tavern.Gold = 100;
            }

            Assert.IsTrue(service.State.Player.Tavern.Shop.Any(card => card != null && card.CardKind == CardKind.Minion && card.Golden));
        }

        [Test]
        public void SecondBatch_VictimsSpecterCopiesLastDeadFriendlyMinionAfterCombat()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestMinion("victim-first"));
            service.State.Player.Board.Add(TestMinion("victim-second"));
            var opponent = TestMinion("victim-opponent");
            opponent.Owner = BoardSide.Opponent;
            opponent.Attack = 10;
            opponent.MaxHealth = 10;
            opponent.Health = 10;
            service.State.Opponent.Board.Add(opponent);
            ActivateRewardDirectly(service, "BG24_Reward_138");

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 222, SafetyLimit = 5 }));

            var copy = service.State.Player.Tavern.Hand.Single(card => card.CardId == "victim-second");
            Assert.AreEqual(PoolSource.Copy, copy.PoolSource);
            Assert.AreEqual(PoolSource.Copy, copy.OriginPoolSource);
            Assert.AreEqual(0, copy.PoolCopiesHeld);
            Assert.IsFalse(copy.CanReturnToPoolAfterAttach);
        }

        [Test]
        public void ThirdBatch_StolenGoldMakesOnlyEdgeCombatCopiesGolden()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var left = TestMinion("stolen-left");
            var middle = TestMinion("stolen-middle");
            var right = TestMinion("stolen-right");
            service.State.Player.Board.Add(left);
            service.State.Player.Board.Add(middle);
            service.State.Player.Board.Add(right);
            service.State.Opponent.Board.Add(TestMinion("stolen-opponent"));
            ActivateRewardDirectly(service, "BG24_Reward_109");

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 123, SafetyLimit = 1 }));

            var snapshot = service.State.LastReplay.InitialSnapshot.Player.Minions;
            Assert.IsTrue(snapshot.First(minion => minion.InstanceId == "stolen-left").Golden);
            Assert.IsFalse(snapshot.First(minion => minion.InstanceId == "stolen-middle").Golden);
            Assert.IsTrue(snapshot.First(minion => minion.InstanceId == "stolen-right").Golden);
            Assert.IsFalse(left.Golden);
            Assert.IsFalse(right.Golden);
        }

        [Test]
        public void ThirdBatch_EvilTwinSummonsHighestHealthCombatCopy()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var low = TestMinion("evil-low");
            var high = TestMinion("evil-high");
            high.Health = 6;
            high.MaxHealth = 6;
            service.State.Player.Board.Add(low);
            service.State.Player.Board.Add(high);
            service.State.Opponent.Board.Add(TestMinion("evil-opponent"));
            ActivateRewardDirectly(service, "BG24_Reward_111");

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 123, SafetyLimit = 1 }));

            var snapshot = service.State.LastReplay.InitialSnapshot.Player.Minions;
            Assert.AreEqual(3, snapshot.Count);
            Assert.AreEqual(2, snapshot.Count(minion => minion.CardId == "evil-high"));
        }

        [Test]
        public void ThirdBatch_RitualDaggerBuffsOriginalDeathrattleMinionAfterCombatDeath()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var ritualTarget = TestMinion("ritual-target");
            ritualTarget.Keywords.Add(Keyword.Deathrattle);
            service.State.Player.Board.Add(ritualTarget);
            var opponent = TestMinion("ritual-opponent");
            opponent.Attack = 10;
            opponent.Health = 10;
            opponent.MaxHealth = 10;
            opponent.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponent);
            ActivateRewardDirectly(service, "BG24_Reward_113");

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 123, SafetyLimit = 1 }));

            Assert.AreEqual(6, ritualTarget.Attack);
            Assert.AreEqual(6, ritualTarget.MaxHealth);
        }

        [Test]
        public void ThirdBatch_CycleOfEnergyAvengeAddsRandomTavernSpell()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestMinion("cycle-one"));
            service.State.Player.Board.Add(TestMinion("cycle-two"));
            service.State.Player.Board.Add(TestMinion("cycle-three"));
            var opponent = TestMinion("cycle-opponent");
            opponent.Attack = 10;
            opponent.Health = 10;
            opponent.MaxHealth = 10;
            opponent.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponent);
            ActivateRewardDirectly(service, "BG28_Reward_504");

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 123, SafetyLimit = 10 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void ThirdBatch_StableAmalgamationAvengeSummonsFiftyFiftyAmalgam()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            for (var index = 0; index < 7; index += 1)
            {
                service.State.Player.Board.Add(TestMinion("stable-" + index));
            }

            var opponent = TestMinion("stable-opponent");
            opponent.Attack = 10;
            opponent.Health = 10;
            opponent.MaxHealth = 10;
            opponent.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponent);
            ActivateRewardDirectly(service, "BG28_Reward_518");

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 123, SafetyLimit = 20 }));

            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Any(minion => minion.Name == "Stable Amalgam" && minion.Attack >= 50 && minion.MaxHealth >= 50));
        }

        [Test]
        public void ThirdBatch_TurbulentTombsAddsOneExtraDeathrattleTrigger()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var manasaber = TestMinion("turbulent-manasaber");
            manasaber.CardId = "BG26_800";
            manasaber.DefinitionId = "BG26_800";
            manasaber.Keywords.Add(Keyword.Deathrattle);
            service.State.Player.Board.Add(manasaber);
            var opponent = TestMinion("turbulent-opponent");
            opponent.Attack = 10;
            opponent.Health = 10;
            opponent.MaxHealth = 10;
            opponent.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponent);
            ActivateRewardDirectly(service, "BG27_Reward_803");

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 123, SafetyLimit = 1 }));

            Assert.AreEqual(4, service.State.LastResult.FinalPlayerBoard.Count(minion => minion.Name == "Cubling"));
        }

        [Test]
        public void DebugReward_RallyingCryAddsOneExtraRallyTrigger()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var supporter = TestMinion("rally-supporter");
            supporter.CardId = "BG33_241";
            supporter.DefinitionId = "BG33_241";
            supporter.Attack = 1;
            supporter.Health = 5;
            supporter.MaxHealth = 5;
            supporter.Keywords.Add(Keyword.Rally);
            var right = TestMinion("rally-right");
            right.Attack = 2;
            right.Health = 2;
            right.MaxHealth = 2;
            service.State.Player.Board.Add(supporter);
            service.State.Player.Board.Add(right);
            var opponent = TestMinion("rally-opponent");
            opponent.Attack = 0;
            opponent.Health = 10;
            opponent.MaxHealth = 10;
            opponent.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponent);
            ActivateRewardDirectly(service, "BG33_Reward_021");

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 123, SafetyLimit = 1 }));

            var buffed = service.State.LastResult.FinalPlayerBoard.First(minion => minion.InstanceId == "rally-right");
            Assert.AreEqual(6, buffed.Attack);
            Assert.AreEqual(6, buffed.MaxHealth);
        }

        [Test]
        public void DebugReward_GhastlyMaskRepeatsEndOfTurnEffects()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            var vendor = TestMinion("ghastly-vendor", 6);
            vendor.CardKind = CardKind.HeroBuddy;
            vendor.CardId = "TB_BaconShop_HERO_16_Buddy";
            vendor.DefinitionId = "TB_BaconShop_HERO_16_Buddy";
            vendor.Attack = 8;
            vendor.Health = 9;
            vendor.MaxHealth = 9;
            var target = TestMinion("ghastly-target", 3);
            target.Attack = 2;
            target.Health = 3;
            target.MaxHealth = 3;
            service.State.Player.Board.Add(vendor);
            service.State.Player.Board.Add(target);
            ActivateRewardDirectly(service, "BG24_Reward_130");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(18, target.Attack);
            Assert.AreEqual(21, target.MaxHealth);
        }

        [Test]
        public void FourthBatch_SecretSinstoneCopiesDiscoveredCard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            ActivateRewardDirectly(service, "BG24_Reward_129");
            var discovered = TestMinion("sinstone-card");
            service.State.Player.Tavern.Discover = new DiscoverState
            {
                Source = "test-discover",
                Options = new List<MinionInstance> { discovered }
            };

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            var copies = service.State.Player.Tavern.Hand.Where(card => card.CardId == "sinstone-card").ToList();
            Assert.AreEqual(2, copies.Count);
            Assert.AreNotEqual(copies[0].InstanceId, copies[1].InstanceId);
        }

        [Test]
        public void FourthBatch_DoppelgangersLocketDiscoversLastOpponentWarbandCopy()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestMinion("locket-player"));
            var target = TestMinion("locket-target");
            target.Owner = BoardSide.Opponent;
            target.Attack = 5;
            target.Health = 7;
            target.MaxHealth = 7;
            target.Enchantments.Add(new Enchantment { Id = "test-buff", SourceId = "test-buff", AttackBonus = 4, HealthBonus = 6 });
            var golden = TestMinion("locket-golden");
            golden.Owner = BoardSide.Opponent;
            golden.Golden = true;
            service.State.Opponent.Board.Add(target);
            service.State.Opponent.Board.Add(golden);
            ActivateRewardDirectly(service, "BG27_Reward_806");

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 123, SafetyLimit = 1 }));

            var discover = service.State.Player.Tavern.Discover;
            Assert.IsNotNull(discover);
            Assert.IsTrue(discover.Options.All(option => !option.Golden));
            var optionIndex = discover.Options.FindIndex(option => option.CardId == "locket-target");
            Assert.GreaterOrEqual(optionIndex, 0);
            var option = discover.Options[optionIndex];
            Assert.IsTrue(option.Enchantments.Any(enchantment => enchantment.SourceId == "test-buff"));
            Assert.AreEqual(PoolSource.Copy, option.PoolSource);
            Assert.AreEqual(PoolSource.Copy, option.OriginPoolSource);
            Assert.AreEqual(0, option.PoolCopiesHeld);
            Assert.IsFalse(option.CanReturnToPoolAfterAttach);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, optionIndex));

            var copy = service.State.Player.Tavern.Hand.Single(card => card.CardId == "locket-target");
            Assert.AreEqual(PoolSource.Copy, copy.PoolSource);
            Assert.AreEqual(PoolSource.Copy, copy.OriginPoolSource);
            Assert.AreEqual(0, copy.PoolCopiesHeld);
            Assert.IsFalse(copy.CanReturnToPoolAfterAttach);
        }

        [Test]
        public void FourthBatch_PartnerInCrimeAddsGoldenBuddy()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { SelectedHeroCardId = "BG24_HERO_100" });
            service.State.Player.Tavern.Hand.Clear();
            QueueQuestChoice(service, "BG24_Quest_314", "BG24_Reward_310");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            CompleteQuestByAddingCards(service);

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card =>
                card.CardKind == CardKind.HeroBuddy &&
                card.CardId == "BG24_HERO_100_Buddy" &&
                card.Golden));
        }

        [Test]
        public void FourthBatch_OpenAuditionsStartsBuddyDiscoverAtTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Discover = null;
            ActivateRewardDirectly(service, "BG28_Reward_513");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var discover = service.State.Player.Tavern.Discover;
            Assert.IsNotNull(discover);
            Assert.AreEqual(3, discover.Options.Count);
            Assert.IsTrue(discover.Options.All(option => option.CardKind == CardKind.HeroBuddy));
        }

        [Test]
        public void FourthBatch_QuaintBoutiqueOffersLesserTrinketNextTurnWithGold()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            QueueQuestChoice(service, "BG24_Quest_314", "BG33_Reward_014");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            CompleteQuestByAddingCards(service);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual("Lesser", request.Slot);
            Assert.AreEqual("quest:BG33_Reward_014", request.Source);
            Assert.AreEqual(service.State.Player.Tavern.MaxGold + 4, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void FourthBatch_JumboWarehouseOffersGreaterTrinketNextTurnWithGold()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            QueueQuestChoice(service, "BG24_Quest_314", "BG33_Reward_015");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            CompleteQuestByAddingCards(service);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual("Greater", request.Slot);
            Assert.AreEqual("quest:BG33_Reward_015", request.Source);
            Assert.AreEqual(service.State.Player.Tavern.MaxGold + 4, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void FourthBatch_CosmicRewardStoresSecondHeroPowerWithoutReplacingCurrent()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var originalHeroPower = service.State.Player.HeroPowerCardId;
            QueueQuestChoice(service, "BG24_Quest_314", "BG33_Reward_017");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            CompleteQuestByAddingCards(service);
            var discover = service.State.Player.Tavern.Discover;
            Assert.IsNotNull(discover);
            Assert.IsTrue(discover.Options.All(option => option.CardKind == CardKind.HeroPower));
            var picked = discover.Options[0].CardId;

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(originalHeroPower, service.State.Player.HeroPowerCardId);
            Assert.IsTrue(service.State.Player.ExtraHeroPowerCardIds.Contains(picked));
        }

        [Test]
        public void RemainingBatch_PilferedLampsMakesTwoCopiesGolden()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            var first = TestMinion("lamp-first");
            var second = TestMinion("lamp-second");
            first.DefinitionId = "lamp-copy";
            first.CardId = "lamp-copy";
            second.DefinitionId = "lamp-copy";
            second.CardId = "lamp-copy";
            service.State.Player.Tavern.Shop.Add(first);
            service.State.Player.Tavern.Shop.Add(second);
            service.State.Player.Tavern.Gold = 10;
            ActivateRewardDirectly(service, "BG24_Reward_350");

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.DefinitionId == "lamp-copy" && card.Golden));
        }

        [Test]
        public void RemainingBatch_TemporalTamperingCastsTavernSpellTwice()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var target = TestMinion("temporal-target");
            service.State.Player.Board.Add(target);
            var spell = TestSpell("temporal-selfish-bounty", 2);
            spell.CardId = "122184";
            spell.DefinitionId = "122184";
            service.State.Player.Tavern.Hand.Add(spell);
            ActivateRewardDirectly(service, "BG28_Reward_501");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(13, target.Attack);
            Assert.AreEqual(13, target.MaxHealth);
        }

        [Test]
        public void RemainingBatch_GoldenHammerSpellcraftRevertsNextTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var target = TestMinion("hammer-target");
            service.State.Player.Board.Add(target);
            ActivateRewardDirectly(service, "BG24_Reward_719");

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.IsTrue(target.Golden);
            Assert.AreEqual(2, target.Attack);
            Assert.AreEqual(2, target.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsFalse(target.Golden);
            Assert.AreEqual(1, target.Attack);
            Assert.AreEqual(1, target.MaxHealth);
        }

        [Test]
        public void RemainingBatch_KidnapSackPreservesMovedTavernCardPoolSource()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            ActivateRewardDirectly(service, "BG24_Reward_718");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var definition = MinionCatalogLoader.LoadFromResources().All.First(card => card.InPool && card.TavernTier == 1);
            var tavern = service.State.Player.Tavern;
            tavern.Shop.Clear();
            tavern.Shop.Add(MinionFactory.Create(definition, BoardSide.Player, "kidnap-pool", false, PoolSource.Pool, 1));
            tavern.Pool[definition.Id] = 0;

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            var stolen = tavern.Hand.Single(card => card.DefinitionId == definition.Id);
            Assert.AreEqual(PoolSource.Pool, stolen.PoolSource);
            Assert.AreEqual(PoolSource.Pool, stolen.OriginPoolSource);
            Assert.AreEqual(1, stolen.PoolCopiesHeld);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.IndexOf(stolen), 0));
            var boardCopy = service.State.Player.Board.Single(card => card.DefinitionId == definition.Id);
            service.Apply(new GameCommand(GameCommandType.SellMinion, boardCopy.InstanceId));

            Assert.AreEqual(1, service.State.Player.Tavern.Pool[definition.Id]);
        }

        [Test]
        public void RemainingBatch_TimelineAccelerationTransformsMinionUpOneTier()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestMinion("timeline-target", 1));
            ActivateRewardDirectly(service, "BG27_Reward_504");

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(2, service.State.Player.Board[0].TavernTier);
        }

        [Test]
        public void RemainingBatch_EtherealEvidenceOffersImmediateBonusRewardChoice()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            ActivateRewardDirectly(service, "BG24_Reward_363");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Quest, request.Kind);
            Assert.AreEqual("quest-ethereal-evidence", request.Source);
            Assert.AreEqual("Bonus", request.Slot);
            Assert.AreEqual(2, request.Options.Count);

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var bonus = service.State.Player.Tavern.AdvancedMechanics.Quests.BonusQuest;
            Assert.IsNotNull(bonus);
            Assert.IsTrue(bonus.Completed);
            Assert.IsTrue(bonus.RewardActive);
        }

        [Test]
        public void RemainingBatch_NorgannonAutoUpgradesOnceNextTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            QueueQuestChoice(service, "BG24_Quest_314", "BG33_Reward_010");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var startingTier = service.State.Player.Tavern.Tier;

            CompleteQuestByAddingCards(service);
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(startingTier + 1, service.State.Player.Tavern.Tier);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(startingTier + 1, service.State.Player.Tavern.Tier);
        }

        [Test]
        public void RemainingBatch_PerpetualIncantationAddsScalingTavernSpellBonus()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var target = TestMinion("incantation-target");
            service.State.Player.Board.Add(target);
            var spell = TestSpell("incantation-selfish-bounty", 2);
            spell.CardId = "122184";
            spell.DefinitionId = "122184";
            service.State.Player.Tavern.Hand.Add(spell);
            ActivateRewardDirectly(service, "BG33_Reward_020");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(9, target.Attack);
            Assert.AreEqual(8, target.MaxHealth);
        }

        [Test]
        public void NormalPool_TinyHenchmenBuffsThreeTierThreeOrLowerMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            var tierOne = TestMinion("tiny-tier-one", 1);
            var tierTwo = TestMinion("tiny-tier-two", 2);
            var tierThree = TestMinion("tiny-tier-three", 3);
            var tierFour = TestMinion("tiny-tier-four", 4);
            service.State.Player.Board.Add(tierOne);
            service.State.Player.Board.Add(tierTwo);
            service.State.Player.Board.Add(tierThree);
            service.State.Player.Board.Add(tierFour);
            ActivateRewardDirectly(service, "BG24_Reward_136");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(4, tierOne.Attack);
            Assert.AreEqual(4, tierOne.MaxHealth);
            Assert.AreEqual(4, tierTwo.Attack);
            Assert.AreEqual(4, tierTwo.MaxHealth);
            Assert.AreEqual(4, tierThree.Attack);
            Assert.AreEqual(4, tierThree.MaxHealth);
            Assert.AreEqual(1, tierFour.Attack);
            Assert.AreEqual(1, tierFour.MaxHealth);
        }

        [Test]
        public void NormalPool_UntoldRichesGrantsGoldAndRaisesMaximumGold()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            QueueQuestChoice(service, "BG24_Quest_313", "BG33_Reward_012");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Gold = 3;
            service.State.Player.Tavern.MaxGold = 10;

            service.Apply(new GameCommand(GameCommandType.DebugCompleteQuest));

            Assert.AreEqual(8, service.State.Player.Tavern.Gold);
            Assert.AreEqual(15, service.State.Player.Tavern.MaxGold);
        }

        [Test]
        public void NormalPool_SixteenGoldCoinPouchGrantsSixteenGoldOnCompletion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            QueueQuestChoice(service, "BG24_Quest_313", "LH_Reward_CoinPouch16");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Gold = 2;

            service.Apply(new GameCommand(GameCommandType.DebugCompleteQuest));

            Assert.AreEqual(18, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void NormalPool_AnotherHiddenBodyDiscoversCurrentTavernTierMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 4;
            QueueQuestChoice(service, "BG24_Quest_313", "BG24_Reward_311");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.Apply(new GameCommand(GameCommandType.DebugCompleteQuest));

            var discover = service.State.Player.Tavern.Discover;
            Assert.IsNotNull(discover);
            Assert.AreEqual("quest:BG24_Reward_311", discover.Source);
            Assert.AreEqual(4, discover.RewardTier);
            Assert.IsTrue(discover.Options.All(option => option.CardKind == CardKind.Minion));
            Assert.IsTrue(discover.Options.All(option => option.TavernTier == 4));
        }

        [Test]
        public void NormalPool_SmeltingChamberMakesTrackedTierFriendlyMinionGoldenAndImproves()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            var tierOne = TestMinion("smelting-tier-one", 1);
            var tierTwo = TestMinion("smelting-tier-two", 2);
            service.State.Player.Board.Add(tierOne);
            service.State.Player.Board.Add(tierTwo);
            ActivateRewardDirectly(service, "BG28_Reward_509");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(tierOne.Golden);
            Assert.IsFalse(tierTwo.Golden);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(tierTwo.Golden);
        }

        [Test]
        public void SireDenathrius_StartsWithQuestChoice()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { SelectedHeroCardId = "BG24_HERO_100" });

            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Quest, request.Kind);
            Assert.AreEqual("sire-denathrius", request.Source);
            Assert.AreEqual("Main", request.Slot);
            Assert.AreEqual(2, request.Options.Count);
        }

        [Test]
        public void SireDenathrius_QuestModeKeepsHeroOpeningQuestChoice()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    SelectedHeroCardId = "BG24_HERO_100",
                    AdvancedMechanicMode = AdvancedMechanicMode.Quests
                });

            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Quest, request.Kind);
            Assert.AreEqual("sire-denathrius", request.Source);
            Assert.AreEqual("Main", request.Slot);
            Assert.AreEqual(2, request.Options.Count);
        }

        [Test]
        public void ShadyAristocrat_SellOffersBonusQuestWithCoinPouchReward()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            var shady = service.HeroCatalog.GetBuddyByCardId("BG24_HERO_100_Buddy");
            var instance = MinionFactory.Create(shady, BoardSide.Player, "shady-test");
            service.State.Player.Board.Add(instance);

            service.Apply(new GameCommand(GameCommandType.SellMinion, instance.InstanceId));

            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Quest, request.Kind);
            Assert.AreEqual("shady-aristocrat", request.Source);
            Assert.AreEqual("Bonus", request.Slot);
            Assert.AreEqual(1, request.Options.Count);
            Assert.AreEqual("LH_Reward_CoinPouch8", request.Options[0].RewardId);
        }

        [Test]
        public void DustForPrints_TracksCardsAddedToHandFromAllSources()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var minion = MinionCatalogLoader.LoadFromResources().All.First(card => card.InPool && card.TavernTier == 1);
            service.State.Player.Tavern.Hand.Clear();
            QueueQuestChoice(service, "BG24_Quest_314", "BG24_Reward_361");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            for (var index = 0; index < 5; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, minion.CardId, CardKind.Minion));
            }

            var active = service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest;
            Assert.IsTrue(active.Completed);
            Assert.AreEqual(active.RequiredAmount, active.Progress);
            Assert.AreEqual("BG24_Quest_314", active.QuestCardId);
        }

        [Test]
        public void MirrorShield_RefreshRewardAddsStatsAndDivineShield()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 20;
            QueueQuestChoice(service, "BG24_Quest_112", "BG24_Reward_128");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            for (var refresh = 0; refresh < 4; refresh += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
            }

            var buffed = service.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            Assert.IsTrue(service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest.Completed);
            Assert.IsTrue(buffed.Any(card => card.Keywords.Contains(Keyword.DivineShield)));
            Assert.IsTrue(buffed.Any(card => card.Attack >= card.BaseAttack + 6 && card.MaxHealth >= card.BaseHealth + 6));
        }

        [Test]
        public void HiddenReward_RedHandBuffsHandMinionAtTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Hand.Add(TestMinion("hand-target"));
            QueueQuestChoice(service, "BG24_Quest_112", "BG24_Reward_131");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            CompleteQuestByRefreshing(service);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var handTarget = service.State.Player.Tavern.Hand.First(card => card.InstanceId == "hand-target");
            Assert.IsTrue(service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest.Completed);
            Assert.AreEqual(13, handTarget.Attack);
            Assert.AreEqual(13, handTarget.MaxHealth);
        }

        [Test]
        public void HiddenReward_AlterEgoAlternatesFrozenShopParity()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(TestMinion("even-shop", 2));
            service.State.Player.Tavern.Shop.Add(TestMinion("odd-shop", 3));
            QueueQuestChoice(service, "BG24_Quest_314", "BG24_Reward_321");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            CompleteQuestByAddingCards(service);

            var even = service.State.Player.Tavern.Shop.First(card => card.InstanceId == "even-shop");
            var odd = service.State.Player.Tavern.Shop.First(card => card.InstanceId == "odd-shop");
            Assert.AreEqual(8, even.Attack);
            Assert.AreEqual(8, even.MaxHealth);
            Assert.AreEqual(1, odd.Attack);
            Assert.AreEqual(1, odd.MaxHealth);

            service.State.Player.Tavern.Frozen = true;
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            even = service.State.Player.Tavern.Shop.First(card => card.InstanceId == "even-shop");
            odd = service.State.Player.Tavern.Shop.First(card => card.InstanceId == "odd-shop");
            Assert.AreEqual(1, even.Attack);
            Assert.AreEqual(1, even.MaxHealth);
            Assert.AreEqual(8, odd.Attack);
            Assert.AreEqual(8, odd.MaxHealth);
        }

        [Test]
        public void DebugCompleteQuest_CompletesActiveQuestThroughRewardFlow()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            QueueQuestChoice(service, "BG24_Quest_313", "BG24_Reward_306");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var active = service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest;
            Assert.IsFalse(active.Completed);

            service.Apply(new GameCommand(GameCommandType.DebugCompleteQuest));

            Assert.IsTrue(active.Completed);
            Assert.IsTrue(active.RewardActive);
            Assert.AreEqual(active.RequiredAmount, active.Progress);
            CollectionAssert.Contains(service.State.Player.Tavern.AdvancedMechanics.Quests.Completed, active);
        }

        [Test]
        public void DebugReplaceQuestReward_UpdatesUncompletedQuestBinding()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            QueueQuestChoice(service, "BG24_Quest_313", "BG24_Reward_306");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.Apply(new GameCommand(GameCommandType.DebugReplaceQuestReward, "BG24_Reward_305", CardKind.QuestReward));

            var active = service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest;
            Assert.IsFalse(active.Completed);
            Assert.AreEqual("BG24_Reward_305", active.RewardId);
            Assert.AreEqual("Anima Bribe", active.RewardName);
            Assert.AreEqual(QuestRewardPowerLevel.Medium, active.RewardPowerLevel);
            Assert.Greater(active.RequiredAmount, 0);
        }

        [Test]
        public void UnityTrainer_ShowsMechanicChoiceModalAndQuestTracker()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));
            var root = new GameObject("QuestUiTestRoot", typeof(RectTransform));
            try
            {
                var controller = root.AddComponent<UnityTavernTrainerController>();
                controller.Initialize(service, null, null, null);
                Assert.IsNotNull(root.transform.Find("UnityAdvancedMechanicChoiceOverlay"));

                service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
                controller.Rebuild();
                Assert.IsNotNull(root.transform.Find("UnityMechanicStatusStrip/UnityQuestTrackerPanel"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void QueueQuestChoice(MatchService service, string questCardId, string rewardId)
        {
            var quest = service.QuestCatalog.GetQuestByCardId(questCardId);
            var reward = service.QuestCatalog.GetRewardById(rewardId);
            service.State.Player.Tavern.AdvancedMechanics.PendingChoice = new MechanicChoiceRequest
            {
                RequestId = "test-quest-" + questCardId,
                Kind = AdvancedMechanicKind.Quest,
                Source = "test",
                Slot = "Main",
                Round = service.State.Round,
                RemainingPicks = 1,
                Options = new List<MechanicChoiceOption>
                {
                    new MechanicChoiceOption
                    {
                        OptionId = quest.CardId + ":" + reward.Id,
                        Kind = AdvancedMechanicKind.Quest,
                        SourceId = quest.CardId,
                        DisplayName = quest.Name,
                        Text = quest.Text,
                        ImagePath = quest.ImagePath,
                        RewardId = reward.Id,
                        RewardName = reward.Name,
                        RewardText = reward.Text,
                        RewardImagePath = reward.ImagePath,
                        Slot = "Main",
                        ImplementationStatus = quest.ImplementationStatus.ToString(),
                        Tags = new List<string>(quest.Tags)
                    }
                }
            };
        }

        private static ActiveQuestState ActivateRewardDirectly(MatchService service, string rewardId)
        {
            QueueQuestChoice(service, "BG24_Quest_112", rewardId);
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var active = service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest;
            active.Completed = true;
            active.RewardActive = true;
            return active;
        }

        private static void CompleteQuestByRefreshing(MatchService service)
        {
            var active = service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest;
            service.State.Player.Tavern.Gold = 100;
            service.State.Player.Tavern.MaxGold = 100;
            for (var index = 0; index < active.RequiredAmount; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
                service.State.Player.Tavern.Gold = 100;
            }

            Assert.IsTrue(active.Completed);
        }

        private static void CompleteQuestByAddingCards(MatchService service)
        {
            var active = service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest;
            var minion = MinionCatalogLoader.LoadFromResources().All.First(card => card.InPool && card.TavernTier == 1);
            for (var index = 0; index < active.RequiredAmount; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, minion.CardId, CardKind.Minion));
                if (!active.Completed && service.State.Player.Tavern.Hand.Count > 0)
                {
                    service.State.Player.Tavern.Hand.RemoveAt(0);
                }
            }

            Assert.IsTrue(active.Completed);
        }

        private static readonly HashSet<string> HiddenRewardIds = new HashSet<string>
        {
            "BG24_Reward_115",
            "BG24_Reward_123",
            "BG24_Reward_125",
            "BG24_Reward_128",
            "BG24_Reward_131",
            "BG24_Reward_312",
            "BG24_Reward_321",
            "BG24_Reward_331",
            "BG24_Reward_364",
            "BG24_Reward_708",
            "BG24_Reward_712",
            "BG24_Reward_715",
            "BG27_Reward_502",
            "BG27_Reward_804",
            "BG27_Reward_810",
            "BG27_Reward_815",
            "BG28_Reward_505",
            "BG33_Reward_003",
            "BG33_Reward_004",
            "BG33_Reward_006"
        };

        private static readonly HashSet<string> DebugOnlyRewardIds = new HashSet<string>
        {
            "BG27_Reward_803",
            "BG24_Reward_310",
            "BG28_Reward_513",
            "BG33_Reward_014",
            "BG33_Reward_015",
            "BG33_Reward_017",
            "BG24_Reward_130",
            "BG24_Reward_135",
            "BG24_Reward_313",
            "BG24_Reward_362",
            "BG24_Reward_363",
            "BG24_Reward_718",
            "BG24_Reward_719",
            "BG27_Reward_504",
            "BG27_Reward_802",
            "BG28_Reward_510",
            "BG28_Reward_514",
            "BG33_Reward_010",
            "BG33_Reward_011",
            "BG33_Reward_021"
        };

        private static readonly HashSet<string> DisabledRewardIds = new HashSet<string>
        {
            "BG24_Reward_134",
            "BG27_Reward_812"
        };

        private static MinionInstance TestMinion(string instanceId, int tavernTier = 1)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = "Quest Test Minion",
                Cost = 3,
                BaseAttack = 1,
                BaseHealth = 1,
                Attack = 1,
                Health = 1,
                MaxHealth = 1,
                TavernTier = tavernTier,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Debug,
                PoolCopiesHeld = 0
            };
        }

        private static MinionInstance TestBattlecryMinion(string instanceId, string cardId, int tavernTier = 1)
        {
            var minion = TestMinion(instanceId, tavernTier);
            minion.DefinitionId = cardId;
            minion.CardId = cardId;
            minion.Keywords.Add(Keyword.Battlecry);
            minion.OfficialKeywords.Add(Keyword.Battlecry);
            return minion;
        }

        private static MinionInstance TestSpell(string instanceId, int cost)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = "Quest Test Tavern Spell",
                Cost = cost,
                BaseAttack = 0,
                BaseHealth = 0,
                Attack = 0,
                Health = 0,
                MaxHealth = 0,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Debug,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_tavern_spell" }
            };
        }
    }
}
