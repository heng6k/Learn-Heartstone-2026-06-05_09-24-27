# Learn Heartstone Project Progress

## Current Date

2026-06-05

## Branch

当前 Unity 工程分支：

```text
codex/unity-full-migration
```

## Source Project Summary

原项目位置：

```text
D:\Users\heng\Desktop\0000\222\jiaocheng
```

核心前端与规则项目：

```text
D:\Users\heng\Desktop\0000\222\jiaocheng\kaifa
```

原项目技术栈：

- React 18
- TypeScript
- Vite
- Zustand
- Vitest

原项目核心功能：

- 酒馆训练器界面。
- 商店、手牌、玩家战场、对手战场。
- 随从编辑器。
- 买随从、卖随从、刷新、冻结、升级、下一回合。
- 卡池占用和归还。
- 三连和金色随从。
- 三连奖励发现。
- 确定性基础战斗模拟。
- 招募日志、战斗日志、回放控制。
- 搜索/提示面板。
- DeepSeek 顾问面板。
- 真实酒馆随从数据接入。

## Target Unity Project

目标 Unity 工程位置：

```text
D:\unity project\Learn Heartstone
```

工程现状：

- 已存在 Unity 工程结构。
- 已初始化 git。
- 已有初始提交 `7b79d99 Initial check-in`。
- 已切换到新分支 `codex/unity-full-migration`。
- `Packages/manifest.json` 已包含 Unity UI、Input System、URP、Unity Test Framework 等依赖。

当前已存在的 Unity 目录：

```text
Assets/
Packages/
ProjectSettings/
UserSettings/
```

当前检测到的未提交 Unity 设置改动：

```text
Assets/Settings/UniversalRP.asset
ProjectSettings/ProjectSettings.asset
ProjectSettings/URPProjectSettings.asset
ProjectSettings/PackageManagerSettings.asset
```

这些文件未被回退，应视为 Unity 工程初始化或用户已有变更。

## Decisions Made

已确认采用方案 A：分层完整迁移。

迁移目标：

- 完整迁移现有训练器能力。
- 使用 Unity + C# 重写。
- 保持代码封装性，便于后续分块修改。
- 不把规则逻辑塞进 `MonoBehaviour`。
- UI 外层增加主功能大厅，后续功能从大厅扩展。

UI 决策：

- 首页是主功能大厅。
- 当前启用模块是酒馆训练器。
- 酒馆训练器内部包含商店、手牌、玩家战场、对手战场、编辑器、日志、回放控制、搜索/提示面板。
- 后续功能入口在主功能大厅中预留，不做假功能。

## Documents Added

已新增：

```text
Docs/UnityMigrationDesign.md
Docs/ProjectProgress.md
```

`UnityMigrationDesign.md` 记录迁移架构、数据迁移、资源迁移、UI 结构、测试策略和实施顺序。

`ProjectProgress.md` 记录当前项目状态、已确认决策和下一步任务。

## Implementation Progress

2026-06-05 已开始完整 Unity + C# 迁移，并新增以下内容：

- `Assets/LearnHearthstone/LearnHearthstone.Runtime.asmdef`
- `Assets/LearnHearthstone/Tests/EditMode/LearnHearthstone.Tests.asmdef`
- `Assets/LearnHearthstone/Runtime/Domain/Models`
- `Assets/LearnHearthstone/Runtime/Domain/Engine`
- `Assets/LearnHearthstone/Runtime/Domain/Data`
- `Assets/LearnHearthstone/Runtime/Application`
- `Assets/LearnHearthstone/Runtime/Adapters`
- `Assets/LearnHearthstone/Runtime/Presentation`
- `Assets/LearnHearthstone/Editor/LearnHearthstoneSceneSetup.cs`
- `Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json`
- `Assets/LearnHearthstone/Resources/CardImages`

已迁移能力：

- 纯 C# 酒馆规则 `TavernRules`。
- 确定性 RNG `SeededRng`。
- 随从模型、战场实例、酒馆状态、对局状态、战斗日志模型。
- 卡池占用、释放、抽商店。
- 三连检测和金色随从合成。
- 基础确定性战斗模拟。
- 279 个随从 JSON 数据加载。
- 卡图资源复制到 Unity Resources。
- MatchService 初始对局、购买、打出、出售、刷新、冻结、升级、下一回合、发现奖励、模拟战斗、调试金币。
- 本地 JSON 存档接口和实现。
- 本地顾问接口和基础建议。
- 主功能大厅 UI。
- 酒馆训练器工作台 UI，包括商店、手牌、玩家战场、对手战场、编辑器、日志、回放/战斗入口、搜索提示面板。
- SampleScene 已接入 `LearnHearthstoneBootstrap`。

验证结果：

- Unity 批编译通过，日志结尾包含 `Exiting batchmode successfully now!`。
- Unity Editor 烟测通过，日志包含 `Learn Heartstone smoke test passed.`。
- 烟测验证了 Resources 中 279 个随从可加载，并且 `MatchService` 能创建 1 回合、3 金币、3 个商店槽位的初始对局。

当前限制：

- Unity 命令行 `-runTests` 当前没有生成 `TestResults.xml`，但批编译和 Editor 烟测可用。
- 随从图片已复制到 Resources，但导入设置尚未批量调整为 Sprite。
- UI 为第一版程序化 UGUI，已具备完整功能入口和核心交互，但视觉 polish、拖拽、复杂编辑器和完整回放时间轴仍适合后续单独迭代。
- 复杂独有效果仍按原项目边界保留为后续扩展。

## Recommended Next Work

下一步建议按以下顺序执行：

1. 创建 Unity C# 目录和 asmdef。
2. 迁移 `Domain` 模型和枚举。
3. 添加 Unity EditMode 测试骨架。
4. 迁移随从 JSON 数据和加载器。
5. 迁移 RNG、酒馆规则、卡池、三连。
6. 迁移 MatchService 基础命令。
7. 迁移确定性战斗。
8. 导入卡图资源并建立图片加载器。
9. 搭建主功能大厅 UI。
10. 搭建酒馆训练器工作台 UI。
11. 接入日志、回放、编辑器、搜索/提示。
12. 接入本地保存。

## Validation Needed Later

当前只完成迁移设计和进度文档，尚未写 C# 迁移代码。

后续实现完成后应验证：

- Unity 工程能正常打开。
- EditMode 测试通过。
- 随从数据能加载。
- 卡图能按 `ImagePath` 显示。
- 商店刷新使用确定性 seed。
- 买卖刷新升级和三连行为与 TS 项目一致。
- 战斗日志和回放步骤能正常展示。
- 主功能大厅能进入酒馆训练器。

## Notes For Future Agents

- 不要直接修改或回退用户已有 Unity 设置变更。
- 规则层应保持纯 C#，避免依赖 Unity 场景对象。
- UI 通过 Application 命令改变状态，不直接改领域状态。
- 所有随机逻辑必须通过 seed 控制。
- 数据兼容逻辑放在 Adapter 层。
- 后续补随从效果时优先按单个效果文件拆分。
