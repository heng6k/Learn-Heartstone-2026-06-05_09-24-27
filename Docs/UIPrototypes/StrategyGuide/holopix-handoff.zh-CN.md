# P3 一图流攻略 Holopix 美术交接

## 冻结方向

最终方向为「酒馆战术桌」：深海军蓝工作区、低饱和毡布层、薄黄铜边、少量青色交互高亮。信息区保持扁平、紧凑和高对比；材质只服务分层，不改变 HTML 原型的控件尺寸和信息层级。

- 主参考：[strategy-guide-prototype.html](./strategy-guide-prototype.html)
- 主模型建议：[应用工具｜扁平插画风](https://holopix.cn/model/102)
- 只在制作角花或徽记时参考：[卡通风格 UI 框 ICON](https://holopix.cn/model/5552)
- 不使用整屏一键成稿作为 Unity 最终界面。
- 不生成文字、数字、图标、卡牌图、角色图或二维码。
- 不使用厚木框、大铆钉、强发光、果冻按钮、巨型圆角和高饱和大面积橙色。

## 通用正向提示词

> 深色奇幻卡牌游戏的酒馆战术桌 UI 组件，现代扁平游戏界面，深海军蓝石板表面，低饱和墨绿毡布内层，纤细旧黄铜包边，极轻微磨损和纸张纤维，边缘清楚，中心平整可拉伸，高对比但不过度发光，适合策略编辑器和阵容浏览，透明背景，单个独立组件，正视图，无透视，无文字，无数字，无图标，无卡牌，无角色，无完整界面截图

## 通用反向提示词

> text, letters, numbers, logo, icon, card art, character, screenshot, full interface, perspective, isometric, thick frame, huge button, oversized bevel, glossy plastic, jelly, neon bloom, excessive ornament, complex center texture, baked shadow outside canvas, illegible details

## 必需资源

所有资源输出透明 PNG、2× 尺寸、正视图；同组状态必须保持轮廓和内边距一致。

| 资源组 | 文件建议 | 状态 | 2× 建议画布 | 关键约束 |
|---|---|---|---:|---|
| 主工作面板 | `panel_workspace` | normal | 512×512 | 24 px 薄边；中心至少 320×320 可拉伸 |
| 左侧攻略栏 | `panel_strategy_rail` | normal | 384×512 | 左右边界清楚；不烘焙标题 |
| 内容卡片 | `panel_content_card` | normal / selected / invalid / disabled | 384×256 | selected 只增加青色描边与左侧金色轨道 |
| 主按钮 | `button_primary` | normal / hover / pressed / disabled | 384×112 | 橙金面，轮廓紧凑，不增加高度 |
| 次按钮 | `button_secondary` | normal / hover / pressed / disabled | 384×112 | 深色面＋细边，不与主按钮抢层级 |
| 安静按钮 | `button_quiet` | normal / hover / pressed / disabled | 320×112 | 近透明表面，仍保留可读命中区 |
| 步骤标签 | `tab_step` | normal / current / completed / disabled | 384×128 | current 为底部青色轨道，completed 使用绿色小圆点槽位 |
| 机制徽记底 | `badge_mechanic` | normal / selected | 160×64 | 不含文字；允许圆角药丸形 |
| 阵容卡槽 | `slot_lineup` | normal / selected / golden / invalid | 256×344 | 卡图窗口保持矩形；金色只在细边和右上角标记槽 |
| 弹窗底板 | `panel_modal` | normal | 640×480 | 中央完全平整；外阴影单独输出或由 Unity 实现 |
| 分隔装饰 | `divider_brass` | normal | 512×32 | 可水平平铺；视觉重量低于标题 |
| 阵容徽章 | `crest_strategy` | beast / mech / demon / neutral | 128×128 | 只做抽象轮廓，不含汉字和种族图标版权素材 |

## 分组提示词补充

### 面板与卡片

在通用提示词后追加：

> rectangular modular UI surface, thin brass rim, flat calm center, subtle felt inset, 9-slice safe center, restrained corner detail, no content

### 按钮

在通用提示词后追加：

> compact horizontal action button skin, 56px final height, clear pressed depth under 4px, simple silhouette, wide safe center, no label, no icon

### 卡槽

在通用提示词后追加：

> seven-slot strategy lineup frame, single reusable card slot skin, narrow border, large transparent artwork window, tiny top-right state marker socket, no card artwork

### 角花与徽记

这一组可以单独参考 UI 框 ICON 模型，但仍限制：

> small restrained tavern brass ornament, simple silhouette, low relief, readable at 32px, transparent background, no text, no character

## Holopix 操作顺序

1. 在自由画布导入 HTML 原型截图或组件板截图作为结构参考。
2. 先生成一套 `panel_workspace`，确认材质、黄铜宽度和圆角；不要先生成整屏。
3. 以同一参考风格逐组生成按钮、卡片、卡槽和徽记。
4. 使用「界面元素拆分」与「雪碧图拆分」得到独立透明 PNG。
5. 检查每张图的透明边、拉伸中心和四角是否完整，再按表中名字导出。
6. Unity 中只替换 Sprite；文字、布局、焦点、禁用态逻辑继续由代码控制。

## 验收门

- 在 1280×720 下，正文、卡名、步骤和主按钮仍先于装饰被看到。
- 主按钮最终高度不超过 56 px，普通按钮不超过 48 px。
- selected、invalid、disabled 不只靠颜色区分。
- 9-slice 拉伸到 0.75×、1×、1.5× 时四角不变形，中心无明显纹理断裂。
- 资源关闭后，Unity 的纯色占位 UI 仍完整可用。
- 不允许任何一张整屏生成图进入 Unity Canvas。
