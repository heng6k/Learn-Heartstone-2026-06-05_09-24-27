# New Tavern UI Parity Plan

## Goal
Add a separate entry point from the start screen into the new realistic Battlegrounds tavern UI, keep the old UI unchanged, and bring the new UI to functional parity with the original UI while improving the look toward the official tavern experience.

## Phases

1. [complete] Map existing old UI, new UI, scene entry flow, and tests.
2. [complete] Implement isolated entry path for the new UI without changing old UI behavior.
3. [complete] Fill functional parity gaps in the new tavern UI.
4. [complete] Polish realistic tavern visuals and interaction feedback.
5. [complete] Add or update focused tests and run Unity verification.

## Constraints

- Old UI must remain available and behaviorally unchanged.
- No Duo, teammate, or pass-specific features.
- Prefer existing Unity UI patterns and project helpers.
- Keep edits scoped to UI entry, new UI, and tests.

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| `rg` denied by OS | Used `rg --files` | Use PowerShell recursive file discovery instead. |
| PowerShell `Format-Hex -Count` unsupported | Tried to inspect UTF-8 bytes | Re-read files with explicit UTF-8 output; source text was correct. |
| Early XML read showed stale test results | Read test XML before Unity finished | Waited for Unity process to exit, then confirmed fresh `234/234` result. |

## Verification

- Unity EditMode passed: `234/234`, `0` failed.
- Result file: `TestResults-NewRealisticUIParity.xml`.
- Log file: `Unity-NewRealisticUIParity.log`.
