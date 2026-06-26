# v0.1-alpha 稳定化测试记录

## 本轮范围

日期：2026-06-20。

目标：

1. 冻结功能面，只保留阻塞修复和明显坏体验修复。
2. 跑 Alpha 前置测试：全量 EditMode、卡池版本控制、进入对局过滤、核心冒烟路径。
3. 更新卡池版本控制已知问题。
4. 为 Windows 发布包准备计划文档。

## 功能冻结状态

状态：已记录。

冻结记录位置：`Docs/AlphaReleaseRoadmap.md` 的 `v0.1 Alpha 功能冻结记录`。

冻结期间暂不做：

- 新英雄/伙伴机制。
- 双打系统、传递、队友奖励或 BGDUO 行为。
- 大 UI 重做。
- 大规模素材系统。
- 正式分发平台接入。

允许做：

- P0/P1 阻塞修复。
- 明显坏体验修复。
- 测试、冒烟、文档和发包准备。

## 自动测试记录

| 测试项 | 结果 | 记录 |
| --- | --- | --- |
| 卡池版本控制入口/UI 目标测试 | 通过 | `TestResults-CardPool-EntryUI.xml`：`Total 1, Passed 1, Failed 0`，时间 2026-06-20 10:45。 |
| 卡池版本进入对局过滤测试 | 通过 | `TestResults-CardPool-MatchService.xml`：`Total 1, Passed 1, Failed 0`，时间 2026-06-20 10:44。 |
| 官方单人卡池一致性测试 | 通过 | `TestResults-OfficialConsistency.xml`：`Total 1, Passed 1, Failed 0`，时间 2026-06-20 12:57。 |
| 最近一次完整 EditMode 记录 | 通过，非本轮即时结果 | `TestResults-EditMode.xml`：`Total 396, Passed 395, Failed 0, Skipped 1`，时间 2026-06-18 18:53。跳过项是显式 30 分钟 soak 测试。 |
| 图片加载回归测试 | 通过 | Unity MCP `run_tests` job `9371e8c593fa42c2a6b417c6c3ab9479`：`Total 1, Passed 1, Failed 0`。 |
| 卡池版本聚焦回归 | 通过 | Unity MCP `run_tests` job `3ec90c6cea044de6b0cc9c976fdf0ee6`：`Total 4, Passed 4, Failed 0`，覆盖图片 provider、弹窗改名/筛选/批量剔除、复制默认进对局、未保存切换提示。 |
| 本轮 EditMode 门禁 | 通过 | Unity MCP `run_tests` job `92183e883d5e4ceaa1ae33c715923453`：`Total 399, Passed 399, Failed 0, Skipped 0`，耗时 `328.87s`。本轮按发布门禁口径排除显式 30 分钟 soak：`RobustnessEdgeTests.ThirtyMinuteExtremeCombatAndRecruitSoak_MaintainsBounds`。 |
| AlphaSmoke batch 冒烟 | 通过 | `TestResults-AlphaSmoke.xml`：`Total 13, Passed 13, Failed 0, Skipped 0`，耗时 `113.00s`。覆盖入口、卡池版本弹窗、改名、筛选、滚动加载、剔除保存、进入对局、刷新、下一回合、战斗回放和日志相关 EditMode 路径。日志见 `Logs/AlphaSmoke.log`。 |

## 本轮测试阻塞

已处理问题：

- 首次全量 MCP 运行 `29b6b8533c1f42dcac641ffc8c867bb2` 把显式 30 分钟 soak 也拉入执行，并暴露 3 个失败；随后改用 399 条非 soak 门禁补跑。
- 图片 provider 曾在 `CardImageProvider_LoadsExplicitPathsAndCardIdFallbacks` 中返回裁切 sprite `227x317`，期望整图 `430x585`。
- 根因是 `CardImageProvider` 的静态整图缓存可能命中 Unity 已销毁的 `Sprite` 假 null 对象，随后外层退回 `Resources.Load<Sprite>` 的裁切图。
- 修复：缓存命中但 `cached == null` 时移除旧项并重建整图 sprite。复测后 provider 和缓存均返回 `430x585`。

剩余阻塞：

- 手动冒烟脚本尝试用 `execute_code` 驱动真实 UGUI 控件完成入口、卡池弹窗、保存、进入对局、刷新、下一回合、战斗和日志路径。
- 第一次脚本因 `TavernSpellDefinition` 命名空间写错未进入运行；第二次修正后进入运行，但 MCP 返回 `Timeout receiving Unity response`。
- Unity 随后持续显示 `Hold on (busy...)`，`Editor.log` 记录 `MCP-FOR-UNITY: Command TCS timed out` 和 `Cannot access a disposed object. Object name: 'System.Net.Sockets.NetworkStream'`。
- 强制关闭 Unity 后，Console 截图中的 `MCP-FOR-UNITY: Command TCS timed out` 属于 MCP 插件等待命令完成超时，不是训练器业务代码异常。关闭后残留 `Temp/UnityLockfile`，已在确认无 Unity 进程后清理。
- 改用 Unity batchmode 执行 AlphaSmoke EditMode 冒烟后正常退出，`TestResults-AlphaSmoke.xml` 记录 `13/13` 通过。复查时无 Unity 进程、无遗留 `Temp/UnityLockfile`。
- `Logs/AlphaSmoke.log` 末尾有 UnityConnect 请求 `https://public-cdn.cloud.unity3d.com/config/production` 超时；该网络访问不影响测试结果，未计入项目业务失败。

判断：

- 自动化门禁已经给出明确通过结果。
- 脚本式 batch EditMode 冒烟已经通过；真实编辑器视觉冒烟仍不能记为通过。
- 之前的长 `execute_code` 阻塞属于 Unity/MCP 长脚本执行环境问题，不是训练器业务逻辑失败。
- 不能把自动测试通过冒充为人工视觉冒烟通过，尤其小窗口布局、弹窗实际观感仍需补看。

补跑建议：

1. 关闭当前 busy 的 Unity 编辑器后重新打开干净实例。
2. 不再用一个长 `execute_code` 脚本跑完整冒烟，拆成入口/卡池弹窗/进入对局/主流程 4 段。
3. 每段执行后立即截图或记录节点结果，避免 MCP 超时后无法判断卡点。
4. 手动视觉确认 994x384 小窗口下弹窗完整性、文字挤压和滚动加载观感。
5. 30 分钟 soak 单独作为稳定性专项，不纳入日常 v0.1-alpha 发布门禁。

## 手动冒烟清单

当前状态：自动化覆盖和 AlphaSmoke batch 冒烟已通过；真实编辑器手动视觉冒烟仍未完成。发布前仍需要人工确认小窗口视觉、弹窗观感和入口可用性。

| 路径 | 状态 | 备注 |
| --- | --- | --- |
| 入口页打开卡池版本控制弹窗 | 自动覆盖；batch 冒烟通过；人工视觉未完成 | `MainHub_BuildCreatesUnityComponentTavernEntry` 覆盖入口按钮；小窗口视觉仍需人工看。 |
| 复制默认版本并改名 | 自动覆盖；batch 冒烟通过；人工视觉未完成 | 聚焦测试已覆盖改名保存；提示是否足够直观仍需人工看。 |
| 随从筛选等级/种族 | 自动覆盖；batch 冒烟通过；人工视觉未完成 | 聚焦测试已覆盖筛选和图片节点。 |
| 滚动到底部继续加载 | 自动覆盖；batch 冒烟通过；人工视觉未完成 | 聚焦测试已覆盖滚动加载；发布前仍建议人工滚一次。 |
| 批量剔除并保存 | 自动覆盖；batch 冒烟通过；人工视觉未完成 | 聚焦测试已覆盖保存文件结果。 |
| 法术搜索/筛选/剔除 | 自动覆盖仍偏弱；人工视觉未完成 | 法术池路径没有像随从池一样完整拆到独立视觉冒烟，建议下轮分段重点补。 |
| 进入对局后版本生效 | 自动覆盖；batch 冒烟通过；人工视觉未完成 | `MatchService` 过滤测试和卡池 UI 进对局测试已覆盖。 |
| 刷新、购买、出售、下一回合 | 自动覆盖；batch 冒烟通过；人工视觉未完成 | 399 门禁和 AlphaSmoke 覆盖主流程按钮与服务状态；人工连续操作仍需补。 |
| 运行战斗并查看日志 | 自动覆盖；batch 冒烟通过；人工视觉未完成 | 399 门禁和 AlphaSmoke 覆盖战斗与日志面板；人工视觉仍需补。 |

## 卡池报告更新

状态：已更新。

更新位置：`Docs/CardPoolVersionControlBugHuntReport.md`。

本轮更新点：

1. 增加 2026-06-20 卡池相关测试结果。
2. 记录 BGDUO/双打卡已从版本控制候选池和对局可用性中排除。
3. 将“只显示前 100 张”改为“滚动到底部继续加载”，并移入已修复观察项。
4. 保留剩余不足：版本名保存规则、随从/法术共享筛选、缺图提示、批量剔除无撤销。

## 结论

第 1 步功能冻结已完成。

第 3 步卡池已知问题文档已更新。

第 2 步自动化测试已完成：399 条非 soak EditMode 门禁通过，卡池聚焦回归通过，AlphaSmoke batch 冒烟 `13/13` 通过。真实编辑器手动视觉冒烟仍未完成；发布前需要在干净 Unity 实例中分段补跑人工视觉冒烟。
