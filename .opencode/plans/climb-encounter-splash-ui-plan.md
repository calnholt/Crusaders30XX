# Climb Encounter Splash — UI Plan

## 1. Context

**Mockup:** `mockups/climb-encounter-splash-v1.html` — "Gate Break" fullscreen splash animation playing when the player clicks an encounter on the climb screen. Enemy name letters slam in individually; portrait scales in with a bounce; a diagonal slash sweeps behind; then everything fades out as the battle scene loads underneath.

**Target scene:** `ECS/Scenes/ClimbScene/` — the splash renders as a fullscreen overlay on top of the existing climb background + header + columns.

**Archetype: B — Screen overlay.** Fullscreen dim + animated content. No card-local rotation; no scroll list. Uses the overlay pattern from `QuestRewardModalDisplaySystem` / `TransitionDisplaySystem`.

**Trigger flow (current → new):**
- Current: click encounter → `ClimbEncounterService.TryQueueEncounter` → `ShowTransition { Scene = Battle }` → `TransitionDisplaySystem` red wipe
- New: click encounter → `ClimbEncounterService.TryQueueEncounter` → `ClimbEncounterSplashRequested` → splash animation plays → load Battle scene during black phase → fade out to reveal battle → `TransitionCompleteEvent`

---

## 2. Files to Create / Modify

| File | Action | Purpose |
|------|--------|---------|
| `ECS/Scenes/ClimbEncounterSplashDisplaySystem.cs` | **CREATE** | Full splash animation system (draw + update). Global in `Game1.cs`, not under `ClimbSceneSystem` — must survive scene change from Climb→Battle. |
| `ECS/Events/ClimbEvents.cs` | **MODIFY** | Add `ClimbEncounterSplashRequested` event class |
| `ECS/Events/SceneEvents.cs` | **MODIFY** | Add `BattleSceneInitializedEvent` class |
| `ECS/Services/ClimbEncounterService.cs` | **MODIFY** | Change `TryQueueEncounter` and `TryQueuePendingFinalEncounter` to publish `ClimbEncounterSplashRequested` instead of `ShowTransition` |
| `Game1.cs` | **MODIFY** | Create `ClimbEncounterSplashDisplaySystem` as global system in `Initialize()`; add its `Draw()` to global overlay section (after `TransitionDisplaySystem.Draw`) |
| `ECS/Scenes/BattleScene/BattleSceneSystem.cs` | **MODIFY** | Publish `BattleSceneInitializedEvent` after `InitBattle()` completes in `OnLoadSceneEvent` |

### Tests to update

| File | Action |
|------|--------|
| `tests/Crusaders30XX.Tests/ClimbEncounterServiceTests.cs` | **MODIFY** — change assertions from `ShowTransition` to `ClimbEncounterSplashRequested` |
| `tests/Crusaders30XX.Tests/ClimbEventSystemTests.cs` | **MODIFY** — same |

---

## 3. Implementation Steps

### Step 1: Add `ClimbEncounterSplashRequested` event

**File:** `ECS/Events/ClimbEvents.cs`

```csharp
public class ClimbEncounterSplashRequested
{
    public string SlotId { get; set; } = string.Empty;
    public string EnemyId { get; set; } = string.Empty;
    public string EnemyName { get; set; } = string.Empty;
}
```

### Step 2: Add `BattleSceneInitializedEvent`

**File:** `ECS/Events/SceneEvents.cs`

```csharp
public class BattleSceneInitializedEvent
{
    public SceneId Scene { get; set; }
}
```

### Step 3: Modify `ClimbEncounterService` to emit splash event

**File:** `ECS/Services/ClimbEncounterService.cs`

In `TryQueueEncounter`, after queuing encounter data (line 44-53), replace `EventManager.Publish(new ShowTransition ...)` with:

```csharp
var enemy = EnemyFactory.Create(slot.enemyId);
EventManager.Publish(new ClimbEncounterSplashRequested
{
    SlotId = encounterSlotId,
    EnemyId = slot.enemyId,
    EnemyName = enemy?.Name ?? slot.enemyId,
});
```

Same pattern in `TryQueuePendingFinalEncounter` (line 140), but with `EnemyId = "fallen_shepherd"` and name resolved from factory.

Remove the `ShowTransition` publish from both methods.

### Step 4: Create `ClimbEncounterSplashDisplaySystem`

**File:** `ECS/Scenes/ClimbEncounterSplashDisplaySystem.cs`

**Namespace:** `Crusaders30XX.ECS.Systems` (same as `TransitionDisplaySystem`)

`[DebugTab("Climb Splash")]` class inheriting `Core.System`. Structure:

**Fields:**
- `_graphicsDevice`, `_spriteBatch`, `_content` — standard scene resources
- `_pixel` — 1×1 white texture
- `_titleFont = FontSingleton.TitleFont` (New Rocker, native size 128)
- `_bodyFont = FontSingleton.ChakraPetchFont` (Chakra Petch, native size 128)
- `_enemyName`, `_enemyId` — from event
- `_enemyPortrait` — `Texture2D` loaded on demand
- `_phase` — enum: `Idle, FadeIn, GateOpen, Hold, FadeOut`
- `_phaseTime`, `_totalTime` — animation timers
- `_sceneLoaded`, `_sceneLoadRequested` — battle loading flags
- `_nextScene` — `SceneId.Battle`
- `_scissorRasterizerState` — `new RasterizerState { ScissorTestEnable = true }`
- `_layout` — `SplashLayout` struct instance

**Constructor:**
- Standard scene resource params
- Create `_pixel`, `_scissorRasterizerState`
- `EventManager.Subscribe<ClimbEncounterSplashRequested>(OnSplashRequested)`
- `EventManager.Subscribe<BattleSceneInitializedEvent>(OnBattleSceneInitialized)`
- `EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCaches)` to clean up `_enemyPortrait`

**Update:**
```csharp
public override void Update(GameTime gameTime)
{
    if (_phase == Phase.Idle) return;
    StateSingleton.IsActive = true;
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
    _phaseTime += dt;
    _totalTime += dt;
    switch (_phase)
    {
        case Phase.FadeIn:
            if (_phaseTime >= FadeInDurationSeconds) { _phase = Phase.GateOpen; _phaseTime = 0f; }
            break;
        case Phase.GateOpen:
            // Load battle scene once shade is fully opaque
            if (!_sceneLoadRequested && GetShadeAlpha() >= ShadeAlpha * 0.99f)
            {
                LoadTargetScene(SceneId.Battle);
            }
            if (_phaseTime >= GateOpenDurationSeconds) { _phase = Phase.Hold; _phaseTime = 0f; }
            break;
        case Phase.Hold:
            if (_phaseTime >= HoldDurationSeconds) { _phase = Phase.FadeOut; _phaseTime = 0f; }
            break;
        case Phase.FadeOut:
            if (_phaseTime >= FadeOutDurationSeconds)
            {
                EventManager.Publish(new TransitionCompleteEvent { Scene = SceneId.Battle });
                StateSingleton.IsActive = false;
                _phase = Phase.Idle;
                _phaseTime = 0f;
                _totalTime = 0f;
            }
            break;
    }
}
```

**Draw:** See Section 7 for full pipeline order.

**Scene load (copied from `TransitionDisplaySystem`):**
```csharp
private void LoadTargetScene(SceneId nextScene)
{
    var sceneEntity = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
    var previous = sceneEntity?.GetComponent<SceneState>()?.Current ?? SceneId.None;
    EventManager.Publish(new DeleteCachesEvent { Scene = nextScene });
    DeleteEntities(nextScene);
    EventManager.Publish(new LoadSceneEvent { Scene = nextScene, PreviousScene = previous });
    _sceneLoadRequested = true;
}
```

### Step 5: Register splash system as global system in `Game1.cs`

**File:** `Game1.cs`

The splash system must be global because it needs to draw AFTER the scene switches from `Climb` to `Battle` during its FadeOut phase. Systems owned by `ClimbSceneSystem` are removed when the scene changes, which would destroy the splash mid-animation.

In `Initialize()`, after the existing `_transitionDisplaySystem` creation (line 184), add:

```csharp
_climbSplashDisplaySystem = new ClimbEncounterSplashDisplaySystem(
    _world.EntityManager, GraphicsDevice, _spriteBatch, Content);
_world.AddSystem(_climbSplashDisplaySystem);
```

Add field near the other display system fields:

```csharp
private ClimbEncounterSplashDisplaySystem _climbSplashDisplaySystem;
```

In `DrawGlobalOverlays()` (around line 604), add AFTER `TransitionDisplaySystem.Draw`:

```csharp
FrameProfiler.Measure("ClimbEncounterSplashDisplaySystem.Draw",
    _climbSplashDisplaySystem.Draw);
```

Drawing after `TransitionDisplaySystem` ensures the splash shade always renders on top of any red wipe from non-climb transitions that might be in progress.

### Step 6: Publish `BattleSceneInitializedEvent` from `BattleSceneSystem`

**File:** `ECS/Scenes/BattleScene/BattleSceneSystem.cs`

In `OnLoadSceneEvent` (around line 199), **after** `InitBattle()` call (and regardless of whether dialog will show), add:

```csharp
EventManager.Publish(new BattleSceneInitializedEvent { Scene = SceneId.Battle });
```

### Step 7: Update tests

**File:** `tests/Crusaders30XX.Tests/ClimbEncounterServiceTests.cs`

- Change `ShowTransition transition = null;` → `ClimbEncounterSplashRequested splash = null;`
- Change subscribe: `EventManager.Subscribe<ClimbEncounterSplashRequested>(evt => splash = evt);`
- Change assertions to verify `splash.EnemyName`, `splash.EnemyId`, etc.
- Same pattern in `ClimbEventSystemTests.cs`

---

## 4. `DebugEditable` Properties

All float scales use `Step = 0.01f`. Grouped by region.

### Timing

| Property | Default | Range | Notes |
|----------|---------|-------|-------|
| `FadeInDurationSeconds` | 0.14f | 0.05–0.5f | Shade fade-in (CSS 6% of 2.4s) |
| `GateOpenDurationSeconds` | 1.73f | 0.5–4f | Gate expand + content anim + hold (CSS 6→78%) |
| `HoldDurationSeconds` | 0.14f | 0–1f | Content fade-out before shade fade (CSS 78→84%) |
| `FadeOutDurationSeconds` | 0.38f | 0.1–1f | Shade fade to reveal battle (CSS 84→100%) |

### Gate

| Property | Default | Range | Notes |
|----------|---------|-------|-------|
| `GateStartSize` | 60f | 10–300 | Initial gate rect size (px) when expanding from center |
| `GateFrameThickness` | 4f | 1–8 | Red border thickness |
| `GateFrameInset` | 0.08f | 0–0.25f | Frame inset as fraction of viewport |

### Slash

| Property | Default | Range | Notes |
|----------|---------|-------|-------|
| `SlashThickness` | 8f | 2–20 | Diagonal line height |
| `SlashDelay` | 0.34f | 0–1f | Seconds after splash start before slash appears |
| `SlashDuration` | 0.19f | 0.05–0.5f | Time slash takes to sweep and disappear |

### Portrait

| Property | Default | Range | Notes |
|----------|---------|-------|-------|
| `PortraitWidth` | 300f | 100–600 | |
| `PortraitHeight` | 380f | 100–600 | |
| `PortraitCropTopBias` | 0.07f | 0–1f | Same as ClimbColumnDisplaySystem.PortraitCropTopBias |
| `PortraitBounceOvershoot` | 1.1f | 1–1.5f | Scale at peak of bounce |
| `PortraitBounceSettle` | 0.97f | 0.8–1.2f | Scale at settle point |
| `PortraitShadowAlpha` | 0.8f | 0–1f | Box-shadow alpha |

### Name Typography

| Property | Default | Range | Notes |
|----------|---------|-------|-------|
| `NameFontScale` | 0.44f | 0.1–1f | 56px CSS / 128px native ≈ 0.438 |
| `NameLetterGap` | 4f | 0–20 | Pixels between letter positions |
| `NameLetterDelay` | 0.026f | 0–0.1f | Seconds stagger per letter |
| `NameStartPercent` | 0.38f | 0–0.8f | Fraction of total dur when first letter arrives |
| `NameDropHeight` | 48f | 0–100 | Y offset letters drop from |
| `NameGapToPortrait` | 20f | 0–80 | Gap between portrait bottom and name top |

### Subtitle

| Property | Default | Range | Notes |
|----------|---------|-------|-------|
| `SubtitleFontScale` | 0.109f | 0.03–0.3f | 14px CSS / 128px native ≈ 0.109 |
| `SubtitleGapToName` | 20f | 0–60 | Gap between name bottom and subtitle top |
| `SubtitleText` | `"Prepare for battle"` | — | Configurable via debug |
| `SubtitleLetterSpacing` | 6f | 0–20 | Additional px spacing (note: spritefont cannot do CSS letter-spacing) |

### Shade / Background

| Property | Default | Range | Notes |
|----------|---------|-------|-------|
| `ShadeAlpha` | 0.92f | 0–1f | Max opacity of black overlay |
| `DimSceneAlpha` | 0.22f | 0–1f | Climb scene dim before shade covers (matches ClimbBackgroundDisplaySystem.BackgroundDimAlpha) |

---

## 5. Color Palette

### Overlay / Chrome

| CSS Variable | CSS Value | MonoGame |
|-------------|-----------|----------|
| (gate bg deep) | `#050505` gradient | `new Color(5, 5, 5)` fading to `new Color(20, 10, 12) * 0.95f` |
| (gate diagonal tint) | `rgba(196,30,58,0.15)` | `new Color(196, 30, 58) * 0.15f` |
| `--red-3` | `#c41e3a` | `new Color(196, 30, 58)` = `ClimbSceneDrawHelpers.Red3` |
| `--red-glow` | `rgba(196,30,58,0.55)` | `new Color(196, 30, 58) * 0.55f` |

### Text

| CSS Variable | CSS Value | MonoGame |
|-------------|-----------|----------|
| `--white-1` | `#ffffff` | `Color.White` = `ClimbSceneDrawHelpers.White1` |
| `--white-2` | `#f0ece6` | `new Color(240, 236, 230)` = `ClimbSceneDrawHelpers.White2` |
| `--white-3` | `#c8c0b8` | `new Color(200, 192, 184)` = `ClimbSceneDrawHelpers.White3` |
| Name text-shadow | `rgba(196,30,58,0.7)` | `new Color(196, 30, 58) * 0.7f` |

### Shade

| CSS | MonoGame |
|-----|----------|
| `rgba(0,0,0,0.92)` | `Color.Black * 0.92f` |
| `rgba(0,0,0,0.22)` (climb dim) | `Color.Black * 0.22f` |

### Slash

| CSS | MonoGame |
|-----|----------|
| `transparent → #fff → #c41e3a → #fff → transparent` | Solid `Red3` rect + 2 white glow strips at lower alpha |

---

## 6. Pixel Position Reference (1920×1080)

All positions screen-space top-left.

| Element | X | Y | W | H | Notes |
|---------|---|---|---|---|-------|
| Fullscreen shade | 0 | 0 | 1920 | 1080 | Black alpha overlay, max opacity 0.92 |
| Scrim dim (climb bg) | 0 | 0 | 1920 | 1080 | Extra dim over climb (phases out early) |
| Gate full area | 0 | 0 | 1920 | 1080 | Expanding clip from center |
| Gate frame (at full) | 154 | 86 | 1612 | 908 | 8% inset: x=1920*0.08=154, y=1080*0.08=86, w=1920-308, h=1080-172 |
| Slash line | 0 | 536 | ~2178 | 8 | Centered at vy=540, rotated atan2(-1080,1920) ≈ -29.3° |
| Portrait rect | 810 | 300 | 300 | 380 | Center: (960, 490) |
| Name text | center at 960 | ~700 | varies | ~61 | y = portraitBottom(680) + 20 gap |
| Subtitle | center at 960 | ~761 | varies | ~20 | y = nameBottom + 20 gap |

---

## 7. Draw Order Pipeline

1. **Climb scene background** — drawn by `ClimbBackgroundDisplaySystem` (climb systems already drawn before splash)
2. **Scrim dim** — `Color.Black * DimSceneAlpha` full rect (visible only while shade is below max; phases out after FadeIn)
3. **Gate background** — expanding rect from center via scissor, filled with dark gradient (`#050505` → `rgba(20,10,12,0.95)`)
4. **Gate frame** — `ClimbSceneDrawHelpers.DrawBorder` at 8% inset, `Red3` color, inside gate clip
5. **Slash** — diagonal rotated rectangle behind portrait; solid `Red3` rect + 2 white glow strips at lower alpha
6. **Portrait shadow** — `Color.Black * PortraitShadowAlpha` rect offset Y +20, height = PortraitHeight - 20
7. **Enemy portrait** — `ClimbSceneDrawHelpers.DrawPortraitCropped` with `PortraitCropTopBias = 0.07f`
8. **Name letters** — `_titleFont.DrawString` per character, staggered positions
9. **Name glow** — behind name letters: offset passes with `new Color(196, 30, 58) * 0.7f` for text-shadow effect
10. **Subtitle** — `_bodyFont.DrawString` centered
11. **Fullscreen shade** — `Color.Black * shadeAlpha` (primary visibility control: fades in, holds, fades out)

---

## 8. Animation Timeline (CSS Keyframe Mapping)

Total splash duration: configurable via `DebugEditable` time properties (default ~2.4s).
Let `TT` = total splash duration = FadeInDuration + GateOpenDuration + HoldDuration + FadeOutDuration.

### Phase: FadeIn (t = 0 → FadeInDurationSeconds)
| Progress | Event |
|----------|-------|
| 0% | Splash begins; `StateSingleton.IsActive = true` |
| 0% → 100% | Shade alpha lerps 0→ShadeAlpha; scrim dim visible |

### Phase: GateOpen (t = FadeInDurationSeconds → FadeIn + GateOpen)
| Progress (within phase) | Event |
|-------------------------|-------|
| 0% → 24% | Gate clip expands (scissor rect from center→fullscreen). EaseOutCubic |
| 0% | **Load battle scene** (shade is fully opaque) via `LoadTargetScene` |
| 14% → 55% | Portrait scales in: 0.2 → 1.1 → 0.97 → 1.0. EaseOutBack bounce |
| 20% → 48% | Slash sweeps: scaleX 0→1→0. Solid Red3 + white glow strips |
| 48% → 65% | Name letters stagger in (each delayed by NameLetterDelay * index) |
| 42% → 60% | Subtitle fades in (alpha 0→1) |
| 60% → 90% | **Hold** — all content at final position |
| 90% → 100% | Content fades out (portrait, letters, subtitle alpha → 0) |

### Phase: Hold (t = FadeIn + GateOpen → FadeIn + GateOpen + HoldDurationSeconds)
| Progress | Event |
|----------|-------|
| 100% | Black-only screen; content already fully faded |

### Phase: FadeOut (t = FadeIn + GateOpen + Hold → total)
| Progress | Event |
|----------|-------|
| 0% → 100% | Shade alpha lerps ShadeAlpha→0. Battle scene becomes visible |
| 100% | Publish `TransitionCompleteEvent { Scene = Battle }`; `StateSingleton.IsActive = false` |

---

## 9. Helper Method Signatures

```csharp
// Layout — computed once per splash start
private struct SplashLayout
{
    public Rectangle Viewport;          // 0,0,1920,1080
    public Rectangle GateFrame;         // 8% inset
    public Vector2 PortraitCenter;      // center of portrait rect
    public Rectangle PortraitRect;      // centered at PortraitCenter
    public Vector2 NameCenter;          // below portrait
    public Vector2 SubtitleCenter;      // below name
    public float SlashAngle;            // atan2(-vh, vw)
    public float SlashLength;           // vw / cos(angle)
    public float[] LetterBaseX;         // per-letter base X positions
}
private SplashLayout ComputeLayout(int vw, int vh);

// Phase helpers
private float GetShadeAlpha();                       // based on _phase and _phaseTime
private float GetGateProgress();                     // 0→1 for gate expansion
private float GetPortraitScale(out float alpha);    // scale + alpha from keyframe
private float GetSlashProgress();                   // scaleX, 0→1→0
private float GetLetterProgress(int index);         // per-letter 0→1
private float GetSubtitleAlpha();
private float GetContentFadeAlpha();                // for fade-out near end of GateOpen

// Draw helpers
private void DrawShade(float alpha);
private void DrawGateBackground(float progress);
private void DrawGateFrame(float progress);
private void DrawSlash(float progress);
private void DrawEnemyPortrait(float scale, float alpha);
private void DrawNameLetters();
private void DrawSubtitle(float alpha);
private void DrawScrimDim(float progress);

// Scene transition (copied from TransitionDisplaySystem)
private void LoadTargetScene(SceneId nextScene);
private void DeleteEntities(SceneId nextScene);

// Easing functions
private static float EaseOutBack(float t);           // c1=1.70158 for bounce
private static float EaseOutCubic(float t);
private static float EaseInOutQuad(float t);
```

---

## 10. Verification Checklist

| Scenario | Expected |
|----------|----------|
| Click encounter on climb screen | Splash plays with enemy name/portrait; battle loads; shade fades to reveal battle |
| Multi-word enemy name (e.g. "Fire Skeleton") | Letters stagger correctly; space character renders as gap |
| Short enemy name (e.g. "Imp") | All letters visible, timing adjusted |
| Click during splash (blocked) | `StateSingleton.IsActive = true` blocks input |
| Debug tab "Climb Splash" | All `DebugEditable` visible and tunable |
| `DebugAction("Preview Gate Break")` | Plays splash without triggering scene change (preview mode) |
| `dotnet build` | No compile errors |
| Unit tests pass | `ClimbEncounterServiceTests` + `ClimbEventSystemTests` with updated assertions |
| Navigate from climb during splash | Splash system is global; survives scene change. Cleans up on `DeleteCachesEvent`. |
| Splash draws on top of battle scene during FadeOut | Verified by draw order: global overlay section draws splash shade AFTER scene draw |
| Non-climb transitions (Location→Battle, etc.) | `TransitionDisplaySystem` red wipe still works |
| `SkipHold` / test-fight paths | ClimbSplash not triggered; existing flow preserved |
| Final encounter (`TryQueuePendingFinalEncounter`) | Splash plays for final boss |
| Enemy has no portrait texture | Graceful fallback — show name text in portrait area |

---

## 11. Fidelity Notes

| CSS Feature | Status | Note |
|-------------|--------|------|
| `letter-spacing` animation on subtitle | Acceptable diff | SpriteFont has no per-glyph spacing control |
| `clip-path: polygon()` gate expansion | Approximated | Scissor rect expanding from center (visually equivalent to square clip expansion) |
| `filter: blur(1px)` on slash | Acceptable diff | 2 white glow strips at low alpha as pseudo-blur; or accept crisp line |
| `cubic-bezier(0.15, 0.85, 0.25, 1)` on gate | Approximated | `EaseOutCubic` is close match |
| `-webkit-background-clip: text` gradient | N/A | Page chrome only; not shipped |
| `box-shadow` on portrait | Approximated | Black rect offset Y+20, height = portraitH-20, alpha 0.8 |
| `text-shadow: 0 0 30px rgba(196,30,58,0.7)` | Approximated | 4 `DrawString` passes at ±2px offsets with `Red3 * 0.7f * 0.25f` |
| `object-fit: cover; object-position: center 7%` | Drawn as | `ClimbSceneDrawHelpers.DrawPortraitCropped` with `PortraitCropTopBias = 0.07f` |
| `linear-gradient` on slash | Acceptable diff | Solid `Red3` rect + 2 white glow strips instead of 5-stop gradient |
| Gate frame `inset 0 0 80px` box-shadow | Acceptable diff | Inner shadow omitted — too complex |

---

## Appendix — Gate Expansion Implementation

Use scissor test for clean gate clip:

```csharp
// In Draw():
_spriteBatch.End(); // end existing batch
_spriteBatch.Begin(rasterizerState: _scissorState);

// Compute expanding clip rect
float gateProgress = GetGateProgress();
int gateW = (int)MathHelper.Lerp(GateStartSize, vw, gateProgress);
int gateH = (int)MathHelper.Lerp(GateStartSize, vh, gateProgress);
var gateClip = new Rectangle(
    (vw - gateW) / 2,
    (vh - gateH) / 2,
    gateW,
    gateH);
_graphicsDevice.ScissorRectangle = gateClip;

// Draw gate background (fullscreen rect, clipped to scissor)
_spriteBatch.Draw(_pixel, new Rectangle(0, 0, vw, vh), gateBgColor);
// Draw gate frame (inset rect, clipped to scissor)
ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, _layout.GateFrame,
    ClimbSceneDrawHelpers.Red3, (int)GateFrameThickness);

_spriteBatch.End();
_spriteBatch.Begin(...); // resume normal batch for remaining elements
```
