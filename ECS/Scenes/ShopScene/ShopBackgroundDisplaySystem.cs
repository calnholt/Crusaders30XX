using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Renders the shop background image with cover scaling, anchored to bottom, centered horizontally.
	/// </summary>
	[DebugTab("Shop Background")]
	public class ShopBackgroundDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private Texture2D _background;

		[DebugEditable(DisplayName = "Offset Y", Step = 2, Min = -2000, Max = 2000)]
		public int OffsetY { get; set; } = 0;

		public ShopBackgroundDisplaySystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_content = content;
			TryLoad();
		}

		private void TryLoad()
		{
			try
			{
				_background = _content.Load<Texture2D>("shop_background");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ShopBackgroundDisplaySystem] Failed to load background: {ex.Message}");
				_background = null;
			}
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			yield break;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			if (_background == null) return;

			int viewportW = _graphicsDevice.Viewport.Width;
			int viewportH = _graphicsDevice.Viewport.Height;

			int texW = _background.Width;
			int texH = _background.Height;

			float scaleX = viewportW / (float)texW;
			float scaleY = viewportH / (float)texH;
			float scale = Math.Max(scaleX, scaleY);

			int drawW = (int)Math.Round(texW * scale);
			int drawH = (int)Math.Round(texH * scale);

			int x = (viewportW - drawW) / 2;
			int y = viewportH - drawH + OffsetY;

			var dest = new Rectangle(x, y, drawW, drawH);
			_spriteBatch.Draw(_background, dest, Color.White);
		}
	}
}


