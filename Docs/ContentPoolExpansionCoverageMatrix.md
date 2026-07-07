# Content Pool Expansion Coverage Matrix

This matrix records the current implementation slice for the content-pool expansion plan. Anomaly historical-pool playability is intentionally deferred.

| Area | Current status | Runtime entry | Focused coverage | Notes |
| --- | --- | --- | --- | --- |
| Timewarped high-impact combat minions | Implemented and second precision slice verified | `CombatEngine` timewarped start, deathrattle, avenge, rally, summon, damage, copy, and refresh branches | `MatchServiceTests`, `TimewarpedHistoricalImplementationTests` 13/13 | Bassgill/Mrrrglr-style hand pressure, Red Whelp/Ragnaros damage, Stoneshell/Guard rally, Henchman copy, Whirl-O-Tron deathrattle copy, and current-pool reward loops are covered. Precision slices now include Timewarped Deathswarmer, which immediately buffs friendly Undead during combat and still queues permanent Undead attack, plus Timewarped Kil'rek, whose deathrattle now queues `AddRandomDemonToHand` from `CombatEngine` while avoiding duplicate player-side service handling. Post-Kil'rek full EditMode passed: 1035 total, 1034 passed, 0 failed, 1 skipped. |
| Timewarped high-impact non-minions | Implemented | `MatchService` timewarped purchase, discover, turn-start, refresh, and direct-cast branches | `MatchServiceTests` | Big Winner, Evolving Tavern, New Recruit, Rat in a Cage, Cloning Device, Conch, Goldenizer, Thief/Master Thief, Revelation, Beanstalk, and hero-power spell cards already have focused coverage. |
| Quest/Trinket start-of-combat stacking | Implemented | `PrepareTrinketCombatStartEffects`, `PrepareQuestCombatStartEffects`, `CombatEngine` next-combat board buff | `QuestTrinketInteractionTests.StartOfCombatQuestAndTrinketBoardBuffsStackOnCombatSnapshot` | Locks the current order and confirms recruit-board state is not permanently mutated by combat-only buffs. |
| Quest/Trinket deathrattle repeat interaction | Implemented | `CombatEngine.GetDeathrattleRepeats` plus first-only trinket repeat state | `QuestTrinketInteractionTests.QuestAndTrinketDeathrattleRepeatsStackWithoutReusingFirstOnlySource` | Confirms quest repeat applies broadly while trinket first-only repeat is consumed once. |
| Quest/Trinket avenge interaction | Implemented | `ResolveQuestAvenge`, trinket avenge counters | `QuestTrinketInteractionTests.QuestAndTrinketAvengeRewardsConsumeSameDeathsIndependently` | Confirms shared death events can feed both systems without suppressing either reward. |
| Quest summon plus trinket summon modifiers | Implemented | `ResolveQuestAvenge`, `AddToken`, `ResolveFriendlySummonTriggers`, `ApplyTrinketCombatSummonModifiers` | `QuestTrinketInteractionTests.QuestSummonedMinionsReceiveTrinketAndQuestSummonModifiers` | Confirms quest-created combat summons receive both quest summon stats and trinket keyword modifiers. |
| Opponent reward isolation | Implemented | `CombatEngine` side rewards, `MatchService.ApplyCombatRewards` player-only application | `QuestTrinketInteractionTests.OpponentCombatRewardsDoNotApplyToPlayerRecruitState` | Opponent combat rewards are visible in the combat result but do not mutate the player's recruit state. |
| Darkmoon first-batch direct prizes | Implemented | `DarkmoonPrizeEngine`, `TavernSpellEngine`, `MatchService` prize helpers | `DarkmoonPrizeSystemTests` | Fresh Tab, Banana Bunch, Gacha Gift, On the House, Mageroyal Blossom, Unfurled Codex, Might of Stormwind, Rat in a Cage, The Bouncer, and Give a Dog a Bone are covered. |
| Darkmoon persistent and cross-system prizes | Implemented | Darkmoon persistent counters and shared prize execution paths | `DarkmoonPrizeSystemTests`, `MatchServiceTests` | Good Stuff, Rocking and Rolling, New Recruit, Crystallization, Evolving Tavern, Time Thief, Raise the Stakes, Gorgeous Goblet, Big Brann Play, Open Bar, Big Winner, and related ordering-sensitive paths are covered. |

## Deferred

| Area | Reason |
| --- | --- |
| Anomaly historical-pool playability | Out of scope for this implementation slice. |
| Full official data parity for every Timewarped card | Current target is high-impact playable precision, not complete official parity. |
| Additional darkmoon edge ordering beyond existing P0/P1/P2 tests | Current Darkmoon coverage already exceeds the first-batch target; future work should be driven by failing scenarios or design needs. |
