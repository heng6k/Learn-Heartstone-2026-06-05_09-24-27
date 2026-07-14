# 指向型法术手动排查清单 / Targeted Spell Manual Audit Checklist

> 适用项目 / Project: Learn Hearthstone — Battlegrounds Tavern Trainer  
> 基准日期 / Baseline date: 2026-07-14  
> 范围 / Scope: 酒馆法术、普通/生成法术、塑造法术、暗月奖品、任务奖励与饰品生成法术。

## 1. 文档目的 / Purpose

本文档用于手动排查所有需要玩家选择目标的法术。重点不是只确认数值是否变化，还要确认完整的输入会话：开始拖拽、目标提示、合法/非法目标反馈、释放、取消、重试、动画、日志和法术消耗。

This document is a manual checklist for every spell that requires the player to select a target. The audit covers the complete input session, not only the final numerical effect: drag start, target cues, valid/invalid feedback, release, cancellation, retry, animation, logging, and spell consumption.

## 2. 当前代码判定规则 / Current Code Classification

当前 Unity 指向系统将满足以下条件的卡牌视为指向型法术：

- `CardKind` 为 `TavernSpell` 或 `Spell`；
- `Tags` 包含 `targeted_spell`；
- 放置前由 `TryValidatePlayerCardDrop` 验证目标；
- 合法目标通常通过 `TargetZone.PlayerBoard` 或 `TargetZone.TavernShop` 提交。

The Unity targeting system currently treats a card as a targeted spell when:

- `CardKind` is `TavernSpell` or `Spell`;
- `Tags` contains `targeted_spell`;
- the destination is validated by `TryValidatePlayerCardDrop` before execution;
- valid targets are normally submitted through `TargetZone.PlayerBoard` or `TargetZone.TavernShop`.

如果卡牌文字明显要求“选择一个随从”，但运行时没有 `targeted_spell`，它可能会被当成无目标法术直接使用。第 9 节专门列出此类高风险条目。

If card text clearly says “Choose/Give a minion” but the runtime card lacks `targeted_spell`, it may be played as a non-targeted spell. Section 9 lists these high-risk semantic mismatches.

## 3. 每张法术的通用排查步骤 / Universal Test Procedure

对下方每张法术至少执行一次完整流程：

1. 将法术加入手牌，保证己方战场至少有 2 个不同随从；涉及酒馆目标时保证酒馆至少有 2 张随从。
2. 按下并拖动法术，确认源卡进入 `Source` 状态并显示指向连线。
3. 依次悬停合法目标、非法目标、空槽、背景、手牌、按钮和被遮挡区域。
4. 合法目标应显示候选高亮；非法目标应显示“不可选”及对应原因。
5. 在非法目标上释放：法术不能消耗，数值不能变化，指向会话应保持或明确返回可重试状态。
6. 将指针移回合法目标：非法反馈应立即清除并恢复合法高亮。
7. 在合法目标上释放：只影响选中的目标或卡面规定的关联目标。
8. 核对金币、手牌数量、临时/永久附魔、关键词、金色状态、酒馆卡池及日志。
9. 重新生成同类法术，确认第二次使用不会继承上一次目标、高亮或连线。
10. 分别测试鼠标快速拖放、慢速拖放、拖出窗口、Esc/取消、减少动态效果模式。

For every spell below, perform at least one complete flow:

1. Put the spell in hand. Keep at least two distinct friendly minions on the board; for Tavern-target spells, keep at least two Tavern minions.
2. Press and drag the spell. Confirm the source enters the `Source` state and the targeting connector appears.
3. Hover a valid target, invalid target, empty slot, background, hand card, button, and obscured area.
4. Valid targets must show candidate highlighting. Invalid targets must show “Invalid Target” and a useful reason.
5. Release over an invalid target: the spell must not be consumed, no effect may resolve, and the session must remain retryable or return cleanly to retry state.
6. Move back to a valid target: invalid feedback must clear immediately and valid highlighting must return.
7. Release over a valid target: only the selected target and explicitly related targets may change.
8. Verify Gold, hand count, temporary/permanent enchantments, keywords, Golden state, Tavern pool state, and logs.
9. Generate another copy and confirm no target, highlight, or connector state leaks from the previous cast.
10. Repeat with fast drag, slow drag, drag outside the window, cancel/Esc, and reduced-motion mode.

## 4. 酒馆法术 / Tavern Spells

| 完成 | Card ID | 中文名 | English name | 等级 | 预期目标 / Expected target | 核心效果 / Core effect | 数据状态 |
|---|---|---|---|---:|---|---|---|
| [ ] | `104445` | 防御者的仪式 | Defender's Rites | 4 | 友方战场随从 / Friendly board minion | +6/+6并获得嘲讽 / +6/+6 and Taunt | implemented |
| [ ] | `104472` | 自然祝福 | Natural Blessing | 4 | 己方战场或酒馆随从 / Friendly board or Tavern minion | 同类型随从+3/+3 / Buff minions of the target's type | implemented |
| [ ] | `110642` | 查抄宝石 | Gem Confiscation | 4 | 友方战场随从 / Friendly board minion | 使用2张宝石并偷取相邻宝石 / Apply 2 Gems and steal adjacent Gems | implemented |
| [ ] | `120900` | 变换之潮 | Shifting Tide | 4 | 友方战场随从 / Friendly board minion | 两次+2/+2；纳迦额外重复 / Trigger +2/+2 twice; repeat for Naga | implemented |
| [ ] | `130310` | 燎原烈焰 | Conflagration | 4 | 友方战场随从 / Friendly board minion | +2/+2，按本回合元素成长 / Scaling buff from Elementals played | implemented |
| [ ] | `131153` | 背靠背 | Back to Back | 4 | 友方战场随从 / Friendly board minion | 递增的+2/+2 / Increasing targeted +2/+2 | implemented |
| [ ] | `105266` | 梦境之拥 | Dreamer's Embrace | 3 | 友方战场随从 / Friendly board minion | +3/+3；龙或鱼人改为+6/+6 / +6/+6 for Dragon or Murloc | generated implementation |
| [ ] | `130853` | 复制透镜 | Duplicating Lens | 4 | 仅己方战场随从 / Friendly board minion only | 获得目标的普通复制 / Get a plain copy | generated implementation |
| [ ] | `98914` | 点金术 | Goldenizer | 5 | 仅己方战场随从 / Friendly board minion only | 变为金色 / Make Golden | 由 `BG27_Anomaly_751` Perfected Alchemy 开局生成 |
| [ ] | `100596` + `anomaly_golden_arrow` | 点金箭 | Golden Arrow | - | 仅当前酒馆随从 / Tavern minion only | +8攻击力 / +8 Attack | 由 `BG31_Anomaly_124` 每3回合生成；与普通尖利箭矢共用Card ID |

### 酒馆法术专项边界 / Tavern-spell edge cases

- 自然祝福：目标无类型、多类型、全类型、酒馆目标和战场目标。
- 查抄宝石：最左、最右、只有一个随从、相邻随从无宝石、目标已有大量宝石。
- 变换之潮：纳迦与非纳迦，确保触发次数正确且动画不会重复残留。
- 背靠背：连续使用第1/2/3张，成长值与目标切换是否正确。
- 复制透镜：只能选择己方战场；金色目标只获得普通复制；手牌满时禁止或正确失败。
- Goldenizer：普通、金色、不可金色化目标；三连与卡池副作用。
- Golden Arrow：只能选择酒馆随从；不能选择己方战场；不得与普通 Pointy Arrow 的目标规则混用。

- Natural Blessing: typeless, multi-type, all-type, Tavern target, and board target.
- Gem Confiscation: left edge, right edge, single-minion board, neighbors without Gems, and large Gem stacks.
- Shifting Tide: Naga versus non-Naga; verify trigger count and animation cleanup.
- Back to Back: cast copies 1/2/3 consecutively and switch targets between casts.
- Duplicating Lens: targeting a Golden minion must still create a plain copy; verify full-hand failure.
- Goldenizer: normal, already-Golden, and ineligible targets; verify Triple and pool side effects.

## 5. 暗月奖品 / Darkmoon Prizes

| 完成 | Card ID | 中文参考名 | English name | 阶段 | 目标 / Target | 预期效果 / Expected effect |
|---|---|---|---|---:|---|---|
| [ ] | `BGS_Treasures_009` | 格鲁尔法则 | Gruul Rules | 2 | 友方非空战场随从 | 获得回合结束时+4/+4效果 / Gain end-of-turn +4/+4 effect |
| [ ] | `BGS_Treasures_018` | 笼中之鼠 | I'm Still Just a Rat in a Cage | 2 | 一个随从 / A minion | +2攻击力后攻击力翻倍 / +2 Attack, then double Attack |
| [ ] | `BGS_Treasures_026` | 保镖 | The Bouncer | 2 | 友方随从 / Friendly minion | 获得嘲讽并生命值翻倍 / Taunt, then double Health |
| [ ] | `BGS_Treasures_015` | 购买圣光 | Buy the Holy Light | 3 | 友方随从 / Friendly minion | +10攻击力和圣盾 / +10 Attack and Divine Shield |
| [ ] | `BGS_Treasures_034` | 回头客 | Repeat Customer | 3 | 非金色友方随从 / Friendly non-Golden minion | 回手并+6/+6 / Return to hand and +6/+6 |
| [ ] | `BGS_Treasures_016` | 提高赌注 | Raise the Stakes | 4 | 友方随从 / Friendly minion | 变金并回手 / Make Golden and return to hand |
| [ ] | `BGS_Treasures_028` | 给狗一根骨头 | Give a Dog a Bone | 4 | 友方随从 / Friendly minion | 圣盾、风怒、+15/+15 / Divine Shield, Windfury, +15/+15 |

重点检查回手类法术在手牌已满、目标已经金色、目标带临时附魔或正处于选择状态时的行为。

Pay special attention to return-to-hand spells when the hand is full, the target is already Golden, the target has temporary enchantments, or another selection session is active.

## 6. 鲜血宝石与通用生成法术 / Blood Gems and Generic Generated Spells

| 完成 | Card ID | 中文名 | English name | 合法目标 / Valid target | 额外限制 / Extra restriction |
|---|---|---|---|---|---|
| [ ] | `BLOOD_GEM` | 鲜血宝石 | Blood Gem | 友方战场随从 | 无 / None |
| [ ] | `BRISTLEBACK_BLOOD_GEM` | 刚毛鲜血宝石 | Bristleback Blood Gem | 友方战场随从 | 野猪人额外获得嘲讽 / Quilboar also gains Taunt |
| [ ] | `REBORN_BLOOD_GEM` | 复生鲜血宝石 | Reborn Blood Gem | 友方战场随从 | 野猪人额外获得复生 / Quilboar also gains Reborn |
| [ ] | `SLIMY_SHIELD` | 黏滑护盾 | Slimy Shield | 己方战场或当前酒馆随从 | +1/+1和嘲讽 / +1/+1 and Taunt |
| [ ] | `100596` | 尖利箭矢 | Pointy Arrow | 己方战场或当前酒馆随从 | +4攻击力 / +4 Attack；不含 `anomaly_golden_arrow` |
| [ ] | `MUKLA_BANANA` | 香蕉 | Tavern Dish Banana | 友方战场随从 | +1/+1 |
| [ ] | `TRINKET_JEWELRY_BOX_TAUNT_GEM` | 嘲讽宝石 | Taunting Blood Gem | 友方野猪人 / Friendly Quilboar | 非野猪人必须不可选 / Non-Quilboar must be invalid |
| [ ] | `TRINKET_JEWELRY_BOX_DIVINE_SHIELD_GEM` | 闪耀宝石 | Gleaming Blood Gem | 友方野猪人 / Friendly Quilboar | 非野猪人必须不可选 |
| [ ] | `TRINKET_JEWELRY_BOX_REBORN_GEM` | 复生宝石 | Reborn Blood Gem | 友方野猪人 / Friendly Quilboar | 非野猪人必须不可选 |

## 7. 塑造法术 / Spellcraft Spells

除特别说明外，塑造法术应在回合结束时移除临时效果，生成的临时法术牌也应按生命周期清理。

Unless explicitly permanent, Spellcraft effects must expire at the end of the turn and temporary Spellcraft cards must follow their cleanup lifecycle.

| 完成 | Card ID | 中文参考名 | English name | 目标限制 / Restriction | 效果重点 / Effect focus |
|---|---|---|---|---|---|
| [ ] | `REEF_RIFFER_SPELL` | 礁石即兴曲 | Reef Riff | 己方战场或当前酒馆随从 | 按酒馆等级给予临时属性 |
| [ ] | `SURF_N_SURF_SPELL` | 冲浪再冲浪 | Surf n' Surf | 己方战场或当前酒馆随从 | 临时获得召唤螃蟹的亡语 |
| [ ] | `DEEP_SEA_ANGLER_SPELL` | 深海垂钓 | Deep Sea Angling | 己方战场或当前酒馆随从 | 临时+2/+6和嘲讽 |
| [ ] | `DEEP_BLUE_SPELL` | 深蓝 | Deep Blue | 己方战场或当前酒馆随从 | 成长数值、回合结束移除；使用次数使用独立全局计数 |
| [ ] | `VOLCANIC_VISITOR_ATTACK_SPELL` | 火山访客：攻击 | Volcanic Visitor Attack | 友方随从 | 临时+4攻击力 |
| [ ] | `VOLCANIC_VISITOR_HEALTH_SPELL` | 火山访客：生命 | Volcanic Visitor Health | 友方随从 | 临时+4生命值 |
| [ ] | `TIMEWARPED_GLOWSCALE_SPELL` | 时空扭曲亮鳞 | Timewarped Glowscale | 友方随从 | 获得圣盾；确认是否临时 |
| [ ] | `WEARY_MAGE_SPELL` | 疲惫法师 | Weary Mage | 己方战场或当前酒馆随从 | +2/+2；纳迦临时获得复生 |
| [ ] | `THAUMATURGIST_SPELL` | 奇术师 | Thaumaturgist | 友方随从 | 可成长属性及永久化饰品交互 |
| [ ] | `TIMEWARPED_SUMMONER_SPELL` | 时空扭曲召唤师 | Timewarped Summoner | 战场或酒馆中至少有一个类型的随从 | 使用目标全部类型的联合候选池变换酒馆；保留每个槽位等级；候选按定义去重 |
| [ ] | `TRINKET_PRECIOUS_PEARL_SPELL` | 珍贵珍珠 | Precious Pearl | 己方战场或当前酒馆随从 | 临时+30/+30 |
| [ ] | `TRINKET_OPHIDIAN_STAFF_SPELL` | 蛇形法杖 | Ophidian Staff | 战场或酒馆中的野兽 | 非野兽不可选；临时+2/+2和复生 |
| [ ] | `TRINKET_VIBRANT_BUBBLE_SPELL` | 活力气泡 | Vibrant Bubble | 友方随从 | 关键词/属性效果 |
| [ ] | `TRINKET_DOUBLE_STITCH_NEEDLE_SPELL` | 双缝针 | Double Stitch Needle | 友方随从 | 属性翻倍并锁手1回合 |
| [ ] | `TRINKET_TOKEN_OF_THE_OLD_GODS_SPELL` | 上古之神信物 | Token of the Old Gods | 友方随从 | 变为高一级随机随从 |
| [ ] | `TRINKET_JAILER_STICKER_SPELL` | 典狱官贴纸 | Jailer Sticker | 友方亡灵 / Friendly Undead | 非亡灵不可选；消灭后生成亡灵 |
| [ ] | `TRINKET_DEMONBLOOD_GOURD_SPELL` | 魔血葫芦 | Demonblood Gourd | 友方随从 | 吞食随机酒馆随从；空酒馆处理 |
| [ ] | `TRINKET_SHIFTING_TIDE_SPELL` | 变换之潮 | Shifting Tide | 友方随从 | 纳迦获得更高加成 |

## 8. 任务奖励与特殊目标法术 / Quest Reward and Special-target Spells

| 完成 | Card ID | 中文名 | English name | 目标 / Target | 检查重点 / Audit focus |
|---|---|---|---|---|---|
| [ ] | `BG24_Reward_715t` | 巨型号角 | Mega Horn | 友方随从 | +5/+5和嘲讽 |
| [ ] | `BG24_Reward_715t2` | 炽燃之刃 | Blazing Blades | 友方随从 | +5/+5和风怒 |
| [ ] | `BG24_Reward_715t3` | 地堡镀层 | Bunker Plating | 友方随从 | +5/+5和圣盾 |
| [ ] | `BG24_Reward_715t4` | 死亡回溯器 | Death Rewinder | 友方随从 | +5/+5和复生 |
| [ ] | `BG33_Reward_006t` | 疾风 | Rushing Winds | 友方随从 | 风怒和圣盾 |
| [ ] | `BG27_Reward_504t` | 时间线加速器 | Timeline Accelerator | 己方战场或当前酒馆随从 | 变为高一级随从；无候选时不消耗 |
| [ ] | `BG24_Reward_718t` | 绑架麻袋 | Kidnap Sack | 己方战场或当前酒馆随从 | 将指定目标移入手牌；手牌满时不可结算 |
| [ ] | `BG24_Reward_719t` | 金色锤 | The Golden Hammer | 友方战场随从 | 临时变金，次回合恢复 |
| [ ] | `FLY_THE_FLAG_SPELL` | 高举旗帜 | Fly the Flag | 酒馆随从 / Tavern minion | 向卡池加入目标的12张复制 |

Kidnap Sack 必须由玩家明确选择目标。战场目标从战场移入手牌；酒馆目标从当前酒馆槽位移入手牌。手牌已满时不得消耗法术，也不得移动目标。

Kidnap Sack requires an explicit player-selected target. A board target moves from the warband to hand; a Tavern target moves from its current Tavern slot to hand. With a full hand, neither the spell nor the target may move.

## 9. 已确认的特殊目标规则与实现风险 / Confirmed Special Target Rules and Implementation Risks

这些条目必须优先手动排查。它们的文字或效果明显依赖目标，但数据标签未必统一包含 `targeted_spell`。

These entries require priority manual auditing. Their text or effect depends on a selected minion, but their runtime tags may not consistently contain `targeted_spell`.

| 完成 | Card ID | 中文名 | English name | 风险 / Risk |
|---|---|---|---|---|
| [ ] | `130311` | 奥术吸收 | Arcane Absorption | 仅己方战场元素；非元素和酒馆随从不可选 / Friendly board Elemental only |
| [ ] | `130312` | 艾欧娜尔的恩惠 | Eonar's Favor | 仅选择己方战场随从；效果强化当前酒馆的同类型随从 / Board target only; affects matching current Tavern minions |
| [ ] | `131218` | 深水氏族 | Deepwater Clan | 选择任意己方战场随从，不要求目标是鱼人；目标+2/+2，所有己方鱼人再+2/+2 / Any board minion; target buff plus all friendly Murlocs |
| [ ] | `122184` / `BG33_813` | 自私悬赏 | Selfish Bounty | 非指向型；自动使最左随从+6/+6；不得出现连线 / Non-targeted; automatically buffs the leftmost minion |
| [ ] | `104472` | 自然祝福 | Natural Blessing | 可选战场或当前酒馆；使用目标全部有效类型匹配两区随从；每个随从只结算一次 / Board or Tavern target; union of all target types; each minion resolves once |
| [ ] | `130853` | 复制透镜 | Duplicating Lens | 仅己方战场随从 / Friendly board minion only |
| [ ] | `TIMEWARPED_SUMMONER_SPELL` | 时空扭曲召唤师 | Timewarped Summoner | 战场或酒馆有类型随从；无类型不可选；多类型使用联合混合池 / Typed board or Tavern target; multi-type union pool |
| [ ] | `TRINKET_PRECIOUS_PEARL_SPELL` | 珍贵珍珠 | Precious Pearl | 战场和酒馆均可 / Board and Tavern targets are valid |
| [ ] | `TRINKET_OPHIDIAN_STAFF_SPELL` | 蛇形法杖 | Ophidian Staff | 战场和酒馆均可，但必须为野兽 / Board or Tavern Beast only |
| [ ] | `100596` + `anomaly_golden_arrow` | 点金箭 | Golden Arrow | 只能选择酒馆随从；与普通尖利箭矢共用Card ID，必须依据生成标签区分 / Tavern only; distinguish by generated tag |
| [ ] | `BG24_Reward_718t` | 绑架麻袋 | Kidnap Sack | 战场和酒馆均可；移动明确选择的目标，而不是自动选择第一个酒馆随从 / Board or Tavern; move the explicitly selected target |

### 自然祝福的多类型规则 / Natural Blessing Multi-type Rule

- 目标类型集合取目标的全部有效类型。
- 候选随从与目标类型集合存在任意交集时获得强化。
- 同一随从匹配多个类型仍只获得一次强化。
- 全类型目标匹配所有有类型随从，但无类型随从不获得强化。
- 酒馆强化只作用于当前展示的酒馆随从，不修改卡池模板或后续刷新。

- Use every valid type carried by the target.
- A candidate is buffed when its type set intersects the target type set.
- Multiple matching types never cause repeated buffs.
- An All-type target matches every typed minion, but not typeless minions.
- Tavern buffs affect only the currently displayed Tavern minions, not future refreshes or base pool definitions.

### 时空扭曲召唤师的多类型规则 / Timewarped Summoner Multi-type Rule

- 使用目标全部有效类型组成联合候选池。
- 每个酒馆槽位从联合池独立随机生成随从，并保留该槽位原随从的等级。
- 同一候选即使匹配多个类型，也只能在候选池中出现一次，不能增加权重。
- 全类型目标使用全部有效部族组成的联合池。
- 无类型目标必须在释放前显示不可选。

- Build one union pool from all valid target types.
- Each Tavern slot rolls independently from the union while preserving that slot's original Tier.
- A candidate matching multiple target types appears only once and receives no extra weight.
- An All-type target uses the union of every valid tribe.
- Typeless targets must be rejected before release.

### 独立全局成长计数 / Independent Global Scaling Counters

- Deep Blue 的所有副本共享 Deep Blue 自己的全局累计值。
- Back to Back 的所有副本共享 Back to Back 自己的全局累计值。
- 两种法术分别统计，互不增加对方的成长值。
- 只有成功结算才增加计数；非法释放、取消和无目标失败不得增加。

- Every Deep Blue copy shares the Deep Blue global scaling value.
- Every Back to Back copy shares the Back to Back global scaling value.
- The two counters are independent and never increase each other.
- Only successful resolution increments a counter; invalid release, cancellation, or missing-target failure does not.

### 巴琳达·斯通赫尔斯专项 / Belinda Stonehearth Special Audit

`BG35_883` 巴琳达只复制“以己方战场随从为目标”的法术，不复制以酒馆随从为目标的施法。

- 普通巴琳达：符合条件的法术总计施放2次。
- 金色巴琳达：符合条件的法术总计施放3次。
- 法术必须包含 `targeted_spell`，且实际目标区域必须为 `FriendlyBoard`。
- 同一友方目标连续使用多张法术时，每张法术分别按2次或3次完整叠加。
- 对当前酒馆随从释放相同法术时只结算原始1次。
- 额外施放不能重复消耗手牌、金币或增加“使用一张法术”的任务次数，但每次实际效果结算应正确叠加。
- 多个普通/金色巴琳达同时在场时，额外次数按各自光环相加。

`BG35_883` Belinda repeats only spells that actually target a friendly warband minion. A spell aimed at a Tavern minion is not repeated.

- Normal Belinda: 2 total resolutions.
- Golden Belinda: 3 total resolutions.
- The spell must contain `targeted_spell` and the actual target zone must be `FriendlyBoard`.
- Repeated casts on the same friendly target stack independently for every spell played.
- The same spell aimed at a current Tavern minion resolves only once.
- Extra resolutions must not consume extra cards or Gold, or add extra “cast a spell” quest actions, while their actual effects must stack correctly.
- Multiple normal/Golden Belindas add their extra-resolution auras together.

## 10. 非法目标矩阵 / Invalid-target Matrix

每一种法术类型至少验证以下矩阵：

| 测试位置 / Destination | 友方随从增益 | 部族限定法术 | 酒馆目标法术 | 回手/消灭法术 |
|---|---|---|---|---|
| 友方战场合法随从 | 应合法 | 符合部族才合法 | 通常非法 | 依牌面规则 |
| 友方战场错误部族 | 一般合法 | 必须非法 | 非法 | 依牌面规则 |
| 酒馆随从 | 通常非法 | 通常非法 | 应合法 | 仅明确允许时合法 |
| 空战场槽 | 非法 | 非法 | 非法 | 非法 |
| 空酒馆槽 | 非法 | 非法 | 非法 | 非法 |
| 手牌卡牌 | 非法 | 非法 | 非法 | 非法 |
| 对手随从 | 非法 | 非法 | 非法 | 非法 |
| 背景/按钮/面板 | 非法 | 非法 | 非法 | 非法 |
| 同一目标重复使用 | 依效果合法 | 依关键词上限 | 依效果合法 | 已离场目标不可用 |

For every spell family, verify the same matrix against friendly board minions, wrong-tribe minions, Tavern minions, empty slots, hand cards, opponent minions, backgrounds, buttons, overlays, and targets that have already moved or been destroyed.

## 11. 动画与视觉检查 / Animation and Visual Audit

- 源卡：按下后立即进入指向源状态，不应提前消失。
- 连线：起点跟随源卡，终点跟随指针；穿过面板时不跳变。
- 合法目标：候选脉冲清晰但不遮挡攻击/生命值。
- 非法目标：红色或不可选反馈只作用于当前悬停目标。
- 释放成功：目标确认闪烁一次，随后清理所有候选状态。
- 释放失败：不能播放成功动画，不能扣除法术牌。
- 取消：连线、标签、outline、shadow、toast 和 pending target 全部清理。
- 减少动态效果：保留颜色、图标和文字反馈，禁用或缩短脉冲位移。
- 多次快速施法：不能留下幽灵卡、旧连线或错误的 `ConfirmedTarget`。

- Source card: enters targeting-source state immediately and remains visible until resolution.
- Connector: follows source and pointer without jumping across panels.
- Valid target: candidate pulse remains readable without covering Attack/Health.
- Invalid target: red/invalid feedback applies only to the currently hovered target.
- Successful release: one confirmation flash, then all candidate states clear.
- Failed release: no success animation and no spell consumption.
- Cancellation: connector, labels, outlines, shadows, toasts, and pending targets all clear.
- Reduced motion: preserve color/icon/text feedback while disabling or shortening pulse movement.
- Rapid casts: no ghost cards, stale connectors, or leaked `ConfirmedTarget` states.

## 12. 状态与效果检查 / State and Effect Audit

- 永久增益与临时增益使用正确的持续时间。
- 临时关键词在回合/战斗边界正确移除。
- 已有圣盾、嘲讽、风怒、复生时不产生重复或错误消耗。
- 变形后实例、拥有者、槽位和卡池来源正确。
- 回手后目标从战场消失并进入正确手牌位置。
- 消灭类法术触发或不触发亡语，应与真实酒馆规则一致。
- 金色化正确处理基础属性、附魔、三连和卡池数量。
- 法术触发器只结算一次：施法计数、纳迦/野猪人/饰品监听器、任务进度。
- 失败施法不得触发“使用法术后”效果。

- Permanent and temporary buffs use the correct duration.
- Temporary keywords expire at the correct turn/combat boundary.
- Existing Divine Shield, Taunt, Windfury, or Reborn does not duplicate or consume incorrectly.
- Transform preserves or replaces instance, owner, slot, and pool source as intended.
- Return-to-hand removes the target from the board and inserts it into the correct hand position.
- Destroy spells trigger or suppress Deathrattles according to real Battlegrounds rules.
- Golden conversion handles base stats, enchantments, Triples, and pool counts correctly.
- Spell listeners resolve exactly once: cast counters, Naga/Quilboar/Trinket listeners, and Quest progress.
- Failed casts must not trigger “after you cast a spell” effects.

## 13. 分辨率与遮挡检查 / Resolution and Occlusion Audit

至少在以下分辨率排查：`1920×1080`、`1366×768`、`1280×720`、`1000×600`、`994×384`。

At minimum, audit at `1920×1080`, `1366×768`, `1280×720`, `1000×600`, and `994×384`.

- 手牌扇形边缘法术能否开始拖拽。
- 最左/最右战场随从能否稳定命中。
- 酒馆操作条、功能抽屉、tooltip、toast 是否遮挡目标射线。
- 滚动面板打开时，背景卡牌不得被误命中。
- 窗口缩放后，旧目标坐标不得继续生效。

- Verify spells at the edges of the hand fan can start dragging.
- Verify leftmost and rightmost board minions are reliably raycastable.
- Verify the Tavern action bar, function drawer, tooltip, and toast do not steal target raycasts.
- When a modal or scroll panel is open, background cards must not be targetable.
- After window resize, stale target coordinates must not remain active.

## 14. 手动结果记录模板 / Manual Result Template

复制下表，为每张失败法术记录一行：

Copy this table and add one row for every failed spell:

| 日期 | 分辨率 | Card ID | 法术 | 来源 | 目标类型 | 操作 | 预期 | 实际 | 动画问题 | 状态问题 | 是否稳定复现 | 截图/日志 | 结论 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| YYYY-MM-DD | 1366×768 |  |  | Tavern/Spellcraft/Quest/Trinket | Board/Tavern |  |  |  |  |  | Yes/No |  |  |

## 15. 建议排查顺序 / Recommended Audit Order

1. 鲜血宝石：最基础的单目标路径。
2. 防御者的仪式、普通尖利箭矢：永久属性和关键词，并分别验证战场/酒馆目标。
3. 部族限定饰品宝石：非法目标原因。
4. 自然祝福、Fly the Flag、Timewarped Summoner：跨战场/酒馆目标。
5. Deep Blue、Weary Mage、Thaumaturgist：临时生命周期。
6. Repeat Customer、Raise the Stakes、Jailer Sticker：目标离场与手牌/消灭。
7. Goldenizer、Golden Arrow、Golden Hammer、Timeline Accelerator：战场/酒馆限制、变形与金色状态。
8. 第 9 节所有语义不一致条目。
9. 最后执行快速连续施法、取消和低分辨率压力检查。

1. Blood Gem: simplest single-target path.
2. Defender's Rites and normal Pointy Arrow: permanent stats, keywords, and board/Tavern targeting.
3. Tribe-restricted Jewelry Box Gems: invalid-target reasons.
4. Natural Blessing, Fly the Flag, and Timewarped Summoner: board/Tavern cross-zone targeting.
5. Deep Blue, Weary Mage, and Thaumaturgist: temporary lifecycle.
6. Repeat Customer, Raise the Stakes, and Jailer Sticker: target movement/destruction.
7. Goldenizer, Golden Arrow, Golden Hammer, and Timeline Accelerator: target-zone restrictions, transforms, and Golden state.
8. Every semantic mismatch in Section 9.
9. Finish with rapid casting, cancellation, and low-resolution stress checks.

## 16. 代码参考入口 / Code Reference Entry Points

- `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernDragController.cs`
- `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernTrainerController.cs`
- `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/TavernSpellEngine.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Models/CardMechanicTemplateResolvers.cs`
- `Assets/LearnHearthstone/Resources/Data/battlegroundsSpells.json`
- `Assets/LearnHearthstone/Resources/Data/darkmoonPrizes.json`

## 17. 完成标准 / Completion Criteria

一张法术只有同时满足以下条件才可标记为通过：

- 所有合法目标均能命中并得到正确结果；
- 所有非法目标均不能结算，且原因清晰；
- 失败释放不消耗法术、不触发施法监听器；
- 成功释放只结算一次；
- 所有视觉状态在成功、失败和取消后都完全清理；
- 临时/永久效果与真实酒馆战棋规则一致；
- 标准、宽屏、紧凑布局至少各验证一次。

A spell passes only when:

- every valid target is reachable and resolves correctly;
- every invalid target is rejected with a clear reason;
- failed release neither consumes the spell nor triggers cast listeners;
- successful release resolves exactly once;
- every visual state clears after success, failure, and cancellation;
- temporary/permanent behavior matches real Battlegrounds rules;
- wide, standard, and compact layouts have each been verified at least once.
