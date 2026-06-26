# 饰品、任务、任务奖励的不完整与模糊实现说明

更新日期：2026-06-26

本文用于回答一个容易混淆的问题：当前饰品、任务、任务奖励是否已经完成。

结论是：系统接入、数据注册、基础触发与可运行路径已经完成；但这不等于所有条目都达到了官方完全一致。当前还有一部分内容属于 `ProxySafe`、`HiddenEffectOnly`、`DebugOnly` 或 `Disabled`，它们可以用于开发验证、近似训练或底层效果验证，但不能被当作最终官方还原。

## 数据来源

- `Assets/LearnHearthstone/Resources/Data/battlegroundsTrinkets.json`
- `Assets/LearnHearthstone/Resources/Data/battlegroundsQuests.json`
- `Assets/LearnHearthstone/Runtime/Domain/Models/TrinketModels.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Models/QuestModels.cs`
- `Assets/LearnHearthstone/Runtime/Adapters/Data/TrinketCatalogLoader.cs`
- `Assets/LearnHearthstone/Runtime/Adapters/Data/QuestCatalogLoader.cs`

## 状态口径

`Implemented` 表示该条目有可运行实现入口。

`Exact` 表示当前实现基本按现有系统能力还原，没有明显代理依赖。

`ProxySafe` 表示允许进入普通流程或可用于训练，但效果中有安全代理、随机替代、占位子卡、简化选择或系统能力缺口。

`HiddenEffectOnly` 表示效果实现存在，但不进入普通候选池，通常用于隐藏效果、平衡观察或等待官方子卡确认。

`DebugOnly` 表示只能用于调试或显式验证，不应该自然出现在普通选择池。

`Disabled` 表示实现路径存在，但因为官方占位内容、选择器或关键子系统未确认，不应该正常启用。

`FrameworkFirst` 表示只接入了数据和框架入口，具体效果尚未安全实现。

## 总览

### 饰品

- 总数：330
- `Implemented`：322
- 普通可选 `Offerable`：321
- `Exact`：276
- `ProxySafe`：46
- `FrameworkFirst` / `Blocked`：8
- `HiddenEffectOnly`：1

饰品的普通池基本可用，但仍有 8 个明确阻塞项，另有 46 个是近似或代理实现。它们不是“没做”，而是“当前系统中可安全跑，但官方一致性仍需补齐”。

### 任务

- 总数：6
- `Implemented`：6

任务目标本身已经全接入。需要注意的是，多数任务目标仍是 MVP 粒度统计，缺少官方动态难度、英雄/护甲/大厅环境等完整校准。

### 任务奖励

- 总数：73
- `Implemented`：73
- 显式 `Offerable`：5
- 未显式填写 `offerPoolStatus`，运行时按默认 `Offerable` 解析：26
- `HiddenEffectOnly`：20
- `DebugOnly`：20
- `Disabled`：2

任务奖励全都有实现入口，但并不是全部都应该进入普通任务奖励池。`HiddenEffectOnly`、`DebugOnly`、`Disabled` 主要用于隔离那些依赖官方子卡、伙伴、英雄技能、商店目标选择、Rally、Secret、placeholder-92 或复杂随机池的效果。

## 饰品：仍未完整实现

以下 8 个饰品仍是 `FrameworkFirst` + `DebugOnly` + `Blocked`。它们只完成了数据导入和共享饰品框架接入，具体效果仍未安全落地。

| ID | 名称 | 槽位 | 效果族 | 当前问题 |
| --- | --- | --- | --- | --- |
| `BG30_MagicItem_804` | Ancient Wishbone | Lesser | pending | 精确效果未实现，仍保留在调试池。 |
| `BG35_MagicItem_803` | Maxwell Sticker | Lesser | pending | 精确效果未实现，仍保留在调试池。 |
| `BG35_MagicItem_803t` | Maxwell Sticker | Greater | golden_triple | 精确效果未实现，仍保留在调试池。 |
| `BG35_MagicItem_801` | Sous Chef Sticker | Lesser | economy | 精确经济效果未实现，仍保留在调试池。 |
| `BG32_MagicItem_906` | Artanis Sticker | Greater | copy_generate | 复制/生成逻辑未确认，仍保留在调试池。 |
| `BG32_MagicItem_300` | Putricide Sticker | Lesser | turn_start | 回合开始效果未实现，仍保留在调试池。 |
| `BG30_MagicItem_707` | Tickatus Sticker | Lesser | turn_start | 回合开始效果未实现，仍保留在调试池。 |
| `BG35_MagicItem_812` | Corrupted Tome | Greater | discover | Discover 目标和官方行为未确认，仍保留在调试池。 |

另有 1 个饰品已经实现主体效果，但因为官方子卡缺失，暂不进普通池：

| ID | 名称 | 槽位 | 状态 | 当前问题 |
| --- | --- | --- | --- | --- |
| `BG35_MagicItem_702` | Stegodon Portrait | Lesser | HiddenEffectOnly + ProxySafe | 战斗开始圣盾效果已在战斗复制体上实现，但 “Get a Stomping Stegodon” 子卡当前 `relatedDbfId=0`，需要确认官方卡牌 ID 后才能进入普通池。 |

## 饰品：ProxySafe 清单

以下饰品已经有安全实现，但存在代理、占位、随机替代或系统简化。它们可以正常验证基础玩法，但不应声明为完全官方一致。

| ID | 名称 | 槽位 | 模糊点 |
| --- | --- | --- | --- |
| `BG32_MagicItem_301` | Bassgill Portrait | Greater | 需要时授予 Bassgill 代理，并用战斗召唤鱼人获得圣盾近似官方效果。 |
| `BG32_MagicItem_415` | Battle Horn | Greater | 装备时发现战吼随从，复仇触发使用已有战吼重放路径，仍是安全近似。 |
| `BG35_MagicItem_923` | Bewitched Ribbon | Greater | 酒馆法术永久强化酒馆随从，战斗内加成通过战斗复制体代理实现。 |
| `BG30_MagicItem_426t` | Colorful Compass | Greater | 使用当前酒馆等级随机随从作为 placeholder-92 代理。 |
| `BG30_MagicItem_402` | Conductor Portrait | Greater | 使用 Snarling Conductor 与弃牌事件代理血宝石统计。 |
| `BG35_MagicItem_925` | Coral Spear | Greater | 依赖现有酒馆法术解析器自动释放 Might of Stormwind。 |
| `BG32_MagicItem_807` | Curator Sticker | Greater | 使用 Golden Mishmash 和 Venomous Amalgam 代理。 |
| `BG30_MagicItem_916` | Essence of Dreams | Greater | 已接入共享框架，但备注仍标记精确效果待确认。 |
| `BG35_MagicItem_156` | Flaming Portrait | Greater | 批处理重写实现，指定随从/种族行为仍需官方核验。 |
| `BG32_MagicItem_367` | Ghastly Sticker | Greater | 批处理重写实现，回合结束行为仍需官方核验。 |
| `BG32_MagicItem_901` | Gold-plated Compass | Greater | 5 次免费刷新已实现，下一次购买随从变金使用 placeholder-92 代理。 |
| `BG32_MagicItem_925` | Hackerfin Portrait | Greater | 已接入共享框架，但备注仍标记精确效果待确认。 |
| `BG30_MagicItem_952` | Jarred Frostling | Greater | 战斗开始/死亡相关波次实现已完成，但仍属批处理代理类。 |
| `BG35_MagicItem_750` | Magicfin Sticker | Greater | 购买酒馆法术后生成 1/1 教授过该法术的鱼人代理卡。 |
| `BG32_MagicItem_205` | Maw Caster Portrait | Greater | 本地卡池缺官方肖像随从和生成法术，使用 Maw Caster 与 3 金币袋代理。 |
| `BG35_MagicItem_816t` | Orb of the Unknown | Greater | 随机可选 Greater 饰品替换代理，并附加 4 金币。 |
| `BG35_MagicItem_714` | Powder Keg | Greater | 战斗开始/死亡相关波次实现已完成，但仍属批处理代理类。 |
| `BG30_MagicItem_918` | Promo Portrait | Greater | 战斗开始/死亡相关波次实现已完成，但仍属批处理代理类。 |
| `BG35_MagicItem_820` | Safety Patch | Greater | 获得 5 金币是精确效果，Ice Block 因 Secret 系统缺失记录为代理。 |
| `BG35_MagicItem_740` | Sky Golem Portrait | Greater | 战斗开始/死亡相关波次实现已完成，但仍属批处理代理类。 |
| `BG35_MagicItem_823t` | Timeworn Candelabra | Greater | Discover 范围使用现有 Major Timewarp 代理随从池。 |
| `BG32_MagicItem_365` | Valdrakken Wind Chimes | Greater | 战斗开始/死亡相关波次实现已完成，但仍属批处理代理类。 |
| `BG30_MagicItem_425` | Azeroth Model Globe | Lesser | 已接入共享框架，但备注仍标记精确效果待确认。 |
| `BG32_MagicItem_806` | Battlecruiser Portrait | Lesser | 本地随从池缺官方卡，使用 Battlecruiser 与升级卡代理。 |
| `BG30_MagicItem_930` | Burgling Claw | Lesser | 从记录的上一个对手战队复制最高等级随从，依赖快照近似。 |
| `BG30_MagicItem_426` | Colorful Compass | Lesser | 使用当前酒馆等级随机随从作为 placeholder-92 代理。 |
| `BG30_MagicItem_435` | Goldenizer Supply | Lesser | 已接入共享框架，但备注仍标记精确效果待确认。 |
| `BG30_MagicItem_777` | Goose Portrait | Lesser | 批处理重写实现，野兽召唤统计仍需官方核验。 |
| `BG32_MagicItem_957` | Grifter Portrait | Lesser | 本地卡池缺 Doubloon Grifter，使用代理卡并实现每回合首次海盗购买免费。 |
| `BG32_MagicItem_950` | Gritty Portrait | Lesser | 已接入共享框架，但备注仍标记精确效果待确认。 |
| `BG32_MagicItem_820` | Impulsive Portrait | Lesser | 批处理重写实现，种族/指定随从行为仍需官方核验。 |
| `BG35_MagicItem_434` | Jewelry Box | Lesser | 已接入共享框架，但备注仍标记精确效果待确认。 |
| `BG35_MagicItem_817` | Lens Case | Lesser | 已接入共享框架，但备注仍标记精确效果待确认。 |
| `BG35_MagicItem_871` | Mama Bear Sticker | Lesser | 战斗中召唤野兽获得 +5/+5 已接入共享钩子，仍需官方边界核验。 |
| `BG30_MagicItem_973` | Minion Bait | Lesser | 每次刷新向酒馆加入两个当前等级 placeholder-92 代理随从。 |
| `BG30_MagicItem_703` | Mystery Cube | Lesser | 装备和回合开始提供免费 Lesser 饰品替换选择，属于替换代理。 |
| `BG35_MagicItem_816` | Orb of the Unknown | Lesser | 随机可选 Lesser 饰品替换代理。 |
| `BG30_MagicItem_917` | Rusty Trident | Lesser | 战斗开始/死亡相关波次实现已完成，但仍属批处理代理类。 |
| `BG30_MagicItem_407` | Ship in a Bottle | Lesser | 战斗开始/死亡相关波次实现已完成，但仍属批处理代理类。 |
| `BG30_MagicItem_888` | Souvenir Stand | Lesser | Greater 饰品装备后 Lesser 槽位复制为该 Greater 饰品，属于槽位转换代理。 |
| `BG35_MagicItem_702` | Stegodon Portrait | Lesser | 主体圣盾效果已实现，但官方子卡 ID 缺失，当前隐藏。 |
| `BG35_MagicItem_922` | Tide Raiser Portrait | Lesser | 增加 Tide Raiser，并在战斗后复制至多三次友方法术施放记录。 |
| `BG35_MagicItem_823` | Timeworn Candelabra | Lesser | Discover 范围使用现有 Minor Timewarp 代理随从池。 |
| `BG30_MagicItem_416` | Token of the Old Gods | Lesser | Spellcraft 变形为高一等级随机池内随从并保留当前身材，属于安全近似。 |
| `BG30_MagicItem_891` | Trip Vouchers | Lesser | 两回合后安排 Greater 饰品替换选择，属于时序代理。 |
| `BG30_MagicItem_994` | Yogg-Tastic Pastry | Lesser | 使用现有尤格萨隆转盘代理结果表。 |

## 任务：已完成但仍是 MVP 口径

任务目标 6 个均已实现，但仍有以下模糊点：

| ID | 名称 | 当前口径 |
| --- | --- | --- |
| `BG24_Quest_112` | Track the Footprints | 统计成功的手动酒馆刷新。免费刷新、自动刷新与官方计数边界仍需逐条核验。 |
| `BG24_Quest_126` | Follow the Money | 购买、刷新、升级和机制花费都会推进任务。官方是否包含所有消耗类型仍需验证。 |
| `BG24_Quest_311` | Cry for Help | 统计打出带战吼关键字的随从，未扩展到所有可能的官方“召唤/获得/触发战吼”边界。 |
| `BG24_Quest_313` | Invite the Guests | 统计购买随从，属于稳定 MVP。 |
| `BG24_Quest_314` | Dust for Prints | 统计加入手牌的卡，包括购买和生成卡，可能比官方口径更宽。 |
| `BG27_Quest_800` | Burn the Evidence | 该目标是 deleted/history-only。售卖事件保留给奖励和调试使用，但不应进入普通任务池。 |

另外，当前任务难度仍采用本地配置的等级、护甲与高血量修正。它能支撑训练，但还不是官方动态难度公式。

## 任务奖励：Disabled

这 2 个奖励实现路径存在，但不应正常启用。

| ID | 名称 | 当前问题 |
| --- | --- | --- |
| `BG27_Reward_812` | Scepter of Guidance | 官方 placeholder-92 内容未解析。当前直接激活会向酒馆填充两个安全随机随从代理。 |
| `BG24_Reward_134` | The Friends Along the Way | 官方 placeholder-92 内容未解析。当前回合开始使用当前等级随机随从作为安全代理。 |

## 任务奖励：DebugOnly

这些奖励已经有实现入口，但因为依赖子系统缺失、官方子卡未确认或交互 UI 不完整，当前只适合调试。

| ID | 名称 | 当前问题 |
| --- | --- | --- |
| `BG33_Reward_017` | Cosmic Reward | 可记录第二英雄技能，但 UI 与使用选择仍需扩展。 |
| `BG24_Reward_362` | Essence of Zerus | 使用代理 Zerus 卡，在回合开始变形成随机可用随从。 |
| `BG24_Reward_363` | Ethereal Evidence | 每回合提供两个普通池奖励作为即时额外选择，仍是代理流程。 |
| `BG24_Reward_130` | Ghastly Mask | 官方子卡未解析，当前重复友方随从回合结束处理一次。 |
| `BG27_Reward_802` | Gilnean War Horn | 官方子卡未解析，当前增加战吼重复次数。 |
| `BG33_Reward_015` | Jumbo Warehouse | 下一回合安排 Greater 饰品选择并先给 4 金币，属于计划内调试实现。 |
| `BG24_Reward_718` | Kidnap Sack | 当前命令模型缺商店目标选择器，代理为移动第一个非金色酒馆卡。 |
| `BG33_Reward_011` | Magicfin Relic | 代理召唤 1/1 鱼人，并启动酒馆法术发现后自动施放到它身上。 |
| `BG33_Reward_010` | Norgannon's Reward | 当前最大酒馆等级已是 7，奖励只安排下一回合免费自动升级。 |
| `BG28_Reward_513` | Open Auditions | 每回合发现伙伴，但伙伴映射与 UI 仍需扩展。 |
| `BG24_Reward_310` | Partner in Crime | 完成后按英雄伙伴映射给金色伙伴，映射缺失时无法完整执行。 |
| `BG33_Reward_014` | Quaint Boutique | 下一回合安排 Lesser 饰品选择并先给 4 金币，属于计划内调试实现。 |
| `BG33_Reward_021` | Rallying Cry | Rally 子系统尚不存在，当前直接激活只是安全被动占位。 |
| `BG28_Reward_510` | Secret Culprit | 官方子卡未解析，当前给随机可用 Tier 7 随从或最高等级兜底。 |
| `BG24_Reward_719` | The Golden Hammer | 生成法术可临时金化友方随从并下回合回退，但目标交互仍是简化模型。 |
| `BG27_Reward_504` | Timeline Acceleration | 生成法术替换友方随从为高一等级随机可用随从，属于目标法术代理。 |
| `BG27_Reward_803` | Turbulent Tombs | 官方子卡未解析，战斗中额外亡语触发已实现。 |
| `BG28_Reward_514` | Untamed Sorcery | 随机释放可用酒馆法术，目标通过自动友方兜底。 |
| `BG24_Reward_313` | Wondrous Wisdomball | 每 3 次刷新应用一次 Knockoff Wisdomball 风格帮助刷新，不是完整官方 Wisdomball。 |
| `BG24_Reward_135` | Yogg-tastic Tasties | 使用确定性随机代理结果表解析经济、强化、刷新、法术或随从结果。 |

## 任务奖励：HiddenEffectOnly

这些奖励效果已实现，但当前不进入普通奖励池。它们通常需要平衡校验、隐藏池语义确认或官方边界验证。

| ID | 名称 | 需要注意的点 |
| --- | --- | --- |
| `BG24_Reward_321` | Alter Ego | 回合开始交替强化偶数/奇数酒馆随从，需确认官方刷新与冻结边界。 |
| `BG24_Reward_708` | Blood Goblet | 使用最大生命值减当前生命值，护甲不计入缺失生命值。 |
| `BG27_Reward_502` | Boom Squad | CombatEngine 追踪复仇 3 并伤害最高生命值敌方随从，需实战校验。 |
| `BG27_Reward_815` | Endless Blood Moon | 完成时强化鲜血宝石，回合开始发宝石由持续奖励处理。 |
| `BG24_Reward_715` | Enhance-a-matic | 每回合生成增强零件，需确认零件池与官方一致性。 |
| `BG24_Reward_123` | Exquisite Conch | 每回合第一次战吼额外触发两次，边界依赖战吼重复解析。 |
| `BG33_Reward_004` | Grim Freshener | CombatEngine 追踪复仇 2 并排队免费刷新奖励。 |
| `BG27_Reward_810` | Map of the Unknown | 打出补齐新类型的随从时触发，混合类型边界需核验。 |
| `BG24_Reward_331` | Menagerie Mayhem | 根据友方不同随从类型数量强化场面，混合类型计数需核验。 |
| `BG24_Reward_128` | Mirror Shield | 成功刷新后随机强化一个酒馆随从并给圣盾，冻结/刷新边界需核验。 |
| `BG24_Reward_131` | Red Hand | 回合开始随机强化一个手牌随从，属于隐藏效果池。 |
| `BG33_Reward_003` | Righteous Charge | 使用现有立即攻击 pending tag，在战斗前准备左侧圣盾随从攻击。 |
| `BG33_Reward_006` | Rushing Winds | 回合开始生成 Spellcraft，目标与持续时间仍需核验。 |
| `BG24_Reward_712` | Sinfall Medallion | 打出随从后按其酒馆等级强化最多两个其他友方随从。 |
| `BG24_Reward_312` | Staff of Origination | 通过下一场战斗面板加成实现战斗开始强化。 |
| `BG27_Reward_804` | Sturdy Shard | 统计友方嘲讽随从并强化非嘲讽随从，类型边界需核验。 |
| `BG24_Reward_125` | The Smoking Gun | 完成时强化当前场面，并为战斗复制体和召唤物提供攻击光环。 |
| `BG24_Reward_115` | Theotar's Parasol | 回合结束给最右侧随从潜行和生命值，下个友方回合开始移除临时潜行。 |
| `BG28_Reward_505` | Tumbling Disaster | 强化战斗召唤物，并按复仇 4 提升存储强化值，官方增量仍需实战验证。 |
| `BG24_Reward_364` | Volatile Venom | 战斗光环已实现，友方攻击者攻击结算后死亡，需确认与圣盾/复生等交互。 |

## 任务奖励：默认 Offerable 但仍有 MVP 或近似备注

以下奖励会按默认 `Offerable` 解析，但备注中明确带有 MVP、简化或本地规则口径。它们是可用实现，不是阻塞项。

| ID | 名称 | 当前口径 |
| --- | --- | --- |
| `LH_Reward_CoinPouch8` | 8-Gold Coin Pouch | Shady Aristocrat 的 MVP 金币奖励。 |
| `LH_Reward_CoinPouch16` | 16-Gold Coin Pouch | Golden Shady Aristocrat 的 MVP 金币奖励。 |
| `BG24_Reward_306` | Cooked Book | 购买随从后强化手牌中的该随从，并每次触发提升 +1/+1。 |
| `BG24_Reward_361` | Hidden Treasure Vault | 回合开始给金币并提升未来金币量，属于 MVP 口径。 |
| `BG24_Reward_136` | Tiny Henchmen | 回合结束强化最多三个 3 级或以下友方随从，属于 MVP 口径。 |
| `BG33_Reward_012` | Untold Riches | 立即给金币并提高最大金币，属于 MVP 口径。 |

## 共性缺口

当前所有不完整或模糊实现主要集中在这些基础能力上：

1. 官方子卡和派生卡缺失：多个效果依赖 unresolved child copy、`relatedDbfId=0` 或 placeholder-92。
2. Hero Power 与 Buddy 系统未完全闭环：第二英雄技能、伙伴发现、金色伙伴奖励需要 UI、映射与使用流程补齐。
3. 目标选择能力不足：商店目标、友方目标、法术自动施放仍有自动兜底或第一目标代理。
4. Rally 子系统缺失：相关奖励只能作为安全被动占位。
5. Secret 系统缺失：Ice Block 之类效果只能记录为代理。
6. Timewarp、Yogg、Wisdomball 等复杂随机池仍使用本地代理表或简化触发频率。
7. Tier 7 与特殊卡池仍依赖本地可用卡池兜底，无法保证官方池完全一致。
8. 任务难度公式仍是本地配置，不是官方动态公式。
9. HiddenEffectOnly 奖励需要进一步确认是否应该进入普通奖励池，以及以什么权重进入。

## 后续建议

优先级最高的是补齐 8 个 `Blocked` 饰品，因为它们是唯一仍未实现具体效果的饰品。

第二优先级是确认 placeholder-92、官方子卡、Timewarp 池和特殊生成卡 ID。它们会同时解锁多个饰品和任务奖励。

第三优先级是补齐交互基础设施，包括商店目标选择、友方目标选择、英雄技能选择/使用、伙伴映射、Rally 和 Secret。

最后再做官方一致性核验，包括任务进度边界、冻结/刷新边界、混合类型计数、战斗中召唤/死亡/复生交互，以及任务奖励权重和池配置。
