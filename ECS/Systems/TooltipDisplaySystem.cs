using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Draws simple black-background, white-text tooltips for hovered UI elements that have a Tooltip component.
	/// </summary>
	public class TooltipDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private readonly Texture2D _pixel;

		public TooltipDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_font = font;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<UIElement>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			if (_font == null) return;
			// Only show for top-most hovered element(s)
			var hoverables = GetRelevantEntities()
				.Select(e => new { E = e, UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>() })
				.Where(x => x.UI?.Tooltip != null && x.UI.IsHovered && !string.IsNullOrWhiteSpace(x.UI.Tooltip))
				.OrderByDescending(x => x.T?.ZOrder ?? 0)
				.ToList();
			var top = hoverables.FirstOrDefault();
			if (top == null) return;

			string text = top.UI.Tooltip;
			float pad = 8f;
			var size = _font.MeasureString(text);
			var rect = new Rectangle(top.UI.Bounds.X, top.UI.Bounds.Y - (int)(size.Y + pad * 2), (int)(size.X + pad * 2), (int)(size.Y + pad * 2));
			// Screen clamp
			rect.X = System.Math.Max(0, System.Math.Min(rect.X, _graphicsDevice.Viewport.Width - rect.Width));
			rect.Y = System.Math.Max(0, System.Math.Min(rect.Y, _graphicsDevice.Viewport.Height - rect.Height));

			_spriteBatch.Draw(_pixel, rect, new Color(0, 0, 0, 220));
			// Border
			DrawBorder(rect, Color.White, 2);
			// Text
			var textPos = new Vector2(rect.X + pad, rect.Y + pad);
			_spriteBatch.DrawString(_font, text, textPos, Color.White);
		}

		private void DrawBorder(Rectangle r, Color color, int thickness)
		{
			_spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);
			_spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
			_spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
			_spriteBatch.Draw(_pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
		}
	}
}


