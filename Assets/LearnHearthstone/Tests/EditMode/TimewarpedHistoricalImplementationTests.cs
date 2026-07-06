using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TimewarpedHistoricalImplementationTests
    {
        [Test]
        public void BuyTavernSpells_TimewarpedSeerDiscountsFirstTwoEachTurn()
        {
            var service = CreateService();
            service.State.Player.Board.Add(TestMinion("seer", "BG34_Giant_008", 2, 3));
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 3;
            service.State.Player.Tavern.MaxGold = 10;

            BuyShopTavernSpell(service, 0, "100596");
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            BuyShopTavernSpell(service, 0, "100596");
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            BuyShopTavernSpell(service, 0, "100596");

            Assert.AreEqual(2, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void CastTavernSpells_TimewarpedElectronMagnetizesSatelliteEverySecondSpell()
        {
            var service = CreateService();
            var target = TestMinion("mech-target", "MECH_TARGET", 2, 2, Tribe.Mech);
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(TestMinion("electron", "BG34_Giant_610", 3, 4));
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Hand.Clear();

            AddCardToHand(service, "100596", CardKind.TavernSpell);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));
            AddCardToHand(service, "100596", CardKind.TavernSpell);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));

            Assert.AreEqual(12, target.Attack);
            Assert.AreEqual(5, target.MaxHealth);
            Assert.IsTrue(target.Tags.Contains("timewarped_electron_satellite"));
        }

        [Test]
        public void TavernSpellStats_TimewarpedExpeditionerBuffsLeftMostHandMinions()
        {
            var service = CreateService();
            var expeditioner = TestMinion("expeditioner", "BG34_Giant_317", 2, 4);
            var firstHand = TestMinion("hand-1", "HAND_1", 1, 1);
            var secondHand = TestMinion("hand-2", "HAND_2", 2, 2);
            var thirdHand = TestMinion("hand-3", "HAND_3", 3, 3);
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(expeditioner);
            service.State.Player.Tavern.Hand.Clear();
            AddCardToHand(service, "100596", CardKind.TavernSpell);
            service.State.Player.Tavern.Hand.Add(firstHand);
            service.State.Player.Tavern.Hand.Add(secondHand);
            service.State.Player.Tavern.Hand.Add(thirdHand);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(6, expeditioner.Attack);
            Assert.AreEqual(5, firstHand.Attack);
            Assert.AreEqual(6, secondHand.Attack);
            Assert.AreEqual(3, thirdHand.Attack);
        }

        [Test]
        public void BloodGemOnTimewarpedTwirlerCastsBloodGemBarrage()
        {
            var service = CreateService();
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(TestMinion("twirler", "BG34_Giant_105", 3, 3, Tribe.Quilboar));
            service.State.Player.Tavern.Hand.Clear();
            AddCardToHand(service, "BLOOD_GEM", CardKind.Spell);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.IsTrue(service.State.Player.Tavern.Growth.ShopModifiers.Any(modifier =>
                modifier.SourceId == "Blood Gem Barrage" &&
                modifier.Attack == 1 &&
                modifier.Health == 1));
        }

        [Test]
        public void SellQuilboar_TimewarpedRelaxerPlaysFourBloodGems()
        {
            var service = CreateService();
            var relaxer = TestMinion("relaxer", "BG34_Giant_002", 1, 5, Tribe.Quilboar);
            var sold = TestMinion("sold-quilboar", "SOLD_QUILBOAR", 2, 2, Tribe.Quilboar);
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(relaxer);
            service.State.Player.Board.Add(sold);

            service.Apply(new GameCommand(GameCommandType.SellMinion, sold.InstanceId));

            Assert.AreEqual(5, relaxer.Attack);
            Assert.AreEqual(9, relaxer.MaxHealth);
            Assert.IsFalse(service.State.Player.Board.Any(minion => minion.InstanceId == sold.InstanceId));
        }

        [Test]
        public void EndTurn_TimewarpedLowFlierBuffsLowerAttackAndHealthMinions()
        {
            var service = CreateService();
            var lowFlier = TestMinion("low-flier", "BG34_Giant_065", 5, 5, Tribe.Beast);
            var target = TestMinion("small", "SMALL", 3, 4, Tribe.Beast);
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(lowFlier);
            service.State.Player.Board.Add(target);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(6, target.MaxHealth);
        }

        [Test]
        public void Combat_TimewarpedArmQueuesPermanentAttackRewardWhenFriendlyMinionIsAttacked()
        {
            var arm = TestMinion("arm", "BG34_Giant_027", 1, 6);
            var defender = TestMinion("defender", "DEFENDER", 1, 20);
            defender.Keywords.Add(Keyword.Taunt);
            var opponents = new[]
            {
                TestMinion("opponent-1", "OPPONENT_1", 1, 10),
                TestMinion("opponent-2", "OPPONENT_2", 1, 10),
                TestMinion("opponent-3", "OPPONENT_3", 1, 10)
            };

            var result = CombatEngine.SimulateBasicCombat(new[] { arm, defender }, opponents, 9001, 1);

            Assert.IsTrue(result.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.BuffOriginalFriendlyMinion &&
                reward.TargetInstanceId == defender.InstanceId &&
                reward.Attack == 8));
        }

        [Test]
        public void Combat_TimewarpedGuardRallyQueuesPermanentDivineShieldReward()
        {
            var guard = TestMinion("guard", "BG34_Giant_068", 1, 10);
            guard.Keywords.Add(Keyword.Rally);
            var target = TestMinion("shield-target", "SHIELD_TARGET", 1, 10);
            var opponent = TestMinion("opponent", "OPPONENT", 1, 30);

            var result = CombatEngine.SimulateBasicCombat(new[] { guard, target }, new[] { opponent }, 9002, 1);

            Assert.IsTrue(result.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.AddKeywordToOriginalFriendlyMinion &&
                reward.TargetInstanceId == target.InstanceId &&
                reward.CardId == Keyword.DivineShield.ToString()));
        }

        [Test]
        public void Combat_TimewarpedStoneshellCopiesGuardRally()
        {
            var stoneshell = TestMinion("stoneshell", "BG34_Giant_601", 1, 10);
            var guard = TestMinion("guard", "BG34_Giant_068", 1, 10);
            guard.Keywords.Add(Keyword.Rally);
            guard.Keywords.Add(Keyword.DivineShield);
            var target = TestMinion("copied-shield-target", "COPIED_SHIELD_TARGET", 1, 10);
            var opponent = TestMinion("opponent", "OPPONENT", 1, 30);

            var result = CombatEngine.SimulateBasicCombat(new[] { stoneshell, guard, target }, new[] { opponent }, 9003, 1);

            Assert.IsTrue(result.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.AddKeywordToOriginalFriendlyMinion &&
                reward.SourceCardId == "BG34_Giant_601" &&
                reward.TargetInstanceId == target.InstanceId &&
                reward.CardId == Keyword.DivineShield.ToString()));
        }

        [Test]
        public void Combat_TimewarpedUltraliskDoublesStatsAtStartOfCombat()
        {
            var ultralisk = TestMinion("ultralisk", "BG34_Treasure_994", 2, 3);
            var opponent = TestMinion("opponent", "OPPONENT", 1, 30);

            var result = CombatEngine.SimulateBasicCombat(new[] { ultralisk }, new[] { opponent }, 9004, 0);
            var final = result.FinalPlayerBoard.Single(minion => minion.InstanceId == ultralisk.InstanceId);

            Assert.AreEqual(4, final.Attack);
            Assert.AreEqual(6, final.MaxHealth);
        }

        [Test]
        public void Combat_TimewarpedViperIsVenomousAndImmuneWhileAttacking()
        {
            var viper = TestMinion("viper", "BG34_Treasure_990", 1, 1);
            var opponent = TestMinion("opponent", "OPPONENT", 10, 10);

            var result = CombatEngine.SimulateBasicCombat(new[] { viper }, new[] { opponent }, 9005, 1);

            Assert.AreEqual(1, result.FinalPlayerBoard.Count);
            Assert.AreEqual(viper.InstanceId, result.FinalPlayerBoard[0].InstanceId);
            Assert.AreEqual(1, result.FinalPlayerBoard[0].Health);
            Assert.AreEqual(0, result.FinalOpponentBoard.Count);
        }

        private static MatchService CreateService()
        {
            return MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
        }

        private static void AddCardToHand(MatchService service, string cardId, CardKind cardKind)
        {
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, cardKind));
        }

        private static void BuyShopTavernSpell(MatchService service, int shopIndex, string cardId)
        {
            AddCardToHand(service, cardId, CardKind.TavernSpell);
            var hand = service.State.Player.Tavern.Hand;
            var spell = hand[hand.Count - 1];
            hand.RemoveAt(hand.Count - 1);
            spell.InstanceId = "shop-" + cardId + "-" + shopIndex + "-" + service.State.Player.Tavern.RecruitLog.Count;
            service.State.Player.Tavern.Shop[shopIndex] = spell;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));
        }

        private static MinionInstance TestMinion(string instanceId, string cardId, int attack, int health, Tribe tribe = Tribe.None)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = cardId,
                Cost = 3,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Debug,
                OriginPoolSource = PoolSource.Debug,
                PoolCopiesHeld = 0
            };
        }
    }
}
