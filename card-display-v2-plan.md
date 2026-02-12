# CardDisplaySystemV2 — Runic Slab Card Design

## Context

The current `CardDisplaySystem` renders cards with a simple flat layout (solid background, text, cost pips, damage trapezoid, block/shield). A new card design has been finalized in `card-concepts-final.html` featuring a layered "Runic Slab" aesthetic: left stripe, stat gutter, stat chips with delta slabs, type notch badge, and structured content area. This plan creates a **new** `CardDisplaySystemV2` alongside the existing system with a runtime toggle, so both can be tested without risk.

---

## Files to Create

| File | Purpose |
|------|---------|
| `ECS/Scenes/CardDisplayBase.cs` | Abstract base class with shared drawing helpers |
| `ECS/Scenes/CardDisplaySystemV2.cs` | New v2 renderer matching HTML design |
| `ECS/Scenes/CardDisplayToggle.cs` | Static toggle class |

## Files to Modify

| File | Change |
|------|--------|
| `ECS/Scenes/CardDisplaySystem.cs` | Inherit from `CardDisplayBase`, add toggle guard |
| `ECS/Singletons/FontSingleton.cs` | Add `ChakraPetchFont` property |
| `ECS/Rendering/RoundedRectTextureFactory.cs` | Add per-corner-radius method |
| `Game1.cs` | Register `CardDisplaySystemV2` |

---

## Step 1: Add ChakraPetch Font to FontSingleton

**File:** `ECS/Singletons/FontSingleton.cs`

Add a third font field so v2 can use ChakraPetch for body text while v1 keeps NewRocker unchanged:

```csharp
private static SpriteFont _chakraPetchFont;
public static SpriteFont ChakraPetchFont => _chakraPetchFont;
// In Initialize():
_chakraPetchFont = content.Load<SpriteFont>("Fonts/ChakraPetch");
```

ChakraPetch.spritefont already exists in `Content/Fonts/` and is registered in `Content.mgcb`.

---

## Step 2: Add Per-Corner Rounded Rect to RoundedRectTextureFactory

**File:** `ECS/Rendering/RoundedRectTextureFactory.cs`

Add a new static method following the exact same AA pattern as `CreateRoundedRect` but with independent corner radii:

```csharp
public static Texture2D CreateRoundedRectPerCorner(
    GraphicsDevice device, int w, int h,
    int radiusTL, int radiusTR, int radiusBR, int radiusBL)
```

This is needed for:
- **Type notch**: `(0, 8, 0, 8)` — top-right and bottom-left rounded
- **Chip with slab (top)**: `(4, 4, 0, 0)` — only top corners rounded
- **Delta slab (bottom)**: `(0, 0, 4, 4)` — only bottom corners rounded

Same subpixel AA technique (distance-based alpha at corner centers), just checking which corner radius to use per quadrant.

---

## Step 3: Create CardDisplayBase Abstract Base Class

**New file:** `ECS/Scenes/CardDisplayBase.cs`

Extract these **shared** members from `CardDisplaySystem.cs` into a protected abstract base:

**Fields (protected):**
- `_graphicsDevice`, `_spriteBatch`, `_content`, `_pixelTexture`
- `_textureCache`, `_roundedRectCache`
- `_nameFont` (TitleFont), `_contentFont` (ContentFont)
- `_settings` (CardVisualSettings, lazy-loaded)

**Drawing helpers (protected):**
- `DrawRectangleRotatedLocalScaled(cardCenter, rotation, localOffset, w, h, color, scale, cardW, cardH)` — **add cardW/cardH params** so v2 can pass its own dimensions instead of reading `_settings`
- `DrawTextureRotatedLocalScaled(cardCenter, rotation, localOffset, texture, size, color, scale, cardW, cardH)`
- `DrawCardTextRotatedSingleScaled(cardCenter, rotation, localOffset, text, color, scale, overallScale, cardW, cardH)`
- `DrawCardTextWrappedRotatedScaled(cardCenter, rotation, localOffset, text, color, scale, overallScale, font, maxWidth, cardW, cardH)`
- `DrawCirclePipRotatedScaled(...)` — same pattern
- `GetRoundedRectTexture(w, h, radius)` — cached
- `GetOrLoadTexture(assetName)`
- `GetCardVisualRect(position, scale, cardW, cardH, offsetYExtra)` — parameterized version

**Constructor:** Accepts `(EntityManager, GraphicsDevice, SpriteBatch, ContentManager)`, creates pixel texture, sets font refs.

**Overrides:** `GetRelevantEntities()` → entities with `CardData`, `UpdateEntity()` → empty.

The key design decision: drawing helpers accept `cardW` and `cardH` as parameters for the coordinate transform (converting local-from-top-left to centered-local), rather than reading from `_settings`. This lets v2 use 240x340 while v1 uses 250x350 through the same helpers.

---

## Step 4: Refactor CardDisplaySystem to Inherit from CardDisplayBase

**File:** `ECS/Scenes/CardDisplaySystem.cs`

- Change: `public class CardDisplaySystem : CardDisplayBase`
- Remove all fields and helpers now in the base class
- Keep: all `[DebugEditable]` properties, `DrawCard()`, all v1-specific draw methods, color methods, event handlers
- Update helper calls to pass `_settings.CardWidth` and `_settings.CardHeight` as the cardW/cardH params
- Add toggle guard to each of the 3 event handlers:
  ```csharp
  if (CardDisplayToggle.UseV2) return;
  ```

---

## Step 5: Create CardDisplayToggle

**New file:** `ECS/Scenes/CardDisplayToggle.cs`

```csharp
public static class CardDisplayToggle
{
    public static bool UseV2 { get; set; } = false;
}
```

---

## Step 6: Create CardDisplaySystemV2

**New file:** `ECS/Scenes/CardDisplaySystemV2.cs`

### 6a. Class Structure

```
[DebugTab("Card Display V2")]
public class CardDisplaySystemV2 : CardDisplayBase
```

### 6b. Toggle Property

```csharp
[DebugEditable(DisplayName = "Use V2 Card Display")]
public bool UseV2 { get => CardDisplayToggle.UseV2; set => CardDisplayToggle.UseV2 = value; }
```

### 6c. DebugEditable Properties (~50 properties, all magic numbers)

**Card Frame:**
- `V2CardWidth = 240`, `V2CardHeight = 340`, `V2CornerRadius = 8`

**Stripe:**
- `StripeWidth = 6`

**Stat Gutter:**
- `GutterX = 6`, `GutterWidth = 55`

**Type Notch:**
- `NotchCornerRadius = 8`, `NotchPadTop = 5`, `NotchPadRight = 10`, `NotchPadBot = 5`, `NotchPadLeft = 14`, `NotchFontScale` (tuned to ~0.5rem equivalent)

**Chip Layout:**
- `ChipSize = 42`, `ChipCornerRadius = 4`, `ChipColumnX = 14`, `ChipColumnTopY = 14`, `ChipSlotHeight = 62`, `ChipBorderThickness = 2` (for BLK border=2.5 → round to 2 or 3), `ChipValueFontScale`, `ChipLabelFontScale`

**Delta Slab:**
- `SlabWidth = 42`, `SlabHeight = 16`, `SlabCornerRadius = 4`, `SlabFontScale`

**Content Area:**
- `ContentMarginLeft = 65`, `ContentPadTop = 14`, `ContentPadRight = 14`

**Cost Pips:**
- `V2PipDiameter = 13`, `V2PipGap = 5`

**Name:**
- `V2NameFontScale`, `V2NameMarginBottom = 6`

**Rule Line:**
- `RuleHeight = 2`, `RuleMarginBottom = 8`

**Description:**
- `V2DescFontScale`, `V2DescLineHeight = 1.45`

**Art:**
- `V2ArtWidth = 170`, `V2ArtHeight = 150`, `V2ArtOffsetRight = -15`, `V2ArtOffsetBottom = -10`

### 6d. Color Palette (private readonly dictionaries)

All CSS colors converted to MonoGame `Color` values. Store as dictionaries keyed by `CardData.CardColor` for DRY color resolution. Example:

```csharp
private static readonly Dictionary<CardData.CardColor, Color> BgColors = new()
{
    { CardData.CardColor.White, new Color(220, 215, 206) },  // #dcd7ce
    { CardData.CardColor.Red,   new Color(28, 12, 12) },     // #1c0c0c
    { CardData.CardColor.Black, new Color(19, 19, 19) },     // #131313
};
```

Full palette (40+ unique colors, see mapping tables below).

### 6e. Event Handlers

Subscribe to `CardRenderEvent`, `CardRenderScaledEvent`, `CardRenderScaledRotatedEvent`. Each handler:
1. Guard: `if (!CardDisplayToggle.UseV2) return;`
2. Publish `HighlightRenderEvent` (same as v1)
3. Call `DrawCardV2(entity, position)`

For scaled/rotated variants, temporarily override transform scale/rotation (same pattern as v1).

### 6f. DrawCardV2 Method — Render Pipeline

```csharp
public void DrawCardV2(Entity entity, Vector2 position)
```

Orchestrates all layers in order:

1. **Background** — AA rounded rect (V2CardWidth x V2CardHeight, V2CornerRadius), tinted by `BgColors[cardColor]`
2. **Stripe** — Solid rect at (0, 0), StripeWidth x V2CardHeight
3. **Stat Gutter** — Rect at (GutterX, 0), GutterWidth x V2CardHeight, semi-transparent color via `Color * alpha`
4. **Type Notch** — Per-corner rounded rect anchored top-right, text for card type name
5. **Stat Chips** — Up to 3 chips rendered at fixed Y positions:
   - `DrawChip(chipType, x, y, value, label, cardColor)` — draws rounded rect + value + label text
   - `DrawDeltaSlab(x, y, delta, cardColor)` — draws slab below chip if delta != 0
   - Chip gets `(r, r, 0, 0)` corners when slab present
6. **Cost Pips** — Row of circles at content area top
7. **Card Name** — NewRocker font, positioned below cost row
8. **Rule Line** — 2px separator
9. **Description** — ChakraPetch font, wrapped text
10. **Card Art** — Texture at bottom-right with overflow offsets

### 6g. Helper: DrawChip

Handles all 4 chip types (BLK, ATK, AP, FREE) with variant-specific colors:
- **BLK**: Bordered chip — draw outer rounded rect in border color, inner rounded rect in bg color, value + label text
- **ATK**: Solid filled chip — single rounded rect, value + label text
- **AP**: Solid filled chip — variant-specific colors
- **FREE**: Dashed border — draw as series of small dash rects along each edge, or use a generated dashed-outline texture

For FREE chip dashed border: generate a texture in `PrimitiveTextureFactory` that creates a dashed rounded-rect outline. Parameters: `(device, size, cornerRadius, borderThickness, dashLength, gapLength)`. Cache like other primitives.

### 6h. Helper: DrawDeltaSlab

Small horizontal bar below chip showing +N or -N:
- Per-corner rounded rect `(0, 0, SlabCornerRadius, SlabCornerRadius)`
- Background color: green (positive) or amber (negative), with W variant overrides
- Centered delta text

---

## Step 7: Register in Game1.cs

**File:** `Game1.cs`

After line 148 (`_cardDisplaySystem = ...`), add:
```csharp
_cardDisplaySystemV2 = new CardDisplaySystemV2(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
```

After line 177 (`_world.AddSystem(_cardDisplaySystem)`), add:
```csharp
_world.AddSystem(_cardDisplaySystemV2);
```

Add field declaration alongside other systems.

---

## Complete Color Palette Reference

### Card Backgrounds
| Variant | CSS | MonoGame |
|---|---|---|
| W | `#dcd7ce` | `new Color(220, 215, 206)` |
| R | `#1c0c0c` | `new Color(28, 12, 12)` |
| K | `#131313` | `new Color(19, 19, 19)` |

### Stripe
| Variant | CSS | MonoGame |
|---|---|---|
| W | `#999` | `new Color(153, 153, 153)` |
| R/K | `#cc2222` | `new Color(204, 34, 34)` |

### Stat Gutter (semi-transparent, use premultiplied: `Color * alpha`)
| Variant | CSS | MonoGame |
|---|---|---|
| W | `rgba(0,0,0,0.05)` | `Color.Black * 0.05f` |
| R | `rgba(0,0,0,0.22)` | `Color.Black * 0.22f` |
| K | `rgba(255,255,255,0.025)` | `Color.White * 0.025f` |

### Type Notch BG
| Variant | CSS | MonoGame |
|---|---|---|
| W | `rgba(0,0,0,0.06)` | `Color.Black * 0.06f` |
| R | `rgba(200,30,30,0.15)` | `new Color(200, 30, 30) * 0.15f` |
| K | `rgba(255,255,255,0.04)` | `Color.White * 0.04f` |

### Type Notch Text
| Variant | CSS | MonoGame |
|---|---|---|
| W | `#999` | `new Color(153, 153, 153)` |
| R | `#884433` | `new Color(136, 68, 51)` |
| K | `#555` | `new Color(85, 85, 85)` |

### BLK Chip
| Variant/Element | CSS | MonoGame |
|---|---|---|
| W border | `#999` | `new Color(153, 153, 153)` |
| W bg | `rgba(0,0,0,0.08)` | `Color.Black * 0.08f` |
| W value | `#555` | `new Color(85, 85, 85)` |
| W label | `#777` | `new Color(119, 119, 119)` |
| R/K border | `#777` | `new Color(119, 119, 119)` |
| R/K bg | `rgba(80,80,80,0.15)` | `new Color(80, 80, 80) * 0.15f` |
| R/K value | `#bbb` | `new Color(187, 187, 187)` |
| R/K label | `#888` | `new Color(136, 136, 136)` |

### ATK Chip (all variants)
| Element | CSS | MonoGame |
|---|---|---|
| bg | `#cc2222` | `new Color(204, 34, 34)` |
| value | `#fff` | `Color.White` |
| label | `#ffccbb` | `new Color(255, 204, 187)` |

### AP Chip
| Variant/Element | CSS | MonoGame |
|---|---|---|
| W bg | `#444` | `new Color(68, 68, 68)` |
| W value | `#ddd` | `new Color(221, 221, 221)` |
| W label | `#aaa` | `new Color(170, 170, 170)` |
| R bg | `#330e0e` | `new Color(51, 14, 14)` |
| R/K value | `#dd4433` | `new Color(221, 68, 51)` |
| R/K label | `#993322` | `new Color(153, 51, 34)` |
| K bg | `#1e1e1e` | `new Color(30, 30, 30)` |

### FREE Chip
| Variant/Element | CSS | MonoGame |
|---|---|---|
| W border | `#aaa` | `new Color(170, 170, 170)` |
| W value | `#888` | `new Color(136, 136, 136)` |
| W label | `#aaa` | `new Color(170, 170, 170)` |
| R/K border | `#883322` | `new Color(136, 51, 34)` |
| R/K value | `#cc5544` | `new Color(204, 85, 68)` |
| R/K label | `#883322` | `new Color(136, 51, 34)` |

### Cost Pips
| Variant | CSS | MonoGame |
|---|---|---|
| W | `#a09888` | `new Color(160, 152, 136)` |
| R | `#666` | `new Color(102, 102, 102)` |
| K | `#555` | `new Color(85, 85, 85)` |

### Card Name Text
| Variant | CSS | MonoGame |
|---|---|---|
| W | `#1a1a1a` | `new Color(26, 26, 26)` |
| R | `#f0e0d8` | `new Color(240, 224, 216)` |
| K | `#e8e4e0` | `new Color(232, 228, 224)` |

### Rule Line
| Variant | CSS | MonoGame |
|---|---|---|
| W | `#c0b8aa` | `new Color(192, 184, 170)` |
| R | `#442020` | `new Color(68, 32, 32)` |
| K | `#333` | `new Color(51, 51, 51)` |

### Description Text
| Variant | CSS | MonoGame |
|---|---|---|
| W | `#444` | `new Color(68, 68, 68)` |
| R | `#cc9988` | `new Color(204, 153, 136)` |
| K | `#999` | `new Color(153, 153, 153)` |

### Delta Slab — Positive
| Variant/Element | CSS | MonoGame |
|---|---|---|
| R/K bg | `#1a5a1a` | `new Color(26, 90, 26)` |
| R/K text | `#5eff5e` | `new Color(94, 255, 94)` |
| W bg | `#2a8a2a` | `new Color(42, 138, 42)` |
| W text | `#fff` | `Color.White` |

### Delta Slab — Negative
| Variant/Element | CSS | MonoGame |
|---|---|---|
| R/K bg | `#5a4200` | `new Color(90, 66, 0)` |
| R/K text | `#ffcc44` | `new Color(255, 204, 68)` |
| W bg | `#7a6210` | `new Color(122, 98, 16)` |
| W text | `#fff` | `Color.White` |

---

## Pixel Position Reference (240x340 card space, from top-left)

| Element | X | Y | Width | Height |
|---|---|---|---|---|
| Background | 0 | 0 | 240 | 340 |
| Stripe | 0 | 0 | 6 | 340 |
| Stat Gutter | 6 | 0 | 55 | 340 |
| Type Notch | right-aligned | 0 | text-dependent | ~22 |
| BLK Chip | 14 | 14 | 42 | 42 |
| BLK Delta Slab | 14 | 56 | 42 | 16 |
| ATK Chip | 14 | 76 | 42 | 42 |
| ATK Delta Slab | 14 | 118 | 42 | 16 |
| AP/FREE Chip | 14 | 284 | 42 | 42 |
| Cost Pips | 65 | 14 | 13 each, 5 gap | 13 |
| Card Name | 65 | ~31 | content-width | font-dependent |
| Rule Line | 65 | below name + 6 | 161 (240-65-14) | 2 |
| Description | 65 | below rule + 8 | 161 | flex |
| Card Art | 85 | 200 | 170 | 150 |

---

## Verification

1. **Build**: `dotnet build` — confirm no compilation errors
2. **Run**: Launch game, enter battle scene with cards visible
3. **Toggle**: Open debug menu → "Card Display V2" tab → check "Use V2 Card Display"
4. **Visual check**: Cards should show the layered Runic Slab layout with stripe, gutter, chips, notch
5. **Variants**: Test with White, Red, and Black cards — verify all color palettes
6. **Deltas**: Play cards that modify block/damage — verify delta slabs appear with correct +/- colors
7. **Toggle back**: Uncheck v2 toggle — v1 should render exactly as before
8. **Tuning**: Use DebugEditable sliders to fine-tune any positions that need adjustment
