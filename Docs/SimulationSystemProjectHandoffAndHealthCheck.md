# Simulation System Project Handoff And Health Check

## Purpose

This document is the handoff map for the current Learn Hearthstone simulation system work. It explains:

- which dirty worktree files belong to which feature topic;
- where each subsystem lives in the project;
- which runtime areas each subsystem touches;
- what has already been validated by tests;
- what remains to implement in the next Timewarped high-impact content slice.

The intended reader is a future developer or designer joining the project without this conversation context.

## Current Scope

This pass covers three practical steps:

1. Dirty worktree scope separation.
2. Full Unity EditMode health check.
3. First two Timewarped high-impact single-card precision slices.

Large UI/Unity interaction redesign is intentionally deferred. The current priority is functional correctness and maintainable simulation coverage.

## Dirty Worktree Scope Map

Status: audited. No files were reverted or staged.

| Topic | Files | Project role | Runtime/test relationship | Current status |
| --- | --- | --- | --- | --- |
| design-validation | `Assets/LearnHearthstone/Runtime/Application/Services/DesignValidationScenarioCatalog.cs`, `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatResultExplainer.cs`, `Assets/LearnHearthstone/Runtime/Domain/Models/CombatAnalysisModels.cs`, `Assets/LearnHearthstone/Tests/EditMode/DesignValidationToolingTests.cs`, and the design-validation portions of `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs` | Provides designer-ready scenarios, combat result explanation, and a report model for "what actually mattered in this simulated fight." | `MatchService` exposes scenario loading and stores `LastCombatExplanation`; `CombatResultExplainer` consumes `CombatOutput`; `DesignValidationToolingTests` validate catalog, explanation, coverage rows, and next-turn ordering. | Belongs together as one coherent design-validation/tooling change. |
| content-pool-expansion | `Assets/LearnHearthstone/Tests/EditMode/QuestTrinketInteractionTests.cs`, `Docs/ContentPoolExpansionCoverageMatrix.md`, `Docs/ContentPoolExpansionImplementationPlan.md`, and the content-pool rows inside `Assets/LearnHearthstone/Runtime/Application/Services/MechanicCoverageReportService.cs` | Documents and tests high-risk content coverage: Quest/Trinket interactions, Timewarped precision, Darkmoon precision, and anomaly deferral. | `QuestTrinketInteractionTests` exercise `MatchService` quest/trinket setup plus `CombatEngine` combat reward paths. The coverage matrix records implementation status and deferred areas. | Belongs together as the current content-pool expansion slice. |
| content-pool-expansion: Timewarped precision slices | `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs`, `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`, `Assets/LearnHearthstone/Tests/EditMode/TimewarpedHistoricalImplementationTests.cs`, `Docs/ContentPoolExpansionCoverageMatrix.md` | Adds two high-impact Timewarped single-card precision fixes: Timewarped Deathswarmer and Timewarped Kil'rek. | Deathswarmer now applies immediate in-combat Undead attack and still queues the permanent `ImproveUndeadAttack` reward. Kil'rek now queues `AddRandomDemonToHand` from `CombatEngine`, while `MatchService` no longer double-applies the old player-side implicit Kil'rek deathrattle handler. | New in this pass. Verified through Unity MCP socket on port 6400 and included in the post-Kil'rek full EditMode green run. |
| bridge between the two | `Assets/LearnHearthstone/Runtime/Application/Services/MechanicCoverageReportService.cs`, `Assets/LearnHearthstone/Runtime/Domain/Models/CombatAnalysisModels.cs` | Converts implementation coverage into designer-readable status rows. | Design-validation UI/tooling can show the report, while the rows reference content-pool systems and test confidence. | Should be committed with design-validation if only one grouping is allowed, but mention content-pool dependency in the commit message. |
| prior opponent-hand/side-state docs index | `Docs/DocumentationIndex.md` | Adds `OpponentHandAndSideStateConfigurationPlan.md` to the documentation index. | Documentation navigation only; no runtime dependency. | Separate small documentation/index change. |
| tracked runtime bridge | `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs` | Adds public accessors for design-validation scenarios, mechanic coverage report, scenario load, and last combat explanation; clears explanation state when combat/scenarios reset. | Connects new design-validation service classes to the existing match lifecycle. | Not Timewarped-specific. Keep with design-validation tooling. |

### Dirty Scope Notes

- The current dirty worktree is mixed but separable. The safest commit grouping later is:
  1. design-validation tooling and explanation;
  2. content-pool expansion coverage/tests;
  3. documentation index update.
- `MechanicCoverageReportService` is a bridge. If a future maintainer wants extremely strict commits, split the report rows by topic; otherwise keep it with design-validation tooling because that is the code path exposing the report.
- `Docs/ContentPoolExpansionImplementationPlan.md` displayed as mojibake in the current shell. Do not regenerate it from terminal output; use the file as-is or reopen it in an editor that preserves its encoding.

## System Location Map

Status: in progress. This section will be expanded while auditing code.

| System | Primary files | What it owns | Closely related systems |
| --- | --- | --- | --- |
| Match service | `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs` | Recruit-phase commands, next-turn flow, mechanic choices, combat preparation, combat reward application | Combat engine, catalogs, Unity trainer UI |
| Combat engine | `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs` | Combat snapshot simulation, deathrattles, summons, avenge, rally, side-specific combat rewards | Match service, combat explanation, design scenarios |
| Tavern and combat models | `Assets/LearnHearthstone/Runtime/Domain/Models/TavernMatchModels.cs` and related model files | Shared state for player/opponent taverns, boards, hands, advanced mechanics, combat results | Match service, UI, tests |
| Timewarped content | `Assets/LearnHearthstone/Resources/Data/timewarpedTavernCards.json`, `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`, `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs`, existing Timewarped tests | Historical cards, Timewarped purchase/combat/hero-power behavior | Match service, combat engine, tests, coverage matrix |
| Quest/Trinket interactions | `Assets/LearnHearthstone/Tests/EditMode/QuestTrinketInteractionTests.cs`, quest/trinket prep paths in `MatchService`, repeat/summon/reward paths in `CombatEngine` | Cross-system trigger ordering and reward isolation | Match service, combat engine, focused interaction tests |
| Design validation tooling | `DesignValidationScenarioCatalog.cs`, `CombatResultExplainer.cs`, `CombatAnalysisModels.cs`, `DesignValidationToolingTests.cs`, `MechanicCoverageReportService.cs` | Reproducible designer scenarios and result explanations | Scenario catalog, combat result explainer, coverage report |

## Test Health Check

Status: passed from Unity NUnit XML. The active Unity socket on port `6400` ran two full EditMode health checks in this pass: one after Deathswarmer, then another after Kil'rek.

| Test suite | Result | What it validates | Notes |
| --- | --- | --- | --- |
| Full EditMode after Deathswarmer | Passed: 1034 total, 1033 passed, 0 failed, 1 skipped | Whole EditMode surface after the Deathswarmer precision slice | Unity MCP job `9ced227d8c21443c8472c24ff7cf06b3` ran through port `6400`. XML archived at `.planning/full-health-dirty-boundary-timewarped-second/EditMode-full-post-deathswarmer-2026-07-07.xml`. |
| Full EditMode after Kil'rek | Passed: 1035 total, 1034 passed, 0 failed, 1 skipped | Whole EditMode surface after the second Timewarped precision slice | The attempted focused run was not narrowed by `run_tests` filter and executed a full EditMode pass instead. XML archived at `.planning/full-health-dirty-boundary-timewarped-second/EditMode-full-post-kilrek-2026-07-07.xml`. This is the current health baseline. |
| `QuestTrinketInteractionTests` | Previously passed 5/5 | Quest/Trinket start-of-combat stacking, deathrattle repeat stacking, avenge sharing, summon modifiers, opponent reward isolation | Previous session result; will remain referenced as focused coverage |
| `QuestSystemTests` | Previously passed 58/58 | Quest core behavior and reward state | Previous session adjacent coverage |
| `TrinketSystemTests` | Previously passed 220/220 | Trinket runtime behavior and broad edge cases | Previous session adjacent coverage |
| `TimewarpedHistoricalImplementationTests` | Passed 13/13 in the post-Kil'rek full EditMode run | Timewarped historical cards hosted in `MatchService` and `CombatEngine`, including Deathswarmer immediate combat buff and Kil'rek demon reward routing | Unity `get_tests` confirmed 13 matching tests after refresh/compile. |

### Full EditMode Runner Notes

The full-suite checks passed, but the MCP polling channel is still noisy during long runs. Treat MCP `Command TCS timed out` messages as runner pressure until NUnit XML or a failed test name says otherwise.

Observed evidence:

- Unity MCP accepted full EditMode runs on port `6400`.
- During long runs, direct `get_test_job` and other socket commands can time out while the Unity process keeps consuming CPU.
- `Editor.log` can show repeated `MCP-FOR-UNITY: Command TCS timed out` warnings before the run finishes.
- The main Unity process can temporarily show `Responding=False`.
- Unity later restores test-run throttling and saves `TestResults.xml`.
- Post-Deathswarmer XML: `Passed`, `total=1034`, `passed=1033`, `failed=0`, `skipped=1`, `duration=479.0931072`.
- Post-Kil'rek XML: `Passed`, `total=1035`, `passed=1034`, `failed=0`, `skipped=1`, `duration=466.0117126`.

Recommended validation fallback:

1. Prefer reading Unity's saved XML if MCP polling loses the final job object.
2. Use `phase=idle` and `compilation.is_compiling=false` from `get_editor_state` as readiness signals for this bridge version; it did not expose `ready_for_tools`.
3. For future repeated health checks, run one giant job only when no one needs the Editor interactively for several minutes.
4. For faster iteration, run EditMode by major suite group and only do the giant full run before a submit/merge.
5. Only treat a result as a code failure when NUnit returns failed test names or an exception stack.

### Full EditMode Coverage By Area

| Area | Test suites | Result | What this tells a maintainer |
| --- | --- | --- | --- |
| Anomaly systems | `AnomalySystemTests` 66/66 | Passed | Current anomaly catalog behavior, known implemented effects, pool filtering, and anomaly-specific reward flows are stable under EditMode. |
| Combat core | `CombatMechanicTests` 2/2, `DomainEngineTests` 26/26, `MatchServiceBattleTestTests` 3/3 | Passed | Combat math, combat command entry points, and domain engine invariants are currently healthy. |
| Combat replay and opponent editor | `CombatReplayAndOpponentEditorTests` 14/14, `OpponentCustomizationTests` 13/13 | Passed | Opponent board/hand configuration, combat replay, and side customization remain functional. |
| Darkmoon prizes | `DarkmoonPrizeSystemTests` 12/12 | Passed | Direct, persistent, and ordering-sensitive Darkmoon prize paths covered by the dedicated tests still work. |
| Design-validation tooling | `DesignValidationToolingTests` 6/6 | Passed | The new scenario catalog, combat explanation, mechanic coverage report, and full next-turn order test are compiling and passing. |
| Catalogs and effect infrastructure | `EffectCatalogTests` 3/3, `EffectDispatcherTests` 2/2, `HeroCatalogTests` 7/7, `HeroEffectImplementationRegistryTests` 11/11, `MinionCatalogTests` 7/7, `SpellCatalogTests` 1/1, `TavernSpellEngineTests` 4/4, `TribeAvailabilityRulesTests` 4/4 | Passed | Data loading, effect registration, tavern spell execution, and tribe availability rules are stable. |
| Hero and buddy effects | `HeroPowerBuddyEffectTests` 147/147, `HeroSetupAndUnmaskedIdentityTests` 6/6 | Passed | Hero-power, buddy, setup, swap, and history-sensitive hero tests are stable after the next-turn/combat changes. |
| Match service broad behavior | `MatchServiceTests` 169/169, `MatchServiceMechanicTests` 8/8, `MatchServiceDebugCardTests` 7/7, `MatchServiceSpellTests` 11/11 | Passed | Recruit flow, debug card handling, mechanics, spells, Timewarped paths hosted in `MatchService`, and broad match-state transitions are healthy. |
| Player-directed advanced mechanics | `PlayerDirectedAdvancedMechanicSelectionTests` 5/5 | Passed | Player choice routing for advanced mechanics remains stable. |
| Quest and Trinket systems | `QuestSystemTests` 58/58, `QuestTrinketInteractionTests` 5/5, `TrinketSystemTests` 220/220 | Passed | Standalone Quest/Trinket behavior and cross-system interactions are healthy. |
| UI/EditMode views | `RealisticTavernTrainerViewTests` 10/10, `TavernTrainerViewTests` 20/20, `UnityTavernTrainerViewTests` 72/72 | Passed | Current Unity UI/EditMode surface is stable, including existing trainer view behavior. |
| Robustness and stress | `RobustnessEdgeTests` 4/5 with 1 explicit skipped, `StressTests` 6/6 | Passed | Core state limits and stress scenarios pass; the skipped case is an explicit 30-minute soak, not a failure. |
| Tier acceptance and minion mechanics | Tier one through seven acceptance suites and tier-three mechanic suites | Passed | Minion catalog acceptance and representative tier mechanics remain healthy. |
| Timewarped historical content | `TimewarpedHistoricalImplementationTests` 13/13 plus related `MatchServiceTests` baseline | Passed | Existing high-impact Timewarped historical card paths passed in the full EditMode run. This pass adds focused coverage for Deathswarmer's immediate in-combat Undead attack buff and Kil'rek's CombatEngine-owned demon reward routing. |

## Timewarped High-Impact Slice

Status: first two cards selected, implemented, and verified through the active Unity MCP socket.

Selection rules:

- Prefer a real uncovered or partial gap over duplicating existing handlers.
- Pick a card that changes combat result, hand pressure, historical stats, or economy-to-board conversion.
- Add or update a focused EditMode test for every implemented card behavior.
- Update this handoff document and `Docs/ContentPoolExpansionCoverageMatrix.md` with the final status.

### First Slice: Timewarped Deathswarmer

Card: `BG34_Giant_081` Timewarped Deathswarmer.

Source text recorded in `Docs/research/timewarped-tavern/timewarped-tavern-research.md`: "Whenever this takes damage, your Undead have +1 Attack this game (wherever they are)."

Existing behavior before this slice:

- `CombatEngine.ResolveTimewarpedDamageTrigger` detected Deathswarmer taking damage.
- It queued `CombatRewardType.ImproveUndeadAttack`.
- `MatchService.ApplyCombatRewards` later applied that reward to `State.Player.Tavern.UndeadAttackBonus` and shop growth.
- Result: future recruit/combat state improved, but the current combat board did not immediately reflect the attack gain.

Implemented behavior in this slice:

- The Deathswarmer damage branch now computes `amount = source.Golden ? 2 : 1`.
- It immediately buffs all living friendly Undead in `owner.Board` through `BuffMinion`.
- It uses `HasCountedTribe(minion, Tribe.Undead)`, so All-tribe minions follow the same counted-tribe rules as other combat effects.
- It keeps the existing permanent `ImproveUndeadAttack` reward so the "this game" state remains persistent after combat.
- It adds a `DamageTriggered` combat log entry for the current fight.

Test added:

- `Combat_TimewarpedDeathswarmerBuffsUndeadImmediatelyAndQueuesPermanentReward`
- Location: `Assets/LearnHearthstone/Tests/EditMode/TimewarpedHistoricalImplementationTests.cs`
- Scenario: player has a Taunt Deathswarmer plus another Undead; opponent has more minions, so opponent attacks first; safety limit is one attack step.
- Assertions: the allied Undead's final combat attack rises from 2 to 3, and `PlayerRewards` still contains `ImproveUndeadAttack` from Deathswarmer with amount 1.

Safety boundary:

- Do not move player-side historical stat setup into `CombatEngine.ApplySideCombatHistoryBonuses`. That guard remains opponent-only because the player side already receives historical board/hand/shop growth through service-layer recruit preparation. Deathswarmer is different: it is a live in-combat trigger, so immediate combat mutation belongs in `CombatEngine`.

Validation status for this slice:

- `git diff --check`: passed, with only existing CRLF normalization warnings on touched C# files.
- Initial Unity command-line focused test attempt: blocked because the project was open in another Unity instance.
- Correct Unity MCP route: connect directly to Unity socket port `6400`, read `WELCOME UNITY-MCP 1 FRAMING=1`, then send 8-byte big-endian length-prefixed JSON commands such as `{"type":"run_tests","params":...}`.
- Unity refresh/compile was required before the new test appeared in the Unity Test Runner list. Before refresh, `get_tests` returned 11 Timewarped historical tests and no Deathswarmer test; after `refresh_unity` with compile request, it returned 12 tests.
- New focused test result: `Combat_TimewarpedDeathswarmerBuffsUndeadImmediatelyAndQueuesPermanentReward` passed 1/1.
- Suite result: `TimewarpedHistoricalImplementationTests` passed 12/12.

### Second Slice: Timewarped Kil'rek

Card: `BG34_Giant_584` Timewarped Kil'rek.

Source text recorded in `Docs/research/timewarped-tavern/timewarped-tavern-research.md`: "Taunt. Deathrattle: Get a random Demon."

Existing behavior before this slice:

- `MatchService.ResolveTimewarpedDeathrattleTrigger` had a player-side branch that added a random Demon to the player's hand when a combat reward later reported that Kil'rek's deathrattle had triggered.
- `CombatEngine.ResolveDeathrattleEffect` already emitted the generic `FriendlyDeathrattleTriggered` reward for any deathrattle minion.
- Result: player-side recruit state could receive the Demon through service-layer after-combat handling, but the combat result itself did not expose a specific `AddRandomDemonToHand` reward for Kil'rek. Opponent-side Kil'rek rewards also had no explicit combat reward surface.

Implemented behavior in this slice:

- `CombatEngine` now owns Kil'rek's specific deathrattle reward.
- `ResolveDeathrattleSummons` queues `CombatRewardType.AddRandomDemonToHand` when `BG34_Giant_584` resolves its deathrattle.
- The reward carries Kil'rek's `SourceCardId` and `SourceInstanceId`, so replay/explanation and future side-specific consumers can identify the exact source.
- Golden Kil'rek queues 2 random Demons per resolved deathrattle, following the existing deathrattle repeat loop style used by nearby reward-producing deathrattles.
- `MatchService.ResolveTimewarpedDeathrattleTrigger` no longer has the Kil'rek branch, preventing the player side from receiving one Demon from the explicit combat reward and another from the old implicit service hook.

Test added:

- `Combat_TimewarpedKilrekQueuesDemonRewardOnceThroughCombatEngine`
- Location: `Assets/LearnHearthstone/Tests/EditMode/TimewarpedHistoricalImplementationTests.cs`
- Scenario: player board contains a Taunt/Deathrattle Timewarped Kil'rek, opponent has board-count advantage and kills it on the first attack.
- Assertions: `State.LastResult.PlayerRewards` contains exactly the explicit `AddRandomDemonToHand` reward from Kil'rek, and the player's hand contains exactly one Demon after `MatchService.ApplyCombatRewards`.

Safety boundary:

- This slice intentionally avoids adding a second service-layer consumer. The engine emits the concrete reward; the service only consumes the reward into player recruit state.
- Opponent rewards remain visible in `CombatOutput.OpponentRewards` but do not mutate the player's recruit state, matching the existing opponent reward isolation rule.

Validation status for this slice:

- Unity `get_tests` confirmed `TimewarpedHistoricalImplementationTests` now has 13 tests and includes `Combat_TimewarpedKilrekQueuesDemonRewardOnceThroughCombatEngine`.
- The attempted focused `run_tests` filter was not narrowed by the Unity MCP bridge and executed a full EditMode pass.
- Post-Kil'rek full EditMode result: `Passed`, 1035 total, 1034 passed, 0 failed, 1 skipped, duration 466.0117126 seconds.
- XML archived at `.planning/full-health-dirty-boundary-timewarped-second/EditMode-full-post-kilrek-2026-07-07.xml`.
- `git diff --check` passed with only existing CRLF normalization warnings on touched C# files.

## Current Handoff Notes

- The simulation core already has strong coverage for opponent hand, side-specific variables, complete next-turn combat flow, Quest/Trinket interaction tests, Darkmoon first batches, and Timewarped broad paths.
- The main risk is not a missing framework, but mixed dirty changes and unverified edge gaps.
- Future work should keep each content slice small, with one runtime entry point, one focused test, and one documentation update per meaningful mechanic.
- Deathswarmer and Kil'rek are now both covered by focused tests inside the Timewarped historical suite and by the post-Kil'rek full EditMode run.
