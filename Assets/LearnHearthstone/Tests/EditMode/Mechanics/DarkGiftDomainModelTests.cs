using System.Collections.Generic;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class DarkGiftDomainModelTests
    {
        [Test]
        public void Definition_ClonePreservesRevisionPoliciesAndOwnsCollections()
        {
            var source = new DarkGiftDefinition
            {
                Id = "gift-test",
                RevisionId = "gift-test@36.2",
                EffectRevision = "gift-test-effect@1",
                DisplayName = "Test Gift",
                Text = "Test text",
                ImagePath = "images/gift-test.png",
                EarliestOfferRound = 3,
                LatestOfferRound = 9,
                TriggerSpec = "turn-start",
                ChoiceSpec = "friendly-minion",
                StackPolicy = "stack",
                DurationPolicy = "persistent",
                ImplementationStatus = DarkGiftImplementationStatus.FrameworkOnly,
                AvailabilityTags = new List<string> { "normal-pool" },
                CompatibilityTags = new List<string> { "non-magnetic" },
                RequiredMinionTags = new List<string> { "keyword:deathrattle" },
                ExcludedMinionTags = new List<string> { "keyword:taunt" },
                EffectIds = new List<string> { "effect-test@1" }
            };

            var clone = source.Clone();
            source.AvailabilityTags.Add("mutated");
            source.CompatibilityTags.Clear();
            source.RequiredMinionTags.Clear();
            source.ExcludedMinionTags.Clear();
            source.EffectIds[0] = "changed";

            Assert.AreEqual("gift-test@36.2", clone.RevisionId);
            Assert.AreEqual("gift-test-effect@1", clone.EffectRevision);
            Assert.AreEqual(3, clone.EarliestOfferRound);
            Assert.AreEqual(9, clone.LatestOfferRound);
            CollectionAssert.AreEqual(new[] { "normal-pool" }, clone.AvailabilityTags);
            CollectionAssert.AreEqual(new[] { "non-magnetic" }, clone.CompatibilityTags);
            CollectionAssert.AreEqual(new[] { "keyword:deathrattle" }, clone.RequiredMinionTags);
            CollectionAssert.AreEqual(new[] { "keyword:taunt" }, clone.ExcludedMinionTags);
            CollectionAssert.AreEqual(new[] { "effect-test@1" }, clone.EffectIds);
        }

        [Test]
        public void Ruleset_DarkGiftProfileCoversSeasonRulesAndIsDeepCopied()
        {
            var profile = new DarkGiftProfile
            {
                Id = "dark-gift-36.2-test",
                Enabled = true,
                NormalEntryStartRound = 3,
                GoldCost = 3,
                UsesPerTurn = 1,
                UsesPerGame = 3,
                OfferCount = 3,
                PickCount = 1,
                DeduplicationPolicy = "distinct-gift-definitions",
                ChoiceQueuePriority = 120,
                ImplementationStatus = DarkGiftImplementationStatus.BlockedByOfficialFact,
                TierRanges = new List<DarkGiftTierRangeRule>
                {
                    new DarkGiftTierRangeRule { FromRound = 3, MinTier = 2, MaxTier = 2 },
                    new DarkGiftTierRangeRule { FromRound = 4, MinTier = 2, MaxTier = 3 }
                },
                CandidateFilter = new DarkGiftCandidateFilter
                {
                    BattlecryAllowedFromRound = 5,
                    ChooseOneAllowedFromRound = 5,
                    ExcludedMechanics = new List<string> { "magnetic", "sell-trigger", "hand-only" }
                },
                CommonTribeGuarantee = new DarkGiftCommonTribeGuarantee
                {
                    Enabled = true,
                    StartRound = 6,
                    MinimumOfferCount = 1
                }
            };
            var ruleset = new RulesetDefinition(
                "ruleset-dark-gift-test",
                1,
                darkGiftProfile: profile);

            profile.TierRanges[0].MinTier = 6;
            profile.CandidateFilter.ExcludedMechanics.Clear();
            profile.CommonTribeGuarantee.MinimumOfferCount = 3;
            var stored = ruleset.DarkGiftProfile;

            Assert.IsTrue(stored.Enabled);
            Assert.AreEqual(3, stored.NormalEntryStartRound);
            Assert.AreEqual(3, stored.GoldCost);
            Assert.AreEqual(1, stored.UsesPerTurn);
            Assert.AreEqual(3, stored.UsesPerGame);
            Assert.AreEqual(3, stored.OfferCount);
            Assert.AreEqual(1, stored.PickCount);
            Assert.AreEqual(2, stored.TierRanges[0].MinTier);
            CollectionAssert.AreEqual(
                new[] { "magnetic", "sell-trigger", "hand-only" },
                stored.CandidateFilter.ExcludedMechanics);
            Assert.AreEqual(1, stored.CommonTribeGuarantee.MinimumOfferCount);
            Assert.AreEqual(DarkGiftImplementationStatus.BlockedByOfficialFact, stored.ImplementationStatus);

            stored.TierRanges[0].MinTier = 5;
            Assert.AreEqual(2, ruleset.DarkGiftProfile.TierRanges[0].MinTier);
        }

        [Test]
        public void BuiltInPreviewRuleset_ContainsConfirmedProfileAndKeepsPriorityBlocked()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = GameVersionResolver.CreateBuiltIn().Resolve(GameVersionIds.Season14Preview, snapshot);
            var profile = resolved.Ruleset.DarkGiftProfile;

            Assert.NotNull(profile);
            Assert.IsTrue(profile.Enabled);
            Assert.AreEqual(3, profile.NormalEntryStartRound);
            Assert.AreEqual(3, profile.GoldCost);
            Assert.AreEqual(1, profile.UsesPerTurn);
            Assert.AreEqual(3, profile.UsesPerGame);
            Assert.AreEqual(3, profile.OfferCount);
            Assert.AreEqual(1, profile.PickCount);
            Assert.AreEqual(10, profile.TierRanges.Count);
            Assert.AreEqual(2, profile.TierRanges[0].MinTier);
            Assert.AreEqual(6, profile.TierRanges[7].MaxTier);
            Assert.AreEqual(11, profile.TierRanges[8].FromRound);
            Assert.AreEqual(5, profile.TierRanges[8].MinTier);
            Assert.AreEqual(6, profile.TierRanges[8].MaxTier);
            Assert.AreEqual(12, profile.TierRanges[9].FromRound);
            Assert.AreEqual(6, profile.TierRanges[9].MinTier);
            Assert.AreEqual(6, profile.TierRanges[9].MaxTier);
            Assert.AreEqual(3, profile.CommonTribeGuarantee.StartRound);
            Assert.AreEqual(DarkGiftOfficialFactStatus.BlockedByOfficialFact, profile.ChoiceQueuePriorityFactStatus);
            Assert.AreEqual(DarkGiftImplementationStatus.Implemented, profile.ImplementationStatus);
            Assert.AreEqual(DarkGiftAutoChoicePolicy.PlayerChoice, profile.AutoChoicePolicy);
        }

        [Test]
        public void PlayerState_CloneOwnsInstancesCountersCooldownsAndHistory()
        {
            var source = CreatePlayerState();

            var clone = source.Clone();
            source.AcquiredGiftInstances[0].StackCount = 9;
            source.Counters["normal-uses"] = 3;
            source.Cooldowns["gift-test"] = 0;
            source.TriggerHistory.Events[0].Targets.Add("mutated");

            Assert.AreEqual(2, clone.AcquiredGiftInstances[0].StackCount);
            Assert.AreEqual(1, clone.Counters["normal-uses"]);
            Assert.AreEqual(2, clone.Cooldowns["gift-test"]);
            CollectionAssert.AreEqual(new[] { "minion-1" }, clone.TriggerHistory.Events[0].Targets);
        }

        [Test]
        public void ScenarioRoundTrip_RestoresPlayerDarkGiftStateWithoutUsingTrinketSlots()
        {
            var source = MatchService.CreateWithDefaultCatalog(1234, new InMemoryTestScenarioRepository()).State;
            source.PlayerDarkGifts = CreatePlayerState();
            var scenario = TestScenarioMapper.Clone(TestScenarioMapper.Capture(source, "dark-gift-round-trip"));
            var target = MatchService.CreateWithDefaultCatalog(1, new InMemoryTestScenarioRepository()).State;

            var restore = TestScenarioMapper.TryApplyTo(target, scenario);
            source.PlayerDarkGifts.AcquiredGiftInstances[0].RemainingUses = 0;
            source.PlayerDarkGifts.TriggerHistory.Events[0].Result = "mutated";

            Assert.AreEqual(TestScenarioRestoreStatus.Applied, restore.Status, restore.Message);
            Assert.AreEqual(1, target.PlayerDarkGifts.AcquiredGiftInstances.Count);
            Assert.AreEqual("gift-instance-1", target.PlayerDarkGifts.AcquiredGiftInstances[0].InstanceId);
            Assert.AreEqual("gift-test@36.2", target.PlayerDarkGifts.AcquiredGiftInstances[0].DefinitionRevisionId);
            Assert.AreEqual(4, target.PlayerDarkGifts.AcquiredGiftInstances[0].RemainingUses);
            Assert.AreEqual(1, target.PlayerDarkGifts.Counters["normal-uses"]);
            Assert.AreEqual(2, target.PlayerDarkGifts.Cooldowns["gift-test"]);
            Assert.AreEqual("applied", target.PlayerDarkGifts.TriggerHistory.Events[0].Result);
            Assert.IsTrue(string.IsNullOrEmpty(target.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId));
            Assert.IsTrue(string.IsNullOrEmpty(target.Player.Tavern.AdvancedMechanics.Trinkets.GreaterTrinketId));
        }

        private static PlayerDarkGiftState CreatePlayerState()
        {
            return new PlayerDarkGiftState
            {
                AcquiredGiftInstances = new List<PlayerDarkGiftInstance>
                {
                    new PlayerDarkGiftInstance
                    {
                        InstanceId = "gift-instance-1",
                        DefinitionRevisionId = "gift-test@36.2",
                        AcquiredRound = 3,
                        Source = "normal-button",
                        StackCount = 2,
                        RemainingUses = 4,
                        Cooldown = 2,
                        Active = true
                    }
                },
                Counters = new Dictionary<string, int> { ["normal-uses"] = 1 },
                Cooldowns = new Dictionary<string, int> { ["gift-test"] = 2 },
                TriggerHistory = new DarkGiftTriggerHistory
                {
                    Events = new List<MechanicEventRecord>
                    {
                        new MechanicEventRecord
                        {
                            Sequence = 7,
                            Round = 3,
                            Phase = MatchPhase.Tavern,
                            Type = "dark-gift.applied",
                            Source = "normal-button",
                            Targets = new List<string> { "minion-1" },
                            Result = "applied",
                            RequestId = "dark-gift-request-1"
                        }
                    }
                }
            };
        }
    }
}
