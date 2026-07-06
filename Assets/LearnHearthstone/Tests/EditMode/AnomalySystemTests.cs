using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class AnomalySystemTests
    {
        [Test]
        public void AnomalyCatalog_LoadsCurrentHsReplaySnapshot()
        {
            var catalog = AnomalyCatalogLoader.LoadFromResources();

            Assert.AreEqual(111, catalog.All.Count);
            Assert.AreEqual(28, catalog.GetByPool(AnomalyPoolVersion.CurrentHsReplay).Count);
            Assert.AreEqual(111, catalog.GetByPool(AnomalyPoolVersion.AllKnown).Count);
            Assert.AreEqual(83, catalog.All.Count(anomaly => anomaly.ImplementationStatus == AnomalyImplementationStatus.Unsupported));
            Assert.AreEqual(83, catalog.All.Count(anomaly =>
                anomaly.Tags.Contains("historical") &&
                anomaly.Tags.Contains("data_only") &&
                anomaly.AvailabilityReasons.Contains(AnomalyAvailabilityReason.RequiresOfficialDataReview)));
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG31_Anomaly_123").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_711").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_303").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_900").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_751").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG31_Anomaly_120").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_572").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_570").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_571").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG35_Anomaly_006").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG31_Anomaly_124").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_301").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG35_Anomaly_001").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG34_Anomaly_805").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_504").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG35_Anomaly_005").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG32_Anomaly_001").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG35_Anomaly_007").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG32_Anomaly_002").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG35_Anomaly_004").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG35_Anomaly_002").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG35_Anomaly_008").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG31_Anomaly_106").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_Prizes2").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_716").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_810").ImplementationStatus);
            Assert.IsFalse(catalog.GetByCardId("BG27_Anomaly_810").AvailabilityReasons.Contains(AnomalyAvailabilityReason.RequiresBuddyMode));
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_580").ImplementationStatus);
            Assert.AreEqual(AnomalyImplementationStatus.Implemented, catalog.GetByCardId("BG27_Anomaly_503").ImplementationStatus);
            Assert.IsFalse(catalog.GetByCardId("BG31_Anomaly_123").AvailabilityReasons.Contains(AnomalyAvailabilityReason.RequiresSecondHeroPowerUi));
            Assert.IsFalse(catalog.GetByCardId("BG27_Anomaly_580").AvailabilityReasons.Contains(AnomalyAvailabilityReason.RequiresSharedLobbyChoice));
            Assert.IsFalse(catalog.GetByCardId("BG27_Anomaly_503").AvailabilityReasons.Contains(AnomalyAvailabilityReason.RequiresSharedLobbyChoice));
            Assert.IsFalse(catalog.GetByCardId("BG27_Anomaly_503").AvailabilityReasons.Contains(AnomalyAvailabilityReason.RequiresYoggWheel));
            Assert.AreEqual(AnomalyEffectFamily.SinglePlayerChoice, catalog.GetByCardId("BG27_Anomaly_580").EffectFamily);
            Assert.AreEqual(AnomalyEffectFamily.SinglePlayerChoice, catalog.GetByCardId("BG27_Anomaly_503").EffectFamily);
            Assert.IsTrue(catalog.GetByCardId("BG27_Anomaly_580").Tags.Contains("single_player_adaptation"));
            Assert.IsTrue(catalog.GetByCardId("BG27_Anomaly_503").Tags.Contains("single_player_adaptation"));
            StringAssert.Contains("Local trainer adaptation", catalog.GetByCardId("BG27_Anomaly_580").Notes);
            StringAssert.Contains("Local trainer adaptation", catalog.GetByCardId("BG27_Anomaly_503").Notes);
            Assert.IsTrue(catalog.GetByPool(AnomalyPoolVersion.CurrentHsReplay)
                .Where(anomaly => anomaly.ImplementationStatus == AnomalyImplementationStatus.Implemented)
                .All(anomaly => anomaly.DbfId > 0));
        }

        [Test]
        public void AnomalyCatalog_RejectsUnknownEffectFamily()
        {
            var json = @"{
                ""snapshotDate"": ""2026-07-05"",
                ""sourceUrl"": """",
                ""count"": 1,
                ""anomalies"": [
                    {
                        ""id"": ""TEST_ANOMALY"",
                        ""cardId"": ""TEST_ANOMALY"",
                        ""name"": ""Test Anomaly"",
                        ""text"": ""Test."",
                        ""sourcePools"": [""CurrentHsReplay""],
                        ""effectFamily"": ""TypoFamily"",
                        ""implementationStatus"": ""Implemented"",
                        ""availabilityReasons"": [],
                        ""tags"": []
                    }
                ]
            }";

            Assert.Throws<InvalidOperationException>(() => AnomalyCatalogLoader.LoadFromJson(json));
        }

        [Test]
        public void AnomalySetup_RandomUsesOnlyDefaultOfferableImplementedAnomalies()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions
                {
                    EnableAnomalies = true,
                    RandomizeAnomaly = true
                });

            var offerable = service.GetDefaultOfferableAnomalies().ToList();
            var anomalies = service.State.Player.Tavern.AdvancedMechanics.Anomalies;
            var offerableCardIds = offerable.Select(anomaly => anomaly.CardId).ToList();

            Assert.AreEqual(28, offerable.Count);
            CollectionAssert.Contains(offerableCardIds, "BG31_Anomaly_123");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_711");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_504");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_Prizes2");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_716");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_303");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_900");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_751");
            CollectionAssert.Contains(offerableCardIds, "BG31_Anomaly_120");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_572");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_570");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_571");
            CollectionAssert.Contains(offerableCardIds, "BG35_Anomaly_006");
            CollectionAssert.Contains(offerableCardIds, "BG31_Anomaly_124");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_301");
            CollectionAssert.Contains(offerableCardIds, "BG35_Anomaly_001");
            CollectionAssert.Contains(offerableCardIds, "BG34_Anomaly_805");
            CollectionAssert.Contains(offerableCardIds, "BG35_Anomaly_005");
            CollectionAssert.Contains(offerableCardIds, "BG32_Anomaly_001");
            CollectionAssert.Contains(offerableCardIds, "BG35_Anomaly_007");
            CollectionAssert.Contains(offerableCardIds, "BG32_Anomaly_002");
            CollectionAssert.Contains(offerableCardIds, "BG35_Anomaly_004");
            CollectionAssert.Contains(offerableCardIds, "BG35_Anomaly_002");
            CollectionAssert.Contains(offerableCardIds, "BG35_Anomaly_008");
            CollectionAssert.Contains(offerableCardIds, "BG31_Anomaly_106");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_810");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_580");
            CollectionAssert.Contains(offerableCardIds, "BG27_Anomaly_503");
            Assert.IsTrue(anomalies.Enabled);
            CollectionAssert.Contains(offerableCardIds, anomalies.ActiveCardId);
        }

        [Test]
        public void AnomalySetup_AllKnownKeepsUnsupportedHistoricalEntriesOutOfDefaultOffers()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions
                {
                    EnableAnomalies = true,
                    RandomizeAnomaly = true,
                    AnomalyPoolVersion = AnomalyPoolVersion.AllKnown
                });

            var candidates = service.GetAnomalyCandidateDefinitions().ToList();
            var offerable = service.GetDefaultOfferableAnomalies().ToList();

            Assert.AreEqual(111, candidates.Count);
            Assert.AreEqual(28, offerable.Count);
            Assert.AreEqual(83, candidates.Count(anomaly => anomaly.ImplementationStatus == AnomalyImplementationStatus.Unsupported));
            Assert.IsTrue(offerable.All(anomaly => anomaly.ImplementationStatus != AnomalyImplementationStatus.Unsupported));
            CollectionAssert.DoesNotContain(
                candidates
                    .Where(anomaly => anomaly.ImplementationStatus == AnomalyImplementationStatus.Unsupported)
                    .Select(anomaly => anomaly.CardId)
                    .ToList(),
                service.State.Player.Tavern.AdvancedMechanics.Anomalies.ActiveCardId);
        }

        [Test]
        public void AnomalySetup_SelectedUnsupportedHistoricalAnomalyIsRejected()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions
                {
                    EnableAnomalies = true,
                    SelectedAnomalyCardId = "BG27_Anomaly_000",
                    AnomalyPoolVersion = AnomalyPoolVersion.AllKnown
                });

            var anomalies = service.State.Player.Tavern.AdvancedMechanics.Anomalies;

            Assert.IsTrue(anomalies.Enabled);
            Assert.IsTrue(string.IsNullOrEmpty(anomalies.ActiveCardId));
            Assert.IsTrue(service.AnomalyCatalog.GetByCardId("BG27_Anomaly_000").ImplementationStatus == AnomalyImplementationStatus.Unsupported);
        }

        [Test]
        public void CosmicDuality_DiscoversAndGrantsSecondHeroPower()
        {
            var service = CreateAnomalyService(
                "BG31_Anomaly_123",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var player = service.State.Player;
            var tavern = player.Tavern;
            var primaryHeroPower = player.HeroPowerCardId;
            var discover = tavern.Discover;

            Assert.IsNotNull(discover);
            Assert.AreEqual("anomaly-cosmic-duality", discover.Source);
            Assert.AreEqual(0, discover.RewardTier);
            Assert.AreEqual(1, discover.RemainingPicks);
            Assert.AreEqual(3, discover.Options.Count);
            Assert.IsTrue(discover.Options.All(option => option.CardKind == CardKind.HeroPower));
            Assert.IsTrue(discover.Options.All(option => option.CardId != primaryHeroPower));
            Assert.AreEqual(discover.Options.Count, discover.Options.Select(option => option.CardId).Distinct().Count());

            var pickedCardId = discover.Options[0].CardId;
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(primaryHeroPower, player.HeroPowerCardId);
            CollectionAssert.Contains(player.ExtraHeroPowerCardIds, pickedCardId);
            Assert.AreEqual(1, player.ExtraHeroPowerUnlockRounds[pickedCardId]);
        }

        [Test]
        public void CosmicDuality_ReportsHeroPowerCandidateImplementationStatuses()
        {
            var service = CreateAnomalyService(
                "BG31_Anomaly_123",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var primaryHeroPower = service.State.Player.HeroPowerCardId;
            var statuses = service.GetCosmicDualityHeroPowerCandidateImplementationStatuses();
            var discoverCardIds = service.State.Player.Tavern.Discover.Options.Select(option => option.CardId).ToList();

            Assert.Greater(statuses.Count, 0);
            Assert.IsTrue(statuses.All(status => status.Source == "anomaly-cosmic-duality"));
            Assert.IsTrue(statuses.All(status => status.CardId != primaryHeroPower));
            Assert.IsTrue(statuses.Any(status => status.Status == "Implemented"));
            Assert.IsTrue(discoverCardIds.All(cardId => statuses.Any(status => status.CardId == cardId)));
        }

        [Test]
        public void AnomalousTimeline_GrantsAlternateTimelineAsSecondHeroPower()
        {
            var service = CreateAnomalyService(
                "BG35_Anomaly_005",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var primaryHeroPower = service.State.Player.HeroPowerCardId;

            Assert.AreNotEqual("BG34_HERO_000p", primaryHeroPower);
            CollectionAssert.Contains(service.State.Player.ExtraHeroPowerCardIds, "BG34_HERO_000p");
            Assert.AreEqual(1, service.State.Player.ExtraHeroPowerUnlockRounds["BG34_HERO_000p"]);
        }

        [Test]
        public void AnomalousTimeline_SecondHeroPowerOpensMajorTimewarpOnTurnEight()
        {
            var service = CreateAnomalyService(
                "BG35_Anomaly_005",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var tavern = service.State.Player.Tavern;

            AdvanceToRound(service, 7);
            Assert.IsFalse(tavern.Timewarp.VisitOpen);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(8, service.State.Round);
            Assert.AreEqual("BG34_HERO_000p", service.State.Player.ExtraHeroPowerCardIds.Single());
            Assert.AreNotEqual("BG34_HERO_000p", service.State.Player.HeroPowerCardId);
            Assert.IsTrue(tavern.Timewarp.VisitOpen);
            Assert.AreEqual(TimewarpTavernPhase.Open, tavern.Timewarp.Phase);
            Assert.AreEqual(TimewarpKind.Major, tavern.Timewarp.PendingKind);
            Assert.AreEqual("murozond-major-timewarp", tavern.Timewarp.PendingSource);
        }

        [Test]
        public void GreaterPouches_GrantsGrowingCollectionAsSecondHeroPower()
        {
            var service = CreateAnomalyService(
                "BG32_Anomaly_001",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var primaryHeroPower = service.State.Player.HeroPowerCardId;

            Assert.AreNotEqual("BG32_HERO_002p", primaryHeroPower);
            CollectionAssert.Contains(service.State.Player.ExtraHeroPowerCardIds, "BG32_HERO_002p");
            Assert.AreEqual(1, service.State.Player.ExtraHeroPowerUnlockRounds["BG32_HERO_002p"]);
        }

        [Test]
        public void GreaterPouches_SecondHeroPowerOffersGreaterTrinketOnTurnEight()
        {
            var service = CreateAnomalyService(
                "BG32_Anomaly_001",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = true });
            var tavern = service.State.Player.Tavern;
            tavern.AdvancedMechanics.Trinkets.LesserTrinketId = "test-lesser-trinket";

            AdvanceToRound(service, 7);
            Assert.IsNull(tavern.AdvancedMechanics.PendingChoice);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var request = tavern.AdvancedMechanics.PendingChoice;
            Assert.AreEqual(8, service.State.Round);
            Assert.AreEqual("BG32_HERO_002p", service.State.Player.ExtraHeroPowerCardIds.Single());
            Assert.AreNotEqual("BG32_HERO_002p", service.State.Player.HeroPowerCardId);
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual(TrinketSlotKind.Greater.ToString(), request.Slot);
            Assert.AreEqual("hero-power:growing-collection", request.Source);
            Assert.IsTrue(request.Options.All(option => option.Slot == TrinketSlotKind.Greater.ToString()));
            Assert.IsTrue(request.Options.All(option =>
                service.TrinketCatalog.GetByCardId(option.SourceId).SlotKind == TrinketSlotKind.Greater));
        }

        [Test]
        public void MarinsTreasureBox_ReplacesSelectedHeroWithMarinAndGrantsGrowingCollection()
        {
            var service = CreateAnomalyService(
                "BG31_Anomaly_106",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var player = service.State.Player;

            Assert.AreEqual("BG30_HERO_304", player.HeroId);
            Assert.AreEqual("BG30_HERO_304p", player.HeroPowerCardId);
            Assert.AreEqual(30, player.Health);
            Assert.AreEqual(30, player.MaxHealth);
            Assert.AreEqual(12, player.Armor);
            CollectionAssert.Contains(player.ExtraHeroPowerCardIds, "BG32_HERO_002p");
            Assert.AreEqual(1, player.ExtraHeroPowerUnlockRounds["BG32_HERO_002p"]);
        }

        [Test]
        public void MarinsTreasureBox_GrowingCollectionOffersGreaterTrinketOnTurnEight()
        {
            var service = CreateAnomalyService(
                "BG31_Anomaly_106",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = true });
            var tavern = service.State.Player.Tavern;
            tavern.AdvancedMechanics.Trinkets.LesserTrinketId = "test-lesser-trinket";

            AdvanceToRound(service, 7);
            Assert.IsNull(tavern.AdvancedMechanics.PendingChoice);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var request = tavern.AdvancedMechanics.PendingChoice;
            Assert.AreEqual(8, service.State.Round);
            Assert.AreEqual("BG30_HERO_304p", service.State.Player.HeroPowerCardId);
            Assert.AreEqual("BG32_HERO_002p", service.State.Player.ExtraHeroPowerCardIds.Single());
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual(TrinketSlotKind.Greater.ToString(), request.Slot);
            Assert.AreEqual("hero-power:growing-collection", request.Source);
            Assert.IsTrue(request.Options.All(option => option.Slot == TrinketSlotKind.Greater.ToString()));
        }

        [Test]
        public void LesserFortune_GrantsLesserCrystalBallAsSecondHeroPower()
        {
            var service = CreateAnomalyService(
                "BG35_Anomaly_007",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var primaryHeroPower = service.State.Player.HeroPowerCardId;

            Assert.AreNotEqual("BG35_Anomaly_007t", primaryHeroPower);
            CollectionAssert.Contains(service.State.Player.ExtraHeroPowerCardIds, "BG35_Anomaly_007t");
            Assert.AreEqual(1, service.State.Player.ExtraHeroPowerUnlockRounds["BG35_Anomaly_007t"]);
        }

        [Test]
        public void LesserFortune_LesserCrystalBallCopiesFirstLesserTrinket()
        {
            var service = CreateAnomalyService(
                "BG35_Anomaly_007",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = true });
            var tavern = service.State.Player.Tavern;

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG30_MagicItem_414", CardKind.Minion, 0));

            var trinkets = tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual("BG30_MagicItem_414", trinkets.LesserTrinketId);
            Assert.AreEqual("BG30_MagicItem_414", trinkets.LesserCrystalBallCopiedTrinketId);
            Assert.IsFalse(service.State.Player.ExtraHeroPowerCardIds.Contains("BG35_Anomaly_007t"));
            Assert.IsFalse(service.State.Player.ExtraHeroPowerUnlockRounds.ContainsKey("BG35_Anomaly_007t"));
            Assert.IsTrue(tavern.AdvancedMechanics.Equipped.Any(equipped =>
                equipped.Kind == AdvancedMechanicKind.Trinket &&
                equipped.SourceId == "BG30_MagicItem_414" &&
                equipped.Slot == "HeroPower" &&
                equipped.DisplayName.Contains("Lesser Crystal Ball")));
        }

        [Test]
        public void GreaterFortune_GrantsGreaterCrystalBallAsSecondHeroPower()
        {
            var service = CreateAnomalyService(
                "BG35_Anomaly_008",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var primaryHeroPower = service.State.Player.HeroPowerCardId;

            Assert.AreNotEqual("BG35_Anomaly_008t", primaryHeroPower);
            CollectionAssert.Contains(service.State.Player.ExtraHeroPowerCardIds, "BG35_Anomaly_008t");
            Assert.AreEqual(1, service.State.Player.ExtraHeroPowerUnlockRounds["BG35_Anomaly_008t"]);
        }

        [Test]
        public void GreaterFortune_GreaterCrystalBallCopiesFirstGreaterTrinket()
        {
            var service = CreateAnomalyService(
                "BG35_Anomaly_008",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = true });
            var tavern = service.State.Player.Tavern;

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG30_MagicItem_996", CardKind.Minion, 1));

            var trinkets = tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual("BG30_MagicItem_996", trinkets.GreaterTrinketId);
            Assert.AreEqual("BG30_MagicItem_996", trinkets.GreaterCrystalBallCopiedTrinketId);
            Assert.IsFalse(service.State.Player.ExtraHeroPowerCardIds.Contains("BG35_Anomaly_008t"));
            Assert.IsFalse(service.State.Player.ExtraHeroPowerUnlockRounds.ContainsKey("BG35_Anomaly_008t"));
            Assert.IsTrue(tavern.AdvancedMechanics.Equipped.Any(equipped =>
                equipped.Kind == AdvancedMechanicKind.Trinket &&
                equipped.SourceId == "BG30_MagicItem_996" &&
                equipped.Slot == "HeroPower" &&
                equipped.DisplayName.Contains("Greater Crystal Ball")));
        }

        [Test]
        public void LesserPouches_GrantsFantasticTreasureAsSecondHeroPower()
        {
            var service = CreateAnomalyService(
                "BG32_Anomaly_002",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var primaryHeroPower = service.State.Player.HeroPowerCardId;

            Assert.AreNotEqual("BG30_HERO_304p", primaryHeroPower);
            CollectionAssert.Contains(service.State.Player.ExtraHeroPowerCardIds, "BG30_HERO_304p");
            Assert.AreEqual(1, service.State.Player.ExtraHeroPowerUnlockRounds["BG30_HERO_304p"]);
        }

        [Test]
        public void LesserPouches_SecondHeroPowerOffersLesserTrinketOnTurnFive()
        {
            var service = CreateAnomalyService(
                "BG32_Anomaly_002",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = true });
            var tavern = service.State.Player.Tavern;

            AdvanceToRound(service, 4);
            Assert.IsNull(tavern.AdvancedMechanics.PendingChoice);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var request = tavern.AdvancedMechanics.PendingChoice;
            Assert.AreEqual(5, service.State.Round);
            Assert.AreEqual("BG30_HERO_304p", service.State.Player.ExtraHeroPowerCardIds.Single());
            Assert.AreNotEqual("BG30_HERO_304p", service.State.Player.HeroPowerCardId);
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual(TrinketSlotKind.Lesser.ToString(), request.Slot);
            Assert.AreEqual("hero-power:fantastic-treasure", request.Source);
            Assert.IsTrue(request.Options.All(option => option.Slot == TrinketSlotKind.Lesser.ToString()));
            Assert.IsTrue(request.Options.All(option =>
                service.TrinketCatalog.GetByCardId(option.SourceId).SlotKind == TrinketSlotKind.Lesser));
        }

        [Test]
        public void AnomalousConflux_GrantsWarpedConfluxAsSecondHeroPower()
        {
            var service = CreateAnomalyService(
                "BG35_Anomaly_004",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var primaryHeroPower = service.State.Player.HeroPowerCardId;

            Assert.AreNotEqual("BG34_HERO_004p", primaryHeroPower);
            CollectionAssert.Contains(service.State.Player.ExtraHeroPowerCardIds, "BG34_HERO_004p");
            Assert.AreEqual(1, service.State.Player.ExtraHeroPowerUnlockRounds["BG34_HERO_004p"]);
        }

        [Test]
        public void AnomalousConflux_SecondHeroPowerOpensMinorTimewarpOnTurnFive()
        {
            var service = CreateAnomalyService(
                "BG35_Anomaly_004",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var tavern = service.State.Player.Tavern;

            AdvanceToRound(service, 4);
            Assert.IsFalse(tavern.Timewarp.VisitOpen);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(5, service.State.Round);
            Assert.AreEqual("BG34_HERO_004p", service.State.Player.ExtraHeroPowerCardIds.Single());
            Assert.AreNotEqual("BG34_HERO_004p", service.State.Player.HeroPowerCardId);
            Assert.IsTrue(tavern.Timewarp.VisitOpen);
            Assert.AreEqual(TimewarpTavernPhase.Open, tavern.Timewarp.Phase);
            Assert.AreEqual(TimewarpKind.Minor, tavern.Timewarp.PendingKind);
            Assert.AreEqual("morchie-minor-timewarp", tavern.Timewarp.PendingSource);
        }

        [Test]
        public void AnomalousCube_GrantsMysteryCubeAsLockedSecondHeroPower()
        {
            var service = CreateAnomalyService(
                "BG35_Anomaly_002",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = false });
            var primaryHeroPower = service.State.Player.HeroPowerCardId;

            Assert.AreNotEqual("BG35_Anomaly_002t", primaryHeroPower);
            CollectionAssert.Contains(service.State.Player.ExtraHeroPowerCardIds, "BG35_Anomaly_002t");
            Assert.AreEqual(5, service.State.Player.ExtraHeroPowerUnlockRounds["BG35_Anomaly_002t"]);
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower, -1, TargetZone.Unspecified, heroPowerCardId: "BG35_Anomaly_002t")));
        }

        [Test]
        public void AnomalousCube_TurnFiveOffersHeroPowerSlotLesserTrinketReplacement()
        {
            var service = CreateAnomalyService(
                "BG35_Anomaly_002",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = true });
            var tavern = service.State.Player.Tavern;

            AdvanceToRound(service, 4);
            Assert.IsNull(tavern.AdvancedMechanics.PendingChoice);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var request = tavern.AdvancedMechanics.PendingChoice;
            Assert.AreEqual(5, service.State.Round);
            Assert.AreEqual("BG35_Anomaly_002t", service.State.Player.ExtraHeroPowerCardIds.Single());
            Assert.AreNotEqual("BG35_Anomaly_002t", service.State.Player.HeroPowerCardId);
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual(TrinketSlotKind.Lesser.ToString(), request.Slot);
            Assert.AreEqual("hero-power:mystery-cube", request.Source);
            Assert.AreEqual(2, request.Options.Count);
            Assert.IsTrue(request.Options.All(option => option.Slot == TrinketSlotKind.Lesser.ToString()));
            Assert.IsTrue(request.Options.All(option =>
                service.TrinketCatalog.GetByCardId(option.SourceId).SlotKind == TrinketSlotKind.Lesser));
            Assert.IsFalse(request.Options.Any(option => option.SourceId == "BG30_MagicItem_703"));
        }

        [Test]
        public void AnomalousCube_SelectedLesserTrinketOccupiesHeroPowerSlotAndRepeatsEachTurn()
        {
            var service = CreateAnomalyService(
                "BG35_Anomaly_002",
                new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_34", EnableTrinkets = true });
            var tavern = service.State.Player.Tavern;

            AdvanceToRound(service, 5);
            var picked = tavern.AdvancedMechanics.PendingChoice.Options[0].SourceId;
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var trinkets = tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(picked, trinkets.MysteryCubeHeroPowerTrinketId);
            Assert.IsTrue(string.IsNullOrEmpty(trinkets.LesserTrinketId));
            Assert.IsFalse(service.State.Player.ExtraHeroPowerCardIds.Contains("BG35_Anomaly_002t"));
            Assert.IsFalse(service.State.Player.ExtraHeroPowerUnlockRounds.ContainsKey("BG35_Anomaly_002t"));
            Assert.IsTrue(tavern.AdvancedMechanics.Equipped.Any(equipped =>
                equipped.Kind == AdvancedMechanicKind.Trinket &&
                equipped.SourceId == picked &&
                equipped.Slot == "HeroPower" &&
                equipped.DisplayName.Contains("Mystery Cube")));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var nextRequest = tavern.AdvancedMechanics.PendingChoice;
            Assert.AreEqual(6, service.State.Round);
            Assert.IsNotNull(nextRequest);
            Assert.AreEqual("hero-power:mystery-cube", nextRequest.Source);
            Assert.AreEqual(2, nextRequest.Options.Count);
            Assert.IsFalse(nextRequest.Options.Any(option => option.SourceId == picked));
        }

        [Test]
        public void DoubleHeader_FirstBoughtCardCreatesNonPoolExactCopy()
        {
            var service = CreateDoubleHeaderService();
            var tavern = service.State.Player.Tavern;
            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var target = tavern.Shop[shopIndex];
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));

            var copies = tavern.Hand.Where(card => card.DefinitionId == target.DefinitionId).ToList();
            Assert.AreEqual(2, copies.Count);
            Assert.AreEqual(1, copies.Count(card => card.PoolCopiesHeld == 1));
            Assert.AreEqual(1, copies.Count(card => card.PoolCopiesHeld == 0 && card.PoolSource == PoolSource.Copy));
        }

        [Test]
        public void DoubleHeader_CopyCanTripleButDoesNotReturnExtraPoolCopies()
        {
            var service = CreateDoubleHeaderService();
            var tavern = service.State.Player.Tavern;
            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var target = tavern.Shop[shopIndex];
            var existing = target.Clone();
            existing.InstanceId = "test-existing-" + existing.DefinitionId;
            existing.Owner = BoardSide.Player;
            existing.PoolSource = PoolSource.Pool;
            existing.OriginPoolSource = PoolSource.Pool;
            existing.PoolCopiesHeld = 1;
            existing.CanReturnToPoolAfterAttach = true;
            tavern.Hand.Add(existing);
            tavern.Pool[target.DefinitionId] = tavern.Pool[target.DefinitionId] - 1;
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));

            var golden = tavern.Hand.Single(card => card.DefinitionId == target.DefinitionId && card.Golden);
            Assert.AreEqual(2, golden.PoolCopiesHeld);

            var poolBeforeSell = tavern.Pool[target.DefinitionId];
            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.IndexOf(golden)));
            service.Apply(new GameCommand(GameCommandType.SellMinion, golden.InstanceId));

            Assert.AreEqual(poolBeforeSell + 2, tavern.Pool[target.DefinitionId]);
        }

        [Test]
        public void GrapnelOfTheTitans_FirstMinionEachTurnIsFree()
        {
            var service = CreateAnomalyService("BG27_Anomaly_303");
            var tavern = service.State.Player.Tavern;
            var minionIndexes = tavern.Shop
                .Select((card, index) => new { card, index })
                .Where(slot => slot.card != null && slot.card.CardKind == CardKind.Minion)
                .Select(slot => slot.index)
                .Take(2)
                .ToList();
            Assert.GreaterOrEqual(minionIndexes.Count, 2);
            tavern.Gold = 3;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, minionIndexes[0]));

            Assert.AreEqual(3, tavern.Gold);

            service.Apply(new GameCommand(GameCommandType.BuyMinion, minionIndexes[1]));

            Assert.AreEqual(0, tavern.Gold);
        }

        [Test]
        public void GolgannethTempest_MinionsCostTwoAndBuyingRefreshesShop()
        {
            var service = CreateAnomalyService("BG27_Anomaly_900");
            var tavern = service.State.Player.Tavern;
            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            Assert.GreaterOrEqual(shopIndex, 0);
            tavern.Gold = 2;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));

            Assert.AreEqual(0, tavern.Gold);
            Assert.IsNotNull(tavern.Shop[shopIndex]);
        }

        [Test]
        public void GolgannethTempest_ManualRefreshIsBlocked()
        {
            var service = CreateAnomalyService("BG27_Anomaly_900");
            service.State.Player.Tavern.Gold = 10;

            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.RerollShop)));
        }

        [Test]
        public void PerfectedAlchemy_StartsWithGoldenizer()
        {
            var service = CreateAnomalyService("BG27_Anomaly_751");
            var goldenizer = service.State.Player.Tavern.Hand.Single(card => card.CardId == "98914");

            Assert.AreEqual(CardKind.TavernSpell, goldenizer.CardKind);
            Assert.AreEqual("Goldenizer", goldenizer.Name);
            Assert.AreEqual(0, goldenizer.Cost);
            Assert.AreEqual(PoolSource.Copy, goldenizer.PoolSource);
        }

        [Test]
        public void ScoutsHonor_StartsWithGoldenPatientScoutThatImprovesAndDiscoversTwice()
        {
            var service = CreateAnomalyService("BG31_Anomaly_120");
            var scout = service.State.Player.Board.Single(card => card.CardId == "BG24_715");

            Assert.AreEqual("bg24_715", scout.DefinitionId);
            Assert.IsTrue(scout.Golden);
            Assert.AreEqual(2, scout.Attack);
            Assert.AreEqual(2, scout.MaxHealth);
            Assert.AreEqual(PoolSource.Copy, scout.PoolSource);
            Assert.AreEqual(0, scout.PoolCopiesHeld);
            Assert.IsFalse(scout.Tags.Contains("anomaly_proxy"));
            Assert.IsFalse(string.IsNullOrEmpty(scout.ImagePath));
            Assert.AreEqual(1, scout.Counters["patient-scout-tier"]);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(2, scout.Counters["patient-scout-tier"]);

            service.Apply(new GameCommand(GameCommandType.SellMinion, scout.InstanceId));
            Assert.AreEqual(2, service.State.Player.Tavern.Discover.RemainingPicks);
            Assert.AreEqual(2, service.State.Player.Tavern.Discover.RewardTier);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(1, service.State.Player.Tavern.Discover.RemainingPicks);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            Assert.IsNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.Minion));
        }

        [TestCase("BG27_Anomaly_572", 5, 3)]
        [TestCase("BG27_Anomaly_570", 7, 5)]
        [TestCase("BG27_Anomaly_571", 8, 6)]
        public void TreasureHoard_TargetRoundDiscoversGoldenTierMinion(string anomalyCardId, int dueRound, int rewardTier)
        {
            var service = CreateAnomalyService(anomalyCardId);

            AdvanceToRound(service, dueRound - 1);
            Assert.IsNull(service.State.Player.Tavern.Discover);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var discover = service.State.Player.Tavern.Discover;
            Assert.IsNotNull(discover);
            Assert.AreEqual(rewardTier, discover.RewardTier);
            Assert.AreEqual(3, discover.Options.Count);
            Assert.IsTrue(discover.Options.All(card => card.Golden && card.TavernTier == rewardTier));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            var reward = service.State.Player.Tavern.Hand.Last();
            Assert.IsTrue(reward.Golden);
            Assert.AreEqual(rewardTier, reward.TavernTier);
            Assert.IsTrue(reward.Tags.Contains("anomaly_treasure_hoard"));
        }

        [Test]
        public void GoldenArrow_EveryThirdTurnAddsGoldenArrowThatBuffsEightAttack()
        {
            var service = CreateAnomalyService("BG31_Anomaly_124");
            var tavern = service.State.Player.Tavern;

            AdvanceToRound(service, 2);
            Assert.AreEqual(0, tavern.Hand.Count(card => card.CardId == "100596"));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var arrow = tavern.Hand.Single(card => card.CardId == "100596");
            Assert.IsTrue(arrow.Golden);
            Assert.AreEqual("Golden Arrow", arrow.Name);
            Assert.AreEqual("Give a minion +8 Attack.", arrow.Text);
            Assert.IsTrue(arrow.Tags.Contains("anomaly_golden_arrow"));

            var target = new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "golden-arrow-target",
                DefinitionId = "golden-arrow-target",
                CardId = "GOLDEN_ARROW_TARGET",
                Name = "Golden Arrow Target",
                BaseAttack = 2,
                Attack = 2,
                BaseHealth = 3,
                Health = 3,
                MaxHealth = 3,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
            tavern.Hand.Remove(arrow);
            tavern.Hand.Add(arrow);
            service.State.Player.Board.Add(target);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.IndexOf(arrow), 0));

            Assert.AreEqual(10, target.Attack);
            Assert.AreEqual(3, target.MaxHealth);
        }

        [Test]
        public void FlyTheFlag_EveryThirdTurnAddsTargetedSpell()
        {
            var service = CreateAnomalyService("BG35_Anomaly_001");
            var tavern = service.State.Player.Tavern;

            AdvanceToRound(service, 2);
            Assert.AreEqual(0, tavern.Hand.Count(card => card.CardId == "FLY_THE_FLAG_SPELL"));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var spell = tavern.Hand.Single(card => card.CardId == "FLY_THE_FLAG_SPELL");
            Assert.AreEqual("Fly the Flag", spell.Name);
            Assert.IsTrue(spell.Tags.Contains("anomaly_fly_the_flag"));
        }

        [Test]
        public void FlyTheFlag_RejectsNonMinionTavernTargets()
        {
            var service = CreateAnomalyService("BG35_Anomaly_001");
            var tavern = service.State.Player.Tavern;
            AdvanceToRound(service, 3);
            var spellIndex = tavern.Hand.FindIndex(card => card.CardId == "FLY_THE_FLAG_SPELL");
            tavern.Shop[0] = new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "fly-the-flag-non-minion-target",
                DefinitionId = "fly-the-flag-non-minion-target",
                CardId = "fly-the-flag-non-minion-target",
                Name = "Non-Minion Target",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
            Assert.GreaterOrEqual(spellIndex, 0);

            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(
                    GameCommandType.PlayMinion,
                    spellIndex,
                    0,
                    TargetZone.TavernShop,
                    -1,
                    TargetZone.Unspecified)));

            Assert.AreEqual(1, tavern.Hand.Count(card => card.CardId == "FLY_THE_FLAG_SPELL"));
            Assert.IsTrue(tavern.RecruitLog.Any(entry => entry.Message.Contains("Fly the Flag: rejected target")));
        }

        [Test]
        public void FlyTheFlag_RejectsMinionsOutsideTheNormalTavernPool()
        {
            var service = CreateAnomalyService("BG35_Anomaly_001");
            var tavern = service.State.Player.Tavern;
            AdvanceToRound(service, 3);
            var spellIndex = tavern.Hand.FindIndex(card => card.CardId == "FLY_THE_FLAG_SPELL");
            tavern.Shop[0] = new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "fly-the-flag-non-pool-target",
                DefinitionId = "bg26_800",
                CardId = "BG26_800",
                Name = "Non-Pool Target",
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.Beast },
                Keywords = new List<Keyword>(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };

            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(
                    GameCommandType.PlayMinion,
                    spellIndex,
                    0,
                    TargetZone.TavernShop,
                    -1,
                    TargetZone.Unspecified)));

            Assert.AreEqual(1, tavern.Hand.Count(card => card.CardId == "FLY_THE_FLAG_SPELL"));
            Assert.IsFalse(tavern.Pool.ContainsKey("bg26_800"));
        }

        [Test]
        public void FlyTheFlag_AddsTwelveCopiesToTavernPoolAndRefreshCanOfferThem()
        {
            var service = CreateAnomalyService("BG35_Anomaly_001");
            var tavern = service.State.Player.Tavern;
            AdvanceToRound(service, 3);
            var spellIndex = tavern.Hand.FindIndex(card => card.CardId == "FLY_THE_FLAG_SPELL");
            var targetIndex = tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var target = tavern.Shop[targetIndex];
            var before = tavern.Pool[target.DefinitionId];

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                spellIndex,
                targetIndex,
                TargetZone.TavernShop,
                -1,
                TargetZone.Unspecified));

            Assert.AreEqual(before + 12, tavern.Pool[target.DefinitionId]);
            Assert.GreaterOrEqual(tavern.PoolCapacities[target.DefinitionId], before + 13);
            Assert.IsTrue(tavern.AdvancedMechanics.Anomalies.AppliedPoolModifiers.Any(value => value.Contains(target.DefinitionId)));

            tavern.Shop.Clear();
            tavern.ShopSlots.Clear();
            ForceOnlyPoolTarget(tavern, target.DefinitionId, 12);
            tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.IsTrue(tavern.Shop.Any(card => card != null && card.DefinitionId == target.DefinitionId));
        }

        [Test]
        public void FlyTheFlag_BoughtInjectedCopiesSellBackToInjectedPoolCapacity()
        {
            var service = CreateAnomalyService("BG35_Anomaly_001");
            var tavern = service.State.Player.Tavern;
            var target = tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
            tavern.Shop.Clear();
            tavern.ShopSlots.Clear();
            ForceOnlyPoolTarget(tavern, target.DefinitionId, 12);
            tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.RerollShop));
            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.DefinitionId == target.DefinitionId);
            Assert.GreaterOrEqual(shopIndex, 0);
            var poolBeforeBuy = tavern.Pool[target.DefinitionId];

            service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));
            var bought = tavern.Hand.Single(card => card.DefinitionId == target.DefinitionId);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.IndexOf(bought)));
            service.Apply(new GameCommand(GameCommandType.SellMinion, bought.InstanceId));

            Assert.AreEqual(poolBeforeBuy + 1, tavern.Pool[target.DefinitionId]);
        }

        [Test]
        public void FlyTheFlag_InjectedCopiesTripleAndReturnHeldPoolCopies()
        {
            var service = CreateAnomalyService("BG35_Anomaly_001");
            var tavern = service.State.Player.Tavern;
            var target = tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
            tavern.Shop.Clear();
            tavern.ShopSlots.Clear();
            ForceOnlyPoolTarget(tavern, target.DefinitionId, 12);
            tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.RerollShop));
            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.DefinitionId == target.DefinitionId);
            Assert.GreaterOrEqual(shopIndex, 0);
            var first = tavern.Shop[shopIndex].Clone();
            first.InstanceId = "fly-the-flag-existing-a";
            first.Owner = BoardSide.Player;
            var second = tavern.Shop[shopIndex].Clone();
            second.InstanceId = "fly-the-flag-existing-b";
            second.Owner = BoardSide.Player;
            tavern.Hand.Add(first);
            tavern.Hand.Add(second);
            tavern.Pool[target.DefinitionId] -= 2;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));

            var golden = tavern.Hand.Single(card => card.DefinitionId == target.DefinitionId && card.Golden);
            Assert.AreEqual(3, golden.PoolCopiesHeld);

            var poolBeforeSell = tavern.Pool[target.DefinitionId];
            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.IndexOf(golden)));
            service.Apply(new GameCommand(GameCommandType.SellMinion, golden.InstanceId));

            Assert.AreEqual(poolBeforeSell + 3, tavern.Pool[target.DefinitionId]);
        }

        [Test]
        public void OathstoneSummoning_TurnSevenInjectsOnlyCurrentMinorTimewarpedMinions()
        {
            var service = CreateAnomalyService("BG34_Anomaly_805");
            var tavern = service.State.Player.Tavern;
            var catalog = TimewarpedTavernCatalogLoader.LoadFromResources();
            var minorIds = catalog.Minor
                .Where(card => card.CardKind == CardKind.Minion)
                .Select(card => OathstonePoolId(card.CardId))
                .ToList();
            var nonMinionIds = catalog.NonMinions
                .Select(card => OathstonePoolId(card.CardId))
                .ToList();

            AdvanceToRound(service, 6);
            Assert.IsFalse(minorIds.Any(id => tavern.Pool.ContainsKey(id)));

            AdvanceToRound(service, 7);

            Assert.Greater(minorIds.Count, 0);
            Assert.IsTrue(minorIds.All(id => tavern.Pool.ContainsKey(id) && tavern.Pool[id] == 1));
            Assert.IsTrue(minorIds.All(id => tavern.PoolCapacities.ContainsKey(id) && tavern.PoolCapacities[id] == 1));
            Assert.IsFalse(nonMinionIds.Any(id => tavern.Pool.ContainsKey(id)));
            Assert.IsTrue(tavern.AdvancedMechanics.Anomalies.AppliedPoolModifiers.Any(value => value.StartsWith("Oathstone:Minor:")));
        }

        [Test]
        public void OathstoneSummoning_TurnTenInjectsCurrentMajorTimewarpedMinions()
        {
            var service = CreateAnomalyService("BG34_Anomaly_805");
            var tavern = service.State.Player.Tavern;
            var catalog = TimewarpedTavernCatalogLoader.LoadFromResources();
            var majorIds = catalog.Major
                .Where(card => card.CardKind == CardKind.Minion)
                .Select(card => OathstonePoolId(card.CardId))
                .ToList();

            AdvanceToRound(service, 10);

            Assert.Greater(majorIds.Count, 0);
            Assert.IsTrue(majorIds.All(id => tavern.Pool.ContainsKey(id) && tavern.Pool[id] == 1));
            Assert.IsTrue(majorIds.All(id => tavern.PoolCapacities.ContainsKey(id) && tavern.PoolCapacities[id] == 1));
            Assert.IsTrue(tavern.AdvancedMechanics.Anomalies.AppliedPoolModifiers.Any(value => value.StartsWith("Oathstone:Major:")));
        }

        [Test]
        public void OathstoneSummoning_DefaultPoolDoesNotInjectHistoricalTimewarpedMinions()
        {
            var service = CreateAnomalyService("BG34_Anomaly_805");
            var tavern = service.State.Player.Tavern;
            var catalog = TimewarpedTavernCatalogLoader.LoadFromResources();
            var historicalIds = catalog.HistoricalExtra
                .Where(card => card.CardKind == CardKind.Minion)
                .Select(card => OathstonePoolId(card.CardId))
                .ToList();

            AdvanceToRound(service, 10);

            Assert.Greater(historicalIds.Count, 0);
            Assert.IsFalse(historicalIds.Any(id => tavern.Pool.ContainsKey(id)));
        }

        [Test]
        public void OathstoneSummoning_HistoricalPoolSettingInjectsHistoricalMinions()
        {
            var service = CreateAnomalyService(
                "BG34_Anomaly_805",
                new MatchSetupOptions
                {
                    UseHistoricalTimewarpedPool = true
                });
            var tavern = service.State.Player.Tavern;
            var historicalMinorIds = service.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor)
                .Where(card => card.CardKind == CardKind.Minion && card.PoolStatus == "historical_extra")
                .Select(card => OathstonePoolId(card.CardId))
                .ToList();

            AdvanceToRound(service, 7);

            Assert.Greater(historicalMinorIds.Count, 0);
            Assert.IsTrue(historicalMinorIds.All(id => tavern.Pool.ContainsKey(id) && tavern.Pool[id] == 1));
        }

        [Test]
        public void OathstoneSummoning_InjectedTimewarpedMinionsCanAppearAfterRefresh()
        {
            var service = CreateAnomalyService("BG34_Anomaly_805");
            var tavern = service.State.Player.Tavern;
            var target = TimewarpedTavernCatalogLoader.LoadFromResources()
                .Minor
                .First(card => card.CardKind == CardKind.Minion);
            var targetId = OathstonePoolId(target.CardId);

            AdvanceToRound(service, 7);
            tavern.Shop.Clear();
            tavern.ShopSlots.Clear();
            tavern.Tier = 3;
            tavern.Gold = 10;
            ForceOnlyPoolTarget(tavern, targetId, 1);

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.IsTrue(tavern.Shop.Any(card =>
                card != null &&
                card.CardKind == CardKind.Minion &&
                card.DefinitionId == targetId &&
                card.CardId == target.CardId &&
                card.Tags.Contains("oathstone_summoning")));
        }

        [Test]
        public void SecretsOfNorgannon_StartsWithArmorAndTierSevenPoolCopies()
        {
            var service = CreateAnomalyService("BG27_Anomaly_504");
            var tavern = service.State.Player.Tavern;
            var legalTierSeven = LegalTierSevenDefinitions(service);

            Assert.AreEqual(10, service.State.Player.Armor);
            Assert.Greater(legalTierSeven.Count, 0);
            Assert.IsTrue(legalTierSeven.All(definition =>
                tavern.Pool.ContainsKey(definition.Id) &&
                tavern.Pool[definition.Id] == 5 &&
                tavern.PoolCapacities.ContainsKey(definition.Id) &&
                tavern.PoolCapacities[definition.Id] == 5));
        }

        [Test]
        public void SecretsOfNorgannon_AllowsUpgradeToTierSeven()
        {
            var service = CreateAnomalyService("BG27_Anomaly_504");
            var tavern = service.State.Player.Tavern;

            UpgradeToTier(service, 7);

            Assert.AreEqual(7, tavern.Tier);
            Assert.AreEqual(0, tavern.UpgradeCost);
        }

        [Test]
        public void SecretsOfNorgannon_TierSevenShopCanOfferPoolMinions()
        {
            var service = CreateAnomalyService("BG27_Anomaly_504");
            var tavern = service.State.Player.Tavern;
            var target = LegalTierSevenDefinitions(service).First();

            UpgradeToTier(service, 7);
            tavern.Shop.Clear();
            tavern.ShopSlots.Clear();
            tavern.Gold = 10;
            ForceOnlyPoolTarget(tavern, target.Id, 1);

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.IsTrue(tavern.Shop.Any(card =>
                card != null &&
                card.CardKind == CardKind.Minion &&
                card.DefinitionId == target.Id &&
                card.TavernTier == 7));
        }

        [Test]
        public void SecretsOfNorgannon_TripleRewardDiscoversTierSeven()
        {
            var service = CreateAnomalyService("BG27_Anomaly_504");
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 6;
            tavern.Hand.Clear();
            tavern.Hand.Add(TripleRewardCard());

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual(7, tavern.Discover.RewardTier);
            Assert.AreEqual(3, tavern.Discover.Options.Count);
            Assert.IsTrue(tavern.Discover.Options.All(card => card.TavernTier == 7));
        }

        [Test]
        public void SecretsOfNorgannon_PatientScoutCanDiscoverTierSeven()
        {
            var service = CreateAnomalyService("BG27_Anomaly_504");
            var scout = PatientScout("secrets-scout", 7);
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(scout);

            service.Apply(new GameCommand(GameCommandType.SellMinion, scout.InstanceId));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(7, service.State.Player.Tavern.Discover.RewardTier);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(card => card.TavernTier == 7));
        }

        [Test]
        public void SecretsOfNorgannon_RespectsTierSevenTribeFiltering()
        {
            var service = CreateAnomalyService(
                "BG27_Anomaly_504",
                new MatchSetupOptions
                {
                    ActiveTribes = new List<Tribe>
                    {
                        Tribe.Beast,
                        Tribe.Demon,
                        Tribe.Dragon,
                        Tribe.Elemental,
                        Tribe.Mech,
                        Tribe.Naga,
                        Tribe.Pirate,
                        Tribe.Quilboar,
                        Tribe.Undead
                    }
                });
            var tavern = service.State.Player.Tavern;
            var inactiveMurlocTierSeven = MinionCatalogLoader.LoadFromResources()
                .All
                .Where(definition =>
                    definition.InPool &&
                    definition.TavernTier == TavernRules.MaxTavernTier &&
                    definition.Tribes.Contains(Tribe.Murloc))
                .ToList();

            Assert.Greater(inactiveMurlocTierSeven.Count, 0);
            Assert.IsFalse(inactiveMurlocTierSeven.Any(definition => tavern.PoolCapacities.ContainsKey(definition.Id)));
        }

        [Test]
        public void TierSevenStaysUnavailableWhenSecretsOfNorgannonDisabled()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 6;
            tavern.Gold = 100;

            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UpgradeTavern)));

            tavern.Hand.Clear();
            tavern.Hand.Add(TripleRewardCard());
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual(6, tavern.Discover.RewardTier);
            Assert.IsFalse(tavern.Discover.Options.Any(card => card.TavernTier == 7));
        }

        [Test]
        public void FalseIdols_TwoCopiesTripleAndGoldenGrantsTavernCoin()
        {
            var service = CreateAnomalyService("BG27_Anomaly_301");
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            tavern.Hand.Add(TestMinion("false-idols-a", "false-idols-test"));
            tavern.Hand.Add(TestMinion("false-idols-b", "false-idols-test"));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            var golden = tavern.Hand.Single(card => card.DefinitionId == "false-idols-test" && card.Golden);
            Assert.AreEqual(0, service.State.Player.Board.Count(card => card.DefinitionId == "false-idols-test"));
            Assert.AreEqual(0, tavern.Hand.Count(card => card.DefinitionId == "triple-reward"));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.IndexOf(golden)));

            Assert.AreEqual(1, tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell && card.CardId == "104436"));
            Assert.AreEqual(0, tavern.Hand.Count(card => card.DefinitionId == "triple-reward"));
            Assert.AreEqual(1, service.State.Player.Board.Count(card => card.DefinitionId == "false-idols-test" && card.Golden));
        }

        [Test]
        public void AnomalousExpedition_DiscoversDelayedRewardsAndGrantsAtTiers()
        {
            var service = CreateAnomalyService("BG35_Anomaly_006");
            var tavern = service.State.Player.Tavern;

            var tier6CardId = ChooseAnomalousExpeditionReward(service, 6);
            var tier4CardId = ChooseAnomalousExpeditionReward(service, 4);
            var tier2CardId = ChooseAnomalousExpeditionReward(service, 2);

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(0, tavern.Hand.Count);
            Assert.AreEqual(tier6CardId, tavern.AdvancedMechanics.Anomalies.Flags["anomalous_expedition_tier_6_card_id"]);
            Assert.AreEqual(tier4CardId, tavern.AdvancedMechanics.Anomalies.Flags["anomalous_expedition_tier_4_card_id"]);
            Assert.AreEqual(tier2CardId, tavern.AdvancedMechanics.Anomalies.Flags["anomalous_expedition_tier_2_card_id"]);

            UpgradeToTier(service, 2);
            AssertAnomalousExpeditionReward(tavern, tier2CardId, 2);
            Assert.AreEqual(1, tavern.Hand.Count(card => card.Tags.Contains("anomaly_anomalous_expedition")));

            UpgradeToTier(service, 3);
            Assert.AreEqual(1, tavern.Hand.Count(card => card.Tags.Contains("anomaly_anomalous_expedition")));

            UpgradeToTier(service, 4);
            AssertAnomalousExpeditionReward(tavern, tier4CardId, 4);
            Assert.AreEqual(2, tavern.Hand.Count(card => card.Tags.Contains("anomaly_anomalous_expedition")));

            UpgradeToTier(service, 6);
            AssertAnomalousExpeditionReward(tavern, tier6CardId, 6);
            Assert.AreEqual(3, tavern.Hand.Count(card => card.Tags.Contains("anomaly_anomalous_expedition")));
        }

        [Test]
        public void DarkmoonFairePrizes_TriggersEveryFourTurnsAndScalesPrizeTier()
        {
            var service = CreateAnomalyService("BG27_Anomaly_Prizes2");
            var tavern = service.State.Player.Tavern;

            AdvanceToRound(service, 3);
            Assert.IsNull(tavern.Discover);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(4, service.State.Round);
            AssertDarkmoonPrizeDiscover(tavern, "anomaly-darkmoon-faire-prizes", 1, false);
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            AdvanceToRound(service, 7);
            Assert.IsNull(tavern.Discover);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(8, service.State.Round);
            AssertDarkmoonPrizeDiscover(tavern, "anomaly-darkmoon-faire-prizes", 2, false);
        }

        [Test]
        public void UpPrizing_UpgradeStartsPrizeDiscoverAndImprovesAfterThreeTurns()
        {
            var service = CreateAnomalyService("BG27_Anomaly_716");
            var tavern = service.State.Player.Tavern;
            tavern.Gold = 100;

            service.Apply(new GameCommand(GameCommandType.UpgradeTavern));

            Assert.AreEqual(2, tavern.Tier);
            AssertDarkmoonPrizeDiscover(tavern, "anomaly-up-prizing", 1, false);
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            AdvanceToRound(service, 4);
            tavern.Gold = 100;
            service.Apply(new GameCommand(GameCommandType.UpgradeTavern));

            Assert.AreEqual(3, tavern.Tier);
            AssertDarkmoonPrizeDiscover(tavern, "anomaly-up-prizing", 2, false);
        }

        [Test]
        public void BringInTheBuddies_StartsDiscoverableBuddiesInSeparatePool()
        {
            var service = CreateAnomalyService("BG27_Anomaly_810");
            var tavern = service.State.Player.Tavern;
            var buddies = DiscoverableBuddies();

            Assert.Greater(buddies.Count, 0);
            Assert.AreEqual(buddies.Count, tavern.BuddyPool.Count);
            foreach (var buddy in buddies)
            {
                Assert.AreEqual(6, tavern.BuddyPool[buddy.CardId]);
                Assert.AreEqual(6, tavern.BuddyPoolCapacities[buddy.CardId]);
                Assert.IsFalse(tavern.Pool.ContainsKey(buddy.CardId));
            }
        }

        [Test]
        public void BringInTheBuddies_ReportsBuddyCandidateImplementationStatuses()
        {
            var service = CreateAnomalyService("BG27_Anomaly_810");
            var statuses = service.GetBuddyPoolCandidateImplementationStatuses();

            Assert.Greater(statuses.Count, 0);
            Assert.IsTrue(statuses.All(status => status.Source == "BuddyPool"));
            Assert.IsTrue(statuses.Any(status => status.Status == "Implemented"));
            Assert.IsTrue(statuses.All(status => !string.IsNullOrEmpty(status.CardId)));

            var bigglesworth = statuses.FirstOrDefault(status => status.CardId == "TB_BaconShop_HERO_70_Buddy");
            if (bigglesworth != null)
            {
                Assert.AreEqual("FrameworkFirst", bigglesworth.Status);
            }
        }

        [Test]
        public void BringInTheBuddies_ShopCanOfferBuddyCardsFromBuddyPool()
        {
            var service = CreateAnomalyService("BG27_Anomaly_810");
            var tavern = service.State.Player.Tavern;
            var buddy = DiscoverableBuddies().First();
            ForceOnlyBuddyTarget(tavern, buddy.CardId, 1);
            tavern.Tier = System.Math.Max(TavernRules.MinTavernTier, buddy.TavernTier);
            tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var offered = tavern.Shop.Single(card => card != null && card.DefinitionId == buddy.CardId);
            Assert.AreEqual(CardKind.HeroBuddy, offered.CardKind);
            Assert.AreEqual(PoolSource.Buddy, offered.PoolSource);
            Assert.AreEqual(1, offered.PoolCopiesHeld);
            Assert.IsTrue(offered.Tags.Contains("buddy_pool"));
            Assert.AreEqual(0, tavern.BuddyPool[buddy.CardId]);
            Assert.IsFalse(tavern.Pool.ContainsKey(buddy.CardId) && tavern.Pool[buddy.CardId] > 0);
        }

        [Test]
        public void BringInTheBuddies_BuyAndSellReturnToBuddyPoolOnly()
        {
            var service = CreateAnomalyService("BG27_Anomaly_810");
            var tavern = service.State.Player.Tavern;
            var buddy = DiscoverableBuddies().First();
            ForceOnlyBuddyTarget(tavern, buddy.CardId, 1);
            tavern.Tier = System.Math.Max(TavernRules.MinTavernTier, buddy.TavernTier);
            tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.RerollShop));
            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.DefinitionId == buddy.CardId);

            service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));
            var bought = tavern.Hand.Single(card => card.DefinitionId == buddy.CardId);
            Assert.AreEqual(0, tavern.BuddyPool[buddy.CardId]);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.IndexOf(bought)));
            service.Apply(new GameCommand(GameCommandType.SellMinion, bought.InstanceId));

            Assert.AreEqual(1, tavern.BuddyPool[buddy.CardId]);
            Assert.IsFalse(tavern.Pool.ContainsKey(buddy.CardId) && tavern.Pool[buddy.CardId] > 0);
        }

        [Test]
        public void BringInTheBuddies_TripleReturnsHeldCopiesToBuddyPool()
        {
            var service = CreateAnomalyService("BG27_Anomaly_810");
            var tavern = service.State.Player.Tavern;
            var buddy = DiscoverableBuddies().First();
            ForceOnlyBuddyTarget(tavern, buddy.CardId, 3);
            tavern.Tier = System.Math.Max(TavernRules.MinTavernTier, buddy.TavernTier);
            tavern.Gold = 20;
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            for (var index = 0; index < 3; index += 1)
            {
                var shopIndex = tavern.Shop.FindIndex(card => card != null && card.DefinitionId == buddy.CardId);
                Assert.GreaterOrEqual(shopIndex, 0);
                service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));
            }

            var golden = tavern.Hand.Single(card => card.DefinitionId == buddy.CardId && card.Golden);
            Assert.AreEqual(PoolSource.Buddy, golden.PoolSource);
            Assert.AreEqual(3, golden.PoolCopiesHeld);
            Assert.AreEqual(0, tavern.BuddyPool[buddy.CardId]);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.IndexOf(golden)));
            service.Apply(new GameCommand(GameCommandType.SellMinion, golden.InstanceId));

            Assert.AreEqual(3, tavern.BuddyPool[buddy.CardId]);
            Assert.IsFalse(tavern.Pool.ContainsKey(buddy.CardId) && tavern.Pool[buddy.CardId] > 0);
        }

        [Test]
        public void AudiencesChoice_StartOfTurnChoiceStoresCardAndEndOfTurnGrantsItOnce()
        {
            var service = CreateAnomalyService("BG27_Anomaly_580");
            var tavern = service.State.Player.Tavern;
            var request = tavern.AdvancedMechanics.PendingChoice;

            AssertSinglePlayerAnomalyChoice(request, "anomaly-audiences-choice");
            Assert.IsTrue(request.Options.All(option => !string.IsNullOrEmpty(option.RewardId)));
            var selectedCardId = request.Options[0].RewardId;
            var handBefore = tavern.Hand.Count;

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.IsNull(tavern.AdvancedMechanics.PendingChoice);
            Assert.AreEqual(handBefore, tavern.Hand.Count);
            Assert.AreEqual(selectedCardId, tavern.AdvancedMechanics.Selections["anomaly_audiences_choice_selected_card"]);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(1, tavern.Hand.Count(card => card.CardId == selectedCardId));
            Assert.IsFalse(tavern.AdvancedMechanics.Selections.ContainsKey("anomaly_audiences_choice_selected_card"));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(1, tavern.Hand.Count(card => card.CardId == selectedCardId));
        }

        [Test]
        public void AudiencesChoice_NoSelectionGrantsNoEndOfTurnReward()
        {
            var service = CreateAnomalyService("BG27_Anomaly_580");
            var tavern = service.State.Player.Tavern;
            AssertSinglePlayerAnomalyChoice(tavern.AdvancedMechanics.PendingChoice, "anomaly-audiences-choice");
            var handBefore = tavern.Hand.Count;

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(handBefore, tavern.Hand.Count);
            AssertSinglePlayerAnomalyChoice(tavern.AdvancedMechanics.PendingChoice, "anomaly-audiences-choice");
            Assert.AreEqual(2, service.State.Round);
        }

        [Test]
        public void YoggIseum_StartOfTurnChoiceStoresRewardAndEndOfTurnResolvesIt()
        {
            var service = CreateAnomalyService("BG27_Anomaly_503");
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Add(TestMinion("yogg-board", "yogg-board"));
            var boardMinion = service.State.Player.Board.Single();
            var request = tavern.AdvancedMechanics.PendingChoice;

            AssertSinglePlayerAnomalyChoice(request, "anomaly-yogg-iseum");
            var selectedReward = request.Options[0].RewardId;
            var handBefore = tavern.Hand.Count;
            var freeRefreshesBefore = tavern.FreeRefreshes;
            var attackBefore = boardMinion.Attack;
            var healthBefore = boardMinion.Health;

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.IsNull(tavern.AdvancedMechanics.PendingChoice);
            Assert.AreEqual(handBefore, tavern.Hand.Count);
            Assert.AreEqual(freeRefreshesBefore, tavern.FreeRefreshes);
            Assert.AreEqual(attackBefore, boardMinion.Attack);
            Assert.AreEqual(selectedReward, tavern.AdvancedMechanics.Selections["anomaly_yogg_iseum_selected_reward"]);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            AssertYoggIseumRewardResolved(tavern, selectedReward, handBefore, freeRefreshesBefore, attackBefore, healthBefore, boardMinion);
            Assert.IsFalse(tavern.AdvancedMechanics.Selections.ContainsKey("anomaly_yogg_iseum_selected_reward"));
        }

        [Test]
        public void YoggIseum_ChoiceOptionsAreSeedStable()
        {
            var first = CreateAnomalyService("BG27_Anomaly_503");
            var second = CreateAnomalyService("BG27_Anomaly_503");

            CollectionAssert.AreEqual(
                first.State.Player.Tavern.AdvancedMechanics.PendingChoice.Options.Select(option => option.RewardId).ToList(),
                second.State.Player.Tavern.AdvancedMechanics.PendingChoice.Options.Select(option => option.RewardId).ToList());
        }

        private static MatchService CreateDoubleHeaderService()
        {
            return CreateAnomalyService("BG27_Anomaly_711");
        }

        private static MatchService CreateAnomalyService(string anomalyCardId)
        {
            return CreateAnomalyService(anomalyCardId, null);
        }

        private static MatchService CreateAnomalyService(string anomalyCardId, MatchSetupOptions setup)
        {
            setup = setup ?? new MatchSetupOptions();
            setup.EnableAnomalies = true;
            setup.SelectedAnomalyCardId = anomalyCardId;
            return MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                setup);
        }

        private static void AdvanceToRound(MatchService service, int round)
        {
            while (service.State.Round < round)
            {
                service.Apply(new GameCommand(GameCommandType.NextTurn));
            }
        }

        private static void UpgradeToTier(MatchService service, int tier)
        {
            while (service.State.Player.Tavern.Tier < tier)
            {
                service.State.Player.Tavern.Gold = 100;
                service.Apply(new GameCommand(GameCommandType.UpgradeTavern));
            }
        }

        private static string ChooseAnomalousExpeditionReward(MatchService service, int tier)
        {
            var discover = service.State.Player.Tavern.Discover;

            Assert.IsNotNull(discover);
            Assert.AreEqual("anomalous-expedition:" + tier, discover.Source);
            Assert.AreEqual(tier, discover.RewardTier);
            Assert.AreEqual(3, discover.Options.Count);
            Assert.IsTrue(discover.Options.All(card => card.TavernTier == tier && card.PoolSource == PoolSource.Copy && card.PoolCopiesHeld == 0));

            var pickedCardId = discover.Options[0].CardId;
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            return pickedCardId;
        }

        private static void AssertDarkmoonPrizeDiscover(TavernState tavern, string source, int tier, bool expectProxy)
        {
            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual(source, tavern.Discover.Source);
            Assert.AreEqual(tier, tavern.Discover.RewardTier);
            Assert.AreEqual(3, tavern.Discover.Options.Count);
            Assert.IsTrue(tavern.Discover.Options.All(card =>
                card.CardKind == CardKind.Spell &&
                card.TavernTier == tier &&
                card.PoolSource == PoolSource.Discover &&
                card.PoolCopiesHeld == 0 &&
                card.Tags.Contains("darkmoon_prize") &&
                card.Tags.Contains("darkmoon_prize_tier_" + tier)));
            if (!expectProxy)
            {
                Assert.IsTrue(tavern.Discover.Options.All(card => !card.Tags.Contains("darkmoon_prize_proxy")));
            }
        }

        private static void AssertSinglePlayerAnomalyChoice(MechanicChoiceRequest request, string source)
        {
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Anomaly, request.Kind);
            Assert.AreEqual(source, request.Source);
            Assert.AreEqual("EndOfTurn", request.Slot);
            Assert.AreEqual(1, request.RemainingPicks);
            Assert.AreEqual(2, request.Options.Count);
            Assert.IsTrue(request.Options.All(option => option.Kind == AdvancedMechanicKind.Anomaly));
            Assert.IsTrue(request.Options.All(option => option.Tags.Contains("end_of_turn_reward")));
        }

        private static void AssertYoggIseumRewardResolved(
            TavernState tavern,
            string selectedReward,
            int handBefore,
            int freeRefreshesBefore,
            int attackBefore,
            int healthBefore,
            MinionInstance boardMinion)
        {
            switch (selectedReward)
            {
                case "next_turn_gold":
                    Assert.AreEqual(tavern.MaxGold + 3, tavern.Gold);
                    break;
                case "board_buff":
                    Assert.AreEqual(attackBefore + 3, boardMinion.Attack);
                    Assert.AreEqual(healthBefore + 3, boardMinion.Health);
                    break;
                case "tavern_spell":
                case "current_tier_minion":
                    Assert.Greater(tavern.Hand.Count, handBefore);
                    break;
                case "free_refreshes":
                    Assert.AreEqual(freeRefreshesBefore + 2, tavern.FreeRefreshes);
                    break;
                case "tavern_coins":
                    Assert.AreEqual(handBefore + 2, tavern.Hand.Count);
                    Assert.IsTrue(tavern.Hand.Count(card => card.Name == "Tavern Coin" || card.CardId == "104436") >= 2);
                    break;
                case "wheel_shots":
                    Assert.GreaterOrEqual(boardMinion.Attack, attackBefore);
                    Assert.GreaterOrEqual(boardMinion.Health, healthBefore);
                    Assert.IsTrue(boardMinion.Attack > attackBefore || boardMinion.Health > healthBefore || tavern.RecruitLog.Any(entry => entry.Message.Contains("Wheel shots")));
                    break;
                case "wheel_darkmoon_prize":
                    Assert.IsNotNull(tavern.Discover);
                    Assert.IsTrue(tavern.Discover.Source.Contains("darkmoon-prize"));
                    Assert.IsTrue(tavern.Discover.Options.All(card =>
                        card.Tags.Contains("darkmoon_prize") &&
                        card.PoolSource == PoolSource.Discover));
                    break;
                case "wheel_tavern_spells":
                    Assert.IsTrue(
                        tavern.Hand.Count > handBefore ||
                        tavern.RecruitLog.Any(entry => entry.Message.Contains("cast") && entry.Message.Contains("Tavern spell")));
                    break;
                case "wheel_stats_transfer":
                    Assert.IsTrue(tavern.RecruitLog.Any(entry => entry.Message.Contains("not enough friendly minions") || entry.Message.Contains("added") && entry.Message.Contains("stats")));
                    break;
                case "wheel_devour_refresh":
                    Assert.IsTrue(tavern.RecruitLog.Any(entry => entry.Message.Contains("refreshed") || entry.Message.Contains("devoured")));
                    break;
                default:
                    Assert.Fail("Unknown Yogg reward: " + selectedReward);
                    break;
            }
        }

        private static void ForceOnlyPoolTarget(TavernState tavern, string definitionId, int count)
        {
            var keys = tavern.Pool.Keys.ToList();
            tavern.Pool = keys.ToDictionary(key => key, key => 0);
            tavern.Pool[definitionId] = count;
            tavern.PoolCapacities[definitionId] = System.Math.Max(count, tavern.PoolCapacities.TryGetValue(definitionId, out var capacity) ? capacity : 0);
        }

        private static void ForceOnlyBuddyTarget(TavernState tavern, string cardId, int count)
        {
            tavern.Shop.Clear();
            tavern.ShopSlots.Clear();
            var poolKeys = tavern.Pool.Keys.ToList();
            tavern.Pool = poolKeys.ToDictionary(key => key, key => 0);
            var buddyKeys = tavern.BuddyPool.Keys.ToList();
            tavern.BuddyPool = buddyKeys.ToDictionary(key => key, key => 0);
            tavern.BuddyPool[cardId] = count;
            tavern.BuddyPoolCapacities[cardId] = System.Math.Max(
                count,
                tavern.BuddyPoolCapacities.TryGetValue(cardId, out var capacity) ? capacity : 0);
        }

        private static List<HeroBuddyDefinition> DiscoverableBuddies()
        {
            return HeroCatalogLoader.LoadFromResources()
                .AllBuddies
                .Where(buddy =>
                    buddy != null &&
                    !string.IsNullOrEmpty(buddy.CardId) &&
                    !buddy.ExcludedFromBuddyDiscover)
                .ToList();
        }

        private static string OathstonePoolId(string cardId)
        {
            return "timewarped-" + cardId;
        }

        private static List<MinionDefinition> LegalTierSevenDefinitions(MatchService service)
        {
            var enabled = service.State.EnabledMinionCardIds ?? new List<string>();
            var active = TribeAvailabilityRules.Normalize(service.State.ActiveTribes);
            return MinionCatalogLoader.LoadFromResources()
                .All
                .Where(definition =>
                    definition.InPool &&
                    definition.TavernTier == TavernRules.MaxTavernTier &&
                    (enabled.Count == 0 || enabled.Contains(definition.CardId)) &&
                    TribeAvailabilityRules.IsMinionAvailable(definition, active))
                .ToList();
        }

        private static MinionInstance TripleRewardCard()
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "test-triple-reward",
                DefinitionId = "triple-reward",
                CardId = "TRIPLE_REWARD",
                Name = "Triple Reward",
                Attack = 0,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 0,
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
        }

        private static MinionInstance PatientScout(string instanceId, int tier)
        {
            var scout = new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = "bg24_715",
                CardId = "BG24_715",
                Name = "Patient Scout",
                Attack = 1,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.None },
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
            scout.Counters["patient-scout-tier"] = tier;
            return scout;
        }

        private static void AssertAnomalousExpeditionReward(TavernState tavern, string cardId, int tier)
        {
            var reward = tavern.Hand.Single(card => card.CardId == cardId);

            Assert.AreEqual(tier, reward.TavernTier);
            Assert.AreEqual(PoolSource.Copy, reward.PoolSource);
            Assert.AreEqual(0, reward.PoolCopiesHeld);
            Assert.IsTrue(reward.Tags.Contains("anomaly_anomalous_expedition"));
        }

        private static MinionInstance TestMinion(string instanceId, string definitionId)
        {
            return TestMinion(instanceId, definitionId, 2, 3);
        }

        private static MinionInstance TestMinion(
            string instanceId,
            string definitionId,
            int attack,
            int health,
            Tribe tribe = Tribe.None,
            int tavernTier = 1,
            params Keyword[] keywords)
        {
            var minion = new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = definitionId,
                CardId = definitionId.ToUpperInvariant(),
                Name = "False Idols Test",
                BaseAttack = attack,
                Attack = attack,
                BaseHealth = health,
                Health = health,
                MaxHealth = health,
                TavernTier = tavernTier,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                OfficialKeywords = keywords.ToList(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
            return minion;
        }
    }
}
