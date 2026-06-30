# Timewarped Tavern Remaining Completion Status

Date: 2026-06-28

All three remaining productization items are implemented.

## Completed Items

1. Default random Timewarp candidates include implemented non-minion offers.
   - `TimewarpedTavernCatalog.Current` still means the 125 current minions.
   - Implemented non-minions are appended through `OfferableCurrentNonMinionsForKind`.
   - Default counts are now Minor 73 and Major 87.
   - Historical mode counts are now Minor 106 and Major 120.
   - Historical extras, blocked non-minions, `timewarp:exit`, and `TimewarpKind.None` non-minions remain excluded from default random offers.

2. `Timewarped Big Winner!` no longer uses the Bounty proxy.
   - It now discovers generated Tier 3 Darkmoon Prize cards tagged `darkmoon_prize` and `darkmoon_prize_tier_3`.
   - Options no longer carry `bounty` or `darkmoon_prize_proxy`.
   - Tier 3 prizes currently implemented: Training Session, Buy the Holy Light, B.A.N.A.N.A.S., Top Shelf, Repeat Customer, All That Glitters, Mindflayer Goggles, Reserve Prices.
   - Darkmoon Prizes are generated as `CardKind.Spell`, not `CardKind.TavernSpell`, so they do not trigger Tavern spell counters or bonuses.

3. Default EditMode hang diagnostics are in place.
   - `BatchEditModeTestRunner` writes a manifest and supports `-batchTestNameFile`, `-batchTestShardIndex`, and `-batchTestShardCount`.
   - `Tools/run-editmode-bisect.ps1` checks Unity process/lock state, runs default EditMode shards with timeout, records last started test, and bisects a timed-out shard.
   - Stress and Marathon remain excluded from default EditMode.

## Validation

- `Logs/CodexCompileCheck.log`: Unity compile returned `ExitCode: 0`.
- `Logs/TimewarpedRemainingCompletionTests.xml`: 8 targeted EditMode tests passed, 0 failed.
- `Logs/TimewarpedRemainingCompletionManifest.txt`: manifest generated for the targeted validation run.
- `Tools/run-editmode-bisect.ps1`: PowerShell parser check passed.

## Operational Note

Full default EditMode was not blindly rerun as part of this implementation pass. Use `Tools/run-editmode-bisect.ps1` in a dedicated run window to diagnose any future full-run hang; the tooling now turns a hang into a shard/test-name report instead of stopping at `test run started`.
