# Quest/Trinket Deathrattle Repeat Summon Counter Test Report

## Purpose

This report records the focused tests added to verify that repeated deathrattle-created combat summons queue one summon reward per actual summoned minion, advance player Trinket summon counters exactly once per summon, and keep opponent-side repeated summon rewards isolated from player recruit state.

## New Tests

- Test: `QuestTrinketInteractionTests.RepeatedDeathrattleSummonsTriggerTrinketSummonRewardsOncePerSummonAndStaySideIsolated`
- Test: `QuestTrinketInteractionTests.OpponentRepeatedDeathrattleSummonsDoNotAdvancePlayerSummonCounters`
- File: `Assets/LearnHearthstone/Tests/EditMode/QuestTrinketInteractionTests.cs`
- Runtime path covered: `RunCombatTest` -> `CombatEngine.GetDeathrattleRepeats` -> Manasaber deathrattle summons -> `ResolveFriendlySummonTriggers` -> `QueueFriendlySummonReward` -> `MatchService.ApplyCombatRewards` -> `ApplyTrinketFriendlySummonRewards`

## Scenarios

- Player P0: active Turbulent Tombs (`BG27_Reward_803`) makes player Manasaber (`BG26_800`) deathrattle resolve twice, producing four Beast Cublings.
- Player Trinkets: Goose Portrait (`BG30_MagicItem_777`) starts at 2/3 Beast summons and Wildfeather Duster (`BG35_MagicItem_700`) starts at 5/6 Beast summons.
- Opponent P0b: opponent Manasaber plus opponent Titus Rivendare (`BG25_354`) produces four opponent Cublings through repeated deathrattle, while the player has the same near-threshold summon counters.

## Assertions

- Player Manasaber records `FriendlyDeathrattleTriggered` with amount 2.
- Player Manasaber queues exactly four player `FriendlyMinionSummoned` rewards, each with amount 1, Beast tribe data, and a unique summoned target id present on the final combat board.
- Four player Beast summon rewards complete Goose Portrait twice and Wildfeather Duster once, leaving Goose counter 0, Wildfeather counter 3, and three Beast minions in player hand.
- Opponent repeated Manasaber summons appear only in `OpponentRewards`.
- Opponent repeated summons do not advance player Goose/Wildfeather counters and do not add cards to player hand.

## Verification

| Run | Result | Job/XML |
| --- | --- | --- |
| Focused P0b rerun | Passed: 1 total, 1 passed, 0 failed, 0 skipped | Unity MCP job `7a9c21a24a0343698eb033293bafcefc` |
| Focused P0 + P0b | Passed: 2 total, 2 passed, 0 failed, 0 skipped | Unity MCP job `59642163358244f4b3cf32f8c9578304` |
| `QuestTrinketInteractionTests` | Passed: 10 total, 10 passed, 0 failed, 0 skipped | Unity MCP job `56b894a6a883415290dfd95ba50919eb` |
| Full EditMode | Passed: 1041 total, 1040 passed, 0 failed, 1 skipped | Unity MCP job `6f3c7c3b0dde431f8f08a9a9ae8305b8`; XML `.planning/quest-trinket-deathrattle-repeat-summon-counter/EditMode-full-post-deathrattle-repeat-summon-counter-2026-07-07.xml` |

## Notes

- No runtime code changed for this slice.
- The final tests set test gold before equipping both a Lesser and Greater Trinket, matching adjacent paid-Trinket test setup.
- The test class now uses a small local `HasCountedTribe` helper for its own hand assertions, avoiding a Unity test compile name-resolution issue around direct `BoardTribeAnalyzer` calls in this file.
- The full-run MCP polling connection timed out mid-run, but Unity completed the run and wrote the final NUnit XML.
- The explicit skipped full-suite test is `RobustnessEdgeTests.ThirtyMinuteExtremeCombatAndRecruitSoak_MaintainsBounds`, the existing 30-minute soak.
