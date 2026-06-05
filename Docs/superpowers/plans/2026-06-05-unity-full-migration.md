# Unity Full Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Unity + C# version of the existing battleground trainer with modular rules, data/resources, application services, main hub UI, tavern trainer UI, logs, replay controls, editor, search hints, persistence, and tests.

**Architecture:** Domain rules are pure C# and independent of Unity scenes. Application services receive UI commands and update match state. Presentation code renders a main hub and trainer workspace through Unity UGUI views backed by ViewModels.

**Tech Stack:** Unity 6000.4.10f1, C#, UGUI, Unity Test Framework, Unity JsonUtility-compatible DTO wrappers, Resources-based data/image loading.

---

## File Structure

- Create `Assets/LearnHearthstone/LearnHearthstone.Runtime.asmdef` for runtime code.
- Create `Assets/LearnHearthstone/Tests/EditMode/LearnHearthstone.Tests.asmdef` for EditMode tests.
- Create `Assets/LearnHearthstone/Runtime/Domain/Models/*` for enums and state objects.
- Create `Assets/LearnHearthstone/Runtime/Domain/Engine/*` for RNG, rules, pool, triples, combat.
- Create `Assets/LearnHearthstone/Runtime/Domain/Data/*` for minion catalog.
- Create `Assets/LearnHearthstone/Runtime/Domain/Effects/*` for effect registry.
- Create `Assets/LearnHearthstone/Runtime/Application/*` for commands, services, view models, logging, replay.
- Create `Assets/LearnHearthstone/Runtime/Adapters/*` for data loading, image loading, persistence, advisor.
- Create `Assets/LearnHearthstone/Runtime/Presentation/*` for UGUI view scripts and programmatic UI builders.
- Copy `kaifa/src/data/battlegroundsMinions.json` to `Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json`.
- Copy card PNG files from `jiaocheng/数据/1-7本随从` to `Assets/LearnHearthstone/Resources/CardImages`.
- Modify `Assets/Scenes/SampleScene.unity` through an Editor scene setup script or a runtime bootstrap object so the project opens to the main hub.
- Update `Docs/ProjectProgress.md` after implementation.

## Task 1: Runtime And Test Assemblies

**Files:**
- Create: `Assets/LearnHearthstone/LearnHearthstone.Runtime.asmdef`
- Create: `Assets/LearnHearthstone/Tests/EditMode/LearnHearthstone.Tests.asmdef`
- Create: `Assets/LearnHearthstone/Tests/EditMode/TavernRulesTests.cs`
- Create: `Assets/LearnHearthstone/Runtime/Domain/Engine/TavernRules.cs`

- [ ] **Step 1: Write the failing TavernRules test**

```csharp
using LearnHearthstone.Domain.Engine;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TavernRulesTests
    {
        [Test]
        public void GetMaxGoldForRound_StartsAtThreeAndCapsAtTen()
        {
            Assert.AreEqual(3, TavernRules.GetMaxGoldForRound(1));
            Assert.AreEqual(10, TavernRules.GetMaxGoldForRound(20));
        }
    }
}
```

- [ ] **Step 2: Run EditMode tests and verify RED**

Run:

```powershell
& 'D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe' -batchmode -quit -projectPath 'D:\unity project\Learn Heartstone' -runTests -testPlatform EditMode -testResults 'D:\unity project\Learn Heartstone\TestResults.xml'
```

Expected: FAIL because `LearnHearthstone.Domain.Engine.TavernRules` does not exist.

- [ ] **Step 3: Add assembly definitions and minimal TavernRules**

Create runtime asmdef with Unity Test Framework compatible references. Implement `TavernRules` with tier limits, shop sizes, upgrade costs, max gold, and cost decrement.

- [ ] **Step 4: Run EditMode tests and verify GREEN**

Expected: TavernRules test passes.

## Task 2: Domain Models, RNG, Pool, Triples, Combat

**Files:**
- Create: `Assets/LearnHearthstone/Runtime/Domain/Models/Enums.cs`
- Create: `Assets/LearnHearthstone/Runtime/Domain/Models/MinionDefinition.cs`
- Create: `Assets/LearnHearthstone/Runtime/Domain/Models/MinionInstance.cs`
- Create: `Assets/LearnHearthstone/Runtime/Domain/Models/TavernState.cs`
- Create: `Assets/LearnHearthstone/Runtime/Domain/Models/MatchState.cs`
- Create: `Assets/LearnHearthstone/Runtime/Domain/Models/CombatModels.cs`
- Create: `Assets/LearnHearthstone/Runtime/Domain/Engine/SeededRng.cs`
- Create: `Assets/LearnHearthstone/Runtime/Domain/Engine/MinionPool.cs`
- Create: `Assets/LearnHearthstone/Runtime/Domain/Engine/TripleEngine.cs`
- Create: `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs`
- Test: `Assets/LearnHearthstone/Tests/EditMode/DomainEngineTests.cs`

- [ ] **Step 1: Write failing tests for RNG, pool, triples, and combat**

Tests cover deterministic RNG, pool occupancy/release, three non-golden copies forming a golden minion, taunt targeting, divine shield removal, poisonous lethal damage, and combat safety limit.

- [ ] **Step 2: Run tests and verify RED**

Expected: FAIL because model and engine types do not exist.

- [ ] **Step 3: Implement model and engine classes**

Port the existing TypeScript behavior conservatively. Keep classes serializable and clone state where command services need immutability.

- [ ] **Step 4: Run tests and verify GREEN**

Expected: all domain engine tests pass.

## Task 3: Data And Resource Migration

**Files:**
- Create: `Assets/LearnHearthstone/Runtime/Domain/Data/MinionCatalog.cs`
- Create: `Assets/LearnHearthstone/Runtime/Adapters/Data/MinionCatalogLoader.cs`
- Create: `Assets/LearnHearthstone/Runtime/Adapters/Images/CardImageProvider.cs`
- Create: `Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json`
- Create: `Assets/LearnHearthstone/Resources/CardImages/*.png`
- Test: `Assets/LearnHearthstone/Tests/EditMode/MinionCatalogTests.cs`

- [ ] **Step 1: Write failing catalog loading tests**

Tests load the copied JSON, assert count `279`, assert `BG35_801` exists with Chinese name, tier 1, attack 2, health 3, and assert at least one image path resolves or falls back to placeholder.

- [ ] **Step 2: Run tests and verify RED**

Expected: FAIL because catalog loader and resources do not exist.

- [ ] **Step 3: Copy data and images**

Copy source JSON and card PNGs into Unity Resources. Normalize image file names by card ID. Create placeholder card art if no card image exists.

- [ ] **Step 4: Implement catalog and image providers**

Parse source JSON into DTOs and map Chinese tribes/keywords into internal enums. Provide fallback handling for unknown keywords and missing images.

- [ ] **Step 5: Run tests and verify GREEN**

Expected: catalog tests pass and no data count regression.

## Task 4: Application Services

**Files:**
- Create: `Assets/LearnHearthstone/Runtime/Application/Commands/GameCommand.cs`
- Create: `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
- Create: `Assets/LearnHearthstone/Runtime/Application/Services/ReplayService.cs`
- Create: `Assets/LearnHearthstone/Runtime/Application/ViewModels/*`
- Create: `Assets/LearnHearthstone/Runtime/Domain/Effects/EffectRegistry.cs`
- Test: `Assets/LearnHearthstone/Tests/EditMode/MatchServiceTests.cs`

- [ ] **Step 1: Write failing command tests**

Tests cover initial match, buy, reroll, freeze, upgrade, play, sell, next turn, choose discover, simulate combat, and debug gold.

- [ ] **Step 2: Run tests and verify RED**

Expected: FAIL because application service classes do not exist.

- [ ] **Step 3: Implement services**

Port `MatchEngine.ts` behavior into C# service methods while splitting responsibilities into command helper methods. Use `Result<T>` or controlled exceptions to surface UI messages.

- [ ] **Step 4: Run tests and verify GREEN**

Expected: all MatchService tests pass.

## Task 5: Persistence And Advisor

**Files:**
- Create: `Assets/LearnHearthstone/Runtime/Adapters/Persistence/ISaveRepository.cs`
- Create: `Assets/LearnHearthstone/Runtime/Adapters/Persistence/JsonSaveRepository.cs`
- Create: `Assets/LearnHearthstone/Runtime/Adapters/Advisor/IAdvisorService.cs`
- Create: `Assets/LearnHearthstone/Runtime/Adapters/Advisor/LocalAdvisorService.cs`
- Test: `Assets/LearnHearthstone/Tests/EditMode/AdapterTests.cs`

- [ ] **Step 1: Write failing persistence and advisor tests**

Tests save and load a match state, and assert local advisor returns actionable hints for low gold and available shop buys.

- [ ] **Step 2: Run tests and verify RED**

Expected: FAIL because adapters do not exist.

- [ ] **Step 3: Implement adapters**

Use JSON file storage under `Application.persistentDataPath` for runtime and a temp path for tests. Keep advisor local and deterministic.

- [ ] **Step 4: Run tests and verify GREEN**

Expected: adapter tests pass.

## Task 6: Main Hub And Tavern Trainer UI

**Files:**
- Create: `Assets/LearnHearthstone/Runtime/Presentation/Common/*`
- Create: `Assets/LearnHearthstone/Runtime/Presentation/MainHub/MainHubView.cs`
- Create: `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/TavernTrainerView.cs`
- Create: `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/*Panel.cs`
- Create: `Assets/LearnHearthstone/Runtime/Presentation/LearnHearthstoneBootstrap.cs`
- Create: `Assets/LearnHearthstone/Editor/LearnHearthstoneSceneSetup.cs`

- [ ] **Step 1: Build programmatic UI**

Create a Canvas-driven main hub with module tiles. The active tile opens Tavern Trainer. Disabled tiles are visually reserved for future modules.

- [ ] **Step 2: Build trainer workspace**

Create panels for shop, hand, player board, opponent board, editor, logs, replay controls, search hints, discover, and tavern controls. Bind buttons to `MatchService` commands.

- [ ] **Step 3: Add card rendering**

Render name, attack, health, tier, tribes, keywords, golden state, and image when available.

- [ ] **Step 4: Add scene setup**

Create an Editor menu item that configures `SampleScene` with the bootstrap object and runtime EventSystem.

- [ ] **Step 5: Run Unity manually or via batch compile**

Expected: editor compiles; scene opens to main hub; clicking Tavern Trainer shows migrated trainer UI.

## Task 7: Verification, Docs, And Commit

**Files:**
- Modify: `Docs/ProjectProgress.md`
- Modify: `Docs/UnityMigrationDesign.md` if implementation deviates from design.

- [ ] **Step 1: Run EditMode tests**

Expected: all available tests pass.

- [ ] **Step 2: Run Unity batch compile**

Expected: Unity exits with code 0 and no compile errors.

- [ ] **Step 3: Check git diff**

Expected: only migration files plus intended copied assets are staged.

- [ ] **Step 4: Update progress documentation**

Record implemented modules, known gaps, test results, and how to open the scene.

- [ ] **Step 5: Commit**

Commit message:

```text
feat: migrate trainer foundation to unity
```

## Self-Review

- Spec coverage: The plan covers domain, application, data, resources, UI, persistence, advisor, tests, docs, and commit.
- Placeholder scan: No task uses unresolved placeholders; future module tiles are intentional disabled UI entries.
- Type consistency: Names use `LearnHearthstone` namespace and match the design document.
