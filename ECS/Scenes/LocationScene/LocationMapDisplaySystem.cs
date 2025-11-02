using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Location Map Display")]
	public class LocationMapDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _pixel;
		private readonly Texture2D _backgroundTexture;

		private int _lastViewportW = -1;
		private int _lastViewportH = -1;
		private Vector2 _cameraCenter;
		private bool _locked;
		private bool _hasPannedOnLoad = false;

		// Map configuration
		private const int BaseMapWidth = 6000;
		private const int BaseMapHeight = 3000;

		[DebugEditable(DisplayName = "Map Scale", Step = 0.1f, Min = 0.1f, Max = 5f)]
		public float MapScale { get; set; } = 1.0f;

		public float MapWidth => BaseMapWidth * MapScale;
		public float MapHeight => BaseMapHeight * MapScale;

		// Camera feel
		[DebugEditable(DisplayName = "Pan Speed (px/s)", Step = 10f, Min = 50f, Max = 5000f)]
		public float BasePanSpeed { get; set; } = 500f;

		[DebugEditable(DisplayName = "Right Stick Deadzone", Step = 0.01f, Min = 0f, Max = 0.5f)]
		public float Deadzone { get; set; } = 0.1f;

		[DebugEditable(DisplayName = "Speed Exponent", Step = 0.1f, Min = 0.1f, Max = 5f)]
		public float SpeedExponent { get; set; } = 1.2f;

		[DebugEditable(DisplayName = "Max Multiplier", Step = 0.1f, Min = 1f, Max = 10f)]
		public float MaxMultiplier { get; set; } = 3f;

		public LocationMapDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
			_backgroundTexture = content.Load<Texture2D>("desert_background_location");
			EventManager.Subscribe<LockLocationCameraEvent>(_ => { _locked = _.Locked; });
			EventManager.Subscribe<FocusLocationCameraEvent>(_ => {
				int w = _graphicsDevice.Viewport.Width;
				int h = _graphicsDevice.Viewport.Height;
				_cameraCenter = _.WorldPos;
				ClampCamera(ref _cameraCenter, w, h);
				PublishCameraState(w, h);
			});
			EventManager.Subscribe<LoadSceneEvent>(_ => {
				if (_.Scene != SceneId.Location)
				{
					_hasPannedOnLoad = false;
					return;
				}
				// Reset flag when location scene loads
				_hasPannedOnLoad = false;
			});
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			// Update once per frame via SceneState presence
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;
			int w = _graphicsDevice.Viewport.Width;
			int h = _graphicsDevice.Viewport.Height;
			if (_lastViewportW != w || _lastViewportH != h)
			{
				_lastViewportW = w;
				_lastViewportH = h;
				_cameraCenter = new Vector2(MapWidth / 2f, MapHeight / 2f);
				ClampCamera(ref _cameraCenter, w, h);
			}

			// Auto-pan camera on scene load (only if not coming from quest completion)
			if (!_hasPannedOnLoad && !TransitionStateSingleton.HasPendingLocationPoiReveal)
			{
				TryAutoPanCamera();
				_hasPannedOnLoad = true;
			}

			if (!Game1.WindowIsActive)
			{
				PublishCameraState(w, h);
				return;
			}

			if (_locked)
			{
				PublishCameraState(w, h);
				return;
			}

			// Gather combined input (keyboard + gamepad)
			Vector2 velocity = Vector2.Zero;

			// Keyboard (WASD) — screen-space: up is negative Y
			var ks = Keyboard.GetState();
			int xAxis = (ks.IsKeyDown(Keys.D) ? 1 : 0) - (ks.IsKeyDown(Keys.A) ? 1 : 0);
			int yAxis = (ks.IsKeyDown(Keys.S) ? 1 : 0) - (ks.IsKeyDown(Keys.W) ? 1 : 0);
			if (xAxis != 0 || yAxis != 0)
			{
				var kbDir = new Vector2(xAxis, yAxis);
				if (kbDir.LengthSquared() > 1f) kbDir.Normalize();
				velocity += kbDir * BasePanSpeed;
			}

			// Gamepad right stick (keep existing feel)
			var gp = GamePad.GetState(PlayerIndex.One);
			if (gp.IsConnected)
			{
				Vector2 stick = gp.ThumbSticks.Right; // X: right+, Y: up+
				float mag = stick.Length();
				if (mag >= Deadzone)
				{
					Vector2 dir = stick / mag;
					float normalized = MathHelper.Clamp((mag - Deadzone) / (1f - Deadzone), 0f, 1f);
					float speedMultiplier = MathHelper.Clamp((float)Math.Pow(normalized, SpeedExponent) * MaxMultiplier, 0f, 10f);
					// In screen space, up is negative Y
					velocity += new Vector2(dir.X, -dir.Y) * BasePanSpeed * speedMultiplier;
				}
			}

			if (velocity == Vector2.Zero)
			{
				PublishCameraState(w, h);
				return;
			}

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_cameraCenter += velocity * dt;
			ClampCamera(ref _cameraCenter, w, h);
			PublishCameraState(w, h);
		}

		private void PublishCameraState(int viewportW, int viewportH)
		{
			var camEntity = EntityManager.GetEntity("LocationCamera");
			if (camEntity == null)
			{
				camEntity = EntityManager.CreateEntity("LocationCamera");
			}
			var state = camEntity.GetComponent<LocationCameraState>();
			if (state == null)
			{
				state = new LocationCameraState();
				EntityManager.AddComponent(camEntity, state);
			}
			float w = viewportW;
			float h = viewportH;
			var origin = new Vector2(_cameraCenter.X - w / 2f, _cameraCenter.Y - h / 2f);
			origin.X = Math.Max(0f, origin.X);
			origin.Y = Math.Max(0f, origin.Y);
			state.Center = _cameraCenter;
			state.Origin = origin;
			state.ViewportW = viewportW;
			state.ViewportH = viewportH;
		}

		private void TryAutoPanCamera()
		{
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
				locationId = "desert"; // Fall back to hardcoded "desert" matching PointOfInterestDisplaySystem
			}

			// Load location definition
			if (!LocationDefinitionCache.TryGet(locationId, out var def) || def == null || def.pointsOfInterest == null)
			{
				return;
			}

			// Find target POI: furthest down completed quest, or first revealed POI
			PointOfInterestDefinition targetPoi = null;

			// First, try to find the furthest down completed quest (highest index)
			for (int i = def.pointsOfInterest.Count - 1; i >= 0; i--)
			{
				var poi = def.pointsOfInterest[i];
				if (SaveCache.IsQuestCompleted(locationId, poi.id))
				{
					targetPoi = poi;
					break;
				}
			}

			// If no completed quests, find first revealed POI
			if (targetPoi == null)
			{
				targetPoi = def.pointsOfInterest.FirstOrDefault(poi => poi.isRevealed);
			}

			// Pan camera to target POI if found
			if (targetPoi != null)
			{
				EventManager.Publish(new FocusLocationCameraEvent { WorldPos = targetPoi.worldPosition });
			}
		}

		private void ClampCamera(ref Vector2 center, int viewportW, int viewportH)
		{
			float halfW = viewportW / 2f;
			float halfH = viewportH / 2f;
			float minX = halfW;
			float minY = halfH;
			float maxX = Math.Max(halfW, MapWidth - halfW);
			float maxY = Math.Max(halfH, MapHeight - halfH);
			center.X = MathHelper.Clamp(center.X, minX, maxX);
			center.Y = MathHelper.Clamp(center.Y, minY, maxY);
		}

		public void Draw()
		{
			int w = _graphicsDevice.Viewport.Width;
			int h = _graphicsDevice.Viewport.Height;
			if (_lastViewportW < 0 || _lastViewportH < 0)
			{
				_lastViewportW = w;
				_lastViewportH = h;
				_cameraCenter = new Vector2(MapWidth / 2f, MapHeight / 2f);
				ClampCamera(ref _cameraCenter, w, h);
			}

			Vector2 origin = new Vector2(_cameraCenter.X - w / 2f, _cameraCenter.Y - h / 2f);
			origin.X = Math.Max(0f, origin.X);
			origin.Y = Math.Max(0f, origin.Y);

			float scaledMapWidth = MapWidth;
			float scaledMapHeight = MapHeight;

			// Calculate the visible portion of the map
			float visibleLeft = Math.Max(0f, origin.X);
			float visibleTop = Math.Max(0f, origin.Y);
			float visibleRight = Math.Min(scaledMapWidth, origin.X + w);
			float visibleBottom = Math.Min(scaledMapHeight, origin.Y + h);
			float visibleWidth = visibleRight - visibleLeft;
			float visibleHeight = visibleBottom - visibleTop;

			if (visibleWidth > 0 && visibleHeight > 0)
			{
				// Calculate source rectangle (proportional to texture size)
				float sourceLeft = visibleLeft / scaledMapWidth;
				float sourceTop = visibleTop / scaledMapHeight;
				float sourceWidth = visibleWidth / scaledMapWidth;
				float sourceHeight = visibleHeight / scaledMapHeight;

				var sourceRect = new Rectangle(
					(int)(sourceLeft * _backgroundTexture.Width),
					(int)(sourceTop * _backgroundTexture.Height),
					(int)(sourceWidth * _backgroundTexture.Width),
					(int)(sourceHeight * _backgroundTexture.Height)
				);

				// Draw the visible portion of the background texture
				var destRect = new Rectangle(
					(int)(visibleLeft - origin.X),
					(int)(visibleTop - origin.Y),
					(int)visibleWidth,
					(int)visibleHeight
				);

				_spriteBatch.Draw(_backgroundTexture, destRect, sourceRect, Color.White);
			}
		}
	}
}


