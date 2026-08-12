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
    public sealed class Season14TrinketFinalBehaviorTests
    {
        private const string ConsumingClawId = "BG36_MagicItem_801";
        private const string RuneOfTransmutationId = "BG36_MagicItem_305";
        private const string OminousStoneId = "BG36_MagicItem_206";
        private const string LovelyLocketId = "BG36_MagicItem_211";
        private const string HoneycombRingId = "BG36_MagicItem_371";
        private const string MultilayeredShieldId = "BG36_MagicItem_373";
        private const string MaldraxxusDaggerId = "BG36_MagicItem_370";
        private const string BloodfuryShieldId = "BG36_MagicItem_830";
        private const string FelsteelCleaverId = "BG36_MagicItem_831";
        private const string AmplifyingEssenceId = "BG36_MagicItem_380";
        private const string TrailblazerStickerId = "BG36_MagicItem_308";
        private const string GlassOfPerspectiveId = "BG36_MagicItem_303";
        private const string HerdingHornId = "BG36_MagicItem_200";
        private const string LionfishPortraitId = "BG36_MagicItem_201";
        private const string DeathwhisperStickerId = "BG36_MagicItem_205";
        private const string FloatingCandleSetId = "BG36_MagicItem_208";
        private const string WolfheadFlailId = "BG36_MagicItem_212";
        private const string InsurrectionistsBladeId = "BG36_MagicItem_214";
        private const string FuneralWreathId = "BG36_MagicItem_217";
        private const string CyclistPortraitId = "BG36_MagicItem_362";
        private const string TargetedTavernSpellId = "100596";
        private const string UntargetedTavernSpellId = "122186";

        [Test]
        public void HoneycombRing_TargetedSpellsImproveUntilTurnEnds()
        {
            var service = CreateService();
            var target = Minion("honey-target", Tribe.Beast);
            service.State.Player.Board.Add(target);
            Equip(service, HoneycombRingId, 1);
            var health = target.MaxHealth;

            Cast(service, TargetedTavernSpellId, 0);
            Cast(service, TargetedTavernSpellId, 0);

            Assert.AreEqual(health + 3, target.MaxHealth);
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            var resetHealth = target.MaxHealth;
            Cast(service, TargetedTavernSpellId, 0);
            Assert.AreEqual(resetHealth + 1, target.MaxHealth);
        }

        [Test]
        public void MultilayeredShield_ScalesWithDistinctFriendlyTypes()
        {
            var service = CreateService();
            var target = Minion("shield-target", Tribe.Beast);
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(Minion("shield-dragon", Tribe.Dragon));
            service.State.Player.Board.Add(Minion("shield-mech", Tribe.Mech));
            Equip(service, MultilayeredShieldId, 1);
            var health = target.MaxHealth;

            Cast(service, TargetedTavernSpellId, 0);

            Assert.AreEqual(health + 6, target.MaxHealth);
        }

        [Test]
        public void BloodfuryShield_EachTavernSpellQueuesOneFodderRefresh()
        {
            var service = CreateService();
            Equip(service, BloodfuryShieldId, 1);

            Cast(service, UntargetedTavernSpellId, -1);
            Cast(service, UntargetedTavernSpellId, -1);

            Assert.AreEqual(2, service.State.Player.Tavern.DemonFodderRefreshes);
        }

        [Test]
        public void FelsteelCleaver_TargetedTavernMinionIsConsumedByFriendlyMinion()
        {
            var service = CreateService();
            var eater = Minion("cleaver-eater", Tribe.Pirate);
            var consumed = Minion("cleaver-shop", Tribe.Beast);
            service.State.Player.Board.Add(eater);
            service.State.Player.Tavern.Shop.Add(consumed);
            Equip(service, FelsteelCleaverId, 1);
            var attack = eater.Attack;

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, TargetedTavernSpellId, CardKind.TavernSpell));
            var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == TargetedTavernSpellId);
            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                spellIndex,
                0,
                TargetZone.TavernShop,
                -1,
                TargetZone.Unspecified,
                consumed.InstanceId));

            Assert.IsNull(service.State.Player.Tavern.Shop[0]);
            Assert.Greater(eater.Attack, attack);
        }

        [Test]
        public void LovelyLocket_CopiesTargetedSpellOntoAnotherFriendlyMinionOnce()
        {
            var service = CreateService();
            var first = Minion("locket-first", Tribe.Beast);
            var second = Minion("locket-second", Tribe.Dragon);
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);
            Equip(service, LovelyLocketId, 0);
            var attack = first.Attack + second.Attack;

            Cast(service, TargetedTavernSpellId, 0);

            Assert.AreEqual(attack + 8, first.Attack + second.Attack);
        }

        [Test]
        public void RuneOfTransmutation_FifteenthSpellReplacesLesserSlotWithGreaterNagaTrinket()
        {
            var service = CreateService();
            Equip(service, RuneOfTransmutationId, 0);

            for (var cast = 0; cast < 15; cast += 1)
            {
                Cast(service, UntargetedTavernSpellId, -1);
            }

            Assert.AreNotEqual(RuneOfTransmutationId, service.State.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId);
            Assert.IsTrue(service.State.Player.Tavern.AdvancedMechanics.Trinkets.Equipped.Any(item =>
                item.SlotKind == TrinketSlotKind.Lesser && item.TrinketId != RuneOfTransmutationId));
        }

        [Test]
        public void ConsumingClaw_DemonAlsoGainsConsumedBonusKeywords()
        {
            var service = CreateService();
            var eater = Minion("claw-eater", Tribe.Demon);
            var consumed = Minion("claw-consumed", Tribe.Beast);
            consumed.Keywords.Add(Keyword.DivineShield);
            consumed.OfficialKeywords.Add(Keyword.DivineShield);
            var mindMuck = Minion("claw-mind-muck", Tribe.Demon);
            mindMuck.CardId = "BG23_357";
            mindMuck.Keywords.Add(Keyword.Battlecry);
            mindMuck.Keywords.Add(Keyword.Devour);
            service.State.Player.Board.Add(eater);
            service.State.Player.Tavern.Shop.Add(consumed);
            service.State.Player.Tavern.Hand.Add(mindMuck);
            Equip(service, ConsumingClawId, 0);

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                eater.InstanceId));

            Assert.Contains(Keyword.DivineShield, eater.Keywords);
        }

        [Test]
        public void TrailblazerSticker_MarksGeneratedChooseOneCardsForBothEffects()
        {
            var service = CreateService();
            Equip(service, TrailblazerStickerId, 1);
            Equip(service, GlassOfPerspectiveId, 0);

            Assert.IsTrue(service.State.Player.Tavern.Hand
                .Where(card => card.Keywords.Contains(Keyword.ChooseOne))
                .All(card => card.Tags.Contains("choose_one_both_effects")));
        }

        [Test]
        public void AmplifyingEssence_ElementalBuffGetsExtraStatsAndImprovesAfterFivePlays()
        {
            var service = CreateService();
            var shop = Minion("essence-shop", Tribe.Beast);
            service.State.Player.Tavern.Shop.Add(shop);
            Equip(service, AmplifyingEssenceId, 1);
            var attack = shop.Attack;
            for (var index = 0; index < 5; index += 1)
            {
                var elemental = Minion("essence-elemental-" + index, Tribe.Elemental);
                elemental.CardId = index == 0 ? "BG25_041" : "essence-vanilla-" + index;
                if (index == 0)
                {
                    elemental.Keywords.Add(Keyword.Battlecry);
                }
                service.State.Player.Tavern.Hand.Add(elemental);
                service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, -1));
            }

            Assert.GreaterOrEqual(shop.Attack, attack + 3);
            Assert.AreEqual(2, service.State.Player.Tavern.AdvancedMechanics.Counters["season14_trinket_amplifying_essence_bonus"]);
        }

        [Test]
        public void OminousStone_QueuesTierFourDarkGiftChoice()
        {
            var service = CreateService();
            service.State.Player.Board.Add(Minion("ominous-one", Tribe.Beast));
            service.State.Player.Board.Add(Minion("ominous-two", Tribe.Beast));

            Equip(service, OminousStoneId, 0);

            var choice = service.State.ChoiceQueue.ActiveChoice ?? service.State.ChoiceQueue.PendingChoices.FirstOrDefault();
            Assert.IsNotNull(choice);
            Assert.AreEqual(ChoiceRequestKind.DarkGift, choice.Kind);
            Assert.AreEqual("trinket:ominous-stone", choice.Source);
            Assert.IsTrue(choice.Options.All(option => option.DifficultyTier == 4));
        }

        [Test]
        public void MaldraxxusDagger_DiscoversPlainCopyOfGiftedWarbandMinionOnEquip()
        {
            var service = CreateService();
            var gifted = Minion("maldraxxus-gifted", Tribe.Undead);
            gifted.CardId = "BG28_300";
            service.State.Player.Board.Add(gifted);
            service.State.PlayerDarkGifts.AcquiredGiftInstances.Add(new PlayerDarkGiftInstance
            {
                InstanceId = gifted.InstanceId,
                DefinitionRevisionId = "dark-gift.attack@36.2-preview-v1",
                Active = true
            });

            Equip(service, MaldraxxusDaggerId, 1);

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(option => option.CardId == "BG28_300"));
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(option => option.PoolSource == PoolSource.Copy));
        }

        [Test]
        public void HerdingHorn_RallyAttackerGrantsFreeRefresh()
        {
            var service = CreateService();
            Equip(service, HerdingHornId, 0);
            var rally = Minion("horn-rally", Tribe.Dragon);
            rally.Keywords.Add(Keyword.Rally);
            rally.Attack = rally.BaseAttack = 4;
            rally.Health = rally.MaxHealth = rally.BaseHealth = 20;
            service.State.Player.Board.Add(rally);
            service.State.Player.Board.Add(Minion("horn-wingman", Tribe.Beast));
            service.State.Opponent.Board.Add(Opponent("horn-opponent", 0, 100));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 2501, SafetyLimit = 1 }));

            Assert.GreaterOrEqual(service.State.Player.Tavern.FreeRefreshes, 1);
        }

        [Test]
        public void LionfishPortrait_BeastAttackerGainsTwoTwo()
        {
            var service = CreateService();
            Equip(service, LionfishPortraitId, 0);
            var beast = Minion("lionfish-beast", Tribe.Beast);
            beast.Health = beast.MaxHealth = beast.BaseHealth = 20;
            service.State.Player.Board.Add(beast);
            service.State.Player.Board.Add(Minion("lionfish-wingman", Tribe.Pirate));
            service.State.Opponent.Board.Add(Opponent("lionfish-opponent", 0, 100));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 2502, SafetyLimit = 1 }));

            var final = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == beast.InstanceId);
            Assert.AreEqual(4, final.Attack);
            Assert.AreEqual(22, final.MaxHealth);
        }

        [Test]
        public void DeathwhisperSticker_RebornBuffsFriendlyWarband()
        {
            var service = CreateService();
            Equip(service, DeathwhisperStickerId, 0);
            var reborn = Minion("deathwhisper-reborn", Tribe.Undead);
            reborn.Keywords.Add(Keyword.Reborn);
            reborn.Health = reborn.MaxHealth = reborn.BaseHealth = 1;
            var ally = Minion("deathwhisper-ally", Tribe.Beast);
            ally.Health = ally.MaxHealth = ally.BaseHealth = 20;
            service.State.Player.Board.Add(reborn);
            service.State.Player.Board.Add(ally);
            service.State.Opponent.Board.Add(Opponent("deathwhisper-opponent", 20, 100));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 2503, SafetyLimit = 1 }));

            var finalAlly = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == ally.InstanceId);
            Assert.AreEqual(4, finalAlly.Attack);
            Assert.AreEqual(22, finalAlly.MaxHealth);
        }

        [Test]
        public void FloatingCandleSet_TriggersFriendlyDeathrattleDuringRecruit()
        {
            var service = CreateService();
            var bonehead = Minion("floating-bonehead", Tribe.Undead);
            bonehead.CardId = "BG28_300";
            bonehead.Keywords.Add(Keyword.Deathrattle);
            service.State.Player.Board.Add(bonehead);
            Equip(service, FloatingCandleSetId, 0);
            var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG36_MagicItem_208t");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, 0));

            Assert.Greater(service.State.Player.Board.Count, 1);
        }

        [Test]
        public void WolfheadFlail_TriggersAllFriendlyDeathrattlesAtTurnEnd()
        {
            var service = CreateService();
            var bonehead = Minion("wolfhead-bonehead", Tribe.Undead);
            bonehead.CardId = "BG28_300";
            bonehead.Keywords.Add(Keyword.Deathrattle);
            service.State.Player.Board.Add(bonehead);
            Equip(service, WolfheadFlailId, 1);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.Greater(service.State.Player.Board.Count, 1);
        }

        [Test]
        public void InsurrectionistsBlade_TriggersAllFriendlyRalliesAtTurnEnd()
        {
            var service = CreateService();
            var glim = Minion("blade-glim", Tribe.Dragon);
            glim.CardId = "BG29_888";
            glim.Keywords.Add(Keyword.Rally);
            service.State.Player.Board.Add(glim);
            Equip(service, InsurrectionistsBladeId, 1);
            var attack = glim.Attack;

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(attack + 2, glim.Attack);
        }

        [Test]
        public void CyclistPortrait_ShowyCyclistDeathrattleStatsPersistAfterCombat()
        {
            var service = CreateService();
            Equip(service, CyclistPortraitId, 1);
            var naga = Minion("cyclist-naga", Tribe.Naga);
            naga.Health = naga.MaxHealth = naga.BaseHealth = 30;
            service.State.Player.Board.Add(naga);
            var cyclistIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG31_925");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, cyclistIndex, 0));
            var cyclist = service.State.Player.Board[0];
            cyclist.Health = cyclist.MaxHealth = cyclist.BaseHealth = 1;
            service.State.Opponent.Board.Add(Opponent("cyclist-opponent", 20, 100));
            var attack = naga.Attack;

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 2504, SafetyLimit = 1 }));

            var persistent = service.State.Player.Board.Single(card => card.InstanceId == naga.InstanceId);
            Assert.Greater(persistent.Attack, attack);
        }

        [Test]
        public void FuneralWreath_RebornAddsPlainCopyToHand()
        {
            var service = CreateService();
            Equip(service, FuneralWreathId, 1);
            var reborn = Minion("wreath-reborn", Tribe.Undead);
            reborn.CardId = "BG28_300";
            reborn.Keywords.Add(Keyword.Reborn);
            reborn.Health = reborn.MaxHealth = reborn.BaseHealth = 1;
            service.State.Player.Board.Add(reborn);
            service.State.Player.Board.Add(Minion("wreath-wingman", Tribe.Beast));
            service.State.Opponent.Board.Add(Opponent("wreath-opponent", 20, 100));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 2505, SafetyLimit = 1 }));

            var copy = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG28_300");
            Assert.IsFalse(copy.Golden);
            Assert.AreEqual(PoolSource.Copy, copy.PoolSource);
        }

        private static MatchService CreateService()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var activeTribes = Enum.GetValues(typeof(Tribe)).Cast<Tribe>()
                .Where(tribe => tribe != Tribe.None && tribe != Tribe.All)
                .ToList();
            var service = MatchService.CreateWithResolvedVersion(
                resolved,
                23456,
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

        private static void Cast(MatchService service, string cardId, int targetIndex)
        {
            service.Apply(new GameCommand(GameCommandType.DebugCastCard, cardId, CardKind.TavernSpell, targetIndex));
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

        private static MinionInstance Opponent(string instanceId, int attack, int health)
        {
            var minion = Minion(instanceId, Tribe.None);
            minion.Owner = BoardSide.Opponent;
            minion.Attack = minion.BaseAttack = attack;
            minion.Health = minion.MaxHealth = minion.BaseHealth = health;
            return minion;
        }
    }
}
