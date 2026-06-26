# 饰品自动施法问题排查记录

## 目的

这份文档记录本次实现 `Pocket Cyclone` 时遇到的自动施法问题、测试失败原因和解决方法。下次实现“自动施放 Tavern spell / Spellcraft / 触发链”类饰品时，可以按这里的流程排查。

## 本次实现内容

实现了大小两个 `Pocket Cyclone`：

- `BG35_MagicItem_850`：小饰品 `Pocket Cyclone`
- `BG35_MagicItem_850t`：大饰品 `Pocket Cyclone`

`Pocket Cyclone` 施放的是固定 Tavern spell，不是随机 Tavern spell：

- 法术名：`Easterly Winds`
- 卡牌编号：`126909`
- 现有常量：`BorrowingEastWindCardNumber`

行为：

- 小饰品：装备时施放 1 次 `Easterly Winds`；每回合开始再施放 1 次。
- 大饰品：装备时施放 4 次 `Easterly Winds`；每回合开始再施放 2 次。

关键区别：

- `Lavish Cape` 是随机施放 Tavern spell，所以必须筛选 `TavernTier <= 当前酒馆等级`。
- `Pocket Cyclone` 是施放指定关联法术，所以不走随机池，也不套“当前酒馆等级及以下”的随机筛选。

## 涉及文件

`Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`

- 在装备饰品分发里增加 `case "pocket_cyclone"`。
- 新增 `ApplyPocketCyclone(TrinketDefinition definition, bool onEquip)`。
- 在 `DispatchTrinketTurnStarted()` 里处理回合开始触发。
- 新增固定 Tavern spell 多次施放入口：
  - `CastTavernSpellImmediate(string cardNumber, int count, string source, string instancePrefix)`
- 保留原有两参数入口的旧行为：
  - `CastTavernSpellImmediate(string cardNumber, string source)`

`Assets/LearnHearthstone/Resources/Data/battlegroundsTrinkets.json`

- 两个 `Pocket Cyclone` 都设置为：
  - `implementationStatus: "Implemented"`
  - `offerPoolStatus: "Offerable"`
  - `effectIds: ["pocket_cyclone"]`
  - `proxyLevel: "Exact"`

`Assets/LearnHearthstone/Tests/EditMode/TrinketSystemTests.cs`

- 更新饰品总数断言。
- 增加 `PocketCyclone_LesserAndGreaterCastEasterlyWindsOnEquipAndTurnStart`。
- 更新 `CoralSpear_SpellcraftCastsMightOfStormwindForEachActualCast` 的预期值。

## 安全自动施法模式

自动施放 Tavern spell 时，必须复用已有保护：

- `TryEnterAutomaticTavernSpellCast(source)`
- `ExitAutomaticTavernSpellCast()`
- `AutomaticTavernSpellCastMaxDepth`

固定施放 1 次时继续用旧入口：

```csharp
CastTavernSpellImmediate(cardNumber, source);
```

固定施放多次时用新入口：

```csharp
CastTavernSpellImmediate(cardNumber, count, source, instancePrefix);
```

不要绕过 `CastAutomaticTavernSpell`。它负责：

- 目标校验
- Tavern spell 加成
- 施法后的副作用
- 任务进度
- 饰品施法触发
- 英雄和随从施法触发
- 递归/触发链保护

## Coral Spear 测试失败

跑完整 `TrinketSystemTests` 时，曾出现这个失败：

```text
LearnHearthstone.Tests.EditMode.TrinketSystemTests.CoralSpear_SpellcraftCastsMightOfStormwindForEachActualCast
Expected: 8
But was:  10
```

根因：

- 测试场上有 `Maelstrom Naga`，卡牌编号 `BG34_922`。
- 测试里打出的 Spellcraft 也被设成了 Tavern spell。
- `Maelstrom Naga` 会让这个 Spellcraft Tavern spell 额外施放一次。
- `Coral Spear` 的文本是“每当你施放一个 Spellcraft spell，施放 Might of Stormwind”。
- 因此 `Coral Spear` 会按“实际 Spellcraft 施放次数”触发两次。
- `Coral Spear` 触发出的 `Might of Stormwind` 也走完整 Tavern spell resolver。
- 所以这个 `Might of Stormwind` 也会被 `Maelstrom Naga` 额外施放。

正确结果：

- `Reef Riffer` Spellcraft 因为 `Maelstrom Naga` 实际结算 2 次。
- `Coral Spear` 因为实际 Spellcraft 施放 2 次而触发 2 次。
- 每次触发出的 `Might of Stormwind` 又因为 `Maelstrom Naga` 结算 2 次。
- 前 4 个友方随从总共吃到 4 次 `Might of Stormwind` 的 `+1/+2`。

所以旧测试预期是过时的，不是 `Pocket Cyclone` 逻辑错误。解决方法是把断言改成包含 `Maelstrom Naga` 对 `Might of Stormwind` 的额外施放。

## Unity MCP 验证流程

改完脚本或数据后，先刷新并编译：

```text
refresh_unity:
  mode: force 或 if_dirty
  scope: all 或 scripts
  compile: request
  wait_for_ready: true
```

如果返回 `compiling`，继续等到 idle：

```text
refresh_unity:
  mode: if_dirty
  scope: scripts
  compile: none
  wait_for_ready: true
```

检查控制台：

```text
read_console:
  types: ["error", "warning"]
```

本次验证里出现过这些非项目错误/噪声：

- `MCP-FOR-UNITY: Client handler error: Cannot access a disposed object.`
- `Saving results to: ... TestResults.xml`
- Unity Performance Testing 的 `IPrebuildSetup` / `IPostBuildCleanup` warning

这些不是项目编译错误，也不是饰品测试失败。

先跑目标测试：

```text
LearnHearthstone.Tests.EditMode.TrinketSystemTests.Catalog_LoadsLesserAndGreaterTrinketsWithVisibleStatuses
LearnHearthstone.Tests.EditMode.TrinketSystemTests.PocketCyclone_LesserAndGreaterCastEasterlyWindsOnEquipAndTurnStart
LearnHearthstone.Tests.EditMode.TrinketSystemTests.LavishCape_OnEquipAndTurnStartCastsForEachDifferentFriendlyType
LearnHearthstone.Tests.EditMode.TrinketSystemTests.LavishCape_RandomCastsOnlyUseCurrentTavernTierOrLower
```

再跑完整饰品测试：

```text
LearnHearthstone.Tests.EditMode.TrinketSystemTests
```

本次最终结果：

```text
115 total
115 passed
0 failed
0 skipped
```

## 当前饰品计数

实现 `Pocket Cyclone` 后：

- 总饰品数：`330`
- 已实现：`127`
- 未实现：`203`
- 可发放：`126`

## 下次建议

建议下一步做 `Pagle's Fishing Rod`。

原因：

- 它也是回合开始类饰品，但比 discover 和种族专属逻辑简单。
- 文本是装备时获得随机 Tier 7 随从，之后每回合开始再获得一个。
- 可以顺手沉淀一个“按酒馆等级随机生成随从到手牌”的 helper。

建议 helper 要求：

- 按精确 Tavern tier 选随从。
- 尊重当前 active tribes 和 card pool。
- 生成的是 copy，不污染池。
- 处理手牌上限。
- 使用确定性 seed，方便测试。

暂时不要优先做：

- `Colorful Compass`：文本里仍有官方占位 `92`，语义不清楚。
- 大批 `tribe_specific`：数量多，而且混有战斗、酒馆、种族池和触发链逻辑。
