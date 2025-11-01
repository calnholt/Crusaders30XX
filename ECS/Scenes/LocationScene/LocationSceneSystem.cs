using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Location Scene System")]
	public class LocationSceneSystem : Core.System
	{
    private readonly SystemManager _systemManager;
    private readonly World _world;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly ContentManager _content;
    private readonly SpriteFont _font;
    private bool _firstLoad = true;
		private LocationMapDisplaySystem _locationMapDisplaySystem;
		private PointOfInterestDisplaySystem _pointOfInterestDisplaySystem;
		private TooltipQuestDisplaySystem _tooltipQuestDisplaySystem;
		private FogDisplaySystem _fogDisplaySystem;
		private LocationPoiRevealCutsceneSystem _poiCutsceneSystem;
		private POIRadiusDebugDisplaySystem _poiRadiusDebugDisplaySystem;
		private HellRiftIndicatorDisplaySystem _hellRiftIndicatorDisplaySystem;
		private RenderTarget2D _sceneRT;
		private int _rtW;
		private int _rtH;

    public LocationSceneSystem(EntityManager entityManager, SystemManager sm, World world, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, SpriteFont font) : base(entityManager)
    {
      _systemManager = sm;
			_world = world;
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
      _font = font;
      EventManager.Subscribe<LoadSceneEvent>(_ => {
        if (_.Scene != SceneId.Location) return;
        AddLocationSystems();
      });
			EventManager.Subscribe<DeleteCachesEvent>(_ => {
				_world.RemoveSystem(_locationMapDisplaySystem);
				_world.RemoveSystem(_pointOfInterestDisplaySystem);
				_world.RemoveSystem(_tooltipQuestDisplaySystem);
				_world.RemoveSystem(_fogDisplaySystem);
				_world.RemoveSystem(_poiCutsceneSystem);
				_world.RemoveSystem(_poiRadiusDebugDisplaySystem);
				_world.RemoveSystem(_hellRiftIndicatorDisplaySystem);
				_firstLoad = true;
				_rtW = 0;
				_rtH = 0;
				_sceneRT?.Dispose();
				_sceneRT = null;
			});
    }

    protected override IEnumerable<Entity> GetRelevantEntities()
    {
      yield break;
    }

		public void Draw()
		{
			// Ensure render target matches current viewport
			var vp = _graphicsDevice.Viewport;
			if (_sceneRT == null || _rtW != vp.Width || _rtH != vp.Height)
			{
				_rtW = vp.Width;
				_rtH = vp.Height;
				_sceneRT?.Dispose();
				_sceneRT = new RenderTarget2D(
					_graphicsDevice,
					_rtW,
					_rtH,
					false,
					SurfaceFormat.Color,
					DepthFormat.None,
					0,
					RenderTargetUsage.DiscardContents
				);
			}

			// Save current SpriteBatch state from Game1 and end it
			var savedBlend = _graphicsDevice.BlendState;
			var savedSampler = _graphicsDevice.SamplerStates[0];
			var savedDepth = _graphicsDevice.DepthStencilState;
			var savedRasterizer = _graphicsDevice.RasterizerState;
			_spriteBatch.End();

			// Capture the location map + POIs into the render target
			var prevTargets = _graphicsDevice.GetRenderTargets();
			_graphicsDevice.SetRenderTarget(_sceneRT);
			_graphicsDevice.Clear(Color.Transparent);
			_spriteBatch.Begin(
				SpriteSortMode.Immediate,
				BlendState.AlphaBlend,
				SamplerState.AnisotropicClamp,
				DepthStencilState.None,
				RasterizerState.CullNone
			);
			FrameProfiler.Measure("LocationMapDisplaySystem.Draw", _locationMapDisplaySystem.Draw);
			FrameProfiler.Measure("PointOfInterestDisplaySystem.Draw", _pointOfInterestDisplaySystem.Draw);
			_spriteBatch.End();
			// Restore whatever was bound before (offscreen capture or backbuffer)
			if (prevTargets != null && prevTargets.Length > 0)
				_graphicsDevice.SetRenderTargets(prevTargets);
			else
				_graphicsDevice.SetRenderTarget(null);

			// Re-begin SpriteBatch with the original states so FogDisplaySystem can temporarily end it
			_spriteBatch.Begin(
				SpriteSortMode.Immediate,
				savedBlend,
				savedSampler,
				savedDepth,
				savedRasterizer
			);

			// Composite: warp + darken outside holes while keeping inside undistorted
			FrameProfiler.Measure("FogDisplaySystem.Composite", () => _fogDisplaySystem.DrawComposite(_sceneRT));
			FrameProfiler.Measure("HellRiftIndicatorDisplaySystem.Draw", _hellRiftIndicatorDisplaySystem.Draw);
			FrameProfiler.Measure("POIRadiusDebugDisplaySystem.Draw", _poiRadiusDebugDisplaySystem.Draw);
			FrameProfiler.Measure("TooltipQuestDisplaySystem.Draw", _tooltipQuestDisplaySystem.Draw);
		}
    private void AddLocationSystems()
		{
			if (!_firstLoad) return;
			_firstLoad = false;
			_locationMapDisplaySystem = new LocationMapDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_world.AddSystem(_locationMapDisplaySystem);
			_pointOfInterestDisplaySystem = new PointOfInterestDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content, _font);
			_world.AddSystem(_pointOfInterestDisplaySystem);
			_poiCutsceneSystem = new LocationPoiRevealCutsceneSystem(_world.EntityManager);
			_world.AddSystem(_poiCutsceneSystem);
			_fogDisplaySystem = new FogDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_world.AddSystem(_fogDisplaySystem);
			_poiRadiusDebugDisplaySystem = new POIRadiusDebugDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_world.AddSystem(_poiRadiusDebugDisplaySystem);
			_tooltipQuestDisplaySystem = new TooltipQuestDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content, _font);
			_world.AddSystem(_tooltipQuestDisplaySystem);
			_hellRiftIndicatorDisplaySystem = new HellRiftIndicatorDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_world.AddSystem(_hellRiftIndicatorDisplaySystem);
    }

    protected override void UpdateEntity(Entity entity, GameTime gameTime)
    {
      throw new System.NotImplementedException();
    }
  }
}