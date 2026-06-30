# 扭曲时空酒馆总机制

## 定义

扭曲时空酒馆是独立于普通酒馆的限时特殊商店。它不是普通刷新池的一部分，也不是把当前酒馆直接替换成另一组普通随从；它在指定回合打开，用独立货币购买一组带 `BACON_TIMEWARPED` 标记的特殊卡。

本项目里建议拆成三个概念:

- `Timewarp Visit`: 第 6/9 回合进入特殊酒馆的一次访问。
- `Timewarped Tavern`: 特殊商店界面和候选池。
- `Chronum`: Timewarped Tavern 的独立购买货币。未花完的 Chronum 可保存到下一次 Timewarp。

## 触发回合

Firestone 数据中的系统牌 `BG34_BlackMarket` 是 `Timewarped Tavern System`，文本为 `On Turns 6 and 9, visit the Timewarped Tavern.`。

实现规则:

- 第 6 回合触发 Minor Timewarp。
- 第 9 回合触发 Major Timewarp。
- 如果后续接入英雄/异常/法术额外触发 Timewarp，仍复用同一套访问流程，只改变候选池、Chronum 或入口来源。

## 和饰品的同回合顺序

用户指定规则: 饰品和扭曲时空同一回合时，先饰品，后扭曲时空酒馆。

工程规则:

- `NextTurn()` 先执行现有回合开始、饰品 turn-start、饰品自动选择逻辑。
- 如果第 6/9 回合饰品选择打开了 `AdvancedMechanics.PendingChoice`，Timewarp 不覆盖它。
- Timewarp 记录为待打开状态，例如:
  - `timewarp:pending-round = State.Round`
  - `timewarp:pending-kind = minor/major`
  - `timewarp:pending-source = turn-schedule`
- 玩家选完饰品后，`ChooseMechanicOption()` 清空 pending，再检查并打开待处理的 Timewarp。
- 如果没有饰品 pending，Timewarp 立即打开。

短期实现可以用 `AdvancedMechanicState.Counters` 存 pending；长期更稳的是把 `PendingChoice` 扩成队列。

## 候选池

### 当前版本池

当前版本随从池按 Firestone 静态数据过滤:

```text
type == "Minion"
premium != true
mechanics contains "BACON_TIMEWARPED"
isBaconPool == true
```

本轮抓取结果:

- 当前版本 Timewarped 随从: 125 个。
- Minor: `techLevel = 3`，55 个。
- Major: `techLevel = 5`，70 个。

### 上线/历史额外池

Firestone 静态数据中还有 33 个普通 Timewarped 随从不在当前池:

```text
type == "Minion"
premium != true
mechanics contains "BACON_TIMEWARPED"
isBaconPool != true
```

这些只作为历史/上线版本候选，不默认进入当前版本。需要做“上线版本复刻”时再开关启用。

### Timewarped 法术/宝藏

Firestone 静态数据还有带 `BACON_TIMEWARPED` 的非随从牌，包括 Timewarped Treasure、第二英雄技能、买入即施放法术等。完整 Timewarped Tavern 应支持这些牌。

第一版可以先做随从购买，但数据结构不要写死为 `MinionDefinition`，建议抽象为 `TimewarpedTavernCardDefinition`，字段至少包含:

- `CardId`
- `DbfId`
- `Name`
- `CardKind`
- `TimewarpKind`
- `Cost`
- `TechLevel`
- `Text`
- `ImagePath`
- `Tags`
- `EffectIds`
- `PoolStatus`

## Minor 与 Major

当前数据里:

- Minor 对应 `techLevel = 3`。
- Major 对应 `techLevel = 5`。
- `techLevel = 0` 的 Timewarped 随从视为历史/特殊候选，不直接按 Minor/Major 投放。

第 6 回合打开 Minor Tavern 时:

- 候选来自当前池 `techLevel = 3`。
- 如果接入 Timewarped spell，也只取 Minor 对应 `techLevel = 3`。

第 9 回合打开 Major Tavern 时:

- 候选来自当前池 `techLevel = 5`。
- 如果接入 Timewarped spell，也只取 Major 对应 `techLevel = 5`。

## Chronum

Chronum 是 Timewarped Tavern 的专用货币，不等同于普通金币。

实现规则:

- 每次 Timewarp 入口发放一批 Chronum。
- 卡牌购买消耗 `cost` 字段，不消耗普通金币。
- 未花完的 Chronum 保留到下一次 Timewarp。
- `Exit the Timewarped Tavern` 是退出/跳过牌，文本是保存 Chronum 到下一次 Timewarp。
- 普通回合结束不清空 Chronum，只有机制明确消耗或重置时才变。

待确认:

- 第 6 回合初始 Chronum 数量。
- 第 9 回合额外发放数量。
- 是否有保底额外 Chronum 或随回合/英雄/异常变化。

建议状态:

```csharp
public sealed class PlayerTimewarpTavernState
{
    public int Chronum;
    public int NextTimewarpBonusChronum;
    public int LastVisitRound;
    public TimewarpKind PendingKind;
    public bool VisitOpen;
    public List<TimewarpedOfferSlot> Offers = new List<TimewarpedOfferSlot>();
}
```

## 商店展示

一次 Timewarped Tavern 访问应生成一组 offer slot。第一版规则建议:

- 打开时一次性生成 offer。
- offer 使用 deterministic seed，保证测试稳定。
- 同一访问内购买后只移除该 slot，不自动从普通池补牌。
- 是否允许刷新 Timewarped Tavern 暂时不要做，除非后续确认官方有刷新按钮/刷新成本。
- 固定加入 `Exit the Timewarped Tavern` 或 UI 退出按钮，用于不购买并保留 Chronum。

待确认:

- 每次展示多少个 offer。
- 是否必定包含退出牌。
- 是否允许冻结、刷新、锁定或保存 offer。

## 购买规则

购买 Timewarped 随从:

- 检查 Chronum 是否足够。
- 检查手牌上限，手牌满则购买失败或提示。
- 扣 Chronum。
- 生成一个新的 `MinionInstance` 到手牌。
- `PoolSource = Copy` 或新增 `PoolSource.Timewarped`。
- 不扣普通 `MinionPool` 副本。
- 生成新 `InstanceId`，不能复用定义 ID 或旧实例 ID。

购买 `Casts When Bought` 的 Timewarped spell:

- 检查 Chronum。
- 扣 Chronum。
- 立即执行效果。
- 不进手牌。
- 写 recruit log。

购买普通 Timewarped spell/treasure:

- 如果牌面是直接法术，按现有 Tavern Spell 目标/发现/施放流程处理。
- 如果牌面写 `Get`，生成对应牌进手牌。
- 如果牌面写 `Discover`，打开 discover 流程。

## 退出规则

退出 Timewarped Tavern 时:

- 关闭特殊商店 UI。
- 保留剩余 Chronum。
- 普通酒馆状态不变。
- 不触发普通刷新事件。
- 不自动推进回合。

如果 Timewarp 打开时普通酒馆已有冻结牌、额外牌、酒馆光环:

- 不释放、不刷新、不改动普通酒馆。
- Timewarped Tavern 使用独立 offer list。

## 和普通酒馆的关系

Timewarped Tavern 不应直接复用普通 `TavernState.Shop`，否则会破坏冻结、池副本、商店光环和刷新触发。

建议新增独立字段:

```csharp
public sealed class TavernState
{
    public TimewarpTavernState Timewarp;
}
```

普通酒馆与 Timewarped Tavern 的边界:

- 普通买随从: 消耗金币，影响普通随从池。
- Timewarped 买随从: 消耗 Chronum，不影响普通随从池。
- 普通刷新: 触发刷新相关效果。
- Timewarped 打开/退出: 不触发普通刷新效果。
- 普通商店光环: 默认不影响 Timewarped offer，除非文本明确影响所有酒馆中的随从。
- Timewarped 卡牌自身写了 Tavern/Refresh 时，按牌面单独接入。

## 和异常的关系

`Oathstone's Summoning` 的文本是:

```text
Minor Timewarped minions enter the Tavern pool on Turn 7,
and Major ones on Turn 10.
```

它不是第 6/9 回合的 Timewarped Tavern 访问，而是把 Timewarped minions 注入普通酒馆池。

因此实现上要拆开:

- `Timewarped Tavern Visit`: 第 6/9 回合特殊商店。
- `Oathstone Pool Injection`: 第 7/10 回合把 Minor/Major Timewarped minions 加入普通刷新池。

这两个机制共享卡牌数据源，但不共享商店状态。

## 和种族禁用的关系

第一版建议按当前禁用种族过滤:

- `NONE` 可用。
- `ALL` 可用。
- 多种族卡只要任一真实种族可用即可。
- 纯被禁种族卡不进入该局 Timewarped Tavern。

如果后续官方规则确认 Timewarped Tavern 不受禁种族影响，再改为独立规则。

## 和三连的关系

Timewarped 随从进入手牌后应参与正常三连检测，除非官方特别排除。

规则:

- 三连按 `DefinitionId` 判断。
- 购买生成的 Timewarped 随从可以和同名同定义 Timewarped 随从合成。
- 金色定义使用 `battlegroundsPremiumDbfId` / `battlegroundsNormalDbfId` 对应关系。
- 合成奖励是否给普通三连奖励，第一版建议沿用现有三连规则。

## 和保存/读档的关系

Timewarp 状态需要随 `MatchState` 保存:

- 当前 Chronum。
- 下一次额外 Chronum。
- 是否有待打开 Timewarp。
- 当前访问是否打开。
- 当前访问 offer 列表。
- 已购买/已移除 slot。

如果读档时缺少 Timewarp 字段:

- 回退为空状态。
- 不影响旧存档。

## 推荐状态机

```text
Idle
  -> DueThisTurn
  -> BlockedByTrinketChoice
  -> Open
  -> Closed
  -> Idle
```

状态含义:

- `Idle`: 没有待处理 Timewarp。
- `DueThisTurn`: 本回合应打开 Timewarp，但还没生成 offer。
- `BlockedByTrinketChoice`: 同回合饰品 pending，Timewarp 等待。
- `Open`: Timewarped Tavern 已打开，玩家可购买或退出。
- `Closed`: 本次访问完成，剩余 Chronum 保存。

关键转移:

- 回合开始到第 6/9 回合: `Idle -> DueThisTurn`。
- 如果饰品 pending: `DueThisTurn -> BlockedByTrinketChoice`。
- 饰品选择完成: `BlockedByTrinketChoice -> Open`。
- 没有饰品 pending: `DueThisTurn -> Open`。
- 玩家退出或买完后关闭: `Open -> Closed -> Idle`。

## 日志

每个关键动作写 recruit log:

- Timewarp due。
- Timewarp opened。
- Chronum gained。
- Offer generated。
- Card bought。
- Casts When Bought resolved。
- Timewarp exited。
- Chronum saved。
- Blocked by Trinket choice。
- Resumed after Trinket choice。

日志里保留 `source`，例如:

- `turn-6-minor`
- `turn-9-major`
- `anomaly:oathstone`
- `trinket-delayed`
- `debug`

## UI 要求

第一版 UI 至少需要:

- 当前 Chronum。
- Minor/Major 标题。
- 卡牌 cost。
- 退出按钮或 `Exit the Timewarped Tavern` 牌。
- 购买失败提示: Chronum 不足、手牌满、目标非法。
- 明确这是 Timewarped Tavern，不是普通酒馆。

如果使用现有酒馆格子:

- 视觉上要区分特殊商店。
- 不显示普通金币购买按钮。
- 不允许冻结普通商店槽位。

## 测试口径

必须覆盖:

- 第 6 回合打开 Minor。
- 第 9 回合打开 Major。
- 同回合饰品和 Timewarp: 先饰品，选完后再 Timewarp。
- 饰品 pending 未处理时 Timewarp 不覆盖 `PendingChoice`。
- Chronum 购买扣减。
- Chronum 未花完保存到下一次 Timewarp。
- 购买 Timewarped 随从进手牌，生成新实例 ID。
- 手牌满时购买失败且不扣 Chronum。
- 退出 Timewarp 不改变普通商店。
- Timewarped Tavern 打开/退出不触发普通刷新效果。
- 禁种族过滤。
- 历史额外池默认不进入当前池。
- 旧存档缺少 Timewarp 字段时不崩溃。

## 第一版实现范围

建议第一版只做可验证闭环:

1. Timewarp 状态模型。
2. 当前池 125 个随从的数据导入。
3. 第 6/9 回合打开特殊商店。
4. Chronum 余额、购买、退出、保存。
5. 同回合饰品优先队列。
6. 随从购买进手牌。
7. UI 显示和日志。
8. 自动测试覆盖时序和购买。

暂缓:

- 38 张 Timewarped spell/treasure 的完整效果。
- 33 张历史额外随从进入当前池。
- `Oathstone's Summoning` 注入普通酒馆池。
- 跨局 `last game` 类效果。
- 所有 Timewarped 随从的完整战斗/招募效果实现。

## 数据文件对应

- 当前/全量随从表: `timewarped-tavern-research.json`
- 逐随从机制清单: `timewarped-minion-mechanisms.json`
- 当前池图片: `images-current`
- 全量图片: `images-all`
- 历史额外图片: `images-historical-extra`
