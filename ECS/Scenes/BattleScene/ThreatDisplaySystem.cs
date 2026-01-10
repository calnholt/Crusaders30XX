using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Renders the enemy's Threat as a black circle with a white outline and centered white text,
	/// anchored to the left of the enemy's HP bar.
	/// </summary>
	[DebugTab("Threat Display")]
	public class ThreatDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.ContentFont;
		private readonly Dictionary<(int radius, uint edgeKey), Texture2D> _circleTextures = new();
		private readonly Dictionary<(int outer, int thickness), Texture2D> _ringTextures = new();

		// Debug-adjustable fields
		[DebugEditable(DisplayName = "Circle Radius", Step = 1, Min = 1, Max = 300)]
		public int CircleRadius { get; set; } = 20;

		[DebugEditable(DisplayName = "Outline Thickness", Step = 1, Min = 1, Max = 50)]
		public int OutlineThickness { get; set; } = 2;

		[DebugEditable(DisplayName = "HP Bar Left Padding", Step = 1, Min = -128, Max = 128)]
		public int HpBarLeftPadding { get; set; } = 8;

		[DebugEditable(DisplayName = "Text Scale Divisor", Step = 1, Min = 1, Max = 200)]
		public int TextScaleDivisor { get; set; } = 88;

		[DebugEditable(DisplayName = "Text Offset X", Step = 1, Min = -500, Max = 500)]
		public int TextOffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Text Offset Y", Step = 1, Min = -500, Max = 500)]
		public int TextOffsetY { get; set; } = 0;

		public ThreatDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			EventManager.Subscribe<ModifyThreatEvent>(OnModifyThreat);
			EventManager.Subscribe<SetThreatEvent>(OnSetThreat);
		}

		private void OnModifyThreat(ModifyThreatEvent evt)
		{
			TriggerPulse();
		}

		private void OnSetThreat(SetThreatEvent evt)
		{
			TriggerPulse();
		}

		private void TriggerPulse()
		{
			var hover = EntityManager.GetEntitiesWithComponent<ThreatTooltipAnchor>().FirstOrDefault();
			if (hover != null)
			{
				EventManager.Publish(new JigglePulseEvent { Target = hover, Config = JigglePulseConfig.Default });
			}
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			// Render threat for entities that have Enemy and Threat
			return EntityManager.GetEntitiesWithComponent<Enemy>()
				.Where(e => e.HasComponent<Threat>());
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var enemyEntity = GetRelevantEntities().FirstOrDefault();
			if (enemyEntity == null) return;

			var threat = enemyEntity.GetComponent<Threat>();
			if (threat == null) return;

			var enemyTransform = enemyEntity.GetComponent<Transform>();
			if (enemyTransform == null || _font == null) return;

			// Fully decouple from portrait breathing and UI scaling: fixed pixel size and fixed offset
			int radius = Math.Max(1, CircleRadius);
			int outlineThickness = Math.Max(1, OutlineThickness);
			if (radius < outlineThickness + 2) radius = outlineThickness + 2; // ensure inner radius stays positive

			// Compute center position. Prefer anchoring to the left of the enemy's HP bar if available;
			// fall back to enemy transform with a vertical offset.
			Vector2 center;
			var hpAnchor = enemyEntity.GetComponent<HPBarAnchor>();
			if (hpAnchor != null && hpAnchor.Rect.Width > 0 && hpAnchor.Rect.Height > 0)
			{
				int xLeft = hpAnchor.Rect.X;
				int yMid = hpAnchor.Rect.Y + hpAnchor.Rect.Height / 2;
				center = new Vector2(xLeft - Math.Max(-128, HpBarLeftPadding) - radius, yMid);
			}
			else
			{
				// Fallback: position relative to enemy transform
				var portraitInfo = enemyEntity.GetComponent<PortraitInfo>();
				float visualHalfHeight = 0f;
				if (portraitInfo != null)
				{
					float baseScale = (portraitInfo.BaseScale > 0f) ? portraitInfo.BaseScale : 1f;
					visualHalfHeight = Math.Max(visualHalfHeight, (portraitInfo.TextureHeight * baseScale) * 0.5f);
				}
				center = new Vector2(enemyTransform.Position.X - radius - HpBarLeftPadding, enemyTransform.Position.Y + visualHalfHeight + 26);
			}

			// Get pulse transform state from the anchor entity
			var hover = EntityManager.GetEntitiesWithComponent<ThreatTooltipAnchor>().FirstOrDefault();
			float rotation = 0f;
			Vector2 scale = Vector2.One;
			if (hover != null)
			{
				var ht = hover.GetComponent<Transform>();
				if (ht != null)
				{
					rotation = ht.Rotation;
					scale = ht.Scale;
				}
			}

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
					rotation: rotation,
					origin: new Vector2(radius, radius),
					scale: scale,
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// Draw inner black fill
			int innerRadius = Math.Max(1, radius - outlineThickness);
			var innerTex = GetOrCreateCircleTexture(innerRadius, Color.Black);
			if (innerTex != null)
			{
				_spriteBatch.Draw(
					innerTex,
					position: center,
					sourceRectangle: null,
					color: Color.Black,
					rotation: rotation,
					origin: new Vector2(innerRadius, innerRadius),
					scale: scale,
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// Draw centered text (threat amount) in white
			string text = Math.Max(0, threat.Amount).ToString();
			float baseTextScale = Math.Min(1.0f, innerRadius / Math.Max(1f, TextScaleDivisor)); // heuristic scale with size
			Vector2 textOrigin = _font.MeasureString(text) / 2f;
			Vector2 textPos = new Vector2(center.X + TextOffsetX, center.Y + TextOffsetY);
			_spriteBatch.DrawString(_font, text, textPos, Color.White, rotation, textOrigin, baseTextScale * scale, SpriteEffects.None, 0f);

			// Update hoverable UI element bounds over the threat circle for tooltip (entity pre-created in factory)
			if (hover != null)
			{
				int diameter = radius * 2;
				var hitRect = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), diameter, diameter);
				var ui = hover.GetComponent<UIElement>();
				if (ui != null) ui.Bounds = hitRect;
				var ht = hover.GetComponent<Transform>();
				if (ht != null) ht.Position = new Vector2(hitRect.X, hitRect.Y);
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

