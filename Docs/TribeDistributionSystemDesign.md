# Tribe Distribution System Design

## Goal

The Battlegrounds trainer uses `BoardTribeAnalyzer` as the single source of truth for board tribe analysis. UI diagnostics, "most common type", "one of each type", "different types", and same-type matching should call this analyzer instead of re-counting tribes locally.

## Count Rules

- Only `CardKind.Minion` entries on the board are counted.
- `null` entries, tavern spells, `Tribe.None`, and missing tribe lists are ignored.
- Dual-tribe minions count once for each listed playable tribe.
- `Tribe.All` expands to every playable tribe: Beast, Murloc, Mech, Demon, Dragon, Pirate, Elemental, Quilboar, Undead, and Naga.
- A single minion can contribute to multiple tribe counts, but `SelectOneOfEachTribe` selects each minion at most once for effects that buff one minion per type.

## Tie Rules

`GetMostCommonTribe` breaks ties with the fixed playable tribe order:

`Beast -> Murloc -> Mech -> Demon -> Dragon -> Pirate -> Elemental -> Quilboar -> Undead -> Naga`

Empty boards return `Tribe.None`. Effects that need a fallback, such as Friendly Bounty and Planar Telescope, convert `Tribe.None` to `Tribe.All`.

## Current Consumers

- Trainer UI: player board shows `种族分布：龙 2 / 鱼人 1` and highlights the highest-count tribe or tribes.
- Friendly Bounty: adds a random minion of the player's most common tribe.
- Planar Telescope: discovers a minion of the player's most common tribe.
- Menagerie Tableware: buffs the board based on `CountDistinctTribes`.
- Misplaced Tea Set / `乱放的茶具`: buffs `SelectOneOfEachTribe`.
- Natural Blessing / same-type target matching: uses `GetCountedTribes`.
- Chef's Choice combat spell target validation: uses `GetCountedTribes`.
- Hamuul's Lost Staff and Chef's Choice acquisition paths: use analyzer-derived target tribes for refresh/discover filters.

## Adding New Cards

Use these entry points:

- "Most common type": `BoardTribeAnalyzer.GetMostCommonTribe(player)`.
- "Different friendly minion types": `BoardTribeAnalyzer.CountDistinctTribes(board)`.
- "One of each type": `BoardTribeAnalyzer.SelectOneOfEachTribe(board)`.
- "Same type as target": `BoardTribeAnalyzer.GetCountedTribes(target)` plus a shared tribe match helper.

Do not use `minion.Tribes.FirstOrDefault(...)` for tribe logic unless the card text explicitly says to use only the first listed tribe.
