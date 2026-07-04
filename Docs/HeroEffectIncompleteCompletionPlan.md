# 未完成英雄与宝宝效果专项完成计划

Date: 2026-07-02

## 结论

当前不是只剩一个英雄或只剩 `HeroEffectRemainingCompletionOrder.md` 里的“第一批第 6 项”。严格按注册表看，剩余主体是：

| 状态 | 数量 | 处理方式 |
| --- | ---: | --- |
| `Planned` | 15 | 目标清楚，按批次继续实现。 |
| `FrameworkFirst` | 33 | 已有部分代理或框架实现，必须先补共用机制再批量收口。 |
| `Deferred` | 2 | 等对应底座完成后再开工。 |

你不需要先把每个实际效果逐个详细说明。后续实现应先从以下来源抽取官方文本、已有代理边界和注册表状态：

- [HeroEffectImplementationRegistry.cs](../Assets/LearnHearthstone/Runtime/Domain/Data/HeroEffectImplementationRegistry.cs)
- [HeroEffectRemainingCompletionOrder.md](HeroEffectRemainingCompletionOrder.md)
- [HeroEffectImplementationGaps.md](HeroEffectImplementationGaps.md)
- [HeroPowerBuddyEffectsImplementationOrder.md](HeroPowerBuddyEffectsImplementationOrder.md)
- `Assets/LearnHearthstone/Resources/Data/battlegroundsHeroes.json`

只有当这些文件互相冲突、没有说明候选池/触发时点、或本地单人酒馆无法等价模拟真实 8 人大厅时，才需要你补充实际语义。所有这类问题必须在实现前标成“待确认”，不能靠猜测写死。

## 总体原则

1. 每个英雄技能和对应宝宝作为一个交付单元；能分层实现时，也要在注册表说明剩余边界。
2. 优先补共用机制，不在单个英雄里重复写临时系统。
3. 涉及选择、目标、临时英雄技能、第二英雄技能、预测和延迟奖励时，必须走统一 pending choice / discover / command 流。
4. 涉及真实对手、最低血量玩家、下一对手、淘汰玩家的效果，单人酒馆可以用代理快照，但不能标成完整官方大厅实现。
5. 完成后同步更新注册表、缺口文档、顺序文档和 focused EditMode 测试；有 UI 的必须补 Unity Trainer 或 UI smoke test。

## P0：第一批剩余低依赖 Planned

目标：先完成不依赖大型专属系统的剩余第一批英雄。

| 顺序 | 英雄 / 宝宝 | 当前状态 | 共用机制 | 当前阻塞 | 最小实现顺序 | 验收 |
| ---: | --- | --- | --- | --- | --- | --- |
| 1 | Galewing / Flight Trainer | `FrameworkFirst` | 航线 pending choice、延迟回合奖励、路线历史 | 三条航线奖励文本本地缺失，当前不能标完整官方实现 | 已完成航线状态、倒计时、不能连续重复选择和 Flight Trainer 双结算；三条奖励暂用明确 proxy | 英雄选择路线、倒计时发奖、连续路线过滤、宝宝双奖励测试已补；待补官方路线奖励文本后再转 `Implemented` |
| 2 | Cariel Roame / Captain Fairmount | `Implemented` | 战斗后选择、英雄技能成长状态、回合结束随机改进 | 无 | 已完成 Conviction 分支状态、战斗后改进选择、回合结束宝宝随机改进 | 战斗后 pending choice、不同升级分支、宝宝在场随机改进和主动 buff 测试已补 |

待确认：

- Galewing 三条航线的本地奖励文本应以 `battlegroundsHeroes.json` 为准；若本地数据缺少完整路线奖励，再请你确认。
- Cariel 每次战斗后的“改进”候选是否固定顺序、随机候选还是三选一，需要按本地文本和现有数据核实。

## P1：开局选择与延迟发放

目标：补 match-start discover、可序列化延迟奖励、Tier 7 候选和花费统计。

| 顺序 | 英雄 / 宝宝 | 当前状态 | 共用机制 | 当前阻塞 | 最小实现顺序 | 验收 |
| ---: | --- | --- | --- | --- | --- | --- |
| 1 | Ambassador Faelin / Submersible Chef | `Implemented` | 开局 Discover 队列、按酒馆等级延迟发放、首回合限制 | 无 | 已完成开局 Tier 6/4/2 当前池 Discover、选择持久化、到达对应酒馆等级发放；宝宝给 Tier 1/3/5 随机随从 | 首回合限制、选择不提前进手牌、达到等级发放、宝宝奖励测试已补 |
| 2 | Thorim / Veranus | `Implemented` | Tier 7 候选池、花费金币累计、延迟手牌奖励、相邻目标变形 | 无 | 已完成开局 Thorim 专属合法 Tier 7 Discover、累计金币花费、60 金币后发放；宝宝回合结束把左侧随从变形成高一等级 | Tier 7 候选、60 金币解锁、Veranus 邻位/最高 Tier 测试已补 |

待确认：

- 已决策：Thorim 自带 Tier 7 Discover/奖励候选，使用 `LegalTierSevenMinionDefinitions()` 过滤禁用种族和卡池，但不全局解锁普通酒馆 Tier 7 刷新。

## P2：任务、暗月奖品、随机法术奖励

目标：把外部奖励池做成共用系统，避免每个英雄维护一套代理。

| 顺序 | 英雄 / 宝宝 | 当前状态 | 共用机制 | 当前阻塞 | 最小实现顺序 | 验收 |
| ---: | --- | --- | --- | --- | --- | --- |
| 1 | Sire Denathrius / Shady Aristocrat | `Implemented` | Quest / Reward 数据模型、任务选择、进度、奖励激活 | 已完成 | 已复用通用 Quest/Reward 目录和任务状态；开局任务选择；进度触发奖励；宝宝出售发现任务并完成后给 8 金币钱袋 | 已有任务选择、进度持久化、奖励激活、宝宝售出任务链测试 |
| 2 | Tickatus / Ticket Collector | `Implemented` | Darkmoon Prize 调度、分级奖品池、奖品 Discover | 已完成 | 已完成每 4 回合调度、分级 Discover、宝宝出售发现下一等级奖品；33 张本地暗月奖品效果均已补齐 | 调度、共享奖品池、P0/P1/P2 奖品效果和 registry 回归测试 |
| 3 | Yogg-Saron / Acolyte of Yogg-Saron | `FrameworkFirst` | 随机 Tavern spell 执行入口、Wheel of Yogg 结果表 | Wheel 官方结果表本地不足 | Turn 3 起回合开始从合法 Tavern spell 池随机施放；宝宝回合开始使用可见 Wheel proxy | 已补解锁时点、随机法术候选、共享法术执行入口、轮盘 proxy 状态测试 |

待确认：

- Darkmoon Prize 各级奖品效果已补齐，生成奖品不再保留 `darkmoon_prize_proxy` 标记；Tickatus 已可作为完整实现验收。
- Wheel of Yogg 目前先做可见 proxy；如果要转 `Implemented`，需要补官方完整结果表和对应效果。

## P3：轮换、临时英雄技能和多英雄技能槽

目标：把“当前英雄技能”“第二/额外英雄技能”“本回合临时英雄技能”“延迟替换英雄技能”统一到同一套状态、候选过滤、命令和 UI 规则里。P3 不是先硬做某一个英雄，而是先把槽位边界定清楚，再按 Rat King -> Nguyen -> Cosmic Duality 复查 -> Genn 直线推进。

### P3 范围边界

- 已有基础：`ExtraHeroPowerCardIds`、`GrantSecondHeroPower(...)`、`ResolveUsableHeroPowerCardId(...)` 和 Unity Trainer 的多英雄技能按钮已经能支撑“额外英雄技能可见、可指定使用”的基本路径；Cosmic Duality 已能发现并加入第二英雄技能。
- 仍缺统一规则：额外技能、临时技能、替换技能现在没有明确的 source / duration / replace target / cleanup 元数据。Nguyen 和 Genn 如果直接各写一套，后续会和 Finley、Cosmic Duality、Timewarped 第二技能互相覆盖。
- P3 必须避免的失败状态：玩家选到完全不可执行的英雄技能；临时技能覆盖主技能后回合结束不能恢复；Genn 替换时把 Cosmic Duality 的额外技能吞掉；同一个英雄技能同时出现在主槽和额外槽；UI 显示多个按钮但命令仍默认打主技能。

### 已确认语义

- Rat King 在每回合开始时随机切换当前随从类型；英雄技能使用后不额外推进类型。
- Rat King 发现的是当时当前池里符合该类型的候选；如果 Buddy 已因特殊规则进入对应池子，则可以被发现，不需要为 Rat King 单独硬编码 Buddy 入口。
- Rat King 的当前类型必须能被 Nguyen 这类英雄技能候选 UI 看见：当 `A Tale of Kings` 出现在候选里时，玩家要知道当前会发现哪个类型。
- Genn 在 Cosmic Duality 畸变中禁用，不进入 Cosmic Duality 的第二英雄技能候选。
- Cosmic Duality / Timewarped 已获得的额外英雄技能允许被后续替换规则替换；P3.5 需要明确替换时清理旧额外槽。
- `Planned` / `FrameworkFirst` / `Deferred` 候选先完整列出，由你逐项判定过滤、显示代理标签，还是允许进入候选。

### P3 共用底座

| 能力 | 要落的规则 | 服务对象 |
| --- | --- | --- |
| 英雄技能槽位模型 | 至少区分 `Primary`、`Extra`、`TemporaryOverride`、`PendingReplacement`；每个槽位记录 card id、来源、解锁回合、持续时间、替换目标和是否可见/可用 | Cosmic Duality、Timewarped 第二技能、Nguyen、Genn |
| 候选过滤策略 | 从 `HeroCatalog.GetDiscoverableHeroPowers(...)` 或等价入口生成候选；排除当前已拥有技能；按 `HeroEffectImplementationRegistry` 标记候选状态；Cosmic Duality 下额外排除 Genn | Finley、Cosmic Duality、Nguyen、Genn、Training Session |
| 使用命令 | `UseHeroPower` 必须继续支持指定 `heroPowerCardId`；UI 选择额外/临时技能时不能回落到主技能 | Cosmic Duality、Nguyen、Genn |
| 生命周期清理 | 永久额外技能跨回合保留，直到被明确替换；Nguyen 临时技能回合结束恢复；Genn Turn 4 替换后持久化；锁定技能在解锁回合前不可用 | Nguyen、Genn、Timewarped 第二技能 |
| 状态可解释性 | Recruit log / debug 状态能说明技能来自哪里、为什么锁定、为什么被过滤或标成不完整 | UI smoke、候选状态报告、后续排障 |

### P3 直线执行路线

| 顺序 | 步骤 | 产出 | 验收 |
| ---: | --- | --- | --- |
| P3.0 | 候选状态审计先行 | 生成 Finley / Cosmic Duality / Nguyen / Genn 共用的英雄技能候选状态报告，列出 `Implemented`、`FrameworkFirst`、`Planned`、`Deferred`、当前已拥有、初始限定和不可替换项 | 报告能解释每个候选为什么可选、过滤或待确认；测试覆盖 `Deferred` 不会静默进入可用候选 |
| P3.1 | 多槽状态和 UI/命令契约 | 在现有 `ExtraHeroPowerCardIds` 基础上补足槽位元数据或等价结构；保留当前主技能行为；额外/临时技能都通过指定 card id 使用 | 主技能、额外技能、锁定额外技能、临时技能的命令分发测试；Unity Trainer smoke 覆盖多按钮和拖拽/点击目标 |
| P3.2 | The Rat King / Pigeon Lord | 每回合开始随机切换当前合法随从类型；英雄技能发现当时当前池里的该类型候选；候选 UI 显示当前类型；Pigeon Lord 在酒馆没有当前类型时让刷新免费 | 禁用种族过滤、当前类型持久化、候选 UI 类型提示、指定类型 Discover、免费刷新和刷新后恢复费用测试 |
| P3.3 | Master Nguyen / Lei Flamepaw | 回合开始二选一临时英雄技能；选择后本回合覆盖可用技能；回合结束恢复 `Power of the Storm`；Lei Flamepaw 按当前英雄技能映射 Buddy | 临时选择、使用临时技能、回合结束恢复、候选过滤、Buddy 映射和 UI smoke 测试 |
| P3.4 | Cosmic Duality 复查 | 复查第二英雄技能候选是否包含未完成技能；把 P3.0 的过滤/标注策略接入 Cosmic Duality 和同类第二技能来源 | Cosmic Duality 不静默给完全不可执行技能；选择后只加入额外槽，不覆盖主槽或临时槽 |
| P3.5 | Genn, Worgen King | 非 Cosmic Duality 局中，Turn 4 发现两个英雄技能替换 `King of Duality`；后续替换规则可以清理 Cosmic Duality / Timewarped 旧额外槽 | Cosmic Duality 局禁用 Genn；Turn 4 多选、两个持久技能、费用/解锁、额外槽替换清理、回合持久化和 UI smoke 测试 |

### 分项最小实现说明

| 英雄 / 机制 | 当前状态 | 最小实现顺序 | 不能跳过的验收 |
| --- | --- | --- | --- |
| The Rat King / Pigeon Lord | `Planned` | 先加回合开始随机当前类型状态；再接候选 UI 类型提示；再接英雄技能 Discover；最后接 Pigeon Lord 免费刷新判定 | 当前类型不落到禁用种族；Discover 使用当时当前池；特殊规则已加入池子的 Buddy 可自然出现；酒馆有当前类型时刷新正常收费，酒馆没有时免费 |
| Master Nguyen / Lei Flamepaw | `Implemented` | 已复用 Discover 队列和临时主技能覆盖；回合开始二选一，选择后本回合替换，回合结束恢复；Lei Flamepaw 延迟到选择后按临时技能映射 Buddy | 临时技能不会永久改写主技能；选择后的技能可被 UI 指定使用；Lei Flamepaw 不会给不存在或被过滤技能的 Buddy |
| Cosmic Duality 候选过滤 | 已实现，需复查 | 先跑候选状态报告；再按产品决策过滤或标注；最后补回归和 UI 状态 | `Deferred` / 不可执行技能不再伪装成完整可用；已拥有技能不会重复出现 |
| Genn, Worgen King | `Deferred` | 必须等 P3.1 和 P3.4 完成；先实现 Cosmic Duality 局禁用；再实现 Turn 4 pending replacement；再做两个持久技能槽；最后处理额外槽替换清理 | `King of Duality` 被替换后不再可用；两个新技能都能被显示和使用；旧 Cosmic Duality/Timewarped 额外技能可按替换规则清理 |

### 待你描述 / 待确认

1. Master Nguyen 与 Lei Flamepaw 的同回合开始顺序已按项目规则落地：先创建 Nguyen 临时英雄技能选择；玩家选择后，Lei Flamepaw 按本回合临时技能给 Buddy；若没有可用临时候选则回退当前/旧技能。
2. 第二/临时英雄技能候选遇到 `Planned` / `FrameworkFirst` / `Deferred` 时：下面清单逐项判定过滤、显示“不完整/代理”，还是允许进入候选。

### P3 英雄技能候选待判别清单

细化实现路线见 `Docs/HeroPowerProxyCandidateImplementationPlan.md`。

当前代码的普通英雄技能发现入口只取 `DiscoverableAfterStart`。下表是会进入 Finley / Cosmic Duality / Nguyen / Training Session 这类发现池的未完成状态候选。

| 状态 | 英雄 | 英雄技能 | CardId | 建议默认 | 你的判定 |
| --- | --- | --- | --- | --- | --- |
| `Deferred` | Mister Clocksworth | Double Time | `BG34_HERO_002p` | 过滤 | 待判定 |
| `FrameworkFirst` | Al'Akir | Swatting Insects | `TB_BaconShop_HP_086` | 标注代理 | 待判定 |
| `FrameworkFirst` | Arch-Villain Rafaam | I'll Take That! | `TB_BaconShop_HP_053` | 标注代理 | 待判定 |
| `FrameworkFirst` | Bru'kan | Embrace the Elements | `BG22_HERO_001p` | 标注代理 | 待判定 |
| `FrameworkFirst` | Deathwing | ALL Will Burn! | `TB_BaconShop_HP_061` | 标注代理 | 待判定 |
| `FrameworkFirst` | Galewing | Dungar's Gryphon | `BG20_HERO_283p` | 标注代理 | 待判定 |
| `FrameworkFirst` | Greybough | Sprout It Out! | `TB_BaconShop_HP_107` | 标注代理 | 待判定 |
| `FrameworkFirst` | Illidan Stormrage | Wingmen | `TB_BaconShop_HP_069` | 标注代理 | 待判定 |
| `FrameworkFirst` | Ini Stormcoil | MechGyver | `BG22_HERO_200p` | 标注代理 | 待判定 |
| `FrameworkFirst` | Lord Jaraxxus | Bloodfury | `TB_BaconShop_HP_036` | 标注代理 | 待判定 |
| `FrameworkFirst` | Mr. Bigglesworth | Kel'Thuzad's Kitty | `TB_BaconShop_HP_080` | 标注代理 | 待判定 |
| `FrameworkFirst` | Onyxia | Broodmother | `BG22_HERO_305p` | 标注代理 | 待判定 |
| `FrameworkFirst` | Rokara | Glory of Combat | `BG20_HERO_100p` | 标注代理 | 待判定 |
| `FrameworkFirst` | Scabbs Cutterbutter | I Spy | `BG21_HERO_010p` | 标注代理 | 待判定 |
| `FrameworkFirst` | Shudderwock | Snicker-snack | `TB_BaconShop_HP_022` | 标注代理 | 待判定 |
| `FrameworkFirst` | Sylvanas Windrunner | Reclaimed Souls | `BG23_HERO_306p` | 标注代理 | 待判定 |
| `FrameworkFirst` | Tamsin Roame | Fragrant Phylactery | `BG20_HERO_282p` | 标注代理 | 待判定 |
| `FrameworkFirst` | Tavish Stormpike | Deadeye | `BG22_HERO_000p` | 标注代理 | 待判定 |
| `FrameworkFirst` | Teron Gorefiend | Rapid Reanimation | `BG25_HERO_103p` | 标注代理 | 待判定 |
| `FrameworkFirst` | Tess Greymane | Bob's Burgles | `TB_BaconShop_HP_077` | 标注代理 | 待判定 |
| `FrameworkFirst` | The Great Akazamzarak | Prestidigitation | `TB_BaconShop_HP_020` | 标注代理 | 待判定 |
| `FrameworkFirst` | The Jailer | Runic Empowerment | `TB_BaconShop_HP_702` | 标注代理 | 待判定 |
| `FrameworkFirst` | Vol'jin | Spirit Swap | `BG20_HERO_201p` | 标注代理 | 待判定 |
| `FrameworkFirst` | Yogg-Saron, Hope's End | Puzzle Box | `TB_BaconShop_HP_039t` | 标注代理 | 待判定 |
| `Planned` | Lady Vashj | Relics of the Deep | `BG23_HERO_304p` | 过滤 | 待判定 |
| `Planned` | Lord Barov | Friendly Wager | `TB_BaconShop_HP_081` | 过滤 | 待判定 |
| `Implemented` | Master Nguyen | Power of the Storm | `BG20_HERO_202p` | 已完成；不再作为未完成候选待判定 | 已完成 |
| `Planned` | Murloc Holmes | Detective for Hire | `BG23_HERO_303p2` | 过滤 | 待判定 |
| `Planned` | Queen Azshara | Azshara's Ambition | `BG22_HERO_007p` | 过滤 | 待判定 |
| `Planned` | The Rat King | A Tale of Kings | `TB_BaconShop_HP_041` | P3.2 完成后允许 | 待判定 |

下面这些也是 `Planned` / `FrameworkFirst` / `Deferred`，但当前 `replacementEligibility` 不是 `DiscoverableAfterStart`，默认不会进入普通英雄技能发现池；只在专门规则修改时才需要判定。

| 资格 | 状态 | 英雄 | 英雄技能 | CardId | 默认处理 |
| --- | --- | --- | --- | --- | --- |
| `InitialOnly` | `Deferred` | Genn, Worgen King | King of Duality | `BG35_HERO_001p` | Cosmic Duality 下禁用 |
| `InitialOnly` | `FrameworkFirst` | Aranna Starseeker | Demon Hunter Training | `TB_BaconShop_HP_065` | 不进普通候选 |
| `InitialOnly` | `FrameworkFirst` | N'Zoth | Avatar of N'Zoth | `TB_BaconShop_HP_105` | 不进普通候选 |
| `InitialOnly` | `FrameworkFirst` | Ozumat | Tentacular | `BG23_HERO_201p` | 不进普通候选 |
| `InitialOnly` | `FrameworkFirst` | Sneed | Pilot the Shredder | `BG21_HERO_030p` | 不进普通候选 |
| `InitialOnly` | `Planned` | Dinotamer Brann | Battle Brand | `TB_BaconShop_HP_048` | 不进普通候选 |
| `InitialOnly` | `Planned` | Loh, the Living Legend | Heroic Inspiration | `BG33_HERO_001p_ALT` | 不进普通候选 |
| `Disabled` | `FrameworkFirst` | Artanis | Warp Gate | `BG31_HERO_802p` | 不进普通候选 |
| `Disabled` | `FrameworkFirst` | Buttons | Growing Collection | `BG32_HERO_002p` | 不进普通候选 |
| `Disabled` | `FrameworkFirst` | Jim Raynor | Lift Off | `BG31_HERO_801p` | 不进普通候选 |
| `Disabled` | `FrameworkFirst` | Kerrigan, Queen of Blades | Spawning Pool | `BG31_HERO_811p` | 不进普通候选 |
| `Disabled` | `FrameworkFirst` | Marin the Manager | Fantastic Treasure | `BG30_HERO_304p` | 不进普通候选 |
| `Disabled` | `FrameworkFirst` | Morchie | Warped Conflux | `BG34_HERO_004p` | 不进普通候选 |
| `Disabled` | `FrameworkFirst` | Murozond, Unbounded | Alternate Timeline | `BG34_HERO_000p` | 不进普通候选 |
| `Disabled` | `FrameworkFirst` | Professor Putricide | Build-An-Undead | `BG25_HERO_100p` | 不进普通候选 |

## P4：对手历史、预测和真实大厅信息

目标：统一 opponent snapshot / last combat memory / eliminated player snapshot / battle prediction，不让每个英雄自己造代理。

| 顺序 | 英雄 / 宝宝 | 当前状态 | 共用机制 | 当前阻塞 | 最小实现顺序 | 验收 |
| ---: | --- | --- | --- | --- | --- | --- |
| 1 | Murloc Holmes / Watfin | `Planned` | 猜测 UI、下一对手上一场战斗记忆、Tavern Coin 奖励 | 单人酒馆没有真实下一对手 | 先用代理对手/最近战斗快照；实现二选一猜测；猜对给 Coin；宝宝给普通复制 | 猜测正确/错误、Coin 发放、宝宝复制、无快照降级测试 |
| 2 | Lord Barov / Barov's Apprentice | `Planned` | 战斗预测选择、战斗结果快照、Coin 打出监听 | 预测目标和结算必须绑定同一场战斗 | 预测 choice；战斗后结算 3 Coin；宝宝监听 Coin 打出给金币 | 预测胜/负/平、跨回合不串数据、Coin 触发测试 |
| 3 | Mr. Bigglesworth / Lil' K.T. | `FrameworkFirst` | 淘汰玩家战队、最低血量对手、发现队列 | 当前只有单人淘汰快照代理 | 保留现有代理；补真实大厅前只能 `FrameworkFirst`；发现队列已可支撑多次发现 | 代理边界测试；真实大厅缺失时 UI 不静默失败 |
| 4 | Scabbs / Warden Thelwater | `FrameworkFirst` | 下一对手排程、对手战队快照、Buddy 映射 | 当前下一对手是单人代理 | 与 Tess/Bigglesworth 共用快照模型；真实排程完成后收口 | 代理路径、无对手快照、Buddy 获取测试 |
| 5 | Tess / Hunter of Old | `FrameworkFirst` | 上一对手战队快照、上一对手 Buddy | 缺真实多玩家上一对手 | 复用 combat-start 快照；真实大厅接入后替换来源 | Bob's Burgles 刷新、宝宝获取、无历史回退测试 |
| 6 | Rafaam / Loyal Henchman | `FrameworkFirst` | 敌方死亡历史、击杀归属、第一/第二死亡奖励 | 非攻击击杀和完整坟场仍不全 | 先补战斗事件坟场，再收口 Rafaam；不要再扩散一套英雄私有死亡记录 | 攻击击杀、非攻击击杀、第一/第二死亡、宝宝奖励测试 |

待确认：

- 单人酒馆下“下一对手”“最低血量对手”“已淘汰玩家”的代理规则是否继续沿用当前调试快照，还是需要你指定固定模拟规则。

## P5：战斗事件公共框架收口

目标：先补事件源，再批量关闭依赖战斗内部事件的 `FrameworkFirst` / `Planned` 项。

必须优先补的公共能力：

| 能力 | 服务对象 |
| --- | --- |
| 友方攻击次数统计、攻击开始/结束事件、立即攻击队列 | Loh、Aranna、Illidan、Onyxia |
| 击杀归属，含攻击、反击、法术、亡语、召唤物伤害 | Rafaam、Rokara、Tavish |
| 友方/敌方死亡历史和死亡时属性快照 | Tamsin、Teron、Sylvanas、Jailer、Rafaam |
| 亡语 payload、亡语复制、亡语召唤和死亡位置 | N'Zoth、Sneed、Al'Akir、Brann's Epic Egg |
| 战斗召唤统一 resolver 和实时棋盘回写 | Greybough、Ozumat、Onyxia、Teron |
| 受到伤害、造成伤害、嘲讽被攻击监听 | Lord Jaraxxus、Greybough、Tavish |
| 战斗开始英雄触发排序 | Illidan、Bru'kan、Deathwing、Al'Akir |

建议实现顺序：

1. 事件模型与测试夹具：先把 CombatEngine 的攻击、击杀、死亡、召唤、伤害事件统一成可断言记录。
2. 攻击计数和立即攻击：收口 Loh、Aranna、Illidan、Onyxia。
3. 击杀归属和坟场：收口 Rafaam、Rokara、Sylvanas、Tavish。
4. 亡语 payload 和召唤位置：收口 N'Zoth、Sneed、Tamsin、Teron、The Jailer、Al'Akir、Dinotamer Brann。
5. 伤害和嘲讽被攻击监听：收口 Lord Jaraxxus、Greybough。
6. 元素选择和战斗开始执行：收口 Bru'kan。

涉及英雄清单：

- `Planned`：Loh, the Living Legend；Dinotamer Brann。
- `FrameworkFirst`：Al'Akir、Deathwing、Illidan、N'Zoth、Tavish、Tamsin、Teron、Rafaam、Rokara、Sylvanas、Sneed、The Jailer、Greybough、Onyxia、Ini Stormcoil、Ozumat、Aranna、Lord Jaraxxus、Bru'kan、Vol'jin、Shudderwock。

验收要求：

- 每个事件必须有 CombatEngine 层测试，不只测 HeroEffectEngine 事后代理。
- 至少一个英雄测试覆盖真实战斗结算路径，不允许只靠“出售代理”通过。
- 注册表说明要区分“战斗后奖励回写已做”和“战斗中实时影响胜负已做”。

待确认：

- 部分官方触发顺序如果本地文档没有写明，尤其是多个英雄战斗开始效果的优先级，需要在实现前确认。

## P6：专属大机制和跨系统英雄

目标：为独立大系统建立数据、候选池、执行入口和测试，不在 HeroEffectEngine 里堆单点逻辑。

| 分组 | 英雄 / 宝宝 | 当前状态 | 共用机制 | 当前阻塞 | 最小实现顺序 | 验收 |
| --- | --- | --- | --- | --- | --- | --- |
| Spellcraft / Naga | Lady Vashj / Coilfang Elite；Queen Azshara / Imperial Defender | `Planned` | Spellcraft 临时法术、回合结束清理、Naga Conquest、友方法术复制 | Spellcraft 不能当永久普通手牌 | 先做临时 Spellcraft 牌和清理；再做酒馆 Spellcraft 复制；最后做 Naga Conquest 与每回合一次复制 | 临时牌清理、复制候选、阈值解锁、每回合一次测试 |
| Trinket | Marin / Fantastic Bellhop；Buttons / Zippers | `FrameworkFirst` | Lesser/Greater Trinket 选择、候选过滤、复制/排除规则 | 当前只有 helpful card/部分代理 | 先做 Trinket catalog/status；再做选择 UI；最后迁移 Marin/Buttons | 不可执行饰品过滤、大小饰品槽、宝宝代理迁移测试 |
| TripleEngine | Mister Clocksworth | `Deferred` | 两张即可合金、三连奖励替换为 Tavern Coin | TripleEngine 规则不可配置 | 先让三连规则按英雄可配置；再替换奖励生成 | 两张合金、普通三连不受影响、奖励替换测试 |
| Secret | The Great Akazamzarak / Street Magician | `FrameworkFirst` | Secret 选择、挂载、触发、移除、战斗时点 | Better Secret 仍是 proxy | 先做 Secret 状态和选择；再接战斗触发；最后替换 Better Secret proxy | 选择/挂载/触发/移除、宝宝 Better Secret 测试 |
| Custom Undead | Professor Putricide / Festergut | `FrameworkFirst` | Undead creation 组件池、费用、结果随从生成 | 只有 Undead Creation proxy | 先做制作命令流；再做组件池和结果生成；最后迁移 Festergut | 制作结果、费用、关键词/亡语 payload、宝宝触发测试 |
| StarCraft | Jim Raynor / Tychus；Artanis / Probius；Kerrigan / Broken Horn | `FrameworkFirst` | Terran/Battlecruiser、Protoss 奖励、Zerg morph | 目前是升级/磁力/6-6 Zerg proxy | 分别建立 Terran、Protoss、Zerg 子系统；再迁移宝宝代理 | 各种族专属池、升级链、延迟奖励、变形限制测试 |
| Timewarped / Timeline | Morchie；Murozond | `FrameworkFirst` | Minor/Major Timewarp 已打开；时间线和对手历史扩展 | Murozond 对手历史扩展未完成 | 保留已完成开放时点；补时间线快照；再接 Murozond 历史奖励 | Turn 5/8 开门回归、时间线快照、历史奖励测试 |

待确认：

- Trinket 候选中 `Exact`、`ProxySafe`、`Blocked/DebugOnly` 的产品展示规则需要你定：过滤掉，还是允许但标注代理。
- StarCraft 三个子系统的优先级需要你定；建议先从最小闭环的 Terran/Battlecruiser 做起，因为已有 Battlecruiser Upgrade proxy。

## 专门需要你说明的事项

只有下面这些在现有文档不足或属于产品取舍时需要你补充：

1. 第二英雄技能候选遇到未完成技能时：过滤，还是显示并标注“不完整/代理”。
2. 单人酒馆里的真实大厅代理规则：下一对手、最低血量、淘汰玩家、预测目标是否继续沿用当前快照代理。
3. Darkmoon Prize、Wheel of Yogg、Secret、Trinket、StarCraft 这类大机制，如果短期不能一次做全，哪些允许先以 proxy 状态上线。
4. 官方触发顺序缺失时，是否采用项目内部统一顺序，还是等你提供具体规则。

## 下一步执行建议

1. 先实现 P0.1 `Galewing / Flight Trainer`。
2. P0 完成后做一次 Cosmic Duality / BuddyPool 候选状态报告，决定未完成英雄技能是否过滤。
3. P1 `Ambassador Faelin / Thorim` 已完成；继续前先保留 focused 回归。
4. P2 中 Denathrius 和 Tickatus 已完成，Yogg 先以 Wheel proxy 标 `FrameworkFirst`。
5. P5、P6 不要穿插单个英雄硬做；先建公共机制，再批量收口注册表状态。
