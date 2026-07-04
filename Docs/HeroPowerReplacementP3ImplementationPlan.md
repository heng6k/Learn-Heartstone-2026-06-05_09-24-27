# P3 英雄技能轮换、临时技能与替换实现文档

Date: 2026-07-04

## 目标

把 P3 这组机制做成一套可执行的实现方案，而不是分别在 Rat King、Master Nguyen、Cosmic Duality、Genn 里写临时分支。范围包括：

- Rat King 的每回合类型轮换、指定类型发现、Pigeon Lord 免费刷新。
- Master Nguyen 的每回合临时英雄技能选择、回合结束恢复、Lei Flamepaw 的 Buddy 映射。
- Cosmic Duality / Cosmic Reward / Timewarped 第二英雄技能的候选过滤和可替换槽位。
- Genn 的 Turn 4 两个英雄技能替换，以及 Cosmic Duality 局禁用。

## 官方/API依据

本轮先查了官方 Hearthstone 站点 API 和 Battle.net Hearthstone Game Data API 文档入口。`.env` 中没有 Battle.net OAuth 凭据，因此不能调用需要 OAuth 的 Battle.net Game Data API；但官方站点 API 可直接返回 Battlegrounds 卡牌文本。

| 对象 | 官方/API文本 | 结论 |
| --- | --- | --- |
| A Tale of Kings / Rat King | `<b>Discover</b> a minion of a specific minion type. Swaps type each turn.` | 官方只说明每回合切换类型，不说明使用后切换。按产品语义定为“回合开始切换，使用后不推进”。 |
| Pigeon Lord | `Your Refreshes are free while the Tavern doesn't have the minion type of your Hero Power.` | 免费刷新只看酒馆是否没有当前英雄技能类型。 |
| Power of the Storm / Master Nguyen | `At the start of every turn, choose from 2 new Hero Powers.` | 每回合开始二选一临时英雄技能。 |
| Lei Flamepaw | `At the start of your turn, get the <b>Buddy</b> of your Hero Power.` | 官方文本没有说明与 Nguyen 同时回合开始时的先后顺序。 |
| King of Duality / Genn | `On Turn 4, Discover two Hero Powers to replace this.` | 两个英雄技能替换 `King of Duality`；局内与额外技能冲突按项目策略处理。 |
| Cosmic Duality | `At the start of the game, <b>Discover</b> a second Hero Power.` | Cosmic Duality 是第二英雄技能来源；Genn 在该畸变下按项目语义禁用。 |
| Cosmic Reward | `<b>Discover</b> a second Hero Power.` | 与 Cosmic Duality 共用第二英雄技能候选策略。 |

来源：

- Official Hearthstone API, key Hero/Buddy cards: `https://hearthstone.blizzard.com/en-us/api/cards?locale=en_US&ids=63127,77843,71909,77514,129685,129684&gameMode=battlegrounds`
- Official Hearthstone API, Chinese localized key cards: `https://hearthstone.blizzard.com/zh-cn/api/cards?locale=zh_CN&ids=63127,77843,71909,77514,129685,129684&gameMode=battlegrounds`
- Official Hearthstone API, Cosmic Duality: `https://hearthstone.blizzard.com/en-us/api/cards?locale=en_US&textFilter=Cosmic%20Duality&gameMode=battlegrounds`
- Official Hearthstone API, Cosmic Reward: `https://hearthstone.blizzard.com/en-us/api/cards?locale=en_US&ids=122924&gameMode=battlegrounds`
- Battle.net Hearthstone Game Data API docs entry: `https://develop.battle.net/documentation/hearthstone/game-data-apis`

置信度：

- 卡牌文本：高。来自官方 API。
- Nguyen 与 Lei 的同回合开始顺序：中低。官方/API 没有给触发排序，只能做项目规则。
- Cosmic Duality 下禁用 Genn、额外技能可被替换：中。来自产品语义，不是官方 API 明文。

## 已确认项目语义

1. Rat King 在每回合开始时随机切换当前随从类型；英雄技能使用后不额外推进类型。
2. Rat King 发现的是使用当时当前池里符合该类型的候选；如果 Buddy 已因特殊规则加入对应池，则可自然被发现，不需要 Rat King 私有 Buddy 分支。
3. Rat King 的当前类型要显示在英雄技能候选 UI 中。Nguyen 看到 `A Tale of Kings` 时，玩家必须知道它当前对应哪个类型。
4. Genn 在 Cosmic Duality 畸变中禁用，不进入 Cosmic Duality 第二英雄技能候选。
5. Cosmic Duality / Cosmic Reward / Timewarped 已获得的额外英雄技能可以被后续替换规则清理。
6. `Planned` / `FrameworkFirst` / `Deferred` 候选不预先拍死；先按完整清单逐项判定。

## 已决策时序规则

Master Nguyen 和 Lei Flamepaw 都是“回合开始”。官方/API 只提供各自文本，没有说明先后顺序。

本项目采用以下规则，并已按此实现：

1. 回合开始先处理 Nguyen 的 `Power of the Storm`，创建 2 个新英雄技能候选。
2. 玩家选择后，所选技能成为本回合 `TemporaryOverride`。
3. 如果 Lei Flamepaw 在场，则 Lei 的 Buddy 奖励延迟到 Nguyen 选择完成后结算，按本回合临时英雄技能映射 Buddy。
4. 如果 Nguyen 没有可用候选、选择被跳过、或没有成功设置临时技能，则 Lei 回退到选择前的当前英雄技能。

原因：

- Lei 文本是“你英雄技能的 Buddy”。Nguyen 选择完成后，本回合可用英雄技能才是玩家实际拥有/使用的技能。
- 当前项目 `HeroEffectEngine` 的 `Resolve(...)` 先调 `DispatchHeroPower(...)`，再调 `DispatchBuddies(...)`；这天然支持“英雄技能回合开始效果先于 Buddy 回合开始效果”的内部顺序。
- Nguyen 的选择需要玩家输入，不能在同一个同步 `TurnStarted` 调用里立即完成，因此实现上要用 pending continuation，而不是直接在 `ResolveTurnStartedBuddies(...)` 里猜一个顺序。

需要你确认：

- 是否接受“Nguyen 选择后，Lei 按本回合临时技能给 Buddy”作为最终产品规则？

## 共享状态设计

新增或等价扩展英雄技能槽位状态，避免继续只靠 `HeroPowerCardId` 和 `ExtraHeroPowerCardIds` 表达所有情况。

| 状态 | 字段 | 用途 |
| --- | --- | --- |
| `Primary` | `CardId`, `SourceHeroCardId`, `OriginalCardId` | 当前基础英雄技能。 |
| `Extra` | `CardId`, `Source`, `UnlockRound`, `Replaceable` | Cosmic Duality / Cosmic Reward / Timewarped 第二英雄技能。 |
| `TemporaryOverride` | `CardId`, `Source`, `ExpiresAtTurnEnd`, `PreviousPrimaryCardId` | Nguyen 本回合临时技能。 |
| `PendingReplacement` | `Source`, `ReplaceTarget`, `RequiredPicks`, `Options`, `Policy` | Genn Turn 4、将来其他替换技能。 |
| `CandidateStatus` | `ImplementationStatus`, `Eligibility`, `Decision`, `DisplayLabel` | 候选过滤或标注“不完整/代理”。 |

保留兼容：

- `State.Player.HeroPowerCardId` 继续作为主技能入口。
- `State.Player.ExtraHeroPowerCardIds` 继续作为旧 UI/测试兼容列表。
- 新槽位状态落地后，旧字段由槽位状态同步生成，直到所有调用点迁移完成。

## 候选策略

当前普通英雄技能发现入口只取 `HeroPowerReplacementEligibility.DiscoverableAfterStart`。所有来源必须走同一套候选过滤函数：

```text
BuildHeroPowerCandidates(source, currentSlots, policy)
  1. 从 HeroCatalog.GetDiscoverableHeroPowers(...) 取基础候选。
  2. 排除已拥有的 Primary / Extra / TemporaryOverride。
  3. 排除当前 source 禁止项，例如 Cosmic Duality 下排除 Genn。
  4. 合并 HeroEffectImplementationRegistry 状态。
  5. 按用户判定执行：Allow / LabelProxy / Filter。
  6. 为动态技能补显示说明，例如 Rat King 当前类型。
```

### 当前待判别候选

| 状态 | 数量 | 默认建议 | 说明 |
| --- | ---: | --- | --- |
| `Deferred` + `DiscoverableAfterStart` | 1 | 过滤 | Mister Clocksworth / Double Time 依赖 TripleEngine。 |
| `FrameworkFirst` + `DiscoverableAfterStart` | 23 | 显示代理标签 | 有部分可见/代理能力，但不应伪装成完整实现。 |
| `Planned` + `DiscoverableAfterStart` | 6 | 过滤，Rat King 在 P3.2 后允许 | 尚未接运行时；避免玩家选到无效果技能。 |
| `InitialOnly` 未完成 | 7 | 不进普通候选 | Genn 属于此类；Cosmic Duality 下禁用。 |
| `Disabled` 未完成 | 8 | 不进普通候选 | 只有专门来源可显式授予。 |

完整逐项清单已写在 `Docs/HeroEffectIncompleteCompletionPlan.md` 的 “P3 英雄技能候选待判别清单”。

## 实现直线

### P3.0 候选审计和决策表

产出：

- `HeroPowerCandidatePolicy` 或等价配置。
- 每个候选记录 `Allow`、`LabelProxy`、`Filter`。
- Cosmic Duality / Cosmic Reward / Nguyen / Training Session 共用。

验收：

- `Deferred` 候选不能静默进入可用池。
- UI 能显示 `FrameworkFirst` 的不完整/代理标签，或按决策过滤。
- Rat King 完成后显示当前类型。

### P3.1 多槽状态和命令契约

产出：

- 槽位状态与旧字段同步。
- `UseHeroPower` 继续支持指定 `heroPowerCardId`。
- 额外槽、临时槽、锁定槽在 UI 中可区分。

验收：

- 主技能不被额外技能覆盖。
- 额外技能可指定使用。
- 临时技能回合结束恢复。
- 被替换的额外槽从旧列表和新槽位状态中同时清理。

### P3.2 Rat King / Pigeon Lord

产出：

- `RatKingCurrentMinionType` 状态。
- 每回合开始随机切换合法类型。
- `A Tale of Kings` 使用当前类型从当前池发现。
- 候选 UI 显示当前类型。
- Pigeon Lord 判断当前酒馆是否没有该类型，决定刷新费用。

验收：

- 类型只在回合开始切换。
- 禁用种族不进入类型池。
- Discover 使用当时当前池；特殊规则已加入池子的 Buddy 可自然出现。
- 酒馆有当前类型时刷新正常收费，没有时免费。

### P3.3 Master Nguyen / Lei Flamepaw

产出：

- 已实现：回合开始创建 2 个新英雄技能候选。
- 已实现：选择后写入本回合临时英雄技能覆盖。
- 已实现：回合结束恢复 `Power of the Storm`。
- 已实现：Lei Flamepaw 通过统一 HeroPower -> Buddy 映射发牌。
- 已实现：Lei 在 Nguyen 选择完成后按临时技能结算；没有候选时回退当前/旧技能。

实现建议：

```text
TurnStarted
  if Primary == Power of the Storm:
      Queue Nguyen Hero Power choice
      if Lei Flamepaw is present:
          Store pending Lei resolution after Nguyen choice

DiscoverChosen(source == nguyen-hero-power-choice)
  Set TemporaryOverride = selected hero power
  if pending Lei resolution:
      Grant buddy mapped to selected hero power

TurnEnded
  Clear TemporaryOverride
  Restore Power of the Storm as primary visible skill
```

验收：

- Nguyen 选择前不能使用旧临时技能。
- 选择后本回合可用的是临时技能。
- 回合结束恢复。
- Lei 获得的 Buddy 与结算时英雄技能一致。
- Rat King 作为候选时显示当前类型。

### P3.4 Cosmic Duality / Cosmic Reward / Timewarped 第二技能复查

产出：

- 共用候选策略接入 Cosmic Duality 和 Cosmic Reward。
- Genn 在 Cosmic Duality 下过滤。
- Timewarped 固定第二技能继续可授予，但可被后续 replacement 清理。

验收：

- Cosmic Duality 不出现 Genn。
- 选择第二技能不覆盖主技能。
- 被替换时旧额外技能不残留在 UI 或命令路径。

### P3.5 Genn, Worgen King

产出：

- 非 Cosmic Duality 局才允许 Genn。
- Turn 4 触发两个英雄技能选择。
- 两个选择替换 `King of Duality`，并可清理已有 Extra 槽。

验收：

- Cosmic Duality 局不会提供 Genn。
- Turn 4 前显示倒计时或锁定状态。
- Turn 4 后 `King of Duality` 不再可用。
- 两个新技能都可显示和使用。
- Cosmic Duality / Timewarped 旧额外技能按替换规则清理。

## 测试矩阵

| 测试组 | 覆盖点 |
| --- | --- |
| CandidatePolicyTests | 状态过滤、代理标签、Genn under Cosmic Duality、Rat King 当前类型显示。 |
| HeroPowerSlotStateTests | Primary / Extra / TemporaryOverride / PendingReplacement 同步和清理。 |
| RatKingHeroPowerTests | 回合开始随机类型、合法类型过滤、Discover 当前池、Pigeon Lord 免费刷新。 |
| NguyenLeiHeroPowerTests | 回合开始候选、选择后临时技能、Lei 延迟结算、回合结束恢复。 |
| CosmicDualitySecondPowerTests | 第二技能候选策略、Genn 过滤、选择后不覆盖主技能。 |
| GennReplacementTests | Turn 4 双技能替换、旧额外槽清理、回合持久化。 |
| UnityTrainerHeroPowerUiTests | 多技能按钮、临时技能标识、代理/不完整标签、Rat King 类型提示。 |

## 需要你最终确认

1. Nguyen 与 Lei：是否采用“Nguyen 选择后，Lei 按本回合临时技能给 Buddy”。
2. 候选策略：`Planned` / `FrameworkFirst` / `Deferred` 清单里哪些过滤、哪些显示代理、哪些允许。

除这两点外，当前官方/API、本地数据和你已给出的产品语义没有发现新的冲突。
