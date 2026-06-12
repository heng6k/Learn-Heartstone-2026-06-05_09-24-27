# Acquisition Modal And Card Art Plan

## Goal
Change the legacy tavern trainer acquisition tab into a centered official-like card browser modal, and correct realistic UI card art cropping/alignment.

## Phases

1. [complete] Inspect reference images and current legacy/new UI code.
2. [complete] Identify root cause of crooked/new UI card art cropping.
3. [complete] Implement legacy acquisition modal with spell/minion toggle, tier filters, and type filters.
4. [complete] Fix card art rendering alignment in the realistic card view.
5. [complete] Add focused EditMode tests and run Unity verification.

## Constraints

- Keep existing old UI tavern/battle/opponent behavior intact except the acquisition entry flow.
- No Duo, teammate, or pass features.
- Reuse existing data loaders, `UiFactory`, and `GameCommand` paths.
- Avoid destructive git actions; preserve existing dirty worktree.

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| Unity command returned before results were written | Read process and log state after command returned | Waited for the Unity process to exit, then read fresh XML. |

## Verification

- Unity EditMode passed: `237/237`, `0` failed.
- Result file: `TestResults-AcquisitionModalCardArt.xml`.
- Log file: `Unity-AcquisitionModalCardArt.log`.
