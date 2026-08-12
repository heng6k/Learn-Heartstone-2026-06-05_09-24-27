# 2026-08-12 Windows 下载与网页文案热修发布记录

本记录是 [2026-08-12 网页发布记录](2026-08-12-web-release.md) 的后续热修。目标是发布已完成的玩家文案精简，并替换网站仍指向 2026-08-07 旧包的 Windows 下载；同时修复 Windows Player 最大化按钮不可用、窗口边框不可缩放的问题。

正式入口：[https://jsoncool.com](https://jsoncool.com)

## 发布结果

| 交付面 | 状态 | 结果 |
| --- | --- | --- |
| Vue3 玩家文案 | Production | 删除玩家无用的内容快照、轻量加载和页脚技术信息；保留 Windows ZIP 三步使用说明 |
| Windows x64 下载包 | Published | 当前 36.2 内容；可拖动边框、可最大化；D3D11/D3D12 正常退出 |
| 完整 Unity WebGL | Production（复用） | 继续复用已验 `b96d544` ReleaseCandidate，未重新构建 Unity WebGL |
| 微信渠道 | not-in-scope | 未构建、未上传 |

## Git 提交链

| 顺序 | 提交 | SHA |
| ---: | --- | --- |
| 1 | `fix(web): simplify player-facing release copy` | `4658d61` |
| 2 | `fix(windows): preserve clean player shutdown` | `9e5dd42` |
| 3 | `chore(unity): normalize plugin metadata` | `1950c69` |
| 4 | `fix(windows): enable resizable player window` | `e6b7bcf` |
| 5 | `fix(release): record verified Windows candidate state` | `24be618` |
| 6 | `fix(web): publish current Windows download` | `57d423e` |

上述提交均已推送到 `origin/codex/wip-current-state`。Pages Preview 与 Production 的源提交均为 `57d423e28f11b089e2016ee865151d37e533419c`；本记录的证据提交位于发布之后，不改变已经上线的产物身份。

## Windows 构建与验收

- Unity：`6000.4.10f1`
- 构建源：`e6b7bcf49c97f6ce464d4d3df1ba8168845c8de5`
- `sourceDirty`：`false`
- 内容快照：`36.2-20260812-b96d544`
- 规则集：`ruleset-legacy-composite-v1`
- 内容指纹：`8f98bc12a2d8580adebc07c5f07ed490f206889a6dbc62a35053ef1a3934a3af`
- 构建任务：`build-e6b7bcf-r1`
- ZIP 字节数：`185030349`（176.46 MiB）
- ZIP SHA-256：`851f34f7ee80d67e35f31467ef7608b77b5e0fec576aa51024fa02da05cd26d1`
- 下载地址：[LearnHeartstone Windows x64](https://downloads.jsoncool.com/windows/36.2-preview/0.1.0-alpha__36.2-20260812-b96d544__build-e6b7bcf-r1/LearnHeartstone-Windows-x64-0.1.0-alpha__36.2-20260812-b96d544__build-e6b7bcf-r1.zip)

实际系统窗口验收结果：

- D3D11 与 D3D12 均检测到 `WS_THICKFRAME` 和 `WS_MAXIMIZEBOX`。
- 两种图形 API 下实际最大化后窗口尺寸均扩大，边框可调整。
- 两种图形 API 下从标题栏关闭均为 Exit 0、无强制终止、无新增崩溃转储。
- ZIP 解压后重新启动 D3D11/D3D12，结果仍为 Exit 0、无新增转储。
- ZIP 包含程序、当前内容清单、`windows-release-meta.json` 和退出保护 DLL。

## R2 与网页发布

- R2 bucket：`learn-heartstone-releases`
- 上传方式：新建不可变对象键，不覆盖 2026-08-07 旧包。
- R2 管理端完整下载回读：`185030349` 字节，SHA-256 与本地完全一致。
- 公开域名：HEAD 200，`application/zip`，下载文件名、长度和 immutable 缓存正确；前 32 MiB 分段回读通过。其后的 CDN 连续回读受本机网络 EOF 影响，因此未虚写成 12 段通过。
- Preview：`c0954fb4-545a-4de6-bf49-d5d855631c4c`
- Preview URL：[https://c0954fb4.learn-heartstone.pages.dev](https://c0954fb4.learn-heartstone.pages.dev)
- Production：`1bd34e47-db91-46af-b884-b39464e7c45f`
- Production URL：[https://1bd34e47.learn-heartstone.pages.dev](https://1bd34e47.learn-heartstone.pages.dev)
- 上一良好 Production / 回滚目标：`4cb42d49-f32b-4069-8510-56e3bc315af1`

## 网页验收

- Web 测试：12/12 通过。
- Vite + 冻结 Unity 候选构建通过。
- 本地 1440×900、390×844 下载页通过，无横向溢出、无 Console 或 page error。
- Preview 与 `jsoncool.com` 的手机版真实浏览器 smoke 通过。
- 下载按钮、公开 manifest、R2 HEAD 和 WebGL `release-meta.json` 身份一致。
- 构建脚本从 `WebApp` 调用时必须传 ReleaseCandidate 相对路径，例如 `../Builds/ReleaseCandidate/<candidate>`；本机 Node 对带空格的 Windows 绝对参数曾误判候选缺少 `index.html`。

## 回滚

- Pages：回滚到 `4cb42d49-f32b-4069-8510-56e3bc315af1`。
- Windows 下载：恢复上一份 manifest 与 2026-08-07 对象链接；不要覆盖或删除新旧不可变对象。
- R2 对象完整回读、Windows 退出或窗口标志任一回归时，不得只修改网页文案掩盖问题。
