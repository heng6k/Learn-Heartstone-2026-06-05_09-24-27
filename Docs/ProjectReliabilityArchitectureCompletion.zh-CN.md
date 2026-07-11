# 项目稳定性与架构阶段完成记录

日期：2026-07-11

## 1. UI 测试稳定性

### Error

- `BoardCard_HoverShowsAndHidesKeywordTooltip` 抛出 `Sequence contains no matching element`。
- `TribeSelectionView_AdvancedMechanicPoolEditorPassesSelectedPools` 超过 NUnit 180 秒限制。

### Expected

- 悬停测试稳定构造商店随从并验证提示框。
- 高级机制池测试只验证 UI 选择结果传递，不受完整资源规模和整页重复重建影响。

### Cause

- 悬停夹具先清空商店，再调用依赖商店随从的 `CreateBoardMinion`。
- 高级池测试重复加载完整目录、逐个选择五个种族并多次重建整页；早期排查中编译失败又导致 Unity 继续运行旧测试程序集，放大了误判。

### Fix

- 在创建商店随从后再清空商店。
- 高级池测试改用最小英雄、任务、奖励、饰品、畸变、随从和法术目录。
- 使用“全部种族”一次进入高级页，避免六次无关整页重建。
- 高级池筛选和切页只重建覆盖层。
- 测试通过 `UiFactory.SetFontOverride` 使用内置字体，避免扩大共享动态字体图集。

### Prevention

- UI 行为测试使用最小目录；完整目录只用于资源完整性测试。
- 不通过提高超时掩盖资源解析、重建和夹具问题。
- Unity 编译失败时不得采信旧程序集测试结果。

结果：目标高级池测试 1/1，通过耗时 1.05 秒；完整 `UnityTavernTrainerViewTests` 103/103 通过。

## 2. 机制覆盖注册表

- 新增 `MechanicCoverageRegistry`，每项机制拥有稳定键、配置性、战斗消费、UI 可见性、测试覆盖、可信度和说明。
- `MechanicCoverageReportService` 只从注册表生成独立报告行，不再维护第二份硬编码事实。
- 校验稳定键和系统名称唯一、必填说明完整、报告行与注册表隔离。

## 3. SideModifierService

- 抽离字段读取/写入、Tavern 同步、历史附魔回算和支持的战斗奖励累积。
- `MatchService` 保留双方选择、调用时机和日志编排。
- 敌方新增/编辑、复制/镜像、工具调整和战斗奖励继续使用同一回算语义。
- 数值写入采用非负与饱和计算，避免增长溢出。

## 4. 场景版本迁移

- 当前版本升级为 `battle-test-loop-v2`。
- 新增有序 `v1 → v2` 迁移，将旧 Tavern 历史字段一次性写入权威 `PlayerCombatModifiers`。
- 当前版本重复迁移保持幂等；未知未来版本明确拒绝。
- Capture、内存仓库和文件仓库统一输出/读取当前版本。

## 5. 后续内容准入

新增 `Docs/ContentAndOpponentMechanicEntryChecklist.zh-CN.md`，覆盖数据、图片、机制注册、敌我一致性、场景迁移、黑盒顺序和回归要求。后续批量补卡及高级敌方机制应以该清单为合入门槛。

## 6. 验证结果

- UI 两项 + 注册表 + 全局参数/迁移：22/22 通过，2.55 秒。
- SideModifier、场景映射、MatchService 战斗测试、设计验证：25/25 通过，1.75 秒。
- 完整 Unity UI 组：103/103 通过，506.52 秒。完整组仍偏慢，但不再有单项 180 秒超时；后续可单独开展全组字体/截图测试性能治理。
- `git diff --check`：通过，仅有既有 CRLF/LF 提示。
