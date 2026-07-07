# 内容池扩容实现文档

## 目标

在“对局验收场景库、战斗结果解释器、机制覆盖热力表”稳定之后，下一阶段不再优先扩 UI 配置项，而是扩充模拟器真正能理解和执行的内容池。

本轮只做 3 个方向：

1. `timewarped` 高影响单卡精确效果。
2. `quest/trinket` 特殊交互。
3. `darkmoon` 奖品精确度。

`anomaly` 历史池可玩化先不做，只保留为后续阶段。

## 总原则

- 每补一个机制，都必须有可复现局面、解释器信号、focused EditMode 测试。
- 不新增平行系统，优先复用现有 `MatchService`、`CombatEngine`、`TavernSpellEngine`、catalog loader、reward/trigger 状态。
- 不追求一次补完全部内容。每批以“设计师真的会拿来判断阵容强度”的高收益机制为准。
- 任何复杂交互先锁定触发顺序，再实现效果。
- 已有 proxy 可以保留，但必须在覆盖表或注册表里写清楚 `Implemented / Partial / Deferred`。

## 本轮不包含

- Anomaly 历史池可玩化。
- 全官方数据完整校验。
- 多人大厅真实对手行为模拟。
- 大规模 UI 重做。
- 新的通用规则语言或脚本引擎。

## 阶段 1：Timewarped 高影响单卡精确效果

### 目标

这里的“逐张精确效果”就是时空随从、时空酒馆法术、时空英雄技能牌的卡牌效果精确还原。优先补能直接改变战斗结果、成长速度、手牌质量和历史身材的单卡。

### 现有基础

可复用现有入口：

- `TimewarpedTavernCatalogLoader`
- `TimewarpedTavernCardDefinition`
- `TimewarpedCardBehavior`
- `MatchService` 内已有 timewarped purchase / turn start / turn end / battlecry / deathrattle / avenge / rally 分发。
- `CombatEngine` 内已有部分 timewarped 战斗处理。
- `OpponentHistoryState` 和上轮补的设计验收场景可用于历史局面验证。

### 第一批优先卡牌类型

优先级按收益排序：

| 类型 | 要补什么 | 判断标准 |
| --- | --- | --- |
| 战斗直接影响 | 战斗开始、亡语、复仇、召唤、攻击触发、手牌召唤 | 会改变胜负或剩余场面 |
| 成长核心 | 永久身材、商店身材、手牌身材、历史身材 | 能影响后续 2 到 3 回合强度 |
| 经济转战力 | Chronum、金币、刷新、锁手牌、买入施放 | 会影响路线选择和节奏 |
| 生成与发现 | 从指定池、指定等级、指定类型拿牌 | 影响可复现性和池合法性 |
| 英雄技能牌 | casts-when-bought 的第二英雄技能 | 影响战斗和招募阶段长期收益 |

### 建议第一批卡牌清单

从现有补充定义和运行时处理看，第一批建议聚焦这些高影响卡：

| 卡牌/效果 | 重点验收 |
| --- | --- |
| Timewarped Bassgill / Mrrrglr 类 | 手牌召唤、手牌身材转场面压力、对手侧也能生效 |
| Timewarped Big Winner | 暗月奖品发现和每 3 回合重复触发 |
| Timewarped Evolving Tavern | 酒馆升级替换、shop slot 同步 |
| Timewarped New Recruit | 酒馆永久 +2/+2、酒馆 7 格规则 |
| Timewarped Rat in a Cage | 目标加成后翻倍，顺序必须精确 |
| Timewarped Cloning Device / Conch | 复制目标实例、保留/重置哪些字段要明确 |
| Timewarped Goldenizer | 目标变金，三连/金色属性不能破坏 |
| Timewarped Thief / Master Thief | 上局战队历史复制，stats/keywords 规则分开测 |
| Timewarped Revelation / Beanstalk | minor/major 池交叉发现、锁手牌 |
| Power of Tavish / Brukan 等英雄技能牌 | casts-when-bought 后第二英雄技能在战斗里真实生效 |

### 实施步骤

1. 做 Timewarped 单卡状态审计。
   - 输出表：`CardId / Name / EffectIds / PurchaseBehavior / RuntimeHandler / Test / Status / Notes`。
   - 状态只允许：`Implemented`、`Partial`、`Proxy`、`Deferred`。

2. 按第一批清单补 runtime handler。
   - 招募阶段效果优先放在 `MatchService` 已有 timewarped 分发点。
   - 战斗阶段效果优先放在 `CombatEngine` 已有 timewarped 分支。
   - 非随从时空酒馆牌优先复用 `TavernSpellEngine` 或已有 debug-cast 路径。

3. 给每张卡加 focused 测试。
   - 一张卡至少一个正向测试。
   - 有目标选择、手牌上限、池过滤、复制实例的卡，必须加边界测试。

4. 把解释器接上可见信号。
   - 战斗中召唤、亡语、复仇、Rally、CombatSpellCast 要能在解释中出现。
   - 招募阶段效果至少要在 recruit log 中留下可读记录。

5. 更新覆盖热力表。
   - `Timewarped historical minions` 可继续拆成 `combat / recruit / history / hero-power` 四行。

### 测试计划

建议新增或扩展：

- `TimewarpedHighImpactCardTests`
- `TimewarpedCombatEffectTests`
- `TimewarpedHistoryCopyTests`
- `TimewarpedHeroPowerCardTests`
- `DesignValidationToolingTests` 中加入 1 到 2 个代表场景回归

必须覆盖：

- 对手侧手牌召唤类效果。
- 历史战队复制生成新 `InstanceId`。
- 目标变金/复制不破坏场上定位。
- 酒馆永久 buff 在刷新和补牌后继续生效。
- casts-when-bought 英雄技能牌能进入后续战斗。
- 无合法候选时安全 no-op 并写日志。

### 完成标准

- 第一批高影响 timewarped 卡全部有明确状态。
- 至少 8 到 12 张高影响卡达到 `Implemented`。
- 每张 `Implemented` 卡有 focused test。
- 设计验收场景能复现至少 3 类时空强度问题：战斗召唤、历史身材、经济转战力。
- 解释器能指出时空效果对结果的贡献。

## 阶段 2：Quest / Trinket 特殊交互

### 目标

任务和饰品单独可用还不够。真实复杂局面里，可信度主要来自它们和战吼、亡语、复仇、召唤、酒馆法术、英雄技能、timewarped 的交互是否稳定。

本阶段重点不是新增大量任务或饰品，而是补“交互闭环”。

### 交互矩阵

先建立一张矩阵，确认每个交互是否支持：

| 交互 | 重点问题 | 优先级 |
| --- | --- | --- |
| Quest reward + Battlecry repeat | 战吼重复是否吃任务奖励，是否重复过量 | 高 |
| Quest reward + Deathrattle | 亡语触发次数、战斗后奖励回写是否稳定 | 高 |
| Quest reward + Avenge/Rally | 战斗内事件是否转成任务奖励资源 | 高 |
| Trinket + Start of Combat | 和英雄技能、timewarped、战斗光环顺序是否稳定 | 高 |
| Trinket + summoned minions | 召唤物是否吃饰品加成，手牌召唤是否一致 | 高 |
| Trinket + Tavern spell stats | 酒馆法术增益是否影响正确目标和持续时间 | 中 |
| Quest + Trinket shared reward | 同类资源是否重复结算或漏结算 | 高 |
| Quest/Trinket + opponent configuration | 对手侧变量是否只影响战斗侧，不污染玩家永久状态 | 中 |

### 触发顺序建议

需要在文档和测试里固定一个顺序：

1. 回合结束效果。
2. 战斗开始前准备：英雄技能、任务、饰品、timewarped、临时战斗变量。
3. CombatEngine 战斗内触发。
4. 战斗结果和 combat rewards 回写。
5. Quest reward after-combat。
6. Trinket after-combat。
7. 回合开始效果。

如果现有代码顺序不同，先以现有代码为准记录，再决定是否调整。不要在没有测试的情况下改顺序。

### 第一批交互任务

| 任务 | 具体工作 |
| --- | --- |
| 交互审计 | 搜索 `DispatchQuestReward...`、`DispatchTrinket...`、`ApplyCombatRewards`、`Prepare...CombatStartEffects`，列出触发点 |
| 重复触发保护 | 对 battlecry/deathrattle/avenge/rally 的重复来源加 source 记录，避免同一来源重复回写 |
| 召唤物一致性 | 手中召唤、亡语召唤、战斗开始召唤都走同一类 buff/keyword 应用规则 |
| after-combat 回写 | 明确哪些 reward 写玩家永久状态，哪些只写战斗日志或解释器 |
| 对手侧隔离 | 对手侧变量只用于对手战斗快照和保留卡展示，不进入玩家 tavern 状态 |

### 代表验收场景

建议固化为 EditMode 测试：

1. 任务奖励让战吼额外触发，饰品也监听战吼：确认只按预期次数结算。
2. 饰品提供战斗开始召唤，任务奖励监听召唤：确认 combat reward 回写稳定。
3. 亡语触发饰品，饰品再生成手牌：确认战斗后手牌变化可解释。
4. 对手侧有手牌召唤和亡语，玩家侧有任务/饰品：确认对手效果不污染玩家任务进度。
5. Timewarped 召唤和饰品召唤同时存在：确认触发顺序和解释器输出稳定。

### 测试计划

建议新增：

- `QuestTrinketInteractionTests`
- `QuestTrinketCombatRewardTests`
- `QuestTrinketTriggerOrderTests`

必须覆盖：

- 同一战斗事件只被预期系统消费。
- 多来源加成不会重复套到同一 minion。
- 战斗 reward 回写后，下一回合状态正确。
- 解释器能指出任务或饰品是关键贡献来源。

### 完成标准

- 有一张交互矩阵文档或测试夹具。
- 至少 5 个高风险交互有自动测试。
- 任务和饰品同时存在时，完整下一回合流程不红。
- 设计验收场景里能看到“资源转战力”的闭环。

## 阶段 3：Anomaly 历史池可玩化

本阶段延期。

保留后续入口：

- 等 timewarped、quest/trinket、darkmoon 的设计验收稳定后，再做 anomaly。
- 后续目标是按版本和效果家族分组，让历史 anomaly 可选、可解释、可测试。
- 当前不新增 anomaly runtime handler，不扩大 anomaly UI。

## 阶段 4：Darkmoon 奖品精确度

### 目标

Darkmoon 当前不需要重建系统。目标是把已有 `darkmoonPrizes.json`、`DarkmoonPrizeEngine`、`TavernSpellEngine`、Tickatus/Sticker/Timewarped Big Winner 入口继续打磨到“效果准、测试够、解释清楚”。

### 重点不是数量，而是精确度

优先补这几类：

| 类型 | 例子 | 重点 |
| --- | --- | --- |
| 低状态即时效果 | Fresh Tab、Banana Bunch、Gacha Gift | 快速补齐，低风险 |
| 目标型 buff | Rat in a Cage、The Bouncer、Give a Dog a Bone | 目标选择、加成顺序、关键词 |
| 发现类 | On the House、Mageroyal Blossom、Gacha Gift | 候选池、当前等级、ban tribe |
| 持续经济 | Open Bar、Rocking and Rolling、Unlimited Coin | 回合开始/结束回写 |
| 跨系统奖励 | Big Winner、Time Thief、Big Brann Play | Discover queue、历史战队、战吼重复 |

### 第一批建议

从低风险到高风险：

1. Fresh Tab。
2. Banana Bunch。
3. Gacha Gift。
4. On the House。
5. Mageroyal Blossom。
6. Unfurled Codex。
7. Might of Stormwind。
8. Rat in a Cage。
9. The Bouncer。
10. Give a Dog a Bone。

### 第二批建议

1. The Good Stuff。
2. Rocking and Rolling。
3. New Recruit。
4. Crystallization。
5. Evolving Tavern。
6. Time Thief。
7. Raise the Stakes。
8. Gorgeous Goblet。

### 第三批建议

1. Gruul Rules。
2. The Unlimited Coin。
3. Big Brann Play。
4. Friends and Family Discount。
5. Open Bar。
6. Big Winner!。

第三批需要先写触发顺序测试，再实现。

### 实施步骤

1. 审计 `darkmoonPrizes.json`。
   - 输出 `CardId / Tier / Effect / CurrentStatus / RuntimeBranch / Test / Notes`。

2. 每张奖品确认唯一执行入口。
   - 能走 `TavernSpellEngine` 就走 `TavernSpellEngine`。
   - 需要 MatchService helper 的，用明确 effect id 分支，不在 UI 层写逻辑。

3. 更新 implementation status。
   - 真正可打出、可测试才标 `Implemented`。
   - 只有发现链路但效果不准，标 `Partial`。

4. 给每张奖品补 focused test。
   - 即时效果测状态变化。
   - 发现类测候选池。
   - 持续效果测下一回合。
   - 跨系统效果测触发顺序。

5. 接入解释器。
   - 关键奖品触发后，combat/recruit log 中要有可读来源。
   - 战斗奖品应在战斗解释里出现贡献信号。

### 完成标准

- 第一批 10 张奖品都有 focused test。
- 第一批全部达到 `Implemented` 或明确 `Deferred`，不能停在含糊 proxy。
- Tickatus、Ticket Collector、Tickatus Sticker、Timewarped Big Winner 都走同一套奖品发现和执行逻辑。
- 设计验收场景能复现至少 2 个 Darkmoon 改变节奏的局面。

## 全阶段测试策略

每个阶段至少跑：

1. 新增 focused tests。
2. 相邻系统测试：
   - Timewarped：`TimewarpedHistoricalImplementationTests`、timewarped boundary tests。
   - Quest/Trinket：`QuestSystemTests`、`TrinketSystemTests`。
   - Darkmoon：`DarkmoonPrizeSystemTests`、Darkmoon consumers tests。
3. `MatchServiceBattleTestTests`。
4. `CombatMechanicTests`。
5. `git diff --check`。

在 Unity MCP 可用时，阶段结束后跑一次更宽的 EditMode。全量 EditMode 不要求每个小批次都跑，但每个大阶段合并前必须跑。

## 文档和验收产物

每个阶段完成时要留下：

- 机制覆盖表更新。
- 已实现卡牌/交互列表。
- 暂不支持列表。
- focused test XML 或测试日志。
- 至少 1 个设计验收场景。
- 如有 UI 可见变化，补一条手动验收记录。

## 推荐执行顺序

1. Timewarped 单卡审计表。
2. Timewarped 第一批高影响卡实现和测试。
3. Quest/Trinket 交互矩阵。
4. Quest/Trinket 5 个高风险交互测试和修复。
5. Darkmoon 第一批 10 张低风险奖品精确化。
6. Darkmoon 第二批持续效果。
7. Darkmoon 第三批跨系统效果。
8. 回头评估是否启动 anomaly 历史池可玩化。

## 风险和处理

| 风险 | 处理 |
| --- | --- |
| 单卡效果越补越散 | 每张卡必须绑定 catalog 状态、runtime handler、focused test |
| 任务和饰品重复消费同一事件 | 先写触发顺序测试，再实现；必要时记录 source id |
| 玩家侧历史加成重复套用 | 玩家侧继续优先走服务层，CombatEngine 中只对没有服务层保障的一侧补战斗快照 |
| 发现池不合法 | 所有发现类都必须经过当前 card pool、tier、tribe 过滤 |
| Darkmoon 变成第二套 Tavern spell 系统 | 优先复用 `TavernSpellEngine` 和现有 helper，不新建平行执行器 |
| 解释器说不清贡献 | 每个新机制都写 recruit/combat log 来源，并补解释器信号 |
