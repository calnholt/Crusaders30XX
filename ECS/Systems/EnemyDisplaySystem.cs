using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Enemy Display")]
	public class EnemyDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Microsoft.Xna.Framework.Content.ContentManager _content;
		private Texture2D _demonTexture;
		private float _pulseTimerSeconds;
		private readonly float _pulseDurationSeconds = 0.25f;

		[DebugEditable(DisplayName = "Screen Height Coverage", Step = 0.02f, Min = 0.05f, Max = 1f)]
		public float ScreenHeightCoverage { get; set; } = 0.30f;
		[DebugEditable(DisplayName = "Center Offset X", Step = 5, Min = -2000, Max = 2000)]
		public int CenterOffsetX { get; set; } = 520;
		[DebugEditable(DisplayName = "Center Offset Y", Step = 5, Min = -2000, Max = 2000)]
		public int CenterOffsetY { get; set; } = -100;

		public EnemyDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Microsoft.Xna.Framework.Content.ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			Crusaders30XX.ECS.Core.EventManager.Subscribe<Crusaders30XX.ECS.Events.DebugCommandEvent>(evt =>
			{
				if (evt.Command == "EnemyAbsorbPulse")
				{
					_pulseTimerSeconds = _pulseDurationSeconds;
				}
			});
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Enemy>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (_pulseTimerSeconds > 0f)
			{
				_pulseTimerSeconds = System.Math.Max(0f, _pulseTimerSeconds - (float)gameTime.ElapsedGameTime.TotalSeconds);
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
				var pos = new Vector2(viewportW / 2f + CenterOffsetX, viewportH / 2f + CenterOffsetY);
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
			if (type == EnemyType.Demon)
			{
				if (_demonTexture == null) _demonTexture = _content.Load<Texture2D>("Demon");
				return _demonTexture;
			}
			return null;
		}
	}
}


