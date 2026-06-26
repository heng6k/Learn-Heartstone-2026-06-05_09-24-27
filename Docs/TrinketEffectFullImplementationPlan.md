# 饰品效果全量实现文档

## 当前结论

目前项目已经有 `TrinketSystemImplementationPlan.md`，但那份文档定位是饰品底座和 MVP：槽位、候选、购买、装备状态、少量效果、Marin/Buttons 接入。它不是 330 个饰品效果的全量落地文档。

本文件补齐“全量实现”部分，后续做饰品时以它为主线：

- `battlegroundsTrinkets.json` 是 330 个饰品的权威清单，不在本文手抄 330 行，避免文档和数据分叉。
- 本文定义每张饰品必须补齐的状态字段、选择池规则、效果家族、运行时触发点、实现批次、测试要求和验收标准。
- 每批实现后必须把 JSON 中对应饰品的 `implementationStatus`、`effectIds`、`offerPoolStatus`、`effectFamily` 和备注同步更新。

## 当前项目状态

当前 `Assets/LearnHearthstone/Resources/Data/battlegroundsTrinkets.json` 中：

| 项目 | 数量 |
| --- | ---: |
| 饰品总数 | 330 |
| 小饰品 Lesser | 157 |
| 大饰品 Greater | 173 |
| 已有可运行效果 | 8 |
| 仅底座接入 `FrameworkFirst` | 322 |
| 已显式填写 `offerPoolStatus` | 330 |

已实现效果：

| ID | 饰品 | 槽位 | effectId | 当前行为 |
| --- | --- | --- | --- | --- |
| `BG30_MagicItem_996` | Bob's Tip Jar | Greater | `bobs_tip_jar` | 装备时获得 4 金币，最大金币 +4 |
| `BG30_MagicItem_879t` | Dalaran Cheese Wheel | Greater | `dalaran_cheese_wheel` | 酒馆随从 +2/+2，每刷新 4 次提升 |
| `BG30_MagicItem_414t` | Kodo Leather Pouch | Greater | `kodo_leather_pouch` | 买牌后随机 2 个友方随从 +4/+4 |
| `BG30_MagicItem_970t` | Valorous Medallion | Greater | `valorous_medallion` | 战斗开始全体 +6/+6 |
| `BG30_MagicItem_879` | Dalaran Cheese Wheel | Lesser | `dalaran_cheese_wheel` | 酒馆随从 +1/+1，每刷新 4 次提升 |
| `BG30_MagicItem_847` | Goblin Wallet | Lesser | `goblin_wallet` | 回合结束后未来最大金币 +1 |
| `BG30_MagicItem_414` | Kodo Leather Pouch | Lesser | `kodo_leather_pouch` | 买牌后随机 2 个友方随从 +2/+1 |
| `BG30_MagicItem_970` | Valorous Medallion | Lesser | `valorous_medallion` | 战斗开始全体 +2/+2 |

现有测试覆盖：

- 目录加载 330 张，157 小饰品，173 大饰品。
- 所有饰品图片可加载。
- 双打、队友、传递类饰品不应混入当前目录。
- 调试选择、购买扣费、装备状态。
- 上面 8 个效果的基础行为。

## 目标与非目标

目标：

- 所有单人可用饰品都有明确状态，不再存在“看起来能选但没有效果”的普通池条目。
- 小饰品和大饰品共用同一套效果处理器，数值通过参数区分。
- 普通选择池只出现 `Offerable` 且效果已落地的饰品。
- `HiddenEffectOnly` 和 `DebugOnly` 可以通过调试、指定 ID 或其他机制挂载，用于测试、历史保留和依赖未完成的效果。
- 任务、饰品、畸变、扭曲时空共用底座兼容，不能互相覆盖 `PendingChoice` 或吞掉触发事件。
- 每个效果至少有一条直接测试，复杂效果有边界测试。

非目标：

- 不追求一次性还原官方全部隐藏概率、智能推荐权重和实时平衡公式。
- 不为每个饰品做独立 UI。普通详情卡、日志和调试信息先复用共用选择弹窗。
- 不启用双打、队友、传递类效果。再次导入数据时仍要自动过滤。
- 不把外部网站作为运行时依赖。HSReplay 和营地只用于人工核对文本、图片和分类。

## 数据字段要求

在 `TrinketDefinition` 和 JSON 中补齐以下字段。

```csharp
public enum TrinketOfferPoolStatus
{
    Offerable,
    HiddenEffectOnly,
    DebugOnly,
    Disabled
}

public enum TrinketPowerTier
{
    Weak,
    Medium,
    Strong,
    Top,
    Unknown
}
```

建议新增字段：

| 字段 | 用途 |
| --- | --- |
| `offerPoolStatus` | 控制能否进入普通小饰品/大饰品选择池 |
| `powerLevel` | 人工强弱评价，用于后续平衡、权重和费用修正，不直接决定效果 |
| `effectFamily` | 主效果家族，例如 `economy`、`shop_aura`、`combat_start` |
| `requires` | 依赖系统，例如 `tavern_spell`、`blood_gem`、`discover`、`combat_event` |
| `proxyLevel` | `Exact`、`ProxySafe`、`DebugProxy`、`Blocked` |
| `notes` | 保留查证点、官方占位、依赖原因 |

`effectIds` 必须稳定，不能用显示名称当逻辑判断。大小饰品同名同机制时复用同一个 `effectId`，通过参数表区分数值，例如 Kodo Leather Pouch 和 Valorous Medallion。

## 选择池策略

| 状态 | 是否普通三/四选一 | 是否可调试挂载 | 适用情况 |
| --- | --- | --- | --- |
| `Offerable` | 是 | 是 | 单人可用，效果已实现或代理足够可靠 |
| `HiddenEffectOnly` | 否 | 是 | 效果要做，但体验弱、过旧、过强、训练器里不适合自然出现 |
| `DebugOnly` | 否 | 是 | 依赖系统未齐、官方占位未确认、代理实现风险较高 |
| `Disabled` | 否 | 否 | 双打、队友、传递、无法在单人训练器表达、数据保留但不应使用 |

普通选择池规则：

1. `OfferTrinketChoice` 只能从 `offerPoolStatus == Offerable` 的同槽位饰品中抽。
2. `ImplementationStatus != Implemented` 的饰品不能进入普通池。
3. `DebugOnly` 和 `HiddenEffectOnly` 只能通过调试命令、指定 ID 测试、任务奖励指定挂载等非普通入口使用。
4. `Disabled` 连指定挂载也应拒绝，并写日志说明原因。
5. 导入或更新数据时继续过滤文本包含 teammate、partner、pass to、passes、passing 等双打/传递语义的条目。

当前数据里 `pass` 的匹配主要来自 `Compass` 文本的官方占位 `92`，不是双打传递，但这类 `92` 占位在含义确认前应先设为 `DebugOnly` 或 `Disabled`。

## 强弱评价

饰品的强弱评价只服务于后续平衡，不要写死在效果逻辑里。

| 评价 | 含义 | 默认处理 |
| --- | --- | --- |
| `Weak` | 收益偏低、节奏慢、训练器体验不明显 | 可以实现，但可放 `HiddenEffectOnly` |
| `Medium` | 稳定收益，局部改变玩法 | 优先 `Offerable` |
| `Strong` | 明显改变经济、战斗或成长速度 | `Offerable`，但需要更完整测试 |
| `Premium` | 构筑核心或显著改变整局节奏 | 先实现，再人工决定是否普通可选 |
| `Pending` | 官方占位、资料不清或依赖未完成 | 不进普通池 |

这套评价应保持灵活。后续如果某个饰品在训练器里过强或过弱，只改 JSON 配置和池状态，不改触发器代码。

## 运行时架构

当前饰品触发逻辑集中在 `MatchService`，已经有：

- `OfferTrinketChoice`
- `EquipTrinketFromOption`
- `ApplyTrinketEquippedEffects`
- `DispatchTrinketTurnEnded`
- `DispatchTrinketCardBought`
- `RecordTrinketShopRefresh`
- `ApplyTrinketShopAuras`
- `PrepareTrinketCombatStartEffects`

330 个饰品全量实现时，不建议继续把所有逻辑堆成一个巨大 `MatchService` switch。推荐分两层：

1. `MatchService` 负责调用时机、读写当前对局状态、日志和命令边界。
2. `TrinketEffectDispatcher` 或若干 `TrinketEffectHandlers` 负责按 `effectId` 执行具体效果。

需要补齐的统一事件入口：

| 事件 | 用于哪些效果 |
| --- | --- |
| `OnEquip` | 装备即获得金币、卡牌、伙伴、复制、解锁规则 |
| `TurnStarted` | 回合开始给牌、给金币、生成法术、刷新可用状态 |
| `TurnEnded` | 回合结束触发战吼、吞食酒馆、成长、给鲜血宝石 |
| `ShopRefreshed` | 刷新计数、酒馆随从增益、特定刷新替换 |
| `CardBought` | 买牌复制、买随从给友方属性、买酒馆法术复制 |
| `CardPlayed` | 打出随从、打出类型、打出战吼、同星级增益 |
| `MinionSold` | 出售后把属性给酒馆、出售计数类触发 |
| `TavernSpellCast` | 酒馆法术额外施放、法术增益、法术计数 |
| `DiscoverResolved` | 发现后额外复制、发现替换、发现伙伴 |
| `TripleCheck` | 两张三连、特殊金色规则 |
| `CostModified` | 酒馆随从费用固定、酒馆法术费用降低 |
| `CombatStarted` | 战斗开始光环、复制、临时金色、立即攻击 |
| `CombatMinionSummoned` | 战斗中召唤后增益、召唤奖励 |
| `CombatMinionDied` | 亡语、复仇、死亡回写永久增益 |
| `CombatMinionAttacked` | 攻击后增益、攻击后死亡、风怒/立即攻击联动 |
| `CombatEnded` | 战斗后发现/复制、最后死亡随从奖励 |

## 效果家族实现要求

### 1. 经济和费用

覆盖：获得金币、最大金币、免费刷新、固定费用、费用降低。

实现要求：

- 所有费用校验和 UI 展示走同一个费用 helper。
- 金币上限改动写入 `PlayerTrinketState`，不要散落在 `TavernState` 临时字段里。
- 免费刷新要区分“本次刷新费用为 0”和“获得一枚可消耗刷新券”。
- 费用类效果必须测试金币不足、折扣后最低值、回合重置和日志。

### 2. 酒馆和刷新

覆盖：酒馆随从光环、刷新后增益、刷新计数成长、替换酒馆、注入指定类型。

实现要求：

- 临时酒馆光环必须可移除和重算，避免多次刷新叠错。
- 被购买的酒馆随从如果已经获得永久属性，进入手牌后保留；如果只是光环，购买前要明确是否固化。
- `Dalaran Cheese Wheel` 现有实现可以作为刷新成长模板，但要抽成通用计数器。
- 所有“随机酒馆随从”在空酒馆时 no-op 并写日志。

### 3. 买、卖、打出触发

覆盖：买牌后复制、买随从后给属性、出售后把属性给酒馆、打出随从后同星级增益、按类型触发。

实现要求：

- 购买触发必须在实际扣费并把卡加入手牌之后执行，避免失败购买触发。
- “第一次购买”类每回合清零。
- 出售触发需要在出售对象释放前记录当前属性、类型、星级和是否金色。
- 打出触发需要区分“从手牌打出”和“战斗/效果召唤”，不能混计。

### 4. 回合开始和回合结束

覆盖：给手牌属性、给随机卡、触发战吼、吞食酒馆、按类型全体增益。

实现要求：

- 回合开始顺序应在新回合金币、刷新、临时状态清理之后，玩家操作前。
- 回合结束顺序应在英雄、随从本身回合结束效果之后，再触发饰品，最后进入下一回合结算。
- 额外触发回合结束效果时必须防递归，不能让“额外触发”再次额外触发自己。
- 目标不存在时 no-op，不抛异常。

### 5. 战斗开始

覆盖：全体临时属性、边缘随从金色、复制最高生命、立即攻击、最左圣盾。

实现要求：

- 只作用于战斗副本，除非文本明确永久。
- 临时金色不能触发三连，也不能回写酒馆棋盘。
- 复制随从要生成新 `InstanceId`，保留附魔还是原始复制按文本决定。
- 战斗开始后产生的立即攻击标记需要 CombatEngine 明确消费和清理。

### 6. 战斗内事件

覆盖：复仇、攻击后死亡、随从死亡回写永久增益、召唤后增益、攻击触发成长。

实现要求：

- CombatEngine 需要回传足够事件：友方死亡、敌方死亡、友方攻击、友方召唤、伤害来源、死亡顺序。
- Avenge 计数按装备的饰品独立维护，战斗结束清理。
- 永久回写必须能定位酒馆棋盘原始随从；衍生物没有原始对象时只记录战斗日志。
- 战斗奖励进手牌时要检查手牌上限。

### 7. 酒馆法术和塑造法术

覆盖：酒馆法术费用降低、额外施放、购买复制、随机施放、生成随机酒馆法术、Spellcraft 临时法术。

实现要求：

- 酒馆法术购买、施放、随机施放都走同一入口，避免额外施放重复扣费。
- 随机施放需要合法目标选择器，找不到目标时跳过该法术。
- Spellcraft 生成的临时牌需要回合开始或回合结束清理，使用后也要移除。
- 临时 Spellcraft 卡必须有来源标签，避免进入永久卡池。

### 8. 发现、复制和生成

覆盖：发现额外复制、战斗后发现对手战队、发现伙伴、获得指定 child card、随机生成卡。

实现要求：

- `DiscoverResolved` 后才能复制最终选中项。
- 如果已有 `PendingChoice`，新发现不能直接覆盖，应排队或写日志延迟。
- `relatedDbfId` 和官方 child card 必须先查证，不能把 `'0'` 文本当真实 ID。
- 复制出来的卡必须生成新 `InstanceId`，并按文本决定是否保留附魔。

### 9. 金色、三连和变形

覆盖：两张三连、使酒馆或友方随从金色、临时金色、Shifter Zerus、升星变形器。

实现要求：

- 三连阈值要进 TripleEngine，不在单个购买逻辑里硬写。
- 临时金色和永久金色必须分开标记。
- 金色酒馆随从不应自动触发玩家手牌三连，除非被购买后满足三连规则。
- 变形卡需要保留“可继续变形”的状态，不只是直接替换卡牌定义。

### 10. 种族专属

覆盖：野兽、恶魔、龙、元素、机械、鱼人、娜迦、海盗、野猪人、亡灵等专属饰品。

实现要求：

- 所有种族判断走现有 `Tribe`/`AssociatedRaces` 体系，不用字符串散判。
- 受当前禁用种族影响的饰品，不满足条件时不进普通池。
- 混合种族随从按当前项目已有规则计算类型数量。
- 种族专属饰品优先实现能复用共用事件的部分，再补特殊 token。

### 11. 官方占位和资料待确认

常见风险：

- 文本中的 `'0'` 通常代表 child card 占位，需要查 HearthstoneJSON 关联卡。
- 文本中的 `92` 当前在 Colorful Compass 等饰品里出现，含义未确认前不进普通池。
- “helpful refresh”“Yogg wheel”“Wisdomball”这类官方智能池不能随便硬猜，先做可测代理并标为 `DebugOnly`。
- 对手历史、第二英雄技能、伙伴发现等依赖其他系统，系统未齐前不进普通池。

## 实现顺序

### 第 0 批：全量数据和选择池闸门

目标：先保证普通池不会出现未实现饰品。

工作：

1. 新增 `TrinketOfferPoolStatus`、`TrinketPowerLevel`、`effectFamily` 等字段。
2. 给 330 个饰品全部补显式 `offerPoolStatus`，初始策略为：8 个已实现可暂定 `Offerable`，其余先按依赖分为 `DebugOnly` 或 `Disabled`，明确低风险的进入 `Planned`。
3. `OfferTrinketChoice` 改为只抽 `Offerable + Implemented`。
4. 目录测试新增“没有空 `offerPoolStatus`”“普通池无 `FrameworkFirst`”。
5. 输出一次全量核对表，按槽位、家族、池状态、实现状态分组。

### 第 1 批：小饰品低风险效果

优先 Lesser，因为数值小、调试反馈快。

范围：

- 装备即获得资源。
- 回合开始/结束给金币、给手牌、给随机法术。
- 买牌后给属性或复制。
- 简单酒馆光环和刷新计数。

完成标准：

- 每个新增 `effectId` 有直接测试。
- 小饰品普通池中只出现能真实生效的条目。
- 同名大饰品如果只差数值，先把参数表设计好但可暂不开放。

### 第 2 批：大饰品同构效果

范围：

- 与小饰品同名、同机制、数值更高的大饰品。
- 当前已有模板：Kodo Leather Pouch、Dalaran Cheese Wheel、Valorous Medallion。

完成标准：

- 大小饰品共享 handler。
- 参数来自 definition 或 effect parameter 表，而不是在 handler 中按 ID 写死。
- 大饰品选择池开始出现第一批稳定可选项。

### 第 3 批：酒馆法术、Spellcraft、发现和复制

范围：

- 酒馆法术费用、复制、额外施放、随机施放。
- Spellcraft 临时牌。
- Discover 后复制、Discover 伙伴、战斗后 Discover。

完成标准：

- `PendingChoice` 支持排队或拒绝覆盖。
- 所有生成卡检查手牌上限。
- 随机施法有合法目标选择器和 no-op 日志。

### 第 4 批：战斗开始效果

范围：

- 战斗开始加属性。
- 复制随从。
- 临时金色。
- 立即攻击。
- 圣盾、潜行、风怒等关键词。

完成标准：

- 战斗副本和酒馆状态分离清楚。
- 战斗结束后临时效果全部清理。
- Replay 和 CombatLog 能看到关键动作。

### 第 5 批：战斗内事件和复仇

范围：

- Avenge。
- 友方攻击。
- 友方/敌方死亡。
- 战斗中召唤。
- 死亡后永久回写。

完成标准：

- CombatEngine 回传事件足够稳定。
- 所有 Avenge 计数独立、可重置。
- 永久回写有原始实例定位和失败日志。

### 第 6 批：种族包

按种族逐包做，推荐顺序：

1. 机械：磁力、酒馆机械、机械复制。
2. 野猪人：鲜血宝石、宝石额外属性。
3. 娜迦：Spellcraft。
4. 鱼人：手牌、发现、召唤。
5. 野兽：召唤、攻击、死亡。
6. 龙：战斗开始、战斗中属性。
7. 元素：刷新、酒馆成长。
8. 海盗：金币、购买、攻击。
9. 恶魔：吞食、伤害、酒馆吞食。
10. 亡灵：复生、亡语、死亡计数。

每包完成后再把对应 `Offerable` 打开，避免半包进入普通池。

### 第 7 批：高风险代理和资料待确认

范围：

- Wisdomball。
- Yogg wheel。
- Shifter Zerus。
- 第二英雄技能。
- 伙伴发现。
- 官方 `92` 占位。
- `Get a copy of '0'` child card。

处理策略：

- 能查证官方 child card 的，做精确实现。
- 依赖系统未完成但可安全代理的，标 `DebugOnly + ProxySafe`。
- 代理会显著误导普通体验的，保持 `DebugOnly`。
- 无法表达的保留 `Disabled`。

### 第 8 批：全量回归和平衡

目标：从“都能跑”变成“普通池可玩”。

工作：

1. 重新审查全部 `offerPoolStatus`。
2. 按 `powerTier` 调整普通池权重或费用策略。
3. 跑目录、选择池、效果、战斗、图片、Unity 冒烟测试。
4. 手动在 Unity 里分别开普通饰品模式、小饰品调试、大饰品调试、任务+饰品兼容局。
5. 把文档、JSON 和测试的数量全部对齐。

## 与任务机制的兼容点

任务机制已经会占用 `PendingChoice`，饰品不能直接覆盖：

- 同一时刻只能有一个主选择弹窗。
- 如果任务开局三选一正在等待，饰品选择应延迟到选择完成后。
- 如果任务奖励触发“选择饰品购买”，要复用饰品选择请求，但 `Source` 必须标明来自任务奖励。
- 日志中要区分 “Quest offered Trinkets” 和 “Trinket turn offered”。
- 任务、饰品同时监听同一事件时，事件顺序固定并写进测试，避免后续改动时隐形变更。

建议顺序：

1. 随从/英雄自身事件。
2. 英雄技能。
3. 饰品。
4. 任务奖励。
5. 畸变或扭曲时空全局修正。

这个顺序后续如果为了官方一致性调整，必须在文档和测试里一起改。

## 测试计划

目录和数据测试：

- 总数保持 330，Lesser 157，Greater 173。
- 所有饰品有图片路径并能加载。
- 所有饰品有非空 `offerPoolStatus`、`powerLevel`、`effectFamily`。
- `Offerable` 必须 `Implemented` 且至少有一个 `effectId`。
- `Disabled` 不允许通过普通或调试装备。
- 文本含队友、传递、双打语义的条目不在目录或必须 `Disabled`。

选择和装备测试：

- 小饰品/大饰品分别只抽对应槽位。
- 普通池不出现 `HiddenEffectOnly`、`DebugOnly`、`Disabled`。
- 金币不足不能购买。
- 已有同槽位饰品不能重复装备。
- 指定 ID 可装备 `HiddenEffectOnly` 和 `DebugOnly`，但不能装备 `Disabled`。

效果测试：

- 每个 `effectId` 至少一条直接测试。
- 同一 handler 的大小饰品都要覆盖数值差异。
- 随机目标用固定 seed。
- 空棋盘、空酒馆、满手牌、满棋盘、无合法目标都要覆盖。
- 所有临时战斗效果战斗后清理。
- 所有复制卡有新 `InstanceId`。

集成测试：

- 任务开局选择和饰品选择不互相覆盖。
- 战斗测试中饰品效果、任务奖励效果、英雄效果的顺序稳定。
- 刷新、买牌、施法、出售、打出随从的共享事件不会重复触发。
- Unity 里图片、名称、费用、文本、槽位展示正常。

## 资料核对要求

实现前需要对以下来源做人工比对：

- HSReplay 饰品页：确认小饰品/大饰品列表、名称、费用和文本。
- 营地酒馆工具：确认图片、筛选结果、中文理解和是否存在遗漏。
- HearthstoneJSON：确认 `dbfId`、`relatedDbfId`、child card、mechanics、referencedTags。

重点查证清单：

- 文本含 `'0'` 的指定复制对象。
- 文本含 `92` 的官方占位。
- Wisdomball、Yogg wheel、Zerus 等官方特殊池。
- 伙伴、第二英雄技能、对手历史这类跨系统依赖。
- 任何新导入后重新出现的队友、传递、双打词条。

## 验收标准

饰品全量实现完成时必须满足：

- 330 个饰品全部有明确 `offerPoolStatus`、`powerLevel`、`effectFamily`、`implementationStatus`。
- 普通小饰品/大饰品选择池中没有无效果条目。
- 所有 `Offerable` 和 `HiddenEffectOnly` 都有可运行效果。
- `DebugOnly` 的代理或阻塞原因写在 `notes` 中。
- `Disabled` 明确说明为什么不能在单人训练器使用。
- 所有图片和 `.meta` 存在，Unity 刷新后无缺图。
- 每个 `effectId` 有测试，核心家族有边界测试。
- 任务+饰品兼容场景通过测试。
- Unity 人工视觉冒烟通过后再进入畸变或扭曲时空下一批。

## 下一步执行建议

第 0 批已经完成：

1. 饰品数据已补 `offerPoolStatus`、`powerLevel`、`effectFamily`、`requires`、`proxyLevel`。
2. 普通选择池已过滤为 `Offerable + Implemented`。
3. 当前 330 个条目中 8 个 `Offerable`，322 个 `DebugOnly`。

下一步进入第 1 批：从 Lesser 的低风险经济、酒馆、买牌触发开始补效果，每实现一张再把对应条目从 `DebugOnly/Pending/Blocked` 调整到真实状态。

这样做完后，饰品机制会从“目录全、效果少”变成“普通池稳定、剩余效果有清晰队列”，后续每一批都能独立测试和回滚。
