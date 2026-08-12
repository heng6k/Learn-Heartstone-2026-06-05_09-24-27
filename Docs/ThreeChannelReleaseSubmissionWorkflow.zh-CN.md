# 如何提交手机版网页、完整网页版和下载包

本文件是 Learn Heartstone 后续网页版本提交、验收与发布记录的统一入口。默认路线先交付不加载 Unity 的手机版轻量网页，再验收完整 Unity 网页版，最后从同一候选生成下载包；微信小程序和微信小游戏仅在版本范围明确包含时执行。

> **现行基准（2026-08-12 起）**：网页统一发布到 Cloudflare Pages 项目 `learn-heartstone`，Production 分支为 `main`，正式域名为 [https://jsoncool.com](https://jsoncool.com)。以后每次上传以 [Windows 下载与网页文案热修发布记录](Releases/2026-08-12-windows-download-hotfix.md) 为最新操作基准，并沿用 [首轮完整网页发布记录](Releases/2026-08-12-web-release.md) 的证据结构；每次必须生成新的 Git SHA、构建身份、Preview/Production deployment ID、ZIP 和 SHA-256，不得复制历史身份冒充新版本。

## 0. 后续上传必须遵守的顺序

正式上传固定按以下顺序执行。任一步失败就停止，不跳步、不在冻结候选上热改：

1. 列出本轮 `in-scope` / `not-in-scope` 交付面和上一已知良好 Production。
2. 按第 3 节分块暂存、检查并提交；禁止 `git add .`。
3. 将最终源提交 push 到远端，确认本地 `HEAD`、远端分支和准备构建的 SHA 一致。
4. 从该 SHA 创建独立、干净的工作树；正式候选的 `sourceDirty` 必须为 `false`。
5. Unity 或玩法内容有变化时做完整 WebGL 构建，`buildScriptsOnly` 必须为 `false`；只改网页壳时可以复用已验 Unity 二进制，但仍要从最终源 SHA 重新组装候选并记录复用边界。
6. 组装新的 ReleaseCandidate，执行 WebApp 测试并生成附带 Unity 的唯一最终 `WebApp/dist`。
7. 用 `wrangler pages dev` 验收同一份 `dist`；先验手机版轻量路径，再验完整 Unity、全屏、响应头和下载。
8. 从同一份 `dist` 部署 Cloudflare Preview，记录 deployment ID 并完成线上 smoke。
9. 上传前记录当前 Production deployment 作为回滚点；Preview 放行后，从同一份、未经修改的 `dist` 创建 `main` Production deployment。
10. 在 `jsoncool.com` 复验版本身份、手机/电脑入口、路由、Brotli/MIME、缓存和全屏。
11. 从同一 ReleaseCandidate 生成 ZIP，解压后重新 HTTP 验收并记录字节数与 SHA-256。
12. 更新本轮发布记录和本文档索引，单独提交并 push 文档证据。

如果本轮同时更换 Windows 桌面下载包，必须在第 6 步之后、Pages Preview 之前额外执行：从已 push 的干净 SHA 构建 Windows x64；验证可缩放边框、最大化按钮、实际最大化、D3D11/D3D12 正常退出与无新增转储；将与 WebGL 相同的内容清单合入候选；生成新 ZIP 并解压复验；上传到新的 R2 不可变对象键；从 R2 完整下载回读并核对字节数和 SHA-256；最后才修改 WebApp 下载清单。不得先切网页链接再补传文件。

Cloudflare Pages 直传会为 Preview 和 Production 各创建一个 deployment；“同源发布”指两者使用同一个最终 Git SHA 和同一份冻结 `dist`，不是要求两个 deployment ID 相同。

## 1. 先确定本次交付面

每次开始前，逐项填写 `in-scope`、`not-in-scope` 或当前状态。不要用“网页版已完成”同时代替手机版网页和完整网页版的结论。

| 交付面 | 面向用户 | 必须包含 | 不包含 |
| --- | --- | --- | --- |
| 共享攻略与卡牌数据 | 所有前端 | 权威攻略、真实牌图、三档训练数据、版本修订号 | 渠道专用布局 |
| 手机版轻量网页 | 手机浏览器用户 | `/guides`、`/guides/:guideId`、8 套攻略、每套 3 档、触控清晰布局 | Unity 下载、完整对局模拟 |
| 完整 Unity 网页版 | 桌面浏览器与主动进入试玩的用户 | Vue 产品壳、确认后加载 Unity、WebGL 内容包、一图流创建/查看/试玩 | 未验收的临时构建 |
| 网页下载包 | 需要离线保存或自行托管的用户 | 与完整网页版同源的候选、启动说明、SHA-256 | Unity 工程、`Library`、日志、凭据 |
| 原生微信小程序 | 微信手机用户 | 原生一图流浏览与训练 | `web-view` 包装 WebGL、Unity 对局 |
| Unity 微信小游戏 | 微信游戏用户 | Unity 一图流查看、创建、导入与试玩入口 | 未配置远程资源的完整模拟器 |

当前产品默认把前四项作为网页发布主线。微信渠道暂停时仍写 `not-in-scope`，不要删除历史状态。

## 2. 统一版本身份

一次发布记录必须写明以下身份：

- Git 源提交；脏工作区只能标记为 `dirty-local-acceptance`
- 攻略目录 `catalogRevisionId`
- Web 产品壳版本和构建时间
- Unity `bundleVersion`
- WebGL `buildId`、`contentVersion`、`packageFingerprint`
- Preview 与 Production 地址及 deployment 标识
- 下载包文件名、字节数和 SHA-256
- 本轮包含的提交块及每块提交 SHA
- 微信渠道版本号、AppID、包体和平台状态，仅在纳入范围时填写

正式候选必须来自干净且已提交的源状态。通过本地验收的 dirty 候选不能标记为 Production。

## 3. 按固定块提交

每块只暂存表中列出的范围。禁止使用 `git add .`，也不要把 `WebApp/dist`、`Builds`、`Library`、`Temp` 或平台凭据提交到仓库。

| 顺序 | 建议提交主题 | 典型路径 | 放行条件 |
| ---: | --- | --- | --- |
| 1 | `feat(content): freeze shared guide and card data` | Unity 权威攻略、卡牌数据、共享规则和测试 | Unity 编译与精确规则测试通过 |
| 2 | `feat(web-mobile): ship lightweight guide experience` | `WebApp/src`、`WebApp/public/data`、手机牌图、Web 测试 | `npm test`、轻量构建和手机浏览器验收通过 |
| 3 | `feat(web-unity): ship full browser play flow` | Unity UI、`Assets/WebGLTemplates`、`.jslib`、`WebApp/scripts/attach-unity.mjs`、WebGL 测试 | WebGL 构建、完整网页 smoke 和 PNG 下载通过 |
| 4 | `build(web): package and configure web release` | `Tools/Release`、`Tools/WebGL`、`WebApp/functions`、响应头和托管配置 | 候选自校验、预压缩响应、ZIP 和 SHA-256 通过 |
| 5 | `feat(miniprogram): update native one-sheet client` | `MiniProgram`、移动投影脚本和测试 | 仅在纳入范围时执行 |
| 6 | `feat(wxgame): update Unity mini-game client` | 微信 Unity SDK、模板和渠道构建器 | 仅在纳入范围时执行 |
| 7 | `docs(release): record delivery surfaces` | 本文件、文档索引和版本记录 | 每个交付面状态、证据和回滚目标完整 |

共享数据先进入第 1 块。手机与桌面共用的 Web 组件进入第 2 块，并在提交说明中列出两个受影响页面；不要复制实现来制造两个提交。

每块执行以下检查：

```powershell
git status --short
git diff --check
git add -- <本块明确文件列表>
git diff --cached --check
git diff --cached --stat
git commit -m "<本块主题>"
```

提交、push、Preview 部署和 Production 部署是四个独立动作。执行正式上传前必须明确授权范围；一旦进入已授权的正式发布流程，仍须按第 0 节完整记录每个动作的结果。

## 4. 执行共享预检

共享预检先冻结内容，再生成手机版网页使用的投影。

1. 写明本轮目标、交付面和回滚版本。
2. 保护工作区已有修改；正式发布前完成分块提交。
3. 运行受影响的 Unity EditMode 与 PlayMode 测试。
4. 同步 Web 与小程序共用的一图流移动投影：

```powershell
python Tools\Release\sync-mini-program-content.py
```

5. 确认 `WebApp/public/data/guides.json` 可解析，包含 8 套攻略且每套包含 3 档。
6. 确认生成的牌图都能从攻略条目解析到本地资源。
7. 执行 `git diff --check`，记录源状态是否为 dirty。

## 5. 验收手机版轻量网页

手机版网页必须在不下载 Unity 的情况下完成浏览、选档和操作步骤阅读。

### 5.1 构建轻量站点

```powershell
Push-Location WebApp
npm test
npm run build
npm run preview -- --host 127.0.0.1
Pop-Location
```

普通 `npm run build` 不附加 Unity 候选。使用真实手机浏览器或 Chromium 设备模拟器验收，不要只检查 Vite 构建退出码。

### 5.2 检查手机用户路径

至少检查 390×844 和 430×932 两个视口：

1. 从 `/` 直接进入一图流。
2. 打开 `/guides`，确认 8 套攻略可见。
3. 直接访问一个 `/guides/:guideId` 深链接，确认刷新后仍能打开。
4. 切换同一攻略的 3 个难度档。
5. 检查标题、核心卡、操作顺序、开局位置和目标阵容是否清晰。
6. 检查页面无横向溢出，底部操作不会遮挡正文。
7. 检查可点击目标在触控下可用，文本不依赖悬停显示。
8. 检查浏览器错误为 0，卡图和数据请求没有非预期失败。
9. 检查 `/unity/` 请求数为 0，页面未创建 Unity `iframe`。

发布记录还要写入首屏传输量和主要资源数量。若它们高于上一 Production，写明原因和是否接受，不要用“性能正常”代替数字。

## 6. 验收完整 Unity 网页版

完整网页版只有在用户明确确认后才加载 Unity，并且必须验证真实 WebGL 数据传输与一图流下载。

### 6.1 生成和附加候选

1. 从已经 push 的最终 SHA 创建干净独立工作树；使用现有 Unity Editor 的 `Temp/WebGLReleaseBuild.request` 入口生成 `Builds/WebGL/<版本>`，不要启动第二个 Editor。
2. 使用 `Tools/Release/assemble-release-candidate.mjs` 组装 `Builds/ReleaseCandidate/<buildId>`。
3. 从 `WebApp` 附加已经验收结构的候选：

```powershell
Push-Location WebApp
npm test
npm run build:with-unity -- "../Builds/ReleaseCandidate/<candidate>"
npm run preview -- --host 127.0.0.1
Pop-Location
```

4. 通过 HTTP 验收 `WebApp/dist`。不要双击 `index.html`。

### 6.2 检查完整浏览器路径

至少检查 390×844、1280×720 和 1600×900 三个视口：

1. 打开 `/play`，确认页面在点击确认前不请求 Unity。
2. 点击确认，检查 Unity loader、WASM、framework、data 和内容分片全部成功。
3. 检查 Brotli 资源返回正确的 `Content-Encoding: br` 和 MIME 类型。
4. 检查一图流查看、创建、阵容码导入和目标档位试玩入口。
5. 检查 Console、page error 和 request failure；阻断性错误必须为 0。
6. 从一图流查看页点击下载，确认浏览器产生 download 事件。
7. 检查下载文件为非空 PNG，尺寸为 1600×900。
8. 桌面浏览器检查原生网页全屏的进入与退出；手机检查沉浸式/PWA 引导、竖屏提示和无法调用原生全屏时的清晰回退。

稳定验收链路上的完整 Unity 冷启动目标仍为 300s 内。超时或断连时先分层定位：如果存在资源 4xx/5xx、哈希不一致、加载器报错，或在稳定网络上可复现，则阻断发布；如果仅特定机器到 `*.pages.dev` 出现 TLS/连接重置，而同一 `dist` 在本地 `wrangler pages dev` 可完整启动、线上路由与响应头正常，则记录吞吐、错误和交叉证据，不把外部链路问题误判为 Unity 构建失败，也不得仅为过门禁而调大超时。

## 7. 决定 Preview 与 Production

手机版网页和完整网页版可以位于同一站点，但必须分别记录状态。按以下规则放行：

- **两者都在范围内**：两者都通过后，才可从同一冻结 `dist` 创建 Production deployment。
- **只发布手机版网页**：使用 `npm run build` 生成不含 Unity 的站点，并在页面与发布记录中标明完整试玩不在本轮范围。
- **手机版通过、完整网页版失败**：可以保留 Preview 供排查，不能把包含失败交付面的 deployment 标记为 Production。
- **dirty 候选**：只能标记 `local-verified` 或 `preview`，不能标记 Production。
- **Preview 通过**：不等于 Production；完成 `main` Production 部署后还要在正式域名复验同一用户路径。

网页状态统一使用 `not-in-scope`、`local-verified`、`preview` 或 `production`。

Cloudflare Pages 当前标准命令如下；`<preview-branch>`、`<sha>` 和提交说明必须替换成本轮真实值：

```powershell
Push-Location WebApp

# 本地必须验收 Cloudflare Functions 与响应头，不以 vite preview 代替最终 Pages smoke。
npx wrangler pages dev dist --port 4180

# Preview：记录 CLI 返回的 URL 与 deployment ID。
npx wrangler pages deploy dist `
    --project-name learn-heartstone `
    --branch <preview-branch> `
    --commit-hash <sha> `
    --commit-message "<本轮 Preview 说明>"

# Preview 放行后，仍使用同一个未修改的 dist 创建 Production。
npx wrangler pages deploy dist `
    --project-name learn-heartstone `
    --branch main `
    --commit-hash <sha> `
    --commit-message "<本轮 Production 说明>"

Pop-Location
```

部署前后都用 `npx wrangler pages deployment list --project-name learn-heartstone` 记录 deployment ID、环境、分支和源提交。Production 前记录上一良好 deployment；Production 后确认 `jsoncool.com/unity/release-meta.json` 的 `sourceCommit`、`sourceDirty` 和本轮候选一致。

## 8. 生成和验收网页下载包

下载包必须来自已经通过完整浏览器验收的同一个 ReleaseCandidate，不要重新构建 Unity。

```powershell
$candidate = '<已验收 ReleaseCandidate 绝对路径>'
$version = '<版本或 buildId>'
$outputRoot = 'Builds\DownloadPackage'
$zip = Join-Path $outputRoot "LearnHeartstone-Web-$version.zip"
$packageFiles = @(
    $candidate,
    'Tools\WebGL\serve_webgl.py',
    'Tools\Release\WebGLDownloadPackage-README.zh-CN.txt'
)

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
Compress-Archive -LiteralPath $packageFiles -DestinationPath $zip -CompressionLevel Optimal
Get-FileHash -LiteralPath $zip -Algorithm SHA256
```

把 ZIP 解压到新的临时目录，再用包内 `serve_webgl.py` 完成 HTTP smoke。检查 `release-meta.json`、`content/content-manifest.json`、部署头、全部分片、服务器脚本和启动说明；不要只检查压缩命令退出码。

## 9. 使用统一验收矩阵

任一“必须”项失败，对应交付面不得标记为完成。

| 门禁 | 手机版轻量网页 | 完整 Unity 网页版 | 网页下载包 |
| --- | :---: | :---: | :---: |
| 权威攻略与卡牌数据可解析 | 必须 | 必须 | 必须 |
| 8 套攻略 × 3 档 | 必须 | 必须 | 继承完整网页版 |
| 390×844 与 430×932 | 必须 | 补充检查 | 不适用 |
| 无横向溢出与遮挡 | 必须 | 必须 | 继承完整网页版 |
| 确认前 0 Unity 请求 | 必须 | 必须 | 必须 |
| Unity 测试与 WebGL 数据加载 | 不适用 | 必须 | 继承完整网页版 |
| Brotli 响应头与分片完整 | 不适用 | 必须 | 解压后必须 |
| 1600×900 PNG 浏览器下载 | 不适用 | 必须 | 解压后必须 |
| 浏览器错误为 0 | 必须 | 必须 | 解压后必须 |
| 版本、哈希和证据记录 | 必须 | 必须 | 必须 |
| 外部状态 | Preview/Production | Preview/Production | 下载地址 |

微信渠道纳入范围时，继续按 [原生小程序交接文档](../MiniProgram/README.md) 和对应 Unity 微信渠道计划验收。它们不能继承网页状态，也不能把开发版上传写成正式发布。

### 9.1 Windows 桌面下载包补充门禁

Windows 原生包不继承 WebGL 的“浏览器可运行”结论，至少需要以下独立证据：

1. 构建源 SHA 已 push，工作树干净，`sourceDirty` 为 `false`。
2. ZIP 内的内容清单与当前上线 WebGL ReleaseCandidate 字节一致。
3. 标题栏最大化按钮可用，窗口边框可拖动，实际最大化会改变窗口尺寸。
4. D3D11、D3D12 均能正常开窗并从标题栏关闭，Exit 0、无强制终止、无新增 dump。
5. ZIP 解压后重复启动/退出 smoke，不能只验证打包前目录。
6. R2 使用新对象键和 immutable 缓存；上传后从 R2 完整下载回读并重算 SHA-256。
7. 正式域名的下载按钮、公开 manifest、Content-Length、Content-Type 与下载文件名全部一致。
8. CDN 长链路受本机网络影响时，如实记录已通过的范围和失败现象；可用 R2 管理端完整回读作为对象完整性证据，但不得虚写未完成的公网分段数量。

## 10. 复制发布记录模板

每次交付把以下模板复制到版本记录或本轮计划目录。保留所有交付面；未包含的交付面填写 `not-in-scope`。

```markdown
## <版本> 网页发布记录

### 共同身份

- 源提交：<sha / dirty-local-acceptance>
- 提交块：<提交主题与 sha>
- 攻略目录：<catalogRevisionId>
- Web 产品壳：<版本与构建时间>
- Unity：<bundleVersion>
- 回滚目标：<上一已知良好版本>

### 手机版轻量网页

- 状态：not-in-scope / local-verified / preview / production
- 地址：<URL>
- 视口：<390×844、430×932 结果>
- 攻略：<套数 × 档数>
- Unity 请求：<数量>
- 横向溢出：<数量>
- 浏览器错误：<数量>
- 首屏传输：<字节数与主要资源数>
- 证据：<截图、日志或报告路径>

### 完整 Unity 网页版

- 状态：not-in-scope / local-verified / preview / production
- 地址：<URL>
- WebGL：<路径>
- ReleaseCandidate：<路径>
- buildId / contentVersion / fingerprint：<值>
- 确认前 Unity 请求：<数量>
- 分片与 Brotli：<结果>
- 冷启动：<耗时与门限>
- 网络诊断：<吞吐、TLS/连接错误、是否属于客户端链路>
- PNG 下载：<文件、尺寸与结果>
- 浏览器错误：<数量>
- 证据：<截图、日志或报告路径>

### 网页下载包

- 状态：not-in-scope / packaged / published
- 文件：<路径或 URL>
- 字节数：<值>
- SHA-256：<值>
- 解压复验：<结果>

### 微信渠道

- 原生小程序：not-in-scope / local-verified / dev-uploaded / review-submitted / approved / published
- Unity 微信小游戏：not-in-scope / local-verified / dev-uploaded / review-submitted / approved / published
- 平台待办：<审核、发布、资质或无>

### 已知边界

- 未通过项：<交付面、现象和影响>
- 暂缓项：<原因>
- Production 决策：<部署、保持原版本或回滚>
```

## 11. 2026-08-12 已验证基准样例

以下值只用于核对“完整记录应该长什么样”，后续上传不得复用为新版本身份：

- 最终源提交：`b96d5441e7ed1dce0afae16248fe2e6857944b07`
- `buildId`：`20260812T082640Z-b96d544`
- `sourceDirty`：`false`
- `packageFingerprint`：`8f98bc12a2d8580adebc07c5f07ed490f206889a6dbc62a35053ef1a3934a3af`
- Preview：`5caa5210-8cb6-46f5-afb4-8de58aaf36b7`
- Production：`4cb42d49-f32b-4069-8510-56e3bc315af1`
- 当时的回滚目标：`c9ae9e3a-69f4-4a4a-b919-9f9cb7219a0a`
- ZIP SHA-256：`5063257066232BB3B793759DAB7137EAE1AFF3CA533DA7345965E424214F24B5`
- 当时的加载基线：12 个数据分片、6 路并发、每片最多 3 次重试并采用退避；这些是当前已验参数，后续只有经过本地完整启动和线上 Preview 对比后才可调整。
- 当时本地 Pages 完整 Unity 启动为 6.8 秒；特定机器到 `*.pages.dev` 曾出现约 348 KiB/s、TLS/连接重置，而 Cloudflare 增量上传本身只需数秒，因此发布记录必须区分“上传耗时”“用户资源下载”和“本机网络异常”。

## 12. 上轮错误候选记录（禁止复用）

这次网页交付发生在 2026-08-11 至 2026-08-12，源状态仍是 `dirty-local-acceptance`，因此没有晋升 Production。该候选现已被 2026-08-12 的干净提交链取代；它只作为失败证据保留，后续构建、部署和下载包不得复用。

### 12.1 手机版轻量网页

- 状态：`preview`
- Preview：[Learn Heartstone Cloudflare Preview](https://769b15fd.learn-heartstone.pages.dev)
- 390×844：8 套攻略、每套 3 档、0 Unity 请求、0 横向溢出、0 浏览器错误
- 路线：从首页直达一图流，`/guides` 与攻略详情可用
- 结论：手机版轻量页通过本轮 Preview 门禁

### 12.2 完整 Unity 网页版

- 状态：`preview`
- WebGL：r13
- ReleaseCandidate：`Builds/ReleaseCandidate/0.1.0-alpha__20260811T133749Z-cc03773-dirty`
- 本地 Chromium：确认前不加载 Unity、12/12 Brotli 分片、10,387,346 字节 WASM、入口、预览和 1600×900 PNG 下载均通过，浏览器错误为 0
- Cloudflare Preview：Pages Function 使用 `encodeBody: manual` 传递预压缩资源，实测分片返回 `Content-Encoding: br`
- 阻断项：本机到 Preview 的完整约 120 MB Unity 冷启动连续超过 300s 门限
- 结论：保留 Preview，`jsoncool.com` Production 不变

### 12.3 网页下载包

- 状态：`packaged`
- 文件：`Builds/DownloadPackage/LearnHeartstone-Web-20260811T133749Z-cc03773-dirty-r2.zip`
- 大小：120,779,352 字节
- SHA-256：`07F51461F86F312FFDC85499400D26FF6FBB609381008A38BD3A2C4D7B868470`
- 解压复验：52 个文件、加载器 8 路并发配置、入口、预览和 1600×900 PNG 下载通过

### 12.4 微信渠道

- 本轮状态：`not-in-scope`
- 原生小程序上一状态：`0.1.0` 已上传开发版，不代表审核或正式发布
- 当前方向：优先维护手机版网页；重新纳入微信渠道时再检查资质、包体与平台审核状态

## 13. 回滚与禁止事项

回滚必须使用已记录的上一良好版本，并保留本轮失败证据：

- 网页只回滚到已知良好的 Cloudflare deployment，或从已冻结的已知良好产物重新部署；不现场修改候选目录。
- 下载包保留上一版本 ZIP 与 SHA-256；发现问题时恢复上一下载链接。
- 不提交构建物、临时目录、预览二维码、AppSecret、API Token 或自动化运行时缓存。
- 不把 Preview 写成 Production，不把 dirty 候选写成正式发布。
- 不因某个渠道暂缓而改写共享攻略规则；渠道只负责投影和呈现。
- 不删除失败记录；写明失败门禁、影响范围和下一次复验条件。

## 14. 当前发布记录

- [2026-08-12 Windows 下载与网页文案热修发布记录](Releases/2026-08-12-windows-download-hotfix.md)
- [2026-08-12 手机版网页、完整 Unity 网页版与下载包发布记录](Releases/2026-08-12-web-release.md)
- 后续操作以 Windows 热修记录为最新基准，并从对应 Git 源提交创建干净工作树；不得从主工作区的未提交文件或第 12 节旧候选上传。
- Preview、Production、下载包和发布后复验结果统一回填到该记录，后续发布沿用同一结构新建文件。
