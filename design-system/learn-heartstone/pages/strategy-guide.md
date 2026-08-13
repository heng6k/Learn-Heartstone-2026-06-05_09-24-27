# Strategy Guide Pages — Page Override

This page override inherits `../MASTER.md`. It applies to the strategy-guide selection screen, local four-step editor, share-card preview, and the Holopix component sheet.

## Product job

- Selection screen: let a player understand one lineup and start the correct difficulty in under 20 seconds.
- Editor screen: let an author finish `基本信息 → 阵容 → 难度 → 校验冻结` without losing context or hiding validation failures.
- The first shipped content is the three frozen 36.2 Showcase guides; components remain data-driven and may not branch on those guide IDs.

## Visual direction

- **Concept:** Tavern tactics table — compact cards and slots arranged on a readable felt-and-slate work surface.
- **Signature:** a restrained warm-gold `strategy rail` connects the selected guide, its seven final slots, and the current editing step. It encodes progression and board order rather than acting as decoration.
- **Surface hierarchy:** background `#0F172A`; workspace `#111F2C`; card `#192134`; raised/selected surface `#203246`.
- **Semantic accents:** CTA gold `#D97706` with ink text; selection/focus cyan `#38A9CF`; valid felt green `#15803D`; destructive red `#DC2626`.
- Keep all text, numbers, and card metadata native. Holopix assets may provide only separable frames, corner ornaments, icons, textures, and 9-slice surfaces.

## Typography

- Display: `STKaiti, KaiTi, "Noto Serif SC", serif`, only for screen and guide titles.
- UI/body: `"Microsoft YaHei UI", "PingFang SC", "Noto Sans SC", system-ui, sans-serif`.
- Utility/revision: `ui-monospace, "Cascadia Code", monospace`.
- 1080p roles: screen title 30–34px; section title 20–24px; body 16px; secondary 14px; utility 12px only for non-essential IDs.

## Desktop landscape layout

### Guide selection

- 72px title-safe header with title left and compact actions right.
- Main body is `320px guide rail + flexible detail workspace`; no full-width empty hero.
- Detail workspace shows identity, seven-slot final lineup, required mechanics, then difficulty/primary CTA.
- Sticky action bar is 76–84px and must reserve matching content inset.

### Four-step editor

- 72px title-safe header.
- 64px step rail; each step is a compact segment with number, label, and state.
- Main body is `minmax(220px, 280px) context rail + flexible form workspace`.
- Sticky action bar is 76–84px. `下一步/校验并冻结` is the only primary CTA; save is secondary.

## Responsive and Unity parity

- Reference: 1920×1080 with `CanvasScaler.ScaleWithScreenSize`, match 0.5 for standard landscape.
- Validate at 1280×720, 1920×1080, 2560×1440, and 2560×1080.
- Below 1100px, context rail becomes a compact summary row; below 760px the prototype stacks but Unity gameplay remains landscape-first.
- Keep primary controls inside 93% action-safe and major content inside 90% title-safe regions.
- Minimum control target 48px; gaps at least 8px; body text never below 14px in Unity.

## Interaction states

- Every selectable card has normal, hover, focus-visible, selected, disabled, and invalid states.
- Current step uses cyan rail + `当前`; completed steps use green check + text; invalid steps use red icon + error count.
- Validation errors appear next to the failing group and in a concise top summary with jump actions.
- Draft autosave is status text (`已保存`, `保存中…`, `保存失败`) rather than a competing primary CTA.
- Motion uses opacity/transform only, 160–220ms, and respects reduced motion.

## Holopix component contract

- Produce separate assets for: large panel 9-slice, compact card 9-slice, selected rail/corner, gold/normal card-slot frame, difficulty emblem, trinket/gift badge frame, primary/secondary button skins, and decorative divider.
- Export at 2× reference scale with transparent padding documented; do not bake Chinese copy, icons, card art, rarity, stats, or glow into a full-screen bitmap.
- Preserve neutral center areas so Unity 9-slice scaling does not distort ornaments.

## Prohibited

- Oversized empty header/preview regions.
- Four full-height step cards.
- Full-width dual primary buttons.
- Fixed footer covering scroll content.
- Nested vertical scrolling in the same screen.
- Text or controls embedded into Holopix raster output.
- Guide-ID-specific layout code.

