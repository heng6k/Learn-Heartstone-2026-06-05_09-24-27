# 酒馆种族 Ban 选机制改动文档

## 目标

在进入 Unity 酒馆模拟器时加入本局可用种族选择：

- 默认流程为 10 个随从类型中选择 5 个，未选择的 5 个类型不会进入本局酒馆池。
- 提供快捷按钮：
  - 随机 5 个
  - 不进行 ban 选，直接启用 10 个种族
- 酒馆法术同步本局可用种族。依赖某个种族的酒馆法术，只有该种族未被 ban 时才会出现在酒馆、随机法术、法术发现和工具卡牌库中。
- 工具中的卡牌库同步本局种族池，并为酒馆法术新增专属种族分类标签。

本改动的目标不是只挡初始酒馆，而是把所有“从池中随机/发现/展示”的路径统一到同一套种族可用性规则上。

## 当前代码现状

入口：

- `LearnHearthstoneBootstrap.Awake()` 当前会立即创建 `MatchService.CreateWithDefaultCatalog()`。
- `MainHubView` 点击主入口后调用 `ShowUnityTrainer()`。
- `UnityTavernTrainerView` 只是拿现有 `MatchService` 构建 Unity 酒馆界面。

酒馆池：

- 初始酒馆在 `MatchService.CreateMatch()` 中通过 `CreateShopFromPool(...)` 创建。
- 刷新走 `RerollShop()`，下一回合未冻结时走 `NextTurn()`，二者也调用 `CreateShopFromPool(...)`。
- `CreateShopFromPool(...)` 当前使用 `new MinionPool(catalog.All, snapshot)`，没有种族过滤。
- `DrawTavernSpell(...)` 当前使用 `spellCatalog.GetTavernSpellsForTier(tier)`，没有种族过滤。

卡牌库：

- `UnityTavernTrainerController` 的工具卡牌库和大卡牌库共用 `FilteredToolsAcquisitionChoices()`。
- 随从已有种族筛选按钮。
- 酒馆法术当前只有“全部/酒馆法术”两个伪分类，没有法术种族标签。

## 术语

- 可用种族：本局选择启用的 5 个或快捷启用的 10 个随从类型。
- 被 ban 种族：10 个随从类型中未启用的类型。
- 中立随从：无种族或只有 `Tribe.None` 的随从。中立不参与 10 选 5，始终可出现。
- 全部种族随从：含 `Tribe.All` 的随从，视为属于所有种族。只要本局至少有一个可用种族，它就可出现。
- 多种族随从：只要任意一个实际种族在可用种族中，就可出现。

可选的 10 个种族固定为：

1. 野兽 `Tribe.Beast`
2. 鱼人 `Tribe.Murloc`
3. 机械 `Tribe.Mech`
4. 恶魔 `Tribe.Demon`
5. 龙 `Tribe.Dragon`
6. 海盗 `Tribe.Pirate`
7. 元素 `Tribe.Elemental`
8. 野猪人 `Tribe.Quilboar`
9. 亡灵 `Tribe.Undead`
10. 纳迦 `Tribe.Naga`

`Tribe.All` 和 `Tribe.None` 不作为 ban 选按钮出现。

## 推荐实现结构

### 1. 新增统一规则类

新增文件：

`Assets/LearnHearthstone/Runtime/Domain/Engine/TribeAvailabilityRules.cs`

职责：

- 提供固定的 `PlayableTribes` 顺序。
- 标准化可用种族列表。
- 判断随从是否可在本局池中出现。
- 判断酒馆法术是否可在本局池中出现。
- 提供酒馆法术的种族标签，供工具卡牌库 UI 使用。

建议 API：

```csharp
public static class TribeAvailabilityRules
{
    public static readonly Tribe[] PlayableTribes = { ... };

    public static List<Tribe> AllPlayableTribes();
    public static List<Tribe> Normalize(IEnumerable<Tribe> tribes);
    public static bool IsTribeActive(IReadOnlyCollection<Tribe> activeTribes, Tribe tribe);
    public static bool IsMinionAvailable(MinionDefinition minion, IReadOnlyCollection<Tribe> activeTribes);
    public static bool IsTavernSpellAvailable(TavernSpellDefinition spell, IReadOnlyCollection<Tribe> activeTribes);
    public static IReadOnlyList<Tribe> SpellTribes(TavernSpellDefinition spell);
}
```

核心判断：

- `Tribe.None`：始终可用。
- `Tribe.All`：始终可用。
- 普通随从：`minion.Tribes.Any(activeTribes.Contains)`。
- 多种族随从：任一实际种族可用即可。
- 法术：无映射种族则视为通用；有映射种族则任一映射种族可用即可。

### 2. MatchService 支持创建时传入可用种族

新增配置模型，建议放在 `Domain/Models/TavernMatchModels.cs`：

```csharp
[Serializable]
public sealed class MatchSetupOptions
{
    public List<Tribe> ActiveTribes = new List<Tribe>();
}
```

给 `MatchState` 或 `TavernState` 增加字段：

```csharp
public List<Tribe> ActiveTribes = new List<Tribe>();
```

推荐放在 `MatchState`，因为这是整局对战配置，不只是玩家酒馆的临时状态。

`MatchService` 增加重载：

```csharp
public static MatchService CreateWithDefaultCatalog(
    int seed = 12345,
    ITestScenarioRepository scenarios = null,
    MatchSetupOptions setup = null)
```

兼容要求：

- 现有测试和旧入口不传 `setup` 时，默认启用 10 个种族。
- Unity 新入口传入 5 个或 10 个可用种族。

内部保存：

- `MatchService` 构造时保存 `activeTribes`。
- `CreateMatch(seed)` 初始化 `State.ActiveTribes`。
- `CreateShopFromPool(...)`、发现、随机生成逻辑都从 `State.ActiveTribes` 或服务字段读取。

### 3. 酒馆随从池过滤

`MinionPool` 建议支持候选过滤，而不是在每次抽取后丢弃：

方案 A，推荐采用：

```csharp
public MinionPool(IEnumerable<MinionDefinition> definitions, IDictionary<string, int> initial = null, IReadOnlyCollection<Tribe> activeTribes = null)
```

构造时只把可用随从加入 `definitions` 和 `counts`。

方案 B：

新增 `DrawShop(int tier, int size, SeededRng rng, Func<MinionDefinition, bool> predicate)`。

推荐方案 A，因为 `Release(...)`、`Snapshot()` 与池计数自然保持一致，被 ban 的随从不会进入池也不会被释放回池。

需要改动：

- `CreateShopFromPool(...)`
- `ReleaseShopToPool()`
- 所有临时创建 `new MinionPool(catalog.All, ...)` 的地方都要传同一套 active tribes。

### 4. 随机随从、发现、三连奖励同步过滤

以下路径都需要接入 `TribeAvailabilityRules.IsMinionAvailable(...)`：

- `CreateTripleDiscover()`
- `StartTierDiscover(...)`
- `StartTribeDiscover(...)`
- `StartScrapperMagneticDiscover(...)`
- `StartReadyDoomsdayDragonEggDiscover()`
- `AddRandomTierOneMinionsToHand(...)`
- `AddRandomTierMinionsToHand(...)`
- `AddRandomTierOneNagaToHand(...)`
- `AddRandomBattlecryMinionToHand(...)`
- `AddRandomTribeMinionToHand(...)`
- `AddRandomMagneticMechToHand(...)`

原则：

- 从正式随从目录随机/发现的候选，必须过滤 active tribes。
- 通过明确卡号生成的 token、临时衍生牌、法术制造的固定牌，可以不受 ban 选限制。
- 若某个指定种族已被 ban，例如 Naga 被 ban 后仍触发 `AddRandomTierOneNagaToHand`，候选应为空，不生成牌，不抛异常。

### 5. 酒馆法术过滤

以下路径都需要接入 `TribeAvailabilityRules.IsTavernSpellAvailable(...)`：

- `DrawTavernSpell(...)`
- `AddRandomTavernSpellToHand(...)`
- `AddRandomTavernSpellToHandByCost(...)`
- `StartTavernSpellDiscover(...)`
- 工具卡牌库中的 `BuildToolsAcquisitionSpellChoices()`

不建议在 `SpellCatalog.GetTavernSpellsForTier(...)` 中直接硬编码当前 active tribes，因为 `SpellCatalog` 是静态目录对象，不应该持有对局状态。

建议在 `MatchService` 中新增 helper，并在本次实现中落地：

```csharp
private IEnumerable<TavernSpellDefinition> AvailableTavernSpells()
{
    return spellCatalog.All.Where(spell =>
        spell.InPool &&
        spell.Category == "TavernSpell" &&
        TribeAvailabilityRules.IsTavernSpellAvailable(spell, ActiveTribes));
}
```

然后所有随机/发现法术路径都基于这个 helper。

## 酒馆法术种族映射表

实现时不要只依赖 `faction` 字段，因为当前数据里有一些官方补充法术是 `faction = 中立`，但用户要求它们按指定种族 ban 选。

建议以 `cardNumber` 为主键建立显式映射，同时允许 `id` 作为兼容键。

### 野猪人

| 法术 | cardNumber | 说明 |
| --- | --- | --- |
| Blood Gem Barrage / 鲜血宝石弹幕 | `126676` | 只有野猪人可用时出现 |
| Gem Confiscation / 查抄宝石 | `110642` | 只有野猪人可用时出现 |

### 海盗

| 法术 | cardNumber | 说明 |
| --- | --- | --- |
| Healthy Bounty | `122182` | 只有海盗可用时出现 |
| Hostile Bounty | `122183` | 只有海盗可用时出现 |
| Selfish Bounty | `122184` | 只有海盗可用时出现 |
| Friendly Bounty | `122185` | 只有海盗可用时出现 |
| Wealthy Bounty | `122186` | 只有海盗可用时出现 |
| Wave of Gold | `127506` | 只有海盗可用时出现 |

### 鱼人

| 法术 | cardNumber | 说明 |
| --- | --- | --- |
| Cloning Conch / 克隆螺号 | `110400` | 只有鱼人可用时出现 |
| Deepwater Clan | `131218` | 只有鱼人可用时出现 |

### 元素

| 法术 | cardNumber | 说明 |
| --- | --- | --- |
| Mounting Avalanche | `122862` | 只有元素可用时出现 |
| Arcane Absorption | `130311` | 只有元素可用时出现 |
| Conflagration | `130310` | 只有元素可用时出现 |
| Easterly Winds | `126909` | 只有元素可用时出现 |
| Temperature Shift | `117670` | 只有元素可用时出现 |

### 纳迦

| 法术 | cardNumber | 说明 |
| --- | --- | --- |
| Shifting Tide | `120900` | 只有纳迦可用时出现 |
| Spitescale Special | `110406` | 只有纳迦可用时出现 |
| Queen's Command | `130713` | 只有纳迦可用时出现 |

注意：纳迦本次确认只覆盖以上 3 张，之前“5 个”的说法是口误。

### 亡灵

| 法术 | cardNumber | 说明 |
| --- | --- | --- |
| Tomb Turning | `126957` | 只有亡灵可用时出现 |
| Butchering | `110412` | 只有亡灵可用时出现 |
| Haunted Carapace | `122489` | 只有亡灵可用时出现 |

### 恶魔

| 法术 | cardNumber | 说明 |
| --- | --- | --- |
| Corrupted Cupcakes | `110407` | 只有恶魔可用时出现 |

### 龙

| 法术 | cardNumber | 说明 |
| --- | --- | --- |
| Brood of Nozdormu | `127503` | 只有龙可用时出现 |

### 野兽

| 法术 | cardNumber | 说明 |
| --- | --- | --- |
| Raptor's Revenge / 迅猛龙的复仇 | `123553` | 只有野兽可用时出现 |

### 机械

| 法术 | cardNumber | 说明 |
| --- | --- | --- |
| Sanctify | `122899` | 只有机械可用时出现 |

### 数据审计项

当前数据文件中还有带非中立 `faction` 的酒馆法术，例如：

- `Haunted Carapace` / `122489`，数据标为亡灵，但不在本次需求列表中。
- `Wave of Gold` / `127506`，数据标为海盗，但不在本次需求列表中。

已确认这两张也加入 ban 选映射表。实现时仍采用“显式映射优先，数据 faction 可作为补充”的策略，避免后续新增卡牌时漏掉明显带种族的酒馆法术。

## 进入酒馆的 UI 流程

新增 Unity UI 视图：

`UnityTavernTribeSelectionView` 或 `UnityTavernTribeSelectionModalComponent`

推荐做成进入酒馆前的全屏选择页，而不是酒馆内弹窗：

1. 用户在 `MainHubView` 点击酒馆训练器。
2. 进入种族选择页。
3. 用户选择 5 个种族，或点击快捷按钮。
4. 创建新的 `MatchService`，传入 active tribes。
5. 构建 `UnityTavernTrainerView`。

按钮状态：

- 10 个种族按钮，点击切换选中。
- 手动模式最多选 5 个。
- 选中不足 5 个时，“进入酒馆”按钮禁用。
- 选中 5 个时，“进入酒馆”按钮启用。
- “随机5个”立即随机选 5 个，可直接进入，或显示结果后让用户确认。
- “全部10个种族”直接进入。
- “返回”回到 MainHub。

为了符合现有 UI 风格，复用：

- `UnityTavernUiStyle`
- `UiFactory`
- `ActionButton` 类似的按钮样式
- `TribeName(...)`
- `TribeAccent(...)`

需要注意：

- 当前 `matchService` 在 `LearnHearthstoneBootstrap.Awake()` 中已经创建。实现时应把 Unity 酒馆的 `MatchService` 创建延后到选择完成后。
- Legacy/Realistic trainer 可以继续使用默认 10 种族，或也接入选择流程。当前需求指向 Unity 酒馆模拟器，优先只改 Unity 主入口。
- 从酒馆返回大厅后再次进入，应重新显示选择页并创建新对局，避免上一局的 ban 选状态泄漏。

## 工具卡牌库同步

### 随从卡牌库

补充修正：`布莱恩·铜须` / `BG_LOE_077` 属于中立随从，卡牌库的中立筛选必须能显示它。

工具卡牌库随从页应同步当前 active tribes：

- 只显示本局可用随从。
- 中立随从始终显示。
- 全部种族随从始终显示。
- 多种族随从只要命中任一 active tribe 就显示。
- 被 ban 的种族标签不应显示为可点筛选项，或显示为禁用状态。推荐直接不显示，减少误解。
- “全部”表示当前本局可用的全部，不是全数据库。
- 新增“显示全部”开关：
  - 关闭时只显示当前 ban 选可用的卡牌。
  - 开启时显示全数据库调试卡牌，忽略本局 active tribes 过滤。
  - 直接指定 id 的调试命令仍不被 ban 选硬拦。

### 酒馆法术卡牌库

法术页新增专属种族分类标签：

- 全部
- 通用法术
- 野兽
- 鱼人
- 机械
- 恶魔
- 龙
- 海盗
- 元素
- 野猪人
- 亡灵
- 纳迦

显示规则：

- 通用法术：没有 `SpellTribes` 映射的法术。
- 种族法术：使用 `TribeAvailabilityRules.SpellTribes(spell)` 返回的标签。
- 标签按钮只显示 active tribes 加“全部/通用法术”。
- 快捷“全部10个种族”时，显示全部种族法术标签。
- 如果某个 active tribe 当前没有可用法术，可以隐藏该标签或置灰。推荐隐藏，保持界面密度。

同一套过滤逻辑要作用于：

- 工具侧边栏里的卡牌库列表。
- 大卡牌库 overlay。
- “加对手”入口复用的大卡牌库。

## 命令和调试行为

保持现有显式调试命令可用：

- `GameCommandType.AddCardToHand`
- `GameCommandType.DebugCastCard`
- `GameCommandType.AddOpponentMinion`

原因：

- 这些命令是直接指定 card id 的调试工具。
- 工具 UI 会隐藏被 ban 的卡，但测试或调试代码直接传 id 时不应被 ban 选硬拦。

如果后续希望“任何方式都不能加入被 ban 卡”，可以再增加一个严格模式，但本次不建议默认启用。

## 兼容性和迁移

默认兼容：

- 不传 `MatchSetupOptions` 时启用 10 个种族。
- 现有 EditMode 测试应继续通过。
- 现有 Legacy/Realistic 入口继续可用。

场景保存：

- 如果测试场景保存/读取包含 `MatchState`，需要确认是否序列化 `ActiveTribes`。
- 加载旧场景缺少 `ActiveTribes` 时，应默认补全 10 个种族。

脏数据处理：

- 若 active tribes 为空或 null，按 10 个种族处理，避免旧入口直接空池。
- 若手动选择超过 5 个，只在 UI 阶段禁止；服务层标准化时去重并保留合法种族。

## 测试计划

### Domain / Service

新增测试文件建议：

`Assets/LearnHearthstone/Tests/EditMode/TribeAvailabilityRulesTests.cs`

覆盖：

- 中立随从始终可用。
- `Tribe.All` 随从在任意 active tribes 下可用。
- 单种族随从在对应种族被 ban 时不可用。
- 多种族随从只要任一族可用就可用。
- 指定法术的 `SpellTribes` 映射正确。
- 通用法术不受 ban 选影响。

扩展 `MatchService` 测试：

- 只启用 5 个种族时，初始酒馆不包含被 ban 种族随从。
- 刷新多次后仍不出现被 ban 种族随从。
- 下一回合生成酒馆仍不出现被 ban 种族随从。
- 三连发现不出现被 ban 种族随从。
- 指定种族随机生成在该族被 ban 时不生成候选。
- 酒馆法术抽取不出现被 ban 种族关联法术。
- `AddRandomTavernSpellToHand` 和 `StartTavernSpellDiscover` 不出现被 ban 种族关联法术。
- 默认不传设置时仍可出现 10 个种族。

### UI

扩展 `UnityTavernTrainerViewTests`：

- 点击主入口后先出现种族选择页，而不是直接进入酒馆。
- 手动选择不足 5 个时进入按钮禁用。
- 手动选择 5 个后进入按钮启用，并进入 Unity 酒馆。
- “随机5个”选中数量为 5。
- “全部10个种族”进入后 `ActiveTribes.Count == 10`。
- 酒馆界面展示当前可用种族摘要。
- 工具卡牌库随从页不显示被 ban 种族随从。
- 工具卡牌库法术页显示法术种族标签，并且被 ban 种族的法术不显示。
- “加对手”大卡牌库同步同样过滤。

### 回归

- 现有购买、出售、刷新、升级、战斗、回放、工具、加对手功能仍通过编译和现有测试。
- `DebugCastCard` 直接指定法术时仍可运行。
- 旧场景加载不因缺少 `ActiveTribes` 崩溃。

## 实施步骤

1. 新增 `TribeAvailabilityRules` 和对应单元测试。
2. 给 `MatchState` 或 `TavernState` 增加 `ActiveTribes`，给 `MatchService` 增加 setup 参数和默认 10 种族兼容。
3. 改 `MinionPool` 或 `CreateShopFromPool`，让酒馆随从池按 active tribes 构建。
4. 改所有随机/发现随从候选查询，统一调用可用性 helper。
5. 改所有随机/发现/酒馆抽取法术候选查询，统一调用法术可用性 helper。
6. 新增进入酒馆前的种族选择 UI，并把 Bootstrap 的 Unity 酒馆入口改为选择完成后创建新 `MatchService`。
7. 改工具卡牌库和大卡牌库：
   - 随从页同步 active tribes。
   - 法术页新增种族标签。
   - 过滤逻辑复用 `TribeAvailabilityRules`。
8. 补 UI 测试和 MatchService 测试。
9. 跑 Roslyn 编译检查和 Unity EditMode 测试。

## 验收标准

- 手动 10 选 5 后进入酒馆，未选的 5 个种族随从不会在初始酒馆、刷新、下一回合、三连发现、随机发现/获取中出现。
- 中立和全部种族随从的行为符合定义。
- “随机5个”和“全部10个种族”快捷入口可用。
- 用户列出的种族限定酒馆法术全部按 active tribes 出现或隐藏。
- 工具卡牌库与大卡牌库同步当前对局 active tribes，法术页有种族分类标签。
- 默认旧入口和旧测试不传 active tribes 时仍等同 10 个种族全开。

## 需要确认的点

1. “显示全部”开关的默认状态建议为关闭，即默认只显示当前 ban 选可用卡牌。
2. 如果后续新增酒馆法术的 `faction` 非中立，但没有显式映射，需要在数据审计或测试中提示补映射，避免静默漏过。
