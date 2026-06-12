# Project Scope

## Single-Player Tavern Only

This project only implements the single-player Battlegrounds Tavern/training experience.

Do not implement Duos-only systems in this project:

- no teammate board, hand, shop, or hero state
- no Passing system
- no team combat rewards
- no Duos-specific minion effects
- no solo approximations for Duos cards

Cards whose ids start with `BGDUO` are intentionally out of scope. If they appear in source data, mark them as `OutOfScope` in implementation registries instead of adding gameplay behavior.

The current design target is: single-player Tavern flow, recruit economy, single-player board state, combat simulation, replay visualization, tier 1/2/3/4 single-player minions, and all official single-player Tavern spells.
