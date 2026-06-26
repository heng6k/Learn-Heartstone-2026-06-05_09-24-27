# 饰品系统实现文档

## 目标

实现单人酒馆训练器中的饰品系统，让后续可以支持管理者马林（Marin the Manager）、巴顿（Buttons）以及相关宝宝从代理奖励升级为真实饰品奖励。

饰品系统第一版重点是可玩和可测试：

- 支持小饰品和大饰品槽位。
- 支持在指定回合或配置时机发现/购买饰品。
- 支持饰品带来被动、回合触发、买卖触发、刷新触发和奖励替换。
- 支持 UI 展示、选择、购买、日志和已知限制。

## 依赖

必须先完成或至少部分完成：

- [SharedAdvancedMechanicsFoundationImplementationPlan.md](SharedAdvancedMechanicsFoundationImplementationPlan.md)

饰品需要复用其中的：

- `MechanicChoiceRequest`
- `MechanicRewardDefinition`
- `AdvancedMechanicEngine`
- `AdvancedMechanicState`
- 注册表状态

## 第一版范围

### 包含

- `Lesser` 和 `Greater` 两种饰品槽位。
- 每个槽位最多装备一个饰品。
- 饰品候选生成。
- 饰品购买或选择命令。
- 饰品效果注册和触发。
- Marin/Buttons 相关最小可玩路径。

### 不包含

- 完整官方饰品池。
- 所有饰品的复杂专属 UI。
- 多人大厅饰品平衡。
- 账号收藏、图鉴和线上分享。

## 数据模型

建议新增：

- `Assets/LearnHearthstone/Runtime/Domain/Models/TrinketModels.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Data/TrinketCatalog.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/TrinketEffectEngine.cs`

核心模型：

```csharp
public enum TrinketSlotKind
{
    Lesser,
    Greater
}

public enum TrinketOfferTiming
{
    ConfiguredTurn,
    HeroPowerTriggered,
    DebugCommand
}

public sealed class TrinketDefinition
{
    public string Id;
    public string CardId;
    public string Name;
    public TrinketSlotKind SlotKind;
    public int Cost;
    public string Text;
    public List<string> Tags = new List<string>();
    public List<MechanicRewardEffect> Effects = new List<MechanicRewardEffect>();
}

public sealed class PlayerTrinketState
{
    public string LesserTrinketId;
    public string GreaterTrinketId;
    public List<string> OfferedTrinketIds = new List<string>();
    public Dictionary<string, int> Counters = new Dictionary<string, int>();
}
```

第一版不要把官方出现概率和完整过滤规则塞进模型。先用 `Tags` 和简单条件表达：

- 需要某种种族。
- 需要某种卡牌类型。
- 需要某个已有机制。
- 当前版本暂不支持。

## 运行时设计

### 触发点

`MatchService` 在以下点调用饰品引擎：

- `MatchStarted`
- `TurnStarted`
- `TurnEnded`
- `CardBought`
- `CardPlayed`
- `MinionSold`
- `TavernSpellCast`
- `ShopRefreshed`
- `CombatStarted`
- `CombatEnded`

`TrinketEffectEngine` 不直接控制金币和牌库。需要改金币、手牌、酒馆、棋盘时，通过 `MatchService` 暴露的受控回调或现有 helper 完成。

### 饰品获取流程

推荐流程：

1. 到达配置时机。
2. 生成候选饰品。
3. 写入 `MechanicChoiceRequest`。
4. UI 弹出候选。
5. 玩家选择并支付费用。
6. 写入 `PlayerTrinketState` 对应槽位。
7. 招募日志记录：获得了什么饰品、花费多少、是否为代理。

不要在文档中把官方回合写死为唯一规则。第一版建议用配置字段：

```csharp
public int LesserTrinketOfferTurn;
public int GreaterTrinketOfferTurn;
```

这样以后方便切换版本或做调试场景。

## UI 设计

### 入口

在局内状态区域增加饰品槽显示：

- 小饰品槽。
- 大饰品槽。
- 未获得时显示空槽。
- 已获得时显示名称、费用历史和简短效果。

### 选择弹窗

使用共用选择弹窗：

- 每个候选显示名称、费用、效果文本。
- 费用不足时禁用选择按钮。
- 暂不支持的候选不进入候选池。
- 代理实现的候选可以进入，但需要在说明或日志中标记。

### 调试支持

为了测试，建议增加调试入口：

- 立即触发小饰品选择。
- 立即触发大饰品选择。
- 清除当前饰品。

这些调试入口只用于编辑器或测试，不进入正式用户主流程。

## MVP 饰品池

第一批只做低风险饰品，不追求数量。

建议分类：

1. **经济类**
   - 回合开始获得金币。
   - 刷新费用降低。
   - 购买某类型卡牌返还金币。

2. **属性类**
   - 回合结束给随机友方随从属性。
   - 买入特定类型后给属性。

3. **酒馆类**
   - 刷新时额外注入某类型随从。
   - 酒馆随从获得临时增益。

4. **奖励类**
   - 周期性给 Tavern Coin。
   - 周期性给随机 Tavern spell。

暂缓：

- 需要复杂官方智能选择的饰品。
- 需要战斗中实时改写胜负的饰品。
- 需要多人大厅、血量排行或对手历史的饰品。

## 关联英雄和宝宝

### Marin the Manager

当前缺口：英雄饰品系统缺失，宝宝只能给 helpful card。

第一版行为：

- Marin 在指定时机触发饰品选择。
- 小饰品和大饰品都走 `PlayerTrinketState`。
- Fantastic Bellhop 的回合结束奖励改为从可用饰品奖励池中给一张可见奖励，而不是模糊 helpful card。

### Buttons

当前缺口：Greater Trinket 系统缺失。

第一版行为：

- Buttons 可在配置时机选择大饰品。
- Zippers 的真实亡语已能触发奖励时，优先给饰品相关奖励或进入大饰品候选。
- 如果战斗内亡语仍无法即时影响，日志明确写“战斗后回写”。

## 测试计划

新增测试建议：

- `TrinketCatalogTests`
  - 没有 duplicate id。
  - 所有候选都有槽位和费用。
  - 暂不支持饰品不会进入默认候选。

- `TrinketOfferTests`
  - 到指定回合生成小饰品候选。
  - 到指定回合生成大饰品候选。
  - 已拥有槽位后不重复弹出同槽位候选。

- `TrinketPurchaseTests`
  - 金币足够时购买成功。
  - 金币不足时购买失败。
  - 购买后写入状态并记录日志。

- `TrinketEffectTests`
  - 回合开始效果触发。
  - 回合结束效果触发。
  - 买牌触发效果触发。
  - 饰品不在槽位时不触发。

- `MarinButtonsTrinketTests`
  - Marin 能触发饰品选择。
  - Buttons 能获得大饰品。
  - 宝宝奖励不再只写不可解释的 helpful card。

## 实施顺序

1. 建立 `TrinketDefinition`、`TrinketCatalog`、`PlayerTrinketState`。
2. 把 `PlayerTrinketState` 挂入 `AdvancedMechanicState` 或 `TavernState`。
3. 做小饰品/大饰品槽 UI。
4. 做饰品候选生成和购买命令。
5. 实现 5 到 8 个低风险饰品。
6. 接 Marin。
7. 接 Buttons。
8. 补日志、注册表和已知限制。
9. 跑目录、购买、效果、英雄专项测试。

## 完成标准

- 玩家能在局内获得并看到小饰品和大饰品。
- 至少 5 个饰品有可玩效果。
- Marin 和 Buttons 不再只是纯代理状态。
- 饰品效果只在装备后触发。
- 所有饰品定义都有注册状态。
- 自动测试覆盖候选、购买、触发、金币不足和宝宝联动。

## 风险

| 风险 | 处理 |
| --- | --- |
| 饰品池过大导致长期填坑。 | 第一版只做少量低风险饰品，其余注册为 Planned 或 Deferred。 |
| 饰品效果和英雄/宝宝效果触发顺序冲突。 | 在共用底座中固定事件顺序，并在日志中显示触发来源。 |
| UI 信息过多。 | 槽位只显示摘要，详情放悬停或点击弹窗。 |
| 官方规则版本变化。 | 饰品数据带版本字段，不直接写死在引擎里。 |
