using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Displays the player's Temperance as a white triangle with a black border and centered black text,
	/// positioned next to the Courage display using the portrait anchor for reference.
	/// </summary>
	[DebugTab("Temperance Display")]
	public class TemperanceDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private readonly System.Collections.Generic.Dictionary<int, Texture2D> _triangleTextures = new();

		// Debug-adjustable fields
		[DebugEditable(DisplayName = "Triangle Size", Step = 1, Min = 8, Max = 512)]
		public int TriangleSize { get; set; } = 50; // pixels (width == height)

		[DebugEditable(DisplayName = "Outline Thickness", Step = 1, Min = 1, Max = 50)]
		public int OutlineThickness { get; set; } = 2;

		[DebugEditable(DisplayName = "Anchor Offset X", Step = 2, Min = -2000, Max = 2000)]
		public int AnchorOffsetX { get; set; } = 60; // to the right of Courage by default

		[DebugEditable(DisplayName = "Anchor Offset Y", Step = 2, Min = -2000, Max = 2000)]
		public int AnchorOffsetY { get; set; } = 208; // align with Courage default

		[DebugEditable(DisplayName = "Text Scale Divisor", Step = 1, Min = 1, Max = 200)]
		public int TextScaleDivisor { get; set; } = 24;

		[DebugEditable(DisplayName = "Text Offset X", Step = 1, Min = -500, Max = 500)]
		public int TextOffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Text Offset Y", Step = 1, Min = -500, Max = 500)]
		public int TextOffsetY { get; set; } = 0;

		public TemperanceDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_font = font;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Player>().Where(e => e.HasComponent<Temperance>());
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var player = GetRelevantEntities().FirstOrDefault();
			if (player == null) return;
			var temperance = player.GetComponent<Temperance>();
			if (temperance == null) return;

			var anchor = EntityManager.GetEntitiesWithComponent<PlayerPortraitAnchor>().FirstOrDefault();
			if (anchor == null) return;
			var t = anchor.GetComponent<Transform>();
			var info = anchor.GetComponent<PlayerPortraitInfo>();
			if (t == null || info == null || _font == null) return;

			int sizeOuter = Math.Max(8, TriangleSize);
			int outline = Math.Max(1, OutlineThickness);
			int sizeInner = Math.Max(4, sizeOuter - outline * 2);

			// Position next to Courage (to the right by default)
			var center = new Vector2(t.Position.X + AnchorOffsetX, t.Position.Y + AnchorOffsetY);

			var triOuter = GetOrCreateTriangle(sizeOuter);
			var triInner = GetOrCreateTriangle(sizeInner);
			if (triOuter == null || triInner == null) return;

			// Outer border (black)
			_spriteBatch.Draw(
				triOuter,
				position: center,
				sourceRectangle: null,
				color: Color.Black,
				rotation: 0f,
				origin: new Vector2(sizeOuter / 2f, sizeOuter / 2f),
				scale: Vector2.One,
				effects: SpriteEffects.None,
				layerDepth: 0f
			);

			// Inner fill (white)
			_spriteBatch.Draw(
				triInner,
				position: center,
				sourceRectangle: null,
				color: Color.White,
				rotation: 0f,
				origin: new Vector2(sizeInner / 2f, sizeInner / 2f),
				scale: Vector2.One,
				effects: SpriteEffects.None,
				layerDepth: 0f
			);

			// Centered value text (black)
			string text = Math.Max(0, temperance.Amount).ToString();
			float textScale = Math.Min(1.0f, sizeInner / Math.Max(1f, (float)TextScaleDivisor));
			var size = _font.MeasureString(text) * textScale;
			var pos = new Vector2(center.X - size.X / 2f + TextOffsetX, center.Y - size.Y / 2f + TextOffsetY);
			_spriteBatch.DrawString(_font, text, pos, Color.Black, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
		}

		private Texture2D GetOrCreateTriangle(int size)
		{
			if (size <= 0) return null;
			if (_triangleTextures.TryGetValue(size, out var tex) && tex != null) return tex;
			tex = CreateFilledTriangleTexture(_graphicsDevice, size);
			_triangleTextures[size] = tex;
			return tex;
		}

		// Generates a square texture of size 'size' containing an upright isosceles triangle, white fill, transparent outside
		private static Texture2D CreateFilledTriangleTexture(GraphicsDevice device, int size)
		{
			int w = size;
			int h = size;
			var tex = new Texture2D(device, w, h);
			var data = new Color[w * h];
			float halfW = w / 2f;
			for (int y = 0; y < h; y++)
			{
				float fy = y + 0.5f;
				float t = MathHelper.Clamp(fy / h, 0f, 1f); // 0 at apex, 1 at base
				float left = halfW * (1f - t);
				float right = w - left;
				for (int x = 0; x < w; x++)
				{
					float fx = x + 0.5f;
					int idx = y * w + x;
					if (fx >= left && fx <= right)
						data[idx] = Color.White;
					else
						data[idx] = Color.Transparent;
				}
			}
			tex.SetData(data);
			return tex;
		}
	}
}


