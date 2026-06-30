# 英雄机制剩余补齐顺序

Date: 2026-06-29

## 目的

本文是 [HeroPowerBuddyEffectsImplementationOrder.md](HeroPowerBuddyEffectsImplementationOrder.md) 的当前补充版，用来回答“现在还有哪些英雄和机制没补齐，以及应该按什么顺序补”。

它不替代 [HeroAndBuddyImplementationProcess.md](HeroAndBuddyImplementationProcess.md) 的接入流程，也不替代 [HeroEffectImplementationGaps.md](HeroEffectImplementationGaps.md) 的缺陷总表。真正开工时，应按本文决定顺序，再回到流程文档补数据、注册、运行时、UI 和测试。

## 当前基线

事实来源：[HeroEffectImplementationRegistry.cs](../Assets/LearnHearthstone/Runtime/Domain/Data/HeroEffectImplementationRegistry.cs)

截至 2026-06-29，注册表状态统计如下：

| 状态 | 数量 | 含义 |
| --- | ---: | --- |
| Implemented | 59 | 英雄技能和关联宝宝已按当前项目能力完成。 |
| FrameworkFirst | 33 | 已有一部分效果或代理实现，但还依赖公共框架、战斗事件、真实大厅信息或专属机制补齐。 |
| Planned | 20 | 已明确实现目标，但主体逻辑尚未完成。 |
| Deferred | 2 | 当前必须等公共底座补完后再做。 |
| Unregistered | 1 | 注册表 fallback 状态，不代表一个具体英雄。 |

当前真正需要继续补的主要是：

- `Planned`：20 个，优先处理，因为目标清楚，很多不需要大改公共框架。
- `FrameworkFirst`：33 个，按缺失公共机制分批收尾，不能逐个硬塞临时逻辑。
- `Deferred`：2 个，必须等对应底座完成后再开。

## 排序原则

1. 先补酒馆阶段、回合阶段、发现、刷新、金币、延迟奖励这类低依赖英雄。
2. 先补公共机制，再批量关闭依赖同一机制的 `FrameworkFirst` 英雄。
3. 每个英雄技能和对应宝宝作为同一个交付单元处理，不只补英雄技能。
4. 涉及真实 8 人大厅的信息，在单人酒馆里要明确代理边界，不能假装完全等同官方大厅。
5. 涉及选择 UI、目标选择、临时英雄技能、第二英雄技能时，要同时补命令流和 UI smoke test。
6. 每批完成后必须更新 `HeroEffectImplementationRegistry` 状态和说明，避免“代码已做但文档仍写 Planned”。

## 不应误判为缺陷的边界

以下规则需要在后续实现中继续保持，不要把它们误写成缺陷：

- 单人酒馆没有真实共享大厅，因此真实对手、投票、淘汰、预测类机制可以先使用快照或代理规则，但文档和 UI 要标明边界。
- 伙伴相关畸变或英雄效果应使用伙伴发现池，不应把所有英雄宝宝无条件塞进普通酒馆池。
- 时空酒馆进入酒馆的应是扭曲时空随从；时空效果牌、法术不进入普通酒馆是正确规则。
- 饰品相关英雄或畸变的核心问题不是“有代理饰品就一定错误”，而是复制、候选、发现、复制目标要遵守官方排除规则。

## 第一批：酒馆阶段低依赖 Planned

目标：先拿下不强依赖战斗事件、不依赖真实多玩家大厅、不依赖大型专属系统的英雄。

建议顺序：

1. Infinite Toki
2. Snake Eyes
3. Alexstrasza
4. A. F. Kay
5. Guff Runetotem
6. Galewing
7. Cariel Roame

### Infinite Toki

状态：已完成（2026-06-30）。`Temporal Tavern` 复用当前酒馆刷新池并优先抽取两个高一等级随从；`Clockwork Assistant` 战吼复用高一等级发现，标准单人局 6 本时截断到 6 本。

需要实现：

- 英雄技能刷新酒馆时，生成两个比当前酒馆等级高 1 级的随从。
- 不能越过当前可用随从池和禁用种族过滤。
- Clockwork Assistant 的战吼发现也应从高 1 级随从池取牌。

为什么放第一批：主要依赖刷新和发现池，风险集中在候选池过滤，不需要真实战斗事件。

### Snake Eyes

需要实现：

- 投骰获得金币。
- 英雄技能冷却按骰子点数变化。
- Box Cars 在回合开始发现与上次骰子点数或记录等级相关的酒馆法术。

为什么放第一批：核心是随机数、金币、冷却、回合开始触发，都是已有系统容易承接的点。

### Alexstrasza

需要实现：

- 升到 4 级酒馆时触发龙牌发现。
- 发现池只取合法龙随从。
- Vaelastrasz 的 Rally 随机龙奖励。

为什么放第一批：逻辑清晰，主要是酒馆等级监听和随从池过滤。

### A. F. Kay

需要实现：

- 前两回合跳过或限制可执行操作。
- 到对应时点后发放 3 级和 4 级随从发现。
- Snack Vendor 回合结束把属性转移给一个 3 级随从。

为什么放第一批：需要回合限制和延迟奖励，但不依赖复杂战斗解析。

### Guff Runetotem

需要实现：

- 统计购买卡牌的酒馆等级总和。
- 达成阈值后给三连奖励。
- Baby Kodo 战吼刷新酒馆，保证包含各等级候选。

为什么放第一批：主要补购买事件统计和奖励发放，复杂度中等但边界明确。

### Galewing

需要实现：

- 航线选择状态。
- 延迟若干回合后发放路线奖励。
- 路线选择避免连续重复。
- Flight Trainer 触发双倍路线奖励或额外路线结算。

为什么放第一批：依赖选择 UI 和延迟奖励，但不需要战斗内部事件。

### Cariel Roame

需要实现：

- 战斗后 Conviction 升级选择或成长状态。
- Captain Fairmount 的随机回合结束强化。
- 如果选择 UI 暂不完整，至少要使用统一的 pending choice 命令流，不要写死自动选择。

为什么放第一批：有战斗后时点，但不需要解析具体攻击、死亡或召唤。

第一批完成标准：

- 这 7 个英雄从 `Planned` 更新为 `Implemented` 或明确的 `FrameworkFirst`。
- 每个英雄至少有 focused edit-mode/unit test。
- 有选择 UI 的英雄要补一次 Unity Trainer 或 UI smoke test。

## 第二批：开局选择、延迟奖励、外部奖励池

目标：补开局发现、延迟到指定酒馆等级发奖、任务和暗月奖品链路。

建议顺序：

1. Ambassador Faelin
2. Thorim, Stormlord
3. Sire Denathrius
4. Tickatus
5. Yogg-Saron, Hope's End

### Ambassador Faelin

需要实现：

- 开局发现 6 级、4 级、2 级随从。
- 选择结果延迟到玩家达到对应酒馆等级时发放。
- 第一回合限制或跳过规则。
- Submersible Chef 发放 1/3/5 级随机随从奖励。

注意：延迟奖励必须落到可序列化状态里，避免重进局或刷新 UI 后丢失。

### Thorim, Stormlord

需要实现：

- 开局 7 级随从发现。
- 记录已选 7 级随从。
- 统计花费金币，到 60 金币后发放。
- Veranus 回合结束使左侧相邻随从升一级或转换到更高等级候选。

注意：7 级池、禁用种族、随从合法性要单独验证。

### Sire Denathrius

需要实现：

- 任务和任务奖励数据接入。
- 开局任务选择。
- 任务进度与奖励激活。
- Shady Aristocrat 出售发现任务，并在完成后发放 8 金币 Coin Pouch。

注意：应复用任务系统底座，不要给 Denathrius 单独写一套任务状态。

### Tickatus

需要实现：

- 每 4 回合暗月奖品调度。
- 按回合或等级选择正确奖品池。
- Ticket Collector 出售时发现下一级暗月奖品。

前置要求：暗月奖品 1/2/4 级目前仍有大量 proxy。可以先实现调度，但要在 UI 或日志里保留 `darkmoon_prize_proxy` 标记；若要把 Tickatus 视为完全实现，应先补齐暗月奖品低级和高级卡牌效果。

### Yogg-Saron, Hope's End

需要实现：

- 第 3 回合或指定时点解锁。
- 回合开始随机施放酒馆法术。
- Acolyte of Yogg-Saron 的 Wheel of Yogg-Saron 结果。

注意：随机施放要走统一法术执行入口，不能绕过目标、费用、候选过滤规则。

第二批完成标准：

- 开局选择、延迟奖励、任务、暗月奖品调度都有持久化状态。
- 奖励池必须能解释为什么某张牌进入或不进入候选。
- Tickatus 完成度要区分“奖品调度完成”和“所有暗月奖品卡牌效果精确实现”。

## 第三批：临时/轮换英雄技能与第二英雄技能安全性

目标：补齐会改变英雄技能槽位、临时替换英雄技能、或依赖多英雄技能 UI 的机制。

建议顺序：

1. The Rat King
2. Master Nguyen
3. Genn, Worgen King
4. Cosmic Duality 相关候选过滤复查

### The Rat King

需要实现：

- 英雄技能随回合或刷新轮换随从类型。
- 当前类型的发现池。
- Pigeon Lord 在酒馆没有当前类型时给免费刷新。

注意：轮换类型必须受当前局可用种族约束。

### Master Nguyen

需要实现：

- 回合开始临时英雄技能选择。
- 当前回合临时替换。
- 回合结束清理。
- Lei Flamepaw 根据当前英雄技能映射获得对应宝宝。

注意：这是 Cosmic Duality 之后最需要关注 UI/命令流的英雄。必须确认临时英雄技能能展示、可点击、可使用，并在回合结束恢复。

### Genn, Worgen King

需要实现：

- 多英雄技能替换时机。
- 与 Cosmic Duality、Finley、Nguyen 这类英雄技能替换/复制逻辑的优先级。
- UI 中多个英雄技能槽位的稳定展示。

为什么放第三批：它现在是 `Deferred`，原因就是多英雄技能替换时机还没有统一底座。先补 Rat King 和 Nguyen，可以顺手验证该底座。

### Cosmic Duality 相关候选过滤复查

Cosmic Duality 本身已经完成第二英雄技能授予和 UI 命令流，但后续补英雄技能时还要复查：

- 第二英雄技能候选中是否包含 `Planned`、`FrameworkFirst`、`Deferred` 技能。
- 是否需要在选择 UI 中标记“不完整实现”或过滤掉无法运行的技能。
- 当第二技能本身会替换英雄技能时，是否会覆盖主技能或造成重复槽位。

第三批完成标准：

- 临时英雄技能、第二英雄技能、多英雄技能替换使用同一套状态模型。
- 回合开始、回合结束、重进 UI 后展示一致。
- Unity Trainer smoke test 覆盖选择、展示、解锁、点击和目标选择。

## 第四批：对手历史、预测和真实大厅信息

目标：把依赖真实多玩家大厅的信息集中处理，不再分散在单个英雄里各写一套代理。

建议顺序：

1. Murloc Holmes
2. Lord Barov
3. Mr. Bigglesworth
4. Scabbs Cutterbutter
5. Tess Greymane
6. Rafaam

### Murloc Holmes

需要实现：

- 选择 UI：猜测对手上一场战斗相关信息。
- 下一对手或代理对手的上一场战斗记忆。
- 猜对后发 Tavern Coin。
- Watfin 在猜对后给普通复制。

单人酒馆边界：没有真实 8 人对局时，可以用当前代理对手或最近战斗快照，但必须明确这是单人规则。

### Lord Barov

需要实现：

- 战斗预测选择 UI。
- 战斗后根据胜负结算。
- 猜中后给 3 个 Tavern Coin。
- Barov's Apprentice 对打出 Coin 的金币触发。

注意：预测目标和战斗结果必须来自同一场战斗快照，不能跨回合串数据。

### Mr. Bigglesworth

需要实现：

- 淘汰玩家战队快照。
- 最低血量或淘汰对象选择规则。
- 从被淘汰玩家战队发现随从。
- Lil' K.T. 从对手战队代理中获得普通随从。

单人酒馆边界：真实淘汰不存在时，可以保留淘汰快照代理，但不要标为完全官方实现。

### Scabbs Cutterbutter

需要实现：

- 下一对手战队快照。
- 从下一对手战队发现普通复制。
- Warden Thelwater 获得该对手的宝宝。

注意：真实下一对手调度未完成前，应继续标记为 `FrameworkFirst`。

### Tess Greymane

需要实现：

- 上一对手战队快照。
- Bob's Burgles 用上一对手战队刷新酒馆。
- Hunter of Old 获得上一对手宝宝。

注意：单人酒馆可以使用上一场代理对手，但要和 Scabbs 共用对手快照模型。

### Rafaam

需要实现：

- 记录下一次战斗中第一个死亡的敌方随从。
- 战斗后给该随从普通复制。
- 宝宝效果依赖同一套敌方死亡历史。

第四批完成标准：

- 有统一的 opponent snapshot / last combat memory / eliminated player snapshot。
- UI 中真实大厅不可用时不会静默失败。
- 单人代理规则写入注册表说明和测试名。

## 第五批：战斗事件公共框架收口

目标：先补公共战斗事件，再批量关闭依赖战斗内部事件的 `FrameworkFirst` 和 `Planned` 项。

需要优先补的公共能力：

- 友方攻击次数统计。
- 友方造成击杀的归因，包括攻击击杀和非攻击伤害击杀。
- 友方随从死亡历史和死亡时属性快照。
- 敌方随从死亡历史。
- 亡语 payload、亡语复制、亡语召唤和亡语记忆。
- 复仇计数和复仇召唤。
- 战斗中召唤事件统一 resolver。
- 立即攻击队列。
- 受到伤害、造成伤害、嘲讽被攻击等监听点。

建议优先关闭的英雄：

1. Tavish Stormpike
2. Tamsin Roame
3. Teron Gorefiend
4. N'Zoth
5. Sneed
6. The Jailer
7. Greybough
8. Onyxia
9. Illidan Stormrage
10. Rokara
11. Sylvanas Windrunner
12. Lord Jaraxxus
13. Bru'kan
14. Loh, the Living Legend
15. Dinotamer Brann

相关 `FrameworkFirst` 可在同批复查：

- Al'Akir
- Deathwing
- Ini Stormcoil
- Ozumat
- Aranna Starseeker

### Loh, the Living Legend

需要实现：

- 友方攻击次数统计，达到条件后给三连奖励。
- Stoneshell Guardian 修改每回合第一个三连奖励，从金色随从池发现。

注意：它应跟攻击计数和三连奖励修改一起做，不建议单独硬写。

### Dinotamer Brann

需要实现：

- 统计购买战吼随从。
- 达成条件后给 Brann Bronzebeard。
- Brann's Epic Egg 的嘲讽亡语召唤和随机战吼随从奖励。

注意：它横跨购买统计和亡语召唤，适合在战斗事件底座稳定后做。

第五批完成标准：

- 战斗事件由 CombatEngine 或统一事件总线发出，不在英雄逻辑里反推战斗结果。
- 同一事件源能同时服务英雄技能、宝宝、任务、饰品和畸变。
- 每个英雄测试至少覆盖一次真实战斗结算路径。

## 第六批：专属大机制和跨系统英雄

目标：处理需要专属子系统的英雄。这些不应插在前面批次里零散实现，否则后续会返工。

建议分组：

1. Lady Vashj / Queen Azshara：Spellcraft、Naga、临时法术、友方法术复制。
2. Marin the Manager / Buttons：饰品选择、复制候选、排除规则和大/小饰品槽位。
3. Mister Clocksworth：两张即可合金、三连奖励替换为 Tavern Coin。
4. The Great Akazamzarak：Secret 战场支持和 Better Secret。
5. Professor Putricide：自定义 Undead 制作。
6. Jim Raynor / Artanis / Kerrigan：Terran、Protoss、Zerg 专属机制。
7. Morchie / Murozond：时空酒馆扩展和对手历史扩展。

### Lady Vashj / Queen Azshara

需要实现：

- Spellcraft 临时法术生成。
- 回合结束清理。
- Naga Conquest 或战队总攻击阈值状态。
- Imperial Defender 的每回合一次友方法术复制。
- Coilfang Elite 复制酒馆 Spellcraft 随从提供的法术。

注意：不要把 Spellcraft 当普通永久手牌处理。

### Marin the Manager / Buttons

需要实现：

- Marin 的饰品选择系统。
- Buttons 的大饰品选择系统。
- 饰品复制、候选、发现、排除规则。
- 与当前已实现饰品池的 `Exact`、`ProxySafe`、`Blocked/DebugOnly` 状态联动。

注意：这里要落实“有些饰品不应进入复制路径”的规则。

### Mister Clocksworth

需要实现：

- TripleEngine 支持两张同名即可合成金色。
- 原三连奖励替换为 Tavern Coin。
- 与普通三连、金色随从、三连奖励 UI 不冲突。

这是 `Deferred`，应等 TripleEngine 可配置化后再做。

### The Great Akazamzarak

需要实现：

- Secret 选择、挂载、触发和移除。
- Street Magician 的 Better Secret。
- 战斗阶段 Secret 触发时点。

注意：当前 Better Secret proxy 可以保留，但不能标为完整 Secret 系统。

### Professor Putricide

需要实现：

- 自定义 Undead 制作 UI 或命令流。
- 组件池、费用、结果随从生成。
- Festergut 当前 proxy 与正式 Undead Creation 的迁移。

### Jim Raynor / Artanis / Kerrigan

需要实现：

- Terran/Battlecruiser 升级链。
- Protoss 延迟奖励系统。
- Zerg morphing tiers。
- 对应宝宝从 proxy 迁移到正式系统。

### Morchie / Murozond

需要实现：

- Minor/Major Timewarped Tavern 已可打开的部分继续保留。
- 扩展对手历史、时间线快照和时空奖励。
- 确认时空酒馆只将时空随从进入酒馆，效果牌和法术按规则留在对应奖励/效果入口。

第六批完成标准：

- 每个专属机制有自己的数据、候选池、执行入口和测试。
- 不再用单个 hero effect 方法承载整套子系统。
- proxy 留存时必须有显式状态和后续替换点。

## 每批通用完成标准

每完成一个英雄或一批英雄，都要做以下检查：

1. 更新 `HeroEffectImplementationRegistry` 的状态、阶段和说明。
2. 更新相关缺陷文档或实现顺序文档。
3. 添加 focused 测试，覆盖正常路径和至少一个边界路径。
4. 涉及 UI 选择、目标、英雄技能槽位、临时英雄技能时，跑一次 Unity Trainer 或 UI smoke test。
5. 确认候选池不会静默包含完全不可执行的卡；如果允许 proxy，必须保留状态标记。
6. 确认单人酒馆代理规则不会伪装成真实 8 人大厅规则。
7. 确认英雄技能和宝宝效果一起验收。

## 下一步建议

最稳的下一步是从第一批开始，优先做 Infinite Toki 或 Snake Eyes。

建议执行顺序：

1. 先为 Cosmic Duality 和 BuddyPool 生成一次候选状态报告，确认第二英雄技能和伙伴发现池不会把完全不可执行项静默放给玩家。
2. 实现 Infinite Toki，验证刷新、发现池和高 1 级候选过滤。
3. 实现 Snake Eyes，验证金币、冷却、骰子状态和 Box Cars 回合开始奖励。
4. 在 Tickatus 前补暗月奖品 1/2/4 级 proxy，或者明确把 Tickatus 标为“调度完成、奖品效果未全精确”。
5. 在 Genn 前补多英雄技能替换时机，避免和 Cosmic Duality、Finley、Master Nguyen 互相覆盖。
