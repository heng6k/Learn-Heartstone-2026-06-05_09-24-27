# UGUI 整体优化排查与安排

## 结论

当前 UGUI 的主要问题不是 CanvasScaler 没配好，也不是单个按钮或单个面板坏掉。

更准确的根因是：主牌桌、训练工具、机制配置、对手模拟、战斗分析这些都很重要，但它们有时在同一屏里抢同一层视觉优先级。再叠加大量 C# 动态构建 UI、固定尺寸、小字号和隐式导航，界面就容易出现“能用但怪”的感觉。

本轮排查使用的判断标准：

- `game-ui-design`：安全区、可读性、手柄/键盘焦点、动线、触控/选择目标尺寸。
- `unity-developer`：uGUI CanvasScaler、LayoutGroup、Canvas 性能、输入模块、Unity Test Framework 验证。
- `frontend-design`：让视觉结构服务内容，避免模板化堆面板。
- `planning-with-files` / `brainstorm`：把模糊的“奇怪”收敛成可执行批次。

## 已确认现状

- Canvas 配置基本正确：`LearnHearthstoneBootstrap.ConfigureCanvas` 使用 `ScreenSpaceOverlay`、`CanvasScaler.ScaleWithScreenSize`、`1920x1080` reference resolution。
- 输入模块方向正确：Bootstrap 会移除旧 `StandaloneInputModule` 并确保 `InputSystemUIInputModule`。
- 当前 UI 主体集中在 `Assets/LearnHearthstone/Runtime/Presentation`。
- 当前最重的动态 UI 构建文件：
  - `UnityTavernTrainerController.cs`，约 335 KB
  - `UnityTavernTribeSelectionView.cs`，约 146 KB
  - `UnityTavernCombatReplayPanelComponent.cs`，约 114 KB
  - `UnityTavernCardComponent.cs`，约 43 KB
- 结构/命令测试覆盖很厚，但主要验证“对象存在、命令能跑、布局大致可达”。对视觉层级、焦点路线、文字重叠、紧凑窗口体验的验收还不够系统。
- 历史计划里出现过 V2 shell，但当前代码中没有 `*V2*.cs`；现状是主入口走 UnityStyle。这需要和历史文档对齐。

## 本轮补充排查：页面级问题

这次重点看了“用户觉得不舒服、页面被过量占用”的 UGUI 页面。结论是：主桌 Batch 0/1 已经明显改善，但 setup、overlay、弹窗和战斗回放仍有几处高优先级风险。

### P1：对手详情/机制配置 overlay 过重

- 证据：`.planning/tavern-ui-screenshot-requirements/captures/step3-opponent-mechanic-config-preview.png` 显示，对手机制编辑使用接近全屏 overlay，底部仍露出大量空板位/空手牌槽。
- 代码：`UnityTavernTrainerController.BuildOpponentPanelOverlay` 在 compact 下锚点为 `0.03/0.08 -> 0.97/0.92`，标准下为 `0.08/0.12 -> 0.92/0.88`。
- 用户体感：只是换英雄技能、任务奖励、饰品时，页面却像进入了完整桌面编辑器；空槽面积也在抢注意力。
- 建议：把对手详情收成任务面板，默认只展示“摘要 + 机制配置 + 关键按钮”；对手场面/手牌区域改为折叠区或独立标签，不在机制编辑默认页里占主要空间。

### P1：紧凑窗口下若干固定弹窗天然溢出

- `UnityTavernTrainerController.BuildPlayerDirectedChoiceModal` compact 固定为 `650x520`。
- `UnityTavernTrainerController.AdvancedMechanicChoicePanelSize` compact 最小为 `680x500`，目标为 `720x540`，现有测试还在断言高度至少 `500`。
- `UnityTavernCardDetailModalComponent` 固定为 `720x430`，内部卡牌预览固定 `170x330`。
- `UnityTavernMinionEditModalComponent` 固定为 `430x456`。
- 对比目标：项目已把 `994x384` 当作 compact 验收窗口，这些面板高度都超过或接近视口高度。
- 建议：先加 compact fit 断言，再把这些弹窗改为“安全区内最大尺寸 + 内部 ScrollView”，不要继续用固定高度兜底。

### P1：Setup 高级机制页像控制台而不是流程页

- 代码：`UnityTavernTribeSelectionView.BuildAdvancedMechanicsOverlay` 使用大 overlay；`BuildAdvancedMechanicsStrip` 在 compact 下仍有 `300f` 高，标准为 `330f`。
- 用户体感：选种族之后只是确认高级机制，但页面视觉重量接近工具/编辑器。
- 建议：把高级机制默认摘要化，保留三个短行：任务、饰品、畸变。详细池编辑、版本编辑、卡池过滤放入二级 overlay。

### P1/P2：战斗回放底部控件可达但分组弱

- 证据：`.planning/tavern-ui-screenshot-requirements/captures/step5-combat-fullscreen-994x384.png` 中底部把最大轮次、播放、速度、统计、日志、返回放在同一水平条。
- 代码：`UnityTavernCombatReplayPanelComponent.BuildPlaybackBar` 在一个 bar 里构建 `UnityCombatMaxStepsGroup`、`UnityCombatReplayControls`、统计按钮、日志按钮、返回按钮。
- 用户体感：小窗口下按钮都能点，但玩家需要重新判断哪些是播放、哪些是分析、哪个是退出。
- 建议：改为三组：播放组居中、分析/日志组靠右或进 drawer、返回固定为退出组。现有战斗回放测试保留，再补一条小窗口分组断言。

### P2：大目录/版本/卡池 overlay 可以全屏，但缺 compact 护栏

- `UnityTavernTrainerController.BuildCardLibraryOverlay`、`UnityTavernTribeSelectionView.BuildAdvancedPoolEditorOverlay`、`BuildVersionEditorOverlay` 都使用近全屏 panel。
- 这些页面任务本身是“浏览大量内容”，全屏并非错误；问题是它们包含 header、filter、bulk actions、list 多层控件，容易在 compact 下挤压正文。
- 建议：先补截图或结构断言：header 不遮挡 filter，filter 不压缩列表到不可用，关闭按钮始终在安全区内。

## 已完成护栏

- Batch 0/1 已补主桌截图验收：`1920x1080` 和 `994x384`。
- 已补主桌 compact 断言：主行、底部动作条、右侧入口、关键按钮可达。
- 已补基础焦点路线：主操作、工具关闭、右侧抽屉关闭等关键按钮具备可见 focus/selection 状态。

## 问题分级

### P0：先修体验主干

1. 主酒馆桌面的视觉层级
   - 顶部状态条、机制条、牌桌、抽屉按钮和底部动作条都在抢注意力。
   - 目标：主屏第一眼只读到“商店、己方战场、手牌、主要动作”。

2. 键盘/手柄焦点与退出路线
   - 当前有 Input System UI，但缺少显式 `Navigation.Mode`、默认选中、Modal 焦点陷阱/释放策略。
   - 目标：不靠鼠标也能完成主流程，并且每个 overlay 都有明确返回路径。

3. 小窗口可读性
   - 多处 9-13 号字存在于工具、卡牌、机制、战斗面板。
   - 目标：主流程可见文字不低于 14；密集工具区允许小字，但必须在 overlay/scroll/detail 层，不放在主操作层。

### P1：整理信息架构

4. Setup 页渐进流程
   - 当前已经比旧版清楚，但仍像控制台：英雄、种族、卡池、高级机制、进入按钮视觉权重接近。
   - 目标：形成 `英雄 -> 种族/卡池 -> 高级机制 -> 进入酒馆` 的明确动线。

5. 工具/对手/卡牌详情 overlay
   - 对手详情 overlay 能覆盖主桌，这是正确方向；但数据编辑感仍重。
   - 目标：主桌只保留入口和摘要，编辑/配置进入 overlay；overlay 内部再按任务分组。

6. 战斗回放底部控件
   - 全屏战斗已经稳定，但底部把播放、最大轮次、统计、日志、返回混在一排。
   - 目标：播放控制是一组，分析/调试是一组，返回是一组；小窗口下仍清楚。

### P2：清理实现债

7. UI builder 拆分
   - `UnityTavernTrainerController.cs` 承担太多屏幕和 overlay 构建。
   - 目标：按 Tavern table、Tools overlay、Opponent overlay、Choice modal、Status strip 拆出更窄组件。

8. 统一文本/按钮/token 策略
   - `UiFactory` 和 `UnityTavernUiStyle` 已经是好入口。
   - 目标：统一字号层级、按钮优先级、危险/调试/主要动作颜色，减少各处手写。

9. Raycast 与装饰元素
   - 项目已经有不少 `raycastTarget=false`，但还需要按屏幕检查装饰 Image 是否拦截输入。
   - 目标：非交互图层不吃点击，Modal 背景只在需要阻塞时吃点击。

## 推荐修复批次

### Batch 0：验收护栏

状态：已完成初版，后续继续给 overlay/弹窗补护栏。

先补测试和截图护栏，不急着大改 UI。

- 给主酒馆桌面增加 `1920x1080`、`1280x720`、`994x384` 截图验收。
- 增加主流程焦点测试：进入酒馆、打开/关闭工具、打开/关闭对手详情、打开/关闭卡牌详情。
- 增加小窗口断言：底部动作条可见，主桌不被调试 overlay 挤压，关键文本不重叠。

### Batch 1：主桌降噪

状态：已完成主桌第一轮；后续只做回归性微调。

- 顶部机制状态条改成更轻的 chips/摘要，不让它压过商店和战场。
- 右侧 `功能` 抽屉按钮改成稳定侧 rail 或底部工具入口，减少“挡在桌上”的感觉。
- 底部动作条分层：主要动作、经济动作、调试/工具动作不要同权重。
- 空槽样式减弱，避免 sparse board 时满屏 wireframe。

### Batch 2：Setup 页动线

- 将 Setup 页拆成更清楚的流程区块：英雄、种族/卡池、高级机制、进入。
- 高级机制默认摘要化，详情/编辑进入 overlay。
- 保留当前测试覆盖，新增“最少选择路径”和“完整自定义路径”两个 UI 流程测试。

### Batch 3：Overlay 一致性

- 统一 overlay 结构：标题、摘要、主要内容、底部确认/关闭。
- 所有 overlay 都有一致的关闭按钮位置和键盘/手柄返回行为。
- 对手详情从“数据编辑列表”向“对手模拟配置面板”收敛：摘要在主桌，细节在 overlay。

### Batch 4：战斗回放控件重排

- 底部播放控件与分析控件分组。
- 小窗口下隐藏低频分析按钮到二级 drawer，保留播放/跳过/返回。
- 保持现有 `UnityCombatReplayPanelAcceptanceTests`，扩展一条小窗口视觉结构断言。

### Batch 5：组件化与样式 token

- 从 `UnityTavernTrainerController` 抽出低风险 UI builder，不改业务命令。
- 在 `UnityTavernUiStyle` 增加文本层级和按钮角色：
  - primary action
  - economy action
  - danger/combat action
  - debug/tool action
  - muted secondary action
- 清理主屏 14px 以下文字；小字只保留在 detail/tool 层。

## 暂不建议

- 不建议现在迁移 UI Toolkit。当前项目 uGUI 测试和组件已经很多，迁移会扩大风险。
- 不建议重开一个全新 V2 shell，除非先决定放弃当前 UnityStyle 主线。
- 不建议追求复刻炉石视觉。可以借鉴信息架构，但素材、框体、动效应保持原创。

## 下一步建议

下一步最划算的是先做一个小的“Batch 2A：紧凑弹窗护栏”，再进入 Setup 页重排：

1. 给 `994x384` 增加对手机制 overlay、卡牌详情、随从编辑、玩家自由选择、高级机制选择的 compact fit 断言。
2. 先修固定高度溢出的弹窗：`PlayerDirectedChoice`、`AdvancedMechanicChoice`、`CardDetail`、`MinionEdit`。
3. 再做对手详情 overlay 的任务分组，把默认页从“完整桌面编辑”收成“机制配置面板”。
4. 最后进入 Batch 2/3/4：Setup 高级机制摘要化、Overlay 统一、战斗回放控件分组。

这样能继续减少“页面被 UI 吃掉”的体感，同时不冒险重写整套 UGUI。
