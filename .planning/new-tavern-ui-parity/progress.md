# New Tavern UI Parity Progress

## 2026-06-11

- Started implementation pass using `planning-with-files` and `frontend-design`.
- Confirmed `rg` is not usable in this environment; switched to PowerShell file search.
- Added separate main-hub routing so the legacy tavern trainer remains on `酒馆训练器` and the new realistic trainer opens from `真实酒馆 UI`.
- Expanded the realistic trainer with old-trainer parity drawers for info, opponent editing, battle/scenario/replay, logs, and debug card acquisition.
- Added direct `RealisticCombatButton` to the realistic action panel so the new UI exposes the old UI's one-click `SimulateCombat` path.
- Added EditMode coverage for the new entry split, drawer parity panels, battle drawer, quick combat button, and drag command mappings.
- Ran Unity EditMode via `LearnHearthstone.Editor.BatchEditModeTestRunner.RunEditMode`; result: `234/234` passed, `0` failed.
