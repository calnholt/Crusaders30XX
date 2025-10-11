using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Location Select")]
	public class LocationSelectDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private readonly ContentManager _content;
		private readonly Dictionary<string, Texture2D> _textureCache = new();
		private readonly Dictionary<string, Entity> _locationEntitiesById = new();
		private Texture2D _pixel;

		// Cached viewport to detect size changes and recompute layout
		private int _lastViewportW = -1;
		private int _lastViewportH = -1;

		[DebugEditable(DisplayName = "Tile Size", Step = 8, Min = 64, Max = 1024)]
		public int TileSize { get; set; } = 256;

		[DebugEditable(DisplayName = "Tile Padding", Step = 2, Min = 0, Max = 200)]
		public int TilePadding { get; set; } = 24;

		[DebugEditable(DisplayName = "Rect Padding", Step = 2, Min = 0, Max = 200)]
		public int RectPadding { get; set; } = 8;

		[DebugEditable(DisplayName = "Rows", Step = 1, Min = 1, Max = 12)]
		public int Rows { get; set; } = 2;

		[DebugEditable(DisplayName = "Columns", Step = 1, Min = 1, Max = 12)]
		public int Columns { get; set; } = 3;

		[DebugEditable(DisplayName = "Y Offset", Step = 4, Min = -2000, Max = 2000)]
		public int YOffset { get; set; } = 0;

		[DebugEditable(DisplayName = "Label Scale", Step = 0.05f, Min = 0.25f, Max = 2f)]
		public float LabelScale { get; set; } = 0.25f;

		[DebugEditable(DisplayName = "Image Scale", Step = 0.05f, Min = 0.1f, Max = 2f)]
		public float ImageScale { get; set; } = 1.0f;

		[DebugEditable(DisplayName = "Circle Radius", Step = 1, Min = 4, Max = 256)]
		public int CircleRadius { get; set; } = 40;

		[DebugEditable(DisplayName = "Circle Offset X", Step = 1, Min = -200, Max = 200)]
		public int CircleOffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Circle Offset Y", Step = 1, Min = -200, Max = 200)]
		public int CircleOffsetY { get; set; } = 0;

		public LocationSelectDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			_font = font;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.WorldMap)
			{
				// Reset viewport cache when leaving scene to ensure layout recomputes on re-entry
				_lastViewportW = -1;
				_lastViewportH = -1;
				return;
			}

			int w = _graphicsDevice.Viewport.Width;
			int h = _graphicsDevice.Viewport.Height;
			if (w != _lastViewportW || h != _lastViewportH)
			{
				_lastViewportW = w;
				_lastViewportH = h;
				LayoutAndSyncLocationEntities(w, h);
			}
			else
			{
				// Bounds might still need to be updated if debug-edited sizes changed
				LayoutAndSyncLocationEntities(w, h);
			}
		}

		private void LayoutAndSyncLocationEntities(int viewportW, int viewportH)
		{
			var all = LocationDefinitionCache.GetAll();
			var list = all?.Values?.OrderBy(d => d?.name ?? d?.id).ToList() ?? new List<LocationDefinition>();
			int count = list.Count;
			if (count == 0) return;

			int tile = System.Math.Max(32, TileSize);
			int pad = System.Math.Max(0, TilePadding);
			int cell = tile + pad;
			int cols = System.Math.Max(1, Columns > 0 ? Columns : viewportW / cell);
			int rows = System.Math.Max(1, Rows > 0 ? Rows : (int)System.Math.Ceiling(count / (float)cols));

			int gridW = cols * cell - pad;
			int gridH = rows * cell - pad;
			int startX = (viewportW - gridW) / 2;
			int startY = (viewportH - gridH) / 2 + YOffset;

			for (int i = 0; i < count; i++)
			{
				var def = list[i];
				if (def == null || string.IsNullOrEmpty(def.id)) continue;
				int r = i / cols;
				int c = i % cols;
				var rect = new Rectangle(startX + c * cell, startY + r * cell, tile, tile);

				if (!_locationEntitiesById.TryGetValue(def.id, out var e) || e == null)
				{
					e = EntityManager.CreateEntity($"Location_{def.id}");
					EntityManager.AddComponent(e, new Transform { Position = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f), ZOrder = 0 });
					EntityManager.AddComponent(e, new UIElement { Bounds = rect, IsInteractable = true, Tooltip = null});
					_locationEntitiesById[def.id] = e;
				}
				else
				{
					var t = e.GetComponent<Transform>();
					if (t != null)
					{
						t.Position = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
					}
					var ui = e.GetComponent<UIElement>();
					if (ui != null)
					{
						ui.Bounds = rect;
						ui.IsInteractable = true;
					}
				}
			}
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.WorldMap) return;

			var all = LocationDefinitionCache.GetAll();
			var list = all?.Values?.OrderBy(d => d?.name ?? d?.id).ToList() ?? new List<LocationDefinition>();
			if (list.Count == 0) return;

			foreach (var def in list)
			{
				if (def == null || string.IsNullOrEmpty(def.id)) continue;
				if (!_locationEntitiesById.TryGetValue(def.id, out var e) || e == null) continue;
				var ui = e.GetComponent<UIElement>();
				if (ui == null) continue;

				// Container background (rounded rect) and border
				var dst = ui.Bounds;
				DrawContainer(dst);

				// Compute inner padded rect for content within the container
				var inner = new Rectangle(dst.X + RectPadding, dst.Y + RectPadding, System.Math.Max(1, dst.Width - 2 * RectPadding), System.Math.Max(1, dst.Height - 2 * RectPadding));

				// Tile image
				Texture2D tex = LoadTextureSafe(def.id);
				if (tex != null)
				{
					// Scale proportionally to fit within the tile, preserving aspect ratio
					float scaleFit = System.Math.Min(inner.Width / (float)tex.Width, inner.Height / (float)tex.Height);
					float scale = System.Math.Max(0.01f, scaleFit * System.Math.Max(0.1f, ImageScale));
					int drawW = System.Math.Max(1, (int)System.Math.Round(tex.Width * scale));
					int drawH = System.Math.Max(1, (int)System.Math.Round(tex.Height * scale));
					int drawX = inner.X + (inner.Width - drawW) / 2;
					int drawY = inner.Y + (inner.Height - drawH) / 2;
					var drawRect = new Rectangle(drawX, drawY, drawW, drawH);
					_spriteBatch.Draw(tex, drawRect, Color.White);
				}
				else
				{
					// No texture available; keep the rounded container only
				}

				// Label (inside the container)
				string label = def.name ?? def.id;
				var size = _font.MeasureString(label) * LabelScale;
				var pos = new Vector2(inner.X + inner.Width / 2f - size.X / 2f, inner.Bottom - size.Y - 4);
				_spriteBatch.DrawString(_font, label, pos, Color.White, 0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);

				// Progress badge in top-right
				DrawProgressBadge(def, dst);
			}
		}

		private void DrawContainer(Rectangle dst)
		{
			// Black rounded rect underlay with a faint border
			int radius = System.Math.Max(4, System.Math.Min(dst.Width, dst.Height) / 12);
			var rounded = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, dst.Width, dst.Height, radius);
			_spriteBatch.Draw(rounded, new Rectangle(dst.X, dst.Y, dst.Width, dst.Height), Color.Black);
		}

		private void DrawProgressBadge(LocationDefinition def, Rectangle container)
		{
			int completed = SaveCache.GetValueOrDefault(def.id, 0);
			int total = def?.quests?.Count ?? 0;
			float pct = (total > 0) ? MathHelper.Clamp((completed / (float)total) * 100f, 0f, 100f) : 0f;

			int radius = System.Math.Max(4, CircleRadius);
			var circle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, radius);
			// Center the circle on the top-right corner of the rounded rectangle, with optional offsets
			int cx = container.Right + CircleOffsetX;
			int cy = container.Top + CircleOffsetY;
			var dst = new Rectangle(cx - radius, cy - radius, radius * 2, radius * 2);
			_spriteBatch.Draw(circle, dst, new Color(110, 0, 0));

			string text = ((int)System.Math.Round(pct)).ToString() + "%";
			var size = _font.MeasureString(text);
			float scale = System.Math.Min((radius * 1.6f) / System.Math.Max(1f, size.X), 0.8f);
			var pos = new Vector2(cx - (size.X * scale) / 2f, cy - (size.Y * scale) / 2f + 1);
			_spriteBatch.DrawString(_font, text, pos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}

		private void DrawBorder(Rectangle rect, int thickness, Color color)
		{
			// Top
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
			// Bottom
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
			// Left
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
			// Right
			_spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
		}

		private Texture2D LoadTextureSafe(string assetName)
		{
			if (string.IsNullOrEmpty(assetName)) return null;
			if (_textureCache.TryGetValue(assetName, out var cached)) return cached;
			try
			{
				var tex = _content.Load<Texture2D>(assetName);
				_textureCache[assetName] = tex;
				return tex;
			}
			catch
			{
				_textureCache[assetName] = null;
				return null;
			}
		}
	}
}


