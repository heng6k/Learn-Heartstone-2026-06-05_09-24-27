using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class DomainEngineTests
    {
        [Test]
        public void SeededRng_RepeatsSequenceForSameSeed()
        {
            var first = new SeededRng(12345);
            var second = new SeededRng(12345);

            Assert.AreEqual(first.NextInt(1000), second.NextInt(1000));
            Assert.AreEqual(first.NextInt(1000), second.NextInt(1000));
        }

        [Test]
        public void MinionPool_DrawsAndReleasesCopies()
        {
            var definition = TestMinion("m1", 1, 2, 3, 2);
            var pool = new MinionPool(new[] { definition });

            var drawn = pool.DrawShop(1, 1, new SeededRng(1));

            Assert.AreEqual("m1", drawn[0].Id);
            Assert.AreEqual(1, pool.Remaining("m1"));

            pool.Release("m1", 1);
            Assert.AreEqual(2, pool.Remaining("m1"));
        }

        [Test]
        public void TripleEngine_CombinesThreeCopiesIntoGolden()
        {
            var items = new List<MinionInstance>
            {
                TestInstance("a", "m1", 1),
                TestInstance("b", "m1", 1),
                TestInstance("c", "m1", 1),
                TestInstance("d", "m2", 0)
            };

            Assert.AreEqual("m1", TripleEngine.FindTripleCandidate(items));

            var result = TripleEngine.ResolveTriple(items, "m1", BoardSide.Player, "unit");

            Assert.AreEqual(1, result.Remaining.Count);
            Assert.IsTrue(result.Golden.Golden);
            Assert.AreEqual(3, result.Golden.PoolCopiesHeld);
        }

        [Test]
        public void TripleEngine_DoesNotUseTavernSpellsAsTripleMaterials()
        {
            var items = new List<MinionInstance>
            {
                TestInstance("a", "m1", 1),
                TestInstance("b", "m1", 1),
                TestInstance("coin", "m1", 0)
            };
            items[2].CardKind = CardKind.TavernSpell;

            Assert.IsNull(TripleEngine.FindTripleCandidate(items));
            Assert.Throws<System.InvalidOperationException>(() => TripleEngine.ResolveTriple(items, "m1", BoardSide.Player, "spell-ignored"));
        }

        [Test]
        public void TripleEngine_PreservesEnchantmentsAndGrantedKeywordsFromAllMaterials()
        {
            var items = new List<MinionInstance>
            {
                TestInstance("buffed-a", "buffed", 1),
                TestInstance("buffed-b", "buffed", 1),
                TestInstance("buffed-c", "buffed", 1)
            };
            for (var index = 0; index < items.Count; index += 1)
            {
                items[index].BaseAttack = 2;
                items[index].BaseHealth = 2;
                var keyword = index == 0 ? Keyword.Taunt : index == 1 ? Keyword.DivineShield : Keyword.Reborn;
                items[index].Keywords.Add(keyword);
                StatMath.ApplyEnchantment(items[index], new Enchantment
                {
                    Id = "shared-buff",
                    SourceId = "shared-buff",
                    AttackBonus = index + 1,
                    HealthBonus = index + 1,
                    AddedKeywords = new List<Keyword> { keyword }
                });
            }

            var result = TripleEngine.ResolveTriple(items, "buffed", BoardSide.Player, "all-buffs");

            Assert.AreEqual(10, result.Golden.Attack);
            Assert.AreEqual(10, result.Golden.MaxHealth);
            Assert.AreEqual(3, result.Golden.Enchantments.Count(enchantment => enchantment.Id == "shared-buff"));
            Assert.Contains(Keyword.Taunt, result.Golden.Keywords);
            Assert.Contains(Keyword.DivineShield, result.Golden.Keywords);
            Assert.Contains(Keyword.Reborn, result.Golden.Keywords);
        }

        [Test]
        public void MatchServiceRecruitCheckpoint_SixSkeletonsCreateTwoGoldenTriplesAndFreeBoardSpace()
        {
            var service = (MatchService)FormatterServices.GetUninitializedObject(typeof(MatchService));
            var board = new List<MinionInstance>();
            var hand = new List<MinionInstance>();
            for (var index = 0; index < 5; index += 1)
            {
                var skeleton = TestInstance("pre-skeleton-" + index, "skeleton", 0);
                skeleton.CardId = "SKELETON";
                skeleton.Name = "Skeleton";
                skeleton.Tribes = new List<Tribe> { Tribe.Undead };
                board.Add(skeleton);
            }

            var bonehead = DeathrattleRewardSource("checkpoint-bonehead", "BG28_300");
            bonehead.Tribes = new List<Tribe> { Tribe.Undead };
            board.Add(bonehead);
            var state = new MatchState
            {
                Round = 1,
                Seed = 12345,
                Player = new LocalPlayerState
                {
                    Board = board,
                    Tavern = new TavernState
                    {
                        Hand = hand,
                        RecruitLog = new List<RecruitLogEntry>()
                    }
                }
            };
            typeof(MatchService).GetProperty(nameof(MatchService.State)).SetValue(service, state);
            var resolveTriples = typeof(MatchService).GetMethod("ResolvePlayerTriples", BindingFlags.Instance | BindingFlags.NonPublic);
            var checkpoint = (Action)Delegate.CreateDelegate(typeof(Action), service, resolveTriples);

            CombatEngine.ResolveRecruitPhaseDeath(board, bonehead, state.Player.Tavern, hand, 611, "test", checkpoint);

            Assert.AreEqual(2, hand.Count(minion => minion.DefinitionId == "skeleton" && minion.Golden));
            Assert.AreEqual(1, board.Count(minion => minion.DefinitionId == "skeleton" && !minion.Golden));
        }

        [Test]
        public void MatchService_GoldenSurfSpellcraftMetadataPreservesGoldenCrabIdentity()
        {
            var service = (MatchService)FormatterServices.GetUninitializedObject(typeof(MatchService));
            var state = new MatchState
            {
                Round = 1,
                Seed = 12345,
                Player = new LocalPlayerState
                {
                    Board = new List<MinionInstance>(),
                    Tavern = new TavernState
                    {
                        Hand = new List<MinionInstance>(),
                        RecruitLog = new List<RecruitLogEntry>()
                    }
                }
            };
            typeof(MatchService).GetProperty(nameof(MatchService.State)).SetValue(service, state);
            var source = TestInstance("golden-surf-source", "surf", 0);
            source.CardId = "BG27_004";
            source.Golden = true;
            var addSpellcraft = typeof(MatchService).GetMethod("AddSpellcraftFromSource", BindingFlags.Instance | BindingFlags.NonPublic);

            addSpellcraft.Invoke(service, new object[] { source, "test-surf", 1, true });

            var spell = state.Player.Tavern.Hand.Single(card => card.CardId == "SURF_N_SURF_SPELL");
            Assert.AreEqual(6, spell.Counters["crab_attack"]);
            Assert.AreEqual(4, spell.Counters["crab_health"]);
            Assert.AreEqual(1, spell.Counters["crab_golden"]);
        }

        [Test]
        public void CombatEngine_PrioritizesTauntAndRemovesDivineShield()
        {
            var attacker = TestInstance("p1", "attacker", 0);
            attacker.Attack = 2;
            attacker.Health = 2;
            var support = TestInstance("p2", "support", 0);
            var taunt = TestInstance("o1", "taunt", 0);
            taunt.Attack = 1;
            taunt.Health = 3;
            taunt.Keywords.Add(Keyword.Taunt);
            taunt.Keywords.Add(Keyword.DivineShield);
            var other = TestInstance("o2", "other", 0);
            other.Attack = 1;
            other.Health = 3;

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker, support }, new[] { other, taunt }, 42, 1);

            Assert.AreEqual(1, result.Steps);
            Assert.AreEqual("o1", result.Log.First(entry => entry.Title == "AttackResolved").TargetId);
            Assert.IsFalse(result.FinalOpponentBoard.Find(m => m.InstanceId == "o1").Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void CombatEngine_VenomousKillsDamagedTargetAndIsConsumed()
        {
            var attacker = TestInstance("p1", "venom", 0);
            attacker.Attack = 1;
            attacker.Keywords.Add(Keyword.Venomous);
            var defender = TestInstance("o1", "defender", 0);
            defender.Attack = 0;
            defender.Health = 10;
            defender.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker }, new[] { defender }, 1, 1);

            Assert.AreEqual(0, result.FinalOpponentBoard.Count);
            Assert.IsFalse(result.FinalPlayerBoard[0].Keywords.Contains(Keyword.Venomous));
        }

        [Test]
        public void CombatEngine_StealthDefendersAreSkippedWhenOtherTargetsExist()
        {
            var attacker = TestInstance("p1", "attacker", 0);
            var support = TestInstance("p2", "support", 0);
            var stealth = TestInstance("o1", "stealth", 0);
            stealth.Keywords.Add(Keyword.Stealth);
            var visible = TestInstance("o2", "visible", 0);

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker, support }, new[] { stealth, visible }, 5, 1);

            Assert.AreEqual("o2", result.Log.First(entry => entry.Title == "AttackResolved").TargetId);
        }

        [Test]
        public void CombatEngine_RotatesAttackersInsteadOfAlwaysUsingFirstMinion()
        {
            var first = TestInstance("p1", "first", 0);
            first.Attack = 1;
            first.Health = 10;
            first.MaxHealth = 10;
            var second = TestInstance("p2", "second", 0);
            second.Attack = 1;
            second.Health = 10;
            second.MaxHealth = 10;
            var defender = TestInstance("o1", "defender", 0);
            defender.Attack = 0;
            defender.Health = 10;
            defender.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { first, second }, new[] { defender }, 1, 3);
            var playerAttacks = result.Log.Where(entry => entry.Title == "AttackResolved" && entry.ActorId.StartsWith("p")).ToList();

            Assert.AreEqual("p1", playerAttacks[0].ActorId);
            Assert.AreEqual("p2", playerAttacks[1].ActorId);
        }

        [Test]
        public void CombatEngine_TierOneDeathrattlesSummonTokens()
        {
            var manasaber = TestInstance("p1", "manasaber", 0);
            manasaber.CardId = "BG26_800";
            manasaber.Attack = 0;
            manasaber.Health = 1;
            manasaber.MaxHealth = 1;
            manasaber.Keywords.Add(Keyword.Deathrattle);
            var attacker = TestInstance("o1", "attacker", 0);
            attacker.Attack = 1;
            attacker.Health = 1;

            var result = CombatEngine.SimulateBasicCombat(new[] { manasaber }, new[] { attacker }, 1, 1);

            Assert.AreEqual(2, result.FinalPlayerBoard.Count(card => card.CardId == "CUBLING"));
            Assert.IsTrue(result.FinalPlayerBoard.Where(card => card.CardId == "CUBLING").All(card => card.Keywords.Contains(Keyword.Taunt)));
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "MinionSummoned"));
        }

        [Test]
        public void CombatEngine_DeathrattleTokensUseDeadMinionsBoardPosition()
        {
            var rover = TestInstance("p-rover", "forest-rover", 0);
            rover.CardId = "BG31_801";
            rover.Attack = 0;
            rover.Health = 1;
            rover.MaxHealth = 1;
            rover.Tribes = new List<Tribe> { Tribe.Beast };
            rover.Keywords.Add(Keyword.Deathrattle);
            var right = TestInstance("p-right", "right", 0);
            right.Health = 10;
            right.MaxHealth = 10;
            var attacker = TestInstance("o1", "attacker", 0);
            attacker.Attack = 1;
            attacker.Health = 1;
            attacker.MaxHealth = 1;

            var result = CombatEngine.SimulateBasicCombat(new[] { rover, right }, new[] { attacker }, 1, 1);

            Assert.AreEqual("beetle", result.FinalPlayerBoard[0].DefinitionId);
            Assert.AreEqual("p-right", result.FinalPlayerBoard[1].InstanceId);
        }

        [Test]
        public void CombatEngine_TierTwoDeathrattlesSummonBasicTokensAndBuffUndead()
        {
            var forestRover = TestInstance("p-forest", "forest-rover", 0);
            forestRover.CardId = "BG31_801";
            forestRover.Attack = 0;
            forestRover.Health = 1;
            forestRover.MaxHealth = 1;
            forestRover.Tribes = new List<Tribe> { Tribe.Beast };
            forestRover.Keywords.Add(Keyword.Deathrattle);
            var smallAttacker = TestInstance("o-small", "small-attacker", 0);
            smallAttacker.Attack = 1;
            smallAttacker.Health = 1;
            smallAttacker.MaxHealth = 1;

            var forestResult = CombatEngine.SimulateBasicCombat(
                new[] { forestRover },
                new[] { smallAttacker },
                1,
                1,
                new TavernState { BeetleAttackBonus = 4, BeetleHealthBonus = 3 });
            var beetle = forestResult.FinalPlayerBoard.First(card => card.DefinitionId == "beetle");
            Assert.AreEqual(4, beetle.Attack);
            Assert.AreEqual(3, beetle.MaxHealth);
            Assert.IsTrue(beetle.Tribes.Contains(Tribe.Beast));

            var glowgullet = TestInstance("p-glow", "glowgullet", 0);
            glowgullet.CardId = "BG32_430";
            glowgullet.Attack = 0;
            glowgullet.Health = 1;
            glowgullet.MaxHealth = 1;
            glowgullet.Tribes = new List<Tribe> { Tribe.Quilboar };
            glowgullet.Keywords.Add(Keyword.Deathrattle);
            var glowAttacker = TestInstance("o-glow", "glow-attacker", 0);
            glowAttacker.Attack = 1;
            glowAttacker.Health = 1;
            glowAttacker.MaxHealth = 1;

            var glowResult = CombatEngine.SimulateBasicCombat(new[] { glowgullet }, new[] { glowAttacker }, 1, 1);
            var quilboars = glowResult.FinalPlayerBoard.Where(card => card.DefinitionId == "quilboar").ToList();
            Assert.AreEqual(2, quilboars.Count);
            Assert.IsTrue(quilboars.All(card => card.Attack == 2 && card.MaxHealth == 2 && card.Keywords.Contains(Keyword.Taunt)));
            Assert.IsTrue(quilboars.All(card => card.Enchantments.Any(enchantment => enchantment.SourceId == "Blood Gem")));

            var undeadTarget = TestInstance("p-undead", "undead-target", 0);
            undeadTarget.Attack = 0;
            undeadTarget.Health = 3;
            undeadTarget.MaxHealth = 3;
            undeadTarget.Tribes = new List<Tribe> { Tribe.Undead };
            undeadTarget.Keywords.Add(Keyword.Stealth);
            var scarletSkull = TestInstance("p-skull", "scarlet-skull", 0);
            scarletSkull.CardId = "BG25_022";
            scarletSkull.Attack = 0;
            scarletSkull.Health = 1;
            scarletSkull.MaxHealth = 1;
            scarletSkull.Tribes = new List<Tribe> { Tribe.Undead };
            scarletSkull.Keywords.Add(Keyword.Deathrattle);
            var skullAttacker = TestInstance("o-skull", "skull-attacker", 0);
            skullAttacker.Attack = 1;
            skullAttacker.Health = 3;
            skullAttacker.MaxHealth = 3;

            var skullResult = CombatEngine.SimulateBasicCombat(new[] { undeadTarget, scarletSkull }, new[] { skullAttacker }, 1, 2);
            var buffed = skullResult.FinalPlayerBoard.First(card => card.InstanceId == "p-undead");
            Assert.AreEqual(undeadTarget.Attack + 1, buffed.Attack);
            Assert.AreEqual(undeadTarget.MaxHealth + 2, buffed.MaxHealth);
        }

        [Test]
        public void CombatEngine_ScarletSkullBuffsUndeadBehindIt()
        {
            var scarletSkull = TestInstance("p-skull", "scarlet-skull", 0);
            scarletSkull.CardId = "BG25_022";
            scarletSkull.Attack = 0;
            scarletSkull.Health = 1;
            scarletSkull.MaxHealth = 1;
            scarletSkull.Tribes = new List<Tribe> { Tribe.Undead };
            scarletSkull.Keywords.Add(Keyword.Deathrattle);
            var undeadTarget = TestInstance("p-undead", "undead-target", 0);
            undeadTarget.Attack = 2;
            undeadTarget.Health = 4;
            undeadTarget.MaxHealth = 4;
            undeadTarget.Tribes = new List<Tribe> { Tribe.Undead };
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 1;
            opponent.Health = 10;
            opponent.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { scarletSkull, undeadTarget }, new[] { opponent }, 7, 1);
            var buffed = result.FinalPlayerBoard.First(card => card.InstanceId == "p-undead");

            Assert.AreEqual(3, buffed.Attack);
            Assert.AreEqual(6, buffed.MaxHealth);
        }

        [Test]
        public void CombatEngine_HummingBirdAuraBuffsFutureBeastSummons()
        {
            var forestRover = TestInstance("p-forest", "forest-rover", 0);
            forestRover.CardId = "BG31_801";
            forestRover.Attack = 0;
            forestRover.Health = 1;
            forestRover.MaxHealth = 1;
            forestRover.Tribes = new List<Tribe> { Tribe.Beast };
            forestRover.Keywords.Add(Keyword.Deathrattle);
            var hummingBird = TestInstance("p-humming", "humming-bird", 0);
            hummingBird.CardId = "BG26_805";
            hummingBird.Attack = 0;
            hummingBird.Health = 5;
            hummingBird.MaxHealth = 5;
            hummingBird.Tribes = new List<Tribe> { Tribe.Beast };
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 1;
            opponent.Health = 10;
            opponent.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { forestRover, hummingBird }, new[] { opponent }, 3, 1);
            var beetle = result.FinalPlayerBoard.First(card => card.DefinitionId == "beetle");

            Assert.AreEqual(3, beetle.Attack);
            Assert.IsTrue(beetle.Enchantments.Any(enchantment => enchantment.SourceId == "Humming Bird"));
        }

        [TestCase(BoardSide.Player)]
        [TestCase(BoardSide.Opponent)]
        public void CombatEngine_GoldrinnBuffsBeastsSummonedAfterItsDeathrattle(BoardSide auraSide)
        {
            var goldrinn = TestInstance(auraSide + "-goldrinn", "goldrinn", 0);
            goldrinn.CardId = "BGS_018";
            goldrinn.Owner = auraSide;
            goldrinn.Attack = 1;
            goldrinn.Health = 3;
            goldrinn.MaxHealth = 3;
            goldrinn.Tribes = new List<Tribe> { Tribe.Beast };
            goldrinn.Keywords.Add(Keyword.Deathrattle);
            var manasaber = TestInstance(auraSide + "-manasaber", "manasaber", 0);
            manasaber.CardId = "BG26_800";
            manasaber.Owner = auraSide;
            manasaber.Attack = 0;
            manasaber.Health = 3;
            manasaber.MaxHealth = 3;
            manasaber.Tribes = new List<Tribe> { Tribe.Beast };
            manasaber.Keywords.Add(Keyword.Deathrattle);
            var blaster = TestInstance((auraSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player) + "-blaster", "tunnel-blaster", 0);
            blaster.CardId = "BG_DAL_775";
            blaster.Owner = auraSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
            blaster.Attack = 0;
            blaster.Health = 1;
            blaster.MaxHealth = 1;
            blaster.Keywords.Add(Keyword.Taunt);
            blaster.Keywords.Add(Keyword.Deathrattle);

            var result = auraSide == BoardSide.Player
                ? CombatEngine.SimulateBasicCombat(new[] { goldrinn, manasaber }, new[] { blaster }, 301, 1)
                : CombatEngine.SimulateBasicCombat(new[] { blaster }, new[] { goldrinn, manasaber }, 301, 1);
            var board = auraSide == BoardSide.Player ? result.FinalPlayerBoard : result.FinalOpponentBoard;
            var cublings = board.Where(minion => minion.CardId == "CUBLING").ToList();

            Assert.AreEqual(2, cublings.Count);
            Assert.IsTrue(cublings.All(minion => minion.Attack == 8 && minion.MaxHealth == 9));
        }

        [TestCase(BoardSide.Player)]
        [TestCase(BoardSide.Opponent)]
        public void CombatEngine_IngeniousInventorBuffsMechsSummonedAfterItsDeathrattle(BoardSide auraSide)
        {
            var inventor = TestInstance(auraSide + "-inventor", "ingenious-inventor", 0);
            inventor.CardId = "BG35_890";
            inventor.Name = "Ingenious Inventor";
            inventor.Owner = auraSide;
            inventor.Attack = 1;
            inventor.Health = 3;
            inventor.MaxHealth = 3;
            inventor.Tribes = new List<Tribe> { Tribe.Mech };
            inventor.Keywords.Add(Keyword.Deathrattle);
            var assembler = TestInstance(auraSide + "-assembler", "auto-assembler", 0);
            assembler.CardId = "BG32_172";
            assembler.Owner = auraSide;
            assembler.Attack = 0;
            assembler.Health = 3;
            assembler.MaxHealth = 3;
            assembler.Tribes = new List<Tribe> { Tribe.Mech };
            assembler.Keywords.Add(Keyword.Deathrattle);
            var blaster = TestInstance((auraSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player) + "-inventor-blaster", "tunnel-blaster", 0);
            blaster.CardId = "BG_DAL_775";
            blaster.Owner = auraSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
            blaster.Attack = 0;
            blaster.Health = 1;
            blaster.MaxHealth = 1;
            blaster.Keywords.Add(Keyword.Taunt);
            blaster.Keywords.Add(Keyword.Deathrattle);

            var result = auraSide == BoardSide.Player
                ? CombatEngine.SimulateBasicCombat(new[] { inventor, assembler }, new[] { blaster }, 302, 1)
                : CombatEngine.SimulateBasicCombat(new[] { blaster }, new[] { inventor, assembler }, 302, 1);
            var board = auraSide == BoardSide.Player ? result.FinalPlayerBoard : result.FinalOpponentBoard;
            var automaton = board.Single(minion => minion.DefinitionId == "ancestral-automaton");

            Assert.AreEqual(5, automaton.Attack);
            Assert.IsTrue(result.PlayerRewards.Concat(result.OpponentRewards).Any(reward =>
                reward.Type == CombatRewardType.AncestralAutomatonSummoned && reward.Side == auraSide));
        }

        [TestCase(BoardSide.Player)]
        [TestCase(BoardSide.Opponent)]
        public void CombatEngine_RotHideGnollUsesDeathsBeforeItEnteredCombat(BoardSide gnollSide)
        {
            var bassgill = TestInstance(gnollSide + "-bassgill", "bassgill", 0);
            bassgill.CardId = "BG34_Giant_071";
            bassgill.Owner = gnollSide;
            bassgill.Attack = 0;
            bassgill.Health = 1;
            bassgill.MaxHealth = 1;
            bassgill.Keywords.Add(Keyword.Taunt);
            bassgill.Keywords.Add(Keyword.Deathrattle);
            var gnoll = TestInstance(gnollSide + "-gnoll", "rot-hide-gnoll", 0);
            gnoll.CardId = "BG25_013";
            gnoll.Owner = gnollSide;
            gnoll.Attack = 2;
            gnoll.Health = 5;
            gnoll.MaxHealth = 5;
            var attackers = new[]
            {
                TestInstance("condition-attacker-1", "attacker-1", 0),
                TestInstance("condition-attacker-2", "attacker-2", 0)
            };
            foreach (var attacker in attackers)
            {
                attacker.Owner = gnollSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
                attacker.Attack = 1;
                attacker.Health = 10;
                attacker.MaxHealth = 10;
            }

            var result = gnollSide == BoardSide.Player
                ? CombatEngine.SimulateBasicCombat(new[] { bassgill }, attackers, 303, 1, playerHand: new[] { gnoll })
                : CombatEngine.SimulateBasicCombat(attackers, new[] { bassgill }, 303, 1, opponentHand: new[] { gnoll });
            var board = gnollSide == BoardSide.Player ? result.FinalPlayerBoard : result.FinalOpponentBoard;

            Assert.AreEqual(3, board.Single(minion => minion.CardId == "BG25_013").Attack);
        }

        [TestCase(BoardSide.Player)]
        [TestCase(BoardSide.Opponent)]
        public void CombatEngine_DynamicHistoryStatsRefreshDuringCombat(BoardSide dynamicSide)
        {
            var eternalKnight = TestInstance(dynamicSide + "-eternal-dead", "eternal-knight-dead", 0);
            eternalKnight.CardId = "BG25_008";
            eternalKnight.Owner = dynamicSide;
            eternalKnight.Attack = 0;
            eternalKnight.Health = 1;
            eternalKnight.MaxHealth = 1;
            eternalKnight.Tribes = new List<Tribe> { Tribe.Undead };
            eternalKnight.Keywords.Add(Keyword.Taunt);
            var eternalSurvivor = TestInstance(dynamicSide + "-eternal-survivor", "eternal-knight-survivor", 0);
            eternalSurvivor.CardId = "BG25_008";
            eternalSurvivor.Owner = dynamicSide;
            eternalSurvivor.Attack = 4;
            eternalSurvivor.Health = 20;
            eternalSurvivor.MaxHealth = 20;
            eternalSurvivor.Tribes = new List<Tribe> { Tribe.Undead };
            var enemies = new[]
            {
                TestInstance("dynamic-enemy-1", "enemy-1", 0),
                TestInstance("dynamic-enemy-2", "enemy-2", 0),
                TestInstance("dynamic-enemy-3", "enemy-3", 0)
            };
            foreach (var enemy in enemies)
            {
                enemy.Owner = dynamicSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
                enemy.Attack = 1;
                enemy.Health = 20;
                enemy.MaxHealth = 20;
            }

            var result = dynamicSide == BoardSide.Player
                ? CombatEngine.SimulateBasicCombat(new[] { eternalKnight, eternalSurvivor }, enemies, 304, 1)
                : CombatEngine.SimulateBasicCombat(enemies, new[] { eternalKnight, eternalSurvivor }, 304, 1);
            var board = dynamicSide == BoardSide.Player ? result.FinalPlayerBoard : result.FinalOpponentBoard;
            var survivor = board.Single(minion => minion.InstanceId == eternalSurvivor.InstanceId);

            Assert.AreEqual(8, survivor.Attack);
            Assert.AreEqual(22, survivor.MaxHealth);
        }

        [TestCase(BoardSide.Player)]
        [TestCase(BoardSide.Opponent)]
        public void CombatEngine_UndeadGrowthAppliesToMinionsSummonedLater(BoardSide undeadSide)
        {
            var plaguerunner = TestInstance(undeadSide + "-plaguerunner", "plaguerunner", 0);
            plaguerunner.CardId = "BG34_690";
            plaguerunner.Name = "Plaguerunner";
            plaguerunner.Owner = undeadSide;
            plaguerunner.Attack = 1;
            plaguerunner.Health = 3;
            plaguerunner.MaxHealth = 3;
            plaguerunner.Tribes = new List<Tribe> { Tribe.Undead };
            plaguerunner.Keywords.Add(Keyword.Deathrattle);
            var bassgill = TestInstance(undeadSide + "-growth-bassgill", "bassgill", 0);
            bassgill.CardId = "BG34_Giant_071";
            bassgill.Owner = undeadSide;
            bassgill.Attack = 0;
            bassgill.Health = 3;
            bassgill.MaxHealth = 3;
            bassgill.Keywords.Add(Keyword.Deathrattle);
            var handUndead = TestInstance(undeadSide + "-hand-undead", "hand-undead", 0);
            handUndead.CardId = "HAND_UNDEAD";
            handUndead.Owner = undeadSide;
            handUndead.Attack = 2;
            handUndead.Health = 5;
            handUndead.MaxHealth = 5;
            handUndead.Tribes = new List<Tribe> { Tribe.Undead };
            var blaster = TestInstance((undeadSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player) + "-growth-blaster", "tunnel-blaster", 0);
            blaster.CardId = "BG_DAL_775";
            blaster.Owner = undeadSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
            blaster.Attack = 0;
            blaster.Health = 1;
            blaster.MaxHealth = 1;
            blaster.Keywords.Add(Keyword.Taunt);
            blaster.Keywords.Add(Keyword.Deathrattle);

            var result = undeadSide == BoardSide.Player
                ? CombatEngine.SimulateBasicCombat(new[] { plaguerunner, bassgill }, new[] { blaster }, 305, 1, playerHand: new[] { handUndead })
                : CombatEngine.SimulateBasicCombat(new[] { blaster }, new[] { plaguerunner, bassgill }, 305, 1, opponentHand: new[] { handUndead });
            var board = undeadSide == BoardSide.Player ? result.FinalPlayerBoard : result.FinalOpponentBoard;

            Assert.AreEqual(4, board.Single(minion => minion.CardId == "HAND_UNDEAD").Attack);
        }

        [TestCase(BoardSide.Player)]
        [TestCase(BoardSide.Opponent)]
        public void CombatEngine_FallingSkyGolemRefreshesWhenDeathrattleTriggers(BoardSide golemSide)
        {
            var manasaber = TestInstance(golemSide + "-golem-manasaber", "manasaber", 0);
            manasaber.CardId = "BG26_800";
            manasaber.Owner = golemSide;
            manasaber.Attack = 0;
            manasaber.Health = 1;
            manasaber.MaxHealth = 1;
            manasaber.Tribes = new List<Tribe> { Tribe.Beast };
            manasaber.Keywords.Add(Keyword.Taunt);
            manasaber.Keywords.Add(Keyword.Deathrattle);
            var golem = TestInstance(golemSide + "-falling-sky-golem", "falling-sky-golem", 0);
            golem.CardId = "BG35_342";
            golem.Owner = golemSide;
            golem.Attack = 4;
            golem.Health = 20;
            golem.MaxHealth = 20;
            var enemies = CreateCombatAttackers(golemSide, "golem-enemy");

            var result = golemSide == BoardSide.Player
                ? CombatEngine.SimulateBasicCombat(new[] { manasaber, golem }, enemies, 306, 1)
                : CombatEngine.SimulateBasicCombat(enemies, new[] { manasaber, golem }, 306, 1);
            var board = golemSide == BoardSide.Player ? result.FinalPlayerBoard : result.FinalOpponentBoard;
            var finalGolem = board.Single(minion => minion.InstanceId == golem.InstanceId);

            Assert.AreEqual(8, finalGolem.Attack);
            Assert.AreEqual(22, finalGolem.MaxHealth);
        }

        [TestCase(BoardSide.Player)]
        [TestCase(BoardSide.Opponent)]
        public void CombatEngine_AncestralAutomatonRefreshesWhenAnotherIsSummoned(BoardSide automatonSide)
        {
            var assembler = TestInstance(automatonSide + "-dynamic-assembler", "auto-assembler", 0);
            assembler.CardId = "BG32_172";
            assembler.Owner = automatonSide;
            assembler.Attack = 0;
            assembler.Health = 1;
            assembler.MaxHealth = 1;
            assembler.Keywords.Add(Keyword.Taunt);
            assembler.Keywords.Add(Keyword.Deathrattle);
            var automaton = TestInstance(automatonSide + "-existing-automaton", "existing-automaton", 0);
            automaton.CardId = "BG_TTN_401";
            automaton.Owner = automatonSide;
            automaton.Attack = 3;
            automaton.Health = 10;
            automaton.MaxHealth = 10;
            automaton.Tribes = new List<Tribe> { Tribe.Mech };
            var enemies = CreateCombatAttackers(automatonSide, "automaton-enemy");

            var result = automatonSide == BoardSide.Player
                ? CombatEngine.SimulateBasicCombat(new[] { assembler, automaton }, enemies, 307, 1)
                : CombatEngine.SimulateBasicCombat(enemies, new[] { assembler, automaton }, 307, 1);
            var board = automatonSide == BoardSide.Player ? result.FinalPlayerBoard : result.FinalOpponentBoard;

            Assert.IsTrue(board.Where(minion => minion.CardId == "BG_TTN_401" || minion.DefinitionId == "ancestral-automaton").All(minion =>
                minion.Attack >= 6 && minion.MaxHealth >= 6));
            Assert.IsTrue(result.PlayerRewards.Concat(result.OpponentRewards).Any(reward =>
                reward.Type == CombatRewardType.AncestralAutomatonSummoned && reward.Side == automatonSide));
        }

        [TestCase(BoardSide.Player)]
        [TestCase(BoardSide.Opponent)]
        public void CombatEngine_BeetleGrowthUpdatesExistingAndLaterBeetles(BoardSide beetleSide)
        {
            var shimmermoth = TestInstance(beetleSide + "-shimmermoth", "silky-shimmermoth", 0);
            shimmermoth.CardId = "BG32_204";
            shimmermoth.Owner = beetleSide;
            shimmermoth.Attack = 0;
            shimmermoth.Health = 1;
            shimmermoth.MaxHealth = 1;
            shimmermoth.Keywords.Add(Keyword.Taunt);
            shimmermoth.Keywords.Add(Keyword.Deathrattle);
            var existingBeetle = TestInstance(beetleSide + "-existing-beetle", "beetle", 0);
            existingBeetle.CardId = "BEETLE";
            existingBeetle.Name = "Beetle";
            existingBeetle.Owner = beetleSide;
            existingBeetle.Attack = 2;
            existingBeetle.Health = 10;
            existingBeetle.MaxHealth = 10;
            existingBeetle.Tribes = new List<Tribe> { Tribe.Beast };
            var enemies = CreateCombatAttackers(beetleSide, "beetle-enemy");

            var result = beetleSide == BoardSide.Player
                ? CombatEngine.SimulateBasicCombat(new[] { shimmermoth, existingBeetle }, enemies, 308, 1)
                : CombatEngine.SimulateBasicCombat(enemies, new[] { shimmermoth, existingBeetle }, 308, 1);
            var board = beetleSide == BoardSide.Player ? result.FinalPlayerBoard : result.FinalOpponentBoard;
            var beetles = board.Where(minion => minion.DefinitionId == "beetle").ToList();

            Assert.AreEqual(2, beetles.Count);
            Assert.IsTrue(beetles.All(minion => minion.Attack == 4));
            Assert.IsTrue(beetles.Any(minion => minion.InstanceId != existingBeetle.InstanceId && minion.MaxHealth == 3));
        }

        [TestCase(BoardSide.Player)]
        [TestCase(BoardSide.Opponent)]
        public void CombatEngine_TimewarpedNestSwarmerGivesExistingAndLaterBeetlesPlusTwoPlusTwo(BoardSide beetleSide)
        {
            var swarmer = TestInstance(beetleSide + "-nest-swarmer", "timewarped-nest-swarmer", 0);
            swarmer.CardId = "BG34_Giant_687";
            swarmer.Owner = beetleSide;
            swarmer.Attack = 0;
            swarmer.Health = 1;
            swarmer.MaxHealth = 1;
            swarmer.Keywords.Add(Keyword.Taunt);
            swarmer.Keywords.Add(Keyword.Deathrattle);
            var existingBeetle = TestInstance(beetleSide + "-nest-existing-beetle", "beetle", 0);
            existingBeetle.CardId = "BEETLE";
            existingBeetle.Name = "Beetle";
            existingBeetle.Owner = beetleSide;
            existingBeetle.Attack = 2;
            existingBeetle.Health = 10;
            existingBeetle.MaxHealth = 10;
            existingBeetle.Tribes = new List<Tribe> { Tribe.Beast };
            var enemies = CreateCombatAttackers(beetleSide, "nest-swarmer-enemy");

            var result = beetleSide == BoardSide.Player
                ? CombatEngine.SimulateBasicCombat(new[] { swarmer, existingBeetle }, enemies, 309, 1)
                : CombatEngine.SimulateBasicCombat(enemies, new[] { swarmer, existingBeetle }, 309, 1);
            var board = beetleSide == BoardSide.Player ? result.FinalPlayerBoard : result.FinalOpponentBoard;
            var beetles = board.Where(minion => minion.DefinitionId == "beetle").ToList();

            Assert.AreEqual(2, beetles.Count);
            Assert.IsTrue(beetles.All(minion => minion.Attack == 4));
            Assert.IsTrue(beetles.Any(minion => minion.InstanceId == existingBeetle.InstanceId && minion.MaxHealth == 12));
            Assert.IsTrue(beetles.Any(minion => minion.InstanceId != existingBeetle.InstanceId && minion.MaxHealth == 4));
        }

        [Test]
        public void CombatEngine_TwilightHatchlingSummonImmediatelyAttacks()
        {
            var hatchling = TestInstance("p-hatchling", "twilight-hatchling", 0);
            hatchling.CardId = "BG34_630";
            hatchling.Attack = 0;
            hatchling.Health = 1;
            hatchling.MaxHealth = 1;
            hatchling.Tribes = new List<Tribe> { Tribe.Dragon };
            hatchling.Keywords.Add(Keyword.Deathrattle);
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 1;
            opponent.Health = 5;
            opponent.MaxHealth = 5;

            var result = CombatEngine.SimulateBasicCombat(new[] { hatchling }, new[] { opponent }, 11, 2);

            Assert.IsTrue(result.Log.Any(entry => entry.Title == "ImmediateAttackQueued"));
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "TriggeredAttackResolved" && entry.ActorId.StartsWith("token-p-hatchling-hatchling")));
            Assert.AreEqual(2, result.FinalOpponentBoard.First().Health);
        }

        [Test]
        public void CombatEngine_WindfuryAttacksTwiceInOneTurn()
        {
            var windfury = TestInstance("p-windfury", "windfury", 0);
            windfury.Attack = 1;
            windfury.Health = 10;
            windfury.MaxHealth = 10;
            windfury.Keywords.Add(Keyword.Windfury);
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 0;
            opponent.Health = 10;
            opponent.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { windfury }, new[] { opponent }, 13, 2);
            var attacks = result.Log.Count(entry =>
                (entry.Title == "AttackResolved" || entry.Title == "TriggeredAttackResolved") &&
                entry.ActorId == "p-windfury");

            Assert.AreEqual(2, attacks);
            Assert.AreEqual(8, result.FinalOpponentBoard.First().Health);
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "WindfuryResolved"));
        }

        [Test]
        public void CombatEngine_TierTwoDeathrattlesQueueRecruitRewards()
        {
            var alert = DeathrattleRewardSource("p-alert", "BG35_340");
            var alertResult = CombatEngine.SimulateBasicCombat(new[] { alert }, new[] { LethalOpponent("o-alert") }, 21, 1);
            var discountReward = alertResult.PlayerRewards.First(reward => reward.Type == CombatRewardType.TavernSpellCostReduction);
            Assert.AreEqual(1, discountReward.Amount);

            var bully = DeathrattleRewardSource("p-bully", "BG35_432");
            var bullyResult = CombatEngine.SimulateBasicCombat(new[] { bully }, new[] { LethalOpponent("o-bully") }, 22, 1);
            Assert.AreEqual("BRISTLEBACK_BLOOD_GEM", bullyResult.PlayerRewards.First(reward => reward.Type == CombatRewardType.AddGeneratedSpellToHand).CardId);

            var hunter = DeathrattleRewardSource("p-hunter", "BG32_170");
            var hunterResult = CombatEngine.SimulateBasicCombat(new[] { hunter }, new[] { LethalOpponent("o-hunter") }, 23, 1);
            Assert.AreEqual("100596", hunterResult.PlayerRewards.First(reward => reward.Type == CombatRewardType.AddGeneratedSpellToHand).CardId);
            Assert.IsTrue(hunterResult.Log.Any(entry => entry.Title == "CombatRewardQueued"));
        }

        [Test]
        public void CombatEngine_TideRaiserCastsShiftingTideOnAdjacentMinion()
        {
            var tideRaiser = DeathrattleRewardSource("p-tide", "BG34_920");
            var adjacentNaga = TestInstance("p-naga", "naga", 0);
            adjacentNaga.Attack = 2;
            adjacentNaga.Health = 3;
            adjacentNaga.MaxHealth = 3;
            adjacentNaga.Tribes = new List<Tribe> { Tribe.Naga };
            var opponent = LethalOpponent("o-tide");

            var result = CombatEngine.SimulateBasicCombat(new[] { tideRaiser, adjacentNaga }, new[] { opponent }, 41, 1);
            var buffed = result.FinalPlayerBoard.First(card => card.InstanceId == "p-naga");

            Assert.AreEqual(6, buffed.Attack);
            Assert.AreEqual(7, buffed.MaxHealth);
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "CombatSpellCast" && entry.TargetId == "p-naga"));
        }

        [Test]
        public void CombatEngine_SleepySupporterRallyBuffsRightMinion()
        {
            var supporter = TestInstance("p-supporter", "sleepy-supporter", 0);
            supporter.CardId = "BG33_241";
            supporter.Attack = 1;
            supporter.Health = 5;
            supporter.MaxHealth = 5;
            supporter.Keywords.Add(Keyword.Rally);
            var right = TestInstance("p-right", "right", 0);
            right.Attack = 2;
            right.Health = 2;
            right.MaxHealth = 2;
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 0;
            opponent.Health = 10;
            opponent.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { supporter, right }, new[] { opponent }, 42, 1);
            var buffed = result.FinalPlayerBoard.First(card => card.InstanceId == "p-right");

            Assert.AreEqual(4, buffed.Attack);
            Assert.AreEqual(4, buffed.MaxHealth);
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "RallyResolved"));
        }

        [Test]
        public void CombatEngine_ExpertAviatorRallySummonsHighestAttackMinionFromHand()
        {
            var aviator = TestInstance("p-aviator", "expert-aviator", 0);
            aviator.CardId = "BG34_140";
            aviator.Attack = 1;
            aviator.Health = 5;
            aviator.MaxHealth = 5;
            aviator.Keywords.Add(Keyword.Rally);
            var low = TestInstance("hand-low", "low", 0);
            low.Attack = 2;
            var high = TestInstance("hand-high", "high", 0);
            high.Attack = 7;
            var opponent = TestInstance("o1", "opponent", 0);
            opponent.Attack = 0;
            opponent.Health = 10;
            opponent.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(
                new[] { aviator },
                new[] { opponent },
                43,
                1,
                null,
                null,
                new[] { low, high });

            Assert.IsTrue(result.FinalPlayerBoard.Any(card => card.InstanceId.Contains("hand-high")));
            Assert.IsFalse(result.FinalPlayerBoard.Any(card => card.InstanceId.Contains("hand-low")));
            Assert.IsTrue(result.Log.Any(entry => entry.Title == "RallyResolved" && entry.TargetId.Contains("hand-high")));
        }

        [Test]
        public void BoardTribeAnalyzer_CountsDualTribeAndAllMinions()
        {
            var dragonMurloc = TestInstance("dual", "dual", 0);
            dragonMurloc.Tribes = new List<Tribe> { Tribe.Dragon, Tribe.Murloc };
            var all = TestInstance("all", "all", 0);
            all.Tribes = new List<Tribe> { Tribe.All };

            var distribution = BoardTribeAnalyzer.Build(new[] { dragonMurloc, all });

            Assert.AreEqual(2, distribution[Tribe.Dragon]);
            Assert.AreEqual(2, distribution[Tribe.Murloc]);
            Assert.AreEqual(1, distribution[Tribe.Beast]);
            Assert.AreEqual(10, distribution.Count);
        }

        [Test]
        public void BoardTribeAnalyzer_HasTribeExpandsAllMinions()
        {
            var all = TestInstance("all", "all", 0);
            all.Tribes = new List<Tribe> { Tribe.All };
            var none = TestInstance("none", "none", 0);

            Assert.IsTrue(BoardTribeAnalyzer.HasTribe(all, Tribe.Beast));
            Assert.IsTrue(BoardTribeAnalyzer.HasTribe(all, Tribe.Naga));
            Assert.IsTrue(BoardTribeAnalyzer.HasTribe(all, Tribe.All));
            Assert.IsFalse(BoardTribeAnalyzer.HasTribe(all, Tribe.None));
            Assert.IsFalse(BoardTribeAnalyzer.HasTribe(none, Tribe.Beast));
        }

        [Test]
        public void BoardTribeAnalyzer_SelectByTribeAndCountTribeIncludeAllMinions()
        {
            var naga = TestInstance("naga", "naga", 0);
            naga.Tribes = new List<Tribe> { Tribe.Naga };
            var demonNaga = TestInstance("demon-naga", "demon-naga", 0);
            demonNaga.Tribes = new List<Tribe> { Tribe.Demon, Tribe.Naga };
            var all = TestInstance("all", "all", 0);
            all.Tribes = new List<Tribe> { Tribe.All };
            var beast = TestInstance("beast", "beast", 0);
            beast.Tribes = new List<Tribe> { Tribe.Beast };

            var board = new[] { naga, demonNaga, all, beast };
            var nagaTargets = BoardTribeAnalyzer.SelectByTribe(board, Tribe.Naga);

            CollectionAssert.AreEqual(new[] { "naga", "demon-naga", "all" }, nagaTargets.Select(minion => minion.InstanceId).ToArray());
            Assert.AreEqual(3, BoardTribeAnalyzer.CountTribe(board, Tribe.Naga));
            Assert.AreEqual(2, BoardTribeAnalyzer.CountTribe(board, Tribe.Demon));
        }

        [Test]
        public void BoardTribeAnalyzer_MostCommonUsesNoneForEmptyBoardAndStableTieOrder()
        {
            var player = new LocalPlayerState();
            Assert.AreEqual(Tribe.None, BoardTribeAnalyzer.GetMostCommonTribe(player));

            var dragon = TestInstance("dragon", "dragon", 0);
            dragon.Tribes = new List<Tribe> { Tribe.Dragon };
            var murloc = TestInstance("murloc", "murloc", 0);
            murloc.Tribes = new List<Tribe> { Tribe.Murloc };
            player.Board.Add(dragon);
            player.Board.Add(murloc);

            Assert.AreEqual(Tribe.Murloc, BoardTribeAnalyzer.GetMostCommonTribe(player));
        }

        [Test]
        public void BoardTribeAnalyzer_SelectOneOfEachTribeDoesNotSelectSameAllMinionMoreThanOnce()
        {
            var board = new List<MinionInstance>();
            for (var index = 0; index < 6; index += 1)
            {
                var minion = TestInstance("typed-" + index, "typed-" + index, 0);
                minion.Tribes = new List<Tribe> { (Tribe)index };
                board.Add(minion);
            }

            var all = TestInstance("all", "all", 0);
            all.Tribes = new List<Tribe> { Tribe.All };
            board.Add(all);

            var selected = BoardTribeAnalyzer.SelectOneOfEachTribe(board);

            Assert.LessOrEqual(selected.Count, 7);
            Assert.AreEqual(selected.Count, selected.Select(minion => minion.InstanceId).Distinct().Count());
            Assert.AreEqual(7, selected.Count);
        }

        [Test]
        public void BoardTribeAnalyzer_SumStatsFromDifferentTribesExcludesSourceAndRespectsMaxCount()
        {
            var source = TestInstance("source", "source", 0);
            source.Tribes = new List<Tribe> { Tribe.Naga };
            source.Attack = 20;
            source.MaxHealth = 20;
            var beast = TestInstance("beast", "beast", 0);
            beast.Tribes = new List<Tribe> { Tribe.Beast };
            beast.Attack = 3;
            beast.MaxHealth = 5;
            var all = TestInstance("all", "all", 0);
            all.Tribes = new List<Tribe> { Tribe.All };
            all.Attack = 7;
            all.MaxHealth = 11;
            var demon = TestInstance("demon", "demon", 0);
            demon.Tribes = new List<Tribe> { Tribe.Demon };
            demon.Attack = 13;
            demon.MaxHealth = 17;

            var stats = BoardTribeAnalyzer.SumStatsFromDifferentTribes(new[] { source, beast, all, demon }, source, 2);

            Assert.AreEqual(10, stats.Attack);
            Assert.AreEqual(16, stats.Health);
        }

        [Test]
        public void BoardTribeAnalyzer_RefreshRecomputesAfterSummonAndRebornBoardChanges()
        {
            var player = new LocalPlayerState();
            var dragon = TestInstance("dragon", "dragon", 0);
            dragon.Tribes = new List<Tribe> { Tribe.Dragon };
            player.Board.Add(dragon);

            BoardTribeAnalyzer.Refresh(player);
            Assert.AreEqual(1, player.BoardTribeDistribution[Tribe.Dragon]);

            var summoned = TestInstance("summoned", "summoned", 0);
            summoned.Tribes = new List<Tribe> { Tribe.Beast };
            player.Board.Add(summoned);
            BoardTribeAnalyzer.Refresh(player);
            Assert.AreEqual(1, player.BoardTribeDistribution[Tribe.Beast]);

            var reborn = TestInstance("reborn", "reborn", 0);
            reborn.Tribes = new List<Tribe> { Tribe.Undead };
            player.Board[1] = reborn;
            BoardTribeAnalyzer.Refresh(player);
            Assert.IsFalse(player.BoardTribeDistribution.ContainsKey(Tribe.Beast));
            Assert.AreEqual(1, player.BoardTribeDistribution[Tribe.Undead]);
        }

        private static MinionDefinition TestMinion(string id, int tier, int attack, int health, int poolCount)
        {
            return new MinionDefinition
            {
                Id = id,
                CardId = id.ToUpperInvariant(),
                DbfId = 1,
                Name = id,
                TavernTier = tier,
                BaseAttack = attack,
                BaseHealth = health,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                InPool = true,
                PoolCount = poolCount
            };
        }

        [Test]
        public void RecruitPhaseDeath_ResolvesDeathrattleSummonsBeforeReborn()
        {
            var target = TestInstance("bonehead", "bonehead", 1);
            target.CardId = "BG28_300";
            target.Tribes = new List<Tribe> { Tribe.Undead };
            target.Keywords.Add(Keyword.Deathrattle);
            target.Keywords.Add(Keyword.Reborn);
            target.OfficialKeywords.Add(Keyword.Reborn);
            target.Attack = 13;
            target.MaxHealth = 14;
            target.Enchantments.Add(new Enchantment { Id = "recruit-permanent-buff", AttackBonus = 7, HealthBonus = 12 });
            target.Enchantments.Add(new Enchantment { Id = "Undead Attack Bonus", SourceId = "Undead Attack Bonus", AttackBonus = 4 });
            target.Counters["recruit-permanent-counter"] = 3;
            var board = new List<MinionInstance> { target };
            board.AddRange(Enumerable.Range(0, 4).Select(index => TestInstance("filler-" + index, "filler", 0)));
            var tavern = new TavernState { UndeadAttackBonus = 4 };

            CombatEngine.ResolveRecruitPhaseDeath(board, target, tavern, new List<MinionInstance>(), 601, "test");

            Assert.AreEqual(7, board.Count);
            Assert.AreEqual(2, board.Count(minion => minion.Name == "Skeleton"));
            var reborn = board.Single(minion => minion.InstanceId.StartsWith("bonehead-reborn-"));
            Assert.AreEqual(1, reborn.Health);
            Assert.AreEqual(13, reborn.Attack);
            Assert.AreEqual(14, reborn.MaxHealth);
            Assert.IsTrue(reborn.Enchantments.Any(enchantment => enchantment.Id == "recruit-permanent-buff"));
            Assert.AreEqual(1, reborn.Enchantments.Count(enchantment => enchantment.Id == "Undead Attack Bonus"));
            Assert.AreEqual(3, reborn.Counters["recruit-permanent-counter"]);
            Assert.IsFalse(reborn.Keywords.Contains(Keyword.Reborn));
            Assert.IsFalse(reborn.OfficialKeywords.Contains(Keyword.Reborn));
            Assert.AreEqual(PoolSource.Summon, reborn.PoolSource);
            Assert.AreEqual(PoolSource.Summon, reborn.OriginPoolSource);
            Assert.AreEqual(0, reborn.PoolCopiesHeld);
        }

        [Test]
        public void RecruitPhaseDeath_DeathrattleCanFillBoardAndPreventReborn()
        {
            var target = TestInstance("full-bonehead", "bonehead", 1);
            target.CardId = "BG28_300";
            target.Tribes = new List<Tribe> { Tribe.Undead };
            target.Keywords.Add(Keyword.Deathrattle);
            target.Keywords.Add(Keyword.Reborn);
            var board = new List<MinionInstance> { target };
            board.AddRange(Enumerable.Range(0, 6).Select(index => TestInstance("full-filler-" + index, "filler", 0)));

            CombatEngine.ResolveRecruitPhaseDeath(board, target, new TavernState(), new List<MinionInstance>(), 602, "test");

            Assert.AreEqual(7, board.Count);
            Assert.AreEqual(1, board.Count(minion => minion.Name == "Skeleton"));
            Assert.IsFalse(board.Any(minion => minion.InstanceId.StartsWith("full-bonehead-reborn-")));
        }

        [Test]
        public void RecruitPhaseDeath_WarghoulTriggersOneAdjacentDeathrattle()
        {
            var bonehead = TestInstance("adjacent-bonehead", "bonehead", 0);
            bonehead.CardId = "BG28_300";
            bonehead.Tribes = new List<Tribe> { Tribe.Undead };
            bonehead.Keywords.Add(Keyword.Deathrattle);
            var warghoul = TestInstance("warghoul", "warghoul", 0);
            warghoul.CardId = "BG34_Giant_331";
            warghoul.Tribes = new List<Tribe> { Tribe.Undead };
            warghoul.Keywords.Add(Keyword.Deathrattle);
            var board = new List<MinionInstance> { bonehead, warghoul };

            CombatEngine.ResolveRecruitPhaseDeath(board, warghoul, new TavernState(), new List<MinionInstance>(), 603, "test");

            Assert.IsTrue(board.Contains(bonehead));
            Assert.AreEqual(2, board.Count(minion => minion.Name == "Skeleton"));
        }

        [Test]
        public void RecruitPhaseDeath_GoldenWarghoulTriggersBothAdjacentDeathrattles()
        {
            var left = TestInstance("left-bonehead", "left-bonehead", 0);
            left.CardId = "BG28_300";
            left.Tribes = new List<Tribe> { Tribe.Undead };
            left.Keywords.Add(Keyword.Deathrattle);
            var warghoul = TestInstance("golden-warghoul", "warghoul", 0);
            warghoul.CardId = "BG34_Giant_331";
            warghoul.Golden = true;
            warghoul.Tribes = new List<Tribe> { Tribe.Undead };
            warghoul.Keywords.Add(Keyword.Deathrattle);
            var right = TestInstance("right-bonehead", "right-bonehead", 0);
            right.CardId = "BG28_300";
            right.Tribes = new List<Tribe> { Tribe.Undead };
            right.Keywords.Add(Keyword.Deathrattle);
            var board = new List<MinionInstance> { left, warghoul, right };

            CombatEngine.ResolveRecruitPhaseDeath(board, warghoul, new TavernState(), new List<MinionInstance>(), 605, "test");

            Assert.AreEqual(4, board.Count(minion => minion.Name == "Skeleton"));
        }

        [Test]
        public void RecruitPhaseDeath_GoldenWarghoulWithTitusTriggersBothSidesTwice()
        {
            var left = DeathrattleRewardSource("left-plaguerunner", "BG34_690");
            left.Tribes = new List<Tribe> { Tribe.Undead };
            var warghoul = DeathrattleRewardSource("golden-warghoul-titus", "BG34_Giant_331");
            warghoul.Golden = true;
            warghoul.Tribes = new List<Tribe> { Tribe.Undead };
            var right = DeathrattleRewardSource("right-plaguerunner", "BG34_690");
            right.Tribes = new List<Tribe> { Tribe.Undead };
            var titus = TestInstance("titus", "titus", 0);
            titus.CardId = "BG25_354";
            var tavern = new TavernState();
            var board = new List<MinionInstance> { left, warghoul, right, titus };

            CombatEngine.ResolveRecruitPhaseDeath(board, warghoul, tavern, new List<MinionInstance>(), 606, "test");

            Assert.AreEqual(16, tavern.UndeadAttackBonus);
        }

        [Test]
        public void RecruitPhaseDeath_GoldrinnDoesNotPermanentlyBuffBeasts()
        {
            var goldrinn = DeathrattleRewardSource("recruit-goldrinn", "BGS_018");
            goldrinn.Tribes = new List<Tribe> { Tribe.Beast };
            goldrinn.Attack = 3;
            goldrinn.Health = 3;
            goldrinn.MaxHealth = 3;
            var warghoul = DeathrattleRewardSource("goldrinn-warghoul", "BG34_Giant_331");
            var board = new List<MinionInstance> { goldrinn, warghoul };

            CombatEngine.ResolveRecruitPhaseDeath(board, warghoul, new TavernState(), new List<MinionInstance>(), 607, "test");

            Assert.AreEqual(3, goldrinn.Attack);
            Assert.AreEqual(3, goldrinn.MaxHealth);
        }

        [Test]
        public void RecruitPhaseDeath_GoldrinnStillResolvesAttachedSurfNSurfDeathrattle()
        {
            var goldrinn = DeathrattleRewardSource("surf-goldrinn", "BGS_018");
            goldrinn.Tribes = new List<Tribe> { Tribe.Beast };
            goldrinn.Tags.Add("surf_n_surf_crab");
            goldrinn.Counters["surf_crab_attack"] = 3;
            goldrinn.Counters["surf_crab_health"] = 2;
            var warghoul = DeathrattleRewardSource("surf-warghoul", "BG34_Giant_331");
            var board = new List<MinionInstance> { goldrinn, warghoul };

            CombatEngine.ResolveRecruitPhaseDeath(board, warghoul, new TavernState(), new List<MinionInstance>(), 608, "test");

            var crab = board.Single(minion => minion.DefinitionId == "crab");
            Assert.AreEqual(3, crab.Attack);
            Assert.AreEqual(2, crab.MaxHealth);
            Assert.IsFalse(crab.Golden);
        }

        [Test]
        public void RecruitPhaseDeath_GoldenBoneheadSummonsFourOneOneSkeletons()
        {
            var bonehead = DeathrattleRewardSource("golden-bonehead", "BG28_300");
            bonehead.Golden = true;
            bonehead.Tribes = new List<Tribe> { Tribe.Undead };
            var board = new List<MinionInstance> { bonehead };

            CombatEngine.ResolveRecruitPhaseDeath(board, bonehead, new TavernState(), new List<MinionInstance>(), 609, "test");

            var skeletons = board.Where(minion => minion.DefinitionId == "skeleton").ToList();
            Assert.AreEqual(4, skeletons.Count);
            Assert.IsTrue(skeletons.All(minion => minion.Attack == 1 && minion.MaxHealth == 1));
        }

        [Test]
        public void RecruitPhaseDeath_GoldenSurfNSurfSummonsGoldenCrab()
        {
            var target = DeathrattleRewardSource("golden-surf-target", "TEST_SURF_TARGET");
            target.Tags.Add("surf_n_surf_crab");
            target.Counters["surf_crab_attack"] = 6;
            target.Counters["surf_crab_health"] = 4;
            target.Counters["surf_crab_golden"] = 1;
            var board = new List<MinionInstance> { target };

            CombatEngine.ResolveRecruitPhaseDeath(board, target, new TavernState(), new List<MinionInstance>(), 610, "test");

            var crab = board.Single(minion => minion.DefinitionId == "crab");
            Assert.IsTrue(crab.Golden);
            Assert.AreEqual("BG27_004_Gt2", crab.CardId);
            Assert.AreEqual(6, crab.Attack);
            Assert.AreEqual(4, crab.MaxHealth);
        }

        [TestCase(false, 4)]
        [TestCase(true, 8)]
        public void RecruitPhaseDeath_PlaguerunnerUsesOutsideCombatAmount(bool golden, int expected)
        {
            var plaguerunner = TestInstance("plaguerunner", "plaguerunner", 0);
            plaguerunner.CardId = "BG34_690";
            plaguerunner.Golden = golden;
            plaguerunner.Tribes = new List<Tribe> { Tribe.Undead };
            plaguerunner.Keywords.Add(Keyword.Deathrattle);
            var survivor = TestInstance("undead-survivor", "survivor", 0);
            survivor.Tribes = new List<Tribe> { Tribe.Undead };
            var tavern = new TavernState();
            var board = new List<MinionInstance> { plaguerunner, survivor };

            CombatEngine.ResolveRecruitPhaseDeath(board, plaguerunner, tavern, new List<MinionInstance>(), 604, "test");

            Assert.AreEqual(expected, tavern.UndeadAttackBonus);
            Assert.AreEqual(2 + expected, survivor.Attack);
        }

        private static MinionInstance TestInstance(string instanceId, string definitionId, int poolCopiesHeld)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = definitionId,
                CardId = definitionId.ToUpperInvariant(),
                Name = definitionId,
                Attack = 2,
                Health = 2,
                MaxHealth = 2,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Golden = false,
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true,
                AttacksThisCombat = 0,
                PoolSource = PoolSource.Pool,
                PoolCopiesHeld = poolCopiesHeld
            };
        }

        private static MinionInstance DeathrattleRewardSource(string instanceId, string cardId)
        {
            var source = TestInstance(instanceId, cardId.ToLowerInvariant(), 0);
            source.CardId = cardId;
            source.Attack = 0;
            source.Health = 1;
            source.MaxHealth = 1;
            source.Keywords.Add(Keyword.Deathrattle);
            return source;
        }

        private static MinionInstance[] CreateCombatAttackers(BoardSide friendlySide, string prefix)
        {
            var enemySide = friendlySide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
            return Enumerable.Range(0, 3)
                .Select(index =>
                {
                    var attacker = TestInstance(prefix + "-" + index, prefix + "-" + index, 0);
                    attacker.Owner = enemySide;
                    attacker.Attack = 1;
                    attacker.Health = 20;
                    attacker.MaxHealth = 20;
                    return attacker;
                })
                .ToArray();
        }

        private static MinionInstance LethalOpponent(string instanceId)
        {
            var opponent = TestInstance(instanceId, "opponent", 0);
            opponent.Attack = 1;
            opponent.Health = 10;
            opponent.MaxHealth = 10;
            return opponent;
        }
    }
}
