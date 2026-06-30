# 扭曲时空酒馆收尾实现计划

## 目标

这份文档只覆盖扭曲时空酒馆主线实现后剩余的产品化收尾项。当前卡牌效果主线已经完成，`BlockedNonMinions` 为 0，剩余问题集中在三类：

1. 默认随机 Timewarp 候选池仍只投放当前 125 张随从，没有把已实现的非随从 Timewarped 卡纳入随机 offer。
2. `Timewarped Big Winner!` 的 Darkmoon Prize 仍使用 `darkmoon_prize_proxy`，底层是 Tier 3 Bounty Tavern spell 代理。
3. targeted EditMode 已通过，但默认全量 EditMode 之前出现过卡在 `test run started` 且没有结果 XML 的情况，需要单独定位和稳定化。

## 当前基线

- 当前池随从效果、P1-P4 卡牌效果批次、历史额外 Deios、38 张非随从 Timewarped 卡效果已经接完。
- `TimewarpedTavernCatalog.BlockedNonMinions.Count == 0`。
- 最近 targeted 验证：
  - `Logs/TimewarpedFinalNonMinionTests.xml`: 7 passed, 0 failed。
  - `Logs/CodexCompileCheck.log`: Unity batch compile return code 0。
- 默认候选池入口在 `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs` 的 `TimewarpedCandidatesForKind()`。
- `TimewarpedTavernCatalog.Minor/Major` 当前来自 `Current`，而 `Current` 只包含 `PoolStatus == "current"` 的 125 张随从；补充的非随从卡使用 `PoolStatus == "implemented_non_minion"`，所以不会进入默认随机 offer。
- `BatchEditModeTestRunner.RunEditMode()` 默认排除 `Stress` 和 `Marathon`，但仍曾出现全量默认 EditMode 卡住，需要定位具体测试或 runner 行为。

## 不变约束

- 历史/上线额外池继续由 `UseHistoricalTimewarpedPool` / `TimewarpedPoolVersion` 控制，不能进入默认当前池。
- 不把 `Stress` / `Marathon` 放回默认全量验证。
- 保留 `TimewarpedTavernCatalog.Current == 125` 作为当前随从池数据不变量，避免现有数据测试、历史池开关和文档语义混乱。
- 非随从默认投放只应包含已实现、可购买、非 utility、非 blocked 的卡。
- Unity batch 前必须检查 Unity 进程和 `Temp/UnityLockfile`，跑完后也要确认没有残留。

## 执行顺序

1. 先收默认随机候选池策略，因为它直接决定玩家是否能自然看到非随从 Timewarped 卡。
2. 再替换 Big Winner 的 Darkmoon Prize 代理，因为它需要新增或扩展奖品数据和通用 Discover 入口。
3. 最后处理默认全量 EditMode 卡住，因为它更像验证基础设施问题，且需要在候选池和 Big Winner 改完后再跑最终默认集。

## 批次 A：默认随机候选池纳入非随从

### 目标

让默认 Minor/Major Timewarped Tavern offer 从“当前池随从”扩展为“当前池随从 + 已实现当前非随从”，同时保持历史额外池仍由开关控制。

### 推荐实现

1. 在 `TimewarpedTavernCatalog` 增加显式查询入口，不改变 `Current` 语义：
   - `ImplementedNonMinions`
   - `ImplementedNonMinionsForKind(TimewarpKind kind)`
   - 或者更直接的 `OfferableCurrentNonMinionsForKind(TimewarpKind kind)`
2. 非随从筛选条件：
   - `CardKind != CardKind.Minion`
   - `PoolStatus == "implemented_non_minion"`
   - `TimewarpKind == Minor/Major`
   - 不含 `blocked_by_non_minion_support`
   - 不含 `timewarp:exit`
3. 修改 `MatchService.TimewarpedCandidatesForKind(kind)`：
   - 先取现有 `timewarpedCatalog.Minor/Major` 随从。
   - 再追加对应 kind 的已实现非随从。
   - `TimewarpedPoolVersion.Current` 时直接返回这个组合结果。
   - 非 Current 版本再按现有逻辑追加 `HistoricalExtra`。
4. 对 `TimewarpKind.None` 的 3 张非随从先不进入默认随机池，除非后续从数据源确认它们应该属于 Minor 或 Major：
   - `BG34_Treasure_300` Timewarped Investment
   - `BG34_HeroPowerSpell_018` Power of Tavish
   - `BG34_HeroPowerSpell_022` Power of Rakanishu
5. 如果后续确认 `TimewarpKind.None` 也应投放，优先补一张显式映射表，而不是按 `TechLevel == 0` 自动混入。

### 测试计划

更新或新增 `MatchServiceTests`：

- 将 `TimewarpedTavern_DefaultCandidatesExcludeHistoricalAndBlockedNonMinions` 改名为类似：
  - `TimewarpedTavern_DefaultCandidatesIncludeImplementedNonMinionsAndExcludeHistorical`
- 断言默认候选池：
  - Minor = 55 张当前随从 + 18 张 Minor 非随从。
  - Major = 70 张当前随从 + 17 张 Major 非随从。
  - 不含 `historical_extra`。
  - 不含 `blocked_by_non_minion_support`。
  - 不含 `timewarp:exit`。
  - 不含 `TimewarpKind.None` 非随从，直到映射表确认。
- 增加 active tribe 过滤覆盖：
  - `Tribe.None` 的非随从不被种族禁用过滤误删。
  - 随从仍按现有 active tribe 规则过滤。
- 增加随机 offer 覆盖：
  - 固定 seed 打开 Minor/Major，确认生成 slot 可以承载 `CardKind.TavernSpell`。
  - 购买 offer 时仍走 Chronum，不走普通 Gold。

### 验收标准

- 默认 Timewarp 随机 offer 能自然出现已实现非随从卡。
- 当前随从池计数仍为 125，`Current` 语义不变。
- 历史额外 33 张仍默认不出现。
- 相关 targeted EditMode 通过。

## 批次 B：Big Winner 替换真实 Darkmoon Prize 后端

### 目标

移除 `Timewarped Big Winner!` 对 Tier 3 Bounty Tavern spell 的模糊代理，建立可复用的 Darkmoon Prize 池和 Discover 入口。

### 推荐实现

1. 先做数据盘点：
   - 搜索本地 `battlegroundsSpells.json`、`battlegroundsHeroes.json`、Firestone/HearthstoneJSON 数据中 Darkmoon Prize 的真实 card id、tier、文本和效果。
   - 如果本地资源没有完整奖品牌，新增 `Assets/LearnHearthstone/Resources/Data/darkmoonPrizes.json`。
2. 建模优先复用 Tavern spell 路径：
   - 将 Darkmoon Prize 作为可生成 spell-like card。
   - 标签使用 `darkmoon_prize`、`darkmoon_prize_tier_3` 等。
   - 不再使用 `darkmoon_prize_proxy`。
3. 新增通用选择器/入口：
   - `SelectDarkmoonPrizeDefinitions(int tier)`
   - `StartDarkmoonPrizeDiscover(int tier, string source, int seedSalt)`
   - 必要时抽一个通用 `StartGeneratedSpellDiscover(...)`，避免只为 Big Winner 写死。
4. 第一阶段只要求 Tier 3 奖品真实可用，因为 Big Winner 文本只需要 Tier 3。
5. 如果部分 Tier 3 奖品效果还无法完整实现：
   - 不要把未实现奖品放入 Big Winner 可发现池。
   - 或显式标记为 `blocked_by_darkmoon_prize_effect`，并在测试中确认不会被选中。
6. 替换 `MatchService.StartTimewarpedBigWinnerDiscover()`：
   - 从真实 Darkmoon Prize 池取 Tier 3。
   - 保留每 3 回合重复调度：`timewarped_big_winner_due_round`。
   - 更新日志文本，去掉 proxy 描述。
7. 为 Tickatus / Ticket Collector 预留复用边界，但本批次不强制实现英雄技能完整效果。

### 测试计划

新增或更新 targeted EditMode：

- Big Winner 首次施放发现 Tier 3 Darkmoon Prize。
- 选项带 `darkmoon_prize` 和 `darkmoon_prize_tier_3`，不带 `darkmoon_prize_proxy`，也不带 `bounty`。
- 每 3 回合重复 Discover 的调度仍生效。
- 如果实现了奖品具体效果，至少覆盖每个 Tier 3 奖品的核心行为。
- 如果暂时只接入已实现奖品，测试确认未实现奖品不会进入可选池。

### 验收标准

- Big Winner 不再依赖 Bounty 代理。
- Big Winner 的 Discover、选择和三回合重复都使用同一套真实 Darkmoon Prize 入口。
- 后续 Tickatus、Ticket Collector、Darkmoon Prize 相关饰品可以复用该入口。

## 批次 C：默认全量 EditMode 卡住定位和稳定化

### 目标

把“默认全量 EditMode 会卡住”从经验问题变成可复现、可定位、可修复的问题。默认全量不包含 Stress/Marathon，但必须能产出 XML 结果。

### 推荐实现

1. 保留 `BatchEditModeTestRunner.RunEditMode()` 默认排除 `Stress` / `Marathon` 的策略。
2. 给 batch runner 增加诊断能力：
   - 生成默认测试清单到 `Logs/EditModeDefaultManifest.txt`。
   - 日志中打印默认测试数量。
   - 支持按清单 shard 运行，例如 `-batchTestShardIndex` / `-batchTestShardCount`，或由外部脚本传 `-batchTestName`。
3. 新增一个 PowerShell 诊断脚本，例如 `Tools/run-editmode-bisect.ps1`：
   - 先检查 Unity 进程和 `Temp/UnityLockfile`。
   - 读取默认测试清单。
   - 分片运行默认测试，每片设置超时。
   - 某片卡住时二分缩小到具体 test fixture 或 test case。
   - 超时后终止 Unity batch 进程，并记录最后开始的测试名、日志路径和 shard 信息。
4. 定位卡住测试后按原因处理：
   - 如果是本应归类为 `Stress` / `Marathon` 的长跑测试，补 NUnit category。
   - 如果是死循环或等待条件缺失，修测试或产品代码。
   - 如果是 Unity TestRunner / wrapper 退出问题，修 runner 或外层脚本，不用改业务测试。
5. 避免用“永久跳过”掩盖真实失败。只有长跑专项测试才进入 `Stress` / `Marathon`。

### 推荐命令

编译：

```powershell
powershell -ExecutionPolicy Bypass -File 'Tools\check-unity-compile.ps1' -UnityPath 'D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe'
```

默认 EditMode：

```powershell
& 'D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'D:\unity project\Learn Heartstone' `
  -executeMethod LearnHearthstone.Editor.BatchEditModeTestRunner.RunEditMode `
  -batchTestResults 'Logs\EditModeDefaultTests.xml' `
  -logFile 'Logs\EditModeDefaultTests.log' -quit
```

专项 Stress 仍单独排期：

```powershell
& 'D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'D:\unity project\Learn Heartstone' `
  -executeMethod LearnHearthstone.Editor.BatchEditModeTestRunner.RunStressEditMode `
  -batchTestResults 'Logs\EditModeStressTests.xml' `
  -logFile 'Logs\EditModeStressTests.log' -quit
```

### 验收标准

- 默认 EditMode 能稳定产出 `Logs/EditModeDefaultTests.xml`。
- XML 显示默认测试通过，且不包含 `Stress` / `Marathon`。
- 失败或超时时能输出具体 test name / shard / log，不再只停在 `test run started`。
- 批处理结束后无 Unity 进程残留，无 `Temp/UnityLockfile`。

## 最终完成定义

三批次全部完成后，扭曲时空酒馆才算产品化收口完成：

- 默认随机 Timewarp offer 包含已实现非随从，且历史额外池仍受开关控制。
- Big Winner 使用真实 Tier 3 Darkmoon Prize 后端，不再使用 Bounty 代理。
- 默认全量 EditMode 稳定通过并产出 XML；Stress/Marathon 继续作为专项测试单独运行。

## 风险和回退

- 非随从进入默认 offer 后，随机局面覆盖面会显著变大。若发现 UI 或选择流问题，优先修具体卡和通用购买流，不回退到全局排除非随从。
- Darkmoon Prize 数据如果短期无法完整确认，可以先只上线 Tier 3 且只投放已实现奖品，但必须去掉 Bounty 代理边界。
- 如果默认全量 EditMode 卡住来自 Unity runner 本身，先保证 bisection 脚本能产出定位信息，再决定是否替换 runner 调用方式。
