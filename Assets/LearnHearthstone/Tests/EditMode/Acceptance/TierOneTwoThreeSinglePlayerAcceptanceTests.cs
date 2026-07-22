using System;
using System.Collections.Generic;
using System.IO;
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
    public sealed class TierOneTwoThreeSinglePlayerAcceptanceTests
    {
        private static readonly HashSet<string> ImplementedTierOneTwoThreeTavernSpells = new HashSet<string>
        {
            "100596",
            "103779",
            "103791",
            "103793",
            "104029",
            "104436",
            "104446",
            "104502",
            "104559",
            "105267",
            "105664",
            "105665",
            "105667",
            "105669",
            "105752",
            "105903",
            "109230",
            "113901",
            "117573",
            "122182",
            "122183",
            "122184",
            "122185",
            "122186",
            "122489",
            "122862",
            "122864",
            "126676",
            "127288",
            "131152"
        };

        [Test]
        public void ProjectScope_DuosCardsRemainOutOfScopeForSinglePlayerTavern()
        {
            Assert.IsTrue(File.Exists("PROJECT_SCOPE.md"));
            Assert.IsTrue(TierThreeMinionImplementationRegistry.All
                .Where(entry => entry.CardId.StartsWith("BGDUO", StringComparison.Ordinal))
                .All(entry => entry.Status == TierThreeImplementationStatus.OutOfScope));
        }

        [Test]
        public void TavernSpells_TierOneTwoThreeAllHaveSinglePlayerImplementationEntries()
        {
            var catalog = SpellCatalogLoader.LoadFromResources();
            var tierOneToThree = catalog.All
                .Where(spell => spell.InPool && spell.Category == "TavernSpell" && spell.TavernTier >= 1 && spell.TavernTier <= 3)
                .Select(spell => spell.CardNumber)
                .OrderBy(id => id)
                .ToList();

            Assert.AreEqual(30, tierOneToThree.Count);
            Assert.AreEqual(tierOneToThree, ImplementedTierOneTwoThreeTavernSpells.OrderBy(id => id).ToList());
        }

        [Test]
        public void TavernSpells_TierOneTwoThreeAllResolveInPreparedSinglePlayerState()
        {
            var catalog = SpellCatalogLoader.LoadFromResources();
            var spells = catalog.All
                .Where(spell => spell.InPool && spell.Category == "TavernSpell" && spell.TavernTier >= 1 && spell.TavernTier <= 3)
                .OrderBy(spell => spell.CardNumber)
                .ToList();

            foreach (var spell in spells)
            {
                var service = PreparedService(9300 + spell.SourceId);
                service.State.Player.Tavern.Tier = Math.Max(1, spell.TavernTier);
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, spell.CardNumber, CardKind.TavernSpell));

                Assert.DoesNotThrow(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1)), spell.CardNumber + " should resolve.");
            }
        }

        [Test]
        public void Minions_TierOneTwoThreeSinglePlayerCardsCanBePlayedAndDuosAreOutOfScope()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            var minions = catalog.All
                .Where(minion => minion.InPool && minion.TavernTier >= 1 && minion.TavernTier <= 3)
                .OrderBy(minion => minion.TavernTier)
                .ThenBy(minion => minion.CardId)
                .ToList();

            Assert.AreEqual(106, minions.Count);
            foreach (var minion in minions)
            {
                if (minion.CardId.StartsWith("BGDUO", StringComparison.Ordinal))
                {
                    if (TierThreeMinionImplementationRegistry.Contains(minion.CardId))
                    {
                        Assert.AreEqual(TierThreeImplementationStatus.OutOfScope, TierThreeMinionImplementationRegistry.Get(minion.CardId).Status);
                    }

                    continue;
                }

                var service = PreparedService(9400 + minion.DbfId);
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, minion.CardId, CardKind.Minion));
                Assert.DoesNotThrow(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1)), minion.CardId + " should be playable.");
            }
        }

        [Test]
        public void MinionCatalog_OfficialSoloPoolBackfillMatchesCurrentTierDistribution()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            var byId = catalog.All.ToDictionary(minion => minion.CardId);

            Assert.IsTrue(byId["BG31_803"].InPool, "Buzzing Vermin should be in the current official solo pool.");
            Assert.IsTrue(byId["BG25_013"].InPool, "Rot Hide Gnoll should be in the current official solo pool.");
            Assert.IsTrue(byId["BG26_529"].InPool, "Upbeat Frontdrake should be in the current official solo pool.");
            Assert.IsFalse(byId["BG26_800"].InPool, "Manasaber is retained as legacy data but not in the current official solo pool.");
            Assert.IsFalse(byId["BG33_809"].InPool, "Holy Mecherel is retained as legacy data but not in the current official solo pool.");
            Assert.IsFalse(byId["BG31_920"].InPool, "Darkcrest Strategist is retained as legacy data but not in the current official solo pool.");
        }

        [Test]
        public void TierOneOfficialBackfill_UpbeatFrontdrakeRewardsDragonEveryThirdEndTurn()
        {
            var service = PreparedService(9530);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Tier = 1;

            AddAndPlay(service, "BG26_529");
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.Tribes.Contains(Tribe.Dragon)));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.Tribes.Contains(Tribe.Dragon)));
        }

        [Test]
        public void TierOneOfficialBackfill_CombatAurasAndDeathrattlesResolve()
        {
            var result = CombatEngine.SimulateBasicCombat(
                new[]
                {
                    TestMinion("vermin", BoardSide.Player, "BG31_803", 1, 1, Tribe.Beast, Keyword.Taunt, Keyword.Deathrattle),
                    TestMinion("gnoll", BoardSide.Player, "BG25_013", 1, 4, Tribe.Undead)
                },
                new[] { TestMinion("wall", BoardSide.Opponent, "WALL", 10, 10, Tribe.None) },
                9531,
                1);

            Assert.IsTrue(result.FinalPlayerBoard.Any(minion => minion.CardId == "BEETLE" && minion.Attack == 2 && minion.MaxHealth == 2));
            var gnoll = result.FinalPlayerBoard.Single(minion => minion.InstanceId == "gnoll");
            Assert.AreEqual(2, gnoll.Attack);
        }

        [Test]
        public void TierThreeNewlyClosedSpellGaps_HaveObservableSinglePlayerEffects()
        {
            var service = PreparedService(9501);
            PlaySpell(service, "103779");
            Assert.AreEqual(2, service.State.Player.Tavern.NextTurnBonusGold);

            service = PreparedService(9502);
            PlaySpell(service, "105665");
            Assert.AreEqual(2, service.State.Player.Tavern.NextCombatBoardAttack);
            Assert.AreEqual(1, service.State.Player.Tavern.NextCombatBoardHealth);

            service = PreparedService(9503);
            PlaySpell(service, "105669");
            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(3, service.State.Player.Tavern.Discover.Options.Count);

            service = PreparedService(9504);
            var originalCardId = service.State.Player.Board[0].CardId;
            var originalAttack = service.State.Player.Board[0].Attack;
            PlaySpell(service, "113901");
            Assert.AreNotEqual(originalCardId, service.State.Player.Board[0].CardId);
            Assert.AreEqual(originalAttack, service.State.Player.Board[0].Attack);

            service = PreparedService(9505);
            PlaySpell(service, "122489");
            Assert.IsTrue(service.State.Player.Board.All(minion => minion.Tags.Contains("temporary_spellcraft")));

            service = PreparedService(9506);
            var elemental = service.State.Player.Board.First(minion => minion.Tribes.Contains(Tribe.Elemental));
            var before = elemental.Attack;
            PlaySpell(service, "122862");
            Assert.Less(service.State.Player.Board.Count, 3);
            Assert.Greater(elemental.Attack, before);
        }

        [Test]
        public void GeneratedBounties_AreSinglePlayerTierThreeTavernSpells()
        {
            var bountyIds = new[] { "122182", "122183", "122184", "122185", "122186" };
            for (var index = 0; index < bountyIds.Length; index += 1)
            {
                var bountyId = bountyIds[index];
                var service = PreparedService(9510 + index);
                service.State.Player.Tavern.Gold = 0;
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, bountyId, CardKind.TavernSpell));

                var bounty = service.State.Player.Tavern.Hand[0];
                Assert.AreEqual(CardKind.TavernSpell, bounty.CardKind);
                Assert.AreEqual(3, bounty.TavernTier);
                Assert.IsTrue(bounty.Tags.Contains("bounty"));
                Assert.DoesNotThrow(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, 0)), bountyId + " should resolve.");
            }
        }

        [Test]
        public void BoardTribeDistribution_DrivesMajorityTribeDiscoverAndBounty()
        {
            var service = PreparedService(9520);
            SetMajorityDragonBoard(service);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "105669", CardKind.TavernSpell));

            Assert.AreEqual(2, service.State.Player.BoardTribeDistribution[Tribe.Dragon]);
            Assert.AreEqual(1, service.State.Player.BoardTribeDistribution[Tribe.Murloc]);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(option => option.Tribes.Contains(Tribe.Dragon) || option.Tribes.Contains(Tribe.All)));

            service = PreparedService(9521);
            SetMajorityDragonBoard(service);
            PlaySpell(service, "122185");

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Minion && (card.Tribes.Contains(Tribe.Dragon) || card.Tribes.Contains(Tribe.All))));
        }

        [Test]
        public void ContingentCombatGoldSpell_PaysOutOnNextTurnAfterWin()
        {
            var service = PreparedService(9507);
            service.State.Opponent.Board.Clear();
            service.State.Opponent.Board.Add(TestMinion("o-weak", BoardSide.Opponent, 1, 1, Tribe.None));
            PlaySpell(service, "105267");

            service.Apply(new GameCommand(GameCommandType.SimulateCombat));

            Assert.AreEqual(2, service.State.Round);
            Assert.AreEqual(0, service.State.Player.Tavern.NextTurnBonusGold);
            Assert.AreEqual(7, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void TierTwoSurfingSylvar_NormalAndGoldenUseOfficialEndTurnRepeats()
        {
            var normal = PreparedService(9532);
            normal.State.Player.Board.Clear();
            normal.State.Player.Board.Add(TestMinion("normal-left", BoardSide.Player, 1, 5, Tribe.None));
            normal.State.Player.Board.Add(TestMinion("normal-sylvar", BoardSide.Player, "BG32_235", 1, 5, Tribe.Elemental));
            normal.State.Player.Board.Add(TestMinion("normal-right", BoardSide.Player, 1, 5, Tribe.None));

            normal.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, normal.State.Player.Board[0].Attack);
            Assert.AreEqual(2, normal.State.Player.Board[2].Attack);

            var golden = PreparedService(9533);
            golden.State.Player.Board.Clear();
            golden.State.Player.Board.Add(TestMinion("golden-left", BoardSide.Player, 1, 5, Tribe.None));
            var sylvar = TestMinion("golden-sylvar", BoardSide.Player, "BG32_235", 2, 10, Tribe.Elemental);
            sylvar.Golden = true;
            golden.State.Player.Board.Add(sylvar);
            golden.State.Player.Board.Add(TestMinion("golden-right", BoardSide.Player, 1, 5, Tribe.None));

            golden.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(5, golden.State.Player.Board[0].Attack);
            Assert.AreEqual(5, golden.State.Player.Board[2].Attack);
        }

        [Test]
        public void TierTwoDefiantShipwright_GainsHealthWhenOtherSourcesGrantAttack()
        {
            var normal = PreparedService(9534);
            normal.State.Player.Board.Clear();
            normal.State.Player.Board.Add(TestMinion("normal-shipwright", BoardSide.Player, "BG21_018", 1, 1, Tribe.Pirate));
            PlaySpell(normal, "109230");

            Assert.AreEqual(2, normal.State.Player.Board[0].Attack);
            Assert.AreEqual(3, normal.State.Player.Board[0].MaxHealth);

            var golden = PreparedService(9535);
            golden.State.Player.Board.Clear();
            var shipwright = TestMinion("golden-shipwright", BoardSide.Player, "BG21_018", 2, 2, Tribe.Pirate);
            shipwright.Golden = true;
            golden.State.Player.Board.Add(shipwright);
            PlaySpell(golden, "109230");

            Assert.AreEqual(3, golden.State.Player.Board[0].Attack);
            Assert.AreEqual(5, golden.State.Player.Board[0].MaxHealth);
        }

        [Test]
        public void TierFiveHotAirSurveyor_WeightsNormalAndGoldenCopiesForHandPlayedBloodGems()
        {
            var service = PreparedService(9536);
            service.State.Player.Board.Clear();
            var target = TestMinion("surveyor-target", BoardSide.Player, 1, 1, Tribe.Quilboar);
            var normal = TestMinion("normal-surveyor", BoardSide.Player, "BG30_121", 1, 1, Tribe.Pirate);
            var golden = TestMinion("golden-surveyor", BoardSide.Player, "BG30_121", 2, 2, Tribe.Pirate);
            golden.Golden = true;
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(normal);
            service.State.Player.Board.Add(golden);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BLOOD_GEM", CardKind.Spell));

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                service.State.Player.Tavern.Hand.Count - 1,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified));

            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(5, target.MaxHealth);
        }

        [Test]
        public void GoldenDuneDwellerAndFreedealingGambler_UseOfficialBattlecryAndSellValues()
        {
            var dune = PreparedService(9537);
            dune.State.Player.Board.Clear();
            dune.State.Player.Tavern.Shop.Clear();
            var elemental = TestMinion("dune-elemental", BoardSide.Player, 1, 1, Tribe.Elemental);
            dune.State.Player.Tavern.Shop.Add(elemental);
            dune.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG31_815", CardKind.Minion));
            dune.State.Player.Tavern.Hand[0].Golden = true;

            dune.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(3, elemental.Attack);
            Assert.AreEqual(3, elemental.MaxHealth);

            var gamblerService = PreparedService(9538);
            gamblerService.State.Player.Board.Clear();
            gamblerService.State.Player.Tavern.Gold = 0;
            var gambler = TestMinion("golden-gambler", BoardSide.Player, "BGS_049", 6, 6, Tribe.Pirate);
            gambler.Golden = true;
            gamblerService.State.Player.Board.Add(gambler);

            gamblerService.Apply(new GameCommand(GameCommandType.SellMinion, gambler.InstanceId));

            Assert.AreEqual(6, gamblerService.State.Player.Tavern.Gold);
        }

        private static MatchService PreparedService(int seed)
        {
            var service = MatchService.CreateWithDefaultCatalog(seed, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;
            service.State.Player.Tavern.Tier = 3;
            service.State.Player.Board.Add(TestMinion("p-elemental", BoardSide.Player, 3, 3, Tribe.Elemental));
            service.State.Player.Board.Add(TestMinion("p-dragon", BoardSide.Player, 4, 4, Tribe.Dragon));
            service.State.Player.Board.Add(TestMinion("p-pirate", BoardSide.Player, 2, 5, Tribe.Pirate));
            return service;
        }

        private static void SetMajorityDragonBoard(MatchService service)
        {
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestMinion("p-dragon-a", BoardSide.Player, 4, 4, Tribe.Dragon));
            service.State.Player.Board.Add(TestMinion("p-dragon-b", BoardSide.Player, 5, 5, Tribe.Dragon));
            service.State.Player.Board.Add(TestMinion("p-murloc", BoardSide.Player, 2, 3, Tribe.Murloc));
        }

        private static void PlaySpell(MatchService service, string cardNumber)
        {
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardNumber, CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
        }

        private static void AddAndPlay(MatchService service, string cardId)
        {
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
        }

        private static MinionInstance TestMinion(string id, BoardSide owner, int attack, int health, Tribe tribe)
        {
            return TestMinion(id, owner, id.ToUpperInvariant(), attack, health, tribe);
        }

        private static MinionInstance TestMinion(string id, BoardSide owner, string cardId, int attack, int health, Tribe tribe, params Keyword[] keywords)
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
                TavernTier = 1,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>(),
                CanAttack = true
            };
        }
    }
}
