using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Cursor System")]
	public class CursorSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private Texture2D _circleTexture;
		private Vector2 _cursorPosition;
		private int _lastViewportW = -1;
		private int _lastViewportH = -1;
		private GamePadState _prevGamePadState;
		private Entity _lastClickedEntity;
		private Entity _lastHoveredEntity;

		[DebugEditable(DisplayName = "Cursor Radius (px)", Step = 1f, Min = 2f, Max = 256f)]
		public int CursorRadius { get; set; } = 40;

		[DebugEditable(DisplayName = "Base Speed (px/s)", Step = 10f, Min = 50f, Max = 4000f)]
		public float BaseSpeed { get; set; } = 1450f;

		[DebugEditable(DisplayName = "Analog Deadzone", Step = 0.01f, Min = 0f, Max = 0.5f)]
		public float Deadzone { get; set; } = 0.25f;

		[DebugEditable(DisplayName = "Speed Exponent", Step = 0.05f, Min = 0.25f, Max = 3f)]
		public float SpeedExponent { get; set; } = 1.0f;

		[DebugEditable(DisplayName = "Max Multiplier", Step = 0.1f, Min = 0.5f, Max = 5f)]
		public float MaxMultiplier { get; set; } = 1.0f;

		[DebugEditable(DisplayName = "LT Speed Multiplier", Step = 0.1f, Min = 0.1f, Max = 10f)]
		public float LtSpeedMultiplier { get; set; } = 2.0f;

		[DebugEditable(DisplayName = "UI Slowdown Coverage Threshold", Step = 0.01f, Min = 0f, Max = 1f)]
		public float SlowdownCoverageThreshold { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "UI Slowdown Multiplier", Step = 0.05f, Min = 0.05f, Max = 1f)]
		public float SlowdownMultiplier { get; set; } = 0.7f;

		public CursorSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
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
			// Reset last clicked flag from previous frame (one-shot click)
			if (_lastClickedEntity != null)
			{
				var uiPrev = _lastClickedEntity.GetComponent<UIElement>();
				if (uiPrev != null) uiPrev.IsClicked = false;
				_lastClickedEntity = null;
			}
			// Clear last hovered from previous frame
			if (_lastHoveredEntity != null)
			{
				var uiPrevH = _lastHoveredEntity.GetComponent<UIElement>();
				if (uiPrevH != null) uiPrevH.IsHovered = false;
				_lastHoveredEntity = null;
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
			bool ignoringTransitions = TransitionStateSingleton.IsActive;

			// Determine top-most UI under cursor center and flag hover
			var point = new Point((int)System.Math.Round(_cursorPosition.X), (int)System.Math.Round(_cursorPosition.Y));
			var topCandidate = (object)null;
			if (!ignoringTransitions)
			{
				var tc = EntityManager.GetEntitiesWithComponent<UIElement>()
					.Select(e2 => new { E = e2, UI = e2.GetComponent<UIElement>(), T = e2.GetComponent<Transform>() })
					.Where(x => x.UI != null && x.UI.IsInteractable && x.UI.Bounds.Width >= 2 && x.UI.Bounds.Height >= 2 && x.UI.Bounds.Contains(point))
					.OrderByDescending(x => x.T?.ZOrder ?? 0)
					.FirstOrDefault();
				if (tc != null)
				{
					tc.UI.IsHovered = true;
					_lastHoveredEntity = tc.E;
				}
				topCandidate = tc;
			}

			// A button edge-triggered click on the same top-most UI
			bool aPressed = gp.Buttons.A == ButtonState.Pressed;
			bool aPrevPressed = _prevGamePadState.Buttons.A == ButtonState.Pressed;
			bool aEdge = aPressed && !aPrevPressed;
			if (aEdge && !ignoringTransitions && topCandidate != null)
			{
				var tc = (dynamic)topCandidate;
				tc.UI.IsClicked = true;
				_lastClickedEntity = tc.E;
			}

			// Publish cursor state event for other systems
			int rForCoverage = System.Math.Max(1, CursorRadius);
			float coverageForTop = 0f;
			if (!ignoringTransitions && topCandidate != null)
			{
				var tc = (dynamic)topCandidate;
				coverageForTop = EstimateCircleRectCoverage(tc.UI.Bounds, _cursorPosition, rForCoverage);
			}
			EventManager.Publish(new CursorStateEvent
			{
				Position = _cursorPosition,
				IsAPressed = aPressed,
				IsAPressedEdge = aEdge,
				Coverage = coverageForTop,
				TopEntity = ignoringTransitions ? null : ((topCandidate == null) ? null : ((dynamic)topCandidate).E)
			});

			_prevGamePadState = gp;

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

			// Slow down when overlapping UI elements beyond threshold
			int r = System.Math.Max(1, CursorRadius);
			float maxCoverage = 0f;
			if (!ignoringTransitions)
			{
				foreach (var e2 in EntityManager.GetEntitiesWithComponent<UIElement>())
				{
					var ui2 = e2.GetComponent<UIElement>();
					if (ui2 == null || !ui2.IsInteractable) continue;
					var bounds2 = ui2.Bounds;
					if (bounds2.Width < 2 || bounds2.Height < 2) continue;
					maxCoverage = System.Math.Max(maxCoverage, EstimateCircleRectCoverage(bounds2, _cursorPosition, r));
				}
			}
			if (maxCoverage >= MathHelper.Clamp(SlowdownCoverageThreshold, 0f, 1f))
			{
				speedMultiplier *= MathHelper.Clamp(SlowdownMultiplier, 0.05f, 1f);
			}
      float rt = gp.Triggers.Right;
			if (rt > 0.1f)
			{
				speedMultiplier *= LtSpeedMultiplier;
			}
			// Optional right-trigger speed boost (pressure-insensitive)
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
			// Only draw the cursor if a controller is connected
			var gp = GamePad.GetState(PlayerIndex.One);
			if (!gp.IsConnected) return;
			int r = System.Math.Max(1, CursorRadius);
			_circleTexture = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, r);
			var dst = new Rectangle((int)System.Math.Round(_cursorPosition.X) - r, (int)System.Math.Round(_cursorPosition.Y) - r, r * 2, r * 2);

			// Compute redness based on overlap with interactable UIElement bounds
			float maxCoverage = 0f;
			foreach (var e in EntityManager.GetEntitiesWithComponent<UIElement>())
			{
				var ui = e.GetComponent<UIElement>();
				if (ui == null || !ui.IsInteractable) continue;
				var bounds = ui.Bounds;
				if (bounds.Width < 2 || bounds.Height < 2) continue;
				maxCoverage = System.Math.Max(maxCoverage, EstimateCircleRectCoverage(bounds, _cursorPosition, r));
			}
			maxCoverage = MathHelper.Clamp(maxCoverage, 0f, 1f);
			var tint = Color.Lerp(Color.White, Color.Red, maxCoverage);
			_spriteBatch.Draw(_circleTexture, dst, tint);
		}

		private static float EstimateCircleRectCoverage(Rectangle rect, Vector2 center, int radius)
		{
			// Sample-based approximation: fraction of circle area inside the rectangle
			int samplesPerAxis = 8;
			int insideCount = 0;
			int totalCount = 0;
			float left = center.X - radius;
			float top = center.Y - radius;
			float step = (radius * 2f) / samplesPerAxis;
			float r2 = radius * radius;
			for (int iy = 0; iy < samplesPerAxis; iy++)
			{
				for (int ix = 0; ix < samplesPerAxis; ix++)
				{
					// sample at cell center
					float sx = left + (ix + 0.5f) * step;
					float sy = top + (iy + 0.5f) * step;
					float dx = sx - center.X;
					float dy = sy - center.Y;
					if (dx * dx + dy * dy <= r2)
					{
						totalCount++;
						if (rect.Contains((int)System.Math.Round(sx), (int)System.Math.Round(sy))) insideCount++;
					}
				}
			}
			if (totalCount <= 0) return 0f;
			return insideCount / (float)totalCount;
		}
	}
}


