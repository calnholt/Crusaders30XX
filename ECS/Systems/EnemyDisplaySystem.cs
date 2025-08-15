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
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Enemy>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

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
				var origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
				var pos = new Vector2(viewportW / 2f + CenterOffsetX, viewportH / 2f + CenterOffsetY);
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


