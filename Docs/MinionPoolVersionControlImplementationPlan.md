# 卡池版本控制实现文档

## 目标

在进入 Unity 酒馆训练器前的入口页右侧增加“卡池版本控制”区域，用来控制本局可用的随从和酒馆法术。

核心能力：
- 当前版本默认为“默认版本”，默认版本从现有卡牌目录的 `InPool` 字段派生，只读，不写回数据源。
- 用户可以新建、复制、重命名、删除有限数量的自定义版本。
- 每个自定义版本可以通过勾选随从和酒馆法术，决定它们是否进入本局卡池。
- 进入酒馆时，当前选择的版本会和种族选择一起传入 `MatchService`，影响商店刷新、发现、随机生成和工具卡牌库。
- 自定义版本保存到本地 JSON，不修改 `battlegroundsMinions.json` 或 `battlegroundsSpells.json`。

第一版建议最多保存 10 个自定义版本。默认版本不计入 10 个上限。

## 当前代码现状

入口页：
- `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernTribeSelectionView.cs` 当前负责进入酒馆前的种族选择。
- 当前页面主体是一个居中的种族选择面板，构建顺序为 `BuildHeader()`、`BuildTribeGrid()`、`BuildQuickActions()`。
- 进入酒馆按钮最终调用 `start?.Invoke(activeTribes)`，只传入当前可用种族。

开局配置：
- `Assets/LearnHearthstone/Runtime/Domain/Models/TavernMatchModels.cs` 中的 `MatchSetupOptions` 目前只有 `ActiveTribes` 和 `SelectedHeroCardId`。
- `MatchService.CreateWithDefaultCatalog(..., MatchSetupOptions setup = null)` 已支持接收开局配置。
- `MatchState` 已持久化 `ActiveTribes`，还没有记录本局使用的卡池版本信息。

随从池：
- `MinionCatalog.GetMinionsForTier()` 直接过滤 `minion.InPool`。
- `MinionPool` 构造时会按 `TribeAvailabilityRules.IsMinionAvailable(...)` 过滤种族，再按 `definition.InPool` 建立池计数。
- `MinionPool.DrawShop()` 当前要求 `definition.InPool && definition.TavernTier <= tier && Remaining(definition.Id) > 0`。
- `MatchService` 内部大量候选生成仍直接写 `minion.InPool`，例如三连发现、随机加手牌、指定种族生成等。

酒馆法术池：
- `SpellCatalog.GetTavernSpellsForTier()` 直接过滤 `spell.InPool && spell.Category == "TavernSpell"`。
- `MatchService.AvailableTavernSpells()` 已集中处理 `spell.InPool`、`Category == "TavernSpell"` 和种族可用性。
- 随机法术、发现法术、商店法术抽取应继续统一走 `AvailableTavernSpells()`。

持久化：
- `Assets/LearnHearthstone/Runtime/Adapters/Persistence/SaveRepositories.cs` 已有 `JsonSaveRepository`，使用 `Application.persistentDataPath` 保存本地 JSON。
- 卡池版本应新增独立 repository，沿用本地 JSON 思路，但不要和对局存档混在一个文件中。

## 术语

- 默认版本：从目录中 `InPool == true` 的随从和酒馆法术动态派生的只读版本。
- 自定义版本：用户保存的卡池配置，包含明确启用的随从 `CardId` 和酒馆法术 `CardNumber`。
- 当前版本：入口页右侧当前选中的卡池版本。未选择自定义版本时为默认版本。
- 可入池：卡牌同时满足目录 `InPool`、当前版本启用、当前种族可用、类型正确等条件。
- 孤儿引用：自定义版本里保存的卡牌键在当前目录中找不到，通常由数据更新或卡牌移除造成。

## 数据模型设计

新增模型建议放在 `Assets/LearnHearthstone/Runtime/Domain/Models/CardPoolVersionModels.cs`。

```csharp
[Serializable]
public sealed class CardPoolVersionProfile
{
    public string Id;
    public string Name;
    public long CreatedAtUnixSeconds;
    public long UpdatedAtUnixSeconds;
    public List<string> EnabledMinionCardIds = new List<string>();
    public List<string> EnabledTavernSpellCardNumbers = new List<string>();
}

[Serializable]
public sealed class CardPoolVersionStore
{
    public string SelectedVersionId;
    public List<CardPoolVersionProfile> Versions = new List<CardPoolVersionProfile>();
}

public sealed class CardPoolVersionSelection
{
    public string VersionId;
    public string VersionName;
    public bool IsDefault;
    public HashSet<string> EnabledMinionCardIds;
    public HashSet<string> EnabledTavernSpellCardNumbers;
}
```

键选择：
- 随从保存 `MinionDefinition.CardId`，因为它比运行时 `Id` 更接近卡牌身份，也和图片路径、官方数据更一致。
- 法术保存 `TavernSpellDefinition.CardNumber`，这是当前法术目录中稳定的卡牌编号。
- 运行时 `MinionPool` 仍使用 `MinionDefinition.Id` 管理池计数，不需要把池计数键改成 `CardId`。

默认版本生成规则：
- `EnabledMinionCardIds = catalog.All.Where(m => m.InPool).Select(m => m.CardId)`。
- `EnabledTavernSpellCardNumbers = spellCatalog.All.Where(s => s.InPool && s.Category == "TavernSpell").Select(s => s.CardNumber)`。
- 默认版本不保存到 JSON，目录更新后自然变化。

自定义版本规则：
- 保存明确启用列表，不保存禁用列表。
- 新建版本时默认复制当前版本，这样用户从“默认版本”开始微调最顺手。
- 复制版本时生成新 `Id`，名称追加“副本”或序号。
- 保存前去重、移除空字符串，并按目录显示顺序排序，减少 JSON diff 噪音。
- 如果目录中找不到旧引用，保留在 JSON 中但 UI 显示“缺失 N 张”，提供清理按钮。

## 持久化设计

新增 repository 建议放在 `Assets/LearnHearthstone/Runtime/Adapters/Persistence/CardPoolVersionRepository.cs`。

建议接口：

```csharp
public interface ICardPoolVersionRepository
{
    CardPoolVersionStore Load();
    void Save(CardPoolVersionStore store);
}
```

默认文件：
- `Application.persistentDataPath/card-pool-versions.json`

加载行为：
- 文件不存在时返回空 store，`SelectedVersionId` 为空，UI 使用默认版本。
- JSON 解析失败时不要阻塞进酒馆，回退默认版本，并在日志中提示。
- `SelectedVersionId` 指向不存在版本时回退默认版本。

保存行为：
- 自定义版本最多 10 个。
- 超过上限时“新建”和“复制”按钮禁用。
- 删除当前选中版本后回到默认版本。
- 默认版本不可删除、不可重命名、不可直接保存；用户编辑默认版本时应先“另存为版本”。

## 运行时过滤设计

新增统一规则类建议放在 `Assets/LearnHearthstone/Runtime/Domain/Engine/CardPoolAvailabilityRules.cs`。

```csharp
public sealed class CardPoolAvailability
{
    public bool AllowsMinion(MinionDefinition minion);
    public bool AllowsTavernSpell(TavernSpellDefinition spell);
}
```

构造输入：
- 当前版本选择 `CardPoolVersionSelection`。
- 当前目录的默认派生集合。

判断规则：
- 默认版本：仍以目录 `InPool` 为准。
- 自定义版本随从：`minion.InPool && EnabledMinionCardIds.Contains(minion.CardId)`。
- 自定义版本法术：`spell.InPool && spell.Category == "TavernSpell" && EnabledTavernSpellCardNumbers.Contains(spell.CardNumber)`。
- 种族过滤继续交给 `TribeAvailabilityRules`，不要混进版本模型。

`MatchSetupOptions` 扩展：

```csharp
[Serializable]
public sealed class MatchSetupOptions
{
    public List<Tribe> ActiveTribes = new List<Tribe>();
    public string SelectedHeroCardId;
    public string CardPoolVersionId;
    public string CardPoolVersionName;
    public bool IsDefaultCardPoolVersion = true;
    public List<string> EnabledMinionCardIds = new List<string>();
    public List<string> EnabledTavernSpellCardNumbers = new List<string>();
}
```

`MatchState` 建议新增：

```csharp
public string CardPoolVersionId;
public string CardPoolVersionName;
public bool IsDefaultCardPoolVersion;
public List<string> EnabledMinionCardIds = new List<string>();
public List<string> EnabledTavernSpellCardNumbers = new List<string>();
```

这样本局开始后，即使用户后来在入口页修改了版本，已开始的对局也能使用开局快照，行为更可控。

## MatchService 接入点

`MatchService` 构造时：
- 从 `setup` 读取版本快照。
- 创建 `CardPoolAvailability` 字段。
- `CreateMatch(seed)` 把版本信息写入 `State`。

随从候选：
- `AvailableMinions()` 应从只处理种族扩展为同时处理版本：

```csharp
private IEnumerable<MinionDefinition> AvailableMinions()
{
    var active = CurrentActiveTribes();
    return catalog.All.Where(minion =>
        cardPoolAvailability.AllowsMinion(minion) &&
        TribeAvailabilityRules.IsMinionAvailable(minion, active));
}
```

法术候选：

```csharp
private IEnumerable<TavernSpellDefinition> AvailableTavernSpells()
{
    var active = CurrentActiveTribes();
    return spellCatalog.All.Where(spell =>
        cardPoolAvailability.AllowsTavernSpell(spell) &&
        TribeAvailabilityRules.IsTavernSpellAvailable(spell, active));
}
```

`MinionPool`：
- 推荐给 `MinionPool` 新增可选过滤器，或直接传入已过滤 definitions。
- 更保守的实现是新增构造参数：

```csharp
public MinionPool(
    IEnumerable<MinionDefinition> definitions,
    IDictionary<string, int> initial = null,
    IReadOnlyCollection<Tribe> activeTribes = null,
    Func<MinionDefinition, bool> availability = null)
```

构造时先应用 `availability`，再应用种族过滤和 `definition.InPool` 池计数逻辑。

必须覆盖的路径：
- 初始商店、刷新、下一回合商店。
- 三连奖励和各类发现。
- 随机加随从到手牌。
- 随机酒馆法术到手牌。
- 酒馆法术发现。
- 工具卡牌库和“加对手”大卡牌库。

调试命令原则：
- 直接按卡牌 ID 添加的调试命令可以继续绕过卡池版本限制。
- UI 列表默认隐藏未入池卡牌，但测试或调试代码显式传 ID 时不强拦。

## 入口 UI 设计

在 `UnityTavernTribeSelectionView` 中把当前居中单面板改为入口页布局：
- 左侧：保留现有种族选择面板。
- 右侧：新增卡池版本控制面板。
- 整体仍在 `UnityTavernUiStyle.BackWall` 之上，使用现有 `UiFactory` 和 `UnityTavernUiStyle`。

建议布局：
- 外层 `UnityTribeEntryLayout` 使用 `HorizontalLayoutGroup`。
- 左侧宽度约 60% 到 65%，右侧宽度约 35% 到 40%。
- compact 布局下可改为上下排列：上方种族选择，下方版本控制，避免右侧面板过窄。

右侧面板内容：
- 顶部：当前版本名称、状态标签（默认/自定义）、启用数量摘要。
- 版本操作：下拉或纵向列表选择版本；按钮包含新建、复制、重命名、删除、另存为。
- 搜索框：按名称、英文名、`CardId`、`CardNumber` 搜索。
- 标签页：随从、酒馆法术。
- 快捷按钮：全选当前筛选、全不选当前筛选、恢复默认。
- 列表：每行一个 toggle，显示等级、名称、种族或费用、稳定编号。
- 底部：保存状态和上限提示，例如 `3/10 个自定义版本`。

默认版本交互：
- 默认版本所有 toggle 可显示为只读。
- 用户尝试勾选默认版本时，提示先“复制为自定义版本”。
- “复制默认版本”是主要入口，复制后立即进入可编辑状态。

自定义版本交互：
- 勾选变化先进入 dirty 状态。
- 点击进入酒馆前，如果当前版本有未保存修改，自动保存或弹出确认。第一版建议自动保存，降低操作负担。
- 保存后入口页当前版本保持选中。

列表性能：
- 第一版可以先用 `ScrollRect + VerticalLayoutGroup`。
- 如果当前列表元素过多导致卡顿，再做简单虚拟列表。
- 搜索和标签页切换时重建列表即可。

## 实施步骤

1. 新增卡池版本模型和 repository。
2. 新增默认版本派生服务，能从 `MinionCatalog` 和 `SpellCatalog` 生成 `CardPoolVersionSelection`。
3. 新增 `CardPoolAvailabilityRules`，覆盖默认版本和自定义版本判断。
4. 扩展 `MatchSetupOptions` 和 `MatchState`，保存本局卡池版本快照。
5. 修改 `MatchService.AvailableMinions()` 和 `AvailableTavernSpells()`，让所有候选生成优先走统一 helper。
6. 修改 `MinionPool` 构造或调用点，确保商店池计数只包含当前版本允许的随从。
7. 在 `UnityTavernTribeSelectionView` 增加右侧版本控制 UI。
8. 修改入口 start callback，从 `Action<List<Tribe>>` 扩展为能传 `MatchSetupOptions` 或新建入口选择对象。
9. 修改 `LearnHearthstoneBootstrap.ShowUnityTrainer()`，在点击进入酒馆时用种族选择和当前版本创建新的 `MatchService`。
10. 修改工具卡牌库/大卡牌库筛选，使其同步当前对局的版本快照。
11. 补 EditMode 测试和 UI 构建测试。

## 测试计划

模型和持久化：
- repository 在文件不存在时返回空 store。
- 保存 1 个自定义版本后重新加载，随从和法术勾选保持一致。
- 超过 10 个自定义版本时拒绝新增。
- 删除当前版本后回到默认版本。
- 孤儿引用不会导致加载失败。

过滤规则：
- 默认版本与目录 `InPool` 一致。
- 自定义版本未勾选的随从不会进入 `AvailableMinions()`。
- 自定义版本未勾选的酒馆法术不会进入 `AvailableTavernSpells()`。
- 目录 `InPool == false` 的卡牌即使出现在自定义版本 JSON 中，也不会进入正式池。
- 种族过滤和版本过滤同时生效。

MatchService：
- 初始商店不出现未勾选随从。
- 多次刷新不出现未勾选随从。
- 下一回合商店不出现未勾选随从。
- 三连发现不出现未勾选随从。
- 随机加手牌不出现未勾选随从。
- 商店酒馆法术不出现未勾选法术。
- 酒馆法术发现不出现未勾选法术。
- 默认不传版本配置时，现有测试行为不变。

UI：
- 入口页右侧显示“默认版本”。
- 复制默认版本后可勾选/取消勾选随从和法术。
- 新建、复制、重命名、删除按钮状态正确。
- 自定义版本达到 10 个后新建/复制禁用。
- 搜索能按名称和编号过滤。
- 随从/酒馆法术标签页切换不会丢失未保存修改。
- 点击进入酒馆后，本局 `MatchState` 记录所选版本名称和快照。

回归：
- 现有种族选择仍然要求手动选择 5 个，或使用全部 10 个种族快捷入口。
- 旧入口和不传 `MatchSetupOptions` 的测试仍可运行。
- 直接调试添加卡牌命令不被版本控制硬拦截。

## 验收标准

- 进入 Unity 酒馆训练器前，入口页右侧可以看到卡池版本控制区域。
- 当前初始版本为“默认版本”，且默认版本内容来自现有目录 `InPool`。
- 用户可以保存最多 10 个自定义版本。
- 自定义版本中取消勾选的随从不会出现在商店、刷新、下一回合、发现和随机生成中。
- 自定义版本中取消勾选的酒馆法术不会出现在商店法术、随机法术和法术发现中。
- 种族选择和版本选择同时生效。
- 自定义版本持久化到本地 JSON，重启后仍存在。
- 原始卡牌 JSON 不被修改。
- 现有 EditMode 测试不因默认版本引入而回归。

## 非第一版范围

- 不做云端同步。
- 不做版本导入/导出分享。
- 不做官方历史赛季数据自动下载。
- 不在第一版支持“按机制一键禁用”。
- 不强制调试命令遵守卡池版本。
- 不把默认版本写入本地 JSON。

## 风险和注意事项

- `InPool` 判断分散在多个路径里，必须通过 `AvailableMinions()`、`AvailableTavernSpells()` 和 `MinionPool` 统一收口，否则会出现“商店不出，但发现会出”的漏网行为。
- 自定义版本保存显式启用列表后，目录新增卡牌不会自动进入旧版本，这是符合版本语义的；用户可以通过“恢复默认”或“同步新增卡牌”处理。
- 如果未来需要历史版本跟随官方赛季，应新增官方版本来源，不要复用本地自定义版本结构硬编码。
- UI 右侧列表会比较长，第一版需要保证搜索、标签页和滚动体验可用，避免入口页变成难操作的大表格。
