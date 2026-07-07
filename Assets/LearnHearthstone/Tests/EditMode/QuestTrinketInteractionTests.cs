using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class QuestTrinketInteractionTests
    {
        [Test]
        public void StartOfCombatQuestAndTrinketBoardBuffsStackOnCombatSnapshot()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            var minion = TestMinion("stacked-start-buff", 2, 3);
            service.State.Player.Board.Add(minion);

            ActivateRewardDirectly(service, "BG24_Reward_312");
            EquipTrinket(service, "BG30_MagicItem_970t");
            RunStartOfCombat(service);

            var combatMinion = FinalPlayerMinion(service, minion.InstanceId);
            Assert.AreEqual(20, combatMinion.Attack);
            Assert.AreEqual(21, combatMinion.MaxHealth);
            Assert.AreEqual(2, minion.Attack);
            Assert.AreEqual(3, minion.MaxHealth);
        }

        [Test]
        public void QuestAndTrinketDeathrattleRepeatsStackWithoutReusingFirstOnlySource()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            ActivateRewardDirectly(service, "BG27_Reward_803");
            EquipTrinket(service, "BG30_MagicItem_700");

            var first = TestMinion("quest-trinket-coldlight-one", 1, 1, Tribe.Murloc, Keyword.Deathrattle);
            first.CardId = "BG33_894";
            first.DefinitionId = "BG33_894";
            var second = TestMinion("quest-trinket-coldlight-two", 1, 1, Tribe.Murloc, Keyword.Deathrattle);
            second.CardId = "BG33_894";
            second.DefinitionId = "BG33_894";
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);

            RunAvengeCombat(service, 5, 100, 2);

            var deathrattleAmounts = service.State.LastResult.PlayerRewards
                .Where(reward => reward.Type == CombatRewardType.FriendlyDeathrattleTriggered && reward.SourceCardId == "BG33_894")
                .ToDictionary(reward => reward.SourceInstanceId, reward => reward.Amount);
            Assert.AreEqual(3, deathrattleAmounts[first.InstanceId]);
            Assert.AreEqual(2, deathrattleAmounts[second.InstanceId]);
            Assert.AreEqual(
                5,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.AddTavernSpellToHand && reward.SourceCardId == "BG33_894")
                    .Sum(reward => reward.Amount));
        }

        [Test]
        public void QuestAndTrinketAvengeRewardsConsumeSameDeathsIndependently()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            ActivateRewardDirectly(service, "BG33_Reward_004");
            EquipTrinket(service, "BG32_MagicItem_270");

            for (var index = 0; index < 3; index += 1)
            {
                service.State.Player.Board.Add(TestMinion("avenge-victim-" + index, 1, 1));
            }

            RunAvengeCombat(service, 1, 100, 8);

            Assert.AreEqual(
                1,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.GainFreeRefresh && reward.SourceCardId == "BG33_Reward_004")
                    .Sum(reward => reward.Amount));
            Assert.AreEqual(1, service.State.Player.Tavern.FreeRefreshes);
            Assert.AreEqual(1, service.State.Player.Tavern.TavernSpellBonusAttack);
        }

        [Test]
        public void QuestSummonedMinionsReceiveTrinketAndQuestSummonModifiers()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            ActivateRewardDirectly(service, "BG28_Reward_505");
            var handMurloc = TestMinion("quest-trinket-hand-murloc", 3, 9, Tribe.Murloc);
            service.State.Player.Tavern.Hand.Add(handMurloc);
            EquipTrinket(service, "BG32_MagicItem_301");

            var bassgill = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG26_350");
            service.State.Player.Tavern.Hand.Remove(bassgill);
            service.State.Player.Board.Add(bassgill);

            RunAvengeCombat(service, 10, 100, 1);

            var summoned = service.State.LastResult.FinalPlayerBoard
                .FirstOrDefault(card => card.CardId == handMurloc.CardId);
            Assert.IsNotNull(summoned);
            Assert.AreEqual(7, summoned.Attack);
            Assert.AreEqual(13, summoned.MaxHealth);
            Assert.IsTrue(summoned.Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void OpponentCombatRewardsDoNotApplyToPlayerRecruitState()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestMinion("player-clean-attacker", 20, 20));
            service.State.Opponent.Board.Clear();
            var opponentColdlight = TestMinion("opponent-coldlight", 1, 1, Tribe.Murloc, Keyword.Taunt, Keyword.Deathrattle);
            opponentColdlight.CardId = "BG33_894";
            opponentColdlight.DefinitionId = "BG33_894";
            opponentColdlight.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponentColdlight);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 99, SafetyLimit = 5 }));

            Assert.IsTrue(service.State.LastResult.OpponentRewards.Any(reward =>
                reward.Type == CombatRewardType.AddTavernSpellToHand &&
                reward.SourceCardId == "BG33_894"));
            Assert.IsEmpty(service.State.Player.Tavern.Hand);
        }

        [Test]
        public void CombatRewardsConvergeWithoutLeakingOpponentRewards()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Board.Clear();
            ActivateRewardDirectly(service, "BG27_Reward_803");
            ActivateRewardDirectly(service, "BG33_Reward_004", bonusQuest: true);
            EquipTrinket(service, "BG30_MagicItem_931");
            service.State.Player.Tavern.AdvancedMechanics.Trinkets.LuckyTabbyDeaths = 6;

            var kilrek = TestMinion("quest-trinket-kilrek", 1, 1, Tribe.Demon, Keyword.Taunt, Keyword.Deathrattle);
            kilrek.CardId = "BG34_Giant_584";
            kilrek.DefinitionId = "BG34_Giant_584";
            service.State.Player.Board.Add(kilrek);
            service.State.Player.Board.Add(TestMinion("quest-trinket-second-death", 1, 1));

            var opponentColdlight = TestMinion("opponent-coldlight-convergence", 1, 1, Tribe.Murloc, Keyword.Taunt, Keyword.Deathrattle);
            opponentColdlight.CardId = "BG33_894";
            opponentColdlight.DefinitionId = "BG33_894";
            opponentColdlight.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponentColdlight);
            var opponentKiller = TestMinion("opponent-convergence-killer", 5, 30);
            opponentKiller.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponentKiller);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 9009, SafetyLimit = 5 }));

            Assert.AreEqual(
                2,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.FriendlyMinionDied)
                    .Sum(reward => reward.Amount));
            Assert.AreEqual(
                2,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.AddRandomDemonToHand && reward.SourceCardId == "BG34_Giant_584")
                    .Sum(reward => reward.Amount));
            Assert.AreEqual(
                1,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.GainFreeRefresh && reward.SourceCardId == "BG33_Reward_004")
                    .Sum(reward => reward.Amount));
            Assert.AreEqual(1, service.State.Player.Tavern.FreeRefreshes);
            Assert.AreEqual(1, service.State.Player.Tavern.AdvancedMechanics.Trinkets.LuckyTabbyDeaths);
            Assert.GreaterOrEqual(service.State.Player.Tavern.Hand.Count, 2);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => BoardTribeAnalyzer.GetCountedTribes(card).Contains(Tribe.Demon)));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => BoardTribeAnalyzer.GetCountedTribes(card).Contains(Tribe.Beast)));
            Assert.IsTrue(service.State.LastResult.OpponentRewards.Any(reward =>
                reward.Type == CombatRewardType.AddTavernSpellToHand &&
                reward.SourceCardId == "BG33_894"));
            Assert.IsFalse(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.AddTavernSpellToHand &&
                reward.SourceCardId == "BG33_894"));
            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.CardId == "104436"));
        }

        [Test]
        public void CombatRewardsApplyBeforeNextRecruitRefreshAndStaySideIsolated()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            tavern.Shop.Clear();
            service.State.Opponent.Board.Clear();
            ActivateRewardDirectly(service, "BG33_Reward_004", bonusQuest: true);

            var frozenBeast = TestMinion("next-turn-frozen-beast", 2, 3, Tribe.Beast);
            tavern.Shop.Add(frozenBeast);
            TavernShopSlots.Ensure(tavern);
            TavernShopSlots.SetSlotFrozen(tavern, 0, true);

            var goldrinn = TestMinion("next-turn-goldrinn", 1, 1, Tribe.Beast, Keyword.Taunt, Keyword.Deathrattle);
            goldrinn.CardId = "BG34_Giant_362";
            goldrinn.DefinitionId = "BG34_Giant_362";
            service.State.Player.Board.Add(goldrinn);
            var kilrek = TestMinion("next-turn-kilrek", 1, 1, Tribe.Demon, Keyword.Deathrattle);
            kilrek.CardId = "BG34_Giant_584";
            kilrek.DefinitionId = "BG34_Giant_584";
            service.State.Player.Board.Add(kilrek);

            var opponentColdlight = TestMinion("next-turn-opponent-coldlight", 1, 1, Tribe.Murloc, Keyword.Taunt, Keyword.Deathrattle);
            opponentColdlight.CardId = "BG33_894";
            opponentColdlight.DefinitionId = "BG33_894";
            opponentColdlight.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponentColdlight);
            var opponentKiller = TestMinion("next-turn-opponent-killer", 5, 30);
            opponentKiller.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponentKiller);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 9010, SafetyLimit = 5 }));

            Assert.IsTrue(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.AddRandomDemonToHand &&
                reward.SourceCardId == "BG34_Giant_584"));
            Assert.IsTrue(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.GainFreeRefresh &&
                reward.SourceCardId == "BG33_Reward_004"));
            Assert.IsTrue(service.State.LastResult.OpponentRewards.Any(reward =>
                reward.Type == CombatRewardType.AddTavernSpellToHand &&
                reward.SourceCardId == "BG33_894"));
            Assert.IsTrue(tavern.Hand.Any(card => BoardTribeAnalyzer.GetCountedTribes(card).Contains(Tribe.Demon)));
            Assert.AreEqual(1, tavern.FreeRefreshes);
            var goldrinnGrowth = tavern.Growth.ShopModifiers
                .Where(modifier => modifier.SourceId == "timewarped-goldrinn" && modifier.Tribe == Tribe.Beast)
                .ToList();
            Assert.IsTrue(goldrinnGrowth.Count > 0);
            var growthAttack = goldrinnGrowth.Sum(modifier => modifier.Attack);
            var growthHealth = goldrinnGrowth.Sum(modifier => modifier.Health);
            Assert.AreEqual(frozenBeast.BaseAttack + growthAttack, frozenBeast.Attack);
            Assert.AreEqual(frozenBeast.BaseHealth + growthHealth, frozenBeast.MaxHealth);
            var nextTurnAttack = frozenBeast.Attack + growthAttack;
            var nextTurnHealth = frozenBeast.MaxHealth + growthHealth;
            Assert.IsFalse(tavern.Hand.Any(card => card.CardId == "104436"));

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(2, service.State.Round);
            Assert.AreEqual(MatchPhase.Tavern, service.State.Phase);
            Assert.IsNull(service.State.LastResult);
            Assert.IsTrue(tavern.Hand.Any(card => BoardTribeAnalyzer.GetCountedTribes(card).Contains(Tribe.Demon)));
            Assert.AreEqual(1, tavern.FreeRefreshes);
            Assert.AreEqual(nextTurnAttack, frozenBeast.Attack);
            Assert.AreEqual(nextTurnHealth, frozenBeast.MaxHealth);
            Assert.IsFalse(tavern.Hand.Any(card => card.CardId == "104436"));
        }

        private static void RunStartOfCombat(MatchService service, int seed = 77)
        {
            service.State.Opponent.Board.Clear();
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = seed, SafetyLimit = 1 }));
        }

        private static void RunAvengeCombat(MatchService service, int opponentAttack, int opponentHealth, int safetyLimit)
        {
            service.State.Opponent.Board.Clear();
            var opponent = TestMinion("quest-trinket-opponent", opponentAttack, opponentHealth);
            opponent.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponent);
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 123, SafetyLimit = safetyLimit }));
        }

        private static MinionInstance FinalPlayerMinion(MatchService service, string instanceId)
        {
            return service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == instanceId);
        }

        private static void EquipTrinket(MatchService service, string cardId)
        {
            QueueTrinketChoice(service, cardId);
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
        }

        private static ActiveQuestState ActivateRewardDirectly(MatchService service, string rewardId, bool bonusQuest = false)
        {
            QueueQuestChoice(service, "BG24_Quest_112", rewardId, bonusQuest ? "Bonus" : "Main");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var quests = service.State.Player.Tavern.AdvancedMechanics.Quests;
            var active = bonusQuest ? quests.BonusQuest : quests.MainQuest;
            active.Completed = true;
            active.RewardActive = true;
            return active;
        }

        private static void QueueQuestChoice(MatchService service, string questCardId, string rewardId, string slot)
        {
            var quest = service.QuestCatalog.GetQuestByCardId(questCardId);
            var reward = service.QuestCatalog.GetRewardById(rewardId);
            service.State.Player.Tavern.AdvancedMechanics.PendingChoice = new MechanicChoiceRequest
            {
                RequestId = "test-quest-" + questCardId + "-" + slot,
                Kind = AdvancedMechanicKind.Quest,
                Source = "test",
                Slot = slot,
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
                        Slot = slot,
                        ImplementationStatus = quest.ImplementationStatus.ToString(),
                        Tags = new List<string>(quest.Tags)
                    }
                }
            };
        }

        private static void QueueTrinketChoice(MatchService service, string cardId)
        {
            var definition = service.TrinketCatalog.GetByCardId(cardId);
            service.State.Player.Tavern.AdvancedMechanics.PendingChoice = new MechanicChoiceRequest
            {
                RequestId = "test-trinket-" + cardId,
                Kind = AdvancedMechanicKind.Trinket,
                Source = "test",
                Slot = definition.SlotKind.ToString(),
                Round = service.State.Round,
                RemainingPicks = 1,
                Options = new List<MechanicChoiceOption>
                {
                    new MechanicChoiceOption
                    {
                        OptionId = definition.CardId,
                        Kind = AdvancedMechanicKind.Trinket,
                        SourceId = definition.CardId,
                        DisplayName = definition.Name,
                        Text = definition.Text,
                        ImagePath = definition.ImagePath,
                        Cost = definition.Cost,
                        Slot = definition.SlotKind.ToString(),
                        ImplementationStatus = definition.ImplementationStatus.ToString(),
                        Tags = new List<string>(definition.Tags)
                    }
                }
            };
        }

        private static MinionInstance TestMinion(string instanceId, int attack, int health, Tribe tribe = Tribe.None, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                Cost = 3,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                OfficialKeywords = keywords.ToList(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Debug,
                PoolCopiesHeld = 0,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>()
            };
        }
    }
}
