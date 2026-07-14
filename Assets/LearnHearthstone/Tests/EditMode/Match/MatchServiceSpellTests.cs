using System;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MatchServiceSpellTests
    {
        [Test]
        public void Apply_PlayPointyArrowBuffsFirstAvailableMinionAttack()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            var beforeAttack = target.Attack;
            AddSpellToHand(service, "100596");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(beforeAttack + 4, service.State.Player.Board[0].Attack);
            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Last().Message.Contains("尖利箭矢"));
        }

        [Test]
        public void Apply_PlayTavernCoinGainsGoldWithoutAddingBoardCard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 0;
            service.State.Player.Tavern.MaxGold = 3;
            AddSpellToHand(service, "104436");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Gold);
            Assert.AreEqual(0, service.State.Player.Board.Count);
            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
        }

        [Test]
        public void Apply_PlayPerfectImageSetsTargetStatsToTwentyTwenty()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            AddBoardTarget(service);
            AddSpellToHand(service, "104601");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(20, service.State.Player.Board[0].Attack);
            Assert.AreEqual(20, service.State.Player.Board[0].Health);
            Assert.AreEqual(20, service.State.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Apply_PlayUnexpectedFruitBuffsShopMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var shopTarget = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            var beforeAttack = shopTarget.Attack;
            var beforeHealth = shopTarget.Health;
            AddSpellToHand(service, "105903");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(beforeAttack + 1, shopTarget.Attack);
            Assert.AreEqual(beforeHealth + 2, shopTarget.Health);
        }

        [Test]
        public void Apply_PlayNewSaplingStartsTierOneDiscover()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            AddSpellToHand(service, "122864");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(1, service.State.Player.Tavern.Discover.RewardTier);
            Assert.AreEqual(3, service.State.Player.Tavern.Discover.Options.Count);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(option => option.TavernTier == 1));
        }

        [Test]
        public void Apply_PlayMischievousPackageAddsThreeSpellCards()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 4;
            service.State.Player.Tavern.Hand.Clear();
            AddSpellToHand(service, "110407");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void Apply_GeneratedSpellsAreNormalSpellsAndIgnoreTavernSpellPower()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            service.State.Player.Tavern.TavernSpellBonusAttack = 3;
            AddGeneratedSpellToHand(service, "BLOOD_GEM");

            Assert.AreEqual(CardKind.Spell, service.State.Player.Tavern.Hand[0].CardKind);
            Assert.IsFalse(service.State.Player.Tavern.Hand[0].Keywords.Contains(Keyword.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(target.BaseAttack + 1, service.State.Player.Board[0].Attack);
            Assert.AreEqual(target.BaseHealth + 1, service.State.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Apply_TavernSpellsUseTavernSpellPower()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            service.State.Player.Tavern.TavernSpellBonusAttack = 3;
            AddSpellToHand(service, "100596");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(target.BaseAttack + 7, service.State.Player.Board[0].Attack);
        }

        [Test]
        public void Apply_TierTwoTavernSpellsResolveRefreshHealthCostAndLockedDiscover()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 2;
            service.State.Player.Tavern.Gold = 0;
            AddSpellToHand(service, "104446");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));
            Assert.AreEqual(2, service.State.Player.Tavern.FreeRefreshes);

            service.Apply(new GameCommand(GameCommandType.RerollShop));
            Assert.AreEqual(0, service.State.Player.Tavern.Gold);
            Assert.AreEqual(1, service.State.Player.Tavern.FreeRefreshes);

            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 0;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "104559", CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.AreEqual(1, service.State.Player.Tavern.Gold);

            service.State.Player.Tavern.Hand.Clear();
            AddSpellToHand(service, "127288");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.AreEqual(2, service.State.Player.Tavern.Discover.RewardTier);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(option => option.TavernTier == 2));
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1)));
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.DoesNotThrow(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1)));
        }

        [Test]
        public void Apply_BuyHastyExcavationCostsHealthInsteadOfGold()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "104559", CardKind.TavernSpell));
            var hasty = service.State.Player.Tavern.Hand[0];
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop[0] = hasty;
            service.State.Player.Tavern.Gold = 0;
            service.State.Player.Health = 30;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(0, service.State.Player.Tavern.Gold);
            Assert.AreEqual(27, service.State.Player.Health);
            Assert.AreEqual("104559", service.State.Player.Tavern.Hand.Last().CardId);
        }

        [Test]
        public void Apply_ChefsChoiceGetsAnotherMinionOfTargetsType()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG26_800", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Tavern.Hand.Clear();
            AddSpellToHand(service, "105664");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Beast)));
        }

        [Test]
        public void Apply_PointyArrowAndSlimyShieldCanTargetTavernMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var target = service.State.Player.Tavern.Shop[shopIndex];
            var beforeAttack = target.Attack;
            AddSpellToHand(service, "100596");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, shopIndex, TargetZone.TavernShop, -1, TargetZone.Unspecified));

            Assert.AreEqual(beforeAttack + 4, target.Attack);

            AddGeneratedSpellToHand(service, "SLIMY_SHIELD");
            var beforeHealth = target.MaxHealth;
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, shopIndex, TargetZone.TavernShop, -1, TargetZone.Unspecified));
            Assert.AreEqual(beforeAttack + 5, target.Attack);
            Assert.AreEqual(beforeHealth + 1, target.MaxHealth);
            Assert.IsTrue(target.Keywords.Contains(Keyword.Taunt));
        }

        [Test]
        public void Apply_GoldenArrowTargetsTavernOnly()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            AddBoardTarget(service);
            AddSpellToHand(service, "100596");
            var arrow = service.State.Player.Tavern.Hand[0];
            arrow.Golden = true;
            arrow.Tags.Add("anomaly_golden_arrow");

            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified)));

            var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var beforeAttack = service.State.Player.Tavern.Shop[shopIndex].Attack;
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, shopIndex, TargetZone.TavernShop, -1, TargetZone.Unspecified));
            Assert.AreEqual(beforeAttack + 8, service.State.Player.Tavern.Shop[shopIndex].Attack);
        }

        [Test]
        public void Apply_KidnapSackMovesSelectedBoardOrTavernMinionToHand()
        {
            var boardService = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var boardTarget = AddBoardTarget(boardService);
            AddGeneratedSpellToHand(boardService, "BG24_Reward_718t");
            boardService.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));
            Assert.IsFalse(boardService.State.Player.Board.Contains(boardTarget));
            Assert.IsTrue(boardService.State.Player.Tavern.Hand.Contains(boardTarget));

            var shopService = MatchService.CreateWithDefaultCatalog(54321, new InMemoryTestScenarioRepository());
            var shopIndex = shopService.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var shopTarget = shopService.State.Player.Tavern.Shop[shopIndex];
            AddGeneratedSpellToHand(shopService, "BG24_Reward_718t");
            shopService.Apply(new GameCommand(GameCommandType.PlayMinion, 0, shopIndex, TargetZone.TavernShop, -1, TargetZone.Unspecified));
            Assert.IsTrue(shopService.State.Player.Tavern.Hand.Contains(shopTarget));
        }

        [Test]
        public void Apply_TimewarpedSummonerUsesAllTargetTribesForTavernPool()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            target.Tribes = new System.Collections.Generic.List<Tribe> { Tribe.Dragon, Tribe.Murloc };
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Hand.Add(new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "timewarped-summoner-test",
                DefinitionId = "TIMEWARPED_SUMMONER_SPELL",
                CardId = "TIMEWARPED_SUMMONER_SPELL",
                Name = "Timewarped Summoner",
                Owner = BoardSide.Player,
                Tags = new System.Collections.Generic.List<string> { "generated_spell", "spellcraft", "targeted_spell" }
            });

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.IsTrue(service.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .All(card => card.Tribes.Contains(Tribe.All) || card.Tribes.Contains(Tribe.Dragon) || card.Tribes.Contains(Tribe.Murloc)));
        }

        [Test]
        public void Apply_NaturalBlessingUsesAllTargetTribesWithoutDoubleBuffing()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            var target = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
            target.InstanceId = "natural-target";
            target.Tribes = new System.Collections.Generic.List<Tribe> { Tribe.Dragon, Tribe.Murloc };
            var dualMatch = target.Clone();
            dualMatch.InstanceId = "natural-dual-match";
            var beast = target.Clone();
            beast.InstanceId = "natural-beast";
            beast.Tribes = new System.Collections.Generic.List<Tribe> { Tribe.Beast };
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(dualMatch);
            service.State.Player.Board.Add(beast);
            var beforeAttack = dualMatch.Attack;
            AddSpellToHand(service, "104472");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(beforeAttack + 3, dualMatch.Attack);
            Assert.AreEqual(beast.BaseAttack, beast.Attack);
        }

        [Test]
        public void Apply_SelfishBountyIsNonTargetedAndBuffsLeftmost()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var left = AddBoardTarget(service);
            var right = left.Clone();
            right.InstanceId = "selfish-right";
            service.State.Player.Board.Add(right);
            var leftAttack = left.Attack;
            var rightAttack = right.Attack;
            AddSpellToHand(service, "122184");

            Assert.IsFalse(service.State.Player.Tavern.Hand[0].Tags.Contains("targeted_spell"));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(leftAttack + 6, left.Attack);
            Assert.AreEqual(rightAttack, right.Attack);
        }

        [Test]
        public void Apply_DeepBlueAndBackToBackUseIndependentGlobalCounters()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            AddBoardTarget(service);
            AddGeneratedSpellToHand(service, "DEEP_BLUE_SPELL");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.Greater(service.State.Player.Tavern.DeepBlueBonusAttack, 0);
            Assert.AreEqual(0, service.State.Player.Tavern.BackToBackBonus);

            AddSpellToHand(service, "131153");
            var deepBlueBonus = service.State.Player.Tavern.DeepBlueBonusAttack;
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(deepBlueBonus, service.State.Player.Tavern.DeepBlueBonusAttack);
            Assert.Greater(service.State.Player.Tavern.BackToBackBonus, 0);
        }

        [Test]
        public void Apply_BelindaRepeatsFriendlyTargetedSpellsButNotTavernTargets()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var boardTarget = AddBoardTarget(service);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG35_883", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            var boardAttack = boardTarget.Attack;
            AddSpellToHand(service, "100596");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));
            Assert.AreEqual(boardAttack + 8, boardTarget.Attack);

            AddSpellToHand(service, "100596");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));
            Assert.AreEqual(boardAttack + 16, boardTarget.Attack);

            var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var shopTarget = service.State.Player.Tavern.Shop[shopIndex];
            var shopAttack = shopTarget.Attack;
            AddSpellToHand(service, "100596");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, shopIndex, TargetZone.TavernShop, -1, TargetZone.Unspecified));
            Assert.AreEqual(shopAttack + 4, shopTarget.Attack);
        }

        [Test]
        public void Apply_GoldenBelindaMakesFriendlyTargetedSpellCastThreeTimes()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var boardTarget = AddBoardTarget(service);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG35_883", CardKind.Minion));
            service.State.Player.Tavern.Hand[0].Golden = true;
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var beforeAttack = boardTarget.Attack;
            AddSpellToHand(service, "100596");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(beforeAttack + 12, boardTarget.Attack);
        }

        private static MinionInstance AddBoardTarget(MatchService service)
        {
            service.State.Player.Board.Clear();
            var target = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            target.InstanceId = "spell-target";
            target.Owner = BoardSide.Player;
            service.State.Player.Board.Add(target);
            return target;
        }

        private static void AddSpellToHand(MatchService service, string cardNumber)
        {
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardNumber, CardKind.TavernSpell));
        }

        private static void AddGeneratedSpellToHand(MatchService service, string cardNumber)
        {
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardNumber, CardKind.Spell));
        }
    }
}
