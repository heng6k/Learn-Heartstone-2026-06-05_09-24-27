using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14TrinketBehaviorTests
    {
        private const string DemonicBloodletterId = "BG36_MagicItem_800";
        private const string FrigidBlossomId = "BG36_MagicItem_300";
        private const string WandOfDivinationId = "BG36_MagicItem_307";
        private const string LesserGoldMalletId = "BG36_MagicItem_302";
        private const string GreaterGoldMalletId = "BG36_MagicItem_302t";
        private const string DefensiveShellId = "BG36_MagicItem_811";
        private const string GlowingCrystalId = "BG36_MagicItem_220";
        private const string SphereOfMemoryId = "BG36_MagicItem_372";
        private const string CookiesStirringRodId = "BG36_MagicItem_850";
        private const string SecretSchematicId = "BG36_MagicItem_840";
        private const string DragonSkullId = "BG36_MagicItem_203";
        private const string BallerPortraitId = "BG36_MagicItem_390";
        private const string LesserInductiveGyrobladeId = "BG36_MagicItem_810";
        private const string GreaterInductiveGyrobladeId = "BG36_MagicItem_810t";
        private const string EmergencyGearbladeId = "BG36_MagicItem_812";
        private const string FlightyPortraitId = "BG36_MagicItem_820";
        private const string LockboxPortraitId = "BG36_MagicItem_301";
        private const string DragonsEyeId = "BG36_MagicItem_215";
        private const string WarcryTotemId = "BG36_MagicItem_202";
        private const string LesserGlassOfPerspectiveId = "BG36_MagicItem_303";
        private const string PlaguerunnerPortraitId = "BG36_MagicItem_204";
        private const string AssemblerPortraitId = "BG36_MagicItem_841";
        private const string EscapeePortraitId = "BG36_MagicItem_363";
        private const string GreaterGlassOfPerspectiveId = "BG36_MagicItem_303t";
        private const string MyrmidonStickerId = "BG36_MagicItem_361";
        private const string EternalPortraitId = "BG36_MagicItem_216";
        private const string LightfeatherStickerId = "BG36_MagicItem_213";
        private const string TargetedTavernSpellId = "100596";
        private const string UntargetedTavernSpellId = "122186";

        [Test]
        public void DemonicBloodletter_TavernSpellBuffsCurrentAndFutureTavernMinions()
        {
            var service = CreateService();
            Equip(service, DemonicBloodletterId, 0);
            var boardTarget = Minion("bloodletter-board", Tribe.Beast);
            var shopTarget = Minion("bloodletter-shop", Tribe.Mech);
            service.State.Player.Board.Add(boardTarget);
            service.State.Player.Tavern.Shop.Add(shopTarget);

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, TargetedTavernSpellId, CardKind.TavernSpell, 0));

            Assert.AreEqual(3, shopTarget.Attack);
            Assert.AreEqual(4, shopTarget.MaxHealth);
            Assert.IsTrue(service.State.Player.Tavern.Growth.ShopModifiers.Any(modifier =>
                modifier.Tribe == Tribe.All && modifier.Attack == 1 && modifier.Health == 1));
        }

        [Test]
        public void FrigidBlossom_RefreshReducesUpgradeCost()
        {
            var service = CreateService();
            Equip(service, FrigidBlossomId, 0);
            service.State.Player.Tavern.UpgradeCost = 5;
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.AreEqual(4, service.State.Player.Tavern.UpgradeCost);
        }

        [Test]
        public void WandOfDivination_EveryThreeTargetedSpellsGrantsOneGold()
        {
            var service = CreateService();
            Equip(service, WandOfDivinationId, 0);
            service.State.Player.Board.Add(Minion("wand-target", Tribe.Dragon));
            service.State.Player.Tavern.Gold = 0;

            for (var cast = 0; cast < 3; cast += 1)
            {
                service.Apply(new GameCommand(GameCommandType.DebugCastCard, TargetedTavernSpellId, CardKind.TavernSpell, 0));
            }

            Assert.AreEqual(1, service.State.Player.Tavern.Gold);
        }

        [TestCase(LesserGoldMalletId, 0, 3)]
        [TestCase(GreaterGoldMalletId, 1, 5)]
        public void GoldMallet_EndTurnBuffIncludesGoldenMinionsPlayedThisGame(string cardId, int slot, int expectedBonus)
        {
            var service = CreateService();
            Equip(service, cardId, slot);
            var golden = Minion("gold-mallet-golden", Tribe.Pirate);
            golden.Golden = true;
            service.State.Player.Tavern.Hand.Add(golden);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));
            var attackBefore = golden.Attack;
            var healthBefore = golden.MaxHealth;
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(attackBefore + expectedBonus, golden.Attack);
            Assert.AreEqual(healthBefore + expectedBonus, golden.MaxHealth);
        }

        [Test]
        public void DefensiveShell_OnlyFirstMinionPlayedEachTurnGainsDivineShield()
        {
            var service = CreateService();
            Equip(service, DefensiveShellId, 0);
            var first = Minion("shell-first", Tribe.Beast);
            var second = Minion("shell-second", Tribe.Demon);
            service.State.Player.Tavern.Hand.Add(first);
            service.State.Player.Tavern.Hand.Add(second);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.IsTrue(first.Keywords.Contains(Keyword.DivineShield));
            Assert.IsFalse(second.Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void GlowingCrystal_StartTurnGrantsGoldPerDistinctFriendlyType()
        {
            var service = CreateService();
            var baseline = CreateService();
            Equip(service, GlowingCrystalId, 0);
            foreach (var target in new[]
            {
                Minion("crystal-beast", Tribe.Beast),
                Minion("crystal-dragon", Tribe.Dragon),
                Minion("crystal-murloc", Tribe.Murloc)
            })
            {
                service.State.Player.Board.Add(target);
                baseline.State.Player.Board.Add(target.Clone());
            }

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            baseline.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(baseline.State.Player.Tavern.Gold + 3, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void SphereOfMemory_EndTurnAddsTwoCopiesOfLastTavernSpell()
        {
            var service = CreateService();
            Equip(service, SphereOfMemoryId, 1);
            service.State.Player.Tavern.LastTavernSpellCardId = UntargetedTavernSpellId;

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == UntargetedTavernSpellId));
        }

        [Test]
        public void CookiesStirringRod_EveryFiveMurlocsAddsTwoTavernSpells()
        {
            var service = CreateService();
            Equip(service, CookiesStirringRodId, 1);
            for (var index = 0; index < 5; index += 1)
            {
                service.State.Player.Tavern.Hand.Add(Minion("cookie-murloc-" + index, Tribe.Murloc));
            }

            for (var index = 0; index < 5; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));
            }

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void SecretSchematic_BuyMechAddsRandomTavernSpell()
        {
            var service = CreateService();
            Equip(service, SecretSchematicId, 1);
            service.State.Player.Tavern.Shop.Add(Minion("schematic-mech", Tribe.Mech));
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void DragonSkull_EachBattlecryTriggerBuffsBothEdgeMinions()
        {
            var service = CreateService();
            Equip(service, DragonSkullId, 0);
            var left = Minion("dragon-skull-left", Tribe.Beast);
            var right = Minion("dragon-skull-right", Tribe.Mech);
            var battlecry = Minion("dragon-skull-battlecry", Tribe.Pirate);
            battlecry.Keywords.Add(Keyword.Battlecry);
            battlecry.OfficialKeywords.Add(Keyword.Battlecry);
            service.State.Player.Board.Add(left);
            service.State.Player.Board.Add(right);
            service.State.Player.Tavern.Hand.Add(battlecry);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(5, left.Attack);
            Assert.AreEqual(6, left.MaxHealth);
            Assert.AreEqual(5, battlecry.Attack);
            Assert.AreEqual(6, battlecry.MaxHealth);
            Assert.AreEqual(2, right.Attack);
            Assert.AreEqual(3, right.MaxHealth);
        }

        [Test]
        public void BallerPortrait_AddsOneBallerOnEquipAndAtTurnStart()
        {
            var service = CreateService();
            Equip(service, BallerPortraitId, 0);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card =>
                card.CardId == "BG31_816" || card.CardId == "BG31_818"));
        }

        [TestCase(LesserInductiveGyrobladeId, 0, 6)]
        [TestCase(GreaterInductiveGyrobladeId, 1, 10)]
        public void InductiveGyroblade_EndTurnAddsScalingMagneticSatellite(string cardId, int slot, int expectedStats)
        {
            var service = CreateService();
            Equip(service, cardId, slot);
            service.State.Player.Tavern.TavernSpellsCastThisTurn = 2;

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var satellite = service.State.Player.Tavern.Hand.Single(card => card.CardId == "MOONSTEEL_SATELLITE");
            Assert.AreEqual(expectedStats, satellite.Attack);
            Assert.AreEqual(expectedStats, satellite.MaxHealth);
            Assert.IsTrue(satellite.Keywords.Contains(Keyword.Magnetic));
        }

        [Test]
        public void EmergencyGearblade_EndTurnCastsRepairJobOnLeftmostMech()
        {
            var service = CreateService();
            Equip(service, EmergencyGearbladeId, 0);
            var beast = Minion("gearblade-beast", Tribe.Beast);
            var leftmostMech = Minion("gearblade-leftmost-mech", Tribe.Mech);
            var otherMech = Minion("gearblade-other-mech", Tribe.Mech);
            service.State.Player.Board.Add(beast);
            service.State.Player.Board.Add(leftmostMech);
            service.State.Player.Board.Add(otherMech);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(6, leftmostMech.Attack);
            Assert.AreEqual(11, leftmostMech.MaxHealth);
            Assert.AreEqual(2, otherMech.Attack);
            Assert.AreEqual(3, otherMech.MaxHealth);
        }

        [Test]
        public void FlightyPortrait_TavernSpellBuffsFlightyScoutsInHandAndOnBoard()
        {
            var service = CreateService();
            Equip(service, FlightyPortraitId, 0);
            var handScout = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG32_330");
            var boardScout = handScout.Clone();
            boardScout.InstanceId = "flighty-board";
            service.State.Player.Board.Add(boardScout);
            var handAttackBefore = handScout.Attack;
            var handHealthBefore = handScout.MaxHealth;
            var boardHealthBefore = boardScout.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, TargetedTavernSpellId, CardKind.TavernSpell, 0));

            Assert.AreEqual(handAttackBefore + 3, handScout.Attack);
            Assert.AreEqual(handHealthBefore + 3, handScout.MaxHealth);
            Assert.AreEqual(boardHealthBefore + 3, boardScout.MaxHealth);
        }

        [Test]
        public void LockboxPortrait_CreatesAndAcceleratesLockboxByTwoEachTurn()
        {
            var service = CreateService();
            Equip(service, LockboxPortraitId, 0);

            Assert.AreEqual(5, service.State.DelayedObjectStates.Single().RemainingTurns);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.DelayedObjectStates.Single().RemainingTurns);
        }

        [Test]
        public void DragonsEye_DragonBattlecryTriggersTwice()
        {
            var service = CreateService();
            Equip(service, DragonsEyeId, 1);
            var dragonBattlecry = Minion("dragons-eye-razorfen", Tribe.Dragon);
            dragonBattlecry.CardId = "BG20_100";
            dragonBattlecry.Keywords.Add(Keyword.Battlecry);
            dragonBattlecry.OfficialKeywords.Add(Keyword.Battlecry);
            service.State.Player.Tavern.Hand.Add(dragonBattlecry);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(4, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
        }

        [Test]
        public void WarcryTotem_FirstTwoBattlecryBuysEachCostTwoLess()
        {
            var service = CreateService();
            Equip(service, WarcryTotemId, 0);
            service.State.Player.Tavern.Gold = 20;
            for (var index = 0; index < 3; index += 1)
            {
                var battlecry = Minion("warcry-buy-" + index, Tribe.Pirate);
                battlecry.Cost = 3;
                battlecry.Keywords.Add(Keyword.Battlecry);
                service.State.Player.Tavern.Shop.Add(battlecry);
            }

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 2));

            Assert.AreEqual(15, service.State.Player.Tavern.Gold);
        }

        [TestCase(LesserGlassOfPerspectiveId, 0, 1)]
        [TestCase(GreaterGlassOfPerspectiveId, 1, 2)]
        public void GlassOfPerspective_AddsChooseOneCardsOnEquipAndTurnStart(string cardId, int slot, int amount)
        {
            var service = CreateService();
            service.State.Player.Tavern.Tier = 6;
            Equip(service, cardId, slot);

            Assert.AreEqual(amount, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.Keywords.Contains(Keyword.ChooseOne)));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(amount * 2, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.Keywords.Contains(Keyword.ChooseOne)));
        }

        [Test]
        public void PlaguerunnerPortrait_DestroyedPlaguerunnerAddsPlainCopy()
        {
            var service = CreateService();
            Equip(service, PlaguerunnerPortraitId, 0);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "110412", CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            var copy = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG34_690");
            Assert.IsFalse(copy.Golden);
        }

        [Test]
        public void EscapeePortrait_AddsGoldenEscapeeAndAcceleratesExistingLockbox()
        {
            var service = CreateService();
            Equip(service, LockboxPortraitId, 0);
            Assert.AreEqual(5, service.State.DelayedObjectStates.Single().RemainingTurns);

            Equip(service, EscapeePortraitId, 1);

            Assert.AreEqual(3, service.State.DelayedObjectStates.Single().RemainingTurns);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Single(card =>
                card.CardId == "BG36_523").Golden);
        }

        [Test]
        public void EternalPortrait_UndeadDeathPermanentlyBuffsGoldenEternalKnight()
        {
            var service = CreateService();
            Equip(service, EternalPortraitId, 1);
            var knight = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG25_008");
            var attackBefore = knight.Attack;
            var healthBefore = knight.MaxHealth;
            var undead = Minion("eternal-portrait-undead", Tribe.Undead);
            undead.Attack = undead.BaseAttack = 0;
            undead.Health = undead.MaxHealth = undead.BaseHealth = 1;
            service.State.Player.Board.Add(undead);
            var opponent = Minion("eternal-portrait-opponent", Tribe.None);
            opponent.Owner = BoardSide.Opponent;
            opponent.Attack = opponent.BaseAttack = 20;
            opponent.Health = opponent.MaxHealth = opponent.BaseHealth = 100;
            service.State.Opponent.Board.Add(opponent);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1441, SafetyLimit = 2 }));

            var finalKnight = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG25_008");
            Assert.AreEqual(attackBefore + 4, finalKnight.Attack);
            Assert.AreEqual(healthBefore + 2, finalKnight.MaxHealth);
        }

        [Test]
        public void MyrmidonSticker_DoublesEdgeNagasAtCombatStart()
        {
            var service = CreateService();
            Equip(service, MyrmidonStickerId, 1);
            var left = Minion("myrmidon-left", Tribe.Naga);
            var middle = Minion("myrmidon-middle", Tribe.Beast);
            var right = Minion("myrmidon-right", Tribe.Naga);
            left.Health = left.MaxHealth = left.BaseHealth = 20;
            middle.Health = middle.MaxHealth = middle.BaseHealth = 20;
            right.Health = right.MaxHealth = right.BaseHealth = 20;
            service.State.Player.Board.Add(left);
            service.State.Player.Board.Add(middle);
            service.State.Player.Board.Add(right);
            var opponent = Minion("myrmidon-opponent", Tribe.None);
            opponent.Owner = BoardSide.Opponent;
            opponent.Attack = opponent.BaseAttack = 0;
            opponent.Health = opponent.MaxHealth = opponent.BaseHealth = 100;
            service.State.Opponent.Board.Add(opponent);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1442, SafetyLimit = 1 }));

            var final = service.State.LastResult.FinalPlayerBoard;
            Assert.AreEqual(4, final.Single(card => card.InstanceId == left.InstanceId).Attack);
            Assert.AreEqual(40, final.Single(card => card.InstanceId == left.InstanceId).MaxHealth);
            Assert.AreEqual(4, final.Single(card => card.InstanceId == right.InstanceId).Attack);
            Assert.AreEqual(40, final.Single(card => card.InstanceId == right.InstanceId).MaxHealth);
            Assert.AreEqual(2, final.Single(card => card.InstanceId == middle.InstanceId).Attack);
        }

        [Test]
        public void LightfeatherSticker_GivesRallyMinionsDivineShieldAtCombatStart()
        {
            var service = CreateService();
            Equip(service, LightfeatherStickerId, 1);
            var rally = Minion("lightfeather-rally", Tribe.Dragon);
            rally.Keywords.Add(Keyword.Rally);
            rally.Health = rally.MaxHealth = rally.BaseHealth = 20;
            service.State.Player.Board.Add(rally);
            var opponent = Minion("lightfeather-opponent", Tribe.None);
            opponent.Owner = BoardSide.Opponent;
            opponent.Attack = opponent.BaseAttack = 0;
            opponent.Health = opponent.MaxHealth = opponent.BaseHealth = 100;
            service.State.Opponent.Board.Add(opponent);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1443, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Single().Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void AssemblerPortrait_MagnetizedDeathrattleSummonsAutomaton()
        {
            var service = CreateService();
            Equip(service, AssemblerPortraitId, 1);
            var mech = Minion("assembler-mech", Tribe.Mech);
            mech.Attack = mech.BaseAttack = 20;
            mech.Health = mech.MaxHealth = mech.BaseHealth = 1;
            service.State.Player.Board.Add(mech);
            var opponent = Minion("assembler-opponent", Tribe.None);
            opponent.Owner = BoardSide.Opponent;
            opponent.Attack = opponent.BaseAttack = 20;
            opponent.Health = opponent.MaxHealth = opponent.BaseHealth = 1;
            service.State.Opponent.Board.Add(opponent);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1444, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Any(card => card.Name == "Ancestral Automaton"));
        }

        private static MatchService CreateService()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var activeTribes = Enum.GetValues(typeof(Tribe))
                .Cast<Tribe>()
                .Where(tribe => tribe != Tribe.None && tribe != Tribe.All)
                .ToList();
            var service = MatchService.CreateWithResolvedVersion(
                resolved,
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    ActiveTribes = activeTribes,
                    EnableQuests = false,
                    EnableTrinkets = true,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                });
            service.State.Phase = MatchPhase.Tavern;
            service.State.ChoiceQueue = new ChoiceQueueState();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Gold = 20;
            return service;
        }

        private static void Equip(MatchService service, string cardId, int slot)
        {
            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, cardId, CardKind.Trinket, slot));
        }

        private static MinionInstance Minion(string instanceId, Tribe tribe)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                Cost = 3,
                BaseAttack = 2,
                BaseHealth = 3,
                Attack = 2,
                Health = 3,
                MaxHealth = 3,
                TavernTier = 1,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>(),
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
        }
    }
}
