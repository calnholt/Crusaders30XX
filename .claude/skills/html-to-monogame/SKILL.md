---
name: html-to-monogame
description: Convert any HTML/CSS mockup into a MonoGame implementation plan for this project's ECS renderers (cards, modals, HUD, panels). Use when the user provides an HTML mockup path, asks for html-to-monogame, or wants a pixel plan before implementing UI in Crusaders30XX.
---

# HTML-to-MonoGame Conversion

Convert an HTML/CSS mockup into a **plan document only** (not C# implementation) that can be built pixel-accurately in Crusaders30XX. The user provides an HTML file path as the primary argument.

**Coordinate system:** `Game1.VirtualWidth` × `Game1.VirtualHeight` (default **1920×1080**). All pixel tables use screen-space top-left origin unless the UI is card-local (see Appendix A).

---

## Workflow Overview

1. **Analyze HTML/CSS** — every visual property
2. **Classify UI archetype** — pick reference code and conventions
3. **Read reference implementation(s)** — archetype-specific
4. **Extract color palette** — exact `Color` values
5. **Build pixel position reference** — absolute layout at virtual resolution
6. **Map CSS → MonoGame primitives** — universal table + archetype notes
7. **Write plan document** — structured markdown for implementer

---

## Step 1: Analyze the HTML File

Read the HTML file. Account for **every** CSS property:

- Colors (hex, rgb, rgba, named)
- Dimensions (width, height, padding, margin, gap)
- Borders (width, color, radius — uniform and per-corner)
- Fonts (family, size, weight, letter-spacing, line-height, text-shadow)
- Layout (flexbox/grid → compute absolute positions)
- Opacity, z-index, overflow
- `box-shadow` (outer, inset)
- Pseudo-elements (`::before`, `::after`)
- Variant classes (e.g. `.card.red`, `.design-a`)

### HTML mockup checklist (preview only)

Mockups under `mockups/` are for design exploration; in-game uses virtual resolution directly.

| Check | Why |
|-------|-----|
| CSS vars use **units** (`1920px` not `1920`) | Unitless values break layout (0×0 viewport) |
| Mockup targets **1920×1080** (or document scale factor) | Match `Game1.VirtualWidth/Height` |
| Ignore page chrome (`.page-header`, tabs, JS scaler) | Not shipped in-game |
| Note **all states** shown (tabs, variants) | Plan may need layout modes |

---

## Step 2: Classify UI Archetype

Pick **one primary** archetype before choosing reference files. Mixed UIs (e.g. modal + card inside) use **Overlay** for chrome and **Card** for the embedded card via events.

| Archetype | When | Primary references |
|-----------|------|-------------------|
| **A — Card-local** | Element rotates/scales with a card; coords relative to card top-left | `ECS/Scenes/CardDisplayBase.cs`, `CardDisplaySystemV2.cs`, `card-display-v2-plan.md` |
| **B — Screen overlay** | Centered modal, full-screen dim, HUD chrome, game-over text | `QuestRewardModalDisplaySystem.cs`, `CardListModalSystem.cs`, `GameOverOverlayDisplaySystem.cs` |
| **C — Panel / scroll list** | Modal with grid/list, optional scroll clip | `CardListModalSystem.cs` + **B** for shell |
| **D — Scene HUD** | Persistent battle/world UI (HP, AP, piles) | Closest existing `*DisplaySystem` in relevant scene folder |

### Routing rules

- **Do not** default to `CardDisplayBase` for modals or fullscreen UI.
- **Do not** use `MouseState` / `GamePad` — input via `UIElement` + `CursorEvents` (project `CLAUDE.md`).
- If mockup embeds a **game card**, plan card via `CardRenderScaledEvent` (or existing card draw path), not by redrawing card HTML in the overlay system.
- Read `ECS/Singletons/FontSingleton.cs` and `Content/Fonts/*.spritefont` for **native font sizes**.

---

## Step 3: Read Reference Implementation

Always read (any archetype):

| File | Purpose |
|------|---------|
| `ECS/Rendering/RoundedRectTextureFactory.cs` | Rounded rects, per-corner radius |
| `ECS/Rendering/PrimitiveTextureFactory.cs` | Circles, polygons, novel shapes |
| `ECS/Singletons/FontSingleton.cs` | `TitleFont`, `ContentFont`, `ChakraPetchFont` |

Plus **archetype files** from Step 2 table.

---

## Step 4: Extract Color Palette

Convert every CSS color to MonoGame `Color`:

| CSS | MonoGame |
|-----|----------|
| `#rrggbb` | `new Color(r, g, b)` |
| `#rgb` | expand to 6-digit first |
| `rgb(r,g,b)` | `new Color(r, g, b)` |
| `rgba(r,g,b,a)` | `new Color(r, g, b) * (a/255f)` or `* aFloat` for premultiplied style |
| `rgba` with 0–1 alpha | `new Color(r, g, b) * a` |

Group by role (chrome, text, accent, variant). Example:

```csharp
private static readonly Color ModalFill = new Color(8, 8, 8) * 0.92f; // rgba(8,8,8,0.92)
```

Do **not** approximate hex values.

---

## Step 5: Build Pixel Position Reference

Produce a table for **each visible element** at virtual resolution:

| Element | X | Y | W | H | Notes |
|---------|---|---|---|---|-------|
| … | … | … | … | … | CSS source |

### Layout rules (all archetypes)

- **Border-box:** `contentRect` = outer rect inset by `borderWidth` on all sides; lay out columns/footer inside `content`, not the outer border.
- **Centered modal:** `x = (vw - w) / 2`, `y = (vh - h) / 2`.
- **Grid/flex:** resolve gaps and padding into absolute rects (columns, footer span, inner padding).
- **Footer / full-width rows:** `grid-column: 1 / -1` → width = content width, y = below body.
- Document **layout modes** if HTML shows multiple tabs/states (e.g. gold-only vs split).

### Font scale (screen overlays & HUD)

SpriteFont `MeasureString` returns **unscaled** pixel sizes. For CSS `font-size: Npx` with native size `S` from `.spritefont`:

```text
scale ≈ N / S
```

Example: Chakra Petch `Size=128`, CSS `14px` → `scale ≈ 0.109` (tune in debug).

Letter-spacing in CSS has **no** SpriteFont equivalent — note as acceptable diff.

---

## Step 6: Map CSS → MonoGame Primitives

### Universal mapping

| CSS | MonoGame (this project) |
|-----|-------------------------|
| Solid fill | `_pixel` + `SpriteBatch.Draw(rect, color)` |
| Border | Four `_pixel` strips or `DrawBorder(rect, color, thickness)` |
| `border-radius` (uniform) | `RoundedRectTextureFactory.CreateRoundedRect` |
| Per-corner radius | `CreateRoundedRectPerCorner` |
| Bordered fill | Outer rounded rect + inner inset fill |
| `linear-gradient` (horizontal) | Multi-strip `_pixel` ramp (alpha or color lerp) |
| `text-shadow` / glow | Multiple `DrawString` passes, offset + lower alpha |
| `box-shadow` (outer) | Same-size or inset rect, offset **down**; height = `h - offsetY` so shadow does not extend past bottom border |
| `box-shadow` (inset) | 1px inset `DrawBorder` on content rect |
| Semi-transparent overlay | `Color.Black * alpha` fullscreen rect |
| Image | `Content.Load<Texture2D>` / existing art paths |
| `:hover` | `UIElement.IsHovered` — instant color swap (optional transition skip) |

### Novel shapes

Plan new cached methods in `PrimitiveTextureFactory.cs` (distance AA, supersampling, cache keyed by device + dimensions). Document signature, algorithm, cache key in the plan.

### Event-driven draws

If the plan uses `CardRenderScaledEvent` (or similar):

- Subscribers run **synchronously** when `Publish` is called during `Draw()`.
- Document **order**: chrome/labels vs card publish (labels above card → publish card first or anchor card top below label; drawing label after publish places it on top of card art).
- Card position is usually **center**; top of visual bounds ≈ `center.Y - (cardH * scale / 2 + offsetYExtra * scale)` — use `CardVisualSettings` for dimensions.

---

## Step 7: Output the Plan Document

Write markdown (e.g. `docs/plans/<feature>-ui-plan.md` or repo root `*-plan.md`). Use `card-display-v2-plan.md` or quest-reward-style plans as structural examples.

### Required sections

1. **Context** — What the mockup is; target system/scene; archetype **A/B/C/D**
2. **Files to create / modify** — Tables
3. **Implementation steps** — Numbered; target file; snippets (structure only, not full impl)
4. **`DebugEditable` properties** — Every magic number; `Step = 0.01f` for float scales; group by region (Modal, Left Column, Typography, Card, …)
5. **Color palette tables** — CSS ↔ MonoGame per group
6. **Pixel position reference** — Full table at 1920×1080 (or note if card-local)
7. **Draw order pipeline** — Back → front, numbered
8. **Helper method signatures** — `ComputeLayout`, `DrawBorder`, `DrawLeftColumn`, etc.
9. **Layout modes** — Conditional rects (if applicable)
10. **Verification checklist** — In-game scenarios + debug tab tuning
11. **Fidelity notes** — Known acceptable diffs (letter-spacing, gradient strips, font hinting)

### Archetype-specific conventions

| Archetype | System base | Layout | State |
|-----------|-------------|--------|-------|
| **A — Card** | `CardDisplayBase` | Local offsets; multiply by `visualScale` (`vs`); rotation via `V2*` helpers | Optional `CardDisplayToggle` for A/B renderer |
| **B — Overlay** | `Core.System` | `ComputeLayout(vw,vh)` → struct of `Rectangle`s; `cursorY` for vertical stacks | Component on overlay entity or system fields |
| **C — Panel** | `Core.System` | Shell per **B**; content area + scissor if scrolling | Modal component |
| **D — HUD** | Existing display system | Match sibling patterns | Scene components |

**Universal project rules:**

- `[DebugTab("…")]` on systems with `Draw()`
- `Draw()` does not mutate game state — layout/position sync in `UpdateEntity` when needed (e.g. button `UIElement.Bounds`)
- Cache textures via existing factories; `DeleteCachesEvent` where applicable
- No features beyond the HTML unless user asked

---

# Appendix A — Card-local UI

Use when the mockup is a **card face** or element drawn in card space.

### Helpers (from `CardDisplaySystemV2`)

```csharp
private void V2Rect(Vector2 center, float rot, Vector2 pos, float w, float h, Color c, float vs)
    => DrawRectangleRotatedLocalScaled(center, rot, pos, w, h, c, vs, CW, CH);
// V2Tex, V2Text, V2TextWrapped — same pattern
```

### Conventions

- Coordinates: **local offsets from card top-left**; helpers convert via `cardCenter` + rotation.
- All dimensions × `visualScale` (`vs`).
- `CW` / `CH` from `CardVisualSettings` or plan properties.
- Toggle guard on event handlers if migrating from V1: `if (CardDisplayToggle.UseV2) return;`

### Plan extras

- Per-corner radius table (notch, chips, slabs)
- Variant palettes (White / Red / Black)
- Draw order inside card (bg → gutter → chips → text → art)

---

# Appendix B — Screen overlay UI

Use for **modals**, reward screens, dim + panel + footer, centered dialogs.

### Reference pattern (`QuestRewardModalDisplaySystem`, `CardListModalSystem`)

```csharp
// Fields
private readonly Texture2D _pixel;
private readonly SpriteFont _titleFont = FontSingleton.TitleFont;
private readonly SpriteFont _bodyFont = FontSingleton.ChakraPetchFont;

// Draw pipeline (typical)
// 1. Fullscreen dim
// 2. Drop shadow (optional, same W/H as modal, Y offset, height = modalH - offset)
// 3. Modal fill + column tints
// 4. Footer
// 5. Inset highlight + outer border (border drawn last)
// 6. Text / labels
// 7. CardRenderScaledEvent if needed
// 8. Proceed button (hover via UIElement.IsHovered)
```

### Layout struct pattern

```csharp
private struct MyLayout {
    public Rectangle Modal, Content, Footer, ProceedButton;
    // columns, Inner rects, Vector2 CardCenter, etc.
}
private MyLayout ComputeLayout(int vw, int vh, /* mode flags */) { … }
```

- `content` = modal inset by `BorderThickness`
- `body` = content minus footer height
- **Inner** rects apply CSS padding (e.g. `padding: 40px 28px` → `LeftInner`)

### Card beside chrome

- Right column: label at `rightColumn.Y + paddingTop`
- Card top anchor: `labelBottom + gap`; `cardCenter.Y = cardTop + scaledHalfHeight + offsetY`
- Read `CardVisualSettings.CardHeight`, `CardOffsetYExtra`, plan `CardPreviewScale`

### Data / events

- Prefer structured fields on overlay event/state (e.g. `RewardGold` int) over parsing `Message` with `\n`
- `UIElement` full-screen blocker + proceed button entity with `HotKey` if needed

### Common pitfalls (document in plan)

| Issue | Fix |
|-------|-----|
| Background below border | Shadow height `modalH - offsetY`; draw border last |
| Text flush to edge | Center within **Inner** rect, not full column |
| Label overlaps card | Top-anchor card; sufficient `StageLabelGap`; label draw order |
| REWARD invisible | Scale = cssPx/128; draw after card only if intentional overlay |

---

# Appendix C — Panel / list with cards

Combine **Appendix B** shell with:

- `CardListModalSystem` — dim, panel, scissor, `CardRenderScaledEvent` per cell
- Grid: cell center, `GridGap`, `ScrollOffset`
- Cards get `ZOrder` bump while modal open

---

## What NOT to Do

- Do **not** write C# implementation in the skill run — **plan only**
- Do **not** skip CSS properties
- Do **not** assume all UI is card-local / `CardDisplayBase`
- Do **not** use `MouseState` or `GamePad`
- Do **not** add features not shown in HTML (unless user requested)
- Do **not** treat browser-only mockup JS as in-game behavior

---

## Quick reference — project fonts

| Mockup font | MonoGame |
|-------------|----------|
| New Rocker / gothic titles | `FontSingleton.TitleFont` |
| Chakra Petch / UI labels | `FontSingleton.ChakraPetchFont` |
| Legacy duplicate | `FontSingleton.ContentFont` (often same as Title) |

Read `Content/Fonts/*.spritefont` → `<Size>` for scale math.
