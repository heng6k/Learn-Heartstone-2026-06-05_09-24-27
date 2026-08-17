# 2026-08-17 手机短横屏 UI、Windows 与网页同步发布记录

## 结论

本轮完成 Unity 手机端 UI Shell 的第一阶段重构，并把同一份已验证内容发布到 Windows 下载包、Cloudflare Pages 完整网页和手机网页。正式域名为 <https://jsoncool.com>。

重构范围只包含 Unity UI、布局组件、响应式策略、展示资源和相应测试；`MatchService`、`GameCommand`、数据库、战斗规则计算和对外接口均未纳入提交。主工作区中其他未提交内容保持原样。

## Git 与内容身份

- 源分支：`codex/mobile-onepage-rail-hotfix-20260813`
- 手机 UI 功能提交：`409bffa73a4e40cd3f1bb1ee3dd2b447c04cf1c6`
- UI 测试契约修正：`778848836bbd72818eb85822ebefc342eb56d0a2`
- 最终站点与下载清单提交：`f70426b77ca9c763d151d9a4df9eb225b84d715e`
- 上述提交：均已 push，远端分支与本地提交对齐
- Unity：`6000.4.10f1`
- `contentVersion` / `snapshotId`：`36.2-20260817-7788488`
- 最终 `buildId`：`20260817T090607Z-f70426b`
- `packageFingerprint`：`2149ca724573242b2a50406878479bce5497f282f377bda1d199bf960f8c2b99`
- `sourceDirty`：`false`
- 上一 Production 回滚点：`b2c1769c-297f-4ec4-b3d7-a7912a3c69be`

Unity 二进制来自 UI 与测试均已提交的 `7788488`。R2 上传和站点清单提交完成后，从最终 `f70426b` 的干净工作树重新组装候选；重组前后的内容指纹一致，站点候选的 `sourceCommit` 为最终提交且 `sourceDirty=false`。

## 手机端 UI 交付范围

本轮交付 ShortLandscape 响应式壳层和酒馆主界面相关组件，重点覆盖 844×390，同时通过基于安全区、物理像素与 Canvas 单位的响应式计算支持其他短横屏尺寸，而不是把布局写死为单一分辨率。

包含的主要内容：

- 独立短横屏判定与布局参数，不再只依赖通用 `IsCompact`。
- 顶部状态、商店、战场、手牌抽屉、底部操作和弹层的移动端编排。
- 统一 Modal Root 行为，弹层打开时拦截下层战场和手牌交互。
- 手机卡牌可读性与准备阶段牌面清晰度修正。
- 移动端视觉资源、工具/更多入口和相关 UI 回归测试。
- 保持原有按钮命令、回调、业务服务和数据库契约不变。

明确未包含：玩法规则、战斗引擎、赛季机制服务、全库贴图导入策略和大量卡图 `.meta` 变更。

## 测试与构建验证

- Unity UI 回归：共 272 项有效通过。
- 首轮 NullGfx：272 项中 261 项通过；11 项渲染测试因 `RenderTexture.Create failed` 无法在 `-nographics` 下执行。
- D3D11 补跑：上述 11 项中 10 项通过，1 项发现测试仍要求旧的矩形遮罩契约。
- 修正测试契约后单项复验通过；运行时代码没有为测试而回退椭圆遮罩。
- WebApp Node 测试：12/12 通过。
- 完整 WebGL 构建：成功，Unity 退出码 0。
- WebGL 原始 Brotli 数据：`111652253` 字节，拆分为 12 个数据分片。
- 最终 ReleaseCandidate：`Builds/ReleaseCandidate/0.1.0-alpha__20260817-f70426b-final`。
- 本地 `wrangler pages dev`：`/`、`/guides`、`/play`、`/downloads`、两份发布清单均为 HTTP 200；Unity 分片返回 `Content-Encoding: br` 和 immutable 缓存。

## Windows 发布

- 构建目录：`Builds/Windows/LearnHeartstone_20260817-7788488`
- 最终发布作业：`build-7788488-r2`
- ZIP：`LearnHeartstone-Windows-x64-0.1.0-alpha__36.2-20260817-7788488__build-7788488-r2.zip`
- 字节数：`189602185`
- SHA-256：`b6f71086957bb5e4416f3c05392ee74fc22795041a0c3688b05f5badfb09f6d5`
- R2 bucket：`learn-heartstone-releases`
- 对象键：`windows/36.2-preview/0.1.0-alpha__36.2-20260817-7788488__build-7788488-r2/LearnHeartstone-Windows-x64-0.1.0-alpha__36.2-20260817-7788488__build-7788488-r2.zip`
- 公开地址：<https://downloads.jsoncool.com/windows/36.2-preview/0.1.0-alpha__36.2-20260817-7788488__build-7788488-r2/LearnHeartstone-Windows-x64-0.1.0-alpha__36.2-20260817-7788488__build-7788488-r2.zip>

独立门禁结果：

- WebGL 与 Windows 的 `content-manifest.json` SHA-256 相同：`76F7D982E7BB054ACAB72E36CBF1F6A636F9BEFFCE3EF62303326075A6EA7083`。
- 打包前与解压后的 EXE 字节一致。
- D3D11、D3D12 均通过可缩放边框、最大化按钮、实际最大化、标题栏关闭 Exit 0。
- 两种图形 API 都没有 fatal 日志或新增 dump。
- R2 管理端完整回读的字节数和 SHA-256 与本地 ZIP 一致。
- 公开域名 HEAD 返回 HTTP 200、`application/zip`、正确文件名和长度；Range 请求返回 HTTP 206 和正确总长度。

曾生成 r1 候选，但第一次上传时缺少 `Content-Disposition`，覆盖同一不可变键后边缘元数据仍可能保留旧值。因此 r1 不进入站点清单；最终 r2 使用全新的对象键并在首次上传时写入完整响应元数据。

## Cloudflare Pages 发布

- Preview deployment：`71d6bd96-ef61-4931-b178-6c4d89d1dec3`
- Preview：<https://71d6bd96.learn-heartstone.pages.dev>
- Preview alias：<https://codex-mobile-ui-20260817.learn-heartstone.pages.dev>
- Production deployment：`d020582b-83b0-453d-937f-62e89a3f1ba4`
- Production：<https://d020582b.learn-heartstone.pages.dev>
- 正式域名：<https://jsoncool.com>

Preview 与 Production 使用同一个冻结 `WebApp/dist`。Production 上传显示 194 个静态资产全部复用，只重新提交 Pages 响应头和 Functions bundle，没有重新构建或替换静态文件。

正式域名回读：

- `/`、`/guides`、`/play`、`/downloads`：HTTP 200。
- `release-meta.sourceCommit`：`f70426b77ca9c763d151d9a4df9eb225b84d715e`。
- `release-meta.sourceDirty`：`false`。
- `snapshotId`：`36.2-20260817-7788488`。
- `packageFingerprint`：`2149ca724573242b2a50406878479bce5497f282f377bda1d199bf960f8c2b99`。
- Windows manifest SHA-256：`b6f71086957bb5e4416f3c05392ee74fc22795041a0c3688b05f5badfb09f6d5`。
- Unity 数据分片在 `Accept-Encoding: br, gzip` 下返回 `Content-Encoding: br` 和一年 immutable 缓存。
- Windows 文件 HEAD 为 HTTP 200，Range 为 HTTP 206，公开长度为 `189602185`。

## 渠道状态

- 私有 Git 源码：已提交并 push。
- 手机轻量网页：Production。
- 完整 Unity 网页：Production。
- Windows 下载包：Production / R2 ready。
- 原生微信小程序：not-in-scope。
- Unity 微信小游戏：not-in-scope。

## 回滚与保留项

- 网页回滚目标：Cloudflare Pages deployment `b2c1769c-297f-4ec4-b3d7-a7912a3c69be`。
- Windows r2 对象键不可变；若发现问题，应恢复上一版站点清单，不覆盖 r2。
- r1 仅作为未引用候选保留，不应出现在玩家下载链接中。
- 主工作区内不属于本轮 UI 发布的业务逻辑、贴图导入和 `.meta` 修改未被暂存、提交或覆盖。
