# Unity Prefab 化酒馆 UI 实现文档

## 1. 文档目标

这份文档记录“舍弃现有临时 UI，改成 Unity + UGUI + Prefab 化 UI”的完整实施步骤。

最终目标不是重写底层规则，而是把现有项目的表现层重做成更符合 Unity 工作方式的界面：

- 底层 C# 规则逻辑继续保留。
- 旧 `Presentation` UI 先作为临时壳保留，等新 UI 达到可用后再逐步移除。
- 新 UI 使用 UGUI、Prefab、可复用组件、卡图、拖拽、动画、回放面板。
- Unity Editor 里可以直接调布局、改 prefab、挂引用，而不是继续大量用代码硬拼 `Panel/Button/Text`。
- UI Toolkit 暂不作为主战场界面方案，只建议后续用于设置页、数据浏览器、筛选器、调试工具等偏工具型页面。

## 2. 当前状态

当前项目底层结构已经比较适合做 UI 重构：

- `Domain`：随从/法术/战斗/酒馆规则/随机种子/三连/卡池等核心规则。
- `Application`：`MatchService` 和 `GameCommand`，是 UI 调用规则逻辑的主入口。
- `Adapters`：数据加载、图片加载、存档、顾问服务等。
- `Presentation`：现有 Unity UI，当前主要还是代码生成式 UGUI。

已经完成的第一阶段：

- 主大厅已新增入口：`Unity 组件酒馆 UI`。
- 新 UI 目录已建立：`Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle`。
- 已新增第一版桥接脚本：
  - `UnityTavernTrainerView.cs`
  - `UnityTavernCardComponent.cs`
  - `UnityTavernZoneComponent.cs`
  - `UnityTavernUiStyle.cs`
- 已新增基础测试：
  - `Assets/LearnHearthstone/Tests/EditMode/UnityTavernTrainerViewTests.cs`

注意：当前 `UnityStyle` 仍然是“组件式过渡版”，还不是最终 prefab 化版本。它的作用是先把入口、职责边界、组件结构搭起来，方便后续逐块改成真正的 prefab。

## 3. 不做什么

这次 UI 重做不要顺手改玩法规则。

不要把项目迁回 Web/React，除非以后明确要做浏览器版。

不要实现双打、队友战场、传牌、团队奖励、`BGDUO` 卡牌行为。项目范围仍然是单人酒馆训练器。

不要立刻删除旧 UI。只有当新 prefab UI 具备核心功能并通过测试后，旧 UI 才能逐步下线。

## 4. 目标架构

新 UI 只负责呈现和输入，规则仍然走 `MatchService`。

```text
Unity 输入 / Prefab UI
  -> UI Controller / Presenter
  -> GameCommand
  -> MatchService
  -> Domain 规则引擎
  -> MatchState / CombatReplay
  -> UI 绑定 / 动画刷新
```

稳定边界：

- UI 读取 `MatchService.State`。
- UI 修改状态只能调用 `MatchService.Apply(GameCommand)`。
- UI 可以保存本地视觉状态，例如选中卡牌、悬停状态、当前弹窗、当前回放帧。
- UI 不复制酒馆规则、战斗规则、卡牌效果、卡池逻辑。

## 5. 推荐目录结构

最终建议把新 UI 整理成下面这种结构：

```text
Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/
  Scripts/
    UnityTavernTrainerView.cs
    UnityTavernTrainerController.cs
    UnityTavernCardComponent.cs
    UnityTavernZoneComponent.cs
    UnityTavernDragController.cs
    UnityTavernReplayController.cs
    UnityTavernUiBinder.cs
    UnityTavernUiStyle.cs
  Prefabs/
    UnityTavernRoot.prefab
    Card/
      TavernCard.prefab
      BoardMinion.prefab
      CardSlot.prefab
    Zones/
      ShopZone.prefab
      HandZone.prefab
      PlayerBoardZone.prefab
      OpponentBoardZone.prefab
    Panels/
      RightInspectorPanel.prefab
      ActionPanel.prefab
      RecruitLogPanel.prefab
      CombatLogPanel.prefab
      AdvisorPanel.prefab
    Modals/
      DiscoverModal.prefab
      CardDetailModal.prefab
      ErrorToast.prefab
    Replay/
      CombatReplayPanel.prefab
      CombatReplayBoard.prefab
      ReplayTimeline.prefab
  Animations/
    CardHover.anim
    CardBuy.anim
    CardPlay.anim
    CardSell.anim
    CombatHit.anim
  Art/
    Frames/
    Badges/
    Board/
```

说明：

- `Scripts` 放行为。
- `Prefabs` 放 Unity 可视化层级和可复用 UI 资产。
- `Animations` 放 UI 动画。
- `Art` 放 UI 专用边框、徽章、桌面、按钮底图等。
- 卡牌原图暂时继续放在 `Resources/CardImages`，除非后续整体改 Addressables 或别的资源策略。

## 6. 分阶段实施步骤

### 阶段 0：保留当前新入口

目标：让当前“Unity 组件酒馆 UI”作为新 UI 的独立入口继续存在。

状态：已基本完成。

步骤：

1. 保留 `MainHubView` 里的 `Unity 组件酒馆 UI` 入口。
2. 保留 `LearnHearthstoneBootstrap.ShowUnityTrainer`。
3. 保留旧的 `TavernTrainerView` 和 `RealisticTavernTrainerView`。
4. 继续把 `UnityStyle` 作为新版 UI 的实验/迁移区。
5. 不要在新 UI 完成前删除旧 UI。

验收标准：

- 主大厅能看到旧版、真实酒馆 UI、新 Unity 组件 UI 三个入口。
- 点击 `Unity 组件酒馆 UI` 能进入新界面。
- 旧两个入口仍然能打开。

### 阶段 1：拆分 Controller 和 View

目标：把“创建界面”和“绑定状态/执行命令”拆开。

步骤：

1. 把 `UnityTavernTrainerView.cs` 拆成：
   - `UnityTavernTrainerView`：负责加载或实例化根 prefab。
   - `UnityTavernTrainerController`：负责持有 `MatchService`、按钮回调、刷新 UI。
   - 可选 `UnityTavernUiBinder`：负责把 `MatchState` 绑定到 prefab 引用。
2. 所有 `GameCommand` 都从 controller 发出。
3. 本地 UI 状态放在 controller：
   - 当前选中卡牌 id
   - 当前右侧面板 tab
   - 当前弹窗
   - 当前回放帧
   - 最近错误信息
4. 重复 UI 区块不要继续靠代码创建子物体，逐步改成 prefab 引用。

验收标准：

- Controller 可以刷新界面。
- Runtime 编译通过。
- 测试可以不依赖完整场景就实例化新 UI。

### 阶段 2：创建根 prefab

目标：把新版酒馆桌面做成真正的 Unity prefab。

Unity Editor 操作步骤：

1. 在 Canvas 下创建 `UnityTavernRoot`。
2. 添加主要区块：
   - 顶部状态栏
   - 对手战场
   - 酒馆商店
   - 玩家战场
   - 手牌区
   - 右侧信息/日志面板
   - 弹窗层
   - Toast 层
3. 给根对象挂 `UnityTavernTrainerController`。
4. 在 controller 上暴露 serialized fields，拖入主要面板引用。
5. 保存为 `UnityTavernRoot.prefab`。
6. 修改 `UnityTavernTrainerView`，让它加载/实例化 `UnityTavernRoot.prefab`。

推荐层级：

```text
UnityTavernRoot
  Background
  TopStatusBar
  PlaySurface
    TableColumn
      OpponentBoardZone
      ShopZone
      PlayerBoardZone
      HandZone
    RightInspectorPanel
  ModalLayer
  ToastLayer
```

验收标准：

- 酒馆主界面存在真实 prefab 文件。
- 可以进入 Prefab Mode 调整布局。
- 调布局时不需要改代码。

### 阶段 3：创建卡牌 prefab

目标：把卡牌显示从代码生成改成可复用 prefab。

需要的 prefab：

- `TavernCard.prefab`：商店、手牌、详情卡。
- `BoardMinion.prefab`：战场上的紧凑卡。
- `CardSlot.prefab`：空槽/卡槽容器。

卡牌 prefab 应包含：

- 卡牌边框 `Image`
- 卡图 `Image`
- 名称 `Text`
- 种族/类型 `Text`
- 关键词摘要 `Text`
- 攻击徽章
- 生命徽章
- 星级/费用徽章
- 主操作按钮
- `UnityTavernCardComponent`
- 悬停/选中状态动画

步骤：

1. 在 Unity 里手动创建 `TavernCard.prefab`。
2. 把 `UnityTavernCardComponent` 从“创建子物体”改成“绑定 serialized references”。
3. 添加字段：
   - `frameImage`
   - `artImage`
   - `nameText`
   - `subtitleText`
   - `attackText`
   - `healthText`
   - `tierText`
   - `actionButton`
4. 实现绑定方法，例如：

```csharp
public void Bind(MinionInstance card, CardViewMode mode, CardActionSet actions)
```

5. 保留缺图 fallback。
6. 添加 hover 和 selected 视觉状态。

验收标准：

- 商店、手牌、战场、详情都使用卡牌 prefab。
- 卡图、名称、属性、种族、关键词都通过 serialized references 显示。
- 缺图不会报错。
- 点击和主按钮操作仍然有效。

### 阶段 4：创建区域 prefab

目标：把商店、手牌、玩家战场、对手战场做成 prefab。

需要的 prefab：

- `ShopZone.prefab`
- `HandZone.prefab`
- `PlayerBoardZone.prefab`
- `OpponentBoardZone.prefab`

每个区域 prefab 应包含：

- 标题文本
- 副标题/数量文本
- 卡槽容器
- 可选 drop target
- 可选状态提示，例如冻结商店、手牌已满

步骤：

1. 在 Unity 里创建每个 zone prefab。
2. 挂 `UnityTavernZoneComponent`。
3. 暴露引用：
   - 标题文本
   - 副标题文本
   - slot parent
   - slot prefab
   - card prefab
4. 实现类似 `BindZone(ZoneViewData zone)` 的绑定方法。
5. 固定核心槽位数量：
   - 玩家战场：7
   - 对手战场：7
   - 手牌：10
   - 商店：根据酒馆等级和法术槽动态

验收标准：

- 区域布局可以在 Unity Editor 里直接调。
- 空槽保持布局稳定。
- 商店、手牌、战场数量显示正确。
- 测试仍能找到稳定对象或组件。

### 阶段 5：实现拖拽

目标：让新 UI 变成真正的卡牌桌面交互。

需要支持：

- 商店卡 -> 手牌：购买
- 手牌随从 -> 玩家战场槽位：上场
- 玩家战场随从 -> 玩家战场槽位：调整站位
- 玩家战场随从 -> 出售区：出售
- 发现选项 -> 手牌：选择发现
- 可选：对手战场编辑拖拽，用于战斗测试

步骤：

1. 新增 `UnityTavernDragController`。
2. 给卡牌 prefab 加 draggable 行为。
3. 给槽位和出售区加 drop target 行为。
4. 拖拽时在顶层 drag layer 创建 ghost card。
5. 高亮合法 drop target。
6. drop 成功后转换成 `GameCommand`。
7. drop 失败时显示 toast。

命令映射：

```text
Shop -> Hand                BuyMinion(index)
Hand -> PlayerBoard         PlayMinion(index, targetIndex)
PlayerBoard -> PlayerBoard  MoveBoardMinion(instanceId, targetIndex)
PlayerBoard -> SellZone     SellMinion(instanceId)
Discover -> Hand            ChooseDiscover(index)
```

验收标准：

- 拖拽不直接修改 `MatchState`。
- 非法拖拽有明确反馈。
- drop 到不同区域会生成正确 `GameCommand`。
- EditMode 测试覆盖命令映射。

### 阶段 6：右侧面板和弹窗 prefab 化

目标：把次级功能放入独立 prefab。

右侧面板：

- 选中卡详情
- 动作按钮
- 招募日志
- 战斗日志
- 顾问建议
- 对手编辑
- 战斗测试工具
- 卡牌获取/调试工具，是否保留可后续决定

弹窗：

- 发现奖励
- 卡牌详情
- 错误 toast
- 后续卡牌获取弹窗

步骤：

1. 创建 `RightInspectorPanel.prefab`。
2. 为日志、顾问、详情创建子 prefab。
3. 创建 `DiscoverModal.prefab`。
4. 弹窗开关状态由 controller 管理。
5. 弹窗层独立于桌面主布局。

验收标准：

- 发现奖励可以在新 UI 中选择。
- 选中卡详情不会挤压主桌面布局。
- 日志可滚动。
- 错误提示出现在桌面上方，不破坏布局。

### 阶段 7：战斗回放 UI

目标：把 `CombatReplay` 做成 Unity 可视化面板。

需要的 prefab：

- `CombatReplayPanel.prefab`
- `CombatReplayBoard.prefab`
- `ReplayTimeline.prefab`
- 可选 `CombatEventMarker.prefab`

步骤：

1. 读取 `service.State.LastReplay`。
2. 显示初始双方棋盘快照。
3. 增加时间轴控制：
   - 上一帧
   - 下一帧
   - 播放/暂停
   - 速度
   - 跳到开头/结尾
4. 把每个 `CombatFrame` 绑定到棋盘显示。
5. 添加事件高亮：
   - 攻击者
   - 目标
   - 受伤随从
   - 死亡随从
   - 召唤随从
   - 触发源
6. 先保证数据绑定正确，再加动画。

验收标准：

- 点击开战后能打开或刷新回放面板。
- 时间轴可以前后切换。
- 棋盘快照和 `CombatFrame` 一致。
- 没有回放数据时显示空状态，不报错。

### 阶段 8：动画和表现打磨

目标：让界面从“工具 UI”变成“卡牌桌面”。

建议动画：

- 卡牌 hover 抬起/缩放
- 购买：卡牌从商店飞到手牌
- 上场：卡牌从手牌飞到战场
- 出售：卡牌飞向出售区并淡出
- 刷新：商店卡牌翻牌/淡入
- 冻结：商店出现明确冻结状态
- 战斗受击：短暂闪光/震动
- 发现弹窗：缩放进入/退出

步骤：

1. 使用 Animator 或轻量脚本 tween。
2. 先做 hover 和 selected。
3. 再增加命令驱动的动画流程：
   - 命令前记录视觉起点
   - 调用 `MatchService.Apply`
   - 根据结果播放动画
   - 动画结束后绑定最终状态
4. 动画必须可跳过、可中断。
5. 不要让动画隐藏关键状态变化。

验收标准：

- 动画中断后 UI 仍可用。
- hover/选中不会导致布局变化。
- 动画不修改玩法状态。

### 阶段 9：下线旧 UI

目标：新 prefab UI 稳定后，逐步移除旧临时 UI。

当前状态：已完成正常入口下线。主大厅的 `酒馆训练器` 现在进入 Unity prefab UI；旧代码生成式 UI 不再作为普通入口出现，仍通过 Unity Editor 菜单 `Learn Heartstone/Play Legacy Tavern Trainer` 保留一段稳定期。

本阶段额外补齐了旧 UI 原本独有的卡牌获取能力：Unity 工具面板现在包含随从/酒馆法术切换、等级筛选、种族筛选和加入手牌操作，命令仍统一走 `MatchService.Apply(GameCommand)`。

前置条件：

- 新 UI 覆盖核心流程。
- 新 UI 有测试。
- 用户已在 Unity 中确认视觉和交互方向。
- 没有只能在旧 UI 中完成的必要操作。

步骤：

1. 先把旧入口标为开发入口或隐藏。
2. 保留旧脚本一小段稳定期。
3. 移除主大厅旧入口。
4. 确认无引用后删除旧 Presentation 代码。
5. 只有在新测试覆盖等价功能后，才删除旧测试。

验收标准：

- 普通使用路径只进入新 prefab UI。
- 删除旧 UI 后无编译错误。
- 测试仍然通过。

## 7. Prefab 清单

### TavernCard.prefab

- [ ] 边框 image
- [ ] 卡图 image
- [ ] 名称 text
- [ ] 类型/种族 text
- [ ] 关键词 text
- [ ] 星级/费用徽章
- [ ] 攻击徽章
- [ ] 生命徽章
- [ ] 主操作按钮
- [ ] hover/selected 状态
- [ ] 缺图 fallback
- [ ] serialized references 已拖好
- [ ] 绑定测试

### ShopZone.prefab

- [ ] 标题
- [ ] 冻结状态
- [ ] 动态槽位容器
- [ ] 刷新视觉状态
- [ ] 购买操作

### HandZone.prefab

- [ ] 10 个稳定槽位
- [ ] 手牌数量
- [ ] 上场/施放操作
- [ ] 接收购买/发现卡牌的 drop target

### PlayerBoardZone.prefab

- [ ] 7 个稳定槽位
- [ ] 战场数量
- [ ] 上场 drop target
- [ ] 调整站位
- [ ] 出售拖拽

### OpponentBoardZone.prefab

- [ ] 7 个稳定槽位
- [ ] 对手战场数量
- [ ] 战斗测试编辑
- [ ] 镜像/复制玩家战场，若继续保留

### RightInspectorPanel.prefab

- [ ] 选中卡详情
- [ ] 动作按钮
- [ ] 顾问建议
- [ ] 招募日志
- [ ] 战斗日志
- [ ] 调试/工具 tab，按需要保留

### DiscoverModal.prefab

- [ ] 遮罩层
- [ ] 三个选项
- [ ] 奖励等级/来源文本
- [ ] 选择按钮
- [ ] 关闭规则，若允许关闭

### CombatReplayPanel.prefab

- [ ] 棋盘快照显示
- [ ] 时间轴
- [ ] 上一帧/下一帧/播放/暂停
- [ ] 事件高亮
- [ ] 空回放状态

## 8. ViewData 策略

不要一开始就引入很重的 ViewModel 层。只有当 prefab 绑定开始复杂时，再加简单 DTO。

建议从小对象开始：

```csharp
public sealed class CardViewData
{
    public MinionInstance Card;
    public bool Selected;
    public bool CanPrimaryAction;
    public string PrimaryActionLabel;
}

public sealed class ZoneViewData
{
    public string Title;
    public string Subtitle;
    public int StableSlotCount;
    public List<CardViewData> Cards;
}
```

ViewData 只用来简化显示绑定，不复制领域逻辑。

## 9. 测试策略

EditMode 测试最少覆盖：

- 主大厅可以打开新 UI。
- 根 prefab/controller 可以构建。
- 卡牌 prefab 能绑定名称、卡图、属性、种族、关键词。
- 商店购买会调用 `BuyMinion`。
- 手牌上场会调用 `PlayMinion`。
- 战场出售会调用 `SellMinion`。
- 拖拽 drop 会生成正确命令。
- 发现弹窗会调用 `ChooseDiscover`。
- 开战按钮会调用 `SimulateCombat`。
- 回放面板能处理有回放和无回放两种状态。
- 缺图不会报错。

手动检查：

1. 打开 `SampleScene`。
2. 进入 Play Mode。
3. 从大厅进入 `Unity 组件酒馆 UI`。
4. 分别测试 16:9、16:10、窄屏比例。
5. 执行买牌、上场、出售、刷新、冻结、升本、下回合。
6. 双方战场有随从时执行开战。
7. 触发发现并选择一个选项。
8. 检查是否有文字重叠、布局挤压、按钮不可点、卡图丢失。

## 10. 视觉方向

新 UI 应该像酒馆卡牌桌面，而不是后台仪表盘。

建议规则：

- 中央是桌面：商店、双方战场、手牌。
- 右侧是操作信息：选中卡、命令、日志、回放。
- 卡图要足够突出。
- 按钮要紧凑，像明确命令，而不是大块说明文字。
- 重复面板要有稳定尺寸。
- hover/选中状态不能改变布局尺寸。
- 不做营销式大 hero。
- 不要单色调 UI。建议用木质暖色、暗绿/暗蓝面板、金色强调、红色出售/危险状态。

## 11. 迁移规则

实施过程中遵守这些规则：

- 新 UI 工作都放在 `UnityStyle` 下，直到它取代旧 UI。
- 不因 UI 需求随意修改 Domain 逻辑。
- 删除旧行为前先补新测试。
- 新 UI 未达到核心功能前，旧 UI 入口继续保留。
- 运行时代码优先使用 serialized prefab references，不依赖 `transform.Find`。
- 测试可以依赖稳定对象名，但生产代码尽量依赖组件引用。
- 所有命令执行放在 controller/presenter 层。
- 布局调整优先在 Unity Editor 里改 prefab，而不是继续写大段代码生成布局。

## 12. 每阶段验证门槛

每个阶段完成后都要过这些检查：

1. Runtime 脚本在 Unity 中编译通过。
2. 相关 EditMode 测试通过。
3. 新 UI 能从主大厅进入。
4. 核心操作仍然走 `MatchService.Apply`。
5. 旧 UI 入口没有被意外破坏。
6. 没有回退或覆盖用户已有的无关改动。

## 13. 完成定义

整个 UI 迁移完成的标准：

- 主大厅正常路径进入 prefab 化的新 Unity 酒馆 UI。
- 旧代码生成式酒馆 UI 不再承担日常功能。
- 核心流程全部可用：
  - 买牌
  - 上场/施放
  - 出售
  - 调整站位
  - 刷新
  - 冻结
  - 升本
  - 下回合
  - 发现
  - 战斗模拟
  - 战斗回放
  - 日志/顾问
- 卡牌、区域、面板、弹窗、回放都是真 prefab。
- 拖拽完成。
- 关键动画和视觉状态完成。
- EditMode 测试覆盖 UI 构建和命令映射。
- 常见 Game view 比例下手动检查通过。

## 14. 下一步建议

下一步最应该做的是：

做一次 Unity Play Mode 视觉和交互验收，然后再决定是否物理删除旧 UI 文件。

原因：

- 第 0-9 阶段的代码路径和测试已经覆盖核心流程。
- 旧 UI 已经不在普通使用入口里，但文件还保留用于短期回滚。
- 物理删除旧 UI 前，最好先在常用窗口比例下确认右侧抽屉、工具卡牌库、战斗回放、发现弹窗没有文字挤压或遮挡。
