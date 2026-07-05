using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class TavernSpellEngine
    {
        private const int HandLimit = 10;
        private const string BloodGemCardId = "BLOOD_GEM";
        private const string BristlebackBloodGemCardId = "BRISTLEBACK_BLOOD_GEM";
        private const string RebornBloodGemCardId = "REBORN_BLOOD_GEM";
        private const string ScarletSurvivorCardId = "BG35_814";
        private const string SlimyShieldCardId = "SLIMY_SHIELD";
        private const string ReefRifferSpellCardId = "REEF_RIFFER_SPELL";
        private const string SurfNSurfSpellCardId = "SURF_N_SURF_SPELL";
        private const string DeepSeaAnglerSpellCardId = "DEEP_SEA_ANGLER_SPELL";
        private const string DeepBlueSpellCardId = "DEEP_BLUE_SPELL";
        private const string VolcanicVisitorAttackSpellCardId = "VOLCANIC_VISITOR_ATTACK_SPELL";
        private const string VolcanicVisitorHealthSpellCardId = "VOLCANIC_VISITOR_HEALTH_SPELL";
        private const string FrostlingPriestessSpellCardId = "FROSTLING_PRIESTESS_SPELL";
        private const string PreciousPearlSpellCardId = "TRINKET_PRECIOUS_PEARL_SPELL";
        private const string OphidianStaffSpellCardId = "TRINKET_OPHIDIAN_STAFF_SPELL";
        private const string CoinPouch3GoldProxyCardId = "TRINKET_COIN_POUCH_3";
        private const string VibrantBubbleSpellCardId = "TRINKET_VIBRANT_BUBBLE_SPELL";
        private const string DoubleStitchNeedleSpellCardId = "TRINKET_DOUBLE_STITCH_NEEDLE_SPELL";
        private const string TokenOfTheOldGodsSpellCardId = "TRINKET_TOKEN_OF_THE_OLD_GODS_SPELL";
        private const string ChillmereMosaicSpellCardId = "TRINKET_CHILLMERE_MOSAIC_SPELL";
        private const string JailerStickerSpellCardId = "TRINKET_JAILER_STICKER_SPELL";
        private const string DemonbloodGourdSpellCardId = "TRINKET_DEMONBLOOD_GOURD_SPELL";
        private const string ShiftingTideSpellCardId = "TRINKET_SHIFTING_TIDE_SPELL";
        private const string TimewarpedGlowscaleSpellCardId = "TIMEWARPED_GLOWSCALE_SPELL";
        private const string TimewarpedEvolvingTavernSpellCardId = "TIMEWARPED_EVOLVING_TAVERN_SPELL";
        private const string WearyMageSpellCardId = "WEARY_MAGE_SPELL";
        private const string ThaumaturgistSpellCardId = "THAUMATURGIST_SPELL";
        private const string HealthyBountyCardId = "BG33_811";
        private const string HostileBountyCardId = "BG33_812";
        private const string SelfishBountyCardId = "BG33_813";
        private const string FriendlyBountyCardId = "BG33_814";
        private const string WealthyBountyCardId = "BG33_815";
        private const string OfficialHealthyBountyCardId = "122182";
        private const string OfficialHostileBountyCardId = "122183";
        private const string OfficialSelfishBountyCardId = "122184";
        private const string OfficialFriendlyBountyCardId = "122185";
        private const string OfficialWealthyBountyCardId = "122186";
        private const string TavernCoinCardId = "104436";
        private const string DarkmoonPrizePocketChangeCardId = "BGS_Treasures_001";
        private const string DarkmoonPrizeGachaGiftCardId = "BGS_Treasures_004";
        private const string DarkmoonPrizeEvolvingTavernCardId = "BGS_Treasures_006";
        private const string DarkmoonPrizeMightOfStormwindCardId = "BGS_Treasures_007";
        private const string DarkmoonPrizeGruulRulesCardId = "BGS_Treasures_009";
        private const string DarkmoonPrizeTimeThiefCardId = "BGS_Treasures_010";
        private const string DarkmoonPrizeTrainingSessionCardId = "BGS_Treasures_011";
        private const string DarkmoonPrizeOnTheHouseCardId = "BGS_Treasures_012";
        private const string DarkmoonPrizeGoodStuffCardId = "BGS_Treasures_013";
        private const string DarkmoonPrizeUnlimitedCoinCardId = "BGS_Treasures_014";
        private const string DarkmoonPrizeBuyTheHolyLightCardId = "BGS_Treasures_015";
        private const string DarkmoonPrizeRaiseTheStakesCardId = "BGS_Treasures_016";
        private const string DarkmoonPrizeRatInACageCardId = "BGS_Treasures_018";
        private const string DarkmoonPrizeBananasCardId = "BGS_Treasures_019";
        private const string DarkmoonPrizeTopShelfCardId = "BGS_Treasures_020";
        private const string DarkmoonPrizeFriendsFamilyDiscountCardId = "BGS_Treasures_022";
        private const string DarkmoonPrizeOpenBarCardId = "BGS_Treasures_023";
        private const string DarkmoonPrizeFreshTabCardId = "BGS_Treasures_025";
        private const string DarkmoonPrizeBouncerCardId = "BGS_Treasures_026";
        private const string DarkmoonPrizeGiveDogBoneCardId = "BGS_Treasures_028";
        private const string DarkmoonPrizeRockingRollingCardId = "BGS_Treasures_029";
        private const string DarkmoonPrizeBigBrannPlayCardId = "BGS_Treasures_030";
        private const string DarkmoonPrizeBigWinnerCardId = "BGS_Treasures_032";
        private const string DarkmoonPrizeNewRecruitCardId = "BGS_Treasures_033";
        private const string DarkmoonPrizeRepeatCustomerCardId = "BGS_Treasures_034";
        private const string DarkmoonPrizeAllThatGlittersCardId = "BGS_Treasures_037";
        private const string DarkmoonPrizeMindflayerGogglesCardId = "BGS_Treasures_039";
        private const string DarkmoonPrizeBananaBunchCardId = "BGS_Treasures_040";
        private const string DarkmoonPrizeUnfurledCodexCardId = "BGS_Treasures_100";
        private const string DarkmoonPrizeMageroyalBlossomCardId = "BGS_Treasures_101";
        private const string DarkmoonPrizeReservePricesCardId = "BGS_Treasures_104";
        private const string DarkmoonPrizeGorgeousGobletCardId = "BGS_Treasures_106";
        private const string DarkmoonPrizeCrystallizationCardId = "BGS_Treasures_110";
        private const string DarkmoonPrizeRockingRefreshCounter = "darkmoon:rocking_and_rolling:free_refreshes_each_turn";
        private const string DarkmoonPrizeOpenBarRefreshCounter = "darkmoon:open_bar:free_refreshes_each_turn";
        private const string DarkmoonPrizeNewRecruitMinShopCardsCounter = "darkmoon:new_recruit:min_shop_cards";
        private const string DarkmoonPrizeUnlimitedCoinReturnCounter = "darkmoon:unlimited_coin:return_count";
        private const string DarkmoonPrizeGruulRulesCounter = "darkmoon_gruul_rules";
        private const string DarkmoonPrizeBigBrannExtraBattlecryCounter = "darkmoon:big_brann_play:extra_battlecries_this_turn";
        private const string DarkmoonPrizeFriendsFamilyDiscountCounter = "darkmoon:friends_family_discount:minion_cost_2";
        private const string LanternLightCardId = "RAKANISHU_LANTERN_LIGHT";
        private const string MuklaBananaCardId = "MUKLA_BANANA";
        private const string BattlecruiserUpgradeCardId = "BATTLECRUISER_UPGRADE";
        private const string BattlecruiserCardId = "BG31_HERO_801pt";
        private const string BattlecruiserUpgradeFreeCounter = "battlecruiser_upgrade_free_remaining";
        private const string BetterSecretProxyCardId = "BETTER_SECRET_PROXY";
        private const string DeepwaterSchoolCardId = "131218";
        private const string ArcaneConsumptionCardId = "130311";
        private const string EnhanceAMaticTauntSpellCardId = "BG24_Reward_715t";
        private const string EnhanceAMaticWindfurySpellCardId = "BG24_Reward_715t2";
        private const string EnhanceAMaticDivineShieldSpellCardId = "BG24_Reward_715t3";
        private const string EnhanceAMaticRebornSpellCardId = "BG24_Reward_715t4";
        private const string RushingWindsSpellCardId = "BG33_Reward_006t";
        private const string LegacyDeepwaterSchoolCardId = "DEEPWATER_SCHOOL";
        private const string LegacyArcaneConsumptionCardId = "ARCANE_CONSUMPTION";
        private const string FireBallerCardId = "BG31_816";
        private const string SnowBallerCardId = "BG31_818";
        private const string DisturbedGraveCounter = "disturbed-grave-round";
        private const string LavaLurkerCardId = "BG23_009";
        private const string TimewarpedLavaLurkerCardId = "BG34_Giant_678";
        private const string TemporarySpellcraftSourceId = "Temporary Spellcraft";
        private const string PermanentSpellcraftSourceId = "Permanent Spellcraft";
        private const string PermanentSpellcraftCounter = "permanent_spellcraft_left";
        private const string LockedTurnsCounter = "locked-turns";
        [ThreadStatic] private static MinionInstance explicitTarget;

        public static string Cast(MinionInstance spell, MatchState state, MinionCatalog minions, SpellCatalog spells, SeededRng rng, int targetIndex = -1, HeroCatalog heroes = null, DarkmoonPrizeCatalog darkmoonPrizes = null)
        {
            if (spell == null || (spell.CardKind != CardKind.TavernSpell && spell.CardKind != CardKind.Spell))
            {
                throw new InvalidOperationException("Target card is not a spell.");
            }

            var previousTarget = explicitTarget;
            explicitTarget = ResolveExplicitTarget(state, targetIndex);
            try
            {
                return CastInternal(spell, state, minions, spells, rng, heroes, darkmoonPrizes);
            }
            finally
            {
                explicitTarget = previousTarget;
            }
        }

        private static string CastInternal(MinionInstance spell, MatchState state, MinionCatalog minions, SpellCatalog spells, SeededRng rng, HeroCatalog heroes, DarkmoonPrizeCatalog darkmoonPrizes)
        {
            var cardNumber = spell.CardId;
            var applyTavernSpellBonus = spell.CardKind == CardKind.TavernSpell;
            if (IsOfficialBattlecruiserUpgrade(cardNumber))
            {
                return ResolveBattlecruiserUpgrade(state, cardNumber, applyTavernSpellBonus);
            }

            switch (cardNumber)
            {
                case BloodGemCardId:
                    Buff(state, FirstFriendlyBoard(state), 1, 1, "Blood Gem", applyTavernSpellBonus);
                    return "Blood Gem: target gains +1/+1";
                case BristlebackBloodGemCardId:
                    var gemTarget = FirstFriendlyBoard(state);
                    Buff(state, gemTarget, 1, 1, "Bristleback Blood Gem", applyTavernSpellBonus);
                    if (gemTarget != null && gemTarget.Tribes.Contains(Tribe.Quilboar))
                    {
                        AddKeyword(gemTarget, Keyword.Taunt);
                    }

                    return "Bristleback Blood Gem: target gains +1/+1 and Quilboar gain Taunt";
                case RebornBloodGemCardId:
                    var rebornGemTarget = FirstFriendlyBoard(state);
                    Buff(state, rebornGemTarget, 1 + state.Player.Tavern.BloodGemBonusAttack, 1 + state.Player.Tavern.BloodGemBonusHealth, "Blood Gem", false);
                    if (rebornGemTarget != null && rebornGemTarget.Tribes.Contains(Tribe.Quilboar))
                    {
                        AddKeyword(rebornGemTarget, Keyword.Reborn);
                    }

                    return "Reborn Blood Gem: target gains Blood Gem stats and Quilboar gain Reborn";
                case DeepSeaAnglerSpellCardId:
                    var anglerTarget = FirstAnyMinion(state);
                    var anglerAttack = spell.Counters != null && spell.Counters.TryGetValue("angler_attack", out var storedAnglerAttack) ? storedAnglerAttack : 2;
                    var anglerHealth = spell.Counters != null && spell.Counters.TryGetValue("angler_health", out var storedAnglerHealth) ? storedAnglerHealth : 6;
                    Buff(state, anglerTarget, anglerAttack, anglerHealth, TemporarySpellcraftSourceId, false);
                    AddKeyword(anglerTarget, Keyword.Taunt);
                    AddTag(anglerTarget, "temporary_spellcraft");
                    return "Deep Sea Angling Spellcraft: target gains stats and Taunt";
                case DeepBlueSpellCardId:
                    var deepTarget = FirstAnyMinion(state);
                    var deepAttack = spell.Counters != null && spell.Counters.TryGetValue("deep_blue_attack", out var storedDeepAttack) ? storedDeepAttack : 2;
                    var deepHealth = spell.Counters != null && spell.Counters.TryGetValue("deep_blue_health", out var storedDeepHealth) ? storedDeepHealth : 2;
                    var deepGrowth = spell.Counters != null && spell.Counters.TryGetValue("deep_blue_growth", out var storedDeepGrowth) ? storedDeepGrowth : 1;
                    Buff(state, deepTarget, deepAttack, deepHealth, ConsumePermanentSpellcraft(deepTarget) ? PermanentSpellcraftSourceId : TemporarySpellcraftSourceId, false);
                    AddTag(deepTarget, "temporary_spellcraft");
                    state.Player.Tavern.DeepBlueBonusAttack += deepGrowth;
                    state.Player.Tavern.DeepBlueBonusHealth += deepGrowth;
                    return "Deep Blue Spellcraft: target gains scaling stats";
                case VolcanicVisitorAttackSpellCardId:
                    var volcanicAttackTarget = FirstAnyMinion(state);
                    var volcanicAttackAmount = spell.Counters != null && spell.Counters.TryGetValue("spellcraft_amount", out var storedVolcanicAttack) ? storedVolcanicAttack : 4;
                    var volcanicAttackPermanent = ConsumePermanentSpellcraft(volcanicAttackTarget);
                    Buff(state, volcanicAttackTarget, volcanicAttackAmount, 0, volcanicAttackPermanent ? PermanentSpellcraftSourceId : TemporarySpellcraftSourceId, false);
                    if (!volcanicAttackPermanent)
                    {
                        AddTag(volcanicAttackTarget, "temporary_spellcraft");
                    }

                    return "Volcanic Visitor Spellcraft: target gains Attack";
                case VolcanicVisitorHealthSpellCardId:
                    var volcanicHealthTarget = FirstAnyMinion(state);
                    var volcanicHealthAmount = spell.Counters != null && spell.Counters.TryGetValue("spellcraft_amount", out var storedVolcanicHealth) ? storedVolcanicHealth : 4;
                    var volcanicHealthPermanent = ConsumePermanentSpellcraft(volcanicHealthTarget);
                    Buff(state, volcanicHealthTarget, 0, volcanicHealthAmount, volcanicHealthPermanent ? PermanentSpellcraftSourceId : TemporarySpellcraftSourceId, false);
                    if (!volcanicHealthPermanent)
                    {
                        AddTag(volcanicHealthTarget, "temporary_spellcraft");
                    }

                    return "Volcanic Visitor Spellcraft: target gains Health";
                case FrostlingPriestessSpellCardId:
                    var frostlingCount = spell.Counters != null && spell.Counters.TryGetValue("spellcraft_multiplier", out var storedFrostlingCount) ? Math.Max(1, storedFrostlingCount) : 1;
                    AddRandomStatTavernSpellsToHand(state, spells, rng, frostlingCount);
                    return "Frostling Priestess Spellcraft: add stat Tavern spell";
                case PreciousPearlSpellCardId:
                    var pearlTarget = FirstAnyMinion(state);
                    Buff(state, pearlTarget, 30, 30, TemporarySpellcraftSourceId, false);
                    AddTag(pearlTarget, "temporary_spellcraft");
                    return "Precious Pearl Spellcraft: target gains +30/+30 until next turn";
                case OphidianStaffSpellCardId:
                    var ophidianTarget = FirstTribeTarget(state, Tribe.Beast);
                    Buff(state, ophidianTarget, 2, 2, TemporarySpellcraftSourceId, false);
                    AddTemporarySpellcraftKeyword(ophidianTarget, Keyword.Reborn);
                    return "Ophidian Staff Spellcraft: Beast gains +2/+2 and Reborn until next turn";
                case VibrantBubbleSpellCardId:
                    var bubbleTarget = FirstTribeTarget(state, Tribe.Murloc);
                    var keyword = RandomBonusKeyword(rng);
                    AddTemporarySpellcraftKeyword(bubbleTarget, keyword);
                    return "Vibrant Bubble Spellcraft: Murloc gains " + keyword + " until next turn";
                case DoubleStitchNeedleSpellCardId:
                    var stitchTarget = FirstFriendlyBoard(state);
                    if (stitchTarget == null)
                    {
                        return "Double Stitch Needle Spellcraft: no friendly target";
                    }

                    if (state.Player.Tavern.Hand.Count >= HandLimit)
                    {
                        return "Double Stitch Needle Spellcraft: hand is full";
                    }

                    var stitchAttack = stitchTarget.Attack;
                    var stitchHealth = stitchTarget.MaxHealth;
                    Buff(state, stitchTarget, stitchAttack, stitchHealth, "Double Stitch Needle", false);
                    stitchTarget.Health = stitchTarget.MaxHealth;
                    state.Player.Board.Remove(stitchTarget);
                    stitchTarget.Counters[LockedTurnsCounter] = 1;
                    AddTag(stitchTarget, "locked_in_hand");
                    state.Player.Tavern.Hand.Add(stitchTarget);
                    return "Double Stitch Needle Spellcraft: target doubled and locked in hand";
                case TokenOfTheOldGodsSpellCardId:
                    var tokenTarget = FirstFriendlyBoard(state);
                    if (TransformMinionOneTierHigher(tokenTarget, minions, rng))
                    {
                        return "Token of the Old Gods Spellcraft: target transformed one Tier higher";
                    }

                    return "Token of the Old Gods Spellcraft: no higher-Tier transform target";
                case ChillmereMosaicSpellCardId:
                    return "Chillmere Mosaic Spellcraft: refresh handled by Trinket";
                case JailerStickerSpellCardId:
                    return "Jailer Sticker Spellcraft: destroy reward handled by Trinket";
                case DemonbloodGourdSpellCardId:
                    return "Demonblood Gourd Spellcraft: devour handled by Trinket";
                case ShiftingTideSpellCardId:
                    return "Shifting Tide Spellcraft: buff handled by Trinket";
                case TimewarpedGlowscaleSpellCardId:
                    AddKeyword(FirstFriendlyBoard(state), Keyword.DivineShield);
                    return "Timewarped Glowscale Spellcraft: target gains Divine Shield";
                case TimewarpedEvolvingTavernSpellCardId:
                    var evolved = RefreshShopWithHigherTierMinions(state, minions, rng);
                    return "Evolving Tavern: evolved " + evolved + " Tavern minion(s)";
                case WearyMageSpellCardId:
                    var wearyTarget = FirstAnyMinion(state);
                    var wearyPermanent = spell.Tags != null && spell.Tags.Contains("permanent_weary_spellcraft");
                    Buff(state, wearyTarget, 2, 2, wearyPermanent ? PermanentSpellcraftSourceId : TemporarySpellcraftSourceId, false);
                    if (wearyPermanent)
                    {
                        if (wearyTarget != null && wearyTarget.Tribes.Contains(Tribe.Naga))
                        {
                            AddKeyword(wearyTarget, Keyword.Reborn);
                        }
                    }
                    else
                    {
                        AddTag(wearyTarget, "temporary_spellcraft");
                        if (wearyTarget != null && wearyTarget.Tribes.Contains(Tribe.Naga))
                        {
                            AddTemporarySpellcraftKeyword(wearyTarget, Keyword.Reborn);
                        }
                    }

                    return wearyPermanent
                        ? "Weary Mage Spellcraft: target permanently gains +2/+2"
                        : "Weary Mage Spellcraft: target gains +2/+2 until next turn";
                case ThaumaturgistSpellCardId:
                    var thaumaturgistTarget = FirstAnyMinion(state);
                    var thaumaturgistAmount = spell.Counters != null && spell.Counters.TryGetValue("spellcraft_amount", out var storedThaumaturgistAmount) ? Math.Max(1, storedThaumaturgistAmount) : 1;
                    var thaumaturgistPermanent = spell.Tags != null && spell.Tags.Contains("permanent_thaumaturgist_spellcraft");
                    var thaumaturgistConsumesPermanent = thaumaturgistPermanent || ConsumePermanentSpellcraft(thaumaturgistTarget);
                    Buff(state, thaumaturgistTarget, thaumaturgistAmount, thaumaturgistAmount, thaumaturgistConsumesPermanent ? PermanentSpellcraftSourceId : TemporarySpellcraftSourceId, false);
                    if (!thaumaturgistConsumesPermanent)
                    {
                        AddTag(thaumaturgistTarget, "temporary_spellcraft");
                    }

                    return thaumaturgistConsumesPermanent
                        ? "Thaumaturgist Spellcraft: target permanently gains +" + thaumaturgistAmount + "/+" + thaumaturgistAmount
                        : "Thaumaturgist Spellcraft: target gains +" + thaumaturgistAmount + "/+" + thaumaturgistAmount + " until next turn";
                case HealthyBountyCardId:
                case OfficialHealthyBountyCardId:
                    BuffAll(state, state.Player.Board.Take(4), 0, 4, "Healthy Bounty", applyTavernSpellBonus);
                    return "Healthy Bounty: four friendly minions gain Health";
                case HostileBountyCardId:
                case OfficialHostileBountyCardId:
                    BuffAll(state, state.Player.Board.Take(4), 4, 0, "Hostile Bounty", applyTavernSpellBonus);
                    return "Hostile Bounty: four friendly minions gain Attack";
                case SelfishBountyCardId:
                case OfficialSelfishBountyCardId:
                    Buff(state, FirstFriendlyBoard(state), 6, 6, "Selfish Bounty", applyTavernSpellBonus);
                    return "Selfish Bounty: friendly minion gains +6/+6";
                case FriendlyBountyCardId:
                case OfficialFriendlyBountyCardId:
                    AddRandomMostCommonTribeMinionToHand(state, minions, heroes, rng, "Friendly Bounty");
                    return "Friendly Bounty: add a minion of your most common type";
                case WealthyBountyCardId:
                case OfficialWealthyBountyCardId:
                    GainGold(state.Player.Tavern, 2);
                    return "Wealthy Bounty: gain 2 Gold";
                case DarkmoonPrizePocketChangeCardId:
                    var pocketChangeCoins = AddTavernSpellCardsToHand(state, spells, TavernCoinCardId, 2, "Pocket Change");
                    return "Pocket Change: get " + pocketChangeCoins + " Tavern Coin(s)";
                case DarkmoonPrizeGachaGiftCardId:
                    StartDiscover(state, minions, rng, 1, "darkmoon-gacha-gift");
                    return "Gacha Gift: discover a Tier 1 minion";
                case DarkmoonPrizeEvolvingTavernCardId:
                    var evolvedDarkmoon = RefreshShopWithHigherTierMinions(state, minions, rng);
                    return "Evolving Tavern: evolved " + evolvedDarkmoon + " Tavern minion(s)";
                case DarkmoonPrizeMightOfStormwindCardId:
                    BuffAll(state, state.Player.Board, Math.Max(1, state.Player.Tavern.Tier), 0, "Might of Stormwind", false);
                    return "Might of Stormwind: friendly minions gain Attack equal to your Tier";
                case DarkmoonPrizeGruulRulesCardId:
                    ApplyGruulRules(state);
                    return "Gruul Rules: target gains end-of-turn +4/+4";
                case DarkmoonPrizeTimeThiefCardId:
                    StartPreviousOpponentWarbandDiscover(state, "darkmoon-time-thief");
                    return "Time Thief: discover from last opponent warband";
                case DarkmoonPrizeTrainingSessionCardId:
                    StartHeroPowerDiscover(state, heroes, rng);
                    return "Training Session: discover a new Hero Power";
                case DarkmoonPrizeOnTheHouseCardId:
                    StartDiscover(state, minions, rng, Math.Max(1, state.Player.Tavern.Tier), "darkmoon-on-the-house");
                    return "On the House: discover a minion of your Tier";
                case DarkmoonPrizeGoodStuffCardId:
                    AddShopGrowth(state, Tribe.All, 1, 1, "The Good Stuff");
                    BuffAll(state, state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), 1, 1, "The Good Stuff", false);
                    return "The Good Stuff: Tavern minions gain +1/+1 this game";
                case DarkmoonPrizeUnlimitedCoinCardId:
                    GainGold(state.Player.Tavern, 1);
                    IncrementAdvancedCounter(state, DarkmoonPrizeUnlimitedCoinReturnCounter, 1);
                    return "The Unlimited Coin: gain 1 Gold and return at end of turn";
                case DarkmoonPrizeBuyTheHolyLightCardId:
                    ResolveBuyTheHolyLight(state);
                    return "Buy the Holy Light: friendly minion gains +10 Attack and Divine Shield";
                case DarkmoonPrizeRaiseTheStakesCardId:
                    ReturnGoldenFriendlyMinionToHand(state);
                    return "Raise the Stakes: make a friendly minion Golden and return it";
                case DarkmoonPrizeRatInACageCardId:
                    DoubleAttackAfterBuff(state, FirstAnyMinion(state), 2, "I'm Still Just a Rat in a Cage");
                    return "I'm Still Just a Rat in a Cage: target gains +2 Attack then doubles Attack";
                case DarkmoonPrizeBananasCardId:
                    AddDarkmoonBananasToHand(state);
                    return "B.A.N.A.N.A.S.: fill hand with Tavern Dish Bananas";
                case DarkmoonPrizeTopShelfCardId:
                    StartHigherTierDiscover(state, minions, rng, "darkmoon-top-shelf");
                    return "Top Shelf: discover a higher-Tier minion";
                case DarkmoonPrizeFriendsFamilyDiscountCardId:
                    SetAdvancedCounterAtLeast(state, DarkmoonPrizeFriendsFamilyDiscountCounter, 1);
                    return "Friends and Family Discount: Tavern minions cost 2 this game";
                case DarkmoonPrizeOpenBarCardId:
                    state.Player.Tavern.FreeRefreshes = StatMath.SaturatingAdd(state.Player.Tavern.FreeRefreshes, 5, 0, StatMath.MaxStat);
                    IncrementAdvancedCounter(state, DarkmoonPrizeOpenBarRefreshCounter, 5);
                    return "Open Bar: gain 5 free Refreshes each turn";
                case DarkmoonPrizeFreshTabCardId:
                    GainGold(state.Player.Tavern, 12);
                    return "Fresh Tab: gain 12 Gold";
                case DarkmoonPrizeBouncerCardId:
                    DoubleHealthAfterTaunt(state, FirstFriendlyBoard(state), "The Bouncer");
                    return "The Bouncer: friendly minion gains Taunt then doubles Health";
                case DarkmoonPrizeGiveDogBoneCardId:
                    ResolveGiveDogBone(state);
                    return "Give a Dog a Bone: friendly minion gains Divine Shield, Windfury, and +15/+15";
                case DarkmoonPrizeRockingRollingCardId:
                    IncrementAdvancedCounter(state, DarkmoonPrizeRockingRefreshCounter, 1);
                    return "Rocking and Rolling: gain 1 free Refresh at the start of each turn";
                case DarkmoonPrizeBigBrannPlayCardId:
                    IncrementAdvancedCounter(state, DarkmoonPrizeBigBrannExtraBattlecryCounter, 1);
                    return "Big Brann Play: Battlecries trigger an extra time this turn";
                case DarkmoonPrizeBigWinnerCardId:
                    StartBigWinnerDarkmoonDiscovers(state, darkmoonPrizes, rng);
                    return "Big Winner!: discover Darkmoon Prizes from Tiers 1, 2, and 3";
                case DarkmoonPrizeNewRecruitCardId:
                    SetAdvancedCounterAtLeast(state, DarkmoonPrizeNewRecruitMinShopCardsCounter, 7);
                    AddShopGrowth(state, Tribe.All, 2, 2, "New Recruit");
                    BuffAll(state, state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), 2, 2, "New Recruit", false);
                    return "New Recruit: Tavern offers an extra minion with +2/+2 this game";
                case DarkmoonPrizeRepeatCustomerCardId:
                    ReturnFriendlyNonGoldenMinionToHand(state);
                    return "Repeat Customer: return a friendly non-Golden minion with +6/+6";
                case DarkmoonPrizeAllThatGlittersCardId:
                    MakeGolden(RandomShopMinion(state, rng));
                    return "All That Glitters: random Tavern minion becomes Golden";
                case DarkmoonPrizeMindflayerGogglesCardId:
                    StealShopAndRefresh(state, minions, rng);
                    return "Mindflayer Goggles: steal the Tavern and refresh it";
                case DarkmoonPrizeBananaBunchCardId:
                    AddDarkmoonBananasToHand(state, 2);
                    return "Banana Bunch: get 2 Tavern Dish Bananas";
                case DarkmoonPrizeUnfurledCodexCardId:
                    AddRandomTavernSpellToHand(state, spells, rng, spellDefinition => spellDefinition.Cost >= 2, "Unfurled Codex");
                    return "Unfurled Codex: get a random Tavern spell that costs 2 or more";
                case DarkmoonPrizeMageroyalBlossomCardId:
                    StartTavernSpellDiscover(state, spells, rng, Math.Max(1, state.Player.Tavern.Tier), "darkmoon-mageroyal-blossom");
                    return "Mageroyal Blossom: discover a Tavern spell of your Tier";
                case DarkmoonPrizeReservePricesCardId:
                    state.Player.Tavern.NextTavernSpellCostReduction = StatMath.SaturatingAdd(
                        state.Player.Tavern.NextTavernSpellCostReduction,
                        1,
                        0,
                        StatMath.MaxStat);
                    return "Reserve Prices: Tavern spells cost (1) less this turn";
                case DarkmoonPrizeGorgeousGobletCardId:
                    AddRandomTavernSpellsToFillHand(state, spells, rng, "Gorgeous Goblet");
                    return "Gorgeous Goblet: fill your hand with random Tavern spells";
                case DarkmoonPrizeCrystallizationCardId:
                    state.Player.Tavern.TavernSpellBonusAttack = StatMath.SaturatingAdd(state.Player.Tavern.TavernSpellBonusAttack, 1, 0, StatMath.MaxStat);
                    state.Player.Tavern.TavernSpellBonusHealth = StatMath.SaturatingAdd(state.Player.Tavern.TavernSpellBonusHealth, 1, 0, StatMath.MaxStat);
                    return "Crystallization: Tavern spells give an extra +1/+1 this game";
                case LanternLightCardId:
                    var lanternAmount = spell.Counters != null && spell.Counters.TryGetValue("lantern_amount", out var storedLanternAmount)
                        ? Math.Max(1, storedLanternAmount)
                        : Math.Max(1, state.Player.Tavern.Tier);
                    Buff(state, FirstAnyMinion(state), lanternAmount, lanternAmount, "Lantern Light", false);
                    return "Lantern Light: target gains +" + lanternAmount + "/+" + lanternAmount;
                case MuklaBananaCardId:
                    Buff(state, FirstFriendlyBoard(state), 1, 1, "Banana", applyTavernSpellBonus);
                    return "Banana: friendly minion gains +1/+1";
                case BattlecruiserUpgradeCardId:
                    var battlecruiser = state.Player.Board.FirstOrDefault(card =>
                        card.Tags.Contains("battlecruiser") ||
                        card.Name.IndexOf("Battlecruiser", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (battlecruiser == null)
                    {
                        return "Battlecruiser Upgrade: no Battlecruiser to upgrade";
                    }

                    Buff(state, battlecruiser, 3, 3, "Battlecruiser Upgrade", applyTavernSpellBonus);
                    return "Battlecruiser Upgrade: Battlecruiser gains +3/+3";
                case BetterSecretProxyCardId:
                    Buff(state, FirstFriendlyBoard(state), 2, 2, "Better Secret", false);
                    return "Better Secret proxy: left-most minion gains +2/+2";
                case DeepwaterSchoolCardId:
                case LegacyDeepwaterSchoolCardId:
                    ResolveDeepwaterClan(state, applyTavernSpellBonus);
                    return "Deepwater Clan: target minion and friendly Murlocs gain stats";
                case "DEEPWATER_SCHOOL_COPY":
                    AddRandomTribeMinionAndCopyToHand(state, minions, rng, Tribe.Murloc, "Deepwater School");
                    return "Deepwater School: add a Murloc and a copy";
                case ArcaneConsumptionCardId:
                case LegacyArcaneConsumptionCardId:
                    ResolveArcaneConsumption(state);
                    return "Arcane Consumption: friendly Elemental gains half of highest Health Tavern minion stats";
                case EnhanceAMaticTauntSpellCardId:
                    ResolveQuestKeywordSpell(state, 5, 5, Keyword.Taunt, "Mega Horn");
                    return "Mega Horn: target gains +5/+5 and Taunt";
                case EnhanceAMaticWindfurySpellCardId:
                    ResolveQuestKeywordSpell(state, 5, 5, Keyword.Windfury, "Blazing Blades");
                    return "Blazing Blades: target gains +5/+5 and Windfury";
                case EnhanceAMaticDivineShieldSpellCardId:
                    ResolveQuestKeywordSpell(state, 5, 5, Keyword.DivineShield, "Bunker Plating");
                    return "Bunker Plating: target gains +5/+5 and Divine Shield";
                case EnhanceAMaticRebornSpellCardId:
                    ResolveQuestKeywordSpell(state, 5, 5, Keyword.Reborn, "Death Rewinder");
                    return "Death Rewinder: target gains +5/+5 and Reborn";
                case RushingWindsSpellCardId:
                    ResolveRushingWinds(state);
                    return "Rushing Winds: target gains Windfury and Divine Shield";
                case ReefRifferSpellCardId:
                    var reefMultiplier = spell.Counters != null && spell.Counters.TryGetValue("spellcraft_multiplier", out var storedMultiplier) ? Math.Max(1, storedMultiplier) : 1;
                    var reefAmount = Math.Max(1, state.Player.Tavern.Tier) * reefMultiplier;
                    var reefTarget = FirstAnyMinion(state);
                    Buff(state, reefTarget, reefAmount, reefAmount, ConsumePermanentSpellcraft(reefTarget) ? PermanentSpellcraftSourceId : TemporarySpellcraftSourceId, false);
                    return "Reef Riffer Spellcraft: target gains +" + reefAmount + "/+" + reefAmount;
                case SurfNSurfSpellCardId:
                    var surfTarget = FirstAnyMinion(state);
                    var permanentSurf = ConsumePermanentSpellcraft(surfTarget);
                    var hadDeathrattle = surfTarget != null && surfTarget.Keywords.Contains(Keyword.Deathrattle);
                    AddKeyword(surfTarget, Keyword.Deathrattle);
                    AddTag(surfTarget, "surf_n_surf_crab");
                    if (surfTarget != null)
                    {
                        surfTarget.Counters["surf_crab_attack"] = spell.Counters != null && spell.Counters.TryGetValue("crab_attack", out var crabAttack) ? crabAttack : 3;
                        surfTarget.Counters["surf_crab_health"] = spell.Counters != null && spell.Counters.TryGetValue("crab_health", out var crabHealth) ? crabHealth : 2;
                        AddTag(surfTarget, permanentSurf ? "permanent_spellcraft" : "temporary_spellcraft");
                        if (!hadDeathrattle && !permanentSurf)
                        {
                            AddTag(surfTarget, "temporary_spellcraft_added_deathrattle");
                        }
                    }

                    return "Surf n' Surf Spellcraft: target gains a Crab Deathrattle";
                case SlimyShieldCardId:
                    var shieldTarget = FirstAnyMinion(state);
                    Buff(state, shieldTarget, 1, 1, "Slimy Shield", applyTavernSpellBonus);
                    AddKeyword(shieldTarget, Keyword.Taunt);
                    return "Slimy Shield: target gains +1/+1 and Taunt";
                case "100596":
                    var arrowAttack = spell.Golden || (spell.Tags != null && spell.Tags.Contains("anomaly_golden_arrow")) ? 8 : 4;
                    Buff(state, FirstAnyMinion(state), arrowAttack, 0, arrowAttack > 4 ? "Golden Arrow" : "Sharp Arrow", applyTavernSpellBonus);
                    return arrowAttack > 4 ? "Golden Arrow: target gains +8 Attack" : "Sharp Arrow: target gains +4 Attack";
                case "103791":
                    Buff(state, FirstAnyMinion(state), 0, 3, "Fortify", applyTavernSpellBonus);
                    AddKeyword(FirstAnyMinion(state), Keyword.Taunt);
                    return "Fortify: target gains +3 Health and Taunt";
                case "105752":
                    Buff(state, FirstAnyMinion(state), 2, 2, "Fruit Plate", applyTavernSpellBonus);
                    return "婵☆偓绲鹃悷銊╁疾瑜斿绋款煥閸涱喚锛橀梺鎸庣⊕濮樸劌煤娴兼潙鍐€闁搞儺鍓氶鐟懊归悩鍙夊攭妤犵偛绻愰?2/+2";
                case "103796":
                    AddKeyword(FirstAnyMinion(state), Keyword.DivineShield);
                    return "Divine Gift: target gains Divine Shield";
                case "104601":
                    SetStats(FirstAnyMinion(state), 20, 20);
                    return "闁诲海鎳撻惉鑲╂閵娿劊浜归柕蹇ョ秬閺変粙鏌ㄥ☉娆愮殤婵炶弓鍗冲浠嬪炊椤掍緡鍚傛繛瀵稿Т妤犳瓕銇愭径瀣枖?0/20";
                case "104445":
                    Buff(state, FirstFriendlyBoard(state), 6, 6, "Defender Rites", applyTavernSpellBonus);
                    AddKeyword(FirstFriendlyBoard(state), Keyword.Taunt);
                    return "Defender Rites: friendly minion gains +6/+6 and Taunt";
                case "105667":
                    var pantsTarget = FirstAnyMinion(state);
                    Buff(state, pantsTarget, 1, 2, "Tricky Trousers", applyTavernSpellBonus);
                    ToggleKeyword(pantsTarget, Keyword.Taunt);
                    return "Tricky Trousers: target gains +1/+2 and toggles Taunt";
                case TavernCoinCardId:
                    GainGold(state.Player.Tavern, 1);
                    return "Tavern Coin: gain 1 Gold";
                case "103779":
                    state.Player.Tavern.NextTurnBonusGold += 2;
                    return "Careful Investment: gain 2 Gold next turn";
                case "104029":
                    state.Player.Tavern.MaxGold += 1;
                    return "闂備胶鏅划顖滄暜娴兼潙鍌ㄩ柣鏃€鐡曡棢闂佹寧绋掑畝鎼佸箺瀹曞洦鏆滃ù锝夘棑閻熸劙姊婚崟顒€濮堢憸棰佺劍椤?";
                case "104446":
                    state.Player.Tavern.FreeRefreshes += 2;
                    return "Quick Look: gain 2 free Refreshes";
                case "104559":
                    GainGold(state.Player.Tavern, 1);
                    return "Desperate Dig: gain Gold";
                case "105267":
                    state.Player.Tavern.PendingCombatWinGold += 3;
                    state.Player.Tavern.PendingCombatDrawGold += 1;
                    return "Hired Headhunter: bank combat outcome Gold";
                case "127288":
                    StartLockedCurrentTierDiscover(state, minions, rng, "Search the Ages");
                    return "Search the Ages: discover a current-tier minion and lock it";
                case "105664":
                    AddSameTribeMinionToHand(state, minions, rng, FirstAnyMinion(state), "Chef Choice");
                    return "Chef Choice: get another minion of the same type";
                case "103785":
                    state.Player.Armor = 5;
                    return "Armor Stash: set Armor to 5";
                case "103793":
                    AddRandomMinionToHand(state, minions, rng, 1, "Recruit Minion");
                    return "Recruit a minion: add a random minion to hand";
                case "105665":
                    state.Player.Tavern.NextCombatBoardAttack += 2;
                    state.Player.Tavern.NextCombatBoardHealth += 1;
                    return "Fleeting Vigor: next combat board buff";
                case "122864":
                    StartDiscover(state, minions, rng, 1, "Tier 1 Discover");
                    return "Discover a Tier 1 minion";
                case "119718":
                    StartDiscover(state, minions, rng, 7, "Tier 7 Discover");
                    return "Discover a Tier 7 minion";
                case "105669":
                    StartMajorityTribeDiscover(state, minions, heroes, rng, "Planar Telescope");
                    return "Planar Telescope: discover majority tribe minion";
                case "109230":
                    BuffAll(state, state.Player.Board, 1, 1, "Shiny Ring", applyTavernSpellBonus);
                    return "Shiny Ring: your minions gain +1/+1";
                case CoinPouch3GoldProxyCardId:
                    state.Player.Tavern.Gold = StatMath.SaturatingAdd(state.Player.Tavern.Gold, 3, 0, StatMath.MaxStat);
                    return "3-Gold Coin Pouch: gain 3 Gold";
                case "113901":
                    TransformFirstMinionOneTierHigher(state, minions, rng);
                    return "Steady Mutation: transform first minion one tier higher";
                case "117573":
                    BuffAll(state, state.Player.Board, 2, 2, "Time Management", applyTavernSpellBonus);
                    return "Time Management: deterministic immediate +2/+2 choice";
                case "122489":
                    BuffAllTemporary(state, state.Player.Board, 3, 1, "Soulbound Carapace");
                    return "Soulbound Carapace: temporary board buff";
                case "122862":
                    SellMinionAndBuffLeftmostElemental(state);
                    return "Cascading Avalanche: sell a minion and buff leftmost Elemental";
                case "109232":
                    BuffAll(state, state.Player.Board, 4, 4, "Board Buff", applyTavernSpellBonus);
                    return "Board Buff: your minions gain +4/+4";
                case "131152":
                    BuffAll(state, state.Player.Board.Take(4), 1, 2, "Might of Stormwind", applyTavernSpellBonus);
                    return "Might of Stormwind: four friendly minions gain +1/+2";
                case "110401":
                    state.Player.Tavern.NextCombatBeetles += 2;
                    return "Boon of Beetles: summon two 1/1 Taunt Beetles next combat";
                case "130310":
                    ResolveConflagration(state, applyTavernSpellBonus);
                    return "Conflagration: first minion gains scaling Elemental stats";
                case "130312":
                    ResolveEonarsFavor(state, applyTavernSpellBonus);
                    return "Eonar's Favor: same-type Tavern minions gain +3/+3 this game";
                case "131153":
                    ResolveBackToBack(state, applyTavernSpellBonus);
                    return "Back to Back: target minion gains scaling stats";
                case "110412":
                    ResolveButchering(state);
                    return "Butchering: destroy a friendly Undead and improve your Undead attack";
                case "130713":
                    ResolveQueensCommand(state, applyTavernSpellBonus);
                    return "Queen's Command: friendly minions gain stats and Naga gain extra";
                case "113902":
                    state.Player.Tavern.HelpfulRefreshes += 2;
                    return "Knockoff Wisdomball: next two refreshes are helpful";
                case "130527":
                    ResolveMenagerieTableware(state, applyTavernSpellBonus);
                    return "Menagerie Tableware: board gains stats for each friendly minion type";
                case "100899":
                    ResolveInvokeTheDevourer(state);
                    return "Invoke the Devourer: sell a minion and pass its stats";
                case "100910":
                case "EBG_Spell_037":
                    StartHeroPowerDiscover(state, heroes, rng);
                    return "Unmasked Identity: discover a new Hero Power";
                case "104494":
                    RefreshShopWithTavernSpells(state, spells, rng);
                    return "Top Shelf: refresh the Tavern into Tavern Spells";
                case "104560":
                    state.Player.Tavern.NextCombatEnemyHealthToOne += 1;
                    return "Overconfidence: next combat sets one enemy Health to 1";
                case "105264":
                    StartTaggedMinionDiscover(state, minions, rng, HasBattlecry, "Head Hunting");
                    return "Head Hunting: discover a Battlecry minion";
                case "105265":
                    StartTaggedMinionDiscover(state, minions, rng, minion => minion.Keywords.Contains(Keyword.Deathrattle), "Recruit a Relic");
                    return "Recruit a Relic: discover a Deathrattle minion";
                case "122899":
                    BuffAll(state, state.Player.Board.Where(minion => minion.Keywords.Contains(Keyword.DivineShield)), 6, 0, "Sacred Gift", applyTavernSpellBonus);
                    return "Sacred Gift: Divine Shield minions gain +6 Attack";
                case "127503":
                    state.Player.Tavern.NextCombatLeftmostDoubleAttack = true;
                    return "Nozdormu's Offspring: next combat doubles leftmost minion Attack";
                case "127506":
                    BuffAll(state, state.Player.Board, 3, 2, "Golden Frenzy", applyTavernSpellBonus);
                    BuffAll(state, state.Player.Board.Where(minion => minion.Golden), 3, 2, "Golden Frenzy Golden", applyTavernSpellBonus);
                    return "Golden Frenzy: your minions gain +3/+2 and Golden minions gain extra +3/+2";
                case "105271":
                    BuffOneOfEachTribe(state, state.Player.Board, 2, 2, "Chaotic Tea Set", applyTavernSpellBonus);
                    return "Chaotic Tea Set: one friendly minion of each type gains +2/+2";
                case "104472":
                    BuffSameTribeAsTarget(state, CurrentBoardAndShopMinions(state), FirstAnyMinion(state), 3, 3, "Natural Blessing", applyTavernSpellBonus);
                    return "Natural Blessing: same-type board and Tavern minions gain +3/+3";
                case "105903":
                    BuffAll(state, state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), 1, 2, "Unexpected Fruit", applyTavernSpellBonus);
                    return "Unexpected Fruit: Tavern minions gain +1/+2";
                case "105276":
                    AddShopGrowth(state, Tribe.All, 2, 2, "Plenty Staff");
                    BuffAll(state, state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), 2, 2, "Plenty Staff", applyTavernSpellBonus);
                    return "Plenty Staff: Tavern minions gain +2/+2 this game";
                case "104448":
                    MakeGolden(RandomShopMinion(state, rng));
                    return "Golden Touch: random Tavern minion becomes Golden";
                case "104502":
                    StealRandomShopMinion(state, rng);
                    return "Enchanted Lasso: steal a random Tavern minion";
                case "110400":
                    AddRandomTribeMinionAndCopyToHand(state, minions, rng, Tribe.Murloc, "Cloning Conch");
                    return "Cloning Conch: get a random Murloc and a copy";
                case "110406":
                case "110407":
                    AddSpellcraftBundleToHand(state, spells, rng);
                    return "Special: get temporary Spellcraft spells";
                case "110642":
                    ApplyBloodGemsAndStealAdjacentGems(state, FirstFriendlyBoard(state));
                    return "Blood Gem Scraper: play Blood Gems and steal adjacent Gems";
                case "117670":
                    AddMinionByCardIdToHand(state, minions, FireBallerCardId, "ballers-fire");
                    AddMinionByCardIdToHand(state, minions, SnowBallerCardId, "ballers-snow");
                    return "Ballers: add Fire Baller and Snow Baller to hand";
                case "120900":
                    ApplyShiftingTide(state, FirstAnyMinion(state), applyTavernSpellBonus);
                    return "Shifting Tide: swap stats and give +2/+2";
                case "123553":
                    state.Player.Tavern.TemporaryAvengeBeastRewards += 1;
                    return "Beast reward: enable temporary Avenge Beast rewards";
                case "126909":
                    state.Player.Tavern.RefreshRightmostBuffAttack = StatMath.SaturatingAdd(state.Player.Tavern.RefreshRightmostBuffAttack, 5, 0, StatMath.MaxStat);
                    state.Player.Tavern.RefreshRightmostBuffHealth = StatMath.SaturatingAdd(state.Player.Tavern.RefreshRightmostBuffHealth, 5, 0, StatMath.MaxStat);
                    return "Rightmost Refresh Buff: after refresh, rightmost Tavern minion gets +5/+5";
                case "126957":
                    StartTribeDiscoverWithTag(state, minions, rng, Tribe.Undead, "Undead Discover", "discover_then_death");
                    return "Undead Discover: discover an Undead that dies later";
                case "126676":
                    var barrageAttack = StatMath.SaturatingAdd(
                        StatMath.SaturatingAdd(1, state.Player.Tavern.TavernSpellBonusAttack, 0, StatMath.MaxStat),
                        state.Player.Tavern.BloodGemBonusAttack,
                        0,
                        StatMath.MaxStat);
                    var barrageHealth = StatMath.SaturatingAdd(
                        StatMath.SaturatingAdd(1, state.Player.Tavern.TavernSpellBonusHealth, 0, StatMath.MaxStat),
                        state.Player.Tavern.BloodGemBonusHealth,
                        0,
                        StatMath.MaxStat);
                    state.Player.Tavern.Growth.ShopModifiers.Add(new TavernGrowthModifier
                    {
                        Scope = BuffScope.ShopGlobal,
                        Tribe = Tribe.All,
                        Attack = barrageAttack,
                        Health = barrageHealth,
                        SourceId = "Blood Gem Barrage"
                    });
                    return "Blood Gem Barrage: create a Blood Gem spell using Tavern spell bonuses";
                case "100601":
                    MakeGolden(FirstFriendlyBoard(state)?.TavernTier <= 4
                        ? FirstFriendlyBoard(state)
                        : state.Player.Board.FirstOrDefault(minion => minion.TavernTier <= 4));
                    return "Eyes of the Earth Mother: golden the first Tier 4 or lower friendly minion";
                case "100911":
                    RefreshShopToTargetTribe(state, minions, rng);
                    return "Hamuul's Lost Staff: refresh the Tavern into the target type";
                case "119599":
                    state.Player.Tavern.NextCombatLeftmostCopiesNearestEnemyStats = true;
                    return "Share the Love: next combat leftmost minion copies nearest enemy stats";
                case "119603":
                    SetFirstFriendlyToBestTeamStats(state);
                    return "Blade of Ambition: target gains the best team stats";
                case "127642":
                    state.Player.Tavern.NextCombatTriggerMixedMechanics = true;
                    return "Hand of Deus: next combat triggers a battlecry deathrattle and rally reward";
                default:
                    return spell.Name + ": effect is not implemented yet";
            }
        }

        private static MinionInstance FirstAnyMinion(MatchState state)
        {
            return ExplicitAnyTarget(state) ?? FirstFriendlyBoard(state) ?? state.Player.Tavern.Shop.FirstOrDefault(card => card != null && card.CardKind == CardKind.Minion);
        }

        private static MinionInstance FirstTribeTarget(MatchState state, Tribe tribe)
        {
            var explicitTarget = ExplicitAnyTarget(state);
            if (MatchesTribe(explicitTarget, tribe))
            {
                return explicitTarget;
            }

            return state.Player.Board.FirstOrDefault(minion => MatchesTribe(minion, tribe))
                ?? state.Player.Tavern.Shop.FirstOrDefault(card => card != null && card.CardKind == CardKind.Minion && MatchesTribe(card, tribe));
        }

        private static MinionInstance FirstFriendlyBoard(MatchState state)
        {
            return ExplicitFriendlyBoardTarget(state) ?? state.Player.Board.FirstOrDefault();
        }

        private static MinionInstance ResolveExplicitTarget(MatchState state, int targetIndex)
        {
            if (state == null || targetIndex < 0 || targetIndex >= state.Player.Board.Count)
            {
                return null;
            }

            return state.Player.Board[targetIndex];
        }

        private static MinionInstance ExplicitAnyTarget(MatchState state)
        {
            if (explicitTarget == null)
            {
                return null;
            }

            return state.Player.Board.FirstOrDefault(minion => minion.InstanceId == explicitTarget.InstanceId)
                ?? state.Player.Tavern.Shop.FirstOrDefault(card => card != null && card.InstanceId == explicitTarget.InstanceId);
        }

        private static MinionInstance ExplicitFriendlyBoardTarget(MatchState state)
        {
            if (explicitTarget == null)
            {
                return null;
            }

            return state.Player.Board.FirstOrDefault(minion => minion.InstanceId == explicitTarget.InstanceId);
        }

        private static IEnumerable<MinionInstance> CurrentBoardAndShopMinions(MatchState state)
        {
            return state.Player.Board
                .Concat(state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion));
        }

        private static MinionInstance RandomShopMinion(MatchState state, SeededRng rng)
        {
            var candidates = state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
            return candidates.Count == 0 ? null : rng.Pick(candidates);
        }

        private static bool IsOfficialBattlecruiserUpgrade(string cardId)
        {
            return !string.IsNullOrEmpty(cardId) &&
                cardId.StartsWith("BG31_HERO_801pt", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(cardId, BattlecruiserCardId, StringComparison.OrdinalIgnoreCase);
        }

        private static MinionInstance FindBattlecruiser(MatchState state)
        {
            return state?.Player?.Board?.FirstOrDefault(card =>
                card != null &&
                (string.Equals(card.CardId, BattlecruiserCardId, StringComparison.OrdinalIgnoreCase) ||
                 (card.Tags != null && card.Tags.Contains("battlecruiser")) ||
                 (!string.IsNullOrEmpty(card.Name) && card.Name.IndexOf("Battlecruiser", StringComparison.OrdinalIgnoreCase) >= 0)));
        }

        private static string ResolveBattlecruiserUpgrade(MatchState state, string cardId, bool applyTavernSpellBonus)
        {
            var battlecruiser = FindBattlecruiser(state);
            if (battlecruiser == null)
            {
                return "Battlecruiser Upgrade: no Battlecruiser to upgrade";
            }

            var family = BattlecruiserUpgradeFamily(cardId);
            var level = Math.Max(1, BattlecruiserUpgradeLevel(cardId));
            battlecruiser.Counters["battlecruiser_upgrade:" + family] = level;

            if (family == "a")
            {
                Buff(state, battlecruiser, level + 1, 0, "Hyperflight Rotors", applyTavernSpellBonus);
            }
            else if (family == "b")
            {
                Buff(state, battlecruiser, 0, level + 1, "Smart Servos", applyTavernSpellBonus);
            }
            else if (family == "c")
            {
                AddTag(battlecruiser, "battlecruiser_yamato");
                battlecruiser.Counters["battlecruiser_yamato"] = Math.Max(1, level);
            }
            else if (family == "d")
            {
                AddKeyword(battlecruiser, Keyword.Rally);
                AddTag(battlecruiser, "battlecruiser_ballistics");
                battlecruiser.Counters["battlecruiser_ballistics_attack"] = level + 1;
            }
            else if (family == "e")
            {
                AddKeyword(battlecruiser, Keyword.Deathrattle);
                AddTag(battlecruiser, "battlecruiser_caduceus");
                battlecruiser.Counters["battlecruiser_caduceus_attack"] = level + 1;
                battlecruiser.Counters["battlecruiser_caduceus_health"] = level + 1;
            }
            else if (family == "f")
            {
                AddTag(battlecruiser, "battlecruiser_advanced_construction");
                battlecruiser.Counters[BattlecruiserUpgradeFreeCounter] = Math.Max(1, level);
            }
            else if (family == "h")
            {
                AddTag(battlecruiser, "battlecruiser_bunker_magnetic");
            }
            else if (family == "i")
            {
                AddTag(battlecruiser, "battlecruiser_missile_pod");
                battlecruiser.Counters["battlecruiser_missile_pod"] = Math.Max(1, level);
            }
            else if (family == "j")
            {
                AddKeyword(battlecruiser, Keyword.Reborn);
                AddTag(battlecruiser, "battlecruiser_full_health_reborn");
            }

            return "Battlecruiser Upgrade: applied " + cardId + " to Battlecruiser";
        }

        private static string BattlecruiserUpgradeFamily(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || cardId.Length <= "BG31_HERO_801pt".Length)
            {
                return string.Empty;
            }

            return cardId.Substring("BG31_HERO_801pt".Length, 1).ToLowerInvariant();
        }

        private static int BattlecruiserUpgradeLevel(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                return 1;
            }

            var prefixLength = "BG31_HERO_801pt".Length + 1;
            if (cardId.Length <= prefixLength)
            {
                return 1;
            }

            var digits = cardId.Substring(prefixLength);
            if (string.IsNullOrEmpty(digits))
            {
                return 1;
            }

            return int.TryParse(digits, out var parsed) ? Math.Max(1, parsed) : 1;
        }

        private static void Buff(MatchState state, MinionInstance target, int attack, int health, string sourceId, bool applyTavernSpellBonus)
        {
            if (target == null)
            {
                return;
            }

            if (applyTavernSpellBonus && (attack != 0 || health != 0))
            {
                attack = StatMath.SaturatingAdd(attack, state.Player.Tavern.TavernSpellBonusAttack);
                health = StatMath.SaturatingAdd(health, state.Player.Tavern.TavernSpellBonusHealth);
            }

            StatMath.ApplyStatDelta(target, attack, health);
            target.Enchantments.Add(new Enchantment
            {
                Id = sourceId,
                SourceId = sourceId,
                AttackBonus = attack,
                HealthBonus = health
            });
            RefreshScarletSurvivor(target);
        }

        private static void BuffAll(MatchState state, IEnumerable<MinionInstance> targets, int attack, int health, string sourceId, bool applyTavernSpellBonus)
        {
            foreach (var target in targets.Where(target => target != null))
            {
                Buff(state, target, attack, health, sourceId, applyTavernSpellBonus);
            }
        }

        private static void BuffOneOfEachTribe(MatchState state, IEnumerable<MinionInstance> targets, int attack, int health, string sourceId, bool applyTavernSpellBonus)
        {
            foreach (var target in BoardTribeAnalyzer.SelectOneOfEachTribe(targets))
            {
                Buff(state, target, attack, health, sourceId, applyTavernSpellBonus);
            }
        }

        private static void BuffSameTribeAsTarget(MatchState state, IEnumerable<MinionInstance> board, MinionInstance target, int attack, int health, string sourceId, bool applyTavernSpellBonus)
        {
            if (target == null)
            {
                return;
            }

            var tribes = BoardTribeAnalyzer.GetCountedTribes(target);
            BuffAll(
                state,
                board.Where(minion => BoardTribeAnalyzer.GetCountedTribes(minion).Any(tribes.Contains)),
                attack,
                health,
                sourceId,
                applyTavernSpellBonus);
        }

        private static void SetStats(MinionInstance target, int attack, int health)
        {
            if (target == null)
            {
                return;
            }

            target.Attack = attack;
            target.MaxHealth = health;
            target.Health = health;
            RefreshScarletSurvivor(target);
        }

        private static void AddKeyword(MinionInstance target, Keyword keyword)
        {
            if (target != null && !target.Keywords.Contains(keyword))
            {
                target.Keywords.Add(keyword);
            }
        }

        private static void ToggleKeyword(MinionInstance target, Keyword keyword)
        {
            if (target == null)
            {
                return;
            }

            if (target.Keywords.Contains(keyword))
            {
                target.Keywords.Remove(keyword);
                return;
            }

            target.Keywords.Add(keyword);
        }

        private static void GainGold(TavernState tavern, int amount)
        {
            tavern.Gold += amount;
        }

        private static void IncrementAdvancedCounter(MatchState state, string key, int amount)
        {
            if (state?.Player?.Tavern?.AdvancedMechanics?.Counters == null)
            {
                return;
            }

            state.Player.Tavern.AdvancedMechanics.Counters.TryGetValue(key, out var current);
            state.Player.Tavern.AdvancedMechanics.Counters[key] = StatMath.SaturatingAdd(current, amount, 0, StatMath.MaxStat);
        }

        private static void SetAdvancedCounterAtLeast(MatchState state, string key, int value)
        {
            if (state?.Player?.Tavern?.AdvancedMechanics?.Counters == null)
            {
                return;
            }

            state.Player.Tavern.AdvancedMechanics.Counters.TryGetValue(key, out var current);
            state.Player.Tavern.AdvancedMechanics.Counters[key] = Math.Max(current, value);
        }

        private static int AddTavernSpellCardsToHand(MatchState state, SpellCatalog spells, string cardNumber, int count, string source)
        {
            if (state == null || spells == null || count <= 0)
            {
                return 0;
            }

            var definition = spells.All.FirstOrDefault(spell => spell.CardNumber == cardNumber || spell.Id == cardNumber);
            if (definition == null)
            {
                return 0;
            }

            var added = 0;
            while (added < count && state.Player.Tavern.Hand.Count < HandLimit)
            {
                state.Player.Tavern.Hand.Add(MinionFactory.Create(
                    definition,
                    BoardSide.Player,
                    "spell-" + source + "-" + state.Round + "-" + state.Player.Tavern.Hand.Count));
                added += 1;
            }

            return added;
        }

        private static int AddRandomTavernSpellToHand(MatchState state, SpellCatalog spells, SeededRng rng, Func<TavernSpellDefinition, bool> predicate, string source)
        {
            if (state == null || spells == null || state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return 0;
            }

            var candidates = spells.All
                .Where(spell => spell.InPool && spell.Category == "TavernSpell" && (predicate == null || predicate(spell)))
                .ToList();
            if (candidates.Count == 0)
            {
                return 0;
            }

            var picked = rng.Pick(candidates);
            state.Player.Tavern.Hand.Add(MinionFactory.Create(
                picked,
                BoardSide.Player,
                "spell-" + source + "-" + state.Round + "-" + state.Player.Tavern.Hand.Count));
            return 1;
        }

        private static void AddRandomTavernSpellsToFillHand(MatchState state, SpellCatalog spells, SeededRng rng, string source)
        {
            while (state.Player.Tavern.Hand.Count < HandLimit)
            {
                if (AddRandomTavernSpellToHand(state, spells, rng, null, source) == 0)
                {
                    return;
                }
            }
        }

        private static void AddShopGrowth(MatchState state, Tribe tribe, int attack, int health, string sourceId)
        {
            state.Player.Tavern.Growth.ShopModifiers.Add(new TavernGrowthModifier
            {
                Scope = BuffScope.ShopGlobal,
                Tribe = tribe,
                Attack = attack,
                Health = health,
                SourceId = sourceId
            });
        }

        private static void AddRandomMinionToHand(MatchState state, MinionCatalog catalog, SeededRng rng, int exactTier, string suffix)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var candidates = catalog.All.Where(minion => minion.InPool && minion.TavernTier == exactTier).ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            state.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "spell-" + suffix + "-" + state.Round, false, PoolSource.Copy, 0));
        }

        private static void AddMinionByCardIdToHand(MatchState state, MinionCatalog catalog, string cardId, string source)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var definition = catalog.All.FirstOrDefault(minion => minion.CardId == cardId);
            if (definition == null)
            {
                return;
            }

            state.Player.Tavern.Hand.Add(MinionFactory.Create(definition, BoardSide.Player, "spell-" + source + "-" + state.Round + "-" + state.Player.Tavern.Hand.Count, false, PoolSource.Copy, 0));
        }

        private static void AddRandomTribeMinionAndCopyToHand(MatchState state, MinionCatalog catalog, SeededRng rng, Tribe tribe, string source)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var candidates = catalog.All
                .Where(minion => minion.InPool && MatchesTribe(minion, tribe))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var picked = rng.Pick(candidates);
            var first = MinionFactory.Create(picked, BoardSide.Player, "spell-" + source + "-" + state.Round + "-0", false, PoolSource.Copy, 0);
            state.Player.Tavern.Hand.Add(first);
            if (state.Player.Tavern.Hand.Count < HandLimit)
            {
                var copy = first.Clone();
                copy.InstanceId = first.InstanceId + "-copy";
                copy.PoolSource = PoolSource.Copy;
                copy.PoolCopiesHeld = 0;
                state.Player.Tavern.Hand.Add(copy);
            }
        }

        private static void AddRandomMostCommonTribeMinionToHand(MatchState state, MinionCatalog catalog, HeroCatalog heroes, SeededRng rng, string source)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var tribe = BoardTribeAnalyzer.GetMostCommonTribe(state.Player);
            if (tribe == Tribe.None)
            {
                tribe = Tribe.All;
            }

            var maxTier = Math.Max(1, state.Player.Tavern.Tier);
            var candidates = catalog.All
                .Where(minion => IsStateMinionAvailable(state, minion) && minion.TavernTier <= maxTier && MatchesTribe(minion, tribe))
                .ToList();
            var buddyCandidates = BuddyPoolCandidates(state, heroes, maxTier, tribe);
            var candidateCount = candidates.Count + buddyCandidates.Count;
            if (candidateCount == 0)
            {
                return;
            }

            var pickedIndex = rng.NextInt(candidateCount);
            state.Player.Tavern.Hand.Add(pickedIndex < candidates.Count
                ? MinionFactory.Create(candidates[pickedIndex], BoardSide.Player, "spell-" + source + "-" + state.Round, false, PoolSource.Copy, 0)
                : MinionFactory.Create(buddyCandidates[pickedIndex - candidates.Count], BoardSide.Player, "spell-" + source + "-" + state.Round, PoolSource.Copy, 0));
        }

        private static void StartDiscover(MatchState state, MinionCatalog catalog, SeededRng rng, int exactTier, string source)
        {
            var candidates = catalog.All.Where(minion => minion.InPool && minion.TavernTier == exactTier).ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "discover-" + source + "-" + options.Count, false, PoolSource.Copy, 0));
            }

            state.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = source,
                RewardTier = exactTier,
                Options = options
            });
        }

        private static void StartTavernSpellDiscover(MatchState state, SpellCatalog catalog, SeededRng rng, int exactTier, string source)
        {
            if (catalog == null)
            {
                return;
            }

            var candidates = catalog.All
                .Where(spell => spell.InPool && spell.Category == "TavernSpell" && spell.TavernTier == exactTier)
                .ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "discover-" + source + "-" + options.Count));
            }

            state.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = source,
                RewardTier = exactTier,
                Options = options
            });
        }

        private static void StartHigherTierDiscover(MatchState state, MinionCatalog catalog, SeededRng rng, string source)
        {
            var tier = Math.Min(7, Math.Max(1, state.Player.Tavern.Tier) + 1);
            StartDiscover(state, catalog, rng, tier, source);
        }

        private static void ResolveBuyTheHolyLight(MatchState state)
        {
            var target = ExplicitFriendlyBoardTarget(state) ?? FirstFriendlyBoard(state);
            if (target == null)
            {
                return;
            }

            Buff(state, target, 10, 0, "Buy the Holy Light", false);
            AddKeyword(target, Keyword.DivineShield);
        }

        private static void AddDarkmoonBananasToHand(MatchState state)
        {
            while (state.Player.Tavern.Hand.Count < HandLimit)
            {
                state.Player.Tavern.Hand.Add(CreateDarkmoonBananaCard(state));
            }
        }

        private static void AddDarkmoonBananasToHand(MatchState state, int count)
        {
            for (var index = 0; index < count && state.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                state.Player.Tavern.Hand.Add(CreateDarkmoonBananaCard(state));
            }
        }

        private static MinionInstance CreateDarkmoonBananaCard(MatchState state)
        {
            var suffix = state.Round + "-" + state.Player.Tavern.Hand.Count;
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "darkmoon-banana-" + suffix,
                DefinitionId = MuklaBananaCardId,
                CardId = MuklaBananaCardId,
                Name = "Tavern Dish Banana",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Text = "Give a friendly minion +1/+1.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_spell", "generated_tavern_spell", "banana", "tavern_dish_banana", "targeted_spell" }
            };
        }

        private static void ReturnFriendlyNonGoldenMinionToHand(MatchState state)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var target = ExplicitFriendlyBoardTarget(state);
            if (target == null || target.Golden)
            {
                target = state.Player.Board.FirstOrDefault(minion => minion != null && !minion.Golden);
            }

            if (target == null)
            {
                return;
            }

            state.Player.Board.Remove(target);
            Buff(state, target, 6, 6, "Repeat Customer", false);
            target.Owner = BoardSide.Player;
            target.PoolSource = PoolSource.Copy;
            target.PoolCopiesHeld = 0;
            state.Player.Tavern.Hand.Add(target);
        }

        private static void ReturnGoldenFriendlyMinionToHand(MatchState state)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var target = ExplicitFriendlyBoardTarget(state) ?? FirstFriendlyBoard(state);
            if (target == null)
            {
                return;
            }

            MakeGolden(target);
            state.Player.Board.Remove(target);
            target.Owner = BoardSide.Player;
            target.PoolSource = PoolSource.Copy;
            target.PoolCopiesHeld = 0;
            state.Player.Tavern.Hand.Add(target);
        }

        private static void DoubleAttackAfterBuff(MatchState state, MinionInstance target, int attack, string source)
        {
            if (target == null)
            {
                return;
            }

            Buff(state, target, attack, 0, source, false);
            var doubled = target.Attack;
            StatMath.ApplyStatDelta(target, doubled, 0);
            target.Enchantments.Add(new Enchantment
            {
                Id = source + " Double Attack",
                SourceId = source,
                AttackBonus = doubled,
                HealthBonus = 0
            });
            RefreshScarletSurvivor(target);
        }

        private static void DoubleHealthAfterTaunt(MatchState state, MinionInstance target, string source)
        {
            if (target == null)
            {
                return;
            }

            AddKeyword(target, Keyword.Taunt);
            var doubled = Math.Max(0, target.MaxHealth);
            Buff(state, target, 0, doubled, source, false);
            target.Health = target.MaxHealth;
        }

        private static void ResolveGiveDogBone(MatchState state)
        {
            var target = FirstFriendlyBoard(state);
            if (target == null)
            {
                return;
            }

            Buff(state, target, 15, 15, "Give a Dog a Bone", false);
            AddKeyword(target, Keyword.DivineShield);
            AddKeyword(target, Keyword.Windfury);
        }

        private static void ApplyGruulRules(MatchState state)
        {
            var target = FirstFriendlyBoard(state);
            if (target == null)
            {
                return;
            }

            target.Counters.TryGetValue(DarkmoonPrizeGruulRulesCounter, out var current);
            target.Counters[DarkmoonPrizeGruulRulesCounter] = StatMath.SaturatingAdd(current, 1, 0, StatMath.MaxStat);
        }

        private static void StartPreviousOpponentWarbandDiscover(MatchState state, string source)
        {
            var previous = state?.OpponentHistory?.LastOpponentWarband ?? new List<MinionInstance>();
            var options = previous
                .Where(card => card != null)
                .Take(3)
                .Select((card, index) =>
                {
                    var copy = card.Clone();
                    copy.InstanceId = "discover-" + source + "-" + index;
                    copy.Owner = BoardSide.Player;
                    copy.PoolSource = PoolSource.Discover;
                    copy.PoolCopiesHeld = 0;
                    copy.CanReturnToPoolAfterAttach = false;
                    return copy;
                })
                .ToList();
            if (options.Count == 0)
            {
                return;
            }

            state.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = source,
                RewardTier = Math.Max(1, state.Player.Tavern.Tier),
                Options = options
            });
        }

        private static void StartBigWinnerDarkmoonDiscovers(MatchState state, DarkmoonPrizeCatalog catalog, SeededRng rng)
        {
            if (catalog == null)
            {
                return;
            }

            for (var tier = 1; tier <= 3; tier += 1)
            {
                QueueDarkmoonPrizeDiscover(state, catalog, rng, tier, "darkmoon-big-winner-tier-" + tier);
            }
        }

        private static void QueueDarkmoonPrizeDiscover(MatchState state, DarkmoonPrizeCatalog catalog, SeededRng rng, int tier, string source)
        {
            var candidates = DarkmoonPrizeEngine.SelectOfferableDefinitions(catalog, tier).ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                var option = DarkmoonPrizeEngine.CreatePrizeCard(definition, source + "-" + options.Count);
                option.PoolSource = PoolSource.Discover;
                option.OriginPoolSource = PoolSource.Discover;
                option.PoolCopiesHeld = 0;
                options.Add(option);
            }

            if (options.Count == 0)
            {
                return;
            }

            state.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = source,
                RewardTier = tier,
                Options = options
            });
        }

        private static void StealShopAndRefresh(MatchState state, MinionCatalog catalog, SeededRng rng)
        {
            var tavern = state.Player.Tavern;
            foreach (var card in tavern.Shop.Where(card => card != null).ToList())
            {
                if (tavern.Hand.Count >= HandLimit)
                {
                    break;
                }

                card.Owner = BoardSide.Player;
                card.PoolSource = PoolSource.Copy;
                card.PoolCopiesHeld = 0;
                tavern.Hand.Add(card);
            }

            var size = Math.Max(1, tavern.Shop.Count == 0 ? TavernRules.GetShopSize(tavern.Tier) : tavern.Shop.Count);
            var candidates = catalog.All
                .Where(minion => minion.InPool && minion.TavernTier <= Math.Max(1, tavern.Tier))
                .ToList();
            var refreshed = new List<MinionInstance>();
            for (var index = 0; index < size && candidates.Count > 0; index += 1)
            {
                var pickedIndex = rng.NextInt(candidates.Count);
                var picked = candidates[pickedIndex];
                candidates.RemoveAt(pickedIndex);
                refreshed.Add(MinionFactory.Create(picked, BoardSide.Player, "mindflayer-goggles-" + state.Round + "-" + index, false, PoolSource.Copy, 0));
            }

            TavernShopSlots.ReplaceShop(tavern, refreshed);
        }

        private static void StartHeroPowerDiscover(MatchState state, HeroCatalog catalog, SeededRng rng)
        {
            if (catalog == null)
            {
                return;
            }

            var candidates = catalog.GetOfferableDiscoverableHeroPowers(state.Player.HeroPowerCardId);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(catalog.CreateDiscoverableHeroPowerOption(definition, BoardSide.Player, "unmasked-identity-" + options.Count));
            }

            state.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = "hero-power:unmasked-identity",
                RewardTier = 0,
                Options = options
            });
        }

        private static void StartMajorityTribeDiscover(MatchState state, MinionCatalog catalog, HeroCatalog heroes, SeededRng rng, string source)
        {
            var tribe = BoardTribeAnalyzer.GetMostCommonTribe(state.Player);
            if (tribe == Tribe.None)
            {
                tribe = Tribe.All;
            }

            var maxTier = Math.Max(1, state.Player.Tavern.Tier);
            var candidates = catalog.All
                .Where(minion => IsStateMinionAvailable(state, minion) && minion.TavernTier <= maxTier && MatchesTribe(minion, tribe))
                .ToList();
            var buddyCandidates = BuddyPoolCandidates(state, heroes, maxTier, tribe);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count + buddyCandidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count + buddyCandidates.Count);
                if (index < candidates.Count)
                {
                    var definition = candidates[index];
                    candidates.RemoveAt(index);
                    options.Add(MinionFactory.Create(definition, BoardSide.Player, "discover-" + source + "-" + options.Count, false, PoolSource.Discover, 0));
                    continue;
                }

                var buddyIndex = index - candidates.Count;
                var buddy = buddyCandidates[buddyIndex];
                buddyCandidates.RemoveAt(buddyIndex);
                options.Add(MinionFactory.Create(buddy, BoardSide.Player, "discover-" + source + "-" + options.Count, PoolSource.Discover, 0));
            }

            state.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = source,
                RewardTier = Math.Max(1, state.Player.Tavern.Tier),
                Options = options
            });
        }

        private static bool IsStateMinionAvailable(MatchState state, MinionDefinition minion)
        {
            if (minion == null || !minion.InPool || !TribeAvailabilityRules.IsMinionAvailable(minion, state?.ActiveTribes))
            {
                return false;
            }

            return state == null ||
                state.IsDefaultCardPoolVersion ||
                (minion.Tags != null && minion.Tags.Contains("oathstone_summoning")) ||
                (state.EnabledMinionCardIds != null && state.EnabledMinionCardIds.Contains(minion.CardId, StringComparer.OrdinalIgnoreCase));
        }

        private static List<HeroBuddyDefinition> BuddyPoolCandidates(MatchState state, HeroCatalog heroes, int maxTier, Tribe tribe)
        {
            var buddyPool = state?.Player?.Tavern?.BuddyPool;
            if (heroes?.AllBuddies == null || buddyPool == null)
            {
                return new List<HeroBuddyDefinition>();
            }

            return heroes.AllBuddies
                .Where(buddy =>
                    buddy != null &&
                    !string.IsNullOrEmpty(buddy.CardId) &&
                    !buddy.ExcludedFromBuddyDiscover &&
                    buddy.TavernTier <= maxTier &&
                    buddyPool.TryGetValue(buddy.CardId, out var remaining) &&
                    remaining > 0 &&
                    IsBuddyTribeAvailable(state, buddy) &&
                    MatchesTribe(buddy, tribe))
                .ToList();
        }

        private static bool IsBuddyTribeAvailable(MatchState state, HeroBuddyDefinition buddy)
        {
            if (buddy.Tribes == null || buddy.Tribes.Count == 0 || buddy.Tribes.All(tribe => tribe == Tribe.None))
            {
                return true;
            }

            if (buddy.Tribes.Contains(Tribe.All))
            {
                return true;
            }

            var active = TribeAvailabilityRules.Normalize(state?.ActiveTribes);
            return buddy.Tribes.Any(active.Contains);
        }

        private static void StartTribeDiscoverWithTag(MatchState state, MinionCatalog catalog, SeededRng rng, Tribe tribe, string source, string tag)
        {
            var candidates = catalog.All
                .Where(minion => minion.InPool && MatchesTribe(minion, tribe))
                .ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                var option = MinionFactory.Create(definition, BoardSide.Player, "discover-" + source + "-" + options.Count, false, PoolSource.Discover, 0);
                AddTag(option, tag);
                option.Counters[DisturbedGraveCounter] = state.Round;
                options.Add(option);
            }

            state.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = source,
                RewardTier = Math.Max(1, state.Player.Tavern.Tier),
                Options = options
            });
        }

        private static void TransformFirstMinionOneTierHigher(MatchState state, MinionCatalog catalog, SeededRng rng)
        {
            TransformMinionOneTierHigher(FirstAnyMinion(state), catalog, rng);
        }

        private static bool TransformMinionOneTierHigher(MinionInstance target, MinionCatalog catalog, SeededRng rng)
        {
            if (target == null)
            {
                return false;
            }

            var nextTier = Math.Min(7, Math.Max(1, target.TavernTier) + 1);
            var candidates = catalog.All.Where(minion => minion.InPool && minion.TavernTier == nextTier).ToList();
            if (candidates.Count == 0)
            {
                return false;
            }

            var attack = target.Attack;
            var health = target.Health;
            var maxHealth = target.MaxHealth;
            var picked = rng.Pick(candidates);
            target.DefinitionId = picked.Id;
            target.CardId = picked.CardId;
            target.Name = picked.Name;
            target.TavernTier = picked.TavernTier;
            target.Tribes = new List<Tribe>(picked.Tribes);
            target.Keywords = new List<Keyword>(picked.Keywords);
            target.Text = picked.Text;
            target.ImagePath = picked.ImagePath;
            target.Attack = attack;
            target.Health = Math.Max(1, health);
            target.MaxHealth = Math.Max(1, maxHealth);
            return true;
        }

        private static void BuffAllTemporary(MatchState state, IEnumerable<MinionInstance> targets, int attack, int health, string sourceId)
        {
            foreach (var target in targets.Where(target => target != null))
            {
                Buff(state, target, attack, health, TemporarySpellcraftSourceId, false);
                AddTag(target, "temporary_spellcraft");
            }
        }

        private static void ApplyShiftingTide(MatchState state, MinionInstance target, bool applyTavernSpellBonus)
        {
            if (target == null)
            {
                return;
            }

            var repeats = target.Tribes.Contains(Tribe.Naga) ? 4 : 2;
            for (var index = 0; index < repeats; index += 1)
            {
                Buff(state, target, 2, 2, "Naga Repeat Buff", applyTavernSpellBonus);
            }
        }

        private static void ResolveConflagration(MatchState state, bool applyTavernSpellBonus)
        {
            var target = FirstAnyMinion(state);
            var amount = 2 + Math.Max(0, state.Player.Tavern.ElementalsPlayedThisTurn);
            Buff(state, target, amount, amount, "Conflagration", applyTavernSpellBonus);
        }

        private static void ResolveEonarsFavor(MatchState state, bool applyTavernSpellBonus)
        {
            var target = FirstAnyMinion(state);
            if (target == null)
            {
                return;
            }

            var tribes = BoardTribeAnalyzer.GetCountedTribes(target);
            if (target.Tribes != null && target.Tribes.Contains(Tribe.All))
            {
                tribes = new List<Tribe> { Tribe.All };
            }

            if (tribes.Count == 0)
            {
                tribes.Add(Tribe.All);
            }

            foreach (var tribe in tribes)
            {
                AddShopGrowth(state, tribe, 3, 3, "Eonar's Favor");
            }

            BuffAll(
                state,
                state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion && MatchesAnyTribe(card, tribes)),
                3,
                3,
                "Eonar's Favor",
                applyTavernSpellBonus);
        }

        private static void ResolveBackToBack(MatchState state, bool applyTavernSpellBonus)
        {
            var amount = 2 + Math.Max(0, state.Player.Tavern.BackToBackBonus);
            Buff(state, FirstAnyMinion(state), amount, amount, "Back to Back", applyTavernSpellBonus);
            state.Player.Tavern.BackToBackBonus += 2;
        }

        private static void ResolveDeepwaterClan(MatchState state, bool applyTavernSpellBonus)
        {
            var target = FirstAnyMinion(state);
            Buff(state, target, 2, 2, "Deepwater Clan", applyTavernSpellBonus);
            BuffAll(
                state,
                state.Player.Board.Where(minion => minion.Tribes.Contains(Tribe.Murloc) && (target == null || minion.InstanceId != target.InstanceId)),
                2,
                2,
                "Deepwater Clan Murlocs",
                applyTavernSpellBonus);
        }

        private static void ResolveQuestKeywordSpell(MatchState state, int attack, int health, Keyword keyword, string source)
        {
            var target = FirstAnyMinion(state);
            Buff(state, target, attack, health, source, false);
            AddKeyword(target, keyword);
        }

        private static void ResolveRushingWinds(MatchState state)
        {
            var target = FirstAnyMinion(state);
            AddKeyword(target, Keyword.Windfury);
            AddKeyword(target, Keyword.DivineShield);
        }

        private static void ResolveButchering(MatchState state)
        {
            var target = ExplicitFriendlyBoardTarget(state);
            if (target == null || !target.Tribes.Contains(Tribe.Undead))
            {
                target = state.Player.Board.FirstOrDefault(minion => minion.Tribes.Contains(Tribe.Undead));
            }

            if (target != null)
            {
                state.Player.Board.Remove(target);
            }

            state.Player.Tavern.UndeadAttackBonus += 4;
            foreach (var undead in state.Player.Board.Where(minion => minion.Tribes.Contains(Tribe.Undead)))
            {
                Buff(state, undead, 4, 0, "Butchering", false);
            }
        }

        private static void ResolveQueensCommand(MatchState state, bool applyTavernSpellBonus)
        {
            BuffAll(state, state.Player.Board, 2, 2, "Queen's Command", applyTavernSpellBonus);
            BuffAll(state, state.Player.Board.Where(minion => minion.Tribes.Contains(Tribe.Naga)), 2, 2, "Queen's Command Naga", applyTavernSpellBonus);
        }

        private static void ResolveMenagerieTableware(MatchState state, bool applyTavernSpellBonus)
        {
            var typeCount = BoardTribeAnalyzer.CountDistinctTribes(state.Player.Board);
            if (typeCount <= 0)
            {
                return;
            }

            BuffAll(state, state.Player.Board, 3 * typeCount, 3 * typeCount, "Menagerie Tableware", applyTavernSpellBonus);
        }

        private static void ResolveInvokeTheDevourer(MatchState state)
        {
            var sold = ExplicitFriendlyBoardTarget(state) ?? state.Player.Board.FirstOrDefault();
            if (sold == null)
            {
                return;
            }

            state.Player.Board.Remove(sold);
            var target = state.Player.Board.FirstOrDefault();
            if (target != null)
            {
                Buff(state, target, sold.Attack, sold.MaxHealth, "Invoke the Devourer", false);
            }
        }

        private static void RefreshShopWithTavernSpells(MatchState state, SpellCatalog spells, SeededRng rng)
        {
            var candidates = spells.All
                .Where(spell => spell.InPool && spell.Category == "TavernSpell" && spell.TavernTier <= Math.Max(1, state.Player.Tavern.Tier))
                .ToList();
            var size = Math.Max(1, state.Player.Tavern.Shop.Count == 0 ? TavernRules.GetShopSize(state.Player.Tavern.Tier) : state.Player.Tavern.Shop.Count);
            state.Player.Tavern.Shop.Clear();
            for (var index = 0; index < size && candidates.Count > 0; index += 1)
            {
                var pickedIndex = rng.NextInt(candidates.Count);
                var picked = candidates[pickedIndex];
                candidates.RemoveAt(pickedIndex);
                state.Player.Tavern.Shop.Add(MinionFactory.Create(picked, BoardSide.Player, "top-shelf-" + state.Round + "-" + index));
            }
        }

        private static int RefreshShopWithHigherTierMinions(MatchState state, MinionCatalog catalog, SeededRng rng)
        {
            if (state?.Player?.Tavern?.Shop == null || catalog == null)
            {
                return 0;
            }

            var evolved = 0;
            var shop = state.Player.Tavern.Shop;
            for (var index = 0; index < shop.Count; index += 1)
            {
                var current = shop[index];
                if (current == null || current.CardKind != CardKind.Minion)
                {
                    continue;
                }

                var nextTier = Math.Min(TavernRules.MaxTavernTier, Math.Max(1, current.TavernTier) + 1);
                var candidates = catalog.All
                    .Where(minion => minion.InPool && minion.TavernTier == nextTier)
                    .ToList();
                if (candidates.Count == 0)
                {
                    continue;
                }

                var replacement = MinionFactory.Create(
                    rng.Pick(candidates),
                    BoardSide.Player,
                    "evolving-tavern-" + state.Round + "-" + index,
                    false,
                    PoolSource.Copy,
                    0);
                if (current.Tags != null && current.Tags.Contains("frozen"))
                {
                    AddTag(replacement, "frozen");
                }

                shop[index] = replacement;
                evolved += 1;
            }

            return evolved;
        }

        private static void StartTaggedMinionDiscover(MatchState state, MinionCatalog catalog, SeededRng rng, Func<MinionDefinition, bool> predicate, string source)
        {
            var candidates = catalog.All
                .Where(minion => minion.InPool && minion.TavernTier <= Math.Max(1, state.Player.Tavern.Tier) && predicate(minion))
                .ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "discover-" + source + "-" + options.Count, false, PoolSource.Discover, 0));
            }

            state.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = source,
                RewardTier = Math.Max(1, state.Player.Tavern.Tier),
                Options = options
            });
        }

        private static bool HasBattlecry(MinionDefinition minion)
        {
            return minion.Keywords.Contains(Keyword.Battlecry)
                || minion.Tags.Any(tag => tag.IndexOf("battlecry", StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrWhiteSpace(minion.Text) && minion.Text.IndexOf("Battlecry", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void RefreshShopToTargetTribe(MatchState state, MinionCatalog catalog, SeededRng rng)
        {
            var target = FirstAnyMinion(state);
            var tribes = BoardTribeAnalyzer.GetCountedTribes(target);
            if (tribes.Count == 0)
            {
                tribes.Add(Tribe.All);
            }

            var candidates = catalog.All
                .Where(minion => minion.InPool && minion.TavernTier <= Math.Max(1, state.Player.Tavern.Tier) && MatchesAnyTribe(minion, tribes))
                .ToList();
            var size = Math.Max(1, state.Player.Tavern.Shop.Count == 0 ? TavernRules.GetShopSize(state.Player.Tavern.Tier) : state.Player.Tavern.Shop.Count);
            state.Player.Tavern.Shop.Clear();
            for (var index = 0; index < size && candidates.Count > 0; index += 1)
            {
                var pickedIndex = rng.NextInt(candidates.Count);
                var picked = candidates[pickedIndex];
                candidates.RemoveAt(pickedIndex);
                state.Player.Tavern.Shop.Add(MinionFactory.Create(picked, BoardSide.Player, "hamuul-" + state.Round + "-" + index));
            }
        }

        private static void SetFirstFriendlyToBestTeamStats(MatchState state)
        {
            var target = FirstFriendlyBoard(state);
            if (target == null)
            {
                return;
            }

            var bestAttack = state.Player.Board.Max(minion => minion.Attack);
            var bestHealth = state.Player.Board.Max(minion => minion.MaxHealth);
            SetStats(target, bestAttack, bestHealth);
        }

        private static void ResolveArcaneConsumption(MatchState state)
        {
            var target = ExplicitAnyTarget(state);
            if (target == null || !target.Tribes.Contains(Tribe.Elemental))
            {
                target = state.Player.Board.FirstOrDefault(minion => minion.Tribes.Contains(Tribe.Elemental))
                    ?? state.Player.Tavern.Shop.FirstOrDefault(card => card != null && card.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Elemental));
            }

            var consumed = state.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .OrderByDescending(card => card.MaxHealth)
                .FirstOrDefault();
            if (target == null || consumed == null)
            {
                return;
            }

            Buff(state, target, Math.Max(1, consumed.Attack / 2), Math.Max(1, consumed.MaxHealth / 2), "Arcane Consumption", false);
        }

        private static void ApplyBloodGemsAndStealAdjacentGems(MatchState state, MinionInstance target)
        {
            if (target == null)
            {
                return;
            }

            ApplyBloodGem(state, target, "Blood Gem");
            ApplyBloodGem(state, target, "Blood Gem");

            var board = state.Player.Board;
            var index = board.FindIndex(minion => minion.InstanceId == target.InstanceId);
            if (index < 0)
            {
                return;
            }

            foreach (var adjacentIndex in new[] { index - 1, index + 1 })
            {
                if (adjacentIndex < 0 || adjacentIndex >= board.Count)
                {
                    continue;
                }

                var adjacent = board[adjacentIndex];
                var gems = adjacent.Enchantments
                    .Where(enchantment => enchantment.SourceId == "Blood Gem" || enchantment.SourceId == "Blood Gem Growth")
                    .ToList();
                foreach (var gem in gems)
                {
                    StatMath.ApplyStatDeltaPreservingDamage(
                        adjacent,
                        StatMath.SaturatingSubtract(0, gem.AttackBonus),
                        StatMath.SaturatingSubtract(0, gem.HealthBonus));
                    StatMath.ApplyStatDelta(target, gem.AttackBonus, gem.HealthBonus);
                    target.Enchantments.Add(new Enchantment
                    {
                        Id = "Stolen Blood Gem",
                        SourceId = "Stolen Blood Gem",
                        AttackBonus = gem.AttackBonus,
                        HealthBonus = gem.HealthBonus
                    });
                    adjacent.Enchantments.Remove(gem);
                }
            }
        }

        private static void ApplyBloodGem(MatchState state, MinionInstance target, string sourceId)
        {
            Buff(state, target, 1 + state.Player.Tavern.BloodGemBonusAttack, 1 + state.Player.Tavern.BloodGemBonusHealth, sourceId, false);
            if (target.Enchantments.Count > 0)
            {
                target.Enchantments[target.Enchantments.Count - 1].SourceId = "Blood Gem";
            }
        }

        private static void SellMinionAndBuffLeftmostElemental(MatchState state)
        {
            var elemental = state.Player.Board.FirstOrDefault(minion => minion.Tribes.Contains(Tribe.Elemental));
            if (elemental == null)
            {
                return;
            }

            var sold = state.Player.Board.FirstOrDefault(minion => minion.InstanceId != elemental.InstanceId)
                ?? state.Player.Board.FirstOrDefault();
            if (sold == null)
            {
                return;
            }

            state.Player.Board.Remove(sold);
            if (sold.InstanceId != elemental.InstanceId)
            {
                Buff(state, elemental, sold.Attack, sold.MaxHealth, "Cascading Avalanche", false);
            }
        }

        private static void StartLockedCurrentTierDiscover(MatchState state, MinionCatalog catalog, SeededRng rng, string source)
        {
            var exactTier = Math.Max(1, state.Player.Tavern.Tier);
            var candidates = catalog.All.Where(minion => minion.InPool && minion.TavernTier == exactTier).ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                var option = MinionFactory.Create(definition, BoardSide.Player, "discover-" + source + "-" + options.Count, false, PoolSource.Discover, 0);
                option.Counters[LockedTurnsCounter] = 1;
                AddTag(option, "locked_in_hand");
                options.Add(option);
            }

            state.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = source,
                RewardTier = exactTier,
                Options = options
            });
        }

        private static void AddSameTribeMinionToHand(MatchState state, MinionCatalog catalog, SeededRng rng, MinionInstance target, string source)
        {
            if (target == null || state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var tribes = BoardTribeAnalyzer.GetCountedTribes(target);
            if (tribes.Count == 0)
            {
                return;
            }

            var candidates = catalog.All
                .Where(minion => minion.InPool && minion.CardId != target.CardId && MatchesAnyTribe(minion, tribes))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            state.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "spell-" + source + "-" + state.Round, false, PoolSource.Copy, 0));
        }

        private static void MakeGolden(MinionInstance target)
        {
            if (target == null || target.Golden)
            {
                return;
            }

            target.Golden = true;
            StatMath.DoubleCurrentStats(target, false);
            RefreshScarletSurvivor(target);
        }

        private static void RefreshScarletSurvivor(MinionInstance target)
        {
            if (target != null && target.CardId == ScarletSurvivorCardId && target.Attack >= 6 && !target.Keywords.Contains(Keyword.DivineShield))
            {
                target.Keywords.Add(Keyword.DivineShield);
            }
        }

        private static void StealRandomShopMinion(MatchState state, SeededRng rng)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var candidates = state.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null && item.Card.CardKind == CardKind.Minion)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var picked = rng.Pick(candidates);
            state.Player.Tavern.Shop[picked.Index] = null;
            picked.Card.Owner = BoardSide.Player;
            picked.Card.PoolSource = PoolSource.Copy;
            state.Player.Tavern.Hand.Add(picked.Card);
        }

        private static void AddSpellcraftBundleToHand(MatchState state, SpellCatalog spells, SeededRng rng)
        {
            var candidates = spells.All.Where(spell => spell.Category == "TavernSpell" && spell.TavernTier <= Math.Max(1, state.Player.Tavern.Tier)).ToList();
            for (var count = 0; count < 3 && state.Player.Tavern.Hand.Count < HandLimit && candidates.Count > 0; count += 1)
            {
                var card = MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "spellcraft-" + state.Round + "-" + count);
                AddTag(card, "generated_spell");
                AddTag(card, "spellcraft");
                AddTag(card, "temporary_spellcraft_card");
                state.Player.Tavern.Hand.Add(card);
            }
        }

        private static void AddRandomStatTavernSpellsToHand(MatchState state, SpellCatalog spells, SeededRng rng, int count)
        {
            var candidates = spells.All
                .Where(spell => spell.InPool &&
                    spell.Category == "TavernSpell" &&
                    spell.TavernTier <= Math.Max(1, state.Player.Tavern.Tier) &&
                    (spell.Tags.Contains("buff_spell") ||
                        spell.Tags.Contains("targeted_spell") ||
                        (!string.IsNullOrWhiteSpace(spell.Text) && (spell.Text.Contains("+") || spell.Text.Contains("stats")))))
                .ToList();
            for (var index = 0; index < count && state.Player.Tavern.Hand.Count < HandLimit && candidates.Count > 0; index += 1)
            {
                var picked = rng.Pick(candidates);
                var card = MinionFactory.Create(picked, BoardSide.Player, "frostling-spellcraft-" + state.Round + "-" + index);
                AddTag(card, "generated_spell");
                AddTag(card, "spellcraft");
                AddTag(card, "stat_tavern_spell");
                state.Player.Tavern.Hand.Add(card);
            }
        }

        private static bool MatchesTribe(MinionDefinition minion, Tribe tribe)
        {
            return tribe == Tribe.All || minion.Tribes.Contains(tribe) || minion.Tribes.Contains(Tribe.All);
        }

        private static bool MatchesTribe(HeroBuddyDefinition buddy, Tribe tribe)
        {
            var tribes = buddy.Tribes ?? new List<Tribe>();
            return tribe == Tribe.All || tribes.Contains(tribe) || tribes.Contains(Tribe.All);
        }

        private static bool MatchesTribe(MinionInstance minion, Tribe tribe)
        {
            return minion != null &&
                (tribe == Tribe.All || minion.Tribes.Contains(tribe) || minion.Tribes.Contains(Tribe.All));
        }

        private static bool MatchesAnyTribe(MinionDefinition minion, IEnumerable<Tribe> tribes)
        {
            var tribeList = (tribes ?? Enumerable.Empty<Tribe>()).ToList();
            var minionTribes = minion.Tribes ?? new List<Tribe>();
            return tribeList.Contains(Tribe.All) || minionTribes.Contains(Tribe.All) || minionTribes.Any(tribeList.Contains);
        }

        private static bool MatchesAnyTribe(MinionInstance minion, IEnumerable<Tribe> tribes)
        {
            var tribeList = (tribes ?? Enumerable.Empty<Tribe>()).ToList();
            var minionTribes = minion.Tribes ?? new List<Tribe>();
            return tribeList.Contains(Tribe.All) || minionTribes.Contains(Tribe.All) || minionTribes.Any(tribeList.Contains);
        }

        private static void AddTag(MinionInstance target, string tag)
        {
            if (target != null && !target.Tags.Contains(tag))
            {
                target.Tags.Add(tag);
            }
        }

        private static Keyword RandomBonusKeyword(SeededRng rng)
        {
            var keywords = new[]
            {
                Keyword.Taunt,
                Keyword.DivineShield,
                Keyword.Windfury,
                Keyword.Reborn
            };
            return keywords[rng.NextInt(keywords.Length)];
        }

        private static void AddTemporarySpellcraftKeyword(MinionInstance target, Keyword keyword)
        {
            if (target == null)
            {
                return;
            }

            var hadKeyword = target.Keywords.Contains(keyword);
            AddKeyword(target, keyword);
            AddTag(target, "temporary_spellcraft");
            if (!hadKeyword)
            {
                AddTag(target, TemporarySpellcraftKeywordTag(keyword));
            }
        }

        private static string TemporarySpellcraftKeywordTag(Keyword keyword)
        {
            switch (keyword)
            {
                case Keyword.Reborn:
                    return "temporary_spellcraft_added_reborn";
                case Keyword.Taunt:
                    return "temporary_spellcraft_added_taunt";
                case Keyword.DivineShield:
                    return "temporary_spellcraft_added_divine_shield";
                case Keyword.Windfury:
                    return "temporary_spellcraft_added_windfury";
                default:
                    return "temporary_spellcraft_added_keyword";
            }
        }

        private static bool ConsumePermanentSpellcraft(MinionInstance target)
        {
            if (target == null ||
                (target.CardId != LavaLurkerCardId && target.CardId != TimewarpedLavaLurkerCardId))
            {
                return false;
            }

            target.Counters.TryGetValue(PermanentSpellcraftCounter, out var left);
            if (left <= 0)
            {
                return false;
            }

            target.Counters[PermanentSpellcraftCounter] = left - 1;
            AddTag(target, "permanent_spellcraft_receiver");
            return true;
        }
    }
}
