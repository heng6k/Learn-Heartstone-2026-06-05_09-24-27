# 素材清单

状态说明：`approved` 可进入成片；`approved-unused` 为保留审计但本版不使用；`pending` 阻塞音频接入。

| ID | 路径 | 类型 | 场景 | 来源 | 真实性/版权 | 状态 |
|---|---|---|---|---|---|---|
| A001 | `../LearnHeartstoneTestVersion/assets/combat-replay.png` | 截图 | 审计辅助 | 当前项目截图 | 自有项目画面 | approved-unused |
| A002 | `../LearnHeartstoneTestVersion/assets/tavern-main.png` | 截图 | S01/S03 辅助 | 当前项目截图 | 自有项目画面 | approved |
| A003 | `../LearnHeartstoneTestVersion/assets/tribes.png` | 截图 | S02 辅助 | 当前项目截图 | 自有项目画面 | approved-unused |
| A004 | `../LearnHeartstoneTestVersion/assets/minion-normal.png` | 卡图 | S07 | 当前项目资源 | 自有项目真实卡图；仅作名称筛选后的目标卡牌示例 | approved |
| A005 | `../LearnHeartstoneTestVersion/assets/minion-golden.png` | 卡图 | 归档 | 当前项目资源 | 发布前复核分发边界 | approved-unused |
| A006 | `../LearnHeartstoneTestVersion/assets/advanced-filter.png` | 截图 | S07 | 当前项目截图 | 自有项目画面 | approved |
| A007 | `../LearnHeartstoneTestVersion/assets/brann.png` | 截图 | 归档 | 当前项目截图 | 用户要求删除铜须内容；本版不进入 composition | approved-unused |
| V001 | `assets/recordings/r01-tribes.mp4` | 录屏 | S02 | 当前 WebGL Build / SHA `0bb5b75e` | 自有录制 | approved |
| V002 | `assets/recordings/r02-hero-to-tavern.mp4` | 录屏 | S02 | 当前 WebGL Build / SHA `0bb5b75e` | 自有录制 | approved |
| V003 | `assets/recordings/r03-recruit-loop.mp4` | 录屏 | S03 | 当前 WebGL Build / SHA `0bb5b75e` | 自有录制；覆盖购买与拖拽 | approved-partial |
| V004 | `assets/recordings/r04-triple-golden.mp4` | 录屏 | 归档 | 当前 WebGL Build / SHA `0bb5b75e` | 仅保留审计；本版不进入 composition | approved-unused |
| V005 | `assets/recordings/r05-opponent-and-combat.mp4` | 录屏 | S04 | 当前 WebGL Build / SHA `0bb5b75e` | 自有录制；仅批准对手配置操作与最终面板 | approved-range |
| V006 | `assets/recordings/r06-combat-resolution.mp4` | 录屏 | 不进入成片 | 当前 Build | 当前 Build 无法证明完整双方战斗 | blocked-build-state |
| V007 | `assets/recordings/r07-replay-controls.mp4` | 录屏 | S05 | 当前 WebGL Build / SHA `0bb5b75e` | 自有录制；仅证明回放控制与逐步查看事件 | approved-range |
| V008 | `assets/recordings/r08-library-filter.mp4` | 录屏 | S06 | 当前 WebGL Build / SHA `0bb5b75e` | 自有录制；覆盖五本、海盗类型与滚动，名称筛选由 A006/A004 补充 | approved-partial |
| AU001 | `assets/audio/voiceover-final.wav` | 旁白 | 全片 | Voicebox v0.5.0 / “曼波”授权克隆 profile / Chatterbox Multilingual（`chatterbox-tts`） | 参考声音授权已由用户确认；60 秒母带已生成并通过规格、响度与峰值审计 | approved |
| AU002 | 无 | BGM/SFX | 全片 | 本版不使用 | 不使用官方原声或来源不明音乐 | approved-unused |
