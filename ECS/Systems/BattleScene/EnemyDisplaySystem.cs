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
		[DebugEditable(DisplayName = "Center Offset X", Step = 5, Min = -2000, Max = 2000)]
		public int CenterOffsetX { get; set; } = 520;
		[DebugEditable(DisplayName = "Center Offset Y", Step = 5, Min = -2000, Max = 2000)]
		public int CenterOffsetY { get; set; } = -100;
		[DebugEditable(DisplayName = "Attack Animation Duration (s)", Step = .01f, Min = 0.01f, Max = 2f)]
		public float _attackAnimDuration = 0.5f;

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
					Crusaders30XX.ECS.Core.EventManager.Publish(new EnemyAttackImpactNow { ContextId = _pendingContextId });
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
				var basePos = new Vector2(viewportW / 2f + CenterOffsetX, viewportH / 2f + CenterOffsetY);
				var pos = basePos;
				// Simple smash animation: move toward player then back
				if (_attackAnimTimer > 0f)
				{
					float ta = 1f - (_attackAnimTimer / _attackAnimDuration); // 0->1
					float outPhase = System.Math.Min(0.5f, ta) * 2f; // 0..1 over first half
					float backPhase = System.Math.Max(0f, ta - 0.5f) * 2f; // 0..1 over second half
					// move toward player position plus attack offset, then back
					Vector2 outPos = _attackTargetPos + _attackOffset;
					Vector2 mid = Vector2.Lerp(basePos, outPos, 1f - (float)System.Math.Pow(1f - outPhase, 3));
					pos = Vector2.Lerp(mid, basePos, backPhase);
				}
				// Keep the entity's transform in sync so other systems (e.g., HP bars) can reference it
				t.Position = pos;
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
				_spriteBatch.Draw(tex, position: pos, sourceRectangle: null, color: Color.White, rotation: 0f, origin: origin, scale: scale, effects: SpriteEffects.None, layerDepth: 0f);
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


