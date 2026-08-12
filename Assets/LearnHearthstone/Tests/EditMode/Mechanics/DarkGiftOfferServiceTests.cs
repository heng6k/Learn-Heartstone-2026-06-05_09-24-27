using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class DarkGiftOfferServiceTests
    {
        [Test]
        public void CreateOffer_SameInputIsStableAcrossCatalogAndGiftOrder()
        {
            var minions = new[]
            {
                Minion("m-a", 3, Tribe.Beast),
                Minion("m-b", 3, Tribe.Mech),
                Minion("m-c", 3, Tribe.Demon),
                Minion("m-d", 4, Tribe.Dragon),
                Minion("m-e", 4, Tribe.Pirate),
                Minion("m-f", 4, Tribe.Elemental)
            };
            var gifts = new[]
            {
                Gift("g-a"), Gift("g-b"), Gift("g-c"), Gift("g-d"), Gift("g-e")
            };
            var request = Request(round: 6, seed: 2468, cursor: 4);

            var first = DarkGiftOfferService.CreateOffer(
                request,
                Profile(offerCount: 3, fromRound: 6, minTier: 3, maxTier: 4),
                new MinionCatalog(minions),
                gifts);
            var second = DarkGiftOfferService.CreateOffer(
                request,
                Profile(offerCount: 3, fromRound: 6, minTier: 3, maxTier: 4),
                new MinionCatalog(minions.Reverse()),
                gifts.Reverse());

            Assert.IsTrue(first.Succeeded, first.Message);
            Assert.IsTrue(second.Succeeded, second.Message);
            CollectionAssert.AreEqual(
                first.Options.Select(item => item.OptionId),
                second.Options.Select(item => item.OptionId));
            Assert.AreEqual(first.NextRngCursor, second.NextRngCursor);
            Assert.Greater(first.NextRngCursor, request.RngCursor);
        }

        [Test]
        public void CreateOffer_NormalEntryBeforeStartRoundReturnsStableFailure()
        {
            var result = DarkGiftOfferService.CreateOffer(
                Request(round: 2),
                Profile(offerCount: 1, fromRound: 3, minTier: 2, maxTier: 2),
                new MinionCatalog(new[] { Minion("m-a", 2, Tribe.Beast) }),
                new[] { Gift("g-a") });

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("dark-gift-offer.not-available", result.Code);
            Assert.IsEmpty(result.Options);
        }

        [Test]
        public void CreateOffer_FiltersTierPoolTribeAndProfileMechanics()
        {
            var profile = Profile(offerCount: 2, fromRound: 4, minTier: 2, maxTier: 3);
            profile.CandidateFilter.BattlecryAllowedFromRound = 5;
            profile.CandidateFilter.ChooseOneAllowedFromRound = 5;
            profile.CandidateFilter.ExcludedMechanics.Add("magnetic");
            var minions = new[]
            {
                Minion("valid-beast", 2, Tribe.Beast),
                Minion("valid-neutral", 3, Tribe.None),
                Minion("battlecry", 2, Tribe.Beast, Keyword.Battlecry),
                Minion("choose-one", 2, Tribe.Beast, Keyword.ChooseOne),
                Minion("magnetic", 2, Tribe.Beast, Keyword.Magnetic),
                Minion("inactive-naga", 2, Tribe.Naga),
                Minion("wrong-tier", 4, Tribe.Beast),
                Minion("out-of-pool", 2, Tribe.Beast, inPool: false)
            };
            var request = Request(round: 4);
            request.ActiveTribes = new List<Tribe> { Tribe.Beast, Tribe.Mech };

            var result = DarkGiftOfferService.CreateOffer(
                request,
                profile,
                new MinionCatalog(minions),
                new[] { Gift("g-a"), Gift("g-b"), Gift("g-c") });

            Assert.IsTrue(result.Succeeded, result.Message);
            CollectionAssert.AreEquivalent(
                new[] { "valid-beast-card", "valid-neutral-card" },
                result.Options.Select(item => item.MinionCardId));
        }

        [Test]
        public void CreateOffer_SoloPoolRejectsDuoCardsAndKeepsRoaringRecruiterAcrossSeeds()
        {
            var roaringRecruiter = Minion("roaring-recruiter", 3, Tribe.Dragon);
            roaringRecruiter.CardId = "BG29_816";
            var duoMinion = Minion("duo-intruder", 3, Tribe.Dragon);
            duoMinion.CardId = "BGDUO_TEST_INTRUDER";
            var profile = Profile(offerCount: 1, fromRound: 6, minTier: 3, maxTier: 3);
            var catalog = new MinionCatalog(new[] { roaringRecruiter, duoMinion });

            for (var seed = 1; seed <= 64; seed++)
            {
                var result = DarkGiftOfferService.CreateOffer(
                    Request(round: 6, seed: seed),
                    profile,
                    catalog,
                    new[] { Gift("g-a") });

                Assert.IsTrue(result.Succeeded, "seed=" + seed + ": " + result.Message);
                Assert.AreEqual("BG29_816", result.Options.Single().MinionCardId, "seed=" + seed);
            }
        }

        [Test]
        public void CreateOffer_UsesExplicitGiftCompatibilityAndDistinctPairs()
        {
            var deathrattle = Minion("deathrattle", 3, Tribe.Beast, Keyword.Deathrattle);
            var magnetic = Minion("magnetic", 3, Tribe.Mech, Keyword.Magnetic);
            var plain = Minion("plain", 3, Tribe.Demon);
            var deathGift = Gift("death-gift");
            deathGift.RequiredMinionTags.Add("keyword:deathrattle");
            var magneticGift = Gift("magnetic-gift");
            magneticGift.RequiredMinionTags.Add("keyword:magnetic");
            var plainGift = Gift("plain-gift");
            plainGift.ExcludedMinionTags.Add("keyword:deathrattle");
            plainGift.ExcludedMinionTags.Add("keyword:magnetic");

            var result = DarkGiftOfferService.CreateOffer(
                Request(round: 6),
                Profile(offerCount: 3, fromRound: 6, minTier: 3, maxTier: 3),
                new MinionCatalog(new[] { deathrattle, magnetic, plain }),
                new[] { deathGift, magneticGift, plainGift });

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(3, result.Options.Select(item => item.MinionCardId).Distinct().Count());
            Assert.AreEqual(3, result.Options.Select(item => item.GiftRevisionId).Distinct().Count());
            Assert.AreEqual("deathrattle-card", result.Options.Single(item => item.GiftRevisionId == "death-gift@1").MinionCardId);
            Assert.AreEqual("magnetic-card", result.Options.Single(item => item.GiftRevisionId == "magnetic-gift@1").MinionCardId);
            Assert.AreEqual("plain-card", result.Options.Single(item => item.GiftRevisionId == "plain-gift@1").MinionCardId);
        }

        [Test]
        public void CreateOffer_FromGuaranteeRoundIncludesStableMostCommonTribe()
        {
            var profile = Profile(offerCount: 3, fromRound: 6, minTier: 3, maxTier: 3);
            profile.CommonTribeGuarantee = new DarkGiftCommonTribeGuarantee
            {
                Enabled = true,
                StartRound = 6,
                MinimumOfferCount = 1
            };
            var request = Request(round: 6);
            request.CurrentBoardTribeCounts = new List<DarkGiftTribeCount>
            {
                new DarkGiftTribeCount { Tribe = Tribe.Mech, Count = 2 },
                new DarkGiftTribeCount { Tribe = Tribe.Beast, Count = 3 }
            };
            var minions = new[]
            {
                Minion("only-beast", 3, Tribe.Beast),
                Minion("mech", 3, Tribe.Mech),
                Minion("demon", 3, Tribe.Demon),
                Minion("dragon", 3, Tribe.Dragon)
            };

            var result = DarkGiftOfferService.CreateOffer(
                request,
                profile,
                new MinionCatalog(minions),
                new[] { Gift("g-a"), Gift("g-b"), Gift("g-c"), Gift("g-d") });

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.IsTrue(result.Options.Any(item => item.MinionTribes.Contains(Tribe.Beast)));
        }

        [Test]
        public void CreateOffer_TransportsMinionRulesTextIntoEveryOption()
        {
            var minion = Minion("rules-text", 2, Tribe.Beast);
            minion.Text = "After you summon a Beast, give it +1/+1.";

            var result = DarkGiftOfferService.CreateOffer(
                Request(round: 3),
                Profile(offerCount: 1, fromRound: 3, minTier: 2, maxTier: 2),
                new MinionCatalog(new[] { minion }),
                new[] { Gift("g-a") });

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(minion.Text, result.Options.Single().MinionText);
        }

        [Test]
        public void CreateOffer_WhenFiveStarCandidatesAreLegalIncludesNeutralCore()
        {
            var brann = Minion("brann", 5, Tribe.None);
            brann.CardId = "BG_LOE_077";
            var result = DarkGiftOfferService.CreateOffer(
                Request(round: 10),
                Profile(offerCount: 3, fromRound: 10, minTier: 5, maxTier: 6),
                new MinionCatalog(new[]
                {
                    Minion("ordinary-a", 5, Tribe.Beast),
                    Minion("ordinary-b", 5, Tribe.Mech),
                    Minion("ordinary-c", 6, Tribe.Demon),
                    brann
                }),
                new[] { Gift("g-a"), Gift("g-b"), Gift("g-c"), Gift("g-d") });

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.IsTrue(result.Options.Any(item => item.MinionCardId == "BG_LOE_077"),
                "A legal five-star neutral core must be represented in the offer.");
        }

        [Test]
        public void CreateOffer_WhenNoNeutralCoreIsLegalFallsBackWithoutFailing()
        {
            var result = DarkGiftOfferService.CreateOffer(
                Request(round: 10),
                Profile(offerCount: 3, fromRound: 10, minTier: 5, maxTier: 6),
                new MinionCatalog(new[]
                {
                    Minion("ordinary-a", 5, Tribe.Beast),
                    Minion("ordinary-b", 5, Tribe.Mech),
                    Minion("ordinary-c", 6, Tribe.Demon)
                }),
                new[] { Gift("g-a"), Gift("g-b"), Gift("g-c") });

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(3, result.Options.Count);
        }

        [Test]
        public void CreateOffer_SourceParametersCanBypassNormalRoundLimitsWithoutNameChecks()
        {
            var gift = Gift("exclusive-gift");
            gift.EarliestOfferRound = 10;
            var request = Request(round: 1);
            request.SourceKind = DarkGiftOfferSourceKind.HeroPower;
            request.SourceId = "arbitrary-source-id";
            request.RequestedTier = 5;
            request.GiftPoolProfileId = "exclusive-21-item-pool";
            request.IgnoreNormalRoundRestrictions = true;
            request.OfferCount = 1;
            request.PickCount = 1;
            var profile = Profile(offerCount: 3, fromRound: 3, minTier: 2, maxTier: 2);
            profile.CandidateFilter.BattlecryAllowedFromRound = 5;

            var result = DarkGiftOfferService.CreateOffer(
                request,
                profile,
                new MinionCatalog(new[] { Minion("tier-five", 5, Tribe.Demon, Keyword.Battlecry) }),
                new[] { gift });

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual("tier-five-card", result.Options.Single().MinionCardId);
            Assert.AreEqual("exclusive-gift@1", result.Options.Single().GiftRevisionId);
            Assert.AreEqual("exclusive-21-item-pool", result.GiftPoolProfileId);
            Assert.AreEqual("arbitrary-source-id", result.SourceId);
        }

        [TestCase("DG-R14")]
        [TestCase("DG-R15")]
        [TestCase("DG-R16")]
        [TestCase("DG-R31")]
        [TestCase("DG-R32")]
        [TestCase("DG-R33")]
        public void CreateOffer_HistoricalGiftRequiresPositiveMatchingCount(string researchKey)
        {
            var gift = Gift("history-gift");
            gift.ResearchKey = researchKey;
            var request = Request(round: 7);
            var profile = Profile(offerCount: 1, fromRound: 7, minTier: 4, maxTier: 4);
            var minions = new MinionCatalog(new[] { Minion("history-target", 4, Tribe.Beast) });

            var blocked = DarkGiftOfferService.CreateOffer(request, profile, minions, new[] { gift });

            Assert.IsFalse(blocked.Succeeded);
            Assert.AreEqual("dark-gift-offer.insufficient-candidates", blocked.Code);

            request.BattlecriesTriggeredThisGame = 1;
            request.DeathrattlesTriggeredThisGame = 1;
            request.TavernSpellsCastThisGame = 1;
            var available = DarkGiftOfferService.CreateOffer(request, profile, minions, new[] { gift });

            Assert.IsTrue(available.Succeeded, available.Message);
            Assert.AreEqual(researchKey, gift.ResearchKey);
        }

        [TestCase(Keyword.Battlecry, "DG-R06", false)]
        [TestCase(Keyword.Battlecry, "DG-R18", true)]
        [TestCase(Keyword.ChooseOne, "DG-R20", false)]
        [TestCase(Keyword.ChooseOne, "DG-R27", true)]
        [TestCase(Keyword.Deathrattle, "DG-R04", false)]
        [TestCase(Keyword.Deathrattle, "DG-R06", true)]
        [TestCase(Keyword.Deathrattle, "DG-R15", true)]
        public void CreateOffer_EnforcesOfficialKeywordCrossRules(
            Keyword keyword,
            string researchKey,
            bool expectedAvailable)
        {
            var gift = Gift("cross-rule-gift");
            gift.ResearchKey = researchKey;
            var request = Request(round: 7);
            request.BattlecriesTriggeredThisGame = 1;
            request.DeathrattlesTriggeredThisGame = 1;
            request.TavernSpellsCastThisGame = 1;

            var result = DarkGiftOfferService.CreateOffer(
                request,
                Profile(offerCount: 1, fromRound: 7, minTier: 4, maxTier: 4),
                new MinionCatalog(new[] { Minion("cross-rule-target", 4, Tribe.Beast, keyword) }),
                new[] { gift });

            Assert.AreEqual(expectedAvailable, result.Succeeded, result.Message);
        }

        [Test]
        public void CreateOffer_EnforcesLobbyTierAndLowestOfferedTierRules()
        {
            var profile = Profile(offerCount: 1, fromRound: 7, minTier: 3, maxTier: 3);
            var request = Request(round: 7);
            request.ActiveTribes = new List<Tribe> { Tribe.Beast, Tribe.Quilboar };
            var shield = Gift("toughened-shield");
            shield.ResearchKey = "DG-R07";
            var tierThree = new MinionCatalog(new[] { Minion("tier-three", 3, Tribe.Beast) });

            Assert.IsFalse(DarkGiftOfferService.CreateOffer(request, profile, tierThree, new[] { shield }).Succeeded);
            request.ActiveTribes = new List<Tribe> { Tribe.Beast };
            Assert.IsTrue(DarkGiftOfferService.CreateOffer(request, profile, tierThree, new[] { shield }).Succeeded);

            var polarization = Gift("polarization");
            polarization.ResearchKey = "DG-R22";
            request.PlayerTavernTier = 2;
            Assert.IsFalse(DarkGiftOfferService.CreateOffer(request, profile, tierThree, new[] { polarization }).Succeeded);
            request.PlayerTavernTier = 3;
            Assert.IsTrue(DarkGiftOfferService.CreateOffer(request, profile, tierThree, new[] { polarization }).Succeeded);

            var gilding = Gift("gilding");
            gilding.ResearchKey = "DG-R17";
            var other = Gift("other");
            var gildingResult = DarkGiftOfferService.CreateOffer(
                Request(round: 7),
                Profile(offerCount: 2, fromRound: 7, minTier: 2, maxTier: 3),
                new MinionCatalog(new[]
                {
                    Minion("lower", 2, Tribe.Beast),
                    Minion("higher", 3, Tribe.Mech)
                }),
                new[] { gilding, other });

            Assert.IsTrue(gildingResult.Succeeded, gildingResult.Message);
            Assert.AreEqual(2, gildingResult.Options.Single(item => item.GiftRevisionId == "gilding@1").MinionTier);
        }

        private static DarkGiftOfferRequest Request(int round, int seed = 1234, int cursor = 0)
        {
            return new DarkGiftOfferRequest
            {
                SourceKind = DarkGiftOfferSourceKind.NormalButton,
                SourceId = "normal-button",
                Round = round,
                OfferCount = 0,
                PickCount = 0,
                ActiveTribes = TribeAvailabilityRules.AllPlayableTribes(),
                GiftPoolProfileId = "normal-pool",
                Seed = seed,
                RngCursor = cursor
            };
        }

        private static DarkGiftProfile Profile(int offerCount, int fromRound, int minTier, int maxTier)
        {
            return new DarkGiftProfile
            {
                Id = "test-profile",
                Enabled = true,
                NormalEntryStartRound = 3,
                OfferCount = offerCount,
                PickCount = 1,
                TierRanges = new List<DarkGiftTierRangeRule>
                {
                    new DarkGiftTierRangeRule { FromRound = fromRound, MinTier = minTier, MaxTier = maxTier }
                },
                CandidateFilter = new DarkGiftCandidateFilter(),
                DeduplicationPolicy = "distinct-gift-definitions-per-offer"
            };
        }

        private static DarkGiftDefinition Gift(string id)
        {
            return new DarkGiftDefinition
            {
                Id = id,
                RevisionId = id + "@1",
                DisplayName = id,
                ImplementationStatus = DarkGiftImplementationStatus.FrameworkOnly
            };
        }

        private static MinionDefinition Minion(
            string id,
            int tier,
            Tribe tribe,
            Keyword? keyword = null,
            bool inPool = true)
        {
            var result = new MinionDefinition
            {
                Id = id,
                CardId = id + "-card",
                RevisionId = id + "@1",
                Name = id,
                Text = id + " rules text",
                TavernTier = tier,
                BaseAttack = tier,
                BaseHealth = tier,
                InPool = inPool,
                Tribes = tribe == Tribe.None ? new List<Tribe>() : new List<Tribe> { tribe }
            };
            if (keyword.HasValue)
            {
                result.Keywords.Add(keyword.Value);
            }
            return result;
        }
    }
}
