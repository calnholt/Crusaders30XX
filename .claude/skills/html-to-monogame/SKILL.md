---
name: html-to-monogame
description: Convert an HTML/CSS mockup into a MonoGame implementation plan with pixel-perfect rendering details
---

# HTML-to-MonoGame Conversion

You are converting an HTML/CSS visual mockup into a detailed MonoGame implementation plan. The user will provide a path to an HTML file as an argument. Your job is to analyze every visual detail and produce a comprehensive plan document that can be implemented pixel-perfectly in this project's ECS rendering system.

## Step 1: Read and Analyze the HTML File

Read the HTML file provided as the argument. Analyze **every** CSS property:
- Colors (hex, rgb, rgba, named)
- Dimensions (width, height, padding, margin)
- Borders (width, color, radius — uniform and per-corner)
- Fonts (family, size, weight, letter-spacing, line-height)
- Layout (flexbox/grid → compute absolute positions, gaps, alignment)
- Opacity, z-index, overflow, box-shadow
- Pseudo-elements (::before, ::after)
- Media queries or variant classes (e.g., `.card.white`, `.card.red`)

## Step 2: Read Reference Implementation

Before writing the plan, read these files to understand the existing rendering infrastructure:

1. **Base class**: [ECS/Scenes/CardDisplayBase.cs](../../../ECS/Scenes/CardDisplayBase.cs) — Abstract base with shared drawing helpers
2. **V2 implementation**: [ECS/Scenes/CardDisplaySystemV2.cs](../../../ECS/Scenes/CardDisplaySystemV2.cs) — Reference implementation to match style
3. **Rounded rects**: [ECS/Rendering/RoundedRectTextureFactory.cs](../../../ECS/Rendering/RoundedRectTextureFactory.cs) — Uniform and per-corner rounded rect textures
4. **Primitives**: [ECS/Rendering/PrimitiveTextureFactory.cs](../../../ECS/Rendering/PrimitiveTextureFactory.cs) — Circles, trapezoids, chevrons, etc.
5. **Fonts**: [ECS/Singletons/FontSingleton.cs](../../../ECS/Singletons/FontSingleton.cs) — Available fonts (TitleFont/NewRocker, ContentFont, ChakraPetchFont)
6. **Existing plan**: [card-display-v2-plan.md](../../../card-display-v2-plan.md) — Structure template to follow

## Step 3: Extract Color Palette

Convert every CSS color to a MonoGame `Color` value:

- `#rrggbb` → `new Color(r, g, b)`
- `#rgb` → expand to 6-digit hex first
- `rgb(r, g, b)` → `new Color(r, g, b)`
- `rgba(r, g, b, a)` → `new Color(r, g, b) * a` (premultiplied alpha)
- Named colors → look up hex equivalent

Group colors by **variant** (e.g., White/Red/Black card types, light/dark theme). Store as:
```csharp
private static readonly Dictionary<VariantEnum, Color> ElementColors = new()
{
    { VariantEnum.A, new Color(r, g, b) },  // #hexvalue
    { VariantEnum.B, new Color(r, g, b) },  // #hexvalue
};
```

Include a static helper:
```csharp
private static Color GetPaletteColor(Dictionary<VariantEnum, Color> palette, VariantEnum variant, Color fallback)
{
    return palette.TryGetValue(variant, out var c) ? c : fallback;
}
```

## Step 4: Build Pixel Position Reference

Convert the CSS layout to absolute pixel coordinates from the **top-left origin** of the root element. For each visual element, compute:

| Element | X | Y | Width | Height | Notes |
|---------|---|---|-------|--------|-------|
| ... | ... | ... | ... | ... | ... |

Account for:
- Padding/margin collapsing
- Flexbox alignment (justify-content, align-items, gap)
- Absolute/relative positioning offsets
- Border-box vs content-box sizing

## Step 5: Map to MonoGame Primitives

Use the established shorthand pattern from `CardDisplaySystemV2`:

```csharp
// Shorthand wrappers that all drawing code uses
private void V2Rect(Vector2 center, float rot, Vector2 pos, float w, float h, Color c, float vs)
    => DrawRectangleRotatedLocalScaled(center, rot, pos, w, h, c, vs, CW, CH);

private void V2Tex(Vector2 center, float rot, Vector2 pos, Texture2D tex, Vector2 size, Color c, float vs)
    => DrawTextureRotatedLocalScaled(center, rot, pos, tex, size, c, vs, CW, CH);

private void V2Text(Vector2 center, float rot, Vector2 pos, string text, Color c, float scale, float vs, SpriteFont font = null)
    => DrawCardTextRotatedSingleScaled(center, rot, pos, text, c, scale, vs, CW, CH, font);

private void V2TextWrapped(Vector2 center, float rot, Vector2 pos, string text, Color c, float scale, float vs, SpriteFont font, float maxW)
    => DrawCardTextWrappedRotatedScaled(center, rot, pos, text, c, scale, vs, font, maxW, CW, CH);
```

### Primitive Mapping Rules

| CSS Concept | MonoGame Approach |
|------------|-------------------|
| `border-radius` (uniform) | `GetRoundedRectTexture(w, h, radius)` → `V2Tex` |
| `border-radius` (per-corner) | `GetPerCornerRoundedRectTexture(w, h, rTL, rTR, rBR, rBL)` → `V2Tex` |
| Bordered element | Double-texture: outer rect in border color, inner rect inset by border-width in fill color |
| `border-radius` + `border` | Two per-corner rounded rects (outer border size, inner fill size) |
| Solid rectangle | `V2Rect` with pixel texture |
| Circle / `border-radius: 50%` | `PrimitiveTextureFactory.GetAntiAliasedCircle()` or `DrawCirclePipRotatedScaled()` |
| Semi-transparent overlay | `Color * alphaFloat` (e.g., `Color.Black * 0.05f`) |
| Text (single line) | `V2Text` with appropriate font and scale |
| Text (wrapped) | `V2TextWrapped` with maxWidth and font |
| Image/sprite | `GetOrLoadTexture(assetName)` → `V2Tex` |
| `box-shadow` | Additional rounded rect drawn behind, slightly larger, with alpha color |
| Novel shapes (stars, badges, arrows, rings, etc.) | **Create a new factory method** in `PrimitiveTextureFactory.cs` (see below) |

### Creating New Primitive Textures

When the HTML mockup contains shapes that don't map to existing primitives, **plan a new cached factory method** in `ECS/Rendering/PrimitiveTextureFactory.cs`. Follow the established patterns:

1. **Cache dictionary** — Add a static cache keyed by `(int deviceId, ...params)`:
   ```csharp
   private static readonly Dictionary<(int deviceId, int width, int height, ...), Texture2D> _newShapeCache = new();
   ```

2. **Public static method** — `GetAntialiased<ShapeName>(GraphicsDevice device, ...)` returning `Texture2D`
   - Check cache first, return early if hit
   - Create white-filled mask texture (alpha-only, `Color.FromNonPremultiplied(255, 255, 255, alphaByte)`) so callers can tint with any color
   - Use subpixel antialiasing: distance-based alpha falloff at edges (inner radius `r - 0.5f` to outer `r + 0.5f`) or 2x supersampling for complex polygons
   - Cache the result before returning

3. **Techniques available** (match what existing methods use):
   - **Distance-based AA**: For circles, rounded shapes — compute distance to edge, linear falloff over 1px (`GetAntiAliasedCircle`)
   - **Supersampling**: For polygons, trapezoids — render at 2x resolution with edge AA (`GetAntialiasedTrapezoid`)
   - **Barycentric coordinates**: For triangles — test point-in-triangle per pixel (`GetEquilateralTriangle`)
   - **Signed distance to line segments**: For stroke/outline shapes — compute perpendicular distance to each edge
   - **Composite**: For complex shapes — combine multiple pie slices or primitives (`GetAnyCostPipTexture`)

4. **In the plan document**, include:
   - The full method signature
   - Which AA technique to use and why
   - Cache key structure
   - A brief description of the rasterization algorithm

### Scale Propagation

All dimensions must be multiplied by `visualScale` (`vs`):
```csharp
float vs = visualScale;
float bgW = CardWidth * vs;
float bgH = CardHeight * vs;
// Every position and dimension uses vs
```

### Rotation Handling

All coordinates are **local offsets from top-left** of the element's bounding box. The `V2*` helpers convert to world-space with rotation around `cardCenter`.

## Step 6: Output the Plan Document

Write a markdown plan document with these sections (matching the structure of `card-display-v2-plan.md`):

### Required Sections

1. **Context** — What the HTML mockup represents, what it will become in-game

2. **Files to Create / Files to Modify** — Tables listing every file touched

3. **Implementation Steps** — Numbered steps, each with:
   - Target file
   - What to add/change
   - Code snippets showing structure (not full implementation)

4. **DebugEditable Properties** — List every magic number as a `[DebugEditable]` property:
   ```csharp
   [DebugEditable(DisplayName = "Card Width", Min = 100, Max = 400, Step = 1)]
   public float CardWidth { get; set; } = 240;
   ```
   Group by logical section (Frame, Stripe, Gutter, Content, etc.)

5. **Color Palette Tables** — One table per element group:
   | Variant | CSS | MonoGame |
   |---------|-----|----------|
   | W | `#dcd7ce` | `new Color(220, 215, 206)` |

6. **Pixel Position Reference** — Complete table of all elements with X, Y, Width, Height

7. **Draw Order Pipeline** — Numbered list of layers drawn back-to-front:
   1. Background (AA rounded rect)
   2. Overlay/gutter
   3. Decorative elements
   4. Text content
   5. ...etc

8. **Helper Method Signatures** — For each non-trivial drawing operation:
   ```csharp
   private void DrawChip(Vector2 cardCenter, float rotation, float vs, ...)
   private void DrawDeltaSlab(Vector2 cardCenter, float rotation, float vs, ...)
   ```

9. **Verification Steps** — Checklist for testing the implementation

### Important Conventions

- **System class**: Inherit from `CardDisplayBase`, add `[DebugTab("Tab Name")]` attribute
- **Toggle pattern**: Use a static toggle class so old/new renderers coexist
- **Event handlers**: Subscribe to render events (e.g., `CardRenderEvent`), guard with toggle check
- **Cursor-based layout**: For flowing content, track a `cursorY` variable:
  ```csharp
  float cursorY = startY;
  // draw element at cursorY
  cursorY += elementHeight + spacing;
  // draw next element at cursorY
  ```
- **No hardcoded magic numbers**: Every numeric value becomes a `[DebugEditable]` property
- **Cache textures**: Use the existing cache dictionaries in `CardDisplayBase`
- **CW/CH constants**: Define card width/height as properties, reference as `CW`/`CH` shorthand:
  ```csharp
  private float CW => CardWidth;
  private float CH => CardHeight;
  ```

## What NOT to Do

- Do NOT write the actual C# implementation — only the plan document
- Do NOT skip any CSS property — account for every visual detail
- Do NOT approximate colors — convert exactly
- Do NOT use `MouseState` or `GamePad` — use CursorEvents (per project CLAUDE.md)
- Do NOT add features beyond what the HTML shows
