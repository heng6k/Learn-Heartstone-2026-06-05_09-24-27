# FrameworkFirst 英雄收口五类计划

更新日期：2026-07-04

## 目标

这份文档最初处理 `HeroEffectImplementationRegistry` 中仍为 `FrameworkFirst` 的 35 个英雄/宝宝组合。2026-07-04 已完成前 4 个决策项后，Tavish、Tamsin、Onyxia、Bru'kan 转为 `Implemented`；随后 Morchie、Scabbs、Tess 也按当前项目完成标准转为 `Implemented`。本轮继续完成 Galewing、Yogg 与 Deathwing，并把 Loh、Dinotamer Brann 从 Planned 转为 `Implemented`，当前剩余 `FrameworkFirst` 为 24 个。文档继续把剩余项归入四个技术收口类别，并维护一个横切的决策队列：

1. 可直接转正/补测试
2. 需要战斗事件底座
3. 需要真大厅代理
4. 需要独立大机制
5. 需要你决策的

前四类是互斥技术归属；第 5 类是横切阻塞项，可能引用前四类里的英雄或机制，用来标记没有你拍板就不该继续推进的地方。

`Planned` 和 `Deferred` 不在本文件范围内。2026-07-04 修补后，Loh 与 Dinotamer Brann 已完成；当前剩余主要是战斗事件泛化、真实大厅代理和独立大机制英雄。

## Confidence Check

针对“完成四个技术收口类和一个决策队列，并给出执行路线”：置信度 0.95。

- 未发现第二套英雄状态登记源；`HeroEffectImplementationRegistry.cs` 仍是收口真源。
- 现有计划文档已覆盖 P3 候选策略、P4 对手历史、P5 战斗事件、P6 独立大机制。
- 官方/API 文本已在 `HeroPowerProxyCandidateImplementationPlan.md` 中覆盖普通可发现英雄技能候选；P6 独立机制仍需在实现各子系统前逐项补官方/API 验证。
- 根因清楚：`FrameworkFirst` 不是“没做”，而是“已有可玩代理或局部实现，但缺少官方完整数据、公共事件源、真大厅状态或专属子系统”。

针对“一次性把 35 个全部改成 official-complete runtime”：置信度约 0.45，不应执行。原因是它横跨 Secret、Trinket、StarCraft、Timewarped、真大厅对手历史、战斗事件和 Deathrattle payload，单批硬做会制造重复临时系统。

## 总览

| 类别 | 数量 | 收口原则 |
| --- | ---: | --- |
| 可直接转正/补测试 | 0 | A1 四个已转 `Implemented`；Morchie、Galewing、Yogg 已转 `Implemented`。 |
| 需要战斗事件底座 | 17 | 先补 CombatEngine 事件源、payload、死亡/召唤/攻击/伤害记录，再逐批收口。 |
| 需要真大厅代理 | 1 | Bigglesworth 仍需要淘汰/最低血量；Scabbs/Tess 已按单人模拟对手代理转正。Murozond 英雄本体已按 Turn 8 Major Timewarped Tavern 转正，timeline/previous-warband 属于 Timewarped Tavern 牌/数据底座。 |
| 需要独立大机制 | 7 | Secret、Trinket、Undead Creation、StarCraft 子系统先落地，再迁移英雄代理。 |
| 需要你决策的 | 0 | 前 4 个决策项已落地；Yogg Wheel 已按用户确认的项目五格表作为当前完成标准。 |

## 1. 可直接转正/补测试

这些不需要新增大底座。收口动作主要是补缺失数据、补更明确的回归测试、补 UI smoke，或由项目确认“当前代理足以视为完成”。

| 英雄 | 当前已完成 | 剩余动作 | 建议优先级 |
| --- | --- | --- | --- |
| Tavish Stormpike | Deadeye 目标记录、战斗开始伤害/移除、有空位直接发射/结算、Crabby 普通复制已接通。 | 已按项目语义转 `Implemented`；目标 UI polish 作为产品跟进，不再阻塞英雄/宝宝完成状态。 | A1 已完成 |
| Tamsin Roame | 战斗开始给最低攻击随从挂属性共享 Deathrattle，Monstrosity 友方死亡成长已接通。 | 已转 `Implemented`；后续通用 Deathrattle payload 命名/框架增强不阻塞本体状态。 | A1 已完成 |
| Onyxia | Avenge(4) 召唤 Whelp、立即攻击、Many Whelps 成长已接通。 | 已转 `Implemented`；后续通用 Avenge/立即攻击框架增强不阻塞本体状态。 | A1 已完成 |
| Bru'kan | 四元素选择、战斗开始调用、Spirit Raptor 记忆/亡语重放已接通。 | 已转 `Implemented`；本地四元素 baseline 被接受为当前完成标准。 | A1 已完成 |
| Morchie | Turn 5 打开 Minor Timewarped Tavern 已接通，且已有 focused Timewarped Tavern 覆盖。 | 已转 `Implemented`；无宝宝映射。 | A2 已完成 |
| Galewing | 航线选择、延迟完成、不连续重复、Flight Trainer 双触发和官方三航线奖励已接通。 | 已转 `Implemented`。 | A3 已完成 |
| Yogg-Saron | Puzzle Box Turn 3 起自动施放合法随机 Tavern spell；Acolyte 项目五格 Wheel 表已接通。 | 已转 `Implemented`；官方 public API 未展开 Wheel 子结果，当前以用户确认表为项目完成标准。 | A3 已完成 |

### 直接收口执行线

1. A1 批次：Tavish、Tamsin、Onyxia、Bru'kan 已完成状态清理并转 `Implemented`，保留 focused validation 作为回归。
2. A2 批次：Morchie 已完成。Turn 5 Minor Timewarped Tavern 行为已有 focused 覆盖，registry 已单独转正。
3. A3 批次：Galewing、Yogg 已完成。Galewing 使用 Westfall/Ironforge/Eastern Plaguelands 奖励表；Yogg 使用用户确认的五格 Wheel 表。

## 2. 需要战斗事件底座

这些英雄目前最大问题不是单个分支没写，而是缺统一战斗事件：攻击次数、击杀归属、死亡快照、Deathrattle payload、召唤位置、伤害统计、Taunt 被攻击监听、战斗开始小顺序。

| 英雄 | 主要缺口 | 先补底座 |
| --- | --- | --- |
| Shudderwock | 更广泛官方 Battlecry 覆盖、二级目标选择。 | Battlecry resolver 能表达多目标和失败原因。 |
| Vol'jin | Master Gadrin 的精确战斗开始左邻 hook。 | 战斗开始 hero/buddy 小顺序和邻位快照。 |
| Al'Akir | Spirit of Air 仍依赖 Tavern death proxy。 | 战斗 Deathrattle dispatch 到英雄宝宝。 |
| Illidan Stormrage | 通用友方攻击计数、更完整立即攻击排序。 | 攻击开始/结束事件和 tagged immediate attack 顺序。 |
| N'Zoth | Fish 收集死亡随从 Deathrattle。 | Deathrattle payload 转移和死亡快照。 |
| Teron Gorefiend | 精确死亡触发时序。 | 战斗开始摧毁、死亡事件、空位复活 exact copy。 |
| Arch-Villain Rafaam | 非攻击击杀归属、完整敌方坟场。 | 击杀归属覆盖攻击/反击/法术/亡语/召唤物。 |
| Rokara | 非攻击击杀来源。 | 友方击杀归属和永久写回。 |
| Sylvanas Windrunner | 上场死亡历史 Discover。 | last-combat death history 和 Discover 队列。 |
| Sneed | 手牌召唤 Deathrattle、Whirl-O-Tron 复制 Deathrattle。 | Deathrattle payload、召唤位置、复制规则。 |
| The Jailer | Mawsworn Soulkeeper 仍依赖 Tavern death proxy。 | 战斗 Deathrattle dispatch 和死亡计数回写。 |
| Greybough | Wandering Treant 的 Taunt 被攻击触发和永久 board-wide buff。 | Taunt 被攻击监听和 combat summon modifier。 |
| Ini Stormcoil | MechGyver 需要友方战斗死亡计数并奖励机械。 | friendly combat death counter 和奖励 resolver。 |
| Ozumat | 任意未来召唤源仍需共享 resolver。 | 战斗召唤统一 resolver 和实时棋盘回写。 |
| Aranna Starseeker | 英雄技能解锁需要友方攻击计数。 | friendly attack counter。 |
| Lord Jaraxxus | Bloodfury 需要友方伤害累计和传送门奖励。 | friendly damage dealt counter 和 portal reward 表。 |

### 战斗底座执行线

1. B1：统一 `CombatEventRecord` 测试夹具，覆盖 attack、kill、death、summon、damage。
2. B2：攻击计数和立即攻击顺序，收口 Illidan、Aranna、Vol'jin 的最小闭环。
3. B3：击杀归属和死亡历史，收口 Rafaam、Rokara、Sylvanas、Ini。
4. B4：Deathrattle payload 和召唤位置，收口 N'Zoth、Sneed、Teron、Jailer、Al'Akir。
5. B5：伤害/嘲讽监听，收口 Jaraxxus、Greybough。

## 3. 需要真大厅代理

这些在单人训练器里可以继续使用快照代理，但不能标成完整官方大厅实现。要转 `Implemented`，必须有可解释的 lobby state 或明确的项目代理政策。

| 英雄 | 当前代理 | official-complete 缺口 | 建议 |
| --- | --- | --- | --- |
| Mr. Bigglesworth | 已淘汰玩家战队快照、最低血量/对手代理。 | 真实英雄淘汰事件、最低血量排序、淘汰玩家战队保留。 | 普通英雄技能候选已过滤；本体保留 `FrameworkFirst`，等大厅状态。 |
| Scabbs Cutterbutter | 当前/上次对手作为“下个对手”代理；宝宝按该对手 Buddy 映射。 | 真实下个对手排程不作为当前单人训练器完成标准。 | 已按模拟对手代理转 `Implemented`。 |
| Tess Greymane | 上次对手战队快照；宝宝按上次对手 Buddy 映射。 | 真实多玩家上一对手历史不作为当前单人训练器完成标准。 | 已按模拟对手代理转 `Implemented`。 |
| Murozond, Unbounded | Turn 8 打开 Major Timewarped Tavern 已接通。 | previous-warband 属于具体 Timewarp 牌/数据效果，不挂在 Murozond 英雄完成度上。 | 已按英雄本体转 `Implemented`；后续放入 Timewarped Tavern 数据底座。 |

### 真大厅代理执行线

1. C1：定义 `OpponentSnapshotSource`，区分 current opponent、last opponent、next opponent、eliminated opponent、timeline history。
2. C2：让 UI/debug state 显示代理来源，避免玩家误以为是真实大厅。
3. C3：Scabbs/Tess 已按稳定模拟对手快照路径收口。
4. C4：Bigglesworth 等真实淘汰/最低血量系统；Murozond 等 timeline history。

## 4. 需要独立大机制

这些不应该在 `HeroEffectEngine` 里继续加特判。先建对应子系统，再把现有宝宝代理迁移过去。

| 机制 | 英雄 | 当前代理 | 子系统完成线 |
| --- | --- | --- | --- |
| Trinket | Marin the Manager | Fantastic Bellhop end-turn helpful card。 | Lesser/Greater Trinket 候选池、选择 UI、槽位、过滤、复制/排除规则。 |
| Trinket | Buttons | Zippers 通过 Deathrattle/Tavern proxy 给 helpful card。 | Greater Trinket 选择规则和真实 Deathrattle 奖励。 |
| Secret | The Great Akazamzarak | Street Magician 生成 Better Secret proxy。 | Secret 选择、挂载、触发、移除、战斗时点。 |
| Custom Undead | Professor Putricide | 已完成两次三选一组件 Discover、结果随从生成、关键词重复过滤和 Festergut 共用 factory。 | 后续只剩官方组件数值/资源微调；不再阻塞英雄 gameplay 完成度。 |
| StarCraft Terran | Jim Raynor | Tychus 给 playable Battlecruiser Upgrade。 | Terran/Battlecruiser 实体、升级池、施放规则。 |
| StarCraft Protoss | Artanis | Probius Magnetic 后让目标 Mech Golden。 | Protoss 奖励轨道、延迟奖励、正式 Magnetize 事件。 |
| StarCraft Zerg | Kerrigan, Queen of Blades | Broken Horn sell 后发现 6/6 Zerg proxy。 | Zerg 随从池、等级解锁、morph 限制、禁止变形标记。 |

### 独立大机制执行线

1. D1：Trinket。项目已有 trinket catalog/runtime，优先补 Marin/Buttons 需要的选择和过滤。
2. D2：StarCraft Terran。Jim Raynor 已有 Battlecruiser Upgrade proxy，最容易形成闭环。
3. D3：StarCraft Zerg。Kerrigan 需要 Zerg pool/morph，但 Broken Horn 入口清晰，适合作为第二个 StarCraft 子系统。
4. D4：StarCraft Protoss。Artanis 依赖 Magnetize 事件和延迟奖励，放在 Terran/Zerg 之后。
5. D5：Secret 和 Custom Undead。二者都需要战斗中 payload/触发框架，建议等 B4 战斗 Deathrattle payload 完成后再做。

## 5. 需要你决策的

这一组不是第五种实现底座，而是执行前必须由你拍板的横切队列。没有结论时，相关英雄继续保持 `FrameworkFirst` 或过滤状态，不强行转正。

| 决策项 | 影响范围 | 默认保守处理 | 你需要拍板的内容 |
| --- | --- | --- | --- |
| A1 转正标准 | Tavish、Tamsin、Onyxia、Bru'kan | 已决策：允许在 UI polish/通用框架后续加强的情况下转 `Implemented`。 | 已执行：四个 registry 状态转 `Implemented`，focused tests 作为回归标准。 |
| Bigglesworth 本体代理 | Mr. Bigglesworth / Lil' K.T. | 已决策：普通英雄技能候选直接过滤；本体运行时保留 `FrameworkFirst` 单人代理，不开放为 Nguyen/Cosmic/Timewarped 候选。 | 后续只在真实大厅淘汰/最低血量系统完成后重新评估转正。 |
| StarCraft 子系统优先级 | Jim Raynor、Kerrigan、Artanis | 已决策：采用 `Jim Raynor -> Kerrigan -> Artanis`。 | 下一步先写/执行 Terran/Battlecruiser 子系统实现文档，再做 Zerg，最后 Protoss。 |
| Galewing 航线奖励标准 | Galewing / Flight Trainer | 已决策并完成：使用 Westfall/Ironforge/Eastern Plaguelands 奖励表。 | 已转 `Implemented`。 |
| Yogg Wheel 完成标准 | Yogg-Saron / Acolyte | 已决策并完成：允许用户确认的项目五格 Wheel 表作为当前完成标准。 | 已转 `Implemented`；官方 public API 未展开子结果，保留来源边界说明。 |

### 决策后动作

1. A1 已允许转正并已更新 registry；后续只补 UI smoke/通用框架增强，不回退状态。
2. Bigglesworth 不禁用本体运行时，但保持候选过滤；真实大厅系统完成前不转正。
3. StarCraft 顺序已确认，先写 Terran/Battlecruiser 子系统实现文档，再开始第一个 runtime 批次。
4. Galewing 与 Yogg 本轮已完成：不再以旧 proxy 状态留在 FrameworkFirst 清单中。

## 推荐总执行顺序

1. A1 直接转正批次已完成：Tavish、Tamsin、Onyxia、Bru'kan。
2. A2 小批次已完成：Morchie。
3. B1-B3 战斗事件底座第一段：attack/kill/death 记录，优先收口 Aranna、Rokara、Rafaam、Ini、Sylvanas。
4. C1-C3 真大厅代理模型：Scabbs、Tess 已按单人模拟对手代理完成；后续只剩 Bigglesworth。
5. D1 Trinket：Marin、Buttons。
6. D2-D4 StarCraft：Jim Raynor -> Kerrigan -> Artanis。
7. B4-B6 剩余战斗 payload/召唤/伤害/对手写回。
8. D5 Secret / Custom Undead。
9. A3 数据补齐已完成：Galewing 官方航线奖励、Yogg 项目确认 Wheel 表。

## 当前需要用户确认的点

前 5 个决策项已处理完成；当前没有阻塞 FrameworkFirst 收口的横切决策项。
