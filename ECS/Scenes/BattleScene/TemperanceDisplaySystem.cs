using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Displays the player's Temperance as a chunked meter composed of slanted parallelogram chunks.
	/// Number of chunks equals the equipped Temperance ability's threshold. Filled chunks are white with
	/// black outline; empty chunks are black with black outline and a white slant indicator.
	/// Positioned next to the Courage display using the portrait anchor for reference.
	/// </summary>
	[DebugTab("Temperance Display")]
	public class TemperanceDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.ContentFont;
		private readonly System.Collections.Generic.Dictionary<string, Texture2D> _chunkFilledCache = new();
		private readonly System.Collections.Generic.Dictionary<string, Texture2D> _chunkEmptyCache = new();

		// Debug-adjustable fields
		[DebugEditable(DisplayName = "Chunk Width", Step = 1, Min = 4, Max = 512)]
		public int ChunkWidth { get; set; } = 14;
		[DebugEditable(DisplayName = "Chunk Height", Step = 1, Min = 4, Max = 512)]
		public int ChunkHeight { get; set; } = 22;
		[DebugEditable(DisplayName = "Chunk Gap", Step = 1, Min = -128, Max = 128)]
		public int ChunkGap { get; set; } = 0;
		[DebugEditable(DisplayName = "Chunk Slant (px)", Step = 1, Min = 0, Max = 128)]
		public int ChunkSlant { get; set; } = 16;

		[DebugEditable(DisplayName = "Outline Thickness", Step = 1, Min = 1, Max = 50)]
		public int OutlineThickness { get; set; } = 2;

		[DebugEditable(DisplayName = "HP Bar Left Padding", Step = 1, Min = -128, Max = 128)]
		public int HpBarLeftPadding { get; set; } = 4;

		[DebugEditable(DisplayName = "Anchor Offset X", Step = 2, Min = -2000, Max = 2000)]
		public int AnchorOffsetX { get; set; } = -180; // to the right of Courage by default

		[DebugEditable(DisplayName = "Anchor Offset Y", Step = 2, Min = -2000, Max = 2000)]
		public int AnchorOffsetY { get; set; } = 188; // align with Courage default

		public TemperanceDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
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

			var anchor = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (anchor == null) return;
			var t = anchor.GetComponent<Transform>();
			var info = anchor.GetComponent<PortraitInfo>();
			if (t == null || info == null) return;

			int outline = Math.Max(1, OutlineThickness);
			int wChunk = Math.Max(4, ChunkWidth);
			int hChunk = Math.Max(4, ChunkHeight);
			int slant = Math.Max(0, ChunkSlant);

			Vector2 center; // computed after threshold is known

			// Resolve threshold from equipped ability (default to 3 if unavailable)
			int threshold = 3;
			Crusaders30XX.ECS.Data.Temperance.TemperanceAbilityDefinition def = null;
			var equipped = player.GetComponent<EquippedTemperanceAbility>();
			if (equipped != null && !string.IsNullOrEmpty(equipped.AbilityId))
			{
				if (Data.Temperance.TemperanceAbilityDefinitionCache.TryGet(equipped.AbilityId, out def) && def != null)
				{
					threshold = Math.Max(1, def.threshold);
				}
			}
			int filled = Math.Max(0, Math.Min(temperance.Amount, threshold));

			// Position to the left of the player's HP bar using HPBarAnchor if available; fall back to portrait anchor
			var playerEntity = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var hpAnchor = playerEntity?.GetComponent<HPBarAnchor>();
			if (hpAnchor != null && hpAnchor.Rect.Width > 0 && hpAnchor.Rect.Height > 0)
			{
				// Compute preview width using the same formula as actual draw so the right edge aligns
				int meterWidthPreview = threshold * wChunk + Math.Max(0, ChunkSlant) + Math.Max(0, (threshold - 1) * Math.Max(0, ChunkGap));
				int xLeft = hpAnchor.Rect.X - Math.Max(-128, HpBarLeftPadding) - meterWidthPreview; // padding left of HP bar
				int yMid = hpAnchor.Rect.Y + hpAnchor.Rect.Height / 2;
				center = new Vector2(xLeft + meterWidthPreview / 2f, yMid);
			}
			else
			{
				center = new Vector2(t.Position.X + AnchorOffsetX, t.Position.Y + AnchorOffsetY);
			}

			// Prepare textures for chunks
			int texW = wChunk + slant; // bounding width of slanted shape
			int texH = hChunk;
			string key = $"{texW}x{texH}s{slant}o{outline}";
			var texFilled = GetOrCreateParallelogramTexture(key + ":filled", texW, texH, slant, Color.White, Color.Black, false);
			var texEmpty = GetOrCreateParallelogramTexture(key + ":empty:slant", texW, texH, slant, Color.Black, Color.Black, true);
			var texEmptyFirst = GetOrCreateParallelogramTexture(key + ":empty:noslant", texW, texH, slant, Color.Black, Color.Black, false);
			if (texFilled == null || texEmpty == null) return;

			// Layout horizontally centered on center.X
			int stepX = wChunk + ChunkGap; // step equals visible width plus configured gap; when gap=0, chunks butt together
			int totalW = threshold * wChunk + Math.Max(0, ChunkSlant) + Math.Max(0, (threshold - 1) * ChunkGap);
			int startX = (int)Math.Round(center.X - totalW / 2f);
			int y = (int)Math.Round(center.Y - texH / 2f);
			for (int i = 0; i < threshold; i++)
			{
				int x = startX + i * stepX;
				var tex = (i < filled) ? texFilled : (i == 0 ? texEmptyFirst : texEmpty);
				_spriteBatch.Draw(tex, new Vector2(x, y), Color.White);
			}

			// Update hoverable UI element bounds over the temperance meter for tooltip (entity pre-created in factory)
			var hover = EntityManager.GetEntitiesWithComponent<TemperanceTooltipAnchor>().FirstOrDefault();
			var hitRect = new Rectangle(startX, y, totalW, texH);
			var ui = hover.GetComponent<UIElement>();
			if (ui != null)
			{
				ui.Bounds = hitRect;
				// Tooltip content is initialized in the factory; bounds only are updated here
			}
			var ht = hover.GetComponent<Transform>();
			if (ht != null) ht.Position = new Vector2(hitRect.X, hitRect.Y);
		}

		private Texture2D GetOrCreateParallelogramTexture(string key, int width, int height, int slant, Color fill, Color outline, bool drawWhiteSlantOnEmpty)
		{
			var dict = (fill == Color.White) ? _chunkFilledCache : _chunkEmptyCache;
			if (dict.TryGetValue(key, out var existing) && existing != null) return existing;
			var tex = new Texture2D(_graphicsDevice, width, height);
			var data = new Color[width * height];
			for (int y = 0; y < height; y++)
			{
				// left edge shifts right as y decreases (top slants right)
				float t = (height <= 1) ? 0f : (1f - (y / (float)(height - 1)));
				int left = (int)Math.Round(slant * t);
				int right = left + (width - 1 - slant);
				for (int x = 0; x < width; x++)
				{
					int idx = y * width + x;
					bool inside = (x >= left && x <= right);
					if (!inside) { data[idx] = Color.Transparent; continue; }
					bool isOutline = (x == left || x == right || y == 0 || y == height - 1);
					if (isOutline)
					{
						data[idx] = outline;
					}
					else
					{
						data[idx] = fill;
					}
				}
			}
			// Optional white slant indicator on empty chunk: draw a diagonal along the slanted edge
			if (drawWhiteSlantOnEmpty && fill == Color.Black)
			{
				for (int yy = 1; yy < height - 1; yy++)
				{
					float t = (height <= 1) ? 0f : (1f - (yy / (float)(height - 1)));
					int xDiag = (int)Math.Round(slant * t) + 1;
					int idx = yy * width + Math.Max(1, Math.Min(width - 2, xDiag));
					data[idx] = Color.White;
				}
			}
			tex.SetData(data);
			dict[key] = tex;
			return tex;
		}
	}
}


