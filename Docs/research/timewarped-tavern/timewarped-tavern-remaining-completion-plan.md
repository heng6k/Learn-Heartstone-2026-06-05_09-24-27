# Timewarped Tavern Remaining Completion Plan

Date: 2026-07-05

This document is the current completion record for Timewarped Tavern P0/P1. The older
productization snapshot was removed because its open items have either been completed or
reclassified as explicit data/product boundaries.

## Current Status

- P0 current-pool suspected missing minion effects are implemented and covered:
  `BG34_Giant_201`, `BG34_Giant_039`, `BG34_Giant_598`, `BG34_Giant_678`,
  `BG34_Giant_309`, `BG34_Giant_333`, `BG34_Giant_323`, `BG34_Giant_676`,
  and `BG34_Giant_599`.
- `timewarpedTavernCards.json` marks exactly those 9 current-pool minions as
  `implementation_status:implemented`; the remaining current-pool cards keep their existing
  data/status tags.
- Timewarped Whirl-O-Tron resolves copied Deathrattles through the generic Deathrattle
  effect path.
- Timewarped Lava Lurker copied Spellcraft casts are converted to permanent copied effects.
- Timewarped Scout Tier 7 rewards use current-pool definitions instead of the max-Tavern-tier
  filtered shop pool.
- Big Winner uses the shared Tier 3 Darkmoon Prize path, and focused coverage now includes
  Training Session, Top Shelf, Repeat Customer, All That Glitters, and Mindflayer Goggles.
- Second/extra Hero Power command entry, independent per-power budgets, temporary/replacement
  Hero Power scoping, and Unity button disabled state are covered by the generic Hero Power
  use-limit implementation.

## Boundary Register

These are the remaining or recently closed Timewarped boundaries from the P0/P1 pass. They are
not classified as P0/P1 runtime card-effect gaps.

Canonical status is tracked in `timewarped-tavern-remaining-completion-status.md`.

| Boundary id | Boundary | Current classification | Why this is not a P0/P1 runtime gap | Release condition | Do not do |
| --- | --- | --- | --- | --- | --- |
| `TW-BDY-001` | `BG34_Treasure_900` Timewarped Evolving Tavern | Closed data/catalog proxy | The effect now uses official local Darkmoon Prize card `BGS_Treasures_006`; the legacy `TIMEWARPED_EVOLVING_TAVERN_SPELL` handler is compatibility code, not the current generated path. | Closed by the `darkmoonPrizes.json` `BGS_Treasures_006` entry and focused Evolving Tavern test coverage. Reopen only if that local official data or focused path regresses. | Do not re-document this as an open proxy while `BGS_Treasures_006` remains local and tested. |
| `TW-BDY-002` | Historical/extra Timewarped pool | Product switch | Historical/launch-extra cards are outside the default current pool by policy. The runtime already has `UseHistoricalTimewarpedPool` / `TimewarpedPoolVersion` gates, so default omission is expected. | Productize this mode only if the project decides historical/extra pools should be a first-class gameplay option; then add status labels, tests, and UI/config exposure for that mode. | Do not silently mix historical/extra cards into default current Timewarp offers. |
| `TW-BDY-003` | Default full EditMode hang route | Test infrastructure | Focused Timewarped validation passes, and the default 8-shard EditMode route now completes without hangs or failed shards. | Closed for the current route by `Logs/EditModeBisectSummary.txt`; keep `Tools/run-editmode-bisect.ps1` as the regression isolation path if a future broad run fails. | Do not treat future broad failures as Timewarped card defects until the bisect route isolates a concrete gameplay test. |

## Boundary Decision Rules

- If official card/catalog data lands, close the data proxy boundary and keep a focused regression
  test proving the official generated id is used.
- If the problem is an optional pool policy, keep it behind an explicit switch and document the
  default exclusion.
- If the problem is broad test execution stability, isolate it with the bisect tool before tying it
  to gameplay implementation.
- A boundary can move back into runtime work only when there is a concrete failing focused test,
  missing sourced card data to wire, or an approved product decision changing the default pool.

## Validation

- `Logs/TimewarpedLavaSingle.xml`: 1/1 passed.
- `Logs/TimewarpedP0P1MatchService.xml`: `MatchServiceTests` 166/166 passed.
- `Logs/TimewarpedReaudit.xml`: 123/123 passed for fresh Timewarped/DarkmoonPrize-focused
  `MatchServiceTests` validation.
- `MatchServiceTests.TimewarpedRepeatNonMinion_EvolvingTavernAddsOfficialPrizeAndRepeatsAtTurnStart`
  covers `BG34_Treasure_900` generating `BGS_Treasures_006` and not the legacy local id.
- `Logs/TimewarpedBoundaryFocused.xml`: 2/2 passed for the shard-5 stabilization blockers.
- `Logs/TimewarpedBoundaryShard6Focused.xml`: 2/2 passed for Coilfang Elite and Ini Stormcoil
  after the shard-6 fix pass.
- `Logs/EditModeBisectSummary.txt`: all 8 default EditMode shards passed on the verified route.
- `timewarpedTavernCards.json` parses successfully with 158 cards.
- Focused static audit confirms the 9 target card ids have runtime/test hits and
  `implementation_status:implemented` tags.

## Follow-Up Ownership

- Product follow-up: decide whether historical/extra-pool gameplay should be exposed beyond the
  existing switch-gated path.
- Test-infrastructure follow-up: maintain the verified bisect route for future regressions; no
  current default EditMode shard remains open from this pass.
