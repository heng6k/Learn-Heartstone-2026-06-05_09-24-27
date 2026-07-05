# Timewarped Tavern Boundary Classification Status

Date: 2026-07-05

This is the canonical status file for the remaining Timewarped Tavern boundaries after the
P0/P1 repair pass. These entries are tracked as boundaries, not as open P0/P1 runtime
card-effect implementation gaps.

## Classification Summary

| Boundary id | Name | Class | State | Runtime impact | Owner lane |
| --- | --- | --- | --- | --- | --- |
| `TW-BDY-001` | Timewarped Evolving Tavern generated spell | Data/catalog proxy | Closed | Uses official local Darkmoon Prize data | Data/content |
| `TW-BDY-002` | Historical/extra Timewarped pool | Product switch | Classified and gated | None for default current pool | Product/config |
| `TW-BDY-003` | Default full EditMode hang route | Test infrastructure | Verified through 8-shard route | None proven for Timewarped cards | Test infrastructure |

## `TW-BDY-001`: Evolving Tavern Generated Spell

**Classification:** Data/catalog proxy.

**Current behavior:**
- `BG34_Treasure_900` Timewarped Evolving Tavern grants `BGS_Treasures_006`.
- `BGS_Treasures_006` is present in `darkmoonPrizes.json` as Evolving Tavern with
  `implementationStatus: Implemented`.
- The generated card carries Darkmoon Prize metadata and refreshes Tavern minion slots into
  random minions from one Tavern Tier higher.
- The legacy `TIMEWARPED_EVOLVING_TAVERN_SPELL` handler remains only as compatibility code; the
  current Timewarped path is covered by tests that assert it is not generated.

**Why this is closed:**
- The card effect is implemented and covered by focused Timewarped tests.
- The official local generated-card identity is now `BGS_Treasures_006`, sourced from the
  Darkmoon Prize catalog.

**Closed by:**
- `darkmoonPrizes.json` entry `BGS_Treasures_006` / Evolving Tavern.
- `MatchService` constant `TimewarpedEvolvingTavernSpellCardId = "BGS_Treasures_006"`.
- `MatchServiceTests.TimewarpedRepeatNonMinion_EvolvingTavernAddsOfficialPrizeAndRepeatsAtTurnStart`.

**Do not:**
- Reopen this as a data proxy unless `BGS_Treasures_006` is removed from local Darkmoon Prize
  data or the focused Evolving Tavern test regresses.
- Reintroduce `TIMEWARPED_EVOLVING_TAVERN_SPELL` as the current generated card.

## `TW-BDY-002`: Historical/Extra Timewarped Pool

**Classification:** Product switch.

**Current behavior:**
- Default Timewarp offers remain current-pool only.
- Historical/launch-extra candidates are appended only when enabled through
  `UseHistoricalTimewarpedPool` / `TimewarpedPoolVersion`.

**Why this is not a P0/P1 runtime gap:**
- Excluding historical/extra cards from the default current pool is intentional.
- The switch-gated path exists; the open question is whether that optional mode should become a
  first-class product mode.

**Close this boundary when:**
- The project explicitly accepts the current switch-gated behavior as final, or
- The project productizes the historical/extra mode with UI/config exposure, status labels, and
  focused candidate-pool tests.

**Do not:**
- Silently mix historical/extra cards into default Timewarp offers.
- Treat default exclusion as missing card implementation.

## `TW-BDY-003`: Default Full EditMode Hang Route

**Classification:** Test infrastructure.

**Current behavior:**
- Focused Timewarped validation passes.
- `Tools/run-editmode-bisect.ps1` is the verified route for broad default EditMode validation.
- The latest 8-shard run completed without hangs or failed shards.
- Stress and Marathon suites remain outside default validation.

**Why this is not a P0/P1 runtime gap:**
- A broad full-suite hang does not identify a specific Timewarped card, test, or runtime path.
- The correct next step is isolation, not changing card behavior preemptively.

**Close this boundary when:**
- Closed for the current default EditMode route by `Logs/EditModeBisectSummary.txt`, which
  reports all 8 shards passed on 2026-07-05.
- Reopen only if a later default route run hangs/fails, then isolate the failing shard/test with
  the same bisect tool before filing a gameplay defect.

**Do not:**
- Rerun the known hanging full-suite path blindly.
- Downgrade completed Timewarped card effects because broad validation has not been isolated.

## Decision Rules

- Missing official card data becomes a `Data/catalog proxy` boundary.
- Optional card-pool behavior becomes a `Product switch` boundary.
- Broad test-runner instability becomes a `Test infrastructure` boundary until a focused repro
  proves gameplay code is at fault.
- A boundary can move back into implementation work only with sourced data, an approved product
  decision, or a concrete failing focused test.

## Validation Evidence

- `Logs/TimewarpedLavaSingle.xml`: 1/1 passed.
- `Logs/TimewarpedP0P1MatchService.xml`: `MatchServiceTests` 166/166 passed, including the
  official `BGS_Treasures_006` Evolving Tavern path.
- `Logs/TimewarpedReaudit.xml`: 123/123 passed for fresh Timewarped/DarkmoonPrize-focused
  `MatchServiceTests` validation after the re-audit.
- `Logs/TimewarpedBoundaryFocused.xml`: 2/2 passed for the shard-5 blockers exposed during
  stabilization.
- `Logs/TimewarpedBoundaryShard6Focused.xml`: 2/2 passed for Coilfang Elite and Ini Stormcoil
  after aligning tests/runtime with current data.
- `Logs/EditModeBisectSummary.txt`: all 8 default EditMode shards passed; final shard completed
  at 2026-07-05 14:04 local time.
- `timewarpedTavernCards.json`: parses with 158 cards, and exactly the 9 P0 target current-pool
  minions carry `implementation_status:implemented`.
- `Tools/run-editmode-bisect.ps1`: parser check previously passed and the route is now verified
  against the default EditMode suite.
