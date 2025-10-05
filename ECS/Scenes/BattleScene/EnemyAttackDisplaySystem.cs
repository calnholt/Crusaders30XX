using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Events;
using System.Collections.Generic;
using System;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Skeleton banner that displays current attack name, base damage (sum of on-hit Damage effects),
	/// and a simple list of leaf blocking conditions. Shown when there is a current planned attack.
	/// </summary>
	[DebugTab("Enemy Attack Display")]
	public class EnemyAttackDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private readonly Texture2D _pixel;

		// Animation state
		private string _lastContextId = null;
		private float _shakeElapsedSeconds = 0f;

		// Impact animation flow (spawn centered -> impact)
		private bool _impactActive = false;
		private float _squashElapsedSeconds = 0f;
		private float _flashElapsedSeconds = 0f;
		private float _shockwaveElapsedSeconds = 0f;
		private float _craterElapsedSeconds = 0f;

		// Prevent repeated confirm presses for the same attack context
		private readonly System.Collections.Generic.HashSet<string> _confirmedForContext = new System.Collections.Generic.HashSet<string>();

		private struct DebrisParticle
		{
			public Vector2 Position;
			public Vector2 Velocity;
			public float Age;
			public float Lifetime;
			public float Size;
			public Color Color;
		}
		private readonly System.Collections.Generic.List<DebrisParticle> _debris = new System.Collections.Generic.List<DebrisParticle>();
		private static readonly System.Random _rand = new System.Random();

		// Absorb tween (panel -> enemy)
		[DebugEditable(DisplayName = "Absorb Duration (s)", Step = 0.02f, Min = 0.05f, Max = 3f)]
		public float AbsorbDurationSeconds { get; set; } = 0.4f;
		[DebugEditable(DisplayName = "Absorb Target Y Offset", Step = 2, Min = -400, Max = 400)]
		public int AbsorbTargetYOffset { get; set; } = -40;
		private float _absorbElapsedSeconds = 0f;
		private bool _absorbCompleteFired = false;

		[DebugEditable(DisplayName = "Center Offset X", Step = 2, Min = -1000, Max = 1000)]
		public int OffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Center Offset Y", Step = 2, Min = -400, Max = 400)]
		public int OffsetY { get; set; } = -192;

		[DebugEditable(DisplayName = "Panel Padding", Step = 1, Min = 4, Max = 40)]
		public int PanelPadding { get; set; } = 20;

		[DebugEditable(DisplayName = "Border Thickness", Step = 1, Min = 1, Max = 8)]
		public int BorderThickness { get; set; } = 2;

		[DebugEditable(DisplayName = "Background Alpha", Step = 5, Min = 0, Max = 255)]
		public int BackgroundAlpha { get; set; } = 200;

		[DebugEditable(DisplayName = "Title Scale", Step = 0.05f, Min = 0.3f, Max = 2.5f)]
		public float TitleScale { get; set; } = 0.25f;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.3f, Max = 2.5f)]
		public float TextScale { get; set; } = 0.1375f;

		[DebugEditable(DisplayName = "Line Spacing Extra", Step = 1, Min = 0, Max = 20)]
		public int LineSpacingExtra { get; set; } = 8;

		// Impact animation tuning

		[DebugEditable(DisplayName = "Overshoot Intensity", Step = 0.05f, Min = 0f, Max = 3f)]
		public float OvershootIntensity { get; set; } = 0.8f; // higher = more overshoot in back-ease

		[DebugEditable(DisplayName = "Shake Duration (s)", Step = 0.05f, Min = 0f, Max = 1.5f)]
		public float ShakeDurationSeconds { get; set; } = 0.25f;

		[DebugEditable(DisplayName = "Shake Amplitude (px)", Step = 1, Min = 0, Max = 50)]
		public int ShakeAmplitudePx { get; set; } = 9;

		// (Approach phase removed)

		// Impact squash/flash/shockwave/crater
		[DebugEditable(DisplayName = "Squash Duration (s)", Step = 0.02f, Min = 0.05f, Max = 1f)]
		public float SquashDurationSeconds { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Squash X Factor", Step = 0.05f, Min = 1f, Max = 2.5f)]
		public float SquashXFactor { get; set; } = 1.25f;

		[DebugEditable(DisplayName = "Squash Y Factor", Step = 0.05f, Min = 0.3f, Max = 1f)]
		public float SquashYFactor { get; set; } = 0.8f;

		[DebugEditable(DisplayName = "Impact Flash Duration (s)", Step = 0.02f, Min = 0f, Max = 1f)]
		public float FlashDurationSeconds { get; set; } = 0.12f;

		[DebugEditable(DisplayName = "Impact Flash Max Alpha", Step = 5, Min = 0, Max = 255)]
		public int FlashMaxAlpha { get; set; } = 180;

		[DebugEditable(DisplayName = "Shockwave Duration (s)", Step = 0.02f, Min = 0f, Max = 1.5f)]
		public float ShockwaveDurationSeconds { get; set; } = 0.49f;

		[DebugEditable(DisplayName = "Shockwave Max Expand (px)", Step = 2, Min = 0, Max = 400)]
		public int ShockwaveMaxExpandPx { get; set; } = 132;

		[DebugEditable(DisplayName = "Shockwave Thickness (px)", Step = 1, Min = 1, Max = 20)]
		public int ShockwaveThicknessPx { get; set; } = 6;

		[DebugEditable(DisplayName = "Shockwave Start Alpha", Step = 5, Min = 0, Max = 255)]
		public int ShockwaveStartAlpha { get; set; } = 180;

		[DebugEditable(DisplayName = "Shockwave FadeOut (s)", Step = 0.02f, Min = 0f, Max = 1.5f)]
		public float ShockwaveFadeOutSeconds { get; set; } = 0.07f;

		[DebugEditable(DisplayName = "Crater Duration (s)", Step = 0.02f, Min = 0f, Max = 1.5f)]
		public float CraterDurationSeconds { get; set; } = 0.45f;

		[DebugEditable(DisplayName = "Crater Max Expand (px)", Step = 2, Min = 0, Max = 200)]
		public int CraterMaxExpandPx { get; set; } = 24;

		[DebugEditable(DisplayName = "Crater Max Alpha", Step = 5, Min = 0, Max = 255)]
		public int CraterMaxAlpha { get; set; } = 120;

		// Confirm button tuning
		[DebugEditable(DisplayName = "Confirm Button Offset Y", Step = 2, Min = -600, Max = 600)]
		public int ConfirmButtonOffsetY { get; set; } = 60;
		[DebugEditable(DisplayName = "Confirm Button Width", Step = 2, Min = 20, Max = 600)]
		public int ConfirmButtonWidth { get; set; } = 160;
		[DebugEditable(DisplayName = "Confirm Button Height", Step = 2, Min = 16, Max = 200)]
		public int ConfirmButtonHeight { get; set; } = 44;
		[DebugEditable(DisplayName = "Confirm Button Text Scale", Step = 0.05f, Min = 0.3f, Max = 3f)]
		public float ConfirmButtonTextScale { get; set; } = 0.175f;
		[DebugEditable(DisplayName = "Confirm Button Z", Step = 10, Min = -100000, Max = 100000)]
		public int ConfirmButtonZ { get; set; } = 20000;

		// Debris
		[DebugEditable(DisplayName = "Debris Count", Step = 1, Min = 0, Max = 100)]
		public int DebrisCount { get; set; } = 100;

		[DebugEditable(DisplayName = "Debris Speed Min", Step = 5, Min = 0, Max = 600)]
		public int DebrisSpeedMin { get; set; } = 210;

		[DebugEditable(DisplayName = "Debris Speed Max", Step = 5, Min = 0, Max = 800)]
		public int DebrisSpeedMax { get; set; } = 420;

		[DebugEditable(DisplayName = "Debris Lifetime (s)", Step = 0.05f, Min = 0f, Max = 2f)]
		public float DebrisLifetimeSeconds { get; set; } = 0.8f;

		public EnemyAttackDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_font = font;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
			EventManager.Subscribe<DebugCommandEvent>(evt =>
			{
				if (evt.Command == "ConfirmEnemyAttack")
				{
					System.Console.WriteLine("[EnemyAttackDisplaySystem] DebugCommand ConfirmEnemyAttack received");
					OnConfirmPressed();
				}
			});

			// Clear any transient visuals when leaving Enemy phases
			EventManager.Subscribe<ChangeBattlePhaseEvent>(evt =>
			{
				if (evt.Current != SubPhase.Block && evt.Current != SubPhase.EnemyAttack)
				{
					_impactActive = false;
					_absorbElapsedSeconds = 0f;
					_absorbCompleteFired = false;
					_lastContextId = null;
					_debris.Clear();
				}
			});
		}

		private void OnConfirmPressed()
		{
			// Determine current context id first
			var enemy = GetRelevantEntities().FirstOrDefault();
			var intent = enemy?.GetComponent<AttackIntent>();
			var ctx = intent?.Planned?.FirstOrDefault()?.ContextId;
			if (string.IsNullOrEmpty(ctx)) return;
			// Lock confirm for this context immediately to avoid double presses
			_confirmedForContext.Add(ctx);
			var confirmBtn = EntityManager.GetEntitiesWithComponent<UIButton>().FirstOrDefault(e => e.GetComponent<UIButton>().Command == "ConfirmEnemyAttack");
			if (confirmBtn != null)
			{
				var ui = confirmBtn.GetComponent<UIElement>();
				if (ui != null)
				{
					ui.IsInteractable = false;
					ui.Bounds = new Rectangle(0, 0, 0, 0);
				}
			}
			EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.EnemyAttack, Previous = SubPhase.Block });
			// Enqueue: Discard assigned blocks as the first step
			// Defer resolution/phase to coordinator; enqueue the standard sequence
			EventQueue.EnqueueRule(new QueuedDiscardAssignedBlocksEvent(EntityManager, ctx));
			EventQueue.EnqueueRule(new QueuedResolveAttackEvent(ctx));
			EventQueue.EnqueueRule(new QueuedWaitAbsorbEvent(ctx));
			EventQueue.EnqueueRule(new QueuedStartEnemyAttackAnimation(ctx));
			EventQueue.EnqueueRule(new QueuedWaitImpactEvent(ctx));
			EventQueue.EnqueueRule(new QueuedAdvanceToNextPlannedAttackEvent(EntityManager, ctx));
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<AttackIntent>();
		}

		[DebugAction("Replay Impact Animation")]
		public void Debug_ReplayImpactAnimation()
		{
			// Trigger a fresh impact sequence even if one is currently playing
			_impactActive = true;
			_squashElapsedSeconds = 0f;
			_flashElapsedSeconds = 0f;
			_shockwaveElapsedSeconds = 0f;
			_craterElapsedSeconds = 0f;
			_shakeElapsedSeconds = 0f;
			_debris.Clear();
			SpawnDebris();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime)
		{
			var phaseNow = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>().Sub;
			if (phaseNow == SubPhase.Block) { 
				_absorbElapsedSeconds = 0f;
				_absorbCompleteFired = false;
			}
			var intent = entity.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned.Count == 0)
			{
				_impactActive = false;
				_impactActive = false;
				_lastContextId = null;
				_debris.Clear();
				return;
			}

			var currentContextId = intent.Planned[0].ContextId;
			if (_lastContextId != currentContextId)
			{
				_lastContextId = currentContextId;
				// New context: reset confirm lock for previous and ensure button can show again
				_confirmedForContext.RemoveWhere(id => id != currentContextId);
				// Spawn centered and trigger immediate impact sequence
				_impactActive = true;
				_squashElapsedSeconds = 0f;
				_flashElapsedSeconds = 0f;
				_shockwaveElapsedSeconds = 0f;
				_craterElapsedSeconds = 0f;
				_shakeElapsedSeconds = 0f;
				_debris.Clear();
				SpawnDebris();

				// Specific discard preselection is handled by MarkedForSpecificDiscardSystem
			}

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (_impactActive)
			{
				_squashElapsedSeconds += dt;
				_flashElapsedSeconds += dt;
				_shockwaveElapsedSeconds += dt;
				_craterElapsedSeconds += dt;
				_shakeElapsedSeconds += dt;
				UpdateDebris(dt);
			}
			// Update absorb tween timer based on battle phase
			if (phaseNow == SubPhase.EnemyAttack)
			{
				_absorbElapsedSeconds += dt;
				if (_absorbElapsedSeconds >= AbsorbDurationSeconds)
				{
					EventManager.Publish(new EnemyAbsorbComplete { ContextId = intent.Planned[0].ContextId });
					_absorbCompleteFired = true;
				}
			}
		}

		public void Draw()
		{
			var enemy = GetRelevantEntities().FirstOrDefault();
			var intent = enemy?.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned.Count == 0 || _font == null) return;
			// Only render during enemy phases (Block / EnemyAttack)
			var phaseNowForDraw = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>().Sub;
			if (phaseNowForDraw != SubPhase.Block && phaseNowForDraw != SubPhase.EnemyAttack) return;
			// Gate display during ambush intro
			var ambushState = EntityManager.GetEntitiesWithComponent<AmbushState>().FirstOrDefault()?.GetComponent<AmbushState>();
			if (ambushState != null && ambushState.IsActive && ambushState.IntroActive) return;
			if (_absorbCompleteFired) return;

			var pa = intent.Planned[0];
			var def = LoadAttackDefinition(pa.AttackId);
			if (def == null) return;

			int extraNotBlockedDamage = (def.effectsOnNotBlocked ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount);

			// Summarize effects that also happen when NOT blocked (in addition to on-hit)
			string notBlockedSummary = SummarizeEffects(def.effectsOnNotBlocked, pa.ContextId);

			// Compose lines: Name, Damage (final + prevented breakdown), and Leaf conditions (with live status)
			var lines = new System.Collections.Generic.List<(string text, float scale, Color color)>();
			lines.Add((def.name, TitleScale, Color.White));
			var progress = FindEnemyAttackProgress(pa.ContextId);
			bool isConditionMet = progress.IsConditionMet;
			int actual = progress.ActualDamage;
			int prevented = progress.AegisTotal;
			int baseDamage = progress.BaseDamage;
			int additionalConditionalDamage = progress.AdditionalConditionalDamageTotal;
			int damageDisplay = Math.Max(0, baseDamage - prevented - progress.AssignedBlockTotal);
			lines.Add(($"Damage: {damageDisplay}{(!isConditionMet && additionalConditionalDamage > 0 ? $" + {additionalConditionalDamage}" : "")} (preventing {progress.TotalPreventedDamage})", TextScale, Color.White));
			if (!string.IsNullOrEmpty(notBlockedSummary))
			{
				lines.Add(($"On not blocked: {notBlockedSummary}", TextScale, Color.OrangeRed));
			}
			AppendLeafConditionsWithStatus(def.conditionsBlocked, pa.ContextId, enemy, lines);

			// Measure and draw a simple panel in the center
			int pad = System.Math.Max(0, PanelPadding);
			float maxW = 0f;
			float totalH = 0f;
			foreach (var (text, lineScale, _) in lines)
			{
				var sz = _font.MeasureString(text);
				maxW = System.Math.Max(maxW, sz.X * lineScale);
				totalH += sz.Y * lineScale + LineSpacingExtra;
			}
			int w = (int)System.Math.Ceiling(maxW) + pad * 2;
			int h = (int)System.Math.Ceiling(totalH) + pad * 2;
			int vx = _graphicsDevice.Viewport.Width;
			int vy = _graphicsDevice.Viewport.Height;

			var center = new Vector2(vx / 2f + OffsetX, vy / 2f + OffsetY);
			Vector2 approachPos = center;
			float panelScale = 1f;
			// During processing, tween panel toward enemy center and scale down to 0
			var phaseNow = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>().Sub;
			if (phaseNow == SubPhase.EnemyAttack)
			{
				var enemyT = enemy?.GetComponent<Transform>();
				var dur = System.Math.Max(0.05f, AbsorbDurationSeconds);
				float tTween = MathHelper.Clamp(_absorbElapsedSeconds / dur, 0f, 1f);
				float ease = 1f - (float)System.Math.Pow(1f - tTween, 3);
				var targetPos = enemyT.Position + new Vector2(0, AbsorbTargetYOffset);
				approachPos = Vector2.Lerp(center, targetPos, ease);
				panelScale = MathHelper.Lerp(1f, 0f, ease);
			}
			int bgAlpha = System.Math.Clamp(BackgroundAlpha, 0, 255);

			// Impact phase visuals: squash/stretch + shake/flash/shockwave/crater
			Vector2 shake = Vector2.Zero;
			float squashX = 1f;
			float squashY = 1f;
			float contentScale = 1f;
			if (_impactActive)
			{
				float t = System.Math.Clamp(_squashElapsedSeconds / System.Math.Max(0.0001f, SquashDurationSeconds), 0f, 1f);
				// easeOutBack-like return towards 1 with overshoot feel
				float back = 1f + (OvershootIntensity) * (float)System.Math.Pow(1f - t, 3);
				squashX = MathHelper.Lerp(SquashXFactor, 1f, t) * back;
				squashY = MathHelper.Lerp(SquashYFactor, 1f, t) / back;
				// Scale content with the squash so text remains inside the panel
				contentScale = System.Math.Min(squashX, squashY);
				if (_shakeElapsedSeconds < ShakeDurationSeconds && ShakeAmplitudePx > 0)
				{
					float shakeT = 1f - System.Math.Clamp(_shakeElapsedSeconds / System.Math.Max(0.0001f, ShakeDurationSeconds), 0f, 1f);
					int sx = _rand.Next(-ShakeAmplitudePx, ShakeAmplitudePx + 1);
					int sy = _rand.Next(-ShakeAmplitudePx, ShakeAmplitudePx + 1);
					shake = new Vector2(sx, sy) * shakeT;
				}
			}

			int drawW = (int)System.Math.Round(w * panelScale * squashX);
			int drawH = (int)System.Math.Round(h * panelScale * squashY);
			var rect = new Rectangle((int)(approachPos.X - drawW / 2f + shake.X), (int)(approachPos.Y - drawH / 2f + shake.Y), drawW, drawH);
			_spriteBatch.Draw(_pixel, rect, new Color(20, 20, 20, bgAlpha));
			DrawRect(rect, Color.White, System.Math.Max(1, BorderThickness));

			// Impact flash overlay
			if (_impactActive && _flashElapsedSeconds < FlashDurationSeconds && FlashMaxAlpha > 0)
			{
				float ft = 1f - System.Math.Clamp(_flashElapsedSeconds / System.Math.Max(0.0001f, FlashDurationSeconds), 0f, 1f);
				int fa = (int)(FlashMaxAlpha * ft);
				_spriteBatch.Draw(_pixel, rect, new Color(255, 255, 255, System.Math.Clamp(fa, 0, 255)));
			}

			// Crater (darkened expanding rect)
			if (_impactActive && _craterElapsedSeconds < CraterDurationSeconds && CraterMaxAlpha > 0)
			{
				float ct = System.Math.Clamp(_craterElapsedSeconds / System.Math.Max(0.0001f, CraterDurationSeconds), 0f, 1f);
				int cexp = (int)System.Math.Round(CraterMaxExpandPx * ct);
				int ca = (int)System.Math.Round(CraterMaxAlpha * (1f - ct));
				var craterRect = new Rectangle(rect.X - cexp, rect.Y - cexp, rect.Width + cexp * 2, rect.Height + cexp * 2);
				_spriteBatch.Draw(_pixel, craterRect, new Color(10, 10, 10, System.Math.Clamp(ca, 0, 255)));
			}

			// Shockwave ring (draw after crater so it remains visible while fading)
			if (_impactActive && _shockwaveElapsedSeconds < (ShockwaveDurationSeconds + ShockwaveFadeOutSeconds) && ShockwaveThicknessPx > 0 && ShockwaveMaxExpandPx > 0)
			{
				float expandT = System.Math.Clamp(_shockwaveElapsedSeconds / System.Math.Max(0.0001f, ShockwaveDurationSeconds), 0f, 1f);
				int expand = (int)System.Math.Round(ShockwaveMaxExpandPx * expandT);
				float totalDuration = ShockwaveDurationSeconds + ShockwaveFadeOutSeconds;
				float totalT = System.Math.Clamp(_shockwaveElapsedSeconds / System.Math.Max(0.0001f, totalDuration), 0f, 1f);
				int alpha = (int)System.Math.Round(ShockwaveStartAlpha * (1f - totalT));
				alpha = System.Math.Clamp(alpha, 0, 255);
				float aNorm = alpha / 255f;
				var premulColor = new Color((int)System.Math.Round(255f * aNorm), (int)System.Math.Round(255f * aNorm), (int)System.Math.Round(255f * aNorm), alpha);
				DrawRing(new Rectangle(rect.X - expand, rect.Y - expand, rect.Width + expand * 2, rect.Height + expand * 2), premulColor, System.Math.Max(1, ShockwaveThicknessPx));
			}

			// Debris
			if (_impactActive && _debris.Count > 0)
			{
				var debrisBase = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
				for (int i = 0; i < _debris.Count; i++)
				{
					var d = _debris[i];
					if (d.Age <= d.Lifetime)
					{
						int ds = (int)System.Math.Max(1, d.Size);
						var p = new Rectangle((int)(debrisBase.X + d.Position.X + shake.X), (int)(debrisBase.Y + d.Position.Y + shake.Y), ds, ds);
						_spriteBatch.Draw(_pixel, p, d.Color);
					}
				}
			}

			// Content
			float y = rect.Y + pad * panelScale * contentScale;
			foreach (var (text, baseScale, color) in lines)
			{
				float s = baseScale * panelScale * contentScale;
				_spriteBatch.DrawString(_font, text, new Vector2(rect.X + pad * panelScale * contentScale, y), color, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
				var sz = _font.MeasureString(text);
				y += sz.Y * s + LineSpacingExtra * panelScale * contentScale;
			}

			// Confirm button below panel (only show in Block phase)
			
			bool showConfirm = phaseNow == SubPhase.Block && !_confirmedForContext.Contains(pa.ContextId);
			if (showConfirm)
			{
				var btnRect = new Rectangle(
					(int)(rect.X + rect.Width / 2f - ConfirmButtonWidth / 2f),
					rect.Bottom + ConfirmButtonOffsetY,
					ConfirmButtonWidth,
					ConfirmButtonHeight
				);
				_spriteBatch.Draw(_pixel, btnRect, new Color(40, 120, 40, 220));
				DrawRect(btnRect, Color.White, 2);
				if (_font != null)
				{
					string label = "Confirm";
					var size = _font.MeasureString(label) * ConfirmButtonTextScale;
					var posText = new Vector2(btnRect.Center.X - size.X / 2f, btnRect.Center.Y - size.Y / 2f);
					_spriteBatch.DrawString(_font, label, posText, Color.White, 0f, Vector2.Zero, ConfirmButtonTextScale, SpriteEffects.None, 0f);
				}

				// Ensure a single clickable UI entity exists and stays in sync
				var confirmBtns = EntityManager.GetEntitiesWithComponent<UIButton>()
					.Where(e => e.GetComponent<UIButton>().Command == "ConfirmEnemyAttack")
					.ToList();
				Entity primaryBtn = confirmBtns.FirstOrDefault();
				// Destroy any extras to avoid ghost clickables
				for (int i = 1; i < confirmBtns.Count; i++)
				{
					EntityManager.DestroyEntity(confirmBtns[i].Id);
				}
				if (primaryBtn == null)
				{
					primaryBtn = EntityManager.CreateEntity("UIButton_ConfirmEnemyAttack");
					EntityManager.AddComponent(primaryBtn, new UIButton { Label = "Confirm", Command = "ConfirmEnemyAttack" });
					EntityManager.AddComponent(primaryBtn, new Transform { Position = new Vector2(btnRect.X, btnRect.Y), ZOrder = ConfirmButtonZ });
					EntityManager.AddComponent(primaryBtn, new UIElement { Bounds = btnRect, IsInteractable = true });
				}
				else
				{
					var ui = primaryBtn.GetComponent<UIElement>();
					var tr = primaryBtn.GetComponent<Transform>();
					if (ui != null) { ui.Bounds = btnRect; ui.IsInteractable = true; }
					if (tr != null) { tr.ZOrder = ConfirmButtonZ; tr.Position = new Vector2(btnRect.X, btnRect.Y); }
				}
			}
			else
			{
				// Hide and destroy confirm buttons when not visible
				var confirmBtns = EntityManager.GetEntitiesWithComponent<UIButton>()
					.Where(e => e.GetComponent<UIButton>().Command == "ConfirmEnemyAttack")
					.ToList();
				foreach (var b in confirmBtns)
				{
					EntityManager.DestroyEntity(b.Id);
				}
			}

			// Update banner anchor transform at center-bottom of rect
			var anchorEntity = EntityManager.GetEntitiesWithComponent<Crusaders30XX.ECS.Components.EnemyAttackBannerAnchor>().FirstOrDefault();
			if (anchorEntity == null)
			{
				anchorEntity = EntityManager.CreateEntity("EnemyAttackBannerAnchor");
				EntityManager.AddComponent(anchorEntity, new Crusaders30XX.ECS.Components.EnemyAttackBannerAnchor());
				EntityManager.AddComponent(anchorEntity, new Transform());
			}
			var anchorTransform = anchorEntity.GetComponent<Transform>();
			if (anchorTransform != null)
			{
				anchorTransform.Position = new Vector2(rect.X + rect.Width / 2f, rect.Bottom);
				anchorTransform.Scale = Vector2.One;
				anchorTransform.Rotation = 0f;
			}
		}

		private AttackDefinition LoadAttackDefinition(string id)
		{
			AttackDefinitionCache.TryGet(id, out var def);
			return def;
		}



		private EnemyAttackProgress FindEnemyAttackProgress(string contextId)
		{
			foreach (var e in EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>())
			{
				var p = e.GetComponent<EnemyAttackProgress>();
				if (p != null && p.ContextId == contextId) return p;
			}
			return null;
		}

		private void AppendLeafConditionsWithStatus(ConditionNode node, string contextId, Entity attacker, List<(string text, float scale, Color color)> lines)
		{
			if (node == null) return;
			if (node.kind == "Leaf")
			{
				if (!string.IsNullOrEmpty(node.leafType))
				{
					bool satisfied = ConditionService.Evaluate(node, contextId, EntityManager, attacker, null);
					Color statusColor = satisfied ? Color.LimeGreen : Color.IndianRed;
					if (node.leafType == "PlayColorAtLeastN" || node.leafType == "PlayAtLeastN")
					{
						var color = node.@params != null && node.@params.TryGetValue("color", out var c) ? c : null;
						var n = node.@params != null && node.@params.TryGetValue("n", out var nStr) ? nStr : "?";
						lines.Add(($"Condition: Block with {n} {color ?? "card"}", TextScale, statusColor));
					}
					else if (node.leafType == "OnHit")
					{
						lines.Add(("Condition: Fully block the attack", TextScale, statusColor));
					}
					else
					{
						lines.Add(($"Condition: {node.leafType}", TextScale, statusColor));
					}
				}
				return;
			}
			if (node.children != null)
			{
				foreach (var c in node.children)
				{
					AppendLeafConditionsWithStatus(c, contextId, attacker, lines);
				}
			}
		}

        

		private string SummarizeEffects(EffectDefinition[] effects, string contextId)
		{
			if (effects == null || effects.Length == 0) return string.Empty;
			var parts = new System.Collections.Generic.List<string>();
			foreach (var e in effects)
			{
				switch (e.type)
				{
					case "Damage":
						if (e.percentage != 100)
						{
							parts.Add($"{e.percentage}% chance for +{e.amount} damage");
						}
						else
						{
							parts.Add($"+{e.amount} damage");
						}
					
						break;
					case "Burn":
						parts.Add($"Gain {e.amount} burn stacks");
						break;
					case "LoseCourage":
						parts.Add($"Lose {e.amount} courage");
						break;
					case "DiscardSpecificCard":
						{
                            var markedCards = EntityManager.GetEntitiesWithComponent<MarkedForSpecificDiscard>()
                                .Where(e => e.GetComponent<MarkedForSpecificDiscard>().ContextId == contextId)
                                .Select(e =>
                                {
                                    var cd = e.GetComponent<CardData>();
                                    if (cd == null) return "Card";
                                    try
                                    {
                                        if (Crusaders30XX.ECS.Data.Cards.CardDefinitionCache.TryGet(cd.CardId ?? string.Empty, out var def) && def != null)
                                        {
                                            return def.name ?? def.id ?? "Card";
                                        }
                                    }
                                    catch { }
                                    return cd.CardId ?? "Card";
                                })
                                .ToList();
							parts.Add($"Discard: {string.Join(", ", markedCards)}");
							break;
						}
					case "Slow":
						parts.Add($"Gain {e.amount} slow stacks");
						break;
					case "Penance":
						parts.Add($"Gain {e.amount} penance");
						break;
					case "Armor":
						parts.Add($"Gain {e.amount} armor");
						break;
					case "Wounded":
						parts.Add($"Gain {e.amount} wounded");
						break;
					default:
						parts.Add(e.type);
						break;
				}
			}
			return string.Join(", ", parts);
		}

		private void DrawRect(Rectangle rect, Color color, int thickness)
		{
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
		}

		private void DrawRing(Rectangle rect, Color color, int thickness)
		{
			// Outer rectangle
			DrawRect(rect, color, thickness);
			// Inner rectangle carve-out by overdrawing with background alpha (simulate ring)
			var inner = new Rectangle(rect.X + thickness, rect.Y + thickness, System.Math.Max(0, rect.Width - thickness * 2), System.Math.Max(0, rect.Height - thickness * 2));
			if (inner.Width > 0 && inner.Height > 0)
			{
				_spriteBatch.Draw(_pixel, inner, new Color(0, 0, 0, 0));
			}
		}

		private void SpawnDebris()
		{
			_debris.Clear();
			var rand = _rand;
			for (int i = 0; i < DebrisCount; i++)
			{
				float ang = (float)(rand.NextDouble() * System.Math.PI * 2);
				float spd = rand.Next(DebrisSpeedMin, DebrisSpeedMax + 1);
				var vel = new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * spd;
				_debris.Add(new DebrisParticle
				{
					Position = Vector2.Zero, // will be positioned at draw time around the rect center
					Velocity = vel,
					Age = 0f,
					Lifetime = DebrisLifetimeSeconds * (0.6f + (float)rand.NextDouble() * 0.8f),
					Size = 2 + (float)rand.NextDouble() * 3f,
					Color = new Color(230, 230, 230, 200)
				});
			}
		}

		private void UpdateDebris(float dt)
		{
			for (int i = 0; i < _debris.Count; i++)
			{
				var d = _debris[i];
				d.Age += dt;
				d.Position += d.Velocity * dt;
				float lifeT = System.Math.Clamp(d.Age / System.Math.Max(0.0001f, d.Lifetime), 0f, 1f);
				int a = (int)(200 * (1f - lifeT));
				d.Color = new Color(d.Color.R, d.Color.G, d.Color.B, System.Math.Clamp(a, 0, 255));
				_debris[i] = d;
			}
		}

	}
}


