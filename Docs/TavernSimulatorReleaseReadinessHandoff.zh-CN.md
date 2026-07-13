# 酒馆模拟器发布就绪工作交接

更新时间：2026-07-12（Asia/Shanghai）

## 1. 交接目标

继续完成《酒馆模拟器》在“不增加新功能”前提下的发布就绪工作。当前任务/奖励、饰品、时空酒馆、英雄机制、中文运行时日志及 L3/L4 玩家旅程均已完成；唯一尚未关闭的发布阻塞是 Windows Player 的退出崩溃，以及正在搭建的 IL2CPP 绕行验证。

新对话应先阅读本文件，再读取以下三份治理文档和持久计划：

- `Docs/TavernSimulatorGlobalRequirements.zh-CN.md`
- `Docs/TavernSimulatorOptimizationPlan.zh-CN.md`
- `Docs/TavernPlayerPerspectiveTestingStandard.zh-CN.md`
- `.planning/tavern_release_readiness_20260711/task_plan.md`
- `.planning/tavern_release_readiness_20260711/findings.md`
- `.planning/tavern_release_readiness_20260711/progress.md`
- `.planning/tavern_release_readiness_20260711/evidence_matrix.md`

## 2. Git 与工作区状态

- 当前分支：`codex/wip-current-state`
- 已有检查点提交：`60c231d feat: checkpoint tavern release readiness work`
- 当前已验证但尚未提交的主要源码：
  - `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
  - `Assets/LearnHearthstone/Tests/EditMode/HeroPowerBuddyEffectTests.cs`
  - `Assets/LearnHearthstone/Tests/EditMode/MatchServiceTests.cs`
  - `Assets/LearnHearthstone/Tests/EditMode/QuestSystemTests.cs`
  - `Assets/LearnHearthstone/Tests/EditMode/TrinketSystemTests.cs`
- IL2CPP 诊断已使 `ProjectSettings/ProjectSettings.asset` 增加：

  ```yaml
  scriptingBackend:
    Android: 1
    Standalone: 1
  ```

  `Standalone: 1` 表示 Windows Standalone 使用 IL2CPP。只有 IL2CPP 最终验证失败并决定放弃该方案时才回退；若验证通过，应保留并提交。
- 临时诊断资产仍存在：
  - `Assets/__Diagnostics/EmptyExitProbe.unity`
  - `Assets/__Diagnostics.meta`

  不要在 IL2CPP 空场景验证完成前删除。验证完成后必须通过 Unity `AssetDatabase.DeleteAsset("Assets/__Diagnostics")` 清理，确认 `SampleScene` 仍为活动场景。
- 不要 reset、checkout 或覆盖用户工作。只提交本任务明确涉及的文件。

## 3. 已完成的产品与质量工作

### 3.1 任务/奖励

已完成任务 offer、选择、进度、完成、替换与奖励效果的中英文运行时文本，包括 Ethereal Evidence、Stolen Gold、Evil Twin、Ritual Dagger、Buddy/Discover、延迟饰品、Norgannon、Magicfin、Theotar、Staff of Origination 等。

- 完整回归：`a8e23e3da2e4484a97e5819f659a9afa`，66/66 passed。

### 3.2 饰品

已完成选择、装备、替换、商店、发现、战斗和计数效果日志。中文模式不再泄漏 CardId、`ImplementationStatus`、`proxy`、`placeholder-92` 或原始枚举。

- 完整回归：`4b96f5a0561a476b8c11aae38b2b81ac`，225/225 passed。

### 3.3 时空酒馆

已完成开启/阻塞/退出、实际 Chronum、购买与购买时施放、Big Winner、Lucky Egg、Zerus、Evolution、Beanstalk、Lava Lurker、Archimonde 等日志，并移除 `rule-unconfirmed`。

- MatchService：`d7ae246aff0546cf929667de7d4197e0`，177/177 passed。
- PJ-04 单条 L4：`57079a7cc30c4cce8ac8db4691d33ea6`，1/1 passed。

### 3.4 英雄与对手机制

已完成 Faelin、Thorim、Galewing、Bru'kan、Cariel、第二英雄技能、关键 Discover、对手英雄技能/任务/饰品配置及 HeroEffectEngine 中文摘要。

- Hero/伙伴：`b0ff9962bd744fd3864634a029a937fa`，148/148 passed。
- OpponentMechanicConfigurationTests：`0513861ab5f8405db895ea13539a4557`，9/9 passed。
- PJ-06 L4：`6ee157cfc33340db965c0f5f91eda89b`，1/1 passed。

### 3.5 跨组日志与玩家旅程

- 中文 RecruitLog 边界安全网已完成：保留正常中文，拦截纯英文、乱码、proxy/debug/placeholder/status 等泄漏；英文模式保持英文。
- 六类 EditMode：`0846cb6f53ad44fc906e81ecc27f04bb`，694/694 passed。
- 四类 PlayMode 旅程：`280413ab78a24503876d39ffcde0cd61`，12/12 passed。
- 最终 MatchService：`f5fec7a3fcf846eda2572bf3a7af948c`，177/177 passed。
- PJ-04、PJ-05、PJ-06 均达到真实 EventSystem/GraphicRaycaster 输入的 L4 证据。
- 五种目标分辨率和 200% DPI 几何映射已通过：

  | 请求物理尺寸 | 客户区逻辑尺寸 | DPI | 换算物理尺寸 |
  | --- | --- | --- | --- |
  | 1920×1080 | 960×540 | 192 | 1920×1080 |
  | 1366×768 | 683×384 | 192 | 1366×768 |
  | 1280×720 | 640×360 | 192 | 1280×720 |
  | 1000×600 | 500×300 | 192 | 1000×600 |
  | 994×384 | 497×192 | 192 | 994×384 |

## 4. Mono Windows Player 阻塞与根因证据

最终 Mono Windows x64 候选构建成功：

- Unity：6000.4.10f1
- Job：`build-e616d842a7`
- 输出：`Builds/Windows/Learn Heartstone.exe`
- 大小：555.0 MB
- 时长：92.1 秒
- 0 errors，9 build warnings

但可见窗口关闭会稳定产生：

- 退出码：`0xC0000005`（PowerShell signed value `-1073741819`）
- 故障模块：`UnityPlayer.dll`
- 固定 fault offset：`0x107b81`
- 固定 WER bucket：`c76830ba8984577863d825dd0cffceff`
- 本机 dump：`%LOCALAPPDATA%/CrashDumps/Learn Heartstone.exe.*.dmp`

已经排除：

- 窗口出现后关闭过早：等待 5 秒仍崩溃。
- 测试脚本伪造关闭：真实 Alt+F4 同样崩溃。
- D3D11：强制 D3D12 同样崩溃。
- Windows.Gaming.Input：XInput 对照包仍在同一地址崩溃。
- Unity Connect/Curl：关闭遥测后 Curl 行消失，但崩溃不变。
- 酒馆业务/场景代码：真正的空场景 Player 仍在同一地址崩溃。

真正空场景证据：

- Job：`build-ffb8e1868d`
- 空场景 `level0`：764 bytes。
- 正常场景 `level0`：2448 bytes。
- 二者 SHA-256 不同。
- 空场景仍以 `UnityPlayer.dll+0x107b81` 崩溃。

因此 Mono 退出问题属于 Unity 6000.4.10f1 / Mono Player / 当前 Windows 原生环境，不应继续修改酒馆业务代码来掩盖。

## 5. IL2CPP 当前状态（最重要）

用户已为 Unity 6000.4.10f1 安装 Windows Build Support (IL2CPP)。已确认以下 variations 存在：

- `win64_player_development_il2cpp`
- `win64_player_nondevelopment_il2cpp`
- 对应 win32/ARM64 IL2CPP variations

Unity Editor 已重启，MCP 实例端口为 **6400**。不要继续使用旧的 6401 作为实例选择值。

首次 IL2CPP 空场景构建：

- Job：`build-4df9071864`
- 输出目标：`Builds/Windows-IL2CPP-Empty-Probe/Learn Heartstone Empty.exe`
- 结果：构建失败，未产生可验收 Player。
- 唯一根因：缺少 Windows C++ 工具链。

Unity 的精确错误：

- Visual Studio Community 2026 已安装在 `D:\vs2026`，但没有 C++ tool components。
- 未安装/未注册 Windows SDK 10.0.19041.0 或更新版本。
- Unity 需要 VS 2019+ 的 C++ x64 compiler 和 Windows SDK ≥ 10.0.19041。

已经执行的 Visual Studio 修改命令：

```powershell
"C:\Program Files (x86)\Microsoft Visual Studio\Installer\setup.exe" modify `
  --installPath "D:\vs2026" `
  --add Microsoft.VisualStudio.Workload.NativeDesktop `
  --includeRecommended `
  --passive `
  --norestart
```

安装现已完成，不要重复启动安装。已验证：

- `Microsoft.VisualStudio.Workload.NativeDesktop` 可被 `vswhere` 检出。
- `Microsoft.VisualStudio.Component.VC.Tools.x86.x64` 可被 `vswhere` 检出。
- Windows SDK `10.0.26100.0` 已安装在 `C:\Program Files (x86)\Windows Kits\10\Lib\10.0.26100.0`。
- VS 状态为 complete/launchable，`isRebootRequired=false`。

新对话应直接从“重跑 IL2CPP 空场景”开始，不需要等待安装或重启系统。

## 6. 新对话的精确续作步骤

### 第一步：恢复技能和计划

按 `AGENTS.md` 使用：

- `planning-with-files`
- `troubleshoot`
- `Confidence Check`
- `unity-developer`
- 编码时使用本地 Ponytail `Tools/ponytail/skills/ponytail/SKILL.md`

读取本交接文档和 `.planning/tavern_release_readiness_20260711/` 全部四份核心计划/证据文件。

### 第二步：快速复核 VS C++ 与 SDK（已安装）

先检查 Visual Studio Installer 是否仍运行：

```powershell
Get-Process setup,vs_installer,vs_installershell -ErrorAction SilentlyContinue
```

再检查组件：

```powershell
& "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" `
  -products * `
  -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
  -format json -utf8
```

检查 Windows SDK：

```powershell
Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\Lib" -Directory
Get-ItemProperty "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SDKs\Windows\v10.0"
```

当前已确认 SDK 为 10.0.26100.0，且 MSVC x64 tools 可被 `vswhere` 检出；复核用于防止新对话在环境变化后直接误判。

### 第三步：重跑 IL2CPP 空场景

确认 Unity 只开一个 Editor，MCP 使用端口 6400。临时场景已经存在：

```text
Assets/__Diagnostics/EmptyExitProbe.unity
```

通过 Unity MCP `manage_build` 重跑：

```json
{
  "action": "build",
  "target": "windows64",
  "output_path": "Builds/Windows-IL2CPP-Empty-Probe/Learn Heartstone Empty.exe",
  "scenes": "[\"Assets/__Diagnostics/EmptyExitProbe.unity\"]",
  "development": "false",
  "options": "[\"clean_build\",\"strict_mode\",\"detailed_report\"]",
  "subtarget": "player",
  "scripting_backend": "il2cpp"
}
```

构建结果必须是非零有效输出、0 errors。首次 IL2CPP 可能需要数分钟。

然后运行 994×384 可见窗口，等待窗口响应并稳定 5 秒，再通过真实 Alt+F4 或 `WM_CLOSE` 退出。必须检查：

- 退出码是否为 0。
- Windows Application Event 1000 是否新增。
- Player.log 是否含 Exception/Error/Fatal、ComputeBuffer/GraphicsBuffer、Curl、资源缺失。

若空 IL2CPP 仍在同一地址崩溃，IL2CPP 绕行失败，回退 `Standalone: 1`，保留 Public Release No-Go，并记录引擎/驱动外部阻塞。

### 第四步：完整 IL2CPP 候选

只有空场景退出干净后才继续：

1. 删除临时诊断资产：

   ```csharp
   UnityEditor.AssetDatabase.DeleteAsset("Assets/__Diagnostics");
   UnityEditor.AssetDatabase.Refresh();
   ```

2. 确认活动场景为 `Assets/Scenes/SampleScene.unity`。
3. 以 IL2CPP、Windows x64、non-development、clean build 构建完整候选到：

   ```text
   Builds/Windows/Learn Heartstone.exe
   ```

4. 记录 job id、大小、耗时、errors/warnings。
5. 依次验证五种分辨率及 200% DPI 映射。
6. 每个尺寸等待稳定后退出，要求 exit code 0 且无新增 WER APPCRASH。
7. 扫描所有新 Player.log：
   - Exception / Error / Fatal
   - ComputeBuffer / GraphicsBuffer
   - Curl
   - missing resource
   - raw English / mojibake
   - proxy / debug / placeholder / implementation status

### 第五步：Git 收尾

所有 IL2CPP Player 门槛通过后：

1. 更新 `.planning/tavern_release_readiness_20260711/` 的计划、发现、进度、证据矩阵。
2. `git diff --check`。
3. 审查并暂存五个已验证源码文件。
4. 若 IL2CPP 成为正式 Windows 后端，同时暂存 `ProjectSettings/ProjectSettings.asset`。
5. 确认不存在 `Assets/__Diagnostics*`。
6. 提交建议：

   ```text
   feat: finalize tavern runtime localization and release candidate
   ```

7. 输出最终 Public Release Go/No-Go 报告。

## 7. 严格约束

- 不增加新功能。
- 尽可能符合《炉石传说：酒馆战棋》的名称、行为和玩家反馈。
- 不启动第二个 Unity Editor。
- `0 tests` 绝不算通过。
- L3/L4 必须来自真实 EventSystem/GraphicRaycaster 输入。
- 使用 `apply_patch` 修改文本文件。
- 不使用 `git reset --hard`、`git checkout --` 或覆盖用户修改。
- 不因测试困难降低断言或伪造上线结论。
- Public Release 目前仍是 **No-Go**；只有完整 IL2CPP 构建、五分辨率、干净退出和日志门槛全部通过后才能改为 Go。

## 8. 给新对话的第一句话建议

可在新对话中直接发送：

> 请先完整读取 `Docs/TavernSimulatorReleaseReadinessHandoff.zh-CN.md`，按其中“新对话的精确续作步骤”继续。Visual Studio C++ workload、MSVC x64 与 Windows SDK 10.0.26100.0 已安装，Unity MCP 使用 6400；直接重跑 IL2CPP 空场景。不要重做已完成的任务/奖励、饰品、时空、英雄和 L4 旅程。
