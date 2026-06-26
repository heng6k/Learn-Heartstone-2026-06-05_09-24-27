# 任务要求与任务奖励全量实现文档

## 目标

这份文档用于核对 Battlegrounds Quest 版本的完整任务要求和 71 个主任务奖励。它补充而不替代：

- `QuestSystemImplementationPlan.md`：任务系统底座。
- `QuestRewardHiddenPoolImplementationPlan.md`：第一批 20 个弱奖励的隐藏池实现细案。

本文件重点回答三件事：

1. 任务要求要支持到什么程度。
2. 71 个任务奖励是否列全、各自怎么实现。
3. 当前代码已经做到哪里，剩余奖励按什么顺序补。

## 当前项目状态

当前 `battlegroundsQuests.json` 已登记：

- 任务：6 个；其中 `BG27_Quest_800` 已按本轮标注改为已删除/历史保留，后续要从普通任务池移除。
- 奖励：45 个，其中 20 个官方弱奖励为 `HiddenEffectOnly`，22 个普通官方奖励为可选池，1 个官方奖励为 `DebugOnly`，2 个 Shady Aristocrat 代理钱袋奖励。

当前已经有可运行实现的官方奖励：

- `BG24_Reward_115` Theotar's Parasol
- `BG24_Reward_123` Exquisite Conch
- `BG24_Reward_125` The Smoking Gun
- `BG24_Reward_128` Mirror Shield
- `BG24_Reward_131` Red Hand
- `BG24_Reward_136` Tiny Henchmen
- `BG24_Reward_306` Cooked Book
- `BG24_Reward_312` Staff of Origination
- `BG24_Reward_321` Alter Ego
- `BG24_Reward_331` Menagerie Mayhem
- `BG24_Reward_361` Hidden Treasure Vault
- `BG24_Reward_364` Volatile Venom
- `BG24_Reward_708` Blood Goblet
- `BG24_Reward_712` Sinfall Medallion
- `BG24_Reward_715` Enhance-a-matic
- `BG27_Reward_502` Boom Squad
- `BG27_Reward_804` Sturdy Shard
- `BG27_Reward_810` Map of the Unknown
- `BG27_Reward_815` Endless Blood Moon
- `BG28_Reward_505` Tumbling Disaster
- `BG33_Reward_003` Righteous Charge
- `BG33_Reward_004` Grim Freshener
- `BG33_Reward_006` Rushing Winds
- `BG33_Reward_012` Untold Riches
- `BG24_Reward_107` Snicker Snacks
- `BG24_Reward_109` Stolen Gold
- `BG24_Reward_111` Evil Twin
- `BG24_Reward_113` Ritual Dagger
- `BG24_Reward_138` Victim's Specter
- `BG24_Reward_305` Anima Bribe
- `BG24_Reward_308` Teal Tiger Sapphire
- `BG24_Reward_309` Devils in the Details
- `BG27_Reward_503` Invigorating Conch
- `BG27_Reward_803` Turbulent Tombs
- `BG27_Reward_811` Bloodsoaked Tome
- `BG28_Reward_500` Beyond the Mirage
- `BG28_Reward_502` Splitting Scroll
- `BG28_Reward_504` Cycle of Energy
- `BG28_Reward_506` Double-Headed Reward
- `BG28_Reward_508` Gift of the Golden Kobold
- `BG28_Reward_515` Stash of the Scribe
- `BG28_Reward_518` Stable Amalgamation
- `BG33_Reward_013` Golden Forge

剩余主奖励：28 个。

官方 HearthstoneJSON 当前还包含两个不应作为主奖励进入本表的条目：

- `BG24_Reward_321t`：Alter Ego 的奇数形态子牌。
- `BG27_Anomaly_555t`：畸变相关子牌，不属于 Quest Reward 主池。

## 任务要求实现范围

官方 Quest 文本中的数字在 HearthstoneJSON 中多为 `0` 占位，实战数值由规则动态填充。项目第一版不需要还原完整官方动态难度公式，但要支持固定 `requiredAmount`、奖励强度分层、英雄护甲修正、可配置倍率，以及后续按回合/版本微调的扩展点。

### 任务难度分层规则

任务难度分为 4 档。每个任务选项由“任务要求 + 任务奖励”组成，生成三选一时先读取奖励评价，再根据英雄当前护甲修正难度，最后把难度档位换算成 `requiredAmount`。

| 难度档 | 名称 | 默认倍率 | 用途 |
| --- | --- | --- | --- |
| 1 | 轻量 | 0.75x | 弱奖励或高护甲英雄，完成速度要明显更快 |
| 2 | 标准 | 1.00x | 中等收益奖励的默认要求 |
| 3 | 困难 | 1.25x | 强奖励，需要多投入一到两回合 |
| 4 | 高压 | 1.50x | 顶级奖励或低护甲强英雄，要求最重 |

奖励评价映射：

| 奖励评价 | 基础难度 |
| --- | --- |
| 弱 | 1 |
| 中 | 2 |
| 强 | 3 |
| 顶级 | 4 |

护甲修正是默认平衡输入，但不要写死成唯一公式。实现时应支持 `QuestDifficultyProfile` 或同类配置，把英雄护甲、英雄特例、版本补丁和人工平衡覆盖合并为一个“难度修正值”。这样后续发现某个奖励或英雄组合过强/过弱时，只改配置，不改任务逻辑。

默认护甲修正：

| 英雄当前护甲 | 难度修正 |
| --- | --- |
| 0-5 | +1 |
| 6-10 | +0 |
| 11-15 | -1 |
| 16+ | -2 |

特殊处理：60 血的帕奇维克虽然不一定表现为 16+ 护甲，也按 16+ 档处理，等效难度修正为 -2。后续如果有类似“高血量、低护甲但容错很高”的英雄，也走同一套 hero override。

当前实现已经落到 `QuestDifficultyProfile`：护甲分段、英雄特例、高血量等效修正、档位倍率都视为默认平衡配置，而不是不可改的公式。计算方式仍是 `difficultyTier = Clamp(1, 4, rewardPowerTier + difficultyModifier)`。最终需求量默认用 `Ceil(baseRequiredAmount * tierMultiplier)`，且最小值不低于 1。没有英雄护甲数据时按 6-10 护甲处理。`DebugOnly` 奖励如果被指定挂载，也使用表内奖励评价计算难度；`Disabled` 奖励不参与任务生成。

### 任务要求总表

| ID | 任务 | 官方要求 | 需要实现到什么程度 | 当前状态/处理决定 |
| --- | --- | --- | --- | --- |
| `BG24_Quest_112` | Track the Footprints | 刷新酒馆 N 次 | 统计成功的手动刷新；免费刷新也算，只要 shop 实际刷新；自动回合刷新不算 | 已实现；接入 4 档难度倍率 |
| `BG24_Quest_114` | Assemble a Lineup | 召唤 N 个随从 | 统计战斗中和酒馆中由效果召唤的友方随从；打出手牌不算召唤；衍生物算 | 未实现；保留 |
| `BG24_Quest_120` | Unmask the Culprit | 输或平 N 场战斗 | 战斗结算后若结果为失败或平局则 +1；训练器手动 combat test 也应触发 | 未实现；保留 |
| `BG24_Quest_123` | Find the Murder Weapon | 使友方随从属性提升 N 次 | 任意友方随从获得正向 Attack 或 Health 增量算 1 次事件；一次群体 buff 按被 buff 随从数计 | 未实现；保留 |
| `BG24_Quest_124` | Reenact the Murder | 友方随从死亡 N 个 | 战斗中友方随从死亡计数；酒馆阶段出售不算死亡 | 未实现；保留 |
| `BG24_Quest_125` | Sort It All Out | 按攻击从低到高排列并战斗 N 次 | 不实现新触发点；数据可保留作历史记录 | 已删除：古早机制，不进任务池 |
| `BG24_Quest_126` | Follow the Money | 花费 N 金币 | 购买、刷新、升级、购买高级机制等真实金币支出计数；生命支付不算金币 | 已实现；接入 4 档难度倍率 |
| `BG24_Quest_151` | Unlikely Duo | 购买 14 个指定两类/占位类型 | 官方文本含 `0 or 0` 占位；需查 child/quest script 决定两类，第一版可 `DebugOnly` | 未实现；资料待确认 |
| `BG24_Quest_311` | Cry for Help | 打出 N 个战吼随从 | 打出带 `Battlecry` 关键词的随从计数；重复触发战吼不额外计数 | 已实现；接入 4 档难度倍率 |
| `BG24_Quest_313` | Invite the Guests | 购买 N 个随从 | 买入 `CardKind.Minion` 计数；购买酒馆法术不算 | 已实现；接入 4 档难度倍率 |
| `BG24_Quest_314` | Dust for Prints | 添加 N 张牌到手牌 | 购买、发现、生成、战斗奖励进手都计数；手牌满导致未加入不计 | 已实现；接入 4 档难度倍率 |
| `BG24_Quest_318` | Witness Protection | 友方嘲讽随从被攻击 N 次 | 不实现新触发点；数据可保留作历史记录 | 已删除：古早机制，不进任务池 |
| `BG24_Quest_320` | Exhume the Bones | 触发 N 个友方亡语 | 不实现任务进度；亡语额外触发仍可服务奖励效果 | 已删除：古早机制，不进任务池 |
| `BG24_Quest_328` | Close the Case | 赢得游戏 | 单人训练器没有大厅胜利；不作为正常任务目标 | 已删除：训练器不适合，不进任务池 |
| `BG24_Quest_351` | Hire an Investigator | 回合结束时剩余未花金币 N 次 | 不实现任务进度；避免鼓励空过回合 | 已删除：古早机制，不进任务池 |
| `BG24_Quest_352` | Crack the Case | 友方随从攻击 N 次 | 战斗中友方随从发起攻击计数；风怒和立即攻击都计 | 未实现；保留 |
| `BG24_Quest_Bob` | An Investigation! | 3 回合后选择任务 | Bob 元任务，不进入普通 quest 池；可作为任务模式开局延迟弹窗的调试代理 | 未实现；非主任务要求 |
| `BG27_Quest_800` | Burn the Evidence | 出售 N 个随从 | 不实现任务进度；出售事件仍可服务奖励效果 | 已删除：古早机制，不进任务池 |
| `BG27_Quest_801` | Pressure the Authorities | 战队总攻击达到 28 | 每次 board 改动和战斗中属性变化后检查友方战队总 Attack 是否达到阈值；阈值走 `requiredAmount`，不固定为 28 | 未实现；保留，可在战斗中完成 |
| `BG27_Quest_802` | Round Up the Suspects | 消灭 N 个敌方随从 | 战斗中由友方随从、法术、奖励导致敌方死亡计数；自毁不算敌方被消灭 | 未实现；保留 |
| `BG28_Quest_500` | Fill the Cauldron | 施放 N 个法术 | 施放任意 spell 计数；酒馆法术、塑造法术、生成 spell 都算；被动自动施放是否计数需标注来源 | 未实现；保留 |

### 任务要求新增触发点

需要扩展 `QuestObjectiveKind`：

```csharp
SummonMinions,
LoseOrTieCombats,
BuffFriendlyMinions,
FriendlyMinionsDied,
BuySpecificTypes,
FriendlyMinionsAttacked,
WarbandTotalAttack,
DestroyEnemyMinions,
CastSpells
```

需要扩展事件记录：

- 酒馆阶段：买牌、施法、加手牌、buff、真实金币支出。
- 战斗阶段：友方召唤、友方死亡、敌方死亡、友方攻击、胜负结果。
- 棋盘检查：总攻击。

## 奖励池策略

推荐四类池：

| 池 | 含义 |
| --- | --- |
| `Offerable` | 可以进入普通任务三选一 |
| `HiddenEffectOnly` | 效果要实现，但因强度低、训练器体验差或用户指定不进普通池 |
| `DebugOnly` | 可通过调试或指定 ID 挂载，但普通池不出现 |
| `Disabled` | 数据保留，不可挂载，通常因为官方占位、双打/传递、当前训练器无对应系统 |

已明确为弱奖励隐藏池的 20 个保持 `HiddenEffectOnly`。涉及伙伴、第二英雄技能、7 本、饰品选择、Yogg 轮盘、Wisdomball、Zerus 等复杂系统的奖励可以先 `DebugOnly`，等依赖系统完成后再评估是否 `Offerable`。

奖励评价用于任务难度，不直接等同于能否进入普通池：

| 奖励评价 | 含义 | 难度影响 |
| --- | --- | --- |
| 弱 | 收益偏低、慢热或用户已指定隐藏；可以实现效果但任务要求应低 | 基础难度 1 |
| 中 | 有稳定收益，但不明显改变整局节奏 | 基础难度 2 |
| 强 | 能显著改变经济、身材、发现或战斗能力 | 基础难度 3 |
| 顶级 | 接近核心构筑或版本级奖励，需要最高任务压力 | 基础难度 4 |
| 待确认 | 官方占位或依赖系统未明，暂不进入普通生成 | 不参与普通生成 |

## 71 个任务奖励全量实现表

| # | ID | 奖励 | 奖励评价 | 池建议 | 当前状态 | 实现到什么程度 |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `BG24_Reward_107` | Snicker Snacks | 强 | Offerable | 未实现 | 回合结束随机 2 个友方 Battlecry 随从触发战吼；复用现有 Battlecry 解析，目标按随从自身战吼规则取默认/自动目标；无合法战吼则 no-op |
| 2 | `BG24_Reward_109` | Stolen Gold | 强 | Offerable | 未实现 | 战斗开始将最左和最右战斗副本临时金色；同一随从只处理一次；不回写酒馆棋盘；需复用 Golden 标记和金色效果倍率 |
| 3 | `BG24_Reward_111` | Evil Twin | 强 | Offerable | 未实现 | 战斗开始召唤最高 Health 友方随从复制；复制保留附魔和关键词，生成新实例；满场 no-op；平手按最左 |
| 4 | `BG24_Reward_113` | Ritual Dagger | 强 | Offerable | 未实现 | 友方带 Deathrattle 随从在战斗中死亡后，酒馆原始随从永久 +5/+5；若死亡的是衍生复制，按 `OriginalInstanceId` 回写，否则记录战斗奖励回传 |
| 5 | `BG24_Reward_115` | Theotar's Parasol | 弱 | HiddenEffectOnly | 已实现 | 回合结束最右随从 +0/+8 并获得临时 Stealth；下个友方回合开始移除该来源 Stealth |
| 6 | `BG24_Reward_123` | Exquisite Conch | 弱 | HiddenEffectOnly | 已实现 | 每回合第一个 Battlecry 额外 +2 次；与 Brann 类效果按“当前重复次数 +2”处理 |
| 7 | `BG24_Reward_125` | The Smoking Gun | 弱 | HiddenEffectOnly | 已实现 | 当前棋盘和后续打出随从获得 +4 Attack；战斗副本和战斗召唤也吃 +4 Attack aura |
| 8 | `BG24_Reward_128` | Mirror Shield | 弱 | HiddenEffectOnly | 已实现 | 成功刷新后随机酒馆随从 +6/+6 并获得 Divine Shield |
| 9 | `BG24_Reward_129` | Secret Sinstone | 强 | Offerable | 未实现 | Discover 选中一张牌后，额外给一张复制进手；需在 `ChooseDiscover` 后记录被选卡，复制生成新实例；手牌满则跳过 |
| 10 | `BG24_Reward_130` | Ghastly Mask | 强 | DebugOnly | 未实现 | 获得指定随从复制，并让回合结束效果额外触发一次；官方 `'0'` child 卡需查；第一版可只实现 end-turn repeat aura |
| 11 | `BG24_Reward_131` | Red Hand | 弱 | HiddenEffectOnly | 已实现 | 回合开始随机手牌随从 +12/+12 |
| 12 | `BG24_Reward_134` | The Friends Along the Way | 待确认 | Disabled | 未实现 | 官方文本为 `2 random 92`，占位 92 含义需查；确认前不进池 |
| 13 | `BG24_Reward_135` | Yogg-tastic Tasties | 中 | DebugOnly | 未实现 | 回合开始转 Yogg 轮盘；需实现轮盘结果表、动画可省略、效果必须可测；第一版可用 6 个代理结果 |
| 14 | `BG24_Reward_136` | Tiny Henchmen | 中 | Offerable | 已实现 | 回合结束最多 3 个 Tier <= 3 友方随从 +3/+3 |
| 15 | `BG24_Reward_138` | Victim's Specter | 中 | Offerable | 未实现 | 战斗后获得最后死亡友方随从的原始复制；plain copy 不保留附魔；需要 CombatEngine 回传死亡顺序 |
| 16 | `BG24_Reward_305` | Anima Bribe | 中 | Offerable | 未实现 | 出售随从后，将被卖随从当前 Attack/Health 给予随机酒馆随从；无酒馆随从 no-op；卖出后再释放进池 |
| 17 | `BG24_Reward_306` | Cooked Book | 强 | Offerable | 已实现 | 购买随从后给其 +2/+2，之后该奖励永久提升 +1/+1 |
| 18 | `BG24_Reward_308` | Teal Tiger Sapphire | 中 | Offerable | 未实现 | 酒馆随从获得本回合刷新次数 * +1/+1；刷新次数每回合清零；新 shop 重算临时 aura，购买后保留已获得属性 |
| 19 | `BG24_Reward_309` | Devils in the Details | 强 | Offerable | 未实现 | 回合结束最左和最右随从各吞食一个酒馆随从获得其属性；从 shop 移除被吞目标并补空位/下次刷新补 |
| 20 | `BG24_Reward_310` | Partner in Crime | 强 | DebugOnly | 未实现 | 完成时获得金色伙伴；依赖 hero/buddy 数据，当前英雄无伙伴时 no-op 并日志 |
| 21 | `BG24_Reward_311` | Another Hidden Body | 中 | Offerable | 未实现 | 完成时 Discover 当前酒馆等级随从；可重复获得意味着奖励完成后保留可再次触发入口，需定义触发条件，第一版按完成时一次 + Debug repeat |
| 22 | `BG24_Reward_312` | Staff of Origination | 弱 | HiddenEffectOnly | 已实现 | 战斗开始战斗副本全体 +12/+12，不回写酒馆 |
| 23 | `BG24_Reward_313` | Wondrous Wisdomball | 强 | DebugOnly | 未实现 | 刷新时偶尔替换为帮助性 shop；需要 Wisdomball 事件池，第一版实现 4 类代理刷新：高星、对子、同种族、含法术 |
| 24 | `BG24_Reward_321` | Alter Ego | 弱 | HiddenEffectOnly | 已实现 | 当前偶数/奇数酒馆随从 +7/+7；回合开始切换 parity；刷新只按当前 parity 重算 |
| 25 | `BG24_Reward_331` | Menagerie Mayhem | 弱 | HiddenEffectOnly | 已实现 | 回合结束按友方不同类型数量给全体 +N/+N |
| 26 | `BG24_Reward_350` | Pilfered Lamps | 顶级 | Offerable | 未实现 | 三连规则改为 2 张相同随从即可；需要 TripleEngine 支持 quest triple threshold；只影响玩家，不影响对手 |
| 27 | `BG24_Reward_361` | Hidden Treasure Vault | 强 | Offerable | 已实现 | 回合开始获得金币并逐回合提升 |
| 28 | `BG24_Reward_362` | Essence of Zerus | 弱 | DebugOnly | 未实现 | 回合结束获得 Shifter Zerus；手牌中的 Zerus 每回合变形成随机合法随从；需要变形卡状态 |
| 29 | `BG24_Reward_363` | Ethereal Evidence | 顶级 | DebugOnly | 未实现 | 每回合开始从 2 个新奖励中选择；选择后替换/叠加当前 reward 需明确，第一版建议作为 BonusReward 叠加，且只从 Offerable/Debug safe 池抽 |
| 30 | `BG24_Reward_364` | Volatile Venom | 弱 | HiddenEffectOnly | 已实现 | 战斗副本 +7/+7，友方随从攻击后死亡 |
| 31 | `BG24_Reward_708` | Blood Goblet | 弱 | HiddenEffectOnly | 已实现 | 回合结束最右随从获得等同已损生命的 Attack；护甲不算缺失生命 |
| 32 | `BG24_Reward_712` | Sinfall Medallion | 弱 | HiddenEffectOnly | 已实现 | 打出随从后，最多 2 个其他同 Tier 友方随从 +4/+4 |
| 33 | `BG24_Reward_715` | Enhance-a-matic | 弱 | HiddenEffectOnly | 已实现 | 回合开始获得一个 +5/+5 且带 Taunt/Windfury/Divine Shield/Reborn 的零件 |
| 34 | `BG24_Reward_718` | Kidnap Sack | 强 | DebugOnly | 未实现 | Spellcraft：选择非金色卡牌移入手牌；可选目标包括酒馆和友方棋盘，第一版建议只允许酒馆非金卡，避免偷自己场面状态复杂 |
| 35 | `BG24_Reward_719` | The Golden Hammer | 强 | DebugOnly | 未实现 | Spellcraft：使友方随从临时金色直到下回合；需临时 Golden 标记和回合开始还原，不回写三连 |
| 36 | `BG27_Reward_502` | Boom Squad | 弱 | HiddenEffectOnly | 已实现 | 战斗中 Avenge(3) 对最高 Health 敌方随从造成 10 伤害并立即结算死亡 |
| 37 | `BG27_Reward_503` | Invigorating Conch | 中 | Offerable | 未实现 | 购买随从时，将买入随从当前属性给予随机友方随从；买入随从仍进手；无友方 no-op |
| 38 | `BG27_Reward_504` | Timeline Acceleration | 中 | DebugOnly | 未实现 | 回合开始获得 2 个 Accelerator 法术，使目标随从变形成高一星随机随从；需生成 spell 和 transform helper |
| 39 | `BG27_Reward_802` | Gilnean War Horn | 强 | DebugOnly | 未实现 | 获得指定复制；你的 Battlecry 额外触发一次；官方 `'0'` child 需查，第一版可只做全局 Battlecry +1 |
| 40 | `BG27_Reward_803` | Turbulent Tombs | 强 | DebugOnly | 未实现 | 获得指定复制；你的 Deathrattle 额外触发一次；需 CombatEngine deathrattle repeat +1 |
| 41 | `BG27_Reward_804` | Sturdy Shard | 弱 | HiddenEffectOnly | 已实现 | 回合结束按嘲讽数量给非嘲讽友方随从 +T/+2T |
| 42 | `BG27_Reward_806` | Doppelganger's Locket | 中 | Offerable | 未实现 | 战斗后 Discover 上个对手战队中非金随从，保留附魔复制进手；依赖 OpponentHistory 和 Discover |
| 43 | `BG27_Reward_810` | Map of the Unknown | 弱 | HiddenEffectOnly | 已实现 | 打出未控制类型随从后，每个友方类型各选一个 +2/+2 |
| 44 | `BG27_Reward_811` | Bloodsoaked Tome | 强 | Offerable | 未实现 | 酒馆随从购买费用固定为 2；不影响酒馆法术；费用 UI 和购买校验共用 helper |
| 45 | `BG27_Reward_812` | Scepter of Guidance | 待确认 | Disabled | 未实现 | 官方文本为 `2 92`，占位 92 含义需查；确认前不进池 |
| 46 | `BG27_Reward_815` | Endless Blood Moon | 弱 | HiddenEffectOnly | 已实现 | Blood Gem 额外 +1/+1；回合开始获得 2 个 Blood Gem |
| 47 | `BG28_Reward_500` | Beyond the Mirage | 强 | Offerable | 未实现 | 酒馆法术费用 -1，最低 0；购买和 UI 都走费用 helper |
| 48 | `BG28_Reward_501` | Temporal Tampering | 强 | Offerable | 未实现 | 你的酒馆法术额外施放一次；在 TavernSpellEngine 入口重复解析一次，避免重复支付费用 |
| 49 | `BG28_Reward_502` | Splitting Scroll | 中 | Offerable | 未实现 | 购买费用 >=3 的酒馆法术后额外获得一张复制；判断折扣后实际费用还是原始费用需实测，第一版按当前购买费用 |
| 50 | `BG28_Reward_504` | Cycle of Energy | 中 | Offerable | 未实现 | 战斗中 Avenge(3) 队列随机酒馆法术奖励，战斗后进手；手牌满跳过 |
| 51 | `BG28_Reward_505` | Tumbling Disaster | 弱 | HiddenEffectOnly | 已实现 | 战斗召唤随从 +当前数值；Avenge(4) 后永久提升，第一版按 +1/+1 |
| 52 | `BG28_Reward_506` | Double-Headed Reward | 强 | Offerable | 未实现 | 每回合第一次购买卡牌时额外获得复制；随从/法术都算；复制进手，手牌满跳过 |
| 53 | `BG28_Reward_508` | Gift of the Golden Kobold | 中 | Offerable | 未实现 | 每刷新 5 次，使当前酒馆最高 Tier 随从金色；计数触发后重置为 5；平手随机/最左需定，第一版最左 |
| 54 | `BG28_Reward_509` | Smelting Chamber | 中 | Offerable | 未实现 | 回合开始使一个友方 Tier 1 随从金色并提升 tier 条件或数量；官方 improve 需确认，第一版每触发后可作用 Tier +1 |
| 55 | `BG28_Reward_510` | Secret Culprit | 强 | DebugOnly | 未实现 | 获得指定 Tier 7 卡复制；依赖 7 本卡池和官方 child，未确认前 DebugOnly |
| 56 | `BG28_Reward_513` | Open Auditions | 强 | DebugOnly | 未实现 | 回合开始 Discover 一个 Buddy；依赖完整 Buddy 池和 hero 限制 |
| 57 | `BG28_Reward_514` | Untamed Sorcery | 顶级 | DebugOnly | 未实现 | 回合开始随机施放 5 个酒馆法术；需随机法术池、合法目标自动选择和防无限触发 |
| 58 | `BG28_Reward_515` | Stash of the Scribe | 强 | Offerable | 未实现 | 回合开始获得 3 个随机酒馆法术；从当前 tier 可用 spell pool 抽，手牌满停止 |
| 59 | `BG28_Reward_518` | Stable Amalgamation | 强 | Offerable | 未实现 | 战斗中 Avenge(7)，有空位时召唤 50/50 Amalgam；需 token 定义和 CombatEngine summon |
| 60 | `BG33_Reward_003` | Righteous Charge | 弱 | HiddenEffectOnly | 已实现 | 战斗开始最左随从 Divine Shield 并立即攻击 |
| 61 | `BG33_Reward_004` | Grim Freshener | 弱 | HiddenEffectOnly | 已实现 | Avenge(2) 获得一次免费刷新，战斗后回写 |
| 62 | `BG33_Reward_006` | Rushing Winds | 弱 | HiddenEffectOnly | 已实现 | 回合开始获得临时 Spellcraft，给予 Windfury 和 Divine Shield |
| 63 | `BG33_Reward_010` | Norgannon's Reward | 顶级 | DebugOnly | 未实现 | 解锁 Tier 7；下回合开始仅一次自动升级酒馆；需要 TavernRules 支持 Tier 7、shop size、升级费用 |
| 64 | `BG33_Reward_011` | Magicfin Relic | 中 | DebugOnly | 未实现 | 回合开始获得 1/1 Murloc，并 Discover 酒馆法术教给它；需要“法术附着到随从”代理，可先将 spell 效果立即施放到 token |
| 65 | `BG33_Reward_012` | Untold Riches | 强 | Offerable | 已实现 | 完成时获得 5 金币并最大金币 +5 |
| 66 | `BG33_Reward_013` | Golden Forge | 中 | Offerable | 未实现 | 回合开始使酒馆最高 Tier 随从金色；平手按最左；只作用 shop，不进三连逻辑 |
| 67 | `BG33_Reward_014` | Quaint Boutique | 强 | DebugOnly | 未实现 | 下回合开始获得 4 金币并选择小饰品购买；依赖饰品选择和购买 UI，可复用 Lesser Trinket pending choice |
| 68 | `BG33_Reward_015` | Jumbo Warehouse | 强 | DebugOnly | 未实现 | 下回合开始获得 4 金币并选择大饰品购买；复用 Greater Trinket pending choice |
| 69 | `BG33_Reward_017` | Cosmic Reward | 顶级 | DebugOnly | 未实现 | Discover 第二个英雄技能；依赖 hero power equip/multiple hero power 系统 |
| 70 | `BG33_Reward_020` | Perpetual Incantation | 强 | Offerable | 未实现 | 酒馆法术给予额外 +2/+1，且可无限提升；需要 TavernSpellEngine 的 stat spell bonus hook 和提升计数 |
| 71 | `BG33_Reward_021` | Rallying Cry | 待确认 | DebugOnly | 未实现 | 获得指定复制；Rally 额外触发一次；依赖 Rally 关键词/触发系统，未完成前 DebugOnly |

## 需要新增的 RewardEffectKind

建议新增或整理为以下效果类型：

```csharp
TriggerBattlecriesAtEndOfTurn,
MakeEdgeMinionsGoldenForCombat,
SummonHighestHealthCopyAtCombatStart,
PermanentBuffDeathrattleMinionAfterDeath,
ExtraDiscoverCopy,
ExtraEndOfTurnTriggers,
GainRandomPlaceholder92,
SpinYoggWheel,
GainLastDeadFriendlyPlainCopyAfterCombat,
SellMinionStatsToShop,
RefreshCountShopBuffAura,
EdgeMinionsConsumeShop,
GainGoldenBuddy,
DiscoverCurrentTierMinionRepeatable,
WisdomballHelpfulRefreshes,
TwoCopiesTripleRule,
GainTransformingZerus,
ChooseNewRewardsEachTurn,
KidnapSackSpellcraft,
TemporaryGoldenSpellcraft,
BuyMinionStatsToFriendly,
GainTierUpTransformSpells,
ExtraBattlecryTriggers,
ExtraDeathrattleTriggers,
DiscoverOpponentWarbandMinionAfterCombat,
SetTavernMinionCost,
GuidancePlaceholder92ShopSlots,
TavernSpellCostDiscount,
ExtraTavernSpellCast,
CopyExpensiveBoughtTavernSpell,
AvengeGainRandomTavernSpell,
FirstBuyEachTurnCopy,
GoldenHighestTierShopAfterRefreshes,
GoldenFriendlyTierOneAndImprove,
GainTierSevenCopy,
DiscoverBuddyEachTurn,
CastRandomTavernSpells,
GainRandomTavernSpells,
AvengeSummonAmalgam,
UnlockTierSevenAndAutoUpgrade,
MagicfinRelic,
DelayedLesserTrinketChoice,
DelayedGreaterTrinketChoice,
DiscoverSecondHeroPower,
ScalingTavernSpellBonus,
ExtraRallyTriggers
```

## 实施顺序

### 第一批：低风险酒馆阶段奖励

优先做不依赖战斗深改和复杂 UI 的奖励：

1. `BG24_Reward_305` Anima Bribe（奖励评价：中）
2. `BG27_Reward_503` Invigorating Conch（奖励评价：中）
3. `BG28_Reward_506` Double-Headed Reward（奖励评价：强）
4. `BG28_Reward_515` Stash of the Scribe（奖励评价：强）
5. `BG28_Reward_500` Beyond the Mirage（奖励评价：强）
6. `BG27_Reward_811` Bloodsoaked Tome（奖励评价：强）
7. `BG28_Reward_502` Splitting Scroll（奖励评价：中）
8. `BG33_Reward_013` Golden Forge（奖励评价：中）

### 第二批：回合结束和刷新奖励

1. `BG24_Reward_107` Snicker Snacks（奖励评价：强）
2. `BG24_Reward_308` Teal Tiger Sapphire（奖励评价：中）
3. `BG24_Reward_309` Devils in the Details（奖励评价：强）
4. `BG28_Reward_508` Gift of the Golden Kobold（奖励评价：中）
5. `BG24_Reward_138` Victim's Specter（奖励评价：中）

### 第三批：战斗注入和 Avenge 奖励

1. `BG24_Reward_109` Stolen Gold（奖励评价：强）
2. `BG24_Reward_111` Evil Twin（奖励评价：强）
3. `BG24_Reward_113` Ritual Dagger（奖励评价：强）
4. `BG28_Reward_504` Cycle of Energy（奖励评价：中）
5. `BG28_Reward_518` Stable Amalgamation（奖励评价：强）
6. `BG27_Reward_803` Turbulent Tombs（奖励评价：强）

### 第四批：发现、伙伴、饰品、英雄技能依赖奖励

1. `BG24_Reward_129` Secret Sinstone（奖励评价：强）
2. `BG27_Reward_806` Doppelganger's Locket（奖励评价：中）
3. `BG24_Reward_310` Partner in Crime（奖励评价：强）
4. `BG28_Reward_513` Open Auditions（奖励评价：强）
5. `BG33_Reward_014` Quaint Boutique（奖励评价：强）
6. `BG33_Reward_015` Jumbo Warehouse（奖励评价：强）
7. `BG33_Reward_017` Cosmic Reward（奖励评价：顶级）

### 第五批：复杂代理和资料待确认

1. `BG24_Reward_130` Ghastly Mask（奖励评价：强）
2. `BG24_Reward_134` The Friends Along the Way（奖励评价：待确认）
3. `BG24_Reward_135` Yogg-tastic Tasties（奖励评价：中）
4. `BG24_Reward_313` Wondrous Wisdomball（奖励评价：强）
5. `BG24_Reward_362` Essence of Zerus（奖励评价：弱）
6. `BG24_Reward_363` Ethereal Evidence（奖励评价：顶级）
7. `BG27_Reward_812` Scepter of Guidance（奖励评价：待确认）
8. `BG28_Reward_510` Secret Culprit（奖励评价：强）
9. `BG28_Reward_514` Untamed Sorcery（奖励评价：顶级）
10. `BG33_Reward_010` Norgannon's Reward（奖励评价：顶级）
11. `BG33_Reward_011` Magicfin Relic（奖励评价：中）
12. `BG33_Reward_020` Perpetual Incantation（奖励评价：强）
13. `BG33_Reward_021` Rallying Cry（奖励评价：待确认）

## 测试要求

每个奖励至少一条直接测试。共性测试：

- 普通三选池不出现 `HiddenEffectOnly/DebugOnly/Disabled`。
- 指定 reward id 可以挂载 `HiddenEffectOnly/DebugOnly`。
- 普通任务池不出现已删除任务：`BG24_Quest_125`、`BG24_Quest_318`、`BG24_Quest_320`、`BG24_Quest_328`、`BG24_Quest_351`、`BG27_Quest_800`。
- 任务三选一同时显示任务要求和任务奖励，并按奖励评价 + 英雄护甲计算 4 档 `requiredAmount`。
- 低护甲强英雄拿强/顶级奖励时难度升高；高护甲英雄拿同奖励时难度降低，结果始终 clamp 到 1-4 档。
- 所有奖励图片可加载，缺图时测试失败。
- 所有 `Discover` 奖励在已有 pending choice 时不覆盖。
- 所有战斗临时效果战斗结束后清理。
- 所有复制出来的卡有新 `InstanceId`。
- 手牌满、酒馆空、棋盘满时不崩溃并写日志。
- 卡池相关奖励遵守版本和种族限制。

## 资料待确认清单

以下内容官方 JSON 有占位或依赖未完成系统，实施前需要重点查证：

- `BG24_Reward_130`、`BG27_Reward_802`、`BG27_Reward_803`、`BG28_Reward_510`、`BG33_Reward_021` 中 `Get a copy of '0'` 的 child card 绑定。
- `BG24_Reward_134` 和 `BG27_Reward_812` 的 `92` 占位含义。
- `BG24_Quest_151` 的两个购买类型占位。
- 已标注删除的 Quest 不再查证；除非后续明确重新启用，否则只作为历史数据保留。
- `BG24_Reward_363` 新奖励是替换主奖励、叠加 bonus reward，还是本回合临时选择。
- `BG28_Reward_509` 的 improve 具体提升规则。
- `BG33_Reward_020` 的“可无限提升”触发来源。
- `Rally` 关键词在项目里的事件定义。

## 完成标准

- 21 个 Quest 要求中，6 个已标注删除并从普通任务池移除，Bob 元任务只保留为调试/延迟开局代理。
- 剩余 14 个主 Quest 要求都有可测试实现或明确 DebugOnly 代理；`BG24_Quest_151` 在占位类型确认前不进普通池。
- 任务需求量支持 4 档难度：奖励越好越难，英雄护甲越多越简单。
- 71 个主奖励均登记到 JSON，且有 `offerPoolStatus`。
- 71 个主奖励均登记奖励评价；`待确认` 和 `Disabled` 不参与普通任务生成。
- 71 个主奖励均有图片路径；缺图必须在文档和测试中暴露。
- 43 个当前已实现官方奖励继续通过回归。
- 剩余 28 个奖励按上面实施顺序逐批完成。
- 所有双打、队友、传递类效果保持删除或 Disabled，不进入普通池。
