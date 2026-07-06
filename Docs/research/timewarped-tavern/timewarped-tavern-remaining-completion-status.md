# Timewarped Tavern Boundary Classification Status

Date: 2026-07-05

This is the canonical status file for the remaining Timewarped Tavern boundaries after the
P0/P1 repair pass. These entries are tracked as boundaries, not as open P0/P1 runtime
card-effect implementation gaps.

## Classification Summary

| Boundary id | Name | Class | State | Runtime impact | Owner lane |
| --- | --- | --- | --- | --- | --- |
| `TW-BDY-001` | Timewarped Evolving Tavern generated spell | Data/catalog proxy | Closed | Uses official local Darkmoon Prize data | Data/content |
| `TW-BDY-002` | Historical/extra Timewarped pool | Runtime-complete product switch | Runtime closed; exposed in start-game version control | Historical modes are playable when selected; default current pool unchanged | Product/config |
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
- The start-game version-control strip exposes this setup as `Current`, `FirestoneAll`, and
  `Launch` pool choices. Selecting a non-current choice sets
  `UseHistoricalTimewarpedPool = true`.
- The 27 historical/extra minions that previously had no runtime handlers now resolve through
  `MatchService` recruit/turn paths or `CombatEngine` combat paths.

**Why this is not an open runtime gap:**
- The historical/extra runtime effects are implemented and covered by focused tests.
- Excluding historical/extra cards from the default current pool is intentional.
- The switch-gated path exists and has a first-class UI/config entry.

**Closed by:**
- `MatchService` handlers for historical/extra recruit, turn, sell, spell, stats-gained, and
  Magnetic effects.
- `CombatEngine` handlers for historical/extra combat, Rally, Deathrattle, Start of Combat,
  Divine Shield loss, Venomous/Immune, and permanent reward effects.
- `timewarpedTavernCards.json` `implementation_status:implemented` tags on the 27 historical/extra
  target cards.
- `UnityTavernTribeSelectionView` version-control button
  `UnityTimewarpedPoolVersionButton`.
- `MatchSetupOptions.TimewarpedPoolVersion` and `UseHistoricalTimewarpedPool` wiring from the
  start-game UI.
- Runtime candidate-pool tests for default current exclusion and historical inclusion.
- UI setup test
  `UnityTavernTrainerViewTests.TribeSelectionView_TimewarpedPoolVersionButtonPassesSetup`.
- `Logs/TimewarpedHistoricalImplementationTests.xml`: 11/11 passed.
- `Logs/TimewarpedHistoricalPoolBoundaryTests.xml`: 4/4 passed.

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
- `Logs/TimewarpedHistoricalImplementationTests.xml`: 11/11 passed for focused historical/extra
  runtime coverage.
- `Logs/TimewarpedHistoricalPoolBoundaryTests.xml`: 4/4 passed for catalog/default exclusion and
  opt-in historical inclusion.
- `timewarpedTavernCards.json`: contains 158 cards; the 9 P0 target current-pool minions and
  27 historical/extra target minions carry `implementation_status:implemented`.
- `Tools/run-editmode-bisect.ps1`: parser check previously passed and the route is now verified
  against the default EditMode suite.
