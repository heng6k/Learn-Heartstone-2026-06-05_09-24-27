using System;
using System.Collections.Generic;
using System.Linq;

namespace LearnHearthstone.Domain.Data
{
    public enum TierFiveImplementationStatus
    {
        Implemented,
        SoloApproximation,
        KeywordOnly,
        OutOfScope
    }

    public sealed class TierFiveMinionImplementation
    {
        public string CardId;
        public string Area;
        public TierFiveImplementationStatus Status;
        public string Note;
    }

    public static class TierFiveMinionImplementationRegistry
    {
        private static readonly TierFiveMinionImplementation[] Entries =
        {
            Entry("BGS_104", "Elemental shop growth", TierFiveImplementationStatus.Implemented, "Elementals played grow current and future Tavern Elementals."),
            Entry("BG29_840", "Odd-tier play trigger", TierFiveImplementationStatus.Implemented, "Odd-tier cards buff odd-tier friendly minions."),
            Entry("BGS_012", "Mech deathrattle", TierFiveImplementationStatus.Implemented, "Deathrattle summons the first friendly Mechs that died this combat as plain copies."),
            Entry("BG_LOE_077", "Battlecry aura", TierFiveImplementationStatus.Implemented, "Friendly Battlecries repeat through the shared battlecry path."),
            Entry("BG33_888", "Battlecry generated Blood Gem", TierFiveImplementationStatus.Implemented, "Adds enhanced Blood Gem through the generated spell path."),
            Entry("BG25_354", "Deathrattle aura", TierFiveImplementationStatus.Implemented, "Friendly Deathrattles repeat in combat."),
            Entry("BG28_551", "Tavern spell one-of-each buff", TierFiveImplementationStatus.Implemented, "Tavern spells buff one friendly minion of each counted type."),
            Entry("BG35_123", "End turn spell copy", TierFiveImplementationStatus.Implemented, "Copies the last Tavern spell cast this turn."),
            Entry("BG28_550", "Battlecry Tavern spell discover", TierFiveImplementationStatus.Implemented, "Starts Tavern spell discover."),
            Entry("BG23_318", "Destroy killer deathrattle", TierFiveImplementationStatus.Implemented, "Deathrattle destroys the minion recorded as killing it."),
            Entry("BG26_199", "End turn left-copy", TierFiveImplementationStatus.Implemented, "Every two end turns copies the left neighbor to hand."),
            Entry("BG29_862", "Battlecry minion deathrattle reward", TierFiveImplementationStatus.Implemented, "Deathrattle rewards random Battlecry minions."),
            Entry("BG26_ICC_901", "End turn aura", TierFiveImplementationStatus.Implemented, "End-turn effects repeat once in the trainer."),
            Entry("BG34_694", "Deathrattle generated spell", TierFiveImplementationStatus.Implemented, "Deathrattle adds Disturbed Grave."),
            Entry("BG30_129", "Summon overflow buff", TierFiveImplementationStatus.Implemented, "Summon overflow buffs the friendly board."),
            Entry("BG28_308", "End turn Undead resummon", TierFiveImplementationStatus.Implemented, "End turn destroys the Undead to its left and summons a plain copy."),
            Entry("BG32_324", "Avenge generated spell", TierFiveImplementationStatus.Implemented, "Avenge rewards Butchering."),
            Entry("BG34_403", "Avenge summon", TierFiveImplementationStatus.Implemented, "Avenge summons an Eternal Knight token with immediate attack pressure."),
            Entry("BG35_334", "End turn/Avenge scaling", TierFiveImplementationStatus.Implemented, "End turn buffs board; Avenge improves the buff."),
            Entry("BG26_162", "Elemental shop growth", TierFiveImplementationStatus.Implemented, "Battlecry and Deathrattle improve Tavern Elementals."),
            Entry("BG35_882", "Battlecry generated spell", TierFiveImplementationStatus.Implemented, "Adds Conflagration."),
            Entry("BG34_858", "Gold spent spellcast", TierFiveImplementationStatus.Implemented, "Every seven Gold spent casts Borrowing East Wind."),
            Entry("BG32_111", "All-type generated spell", TierFiveImplementationStatus.Implemented, "Battlecry and Deathrattle add Menagerie Tableware."),
            Entry("BG32_891", "Deathrattle generated spell", TierFiveImplementationStatus.Implemented, "Deathrattle adds Staff of Enrichment."),
            Entry("BG32_873", "Hero damage shop buff", TierFiveImplementationStatus.Implemented, "Hero damage is rewound and current Tavern minions gain the temporary turn buff."),
            Entry("BG35_152", "Low-tier shop growth", TierFiveImplementationStatus.Implemented, "Current and future Tavern minions at tier 3 or lower gain stats."),
            Entry("BG21_005", "End turn devour", TierFiveImplementationStatus.Implemented, "Friendly Demons devour Tavern minions at end of turn."),
            Entry("BG28_633", "Spell-count devour", TierFiveImplementationStatus.Implemented, "Every three Tavern spells devours a Tavern minion."),
            Entry("BG32_821", "End turn Tavern spell scaling", TierFiveImplementationStatus.Implemented, "End turn improves Tavern spell stat bonus."),
            Entry("BG26_148", "Magnetic deathrattle reward", TierFiveImplementationStatus.Implemented, "Deathrattle rewards random Magnetic Mechs."),
            Entry("BG35_890", "Mech deathrattle combat buff", TierFiveImplementationStatus.Implemented, "Deathrattle buffs friendly Mechs in combat."),
            Entry("BG28_741", "Tavern spell shield attack trigger", TierFiveImplementationStatus.Implemented, "Tavern spells buff Divine Shield minions."),
            Entry("BG35_701", "End turn Pirate buff", TierFiveImplementationStatus.Implemented, "End turn buffs leftmost Pirate based on cards played."),
            Entry("BG33_821", "Bounty battlecry/deathrattle", TierFiveImplementationStatus.Implemented, "Battlecry and Deathrattle reward Bounties."),
            Entry("BG33_825", "Bounty aura", TierFiveImplementationStatus.Implemented, "Bounty Tavern spells cast one extra time."),
            Entry("BG34_922", "Combat Tavern spell aura", TierFiveImplementationStatus.Implemented, "Tavern spell casts receive one extra deterministic resolution in the trainer."),
            Entry("BG32_835", "Spellcraft Tavern spell scaling", TierFiveImplementationStatus.Implemented, "Generates a stat Tavern spell through Spellcraft."),
            Entry("BG23_008", "Spellcraft Divine Shield", TierFiveImplementationStatus.Implemented, "Generates a deterministic Divine Shield Spellcraft card."),
            Entry("BG35_604", "Beast deathrattle summon", TierFiveImplementationStatus.Implemented, "Deathrattle summons Taunt Beast tokens."),
            Entry("BG29_808", "Reborn deathrattle board damage", TierFiveImplementationStatus.Implemented, "Deathrattle buffs and damages the friendly board."),
            Entry("BG35_602", "Beast summon scaling", TierFiveImplementationStatus.Implemented, "Friendly Beast summons gain scaling Attack."),
            Entry("BG29_806", "Beast damage trigger", TierFiveImplementationStatus.Implemented, "Damaged friendly Beasts cause another Beast to gain stats."),
            Entry("BG31_809", "Beetle deathrattle growth", TierFiveImplementationStatus.Implemented, "Deathrattle improves Beetles and summons one."),
            Entry("BG26_867", "Blood Gem deathrattle", TierFiveImplementationStatus.Implemented, "Deathrattle applies Blood Gem stats to friendly Quilboar."),
            Entry("BG23_018", "Gold spent Blood Gem trigger", TierFiveImplementationStatus.Implemented, "Every eight Gold spent buffs friendly Quilboar."),
            Entry("BG30_121", "Blood Gem aura", TierFiveImplementationStatus.Implemented, "Blood Gems cast from hand receive an extra deterministic application."),
            Entry("BG35_142", "End turn Murgleton reward", TierFiveImplementationStatus.Implemented, "End turn adds Auntie or Daddy."),
            Entry("BG33_318", "Venomous strike", TierFiveImplementationStatus.Implemented, "Strike grants another friendly Murloc Venomous."),
            Entry("BG35_895", "Murloc/Tavern spell scaling", TierFiveImplementationStatus.Implemented, "Tavern spells get current-turn bonus and Murlocs played improve it."),
            Entry("BGS_020", "Murloc discover battlecry", TierFiveImplementationStatus.Implemented, "Battlecry discovers Murlocs when another friendly Murloc exists."),
            Entry("BG30_122", "Murloc played buff", TierFiveImplementationStatus.Implemented, "Murlocs played buff a friendly minion and hand minion."),
            Entry("BGS_041", "Battlecry Dragon aura", TierFiveImplementationStatus.Implemented, "Battlecries buff friendly Dragons."),
            Entry("BG34_633", "Chromawhelp reward", TierFiveImplementationStatus.Implemented, "Battlecry and Deathrattle reward Chromawhelps."),
            Entry("BGDUO_105", "Out of scope", TierFiveImplementationStatus.OutOfScope, "Duos teammate board card."),
            Entry("BGDUO_109", "Out of scope", TierFiveImplementationStatus.OutOfScope, "Duos teammate board card."),
            Entry("BGDUO_120", "Out of scope", TierFiveImplementationStatus.OutOfScope, "Duos pass card."),
            Entry("BGDUO_121", "Out of scope", TierFiveImplementationStatus.OutOfScope, "Duos team Tavern card."),
            Entry("BGDUO_122", "Out of scope", TierFiveImplementationStatus.OutOfScope, "Duos pass Tavern spell card."),
            Entry("BGDUO31_205", "Out of scope", TierFiveImplementationStatus.OutOfScope, "Duos team Gold cap card.")
        };

        public static IReadOnlyList<TierFiveMinionImplementation> All => Entries;

        public static bool Contains(string cardId)
        {
            return Entries.Any(entry => string.Equals(entry.CardId, cardId, StringComparison.Ordinal));
        }

        private static TierFiveMinionImplementation Entry(string cardId, string area, TierFiveImplementationStatus status, string note)
        {
            return new TierFiveMinionImplementation
            {
                CardId = cardId,
                Area = area,
                Status = status,
                Note = note
            };
        }
    }
}
