using System;
using System.Collections.Generic;
using System.Linq;

namespace LearnHearthstone.Domain.Data
{
    public enum HeroEffectImplementationStatus
    {
        Implemented,
        Next,
        Planned,
        FrameworkFirst,
        Deferred,
        Unregistered
    }

    public sealed class HeroEffectImplementation
    {
        public string HeroCardId;
        public string HeroName;
        public string HeroPowerCardId;
        public string BuddyCardId;
        public string BuddyName;
        public string Phase;
        public HeroEffectImplementationStatus Status;
        public string Note;
    }

    public static class HeroEffectImplementationRegistry
    {
        private static readonly HeroEffectImplementation[] Entries =
        {
            Entry("TB_BaconShop_HERO_34", "Patchwerk", "TB_BaconShop_HP_035", "TB_BaconShop_HERO_34_Buddy", "Weebomination", "Phase 1", HeroEffectImplementationStatus.Implemented, "Health baseline is handled by hero data; buddy end-turn left-neighbor Health buff is implemented."),
            Entry("BG32_HERO_001", "Forest Lord Cenarius", "BG32_HERO_001p", "BG32_HERO_001_Buddy", "Malorne", "Phase 1", HeroEffectImplementationStatus.Implemented, "Active maximum Gold growth and Malorne spent-Gold scaling are implemented."),
            Entry("TB_BaconShop_HERO_57", "Nozdormu", "TB_BaconShop_HP_063", "TB_BaconShop_HERO_57_Buddy", "Chromie", "Phase 1", HeroEffectImplementationStatus.Implemented, "Start-of-turn free refresh and Chromie helpful refresh buff are implemented."),
            Entry("TB_BaconShop_HERO_60", "Kael'thas Sunstrider", "TB_BaconShop_HP_066", "TB_BaconShop_HERO_60_Buddy", "Crimson Hand Centurion", "Phase 1", HeroEffectImplementationStatus.Implemented, "Third bought minion grants Tavern Coin; buddy gains triggering minion stats."),
            Entry("BG31_HERO_006", "Exarch Othaar", "BG31_HERO_006p", "BG31_HERO_006_Buddy", "The Celestial Archive", "Phase 1", HeroEffectImplementationStatus.Implemented, "Turn-start Tavern spell discount and zero-cost spell copy are implemented."),
            Entry("BG28_HERO_800", "Tae'thelan Bloodwatcher", "BG28_HERO_800p", "BG28_HERO_800_Buddy", "Reliquary Attendant", "Phase 1", HeroEffectImplementationStatus.Implemented, "Every fourth Tavern spell costs zero; buddy copies the first cast Tavern spell each turn."),
            Entry("BG22_HERO_004", "Varden Dawngrasp", "BG22_HERO_004p", "BG22_HERO_004_Buddy", "Varden's Aquarrior", "Phase 1", HeroEffectImplementationStatus.Implemented, "Refresh copies and freezes the highest-tier Tavern minion; buddy buffs both copies."),
            Entry("TB_BaconShop_HERO_49", "Millhouse Manastorm", "TB_BaconShop_HP_054", "TB_BaconShop_HERO_49_Buddy", "Magnus Manastorm", "Phase 1", HeroEffectImplementationStatus.Implemented, "Minion, refresh, and upgrade cost modifiers are implemented; buddy grants two free refreshes each turn."),
            Entry("TB_BaconShop_HERO_10", "Trade Prince Gallywix", "TB_BaconShop_HP_008", "TB_BaconShop_HERO_10_Buddy", "Bilgewater Mogul", "Phase 1", HeroEffectImplementationStatus.Implemented, "Sell banking and buddy maximum Gold growth are implemented."),

            Entry("TB_BaconShop_HERO_74", "Forest Warden Omu", "TB_BaconShop_HP_082", "TB_BaconShop_HERO_74_Buddy", "Evergreen Botani", "Phase 2", HeroEffectImplementationStatus.Implemented, "Upgrade refund plus end-turn Tavern-tier minion reward are implemented."),
            Entry("BG26_HERO_101", "Cap'n Hoggarr", "BG26_HERO_101p", "BG26_HERO_101_Buddy", "Shining Sailor", "Phase 2", HeroEffectImplementationStatus.Implemented, "Pirate buy refund and Pirate injection on refresh are implemented."),
            Entry("TB_BaconShop_HERO_53", "Ysera", "TB_BaconShop_HP_062", "TB_BaconShop_HERO_53_Buddy", "Valithria Dreamwalker", "Phase 2", HeroEffectImplementationStatus.Implemented, "Refresh Dragon injection and buddy Dragon-enter growth are implemented."),

            Entry("BG24_HERO_204", "Enhance-o Mechano", "BG24_HERO_204p", "BG24_HERO_204_Buddy", "Enhance-o Medico", "Phase 3", HeroEffectImplementationStatus.Implemented, "Refresh grants a Tavern minion a random Bonus Keyword; buddy gains +3/+3 per Bonus Keyword on bought minions."),
            Entry("BG20_HERO_280", "Kurtrus Ashfallen", "BG20_HERO_280p5", "BG20_HERO_280_Buddy", "Living Nightmare", "Phase 3", HeroEffectImplementationStatus.Implemented, "Once-per-turn third bought minion copy and Living Nightmare Tavern +2/+2 buy trigger are implemented."),
            Entry("TB_BaconShop_HERO_55", "Fungalmancer Flurgl", "TB_BaconShop_HP_056", "TB_BaconShop_HERO_55_Buddy", "Sparkfin Soothsayer", "Phase 3", HeroEffectImplementationStatus.Implemented, "Sell-five Murloc reward and Sparkfin same-tier Tavern Murloc transform are implemented."),
            Entry("BG20_HERO_102", "Overlord Saurfang", "BG20_HERO_102p", "BG20_HERO_102_Buddy", "Dranosh Saurfang", "Phase 3", HeroEffectImplementationStatus.Implemented, "Tavern buff improvement after four bought minions and Dranosh half-stat growth are implemented."),
            Entry("TB_BaconShop_HERO_01", "Edwin VanCleef", "TB_BaconShop_HP_001", "TB_BaconShop_HERO_01_Buddy", "SI:7 Scout", "Phase 3", HeroEffectImplementationStatus.Implemented, "Targeted Sharpen Blades scaling and SI:7 Scout buy growth are implemented."),

            Entry("TB_BaconShop_HERO_68", "Skycap'n Kragg", "TB_BaconShop_HP_076", "TB_BaconShop_HERO_68_Buddy", "Sharkbait", "Phase 4", HeroEffectImplementationStatus.Implemented, "Piggy Bank grants growing once-per-game Gold; Sharkbait sale refreshes that use."),
            Entry("TB_BaconShop_HERO_15", "George the Fallen", "TB_BaconShop_HP_010", "TB_BaconShop_HERO_15_Buddy", "Karl the Lost", "Phase 4", HeroEffectImplementationStatus.Implemented, "Boon of Light gives a friendly minion Divine Shield; Karl buffs Divine Shield minions after hero power use."),
            Entry("BG31_HERO_003", "Farseer Nobundo", "BG31_HERO_003p", null, null, "Phase 4", HeroEffectImplementationStatus.Implemented, "The Galaxy's Lens copies the last cast Tavern spell and gains a stacking next-use discount each turn."),
            Entry("BG28_HERO_801", "Doctor Holli'dae", "BG28_HERO_801p", "BG28_HERO_801_Buddy", "The Nine Frogs", "Phase 4", HeroEffectImplementationStatus.Implemented, "Blessing of the Nine Frogs gives a random Tavern spell; The Nine Frogs gives same-tier Tavern spells after minion buys for nine charges."),
            Entry("BG20_HERO_103", "Death Speaker Blackthorn", "BG20_HERO_103p", "BG20_HERO_103_Buddy", "Death's Head Sage", "Phase 4", HeroEffectImplementationStatus.Implemented, "Bloodbound grants Blood Gems twice per turn; Death's Head Sage adds extra Blood Gem copies."),
            Entry("TB_BaconShop_HERO_25", "Lich Baz'hial", "TB_BaconShop_HP_049", "TB_BaconShop_HERO_25_Buddy", "Unearthed Underling", "Phase 4", HeroEffectImplementationStatus.Implemented, "Graveyard Shift steals a Tavern card and deals self-damage; Unearthed Underling rewinds that damage and gains stats."),
            Entry("TB_BaconShop_HERO_75", "Rakanishu", "TB_BaconShop_HP_085", "TB_BaconShop_HERO_75_Buddy", "Lantern Tender", "Phase 4", HeroEffectImplementationStatus.Implemented, "Tavern Lighting gives a playable Lantern Light; Lantern Tender adds stat Tavern spells at end of turn."),
            Entry("TB_BaconShop_HERO_41", "Reno Jackson", "TB_BaconShop_HP_046", "TB_BaconShop_HERO_41_Buddy", "Sr. Tomb Diver", "Phase 4", HeroEffectImplementationStatus.Implemented, "Gonna Be Rich! makes a target Golden once per game; Sr. Tomb Diver goldens the right-most minion on its Tavern death proxy."),
            Entry("TB_BaconShop_HERO_18", "Patches the Pirate", "TB_BaconShop_HP_072", "TB_BaconShop_HERO_18_Buddy", "Tuskarr Raider", "Phase 4", HeroEffectImplementationStatus.Implemented, "Pirate Parrrrty! gains Pirates with buy-based discounts; Tuskarr Raider adds Bounties on play and Tavern death proxy."),
            Entry("TB_BaconShop_HERO_38", "King Mukla", "TB_BaconShop_HP_038", "TB_BaconShop_HERO_38_Buddy", "Crazy Monkey", "Phase 4", HeroEffectImplementationStatus.Implemented, "Bananarama grants Bananas at turn start; Crazy Monkey improves after Tavern spells and feeds Bananas when sold."),
            Entry("TB_BaconShop_HERO_29", "C'Thun", "TB_BaconShop_HP_104", "TB_BaconShop_HERO_29_Buddy", "Tentacle of C'Thun", "Phase 4", HeroEffectImplementationStatus.Implemented, "Saturday C'Thuns! buffs friendly minions at end of turn and improves each turn; Tentacle gains temporary stats from those buffs."),
            Entry("TB_BaconShop_HERO_64", "Captain Eudora", "TB_BaconShop_HP_074", "TB_BaconShop_HERO_64_Buddy", "Dagwik Stickytoe", "Phase 4", HeroEffectImplementationStatus.Implemented, "Buried Treasure tracks four digs for a Golden minion; Dagwik buffs a Golden friendly minion at end of turn."),
            Entry("TB_BaconShop_HERO_42", "Elise Starseeker", "TB_BaconShop_HP_047", "TB_BaconShop_HERO_42_Buddy", "Jr. Navigator", "Phase 4", HeroEffectImplementationStatus.Implemented, "Lead Explorer discovers a current-tier minion with increasing cost; Jr. Navigator reduces that cost on play."),
            Entry("TB_BaconShop_HERO_17", "Millificent Manastorm", "TB_BaconShop_HP_015", "TB_BaconShop_HERO_17_Buddy", "Elementium Squirrel Bomb", "Phase 4", HeroEffectImplementationStatus.Implemented, "Tinker discovers Magnetic Mechs after Tier 4; Elementium Squirrel Bomb uses the current Tavern death proxy for Mech-death damage."),
            Entry("TB_BaconShop_HERO_22", "The Lich King", "TB_BaconShop_HP_024", "TB_BaconShop_HERO_22_Buddy", "Arfus", "Phase 4", HeroEffectImplementationStatus.Implemented, "Reborn Rites grants temporary Reborn and Arfus adds its Attack after the hero power gives Reborn."),
            Entry("TB_BaconShop_HERO_23", "Shudderwock", "TB_BaconShop_HP_022", "TB_BaconShop_HERO_23_Buddy", "Muckslinger", "Phase 4", HeroEffectImplementationStatus.Implemented, "Muckslinger Battlecry reward and Snicker-snack shared Battlecry replay resolver are implemented for registered Battlecries, including secondary target command data."),
            Entry("TB_BaconShop_HERO_71", "Jandice Barov", "TB_BaconShop_HP_084", "TB_BaconShop_HERO_71_Buddy", "Jandice's Apprentice", "Phase 4", HeroEffectImplementationStatus.Implemented, "Friendly non-Golden/Tavern minion swap and repeat-play buddy board buff are implemented."),
            Entry("BG20_HERO_301", "Mutanus the Devourer", "BG20_HERO_301p", "BG20_HERO_301_Buddy", "Nightmare Ectoplasm", "Phase 4", HeroEffectImplementationStatus.Implemented, "Devour sells a friendly minion and spits stats to random friendly minions; Nightmare Ectoplasm adds one extra spit target when devoured."),
            Entry("BG20_HERO_101", "Xyrella", "BG20_HERO_101p", "BG20_HERO_101_Buddy", "Baby Elekk", "Phase 4", HeroEffectImplementationStatus.Implemented, "See the Light sets a Tavern minion to 2/2 and adds it to hand; Baby Elekk buffs lower-Attack played minions and improves its buff amount."),
            Entry("TB_BaconShop_HERO_39", "Pyramad", "TB_BaconShop_HP_040", "TB_BaconShop_HERO_39_Buddy", "Titanic Guardian", "Phase 4", HeroEffectImplementationStatus.Implemented, "Brick by Brick steals a random Tavern minion and doubles its Health; Titanic Guardian follows hero-effect Health gains in the current layer."),
            Entry("BG20_HERO_201", "Vol'jin", "BG20_HERO_201p", "BG20_HERO_201_Buddy", "Master Gadrin", "Phase 4", HeroEffectImplementationStatus.Implemented, "Spirit Swap accepts two explicit friendly-board/Tavern targets; Master Gadrin has a start-of-combat left-neighbor hook."),
            Entry("BG26_HERO_102", "Inge, the Iron Hymn", "BG26_HERO_102p", "BG26_HERO_102_Buddy", "Solemn Serenader", "Phase 4", HeroEffectImplementationStatus.Implemented, "Major Hymn alternates Attack/Health by turn and Solemn Serenader adds half its Attack to the hero-power target in the active mode."),
            Entry("TB_BaconShop_HERO_58", "Malygos", "TB_BaconShop_HP_052", "TB_BaconShop_HERO_58_Buddy", "Nexus Lord", "Phase 4", HeroEffectImplementationStatus.Implemented, "Arcane Alteration replaces board or Tavern targets twice per turn; Nexus Lord upgrades the replacement tier by one."),
            Entry("TB_BaconShop_HERO_62", "Maiev Shadowsong", "TB_BaconShop_HP_068", "TB_BaconShop_HERO_62_Buddy", "Shadow Warden", "Phase 4", HeroEffectImplementationStatus.Implemented, "Imprison moves a Tavern card to hand with the existing two-turn lock; Shadow Warden makes the next imprisoned card Golden."),
            Entry("TB_BaconShop_HERO_91", "Zephrys, the Great", "TB_BaconShop_HP_102", "TB_BaconShop_HERO_91_Buddy", "Phyresz", "Phase 4", HeroEffectImplementationStatus.Implemented, "Three Wishes finds the third copy while wishes remain; Phyresz starts a plain-copy Discover for singleton minions on its Tavern death proxy."),
            Entry("TB_BaconShop_HERO_67", "Captain Hooktusk", "TB_BaconShop_HP_075", "TB_BaconShop_HERO_67_Buddy", "Raging Contender", "Phase 4", HeroEffectImplementationStatus.Implemented, "Trash for Treasure removes a friendly minion and discovers a lower-tier minion; Raging Contender grants Gold equal to the removed minion's Tier."),
            Entry("BG26_HERO_104", "Rock Master Voone", "BG26_HERO_104p", "BG26_HERO_104_Buddy", "Akali, Rock Rhino", "Phase 4", HeroEffectImplementationStatus.Implemented, "End-of-turn counters copy the left-most hand card every three turns for Voone and every two turns for Akali."),
            Entry("BG31_HERO_005", "Zerek, Master Cloner", "BG31_HERO_005p", "BG31_HERO_005_Buddy", "Mini-Zerek", "Phase 4", HeroEffectImplementationStatus.Implemented, "Cloning Gallery summons one exact friendly copy once per game; Mini-Zerek transforms into the explicit Tavern minion target."),
            Entry("BG23_HERO_305", "Heistbaron Togwaggle", "BG23_HERO_305p", "BG23_HERO_305_Buddy", "Waxadred, the Drippy", "Phase 4", HeroEffectImplementationStatus.Implemented, "The Perfect Crime steals all Tavern cards with a per-turn discount; Waxadred now reads the last-opponent warband snapshot before falling back to the current opponent board."),
            Entry("TB_BaconShop_HERO_78", "Chenvaala", "TB_BaconShop_HP_088", "TB_BaconShop_HERO_78_Buddy", "Snow Elemental", "Phase 4", HeroEffectImplementationStatus.Implemented, "Every third played Elemental reduces upgrade cost by 3; Snow Elemental adds an extra Frozen Elemental on refresh."),
            Entry("TB_BaconShop_HERO_33", "The Curator", "TB_BaconShop_HP_033", "TB_BaconShop_HERO_33_Buddy", "Mishmash", "Phase 4", HeroEffectImplementationStatus.Implemented, "Match start now creates the Venomous all-type Amalgam; Mishmash mirrors the Amalgam's positive stat delta in this hero-effect layer."),
            Entry("TB_BaconShop_HERO_36", "Dancin' Deryl", "TB_BaconShop_HP_042", "TB_BaconShop_HERO_36_Buddy", "Asher the Haberdasher", "Phase 4", HeroEffectImplementationStatus.Implemented, "Hat Trick adds and passes +1/+1 hats through the Tavern sell event; Asher gains two hats after sells and passes them when sold."),
            Entry("TB_BaconShop_HERO_11", "Ragnaros the Firelord", "TB_BaconShop_HP_087", "TB_BaconShop_HERO_11_Buddy", "Lucifron", "Phase 4", HeroEffectImplementationStatus.Implemented, "Buying 16 cards unlocks Sulfuras end-turn buffs; Lucifron repeats that implemented end-turn effect once."),
            Entry("BG34_HERO_001", "Time Twister Chromie", "BG34_HERO_001p", null, null, "Phase 4", HeroEffectImplementationStatus.Implemented, "Mana Per Minute refreshes generated Tavern slots into Tavern spells."),
            Entry("TB_BaconShop_HERO_27", "Sindragosa", "TB_BaconShop_HP_014", "TB_BaconShop_HERO_27_Buddy", "Thawed Champion", "Phase 4", HeroEffectImplementationStatus.Implemented, "Minions cost 2, the Tavern draws one fewer minion, end-turn freeze marks one Tavern slot, and Thawed Champion makes a random Frozen Tavern minion Golden."),

            Entry("TB_BaconShop_HERO_76", "Al'Akir", "TB_BaconShop_HP_086", "TB_BaconShop_HERO_76_Buddy", "Spirit of Air", "Phase 5", HeroEffectImplementationStatus.Implemented, "Start-of-combat left-most Windfury/Divine Shield/Taunt and Spirit of Air combat Deathrattle keyword grant are implemented."),
            Entry("TB_BaconShop_HERO_92", "Y'Shaarj", "TB_BaconShop_HP_103", "TB_BaconShop_HERO_92_Buddy", "Baby Y'Shaarj", "Phase 5", HeroEffectImplementationStatus.Implemented, "Start-of-combat same-tier summon, hand copy, and Baby Y'Shaarj board +1/+1 reaction are implemented for hero-effect and CombatEngine internal combat summons."),
            Entry("TB_BaconShop_HERO_52", "Deathwing", "TB_BaconShop_HP_061", "TB_BaconShop_HERO_52_Buddy", "Sinestra", "Phase 5", HeroEffectImplementationStatus.Implemented, "Start-of-combat +2 Attack is applied to combat boards, friendly minions keep it, and Sinestra converts friendly Attack gains to +1 Health. Opponent warband permanence is intentionally out of scope for the single-player trainer."),
            Entry("TB_BaconShop_HERO_08", "Illidan Stormrage", "TB_BaconShop_HP_069", "TB_BaconShop_HERO_08_Buddy", "Eclipsion Illidari", "Phase 5", HeroEffectImplementationStatus.Implemented, "Wingmen edge +2/+1, pre-normal-combat immediate attacks, Eclipsion Illidari's one-attack Immune, and friendly-attack combat rewards are implemented."),
            Entry("TB_BaconShop_HERO_14", "Queen Wagtoggle", "TB_BaconShop_HP_037a", "TB_BaconShop_HERO_14_Buddy", "Elder Taggawag", "Phase 5", HeroEffectImplementationStatus.Implemented, "Start-of-combat one-minion-per-type buffs and Elder Taggawag four-type stat gain are implemented."),
            Entry("TB_BaconShop_HERO_93", "N'Zoth", "TB_BaconShop_HP_105", "TB_BaconShop_HERO_93_Buddy", "Baby N'Zoth", "Phase 5", HeroEffectImplementationStatus.Implemented, "Starting Fish, Baby N'Zoth Golden Battlecry, Fish Deathrattle collection, and copied Deathrattle replay are implemented."),
            Entry("BG22_HERO_003", "Vanndar Stormpike", "BG22_HERO_003p", "BG22_HERO_003_Buddy", "Stormpike Lieutenant", "Phase 5", HeroEffectImplementationStatus.Implemented, "Turn-7 highest-Health combat copy and Stormpike Lieutenant right-most +10 Health are implemented."),
            Entry("BG22_HERO_002", "Drek'Thar", "BG22_HERO_002p", "BG22_HERO_002_Buddy", "Frostwolf Lieutenant", "Phase 5", HeroEffectImplementationStatus.Implemented, "Turn-7 highest-Attack combat copy and Frostwolf Lieutenant left-most +10 Attack are implemented."),
            Entry("BG22_HERO_000", "Tavish Stormpike", "BG22_HERO_000p", "BG22_HERO_000_Buddy", "Crabby", "Phase 5", HeroEffectImplementationStatus.Implemented, "Deadeye target selection, start-of-combat damage/removal, empty-slot immediate launch resolution, and Crabby plain-copy reward are implemented; targeting UI polish remains a product follow-up."),
            Entry("BG20_HERO_282", "Tamsin Roame", "BG20_HERO_282p", "BG20_HERO_282_Buddy", "Monstrosity", "Phase 5", HeroEffectImplementationStatus.Implemented, "Fragrant Phylactery start-of-combat stat-sharing Deathrattle and Monstrosity friendly-death Attack growth are implemented through combat death hooks."),
            Entry("BG25_HERO_103", "Teron Gorefiend", "BG25_HERO_103p", "BG25_HERO_103_Buddy", "Shadowy Construct", "Phase 5", HeroEffectImplementationStatus.Implemented, "Hero Power target marking, start-of-combat destroy/resummon, summon modifiers, and Shadowy Construct stat gain are implemented."),
            Entry("TB_BaconShop_HERO_45", "Arch-Villain Rafaam", "TB_BaconShop_HP_053", "TB_BaconShop_HERO_45_Buddy", "Loyal Henchman", "Phase 5", HeroEffectImplementationStatus.Implemented, "Combat kill ownership, first killed enemy copy, and Loyal Henchman second-kill copy are implemented through shared combat rewards."),
            Entry("BG20_HERO_100", "Rokara", "BG20_HERO_100p", "BG20_HERO_100_Buddy", "Icesnarl the Mighty", "Phase 5", HeroEffectImplementationStatus.Implemented, "Friendly kill rewards persist Rokara Attack and Icesnarl Health through shared combat kill ownership."),
            Entry("BG23_HERO_306", "Sylvanas Windrunner", "BG23_HERO_306p", "BG23_HERO_306_Buddy", "Nathanos Blightcaller", "Phase 5", HeroEffectImplementationStatus.Implemented, "Reclaimed Souls discovers from last-combat friendly death history; Nathanos targeted sell-and-split Battlecry is implemented."),
            Entry("BG21_HERO_030", "Sneed", "BG21_HERO_030p", "BG21_HERO_030_Buddy", "Piloted Whirl-O-Tron", "Phase 5", HeroEffectImplementationStatus.Implemented, "Starting Shredder, target summon Deathrattle, combat summon resolver, and Piloted Whirl-O-Tron Deathrattle copying are implemented."),
            Entry("TB_BaconShop_HERO_702", "The Jailer", "TB_BaconShop_HP_702", "TB_BaconShop_HERO_702_Buddy", "Mawsworn Soulkeeper", "Phase 5", HeroEffectImplementationStatus.Implemented, "Runic Empowerment from friendly death counters and Mawsworn Soulkeeper combat Deathrattle Undead summons are implemented."),
            Entry("TB_BaconShop_HERO_95", "Greybough", "TB_BaconShop_HP_107", "TB_BaconShop_HERO_95_Buddy", "Wandering Treant", "Phase 5", HeroEffectImplementationStatus.Implemented, "Sprout It Out combat summon modifiers and Wandering Treant friendly-Taunt-attacked permanent board buff are implemented."),
            Entry("BG22_HERO_305", "Onyxia", "BG22_HERO_305p", "BG22_HERO_305_Buddy", "Many Whelps", "Phase 5", HeroEffectImplementationStatus.Implemented, "Broodmother hero-level Avenge summon, immediate Whelp attack queue, and Many Whelps permanent growth are implemented."),
            Entry("BG22_HERO_200", "Ini Stormcoil", "BG22_HERO_200p", "BG22_HERO_200_Buddy", "Sub Scrubber", "Phase 5", HeroEffectImplementationStatus.Implemented, "Sub Scrubber Mech-play growth and MechGyver friendly combat Mech-death Magnetic Mech rewards are implemented."),
            Entry("BG23_HERO_201", "Ozumat", "BG23_HERO_201p", "BG23_HERO_201_Buddy", "Tamuzo", "Phase 5", HeroEffectImplementationStatus.Implemented, "Tentacle combat summon, sell/combat-death growth, and Tamuzo doubling through shared combat summon modifiers are implemented."),
            Entry("TB_BaconShop_HERO_59", "Aranna Starseeker", "TB_BaconShop_HP_065", "TB_BaconShop_HERO_59_Buddy", "Sklibb, Demon Hunter", "Phase 5", HeroEffectImplementationStatus.Implemented, "Friendly-attack unlock for a free first minion buy and Sklibb refresh extra higher-tier minion are implemented."),
            Entry("TB_BaconShop_HERO_37", "Lord Jaraxxus", "TB_BaconShop_HP_036", "TB_BaconShop_HERO_37_Buddy", "Kil'rek", "Phase 5", HeroEffectImplementationStatus.Implemented, "Bloodfury buffs friendly Demons and Kil'rek rewards random Demons from Tavern and combat Deathrattle paths."),
            Entry("BG22_HERO_001", "Bru'kan", "BG22_HERO_001p", "BG22_HERO_001_Buddy", "Spirit Raptor", "Phase 5", HeroEffectImplementationStatus.Implemented, "Element choice state, start-of-combat element calls, and Spirit Raptor remembered Deathrattle replay are implemented with local baseline element effects."),

            Entry("TB_BaconShop_HERO_90", "Silas Darkmoon", "TB_BaconShop_HP_101", "TB_BaconShop_HERO_90_Buddy", "Burth", "Phase 6", HeroEffectImplementationStatus.Implemented, "Darkmoon Tickets are tagged onto Tavern minions on refresh; buying three starts a current-tier minion Discover. Burth buffs discovered minions and improves."),
            Entry("BG21_HERO_020", "Cookie the Cook", "BG21_HERO_020p", "BG21_HERO_020_Buddy", "Sous Chef", "Phase 6", HeroEffectImplementationStatus.Implemented, "Stir the Pot consumes Tavern or friendly minions, tracks their types, and discovers from those types after three feeds; Sous Chef grants one extra use each turn."),
            Entry("TB_BaconShop_HERO_02", "Galakrond", "TB_BaconShop_HP_011", "TB_BaconShop_HERO_02_Buddy", "Apostle of Galakrond", "Phase 6", HeroEffectImplementationStatus.Implemented, "Galakrond's Greed discovers a higher-tier replacement for a Tavern minion; Apostle replaces Tavern minions with higher-tier minions on Battlecry."),
            Entry("BG25_HERO_105", "E.T.C., Band Manager", "BG25_HERO_105p", "BG25_HERO_105_Buddy", "Talent Scout", "Phase 6", HeroEffectImplementationStatus.Implemented, "Sign a New Artist discovers real Buddy cards after Tier 2; Talent Scout makes a Buddy Golden on Battlecry."),
            Entry("TB_BaconShop_HERO_40", "Sir Finley Mrrgglton", "TB_BaconShop_HP_057", "TB_BaconShop_HERO_40_Buddy", "Maxwell, Mighty Steed", "Phase 6", HeroEffectImplementationStatus.Implemented, "Adventure! starts a Hero Power Discover at match start; Maxwell adds the Buddy mapped to the current Hero Power when sold."),
            Entry("BG23_HERO_303", "Murloc Holmes", "BG23_HERO_303p2", "BG23_HERO_303_Buddy", "Watfin", "Phase 6", HeroEffectImplementationStatus.Implemented, "Detective for Hire starts an opponent-snapshot guess Discover, correct guesses grant a Tavern Coin, and Watfin grants a plain copy after a correct guess under the single-player opponent proxy."),
            Entry("BG27_HERO_801", "Thorim, Stormlord", "BG27_HERO_801p2", "BG27_HERO_801_Buddy", "Veranus, Stormlord's Mount", "Phase 6", HeroEffectImplementationStatus.Implemented, "Choose Your Champion starts a Thorim-only legal Tier 7 Discover, stores the selected minion, tracks Gold spent, and grants it after 60 Gold without globally unlocking Tier 7 shops; Veranus transforms the minion to its left into one from a Tier higher, up to Tier 7."),
            Entry("BG28_HERO_400", "Snake Eyes", "BG28_HERO_400p", "BG28_HERO_400_Buddy", "Box Cars", "Phase 6", HeroEffectImplementationStatus.Implemented, "Lucky Roll spends 1 Gold, rolls a six-sided die, gains that much Gold, and cools down until the rolled turn count has passed; Box Cars rolls at turn start and discovers an exact-tier Tavern spell from the active Tavern spell pool."),
            Entry("BG20_HERO_283", "Galewing", "BG20_HERO_283p", "BG20_HERO_283_Buddy", "Flight Trainer", "Phase 6", HeroEffectImplementationStatus.Implemented, "Flightpath pending choice, delayed completion, no-repeat route filtering, official Westfall/Ironforge/Eastern Plaguelands rewards, and Flight Trainer double trigger are implemented."),
            Entry("BG21_HERO_000", "Cariel Roame", "BG21_HERO_000p", "BG21_HERO_000_Buddy", "Captain Fairmount", "Phase 6", HeroEffectImplementationStatus.Implemented, "Conviction buffs random friendly minions using its current target/stat improvements; after each combat it offers an improvement choice and Captain Fairmount randomly improves Conviction at end of turn."),
            Entry("TB_BaconShop_HERO_28", "Infinite Toki", "TB_BaconShop_HP_028", "TB_BaconShop_HERO_28_Buddy", "Clockwork Assistant", "Phase 6", HeroEffectImplementationStatus.Implemented, "Temporal Tavern fills the rightmost replaceable minion slots with two ordinary current-pool higher-tier minions and refills frozen shops by missing card type; Clockwork Assistant Battlecry discovers from remaining current-pool minions and BuddyPool, clamped to the current 6/7 max Tavern tier."),
            Entry("BG22_HERO_201", "Ambassador Faelin", "BG22_HERO_201p", "BG22_HERO_201_Buddy", "Submersible Chef", "Phase 6", HeroEffectImplementationStatus.Implemented, "Expedition Plans skips the first turn, queues start-game Tier 6/4/2 current-pool Discovers, stores selections in advanced state, and grants them at the matching Tavern tiers; Submersible Chef Battlecry adds random Tier 1, 3, and 5 minions."),
            Entry("BG20_HERO_242", "Guff Runetotem", "BG20_HERO_242p", "BG20_HERO_242_Buddy", "Baby Kodo", "Phase 6", HeroEffectImplementationStatus.Implemented, "Tracks bought-card Tavern-tier totals toward Triple Reward grants with rollover; Baby Kodo Battlecry refreshes the Tavern with one minion from each available tier."),
            Entry("TB_BaconShop_HERO_12", "The Rat King", "TB_BaconShop_HP_041", "TB_BaconShop_HERO_12_Buddy", "Pigeon Lord", "Phase 6", HeroEffectImplementationStatus.Implemented, "Turn-start minion-type rotation, current-type Discover, and Pigeon Lord free refreshes while the Tavern lacks the current Hero Power type are implemented."),
            Entry("TB_BaconShop_HERO_56", "Alexstrasza", "TB_BaconShop_HP_064", "TB_BaconShop_HERO_56_Buddy", "Vaelastrasz", "Phase 6", HeroEffectImplementationStatus.Implemented, "Upgrading to Tavern Tier 4 starts a current-pool Dragon Discover; Vaelastrasz Rally adds random Dragon rewards."),
            Entry("BG24_HERO_100", "Sire Denathrius", "BG24_HERO_100p", "BG24_HERO_100_Buddy", "Shady Aristocrat", "Phase 6", HeroEffectImplementationStatus.Implemented, "Uses the shared Quest system for match-start two-Quest choice, Quest progress/rewards, and Shady Aristocrat sell Bonus Quest with an 8-Gold Coin Pouch reward."),
            Entry("TB_BaconShop_HERO_94", "Tickatus", "TB_BaconShop_HP_106", "TB_BaconShop_HERO_94_Buddy", "Ticket Collector", "Phase 6", HeroEffectImplementationStatus.Implemented, "Four-turn Prize Wall and Ticket Collector sell Discover use the shared Darkmoon Prize catalog; all 33 local Darkmoon Prize effects are implemented."),
            Entry("BG20_HERO_202", "Master Nguyen", "BG20_HERO_202p", "BG20_HERO_202_Buddy", "Lei Flamepaw", "Phase 6", HeroEffectImplementationStatus.Implemented, "Queues a two-option temporary Hero Power Discover at turn start, restores Master Nguyen's Hero Power at turn end, and resolves Lei Flamepaw after the choice against the selected temporary Hero Power."),
            Entry("TB_BaconShop_HERO_16", "A. F. Kay", "TB_BaconShop_HP_044", "TB_BaconShop_HERO_16_Buddy", "Snack Vendor", "Phase 6", HeroEffectImplementationStatus.Implemented, "Blocks the first two Tavern turns, then queues Tier 3 and Tier 4 current-pool Discovers; Snack Vendor transfers its stats to a Tier 3 minion at turn end."),
            Entry("BG33_HERO_001", "Loh, the Living Legend", "BG33_HERO_001p_ALT", "BG33_HERO_000_Buddy", "Stoneshell Guardian", "Phase 6", HeroEffectImplementationStatus.Implemented, "Friendly attack counting grants Triple Rewards, and Stoneshell Guardian modifies the first Triple Reward played each turn into a Discover from Golden minions."),
            Entry("TB_BaconShop_HERO_43", "Dinotamer Brann", "TB_BaconShop_HP_048", "TB_BaconShop_HERO_43_Buddy", "Brann's Epic Egg", "Phase 6", HeroEffectImplementationStatus.Implemented, "Battlecry-minion purchases grant Brann Bronzebeard once per game after 4 buys; Brann's Epic Egg Deathrattle summons and grants random Battlecry minions capped at current Tavern Tier, doubled when Golden."),
            Entry("TB_BaconShop_HERO_35", "Yogg-Saron, Hope's End", "TB_BaconShop_HP_039t", "TB_BaconShop_HERO_35_Buddy", "Acolyte of Yogg-Saron", "Phase 6", HeroEffectImplementationStatus.Implemented, "Puzzle Box casts a legal random Tavern spell at start of turn from Turn 3; Acolyte spins the project-approved five-result Wheel table, including shots, Darkmoon Prize, Tavern spells, stat transfer, and devour-refresh."),
            Entry("BG22_HERO_007", "Queen Azshara", "BG22_HERO_007p", "BG22_HERO_007_Buddy", "Imperial Defender", "Phase 6", HeroEffectImplementationStatus.Implemented, "Tracks warband total Attack, starts Naga Conquest at 30 Attack, and Imperial Defender copies the first Spellcraft cast on another friendly minion each turn."),
            Entry("BG23_HERO_304", "Lady Vashj", "BG23_HERO_304p", "BG23_HERO_304_Buddy", "Coilfang Elite", "Phase 6", HeroEffectImplementationStatus.Implemented, "Relics of the Deep grants a temporary random Spellcraft spell not above the current Tavern Tier each turn; Coilfang Elite copies Spellcraft spells from Spellcraft minions that appear in the Tavern."),
            Entry("TB_BaconShop_HERO_72", "Lord Barov", "TB_BaconShop_HP_081", "TB_BaconShop_HERO_72_Buddy", "Barov's Apprentice", "Phase 6", HeroEffectImplementationStatus.Implemented, "Friendly Wager records win/loss/draw predictions through choiceId, settles correct combat predictions for three Tavern Coins, and Barov's Apprentice grants Gold after Tavern Coin casts."),

            Entry("BG36_HERO_105", "Nightmare Lord Xavius", "BG36_HERO_105p", null, null, "Season 14 Preview", HeroEffectImplementationStatus.Implemented, "Feel Devastation uses the scheduled Dark Gift minion Discover flow every four turns."),
            Entry("BG36_HERO_101", "Tras'tath, Soul Parasite", "BG36_HERO_101p", null, null, "Season 14 Preview", HeroEffectImplementationStatus.Implemented, "Void Power schedules the Tier 5 Dark Gift minion Discover and Turn 7 unlock flow."),

            Entry("BG30_HERO_304", "Marin the Manager", "BG30_HERO_304p", "BG30_HERO_304_Buddy", "Fantastic Bellhop", "Phase 7", HeroEffectImplementationStatus.Implemented, "Fantastic Treasure is wired to the scheduled Lesser Trinket choice system; Fantastic Bellhop end-turn helpful card is implemented."),
            Entry("BG32_HERO_002", "Buttons", "BG32_HERO_002p", "BG32_HERO_002_Buddy", "Zippers", "Phase 7", HeroEffectImplementationStatus.Implemented, "Growing Collection is wired to the scheduled Greater Trinket choice system; Zippers helpful-card Tavern and combat Deathrattle rewards are implemented."),
            Entry("BG34_HERO_002", "Mister Clocksworth", "BG34_HERO_002p", null, null, "Phase 7", HeroEffectImplementationStatus.Implemented, "Double Time makes two copies combine into a Golden minion and replaces Golden-minion Triple Rewards with Tavern Coins."),
            Entry("BG34_HERO_004", "Morchie", "BG34_HERO_004p", null, null, "Phase 7", HeroEffectImplementationStatus.Implemented, "Warped Conflux opens the Minor Timewarped Tavern on Turn 5 with focused Timewarped Tavern coverage; no buddy mapping."),
            Entry("BG34_HERO_000", "Murozond, Unbounded", "BG34_HERO_000p", null, null, "Phase 7", HeroEffectImplementationStatus.Implemented, "Alternate Timeline opens the Major Timewarped Tavern on Turn 8; previous-warband Timewarp card effects are tracked under Timewarped Tavern data/effect work, not this hero body."),
            Entry("BG35_HERO_001", "Genn, Worgen King", "BG35_HERO_001p", null, null, "Phase 7", HeroEffectImplementationStatus.Implemented, "King of Duality is disabled under Cosmic Duality; otherwise Turn 4 discovers two replacement Hero Powers, installing one as primary and one as an extra Hero Power."),
            Entry("TB_BaconShop_HERO_21", "The Great Akazamzarak", "TB_BaconShop_HP_020", "TB_BaconShop_HERO_21_Buddy", "Street Magician", "Phase 7", HeroEffectImplementationStatus.Implemented, "Prestidigitation is 0-Cost, opens non-duplicate Secret Discover up to 4 active Secrets, preserves untriggered Secrets, removes triggered Secrets, and Street Magician switches to Better Secrets."),
            Entry("BG25_HERO_100", "Professor Putricide", "BG25_HERO_100p", "BG25_HERO_100_Buddy", "Festergut", "Phase 7", HeroEffectImplementationStatus.Implemented, "Build-An-Undead spends 3 Gold, tracks 3 creations, runs two sequential 3-option component Discovers, stacks the selected stats/effects, filters duplicate keyword components, and Festergut shares the same Putricide's Creation factory."),
            Entry("BG31_HERO_801", "Jim Raynor", "BG31_HERO_801p", "BG31_HERO_801_Buddy", "Tychus Findlay", "Phase 7", HeroEffectImplementationStatus.Implemented, "Lift Off starts with Battlecruiser, refresh/Tychus generate official Battlecruiser Upgrades, and the upgrade tags resolve through Tavern spell and combat hooks."),
            Entry("BG31_HERO_802", "Artanis", "BG31_HERO_802p", "BG31_HERO_802_Buddy", "Probius", "Phase 7", HeroEffectImplementationStatus.Implemented, "Warp Gate records the opening Protoss choice, buys count toward the selected reward, Protoss combat hooks are wired, and Probius still makes Magnetic Mech targets Golden."),
            Entry("BG31_HERO_811", "Kerrigan, Queen of Blades", "BG31_HERO_811p", "BG31_HERO_811_Buddy", "Broken Horn", "Phase 7", HeroEffectImplementationStatus.Implemented, "Spawning Pool starts with Larva, unlocks Zerg tiers, queues morph Discovers, Broken Horn discovers real 6/6 non-morphing Zerg, and core Zerg combat hooks are wired."),
            Entry("TB_BaconShop_HERO_70", "Mr. Bigglesworth", "TB_BaconShop_HP_080", "TB_BaconShop_HERO_70_Buddy", "Lil' K.T.", "Phase 7", HeroEffectImplementationStatus.FrameworkFirst, "Kel'Thuzad's Kitty can discover from eliminated-player warband snapshots and Lil' K.T. gains a plain minion from the opponent warband proxy; true lobby eliminations and lowest-health opponent selection remain deferred."),
            Entry("BG21_HERO_010", "Scabbs Cutterbutter", "BG21_HERO_010p", "BG21_HERO_010_Buddy", "Warden Thelwater", "Phase 7", HeroEffectImplementationStatus.Implemented, "I Spy discovers a plain copy from the simulated next-opponent warband; Warden Thelwater gets that opponent's Buddy under the accepted single-player opponent proxy."),
            Entry("TB_BaconShop_HERO_50", "Tess Greymane", "TB_BaconShop_HP_077", "TB_BaconShop_HERO_50_Buddy", "Hunter of Old", "Phase 7", HeroEffectImplementationStatus.Implemented, "Bob's Burgles refreshes the Tavern from the last-opponent warband snapshot; Hunter of Old gets the last opponent's Buddy under the accepted single-player opponent proxy.")
        };

        public static IReadOnlyList<HeroEffectImplementation> All => Entries;

        public static HeroEffectImplementation FindByHeroCardId(string heroCardId)
        {
            return Find(entry => entry.HeroCardId, heroCardId);
        }

        public static HeroEffectImplementation FindByHeroPowerCardId(string heroPowerCardId)
        {
            return Find(entry => entry.HeroPowerCardId, heroPowerCardId);
        }

        public static HeroEffectImplementation FindByBuddyCardId(string buddyCardId)
        {
            return Find(entry => entry.BuddyCardId, buddyCardId);
        }

        public static HeroEffectImplementationStatus GetStatusByHeroPowerCardId(string heroPowerCardId)
        {
            return FindByHeroPowerCardId(heroPowerCardId).Status;
        }

        public static HeroEffectImplementationStatus GetStatusByBuddyCardId(string buddyCardId)
        {
            return FindByBuddyCardId(buddyCardId).Status;
        }

        private static HeroEffectImplementation Find(Func<HeroEffectImplementation, string> selector, string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return Unregistered(cardId);
            }

            return Entries.FirstOrDefault(entry => string.Equals(selector(entry), cardId, StringComparison.OrdinalIgnoreCase))
                   ?? Unregistered(cardId);
        }

        private static HeroEffectImplementation Unregistered(string cardId)
        {
            return new HeroEffectImplementation
            {
                HeroCardId = cardId,
                HeroName = "Unregistered",
                Status = HeroEffectImplementationStatus.Unregistered,
                Phase = "Unregistered",
                Note = "No hero power or buddy effect implementation status has been registered for this cardId."
            };
        }

        private static HeroEffectImplementation Entry(
            string heroCardId,
            string heroName,
            string heroPowerCardId,
            string buddyCardId,
            string buddyName,
            string phase,
            HeroEffectImplementationStatus status,
            string note)
        {
            return new HeroEffectImplementation
            {
                HeroCardId = heroCardId,
                HeroName = heroName,
                HeroPowerCardId = heroPowerCardId,
                BuddyCardId = buddyCardId,
                BuddyName = buddyName,
                Phase = phase,
                Status = status,
                Note = note
            };
        }
    }
}
