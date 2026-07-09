# Player Choice Blocking Implementation Plan

## Purpose

Player-required choices are blocking decisions. The game must not advance a normal player turn, start combat, or complete an automated player turn while any required choice remains unresolved.

This rule prevents the system from silently choosing for the player when they still need to select a Quest, Trinket, Anomaly, Distortion, Discover option, or any future advanced mechanic option.

## Blocking States

The first implementation treats these states as blockers:

- `TavernState.AdvancedMechanics.PendingChoice != null`
- `TavernState.Discover != null`

`PendingChoice` includes:

- Quest choices, such as Sire Denathrius' opening 2-option choice and normal 3-option Quest offers.
- Trinket choices, including Lesser Trinkets, Greater Trinkets, replacement Trinkets, and delayed Trinket rewards.
- Anomaly choices.
- Distortion choices.
- Any future advanced mechanic represented by `MechanicChoiceRequest`.

`Discover` includes any unresolved player-facing Discover flow with remaining picks.

## Required Behavior

When the player clicks a normal advancement command such as Next Turn, Combat, or a full-turn shortcut:

1. Check whether the player has unresolved required choices.
2. If no blocker exists, continue with the existing command.
3. If a blocker exists, do not advance the turn, do not enter combat, and do not auto-select an option.
4. Return a clear message such as:
   - `请先完成当前任务选择。`
   - `请先完成当前饰品选择。`
   - `请先完成当前畸变选择。`
   - `请先完成当前扭曲选择。`
   - `请先完成当前发现选择。`
5. Leave the existing choice modal visible, or rebuild the UI so the pending choice is visible.

## Allowed Automatic Choices

Automatic or forced selection is only allowed when it is explicit:

- Debug commands.
- Test setup code that directly invokes choose commands.
- A future user-visible button such as Auto Choose or Random Choose.
- Simulation-only flows that are clearly not normal player input.

Normal player advancement commands must never choose on the player's behalf.

Current implementation guards normal `NextTurn` and `SimulateCombat` commands. It intentionally leaves `DebugSkipToNextTurn`, direct choose commands, and combat test tooling as explicit debug/test paths.

## Implementation Steps

1. Add a shared `MatchService` guard that detects unresolved player choices and throws an `InvalidOperationException` with a user-facing message.
2. Call the guard at the start of normal turn/combat advancement commands.
3. Do not call the guard from choose commands such as `ChooseDiscover`, `ChooseMechanicOption`, or player-directed choice commands.
4. Keep existing modal rendering paths. The current UGUI rebuild already shows `Discover` and `PendingChoice` overlays.
5. Add focused EditMode tests for:
   - Next Turn blocked by Discover.
   - Combat/full-turn advancement blocked by Discover, if that command exists.
   - Next Turn blocked by Quest pending choice.
   - Next Turn blocked by Trinket pending choice.
   - Next Turn blocked by Anomaly pending choice, if an existing debug/helper path can create one.

## Acceptance Criteria

- A player cannot advance a normal turn while `Discover` is active.
- A player cannot advance a normal turn while `AdvancedMechanics.PendingChoice` is active.
- Pending Quest, Trinket, Anomaly, and Distortion choices all use the same guard.
- The UI reports a clear error instead of silently advancing.
- Existing explicit choose/debug flows still work.

