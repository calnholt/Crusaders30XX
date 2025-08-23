using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Renders the player's Courage as a red circle with a white outline and centered white text,
	/// anchored just below the player portrait.
	/// </summary>
	[DebugTab("Courage Display")]
	public class CourageDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private readonly Dictionary<(int radius, uint edgeKey), Texture2D> _circleTextures = new();
		private readonly Dictionary<(int outer, int thickness), Texture2D> _ringTextures = new();

		// Debug-adjustable fields
		[DebugEditable(DisplayName = "Circle Radius", Step = 1, Min = 1, Max = 300)]
		public int CircleRadius { get; set; } = 25;

		[DebugEditable(DisplayName = "Outline Thickness", Step = 1, Min = 1, Max = 50)]
		public int OutlineThickness { get; set; } = 2;

		[DebugEditable(DisplayName = "Vertical Offset From Anchor", Step = 2, Min = -1000, Max = 1000)]
		public int AnchorOffsetY { get; set; } = 230;

		[DebugEditable(DisplayName = "Text Scale Divisor", Step = 1, Min = 1, Max = 200)]
		public int TextScaleDivisor { get; set; } = 26;

		[DebugEditable(DisplayName = "Text Offset X", Step = 1, Min = -500, Max = 500)]
		public int TextOffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Text Offset Y", Step = 1, Min = -500, Max = 500)]
		public int TextOffsetY { get; set; } = 0;

		public CourageDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_font = font;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			// Render courage for the first entity that has Player and Courage
			return EntityManager.GetEntitiesWithComponent<Player>()
				.Where(e => e.HasComponent<Courage>());
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var playerEntity = GetRelevantEntities().FirstOrDefault();
			if (playerEntity == null) return;

			var courage = playerEntity.GetComponent<Courage>();
			if (courage == null) return;

			// Find the portrait anchor (created by PlayerDisplaySystem)
			var anchor = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (anchor == null) return;
			var anchorTransform = anchor.GetComponent<Transform>();
			var portraitInfo = anchor.GetComponent<PortraitInfo>();
			if (anchorTransform == null || portraitInfo == null || _font == null) return;

			// Fully decouple from portrait breathing and UI scaling: fixed pixel size and fixed offset
			int radius = Math.Max(1, CircleRadius);
			int outlineThickness = Math.Max(1, OutlineThickness);
			if (radius < outlineThickness + 2) radius = outlineThickness + 2; // ensure inner radius stays positive

			// Center position for the badge below the portrait anchor center using a fixed pixel offset (no scaling)
			var center = new Vector2(anchorTransform.Position.X, anchorTransform.Position.Y + AnchorOffsetY);

			// Fetch cached filled circle textures of the required radii
			var outerTex = GetOrCreateCircleTexture(radius, Color.White);
			if (outerTex == null) return;

			// Draw outer white ring (true circular outline)
			var ringTex = GetOrCreateRingTexture(radius, outlineThickness);
			if (ringTex != null)
			{
				_spriteBatch.Draw(
					ringTex,
					position: center,
					sourceRectangle: null,
					color: Color.White,
					rotation: 0f,
					origin: new Vector2(radius, radius),
					scale: Vector2.One,
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// Draw inner red fill
			int innerRadius = Math.Max(1, radius - outlineThickness);
			var innerTex = GetOrCreateCircleTexture(innerRadius, Color.Red);
			if (innerTex != null)
			{
				_spriteBatch.Draw(
					innerTex,
					position: center,
					sourceRectangle: null,
					color: Color.Red,
					rotation: 0f,
					origin: new Vector2(innerRadius, innerRadius),
					scale: Vector2.One,
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// Draw centered text (courage amount) in white
			string text = Math.Max(0, courage.Amount).ToString();
			float textScale = Math.Min(1.0f, innerRadius / Math.Max(1f, TextScaleDivisor)); // heuristic scale with size
			var textSize = _font.MeasureString(text) * textScale;
			var textPos = new Vector2(center.X - textSize.X / 2f + TextOffsetX, center.Y - textSize.Y / 2f + TextOffsetY);
			_spriteBatch.DrawString(_font, text, textPos, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

			// Ensure a hoverable UI element exists over the courage circle for tooltip
			var hover = EntityManager.GetEntitiesWithComponent<CourageTooltipAnchor>().FirstOrDefault();
			int diameter = radius * 2;
			var hitRect = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), diameter, diameter);
			if (hover == null)
			{
				hover = EntityManager.CreateEntity("UI_CourageTooltip");
				EntityManager.AddComponent(hover, new CourageTooltipAnchor());
				EntityManager.AddComponent(hover, new Transform { Position = new Vector2(hitRect.X, hitRect.Y), ZOrder = 10001 });
				EntityManager.AddComponent(hover, new UIElement { Bounds = hitRect, IsInteractable = true, Tooltip = "Courage" });
			}
			else
			{
				var ui = hover.GetComponent<UIElement>();
				if (ui != null) ui.Bounds = hitRect;
			}
		}

		private Texture2D GetOrCreateCircleTexture(int radius, Color edgeColor)
		{
			if (radius <= 0) return null;
			var key = (radius, edgeColor.PackedValue);
			if (_circleTextures.TryGetValue(key, out var tex) && tex != null) return tex;
			tex = CreateFilledCircleTexture(_graphicsDevice, radius, edgeColor);
			_circleTextures[key] = tex;
			return tex;
		}

		private Texture2D GetOrCreateRingTexture(int outerRadius, int thickness)
		{
			if (outerRadius <= 0 || thickness <= 0) return null;
			var key = (outerRadius, thickness);
			if (_ringTextures.TryGetValue(key, out var tex) && tex != null) return tex;
			tex = CreateRingTexture(_graphicsDevice, outerRadius, thickness);
			_ringTextures[key] = tex;
			return tex;
		}

		private static Texture2D CreateFilledCircleTexture(GraphicsDevice device, int radius, Color edgeColor)
		{
			int diameter = radius * 2;
			var texture = new Texture2D(device, diameter, diameter);
			var data = new Color[diameter * diameter];
			int rSq = radius * radius;
			for (int y = 0; y < diameter; y++)
			{
				int dy = y - radius;
				for (int x = 0; x < diameter; x++)
				{
					int dx = x - radius;
					int idx = y * diameter + x;
					bool inside = (dx * dx + dy * dy) <= rSq;
					if (inside)
					{
						data[idx] = Color.White; // opaque fill; final tint applied at draw time
					}
					else
					{
						// Fully transparent outside the circle to avoid any solid square fill
						data[idx] = Color.Transparent;
					}
				}
			}
			texture.SetData(data);
			return texture;
		}

		private static Texture2D CreateRingTexture(GraphicsDevice device, int outerRadius, int thickness)
		{
			int diameter = outerRadius * 2;
			int innerRadius = Math.Max(1, outerRadius - thickness);
			int outerSq = outerRadius * outerRadius;
			int innerSq = innerRadius * innerRadius;
			var texture = new Texture2D(device, diameter, diameter);
			var data = new Color[diameter * diameter];
			for (int y = 0; y < diameter; y++)
			{
				int dy = y - outerRadius;
				for (int x = 0; x < diameter; x++)
				{
					int dx = x - outerRadius;
					int d2 = dx * dx + dy * dy;
					int idx = y * diameter + x;
					if (d2 <= outerSq && d2 >= innerSq)
					{
						data[idx] = Color.White;
					}
					else
					{
						data[idx] = Color.Transparent;
					}
				}
			}
			texture.SetData(data);
			return texture;
		}
	}
}


