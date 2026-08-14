# 2026-08-14 手机一图流选择器、Windows 与网页同步发布记录

## 结论

本轮完成手机端一图流创建入口的双重缩放修复，并把同一份 Unity 内容同步到 Windows 下载包、Cloudflare Pages 网页版和手机网页。正式域名为 <https://jsoncool.com>。

本记录保存本轮不可变发布身份和可复用经验。历史 SHA、部署 ID、候选目录与 ZIP 地址只用于审计和回滚，下一次发布必须生成新身份。

## 源码与内容身份

- 源分支：`codex/mobile-onepage-rail-hotfix-20260813`
- Unity 功能提交：`f61e7bb448da32ed8539fd091b3c76f744a441ea`
- 最终站点提交：`2e24703b55eaa402d39b2ddf081aba113c10c47e`
- 最终提交状态：已 push，远端分支与本地 SHA 对齐
- Unity：`6000.4.10f1`
- `contentVersion` / `snapshotId`：`36.2-20260813-f61e7bb`
- 最终 `buildId`：`20260813T235949Z-2e24703`
- `packageFingerprint`：`45b9e990b78fe72f1773a09aa01b490dc1c9e78201639c6cd6e74e6781336ab1`
- `sourceDirty`：`false`
- 上一 Production 回滚点：`9691901d-20de-475c-b2cf-8b27630edc6d`

最终站点提交只修改站点内容快照、下载元数据和对应测试；Unity 二进制复用边界是功能提交 `f61e7bb`。从最终提交重新组装 ReleaseCandidate 后，内容指纹与功能提交候选一致，证明复用没有改变 Unity 内容。

## 手机端 UI 修复

### 根因

`StrategyGuideSelectionView` 在 `CanvasScaler.ScaleWithScreenSize` 下直接把手机物理视口宽高写入 `RectTransform.sizeDelta`。物理像素随后又被 CanvasScaler 缩放一次，导致 390×844 和 844×390 下的创建/草稿选择卡片只剩很窄的一块，标题、日期与按钮相互挤压。

### 修复原则

- 先以物理视口计算卡片安全宽高，再用 `CanvasUnitsForPhysicalPixels` 转成 Canvas 单位，只转换一次。
- 手机紧凑布局保留日期、继续编辑、删除和模板选择按钮的物理最小宽度。
- 桌面布局继续使用原有尺寸，不把手机修复扩散成桌面重排。
- 不能用继续缩小字体掩盖容器宽度错误。

### 验证

- `StrategyGuideUiTests`：29/29 通过。
- 新增 390×844、844×390 双视口回归。
- WebApp Node 测试：12/12 通过。
- Playwright：1440×900、390×844、844×390 全部通过。
- `/`、`/guides`、`/play`、`/download` 均无横向溢出。
- 本地截图证据位于 `.planning/mobile-onepage-release-20260813/browser-evidence/`，不是线上运行依赖。

## Windows 发布

- 构建目录：`Builds/Windows/LearnHeartstone_20260813-f61e7bb`
- 发布作业：`build-f61e7bb-r1`
- ZIP：`LearnHeartstone-Windows-x64-0.1.0-alpha__36.2-20260813-f61e7bb__build-f61e7bb-r1.zip`
- 字节数：`185071735`
- SHA-256：`4909f564dd5bb9637d17805596e92aaf30f90ae31a9fdf43dc049e2b0870c5c5`
- R2 bucket：`learn-heartstone-releases`
- 对象键：`windows/36.2-preview/0.1.0-alpha__36.2-20260813-f61e7bb__build-f61e7bb-r1/LearnHeartstone-Windows-x64-0.1.0-alpha__36.2-20260813-f61e7bb__build-f61e7bb-r1.zip`
- 公开地址：<https://downloads.jsoncool.com/windows/36.2-preview/0.1.0-alpha__36.2-20260813-f61e7bb__build-f61e7bb-r1/LearnHeartstone-Windows-x64-0.1.0-alpha__36.2-20260813-f61e7bb__build-f61e7bb-r1.zip>

独立门禁结果：

- D3D11、D3D12 均通过 ThickFrame、MaximizeBox、实际最大化、标题栏关闭 Exit 0。
- 两种图形 API 均没有新增 dump。
- ZIP 使用新不可变 R2 对象键和 `public, max-age=31536000, immutable`。
- 上传后通过 Wrangler 从 R2 完整回读，字节数与 SHA-256 和本地 ZIP 完全一致。
- 公开域名 Range smoke 返回 HTTP 206、`application/zip`、正确文件名和总长度 `185071735`。

## WebGL 与 Cloudflare Pages

- 完整 WebGL 构建：成功，Unity batchmode return code 0。
- 最终 ReleaseCandidate：`0.1.0-alpha__20260813T235949Z-2e24703`
- ReleaseCandidate：50 个文件，123202389 字节。
- WebGL 数据压缩字节：107357298；拆分为 12 个分片。
- Preview deployment：`ac575b7e-603b-47db-9d43-0f9fbec2655c`
- Preview：<https://ac575b7e.learn-heartstone.pages.dev>
- Production deployment：`97d222ee-31e8-45c6-9c19-43779ee09c62`
- Production：<https://97d222ee.learn-heartstone.pages.dev>
- 正式域名：<https://jsoncool.com>

Preview 与 Production 使用同一份冻结 `WebApp/dist`。Production 显示 194/194 个资产全部复用，没有重新上传不同静态文件。

正式域名回读：

- `release-meta.sourceCommit`：`2e24703b55eaa402d39b2ddf081aba113c10c47e`
- `release-meta.sourceDirty`：`false`
- `snapshotId`：`36.2-20260813-f61e7bb`
- `packageFingerprint`：`45b9e990b78fe72f1773a09aa01b490dc1c9e78201639c6cd6e74e6781336ab1`
- Windows manifest SHA-256：`4909f564dd5bb9637d17805596e92aaf30f90ae31a9fdf43dc049e2b0870c5c5`
- 带 `Accept-Encoding: br, gzip` 请求 Unity 分片时返回 `Content-Encoding: br`、immutable 缓存和正确长度。

## 可复用的关键经验

### 下载包与网页清单必须闭环

顺序必须是：生成 ZIP → 本地 SHA → R2 新对象键上传 → R2 完整回读 SHA → 更新 `windows-release-manifest.json` 和 `site-content.js` → 测试 → commit/push → 最终 dist。不能先改页面再假定上传成功。

### 站点当前内容快照也必须同步

仅更新下载按钮和 Windows manifest 不够。`currentVersion.contentSnapshotId`、更新时间和 `unityRelease.sourceDataBytes` 也要对应新构建，否则站点内部仍混用旧快照。

数据大小测试不应锁死某次 Brotli 构建的精确字节；应验证资源规模契约（正整数、合理下限、分片数），由发布清单保存精确值。

### Windows 上必须排除残留 Vite 进程

测试辅助脚本停止 `npm` 父进程后，Windows 可能残留 `node ... vite preview` 子进程继续占用 4173。新测试会误访问旧 `dist`，表现为源码与新 dist 都正确，但页面仍显示旧下载地址。

恢复步骤：

1. 用 `netstat -ano -p tcp` 找到监听端口 PID。
2. 读取 PID 命令行，确认属于旧 Vite preview。
3. 只关闭确认过的进程，或改用未占用端口。
4. 从 dist 搜索旧版本标识，再重新跑浏览器 smoke。

### Brotli 必须按 HTTP 协商验证

未声明 `Accept-Encoding: br` 的 curl 请求不一定返回 `Content-Encoding: br`。验收要模拟浏览器发送 `Accept-Encoding: br, gzip`，再检查编码、压缩长度、缓存头和状态码。

### ReleaseCandidate 必须来自最终已 push SHA

若 R2 上传后还需提交下载 manifest，最终站点提交会晚于 Unity 功能提交。可以复用已验 Unity 二进制，但必须记录复用边界，从最终已 push SHA 的干净工作树重新组装候选，确认 `sourceDirty=false`，并确认前后 `packageFingerprint` 一致。

### Cloudflare Pages 不是 Git 自动部署

本项目 Pages 没有 Git Provider 集成。Git push 不会更新 `jsoncool.com`。必须手动执行 Wrangler Preview 与 `--branch main` Production，使用同一冻结 dist，并在正式域名复验。

### 长命令的非零退出要定位具体阶段

本轮线上主体检查已通过，但最后截断 Wrangler 长表格使组合命令返回非零。必须分开判断页面 smoke、身份回读、响应头检查和辅助列表命令，不能把最后一段管道错误误判为 Production 失败。

## 渠道状态

- 私有 Git 源码：已 push。
- 手机轻量网页：Production。
- 完整 Unity 网页：Production。
- Windows 下载包：Production/R2 ready。
- 原生微信小程序：not-in-scope。
- Unity 微信小游戏：not-in-scope。

## 权限边界

GitHub 仓库当前为 Private，只有获授权账号可以访问源码。公开网站、WebGL 和 Windows ZIP 是编译后的玩家交付物，不等同于开放源码；但编译产物仍可能被逆向分析，不能把客户端包当作绝对保密边界。
