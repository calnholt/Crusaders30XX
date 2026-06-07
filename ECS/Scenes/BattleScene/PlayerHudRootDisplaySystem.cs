using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Player HUD Root")]
	public class PlayerHudRootDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;

		public PlayerHudRootDisplaySystem(
			EntityManager entityManager,
			GraphicsDevice graphicsDevice,
			SpriteBatch spriteBatch) : base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var root = EntityManager.GetEntitiesWithComponent<PlayerHudAnchor>().FirstOrDefault();
			var anchor = root?.GetComponent<PlayerHudAnchor>();
			if (anchor == null || anchor.Bounds.Width <= 0 || anchor.Bounds.Height <= 0) return;

			var regions = EntityManager.GetEntitiesWithComponent<PlayerHudRegion>()
				.Select(entity => entity.GetComponent<PlayerHudRegion>())
				.Where(region => region != null
					&& region.Type != PlayerHudRegionType.Root
					&& region.IsVisible
					&& region.Bounds.Width > 0
					&& region.Bounds.Height > 0)
				.ToList();

			int blur = Math.Max(0, anchor.ShadowBlurRadius);
			const int layers = 5;
			for (int layer = layers; layer >= 1; layer--)
			{
				float fraction = layer / (float)layers;
				int spread = (int)Math.Round(blur * fraction);
				byte alpha = (byte)Math.Round(anchor.ShadowAlpha * (1f - fraction * 0.72f) / layers);
				foreach (var region in regions)
				{
					DrawRegionShadow(region.Bounds, anchor, spread, alpha);
				}
			}
		}

		private void DrawRegionShadow(Rectangle bounds, PlayerHudAnchor anchor, int spread, byte alpha)
		{
			var destination = new Rectangle(
				bounds.X - spread,
				bounds.Y + anchor.ShadowOffsetY - spread,
				bounds.Width + spread * 2,
				bounds.Height + spread * 2);
			float skewPercent = destination.Width <= 0
				? 0f
				: anchor.Slant * 100f / destination.Width;
			var mask = PrimitiveTextureFactory.GetParallelogramMask(
				_graphicsDevice,
				destination.Width,
				destination.Height,
				skewPercent);
			_spriteBatch.Draw(mask, destination, Color.FromNonPremultiplied(0, 0, 0, alpha));
		}
	}
}
