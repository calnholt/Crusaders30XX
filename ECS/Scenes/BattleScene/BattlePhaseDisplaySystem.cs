using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Draws the current battle phase in the top-right corner and animates a
	/// large phase transition banner when the phase changes.
	/// </summary>
	[DebugTab("Battle Phase Display")]
	public class BattlePhaseDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private Texture2D _pixel;
		private Texture2D _trapezoidTexture;

		// Small corner label
		[DebugEditable(DisplayName = "Label Offset X", Step = 2, Min = -2000, Max = 2000)]
		public int LabelOffsetX { get; set; } = -16;
		[DebugEditable(DisplayName = "Label Offset Y", Step = 2, Min = -2000, Max = 2000)]
		public int LabelOffsetY { get; set; } = 14;
		[DebugEditable(DisplayName = "Label Scale", Step = 0.05f, Min = 0.2f, Max = 3f)]
		public float LabelScale { get; set; } = 0.15f;

		// Transition banner
		[DebugEditable(DisplayName = "Trans In (s)", Step = 0.05f, Min = 0.05f, Max = 5f)]
		public float TransitionInSeconds { get; set; } = 0.5f;
		[DebugEditable(DisplayName = "Trans Hold (s)", Step = 0.05f, Min = 0.05f, Max = 5f)]
		public float TransitionHoldSeconds { get; set; } = 0.9f;
		[DebugEditable(DisplayName = "Trans Out (s)", Step = 0.05f, Min = 0.05f, Max = 5f)]
		public float TransitionOutSeconds { get; set; } = 0.5f;
		[DebugEditable(DisplayName = "Trans Y Offset", Step = 2, Min = -2000, Max = 2000)]
		public int TransitionOffsetY { get; set; } = 140;
		[DebugEditable(DisplayName = "Trans Scale", Step = 0.05f, Min = 0.2f, Max = 4f)]
		public float TransitionScale { get; set; } = 0.263f;
		[DebugEditable(DisplayName = "Shadow Offset", Step = 1, Min = 0, Max = 20)]
		public int ShadowOffset { get; set; } = 0;

		// Trapezoid background
		[DebugEditable(DisplayName = "Trapezoid Width Padding", Step = 2f, Min = -200f, Max = 200f)]
		public float TrapezoidWidthPadding { get; set; } = 52f;
		[DebugEditable(DisplayName = "Trapezoid Height Padding", Step = 2f, Min = -200f, Max = 200f)]
		public float TrapezoidHeightPadding { get; set; } = 6f;
		[DebugEditable(DisplayName = "Trapezoid Left Side Offset", Step = 1f, Min = -100f, Max = 100f)]
		public float TrapezoidLeftSideOffset { get; set; } = 0f;
		[DebugEditable(DisplayName = "Trapezoid Top Edge Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float TrapezoidTopEdgeAngle { get; set; } = 3f;
		[DebugEditable(DisplayName = "Trapezoid Right Edge Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float TrapezoidRightEdgeAngle { get; set; } = -14f;
		[DebugEditable(DisplayName = "Trapezoid Bottom Edge Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float TrapezoidBottomEdgeAngle { get; set; } = -2f;
		[DebugEditable(DisplayName = "Trapezoid Left Edge Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float TrapezoidLeftEdgeAngle { get; set; } = 21f;
		[DebugEditable(DisplayName = "Trapezoid Offset X", Step = 1f, Min = -200f, Max = 200f)]
		public float TrapezoidOffsetX { get; set; } = 0f;
		[DebugEditable(DisplayName = "Trapezoid Offset Y", Step = 1f, Min = -200f, Max = 200f)]
		public float TrapezoidOffsetY { get; set; } = 0f;
		[DebugEditable(DisplayName = "Trapezoid Alpha", Step = 0.05f, Min = 0f, Max = 1f)]
		public float TrapezoidAlpha { get; set; } = 1f;

		private SubPhase _lastPhase = SubPhase.StartBattle;
		private int _lastTurn = 0;
		private bool _transitionActive;
		private bool _transitionJustStarted;
		private float _transitionT; // seconds in current transition
		private string _transitionText = string.Empty;
		private bool _playedInitial;

		public BattlePhaseDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_font = font;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
			// If dialog ran before battle start, re-arm the initial phase banner
			EventManager.Subscribe<DialogEnded>(_ => {
				_playedInitial = false;
				_transitionActive = false;
				_transitionT = 0f;
			});
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<PhaseState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var phase = entity.GetComponent<PhaseState>();
			if (!_playedInitial)
			{
				_playedInitial = true;
				_lastPhase = phase.Sub;
				_transitionActive = true;
				_transitionJustStarted = true;
				_transitionT = 0f;
				_lastTurn = phase.TurnNumber;
				_transitionText = SubPhaseToString(_lastPhase);
			}
			else if (_lastTurn != phase.TurnNumber)
			{
				_lastPhase = phase.Sub;
				_lastTurn = phase.TurnNumber;
				_transitionActive = true;
				_transitionJustStarted = true;
				_transitionT = 0f;
				_transitionText = SubPhaseToString(_lastPhase);
			}
			else if (_lastPhase != phase.Sub)
			{
				_lastPhase = phase.Sub;
				_transitionActive = true;
				_transitionJustStarted = true;
				_transitionT = 0f;
				_transitionText = SubPhaseToString(_lastPhase);
			}
			if (_transitionActive)
			{
				_transitionT += (float)gameTime.ElapsedGameTime.TotalSeconds;
				float total = TransitionInSeconds + TransitionHoldSeconds + TransitionOutSeconds;
				if (_transitionT >= total)
				{
					_transitionActive = false;
				}
			}
			else
			{
				// Continue updating time during exit animation even after transition is marked inactive
				var transEntity = EntityManager.GetEntity("UI_PhaseTransitionBanner");
				if (transEntity != null)
				{
					var transT = transEntity.GetComponent<Transform>();
					if (transT != null)
					{
						float holdEnd = TransitionInSeconds + TransitionHoldSeconds;
						// Continue exit animation if we're past the hold phase
						if (_transitionT > holdEnd)
						{
							_transitionT += (float)gameTime.ElapsedGameTime.TotalSeconds;
							// Stop updating once fully off-screen
							float exitProgress = _transitionT - holdEnd;
							float totalExit = TransitionOutSeconds;
							if (exitProgress >= totalExit)
							{
								_transitionT = holdEnd + totalExit; // Clamp to max
							}
						}
					}
				}
			}
		}

		public void Draw()
		{
			if (_font == null) return;
			var stateEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
			if (stateEntity == null) return;
			var state = stateEntity.GetComponent<PhaseState>();
			int vw = _graphicsDevice.Viewport.Width;
			int xRight = vw + LabelOffsetX;
			string label = "";
			if (state.Main == MainPhase.StartBattle)
			{
				label = $"{MainPhaseToString(state.Main)}";
			}
			else 
			{
				label = $"{MainPhaseToString(state.Main)} - {SubPhaseToString(state.Sub)} ({state.TurnNumber})";
			}
			var size = _font.MeasureString(label) * LabelScale;
			var basePos = new Vector2(xRight - size.X, LabelOffsetY);
			// Ensure a label entity with Transform + Parallax; write BasePosition only
			var labelEntity = EntityManager.GetEntity("UI_PhaseLabelTopRight");
			if (labelEntity == null)
			{
				labelEntity = EntityManager.CreateEntity("UI_PhaseLabelTopRight");
				EntityManager.AddComponent(labelEntity, new Transform { BasePosition = basePos, Position = basePos });
				EntityManager.AddComponent(labelEntity, ParallaxLayer.GetUIParallaxLayer());
			}
			var labelT = labelEntity.GetComponent<Transform>();
			if (labelT != null)
			{
				labelT.BasePosition = basePos;
				var drawPos = labelT.Position;
				_spriteBatch.DrawString(_font, label, drawPos + new Vector2(ShadowOffset, ShadowOffset), Color.Black * 0.6f, 0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);
				_spriteBatch.DrawString(_font, label, drawPos, Color.White, 0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);
			}

			// Draw transition banner if active or still exiting
			var transEntity = EntityManager.GetEntity("UI_PhaseTransitionBanner");
			if (transEntity == null && _transitionActive)
			{
				// Create entity when transition starts
				transEntity = EntityManager.CreateEntity("UI_PhaseTransitionBanner");
				float centerX = vw * 0.5f;
				var tSize = _font.MeasureString(_transitionText) * TransitionScale;
				float spawnX = -tSize.X - 80f;
				var start = new Vector2(spawnX, TransitionOffsetY);
				EntityManager.AddComponent(transEntity, new Transform { BasePosition = start, Position = start });
				EntityManager.AddComponent(transEntity, ParallaxLayer.GetUIParallaxLayer());
			}
			
			if (transEntity != null && _transitionActive)
			{
				float t = _transitionT;
				float inEnd = TransitionInSeconds;
				float holdEnd = TransitionInSeconds + TransitionHoldSeconds;

				float centerX = vw * 0.5f;
				var tSize = _font.MeasureString(_transitionText) * TransitionScale;
				float targetX = centerX - tSize.X / 2f;
				float y = TransitionOffsetY;
				float x;
				if (t <= inEnd)
				{
					float p = MathHelper.Clamp(t / Math.Max(0.001f, TransitionInSeconds), 0f, 1f);
					p = EaseOutCubic(p);
					x = MathHelper.Lerp(-tSize.X - 80f, targetX, p);
				}
				else if (t <= holdEnd)
				{
					x = targetX;
				}
				else
				{
					float u = MathHelper.Clamp((t - holdEnd) / Math.Max(0.001f, TransitionOutSeconds), 0f, 1f);
					u = EaseInCubic(u);
					x = MathHelper.Lerp(targetX, vw + 80f, u);
				}

				var transT = transEntity.GetComponent<Transform>();
				if (transT != null)
				{
					// If this banner just appeared or transition just started, spawn its base offscreen to the left so it flies in
					if (transT.BasePosition == Vector2.Zero || _transitionJustStarted)
					{
						float spawnX = -tSize.X - 80f;
						var spawn = new Vector2(spawnX, y);
						transT.BasePosition = spawn;
						transT.Position = spawn;
					}
					else
					{
						// Update BasePosition with the calculated animation position
						transT.BasePosition = new Vector2(x, y);
					}
					var pDraw = transT.Position;
					// Draw if banner is still visible on screen (check if any part is visible)
					if (pDraw.X + tSize.X >= 0 && pDraw.X <= vw + tSize.X)
					{
						// Draw trapezoid background
						float trapezoidWidth = tSize.X + TrapezoidWidthPadding;
						float trapezoidHeight = tSize.Y + TrapezoidHeightPadding;
						_trapezoidTexture = PrimitiveTextureFactory.GetAntialiasedTrapezoid(
							_graphicsDevice,
							trapezoidWidth,
							trapezoidHeight,
							TrapezoidLeftSideOffset,
							TrapezoidTopEdgeAngle,
							TrapezoidRightEdgeAngle,
							TrapezoidBottomEdgeAngle,
							TrapezoidLeftEdgeAngle
						);
						if (_trapezoidTexture != null)
						{
							Vector2 trapezoidPos = pDraw + new Vector2(TrapezoidOffsetX - TrapezoidWidthPadding / 2f, TrapezoidOffsetY);
							Vector2 scale = new Vector2(trapezoidWidth / _trapezoidTexture.Width, trapezoidHeight / _trapezoidTexture.Height);
							_spriteBatch.Draw(_trapezoidTexture, trapezoidPos, null, Color.Black * TrapezoidAlpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
						}
						// simple shadow then text
						_spriteBatch.DrawString(_font, _transitionText, pDraw + new Vector2(ShadowOffset, ShadowOffset), Color.Black * 0.6f, 0f, Vector2.Zero, TransitionScale, SpriteEffects.None, 0f);
						_spriteBatch.DrawString(_font, _transitionText, pDraw, Color.White, 0f, Vector2.Zero, TransitionScale, SpriteEffects.None, 0f);
					}
				}
				// Reset the flag after handling the transition start
				_transitionJustStarted = false;
			}
			else if (transEntity != null && !_transitionActive)
			{
				// Continue exit animation even after transition is marked inactive
				var transT = transEntity.GetComponent<Transform>();
				if (transT != null)
				{
					float centerX = vw * 0.5f;
					var tSize = _font.MeasureString(_transitionText) * TransitionScale;
					float targetX = centerX - tSize.X / 2f;
					float y = TransitionOffsetY;
					
					// Continue exit animation from where we left off
					float holdEnd = TransitionInSeconds + TransitionHoldSeconds;
					float exitProgress = MathHelper.Clamp(_transitionT - holdEnd, 0f, TransitionOutSeconds);
					float u = MathHelper.Clamp(exitProgress / Math.Max(0.001f, TransitionOutSeconds), 0f, 1f);
					u = EaseInCubic(u);
					float x = MathHelper.Lerp(targetX, vw + 80f, u);
					
					transT.BasePosition = new Vector2(x, y);
					var pDraw = transT.Position;
					// Draw if banner is still visible on screen (check if any part is visible)
					if (pDraw.X + tSize.X >= 0 && pDraw.X <= vw + tSize.X)
					{
						// Draw trapezoid background
						float trapezoidWidth = tSize.X + TrapezoidWidthPadding;
						float trapezoidHeight = tSize.Y + TrapezoidHeightPadding;
						_trapezoidTexture = PrimitiveTextureFactory.GetAntialiasedTrapezoid(
							_graphicsDevice,
							trapezoidWidth,
							trapezoidHeight,
							TrapezoidLeftSideOffset,
							TrapezoidTopEdgeAngle,
							TrapezoidRightEdgeAngle,
							TrapezoidBottomEdgeAngle,
							TrapezoidLeftEdgeAngle
						);
						if (_trapezoidTexture != null)
						{
							Vector2 trapezoidPos = pDraw + new Vector2(TrapezoidOffsetX - TrapezoidWidthPadding / 2f, TrapezoidOffsetY);
							Vector2 scale = new Vector2(trapezoidWidth / _trapezoidTexture.Width, trapezoidHeight / _trapezoidTexture.Height);
							_spriteBatch.Draw(_trapezoidTexture, trapezoidPos, null, Color.Black * TrapezoidAlpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
						}
						// simple shadow then text
						_spriteBatch.DrawString(_font, _transitionText, pDraw + new Vector2(ShadowOffset, ShadowOffset), Color.Black * 0.6f, 0f, Vector2.Zero, TransitionScale, SpriteEffects.None, 0f);
						_spriteBatch.DrawString(_font, _transitionText, pDraw, Color.White, 0f, Vector2.Zero, TransitionScale, SpriteEffects.None, 0f);
					}
				}
			}
		}

		private static string MainPhaseToString(MainPhase p)
		{
			return p switch
			{
				MainPhase.EnemyTurn => "Enemy Turn",
				MainPhase.PlayerTurn => "Player Turn",
				MainPhase.StartBattle => "Start of Battle",
				_ => p.ToString()
			};
		}
		private static string SubPhaseToString(SubPhase sp)
		{
			return sp switch
			{
				SubPhase.StartBattle => "Start of Battle",
				SubPhase.Block => "Block Phase",
				SubPhase.Action => "Action Phase",
				_ => ""
			};
		}

		private static float EaseOutCubic(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			float u = 1f - t;
			return 1f - u * u * u;
		}

		private static float EaseInCubic(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			return t * t * t;
		}

		[DebugAction("Replay Transition Animation")]
		public void Debug_ReplayTransitionAnimation()
		{
			_transitionActive = true;
			_transitionJustStarted = true;
			_transitionT = 0f;
			// Get current phase to set the text
			var stateEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
			if (stateEntity != null)
			{
				var phase = stateEntity.GetComponent<PhaseState>();
				_transitionText = SubPhaseToString(phase.Sub);
			}
		}
	}
}


