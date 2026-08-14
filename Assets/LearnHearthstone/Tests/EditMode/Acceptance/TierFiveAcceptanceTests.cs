using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TierFiveAcceptanceTests
    {
        private static readonly string[] TierFiveSpellIds =
        {
            "100899", "100910", "103785", "104448", "104494", "104560",
            "105264", "105265", "110407", "122899", "127503", "127506",
            "110412", "130713"
        };

        [Test]
        public void TierFiveCatalog_CountsAndDuosScopeAreStable()
        {
            var minions = MinionCatalogLoader.LoadFromResources().All.Where(minion => minion.InPool && minion.TavernTier == 5).ToList();
            var spells = SpellCatalogLoader.LoadFromResources().All.Where(spell => spell.InPool && spell.Category == "TavernSpell" && spell.TavernTier == 5).ToList();

            Assert.AreEqual(59, minions.Count);
            Assert.AreEqual(53, minions.Count(minion => !minion.CardId.StartsWith("BGDUO")));
            Assert.AreEqual(6, minions.Count(minion => minion.CardId.StartsWith("BGDUO")));
            Assert.AreEqual(14, spells.Count);
            Assert.AreEqual(TierFiveSpellIds.OrderBy(id => id).ToList(), spells.Select(spell => spell.CardNumber).OrderBy(id => id).ToList());
        }

        [Test]
        public void TierFiveRegistry_CoversEveryTierFiveMinionAndKeepsDuosOutOfScope()
        {
            var tierFiveIds = MinionCatalogLoader.LoadFromResources().All
                .Where(minion => minion.InPool && minion.TavernTier == 5)
                .Select(minion => minion.CardId)
                .OrderBy(id => id)
                .ToList();
            var registeredIds = TierFiveMinionImplementationRegistry.All.Select(entry => entry.CardId).OrderBy(id => id).ToList();

            Assert.AreEqual(tierFiveIds, registeredIds);
            Assert.AreEqual(59, registeredIds.Distinct().Count());
            Assert.IsTrue(TierFiveMinionImplementationRegistry.All.All(entry => !string.IsNullOrWhiteSpace(entry.Area)));
            Assert.IsFalse(TierFiveMinionImplementationRegistry.All.Any(entry => entry.Status == TierFiveImplementationStatus.SoloApproximation));
            Assert.IsFalse(TierFiveMinionImplementationRegistry.All.Any(entry => entry.Status == TierFiveImplementationStatus.KeywordOnly));
            Assert.IsTrue(TierFiveMinionImplementationRegistry.All.Where(entry => entry.CardId.StartsWith("BGDUO")).All(entry => entry.Status == TierFiveImplementationStatus.OutOfScope));
        }

        [Test]
        public void TierFiveTavernSpells_AllResolveWithoutFallingThroughDefault()
        {
            foreach (var spellId in TierFiveSpellIds)
            {
                var service = MatchService.CreateWithDefaultCatalog(9500 + spellId.GetHashCode(), new InMemoryTestScenarioRepository());
                service.State.Player.Tavern.Tier = 5;
                service.State.Player.Tavern.Gold = 10;
                service.State.Player.Tavern.Hand.Clear();
                service.State.Player.Board.Clear();
                var spellTarget = Card("board-undead", BoardSide.Player, "BOARD_UNDEAD", 3, 3, Tribe.Undead);
                spellTarget.Tribes.Add(Tribe.Demon);
                service.State.Player.Board.Add(spellTarget);
                service.State.Player.Tavern.Shop.Clear();
                service.State.Player.Tavern.Shop.Add(Card("shop-a", BoardSide.Player, "SHOP_A", 2, 5, Tribe.Elemental));
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, spellId, CardKind.TavernSpell));

                Assert.DoesNotThrow(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0)), spellId);
                Assert.IsFalse(service.State.Player.Tavern.RecruitLog.Last().Message.Contains("暂未实现"), spellId);
            }
        }

        [Test]
        public void TierFiveBattlecryAuras_BrannRepeatsAndKalecgosBuffsDragons()
        {
            var service = MatchService.CreateWithDefaultCatalog(9510, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(Card("brann", BoardSide.Player, "BG_LOE_077", 2, 4, Tribe.None, Keyword.Battlecry));
            service.State.Player.Board.Add(Card("second-brann", BoardSide.Player, "BG_LOE_077", 2, 4, Tribe.None, Keyword.Battlecry));
            var dragon = Card("dragon", BoardSide.Player, "FRIENDLY_DRAGON", 4, 4, Tribe.Dragon);
            service.State.Player.Board.Add(dragon);
            service.State.Player.Board.Add(Card("kalecgos", BoardSide.Player, "BGS_041", 4, 12, Tribe.Dragon));

            AddAndPlay(service, "BGS_116");

            Assert.AreEqual(6, service.State.Player.Tavern.FreeRefreshes);
            Assert.AreEqual(10, dragon.Attack);
            Assert.AreEqual(10, dragon.MaxHealth);
        }

        [Test]
        public void TierFiveNomi_GrowsCurrentAndFutureTavernElementals()
        {
            var service = MatchService.CreateWithDefaultCatalog(9511, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Shop.Clear();
            var shopElemental = Card("shop-elemental", BoardSide.Player, "SHOP_ELEMENTAL", 2, 2, Tribe.Elemental);
            service.State.Player.Tavern.Shop.Add(shopElemental);

            AddAndPlay(service, "BGS_104");
            AddAndPlay(service, "BGS_123");

            Assert.AreEqual(6, shopElemental.Attack);
            Assert.AreEqual(6, shopElemental.MaxHealth);
            Assert.IsTrue(service.State.Player.Tavern.Growth.ShopModifiers.Any(modifier => modifier.Tribe == Tribe.Elemental && modifier.Attack == 4 && modifier.Health == 4));
        }

        [Test]
        public void TierFiveTavernSpellTriggers_NalaaAndCataclysmicChampionRewardSpells()
        {
            var service = MatchService.CreateWithDefaultCatalog(9512, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 5;
            service.State.Player.Board.Clear();
            var murloc = Card("murloc", BoardSide.Player, "FRIENDLY_MURLOC", 2, 2, Tribe.Murloc);
            var dragon = Card("dragon", BoardSide.Player, "FRIENDLY_DRAGON", 3, 3, Tribe.Dragon);
            service.State.Player.Board.Add(Card("nalaa", BoardSide.Player, "BG28_551", 5, 6, Tribe.None));
            service.State.Player.Board.Add(Card("cataclysmic", BoardSide.Player, "BG35_123", 4, 4, Tribe.None));
            service.State.Player.Board.Add(murloc);
            service.State.Player.Board.Add(dragon);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "109230", CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.GreaterOrEqual(murloc.Attack, 7);
            Assert.GreaterOrEqual(dragon.Attack, 8);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "109230"));
        }

        [Test]
        public void TierFiveDrakkari_DoesNotStackAcrossMultipleCopies()
        {
            var service = MatchService.CreateWithDefaultCatalog(9514, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 5;
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(Card("cataclysmic", BoardSide.Player, "BG35_123", 4, 4, Tribe.None));
            service.State.Player.Board.Add(Card("drakkari-a", BoardSide.Player, "BG26_ICC_901", 1, 5, Tribe.None));
            service.State.Player.Board.Add(Card("drakkari-b", BoardSide.Player, "BG26_ICC_901", 1, 5, Tribe.None));
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "109230", CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "109230"));
        }

        [Test]
        public void TierFiveKelThuzad_ResummonsExactCopyOfLeftUndead()
        {
            var service = MatchService.CreateWithDefaultCatalog(9515, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            AddAndPlay(service, "BG25_008");
            var undead = service.State.Player.Board[0];
            undead.Attack += 50;
            undead.MaxHealth += 50;
            undead.Health = undead.MaxHealth;
            undead.Enchantments.Add(new Enchantment { SourceId = "test-buff", AttackBonus = 50, HealthBonus = 50 });
            AddAndPlay(service, "BG28_308");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var copy = service.State.Player.Board[0];
            Assert.AreEqual("BG25_008", copy.CardId);
            Assert.AreEqual(undead.Attack, copy.Attack);
            Assert.AreEqual(undead.MaxHealth, copy.MaxHealth);
            Assert.IsTrue(copy.Enchantments.Any(enchantment => enchantment.SourceId == "test-buff"));
        }

        [Test]
        public void TierFiveKelThuzad_DeathrattleThenRebornThenExactCopy()
        {
            var service = MatchService.CreateWithDefaultCatalog(95151, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            AddAndPlay(service, "BG28_300");
            var bonehead = service.State.Player.Board[0];
            bonehead.Keywords.Add(Keyword.Reborn);
            bonehead.Attack += 7;
            bonehead.MaxHealth += 7;
            bonehead.Health = bonehead.MaxHealth;
            bonehead.Enchantments.Add(new Enchantment { Id = "bonehead-buff", SourceId = "bonehead-buff", AttackBonus = 7, HealthBonus = 7 });
            AddAndPlay(service, "BG28_308");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Board.Count(minion => minion.Name == "Skeleton"));
            var copies = service.State.Player.Board.Where(minion => minion.CardId == "BG28_300").ToList();
            Assert.AreEqual(2, copies.Count);
            var reborn = copies.Single(minion => !minion.Keywords.Contains(Keyword.Reborn));
            var exactCopy = copies.Single(minion => minion.Keywords.Contains(Keyword.Reborn));
            Assert.AreEqual(1, reborn.Health);
            Assert.That(reborn.InstanceId, Does.Contain("-reborn-"));
            Assert.IsFalse(reborn.Enchantments.Any(enchantment => enchantment.Id == "bonehead-buff"));
            Assert.IsTrue(exactCopy.InstanceId.StartsWith("kel-thuzad-"));
            Assert.IsTrue(exactCopy.Enchantments.Any(enchantment => enchantment.Id == "bonehead-buff"));
        }

        [Test]
        public void TierFiveKelThuzad_SixRecruitSkeletonsCreateTwoGoldenTriplesAndFreeBoardSpace()
        {
            var service = MatchService.CreateWithDefaultCatalog(95153, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            for (var index = 0; index < 5; index += 1)
            {
                var skeleton = Card("pre-skeleton-" + index, BoardSide.Player, "SKELETON", 1, 1, Tribe.Undead);
                skeleton.DefinitionId = "skeleton";
                skeleton.Name = "Skeleton";
                service.State.Player.Board.Add(skeleton);
            }

            AddAndPlay(service, "BG28_300");
            AddAndPlay(service, "BG28_308");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(minion => minion.DefinitionId == "skeleton" && minion.Golden));
            Assert.AreEqual(1, service.State.Player.Board.Count(minion => minion.DefinitionId == "skeleton" && !minion.Golden));
            Assert.IsTrue(service.State.Player.Board.Any(minion => minion.InstanceId.StartsWith("kel-thuzad-")));
        }

        [Test]
        public void TierFiveKelThuzad_RebornFillsBoardBeforeExactCopy()
        {
            var service = MatchService.CreateWithDefaultCatalog(95152, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            for (var index = 0; index < 5; index += 1)
            {
                service.State.Player.Board.Add(Card("kel-filler-" + index, BoardSide.Player, "KEL_FILLER_" + index, 1, 1, Tribe.None));
            }
            AddAndPlay(service, "BG25_008");
            var undead = service.State.Player.Board.Single(minion => minion.CardId == "BG25_008");
            undead.Keywords.Add(Keyword.Reborn);
            AddAndPlay(service, "BG28_308");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(7, service.State.Player.Board.Count);
            Assert.AreEqual(1, service.State.Player.Board.Count(minion => minion.CardId == "BG25_008"));
            Assert.IsFalse(service.State.Player.Board.Any(minion => minion.InstanceId.StartsWith("kel-thuzad-")));
        }

        [Test]
        public void TierFiveAshenCorruptor_RewindsHeroDamageAndBuffsCurrentTavern()
        {
            var service = MatchService.CreateWithDefaultCatalog(9516, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Shop.Clear();
            var shop = Card("shop", BoardSide.Player, "SHOP", 2, 2, Tribe.None);
            service.State.Player.Tavern.Shop.Add(shop);
            AddAndPlay(service, "BGS_004");
            var healthBefore = service.State.Player.Health;

            AddAndPlay(service, "BG32_873");

            Assert.AreEqual(healthBefore, service.State.Player.Health);
            Assert.AreEqual(3, shop.Attack);
            Assert.AreEqual(3, shop.MaxHealth);
        }

        [Test]
        public void TierFiveVoidpupTrainer_GrowthKeepsTierThreeCap()
        {
            var service = MatchService.CreateWithDefaultCatalog(9517, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Shop.Clear();
            var low = Card("low", BoardSide.Player, "LOW", 2, 2, Tribe.None);
            low.TavernTier = 3;
            var high = Card("high", BoardSide.Player, "HIGH", 4, 4, Tribe.None);
            high.TavernTier = 4;
            service.State.Player.Tavern.Shop.Add(low);
            service.State.Player.Tavern.Shop.Add(high);

            AddAndPlay(service, "BG35_152");

            Assert.AreEqual(5, low.Attack);
            Assert.AreEqual(4, high.Attack);
            Assert.IsTrue(service.State.Player.Tavern.Growth.ShopModifiers.Any(modifier => modifier.TierCap == 3));
        }

        [Test]
        public void TierFiveExtraTavernSpellAuras_ProudPrivateerRecastsButMaelstromDoesNotTriggerInRecruit()
        {
            var bounty = MatchService.CreateWithDefaultCatalog(9518, new InMemoryTestScenarioRepository());
            bounty.State.Player.Board.Clear();
            var bountyTarget = Card("target", BoardSide.Player, "TARGET", 1, 1, Tribe.None);
            bounty.State.Player.Board.Add(bountyTarget);
            bounty.State.Player.Board.Add(Card("privateer", BoardSide.Player, "BG33_825", 4, 4, Tribe.Pirate));
            bounty.Apply(new GameCommand(GameCommandType.AddCardToHand, "122184", CardKind.TavernSpell));
            bounty.Apply(new GameCommand(GameCommandType.PlayMinion, bounty.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(13, bountyTarget.Attack);

            var goldenBounty = MatchService.CreateWithDefaultCatalog(9520, new InMemoryTestScenarioRepository());
            goldenBounty.State.Player.Board.Clear();
            var goldenBountyTarget = Card("golden-target", BoardSide.Player, "TARGET", 1, 1, Tribe.None);
            var goldenPrivateer = Card("golden-privateer", BoardSide.Player, "BG33_825", 8, 8, Tribe.Pirate);
            goldenPrivateer.Golden = true;
            goldenBounty.State.Player.Board.Add(goldenBountyTarget);
            goldenBounty.State.Player.Board.Add(goldenPrivateer);
            goldenBounty.Apply(new GameCommand(GameCommandType.AddCardToHand, "122184", CardKind.TavernSpell));
            goldenBounty.Apply(new GameCommand(GameCommandType.PlayMinion, goldenBounty.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(19, goldenBountyTarget.Attack);

            goldenBounty.State.Player.Board.Add(Card("normal-privateer", BoardSide.Player, "BG33_825", 4, 4, Tribe.Pirate));
            goldenBounty.Apply(new GameCommand(GameCommandType.AddCardToHand, "122184", CardKind.TavernSpell));
            goldenBounty.Apply(new GameCommand(GameCommandType.PlayMinion, goldenBounty.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(43, goldenBountyTarget.Attack);

            var maelstrom = MatchService.CreateWithDefaultCatalog(9519, new InMemoryTestScenarioRepository());
            maelstrom.State.Player.Board.Clear();
            var boardTarget = Card("board-target", BoardSide.Player, "BOARD_TARGET", 1, 1, Tribe.None);
            maelstrom.State.Player.Board.Add(boardTarget);
            maelstrom.State.Player.Board.Add(Card("maelstrom", BoardSide.Player, "BG34_922", 4, 4, Tribe.Naga));
            maelstrom.Apply(new GameCommand(GameCommandType.AddCardToHand, "109230", CardKind.TavernSpell));
            maelstrom.Apply(new GameCommand(GameCommandType.PlayMinion, maelstrom.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(2, boardTarget.Attack);
        }

        [Test]
        public void TierFiveMaelstromEmergent_NormalAndGoldenRepeatStartOfCombatTavernSpells()
        {
            AssertMaelstromCombatSpellStats(false, 5, 22);
            AssertMaelstromCombatSpellStats(true, 7, 23);
        }

        [Test]
        public void TierFiveCombatDeathrattles_TitusStacksForScrapScraperReward()
        {
            var player = new[]
            {
                Card("titus-a", BoardSide.Player, "BG25_354", 0, 30, Tribe.None),
                Card("titus-b", BoardSide.Player, "BG25_354", 0, 30, Tribe.None),
                Card("scrap", BoardSide.Player, "BG26_148", 1, 1, Tribe.Mech, Keyword.Taunt, Keyword.Deathrattle)
            };
            var opponent = new[]
            {
                Card("opponent", BoardSide.Opponent, "OPPONENT", 5, 5, Tribe.None)
            };

            var result = CombatEngine.SimulateBasicCombat(player, opponent, 9513, 20, new TavernState());

            Assert.AreEqual(3, result.PlayerRewards.Where(reward => reward.Type == CombatRewardType.AddRandomMagneticMechToHand).Sum(reward => reward.Amount));
        }

        [Test]
        public void TierFiveCombatDeathrattles_LeeroyDestroysKillerAndKangorRebuildsFirstDeadMech()
        {
            var leeroyPlayer = new[]
            {
                Card("leeroy", BoardSide.Player, "BG23_318", 0, 1, Tribe.None, Keyword.Taunt, Keyword.Deathrattle)
            };
            var leeroyOpponent = new[]
            {
                Card("killer", BoardSide.Opponent, "KILLER", 1, 5, Tribe.None, Keyword.Taunt),
                Card("bigger", BoardSide.Opponent, "BIGGER", 20, 20, Tribe.None)
            };

            var leeroyResult = CombatEngine.SimulateBasicCombat(leeroyPlayer, leeroyOpponent, 9520, 20, new TavernState());

            Assert.IsFalse(leeroyResult.FinalOpponentBoard.Any(minion => minion.InstanceId == "killer"));
            Assert.IsTrue(leeroyResult.FinalOpponentBoard.Any(minion => minion.InstanceId == "bigger"));

            var kangorPlayer = new[]
            {
                Card("mech", BoardSide.Player, "DEAD_MECH", 1, 1, Tribe.Mech, Keyword.Taunt),
                Card("kangor", BoardSide.Player, "BGS_012", 0, 1, Tribe.None, Keyword.Deathrattle)
            };
            var kangorOpponent = new[]
            {
                Card("kangor-killer", BoardSide.Opponent, "KANGOR_KILLER", 1, 10, Tribe.None)
            };

            var kangorResult = CombatEngine.SimulateBasicCombat(kangorPlayer, kangorOpponent, 9521, 30, new TavernState());

            Assert.IsTrue(kangorResult.Replay.Frames.Any(frame => frame.PlayerBoardSnapshot.Minions.Any(minion => minion.CardId == "DEAD_MECH" && minion.InstanceId.StartsWith("kangor"))));
        }

        private static void AddAndPlay(MatchService service, string cardId)
        {
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
        }

        private static void AssertMaelstromCombatSpellStats(bool golden, int expectedAttack, int expectedHealth)
        {
            var service = MatchService.CreateWithDefaultCatalog(golden ? 9523 : 9522, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var target = Card("maelstrom-target", BoardSide.Player, "TARGET", 1, 20, Tribe.None);
            var maelstrom = Card("maelstrom-source", BoardSide.Player, "BG34_922", 0, 20, Tribe.Naga);
            maelstrom.Golden = golden;
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(maelstrom);
            service.State.Opponent.Board.Add(Card("maelstrom-wall", BoardSide.Opponent, "WALL", 0, 100, Tribe.None));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "105665", CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = golden ? 9523 : 9522, SafetyLimit = 1 }));

            var finalTarget = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == target.InstanceId);
            Assert.AreEqual(expectedAttack, finalTarget.Attack);
            Assert.AreEqual(expectedHealth, finalTarget.MaxHealth);
        }

        private static MinionInstance Card(string id, BoardSide owner, string cardId, int attack, int health, Tribe tribe, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                InstanceId = id,
                DefinitionId = id,
                CardId = cardId,
                Name = id,
                Attack = attack,
                BaseAttack = attack,
                Health = health,
                MaxHealth = health,
                BaseHealth = health,
                Owner = owner,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>(),
                CanAttack = true
            };
        }
    }
}
