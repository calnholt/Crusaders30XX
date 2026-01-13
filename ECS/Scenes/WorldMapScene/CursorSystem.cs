using System.Linq;
using System.Collections.Generic;
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

		// Hit zone entry tracking for tie-breaking by recency
		private Dictionary<Entity, int> _hitZoneEntryOrder = new();
		private int _entryCounter = 0;

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

		[DebugEditable(DisplayName = "Hysteresis Threshold", Step = 0.01f, Min = 0f, Max = 0.5f)]
		public float HysteresisThreshold { get; set; } = 0.15f;

		// Hysteresis: track current top entity to prevent flickering during hover animations
		private Entity _currentTopEntity;

		// Candidate info used by helper methods
		private class CandidateInfo
		{
			public Entity E;
			public UIElement UI;
			public Transform T;
			public float Coverage;
			public int EntryOrder;
		}

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

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (!_isEnabled) return;

			// Clear last clicked tracking from previous frame (InputSystem owns IsClicked state)
			_lastClickedEntity = null;

			// Clear last hovered from previous frame
			if (_lastHoveredEntity != null)
			{
				var uiPrevH = _lastHoveredEntity.GetComponent<UIElement>();
				if (uiPrevH != null) uiPrevH.IsHovered = false;
				_lastHoveredEntity = null;
			}

			// Initialize position centered on first entry to scene or when viewport changes
			int w = Game1.VirtualWidth;
			int h = Game1.VirtualHeight;
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
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			bool ignoringTransitions = StateSingleton.IsActive;

			// Clear hover state on all UI elements to ensure exclusivity
			foreach (var e2 in EntityManager.GetEntitiesWithComponent<UIElement>())
			{
				var ui2 = e2.GetComponent<UIElement>();
				if (ui2 != null) ui2.IsHovered = false;
			}

			// ─── Input-specific: Update cursor position ───
			bool isPressed = false;
			bool isPressedEdge = false;
			InputMethod inputSource;

			if (useGamepad)
			{
				inputSource = InputMethod.Gamepad;
				UpdateRumble(dt);

				// Read button state
				bool aPressed = gp.Buttons.A == ButtonState.Pressed;
				bool aPrevPressed = _prevGamePadState.Buttons.A == ButtonState.Pressed;
				isPressed = aPressed;
				isPressedEdge = aPressed && !aPrevPressed;
			}
			else
			{
				inputSource = InputMethod.Mouse;

				// Mouse-driven: set position directly
				var ms = Mouse.GetState();

				// Transform mouse coordinates from Screen Space to Virtual Space
				var dest = Game1.RenderDestination;
				float scaleX = (float)dest.Width / Game1.VirtualWidth;
				float scaleY = (float)dest.Height / Game1.VirtualHeight;

				// Avoid division by zero
				if (scaleX <= 0.001f) scaleX = 1f;
				if (scaleY <= 0.001f) scaleY = 1f;

				float virtX = (ms.X - dest.X) / scaleX;
				float virtY = (ms.Y - dest.Y) / scaleY;

				_cursorPosition = new Vector2(virtX, virtY);
				_cursorPosition.X = MathHelper.Clamp(_cursorPosition.X, 0f, w);
				_cursorPosition.Y = MathHelper.Clamp(_cursorPosition.Y, 0f, h);

				// Ensure rumble is disabled when switching to mouse
				if (_rumbleTimeRemaining > 0f)
				{
					_rumbleTimeRemaining = 0f;
					GamePad.SetVibration(PlayerIndex.One, 0f, 0f);
				}

				// Read button state
				bool lPressed = ms.LeftButton == ButtonState.Pressed;
				bool lPrevPressed = _prevMouseState.LeftButton == ButtonState.Pressed;
				isPressed = lPressed;
				isPressedEdge = lPressed && !lPrevPressed;

				_prevMouseState = ms;
			}

			// ─── Shared: Determine top candidate and handle hover/click ───
			CandidateInfo topCandidate = null;
			float coverageForTop = 0f;

			if (!ignoringTransitions)
			{
				// Get all candidates with coverage, update entry tracking, and select top
				var candidates = GetCandidatesWithCoverage(forHover: true);
				UpdateHitZoneTracking(candidates);
				topCandidate = GetTopCandidate(candidates);

				if (topCandidate != null)
				{
					topCandidate.UI.IsHovered = true;
					_lastHoveredEntity = topCandidate.E;
					coverageForTop = topCandidate.Coverage;

					// Gamepad-specific: trigger rumble on new hover
					if (useGamepad && _prevHoverEntityForRumble != topCandidate.E && topCandidate.UI.IsInteractable && !topCandidate.UI.IsHidden)
					{
						_rumbleTimeRemaining = Math.Max(0f, RumbleDurationSeconds);
						if (_rumbleTimeRemaining > 0f)
						{
							GamePad.SetVibration(PlayerIndex.One, MathHelper.Clamp(RumbleLow, 0f, 1f), MathHelper.Clamp(RumbleHigh, 0f, 1f));
						}
					}
				}

				_prevHoverEntityForRumble = topCandidate?.E;

				// Update cross animation
				UpdateCrossAnimation(dt, topCandidate);

				// Handle click
				if (isPressedEdge)
				{
					HandleClick(topCandidate);
				}
			}
			else
			{
				// Ease cross scale back to 1 during transitions
				_crossScaleCurrent += (1f - _crossScaleCurrent) * MathHelper.Clamp(dt * CrossAnimSpeed, 0f, 1f);
				_crossPulseTimer = Math.Max(0f, _crossPulseTimer - dt);
				_prevHoverInteractable = null;
			}

			// ─── Shared: Publish cursor state ───
			PublishCursorState(_cursorPosition, isPressed, isPressedEdge, coverageForTop, topCandidate?.E, ignoringTransitions, inputSource);

			// ─── Gamepad-specific: Apply movement with slowdown ───
			if (useGamepad)
			{
				_prevGamePadState = gp;

				Vector2 stick = gp.ThumbSticks.Left; // X: right+, Y: up+
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
				float maxCoverage = GetMaxInteractableCoverage(ignoringTransitions);
				float rt = gp.Triggers.Right;
				if (rt <= 0.1f && maxCoverage >= MathHelper.Clamp(SlowdownCoverageThreshold, 0f, 1f))
				{
					speedMultiplier *= MathHelper.Clamp(SlowdownMultiplier, 0.05f, 1f);
				}
				if (rt > 0.1f)
				{
					speedMultiplier *= LtSpeedMultiplier;
				}

				// In screen space, up on stick is negative Y
				Vector2 velocity = new Vector2(dir.X, -dir.Y) * BaseSpeed * speedMultiplier;
				_cursorPosition += velocity * dt;

				// Clamp cursor center to remain within the screen
				_cursorPosition.X = MathHelper.Clamp(_cursorPosition.X, 0f, w);
				_cursorPosition.Y = MathHelper.Clamp(_cursorPosition.Y, 0f, h);
			}
		}

		/// <summary>
		/// Gets all UI entities that overlap the cursor hitbox, with their coverage calculated.
		/// </summary>
		private List<CandidateInfo> GetCandidatesWithCoverage(bool forHover)
		{
			int rHitbox = Math.Max(0, HitboxRadius);
			var result = new List<CandidateInfo>();

			foreach (var e in EntityManager.GetEntitiesWithComponent<UIElement>())
			{
				var ui = e.GetComponent<UIElement>();
				var t = e.GetComponent<Transform>();

				if (ui == null || ui.IsHidden) continue;
				if (ui.Bounds.Width < 2 || ui.Bounds.Height < 2) continue;

				// For hover, include entities with tooltip or card tooltip type even if not interactable
				if (forHover && !(ui.IsInteractable || !string.IsNullOrWhiteSpace(ui.Tooltip) || ui.TooltipType == TooltipType.Card))
					continue;

				float coverage = EstimateCircleRectCoverage(ui.Bounds, _cursorPosition, rHitbox, t?.Rotation ?? 0f);
				if (coverage > 0f)
				{
					result.Add(new CandidateInfo
					{
						E = e,
						UI = ui,
						T = t,
						Coverage = coverage,
						EntryOrder = _hitZoneEntryOrder.GetValueOrDefault(e, 0)
					});
				}
			}

			return result;
		}

		/// <summary>
		/// Updates the hit zone entry tracking dictionary.
		/// Adds new entries for entities that just entered, removes entities that left.
		/// </summary>
		private void UpdateHitZoneTracking(List<CandidateInfo> currentCandidates)
		{
			var currentEntities = new HashSet<Entity>(currentCandidates.Select(c => c.E));

			// Add new entries
			foreach (var candidate in currentCandidates)
			{
				if (!_hitZoneEntryOrder.ContainsKey(candidate.E))
				{
					_entryCounter++;
					_hitZoneEntryOrder[candidate.E] = _entryCounter;
					candidate.EntryOrder = _entryCounter;
				}
			}

			// Remove entities that are no longer in the hit zone
			var toRemove = _hitZoneEntryOrder.Keys.Where(e => !currentEntities.Contains(e)).ToList();
			foreach (var e in toRemove)
			{
				_hitZoneEntryOrder.Remove(e);
			}
		}

		/// <summary>
		/// Selects the top candidate from the list with hysteresis.
		/// The current top entity stays selected unless another entity exceeds its coverage by the threshold.
		/// Priority: 1) highest coverage, 2) most recent entry (highest entry order).
		/// </summary>
		private CandidateInfo GetTopCandidate(List<CandidateInfo> candidates)
		{
			if (candidates.Count == 0)
			{
				_currentTopEntity = null;
				return null;
			}

			// Find the current top's info if it's still in the candidates
			var currentTopInfo = candidates.FirstOrDefault(c => c.E == _currentTopEntity);

			// If current top is still overlapping, require new candidates to exceed by threshold
			if (currentTopInfo != null && currentTopInfo.Coverage > 0f)
			{
				float requiredCoverage = currentTopInfo.Coverage + HysteresisThreshold;
				var challenger = candidates
					.Where(c => c.E != _currentTopEntity && c.Coverage >= requiredCoverage)
					.OrderByDescending(c => c.Coverage)
					.ThenByDescending(c => c.EntryOrder)
					.FirstOrDefault();

				if (challenger != null)
				{
					_currentTopEntity = challenger.E;
					return challenger;
				}

				// Current top retains selection
				return currentTopInfo;
			}

			// Normal selection (no current top or it left the hit zone)
			var winner = candidates
				.OrderByDescending(c => c.Coverage)
				.ThenByDescending(c => c.EntryOrder)
				.FirstOrDefault();

			_currentTopEntity = winner?.E;
			return winner;
		}

		/// <summary>
		/// Updates the cross cursor animation based on hover state.
		/// </summary>
		private void UpdateCrossAnimation(float dt, CandidateInfo topCandidate)
		{
			Entity currentInteractable = (topCandidate != null && topCandidate.UI.IsInteractable && !topCandidate.UI.IsHidden) ? topCandidate.E : null;

			if (_prevHoverInteractable != currentInteractable)
			{
				_crossPulseTimer = EnterPulseDuration;
				_prevHoverInteractable = currentInteractable;
				if (currentInteractable != null)
				{
					EventManager.Publish(new PlaySfxEvent { Track = SfxTrack.Interface, Volume = 0.05f });
				}
			}

			float targetScale = 1f;
			if (currentInteractable != null)
			{
				targetScale = (_crossPulseTimer > 0f) ? (HoverScale - EnterPulseExtra) : HoverScale;
			}

			_crossScaleCurrent += (targetScale - _crossScaleCurrent) * MathHelper.Clamp(dt * CrossAnimSpeed, 0f, 1f);
			_crossPulseTimer = Math.Max(0f, _crossPulseTimer - dt);
		}

		/// <summary>
		/// Handles click logic when the action button is edge-triggered.
		/// </summary>
		private void HandleClick(CandidateInfo topCandidate)
		{
			if (topCandidate == null) return;
			if (topCandidate.UI.IsPreventDefaultClick) return;
			if (StateSingleton.PreventClicking) return;
			if (StateSingleton.IsTutorialActive) return;

			// For click, we need an interactable element
			if (!topCandidate.UI.IsInteractable || topCandidate.UI.IsHidden) return;

			Console.WriteLine($"[CursorSystem] Clicked: {topCandidate.E.Id}");
			_lastClickedEntity = topCandidate.E;
		}

		/// <summary>
		/// Publishes the cursor state event for other systems.
		/// </summary>
		private void PublishCursorState(Vector2 position, bool isPressed, bool isPressedEdge, float coverage, Entity topEntity, bool ignoringTransitions, InputMethod source)
		{
			EventManager.Publish(new CursorStateEvent
			{
				Position = position,
				IsAPressed = isPressed,
				IsAPressedEdge = isPressedEdge,
				Coverage = coverage,
				TopEntity = ignoringTransitions ? null : topEntity,
				Source = source
			});
		}

		/// <summary>
		/// Updates gamepad rumble state.
		/// </summary>
		private void UpdateRumble(float dt)
		{
			if (_rumbleTimeRemaining > 0f)
			{
				_rumbleTimeRemaining -= dt;
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
		}

		/// <summary>
		/// Gets the maximum coverage of any interactable UI element for slowdown calculation.
		/// </summary>
		private float GetMaxInteractableCoverage(bool ignoringTransitions)
		{
			if (ignoringTransitions) return 0f;

			int r = Math.Max(1, CursorRadius);
			float maxCoverage = 0f;

			foreach (var e in EntityManager.GetEntitiesWithComponent<UIElement>())
			{
				var ui = e.GetComponent<UIElement>();
				var t = e.GetComponent<Transform>();
				if (ui == null || !ui.IsInteractable || ui.IsHidden) continue;
				if (ui.Bounds.Width < 2 || ui.Bounds.Height < 2) continue;
				maxCoverage = Math.Max(maxCoverage, EstimateCircleRectCoverage(ui.Bounds, _cursorPosition, r, t?.Rotation ?? 0f));
			}

			return maxCoverage;
		}

		public void Draw()
		{
			if (!_isEnabled) return;
			int r = Math.Max(1, CursorRadius);
			_circleTexture = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, r);
			var dst = new Rectangle((int)Math.Round(_cursorPosition.X) - r, (int)Math.Round(_cursorPosition.Y) - r, r * 2, r * 2);

			float a = MathHelper.Clamp(CursorOpacity, 0f, 1f);
			var whiteWithAlpha = Color.FromNonPremultiplied(255, 255, 255, (byte)Math.Round(a * 255f));
			_spriteBatch.Draw(_circleTexture, dst, whiteWithAlpha);

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

		private static float EstimateCircleRectCoverage(Rectangle rect, Vector2 center, int radius, float rotation = 0f)
		{
			// Sample-based approximation: fraction of circle area inside the rectangle
			int samplesPerAxis = 8;
			int insideCount = 0;
			int totalCount = 0;
			float left = center.X - radius;
			float top = center.Y - radius;
			float step = (radius * 2f) / samplesPerAxis;
			float r2 = radius * radius;

			// Precompute rotation data if significant
			bool hasRotation = Math.Abs(rotation) > 0.001f;
			float cos = 0f, sin = 0f;
			Vector2 rectCenter = Vector2.Zero;
			if (hasRotation)
			{
				cos = (float)Math.Cos(-rotation); // rotate point by negative angle to bring it to AABB space
				sin = (float)Math.Sin(-rotation);
				rectCenter = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
			}

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
						int checkX = (int)Math.Round(sx);
						int checkY = (int)Math.Round(sy);
						
						if (hasRotation)
						{
							// Rotate point around rect center
							float lx = sx - rectCenter.X;
							float ly = sy - rectCenter.Y;
							float rx = lx * cos - ly * sin;
							float ry = lx * sin + ly * cos;
							checkX = (int)Math.Round(rectCenter.X + rx);
							checkY = (int)Math.Round(rectCenter.Y + ry);
						}

						if (rect.Contains(checkX, checkY)) insideCount++;
					}
				}
			}
			if (totalCount <= 0) return 0f;
			return insideCount / (float)totalCount;
		}
	}
}
