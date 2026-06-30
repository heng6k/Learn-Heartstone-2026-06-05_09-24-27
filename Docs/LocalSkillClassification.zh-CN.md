# 本地 Skill 分类与使用路由

本文件盘点当前机器可见的本地 skills，并按任务场景分类。扫描范围包括：

- `C:\Users\wch\.codex\skills`
- `C:\Users\wch\.agents\skills`
- `C:\Users\wch\.codex\plugins\cache\openai-api-curated\github\3fdeeb49\skills`
- `C:\Users\wch\.codex\plugins\cache\openai-primary-runtime`
- `D:\unity project\Learn Heartstone\Tools\ponytail\skills`

共发现 57 个 `SKILL.md`。同名重复项：`skill-creator`、`pdf`。

## 使用原则

1. 先按任务场景选技能，不按技能名字猜。
2. 多个技能都适用时，先用流程/诊断类技能，再用领域技能。
3. 文档、表格、PPT、PDF 这类文件任务优先用对应文件技能。
4. UI/前端任务优先用 UI/前端技能，并用 Playwright 类技能验证。
5. GitHub PR、CI、评论、发布任务优先用 GitHub workflow 技能。
6. 编码实现时默认叠加 `ponytail` 的最小正确实现原则，但不要用它替代安全、正确性、可访问性和必要测试。

## 快速路由表

| 任务场景 | 优先使用 |
| --- | --- |
| 多步骤任务、长期任务、需要进度恢复 | `planning-with-files`, `pm` |
| 模糊需求、需要一起想方案 | `brainstorm` |
| 报错、bug、异常行为 | `troubleshoot`, 必要时再用 `ponytail` 做最小根因修复 |
| 开始实现前需要把握风险 | `Confidence Check` |
| 普通编码、修 bug、重构、依赖选择 | `ponytail` |
| 审查 diff 是否过度工程化 | `ponytail-review` |
| 全仓库查找可删除/可简化内容 | `ponytail-audit` |
| 汇总 `ponytail:` 延迟项 | `ponytail-debt` |
| Web/UI 设计、改版、体验优化 | `frontend-design`, `ui-ux-pro-max`, `web-design-guidelines` |
| React/Next 性能、组合模式、转场动画 | `vercel-react-best-practices`, `vercel-composition-patterns`, `vercel-react-view-transitions` |
| Web app 浏览器验证 | `webapp-testing` |
| Vercel 部署、token 部署、成本优化 | `deploy-to-vercel`, `vercel-cli-with-tokens`, `vercel-optimize` |
| Word/docx | `docx`, `documents` |
| PDF | `pdf`, `pdf:pdf` |
| PPT/slides/deck | `pptx`, `presentations` |
| Excel/CSV/表格 | `xlsx`, `spreadsheets` |
| 写文档、规范、报告、内部沟通 | `doc-coauthoring`, `writing-guidelines`, `internal-comms` |
| 创建/编辑图片、海报、GIF、媒体 | `imagegen`, `canvas-design`, `algorithmic-art`, `slack-gif-creator`, `mmx-cli` |
| OpenAI/Codex 官方信息 | `openai-docs` |
| Claude/Anthropic API | `claude-api` |
| MCP server | `mcp-builder` |
| 创建/安装/查找 skills 或插件 | `skill-creator`, `skill-installer`, `find-skills`, `plugin-creator` |
| GitHub repo/PR/issue 总览 | `github` |
| GitHub PR review comments | `gh-address-comments` |
| GitHub Actions CI 修复 | `gh-fix-ci` |
| 提交、推送、开 PR | `yeet` |

## 1. 流程、计划、诊断

| Skill | 来源 | 使用场景 | 边界 |
| --- | --- | --- | --- |
| `using-superpowers` | `.codex/skills` | 会话开始时建立 skill 使用规则 | 不是业务技能 |
| `planning-with-files` | `.codex/skills` | 多步骤任务、研究、跨多次工具调用的工作 | 简单问答不需要 |
| `pm` | `.codex/skills` | PDCA、项目管理、进度跟踪 | 不替代具体领域技能 |
| `brainstorm` | `.codex/skills` | 需求模糊、探索方案、创意发散 | 不直接实现 |
| `deep-research` | `.codex/skills` | 系统性研究、需要当前信息和引用 | 不是快速查找 |
| `troubleshoot` | `.codex/skills` | 报错、bug、异常行为，需要根因分析 | 不盲目重试 |
| `Confidence Check` | `.codex/skills` | 实现前做重复检查、架构一致性、官方文档核对 | 不应变成拖延实现 |
| `token-efficiency` | `.codex/skills` | 上下文紧张或用户要求极简输出 | 不用于需要完整解释的场景 |

## 2. 编码最小化与复杂度控制

| Skill | 来源 | 使用场景 | 边界 |
| --- | --- | --- | --- |
| `ponytail` | `Tools/ponytail` | 编码、修 bug、重构、依赖选择，追求最小正确实现 | 不用于非编码请求 |
| `ponytail-review` | `Tools/ponytail` | 当前 diff 的过度工程化审查 | 不做正确性/安全/性能审查 |
| `ponytail-audit` | `Tools/ponytail` | 全仓库查找可删代码、冗余抽象、可替换依赖 | 只报告，不自动改 |
| `ponytail-debt` | `Tools/ponytail` | 汇总 `ponytail:` 注释中的延迟项和升级触发条件 | 不创建新债务 |
| `ponytail-gain` | `Tools/ponytail` | 展示 Ponytail 官方 benchmark 收益 | 不估算当前项目收益 |
| `ponytail-help` | `Tools/ponytail` | 查看 Ponytail 命令、模式和用法 | 不改变代码 |

## 3. Web、UI、前端与移动端

| Skill | 来源 | 使用场景 | 边界 |
| --- | --- | --- | --- |
| `frontend-design` | `.codex/skills` | 新 UI、改版、视觉方向、排版、非模板化设计 | 不负责浏览器自动化验证 |
| `ui-ux-pro-max` | `.agents/skills` | Web/mobile UI/UX 设计、组件、布局、可访问性、配色 | 不替代项目代码阅读 |
| `web-design-guidelines` | `.codex/skills` | 审查 UI、可访问性、UX、设计规范 | 主要是 review |
| `webapp-testing` | `.codex/skills` | Playwright 截图、浏览器交互、控制台日志验证 | 需要本地服务或可打开页面 |
| `web-artifacts-builder` | `.codex/skills` | 复杂 HTML/React/Tailwind/shadcn artifact | 简单单文件 HTML 不必用 |
| `vercel-react-best-practices` | `.codex/skills` | React/Next 性能、数据获取、bundle 优化 | 不用于非 React/Next |
| `vercel-composition-patterns` | `.codex/skills` | React 组合组件、render props、compound components | 不用于普通小组件 |
| `vercel-react-view-transitions` | `.codex/skills` | React/Next 页面转场、共享元素动画、View Transition API | 不用于普通 CSS 动画 |
| `vercel-react-native-skills` | `.codex/skills` | React Native/Expo、移动端列表性能、动画、原生模块 | 不用于 Web-only 项目 |

## 4. Vercel、部署与性能成本

| Skill | 来源 | 使用场景 | 边界 |
| --- | --- | --- | --- |
| `deploy-to-vercel` | `.codex/skills` | 部署应用、创建 Vercel preview/live 链接 | 交互登录/token 场景要区分 |
| `vercel-cli-with-tokens` | `.codex/skills` | 使用 Vercel token 部署、配置项目和环境变量 | 不使用交互登录 |
| `vercel-optimize` | `.codex/skills` | Vercel 成本、性能、缓存、调用量、Core Web Vitals 优化 | 需要指标支撑，不凭空建议 |

## 5. 文档、PDF、PPT、表格与模板

| Skill | 来源 | 使用场景 | 边界 |
| --- | --- | --- | --- |
| `docx` | `.codex/skills` | 创建、读取、编辑 Word `.docx` | 不用于 PDF/表格 |
| `documents` | primary-runtime | `.docx`/Word/Google Docs 定向文档，含渲染视觉 QA | 更适合最终交付文档 |
| `pdf` | `.codex/skills` | PDF 提取、合并、拆分、旋转、水印、OCR、表单等 | 通用 PDF 操作 |
| `pdf:pdf` | primary-runtime | PDF 读取、创建、渲染、视觉检查 | 版式重要时优先 |
| `pptx` | `.codex/skills` | `.pptx`、slides、deck、presentation 读写编辑 | 不用于 Word/PDF |
| `presentations` | primary-runtime | 创建/编辑 PowerPoint 或 Google Slides deck | 更适合正式 deck 输出 |
| `xlsx` | `.codex/skills` | `.xlsx/.xlsm/.csv/.tsv` 作为主要输入/输出 | 交付物必须是表格 |
| `spreadsheets` | primary-runtime | 表格创建、修改、分析、图表、公式、重算 | 更适合复杂表格交付 |
| `template-creator` | primary-runtime | 从 Word/PPT/Excel 创建或更新可复用 artifact template skill | 不用于一次性 artifact |
| `doc-coauthoring` | `.codex/skills` | 共同撰写文档、技术规范、提案、决策文档 | 不是文件格式处理器 |
| `writing-guidelines` | `.codex/skills` | 审查文档/文案风格、语气、一致性 | 主要是 review |
| `internal-comms` | `.codex/skills` | 内部沟通、状态更新、领导汇报、FAQ、事故报告 | 面向公司内部沟通 |

## 6. 视觉、品牌、媒体与艺术

| Skill | 来源 | 使用场景 | 边界 |
| --- | --- | --- | --- |
| `imagegen` | `.codex/skills/.system` | 生成/编辑位图、照片、插画、纹理、sprite、透明背景图 | 不用于 SVG/代码原生资产更合适的场景 |
| `canvas-design` | `.codex/skills` | 海报、静态视觉设计、PNG/PDF 视觉艺术 | 不复制现有艺术家风格 |
| `algorithmic-art` | `.codex/skills` | p5.js 生成艺术、粒子、flow fields | 面向代码生成艺术 |
| `slack-gif-creator` | `.codex/skills` | Slack 优化 GIF | 不用于普通静态图 |
| `mmx-cli` | `.codex/skills` | MiniMax 文本、图片、视频、语音、音乐、搜索 | 依赖 mmx/MiniMax |
| `brand-guidelines` | `.codex/skills` | Anthropic 品牌颜色、字体和品牌风格 | 只在品牌适用时用 |
| `theme-factory` | `.codex/skills` | 给 slides/docs/HTML/report 套主题或生成主题 | 不是内容写作技能 |

## 7. LLM、API、MCP 与 Codex/OpenAI/Claude

| Skill | 来源 | 使用场景 | 边界 |
| --- | --- | --- | --- |
| `openai-docs` | `.codex/skills/.system` | OpenAI/Codex/API 官方文档、模型选择、提示迁移 | 限官方 OpenAI 来源 |
| `claude-api` | `.codex/skills` | Claude/Anthropic SDK、模型、价格、缓存、MCP、token | 任务涉及其他 provider 时跳过 |
| `mcp-builder` | `.codex/skills` | 构建 MCP server，设计 tools/resources/prompts | 不是普通 API client 技能 |

## 8. Skill、插件与能力管理

| Skill | 来源 | 使用场景 | 边界 |
| --- | --- | --- | --- |
| `find-skills` | `.agents/skills` | 用户问“有没有 skill 可以做 X”或想找可安装能力 | 发现/推荐为主 |
| `skill-installer` | `.codex/skills/.system` | 安装 Codex skills 到 `$CODEX_HOME/skills` | 不用于创建 skill |
| `skill-creator` | `.codex/skills/.system` | 创建/更新 Codex skill，写 `SKILL.md` | 基础创建流程 |
| `skill-creator` | `.agents/skills` | 创建、修改、优化、评估 skill，跑 eval/benchmark | 更偏高级优化 |
| `plugin-creator` | `.codex/skills/.system` | 创建 Codex plugin、`.codex-plugin/plugin.json`、marketplace 条目 | 不用于普通 repo 插件 |

## 9. GitHub 工作流

| Skill | 来源 | 使用场景 | 边界 |
| --- | --- | --- | --- |
| `github` | GitHub plugin cache | GitHub repo/PR/issue 总览、元数据摘要、路由到专项 workflow | 不直接替代 CI 或 review-thread 专项 |
| `gh-address-comments` | GitHub plugin cache | 处理 PR unresolved review threads、requested changes、inline comments | 需要 thread 状态时用 `gh` GraphQL |
| `gh-fix-ci` | GitHub plugin cache | GitHub Actions PR check 失败，读取日志并修复 | 必须先看 Actions 日志 |
| `yeet` | GitHub plugin cache | 确认范围、commit、push、开 draft PR | 发布本地变更时用 |

## 10. 项目特定建议

这个仓库是 Unity 项目。通常优先级如下：

1. 报错或测试失败：`troubleshoot`，再按 Unity 代码路径定位。
2. 实现或修复逻辑：`ponytail`，保持最小正确改动。
3. 多阶段系统设计或长任务：`planning-with-files`。
4. UI 相关：如果是 Unity UI 设计理念，可参考 `frontend-design`/`ui-ux-pro-max`；如果是 Web UI 才用 `webapp-testing`。
5. 文档交付：普通 Markdown 不需要 docx/pdf/pptx/xlsx；只有目标文件格式明确时才触发对应文件技能。

## 冲突处理

- `pdf` vs `pdf:pdf`：普通 PDF 操作用 `pdf`；版式、渲染、视觉 QA 重要时用 `pdf:pdf`。
- `docx` vs `documents`：快速 Word 操作用 `docx`；正式交付、需要渲染验证时用 `documents`。
- `xlsx` vs `spreadsheets`：简单表格处理用 `xlsx`；复杂公式、图表、格式和重算用 `spreadsheets`。
- `pptx` vs `presentations`：简单 deck 读写用 `pptx`；正式演示稿创建/编辑用 `presentations`。
- `skill-creator` 双版本：创建基础 Codex skill 用系统版；评估、优化、benchmark skill 用 `.agents` 版。
- `ponytail` 与领域技能：领域技能决定正确做法，`ponytail` 负责把实现保持在最小正确范围。
