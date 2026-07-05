# Dirty Worktree Triage - 2026-07-05

This document records how the visible dirty worktree was classified before the checkpoint
cleanup. No file was reverted or discarded during this triage.

## Handling Decision

The visible dirty changes are completed implementation, test, data, and documentation batches
from the current development thread. They should be preserved together in a checkpoint commit so
the working tree returns to a clean state and the completed batches remain reviewable.

Ignored validation outputs under `Logs/` remain untracked by design and are not part of the
checkpoint.

## Group A - Trinket Completion And Smart Offers

Purpose: close the remaining Trinket catalog/runtime gaps, clean catalog metadata, implement
Hero Power use-limit boundaries needed by Sous Chef Sticker and Ancient Wishbone, and add the
local Trinket smart-offer rule.

Files:

| File | Role |
| --- | --- |
| `Assets/LearnHearthstone/Resources/Data/battlegroundsTrinkets.json` | Final Trinket status, effect ids, metadata, and catalog hygiene. |
| `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs` | Trinket equip/trigger handling, Hero Power use budgets, smart-offer picker, and shared helper paths. |
| `Assets/LearnHearthstone/Runtime/Domain/Engine/TribeAvailabilityRules.cs` | Shared Trinket tribe parsing for smart-offer scoring and availability. |
| `Assets/LearnHearthstone/Tests/EditMode/TrinketSystemTests.cs` | Final-seven, catalog cleanup, and smart-offer coverage. |
| `TrinketRemainingImplementationPlan.md` | Current completion state and validation record. |

Validation recorded in planning/logs: full Trinket fixture passed through
`Logs/TrinketSmartOfferFull.xml` with 220/220.

## Group B - Hero, Buddy, And Combat Completion

Purpose: complete multiple hero-effect batches, combat-event support, Secret support,
Putricide/StarCraft hero systems, candidate policy updates, and related registry/docs.

Files:

| File | Role |
| --- | --- |
| `Assets/LearnHearthstone/Runtime/Domain/Data/HeroEffectImplementationRegistry.cs` | Hero/buddy implementation status sync. |
| `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs` | Combat event, Secret, Deathrattle, kill/attack, and hero/buddy combat behavior. |
| `Assets/LearnHearthstone/Runtime/Domain/Engine/HeroEffectEngine.cs` | Hero Power, Buddy, Putricide, StarCraft, and turn/combat effect behavior. |
| `Assets/LearnHearthstone/Runtime/Domain/Engine/TavernSpellEngine.cs` | Tavern spell and shared generated-card behavior used by hero/timewarped systems. |
| `Assets/LearnHearthstone/Runtime/Domain/Models/CombatModels.cs` | Combat reward/model support. |
| `Assets/LearnHearthstone/Runtime/Domain/Models/TavernMatchModels.cs` | Match/tavern state support for added systems. |
| `Assets/LearnHearthstone/Tests/EditMode/HeroCatalogTests.cs` | Hero catalog and candidate-policy checks. |
| `Assets/LearnHearthstone/Tests/EditMode/HeroEffectImplementationRegistryTests.cs` | Registry count/status checks. |
| `Assets/LearnHearthstone/Tests/EditMode/HeroPowerBuddyEffectTests.cs` | Hero/buddy runtime coverage. |
| `Docs/HeroEffectImplementationGaps.md` | Status sync. |
| `Docs/HeroEffectIncompleteCompletionPlan.md` | Plan/status sync. |
| `Docs/HeroEffectRemainingCompletionOrder.md` | Remaining-order sync. |
| `Docs/HeroFrameworkFirstCompletionPlan.md` | FrameworkFirst closure decisions. |
| `Docs/HeroPowerBuddyEffectsImplementationOrder.md` | Hero/buddy implementation order sync. |
| `Docs/HeroPowerProxyCandidateImplementationPlan.md` | Candidate/proxy policy sync. |
| `Docs/HeroFrameworkFirst19ImplementationPlan.md` | New 19-hero implementation plan. |
| `Docs/HeroLargeSystemsFourImplementationPlan.md` | New large-system hero implementation plan. |
| `Docs/HeroRemainingGameplayImplementationPlan.md` | New remaining-gameplay plan. |

Validation recorded in planning/logs includes focused hero/combat runs, compile probes, and the
later full EditMode bisect route after Timewarped boundary stabilization.

## Group C - Timewarped Tavern, Darkmoon Prize, And Boundary Stabilization

Purpose: complete Timewarped P0/P1 runtime gaps, close the Evolving Tavern data boundary, verify
the broad EditMode bisect route, and implement the Timewarped smart-offer picker.

Files:

| File | Role |
| --- | --- |
| `Assets/LearnHearthstone/Resources/Data/timewarpedTavernCards.json` | Current Timewarped status tags for implemented P0 cards. |
| `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs` | Timewarped offer generation, purchase/effect handling, Darkmoon Prize consumers, and smart-offer picker. |
| `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs` | Timewarped combat behavior such as copied Deathrattle resolution. |
| `Assets/LearnHearthstone/Runtime/Domain/Engine/TavernSpellEngine.cs` | Timewarped/Darkmoon generated spell behavior. |
| `Assets/LearnHearthstone/Tests/EditMode/MatchServiceTests.cs` | Timewarped, Darkmoon Prize, Hero Power budget, and smart-offer tests. |
| `Assets/LearnHearthstone/Tests/EditMode/TavernSpellEngineTests.cs` | Tavern spell regression sync. |
| `Assets/LearnHearthstone/Tests/EditMode/AnomalySystemTests.cs` | Broad-route anomaly/YoggIseum stabilization. |
| `Docs/research/timewarped-tavern/timewarped-tavern-incomplete-implementation-audit.md` | Current Timewarped audit status. |
| `Docs/research/timewarped-tavern/timewarped-tavern-remaining-completion-plan.md` | Current Timewarped completion/boundary plan. |
| `Docs/research/timewarped-tavern/timewarped-tavern-remaining-completion-status.md` | Canonical boundary register. |
| `Docs/research/timewarped-tavern/timewarped-tavern-smart-offer-implementation-plan.md` | Smart-offer plan plus implementation status. |
| `Tools/run-editmode-bisect.ps1` | Default EditMode stabilization route. |

Validation recorded in planning/logs:

- `Logs/TimewarpedReaudit.xml`: 123/123 passed.
- `Logs/EditModeBisectSummary.txt`: all 8 default EditMode shards passed.
- `Logs/TimewarpedSmartOffer.xml`: 2/2 passed.
- `Logs/TimewarpedSmartOfferRegression.xml`: 125/125 passed.

## Group D - UI And Asset Audit Support

Purpose: support runtime visibility and broad-route UI tests affected by generated cards and Hero
Power availability.

Files:

| File | Role |
| --- | --- |
| `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernCardComponent.cs` | Generated-card description fallback for UI tests. |
| `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernTrainerController.cs` | Hero Power button disabled state for per-turn budgets. |
| `Docs/ImageAssetAudit.zh-CN.md` | Image audit status sync. |

## Hygiene Result

- `git diff --check` passes for the visible dirty worktree with only line-ending normalization
  warnings.
- Unity compile log `Logs/CodexCompileCheck.log` records `ExitCode: 0` and `Tundra build success`.
- The appropriate handling is a single checkpoint commit on `codex/wip-current-state`, because the
  current diff is a coherent accumulated local implementation checkpoint rather than disposable
  temporary work.
