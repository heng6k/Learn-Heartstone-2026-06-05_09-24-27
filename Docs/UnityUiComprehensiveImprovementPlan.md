# Unity UI 全面改进文档

## 目标

把当前 `Learn Heartstone` 的 Unity 界面从“能用但拥挤、信息堆叠、难以继续扩展”的状态，改成一个稳定、清晰、可持续迭代的训练器界面。

改进重点不是重写玩法逻辑，而是重整 UI 体验、布局系统、组件边界和验证流程。项目范围仍遵循 `PROJECT_SCOPE.md`：只做单人酒馆训练体验，不引入 Duos 系统。

## 当前审计结论

### Unity/MCP 状态

- Unity 项目已通过 MCP 连接，Editor 状态可读，当前场景是 `Assets/Scenes/SampleScene.unity`。
- 场景本体很轻：`Main Camera`、`Global Light 2D`、`LearnHearthstoneBootstrap`，Play Mode 运行后会生成 `EventSystem` 和 `LearnHearthstoneCanvas`。
- Console 在本次审计开始时只有 MCP 启动日志，没有项目编译错误。
- MCP 对 `Screen Space - Overlay` UI 的截图不稳定：摄像机截图会漏掉 Overlay UI。因此后续 UI 验证应补一个专用截图/布局测试工具，而不是只依赖摄像机截图。

### 运行时证据

在当前 Game View 下，Canvas 运行尺寸为：

- `pixelRect`: `994 x 384.5`
- `CanvasScaler.scaleFactor`: `0.4293178`
- `referenceResolution`: `1920 x 1080`
- `matchWidthOrHeight`: `0.5`

这个数据直接解释了“界面很乱”的一部分根因：很多 UI 尺寸是以 1920x1080 设计的固定逻辑像素，但在当前短屏 Game View 中被缩到 43%。例如：

- 主菜单按钮逻辑高度约 `34`，实际屏幕高度只有约 `14.6` 像素。
- 主菜单模块卡片逻辑尺寸 `260 x 128`，实际约 `112 x 55` 像素。
- 主菜单标题 `34` 号字体实际显示非常小。

训练器界面源码也存在同类风险：

- 顶栏固定 `78` 高。
- 主区域固定上下边距 `18` 和 `92`。
- 右侧浮动面板固定约 `450` 宽。
- 商店区、棋盘区、手牌区使用多个固定高度，例如 `168`、`236`。
- 卡牌 prefab 固定 `128 x 184` 或 `112 x 126`。
- 回放面板内部棋盘固定 `360` 高。
- 调试/工具弹窗使用大量固定行高和固定列数。

这些固定值在 16:9 大窗口下能勉强成立，但在 Unity Editor 常见的窄高/短高 Game View 下会明显拥挤。

## 当前 UI 结构

### 入口链路

- `Assets/LearnHearthstone/Runtime/Presentation/LearnHearthstoneBootstrap.cs`
  - 创建/配置 Canvas。
  - 创建 `MatchService` 和 `LocalAdvisorService`。
  - 负责在 Main Hub、旧训练器、Realistic 训练器、Unity 训练器之间切换。

- `Assets/LearnHearthstone/Runtime/Presentation/MainHub/MainHubView.cs`
  - 构建主菜单。
  - 当前使用固定 3 列 `GridLayoutGroup`。
  - 多个未开放模块也占据首屏空间。

- `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernTrainerView.cs`
  - 实例化 `UnityTavernRoot.prefab`，或 fallback 到代码生成 root。
  - 挂载 `UnityTavernTrainerController`。

- `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernTrainerController.cs`
  - 当前 UI 的主要复杂度集中点。
  - 负责背景、顶栏、棋盘、商店、手牌、右侧面板、弹窗、回放、调试工具、Toast、拖拽等。
  - 每次 `Rebuild()` 会清空并重建一大段 UI。

- `Assets/LearnHearthstone/Editor/UnityTavernPrefabBuilder.cs`
  - 生成 `UnityStyle/Prefabs` 下的根 prefab、卡牌 prefab、区域 prefab、面板 prefab、弹窗 prefab、回放 prefab。

### 优点

- 项目已经从纯手写 UI 走向 prefab 化，方向是对的。
- Domain/Application/Presentation/Test 分层清晰，UI 改造不需要碰太多玩法核心。
- EditMode 测试数量多，后续可以加 UI 结构测试和布局测试。
- `UiFactory`、`UnityTavernUiStyle` 已经提供了初步统一入口。

### 主要问题

1. 布局没有响应式规则

当前界面主要依赖固定宽高、固定列数、固定区域高度。Unity Editor Game View 一旦不是理想比例，按钮、卡牌、文字、面板都会被压得过小。

2. 信息架构不够聚焦

训练器需要玩家同时理解商店、己方棋盘、对手棋盘、手牌、金币、酒馆等级、日志、建议、选中卡详情、战斗回放、调试工具。现在这些区域更像“全部摆出来”，缺少明确主次。

3. 主菜单浪费首屏空间

主菜单当前显示 5 个模块，其中 4 个是未开放入口。对于这个项目，用户多数情况下是进入酒馆训练器，主菜单应该更直接，把未开放模块弱化。

4. 右侧面板在小窗口下成本太高

固定 450 宽的右侧面板会挤压主棋盘区域。对训练器这种横向信息密集界面，小窗口下应改成 drawer、tabs、底部抽屉或可切换面板，而不是始终占据横向空间。

5. 卡牌信息过载

卡牌上同时承载 art、tier、种族、名字、副标题、攻血、费用、操作按钮。尺寸被压缩后，所有文本都会竞争空间。应明确卡牌本体只显示战斗决策必需信息，把详情放入选中面板或 hover/长按详情。

6. 视觉语言偏单一

当前色彩集中在深绿、深棕、暗青、金色，整体接近单一暗色酒馆风。需要保留酒馆氛围，但用更明确的语义色和层级对比区分：

- 可操作
- 已选中
- 不可用
- 危险
- 收益
- 战斗事件
- 调试/工具态

7. 组件边界仍然偏粗

`UnityTavernTrainerController` 承担了过多职责。虽然已有若干 component prefab，但主控制器仍直接管理大量 layout、业务操作、弹窗内容和拖拽状态。继续迭代会越来越难。

8. 缺少 UI 验证标准

当前测试主要覆盖逻辑和部分 View 行为，缺少明确的布局验收：

- 不同分辨率下是否有重叠。
- 按钮实际屏幕高度是否可点。
- 文本是否截断到不可读。
- 关键操作是否在首屏可见。
- Console 是否干净。

## 改进原则

1. 训练器优先，不做展示型主页

第一屏应优先让用户进入训练和操作，而不是展示多个未开放模块。

2. 棋盘和商店是主舞台

酒馆训练器的核心是“看局面、买卖、摆位、战斗验证”。主区域应优先保证商店、己方棋盘、手牌和关键操作可读。

3. 信息分层，不同状态放在不同层

- 常驻层：金币、回合、生命、酒馆等级、冻结/刷新/升级等主操作。
- 选择层：选中卡详情、可执行操作、目标提示。
- 辅助层：建议、日志、回放入口。
- 工具层：调试发牌、过滤器、场景保存等。

4. 小窗口下不要硬塞

当高度不足或宽度不足时，应折叠辅助信息，而不是继续缩小所有内容。

5. Prefab 化和代码生成要分工明确

Prefab 负责稳定结构、视觉样式、引用绑定；代码负责填充数据和响应事件。不要让 prefab builder 和 runtime controller 长期双写同一套布局细节。

## 推荐目标形态

### 主菜单

建议把 Main Hub 改成“直接入口 + 次级入口”：

- 首屏主操作：`进入酒馆训练器`
- 次级操作：旧版训练器、Realistic 训练器、数据浏览、设置
- 未开放模块不占主网格，放到“即将推出”小区域或隐藏

主菜单布局：

- 大窗口：左侧项目标题，右侧主入口和最近测试场景。
- 中窗口：单列垂直按钮组。
- 短屏：只保留标题、主入口、次级入口折叠按钮。

### 训练器主界面

推荐结构：

```text
Top Bar
  回合 / 金币 / 等级 / 生命 / 设置 / 返回

Main Play Area
  Opponent Board   optional compact
  Shop             primary
  Player Board     primary
  Hand             primary

Action Rail / Bottom Bar
  刷新 / 冻结 / 升级 / 结束回合 / 战斗测试 / 回放

Inspector Drawer
  选中卡详情 / 可用操作 / 建议 / 日志 tabs

Tool Modal
  调试发牌 / 过滤 / 场景存取 / 对手编辑
```

### 响应式断点

建议先定义三个断点，避免每个组件各自判断：

| 模式 | 条件 | UI 行为 |
| --- | --- | --- |
| Wide | 宽 >= 1400 且高 >= 800 | 右侧 Inspector 可常驻，棋盘/商店完整显示 |
| Standard | 宽 >= 1000 且高 >= 650 | Inspector 默认折叠，操作栏固定，日志进 tab |
| Compact | 宽 < 1000 或高 < 650 | 辅助面板改底部抽屉，减少对手区高度，卡牌信息压缩 |

当前审计窗口 `994 x 384.5` 应被归类为 `Compact`。在这个模式下，不应继续展示完整高度的主菜单卡片、右侧面板或完整回放面板。

## 具体改进计划

### Phase 0：稳定审计和验证工具

目标：先保证之后每次改 UI 都能被稳定验证。

任务：

- 增加一个 UI 布局审计工具，输出当前 Canvas、所有关键 RectTransform、按钮实际屏幕尺寸、文本截断风险。
- 增加固定分辨率测试入口：`1920x1080`、`1366x768`、`1280x720`、`1000x600`、`994x384`。
- 为 MCP 截图问题建立替代方案：用 `ScreenCapture.CaptureScreenshot` 或专门的 PlayMode capture helper 捕获 Overlay UI。
- 记录 baseline 截图到 `Temp` 或测试输出目录，不放进 `Assets`。
- 确认 Play Mode 退出后 Console 无新增项目错误。

验收：

- 能在不手动观察的情况下得到每个分辨率的布局报告。
- 任一关键按钮实际屏幕高度低于 32 像素时，报告失败。
- 任一关键面板超出屏幕边界时，报告失败。

### Phase 1：Canvas 和布局基础修正

目标：解决“整体被缩得太小”的基础问题。

任务：

- 在 `LearnHearthstoneBootstrap.ConfigureCanvas` 中评估 CanvasScaler 策略。
- 对短高 Game View 增加最低可读 scale 或 Compact 布局，而不是继续全局缩小。
- 为 UI 定义统一 spacing/token：
  - `SpacingXs = 4`
  - `SpacingSm = 8`
  - `SpacingMd = 12`
  - `SpacingLg = 18`
  - `TouchHeight = 44`
  - `CompactTouchHeight = 52`
- 将按钮、tabs、chips 的最小高度统一到 token，不再散落 `30`、`32`、`34`、`40`。
- 把主菜单 Grid 从固定 3 列改为自适应列数。
- 对高度不足模式启用 ScrollView 或折叠区。

建议修改文件：

- `LearnHearthstoneBootstrap.cs`
- `UiFactory.cs`
- `UnityTavernUiStyle.cs`
- `MainHubView.cs`

验收：

- `994 x 384.5` 下主菜单按钮仍可读可点。
- `1280 x 720` 下主菜单不出现大面积空洞或拥挤。
- 不开放模块不会抢占主要入口空间。

### Phase 2：训练器信息架构重排

目标：让玩家第一眼知道该看哪里、点哪里。

任务：

- 将 `BuildPlaySurface` 重构为 Board/Shop/Hand 的核心布局，不让右侧面板默认挤压主区。
- 把右侧面板改为 `InspectorDrawer`：
  - Wide 模式常驻。
  - Standard/Compact 模式默认折叠。
  - 内部用 tabs：`详情`、`操作`、`建议`、`日志`。
- 将 `BuildActionStripPrefab` 从右侧面板中独立出来，放到底部或商店附近。
- 让选中卡详情与动作按钮绑定，减少卡牌本体上的文字和按钮。
- 对手棋盘在 Compact 模式下可变为较矮的摘要区，进入战斗/回放时再展开。

建议修改文件：

- `UnityTavernTrainerController.cs`
- `UnityTavernRightPanelComponent.cs`
- `UnityTavernActionPanelComponent.cs`
- `UnityTavernSelectedCardPanelComponent.cs`
- `UnityTavernZoneComponent.cs`

验收：

- 玩家在 3 秒内能找到刷新、冻结、升级、结束回合。
- 玩家选中一张卡后，详情和可用操作在同一视觉区域出现。
- 日志和建议不再长期挤占主棋盘空间。

### Phase 3：卡牌和区域组件精简

目标：让卡牌可读，区域边界清楚。

任务：

- 定义卡牌信息优先级：
  - 始终显示：名字、攻血/费用、等级、种族/法术类型。
  - 条件显示：关键词摘要，最多 2 个。
  - 移到详情面板：完整描述、长效果、调试信息。
- 商店卡和棋盘随从使用不同密度：
  - 商店卡重视购买决策，保留费用/等级/名字。
  - 棋盘随从重视战斗，优先攻血/关键词/嘲讽等状态。
- 卡牌 art 区域不应挤压操作按钮；操作按钮放到选中区或底部操作栏。
- Zone header 保留简短状态，例如 `商店 5/5`、`己方 3/7`，减少长文本。
- Compact 模式下减少卡牌内字号种类，避免 9/10/11/12 混杂。

建议修改文件：

- `UnityTavernCardComponent.cs`
- `UnityTavernZoneComponent.cs`
- `UnityTavernPrefabBuilder.cs`

验收：

- 七格棋盘满员时，卡牌之间不重叠。
- 一张长中文卡名不会盖住攻血或按钮。
- 选中态、可拖拽态、不可操作态清晰可区分。

### Phase 4：弹窗、工具和回放整理

目标：把复杂功能留住，但不让它们压垮主界面。

任务：

- `TrainerToolsModal` 改成分区 tab：
  - 添加卡牌
  - 过滤器
  - 对手编辑
  - 场景存取
- 工具弹窗内部所有列表必须可滚动，不能靠固定高度硬撑。
- 回放面板应优先展示当前帧、双方棋盘和播放控制，timeline 可折叠。
- 回放事件 chips 使用语义色，但减少同屏 chips 数量，避免一行塞太多状态。
- 关闭按钮、播放按钮、速度按钮使用稳定尺寸，并在 Compact 模式下换成图标/短标签。

建议修改文件：

- `UnityTavernToolsModalComponent.cs`
- `UnityTavernCombatReplayPanelComponent.cs`
- `UnityTavernPrefabBuilder.cs`

验收：

- 工具弹窗在 `1000 x 600` 下不越界。
- 回放面板在没有回放帧时也保持空态清楚。
- 回放播放、上一帧、下一帧、速度切换不会因为长文本挤压。

### Phase 5：视觉系统统一

目标：保留酒馆风格，但让 UI 更专业、更清楚。

任务：

- 扩展 `UnityTavernUiStyle` 为真正的 design tokens：
  - 颜色：背景、表面、表面高亮、边框、文本、弱文本、主操作、危险、收益、选中。
  - 字号：标题、区域标题、正文、小字、徽章。
  - 间距：xs/sm/md/lg。
  - 尺寸：按钮高度、chip 高度、卡牌最小尺寸。
- 减少同屏相近暗色面板，增加边框/分割线/阴影层级。
- 所有可点击控件必须有 hover/pressed/disabled 视觉差异。
- 所有禁用入口要明显弱化，不使用与可点入口相近的样式。
- 文本颜色避免只靠金色表达重点；金色应留给资源、奖励、重要提示。

建议修改文件：

- `UnityTavernUiStyle.cs`
- `UiFactory.cs`
- `UnityTavernPrefabBuilder.cs`
- 各 `UnityTavern*Component.cs`

验收：

- 不看文字也能区分主操作、次操作、危险操作、不可用操作。
- 一屏内主要面板层级清楚，不是所有区域都像同一张暗色卡片。
- 颜色不会只呈现单一深棕/深绿主题。

### Phase 6：组件边界和重建策略

目标：降低 `UnityTavernTrainerController` 继续膨胀的风险。

任务：

- 将 controller 分为更小的 presenter/binder：
  - `TavernTopBarPresenter`
  - `TavernBoardPresenter`
  - `TavernShopPresenter`
  - `TavernHandPresenter`
  - `TavernInspectorPresenter`
  - `TavernModalCoordinator`
- `Rebuild()` 不再每次清空全树，改为局部刷新：
  - 资源变化只刷新 TopBar。
  - 选中卡变化只刷新 Inspector。
  - 买卖/摆位只刷新相关 zone。
  - 打开弹窗只创建/销毁弹窗层。
- 将 layout 决策集中到一个 `UnityTavernLayoutMode` 或 `UnityTavernLayoutContext`，避免每个组件各自判断屏幕尺寸。
- Prefab references 缺失时可以 fallback，但应输出明确 warning，避免静默走 generated UI。

建议修改文件：

- `UnityTavernTrainerController.cs`
- 新增若干 presenter/component 文件
- `UnityTavernPrefabBuilder.cs`
- `UnityTavernTrainerViewTests.cs`

验收：

- 一个普通按钮点击不再重建整个训练器树。
- 新增一个面板不需要修改巨大 controller 的多个远距离方法。
- Prefab 丢引用时测试能失败或日志明确提示。

## 推荐实施顺序

1. 先做 Phase 0 和 Phase 1。

这两步能最快改善“乱”和“太小”的底层问题，也能建立之后不反复退化的验证手段。

2. 再做 Phase 2。

信息架构重排会带来最大主观改善：玩家知道该看哪里、该点哪里。

3. 再做 Phase 3 和 Phase 4。

卡牌、弹窗、回放属于复杂局部，应该在基础布局稳定后逐个改。

4. 最后做 Phase 5 和 Phase 6。

视觉系统和组件边界可以渐进做，但需要贯穿所有后续 UI 变更。

## 第一轮可执行任务清单

第一轮建议只做低风险、收益高的改动：

- 新增 `UnityTavernLayoutContext`，集中计算 `Wide/Standard/Compact`。
- 修改 `MainHubView`：
  - 主入口放大。
  - 未开放模块弱化或折叠。
  - 固定 3 列改为按宽度计算列数。
- 修改 `LearnHearthstoneBootstrap.ConfigureCanvas`：
  - 保留 1920x1080 参考，但对短屏增加 Compact 兜底。
- 修改 `UiFactory.Button`：
  - 统一按钮最小高度。
  - 禁用态更明显。
- 修改 `UnityTavernTrainerController.BuildFloatingRightPanel`：
  - Compact 模式默认折叠。
  - 右侧面板宽度改为按屏幕比例和最大值计算。
- 新增 EditMode 测试：
  - MainHub 在 `994x384` 模拟尺寸下主入口可读。
  - 关键按钮屏幕高度不低于阈值。

## 当前执行进度

- 已完成第一轮基础改造：新增 `UnityTavernLayoutContext`，让 Canvas、MainHub、右侧面板和底部主操作栏进入 `Wide/Standard/Compact` 布局路径。
- 已完成 Phase 2 前半段：右侧 Inspector 默认折叠并改为 tabs，刷新/冻结/升本/下回合/战斗/回放/工具移动到底部主操作栏。
- 已完成本轮补充：训练器主区域的对手棋盘、商店、玩家棋盘、手牌改为从 `UnityTavernLayoutContext.ZoneMetrics` 获取高度、槽位尺寸、槽位间距和 Compact 卡牌缩放。
- 已完成卡牌图片入口整理：`CardImageProvider` 统一解析 `ImagePath`、`CardId`、`CardImages` 和 `CardImages/TavernSpells`，UnityStyle/Realistic 两套卡牌视图共用同一套图片加载规则。
- 当前卡牌图片策略：真实卡图放在本地 `Assets/LearnHearthstone/Resources/CardImages/<CardId>.png`；酒馆法术可放在 `Assets/LearnHearthstone/Resources/CardImages/TavernSpells/<CardId>.png`；有完整卡图时优先显示完整卡面并减少叠加文字，缺图时才显示稳定的种族/法术占位。
- 已完成全卡图交互微调：有完整卡图时，购买/出售/使用按钮改为右上角小型 chip，不再遮挡卡面底部的攻血、费用或效果文字；缺图卡仍保留底部按钮和占位信息。
- 已完成全卡图随从战斗 HUD 修复：即使随从使用完整本地卡图，也会常驻显示酒馆等级、攻击、生命和最多 3 个关键词；关键词会映射为中文短标签并使用底部紧凑色条，保证训练时不需要依赖卡图原文字号。
- 已完成拖放反馈增强：拖起卡牌时，合法落点使用手牌/棋盘/出售区的语义色提示，非法落点轻微压暗，拖放结束后自动恢复，减少玩家试错。
- 已完成区域视觉层级增强：商店、己方棋盘、对手棋盘、手牌改为不同语义底色，header 增加左侧色标，row 和 slot 使用分层底色，减少四个区域同质化造成的视觉拥挤。
- 已完成右侧功能面板内部视觉优化：面板根节点、标题栏、标签页、功能内容宿主、动作/详情/建议/日志子面板统一增加 UGUI surface/outline 层级；当前标签具备明确高亮，动作按钮网格高度提升到 40，建议区高度放宽，减少右侧抽屉内部拥挤感。
- 已完成右侧功能面板子内容优化：详情页把选中卡、牌面摘要、效果文本和详情操作整合成紧凑 inspector；建议页和日志页从普通文字列改为带左侧色标的条目行，提升扫读效率并减少信息粘连。
- 已完成工具弹窗内部优化：训练工具弹窗、普通工具区、卡牌库筛选区和卡牌库结果列表统一增加标题条、强调色标、surface/outline 层级；卡牌库 active filter 和结果行加入明确反馈，加入按钮与列表行高度放宽，提升发随从/发法术时的扫描和点击效率。
- 已完成战斗回放面板内部优化：回放弹窗、标题栏、控制栏、事件条、双方棋盘和时间线统一增加 surface/outline 层级；己方/对手棋盘使用不同语义色标，播放/速度按钮和事件行具备更清晰反馈，回放时更容易分辨当前帧、事件类型和双方站位。
- 已完成主操作按钮优先级优化：刷新/冻结/升本使用经济色，结束回合使用主操作色，开战使用战斗色，回放/工具使用功能色；按钮统一增加 outline 和左侧色标，无回放、金币不足、无法升本等不可用状态会明显弱化并禁止触发命令。
- 当前本地图片审计：`Resources/CardImages` 下有 310 张图片；当前数据表可直接匹配 244/280 个随从、57/73 个法术，剩余项主要是 Duos、token 或新补充条目，后续可按同一命名规则补齐。
- 当前验证：`UnityTavernTrainerViewTests`、`RealisticTavernTrainerViewTests`、`SpellCatalogTests` 通过，51/51。

## 验收标准

### 主观验收

- 打开项目后，不需要解释也能看出主入口。
- 进入酒馆训练器后，玩家能立即看到商店、己方棋盘、手牌和主操作。
- 右侧信息不再把主区域挤得喘不过气。
- 调试工具不再像主流程的一部分，而是明确的工具层。

### 客观验收

- Console 无项目错误。
- EditMode 测试通过。
- 关键分辨率布局报告通过：
  - `1920 x 1080`
  - `1366 x 768`
  - `1280 x 720`
  - `1000 x 600`
  - `994 x 384`
- 关键按钮实际屏幕高度 >= 32 像素，理想目标 >= 40 像素。
- 关键文本不出现不可理解的截断。
- 满棋盘、满手牌、商店满员、打开右侧面板、打开工具弹窗、打开回放面板时都不越界。

## 风险和注意事项

- 不要一次性重写整个 UI。当前项目已有大量测试和可用逻辑，应该分阶段替换。
- 不要把所有布局逻辑塞进 prefab builder。运行时需要根据窗口尺寸切换布局，必须有 runtime layout context。
- 不要只按 1920x1080 设计。Unity Editor 实际 Game View 经常更短、更窄。
- 不要把调试工具当成主流程常驻信息。训练体验和开发工具要分层。
- 不要在 Play Mode 下保存运行时生成对象到场景，除非明确是 prefab builder 操作。

## 后续建议

下一步建议直接从“第一轮可执行任务清单”开工。最先做 `MainHub + CanvasScaler + LayoutContext`，因为它们范围小、反馈快，也能立刻改善当前最明显的乱和小。

完成第一轮后，再重跑布局报告和截图审计，把文档中的 Phase 2 细化成具体 PR 任务。
