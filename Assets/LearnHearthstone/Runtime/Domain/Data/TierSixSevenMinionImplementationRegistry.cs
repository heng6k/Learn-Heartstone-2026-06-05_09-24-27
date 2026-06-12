using System;
using System.Collections.Generic;
using System.Linq;

namespace LearnHearthstone.Domain.Data
{
    public enum HighTierImplementationStatus
    {
        Implemented,
        OutOfScope
    }

    public sealed class HighTierMinionImplementation
    {
        public string CardId;
        public int TavernTier;
        public string Area;
        public HighTierImplementationStatus Status;
        public string Note;
    }

    public static class TierSixSevenMinionImplementationRegistry
    {
        private static readonly HighTierMinionImplementation[] Entries =
        {
            Entry("BG25_009", 6, "Deathrattle/Reborn", HighTierImplementationStatus.Implemented, "Reborn and deathrattle summons Eternal Knights in combat."),
            Entry("BG26_157", 6, "Avenge Blood Gems", HighTierImplementationStatus.Implemented, "Avenge applies trainer Blood Gem stats to friendly Quilboar."),
            Entry("BG26_175", 6, "Elemental triple wildcard", HighTierImplementationStatus.Implemented, "Surprise Elemental participates in Elemental triple checks."),
            Entry("BG26_354", 6, "Start of combat hand stats", HighTierImplementationStatus.Implemented, "Gains stats from friendly hand minions at combat start."),
            Entry("BG28_595", 6, "End turn Tavern spells", HighTierImplementationStatus.Implemented, "Adds random Tavern spells at end of turn."),
            Entry("BG29_841", 6, "Even-tier play trigger", HighTierImplementationStatus.Implemented, "Even-tier cards buff even-tier friendly minions."),
            Entry("BG31_035", 6, "Naga/spell scaling", HighTierImplementationStatus.Implemented, "Naga plays buff self, spells improve the buff every four casts."),
            Entry("BG31_171", 6, "End turn Magnetic tokens", HighTierImplementationStatus.Implemented, "Adds scaling Magnetic Satellites at end of turn."),
            Entry("BG31_820", 6, "Pirate attack trigger", HighTierImplementationStatus.Implemented, "Friendly Pirate attack buffs give this Health."),
            Entry("BG31_835", 6, "Avenge/Deathrattle Undead", HighTierImplementationStatus.Implemented, "Avenge rewards Undead; deathrattle summons the leftmost hand minion for combat."),
            Entry("BG32_204", 6, "Beetle deathrattle", HighTierImplementationStatus.Implemented, "Deathrattle improves Beetles and summons one."),
            Entry("BG32_234", 6, "Pirate acquisition", HighTierImplementationStatus.Implemented, "Pirates added to hand buff the board, golden minions receive the larger buff."),
            Entry("BG32_822", 6, "Dragon combat spell scaling", HighTierImplementationStatus.Implemented, "Tavern spells improve start-of-combat Dragon buff."),
            Entry("BG32_846", 6, "Elemental play trigger", HighTierImplementationStatus.Implemented, "Elementals played buff friendly Elementals."),
            Entry("BG33_154", 6, "Demon damage trigger", HighTierImplementationStatus.Implemented, "Combat damage by friendly Demons buffs other friendly minions."),
            Entry("BG33_240", 6, "Rally Dragon health", HighTierImplementationStatus.Implemented, "Rally buffs friendly Dragons by its Health."),
            Entry("BG33_823", 6, "Gold-spent Bounty", HighTierImplementationStatus.Implemented, "Every nine Gold spent adds Bounties."),
            Entry("BG33_891", 6, "Tavern spell buy trigger", HighTierImplementationStatus.Implemented, "Buying Tavern spells adds deterministic taught Murlocs once per turn."),
            Entry("BG33_893", 6, "Low-tier play trigger", HighTierImplementationStatus.Implemented, "Tier 3 or lower cards buff friendly Murlocs."),
            Entry("BG33_920", 6, "Naga health trigger", HighTierImplementationStatus.Implemented, "Friendly Naga Health buffs grant matching Attack."),
            Entry("BG33_923", 6, "Spell health aura", HighTierImplementationStatus.Implemented, "Spells give friendly minions Health."),
            Entry("BG34_175", 6, "Magnetic trigger", HighTierImplementationStatus.Implemented, "Magnetizing a minion buffs the board."),
            Entry("BG34_321", 6, "Beast play trigger", HighTierImplementationStatus.Implemented, "Beasts played buff and damage friendly Beasts."),
            Entry("BG34_692", 6, "Tavern spell Undead growth", HighTierImplementationStatus.Implemented, "Tavern spells improve Undead Attack wherever possible."),
            Entry("BG34_765", 6, "Rally attack grant", HighTierImplementationStatus.Implemented, "Rally gives other friendly minions this minion's Attack."),
            Entry("BG34_921", 6, "Attack spell trigger", HighTierImplementationStatus.Implemented, "Friendly attacks cast deterministic Shiny Ring buffs."),
            Entry("BG34_926", 6, "Queen's Command triggers", HighTierImplementationStatus.Implemented, "Battlecry, deathrattle, and rally cast Queen's Command style buffs."),
            Entry("BG35_153", 6, "Devour trigger", HighTierImplementationStatus.Implemented, "Devours buff current Tavern minions for the turn."),
            Entry("BG35_155", 6, "Sell Demon Fodder refresh", HighTierImplementationStatus.Implemented, "Selling minions injects Demon Fodder on future refreshes."),
            Entry("BG35_342", 6, "Deathrattle count scaling", HighTierImplementationStatus.Implemented, "Tracks deathrattles triggered this game and applies global stats."),
            Entry("BG35_431", 6, "End turn Blood Gems", HighTierImplementationStatus.Implemented, "Applies Blood Gems to the board, repeating for extra keywords."),
            Entry("BG35_437", 6, "Deathrattle Blood Gem growth", HighTierImplementationStatus.Implemented, "Friendly Deathrattle deaths improve Blood Gem Attack."),
            Entry("BG35_700", 6, "Rally immediate Pirate", HighTierImplementationStatus.Implemented, "Rally summons an attacking Sky Pirate token."),
            Entry("BG35_883", 6, "Friendly targeted spell aura", HighTierImplementationStatus.Implemented, "Friendly-target trainer Tavern spells receive an extra cast."),
            Entry("BGS_018", 6, "Beast deathrattle aura", HighTierImplementationStatus.Implemented, "Deathrattle buffs friendly Beasts for combat."),
            Entry("BGDUO31_202", 6, "Out of scope", HighTierImplementationStatus.OutOfScope, "Duos teammate board card."),
            Entry("BGDUO31_211", 6, "Out of scope", HighTierImplementationStatus.OutOfScope, "Duos pass-scaling Magnetic card."),
            Entry("BGDUO33_150", 6, "Out of scope", HighTierImplementationStatus.OutOfScope, "Duos teammate sell trigger."),

            Entry("BG23_017", 7, "Blood Gem battlecry/deathrattle", HighTierImplementationStatus.Implemented, "Battlecry and deathrattle improve Blood Gems."),
            Entry("BG25_034", 7, "Golden battlecry", HighTierImplementationStatus.Implemented, "Battlecry makes friendly tier 6 or lower minions golden."),
            Entry("BG26_149", 7, "Magnetic echo", HighTierImplementationStatus.Implemented, "Magnetized stats are also copied onto this minion."),
            Entry("BG27_016", 7, "Shop growth battlecry/deathrattle", HighTierImplementationStatus.Implemented, "Battlecry and deathrattle improve Tavern minions this game."),
            Entry("BG27_017", 7, "Rally damage", HighTierImplementationStatus.Implemented, "Rally deals deterministic adjacent damage."),
            Entry("BG27_514", 7, "Spellcraft shop copy", HighTierImplementationStatus.Implemented, "Turn start adds a copy of a Tavern minion."),
            Entry("BG31_999", 7, "Start combat stitch", HighTierImplementationStatus.Implemented, "Destroys a neighbor at combat start and deathrattle summons the stored copy."),
            Entry("BG34_145", 7, "End turn hand buff", HighTierImplementationStatus.Implemented, "End turn buffs the leftmost hand minion by this minion's stats."),
            Entry("BG34_319", 7, "Tier 6 reward triggers", HighTierImplementationStatus.Implemented, "Battlecry, deathrattle, and rally add tier 6 minions."),
            Entry("BG34_320", 7, "Menagerie rally", HighTierImplementationStatus.Implemented, "Rally buffs one friendly minion of each type."),
            Entry("BG34_322", 7, "Combat summon aura", HighTierImplementationStatus.Implemented, "First three friendly combat summons gain this minion's largest stat."),
            Entry("BG34_950", 7, "Buy trigger", HighTierImplementationStatus.Implemented, "First bought minion each turn gains +10/+10 and doubled/tripled stats."),
            Entry("BGDUO_125", 7, "Out of scope", HighTierImplementationStatus.OutOfScope, "Duos teammate-copy combat card.")
        };

        public static IReadOnlyList<HighTierMinionImplementation> All => Entries;

        public static bool Contains(string cardId)
        {
            return Entries.Any(entry => string.Equals(entry.CardId, cardId, StringComparison.Ordinal));
        }

        private static HighTierMinionImplementation Entry(string cardId, int tavernTier, string area, HighTierImplementationStatus status, string note)
        {
            return new HighTierMinionImplementation
            {
                CardId = cardId,
                TavernTier = tavernTier,
                Area = area,
                Status = status,
                Note = note
            };
        }
    }
}
