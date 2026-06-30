# Unimplemented Battlegrounds Anomalies Production Plan

Date: 2026-06-29

## Current Baseline

Current catalog: `Assets/LearnHearthstone/Resources/Data/battlegroundsAnomalies.json`

- Total current HSReplay anomalies: 28
- Implemented: 28
- Planned but not implemented: 0
- Blocked by dependency: 0

Implemented anomalies should stay in the default random pool only when `ImplementationStatus == Implemented` and all availability gates pass. Planned or dependency-blocked anomalies must remain selectable only by explicit/debug flows until their prerequisites and focused tests are complete.

## Remaining Anomalies

| Card ID | Name | Family | Current Status | Main blocker |
| --- | --- | --- | --- | --- |
| `BG35_Anomaly_001` | Fly the Flag | GeneratedSpell | Implemented | Completed |
| `BG34_Anomaly_805` | Oathstone's Summoning | Timewarp | Implemented | Completed |
| `BG27_Anomaly_504` | Secrets of Norgannon | MinionPool | Implemented | Completed |
| `BG31_Anomaly_123` | Cosmic Duality | SecondHeroPower | Implemented | Completed |
| `BG35_Anomaly_005` | Anomalous Timeline | SecondHeroPower | Implemented | Completed |
| `BG32_Anomaly_001` | Greater Pouches | SecondHeroPower | Implemented | Completed |
| `BG35_Anomaly_007` | Lesser Fortune | SecondHeroPower | Implemented | Completed |
| `BG32_Anomaly_002` | Lesser Pouches | SecondHeroPower | Implemented | Completed |
| `BG35_Anomaly_004` | Anomalous Conflux | SecondHeroPower | Implemented | Completed |
| `BG35_Anomaly_002` | Anomalous Cube | SecondHeroPower | Implemented | Completed |
| `BG35_Anomaly_008` | Greater Fortune | SecondHeroPower | Implemented | Completed |
| `BG31_Anomaly_106` | Marin's Treasure Box | HeroReplacement | Implemented | Completed |
| `BG27_Anomaly_Prizes2` | Darkmoon Faire Prizes | DarkmoonPrize | Implemented | Completed |
| `BG27_Anomaly_716` | Up-Prizing | DarkmoonPrize | Implemented | Completed |
| `BG27_Anomaly_810` | Bring in the Buddies | Buddy | Implemented | Completed |
| `BG27_Anomaly_580` | Audience's Choice | SinglePlayerChoice | Implemented | Completed |
| `BG27_Anomaly_503` | The Yogg-iseum | SinglePlayerChoice | Implemented | Completed |

## Implementation Order

### Batch 1: Pool and Spell Anomalies

Goal: finish the three non-shared current-pool anomalies that can be built on existing match/shop systems.

1. `BG35_Anomaly_001` Fly the Flag
   - Generate a targeted spell every 3 turns.
   - Spell target must resolve to a legal buyable minion definition.
   - Add exactly 12 copies to the Tavern pool, not the player's private pool.
   - Reject tokens, non-minions, disabled pool entries, and unavailable tribes with a recruit log entry.
   - Tests: turn cadence, target validation, +12 pool count, refresh can offer injected copies, buy/sell/triple returns use normal pool semantics.

2. `BG34_Anomaly_805` Oathstone's Summoning
   - On turn 7, add Minor Timewarped minions to Tavern offerings/pool.
   - On turn 10, add Major Timewarped minions.
   - Use only minion definitions; do not inject Timewarped spells, exit cards, or blocked non-minion support cards.
   - Preserve current vs historical Timewarped pool rules.
   - Tests: before/after turn 7 and 10, minion-only injection, no historical leakage when disabled, refresh can surface injected minions.

3. `BG27_Anomaly_504` Secrets of Norgannon
   - Raise Tavern tier cap to 7.
   - Add 10 armor at match start.
   - Include legal Tier 7 minions in shop, discover, Patient Scout, and triple reward paths.
   - Tier 7 pool count should be 5 per legal minion unless local data explicitly says otherwise.
   - Tests: armor grant, upgrade to tier 7, shop generation, discover generation, tribe filtering, no tier 7 when anomaly disabled.

Exit criteria:
- `AnomalySystemTests` has focused coverage for all three.
- `MatchServiceTests` still passes.
- Catalog statuses move from `Planned` to `Implemented` only after tests pass.

### Batch 2: Second Hero Power Foundation

Status: Completed.

Goal: implement one reusable second-hero-power path before enabling eight related anomalies.

Foundation work:
- Make `ExtraHeroPowerCardIds` a first-class command/UI path, not just state storage.
- `UseHeroPower` must be able to target a specific hero power card id.
- Cost, lock/unlock round, once-per-turn counters, passive powers, and logs must be separated per hero power.
- UI must display multiple hero powers without replacing the primary hero power.
- Add helper API: `GrantSecondHeroPower(cardId, source, unlockRound = 1)`.

Then enable:
- `BG35_Anomaly_005` Anomalous Timeline
- `BG32_Anomaly_001` Greater Pouches
- `BG35_Anomaly_007` Lesser Fortune
- `BG32_Anomaly_002` Lesser Pouches
- `BG35_Anomaly_004` Anomalous Conflux
- `BG35_Anomaly_008` Greater Fortune
- `BG35_Anomaly_002` Anomalous Cube, with turn-5 unlock
- `BG31_Anomaly_123` Cosmic Duality

Tests:
- primary hero power remains usable;
- each fixed second hero power is visible and usable;
- delayed unlock works for Anomalous Cube;
- Cosmic Duality offers legal hero powers and stores the selected card id;
- blocked second-hero-power anomalies stay out of the default random pool until the foundation is complete.

### Batch 3: Marin Hero Replacement

Goal: implement `BG31_Anomaly_106` after Batch 2.

Implementation:
- Replace all heroes with Marin when the anomaly is active, or use a single-player proxy that replaces only the local player while clearly logging the proxy.
- Grant Growing Collection as the second hero power through the Batch 2 foundation.
- Ensure armor/health/hero power metadata comes from the resolved Marin hero definition.

Tests:
- selected hero becomes Marin or documented proxy Marin;
- Growing Collection appears as second hero power;
- primary/secondary hero power usage and UI remain stable;
- default random pool includes this anomaly only after exact behavior is supported.

### Batch 4: Darkmoon Prize Backend

Status: Completed.

Goal: build a shared Darkmoon Prize backend, then enable prize anomalies.

Foundation work:
- Add `DarkmoonPrizeCatalog` with tiers, card ids, text, implementation status, and source pool.
- Add `DarkmoonPrizeEngine` for discover, generated prize cards, and immediate prize resolution.
- Migrate existing Timewarped Big Winner prize handling to the shared backend instead of maintaining a local branch.

Then enable:
- `BG27_Anomaly_Prizes2` Darkmoon Faire Prizes
- `BG27_Anomaly_716` Up-Prizing

Tests:
- prize catalog loads with tier grouping;
- every offered prize has an implemented or explicit proxy handler;
- Darkmoon Faire Prizes triggers every 4 turns;
- Up-Prizing scales prize tier after tavern upgrades according to its rule;
- Big Winner still works through the shared backend.

### Batch 5: Buddy Backend and Bring in the Buddies

Goal: implement the user-refined buddy rule correctly instead of mixing buddies into the normal minion pool.

Status: Completed.

Foundation work:
- Add an independent `BuddyPool` or a distinct pool layer/source equivalent.
- Add a discoverable-buddy predicate.
- Add hero-to-buddy mapping validation.
- Define buy/sell/triple return rules for BuddyPool copies.

Enable:
- `BG27_Anomaly_810` Bring in the Buddies

Rules:
- Only discoverable buddies enter the pool.
- Add 6 copies per eligible buddy.
- Do not mix BuddyPool counts into normal minion pool counts.
- Generated/copy buddies should not return to BuddyPool unless they were purchased from it.

Tests:
- eligible buddy count is exactly 6 each;
- shop can offer buddy cards;
- buy/sell/triple returns affect BuddyPool only;
- generated/copy buddies do not return unless they carry BuddyPool copies.

### Batch 6: Single-Player Start/End Turn Choices

Goal: implement the former shared-lobby anomalies as fully supported single-player Tavern choices.

Status: Completed.

1. `BG27_Anomaly_580` Audience's Choice
   - At the start of the player's turn, offer two legal reward options.
   - Store the selected option for the current turn.
   - At end of turn, grant the selected reward.
   - Mark as `Implemented` once the start-of-turn choice and end-of-turn grant are covered by focused tests.

2. `BG27_Anomaly_503` The Yogg-iseum
   - Build or reuse a Yogg wheel reward backend.
   - At the start of the player's turn, offer two Yogg reward options.
   - Store the selected option for the current turn.
   - At end of turn, resolve the selected Yogg reward.
   - Mark as `Implemented` once the start-of-turn choice and end-of-turn reward are exact enough for default random selection.

Tests:
- start-of-turn choice opens with exactly two legal options;
- choosing an option stores it without granting immediately;
- end-of-turn grants the selected reward exactly once;
- no reward is granted when no option was selected;
- Yogg reward results are seed-stable where randomness is involved;
- default random pool includes these anomalies only after the full single-player choice flow is implemented.

## 已实现畸变的遗留缺陷与依赖风险

本节记录的是“畸变入口已经进入 `Implemented`，但背后的卡牌、池子或子系统仍存在代理、近似实现、单人规则改写或逐项效果未覆盖”的问题。它们不等同于畸变入口未完成，但会影响实机体验的精确度。后续如果要把畸变机制从“默认可玩”推进到“接近官方逐项还原”，应按本节继续拆任务和补测试。

### 1. 暗月奖品后端仍有大量 Proxy 奖品

影响畸变：
- `BG27_Anomaly_Prizes2` Darkmoon Faire Prizes
- `BG27_Anomaly_716` Up-Prizing

当前实现：
- 畸变触发节奏已经完成：Darkmoon Faire Prizes 每 4 回合发现奖品，Up-Prizing 在升级酒馆后发现奖品，并随时间提高奖品等级。
- `DarkmoonPrizeCatalog` 和 `DarkmoonPrizeEngine` 已经存在，奖品发现、生成牌、标签、来源和测试入口都已经接入。
- `Assets/LearnHearthstone/Resources/Data/darkmoonPrizes.json` 中共有 33 个暗月奖品，其中 3 级 8 个和 1 级 `Pocket Change` 已为 `Implemented`，1 级、2 级、4 级还剩 24 个仍为 `Proxy`。
- `DarkmoonPrizeEngine.CreatePrizeCard()` 会给非 `Implemented` 奖品添加 `darkmoon_prize_proxy` 标签。
- `AnomalySystemTests` 中 `AssertDarkmoonPrizeDiscover(..., expectProxy: true)` 明确允许暗月奖品池在逐步去 Proxy 期间同时出现已实现奖品和代理奖品。

缺陷性质：
- 这不是畸变调度缺陷，而是奖品卡牌效果未逐张实现。
- 玩家能看到并选择奖品，但 1/2/4 级奖品大多只是带文本和标签的代理牌，不能保证释放后产生官方效果。
- 受影响范围直接覆盖两个默认池畸变，也会影响任何复用暗月奖品池的其它机制，例如 Tickatus/Big Winner 类链路。

实施建议：
1. 先按等级补齐 1 级和 2 级奖品，因为它们在对局前中期出现频率最高。
2. 每实现一个奖品，将 `implementationStatus` 从 `Proxy` 改为 `Implemented`，并在 `DarkmoonPrizeEngine` 或 `MatchService` 中加入明确解析逻辑。
3. 为每个奖品加一条最小行为测试：生成、可选、使用后效果、日志或状态变化。
4. 最后补 4 级奖品，重点处理长期光环、刷新规则、经济规则和多重发现类奖品。

验收标准：
- `darkmoonPrizes.json` 中不再有默认可发现奖品处于 `Proxy`。
- 暗月奖品发现测试不再需要 `expectProxy: true`。
- 使用奖品后不再出现 `darkmoon_prize_proxy` 标签。

### 2. Scout's Honor 的 Patient Scout 是代理随从（视为一张法术，能使友方随从变成金色，（卖出时回归酒馆逻辑注意））

影响畸变：
- `BG31_Anomaly_120` Scout's Honor

当前实现：
- 畸变开局会给玩家一个金色 Patient Scout。
- 当前代码通过 `CreateProxyMinion()` 生成 Patient Scout，然后移除 `trinket_proxy` 并添加 `anomaly_proxy`。
- Patient Scout 的核心行为已经有测试覆盖：出售后发现随从，并且发现等级会随回合提高；金色状态下应按金色逻辑执行。

缺陷性质：
- 当前 Patient Scout 不是从正式随从卡牌定义、正式池子和正式效果脚本创建，而是一个手写代理对象。
- 如果后续需要完整还原官方 Patient Scout，包括池子来源、三连、图像、关键词、文本、本地化和其它系统交互，应把它升级为正式随从定义或正式生成牌定义。

实施建议：
1. 在随从数据中补充 Patient Scout 的正式定义，或新增一个明确的 anomaly-generated minion 数据源。
2. 用正式定义替换 `CreateProxyMinion()` 路径。
3. 保留现有出售发现和等级成长测试，再增加“不是 proxy 标签”“卡牌元数据正确”“金色复制/三连交互稳定”的测试。

验收标准：
- Patient Scout 不再带 `anomaly_proxy`。
- Scout's Honor 测试仍通过，并能验证正式卡牌定义字段。

### 3. Audience's Choice 和 The Yogg-iseum 是单人规则改写

影响畸变：
- `BG27_Anomaly_580` Audience's Choice
- `BG27_Anomaly_503` The Yogg-iseum

当前实现：
- 按当前项目规则，这两个畸变不再做共享大厅投票。
- 单人酒馆中，玩家在回合开始从 2 个选项里选择 1 个，回合结束获得或结算该选项。
- 测试已覆盖：回合开始出现两个选项、选择后不立即发奖、回合结束只发一次、未选择则不发奖、Yogg 奖励在随机种子下稳定。

缺陷性质：
- 这是有意的产品规则调整，不是 Bug。
- 但它不是官方共享大厅原版。如果未来目标改成多人/共享大厅，则当前实现只能作为单人代理版本。

实施建议：
1. 保留当前单人实现作为 Trainer 默认行为。
2. 如果未来引入多人或模拟全大厅，需要新增 lobby choice state，记录每个玩家的选择和聚合结果。
3. 区分文档和日志文案：当前状态应描述为 `Single-player adapted`，不要称为官方共享投票完整实现。

验收标准：
- 单人模式继续按当前规则稳定工作。
- 如果新增大厅模式，应有独立测试覆盖多玩家投票、平票、未选择玩家、结算时机和奖励一致性。

### 4. 饰品相关畸变的复制/候选池过滤需要按官方规则细化

影响畸变：
- `BG32_Anomaly_001` Greater Pouches
- `BG32_Anomaly_002` Lesser Pouches
- `BG35_Anomaly_002` Anomalous Cube
- `BG35_Anomaly_007` Lesser Fortune
- `BG35_Anomaly_008` Greater Fortune
- `BG31_Anomaly_106` Marin's Treasure Box

当前实现：
- 第二英雄技能基础链路已经完成：授予、展示、解锁、指定英雄技能使用、拖拽/点击命令流都有测试覆盖。
- Greater/Lesser Pouches、Lesser/Greater Fortune、Anomalous Cube 和 Marin 的 Growing Collection 都能触发对应饰品选择或复制流程。
- 当前候选池会按基础可用性过滤：`Implemented`、offer pool 状态、当前种族可用性、已装备饰品排除等。
- Mystery Cube 的英雄技能选择会额外排除 Mystery Cube 自身，避免自己选自己。
- Lesser/Greater Crystal Ball 当前会在玩家首次购买对应等级饰品时，把英雄技能转换成该饰品副本。

缺陷性质：
- 实际酒馆里并不是所有饰品都应该进入这些“选择/替换/复制”路径；部分饰品应从复制池或特定英雄技能候选池中排除。
- 当前实现还缺一套明确的官方排除清单，例如 `不可被 Crystal Ball 复制`、`不可被 Mystery Cube 替换成英雄技能饰品`、`不可作为某类额外饰品候选`。
- `ProxySafe` 仍是效果精确度风险，但它不是这里最核心的规则问题；核心问题是候选池和复制触发范围可能比官方更宽。

实施建议：
1. 在饰品数据里增加更细的资格字段或标签，例如 `copyEligible`、`crystalBallEligible`、`mysteryCubeEligible`、`extraTrinketChoiceEligible`。
2. 为这些畸变拆出独立过滤函数，不要全部复用普通饰品选择池：`IsTrinketEligibleForCrystalBallCopy()`、`IsTrinketEligibleForMysteryCubeHeroPowerChoice()`、`IsTrinketEligibleForExtraTrinketChoice()`。
3. Crystal Ball 的触发逻辑需要先检查被购买饰品是否允许复制；不允许复制的饰品应正常装备，但不应让 Crystal Ball 变成它。
4. 增加候选池调试报告，列出被排除饰品和排除原因，方便之后按官方规则校正。

验收标准：
- 官方不应进入复制/替换路径的饰品不会出现在对应候选池中。
- 不可复制饰品被购买后，不会触发 Crystal Ball 转换。
- 可复制饰品仍能正常触发 Crystal Ball 或 Mystery Cube，并且复制后的效果与原饰品一致。

### 5. 时空相关畸变只应追踪时空随从精确度

影响畸变：
- `BG34_Anomaly_805` Oathstone's Summoning
- `BG35_Anomaly_005` Anomalous Timeline
- `BG35_Anomaly_004` Anomalous Conflux

当前实现：
- Oathstone's Summoning 会在第 7 回合注入当前 Minor Timewarped 随从，在第 10 回合注入当前 Major Timewarped 随从。
- 代码通过 `OathstoneTimewarpedMinionDefinitions()` 明确过滤 `CardKind == Minion`，只把时空随从加入普通酒馆池。
- 时空效果牌、时空法术、出口牌等非随从内容不进入 Oathstone 的酒馆注入池；这符合当前实际酒馆规则，不应视为缺陷。
- 当前池与 historical 池的隔离、只注入随从、不注入非随从内容等规则都有测试覆盖。
- 剩余需要追踪的是：进入池子的时空随从本身，是否拥有足够精确的战吼、亡语、回合开始、回合结束、战斗开始等效果实现。

缺陷性质：
- 不应把“时空效果牌没有进入酒馆”记录成问题；它们本来就不该进入 Oathstone 注入池。
- 真正的问题是时空随从效果是否逐张精确：某些复杂时空随从可能仍依赖通用模板、关键词支持或代理路径。
- Anomalous Timeline 和 Anomalous Conflux 如果后续打开类似时空选择，也应沿用“只进入合法时空随从”的池子规则。

实施建议：
1. 保持 Oathstone 的候选池只使用时空随从，不要把时空效果牌加入普通酒馆。
2. 给时空随从增加逐张实现状态，例如 `Exact`、`TemplateSupported`、`ProxySupported`、`DataOnly`。
3. 优先补当前 Minor/Major 时空随从的具体效果测试，historical extra 可以后置。
4. 对需要代理的时空随从保留明确标记，避免误判为完整官方实现。

验收标准：
- Oathstone 注入池始终只包含合法时空随从。
- 当前 Minor/Major 时空随从至少有明确实现级别和对应测试。
- 非随从时空效果牌不进入普通酒馆池，并有测试防止回归。

### 6. 伙伴畸变的池子规则正确，剩余风险是入池伙伴自身效果

影响畸变：
- `BG27_Anomaly_810` Bring in the Buddies
- `BG31_Anomaly_123` Cosmic Duality

当前实现：
- Bring in the Buddies 已实现独立 BuddyPool：只放入可发现伙伴，每个伙伴 6 份，购买、出售、三连回池都按 BuddyPool 处理。
- 入池逻辑使用 `DiscoverableBuddyDefinitions()`，会排除 `ExcludedFromBuddyDiscover` 的伙伴；也就是说，只有伙伴发现池允许出现的伙伴才会加入酒馆。
- Cosmic Duality 已实现开局发现第二英雄技能，并通过第二英雄技能 UI/命令流使用。
- `HeroEffectImplementationRegistry` 中并非所有英雄技能和伙伴效果都是 `Implemented`。当前还存在 `FrameworkFirst`、`Planned`、`Deferred`、`Unregistered` 等状态。

缺陷性质：
- 伙伴池规则本身不是问题：非伙伴发现池的伙伴不应进入酒馆，当前实现方向正确。
- 剩余问题是入池伙伴买到以后，其伙伴效果是否完整实现；部分伙伴仍可能依赖 `HeroEffectImplementationRegistry` 中的 `FrameworkFirst` 或 `Planned` 状态。
- Cosmic Duality 是另一条英雄技能发现链路：它的问题不是 BuddyPool，而是被发现的英雄技能自身可能未完整实现。

实施建议：
1. 保持 `DiscoverableBuddyDefinitions()` 只使用伙伴发现池，不要把不可发现伙伴加入 BuddyPool。
2. 给 BuddyPool 候选项增加实现状态报告：哪些伙伴效果是 `Implemented`，哪些只是 `FrameworkFirst` 或 `Planned`。
3. 优先补齐已能进入 BuddyPool 的高频伙伴效果，而不是扩大伙伴池。
4. Cosmic Duality 另行增加英雄技能候选项状态报告，避免玩家选到完全未实现的英雄技能。

验收标准：
- Bring in the Buddies 的酒馆池只包含伙伴发现池允许出现的伙伴。
- BuddyPool 候选能报告伙伴效果实现状态。
- Cosmic Duality 的候选英雄技能列表能报告每个选项的实现状态，未实现项不应静默伪装成完整可用。

### 推荐后续实施优先级

1. 暗月奖品 1/2/4 级继续去 Proxy：`Pocket Change` 已完成，剩余 24 个奖品仍影响两个畸变，缺口清晰，测试边界明确，收益最高。
2. 饰品复制/候选池排除清单：先明确哪些饰品不能被 Crystal Ball、Mystery Cube 或额外饰品选择路径复制/替换。
3. Cosmic Duality 和 Bring in the Buddies 的候选项实现状态标记：避免玩家选到看似可用但实际未完成的英雄技能/伙伴。
4. Scout's Honor 的 Patient Scout 正式卡牌化：范围小，适合作为生成随从从 proxy 转正式定义的样板。
5. 时空随从逐张状态分级：保持只进随从的池子规则，再按当前池高频牌推进具体效果。
6. Audience's Choice 和 The Yogg-iseum 多人共享大厅版：只有未来需要多人大厅时才做；当前单人规则已经满足项目需求。

## Status Update Rules

- `Planned` -> `Implemented`: only after behavior is exact enough for default random pool and focused tests pass.
- `BlockedByDependency` -> `Implemented`: only after the dependency is complete and the specific anomaly test passes.
- `BlockedByDependency` -> `DebugOnly`: acceptable for temporary proxies, but must remain out of default random selection.
- `availabilityReasons` should be removed only when the dependency is genuinely gone.

## Recommended Test Runs Per Batch

Always run:
- `AnomalySystemTests`
- `MatchServiceTests`

Run additionally:
- Batch 1: tier/shop/discover related tests if touched.
- Batch 2 and 3: hero power and Unity trainer view tests.
- Batch 4: Darkmoon prize tests plus Timewarped tests that use Big Winner.
- Batch 5: buddy catalog/pool tests once added.
- Batch 6: single-player choice tests plus deterministic seed tests.
