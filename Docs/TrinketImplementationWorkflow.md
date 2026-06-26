# Trinket Implementation Workflow

This document is the repeatable checklist for implementing one Battlegrounds Trinket effect.

Use it when taking a Trinket from `FrameworkFirst` or `DebugOnly` to a real playable implementation.

## Goal

Each finished Trinket should have:

- A stable `effectId`.
- Runtime behavior wired to the correct event.
- Deterministic focused tests.
- Accurate JSON status fields.
- Planning notes updated with the result and any reusable findings.
- Unity MCP validation with real matched tests, not `summary.total=0`.

## Main Files

Runtime:

- `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/TavernSpellEngine.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Models/AdvancedMechanicModels.cs`

Data:

- `Assets/LearnHearthstone/Resources/Data/battlegroundsTrinkets.json`
- `Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json`
- `Assets/LearnHearthstone/Resources/Data/tavernSpells.json`

Tests and planning:

- `Assets/LearnHearthstone/Tests/EditMode/TrinketSystemTests.cs`
- `.planning/trinket-system-implementation/progress.md`
- `.planning/trinket-system-implementation/findings.md`

Reference docs:

- `Docs/TrinketSystemImplementationPlan.md`
- `Docs/TrinketEffectFullImplementationPlan.md`
- `Docs/TrinketAutoCastTroubleshooting.md`

## Step 1: Pick and Classify the Trinket

Record the target first:

- `cardId`
- name
- Lesser or Greater
- cost
- text
- `relatedDbfId`
- `mechanics`
- `referencedTags`
- current `implementationStatus`
- current `offerPoolStatus`
- current `effectFamily`
- current `requires`
- current `proxyLevel`

Skip or defer if:

- The text still contains official placeholder values such as `92`.
- It depends on unsupported Duo, teammate, pass, or partner behavior.
- It needs a complex system that is not implemented yet and cannot be proxied safely.
- It would silently mislead normal play if marked `Offerable`.

If deferred, keep it `DebugOnly` or `Disabled` and write the reason in `notes`.

## Step 2: Search for Existing Building Blocks

Before writing code, search for:

- The Trinket `cardId`.
- The target minion, spell, or reward card id.
- Existing `effectId` patterns nearby.
- Similar tests in `TrinketSystemTests`.
- Existing dispatch hooks in `MatchService`.

Prefer existing helpers over new one-off logic. Good examples:

- `AddMinionByCardIdToHand`
- `AddTavernSpellToHand`
- `HasEquippedTrinketEffect`
- `DispatchTrinketTurnStarted`
- `DispatchTrinketTurnEnded`
- `DispatchTrinketTavernSpellCast`
- `DispatchTrinketMinionSold`
- `DispatchTrinketGoldSpent`
- `DispatchTrinketMagnetized`
- `BuffAllMinions`
- `ApplyBloodGemToMinion`

Use stable card ids. Do not key behavior off display names.

## Step 3: Choose the Runtime Hook

Map the text to exactly one primary hook:

| Text pattern | Usual hook |
| --- | --- |
| "Get X." | `ApplyTrinketEquippedEffects` |
| "At the start of each turn" | turn-start dispatch |
| "At the end of your turn" | turn-end dispatch |
| "After you buy" | buy dispatch |
| "After you spend Gold" | gold-spent dispatch |
| "After you sell" | sell dispatch |
| "After you play" | card/minion played dispatch |
| "After you cast a Tavern spell" | Tavern spell cast dispatch |
| "Whenever a friendly minion is Magnetized" | magnetic dispatch |
| "Avenge" or combat deaths | combat state plus `CombatEngine` |
| "Start of combat" | combat-start preparation |
| "Discover" | pending choice / discover resolution |
| "Spellcraft" | generated spell card plus spell engine handling |

If the hook does not exist, add a reusable dispatch method instead of wiring a single effect directly into unrelated code.

## Step 4: Implement Behavior Conservatively

Keep the change narrow:

1. Add constants for any card ids, counter keys, or generated card ids.
2. Add or reuse a helper for the behavior.
3. Add the `effectId` case in the correct dispatch switch.
4. Ensure full-hand, empty-board, empty-shop, no-valid-target, and missing-catalog cases are safe no-ops.
5. Use `SeededRng` for random behavior so tests remain deterministic.
6. Use `PoolSource.Copy` and `PoolCopiesHeld = 0` for generated copies that should not consume the shared pool.
7. Call existing hand/shop/board side-effect helpers when adding cards or changing shop contents.

For specified minions:

- Prefer `AddMinionByCardIdToHand(cardId, source)`.
- Only add a proxy fallback if the local minion catalog lacks the card.
- If a proxy is used, set `proxyLevel` to `ProxySafe` unless the proxy is intentionally debug-only.

For Tavern spells:

- Prefer `AddTavernSpellToHand(cardNumber, source)`.
- Automatic casts should use the shared safe auto-cast path.
- Respect current Tavern Tier when text says random Tavern spell limited by Tavern Tier.
- Guard recursion and trigger chains.

For shop auras:

- Make aura application removable and recalculable.
- Avoid stacking the same aura repeatedly after refreshes.

For combat effects:

- Keep temporary combat-only state separate from permanent Tavern state.
- Write permanent changes back only when the text requires it.
- Add combat reward plumbing if state must be applied after combat.

## Step 5: Update Trinket JSON

When the behavior is real, update the target row in `battlegroundsTrinkets.json`:

```json
"effectIds": [
  "example_effect_id"
],
"implementationStatus": "Implemented",
"notes": "Short explanation of implemented behavior and any proxy limits.",
"offerPoolStatus": "Offerable",
"powerLevel": "Medium",
"effectFamily": "turn_start",
"requires": [
  "tribe_pool",
  "turn_start"
],
"proxyLevel": "Exact"
```

Rules:

- `Offerable` requires `Implemented`.
- `Offerable` requires at least one `effectId`.
- Convert `requires` to an array. Unity `JsonUtility` does not reliably deserialize imported string values such as `"requires": "tribe_pool"` into `List<string>`.
- Use `Exact` only when the implemented behavior matches the local system directly.
- Use `ProxySafe` when behavior is playable but intentionally approximated.
- Keep placeholder or uncertain official semantics out of `Offerable`.

After editing JSON, run an aggregate count check:

```powershell
$json = Get-Content -Raw 'Assets\LearnHearthstone\Resources\Data\battlegroundsTrinkets.json' | ConvertFrom-Json
$all = @($json.trinkets)
[pscustomobject]@{
  Total = $all.Count
  Implemented = @($all | Where-Object implementationStatus -eq 'Implemented').Count
  Unimplemented = @($all | Where-Object implementationStatus -ne 'Implemented').Count
  Offerable = @($all | Where-Object offerPoolStatus -eq 'Offerable').Count
  LesserOfferable = @($all | Where-Object { $_.offerPoolStatus -eq 'Offerable' -and $_.slotKind -eq 'Lesser' }).Count
  GreaterOfferable = @($all | Where-Object { $_.offerPoolStatus -eq 'Offerable' -and $_.slotKind -eq 'Greater' }).Count
  OfferablePending = @($all | Where-Object { $_.implementationStatus -ne 'Implemented' -and $_.offerPoolStatus -eq 'Offerable' }).Count
} | Format-List
```

## Step 6: Add Focused Tests

Update `TrinketSystemTests.cs`.

Minimum tests:

- Catalog test count changes.
- Catalog assertions for the target Trinket:
  - slot kind
  - `Implemented`
  - `Offerable`
  - `powerLevel`
  - `effectFamily`
  - `proxyLevel`
  - `effectIds`
  - required `requires`
- One behavior test that exercises the real trigger.

Good behavior test shape:

1. Create `MatchService.CreateWithDefaultCatalog(seed)`.
2. Set enough Gold if the test equips through a choice.
3. Queue the target Trinket.
4. Equip through `ChooseMechanicOption`.
5. Set up only the needed board/hand/shop state.
6. Trigger the event.
7. Assert positive and negative targets.
8. Assert generated cards have correct `CardKind`, tier, tribes, keywords, `PoolSource.Copy`, and `PoolCopiesHeld = 0`.

Avoid broad tests unless the implementation touches shared behavior with large blast radius.

## Step 7: Refresh Unity Correctly

After C# edits:

- Refresh Unity with compile request.

After JSON or `Resources/Data` edits:

- Refresh Unity with `scope: all`.
- A script-only refresh may leave `TextAsset` data stale.

Before running tests, confirm Unity is idle and not compiling.

MCP command shape is framed JSON:

```json
{ "type": "run_tests", "params": { "mode": "EditMode", "testNames": ["Full.NUnit.Test.Name"] } }
```

Do not use JSON-RPC `method`.

Use full NUnit names. A run with `summary.total=0` is not validation.

## Step 8: Run Targeted Tests

Prefer targeted tests for one Trinket:

- `LearnHearthstone.Tests.EditMode.TrinketSystemTests.Catalog_LoadsLesserAndGreaterTrinketsWithVisibleStatuses`
- The new behavior test.
- Any existing shared helper test that changed.

Pass criteria:

- `summary.total` matches the number of expected tests.
- `failed = 0`.
- `skipped = 0` unless intentionally skipped and documented.

If catalog count fails after JSON changes, refresh Unity with `scope: all` and rerun before changing logic.

## Step 9: Update Planning Docs

Update `.planning/trinket-system-implementation/progress.md` with:

- Date and Trinket name/card id.
- Runtime behavior implemented.
- JSON status changes.
- Tests added.
- Unity MCP result.
- New catalog counts.

Update `.planning/trinket-system-implementation/findings.md` when the work reveals:

- A card-id mapping.
- A proxy fallback.
- A reusable helper.
- A Unity MCP or JSON import pitfall.
- A new trigger entry point.

Do not add noisy findings for ordinary one-off implementation details.

## Step 10: Final Response Checklist

Report:

- What was implemented.
- Files touched.
- Tests run and result.
- Current implemented/remaining counts if asked or useful.
- A concrete next-step recommendation.

Keep the final answer short. The user can inspect the files directly.

## Common Templates

### Specified Minion Portrait

Use when text says "Get X" and modifies X.

Checklist:

- Add card id constant if missing.
- `ApplyTrinketEquippedEffects`: add X to hand.
- Modify X's existing effect branch with `HasEquippedTrinketEffect(effectId)`.
- Test equip card properties.
- Test modified effect.

Examples:

- Bronzebeard Portrait
- Drakkari Portrait
- Enforcer Portrait
- Bristlebach Portrait
- Czarina Portrait

### Turn-Start Card Generator

Use when text says "Get X. At the start of each turn, get another."

Checklist:

- Add X on equip.
- Add X in turn-start dispatch.
- Test equip and next turn.
- Verify hand limit behavior if helper does not already handle it.

Examples:

- Boom's Monster Portrait
- Butcher's Sickle
- Pocket Cyclone

### Sell-Count Trigger

Use when text says "After you sell N minions..."

Checklist:

- Store a counter under `AdvancedMechanicState.Counters` or `PlayerTrinketState`.
- Increment only from the shared sell path.
- Preserve overflow only if text implies it.
- Reset per turn only if text says "this turn".
- Test threshold and non-threshold sells.

Examples:

- Lava Lamp
- Fungalmancer Sticker
- Avalanche Portrait

### Random Generated Minion

Use when text grants random minions by tier or tribe.

Checklist:

- Filter from current available catalog, not a hard-coded list, unless the official pool is a fixed token family.
- Respect active tribes and Tavern Tier when required.
- Use seeded randomness.
- Generated copies use `PoolSource.Copy` unless text explicitly draws from Tavern pool.
- Test with a controlled pool when possible.

Examples:

- Pagle's Fishing Rod
- Explorer's Binoculars
- Chromatic Tear

### Automatic Tavern Spell Cast

Use when text casts Tavern spells automatically.

Checklist:

- Route through the safe auto-cast helper.
- Limit to current Tavern Tier when text requires it.
- Guard recursion depth and trigger chains.
- Do not double charge Gold.
- Test counters and final board state.

Examples:

- Lavish Cape
- Pocket Cyclone

## Common Pitfalls

- `rg.exe` may fail with `Access is denied` in this workspace; use PowerShell `Get-ChildItem | Select-String`.
- JSON patches can accidentally match another pending Trinket with the same empty `effectIds`; include unique `cardId` and nearby fields.
- `requires` must be an array before catalog assertions rely on it.
- Do not mark proxy behavior `Exact`.
- Do not make placeholder text such as official `92` playable.
- Do not use simple test names in Unity MCP if they match zero tests.
- Do not trust a green MCP result with `summary.total=0`.
- Do not refresh only scripts after editing `Resources/Data`.
- Do not add a Trinket to `Offerable` unless it is actually implemented and tested.

## Done Definition

A Trinket is done when:

- The runtime effect works from real equipped state.
- Catalog status is `Implemented`.
- Normal pool status is correct.
- Proxy limitations are documented.
- Targeted EditMode tests pass in Unity.
- Aggregate counts are updated.
- Progress docs record the work.
- The next implementation candidate is clear.
