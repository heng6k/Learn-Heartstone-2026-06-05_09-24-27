# 真实录屏计划

固定版本：`0bb5b75eea27b44c9fbb139eff9111b63548797f`
优先入口：`WebDeploy/`；若 WebGL 输入/录制不稳定，使用同 SHA 对应 Windows 构建。
录制规格：1920×1080、60fps、H.264；每段操作前后各保留至少 1 秒。

| ID | 前置状态 / fixture | 操作步骤 | 期望结果 | 输出 | 当前状态 |
|---|---|---|---|---|---|
| LH16-R01 | 干净启动页 | 进入训练器→选择可用种族→确认 | 清晰进入下一阶段 | `assets/recordings/r01-tribes.mp4` | approved |
| LH16-R02 | R01 结束状态 | 选择英雄/对局配置→进入酒馆 | 英雄信息清楚，酒馆加载完成 | `assets/recordings/r02-hero-to-tavern.mp4` | approved |
| LH16-R03 | 固定招募局面 | 购买→拖拽上场 | 两步均有可见状态变化；刷新/站位仍需补录 | `assets/recordings/r03-recruit-loop.mp4` | partial |
| LH16-R04 | 工具重复添加首张可用随从 | 三张同名随从依次打出→触发三连→出现发现奖励 | 金色 4/6 随从与三连奖励可见 | `assets/recordings/r04-triple-golden.mp4` | approved-range |
| LH16-R05 | 已进入酒馆 | 打开对手编辑→加入三张敌方随从→查看对手 | 对手阵容与配置面板清楚 | `assets/recordings/r05-opponent-and-combat.mp4` | approved-range |
| LH16-R06 | 亡语/召唤链 fixture | 运行战斗并保留连续结算 | 攻击、伤害、死亡、亡语/召唤顺序可观察 | `assets/recordings/r06-combat-resolution.mp4` | blocked-build-state |
| LH16-R07 | 已进入战斗回放 | 播放/暂停→前后步进→跳过→速度/日志 | 回放控制与逐步查看事件的能力可见 | `assets/recordings/r07-replay-controls.mp4` | approved-range |
| LH16-R08 | 完整卡池 | 打开卡池→五本→海盗类型→滚动列表 | 动态筛选条件、结果和滚动动作可读；布莱恩搜索由批准截图补充 | `assets/recordings/r08-library-filter.mp4` | approved-partial |

## 重录条件

- 画面出现调试工具、路径、通知、控制台或错误。
- 关键按钮/卡名/描述不可读。
- 操作失败、随机结果偏离 fixture、结算过快无法理解。
- 鼠标遮挡关键信息或无目的晃动。
- 音频爆音、持续静音或录入桌面无关声音。

## 当前录制说明

- Playwright 原始录制为 1920×1080、25fps VP8 WebM；批准素材已转为 1920×1080 H.264 MP4。
- 最终 HyperFrames 输出仍按 30fps；Playwright 当前不提供 60fps 录制控制，因此 G2 接受 25fps 真实操作源，战斗结算镜头仍优先寻找/录制更高帧率来源。
- R04 中三连奖励卡短暂显示内部英文占位样式；成片只允许使用金色随从形成与“发现奖励”弹窗范围，不得使用占位卡帧。
- R06 当前 WebGL 工具路径中，己方随从在酒馆界面已上场，但进入战斗快照后显示为 `我方 0/7`。在找到可复现 fixture 或修复前，不批准“完整双方战斗”声明。
- R05 仅批准卡牌库“加入敌方”操作及最终对手配置面板范围，不得把文件名中的 `combat` 解读为已证明双方战斗。
- R07 仅批准回放播放、步进、跳过、速度和日志控件；该素材不证明完整双方战斗结算。
- R08 动态录屏覆盖五本、海盗类型和滚动列表；Unity Canvas 未接收 Playwright 键盘输入，因此“布莱恩”搜索只使用已批准静态截图。
