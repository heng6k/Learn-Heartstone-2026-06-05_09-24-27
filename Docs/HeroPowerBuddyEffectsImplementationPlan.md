# Hero Power and Buddy Effects Implementation Plan

## Goal

Implement hero power effects and their matching hero buddy effects through one shared runtime path. Hero powers own the core rule, and buddies either enhance that rule or react to the same trigger while they are on the player's board.

For the concrete per-hero rollout order, see `HeroPowerBuddyEffectsImplementationOrder.md`.

## Architecture

- `MatchService` remains the orchestration layer for commands, economy, shop changes, and recruit logs.
- `HeroEffectEngine` owns card-id keyed hero power and buddy behavior.
- `TavernState.HeroEffectCounters` stores persistent counters such as Kael'thas minion buys, Tae'thelan spell buys, and per-turn buddy limits.
- Existing `MinionFactory.Create(HeroBuddyDefinition)` keeps buddies as playable `CardKind.HeroBuddy` minion cards.
- Active hero powers use `GameCommandType.UseHeroPower` with the existing command target index field for later targeted skills.

## Dispatch Points

- `HeroEffectEventType.MatchStarted`: one-time setup and passive normalization.
- `HeroEffectEventType.HeroPowerUsed`: active hero powers that spend gold and immediately resolve.
- `HeroEffectEventType.CardBought`: buy counters, spell discounts, zero-cost spell copy checks.
- `HeroEffectEventType.CardPlayed`: played-minion and buddy battlecry style effects.
- `HeroEffectEventType.TavernSpellCast`: once-per-turn spell-copy buddy effects.
- `HeroEffectEventType.MinionSold`: sell counters and sell-triggered buddies.
- `HeroEffectEventType.ShopRefreshed`: refresh-copy and refresh-buff heroes.
- `HeroEffectEventType.TurnStarted` / `TurnEnded`: recurring free refreshes, end-turn buffs, and delayed cleanup.

## First Implemented Slice

- Patchwerk: health is already initialized from hero data; Weebomination buffs the minion to its left at end of turn by `+0/+1`, improved by missing hero Health.
- Forest Lord Cenarius: active hero power spends 3 gold to increase maximum gold by 1; Malorne gains tracked stats equal to `floor(gold spent this game / 3)`.
- Nozdormu: start of turn grants one free refresh; Chromie makes one refresh per turn helpful by buffing a random Tavern minion.
- Kael'thas Sunstrider: every third bought minion grants a Tavern Coin; Crimson Hand Centurion gains the last bought minion's stats when Verdant Spheres triggers.
- Exarch Othaar: from turn 3 onward, start of turn discounts the next bought Tavern spell by 1; The Celestial Archive copies bought zero-cost Tavern spells.
- Tae'thelan Bloodwatcher: every fourth bought Tavern spell costs 0; Reliquary Attendant copies the first cast Tavern spell each turn.
- Varden Dawngrasp: after refresh, copy the highest-tier Tavern minion and freeze the shop; Varden's Aquarrior also buffs both copies by the player's Tavern tier.
- Millhouse Manastorm: minion, refresh, and upgrade costs are adjusted; Magnus Manastorm grants the first two normal refreshes each turn for free while on board.
- Trade Prince Gallywix: sold minions bank Gold for next turn; Bilgewater Mogul increases maximum Gold at end of turn.
- Forest Warden Omu: Tavern upgrades refund 2 Gold; Evergreen Botani adds a random eligible minion to the board at end of turn.
- Cap'n Hoggarr: buying Pirates grants 1 Gold; Shining Sailor grows from bought Pirates and Hoggarr refreshes can inject a Pirate into the Tavern.
- Ysera: refreshes guarantee a Dragon in the Tavern; Valithria Dreamwalker grows based on Dragons on the player's board.
- Enhance-o Mechano: refreshes give a random Tavern minion a random Bonus Keyword; Enhance-o Medico gains +3/+3 for each Bonus Keyword on bought minions.
- Kurtrus Ashfallen: once each turn, every third bought minion creates a plain copy; Living Nightmare buffs Tavern minions after card buys.
- Fungalmancer Flurgl: every fifth sold minion adds a random Murloc to hand; Sparkfin Soothsayer transforms Tavern minions into same-tier Murlocs.
- Overlord Saurfang: buying four minions improves the Tavern health buff; Dranosh Saurfang gains half of bought minions' stats.
- Edwin VanCleef: targeted Sharpen Blades grows after five bought cards; SI:7 Scout gains +2/+2 after each bought card.
- Skycap'n Kragg: Piggy Bank grants growing once-per-game Gold; Sharkbait refreshes that Hero Power when sold.
- George the Fallen: Boon of Light gives a friendly minion Divine Shield; Karl the Lost gives Divine Shield minions +2 Attack after hero power use.
- Farseer Nobundo: The Galaxy's Lens copies the last cast Tavern spell and gets a stacking next-use hero power discount each turn.
- Doctor Holli'dae: Blessing of the Nine Frogs gives a random Tavern spell; The Nine Frogs gives same-tier Tavern spells after minion buys for nine charges.
- Death Speaker Blackthorn: Bloodbound grants Blood Gems twice per turn; Death's Head Sage adds extra Blood Gem copies.
- Lich Baz'hial: Graveyard Shift steals a Tavern card and deals self-damage; Unearthed Underling rewinds that damage and gains matching stats.
- Rakanishu: Tavern Lighting gives a playable Lantern Light; Lantern Tender adds two random stat Tavern spells at end of turn.
- Reno Jackson: Gonna Be Rich! makes a friendly minion Golden once per game; Sr. Tomb Diver uses the current Tavern death proxy to Golden the right-most minion.
- Patches the Pirate: Pirate Parrrrty! gains Pirates with buy-based discounts; Tuskarr Raider gains Bounties on play and the current Tavern death proxy.
- King Mukla: Bananarama adds playable Bananas at turn start; Crazy Monkey improves after Tavern spells and feeds Bananas when sold.
- C'Thun: Saturday C'Thuns! applies improving end-turn buffs; Tentacle of C'Thun gains temporary tracked stats from those buffs.
- Captain Eudora: Buried Treasure tracks four digs and rewards a Golden minion; Dagwik Stickytoe buffs a Golden friendly minion at end of turn.
- Elise Starseeker: Lead Explorer discovers a current-tier minion with increasing cost; Jr. Navigator reduces that cost on play.
- Millificent Manastorm: Tinker discovers Magnetic Mechs after Tier 4; Elementium Squirrel Bomb uses the current Tavern death proxy for Mech-death damage.
- The Lich King: Reborn Rites grants temporary Reborn; Arfus adds its Attack when the hero power gives Reborn.
- Shudderwock: Muckslinger gives a random Battlecry minion; Snicker-snack records a visible Battlecry replay proxy until the Battlecry resolver is public.
- Jandice Barov: Swap, Lock, & Shop It swaps a friendly non-Golden minion with a Tavern minion; Jandice's Apprentice buffs the board after repeat plays.
- Mutanus the Devourer: Devour removes a friendly minion, grants sell Gold, and spits stats to random friendly minions; Nightmare Ectoplasm adds an extra spit target when devoured.
- Xyrella: See the Light sets a Tavern minion to 2/2 and adds it to hand; Baby Elekk buffs lower-Attack played minions and improves its buff amount.
- Pyramad: Brick by Brick steals a random Tavern minion and doubles Health; Titanic Guardian follows hero-effect Health gains in this layer.
- Vol'jin: Spirit Swap currently uses one explicit target plus a random friendly partner; true two-target commands and Master Gadrin's start-of-combat hook remain framework work.
- Inge, the Iron Hymn: Major Hymn alternates Attack/Health by turn; Solemn Serenader enhances the hero-power target.
- Malygos: Arcane Alteration replaces board or Tavern targets twice per turn; Nexus Lord shifts replacements one Tier higher.
- Maiev Shadowsong: Imprison reuses existing hand lock counters for a two-turn delayed card; Shadow Warden makes the next imprisoned card Golden.
- Zephrys, the Great: Three Wishes finds the third copy; Phyresz starts a singleton plain-copy Discover via the Tavern death proxy.
- Captain Hooktusk: Trash for Treasure removes a friendly minion and starts a lower-tier Discover; Raging Contender grants Gold equal to the removed minion's Tier.
- Rock Master Voone: hero and buddy end-turn counters copy the left-most hand card every three and two turns respectively.
- Zerek, Master Cloner: Cloning Gallery summons an exact friendly copy once per game; Mini-Zerek uses the first Tavern minion as the current Tavern-target proxy.
- Heistbaron Togwaggle: The Perfect Crime steals Tavern cards with a per-turn discount; Waxadred uses the current opponent board as the available opponent-warband proxy.
- Chenvaala: every third played Elemental reduces upgrade cost; Snow Elemental adds an extra Frozen Elemental on refresh.
- The Curator: MatchStarted now creates the Venomous all-type Amalgam; Mishmash mirrors that Amalgam's positive stat delta.
- Dancin' Deryl: Hat Trick and Asher hats are implemented through played-minion and sell events.
- Ragnaros the Firelord: buying 16 cards unlocks Sulfuras end-turn buffs; Lucifron repeats that implemented end-turn effect.
- Time Twister Chromie: shop refresh converts offered slots into Tavern spells.
- Sindragosa: minion cost, end-turn freeze, and Thawed Champion Golden proxy are implemented; exact smaller shop and per-card Frozen state remain framework work.
- Phase 5 combat-context slice: implemented the combat-start hero context plus exact or framework-first behavior for Al'Akir, Y'Shaarj, Deathwing, Illidan, Queen Wagtoggle, N'Zoth, Vanndar, Drek'Thar, Teron, Sylvanas/Nathanos, Sneed, The Jailer, Greybough, Ini, Ozumat, Aranna/Sklibb, and Jaraxxus/Kil'rek.

Known proxy and framework-first implementations are tracked in `Docs/HeroEffectImplementationGaps.md` so incomplete combat, Deathrattle, kill, choice, and history systems stay visible during later batches.

## Deferred Families

These remain explicit follow-up slices because they need additional subsystems:

- Discover/choice heroes with multi-step UI beyond current `DiscoverState`.
- Kill tracking, last-opponent memory, and combat-targeting heroes.
- Quests, trinkets, timewarp, secrets, StarCraft race systems, and custom undead creation.
- Duos-only effects.

## Testing

- Add focused EditMode tests for each implemented family.
- Verify `UseHeroPower` cost/payment behavior.
- Verify buddy effects only run while the matching buddy card is on the board.
- Keep existing Unmasked Identity and hero catalog tests green.
