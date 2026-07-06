# Battlegrounds Anomalies Historical Completion Plan

Date: 2026-06-29
Status: Historical archive, superseded for current-state auditing.

For the current anomaly audit and remaining recommendations, use:

- `Docs/AnomalySystemCurrentIssuesAndRecommendations.md`

This document used to track the work needed to bring the current HSReplay Battlegrounds anomaly
pool online. That implementation plan is now complete for the current 28-anomaly default pool.
Older task details are intentionally not repeated here because they included stale intermediate
state, especially around Darkmoon Prize proxy coverage.

## Current Baseline

Current catalog:

- `Assets/LearnHearthstone/Resources/Data/battlegroundsAnomalies.json`

Current default anomaly pool:

- Total current HSReplay anomalies: 28
- Implemented: 28
- Planned but not implemented: 0
- Blocked by dependency: 0

The default random pool should continue to offer only anomalies that are `Implemented` and pass
their availability gates.

## Completed Current-Pool Anomalies

| Card ID | Name | Family | Status |
| --- | --- | --- | --- |
| `BG31_Anomaly_123` | Cosmic Duality | SecondHeroPower | Implemented |
| `BG27_Anomaly_504` | Secrets of Norgannon | MinionPool | Implemented |
| `BG35_Anomaly_005` | Anomalous Timeline | SecondHeroPower | Implemented |
| `BG32_Anomaly_001` | Greater Pouches | SecondHeroPower | Implemented |
| `BG35_Anomaly_007` | Lesser Fortune | SecondHeroPower | Implemented |
| `BG34_Anomaly_805` | Oathstone's Summoning | Timewarp | Implemented |
| `BG32_Anomaly_002` | Lesser Pouches | SecondHeroPower | Implemented |
| `BG35_Anomaly_004` | Anomalous Conflux | SecondHeroPower | Implemented |
| `BG31_Anomaly_106` | Marin's Treasure Box | HeroReplacement | Implemented |
| `BG35_Anomaly_002` | Anomalous Cube | SecondHeroPower | Implemented |
| `BG27_Anomaly_711` | Double Header | Economy | Implemented |
| `BG35_Anomaly_001` | Fly the Flag | GeneratedSpell | Implemented |
| `BG35_Anomaly_008` | Greater Fortune | SecondHeroPower | Implemented |
| `BG27_Anomaly_Prizes2` | Darkmoon Faire Prizes | DarkmoonPrize | Implemented |
| `BG27_Anomaly_303` | Grapnel of the Titans | Economy | Implemented |
| `BG27_Anomaly_580` | Audience's Choice | SinglePlayerChoice | Implemented |
| `BG27_Anomaly_751` | Perfected Alchemy | GeneratedSpell | Implemented |
| `BG35_Anomaly_006` | Anomalous Expedition | DelayedReward | Implemented |
| `BG31_Anomaly_124` | Golden Arrow | GeneratedSpell | Implemented |
| `BG27_Anomaly_301` | False Idols | TripleRule | Implemented |
| `BG27_Anomaly_716` | Up-Prizing | DarkmoonPrize | Implemented |
| `BG27_Anomaly_810` | Bring in the Buddies | Buddy | Implemented |
| `BG27_Anomaly_900` | Golganneth's Tempest | TavernRefresh | Implemented |
| `BG31_Anomaly_120` | Scout's Honor | GeneratedMinion | Implemented |
| `BG27_Anomaly_503` | The Yogg-iseum | SinglePlayerChoice | Implemented |
| `BG27_Anomaly_572` | Treasure Hoard | DelayedReward | Implemented |
| `BG27_Anomaly_570` | Treasure Hoard | DelayedReward | Implemented |
| `BG27_Anomaly_571` | Treasure Hoard | DelayedReward | Implemented |

## Corrected Notes

- Darkmoon Prize is no longer a 24-proxy blocker for current anomalies. The current
  `darkmoonPrizes.json` catalog has 33 prizes and all 33 are `Implemented`.
- Audience's Choice and The Yogg-iseum are accepted single-player trainer adaptations, not
  official shared-lobby voting implementations.
- Scout's Honor still uses a generated Patient Scout proxy. This is a fidelity polish item, not a
  current-pool runtime blocker.
- Bring in the Buddies and Cosmic Duality would benefit from clearer candidate implementation
  status reporting.
- The historical/all-known 111-anomaly target is a future product/data scope. It is not a current
  default-flow runtime gap.

## Future Scope

Historical or all-known anomaly support should be treated as a separate product decision:

1. Import historical anomaly data with card ids, dbf ids, source pools, implementation status, and
   availability reasons.
2. Keep `CurrentHsReplay` as the default pool.
3. Add a start-game anomaly pool selector only after historical data is classified.
4. Keep historical/all-known entries behind explicit opt-in setup.

## Validation

Use the current audit document for detailed validation routes. The usual focused check for
current-pool metadata changes is `AnomalySystemTests`.
