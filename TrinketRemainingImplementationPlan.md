# Trinket Remaining Implementation Plan

## Completion Status

As of 2026-07-05, the trinket catalog has no remaining implementation gap.

| Metric | Count |
| --- | ---: |
| Total | 330 |
| Implemented | 330 |
| FrameworkFirst | 0 |
| DebugOnly | 0 |
| Offerable | 329 |
| HiddenEffectOnly | 1 |
| Implemented entries with empty effectIds | 0 |

The final seven formerly incomplete trinkets are implemented and offerable:

| CardId | Name | Effect Id | Proxy Level |
| --- | --- | --- | --- |
| `BG35_MagicItem_803` | Maxwell Sticker | `maxwell_sticker` | Exact |
| `BG35_MagicItem_803t` | Maxwell Sticker | `maxwell_sticker_greater` | Exact |
| `BG32_MagicItem_300` | Putricide Sticker | `putricide_sticker` | Exact |
| `BG35_MagicItem_801` | Sous Chef Sticker | `sous_chef_sticker` | Exact |
| `BG30_MagicItem_804` | Ancient Wishbone | `ancient_wishbone` | ProxySafe |
| `BG35_MagicItem_812` | Corrupted Tome | `corrupted_tome` | Exact |
| `BG32_MagicItem_906` | Artanis Sticker | `artanis_sticker` | Exact |

## Current Boundaries

- Sous Chef Sticker is exact: the runtime has a generic per-turn Hero Power use budget, and Sous Chef Sticker adds one extra use while granting 1 Gold after each successful Hero Power use.
- Ancient Wishbone remains ProxySafe by design: it repeats the Hero Power trigger once with recursion protection and refunds the repeated trigger's base Hero Power cost, so one manual use triggers twice without consuming a second use budget.
- Corrupted Tome grants Triple Prize on equip and replaces ordinary Triple Rewards with Tier 3 Darkmoon Prize Discover.
- Artanis Sticker resolves `relatedDbfId=119960` to Mothership (`BG31_HERO_802pt7`) and adds that Protoss reward copy on equip.
- The former `92` display placeholders have been resolved to readable minion text in the trinket catalog.
- Specified-minion portrait entries marked Exact now have notes that match their exact catalog status.

## Validation Evidence

- `Logs/TrinketFinalSeven.xml`: focused final-seven tests passed 7/7.
- `Logs/TrinketSystemFull.xml`: full `TrinketSystemTests` passed 218/218.
- `Logs/HeroPowerUseLimitFocused.xml`: Hero Power use-limit focused coverage passed 9/9.
- `git diff --check` passes for the touched trinket files with only the repository's existing CRLF normalization warnings.

## Follow-Up Policy

No remaining trinket needs implementation work. Future trinket work should be treated as polish or data fidelity unless a new audit finds one of these regressions:

- a non-Implemented entry entering the default offer pool;
- an Implemented entry with empty `effectIds`;
- unresolved placeholder display text;
- a ProxySafe implementation incorrectly labeled Exact;
- a failing focused trinket regression test.
