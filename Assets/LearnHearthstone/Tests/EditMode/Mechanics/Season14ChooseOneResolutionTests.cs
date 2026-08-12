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
    public sealed class Season14ChooseOneResolutionTests
    {
        private const string AllianceFlagCardId = "117567";
        private const string FandralFortuneCardId = "116221";
        private const string TrailblazerStickerId = "BG36_MagicItem_308";
        private const string AmplifyingEssenceId = "BG36_MagicItem_380";
        private const string BothEffectsTag = "choose_one_both_effects";
        private const string BothEffectsCapabilityCounter = "choose-one-both-effects";

        [Test]
        public void TrailblazerSticker_AbilityResolvesBothSpellBranchesWithoutCachedTag()
        {
            var service = CreateService();
            Equip(service, TrailblazerStickerId, 1);
            var target = AddAllianceTarget(service, "sticker-target");
            var spell = AddSpell(service, AllianceFlagCardId);
            spell.Tags.Remove(BothEffectsTag);
            spell.Counters.Remove(BothEffectsCapabilityCounter);
            service.State.MechanicEvents.Clear();
            var castsBefore = service.State.Player.Tavern.TavernSpellsCastThisTurn;

            PlaySpell(service, target, "attack");

            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(5, target.MaxHealth);
            Assert.AreEqual(castsBefore + 1, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.IsEmpty(service.State.Player.Tavern.Hand, "The spell must be consumed exactly once.");
            AssertStableBothEffectsTrace(service, spell.InstanceId, target.InstanceId);
        }

        [Test]
        public void WithoutBothEffectsAbility_OnlySelectedSpellBranchResolves()
        {
            var service = CreateService();
            var target = AddAllianceTarget(service, "single-target");
            var spell = AddSpell(service, AllianceFlagCardId);
            spell.Tags.Remove(BothEffectsTag);
            spell.Counters.Remove(BothEffectsCapabilityCounter);
            service.State.MechanicEvents.Clear();

            PlaySpell(service, target, "attack");

            Assert.AreEqual(4, target.Attack);
            Assert.AreEqual(2, target.MaxHealth);
            Assert.IsFalse(service.State.MechanicEvents.Any(item => item.Type.StartsWith("choose-one.", StringComparison.Ordinal)));
        }

        [Test]
        public void ReplacingTrailblazerSticker_RemovesDerivedAbilityAndReturnsCardToSingleChoice()
        {
            var service = CreateService();
            Equip(service, TrailblazerStickerId, 1);
            var target = AddAllianceTarget(service, "removed-sticker-target");
            var spell = AddSpell(service, AllianceFlagCardId);
            Assert.Contains(BothEffectsTag, spell.Tags);
            Assert.IsFalse(spell.Counters.ContainsKey(BothEffectsCapabilityCounter), "A global trinket must not become a permanent per-card capability.");

            Equip(service, AmplifyingEssenceId, 1);
            Assert.IsFalse(spell.Tags.Contains(BothEffectsTag));
            service.State.MechanicEvents.Clear();
            PlaySpell(service, target, "attack");

            Assert.AreEqual(4, target.Attack);
            Assert.AreEqual(2, target.MaxHealth);
            Assert.IsFalse(service.State.MechanicEvents.Any(item => item.Type.StartsWith("choose-one.", StringComparison.Ordinal)));
        }

        [Test]
        public void FandralsFortune_GrantsPersistentBothEffectsCapabilityToDiscoveredCardAndCopy()
        {
            var service = CreateService();
            AddSpell(service, FandralFortuneCardId);

            PlaySpell(service, null, null);
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            var granted = service.State.Player.Tavern.Hand.Single();
            Assert.AreEqual(1, granted.Counters[BothEffectsCapabilityCounter]);
            Assert.Contains(BothEffectsTag, granted.Tags, "The display/compatibility tag is derived from the capability.");
            var copy = granted.Clone();
            copy.Tags.Remove(BothEffectsTag);
            Assert.AreEqual(1, copy.Counters[BothEffectsCapabilityCounter], "Copies must keep the behavioral capability even without the display tag.");
        }

        [Test]
        public void CurrentVersionChooseOnePool_UsesVersionMembershipForMinionsAndSpells()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var service = CreateService();
            var availability = new CardPoolAvailability(
                new CardPoolVersionSelection { IsDefault = true },
                snapshot.VersionedContent.ContentSets.Single(item => item.Id == ContentSetIds.Season14Preview));

            var pool = ChooseOneCardPool.Create(service.Catalogs.Minions, service.Catalogs.Spells, availability);

            Assert.IsTrue(pool.Minions.Any(item => item.CardId == "BG27_084"), "Sprightly Scarab is the targeting interaction baseline.");
            Assert.IsTrue(pool.Minions.All(availability.AllowsMinion));
            Assert.IsTrue(pool.TavernSpells.All(availability.AllowsTavernSpell));
            Assert.IsTrue(pool.TavernSpells.All(ChooseOneCardPool.IsChooseOneSpell));
        }

        [Test]
        public void EffectChooseOneSpells_HaveCentralizedDistinctOptionsAndTargetRules()
        {
            var expected = new[]
            {
                "115910",
                "117567",
                "117573",
                "117584",
                "VOLCANIC_VISITOR_CHOICE_SPELL"
            };

            CollectionAssert.AreEquivalent(expected, ChooseOneOptionRegistry.RegisteredCardIds);
            foreach (var cardId in expected)
            {
                Assert.IsTrue(ChooseOneOptionRegistry.TryGetOptions(cardId, out var options), cardId);
                Assert.AreEqual(2, options.Count, cardId);
                Assert.AreEqual(2, options.Select(option => option.ChoiceId).Distinct().Count(), cardId);
                Assert.IsTrue(options.All(option =>
                    !string.IsNullOrWhiteSpace(option.Name) &&
                    !string.IsNullOrWhiteSpace(option.Text)), cardId);
            }

            Assert.IsTrue(ChooseOneOptionRegistry.TryGetOption("117584", "target", out var targetOption));
            Assert.IsTrue(targetOption.RequiresPlayerTarget);
            Assert.IsTrue(ChooseOneOptionRegistry.TryGetOption("117584", "board", out var boardOption));
            Assert.IsFalse(boardOption.RequiresPlayerTarget);
        }

        [Test]
        public void IntrepidBotanist_RightChoiceImprovesOnlyTavernSpellHealth()
        {
            var service = CreateService();
            var botanist = MinionFactory.Create(
                service.Catalogs.Minions.All.Single(item => item.CardId == "BG32_237"),
                BoardSide.Player,
                "intrepid-botanist",
                false,
                PoolSource.Copy,
                0);
            service.State.Player.Tavern.Hand.Add(botanist);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(2, service.State.Player.Tavern.Discover.Options.Count);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Source.StartsWith("intrepid-botanist:", StringComparison.Ordinal));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 1));

            Assert.AreEqual(0, service.State.Player.Tavern.TavernSpellBonusAttack);
            Assert.AreEqual(1, service.State.Player.Tavern.TavernSpellBonusHealth);
        }

        [Test]
        public void CardCapability_ScenarioRoundTripResolvesBothSpellBranchesWithoutTag()
        {
            var source = CreateService();
            var target = AddAllianceTarget(source, "scenario-target");
            var spell = AddSpell(source, AllianceFlagCardId);
            spell.Tags.Remove(BothEffectsTag);
            spell.Counters[BothEffectsCapabilityCounter] = 1;
            var scenario = TestScenarioMapper.Clone(TestScenarioMapper.Capture(source.State, "p1e-card-capability"));
            var restored = CreateService();

            TestScenarioMapper.ApplyTo(restored.State, scenario);
            restored.State.MechanicEvents.Clear();
            var restoredSpell = restored.State.Player.Tavern.Hand.Single();
            var restoredTarget = restored.State.Player.Board.Single(item => item.InstanceId == target.InstanceId);
            Assert.IsFalse(restoredSpell.Tags.Contains(BothEffectsTag));

            PlaySpell(restored, restoredTarget, "health");

            Assert.AreEqual(5, restoredTarget.Attack);
            Assert.AreEqual(5, restoredTarget.MaxHealth);
            AssertStableBothEffectsTrace(restored, restoredSpell.InstanceId, restoredTarget.InstanceId);
        }

        [Test]
        public void TrailblazerSticker_TransformedOrGeneratedChooseOneCardUsesBothOptionsAndLeavesNoChoice()
        {
            var service = CreateService();
            Equip(service, TrailblazerStickerId, 1);
            var crater = CreateCatalogMinion(service, "POOL-D10", "dynamic-crater");
            crater.Tags.Remove(BothEffectsTag);
            crater.Counters.Remove(BothEffectsCapabilityCounter);
            service.State.Player.Tavern.Hand.Add(crater);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var orderedOptionIds = service.State.Player.Tavern.Discover.Options.Select(option => option.CardId).ToArray();
            service.State.MechanicEvents.Clear();
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 1));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardId == "116596"));
            Assert.IsNull(service.State.Player.Tavern.Discover);
            Assert.IsNull(service.State.ChoiceQueue.ActiveChoice);
            AssertStableBothEffectsTrace(service, crater.InstanceId, null, orderedOptionIds);
        }

        [Test]
        public void ActiveBothEffectsDiscover_ScenarioRoundTripPreservesFlagAndStableOptionOrder()
        {
            var source = CreateService();
            var crater = CreateCatalogMinion(source, "POOL-D10", "saved-crater");
            crater.Counters[BothEffectsCapabilityCounter] = 1;
            crater.Tags.Remove(BothEffectsTag);
            source.State.Player.Tavern.Hand.Add(crater);
            source.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.IsTrue(source.State.Player.Tavern.Discover.ResolveAllOptions);
            var scenario = TestScenarioMapper.Clone(TestScenarioMapper.Capture(source.State, "p1e-active-choice"));
            var restored = CreateService();

            TestScenarioMapper.ApplyTo(restored.State, scenario);
            var discover = restored.State.Player.Tavern.Discover;
            Assert.IsTrue(discover.ResolveAllOptions);
            var orderedOptionIds = discover.Options.Select(option => option.CardId).ToArray();
            restored.State.MechanicEvents.Clear();
            restored.Apply(new GameCommand(GameCommandType.ChooseDiscover, 1));

            Assert.AreEqual(2, restored.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
            Assert.AreEqual(1, restored.State.Player.Tavern.Hand.Count(card => card.CardId == "116596"));
            Assert.IsNull(restored.State.Player.Tavern.Discover);
            Assert.IsNull(restored.State.ChoiceQueue.ActiveChoice);
            AssertStableBothEffectsTrace(restored, crater.InstanceId, null, orderedOptionIds);
        }

        private static void AssertStableBothEffectsTrace(
            MatchService service,
            string sourceInstanceId,
            string targetInstanceId,
            IReadOnlyList<string> expectedBranchResults = null)
        {
            var trace = service.State.MechanicEvents
                .Where(item => item.Type.StartsWith("choose-one.", StringComparison.Ordinal))
                .ToList();
            CollectionAssert.AreEqual(
                new[] { "choose-one.resolved", "choose-one.branch-resolved", "choose-one.branch-resolved" },
                trace.Select(item => item.Type).ToArray());
            Assert.AreEqual(1, trace.Select(item => item.RequestId).Distinct().Count());
            Assert.IsTrue(trace.All(item => item.Source == sourceInstanceId));
            if (!string.IsNullOrEmpty(targetInstanceId))
            {
                Assert.IsTrue(trace.All(item => item.Targets.Contains(targetInstanceId)));
            }

            if (expectedBranchResults == null)
            {
                CollectionAssert.AreEqual(new[] { "option:0", "option:1" }, trace.Skip(1).Select(item => item.Result).ToArray());
            }
            else
            {
                CollectionAssert.AreEqual(expectedBranchResults, trace.Skip(1).Select(item => item.Result).ToArray());
            }
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
                34567,
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

        private static MinionInstance AddSpell(MatchService service, string cardId)
        {
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, CardKind.TavernSpell));
            return service.State.Player.Tavern.Hand.Single();
        }

        private static void PlaySpell(MatchService service, MinionInstance target, string choiceId)
        {
            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                target == null ? -1 : 0,
                target == null ? TargetZone.Unspecified : TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target?.InstanceId,
                choiceId: choiceId));
        }

        private static MinionInstance AddAllianceTarget(MatchService service, string instanceId)
        {
            var target = new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                CardKind = CardKind.Minion,
                Owner = BoardSide.Player,
                BaseAttack = 1,
                BaseHealth = 1,
                Attack = 1,
                Health = 1,
                MaxHealth = 1,
                Tribes = new List<Tribe> { Tribe.Murloc }
            };
            service.State.Player.Board.Add(target);
            return target;
        }

        private static MinionInstance CreateCatalogMinion(MatchService service, string researchKey, string instanceId)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == researchKey);
            return MinionFactory.Create(definition, BoardSide.Player, instanceId, false, PoolSource.Copy, 0);
        }
    }
}
