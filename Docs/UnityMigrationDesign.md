# Learn Heartstone Unity Migration Design

## Goal

将原 `jiaocheng/kaifa` React + TypeScript 酒馆战棋单人训练器完整迁移为 Unity + C# 项目，并保持模块边界清晰，方便后续按功能块继续修改。迁移范围包括训练器 UI、买卖刷新升级、三连、卡池、确定性战斗、日志、随从数据和图片资源。

项目定位保持为本地单人教学训练工具，不扩展成完整线上游戏。当前迁移目标是复刻已有训练器能力，而不是补齐所有官方酒馆战棋系统。

## Selected Approach

采用分层完整迁移方案。

Unity 负责呈现、输入和资源管理；核心规则迁为纯 C# 领域层，避免规则代码依赖 `MonoBehaviour`、场景对象或 UI 组件。

主要分层：

- `Domain`: 纯 C# 规则模型和算法。
- `Application`: 对局用例服务，负责接收命令并调用领域层。
- `Adapters`: JSON、图片、本地存档、外部服务等适配。
- `Presentation`: Unity UI、ViewModel、Presenter、交互绑定。
- `Resources`: 随从数据、图片等运行时资源。
- `Tests`: 规则层和应用层测试。

## Target Project Structure

目标根目录：`D:\unity project\Learn Heartstone`

建议目录：

```text
Assets/
  LearnHearthstone/
    Runtime/
      Domain/
        Models/
        Engine/
        Data/
        Effects/
      Application/
        Commands/
        Services/
        ViewModels/
      Adapters/
        Data/
        Persistence/
        Images/
        Advisor/
      Presentation/
        MainHub/
        TavernTrainer/
        Common/
    Resources/
      Data/
      CardImages/
    Tests/
      EditMode/
Docs/
  UnityMigrationDesign.md
  ProjectProgress.md
```

## Domain Layer

`Domain` 是迁移的核心，目标是把原 TypeScript 规则文件翻译成不依赖 Unity 的 C# 类。

对应迁移关系：

- `src/domain/types/Minion.ts` -> `Domain/Models/MinionDefinition.cs`, `MinionInstance.cs`, `Enums.cs`
- `src/domain/types/Tavern.ts` -> `Domain/Models/TavernState.cs`
- `src/domain/types/Combat.ts` -> `Domain/Models/CombatResult.cs`, `CombatLogEntry.cs`
- `src/domain/types/Match.ts` -> `Domain/Models/MatchState.cs`
- `src/domain/engine/rng.ts` -> `Domain/Engine/SeededRng.cs`
- `src/domain/engine/tavernRules.ts` -> `Domain/Engine/TavernRules.cs`
- `src/domain/engine/MinionPool.ts` -> `Domain/Engine/MinionPool.cs`
- `src/domain/engine/TripleEngine.ts` -> `Domain/Engine/TripleEngine.cs`
- `src/domain/engine/CombatEngine.ts` -> `Domain/Engine/CombatEngine.cs`
- `src/domain/engine/EffectSystem.ts` -> `Domain/Effects/EffectRegistry.cs`
- `src/domain/engine/MatchEngine.ts` -> 拆分为 `Application/Services/MatchService.cs` 与多个领域服务。

设计原则：

- 领域对象使用普通 C# 类型，不继承 `MonoBehaviour`。
- 引擎方法尽量保持无副作用，或返回新的状态对象。
- 所有随机逻辑必须通过 `SeededRng` 注入，确保战斗和商店刷新可复现。
- 错误通过明确的结果类型或受控异常返回给应用层，由 UI 展示。
- 效果系统保留插件式注册结构，后续每个随从效果可以独立文件实现。

## Application Layer

`Application` 负责把 UI 操作转成命令，并维护当前对局状态。

核心服务：

- `MatchService`: 初始化对局、执行命令、持有当前 `MatchState`。
- `TavernCommandService`: 买随从、卖随从、刷新、冻结、升级、进入下一回合。
- `BoardCommandService`: 打出随从、移动随从、编辑双方战场。
- `CombatService`: 执行确定性战斗并返回结果与日志。
- `DiscoverService`: 处理三连奖励发现。
- `ReplayService`: 管理战斗日志步骤、播放状态和回放索引。

命令类型：

- `BuyMinionCommand`
- `SellMinionCommand`
- `RerollShopCommand`
- `FreezeShopCommand`
- `UpgradeTavernCommand`
- `PlayMinionCommand`
- `MoveMinionCommand`
- `UpdateMinionCommand`
- `ChooseDiscoverCommand`
- `NextTurnCommand`
- `SimulateCombatCommand`
- `DebugAddGoldCommand`

应用层输出 ViewModel，不让 UI 直接读取复杂领域对象。

## UI Design

Unity UI 分为两层。

第一层是主功能大厅。它是项目首页，用于承载后续功能扩展。目前启用入口：

- 酒馆训练器

预留入口：

- 英雄训练
- 阵容库
- 教学场景
- 数据浏览
- 设置

未完成入口保持禁用或占位，避免制造虚假功能。

第二层是酒馆训练器工作台。点击主功能大厅中的酒馆训练器后进入该界面。

训练器面板：

- 商店
- 手牌
- 玩家战场
- 对手战场
- 编辑器
- 日志
- 回放控制
- 搜索/提示面板

推荐布局：

- 中央上方：商店。
- 中央中部：玩家战场。
- 右侧：对手战场、编辑器、搜索/提示。
- 底部：手牌、回放控制。
- 左侧或右下：战斗日志与招募日志，可切换标签。

UI 数据流：

```text
Unity Button / Drag Input
  -> Presenter
  -> Application Command
  -> Domain Engine
  -> MatchState
  -> ViewModel
  -> View Refresh
```

UI 不直接修改规则状态。每个面板拥有自己的 View 和 Presenter，后续可以单独替换或重做。

## Data Migration

数据源：

- `jiaocheng/kaifa/src/data/battlegroundsMinions.json`
- `jiaocheng/数据/酒馆战棋随从信息总表.json`
- `jiaocheng/数据/酒馆战棋随从信息总表.csv`
- `jiaocheng/数据/1-7本随从/**`
- `jiaocheng/数据/10大种族+中立/**`

优先迁移 `kaifa/src/data/battlegroundsMinions.json`，因为它已经被现有训练器验证过，字段最接近当前代码。根目录 `数据` 文件夹作为补充来源，用于补图片和校对中文资料。

目标位置：

```text
Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json
Assets/LearnHearthstone/Resources/Data/minionDataSchema.md
```

Unity 中通过 `TextAsset` 加载 JSON，再由 `MinionCatalogLoader` 转为 `MinionDefinition` 列表。

字段映射：

- `id` -> `Id`
- `cardId` -> `CardId`
- `dbfId` -> `DbfId`
- `name` -> `Name`
- `tavernTier` -> `TavernTier`
- `baseAttack` -> `BaseAttack`
- `baseHealth` -> `BaseHealth`
- `tribes` -> `Tribes`
- `keywords` -> `Keywords`
- `text` -> `Text`
- `inPool` -> `InPool`
- `poolCount` -> `PoolCount`
- `golden` -> `Golden`
- `imagePath` -> `ImagePath`
- `effectIds` -> `EffectIds`
- `tokenId` -> `TokenId`

数据读取规则：

- 运行时只通过 `MinionCatalog` 查询随从。
- UI 不直接读 JSON。
- 旧数据字段不在 UI 层做兼容，统一在 Adapter 层转换。
- 迁移时保留原始 JSON 副本，避免资料丢失。

## Resource Migration

图片源：

- `jiaocheng/数据/1-7本随从/**.png`
- `jiaocheng/数据/1-7本随从/酒馆战棋随从 - HSReplay.net_files/**.png`
- `jiaocheng/cards_db_work/extract_probe/**.png`

目标位置：

```text
Assets/LearnHearthstone/Resources/CardImages/
```

图片命名优先使用卡牌 ID，例如：

```text
Assets/LearnHearthstone/Resources/CardImages/BG35_801.png
```

图片加载策略：

- `MinionDefinition.ImagePath` 存储 Resources 相对路径，例如 `CardImages/BG35_801`。
- `CardImageProvider` 根据路径加载 `Sprite`。
- 找不到图片时返回统一占位卡图，避免 UI 空白或报错。

导入设置建议：

- Texture Type: Sprite
- Sprite Mode: Single
- Pixels Per Unit: 100
- Compression: Normal Quality
- Mip Maps: Disabled

资源迁移脚本可以后续用 Editor 工具实现，但运行时不依赖脚本生成。

## Persistence

本地保存不在领域层实现，放在 `Adapters/Persistence`。

保存内容：

- 当前对局状态。
- 玩家战场、对手战场。
- 手牌、商店、卡池快照。
- 回合、金币、等级、冻结状态。
- 招募日志和战斗日志。
- 设置项。

推荐使用 JSON 文件保存到 `Application.persistentDataPath`。

## Advisor Panel

原项目有 `DeepSeekAdvisorPanel` 和 `DeepSeekClient`。Unity 迁移时先保留接口，不强依赖在线服务。

设计：

- `IAdvisorService` 定义建议接口。
- `LocalAdvisorService` 根据当前搜索提示和基础规则给出本地建议。
- `RemoteAdvisorService` 预留给后续 DeepSeek 或其他模型服务。
- UI 面板显示服务返回结果，不直接调用网络。

这样即使没有 API key，训练器也能完整运行。

## Testing Strategy

优先迁移现有 Vitest 用例覆盖的规则：

- 酒馆规则。
- 卡池占用与归还。
- 三连检测与金色随从生成。
- 确定性战斗。
- 效果系统 no-op 与注册调用。
- 随从目录加载。
- 买卖刷新升级、打出、下一回合。

目标测试位置：

```text
Assets/LearnHearthstone/Tests/EditMode/
```

测试使用 Unity Test Framework 的 EditMode 测试。领域层不依赖场景，因此可以快速运行。

## Implementation Order

推荐执行顺序：

1. 建立 `Assets/LearnHearthstone` 目录与 asmdef。
2. 迁移 C# 模型和枚举。
3. 迁移 JSON 数据加载和随从目录。
4. 迁移 RNG、酒馆规则、卡池、三连。
5. 迁移 MatchService 的买卖刷新升级、打出、下一回合。
6. 迁移确定性战斗与日志。
7. 导入随从图片并接入 `CardImageProvider`。
8. 搭建主功能大厅。
9. 搭建酒馆训练器工作台各面板。
10. 接入编辑器、日志、回放、搜索/提示。
11. 接入本地保存。
12. 补测试并跑通 Unity EditMode 测试。

## Current Known Limits

完整迁移不等于完整官方规则实现。现有 TS 项目中未完整实现的内容继续保持未完成状态：

- 所有 279 个随从的独有效果。
- 英雄、护甲和英雄技能完整规则。
- 酒馆法术、任务、饰品等完整官方系统。
- 官方战斗细节中的高级互动。

这些应作为后续功能从主功能大厅逐步扩展，而不是混进本次迁移造成范围失控。
