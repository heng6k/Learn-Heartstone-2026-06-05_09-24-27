using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class FishbaitRecruitAttackTests
    {
        [Test]
        public void ReplaceAndAttack_ReplacesSelectedShopCardUsesLeftmostBeastAndRewardsKiller()
        {
            var state = CreateState();
            state.Player.Board.Add(Minion("non-beast", "TEST_NON_BEAST", 20, 20, Tribe.Pirate));
            var leftmostBeast = Minion("leftmost-beast", "TEST_LEFTMOST_BEAST", 2, 3, Tribe.Beast);
            state.Player.Board.Add(leftmostBeast);
            state.Player.Board.Add(Minion("right-beast", "TEST_RIGHT_BEAST", 9, 9, Tribe.Beast));
            state.Player.Tavern.Shop.Add(Minion("untouched-shop", "TEST_SHOP", 1, 2, Tribe.None));
            state.Player.Tavern.Shop.Add(Minion("replace-me", "TEST_REPLACE", 4, 4, Tribe.None));

            var result = FishbaitRecruitAttackService.ReplaceAndAttack(
                state,
                "replace-me",
                Fishbait("fishbait-1"),
                54321);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.IsTrue(result.TargetDied);
            Assert.AreEqual(leftmostBeast.InstanceId, result.AttackerInstanceId);
            var liveAttacker = state.Player.Board.Single(item => item.InstanceId == leftmostBeast.InstanceId);
            Assert.AreEqual(7, liveAttacker.Attack);
            Assert.AreEqual(8, liveAttacker.Health);
            Assert.AreEqual(8, liveAttacker.MaxHealth);
            CollectionAssert.AreEqual(new[] { "untouched-shop" }, state.Player.Tavern.Shop.ConvertAll(item => item.InstanceId));
            CollectionAssert.AreEqual(
                new[] { "fishbait.replaced", "recruit-attack.resolved", "fishbait.reward.resolved" },
                state.MechanicEvents.ConvertAll(item => item.Type));
        }

        [Test]
        public void RefreshAndAttack_RefreshesBeforeImmediateLeftmostBeastAttack()
        {
            var state = CreateState();
            var attacker = Minion("leftmost-beast", "TEST_LEFTMOST_BEAST", 2, 3, Tribe.Beast);
            state.Player.Board.Add(attacker);
            state.Player.Tavern.Shop.Add(Minion("old-shop", "TEST_OLD_SHOP", 1, 1, Tribe.None));
            var refreshCalls = 0;

            var result = FishbaitRecruitAttackService.RefreshAndAttack(
                state,
                live =>
                {
                    refreshCalls += 1;
                    Assert.AreEqual(0, live.MechanicEvents.Count);
                    live.Player.Tavern.Shop.Clear();
                    var fishbait = Fishbait("refreshed-fishbait");
                    live.Player.Tavern.Shop.Add(fishbait);
                    return fishbait;
                },
                54321,
                "snarky-shark-1");

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(1, refreshCalls);
            var liveAttacker = state.Player.Board.Single(item => item.InstanceId == attacker.InstanceId);
            Assert.AreEqual(7, liveAttacker.Attack);
            Assert.AreEqual(8, liveAttacker.Health);
            CollectionAssert.AreEqual(
                new[] { "fishbait.refreshed", "recruit-attack.resolved", "fishbait.reward.resolved" },
                state.MechanicEvents.ConvertAll(item => item.Type));
        }

        [Test]
        public void Season14Venomous_DoesNotTriggerDuringRecruitAttackWhileLegacyBehaviorRemainsAvailable()
        {
            var season14 = CreateState();
            var season14Attacker = Minion("season14-venom", "TEST_VENOM", 1, 6, Tribe.Beast, Keyword.Venomous);
            season14.Player.Board.Add(season14Attacker);
            season14.Player.Tavern.Shop.Add(Minion("season14-target", "TEST_TARGET", 0, 4, Tribe.None));

            var current = CombatEngine.ResolveRecruitPhaseAttack(
                season14,
                Attack("season14-venom", "season14-target"),
                54321,
                venomousEffectRevision: VenomousEffectRevisions.PerCombat);

            Assert.IsTrue(current.Succeeded, current.Message);
            Assert.IsFalse(current.TargetDied);
            Assert.IsTrue(season14.Player.Board.Single().Keywords.Contains(Keyword.Venomous));

            var legacy = CreateState();
            var legacyAttacker = Minion("legacy-venom", "TEST_VENOM", 1, 6, Tribe.Beast, Keyword.Venomous);
            legacy.Player.Board.Add(legacyAttacker);
            legacy.Player.Tavern.Shop.Add(Minion("legacy-target", "TEST_TARGET", 0, 4, Tribe.None));

            var old = CombatEngine.ResolveRecruitPhaseAttack(
                legacy,
                Attack("legacy-venom", "legacy-target"),
                54321);

            Assert.IsTrue(old.Succeeded, old.Message);
            Assert.IsTrue(old.TargetDied);
            Assert.IsFalse(legacy.Player.Board.Single().Keywords.Contains(Keyword.Venomous));
        }

        [Test]
        public void BuiltInRulesets_UseVersionedVenomousEffectRevision()
        {
            var resolver = GameVersionResolver.CreateBuiltIn();
            var preview = resolver.Resolve(GameVersionIds.Season14Preview, EmbeddedSnapshot());
            var legacy = resolver.Resolve(GameVersionIds.LegacyCompositeSandbox, EmbeddedSnapshot());

            Assert.AreEqual(VenomousEffectRevisions.PerCombat, preview.Ruleset.VenomousEffectRevision);
            Assert.AreEqual(VenomousEffectRevisions.LegacySingleUse, legacy.Ruleset.VenomousEffectRevision);
        }

        private static RecruitPhaseAttackContext Attack(string attackerId, string targetId)
        {
            return new RecruitPhaseAttackContext
            {
                AttackerInstanceId = attackerId,
                TavernTargetInstanceId = targetId,
                DamageContext = "fishbait-damage",
                DeathContext = "fishbait-death",
                RewardSource = "fishbait-test",
                Sequence = 1
            };
        }

        private static MatchState CreateState()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Phase = MatchPhase.Tavern;
            state.Player.Board.Clear();
            state.Player.Tavern.Shop.Clear();
            state.MechanicEvents.Clear();
            state.LastReplay = null;
            return state;
        }

        private static MinionInstance Fishbait(string instanceId, int health = 1)
        {
            var fishbait = Minion(instanceId, FishbaitRecruitAttackService.FishbaitCardId, 0, health, Tribe.Beast, Keyword.Deathrattle);
            fishbait.BaseAttack = 0;
            fishbait.BaseHealth = 1;
            return fishbait;
        }

        private static MinionInstance Minion(
            string instanceId,
            string cardId,
            int attack,
            int health,
            Tribe tribe,
            params Keyword[] keywords)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = instanceId,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Owner = BoardSide.Player,
                CanAttack = true,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(keywords ?? Array.Empty<Keyword>()),
                OfficialKeywords = new List<Keyword>(keywords ?? Array.Empty<Keyword>()),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>()
            };
        }

        private static GameCatalogSnapshot EmbeddedSnapshot()
        {
            var baseline = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).Catalogs;
            return new GameCatalogSnapshot(
                new ContentSnapshotInfo(
                    "m5-test-snapshot",
                    "m5-test-fingerprint",
                    ContentSnapshotSource.Embedded,
                    "test",
                    DateTime.UnixEpoch,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty),
                baseline,
                baseline);
        }
    }
}
