# Tavern Trainer UI 皮肤与组件路线

## 目的

本文记录 Tavern Trainer 后续 UI 改造的执行路线、外部素材/开源组件候选、项目内组件封装边界，以及中文字体决策。

当前结论是：先用镜像入口把布局和交互定下来，再选择现成素材皮肤，最后只封项目需要的 UGUI 小组件。不要在方向未定时引入完整 UI 框架，也不要复制任何受保护游戏 UI 美术。

## 当前路线

### 1. 先用镜像入口定布局

当前已经不继续单独 V2 壳子路线，改为保留原酒馆训练器入口，再加一个完全相同的镜像入口。

后续所有布局试验优先在镜像入口上做：

- 主入口继续可回退。
- 镜像入口承接后续改造。
- 第一阶段只改布局、信息层级和交互顺序。
- 暂时不急着做完整视觉风格。

### 2. 再选现成素材皮肤

布局确认后，再选一套现成 UI 资产作为皮肤层。优先方向仍是 Concept A 的“羊皮纸战术桌”：

- 深木桌面作为主背景。
- 黄铜/暗金分割线用于层级和重点。
- 羊皮纸信息面板用于右侧抽屉、日志、建议和说明。
- 卡牌图片优先，文字只保留决策必需信息。

素材只作为皮肤层，不接管项目的信息架构和交互逻辑。

### 3. 最后封项目内 UGUI 组件

UI 方向确认后，再封一组项目内小组件：

| 组件 | 用途 | 说明 |
| --- | --- | --- |
| `TavernPanel` | 通用面板 | 承载羊皮纸、木板、暗色工具面板等皮肤。 |
| `TavernButton` | 操作按钮 | 统一主操作、次操作、危险操作、禁用态、选中态。 |
| `TavernStatusChip` | 顶部状态块 | 金币、回合、酒馆等级、生命、冻结、风险等状态。 |
| `TavernCardSlot` | 卡牌槽位 | 商店、手牌、己方棋盘、对手棋盘的槽位和选中/可拖拽反馈。 |
| `TavernDrawer` | 信息抽屉 | 选中卡、操作、建议、日志、工具入口。 |

这些组件应包在现有 `UiFactory`、`UnityTavernUiStyle`、`UnityTavern*Component` 之上，避免另起一套 UI 框架。

## 外部素材候选

### Parchment Game User Interface Kit

链接：[Parchment game user interface kit](https://gamedeveloperstudio.itch.io/parchment-game-user-interface-kit)

用途：

- 最贴近“羊皮纸战术桌”的皮肤方向。
- itch 页面说明包含旧纸羊皮纸 GUI、预制面板、按钮、切换按钮、复选框、meter、图标、PNG/SVG 文件和可编辑 GUI 文件。

注意：

- 这是付费资产，需要购买并确认授权后才能导入项目。
- 只应使用其可授权素材，不应复制展示页排版或其他游戏界面。

### Game Developer Studio Parchment UI

链接：[Parchment game user interface](https://www.gamedeveloperstudio.com/graphics/viewgraphic.php?page-name=Parchment-game-user-interface&item=194q9g3b4r0u3d2y9l)

用途：

- 作为同类羊皮纸 UI 资产备选。
- 页面说明包含 premade panels、buttons、banners，以及可拼接不同尺寸面板的 panel pieces。

注意：

- 需要确认当前价格、下载方式和 license。
- 可作为皮肤参考或授权素材来源，不作为架构参考。

### Kenney UI Pack RPG Expansion

链接：[UI Pack (RPG Expansion)](https://kenney.nl/assets/ui-pack-rpg-expansion)

用途：

- 免费 CC0 RPG UI 资产包。
- 页面标注 85 个 assets，标签包含 button、panel、slider、rpg、interface。
- 适合低风险补充按钮、面板、slider 等基础 UI 皮肤。

注意：

- 视觉可能不如 Parchment kit 贴近目标，需要二次组合和调色。
- CC0 风险低，适合先做原型皮肤。

## 游戏风格参考

以下只作为风格和信息层级参考，不复制素材、图标、卡框或具体布局。

| 来源 | 可参考点 | 不可做 |
| --- | --- | --- |
| [GWENT](https://store.steampowered.com/app/1284410/GWENT_The_Witcher_Card_Game/) | 卡牌图片优先、桌面高级感、卡牌区域清晰。 | 不复制卡框、图标、背景和整体版式。 |
| [Slay the Spire](https://store.steampowered.com/app/646570/Slay_the_Spire/) | 卡牌可读性、主操作明确、战斗/选择节奏清楚。 | 不复制卡牌框体和视觉语言。 |
| [Root](https://store.steampowered.com/app/965580/Root/) | 数字桌游的版图感、清晰棋盘区域、低噪音策略信息。 | 不复制插画风格和品牌元素。 |
| [Armello](https://store.steampowered.com/app/290340/Armello/) | 奇幻桌游氛围、纸面/棋盘/RPG 混合质感。 | 不复制角色、美术、纹样和 UI 框体。 |

## 开源 Unity UI 组件候选

这些包只用于补能力，不接管 Tavern Trainer 的 UI 架构。默认不引入，只有当镜像入口布局稳定且确实出现重复需求时再评估。

### UIEffect

链接：[UIEffect](https://github.com/mob-sakai/UIEffect) / [OpenUPM](https://openupm.com/packages/com.coffee.ui-effect/)

用途：

- OpenUPM 页面说明它可以在 Inspector 或代码里给 UI 添加 grayscale、blur、dissolve 等效果。
- 适合后期做冻结、选中、禁用、焦点、不可用卡牌等视觉反馈。

引入条件：

- 项目内出现多个 UI 状态需要统一 shader/effect 表达。
- 现有 `Image`、`Outline`、`CanvasGroup`、颜色叠加已经不够用。

暂不引入原因：

- 当前还在布局阶段。
- 冻结/禁用/选中可以先用颜色、描边、透明度解决。

### FancyScrollView

链接：[FancyScrollView](https://github.com/setchi/FancyScrollView) / [OpenUPM](https://openupm.com/packages/jp.setchi.fancyscrollview/)

用途：

- OpenUPM 页面说明它是可实现灵活动画的 Unity ScrollView 组件。
- 适合日志、建议、卡牌列表、工具列表需要更顺滑滚动或轮播时使用。

引入条件：

- 普通 `ScrollRect` 已经不能满足交互。
- 列表需要居中吸附、轮播、缩放、复杂动画。

暂不引入原因：

- 训练器当前更需要稳定信息布局，不需要复杂滚动动画。
- 普通 `ScrollRect` 足够覆盖第一阶段。

### SoftMaskForUGUI

链接：[SoftMaskForUGUI](https://github.com/mob-sakai/SoftMaskForUGUI)

用途：

- 给 UGUI 做软遮罩。
- 如果羊皮纸边缘、右侧抽屉、非矩形面板需要柔和裁切，可以再评估。

引入条件：

- 确认皮肤素材需要 soft mask 才能达到可接受视觉。
- 现有矩形 mask 或 sprite 九宫格无法满足。

暂不引入原因：

- 软遮罩属于视觉增强，不是布局必需。
- 会增加渲染和调试复杂度。

### Unity UI Extensions

链接：[Unity UI Extensions](https://github.com/Unity-UI-Extensions/com.unity.uiextensions)

用途：

- UGUI 扩展控件工具箱。
- 可作为后续缺少特定控件时的备选。

引入条件：

- 明确需要其中某个控件，并确认许可证、维护状态和 Unity 版本兼容。

暂不引入原因：

- 整包范围较大。
- 当前项目已有 `UiFactory` 和多套 `UnityTavern*Component`，不应让工具箱反客为主。

## 中文字体决策

项目后续中文字体统一优先使用思源宋体。

链接：[Source Han Serif / 思源宋体](https://github.com/adobe-fonts/source-han-serif)

决策：

- 游戏里的所有中文后续统一按思源宋体方向处理。
- 当前阶段不着急改字体，不把字体迁移塞进布局改造第一轮。
- 等基础布局和皮肤方向确认后，再做字体资源导入、TMP/UGUI 字体配置和回归检查。

理由：

- 思源宋体是开源字体，适合中文长文本和奇幻/书卷气质。
- 羊皮纸 UI 使用宋体类中文更贴近纸面阅读感。
- 先改布局再改字体，可以避免同时引入排版变化和布局变化导致难以判断问题来源。

后续注意：

- 如果使用 TextMeshPro，需要准备对应 Font Asset 和 fallback。
- 如果继续使用 UGUI `Text`，需要统一字体引用和字号 token。
- 字体导入后必须检查长中文卡名、按钮、日志、右侧抽屉和小窗口下的裁切。

## 推荐实施顺序

1. 镜像入口中先改布局和信息层级。
2. 选择皮肤资产：优先确认 Parchment kit 是否购买；如果不买，先用 Kenney CC0 或项目自制简单皮肤。
3. 将稳定样式收敛成 `TavernPanel`、`TavernButton`、`TavernStatusChip`、`TavernCardSlot`、`TavernDrawer`。
4. 只有在出现明确重复需求时才引入开源包：
   - 视觉状态复杂，再评估 UIEffect。
   - 滚动/轮播复杂，再评估 FancyScrollView。
   - 软边裁切必要，再评估 SoftMaskForUGUI。
   - 特定 UGUI 控件缺失，再评估 Unity UI Extensions。
5. 最后统一中文字体到思源宋体。

## 验收标准

- 原入口仍可用。
- 镜像入口可独立承接 UI 改造。
- 素材来源和授权清楚。
- 组件只封项目内反复出现的概念，不提前做大框架。
- 所有新增开源包都有明确用途、许可证确认和回退方案。
- 中文字体决策已记录，但不会阻塞当前布局改造。

## Sources

- [Parchment game user interface kit](https://gamedeveloperstudio.itch.io/parchment-game-user-interface-kit)
- [Parchment game user interface](https://www.gamedeveloperstudio.com/graphics/viewgraphic.php?page-name=Parchment-game-user-interface&item=194q9g3b4r0u3d2y9l)
- [Kenney UI Pack RPG Expansion](https://kenney.nl/assets/ui-pack-rpg-expansion)
- [UIEffect OpenUPM](https://openupm.com/packages/com.coffee.ui-effect/)
- [UIEffect GitHub](https://github.com/mob-sakai/UIEffect)
- [FancyScrollView OpenUPM](https://openupm.com/packages/jp.setchi.fancyscrollview/)
- [FancyScrollView GitHub](https://github.com/setchi/FancyScrollView)
- [SoftMaskForUGUI GitHub](https://github.com/mob-sakai/SoftMaskForUGUI)
- [Unity UI Extensions GitHub](https://github.com/Unity-UI-Extensions/com.unity.uiextensions)
- [Source Han Serif / 思源宋体](https://github.com/adobe-fonts/source-han-serif)
