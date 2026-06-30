# Ponytail Skill Routing

Source: `Tools/ponytail`, version `4.8.4`, installed through the local Codex marketplace `ponytail-local`.

This document classifies the downloaded Ponytail skills and records when each one should be used.

## Quick Matrix

| Situation | Skill | Use it for | Do not use it for |
| --- | --- | --- | --- |
| Coding task | `@ponytail` | Implementing, fixing, refactoring, designing code, choosing dependencies | Non-coding work, prose, translation, general summaries |
| Current diff feels too large | `@ponytail-review` | Finding over-engineering in the current diff | Correctness review, security review, performance review, applying fixes |
| Whole repo may have bloat | `@ponytail-audit` | Repo-wide delete/simplify opportunities | Applying fixes, judging correctness/security/performance |
| Deferred shortcuts need tracking | `@ponytail-debt` | Listing `ponytail:` comments and upgrade triggers | Creating new debt, changing code |
| User asks about saved effort | `@ponytail-gain` | Showing published benchmark impact | Claiming live per-repo savings |
| User asks how to use Ponytail | `@ponytail-help` | Showing commands, modes, and activation notes | Changing code or mode by itself |

## Categories

### 1. Build-Time Minimalism

Skill: `@ponytail`

Use for any coding request unless the user explicitly asks for normal mode or a non-Ponytail approach.

Trigger examples:

- "implement this"
- "fix this bug"
- "refactor this"
- "simplify this"
- "use the simplest solution"
- "avoid over-engineering"
- "choose a dependency"

Method:

1. Ask whether the requested thing needs to exist at all.
2. Reuse a helper, pattern, type, or API already in this codebase.
3. Prefer the language standard library.
4. Prefer native platform features.
5. Prefer already-installed dependencies over new dependencies.
6. Use a one-liner if it is correct and readable.
7. Only then write the minimum custom code.

Safety boundary:

Never remove trust-boundary validation, data-loss prevention, security basics, accessibility basics, explicitly requested behavior, or the small runnable check needed for non-trivial logic.

### 2. Diff-Level Complexity Review

Skill: `@ponytail-review`

Use when the user asks whether a current change is over-engineered or what can be deleted from a diff.

Expected output shape:

```text
<file>:L<line>: <tag>: <what to cut>. <replacement>.
net: -<N> lines possible.
```

Tags:

- `delete:` remove dead or speculative code.
- `stdlib:` replace hand-rolled logic with standard library.
- `native:` replace dependency/custom code with platform feature.
- `yagni:` remove abstraction with one implementation or one caller.
- `shrink:` keep behavior with fewer lines.

Boundary:

This is not a bug review. If correctness, security, or performance risk is present, run a normal review separately.

### 3. Repo-Wide Complexity Audit

Skill: `@ponytail-audit`

Use when the user asks to audit the whole repo for bloat, excessive abstraction, unnecessary dependencies, or code that can be deleted.

Method:

- Scan broadly.
- Rank the biggest deletions or simplifications first.
- Report only; do not apply changes unless the user separately asks for implementation.

Expected ending:

```text
net: -<N> lines, -<M> deps possible.
```

If nothing useful is found:

```text
Lean already. Ship.
```

### 4. Deferred Shortcut Ledger

Skill: `@ponytail-debt`

Use when the user asks what Ponytail deferred, what shortcuts exist, or to build a ledger from `ponytail:` comments.

Scan pattern:

```text
(#|//) ?ponytail:
```

Expected row:

```text
<file>:<line>, <what was simplified>. ceiling: <limit>. upgrade: <trigger>.
```

Flag any marker with no explicit upgrade path as `no-trigger`.

Boundary:

Reads and reports only. Persist to a file only if the user asks.

### 5. Benchmark Impact Card

Skill: `@ponytail-gain`

Use when the user asks what Ponytail saves or wants the Ponytail impact scoreboard.

Boundary:

The numbers are published benchmark medians, not measurements from this repo. Do not say "this repo saved X lines/tokens/cost" unless there is a real measured baseline.

### 6. Help And Commands

Skill: `@ponytail-help`

Use when the user asks how to use Ponytail, what commands exist, how modes work, or how to disable it.

Mode summary:

- `lite`: build what was asked, mention the lazier alternative.
- `full`: default ladder-enforced mode.
- `ultra`: deletion-first, strongest YAGNI stance.
- `off` / "normal mode": stop Ponytail behavior.

## Conflict Rules

When multiple skills seem relevant:

1. If the user asks to build or fix code, start with `@ponytail`.
2. If the user asks to review a diff for excess complexity, use `@ponytail-review`.
3. If the user asks to inspect the entire repo for excess complexity, use `@ponytail-audit`.
4. If the user asks about deferred shortcuts, use `@ponytail-debt`.
5. If the user asks about commands or setup, use `@ponytail-help`.
6. If the user asks about measured benefit, use `@ponytail-gain`.

Ponytail does not replace domain-specific skills or normal engineering checks. For security, correctness, performance, UI/UX, documents, PDFs, spreadsheets, GitHub PR work, or deployment work, use the appropriate specialist workflow first and apply Ponytail only to keep the resulting implementation minimal.
