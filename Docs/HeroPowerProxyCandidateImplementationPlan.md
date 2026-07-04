# P3 英雄技能代理候选实现计划

更新时间：2026-07-04

## 目标

这份文档承接 `HeroPowerReplacementP3ImplementationPlan.md`。阮大师 / 雷焰爪已先行实现；下一步要决定剩余 `DiscoverableAfterStart` 候选在 Master Nguyen、Cosmic Duality、Cosmic Reward、Timewarped 第二英雄技能等来源里应该：

1. 直接过滤；
2. 显示但标注“不完整/代理”；
3. 完成前置机制后开放准入；
4. 直接作为已实现技能开放。

## 已查来源

- 官方 Hearthstone 站点 API，英文批量查询：`https://hearthstone.blizzard.com/en-us/api/cards?locale=en_US&ids=80229,71464,71909,77911,75703,76563,77990,79720,79619,81572,82114,90403,85126,89294,98728,126533,58022,58028,59808,122958,63127,60381,61406,61851,63162,63320,63600,64402,67554,97185&gameMode=battlegrounds`
- 官方 Hearthstone 站点 API，中文批量查询：`https://hearthstone.blizzard.com/zh-cn/api/cards?locale=zh_CN&ids=80229,71464,71909,77911,75703,76563,77990,79720,79619,81572,82114,90403,85126,89294,98728,126533,58022,58028,59808,122958,63127,60381,61406,61851,63162,63320,63600,64402,67554,97185&gameMode=battlegrounds`
- 官方 API 返回 28/30 张。未返回 `BG22_HERO_000p` Tavish / Deadeye 和 `TB_BaconShop_HP_039t` Yogg / Puzzle Box。
- HearthstoneJSON 辅证缺失两张：`https://api.hearthstonejson.com/v1/latest/enUS/cards.json` 返回 `Deadeye` 与 `Puzzle Box` 文本。
- 本地项目数据源：`Assets/LearnHearthstone/Resources/Data/battlegroundsHeroes.json`，其元数据来源包含 Firestone 页面与 cards_enUS 数据。

## 需要你判定

1. `FrameworkFirst` 候选默认是过滤，还是允许进入候选但 UI/日志标注“代理/不完整”。我的建议：允许进入，但必须显示成熟度标签。
2. 依赖真实对手或下个对手的技能，在单人训练器里接受“当前/上次对手快照”代理；依赖英雄死亡/淘汰或下注竞猜的技能直接过滤。
3. 纯战斗开始/战斗内被动技能作为 Nguyen 的临时技能时，本回合如果已经过了 Tavern 操作窗口，是否仍允许选择。我的建议：允许，因为选择发生在本回合开始，仍会影响随后战斗。
4. Tavish 语义按项目规则确认：移除一个目标，之后有空位时直接发射/结算；不是竞猜或真实大厅阻塞。

## 默认准入策略

| 状态 | 默认处理 | 原因 |
| --- | --- | --- |
| `Implemented` | 开放准入 | 已有可执行路径和 focused 测试。 |
| `FrameworkFirst` | 显示代理标签后开放，或按你的判定过滤 | 大多已有可玩近似或框架入口，但缺真实大厅/完整 UI/完整战斗语义。 |
| `Planned` | 默认过滤 | 缺关键机制，进入候选会误导为可用。 |
| `Deferred` | 默认过滤 | 明确依赖大系统，暂不进入普通候选。 |
| 延迟触发且改核心规则 | 默认过滤 | 例如 Double Time 会改三连/金色规则，不能用普通代理安全替代。 |

## 候选逐项计划

| 状态标记 | 英雄 | 英雄技能 | 卡牌 ID | 官方/API 证据 | 建议默认处理 | 实现直线 |
| --- | --- | --- | --- | --- | --- | --- |
| 延迟触发型 | 时钟先生 | Double Time / 双倍速 | `BG34_HERO_002p` | 官方 API：2 复制成金，金色随从不给三连奖励、改给酒馆币。 | 直接过滤 | 等二复制 TripleEngine 与“金色不发 Triple Reward”规则完成后再开放。 |
| FrameworkFirst | 奥拉基尔 | Swatting Insects / 随风而行 | `TB_BaconShop_HP_086` | 官方 API：战斗开始最左随从获 Windfury/Divine Shield/Taunt。 | 标注代理占位 | 复查战斗开始关键字已落地；补 Nguyen/Cosmic 候选标签测试。 |
| FrameworkFirst | 反派大盗拉法姆 | I'll Take That! / 归我了 | `TB_BaconShop_HP_053` | 官方 API：下场战斗获得你消灭的第一个随从原始复制。 | 标注代理占位 | 接入战斗击杀首个敌方随从快照；无真实战斗目标时保持代理标签。 |
| FrameworkFirst | 布鲁坎 | Embrace the Elements / 拥抱元素 | `BG22_HERO_001p` | 官方 API：选择元素，战斗开始唤起所选元素。 | 标注代理占位 | 先做元素选择状态，再做 4 元素战斗开始效果与 UI 标签。 |
| FrameworkFirst | 死亡之翼 | ALL Will Burn! / 万物尽焚 | `TB_BaconShop_HP_061` | 官方 API：战斗开始所有随从永久 +2 攻击。 | 标注代理占位 | 复查双方永久攻击写回；补候选准入测试。 |
| FrameworkFirst | 风翼 | Dungar's Gryphon / 杜加尔的狮鹫 | `BG20_HERO_283p` | 官方 API：选择航线，完成获得奖励。 | 标注代理占位 | 已有代理航线奖励；保留 `FrameworkFirst`，等真实航线奖励数据补齐。 |
| FrameworkFirst | 灰枝 | Sprout It Out! / 老树新芽 | `TB_BaconShop_HP_107` | 官方 API：战斗阶段召唤的随从 +1/+2 Taunt。 | 标注代理占位 | 复查战斗召唤修饰器；缺失时接入 combat summon modifier。 |
| FrameworkFirst | 伊利丹 | Wingmen / 左膀右臂 | `TB_BaconShop_HP_069` | 官方 API：战斗开始左右随从 +2/+1 并立即攻击。 | 标注代理占位 | 完整实现需要战斗动作插队；先保留代理标签。 |
| FrameworkFirst | 伊妮 | MechGyver / 敲打机械 | `BG22_HERO_200p` | 官方 API：9 个友方随从死亡后随机获取机械，循环。 | 标注代理占位 | 用 combat/tavern 友方死亡计数器，奖励当前池机械。 |
| FrameworkFirst | 加拉克苏斯 | Bloodfury / 血怒 | `TB_BaconShop_HP_036` | 官方 API：友方随从造成 150 伤害后开传送门。 | 标注代理占位 | 需要战斗伤害统计和传送门奖励定义；先不标完整。 |
| FrameworkFirst | 比格沃斯先生 | Kel'Thuzad's Kitty / 克尔苏加德的猫 | `TB_BaconShop_HP_080` | 官方 API：其他英雄死亡后，从其战队发现随从并保留附加效果。 | 直接过滤 | 用户已判定删除此代理候选；不再用淘汰/低血量快照模拟。等真实大厅死亡/淘汰快照系统完成后再重新评估。 |
| FrameworkFirst | 奥妮克希亚 | Broodmother / 巢母 | `BG22_HERO_305p` | 官方 API：Avenge(4) 召唤会立即攻击的 Whelp，效果成长。 | 标注代理占位 | 接入 Avenge 计数、Whelp token、立即攻击；UI 标代理直到战斗动作完整。 |
| FrameworkFirst | 罗卡拉 | Glory of Combat / 战斗的荣耀 | `BG20_HERO_100p` | 官方 API：友方随从击杀后永久 +1 攻击。 | 标注代理占位 | 复查击杀事件永久写回；补候选标签回归。 |
| FrameworkFirst | 斯卡布斯 | I Spy / 间谍探查 | `BG21_HERO_010p` | 官方 API：发现下个对手战队随从原始复制。 | 标注代理占位 | 当前可用单人“当前/上次对手”代理；真实排程前不标完整。 |
| FrameworkFirst | 沙德沃克 | Snicker-snack / 奇诡尖啸 | `TB_BaconShop_HP_022` | 官方 API：触发友方随从 Battlecry，第 3 回合解锁。 | 标注代理占位 | 已有战吼重放框架；补双目标 UI 和已实现战吼边界说明。 |
| FrameworkFirst | 希尔瓦娜斯 | Reclaimed Souls / 重拾灵魂 | `BG23_HERO_306p` | 官方 API：发现上一场战斗死亡的随从原始复制，第 3 回合解锁。 | 标注代理占位 | 用上一场战斗死亡快照；真实完整附加效果另列。 |
| FrameworkFirst | 塔姆辛 | Fragrant Phylactery / 香氛护命匣 | `BG20_HERO_282p` | 官方 API：战斗开始给最低攻随从亡语，使其他随从获得其属性。 | 标注代理占位 | 需要战斗开始选择、亡语挂载、属性广播。 |
| FrameworkFirst | 塔维什 | Deadeye / 精准狙击 | `BG22_HERO_000p` | 官方站点 API 未返回；HearthstoneJSON：瞄准，战斗开始对目标造成 99 伤害。 | 标注代理占位 | 按用户确认的项目语义实现：移除一个目标，之后有空位时直接发射/结算；补目标选择状态、移除记录和空位触发。 |
| FrameworkFirst | 泰隆 | Rapid Reanimation / 飞速复活 | `BG25_HERO_103p` | 官方 API：选择友方随从，战斗开始消灭，有空位时复活完全复制。 | 标注代理占位 | 需要目标选择、战斗开始死亡、空位复活 exact copy。 |
| FrameworkFirst | 苔丝 | Bob's Burgles / 鲍勃的豪夺 | `TB_BaconShop_HP_077` | 官方 API：刷新酒馆为上个对手战队原始复制。 | 标注代理占位 | 已有上次对手快照方向；真实对手历史完整前保留代理。 |
| FrameworkFirst | 阿扎扎拉克 | Prestidigitation / 神奇魔术 | `TB_BaconShop_HP_020` | 官方 API：选择奥秘并置入战场。 | 标注代理占位 | 需要 Secret 系统；短期用 Better Secret proxy。 |
| FrameworkFirst | 典狱长 | Runic Empowerment / 符文强化 | `TB_BaconShop_HP_702` | 官方 API：给随从 +/+，五个友方随从死亡后提升。 | 标注代理占位 | 接目标 buff、死亡计数、成长数值显示。 |
| FrameworkFirst | 沃金 | Spirit Swap / 灵魂互换 | `BG20_HERO_201p` | 官方 API：选择两个随从，直到下回合互得攻击力。 | 标注代理占位 | 需要双目标 UI 和下回合恢复/到期状态。 |
| FrameworkFirst | 尤格萨隆 | Puzzle Box / 迷之匣 | `TB_BaconShop_HP_039t` | 官方站点 API 未返回；HearthstoneJSON：第 3 回合后回合开始施放随机酒馆法术。 | 可开放但标注代理 | 英雄技能本体已可玩；注册表仍因 Buddy Wheel 代理保持 `FrameworkFirst`。候选标签应说明“技能可用，伙伴轮盘仍代理”。 |
| Planned | 瓦丝琪女士 | Relics of the Deep / 深海遗物 | `BG23_HERO_304p` | 官方 API：每回合开始随机获得 Spellcraft 法术。 | 直接过滤 | 等 Spellcraft 生成池、临时法术清理、Naga 相关交互稳定后开放。 |
| Planned | 巴罗夫领主 | Friendly Wager / 友好投注 | `TB_BaconShop_HP_081` | 官方 API：猜下场战斗胜者，猜中得 3 张酒馆币。 | 直接过滤 | 需要真实对阵预测/选择 UI；单人代理需你确认。 |
| Implemented | 阮大师 | Power of the Storm / 风暴之力 | `BG20_HERO_202p` | 官方 API：每回合开始从 2 个新英雄技能中选择。 | 已完成，不再作为未完成候选 | 已实现临时选择、恢复、Lei Flamepaw 延迟 Buddy。 |
| Planned | 鱼人福尔摩斯 | Detective for Hire / 特邀侦探 | `BG23_HERO_303p2` | 官方 API：看 2 个随从，猜中来自下个对手上一场战斗的随从得酒馆币。 | 直接过滤 | 需要下个对手上一场战斗快照和竞猜 UI。 |
| Planned | 艾萨拉女王 | Azshara's Ambition / 艾萨拉的野心 | `BG22_HERO_007p` | 官方 API：战队 30 攻后开启纳迦远征。 | 直接过滤 | 需要 Naga expedition 奖励/形态切换；前置系统未定。 |
| Planned | 鼠王 | A Tale of Kings / 鼠王的故事 | `TB_BaconShop_HP_041` | 官方 API：发现特定类型随从，每回合切换类型。 | P3.2 完成后开放准入 | 按用户语义：回合开始随机切换类型，发现使用当时当前池；Buddy 只在特殊规则已入池时自然出现。 |

## 推荐执行顺序

1. 候选过滤/标签中间层：把 `HeroEffectImplementationRegistry` 状态接到 `HeroCatalog.GetDiscoverableHeroPowers` 或其调用点，先只影响 Nguyen/Cosmic/Timewarped 候选显示，不改各英雄效果。
2. 可开放的 `FrameworkFirst` 标签：为已能近似运行的技能保留候选，但在 option tags、日志、UI 状态里写入 `proxy_hero_power` / `framework_first`。
3. 直接过滤组：`Double Time`、`Deferred`、`Planned` 未完成项默认不进候选，除非你逐项允许代理。
4. 高价值补全组：优先补 `Yogg Puzzle Box` 候选标签清理、`Rokara/Al'Akir/Deathwing` 这类已有战斗入口的 focused 回归，再处理 Rafaam/Scabbs/Tess 这些可面向模拟对手的技能。
5. 大系统/过滤组：Secret、Spellcraft/Naga、Tavish 移除后发射、Double Time 二复制三连系统单独开批；Bigglesworth、Barov、Holmes 暂时直接过滤。

## 验收矩阵

- Nguyen：未完成候选按策略过滤或带标签；选择后临时技能仍能恢复。
- Cosmic Duality / Cosmic Reward：不会静默给 `Deferred` 或未允许的 `Planned` 技能。
- Timewarped 第二英雄技能：同一套候选策略，不私有分叉。
- UI：代理候选必须可见标注，不能只在日志里出现。
- Registry：候选策略测试覆盖 `Implemented`、`FrameworkFirst`、`Planned`、`Deferred`、延迟触发型过滤。
# 2026-07-04 Implementation Update

- Shared candidate policy is now implemented in `HeroCatalog.GetOfferableDiscoverableHeroPowers`.
- Directly filtered from replacement/discover pools: Double Time, Bigglesworth, Barov, Holmes, and all non-offerable `Planned`/`Deferred`/`Unregistered` Hero Powers.
- `FrameworkFirst` Hero Powers remain candidate-eligible and generated options receive visible tags:
  - `implementation_status:FrameworkFirst`
  - `hero_power_proxy`
  - `framework_first`
  - `incomplete_hero_power`
- Implemented Hero Powers receive `implementation_status:Implemented` and no proxy tag.
- Nguyen, Cosmic/second Hero Power, Training Session, and Unmasked Identity now share the same offerable candidate policy.
- Start-of-combat Hero Power effects now receive the active Hero Power list, so unlocked second Hero Powers can trigger in the existing HeroEffectEngine phase before Trinket, Quest, and Timewarped combat-start effects.
- 2026-07-04 follow-up: Tavish, Tamsin, Onyxia, and Bru'kan have now been implemented in the focused combat-event batch with dedicated ordering tests. They remain `FrameworkFirst` for candidate labeling because broader UI polish and full generic combat-event coverage are still product follow-ups, but their runtime hero/buddy effects are connected.
