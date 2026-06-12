using System;
using System.Collections.Generic;
using System.Linq;

namespace LearnHearthstone.Domain.Data
{
    public enum TierThreeImplementationStatus
    {
        Implemented,
        SoloApproximation,
        KeywordOnly,
        OutOfScope
    }

    public sealed class TierThreeMinionImplementation
    {
        public string CardId;
        public string Area;
        public TierThreeImplementationStatus Status;
        public string Note;
    }

    public static class TierThreeMinionImplementationRegistry
    {
        private static readonly TierThreeMinionImplementation[] Entries =
        {
            Entry("BG_BOT_911", "Magnetic/keywords", TierThreeImplementationStatus.Implemented, "Magnetic attach to Mechs; Divine Shield and Taunt transfer."),
            Entry("BG23_004", "Spellcraft", TierThreeImplementationStatus.Implemented, "Adds a Deep Sea spellcraft spell each turn."),
            Entry("BG24_500", "Combat start", TierThreeImplementationStatus.Implemented, "Buffs another friendly Dragon and grants Divine Shield."),
            Entry("BG24_707", "Combat death", TierThreeImplementationStatus.Implemented, "Friendly Taunt deaths queue Blood Gems."),
            Entry("BG25_010", "Combat deathrattle", TierThreeImplementationStatus.Implemented, "Summons Reborn Undead tokens with pointer retargeting."),
            Entry("BG25_032", "Hand trigger", TierThreeImplementationStatus.Implemented, "Cards added to hand buff another friendly Pirate."),
            Entry("BG25_039", "Spell trigger", TierThreeImplementationStatus.Implemented, "Deterministic spell target gains temporary Venomous."),
            Entry("BG25_041", "Battlecry", TierThreeImplementationStatus.Implemented, "Current and future shop minions gain +2/+1."),
            Entry("BG25_806", "Combat deathrattle", TierThreeImplementationStatus.Implemented, "Summons deterministic Beast token."),
            Entry("BG26_147", "Turn start", TierThreeImplementationStatus.Implemented, "Gains start-of-turn gold."),
            Entry("BG26_159", "Battlecry", TierThreeImplementationStatus.Implemented, "Improves Blood Gem health."),
            Entry("BG26_160", "Combat deathrattle", TierThreeImplementationStatus.Implemented, "Improves Blood Gem attack after combat."),
            Entry("BG26_360", "Combat deathrattle", TierThreeImplementationStatus.Implemented, "Buffs a hand minion after combat."),
            Entry("BG26_502", "Spellcraft", TierThreeImplementationStatus.Implemented, "Adds and scales Deep Blue spellcraft."),
            Entry("BG26_524", "Refresh", TierThreeImplementationStatus.Implemented, "Two refreshes can cost health instead of gold."),
            Entry("BG26_810", "Gold spent", TierThreeImplementationStatus.Implemented, "Every six gold spent buffs friendly Pirates' attack."),
            Entry("BG27_005", "Tavern spell trigger", TierThreeImplementationStatus.Implemented, "Tavern spells buff friendly minion attack."),
            Entry("BG27_084", "Choose One", TierThreeImplementationStatus.Implemented, "Sprightly Scarab exposes both official Choose One branches and respects the selected Beast target."),
            Entry("BG28_303", "Battlecry", TierThreeImplementationStatus.Implemented, "Destroys the selected friendly Undead and adds plain copies, with Golden producing two copies."),
            Entry("BG28_309", "Combat deathrattle", TierThreeImplementationStatus.Implemented, "Gives a different friendly Undead Reborn."),
            Entry("BG29_816", "Combat attack trigger", TierThreeImplementationStatus.Implemented, "Buffs attacking Dragons."),
            Entry("BG30_125", "Combat deathrattle", TierThreeImplementationStatus.Implemented, "Summons Skeleton tokens."),
            Entry("BG31_859", "Magnetic", TierThreeImplementationStatus.Implemented, "Magnetizes to Mechs and Elementals."),
            Entry("BG32_434", "Combat deathrattle", TierThreeImplementationStatus.Implemented, "Applies Blood Gems to adjacent minions."),
            Entry("BG32_842", "Combat deathrattle", TierThreeImplementationStatus.Implemented, "Queues future Elemental health growth."),
            Entry("BG33_323", "Combat rally", TierThreeImplementationStatus.Implemented, "Buffs friendly Undead attack when it attacks."),
            Entry("BG33_820", "End turn", TierThreeImplementationStatus.Implemented, "Adds random generated Bounty Tavern Spells from the single-player bounty pool."),
            Entry("BG33_894", "Battlecry/deathrattle", TierThreeImplementationStatus.Implemented, "Adds tier 1 Tavern spells."),
            Entry("BG34_312", "Combat damage trigger", TierThreeImplementationStatus.Implemented, "Buffs other friendly minions after taking damage."),
            Entry("BG34_634t", "Battlecry", TierThreeImplementationStatus.Implemented, "Adds a random 2-cost Tavern spell."),
            Entry("BG34_635t", "Battlecry", TierThreeImplementationStatus.Implemented, "Improves Tavern spell health buffs."),
            Entry("BG34_636t", "Battlecry", TierThreeImplementationStatus.Implemented, "Buffs other friendly Dragons +2/+4."),
            Entry("BG34_637t", "Battlecry", TierThreeImplementationStatus.Implemented, "Buffs other friendly Dragons +4/+2."),
            Entry("BG34_638t", "Battlecry", TierThreeImplementationStatus.Implemented, "Improves Tavern spell attack buffs."),
            Entry("BG34_683", "Battlecry", TierThreeImplementationStatus.Implemented, "Adds Blood Gem Barrage."),
            Entry("BG34_856", "Combat deathrattle", TierThreeImplementationStatus.Implemented, "Future refreshes buff a shop minion."),
            Entry("BG35_140", "Battlecry", TierThreeImplementationStatus.Implemented, "Scales other Murloc attack by Murgle count."),
            Entry("BG35_141", "Battlecry", TierThreeImplementationStatus.Implemented, "Scales other Murloc health by Murgle count."),
            Entry("BGDUO_107", "Out of scope", TierThreeImplementationStatus.OutOfScope, "Duos card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO_115", "Out of scope", TierThreeImplementationStatus.OutOfScope, "Duos/pass card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO_117", "Out of scope", TierThreeImplementationStatus.OutOfScope, "Duos/pass card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO_118", "Out of scope", TierThreeImplementationStatus.OutOfScope, "Duos card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO_119", "Out of scope", TierThreeImplementationStatus.OutOfScope, "Duos card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO31_207", "Out of scope", TierThreeImplementationStatus.OutOfScope, "Duos card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO33_140", "Out of scope", TierThreeImplementationStatus.OutOfScope, "Duos card. Do not implement in this single-player Tavern project."),
            Entry("BGS_071", "Combat summon trigger", TierThreeImplementationStatus.Implemented, "Mech summons buff it and restore Divine Shield."),
            Entry("BGS_126", "Combat overkill", TierThreeImplementationStatus.Implemented, "Excess attack damage splashes to adjacent enemies."),
            Entry("BGS_131", "Keywords", TierThreeImplementationStatus.Implemented, "Core Venomous keyword resolves in combat and is consumed after damaged-target destruction.")
        };

        public static IReadOnlyList<TierThreeMinionImplementation> All => Entries;

        public static bool Contains(string cardId)
        {
            return Entries.Any(entry => string.Equals(entry.CardId, cardId, StringComparison.Ordinal));
        }

        public static TierThreeMinionImplementation Get(string cardId)
        {
            return Entries.First(entry => string.Equals(entry.CardId, cardId, StringComparison.Ordinal));
        }

        private static TierThreeMinionImplementation Entry(string cardId, string area, TierThreeImplementationStatus status, string note)
        {
            return new TierThreeMinionImplementation
            {
                CardId = cardId,
                Area = area,
                Status = status,
                Note = note
            };
        }
    }
}
