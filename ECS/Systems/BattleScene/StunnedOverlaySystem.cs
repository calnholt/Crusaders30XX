using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	public class StunnedOverlaySystem : Core.System
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteFont _font;
		private float _timer;

		public StunnedOverlaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_font = font;
			EventManager.Subscribe<ShowStunnedOverlay>(e => { _timer = 1.0f; });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Enemy>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			_timer = System.Math.Max(0f, _timer - (float)gameTime.ElapsedGameTime.TotalSeconds);
		}

		public void Draw()
		{
			if (_timer <= 0f || _font == null) return;
			var enemy = GetRelevantEntities().FirstOrDefault();
			if (enemy == null) return;
			var t = enemy.GetComponent<Transform>();
			var info = enemy.GetComponent<PortraitInfo>();
			if (t == null || info == null) return;
			string s = "Stunned!";
			var size = _font.MeasureString(s) * 0.8f;
			var textPos = new Vector2(t.Position.X - size.X / 2f, t.Position.Y - info.TextureHeight * info.CurrentScale / 2f - 40);
			_spriteBatch.DrawString(_font, s, textPos + new Vector2(2, 2), Color.Black * 0.6f, 0f, Vector2.Zero, 0.2f, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_font, s, textPos, Color.White, 0f, Vector2.Zero, 0.2f, SpriteEffects.None, 0f);
		}
	}
}


