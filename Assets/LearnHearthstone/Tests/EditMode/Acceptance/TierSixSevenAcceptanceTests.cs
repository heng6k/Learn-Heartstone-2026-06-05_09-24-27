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

        private static readonly string[] CombatSkippedTaughtSpellIds =
        {
            "104560", "105665", "110401", "119599", "127503", "127642",
            "123553", "103785", "100899", "122862", "110407"
        };

        private static readonly string[] CombatAutoDiscoverSpellIds =
        {
            "100910", "105264", "105265", "105669",
            "119718", "122864", "126957", "127288"
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
                var handIndex = service.State.Player.Tavern.Hand.Count - 1;
                var command = BuildTavernSpellPlayCommand(service, handIndex, service.State.Player.Tavern.Hand[handIndex]);

                Assert.DoesNotThrow(() => service.Apply(command), spellId);
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
        public void TierSixFelfin_OnlyConsumesSuccessfulApprenticeInsertionsAndGoldenAllowsTwo()
        {
            var normal = MatchService.CreateWithDefaultCatalog(9620, new InMemoryTestScenarioRepository());
            normal.State.Player.Tavern.Gold = 20;
            normal.State.Player.Tavern.Hand.Clear();
            normal.State.Player.Tavern.Shop.Clear();
            normal.State.Player.Board.Clear();
            var normalFelfin = Card("normal-felfin", BoardSide.Player, "BG33_891", 4, 8, Tribe.Murloc, 6);
            normal.State.Player.Board.Add(normalFelfin);
            for (var index = 0; index < 9; index += 1)
            {
                normal.State.Player.Tavern.Hand.Add(Card("filler-" + index, BoardSide.Player, "FILLER_" + index, 1, 1, Tribe.None, 1));
            }

            normal.State.Player.Tavern.Shop.Add(TavernSpell("blocked-spell", "131153", 4));
            normal.State.Player.Tavern.Shop.Add(TavernSpell("first-success", "131153", 4));
            normal.State.Player.Tavern.Shop.Add(TavernSpell("over-limit", "131153", 4));

            normal.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(0, normal.State.Player.Tavern.Hand.Count(IsTaughtApprentice));
            Assert.AreEqual(0, normalFelfin.Counters.TryGetValue("felfin_uses", out var blockedUses) ? blockedUses : 0);

            normal.State.Player.Tavern.Hand.Clear();
            normal.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            normal.Apply(new GameCommand(GameCommandType.BuyMinion, 2));

            Assert.AreEqual(1, normal.State.Player.Tavern.Hand.Count(IsTaughtApprentice));
            Assert.AreEqual(1, normalFelfin.Counters["felfin_uses"]);

            var golden = MatchService.CreateWithDefaultCatalog(9621, new InMemoryTestScenarioRepository());
            golden.State.Player.Tavern.Gold = 20;
            golden.State.Player.Tavern.Hand.Clear();
            golden.State.Player.Tavern.Shop.Clear();
            golden.State.Player.Board.Clear();
            var goldenFelfin = Card("golden-felfin", BoardSide.Player, "BG33_891", 8, 16, Tribe.Murloc, 6);
            goldenFelfin.Golden = true;
            golden.State.Player.Board.Add(goldenFelfin);
            golden.State.Player.Tavern.Shop.Add(TavernSpell("golden-spell-a", "131153", 4));
            golden.State.Player.Tavern.Shop.Add(TavernSpell("golden-spell-b", "131153", 4));
            golden.State.Player.Tavern.Shop.Add(TavernSpell("golden-spell-c", "131153", 4));

            golden.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            golden.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            golden.Apply(new GameCommand(GameCommandType.BuyMinion, 2));

            Assert.AreEqual(2, golden.State.Player.Tavern.Hand.Count(IsTaughtApprentice));
            Assert.AreEqual(2, goldenFelfin.Counters["felfin_uses"]);
        }

        [Test]
        public void TierSixFelfin_ApprenticeConsumesProxyAndCastsItsLearnedSpell()
        {
            var service = MatchService.CreateWithDefaultCatalog(9622, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Board.Clear();
            var target = Card("learned-target", BoardSide.Player, "LEARNED_TARGET", 1, 1, Tribe.Murloc, 1);
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(Card("felfin", BoardSide.Player, "BG33_891", 4, 8, Tribe.Murloc, 6));
            service.State.Player.Tavern.Shop.Add(TavernSpell("learned-back-to-back", "131153", 4));

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            var apprentice = service.State.Player.Tavern.Hand.Single(IsTaughtApprentice);
            var apprenticeHandIndex = service.State.Player.Tavern.Hand.IndexOf(apprentice);

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                apprenticeHandIndex,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.InstanceId == apprentice.InstanceId));
            Assert.IsTrue(service.State.Player.Board.Any(card => card.CardId == "TAUGHT_MURLOC"));
            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(5, target.MaxHealth);
            Assert.AreEqual(1, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(1, service.State.Player.Tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(4, service.State.Player.Tavern.BackToBackAttackBonus);
            Assert.AreEqual(4, service.State.Player.Tavern.BackToBackHealthBonus);
        }

        [TestCase(false, 0, 0, 0, 0, 1)]
        [TestCase(true, 0, 0, 0, 0, 2)]
        [TestCase(false, 2, 0, 0, 0, 3)]
        [TestCase(false, 1, 1, 0, 0, 4)]
        [TestCase(false, 0, 0, 2, 0, 3)]
        [TestCase(false, 0, 0, 1, 1, 4)]
        [TestCase(true, 1, 1, 1, 1, 32)]
        public void TierSixMagicfinApprentice_MultipliesGoldenBrannAndBelindaActualCasts(
            bool goldenApprentice,
            int normalBranns,
            int goldenBranns,
            int normalBelindas,
            int goldenBelindas,
            int expectedCasts)
        {
            var service = MatchService.CreateWithDefaultCatalog(9630 + expectedCasts, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.TavernSpellBonusAttack = 1;
            service.State.Player.Tavern.TavernSpellBonusHealth = 3;
            var target = Card("multiplier-target", BoardSide.Player, "MULTIPLIER_TARGET", 1, 1, Tribe.Murloc, 1);
            service.State.Player.Board.Add(target);
            AddRepeaters(service, normalBranns, goldenBranns, normalBelindas, goldenBelindas);
            var apprentice = TaughtApprentice("multiplier-apprentice", "131153", goldenApprentice);
            service.State.Player.Tavern.Hand.Add(apprentice);

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            var triangularCasts = expectedCasts * (expectedCasts + 1) / 2;
            Assert.AreEqual(1 + triangularCasts * 5, target.Attack);
            Assert.AreEqual(1 + triangularCasts * 7, target.MaxHealth);
            Assert.AreEqual(expectedCasts, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(expectedCasts, service.State.Player.Tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(expectedCasts * 5, service.State.Player.Tavern.BackToBackAttackBonus);
            Assert.AreEqual(expectedCasts * 7, service.State.Player.Tavern.BackToBackHealthBonus);
            Assert.AreEqual(expectedCasts * 5, service.State.Player.Tavern.BackToBackBonus);
            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.InstanceId == apprentice.InstanceId));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void TierSixHeavyMetalWyrm_TriggersOneOrBothAdjacentTaughtBattlecries(bool golden, int expectedCasts)
        {
            var left = TaughtApprentice("left-apprentice", "131153");
            var right = TaughtApprentice("right-apprentice", "131153");
            var wyrm = Card("heavy-metal-wyrm", BoardSide.Player, "BG26_801", 0, 1, Tribe.Beast, 4, Keyword.Taunt, Keyword.Deathrattle);
            wyrm.Golden = golden;
            left.CanAttack = false;
            right.CanAttack = false;
            wyrm.CanAttack = false;
            var opponents = Enumerable.Range(0, 4)
                .Select(index => Card("wyrm-opponent-" + index, BoardSide.Opponent, "WALL_" + index, 10, 10, Tribe.None, 1))
                .ToList();

            var result = CombatEngine.SimulateBasicCombat(
                new[] { left, wyrm, right },
                opponents,
                9640 + expectedCasts,
                1,
                new TavernState(),
                minionCatalog: MinionCatalogLoader.LoadFromResources(),
                spellCatalog: SpellCatalogLoader.LoadFromResources(),
                isolateTavernState: true);

            Assert.AreEqual(expectedCasts, result.FinalPlayerTavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(expectedCasts, result.FinalPlayerTavern.TavernSpellsCastThisGame);
            Assert.AreEqual(expectedCasts * 4, result.FinalPlayerTavern.BackToBackAttackBonus);
            Assert.AreEqual(expectedCasts, result.Replay.Frames.Count(frame => frame.EventType == CombatEventType.CombatSpellCast));
        }

        [Test]
        public void TierSixRylakPortrait_TriggersTaughtBattlecryAtCombatStart()
        {
            var tavern = new TavernState { TrinketRylakPortraitActive = true };
            var apprentice = TaughtApprentice("portrait-apprentice", "131153");
            var wyrm = Card("portrait-wyrm", BoardSide.Player, "BG26_801", 0, 5, Tribe.Beast, 4, Keyword.Deathrattle);
            var wall = Card("portrait-wall", BoardSide.Opponent, "PORTRAIT_WALL", 0, 20, Tribe.None, 1);
            apprentice.CanAttack = false;
            wyrm.CanAttack = false;
            wall.CanAttack = false;

            var result = CombatEngine.SimulateBasicCombat(
                new[] { apprentice, wyrm },
                new[] { wall },
                9650,
                1,
                tavern,
                minionCatalog: MinionCatalogLoader.LoadFromResources(),
                spellCatalog: SpellCatalogLoader.LoadFromResources(),
                isolateTavernState: true);

            Assert.AreEqual(1, result.FinalPlayerTavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(4, result.FinalPlayerTavern.BackToBackAttackBonus);
            Assert.AreEqual(1, result.Replay.Frames.Count(frame => frame.EventType == CombatEventType.CombatSpellCast));
            Assert.IsTrue(result.FinalPlayerBoard.Any(card => card.InstanceId == wyrm.InstanceId));
        }

        [Test]
        public void TierSixCombatTaughtSpell_BrannBelindaAndEvokersPersistEveryActualCast()
        {
            var service = MatchService.CreateWithDefaultCatalog(9660, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var brann = Card("combat-brann", BoardSide.Player, "BG_LOE_077", 0, 20, Tribe.None, 5);
            var belinda = Card("combat-belinda", BoardSide.Player, "BG35_883", 0, 20, Tribe.None, 6);
            var normalEvoker = Card("normal-evoker", BoardSide.Player, "BG32_822", 0, 20, Tribe.Dragon, 6);
            var goldenEvoker = Card("golden-evoker", BoardSide.Player, "BG32_822", 0, 20, Tribe.Dragon, 6);
            goldenEvoker.Golden = true;
            var apprentice = TaughtApprentice("combat-apprentice", "131153");
            var wyrm = Card("combat-wyrm", BoardSide.Player, "BG26_801", 0, 1, Tribe.Beast, 4, Keyword.Taunt, Keyword.Deathrattle);
            foreach (var minion in new[] { brann, belinda, normalEvoker, goldenEvoker, apprentice, wyrm })
            {
                minion.CanAttack = false;
                service.State.Player.Board.Add(minion);
            }

            for (var index = 0; index < 7; index += 1)
            {
                service.State.Opponent.Board.Add(Card("combat-opponent-" + index, BoardSide.Opponent, "COMBAT_WALL_" + index, 10, 10, Tribe.None, 1));
            }

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 9660, SafetyLimit = 1 }));

            Assert.AreEqual(4, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(4, service.State.Player.Tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(16, service.State.Player.Tavern.BackToBackAttackBonus);
            Assert.AreEqual(16, service.State.Player.Tavern.BackToBackHealthBonus);
            Assert.AreEqual(8, normalEvoker.Counters["dragon_spell_attack"]);
            Assert.AreEqual(4, normalEvoker.Counters["dragon_spell_health"]);
            Assert.AreEqual(16, goldenEvoker.Counters["dragon_spell_attack"]);
            Assert.AreEqual(8, goldenEvoker.Counters["dragon_spell_health"]);
            Assert.AreEqual(4, service.State.LastResult.Replay.Frames.Count(frame => frame.EventType == CombatEventType.CombatSpellCast));
            Assert.AreEqual(8, service.State.LastResult.PlayerRewards
                .Where(reward => reward.Type == CombatRewardType.ImproveFireforgedEvoker && reward.TargetInstanceId == normalEvoker.InstanceId)
                .Sum(reward => reward.Attack));
            Assert.AreEqual(16, service.State.LastResult.PlayerRewards
                .Where(reward => reward.Type == CombatRewardType.ImproveFireforgedEvoker && reward.TargetInstanceId == goldenEvoker.InstanceId)
                .Sum(reward => reward.Attack));

            service.State.Opponent.Board.Clear();
            var passiveWall = Card("passive-wall", BoardSide.Opponent, "PASSIVE_WALL", 0, 20, Tribe.None, 1);
            passiveWall.CanAttack = false;
            service.State.Opponent.Board.Add(passiveWall);
            var nextDragon = Card("next-combat-dragon", BoardSide.Player, "NEXT_DRAGON", 1, 1, Tribe.Dragon, 1);
            nextDragon.CanAttack = false;
            service.State.Player.Board.Add(nextDragon);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 9661, SafetyLimit = 1 }));

            var combatDragon = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == nextDragon.InstanceId);
            Assert.AreEqual(31, combatDragon.Attack);
            Assert.AreEqual(16, combatDragon.MaxHealth);
        }

        [TestCaseSource(nameof(CombatSkippedTaughtSpellIds))]
        public void TierSixCombatTaughtSpell_UnsafeSpellsSkipBeforeTargetsAndCastAccounting(string spellCardId)
        {
            var tavern = new TavernState
            {
                TrinketRylakPortraitActive = true,
                TavernSpellsCastThisTurn = 5,
                TavernSpellsCastThisGame = 8,
                LastTavernSpellCardId = "baseline-spell",
                BackToBackAttackBonus = 7,
                BackToBackHealthBonus = 9,
                TemporaryAvengeBeastRewards = 2
            };
            var shop = new[]
            {
                Card("skip-shop-a", BoardSide.Player, "SKIP_SHOP_A", 2, 3, Tribe.Demon, 1),
                Card("skip-shop-b", BoardSide.Player, "SKIP_SHOP_B", 4, 5, Tribe.Murloc, 2),
                Card("skip-shop-c", BoardSide.Player, "SKIP_SHOP_C", 6, 7, Tribe.Elemental, 3)
            };
            foreach (var card in shop)
            {
                card.PoolCopiesHeld = 1;
                tavern.Shop.Add(card);
                tavern.Pool[card.DefinitionId] = 0;
                tavern.PoolCapacities[card.DefinitionId] = 10;
            }

            var apprentice = TaughtApprentice("skip-apprentice", spellCardId, true);
            var wyrm = Card("skip-wyrm", BoardSide.Player, "BG26_801", 0, 20, Tribe.Beast, 4, Keyword.Deathrattle);
            var brann = Card("skip-brann", BoardSide.Player, "BG_LOE_077", 0, 20, Tribe.None, 5);
            brann.Golden = true;
            var belinda = Card("skip-belinda", BoardSide.Player, "BG35_883", 0, 20, Tribe.None, 6);
            belinda.Golden = true;
            var demon = Card("skip-demon", BoardSide.Player, "SKIP_DEMON", 3, 11, Tribe.Demon, 1);
            var evoker = Card("skip-evoker", BoardSide.Player, "BG32_822", 0, 20, Tribe.Dragon, 6);
            var board = new[] { apprentice, wyrm, brann, belinda, demon, evoker };
            foreach (var minion in board)
            {
                minion.CanAttack = false;
            }

            var wall = Card("skip-wall", BoardSide.Opponent, "SKIP_WALL", 0, 20, Tribe.None, 1);
            wall.CanAttack = false;

            var result = CombatEngine.SimulateBasicCombat(
                board,
                new[] { wall },
                9665 + StableNumber(spellCardId),
                1,
                tavern,
                minionCatalog: MinionCatalogLoader.LoadFromResources(),
                spellCatalog: SpellCatalogLoader.LoadFromResources(),
                isolateTavernState: true);

            Assert.AreEqual(5, result.FinalPlayerTavern.TavernSpellsCastThisTurn, spellCardId);
            Assert.AreEqual(8, result.FinalPlayerTavern.TavernSpellsCastThisGame, spellCardId);
            Assert.AreEqual("baseline-spell", result.FinalPlayerTavern.LastTavernSpellCardId, spellCardId);
            Assert.AreEqual(7, result.FinalPlayerTavern.BackToBackAttackBonus, spellCardId);
            Assert.AreEqual(9, result.FinalPlayerTavern.BackToBackHealthBonus, spellCardId);
            Assert.AreEqual(2, result.FinalPlayerTavern.TemporaryAvengeBeastRewards, spellCardId);
            Assert.AreEqual(0, result.FinalPlayerTavern.NextCombatTavernSpellCardIds.Count, spellCardId);
            Assert.AreEqual(0, result.FinalPlayerTavern.NextCombatBoardAttack, spellCardId);
            Assert.AreEqual(0, result.FinalPlayerTavern.NextCombatBoardHealth, spellCardId);
            Assert.AreEqual(0, result.FinalPlayerTavern.NextCombatBeetles, spellCardId);
            Assert.AreEqual(0, result.FinalPlayerTavern.NextCombatEnemyHealthToOne, spellCardId);
            Assert.IsFalse(result.FinalPlayerTavern.NextCombatLeftmostCopiesNearestEnemyStats, spellCardId);
            Assert.IsFalse(result.FinalPlayerTavern.NextCombatLeftmostDoubleAttack, spellCardId);
            Assert.IsFalse(result.FinalPlayerTavern.NextCombatTriggerMixedMechanics, spellCardId);
            CollectionAssert.AreEquivalent(board.Select(card => card.InstanceId), result.FinalPlayerBoard.Select(card => card.InstanceId), spellCardId);
            Assert.AreEqual(3, result.FinalPlayerBoard.Single(card => card.InstanceId == demon.InstanceId).Attack, spellCardId);
            Assert.AreEqual(11, result.FinalPlayerBoard.Single(card => card.InstanceId == demon.InstanceId).MaxHealth, spellCardId);
            CollectionAssert.AreEquivalent(shop.Select(card => card.InstanceId), result.FinalPlayerTavern.Shop.Where(card => card != null).Select(card => card.InstanceId), spellCardId);
            Assert.IsTrue(result.FinalPlayerTavern.Pool.All(pair => pair.Value == 0), spellCardId);
            var finalEvoker = result.FinalPlayerBoard.Single(card => card.InstanceId == evoker.InstanceId);
            Assert.IsFalse(finalEvoker.Counters.ContainsKey("dragon_spell_attack"), spellCardId);
            Assert.AreEqual(0, result.Replay.Frames.Count(frame => frame.EventType == CombatEventType.CombatSpellCast), spellCardId);
        }

        [Test]
        public void TierSixCombatTaughtSpell_OverconfidenceUsesTheCurrentCombatOutcomeAndStacksActualCasts()
        {
            var service = CreateCombatTaughtSpellService("105267", 9666, true, true);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 9666, SafetyLimit = 1 }));

            Assert.AreEqual(CombatWinner.Player, service.State.LastResult.Winner);
            Assert.AreEqual(4, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(4, service.State.Player.Tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(12, service.State.Player.Tavern.NextTurnBonusGold);
            Assert.AreEqual(0, service.State.Player.Tavern.PendingCombatWinGold);
            Assert.AreEqual(0, service.State.Player.Tavern.PendingCombatDrawGold);
            Assert.AreEqual(4, service.State.LastResult.Replay.Frames.Count(frame => frame.EventType == CombatEventType.CombatSpellCast));
        }

        [TestCaseSource(nameof(CombatAutoDiscoverSpellIds))]
        public void TierSixCombatTaughtSpell_DiscoversChooseSeededOptionsWithoutLeavingPlayerInput(string spellCardId)
        {
            var service = CreateCombatTaughtSpellService(spellCardId, 9667 + StableNumber(spellCardId));
            var startingHeroPower = service.State.Player.HeroPowerCardId;

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions
            {
                Seed = 9667 + StableNumber(spellCardId),
                SafetyLimit = 1
            }));

            Assert.IsNull(service.State.Player.Tavern.Discover, spellCardId);
            Assert.AreEqual(0, service.State.Player.Tavern.DiscoverQueue.Count, spellCardId);
            Assert.AreEqual(1, service.State.Player.Tavern.TavernSpellsCastThisTurn, spellCardId);
            Assert.AreEqual(1, service.State.LastResult.Replay.Frames.Count(frame => frame.EventType == CombatEventType.CombatSpellCast), spellCardId);
            if (spellCardId == "100910")
            {
                Assert.IsNotEmpty(service.State.Player.HeroPowerCardId, spellCardId);
                Assert.AreNotEqual(startingHeroPower, service.State.Player.HeroPowerCardId, spellCardId);
            }
            else
            {
                Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count, spellCardId);
            }
        }

        [Test]
        public void TierSixCombatTaughtSpell_GoldenApprenticeAndBrannAutoResolveEveryDiscover()
        {
            var service = CreateCombatTaughtSpellService("122864", 9668, true, true);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 9668, SafetyLimit = 1 }));

            Assert.IsNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(0, service.State.Player.Tavern.DiscoverQueue.Count);
            Assert.AreEqual(4, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.Minion && card.TavernTier == 1));
            Assert.AreEqual(4, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(4, service.State.LastResult.Replay.Frames.Count(frame => frame.EventType == CombatEventType.CombatSpellCast));
        }

        [TestCase(9600, true)]
        [TestCase(9605, false)]
        public void TierSixCombatTaughtSpell_TimeManagementRandomlyUsesImmediateOrDelayedChoice(int seed, bool expectedDelayed)
        {
            var tavern = new TavernState
            {
                TrinketRylakPortraitActive = true,
                TavernSpellBonusAttack = 1,
                TavernSpellBonusHealth = 3
            };
            var apprentice = TaughtApprentice("time-management-apprentice", "117573");
            var wyrm = Card("time-management-wyrm", BoardSide.Player, "BG26_801", 0, 20, Tribe.Beast, 4, Keyword.Deathrattle);
            var target = Card("time-management-target", BoardSide.Player, "TIME_MANAGEMENT_TARGET", 3, 7, Tribe.Murloc, 1);
            var wall = Card("time-management-wall", BoardSide.Opponent, "TIME_MANAGEMENT_WALL", 0, 20, Tribe.None, 1);
            foreach (var minion in new[] { apprentice, wyrm, target, wall })
            {
                minion.CanAttack = false;
            }

            var result = CombatEngine.SimulateBasicCombat(
                new[] { apprentice, wyrm, target },
                new[] { wall },
                seed,
                1,
                tavern,
                minionCatalog: MinionCatalogLoader.LoadFromResources(),
                spellCatalog: SpellCatalogLoader.LoadFromResources(),
                round: 0,
                isolateTavernState: true);
            var finalTarget = result.FinalPlayerBoard.Single(card => card.InstanceId == target.InstanceId);

            Assert.AreEqual(expectedDelayed ? 2 : 0, result.FinalPlayerTavern.PendingTimeManagementEnchantments.Count);
            Assert.AreEqual(expectedDelayed ? 3 : 6, finalTarget.Attack);
            Assert.AreEqual(expectedDelayed ? 7 : 12, finalTarget.MaxHealth);
            if (expectedDelayed)
            {
                Assert.IsTrue(result.FinalPlayerTavern.PendingTimeManagementEnchantments.All(enchantment =>
                    enchantment.AttackBonus == 3 && enchantment.HealthBonus == 5));
            }

            Assert.AreEqual(1, result.FinalPlayerTavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(1, result.Replay.Frames.Count(frame => frame.EventType == CombatEventType.CombatSpellCast));
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

        private static GameCommand BuildTavernSpellPlayCommand(MatchService service, int handIndex, MinionInstance spell)
        {
            var boardTarget = service.State.Player.Board
                .Select((card, index) => new { Card = card, Index = index })
                .FirstOrDefault(item => item.Card != null && TavernSpellEngine.IsLegalFriendlyMinionTarget(spell, item.Card));
            if (boardTarget != null && TavernSpellEngine.TargetsFriendlyMinion(spell))
            {
                return new GameCommand(
                    GameCommandType.PlayMinion,
                    handIndex,
                    boardTarget.Index,
                    TargetZone.FriendlyBoard,
                    -1,
                    TargetZone.Unspecified,
                    boardTarget.Card.InstanceId);
            }

            var shopTarget = service.State.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .FirstOrDefault(item => item.Card != null && TavernSpellEngine.IsLegalFriendlyMinionTarget(spell, item.Card));
            if (shopTarget != null && TavernSpellEngine.CanTargetTavernMinion(spell))
            {
                return new GameCommand(
                    GameCommandType.PlayMinion,
                    handIndex,
                    shopTarget.Index,
                    TargetZone.TavernShop,
                    -1,
                    TargetZone.Unspecified,
                    shopTarget.Card.InstanceId);
            }

            return new GameCommand(GameCommandType.PlayMinion, handIndex);
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

        private static bool IsTaughtApprentice(MinionInstance card)
        {
            return card != null &&
                   card.CardId == "TAUGHT_MURLOC" &&
                   card.Tags != null &&
                   card.Tags.Any(tag => tag.StartsWith("taught_spell:"));
        }

        private static MinionInstance TaughtApprentice(string id, string spellCardId, bool golden = false)
        {
            var apprentice = Card(id, BoardSide.Player, "TAUGHT_MURLOC", 1, 1, Tribe.Murloc, 1, Keyword.Battlecry);
            apprentice.DefinitionId = "BG33_890t";
            apprentice.Golden = golden;
            apprentice.Tags.Add("generated_minion");
            apprentice.Tags.Add("taught_spell:" + spellCardId);
            return apprentice;
        }

        private static MatchService CreateCombatTaughtSpellService(
            string spellCardId,
            int seed,
            bool goldenApprentice = false,
            bool includeBrann = false)
        {
            var service = MatchService.CreateWithDefaultCatalog(seed, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 6;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId = "BG35_MagicItem_834";
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(TaughtApprentice("combat-choice-apprentice", spellCardId, goldenApprentice));
            service.State.Player.Board.Add(Card("combat-choice-wyrm", BoardSide.Player, "BG26_801", 0, 20, Tribe.Beast, 4, Keyword.Deathrattle));
            service.State.Player.Board.Add(Card("combat-choice-murloc", BoardSide.Player, "COMBAT_CHOICE_MURLOC", 2, 20, Tribe.Murloc, 1));
            if (includeBrann)
            {
                service.State.Player.Board.Add(Card("combat-choice-brann", BoardSide.Player, "BG_LOE_077", 0, 20, Tribe.None, 5));
            }

            service.State.Opponent.Board.Add(Card("combat-choice-wall", BoardSide.Opponent, "COMBAT_CHOICE_WALL", 0, 20, Tribe.None, 1));
            foreach (var minion in service.State.Player.Board.Concat(service.State.Opponent.Board))
            {
                minion.CanAttack = false;
            }

            return service;
        }

        private static void AddRepeaters(
            MatchService service,
            int normalBranns,
            int goldenBranns,
            int normalBelindas,
            int goldenBelindas)
        {
            for (var index = 0; index < normalBranns; index += 1)
            {
                service.State.Player.Board.Add(Card("normal-brann-" + index, BoardSide.Player, "BG_LOE_077", 0, 20, Tribe.None, 5));
            }

            for (var index = 0; index < goldenBranns; index += 1)
            {
                var brann = Card("golden-brann-" + index, BoardSide.Player, "BG_LOE_077", 0, 20, Tribe.None, 5);
                brann.Golden = true;
                service.State.Player.Board.Add(brann);
            }

            for (var index = 0; index < normalBelindas; index += 1)
            {
                service.State.Player.Board.Add(Card("normal-belinda-" + index, BoardSide.Player, "BG35_883", 0, 20, Tribe.None, 6));
            }

            for (var index = 0; index < goldenBelindas; index += 1)
            {
                var belinda = Card("golden-belinda-" + index, BoardSide.Player, "BG35_883", 0, 20, Tribe.None, 6);
                belinda.Golden = true;
                service.State.Player.Board.Add(belinda);
            }
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
