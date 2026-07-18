# 选择后闪烁与列表闪回问题排查及修改方案

## 1. 问题范围

用户反馈包含两个连续出现的现象：

1. 点击选择/添加后，界面会明显闪烁一下。
2. 在卡牌库、卡池编辑等长列表中连续添加时，添加完成后列表会回到进入界面时的第一项，导致无法连续处理当前位置附近的卡牌。

本方案先固定根因和修改边界，暂不直接修改生产代码。

## 2. Root Cause Analysis

**Error**: 选择或添加成功后 UI 闪烁，并且长列表滚动位置回到顶部（用户感知为“闪回进入界面的第一个界面/第一项”）。

**Expected**:

- 选择后只更新被选中的行、统计摘要和业务状态。
- 当前弹窗、筛选条件、搜索文本、滚动位置和焦点保持不变。
- 可以在当前位置连续添加多张卡牌，不需要重新滚动。

**Cause**:

1. 选择操作采用“全量重建 UI”，不是局部刷新。
2. 全量重建前没有保存 `ScrollRect.verticalNormalizedPosition`、当前焦点对象和可见项状态。
3. 运行时旧 UI 使用延迟 `Destroy()`，重建期间旧树和新树存在于同一帧生命周期，产生一帧可见闪烁。

**Fix**:

- 卡池编辑和卡牌库添加动作改为“状态先更新、局部 UI 刷新优先”。
- 对仍需要全量重建的场景，统一保存并恢复弹窗状态、滚动位置和焦点；恢复必须发生在布局完成后。
- 不改变 `MatchService` 的添加规则，不通过延迟等待、强制 `DestroyImmediate` 或隐藏一帧来掩盖问题。

**Prevention**:

- 为卡池勾选、玩家卡牌库添加、对手卡牌库添加分别增加滚动位置/焦点回归测试。
- 将“全量重建是否允许丢失交互状态”列入 UI 修改检查项。

## 3. 已确认的代码证据

### 3.1 设置页：卡池编辑是最直接的复现路径

文件：`Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernTribeSelectionView.cs`

- `Build()`（约 177-240 行）在已有 `shell` 时先清空整个页面，再重新创建所有页面、弹窗和列表。
- `BuildCardList()`（约 1998-2085 行）每次都新建 `UiFactory.ScrollView("UnityCardPoolVersionScroll", ...)`。
- 每个卡池 Toggle 的回调在修改集合后直接调用 `Build()`：
  - 随从：约 2025-2031 行
  - 酒馆法术：约 2049-2055 行
  - 时空酒馆：约 2073-2079 行
- `ClearChildren()`（约 3191-3205 行）在播放模式使用 `UnityEngine.Object.Destroy()`，销毁延迟到本帧末。
- `ConfigureCardPoolScrollLoading()` 只在“加载更多”时使用 `keepVersionListAtBottom`，普通勾选没有保存滚动位置。

因此，点击一张卡牌的实际链路是：

```text
Toggle.onValueChanged
  -> SetEnabled(...)
  -> MarkCardPoolDirty()
  -> Build()
  -> ClearChildren(shell)
  -> 新建 ScrollRect（默认位置）
```

这同时解释了闪烁和回到第一项两个现象。

### 3.2 训练器卡牌库：添加后也会触发同型问题

文件：`Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernTrainerController.cs`

- `Apply(GameCommand)`（约 8164-8180 行）在 `service.Apply(command)` 后无条件调用 `Rebuild()`。
- `Rebuild()`（约 226-336 行）先调用 `ClearChildren()`，再重建背景、主界面和当前弹窗。
- `ApplyCardLibraryChoice()`（约 3985-4011 行）处理玩家手牌、对手手牌、对手战场的添加，最终都会进入 `Apply(GameCommand)`。
- `BuildCardLibraryCenterPanel()`（约 3662-3703 行）每次重新创建 `UnityCardLibraryScroll`，没有恢复滚动位置。
- `UiFactory.ScrollView()`（`Assets/LearnHearthstone/Runtime/Presentation/Common/UiFactory.cs` 约 70-129 行）创建新的 `ScrollRect`，未提供位置快照接口。

这里业务状态不会主动关闭 `cardLibraryOpen`，所以更准确的描述是“弹窗仍然打开，但列表视口回到顶部”，而不是添加命令真的把页面切走。

### 3.3 现有测试缺口

文件：`Assets/LearnHearthstone/Tests/EditMode/UI/UnityTavernTrainerViewTests.cs`

现有测试已经覆盖：

- 卡池 Toggle 修改后数据是否保存；
- 卡牌库筛选、搜索和添加是否成功；
- 对手卡牌库和时空酒馆卡牌是否可添加。

但目前没有断言：

- 添加前后的 `ScrollRect.verticalNormalizedPosition` 是否保持；
- 当前选中/聚焦对象是否保持；
- 添加后是否仍停留在同一个弹窗和同一个筛选上下文；
- 选择时是否发生整棵 UI 树重建或可见的一帧空白。

## 4. 针对性修改方案

### 4.1 P0：先修复用户最常见的卡池编辑闪回

修改文件：

- `UnityTavernTribeSelectionView.cs`

建议顺序：

1. 新增卡池编辑 UI 状态快照，至少包含：
   - `activeTab`
   - `searchText`
   - `versionTierFilter`
   - `versionTribeFilter`
   - `visibleCardPoolItemCount`
   - `ScrollRect.verticalNormalizedPosition`
   - 当前焦点卡牌的 `CardId`（如果有）
2. 在卡牌 Toggle 回调进入刷新前保存快照。
3. 优先将“单张卡牌勾选”改为局部更新：
   - 直接保留当前 `Toggle` 的视觉状态；
   - 更新版本摘要、脏状态和筛选统计；
   - 不重建 `shell`、弹窗、`ScrollRect` 和未变更的行。
4. 如果当前架构阶段仍必须调用 `Build()`，则在 Build 后执行恢复流程：
   - 等待布局完成（推荐 `Canvas.ForceUpdateCanvases()` / `LayoutRebuilder.ForceRebuildLayoutImmediate()` 后恢复）；
   - 恢复 `verticalNormalizedPosition`；
   - 按 `CardId` 恢复 `EventSystem.current` 的选中对象；
   - 仅在筛选条件、页签或搜索真正变化时重置到顶部。
5. 保留现有“加载更多后锚定底部”逻辑，但将其与普通勾选的滚动恢复分开，避免互相覆盖。

### 4.2 P1：修复训练器卡牌库连续添加体验

修改文件：

- `UnityTavernTrainerController.cs`
- 必要时增加一个 UI 状态快照小类，避免把状态散落在多个字段中。

建议顺序：

1. 在 `ApplyCardLibraryChoice()` 发出添加命令前保存卡牌库快照：
   - `cardLibraryOpen`
   - `cardLibraryDestination`
   - `toolsAcquisitionKind`
   - 等级/种族/搜索条件
   - `UnityCardLibraryScroll` 的滚动位置
   - 当前焦点卡牌的 `CardId`
2. 添加成功后优先局部刷新手牌/对手战场和按钮可用状态；不要为一次添加重建整个训练器页面。
3. 若暂时保留统一 `Apply()` 全量重建，则：
   - 仅对 `AddCardToHand`、`AddOpponentMinion`、`DebugCastCard`、`SetOpponentStartOfCombatSpell` 启用状态快照恢复；
   - 在 `Rebuild()` 之后、下一次布局完成时恢复 `ScrollRect` 和焦点；
   - 不要把所有 `Rebuild()` 调用改成同一种恢复逻辑，关闭弹窗、切换页签等操作应继续按预期重置位置。
4. 添加后保持 `cardLibraryOpen = true`，不返回训练工具首页；只有用户点击“返回工具”或“关闭”时才切换页面。

### 4.3 P2：降低全量重建带来的闪烁风险

这一步不是用来掩盖状态丢失，而是减少剩余重建的视觉副作用：

- 为需要重建的弹窗提供独立的内容容器，优先替换内容子树，不销毁整个训练器根节点。
- 对动态列表使用“复用行对象 + 更新绑定数据”，避免每次选择都 `new GameObject`。
- 不建议在播放模式使用 `DestroyImmediate()` 作为通用修复；它可能破坏 Unity 当前事件分发和布局生命周期。
- 不建议加入固定 `WaitForSeconds` 或人为延迟来“等闪烁结束”；这会放大输入延迟且不能保存滚动状态。

## 5. 验证矩阵

### 5.1 卡池编辑页

| 场景 | 验收标准 |
|---|---|
| 列表顶部勾选一张卡 | 只更新当前行/摘要，无明显闪烁 |
| 列表滚动到中部后勾选 | 位置保持在原视口，附近卡牌不跳回第一项 |
| 列表滚动到底部并加载更多 | 仍保持底部锚定，且新行只追加一次 |
| 切换随从/法术/时空页签 | 按设计重置到新页签顶部，不继承旧页签位置 |
| 修改搜索、等级、种族筛选 | 筛选变化时允许回到顶部，但不能出现旧树和新树重叠闪烁 |
| 连续勾选 10 张卡 | 10 次操作都留在当前弹窗，可连续完成 |

### 5.2 训练器卡牌库

| 场景 | 验收标准 |
|---|---|
| 玩家手牌添加 | 添加成功，卡牌库仍打开，筛选/搜索/滚动位置保持 |
| 对手战场添加 | 添加成功，卡牌库仍打开，金卡开关状态保持 |
| 对手手牌添加 | 同上，不返回训练工具首页 |
| 添加后手牌/战场达到上限 | 只更新按钮禁用状态，不重置列表位置 |
| 连续添加不同卡牌 | 可连续点击当前位置的卡牌，不需要重新滚动 |
| 点击返回工具/关闭 | 明确返回对应页面，且这是唯一的页面切换来源 |

### 5.3 输入与视觉

- 鼠标连续点击：无整屏闪白/闪黑或内容跳动。
- 键盘/手柄：焦点保持在对应卡牌或可预测的邻近按钮。
- WebGL/Standalone 至少各验证一次；低分辨率和长列表必须验证。
- 用 PlayMode 截图或录屏确认“点击前一帧—点击后一帧”没有旧 UI 与新 UI 同时可见。

## 6. 建议新增的回归测试

### EditMode

1. `TribeSelectionView_CardPoolTogglePreservesScrollPosition`
   - 打开自定义卡池；滚动到中部；切换一个可交互 Toggle；断言重建后 `verticalNormalizedPosition` 在容差内保持。
2. `TribeSelectionView_CardPoolTogglePreservesActiveTabAndFocus`
   - 在法术或时空页签切换卡牌；断言页签、搜索/筛选字段和焦点卡牌不变。
3. `Tools_CardLibraryAddPreservesModalAndScrollPosition`
   - 打开卡牌库并滚动；添加一张卡；断言 `UnityCardLibraryOverlay` 仍存在、目标筛选仍存在、滚动位置保持。
4. `Tools_OpponentCardLibraryAddPreservesModalAndGoldenToggle`
   - 对手战场添加金卡后，断言卡牌库仍打开且金卡开关保持。

### PlayMode

- 在真实 Canvas/布局生命周期下重复执行上述连续添加流程，记录每次点击后的 1 帧和 2 帧截图。
- 断言没有出现临时空白根节点、重复弹窗或焦点丢失。

## 7. 不应修改的范围

- 不修改 `MatchService` 的卡牌添加规则、手牌上限和对手战场上限。
- 不修改卡池数据、卡牌排序或筛选语义来规避 UI 问题。
- 不通过延迟关闭、自动滚回、自动重新打开弹窗来伪造“保持位置”。
- 不把所有页面的 `Rebuild()` 一律替换成局部刷新；先限定在卡池 Toggle 和卡牌库添加两个复现路径。

## 8. 完成判定

本问题可认为修复完成，需要同时满足：

1. 选择/添加后无可见闪烁，或只保留明确、短促且不影响操作的状态反馈。
2. 卡池编辑和卡牌库添加均不会回到列表第一项。
3. 连续添加 10 次不需要重复打开页面或重新滚动。
4. 现有卡池、卡牌库、对手添加和时空酒馆相关测试全部通过。
5. 新增的滚动位置、焦点和弹窗保持测试通过。

## 9. 本地实施结果（2026-07-17）

本轮已按 P2 完成两个复现路径的局部刷新，尚未发布或更新线上版本。

### 9.1 卡池编辑

- 单张随从、酒馆法术、时空卡牌 Toggle 不再调用完整 `Build()`。
- 当前 Toggle 对象、卡池弹窗和 `ScrollRect` 保持不变，因此鼠标、键盘或手柄焦点不会因对象销毁而丢失。
- Toggle 自身只更新勾选和描边状态，同时局部更新：
  - 主页面版本摘要；
  - 弹窗版本摘要；
  - “保存/保存*”文本、颜色和可用状态。
- 页签、搜索、等级/种族筛选、版本切换和关闭弹窗仍按原设计完整重建并回到顶部。
- 批量包含/排除和懒加载仍沿用 P0/P1 的滚动位置保存恢复逻辑。

### 9.2 训练器卡牌库

- 卡牌库添加命令成功后不再调用训练器根级 `Rebuild()`。
- 保留原 `UnityCardLibraryOverlay`、`UnityPlaySurface` 和 `UnityCardLibraryScroll` 对象。
- 玩家手牌区域存在时，使用现有 `UnityTavernZoneComponent.Build()` 只重新绑定该区域内容；对手区域仅在当前已构建时局部绑定。
- 已构建卡牌的添加按钮会统一刷新 `interactable` 和文本，因此手牌/战场满、战斗开始法术已配置等状态立即更新。
- 命令失败仍回退到完整 `Rebuild()`，确保错误提示和既有异常处理不变。
- 筛选、页签、打开详情、返回工具和关闭弹窗仍使用原有完整重建路径。

### 9.3 验证结果

- 核心 EditMode：3/3 通过。
- 相邻卡池/卡牌库 EditMode：8/8 通过。
- 卡牌库 PlayMode：1/1 通过。
- 卡池编辑 PlayMode：1/1 通过。
- `git diff --check` 通过。

新增断言不仅检查滚动值，还检查操作前后的 overlay、根界面、`ScrollRect`、目标 Toggle/区域根对象引用相同，用于防止后续重新引入整棵 UI 重建。
