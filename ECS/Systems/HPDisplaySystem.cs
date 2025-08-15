using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Rendering;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Renders a horizontal HP bar below entities with an HP component.
	/// Currently positioned relative to the player's portrait anchor.
	/// </summary>
	[DebugTab("HP Display")]
	public class HPDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private Texture2D _pixel;
		private Texture2D _roundedBack;
		private Texture2D _roundedFill;
		private Texture2D _roundedHighlight;
		private int _cachedRoundedWidth;
		private int _cachedRoundedHeight;
		private int _cachedCornerRadius;
		private SpriteFont _font;

		[DebugEditable(DisplayName = "Bar Width", Step = 2, Min = 10, Max = 2000)]
		public int BarWidth { get; set; } = 290;

		[DebugEditable(DisplayName = "Bar Height", Step = 1, Min = 4, Max = 200)]
		public int BarHeight { get; set; } = 22;

		[DebugEditable(DisplayName = "Offset X", Step = 2, Min = -2000, Max = 2000)]
		public int OffsetX { get; set; } = 46;

		[DebugEditable(DisplayName = "Offset Y", Step = 2, Min = -2000, Max = 2000)]
		public int OffsetY { get; set; } = 56;

		[DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int CornerRadius { get; set; } = 12;

		// Pill look controls (do not modify texture creation)
		[DebugEditable(DisplayName = "Highlight Opacity", Step = 0.05f, Min = 0f, Max = 1f)]
		public float HighlightOpacity { get; set; } = 0.45f;

		[DebugEditable(DisplayName = "Highlight Height", Step = 1, Min = 1, Max = 400)]
		public int HighlightHeight { get; set; } = 13;

		[DebugEditable(DisplayName = "Highlight Offset Y", Step = 1, Min = -200, Max = 200)]
		public int HighlightOffsetY { get; set; } = 1;

		public HPDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
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
			return EntityManager.GetEntitiesWithComponent<Crusaders30XX.ECS.Components.HP>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var hpEntity = GetRelevantEntities().FirstOrDefault();
			if (hpEntity == null) return;
			var hp = hpEntity.GetComponent<Crusaders30XX.ECS.Components.HP>();
			if (hp == null) return;

			// Anchor under player portrait for now
			var anchor = EntityManager.GetEntitiesWithComponent<PlayerPortraitAnchor>().FirstOrDefault();
			if (anchor == null) return;
			var t = anchor.GetComponent<Transform>();
			var info = anchor.GetComponent<PlayerPortraitInfo>();
			if (t == null) return;

			int width = Math.Max(4, BarWidth);
			int height = Math.Max(2, BarHeight);

			// Position directly below the player portrait using unscaled height (do not include portrait scale)
			float halfPortraitHeight = (info != null) ? (info.TextureHeight * 0.5f) : 0f;
			var center = new Vector2(t.Position.X + OffsetX, t.Position.Y + halfPortraitHeight + OffsetY);
			int x = (int)Math.Round(center.X - width / 2f);
			int y = (int)Math.Round(center.Y - height / 2f);

			// Prepare rounded textures (cache per size)
			int radius = Math.Max(0, Math.Min(CornerRadius, Math.Min(width, height) / 2));
			bool needsRebuild = _roundedBack == null || _cachedRoundedWidth != width || _cachedRoundedHeight != height || _cachedCornerRadius != radius;
			if (needsRebuild)
			{
				_roundedBack?.Dispose();
				_roundedFill?.Dispose();
				_roundedHighlight?.Dispose();
				_roundedBack = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
				_roundedFill = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
				_roundedHighlight = CreateRoundedHighlightTexture(_graphicsDevice, width, height, radius);
				_cachedRoundedWidth = width;
				_cachedRoundedHeight = height;
				_cachedCornerRadius = radius;
			}

			// Background rounded rect (dark gray)
			var backRect = new Rectangle(x, y, width, height);
			_spriteBatch.Draw(_roundedBack, backRect, new Color((byte)40, (byte)40, (byte)40));

			// Fill
			float pct = hp.Max > 0 ? MathHelper.Clamp(hp.Current / (float)hp.Max, 0f, 1f) : 0f;
			int fillW = (int)Math.Round(width * pct);
			var fillRect = new Rectangle(x, y, Math.Max(0, fillW), height);
			var fillColor = Color.Lerp(new Color((byte)120, (byte)0, (byte)0), new Color((byte)255, (byte)40, (byte)40), pct);
			// For the fill, draw the rounded texture but clip to fill width via destination width
			if (fillRect.Width > 0)
			{
				int srcW = Math.Max(1, fillRect.Width);
				var src = new Rectangle(0, 0, srcW, height);
				_spriteBatch.Draw(_roundedFill, fillRect, src, fillColor);
				if (_roundedHighlight != null)
				{
					// Adjustable pill overlay band
					int bandH = Math.Max(1, Math.Min(HighlightHeight, height));
					int destY = y + (height - bandH) / 2 + HighlightOffsetY;
					var bandDest = new Rectangle(x, destY, fillRect.Width, bandH);
					int srcBandY = Math.Max(0, (height - bandH) / 2);
					var bandSrc = new Rectangle(0, srcBandY, srcW, bandH);
					var tint = Color.FromNonPremultiplied(255, 255, 255, (int)(MathHelper.Clamp(HighlightOpacity, 0f, 1f) * 255f));
					_spriteBatch.Draw(_roundedHighlight, bandDest, bandSrc, tint);
				}
			}

			// Centered HP text: "current/max"
			if (_font != null)
			{
				string hpText = $"{Math.Max(0, hp.Current)}/{Math.Max(0, hp.Max)}";
				var textSize = _font.MeasureString(hpText);
				float scale = Math.Min(1f, Math.Min(width / Math.Max(1f, textSize.X), height / Math.Max(1f, textSize.Y)));
				var textPos = new Vector2(x + width / 2f - (textSize.X * scale) / 2f, y + height / 2f - (textSize.Y * scale) / 2f);
				_spriteBatch.DrawString(_font, hpText, textPos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			}
		}

		private static Texture2D CreateRoundedHighlightTexture(GraphicsDevice gd, int width, int height, int radius)
		{
			var tex = new Texture2D(gd, width, height, false, SurfaceFormat.Color);
			var data = new Color[width * height];
			int r = System.Math.Max(0, radius);
			int r2 = r * r;
			int w = width;
			int h = height;
			for (int y = 0; y < h; y++)
			{
				float v = h > 1 ? (y / (float)(h - 1)) : 0.5f; // 0..1
				float t = 2f * (v - 0.5f);
				float parabola = System.Math.Max(0f, 1f - (t * t)); // 1 at center, 0 at edges
				float maxAlpha = 0.35f; // intensity
				byte a = (byte)System.Math.Round(MathHelper.Clamp(parabola * maxAlpha, 0f, 1f) * 255f);
				for (int x = 0; x < w; x++)
				{
					bool inside = true;
					if (x < r && y < r)
					{
						int dx = r - x - 1;
						int dy = r - y - 1;
						inside = (dx * dx + dy * dy) <= r2;
					}
					else if (x >= w - r && y < r)
					{
						int dx = x - (w - r);
						int dy = r - y - 1;
						inside = (dx * dx + dy * dy) <= r2;
					}
					else if (x < r && y >= h - r)
					{
						int dx = r - x - 1;
						int dy = y - (h - r);
						inside = (dx * dx + dy * dy) <= r2;
					}
					else if (x >= w - r && y >= h - r)
					{
						int dx = x - (w - r);
						int dy = y - (h - r);
						inside = (dx * dx + dy * dy) <= r2;
					}

					data[y * w + x] = inside ? Color.FromNonPremultiplied(255, 255, 255, a) : Color.Transparent;
				}
			}
			tex.SetData(data);
			return tex;
		}
	}
}


