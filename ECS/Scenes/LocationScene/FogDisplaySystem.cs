using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Location Fog Display")]
	public class FogDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private CircularMaskOverlay _overlay;

		[DebugEditable(DisplayName = "Mask Radius (px)", Step = 5f, Min = 10f, Max = 1000f)]
		public float RadiusPx { get; set; } = 160f;

		[DebugEditable(DisplayName = "Feather (px)", Step = 1f, Min = 0f, Max = 64f)]
		public float FeatherPx { get; set; } = 6f;

		public FogDisplaySystem(EntityManager em, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(em)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;

			EnsureOverlayLoaded();
			if (_overlay == null || !_overlay.IsAvailable) return;

			var centers = EntityManager
				.GetEntitiesWithComponent<PointOfInterest>()
				.Select(e => e.GetComponent<Transform>())
				.Where(t => t != null)
				.Select(t => t.Position)
				.ToList();

			if (centers.Count == 0) return;

			_overlay.CentersPx = centers;
			_overlay.RadiusPx = RadiusPx;
			_overlay.FeatherPx = FeatherPx;

			// Save current SpriteBatch device states and temporarily end the batch
			var savedBlend = _graphicsDevice.BlendState;
			var savedSampler = _graphicsDevice.SamplerStates[0];
			var savedDepth = _graphicsDevice.DepthStencilState;
			var savedRasterizer = _graphicsDevice.RasterizerState;
			_spriteBatch.End();

			// Draw overlay with its own begin/end using the effect
			_overlay.Begin(_spriteBatch);
			_overlay.Draw(_spriteBatch);
			_overlay.End(_spriteBatch);

			// Restore the previous SpriteBatch with saved states for subsequent draws
			_spriteBatch.Begin(
				SpriteSortMode.Immediate,
				savedBlend,
				savedSampler,
				savedDepth,
				savedRasterizer
			);
		}

		private void EnsureOverlayLoaded()
		{
			if (_overlay != null) return;
			Effect fx = null;
			try
			{
				fx = _content.Load<Effect>("Shaders/CircularMask");
			}
			catch { }
			_overlay = new CircularMaskOverlay(_graphicsDevice, fx);
		}
	}
}


