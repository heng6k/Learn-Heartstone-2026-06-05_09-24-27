---
workflow: general-video
flow: automation
storyboard: yes
destination: general-social-video
aspect: 16:9
language: zh-CN
---

# Learn Heartstone 实战模拟宣传片

## Status

- G0：PASS
- G1：PASS（16:9、测试版边界与真实素材范围已确认）
- G4：REVISED（删除铜须场景，新增 Voicebox 中文旁白）
- G10：PASS（用户已明确批准新版有声预览）
- G11：PASS（60 秒 high 成片完整解码，音视频流与规格正确）
- G12：PASS（最终 MP4 与 SHA256 已锁定）
- 项目版本：Alpha / 测试版
- Build SHA：`0bb5b75eea27b44c9fbb139eff9111b63548797f`
- WebGL Build UTC：`2026-07-18T07:24:56.0861572Z`

## Purpose

用真实项目画面展示 Learn Heartstone 如何构造单人酒馆战棋局面、完成招募与上场、配置对手、进入回放并逐步检视事件，强调它是用于验证阵容与机制的非官方训练/模拟工具。

## Audience

- 熟悉酒馆战棋、希望验证阵容和机制的玩家。
- 想复盘攻击顺序、亡语、召唤与关键词变化的玩家。
- 愿意体验 Alpha 并反馈问题的测试用户。

## Output

- 主成片：1920×1080，16:9，30fps，目标 60 秒。
- 编码：H.264 + AAC。
- 使用 Voicebox “曼波”授权克隆 profile 生成中文旁白；屏幕文案仍保留关键结论与边界声明。
- 不交付独立音频文件。
- 全程显示“测试版”；结尾显示“非官方单人酒馆训练 / 模拟工具”。

## Core Message

从配置对局到逐帧复盘，先把阵容放进真实规则链路里打一次。

## Must Show

1. 选择本局种族与英雄/对局环境。
2. 刷新、购买、上场、出售/调整站位等招募阶段操作。
3. 对手阵容配置与对手面板。
4. 战斗回放的播放/暂停/逐步/时间轴和事件记录。
5. 卡牌库按等级、种族/类型和名称筛选并继续加载。
6. Alpha/测试版、非官方、单人训练边界。

## Must Not Claim or Show

- 不声称“完整官方模拟器”“100% 还原”“所有机制完全一致”。
- 不展示双打、真实八人大厅或未实现/代理实现能力为完整功能。
- 不展示 Unity Editor、Console、终端、开发者工具、个人路径、调试错误和个人信息。
- 不使用《炉石传说》官方音乐。

## Assets and Evidence

- 当前 WebGL：`Builds/ReleaseCandidate/<已验收版本>/`
- Windows Alpha：`Builds/BattlegroundsTrainer_v0.1.0-alpha_win/`
- 现有截图与卡图：`PromoVideo/LearnHeartstoneTestVersion/assets/`
- 测试基线：EditMode 1336（1335 通过、1 跳过），全部 PlayMode 19/19 通过。
- 连续操作素材必须按 `RECORDING_PLAN.md` 采集并在 `ASSET_MANIFEST.md` 批准。

## Audio

- 使用本地 Voicebox v0.5.0、“曼波”授权克隆 profile 与 Chatterbox Multilingual（`chatterbox-tts`）生成中文旁白。
- 删除“铜须 / 五本卡铜须”对应旁白；按 `SCRIPT.md` 的 7 句新版文本分段生成并对齐场景。
- 本版不添加 BGM 或官方游戏原声，避免遮蔽旁白；旁白只内嵌成片，不作为独立交付物。
- 生成后必须检查响度、爆音、静音、重复和场景错位，并据实生成 captions/transcript。

## Human Gates

- G1：确认本 BRIEF 与 `ACCEPTANCE.md`。
- G3：确认横屏视觉方向。
- G4：确认屏幕文案。
- G10：Studio 最终预览人工批准后，才能渲染 draft/high。
