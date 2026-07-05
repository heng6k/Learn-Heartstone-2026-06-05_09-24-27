# Timewarped Tavern Implementation Audit

Date: 2026-07-05

This audit supersedes the earlier incomplete-implementation snapshot. The P0/P1 repair pass
closed the suspected current-pool minion gaps and the second-Hero-Power integration blocker.

## Closed Items

| Area | Current result | Evidence |
| --- | --- | --- |
| Current-pool minion effects | Closed for the 9 suspected missing cards. | `BG34_Giant_201`, `BG34_Giant_039`, `BG34_Giant_598`, `BG34_Giant_678`, `BG34_Giant_309`, `BG34_Giant_333`, `BG34_Giant_323`, `BG34_Giant_676`, `BG34_Giant_599` are tagged `implementation_status:implemented` and covered in `MatchServiceTests`. |
| Whirl-O-Tron Deathrattle copying | Closed. | Copied Deathrattles now resolve through the generic Deathrattle effect path. |
| Lava Lurker copied Spellcraft permanence | Closed. | Copied Spellcraft effects are converted to permanent copied effects; focused test verifies two copied Reef Riff casts persist across turn cleanup. |
| Scout Tier 7 reward source | Closed. | Rewards draw from current-pool definitions instead of the max-Tavern-tier filtered available-minion path. |
| Second/extra Hero Power usage | Closed. | `UseHeroPower(... heroPowerCardId)` plus per-power turn budgets cover primary, extra, replacement, and Master Nguyen temporary Hero Powers; Unity buttons disable exhausted unlocked powers. |
| Big Winner / Darkmoon Prize coverage | Closed for the previously missing Tier 3 branches. | Training Session, Top Shelf, Repeat Customer, All That Glitters, and Mindflayer Goggles are covered. |

## Boundary Classification

The following items remain visible in audits, but they are not open P0/P1 runtime
implementation defects.

Canonical status is tracked in `timewarped-tavern-remaining-completion-status.md`.

| Boundary id | Area | Classification | Current behavior | Why it stays open | Action that closes it |
| --- | --- | --- | --- | --- | --- |
| `TW-BDY-001` | Timewarped Evolving Tavern | Closed data/catalog proxy | `BG34_Treasure_900` now adds official local Darkmoon Prize card `BGS_Treasures_006`; the legacy `TIMEWARPED_EVOLVING_TAVERN_SPELL` branch is compatibility code only. | Closed because local Darkmoon Prize data now contains Evolving Tavern and focused tests assert the official id is generated. | Reopen only if the official local data or focused generation path regresses. |
| `TW-BDY-002` | Historical/extra Timewarped pool | Product switch | Default current Timewarp offers exclude historical/launch-extra cards. Historical candidates are appended only through `UseHistoricalTimewarpedPool` / `TimewarpedPoolVersion`. | This is a deliberate pool boundary, not missing default implementation. | Make historical/extra mode an approved first-class mode, then add explicit UI/config/test coverage for it. |
| `TW-BDY-003` | Default full EditMode stability | Test infrastructure | Focused Timewarped suites pass; the latest default 8-shard EditMode route also passed. | This remains a test-infrastructure classification for future regressions, not a Timewarped card-effect defect. | Closed for the current route; rerun `Tools/run-editmode-bisect.ps1` only when a future broad run hangs or fails. |

## Boundary Handling Rules

- Keep generated proxies explicit when official card data is absent.
- Close generated-proxy boundaries when official local card data exists and the generated id is
  covered by focused tests.
- Keep historical/extra cards out of default current-pool offers unless a product decision changes
  the default pool.
- Treat broad EditMode hangs as infrastructure until a focused repro identifies gameplay code.
- Do not downgrade completed P0/P1 card effects because of these boundaries.

## Non-Issues Confirmed

- `BG34_Giant_007` Timewarped Annoy-o-Tron and `BG34_Giant_012` Timewarped Cyclone remain static keyword bodies; they do not require effect branches.
- Historical/extra cards are not expected to appear in the default current pool.
- Remaining `darkmoon_prize_proxy` search hits are negative assertions or old non-authoritative context; Big Winner no longer relies on the Bounty proxy path.
- Evolving Tavern no longer has an open proxy boundary: the current generated id is
  `BGS_Treasures_006`, and the old local id is asserted absent from the focused path.

## Validation

- `Logs/TimewarpedLavaSingle.xml`: 1/1 passed.
- `Logs/TimewarpedP0P1MatchService.xml`: `MatchServiceTests` 166/166 passed.
- `Logs/TimewarpedReaudit.xml`: 123/123 passed for fresh Timewarped/DarkmoonPrize-focused
  `MatchServiceTests` validation.
- `MatchServiceTests.TimewarpedRepeatNonMinion_EvolvingTavernAddsOfficialPrizeAndRepeatsAtTurnStart`
  covers official `BGS_Treasures_006` generation for `BG34_Treasure_900`.
- `Logs/TimewarpedBoundaryFocused.xml`: 2/2 passed for shard-5 stabilization fixes.
- `Logs/TimewarpedBoundaryShard6Focused.xml`: 2/2 passed for shard-6 stabilization fixes.
- `Logs/EditModeBisectSummary.txt`: all 8 default EditMode shards passed.
- Static JSON parse: `timewarpedTavernCards.json` contains 158 cards and the 9 target current-pool minions are the implemented Timewarped card ids in this pass.
