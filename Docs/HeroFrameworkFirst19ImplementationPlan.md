# 19 个 FrameworkFirst 英雄收口实现计划

Date: 2026-07-04

## 目标

本文件把当前剩余 `FrameworkFirst` 中适合下一批收口的 19 个英雄单独列出，给出实现顺序、公共底座、逐英雄改动点和测试要求。

当前注册表基线：

| 状态 | 数量 |
| --- | ---: |
| Implemented | 109 |
| FrameworkFirst | 5 |
| Planned | 0 |
| Deferred | 0 |

本文件不包含以下 5 个剩余 `FrameworkFirst`：

| 英雄 | 原因 |
| --- | --- |
| Mr. Bigglesworth | 按用户决策从本批删除，普通英雄技能候选仍过滤；真实大厅淘汰/最低血量以后另开。 |
| Professor Putricide | Custom Undead 制作是独立大系统，暂不并入本批。 |
| Jim Raynor | Terran/Battlecruiser 是独立 StarCraft 子系统。 |
| Kerrigan, Queen of Blades | Zerg morphing tiers 是独立 StarCraft 子系统。 |
| Artanis | Protoss delayed reward 是独立 StarCraft 子系统。 |

Deathwing 已按 2026-07-04 决策转为 `Implemented`：对手永久攻击写回不再作为单人训练器完成标准。

## 外部资料与证据

官方 Hearthstone 卡库可访问的证据：

- `Prestidigitation`：官方页面文本为 `Choose a Secret. Put it into the battlefield.`  
  Source: https://hearthstone.blizzard.com/en-us/cards/58022-prestidigitation
- `Street Magician`：官方页面文本为 `'Prestidigitation' chooses from Better Secrets.`  
  Source: https://hearthstone.blizzard.com/en-us/cards/77839-street-magician
- 普通 Battlegrounds Secret 子卡可在官方卡库查到部分页面，例如：
  - Venomstrike Trap: https://hearthstone.blizzard.com/en-us/cards/58499
  - Snake Trap: https://hearthstone.blizzard.com/en-us/cards/58500
  - Splitting Image: https://hearthstone.blizzard.com/en-us/cards/58502
  - Autodefense Matrix: https://hearthstone.blizzard.com/en-us/cards/58505
  - Avenge: https://hearthstone.blizzard.com/en-us/cards/58507
  - Redemption: https://hearthstone.blizzard.com/en-us/cards/58509
  - Ice Block: https://hearthstone.blizzard.com/en-us/cards/58512
  - Competitive Spirit: https://hearthstone.blizzard.com/en-us/cards/70114
  - Reckoning: https://hearthstone.blizzard.com/en-us/cards/104758

补充数据源：

- HearthstoneJSON latest `cards.json` 暴露 Battlegrounds Secret 和 Better Secret 子卡。  
  Source: https://api.hearthstonejson.com/v1/latest/enUS/cards.json
- 该数据源显示 4 张 Better Secret：
  - `TB_Bacon_Secrets_01b` / Better Venomstrike Trap
  - `TB_Bacon_Secrets_07b` / Better Autodefense Matrix
  - `TB_Bacon_Secrets_10b` / Better Redemption
  - `TB_Bacon_Secrets_15b` / Better Pack Tactics

注意：4 张 Better Secret 的官网单卡 URL 目前返回 404；因此本计划将“宝宝改为 Better Secret 池”作为官方文本确认，将“Better Secret 四卡表”标为 HearthstoneJSON 补充表，需要本地 JSON 固化。

## 总体实现顺序

1. P0：公共战斗事件底座。
2. P1：亡语 payload 与战斗召唤 resolver。
3. P2：攻击/击杀/伤害归属与死亡历史。
4. P3：选择 UI 与多目标命令流收口。
5. P4：饰品选择最小系统。
6. P5：Secret 系统与阿扎扎拉克四选一。
7. P6：逐英雄转正、注册表和回归测试。

完成标准：

- 每个英雄和宝宝同批验收。
- 每个英雄至少有 focused EditMode 测试。
- 涉及选择/目标/奥秘/饰品的英雄必须有命令流测试；UI smoke test 在 Unity 锁解除后补跑。
- 注册表从 `FrameworkFirst` 转 `Implemented` 时，文档、候选策略和状态计数必须同步。

## P0 公共战斗事件底座

目标：让 CombatEngine 在真实战斗中发出可复用事件，不再让英雄效果反推战斗结果。

需要新增或统一：

- `CombatEvent.AttackStarted`
- `CombatEvent.AttackResolved`
- `CombatEvent.MinionDamaged`
- `CombatEvent.MinionKilled`
- `CombatEvent.MinionDied`
- `CombatEvent.DeathrattleQueued`
- `CombatEvent.MinionSummoned`
- `CombatEvent.TauntAttacked`

事件 payload 至少包含：

- 事件时点。
- 事件方：Player / Opponent。
- 来源随从快照。
- 目标随从快照。
- 是否由攻击造成。
- 是否由非攻击伤害造成。
- 是否由亡语、法术、Secret、英雄技能、宝宝造成。
- 死亡时随从完整快照：CardId、DefinitionId、Name、Attack、Health、MaxHealth、Golden、Keywords、Tribes、Enchantments、Owner。

优先复用现有：

- CombatEngine 当前攻击循环。
- 已有 immediate attack queue。
- 已有 combat summon helper。
- 已有 post-combat reward 写回。
- 已有 `CombatReward` 模型。

## P1 亡语 payload 与召唤 resolver

目标：把当前多个 Tavern death proxy 迁移到真实战斗死亡触发。

需要统一：

- 亡语触发时机。
- 亡语 payload 注册表。
- 亡语复制。
- 亡语召唤。
- 亡语给手牌/发现/永久 buff。
- 战斗中召唤后触发 hero/buddy/trinket/quest 监听。

最小数据结构建议：

```csharp
public sealed class CombatDeathrattlePayload
{
    public string SourceCardId { get; init; }
    public BoardSide Owner { get; init; }
    public int RepeatCount { get; init; } = 1;
    public IReadOnlyList<CombatReward> Rewards { get; init; }
}
```

## P2 死亡历史、击杀归属和攻击计数

目标：服务 Rafaam、Rokara、Aranna、Illidan、Sylvanas、Ini、Jaraxxus 等。

需要统一：

- 本场战斗友方攻击次数。
- 本场战斗友方造成击杀次数。
- 本场战斗非攻击击杀归属。
- 本场战斗死亡顺序。
- 上一场战斗死亡历史快照。
- 战斗造成伤害累计。

单人训练器边界：

- 只要求玩家侧训练体验和当前模拟对手一致。
- 不要求真实 8 人大厅完整调度。
- Bigglesworth 不在本批。

## P3 选择 UI 与多目标命令流

目标：收口 Shudderwock、Vol'jin、Secret、Trinket 等需要选择/目标的英雄。

需要统一：

- `PendingChoice` 支持类型：HeroPowerOption、SecretOption、TrinketOption、TargetOption。
- 二目标确认：例如 Vol'jin、Shudderwock 未来官方 Battlecry 二目标。
- 取消/重选规则。
- 选择完成后的状态持久化。
- 发现队列与 PendingChoice 不互相覆盖。

## P4 Trinket 选择最小系统

目标：先让 Marin / Buttons 可以从本地合法饰品池选择，而不是完整重写饰品系统。

最小范围：

- 小饰品槽 / 大饰品槽状态。
- 可选饰品候选过滤。
- 排除 `Blocked` / `DebugOnly`。
- `ProxySafe` 是否可进入由文档策略控制。
- 选择结果写入 `AdvancedMechanicState` 或专用 Trinket state。

不在本批：

- 复制所有饰品复杂效果。
- Duos/shared-resource 饰品。
- 对当前饰品目录做大清理。

## P5 Secret 系统与阿扎扎拉克

### 官方/数据结论

官方确认：

- 阿扎扎拉克英雄技能 `Prestidigitation`：选择一个 Secret，并将其置入战场。
- `Street Magician`：让 `Prestidigitation` 从 Better Secrets 中选择。

本批项目规则：

- 英雄技能打开 Secret 选择。
- 普通阿扎扎拉克：从普通 Battlegrounds Secret 池中随机展示 4 个候选，用户 4 选 1。
- 有普通 `Street Magician`：从 Better Secret 四卡表中展示 4 个候选，用户 4 选 1。
- 有金色 `Street Magician`：按官方文本 `twice`，连续选择/挂载两次 Better Secret；若第一个选择后池中可选不足，少显示或允许重复需要测试决定。建议第一版允许两个选择队列连续弹出，但同一 Secret 不重复。

### 普通 Secret 初始池

普通池从本地/官方 Battlegrounds Secret 子卡开始：

| CardId | 名称 | 触发 |
| --- | --- | --- |
| `TB_Bacon_Secrets_01` | Venomstrike Trap | 友方随从被攻击时，召唤 2/3 Poisonous Cobra。 |
| `TB_Bacon_Secrets_02` | Snake Trap | 友方随从被攻击时，召唤三个 1/1 Snake。 |
| `TB_Bacon_Secrets_04` | Splitting Image | 友方随从被攻击时，召唤其复制。 |
| `TB_Bacon_Secrets_05` | Effigy | 友方随从死亡时，召唤同 Cost 随机随从。 |
| `TB_Bacon_Secrets_07` | Autodefense Matrix | 友方随从被攻击时，给其 Divine Shield。 |
| `TB_Bacon_Secrets_08` | Avenge | 友方随从死亡时，随机友方 +3/+2。 |
| `TB_Bacon_Secrets_10` | Redemption | 友方随从死亡时，以 1 Health 复活。 |
| `TB_Bacon_Secrets_11` | Hand of Salvation | 每回合第二个友方随从死亡时复活。 |
| `TB_Bacon_Secrets_12` | Ice Block | 英雄受到致命伤害时防止并免疫。 |
| `TB_Bacon_Secrets_13` | Competitive Spirit | 回合开始给全体友方 +1/+1。 |
| `TB_Bacon_Secrets_14` | Reckoning | 敌方随从造成 3 点或更多伤害后消灭它。 |
| `TB_Bacon_Secrets_15` | Pack Tactics | 友方随从被攻击时，召唤 3/3 复制。 |

### Better Secret 四卡表

Better Secret 池固定为 4 张，因此界面展示 4 个候选：

| CardId | 名称 | 触发 |
| --- | --- | --- |
| `TB_Bacon_Secrets_01b` | Better Venomstrike Trap | 友方随从被攻击时，召唤 2/3 Poisonous Cobra 并给它 Reborn。 |
| `TB_Bacon_Secrets_07b` | Better Autodefense Matrix | 友方随从被攻击时，给 Divine Shield；本场战斗需要 2 次命中才会破。 |
| `TB_Bacon_Secrets_10b` | Better Redemption | 友方随从死亡时，以 full Health and enchantments 复活。 |
| `TB_Bacon_Secrets_15b` | Better Pack Tactics | 友方随从被攻击时，召唤其复制。 |

### Secret 状态模型

建议新增：

```csharp
public sealed class SecretState
{
    public string SecretCardId { get; init; }
    public string Source { get; init; }
    public BoardSide Owner { get; init; }
    public bool Better { get; init; }
    public int CreatedRound { get; init; }
    public bool Triggered { get; set; }
}
```

挂载规则：

- 同名 Secret 默认不重复挂载。
- 如果当前挂载区已有同名 Secret，选择池过滤它。
- Secret 触发后移除。
- 招募阶段 Secret 只显示，不触发；战斗阶段按触发条件触发。

### Secret 第一版触发优先级

1. When attacked：Autodefense Matrix / Better Autodefense Matrix 先于伤害结算。
2. When attacked summon：Venomstrike / Snake / Splitting Image / Pack Tactics 在攻击开始后、伤害前或伤害后需要项目统一。建议第一版在攻击目标锁定后、伤害前触发。
3. When friendly minion dies：Avenge / Redemption / Hand of Salvation / Better Redemption 在 `MinionDied` 事件触发。
4. When enemy deals damage：Reckoning 在伤害结算后触发。
5. Hero fatal damage：Ice Block 在战斗结算英雄伤害前触发。
6. Turn start：Competitive Spirit 在招募回合开始触发并移除。

## 19 个英雄逐项计划

### 1. Shudderwock / Muckslinger

当前：Muckslinger Battlecry reward 已实现；Snicker-snack 能复放已实现 Battlecry。

缺口：

- 更广 Battlecry resolver。
- 二目标 UI。
- Battlecry target validation。

实现：

- 复用 P3 二目标命令流。
- Battlecry resolver 只接已登记可重放 Battlecry，不做文本解析。
- 未实现 Battlecry 给清晰失败日志，不吞操作。

测试：

- 单目标 Battlecry 复放。
- 二目标 Battlecry 复放。
- 无合法目标时拒绝。
- Muckslinger reward 不回退。

### 2. Vol'jin / Master Gadrin

当前：Spirit Swap 已支持两个显式目标。

缺口：

- Master Gadrin 开战左邻 hook。

实现：

- 在 HeroEffectEngine 战斗状态准备阶段记录 Gadrin 所在位置。
- CombatEngine 开战事件读取左侧邻位并执行官方 buff。

测试：

- 有左邻时触发。
- 无左邻时不触发。
- 金色/多个 Gadrin 行为。

### 3. Al'Akir / Spirit of Air

当前：开战最左随从 Windfury/Divine Shield/Taunt 已实现；Spirit of Air 仍走 Tavern death proxy。

缺口：

- 宝宝真实战斗亡语。

实现：

- 给 `TB_BaconShop_HERO_76_Buddy` 注册 Deathrattle payload。
- 真实战斗死亡后给随机友方 Windfury/Divine Shield/Taunt。

测试：

- 战斗死亡触发。
- 非战斗出售不触发该战斗亡语。
- 随机目标不选已死亡对象。

### 4. Illidan Stormrage / Eclipsion Illidari

当前：边位 +2/+1、前置立即攻击、一次攻击免疫已实现。

缺口：

- 通用友方攻击计数。
- 更完整 hero trigger ordering 回归。

实现：

- 把 tagged immediate attacks 统一记入 `AttackStarted/AttackResolved`。
- 让其他英雄/宝宝监听友方攻击时不用关心攻击来源。

测试：

- 立即攻击计入攻击次数。
- 免疫只消费一次。
- 与战斗开始其他效果排序不变。

### 5. N'Zoth / Baby N'Zoth

当前：开局 Fish、Baby N'Zoth 金色战吼已实现。

缺口：

- Fish 收集真实战斗 Deathrattle。
- Deathrattle 转移/复制。

实现：

- Fish 监听友方 Deathrattle minion died。
- 记录 Deathrattle payload 到 Fish。
- Baby N'Zoth 对 Fish 追加/金化相关逻辑走同一 payload。

测试：

- Fish 获得一个亡语。
- 多亡语顺序。
- Fish 死亡后执行收集的 payload。

### 6. Teron Gorefiend / Shadowy Construct

当前：目标标记和开战 destroy/resummon proxy 已实现。

缺口：

- 精确死亡触发时机。

实现：

- 开战销毁目标时走真实 `MinionDied` 和 Deathrattle queue。
- 复活在死亡/亡语处理后进入召唤 resolver。

测试：

- 被标记随从死亡事件可被其他效果看到。
- 亡语触发后复活。
- 复活体保留预期属性。

### 7. Arch-Villain Rafaam / Loyal Henchman

当前：直接攻击/反击击杀归属和第一/第二死亡敌方复制已实现。

缺口：

- 非攻击击杀归属。
- 完整死亡历史。

实现：

- `MinionKilled` 记录 killer source。
- Spell/Deathrattle/Secret 造成死亡时也写 source。
- Rafaam 读取本场敌方死亡历史第一项。

测试：

- 攻击击杀。
- 亡语击杀。
- Secret/非攻击击杀。
- Loyal Henchman 第二击杀。

### 8. Rokara / Icesnarl the Mighty

当前：直接攻击/反击友方击杀奖励已实现。

缺口：

- 非攻击击杀来源。

实现：

- 复用 Rafaam 的 `MinionKilled` source。
- 友方来源造成击杀时给 Rokara/Icesnarl 成长。

测试：

- 攻击击杀成长。
- 亡语击杀成长。
- 敌方自毁不误触。

### 9. Sylvanas Windrunner / Nathanos Blightcaller

当前：Nathanos targeted sell-and-split Battlecry 已实现。

缺口：

- 上一场战斗死亡历史。
- Reclaimed Souls Discover。

实现：

- 战斗结束保存 friendly death history。
- Hero Power 启动死亡随从 Discover。
- 选择后获得对应随从/效果。

测试：

- 上场死亡池进入 Discover。
- 无死亡历史时禁用或空提示。
- Nathanos 现有 Battlecry 不受影响。

### 10. Sneed / Piloted Whirl-O-Tron

当前：Starting Shredder token 已实现。

缺口：

- Hero Power 给手牌/目标随从挂 summon Deathrattle。
- Whirl-O-Tron 复制 Deathrattle payload。

实现：

- Sneed Hero Power 给目标添加 Deathrattle payload。
- Whirl-O-Tron 死亡时复制友方 Deathrattle payload。
- 召唤结果走 combat summon resolver。

测试：

- 被赋予 Deathrattle 的随从死亡后召唤。
- Whirl-O-Tron 复制并触发。
- 金色翻倍规则。

### 11. The Jailer / Mawsworn Soulkeeper

当前：Runic Empowerment 已从友方死亡计数实现；宝宝仍 Tavern death proxy。

缺口：

- 宝宝真实战斗亡语。

实现：

- 注册 `Mawsworn Soulkeeper` Deathrattle payload。
- 战斗死亡后按当前数值给随机/指定友方 buff。

测试：

- 真实战斗死亡触发。
- 死亡计数提升后数值正确。
- 出售不触发战斗亡语。

### 12. Greybough / Wandering Treant

当前：Sprout It Out 对 hero-effect 和 CombatEngine 内部召唤生效。

缺口：

- friendly Taunt-attacked hook。
- Wandering Treant 永久全队 buff。

实现：

- `TauntAttacked` 事件。
- Wandering Treant 监听友方 Taunt 被攻击，给全队永久 buff。

测试：

- 嘲讽被攻击触发。
- 非嘲讽被攻击不触发。
- 召唤随从仍吃 Greybough buff。

### 13. Ini Stormcoil / Sub Scrubber

当前：Sub Scrubber Mech-play growth 已实现。

缺口：

- MechGyver 友方机械战斗死亡计数和机械奖励。

实现：

- 监听 friendly Mech died。
- 达到阈值给机械奖励或刷新/入手。

测试：

- 机械死亡计数。
- 非机械死亡不计数。
- 多次死亡奖励节奏。

### 14. Ozumat / Tamuzo

当前：Tentacle summon、sell/combat-death growth、Tamuzo 对部分召唤翻倍已实现。

缺口：

- 所有未来召唤源统一 resolver。

实现：

- 把 hero-effect、Deathrattle、Secret、Trinket、Quest、Timewarped 的战斗召唤入口统一到 `CombatSummonResolver`。
- Tamuzo 只监听 resolver，不再各路径特判。

测试：

- Hero summon 翻倍。
- Deathrattle summon 翻倍。
- Secret summon 翻倍。
- 不应翻倍的招募阶段召唤不触发。

### 15. Aranna Starseeker / Sklibb

当前：Sklibb refresh extra higher-tier minion 已实现。

缺口：

- Aranna 友方攻击次数解锁。

实现：

- 使用 P2 攻击计数。
- 满足次数后永久切换 Hero Power/状态。

测试：

- 普通攻击计数。
- Illidan tagged immediate attack 计数。
- 解锁后刷新行为正确。

### 16. Lord Jaraxxus / Kil'rek

当前：Kil'rek 用 Tavern death proxy 给 Demon reward。

缺口：

- Bloodfury 战斗伤害累计。
- Portal rewards。
- Kil'rek 真实死亡触发。

实现：

- `MinionDamaged`/`DamageDealt` 事件累计友方 Demon 或指定来源伤害。
- 达到阈值发 portal reward。
- Kil'rek 迁移到 Deathrattle payload。

测试：

- 战斗伤害累计。
- 非战斗 buff 不计伤害。
- Kil'rek 真实死亡奖励。

### 17. Marin the Manager / Fantastic Bellhop

当前：Fantastic Bellhop end-turn helpful card 已实现。

缺口：

- Marin Trinket choice system。

实现：

- 回合/时点触发小饰品选择。
- 候选来自合法小饰品池。
- 写入 Trinket slot。
- 不允许 `Blocked/DebugOnly`。

测试：

- 选择后进入饰品槽。
- 候选过滤。
- Fantastic Bellhop 不受影响。

### 18. Buttons / Zippers

当前：Zippers helpful-card Deathrattle 用 Tavern proxy。

缺口：

- Greater Trinket choice。
- Zippers 真实亡语奖励。

实现：

- Buttons 在对应时点打开 Greater Trinket choice。
- 候选来自合法大饰品池。
- Zippers 迁移到真实 Deathrattle payload。

测试：

- 大饰品选择。
- 候选过滤。
- Zippers 战斗死亡奖励。

### 19. The Great Akazamzarak / Street Magician

当前：Street Magician 生成 `BETTER_SECRET_PROXY`；没有真实 Secret 系统。

缺口：

- Secret 选择 UI。
- Secret 挂载区。
- Secret 触发/移除。
- Better Secret 四选一。

实现：

- 新增 `SecretCatalog`：普通 Secret 池 + Better Secret 四卡表。
- `Prestidigitation` 打开 4 个 Secret 选择。
- 有 `Street Magician` 时使用 Better Secret 四卡表。
- 金色 `Street Magician` 连续排队两次 Better Secret 选择。
- 打出/选择 Secret 后挂载到 `SecretState`。
- CombatEngine/MatchService 在对应事件触发 Secret 并移除。
- 删除 `BETTER_SECRET_PROXY` 运行时代理路径，或保留为 DebugOnly 兼容入口但不再由 Street Magician 生成。

测试：

- 无宝宝：普通 Secret 四选一并挂载。
- 有宝宝：Better Secret 四选一并挂载。
- 金色宝宝：连续两次 Better Secret 选择。
- Autodefense Matrix 攻击前触发。
- Redemption 死亡后触发。
- Ice Block 英雄致命伤害触发。
- 已挂同名 Secret 不重复进入候选。

## 建议批次拆分

### Batch A：战斗事件最小闭环

英雄：

- Illidan
- Aranna
- Rafaam
- Rokara

交付：

- AttackStarted / AttackResolved
- MinionKilled source
- focused tests

### Batch B：亡语 payload 第一批

英雄：

- Al'Akir
- The Jailer
- Sneed
- Teron

交付：

- Deathrattle payload registry
- Combat summon resolver first version
- focused tests

### Batch C：死亡历史和复杂亡语

英雄：

- N'Zoth
- Sylvanas
- Ini Stormcoil
- Lord Jaraxxus

交付：

- death history
- damage tracking
- Fish / Reclaimed Souls / MechGyver / Kil'rek tests

### Batch D：召唤 resolver 和触发点补齐

英雄：

- Ozumat
- Greybough
- Vol'jin
- Shudderwock

交付：

- CombatSummonResolver all-source routing
- TauntAttacked
- start-of-combat neighbor hook
- Battlecry resolver expansion

### Batch E：Trinket 选择

英雄：

- Marin
- Buttons

交付：

- Trinket choice state
- small/greater candidate filtering
- Zippers Deathrattle migration

### Batch F：Secret 系统

英雄：

- The Great Akazamzarak

交付：

- SecretCatalog
- SecretState
- 4-option choice UI/command
- Better Secret four-card table
- combat trigger hooks

## 验证矩阵

| 范围 | 测试 |
| --- | --- |
| Registry | `Implemented=113 / FrameworkFirst=1 / Planned=0 / Deferred=0`，其中剩余 1 是 Bigglesworth。Putricide 已按用户确认的两段组件 Discover 语义转为 `Implemented`。 |
| Candidate policy | 19 个完成后不再带 `implementation_status:framework_first`；Bigglesworth 仍过滤。 |
| Combat events | 攻击、击杀、死亡、亡语、召唤、嘲讽被攻击、伤害累计均有 focused tests。 |
| Secret | 普通 Secret、Better Secret、金色宝宝双选择、触发移除。 |
| Trinket | 小/大饰品候选过滤、选择入槽、Blocked/DebugOnly 排除。 |
| Unity | Unity 锁解除后跑 focused EditMode 和必要 UI smoke。 |

## 风险与边界

- Bigglesworth 不在本批，不因为 DeathHistory 做好了就自动转正。
- Secret 的 Better Secret 四卡表来自 HearthstoneJSON；官网只确认 Better Secret 机制文字，不暴露四张子卡页面。
- Trinket 选择只做 Marin/Buttons 所需最小闭环，不清理整个饰品目录成熟度标签。
- Shudderwock 不做文本级 Battlecry 解释器，只复用已登记 Battlecry resolver。
- 所有战斗事件必须保持既有战斗开始大顺序：HeroEffectEngine 准备英雄战斗状态，然后 Trinket、Quest、Timewarped，最后 CombatEngine 内部在 tagged immediate attacks 前结算英雄战斗事件。

## 2026-07-04 Implementation Sync

Status after runtime sync:

| Status | Count |
| --- | ---: |
| Implemented | 109 |
| FrameworkFirst | 5 |
| Planned | 0 |
| Deferred | 0 |

The 19-hero batch is now wired in runtime and registry. Remaining `FrameworkFirst` entries are intentionally limited to:

- Mr. Bigglesworth
Professor Putricide has since been completed: Build-An-Undead runs two sequential 3-option component Discovers, stacks the selected component stats/effects, filters duplicate keyword components, and Festergut shares the same factory.
- Jim Raynor（2026-07-04 已转 Implemented）
- Kerrigan, Queen of Blades（2026-07-04 已转 Implemented）
- Artanis（2026-07-04 已转 Implemented）

Implemented validation coverage added or updated:

- Secret Discover, mounted Secret state, 0-Cost use, 4 active Secret cap, duplicate filtering, untriggered Secret persistence, and triggered Secret removal for The Great Akazamzarak / Street Magician.
- Autodefense Matrix, Better Autodefense Matrix, Redemption, Better Redemption, Ice Block, Snake Trap, Venomstrike Trap, Better Venomstrike Trap, Splitting Image, Pack Tactics, Better Pack Tactics, Effigy, Avenge, Hand of Salvation, Competitive Spirit, and Reckoning trigger coverage.
- Master Gadrin start-of-combat left-neighbor hook.
- Sneed hero-power Deathrattle summon path.
- Ini combat Mech-death Magnetic Mech reward.
- Aranna friendly-combat-attack unlock for the free first minion buy.
- Wandering Treant Taunt-attacked permanent board buff.
- Registry and candidate policy assertions for `Implemented=113 / FrameworkFirst=1`.

Unity EditMode validation still requires the existing Unity lock to be released before the focused test run can execute.
