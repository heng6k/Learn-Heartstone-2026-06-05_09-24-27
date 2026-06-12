using System;
using System.Collections.Generic;
using System.Linq;

namespace LearnHearthstone.Domain.Data
{
    public enum TierFourImplementationStatus
    {
        Implemented,
        SoloApproximation,
        KeywordOnly,
        OutOfScope
    }

    public sealed class TierFourMinionImplementation
    {
        public string CardId;
        public string Area;
        public TierFourImplementationStatus Status;
        public string Note;
    }

    public static class TierFourMinionImplementationRegistry
    {
        private static readonly TierFourMinionImplementation[] Entries =
        {
            Entry("BG_DAL_775", "CombatAoeDeathrattle", TierFourImplementationStatus.Implemented, "Deathrattle deals area damage to all minions."),
            Entry("BG_DEEP_015", "Magnetic/keywords", TierFourImplementationStatus.Implemented, "Magnetic Reborn Undead/Mech attachment is supported through the shared magnetic path."),
            Entry("BG21_014", "StartOfCombat", TierFourImplementationStatus.Implemented, "Start of combat buffs friendly Dragons."),
            Entry("BG24_018", "Economy/Sell", TierFourImplementationStatus.Implemented, "Sells for five Gold after losing the previous combat."),
            Entry("BG25_016", "StrikePurgeAndSteal", TierFourImplementationStatus.Implemented, "Strike removes target Reborn and Taunt before damage."),
            Entry("BG26_137", "Hand trigger", TierFourImplementationStatus.Implemented, "Grows in hand after friendly Murlocs are played."),
            Entry("BG26_505", "SpellcraftCopyAndGeneratedSpellPool", TierFourImplementationStatus.Implemented, "Copies the first Spellcraft spell used on it each turn."),
            Entry("BG26_525", "Battlecry/discover", TierFourImplementationStatus.Implemented, "Starts Demon discover and damages the hero by the picked Demon's Tavern tier."),
            Entry("BG26_801", "BattlecryEchoDeathrattle", TierFourImplementationStatus.Implemented, "Deathrattle triggers one adjacent Battlecry, or both adjacent Battlecries when golden, through combat-local effects and rewards."),
            Entry("BG26_802", "Summon trigger", TierFourImplementationStatus.Implemented, "Friendly Beast summons have their Attack doubled in combat."),
            Entry("BG26_814", "Gold spent battlecry", TierFourImplementationStatus.Implemented, "Battlecry buffs a Pirate's Health by one plus Gold spent this turn; golden repeats the buff."),
            Entry("BG26_817", "Cleave", TierFourImplementationStatus.Implemented, "Deals attack damage to adjacent minions of the attack target."),
            Entry("BG27_556", "CombatHandSummon", TierFourImplementationStatus.Implemented, "Summons highest-attack hand Murloc as a combat-only copy."),
            Entry("BG28_583", "BloodGemRedistribution", TierFourImplementationStatus.Implemented, "Blood Gems used on it apply a second gem to a different friendly minion."),
            Entry("BG29_503", "Magnetic/discover", TierFourImplementationStatus.Implemented, "Battlecry targets a friendly Mech, opens magnetic Mech discover, and magnetizes the chosen card; golden repeats the discover."),
            Entry("BG29_807", "CombatPersistentCarryover", TierFourImplementationStatus.Implemented, "Combat health growth is written back after combat."),
            Entry("BG29_813", "CombatPersistentCarryover", TierFourImplementationStatus.Implemented, "Adjacent Dragons retain positive combat stats and new keywords."),
            Entry("BG30_117", "Spellcraft", TierFourImplementationStatus.Implemented, "Generates attack and health Spellcraft choices with temporary buff cleanup."),
            Entry("BG30_123", "Choose One/Blood Gem", TierFourImplementationStatus.Implemented, "Uses the discover UI to choose Blood Gem quality growth or generated Blood Gems."),
            Entry("BG31_175", "StrikePurgeAndSteal", TierFourImplementationStatus.Implemented, "Strike rewards random magnetic Mech cards."),
            Entry("BG31_178", "End turn", TierFourImplementationStatus.Implemented, "End of turn adds random Tavern spells."),
            Entry("BG31_824", "Gold spent", TierFourImplementationStatus.Implemented, "Every five Gold spent buffs two friendly Pirates."),
            Entry("BG32_172", "Magnetic deathrattle", TierFourImplementationStatus.Implemented, "Deathrattle summons an Ancestral Automaton token."),
            Entry("BG32_341", "Tavern spell aura", TierFourImplementationStatus.Implemented, "Current Tavern spells receive +1/+2 extra stats."),
            Entry("BG32_433", "Avenge/Blood Gem", TierFourImplementationStatus.Implemented, "Avenge improves Blood Gem Health."),
            Entry("BG32_841", "Battlecry growth", TierFourImplementationStatus.Implemented, "Elementals gain permanent shop/hand/board attack growth."),
            Entry("BG32_880", "Deathrattle growth", TierFourImplementationStatus.Implemented, "Deathrattle improves future Tavern spell attack buffs."),
            Entry("BG33_155", "CombatPersistentCarryover", TierFourImplementationStatus.Implemented, "Grows from other friendly Demon damage and writes back after combat."),
            Entry("BG33_156", "CombatAoeDeathrattle", TierFourImplementationStatus.Implemented, "Deathrattle damages all minions except friendly Demons."),
            Entry("BG33_319", "Spellcraft", TierFourImplementationStatus.Implemented, "Generates random stat-granting Tavern spells through the Spellcraft path."),
            Entry("BG33_822", "BountyExtension", TierFourImplementationStatus.Implemented, "Strike adds generated Bounty Tavern spells."),
            Entry("BG34_500", "End turn devour", TierFourImplementationStatus.Implemented, "End of turn devours highest-Health Tavern minion."),
            Entry("BG34_523", "Battlecry/discover", TierFourImplementationStatus.Implemented, "Starts Beast discover."),
            Entry("BG34_604", "StrikePurgeAndSteal", TierFourImplementationStatus.Implemented, "Strike gains target Attack before damage."),
            Entry("BG34_632", "Avenge/generated pool", TierFourImplementationStatus.Implemented, "Avenge rewards generated Chromawhelps."),
            Entry("BG34_639", "Delayed hatch", TierFourImplementationStatus.Implemented, "Locks in hand for two turns, then discovers a tier-6 Dragon and hatches into the chosen card."),
            Entry("BG34_682", "Deathrattle reward", TierFourImplementationStatus.Implemented, "Deathrattle adds Blood Gem Barrage."),
            Entry("BG34_690", "Deathrattle growth", TierFourImplementationStatus.Implemented, "Deathrattle improves Undead attack; outside-combat death gives larger growth."),
            Entry("BG34_731", "Deathrattle summon", TierFourImplementationStatus.Implemented, "Summons Taunt Twilight Hatchlings with seven-slot overflow handling."),
            Entry("BG34_865", "Battlecry refresh", TierFourImplementationStatus.Implemented, "Future refreshes buff a Tavern minion."),
            Entry("BG34_925", "Strike spellcast", TierFourImplementationStatus.Implemented, "Strike casts Chef's Choice on the right neighbor and rewards a random minion sharing its selected type; golden triggers twice."),
            Entry("BG35_143", "Generated spell pool", TierFourImplementationStatus.Implemented, "Battlecry and Deathrattle add Deepwater School generated spells."),
            Entry("BG35_151", "Refresh queue", TierFourImplementationStatus.Implemented, "End of turn queues Demon Fodder for the next three refreshes."),
            Entry("BG35_341", "Magnetic/Tavern spell aura", TierFourImplementationStatus.Implemented, "Magnetic and current Tavern spell +1/+1 aura are supported."),
            Entry("BG35_433", "Generated Blood Gem", TierFourImplementationStatus.Implemented, "End of turn adds a Reborn-granting Blood Gem for Quilboar."),
            Entry("BG35_601", "Damage refresh", TierFourImplementationStatus.Implemented, "Actual combat damage queues free refresh rewards, capped at three triggers per combat."),
            Entry("BG35_702", "Battlecry scaling", TierFourImplementationStatus.Implemented, "Battlecry buff scales with Tavern spells cast this turn."),
            Entry("BG35_881", "Generated spell pool", TierFourImplementationStatus.Implemented, "Battlecry and Deathrattle add Arcane Consumption generated spells."),
            Entry("BG35_921", "Tavern spell trigger", TierFourImplementationStatus.Implemented, "Grows whenever a Tavern spell is cast."),
            Entry("BGS_030", "Battlecry", TierFourImplementationStatus.Implemented, "Buffs other Murlocs in hand and on board."),
            Entry("BGS_078", "Strike deathrattle", TierFourImplementationStatus.Implemented, "Strike triggers leftmost other friendly Deathrattle."),
            Entry("BGS_116", "Battlecry", TierFourImplementationStatus.Implemented, "Battlecry grants free refreshes."),
            Entry("BGS_123", "Battlecry", TierFourImplementationStatus.Implemented, "Battlecry adds random Elementals."),
            Entry("BGDUO_108", "Out of scope", TierFourImplementationStatus.OutOfScope, "Duos/pass card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO_110", "Out of scope", TierFourImplementationStatus.OutOfScope, "Duos card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO_112", "Out of scope", TierFourImplementationStatus.OutOfScope, "Duos/teammate card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO31_203", "Out of scope", TierFourImplementationStatus.OutOfScope, "Duos/teammate card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO31_208", "Out of scope", TierFourImplementationStatus.OutOfScope, "Duos/team counter card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO31_209", "Out of scope", TierFourImplementationStatus.OutOfScope, "Duos/pass card. Do not implement in this single-player Tavern project."),
            Entry("BGDUO31_212", "Out of scope", TierFourImplementationStatus.OutOfScope, "Duos/pass card. Do not implement in this single-player Tavern project.")
        };

        public static IReadOnlyList<TierFourMinionImplementation> All => Entries;

        public static bool Contains(string cardId)
        {
            return Entries.Any(entry => string.Equals(entry.CardId, cardId, StringComparison.Ordinal));
        }

        private static TierFourMinionImplementation Entry(string cardId, string area, TierFourImplementationStatus status, string note)
        {
            return new TierFourMinionImplementation
            {
                CardId = cardId,
                Area = area,
                Status = status,
                Note = note
            };
        }
    }
}
