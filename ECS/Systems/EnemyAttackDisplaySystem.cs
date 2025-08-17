using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;

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
		private bool _justImpacted = false;
		private float _squashElapsedSeconds = 0f;
		private float _flashElapsedSeconds = 0f;
		private float _shockwaveElapsedSeconds = 0f;
		private float _craterElapsedSeconds = 0f;

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
		public float TitleScale { get; set; } = 1f;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.3f, Max = 2.5f)]
		public float TextScale { get; set; } = 0.55f;

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
			_justImpacted = true;
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
			var intent = entity.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned.Count == 0)
			{
				_impactActive = false;
				_impactActive = false;
				_justImpacted = false;
				_lastContextId = null;
				_debris.Clear();
				return;
			}

			var currentContextId = intent.Planned[0].ContextId;
			if (_lastContextId != currentContextId)
			{
				_lastContextId = currentContextId;
				// Spawn centered and trigger immediate impact sequence
				_impactActive = true;
				_justImpacted = true;
				_squashElapsedSeconds = 0f;
				_flashElapsedSeconds = 0f;
				_shockwaveElapsedSeconds = 0f;
				_craterElapsedSeconds = 0f;
				_shakeElapsedSeconds = 0f;
				_debris.Clear();
				SpawnDebris();
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
				if (_squashElapsedSeconds > SquashDurationSeconds && _flashElapsedSeconds > FlashDurationSeconds && _shockwaveElapsedSeconds > ShockwaveDurationSeconds)
				{
					_justImpacted = false;
				}
			}
		}

		public void Draw()
		{
			var enemy = GetRelevantEntities().FirstOrDefault();
			var intent = enemy?.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned.Count == 0 || _font == null) return;

			var pa = intent.Planned[0];
			var def = LoadAttackDefinition(pa.AttackId);
			if (def == null) return;

			int baseDamage = DamagePredictionService.ComputeFullDamage(def);
			int extraNotBlockedDamage = (def.effectsOnNotBlocked ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount);

			// Summarize effects that also happen when NOT blocked (in addition to on-hit)
			string notBlockedSummary = SummarizeEffects(def.effectsOnNotBlocked);

			// Compose lines: Name, Damage (final + prevented breakdown), and Leaf conditions (with live status)
			var lines = new System.Collections.Generic.List<(string text, float scale, Color color)>();
			lines.Add((def.name, TitleScale, Color.White));
			bool isBlocked = ConditionService.Evaluate(def.conditionsBlocked, pa.ContextId, EntityManager, enemy, null);
			int actual = DamagePredictionService.ComputeActualDamage(def, EntityManager, pa.ContextId);
			int prevented = DamagePredictionService.ComputePreventedDamage(def, EntityManager, pa.ContextId, isBlocked);
			lines.Add(($"Damage: {actual} (preventing {prevented})", TextScale, Color.White));
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
		}

		private AttackDefinition LoadAttackDefinition(string id)
		{
			Crusaders30XX.ECS.Data.Attacks.AttackDefinitionCache.TryGet(id, out var def);
			return def;
		}

		private void AppendLeafConditionsWithStatus(ConditionNode node, string contextId, Entity attacker, System.Collections.Generic.List<(string text, float scale, Color color)> lines)
		{
			if (node == null) return;
			if (node.kind == "Leaf")
			{
				if (!string.IsNullOrEmpty(node.leafType))
				{
					bool satisfied = ConditionService.Evaluate(node, contextId, EntityManager, attacker, null);
					Color statusColor = satisfied ? Color.LimeGreen : Color.IndianRed;
					if (node.leafType == "PlayColorAtLeastN")
					{
						var color = node.@params != null && node.@params.TryGetValue("color", out var c) ? c : "?";
						var n = node.@params != null && node.@params.TryGetValue("n", out var nStr) ? nStr : "?";
						lines.Add(($"Condition: Play {n} {color}", TextScale, statusColor));
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

        

		private static string SummarizeEffects(EffectDefinition[] effects)
		{
			if (effects == null || effects.Length == 0) return string.Empty;
			var parts = new System.Collections.Generic.List<string>();
			foreach (var e in effects)
			{
				switch (e.type)
				{
					case "Damage":
						parts.Add($"Damage {e.amount}");
						break;
					case "ApplyStatus":
						var st = string.IsNullOrEmpty(e.status) ? "Status" : e.status;
						var stacks = e.stacks > 0 ? e.stacks.ToString() : "1";
						parts.Add($"{st}({stacks})");
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

		private static float EaseOutBack(float start, float end, float t, float overshoot)
		{
			// Standard easeOutBack on [0,1], remapped to [start,end]
			float c1 = overshoot;
			float c3 = c1 + 1f;
			float eased = 1f + c3 * (float)System.Math.Pow(t - 1f, 3) + c1 * (float)System.Math.Pow(t - 1f, 2);
			return start + (end - start) * eased;
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


