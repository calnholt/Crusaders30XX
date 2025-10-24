using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Location POI Display")]
	public class PointOfInterestDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _pixel;
		private bool _spawned;
		private readonly System.Collections.Generic.List<Entity> _pois = new System.Collections.Generic.List<Entity>();
		private readonly System.Collections.Generic.Dictionary<int, Vector2> _worldByEntityId = new System.Collections.Generic.Dictionary<int, Vector2>();

		// Hardcoded POIs in world-space coordinates
		private static readonly Vector2[] PoiPositions = new Vector2[]
		{
			new Vector2(800, 900),
			new Vector2(2200, 1400),
			new Vector2(5200, 2400),
			new Vector2(3000, 600),
		};

		public PointOfInterestDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;

			if (!_spawned)
			{
				SpawnPois();
				_spawned = true;
			}

			// Provide base positions (screen-space) for parallax to offset from
			var cam = EntityManager.GetEntity("LocationCamera")?.GetComponent<LocationCameraState>();
			if (cam == null) return;
			var origin = cam.Origin;
			foreach (var e in _pois)
			{
				var t = e.GetComponent<Transform>();
				if (t == null) continue;
				if (!_worldByEntityId.TryGetValue(e.Id, out var world)) continue;
				var screenPos = world - origin;
				// Hand off screen base position to the Parallax system; it will set t.Position
				t.BasePosition = screenPos;
			}
		}

		private void SpawnPois()
		{
			int i = 0;
			foreach (var pos in PoiPositions)
			{
				var e = EntityManager.CreateEntity($"POI_{i++}");
				_worldByEntityId[e.Id] = pos;
				_pois.Add(e);
				// Initialize transform: Position will be driven by Parallax from BasePosition
				EntityManager.AddComponent(e, new Transform { Position = pos, ZOrder = 10 });
				// UI bounds size only; Parallax will center bounds at Transform.Position when AffectsUIBounds is true
				EntityManager.AddComponent(e, new UIElement { Bounds = new Rectangle(0, 0, 100, 100), IsInteractable = false });
				EntityManager.AddComponent(e, ParallaxLayer.GetLocationParallaxLayer());
				// Attach POI component for fog-of-war and interactions
				EntityManager.AddComponent(e, new PointOfInterest { WorldPosition = pos, RevealRadius = 300 });
			}
		}

		public void Draw()
		{
			var cam = EntityManager.GetEntity("LocationCamera")?.GetComponent<LocationCameraState>();
			if (cam == null) return;
			var origin = cam.Origin;
			int w = cam.ViewportW;
			int h = cam.ViewportH;

			// Draw only POIs that intersect screen
            foreach (var e in _pois)
            {
                var t = e.GetComponent<Transform>();
                var ui = e.GetComponent<UIElement>();
                if (t == null || ui == null) continue;

                var rect = new Rectangle((int)System.Math.Round(t.Position.X - 50), (int)System.Math.Round(t.Position.Y - 50), 100, 100);
                if (rect.Right < 0 || rect.Bottom < 0 || rect.Left > w || rect.Top > h) continue;
                _spriteBatch.Draw(_pixel, rect, Color.Red);
            }
		}
	}
}


