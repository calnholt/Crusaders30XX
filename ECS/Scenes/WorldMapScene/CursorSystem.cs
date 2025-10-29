using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Events;
using System;
using Microsoft.Xna.Framework.Content;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Cursor System")]
	public class CursorSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private Texture2D _circleTexture;
		private Texture2D _cursorCross;
		private Vector2 _cursorPosition;
		private int _lastViewportW = -1;
		private int _lastViewportH = -1;
		private GamePadState _prevGamePadState;
		private MouseState _prevMouseState;
		private Entity _lastClickedEntity;
		private Entity _lastHoveredEntity;
		private Entity _prevHoverEntityForRumble;
		private float _rumbleTimeRemaining;
		private bool _isEnabled = true;

		// Cursor cross debug + animation
		[DebugEditable(DisplayName = "Cross Scale Multiplier", Step = 0.05f, Min = 0.25f, Max = 3f)]
		public float CrossScale { get; set; } = 1.0f;

		[DebugEditable(DisplayName = "Cross Anim Speed", Step = 1f, Min = 1f, Max = 60f)]
		public float CrossAnimSpeed { get; set; } = 16f;

		private const float HoverScale = 0.9f;
		private const float EnterPulseExtra = 0.06f;
		private const float EnterPulseDuration = 0.06f;
		private float _crossScaleCurrent = 1f;
		private float _crossPulseTimer = 0f;
		private Entity _prevHoverInteractable;

		[DebugEditable(DisplayName = "Cursor Radius (px)", Step = 1f, Min = 2f, Max = 256f)]
		public int CursorRadius { get; set; } = 40;

		[DebugEditable(DisplayName = "Base Speed (px/s)", Step = 10f, Min = 50f, Max = 4000f)]
		public float BaseSpeed { get; set; } = 1450f;

		[DebugEditable(DisplayName = "Analog Deadzone", Step = 0.01f, Min = 0f, Max = 0.5f)]
		public float Deadzone { get; set; } = 0f;

		[DebugEditable(DisplayName = "Speed Exponent", Step = 0.05f, Min = 0.25f, Max = 3f)]
		public float SpeedExponent { get; set; } = 1.0f;

		[DebugEditable(DisplayName = "Max Multiplier", Step = 0.1f, Min = 0.5f, Max = 5f)]
		public float MaxMultiplier { get; set; } = 1.0f;

		[DebugEditable(DisplayName = "LT Speed Multiplier", Step = 0.1f, Min = 0.1f, Max = 10f)]
		public float LtSpeedMultiplier { get; set; } = 2.0f;

		[DebugEditable(DisplayName = "UI Slowdown Coverage Threshold", Step = 0.01f, Min = 0f, Max = 1f)]
		public float SlowdownCoverageThreshold { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "UI Slowdown Multiplier", Step = 0.05f, Min = 0.05f, Max = 1f)]
		public float SlowdownMultiplier { get; set; } = 0.4f;

		[DebugEditable(DisplayName = "Cursor Opacity", Step = 0.05f, Min = 0f, Max = 1f)]
		public float CursorOpacity { get; set; } = .45f;

		[DebugEditable(DisplayName = "Hitbox Radius (px)", Step = 1f, Min = 0f, Max = 256f)]
		public int HitboxRadius { get; set; } = 34;

		[DebugEditable(DisplayName = "Rumble Duration (s)", Step = 0.01f, Min = 0f, Max = 1f)]
		public float RumbleDurationSeconds { get; set; } = 0.04f;

		[DebugEditable(DisplayName = "Rumble Low Intensity", Step = 0.05f, Min = 0f, Max = 1f)]
		public float RumbleLow { get; set; } = 0.3f;

		[DebugEditable(DisplayName = "Rumble High Intensity", Step = 0.05f, Min = 0f, Max = 1f)]
		public float RumbleHigh { get; set; } = 0.2f;

		public CursorSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			// Load cross texture once
			_cursorCross = _content.Load<Texture2D>("cursor_cross");
			EventManager.Subscribe<SetCursorEnabledEvent>(_ => { _isEnabled = _.Enabled; });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (!_isEnabled) return;
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
			if (!Game1.WindowIsActive) return;

			var gp = GamePad.GetState(PlayerIndex.One);
			bool useGamepad = gp.IsConnected;
			Vector2 stick = useGamepad ? gp.ThumbSticks.Left : Vector2.Zero; // X: right+, Y: up+
			bool ignoringTransitions = TransitionStateSingleton.IsActive;

			// Clear hover state on all UI elements to ensure exclusivity
			foreach (var e2 in EntityManager.GetEntitiesWithComponent<UIElement>())
			{
				var ui2 = e2.GetComponent<UIElement>();
				if (ui2 != null) ui2.IsHovered = false;
			}

			// Determine top-most UI intersecting the cursor and flag hover
			var point = new Point((int)Math.Round(_cursorPosition.X), (int)Math.Round(_cursorPosition.Y));
			var topCandidate = (object)null;
			bool isPressed = false;
			bool isPressedEdge = false;
			float coverageForTop = 0f;
			if (useGamepad)
			{
				// Manage rumble decay each frame while gamepad is connected
				float dtRumble = (float)gameTime.ElapsedGameTime.TotalSeconds;
				if (_rumbleTimeRemaining > 0f)
				{
					_rumbleTimeRemaining -= dtRumble;
					if (_rumbleTimeRemaining > 0f)
					{
						GamePad.SetVibration(PlayerIndex.One, MathHelper.Clamp(RumbleLow, 0f, 1f), MathHelper.Clamp(RumbleHigh, 0f, 1f));
					}
					else
					{
						_rumbleTimeRemaining = 0f;
						GamePad.SetVibration(PlayerIndex.One, 0f, 0f);
					}
				}

				if (!ignoringTransitions)
				{
					int rHitbox = Math.Max(0, HitboxRadius);
					var tc = EntityManager.GetEntitiesWithComponent<UIElement>()
						.Select(e2 => new { E = e2, UI = e2.GetComponent<UIElement>(), T = e2.GetComponent<Transform>() })
						.Where(x => x.UI != null && x.UI.Bounds.Width >= 2 && x.UI.Bounds.Height >= 2 && EstimateCircleRectCoverage(x.UI.Bounds, _cursorPosition, rHitbox) > 0f)
						.OrderByDescending(x => x.T?.ZOrder ?? 0)
						.FirstOrDefault();
					Entity hoveredEntityForRumble = null;
					if (tc != null)
					{
						tc.UI.IsHovered = true;
						hoveredEntityForRumble = tc.E;
						_lastHoveredEntity = hoveredEntityForRumble;
						// Trigger a short rumble when a new entity becomes hovered
						if (_prevHoverEntityForRumble != hoveredEntityForRumble && tc.UI.IsInteractable)
						{
							_rumbleTimeRemaining = Math.Max(0f, RumbleDurationSeconds);
							if (_rumbleTimeRemaining > 0f)
							{
								GamePad.SetVibration(PlayerIndex.One, MathHelper.Clamp(RumbleLow, 0f, 1f), MathHelper.Clamp(RumbleHigh, 0f, 1f));
							}
						}
						coverageForTop = EstimateCircleRectCoverage(tc.UI.Bounds, _cursorPosition, Math.Max(0, HitboxRadius));
					}
					// Cross animation: shrink slightly on entering a new interactable hover, ease back otherwise
					Entity currentInteractable = (tc != null && tc.UI != null && tc.UI.IsInteractable) ? tc.E : null;
					if (_prevHoverInteractable != currentInteractable)
					{
						_crossPulseTimer = EnterPulseDuration;
						_prevHoverInteractable = currentInteractable;
					}
					float targetScale = 1f;
					if (currentInteractable != null)
					{
						targetScale = (_crossPulseTimer > 0f) ? (HoverScale - EnterPulseExtra) : HoverScale;
					}
					_crossScaleCurrent += (targetScale - _crossScaleCurrent) * MathHelper.Clamp(dtRumble * CrossAnimSpeed, 0f, 1f);
					_crossPulseTimer = Math.Max(0f, _crossPulseTimer - dtRumble);
					topCandidate = tc;
					_prevHoverEntityForRumble = hoveredEntityForRumble;
				}

				// A button edge-triggered click: use the same coverage criterion as hover
				bool aPressed = gp.Buttons.A == ButtonState.Pressed;
				bool aPrevPressed = _prevGamePadState.Buttons.A == ButtonState.Pressed;
				bool aEdge = aPressed && !aPrevPressed;
				isPressed = aPressed;
				isPressedEdge = aEdge;
				if (aEdge && !ignoringTransitions)
				{
					int rHitboxClick = Math.Max(0, HitboxRadius);
					var clickCandidate = EntityManager.GetEntitiesWithComponent<UIElement>()
						.Select(e2 => new { E = e2, UI = e2.GetComponent<UIElement>(), T = e2.GetComponent<Transform>() })
						.Where(x => x.UI != null && x.UI.Bounds.Width >= 2 && x.UI.Bounds.Height >= 2 && EstimateCircleRectCoverage(x.UI.Bounds, _cursorPosition, rHitboxClick) > 0f)
						.OrderByDescending(x => x.T?.ZOrder ?? 0)
						.FirstOrDefault();
					if (clickCandidate != null)
					{
						if (clickCandidate.UI.EventType != UIElementEventType.None)
						{
							UIElementEventDelegateService.HandleEvent(clickCandidate.UI.EventType, clickCandidate.E);
						}
						else
						{
							clickCandidate.UI.IsClicked = true;
						}
						Console.WriteLine($"[CursorSystem] Clicked: {clickCandidate.E.Id}");
						_lastClickedEntity = clickCandidate.E;
					}
				}

				// Publish cursor state event for other systems
				EventManager.Publish(new CursorStateEvent
				{
					Position = _cursorPosition,
					IsAPressed = isPressed,
					IsAPressedEdge = isPressedEdge,
					Coverage = coverageForTop,
					TopEntity = ignoringTransitions ? null : ((topCandidate == null) ? null : ((dynamic)topCandidate).E)
				});

				_prevGamePadState = gp;

				// Apply circular deadzone for movement
				float mag = stick.Length();
				if (mag < Deadzone)
				{
					return;
				}

				// Normalize and scale by exponent curve
				Vector2 dir = (mag > 0f) ? (stick / mag) : Vector2.Zero;
				float normalized = MathHelper.Clamp((mag - Deadzone) / (1f - Deadzone), 0f, 1f);
				float speedMultiplier = MathHelper.Clamp((float)Math.Pow(normalized, SpeedExponent) * MaxMultiplier, 0f, 10f);

				// Slow down when overlapping UI elements beyond threshold
				int r = Math.Max(1, CursorRadius);
				float maxCoverage = 0f;
				if (!ignoringTransitions)
				{
					foreach (var e2 in EntityManager.GetEntitiesWithComponent<UIElement>())
					{
						var ui2 = e2.GetComponent<UIElement>();
						if (ui2 == null || !ui2.IsInteractable) continue;
						var bounds2 = ui2.Bounds;
						if (bounds2.Width < 2 || bounds2.Height < 2) continue;
						maxCoverage = Math.Max(maxCoverage, EstimateCircleRectCoverage(bounds2, _cursorPosition, r));
					}
				}
				float rt = gp.Triggers.Right;
				if (rt <= 0.1f && maxCoverage >= MathHelper.Clamp(SlowdownCoverageThreshold, 0f, 1f))
				{
					speedMultiplier *= MathHelper.Clamp(SlowdownMultiplier, 0.05f, 1f);
				}
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
			else
			{
				// Mouse-driven path: set position directly and handle hover/click; no drawing occurs in Draw()
				var ms = Mouse.GetState();
				_cursorPosition = new Vector2(ms.X, ms.Y);
				_cursorPosition.X = MathHelper.Clamp(_cursorPosition.X, 0f, w);
				_cursorPosition.Y = MathHelper.Clamp(_cursorPosition.Y, 0f, h);
				// Ensure rumble is disabled when switching to mouse
				if (_rumbleTimeRemaining > 0f)
				{
					_rumbleTimeRemaining = 0f;
					GamePad.SetVibration(PlayerIndex.One, 0f, 0f);
				}

				if (!ignoringTransitions)
				{
					var tc = EntityManager.GetEntitiesWithComponent<UIElement>()
						.Select(e2 => new { E = e2, UI = e2.GetComponent<UIElement>(), T = e2.GetComponent<Transform>() })
						.Where(x => x.UI != null && x.UI.Bounds.Width >= 2 && x.UI.Bounds.Height >= 2 && x.UI.Bounds.Contains(point))
						.OrderByDescending(x => x.T?.ZOrder ?? 0)
						.FirstOrDefault();
					if (tc != null)
					{
						tc.UI.IsHovered = true;
						_lastHoveredEntity = tc.E;
						coverageForTop = 1f; // Point inside bounds; treat as full coverage for UI logic
					}
					topCandidate = tc;
				}

				bool lPressed = ms.LeftButton == ButtonState.Pressed;
				bool lPrevPressed = _prevMouseState.LeftButton == ButtonState.Pressed;
				bool lEdge = lPressed && !lPrevPressed;
				isPressed = lPressed;
				isPressedEdge = lEdge;
				if (lEdge && !ignoringTransitions && topCandidate != null)
				{
					var tc = (dynamic)topCandidate;
					if (tc.UI.EventType != UIElementEventType.None)
					{
						UIElementEventDelegateService.HandleEvent(tc.UI.EventType, tc.E);
					}
					else
					{
						tc.UI.IsClicked = true;
					}
					Console.WriteLine($"[CursorSystem] Clicked: {tc.E.Id}");
					_lastClickedEntity = tc.E;
				}

				EventManager.Publish(new CursorStateEvent
				{
					Position = _cursorPosition,
					IsAPressed = isPressed,
					IsAPressedEdge = isPressedEdge,
					Coverage = coverageForTop,
					TopEntity = ignoringTransitions ? null : ((topCandidate == null) ? null : ((dynamic)topCandidate).E)
				});

				_prevMouseState = ms;
			}

			// Ease cross scale back to 1 when not using gamepad or during transitions
			if (!useGamepad || ignoringTransitions)
			{
				float dtGeneral = (float)gameTime.ElapsedGameTime.TotalSeconds;
				_crossScaleCurrent += (1f - _crossScaleCurrent) * MathHelper.Clamp(dtGeneral * CrossAnimSpeed, 0f, 1f);
				_crossPulseTimer = Math.Max(0f, _crossPulseTimer - dtGeneral);
				_prevHoverInteractable = null;
			}

		}

		public void Draw()
		{
			if (!_isEnabled) return;
			// Only draw the cursor if a controller is connected
			var gp = GamePad.GetState(PlayerIndex.One);
			if (!gp.IsConnected) return;
			int r = Math.Max(1, CursorRadius);
			_circleTexture = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, r);
			var dst = new Rectangle((int)Math.Round(_cursorPosition.X) - r, (int)Math.Round(_cursorPosition.Y) - r, r * 2, r * 2);

			float a = MathHelper.Clamp(CursorOpacity, 0f, 1f);
			var whiteWithAlpha = Color.FromNonPremultiplied(255, 255, 255, (byte)Math.Round(a * 255f));
			_spriteBatch.Draw(_circleTexture, dst, whiteWithAlpha);

			// Draw the inner hitbox circle
			// int rHitboxDraw = Math.Max(0, HitboxRadius);
			// if (rHitboxDraw > 0)
			// {
			// 	var hitboxTexture = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, rHitboxDraw);
			// 	var dstHitbox = new Rectangle((int)Math.Round(_cursorPosition.X) - rHitboxDraw, (int)Math.Round(_cursorPosition.Y) - rHitboxDraw, rHitboxDraw * 2, rHitboxDraw * 2);
			// 	var hitboxColor = Color.FromNonPremultiplied(Color.Gold.R, Color.Gold.G, Color.Gold.B, (byte)Math.Round(a * 255f));
			// 	_spriteBatch.Draw(hitboxTexture, dstHitbox, hitboxColor);
			// }

			// Draw the cross overlay, centered and scaled within the cursor
			if (_cursorCross != null)
			{
				var origin = new Vector2(_cursorCross.Width / 2f, _cursorCross.Height / 2f);
				float maxDim = Math.Max(_cursorCross.Width, _cursorCross.Height);
				float baseFit = (r * 2f) / maxDim * 0.75f; // keep within outer circle
				float scale = baseFit * MathHelper.Clamp(CrossScale, 0.25f, 3f) * _crossScaleCurrent;
				_spriteBatch.Draw(_cursorCross, _cursorPosition, null, whiteWithAlpha, 0f, origin, scale, SpriteEffects.None, 0f);
			}
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
						if (rect.Contains((int)Math.Round(sx), (int)Math.Round(sy))) insideCount++;
					}
				}
			}
			if (totalCount <= 0) return 0f;
			return insideCount / (float)totalCount;
		}
	}
}


