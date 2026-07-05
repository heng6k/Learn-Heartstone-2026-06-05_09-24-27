# 四个大型英雄机制实现计划

更新时间：2026-07-04

## 范围结论

本文件只覆盖四个当前仍需独立大机制收口的英雄：

| 英雄 | 英雄技能 | 卡牌 ID | 当前状态 | 本文件目标 |
| --- | --- | --- | --- | --- |
| 普崔塞德教授（Professor Putricide） | 构造亡灵 / Build-An-Undead | `BG25_HERO_100p` | `FrameworkFirst` | 从随机亡灵代理迁移到自定义亡灵系统 |
| 吉姆·雷诺（Jim Raynor） | 升空 / Lift Off | `BG31_HERO_801p` | `Implemented` | 已从 `+3/+3` 战巡升级代理迁移到官方战列巡航舰升级链 |
| 刀锋女王凯瑞甘（Kerrigan, Queen of Blades） | 孵化池 / Spawning Pool | `BG31_HERO_811p` | `Implemented` | 已从 6/6 Zerg proxy 迁移到异虫解锁、幼虫和变异系统 |
| 阿塔尼斯（Artanis） | 折跃门 / Warp Gate | `BG31_HERO_802p` | `Implemented` | 已从伙伴磁力代理扩展到星灵二选一和买 14 张牌后获得奖励 |

`Mr. Bigglesworth` 按用户决策从本轮删除：不进入普通英雄技能候选，不作为本文件实现目标。它仍可能在注册表中保留 `FrameworkFirst`，原因是项目缺少真实 8 人大厅淘汰和最低血量对手系统；这不是本轮要解决的问题。

## 资料依据

| 来源 | 用途 | 当前结论 |
| --- | --- | --- |
| 旅法师营地酒馆战棋工具 `https://www.iyingdi.com/tz/tool/general/battlegrounds` | 用户指定的人工核对入口。页面筛选项包含“普崔塞德 / 凯瑞甘 / 吉姆·雷诺 / 阿塔尼斯”，可用于核对图标和分类。 | 页面可访问，但本地没有确认稳定公开卡表 API；实现时优先用它做人工核对和图片/分类校验。 |
| HearthstoneJSON `https://api.hearthstonejson.com/v1/latest/zhCN/cards.json` | 中文卡牌名称、文本、费用、金色伙伴文本。 | 能确认四个英雄技能和伙伴的官方文本。 |
| HearthstoneJSON `https://api.hearthstonejson.com/v1/latest/enUS/cards.json` | 子卡 ID、机制字段、英文原文，尤其是 StarCraft 子系统。 | 能拉出 `BG31_HERO_801pt*` 战巡升级、`BG31_HERO_811t*` 异虫、`BG31_HERO_802pt*` 星灵奖励。 |
| 本地 `Assets/LearnHearthstone/Resources/Data/battlegroundsHeroes.json` | 当前项目的英雄/技能/伙伴基础数据和图片路径。 | 四个英雄的本体、技能、伙伴和本地图片路径已存在。 |
| 本地 `Assets/LearnHearthstone/Resources/HeroBuddyImages/manifest.json` | 英雄和伙伴图片源 URL。 | 四个英雄与伙伴的 zerotoheroes 图片 URL 已有；英雄技能图片在 `battlegroundsHeroes.json` 中有本地路径。 |

## 当前本地状态

注册表位置：`Assets/LearnHearthstone/Runtime/Domain/Data/HeroEffectImplementationRegistry.cs`

| 英雄 | 当前可运行内容 | 缺口 |
| --- | --- | --- |
| Putricide | `Festergut` 可在出售代理和战斗亡语路径中召唤/获取随机亡灵造物代理。 | 英雄技能 3 费 3 次“构造亡灵”未实现；亡灵造物不是官方自定义结果。 |
| Jim Raynor | 开局 2/2 战巡、刷新塞入官方升级、Tychus 给官方升级、核心升级和战斗钩子已接。 | 后续只剩 UI/资产/官方数值微调，不再阻塞英雄完成度。 |
| Kerrigan | 开局幼虫、费用递减、英雄技能阶段解锁、每回合 Zerg morph、Broken Horn 真实 6/6 非变异 Zerg 已接。 | 后续只剩 UI/资产/官方数值微调，不再阻塞英雄完成度。 |
| Artanis | 开局星灵二选一、买 14 张牌获得所选奖励、Probius 磁力变金和 Protoss 核心战斗钩子已接。 | 后续只剩 UI/资产/官方数值微调，不再阻塞英雄完成度。 |

## 不清楚或需补资料的点

1. Putricide 的“自定义亡灵”组件池没有像 StarCraft 子卡那样完整暴露在当前 HearthstoneJSON 查询结果里。能确认 `BG25_HERO_100pt` 是 `Putricide's Creation`，但无法仅凭公开 JSON 确认每个可选组件、组合费用和 UI 步骤。需要后续从营地“普崔塞德”筛选或官方客户端表现补全组件表。
2. 营地页面本身能显示四个分类和图标，但本地抓取时没有确认稳定公开 API。实现时如果要做到“完全按营地分类表导入”，需要人工截图/导出或继续逆向接口；如果只要求官方卡牌文本和子卡 ID，HearthstoneJSON 已足够支撑 StarCraft 三个系统的第一版。

## 统一实现原则

1. 四个英雄技能继续保持 `replacementEligibility: Disabled`，不进入 Nguyen、Cosmic Duality、Timewarped 等普通可替换技能池。它们即使转 `Implemented`，也只是“本体 gameplay 完整”，不是“可作为普通候选技能”。
2. Bigglesworth 不参与本批，不因为任何共享底座完成而自动转正。
3. 优先复用现有 `HeroEffectEngine`、`TavernSpellEngine`、`DiscoverState`、`MinionFactory`、`CombatEngine` 事件；只有在这些抽象无法承载官方语义时才新增 catalog/state。
4. 所有生成卡必须带稳定标签，例如 `terran_battlecruiser`、`battlecruiser_upgrade`、`zerg_morphing`、`zerg_no_morph`、`protoss_reward`、`putricide_creation`，方便测试和 UI 过滤。
5. 迁移完成后，proxy 常量只能作为兼容旧存档或测试兜底，不再是主路径。

## 建议实施顺序

### P0：资料和数据底座

目标：先把官方子卡表落成数据，避免继续在引擎里硬编码 proxy。

实现项：

1. 新增或扩展数据文件：
   - `Assets/LearnHearthstone/Resources/Data/battlegroundsStarCraftCards.json`
   - `Assets/LearnHearthstone/Resources/Data/battlegroundsCustomUndeadCards.json`
2. 导入 Raynor 子卡：
   - 战巡：`BG31_HERO_801pt`
   - 升级：`BG31_HERO_801pta*`、`ptb*`、`ptc*`、`ptd*`、`pte*`、`ptf*`、`pth*`、`pti*`、`ptj*`
3. 导入 Kerrigan 子卡：
   - 英雄技能阶段：`BG31_HERO_811p`、`BG31_HERO_811p2`、`BG31_HERO_811p3`
   - 幼虫和异虫：`BG31_HERO_811t`、`t2` 到 `t10` 及金色版本
4. 导入 Artanis 子卡：
   - 星灵奖励池：`BG31_HERO_802pt`、`pt1`、`pt4`、`pt5`、`pt7` 及金色/衍生 token
5. Putricide 先导入已确认 token：
   - `BG25_HERO_100pt`（Putricide's Creation）
   - `BG25_HERO_100_Buddy_G` 金色伙伴文本
   - 组件池待补，不硬造官方表。

验收：

- 数据 loader 能按 `cardId` 查询上述子卡。
- 图片路径使用现有 `HeroBuddyImages`；缺失子卡图片时先走统一生成牌 fallback，不阻塞逻辑。
- 新数据不改变普通酒馆随从池，除非英雄机制显式生成。

### P1：Jim Raynor / Terran Battlecruiser

官方文本：

- `Lift Off`：开局拥有一艘 2/2 战列巡航舰。每当酒馆刷新时，在酒馆中加入一项战列巡航舰升级。
- `Tychus Findlay`：在你施放两个酒馆法术后，随机获取一张战列巡航舰升级；金色版获取 2 张。

核心状态：

| 状态 | 说明 |
| --- | --- |
| `BattlecruiserInstanceId` | 当前战巡实例；死亡后是否还允许刷新升级，以官方表现为准，默认仍可向“你的战巡”施放但无目标时不应落到最左随从。 |
| `BattlecruiserUpgradeLevelByFamily` | `a/b/c/d/e/f/h/i/j` 各升级族的当前等级，用于从 `pta` 递进到 `pta2` 等。 |
| `FreeBattlecruiserUpgradeRemainingThisTurn` | Advanced Construction 的每回合免费升级次数。 |
| `YamatoTriggers` | Missile Pod 使 Yamato Cannon 触发两次。 |

实现步骤：

1. 对局开始时，如果英雄技能是 `BG31_HERO_801p`，向玩家战队加入 `BG31_HERO_801pt` 2/2 机械战巡。
2. 酒馆刷新后，向酒馆加入一个随机可用 `Battlecruiser Upgrade`，并标记为只可购买/施放给战巡。
3. `TavernSpellEngine` 新增官方升级分发：
   - `Hyperflight Rotors`：给战巡攻击。
   - `Smart Servos`：给战巡生命。
   - `Yamato Cannon`：给战巡开战前炮击最高生命敌方随从。
   - `Advanced Ballistics`：给战巡 `Rally`，触发时给其他友方攻击。
   - `Caduceus Reactor`：给战巡亡语，给最左随从属性。
   - `Advanced Construction`：本回合/每回合首个战巡升级免费。
   - `Fortified Bunker`：回合结束获得随机 Magnetic 机械。
   - `Missile Pod`：Yamato Cannon 触发两次。
   - `Ultra-Capacitor`：战巡获得 Reborn，复生保留附魔和满血。
4. `Tychus Findlay` 从当前 `BATTLECRUISER_UPGRADE` 代理改为从官方升级池获取；金色版一次给 2 张。
5. 移除“无战巡就 buff 最左随从”的 proxy 语义。官方升级要求目标是战巡；没有战巡时应不可施放或无效提示。

测试：

- 开局生成 2/2 战巡，刷新后酒馆新增升级。
- 每个升级族至少一条 focused test。
- Yamato 开战前触发顺序位于已有英雄/饰品/任务/Timewarped 大顺序之后、immediate attacks 之前。
- Tychus 普通/金色计数正确，施放两个酒馆法术后给 1/2 张官方升级。
- Advanced Construction 免费次数跨回合重置。

### P2：Kerrigan / Zerg Morph

官方文本：

- `Spawning Pool`：解锁 2 阶异虫；每回合费用减少 1；被动：开局拥有 2/2 幼虫。
- `Evolution Chamber`：解锁 3 阶异虫；每回合费用减少 1；被动：异虫可以变异为 2 阶异虫。
- `Ultralisk Cavern`：你的异虫可以变异为 3 阶异虫。
- `Broken Horn`：出售后发现一个异虫随从并设为 6/6，不会变异；金色版发现两个。

异虫池：

| 阶段 | 卡牌 |
| --- | --- |
| 幼虫 | `BG31_HERO_811t` Larva |
| 2 阶异虫 | `BG31_HERO_811t2` Zergling、`t3` Roach、`t4` Hydralisk、`t5` Baneling |
| 3 阶异虫 | `BG31_HERO_811t6` Mutalisk、`t7` Lurker、`t8` Viper、`t9` Infestor、`t10` Ultralisk |

核心状态：

| 状态 | 说明 |
| --- | --- |
| `ZergUnlockedTier` | 当前最高可变异异虫等级，初始 2，使用技能后升到 3。 |
| `KerriganPowerCost` | 初始 6；每个回合减少 1，最低 0。进入 `p2` 后按官方文本从 8 开始减少。 |
| `ZergMorphQueue` | 回合开始时需要选择变异的异虫实例队列。 |
| `NoMorph` 标签 | Broken Horn 发现出的 6/6 异虫必须保留 `does_not_morph`，不进入变异队列。 |

实现步骤：

1. 对局开始时生成 `BG31_HERO_811t` 2/2 幼虫。
2. 回合开始时，对所有可变异异虫排队选择：
   - 保留原实例的永久属性、附魔、金色状态和位置。
   - 替换为所选异虫的文本、关键字、触发 payload。
   - `does_not_morph` 的 Broken Horn 结果跳过。
3. 英雄技能费用每回合递减；使用 `BG31_HERO_811p` 后切换到 `BG31_HERO_811p2`，使用 `p2` 后切换到 `p3`。
4. 实现异虫触发：
   - Zergling：开战召唤复制。
   - Roach：回合结束按酒馆等级加生命。
   - Hydralisk：`Rally` 后永久加攻击。
   - Baneling：亡语对随机敌方随从造成等同攻击的伤害。
   - Mutalisk：友方随从击杀敌方后永久加攻击。
   - Lurker：潜行，复仇 1 永久加身材。
   - Viper：攻击时 Venomous 且 Immune。
   - Infestor：每当打出一张牌，给己方随从永久身材。
   - Ultralisk：攻击溅射；开战翻倍/金色三倍。
5. Broken Horn 从 proxy 发现改为从当前已解锁异虫池发现；结果固定为 6/6，带 `does_not_morph`，金色版队列两个发现或一个发现给两个结果，以 UI 能力为准。

测试：

- 开局幼虫存在，回合开始出现变异选择。
- 使用技能后解锁等级和费用变化正确。
- 变异保留属性、附魔、位置和金色状态。
- Broken Horn 结果 6/6 且永不变异。
- 每个异虫至少一条触发测试；涉及开战的触发遵守现有战斗开始大顺序。

### P3：Artanis / Protoss Reward

官方文本：

- `Warp Gate`：对战开始时，从 2 个星灵随从中选择 1 个；在你购买 14 张牌后获得它。
- `Probius`：Magnetic；吸附到机械后使目标机械变为金色。

星灵奖励池：

| 卡牌 | 机制 |
| --- | --- |
| `BG31_HERO_802pt` Colossus | `Rally` 对目标相邻随从造成伤害，随花费铸币改进。 |
| `BG31_HERO_802pt1` Carrier | `Avenge (4)` 召唤 Interceptor，然后永久改进。 |
| `BG31_HERO_802pt4` Immortal | 开战获得相邻随从属性。 |
| `BG31_HERO_802pt5` Void Ray | Divine Shield；友方攻击时给攻击者和自身永久攻击。 |
| `BG31_HERO_802pt7` Mothership | `Avenge (4)` 获取随机星灵随从。 |

核心状态：

| 状态 | 说明 |
| --- | --- |
| `SelectedProtossRewardCardId` | 开局二选一后锁定的奖励。 |
| `CardsBoughtUntilProtossReward` | 初始 14；每买一张牌减少 1。 |
| `ProtossRewardClaimed` | 防止重复获得。 |
| `ProtossSpendGoldCounter` | 支撑 Colossus 随花费铸币改进。 |

实现步骤：

1. 对局开始时，从星灵奖励池随机展示 2 个 `DiscoverState` 选项，选择后记录 `SelectedProtossRewardCardId`，不立刻给牌。
2. 购买任意牌后递减 `CardsBoughtUntilProtossReward`；归零时将所选星灵加入手牌或战队，按官方表现确认默认落点。若手牌满，排队奖励或延迟到有空间，不能静默丢失。
3. 实现星灵触发：
   - Colossus：`Rally` 溅射，且根据本局/持有期间花费铸币提高伤害。
   - Carrier：复仇召唤 Interceptor 并永久改进。
   - Immortal：开战获得相邻属性。
   - Void Ray：友方攻击触发永久加攻击。
   - Mothership：复仇获取随机星灵。
4. `Probius` 保留现有磁力变金实现，但测试要补金色 Probius、目标不是机械、目标已经金色、磁力后 payload 保留。

测试：

- 开局只出现 2 个星灵选项，选择后不立即给牌。
- 买 14 张牌后获得所选星灵；手牌满时有确定的延迟/队列行为。
- Probius 磁力变金不破坏目标已有附魔。
- 每个星灵奖励至少一条行为测试。

### P4：Professor Putricide / Custom Undead

官方文本：

- `Build-An-Undead`：3 费，制造一个自定义亡灵，还剩 3 次创造。
- `Festergut`：亡语召唤并获取一个随机亡灵造物；金色版召唤并获取 2 个。

当前可先实现的确定部分：

1. 英雄技能消耗 3 铸币，每局最多 3 次。
2. 生成结果必须是亡灵，使用 `BG25_HERO_100pt` / Putricide's Creation 作为结果实体。
3. `Festergut` 和 `Timewarped Festergut` 不应继续生成普通随机亡灵；应调用同一个 `UndeadCreationFactory`。
4. 金色 `Festergut` 生成数量翻倍。

待补官方组件表：

| 缺口 | 处理 |
| --- | --- |
| 组件列表 | 从营地“普崔塞德”分类或官方客户端截图补表。 |
| 组件费用/选项数量 | 未确认前不要硬标 `Implemented`。 |
| 组件可组合规则 | 需要确认是否有互斥组件、关键字、亡语 payload、身材模块。 |

实现步骤：

1. 新增 `UndeadCreationDefinition` 和 `UndeadCreationFactory`：
   - 结果卡牌 ID 固定 `BG25_HERO_100pt`。
   - 组合输出包含身材、关键词、亡语 payload、文本、来源标签。
2. 英雄技能使用时打开自定义亡灵选择流程：
   - 如果组件表未完全确认，只允许 debug/proxy 模式，不能转 `Implemented`。
   - 组件表确认后，按官方 UI 选择数量和费用结算。
3. `Festergut` 亡语改为调用 `UndeadCreationFactory.CreateRandomOfficialCreation`：
   - 普通：召唤 1 个并获取 1 个。
   - 金色：召唤 2 个并获取 2 个。
   - 战队满/手牌满时按现有 summon/hand limit 规则处理，并写消息。
4. 将 `Putricide Sticker` 后续也接到同一 factory，避免饰品和英雄各自造一套代理。

测试：

- 英雄技能费用、次数、金币不足、次数用尽。
- 创建结果是亡灵并保留组件文本/标签。
- Festergut 普通/金色数量正确。
- 战队满和手牌满不崩溃且不丢状态。
- `Timewarped Festergut` 与本体 Festergut 复用同一 factory。

## 注册表转正标准

四个英雄从 `FrameworkFirst` 转 `Implemented` 的最低标准：

| 英雄 | 转正条件 |
| --- | --- |
| Jim Raynor | 战巡开局、刷新升级、Tychus、官方升级族、至少 Yamato/Deathrattle/Reborn/免费升级等跨阶段效果有测试。 |
| Kerrigan | 幼虫、解锁、变异、Broken Horn、2 阶/3 阶异虫核心触发都有测试。 |
| Artanis | 开局二选一、买 14 张牌奖励、Probius、5 个星灵奖励核心触发都有测试。 |
| Putricide | 官方组件表确认并落地；英雄技能和 Festergut 都走同一自定义亡灵 factory。 |

如果 Putricide 组件表仍未确认，只能保持 `FrameworkFirst`，即使 Festergut 已从随机亡灵 proxy 改为更好的 factory 也不能转正。

## 建议下一步

1. 先做 P0 数据导入和 focused tests，不改行为。
2. 按 `Jim Raynor -> Kerrigan -> Artanis` 完成 StarCraft 三连，因为官方子卡 ID 已完整可查。
3. Putricide 在拿到组件表后再转正；未拿到前只做 factory 框架和 Festergut 迁移，不硬标完成。
4. 完成每个英雄后同步：
   - `HeroEffectImplementationRegistry.cs`
   - `Docs/HeroEffectImplementationGaps.md`
   - `Docs/HeroEffectRemainingCompletionOrder.md`
   - `Docs/HeroPowerBuddyEffectsImplementationOrder.md`

## 2026-07-04 实施结果

- Jim Raynor 已完成主路径：开局 2/2 Battlecruiser，刷新和 Tychus 生成官方 `BG31_HERO_801pt*` 升级，升级施放不再回退到最左随从，并接入 Yamato、Rally、Deathrattle、免费升级、Magnetic 奖励和 Reborn 等核心钩子。
- Kerrigan 已完成主路径：开局 Larva，英雄技能费用递减和阶段解锁，回合开始对可变异 Zerg 排队 Discover，选择后保留实例/身材/附魔并替换为真实 Zerg；Broken Horn 改为发现真实 6/6 非变异 Zerg。
- Artanis 已完成主路径：开局二选一记录所选 Protoss，购买 14 张牌后获得该奖励；Colossus、Carrier、Immortal、Void Ray、Mothership 的核心战斗触发已接入，Probius 磁力变金保持可用。
- Professor Putricide 已按用户确认语义完成：Build-An-Undead 消耗 3 金、每局 3 次，依次进行两次三选一组件 Discover，最终生成 `BG25_HERO_100pt`；两个组件的身材、关键词和效果标签叠加，第二次 Discover 会过滤与第一次重复的关键词组件。Festergut/烂肠也复用同一 Putricide's Creation factory 随机组合组件。
- 当前注册表边界：`Implemented=113 / FrameworkFirst=1 / Planned=0 / Deferred=0`。剩余 `FrameworkFirst` 仅为 Mr. Bigglesworth（用户决策删除/过滤）。

## 2026-07-04 Putricide Completion Update

User-confirmed Putricide semantics are now the project source of truth for implementation:

- Build-An-Undead runs two sequential 3-option component Discovers.
- The final `BG25_HERO_100pt` stacks both selected components' stats, keywords, text, and stable tags.
- Duplicate keyword components are filtered out of the second Discover.
- Festergut and Timewarped/Festergut-style random Undead Creation paths use the same two-component factory.
- Putricide is now `Implemented`; Bigglesworth remains the only `FrameworkFirst` entry by user decision.
