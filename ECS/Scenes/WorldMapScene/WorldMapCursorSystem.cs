using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("WorldMap Cursor")]
	public class WorldMapCursorSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private Texture2D _circleTexture;
		private Vector2 _cursorPosition;
		private int _lastViewportW = -1;
		private int _lastViewportH = -1;

		[DebugEditable(DisplayName = "Cursor Radius (px)", Step = 1f, Min = 2f, Max = 256f)]
		public int CursorRadius { get; set; } = 40;

		[DebugEditable(DisplayName = "Base Speed (px/s)", Step = 10f, Min = 50f, Max = 4000f)]
		public float BaseSpeed { get; set; } = 1450f;

		[DebugEditable(DisplayName = "Analog Deadzone", Step = 0.01f, Min = 0f, Max = 0.5f)]
		public float Deadzone { get; set; } = 0.35f;

		[DebugEditable(DisplayName = "Speed Exponent", Step = 0.05f, Min = 0.25f, Max = 3f)]
		public float SpeedExponent { get; set; } = 1.0f;

		[DebugEditable(DisplayName = "Max Multiplier", Step = 0.1f, Min = 0.5f, Max = 5f)]
		public float MaxMultiplier { get; set; } = 1.0f;

		[DebugEditable(DisplayName = "LT Speed Multiplier", Step = 0.1f, Min = 0.1f, Max = 10f)]
		public float LtSpeedMultiplier { get; set; } = 2.0f;

		public WorldMapCursorSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.WorldMap)
			{
				_lastViewportW = -1;
				_lastViewportH = -1;
				return;
			}

			// Initialize position centered on first entry to scene or when viewport changes
			int w = _graphicsDevice.Viewport.Width;
			int h = _graphicsDevice.Viewport.Height;
			if (w != _lastViewportW || h != _lastViewportH)
			{
				_lastViewportW = w;
				_lastViewportH = h;
				_cursorPosition = new Vector2(w / 2f, h / 2f);
			}

			// Ignore input if game window inactive
			if (!Crusaders30XX.Game1.WindowIsActive) return;

			var gp = GamePad.GetState(PlayerIndex.One);
			Vector2 stick = gp.ThumbSticks.Left; // X: right+, Y: up+
			// Apply circular deadzone
			float mag = stick.Length();
			if (mag < Deadzone)
			{
				return;
			}

			// Normalize and scale by exponent curve
			Vector2 dir = (mag > 0f) ? (stick / mag) : Vector2.Zero;
			float normalized = MathHelper.Clamp((mag - Deadzone) / (1f - Deadzone), 0f, 1f);
			float speedMultiplier = MathHelper.Clamp((float)System.Math.Pow(normalized, SpeedExponent) * MaxMultiplier, 0f, 10f);
      float rt = gp.Triggers.Right;
			if (rt > 0.1f)
			{
				speedMultiplier *= LtSpeedMultiplier;
			}
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			// In screen space, up on stick is negative Y
			Vector2 velocity = new Vector2(dir.X, -dir.Y) * BaseSpeed * speedMultiplier;
			_cursorPosition += velocity * dt;

			// Clamp cursor center to remain within the screen (allowing the circle to go offscreen)
			_cursorPosition.X = MathHelper.Clamp(_cursorPosition.X, 0f, w);
			_cursorPosition.Y = MathHelper.Clamp(_cursorPosition.Y, 0f, h);
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.WorldMap) return;

			// Hide cursor while quest-select overlay is open
			var qs = EntityManager.GetEntitiesWithComponent<QuestSelectState>().FirstOrDefault()?.GetComponent<QuestSelectState>();
			if (qs != null && qs.IsOpen) return;

			int r = System.Math.Max(1, CursorRadius);
			_circleTexture = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, r);
			var dst = new Rectangle((int)System.Math.Round(_cursorPosition.X) - r, (int)System.Math.Round(_cursorPosition.Y) - r, r * 2, r * 2);
			_spriteBatch.Draw(_circleTexture, dst, Color.White);
		}
	}
}


