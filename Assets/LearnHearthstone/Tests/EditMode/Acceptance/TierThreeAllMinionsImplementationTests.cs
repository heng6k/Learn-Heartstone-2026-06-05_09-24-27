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
    public sealed class TierThreeAllMinionsImplementationTests
    {
        [Test]
        public void TierThreeRegistry_CoversEveryInPoolTierThreeMinion()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            var tierThreeIds = catalog.All
                .Where(minion => minion.InPool && minion.TavernTier == 3)
                .Select(minion => minion.CardId)
                .OrderBy(id => id)
                .ToList();
            var registeredIds = TierThreeMinionImplementationRegistry.All
                .Select(entry => entry.CardId)
                .OrderBy(id => id)
                .ToList();

            Assert.AreEqual(49, tierThreeIds.Count);
            Assert.AreEqual(tierThreeIds, registeredIds);
            Assert.AreEqual(49, registeredIds.Distinct().Count());
            Assert.IsTrue(TierThreeMinionImplementationRegistry.All.All(entry => !string.IsNullOrWhiteSpace(entry.Area)));
            Assert.IsTrue(TierThreeMinionImplementationRegistry.All.All(entry => !string.IsNullOrWhiteSpace(entry.Note)));
            Assert.IsTrue(TierThreeMinionImplementationRegistry.All
                .Where(entry => entry.CardId.StartsWith("BGDUO"))
                .All(entry => entry.Status == TierThreeImplementationStatus.OutOfScope));
            Assert.IsFalse(TierThreeMinionImplementationRegistry.All.Any(entry => entry.Status == TierThreeImplementationStatus.SoloApproximation));
            Assert.IsFalse(TierThreeMinionImplementationRegistry.All.Any(entry => entry.Status == TierThreeImplementationStatus.KeywordOnly));
        }

        [Test]
        public void TierThreeTavernPhase_BloodGemDeepBlueAndMagneticEffectsResolve()
        {
            var service = MatchService.CreateWithDefaultCatalog(8801);
            AddAndPlay(service, "BG20_100");
            AddAndPlay(service, "BG26_159");
            Assert.AreEqual(1, service.State.Player.Tavern.BloodGemBonusHealth);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BLOOD_GEM", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));
            Assert.GreaterOrEqual(service.State.Player.Board[0].MaxHealth, service.State.Player.Board[0].BaseHealth + 2);

            AddAndPlay(service, "BG26_502");
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            var deepBlueIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "DEEP_BLUE_SPELL");
            Assert.GreaterOrEqual(deepBlueIndex, 0);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, deepBlueIndex, 0));
            Assert.AreEqual(1, service.State.Player.Tavern.DeepBlueBonusAttack);
            Assert.AreEqual(1, service.State.Player.Tavern.DeepBlueBonusHealth);

            AddAndPlay(service, "BG31_815");
            var elementalIndex = service.State.Player.Board.FindIndex(minion => minion.Tribes.Contains(Tribe.Elemental));
            var beforeBoardCount = service.State.Player.Board.Count;
            AddAndPlay(service, "BG31_859", elementalIndex);
            Assert.AreEqual(beforeBoardCount, service.State.Player.Board.Count);
            Assert.Greater(service.State.Player.Board[elementalIndex].Attack, service.State.Player.Board[elementalIndex].BaseAttack);
        }

        [Test]
        public void TierThreeTavernPhase_PeggyGoldSpendAndChromawhelpTriggersResolve()
        {
            var service = MatchService.CreateWithDefaultCatalog(8802);
            AddAndPlay(service, "BG25_032");
            AddAndPlay(service, "BG26_135");
            var pirate = service.State.Player.Board.First(minion => minion.CardId == "BG26_135");
            var attackBeforeCardAdded = pirate.Attack;

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "104436", CardKind.TavernSpell));
            Assert.Greater(pirate.Attack, attackBeforeCardAdded);

            AddAndPlay(service, "BG26_810");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;
            service.Apply(new GameCommand(GameCommandType.UpgradeTavern));
            Assert.IsTrue(service.State.Player.Board.Any(minion => minion.Tribes.Contains(Tribe.Pirate) && minion.Attack > minion.BaseAttack));

            AddAndPlay(service, "BG34_635t");
            AddAndPlay(service, "BG34_638t");
            Assert.GreaterOrEqual(service.State.Player.Tavern.TavernSpellBonusAttack, 1);
            Assert.GreaterOrEqual(service.State.Player.Tavern.TavernSpellBonusHealth, 1);
        }

        [Test]
        public void TierThreeTavernPhase_LostCityLooterAddsPlayableBountyTavernSpells()
        {
            var service = MatchService.CreateWithDefaultCatalog(8812, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            AddAndPlay(service, "BG33_820");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.Tags.Contains("bounty")));
            var bounty = service.State.Player.Tavern.Hand.First(card => card.Tags.Contains("bounty"));
            Assert.AreEqual(CardKind.TavernSpell, bounty.CardKind);
            Assert.AreEqual(3, bounty.TavernTier);
            Assert.Contains(bounty.CardId, new[] { "122182", "122183", "122184", "122185", "122186" });
        }

        [Test]
        public void TierThreeTavernPhase_BountySpellsResolveAndTriggerTavernSpellCastEffects()
        {
            var service = MatchService.CreateWithDefaultCatalog(8813, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            AddAndPlay(service, "BG27_005");
            AddAndPlay(service, "BG26_135");
            var pirate = service.State.Player.Board.First(minion => minion.CardId == "BG26_135");
            var beforeAttack = pirate.Attack;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 0;

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "122186", CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(2, service.State.Player.Tavern.Gold);
            Assert.Greater(pirate.Attack, beforeAttack);
        }

        [Test]
        public void TierThreeTavernPhase_SprightlyScarabChooseOneBranchesRespectTarget()
        {
            var rebornService = MatchService.CreateWithDefaultCatalog(8814, new InMemoryTestScenarioRepository());
            rebornService.State.Player.Board.Clear();
            rebornService.State.Player.Tavern.Hand.Clear();
            var targetBeast = CardMinion("target-beast", BoardSide.Player, "TARGET_BEAST", 2, 3, Tribe.Beast);
            rebornService.State.Player.Board.Add(targetBeast);

            AddAndPlay(rebornService, "BG27_084", 0);
            Assert.IsNotNull(rebornService.State.Player.Tavern.Discover);
            rebornService.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(3, targetBeast.Attack);
            Assert.AreEqual(4, targetBeast.MaxHealth);
            Assert.IsTrue(targetBeast.Keywords.Contains(Keyword.Reborn));

            var windfuryService = MatchService.CreateWithDefaultCatalog(8815, new InMemoryTestScenarioRepository());
            windfuryService.State.Player.Board.Clear();
            windfuryService.State.Player.Tavern.Hand.Clear();

            AddAndPlay(windfuryService, "BG27_084");
            windfuryService.Apply(new GameCommand(GameCommandType.ChooseDiscover, 1));

            var scarab = windfuryService.State.Player.Board.Single(minion => minion.CardId == "BG27_084");
            Assert.AreEqual(scarab.BaseAttack + 4, scarab.Attack);
            Assert.IsTrue(scarab.Keywords.Contains(Keyword.Windfury));
        }

        [Test]
        public void TierThreeTavernPhase_DisguisedGraverobberDestroysSelectedUndeadAndAddsPlainCopy()
        {
            var service = MatchService.CreateWithDefaultCatalog(8816, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(CardMinion("undead-a", BoardSide.Player, "UNDEAD_A", 1, 1, Tribe.Undead));
            var selected = CardMinion("undead-b", BoardSide.Player, "UNDEAD_B", 2, 2, Tribe.Undead);
            selected.Attack = 9;
            selected.MaxHealth = 9;
            selected.Health = 9;
            selected.Enchantments.Add(new Enchantment { Id = "buff", SourceId = "buff", AttackBonus = 7, HealthBonus = 7 });
            service.State.Player.Board.Add(selected);

            AddAndPlay(service, "BG28_303", 1);

            Assert.IsFalse(service.State.Player.Board.Any(minion => minion.InstanceId == "undead-b"));
            Assert.IsTrue(service.State.Player.Board.Any(minion => minion.InstanceId == "undead-a"));
            var copy = service.State.Player.Tavern.Hand.Single(card => card.CardId == "UNDEAD_B");
            Assert.AreEqual(2, copy.Attack);
            Assert.AreEqual(2, copy.MaxHealth);
            Assert.AreEqual(0, copy.Enchantments.Count);
        }

        [Test]
        public void TierThreeTavernPhase_DisguisedGraverobberResolvesDestroyedMinionDeathrattle()
        {
            var service = MatchService.CreateWithDefaultCatalog(88161, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var bonehead = CardMinion("graverobber-bonehead", BoardSide.Player, "BG28_300", 1, 1, Tribe.Undead);
            bonehead.Keywords.Add(Keyword.Deathrattle);
            service.State.Player.Board.Add(bonehead);

            AddAndPlay(service, "BG28_303", 0);

            Assert.AreEqual(2, service.State.Player.Board.Count(minion => minion.Name == "Skeleton"));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "BG28_300"));
        }

        [Test]
        public void TierThreeTavernPhase_GoldenGraverobberDoesNotOverflowFullHand()
        {
            var service = MatchService.CreateWithDefaultCatalog(8817, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(CardMinion("undead", BoardSide.Player, "UNDEAD", 2, 2, Tribe.Undead));
            for (var index = 0; index < 9; index += 1)
            {
                service.State.Player.Tavern.Hand.Add(CardMinion("filler-" + index, BoardSide.Player, "FILLER_" + index, 1, 1, Tribe.None));
            }

            var goldenGraverobber = CardMinion("golden-graverobber", BoardSide.Player, "BG28_303", 8, 8, Tribe.None, Keyword.Battlecry);
            goldenGraverobber.Golden = true;
            service.State.Player.Tavern.Hand.Add(goldenGraverobber);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.AreEqual(10, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardId == "UNDEAD"));
            Assert.IsFalse(service.State.Player.Board.Any(minion => minion.InstanceId == "undead"));
        }

        [Test]
        public void TierThreeTavernPhase_PufferquilUsesSelectedSpellTarget()
        {
            var service = MatchService.CreateWithDefaultCatalog(8818, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var first = CardMinion("first", BoardSide.Player, "FIRST", 4, 4, Tribe.None);
            var pufferquil = CardMinion("pufferquil", BoardSide.Player, "BG25_039", 2, 6, Tribe.Quilboar);
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(pufferquil);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BLOOD_GEM", CardKind.Spell));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 1));

            Assert.IsFalse(first.Keywords.Contains(Keyword.Venomous));
            Assert.IsTrue(pufferquil.Keywords.Contains(Keyword.Venomous));
            Assert.AreEqual(3, pufferquil.Attack);
            Assert.AreEqual(7, pufferquil.MaxHealth);
        }

        [Test]
        public void TierThreeCatalog_MummifierReferencesRebornWithoutStartingWithIt()
        {
            var definition = MinionCatalogLoader.LoadFromResources().GetByCardId("BG28_309");

            Assert.That(definition.Keywords, Does.Contain(Keyword.Deathrattle));
            Assert.IsFalse(definition.Keywords.Contains(Keyword.Reborn));
            Assert.That(definition.OfficialKeywords, Does.Contain(Keyword.Reborn));
            Assert.That(definition.Golden.Keywords, Does.Contain(Keyword.Deathrattle));
            Assert.IsFalse(definition.Golden.Keywords.Contains(Keyword.Reborn));
            Assert.That(definition.Golden.OfficialKeywords, Does.Contain(Keyword.Reborn));
        }

        [Test]
        public void TierThreeCombat_EyesOfTheEarthMotherGoldenMummifierGivesTwoDifferentUndeadReborn()
        {
            var service = MatchService.CreateWithDefaultCatalog(8820, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            AddAndPlay(service, "BG28_309");
            var source = service.State.Player.Board.Single();
            source.InstanceId = "p-eyes-mummifier";
            source.Keywords.Remove(Keyword.Reborn);
            var first = CardMinion("p-eyes-undead-a", BoardSide.Player, "UNDEAD_A", 2, 20, Tribe.Undead);
            var second = CardMinion("p-eyes-undead-b", BoardSide.Player, "UNDEAD_B", 2, 20, Tribe.Undead);
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "100601", CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.IsTrue(source.Golden);
            var targets = ResolveMummifierTargets(service.State.Player.Board, source.InstanceId, 8820);
            CollectionAssert.AreEquivalent(new[] { first.InstanceId, second.InstanceId }, targets);
        }

        [Test]
        public void TierThreeCombat_SharedGoldenTransformerMummifierUsesTwoTargetEffect()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            var source = CardMinion("p-shared-golden-mummifier", BoardSide.Player, "BG28_309", 5, 2, Tribe.Undead, Keyword.Deathrattle);
            var first = CardMinion("p-shared-golden-undead-a", BoardSide.Player, "UNDEAD_A", 2, 20, Tribe.Undead);
            var second = CardMinion("p-shared-golden-undead-b", BoardSide.Player, "UNDEAD_B", 2, 20, Tribe.Undead);

            Assert.IsTrue(GoldenMinionTransformer.MakeGoldenInPlace(source, catalog));
            var targets = ResolveMummifierTargets(new[] { source, first, second }, source.InstanceId, 8825);

            Assert.IsTrue(source.Golden);
            CollectionAssert.AreEquivalent(new[] { first.InstanceId, second.InstanceId }, targets);
        }

        [Test]
        public void TierThreeCombat_TripleMummifierUsesGoldenTwoTargetEffect()
        {
            var materials = new[]
            {
                CardMinion("material-a", BoardSide.Player, "BG28_309", 5, 2, Tribe.Undead, Keyword.Deathrattle),
                CardMinion("material-b", BoardSide.Player, "BG28_309", 5, 2, Tribe.Undead, Keyword.Deathrattle),
                CardMinion("material-c", BoardSide.Player, "BG28_309", 5, 2, Tribe.Undead, Keyword.Deathrattle)
            };
            var source = TripleEngine.CreateGoldenFromMaterials(materials, "bg28_309", BoardSide.Player, "mummifier-test");
            var first = CardMinion("p-triple-undead-a", BoardSide.Player, "UNDEAD_A", 2, 20, Tribe.Undead);
            var second = CardMinion("p-triple-undead-b", BoardSide.Player, "UNDEAD_B", 2, 20, Tribe.Undead);

            var targets = ResolveMummifierTargets(new[] { source, first, second }, source.InstanceId, 8821);

            Assert.IsTrue(source.Golden);
            CollectionAssert.AreEquivalent(new[] { first.InstanceId, second.InstanceId }, targets);
        }

        [Test]
        public void TierThreeCombat_NormalMummifierRandomlySelectsExactlyOneCandidate()
        {
            var selectedAcrossSeeds = new HashSet<string>();
            for (var seed = 8822; seed < 8842; seed += 1)
            {
                var source = CardMinion("p-random-mummifier", BoardSide.Player, "BG28_309", 5, 2, Tribe.Undead, Keyword.Deathrattle);
                var candidates = new[]
                {
                    CardMinion("p-random-a", BoardSide.Player, "UNDEAD_A", 2, 20, Tribe.Undead),
                    CardMinion("p-random-b", BoardSide.Player, "UNDEAD_B", 2, 20, Tribe.Undead),
                    CardMinion("p-random-c", BoardSide.Player, "UNDEAD_C", 2, 20, Tribe.Undead)
                };

                var targets = ResolveMummifierTargets(new[] { source }.Concat(candidates), source.InstanceId, seed);

                Assert.AreEqual(1, targets.Count, "seed " + seed);
                selectedAcrossSeeds.Add(targets.Single());
            }

            Assert.Greater(selectedAcrossSeeds.Count, 1, "Mummifier must not always select the leftmost candidate.");
        }

        [Test]
        public void TierThreeRecruitDeath_GoldenMummifierExcludesAllMummifiersAndUsesUnifiedUndeadTribeRules()
        {
            var source = CardMinion("p-recruit-mummifier", BoardSide.Player, "BG28_309", 10, 4, Tribe.Undead, Keyword.Deathrattle);
            source.Golden = true;
            var otherMummifier = CardMinion("p-other-mummifier", BoardSide.Player, "BG28_309", 5, 2, Tribe.Undead, Keyword.Deathrattle);
            var otherGoldenMummifier = CardMinion("p-other-golden-mummifier", BoardSide.Player, "BG28_309_G", 10, 4, Tribe.Undead, Keyword.Deathrattle);
            otherGoldenMummifier.Golden = true;
            var handless = CardMinion("p-handless", BoardSide.Player, "BG25_010", 2, 1, Tribe.Undead, Keyword.Deathrattle);
            var allTribes = CardMinion("p-all", BoardSide.Player, "ALL_TRIBES", 2, 20, Tribe.All);
            var alreadyReborn = CardMinion("p-reborn", BoardSide.Player, "UNDEAD_REBORN", 2, 20, Tribe.Undead, Keyword.Reborn);
            var nonUndead = CardMinion("p-neutral", BoardSide.Player, "NEUTRAL", 2, 20, Tribe.None);
            var board = new List<MinionInstance> { source, otherMummifier, otherGoldenMummifier, handless, allTribes, alreadyReborn, nonUndead };

            CombatEngine.ResolveRecruitPhaseDeath(board, source, new TavernState(), new List<MinionInstance>(), 8842, "test");

            Assert.IsFalse(otherMummifier.Keywords.Contains(Keyword.Reborn));
            Assert.IsFalse(otherGoldenMummifier.Keywords.Contains(Keyword.Reborn));
            Assert.That(handless.Keywords, Does.Contain(Keyword.Reborn));
            Assert.That(allTribes.Keywords, Does.Contain(Keyword.Reborn));
            Assert.That(alreadyReborn.Keywords.Count(keyword => keyword == Keyword.Reborn), Is.EqualTo(1));
            Assert.IsFalse(nonUndead.Keywords.Contains(Keyword.Reborn));
        }

        [Test]
        public void TierThreeRecruitDeath_NormalMummifierAcceptsDualUndeadCandidate()
        {
            var source = CardMinion("p-dual-mummifier", BoardSide.Player, "BG28_309", 5, 2, Tribe.Undead, Keyword.Deathrattle);
            var dualUndead = CardMinion("p-dual-undead", BoardSide.Player, "DUAL_UNDEAD_MECH", 2, 20, Tribe.Mech);
            dualUndead.Tribes.Add(Tribe.Undead);
            var board = new List<MinionInstance> { source, dualUndead };

            CombatEngine.ResolveRecruitPhaseDeath(board, source, new TavernState(), new List<MinionInstance>(), 8845, "test");

            Assert.That(dualUndead.Keywords, Does.Contain(Keyword.Reborn));
        }

        [Test]
        public void TierThreeRecruitDeath_GoldenMummifierUsesAvailableCandidateWhenOnlyOneExists()
        {
            var source = CardMinion("p-shortage-mummifier", BoardSide.Player, "BG28_309", 10, 4, Tribe.Undead, Keyword.Deathrattle);
            source.Golden = true;
            var onlyCandidate = CardMinion("p-only-undead", BoardSide.Player, "UNDEAD", 2, 20, Tribe.Undead);
            var board = new List<MinionInstance> { source, onlyCandidate };

            CombatEngine.ResolveRecruitPhaseDeath(board, source, new TavernState(), new List<MinionInstance>(), 8843, "test");

            Assert.That(onlyCandidate.Keywords, Does.Contain(Keyword.Reborn));
        }

        [Test]
        public void TierThreeCombat_TitusRepeatsMummifierDeathrattleAgainstRemainingCandidates()
        {
            var source = CardMinion("p-titus-mummifier", BoardSide.Player, "BG28_309", 5, 2, Tribe.Undead, Keyword.Deathrattle);
            var titus = CardMinion("p-titus", BoardSide.Player, "BG25_354", 1, 30, Tribe.None);
            var first = CardMinion("p-titus-undead-a", BoardSide.Player, "UNDEAD_A", 2, 20, Tribe.Undead);
            var second = CardMinion("p-titus-undead-b", BoardSide.Player, "UNDEAD_B", 2, 20, Tribe.Undead);

            var targets = ResolveMummifierTargets(new[] { source, titus, first, second }, source.InstanceId, 8844);

            CollectionAssert.AreEquivalent(new[] { first.InstanceId, second.InstanceId }, targets);
        }

        [Test]
        public void TierThreeCombat_DeathrattleRewardsAndSummonTriggersResolve()
        {
            var piper = CardMinion("p-piper", BoardSide.Player, "BG26_160", 1, 1, Tribe.Quilboar, Keyword.Deathrattle);
            var mummifier = CardMinion("p-mummy", BoardSide.Player, "BG28_309", 1, 1, Tribe.Undead, Keyword.Deathrattle);
            var undead = CardMinion("p-undead", BoardSide.Player, "BG25_001", 2, 5, Tribe.Undead);
            var enemy = CardMinion("o-wall", BoardSide.Opponent, "WALL", 20, 20, Tribe.None);

            var result = CombatEngine.SimulateBasicCombat(new[] { piper, mummifier, undead }, new[] { enemy }, 8803, 40, new TavernState());

            Assert.That(result.PlayerRewards.Select(reward => reward.Type), Does.Contain(CombatRewardType.ImproveBloodGemAttack));

            var deflecto = CardMinion("p-deflecto", BoardSide.Player, "BGS_071", 3, 4, Tribe.Mech);
            var cordPuller = CardMinion("p-cord", BoardSide.Player, "BG29_611", 1, 1, Tribe.Mech, Keyword.Deathrattle, Keyword.Taunt);
            var deflectoResult = CombatEngine.SimulateBasicCombat(
                new[] { cordPuller, deflecto },
                new[]
                {
                    CardMinion("o-a", BoardSide.Opponent, "A", 3, 3, Tribe.None),
                    CardMinion("o-b", BoardSide.Opponent, "B", 3, 3, Tribe.None),
                    CardMinion("o-c", BoardSide.Opponent, "C", 3, 3, Tribe.None)
                },
                8807,
                10);
            Assert.IsTrue(deflectoResult.Replay.Frames.Any(frame => frame.EventType == CombatEventType.AttackTriggered && frame.ActorId == "p-deflecto"));

            var mummifierOnly = CardMinion("p-mummy-only", BoardSide.Player, "BG28_309", 1, 1, Tribe.Undead, Keyword.Deathrattle);
            var undeadOnly = CardMinion("p-undead-only", BoardSide.Player, "BG25_001", 2, 5, Tribe.Undead);
            var mummifierResult = CombatEngine.SimulateBasicCombat(new[] { mummifierOnly, undeadOnly }, new[] { enemy.Clone() }, 8805, 12);
            Assert.IsTrue(mummifierResult.Replay.Frames.Any(frame => frame.EventType == CombatEventType.DeathrattleResolved && frame.TargetId == "p-undead-only"));
        }

        [Test]
        public void TierThreeCombat_WildfireAndRefreshGrowthRewardsResolveUnderPressure()
        {
            var wildfire = CardMinion("p-wildfire", BoardSide.Player, "BGS_126", 10, 10, Tribe.Elemental);
            var wave = CardMinion("p-wave", BoardSide.Player, "BG34_856", 1, 1, Tribe.Elemental, Keyword.Deathrattle);
            var filler = CardMinion("p-filler", BoardSide.Player, "FILLER", 1, 10, Tribe.None);
            var left = CardMinion("o-left", BoardSide.Opponent, "LEFT", 2, 8, Tribe.None);
            var center = CardMinion("o-center", BoardSide.Opponent, "CENTER", 1, 2, Tribe.None, Keyword.Taunt);
            var right = CardMinion("o-right", BoardSide.Opponent, "RIGHT", 2, 8, Tribe.None);

            var result = CombatEngine.SimulateBasicCombat(new[] { wildfire, wave, filler }, new[] { left, center, right }, 8804, 40, new TavernState());
            var waveResult = CombatEngine.SimulateBasicCombat(new[] { wave.Clone() }, new[] { CardMinion("o-killer", BoardSide.Opponent, "KILLER", 5, 5, Tribe.None) }, 8806, 10, new TavernState());

            Assert.IsFalse(result.SafetyStopped);
            Assert.That(waveResult.PlayerRewards.Select(reward => reward.Type), Does.Contain(CombatRewardType.ImproveRefreshBuff));
            Assert.IsTrue(result.Replay.Frames.Any(frame => frame.EventType == CombatEventType.DamageTriggered && frame.ActorId == "p-wildfire"));
            Assert.IsTrue(result.Replay.Frames.All(frame => frame.PlayerBoardSnapshot.Minions.Count <= 7 && frame.OpponentBoardSnapshot.Minions.Count <= 7));
        }

        private static void AddAndPlay(MatchService service, string cardId, int targetIndex = -1)
        {
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, targetIndex));
        }

        private static List<string> ResolveMummifierTargets(IEnumerable<MinionInstance> board, string sourceInstanceId, int seed)
        {
            var result = CombatEngine.SimulateBasicCombat(
                board,
                new[] { CardMinion("o-mummifier-wall", BoardSide.Opponent, "WALL", 50, 50, Tribe.None, Keyword.Taunt) },
                seed,
                8,
                new TavernState());
            return result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.DeathrattleResolved &&
                                frame.ActorId == sourceInstanceId &&
                                !string.IsNullOrEmpty(frame.TargetId))
                .Select(frame => frame.TargetId)
                .ToList();
        }

        private static MinionInstance CardMinion(string id, BoardSide owner, string cardId, int attack, int health, Tribe tribe, params Keyword[] keywords)
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
                CanAttack = true
            };
        }
    }
}
