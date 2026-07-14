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
    public sealed class TierSixSevenAcceptanceTests
    {
        private static readonly string[] TierSixSpellIds =
        {
            "100601", "100911", "104601", "109232", "127642", "113902"
        };

        private static readonly string[] TierSevenSpellIds =
        {
            "103796", "119599", "119603", "119718", "130527"
        };

        [Test]
        public void TierSixSevenCatalog_CountsAndDuosScopeAreStable()
        {
            var minions = MinionCatalogLoader.LoadFromResources().All.Where(minion => minion.InPool).ToList();
            var spells = SpellCatalogLoader.LoadFromResources().All
                .Where(spell => spell.InPool && spell.Category == "TavernSpell")
                .ToList();

            var tierSixMinions = minions.Where(minion => minion.TavernTier == 6).ToList();
            var tierSevenMinions = minions.Where(minion => minion.TavernTier == 7).ToList();

            Assert.AreEqual(38, tierSixMinions.Count);
            Assert.AreEqual(35, tierSixMinions.Count(minion => !minion.CardId.StartsWith("BGDUO")));
            Assert.AreEqual(3, tierSixMinions.Count(minion => minion.CardId.StartsWith("BGDUO")));
            Assert.AreEqual(13, tierSevenMinions.Count);
            Assert.AreEqual(12, tierSevenMinions.Count(minion => !minion.CardId.StartsWith("BGDUO")));
            Assert.AreEqual(1, tierSevenMinions.Count(minion => minion.CardId.StartsWith("BGDUO")));
            Assert.AreEqual(TierSixSpellIds.OrderBy(id => id).ToList(), spells.Where(spell => spell.TavernTier == 6).Select(spell => spell.CardNumber).OrderBy(id => id).ToList());
            Assert.AreEqual(TierSevenSpellIds.OrderBy(id => id).ToList(), spells.Where(spell => spell.TavernTier == 7).Select(spell => spell.CardNumber).OrderBy(id => id).ToList());
        }

        [Test]
        public void TierSixSevenRegistry_CoversEveryHighTierMinionAndKeepsDuosOutOfScope()
        {
            var highTierIds = MinionCatalogLoader.LoadFromResources().All
                .Where(minion => minion.InPool && (minion.TavernTier == 6 || minion.TavernTier == 7))
                .Select(minion => minion.CardId)
                .OrderBy(id => id)
                .ToList();
            var registeredIds = TierSixSevenMinionImplementationRegistry.All.Select(entry => entry.CardId).OrderBy(id => id).ToList();

            Assert.AreEqual(highTierIds, registeredIds);
            Assert.AreEqual(51, registeredIds.Distinct().Count());
            Assert.IsTrue(TierSixSevenMinionImplementationRegistry.All.All(entry => !string.IsNullOrWhiteSpace(entry.Area)));
            Assert.IsTrue(TierSixSevenMinionImplementationRegistry.All.Where(entry => entry.CardId.StartsWith("BGDUO")).All(entry => entry.Status == HighTierImplementationStatus.OutOfScope));
            Assert.IsTrue(TierSixSevenMinionImplementationRegistry.All.Where(entry => !entry.CardId.StartsWith("BGDUO")).All(entry => entry.Status == HighTierImplementationStatus.Implemented));
        }

        [Test]
        public void TierSixSevenTavernSpells_AllResolveWithoutFallingThroughDefault()
        {
            foreach (var spellId in TierSixSpellIds.Concat(TierSevenSpellIds))
            {
                var service = MatchService.CreateWithDefaultCatalog(9600 + StableNumber(spellId), new InMemoryTestScenarioRepository());
                service.State.Player.Tavern.Tier = 7;
                service.State.Player.Tavern.Gold = 10;
                service.State.Player.Tavern.Hand.Clear();
                service.State.Player.Board.Clear();
                service.State.Player.Tavern.Shop.Clear();
                service.State.Player.Board.Add(Card("target-murloc", BoardSide.Player, "TARGET_MURLOC", 2, 5, Tribe.Murloc, 3));
                service.State.Player.Board.Add(Card("target-dragon", BoardSide.Player, "TARGET_DRAGON", 6, 10, Tribe.Dragon, 6));
                service.State.Player.Tavern.Shop.Add(Card("shop-murloc", BoardSide.Player, "SHOP_MURLOC", 4, 8, Tribe.Murloc, 6));
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, spellId, CardKind.TavernSpell));

                Assert.DoesNotThrow(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1)), spellId);
                Assert.IsFalse((service.State.Player.Tavern.RecruitLog.Last().Message ?? string.Empty).Contains("\u6682\u672a\u5b9e\u73b0"), spellId);
            }
        }

        [Test]
        public void TierSixSevenTavernSpells_ChooseFriendlyTargetsWhenTargetIndexIsProvided()
        {
            var blade = MatchService.CreateWithDefaultCatalog(9607, new InMemoryTestScenarioRepository());
            blade.State.Player.Tavern.Tier = 7;
            blade.State.Player.Tavern.Hand.Clear();
            blade.State.Player.Board.Clear();
            blade.State.Player.Board.Add(Card("largest", BoardSide.Player, "LARGEST", 20, 30, Tribe.None, 7));
            blade.State.Player.Board.Add(Card("chosen", BoardSide.Player, "CHOSEN", 2, 3, Tribe.Murloc, 3));
            blade.Apply(new GameCommand(GameCommandType.AddCardToHand, "119603", CardKind.TavernSpell));

            blade.Apply(new GameCommand(GameCommandType.PlayMinion, blade.State.Player.Tavern.Hand.Count - 1, 1));

            Assert.AreEqual(20, blade.State.Player.Board[1].Attack);
            Assert.AreEqual(30, blade.State.Player.Board[1].MaxHealth);

            var eyes = MatchService.CreateWithDefaultCatalog(9608, new InMemoryTestScenarioRepository());
            eyes.State.Player.Tavern.Tier = 7;
            eyes.State.Player.Tavern.Hand.Clear();
            eyes.State.Player.Board.Clear();
            eyes.State.Player.Board.Add(Card("tier-five", BoardSide.Player, "TIER_FIVE", 5, 5, Tribe.None, 5));
            eyes.State.Player.Board.Add(Card("tier-four", BoardSide.Player, "TIER_FOUR", 4, 4, Tribe.Dragon, 4));
            eyes.Apply(new GameCommand(GameCommandType.AddCardToHand, "100601", CardKind.TavernSpell));

            eyes.Apply(new GameCommand(GameCommandType.PlayMinion, eyes.State.Player.Tavern.Hand.Count - 1, 1));

            Assert.IsFalse(eyes.State.Player.Board[0].Golden);
            Assert.IsTrue(eyes.State.Player.Board[1].Golden);
            Assert.AreEqual(8, eyes.State.Player.Board[1].Attack);
            Assert.AreEqual(8, eyes.State.Player.Board[1].MaxHealth);
        }

        [Test]
        public void TierSixNagaSpellPackage_SlitherspearConvertsMatriarchHealthIntoAttack()
        {
            var service = MatchService.CreateWithDefaultCatalog(9610, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 6;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(Card("slitherspear", BoardSide.Player, "BG33_920", 4, 8, Tribe.Naga, 6));
            service.State.Player.Board.Add(Card("matriarch", BoardSide.Player, "BG33_923", 4, 4, Tribe.Naga, 6));
            var target = Card("target-naga", BoardSide.Player, "TARGET_NAGA", 2, 5, Tribe.Naga, 3);
            service.State.Player.Board.Add(target);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "109230", CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(6, target.Attack);
            Assert.AreEqual(9, target.MaxHealth);
        }

        [Test]
        public void TierSixMoonsteel_EndTurnAddsMagneticSatellites()
        {
            var service = MatchService.CreateWithDefaultCatalog(9611, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            AddAndPlay(service, "BG31_171");
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var satellites = service.State.Player.Tavern.Hand.Where(card => card.CardId == "MOONSTEEL_SATELLITE").ToList();
            Assert.AreEqual(2, satellites.Count);
            Assert.IsTrue(satellites.All(card => card.Keywords.Contains(Keyword.Magnetic)));
            Assert.IsTrue(satellites.All(card => card.Attack == 6 && card.MaxHealth == 6));
        }

        [Test]
        public void TierSixSevenBuyTriggers_FelfinTeachesSpellAndRockRockBuffsFirstMinionBought()
        {
            var felfin = MatchService.CreateWithDefaultCatalog(9612, new InMemoryTestScenarioRepository());
            felfin.State.Player.Tavern.Gold = 10;
            felfin.State.Player.Tavern.Hand.Clear();
            felfin.State.Player.Tavern.Shop.Clear();
            AddAndPlay(felfin, "BG33_891");
            felfin.State.Player.Tavern.Hand.Clear();
            felfin.State.Player.Tavern.Shop.Add(TavernSpell("shop-spell", "109230", 2));

            felfin.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.IsTrue(felfin.State.Player.Tavern.Hand.Any(card => card.CardId == "TAUGHT_MURLOC" && card.Tags.Contains("taught_spell:109230")));

            var rock = MatchService.CreateWithDefaultCatalog(9613, new InMemoryTestScenarioRepository());
            rock.State.Player.Tavern.Gold = 10;
            rock.State.Player.Tavern.Hand.Clear();
            rock.State.Player.Tavern.Shop.Clear();
            AddAndPlay(rock, "BG34_950");
            rock.State.Player.Tavern.Shop.Add(Card("shop-target", BoardSide.Player, "SHOP_TARGET", 2, 3, Tribe.Elemental, 2));

            rock.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            var bought = rock.State.Player.Tavern.Hand.Last(card => card.CardId == "SHOP_TARGET");
            Assert.AreEqual(24, bought.Attack);
            Assert.AreEqual(26, bought.MaxHealth);
        }

        [Test]
        public void TierSixCombatTriggers_AvengeDamageAndDeathrattleRewardsResolve()
        {
            var ruinsLordResult = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    Card("demon", BoardSide.Player, "FRIENDLY_DEMON", 2, 10, Tribe.Demon, 1),
                    Card("ruins-lord", BoardSide.Player, "BG33_154", 4, 8, Tribe.Demon, 6),
                    Card("buddy", BoardSide.Player, "BUDDY", 1, 10, Tribe.None, 1)
                },
                new[] { Card("wall", BoardSide.Opponent, "WALL", 1, 30, Tribe.None, 1) },
                9614,
                1,
                new TavernState());
            var buddy = ruinsLordResult.FinalPlayerBoard.First(minion => minion.InstanceId == "buddy");
            Assert.AreEqual(3, buddy.Attack);
            Assert.AreEqual(11, buddy.MaxHealth);

            var sporebatResult = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    Card("one", BoardSide.Player, "ONE", 1, 1, Tribe.None, 1, Keyword.Taunt),
                    Card("two", BoardSide.Player, "TWO", 1, 1, Tribe.None, 1, Keyword.Taunt),
                    Card("three", BoardSide.Player, "THREE", 1, 1, Tribe.None, 1, Keyword.Taunt),
                    Card("four", BoardSide.Player, "FOUR", 1, 1, Tribe.None, 1, Keyword.Taunt),
                    Card("sporebat", BoardSide.Player, "BG31_835", 2, 20, Tribe.Undead, 6, Keyword.Avenge)
                },
                new[] { Card("killer", BoardSide.Opponent, "KILLER", 10, 50, Tribe.None, 1) },
                9615,
                4,
                new TavernState());
            Assert.IsTrue(sporebatResult.PlayerRewards.Any(reward => reward.Type == CombatRewardType.AddRandomSameTribeMinionToHand && reward.CardId == Tribe.Undead.ToString()));

            var tavern = new TavernState { BloodGemBonusAttack = 0, BloodGemBonusHealth = 0 };
            var charlgaResult = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    Card("small-a", BoardSide.Player, "SMALL_A", 1, 1, Tribe.None, 1, Keyword.Taunt),
                    Card("small-b", BoardSide.Player, "SMALL_B", 1, 1, Tribe.None, 1, Keyword.Taunt),
                    Card("quilboar", BoardSide.Player, "QUILBOAR", 1, 10, Tribe.Quilboar, 1),
                    Card("charlga", BoardSide.Player, "BG26_157", 4, 20, Tribe.Quilboar, 6, Keyword.Avenge)
                },
                new[] { Card("big", BoardSide.Opponent, "BIG", 10, 50, Tribe.None, 1) },
                9616,
                2,
                tavern);
            var quilboar = charlgaResult.FinalPlayerBoard.First(minion => minion.InstanceId == "quilboar");
            Assert.AreEqual(3, quilboar.Attack);
            Assert.AreEqual(12, quilboar.MaxHealth);

            var trailblazerResult = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    Card("rattle", BoardSide.Player, "RATTLE", 1, 1, Tribe.None, 1, Keyword.Taunt, Keyword.Deathrattle),
                    Card("trailblazer", BoardSide.Player, "BG35_437", 2, 20, Tribe.Quilboar, 6)
                },
                new[] { Card("rattle-killer", BoardSide.Opponent, "RATTLE_KILLER", 4, 20, Tribe.None, 1) },
                9617,
                1,
                new TavernState());
            Assert.IsTrue(trailblazerResult.PlayerRewards.Any(reward => reward.Type == CombatRewardType.FriendlyDeathrattleTriggered && reward.Amount == 1));
            Assert.IsTrue(trailblazerResult.PlayerRewards.Any(reward => reward.Type == CombatRewardType.ImproveBloodGemAttack && reward.Amount == 1));
        }

        [Test]
        public void TierSixFallenSkyGolem_TracksDeathrattlesAcrossCombatRewards()
        {
            var service = MatchService.CreateWithDefaultCatalog(9618, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var golem = Card("golem", BoardSide.Player, "BG35_342", 4, 4, Tribe.Mech, 6);
            service.State.Player.Board.Add(Card("rattle", BoardSide.Player, "RATTLE", 1, 1, Tribe.None, 1, Keyword.Taunt, Keyword.Deathrattle));
            service.State.Player.Board.Add(golem);
            service.State.Opponent.Board.Add(Card("killer", BoardSide.Opponent, "KILLER", 4, 20, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 9618, SafetyLimit = 1 }));

            Assert.AreEqual(1, service.State.Player.Tavern.DeathrattlesTriggeredThisGame);
            Assert.AreEqual(8, golem.Attack);
            Assert.AreEqual(6, golem.MaxHealth);
        }

        private static void AddAndPlay(MatchService service, string cardId)
        {
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
        }

        private static MinionInstance TavernSpell(string id, string cardId, int tier)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = id,
                DefinitionId = id,
                CardId = cardId,
                Name = id,
                Cost = 1,
                TavernTier = tier,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string> { "tavern_spell" }
            };
        }

        private static MinionInstance Card(string id, BoardSide owner, string cardId, int attack, int health, Tribe tribe, int tavernTier, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = id,
                DefinitionId = id,
                CardId = cardId,
                Name = id,
                Attack = attack,
                BaseAttack = attack,
                Health = health,
                MaxHealth = health,
                BaseHealth = health,
                TavernTier = tavernTier,
                Owner = owner,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>(),
                CanAttack = true
            };
        }

        private static int StableNumber(string value)
        {
            var result = 0;
            foreach (var c in value)
            {
                result = result * 31 + c;
            }

            return result & 0x7fffffff;
        }
    }
}
