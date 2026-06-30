# Project Agent Routing

For the full local skill index, see `Docs/LocalSkillClassification.zh-CN.md`.

Choose skills by task shape:

- Use `planning-with-files` for multi-step work, research, or tasks likely to need 5+ tool calls.
- Use `troubleshoot` when the user reports an error, failing test, bug, or unexpected behavior.
- Use `brainstorm` when requirements are vague and need discovery.
- Use `Confidence Check` before substantial implementation when architecture, duplicates, official docs, or root cause need verification.
- Use file-specific skills when the target artifact is explicit: `docx`/`documents`, `pdf`/`pdf:pdf`, `pptx`/`presentations`, `xlsx`/`spreadsheets`.
- Use UI/frontend skills for UI work: `frontend-design`, `ui-ux-pro-max`, `web-design-guidelines`, `webapp-testing`, and the Vercel React skills when React/Next-specific.
- Use Vercel skills only for Vercel deployment, token setup, or cost/performance optimization.
- Use GitHub workflow skills for repository, PR, issue, review-comment, CI, commit, push, and PR publishing work.
- Use `openai-docs`, `claude-api`, or `mcp-builder` for OpenAI/Codex, Anthropic/Claude, or MCP-specific work.
- Use skill/plugin management skills only when the user asks to find, install, create, or optimize skills/plugins.

Ponytail is installed locally from `Tools/ponytail` through the `ponytail-local` Codex marketplace.

Use the Ponytail skills by task shape:

- Use `@ponytail` for coding work: implementation, refactoring, bug fixing, dependency decisions, and code design. Apply the ladder: skip speculative work, reuse existing code, prefer stdlib/native platform features, use installed dependencies before adding new ones, then write the smallest correct change.
- Use `@ponytail-review` only for current-diff over-engineering review. It finds what to delete or shrink; it is not a correctness, security, or performance review.
- Use `@ponytail-audit` for whole-repo over-engineering audits. It reports delete/simplify opportunities and does not apply fixes.
- Use `@ponytail-debt` to collect `ponytail:` shortcut comments into a debt ledger.
- Use `@ponytail-gain` only to show Ponytail benchmark impact. Do not invent per-repo savings numbers.
- Use `@ponytail-help` when the user asks how Ponytail works or what commands are available.

Do not use Ponytail skills for non-coding requests such as prose writing, translation, general summaries, or unrelated research.
