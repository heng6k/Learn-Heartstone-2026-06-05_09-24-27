# Official Consistency Roadmap

## Scope

This project implements the single-player Battlegrounds Tavern/training experience.
Duos, teammate boards, Passing, and BGDUO gameplay behavior remain out of scope.

## Current Official Checks

Run these checks from the project root:

```text
node Tools/validate-official-tavern-spells.mjs
node Tools/validate-official-battlegrounds-minions.mjs
```

Current minion API result after the 2026-06-11 pass:

```text
official_solo_minions=248
local_solo_minions=248
local_duos_out_of_scope=28
missing_official=0
unexpected_local=0
stat_mismatches=0
keyword_mismatches=0
```

Official keyword checks are now hard validation. Local JSON keeps gameplay
mechanism hints in `keywords`/`tags`, while `officialKeywords` mirrors Blizzard
`keywordIds` for display and API consistency.

## Latest Pool Corrections

Official solo pool entries restored:

- `BG31_803` Buzzing Vermin
- `BG25_013` Rot Hide Gnoll
- `BG26_529` Upbeat Frontdrake

Legacy entries retained in data but removed from the current official solo pool:

- `BG26_800` Manasaber
- `BG33_809` Holy Mecherel
- `BG31_920` Darkcrest Strategist

## Deterministic Trainer Differences

These are acceptable training-mode approximations, but should be visible in review:

- Random generation uses seeded deterministic choices.
- Discover pools use local candidates and fixed seed order.
- Combat rewards that say "random" are resolved through local helper pools.
- Some complex targets use leftmost or first valid targets when no explicit target is supplied.
- UI keyword display prefers `officialKeywords`; gameplay still reads local
  mechanism keywords and tags.

## Recommended Next Fix Order

1. Add targeted tests for Strike, Rally, Avenge, Deathrattle, Reborn, Magnetic, and golden repeat ordering.
2. Add card-detail UI notes for official text, local implementation status, and trainer approximation.
3. Keep official API checks as offline validation tools, not runtime dependencies.
4. Continue removing legacy in-pool entries when official solo data changes.
