# Battlegrounds Anomaly Current Issues and Recommendations

Date: 2026-07-06

This document records the current state of the Battlegrounds Anomaly system after the second-pass
audit that followed the Timewarped Tavern completion work.

## Current State Summary

- Current catalog file: `Assets/LearnHearthstone/Resources/Data/battlegroundsAnomalies.json`.
- Catalog count is consistent: `count = 111`, actual anomaly entries = `111`.
- Current default pool: 28 `CurrentHsReplay` anomalies, all `Implemented`.
- Historical/all-known pool: 83 additional `AllKnown` anomalies, all `Unsupported`, tagged
  `historical` and `data_only`, and gated by `RequiresOfficialDataReview`.
- Every anomaly entry has a positive `dbfId`.
- Default random anomaly selection only offers implemented/currently available entries.
- Start-game UI supports anomaly enable/disable, random selection, explicit implemented anomaly
  selection, and anomaly pool-version opt-in.
- `AllKnown` is an explicit opt-in data/product boundary, not a claim that every historical
  anomaly is playable.

## Source of Truth

- `Assets/LearnHearthstone/Resources/Data/battlegroundsAnomalies.json`
- `Assets/LearnHearthstone/Runtime/Adapters/Data/AnomalyCatalogLoader.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Data/AnomalyCatalog.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Models/AnomalyModels.cs`
- `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
- `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernTribeSelectionView.cs`
- `Assets/LearnHearthstone/Tests/EditMode/AnomalySystemTests.cs`
- `Assets/LearnHearthstone/Tests/EditMode/DarkmoonPrizeSystemTests.cs`
- `Assets/LearnHearthstone/Tests/EditMode/UnityTavernTrainerViewTests.cs`

## ANOM-001 Through ANOM-009 Status

| ID | Current status | Evidence |
| --- | --- | --- |
| ANOM-001 | Fixed | `SinglePlayerChoice` exists in the enum, loader mapping, and catalog tests; unknown families now fail fast. |
| ANOM-002 | Fixed | All 111 anomaly entries have positive `dbfId`; tests require positive ids for implemented current entries. |
| ANOM-003 | Fixed | The old unimplemented plan is now a historical archive and no longer describes Darkmoon proxy state as current. |
| ANOM-004 | Fixed as data/product boundary | Catalog imports 111 anomalies; 28 current entries are implemented, 83 historical entries are unsupported/data-only. |
| ANOM-005 | Fixed | UI exposes an anomaly pool-version control and passes `AnomalyPoolVersion` into setup. |
| ANOM-006 | Fixed as local policy | Audience's Choice and The Yogg-iseum carry `single_player_adaptation` metadata and trainer-policy notes. |
| ANOM-007 | Fixed | Scout's Honor creates formal `BG24_715` Patient Scout data, not an `anomaly_proxy`. |
| ANOM-008 | Fixed | BuddyPool and Cosmic Duality candidate implementation-status reporting is exposed and tested. |
| ANOM-009 | Fixed | Direct Darkmoon Prize tests live in `DarkmoonPrizeSystemTests`; anomaly-trigger tests remain in `AnomalySystemTests`. |

## Current Findings

### ANOM-010: Explicit Setup Selection Must Respect Eligibility

Severity: P2 service-boundary bug.

Second-pass audit found that UI selection and random selection correctly filter unsupported
historical anomalies, but direct callers of `MatchSetupOptions.SelectedAnomalyCardId` could pass an
unsupported historical card id and activate an enabled anomaly state with no runtime handler.

Recommended fix:

1. Reuse the same anomaly eligibility check for explicit selected anomalies.
2. Add a regression test using an unsupported `AllKnown` anomaly.

Status: fixed in the current working tree.

## Non-Problems

- The 28 current-pool anomalies are not missing runtime handlers.
- Historical/all-known entries being unsupported is intentional until productized, not a default
  gameplay bug.
- Darkmoon Prize is no longer an anomaly blocker; the direct prize system has its own focused tests.
- Audience's Choice and The Yogg-iseum are accepted single-player trainer adaptations, not official
  shared-lobby simulations.

## Validation Snapshot

- `Logs/AnomalySystemAuditTests.xml`: 66/66 passed after ANOM-010.
- `TestResults-DarkmoonPrizeSystemTests.xml`: 12/12 passed.
- `TestResults-UnityTavernTrainerViewTests.xml`: 72/72 passed.
- `git diff --check` on anomaly-related files reported no whitespace errors.
