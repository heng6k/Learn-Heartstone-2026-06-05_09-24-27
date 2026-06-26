# Remaining Trinket Batch Implementation Plan

Last updated: 2026-06-26, after Batch 4 completion.

## Purpose

The one-by-one JSON order is now too slow. This document groups the remaining unimplemented Battlegrounds Trinkets by reusable runtime work, so future work can land several related effects in one pass.

Current catalog snapshot:

| Metric | Count |
| --- | ---: |
| Total Trinkets | 330 |
| Implemented | 266 |
| Remaining unimplemented | 64 |
| Remaining Lesser | 34 |
| Remaining Greater | 30 |

The implementation order below is intentionally not strict JSON order. Each batch should add or harden one shared mechanism, then implement every listed Trinket that can use it.

## Batch Rules

For each batch:

1. Add or harden shared helpers first.
2. Implement Lesser and Greater variants together when the effect only differs by numbers.
3. Update `battlegroundsTrinkets.json` only for effects that are real and tested.
4. Convert `requires` to arrays for every newly implemented entry.
5. Add catalog assertions and focused behavior tests.
6. Run targeted Unity MCP tests, then full `TrinketSystemTests` if a shared trigger changed.
7. Keep uncertain or proxy-heavy effects `DebugOnly` until the required system exists.

Recommended order:

1. Parameterized low-risk backfill.
2. Periodic generators and turn-end rewards.
3. Shop, refresh, cost, buy, and economy.
4. Discover, copy, Spellcraft, and generated hand cards.
5. Combat start and combat event engine.
6. Tribe portraits and specified-minion rewrites.
7. High-risk cross-system items.

## Batch 1: Parameterized Low-Risk Backfill

Count: 27 items, complete.

Why this is first: these effects mostly reuse hooks that already exist, or they are numeric variants of implemented mechanics. This batch should produce a large count jump with modest blast radius.

Shared work:

- Add a parameter table for simple stat auras and "improve this" counters.
- Reuse `DispatchTrinketCardPlayed`, spell-cast hooks, Tavern spell hooks, `DispatchTrinketMagnetized`, and end-turn dispatch.
- Make board/shop/hand/wherever buffs use counted-tribe helpers.
- Keep random target selection deterministic with `SeededRng`.

Completed in first wave:

- `BG30_MagicItem_900` Dragonwing Glider (Lesser).
- `BG35_MagicItem_300` Copper Coil (Lesser).
- `BG30_MagicItem_984` Charging Staff (Lesser).
- `BG32_MagicItem_808` Bloodbound Earrings (Lesser).
- `BG30_MagicItem_880` / `BG30_MagicItem_880t` Feral Talisman.
- `BG30_MagicItem_989` / `BG30_MagicItem_989t` Artisanal Urn.
- `BG32_MagicItem_231` / `BG32_MagicItem_231t` Gilded Anchor.

Completed in second wave:

- `BG30_MagicItem_422` / `BG30_MagicItem_422t` Lorewalker Scroll.
- `BG30_MagicItem_914` / `BG30_MagicItem_914t` Nerglish Phrasebook.
- `BG30_MagicItem_544` / `BG30_MagicItem_544t` Nomi Sticker.
- `BG32_MagicItem_802` / `BG32_MagicItem_802t` Fountain Pen.

Completed in final Batch 1 wave:

- `BG30_MagicItem_988` / `BG30_MagicItem_988t` Great Boar Sticker.
- `BG32_MagicItem_893` Bluegill Flippers.
- `BG32_MagicItem_170` Spell-powered Wrench.
- `BG32_MagicItem_888` Recycling Sticker.
- `BG32_MagicItem_954` Auric Offering.
- `BG32_MagicItem_111` Toxic Stinger.
- `BG32_MagicItem_276` Enigmatic Headstone.
- `BG32_MagicItem_279` Tough Tusk Sticker.

Items:

| Card ID | Slot | Name | Shared implementation path |
| --- | --- | --- | --- |
| `BG30_MagicItem_989` | Lesser | Artisanal Urn | Undead Attack aura, paired with Greater. |
| `BG30_MagicItem_989t` | Greater | Artisanal Urn | Undead Attack aura, paired with Lesser. |
| `BG30_MagicItem_880` | Lesser | Feral Talisman | Generic friendly board aura. |
| `BG30_MagicItem_880t` | Greater | Feral Talisman | Generic friendly board aura. |
| `BG32_MagicItem_231` | Lesser | Gilded Anchor | End-turn Golden minion buff. |
| `BG32_MagicItem_231t` | Greater | Gilded Anchor | End-turn Golden minion buff. |
| `BG30_MagicItem_984` | Lesser | Charging Staff | Lesser variant of implemented Greater Charging Staff. |
| `BG35_MagicItem_300` | Lesser | Copper Coil | Lesser variant of implemented Greater Copper Coil. |
| `BG30_MagicItem_900` | Lesser | Dragonwing Glider | Lesser variant of implemented Greater Dragonwing. |
| `BG32_MagicItem_808` | Lesser | Bloodbound Earrings | Lesser threshold/value variant of existing spell-count Blood Gem handler. |
| `BG30_MagicItem_422` | Lesser | Lorewalker Scroll | Spell-on-minion buff, paired with Greater. |
| `BG30_MagicItem_422t` | Greater | Lorewalker Scroll | Spell-on-minion buff, paired with Lesser. |
| `BG30_MagicItem_914` | Lesser | Nerglish Phrasebook | Play-minion hand buff, paired with Greater. |
| `BG30_MagicItem_914t` | Greater | Nerglish Phrasebook | Play-minion hand buff, paired with Lesser. |
| `BG30_MagicItem_544` | Lesser | Nomi Sticker | Elemental-play shop growth, paired with Greater. |
| `BG30_MagicItem_544t` | Greater | Nomi Sticker | Elemental-play shop growth, paired with Lesser. |
| `BG32_MagicItem_802` | Lesser | Fountain Pen | Extra stat grants from Elementals. |
| `BG32_MagicItem_802t` | Greater | Fountain Pen | Extra stat grants from Elementals. |
| `BG30_MagicItem_988` | Lesser | Great Boar Sticker | Blood Gem bonus, paired with Greater. |
| `BG30_MagicItem_988t` | Greater | Great Boar Sticker | Blood Gem bonus, paired with Lesser. |
| `BG32_MagicItem_893` | Lesser | Bluegill Flippers | Tavern Spell cast buffs left-most hand and board minions. |
| `BG32_MagicItem_170` | Lesser | Spell-powered Wrench | Played Magnetic minion gives Tavern Spell. |
| `BG32_MagicItem_888` | Greater | Recycling Sticker | Played Elemental grants free refresh. |
| `BG32_MagicItem_954` | Lesser | Auric Offering | End-turn left-most buff repeats for Golden minions. |
| `BG32_MagicItem_111` | Greater | Toxic Stinger | End-turn random Murloc buff plus Venomous. |
| `BG32_MagicItem_276` | Lesser | Enigmatic Headstone | End-turn Undead wherever Attack growth. |
| `BG32_MagicItem_279` | Lesser | Tough Tusk Sticker | Hand-played Blood Gems give temporary Divine Shield. |

Suggested done test shape:

- One catalog test update for all implemented items.
- One paired-value test per shared helper.
- One no-target/no-valid-tribe test per helper family.
- Full `TrinketSystemTests` after the batch, because several shared triggers are touched.

## Batch 2: Periodic Generators And Turn-End Rewards

Count: 24 items, complete.

Why this is second: most of these are "get a card now, then repeat on a schedule" or simple end-turn generation. They mainly need consistent turn counters, hand-limit behavior, and copy metadata.

Shared work:

- Add a reusable scheduled grant helper keyed by Trinket card id.
- Support every-N-turn counters in `AdvancedMechanicState.Counters`.
- Add generated-copy helpers that always set new `InstanceId`, `PoolSource.Copy`, and `PoolCopiesHeld = 0`.
- Ensure full-hand behavior is deterministic and logged.

Items:

| Card ID | Slot | Name | Shared implementation path |
| --- | --- | --- | --- |
| `BG35_MagicItem_842` | Lesser | Egg of the Endtimes Portrait | Get Egg now, repeat every two turns. |
| `BG35_MagicItem_848t` | Greater | Egg of the Endtimes Portrait | Get Golden Egg now, hatch next turn. |
| `BG30_MagicItem_916` | Greater | Essence of Dreams | Grant Dreamer's Embrace copies now and each turn. |
| `BG35_MagicItem_840` | Lesser | Chromatic Tear | Lesser version of Chromadrake generation. |
| `BG30_MagicItem_942` | Greater | Mecha-Jaraxxus Sticker | Repeating random Magnetic Mecha-Demons. |
| `BG35_MagicItem_712` | Greater | Privateer Portrait | Proud Privateer plus repeating Bounties. |
| `BG35_MagicItem_890` | Lesser | Sunken Anchor | Repeating random Bounties. |
| `BG35_MagicItem_309` | Lesser | Errgl Sticker | Repeating Mama/Papa Mrrglton grant. |
| `BG32_MagicItem_950` | Lesser | Gritty Portrait | Repeating Gritty Headhunter and contract card. |
| `BG35_MagicItem_434` | Lesser | Jewelry Box | Repeating special Blood Gem generation. |
| `BG35_MagicItem_305` | Lesser | Conch Portrait | Repeating Cloning Conch every two turns. |
| `BG35_MagicItem_817` | Lesser | Lens Case | Repeating Duplicating Lens every two turns. |
| `BG30_MagicItem_425` | Lesser | Azeroth Model Globe | Every two turns: Gold plus Tier 6 Discover. |
| `BG32_MagicItem_951` | Lesser | Gold Pendant | Repeating random friendly Tier 4-or-below Golden. |
| `BG30_MagicItem_435` | Lesser | Goldenizer Supply | Every three turns: Goldenizer. |
| `BG32_MagicItem_817` | Lesser | Rendle Sticker | Steal highest-Tier Tavern card, repeat end-turn. |
| `BG30_MagicItem_419` | Greater | Exquisite Dishware | End-turn one random minion of each controlled type. |
| `BG32_MagicItem_925` | Greater | Hackerfin Portrait | Get Hackerfin and trigger Hackerfin Battlecries end-turn. |
| `BG35_MagicItem_753` | Greater | Murky Sticker | End-turn left-most buffs improved by Battlecry count. |
| `BG32_MagicItem_890` | Lesser | Cliffdiver Sticker | End-turn left-most buff improved by Battlecry count. |
| `BG32_MagicItem_832` | Lesser | Windfall Portrait | End-turn Windfall Tornado with sold-minion scaling. |
| `BG32_MagicItem_832t` | Greater | Windfall Portrait | Greater Windfall Tornado with sold-minion scaling. |
| `BG32_MagicItem_894` | Lesser | Blessing Portrait | Repeating Natural Blessing and shop compatibility. |
| `BG30_MagicItem_711` | Lesser | Marine Signet | After every four played minions, grant/improve Tavern Spell. |

Suggested done test shape:

- Test schedule counters across multiple turns.
- Test hand full behavior for at least one grant helper.
- Test current and next-turn generated card metadata.
- Test scaling counters for Murky, Cliffdiver, and Windfall separately.

## Batch 3: Shop, Refresh, Buy, Cost, And Economy

Count: 21 items, complete.

Why this is third: these effects should share shop refresh, cost override, free refresh, spend Gold, health-cost, and buy-trigger plumbing. Doing them together avoids several competing cost helpers.

Shared work:

- Centralize shop cost overrides for Gold cost, Health cost, and fixed-cost cards.
- Add refresh-result decorators that can inject or transform shop cards after a refresh.
- Track per-turn and per-game cost counters in one place.
- Add one common "Tavern always has N cards" helper.
- Keep shop auras removable/recalculable.

Items:

| Card ID | Slot | Name | Shared implementation path |
| --- | --- | --- | --- |
| `BG35_MagicItem_743` | Greater | Electrode Attractor | Magnetic Mechs cost 2, extra Magnetic on refresh. |
| `BG32_MagicItem_366` | Greater | Guiding Candle | First two refreshes each turn contain Tier 6 cards. |
| `BG35_MagicItem_862` | Greater | Upstart Embers | Refresh doubles highest-Health Tavern minion stats. |
| `BG35_MagicItem_930` | Greater | Warband Whistle | Free refresh containing plain copies of warband minions. |
| `BG32_MagicItem_806` | Lesser | Battlecruiser Portrait | Add Battlecruiser and upgrades on refresh. |
| `BG35_MagicItem_152` | Lesser | Demonic Tapestry | After four refreshes, highest-Tier Tavern minion costs Health. |
| `BG32_MagicItem_891` | Lesser | Finley's Helmet | Refresh buffs Tavern Murlocs and grants Bonus Keyword. |
| `BG30_MagicItem_423` | Lesser | Innkeeper's Stein | Extra higher-Tier minion on refresh. |
| `BG30_MagicItem_991` | Greater | Felbat Portrait | Get Felbat; Tavern always has seven cards. |
| `BG30_MagicItem_541` | Greater | Nether Pendant | Tavern aura improves from out-of-combat hero damage. |
| `BG30_MagicItem_841` | Lesser | Glowing Gauntlet | Tavern minions +3/+3; Tavern always has seven cards. |
| `BG32_MagicItem_821` | Lesser | Pilgrimp Sticker | One Demon each turn costs Health instead of Gold. |
| `BG32_MagicItem_822` | Lesser | Bazaar Sticker | One Tavern Spell each turn costs Health instead of Gold. |
| `BG35_MagicItem_750` | Greater | Magicfin Sticker | After buying Tavern Spell, make taught Murloc. |
| `BG30_MagicItem_701` | Greater | The Eye of Sargeras | Every fourth bought card costs Health instead of Gold. |
| `BG32_MagicItem_957` | Lesser | Grifter Portrait | Get Doubloon Grifter; first Pirate each turn is free. |
| `BG32_MagicItem_230` | Greater | Extravagant Scale | After spending 20 Gold, double board Attack twice per game. |
| `BG30_MagicItem_999` | Greater | Fancy Spellbook | After spending 7 Gold, cast Shiny Ring. |
| `BG32_MagicItem_232` | Greater | Shark Cannon | After spending 10 Gold, buff Pirates and improve. |
| `BG32_MagicItem_205` | Greater | Maw Caster Portrait | Destroy-outside-combat economy reward. |
| `BG35_MagicItem_820` | Greater | Safety Patch | Gain Gold plus Ice Block proxy decision. |

Suggested done test shape:

- Cost display and purchase validation must agree.
- Test free/Health-cost reset on `NextTurn`.
- Test frozen shop interactions for refresh decorators.
- Test no duplicate aura stacking after repeated refreshes.

## Batch 4: Discover, Copy, Spellcraft, And Generated Hand Cards

Count: 22 items, complete.

Why this is fourth: these items need robust `PendingChoice`, generated card metadata, hand-copy limits, and Spellcraft/Tavern Spell integration. Several of them can share one discover/copy foundation.

Shared work:

- Add a queue for multiple pending choices, or a safe "defer if choice already exists" policy.
- Add helpers for Discover with stat overrides and hand locks.
- Add generic "copy next N bought minions" state.
- Add Spellcraft generated-card helpers and cleanup.
- Add generated card tests for `CardKind`, tier, tags, source, and lock counters.

Items:

| Card ID | Slot | Name | Shared implementation path |
| --- | --- | --- | --- |
| `BG30_MagicItem_709` | Lesser | Electromagnetic Device | Discover Magnetic Mech; magnetized target buff. |
| `BG30_MagicItem_709t` | Greater | Electromagnetic Device | Discover two Magnetic Mechs; magnetized target buff. |
| `BG32_MagicItem_362` | Lesser | Innkeeper's Hearth | Discover current-Tier minion, set stats 12/12. |
| `BG32_MagicItem_362t` | Greater | Innkeeper's Hearth | Discover two Tier 6 minions, set stats 20/20. |
| `BG35_MagicItem_821` | Lesser | Kaleidoscope | Discover locked Tier 7 minion. |
| `BG35_MagicItem_821t` | Greater | Kaleidoscope | Discover locked Golden Tier 7 minion. |
| `BG35_MagicItem_733` | Greater | Jailer Sticker | Spellcraft destroys Undead to get two random Undead. |
| `BG35_MagicItem_306` | Lesser | Jailer Sticker | Spellcraft destroys Undead to get one random Undead. |
| `BG30_MagicItem_429` | Lesser | Demonblood Gourd | Spellcraft target consumes random Tavern minion. |
| `BG32_MagicItem_902` | Greater | Statue of Hir'eek | After two Tavern minions are consumed, get Tavern Spell. |
| `BG30_MagicItem_828` | Greater | Shaker Portrait | Zesty Shaker extra spell copy behavior. |
| `BG35_MagicItem_931` | Lesser | Transcribing Typewriter | Copy next two bought minions. |
| `BG35_MagicItem_931t` | Greater | Transcribing Typewriter | Copy next four bought minions. |
| `BG32_MagicItem_807` | Greater | Curator Sticker | Golden Mishmash and Venomous Amalgam generation. |
| `BG32_MagicItem_350` | Lesser | Splinter of Aurum | Once at 15 Gold, get random Golden Tier 5 minion. |
| `BG32_MagicItem_304` | Lesser | Horn of Summoning | Get six different Tier 1 minions. |
| `BG35_MagicItem_815` | Lesser | Magician's Top Hat | Get two minions each from Tiers 1, 2, and 3. |
| `BG32_MagicItem_400` | Lesser | Shrine of Evolution | Transform board into random Tier 4 minions. |
| `BG35_MagicItem_922` | Lesser | Tide Raiser Portrait | Copy combat spell casts, limited per combat. |
| `BG32_MagicItem_361` | Lesser | Portable Factory | Discover typed Tier 4 minion and repeat copy. |
| `BG32_MagicItem_361t` | Greater | Portable Factory | Discover typed Tier 5 minion and repeat copy. |
| `BG30_MagicItem_434` | Lesser | Replica Cathedral | First spell each turn casts an extra time. |

Suggested done test shape:

- Test `PendingChoice` collision behavior.
- Test locked hand cards decrement over turns.
- Test hand limit and duplicate `InstanceId` safety.
- Test Spellcraft generation, use, and cleanup.

## Batch 5: Combat Start, Combat Events, And Deathrattles

Count: 19 items, 7 Lesser, 12 Greater.

Why this is fifth: this batch should be done when the team is ready to touch `CombatEngine`. It needs event logging, replay safety, and clear separation between combat-clone state and permanent tavern state.

Shared work:

- Add reusable combat flags for "start-of-combat effects trigger extra".
- Add combat event hooks for friendly attacks, summons, Deathrattles triggered, minion damage taken, and overflow summons.
- Add post-combat reward plumbing for permanent write-back where official text requires it.
- Append any new `CombatEventType` values at the enum tail.

Items:

| Card ID | Slot | Name | Shared implementation path |
| --- | --- | --- | --- |
| `BG30_MagicItem_952` | Greater | Jarred Frostling | Start of Combat grants Deathrattle to Elementals. |
| `BG35_MagicItem_714` | Greater | Powder Keg | Start of Combat grants Sky Pirate Deathrattle to Pirates. |
| `BG30_MagicItem_918` | Greater | Promo Portrait | Get Promo-Drake; first Start of Combat effect repeats. |
| `BG35_MagicItem_740` | Greater | Sky Golem Portrait | Start of Combat grants board Deathrattle buff. |
| `BG32_MagicItem_365` | Greater | Valdrakken Wind Chimes | Start of Combat effects trigger extra. |
| `BG30_MagicItem_411` | Lesser | Hoggy Bank | Quilboar Deathrattle grants Blood Gems. |
| `BG30_MagicItem_407` | Lesser | Ship in a Bottle | Summon and get random Pirate; immediate attack. |
| `BG30_MagicItem_864` | Greater | Gilnean Thorned Rose | Avenge 3 permanent board buff plus self-damage. |
| `BG30_MagicItem_546` | Greater | Jar o' Gems | After two friendly attacks, play Blood Gems on Quilboar. |
| `BG30_MagicItem_438t` | Greater | Mug of the Sire | Overflow summon converts into board Attack buff. |
| `BG35_MagicItem_431t` | Greater | Thornspike Pauldron | Deathrattle triggers improve Blood Gems until next combat. |
| `BG30_MagicItem_427` | Lesser | Tiger Carving | Friendly damage taken buffs another friendly minion. |
| `BG30_MagicItem_427t` | Greater | Tiger Carving | Friendly damage taken buffs another friendly minion. |
| `BG30_MagicItem_978` | Lesser | Blingtron's Sunglasses | Combat Mech summon gives friendly Mech Divine Shield. |
| `BG35_MagicItem_430` | Lesser | Scrapsmith Portrait | Friendly Taunt death plays permanent Blood Gem. |
| `BG30_MagicItem_917` | Lesser | Rusty Trident | Start of Combat gives Naga Spellcraft Deathrattle. |
| `BG30_MagicItem_981` | Lesser | The Eye of Dalaran | Typeless friendly death grants Tavern Spell. |
| `BG30_MagicItem_923` | Greater | Elementium Chest | Pirate attack counter grants Gold next turn. |
| `BG35_MagicItem_742` | Greater | Accord-o-Tron Portrait | End-turn magnetize Accord-o-Tron to edge Mechs. |

Suggested done test shape:

- Combat replay and combat log assertions for new event types.
- Tests for combat-only cleanup after combat.
- Tests for permanent write-back only when source board instance can be resolved.
- Safety tests for full board, no valid target, and generated tokens.

## Batch 6: Tribe Portraits And Specified-Minion Rewrites

Count: 24 items, 13 Lesser, 11 Greater.

Why this is sixth: these are individually flavored, but most follow the same pattern: add a known minion, then modify that minion's existing Battlecry, Deathrattle, consume, Blood Gem, or tribe behavior.

Shared work:

- Prefer `AddMinionByCardIdToHand`.
- Use proxy fallback only when the local minion catalog lacks the card.
- Add a table mapping portrait Trinket ids to minion ids and modified behavior.
- Avoid display-name checks; use card ids, tags, keywords, and counted tribes.
- Add one "specified portrait add-to-hand" parameterized test table.

Items:

| Card ID | Slot | Name | Shared implementation path |
| --- | --- | --- | --- |
| `BG30_MagicItem_921` | Greater | Flagbearer Portrait | Sky Pirate Flagbearer plus Sky Pirate Attack aura. |
| `BG35_MagicItem_156` | Greater | Flaming Portrait | Flaming Enforcer trigger affects neighbors. |
| `BG32_MagicItem_204` | Greater | Kel'Thuzad Portrait | Get Kel'Thuzad; outside-combat destroy buffs board. |
| `BG30_MagicItem_943` | Greater | Surveyor Portrait | Hot-Air Surveyor plus hand Blood Gem bonus. |
| `BG35_MagicItem_433` | Greater | Vinespeaker Portrait | Vinespeaker improves Blood Gem Health too. |
| `BG30_MagicItem_869` | Lesser | Felblood Portrait | Felbloods give Attack and Health. |
| `BG32_MagicItem_830` | Lesser | Felemental Portrait | Felemental grants extra stats. |
| `BG32_MagicItem_953` | Lesser | Goldgrubber Portrait | Get Goldgrubber and Aureate Laureate. |
| `BG30_MagicItem_777` | Lesser | Goose Portrait | Silver Fledgling summons progress toward Lucky Egg. |
| `BG32_MagicItem_824` | Lesser | Implicator Portrait | Demons consume highest-Health Tavern minion. |
| `BG32_MagicItem_820` | Lesser | Impulsive Portrait | Impulsive Trickster Deathrattle targets adjacent minions. |
| `BG30_MagicItem_803` | Lesser | Kaboom Bot Portrait | Kaboom Bot Deathrattle extra damage. |
| `BG32_MagicItem_803` | Lesser | Macaw Portrait | Macaw also triggers left-most Battlecry. |
| `BG30_MagicItem_868` | Lesser | Rewinder Portrait | Soul Rewinder also gains Attack. |
| `BG32_MagicItem_887` | Lesser | Shadowy Elixir | Armor plus Demon-play hero damage. |
| `BG30_MagicItem_825` | Lesser | Smuggler Portrait | Whelp Smuggler becomes 12/12 Dragon. |
| `BG32_MagicItem_416` | Lesser | War Drum | One Battlecry each turn triggers two extra times. |
| `BG35_MagicItem_154` | Greater | Ur'zul Sticker | Demon play causes another Demon to consume Tavern minion. |
| `BG35_MagicItem_713` | Greater | Trusty Crowbar | When getting a Pirate, buff left-most minion. |
| `BG32_MagicItem_282` | Greater | Turbocharged Drill | Get five different Magnetic Mechs of any Tier. |
| `BG30_MagicItem_843t` | Greater | Horde Keychain | Tier 3-or-lower minions stat aura. |
| `BG32_MagicItem_804` | Lesser | Selfless Portrait | Selfless Hero also triggers on Battlecry. |
| `BG32_MagicItem_367` | Greater | Ghastly Sticker | End-of-turn effects trigger extra. |
| `BG35_MagicItem_752` | Greater | Young Murk-Eye Sticker | End-turn trigger edge minions' Battlecries. |

Suggested done test shape:

- Parameterized equip tests for every portrait with a known minion.
- One behavior test per modified minion family.
- Proxy tests must assert `ProxySafe`; exact tests must assert real catalog card id.

## Batch 7: High-Risk Or Foundation-First Items

Count: 21 items, 14 Lesser, 7 Greater.

Why this is last: these require systems that are incomplete, ambiguous official placeholders, or broad cross-system rewrites. Some may remain `DebugOnly` even after a proxy exists.

Shared work:

- Resolve official placeholders such as `92` and `'0'` using HearthstoneJSON before writing runtime logic.
- Add Buddy/Hero Power support before Buddy Trinkets become `Offerable`.
- Add Timewarp, Darkmoon Prize, Yogg wheel, and Ice Block/Secret proxy decisions before marking those exact.
- Add Trinket replacement/upgrade flow before Mystery Cube, Souvenir Stand, and Trip Vouchers enter normal pools.

Items:

| Card ID | Slot | Name | Blocker or required foundation |
| --- | --- | --- | --- |
| `BG30_MagicItem_804` | Lesser | Ancient Wishbone | Hero Power double-trigger framework. |
| `BG35_MagicItem_803` | Lesser | Maxwell Sticker | Buddy of Hero Power. |
| `BG35_MagicItem_803t` | Greater | Maxwell Sticker | Golden Buddy of Hero Power. |
| `BG35_MagicItem_801` | Lesser | Sous Chef Sticker | Extra Hero Power use plus hero-power trigger reward. |
| `BG30_MagicItem_703` | Lesser | Mystery Cube | Trinket replacement choice flow. |
| `BG35_MagicItem_816` | Lesser | Orb of the Unknown | Random Lesser Trinket grant/replacement policy. |
| `BG35_MagicItem_816t` | Greater | Orb of the Unknown | Random Greater Trinket grant plus Gold. |
| `BG30_MagicItem_994` | Lesser | Yogg-Tastic Pastry | Wheel of Yogg-Saron system. |
| `BG30_MagicItem_426` | Lesser | Colorful Compass | Official `92` placeholder. |
| `BG30_MagicItem_426t` | Greater | Colorful Compass | Official `92` placeholder. |
| `BG32_MagicItem_901` | Greater | Gold-plated Compass | Official `92` placeholder plus Golden-next-buy. |
| `BG30_MagicItem_973` | Lesser | Minion Bait | Official `92` placeholder. |
| `BG35_MagicItem_823` | Lesser | Timeworn Candelabra | Minor Timewarp card pool. |
| `BG35_MagicItem_823t` | Greater | Timeworn Candelabra | Major Timewarp card pool. |
| `BG32_MagicItem_906` | Greater | Artanis Sticker | Text contains child-card placeholder `'0'`. |
| `BG30_MagicItem_930` | Lesser | Burgling Claw | Last opponent warband history. |
| `BG32_MagicItem_300` | Lesser | Putricide Sticker | Custom Undead crafting. |
| `BG30_MagicItem_707` | Lesser | Tickatus Sticker | Darkmoon Prize system. |
| `BG35_MagicItem_812` | Greater | Corrupted Tome | Triple Prize replacement system. |
| `BG30_MagicItem_888` | Lesser | Souvenir Stand | Transform into bought Greater Trinket. |
| `BG30_MagicItem_891` | Lesser | Trip Vouchers | Delayed Greater Trinket purchase and replacement. |

Note: keep both Maxwell entries in Batch 7 until Buddy/Hero Power support is ready. After that foundation exists, they can be implemented with the same add-to-hand test style used by Batch 6 portraits.

## Coverage Check

The original classification covered all 158 unimplemented Trinkets after Dragonwing. Batches 1-5 are now complete, leaving 45 Trinkets in Batches 6-7:

| Batch | Status / Count |
| --- | --- |
| Batch 1 | 27, complete |
| Batch 2 | 24, complete |
| Batch 3 | 21, complete |
| Batch 4 | 22, complete |
| Batch 5 | 19, complete |
| Batch 6 | 24 |
| Batch 7 | 21 |
| Remaining total | 45 |

Validation command used to check the current source of truth:

```powershell
$json = Get-Content -LiteralPath 'Assets\LearnHearthstone\Resources\Data\battlegroundsTrinkets.json' -Raw | ConvertFrom-Json
$pending = @($json.trinkets | Where-Object { $_.implementationStatus -ne 'Implemented' })
$pending.Count
$pending | Group-Object effectFamily | Sort-Object Count -Descending | Select-Object Count,Name
```

## Recommended Next Move

Start Batch 6. Batch 5 is complete and the next set should implement tribe portraits and specified-minion rewrites:

- Prefer `AddMinionByCardIdToHand` for portrait minions.
- Use proxy fallback only when the local minion catalog lacks the card.
- Add a table mapping portrait Trinket ids to minion ids and modified behavior.
- Avoid display-name checks; use card ids, tags, keywords, and counted tribes.

Run a full `TrinketSystemTests` pass after Batch 6 shared portrait hooks because several entries alter existing minion behavior.
