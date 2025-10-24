using System;
using System.Collections.Generic;
using Crusaders30XX;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
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

		private int _lastViewportW = -1;
		private int _lastViewportH = -1;
		private Vector2 _cameraCenter;

		// Map configuration
		public const int MapWidth = 6000;
		public const int MapHeight = 3000;
		public const int TileSize = 300;

		// Camera feel
		[DebugEditable(DisplayName = "Pan Speed (px/s)", Step = 10f, Min = 50f, Max = 5000f)]
		public float BasePanSpeed { get; set; } = 500f;

		[DebugEditable(DisplayName = "Right Stick Deadzone", Step = 0.01f, Min = 0f, Max = 0.5f)]
		public float Deadzone { get; set; } = 0.1f;

		[DebugEditable(DisplayName = "Speed Exponent", Step = 0.1f, Min = 0.1f, Max = 5f)]
		public float SpeedExponent { get; set; } = 1.2f;

		[DebugEditable(DisplayName = "Max Multiplier", Step = 0.1f, Min = 1f, Max = 10f)]
		public float MaxMultiplier { get; set; } = 3f;

		public LocationMapDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
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

			if (!Game1.WindowIsActive)
			{
				PublishCameraState(w, h);
				return;
			}

			var gp = GamePad.GetState(PlayerIndex.One);
			if (!gp.IsConnected)
			{
				PublishCameraState(w, h);
				return;
			}

			Vector2 stick = gp.ThumbSticks.Right; // X: right+, Y: up+
			float mag = stick.Length();
			if (mag < Deadzone)
			{
				PublishCameraState(w, h);
				return;
			}

			Vector2 dir = (mag > 0f) ? (stick / mag) : Vector2.Zero;
			float normalized = MathHelper.Clamp((mag - Deadzone) / (1f - Deadzone), 0f, 1f);
			float speedMultiplier = MathHelper.Clamp((float)Math.Pow(normalized, SpeedExponent) * MaxMultiplier, 0f, 10f);
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			// In screen space, up is negative Y
			Vector2 velocity = new Vector2(dir.X, -dir.Y) * BasePanSpeed * speedMultiplier;
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

		private static void ClampCamera(ref Vector2 center, int viewportW, int viewportH)
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

			float right = Math.Min(MapWidth, origin.X + w);
			float bottom = Math.Min(MapHeight, origin.Y + h);

			int startTileX = (int)Math.Floor(origin.X / TileSize);
			int startTileY = (int)Math.Floor(origin.Y / TileSize);
			int endTileX = (int)Math.Floor((right - 1) / TileSize);
			int endTileY = (int)Math.Floor((bottom - 1) / TileSize);

			for (int ty = startTileY; ty <= endTileY; ty++)
			{
				for (int tx = startTileX; tx <= endTileX; tx++)
				{
					int worldX = tx * TileSize;
					int worldY = ty * TileSize;
					int width = Math.Min(TileSize, MapWidth - worldX);
					int height = Math.Min(TileSize, MapHeight - worldY);
					if (width <= 0 || height <= 0) continue;

					int screenX = (int)Math.Round(worldX - origin.X);
					int screenY = (int)Math.Round(worldY - origin.Y);
					var dest = new Rectangle(screenX, screenY, width, height);
					bool red = ((tx + ty) % 2 == 0);
					var color = red ? Color.LightGreen : Color.LightGray;
					_spriteBatch.Draw(_pixel, dest, color);
				}
			}
		}
	}
}


