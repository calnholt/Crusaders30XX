# Enemy Display Systems Parallax Migration

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate EnemyDisplaySystem and EnemyAttackDisplaySystem to write `Transform.Position` in Update instead of Draw, matching the parallax pattern used by all other migrated systems.

**Architecture:** Both systems currently write Position in Draw, overwriting the parallax-adjusted position every frame. The fix follows the same pattern applied to EndTurnDisplaySystem, MedalDisplaySystem, etc. in commit `dfdb4ff`: move Position writes to Update so the ParallaxLayerSystem can detect them and apply its offset before Draw reads them.

**Tech Stack:** C# / .NET 8.0 / MonoGame DesktopGL

---

### Task 1: EnemyDisplaySystem — Move Position Write to Update

**Files:**
- Modify: `ECS/Scenes/BattleScene/EnemyDisplaySystem.cs`

**Root cause:** Line 133 writes `t.Position = basePos` inside `Draw()`. The parallax system runs during Update, so this overwrites the parallax-adjusted position every frame. The enemy portrait never moves with parallax.

- [ ] **Step 1: Write base position in UpdateEntity**

In `ECS/Scenes/BattleScene/EnemyDisplaySystem.cs`, at the end of `UpdateEntity` (after the attack timer block, before the closing brace at line 78), add the base position computation and Position write:

```csharp
			// Write base position so parallax system can adjust it before Draw
			var t = entity.GetComponent<Transform>();
			if (t != null)
			{
				int viewportW = Game1.VirtualWidth;
				int viewportH = Game1.VirtualHeight;
				t.Position = new Vector2(
					viewportW * (0.5f + CenterOffsetXPct),
					viewportH * (0.5f + CenterOffsetYPct)
				);
			}
```

- [ ] **Step 2: Refactor Draw to read parallax-adjusted Position**

In the `Draw()` method, replace the block from `var basePos` computation through `var drawPos` (lines 107-145) with code that reads the parallax-adjusted `t.Position` and computes only the attack animation offset:

Remove lines 107-110 (`var basePos = new Vector2(...)`) and line 111 (`var posForAnim = basePos;`).

Remove line 133 (`t.Position = basePos;`).

Remove line 145 (`var drawPos = t.Position + (posForAnim - basePos);`).

Replace the attack animation block and drawPos computation (lines 111-145) with:

```csharp
				// t.Position is parallax-adjusted (written in Update, offset by ParallaxLayerSystem)
				var drawPos = t.Position;
				if (_attackAnimTimer > 0f)
				{
					float ta = 1f - (_attackAnimTimer / _attackAnimDuration); // 0->1
					float outPhase = System.Math.Min(0.5f, ta) * 2f; // 0..1 over first half
					float backPhase = System.Math.Max(0f, ta - 0.5f) * 2f; // 0..1 over second half
					Vector2 desired = _attackTargetPos + _attackOffset;
					Vector2 dir = desired - t.Position;
					if (dir.LengthSquared() > 0.0001f)
					{
						dir = Vector2.Normalize(dir);
					}
					else
					{
						dir = Vector2.Normalize(_attackOffset);
					}
					Vector2 outPos = t.Position + dir * AttackNudgePixels;
					Vector2 mid = Vector2.Lerp(t.Position, outPos, 1f - (float)System.Math.Pow(1f - outPhase, 3));
					drawPos = Vector2.Lerp(mid, t.Position, backPhase);
				}
```

The PortraitInfo writes (lines 134-140) and UIElement bounds update (lines 147-155) and the `_spriteBatch.Draw` call (line 156) remain unchanged — they all use `drawPos` which is now parallax-aware.

- [ ] **Step 3: Commit**

```bash
git add ECS/Scenes/BattleScene/EnemyDisplaySystem.cs
git commit -m "refactor: migrate EnemyDisplaySystem to write Position in Update for parallax"
```

---

### Task 2: EnemyAttackDisplaySystem — Move Entity Creation and Position Writes to Update

**Files:**
- Modify: `ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.cs`
- Modify: `ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.Draw.cs`

**Root cause:** Three Position writes happen in Draw methods:
1. `UpdateBannerAnchorTransform` writes banner anchor Position (Draw.cs:397)
2. `DrawConfirmButton` writes confirm button Position (Draw.cs:373)
3. `DrawTextContent` writes tooltip entity Position (Draw.cs:289,302) — this entity has no ParallaxLayer so it's not affected, but entity creation in Draw should still be moved

The banner anchor entity is also lazily created in `BuildDrawContext` (Draw.cs:117-127) which means it doesn't exist during the first Update frame.

- [ ] **Step 1: Move banner anchor entity creation and Position write to UpdateEntity**

In `ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.cs`, in `UpdateEntity`, after the existing `_bannerRect` computation block (after line 408, before the closing brace of UpdateEntity), add:

```csharp
			// Ensure banner anchor entity exists and write its Position for parallax
			var anchorEntity = EntityManager.GetEntitiesWithComponent<EnemyAttackBannerAnchor>().FirstOrDefault();
			if (anchorEntity == null)
			{
				anchorEntity = EntityManager.CreateEntity("EnemyAttackBannerAnchor");
				EntityManager.AddComponent(anchorEntity, new EnemyAttackBannerAnchor());
				EntityManager.AddComponent(anchorEntity, new Transform());
				var parallaxLayer = ParallaxLayer.GetUIParallaxLayer();
				parallaxLayer.MultiplierX = 0.045f;
				parallaxLayer.MultiplierY = 0.045f;
				EntityManager.AddComponent(anchorEntity, parallaxLayer);
				EntityManager.AddComponent(anchorEntity, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), IsInteractable = false });
			}
			var anchorTransform = anchorEntity.GetComponent<Transform>();
			if (anchorTransform != null)
			{
				var centerBase = new Vector2(Game1.VirtualWidth / 2f + OffsetX, Game1.VirtualHeight / 2f + OffsetY);
				anchorTransform.Position = new Vector2(centerBase.X, centerBase.Y + _bannerRect.Height / 2f);
			}
```

- [ ] **Step 2: Write confirm button Position in UpdateEntity and match parallax multipliers**

In `ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.cs`, update `CreateConfirmButton` to use the same parallax multipliers as the banner anchor (so the button shifts in sync with the banner):

```csharp
		private void CreateConfirmButton()
		{
			var primaryBtn = EntityManager.CreateEntity("UIButton_ConfirmEnemyAttack");
			EntityManager.AddComponent(primaryBtn, new Transform{});
			EntityManager.AddComponent(primaryBtn, new UIElement { IsInteractable = true, EventType = UIElementEventType.ConfirmBlocks });
			EntityManager.AddComponent(primaryBtn, new HotKey { Button = FaceButton.Y });
			var parallaxLayer = ParallaxLayer.GetUIParallaxLayer();
			parallaxLayer.MultiplierX = 0.045f;
			parallaxLayer.MultiplierY = 0.045f;
			EntityManager.AddComponent(primaryBtn, parallaxLayer);
		}
```

Then, in `UpdateEntity`, after the anchor entity block added in Step 1, add the confirm button Position write:

```csharp
			// Write confirm button Position so parallax can adjust it
			var confirmBtn = EntityManager.GetEntity("UIButton_ConfirmEnemyAttack");
			if (confirmBtn != null)
			{
				var btnTransform = confirmBtn.GetComponent<Transform>();
				if (btnTransform != null)
				{
					btnTransform.Position = new Vector2(
						_bannerRect.X + _bannerRect.Width / 2f - ConfirmButtonWidth / 2f,
						_bannerRect.Bottom + ConfirmButtonOffsetY
					);
					btnTransform.ZOrder = ConfirmButtonZ;
				}
			}
```

- [ ] **Step 3: Refactor BuildDrawContext to read parallax-adjusted anchor Position**

In `ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.Draw.cs`, in `BuildDrawContext`:

Remove the entity creation block (lines 117-127). The anchor entity is now created in UpdateEntity. Replace the `centerBase`/`center` computation (lines 116-130) with:

```csharp
			var anchorEntity = EntityManager.GetEntitiesWithComponent<EnemyAttackBannerAnchor>().FirstOrDefault();
			if (anchorEntity == null) return null; // Anchor not yet created by Update
			var anchorTransform = anchorEntity.GetComponent<Transform>();
			// Read parallax-adjusted center from the anchor entity
			var center = anchorTransform?.Position ?? new Vector2(vx / 2f + OffsetX, vy / 2f + OffsetY);
```

Remove `CenterBase` from the DrawContext struct (line 21):

```csharp
			public Vector2 Shake, ApproachPos;
```

Remove `CenterBase = centerBase,` from the DrawContext return (around line 174).

- [ ] **Step 4: Remove Position writes from Draw helper methods**

In `ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.Draw.cs`:

**UpdateBannerAnchorTransform** — remove the Position write but keep Scale/Rotation:

```csharp
		private void UpdateBannerAnchorTransform(DrawContext ctx)
		{
			var anchorTransform = ctx.AnchorEntity.GetComponent<Transform>();
			if (anchorTransform != null)
			{
				anchorTransform.Scale = Vector2.One;
				anchorTransform.Rotation = 0f;
			}
		}
```

**DrawConfirmButton** — read the button's parallax-adjusted Position instead of computing from `ctx.Rect`. Replace the button rect computation and the Position/ZOrder write (lines 351-373):

Replace the `btnRect` computation:
```csharp
				// Read parallax-adjusted button position (written in Update, offset by parallax)
				var btnTr = primaryBtn.GetComponent<Transform>();
				var btnPos = btnTr?.Position ?? new Vector2(ctx.Rect.X + ctx.Rect.Width / 2f - ConfirmButtonWidth / 2f, ctx.Rect.Bottom + ConfirmButtonOffsetY);
				var btnRect = new Rectangle(
					(int)btnPos.X,
					(int)btnPos.Y,
					ConfirmButtonWidth,
					ConfirmButtonHeight
				);
```

Remove the old ZOrder/Position write line:
```csharp
				// REMOVE: if (tr != null) { tr.ZOrder = ConfirmButtonZ; tr.Position = new Vector2(btnRect.X, btnRect.Y); }
```

Update the bounds-only write at the end to use the local variable:
```csharp
				if (ui != null) { ui.Bounds = btnRect; }
```

- [ ] **Step 5: Commit**

```bash
git add ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.cs ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.Draw.cs
git commit -m "refactor: migrate EnemyAttackDisplaySystem to write Position in Update for parallax"
```

---

### Task 3: Verify and Commit

- [ ] **Step 1: Build the project**

Run: `dotnet build`
Expected: Build succeeds with no errors.

- [ ] **Step 2: Run the game and verify parallax behavior**

Run: `dotnet run`

Verify:
- Enemy portrait has subtle parallax movement when moving cursor (should match player portrait behavior)
- Enemy attack banner has subtle parallax movement during Block phase
- Confirm button moves in sync with the banner (same parallax amount)
- Attack nudge animation still works (enemy lunges toward player on attack)
- Absorb tween animation still works (banner shrinks toward enemy after confirm)
- No visual jitter or snapping when entering/leaving battle phases
