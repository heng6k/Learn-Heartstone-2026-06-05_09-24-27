# Card Acquisition and Opponent Customization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a right-side tab workflow that lets the player add any minion or tavern spell to hand and customize the opponent board for combat testing.

**Architecture:** Keep UI state in `TavernTrainerView`, but route all match mutations through `GameCommand` and `MatchService`. Use catalog-backed instance creation so debug cards behave like normal minions/spells without consuming pool copies.

**Tech Stack:** Unity C#, UGUI, NUnit EditMode tests, existing `MatchService`, `GameCommand`, `MinionCatalogLoader`, `SpellCatalogLoader`, and `TavernTrainerView`.

---

## File Structure

- Modify `Assets/LearnHearthstone/Runtime/Application/Commands/GameCommand.cs`
  - Add command types and constructors for card lookup and opponent editing.
- Modify `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
  - Implement add-card-to-hand, add/remove/move/update opponent minion commands.
- Create `Assets/LearnHearthstone/Tests/EditMode/MatchServiceDebugCardTests.cs`
  - Cover player hand debug acquisition.
- Create `Assets/LearnHearthstone/Tests/EditMode/OpponentCustomizationTests.cs`
  - Cover opponent board customization commands.
- Modify `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/TavernTrainerView.cs`
  - Add right inspector tabs, card acquisition tab, and opponent customization tab.
- Modify `Assets/LearnHearthstone/Tests/EditMode/TavernTrainerViewTests.cs`
  - Cover tab construction and key UI entries.

---

## Task 1: Player Hand Debug Acquisition

**Files:**

- Modify: `Assets/LearnHearthstone/Runtime/Application/Commands/GameCommand.cs`
- Modify: `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
- Create: `Assets/LearnHearthstone/Tests/EditMode/MatchServiceDebugCardTests.cs`

- [ ] **Step 1: Write failing tests**

Create `MatchServiceDebugCardTests.cs` with tests equivalent to:

```csharp
[Test]
public void Apply_AddMinionCardToHandCreatesPlayerHandInstance()
{
    var service = MatchService.CreateWithDefaultCatalog(12345);
    var cardId = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).CardId;
    service.State.Player.Tavern.Hand.Clear();

    service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, CardKind.Minion));

    Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
    Assert.AreEqual(cardId, service.State.Player.Tavern.Hand[0].CardId);
    Assert.AreEqual(BoardSide.Player, service.State.Player.Tavern.Hand[0].Owner);
    Assert.AreEqual(PoolSource.Copy, service.State.Player.Tavern.Hand[0].PoolSource);
}
```

Also test tavern spells, hand full failure, and pool count unchanged.

- [ ] **Step 2: Run red test**

Run:

```powershell
Remove-Item -LiteralPath "D:\unity project\Learn Heartstone\CodexEditModeResults.xml" -ErrorAction SilentlyContinue
& "D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe" -batchmode -quit -projectPath "D:\unity project\Learn Heartstone" -executeMethod CodexEditModeTestRunner.Run -logFile "D:\unity project\Learn Heartstone\UnityDebugCardRed.log"
```

Expected: compile failure because `GameCommandType.AddCardToHand` and the matching constructor do not exist.

- [ ] **Step 3: Implement minimal command and service support**

Add `AddCardToHand` to `GameCommandType`, add a constructor accepting `(GameCommandType type, string cardId, CardKind cardKind)`, expose `CardId` and `CardKind`, and implement `MatchService.AddCardToHand`.

The implementation should:

- Validate hand limit 10.
- Resolve minions through `catalog.GetByCardId(cardId)`.
- Resolve tavern spells through `spellCatalog.GetByCardId(cardId)`.
- Clone/create an instance with `Owner = BoardSide.Player`, `PoolSource = PoolSource.Copy`, `PoolCopiesHeld = 0`.
- Append to `State.Player.Tavern.Hand`.
- Add a recruit log entry.

- [ ] **Step 4: Run green test**

Run the same Unity EditMode command with `UnityDebugCardGreen.log`.

Expected: all tests pass.

---

## Task 2: Opponent Board Customization Commands

**Files:**

- Modify: `Assets/LearnHearthstone/Runtime/Application/Commands/GameCommand.cs`
- Modify: `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
- Create: `Assets/LearnHearthstone/Tests/EditMode/OpponentCustomizationTests.cs`

- [ ] **Step 1: Write failing tests**

Create tests covering:

```csharp
[Test]
public void Apply_AddOpponentMinionCreatesOpponentBoardInstance()
{
    var service = MatchService.CreateWithDefaultCatalog(12345);
    var cardId = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).CardId;
    service.State.Opponent.Board.Clear();

    service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, cardId));

    Assert.AreEqual(1, service.State.Opponent.Board.Count);
    Assert.AreEqual(cardId, service.State.Opponent.Board[0].CardId);
    Assert.AreEqual(BoardSide.Opponent, service.State.Opponent.Board[0].Owner);
}
```

Also test board full failure, remove, move, update stats, and update keywords.

- [ ] **Step 2: Run red test**

Run Unity EditMode with `UnityOpponentCustomizationRed.log`.

Expected: compile failure because opponent customization command types do not exist.

- [ ] **Step 3: Implement opponent commands**

Add command types:

- `AddOpponentMinion`
- `RemoveOpponentMinion`
- `MoveOpponentMinion`
- `UpdateOpponentMinion`

Implement `MatchService` handlers:

- `AddOpponentMinion(string cardId)` creates an opponent-owned board instance from `MinionCatalog`.
- `RemoveOpponentMinion(string instanceId)` removes from `State.Opponent.Board`.
- `MoveOpponentMinion(string instanceId, int targetIndex)` reorders within opponent board.
- `UpdateOpponentMinion(string instanceId, MinionPatch patch)` applies attack, health, max health, golden, keywords, and tribes to opponent board minions.

Clamp opponent update values:

- Attack: `Math.Max(0, value)`
- MaxHealth: `Math.Max(1, value)`
- Health: `Math.Max(1, Math.Min(value, nextMaxHealth))`

- [ ] **Step 4: Run green test**

Run Unity EditMode with `UnityOpponentCustomizationGreen.log`.

Expected: all tests pass.

---

## Task 3: Right Inspector Tabs and UI Entries

**Files:**

- Modify: `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/TavernTrainerView.cs`
- Modify: `Assets/LearnHearthstone/Tests/EditMode/TavernTrainerViewTests.cs`

- [ ] **Step 1: Write failing UI construction tests**

Add tests asserting these objects exist after `Build()`:

```csharp
Assert.IsNotNull(FindChild(rootObject.transform, "RightInspectorTabs"));
Assert.IsNotNull(FindChild(rootObject.transform, "Tab-CardAcquisition"));
Assert.IsNotNull(FindChild(rootObject.transform, "Tab-OpponentCustomization"));
Assert.IsNotNull(FindChild(rootObject.transform, "CardAcquisitionPanel"));
Assert.IsNotNull(FindChild(rootObject.transform, "OpponentCustomizationPanel"));
```

Also assert labels/buttons include:

- `加入手牌`
- `添加对手`
- `左移`
- `右移`
- `删除`

- [ ] **Step 2: Run red test**

Run Unity EditMode with `UnityRightTabsRed.log`.

Expected: UI tests fail because the tab objects do not exist.

- [ ] **Step 3: Implement tab state and panels**

In `TavernTrainerView`:

- Add `RightInspectorTab activeRightTab`.
- Add enum values `Info`, `CardAcquisition`, `OpponentCustomization`.
- Replace the right inspector body with a tab row and conditional content.
- Keep existing opponent preview/editor/hints/logs inside the `Info` tab.
- Build `CardAcquisitionPanel` with search result rows and `加入手牌` buttons.
- Build `OpponentCustomizationPanel` with opponent board slots, search result rows, selected opponent editor, and move/delete buttons.

First version search behavior can use a compact default list:

- Search text empty: show first 8 minions or spells based on active section.
- Search text filled: match `Name`, `CardId`, or `Text`.

- [ ] **Step 4: Run green test**

Run Unity EditMode with `UnityRightTabsGreen.log`.

Expected: all tests pass.

---

## Task 4: Final Verification and Commit

**Files:**

- All modified implementation and test files.

- [ ] **Step 1: Run full Unity EditMode tests**

Run:

```powershell
Remove-Item -LiteralPath "D:\unity project\Learn Heartstone\CodexEditModeResults.xml" -ErrorAction SilentlyContinue
& "D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe" -batchmode -quit -projectPath "D:\unity project\Learn Heartstone" -executeMethod CodexEditModeTestRunner.Run -logFile "D:\unity project\Learn Heartstone\UnityCardAcquisitionOpponentFinal.log"
```

Expected log line:

```text
CODEX_EDITMODE_RESULT total=<n> passed=<n> failed=0 skipped=0 state=Passed
```

- [ ] **Step 2: Run git checks**

Run:

```powershell
git diff --check
git status --short
```

Expected: `git diff --check` exits 0; status only contains intended source, test, plan, and Unity `.meta` changes.

- [ ] **Step 3: Commit and push**

Run:

```powershell
git add -A
git add -f "Docs\superpowers\plans\2026-06-06-card-acquisition-opponent-customization.md"
git commit -m "Add card acquisition and opponent customization UI"
git push
```
