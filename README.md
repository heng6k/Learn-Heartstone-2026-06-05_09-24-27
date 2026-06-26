# Learn Heartstone

Learn Heartstone is an unofficial single-player Battlegrounds training tool for testing Tavern decisions, combat setups, positioning, card-pool variants, and replay logs.

Current release target: `v0.1.0-alpha`.

This is an alpha build, not a complete official simulator and not a beginner tutorial. It is intended for players who want to construct situations, test ideas, and report bugs.

## Download

Download the latest Windows package from the repository's **Releases** page:

```text
BattlegroundsTrainer_v0.1.0-alpha_win.zip
```

After downloading:

1. Extract the zip.
2. Open `Learn Heartstone.exe`.
3. Read the included `README_使用说明.md` and `KNOWN_ISSUES_已知问题.md`.

## What You Can Test

- Enter the single-player Tavern trainer.
- Choose active tribes.
- Create and name card-pool versions.
- Filter minions and Tavern spells by tier, type, and search text.
- Exclude cards from a custom card pool and save the version.
- Enter a match using the selected card-pool version.
- Refresh, buy, sell, freeze, upgrade, and advance turns.
- Run combat and inspect combat/recruit logs.
- Use the bottom-right window-ratio debug button to check small-window layouts.

## Alpha Limits

- This project only targets the single-player Tavern/training flow.
- Duos-only cards and BGDUO behavior are intentionally out of scope.
- Some hero, buddy, and complex card effects are framework-first or simplified.
- Some images may be missing.
- UI polish is still in progress, especially in small windows.
- Batch card-pool removal has no undo button yet.

## Feedback

Please include:

```text
Version:
What you tried to do:
What happened:
Expected result:
Can you reproduce it:
Screenshot / board state / log:
```

For public release, also confirm that all bundled assets are suitable for the distribution scope you choose.

## Documentation

- [v0.1-alpha release package plan](Docs/V0.1AlphaReleasePackagePlan.md)
- [Alpha stabilization test report](Docs/AlphaStabilizationTestReport.md)
- [Card-pool version-control bug hunt](Docs/CardPoolVersionControlBugHuntReport.md)
- [Project scope](PROJECT_SCOPE.md)
