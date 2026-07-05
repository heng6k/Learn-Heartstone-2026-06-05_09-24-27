# 英雄剩余 6+28 实现计划

Date: 2026-07-04

## 目标

本文只跟踪当前仍未完全完成的英雄/宝宝效果，分为两类：

- **6 个真正未实现 gameplay runtime 的英雄**：注册表为 `Planned` 或 `Deferred`，当前没有可用玩法运行时路径。
- **28 个已有 runtime 代理但未 official-complete 的英雄**：注册表为 `FrameworkFirst`，已有局部实现或单人训练器代理，但还缺官方完整数据、公共事件底座、真实大厅状态或独立大机制。

本文不重复列已完成的 `Implemented` 英雄，例如 Tavish、Tamsin、Onyxia、Bru'kan、Morchie、Scabbs、Tess、Master Nguyen、Tickatus、The Rat King、Murloc Holmes、Lord Barov 等。

## 当前基线

| 状态 | 数量 | 含义 |
| --- | ---: | --- |
| `Planned` | 4 | 目标明确，但 gameplay runtime 尚未实现。 |
| `Deferred` | 2 | 依赖底座未完成，暂缓实现。 |
| `FrameworkFirst` | 28 | 已有运行时代理或局部实现，但不是官方完整语义。 |

实现时必须遵守三个边界：

1. 不把已有 proxy 重写一遍；先确认现有 runtime 和测试，再补缺口。
2. 不为了单个英雄堆临时系统；攻击、死亡、亡语、召唤、伤害、对手历史、饰品、奥秘、星际机制都必须走可复用底座。
3. 只有 runtime 路径、focused 测试和注册表说明都闭合后，才把状态转为 `Implemented`。

## 总执行顺序

1. **P0：候选和注册表一致性检查**
   - 输出当前 9+28 清单。
   - 跑 `HeroEffectImplementationRegistryTests`。
   - 确认普通英雄技能发现池不会给出完全不可执行项。

2. **P1：先做 9 个无 gameplay runtime 的低/中依赖项**
   - 推荐顺序：Rat King -> Holmes -> Barov -> Vashj -> Azshara。
   - Loh 和 Dinotamer Brann 等 P3 战斗事件底座。
   - Mister Clocksworth 和 Genn 等各自底座。

3. **P2：补战斗事件底座，批量收口 17 个 FrameworkFirst**
   - 事件源：攻击、击杀归属、死亡快照、亡语 payload、召唤位置、伤害、嘲讽被攻击。
   - 优先能连带收口多个英雄的能力。

4. **P3：补数据/完成标准**
   - Galewing 官方航线奖励。
   - Yogg 官方 Wheel of Yogg 结果表。

5. **P4：补 Timewarp 历史状态**
   - Murozond：Turn 8 进入 Major Timewarp 已接通；剩余问题是 Major Timewarp 中“发现上一局战队随从”类牌的数据来源。
   - Bigglesworth 已从本计划删除，继续保持候选过滤，不作为后续 gameplay 补完目标。

6. **P5：补独立大机制**
   - Trinket：Marin、Buttons。
   - Secret：Akazamzarak。
   - Custom Undead：Putricide。
   - StarCraft：Jim Raynor -> Kerrigan -> Artanis。

7. **P6：最终验收**
   - 更新 registry、计划文档、候选策略文档。
   - 跑 focused EditMode 测试和 Unity batchmode 编译。
   - 对 UI 相关项补 Unity Trainer smoke。

## A. 真正未实现 gameplay runtime：6 个

### A1. The Rat King / Pigeon Lord

| 项 | 内容 |
| --- | --- |
| 中文名 | 鼠王 / Pigeon Lord |
| 状态 | `Implemented` |
| 当前缺口 | 没有每回合开始随机种族状态；没有按当前种族发现；没有 Pigeon Lord 免费刷新。 |
| 依赖底座 | 英雄技能状态提示、随从类型池过滤、刷新费用覆盖。 |
| 实现顺序 | 1. 在 Tavern/hero effect state 记录当前 Rat King 种族。2. 每回合开始从当前可用种族随机切换。3. 英雄技能按当前种族从当前随从池 Discover。4. Nguyen / Cosmic 等候选 UI 显示当前会发现的种族。5. Pigeon Lord 判断酒馆没有当前种族时让刷新免费。 |
| 验收测试 | 禁用种族不被选中；当前类型跨 UI 可见；使用英雄技能只发现当前类型；Buddy 已自然进入池时可被发现；酒馆有当前类型时刷新收费、没有时免费。 |
| 已完成 | 2026-07-04 已转 `Implemented`：回合开始切换当前可用种族、英雄技能按当前种族 Discover、Pigeon Lord 在酒馆缺当前种族时给一次免费刷新。 |
| 待确认 | 无。已确认 Rat King 是每回合开始变换，发现当时池子里对应种族。 |

### A2. Murloc Holmes / Watfin

| 项 | 内容 |
| --- | --- |
| 中文名 | 鱼人福尔摩斯 / Watfin |
| 状态 | `Implemented` |
| 当前缺口 | 没有竞猜 UI；没有下一对手/上一场战斗记忆；没有猜中奖励和宝宝复制。 |
| 依赖底座 | OpponentHistory、pending choice、Tavern Coin 奖励、Buddy 复制。 |
| 实现顺序 | 1. 建立 Detective choice，至少支持二选一或三选一。2. 从模拟对手/最近战斗快照生成竞猜项。3. 结算正确/错误结果。4. 正确时发 Tavern Coin。5. Watfin 在正确竞猜后给普通复制。 |
| 验收测试 | 有快照时可竞猜；猜对给 Coin；猜错不给；无快照时降级提示不崩；Watfin 只在猜对后给复制。 |
| 已完成 | 2026-07-04 已转 `Implemented`：单人训练器使用当前/下一对手快照出正确项，混入当前池干扰项；猜中给 Tavern Coin，Watfin 给普通复制。 |
| 待确认 | 竞猜题型的精确选项来源可按本地数据先做最小闭环。 |

### A3. Lord Barov / Barov's Apprentice

| 项 | 内容 |
| --- | --- |
| 中文名 | 巴罗夫领主 / Barov's Apprentice |
| 状态 | `Implemented` |
| 当前缺口 | 没有战斗预测选择；没有战后按预测结算；宝宝没有监听 Coin 使用。 |
| 依赖底座 | 战斗结果快照、pending prediction、Tavern Coin 牌、卡牌打出事件。 |
| 实现顺序 | 1. 英雄技能创建预测选择。2. 记录预测绑定的回合/战斗。3. 战斗后读取本场结果并结算。4. 猜中给 3 个 Tavern Coin。5. Barov's Apprentice 监听 Coin 打出并给金币。 |
| 验收测试 | 预测胜/负/平；跨回合预测不串；同一场战斗只结算一次；Coin 入手牌；宝宝监听 Coin 触发金币。 |
| 已完成 | 2026-07-04 已转 `Implemented`：`choiceId` 记录胜/负/平预测，`CombatEnded` 后猜中发 3 个 Tavern Coin，Barov's Apprentice 监听 Tavern Coin 施放并给金币。 |
| 待确认 | 用户已要求 Holmes/Barov 这类竞猜可做单人代理，Barov/Holmes 不再直接删除。 |

### A4. Lady Vashj / Coilfang Elite

| 项 | 内容 |
| --- | --- |
| 中文名 | 瓦丝琪女士 / Coilfang Elite |
| 状态 | `Planned` |
| 当前缺口 | 没有 Spellcraft 临时法术生成；没有回合结束清理；宝宝不能复制酒馆 Spellcraft。 |
| 依赖底座 | Spellcraft 临时牌、回合结束清理、酒馆 Spellcraft 识别。 |
| 实现顺序 | 1. 给 Spellcraft 牌打临时标记。2. 回合结束清理未使用 Spellcraft。3. 英雄技能生成/发现 Spellcraft。4. Coilfang Elite 复制酒馆 Spellcraft 来源。 |
| 验收测试 | 临时牌不会跨回合保留；普通手牌不误删；酒馆有 Spellcraft 时宝宝复制；无候选时给可解释日志。 |
| 待确认 | 需要按本地卡池确定 Spellcraft 候选来源。 |

### A5. Queen Azshara / Imperial Defender

| 项 | 内容 |
| --- | --- |
| 中文名 | 艾萨拉女王 / Imperial Defender |
| 状态 | `Planned` |
| 当前缺口 | 没有战队总攻击力阈值；没有 Naga Conquest 状态；宝宝没有每回合一次法术复制。 |
| 依赖底座 | 战队属性统计、英雄技能替换/解锁状态、友方法术施放监听。 |
| 实现顺序 | 1. 每次关键状态变更后计算战队总攻击力。2. 达阈值后切换到 Naga Conquest。3. 处理 Naga Conquest 的英雄技能效果。4. Imperial Defender 每回合第一次友方法术复制。 |
| 验收测试 | 攻击力达标后只解锁一次；状态可持久化；回合重置宝宝复制次数；只复制友方法术。 |
| 待确认 | Naga Conquest 的本地候选/奖励文本需要实现前核对。 |

### A6. Loh, the Living Legend / Stoneshell Guardian

| 项 | 内容 |
| --- | --- |
| 中文名 | 活体传说洛恩 / Stoneshell Guardian |
| 状态 | `Planned` |
| 当前缺口 | 没有友方攻击计数；没有达标后 Triple Reward；宝宝不能改写每回合第一个 Triple Reward。 |
| 依赖底座 | 友方攻击事件、Triple Reward 生成钩子、每回合一次替换。 |
| 实现顺序 | 1. CombatEngine 发出友方攻击事件。2. Loh 统计攻击次数。3. 达标后生成 Triple Reward。4. Stoneshell Guardian 拦截本回合第一个 Triple Reward，改为从 Golden minions 中 Discover。 |
| 验收测试 | 立即攻击和普通攻击都计数；敌方攻击不计数；Triple Reward 只给一次；宝宝每回合只改写第一个奖励。 |
| 待确认 | 等 P2 战斗事件底座。 |

### A7. Dinotamer Brann / Brann's Epic Egg

| 项 | 内容 |
| --- | --- |
| 中文名 | 恐龙大师布莱恩 / Brann's Epic Egg |
| 状态 | `Planned` |
| 当前缺口 | 没有购买战吼随从计数；没有一次性 Brann Bronzebeard 奖励；宝宝亡语未实现。 |
| 依赖底座 | 购买事件、Battlecry minion 识别、亡语 payload、死亡位置召唤。 |
| 实现顺序 | 1. 监听购买战吼随从。2. 达标后给 Brann Bronzebeard，一局一次。3. Brann's Epic Egg 获得 Taunt 和亡语 payload。4. 死亡时按位置召唤并给随机 Battlecry minion。 |
| 验收测试 | 只统计购买，不统计发现/生成；一局只给一次；宝宝亡语触发位置正确；手牌满/棋盘满时降级可解释。 |
| 待确认 | 等亡语 payload 和死亡位置底座。 |

### A8. Mister Clocksworth

| 项 | 内容 |
| --- | --- |
| 中文名 | 时钟先生 |
| 状态 | `Deferred` |
| 当前缺口 | TripleEngine 不支持“两张即可合金”；不能把三连奖励替换为 Tavern Coin。 |
| 依赖底座 | 可配置 Triple 规则、奖励替换策略。 |
| 实现顺序 | 1. TripleEngine 支持按英雄配置 requiredCopies。2. Clocksworth 局中 requiredCopies = 2。3. 普通三连奖励替换为 Tavern Coin。4. 保证普通英雄仍为三张合金。 |
| 验收测试 | Clocksworth 两张合金；普通英雄三张合金；Clocksworth 不给普通 Triple Reward；给 Coin 替代。 |
| 待确认 | 无，等 TripleEngine 改造。 |

### A9. Genn, Worgen King

| 项 | 内容 |
| --- | --- |
| 中文名 | 吉恩，座狼之王 |
| 状态 | `Deferred` |
| 当前缺口 | 没有 Turn 4 双英雄技能替换；多英雄技能槽位/替换清理不完整。 |
| 依赖底座 | HeroPowerSlot 模型、候选过滤、额外/临时/替换技能生命周期。 |
| 实现顺序 | 1. Cosmic Duality 下禁用 Genn。2. Turn 4 触发 pending replacement。3. 发现两个英雄技能并持久化。4. 替换/清理旧额外技能槽。5. UI 显示两个可用技能。 |
| 验收测试 | Cosmic Duality 不出现 Genn；Turn 4 替换；两个新技能都可使用；旧技能清理；跨回合保留。 |
| 待确认 | 已确认 Genn 在 Cosmic Duality 下禁用。 |

## B. 已有 runtime 代理但未 official-complete：28 个

### B1. 战斗事件底座组：17 个

这些英雄不应该继续各写一套私有代理。先补 CombatEngine 事件，再逐个迁移和转正。
本表统一写法为“中文名（英文名）/ 宝宝名”，方便和注册表、官方文本、中文计划互相对照。

| 序号 | 英雄 / 宝宝 | 当前已有 | official-complete 缺口 | 实现计划 | 验收重点 |
| ---: | --- | --- | --- | --- | --- |
| 1 | 沙德沃克（Shudderwock）/ Muckslinger | Muckslinger 战吼奖励；英雄技能可重放已实现的 Battlecry。 | 更广的官方 Battlecry 覆盖、多目标/二级目标选择。 | 扩展 Battlecry resolver 的目标模型和失败原因；给 Shudderwock 接入同一 resolver。 | 单目标、多目标、无合法目标、宝宝奖励。 |
| 2 | 沃金（Vol'jin）/ Master Gadrin | Spirit Swap 可指定两个目标。 | Master Gadrin 精确战斗开始左邻 hook。 | 补战斗开始小顺序和邻位快照；宝宝按左邻触发。 | 左邻存在/不存在、战斗开始顺序、临时/永久属性边界。 |
| 3 | 奥拉基尔（Al'Akir）/ Spirit of Air | 左侧随从战斗开始获得 Windfury/Divine Shield/Taunt。 | Spirit of Air 仍走 Tavern death proxy。 | 亡语 dispatch 接入英雄宝宝；Spirit of Air 在战斗死亡时触发。 | 亡语触发、关键字继承、非战斗死亡不误触。 |
| 4 | 死亡之翼（Deathwing）/ Sinestra | 双方战斗中 +2 Attack；友方保留攻击；Sinestra 转 Health。 | 无；对手永久攻击写回已按 2026-07-04 用户决策删除。 | 已转 `Implemented`。 | 保留现有双方战斗副本 +2、友方永久保留和 Sinestra 生命转换回归。 |
| 5 | 伊利丹・怒风（Illidan Stormrage）/ Eclipsion Illidari | 边缘随从 +2/+1，战斗前立即攻击，宝宝攻击时免疫。 | 通用友方攻击计数和更完整立即攻击排序。 | 把 tagged immediate attacks 放入统一队列；暴露 attack start/end 事件。 | 左右边界、攻击顺序、与其他战斗开始效果排序。 |
| 6 | 恩佐斯（N'Zoth）/ Baby N'Zoth | 开局 Fish 和宝宝 Golden Battlecry。 | Fish 收集死亡随从 Deathrattle。 | 死亡快照记录亡语 payload；Fish 接收 payload 列表。 | 多亡语、无亡语、Golden/普通亡语差异。 |
| 7 | 泰隆・血魔（Teron Gorefiend）/ Shadowy Construct | 目标标记和战斗开始摧毁/复活 proxy。 | 精确死亡触发时序和 exact copy 复活。 | 战斗开始摧毁走死亡事件；记录死亡位置；空位复活 exact copy。 | 死亡事件触发、复活位置、棋盘满。 |
| 8 | 反派大盗拉法姆（Arch-Villain Rafaam）/ Loyal Henchman | 攻击/反击击杀归属和第一/第二死亡奖励。 | 法术/亡语/召唤物击杀归属、完整敌方坟场。 | 统一 KillAttribution；记录敌方死亡顺序。 | 第一死亡、第二死亡、非攻击击杀、宝宝奖励。 |
| 9 | 罗卡拉（Rokara）/ Icesnarl | 攻击/反击友方击杀后永久 Attack/Health 奖励。 | 非攻击击杀来源。 | 复用 KillAttribution，支持法术/亡语/召唤物伤害。 | 友方击杀才触发、非攻击源、永久写回。 |
| 10 | 希尔瓦娜斯・风行者（Sylvanas Windrunner）/ Nathanos | Nathanos 定向卖出并分配属性。 | Reclaimed Souls 需要上场死亡历史和 Discover。 | 记录 last-combat friendly deaths；英雄技能 Discover 死亡随从。 | 死亡历史、无历史回退、Discover 选择。 |
| 11 | 斯尼德（Sneed）/ Piloted Whirl-O-Tron | 开局 2/1 Shredder。 | 手牌召唤亡语、Whirl-O-Tron 复制亡语。 | 实现亡语 payload 和手牌召唤位置；宝宝复制目标亡语。 | 起始随从测试、亡语召唤、复制亡语。 |
| 12 | 典狱长（The Jailer）/ Mawsworn Soulkeeper | Runic Empowerment 基于友方死亡计数。 | 宝宝仍走 Tavern death proxy。 | 战斗死亡计数回写；Mawsworn 通过战斗亡语 dispatch 触发。 | 死亡计数、亡语触发、战后回写。 |
| 13 | 灰枝（Greybough）/ Wandering Treant | 战斗召唤获得 Taunt/buff。 | 宝宝 Taunt 被攻击监听和永久 board-wide buff。 | 增加 Taunt attacked 事件；宝宝监听并永久 buff。 | 嘲讽被攻击、非嘲讽不触发、永久写回。 |
| 14 | 伊妮・风暴线圈（Ini Stormcoil）/ Sub Scrubber | Sub Scrubber 机械打出成长。 | MechGyver 需要友方机械战斗死亡计数并给机械奖励。 | 战斗死亡按种族统计；回合后奖励机械。 | 机械死亡计数、非机械不计数、奖励合法。 |
| 15 | 奥祖玛特（Ozumat）/ Tamuzo | 触手召唤、出售/战斗死亡成长、Tamuzo doubling 部分接通。 | 任意未来召唤源仍需共享 resolver。 | 所有 combat summon 走统一 resolver，并支持 modifiers。 | 召唤源一致、触手属性、Tamuzo 翻倍。 |
| 16 | 阿兰娜・逐星（Aranna Starseeker）/ Sklibb | Sklibb 刷新额外高一级随从。 | 英雄技能解锁需要友方攻击计数。 | 接 attack counter；达到阈值后切换/解锁英雄技能效果。 | 攻击计数、立即攻击计数、解锁持久化。 |
| 17 | 加拉克苏斯大王（Lord Jaraxxus）/ Kil'rek | Kil'rek 走 Tavern death proxy 给 Demon。 | Bloodfury 需要友方造成伤害统计和 portal reward。 | 增加 friendly damage dealt counter；建立 Demon portal reward 表。 | 伤害统计、奖励发放、宝宝真实亡语。 |

### B2. 数据/完成标准组：2 个

| 序号 | 英雄 / 宝宝 | 当前已有 | 缺口 | 实现计划 | 验收重点 |
| ---: | --- | --- | --- | --- | --- |
| 18 | 风翼（Galewing）/ Flight Trainer | 航线选择、延迟完成、不能连续重复、宝宝双触发。 | 已完成：官方三航线奖励表已接入。 | Westfall 1 回合随机 1 费 Tavern spell；Ironforge 2 回合 +2 Gold；Eastern Plaguelands 3 回合发现当前等级随从。 | 三航线奖励、延迟回合、不能连续同航线、宝宝双触发。 |
| 19 | 绝望之尤格萨隆（Yogg-Saron, Hope's End）/ Acolyte | Turn 3 起 Puzzle Box 自动施放合法 Tavern spell；宝宝五格 Wheel 表。 | 已完成：采用用户确认的项目 Wheel 表；官方 public API 未展开子结果。 | 五格结果分别接入：开炮、Darkmoon Prize、4 个 Tavern spell、身材转移、吞噬刷新。 | 每个轮盘结果、随机池、日志、宝宝触发。 |

### B3. Timewarp 历史组：1 个

| 序号 | 英雄 / 宝宝 | 当前已有 | 缺口 | 实现计划 | 验收重点 |
| ---: | --- | --- | --- | --- | --- |
| 20 | 姆诺兹多，无界者（Murozond, Unbounded） | Turn 8 打开 Major Timewarped Tavern。 | Major Timewarp 中“发现上一局战队随从”类牌缺少稳定历史来源。 | 建立 previous-run / timeline warband snapshot；让 Timewarped previous-warband 奖励读取该来源；无历史时明确降级。 | Turn 8 开门、上一局战队来源、无历史回退。 |

### B4. 独立大机制组：7 个

| 序号 | 英雄 / 宝宝 | 当前已有 | 缺口 | 实现计划 | 验收重点 |
| ---: | --- | --- | --- | --- | --- |
| 22 | 经理马林（Marin the Manager）/ Fantastic Bellhop | Bellhop 回合结束给 helpful card。 | Lesser/Greater Trinket 选择、槽位、候选过滤。 | 复用 Trinket catalog；补英雄专属 Trinket choice；接 UI 和选择命令。 | 饰品槽、候选过滤、宝宝奖励迁移。 |
| 23 | Buttons（Buttons）/ Zippers | Zippers 通过代理给 helpful card。 | Greater Trinket 选择规则和真实亡语奖励。 | 接 Greater Trinket choice；Zippers 走真实亡语 dispatch。 | Turn 8 选择、候选合法、亡语触发。 |
| 24 | 了不起的阿扎扎拉克（The Great Akazamzarak）/ Street Magician | Street Magician 生成 Better Secret proxy。 | Secret 选择、挂载、触发、移除、战斗时点。 | 建立 Secret state；英雄技能选择 Secret；战斗/招募阶段触发并移除。 | Secret 可见、触发时点、重复/替换规则、宝宝 Better Secret。 |
| 25 | 普崔塞德教授（Professor Putricide）/ Festergut | Festergut 召唤/获得 Undead Creation proxy。 | Custom Undead 组件池、费用、结果随从、关键词/亡语。 | 建立 Undead Creation 数据模型；制作命令流；结果随从生成；迁移 Festergut。 | 组件合法、费用、生成结果、亡语 payload。 |
| 26 | 吉姆・雷诺（Jim Raynor）/ Tychus | Tychus 给 playable Battlecruiser Upgrade。 | Terran/Battlecruiser 实体、升级池、施放规则。 | 先做 Battlecruiser 实体和 upgrade catalog；再接 Tychus 奖励。 | 升级链、施放限制、状态持久化。 |
| 27 | 凯瑞甘，刀锋女王（Kerrigan, Queen of Blades）/ Broken Horn | Broken Horn sell 后发现 6/6 Zerg proxy。 | Zerg pool、等级解锁、morph 限制、禁止变形标记。 | 建立 Zerg minion pool；实现 morph 目标过滤；迁移 Broken Horn Discover。 | 6/6 proxy 替换、morph 合法性、禁变形标记。 |
| 28 | 阿塔尼斯（Artanis）/ Probius | Probius Magnetic 后让目标 Mech Golden。 | Protoss 延迟奖励、正式 Magnetize 事件。 | 增加 Magnetize 事件；建立 Protoss reward track；迁移 Probius。 | 磁力事件、目标 Mech、延迟奖励、Golden 转换。 |

## 公共底座清单

| 底座 | 服务对象 | 最小交付 |
| --- | --- | --- |
| HeroPowerSlot | Rat King、Genn、Nguyen、Cosmic Duality、Timewarped | 主技能、额外技能、临时覆盖、延迟替换都可解释、可显示、可指定使用。 |
| OpponentHistory | Holmes、Barov、Murozond、Rafaam、Tess/Scabbs 回归 | last/current/next/timeline 来源明确。 |
| CombatEventRecord | Loh、Aranna、Illidan、Rafaam、Rokara、Sylvanas、Ini | 攻击、击杀、死亡、召唤、伤害事件可断言。 |
| DeathrattlePayload | N'Zoth、Sneed、Teron、Jailer、Al'Akir、Dinotamer Brann、Putricide | 亡语可复制、可转移、可按死亡位置召唤。 |
| CombatSummonResolver | Greybough、Ozumat、Teron、Onyxia 回归 | 所有战斗召唤统一走 modifiers。 |
| Spellcraft/Naga | Vashj、Azshara | 临时法术、回合清理、复制来源、阈值状态。 |
| TrinketChoice | Marin、Buttons | 大小饰品槽、候选过滤、选择 UI、宝宝迁移。 |
| SecretSystem | Akazamzarak | Secret 状态、触发、移除、日志。 |
| StarCraftSubsystem | Jim Raynor、Kerrigan、Artanis | Terran/Zerg/Protoss 数据池和专属规则。 |
| TripleRuleConfig | Mister Clocksworth | 两张合金、奖励替换，不影响普通三连。 |

## 推荐里程碑

### M1：无 runtime 第一批

范围：Rat King、Holmes、Barov。

验收：

- 三个英雄从 `Planned` 转 `Implemented` 或有明确剩余说明。
- 单人训练器能完整使用英雄技能和宝宝效果。
- 候选池不会把未完成版本暴露为完整可用。

### M2：Spellcraft / Naga

范围：Vashj、Azshara。

验收：

- Spellcraft 临时牌和清理规则稳定。
- Naga Conquest 状态可显示、可持久化。

### M3：战斗事件底座第一段

范围：Loh、Aranna、Illidan、Rokara、Rafaam、Sylvanas、Ini。

验收：

- CombatEngine 层有事件测试。
- 相关英雄不再依赖事后猜测。

### M4：亡语和召唤底座

范围：N'Zoth、Sneed、Teron、Jailer、Al'Akir、Dinotamer Brann、Greybough、Ozumat。

验收：

- 亡语 payload 可复制/转移。
- 死亡位置和召唤位置可断言。

### M5：独立大机制

范围：Marin、Buttons、Akazamzarak、Putricide、Jim Raynor、Kerrigan、Artanis。

验收：

- 不再以 hero-specific proxy 作为主路径。
- 每个子系统有自己的 catalog/state/test。

### M6：历史/数据收尾

范围：Galewing、Yogg、Murozond、Mister Clocksworth、Genn。

验收：

- 数据缺口补齐或明确继续保留 `FrameworkFirst/Deferred`。
- 所有注册表状态和文档一致。

## P0-P4 剩余问题清单

本节只列 P0-P4 范围内当前还不能硬标完成的问题。`Rat King`、`Murloc Holmes`、`Lord Barov` 已在 2026-07-04 转 `Implemented`，不再列入待实现 runtime 清单。2026-07-04 追加修补后，`Lady Vashj`、`Queen Azshara`、`Mister Clocksworth`、`Genn, Worgen King`、`Murozond, Unbounded` 英雄本体也已从本节剩余 gameplay 清单移出；其中 Murozond 本体完成标准是 Turn 8 打开 Major Timewarped Tavern，previous-warband 属于 Timewarped Tavern 牌/数据效果，不挂在 Murozond 英雄完成度上。

2026-07-04 本轮收口结果：`Loh, the Living Legend`、`Dinotamer Brann`、`Galewing`、`Yogg-Saron, Hope's End` 已按用户补充语义转 `Implemented`。`Timewarped Master Thief / Timewarped Thief` 的 previous-warband 误归属已改为固定 Golden Brann / Golden Titus / Golden Drakkari 三选一；该项不再挂在 Murozond 英雄完成度上。Bigglesworth 继续从本计划删除并保持普通英雄技能候选过滤。

### P1：未完成 gameplay runtime

| 序号 | 英雄 / 宝宝 | 当前状态 | 剩余问题 | 阻塞点 | 建议解决办法 | 建议顺序 |
| ---: | --- | --- | --- | --- | --- | ---: |
| 1 | 活体传说洛恩（Loh, the Living Legend）/ Stoneshell Guardian | `Implemented` | 已完成：友方攻击计数和达标 Triple Reward 已接上；Stoneshell Guardian 会把每回合第一个打出的 Triple Reward 改为从 Golden minions 中 Discover。 | 无。 | 保留 focused test：首个 Triple Reward 被替换为金色随从发现，且同回合只替换一次。 | 已完成 |##（任务那里已有对于进攻的统计，本体已用友方攻击事件计数）
| 2 | 恐龙大师布莱恩（Dinotamer Brann）/ Brann's Epic Egg | `Implemented` | 已完成：本体购买 4 个战吼随从获得 Brann Bronzebeard；Brann's Epic Egg 亡语按当前酒馆等级上限召唤并获得随机 Battlecry minion，金色翻倍。 | 无；通用 Deathrattle payload 后续仍可增强其它英雄，但不再阻塞 Dinotamer Brann 完成度。 | 保留 focused test：宝宝死亡后召唤 Battlecry minion，并把不高于当前酒馆等级的 Battlecry minion 加入手牌。 | 已完成 |##（类似于任务，购买到4个战吼随从获得5本卡铜须；宝宝另走亡语底座）

### P2：公共战斗底座问题

| 序号 | 底座问题 | 影响范围 | 当前缺口 | 建议解决办法 | 建议顺序 |
| ---: | --- | --- | --- | --- | ---: |
| 3 | 击杀归属扩展 | Rafaam、Rokara | 当前主要覆盖直接攻击/反击，非攻击来源不完整。 | 统一 `KillAttribution`，覆盖攻击、反击、法术、亡语、召唤物伤害；记录敌方死亡顺序。 | 3 |
| 4 | 死亡历史和 Deathrattle payload | N'Zoth、Sneed、Teron、Jailer、Al'Akir；Dinotamer Brann 已有最小闭环 | 通用死亡快照、死亡位置、可复制/转移的亡语 payload 仍需服务其它英雄。 | Brann's Epic Egg 已先走 focused Deathrattle payload；后续在 P5/B4 扩成通用 payload，英雄/宝宝只消费该 payload，不各自重建亡语。 | 后续 P5 |##（对于死亡发现，死掉的相同的随从附加物不重复计算，但是算计数）
| 5 | 战斗召唤统一 resolver | Greybough、Ozumat、Teron、Onyxia 回归；Brann's Epic Egg 已接入战斗召唤池 | 不同召唤源仍可能绕过 Taunt/buff/double stats modifiers。 | Brann's Epic Egg 已使用 `CombatSummonPool`、召唤 aura 和友方召唤触发；后续把其它战斗召唤源继续收敛到同一 resolver。 | 后续 P5 |

### P3：数据/完成标准问题

| 序号 | 英雄 / 宝宝 | 当前状态 | 剩余问题 | 阻塞点 | 建议解决办法 | 建议顺序 |
| ---: | --- | --- | --- | --- | --- | ---: |
| 6 | 风翼（Galewing）/ Flight Trainer | `Implemented` | 已完成：Westfall 1 回合后获得随机 1 费酒馆法术；Ironforge 2 回合后获得 2 金币；Eastern Plaguelands 3 回合后发现当前酒馆等级随从；Flight Trainer 双触发。 | 无。 | 保留三航线奖励、延迟回合、不能连续同航线、宝宝双触发 focused tests。 | 已完成 |
| 7 | 绝望之尤格萨隆（Yogg-Saron, Hope's End）/ Acolyte | `Implemented` | 已完成：Puzzle Box 从 Turn 3 起自动施放合法随机 Tavern spell；Acolyte 使用项目认可的 5 格 Wheel 表。 | 官方 public API 未展开 Wheel 子结果；当前以用户确认的项目表为完成标准。 | 保留五种 Wheel 结果测试：开炮 +10/+10、Darkmoon Prize、4 个 Tavern spell、身材转移、吞噬酒馆并刷新。 | 已完成 |

### P4：Timewarp 历史问题

| 序号 | 英雄 / 宝宝 | 当前状态 | 剩余问题 | 阻塞点 | 建议解决办法 | 建议顺序 |
| ---: | --- | --- | --- | --- | --- | ---: |
| 8 | Timewarped Tavern previous-warband 牌/数据 | Timewarp 数据底座 | 已完成本轮修正：`Timewarped Master Thief` / `Timewarped Thief` 不再读取 previous-warband，改为固定 Golden Brann / Golden Titus / Golden Drakkari 三选一。 | 无；若未来另有 Timewarp 牌确实需要 previous-warband，再归入 Timewarped Tavern 数据底座。 | 保留 Timewarped 固定金色三选一 focused tests；Murozond 英雄完成度仍只按 Turn 8 Major Timewarped Tavern 判断。 | 已完成 |

Bigglesworth / Kel'Thuzad's Kitty 已从 P4 实现计划删除：该英雄继续保持候选过滤，不作为后续 gameplay 补完目标。

### 建议批次

| 批次 | 范围 | 完成定义 |
| --- | --- | --- |
| Batch 1 | Stoneshell Guardian | 已完成：每回合第一个 Triple Reward 替换为从金色随从发现。 |
| Batch 2 | Brann's Epic Egg | 已完成：亡语召唤并获得当前酒馆等级上限内的随机 Battlecry minion，金色翻倍。 |
| Batch 3 | 击杀归属扩展、战斗召唤 resolver | Brann's Epic Egg 已接最小召唤 resolver；更广的非攻击击杀和召唤 modifiers 迁入后续 P5。 |
| Batch 4 | Galewing、Yogg | 已完成：Galewing 官方航线奖励表接入；Yogg 使用用户确认的 5 格 Wheel 表。 |
| Batch 5 | Timewarped previous-warband 牌/数据 | 已完成当前定位项：相关 Timewarp Thief 牌改为固定金色三选一，不再挂 Murozond。 |

## 每批完成定义

每个英雄完成时必须同时满足：

1. Registry 状态、Note 和实际 runtime 一致。
2. Focused EditMode 测试覆盖英雄技能和宝宝。
3. 需要 UI 的项有 pending choice / discover / target / smoke 覆盖。
4. 需要公共底座的项不能只靠单个英雄私有分支通过。
5. 文档同步更新本文、`HeroFrameworkFirstCompletionPlan.md`、`HeroEffectIncompleteCompletionPlan.md`。
