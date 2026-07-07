# Battlegrounds UI V2 Research And MCP Plan

## 目的

这份文档记录 Tavern Trainer / Battlegrounds 风格 UI V2 的调研结论、参考来源、推荐 skill 组合和后续 MCP 实施路径。它用于后续继续重做 UI 时快速恢复上下文，避免重新争论“复制旧 UI 还是开新 UI”“UGUI 还是 UI Toolkit”等方向问题。

## 核心结论

后续适合直接开一个新的 UGUI V2 壳子，用 MCP 截图边看边改。

不建议复制旧 UI 后再修改。旧 UI 的主要问题是信息架构和布局骨架本身不清晰，复制会把这些问题一起带到新入口里，最后容易变成两套难维护的旧 UI。

不建议第一刀切到 UI Toolkit。项目现有 Tavern Trainer 界面明显基于 UGUI，短期换 UI Toolkit 会变成基础设施迁移，反而拖慢视觉救火和 MCP 直观迭代。

## 为什么继续用 UGUI

项目当前已经具备 UGUI 基础：

- `Packages/manifest.json` 包含 `com.unity.ugui`。
- Tavern Trainer presentation 代码大量使用 `UnityEngine.UI`、`Canvas`、`HorizontalLayoutGroup`、`VerticalLayoutGroup`。
- 现有 UI 测试和 MCP 运行路径都围绕 Unity UGUI 运行。
- 继续用 UGUI 可以复用现有 `MatchService`、命令处理、卡牌数据、图片加载、focused EditMode 测试和组件 helper。

UI Toolkit 可以作为长期技术选项，但不适合作为本轮 V2 的第一刀。

## 参考方向

| 来源 | 用途 | 注意事项 |
| --- | --- | --- |
| Hearthstone Battlegrounds 官方页面 | 参考“中央战场 + 商店 + 卡牌池”的信息结构和题材语境 | 只参考结构，不复制受保护美术 |
| Firestone | 参考 Hearthstone companion 工具的信息密度、overlay 和工具面板组织 | 不是 Unity 实现参考 |
| Hearthstone Deck Tracker | 参考成熟状态追踪、历史、统计和侧边信息组织 | 适合右侧信息/日志/报告抽屉 |
| Unity card-game prototype | 轻度参考 Unity 卡牌/棋盘结构 | 不作为架构主参考 |
| 低星 Battlegrounds/HDT 插件 | 只作为窄功能观察 | 不适合当 V2 架构参考 |

## 设计方向

V2 应该是新的信息架构，而不是旧 UI 的视觉补丁。

建议第一版使用原创 tavern-table 视觉语言：

- 深木桌面作为主背景。
- 黄铜或暗金分割线用于层级和重点，而不是到处铺金色。
- 羊皮纸感的浅面板用于说明、报告、日志等阅读型内容。
- 卡牌图片作为第一视觉信号，文本只保留决策必需信息。
- 数值 chip 用清晰语义色区分金币、生命、等级、回合、冻结、战斗等状态。

避免直接照抄 Hearthstone 的 UI 美术、图标、框体和具体装饰。

## V2 主桌面骨架

第一刀建议只做主桌面骨架，不同时迁移所有弹窗和工具。

推荐结构：

```text
Top Status Strip
  Player / Opponent / Round / Gold / Tavern Tier / Health

Center Table
  Opponent board summary
  Shop row
  Player warband board

Bottom Rail
  Hand
  Refresh / Freeze / Upgrade / End Turn / Combat Test / Replay / Tools

Right Drawer
  Selected card
  Actions
  Advisor
  Logs
  Mechanic Coverage / debug tools
```

布局优先级：

1. 中央战场和商店优先。
2. 手牌和主要行动保持常驻。
3. 右侧工具、日志、报告默认可折叠。
4. 详细信息用 drawer/modal，不挤占主桌面。

## MCP 实施路径

1. 新建 V2 UGUI entry/shell，同时保留旧 UI 作为 fallback。
2. 第一轮只实现 MCP 可见的主桌面布局骨架。
3. 使用占位原创视觉材料：桌面、分割线、卡槽、status chip、右侧抽屉。
4. 每完成一小块就用 MCP 截图检查：
   - 桌面是否非空。
   - 卡牌/面板是否可见。
   - 文本是否重叠或截断。
   - 小窗口下主操作是否仍可见。
5. 同步补 focused EditMode UI 测试，锁住入口、关键节点和布局结构。
6. V2 壳子可用后，再逐块迁移 tools modal、card library、combat replay、selection modal。
7. 最终只保留一个真实入口，避免长期双维护。

## 推荐 skill 和工具组合

主用本地 skill：

- `planning-with-files`：维护 V2 迁移计划、进度、错误记录。
- `frontend-design`：定视觉方向、色彩/材质/布局概念。
- `ui-ux-pro-max`：检查信息层级、间距、交互状态、可读性和可访问性。
- `Confidence Check`：实现前确认没有重复入口、架构方向正确、根因清楚。
- `@ponytail`：编码时保持最小正确改动，不把 V2 做成新大泥团。

可选外部 skill：

- `rmyndharis/antigravity-skills@unity-developer`：`npx skills find "unity ui"` 中安装量约 2.3K，可作为 Unity 开发参考。
- `omer-metin/skills-for-antigravity@game-ui-design`：`npx skills find "game ui"` 中安装量约 2K，可作为游戏 UI 设计参考。

不建议当前安装 UI Toolkit 专门 skill。相关结果安装量较低，而且项目当前不走 UI Toolkit 主线。

## 第一刀验收标准

第一刀只验收 V2 主桌面骨架：

- 旧 UI 仍可回退。
- V2 入口能打开，不影响现有 `MatchService` 逻辑。
- 顶部状态、中央战场、商店、手牌、底部行动、右侧抽屉都能在 MCP 截图中清楚看到。
- 小窗口下主操作不消失，主桌面不被右侧信息挤爆。
- 不复制 Hearthstone 受保护美术。
- focused EditMode UI 测试通过。
- 不跑 full EditMode，除非这轮改动进入主入口并影响广泛 UI 行为。

## 开工前检查

开工前建议确认：

- 当前 worktree 干净，或明确哪些改动属于本轮。
- Unity 和 MCP 连接稳定。
- 新建独立 planning 目录，例如 `.planning/tavern-ui-v2-shell`。
- 先做 Confidence Check，再动代码。
- 每轮截图和测试结果写入 planning progress。

## Sources

- [Hearthstone Battlegrounds](https://hearthstone.blizzard.com/en-us/battlegrounds)
- [Firestone](https://github.com/Zero-to-Heroes/firestone)
- [Hearthstone Deck Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker)
- [CardGameHexBoard](https://github.com/ycarowr/CardGameHexBoard)
- [Unity UI systems comparison](https://docs.unity3d.com/Manual/UI-system-compare.html)
- [Unity UI Toolkit manual](https://docs.unity3d.com/Manual/UIElements.html)
- [Unity UGUI manual](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/index.html)
- [Unity 2D Sprite manual](https://docs.unity3d.com/Manual/com.unity.2d.sprite.html)
- [Unity Sprite Atlas reference](https://docs.unity3d.com/Manual/class-SpriteAtlas.html)

