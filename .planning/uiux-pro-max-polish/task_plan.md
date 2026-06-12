# UIUX Pro Max Polish Plan

## Goal
Use the PSYOP-Z/UIUXProMax guidance and local UI/UX Pro Max skill rules to improve the existing old Unity UI so it is more polished, readable, and convenient while preserving current gameplay behavior.

## Phases

1. [complete] Read UIUXProMax guidance and map the existing Unity UI architecture.
2. [complete] Run a Confidence Check and choose a tightly scoped UI improvement target.
3. [complete] Implement visual and usability polish using existing project UI helpers.
4. [complete] Update focused tests and run Unity verification.
5. [complete] Summarize changes, verification, and residual risks.

## Constraints

- Preserve existing dirty worktree changes and do not revert user work.
- Keep the single-player tavern scope; no Duos features.
- Prefer existing Unity UI patterns, helpers, and tests.
- Avoid introducing new UI frameworks or asset dependencies.
- Keep old UI behavior intact except for visual clarity and interaction ergonomics.

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| `rg` access denied | Tried fast file listing/search | Use `git ls-files`, `Get-ChildItem`, and `Select-String` instead. |
| GitHub clone reset | Tried cloning `PSYOP-Z/UIUXProMax` over HTTPS | Switch to raw/zip or alternate GitHub endpoints for instructions. |

## Verification

- Unity EditMode passed: `239/239`, `0` failed.
- Result file: `TestResults-UIUXProMaxPolish.xml`.
- Log file: `Unity-UIUXProMaxPolish.log`.
