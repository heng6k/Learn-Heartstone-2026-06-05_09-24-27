# 卡池版本控制找茬测试报告

## 测试身份

这轮测试按一个狂热游戏爱好者的心态来做：不是只看功能有没有按钮，而是故意去点容易误伤卡池、容易混淆保存状态、容易让版本变得不可复现的路径。

## 覆盖范围

- 入口页卡池版本弹窗模式。
- 自定义版本复制、命名、保存。
- 随从与酒馆法术的勾选开关。
- 等级筛选、种族/通用分类筛选、搜索。
- 当前筛选结果批量剔除与加入。
- 卡牌行图片缩略图显示。
- 版本配置进入对局时传递给 `MatchSetupOptions`。

## 已执行验证

- Runtime 离线编译通过。
- Tests 离线编译通过。
- 新增 EditMode 用例：`TribeSelectionView_CardPoolModalRenamesFiltersAndBulkExcludes`，覆盖复制默认版本、版本重命名、按等级/种族筛选、卡牌图片节点、批量剔除和保存文件结果。
- Unity EditMode 目标测试 `TribeSelectionView_CardPoolModalRenamesFiltersAndBulkExcludes` 通过：`Passed: 1, Failed: 0`。
- Unity EditMode 回归测试 `TribeSelectionView_CardPoolPanelCopiesDefaultAndPassesCustomSetup` 通过：`Passed: 1, Failed: 0`。
- Unity EditMode 目标测试 `TribeSelectionView_CardPoolModalPromptsBeforeSwitchingUnsavedVersion` 通过：`Passed: 1, Failed: 0`。
- 2026-06-20 复测 `TestResults-CardPool-EntryUI.xml`：`Total: 1, Passed: 1, Failed: 0`。
- 2026-06-20 复测 `TestResults-CardPool-MatchService.xml`：`Total: 1, Passed: 1, Failed: 0`。
- 2026-06-20 图片加载回归 `CardImageProvider_LoadsExplicitPathsAndCardIdFallbacks` 通过：Unity MCP job `9371e8c593fa42c2a6b417c6c3ab9479`，`Total: 1, Passed: 1, Failed: 0`。
- 2026-06-20 卡池聚焦回归通过：Unity MCP job `3ec90c6cea044de6b0cc9c976fdf0ee6`，`Total: 4, Passed: 4, Failed: 0`。
- 2026-06-20 EditMode 门禁通过：Unity MCP job `92183e883d5e4ceaa1ae33c715923453`，`Total: 399, Passed: 399, Failed: 0, Skipped: 0`。本轮发布门禁排除显式 30 分钟 soak：`RobustnessEdgeTests.ThirtyMinuteExtremeCombatAndRecruitSoak_MaintainsBounds`。
- 2026-06-20 AlphaSmoke batch 冒烟通过：`TestResults-AlphaSmoke.xml`，`Total: 13, Passed: 13, Failed: 0, Skipped: 0`。覆盖入口、卡池版本弹窗、改名、筛选、滚动加载、剔除保存、进入对局、刷新、下一回合、战斗回放和日志相关 EditMode 路径。
- 2026-06-20 手动冒烟脚本尝试执行真实 UGUI 路径，但被 `execute_code` 超时和 Unity busy 阻塞；不能记为人工视觉冒烟通过，需在干净 Unity 实例中分段补跑。

## 本轮修复补充

- 勾选单张卡牌、批量剔除、批量加入后会标记“未保存”。
- 保存按钮在未保存时显示为 `保存*` 并高亮，保存后恢复禁用状态。
- 切换版本前如果有未保存卡池改动，会弹出确认框：`保存并切换`、`放弃修改`、`取消`。
- 版本控制候选池排除双打/BGDUO 卡牌，默认卡池和进入对局后的 `CardPoolAvailability` 保持一致。
- 卡牌列表从一次性显示前 100 张改为初始 100 张、滚动到底部继续加载，避免误以为后续卡牌不存在。
- 批量操作区显示范围提示，说明操作会影响当前筛选的全部卡牌；当前可见列表只是已加载窗口，不代表完整筛选结果。
- 酒馆法术、英雄、英雄技能和伙伴图片走整图加载；同时修复 `CardImageProvider` 静态缓存命中 Unity 已销毁 `Sprite` 假 null 后退回裁切图的问题。复测 `BG28_168` 返回 `430x585`，不再返回裁切 sprite 的 `227x317`。

## 剩余不足

| 严重度 | 问题 | 影响 | 建议 |
| --- | --- | --- | --- |
| 中 | 版本名输入框是失焦即保存，但卡牌勾选是手动保存。 | 保存规则仍不完全一致，玩家可能需要适应。 | 后续可在版本名旁显示“已保存”，或把版本名也改成显式保存。 |
| 低 | 随从和法术共用同一组等级/种族筛选。 | 从随从切到法术时可能突然 0 张，让玩家以为法术池坏了。 | 切换标签时重置分类筛选，或为随从/法术分别记忆筛选。 |
| 低 | 缩略图缺图时只显示“无图”。 | 可以识别缺图，但不方便定位资源缺口。 | 缺图时显示卡牌编号，并在资源审计文档里汇总缺图列表。 |
| 低 | 批量剔除没有撤销按钮。 | 大量剔除后只能靠“加入当前筛选”局部恢复，不适合连续试错。 | 增加“撤销上次批量操作”或“还原到上次保存”。 |

## 已修复但需要继续观察

| 问题 | 当前状态 | 复测重点 |
| --- | --- | --- |
| 列表只显示前 100 张，用户容易以为卡池被截断。 | 已改为滚动到底部继续加载。 | 在随从和法术标签下分别滚动到底，确认加载后筛选、勾选、批量剔除仍作用于完整筛选集合。 |
| 双打卡混入版本筛选候选池。 | 已从默认版本、版本编辑候选池和对局可用性判断中排除 BGDUO。 | 搜索 `BGDUO` 或双打专属卡名，确认不会出现在版本编辑和实际商店池。 |
| 酒馆法术图片被裁切。 | 已改为整图加载，并修复静态缓存中的失效 sprite 命中。 | 长时间开着 Unity、经历 domain reload 后，重新打开卡池弹窗确认法术缩略图仍是整图比例。 |
| 卡池弹窗路径只靠单点测试，担心串起来后漏问题。 | AlphaSmoke batch 已把入口、弹窗、筛选、滚动加载、保存和进对局串成 13 条 EditMode 冒烟路径。 | 仍需人工在小窗口下看真实视觉表现，尤其文字挤压、提示可见性和滚动手感。 |

## 复测建议

1. 在 994x384 一类小窗口下手动打开版本控制弹窗，重点看筛选按钮是否挤压、文字是否换行异常。
2. 用“全部筛选 + 批量剔除”做一次压力路径，确认保存后进入对局不会生成被剔除的随从和法术。
3. 手动验证 `放弃修改` 分支的视觉反馈，确认玩家能明显意识到当前版本改动被丢弃。
4. 从列表顶部滚动到底部触发继续加载，确认图片、勾选状态和搜索/分类筛选不会错位。
