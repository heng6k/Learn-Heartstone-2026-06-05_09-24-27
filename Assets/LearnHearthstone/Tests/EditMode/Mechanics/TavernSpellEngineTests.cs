using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TavernSpellEngineTests
    {
        [Test]
        public void Cast_GoldenTouchSynchronizesGoldenDescription()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            var minions = MinionCatalogLoader.LoadFromResources();
            var definition = minions.GetByCardId("BG28_300");
            state.Player.Tavern.Shop.Clear();
            state.Player.Tavern.Shop.Add(new MinionInstance
            {
                DefinitionId = definition.Id,
                CardId = definition.CardId,
                Text = definition.Text,
                BaseAttack = definition.BaseAttack,
                BaseHealth = definition.BaseHealth,
                Attack = definition.BaseAttack,
                Health = definition.BaseHealth,
                MaxHealth = definition.BaseHealth,
                CardKind = CardKind.Minion
            });

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "104448", Name = "Golden Touch" },
                state,
                minions,
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            Assert.IsTrue(state.Player.Tavern.Shop[0].Golden);
            Assert.AreEqual(definition.Golden.Text, state.Player.Tavern.Shop[0].Text);
        }

        [Test]
        public void Cast_BloodGemBarrageSnapshotsSpellScalingAndDefersBloodGemQuality()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Player.Tavern.SpellPower = 2;
            state.Player.Tavern.TavernSpellBonusAttack = 3;
            state.Player.Tavern.TavernSpellBonusHealth = 4;
            state.Player.Tavern.BloodGemBonusAttack = 5;
            state.Player.Tavern.BloodGemBonusHealth = 6;
            var spell = new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                CardId = "126676",
                Name = "鲜血宝石弹幕"
            };

            var result = TavernSpellEngine.Cast(
                spell,
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            Assert.IsTrue(result.Contains("Blood Gem Barrage"));
            Assert.AreEqual(1, state.Player.Tavern.Growth.ShopModifiers.Count);
            Assert.AreEqual(BuffScope.ShopGlobal, state.Player.Tavern.Growth.ShopModifiers[0].Scope);
            Assert.AreEqual(6, state.Player.Tavern.Growth.ShopModifiers[0].Attack);
            Assert.AreEqual(7, state.Player.Tavern.Growth.ShopModifiers[0].Health);
            Assert.AreEqual(EnchantmentKind.BloodGem, state.Player.Tavern.Growth.ShopModifiers[0].EnchantmentKind);
        }

        [Test]
        public void Refresh_BloodGemBarrageUsesCurrentQualityAndAttachesBloodGems()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            tavern.SpellPower = 2;
            tavern.TavernSpellBonusAttack = 3;
            tavern.TavernSpellBonusHealth = 4;
            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "126676", Name = "Blood Gem Barrage" },
                service.State,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            tavern.BloodGemBonusAttack = 7;
            tavern.BloodGemBonusHealth = 8;
            tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var gems = tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .Select(card => card.Enchantments.Single(StatMath.IsBloodGemEnchantment))
                .ToList();
            Assert.IsNotEmpty(gems);
            Assert.IsTrue(gems.All(gem => gem.Kind == EnchantmentKind.BloodGem));
            Assert.IsTrue(gems.All(gem => gem.AttackBonus == 13));
            Assert.IsTrue(gems.All(gem => gem.HealthBonus == 15));
        }

        [Test]
        public void Play_RebornBloodGemAppliesBloodGemQualityOnce()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestBoardMinion("quilboar", "Quilboar", 1, 1, Tribe.Quilboar));
            service.State.Player.Tavern.BloodGemBonusAttack = 2;
            service.State.Player.Tavern.BloodGemBonusHealth = 3;

            var target = service.State.Player.Board[0];
            var beforeAttack = target.Attack;
            var beforeHealth = target.MaxHealth;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "REBORN_BLOOD_GEM", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(beforeAttack + 3, target.Attack);
            Assert.AreEqual(beforeHealth + 4, target.MaxHealth);
            Assert.Contains(Keyword.Reborn, target.Keywords);
            var gem = target.Enchantments.Single(StatMath.IsBloodGemEnchantment);
            Assert.AreEqual(EnchantmentKind.BloodGem, gem.Kind);
            Assert.AreEqual(3, gem.AttackBonus);
            Assert.AreEqual(4, gem.HealthBonus);
        }

        [Test]
        public void Play_BloodGemTargetsTavernMinionAndRemainsAttachedAfterPurchase()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var target = tavern.Shop[shopIndex];
            var beforeAttack = target.Attack;
            var beforeHealth = target.MaxHealth;
            tavern.BloodGemBonusAttack = 2;
            tavern.BloodGemBonusHealth = 3;
            tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BLOOD_GEM", CardKind.Spell));

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                shopIndex,
                TargetZone.TavernShop,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.AreEqual(beforeAttack + 3, target.Attack);
            Assert.AreEqual(beforeHealth + 4, target.MaxHealth);
            Assert.AreEqual(EnchantmentKind.BloodGem, target.Enchantments.Single(StatMath.IsBloodGemEnchantment).Kind);

            service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));

            var bought = tavern.Hand.Single(card => card.InstanceId == target.InstanceId);
            Assert.AreSame(target, bought);
            Assert.AreEqual(3, bought.Enchantments.Single(StatMath.IsBloodGemEnchantment).AttackBonus);
            Assert.AreEqual(4, bought.Enchantments.Single(StatMath.IsBloodGemEnchantment).HealthBonus);
        }

        [Test]
        public void Cast_PerfectVisionUsesSpellPowerAndPreservesHiddenBloodGem()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Player.Board.Clear();
            var target = TestBoardMinion("vision", "Vision Target", 4, 5, Tribe.Quilboar);
            StatMath.ApplyEnchantment(target, BloodGem(3, 4, "Before Perfect Vision"));
            state.Player.Board.Add(target);
            state.Player.Tavern.SpellPower = 2;
            state.Player.Tavern.TavernSpellBonusAttack = 1;
            state.Player.Tavern.TavernSpellBonusHealth = 3;

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "104601", Name = "Perfect Vision" },
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1),
                0);

            Assert.AreEqual(23, target.Attack);
            Assert.AreEqual(25, target.MaxHealth);
            Assert.AreEqual(25, target.Health);
            Assert.AreEqual(1, target.Enchantments.Count(StatMath.IsBloodGemEnchantment));
            Assert.IsTrue(target.Enchantments.Any(enchantment => enchantment.Kind == EnchantmentKind.SetStats));
        }

        [Test]
        public void Cast_BloodGemScraperMovesHiddenGemButLeavesSpecialKeyword()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Player.Board.Clear();
            state.Player.Tavern.BloodGemBonusAttack = 2;
            state.Player.Tavern.BloodGemBonusHealth = 3;
            var source = TestBoardMinion("source", "Source", 5, 5, Tribe.Quilboar);
            var target = TestBoardMinion("target", "Target", 1, 1, Tribe.Quilboar);
            state.Player.Board.Add(source);
            state.Player.Board.Add(target);
            var minions = MinionCatalogLoader.LoadFromResources();
            var spells = SpellCatalogLoader.LoadFromResources();

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.Spell, CardId = "BRISTLEBACK_BLOOD_GEM", Name = "Bristleback Blood Gem" },
                state,
                minions,
                spells,
                new SeededRng(1),
                0);
            StatMath.SetStats(source, 2, 2, "See the Light");

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "110642", Name = "Blood Gem Scraper" },
                state,
                minions,
                spells,
                new SeededRng(2),
                1);

            Assert.AreEqual(2, source.Attack);
            Assert.AreEqual(2, source.MaxHealth);
            Assert.Contains(Keyword.Taunt, source.Keywords);
            Assert.IsFalse(source.Enchantments.Any(StatMath.IsBloodGemEnchantment));
            Assert.AreEqual(10, target.Attack);
            Assert.AreEqual(13, target.MaxHealth);
            Assert.AreEqual(3, target.Enchantments.Count(StatMath.IsBloodGemEnchantment));
        }

        [Test]
        public void Cast_DevourTransfersFinalStatsAsOrdinaryBuff()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Player.Board.Clear();
            var consumed = TestBoardMinion("consumed", "Consumed", 3, 4, Tribe.Quilboar);
            var receiver = TestBoardMinion("receiver", "Receiver", 1, 1, Tribe.Demon);
            StatMath.ApplyEnchantment(consumed, BloodGem(2, 3, "Consumed Blood Gem"));
            state.Player.Board.Add(consumed);
            state.Player.Board.Add(receiver);

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "100899", Name = "Invoke the Devourer" },
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1),
                0);

            Assert.AreEqual(1, state.Player.Board.Count);
            Assert.AreSame(receiver, state.Player.Board[0]);
            Assert.AreEqual(6, receiver.Attack);
            Assert.AreEqual(8, receiver.MaxHealth);
            Assert.IsFalse(receiver.Enchantments.Any(StatMath.IsBloodGemEnchantment));
        }

        [Test]
        public void PersistCombatGains_KeepsBloodGemIdentityAndFlattensOtherStats()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var original = TestBoardMinion("poet-dragon", "Poet Dragon", 2, 2, Tribe.Dragon);
            var final = original.Clone();
            StatMath.ApplyEnchantment(final, BloodGem(3, 4, "Combat Blood Gem"));
            StatMath.ApplyEnchantment(final, new Enchantment
            {
                Id = "Combat ordinary",
                SourceId = "Combat ordinary",
                AttackBonus = 5,
                HealthBonus = 6
            });
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(original);
            var persist = typeof(MatchService).GetMethod("PersistPositiveCombatDelta", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(persist);
            persist.Invoke(service, new object[] { original, final, "Persistent Poet", 1 });

            Assert.AreEqual(10, original.Attack);
            Assert.AreEqual(12, original.MaxHealth);
            Assert.AreEqual(1, original.Enchantments.Count(StatMath.IsBloodGemEnchantment));
            Assert.IsTrue(original.Enchantments.Any(enchantment => enchantment.SourceId == "Persistent Poet" && enchantment.Kind != EnchantmentKind.BloodGem));

            var target = TestBoardMinion("poet-target", "Poet Target", 1, 1, Tribe.Quilboar);
            service.State.Player.Board.Add(target);
            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "110642", Name = "Blood Gem Scraper" },
                service.State,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(3),
                1);

            Assert.AreEqual(7, original.Attack);
            Assert.AreEqual(8, original.MaxHealth);
            Assert.IsFalse(original.Enchantments.Any(StatMath.IsBloodGemEnchantment));
            Assert.AreEqual(6, target.Attack);
            Assert.AreEqual(7, target.MaxHealth);
        }

        [Test]
        public void PersistCombatGains_DoesNotDuplicateAlreadyPermanentBloodAmuletGem()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var original = TestBoardMinion("amulet-tarecgosa", "Amulet Tarecgosa", 2, 2, Tribe.Dragon);
            var final = original.Clone();
            StatMath.ApplyEnchantment(final, BloodGem(3, 4, "BG35_MagicItem_432"));
            var persist = typeof(MatchService).GetMethod("PersistPositiveCombatDelta", BindingFlags.Instance | BindingFlags.NonPublic);

            persist.Invoke(service, new object[] { original, final, "Tarecgosa", 1 });

            Assert.AreEqual(2, original.Attack);
            Assert.AreEqual(2, original.MaxHealth);
            Assert.IsFalse(original.Enchantments.Any(StatMath.IsBloodGemEnchantment));
        }

        [Test]
        public void Clone_DeepCopiesBloodGemEnchantments()
        {
            var original = TestBoardMinion("clone", "Clone", 2, 2, Tribe.Quilboar);
            StatMath.ApplyEnchantment(original, BloodGem(3, 4, "Clone Blood Gem"));

            var clone = original.Clone();
            clone.Enchantments[0].AttackBonus = 99;
            clone.Enchantments[0].AddedKeywords.Add(Keyword.Taunt);

            Assert.AreEqual(3, original.Enchantments[0].AttackBonus);
            Assert.IsEmpty(original.Enchantments[0].AddedKeywords);
        }

        [Test]
        public void Cast_TierOneGeneratedAndEconomySpellsResolve()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            var minion = state.Player.Tavern.Shop[0].Clone();
            state.Player.Board.Add(minion);

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "BLOOD_GEM", Name = "鲜血宝石" },
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            Assert.AreEqual(minion.BaseAttack + 1, state.Player.Board[0].Attack);
            Assert.AreEqual(minion.BaseHealth + 1, state.Player.Board[0].MaxHealth);

            state.Player.Tavern.Gold = state.Player.Tavern.MaxGold;
            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "104436", Name = "酒馆币" },
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            Assert.AreEqual(state.Player.Tavern.MaxGold + 1, state.Player.Tavern.Gold);
        }

        [Test]
        public void Cast_MenagerieTablewareUsesAnalyzerDistinctTribeCount()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Player.Board.Clear();
            state.Player.Board.Add(TestBoardMinion("all", "All", 1, 1, Tribe.All));

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "130527", Name = "Menagerie Tableware" },
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            Assert.AreEqual(31, state.Player.Board[0].Attack);
            Assert.AreEqual(31, state.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Cast_MisplacedTeaSetSelectsEachMinionAtMostOnce()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Player.Board.Clear();
            state.Player.Board.Add(TestBoardMinion("all", "All", 1, 1, Tribe.All));
            state.Player.Board.Add(TestBoardMinion("dragon", "Dragon", 1, 1, Tribe.Dragon));

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "105271", Name = "乱放的茶具" },
                state,
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                new SeededRng(1));

            Assert.AreEqual(3, state.Player.Board[0].Attack);
            Assert.AreEqual(3, state.Player.Board[0].MaxHealth);
            Assert.AreEqual(3, state.Player.Board[1].Attack);
            Assert.AreEqual(3, state.Player.Board[1].MaxHealth);
        }

        [Test]
        public void StartOfCombatSpellQueue_PreventsDuplicateAndConsumesAllEffects()
        {
            var tavern = new TavernState();

            Assert.IsTrue(TavernSpellEngine.TryQueueStartOfCombatSpell("110401", tavern));
            Assert.IsFalse(TavernSpellEngine.TryQueueStartOfCombatSpell("110401", tavern));
            Assert.AreEqual(2, tavern.NextCombatBeetles);
            CollectionAssert.AreEqual(new[] { "110401" }, tavern.NextCombatTavernSpellCardIds);

            TavernSpellEngine.ConsumeStartOfCombatSpells(tavern);

            Assert.AreEqual(0, tavern.NextCombatBeetles);
            Assert.IsEmpty(tavern.NextCombatTavernSpellCardIds);
            Assert.IsTrue(TavernSpellEngine.TryQueueStartOfCombatSpell("110401", tavern));
        }

        [Test]
        public void MatchService_OpponentStartOfCombatSpellsMaterializeAndExpireAfterCombat()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();

            service.Apply(new GameCommand(GameCommandType.SetOpponentStartOfCombatSpell, "110401", CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.SetOpponentStartOfCombatSpell, "104560", CardKind.TavernSpell));

            var preview = service.GetOpponentCombatTavernStatePreview();
            Assert.AreEqual(2, preview.NextCombatBeetles);
            Assert.AreEqual(1, preview.NextCombatEnemyHealthToOne);
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.SetOpponentStartOfCombatSpell, "110401", CardKind.TavernSpell)));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 91, SafetyLimit = 4 }));

            Assert.IsEmpty(service.State.Opponent.NextCombatTavernSpellCardIds);
            Assert.AreEqual(0, service.GetOpponentCombatTavernStatePreview().NextCombatBeetles);
            Assert.DoesNotThrow(() =>
                service.Apply(new GameCommand(GameCommandType.SetOpponentStartOfCombatSpell, "110401", CardKind.TavernSpell)));
        }

        [Test]
        public void MatchService_UntriggeredPlayerStartOfCombatSpellsStillExpire()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var template = service.State.Player.Tavern.Shop[0];
            for (var index = 0; index < 7; index += 1)
            {
                var minion = template.Clone();
                minion.InstanceId = "full-board-" + index;
                service.State.Player.Board.Add(minion);
            }

            Assert.IsTrue(TavernSpellEngine.TryQueueStartOfCombatSpell("110401", service.State.Player.Tavern));
            Assert.IsTrue(TavernSpellEngine.TryQueueStartOfCombatSpell("104560", service.State.Player.Tavern));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 92, SafetyLimit = 4 }));

            Assert.AreEqual(0, service.State.Player.Tavern.NextCombatBeetles);
            Assert.AreEqual(0, service.State.Player.Tavern.NextCombatEnemyHealthToOne);
            Assert.IsEmpty(service.State.Player.Tavern.NextCombatTavernSpellCardIds);
        }

        private static MinionInstance TestBoardMinion(string id, string name, int attack, int health, params Tribe[] tribes)
        {
            return new MinionInstance
            {
                InstanceId = id,
                DefinitionId = id,
                CardId = id,
                Name = name,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Tribes = new List<Tribe>(tribes),
                Owner = BoardSide.Player
            };
        }

        private static Enchantment BloodGem(int attack, int health, string source)
        {
            return new Enchantment
            {
                Id = source,
                SourceId = source,
                Kind = EnchantmentKind.BloodGem,
                AttackBonus = attack,
                HealthBonus = health
            };
        }
    }
}
