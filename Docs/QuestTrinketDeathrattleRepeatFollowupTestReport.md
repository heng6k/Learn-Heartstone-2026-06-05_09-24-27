# Quest/Trinket Deathrattle Repeat Follow-up Test Report

## Purpose

This report records the follow-up tests added after the main repeated-deathrattle summon counter slice. The goal is to lock higher-risk edges around board space, stacked repeat sources, tribe filtering, counter remainders across combats, and replay/frame double-count boundaries.

## New Tests

- Test: `QuestTrinketInteractionTests.RepeatedDeathrattleSummonRewardsRespectBoardSpaceAndOverflow`
- Test: `QuestTrinketInteractionTests.DeathrattleRepeatSourcesStackBeforeSummonCounterRewards`
- Test: `QuestTrinketInteractionTests.NonBeastRepeatedDeathrattleSummonsDoNotAdvanceBeastSummonTrinkets`
- Test: `QuestTrinketInteractionTests.RepeatedDeathrattleSummonCounterRemainderPersistsAcrossCombats`
- Test: `QuestTrinketInteractionTests.RepeatedDeathrattleSummonRewardsDoNotDoubleCountReplayCopies`
- File: `Assets/LearnHearthstone/Tests/EditMode/QuestTrinketInteractionTests.cs`
- Runtime path covered: `RunCombatTest` -> `CombatEngine.GetDeathrattleRepeats` -> token insertion/overflow -> `ResolveFriendlySummonTriggers` -> `QueueFriendlySummonReward` -> `MatchService.ApplyCombatRewards` -> `ApplyTrinketFriendlySummonRewards`

## Scenarios

- Board-space overflow: Turbulent Tombs repeats Manasaber while the player board only has room for two of four attempted Cublings.
- Stacked repeat sources: Turbulent Tombs plus Titus Rivendare make Manasaber resolve three deathrattle passes, producing six Cublings before Goose/Wildfeather counters pay out.
- Non-Beast negative control: Turbulent Tombs repeats Harmless Bonehead, producing four Undead Skeleton summon rewards that must not advance Beast summon Trinkets.
- Cross-combat remainder: Wildfeather Duster stores a four-summon remainder after the first repeated Manasaber combat, survives a debug next-turn transition, then pays exactly once in the next combat.
- Replay double-count guard: repeated Manasaber summons are present in combat rewards and replay frames, but player Trinket state reflects a single reward application.

## Assertions

- Overflowed summon attempts create `SummonOverflowed` replay frames but do not queue `FriendlyMinionSummoned` rewards or advance Goose/Wildfeather counters.
- Stacked repeat sources produce `FriendlyDeathrattleTriggered` amount 3 and six unique Beast summon rewards before player counters are consumed.
- Non-Beast repeated summon rewards carry Undead tribe data and leave Beast summon counters plus player hand unchanged.
- Wildfeather Duster remainder persists across `DebugSkipToNextTurn`, then pays exactly once when the next combat crosses the threshold.
- Replay `MinionSummoned` frames and cloned replay rewards do not double-apply player Trinket counters or hand rewards.

## Verification

| Run | Result | Job/XML |
| --- | --- | --- |
| `QuestTrinketInteractionTests` after refresh | Passed: 15 total, 15 passed, 0 failed, 0 skipped | Unity MCP job `688f95404ea949bfabee43be75307aa3`; XML `.planning/quest-trinket-deathrattle-repeat-followups/EditMode-QuestTrinketInteractionTests-post-followups-2026-07-07.xml` |
| Full EditMode | Passed: 1046 total, 1045 passed, 0 failed, 1 skipped | Unity MCP job `1f2383b30e134352a4258e1127533f62`; XML `.planning/quest-trinket-deathrattle-repeat-followups/EditMode-full-post-deathrattle-repeat-followups-2026-07-07.xml` |

## Notes

- No runtime code changed for this slice.
- The full-run MCP polling connection failed mid-run at 518/1046, but Unity completed and wrote final NUnit XML.
- The explicit skipped full-suite test remains `RobustnessEdgeTests.ThirtyMinuteExtremeCombatAndRecruitSoak_MaintainsBounds`.
- The first exact-method run before refresh matched 0 tests; the class rerun after refresh is the counted focused verification.
