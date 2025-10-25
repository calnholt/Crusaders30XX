using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Location Fog Display")]
	public class FogDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteBatch _offscreenBatch;
		private RenderTarget2D _mask;
		private readonly BlendState _alphaErase;

		public FogDisplaySystem(EntityManager em, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(em)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_offscreenBatch = new SpriteBatch(graphicsDevice);
			// Multiplicative alpha cutout: start from alpha=1 and scale by (1 - srcA)
			_alphaErase = new BlendState
			{
				ColorWriteChannels = ColorWriteChannels.Alpha,
				ColorSourceBlend = Blend.Zero,
				ColorDestinationBlend = Blend.One,
				AlphaSourceBlend = Blend.Zero,
				AlphaDestinationBlend = Blend.InverseSourceAlpha,
				ColorBlendFunction = BlendFunction.Add,
				AlphaBlendFunction = BlendFunction.Add
			};



		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// No-op; fog mask is generated during Draw to avoid SpriteBatch.Begin/End issues in Update.
		}

		private void EnsureMask()
		{
			int w = _graphicsDevice.Viewport.Width;
			int h = _graphicsDevice.Viewport.Height;
			if (_mask == null || _mask.Width != w || _mask.Height != h || _mask.IsDisposed)
			{
				_mask?.Dispose();
				_mask = new RenderTarget2D(_graphicsDevice, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
			}
		}

		public void Draw()
		{
			var sceneEntity = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
			if (sceneEntity == null) return;
			var scene = sceneEntity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;

			var cam = EntityManager.GetEntity("LocationCamera")?.GetComponent<LocationCameraState>();
			if (cam == null) return;
			EnsureMask();

			// Build mask off-screen without touching the on-screen SpriteBatch
			_graphicsDevice.SetRenderTarget(_mask);
			_graphicsDevice.Clear(new Color(0, 0, 0, 255)); // opaque black alpha=1
			_offscreenBatch.Begin(SpriteSortMode.Immediate, _alphaErase);
			// var origin = cam.Origin;
			// foreach (var poiEntity in EntityManager.GetEntitiesWithComponent<PointOfInterest>())
			// {
			// 	var poi = poiEntity.GetComponent<PointOfInterest>();
			// 	if (poi == null) continue;
			// 	var center = poi.WorldPosition - origin;
				// var tex = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, System.Math.Max(1, poi.RevealRadius));
			// 	var dest = new Rectangle(
			// 		(int)System.Math.Round(center.X - poi.RevealRadius),
			// 		(int)System.Math.Round(center.Y - poi.RevealRadius),
			// 		poi.RevealRadius * 2,
			// 		poi.RevealRadius * 2);
			// 	// Write alpha=1 into the area we want to subtract from the mask (holes)
			// 	_offscreenBatch.Draw(tex, dest, Color.White);
			// }
			// _offscreenBatch.End();
			// _graphicsDevice.SetRenderTarget(null);
			// // Restore default blend for main on-screen batch so subsequent draws render normally
			// _graphicsDevice.BlendState = BlendState.AlphaBlend;
			// _graphicsDevice.DepthStencilState = DepthStencilState.None;

			// // Draw the mask as black fog using alpha channel (on-screen batch already begun by Game1)
			// _spriteBatch.Draw(_mask, new Rectangle(0, 0, _mask.Width, _mask.Height), Color.Black);

			_offscreenBatch.Draw(PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, System.Math.Max(1, 300)), new Rectangle(200, 200, 200, 200), Color.White);
_offscreenBatch.End();

_graphicsDevice.SetRenderTarget(null);

// Draw fog
_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
_spriteBatch.Draw(_mask, Vector2.Zero, Color.Black);
		}
	}
}


