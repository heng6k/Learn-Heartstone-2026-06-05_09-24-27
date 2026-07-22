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
    public sealed class TierFourAcceptanceTests
    {
        private static readonly string[] TierFourSpellIds =
        {
            "104445", "104472", "105271", "105276", "110400", "110406",
            "110401", "110642", "117670", "120900", "123553", "126909",
            "126957", "130310", "130311", "130312", "131153", "131218"
        };

        [Test]
        public void TierFourCatalog_CountsAndDuosScopeAreStable()
        {
            var minions = MinionCatalogLoader.LoadFromResources().All.Where(minion => minion.InPool && minion.TavernTier == 4).ToList();
            var spells = SpellCatalogLoader.LoadFromResources().All.Where(spell => spell.InPool && spell.Category == "TavernSpell" && spell.TavernTier == 4).ToList();

            Assert.AreEqual(60, minions.Count);
            Assert.AreEqual(53, minions.Count(minion => !minion.CardId.StartsWith("BGDUO")));
            Assert.AreEqual(7, minions.Count(minion => minion.CardId.StartsWith("BGDUO")));
            Assert.AreEqual(18, spells.Count);
            Assert.AreEqual(TierFourSpellIds.OrderBy(id => id).ToList(), spells.Select(spell => spell.CardNumber).OrderBy(id => id).ToList());
        }

        [Test]
        public void TierFourRegistry_CoversEveryTierFourMinionAndKeepsDuosOutOfScope()
        {
            var tierFourIds = MinionCatalogLoader.LoadFromResources().All
                .Where(minion => minion.InPool && minion.TavernTier == 4)
                .Select(minion => minion.CardId)
                .OrderBy(id => id)
                .ToList();
            var registeredIds = TierFourMinionImplementationRegistry.All.Select(entry => entry.CardId).OrderBy(id => id).ToList();

            Assert.AreEqual(tierFourIds, registeredIds);
            Assert.AreEqual(60, registeredIds.Distinct().Count());
            Assert.IsTrue(TierFourMinionImplementationRegistry.All.All(entry => !string.IsNullOrWhiteSpace(entry.Area)));
            Assert.IsFalse(TierFourMinionImplementationRegistry.All.Any(entry => entry.Status == TierFourImplementationStatus.SoloApproximation));
            Assert.IsFalse(TierFourMinionImplementationRegistry.All.Any(entry => entry.Status == TierFourImplementationStatus.KeywordOnly));
            Assert.IsTrue(TierFourMinionImplementationRegistry.All.Where(entry => entry.CardId.StartsWith("BGDUO")).All(entry => entry.Status == TierFourImplementationStatus.OutOfScope));
        }

        [Test]
        public void TierFourTavernSpells_AllResolveWithoutFallingThroughDefault()
        {
            foreach (var spellId in TierFourSpellIds)
            {
                var service = MatchService.CreateWithDefaultCatalog(9400 + spellId.GetHashCode(), new InMemoryTestScenarioRepository());
                service.State.Player.Tavern.Tier = 4;
                service.State.Player.Tavern.Gold = 10;
                service.State.Player.Tavern.Hand.Clear();
                service.State.Player.Board.Clear();
                AddAndPlay(service, "BGS_116");
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, spellId, CardKind.TavernSpell));

                Assert.DoesNotThrow(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1)), spellId);
                Assert.IsFalse(service.State.Player.Tavern.RecruitLog.Last().Message.Contains("暂未实现"), spellId);
            }
        }

        [Test]
        public void TierFourGeneratedPools_AddTaggedGeneratedCards()
        {
            var service = MatchService.CreateWithDefaultCatalog(9410, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            AddAndPlay(service, "BG35_143");
            AddAndPlay(service, "BG35_881");
            AddAndPlay(service, "BG35_433");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "131218" && card.Tags.Contains("generated_spell")));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "130311" && card.Tags.Contains("generated_spell")));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "REBORN_BLOOD_GEM" && card.Tags.Contains("quilboar_reborn_grant")));
        }

        [Test]
        public void TierFourPricklyPiper_DamagesAfterChoosingDiscoveredDemon()
        {
            var service = MatchService.CreateWithDefaultCatalog(9411, new InMemoryTestScenarioRepository());
            AddAndPlay(service, "BG26_525");
            var picked = service.State.Player.Tavern.Discover.Options[0];
            var healthBefore = service.State.Player.Health;

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(healthBefore - picked.TavernTier, service.State.Player.Health);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == picked.CardId));
        }

        [Test]
        public void TierFourTombTurning_DiesThroughRecruitDeathPipelineWithoutSellEffect()
        {
            var service = MatchService.CreateWithDefaultCatalog(94111, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "126957", CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            var doomed = service.State.Player.Tavern.Hand.Single(card => card.Tags.Contains("discover_then_death"));
            doomed.CardId = "BG34_690";
            doomed.Name = "Plaguerunner";
            doomed.Tribes = new List<Tribe> { Tribe.Undead };
            if (!doomed.Keywords.Contains(Keyword.Deathrattle))
            {
                doomed.Keywords.Add(Keyword.Deathrattle);
            }

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.IndexOf(doomed)));

            Assert.IsFalse(service.State.Player.Board.Any(minion => minion.InstanceId == doomed.InstanceId));
            Assert.AreEqual(4, service.State.Player.Tavern.UndeadAttackBonus);
        }

        [Test]
        public void TierFourFearlessFoodie_ChoiceCanImproveGemsOrAddGems()
        {
            var growth = MatchService.CreateWithDefaultCatalog(9412, new InMemoryTestScenarioRepository());
            AddAndPlay(growth, "BG30_123");

            growth.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(1, growth.State.Player.Tavern.BloodGemBonusAttack);
            Assert.AreEqual(1, growth.State.Player.Tavern.BloodGemBonusHealth);

            var gems = MatchService.CreateWithDefaultCatalog(9413, new InMemoryTestScenarioRepository());
            gems.State.Player.Tavern.Hand.Clear();
            AddAndPlay(gems, "BG30_123");

            gems.Apply(new GameCommand(GameCommandType.ChooseDiscover, 1));

            Assert.AreEqual(4, gems.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
        }

        [Test]
        public void TierFourSpellcraftMinions_GenerateAndResolveTheirSpells()
        {
            var service = MatchService.CreateWithDefaultCatalog(9414, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 4;
            service.State.Player.Tavern.Hand.Clear();
            AddAndPlay(service, "BG30_117");
            AddAndPlay(service, "BG33_319");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "VOLCANIC_VISITOR_ATTACK_SPELL"));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "VOLCANIC_VISITOR_HEALTH_SPELL"));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "FROSTLING_PRIESTESS_SPELL"));

            var volcanic = service.State.Player.Board.First(card => card.CardId == "BG30_117");
            var attackBefore = volcanic.Attack;
            var attackSpellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "VOLCANIC_VISITOR_ATTACK_SPELL");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, attackSpellIndex));

            Assert.AreEqual(attackBefore + 4, volcanic.Attack);
            Assert.IsTrue(volcanic.Tags.Contains("temporary_spellcraft"));

            var frostlingSpellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "FROSTLING_PRIESTESS_SPELL");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, frostlingSpellIndex));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell && card.Tags.Contains("stat_tavern_spell")));
        }

        [Test]
        public void TierFourDoomsdayDragonEgg_LocksThenHatchesIntoChosenTierSixDragon()
        {
            var service = MatchService.CreateWithDefaultCatalog(9415, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG34_639", CardKind.Minion));
            Assert.IsTrue(service.State.Player.Tavern.Hand[0].Tags.Contains("locked_in_hand"));

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.IsNull(service.State.Player.Tavern.Discover);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(card => card.TavernTier == 6 && card.Tribes.Contains(Tribe.Dragon)));
            var picked = service.State.Player.Tavern.Discover.Options[0];

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(picked.CardId, service.State.Player.Tavern.Hand[0].CardId);
            Assert.AreEqual(6, service.State.Player.Tavern.Hand[0].TavernTier);
            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.CardId == "BG34_639"));
        }

        [Test]
        public void TierFourBalladist_BuffsPirateHealthByGoldSpentThisTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(9416, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            var pirate = Card("target-pirate", BoardSide.Player, "TARGET_PIRATE", 3, 5, Tribe.Pirate);
            service.State.Player.Board.Add(pirate);
            var healthBefore = pirate.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.RerollShop));
            AddAndPlay(service, "BG26_814");

            Assert.AreEqual(healthBefore + 2, pirate.MaxHealth);
        }

        [Test]
        public void TierFourWyvernFrontman_QueuesFreeRefreshesOnlyForActualDamageAndCapsAtThree()
        {
            var player = new[]
            {
                Card("wyvern", BoardSide.Player, "BG35_601", 2, 8, Tribe.Beast)
            };
            var opponent = new[]
            {
                Card("wall", BoardSide.Opponent, "WALL", 1, 30, Tribe.None)
            };

            var result = CombatEngine.SimulateBasicCombat(player, opponent, 9417, 40, new TavernState());

            Assert.AreEqual(3, result.PlayerRewards.Where(reward => reward.Type == CombatRewardType.GainFreeRefresh).Sum(reward => reward.Amount));
        }

        [Test]
        public void TierFourScrapper_TargetsFriendlyMechThenMagnetizesChosenDiscover()
        {
            var service = MatchService.CreateWithDefaultCatalog(9418, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            var target = Card("target-mech", BoardSide.Player, "TARGET_MECH", 1, 5, Tribe.Mech);
            service.State.Player.Board.Add(target);

            AddAndPlay(service, "BG29_503", 0);

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual("scrapper-magnetic", service.State.Player.Tavern.Discover.Source);
            Assert.AreEqual(target.InstanceId, service.State.Player.Tavern.Discover.TargetInstanceId);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(card => card.Tribes.Contains(Tribe.Mech)));
            var picked = service.State.Player.Tavern.Discover.Options[0];
            var attackBefore = target.Attack;
            var healthBefore = target.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(attackBefore + picked.Attack, target.Attack);
            Assert.AreEqual(healthBefore + picked.MaxHealth, target.MaxHealth);
            Assert.IsNull(service.State.Player.Tavern.Discover);
        }

        [Test]
        public void TierFourGoldenScrapper_RepeatsMagneticDiscoverWithoutDoublingFirstPick()
        {
            var service = MatchService.CreateWithDefaultCatalog(9419, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            var target = Card("target-mech", BoardSide.Player, "TARGET_MECH", 1, 5, Tribe.Mech);
            service.State.Player.Board.Add(target);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG29_503", CardKind.Minion));
            service.State.Player.Tavern.Hand[0].Golden = true;

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            var first = service.State.Player.Tavern.Discover.Options[0];
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(1, service.State.Player.Tavern.Discover.RemainingPicks);

            var second = service.State.Player.Tavern.Discover.Options[0];
            var attackBeforeSecond = target.Attack;
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(1 + first.Attack + second.Attack, target.Attack);
            Assert.AreEqual(attackBeforeSecond + second.Attack, target.Attack);
            Assert.IsNull(service.State.Player.Tavern.Discover);
        }

        [Test]
        public void TierFourHeavyMetalWyrm_GoldenTriggersBothAdjacentBattlecries()
        {
            var wyrm = Card("p-wyrm", BoardSide.Player, "BG26_801", 1, 1, Tribe.Beast, Keyword.Taunt, Keyword.Deathrattle);
            wyrm.Golden = true;
            var player = new[]
            {
                Card("p-refresh", BoardSide.Player, "BGS_116", 0, 10, Tribe.None, Keyword.Battlecry),
                wyrm,
                Card("p-chieftain", BoardSide.Player, "BG35_143", 0, 10, Tribe.Naga, Keyword.Battlecry)
            };
            var opponent = new[]
            {
                Card("o-wall", BoardSide.Opponent, "WALL", 1, 40, Tribe.None)
            };

            var result = CombatEngine.SimulateBasicCombat(player, opponent, 9421, 20, new TavernState());

            Assert.IsTrue(result.PlayerRewards.Any(reward => reward.Type == CombatRewardType.GainFreeRefresh && reward.Amount == 2));
            Assert.IsTrue(result.PlayerRewards.Any(reward => reward.Type == CombatRewardType.AddGeneratedSpellToHand && reward.CardId == "131218"));
        }

        [Test]
        public void TierFourRecruiterOfTheDeep_CastsChefChoiceForSameTypeReward()
        {
            var player = new[]
            {
                Card("p-recruiter", BoardSide.Player, "BG34_925", 3, 5, Tribe.Naga),
                Card("p-right", BoardSide.Player, "RIGHT_NAGA", 1, 10, Tribe.Naga)
            };
            var opponent = new[]
            {
                Card("o-wall", BoardSide.Opponent, "WALL", 0, 40, Tribe.None, Keyword.Taunt)
            };

            var result = CombatEngine.SimulateBasicCombat(player, opponent, 9422, 3, new TavernState());

            Assert.IsTrue(result.PlayerRewards.Any(reward => reward.Type == CombatRewardType.AddRandomSameTribeMinionToHand && reward.CardId == Tribe.Naga.ToString()));
            Assert.IsFalse(result.FinalPlayerBoard.First(minion => minion.InstanceId == "p-right").Enchantments.Any(enchantment => enchantment.SourceId == "Chef's Choice"));
        }

        [Test]
        public void TierFourCombatPressure_DeathrattleChainsStayWithinSevenSlots()
        {
            var player = new[]
            {
                Card("p-blaster", BoardSide.Player, "BG_DAL_775", 1, 1, Tribe.None, Keyword.Taunt, Keyword.Deathrattle),
                Card("p-punisher", BoardSide.Player, "BG33_156", 1, 1, Tribe.Demon, Keyword.Taunt, Keyword.Deathrattle),
                Card("p-nest", BoardSide.Player, "BG34_731", 1, 1, Tribe.Dragon, Keyword.Deathrattle),
                Card("p-auto", BoardSide.Player, "BG32_172", 1, 1, Tribe.Mech, Keyword.Deathrattle),
                Card("p-wyrm", BoardSide.Player, "BG26_801", 1, 1, Tribe.Beast, Keyword.Taunt, Keyword.Deathrattle),
                Card("p-filler-a", BoardSide.Player, "A", 1, 1, Tribe.None),
                Card("p-filler-b", BoardSide.Player, "B", 1, 1, Tribe.None)
            };
            var opponent = new[]
            {
                Card("o-a", BoardSide.Opponent, "OA", 8, 8, Tribe.None),
                Card("o-b", BoardSide.Opponent, "OB", 8, 8, Tribe.None),
                Card("o-c", BoardSide.Opponent, "OC", 8, 8, Tribe.None)
            };

            var result = CombatEngine.SimulateBasicCombat(player, opponent, 9420, 80, new TavernState());

            Assert.IsFalse(result.SafetyStopped);
            Assert.IsTrue(result.Replay.Frames.All(frame => frame.PlayerBoardSnapshot.Minions.Count <= 7 && frame.OpponentBoardSnapshot.Minions.Count <= 7));
            Assert.IsTrue(result.Replay.Frames.Any(frame => frame.EventType == CombatEventType.SummonOverflowed || frame.EventType == CombatEventType.DeathrattleResolved));
        }

        [Test]
        public void TierFourGoldenEconomyAndRazorfenFlapper_UseOfficialGoldenValues()
        {
            var service = MatchService.CreateWithDefaultCatalog(9423, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Gold = 0;
            service.State.Player.Tavern.MaxGold = 20;
            service.State.Player.Tavern.LostLastCombat = true;
            var turtle = Card("golden-turtle", BoardSide.Player, "BG24_018", 8, 8, Tribe.Beast);
            turtle.Golden = true;
            service.State.Player.Board.Add(turtle);

            service.Apply(new GameCommand(GameCommandType.SellMinion, turtle.InstanceId));

            Assert.AreEqual(10, service.State.Player.Tavern.Gold);

            var flapper = Card("golden-flapper", BoardSide.Player, "BG34_682", 0, 1, Tribe.Quilboar, Keyword.Taunt, Keyword.Deathrattle);
            flapper.Golden = true;
            var result = CombatEngine.SimulateBasicCombat(
                new[] { flapper },
                new[] { Card("flapper-wall", BoardSide.Opponent, "WALL", 1, 20, Tribe.None) },
                9424,
                20,
                new TavernState());

            Assert.AreEqual(
                2,
                result.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.AddTavernSpellToHand && reward.CardId == "126676")
                    .Sum(reward => reward.Amount));
        }

        private static void AddAndPlay(MatchService service, string cardId, int targetIndex = -1)
        {
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, targetIndex));
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
