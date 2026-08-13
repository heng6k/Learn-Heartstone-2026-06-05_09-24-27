# Design System Master File

> **LOGIC:** When building a specific page, first check `design-system/pages/[page-name].md`.
> If that file exists, its rules **override** this Master file.
> If not, strictly follow the rules below.

---

**Project:** Learn Heartstone
**Generated:** 2026-08-07 08:56:23
**Category:** Card & Board Game
**Design Dials:** Variance 6/10 (Balanced / Modern) | Motion 3/10 (Subtle) | Density 6/10 (Standard)

---

## Approved Project Overrides

These decisions resolve generator output against the product brief and higher-priority accessibility/performance rules. They are part of the master system, not optional page deviations.

- **Page pattern:** Feature-Rich Showcase. Static product/version information renders first; Unity is never the full-screen opening interaction and loads only after an explicit click on `/play`.
- **Signature:** “版本牌轨” — one restrained gold rail linking the current version badge, supported mechanics and the primary play action. It is structural, not decorative 3D.
- **Typography:** Chinese display uses `STKaiti, KaiTi, "Noto Serif SC", serif` sparingly; body/UI uses `"Microsoft YaHei UI", "PingFang SC", "Noto Sans SC", system-ui, sans-serif`. No remote font request.
- **Accent contrast:** `#D97706` uses `#0F172A` text (5.60:1). White on this accent is forbidden for normal text (3.19:1).
- **Motion:** native CSS transform/opacity only, 180–280ms. No GSAP dependency. Respect `prefers-reduced-motion`.
- **Surfaces:** cards use `#192134`; elevated surfaces use `#111F2C`; warm primary text may use `#F6E7C4` while body text remains `#E2E8F0`.
- **Effects:** at most one ambient radial glow in the hero and subtle 1px translucent borders. No animated blobs, haptics, cyberpunk scanlines, complex shadows or 3D effects.
- **Icons:** one consistent custom outline SVG set with 24px viewBox and `currentColor`; do not add a component library solely for six navigation/action icons.

---

## Global Rules

### Color Palette

| Role | Hex | CSS Variable |
|------|-----|--------------|
| Primary | `#15803D` | `--color-primary` |
| On Primary | `#FFFFFF` | `--color-on-primary` |
| Secondary | `#166534` | `--color-secondary` |
| Accent/CTA | `#D97706` | `--color-accent` |
| On Accent | `#0F172A` | `--color-on-accent` |
| Background | `#0F172A` | `--color-background` |
| Foreground | `#FFFFFF` | `--color-foreground` |
| Card | `#192134` | `--color-card` |
| Elevated | `#111F2C` | `--color-elevated` |
| Warm Text | `#F6E7C4` | `--color-warm` |
| Muted | `#0F1F2B` | `--color-muted` |
| Border | `rgba(255,255,255,0.08)` | `--color-border` |
| Destructive | `#DC2626` | `--color-destructive` |
| Ring | `#15803D` | `--color-ring` |

**Color Notes:** Felt green + gold on dark

### Typography

- **Display:** `STKaiti, KaiTi, "Noto Serif SC", serif`
- **Body/UI:** `"Microsoft YaHei UI", "PingFang SC", "Noto Sans SC", system-ui, sans-serif`
- **Utility/data:** `ui-monospace, "Cascadia Code", "SFMono-Regular", monospace`
- **Mood:** readable tavern ledger, warm fantasy accents, restrained competitive utility
- **Loading:** local/system fonts only; no Google Fonts or other runtime font hotlinks

### Spacing Variables

*Density: 6/10 — Standard*

| Token | Value | Usage |
|-------|-------|-------|
| `--space-xs` | `4px` / `0.25rem` | Tight gaps |
| `--space-sm` | `8px` / `0.5rem` | Icon gaps, inline spacing |
| `--space-md` | `16px` / `1rem` | Standard padding |
| `--space-lg` | `24px` / `1.5rem` | Section padding |
| `--space-xl` | `32px` / `2rem` | Large gaps |
| `--space-2xl` | `48px` / `3rem` | Section margins |
| `--space-3xl` | `64px` / `4rem` | Hero padding |

### Shadow Depths

| Level | Value | Usage |
|-------|-------|-------|
| `--shadow-sm` | `0 1px 2px rgba(0,0,0,0.05)` | Subtle lift |
| `--shadow-md` | `0 4px 6px rgba(0,0,0,0.1)` | Cards, buttons |
| `--shadow-lg` | `0 10px 15px rgba(0,0,0,0.1)` | Modals, dropdowns |
| `--shadow-xl` | `0 20px 25px rgba(0,0,0,0.15)` | Hero images, featured cards |

---

## Component Specs

### Buttons

```css
/* Primary Button */
.btn-primary {
  background: #D97706;
  color: #0F172A;
  padding: 12px 24px;
  border-radius: 8px;
  font-weight: 600;
  transition: all 200ms ease;
  cursor: pointer;
}

.btn-primary:hover {
  opacity: 0.9;
  transform: translateY(-1px);
}

/* Secondary Button */
.btn-secondary {
  background: transparent;
  color: #15803D;
  border: 2px solid #15803D;
  padding: 12px 24px;
  border-radius: 8px;
  font-weight: 600;
  transition: all 200ms ease;
  cursor: pointer;
}
```

### Cards

```css
.card {
  background: #192134;
  border-radius: 12px;
  padding: 24px;
  box-shadow: var(--shadow-md);
  transition: all 200ms ease;
}

.card[href]:hover,
.card:has(a):hover {
  box-shadow: var(--shadow-lg);
  transform: translateY(-2px);
}
```

### Inputs

```css
.input {
  color: #E2E8F0;
  background: #111F2C;
  padding: 12px 16px;
  border: 1px solid rgba(255,255,255,0.12);
  border-radius: 8px;
  font-size: 16px;
  transition: border-color 200ms ease;
}

.input:focus {
  border-color: #15803D;
  outline: none;
  box-shadow: 0 0 0 3px #15803D20;
}
```

### Modals

```css
.modal-overlay {
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
}

.modal {
  color: #E2E8F0;
  background: #111F2C;
  border-radius: 16px;
  padding: 32px;
  box-shadow: var(--shadow-xl);
  max-width: 500px;
  width: 90%;
}
```

---

## Style Guidelines

**Style:** Modern Dark (Cinema Mobile)

**Keywords:** dark mode, cinematic, ambient light, glassmorphism, deep black, indigo, glow, blur, atmospheric, reanimated, haptic, premium, layered, frosted glass, linear gradient

**Best For:** Developer tools, pro productivity apps, fintech/trading dashboards, media/streaming platforms, AI tool interfaces, high-end gaming companion apps

**Key Effects:** one static ambient hero glow; subtle translucent borders; `cubic-bezier(0.16,1,0.3,1)` for short transform/opacity transitions; avoid pure black and expensive continuous blur animation

### Page Pattern

**Pattern Name:** Feature-Rich Showcase

- **Conversion Strategy:** explain current version and support boundary before offering play/download actions
- **CTA Placement:** primary play action in hero and after the current-version block; download remains secondary while unavailable
- **Section Order:** 1. Hero thesis + CTA, 2. Current version rail, 3. Two supported mechanisms, 4. product capabilities, 5. final CTA

---

## Motion

**Page/section reveal** (Subtle) — Trigger: initial render | Duration: 240ms | Easing: `cubic-bezier(0.16,1,0.3,1)`

```css
@keyframes reveal {
  from { opacity: 0; transform: translateY(8px); }
  to { opacity: 1; transform: translateY(0); }
}
```

- ✅ Keep the offset at 8px and animate only transform/opacity
- ✅ Content is visible by default; animation is progressive enhancement
- ❌ Do not add GSAP for this motion tier

---

## Anti-Patterns (Do NOT Use)

- ❌ Complex shadows
- ❌ 3D effects

### Additional Forbidden Patterns

- ❌ **Emojis as icons** — Use SVG icons (Heroicons, Lucide, Simple Icons)
- ❌ **Missing cursor:pointer** — All clickable elements must have cursor:pointer
- ❌ **Layout-shifting hovers** — Avoid scale transforms that shift layout
- ❌ **Low contrast text** — Maintain 4.5:1 minimum contrast ratio
- ❌ **Instant state changes** — Always use transitions (150-300ms)
- ❌ **Invisible focus states** — Focus states must be visible for a11y

---

## Pre-Delivery Checklist

Before delivering any UI code, verify:

- [ ] No emojis used as icons (use SVG instead)
- [ ] All icons from consistent icon set (Heroicons/Lucide)
- [ ] `cursor-pointer` on all clickable elements
- [ ] Hover states with smooth transitions (150-300ms)
- [ ] Light mode: text contrast 4.5:1 minimum
- [ ] Focus states visible for keyboard navigation
- [ ] `prefers-reduced-motion` respected
- [ ] Responsive: 375px, 768px, 1024px, 1440px
- [ ] No content hidden behind fixed navbars
- [ ] No horizontal scroll on mobile
