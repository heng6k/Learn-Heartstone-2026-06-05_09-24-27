# 时空酒馆智能候选机制实现计划

日期：2026-07-05

## 目标

参考已经完成的饰品智能候选机制，为时空酒馆制定一套确定性的智能候选规则。

这份文档只规划候选生成机制，不重新打开已经完成的时空酒馆卡牌效果，也不默认把历史/额外池产品化。

时空酒馆和饰品的关键区别：

- 饰品是一次性的四选一奖励。
- 时空酒馆是一个使用 `Chronum` 购买的临时商店。
- 所以可以复用“先硬过滤，再打分，再分桶，再 deterministic 回填”的框架，但不能照搬饰品的所有运行时边界。

## 当前基线

当前 `GenerateTimewarpedOffers(kind, source)` 的行为是：

1. 通过 `TimewarpedCandidatesForKind(kind)` 取 Minor 或 Major 当前池。
2. 追加同 kind 的已实现当前非随从牌。
3. 当 `timewarpedPoolVersion != Current` 时，才追加历史/额外池。
4. 通过 `IsOfferableTimewarpedDefinition(...)` 排除未支持的非随从。
5. 当 `timewarpRules.RespectActiveTribes` 开启时，通过 `IsTimewarpedCardAvailable(...)` 排除不可用种族。
6. 从合法池里用 deterministic random 抽 `timewarpRules.OfferCount` 张。

本地数据当前稳定计数：

- Minor 当前池：55 张随从。
- Major 当前池：70 张随从。
- 历史/额外池：33 张随从，默认不进入当前池。

当前唯一开放边界仍是 `TW-BDY-002`：历史/额外池是否要变成正式产品模式。

## 设计原则

沿用饰品实现里的分层：

1. 先硬过滤。
2. 只对已经合法的候选打分。
3. 按角色组成 4 个 offer。
4. 任何角色候选不足时，从剩余合法池 deterministic 回填。
5. 同 seed、同状态必须输出同顺序，保证测试稳定。

历史/额外池不能偷偷混入默认当前池。它只改变“合法池”，不改变智能选择算法本身。

## 硬过滤规则

智能 picker 接收到的必须已经是合法候选。合法池应排除：

- 错误 Timewarp kind：
  - Minor visit 不出 Major-only 卡。
  - Major visit 不出 Minor-only 卡。
- 默认 `Current` 模式下的 `poolStatus = historical_extra` 卡。
- `blocked_by_non_minion_support` 等未支持非随从。
- `Exit` 卡，除非明确启用 `timewarpRules.IncludeExitCard`。
- `RespectActiveTribes` 开启时，全部真实种族都不可用的卡。
- 合并 current、non-minion、historical 后重复的 `CardId`。

和饰品不同的一点：

- 饰品的未实现/隐藏/禁用不能出。
- 时空酒馆的当前池随从即使带 `implementation_status:data_only`，也仍可作为“可购买白板/带基础关键字身体”出现。这是当前机制的一部分。
- 严格“未实现不出”应先用于未支持的非随从和未来 unsupported purchase behavior，不应误伤当前池 data-only 随从，除非以后新增一个“只出完整效果卡”的模式。

## 打分输入

第一版只使用和饰品一致的主信号：

| 来源 | 分值 |
| --- | ---: |
| 当前战队每个可用可玩种族 | `+3` |
| 当前手牌每个可用可玩种族 | `+1` |
| 通用/无真实种族候选 | 不参与种族分，只保留通用角色 |

暂不把普通酒馆里的随从计入分数。

原因：时空酒馆是独立商店，不应该因为普通酒馆刚刷新出的临时内容而过度改变 Timewarp 候选方向。后续如果需要，可以单独加“普通酒馆低权重 +1”的二期规则。

## 候选角色

对每张合法候选解析 active tribes 后分为三类：

- `Focus`：至少一个 active tribe 在当前战队/手牌里有正分。
- `Expansion`：有 active tribe，但这些种族当前没有正分。
- `Generic`：没有真实 active tribe，例如无种族、`None`、`All`、通用非随从或功能牌。

`Focus` 内部按最高分优先；同分时先按稳定 `CardId` 排序，再用 seed 打破平局，和饰品 picker 的模式保持一致。

## 4 个 Offer 的组成

默认 `OfferCount = 4` 时，目标组成：

1. 2 个 `Focus`：贴合当前战队/手牌主方向。
2. 1 个 `Expansion`：当前可用但非主方向的扩展选择。
3. 1 个 `Generic` 或功能牌；如果没有，则从剩余合法池随机补。

回填规则：

- Focus 不足 2 个，从剩余合法池补。
- Expansion 不存在，从剩余合法池补。
- Generic/utility 不存在，从剩余合法池补。
- 同一次 offer 不能重复 `CardId`。
- 合法池不足 `OfferCount` 时，有多少出多少。

建议实现形状：

```text
GenerateTimewarpedOffers
  -> Build legal candidates
  -> Build TimewarpedOfferCandidate records
  -> Pick focus/focus/expansion/generic
  -> Deterministic legal fallback
  -> Convert to TimewarpedOfferSlot
```

## Seed 规则

保留当前 seed 形状即可：

```text
State.Seed
+ State.Round * 1741
+ kind discriminator
```

如果未来出现非第 6/9 回合的多来源 Timewarp，可以再加入稳定 source discriminator。

要求：

- 同 seed、同 round、同 kind、同 active tribes、同战队、同手牌、同 pool version，输出顺序完全一致。

## 历史/额外池边界

历史/额外池仍是产品开关：

- 默认当前池不能出 `poolStatus = historical_extra`。
- `TimewarpedPoolVersion.FirestoneAll` 或 `Launch` 开启后，历史候选进入合法池，再走同一套智能选择规则。
- 不为历史模式写第二套 picker。

测试必须覆盖：

- 默认 current 模式不出历史/额外卡。
- 开启历史/额外池后，候选可以进入合法池。
- 开启历史模式不改变 current 默认行为。

## 实现阶段

### Phase 1：抽出候选 helper

在现有 Timewarped offer 代码附近新增内部候选结构：

- `TimewarpedOfferCandidate`

新增 helper：

- `BuildTimewarpedOfferCandidates(...)`
- `CurrentTimewarpedOfferTribeScores(...)`
- `PickBestTimewarpedCandidate(...)`
- `PickRandomTimewarpedCandidate(...)`

复用：

- `TribeAvailabilityRules.PlayableTribes`
- `TribeAvailabilityRules.IsTribeActive(...)`
- 现有 `IsTimewarpedCardAvailable(...)`

验收：

- 不改数据。
- 不改卡牌效果状态。
- 现有 Timewarp offer 测试继续通过。

### Phase 2：替换纯随机 picker

只替换 `GenerateTimewarpedOffers(...)` 里的选择阶段。

保持不变：

- 前置 hard filter。
- `TimewarpedOfferSlot` 输出结构。
- UI 和购买命令入口。
- Chronum 购买逻辑。

验收：

- Minor/Major 合法池足够时仍生成 `OfferCount` 个 slot。
- 同 seed/state 输出相同顺序。
- 打开 Timewarp 不修改普通 `TavernState.Shop`。

### Phase 3：补 focused tests

建议测试放在现有 Timewarped/`MatchServiceTests` 覆盖区域。

测试项：

- 主方向：
  - active tribes 固定为一小组。
  - 战队放多个 Beast。
  - 手牌放一个 Murloc。
  - 打开 Minor，若合法池足够，断言至少 2 个 offer 贴合 Beast/Murloc。
- 扩展：
  - 若合法池有 active 非主方向候选，断言至少 1 个出现。
- 通用/功能：
  - 若合法池有无种族或功能候选，断言至少 1 个出现。
  - 若没有，则断言 deterministic fallback 仍补满。
- 确定性：
  - 两个相同 seed/state 的 service 输出相同 offer id 顺序。
- 硬过滤：
  - inactive tribe-only 不出现。
  - 默认 current 模式不出现 historical_extra。
  - 开启 historical pool 后才允许历史候选进入合法池。

验收：

- focused smart-offer tests 通过。
- 现有 Timewarped P0/P1 focused tests 继续通过。

### Phase 4：文档同步

实现后同步：

- 更新本文件的“当前行为”。
- 只有当历史/额外池产品策略被改变时，才更新 `timewarped-tavern-remaining-completion-status.md` 的 `TW-BDY-002`。
- 不重新打开 `TW-BDY-001` 或 `TW-BDY-003`。

## 验证路线

建议顺序：

1. 新增 focused smart-offer tests。
2. 现有 Timewarped/DarkmoonPrize-focused `MatchServiceTests`。
3. `git diff --check` 检查 touched runtime/test/doc files。
4. 如果怀疑默认全量 EditMode 稳定性，使用 `Tools/run-editmode-bisect.ps1`；不要把未隔离的 broad hang 当成 Timewarped picker 缺陷。

## 风险与决策

| 风险/决策 | 处理 |
| --- | --- |
| `implementation_status:data_only` 容易被误解为未实现 | 当前池随从仍可购买；只排除 blocked non-minion。 |
| 历史/额外模式还没有产品 UI | 保持 switch-gated，不改变默认 current。 |
| Major 的 generic 候选比 Minor 少 | 用 deterministic legal fallback。 |
| 未来可出非随从变多 | 可把无种族非随从视为 Generic/Utility。 |
| 战队种族过强导致过拟合 | 只保留 2 个 Focus，留 1 个 Expansion。 |

## 实现状态

2026-07-05 已按本文档落地运行时实现。

已完成内容：

- `GenerateTimewarpedOffers(...)` 保留原 hard filter，只替换合法池之后的纯随机选择阶段。
- 新增 Timewarped offer candidate helper，按当前战队/手牌种族打分。
- 默认 4 个 offer 按 2 个 `Focus`、1 个 `Expansion`、1 个 `Generic` 或 deterministic legal fallback 组成。
- `implementation_status:data_only` 的当前池随从仍可作为可购买 Timewarped body 出现。
- `TW-BDY-002` 未改变：历史/额外池仍只在对应开关打开后进入合法池，不混入默认 current 流程。

验证结果：

- `Logs/TimewarpedSmartOffer.xml`：新增 focused smart-offer tests 2/2 通过。
- `Logs/TimewarpedSmartOfferRegression.xml`：Timewarped/DarkmoonPrize 相关回归 125/125 通过。
- Unity 编译日志 `Logs/CodexCompileCheck.log` 记录 `ExitCode: 0` 和 `Tundra build success`。
