using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TrinketSystemTests
    {
        private const string DebugNoBoardSpellCardId = "122186";
        private const string DebugBoardHealthSpellCardId = "122182";
        private const string HastyExcavationCardId = "104559";
        private const string EasterlyWindsCardId = "126909";
        private const string MountingAvalancheCardId = "122862";
        private const string ButcheringCardId = "110412";
        private const string ChannelTheDevourerCardId = "100899";
        private const string AzeriteEmpowermentCardId = "109232";
        private const string KnockoffWisdomballCardId = "113902";
        private const string TemperatureShiftCardId = "117670";
        private const string RazorfenGeomancerCardId = "BG20_100";
        private const string SnarlingConductorCardId = "BG28_585";
        private const string ChargingCzarinaCardId = "BG28_741";
        private const string WoodlandDefilerCardId = "BG35_151";
        private const string DemonFodderCardId = "DEMON_FODDER";
        private const string DefilerPortraitAuraSourceId = "Trinket:Defiler Portrait";
        private const string FeralTalismanAuraSourceId = "Trinket:Feral Talisman";
        private const string ArtisanalUrnAuraSourceId = "Trinket:Artisanal Urn";
        private const string CopperCoilLesserAttackCounter = "trinket:copper_coil:BG35_MagicItem_300:attack";
        private const string CopperCoilLesserHealthCounter = "trinket:copper_coil:BG35_MagicItem_300:health";
        private const string CopperCoilGreaterAttackCounter = "trinket:copper_coil:BG35_MagicItem_300t:attack";
        private const string CopperCoilGreaterHealthCounter = "trinket:copper_coil:BG35_MagicItem_300t:health";
        private const string ChillmereMosaicSpellCardId = "TRINKET_CHILLMERE_MOSAIC_SPELL";
        private const string ChromaticTearBattlecryCounter = "chromatic_tear_battlecries";
        private const string JailerStickerSpellCardId = "TRINKET_JAILER_STICKER_SPELL";
        private const string DemonbloodGourdSpellCardId = "TRINKET_DEMONBLOOD_GOURD_SPELL";
        private const string ShiftingTideSpellCardId = "TRINKET_SHIFTING_TIDE_SPELL";
        private const string ZestyShakerCardId = "BG26_505";
        private const string TideRaiserCardId = "BG34_920";
        private const string NerubianDeathswarmerCardId = "BG25_011";
        private const string LockedTurnsCounter = "locked-turns";
        private const string SecretsOfNorgannonAnomalyCardId = "BG27_Anomaly_504";
        private static readonly HashSet<string> Batch2BountyCardIds = new HashSet<string>
        {
            "122182",
            "122183",
            "122184",
            "122185",
            "122186"
        };

        private static readonly HashSet<string> JewelryBoxGemCardIds = new HashSet<string>
        {
            "TRINKET_JEWELRY_BOX_TAUNT_GEM",
            "TRINKET_JEWELRY_BOX_DIVINE_SHIELD_GEM",
            "TRINKET_JEWELRY_BOX_REBORN_GEM"
        };

        private static readonly HashSet<string> ChromadrakeCardIds = new HashSet<string>
        {
            "BG34_634t",
            "BG34_635t",
            "BG34_636t",
            "BG34_637t",
            "BG34_638t"
        };

        [Test]
        public void Catalog_LocalizesEveryTrinketAndPreservesEnglishMode()
        {
            var chinese = TrinketCatalogLoader.LoadFromResources(false);
            var english = TrinketCatalogLoader.LoadFromResources(true);

            Assert.AreEqual(330, chinese.All.Count);
            Assert.IsTrue(chinese.All.All(trinket => ContainsChinese(trinket.Name) && ContainsChinese(trinket.Text)));
            Assert.AreEqual("Artanis Sticker", english.GetByCardId("BG32_MagicItem_906").Name);
            Assert.AreEqual("阿塔尼斯标签", chinese.GetByCardId("BG32_MagicItem_906").Name);
            StringAssert.Contains("母舰", chinese.GetByCardId("BG32_MagicItem_906").Text);
            StringAssert.DoesNotContain("92", string.Join("", chinese.All.Select(trinket => trinket.Text)));
            StringAssert.DoesNotContain(">0<", string.Join("", chinese.All.Select(trinket => trinket.Text)));
            StringAssert.DoesNotContain("</i>4", string.Join("", chinese.All.Select(trinket => trinket.Text)));
            StringAssert.Contains("当前酒馆等级", chinese.GetByCardId("BG30_MagicItem_426").Text);
            StringAssert.Contains("仅记录提示", chinese.GetByCardId("BG35_MagicItem_820").Text);

            var chineseService = MatchService.CreateWithDefaultCatalog(setup: new MatchSetupOptions { UseEnglish = false });
            var englishService = MatchService.CreateWithDefaultCatalog(setup: new MatchSetupOptions { UseEnglish = true });
            Assert.AreEqual("随从诱饵", chineseService.TrinketCatalog.GetByCardId("BG30_MagicItem_973").Name);
            Assert.AreEqual("Minion Bait", englishService.TrinketCatalog.GetByCardId("BG30_MagicItem_973").Name);
        }

        [Test]
        public void Catalog_LoadsLesserAndGreaterTrinketsWithVisibleStatuses()
        {
            var catalog = TrinketCatalogLoader.LoadFromResources();

            Assert.AreEqual(330, catalog.All.Count);
            Assert.AreEqual(157, catalog.Lesser.Count);
            Assert.AreEqual(173, catalog.Greater.Count);
            Assert.AreEqual(330, catalog.Implemented.Count);
            Assert.AreEqual(329, catalog.Offerable.Count);
            Assert.AreEqual(156, catalog.GetOfferableBySlot(TrinketSlotKind.Lesser).Count);
            Assert.AreEqual(173, catalog.GetOfferableBySlot(TrinketSlotKind.Greater).Count);
            Assert.IsTrue(catalog.All.All(trinket => !string.IsNullOrWhiteSpace(trinket.ImagePath)));
            Assert.IsTrue(catalog.All.All(trinket => Resources.Load<Texture2D>(trinket.ImagePath) != null));
            Assert.IsTrue(catalog.All.All(trinket => !string.IsNullOrWhiteSpace(trinket.EffectFamily)));
            Assert.IsTrue(catalog.All.All(trinket => !string.IsNullOrWhiteSpace(trinket.ProxyLevel)));
            Assert.IsTrue(catalog.All.All(trinket => trinket.Requires != null));
            Assert.IsTrue(catalog.Offerable.All(trinket => trinket.ImplementationStatus == TrinketImplementationStatus.Implemented));
            Assert.IsTrue(catalog.Offerable.All(trinket => trinket.OfferPoolStatus == TrinketOfferPoolStatus.Offerable));
            Assert.IsTrue(catalog.Offerable.All(trinket => trinket.EffectIds.Count > 0));
            Assert.IsFalse(catalog.All.Any(trinket =>
                trinket.ImplementationStatus != TrinketImplementationStatus.Implemented &&
                trinket.OfferPoolStatus == TrinketOfferPoolStatus.Offerable));
            Assert.IsFalse(catalog.All.Any(IsDuoOrPassTrinket));
            Assert.IsNull(Resources.Load<Texture2D>("CardImages/BGDUO_MagicItem_001"));
            Assert.AreEqual(
                catalog.All.Count,
                TrinketImplementationRegistry.All(catalog).Count(entry => entry.Status != TrinketImplementationStatus.Unregistered));

            var kodo = catalog.GetByCardId("BG30_MagicItem_414");
            Assert.AreEqual(TrinketSlotKind.Lesser, kodo.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, kodo.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, kodo.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, kodo.PowerLevel);
            Assert.AreEqual("buy_trigger", kodo.EffectFamily);
            Assert.AreEqual("Exact", kodo.ProxyLevel);
            Assert.AreEqual("CardImages/BG30_MagicItem_414", kodo.ImagePath);
            Assert.IsNotNull(CardImageProvider.LoadSprite(kodo.ImagePath, kodo.CardId, CardKind.Trinket));
            CollectionAssert.Contains(kodo.EffectIds, "kodo_leather_pouch");

            var lavishCape = catalog.GetByCardId("BG32_MagicItem_286");
            Assert.AreEqual(TrinketSlotKind.Greater, lavishCape.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, lavishCape.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, lavishCape.OfferPoolStatus);
            Assert.AreEqual("tavern_spell", lavishCape.EffectFamily);
            Assert.AreEqual("Exact", lavishCape.ProxyLevel);
            CollectionAssert.Contains(lavishCape.EffectIds, "lavish_cape");

            var lesserPocketCyclone = catalog.GetByCardId("BG35_MagicItem_850");
            Assert.AreEqual(TrinketSlotKind.Lesser, lesserPocketCyclone.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, lesserPocketCyclone.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, lesserPocketCyclone.OfferPoolStatus);
            Assert.AreEqual("turn_start", lesserPocketCyclone.EffectFamily);
            Assert.AreEqual("Exact", lesserPocketCyclone.ProxyLevel);
            CollectionAssert.Contains(lesserPocketCyclone.EffectIds, "pocket_cyclone");

            var greaterPocketCyclone = catalog.GetByCardId("BG35_MagicItem_850t");
            Assert.AreEqual(TrinketSlotKind.Greater, greaterPocketCyclone.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, greaterPocketCyclone.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, greaterPocketCyclone.OfferPoolStatus);
            Assert.AreEqual("turn_start", greaterPocketCyclone.EffectFamily);
            Assert.AreEqual("Exact", greaterPocketCyclone.ProxyLevel);
            CollectionAssert.Contains(greaterPocketCyclone.EffectIds, "pocket_cyclone");

            var paglesFishingRod = catalog.GetByCardId("BG30_MagicItem_993");
            Assert.AreEqual(TrinketSlotKind.Greater, paglesFishingRod.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, paglesFishingRod.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, paglesFishingRod.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Strong, paglesFishingRod.PowerLevel);
            Assert.AreEqual("turn_start", paglesFishingRod.EffectFamily);
            Assert.AreEqual("Exact", paglesFishingRod.ProxyLevel);
            CollectionAssert.Contains(paglesFishingRod.EffectIds, "pagles_fishing_rod");
            CollectionAssert.Contains(paglesFishingRod.Requires, "turn_start");

            var explorersBinoculars = catalog.GetByCardId("BG32_MagicItem_858");
            Assert.AreEqual(TrinketSlotKind.Lesser, explorersBinoculars.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, explorersBinoculars.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, explorersBinoculars.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, explorersBinoculars.PowerLevel);
            Assert.AreEqual("economy", explorersBinoculars.EffectFamily);
            Assert.AreEqual("Exact", explorersBinoculars.ProxyLevel);
            CollectionAssert.Contains(explorersBinoculars.EffectIds, "explorers_binoculars");
            CollectionAssert.Contains(explorersBinoculars.Requires, "tribe_pool");

            var lavaLamp = catalog.GetByCardId("BG30_MagicItem_951");
            Assert.AreEqual(TrinketSlotKind.Greater, lavaLamp.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, lavaLamp.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, lavaLamp.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, lavaLamp.PowerLevel);
            Assert.AreEqual("sell_trigger", lavaLamp.EffectFamily);
            Assert.AreEqual("Exact", lavaLamp.ProxyLevel);
            CollectionAssert.Contains(lavaLamp.EffectIds, "lava_lamp");
            CollectionAssert.Contains(lavaLamp.Requires, "tribe_pool");
            CollectionAssert.Contains(lavaLamp.Requires, "sell_trigger");

            var fungalmancerSticker = catalog.GetByCardId("BG30_MagicItem_710");
            Assert.AreEqual(TrinketSlotKind.Lesser, fungalmancerSticker.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, fungalmancerSticker.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, fungalmancerSticker.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, fungalmancerSticker.PowerLevel);
            Assert.AreEqual("sell_trigger", fungalmancerSticker.EffectFamily);
            Assert.AreEqual("Exact", fungalmancerSticker.ProxyLevel);
            CollectionAssert.Contains(fungalmancerSticker.EffectIds, "fungalmancer_sticker");
            CollectionAssert.Contains(fungalmancerSticker.Requires, "tribe_pool");
            CollectionAssert.Contains(fungalmancerSticker.Requires, "sell_trigger");

            var avalancheSticker = catalog.GetByCardId("BG35_MagicItem_863");
            Assert.AreEqual(TrinketSlotKind.Greater, avalancheSticker.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, avalancheSticker.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, avalancheSticker.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, avalancheSticker.PowerLevel);
            Assert.AreEqual("sell_trigger", avalancheSticker.EffectFamily);
            Assert.AreEqual("Exact", avalancheSticker.ProxyLevel);
            CollectionAssert.Contains(avalancheSticker.EffectIds, "avalanche_sticker");
            CollectionAssert.Contains(avalancheSticker.Requires, "tavern_spell");
            CollectionAssert.Contains(avalancheSticker.Requires, "sell_trigger");
            CollectionAssert.Contains(avalancheSticker.Requires, "tribe_pool");

            var gemDonation = catalog.GetByCardId("BG32_MagicItem_809");
            Assert.AreEqual(TrinketSlotKind.Lesser, gemDonation.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, gemDonation.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, gemDonation.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, gemDonation.PowerLevel);
            Assert.AreEqual("sell_trigger", gemDonation.EffectFamily);
            Assert.AreEqual("Exact", gemDonation.ProxyLevel);
            CollectionAssert.Contains(gemDonation.EffectIds, "gem_donation");
            CollectionAssert.Contains(gemDonation.Requires, "blood_gem");
            CollectionAssert.Contains(gemDonation.Requires, "sell_trigger");
            CollectionAssert.Contains(gemDonation.Requires, "tribe_pool");

            var boomsMonsterPortrait = catalog.GetByCardId("BG32_MagicItem_172");
            Assert.AreEqual(TrinketSlotKind.Greater, boomsMonsterPortrait.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, boomsMonsterPortrait.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, boomsMonsterPortrait.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, boomsMonsterPortrait.PowerLevel);
            Assert.AreEqual("turn_start", boomsMonsterPortrait.EffectFamily);
            Assert.AreEqual("Exact", boomsMonsterPortrait.ProxyLevel);
            CollectionAssert.Contains(boomsMonsterPortrait.EffectIds, "booms_monster_portrait");
            CollectionAssert.Contains(boomsMonsterPortrait.Requires, "tribe_pool");
            CollectionAssert.Contains(boomsMonsterPortrait.Requires, "turn_start");

            var butchersSickle = catalog.GetByCardId("BG30_MagicItem_406");
            Assert.AreEqual(TrinketSlotKind.Greater, butchersSickle.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, butchersSickle.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, butchersSickle.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, butchersSickle.PowerLevel);
            Assert.AreEqual("turn_start", butchersSickle.EffectFamily);
            Assert.AreEqual("Exact", butchersSickle.ProxyLevel);
            CollectionAssert.Contains(butchersSickle.EffectIds, "butchers_sickle");
            CollectionAssert.Contains(butchersSickle.Requires, "tavern_spell");
            CollectionAssert.Contains(butchersSickle.Requires, "turn_start");
            CollectionAssert.Contains(butchersSickle.Requires, "tribe_pool");

            AssertCatalogTurnStartSpellTrinket(catalog.GetByCardId("BG30_MagicItem_543"), "devourer_sticker");
            AssertCatalogTurnStartSpellTrinket(catalog.GetByCardId("BG32_MagicItem_944"), "empowerment_portrait");
            AssertCatalogTurnStartSpellTrinket(catalog.GetByCardId("BG31_MagicItem_903"), "wisdomball_supply");
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG35_MagicItem_741"), "beatboxer_portrait");
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG32_MagicItem_926"), "morgl_portrait");
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG30_MagicItem_555"), "surprise_portrait");
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG32_MagicItem_998"), "behemoth_portrait");
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG30_MagicItem_876"), "manipulator_portrait", effectFamily: "economy", requiresTribePool: false);
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG32_MagicItem_364"), "poet_portrait");
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG35_MagicItem_310"), "radio_star_portrait");
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG30_MagicItem_821"), "fish_portrait", TrinketSlotKind.Lesser);
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG35_MagicItem_870"), "leapfrogger_portrait", TrinketSlotKind.Lesser);
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG35_MagicItem_303"), "skipper_portrait", TrinketSlotKind.Lesser);
            var bronzebeardPortrait = catalog.GetByCardId("BG30_MagicItem_418");
            AssertCatalogSpecifiedMinionTrinket(bronzebeardPortrait, "bronzebeard_portrait");
            CollectionAssert.Contains(bronzebeardPortrait.Requires, "battlecry");
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG32_MagicItem_179"), "drakkari_portrait");
            AssertCatalogSpecifiedMinionTrinket(catalog.GetByCardId("BG30_MagicItem_971"), "enforcer_portrait");
            var balladistPortrait = catalog.GetByCardId("BG30_MagicItem_987");
            AssertCatalogSpecifiedMinionTrinket(balladistPortrait, "balladist_portrait", effectFamily: "turn_start");
            CollectionAssert.Contains(balladistPortrait.Requires, "battlecry");
            CollectionAssert.Contains(balladistPortrait.Requires, "turn_start");
            var bristlebachPortrait = catalog.GetByCardId("BG32_MagicItem_274");
            AssertCatalogSpecifiedMinionTrinket(bristlebachPortrait, "bristlebach_portrait");
            CollectionAssert.Contains(bristlebachPortrait.Requires, "blood_gem");

            var ballerPortrait = catalog.GetByCardId("BG35_MagicItem_861");
            AssertCatalogSpecifiedMinionTrinket(ballerPortrait, "baller_portrait");
            CollectionAssert.Contains(ballerPortrait.Requires, "tavern_spell");

            var chargingStaff = catalog.GetByCardId("BG30_MagicItem_984t");
            Assert.AreEqual(TrinketSlotKind.Greater, chargingStaff.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, chargingStaff.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, chargingStaff.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, chargingStaff.PowerLevel);
            Assert.AreEqual("turn_end", chargingStaff.EffectFamily);
            Assert.AreEqual("Exact", chargingStaff.ProxyLevel);
            CollectionAssert.Contains(chargingStaff.EffectIds, "charging_staff");
            CollectionAssert.Contains(chargingStaff.Requires, "divine_shield");
            CollectionAssert.Contains(chargingStaff.Requires, "turn_end");
            CollectionAssert.Contains(chargingStaff.Requires, "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_984"),
                "charging_staff",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_end",
                "divine_shield",
                "turn_end",
                "tribe_pool");

            var chillmereMosaic = catalog.GetByCardId("BG35_MagicItem_755");
            Assert.AreEqual(TrinketSlotKind.Greater, chillmereMosaic.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, chillmereMosaic.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, chillmereMosaic.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, chillmereMosaic.PowerLevel);
            Assert.AreEqual("spellcraft", chillmereMosaic.EffectFamily);
            Assert.AreEqual("Exact", chillmereMosaic.ProxyLevel);
            CollectionAssert.Contains(chillmereMosaic.EffectIds, "chillmere_mosaic");
            CollectionAssert.Contains(chillmereMosaic.Requires, "battlecry");
            CollectionAssert.Contains(chillmereMosaic.Requires, "spellcraft");
            CollectionAssert.Contains(chillmereMosaic.Requires, "tribe_pool");

            var chromaticTear = catalog.GetByCardId("BG35_MagicItem_840t");
            Assert.AreEqual(TrinketSlotKind.Greater, chromaticTear.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, chromaticTear.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, chromaticTear.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, chromaticTear.PowerLevel);
            Assert.AreEqual("tribe_specific", chromaticTear.EffectFamily);
            Assert.AreEqual("Exact", chromaticTear.ProxyLevel);
            CollectionAssert.Contains(chromaticTear.EffectIds, "chromatic_tear");
            CollectionAssert.Contains(chromaticTear.Requires, "battlecry");
            CollectionAssert.Contains(chromaticTear.Requires, "tribe_pool");

            var conductorPortrait = catalog.GetByCardId("BG30_MagicItem_402");
            Assert.AreEqual(TrinketSlotKind.Greater, conductorPortrait.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, conductorPortrait.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, conductorPortrait.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, conductorPortrait.PowerLevel);
            Assert.AreEqual("discard", conductorPortrait.EffectFamily);
            Assert.AreEqual("ProxySafe", conductorPortrait.ProxyLevel);
            CollectionAssert.Contains(conductorPortrait.EffectIds, "conductor_portrait");
            CollectionAssert.Contains(conductorPortrait.Requires, "blood_gem");
            CollectionAssert.Contains(conductorPortrait.Requires, "discard");
            CollectionAssert.Contains(conductorPortrait.Requires, "tribe_pool");

            var copperCoil = catalog.GetByCardId("BG35_MagicItem_300t");
            Assert.AreEqual(TrinketSlotKind.Greater, copperCoil.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, copperCoil.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, copperCoil.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, copperCoil.PowerLevel);
            Assert.AreEqual("magnetic", copperCoil.EffectFamily);
            Assert.AreEqual("Exact", copperCoil.ProxyLevel);
            CollectionAssert.Contains(copperCoil.EffectIds, "copper_coil");
            CollectionAssert.Contains(copperCoil.Requires, "magnetic");
            CollectionAssert.Contains(copperCoil.Requires, "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_300"),
                "copper_coil",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "magnetic",
                "magnetic",
                "tribe_pool");

            var czarinaPortrait = catalog.GetByCardId("BG32_MagicItem_283");
            AssertCatalogSpecifiedMinionTrinket(czarinaPortrait, "czarina_portrait");
            CollectionAssert.Contains(czarinaPortrait.Requires, "divine_shield");
            CollectionAssert.Contains(czarinaPortrait.Requires, "tavern_spell");

            var darnassusPie = catalog.GetByCardId("BG30_MagicItem_992");
            Assert.AreEqual(TrinketSlotKind.Greater, darnassusPie.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, darnassusPie.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, darnassusPie.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, darnassusPie.PowerLevel);
            Assert.AreEqual("shop_aura", darnassusPie.EffectFamily);
            Assert.AreEqual("Exact", darnassusPie.ProxyLevel);
            CollectionAssert.Contains(darnassusPie.EffectIds, "darnassus_pie");
            CollectionAssert.Contains(darnassusPie.Requires, "sell_trigger");
            CollectionAssert.Contains(darnassusPie.Requires, "shop_refresh");
            CollectionAssert.Contains(darnassusPie.Requires, "tribe_pool");

            var doubleDarnassusPie = catalog.GetByCardId("BG30_MagicItem_992t");
            Assert.AreEqual(TrinketSlotKind.Greater, doubleDarnassusPie.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, doubleDarnassusPie.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, doubleDarnassusPie.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Strong, doubleDarnassusPie.PowerLevel);
            Assert.AreEqual("shop_aura", doubleDarnassusPie.EffectFamily);
            Assert.AreEqual("Exact", doubleDarnassusPie.ProxyLevel);
            CollectionAssert.Contains(doubleDarnassusPie.EffectIds, "darnassus_pie_double");
            CollectionAssert.Contains(doubleDarnassusPie.Requires, "sell_trigger");
            CollectionAssert.Contains(doubleDarnassusPie.Requires, "shop_refresh");
            CollectionAssert.Contains(doubleDarnassusPie.Requires, "tribe_pool");

            var deathtouchApple = catalog.GetByCardId("BG35_MagicItem_731");
            Assert.AreEqual(TrinketSlotKind.Greater, deathtouchApple.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, deathtouchApple.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, deathtouchApple.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, deathtouchApple.PowerLevel);
            Assert.AreEqual("tribe_specific", deathtouchApple.EffectFamily);
            Assert.AreEqual("Exact", deathtouchApple.ProxyLevel);
            CollectionAssert.Contains(deathtouchApple.EffectIds, "deathtouch_apple");
            CollectionAssert.Contains(deathtouchApple.Requires, "combat_event");
            CollectionAssert.Contains(deathtouchApple.Requires, "reborn");
            CollectionAssert.Contains(deathtouchApple.Requires, "tribe_pool");

            var designerEyepatch = catalog.GetByCardId("BG30_MagicItem_439");
            Assert.AreEqual(TrinketSlotKind.Greater, designerEyepatch.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, designerEyepatch.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, designerEyepatch.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Strong, designerEyepatch.PowerLevel);
            Assert.AreEqual("golden_triple", designerEyepatch.EffectFamily);
            Assert.AreEqual("Exact", designerEyepatch.ProxyLevel);
            CollectionAssert.Contains(designerEyepatch.EffectIds, "designer_eyepatch");
            CollectionAssert.Contains(designerEyepatch.Requires, "golden_triple");
            CollectionAssert.Contains(designerEyepatch.Requires, "tribe_pool");

            var dragonwingGlider = catalog.GetByCardId("BG30_MagicItem_900t");
            Assert.AreEqual(TrinketSlotKind.Greater, dragonwingGlider.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, dragonwingGlider.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, dragonwingGlider.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Strong, dragonwingGlider.PowerLevel);
            Assert.AreEqual("play_trigger", dragonwingGlider.EffectFamily);
            Assert.AreEqual("Exact", dragonwingGlider.ProxyLevel);
            CollectionAssert.Contains(dragonwingGlider.EffectIds, "dragonwing_glider_greater");
            CollectionAssert.Contains(dragonwingGlider.Requires, "play_trigger");
            CollectionAssert.Contains(dragonwingGlider.Requires, "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_900"),
                "dragonwing_glider",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "play_trigger",
                "play_trigger",
                "tribe_pool");

            var greaterDefilerPortrait = catalog.GetByCardId("BG35_MagicItem_151t");
            Assert.AreEqual(TrinketSlotKind.Greater, greaterDefilerPortrait.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, greaterDefilerPortrait.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, greaterDefilerPortrait.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Strong, greaterDefilerPortrait.PowerLevel);
            Assert.AreEqual("shop_aura", greaterDefilerPortrait.EffectFamily);
            Assert.AreEqual("Exact", greaterDefilerPortrait.ProxyLevel);
            CollectionAssert.Contains(greaterDefilerPortrait.EffectIds, "defiler_portrait_greater");
            CollectionAssert.Contains(greaterDefilerPortrait.Requires, "shop_refresh");
            CollectionAssert.Contains(greaterDefilerPortrait.Requires, "tribe_pool");

            var lesserDefilerPortrait = catalog.GetByCardId("BG35_MagicItem_151");
            Assert.AreEqual(TrinketSlotKind.Lesser, lesserDefilerPortrait.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, lesserDefilerPortrait.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, lesserDefilerPortrait.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, lesserDefilerPortrait.PowerLevel);
            Assert.AreEqual("shop_aura", lesserDefilerPortrait.EffectFamily);
            Assert.AreEqual("Exact", lesserDefilerPortrait.ProxyLevel);
            CollectionAssert.Contains(lesserDefilerPortrait.EffectIds, "defiler_portrait");
            CollectionAssert.Contains(lesserDefilerPortrait.Requires, "shop_refresh");
            CollectionAssert.Contains(lesserDefilerPortrait.Requires, "tribe_pool");

            var battleHorn = catalog.GetByCardId("BG32_MagicItem_415");
            Assert.AreEqual(TrinketSlotKind.Greater, battleHorn.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, battleHorn.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, battleHorn.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, battleHorn.PowerLevel);
            Assert.AreEqual("avenge", battleHorn.EffectFamily);
            Assert.AreEqual("ProxySafe", battleHorn.ProxyLevel);
            CollectionAssert.Contains(battleHorn.EffectIds, "battle_horn");
            CollectionAssert.Contains(battleHorn.Requires, "battlecry");
            CollectionAssert.Contains(battleHorn.Requires, "discover");
            CollectionAssert.Contains(battleHorn.Requires, "combat_event");
            CollectionAssert.Contains(battleHorn.Requires, "tribe_pool");

            var bloodboundEarrings = catalog.GetByCardId("BG32_MagicItem_808t");
            Assert.AreEqual(TrinketSlotKind.Greater, bloodboundEarrings.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, bloodboundEarrings.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, bloodboundEarrings.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, bloodboundEarrings.PowerLevel);
            Assert.AreEqual("spell_cast", bloodboundEarrings.EffectFamily);
            Assert.AreEqual("Exact", bloodboundEarrings.ProxyLevel);
            CollectionAssert.Contains(bloodboundEarrings.EffectIds, "bloodbound_earrings");
            CollectionAssert.Contains(bloodboundEarrings.Requires, "blood_gem");
            CollectionAssert.Contains(bloodboundEarrings.Requires, "spell_cast");
            CollectionAssert.Contains(bloodboundEarrings.Requires, "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_808"),
                "bloodbound_earrings",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "spell_cast",
                "blood_gem",
                "spell_cast",
                "tribe_pool");

            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_422"),
                "lorewalker_scroll",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "spell_cast",
                "spell",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_422t"),
                "lorewalker_scroll",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "spell_cast",
                "spell",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_914"),
                "nerglish_phrasebook",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "play_trigger",
                "play_trigger",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_914t"),
                "nerglish_phrasebook",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "play_trigger",
                "play_trigger",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_544"),
                "nomi_sticker",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "shop_aura",
                "play_trigger",
                "shop_refresh",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_544t"),
                "nomi_sticker",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "shop_aura",
                "play_trigger",
                "shop_refresh",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_802"),
                "fountain_pen",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "tribe_specific",
                "stat_grant",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_802t"),
                "fountain_pen",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "tribe_specific",
                "stat_grant",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_988"),
                "great_boar_sticker",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "blood_gem",
                "blood_gem",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_988t"),
                "great_boar_sticker",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "blood_gem",
                "blood_gem",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_893"),
                "bluegill_flippers",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "tavern_spell",
                "tavern_spell",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_170"),
                "spell_powered_wrench",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "tavern_spell",
                "tavern_spell",
                "magnetic",
                "play_trigger",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_888"),
                "recycling_sticker",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "shop_refresh",
                "play_trigger",
                "shop_refresh",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_954"),
                "auric_offering",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_end",
                "golden_triple",
                "turn_end",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_111"),
                "toxic_stinger",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "turn_end",
                "turn_end",
                "venomous",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_276"),
                "enigmatic_headstone",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_end",
                "turn_end",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_279"),
                "tough_tusk_sticker",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "spell_cast",
                "blood_gem",
                "divine_shield",
                "spell_cast",
                "tribe_pool");

            var bloodboundRing = catalog.GetByCardId("BG35_MagicItem_435");
            Assert.AreEqual(TrinketSlotKind.Greater, bloodboundRing.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, bloodboundRing.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, bloodboundRing.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, bloodboundRing.PowerLevel);
            Assert.AreEqual("spell_cast", bloodboundRing.EffectFamily);
            Assert.AreEqual("Exact", bloodboundRing.ProxyLevel);
            CollectionAssert.Contains(bloodboundRing.EffectIds, "bloodbound_ring");
            CollectionAssert.Contains(bloodboundRing.Requires, "blood_gem");
            CollectionAssert.Contains(bloodboundRing.Requires, "spell_cast");
            CollectionAssert.Contains(bloodboundRing.Requires, "tribe_pool");

            var lesserBootyBayBrew = catalog.GetByCardId("BG30_MagicItem_924");
            Assert.AreEqual(TrinketSlotKind.Lesser, lesserBootyBayBrew.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, lesserBootyBayBrew.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, lesserBootyBayBrew.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, lesserBootyBayBrew.PowerLevel);
            Assert.AreEqual("economy", lesserBootyBayBrew.EffectFamily);
            Assert.AreEqual("Exact", lesserBootyBayBrew.ProxyLevel);
            CollectionAssert.Contains(lesserBootyBayBrew.EffectIds, "booty_bay_brew");
            CollectionAssert.Contains(lesserBootyBayBrew.Requires, "tribe_pool");

            var greaterBootyBayBrew = catalog.GetByCardId("BG30_MagicItem_924t");
            Assert.AreEqual(TrinketSlotKind.Greater, greaterBootyBayBrew.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, greaterBootyBayBrew.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, greaterBootyBayBrew.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Strong, greaterBootyBayBrew.PowerLevel);
            Assert.AreEqual("economy", greaterBootyBayBrew.EffectFamily);
            Assert.AreEqual("Exact", greaterBootyBayBrew.ProxyLevel);
            CollectionAssert.Contains(greaterBootyBayBrew.EffectIds, "booty_bay_brew");
            CollectionAssert.Contains(greaterBootyBayBrew.Requires, "tribe_pool");

            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_880"),
                "feral_talisman",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "board_aura",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_880t"),
                "feral_talisman",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "board_aura",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_989"),
                "artisanal_urn",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "board_aura",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_989t"),
                "artisanal_urn",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "board_aura",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_231"),
                "gilded_anchor",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_end",
                "golden_triple",
                "turn_end",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_231t"),
                "gilded_anchor",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "turn_end",
                "golden_triple",
                "turn_end",
                "tribe_pool");

            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_842"),
                "egg_of_the_endtimes_portrait",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_848t"),
                "egg_of_the_endtimes_portrait",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "turn_start",
                "turn_start",
                "golden_triple",
                "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG30_MagicItem_916"),
                "essence_of_dreams",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "turn_start",
                "ProxySafe",
                "tavern_spell",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_840"),
                "chromatic_tear_lesser",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_942"),
                "mecha_jaraxxus_sticker",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "turn_start",
                "magnetic",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_712"),
                "privateer_portrait",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "turn_start",
                "tavern_spell",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_890"),
                "sunken_anchor",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "tavern_spell",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_309"),
                "errgl_sticker",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "battlecry",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG32_MagicItem_950"),
                "gritty_portrait",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "ProxySafe",
                "battlecry",
                "tavern_spell",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG35_MagicItem_434"),
                "jewelry_box",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "ProxySafe",
                "blood_gem",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_305"),
                "conch_portrait",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "tavern_spell",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG35_MagicItem_817"),
                "lens_case",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "ProxySafe",
                "tavern_spell",
                "turn_start");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG30_MagicItem_425"),
                "azeroth_model_globe",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "ProxySafe",
                "discover",
                "turn_start");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_951"),
                "gold_pendant",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "golden_triple",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG30_MagicItem_435"),
                "goldenizer_supply",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_end",
                "ProxySafe",
                "golden_triple",
                "tavern_spell",
                "turn_end");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_817"),
                "rendle_sticker",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_end",
                "turn_end");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_419"),
                "exquisite_dishware",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "turn_end",
                "turn_end",
                "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG32_MagicItem_925"),
                "hackerfin_portrait",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "turn_end",
                "ProxySafe",
                "battlecry",
                "turn_end",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_753"),
                "murky_sticker",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "turn_end",
                "battlecry",
                "turn_end",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_890"),
                "cliffdiver_sticker",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_end",
                "battlecry",
                "turn_end",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_832"),
                "windfall_portrait",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_end",
                "sell_trigger",
                "turn_end",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_832t"),
                "windfall_portrait",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "turn_end",
                "sell_trigger",
                "turn_end",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_894"),
                "blessing_portrait",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "tavern_spell",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_711"),
                "marine_signet",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "tavern_spell",
                "tavern_spell",
                "play_trigger",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_743"),
                "electrode_attractor",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "shop_refresh",
                "magnetic",
                "tribe_pool",
                "shop_refresh");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_366"),
                "guiding_candle",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "shop_refresh",
                "shop_refresh");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_862"),
                "upstart_embers",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "shop_refresh",
                "tribe_pool",
                "shop_refresh");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_930"),
                "warband_whistle",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "shop_refresh",
                "shop_refresh");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG32_MagicItem_806"),
                "battlecruiser_portrait",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "shop_refresh",
                "ProxySafe",
                "shop_refresh");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_152"),
                "demonic_tapestry",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "shop_refresh",
                "shop_refresh",
                "health_cost");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_891"),
                "finleys_helmet",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "shop_refresh",
                "tribe_pool",
                "shop_refresh");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_423"),
                "innkeepers_stein",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "shop_refresh",
                "tribe_pool",
                "shop_refresh");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_991"),
                "felbat_portrait",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "tribe_specific",
                "tribe_pool",
                "shop_aura");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_541"),
                "nether_pendant",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "tribe_specific",
                "tribe_pool",
                "hero_damage",
                "shop_aura");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_841"),
                "glowing_gauntlet",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "tribe_specific",
                "tribe_pool",
                "shop_aura");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_821"),
                "pilgrimp_sticker",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "economy",
                "tribe_pool",
                "health_cost");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_822"),
                "bazaar_sticker",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "tavern_spell",
                "tavern_spell",
                "health_cost");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG35_MagicItem_750"),
                "magicfin_sticker",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "buy_trigger",
                "ProxySafe",
                "tavern_spell",
                "buy_trigger",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_701"),
                "eye_of_sargeras",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "buy_trigger",
                "buy_trigger",
                "health_cost");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG32_MagicItem_957"),
                "grifter_portrait",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "buy_trigger",
                "ProxySafe",
                "tribe_pool",
                "buy_trigger");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_230"),
                "extravagant_scale",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "economy",
                "spend_gold",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_999"),
                "fancy_spellbook",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "economy",
                "spend_gold",
                "tavern_spell");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_232"),
                "shark_cannon",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "economy",
                "spend_gold",
                "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG32_MagicItem_205"),
                "maw_caster_portrait",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Medium,
                "economy",
                "ProxySafe",
                "destroy",
                "tavern_spell");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG35_MagicItem_820"),
                "safety_patch",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "economy",
                "ProxySafe",
                "economy");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_709"),
                "electromagnetic_device",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "discover",
                "magnetic",
                "discover",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_709t"),
                "electromagnetic_device",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "discover",
                "magnetic",
                "discover",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_362"),
                "innkeepers_hearth",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "discover",
                "discover",
                "stat_grant");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_362t"),
                "innkeepers_hearth",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "discover",
                "discover",
                "stat_grant");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_821"),
                "kaleidoscope",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "discover",
                "discover",
                "economy");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_821t"),
                "kaleidoscope",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "discover",
                "discover",
                "golden_triple",
                "economy");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_306"),
                "jailer_sticker",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "spellcraft",
                "spellcraft",
                "tribe_pool",
                "destroy");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_733"),
                "jailer_sticker",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "spellcraft",
                "spellcraft",
                "tribe_pool",
                "destroy");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_429"),
                "demonblood_gourd",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "spellcraft",
                "spellcraft",
                "tribe_pool",
                "shop_pool",
                "stat_grant");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_902"),
                "statue_of_hireek",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "tavern_spell",
                "tavern_spell",
                "tribe_pool",
                "shop_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_828"),
                "shaker_portrait",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "copy_generate",
                "tribe_pool",
                "spellcraft");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_931"),
                "transcribing_typewriter",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "copy_generate",
                "buy_trigger");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_931t"),
                "transcribing_typewriter",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "copy_generate",
                "buy_trigger");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG32_MagicItem_807"),
                "curator_sticker",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "copy_generate",
                "ProxySafe",
                "golden_triple",
                "tribe_pool",
                "venomous");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_350"),
                "splinter_of_aurum",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "economy",
                "economy",
                "golden_triple",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_304"),
                "horn_of_summoning",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "copy_generate",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG35_MagicItem_815"),
                "magicians_top_hat",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "copy_generate",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_400"),
                "shrine_of_evolution",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "stats",
                "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(
                catalog.GetByCardId("BG35_MagicItem_922"),
                "tide_raiser_portrait",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "copy_generate",
                "ProxySafe",
                "combat_event",
                "spell_cast",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_361"),
                "portable_factory",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "turn_start",
                "discover",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG32_MagicItem_361t"),
                "portable_factory",
                TrinketSlotKind.Greater,
                TrinketPowerLevel.Strong,
                "turn_start",
                "discover",
                "turn_start",
                "tribe_pool");
            AssertCatalogTrinket(
                catalog.GetByCardId("BG30_MagicItem_434"),
                "replica_cathedral",
                TrinketSlotKind.Lesser,
                TrinketPowerLevel.Medium,
                "spell_cast",
                "spell_cast",
                "tavern_spell_cast");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_952"), "jarred_frostling", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "combat_start", "ProxySafe", "deathrattle", "combat_event", "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG35_MagicItem_714"), "powder_keg", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "combat_start", "ProxySafe", "deathrattle", "combat_event", "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_918"), "promo_portrait", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "combat_start", "ProxySafe", "combat_event", "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG35_MagicItem_740"), "sky_golem_portrait", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "combat_start", "ProxySafe", "deathrattle", "combat_event", "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG32_MagicItem_365"), "valdrakken_wind_chimes", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "combat_start", "ProxySafe", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_411"), "hoggy_bank", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "combat_start", "deathrattle", "blood_gem", "combat_event", "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_407"), "ship_in_a_bottle", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "combat_start", "ProxySafe", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_864"), "gilnean_thorned_rose", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "avenge", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_546"), "jar_o_gems", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "combat_event", "blood_gem", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_438t"), "mug_of_the_sire", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "combat_event", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG35_MagicItem_431t"), "thornspike_pauldron", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "deathrattle", "deathrattle", "blood_gem", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_427"), "tiger_carving", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "combat_event", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_427t"), "tiger_carving", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "combat_event", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_978"), "blingtrons_sunglasses", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "combat_event", "divine_shield", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG35_MagicItem_430"), "scrapsmith_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "combat_event", "blood_gem", "combat_event", "tribe_pool", "taunt");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_917"), "rusty_trident", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "combat_start", "ProxySafe", "deathrattle", "spellcraft", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_981"), "eye_of_dalaran", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "combat_event", "tavern_spell", "combat_event");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_923"), "elementium_chest", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "economy", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG35_MagicItem_742"), "accord_o_tron_portrait", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "turn_end", "magnetic", "tribe_pool", "turn_end");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_921"), "flagbearer_portrait", TrinketSlotKind.Greater, TrinketPowerLevel.Medium, "deathrattle", "deathrattle", "combat_event", "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG35_MagicItem_156"), "flaming_portrait", TrinketSlotKind.Greater, TrinketPowerLevel.Medium, "tribe_specific", "ProxySafe", "turn_end", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG32_MagicItem_204"), "kelthuzad_portrait", TrinketSlotKind.Greater, TrinketPowerLevel.Medium, "tribe_specific", "destroy", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_943"), "surveyor_portrait", TrinketSlotKind.Greater, TrinketPowerLevel.Medium, "tribe_specific", "blood_gem", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG35_MagicItem_433"), "vinespeaker_portrait", TrinketSlotKind.Greater, TrinketPowerLevel.Medium, "tribe_specific", "blood_gem", "deathrattle", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_869"), "felblood_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "tribe_specific", "battlecry", "shop_refresh", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG32_MagicItem_830"), "felemental_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "tribe_specific", "battlecry", "shop_refresh", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG32_MagicItem_953"), "goldgrubber_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "tribe_specific", "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_777"), "goose_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "tribe_specific", "ProxySafe", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG32_MagicItem_824"), "implicator_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "tribe_specific", "consume", "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG32_MagicItem_820"), "impulsive_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "tribe_specific", "ProxySafe", "deathrattle", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_803"), "kaboom_bot_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "tribe_specific", "deathrattle", "combat_event", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG32_MagicItem_803"), "macaw_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "tribe_specific", "battlecry", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_868"), "rewinder_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "hero_damage", "hero_damage", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG32_MagicItem_887"), "shadowy_elixir", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "hero_damage", "hero_damage", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_825"), "smuggler_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "tribe_specific", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG32_MagicItem_416"), "war_drum", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "battlecry", "battlecry", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG35_MagicItem_154"), "urzul_sticker", TrinketSlotKind.Greater, TrinketPowerLevel.Medium, "tribe_specific", "consume", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG35_MagicItem_713"), "trusty_crowbar", TrinketSlotKind.Greater, TrinketPowerLevel.Medium, "tribe_specific", "add_to_hand", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG32_MagicItem_282"), "turbocharged_drill", TrinketSlotKind.Greater, TrinketPowerLevel.Medium, "magnetic", "magnetic", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_843t"), "horde_keychain", TrinketSlotKind.Greater, TrinketPowerLevel.Medium, "tribe_specific", "tribe_pool");
            AssertCatalogTrinket(catalog.GetByCardId("BG32_MagicItem_804"), "selfless_portrait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "battlecry", "battlecry");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG32_MagicItem_367"), "ghastly_sticker", TrinketSlotKind.Greater, TrinketPowerLevel.Medium, "turn_end", "ProxySafe", "turn_end");
            AssertCatalogTrinket(catalog.GetByCardId("BG35_MagicItem_752"), "young_murk_eye_sticker", TrinketSlotKind.Greater, TrinketPowerLevel.Medium, "turn_end", "battlecry", "turn_end", "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_703"), "mystery_cube", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "turn_start", "ProxySafe", "trinket_choice");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG35_MagicItem_816"), "orb_of_the_unknown", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "trinket_choice", "ProxySafe", "trinket_choice");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG35_MagicItem_816t"), "orb_of_the_unknown", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "economy", "ProxySafe", "trinket_choice", "gold");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_994"), "yogg_tastic_pastry", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "turn_start", "ProxySafe", "yogg_proxy");
            AssertCatalogTrinket(catalog.GetByCardId("BG30_MagicItem_707"), "tickatus_sticker", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "turn_start", "discover", "turn_start");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_426"), "colorful_compass", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "turn_start", "ProxySafe", "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_426t"), "colorful_compass", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "turn_start", "ProxySafe", "tribe_pool");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG32_MagicItem_901"), "gold_plated_compass", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "shop_refresh", "ProxySafe", "golden_triple", "shop_refresh");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_973"), "minion_bait", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "shop_refresh", "ProxySafe", "tribe_pool", "shop_refresh");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG35_MagicItem_823"), "timeworn_candelabra", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "discover", "ProxySafe", "discover", "timewarp_proxy");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG35_MagicItem_823t"), "timeworn_candelabra", TrinketSlotKind.Greater, TrinketPowerLevel.Strong, "discover", "ProxySafe", "discover", "timewarp_proxy");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_930"), "burgling_claw", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "turn_start", "ProxySafe", "opponent_history");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_888"), "souvenir_stand", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "buy_trigger", "ProxySafe", "trinket_choice");
            AssertCatalogTrinketWithProxyLevel(catalog.GetByCardId("BG30_MagicItem_891"), "trip_vouchers", TrinketSlotKind.Lesser, TrinketPowerLevel.Medium, "buy_trigger", "ProxySafe", "trinket_choice");
        }

        [Test]
        public void Setup_DisabledTrinketsRejectsDebugOffers()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { EnableTrinkets = false });

            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets)));
        }

        [Test]
        public void Setup_SelectedTrinketPoolsFilterLesserAndGreaterSeparately()
        {
            var catalog = TrinketCatalogLoader.LoadFromResources();
            var activeTribes = TribeAvailabilityRules.AllPlayableTribes();
            var lesser = catalog.Lesser.First(trinket =>
                trinket.ImplementationStatus == TrinketImplementationStatus.Implemented &&
                trinket.OfferPoolStatus == TrinketOfferPoolStatus.Offerable &&
                TribeAvailabilityRules.IsTrinketAvailable(trinket, activeTribes));
            var greater = catalog.Greater.First(trinket =>
                trinket.ImplementationStatus == TrinketImplementationStatus.Implemented &&
                trinket.OfferPoolStatus == TrinketOfferPoolStatus.Offerable &&
                TribeAvailabilityRules.IsTrinketAvailable(trinket, activeTribes));
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    ActiveTribes = activeTribes,
                    EnableTrinkets = true,
                    EnabledLesserTrinketCardIds = new List<string> { lesser.CardId },
                    EnabledGreaterTrinketCardIds = new List<string> { greater.CardId }
                });

            CollectionAssert.AreEquivalent(
                new[] { lesser.CardId },
                service.GetDebugSelectableTrinkets(TrinketSlotKind.Lesser).Select(trinket => trinket.CardId).ToList());
            CollectionAssert.AreEquivalent(
                new[] { greater.CardId },
                service.GetDebugSelectableTrinkets(TrinketSlotKind.Greater).Select(trinket => trinket.CardId).ToList());
            CollectionAssert.AreEquivalent(new[] { lesser.CardId }, service.State.EnabledLesserTrinketCardIds);
            CollectionAssert.AreEquivalent(new[] { greater.CardId }, service.State.EnabledGreaterTrinketCardIds);
        }

        [Test]
        public void TrinketChoices_RespectActiveTribeBan()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { ActiveTribes = new List<Tribe> { Tribe.Murloc } });

            service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));
            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.IsTrue(request.Options.Count > 0);
            Assert.IsTrue(request.Options
                .Select(option => service.TrinketCatalog.GetByCardId(option.SourceId))
                .All(definition => TribeAvailabilityRules.IsTrinketAvailable(definition, service.State.ActiveTribes)));
        }

        [Test]
        public void TrinketAvailability_ParsesCanonicalUppercaseRaceTags()
        {
            var dragon = new TrinketDefinition { AssociatedRaces = new List<string> { "DRAGON" } };
            var mech = new TrinketDefinition { AssociatedRaces = new List<string> { "MECHANICAL" } };

            Assert.IsTrue(TribeAvailabilityRules.IsTrinketAvailable(dragon, new[] { Tribe.Dragon }));
            Assert.IsFalse(TribeAvailabilityRules.IsTrinketAvailable(dragon, new[] { Tribe.Murloc }));
            Assert.IsTrue(TribeAvailabilityRules.IsTrinketAvailable(mech, new[] { Tribe.Mech }));
            Assert.IsFalse(TribeAvailabilityRules.IsTrinketAvailable(mech, new[] { Tribe.Dragon }));
        }

        [Test]
        public void TrinketChoices_PrioritizeBoardHandDirectionExpansionAndGeneric()
        {
            var service = CreateDirectionalTrinketOfferService(24680);

            service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));
            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;

            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual(4, request.Options.Count);

            var active = service.State.ActiveTribes;
            var main = new List<Tribe> { Tribe.Beast, Tribe.Murloc };
            var definitions = request.Options
                .Select(option => service.TrinketCatalog.GetByCardId(option.SourceId))
                .ToList();
            var legal = LegalOfferableTrinkets(service, TrinketSlotKind.Lesser, active);

            Assert.IsTrue(definitions.All(definition => TribeAvailabilityRules.IsTrinketAvailable(definition, active)));
            if (legal.Count(definition => IsFocusTrinket(definition, active, main)) >= 2)
            {
                Assert.GreaterOrEqual(definitions.Count(definition => IsFocusTrinket(definition, active, main)), 2);
            }

            if (legal.Any(definition => IsExpansionTrinket(definition, active, main)))
            {
                Assert.IsTrue(definitions.Any(definition => IsExpansionTrinket(definition, active, main)));
            }

            if (legal.Any(definition => IsGenericTrinket(definition, active)))
            {
                Assert.IsTrue(definitions.Any(definition => IsGenericTrinket(definition, active)));
            }
        }

        [Test]
        public void TrinketChoices_AreDeterministicForSameSeedAndState()
        {
            var first = CreateDirectionalTrinketOfferService(24681);
            var second = CreateDirectionalTrinketOfferService(24681);

            first.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));
            second.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));

            var firstIds = first.State.Player.Tavern.AdvancedMechanics.PendingChoice.Options
                .Select(option => option.SourceId)
                .ToList();
            var secondIds = second.State.Player.Tavern.AdvancedMechanics.PendingChoice.Options
                .Select(option => option.SourceId)
                .ToList();

            CollectionAssert.AreEqual(firstIds, secondIds);
        }

        [Test]
        public void TickatusSticker_IsExactAndVisibleWhenProxySafeChoicesAreHidden()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { ShowProxySafe = false });

            var tickatusSticker = service.TrinketCatalog.GetByCardId("BG30_MagicItem_707");
            Assert.AreEqual("Exact", tickatusSticker.ProxyLevel);
            Assert.IsTrue(service.GetDebugSelectableTrinkets(TrinketSlotKind.Lesser)
                .Any(trinket => trinket.CardId == "BG30_MagicItem_707"));
        }

        [Test]
        public void DebugReplaceTrinket_ReplacesSlotThroughEquipFlow()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 0;

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG30_MagicItem_414", CardKind.Trinket, 0));
            Assert.AreEqual("BG30_MagicItem_414", service.State.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId);

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG35_MagicItem_850", CardKind.Trinket, 0));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual("BG35_MagicItem_850", trinkets.LesserTrinketId);
            Assert.AreEqual(1, trinkets.Equipped.Count(equipped => equipped.SlotKind == TrinketSlotKind.Lesser));
            Assert.AreEqual("BG35_MagicItem_850", trinkets.Equipped.Single(equipped => equipped.SlotKind == TrinketSlotKind.Lesser).TrinketId);
            Assert.AreEqual(1, service.State.Player.Tavern.AdvancedMechanics.Equipped.Count(equipped =>
                equipped.Kind == AdvancedMechanicKind.Trinket &&
                equipped.Slot == TrinketSlotKind.Lesser.ToString()));
        }

        [Test]
        public void PaidReplacement_InsufficientGoldPreservesOldTrinketAndPendingChoice()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG30_MagicItem_880");
            QueueTrinketReplacementChoice(service, "BG32_MagicItem_858");
            service.State.Player.Tavern.Gold = 0;

            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0)));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual("BG30_MagicItem_880", trinkets.LesserTrinketId);
            Assert.IsNotNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);
            Assert.AreEqual("BG32_MagicItem_858", service.State.Player.Tavern.AdvancedMechanics.PendingChoice.Options[0].SourceId);
        }

        [Test]
        public void DebugReplaceTrinket_RemovesOldBoardAndShopAurasImmediately()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var boardMinion = TestShopMinion("replacement-board-aura", 3, 4);
            var shopMinion = TestShopMinion("replacement-shop-aura", 5, 6);
            service.State.Player.Board.Add(boardMinion);
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(shopMinion);

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG30_MagicItem_880", CardKind.Trinket, 0));
            Assert.AreEqual(5, boardMinion.Attack);
            Assert.AreEqual(5, boardMinion.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG30_MagicItem_879", CardKind.Trinket, 0));
            Assert.AreEqual(3, boardMinion.Attack);
            Assert.AreEqual(4, boardMinion.MaxHealth);
            Assert.AreEqual(6, shopMinion.Attack);
            Assert.AreEqual(7, shopMinion.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG32_MagicItem_858", CardKind.Trinket, 0));
            Assert.AreEqual(5, shopMinion.Attack);
            Assert.AreEqual(6, shopMinion.MaxHealth);
        }

        [Test]
        public void TickatusSticker_DiscoversTierThreeDarkmoonPrizeOnEquipAndRepeatsEveryThreeTurns()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            tavern.Gold = 20;

            EquipTrinket(service, "BG30_MagicItem_707");

            AssertDarkmoonPrizeDiscover(service, "tickatus-sticker", 3);
            Assert.AreEqual(4, tavern.AdvancedMechanics.Counters["trinket_tickatus_sticker_due_round"]);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            tavern = service.State.Player.Tavern;
            Assert.IsNull(tavern.Discover);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            tavern = service.State.Player.Tavern;

            AssertDarkmoonPrizeDiscover(service, "tickatus-sticker", 3);
            Assert.AreEqual(7, tavern.AdvancedMechanics.Counters["trinket_tickatus_sticker_due_round"]);
        }

        [Test]
        public void DebugOfferAndChooseMechanicOption_EquipSelectedTrinketAndDeductCost()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));
            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual(4, request.Options.Count);
            Assert.IsTrue(request.Options.All(option =>
            {
                var definition = service.TrinketCatalog.GetByCardId(option.SourceId);
                return definition.ImplementationStatus == TrinketImplementationStatus.Implemented &&
                    definition.OfferPoolStatus == TrinketOfferPoolStatus.Offerable;
            }));
            var expected = service.TrinketCatalog.GetByCardId(request.Options[0].SourceId);
            Assert.AreEqual(expected.ImagePath, request.Options[0].ImagePath);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(entry => entry.Message.Contains("已提供小型饰品选项")));

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var pendingAfterEquip = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsTrue(pendingAfterEquip == null || pendingAfterEquip.RequestId != request.RequestId);
            Assert.AreEqual(expected.CardId, service.State.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId);
            var expectedGold = 10 - expected.Cost;
            if (expected.EffectIds.Contains("ornate_clock") || expected.EffectIds.Contains("wax_imprinter"))
            {
                expectedGold += 2;
            }

            if (expected.EffectIds.Contains("bob_blehead"))
            {
                expectedGold += 2;
            }

            if (expected.EffectIds.Contains("mysterious_orb"))
            {
                expectedGold += 8;
            }

            Assert.AreEqual(expectedGold, service.State.Player.Tavern.Gold);
            Assert.AreEqual(1, service.State.Player.Tavern.AdvancedMechanics.Equipped.Count);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(entry => entry.Message == "已装备小型饰品：" + expected.Name + "。"));
            Assert.IsFalse(service.State.Player.Tavern.RecruitLog.Any(entry => entry.Message.Contains(expected.CardId) || entry.Message.Contains("ImplementationStatus") || entry.Message.Contains("proxy")));
        }

        [Test]
        public void TrinketRuntimeLogs_PreserveEnglishMode()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { UseEnglish = true });
            service.State.Player.Tavern.Gold = 20;
            QueueTrinketChoice(service, "BG30_MagicItem_973");

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(entry => entry.Message == "Equipped Lesser Trinket: Minion Bait."));
            Assert.IsFalse(service.State.Player.Tavern.RecruitLog.Any(entry => entry.Message.Contains("已装备小型饰品")));
        }

        [Test]
        public void DebugOfferGreaterTrinkets_UsesOnlyImplementedOfferableTrinkets()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.DebugOfferGreaterTrinkets));
            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;

            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual(4, request.Options.Count);
            Assert.IsTrue(request.Options.All(option =>
            {
                var definition = service.TrinketCatalog.GetByCardId(option.SourceId);
                return definition.SlotKind == TrinketSlotKind.Greater &&
                    definition.ImplementationStatus == TrinketImplementationStatus.Implemented &&
                    definition.OfferPoolStatus == TrinketOfferPoolStatus.Offerable;
            }));
        }

        [Test]
        public void BobsTipJar_OnEquipGrantsGoldAndPersistentMaxGold()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 3;
            service.State.Player.Tavern.MaxGold = 3;

            QueueTrinketChoice(service, "BG30_MagicItem_996");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual("BG30_MagicItem_996", trinkets.GreaterTrinketId);
            Assert.AreEqual(7, service.State.Player.Tavern.Gold);
            Assert.AreEqual(7, service.State.Player.Tavern.MaxGold);
            Assert.AreEqual(4, trinkets.ExtraMaxGold);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(entry => entry.Message.Contains("获得4枚铸币") && entry.Message.Contains("铸币上限提高4枚")));
            Assert.IsFalse(service.State.Player.Tavern.RecruitLog.Any(entry => entry.Message.Contains("Bob's Tip Jar:")));
        }

        [Test]
        public void GoblinWallet_EndTurnIncreasesFutureMaxGold()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 10;

            QueueTrinketChoice(service, "BG30_MagicItem_847");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(1, trinkets.ExtraMaxGold);
            Assert.AreEqual(TavernRules.GetMaxGoldForRound(2) + 1, service.State.Player.Tavern.MaxGold);
        }

        [Test]
        public void KodoLeatherPouch_AfterBuyBuffsTwoFriendlyMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var minions = MinionCatalogLoader.LoadFromResources().All
                .Where(minion => minion.InPool && minion.TavernTier == 1)
                .Take(2)
                .ToList();
            service.State.Player.Board.Add(MinionFactory.Create(minions[0], BoardSide.Player, "trinket-test-1"));
            service.State.Player.Board.Add(MinionFactory.Create(minions[1], BoardSide.Player, "trinket-test-2"));
            var before = service.State.Player.Board
                .Select(minion => new { minion.InstanceId, minion.Attack, minion.MaxHealth })
                .ToList();
            service.State.Player.Tavern.Gold = 10;

            QueueTrinketChoice(service, "BG30_MagicItem_414");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null);
            service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));

            foreach (var minion in service.State.Player.Board)
            {
                var original = before.First(entry => entry.InstanceId == minion.InstanceId);
                Assert.AreEqual(original.Attack + 2, minion.Attack);
                Assert.AreEqual(original.MaxHealth + 1, minion.MaxHealth);
            }
        }

        [Test]
        public void DalaranCheeseWheel_AppliesShopAuraAndImprovesAfterFourRefreshes()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_879");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            AssertShopMinionsHaveAtLeastBonus(service, 1, 1);

            for (var refresh = 0; refresh < 4; refresh += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
            }

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(4, trinkets.DalaranCheeseWheelRefreshes);
            Assert.AreEqual(2, trinkets.DalaranCheeseWheelBonusAttack);
            AssertShopMinionsHaveAtLeastBonus(service, 2, 2);
        }

        [Test]
        public void DalaranCheeseWheel_ReplacingLastCopyResetsGrowthProgress()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG30_MagicItem_879", CardKind.Trinket, 0));
            for (var index = 0; index < 3; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
            }

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(3, trinkets.DalaranCheeseWheelRefreshes);

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG32_MagicItem_858", CardKind.Trinket, 0));
            Assert.AreEqual(0, trinkets.DalaranCheeseWheelRefreshes);
            Assert.AreEqual(0, trinkets.DalaranCheeseWheelBonusAttack);
            Assert.AreEqual(0, trinkets.DalaranCheeseWheelBonusHealth);

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG30_MagicItem_879", CardKind.Trinket, 0));
            Assert.AreEqual(0, trinkets.DalaranCheeseWheelRefreshes);
            Assert.AreEqual(1, trinkets.DalaranCheeseWheelBonusAttack);
            Assert.AreEqual(1, trinkets.DalaranCheeseWheelBonusHealth);
        }

        [Test]
        public void OrnateClock_OnEquipGrantsGoldAndOffersGreaterNextTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Round = 6;
            service.State.Player.Tavern.Gold = 5;

            QueueTrinketChoice(service, "BG32_MagicItem_271");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(7, service.State.Player.Tavern.Gold);
            Assert.AreEqual(7, trinkets.OrnateClockGreaterOfferRound);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;

            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual(TrinketSlotKind.Greater.ToString(), request.Slot);
            Assert.IsTrue(request.Options.All(option =>
                service.TrinketCatalog.GetByCardId(option.SourceId).SlotKind == TrinketSlotKind.Greater));
            Assert.AreEqual(0, trinkets.OrnateClockGreaterOfferRound);
        }

        [Test]
        public void WornTreasureMap_GainsTenGoldTwoTurnsAfterEquipOnce()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Round = 4;
            service.State.Player.Tavern.Gold = 5;

            QueueTrinketChoice(service, "BG32_MagicItem_428");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(6, trinkets.WornTreasureMapDueRound);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.IsFalse(trinkets.WornTreasureMapClaimed);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;

            Assert.IsTrue(trinkets.WornTreasureMapClaimed);
            Assert.AreEqual(0, trinkets.WornTreasureMapDueRound);
            Assert.AreEqual(service.State.Player.Tavern.MaxGold + 10, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void WornTreasureMap_ReplacingBeforeClaimClearsScheduleAndReequipStartsFresh()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Round = 4;

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG32_MagicItem_428", CardKind.Trinket, 0));
            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(6, trinkets.WornTreasureMapDueRound);

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG32_MagicItem_858", CardKind.Trinket, 0));
            Assert.AreEqual(0, trinkets.WornTreasureMapDueRound);
            Assert.IsFalse(trinkets.WornTreasureMapClaimed);

            service.State.Round = 7;
            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG32_MagicItem_428", CardKind.Trinket, 0));

            Assert.AreEqual(9, trinkets.WornTreasureMapDueRound);
            Assert.IsFalse(trinkets.WornTreasureMapClaimed);
        }

        [Test]
        public void StuffedCoinPurse_ReachingTierSixGrantsTwelveGoldOnce()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Tier = 5;
            service.State.Player.Tavern.UpgradeCost = 0;
            service.State.Player.Tavern.Gold = 0;
            UnlockTierSevenForTest(service);

            QueueTrinketChoice(service, "BG35_MagicItem_814");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.IsFalse(trinkets.StuffedCoinPurseClaimed);

            service.Apply(new GameCommand(GameCommandType.UpgradeTavern));

            Assert.AreEqual(6, service.State.Player.Tavern.Tier);
            Assert.IsTrue(trinkets.StuffedCoinPurseClaimed);
            Assert.AreEqual(12, service.State.Player.Tavern.Gold);

            service.State.Player.Tavern.UpgradeCost = 0;
            service.Apply(new GameCommand(GameCommandType.UpgradeTavern));

            Assert.AreEqual(7, service.State.Player.Tavern.Tier);
            Assert.AreEqual(12, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void BobBlehead_OnEquipGainsGoldAndFiltersFutureTavernOffers()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Tier = 4;
            service.State.Player.Tavern.Gold = 5;

            QueueTrinketChoice(service, "BG30_MagicItem_998");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(7, service.State.Player.Tavern.Gold);

            service.State.Player.Tavern.Gold = 20;
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var offeredCards = service.State.Player.Tavern.Shop
                .Where(card => card != null)
                .ToList();
            Assert.IsNotEmpty(offeredCards);
            Assert.IsTrue(offeredCards.All(card => card.TavernTier >= 3));
        }

        [Test]
        public void MysteriousOrb_NextScheduledGreaterChoiceUsesLesserPool()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Round = 8;
            service.State.Player.Tavern.Gold = 0;

            QueueTrinketChoice(service, "BG35_MagicItem_818");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual("BG35_MagicItem_818", trinkets.LesserTrinketId);
            Assert.AreEqual(8, service.State.Player.Tavern.Gold);
            Assert.IsTrue(trinkets.MysteriousOrbNextTrinketIsLesser);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;

            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual(TrinketSlotKind.Greater.ToString(), request.Slot);
            Assert.IsTrue(request.Source.Contains("mysterious_orb"));
            Assert.IsTrue(request.Options.All(option => option.Slot == TrinketSlotKind.Greater.ToString()));
            Assert.IsTrue(request.Options.All(option =>
                service.TrinketCatalog.GetByCardId(option.SourceId).SlotKind == TrinketSlotKind.Lesser));
            Assert.IsFalse(request.Options.Any(option => option.SourceId == "BG35_MagicItem_818"));
            Assert.IsFalse(trinkets.MysteriousOrbNextTrinketIsLesser);

            var selected = request.Options[0];
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual("BG35_MagicItem_818", trinkets.LesserTrinketId);
            Assert.AreEqual(selected.SourceId, trinkets.GreaterTrinketId);
            Assert.AreEqual(
                TrinketSlotKind.Greater.ToString(),
                service.State.Player.Tavern.AdvancedMechanics.Equipped.Last().Slot);
        }

        [Test]
        public void SacrificialAltar_OnEquipRemovesBoardAndGainsGoldPerMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var minions = MinionCatalogLoader.LoadFromResources().All
                .Where(minion => minion.InPool && minion.TavernTier == 1)
                .Take(3)
                .ToList();
            for (var index = 0; index < minions.Count; index += 1)
            {
                service.State.Player.Board.Add(MinionFactory.Create(minions[index], BoardSide.Player, "altar-test-" + index));
            }
            service.State.Player.Board[0].CardId = "BG28_300";
            service.State.Player.Board[0].Name = "Harmless Bonehead";
            if (!service.State.Player.Board[0].Keywords.Contains(Keyword.Deathrattle))
            {
                service.State.Player.Board[0].Keywords.Add(Keyword.Deathrattle);
            }
            service.State.Player.Tavern.Gold = 2;

            QueueTrinketChoice(service, "BG32_MagicItem_844");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.IsEmpty(service.State.Player.Board);
            Assert.AreEqual(10, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void BartendOTronsOilcan_ReducesUpgradeCostOnEquipAndTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.UpgradeCost = 8;

            QueueTrinketChoice(service, "BG30_MagicItem_705");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(5, service.State.Player.Tavern.UpgradeCost);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(1, service.State.Player.Tavern.UpgradeCost);
        }

        [Test]
        public void WaxImprinter_GainsGoldAndDamagesHeroOnEquipAndTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Health = 30;
            service.State.Player.Tavern.Gold = 3;

            QueueTrinketChoice(service, "BG32_MagicItem_823");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(5, service.State.Player.Tavern.Gold);
            Assert.AreEqual(28, service.State.Player.Health);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(26, service.State.Player.Health);
            Assert.AreEqual(service.State.Player.Tavern.MaxGold + 2, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void CursedCrystal_AfterRefreshBuffsCurrentShopMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_150");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var shopMinions = service.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            Assert.IsNotEmpty(shopMinions);
            Assert.IsTrue(shopMinions.All(minion => minion.Attack >= minion.BaseAttack + 3));
            Assert.IsTrue(shopMinions.All(minion => minion.MaxHealth >= minion.BaseHealth + 3));
            Assert.IsTrue(shopMinions.All(minion => minion.Enchantments.Any(enchantment => enchantment.SourceId == "Cursed Crystal")));
        }

        [Test]
        public void LightningInABottle_AfterRefreshGivesHighestAttackStatsToLowestAttackShopMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_852");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var shopMinions = service.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            Assert.IsNotEmpty(shopMinions);
            var buffed = shopMinions
                .Where(minion => minion.Enchantments.Any(enchantment => enchantment.SourceId == "Lightning in a Bottle"))
                .ToList();
            Assert.AreEqual(1, buffed.Count);
            Assert.Greater(buffed[0].Attack, buffed[0].BaseAttack);
            Assert.Greater(buffed[0].MaxHealth, buffed[0].BaseHealth);
        }

        [Test]
        public void RockinMusicBoxAndScraperSticker_AddGeneratedCardsOnEquipAndTurnStart()
        {
            var battlecryService = MatchService.CreateWithDefaultCatalog(12345);
            battlecryService.State.Player.Tavern.Gold = 10;

            QueueTrinketChoice(battlecryService, "BG30_MagicItem_430");
            battlecryService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, battlecryService.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(battlecryService.State.Player.Tavern.Hand.All(card => card.Keywords.Contains(Keyword.Battlecry)));

            battlecryService.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, battlecryService.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(battlecryService.State.Player.Tavern.Hand.All(card => card.Keywords.Contains(Keyword.Battlecry)));

            var magneticService = MatchService.CreateWithDefaultCatalog(12345);
            magneticService.State.Player.Tavern.Gold = 10;

            QueueTrinketChoice(magneticService, "BG35_MagicItem_301");
            magneticService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, magneticService.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(magneticService.State.Player.Tavern.Hand.All(card =>
                card.Tribes.Contains(Tribe.Mech) && card.Keywords.Contains(Keyword.Magnetic)));

            magneticService.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, magneticService.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(magneticService.State.Player.Tavern.Hand.All(card =>
                card.Tribes.Contains(Tribe.Mech) && card.Keywords.Contains(Keyword.Magnetic)));
        }

        [Test]
        public void ReflectivePendant_CopiesPlainFriendlyMinionOnEquipAndTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = TestShopMinion("reflective-source", 2, 3);
            source.Attack = 9;
            source.Health = 8;
            source.MaxHealth = 8;
            source.Enchantments.Add(new Enchantment { SourceId = "test-buff", AttackBonus = 7, HealthBonus = 5 });
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_706");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            var copy = service.State.Player.Tavern.Hand[0];
            Assert.AreEqual(source.CardId, copy.CardId);
            Assert.AreEqual(2, copy.Attack);
            Assert.AreEqual(3, copy.MaxHealth);
            Assert.IsEmpty(copy.Enchantments);
            Assert.AreEqual(PoolSource.Copy, copy.PoolSource);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == source.CardId));
        }

        [Test]
        public void SellementalPortrait_AddsSellementalOnEquipAndTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 10;

            QueueTrinketChoice(service, "BG32_MagicItem_831");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BGS_115"));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BGS_115"));
        }

        [Test]
        public void ShamanPrayerBeads_AfterBuyingTwoBattlecryMinionsAddsRandomBattlecryMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_982");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestShopMinion("battlecry-buy-one", 2, 2, Keyword.Battlecry),
                TestShopMinion("battlecry-buy-two", 3, 3, Keyword.Battlecry),
                TestShopMinion("plain-buy", 4, 4)
            };

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(1, trinkets.ShamanPrayerBeadsBattlecryBuys);
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));

            Assert.AreEqual(0, trinkets.ShamanPrayerBeadsBattlecryBuys);
            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Last().Keywords.Contains(Keyword.Battlecry));
        }

        [Test]
        public void ReusableBatteries_FirstBoughtMinionEachTurnAddsStatMatchedMagneticSatellite()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_278");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestShopMinion("battery-buy-one", 5, 7),
                TestShopMinion("battery-buy-two", 3, 4)
            };

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            var satellite = service.State.Player.Tavern.Hand.Single(card => card.CardId == "MOONSTEEL_SATELLITE");
            Assert.AreEqual(5, satellite.Attack);
            Assert.AreEqual(7, satellite.MaxHealth);
            Assert.IsTrue(satellite.Keywords.Contains(Keyword.Magnetic));

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardId == "MOONSTEEL_SATELLITE"));
            Assert.AreEqual(service.State.Round, service.State.Player.Tavern.AdvancedMechanics.Trinkets.ReusableBatteriesLastTriggerRound);
        }

        [Test]
        public void BookOfMedivh_OffersTavernSpellDiscoverOnEquipAndTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_420");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var discover = service.State.Player.Tavern.Discover;
            Assert.IsNotNull(discover);
            Assert.AreEqual(1, discover.RemainingPicks);
            Assert.IsTrue(discover.Options.All(option => option.CardKind == CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            Assert.IsNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            discover = service.State.Player.Tavern.Discover;
            Assert.IsNotNull(discover);
            Assert.AreEqual(1, discover.RemainingPicks);
            Assert.IsTrue(discover.Options.All(option => option.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void GreaterBookOfMedivh_QueuesSecondTavernSpellDiscover()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_420t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(2, service.State.Player.Tavern.Discover.RemainingPicks);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(1, service.State.Player.Tavern.Discover.RemainingPicks);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(option => option.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void LavishCape_OnEquipAndTurnStartCastsForEachDifferentFriendlyType()
        {
            var service = CreateServiceWithEnabledTavernSpells(12345, "100596");
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Board.Add(TestTribeMinion("lavish-beast", 1, 1, Tribe.Beast));
            service.State.Player.Board.Add(TestTribeMinion("lavish-murloc", 2, 2, Tribe.Murloc));
            var startingAttack = service.State.Player.Board.Sum(minion => minion.Attack);

            QueueTrinketChoice(service, "BG32_MagicItem_286");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(2, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(2, service.State.Player.Tavern.TavernSpellsCastThisGame);
            Assert.AreEqual("100596", service.State.Player.Tavern.LastTavernSpellCardId);
            Assert.AreEqual(startingAttack + 8, service.State.Player.Board.Sum(minion => minion.Attack));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(4, service.State.Player.Tavern.TavernSpellsCastThisGame);
            Assert.AreEqual("100596", service.State.Player.Tavern.LastTavernSpellCardId);
            Assert.AreEqual(startingAttack + 16, service.State.Player.Board.Sum(minion => minion.Attack));
        }

        [Test]
        public void LavishCape_RandomCastsOnlyUseCurrentTavernTierOrLower()
        {
            var service = CreateServiceWithEnabledTavernSpells(12345, HastyExcavationCardId);
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Board.Add(TestTribeMinion("lavish-beast", 1, 1, Tribe.Beast));

            QueueTrinketChoice(service, "BG32_MagicItem_286");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(0, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.IsTrue(string.IsNullOrEmpty(service.State.Player.Tavern.LastTavernSpellCardId));

            service.State.Player.Tavern.Tier = 2;
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(1, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(HastyExcavationCardId, service.State.Player.Tavern.LastTavernSpellCardId);
        }

        [Test]
        public void PocketCyclone_LesserAndGreaterCastEasterlyWindsOnEquipAndTurnStart()
        {
            var lesserService = CreateServiceWithEnabledTavernSpells(12345, EasterlyWindsCardId);
            lesserService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(lesserService, "BG35_MagicItem_850");
            lesserService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, lesserService.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(1, lesserService.State.Player.Tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(EasterlyWindsCardId, lesserService.State.Player.Tavern.LastTavernSpellCardId);
            Assert.AreEqual(5, lesserService.State.Player.Tavern.RefreshRightmostBuffAttack);
            Assert.AreEqual(5, lesserService.State.Player.Tavern.RefreshRightmostBuffHealth);

            lesserService.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(1, lesserService.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(2, lesserService.State.Player.Tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(EasterlyWindsCardId, lesserService.State.Player.Tavern.LastTavernSpellCardId);
            Assert.AreEqual(10, lesserService.State.Player.Tavern.RefreshRightmostBuffAttack);
            Assert.AreEqual(10, lesserService.State.Player.Tavern.RefreshRightmostBuffHealth);

            var greaterService = CreateServiceWithEnabledTavernSpells(12345, EasterlyWindsCardId);
            greaterService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(greaterService, "BG35_MagicItem_850t");
            greaterService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(4, greaterService.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(4, greaterService.State.Player.Tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(EasterlyWindsCardId, greaterService.State.Player.Tavern.LastTavernSpellCardId);
            Assert.AreEqual(20, greaterService.State.Player.Tavern.RefreshRightmostBuffAttack);
            Assert.AreEqual(20, greaterService.State.Player.Tavern.RefreshRightmostBuffHealth);

            greaterService.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, greaterService.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(6, greaterService.State.Player.Tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(EasterlyWindsCardId, greaterService.State.Player.Tavern.LastTavernSpellCardId);
            Assert.AreEqual(30, greaterService.State.Player.Tavern.RefreshRightmostBuffAttack);
            Assert.AreEqual(30, greaterService.State.Player.Tavern.RefreshRightmostBuffHealth);
        }

        [Test]
        public void PaglesFishingRod_AddsCurrentPoolTierSevenMinionOnEquipAndTurnStart()
        {
            var tierSeven = MinionCatalogLoader.LoadFromResources().All
                .First(card => card.InPool && card.TavernTier == TavernRules.MaxTavernTier && card.PoolCount > 0 && !card.CardId.StartsWith("BGDUO"));
            var service = CreateServiceWithEnabledMinions(12345, tierSeven.CardId);
            service.State.Player.Tavern.Gold = 20;
            UnlockTierSevenForTest(service);

            QueueTrinketChoice(service, "BG30_MagicItem_993");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == tierSeven.CardId));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.Minion));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.TavernTier == TavernRules.MaxTavernTier));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == tierSeven.CardId));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.Minion));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.TavernTier == TavernRules.MaxTavernTier));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));
        }

        [Test]
        public void ExplorersBinoculars_AddsCurrentPoolTierFourMinionsOnEquip()
        {
            var tierFour = MinionCatalogLoader.LoadFromResources().All
                .First(card => card.InPool && card.TavernTier == 4 && card.PoolCount > 0 && !card.CardId.StartsWith("BGDUO"));
            var service = CreateServiceWithEnabledMinions(12345, tierFour.CardId);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_858");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == tierFour.CardId));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.Minion));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.TavernTier == 4));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));
        }

        [Test]
        public void BoomsMonsterPortrait_AddsDrBoomsMonsterOnEquipAndTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_172");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == "BG32_172"));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.Minion));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.TavernTier == 4));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.Tribes.Contains(Tribe.Mech)));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.Keywords.Contains(Keyword.Magnetic)));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == "BG32_172"));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.Minion));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.TavernTier == 4));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.Tribes.Contains(Tribe.Mech)));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.Keywords.Contains(Keyword.Magnetic)));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));
        }

        [Test]
        public void SpecifiedMinionPortraits_AddMinionOnEquip()
        {
            AssertSpecifiedMinionPortrait("BG35_MagicItem_741", "BG26_149", 7, Tribe.Mech, Keyword.Magnetic);
            AssertSpecifiedMinionPortrait("BG32_MagicItem_926", "BG35_895", 5, Tribe.Murloc, Keyword.TavernSpell);
            AssertSpecifiedMinionPortrait("BG30_MagicItem_555", "BG26_175", 6, Tribe.Elemental, Keyword.DivineShield);
            AssertSpecifiedMinionPortrait("BG32_MagicItem_998", "BG31_360", 7, Tribe.Elemental, Keyword.Taunt);
            AssertSpecifiedMinionPortrait("BG30_MagicItem_876", "BG_EX1_564", 1, null, Keyword.Battlecry);
            AssertSpecifiedMinionPortrait("BG32_MagicItem_364", "BG34_Giant_314", 5, Tribe.Dragon, Keyword.DivineShield);
            AssertSpecifiedMinionPortrait("BG35_MagicItem_310", "BG34_Giant_330", 5, Tribe.Undead, Keyword.Deathrattle, Keyword.Reborn);
            AssertSpecifiedMinionPortrait("BG30_MagicItem_821", "TB_BaconShop_HP_105t", 1, Tribe.Beast);
            AssertSpecifiedMinionPortrait("BG35_MagicItem_870", "BG34_Giant_031", 3, Tribe.Beast, Keyword.Taunt, Keyword.Reborn, Keyword.Deathrattle);
            AssertSpecifiedMinionPortrait("BG35_MagicItem_303", "BG34_Giant_072", 3, Tribe.Murloc);
            AssertSpecifiedMinionPortrait("BG32_MagicItem_283", ChargingCzarinaCardId, 5, Tribe.Mech, Keyword.DivineShield, Keyword.TavernSpell);
            AssertSpecifiedMinionPortrait("BG35_MagicItem_151t", WoodlandDefilerCardId, 4, Tribe.Demon);
            AssertSpecifiedMinionPortrait("BG35_MagicItem_151", WoodlandDefilerCardId, 4, Tribe.Demon);
            AssertSpecifiedMinionPortrait("BG30_MagicItem_921", "BG30_119", 5, Tribe.Pirate, Keyword.Deathrattle);
            AssertSpecifiedMinionPortrait("BG35_MagicItem_156", "BG34_500", 4, Tribe.Demon);
            AssertSpecifiedMinionPortrait("BG32_MagicItem_204", "BG28_308", 5, Tribe.Undead);
            AssertSpecifiedMinionPortrait("BG30_MagicItem_943", "BG30_121", 5, Tribe.Quilboar);
            AssertSpecifiedMinionPortrait("BG35_MagicItem_433", "BG35_437", 6, Tribe.Quilboar);
            AssertSpecifiedMinionPortrait("BG30_MagicItem_869", "BG29_873", 3, Tribe.Demon, Keyword.Battlecry);
            AssertSpecifiedMinionPortrait("BG32_MagicItem_830", "BG25_041", 3, Tribe.Elemental, Keyword.Battlecry);
            AssertSpecifiedMinionPortrait("BG30_MagicItem_777", "BG29_801", 2, Tribe.Beast, Keyword.Deathrattle);
            AssertSpecifiedMinionPortrait("BG32_MagicItem_820", "BG21_006", 1, Tribe.Demon, Keyword.Deathrattle);
            AssertSpecifiedMinionPortrait("BG30_MagicItem_803", "BG_BOT_606", 2, Tribe.Mech, Keyword.Deathrattle);
            AssertSpecifiedMinionPortrait("BG32_MagicItem_803", "BGS_078", 4, Tribe.Beast);
            AssertSpecifiedMinionPortrait("BG32_MagicItem_804", "BG_OG_221", 1, null, Keyword.Battlecry);

            AssertSingleEquipAdds("BG32_MagicItem_953", 1, card => card.CardId == "BGS_066");
            AssertSingleEquipAdds("BG32_MagicItem_953", 1, card => card.CardId == "BG32_236");
            AssertSingleEquipAdds("BG32_MagicItem_824", 2, card => card.CardId == "BG29_140");
            AssertSingleEquipAdds("BG30_MagicItem_868", 1, card => card.CardId == "BG26_174");
            AssertSingleEquipAdds("BG30_MagicItem_868", 1, card => card.CardId == "BGS_004");
            AssertSingleEquipAdds(
                "BG32_MagicItem_282",
                5,
                card => card.Tribes.Contains(Tribe.Mech) && card.Keywords.Contains(Keyword.Magnetic));

            var smugglerService = MatchService.CreateWithDefaultCatalog(12345);
            smugglerService.State.Player.Tavern.Gold = 20;
            EquipTrinket(smugglerService, "BG30_MagicItem_825");
            var smuggler = smugglerService.State.Player.Tavern.Hand.Single(card => card.CardId == "BG21_013");
            Assert.AreEqual(12, smuggler.Attack);
            Assert.AreEqual(12, smuggler.MaxHealth);
            CollectionAssert.Contains(BoardTribeAnalyzer.GetCountedTribes(smuggler), Tribe.Dragon);
        }

        [Test]
        public void MacawPortrait_TriggersLeftmostBattlecryOnAttackNotWhenPlayed()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Board.Add(TestTribeMinion(RazorfenGeomancerCardId, 1, 20, Tribe.Quilboar, Keyword.Battlecry));

            EquipTrinket(service, "BG32_MagicItem_803");
            var macaw = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BGS_078");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.IndexOf(macaw), -1));

            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.CardId == "BLOOD_GEM"));

            macaw = service.State.Player.Board.Single(card => card.CardId == "BGS_078");
            service.State.Player.Board.Remove(macaw);
            service.State.Player.Board.Insert(0, macaw);
            RunAvengeCombat(service, 1, 100, 1);

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
        }

        [Test]
        public void ManipulatorPortrait_FacelessBattlecryBecomesIndependentCopyOfFriendlyMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 20;
            var target = TestTribeMinion("FACELESS_TARGET", 4, 5, Tribe.Dragon, Keyword.DivineShield);
            target.InstanceId = "faceless-target";
            target.Name = "Faceless Target";
            target.Attack = 11;
            target.MaxHealth = 13;
            target.Health = 9;
            target.Golden = true;
            target.Enchantments.Add(new Enchantment
            {
                Id = "faceless-target-buff",
                SourceId = "test",
                AttackBonus = 7,
                HealthBonus = 8
            });
            target.Counters["faceless-target-counter"] = 2;
            target.EffectIds.Add("faceless-target-effect");
            target.Tags.Add("faceless-target-tag");
            target.PoolSource = PoolSource.Pool;
            target.OriginPoolSource = PoolSource.Pool;
            target.PoolCopiesHeld = 1;
            service.State.Player.Board.Add(target);

            QueueTrinketChoice(service, "BG30_MagicItem_876");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var faceless = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG_EX1_564");

            Assert.IsTrue(service.RequiresExplicitBattlecryTarget(faceless));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            var copy = service.State.Player.Board.Single(minion =>
                minion.CardId == target.CardId &&
                minion.InstanceId != target.InstanceId);
            Assert.AreNotSame(target, copy);
            Assert.AreEqual(target.CardId, copy.CardId);
            Assert.AreEqual(target.Name, copy.Name);
            Assert.AreEqual(target.Attack, copy.Attack);
            Assert.AreEqual(target.Health, copy.Health);
            Assert.AreEqual(target.MaxHealth, copy.MaxHealth);
            Assert.AreEqual(target.Golden, copy.Golden);
            CollectionAssert.AreEqual(target.Keywords, copy.Keywords);
            Assert.AreEqual(target.Counters["faceless-target-counter"], copy.Counters["faceless-target-counter"]);
            CollectionAssert.AreEqual(target.EffectIds, copy.EffectIds);
            CollectionAssert.AreEqual(target.Tags, copy.Tags);
            Assert.AreNotSame(target.Enchantments, copy.Enchantments);
            Assert.AreNotSame(target.Counters, copy.Counters);
            Assert.AreEqual(PoolSource.Copy, copy.PoolSource);
            Assert.AreEqual(PoolSource.Copy, copy.OriginPoolSource);
            Assert.AreEqual(0, copy.PoolCopiesHeld);

            target.Attack += 1;
            target.Enchantments[0].AttackBonus += 1;
            Assert.AreEqual(11, copy.Attack);
            Assert.AreEqual(7, copy.Enchantments[0].AttackBonus);
        }

        [Test]
        public void ManipulatorPortrait_FacelessRequiresFriendlyBoardTarget()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 20;
            QueueTrinketChoice(service, "BG30_MagicItem_876");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var exception = Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1)));

            StringAssert.Contains("needs a friendly board target", exception.Message);
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(0, service.State.Player.Board.Count);
        }

        [Test]
        public void Batch6TribePortraits_TavernPhaseHooksApplyModifiedEffects()
        {
            var surveyor = MatchService.CreateWithDefaultCatalog(12345);
            var gemTarget = TestShopMinion("surveyor-gem-target", 2, 3);
            surveyor.State.Player.Board.Add(gemTarget);
            surveyor.State.Player.Tavern.Gold = 20;
            EquipTrinket(surveyor, "BG30_MagicItem_943");
            AddBloodGemSpellToHand(surveyor, "surveyor");
            surveyor.Apply(new GameCommand(GameCommandType.PlayMinion, surveyor.State.Player.Tavern.Hand.Count - 1, 0));
            Assert.AreEqual(9, gemTarget.Attack);
            Assert.AreEqual(10, gemTarget.MaxHealth);

            var felemental = MatchService.CreateWithDefaultCatalog(12345);
            var felementalShop = TestShopMinion("felemental-shop", 1, 1);
            felemental.State.Player.Tavern.Shop = new List<MinionInstance> { felementalShop };
            felemental.State.Player.Tavern.Gold = 20;
            EquipTrinket(felemental, "BG32_MagicItem_830");
            PlayHandCard(felemental, felemental.State.Player.Tavern.Hand.Single(card => card.CardId == "BG25_041"));
            Assert.AreEqual(5, felementalShop.Attack);
            Assert.AreEqual(4, felementalShop.MaxHealth);

            var felblood = MatchService.CreateWithDefaultCatalog(12345);
            var felbloodShop = TestShopMinion("felblood-shop", 1, 1);
            felblood.State.Player.Tavern.Shop = new List<MinionInstance> { felbloodShop };
            felblood.State.Player.Tavern.Gold = 20;
            EquipTrinket(felblood, "BG30_MagicItem_869");
            PlayHandCard(felblood, felblood.State.Player.Tavern.Hand.Single(card => card.CardId == "BG29_873"));
            Assert.AreEqual(3, felbloodShop.Attack);
            Assert.AreEqual(3, felbloodShop.MaxHealth);

            var implicator = MatchService.CreateWithDefaultCatalog(12345);
            implicator.State.Player.Tavern.Gold = 20;
            EquipTrinket(implicator, "BG32_MagicItem_824");
            implicator.State.Player.Tavern.Hand.Clear();
            implicator.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestShopMinion("implicator-low", 2, 2),
                TestShopMinion("implicator-high", 4, 6)
            };
            var demon = TestTribeMinion("implicator-played-demon", 1, 1, Tribe.Demon);
            implicator.State.Player.Tavern.Hand.Add(demon);
            PlayHandCard(implicator, demon);
            Assert.AreEqual(5, demon.Attack);
            Assert.AreEqual(7, demon.MaxHealth);
            Assert.IsFalse(implicator.State.Player.Tavern.Shop.Any(card => card != null && card.CardId == "implicator-high"));

            var shadowy = MatchService.CreateWithDefaultCatalog(12345);
            shadowy.State.Player.Health = 30;
            shadowy.State.Player.Armor = 0;
            shadowy.State.Player.Tavern.Gold = 20;
            EquipTrinket(shadowy, "BG32_MagicItem_887");
            Assert.AreEqual(5, shadowy.State.Player.Armor);
            var shadowyDemon = TestTribeMinion("shadowy-played-demon", 1, 1, Tribe.Demon);
            shadowy.State.Player.Tavern.Hand.Add(shadowyDemon);
            PlayHandCard(shadowy, shadowyDemon);
            Assert.AreEqual(4, shadowy.State.Player.Armor);
            Assert.AreEqual(30, shadowy.State.Player.Health);

            var horde = MatchService.CreateWithDefaultCatalog(12345);
            var existingLowTier = TestShopMinion("horde-existing-low", 1, 1);
            existingLowTier.TavernTier = 3;
            var existingHighTier = TestShopMinion("horde-existing-high", 1, 1);
            existingHighTier.TavernTier = 4;
            horde.State.Player.Board.Add(existingLowTier);
            horde.State.Player.Board.Add(existingHighTier);
            horde.State.Player.Tavern.Gold = 20;
            EquipTrinket(horde, "BG30_MagicItem_843t");
            Assert.AreEqual(8, existingLowTier.Attack);
            Assert.AreEqual(6, existingLowTier.MaxHealth);
            Assert.AreEqual(1, existingHighTier.Attack);
            var playedLowTier = TestShopMinion("horde-played-low", 2, 2);
            playedLowTier.TavernTier = 2;
            horde.State.Player.Tavern.Hand.Add(playedLowTier);
            PlayHandCard(horde, playedLowTier);
            Assert.AreEqual(9, playedLowTier.Attack);
            Assert.AreEqual(7, playedLowTier.MaxHealth);

            var crowbar = MatchService.CreateWithDefaultCatalog(12345);
            var left = TestShopMinion("crowbar-left", 1, 1);
            var right = TestShopMinion("crowbar-right", 1, 1);
            crowbar.State.Player.Board.Add(left);
            crowbar.State.Player.Board.Add(right);
            crowbar.State.Player.Tavern.Gold = 20;
            EquipTrinket(crowbar, "BG35_MagicItem_713");
            crowbar.State.Player.Tavern.Shop = new List<MinionInstance> { TestTribeMinion("crowbar-pirate", 2, 2, Tribe.Pirate) };
            crowbar.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            Assert.AreEqual(13, left.Attack);
            Assert.AreEqual(13, left.MaxHealth);
            Assert.AreEqual(1, right.Attack);

            var rewinder = MatchService.CreateWithDefaultCatalog(12345);
            rewinder.State.Player.Health = 30;
            rewinder.State.Player.Tavern.Gold = 20;
            EquipTrinket(rewinder, "BG30_MagicItem_868");
            rewinder.State.Player.Tavern.Hand.Clear();
            var soulRewinder = TestShopMinion("BG26_174", 3, 1);
            rewinder.State.Player.Board.Add(soulRewinder);
            rewinder.State.Player.Tavern.Shop = new List<MinionInstance> { TestTavernSpell(HastyExcavationCardId, "rewinder", 2) };
            rewinder.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            Assert.AreEqual(30, rewinder.State.Player.Health);
            Assert.AreEqual(4, soulRewinder.Attack);
            Assert.AreEqual(2, soulRewinder.MaxHealth);
        }

        [Test]
        public void WarDrum_RepeatsOneBattlecryEachTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var shopMinion = TestShopMinion("war-drum-shop", 1, 1);
            service.State.Player.Tavern.Shop = new List<MinionInstance> { shopMinion };
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG32_MagicItem_416");

            var first = TestTribeMinion("BG25_041", 3, 3, Tribe.Elemental, Keyword.Battlecry);
            service.State.Player.Tavern.Hand.Add(first);
            PlayHandCard(service, first);

            Assert.AreEqual(7, shopMinion.Attack);
            Assert.AreEqual(4, shopMinion.MaxHealth);

            var second = TestTribeMinion("BG25_041", 3, 3, Tribe.Elemental, Keyword.Battlecry);
            service.State.Player.Tavern.Hand.Add(second);
            PlayHandCard(service, second);

            Assert.AreEqual(9, shopMinion.Attack);
            Assert.AreEqual(5, shopMinion.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            var nextShopMinion = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
            var nextShopAttack = nextShopMinion.Attack;
            var nextShopHealth = nextShopMinion.MaxHealth;
            var third = TestTribeMinion("BG25_041", 3, 3, Tribe.Elemental, Keyword.Battlecry);
            service.State.Player.Tavern.Hand.Add(third);
            PlayHandCard(service, third);

            Assert.AreEqual(nextShopAttack + 6, nextShopMinion.Attack);
            Assert.AreEqual(nextShopHealth + 3, nextShopMinion.MaxHealth);
        }

        [Test]
        public void BronzebeardPortrait_AddsBrannAndBattlecryMinionAndMakesBrannMurlocDragon()
        {
            var service = CreateServiceWithEnabledMinions(12345, "BG34_523");
            var existingBoardBrann = TestShopMinion("BG_LOE_077", 2, 4, Keyword.Battlecry);
            service.State.Player.Board.Add(existingBoardBrann);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_418");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
            var handBrann = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG_LOE_077");
            Assert.AreEqual(CardKind.Minion, handBrann.CardKind);
            Assert.AreEqual(5, handBrann.TavernTier);
            Assert.IsTrue(handBrann.Keywords.Contains(Keyword.Battlecry));
            AssertHasPortraitTribes(handBrann, Tribe.Murloc, Tribe.Dragon);
            AssertHasPortraitTribes(existingBoardBrann, Tribe.Murloc, Tribe.Dragon);

            var randomBattlecry = service.State.Player.Tavern.Hand.Single(card => card.InstanceId != handBrann.InstanceId);
            Assert.IsTrue(randomBattlecry.Keywords.Contains(Keyword.Battlecry));
            Assert.AreEqual(PoolSource.Copy, handBrann.PoolSource);
            Assert.AreEqual(0, handBrann.PoolCopiesHeld);
            Assert.AreEqual(PoolSource.Copy, randomBattlecry.PoolSource);
            Assert.AreEqual(0, randomBattlecry.PoolCopiesHeld);
        }

        [Test]
        public void DrakkariPortrait_AddsDrakkariAndMakesDrakkarisMechElemental()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var existingBoardDrakkari = TestShopMinion("BG26_ICC_901", 1, 5);
            service.State.Player.Board.Add(existingBoardDrakkari);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_179");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            var handDrakkari = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG26_ICC_901");
            Assert.AreEqual(CardKind.Minion, handDrakkari.CardKind);
            Assert.AreEqual(5, handDrakkari.TavernTier);
            AssertHasPortraitTribes(handDrakkari, Tribe.Mech, Tribe.Elemental);
            AssertHasPortraitTribes(existingBoardDrakkari, Tribe.Mech, Tribe.Elemental);
            Assert.AreEqual(PoolSource.Copy, handDrakkari.PoolSource);
            Assert.AreEqual(0, handDrakkari.PoolCopiesHeld);
        }

        [Test]
        public void EnforcerPortrait_AddsLightfangAndMakesLightfangsAllTypes()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var existingBoardLightfang = TestShopMinion("BGS_009", 2, 2);
            service.State.Player.Board.Add(existingBoardLightfang);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_971");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            var handLightfang = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BGS_009");
            Assert.AreEqual(CardKind.Minion, handLightfang.CardKind);
            Assert.AreEqual(5, handLightfang.TavernTier);
            AssertHasAllPortraitTypes(handLightfang);
            AssertHasAllPortraitTypes(existingBoardLightfang);
            Assert.AreEqual(PoolSource.Copy, handLightfang.PoolSource);
            Assert.AreEqual(0, handLightfang.PoolCopiesHeld);
        }

        [Test]
        public void BalladistPortrait_AddsBalladistOnEquipAndTurnStartAndAddsAttackToBattlecry()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var pirate = TestTribeMinion("balladist-pirate", 2, 3, Tribe.Pirate);
            service.State.Player.Board.Add(pirate);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_987");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            var firstBalladist = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG26_814");
            Assert.AreEqual(CardKind.Minion, firstBalladist.CardKind);
            Assert.AreEqual(4, firstBalladist.TavernTier);
            Assert.IsTrue(firstBalladist.Tribes.Contains(Tribe.Pirate));
            Assert.IsTrue(firstBalladist.Keywords.Contains(Keyword.Battlecry));
            Assert.AreEqual(PoolSource.Copy, firstBalladist.PoolSource);
            Assert.AreEqual(0, firstBalladist.PoolCopiesHeld);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BG26_814"));

            service.State.Player.Tavern.GoldSpentThisTurn = 3;
            var balladistIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG26_814");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, balladistIndex, -1));

            Assert.AreEqual(6, pirate.Attack);
            Assert.AreEqual(7, pirate.MaxHealth);
        }

        [Test]
        public void BristlebachPortrait_AddsBristlebachAndAvengeGemsAllFriendlyMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_274");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var handBristlebach = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG26_157");
            Assert.AreEqual(CardKind.Minion, handBristlebach.CardKind);
            Assert.AreEqual(6, handBristlebach.TavernTier);
            Assert.IsTrue(handBristlebach.Tribes.Contains(Tribe.Quilboar));
            Assert.IsTrue(handBristlebach.Keywords.Contains(Keyword.Avenge));
            Assert.AreEqual(PoolSource.Copy, handBristlebach.PoolSource);
            Assert.AreEqual(0, handBristlebach.PoolCopiesHeld);

            service.State.Player.Tavern.Hand.Remove(handBristlebach);
            handBristlebach.InstanceId = "bristlebach-portrait-source";
            var firstVictim = TestShopMinion("bristlebach-victim-one", 1, 1);
            var secondVictim = TestShopMinion("bristlebach-victim-two", 1, 1);
            var beast = TestTribeMinion("bristlebach-friendly-beast", 5, 20, Tribe.Beast);
            var quilboar = TestTribeMinion("bristlebach-friendly-quilboar", 4, 20, Tribe.Quilboar);
            service.State.Player.Board.Add(firstVictim);
            service.State.Player.Board.Add(secondVictim);
            service.State.Player.Board.Add(handBristlebach);
            service.State.Player.Board.Add(beast);
            service.State.Player.Board.Add(quilboar);

            RunAvengeCombat(service, 1, 100, 6);

            Assert.AreEqual(5, FinalCombatMinion(service, handBristlebach).Attack);
            Assert.AreEqual(12, FinalCombatMinion(service, handBristlebach).MaxHealth);
            Assert.AreEqual(7, FinalCombatMinion(service, beast).Attack);
            Assert.AreEqual(22, FinalCombatMinion(service, beast).MaxHealth);
            Assert.AreEqual(6, FinalCombatMinion(service, quilboar).Attack);
            Assert.AreEqual(22, FinalCombatMinion(service, quilboar).MaxHealth);
        }

        [Test]
        public void CzarinaPortrait_AddsChargingCzarinaAndCzarinasGiveHealthToDivineShieldMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_283");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var czarina = service.State.Player.Tavern.Hand.Single(card => card.CardId == ChargingCzarinaCardId);
            Assert.AreEqual(CardKind.Minion, czarina.CardKind);
            Assert.AreEqual(5, czarina.TavernTier);
            Assert.IsTrue(czarina.Tribes.Contains(Tribe.Mech));
            Assert.IsTrue(czarina.Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(czarina.Keywords.Contains(Keyword.TavernSpell));
            Assert.AreEqual(PoolSource.Copy, czarina.PoolSource);
            Assert.AreEqual(0, czarina.PoolCopiesHeld);

            service.State.Player.Tavern.Hand.Remove(czarina);
            czarina.InstanceId = "czarina-portrait-source";
            var shielded = TestShopMinion("czarina-shielded", 2, 3, Keyword.DivineShield);
            var unshielded = TestShopMinion("czarina-unshielded", 5, 6);
            service.State.Player.Board.Add(czarina);
            service.State.Player.Board.Add(shielded);
            service.State.Player.Board.Add(unshielded);

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(8, czarina.Attack);
            Assert.AreEqual(5, czarina.MaxHealth);
            Assert.AreEqual(6, shielded.Attack);
            Assert.AreEqual(7, shielded.MaxHealth);
            Assert.AreEqual(5, unshielded.Attack);
            Assert.AreEqual(6, unshielded.MaxHealth);
        }

        [Test]
        public void ConductorPortrait_AddsSnarlingConductorAndDiscardPlaysBloodGemOnAllFriendlyMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var beast = TestTribeMinion("conductor-beast", 2, 10, Tribe.Beast);
            var quilboar = TestTribeMinion("conductor-quilboar", 3, 12, Tribe.Quilboar);
            service.State.Player.Board.Add(beast);
            service.State.Player.Board.Add(quilboar);
            service.State.Player.Tavern.BloodGemBonusAttack = 2;
            service.State.Player.Tavern.BloodGemBonusHealth = 1;
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_402");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var conductor = service.State.Player.Tavern.Hand.Single(card => card.CardId == SnarlingConductorCardId);
            Assert.AreEqual(CardKind.Minion, conductor.CardKind);
            Assert.AreEqual("Snarling Conductor", conductor.Name);
            Assert.AreEqual(4, conductor.Attack);
            Assert.AreEqual(5, conductor.MaxHealth);
            Assert.AreEqual(4, conductor.TavernTier);
            Assert.IsTrue(conductor.Tribes.Contains(Tribe.Quilboar));
            Assert.AreEqual(PoolSource.Copy, conductor.PoolSource);
            Assert.AreEqual(0, conductor.PoolCopiesHeld);

            service.State.Player.Tavern.Hand.Add(TestTavernSpell("CONDUCTOR_DISCARD_TEST", "discard", 0));
            var discardIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "CONDUCTOR_DISCARD_TEST");
            service.Apply(new GameCommand(GameCommandType.DiscardCardFromHand, discardIndex));

            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.CardId == "CONDUCTOR_DISCARD_TEST"));
            Assert.AreEqual(5, beast.Attack);
            Assert.AreEqual(12, beast.MaxHealth);
            Assert.AreEqual(6, quilboar.Attack);
            Assert.AreEqual(14, quilboar.MaxHealth);
            Assert.IsTrue(beast.Enchantments.Any(enchantment => enchantment.SourceId == "Conductor Portrait"));
            Assert.IsTrue(quilboar.Enchantments.Any(enchantment => enchantment.SourceId == "Conductor Portrait"));
        }

        [Test]
        public void CopperCoilGreater_MagnetizeBuffsTargetAndImprovesNextTrigger()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var mech = TestTribeMinion("copper-target-mech", 10, 20, Tribe.Mech);
            service.State.Player.Board.Add(mech);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_300t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.State.Player.Tavern.Hand.Add(TestTribeMinion("copper-magnetic-one", 2, 4, Tribe.Mech, Keyword.Magnetic));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(15, mech.Attack);
            Assert.AreEqual(26, mech.MaxHealth);
            Assert.AreEqual(6, service.State.Player.Tavern.AdvancedMechanics.Counters[CopperCoilGreaterAttackCounter]);
            Assert.AreEqual(4, service.State.Player.Tavern.AdvancedMechanics.Counters[CopperCoilGreaterHealthCounter]);

            service.State.Player.Tavern.Hand.Add(TestTribeMinion("copper-magnetic-two", 1, 1, Tribe.Mech, Keyword.Magnetic));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(22, mech.Attack);
            Assert.AreEqual(31, mech.MaxHealth);
            Assert.AreEqual(9, service.State.Player.Tavern.AdvancedMechanics.Counters[CopperCoilGreaterAttackCounter]);
            Assert.AreEqual(6, service.State.Player.Tavern.AdvancedMechanics.Counters[CopperCoilGreaterHealthCounter]);
            Assert.AreEqual(2, mech.Enchantments.Count(enchantment => enchantment.SourceId == "Copper Coil"));
        }

        [Test]
        public void CopperCoilLesser_MagnetizeBuffsTargetAndImprovesNextTrigger()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var mech = TestTribeMinion("copper-lesser-target-mech", 10, 20, Tribe.Mech);
            service.State.Player.Board.Add(mech);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_300");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.State.Player.Tavern.Hand.Add(TestTribeMinion("copper-lesser-magnetic-one", 2, 4, Tribe.Mech, Keyword.Magnetic));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(13, mech.Attack);
            Assert.AreEqual(25, mech.MaxHealth);
            Assert.AreEqual(2, service.State.Player.Tavern.AdvancedMechanics.Counters[CopperCoilLesserAttackCounter]);
            Assert.AreEqual(2, service.State.Player.Tavern.AdvancedMechanics.Counters[CopperCoilLesserHealthCounter]);

            service.State.Player.Tavern.Hand.Add(TestTribeMinion("copper-lesser-magnetic-two", 1, 1, Tribe.Mech, Keyword.Magnetic));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(16, mech.Attack);
            Assert.AreEqual(28, mech.MaxHealth);
            Assert.AreEqual(3, service.State.Player.Tavern.AdvancedMechanics.Counters[CopperCoilLesserAttackCounter]);
            Assert.AreEqual(3, service.State.Player.Tavern.AdvancedMechanics.Counters[CopperCoilLesserHealthCounter]);
            Assert.AreEqual(2, mech.Enchantments.Count(enchantment => enchantment.SourceId == "Copper Coil"));
        }

        [Test]
        public void DesignerEyepatch_TwoPirateCopiesMakeGolden()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Hand.Add(TestTripleMinion("eyepatch-pirate", "hand", Tribe.Pirate, 3, 4));
            service.State.Player.Tavern.Shop.Add(TestTripleMinion("eyepatch-pirate", "shop", Tribe.Pirate, 3, 4));
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_439");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            var golden = service.State.Player.Tavern.Hand.Single(card => card.DefinitionId == "eyepatch-pirate");
            Assert.IsTrue(golden.Golden);
            Assert.AreEqual(6, golden.Attack);
            Assert.AreEqual(8, golden.MaxHealth);
        }

        [Test]
        public void DesignerEyepatch_DoesNotReduceNonPirateTripleRequirement()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Hand.Add(TestTripleMinion("eyepatch-beast", "hand", Tribe.Beast, 3, 4));
            service.State.Player.Tavern.Shop.Add(TestTripleMinion("eyepatch-beast", "shop", Tribe.Beast, 3, 4));
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_439");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            var copies = service.State.Player.Tavern.Hand
                .Where(card => card.DefinitionId == "eyepatch-beast")
                .ToList();
            Assert.AreEqual(2, copies.Count);
            Assert.IsTrue(copies.All(card => !card.Golden));
        }

        [Test]
        public void DesignerEyepatch_TwoAllTribeCopiesCountAsPirates()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Hand.Add(TestTripleMinion("eyepatch-all", "hand", Tribe.All, 2, 5));
            service.State.Player.Tavern.Shop.Add(TestTripleMinion("eyepatch-all", "shop", Tribe.All, 2, 5));
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_439");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            var golden = service.State.Player.Tavern.Hand.Single(card => card.DefinitionId == "eyepatch-all");
            Assert.IsTrue(golden.Golden);
            Assert.AreEqual(4, golden.Attack);
            Assert.AreEqual(10, golden.MaxHealth);
        }

        [Test]
        public void DragonwingGliderLesser_PlayingMinionBuffsFriendlyDragon()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var dragon = TestTribeMinion("dragonwing-lesser-dragon", 4, 7, Tribe.Dragon);
            var beast = TestTribeMinion("dragonwing-lesser-beast", 5, 8, Tribe.Beast);
            service.State.Player.Board.Add(dragon);
            service.State.Player.Board.Add(beast);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_900");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Hand.Add(TestTribeMinion("dragonwing-lesser-played-beast", 1, 1, Tribe.Beast));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(8, dragon.Attack);
            Assert.AreEqual(11, dragon.MaxHealth);
            Assert.AreEqual(11, dragon.Health);
            Assert.AreEqual(5, beast.Attack);
            Assert.AreEqual(8, beast.MaxHealth);
            Assert.IsTrue(dragon.Enchantments.Any(enchantment => enchantment.SourceId == "Dragonwing Glider"));
        }

        [Test]
        public void DragonwingGliderGreater_PlayingMinionBuffsFriendlyDragon()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var dragon = TestTribeMinion("dragonwing-minion-dragon", 4, 7, Tribe.Dragon);
            var beast = TestTribeMinion("dragonwing-minion-beast", 5, 8, Tribe.Beast);
            service.State.Player.Board.Add(dragon);
            service.State.Player.Board.Add(beast);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_900t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Hand.Add(TestTribeMinion("dragonwing-played-beast", 1, 1, Tribe.Beast));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(10, dragon.Attack);
            Assert.AreEqual(11, dragon.MaxHealth);
            Assert.AreEqual(11, dragon.Health);
            Assert.AreEqual(5, beast.Attack);
            Assert.AreEqual(8, beast.MaxHealth);
            Assert.IsTrue(dragon.Enchantments.Any(enchantment => enchantment.SourceId == "Dragonwing Glider"));
        }

        [Test]
        public void DragonwingGliderGreater_PlayingTavernSpellFromHandBuffsDragonButDebugCastDoesNot()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var dragon = TestTribeMinion("dragonwing-spell-dragon", 2, 5, Tribe.Dragon);
            service.State.Player.Board.Add(dragon);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_900t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));
            Assert.AreEqual(2, dragon.Attack);
            Assert.AreEqual(5, dragon.MaxHealth);

            service.State.Player.Tavern.Hand.Add(TestTavernSpell(DebugNoBoardSpellCardId, 0, "Gain 2 Gold."));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(8, dragon.Attack);
            Assert.AreEqual(9, dragon.MaxHealth);
            Assert.AreEqual(9, dragon.Health);
        }

        [Test]
        public void DragonwingGliderGreater_MagneticHandPlayCountsAsCardPlayed()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var mech = TestTribeMinion("dragonwing-magnetic-mech", 2, 2, Tribe.Mech);
            var dragon = TestTribeMinion("dragonwing-magnetic-dragon", 5, 6, Tribe.Dragon);
            service.State.Player.Board.Add(mech);
            service.State.Player.Board.Add(dragon);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_900t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Hand.Add(TestTribeMinion("dragonwing-magnetic-card", 1, 1, Tribe.Mech, Keyword.Magnetic));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(3, mech.Attack);
            Assert.AreEqual(3, mech.MaxHealth);
            Assert.AreEqual(11, dragon.Attack);
            Assert.AreEqual(10, dragon.MaxHealth);
            Assert.AreEqual(10, dragon.Health);
        }

        [Test]
        public void DragonwingGliderGreater_UsesCountedTribesAndSkipsWhenNoFriendlyDragon()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var allTribe = TestTribeMinion("dragonwing-all-tribe", 1, 1, Tribe.All);
            service.State.Player.Board.Add(allTribe);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_900t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Hand.Add(TestTribeMinion("dragonwing-all-played", 2, 2, Tribe.Beast));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(7, allTribe.Attack);
            Assert.AreEqual(5, allTribe.MaxHealth);
            Assert.AreEqual(5, allTribe.Health);

            var noDragonService = MatchService.CreateWithDefaultCatalog(12345);
            var beast = TestTribeMinion("dragonwing-no-dragon-beast", 3, 4, Tribe.Beast);
            noDragonService.State.Player.Board.Add(beast);
            noDragonService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(noDragonService, "BG30_MagicItem_900t");
            noDragonService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            noDragonService.State.Player.Tavern.Hand.Add(TestTribeMinion("dragonwing-no-dragon-played", 1, 1, Tribe.Pirate));
            noDragonService.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(3, beast.Attack);
            Assert.AreEqual(4, beast.MaxHealth);
            Assert.AreEqual(4, beast.Health);
        }

        [Test]
        public void BallerPortrait_AddsTemperatureShiftAndAfterNineElementalsAddsAnother()
        {
            var service = CreateServiceWithEnabledTavernSpells(12345, TemperatureShiftCardId);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_861");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            AssertTavernSpellHand(service, 1, TemperatureShiftCardId, 4);

            for (var index = 0; index < 9; index += 1)
            {
                service.State.Player.Tavern.Hand.Add(TestTribeMinion("baller-elemental-" + index, 1, 1, Tribe.Elemental));
                service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, -1));
                service.State.Player.Board.Clear();
            }

            AssertTavernSpellHand(service, 2, TemperatureShiftCardId, 4);
        }

        [Test]
        public void BattleHorn_DiscoversBattlecryMinionAndAvengeTriggersFriendlyBattlecry()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_415");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var discover = service.State.Player.Tavern.Discover;
            Assert.IsNotNull(discover);
            Assert.AreEqual(1, discover.RemainingPicks);
            Assert.IsTrue(discover.Options.All(option => option.CardKind == CardKind.Minion));
            Assert.IsTrue(discover.Options.All(option => option.Keywords.Contains(Keyword.Battlecry)));
            service.State.Player.Tavern.Discover = null;
            service.State.Player.Tavern.Hand.Clear();

            service.State.Player.Board.Add(TestShopMinion("battle-horn-victim-one", 1, 1));
            service.State.Player.Board.Add(TestShopMinion("battle-horn-victim-two", 1, 1));
            service.State.Player.Board.Add(TestShopMinion(RazorfenGeomancerCardId, 1, 20, Keyword.Battlecry));

            RunAvengeCombat(service, 1, 100, 6);

            Assert.AreEqual(
                1,
                service.State.LastResult.PlayerRewards.Count(reward => reward.Type == CombatRewardType.TriggerFriendlyBattlecry));
            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
        }

        [Test]
        public void BloodboundEarrings_AfterFiveSpellsPlaysTwoGemsOnAllMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var first = TestShopMinion("earrings-first", 2, 2);
            var second = TestShopMinion("earrings-second", 3, 3);
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_808t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            for (var index = 0; index < 5; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));
            }

            Assert.AreEqual(4, first.Attack);
            Assert.AreEqual(4, first.MaxHealth);
            Assert.AreEqual(5, second.Attack);
            Assert.AreEqual(5, second.MaxHealth);
        }

        [Test]
        public void BloodboundEarrings_LesserAndGreaterTrackIndependentSpellThresholds()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var first = TestShopMinion("earrings-stacked-first", 2, 2);
            var second = TestShopMinion("earrings-stacked-second", 3, 3);
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_808");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            QueueTrinketChoice(service, "BG32_MagicItem_808t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            for (var index = 0; index < 4; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));
            }

            Assert.AreEqual(3, first.Attack);
            Assert.AreEqual(3, first.MaxHealth);
            Assert.AreEqual(4, second.Attack);
            Assert.AreEqual(4, second.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(5, first.Attack);
            Assert.AreEqual(5, first.MaxHealth);
            Assert.AreEqual(6, second.Attack);
            Assert.AreEqual(6, second.MaxHealth);
        }

        [Test]
        public void BloodboundRing_HandBloodGemBuffsDivineShieldMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var divineShield = TestShopMinion("ring-divine-shield", 2, 2, Keyword.DivineShield);
            var target = TestShopMinion("ring-target", 3, 3);
            var untouched = TestShopMinion("ring-untouched", 4, 4);
            service.State.Player.Board.Add(divineShield);
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(untouched);
            service.State.Player.Tavern.BloodGemBonusAttack = 1;
            service.State.Player.Tavern.BloodGemBonusHealth = 2;
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_435");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            AddBloodGemSpellToHand(service, "ring");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 1));

            Assert.AreEqual(4, divineShield.Attack);
            Assert.AreEqual(5, divineShield.MaxHealth);
            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(6, target.MaxHealth);
            Assert.AreEqual(4, untouched.Attack);
            Assert.AreEqual(4, untouched.MaxHealth);
        }

        [Test]
        public void BootyBayBrewLesser_SpendingGoldBuffsOneFriendlyPirate()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var firstPirate = TestTribeMinion("booty-lesser-pirate-one", 2, 3, Tribe.Pirate);
            var secondPirate = TestTribeMinion("booty-lesser-pirate-two", 5, 7, Tribe.Pirate);
            var beast = TestTribeMinion("booty-lesser-beast", 4, 6, Tribe.Beast);
            service.State.Player.Board.Add(firstPirate);
            service.State.Player.Board.Add(secondPirate);
            service.State.Player.Board.Add(beast);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_924");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(2, firstPirate.Attack);
            Assert.AreEqual(3, firstPirate.MaxHealth);
            Assert.AreEqual(5, secondPirate.Attack);
            Assert.AreEqual(7, secondPirate.MaxHealth);

            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestShopMinion("booty-lesser-shop-buy", 1, 1)
            };
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            var buffedCount = 0;
            if (firstPirate.Attack == 6 && firstPirate.MaxHealth == 6)
            {
                buffedCount += 1;
            }

            if (secondPirate.Attack == 9 && secondPirate.MaxHealth == 10)
            {
                buffedCount += 1;
            }

            Assert.AreEqual(1, buffedCount);
            Assert.AreEqual(11, firstPirate.Attack + secondPirate.Attack);
            Assert.AreEqual(13, firstPirate.MaxHealth + secondPirate.MaxHealth);
            Assert.AreEqual(4, beast.Attack);
            Assert.AreEqual(6, beast.MaxHealth);
            Assert.AreEqual(16, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void BootyBayBrewGreater_FreeRefreshDoesNotTriggerButUpgradeDoes()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var firstPirate = TestTribeMinion("booty-greater-pirate-one", 2, 3, Tribe.Pirate);
            var secondPirate = TestTribeMinion("booty-greater-pirate-two", 5, 7, Tribe.Pirate);
            var elemental = TestTribeMinion("booty-greater-elemental", 4, 6, Tribe.Elemental);
            service.State.Player.Board.Add(firstPirate);
            service.State.Player.Board.Add(secondPirate);
            service.State.Player.Board.Add(elemental);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_924t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.State.Player.Tavern.FreeRefreshes = 1;
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.AreEqual(2, firstPirate.Attack);
            Assert.AreEqual(3, firstPirate.MaxHealth);
            Assert.AreEqual(5, secondPirate.Attack);
            Assert.AreEqual(7, secondPirate.MaxHealth);
            Assert.AreEqual(20, service.State.Player.Tavern.Gold);

            var upgradeCost = service.State.Player.Tavern.UpgradeCost;
            service.Apply(new GameCommand(GameCommandType.UpgradeTavern));

            var buffedCount = 0;
            if (firstPirate.Attack == 7 && firstPirate.MaxHealth == 9)
            {
                buffedCount += 1;
            }

            if (secondPirate.Attack == 10 && secondPirate.MaxHealth == 13)
            {
                buffedCount += 1;
            }

            Assert.AreEqual(1, buffedCount);
            Assert.AreEqual(12, firstPirate.Attack + secondPirate.Attack);
            Assert.AreEqual(16, firstPirate.MaxHealth + secondPirate.MaxHealth);
            Assert.AreEqual(4, elemental.Attack);
            Assert.AreEqual(6, elemental.MaxHealth);
            Assert.AreEqual(20 - upgradeCost, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void ButchersSickle_AddsButcheringOnEquipAndTurnStart()
        {
            var service = CreateServiceWithEnabledTavernSpells(12345, ButcheringCardId);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_406");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.TavernSpell));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == ButcheringCardId));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.TavernTier == 5));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.TavernSpell));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == ButcheringCardId));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.TavernTier == 5));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));
        }

        [Test]
        public void DevourerEmpowermentAndWisdomballTrinkets_AddSpecifiedSpellsOnEquipAndTurnStart()
        {
            AssertTurnStartTavernSpellTrinket("BG30_MagicItem_543", ChannelTheDevourerCardId, 5);
            AssertTurnStartTavernSpellTrinket("BG32_MagicItem_944", AzeriteEmpowermentCardId, 6);
            AssertTurnStartTavernSpellTrinket("BG31_MagicItem_903", KnockoffWisdomballCardId, 6);
        }

        [Test]
        public void LavaLamp_AddsRandomElementalAfterFiveSoldMinions()
        {
            var service = CreateServiceWithSingleRewardTribeMinion(12345, Tribe.Elemental, out var elemental);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_951");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var soldIds = AddSellableBoardMinions(service, "lava-lamp", 5);
            for (var index = 0; index < 4; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.SellMinion, soldIds[index]));
            }

            Assert.IsEmpty(service.State.Player.Tavern.Hand);
            Assert.AreEqual(4, service.State.Player.Tavern.AdvancedMechanics.Trinkets.LavaLampSoldMinions);

            service.Apply(new GameCommand(GameCommandType.SellMinion, soldIds[4]));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(0, service.State.Player.Tavern.AdvancedMechanics.Trinkets.LavaLampSoldMinions);
            var reward = service.State.Player.Tavern.Hand.Single();
            Assert.AreEqual(elemental.CardId, reward.CardId);
            CollectionAssert.Contains(reward.Tribes, Tribe.Elemental);
            Assert.AreEqual(PoolSource.Copy, reward.PoolSource);
            Assert.AreEqual(0, reward.PoolCopiesHeld);
        }

        [Test]
        public void FungalmancerSticker_AddsRandomMurlocAfterFiveSoldMinions()
        {
            var service = CreateServiceWithSingleRewardTribeMinion(12345, Tribe.Murloc, out var murloc);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_710");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var soldIds = AddSellableBoardMinions(service, "fungalmancer-sticker", 5);
            foreach (var soldId in soldIds)
            {
                service.Apply(new GameCommand(GameCommandType.SellMinion, soldId));
            }

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(0, service.State.Player.Tavern.AdvancedMechanics.Trinkets.FungalmancerStickerSoldMinions);
            var reward = service.State.Player.Tavern.Hand.Single();
            Assert.AreEqual(murloc.CardId, reward.CardId);
            CollectionAssert.Contains(reward.Tribes, Tribe.Murloc);
            Assert.AreEqual(PoolSource.Copy, reward.PoolSource);
            Assert.AreEqual(0, reward.PoolCopiesHeld);
        }

        [Test]
        public void AvalancheSticker_AddsMountingAvalancheOnEquipAndAfterFourSoldMinions()
        {
            var service = CreateServiceWithEnabledTavernSpells(12345, MountingAvalancheCardId);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_863");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.TavernSpell));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == MountingAvalancheCardId));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.TavernTier == 3));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));

            var soldIds = AddSellableBoardMinions(service, "avalanche-sticker", 4);
            for (var index = 0; index < 3; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.SellMinion, soldIds[index]));
            }

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(3, service.State.Player.Tavern.AdvancedMechanics.Trinkets.AvalancheStickerSoldMinions);

            service.Apply(new GameCommand(GameCommandType.SellMinion, soldIds[3]));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(0, service.State.Player.Tavern.AdvancedMechanics.Trinkets.AvalancheStickerSoldMinions);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.TavernSpell));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == MountingAvalancheCardId));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));
        }

        [Test]
        public void DarnassusPie_BuffsShopForEachSoldMinionAndClearsFrozenShopNextTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            var firstShopMinion = TestShopMinion("darnassus-shop-one", 2, 3);
            var secondShopMinion = TestShopMinion("darnassus-shop-two", 4, 5);
            var shopSpell = TestTavernSpell("darnassus-spell", 1, "+1/+1", "buff_spell");
            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                firstShopMinion,
                shopSpell,
                secondShopMinion
            };

            QueueTrinketChoice(service, "BG30_MagicItem_992");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            AssertDarnassusAura(firstShopMinion, 0);
            AssertDarnassusAura(secondShopMinion, 0);

            var soldIds = AddSellableBoardMinions(service, "darnassus-pie", 2);
            service.Apply(new GameCommand(GameCommandType.SellMinion, soldIds[0]));

            Assert.AreEqual(1, service.State.Player.Tavern.AdvancedMechanics.Trinkets.DarnassusPieSoldMinionsThisTurn);
            AssertDarnassusAura(firstShopMinion, 1);
            AssertDarnassusAura(secondShopMinion, 1);
            Assert.AreEqual(0, shopSpell.Attack);
            Assert.AreEqual(0, shopSpell.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.SellMinion, soldIds[1]));

            Assert.AreEqual(2, service.State.Player.Tavern.AdvancedMechanics.Trinkets.DarnassusPieSoldMinionsThisTurn);
            AssertDarnassusAura(firstShopMinion, 2);
            AssertDarnassusAura(secondShopMinion, 2);

            service.Apply(new GameCommand(GameCommandType.FreezeShop, true));
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            firstShopMinion = service.State.Player.Tavern.Shop.Single(card => card.InstanceId == firstShopMinion.InstanceId);
            secondShopMinion = service.State.Player.Tavern.Shop.Single(card => card.InstanceId == secondShopMinion.InstanceId);

            Assert.AreEqual(0, service.State.Player.Tavern.AdvancedMechanics.Trinkets.DarnassusPieSoldMinionsThisTurn);
            AssertDarnassusAura(firstShopMinion, 0);
            AssertDarnassusAura(secondShopMinion, 0);
        }

        [Test]
        public void DarnassusPieDouble_RecalculatesCurrentBonusAfterShopRefresh()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Tavern.FreeRefreshes = 1;
            var originalShopMinion = TestShopMinion("darnassus-double-shop", 3, 4);
            service.State.Player.Tavern.Shop = new List<MinionInstance> { originalShopMinion };

            QueueTrinketChoice(service, "BG30_MagicItem_992t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var soldIds = AddSellableBoardMinions(service, "darnassus-pie-double", 2);
            service.Apply(new GameCommand(GameCommandType.SellMinion, soldIds[0]));
            service.Apply(new GameCommand(GameCommandType.SellMinion, soldIds[1]));

            Assert.AreEqual(2, service.State.Player.Tavern.AdvancedMechanics.Trinkets.DarnassusPieSoldMinionsThisTurn);
            AssertDarnassusAura(originalShopMinion, 4);

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var refreshedMinions = service.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            Assert.IsNotEmpty(refreshedMinions);
            foreach (var refreshed in refreshedMinions)
            {
                AssertDarnassusAura(refreshed, 4);
            }
        }

        [Test]
        public void DefilerPortraitGreater_BuffsFoddersInCurrentAndRefreshedTavern()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Tavern.FreeRefreshes = 1;
            var cardIdFodder = CreateTestDemonFodder("defiler-current-cardid", 1, 1);
            var taggedFodder = TestTribeMinion("defiler-current-tagged", 2, 2, Tribe.Demon);
            taggedFodder.Tags.Add("demon_fodder");
            var plainDemon = TestTribeMinion("defiler-current-plain", 3, 4, Tribe.Demon);
            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                cardIdFodder,
                taggedFodder,
                plainDemon
            };

            QueueTrinketChoice(service, "BG35_MagicItem_151t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == WoodlandDefilerCardId));
            AssertDefilerAura(cardIdFodder, 10);
            AssertDefilerAura(taggedFodder, 10);
            AssertDefilerAura(plainDemon, 0);

            service.State.Player.Tavern.DemonFodderRefreshes = 1;
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var refreshedFodder = service.State.Player.Tavern.Shop
                .Single(card => card != null && card.CardId == DemonFodderCardId);
            AssertDefilerAura(refreshedFodder, 10);
            foreach (var refreshed in service.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion && card.CardId != DemonFodderCardId))
            {
                AssertDefilerAura(refreshed, 0);
            }
        }

        [Test]
        public void DefilerPortraitLesser_BuffsTaggedFodderWithoutStackingAcrossFrozenShop()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            var taggedFodder = TestTribeMinion("defiler-lesser-tagged", 2, 3, Tribe.Demon);
            taggedFodder.Tags.Add("demon_fodder");
            service.State.Player.Tavern.Shop = new List<MinionInstance> { taggedFodder };

            QueueTrinketChoice(service, "BG35_MagicItem_151");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == WoodlandDefilerCardId));
            AssertDefilerAura(taggedFodder, 2);

            service.Apply(new GameCommand(GameCommandType.FreezeShop, true));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            AssertDefilerAura(taggedFodder, 2);
        }

        [Test]
        public void DeathtouchApple_ReappliesRebornToFriendlyUndeadThreeTimesPerCombat()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            var undead = TestTribeMinion("deathtouch-apple-undead", 1, 1, Tribe.Undead, Keyword.Reborn);
            service.State.Player.Board.Add(undead);

            QueueTrinketChoice(service, "BG35_MagicItem_731");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 1, 100, 10);

            var result = service.State.LastResult;
            Assert.IsFalse(result.SafetyStopped);
            Assert.AreEqual(4, result.Log.Count(entry => entry.Title == "RebornResolved"));
            Assert.AreEqual(3, result.Log.Count(entry => entry.Title == "TrinketRebornTriggered"));
            Assert.AreEqual(
                3,
                result.Replay.Frames.Count(frame =>
                    frame.EventType == CombatEventType.TrinketTriggered &&
                    frame.ActorId == "BG35_MagicItem_731"));
            Assert.IsTrue(result.Replay.Frames.Any(frame =>
                frame.EventType == CombatEventType.TrinketTriggered &&
                frame.ActorId == "BG35_MagicItem_731" &&
                frame.MechanicCounter == 3 &&
                frame.MechanicThreshold == 3));
            Assert.IsEmpty(result.FinalPlayerBoard);
        }

        [Test]
        public void DeathtouchApple_UsesCountedTribesAndSkipsNonUndead()
        {
            var allTribe = TestTribeMinion("deathtouch-apple-all", 1, 1, Tribe.All, Keyword.Reborn);
            var allOpponent = TestShopMinion("deathtouch-apple-all-opponent", 1, 100);
            allOpponent.Owner = BoardSide.Opponent;
            var allTavern = new TavernState { TrinketDeathtouchAppleUses = 3 };

            var allResult = CombatEngine.SimulateBasicCombat(
                new[] { allTribe },
                new[] { allOpponent },
                123,
                10,
                allTavern);

            Assert.IsFalse(allResult.SafetyStopped);
            Assert.AreEqual(3, allResult.Log.Count(entry => entry.Title == "TrinketRebornTriggered"));
            Assert.AreEqual(0, allTavern.TrinketDeathtouchAppleUses);

            var beast = TestTribeMinion("deathtouch-apple-beast", 1, 1, Tribe.Beast, Keyword.Reborn);
            var beastOpponent = TestShopMinion("deathtouch-apple-beast-opponent", 1, 100);
            beastOpponent.Owner = BoardSide.Opponent;
            var beastTavern = new TavernState { TrinketDeathtouchAppleUses = 3 };

            var beastResult = CombatEngine.SimulateBasicCombat(
                new[] { beast },
                new[] { beastOpponent },
                123,
                10,
                beastTavern);

            Assert.IsFalse(beastResult.SafetyStopped);
            Assert.AreEqual(1, beastResult.Log.Count(entry => entry.Title == "RebornResolved"));
            Assert.AreEqual(0, beastResult.Log.Count(entry => entry.Title == "TrinketRebornTriggered"));
            Assert.AreEqual(3, beastTavern.TrinketDeathtouchAppleUses);
        }

        [Test]
        public void GemDonation_FirstSoldMinionEachTurnPlaysBloodGemsOnHighestTierTavernMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_809");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.State.Player.Tavern.BloodGemBonusAttack = 2;
            service.State.Player.Tavern.BloodGemBonusHealth = 3;
            var low = TestShopMinion("gem-donation-low", 1, 1);
            low.TavernTier = 1;
            var mid = TestShopMinion("gem-donation-mid", 4, 4);
            mid.TavernTier = 4;
            var highLeft = TestShopMinion("gem-donation-high-left", 6, 6);
            highLeft.TavernTier = 6;
            var highRight = TestShopMinion("gem-donation-high-right", 5, 5);
            highRight.TavernTier = 6;
            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                low,
                highLeft,
                TestTavernSpell("gem-donation-spell", 1, "+1/+1", "buff_spell"),
                mid,
                highRight
            };

            var soldIds = AddSellableBoardMinions(service, "gem-donation", 3);
            service.Apply(new GameCommand(GameCommandType.SellMinion, soldIds[0]));

            Assert.AreEqual(9, highLeft.Attack);
            Assert.AreEqual(10, highLeft.MaxHealth);
            Assert.AreEqual(8, highRight.Attack);
            Assert.AreEqual(9, highRight.MaxHealth);
            Assert.AreEqual(7, mid.Attack);
            Assert.AreEqual(8, mid.MaxHealth);
            Assert.AreEqual(1, low.Attack);
            Assert.AreEqual(1, low.MaxHealth);
            Assert.AreEqual(service.State.Round, service.State.Player.Tavern.AdvancedMechanics.Trinkets.GemDonationSoldRound);
            Assert.IsTrue(highLeft.Enchantments.Any(enchantment => enchantment.SourceId == "Gem Donation Blood Gem"));

            service.Apply(new GameCommand(GameCommandType.SellMinion, soldIds[1]));

            Assert.AreEqual(9, highLeft.Attack);
            Assert.AreEqual(8, highRight.Attack);
            Assert.AreEqual(7, mid.Attack);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            var nextLow = TestShopMinion("gem-donation-next-low", 2, 2);
            nextLow.TavernTier = 2;
            var nextMid = TestShopMinion("gem-donation-next-mid", 3, 3);
            nextMid.TavernTier = 3;
            var nextHigh = TestShopMinion("gem-donation-next-high", 5, 5);
            nextHigh.TavernTier = 5;
            service.State.Player.Tavern.Shop = new List<MinionInstance> { nextLow, nextHigh, nextMid };

            service.Apply(new GameCommand(GameCommandType.SellMinion, soldIds[2]));

            Assert.AreEqual(5, nextLow.Attack);
            Assert.AreEqual(6, nextLow.MaxHealth);
            Assert.AreEqual(6, nextMid.Attack);
            Assert.AreEqual(7, nextMid.MaxHealth);
            Assert.AreEqual(8, nextHigh.Attack);
            Assert.AreEqual(9, nextHigh.MaxHealth);
            Assert.AreEqual(service.State.Round, service.State.Player.Tavern.AdvancedMechanics.Trinkets.GemDonationSoldRound);
        }

        [Test]
        public void PeacebloomCandle_FirstThreeBoughtTavernSpellsAreFree()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_986");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestTavernSpell("free-spell-1", 5, "+1/+1", "buff_spell"),
                TestTavernSpell("free-spell-2", 5, "+1/+1", "buff_spell"),
                TestTavernSpell("free-spell-3", 5, "+1/+1", "buff_spell"),
                TestTavernSpell("paid-spell", 5, "+1/+1", "buff_spell")
            };

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 2));

            Assert.AreEqual(18, service.State.Player.Tavern.Gold);
            Assert.AreEqual(3, service.State.Player.Tavern.AdvancedMechanics.Trinkets.PeacebloomCandleBuysThisRound);

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 3));

            Assert.AreEqual(13, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void CowrieNecklace_ReducesStatTavernSpellBuyCost()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_921");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestTavernSpell("stat-spell", 5, "Give a minion +2/+2.", "buff_spell")
            };

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(17, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void HeartOfTheForest_AddsStatsToTavernSpellsAndImproves()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Board.Add(TestShopMinion("heart-target", 1, 1));

            QueueTrinketChoice(service, "BG32_MagicItem_801");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            AddBloodGemSpellToHand(service, "heart-1");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(3, service.State.Player.Board[0].Attack);
            Assert.AreEqual(3, service.State.Player.Board[0].MaxHealth);

            for (var index = 0; index < 4; index += 1)
            {
                AddBloodGemSpellToHand(service, "heart-extra-" + index);
                service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));
            }

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(2, trinkets.HeartOfForestBonusAttack);
            Assert.AreEqual(2, trinkets.HeartOfForestBonusHealth);
        }

        [Test]
        public void MarvelousMushroom_ImprovesTavernSpellBonusAtTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Board.Add(TestShopMinion("mushroom-target", 1, 1));

            QueueTrinketChoice(service, "BG32_MagicItem_700");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            AddBloodGemSpellToHand(service, "mushroom");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(4, service.State.Player.Board[0].Attack);
            Assert.AreEqual(4, service.State.Player.Board[0].MaxHealth);
            Assert.AreEqual(2, service.State.Player.Tavern.AdvancedMechanics.Trinkets.MarvelousMushroomBonusAttack);
        }

        [Test]
        public void WizardsPipe_AfterTavernSpellBuffsTypelessFriendlyMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            var typeless = TestShopMinion("typeless", 1, 1);
            var murloc = TestShopMinion("murloc", 2, 2);
            murloc.Tribes = new List<Tribe> { Tribe.Murloc };
            service.State.Player.Board.Add(typeless);
            service.State.Player.Board.Add(murloc);

            QueueTrinketChoice(service, "BG32_MagicItem_281");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            AddBloodGemSpellToHand(service, "pipe");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(6, typeless.Attack);
            Assert.AreEqual(6, typeless.MaxHealth);
            Assert.AreEqual(2, murloc.Attack);
            Assert.AreEqual(2, murloc.MaxHealth);
        }

        [Test]
        public void SinstoneSticker_FirstTwoDiscoversCopyPickedCard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_801");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            StartTestDiscover(service, "sinstone-one");

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "sinstone-one"));
            Assert.AreEqual(1, service.State.Player.Tavern.AdvancedMechanics.Trinkets.SinstoneStickerCopiesThisRound);
        }

        [Test]
        public void LubberSticker_RerollAddsExtraTavernSpellAndDiscountsFirstTavernSpellEachTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_935");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.State.Player.Tavern.Gold = 20;
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.GreaterOrEqual(service.State.Player.Tavern.Shop.Count(card => card != null && card.CardKind == CardKind.TavernSpell), 2);

            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestTavernSpell("lubber-spell-one", 5, "Give a minion +1/+1.", "buff_spell"),
                TestTavernSpell("lubber-spell-two", 5, "Give a minion +1/+1.", "buff_spell")
            };
            service.State.Player.Tavern.Gold = 20;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            Assert.AreEqual(16, service.State.Player.Tavern.Gold);

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            Assert.AreEqual(11, service.State.Player.Tavern.Gold);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.GreaterOrEqual(service.State.Player.Tavern.Shop.Count(card => card != null && card.CardKind == CardKind.TavernSpell), 2);
        }

        [Test]
        public void WaterWheel_PlayedElementalsAddAtMostTwoTavernSpellsPerTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_851");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Hand.Add(TestTribeMinion("water-elemental-one", 1, 1, Tribe.Elemental));
            service.State.Player.Tavern.Hand.Add(TestTribeMinion("water-elemental-two", 1, 1, Tribe.Elemental));
            service.State.Player.Tavern.Hand.Add(TestTribeMinion("water-elemental-three", 1, 1, Tribe.Elemental));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
            Assert.AreEqual(2, service.State.Player.Tavern.AdvancedMechanics.Trinkets.WaterWheelTriggersThisRound);
        }

        [Test]
        public void PrimordialTerrarium_PlayedElementalDiscountsNextTavernSpell()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_979");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Tavern.Hand.Add(TestTribeMinion("terrarium-elemental", 1, 1, Tribe.Elemental));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(1, service.State.Player.Tavern.NextTavernSpellCostReduction);

            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestTavernSpell("terrarium-spell", 5, "Give a minion +1/+1.", "buff_spell")
            };
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(16, service.State.Player.Tavern.Gold);
            Assert.AreEqual(0, service.State.Player.Tavern.NextTavernSpellCostReduction);
        }

        [Test]
        public void PrimalfinPortrait_AddsPrimalfinAndTavernSpellAfterDiscoveringMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_702");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BGS_020"));

            StartTestDiscover(service, "primalfin-discover");
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void AzsharanStatuette_AddsTemporarySpellcraftOnEquipAndTurnStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_931");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count(card => card.Tags.Contains("temporary_spellcraft_card")));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count(card => card.Tags.Contains("temporary_spellcraft_card")));
        }

        [Test]
        public void PreciousPearl_SpellcraftBuffsUntilNextTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("pearl-target", 2, 2);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_714");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(32, target.Attack);
            Assert.AreEqual(32, target.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, target.Attack);
            Assert.AreEqual(2, target.MaxHealth);
        }

        [Test]
        public void OphidianStaff_SpellcraftBuffsBeastAndTemporaryReborn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var beast = TestTribeMinion("ophidian-beast", 3, 3, Tribe.Beast);
            service.State.Player.Board.Add(beast);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_872");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(5, beast.Attack);
            Assert.AreEqual(5, beast.MaxHealth);
            Assert.IsTrue(beast.Keywords.Contains(Keyword.Reborn));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(3, beast.Attack);
            Assert.AreEqual(3, beast.MaxHealth);
            Assert.IsFalse(beast.Keywords.Contains(Keyword.Reborn));
        }

        [Test]
        public void VibrantBubble_SpellcraftGivesMurlocTemporaryBonusKeyword()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var murloc = TestTribeMinion("vibrant-murloc", 4, 4, Tribe.Murloc);
            var bonusKeywords = new[] { Keyword.Taunt, Keyword.DivineShield, Keyword.Windfury, Keyword.Reborn };
            service.State.Player.Board.Add(murloc);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_892");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.IsTrue(bonusKeywords.Any(keyword => murloc.Keywords.Contains(keyword)));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsFalse(bonusKeywords.Any(keyword => murloc.Keywords.Contains(keyword)));
        }

        [Test]
        public void GroundbreakerPortrait_AddsGroundbreakerAndBuffsLeftNeighborWhenNagaIsPlayed()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var left = TestShopMinion("groundbreaker-left", 2, 3);
            service.State.Player.Board.Add(left);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_924");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var groundbreakerIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG31_035");
            Assert.AreNotEqual(-1, groundbreakerIndex);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, groundbreakerIndex, -1));
            var groundbreaker = service.State.Player.Board.Single(card => card.CardId == "BG31_035");

            service.State.Player.Tavern.Hand.Add(TestTribeMinion("groundbreaker-naga", 1, 1, Tribe.Naga));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, -1));

            Assert.AreEqual(3, left.Attack);
            Assert.AreEqual(4, left.MaxHealth);
            Assert.AreEqual(6, groundbreaker.Attack);
            Assert.AreEqual(5, groundbreaker.MaxHealth);
        }

        [Test]
        public void GlowscalePortrait_AddsTimewarpedGlowscaleAndBuffsDivineShieldMinionsAfterSpellcraft()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("glowscale-target", 2, 2);
            var shielded = TestShopMinion("glowscale-shielded", 4, 4, Keyword.DivineShield);
            var unshielded = TestShopMinion("glowscale-unshielded", 6, 6);
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(shielded);
            service.State.Player.Board.Add(unshielded);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_548");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var glowscaleIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG34_Giant_035");
            Assert.AreNotEqual(-1, glowscaleIndex);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, glowscaleIndex, -1));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "TIMEWARPED_GLOWSCALE_SPELL");
            Assert.AreNotEqual(-1, spellIndex);
            Assert.IsTrue(service.State.Player.Tavern.Hand[spellIndex].Tags.Contains("temporary_spellcraft_card"));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, 0));

            Assert.IsTrue(target.Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(5, target.MaxHealth);
            Assert.AreEqual(7, shielded.Attack);
            Assert.AreEqual(7, shielded.MaxHealth);
            Assert.AreEqual(6, unshielded.Attack);
            Assert.AreEqual(6, unshielded.MaxHealth);
        }

        [Test]
        public void WearyPortrait_AddsWearyMageAndMakesWearySpellcraftPermanent()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestTribeMinion("weary-target", 2, 3, Tribe.Naga);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_933");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var wearyIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG31_830");
            Assert.AreNotEqual(-1, wearyIndex);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, wearyIndex, -1));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "WEARY_MAGE_SPELL");
            Assert.AreNotEqual(-1, spellIndex);
            Assert.IsTrue(service.State.Player.Tavern.Hand[spellIndex].Tags.Contains("temporary_spellcraft_card"));
            Assert.IsTrue(service.State.Player.Tavern.Hand[spellIndex].Tags.Contains("permanent_weary_spellcraft"));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, 0));

            Assert.AreEqual(4, target.Attack);
            Assert.AreEqual(5, target.MaxHealth);
            Assert.IsTrue(target.Keywords.Contains(Keyword.Reborn));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(4, target.Attack);
            Assert.AreEqual(5, target.MaxHealth);
            Assert.IsTrue(target.Keywords.Contains(Keyword.Reborn));
        }

        [Test]
        public void AzeritePortrait_AddsLivingAzeriteAndBuffsFriendlyElementalsWhenTavernSpellIsCast()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var elemental = TestTribeMinion("azerite-elemental", 2, 4, Tribe.Elemental);
            var neutral = TestShopMinion("azerite-neutral", 5, 5);
            var shopElemental = TestTribeMinion("azerite-shop-elemental", 1, 1, Tribe.Elemental);
            var shopNeutral = TestShopMinion("azerite-shop-neutral", 3, 3);
            service.State.Player.Board.Add(elemental);
            service.State.Player.Board.Add(neutral);
            service.State.Player.Tavern.Shop = new List<MinionInstance> { shopElemental, shopNeutral };
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_431");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var livingAzeriteIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG28_707");
            Assert.AreNotEqual(-1, livingAzeriteIndex);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, livingAzeriteIndex, -1));
            var livingAzerite = service.State.Player.Board.Single(card => card.CardId == "BG28_707");

            AddBloodGemSpellToHand(service, "azerite");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 1));

            Assert.AreEqual(5, elemental.Attack);
            Assert.AreEqual(6, elemental.MaxHealth);
            Assert.AreEqual(6, neutral.Attack);
            Assert.AreEqual(6, neutral.MaxHealth);
            Assert.AreEqual(9, livingAzerite.Attack);
            Assert.AreEqual(7, livingAzerite.MaxHealth);
            Assert.AreEqual(4, shopElemental.Attack);
            Assert.AreEqual(3, shopElemental.MaxHealth);
            Assert.AreEqual(3, shopNeutral.Attack);
            Assert.AreEqual(3, shopNeutral.MaxHealth);
        }

        [Test]
        public void NazjatarPostcard_PlayNagaAddsRandomTemporarySpellcraft()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_919");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.State.Player.Tavern.Hand.Add(TestTribeMinion("nazjatar-naga", 1, 1, Tribe.Naga));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, -1));

            var spell = service.State.Player.Tavern.Hand.Single();
            Assert.AreEqual(CardKind.Spell, spell.CardKind);
            Assert.IsTrue(spell.Tags.Contains("spellcraft"));
            Assert.IsTrue(spell.Tags.Contains("temporary_spellcraft_card"));
        }

        [Test]
        public void ArchaicScroll_AfterSixSpellsAddsRandomNaga()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("archaic-target", 1, 20);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_930");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            for (var index = 0; index < 6; index += 1)
            {
                AddBloodGemSpellToHand(service, "archaic-" + index);
                service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));
            }

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card =>
                card.CardKind == CardKind.Minion &&
                card.Tribes.Contains(Tribe.Naga)));
        }

        [Test]
        public void SpitescaleSushiRoll_FirstTwoSpellcraftsEachTurnCastExtraTime()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("spitescale-target", 2, 20);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Tier = 2;
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_920");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "110406"));
            for (var index = 0; index < 3; index += 1)
            {
                service.State.Player.Tavern.Hand.Add(TestSpellcraftSpell("REEF_RIFFER_SPELL", "spitescale-" + index));
                service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));

                Assert.AreEqual(new[] { 6, 10, 12 }[index], target.Attack);
            }

            Assert.AreEqual(12, target.Attack);
            Assert.AreEqual(30, target.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(2, target.Attack);
            Assert.AreEqual(20, target.MaxHealth);

            for (var index = 0; index < 2; index += 1)
            {
                service.State.Player.Tavern.Hand.Add(TestSpellcraftSpell("REEF_RIFFER_SPELL", "spitescale-next-" + index));
                service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));
            }

            Assert.AreEqual(10, target.Attack);
            Assert.AreEqual(28, target.MaxHealth);
        }

        [Test]
        public void CoralSpear_SpellcraftCastsMightOfStormwindForEachActualCast()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("coral-target", 2, 20);
            var allyOne = TestShopMinion("coral-ally-one", 3, 3);
            var allyTwo = TestShopMinion("coral-ally-two", 4, 4);
            var allyThree = TestShopMinion("coral-ally-three", 5, 5);
            var untouched = TestShopMinion("coral-untouched", 6, 6);
            service.State.Player.Board.AddRange(new[] { target, allyOne, allyTwo, allyThree, untouched });
            service.State.Player.Tavern.Tier = 2;
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG30_MagicItem_434");
            EquipTrinket(service, "BG35_MagicItem_925");

            var attackBefore = service.State.Player.Board.Sum(minion => minion.Attack);
            var healthBefore = service.State.Player.Board.Sum(minion => minion.MaxHealth);

            var spell = TestSpellcraftSpell("REEF_RIFFER_SPELL", "coral");
            spell.CardKind = CardKind.TavernSpell;
            spell.Keywords.Add(Keyword.TavernSpell);
            spell.OfficialKeywords.Add(Keyword.TavernSpell);
            service.State.Player.Tavern.Hand.Add(spell);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.AreEqual(attackBefore + 12, service.State.Player.Board.Sum(minion => minion.Attack));
            Assert.AreEqual(healthBefore + 20, service.State.Player.Board.Sum(minion => minion.MaxHealth));
            Assert.AreEqual(4, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(8, service.State.Player.Board.Sum(minion =>
                minion.Enchantments.Count(enchantment => enchantment.SourceId == "Might of Stormwind")));
        }

        [Test]
        public void DoubleStitchNeedle_SpellcraftDoublesFriendlyMinionAndLocksItInHand()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("double-stitch-target", 3, 5);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_838");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "TRINKET_DOUBLE_STITCH_NEEDLE_SPELL");
            Assert.AreNotEqual(-1, spellIndex);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, 0));

            Assert.IsFalse(service.State.Player.Board.Contains(target));
            Assert.AreEqual(6, target.Attack);
            Assert.AreEqual(10, target.MaxHealth);
            Assert.AreEqual(10, target.Health);
            Assert.IsTrue(target.Tags.Contains("locked_in_hand"));
            Assert.AreEqual(1, target.Counters["locked-turns"]);

            var lockedIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.InstanceId == target.InstanceId);
            Assert.AreNotEqual(-1, lockedIndex);
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.PlayMinion, lockedIndex, -1)));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var unlockedIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.InstanceId == target.InstanceId);
            Assert.AreNotEqual(-1, unlockedIndex);
            target = service.State.Player.Tavern.Hand[unlockedIndex];
            Assert.IsFalse(target.Tags.Contains("locked_in_hand"));
            Assert.IsFalse(target.Counters.ContainsKey("locked-turns"));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, unlockedIndex, -1));

            Assert.IsTrue(service.State.Player.Board.Contains(target));
        }

        [Test]
        public void TokenOfTheOldGods_SpellcraftTransformsFriendlyMinionOneTierHigher()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("token-target", 4, 9);
            target.TavernTier = 2;
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_416");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "TRINKET_TOKEN_OF_THE_OLD_GODS_SPELL");
            Assert.AreNotEqual(-1, spellIndex);
            var originalCardId = target.CardId;
            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, 0));

            Assert.AreNotEqual(originalCardId, target.CardId);
            Assert.AreEqual(3, target.TavernTier);
            Assert.AreEqual(4, target.Attack);
            Assert.AreEqual(9, target.Health);
            Assert.AreEqual(9, target.MaxHealth);
        }

        [Test]
        public void ChillmereMosaic_SpellcraftRefreshesTavernWithBattlecryMinionsCostingOne()
        {
            var minions = MinionCatalogLoader.LoadFromResources().All;
            var battlecryIds = minions
                .Where(card =>
                    card.InPool &&
                    card.PoolCount > 0 &&
                    card.TavernTier <= 4 &&
                    card.Keywords.Contains(Keyword.Battlecry) &&
                    !card.CardId.StartsWith("BGDUO"))
                .Take(6)
                .Select(card => card.CardId)
                .ToList();
            Assert.GreaterOrEqual(battlecryIds.Count, 1);

            var nonBattlecry = minions.First(card =>
                card.InPool &&
                card.PoolCount > 0 &&
                card.TavernTier <= 4 &&
                !card.Keywords.Contains(Keyword.Battlecry) &&
                !card.CardId.StartsWith("BGDUO") &&
                !battlecryIds.Contains(card.CardId));
            battlecryIds.Add(nonBattlecry.CardId);

            var service = CreateServiceWithEnabledMinions(12345, battlecryIds.ToArray());
            service.State.Player.Tavern.Tier = 4;
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_755");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == ChillmereMosaicSpellCardId);
            Assert.AreNotEqual(-1, spellIndex);
            var spell = service.State.Player.Tavern.Hand[spellIndex];
            Assert.AreEqual(CardKind.Spell, spell.CardKind);
            Assert.IsTrue(spell.Tags.Contains("spellcraft"));
            Assert.IsTrue(spell.Tags.Contains("temporary_spellcraft_card"));
            Assert.IsFalse(spell.Tags.Contains("targeted_spell"));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, -1));

            var shop = service.State.Player.Tavern.Shop.Where(card => card != null).ToList();
            Assert.Greater(shop.Count, 0);
            Assert.IsTrue(shop.All(card => card.CardKind == CardKind.Minion));
            Assert.IsTrue(shop.All(card => card.Keywords.Contains(Keyword.Battlecry)));
            Assert.IsTrue(shop.All(card => card.Cost == 1));
            Assert.IsFalse(shop.Any(card => card.CardId == nonBattlecry.CardId));

            var bought = shop[0];
            var beforeGold = service.State.Player.Tavern.Gold;
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(beforeGold - 1, service.State.Player.Tavern.Gold);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.InstanceId == bought.InstanceId));
        }

        [Test]
        public void ChromaticTear_AddsChromadrakesAndRepeatsAfterSevenBattlecryMinions()
        {
            var service = CreateServiceWithEnabledMinions(12345, ChromadrakeCardIds.ToArray());
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_840t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(IsChromadrake));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));

            var plain = TestShopMinion("chromatic-tear-plain", 1, 1);
            service.State.Player.Tavern.Hand.Add(plain);
            PlayHandCard(service, plain);

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
            Assert.IsFalse(service.State.Player.Tavern.AdvancedMechanics.Counters.ContainsKey(ChromaticTearBattlecryCounter));
            service.State.Player.Board.Clear();

            for (var index = 0; index < 6; index += 1)
            {
                var battlecry = TestShopMinion("chromatic-tear-battlecry-" + index, 1, 1, Keyword.Battlecry);
                service.State.Player.Tavern.Hand.Add(battlecry);
                PlayHandCard(service, battlecry);
            }

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(6, service.State.Player.Tavern.AdvancedMechanics.Counters[ChromaticTearBattlecryCounter]);

            var seventh = TestShopMinion("chromatic-tear-battlecry-6", 1, 1, Keyword.Battlecry);
            service.State.Player.Tavern.Hand.Add(seventh);
            PlayHandCard(service, seventh);

            Assert.AreEqual(4, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(0, service.State.Player.Tavern.AdvancedMechanics.Counters[ChromaticTearBattlecryCounter]);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(IsChromadrake));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.Minion));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.Tribes.Contains(Tribe.Dragon)));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.Keywords.Contains(Keyword.Battlecry)));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));
        }

        [Test]
        public void ThaumaturgistPortrait_AddsThaumaturgistAndMakesItsSpellcraftPermanent()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("thaumaturgist-target", 2, 3);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_920");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var thaumaturgistIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG31_924");
            Assert.AreNotEqual(-1, thaumaturgistIndex);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, thaumaturgistIndex, -1));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "THAUMATURGIST_SPELL");
            Assert.AreNotEqual(-1, spellIndex);
            Assert.IsTrue(service.State.Player.Tavern.Hand[spellIndex].Tags.Contains("temporary_spellcraft_card"));
            Assert.IsTrue(service.State.Player.Tavern.Hand[spellIndex].Tags.Contains("permanent_thaumaturgist_spellcraft"));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, 0));

            Assert.AreEqual(3, target.Attack);
            Assert.AreEqual(4, target.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(3, target.Attack);
            Assert.AreEqual(4, target.MaxHealth);
        }

        [Test]
        public void CharmingPanpipes_EndTurnBuffsLeftMostAndImprovesAfterSpells()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var left = TestShopMinion("panpipes-left", 2, 3);
            var right = TestShopMinion("panpipes-right", 10, 10);
            service.State.Player.Board.Add(left);
            service.State.Player.Board.Add(right);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_922");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(3, trinkets.CharmingPanpipesAttack);
            Assert.AreEqual(3, trinkets.CharmingPanpipesHealth);

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));
            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(5, trinkets.CharmingPanpipesAttack);
            Assert.AreEqual(5, trinkets.CharmingPanpipesHealth);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(7, left.Attack);
            Assert.AreEqual(8, left.MaxHealth);
            Assert.AreEqual(10, right.Attack);
            Assert.AreEqual(10, right.MaxHealth);
        }

        [Test]
        public void ChargingStaff_EndTurnBuffsDivineShieldMinionsAttackOnly()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var firstShielded = TestShopMinion("charging-staff-first", 2, 3, Keyword.DivineShield);
            var unshielded = TestShopMinion("charging-staff-unshielded", 5, 6);
            var secondShielded = TestShopMinion("charging-staff-second", 8, 9, Keyword.DivineShield);
            service.State.Player.Board.Add(firstShielded);
            service.State.Player.Board.Add(unshielded);
            service.State.Player.Board.Add(secondShielded);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_984t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(9, firstShielded.Attack);
            Assert.AreEqual(3, firstShielded.MaxHealth);
            Assert.AreEqual(5, unshielded.Attack);
            Assert.AreEqual(6, unshielded.MaxHealth);
            Assert.AreEqual(15, secondShielded.Attack);
            Assert.AreEqual(9, secondShielded.MaxHealth);
        }

        [Test]
        public void ChargingStaffLesser_EndTurnBuffsDivineShieldMinionsAttackOnly()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var firstShielded = TestShopMinion("charging-staff-lesser-first", 2, 3, Keyword.DivineShield);
            var unshielded = TestShopMinion("charging-staff-lesser-unshielded", 5, 6);
            var secondShielded = TestShopMinion("charging-staff-lesser-second", 8, 9, Keyword.DivineShield);
            service.State.Player.Board.Add(firstShielded);
            service.State.Player.Board.Add(unshielded);
            service.State.Player.Board.Add(secondShielded);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_984");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(5, firstShielded.Attack);
            Assert.AreEqual(3, firstShielded.MaxHealth);
            Assert.AreEqual(5, unshielded.Attack);
            Assert.AreEqual(6, unshielded.MaxHealth);
            Assert.AreEqual(11, secondShielded.Attack);
            Assert.AreEqual(9, secondShielded.MaxHealth);
        }

        [Test]
        public void DazzlingDagger_TracksFourSpellThresholdAndAppliesToPlayedAndCombatMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var existing = TestShopMinion("dazzling-existing", 3, 4);
            service.State.Player.Board.Add(existing);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_934");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(4, existing.Attack);

            for (var index = 0; index < 3; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));
            }

            Assert.AreEqual(4, existing.Attack);

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(5, existing.Attack);

            var played = TestShopMinion("dazzling-played", 5, 6);
            service.State.Player.Tavern.Hand.Add(played);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, -1));

            Assert.AreEqual(7, played.Attack);

            RunStartOfCombat(service);

            Assert.AreEqual(5, FinalCombatMinion(service, existing).Attack);
            Assert.AreEqual(7, FinalCombatMinion(service, played).Attack);
        }

        [Test]
        public void DebugReplaceTrinket_RemovesDazzlingDaggerAndHordeKeychainAuras()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var minion = TestShopMinion("replacement-tracked-aura", 3, 4);
            service.State.Player.Board.Add(minion);

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG32_MagicItem_934", CardKind.Trinket, 1));
            Assert.AreEqual(4, minion.Attack);
            Assert.AreEqual(4, minion.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG30_MagicItem_843t", CardKind.Trinket, 1));
            Assert.AreEqual(10, minion.Attack);
            Assert.AreEqual(9, minion.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, "BG30_MagicItem_426t", CardKind.Trinket, 1));
            Assert.AreEqual(3, minion.Attack);
            Assert.AreEqual(4, minion.MaxHealth);
        }

        [Test]
        public void BewitchedRibbon_SpellsPermanentlyBuffBoardAndAddsCombatOnlyBonus()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var first = TestShopMinion("ribbon-first", 2, 3);
            var second = TestShopMinion("ribbon-second", 4, 5);
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_923");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));
            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(4, first.Attack);
            Assert.AreEqual(5, first.MaxHealth);
            Assert.AreEqual(6, second.Attack);
            Assert.AreEqual(7, second.MaxHealth);

            RunStartOfCombat(service);

            Assert.AreEqual(6, FinalCombatMinion(service, first).Attack);
            Assert.AreEqual(7, FinalCombatMinion(service, first).MaxHealth);
            Assert.AreEqual(8, FinalCombatMinion(service, second).Attack);
            Assert.AreEqual(9, FinalCombatMinion(service, second).MaxHealth);
            Assert.AreEqual(4, first.Attack);
            Assert.AreEqual(5, first.MaxHealth);
        }

        [Test]
        public void ComfyCoffin_TavernSpellImprovesUndeadWhereverTheyAre()
        {
            var lesserService = MatchService.CreateWithDefaultCatalog(12345);
            var lesserBoard = TestTribeMinion("coffin-lesser-board", 2, 3, Tribe.Undead);
            var lesserHand = TestTribeMinion("coffin-lesser-hand", 4, 5, Tribe.Undead);
            var lesserShop = TestTribeMinion("coffin-lesser-shop", 6, 7, Tribe.Undead);
            var lesserNonUndead = TestTribeMinion("coffin-lesser-beast", 8, 9, Tribe.Beast);
            lesserService.State.Player.Board.Add(lesserBoard);
            lesserService.State.Player.Tavern.Hand.Add(lesserHand);
            lesserService.State.Player.Tavern.Shop.Clear();
            lesserService.State.Player.Tavern.Shop.Add(lesserShop);
            lesserService.State.Player.Tavern.Shop.Add(lesserNonUndead);
            lesserService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(lesserService, "BG30_MagicItem_547");
            lesserService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            lesserService.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(3, lesserBoard.Attack);
            Assert.AreEqual(5, lesserHand.Attack);
            Assert.AreEqual(7, lesserShop.Attack);
            Assert.AreEqual(8, lesserNonUndead.Attack);
            Assert.AreEqual(1, lesserService.State.Player.Tavern.UndeadAttackBonus);
            Assert.IsTrue(lesserService.State.Player.Tavern.Growth.ShopModifiers.Any(modifier =>
                modifier.Tribe == Tribe.Undead &&
                modifier.Attack == 1 &&
                modifier.SourceId == "Comfy Coffin"));

            var greaterService = MatchService.CreateWithDefaultCatalog(12345);
            var greaterBoard = TestTribeMinion("coffin-greater-board", 2, 3, Tribe.Undead);
            var greaterHand = TestTribeMinion("coffin-greater-hand", 4, 5, Tribe.Undead);
            var greaterShop = TestTribeMinion("coffin-greater-shop", 6, 7, Tribe.Undead);
            greaterService.State.Player.Board.Add(greaterBoard);
            greaterService.State.Player.Tavern.Hand.Add(greaterHand);
            greaterService.State.Player.Tavern.Shop.Clear();
            greaterService.State.Player.Tavern.Shop.Add(greaterShop);
            greaterService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(greaterService, "BG30_MagicItem_547t");
            greaterService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            greaterService.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(4, greaterBoard.Attack);
            Assert.AreEqual(6, greaterHand.Attack);
            Assert.AreEqual(8, greaterShop.Attack);
            Assert.AreEqual(2, greaterService.State.Player.Tavern.UndeadAttackBonus);
            Assert.IsTrue(greaterService.State.Player.Tavern.Growth.ShopModifiers.Any(modifier =>
                modifier.Tribe == Tribe.Undead &&
                modifier.Attack == 2 &&
                modifier.SourceId == "Comfy Coffin"));
        }

        [Test]
        public void FeralTalisman_LesserAndGreaterStackAsOneBoardAura()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var existing = TestShopMinion("feral-existing", 3, 4);
            service.State.Player.Board.Add(existing);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_880");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(5, existing.Attack);
            Assert.AreEqual(5, existing.MaxHealth);

            QueueTrinketChoice(service, "BG30_MagicItem_880t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(13, existing.Attack);
            Assert.AreEqual(10, existing.MaxHealth);
            Assert.AreEqual(1, existing.Enchantments.Count(enchantment => enchantment.SourceId == FeralTalismanAuraSourceId));
            Assert.IsTrue(existing.Enchantments.Any(enchantment =>
                enchantment.SourceId == FeralTalismanAuraSourceId &&
                enchantment.AttackBonus == 10 &&
                enchantment.HealthBonus == 6));

            var played = TestShopMinion("feral-played", 2, 3);
            service.State.Player.Tavern.Hand.Add(played);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(12, played.Attack);
            Assert.AreEqual(9, played.MaxHealth);
            Assert.AreEqual(1, played.Enchantments.Count(enchantment => enchantment.SourceId == FeralTalismanAuraSourceId));

            RunStartOfCombat(service);

            Assert.AreEqual(13, FinalCombatMinion(service, existing).Attack);
            Assert.AreEqual(10, FinalCombatMinion(service, existing).MaxHealth);
            Assert.AreEqual(12, FinalCombatMinion(service, played).Attack);
            Assert.AreEqual(9, FinalCombatMinion(service, played).MaxHealth);
        }

        [Test]
        public void ArtisanalUrn_LesserAndGreaterStackForUndeadAndCountedTribes()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var undead = TestTribeMinion("urn-undead", 2, 3, Tribe.Undead);
            var allTribe = TestTribeMinion("urn-all", 4, 5, Tribe.All);
            var beast = TestTribeMinion("urn-beast", 6, 7, Tribe.Beast);
            service.State.Player.Board.Add(undead);
            service.State.Player.Board.Add(allTribe);
            service.State.Player.Board.Add(beast);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_989");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            QueueTrinketChoice(service, "BG30_MagicItem_989t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(20, undead.Attack);
            Assert.AreEqual(3, undead.MaxHealth);
            Assert.AreEqual(22, allTribe.Attack);
            Assert.AreEqual(5, allTribe.MaxHealth);
            Assert.AreEqual(6, beast.Attack);
            Assert.AreEqual(7, beast.MaxHealth);
            Assert.AreEqual(1, undead.Enchantments.Count(enchantment => enchantment.SourceId == ArtisanalUrnAuraSourceId));
            Assert.AreEqual(1, allTribe.Enchantments.Count(enchantment => enchantment.SourceId == ArtisanalUrnAuraSourceId));
            Assert.AreEqual(0, beast.Enchantments.Count(enchantment => enchantment.SourceId == ArtisanalUrnAuraSourceId));

            var playedUndead = TestTribeMinion("urn-played-undead", 1, 2, Tribe.Undead);
            service.State.Player.Tavern.Hand.Add(playedUndead);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(19, playedUndead.Attack);
            Assert.AreEqual(2, playedUndead.MaxHealth);
            Assert.AreEqual(1, playedUndead.Enchantments.Count(enchantment => enchantment.SourceId == ArtisanalUrnAuraSourceId));
        }

        [Test]
        public void GildedAnchor_LesserAndGreaterEndTurnBuffGoldenMinionsOnly()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var golden = TestShopMinion("gilded-anchor-golden", 5, 6);
            golden.Golden = true;
            var normal = TestShopMinion("gilded-anchor-normal", 7, 8);
            service.State.Player.Board.Add(golden);
            service.State.Player.Board.Add(normal);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_231");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            QueueTrinketChoice(service, "BG32_MagicItem_231t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(18, golden.Attack);
            Assert.AreEqual(19, golden.MaxHealth);
            Assert.AreEqual(7, normal.Attack);
            Assert.AreEqual(8, normal.MaxHealth);
        }

        [Test]
        public void LorewalkerScroll_LesserAndGreaterBuffSpellTarget()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("lorewalker-target", 2, 3);
            var other = TestShopMinion("lorewalker-other", 4, 5);
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(other);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_422");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            QueueTrinketChoice(service, "BG30_MagicItem_422t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            AddBloodGemSpellToHand(service, "lorewalker");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.AreEqual(17, target.Attack);
            Assert.AreEqual(18, target.MaxHealth);
            Assert.AreEqual(4, other.Attack);
            Assert.AreEqual(5, other.MaxHealth);
        }

        [Test]
        public void NerglishPhrasebook_LesserAndGreaterBuffLeftMostHandMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var played = TestShopMinion("nerglish-played", 2, 2);
            var leftMostMinion = TestShopMinion("nerglish-left", 3, 4);
            var rightMinion = TestShopMinion("nerglish-right", 5, 6);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Hand.Add(played);
            service.State.Player.Tavern.Hand.Add(TestTavernSpell("NERGLISH_TEST_SPELL", 0, "No-op test spell"));
            service.State.Player.Tavern.Hand.Add(leftMostMinion);
            service.State.Player.Tavern.Hand.Add(rightMinion);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_914");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            QueueTrinketChoice(service, "BG30_MagicItem_914t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(12, leftMostMinion.Attack);
            Assert.AreEqual(13, leftMostMinion.MaxHealth);
            Assert.AreEqual(5, rightMinion.Attack);
            Assert.AreEqual(6, rightMinion.MaxHealth);
        }

        [Test]
        public void NomiSticker_LesserAndGreaterGrowCurrentAndFutureTavernElementals()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var shopElemental = TestTribeMinion("nomi-sticker-shop-elemental", 2, 3, Tribe.Elemental);
            var shopBeast = TestTribeMinion("nomi-sticker-shop-beast", 4, 5, Tribe.Beast);
            var playedElemental = TestTribeMinion("nomi-sticker-played", 1, 1, Tribe.Elemental);
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(shopElemental);
            service.State.Player.Tavern.Shop.Add(shopBeast);
            service.State.Player.Tavern.Hand.Add(playedElemental);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_544");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            QueueTrinketChoice(service, "BG30_MagicItem_544t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, -1));

            Assert.AreEqual(9, shopElemental.Attack);
            Assert.AreEqual(10, shopElemental.MaxHealth);
            Assert.AreEqual(4, shopBeast.Attack);
            Assert.AreEqual(5, shopBeast.MaxHealth);
            Assert.IsTrue(service.State.Player.Tavern.Growth.ShopModifiers.Any(modifier =>
                modifier.SourceId == "Nomi Sticker" &&
                modifier.Tribe == Tribe.Elemental &&
                modifier.Attack == 2 &&
                modifier.Health == 2));
            Assert.IsTrue(service.State.Player.Tavern.Growth.ShopModifiers.Any(modifier =>
                modifier.SourceId == "Nomi Sticker" &&
                modifier.Tribe == Tribe.Elemental &&
                modifier.Attack == 5 &&
                modifier.Health == 5));
        }

        [Test]
        public void FountainPen_LesserAndGreaterImproveElementalStatGrantsAndFutureShopGrowth()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var nomi = TestTribeMinion("BGS_104", 1, 1, Tribe.Elemental);
            var shopElemental = TestTribeMinion("fountain-shop-elemental", 2, 3, Tribe.Elemental);
            var shopBeast = TestTribeMinion("fountain-shop-beast", 4, 5, Tribe.Beast);
            var playedElemental = TestTribeMinion("fountain-played", 1, 1, Tribe.Elemental);
            service.State.Player.Board.Add(nomi);
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(shopElemental);
            service.State.Player.Tavern.Shop.Add(shopBeast);
            service.State.Player.Tavern.Hand.Add(playedElemental);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_802");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            QueueTrinketChoice(service, "BG32_MagicItem_802t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, -1));

            Assert.AreEqual(12, shopElemental.Attack);
            Assert.AreEqual(10, shopElemental.MaxHealth);
            Assert.AreEqual(4, shopBeast.Attack);
            Assert.AreEqual(5, shopBeast.MaxHealth);
            Assert.IsTrue(shopElemental.Enchantments.Any(enchantment =>
                enchantment.SourceId == "Fountain Pen" &&
                enchantment.AttackBonus == 6 &&
                enchantment.HealthBonus == 3));
            Assert.IsTrue(service.State.Player.Tavern.Growth.ShopModifiers.Any(modifier =>
                modifier.SourceId == "Nomi" &&
                modifier.Tribe == Tribe.Elemental &&
                modifier.Attack == 10 &&
                modifier.Health == 7));
        }

        [Test]
        public void Batch2EquipGenerators_AddExpectedCardsWithCopyMetadata()
        {
            AssertSingleEquipAdds("BG30_MagicItem_916", 2, card => card.CardId == "105266");
            AssertSingleEquipAdds("BG35_MagicItem_305", 1, card => card.CardId == "110400");
            AssertSingleEquipAdds("BG35_MagicItem_817", 1, card => card.CardId == "130853");
            AssertSingleEquipAdds("BG35_MagicItem_434", 1, card => JewelryBoxGemCardIds.Contains(card.CardId));
            AssertSingleEquipAdds("BG32_MagicItem_894", 1, card => card.CardId == "104472");
            AssertSingleEquipAdds("BG35_MagicItem_840", 1, IsChromadrake);
            AssertSingleEquipAdds("BG35_MagicItem_712", 1, card => card.CardId == "BG33_825");
            AssertSingleEquipAdds("BG35_MagicItem_712", 2, card => Batch2BountyCardIds.Contains(card.CardId));
            AssertSingleEquipAdds("BG35_MagicItem_890", 2, card => Batch2BountyCardIds.Contains(card.CardId));
            AssertSingleEquipAdds("BG35_MagicItem_309", 1, card => card.CardId == "BG35_140" || card.CardId == "BG35_141");
            AssertSingleEquipAdds("BG32_MagicItem_950", 1, card => card.CardId == "BG31_822");
            AssertSingleEquipAdds("BG32_MagicItem_925", 1, card => card.CardId == "BG31_148");
        }

        [Test]
        public void Batch2ScheduledGenerators_RespectCadence()
        {
            var eggService = MatchService.CreateWithDefaultCatalog(12345);
            eggService.State.Player.Tavern.Hand.Clear();
            eggService.State.Player.Tavern.Gold = 20;
            EquipTrinket(eggService, "BG35_MagicItem_842");

            Assert.AreEqual(1, eggService.State.Player.Tavern.Hand.Count(card => card.CardId == "BG34_639"));
            eggService.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(1, eggService.State.Player.Tavern.Hand.Count(card => card.CardId == "BG34_639"));
            eggService.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(2, eggService.State.Player.Tavern.Hand.Count(card => card.CardId == "BG34_639"));

            var greaterEggService = MatchService.CreateWithDefaultCatalog(12345);
            greaterEggService.State.Player.Tavern.Hand.Clear();
            greaterEggService.State.Player.Tavern.Gold = 20;
            EquipTrinket(greaterEggService, "BG35_MagicItem_848t");

            var goldenEgg = greaterEggService.State.Player.Tavern.Hand.Single(card => card.CardId == "BG34_639");
            Assert.IsTrue(goldenEgg.Golden);
            Assert.IsTrue(goldenEgg.Tags.Contains("locked_in_hand"));
            Assert.IsTrue(goldenEgg.Counters.ContainsKey("locked-turns"));

            var conchService = MatchService.CreateWithDefaultCatalog(12345);
            conchService.State.Player.Tavern.Hand.Clear();
            conchService.State.Player.Tavern.Gold = 20;
            EquipTrinket(conchService, "BG35_MagicItem_305");

            Assert.AreEqual(1, conchService.State.Player.Tavern.Hand.Count(card => card.CardId == "110400"));
            conchService.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(1, conchService.State.Player.Tavern.Hand.Count(card => card.CardId == "110400"));
            conchService.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(2, conchService.State.Player.Tavern.Hand.Count(card => card.CardId == "110400"));

            var lensService = MatchService.CreateWithDefaultCatalog(12345);
            lensService.State.Player.Tavern.Hand.Clear();
            lensService.State.Player.Tavern.Gold = 20;
            EquipTrinket(lensService, "BG35_MagicItem_817");

            Assert.AreEqual(1, lensService.State.Player.Tavern.Hand.Count(card => card.CardId == "130853"));
            lensService.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(1, lensService.State.Player.Tavern.Hand.Count(card => card.CardId == "130853"));
            lensService.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(2, lensService.State.Player.Tavern.Hand.Count(card => card.CardId == "130853"));

            var goldenizerService = MatchService.CreateWithDefaultCatalog(12345);
            goldenizerService.State.Player.Tavern.Hand.Clear();
            goldenizerService.State.Player.Tavern.Gold = 20;
            EquipTrinket(goldenizerService, "BG30_MagicItem_435");

            goldenizerService.Apply(new GameCommand(GameCommandType.NextTurn));
            goldenizerService.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(0, goldenizerService.State.Player.Tavern.Hand.Count(card => card.CardId == "98914"));
            goldenizerService.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(1, goldenizerService.State.Player.Tavern.Hand.Count(card => card.CardId == "98914"));

            var globeService = MatchService.CreateWithDefaultCatalog(12345);
            globeService.State.Player.Tavern.Gold = 20;
            EquipTrinket(globeService, "BG30_MagicItem_425");

            globeService.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.IsNull(globeService.State.Player.Tavern.Discover);
            globeService.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.IsNotNull(globeService.State.Player.Tavern.Discover);
            Assert.IsTrue(globeService.State.Player.Tavern.Discover.Options.All(card => card.TavernTier == 6));
        }

        [Test]
        public void Batch2RendleAndGritty_StealTavernCardsAsCopies()
        {
            var rendleService = MatchService.CreateWithDefaultCatalog(12345);
            var lowTier = TestShopMinion("rendle-low", 1, 1);
            lowTier.TavernTier = 2;
            var highTier = TestShopMinion("rendle-high", 6, 6);
            highTier.TavernTier = 6;
            rendleService.State.Player.Tavern.Shop.Clear();
            rendleService.State.Player.Tavern.Shop.Add(lowTier);
            rendleService.State.Player.Tavern.Shop.Add(highTier);
            rendleService.State.Player.Tavern.Hand.Clear();
            rendleService.State.Player.Tavern.Gold = 20;

            EquipTrinket(rendleService, "BG32_MagicItem_817");

            Assert.AreEqual(1, rendleService.State.Player.Tavern.Hand.Count);
            Assert.AreEqual("rendle-high", rendleService.State.Player.Tavern.Hand[0].CardId);
            Assert.IsFalse(rendleService.State.Player.Tavern.Shop.Any(card => card.CardId == "rendle-high"));
            AssertCopyCardMetadata(rendleService.State.Player.Tavern.Hand[0]);

            var grittyService = MatchService.CreateWithDefaultCatalog(12345);
            var pirate = TestTribeMinion("gritty-shop-pirate", 3, 3, Tribe.Pirate);
            var beast = TestTribeMinion("gritty-shop-beast", 4, 4, Tribe.Beast);
            grittyService.State.Player.Tavern.Shop.Clear();
            grittyService.State.Player.Tavern.Shop.Add(pirate);
            grittyService.State.Player.Tavern.Shop.Add(beast);
            grittyService.State.Player.Tavern.Hand.Clear();
            grittyService.State.Player.Tavern.Gold = 20;

            EquipTrinket(grittyService, "BG32_MagicItem_950");
            var grittyIndex = grittyService.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG31_822");
            Assert.AreNotEqual(-1, grittyIndex);
            grittyService.Apply(new GameCommand(GameCommandType.PlayMinion, grittyIndex, -1));

            var contractIndex = grittyService.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG31_891");
            Assert.AreNotEqual(-1, contractIndex);
            grittyService.Apply(new GameCommand(GameCommandType.PlayMinion, contractIndex, -1));

            Assert.IsTrue(grittyService.State.Player.Tavern.Hand.Any(card => card.CardId == "gritty-shop-pirate"));
            Assert.IsFalse(grittyService.State.Player.Tavern.Shop.Any(card => card.CardId == "gritty-shop-pirate"));
            Assert.IsTrue(grittyService.State.Player.Tavern.Shop.Any(card => card.CardId == "gritty-shop-beast"));
            AssertCopyCardMetadata(grittyService.State.Player.Tavern.Hand.Single(card => card.CardId == "gritty-shop-pirate"));
        }

        [Test]
        public void Batch2GoldPendant_MakesTierFourOrLowerGoldenOnly()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var eligible = TestShopMinion("gold-pendant-eligible", 3, 3);
            eligible.TavernTier = 4;
            var tooHigh = TestShopMinion("gold-pendant-too-high", 5, 5);
            tooHigh.TavernTier = 5;
            service.State.Player.Board.Add(eligible);
            service.State.Player.Board.Add(tooHigh);
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG32_MagicItem_951");

            Assert.IsTrue(eligible.Golden);
            Assert.IsFalse(tooHigh.Golden);
        }

        [Test]
        public void Batch2TurnEndRewards_ScaleFromBattlecriesAndSoldMinions()
        {
            var battlecryService = MatchService.CreateWithDefaultCatalog(12345);
            var left = TestShopMinion("cliffdiver-left", 2, 3);
            var second = TestShopMinion("murky-second", 4, 5);
            battlecryService.State.Player.Board.Add(left);
            battlecryService.State.Player.Board.Add(second);
            battlecryService.State.Player.Tavern.Gold = 100;
            EquipTrinket(battlecryService, "BG32_MagicItem_890");
            EquipTrinket(battlecryService, "BG35_MagicItem_753");

            battlecryService.State.Player.Tavern.Hand.Add(TestShopMinion("batch2-battlecry-one", 1, 1, Keyword.Battlecry));
            battlecryService.State.Player.Tavern.Hand.Add(TestShopMinion("batch2-battlecry-two", 1, 1, Keyword.Battlecry));
            battlecryService.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));
            battlecryService.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));
            battlecryService.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(10, left.Attack);
            Assert.AreEqual(10, left.MaxHealth);
            Assert.AreEqual(7, second.Attack);
            Assert.AreEqual(8, second.MaxHealth);

            var windfallService = MatchService.CreateWithDefaultCatalog(12345);
            var soldOne = TestShopMinion("windfall-sold-one", 1, 1);
            var soldTwo = TestShopMinion("windfall-sold-two", 1, 1);
            windfallService.State.Player.Board.Add(soldOne);
            windfallService.State.Player.Board.Add(soldTwo);
            windfallService.State.Player.Tavern.Hand.Clear();
            windfallService.State.Player.Tavern.Gold = 100;
            EquipTrinket(windfallService, "BG32_MagicItem_832");
            EquipTrinket(windfallService, "BG32_MagicItem_832t");

            windfallService.Apply(new GameCommand(GameCommandType.SellMinion, soldOne.InstanceId));
            windfallService.Apply(new GameCommand(GameCommandType.SellMinion, soldTwo.InstanceId));
            windfallService.Apply(new GameCommand(GameCommandType.NextTurn));

            var tornados = windfallService.State.Player.Tavern.Hand.Where(card => card.CardId == "BG34_858").ToList();
            Assert.AreEqual(2, tornados.Count);
            CollectionAssert.AreEquivalent(
                new[] { 3, 4 },
                tornados.Select(card => card.Enchantments.Single(enchantment => enchantment.SourceId == "Windfall Portrait").AttackBonus));
            Assert.IsTrue(tornados.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(tornados.All(card => card.PoolCopiesHeld == 0));
        }

        [Test]
        public void Batch2HackerfinPortrait_TriggersBoardHackerfinsAtTurnEnd()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var hackerfin = TestTribeMinion("BG31_148", 5, 3, Tribe.Murloc, Keyword.Battlecry);
            var taunt = TestShopMinion("hackerfin-taunt", 1, 2, Keyword.Taunt);
            var shield = TestShopMinion("hackerfin-shield", 3, 4, Keyword.DivineShield);
            service.State.Player.Board.Add(hackerfin);
            service.State.Player.Board.Add(taunt);
            service.State.Player.Board.Add(shield);
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG32_MagicItem_925");
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(5, hackerfin.Attack);
            Assert.AreEqual(3, hackerfin.MaxHealth);
            Assert.AreEqual(4, taunt.Attack);
            Assert.AreEqual(6, taunt.MaxHealth);
            Assert.AreEqual(6, shield.Attack);
            Assert.AreEqual(8, shield.MaxHealth);
        }

        [Test]
        public void Batch2ExquisiteDishware_AddsMinionOfControlledType()
        {
            var service = CreateServiceWithSingleRewardTribeMinion(12345, Tribe.Pirate, out _);
            service.State.Player.Board.Add(TestTribeMinion("dishware-pirate", 2, 2, Tribe.Pirate));
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG30_MagicItem_419");
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            var reward = service.State.Player.Tavern.Hand[0];
            Assert.AreEqual(CardKind.Minion, reward.CardKind);
            CollectionAssert.Contains(BoardTribeAnalyzer.GetCountedTribes(reward), Tribe.Pirate);
            AssertCopyCardMetadata(reward);
        }

        [Test]
        public void Batch2ExquisiteDishware_CountsAllTypeForSupplySelectors()
        {
            var pirateId = SelectMinionIds(card => MatchesTribeDefinition(card, Tribe.Pirate), 1).Single();
            var service = CreateServiceWithExactEnabledMinions(12345, pirateId);
            service.State.Player.Board.Add(TestTribeMinion("dishware-all", 2, 2, Tribe.All));
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG30_MagicItem_419");
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            CollectionAssert.Contains(BoardTribeAnalyzer.GetCountedTribes(service.State.Player.Tavern.Hand[0]), Tribe.Pirate);
        }

        [Test]
        public void Batch2GeneratedTavernSpells_ApplyJewelryBoxAndBlessingEffects()
        {
            var jewelryService = MatchService.CreateWithDefaultCatalog(12345);
            var quilboar = TestTribeMinion("jewelry-quilboar", 2, 3, Tribe.Quilboar);
            jewelryService.State.Player.Board.Add(quilboar);
            jewelryService.State.Player.Tavern.Hand.Clear();
            jewelryService.State.Player.Tavern.Gold = 20;

            EquipTrinket(jewelryService, "BG35_MagicItem_434");
            var gem = jewelryService.State.Player.Tavern.Hand.Single(card => JewelryBoxGemCardIds.Contains(card.CardId));
            jewelryService.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(3, quilboar.Attack);
            Assert.AreEqual(4, quilboar.MaxHealth);
            if (gem.CardId == "TRINKET_JEWELRY_BOX_TAUNT_GEM")
            {
                CollectionAssert.Contains(quilboar.Keywords, Keyword.Taunt);
            }
            else if (gem.CardId == "TRINKET_JEWELRY_BOX_DIVINE_SHIELD_GEM")
            {
                CollectionAssert.Contains(quilboar.Keywords, Keyword.DivineShield);
            }
            else
            {
                CollectionAssert.Contains(quilboar.Keywords, Keyword.Reborn);
            }

            var blessingService = MatchService.CreateWithDefaultCatalog(12345);
            var boardDragon = TestTribeMinion("blessing-board-dragon", 2, 2, Tribe.Dragon);
            var handDragon = TestTribeMinion("blessing-hand-dragon", 4, 5, Tribe.Dragon);
            var handBeast = TestTribeMinion("blessing-hand-beast", 6, 7, Tribe.Beast);
            blessingService.State.Player.Board.Add(boardDragon);
            blessingService.State.Player.Tavern.Hand.Clear();
            blessingService.State.Player.Tavern.Hand.Add(handDragon);
            blessingService.State.Player.Tavern.Hand.Add(handBeast);
            blessingService.State.Player.Tavern.Gold = 20;

            EquipTrinket(blessingService, "BG32_MagicItem_894");
            var blessingIndex = blessingService.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "104472");
            Assert.AreNotEqual(-1, blessingIndex);
            blessingService.Apply(new GameCommand(GameCommandType.PlayMinion, blessingIndex, 0));

            Assert.AreEqual(7, handDragon.Attack);
            Assert.AreEqual(8, handDragon.MaxHealth);
            Assert.AreEqual(6, handBeast.Attack);
            Assert.AreEqual(7, handBeast.MaxHealth);
        }

        [Test]
        public void Batch2MarineSignet_GrantsExactTierTavernSpellsAndImproves()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG30_MagicItem_711");
            for (var index = 0; index < 4; index += 1)
            {
                var minion = TestShopMinion("marine-signet-first-" + index, 1, 1);
                service.State.Player.Tavern.Hand.Add(minion);
                PlayHandCard(service, minion);
                service.State.Player.Board.Clear();
            }

            var firstReward = service.State.Player.Tavern.Hand.Single(card => card.CardKind == CardKind.TavernSpell);
            Assert.AreEqual(1, firstReward.TavernTier);
            AssertCopyCardMetadata(firstReward);

            for (var index = 0; index < 4; index += 1)
            {
                var minion = TestShopMinion("marine-signet-second-" + index, 1, 1);
                service.State.Player.Tavern.Hand.Add(minion);
                PlayHandCard(service, minion);
                service.State.Player.Board.Clear();
            }

            var rewardTiers = service.State.Player.Tavern.Hand
                .Where(card => card.CardKind == CardKind.TavernSpell)
                .Select(card => card.TavernTier)
                .OrderBy(tier => tier)
                .ToList();
            CollectionAssert.AreEqual(new[] { 1, 2 }, rewardTiers);
        }

        [Test]
        public void GreatBoarSticker_LesserAndGreaterGrantAndImproveBloodGems()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("great-boar-target", 2, 3);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_988");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            QueueTrinketChoice(service, "BG30_MagicItem_988t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(8, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
            Assert.AreEqual(5, service.State.Player.Tavern.BloodGemBonusAttack);
            Assert.AreEqual(4, service.State.Player.Tavern.BloodGemBonusHealth);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(8, target.Attack);
            Assert.AreEqual(8, target.MaxHealth);
        }

        [Test]
        public void BluegillFlippers_TavernSpellBuffsLeftMostHandAndWarbandMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var boardLeft = TestShopMinion("bluegill-board-left", 2, 3);
            var boardRight = TestShopMinion("bluegill-board-right", 4, 5);
            var handLeft = TestShopMinion("bluegill-hand-left", 6, 7);
            var handRight = TestShopMinion("bluegill-hand-right", 8, 9);
            service.State.Player.Board.Add(boardLeft);
            service.State.Player.Board.Add(boardRight);
            service.State.Player.Tavern.Hand.Add(handLeft);
            service.State.Player.Tavern.Hand.Add(handRight);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_893");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(5, boardLeft.Attack);
            Assert.AreEqual(6, boardLeft.MaxHealth);
            Assert.AreEqual(4, boardRight.Attack);
            Assert.AreEqual(5, boardRight.MaxHealth);
            Assert.AreEqual(9, handLeft.Attack);
            Assert.AreEqual(10, handLeft.MaxHealth);
            Assert.AreEqual(8, handRight.Attack);
            Assert.AreEqual(9, handRight.MaxHealth);
        }

        [Test]
        public void SpellPoweredWrench_MagneticHandPlayAddsRandomTavernSpell()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestTribeMinion("wrench-mech-target", 2, 3, Tribe.Mech);
            var magnetic = TestTribeMinion("wrench-magnetic", 1, 2, Tribe.Mech, Keyword.Magnetic);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_170");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Hand.Add(magnetic);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell));
            Assert.AreEqual(3, target.Attack);
            Assert.AreEqual(5, target.MaxHealth);
        }

        [Test]
        public void RecyclingSticker_ElementalPlayGrantsFreeRefresh()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var elemental = TestTribeMinion("recycling-elemental", 1, 1, Tribe.Elemental);
            service.State.Player.Tavern.Hand.Add(elemental);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_888");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, -1));

            Assert.AreEqual(1, service.State.Player.Tavern.FreeRefreshes);
            var goldBefore = service.State.Player.Tavern.Gold;
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.AreEqual(goldBefore, service.State.Player.Tavern.Gold);
            Assert.AreEqual(0, service.State.Player.Tavern.FreeRefreshes);
        }

        [Test]
        public void AuricOffering_EndTurnRepeatsForEachFriendlyGoldenMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var left = TestShopMinion("auric-left", 2, 3);
            var goldenOne = TestShopMinion("auric-golden-one", 4, 5);
            goldenOne.Golden = true;
            var goldenTwo = TestShopMinion("auric-golden-two", 6, 7);
            goldenTwo.Golden = true;
            service.State.Player.Board.Add(left);
            service.State.Player.Board.Add(goldenOne);
            service.State.Player.Board.Add(goldenTwo);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_954");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(14, left.Attack);
            Assert.AreEqual(12, left.MaxHealth);
            Assert.AreEqual(4, goldenOne.Attack);
            Assert.AreEqual(5, goldenOne.MaxHealth);
        }

        [Test]
        public void ToxicStinger_EndTurnBuffsFriendlyMurlocAndGivesVenomous()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var murloc = TestTribeMinion("toxic-murloc", 2, 3, Tribe.Murloc);
            var beast = TestTribeMinion("toxic-beast", 4, 5, Tribe.Beast);
            service.State.Player.Board.Add(murloc);
            service.State.Player.Board.Add(beast);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_111");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(10, murloc.Attack);
            Assert.AreEqual(11, murloc.MaxHealth);
            Assert.IsTrue(murloc.Keywords.Contains(Keyword.Venomous));
            Assert.AreEqual(4, beast.Attack);
            Assert.AreEqual(5, beast.MaxHealth);
            Assert.IsFalse(beast.Keywords.Contains(Keyword.Venomous));
        }

        [Test]
        public void EnigmaticHeadstone_EndTurnImprovesUndeadWhereverTheyAre()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var boardUndead = TestTribeMinion("headstone-board", 2, 3, Tribe.Undead);
            var handUndead = TestTribeMinion("headstone-hand", 4, 5, Tribe.Undead);
            var shopUndead = TestTribeMinion("headstone-shop", 6, 7, Tribe.Undead);
            var beast = TestTribeMinion("headstone-beast", 8, 9, Tribe.Beast);
            service.State.Player.Board.Add(boardUndead);
            service.State.Player.Tavern.Hand.Add(handUndead);
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(shopUndead);
            service.State.Player.Tavern.Shop.Add(beast);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_276");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(4, boardUndead.Attack);
            Assert.AreEqual(6, handUndead.Attack);
            Assert.AreEqual(8, shopUndead.Attack);
            Assert.AreEqual(8, beast.Attack);
            Assert.AreEqual(2, service.State.Player.Tavern.UndeadAttackBonus);
            Assert.IsTrue(service.State.Player.Tavern.Growth.ShopModifiers.Any(modifier =>
                modifier.SourceId == "Enigmatic Headstone" &&
                modifier.Tribe == Tribe.Undead &&
                modifier.Attack == 2));
        }

        [Test]
        public void ToughTuskSticker_HandBloodGemGivesTemporaryDivineShield()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("tough-tusk-target", 2, 3);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_279");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            AddBloodGemSpellToHand(service, "tough-tusk");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.IsTrue(target.Keywords.Contains(Keyword.DivineShield));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsFalse(target.Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void BubbleCrown_ActivatesAfterTenSpellCastsAndImprovesTavernSpells()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("bubble-crown-target", 2, 3);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_920");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            for (var index = 0; index < 9; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));
            }

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugBoardHealthSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(2, target.Attack);
            Assert.AreEqual(7, target.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugBoardHealthSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(6, target.Attack);
            Assert.AreEqual(15, target.MaxHealth);
        }

        [Test]
        public void MiniatureShip_TavernSpellBuffsFriendlyPiratesOnly()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var pirate = TestTribeMinion("miniature-ship-pirate", 2, 3, Tribe.Pirate);
            var beast = TestTribeMinion("miniature-ship-beast", 4, 5, Tribe.Beast);
            service.State.Player.Board.Add(pirate);
            service.State.Player.Board.Add(beast);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_710");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugNoBoardSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(4, pirate.Attack);
            Assert.AreEqual(5, pirate.MaxHealth);
            Assert.AreEqual(4, beast.Attack);
            Assert.AreEqual(5, beast.MaxHealth);
        }

        [Test]
        public void FelburnedLedger_HeroDamageImprovesTavernSpellsThisTurnAndResets()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("felburned-ledger-target", 2, 3);
            service.State.Player.Board.Add(target);
            service.State.Player.Health = 30;
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_155");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestTavernSpell(HastyExcavationCardId, "one", 2)
            };
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(28, service.State.Player.Health);
            Assert.AreEqual(1, trinkets.FelburnedLedgerBonusThisTurn);

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugBoardHealthSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(3, target.Attack);
            Assert.AreEqual(8, target.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;

            Assert.AreEqual(0, trinkets.FelburnedLedgerBonusThisTurn);

            var rewinder = TestShopMinion("BG26_174", 3, 1);
            service.State.Player.Board.Add(rewinder);
            var healthBeforePreventedDamage = service.State.Player.Health;
            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestTavernSpell(HastyExcavationCardId, "two", 2)
            };
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(healthBeforePreventedDamage, service.State.Player.Health);
            Assert.AreEqual(0, trinkets.FelburnedLedgerBonusThisTurn);

            service.Apply(new GameCommand(GameCommandType.DebugCastCard, DebugBoardHealthSpellCardId, CardKind.TavernSpell, -1));

            Assert.AreEqual(3, target.Attack);
            Assert.AreEqual(12, target.MaxHealth);
        }

        [Test]
        public void Batch3CostOverrides_UseSharedPurchaseEvaluation()
        {
            var pilgrimp = MatchService.CreateWithDefaultCatalog(12345);
            pilgrimp.State.Player.Health = 30;
            pilgrimp.State.Player.Tavern.Gold = 20;
            EquipTrinket(pilgrimp, "BG32_MagicItem_821");
            pilgrimp.State.Player.Tavern.Gold = 6;
            pilgrimp.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestTribeMinion("pilgrimp-demon-one", 1, 1, Tribe.Demon),
                TestTribeMinion("pilgrimp-demon-two", 1, 1, Tribe.Demon)
            };

            pilgrimp.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            pilgrimp.Apply(new GameCommand(GameCommandType.BuyMinion, 1));

            Assert.AreEqual(27, pilgrimp.State.Player.Health);
            Assert.AreEqual(3, pilgrimp.State.Player.Tavern.Gold);

            var bazaar = MatchService.CreateWithDefaultCatalog(12345);
            bazaar.State.Player.Health = 30;
            bazaar.State.Player.Tavern.Gold = 20;
            EquipTrinket(bazaar, "BG32_MagicItem_822");
            bazaar.State.Player.Tavern.Gold = 6;
            bazaar.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestTavernSpell("bazaar-spell-one", 3, "No-op test spell"),
                TestTavernSpell("bazaar-spell-two", 3, "No-op test spell")
            };

            bazaar.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            bazaar.Apply(new GameCommand(GameCommandType.BuyMinion, 1));

            Assert.AreEqual(27, bazaar.State.Player.Health);
            Assert.AreEqual(3, bazaar.State.Player.Tavern.Gold);

            var eye = MatchService.CreateWithDefaultCatalog(12345);
            eye.State.Player.Health = 30;
            eye.State.Player.Tavern.Gold = 20;
            EquipTrinket(eye, "BG30_MagicItem_701");
            eye.State.Player.Tavern.Gold = 20;
            eye.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestShopMinion("eye-buy-one", 1, 1),
                TestShopMinion("eye-buy-two", 1, 1),
                TestShopMinion("eye-buy-three", 1, 1),
                TestShopMinion("eye-buy-four", 1, 1)
            };

            eye.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            eye.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            eye.Apply(new GameCommand(GameCommandType.BuyMinion, 2));
            eye.Apply(new GameCommand(GameCommandType.BuyMinion, 3));

            Assert.AreEqual(27, eye.State.Player.Health);
            Assert.AreEqual(11, eye.State.Player.Tavern.Gold);

            var grifter = MatchService.CreateWithDefaultCatalog(12345);
            grifter.State.Player.Tavern.Gold = 20;
            EquipTrinket(grifter, "BG32_MagicItem_957");
            grifter.State.Player.Tavern.Gold = 6;
            grifter.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestTribeMinion("grifter-pirate-one", 1, 1, Tribe.Pirate),
                TestTribeMinion("grifter-pirate-two", 1, 1, Tribe.Pirate)
            };

            grifter.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            grifter.Apply(new GameCommand(GameCommandType.BuyMinion, 1));

            Assert.AreEqual(3, grifter.State.Player.Tavern.Gold);

            var electrode = MatchService.CreateWithDefaultCatalog(12345);
            electrode.State.Player.Tavern.Gold = 20;
            EquipTrinket(electrode, "BG35_MagicItem_743");
            electrode.State.Player.Tavern.Gold = 2;
            electrode.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestTribeMinion("electrode-magnetic", 1, 1, Tribe.Mech, Keyword.Magnetic)
            };

            electrode.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(0, electrode.State.Player.Tavern.Gold);
            Assert.AreEqual(2, electrode.State.Player.Tavern.Hand.Last().Counters["last_purchase_cost"]);
        }

        [Test]
        public void Batch3GoldSpentTriggers_ApplyEconomyRewards()
        {
            var scale = MatchService.CreateWithDefaultCatalog(12345);
            var scaled = TestShopMinion("scale-board", 3, 4);
            scale.State.Player.Board.Add(scaled);
            scale.State.Player.Tavern.Gold = 30;
            EquipTrinket(scale, "BG32_MagicItem_230");
            scale.State.Player.Tavern.Gold = 25;
            var scaleBuyOne = TestShopMinion("scale-buy-one", 1, 1);
            var scaleBuyTwo = TestShopMinion("scale-buy-two", 1, 1);
            scaleBuyOne.Cost = 10;
            scaleBuyTwo.Cost = 10;
            scale.State.Player.Tavern.Shop = new List<MinionInstance> { scaleBuyOne, scaleBuyTwo };

            scale.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            Assert.AreEqual(3, scaled.Attack);
            scale.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            Assert.AreEqual(6, scaled.Attack);

            var spellbook = MatchService.CreateWithDefaultCatalog(12345);
            var ringTarget = TestShopMinion("spellbook-board", 1, 1);
            spellbook.State.Player.Board.Add(ringTarget);
            spellbook.State.Player.Tavern.Gold = 20;
            EquipTrinket(spellbook, "BG30_MagicItem_999");
            spellbook.State.Player.Tavern.Gold = 10;
            var spellbookBuy = TestShopMinion("spellbook-buy", 1, 1);
            spellbookBuy.Cost = 7;
            spellbook.State.Player.Tavern.Shop = new List<MinionInstance> { spellbookBuy };

            spellbook.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(2, ringTarget.Attack);
            Assert.AreEqual(2, ringTarget.MaxHealth);

            var shark = MatchService.CreateWithDefaultCatalog(12345);
            var pirate = TestTribeMinion("shark-pirate", 2, 2, Tribe.Pirate);
            var beast = TestTribeMinion("shark-beast", 2, 2, Tribe.Beast);
            shark.State.Player.Board.Add(pirate);
            shark.State.Player.Board.Add(beast);
            shark.State.Player.Tavern.Gold = 30;
            EquipTrinket(shark, "BG32_MagicItem_232");
            shark.State.Player.Tavern.Gold = 25;
            var sharkBuyOne = TestShopMinion("shark-buy-one", 1, 1);
            var sharkBuyTwo = TestShopMinion("shark-buy-two", 1, 1);
            sharkBuyOne.Cost = 10;
            sharkBuyTwo.Cost = 10;
            shark.State.Player.Tavern.Shop = new List<MinionInstance> { sharkBuyOne, sharkBuyTwo };

            shark.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            Assert.AreEqual(3, pirate.Attack);
            Assert.AreEqual(2, beast.Attack);
            shark.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            Assert.AreEqual(5, pirate.Attack);
            Assert.AreEqual(2, beast.Attack);
        }

        [Test]
        public void Batch3RefreshDecorators_ApplyShopMutations()
        {
            var glowing = MatchService.CreateWithDefaultCatalog(12345);
            glowing.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestShopMinion("glowing-current", 2, 3)
            };
            glowing.State.Player.Tavern.Gold = 20;
            EquipTrinket(glowing, "BG30_MagicItem_841");

            Assert.GreaterOrEqual(glowing.State.Player.Tavern.Shop.Count(card => card != null), 7);
            Assert.AreEqual(5, glowing.State.Player.Tavern.Shop.First(card => card.CardId == "glowing-current").Attack);
            Assert.AreEqual(
                1,
                glowing.State.Player.Tavern.Shop.First(card => card.CardId == "glowing-current")
                    .Enchantments.Count(enchantment => enchantment.SourceId == "Trinket:Glowing Gauntlet"));

            var tierSix = MinionCatalogLoader.LoadFromResources().All.First(card =>
                card.InPool &&
                card.TavernTier == 6 &&
                card.PoolCount > 0 &&
                !card.CardId.StartsWith("BGDUO"));
            var guiding = CreateServiceWithEnabledMinions(12345, tierSix.CardId);
            guiding.State.Player.Tavern.Tier = 1;
            guiding.State.Player.Tavern.Gold = 20;
            EquipTrinket(guiding, "BG32_MagicItem_366");
            guiding.State.Player.Tavern.FreeRefreshes = 1;

            guiding.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.IsTrue(guiding.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .All(card => card.TavernTier == 6));

            var upstart = MatchService.CreateWithDefaultCatalog(12345);
            upstart.State.Player.Tavern.Tier = 6;
            upstart.State.Player.Tavern.Gold = 20;
            EquipTrinket(upstart, "BG35_MagicItem_862");
            upstart.State.Player.Tavern.FreeRefreshes = 1;

            upstart.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.IsTrue(upstart.State.Player.Tavern.Shop.Any(card =>
                card != null &&
                card.CardKind == CardKind.Minion &&
                card.Enchantments.Any(enchantment => enchantment.SourceId == "Upstart Embers")));

            var demonic = MatchService.CreateWithDefaultCatalog(12345);
            demonic.State.Player.Tavern.Gold = 20;
            EquipTrinket(demonic, "BG35_MagicItem_152");
            for (var refresh = 0; refresh < 4; refresh += 1)
            {
                demonic.State.Player.Tavern.FreeRefreshes = 1;
                demonic.Apply(new GameCommand(GameCommandType.RerollShop));
            }

            Assert.IsTrue(demonic.State.Player.Tavern.Shop.Any(card =>
                card?.Tags != null && card.Tags.Contains("trinket_demonic_tapestry_health_cost")));

            var murloc = MinionCatalogLoader.LoadFromResources().All.First(card =>
                card.InPool &&
                card.PoolCount > 0 &&
                card.Tribes != null &&
                card.Tribes.Contains(Tribe.Murloc) &&
                !card.CardId.StartsWith("BGDUO"));
            var finley = CreateServiceWithExactEnabledMinions(12345, murloc.CardId);
            finley.State.Player.Tavern.Tier = System.Math.Max(1, murloc.TavernTier);
            finley.State.Player.Tavern.Gold = 20;
            EquipTrinket(finley, "BG32_MagicItem_891");
            finley.State.Player.Tavern.FreeRefreshes = 1;

            finley.Apply(new GameCommand(GameCommandType.RerollShop));

            var refreshedMurlocs = finley.State.Player.Tavern.Shop
                .Where(card => card != null && BoardTribeAnalyzer.GetCountedTribes(card).Contains(Tribe.Murloc))
                .ToList();
            Assert.IsNotEmpty(refreshedMurlocs);
            Assert.IsTrue(refreshedMurlocs.All(card =>
                card.Enchantments.Any(enchantment => enchantment.SourceId == "Finley's Helmet")));
        }

        [Test]
        public void Batch3ProxyAndBuyTriggers_GenerateExpectedCards()
        {
            var battlecruiser = MatchService.CreateWithDefaultCatalog(12345);
            battlecruiser.State.Player.Tavern.Gold = 20;
            EquipTrinket(battlecruiser, "BG32_MagicItem_806");
            Assert.IsTrue(battlecruiser.State.Player.Tavern.Hand.Any(card => card.CardId == "TRINKET_BATTLECRUISER"));

            var grifter = MatchService.CreateWithDefaultCatalog(12345);
            grifter.State.Player.Tavern.Gold = 20;
            EquipTrinket(grifter, "BG32_MagicItem_957");
            Assert.IsTrue(grifter.State.Player.Tavern.Hand.Any(card => card.CardId == "TRINKET_DOUBLOON_GRIFTER"));

            var maw = MatchService.CreateWithDefaultCatalog(12345);
            maw.State.Player.Tavern.Gold = 20;
            EquipTrinket(maw, "BG32_MagicItem_205");
            Assert.IsTrue(maw.State.Player.Tavern.Hand.Any(card => card.CardId == "TRINKET_MAW_CASTER"));

            var safety = MatchService.CreateWithDefaultCatalog(12345);
            safety.State.Player.Tavern.Gold = 0;
            EquipTrinket(safety, "BG35_MagicItem_820");
            Assert.AreEqual(5, safety.State.Player.Tavern.Gold);

            var magicfin = MatchService.CreateWithDefaultCatalog(12345);
            magicfin.State.Player.Tavern.Gold = 20;
            EquipTrinket(magicfin, "BG35_MagicItem_750");
            magicfin.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestTavernSpell("magicfin-spell-one", 0, "No-op test spell"),
                TestTavernSpell("magicfin-spell-two", 0, "No-op test spell"),
                TestTavernSpell("magicfin-spell-three", 0, "No-op test spell")
            };

            magicfin.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            magicfin.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            magicfin.Apply(new GameCommand(GameCommandType.BuyMinion, 2));

            var taughtMurlocs = magicfin.State.Player.Tavern.Hand
                .Where(card => card.Tags != null && card.Tags.Contains("magicfin_taught_murloc"))
                .ToList();
            Assert.AreEqual(2, taughtMurlocs.Count);
            Assert.IsTrue(taughtMurlocs.All(card => BoardTribeAnalyzer.GetCountedTribes(card).Contains(Tribe.Murloc)));
            Assert.IsTrue(taughtMurlocs.All(card => card.Tags.Any(tag => tag.StartsWith("taught_tavern_spell:"))));

            var coinPouch = MatchService.CreateWithDefaultCatalog(12345);
            coinPouch.State.Player.Tavern.Gold = 0;
            coinPouch.State.Player.Tavern.Hand.Add(TestTavernSpell("TRINKET_COIN_POUCH_3", 0, "Gain 3 Gold."));

            coinPouch.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1));

            Assert.AreEqual(3, coinPouch.State.Player.Tavern.Gold);
        }

        [Test]
        public void ValorousMedallion_StartOfCombatBuffsCombatBoard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var definition = MinionCatalogLoader.LoadFromResources().All.First(minion => minion.InPool && minion.TavernTier == 1);
            var minion = MinionFactory.Create(definition, BoardSide.Player, "valorous-test");
            service.State.Player.Board.Add(minion);
            service.State.Player.Tavern.Gold = 10;

            QueueTrinketChoice(service, "BG30_MagicItem_970t");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 77, SafetyLimit = 1 }));

            var combatMinion = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == minion.InstanceId);
            Assert.AreEqual(minion.Attack + 6, combatMinion.Attack);
            Assert.AreEqual(minion.MaxHealth + 6, combatMinion.MaxHealth);
        }

        [Test]
        public void BronzeTimepiece_StartOfCombatAddsHalfAttackHealthToCombatClone()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var first = TestShopMinion("bronze-first", 8, 10);
            var second = TestShopMinion("bronze-second", 3, 5);
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_995");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            Assert.AreEqual(14, FinalCombatMinion(service, first).MaxHealth);
            Assert.AreEqual(6, FinalCombatMinion(service, second).MaxHealth);
            Assert.AreEqual(10, first.MaxHealth);
            Assert.AreEqual(5, second.MaxHealth);
        }

        [Test]
        public void TrainingCertificate_StartOfCombatDoublesTwoLowestAttackMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var lowest = TestShopMinion("training-lowest", 1, 4);
            var secondLowest = TestShopMinion("training-second", 2, 3);
            var highest = TestShopMinion("training-highest", 5, 6);
            service.State.Player.Board.Add(lowest);
            service.State.Player.Board.Add(secondLowest);
            service.State.Player.Board.Add(highest);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_962");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            Assert.AreEqual(2, FinalCombatMinion(service, lowest).Attack);
            Assert.AreEqual(8, FinalCombatMinion(service, lowest).MaxHealth);
            Assert.AreEqual(4, FinalCombatMinion(service, secondLowest).Attack);
            Assert.AreEqual(6, FinalCombatMinion(service, secondLowest).MaxHealth);
            Assert.AreEqual(5, FinalCombatMinion(service, highest).Attack);
            Assert.AreEqual(6, FinalCombatMinion(service, highest).MaxHealth);
            Assert.AreEqual(1, lowest.Attack);
            Assert.AreEqual(4, lowest.MaxHealth);
        }

        [Test]
        public void HandReferencedStartOfCombatTrinketsUseHandStats()
        {
            var dramalocService = MatchService.CreateWithDefaultCatalog(12345);
            var murloc = TestTribeMinion("dramaloc-murloc", 2, 3, Tribe.Murloc);
            var demon = TestTribeMinion("dramaloc-demon", 4, 4, Tribe.Demon);
            dramalocService.State.Player.Board.Add(murloc);
            dramalocService.State.Player.Board.Add(demon);
            dramalocService.State.Player.Tavern.Hand.Add(TestShopMinion("dramaloc-hand-small", 5, 2));
            dramalocService.State.Player.Tavern.Hand.Add(TestShopMinion("dramaloc-hand-big", 9, 1));
            dramalocService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(dramalocService, "BG35_MagicItem_754");
            dramalocService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(dramalocService);

            Assert.AreEqual(11, FinalCombatMinion(dramalocService, murloc).Attack);
            Assert.AreEqual(4, FinalCombatMinion(dramalocService, demon).Attack);

            var tinyfinService = MatchService.CreateWithDefaultCatalog(12345);
            var left = TestShopMinion("tinyfin-left", 2, 2);
            var right = TestShopMinion("tinyfin-right", 3, 3);
            tinyfinService.State.Player.Board.Add(left);
            tinyfinService.State.Player.Board.Add(right);
            tinyfinService.State.Player.Tavern.Hand.Add(TestShopMinion("tinyfin-high-attack", 9, 3));
            tinyfinService.State.Player.Tavern.Hand.Add(TestShopMinion("tinyfin-high-health", 5, 8));
            tinyfinService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(tinyfinService, "BG30_MagicItem_441");
            tinyfinService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(tinyfinService);

            Assert.AreEqual(7, FinalCombatMinion(tinyfinService, left).Attack);
            Assert.AreEqual(10, FinalCombatMinion(tinyfinService, left).MaxHealth);
            Assert.AreEqual(3, FinalCombatMinion(tinyfinService, right).Attack);
        }

        [Test]
        public void DragonAndTypelessStartOfCombatTrinketsApplyWarbandStats()
        {
            var emeraldService = MatchService.CreateWithDefaultCatalog(12345);
            var lowDragon = TestTribeMinion("emerald-low-dragon", 3, 5, Tribe.Dragon);
            var midDragon = TestTribeMinion("emerald-mid-dragon", 6, 6, Tribe.Dragon);
            var topAttack = TestShopMinion("emerald-top", 10, 4);
            emeraldService.State.Player.Board.Add(lowDragon);
            emeraldService.State.Player.Board.Add(midDragon);
            emeraldService.State.Player.Board.Add(topAttack);
            emeraldService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(emeraldService, "BG30_MagicItem_542");
            emeraldService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(emeraldService);

            Assert.AreEqual(10, FinalCombatMinion(emeraldService, lowDragon).Attack);
            Assert.AreEqual(10, FinalCombatMinion(emeraldService, midDragon).Attack);
            Assert.AreEqual(10, FinalCombatMinion(emeraldService, topAttack).Attack);

            var anvilService = MatchService.CreateWithDefaultCatalog(12345);
            var typeless = TestShopMinion("anvil-typeless", 3, 4);
            var murloc = TestTribeMinion("anvil-murloc", 2, 3, Tribe.Murloc);
            anvilService.State.Player.Board.Add(typeless);
            anvilService.State.Player.Board.Add(murloc);
            anvilService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(anvilService, "BG30_MagicItem_403");
            anvilService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(anvilService);

            Assert.AreEqual(9, FinalCombatMinion(anvilService, typeless).Attack);
            Assert.AreEqual(12, FinalCombatMinion(anvilService, typeless).MaxHealth);
            Assert.AreEqual(2, FinalCombatMinion(anvilService, murloc).Attack);
            Assert.AreEqual(3, FinalCombatMinion(anvilService, murloc).MaxHealth);
        }

        [Test]
        public void EdgeKeywordStartOfCombatTrinketsApplyToCombatClone()
        {
            var malletService = MatchService.CreateWithDefaultCatalog(12345);
            var left = TestShopMinion("mallet-left", 2, 2);
            var middle = TestShopMinion("mallet-middle", 3, 3);
            var right = TestShopMinion("mallet-right", 4, 4);
            malletService.State.Player.Board.Add(left);
            malletService.State.Player.Board.Add(middle);
            malletService.State.Player.Board.Add(right);
            malletService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(malletService, "BG30_MagicItem_902");
            malletService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(malletService);

            Assert.IsTrue(FinalCombatMinion(malletService, left).Keywords.Contains(Keyword.DivineShield));
            Assert.IsFalse(FinalCombatMinion(malletService, middle).Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(FinalCombatMinion(malletService, right).Keywords.Contains(Keyword.DivineShield));
            Assert.IsFalse(left.Keywords.Contains(Keyword.DivineShield));

            var incenseService = MatchService.CreateWithDefaultCatalog(12345);
            var undeadLeft = TestTribeMinion("incense-left", 2, 2, Tribe.Undead);
            var nonUndead = TestTribeMinion("incense-middle", 3, 3, Tribe.Beast);
            var undeadRight = TestTribeMinion("incense-right", 4, 4, Tribe.Undead);
            incenseService.State.Player.Board.Add(undeadLeft);
            incenseService.State.Player.Board.Add(nonUndead);
            incenseService.State.Player.Board.Add(undeadRight);
            incenseService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(incenseService, "BG32_MagicItem_360");
            incenseService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(incenseService);

            Assert.IsTrue(FinalCombatMinion(incenseService, undeadLeft).Keywords.Contains(Keyword.Reborn));
            Assert.IsFalse(FinalCombatMinion(incenseService, nonUndead).Keywords.Contains(Keyword.Reborn));
            Assert.IsTrue(FinalCombatMinion(incenseService, undeadRight).Keywords.Contains(Keyword.Reborn));
        }

        [Test]
        public void ProtectiveRing_GivesFourPiratesDivineShieldAtCombatStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            for (var index = 0; index < 5; index += 1)
            {
                service.State.Player.Board.Add(TestTribeMinion("ring-pirate-" + index, 2, 2, Tribe.Pirate));
            }

            var murloc = TestTribeMinion("ring-murloc", 2, 2, Tribe.Murloc);
            service.State.Player.Board.Add(murloc);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_711");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            var shieldedPirates = service.State.LastResult.FinalPlayerBoard
                .Where(card => card.Tribes.Contains(Tribe.Pirate) && card.Keywords.Contains(Keyword.DivineShield))
                .Count();
            Assert.AreEqual(4, shieldedPirates);
            Assert.IsFalse(FinalCombatMinion(service, murloc).Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void KarazhanChessSet_SummonsCombatOnlyCopyOfLeftmostMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var left = TestShopMinion("karazhan-left", 6, 7);
            service.State.Player.Board.Add(left);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_972");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            var copies = service.State.LastResult.FinalPlayerBoard
                .Where(card => card.CardId == left.CardId)
                .ToList();
            Assert.AreEqual(2, copies.Count);
            Assert.IsTrue(copies.Any(card => card.InstanceId != left.InstanceId));
            Assert.AreEqual(1, service.State.Player.Board.Count);
        }

        [Test]
        public void PortraitStartOfCombatTrinketsSummonAndGrantKeywords()
        {
            var automatonService = MatchService.CreateWithDefaultCatalog(12345);
            automatonService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(automatonService, "BG30_MagicItem_303");
            automatonService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(automatonService);

            Assert.IsTrue(automatonService.State.LastResult.FinalPlayerBoard.Any(card => card.CardId == "BG_TTN_401"));
            Assert.IsEmpty(automatonService.State.Player.Board);

            var eternalService = MatchService.CreateWithDefaultCatalog(12345);
            eternalService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(eternalService, "BG30_MagicItem_301");
            eternalService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var knight = eternalService.State.Player.Tavern.Hand.Single(card => card.CardId == "BG25_008");
            eternalService.State.Player.Tavern.Hand.Remove(knight);
            eternalService.State.Player.Board.Add(knight);
            RunStartOfCombat(eternalService);

            var combatKnight = FinalCombatMinion(eternalService, knight);
            Assert.IsTrue(combatKnight.Keywords.Contains(Keyword.Taunt));
            Assert.IsTrue(combatKnight.Keywords.Contains(Keyword.Reborn));
            Assert.IsFalse(knight.Keywords.Contains(Keyword.Taunt));
            Assert.IsFalse(knight.Keywords.Contains(Keyword.Reborn));
        }

        [Test]
        public void HogwashBasin_StartOfCombatPlaysThreeBloodGemsOnCombatClone()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var naga = TestTribeMinion("hogwash-naga", 2, 4, Tribe.Naga);
            service.State.Player.Board.Add(naga);
            service.State.Player.Tavern.BloodGemBonusAttack = 1;
            service.State.Player.Tavern.BloodGemBonusHealth = 2;
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_904");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            Assert.AreEqual(8, FinalCombatMinion(service, naga).Attack);
            Assert.AreEqual(13, FinalCombatMinion(service, naga).MaxHealth);
            Assert.AreEqual(2, naga.Attack);
            Assert.AreEqual(4, naga.MaxHealth);
        }

        [Test]
        public void RivendarePortrait_GivesTitusAndDoublesTitusHealthAtCombatStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_310");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var titus = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG25_354");
            service.State.Player.Tavern.Hand.Remove(titus);
            service.State.Player.Board.Add(titus);
            RunStartOfCombat(service);

            Assert.AreEqual(14, FinalCombatMinion(service, titus).MaxHealth);
            Assert.AreEqual(7, titus.MaxHealth);
        }

        [Test]
        public void CrochetedSungill_BuffsHighestHealthHandMinionAndSummonsCombatCopy()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var small = TestShopMinion("crocheted-small", 2, 3);
            var big = TestShopMinion("crocheted-big", 5, 8);
            service.State.Player.Tavern.Hand.Add(small);
            service.State.Player.Tavern.Hand.Add(big);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_960");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            Assert.AreEqual(9, big.Attack);
            Assert.AreEqual(12, big.MaxHealth);
            Assert.AreEqual(2, small.Attack);
            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Any(card =>
                card.CardId == big.CardId &&
                card.InstanceId != big.InstanceId &&
                card.Attack == 9 &&
                card.MaxHealth == 12));
        }

        [Test]
        public void EclecticShrine_StartOfCombatBuffsOneOfEachTypeAndImprovesPermanently()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var beast = TestTribeMinion("eclectic-beast", 2, 2, Tribe.Beast);
            var dragon = TestTribeMinion("eclectic-dragon", 3, 3, Tribe.Dragon);
            var typeless = TestShopMinion("eclectic-none", 4, 4);
            service.State.Player.Board.Add(beast);
            service.State.Player.Board.Add(dragon);
            service.State.Player.Board.Add(typeless);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_280");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            Assert.AreEqual(5, FinalCombatMinion(service, beast).Attack);
            Assert.AreEqual(4, FinalCombatMinion(service, beast).MaxHealth);
            Assert.AreEqual(6, FinalCombatMinion(service, dragon).Attack);
            Assert.AreEqual(5, FinalCombatMinion(service, dragon).MaxHealth);
            Assert.AreEqual(4, FinalCombatMinion(service, typeless).Attack);

            RunStartOfCombat(service);

            Assert.AreEqual(8, FinalCombatMinion(service, beast).Attack);
            Assert.AreEqual(6, FinalCombatMinion(service, beast).MaxHealth);
            Assert.AreEqual(9, FinalCombatMinion(service, dragon).Attack);
            Assert.AreEqual(7, FinalCombatMinion(service, dragon).MaxHealth);
        }

        [Test]
        public void VashjirAnemone_StartOfCombatBuffsNagaBySpellsCastThisGame()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var naga = TestTribeMinion("vashjir-naga", 3, 4, Tribe.Naga);
            var murloc = TestTribeMinion("vashjir-murloc", 2, 2, Tribe.Murloc);
            service.State.Player.Board.Add(naga);
            service.State.Player.Board.Add(murloc);
            service.State.Player.Tavern.TavernSpellsCastThisGame = 7;
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_932");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            Assert.AreEqual(6, FinalCombatMinion(service, naga).MaxHealth);
            Assert.AreEqual(2, FinalCombatMinion(service, murloc).MaxHealth);
        }

        [Test]
        public void YulonSticker_StartOfCombatMakesHighestTierDragonGoldenForCombatOnly()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var lowDragon = TestTribeMinion("yulon-low", 2, 3, Tribe.Dragon);
            var highDragon = TestTribeMinion("yulon-high", 5, 6, Tribe.Dragon);
            lowDragon.TavernTier = 2;
            highDragon.TavernTier = 5;
            service.State.Player.Board.Add(lowDragon);
            service.State.Player.Board.Add(highDragon);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_419");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            Assert.IsFalse(FinalCombatMinion(service, lowDragon).Golden);
            Assert.IsTrue(FinalCombatMinion(service, highDragon).Golden);
            Assert.AreEqual(10, FinalCombatMinion(service, highDragon).Attack);
            Assert.AreEqual(12, FinalCombatMinion(service, highDragon).MaxHealth);
            Assert.IsFalse(highDragon.Golden);
        }

        [Test]
        public void StegodonPortrait_StartOfCombatGivesTwoLeftmostBeastsDivineShieldForCombatOnly()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var firstBeast = TestTribeMinion("stegodon-first", 2, 2, Tribe.Beast);
            var nonBeast = TestTribeMinion("stegodon-naga", 3, 3, Tribe.Naga);
            var secondBeast = TestTribeMinion("stegodon-second", 4, 4, Tribe.Beast);
            var thirdBeast = TestTribeMinion("stegodon-third", 5, 5, Tribe.Beast);
            service.State.Player.Board.Add(firstBeast);
            service.State.Player.Board.Add(nonBeast);
            service.State.Player.Board.Add(secondBeast);
            service.State.Player.Board.Add(thirdBeast);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_702");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            Assert.IsTrue(FinalCombatMinion(service, firstBeast).Keywords.Contains(Keyword.DivineShield));
            Assert.IsFalse(FinalCombatMinion(service, nonBeast).Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(FinalCombatMinion(service, secondBeast).Keywords.Contains(Keyword.DivineShield));
            Assert.IsFalse(FinalCombatMinion(service, thirdBeast).Keywords.Contains(Keyword.DivineShield));
            Assert.IsFalse(firstBeast.Keywords.Contains(Keyword.DivineShield));
            Assert.IsFalse(secondBeast.Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void BassgillPortrait_GivesBassgillAndShieldsCombatSummonedMurloc()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var handMurloc = TestTribeMinion("bassgill-hand-murloc", 3, 9, Tribe.Murloc);
            service.State.Player.Tavern.Hand.Add(handMurloc);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_301");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var bassgill = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG26_350");
            service.State.Player.Tavern.Hand.Remove(bassgill);
            service.State.Player.Board.Add(bassgill);
            RunAvengeCombat(service, 10, 100, 1);

            var summonedMurloc = service.State.LastResult.FinalPlayerBoard
                .FirstOrDefault(card => card.CardId == handMurloc.CardId);
            Assert.IsNotNull(summonedMurloc);
            Assert.IsTrue(summonedMurloc.Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void MamaBearAndSlammaSticker_BuffCombatSummonedBeasts()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_871");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            QueueTrinketChoice(service, "BG30_MagicItem_540");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.NextCombatBeetles = 1;
            RunStartOfCombat(service);

            var beetle = service.State.LastResult.FinalPlayerBoard.Single(card => card.Name == "Beetle");
            Assert.AreEqual(14, beetle.Attack);
            Assert.AreEqual(7, beetle.MaxHealth);
        }

        [Test]
        public void ReinforcedShield_GivesFirstFiveCombatSummonsDivineShield()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_886");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.NextCombatBeetles = 6;
            RunStartOfCombat(service);

            var beetles = service.State.LastResult.FinalPlayerBoard
                .Where(card => card.Name == "Beetle")
                .ToList();
            Assert.AreEqual(6, beetles.Count);
            Assert.AreEqual(5, beetles.Count(card => card.Keywords.Contains(Keyword.DivineShield)));
        }

        [Test]
        public void TwinSkyLanterns_CopiesOnlyFirstCombatSummon()
        {
            var oneCopyService = MatchService.CreateWithDefaultCatalog(12345);
            oneCopyService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(oneCopyService, "BG30_MagicItem_822");
            oneCopyService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            oneCopyService.State.Player.Tavern.NextCombatBeetles = 1;
            RunStartOfCombat(oneCopyService);

            Assert.AreEqual(2, oneCopyService.State.LastResult.FinalPlayerBoard.Count(card => card.Name == "Beetle"));

            var twoCopyService = MatchService.CreateWithDefaultCatalog(12345);
            twoCopyService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(twoCopyService, "BG30_MagicItem_822t2");
            twoCopyService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            twoCopyService.State.Player.Tavern.NextCombatBeetles = 1;
            RunStartOfCombat(twoCopyService);

            Assert.AreEqual(3, twoCopyService.State.LastResult.FinalPlayerBoard.Count(card => card.Name == "Beetle"));
        }

        [Test]
        public void CeremonialSword_BuffsFriendlyAttackerBeforeDamage()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var attacker = TestShopMinion("ceremonial-attacker", 2, 10);
            service.State.Player.Board.Add(attacker);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_925");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 5, 100, 1);

            var combatAttacker = FinalCombatMinion(service, attacker);
            Assert.AreEqual(6, combatAttacker.Attack);
            Assert.AreEqual(5, combatAttacker.Health);
            Assert.AreEqual(2, attacker.Attack);
        }

        [Test]
        public void FaerieDragonScale_ShieldsFirstThreeFriendlyDragonAttacks()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var dragons = new List<MinionInstance>();
            for (var index = 0; index < 4; index += 1)
            {
                var dragon = TestTribeMinion("faerie-dragon-" + index, 1, 20, Tribe.Dragon);
                dragons.Add(dragon);
                service.State.Player.Board.Add(dragon);
            }

            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_363");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 0, 100, 7);

            Assert.IsTrue(FinalCombatMinion(service, dragons[0]).Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(FinalCombatMinion(service, dragons[1]).Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(FinalCombatMinion(service, dragons[2]).Keywords.Contains(Keyword.DivineShield));
            Assert.IsFalse(FinalCombatMinion(service, dragons[3]).Keywords.Contains(Keyword.DivineShield));
            Assert.IsFalse(dragons[0].Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void AllPurposeKibble_BuffsFriendlyBeastAttacksAndImprovesPermanently()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var beast = TestTribeMinion("all-purpose-kibble-beast", 2, 20, Tribe.Beast);
            var neutral = TestShopMinion("all-purpose-kibble-neutral", 5, 20);
            service.State.Player.Board.Add(beast);
            service.State.Player.Board.Add(neutral);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_200");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 1, 100, 1);

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(4, FinalCombatMinion(service, beast).Attack);
            Assert.AreEqual(2, beast.Attack);
            Assert.AreEqual(3, trinkets.AllPurposeKibbleAttack);
            Assert.AreEqual(
                1,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.ImproveAllPurposeKibble)
                    .Sum(reward => reward.Amount));

            RunAvengeCombat(service, 1, 100, 1);
            trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;

            Assert.AreEqual(5, FinalCombatMinion(service, beast).Attack);
            Assert.AreEqual(4, trinkets.AllPurposeKibbleAttack);
        }

        [Test]
        public void AllianceKeychain_BuffsOneOrTwoFriendlyMinionsOnFirstFriendlyDeath()
        {
            var lesserService = MatchService.CreateWithDefaultCatalog(12345);
            var lesserVictim = TestShopMinion("alliance-lesser-victim", 3, 4);
            var lesserSurvivor = TestShopMinion("alliance-lesser-survivor", 10, 20);
            lesserService.State.Player.Board.Add(lesserVictim);
            lesserService.State.Player.Board.Add(lesserSurvivor);
            lesserService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(lesserService, "BG30_MagicItem_433");
            lesserService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(lesserService, 5, 100, 1);

            var lesserCombatSurvivor = FinalCombatMinion(lesserService, lesserSurvivor);
            Assert.AreEqual(13, lesserCombatSurvivor.Attack);
            Assert.AreEqual(24, lesserCombatSurvivor.MaxHealth);
            Assert.AreEqual(10, lesserSurvivor.Attack);
            Assert.AreEqual(20, lesserSurvivor.MaxHealth);

            var greaterService = MatchService.CreateWithDefaultCatalog(12345);
            var greaterVictim = TestShopMinion("alliance-greater-victim", 2, 5);
            var firstSurvivor = TestShopMinion("alliance-greater-survivor-one", 10, 20);
            var secondSurvivor = TestShopMinion("alliance-greater-survivor-two", 30, 40);
            greaterService.State.Player.Board.Add(greaterVictim);
            greaterService.State.Player.Board.Add(firstSurvivor);
            greaterService.State.Player.Board.Add(secondSurvivor);
            greaterService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(greaterService, "BG30_MagicItem_433t");
            greaterService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(greaterService, 5, 100, 1);

            Assert.AreEqual(12, FinalCombatMinion(greaterService, firstSurvivor).Attack);
            Assert.AreEqual(25, FinalCombatMinion(greaterService, firstSurvivor).MaxHealth);
            Assert.AreEqual(32, FinalCombatMinion(greaterService, secondSurvivor).Attack);
            Assert.AreEqual(45, FinalCombatMinion(greaterService, secondSurvivor).MaxHealth);
        }

        [Test]
        public void DeathlyPhylactery_DiscoversAndOnlyFirstDeathrattleTriggersExtra()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_700");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(card => card.Keywords.Contains(Keyword.Deathrattle)));

            QueueTrinketChoice(service, "BG32_MagicItem_306");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var first = TestTribeMinion("BG33_894", 1, 5, Tribe.Murloc, Keyword.Deathrattle);
            first.InstanceId = "test-coldlight-one";
            var second = TestTribeMinion("BG33_894", 1, 5, Tribe.Murloc, Keyword.Deathrattle);
            second.InstanceId = "test-coldlight-two";
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);

            RunStartOfCombat(service);

            var shellRewards = service.State.LastResult.PlayerRewards
                .Where(reward => reward.Type == CombatRewardType.AddTavernSpellToHand && reward.SourceCardId == "BG33_894")
                .ToList();
            Assert.AreEqual(3, shellRewards.Sum(reward => reward.Amount));
            CollectionAssert.AreEqual(
                new[] { 2, 1 },
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.FriendlyDeathrattleTriggered && reward.SourceCardId == "BG33_894")
                    .Select(reward => reward.Amount)
                    .ToList());
        }

        [Test]
        public void DeathlyPhylactery_GoldenTitusStacksDuringActualCombatDeath()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_700");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var coldlight = TestTribeMinion("BG33_894", 1, 1, Tribe.Murloc, Keyword.Taunt, Keyword.Deathrattle);
            coldlight.InstanceId = "actual-combat-coldlight";
            var goldenTitus = TestShopMinion("BG25_354", 0, 30);
            goldenTitus.InstanceId = "actual-combat-golden-titus";
            goldenTitus.Golden = true;
            service.State.Player.Board.Add(coldlight);
            service.State.Player.Board.Add(goldenTitus);

            RunAvengeCombat(service, 5, 100, 1);

            Assert.AreEqual(
                4,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.FriendlyDeathrattleTriggered && reward.SourceInstanceId == coldlight.InstanceId)
                    .Sum(reward => reward.Amount));
            Assert.AreEqual(
                4,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.AddTavernSpellToHand && reward.SourceCardId == "BG33_894")
                    .Sum(reward => reward.Amount));
        }

        [Test]
        public void HeraldSticker_DeathlyPhylacteryAndGoldenTitusRepeatOnlyFirstExtraDeathrattle()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_700");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            QueueTrinketChoice(service, "BG32_MagicItem_306");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var first = TestTribeMinion("BG33_894", 1, 5, Tribe.Murloc, Keyword.Deathrattle);
            first.InstanceId = "herald-golden-titus-first";
            var second = TestTribeMinion("BG33_894", 1, 5, Tribe.Murloc, Keyword.Deathrattle);
            second.InstanceId = "herald-golden-titus-second";
            var goldenTitus = TestShopMinion("BG25_354", 0, 30);
            goldenTitus.InstanceId = "herald-golden-titus";
            goldenTitus.Golden = true;
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);
            service.State.Player.Board.Add(goldenTitus);

            RunStartOfCombat(service);

            var deathrattleAmounts = service.State.LastResult.PlayerRewards
                .Where(reward => reward.Type == CombatRewardType.FriendlyDeathrattleTriggered && reward.SourceCardId == "BG33_894")
                .ToDictionary(reward => reward.SourceInstanceId, reward => reward.Amount);
            Assert.AreEqual(4, deathrattleAmounts[first.InstanceId]);
            Assert.AreEqual(3, deathrattleAmounts[second.InstanceId]);
            Assert.AreEqual(
                7,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.AddTavernSpellToHand && reward.SourceCardId == "BG33_894")
                    .Sum(reward => reward.Amount));
        }

        [Test]
        public void HeraldSticker_StartOfCombatTriggersDeathrattlesBeforeMinionStartAuras()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var manasaber = TestTribeMinion("BG26_800", 1, 4, Tribe.Beast, Keyword.Deathrattle);
            var hummingBird = TestTribeMinion("BG26_805", 0, 4, Tribe.Beast);
            service.State.Player.Board.Add(manasaber);
            service.State.Player.Board.Add(hummingBird);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_306");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            var cublings = service.State.LastResult.FinalPlayerBoard
                .Where(card => card.Name == "Cubling")
                .ToList();
            Assert.AreEqual(2, cublings.Count);
            Assert.IsTrue(cublings.All(card => card.Attack == 1));
        }

        [Test]
        public void RylakPortrait_GivesRylakAndTriggersOnlyRylakDeathrattlesAtCombatStart()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_834");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var rylak = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG26_801");
            service.State.Player.Tavern.Hand.Remove(rylak);
            service.State.Player.Board.Add(rylak);
            service.State.Player.Board.Add(TestTribeMinion("BG34_523", 2, 2, Tribe.Beast, Keyword.Battlecry));
            service.State.Player.Board.Add(TestTribeMinion("BG33_894", 1, 5, Tribe.Murloc, Keyword.Deathrattle));

            RunStartOfCombat(service);

            Assert.AreEqual(
                1,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.AddRandomBeastToHand && reward.SourceCardId == "BG34_523")
                    .Sum(reward => reward.Amount));
            Assert.IsFalse(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.AddTavernSpellToHand &&
                reward.SourceCardId == "BG33_894"));
        }

        [Test]
        public void RylakPortrait_GoldenTitusRepeatsOnlyRylakStartOfCombatDeathrattle()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_834");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var rylak = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG26_801");
            service.State.Player.Tavern.Hand.Remove(rylak);
            service.State.Player.Board.Add(rylak);
            service.State.Player.Board.Add(TestTribeMinion("BG34_523", 2, 2, Tribe.Beast, Keyword.Battlecry));
            service.State.Player.Board.Add(TestTribeMinion("BG33_894", 1, 5, Tribe.Murloc, Keyword.Deathrattle));
            var goldenTitus = TestShopMinion("BG25_354", 0, 30);
            goldenTitus.InstanceId = "rylak-golden-titus";
            goldenTitus.Golden = true;
            service.State.Player.Board.Add(goldenTitus);

            RunStartOfCombat(service);

            Assert.AreEqual(
                3,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.FriendlyDeathrattleTriggered && reward.SourceCardId == "BG26_801")
                    .Sum(reward => reward.Amount));
            Assert.AreEqual(
                3,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.AddRandomBeastToHand && reward.SourceCardId == "BG34_523")
                    .Sum(reward => reward.Amount));
            Assert.IsFalse(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.AddTavernSpellToHand &&
                reward.SourceCardId == "BG33_894"));
        }

        [Test]
        public void RylakPortrait_TriggeredBattlecryPaysUndeadAttackReward()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_834");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            var rylak = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG26_801");
            service.State.Player.Tavern.Hand.Remove(rylak);
            service.State.Player.Board.Add(rylak);
            service.State.Player.Board.Add(TestTribeMinion(NerubianDeathswarmerCardId, 1, 20, Tribe.Undead, Keyword.Battlecry));

            RunStartOfCombat(service);

            Assert.AreEqual(1, service.State.Player.Tavern.UndeadAttackBonus);
            Assert.IsTrue(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.ImproveUndeadAttack &&
                reward.SourceCardId == NerubianDeathswarmerCardId &&
                reward.Amount == 1));
        }

        [Test]
        public void DivineSignet_QueuesOnlyFirstFourRandomTavernSpells()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            for (var index = 0; index < 5; index += 1)
            {
                service.State.Player.Board.Add(TestShopMinion("divine-signet-" + index, 1, 20, Keyword.DivineShield));
            }

            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_171");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 1, 100, 7);

            Assert.AreEqual(
                4,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.AddRandomTavernSpellToHand && reward.SourceCardId == "BG32_MagicItem_171")
                    .Sum(reward => reward.Amount));
        }

        [Test]
        public void MechagonAdapter_RestoresOnlyFriendlyMechDivineShieldThreeTimes()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var mech = TestTribeMinion("mechagon-mech", 1, 20, Tribe.Mech, Keyword.DivineShield);
            service.State.Player.Board.Add(mech);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_910");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 1, 100, 4);

            Assert.AreEqual(
                3,
                service.State.LastResult.Replay.Frames.Count(frame => frame.LogText.Contains("Mechagon Adapter restored Divine Shield")));
            Assert.IsFalse(FinalCombatMinion(service, mech).Keywords.Contains(Keyword.DivineShield));

            var nonMechService = MatchService.CreateWithDefaultCatalog(12345);
            var naga = TestTribeMinion("mechagon-naga", 1, 20, Tribe.Naga, Keyword.DivineShield);
            nonMechService.State.Player.Board.Add(naga);
            nonMechService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(nonMechService, "BG30_MagicItem_910");
            nonMechService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(nonMechService, 1, 100, 1);

            Assert.IsFalse(FinalCombatMinion(nonMechService, naga).Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(
                0,
                nonMechService.State.LastResult.Replay.Frames.Count(frame => frame.LogText.Contains("Mechagon Adapter restored Divine Shield")));
        }

        [Test]
        public void LuckyTabby_AddsRandomBeastAfterSevenFriendlyDeaths()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_931");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Hand.Clear();
            for (var index = 0; index < 7; index += 1)
            {
                service.State.Player.Board.Add(TestShopMinion("lucky-tabby-victim-" + index, 1, 1));
            }

            RunAvengeCombat(service, 1, 100, 20);

            Assert.AreEqual(0, service.State.Player.Tavern.AdvancedMechanics.Trinkets.LuckyTabbyDeaths);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card =>
                card.CardKind == CardKind.Minion &&
                BoardTribeAnalyzer.GetCountedTribes(card).Contains(Tribe.Beast)));
        }

        [Test]
        public void BleedingHeart_AddsRandomUndeadAfterEightFriendlyDeaths()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_713");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Hand.Clear();
            for (var index = 0; index < 4; index += 1)
            {
                service.State.Player.Board.Add(TestShopMinion("bleeding-heart-victim-" + index, 1, 1, Keyword.Reborn));
            }

            RunAvengeCombat(service, 1, 100, 30);

            Assert.AreEqual(0, service.State.Player.Tavern.AdvancedMechanics.Trinkets.BleedingHeartDeaths);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card =>
                card.CardKind == CardKind.Minion &&
                BoardTribeAnalyzer.GetCountedTribes(card).Contains(Tribe.Undead)));
        }

        [Test]
        public void StormcoilSticker_AddsRandomMechAfterEightFriendlyDeaths()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_302");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Hand.Clear();
            for (var index = 0; index < 4; index += 1)
            {
                service.State.Player.Board.Add(TestShopMinion("stormcoil-victim-" + index, 1, 1, Keyword.Reborn));
            }

            RunAvengeCombat(service, 1, 100, 30);

            Assert.AreEqual(0, service.State.Player.Tavern.AdvancedMechanics.Trinkets.StormcoilStickerDeaths);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card =>
                card.CardKind == CardKind.Minion &&
                BoardTribeAnalyzer.GetCountedTribes(card).Contains(Tribe.Mech)));
        }

        [Test]
        public void BoomController_SummonsExactCopyOfFirstFriendlyMechDeathOncePerCombat()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var mech = TestTribeMinion("boom-controller-first-mech", 3, 1, Tribe.Mech);
            mech.Attack = 5;
            mech.MaxHealth = 4;
            mech.Health = 1;
            mech.Enchantments.Add(new Enchantment
            {
                Id = "test-boom-controller-buff",
                SourceId = "test-boom-controller-buff",
                AttackBonus = 2,
                HealthBonus = 3
            });
            service.State.Player.Board.Add(mech);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_440");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 100, 100, 6);

            var frames = service.State.LastResult.Replay.Frames
                .Where(frame => frame.LogText.Contains("Boom Controller summoned exact copy"))
                .ToList();
            Assert.AreEqual(1, frames.Count);
            var copy = frames[0].PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId.StartsWith("boom-controller-"));
            Assert.AreEqual(mech.CardId, copy.CardId);
            Assert.AreEqual(5, copy.Attack);
            Assert.AreEqual(4, copy.MaxHealth);
            Assert.AreEqual(4, copy.Health);
        }

        [Test]
        public void BloodGolemSticker_SummonsGolemWithDeadQuilboarsBloodGemStats()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var quilboar = TestTribeMinion("blood-golem-quilboar", 1, 1, Tribe.Quilboar);
            quilboar.Attack = 3;
            quilboar.MaxHealth = 4;
            quilboar.Health = 1;
            quilboar.Enchantments.Add(new Enchantment
            {
                Id = "Blood Gem",
                SourceId = "Blood Gem",
                AttackBonus = 2,
                HealthBonus = 3
            });
            service.State.Player.Board.Add(quilboar);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_442");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 100, 100, 4);

            var frame = service.State.LastResult.Replay.Frames.First(item => item.LogText.Contains("summoned Blood Golem"));
            var golem = frame.PlayerBoardSnapshot.Minions.Single(minion => minion.Name == "Blood Golem");
            Assert.AreEqual(2, golem.Attack);
            Assert.AreEqual(3, golem.MaxHealth);
            Assert.AreEqual(3, golem.Health);
        }

        [Test]
        public void BloodAmulet_PlaysPermanentBloodGemsAfterFriendlyDeathrattle()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = TestShopMinion("blood-amulet-source", 1, 1, Keyword.Deathrattle);
            var first = TestShopMinion("blood-amulet-first", 10, 20);
            var second = TestShopMinion("blood-amulet-second", 20, 30);
            var third = TestShopMinion("blood-amulet-third", 30, 40);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);
            service.State.Player.Board.Add(third);
            service.State.Player.Tavern.BloodGemBonusAttack = 2;
            service.State.Player.Tavern.BloodGemBonusHealth = 1;
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_432");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 5, 100, 1);

            Assert.AreEqual(13, first.Attack);
            Assert.AreEqual(22, first.MaxHealth);
            Assert.AreEqual(23, second.Attack);
            Assert.AreEqual(32, second.MaxHealth);
            Assert.AreEqual(33, third.Attack);
            Assert.AreEqual(42, third.MaxHealth);
            Assert.AreEqual(
                3,
                service.State.LastResult.PlayerRewards.Count(reward =>
                    reward.Type == CombatRewardType.BuffOriginalFriendlyMinion &&
                    reward.SourceCardId == "BG35_MagicItem_432"));
        }

        [Test]
        public void AggemSticker_EndTurnPlaysSevenBloodGemsOnOneFriendlyOfEachType()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var beast = TestTribeMinion("aggem-beast", 2, 20, Tribe.Beast);
            var demon = TestTribeMinion("aggem-demon", 3, 30, Tribe.Demon);
            var neutral = TestShopMinion("aggem-neutral", 5, 50);
            service.State.Player.Board.Add(beast);
            service.State.Player.Board.Add(demon);
            service.State.Player.Board.Add(neutral);
            service.State.Player.Tavern.BloodGemBonusAttack = 1;
            service.State.Player.Tavern.BloodGemBonusHealth = 2;
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_284");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(16, beast.Attack);
            Assert.AreEqual(41, beast.MaxHealth);
            Assert.AreEqual(17, demon.Attack);
            Assert.AreEqual(51, demon.MaxHealth);
            Assert.AreEqual(5, neutral.Attack);
            Assert.AreEqual(50, neutral.MaxHealth);
            Assert.IsTrue(beast.Enchantments.Any(enchantment => enchantment.SourceId.Contains("Blood Gem")));
            Assert.IsTrue(demon.Enchantments.Any(enchantment => enchantment.SourceId.Contains("Blood Gem")));
        }

        [Test]
        public void RedeemerPortrait_AddsNalaaAndImprovesNalaaTavernSpellBuff()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_944");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "BG28_551"));

            var nalaa = TestShopMinion("BG28_551", 5, 6);
            var beast = TestTribeMinion("redeemer-beast", 2, 20, Tribe.Beast);
            var demon = TestTribeMinion("redeemer-demon", 3, 30, Tribe.Demon);
            service.State.Player.Board.Add(nalaa);
            service.State.Player.Board.Add(beast);
            service.State.Player.Board.Add(demon);

            AddBloodGemSpellToHand(service, "redeemer");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 1));

            Assert.AreEqual(11, beast.Attack);
            Assert.AreEqual(28, beast.MaxHealth);
            Assert.AreEqual(11, demon.Attack);
            Assert.AreEqual(37, demon.MaxHealth);
        }

        [Test]
        public void WildfeatherDuster_AddsRandomBeastAfterSixBeastSummons()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_700");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            service.State.Player.Tavern.Hand.Clear();
            for (var index = 0; index < 3; index += 1)
            {
                service.State.Player.Board.Add(TestTribeMinion("BG26_800", 1, 1, Tribe.Beast, Keyword.Deathrattle));
                service.State.Player.Board.Last().InstanceId = "test-wildfeather-manasaber-" + index;
            }

            RunAvengeCombat(service, 2, 100, 30);

            Assert.AreEqual(0, service.State.Player.Tavern.AdvancedMechanics.Trinkets.WildfeatherDusterBeastSummons);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card =>
                card.CardKind == CardKind.Minion &&
                BoardTribeAnalyzer.GetCountedTribes(card).Contains(Tribe.Beast)));
        }

        [Test]
        public void FangAnklet_BuffsBeastsAtCombatStartAndImprovesAfterBeastSummons()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var beast = TestTribeMinion("fang-anklet-beast", 2, 20, Tribe.Beast);
            var neutral = TestShopMinion("fang-anklet-neutral", 5, 20);
            service.State.Player.Board.Add(beast);
            service.State.Player.Board.Add(neutral);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_701");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            Assert.AreEqual(3, FinalCombatMinion(service, beast).Attack);
            Assert.AreEqual(21, FinalCombatMinion(service, beast).MaxHealth);
            Assert.AreEqual(5, FinalCombatMinion(service, neutral).Attack);
            Assert.AreEqual(20, FinalCombatMinion(service, neutral).MaxHealth);
            Assert.AreEqual(2, beast.Attack);
            Assert.AreEqual(20, beast.MaxHealth);

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(1, trinkets.FangAnkletBonusAttack);
            Assert.AreEqual(1, trinkets.FangAnkletBonusHealth);

            service.State.Player.Board.Clear();
            var manasaber = TestTribeMinion("BG26_800", 1, 1, Tribe.Beast, Keyword.Deathrattle);
            manasaber.InstanceId = "test-fang-anklet-manasaber";
            service.State.Player.Board.Add(manasaber);
            RunAvengeCombat(service, 2, 100, 10);
            trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;

            Assert.AreEqual(3, trinkets.FangAnkletBonusAttack);
            Assert.AreEqual(3, trinkets.FangAnkletBonusHealth);

            service.State.Player.Board.Clear();
            var nextBeast = TestTribeMinion("fang-anklet-next-beast", 2, 20, Tribe.Beast);
            service.State.Player.Board.Add(nextBeast);
            RunStartOfCombat(service);

            Assert.AreEqual(5, FinalCombatMinion(service, nextBeast).Attack);
            Assert.AreEqual(23, FinalCombatMinion(service, nextBeast).MaxHealth);
        }

        [Test]
        public void BeatboxerPortrait_MagnetizesDifferentMechAndBeatboxer()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG35_MagicItem_741");

            var beatboxer = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG26_149");
            service.State.Player.Tavern.Hand.Remove(beatboxer);
            service.State.Player.Board.Add(beatboxer);
            var target = TestTribeMinion("beatboxer-target", 1, 1, Tribe.Mech);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Hand.Add(
                TestTribeMinion("beatboxer-magnetic", 2, 3, Tribe.Mech, Keyword.Magnetic, Keyword.DivineShield));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));

            Assert.AreEqual(3, target.Attack);
            Assert.AreEqual(4, target.MaxHealth);
            Assert.IsTrue(target.Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(7, beatboxer.Attack);
            Assert.AreEqual(13, beatboxer.MaxHealth);
            Assert.IsTrue(beatboxer.Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void FishySticker_SummonsGoldenFishAndTriggersCopiedDeathrattles()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var bonehead = TestTribeMinion("BG28_300", 1, 1, Tribe.Undead, Keyword.Deathrattle);
            bonehead.InstanceId = "test-fishy-bonehead";
            service.State.Player.Board.Add(bonehead);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_821t2");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 100, 100, 30);

            Assert.IsTrue(service.State.LastResult.Replay.Frames.Any(frame =>
                frame.LogText.Contains("Fishy Sticker summoned Golden Fish of N'Zoth")));
            Assert.GreaterOrEqual(
                service.State.LastResult.Replay.Frames.Count(frame =>
                    frame.EventType == CombatEventType.MinionSummoned &&
                    frame.LogText.Contains("summoned Skeleton")),
                6);
        }

        [Test]
        public void FishPortrait_FishGainsAndTriggersFriendlyDeathrattle()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG30_MagicItem_821");

            var bonehead = TestTribeMinion("BG28_300", 1, 1, Tribe.Undead, Keyword.Taunt, Keyword.Deathrattle);
            bonehead.InstanceId = "test-fish-portrait-bonehead";
            service.State.Player.Board.Add(bonehead);
            var fish = service.State.Player.Tavern.Hand.Single(card => card.CardId == "TB_BaconShop_HP_105t");
            service.State.Player.Tavern.Hand.Remove(fish);
            fish.Health = 150;
            fish.MaxHealth = 150;
            service.State.Player.Board.Add(fish);

            RunAvengeCombat(service, 100, 1000, 30);

            Assert.AreEqual(
                4,
                service.State.LastResult.Replay.Frames.Count(frame =>
                    frame.EventType == CombatEventType.MinionSummoned &&
                    frame.LogText.Contains("summoned Skeleton")),
                "The original Bonehead and Fish of N'Zoth should each summon two Skeletons.");
        }

        [Test]
        public void SoulFermenter_DestroysLeftmostThreeAndResummonsAfterLastMinionDies()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Board.Add(TestShopMinion("soul-one", 2, 3));
            service.State.Player.Board.Add(TestShopMinion("soul-two", 4, 5));
            service.State.Player.Board.Add(TestShopMinion("soul-three", 6, 7));
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_732");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            var finalBoard = service.State.LastResult.FinalPlayerBoard;
            Assert.AreEqual(3, finalBoard.Count);
            Assert.IsTrue(finalBoard.All(minion => minion.InstanceId.StartsWith("soul-fermenter-resummon-")));
            CollectionAssert.AreEqual(
                new[] { "soul-one", "soul-two", "soul-three" },
                finalBoard.Select(minion => minion.CardId).ToArray());
            Assert.AreEqual(
                3,
                service.State.LastResult.PlayerRewards
                    .Where(reward => reward.Type == CombatRewardType.FriendlyMinionDied)
                    .Sum(reward => reward.Amount));
        }

        [Test]
        public void STharaSticker_ResummonsFirstDeadDemonAfterLastFriendlyDies()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var firstDemon = TestTribeMinion("sthara-first-demon", 1, 1, Tribe.Demon);
            firstDemon.Attack = 9;
            firstDemon.MaxHealth = 8;
            firstDemon.Health = 1;
            firstDemon.Enchantments.Add(new Enchantment
            {
                Id = "test-sthara-buff",
                SourceId = "test-sthara-buff",
                AttackBonus = 8,
                HealthBonus = 7
            });
            var secondDemon = TestTribeMinion("sthara-second-demon", 3, 1, Tribe.Demon);
            service.State.Player.Board.Add(firstDemon);
            service.State.Player.Board.Add(secondDemon);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_907");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 100, 100, 2);

            var finalBoard = service.State.LastResult.FinalPlayerBoard;
            Assert.AreEqual(1, finalBoard.Count);
            var copy = finalBoard.Single();
            Assert.IsTrue(copy.InstanceId.StartsWith("sthara-resummon-"));
            Assert.AreEqual(firstDemon.CardId, copy.CardId);
            Assert.AreEqual(9, copy.Attack);
            Assert.AreEqual(8, copy.MaxHealth);
            Assert.AreEqual(8, copy.Health);
            Assert.AreEqual(
                1,
                service.State.LastResult.Replay.Frames.Count(frame =>
                    frame.LogText.Contains("S'Thara Sticker resummoned first dead Demon")));
        }

        [Test]
        public void BelcherPortrait_AddsBelcherAndPermanentlyBuffsWhenVenomousIsLost()
        {
            var lesserService = MatchService.CreateWithDefaultCatalog(12345);
            var lesserTarget = TestTribeMinion("belcher-lesser-target", 2, 20, Tribe.Murloc, Keyword.Venomous);
            lesserService.State.Player.Board.Add(lesserTarget);
            lesserService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(lesserService, "BG30_MagicItem_432");
            lesserService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            Assert.IsTrue(lesserService.State.Player.Tavern.Hand.Any(card => card.CardId == "BG33_318"));
            RunAvengeCombat(lesserService, 1, 100, 1);

            Assert.AreEqual(6, lesserTarget.Attack);
            Assert.AreEqual(24, lesserTarget.MaxHealth);
            Assert.AreEqual(
                "BG30_MagicItem_432",
                lesserService.State.LastResult.PlayerRewards.Single(reward => reward.Type == CombatRewardType.BuffOriginalFriendlyMinion).SourceCardId);

            var greaterService = MatchService.CreateWithDefaultCatalog(12345);
            var greaterTarget = TestTribeMinion("belcher-greater-target", 2, 20, Tribe.Murloc, Keyword.Venomous);
            greaterService.State.Player.Board.Add(greaterTarget);
            greaterService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(greaterService, "BG30_MagicItem_432t");
            greaterService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            Assert.IsTrue(greaterService.State.Player.Tavern.Hand.Any(card => card.CardId == "BG33_318"));
            RunAvengeCombat(greaterService, 1, 100, 1);

            Assert.AreEqual(16, greaterTarget.Attack);
            Assert.AreEqual(34, greaterTarget.MaxHealth);
            Assert.AreEqual(
                "BG30_MagicItem_432t",
                greaterService.State.LastResult.PlayerRewards.Single(reward => reward.Type == CombatRewardType.BuffOriginalFriendlyMinion).SourceCardId);
        }

        [Test]
        public void BirdFeeder_AvengeBuffsCombatBoardOnly()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var firstVictim = TestShopMinion("bird-victim-one", 1, 1);
            var secondVictim = TestShopMinion("bird-victim-two", 1, 1);
            var survivor = TestShopMinion("bird-survivor", 20, 20);
            service.State.Player.Board.Add(firstVictim);
            service.State.Player.Board.Add(secondVictim);
            service.State.Player.Board.Add(survivor);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_864");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 2, 100, 6);

            var combatSurvivor = FinalCombatMinion(service, survivor);
            Assert.AreEqual(21, combatSurvivor.Attack);
            Assert.AreEqual(21, combatSurvivor.MaxHealth);
            Assert.AreEqual(20, survivor.Attack);
            Assert.AreEqual(20, survivor.MaxHealth);
        }

        [Test]
        public void BeetleBand_AvengeSummonsTauntBeetle()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            for (var index = 0; index < 5; index += 1)
            {
                service.State.Player.Board.Add(TestShopMinion("beetle-victim-" + index, 1, 1));
            }

            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_860");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 1, 100, 5);

            var beetle = service.State.LastResult.FinalPlayerBoard.FirstOrDefault(minion => minion.Name == "Beetle");
            Assert.IsNotNull(beetle);
            Assert.AreEqual(2, beetle.Attack);
            Assert.AreEqual(2, beetle.MaxHealth);
            Assert.IsTrue(beetle.Keywords.Contains(Keyword.Taunt));
        }

        [Test]
        public void QuilligraphySet_AvengeImprovesBloodGemsPermanently()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            for (var index = 0; index < 4; index += 1)
            {
                service.State.Player.Board.Add(TestShopMinion("quilligraphy-victim-" + index, 1, 1));
            }

            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_410t2");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 1, 100, 10);

            Assert.AreEqual(1, service.State.Player.Tavern.BloodGemBonusAttack);
            Assert.AreEqual(1, service.State.Player.Tavern.BloodGemBonusHealth);
        }

        [Test]
        public void WickedTome_AvengeImprovesTavernSpellBuffsPermanently()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            for (var index = 0; index < 3; index += 1)
            {
                service.State.Player.Board.Add(TestShopMinion("wicked-victim-" + index, 1, 1));
            }

            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_270");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 1, 100, 8);

            Assert.AreEqual(1, service.State.Player.Tavern.TavernSpellBonusAttack);
            Assert.AreEqual(0, service.State.Player.Tavern.TavernSpellBonusHealth);
        }

        [Test]
        public void StaffOfTheScourge_AvengeGivesFriendlyUndeadReborn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            for (var index = 0; index < 5; index += 1)
            {
                service.State.Player.Board.Add(TestShopMinion("scourge-victim-" + index, 1, 1));
            }

            var undead = TestTribeMinion("scourge-undead", 3, 20, Tribe.Undead);
            service.State.Player.Board.Add(undead);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_437");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 1, 100, 5);

            Assert.IsTrue(FinalCombatMinion(service, undead).Keywords.Contains(Keyword.Reborn));
            Assert.IsFalse(undead.Keywords.Contains(Keyword.Reborn));
        }

        [Test]
        public void CloudSerpentHorn_AvengeGivesRightmostAttackToDragon()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            for (var index = 0; index < 3; index += 1)
            {
                service.State.Player.Board.Add(TestShopMinion("cloud-victim-" + index, 1, 1));
            }

            var dragon = TestTribeMinion("cloud-dragon", 2, 20, Tribe.Dragon);
            var rightmost = TestTribeMinion("cloud-rightmost", 9, 20, Tribe.Dragon);
            service.State.Player.Board.Add(dragon);
            service.State.Player.Board.Add(rightmost);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG35_MagicItem_849");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 1, 100, 12);

            Assert.AreEqual(11, FinalCombatMinion(service, dragon).Attack);
            Assert.AreEqual(2, dragon.Attack);
        }

        [Test]
        public void FridgeMagnet_AvengeAddsRandomMagneticMechToHand()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Hand.Clear();
            for (var index = 0; index < 3; index += 1)
            {
                service.State.Player.Board.Add(TestShopMinion("fridge-victim-" + index, 1, 1));
            }

            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG30_MagicItem_545");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 1, 100, 5);

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card =>
                card.CardKind == CardKind.Minion &&
                card.Tribes.Contains(Tribe.Mech) &&
                card.Keywords.Contains(Keyword.Magnetic)));
        }

        [Test]
        public void TarecgosaSticker_PersistsOnlyEdgeDragonCombatBuffsAndKeywords()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var left = TestTribeMinion("tarecgosa-left", 2, 20, Tribe.Dragon);
            var middle = TestTribeMinion("tarecgosa-middle", 4, 20, Tribe.Dragon);
            var right = TestTribeMinion("tarecgosa-right", 6, 20, Tribe.Dragon);
            var guardian = TestShopMinion("BG24_500", 1, 20);
            service.State.Player.Board.Add(left);
            service.State.Player.Board.Add(middle);
            service.State.Player.Board.Add(right);
            service.State.Player.Board.Add(guardian);
            service.State.Player.Tavern.NextCombatBoardAttack = 3;
            service.State.Player.Tavern.NextCombatBoardHealth = 2;
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_417");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunStartOfCombat(service);

            Assert.AreEqual(7, left.Attack);
            Assert.AreEqual(24, left.MaxHealth);
            Assert.IsTrue(left.Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(4, middle.Attack);
            Assert.AreEqual(20, middle.MaxHealth);
            Assert.IsFalse(middle.Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(9, right.Attack);
            Assert.AreEqual(22, right.MaxHealth);
        }

        [Test]
        public void UnholySanctum_PermanentlyBuffsOriginalRightmostAfterDeathrattle()
        {
            var lesserService = MatchService.CreateWithDefaultCatalog(12345);
            var lesserSource = TestTribeMinion("BG33_894", 1, 5, Tribe.Murloc, Keyword.Deathrattle);
            var lesserTarget = TestShopMinion("unholy-lesser-target", 10, 20);
            lesserService.State.Player.Board.Add(lesserSource);
            lesserService.State.Player.Board.Add(lesserTarget);
            lesserService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(lesserService, "BG32_MagicItem_862");
            lesserService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(lesserService, 5, 100, 1);

            Assert.AreEqual(12, lesserTarget.Attack);
            Assert.AreEqual(22, lesserTarget.MaxHealth);
            Assert.AreEqual(12, FinalCombatMinion(lesserService, lesserTarget).Attack);
            Assert.AreEqual(
                lesserTarget.InstanceId,
                lesserService.State.LastResult.PlayerRewards.Single(reward => reward.Type == CombatRewardType.BuffOriginalFriendlyMinion).TargetInstanceId);

            var greaterService = MatchService.CreateWithDefaultCatalog(12345);
            var greaterSource = TestTribeMinion("BG33_894", 1, 5, Tribe.Murloc, Keyword.Deathrattle);
            greaterSource.InstanceId = "test-unholy-greater-source";
            var greaterTarget = TestShopMinion("unholy-greater-target", 10, 20);
            greaterService.State.Player.Board.Add(greaterSource);
            greaterService.State.Player.Board.Add(greaterTarget);
            greaterService.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(greaterService, "BG32_MagicItem_862t");
            greaterService.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(greaterService, 5, 100, 1);

            Assert.AreEqual(16, greaterTarget.Attack);
            Assert.AreEqual(24, greaterTarget.MaxHealth);
            Assert.AreEqual(16, FinalCombatMinion(greaterService, greaterTarget).Attack);
        }

        [Test]
        public void UnholySanctum_SkipsGeneratedRightmostPermanentWriteback()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var manasaber = TestTribeMinion("BG26_800", 1, 4, Tribe.Beast, Keyword.Deathrattle);
            service.State.Player.Board.Add(manasaber);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, "BG32_MagicItem_862");
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            RunAvengeCombat(service, 4, 100, 1);

            var cublings = service.State.LastResult.FinalPlayerBoard
                .Where(card => card.Name == "Cubling")
                .ToList();
            Assert.AreEqual(2, cublings.Count);
            Assert.IsTrue(cublings.Any(card => card.Attack == 2 && card.MaxHealth == 3));
            Assert.AreEqual(1, manasaber.Attack);
            Assert.AreEqual(4, manasaber.MaxHealth);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(entry => entry.Message.Contains("找不到原随从")));
            Assert.IsFalse(service.State.Player.Tavern.RecruitLog.Any(entry => entry.Message.Contains("BG32_MagicItem_862")));
        }

        [Test]
        public void ElectromagneticDevice_DiscoverAndMagnetizeBuffsTarget()
        {
            var magneticIds = SelectMinionIds(card =>
                card.Tribes != null &&
                card.Tribes.Contains(Tribe.Mech) &&
                card.Keywords.Contains(Keyword.Magnetic), 3);
            var service = CreateServiceWithExactEnabledMinions(12345, magneticIds.ToArray());
            var target = TestTribeMinion("electromagnetic-target", 2, 2, Tribe.Mech);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG30_MagicItem_709");

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(1, service.State.Player.Tavern.Discover.RemainingPicks);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(card =>
                card.Tribes.Contains(Tribe.Mech) &&
                card.Keywords.Contains(Keyword.Magnetic)));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            var magnetic = service.State.Player.Tavern.Hand.Single(card => card.Keywords.Contains(Keyword.Magnetic));
            Assert.AreEqual(PoolSource.Discover, magnetic.PoolSource);
            var expectedAttack = target.Attack + magnetic.Attack + 3;
            var expectedHealth = target.MaxHealth + magnetic.MaxHealth + 3;
            var handIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.InstanceId == magnetic.InstanceId);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, handIndex, 0));

            Assert.AreEqual(expectedAttack, target.Attack);
            Assert.AreEqual(expectedHealth, target.MaxHealth);
        }

        [Test]
        public void Batch4DiscoverRewards_ApplyStatsLocksAndTurnStartCopies()
        {
            var tierFourIds = SelectMinionIds(card => card.TavernTier == 4, 3);
            var hearth = CreateServiceWithExactEnabledMinions(12345, tierFourIds.ToArray());
            hearth.State.Player.Tavern.Tier = 4;
            hearth.State.Player.Tavern.Gold = 20;

            EquipTrinket(hearth, "BG32_MagicItem_362");
            hearth.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            var hearthReward = hearth.State.Player.Tavern.Hand.Single();
            Assert.AreEqual(12, hearthReward.Attack);
            Assert.AreEqual(12, hearthReward.MaxHealth);
            Assert.AreEqual(PoolSource.Discover, hearthReward.PoolSource);

            var maxTier = MinionCatalogLoader.LoadFromResources().All
                .Where(card => card.InPool && card.PoolCount > 0 && !card.CardId.StartsWith("BGDUO"))
                .Select(card => card.TavernTier)
                .DefaultIfEmpty(1)
                .Max();
            var kaleidoscopeIds = SelectMinionIds(card => card.TavernTier == maxTier, 3);
            var kaleidoscope = CreateServiceWithExactEnabledMinions(12345, kaleidoscopeIds.ToArray());
            kaleidoscope.State.Player.Tavern.Gold = 20;
            UnlockTierSevenForTest(kaleidoscope);

            EquipTrinket(kaleidoscope, "BG35_MagicItem_821t");
            kaleidoscope.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            var locked = kaleidoscope.State.Player.Tavern.Hand.Single();
            Assert.IsTrue(locked.Golden);
            Assert.AreEqual(maxTier, locked.TavernTier);
            Assert.IsTrue(locked.Tags.Contains("locked_in_hand"));
            Assert.AreEqual(2, locked.Counters[LockedTurnsCounter]);

            kaleidoscope.Apply(new GameCommand(GameCommandType.NextTurn));
            locked = kaleidoscope.State.Player.Tavern.Hand.Single(card => card.InstanceId == locked.InstanceId);
            Assert.AreEqual(1, locked.Counters[LockedTurnsCounter]);
            kaleidoscope.Apply(new GameCommand(GameCommandType.NextTurn));
            locked = kaleidoscope.State.Player.Tavern.Hand.Single(card => card.InstanceId == locked.InstanceId);
            Assert.IsFalse(locked.Tags.Contains("locked_in_hand"));
            Assert.IsFalse(locked.Counters.ContainsKey(LockedTurnsCounter));

            var typedTierFourIds = SelectMinionIds(card =>
                card.TavernTier == 4 &&
                card.Tribes != null &&
                card.Tribes.Any(tribe => tribe != Tribe.None), 3);
            var factory = CreateServiceWithExactEnabledMinions(12345, typedTierFourIds.ToArray());
            factory.State.Player.Tavern.Gold = 20;

            EquipTrinket(factory, "BG32_MagicItem_361");
            factory.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            var chosenFactoryCardId = factory.State.Player.Tavern.Hand.Single().CardId;

            factory.Apply(new GameCommand(GameCommandType.NextTurn));

            var factoryCopies = factory.State.Player.Tavern.Hand
                .Where(card => card.CardId == chosenFactoryCardId)
                .ToList();
            Assert.AreEqual(2, factoryCopies.Count);
            Assert.IsTrue(factoryCopies.All(card => card.PoolCopiesHeld == 0));
            Assert.AreEqual(PoolSource.Copy, factoryCopies.Last().PoolSource);
        }

        [Test]
        public void TranscribingTypewriter_CopiesNextBoughtMinionsAndStops()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestShopMinion("typewriter-buy-one", 2, 3),
                TestShopMinion("typewriter-buy-two", 4, 5),
                TestShopMinion("typewriter-buy-three", 6, 7)
            };

            EquipTrinket(service, "BG35_MagicItem_931");

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 2));

            var copies = service.State.Player.Tavern.Hand
                .Where(card => card.InstanceId.StartsWith("typewriter-copy-BG35_MagicItem_931"))
                .ToList();
            Assert.AreEqual(2, copies.Count);
            Assert.IsTrue(copies.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(copies.All(card => card.PoolCopiesHeld == 0));
            Assert.IsFalse(copies.Any(card => card.CardId == "typewriter-buy-three"));
        }

        [Test]
        public void Batch4Spellcrafts_DestroyDevourAndShakerCopies()
        {
            var jailer = CreateServiceWithSingleRewardTribeMinion(12345, Tribe.Undead, out _);
            var undead = TestTribeMinion("jailer-undead", 2, 2, Tribe.Undead);
            undead.CardId = "BG28_300";
            undead.Name = "Harmless Bonehead";
            undead.Keywords.Add(Keyword.Deathrattle);
            jailer.State.Player.Board.Add(undead);
            jailer.State.Player.Tavern.Gold = 20;

            EquipTrinket(jailer, "BG35_MagicItem_733");

            var jailerSpellIndex = jailer.State.Player.Tavern.Hand.FindIndex(card => card.CardId == JailerStickerSpellCardId);
            Assert.AreNotEqual(-1, jailerSpellIndex);
            jailer.Apply(new GameCommand(GameCommandType.PlayMinion, jailerSpellIndex, 0));

            Assert.IsFalse(jailer.State.Player.Board.Contains(undead));
            Assert.AreEqual(2, jailer.State.Player.Board.Count(minion => minion.Name == "Skeleton"));
            Assert.AreEqual(
                2,
                jailer.State.Player.Tavern.Hand.Count(card =>
                    card.CardKind == CardKind.Minion &&
                    BoardTribeAnalyzer.GetCountedTribes(card).Contains(Tribe.Undead)));

            var devour = MatchService.CreateWithDefaultCatalog(12345);
            var eater = TestShopMinion("demonblood-eater", 2, 3);
            devour.State.Player.Board.Add(eater);
            devour.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestShopMinion("demonblood-shop-one", 1, 2),
                TestShopMinion("demonblood-shop-two", 3, 4)
            };
            devour.State.Player.Tavern.Gold = 20;

            EquipTrinket(devour, "BG30_MagicItem_429");
            EquipTrinket(devour, "BG32_MagicItem_902");

            var devourSpellIndex = devour.State.Player.Tavern.Hand.FindIndex(card => card.CardId == DemonbloodGourdSpellCardId);
            Assert.AreNotEqual(-1, devourSpellIndex);
            devour.Apply(new GameCommand(GameCommandType.PlayMinion, devourSpellIndex, 0));
            devour.State.Player.Tavern.Hand.Add(TestSpellcraftSpell(DemonbloodGourdSpellCardId, "second-devour"));
            devour.Apply(new GameCommand(GameCommandType.PlayMinion, devour.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.AreEqual(6, eater.Attack);
            Assert.AreEqual(9, eater.MaxHealth);
            Assert.IsTrue(devour.State.Player.Tavern.Shop.All(card => card == null || card.CardKind != CardKind.Minion));
            Assert.AreEqual(1, devour.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));

            var shaker = MatchService.CreateWithDefaultCatalog(12345);
            shaker.State.Player.Tavern.Tier = 2;
            shaker.State.Player.Tavern.Gold = 20;

            EquipTrinket(shaker, "BG30_MagicItem_828");

            var zestyIndex = shaker.State.Player.Tavern.Hand.FindIndex(card => card.CardId == ZestyShakerCardId);
            Assert.AreNotEqual(-1, zestyIndex);
            shaker.Apply(new GameCommand(GameCommandType.PlayMinion, zestyIndex, -1));
            var zestyBoardIndex = shaker.State.Player.Board.FindIndex(card => card.CardId == ZestyShakerCardId);
            shaker.State.Player.Tavern.Hand.Add(TestSpellcraftSpell("REEF_RIFFER_SPELL", "shaker"));
            shaker.Apply(new GameCommand(GameCommandType.PlayMinion, shaker.State.Player.Tavern.Hand.Count - 1, zestyBoardIndex));

            var zestyCopies = shaker.State.Player.Tavern.Hand
                .Where(card => card.InstanceId.StartsWith("zesty-copy-"))
                .ToList();
            Assert.AreEqual(2, zestyCopies.Count);
            Assert.IsTrue(zestyCopies.All(card => card.PoolSource == PoolSource.Copy));
        }

        [Test]
        public void Batch4GeneratedBundles_AddExpectedHandCardsAndTransformBoard()
        {
            var curator = MatchService.CreateWithDefaultCatalog(12345);
            curator.State.Player.Tavern.Gold = 20;
            EquipTrinket(curator, "BG32_MagicItem_807");

            var mishmash = curator.State.Player.Tavern.Hand.Single(card => card.CardId == "TB_BaconShop_HERO_33_Buddy");
            Assert.IsTrue(mishmash.Golden);
            AssertHasAllPortraitTypes(mishmash);
            var amalgam = curator.State.Player.Tavern.Hand.Single(card => card.CardId == "TRINKET_CURATOR_AMALGAM");
            Assert.AreEqual(10, amalgam.Attack);
            Assert.AreEqual(10, amalgam.MaxHealth);
            Assert.IsTrue(amalgam.Keywords.Contains(Keyword.Venomous));
            AssertHasAllPortraitTypes(amalgam);

            var horn = MatchService.CreateWithDefaultCatalog(12345);
            horn.State.Player.Tavern.Gold = 20;
            EquipTrinket(horn, "BG32_MagicItem_304");
            Assert.AreEqual(6, horn.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(horn.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.Minion && card.TavernTier == 1));
            Assert.AreEqual(6, horn.State.Player.Tavern.Hand.Select(card => card.CardId).Distinct().Count());
            Assert.IsTrue(horn.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));

            var topHat = MatchService.CreateWithDefaultCatalog(12345);
            topHat.State.Player.Tavern.Gold = 20;
            EquipTrinket(topHat, "BG35_MagicItem_815");
            Assert.AreEqual(6, topHat.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(2, topHat.State.Player.Tavern.Hand.Count(card => card.TavernTier == 1));
            Assert.AreEqual(2, topHat.State.Player.Tavern.Hand.Count(card => card.TavernTier == 2));
            Assert.AreEqual(2, topHat.State.Player.Tavern.Hand.Count(card => card.TavernTier == 3));

            var shrine = MatchService.CreateWithDefaultCatalog(12345);
            shrine.State.Player.Board.Add(TestShopMinion("shrine-one", 1, 1));
            shrine.State.Player.Board.Add(TestShopMinion("shrine-two", 2, 2));
            shrine.State.Player.Tavern.Gold = 20;
            EquipTrinket(shrine, "BG32_MagicItem_400");

            Assert.AreEqual(2, shrine.State.Player.Board.Count);
            Assert.IsTrue(shrine.State.Player.Board.All(card => card.TavernTier == 4));
            Assert.IsTrue(shrine.State.Player.Board.All(card => card.PoolSource == PoolSource.Copy));
        }

        [Test]
        public void SplinterOfAurumAndReplicaCathedral_TriggerAtThresholdAndOncePerTurn()
        {
            var splinter = MatchService.CreateWithDefaultCatalog(12345);
            splinter.State.Player.Tavern.Gold = 20;

            EquipTrinket(splinter, "BG32_MagicItem_350");

            var goldenTierFive = splinter.State.Player.Tavern.Hand.Single();
            Assert.AreEqual(CardKind.Minion, goldenTierFive.CardKind);
            Assert.AreEqual(5, goldenTierFive.TavernTier);
            Assert.IsTrue(goldenTierFive.Golden);
            Assert.AreEqual(PoolSource.Copy, goldenTierFive.PoolSource);

            splinter.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(1, splinter.State.Player.Tavern.Hand.Count(card => card.Golden && card.TavernTier == 5));

            var replica = MatchService.CreateWithDefaultCatalog(12345);
            var target = TestShopMinion("replica-target", 2, 3);
            replica.State.Player.Board.Add(target);
            replica.State.Player.Tavern.Gold = 20;

            EquipTrinket(replica, "BG30_MagicItem_434");

            AddBloodGemSpellToHand(replica, "replica-one");
            replica.Apply(new GameCommand(GameCommandType.PlayMinion, replica.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.AreEqual(4, target.Attack);
            Assert.AreEqual(5, target.MaxHealth);

            AddBloodGemSpellToHand(replica, "replica-two");
            replica.Apply(new GameCommand(GameCommandType.PlayMinion, replica.State.Player.Tavern.Hand.Count - 1, 0));

            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(6, target.MaxHealth);
        }

        [Test]
        public void TideRaiserPortrait_CopiesCombatSpellCastsAfterCombat()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG35_MagicItem_922");

            var tide = service.State.Player.Tavern.Hand.Single(card => card.CardId == TideRaiserCardId);
            service.State.Player.Tavern.Hand.Remove(tide);
            tide.Attack = 1;
            tide.Health = 1;
            tide.MaxHealth = 1;
            service.State.Player.Board.Add(tide);
            service.State.Player.Board.Add(TestTribeMinion("tide-naga", 3, 10, Tribe.Naga));

            RunAvengeCombat(service, 100, 100, 1);

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardId == ShiftingTideSpellCardId));
            var copiedSpell = service.State.Player.Tavern.Hand.Single(card => card.CardId == ShiftingTideSpellCardId);
            Assert.AreEqual(CardKind.Spell, copiedSpell.CardKind);
            Assert.IsTrue(copiedSpell.Tags.Contains("spellcraft"));
            Assert.IsTrue(copiedSpell.Tags.Contains("temporary_spellcraft_card"));
        }

        private static void RunStartOfCombat(MatchService service, int seed = 77)
        {
            service.State.Opponent.Board.Clear();
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = seed, SafetyLimit = 1 }));
        }

        private static void RunAvengeCombat(MatchService service, int opponentAttack, int opponentHealth, int safetyLimit)
        {
            service.State.Opponent.Board.Clear();
            var opponent = TestShopMinion("avenge-opponent", opponentAttack, opponentHealth);
            opponent.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(opponent);
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 123, SafetyLimit = safetyLimit }));
        }

        private static MinionInstance FinalCombatMinion(MatchService service, MinionInstance source)
        {
            return service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == source.InstanceId);
        }

        private static MatchService CreateServiceWithEnabledTavernSpells(int seed, params string[] spellCardNumbers)
        {
            var minion = MinionCatalogLoader.LoadFromResources().All
                .First(card => card.InPool && card.TavernTier == 1 && card.PoolCount > 0 && !card.CardId.StartsWith("BGDUO"));
            var setup = new MatchSetupOptions
            {
                ActiveTribes = TribeAvailabilityRules.AllPlayableTribes(),
                CardPoolVersionId = "test-lavish-cape",
                CardPoolVersionName = "Lavish Cape test",
                IsDefaultCardPoolVersion = false,
                EnabledMinionCardIds = new List<string> { minion.CardId },
                EnabledTavernSpellCardNumbers = spellCardNumbers.ToList()
            };

            return MatchService.CreateWithDefaultCatalog(seed, null, setup);
        }

        private static void AssertTurnStartTavernSpellTrinket(string trinketCardId, string expectedSpellCardId, int expectedTier)
        {
            var service = CreateServiceWithEnabledTavernSpells(12345, expectedSpellCardId);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, trinketCardId);
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            AssertTavernSpellHand(service, 1, expectedSpellCardId, expectedTier);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            AssertTavernSpellHand(service, 2, expectedSpellCardId, expectedTier);
        }

        private static void AssertTavernSpellHand(MatchService service, int expectedCount, string expectedSpellCardId, int expectedTier)
        {
            Assert.AreEqual(expectedCount, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.TavernSpell));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.CardId == expectedSpellCardId));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.TavernTier == expectedTier));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));
        }

        private static void AssertDarkmoonPrizeDiscover(MatchService service, string source, int tier)
        {
            var discover = service.State.Player.Tavern.Discover;
            Assert.IsNotNull(discover);
            Assert.AreEqual(source, discover.Source);
            Assert.AreEqual(tier, discover.RewardTier);
            var prizeIds = service.DarkmoonPrizeCatalog.GetByTier(tier).Select(prize => prize.CardId).ToList();
            Assert.IsNotEmpty(prizeIds);
            Assert.IsTrue(discover.Options.All(card =>
                card.CardKind == CardKind.Spell &&
                card.TavernTier == tier &&
                prizeIds.Contains(card.CardId) &&
                card.Tags.Contains("darkmoon_prize") &&
                card.Tags.Contains("darkmoon_prize_tier_" + tier)));
        }

        private static void AssertSingleEquipAdds(string trinketCardId, int expectedCount, System.Func<MinionInstance, bool> predicate)
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, trinketCardId);

            Assert.AreEqual(expectedCount, service.State.Player.Tavern.Hand.Count(predicate));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolSource == PoolSource.Copy));
            Assert.IsTrue(service.State.Player.Tavern.Hand.All(card => card.PoolCopiesHeld == 0));
            Assert.AreEqual(
                service.State.Player.Tavern.Hand.Count,
                service.State.Player.Tavern.Hand.Select(card => card.InstanceId).Distinct().Count());
        }

        private static void AssertSpecifiedMinionPortrait(
            string trinketCardId,
            string expectedMinionCardId,
            int expectedTier,
            Tribe? expectedTribe,
            params Keyword[] expectedKeywords)
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;

            QueueTrinketChoice(service, trinketCardId);
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            var minion = service.State.Player.Tavern.Hand[0];
            Assert.AreEqual(CardKind.Minion, minion.CardKind);
            Assert.AreEqual(expectedMinionCardId, minion.CardId);
            Assert.AreEqual(expectedTier, minion.TavernTier);
            if (expectedTribe.HasValue)
            {
                Assert.IsTrue(BoardTribeAnalyzer.GetCountedTribes(minion).Contains(expectedTribe.Value));
            }

            foreach (var expectedKeyword in expectedKeywords)
            {
                Assert.IsTrue(minion.Keywords.Contains(expectedKeyword));
            }

            Assert.AreEqual(PoolSource.Copy, minion.PoolSource);
            Assert.AreEqual(0, minion.PoolCopiesHeld);
        }

        private static void AssertHasPortraitTribes(MinionInstance minion, params Tribe[] expectedTribes)
        {
            var countedTribes = BoardTribeAnalyzer.GetCountedTribes(minion);
            foreach (var expectedTribe in expectedTribes)
            {
                CollectionAssert.Contains(countedTribes, expectedTribe);
            }

            Assert.IsFalse(minion.Tribes.Contains(Tribe.None));
        }

        private static void AssertHasAllPortraitTypes(MinionInstance minion)
        {
            Assert.IsTrue(minion.Tribes.Contains(Tribe.All));
            Assert.IsFalse(minion.Tribes.Contains(Tribe.None));
            var countedTribes = BoardTribeAnalyzer.GetCountedTribes(minion);
            CollectionAssert.Contains(countedTribes, Tribe.Beast);
            CollectionAssert.Contains(countedTribes, Tribe.Murloc);
            CollectionAssert.Contains(countedTribes, Tribe.Mech);
            CollectionAssert.Contains(countedTribes, Tribe.Demon);
            CollectionAssert.Contains(countedTribes, Tribe.Dragon);
            CollectionAssert.Contains(countedTribes, Tribe.Pirate);
            CollectionAssert.Contains(countedTribes, Tribe.Elemental);
            CollectionAssert.Contains(countedTribes, Tribe.Quilboar);
            CollectionAssert.Contains(countedTribes, Tribe.Undead);
            CollectionAssert.Contains(countedTribes, Tribe.Naga);
        }

        private static MatchService CreateServiceWithEnabledMinions(int seed, params string[] minionCardIds)
        {
            var tierOne = MinionCatalogLoader.LoadFromResources().All
                .First(card => card.InPool && card.TavernTier == 1 && card.PoolCount > 0 && !card.CardId.StartsWith("BGDUO"));
            var enabled = new List<string> { tierOne.CardId };
            enabled.AddRange(minionCardIds.Where(cardId => !string.IsNullOrEmpty(cardId)));
            var setup = new MatchSetupOptions
            {
                ActiveTribes = TribeAvailabilityRules.AllPlayableTribes(),
                CardPoolVersionId = "test-enabled-minions",
                CardPoolVersionName = "Enabled minions test",
                IsDefaultCardPoolVersion = false,
                EnabledMinionCardIds = enabled.Distinct().ToList()
            };

            return MatchService.CreateWithDefaultCatalog(seed, null, setup);
        }

        private static MatchService CreateServiceWithExactEnabledMinions(int seed, params string[] minionCardIds)
        {
            var setup = new MatchSetupOptions
            {
                ActiveTribes = TribeAvailabilityRules.AllPlayableTribes(),
                CardPoolVersionId = "test-exact-enabled-minions",
                CardPoolVersionName = "Exact enabled minions test",
                IsDefaultCardPoolVersion = false,
                EnabledMinionCardIds = minionCardIds
                    .Where(cardId => !string.IsNullOrEmpty(cardId))
                    .Distinct()
                    .ToList()
            };

            return MatchService.CreateWithDefaultCatalog(seed, null, setup);
        }

        private static List<string> SelectMinionIds(System.Func<MinionDefinition, bool> predicate, int count)
        {
            var ids = MinionCatalogLoader.LoadFromResources().All
                .Where(card =>
                    card.InPool &&
                    card.PoolCount > 0 &&
                    !card.CardId.StartsWith("BGDUO") &&
                    predicate(card))
                .Select(card => card.CardId)
                .Distinct()
                .Take(count)
                .ToList();
            Assert.GreaterOrEqual(ids.Count, count);
            return ids;
        }

        private static MatchService CreateServiceWithSingleRewardTribeMinion(int seed, Tribe tribe, out MinionDefinition reward)
        {
            var minions = MinionCatalogLoader.LoadFromResources().All;
            reward = minions.First(card =>
                card.InPool &&
                card.PoolCount > 0 &&
                card.Tribes != null &&
                card.Tribes.Contains(tribe) &&
                !card.CardId.StartsWith("BGDUO"));
            var filler = minions.First(card =>
                card.InPool &&
                card.TavernTier == 1 &&
                card.PoolCount > 0 &&
                !MatchesTribeDefinition(card, tribe) &&
                !card.CardId.StartsWith("BGDUO"));
            var setup = new MatchSetupOptions
            {
                ActiveTribes = TribeAvailabilityRules.AllPlayableTribes(),
                CardPoolVersionId = "test-single-tribe-reward",
                CardPoolVersionName = "Single tribe reward test",
                IsDefaultCardPoolVersion = false,
                EnabledMinionCardIds = new List<string> { filler.CardId, reward.CardId }
            };

            return MatchService.CreateWithDefaultCatalog(seed, null, setup);
        }

        private static bool MatchesTribeDefinition(MinionDefinition minion, Tribe tribe)
        {
            return minion?.Tribes != null && (minion.Tribes.Contains(tribe) || minion.Tribes.Contains(Tribe.All));
        }

        private static bool IsChromadrake(MinionInstance card)
        {
            return card != null && ChromadrakeCardIds.Contains(card.CardId);
        }

        private static void PlayHandCard(MatchService service, MinionInstance card)
        {
            var index = service.State.Player.Tavern.Hand.FindIndex(handCard => handCard.InstanceId == card.InstanceId);
            Assert.AreNotEqual(-1, index);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, index, -1));
        }

        private static List<string> AddSellableBoardMinions(MatchService service, string prefix, int count)
        {
            var instanceIds = new List<string>();
            for (var index = 0; index < count; index += 1)
            {
                var minion = TestShopMinion(prefix + "-" + index, 1, 1);
                service.State.Player.Board.Add(minion);
                instanceIds.Add(minion.InstanceId);
            }

            return instanceIds;
        }

        [Test]
        public void FelbatPortrait_AddsFelbatKeepsSevenShopCardsAndDevoursAtTurnEnd()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG30_MagicItem_991");

            var felbat = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG21_005");
            Assert.GreaterOrEqual(service.State.Player.Tavern.Shop.Count(card => card != null), 7);

            service.State.Player.Tavern.FreeRefreshes = 1;
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.GreaterOrEqual(service.State.Player.Tavern.Shop.Count(card => card != null), 7);

            service.State.Player.Tavern.Hand.Remove(felbat);
            service.State.Player.Board.Add(felbat);
            service.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestShopMinion("felbat-food-high", 4, 9),
                TestShopMinion("felbat-food-one", 1, 1),
                TestShopMinion("felbat-food-two", 1, 2),
                TestShopMinion("felbat-food-three", 1, 3),
                TestShopMinion("felbat-food-four", 1, 4),
                TestShopMinion("felbat-food-five", 1, 5),
                TestShopMinion("felbat-food-six", 1, 6)
            };
            var attackBefore = felbat.Attack;
            var healthBefore = felbat.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(attackBefore + 4, felbat.Attack);
            Assert.AreEqual(healthBefore + 9, felbat.MaxHealth);
        }

        [Test]
        public void SkyGolemPortrait_AddsGolemAndPermanentlyBuffsSurvivorFromGrantedDeathrattle()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG35_MagicItem_740");

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "BG35_342"));
            var victim = TestShopMinion("sky-golem-portrait-victim", 1, 1, Keyword.Taunt);
            var survivor = TestShopMinion("sky-golem-portrait-survivor", 3, 20);
            service.State.Player.Board.Add(victim);
            service.State.Player.Board.Add(survivor);

            RunAvengeCombat(service, 2, 100, 1);

            Assert.AreEqual(5, survivor.Attack);
            Assert.AreEqual(22, survivor.MaxHealth);
            Assert.AreEqual(5, FinalCombatMinion(service, survivor).Attack);
            Assert.AreEqual(22, FinalCombatMinion(service, survivor).MaxHealth);
            Assert.IsTrue(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.BuffOriginalFriendlyMinion &&
                reward.SourceCardId == "BG35_MagicItem_740" &&
                reward.TargetInstanceId == survivor.InstanceId));
        }

        [Test]
        public void PromoPortrait_PrizedPromoDrakeFirstStartOfCombatTriggersTwice()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG30_MagicItem_918");

            var promoDrake = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG21_014");
            service.State.Player.Tavern.Hand.Remove(promoDrake);
            service.State.Player.Board.Add(promoDrake);
            var target = TestTribeMinion("promo-portrait-dragon", 2, 3, Tribe.Dragon);
            service.State.Player.Board.Add(target);

            RunStartOfCombat(service);

            var combatTarget = FinalCombatMinion(service, target);
            Assert.AreEqual(10, combatTarget.Attack);
            Assert.AreEqual(11, combatTarget.MaxHealth);
        }

        [Test]
        public void JarredFrostling_StartOfCombatGrantsElementalDeathrattle()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG30_MagicItem_952");
            service.State.Player.Board.Add(TestTribeMinion("test-elemental", 1, 1, Tribe.Elemental));
            service.State.Opponent.Board.Add(TestTribeMinion("test-opponent", 1, 1, Tribe.Beast));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest));

            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Any(minion => minion.CardId == "TRINKET_FLOURISHING_FROSTLING"));
            Assert.IsTrue(service.State.LastReplay.Frames.Any(frame => frame.EventType == CombatEventType.TrinketTriggered && frame.LogText.Contains("Jarred Frostling")));
        }

        [Test]
        public void JarOGems_AfterTwoFriendlyAttacksPlaysBloodGemsOnQuilboar()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG30_MagicItem_546");
            service.State.Player.Board.Add(TestTribeMinion("test-quilboar-a", 1, 10, Tribe.Quilboar));
            service.State.Player.Board.Add(TestTribeMinion("test-quilboar-b", 1, 10, Tribe.Quilboar));
            service.State.Opponent.Board.Add(TestTribeMinion("test-opponent", 0, 10, Tribe.Beast));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest));

            Assert.IsTrue(service.State.LastReplay.Frames.Any(frame => frame.LogText.Contains("Jar o' Gems")));
            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Count(minion => minion.MaxHealth > 10) >= 2);
        }

        [Test]
        public void TigerCarving_DamageTakenPermanentlyBuffsAnotherFriendlyMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG30_MagicItem_427");
            var damaged = TestTribeMinion("test-damaged", 1, 10, Tribe.Beast);
            var target = TestTribeMinion("test-target", 2, 2, Tribe.Beast);
            service.State.Player.Board.Add(damaged);
            service.State.Player.Board.Add(target);
            service.State.Opponent.Board.Add(TestTribeMinion("test-opponent", 1, 1, Tribe.Beast));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest));

            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(3, target.MaxHealth);
        }

        [Test]
        public void ElementiumChest_AfterTwoPirateAttacksQueuesGoldNextTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG30_MagicItem_923");
            service.State.Player.Board.Add(TestTribeMinion("test-pirate-a", 1, 5, Tribe.Pirate));
            service.State.Player.Board.Add(TestTribeMinion("test-pirate-b", 3, 5, Tribe.Pirate));
            service.State.Opponent.Board.Add(TestTribeMinion("test-opponent", 0, 4, Tribe.Beast));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest));

            Assert.AreEqual(1, service.State.Player.Tavern.NextTurnBonusGold);
        }

        [Test]
        public void AccordOTronPortrait_EndTurnMagnetizesEdgeMechs()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 20;
            EquipTrinket(service, "BG35_MagicItem_742");
            var left = TestTribeMinion("test-left-mech", 2, 2, Tribe.Mech);
            var right = TestTribeMinion("test-right-mech", 2, 2, Tribe.Mech);
            service.State.Player.Board.Add(left);
            service.State.Player.Board.Add(TestTribeMinion("test-middle-beast", 2, 2, Tribe.Beast));
            service.State.Player.Board.Add(right);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.Greater(left.Attack, 2);
            Assert.Greater(left.MaxHealth, 2);
            Assert.Greater(right.Attack, 2);
            Assert.Greater(right.MaxHealth, 2);
        }

        [Test]
        public void MysteryCube_OffersFreeLesserReplacementAfterEquip()
        {
            var service = MatchService.CreateWithDefaultCatalog(7201);
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG30_MagicItem_703");

            var request = service.State.Player.Tavern.AdvancedMechanics.PendingChoice;
            Assert.IsNotNull(request);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, request.Kind);
            Assert.AreEqual(2, request.Options.Count);
            Assert.IsTrue(request.Source.StartsWith("trinket-replace-free:"));
            Assert.IsTrue(request.Options.All(option => option.Cost == 0));
            Assert.IsFalse(request.Options.Any(option => option.SourceId == "BG30_MagicItem_703"));
            Assert.IsTrue(request.Options.All(option =>
                service.TrinketCatalog.GetByCardId(option.SourceId).SlotKind == TrinketSlotKind.Lesser));
        }

        [Test]
        public void OrbOfTheUnknown_ReplacesItselfWithRandomOfferableTrinket()
        {
            var service = MatchService.CreateWithDefaultCatalog(7202);
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG35_MagicItem_816");

            var equipped = service.State.Player.Tavern.AdvancedMechanics.Trinkets.LesserTrinketId;
            Assert.IsFalse(string.Equals("BG35_MagicItem_816", equipped, System.StringComparison.OrdinalIgnoreCase));
            var replacement = service.TrinketCatalog.GetByCardId(equipped);
            Assert.AreEqual(TrinketSlotKind.Lesser, replacement.SlotKind);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, replacement.OfferPoolStatus);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, replacement.ImplementationStatus);
        }

        [Test]
        public void TimewornCandelabra_OpensTimewarpDiscover()
        {
            var service = MatchService.CreateWithDefaultCatalog(7203);
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG35_MagicItem_823");

            var discover = service.State.Player.Tavern.Discover;
            Assert.IsNotNull(discover);
            Assert.AreEqual(3, discover.Options.Count);
            Assert.IsTrue(discover.Options.All(option => option.Name.Contains("Timewarped")));
        }

        [Test]
        public void BurglingClaw_CopiesHighestTierMinionFromLastOpponentWarband()
        {
            var service = MatchService.CreateWithDefaultCatalog(7204);
            service.State.Player.Tavern.Gold = 20;
            service.State.OpponentHistory.LastOpponentWarband.Add(CreateTestOpponentMinion("low-test", "Low Test", 2, 2, 2));
            service.State.OpponentHistory.LastOpponentWarband.Add(CreateTestOpponentMinion("high-test", "High Test", 6, 7, 8));

            EquipTrinket(service, "BG30_MagicItem_930");
            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            var copied = service.State.Player.Tavern.Hand.SingleOrDefault(card => card.CardId == "high-test");
            Assert.IsNotNull(copied);
            Assert.AreEqual(PoolSource.Copy, copied.PoolSource);
            Assert.AreEqual(6, copied.TavernTier);
        }

        [Test]
        public void SouvenirStand_TransformsLesserSlotWhenGreaterTrinketEquipped()
        {
            var service = MatchService.CreateWithDefaultCatalog(7205);
            service.State.Player.Tavern.Gold = 20;
            var minion = TestShopMinion("souvenir-stand-double-aura", 3, 4);
            service.State.Player.Board.Add(minion);

            EquipTrinket(service, "BG30_MagicItem_888");
            EquipTrinket(service, "BG30_MagicItem_880t");

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual("BG30_MagicItem_880t", trinkets.GreaterTrinketId);
            Assert.AreEqual("BG30_MagicItem_880t", trinkets.LesserTrinketId);
            Assert.AreEqual(19, minion.Attack);
            Assert.AreEqual(14, minion.MaxHealth);
            Assert.IsTrue(minion.Enchantments.Any(enchantment =>
                enchantment.SourceId == FeralTalismanAuraSourceId &&
                enchantment.AttackBonus == 16 &&
                enchantment.HealthBonus == 10));
        }

        [Test]
        public void SouvenirStand_CopyRepeatsGreaterOnAcquireEffect()
        {
            var service = MatchService.CreateWithDefaultCatalog(7206);
            service.State.Player.Tavern.Gold = 20;
            var startingMaxGold = service.State.Player.Tavern.MaxGold;

            EquipTrinket(service, "BG30_MagicItem_888");
            EquipTrinket(service, "BG30_MagicItem_996");

            Assert.AreEqual(8, service.State.Player.Tavern.AdvancedMechanics.Trinkets.ExtraMaxGold);
            Assert.AreEqual(startingMaxGold + 8, service.State.Player.Tavern.MaxGold);
        }

        [Test]
        public void MaxwellStickers_AddHeroPowerBuddyAndGoldenBuddy()
        {
            var lesser = MatchService.CreateWithDefaultCatalog(7301);
            lesser.State.Player.Tavern.Gold = 20;
            lesser.State.Player.HeroPowerCardId = "TB_BaconShop_HP_085";

            EquipTrinket(lesser, "BG35_MagicItem_803");

            var buddy = lesser.State.Player.Tavern.Hand.Single(card => card.CardId == "TB_BaconShop_HERO_75_Buddy");
            Assert.IsFalse(buddy.Golden);

            var greater = MatchService.CreateWithDefaultCatalog(7302);
            greater.State.Player.Tavern.Gold = 20;
            greater.State.Player.HeroPowerCardId = "TB_BaconShop_HP_085";

            EquipTrinket(greater, "BG35_MagicItem_803t");

            var goldenBuddy = greater.State.Player.Tavern.Hand.Single(card => card.CardId == "TB_BaconShop_HERO_75_Buddy");
            Assert.IsTrue(goldenBuddy.Golden);
        }

        [Test]
        public void PutricideSticker_CraftsCreationAndRepeatsEveryTwoTurns()
        {
            var service = MatchService.CreateWithDefaultCatalog(7303);
            var tavern = service.State.Player.Tavern;
            tavern.Gold = 20;

            EquipTrinket(service, "BG32_MagicItem_300");

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual(HeroEffectEngine.PutricideFirstDiscoverSource, tavern.Discover.Source);
            Assert.AreEqual(3, tavern.Discover.Options.Count);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            Assert.AreEqual(HeroEffectEngine.PutricideSecondDiscoverSource, tavern.Discover.Source);
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsTrue(tavern.Hand.Any(card => card.CardId == "BG25_HERO_100pt" && card.Tags.Contains(HeroEffectEngine.PutricideCreationTag)));
            tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            tavern = service.State.Player.Tavern;
            Assert.IsNull(tavern.Discover);
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            tavern = service.State.Player.Tavern;

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual(HeroEffectEngine.PutricideFirstDiscoverSource, tavern.Discover.Source);
        }

        [Test]
        public void SousChefSticker_GainsGoldAfterHeroPowerUse()
        {
            var service = MatchService.CreateWithDefaultCatalog(7304);
            var tavern = service.State.Player.Tavern;
            tavern.Gold = 20;
            service.State.Player.HeroPowerCardId = "TB_BaconShop_HP_085";

            EquipTrinket(service, "BG35_MagicItem_801");
            tavern.Gold = 5;
            Assert.AreEqual(2, service.GetHeroPowerUsesRemainingThisTurn());

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));
            Assert.AreEqual(1, service.GetHeroPowerUsesRemainingThisTurn());
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(5, tavern.Gold);
            Assert.AreEqual(2, tavern.Hand.Count(card => card.CardId == "RAKANISHU_LANTERN_LIGHT"));
            Assert.AreEqual(0, service.GetHeroPowerUsesRemainingThisTurn());
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower)));
        }

        [Test]
        public void AncientWishbone_RepeatsHeroPowerWithoutDoubleChargingBaseCost()
        {
            var service = MatchService.CreateWithDefaultCatalog(7305);
            var tavern = service.State.Player.Tavern;
            tavern.Gold = 20;
            service.State.Player.HeroPowerCardId = "TB_BaconShop_HP_085";

            EquipTrinket(service, "BG30_MagicItem_804");
            tavern.Gold = 5;
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(4, tavern.Gold);
            Assert.AreEqual(2, tavern.Hand.Count(card => card.CardId == "RAKANISHU_LANTERN_LIGHT"));
            Assert.AreEqual(1, tavern.HeroEffectCounters["hero-power-use:count:TB_BaconShop_HP_085"]);
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower)));
        }

        [Test]
        public void CorruptedTome_GrantsTriplePrizeAndReplacesTripleRewards()
        {
            var service = MatchService.CreateWithDefaultCatalog(7306);
            var tavern = service.State.Player.Tavern;
            tavern.Gold = 20;

            EquipTrinket(service, "BG35_MagicItem_812");

            var prize = tavern.Hand.Single(card => card.CardId == "BG35_MagicItem_812t");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.IndexOf(prize)));
            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual(3, tavern.Discover.RewardTier);
            Assert.IsTrue(tavern.Discover.Options.All(card => card.Tags.Contains("darkmoon_prize_tier_3")));
            tavern.CompleteDiscover();
            tavern.Hand.Clear();

            var golden = TestTripleMinion("corrupted-tome-test", "golden", Tribe.Beast, 4, 4);
            golden.Golden = true;
            tavern.Hand.Add(golden);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsTrue(tavern.Hand.Any(card => card.CardId == "BG35_MagicItem_812t"));
            Assert.IsFalse(tavern.Hand.Any(card => card.CardId == "TRIPLE_REWARD"));
        }

        [Test]
        public void ArtanisSticker_AddsMothershipCopy()
        {
            var service = MatchService.CreateWithDefaultCatalog(7307);
            service.State.Player.Tavern.Gold = 20;

            EquipTrinket(service, "BG32_MagicItem_906");

            var mothership = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG31_HERO_802pt7");
            Assert.AreEqual("Mothership", mothership.Name);
            Assert.IsTrue(mothership.Tags.Contains("protoss_reward"));
        }

        private static MatchService CreateDirectionalTrinketOfferService(int seed)
        {
            var service = MatchService.CreateWithDefaultCatalog(
                seed,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    ActiveTribes = new List<Tribe> { Tribe.Beast, Tribe.Murloc, Tribe.Pirate, Tribe.Dragon }
                });
            service.State.Player.Board.Add(TestTribeMinion("test-beast-a", 2, 2, Tribe.Beast));
            service.State.Player.Board.Add(TestTribeMinion("test-beast-b", 3, 3, Tribe.Beast));
            service.State.Player.Board.Add(TestTribeMinion("test-beast-c", 4, 4, Tribe.Beast));
            service.State.Player.Tavern.Hand.Add(TestTribeMinion("test-murloc-hand", 1, 1, Tribe.Murloc));
            return service;
        }

        private static List<TrinketDefinition> LegalOfferableTrinkets(
            MatchService service,
            TrinketSlotKind slotKind,
            IReadOnlyCollection<Tribe> active)
        {
            return service.TrinketCatalog.GetBySlot(slotKind)
                .Where(definition =>
                    definition.ImplementationStatus == TrinketImplementationStatus.Implemented &&
                    definition.OfferPoolStatus == TrinketOfferPoolStatus.Offerable &&
                    TribeAvailabilityRules.IsTrinketAvailable(definition, active))
                .ToList();
        }

        private static bool IsFocusTrinket(
            TrinketDefinition definition,
            IReadOnlyCollection<Tribe> active,
            IReadOnlyCollection<Tribe> main)
        {
            return ActiveTrinketTribes(definition, active).Any(main.Contains);
        }

        private static bool IsExpansionTrinket(
            TrinketDefinition definition,
            IReadOnlyCollection<Tribe> active,
            IReadOnlyCollection<Tribe> main)
        {
            var tribes = ActiveTrinketTribes(definition, active);
            return tribes.Count > 0 && !tribes.Any(main.Contains);
        }

        private static bool IsGenericTrinket(TrinketDefinition definition, IReadOnlyCollection<Tribe> active)
        {
            return ActiveTrinketTribes(definition, active).Count == 0;
        }

        private static List<Tribe> ActiveTrinketTribes(TrinketDefinition definition, IReadOnlyCollection<Tribe> active)
        {
            return TribeAvailabilityRules.TrinketTribes(definition)
                .Where(tribe => TribeAvailabilityRules.IsTribeActive(active, tribe))
                .ToList();
        }

        private static void QueueTrinketChoice(MatchService service, string cardId)
        {
            var definition = service.TrinketCatalog.GetByCardId(cardId);
            service.State.Player.Tavern.AdvancedMechanics.PendingChoice = new MechanicChoiceRequest
            {
                RequestId = "test-" + cardId,
                Kind = AdvancedMechanicKind.Trinket,
                Source = "test",
                Slot = definition.SlotKind.ToString(),
                Round = service.State.Round,
                RemainingPicks = 1,
                Options = new List<MechanicChoiceOption>
                {
                    new MechanicChoiceOption
                    {
                        OptionId = definition.CardId,
                        Kind = AdvancedMechanicKind.Trinket,
                        SourceId = definition.CardId,
                        DisplayName = definition.Name,
                        Text = definition.Text,
                        ImagePath = definition.ImagePath,
                        Cost = definition.Cost,
                        Slot = definition.SlotKind.ToString(),
                        ImplementationStatus = definition.ImplementationStatus.ToString(),
                        Tags = new List<string>(definition.Tags)
                    }
                }
            };
        }

        private static void QueueTrinketReplacementChoice(MatchService service, string cardId)
        {
            QueueTrinketChoice(service, cardId);
            service.State.Player.Tavern.AdvancedMechanics.PendingChoice.Source = "trinket-replace:test";
        }

        private static void EquipTrinket(MatchService service, string cardId)
        {
            QueueTrinketChoice(service, cardId);
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
        }

        private static void UnlockTierSevenForTest(MatchService service)
        {
            var anomalies = service.State.Player.Tavern.AdvancedMechanics.Anomalies;
            anomalies.Enabled = true;
            anomalies.ActiveAnomalyId = SecretsOfNorgannonAnomalyCardId;
            anomalies.ActiveCardId = SecretsOfNorgannonAnomalyCardId;
            anomalies.ActiveName = "Secrets of Norgannon";
            anomalies.ActiveText = "Tavern Tier 7 exists. Start with 10 extra Armor.";
            anomalies.ImplementationStatus = AnomalyImplementationStatus.Implemented;
        }

        private static MinionInstance CreateTestOpponentMinion(string cardId, string name, int tier, int attack, int health)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "opponent-" + cardId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = 3,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = tier,
                Owner = BoardSide.Opponent,
                Tribes = new List<Tribe> { Tribe.Beast },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>()
            };
        }

        private static void AssertCopyCardMetadata(MinionInstance card)
        {
            Assert.AreEqual(PoolSource.Copy, card.PoolSource);
            Assert.AreEqual(0, card.PoolCopiesHeld);
        }

        private static void AssertCatalogTurnStartSpellTrinket(TrinketDefinition trinket, string effectId)
        {
            Assert.AreEqual(TrinketSlotKind.Greater, trinket.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, trinket.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, trinket.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, trinket.PowerLevel);
            Assert.AreEqual("turn_start", trinket.EffectFamily);
            Assert.AreEqual("Exact", trinket.ProxyLevel);
            CollectionAssert.Contains(trinket.EffectIds, effectId);
            CollectionAssert.Contains(trinket.Requires, "tavern_spell");
            CollectionAssert.Contains(trinket.Requires, "turn_start");
        }

        private static void AssertCatalogSpecifiedMinionTrinket(
            TrinketDefinition trinket,
            string effectId,
            TrinketSlotKind slotKind = TrinketSlotKind.Greater,
            string effectFamily = "tribe_specific",
            bool requiresTribePool = true)
        {
            Assert.AreEqual(slotKind, trinket.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, trinket.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, trinket.OfferPoolStatus);
            Assert.AreEqual(TrinketPowerLevel.Medium, trinket.PowerLevel);
            Assert.AreEqual(effectFamily, trinket.EffectFamily);
            Assert.AreEqual("Exact", trinket.ProxyLevel);
            CollectionAssert.Contains(trinket.EffectIds, effectId);
            if (requiresTribePool)
            {
                CollectionAssert.Contains(trinket.Requires, "tribe_pool");
            }
        }

        private static void AssertCatalogTrinket(
            TrinketDefinition trinket,
            string effectId,
            TrinketSlotKind slotKind,
            TrinketPowerLevel powerLevel,
            string effectFamily,
            params string[] requires)
        {
            Assert.AreEqual(slotKind, trinket.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, trinket.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, trinket.OfferPoolStatus);
            Assert.AreEqual(powerLevel, trinket.PowerLevel);
            Assert.AreEqual(effectFamily, trinket.EffectFamily);
            Assert.AreEqual("Exact", trinket.ProxyLevel);
            CollectionAssert.Contains(trinket.EffectIds, effectId);
            foreach (var requirement in requires)
            {
                CollectionAssert.Contains(trinket.Requires, requirement);
            }
        }

        private static void AssertCatalogTrinketWithProxyLevel(
            TrinketDefinition trinket,
            string effectId,
            TrinketSlotKind slotKind,
            TrinketPowerLevel powerLevel,
            string effectFamily,
            string proxyLevel,
            params string[] requires)
        {
            Assert.AreEqual(slotKind, trinket.SlotKind);
            Assert.AreEqual(TrinketImplementationStatus.Implemented, trinket.ImplementationStatus);
            Assert.AreEqual(TrinketOfferPoolStatus.Offerable, trinket.OfferPoolStatus);
            Assert.AreEqual(powerLevel, trinket.PowerLevel);
            Assert.AreEqual(effectFamily, trinket.EffectFamily);
            Assert.AreEqual(proxyLevel, trinket.ProxyLevel);
            CollectionAssert.Contains(trinket.EffectIds, effectId);
            foreach (var requirement in requires)
            {
                CollectionAssert.Contains(trinket.Requires, requirement);
            }
        }

        private static bool IsDuoOrPassTrinket(TrinketDefinition definition)
        {
            var text = string.Join(
                "\n",
                definition.Id,
                definition.CardId,
                definition.Name,
                definition.Text,
                definition.Notes);
            return text.Contains("BGDUO")
                || text.ToLowerInvariant().Contains("teammate")
                || text.ToLowerInvariant().Contains("team mate")
                || text.ToLowerInvariant().Contains("your partner")
                || text.ToLowerInvariant().Contains("pass to")
                || text.ToLowerInvariant().Contains("passes")
                || text.ToLowerInvariant().Contains("passing");
        }

        private static void AddBloodGemSpellToHand(MatchService service, string suffix)
        {
            service.State.Player.Tavern.Hand.Add(TestTavernSpell("BLOOD_GEM", 0, "Give a minion +1/+1.", "buff_spell"));
            service.State.Player.Tavern.Hand.Last().InstanceId = "test-blood-gem-" + suffix;
        }

        private static void StartTestDiscover(MatchService service, string cardId)
        {
            service.State.Player.Tavern.Discover = new DiscoverState
            {
                Source = "test-discover",
                RewardTier = 1,
                RemainingPicks = 1,
                Options = new List<MinionInstance>
                {
                    TestShopMinion(cardId, 2, 2),
                    TestShopMinion(cardId + "-two", 3, 3),
                    TestShopMinion(cardId + "-three", 4, 4)
                }
            };
        }

        private static MinionInstance TestTavernSpell(string cardId, int cost, string text, params string[] tags)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "test-spell-" + cardId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = cardId,
                Cost = cost,
                Attack = 0,
                Health = 0,
                MaxHealth = 0,
                TavernTier = 1,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                OfficialKeywords = new List<Keyword> { Keyword.TavernSpell },
                Text = text,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = tags.ToList()
            };
        }

        private static MinionInstance TestSpellcraftSpell(string cardId, string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "test-spellcraft-" + suffix,
                DefinitionId = cardId,
                CardId = cardId,
                Name = cardId,
                Cost = 0,
                Attack = 0,
                Health = 0,
                MaxHealth = 0,
                TavernTier = 0,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                OfficialKeywords = new List<Keyword> { Keyword.Spellcraft },
                Text = "Spellcraft test spell",
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int> { { "spellcraft_multiplier", 1 } },
                Tags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card", "targeted_spell", "buff_spell" }
            };
        }

        private static MinionInstance TestTavernSpell(string cardId, string suffix, int cost)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "test-tavern-spell-" + suffix,
                DefinitionId = cardId,
                CardId = cardId,
                Name = cardId,
                Cost = cost,
                Attack = 0,
                Health = 0,
                MaxHealth = 0,
                TavernTier = 0,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                OfficialKeywords = new List<Keyword> { Keyword.TavernSpell },
                Text = "Tavern spell test card",
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string> { "generated_spell", "generated_tavern_spell" }
            };
        }

        private static MinionInstance TestShopMinion(string cardId, int attack, int health, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "test-" + cardId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = cardId,
                Cost = 3,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = keywords.ToList(),
                OfficialKeywords = keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>()
            };
        }

        private static MinionInstance TestTribeMinion(string cardId, int attack, int health, Tribe tribe, params Keyword[] keywords)
        {
            var minion = TestShopMinion(cardId, attack, health, keywords);
            minion.Tribes = new List<Tribe> { tribe };
            return minion;
        }

        private static MinionInstance TestTripleMinion(string definitionId, string suffix, Tribe tribe, int attack, int health)
        {
            var minion = TestTribeMinion(definitionId, attack, health, tribe);
            minion.InstanceId = "test-" + definitionId + "-" + suffix;
            minion.DefinitionId = definitionId;
            minion.CardId = definitionId;
            return minion;
        }

        private static MinionInstance CreateTestDemonFodder(string suffix, int attack, int health)
        {
            var minion = TestTribeMinion("test-demon-fodder-" + suffix, attack, health, Tribe.Demon);
            minion.InstanceId = "test-demon-fodder-" + suffix;
            minion.DefinitionId = "demon-fodder";
            minion.CardId = DemonFodderCardId;
            minion.Name = "Demon Fodder";
            minion.Tags.Add("demon_fodder");
            return minion;
        }

        private static void AssertDarnassusAura(MinionInstance minion, int expectedBonus)
        {
            Assert.AreEqual(minion.BaseAttack + expectedBonus, minion.Attack);
            Assert.AreEqual(minion.BaseHealth + expectedBonus, minion.MaxHealth);
            Assert.AreEqual(minion.MaxHealth, minion.Health);
            var enchantments = minion.Enchantments
                .Where(enchantment => enchantment.SourceId == "Trinket:Darnassus Pie")
                .ToList();
            if (expectedBonus <= 0)
            {
                Assert.IsEmpty(enchantments);
                return;
            }

            Assert.AreEqual(1, enchantments.Count);
            Assert.AreEqual(expectedBonus, enchantments[0].AttackBonus);
            Assert.AreEqual(expectedBonus, enchantments[0].HealthBonus);
        }

        private static void AssertDefilerAura(MinionInstance minion, int expectedBonus)
        {
            Assert.AreEqual(minion.BaseAttack + expectedBonus, minion.Attack);
            Assert.AreEqual(minion.BaseHealth + expectedBonus, minion.MaxHealth);
            Assert.AreEqual(minion.MaxHealth, minion.Health);
            var enchantments = minion.Enchantments
                .Where(enchantment => enchantment.SourceId == DefilerPortraitAuraSourceId)
                .ToList();
            if (expectedBonus <= 0)
            {
                Assert.IsEmpty(enchantments);
                return;
            }

            Assert.AreEqual(1, enchantments.Count);
            Assert.AreEqual(expectedBonus, enchantments[0].AttackBonus);
            Assert.AreEqual(expectedBonus, enchantments[0].HealthBonus);
        }

        private static void AssertShopMinionsHaveAtLeastBonus(MatchService service, int attack, int health)
        {
            var shopMinions = service.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            Assert.IsNotEmpty(shopMinions);
            Assert.IsTrue(shopMinions.All(minion => minion.Attack >= minion.BaseAttack + attack));
            Assert.IsTrue(shopMinions.All(minion => minion.MaxHealth >= minion.BaseHealth + health));
        }

        private static bool ContainsChinese(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Any(character => character >= '\u3400' && character <= '\u9fff');
        }
    }
}
