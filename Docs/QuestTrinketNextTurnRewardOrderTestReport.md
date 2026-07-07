# Quest/Trinket Next-Turn Reward Order Test Report

## Purpose

This report records the focused test added to verify that player combat rewards are applied before the next recruit refresh, while opponent rewards remain isolated.

## New Test

- Test: `QuestTrinketInteractionTests.CombatRewardsApplyBeforeNextRecruitRefreshAndStaySideIsolated`
- File: `Assets/LearnHearthstone/Tests/EditMode/QuestTrinketInteractionTests.cs`
- Runtime path covered: `RunCombatTest` -> `ApplyCombatRewards` -> `DebugSkipToNextTurn` -> turn-start shop refresh -> `ApplyPostShopRefreshEffects` -> `ApplyShopGrowth`

## Scenario

- Player has Timewarped Kil'rek, Timewarped Goldrinn, and active Grim Freshener.
- Player tavern has a frozen Beast in the shop before combat.
- Opponent has Coldlight Diver plus a larger attacker.
- Combat kills the player minions and the opponent Coldlight.

## Assertions

- Player rewards add a Demon to hand through Kil'rek.
- Grim Freshener adds one free refresh.
- Timewarped Goldrinn immediately buffs current friendly-owned Beast cards, including the frozen shop Beast.
- Timewarped Goldrinn also records future Beast shop growth.
- `DebugSkipToNextTurn` advances to the next recruit turn without resolving another combat.
- The frozen Beast remains in shop and receives the recorded shop growth during turn-start refresh.
- Opponent Coldlight's Tavern Spell reward stays in `OpponentRewards` and never adds card `104436` to the player's hand.

## Verification

| Run | Result | Job/XML |
| --- | --- | --- |
| Focused new test | Passed: 1 total, 1 passed, 0 failed, 0 skipped | Unity MCP job `20ffc53cc1d14b259de3cef22de40d29` |
| `QuestTrinketInteractionTests` | Passed: 7 total, 7 passed, 0 failed, 0 skipped | Unity MCP job `96873dc0b9d0422891241025733bf934` |
| Full EditMode | Passed: 1038 total, 1037 passed, 0 failed, 1 skipped | Unity MCP job `1fadc89dd20b462a84640f633c438f52`; XML `.planning/quest-trinket-next-turn-reward-order/EditMode-full-post-next-turn-reward-order-2026-07-07.xml` |

## Notes

- No runtime code changed for this test.
- During Unity compile/domain reload, the MCP bridge temporarily disconnected; rerunning/polling after the bridge returned produced valid results.
- The explicit skipped full-suite test is `RobustnessEdgeTests.ThirtyMinuteExtremeCombatAndRecruitSoak_MaintainsBounds`, the existing 30-minute soak.
