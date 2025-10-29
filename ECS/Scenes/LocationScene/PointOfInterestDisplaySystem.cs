using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
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
			LocationDefinitionCache.TryGet("desert", out var def);
			
			foreach (var pos in def.pointsOfInterest)
			{
				var e = EntityManager.CreateEntity($"POI_{i++}");
				_worldByEntityId[e.Id] = pos.worldPosition;
				_pois.Add(e);
				// Initialize transform: Position will be driven by Parallax from BasePosition
				EntityManager.AddComponent(e, new Transform { Position = pos.worldPosition, ZOrder = 10 });
				// UI bounds size only; Parallax will center bounds at Transform.Position when AffectsUIBounds is true
				EntityManager.AddComponent(e, new UIElement { Bounds = new Rectangle(0, 0, 50, 50), IsInteractable = true, TooltipType = TooltipType.Quests });
				EntityManager.AddComponent(e, ParallaxLayer.GetLocationParallaxLayer());
				// Attach POI component for fog-of-war and interactions
				var poi = new PointOfInterest {
					Id = pos.id,
					WorldPosition = pos.worldPosition,
					RevealRadius = pos.revealRadius,
					UnrevealedRadius = pos.unrevealedRadius,
					IsRevealed = pos.isRevealed,
					IsCompleted = SaveCache.IsQuestCompleted(def.id, pos.id)
				};
				// Initialize display radius consistent with current state
				if (poi.IsCompleted)
				{
					poi.DisplayRadius = poi.RevealRadius;
				}
				else if (poi.IsRevealed)
				{
					poi.DisplayRadius = poi.UnrevealedRadius;
				}
				else
				{
					poi.DisplayRadius = 0f;
				}
				EntityManager.AddComponent(e, poi);
			}
		}

		public void Draw()
		{
			var cam = EntityManager.GetEntity("LocationCamera")?.GetComponent<LocationCameraState>();
			if (cam == null) return;
			var origin = cam.Origin;
			int w = cam.ViewportW;
			int h = cam.ViewportH;

			// Build sets: unlockers (revealed or completed) and visible (unlockers or within any unlocker's reveal radius)
			var list = _pois
				.Select(e => new { E = e, P = e.GetComponent<PointOfInterest>(), T = e.GetComponent<Transform>(), UI = e.GetComponent<UIElement>() })
				.Where(x => x.P != null && x.T != null && x.UI != null)
				.ToList();
			var unlockers = list.Where(x => x.P.IsRevealed || x.P.IsCompleted).ToList();
			var visibleIds = new System.Collections.Generic.HashSet<int>(unlockers.Select(x => x.E.Id));
			foreach (var x in list)
			{
				if (visibleIds.Contains(x.E.Id)) continue;
				foreach (var u in unlockers)
				{
					float dx = x.P.WorldPosition.X - u.P.WorldPosition.X;
					float dy = x.P.WorldPosition.Y - u.P.WorldPosition.Y;
					float r = (u.P.DisplayRadius > 0f) ? u.P.DisplayRadius : (u.P.IsCompleted ? u.P.RevealRadius : u.P.UnrevealedRadius);
					if ((dx * dx) + (dy * dy) <= (r * r))
					{
						visibleIds.Add(x.E.Id);
						break;
					}
				}
			}

			// Draw only POIs that are visible and intersect screen
			foreach (var x in list)
			{
				if (!visibleIds.Contains(x.E.Id)) continue;
				var rect = new Rectangle((int)System.Math.Round(x.T.Position.X - x.UI.Bounds.Width / 2), (int)System.Math.Round(x.T.Position.Y - x.UI.Bounds.Height / 2), x.UI.Bounds.Width, x.UI.Bounds.Height);
				if (rect.Right < 0 || rect.Bottom < 0 || rect.Left > w || rect.Top > h) continue;
				_spriteBatch.Draw(_pixel, rect, x.P.IsCompleted ? Color.Green : Color.Red);
			}
		}
	}
}


