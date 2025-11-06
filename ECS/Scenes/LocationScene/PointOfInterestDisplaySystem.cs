using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Location POI Display")]
	public class PointOfInterestDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _pixel;
		private readonly Texture2D _questIconTexture;
		private readonly Texture2D _hellriftIconTexture;
		private bool _spawned;
		private readonly System.Collections.Generic.List<Entity> _pois = new System.Collections.Generic.List<Entity>();
		private readonly System.Collections.Generic.Dictionary<int, Vector2> _worldByEntityId = new System.Collections.Generic.Dictionary<int, Vector2>();
		private readonly System.Collections.Generic.Dictionary<int, float> _hoverScales = new System.Collections.Generic.Dictionary<int, float>();

		[DebugEditable(DisplayName = "Icon Size", Step = 1f, Min = 10f, Max = 200f)]
		public float IconSize { get; set; } = 140f;

		[DebugEditable(DisplayName = "Circle Size", Step = 1f, Min = 4f, Max = 32f)]
		public float CircleSize { get; set; } = 16f;

		[DebugEditable(DisplayName = "Circle Offset X", Step = 0.05f, Min = -2f, Max = 2f)]
		public float CircleOffsetX { get; set; } = 1f;

		[DebugEditable(DisplayName = "Circle Offset Y", Step = 0.05f, Min = -2f, Max = 2f)]
		public float CircleOffsetY { get; set; } = -1f;

		[DebugEditable(DisplayName = "Hover Scale", Step = 0.05f, Min = 1f, Max = 2f)]
		public float HoverScale { get; set; } = 1.1f;

		[DebugEditable(DisplayName = "Animation Speed", Step = 1f, Min = 1f, Max = 30f)]
		public float AnimationSpeed { get; set; } = 20f;

		public PointOfInterestDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
			try
			{
				_questIconTexture = content.Load<Texture2D>("Quest_poi");
			}
			catch
			{
				_questIconTexture = null;
			}
			try
			{
				_hellriftIconTexture = content.Load<Texture2D>("Hellrift_poi");
			}
			catch
			{
				_hellriftIconTexture = null;
			}
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
			float mapScale = cam.MapScale;
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			foreach (var e in _pois)
			{
				var t = e.GetComponent<Transform>();
				if (t == null) continue;
				if (!_worldByEntityId.TryGetValue(e.Id, out var world)) continue;
				// Scale world position by map scale to match scaled world space
				var scaledWorld = world * mapScale;
				var screenPos = scaledWorld - origin;
				// Hand off screen base position to the Parallax system; it will set t.Position
				t.BasePosition = screenPos;

				// Update hover scale animation
				var ui = e.GetComponent<UIElement>();
				if (ui != null)
				{
					float targetScale = ui.IsHovered ? HoverScale : 1f;
					if (!_hoverScales.TryGetValue(e.Id, out float currentScale))
					{
						currentScale = 1f;
						_hoverScales[e.Id] = currentScale;
					}
					float lerpSpeed = AnimationSpeed * dt;
					currentScale = MathHelper.Lerp(currentScale, targetScale, MathHelper.Clamp(lerpSpeed, 0f, 1f));
					_hoverScales[e.Id] = currentScale;
					
					// Update UI bounds size based on zoom and hover scale
					var poi = e.GetComponent<PointOfInterest>();
					if (poi != null)
					{
						Texture2D iconTexture = (poi.Type == "Hellrift" && _hellriftIconTexture != null) ? _hellriftIconTexture : _questIconTexture;
						
						// Calculate bounds size scaled by map zoom and hover scale
						float boundsWidth = IconSize * mapScale * currentScale;
						float boundsHeight = boundsWidth;
						if (iconTexture != null && iconTexture.Width > 0 && iconTexture.Height > 0)
						{
							float aspectRatio = iconTexture.Height / (float)iconTexture.Width;
							boundsHeight = boundsWidth * aspectRatio;
						}
						
						// Update bounds size and center around transform position
						// (ParallaxLayerSystem may update position, but this ensures alignment)
						ui.Bounds = new Rectangle(
							(int)System.Math.Round(t.Position.X - boundsWidth / 2f),
							(int)System.Math.Round(t.Position.Y - boundsHeight / 2f),
							(int)System.Math.Round(boundsWidth),
							(int)System.Math.Round(boundsHeight));
					}
				}
			}

			// Calculate proximity revelation for Hellrift POIs
			var poiComponents = _pois.Select(e => new { E = e, P = e.GetComponent<PointOfInterest>() })
				.Where(x => x.P != null)
				.ToList();
			
			// Get unlockers: revealed or completed POIs (excluding Hellrifts)
			var unlockers = poiComponents
				.Where(x => x.P.Type != "Hellrift" && (x.P.IsRevealed || x.P.IsCompleted))
				.ToList();
			
			// For each Hellrift, check if it's within reveal radius of any unlocker
			foreach (var hellrift in poiComponents.Where(x => x.P.Type == "Hellrift"))
			{
				bool isRevealedByProximity = false;
				foreach (var u in unlockers)
				{
					// Scale world positions by map scale for distance checks
					float dx = (hellrift.P.WorldPosition.X - u.P.WorldPosition.X) * mapScale;
					float dy = (hellrift.P.WorldPosition.Y - u.P.WorldPosition.Y) * mapScale;
					float r = (u.P.DisplayRadius > 0f) ? u.P.DisplayRadius : (u.P.IsCompleted ? u.P.RevealRadius : u.P.UnrevealedRadius);
					// Scale radius by map scale for visibility checks
					r *= mapScale;
					if ((dx * dx) + (dy * dy) <= (r * r))
					{
						isRevealedByProximity = true;
						break;
					}
				}
				hellrift.P.IsRevealedByProximity = isRevealedByProximity;
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
				
				// Determine POI type and appropriate texture
				string poiType = pos.type ?? "Quest";
				// Ensure case-insensitive matching
				if (poiType.Equals("Hellrift", System.StringComparison.OrdinalIgnoreCase))
				{
					poiType = "Hellrift";
				}
				else
				{
					poiType = "Quest";
				}
				Texture2D iconTexture = (poiType == "Hellrift" && _hellriftIconTexture != null) ? _hellriftIconTexture : _questIconTexture;
				
				// Calculate UI bounds based on actual icon dimensions
				int boundsWidth = (int)IconSize;
				int boundsHeight = (int)IconSize;
				if (iconTexture != null && iconTexture.Width > 0 && iconTexture.Height > 0)
				{
					float aspectRatio = iconTexture.Height / (float)iconTexture.Width;
					boundsHeight = (int)(IconSize * aspectRatio);
				}
				
				// UI bounds size only; Parallax will center bounds at Transform.Position when AffectsUIBounds is true
				// Hellrift POIs are not interactable
				bool isInteractable = poiType != "Hellrift";
				EntityManager.AddComponent(e, new UIElement { Bounds = new Rectangle(0, 0, boundsWidth, boundsHeight), IsInteractable = isInteractable, TooltipType = TooltipType.Quests, EventType = UIElementEventType.QuestSelect, IsPreventDefaultClick = true });
				EntityManager.AddComponent(e, ParallaxLayer.GetLocationParallaxLayer());
				// Attach POI component for fog-of-war and interactions
				var poi = new PointOfInterest {
					Id = pos.id,
					WorldPosition = pos.worldPosition,
					RevealRadius = pos.revealRadius,
					UnrevealedRadius = pos.unrevealedRadius,
					IsRevealed = pos.isRevealed,
					IsCompleted = SaveCache.IsQuestCompleted(def.id, pos.id),
					Type = poiType
				};
				// Initialize display radius consistent with current state
				if (poiType == "Hellrift")
				{
					// Hellrift POIs always show with UnrevealedRadius
					poi.DisplayRadius = poi.UnrevealedRadius;
					poi.IsRevealed = true; // Always visible
				}
				else if (poi.IsCompleted)
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
			// Hellrift POIs are always visible
			var list = _pois
				.Select(e => new { E = e, P = e.GetComponent<PointOfInterest>(), T = e.GetComponent<Transform>(), UI = e.GetComponent<UIElement>() })
				.Where(x => x.P != null && x.T != null && x.UI != null)
				.ToList();
			var unlockers = list.Where(x => x.P.IsRevealed || x.P.IsCompleted || x.P.Type == "Hellrift").ToList();
			var visibleIds = new System.Collections.Generic.HashSet<int>(unlockers.Select(x => x.E.Id));
			float mapScale = cam.MapScale;
			foreach (var x in list)
			{
				if (visibleIds.Contains(x.E.Id)) continue;
				// Hellrift POIs are always visible, skip distance check (should already be in visibleIds, but double-check)
				if (x.P.Type == "Hellrift")
				{
					visibleIds.Add(x.E.Id);
					continue;
				}
				foreach (var u in unlockers)
				{
					// Scale world positions by map scale for distance checks
					float dx = (x.P.WorldPosition.X - u.P.WorldPosition.X) * mapScale;
					float dy = (x.P.WorldPosition.Y - u.P.WorldPosition.Y) * mapScale;
					float r = (u.P.DisplayRadius > 0f) ? u.P.DisplayRadius : (u.P.IsCompleted ? u.P.RevealRadius : u.P.UnrevealedRadius);
					// Scale radius by map scale for visibility checks
					r *= mapScale;
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
				
				// Get current hover scale
				float scale = _hoverScales.TryGetValue(x.E.Id, out float s) ? s : 1f;
				
				// Determine which texture to use based on POI type
				Texture2D iconTexture = (x.P.Type == "Hellrift" && _hellriftIconTexture != null) ? _hellriftIconTexture : _questIconTexture;
				
				// Calculate icon dimensions preserving aspect ratio, scaled by map zoom
				float iconWidth = IconSize * mapScale * scale;
				float iconHeight = iconWidth;
				if (iconTexture != null && iconTexture.Width > 0 && iconTexture.Height > 0)
				{
					float aspectRatio = iconTexture.Height / (float)iconTexture.Width;
					iconHeight = iconWidth * aspectRatio;
				}
				
				// Calculate icon bounds
				float halfWidth = iconWidth / 2f;
				float halfHeight = iconHeight / 2f;
				var iconPos = new Vector2(x.T.Position.X, x.T.Position.Y);
				var iconRect = new Rectangle((int)System.Math.Round(iconPos.X - halfWidth), (int)System.Math.Round(iconPos.Y - halfHeight), (int)System.Math.Round(iconWidth), (int)System.Math.Round(iconHeight));
				
				// Skip if off-screen
				if (iconRect.Right < 0 || iconRect.Bottom < 0 || iconRect.Left > w || iconRect.Top > h) continue;
				
				// Draw icon texture
				if (iconTexture != null)
				{
					_spriteBatch.Draw(iconTexture, iconRect, Color.White);
				}
				else
				{
					// Fallback to pixel if texture failed to load
					_spriteBatch.Draw(_pixel, iconRect, x.P.IsCompleted ? Color.Green : Color.Red);
				}
				
				// Draw red circle for incomplete quests (not for Hellrift POIs)
				if (!x.P.IsCompleted && x.P.Type == "Quest")
				{
					DrawCircle(iconPos, halfWidth, halfHeight, CircleSize * mapScale * scale);
				}
			}
		}

		private void DrawCircle(Vector2 iconCenter, float iconHalfWidth, float iconHalfHeight, float circleSize)
		{
			// Position circle relative to icon center
			float offsetX = iconHalfWidth * CircleOffsetX;
			float offsetY = iconHalfHeight * CircleOffsetY;
			Vector2 circlePos = iconCenter + new Vector2(offsetX, offsetY);
			
			// Get anti-aliased circle texture
			int radius = (int)System.Math.Round(circleSize / 2f);
			if (radius < 1) radius = 1;
			var circleTexture = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, radius);
			
			// Draw circle centered at position
			Vector2 origin = new Vector2(radius, radius);
			_spriteBatch.Draw(circleTexture, circlePos, null, Color.Red, 0f, origin, 1f, SpriteEffects.None, 0f);
		}
	}
}


