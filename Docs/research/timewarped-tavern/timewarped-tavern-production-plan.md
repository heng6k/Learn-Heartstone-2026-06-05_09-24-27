# 扭曲时空酒馆制作顺序计划

## 目标

这份文档用于指导 `Learn Hearthstone` 里扭曲时空酒馆的完整制作顺序。范围包括:

- 第 6/9 回合进入 Timewarped Tavern。
- 同回合饰品优先，饰品选完后再打开扭曲时空酒馆。
- Chronum 货币、特殊商店、购买、退出和保存。
- 当前版本 125 个 Timewarped 随从的导入、图片、购买和逐步效果实现。
- 33 个历史/上线额外随从的后续开关。
- Timewarped 非随从牌、法术、宝藏、退出牌的后续接入。
- UI、日志、测试、验收和回归顺序。

制作原则: 先做可运行闭环，再批量接卡牌；先保证数据和状态边界正确，再追求每张牌效果完整。

## 当前约束

已确认数据:

- 当前池随从 125 个: Minor 55 个，Major 70 个。
- 全 Firestone Timewarped 随从 158 个。
- 历史/上线额外随从 33 个，默认不进入当前版本。
- 当前池图片 125 张、全量图片 158 张、历史额外图片 33 张均已下载，失败 0。
- 当前随从成本: 76 张 cost 1，49 张 cost 2。

已确认工程边界:

- `AdvancedMechanicKind.Timewarp` 已存在。
- `AdvancedMechanicState` 目前只有单个 `PendingChoice`。
- `TavernState.Shop` 是普通酒馆，不应直接复用给 Timewarped Tavern。
- `NextTurn()` 当前先刷新普通酒馆并设置金币，然后执行 `DispatchTrinketTurnStarted()` 和 `MaybeOfferScheduledTrinketChoice()`。
- `ChooseMechanicOption()` 只支持 Trinket/Quest，选完后会清空 `PendingChoice`。
- `BuyMinion()` 会扣普通金币、移动普通商店槽位、触发购买事件、任务、英雄、饰品和三连。
- 图片运行时路径默认是 `Resources/CardImages/{cardId}`。

待确认官方细节:

- 第 6 回合和第 9 回合分别给多少 Chronum。
- Timewarped Tavern 每次展示多少格。
- 是否固定展示 `Exit the Timewarped Tavern` 卡，还是用 UI 退出按钮。
- Timewarped Tavern 是否严格受禁用种族过滤。
- 非随从 Timewarped 法术和宝藏的完整上线池。

这些待确认项第一版都要做成配置，不要写死在散落逻辑里。

## 总制作顺序

### 0. 锁定数据和验收口径

目的: 防止后续实现时数据源、图片、池定义反复变化。

制作顺序:

1. 固化 `Tools/scrape-timewarped-tavern.mjs` 的输出格式。
2. 以 `timewarped-tavern-research.json` 作为当前池和历史池来源。
3. 以 `timewarped-minion-mechanisms.json` 作为卡牌效果排期来源。
4. 增加一份实现追踪表，建议生成 `Docs/research/timewarped-tavern/timewarped-card-implementation-tracker.md`。
5. 在追踪表里给每张卡设置 `data_only`、`playable`、`partial_effect`、`complete_effect`、`blocked_by_rule` 状态。

验收:

- 当前池数量必须仍为 125。
- Minor 必须仍为 55，Major 必须仍为 70。
- 历史额外池必须仍为 33，并默认关闭。
- 图片失败列表仍为空。

### 1. 数据结构先行

目的: 让 Timewarped Tavern 独立于普通酒馆和普通随从池。

制作顺序:

1. 新增 `TimewarpKind`:
   - `None`
   - `Minor`
   - `Major`
   - `Historical`
2. 新增 `TimewarpTavernPhase`:
   - `Idle`
   - `DueThisTurn`
   - `BlockedByTrinketChoice`
   - `Open`
   - `Closed`
3. 新增 `TimewarpedTavernCardDefinition`，不要只绑定 `MinionDefinition`:
   - `CardId`
   - `DbfId`
   - `Name`
   - `ZhName`
   - `CardKind`
   - `TimewarpKind`
   - `Cost`
   - `TechLevel`
   - `Attack`
   - `Health`
   - `Tribes`
   - `Keywords`
   - `Text`
   - `ZhText`
   - `ImagePath`
   - `EffectIds`
   - `Tags`
   - `PoolStatus`
4. 新增 `TimewarpedOfferSlot`:
   - `SlotId`
   - `CardId`
   - `CardKind`
   - `Cost`
   - `Purchased`
   - `Source`
5. 新增 `PlayerTimewarpTavernState`:
   - `int Chronum`
   - `int NextTimewarpBonusChronum`
   - `int LastVisitRound`
   - `TimewarpKind PendingKind`
   - `TimewarpTavernPhase Phase`
   - `bool VisitOpen`
   - `string PendingSource`
   - `List<TimewarpedOfferSlot> Offers`
6. 在 `TavernState` 增加 `Timewarp` 字段。
7. 做旧存档兼容: `EnsureTimewarpState(tavern)` 为空时创建默认状态。

验收:

- 旧存档没有 Timewarp 字段时不崩。
- 普通 `Shop`、`ShopSlots`、`Frozen` 不因 Timewarp 状态初始化而变化。
- Timewarp 状态能随 `MatchState` 序列化。

### 2. Timewarped 卡牌目录

目的: 运行时能稳定拿到当前池、Minor/Major、历史池和图片路径。

制作顺序:

1. 由研究 JSON 生成运行时 JSON，建议文件:
   - `Assets/LearnHearthstone/Resources/Data/timewarpedTavernCards.json`
2. 新增 `TimewarpedTavernCatalog`:
   - `All`
   - `Current`
   - `Minor`
   - `Major`
   - `HistoricalExtra`
   - `GetByCardId(cardId)`
3. 新增 `TimewarpedTavernCatalogLoader`，读取 `Resources/Data/timewarpedTavernCards`。
4. 给 125 个当前池卡都生成 `ImagePath = CardImages/{cardId}`。
5. 保留 `costInTimewarpedTavern`，不要复用普通随从 3 金成本。
6. 将当前池随从同步到主 `MinionCatalog` 的策略二选一:
   - 第一版推荐: 同步到 `battlegroundsMinions.json`，但 `InPool = false` 或用 tag 标记 `timewarped`，避免进普通池。
   - 更长期方案: Timewarp catalog 可临时转 `MinionInstance`，主 MinionCatalog 只用于已存在定义。
7. 历史额外池先进入 JSON，但 `PoolStatus = historical_extra`，默认不参与当前 Timewarp。

验收:

- 加载器能读到 158 张随从定义。
- 当前池过滤返回 125。
- Minor 返回 55。
- Major 返回 70。
- HistoricalExtra 返回 33。
- 每张当前池卡都有 `ImagePath`。

### 3. 图片进入 Unity Resources

目的: 已下载图片能被运行时加载。

制作顺序:

1. 将 `Docs/research/timewarped-tavern/images-all/*.jpg` 导入到:
   - `Assets/LearnHearthstone/Resources/CardImages/{cardId}.jpg`
2. 如果现有卡图主要使用 PNG，可统一转 PNG:
   - `Assets/LearnHearthstone/Resources/CardImages/{cardId}.png`
3. 确认 `CardImageProvider` 可以加载 JPG；如果不能，优先统一转 PNG。
4. 生成或让 Unity 生成 `.meta`，不要手写不一致的 GUID。
5. 增加图片缺失校验测试或编辑器检查脚本:
   - 当前池 125 张必须能找到。
   - 全量 158 张建议能找到。

验收:

- 当前池 125 张在 Unity UI 中不走 fallback。
- 金色卡如果没有单独图片，先使用普通图或标记为待补。
- 图片导入不改变既有普通卡图。

### 4. Timewarp 触发时序

目的: 第 6/9 回合按规则打开，且同回合饰品先处理。

制作顺序:

1. 在 `NextTurn()` 中保持现有顺序:
   - 普通商店刷新。
   - 金币设置。
   - 回合开始日志。
   - `DispatchTrinketTurnStarted()`
   - `MaybeOfferScheduledTrinketChoice()`
2. 在 `MaybeOfferScheduledTrinketChoice()` 之后新增:
   - `MaybeScheduleTimewarpVisit()`
3. `MaybeScheduleTimewarpVisit()` 判断:
   - `State.Round == 6` -> Minor。
   - `State.Round == 9` -> Major。
   - `AdvancedMechanicMode` 允许 Timewarp 或 Mixed 时才触发；如果当前产品决定默认总是触发，则在 setup 里明确记录。
4. 如果 `AdvancedMechanics.PendingChoice != null`:
   - 不覆盖饰品选择。
   - `Timewarp.Phase = BlockedByTrinketChoice`
   - `PendingKind = Minor/Major`
   - `PendingSource = turn-schedule`
   - 写 recruit log: blocked by Trinket choice。
5. 如果没有 pending:
   - 直接 `OpenTimewarpedTavern(kind, source)`。
6. 修改 `ChooseMechanicOption()`:
   - 清空 `PendingChoice` 后调用 `MaybeResumePendingTimewarpAfterChoice(request.Kind)`。
   - 只在 Timewarp 状态是 `BlockedByTrinketChoice` 且 pending round 等于当前回合时恢复。
7. 不要把 Timewarp 作为新的 `PendingChoice`，否则会重新撞上单 pending 限制。

验收:

- 第 6 回合无饰品 pending 时直接打开 Minor。
- 第 9 回合无饰品 pending 时直接打开 Major。
- 第 6/9 回合有饰品 pending 时，先看到饰品选择。
- 饰品选完后，Timewarp 自动打开。
- 饰品 pending 未处理时，Timewarp 不覆盖 `PendingChoice`。

### 5. Chronum 和配置

目的: 货币独立、可保存、可调参。

制作顺序:

1. 新增 `TimewarpTavernRules`:
   - `MinorVisitRound = 6`
   - `MajorVisitRound = 9`
   - `MinorInitialChronum`
   - `MajorChronumGrant`
   - `OfferCount`
   - `IncludeExitCard`
   - `RespectActiveTribes`
2. Chronum 数量待官方确认，第一版用配置默认值并在日志里标注 `rule-unconfirmed`。
3. `OpenTimewarpedTavern()` 时:
   - 增加本次 Chronum。
   - 加上 `NextTimewarpBonusChronum`。
   - 清零已结算 bonus。
   - 保存 `LastVisitRound`。
4. 购买只扣 Chronum，不扣 `tavern.Gold`。
5. 退出和普通回合结束不清空 Chronum。
6. 如未来有奖励增加下次 Chronum，只写 `NextTimewarpBonusChronum`。

验收:

- Chronum 不影响普通金币。
- 购买失败不扣 Chronum。
- 第 6 回合剩余 Chronum 能带到第 9 回合。
- 退出保存 Chronum。
- 读档后 Chronum 仍在。

### 6. Offer 生成

目的: Timewarped Tavern 有独立商品列表，不污染普通酒馆。

制作顺序:

1. `OpenTimewarpedTavern(kind, source)` 根据 kind 取候选:
   - Minor: 当前池 `techLevel = 3`
   - Major: 当前池 `techLevel = 5`
2. 按 `TribeAvailabilityRules.IsMinionAvailable()` 过滤，除非配置关闭。
3. 用 deterministic seed:
   - `State.Seed`
   - `State.Round`
   - `kind`
   - `LastVisitRound`
4. 一次性生成 `OfferCount` 个 slot。
5. 购买后只标记或移除该 slot，不从普通池补牌。
6. 第一版不做 Timewarped Tavern 刷新。
7. 第一版使用 UI 退出按钮即可；如果要贴近官方卡面，再把 `BG34_BlackMarket_Skip` 做成固定 offer。

验收:

- 普通 `TavernState.Shop` 不变。
- 普通冻结槽位不变。
- 普通商店刷新光环不因 Timewarp 打开触发。
- 同一 seed 下 offer 稳定。
- 禁用种族过滤可开关。

### 7. Timewarp 购买和退出命令

目的: 建立完整交互闭环。

制作顺序:

1. 在 `GameCommandType` 新增:
   - `BuyTimewarpedTavernCard`
   - `ExitTimewarpedTavern`
2. 在 `MatchService.Apply()` 分发新增命令。
3. 新增 `BuyTimewarpedTavernCard(int offerIndex)`:
   - 检查 Timewarp 是否 open。
   - 检查 offer 存在且未购买。
   - 检查 Chronum 是否足够。
   - 如果是随从，检查手牌上限。
   - 扣 Chronum。
   - 生成新 `MinionInstance` 到手牌。
   - `PoolSource = Copy` 或新增 `PoolSource.Timewarped`。
   - `PoolCopiesHeld = 0`。
   - 不修改普通 `MinionPool` 副本。
   - 标记 offer purchased 或移除。
   - 写 recruit log。
   - 调用 `HandleCardsAddedToHand(1, "timewarp")`。
   - 解析三连。
4. 不直接调用 `BuyMinion()`，因为它会扣普通金币并清普通商店槽。
5. `ExitTimewarpedTavern()`:
   - `VisitOpen = false`
   - `Phase = Closed`
   - 保存 Chronum。
   - 清理本次临时 pending。
   - 写 recruit log。
6. 关闭后是否清空 Offers:
   - 第一版建议保留到下次 open 前，便于读档和日志。
   - 下一次 open 时重新生成。

验收:

- Timewarp 买随从进手牌。
- 新实例 ID 唯一。
- 购买扣 Chronum，不扣金币。
- 手牌满时失败且不扣 Chronum。
- 退出不推进回合。
- 退出后普通酒馆仍是原样。
- 三连逻辑对 Timewarped 随从生效。

### 8. UI 第一版

目的: 玩家能看、买、退出，且清楚这是特殊商店。

制作顺序:

1. 主入口优先改 `UnityTavernTrainerController`。
2. 如果 `Timewarp.VisitOpen == true`:
   - 在普通商店区域上方或替代区域显示 Timewarped Tavern 面板。
   - 标题显示 `Minor Timewarped Tavern` 或 `Major Timewarped Tavern`。
   - 显示当前 Chronum。
   - Offer 卡显示 cost。
   - 提供退出按钮。
3. Timewarp 面板中的购买按钮发 `BuyTimewarpedTavernCard`。
4. 普通刷新、冻结按钮在 Timewarp 面板打开时不要作用于 Timewarp offers。
5. 旧版 `TavernTrainerView` 和 `RealisticTavernTrainerView` 最低要求:
   - 不崩。
   - 可显示状态提示。
   - 可以通过简单按钮退出。
6. Advanced choice 状态面板增加 Timewarp pending/open 状态。

验收:

- Timewarp 打开时能看到特殊标题和 Chronum。
- 点击购买只买 Timewarp offer。
- 点击退出回到普通酒馆。
- 饰品选择 UI 和 Timewarp UI 不同时争用同一面板。
- 旧视图运行不崩。

### 9. 当前池 125 张随从的数据可玩

目的: 先让所有当前版本 Timewarped 随从可以买到、显示、进手牌、可三连。

制作顺序:

1. 当前池 125 张全部进入 Timewarped catalog。
2. 当前池 125 张的普通定义进入运行时可实例化路径。
3. 金色定义关系接入:
   - 使用 `goldenCardId`
   - 使用 `goldenDbfId`
4. 全部卡加 tag:
   - `timewarped`
   - `timewarp:minor` 或 `timewarp:major`
   - `timewarp:current`
   - 机制组 tag，例如 `timewarp_effect:stats`
5. 对未实现效果的卡，不阻止购买；在效果层标记 `implementation_status:data_only`。
6. 三连时优先沿用现有三连规则。

验收:

- 当前池 125 张任一抽到都不会实例化失败。
- 每张卡都能显示名称、文本、身材、种族、cost、图片。
- 每张卡都能进手牌。
- 三张同定义能合成，或如果暂未接三连，也必须明确测试标记。

### 10. 卡牌效果实现总顺序

目的: 用机制组推进，而不是逐卡随机补。

当前池机制分布:

| 机制组 | 当前池数量 | 制作优先级 |
| --- | ---: | --- |
| stats | 64 | P1 |
| card_generation | 51 | P1 |
| tribe_synergy | 40 | P1 |
| keyword_grant_or_keyword_body | 28 | P1 |
| shop_or_refresh | 23 | P2 |
| tavern_spell_synergy | 13 | P2 |
| summon | 13 | P2 |
| economy | 11 | P2 |
| damage | 9 | P3 |
| blood_gem | 9 | P2 |
| spellcraft | 8 | P2 |
| copy | 5 | P3 |
| transform | 4 | P3 |
| special | 4 | P4 |
| combat_only | 2 | P3 |
| hero_or_buddy | 1 | P4 |

效果实现顺序:

1. P1-A: 关键词和静态身材
   - 嘲讽、圣盾、复生、风怒、顺劈等。
   - 只需要定义数据和现有战斗引擎识别。
2. P1-B: 通用数值修改
   - 对自身、友方、手牌、酒馆、全局种族的攻击/生命修改。
   - 优先复用 `Enchantment` 和已有 buff 方法。
3. P1-C: 种族过滤和种族统计
   - 按 `TribeAvailabilityRules` 过滤。
   - 支持“每个类型”“友方 Naga 数量”“Beast/Undead/Demon”等统计。
4. P1-D: 通用衍生牌
   - 获取随机同等级随从。
   - 获取随机种族随从。
   - 获取 Tavern Coin、Blood Gem、Tavern Spell、Spellcraft。
5. P2-A: 回合开始/回合结束/战吼/亡语/Avenge/Rally 挂点
   - 每类先做通用触发接口，再接具体卡。
6. P2-B: 商店/刷新相关
   - 额外 offer。
   - 刷新计数。
   - 最高等级随从变金。
   - 酒馆中特定种族替换或追加。
7. P2-C: Tavern Spell 和 Spellcraft
   - Spellcraft 临时牌生成。
   - Tavern Spell 获取、复制、费用降低。
   - 与现有 `TavernSpellEngine` 对齐。
8. P2-D: Blood Gem
   - 宝石攻击/生命全局增益。
   - 生成 Blood Gem。
   - 自动打宝石。
9. P3-A: 战斗召唤和 combat-only
   - 从手牌临时召唤。
   - 战斗后不保留的关键词或身材。
10. P3-B: 伤害和回溯
   - 对敌方随机/最高生命目标造成伤害。
   - 英雄受伤回溯。
   - 邻近伤害。
11. P3-C: 复制和变形
   - 复制敌方/友方/酒馆法术。
   - 泽鲁斯、变色龙、幸运彩蛋、召唤师。
12. P4: 特殊牌
   - 转动尤格萨隆命运之轮。
   - 获取英雄技能的伙伴。
   - 触发所有友方亡语。
   - 触发所有友方战吼。

每完成一个机制组，都要:

- 更新实现追踪表。
- 给该组至少加 2 个单卡测试。
- 给一个跨系统回归测试。
- 在 recruit log 中确认关键动作可见。

### 11. 当前池随从批次建议

目的: 尽快把 125 张卡从“可以买”推进到“常见局可用”。

批次 1: 纯数据和关键词牌

- 目标: 所有无复杂触发的卡显示、购买、手牌、三连可用。
- 包含: 关键词本体、静态身材、基础种族。
- 风险低，适合最先做。

批次 2: 通用触发牌

- 目标: 接入 `start_of_turn`、`end_of_turn`、`battlecry`、`deathrattle`、`avenge`、`rally`。
- 做法: 先建通用触发分发，再接卡。
- 代表: Alleycat、Busker、Pillager、Geomancer、Pashmar 等。

批次 3: 生成牌和经济牌

- 目标: 衍生牌、金币、Tavern Coin、Blood Gem、随机随从。
- 做法: 复用现有 `Add...ToHand` 和 `HandleCardsAddedToHand`。
- 验收重点: 手牌满不崩、不重复发、不丢三连。

批次 4: 商店/刷新牌

- 目标: 额外酒馆牌、刷新计数、酒馆变金、酒馆替换。
- 做法: 只作用普通酒馆，除非牌面明确 Timewarped Tavern。
- 验收重点: 不影响 Timewarped offers。

批次 5: 战斗牌

- 目标: 战斗开始、战斗内召唤、亡语、击杀后复制、战斗临时效果。
- 做法: 优先接已有 `CombatEngine` 机制。
- 验收重点: 战斗结束后临时效果清理。

批次 6: 复杂特殊牌

- 目标: Wheel of Yogg、Buddy、触发所有战吼/亡语、Zerus、Lucky Egg。
- 做法: 每张单独写设计说明和测试。
- 验收重点: 不用模糊 proxy 长期替代正式效果。

### 12. 非随从 Timewarped 卡

目的: 给完整 Timewarped Tavern 留好扩展口，不阻塞第一版。

制作顺序:

1. 先把非随从卡纳入 `TimewarpedTavernCardDefinition.CardKind`。
2. 接入 `BG34_BlackMarket_Skip`:
   - 如果作为 offer，购买/选择后等同 `ExitTimewarpedTavern()`。
   - 如果作为 UI 按钮，仍保留定义用于日志和图片。
3. 接入 `Casts When Bought` 类:
   - 检查 Chronum。
   - 扣 Chronum。
   - 立即执行效果。
   - 不进手牌。
4. 接入普通 Tavern Spell:
   - 允许进入手牌或直接进入现有施放流程，按牌面分类。
5. 接入 `Get`、`Discover`、`Choose` 类:
   - 复用现有 Discover/Choice 流程。
6. 接入宝藏和第二英雄技能:
   - 不要混进 `MinionDefinition`。
   - 使用独立 card kind 或现有 `HeroPowerDefinition`。

第一版范围:

- 可以先只做 `Exit` 和随从购买。
- 其他 38 张非随从 Timewarped 卡标记为 `blocked_by_non_minion_support`。

### 13. 历史/上线额外池

目的: 支持“上线版本复刻”，但不污染当前版本。

制作顺序:

1. 历史额外 33 张进入 catalog。
2. 默认 `PoolStatus = historical_extra` 且不进入 current。
3. 新增开关:
   - `UseHistoricalTimewarpedPool`
   - 或 `TimewarpedPoolVersion = current/firestone_all/launch`
4. UI debug 面板可显示当前 Timewarp pool version。
5. 测试默认不开历史池。

验收:

- 默认 Timewarp 仍只抽 125 当前池。
- 开历史池时能抽到 techLevel 0 的历史额外卡，但必须按配置定义 Minor/Major 归属或标记为 unknown。

### 14. Oathstone's Summoning 异常

目的: 明确它不是第 6/9 回合 Timewarped Tavern。

制作顺序:

1. 单独建 `Oathstone Pool Injection` 逻辑。
2. 第 7 回合把 Minor Timewarped minions 注入普通酒馆池。
3. 第 10 回合把 Major Timewarped minions 注入普通酒馆池。
4. 注入后进入普通 `RefreshShopFromPoolPreservingFrozen()` 候选。
5. 该异常不打开特殊商店，不给 Chronum。

验收:

- 第 6/9 Timewarp visit 和第 7/10 pool injection 相互独立。
- Oathstone 注入会影响普通刷新池。
- Timewarp Tavern 购买仍不扣普通池。

### 15. 测试顺序

先写系统测试，再写卡牌测试。

系统测试:

1. `TimewarpCatalogTests`
   - 当前池 125。
   - Minor 55。
   - Major 70。
   - 历史额外 33。
   - 图片路径非空。
2. `TimewarpTavernSystemTests`
   - 第 6 回合打开 Minor。
   - 第 9 回合打开 Major。
   - Timewarp 不改普通 shop。
   - deterministic offers。
3. `TimewarpTrinketOrderingTests`
   - 同回合饰品先出现。
   - Timewarp blocked 状态正确。
   - `ChooseMechanicOption()` 后恢复 Timewarp。
4. `TimewarpPurchaseTests`
   - Chronum 足够可购买。
   - Chronum 不足失败。
   - 手牌满失败且不扣 Chronum。
   - 购买生成新实例 ID。
   - 购买不扣金币。
   - 购买不扣普通池。
5. `TimewarpPersistenceTests`
   - Chronum 结转。
   - open visit 读档。
   - 旧存档缺字段不崩。
6. `TimewarpUiTests`
   - UnityStyle 显示 Chronum。
   - 可以购买。
   - 可以退出。
   - 旧视图不崩。

卡牌测试:

1. 每个机制组至少 2 张代表卡。
2. 每个触发时机至少 1 个测试:
   - start of turn
   - end of turn
   - battlecry
   - deathrattle
   - avenge
   - rally
   - start of combat
   - damage reactive
3. 每个资源系统至少 1 个测试:
   - Blood Gem
   - Tavern Spell
   - Spellcraft
   - Discover
   - Copy
   - Transform
4. 每批卡牌完成后跑:
   - `MatchServiceMechanicTests`
   - `TrinketSystemTests`
   - `TavernSpellEngineTests`
   - `CombatMechanicTests`
   - `UnityTavernTrainerViewTests`

### 16. 日志和调试

目的: 方便确认时序和购买问题。

制作顺序:

1. recruit log 增加这些消息:
   - `Timewarp due`
   - `Timewarp opened`
   - `Chronum gained`
   - `Offer generated`
   - `Timewarped card bought`
   - `Timewarp exited`
   - `Chronum saved`
   - `Blocked by Trinket choice`
   - `Resumed after Trinket choice`
2. 日志里保留 source:
   - `turn-6-minor`
   - `turn-9-major`
   - `trinket-delayed`
   - `debug`
   - `anomaly:oathstone`
3. Debug 命令:
   - `DebugOpenMinorTimewarp`
   - `DebugOpenMajorTimewarp`
   - `DebugAddChronum`
   - `DebugCloseTimewarp`
4. Debug 命令不进入正式 UI 主路径，但可给开发按钮。

验收:

- 用户能从日志判断 Timewarp 是否被饰品延后。
- 购买失败原因明确。
- Debug 命令不影响正常触发测试。

### 17. 验收里程碑

Milestone A: 数据和图片完成

- Catalog 加载 158 张。
- 当前池/历史池数量正确。
- 当前池图片可加载。

Milestone B: Timewarp 商店闭环

- 第 6/9 回合触发。
- 饰品优先。
- 能打开、购买、退出。
- Chronum 保存。
- 普通酒馆不受影响。

Milestone C: 125 张当前池 data-only 可玩

- 当前池每张卡都能作为 offer。
- 每张卡可买进手牌。
- 不因效果未实现崩溃。

Milestone D: P1 效果完成

- 关键词、基础数值、种族联动、基础衍生牌完成。
- 覆盖当前池中最大量机制。

Milestone E: P2 效果完成

- 商店/刷新、Tavern Spell、Spellcraft、Blood Gem、召唤、经济完成。

Milestone F: P3/P4 效果完成

- 战斗伤害、复制、变形、特殊牌完成。

Milestone G: 扩展池完成

- 非随从 Timewarped 卡。
- 历史/上线额外池开关。
- Oathstone 异常注入。

## 不要提前做的事

- 不要把 Timewarped Tavern 直接塞进普通 `TavernState.Shop`。
- 不要让 Timewarp 覆盖饰品 `PendingChoice`。
- 不要让 Timewarp 购买扣普通金币。
- 不要让 Timewarp 购买扣普通随从池副本。
- 不要在 Chronum 官方数值未确认前把数值写死在多个方法里。
- 不要为了第一版强行实现 38 张非随从 Timewarped 卡。
- 不要默认把 33 张历史额外随从放入当前版本池。

## 推荐第一轮实际开发切片

第一轮只做这些:

1. `PlayerTimewarpTavernState` 和 catalog。
2. 当前池 125 张 data-only。
3. 图片进 `Resources/CardImages`。
4. 第 6/9 回合打开 Minor/Major。
5. 饰品 pending 时阻塞，饰品选完后恢复。
6. Chronum 配置、扣费、保存。
7. `BuyTimewarpedTavernCard` 和 `ExitTimewarpedTavern`。
8. UnityStyle UI 显示、购买、退出。
9. 系统测试覆盖触发、购买、退出、保存和饰品顺序。

第一轮完成后，再按机制组推进 125 张卡牌效果。

## 相关资料

- `timewarped-tavern-system-mechanics.md`: 总机制。
- `mechanics-and-api-notes.md`: 数据源、API 和工程时序调研。
- `timewarped-tavern-research.json`: 当前池、全量池、历史额外池结构化数据。
- `timewarped-minion-mechanisms.json`: 逐随从机制分类。
- `images-current`: 当前池图片。
- `images-all`: 全量 Timewarped 随从图片。
- `images-historical-extra`: 历史额外图片。
