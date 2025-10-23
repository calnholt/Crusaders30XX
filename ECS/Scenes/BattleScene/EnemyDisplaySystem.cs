using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Enemy Display")]
	public class EnemyDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private Texture2D _enemyTexture;
		private float _pulseTimerSeconds;
		private readonly float _pulseDurationSeconds = 0.25f;
		private Vector2 _attackOffset = new Vector2(-80f, -20f);
		private Vector2 _attackTargetPos;

		[DebugEditable(DisplayName = "Screen Height Coverage", Step = 0.02f, Min = 0.05f, Max = 1f)]
		public float ScreenHeightCoverage { get; set; } = 0.30f;
		[DebugEditable(DisplayName = "Center Offset X (% of width)", Step = 0.01f, Min = -1.0f, Max = 1.0f)]
		public float CenterOffsetXPct { get; set; } = 0.3f; // positive = right, negative = left
		[DebugEditable(DisplayName = "Center Offset Y (% of height)", Step = 0.01f, Min = -1.0f, Max = 1.0f)]
		public float CenterOffsetYPct { get; set; } = -0.10f; // positive = down, negative = up
		[DebugEditable(DisplayName = "Attack Animation Duration (s)", Step = .01f, Min = 0.01f, Max = 2f)]
		public float _attackAnimDuration = 0.2f;
		[DebugEditable(DisplayName = "Attack Nudge Distance (px)", Step = 1f, Min = 0f, Max = 200f)]
		public float AttackNudgePixels { get; set; } = 36f;

		public EnemyDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			EventManager.Subscribe<DebugCommandEvent>(evt =>
			{
				if (evt.Command == "EnemyAbsorbPulse")
				{
					_pulseTimerSeconds = _pulseDurationSeconds;
					System.Console.WriteLine("[EnemyDisplaySystem] DebugCommand EnemyAbsorbPulse received");
				}
			});
			EventManager.Subscribe<StartEnemyAttackAnimation>(evt =>
			{
				// Start a brief attack animation timer; on completion, signal impact
				_attackAnimTimer = _attackAnimDuration;
				_pendingContextId = evt.ContextId;
				System.Console.WriteLine($"[EnemyDisplaySystem] StartEnemyAttackAnimation context={evt.ContextId}");
				// Capture current player position as target (find Player Transform)
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				var pt = player?.GetComponent<Transform>();
				_attackTargetPos = pt?.Position ?? Vector2.Zero;
			});
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Enemy>();
		}

		private float _attackAnimTimer;
		private string _pendingContextId;

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (_pulseTimerSeconds > 0f)
			{
				_pulseTimerSeconds = System.Math.Max(0f, _pulseTimerSeconds - (float)gameTime.ElapsedGameTime.TotalSeconds);
			}
			if (_attackAnimTimer > 0f)
			{
				_attackAnimTimer = System.Math.Max(0f, _attackAnimTimer - (float)gameTime.ElapsedGameTime.TotalSeconds);
				if (_attackAnimTimer == 0f && !string.IsNullOrEmpty(_pendingContextId))
				{
                    EventManager.Publish(new EnemyAttackImpactNow { ContextId = _pendingContextId });
					_pendingContextId = null;
				}
			}
		}

		public void Draw()
		{
			foreach (var e in GetRelevantEntities())
			{
				var enemy = e.GetComponent<Enemy>();
				var t = e.GetComponent<Transform>();
				if (enemy == null || t == null) continue;
				Texture2D tex = GetTextureFor(enemy.Type);
				if (tex == null) continue;
				int viewportW = _graphicsDevice.Viewport.Width;
				int viewportH = _graphicsDevice.Viewport.Height;
				float desiredHeight = ScreenHeightCoverage * viewportH;
				float scale = desiredHeight / tex.Height;
				if (_pulseTimerSeconds > 0f)
				{
					float tp = 1f - (_pulseTimerSeconds / _pulseDurationSeconds);
					float bump = 1f + 0.15f * (float)System.Math.Sin(tp * System.Math.PI);
					scale *= bump;
				}
				var origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
				var basePos = new Vector2(
					viewportW * (0.5f + CenterOffsetXPct),
					viewportH * (0.5f + CenterOffsetYPct)
				);
				var posForAnim = basePos;
				// Simple smash animation: move toward player then back
				if (_attackAnimTimer > 0f)
				{
					float ta = 1f - (_attackAnimTimer / _attackAnimDuration); // 0->1
					float outPhase = System.Math.Min(0.5f, ta) * 2f; // 0..1 over first half
					float backPhase = System.Math.Max(0f, ta - 0.5f) * 2f; // 0..1 over second half
					// move a short nudge toward player direction, then back
					Vector2 desired = _attackTargetPos + _attackOffset;
					Vector2 dir = desired - basePos;
					if (dir.LengthSquared() > 0.0001f)
					{
						dir = Vector2.Normalize(dir);
					}
					else
					{
						dir = Vector2.Normalize(_attackOffset);
					}
					Vector2 outPos = basePos + dir * AttackNudgePixels;
					Vector2 mid = Vector2.Lerp(basePos, outPos, 1f - (float)System.Math.Pow(1f - outPhase, 3));
					posForAnim = Vector2.Lerp(mid, basePos, backPhase);
				}
				// Keep the entity's transform base position stable for parallax and UI
				t.BasePosition = basePos;
				// Share scale and texture dims for accurate HP positioning if needed
				var info = e.GetComponent<PortraitInfo>();
				if (info == null)
				{
					return;
				}
				info.TextureWidth = tex.Width;
				info.TextureHeight = tex.Height;
				info.CurrentScale = scale;
				info.BaseScale = desiredHeight / tex.Height;
				// Draw at Transform.Position plus the attack nudge offset (immediate, not smoothed by parallax)
				var drawPos = t.Position + (posForAnim - basePos);
				// Update UI bounds so hover/tooltip works over the enemy portrait
				var ui = e.GetComponent<UIElement>();
				if (ui != null)
				{
					int wPx = (int)System.Math.Round(tex.Width * scale);
					int hPx = (int)System.Math.Round(tex.Height * scale);
					int x0 = (int)System.Math.Round(drawPos.X - wPx / 2f);
					int y0 = (int)System.Math.Round(drawPos.Y - hPx / 2f);
					ui.Bounds = new Rectangle(x0, y0, wPx, hPx);
				}
				_spriteBatch.Draw(tex, position: drawPos, sourceRectangle: null, color: Color.White, rotation: 0f, origin: origin, scale: scale, effects: SpriteEffects.None, layerDepth: 0f);
			}
		}

		private Texture2D GetTextureFor(EnemyType type)
		{
			var queuedEntity = EntityManager.GetEntity("QueuedEvents");
			var queued = queuedEntity.GetComponent<QueuedEvents>();
			_enemyTexture = _content.Load<Texture2D>(queued.Events[queued.CurrentIndex].EventId);
			return _enemyTexture;
		}
	}
}


