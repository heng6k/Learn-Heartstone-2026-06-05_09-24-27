using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14P1AContractTests
    {
        private const string SnapshotRelativePath = "Docs/data/season14-p1a-facts-20260806.json";
        private const string LockboxDefinitionRevisionId = "NEUTRAL_ROGUE_BG36_520t@36.2";
        private const string LockboxOpenResolverId = "season14.lockbox.open@1";
        private const string InsurrectionistsBladeId = "BG36_MagicItem_214";
        private const string AllianceFlagCardId = "117567";

        [Test]
        public void FactSnapshot_BindsEveryRecordToResolvedSeason14FingerprintAndReviewMetadata()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            Assert.IsNotNull(projectRoot);
            var path = Path.Combine(projectRoot, SnapshotRelativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(path), "Missing P1-A fact snapshot: " + path);

            var facts = JsonUtility.FromJson<FactSnapshot>(File.ReadAllText(path));
            Assert.IsNotNull(facts);
            Assert.AreEqual("season14-p1a-facts-20260806", facts.snapshotId);
            Assert.AreEqual(GameVersionIds.Season14Preview, facts.gameVersionId);
            Assert.AreEqual("Partial", facts.implementationStatus);

            var embedded = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = embedded.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, embedded);
            Assert.AreEqual(
                resolved.ContentFingerprint,
                facts.contentFingerprint,
                "Replace the capture placeholder with this resolver-produced fingerprint before P1-A closes.");

            Assert.IsNotNull(facts.sources);
            Assert.IsNotEmpty(facts.sources);
            Assert.IsNotNull(facts.facts);
            Assert.GreaterOrEqual(facts.facts.Length, 20);
            var sourceIds = new HashSet<string>(facts.sources.Select(source => source.sourceId), StringComparer.Ordinal);
            foreach (var source in facts.sources)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(source.sourceId));
                Assert.IsFalse(string.IsNullOrWhiteSpace(source.sourceLevel));
                Assert.IsFalse(string.IsNullOrWhiteSpace(source.url));
                Assert.IsTrue(DateTime.TryParse(source.capturedAtUtc, out _), source.sourceId);
            }

            foreach (var fact in facts.facts)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(fact.factId));
                Assert.IsFalse(string.IsNullOrWhiteSpace(fact.revisionId), fact.factId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(fact.sourceLevel), fact.factId);
                Assert.AreEqual(facts.contentFingerprint, fact.contentFingerprint, fact.factId);
                Assert.IsTrue(DateTime.TryParse(fact.capturedAtUtc, out _), fact.factId);
                Assert.IsNotNull(fact.sourceIds, fact.factId);
                Assert.IsNotEmpty(fact.sourceIds, fact.factId);
                Assert.IsTrue(fact.sourceIds.All(sourceIds.Contains), fact.factId + " has an unknown source id.");
            }

            var factIds = new HashSet<string>(facts.facts.Select(fact => fact.factId), StringComparer.Ordinal);
            CollectionAssert.IsSubsetOf(new[]
            {
                "setup.full-hero-selection",
                "setup.custom-tribes-5-to-10",
                "setup.season14-mechanism-boundary",
                "lockbox.turn-end-base",
                "lockbox.drakkari-normal",
                "lockbox.drakkari-golden",
                "minion.aureate-laureate-36.2",
                "minion.silent-deliverer-generated-golden",
                "choose-one.combined-effects",
                "rally.recruit-phase-propagation",
                "hero.nightmare-lord-xavius",
                "hero.trastath-soul-parasite",
                "armor.season14-solo-profile",
                "hero-override.edwin-vancleef",
                "hero-override.rakanishu",
                "hero-override.cariel-roame",
                "hero-override.ragnaros",
                "hero-override.overlord-saurfang",
                "hero-override-enhance-o-mechano"
            }, factIds);
        }

        [Test]
        public void TribeSelection_ManualSixthChoiceRemainsSelectableAndCanContinue()
        {
            var rootObject = new GameObject("P1A-TribeSelection", typeof(RectTransform));
            try
            {
                List<Tribe> startedWith = null;
                new UnityTavernTribeSelectionView(
                    rootObject.transform,
                    tribes => startedWith = tribes,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();

                var firstSix = TribeAvailabilityRules.PlayableTribes.Take(6).ToList();
                Assert.AreEqual(6, firstSix.Count, "The production catalog must expose at least six playable tribes.");
                foreach (var tribe in firstSix.Take(5))
                {
                    FindChild(rootObject.transform, "UnityTribeSelection" + tribe + "Button")
                        .GetComponent<Button>().onClick.Invoke();
                }

                var sixthButton = FindChild(rootObject.transform, "UnityTribeSelection" + firstSix[5] + "Button")
                    .GetComponent<Button>();
                Assert.IsTrue(sixthButton.interactable, "P1-B: selecting five tribes must not disable choices six through ten.");
                sixthButton.onClick.Invoke();
                StringAssert.Contains("6/10", FindChild(rootObject.transform, "UnityTribeSelectionSummary").GetComponent<Text>().text);

                FindChild(rootObject.transform, "UnityTribeSelectionEnterButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityAdvancedMechanicsSetupOverlay"));
                FindChild(rootObject.transform, "UnityAdvancedMechanicsStartButton").GetComponent<Button>().onClick.Invoke();
                CollectionAssert.AreEquivalent(firstSix, startedWith);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [TestCase(false, 3)]
        [TestCase(true, 2)]
        public void Lockbox_TurnEndUsesStrongestDrakkariOccurrenceCount(bool golden, int expectedRemainingTurns)
        {
            var delayedResolvers = new DelayedObjectResolverRegistry();
            var service = CreateSeason14Service(false, delayedResolvers: delayedResolvers);
            Assert.IsTrue(LockboxMechanicService.CreateOrAccelerate(
                service.State,
                new LockboxMechanicRequest
                {
                    InstanceId = "p1a-lockbox",
                    DefinitionRevisionId = LockboxDefinitionRevisionId,
                    OpenResolverId = LockboxOpenResolverId,
                    Source = "p1a",
                    RequestId = "p1a-lockbox-create"
                },
                delayedResolvers).Succeeded);
            var drakkari = TestMinion("p1a-drakkari", "BG26_ICC_901", Tribe.None);
            drakkari.Golden = golden;
            service.State.Player.Board.Add(drakkari);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(expectedRemainingTurns, service.State.DelayedObjectStates.Single().RemainingTurns);
            Assert.AreEqual(
                golden ? 3 : 2,
                service.State.MechanicEvents.Count(item => item.Type == "delayed-object.turn-ended"),
                "Each Drakkari repeat must be a distinct idempotent TurnEnded occurrence.");
        }

        [Test]
        public void AureateLaureate_Season14TargetDoesNotRewriteLegacyDefinition()
        {
            var embedded = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolver = embedded.VersionedContent.CreateResolver();
            var preview = resolver.Resolve(GameVersionIds.Season14Preview, embedded)
                .Snapshot.English.Minions.GetByCardId("BG32_236");
            var legacy = resolver.Resolve(GameVersionIds.LegacyCompositeSandbox, embedded)
                .Snapshot.English.Minions.GetByCardId("BG32_236");

            Assert.AreEqual(1, legacy.BaseAttack, "Historical Aureate Laureate must remain 1/1.");
            Assert.AreEqual(1, legacy.BaseHealth, "Historical Aureate Laureate must remain 1/1.");
            CollectionAssert.Contains(legacy.Keywords, Keyword.Battlecry);

            Assert.AreEqual(2, preview.BaseAttack, "P1-D: 36.2 Aureate Laureate is 2/2.");
            Assert.AreEqual(2, preview.BaseHealth, "P1-D: 36.2 Aureate Laureate is 2/2.");
            CollectionAssert.DoesNotContain(preview.Keywords, Keyword.Battlecry);
            StringAssert.Contains("always Golden", preview.Text);
            StringAssert.Contains("Triple Reward", preview.Text);
            Assert.IsNotNull(preview.Golden);
            Assert.AreEqual(2, preview.Golden.BaseAttack);
            Assert.AreEqual(2, preview.Golden.BaseHealth);
            CollectionAssert.DoesNotContain(preview.Golden.Keywords, Keyword.Battlecry);
        }

        [Test]
        public void ChooseOneCombinedTag_AllianceFlagResolvesBothBranchesOnce()
        {
            var service = CreateSeason14Service(false);
            var target = TestMinion("p1a-alliance-target", "p1a-alliance-target", Tribe.Murloc);
            target.BaseAttack = target.Attack = 1;
            target.BaseHealth = target.Health = target.MaxHealth = 1;
            service.State.Player.Board.Add(target);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, AllianceFlagCardId, CardKind.TavernSpell));
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            service.State.Player.Tavern.Hand[0].Tags.Add("choose_one_both_effects");

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId,
                choiceId: "attack"));

            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(5, target.MaxHealth);
            Assert.AreEqual(2, target.Enchantments.Count, "Both ordered branches should resolve while the spell is consumed once.");
            Assert.IsEmpty(service.State.Player.Tavern.Hand);
        }

        [Test]
        public void RecruitPhaseRally_PropagatesToBoundDarkGiftObserversWithoutDuplicatingSelfEffect()
        {
            var service = CreateBoundRallyGiftService(out var gifted);
            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, InsurrectionistsBladeId, CardKind.Trinket, 1));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(3, gifted.Attack, "Insurrectionist's Blade should trigger Glim Guardian's own Rally once.");
            Assert.AreEqual(
                2,
                service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"),
                "P1-F: recruit-phase Rally must also dispatch RallyResolved to the bound Dark Gift observer.");
            Assert.AreEqual(1, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
        }

        [Test]
        public void RecruitPhaseRally_EmitsDistinctOccurrenceIdsForEveryLegalTrigger()
        {
            var rally = TestMinion("p1f-rally-source", "BG29_888", Tribe.Dragon);
            rally.Keywords.Add(Keyword.Rally);
            var board = new List<MinionInstance> { rally };

            var rewards = CombatEngine.ResolveRecruitPhaseRally(
                    board,
                    rally,
                    new TavernState(),
                    new List<MinionInstance>(),
                    36201,
                    2)
                .Where(reward => reward.Type == CombatRewardType.FriendlyRallyTriggered)
                .ToList();

            Assert.AreEqual(2, rewards.Count);
            Assert.IsTrue(rewards.All(reward => !string.IsNullOrWhiteSpace(reward.RallyOccurrenceId)));
            Assert.AreEqual(2, rewards.Select(reward => reward.RallyOccurrenceId).Distinct().Count());
            Assert.IsTrue(rewards.All(reward => reward.SourceInstanceId == rally.InstanceId));
        }

        [Test]
        public void RallyObservers_ReplayingTheSameOccurrenceDispatchesOnlyOnce()
        {
            var service = CreateBoundRallyGiftService(out var gifted);
            var reward = RallyReward(gifted, "p1f-replayed-occurrence");

            ApplyCombatRewards(service, new[] { reward });
            ApplyCombatRewards(service, new[] { reward });

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
            Assert.AreEqual(1, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
            Assert.AreEqual(1, service.State.MechanicEvents.Count(item => item.Type == "rally.observers-dispatched"));
        }

        [Test]
        public void RallyObservers_DifferentOccurrencesDispatchSeparately()
        {
            var service = CreateBoundRallyGiftService(out var gifted);

            ApplyCombatRewards(service, new[]
            {
                RallyReward(gifted, "p1f-occurrence-1"),
                RallyReward(gifted, "p1f-occurrence-2")
            });

            Assert.AreEqual(4, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
            Assert.AreEqual(2, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
            Assert.AreEqual(2, service.State.MechanicEvents.Count(item => item.Type == "rally.observers-dispatched"));
        }

        private static MatchService CreateBoundRallyGiftService(out MinionInstance gifted)
        {
            var definition = new DarkGiftDefinition
            {
                Id = "p1a-consanguinity",
                RevisionId = "p1a-consanguinity@1",
                EffectRevision = Season14DarkGiftResolvers.ConsanguinityRevision,
                DisplayName = "P1-A Consanguinity",
                TriggerSpec = MechanicEventType.RallyResolved.ToString(),
                StackPolicy = DarkGiftStackPolicies.Reject,
                DurationPolicy = DarkGiftDurationPolicies.Persistent,
                ImplementationStatus = DarkGiftImplementationStatus.FrameworkOnly
            };
            var giftResolvers = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(giftResolvers);
            var service = CreateSeason14Service(true, new[] { definition }, giftResolvers);
            gifted = TestMinion("p1a-gifted-glim", "BG29_888", Tribe.Dragon);
            gifted.BaseAttack = gifted.Attack = 1;
            gifted.BaseHealth = gifted.Health = gifted.MaxHealth = 10;
            service.State.Player.Board.Add(gifted);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(
                service.State,
                gifted,
                definition,
                "p1a",
                "p1a-acquire-consanguinity",
                giftResolvers).Succeeded);
            return service;
        }

        private static CombatReward RallyReward(MinionInstance source, string occurrenceId)
        {
            return new CombatReward
            {
                Type = CombatRewardType.FriendlyRallyTriggered,
                Side = BoardSide.Player,
                SourceCardId = source.CardId,
                SourceInstanceId = source.InstanceId,
                RallyOccurrenceId = occurrenceId,
                Amount = 1
            };
        }

        private static void ApplyCombatRewards(MatchService service, IEnumerable<CombatReward> rewards)
        {
            var method = typeof(MatchService).GetMethod("ApplyCombatRewards", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(service, new object[] { rewards });
        }

        private static MatchService CreateSeason14Service(
            bool enableTrinkets,
            IEnumerable<DarkGiftDefinition> darkGiftDefinitions = null,
            DarkGiftResolverRegistry darkGiftResolvers = null,
            DelayedObjectResolverRegistry delayedResolvers = null)
        {
            var embedded = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = embedded.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, embedded);
            var service = MatchService.CreateWithResolvedVersion(
                resolved,
                36201,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    ActiveTribes = Enum.GetValues(typeof(Tribe)).Cast<Tribe>()
                        .Where(tribe => tribe != Tribe.None && tribe != Tribe.All)
                        .ToList(),
                    EnableQuests = false,
                    EnableTrinkets = enableTrinkets,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                },
                delayedObjectResolvers: delayedResolvers,
                darkGiftDefinitions: darkGiftDefinitions,
                darkGiftResolvers: darkGiftResolvers);
            service.State.Phase = MatchPhase.Tavern;
            service.State.Round = 3;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Gold = 20;
            service.State.DelayedObjectStates.Clear();
            service.State.MechanicEvents.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            return service;
        }

        private static MinionInstance TestMinion(string instanceId, string cardId, Tribe tribe)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = cardId,
                Name = instanceId,
                CardKind = CardKind.Minion,
                Owner = BoardSide.Player,
                BaseAttack = 2,
                BaseHealth = 3,
                Attack = 2,
                Health = 3,
                MaxHealth = 3,
                TavernTier = 1,
                Tribes = new List<Tribe> { tribe },
                PoolSource = PoolSource.Debug
            };
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                var found = FindChild(root.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        [Serializable]
        private sealed class FactSnapshot
        {
            public string snapshotId;
            public string gameVersionId;
            public string contentFingerprint;
            public string implementationStatus;
            public FactSource[] sources;
            public FactRecord[] facts;
        }

        [Serializable]
        private sealed class FactSource
        {
            public string sourceId;
            public string sourceLevel;
            public string url;
            public string capturedAtUtc;
        }

        [Serializable]
        private sealed class FactRecord
        {
            public string factId;
            public string revisionId;
            public string sourceLevel;
            public string contentFingerprint;
            public string capturedAtUtc;
            public string[] sourceIds;
        }
    }
}
