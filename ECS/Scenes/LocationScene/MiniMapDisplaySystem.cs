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
	[DebugTab("Mini Map Display")]
	public class MiniMapDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _pixel;

		// Map configuration (matches LocationMapDisplaySystem)
		private const int BaseMapWidth = 6000;
		private const int BaseMapHeight = 3000;

		[DebugEditable(DisplayName = "Size Percentage", Step = 0.01f, Min = 0.05f, Max = 0.5f)]
		public float SizePercentage { get; set; } = 0.15f;

		[DebugEditable(DisplayName = "Margin", Step = 1f, Min = 0f, Max = 100f)]
		public float Margin { get; set; } = 20f;

		[DebugEditable(DisplayName = "Background Alpha", Step = 0.05f, Min = 0f, Max = 1f)]
		public float BackgroundAlpha { get; set; } = 0.7f;

		[DebugEditable(DisplayName = "Dot Size", Step = 0.5f, Min = 1f, Max = 10f)]
		public float DotSize { get; set; } = 3f;

		[DebugEditable(DisplayName = "Hellrift Dot Size", Step = 0.5f, Min = 1f, Max = 15f)]
		public float HellriftDotSize { get; set; } = 5f;

		public MiniMapDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
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
			// No-op - this system only draws
		}

		public void Draw()
		{
			// Check if we're in the Location scene
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;

			// Get location ID - try QueuedEvents first, fall back to "desert"
			string locationId = null;
			var queuedEventsEntity = EntityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault();
			if (queuedEventsEntity != null)
			{
				var queuedEvents = queuedEventsEntity.GetComponent<QueuedEvents>();
				if (!string.IsNullOrEmpty(queuedEvents?.LocationId))
				{
					locationId = queuedEvents.LocationId;
				}
			}
			if (string.IsNullOrEmpty(locationId))
			{
				locationId = "desert"; // Fall back to hardcoded "desert"
			}

			// Load location definition
			if (!LocationDefinitionCache.TryGet(locationId, out var def) || def == null || def.pointsOfInterest == null)
			{
				return;
			}

			// Get camera state
			var cam = EntityManager.GetEntity("LocationCamera")?.GetComponent<LocationCameraState>();
			if (cam == null) return;

			int viewportW = Game1.VirtualWidth;
			int viewportH = Game1.VirtualHeight;

			// Calculate minimap dimensions and position
			float minimapWidth = viewportW * SizePercentage;
			float minimapHeight = viewportH * SizePercentage;
			float minimapX = viewportW - minimapWidth - Margin;
			float minimapY = viewportH - minimapHeight - Margin;
			var minimapRect = new Rectangle((int)minimapX, (int)minimapY, (int)minimapWidth, (int)minimapHeight);

			// Calculate map dimensions (scaled by MapScale)
			float scaledMapWidth = BaseMapWidth * cam.MapScale;
			float scaledMapHeight = BaseMapHeight * cam.MapScale;

			// Calculate scale factor to fit map into minimap
			float scaleX = minimapWidth / scaledMapWidth;
			float scaleY = minimapHeight / scaledMapHeight;
			float scale = MathHelper.Min(scaleX, scaleY); // Use smaller scale to fit entire map

			// Actual minimap size (may be smaller if aspect ratio differs)
			float actualMinimapWidth = scaledMapWidth * scale;
			float actualMinimapHeight = scaledMapHeight * scale;
			float actualMinimapX = minimapX + (minimapWidth - actualMinimapWidth) / 2f;
			float actualMinimapY = minimapY + (minimapHeight - actualMinimapHeight) / 2f;

			// Draw minimap background (slightly transparent black)
			var bgColor = new Color((byte)0, (byte)0, (byte)0, (byte)(255 * BackgroundAlpha));
			_spriteBatch.Draw(_pixel, minimapRect, bgColor);

			// Draw white border around minimap
			var borderColor = Color.White;
			int borderWidth = 2;
			// Top border
			_spriteBatch.Draw(_pixel, new Rectangle((int)minimapX, (int)minimapY, (int)minimapWidth, borderWidth), borderColor);
			// Bottom border
			_spriteBatch.Draw(_pixel, new Rectangle((int)minimapX, (int)(minimapY + minimapHeight - borderWidth), (int)minimapWidth, borderWidth), borderColor);
			// Left border
			_spriteBatch.Draw(_pixel, new Rectangle((int)minimapX, (int)minimapY, borderWidth, (int)minimapHeight), borderColor);
			// Right border
			_spriteBatch.Draw(_pixel, new Rectangle((int)(minimapX + minimapWidth - borderWidth), (int)minimapY, borderWidth, (int)minimapHeight), borderColor);

			// Draw POIs (filtered visibility)
			var poiEntities = EntityManager.GetEntitiesWithComponent<PointOfInterest>()?.ToList();
			if (poiEntities != null && poiEntities.Count > 0)
			{
				// Use live POI components when available
				var pois = poiEntities
					.Select(e => e.GetComponent<PointOfInterest>())
					.Where(p => p != null)
					.ToList();

				var unlockers = pois.Where(p => p.IsRevealed || p.IsCompleted).ToList();

				foreach (var p in pois)
				{
					bool isHellrift = p.Type != null && p.Type == PointOfInterestType.Hellrift;
					bool isCompleted = p.IsCompleted;
					// Visible if: always Hellrift, or completed, or revealed, or proximity-visible
					bool isVisible = isHellrift || isCompleted || p.IsRevealed || IsVisibleByProximity(p, unlockers, cam.MapScale);
					if (!isVisible) continue;

					float minimapPoiX = actualMinimapX + (p.WorldPosition.X * cam.MapScale * scale);
					float minimapPoiY = actualMinimapY + (p.WorldPosition.Y * cam.MapScale * scale);

					if (minimapPoiX < minimapX || minimapPoiX > minimapX + minimapWidth ||
						minimapPoiY < minimapY || minimapPoiY > minimapY + minimapHeight)
					{
						continue;
					}

					float dotSize = (isHellrift && !isCompleted) ? HellriftDotSize : DotSize;
					Color dotColor = isCompleted ? Color.White : Color.Red;

					var dotRect = new Rectangle(
						(int)(minimapPoiX - dotSize / 2f),
						(int)(minimapPoiY - dotSize / 2f),
						(int)dotSize,
						(int)dotSize
					);
					_spriteBatch.Draw(_pixel, dotRect, dotColor);
				}
			}
			else
			{
				// Fallback to definitions when live POIs are not present
				var defUnlockers = def.pointsOfInterest
					.Where(p => p != null && (p.isRevealed || SaveCache.IsQuestCompleted(locationId, p.id)))
					.ToList();

				foreach (var poi in def.pointsOfInterest)
				{
					if (poi == null) continue;

					bool isCompleted = SaveCache.IsQuestCompleted(locationId, poi.id);
					bool isHellrift = poi.type == PointOfInterestType.Hellrift;
					bool isVisible = isHellrift || isCompleted || poi.isRevealed || IsVisibleByProximityDef(poi, defUnlockers, locationId, cam.MapScale);
					if (!isVisible) continue;

					float minimapPoiX = actualMinimapX + (poi.worldPosition.X * cam.MapScale * scale);
					float minimapPoiY = actualMinimapY + (poi.worldPosition.Y * cam.MapScale * scale);

					if (minimapPoiX < minimapX || minimapPoiX > minimapX + minimapWidth ||
						minimapPoiY < minimapY || minimapPoiY > minimapY + minimapHeight)
					{
						continue;
					}

					float dotSize = (isHellrift && !isCompleted) ? HellriftDotSize : DotSize;
					Color dotColor = isCompleted ? Color.White : Color.Red;

					var dotRect = new Rectangle(
						(int)(minimapPoiX - dotSize / 2f),
						(int)(minimapPoiY - dotSize / 2f),
						(int)dotSize,
						(int)dotSize
					);
					_spriteBatch.Draw(_pixel, dotRect, dotColor);
				}
			}

			// Draw camera viewport rectangle
			// Camera origin is the top-left of the viewport in world space
			float cameraMinX = cam.Origin.X;
			float cameraMinY = cam.Origin.Y;
			float cameraMaxX = cam.Origin.X + cam.ViewportW;
			float cameraMaxY = cam.Origin.Y + cam.ViewportH;

			// Convert camera bounds to minimap coordinates
			float minimapCamMinX = actualMinimapX + (cameraMinX * scale);
			float minimapCamMinY = actualMinimapY + (cameraMinY * scale);
			float minimapCamMaxX = actualMinimapX + (cameraMaxX * scale);
			float minimapCamMaxY = actualMinimapY + (cameraMaxY * scale);

			// Clamp camera rectangle to minimap bounds
			minimapCamMinX = MathHelper.Clamp(minimapCamMinX, minimapX, minimapX + minimapWidth);
			minimapCamMinY = MathHelper.Clamp(minimapCamMinY, minimapY, minimapY + minimapHeight);
			minimapCamMaxX = MathHelper.Clamp(minimapCamMaxX, minimapX, minimapX + minimapWidth);
			minimapCamMaxY = MathHelper.Clamp(minimapCamMaxY, minimapY, minimapY + minimapHeight);

			// Draw white outline rectangle for camera viewport
			int camRectWidth = (int)(minimapCamMaxX - minimapCamMinX);
			int camRectHeight = (int)(minimapCamMaxY - minimapCamMinY);
			int outlineWidth = 1;

			if (camRectWidth > 0 && camRectHeight > 0)
			{
				// Top border
				_spriteBatch.Draw(_pixel, new Rectangle((int)minimapCamMinX, (int)minimapCamMinY, camRectWidth, outlineWidth), borderColor);
				// Bottom border
				_spriteBatch.Draw(_pixel, new Rectangle((int)minimapCamMinX, (int)(minimapCamMaxY - outlineWidth), camRectWidth, outlineWidth), borderColor);
				// Left border
				_spriteBatch.Draw(_pixel, new Rectangle((int)minimapCamMinX, (int)minimapCamMinY, outlineWidth, camRectHeight), borderColor);
				// Right border
				_spriteBatch.Draw(_pixel, new Rectangle((int)(minimapCamMaxX - outlineWidth), (int)minimapCamMinY, outlineWidth, camRectHeight), borderColor);
			}
		}

		private static bool IsVisibleByProximity(PointOfInterest poi, List<PointOfInterest> unlockers, float mapScale)
		{
			if (unlockers == null || unlockers.Count == 0) return false;
			foreach (var u in unlockers)
			{
				float dx = (poi.WorldPosition.X - u.WorldPosition.X) * mapScale;
				float dy = (poi.WorldPosition.Y - u.WorldPosition.Y) * mapScale;
				float r = (u.DisplayRadius > 0f) ? u.DisplayRadius : (u.IsCompleted ? u.RevealRadius : u.UnrevealedRadius);
				r *= mapScale;
				if ((dx * dx) + (dy * dy) <= (r * r)) return true;
			}
			return false;
		}

		private static bool IsVisibleByProximityDef(PointOfInterestDefinition poi, List<PointOfInterestDefinition> unlockers, string locationId, float mapScale)
		{
			if (unlockers == null || unlockers.Count == 0) return false;
			foreach (var u in unlockers)
			{
				float dx = (poi.worldPosition.X - u.worldPosition.X) * mapScale;
				float dy = (poi.worldPosition.Y - u.worldPosition.Y) * mapScale;
				bool uCompleted = SaveCache.IsQuestCompleted(locationId, u.id);
				float r = (uCompleted ? u.revealRadius : u.unrevealedRadius) * mapScale;
				if ((dx * dx) + (dy * dy) <= (r * r)) return true;
			}
			return false;
		}
	}
}

