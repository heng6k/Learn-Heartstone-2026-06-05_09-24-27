# Unity WebGL ReleaseCandidate 与 Vercel 发布指南

## 1. 文档目的

本文说明如何把 Unity、Prefab、浏览器外壳或内容真源的修改，构建为可审计的 WebGL ReleaseCandidate，并按 Preview → smoke → Promote 的顺序发布到 Vercel。

固定发布链如下：

```mermaid
flowchart LR
    A["修改源码真源"] --> B["Unity 测试与编译门禁"]
    B --> C["构建 Builds/WebGL"]
    C --> D["组装 ReleaseCandidate"]
    D --> E["本地 HTTP 验收"]
    E --> F["Vercel Preview"]
    F --> G["线上 smoke"]
    G --> H["Promote 同一 Deployment"]
    H --> I["Production smoke"]
    I --> J["自定义域名验收"]
```

禁止把 Unity 工程、`WebDeploy/` 或重新组装的另一份候选直接作为 Production 输入。

## 2. 当前发布边界

当前 Vercel 配置：

- Team：`heng6ks-projects`
- Project：`hengheng`
- Project ID：`prj_Zp39f5gUOYF0DMWsyllfu7bEdRov`
- Framework Preset：`Other`
- Root Directory：项目根，即 `null`
- Git Integration：已断开；源码 push 不会自动部署
- 稳定 Production：<https://hengheng-one.vercel.app/>
- 自定义域名：`jsoncool.com`，等待阿里云 DNS A 记录生效后验收

`WebDeploy/` 已退出发布主链。它只允许作为本机短期保留的旧生成物镜像，已被 Git ignore，不再同步、不再提交，也不再作为 Vercel Root Directory。

## 3. 真源与生成物

| 类型 | 路径 | 责任 |
| --- | --- | --- |
| Unity/C#、场景、Prefab、测试 | `Assets/LearnHearthstone/` | 游戏与 UI 源码真源 |
| 浏览器外壳 | `Assets/WebGLTemplates/LearnHeartstone/` | HTML、Canvas 容器、响应式样式真源 |
| Vercel 配置 | `Deploy/Vercel/vercel.json` | MIME、Brotli、缓存和 SPA rewrite 唯一真源 |
| 发布工具 | `Tools/Release/` | 组装并校验 ReleaseCandidate |
| 内容真源 | `Assets/LearnHearthstone/Resources/Data/` | 唯一人工编辑内容真源与内置回退 |
| Unity WebGL 输出 | `Builds/WebGL/<版本>/` | 生成物，不进源码 Git |
| 发布候选包 | `Builds/ReleaseCandidate/<版本>/` | Preview 与 Production 的唯一部署输入，不进源码 Git |
| 旧部署镜像 | `WebDeploy/` | 已退役生成物，本地可暂留但不跟踪 |

长期修改必须回到对应真源。不要直接修改 ReleaseCandidate 或 `WebDeploy` 来代替源码修复。

## 4. 发布前门禁

以下命令默认从项目根目录执行：

```powershell
Set-Location 'D:\unity project\Learn Heartstone'
git status --short --branch
git branch --show-current
git diff --check
```

开始构建前确认：

- 当前改动范围清楚，没有覆盖其他未完成工作。
- Unity 使用项目固定版本，脚本编译成功且 Console 没有未解释错误。
- 相关 EditMode/PlayMode 测试已通过。
- 内容版本没有复用为不同字节；修改内容真源时必须使用新的 `contentVersion`。
- 当前项目若已有 Unity Editor，保持该实例并使用 Editor request；禁止启动第二实例。

## 5. 构建 WebGL

### 5.1 已有 Unity Editor 正在运行

使用项目内 `WebGLReleaseBuild` request runner，让现有 Editor 执行构建：

```powershell
$project = 'D:\unity project\Learn Heartstone'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$output = Join-Path $project "Builds\WebGL\LearnHeartstone_$stamp"
$request = Join-Path $project 'Temp\WebGLReleaseBuild.request'
$result = Join-Path $project 'Temp\WebGLReleaseBuild.result'

if (Test-Path -LiteralPath $request) {
    throw "已有未消费的 WebGL build request：$request"
}

Remove-Item -LiteralPath $result -ErrorAction SilentlyContinue
[System.IO.File]::WriteAllText(
    $request,
    $output,
    [System.Text.UTF8Encoding]::new($false)
)

$deadline = (Get-Date).AddMinutes(40)
while (-not (Test-Path -LiteralPath $result)) {
    if ((Get-Date) -gt $deadline) {
        throw "等待 WebGL 构建结果超时；检查 Unity Console 和 Editor.log，不要重复创建 request。"
    }
    Start-Sleep -Seconds 2
}

Get-Content -LiteralPath $result
```

结果第一行必须为 `success`，第二行是最终输出绝对路径。失败时先读取异常和 Editor.log；不要重复启动同一构建。

### 5.2 没有 Unity Editor 正在运行

只有确认项目没有其他 Editor 实例后，才使用批处理入口：

```powershell
$project = 'D:\unity project\Learn Heartstone'
$unity = 'D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$output = Join-Path $project "Builds\WebGL\LearnHeartstone_$stamp"
$log = Join-Path $project "Logs\WebGLBuild_$stamp.log"

New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null
& $unity `
    -batchmode -nographics -quit `
    -projectPath $project `
    -executeMethod WebGLReleaseBuild.BuildFromCommandLine `
    -webglOutput $output `
    -logFile $log

if ($LASTEXITCODE -ne 0) {
    throw "WebGL 构建失败，请检查：$log"
}
```

## 6. 组装 ReleaseCandidate

```powershell
$contentVersion = '20260727' # 内容真源变化时改为新版本
node Tools\Release\assemble-release-candidate.mjs `
    --webgl $output `
    --content-version $contentVersion
```

脚本只允许输出到 `Builds/ReleaseCandidate/`，且不会联网或部署。把输出中的 `ReleaseCandidate:` 路径保存为 `$candidate`。

候选包至少包含：

```text
index.html
Build/
TemplateData/
release-meta.json
vercel.json
content/
  content-manifest.json
  battlegroundsMinions.v<contentVersion>.json
```

复核：

```powershell
Get-ChildItem -LiteralPath $candidate
Get-ChildItem -LiteralPath (Join-Path $candidate 'Build')
Get-Content -LiteralPath (Join-Path $candidate 'release-meta.json')
Get-Content -LiteralPath (Join-Path $candidate 'vercel.json')
Get-Content -LiteralPath (Join-Path $candidate 'content\content-manifest.json')
git check-ignore -v -- $candidate
git ls-files -- $candidate
```

最后一条命令必须没有输出。ReleaseCandidate 一旦完成本地验收就冻结；后续 Preview 和 Production 必须使用同一目录、同一 deployment，不得重新组装。

## 7. 本地 HTTP 验收

Unity WebGL 不能通过双击 `index.html` 验收。使用支持 Brotli 响应头的本地服务器：

```powershell
python Tools\WebGL\serve_webgl.py "$candidate" --port 8125
```

至少检查：

- 主大厅能加载，中文和英文没有缺字、重影或截断。
- 桌面、紧凑横屏、手机横屏和竖屏提示正常。
- `Remote -> LKG -> Embedded Resources` 三条内容路径符合预期。
- `content-manifest.json` 不长期缓存；版本化内容和 Build 资源为 immutable。
- `.wasm.br`、`.framework.js.br`、`.data.br` 的 MIME 与 `Content-Encoding: br` 正确。
- 任意深链返回 `index.html`，页面包含 Unity Canvas。
- 浏览器 Console 没有未解释错误。

## 8. 部署 Vercel Preview

使用已登录的 Vercel CLI，或通过进程环境变量提供凭据。禁止把 token 放入 `--token`、日志、文件或命令输出。

先做无部署 dry-run，确认项目设置不会追加旧 `WebDeploy` 路径：

```powershell
npx --yes vercel@58.0.0 "$candidate" `
    --dry `
    --project hengheng `
    --scope heng6ks-projects
```

然后只创建 Preview：

```powershell
npx --yes vercel@58.0.0 "$candidate" `
    --archive=tgz `
    --project hengheng `
    --scope heng6ks-projects `
    --yes
```

记录 CLI 返回的 Preview URL 与 deployment ID，再检查：

```powershell
npx --yes vercel@58.0.0 inspect <preview-url-or-deployment-id> `
    --scope heng6ks-projects
```

Preview 必须为 Ready。若 Deployment Protection 开启，自动化验收只能使用项目现有 Automation Bypass，并且 secret 只允许留在单次父/子进程内存中；不要输出或写盘。

## 9. Preview 线上 smoke

Preview 至少通过以下矩阵：

| 用例 | 期望 |
| --- | --- |
| Remote | 选择当前远程内容版本，manifest 和版本文件成功请求 |
| LKG | 同一浏览器存储已有有效包，阻断 `content/**` 后选择 Last Known Good |
| Embedded | 全新浏览器存储首次启动即阻断 `content/**`，选择内置 Resources |
| Brotli/MIME | wasm/framework/data 响应头正确 |
| 缓存 | manifest 可重新验证；版本内容和 Build 长期 immutable |
| SPA | `/preview/deep-link` 返回 200/index 且包含 Unity Canvas |
| 浏览器 | 无加载超时、page error、严重 Console 或非预期请求失败 |

任一项失败时拒绝该 Preview。修真源、提交配置检查点、重新组装并部署新 Preview；不要原地修改已冻结候选。

## 10. 晋升 Production

只有 Preview 全部通过后，才晋升同一个 deployment：

```powershell
npx --yes vercel@58.0.0 promote <preview-deployment-id-or-url> `
    --scope heng6ks-projects `
    --yes
```

不要从另一个目录执行 `deploy --prod`，也不要为 Production 重新构建或重新组装。

晋升后在 <https://hengheng-one.vercel.app/> 重跑第 9 节完整 smoke，并核对关键资源 ETag/长度与 Preview 一致。

## 11. `jsoncool.com` DNS 与验收

Vercel 项目已经添加 `jsoncool.com`。当前域名使用阿里云 nameserver，推荐保留阿里云 DNS，并在阿里云 DNS 控制台添加：

```text
记录类型：A
主机记录：@
记录值：76.76.21.21
```

不要同时添加冲突的 apex A/AAAA/CNAME。保存后检查：

```powershell
Resolve-DnsName jsoncool.com -Type A
npx --yes vercel@58.0.0 domains inspect jsoncool.com `
    --scope heng6ks-projects
```

公共 DNS 返回 Vercel 地址且 Vercel 不再报告 misconfigured 后，再访问 <https://jsoncool.com/>，重跑 Production smoke 并确认 HTTPS 证书正常。

也可以把 nameserver 改成 `ns1.vercel-dns.com` / `ns2.vercel-dns.com`，但这会把整个域名 DNS 托管迁移到 Vercel；除非明确决定迁移，否则优先使用上面的 A 记录。

## 12. Git 与发布分离

源码 Git 只保存真源、测试和发布配置：

```powershell
git status --short
git diff --check
git add -- <本次源码和文档文件>
git diff --cached --check
git diff --cached --stat
git commit -m "<清晰的提交说明>"
```

固定规则：

- 不使用 `git add .` 混入无关工作。
- `Builds/**`、`WebDeploy/`、日志、临时文件和 `.vercel/` 不进入源码提交。
- Git push 需要单独确认；push 只发布源码，不触发 Vercel 部署。
- Vercel 发布只接受已冻结 ReleaseCandidate，并始终先 Preview 后 Promote。

## 13. 回滚

### 13.1 Production 回滚

出现 P0 问题时，在 Vercel Dashboard 选择最近已知良好 deployment 执行回滚，或重新 Promote 该已知良好 deployment。回滚后立即复跑 Production smoke。

### 13.2 内容故障

客户端会自动按 Remote → LKG → Embedded 回退。不要覆盖同一 `contentVersion` 的字节；修复内容必须生成新版本并走完整 Preview/Promote 流程。

### 13.3 源码回滚

源码历史与线上 deployment 分离。需要撤销源码时使用可审计的新 revert 提交，不要用破坏性 reset 覆盖本地或他人工作。

## 14. 常见问题

### Preview 寻找 `<candidate>/WebDeploy`

说明 Vercel 项目 Root Directory 又被设置为 `WebDeploy`。先停止部署，检查项目设置；正确值为项目根 `null`。

### Git push 后没有自动部署

这是当前预期行为。Git Integration 已主动断开，发布必须从 ReleaseCandidate 使用 Vercel CLI 创建 Preview。

### 页面 404 或深链失败

确认候选中的 `vercel.json` 与 `Deploy/Vercel/vercel.json` 字节一致，并包含 SPA rewrite：`/(.*) -> /index.html`。

### 页面白屏或一直加载

检查 Build 文件引用、Brotli/MIME 响应头、浏览器 Console、Unity loader 错误以及缓存是否仍命中旧文件。

### `jsoncool.com` 仍不可访问

先看公共 DNS 是否已有 `A 76.76.21.21`，再看 `vercel domains inspect`。域名刚修改时等待 DNS TTL 和证书签发，不要通过反复重新部署解决 DNS 问题。

## 15. 一页式发布检查清单

### 源码与构建

- [ ] 修改落在 Unity、WebGL Template、内容或 Vercel 配置真源。
- [ ] Unity 编译和相关测试通过。
- [ ] 没有启动第二个 Unity Editor。
- [ ] WebGL 输出使用新的版本/时间戳目录。
- [ ] ReleaseCandidate 组装成功、被 Git ignore 且 0 tracked 文件。
- [ ] 内容版本、字节数和 SHA-256 正确。

### Preview

- [ ] Vercel dry-run 不再寻找 `WebDeploy`。
- [ ] Preview deployment 为 Ready。
- [ ] Remote、LKG、Embedded 全部通过。
- [ ] Brotli、MIME、缓存和 SPA 深链通过。
- [ ] 浏览器无未解释错误。

### Production 与域名

- [ ] Promote 的是同一已验 Preview deployment。
- [ ] Production smoke 与 Preview 一致。
- [ ] `jsoncool.com` 公共 DNS 已指向 `76.76.21.21`。
- [ ] 自定义域名 HTTPS 与完整 smoke 通过。

### Git

- [ ] `WebDeploy/` 和 `Builds/**` 未进入索引。
- [ ] 只暂存本次源码、配置和文档。
- [ ] `git diff --cached --check` 通过。
- [ ] push 前已取得单独确认。
