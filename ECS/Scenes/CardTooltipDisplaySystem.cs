using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Card Tooltip")]
	public class CardTooltipDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private CardVisualSettings _settings;
		private readonly Dictionary<string, Entity> _tooltipCardCache = new();

		[DebugEditable(DisplayName = "Tooltip Scale", Step = 0.05f, Min = 0.25f, Max = 2.0f)]
		public float TooltipScale { get; set; } = 0.6f;

		[DebugEditable(DisplayName = "Gap Override (px)", Step = 1, Min = 0, Max = 200)]
		public int GapOverride { get; set; } = 0;

		[DebugEditable(DisplayName = "Screen Padding (px)", Step = 1, Min = 0, Max = 200)]
		public int ScreenPadding { get; set; } = 8;

		public CardTooltipDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<CardTooltip>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			// Find top-most hovered entity with CardTooltip
			var hoverables = GetRelevantEntities()
				.Select(e => new { E = e, UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), CT = e.GetComponent<CardTooltip>(), CD = e.GetComponent<CardData>() })
				.Where(x => x.UI != null && x.UI.IsHovered && x.CT != null && !string.IsNullOrWhiteSpace(x.CT.CardId))
				.OrderByDescending(x => x.T?.ZOrder ?? 0)
				.ToList();
			var top = hoverables.FirstOrDefault();
			if (top == null) return;

			// Ensure settings
			if (_settings == null)
			{
				var s = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
				_settings = s?.GetComponent<CardVisualSettings>();
				if (_settings == null) return;
			}

			int w = (int)System.Math.Round(_settings.CardWidth * TooltipScale);
			int h = (int)System.Math.Round(_settings.CardHeight * TooltipScale);
			int gap = GapOverride > 0 ? GapOverride : System.Math.Max(0, top.UI.TooltipOffsetPx);

			int rx = top.UI.Bounds.X;
			int ry = top.UI.Bounds.Y;
			switch (top.UI.TooltipPosition)
			{
				case TooltipPosition.Above:
					rx = top.UI.Bounds.X + (top.UI.Bounds.Width - w) / 2;
					ry = top.UI.Bounds.Y - h - gap;
					break;
				case TooltipPosition.Below:
					rx = top.UI.Bounds.X + (top.UI.Bounds.Width - w) / 2;
					ry = top.UI.Bounds.Bottom + gap;
					break;
				case TooltipPosition.Right:
					rx = top.UI.Bounds.Right + gap;
					ry = top.UI.Bounds.Y + (top.UI.Bounds.Height - h) / 2;
					break;
				case TooltipPosition.Left:
					rx = top.UI.Bounds.X - w - gap;
					ry = top.UI.Bounds.Y + (top.UI.Bounds.Height - h) / 2;
					break;
			}

			// Clamp to screen
			var vp = _graphicsDevice.Viewport;
			int pad = System.Math.Max(0, ScreenPadding);
			rx = System.Math.Max(pad, System.Math.Min(rx, vp.Width - w - pad));
			ry = System.Math.Max(pad, System.Math.Min(ry, vp.Height - h - pad));

			// Convert tooltip rect top-left to the card center expected by CardDisplaySystem (account for CardOffsetYExtra)
			int offsetY = (int)System.Math.Round(_settings.CardOffsetYExtra * TooltipScale);
			var center = new Vector2(rx + w / 2f, ry + (h / 2f + offsetY));

			// Get or create the visualization card entity for this tooltip
			var color = top.CD?.Color ?? CardData.CardColor.White;
			var key = top.CT.CardId + "|" + color.ToString();
			if (!_tooltipCardCache.TryGetValue(key, out var cardEntity) || cardEntity == null)
			{
				cardEntity = ECS.Factories.EntityFactory.CreateCardFromDefinition(EntityManager, top.CT.CardId, color, allowWeapons: true, index: 0);
				if (cardEntity != null)
				{
					var ui = cardEntity.GetComponent<UIElement>();
					if (ui != null) ui.IsInteractable = false; // ensure the tooltip card never intercepts hover/clicks
					_tooltipCardCache[key] = cardEntity;
				}
			}
			if (cardEntity == null) return;

			// Render via CardDisplaySystem using scaled event
			EventManager.Publish(new CardRenderScaledEvent { Card = cardEntity, Position = center, Scale = TooltipScale });
		}
	}
}




