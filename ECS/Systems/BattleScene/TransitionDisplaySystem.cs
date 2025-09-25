using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Transition")] 
	public class TransitionDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _pixel;
		private readonly SpriteFont _font;

		// Animation state
		private enum Phase { Idle, WipeIn, Hold, WipeOut }
		private Phase _phase = Phase.Idle;
		private float _t = 0f; // time within current phase
		private bool _suppressLoadScene = false; // one-shot debug preview flag
        private SceneId _nextScene;

        [DebugEditable(DisplayName = "Wipe Duration (s)", Step = 0.05f, Min = 0.05f, Max = 3f)]
		public float WipeDurationSeconds { get; set; } = 0.55f;
		[DebugEditable(DisplayName = "Hold Black (s)", Step = 0.05f, Min = 0f, Max = 2f)]
		public float HoldSeconds { get; set; } = 0.5f;
		[DebugEditable(DisplayName = "Angle Degrees", Step = 1f, Min = -90f, Max = 90f)]
		public float AngleDegrees { get; set; } = 40f; // diagonal like Star Wars
		[DebugEditable(DisplayName = "Color Alpha", Step = 5, Min = 0, Max = 255)]
		public int Alpha { get; set; } = 255;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.1f, Min = 0.5f, Max = 6f)]
		public float TextScale { get; set; } = 1f;

		[DebugEditable(DisplayName = "Pulse Amplitude", Step = 0.05f, Min = 0f, Max = 1f)]
		public float PulseAmplitude { get; set; } = 0.05f;

		public TransitionDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_font = font;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
			EventManager.Subscribe<ShowTransition>(BeginWipeIn);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return System.Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public override void Update(GameTime gameTime)
		{
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (_phase == Phase.Idle) return;
			// While transition is active, publish a global flag entity for other systems to consult
			EnsureTransitionFlag(true);
			_t += dt;
			switch (_phase)
			{
				case Phase.WipeIn:
					if (_t >= WipeDurationSeconds)
					{
						// Fully black now; start hold and request next battle
						_phase = Phase.Hold; 
						_t = 0f;
					}
					break;
				case Phase.Hold:
					if (_t >= HoldSeconds)
					{
						if (!_suppressLoadScene)
						{
							Console.WriteLine($"[TransitionDisplaySystem] Loading scene: {_nextScene}");
							EventManager.Publish(new LoadSceneEvent { Scene = _nextScene });
						}
						else
						{
							// reset the one-shot flag after a preview run
							_suppressLoadScene = false;
						}
						_phase = Phase.WipeOut; _t = 0f;
					}
					break;
				case Phase.WipeOut:
					if (_t >= WipeDurationSeconds)
					{
						_phase = Phase.Idle; _t = 0f;
						EnsureTransitionFlag(false);
					}
					break;
			}
		}

		public void Draw()
		{
			if (_phase == Phase.Idle) return;
			int vw = _graphicsDevice.Viewport.Width;
			int vh = _graphicsDevice.Viewport.Height;
			float angle = MathHelper.ToRadians(AngleDegrees);
			// progress 0..1 for wipe-in, 1..0 for wipe-out
			float p = MathHelper.Clamp(_t / System.Math.Max(0.0001f, WipeDurationSeconds), 0f, 1f);
			if (_phase == Phase.WipeOut) p = 1f - p;
			if (_phase == Phase.Hold) p = 1f;

			// Build a covering parallelogram whose width expands with p
			// Ensure full coverage for any angle by computing total length considering diagonal span
			int stripes = 128;
			int stripeThickness = (int)System.Math.Ceiling(vh / (float)stripes) + 2;
			byte a = (byte)System.Math.Clamp(Alpha, 0, 255);
			var color = Color.DarkRed;
			float tan = (float)System.Math.Tan(angle);
			float dxSpan = tan * vh; // total horizontal offset from top to bottom
			float dxMin = System.Math.Min(0f, dxSpan);
			float dxMax = System.Math.Max(0f, dxSpan);
			float margin = 1000f; // extra overdraw on both sides
			float startBase = dxMin - margin;
			float totalLen = vw + (dxMax - dxMin) + 2f * margin;
			float fillLen = totalLen * p;
			// For wipe-out, retract from the top-left by advancing the start position
			float startShift = (_phase == Phase.WipeOut) ? (totalLen - fillLen) : 0f;
			for (int i = -2; i < stripes + 2; i++)
			{
				float y0 = i * stripeThickness;
				float dx = tan * y0;
				var rect = new Rectangle((int)(startBase + startShift + dx), (int)y0, (int)System.Math.Ceiling(fillLen), stripeThickness);
				_spriteBatch.Draw(_pixel, rect, color);
			}

			// Reveal centered white text when the wipe overlaps the screen center (or during Hold)
			bool coveredAtCenter = false;
			{
				float centerY = vh * 0.5f;
				float centerX = vw * 0.5f;
				float dxCenter = tan * centerY;
				float startX = startBase + startShift + dxCenter;
				float endX = startX + fillLen;
				coveredAtCenter = centerX >= startX && centerX <= endX;
			}
			if (_phase == Phase.Hold) coveredAtCenter = true;

			if (coveredAtCenter && _font != null)
			{
				string text = "Deus Vult!";
				float scale = TextScale;
				if (_phase == Phase.Hold && HoldSeconds > 0f && PulseAmplitude > 0f)
				{
					float n = MathHelper.Clamp(_t / System.Math.Max(0.0001f, HoldSeconds), 0f, 1f);
					// Single quick pulse over the hold window
					scale *= 1f + PulseAmplitude * (float)System.Math.Sin(System.Math.PI * n);
				}
				var size = _font.MeasureString(text) * scale;
				var pos = new Vector2((vw - size.X) * 0.5f, (vh - size.Y) * 0.5f);
				_spriteBatch.DrawString(_font, text, pos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			}
		}

		private void BeginWipeIn(ShowTransition transition)
		{
			_suppressLoadScene = transition.Scene == SceneId.None;
			_nextScene = transition.Scene;
			_phase = Phase.WipeIn;
			_t = 0f;
			EnsureTransitionFlag(true);
		}

		private void EnsureTransitionFlag(bool active)
		{
			var e = EntityManager.GetEntity("TransitionState");
			if (e == null)
			{
				e = EntityManager.CreateEntity("TransitionState");
				EntityManager.AddComponent(e, new UIElement { Bounds = new Rectangle(0,0,0,0), IsInteractable = false });
			}
			// Reuse UIElement.IsHovered as a simple flag container to avoid a new component type
			var ui = e.GetComponent<UIElement>();
			if (ui == null)
			{
				EntityManager.AddComponent(e, new UIElement { Bounds = new Rectangle(0,0,0,0), IsInteractable = false, IsHovered = active });
			}
			else
			{
				ui.IsHovered = active;
			}
		}

		[DebugAction("Preview Wipe (visual only)")]
		private void Debug_PreviewWipe()
		{
			_suppressLoadScene = true;
			BeginWipeIn(new ShowTransition { Scene = SceneId.None });
		}
		[DebugAction("Preview Wipe (actually proceeds)")]
		private void Debug_PreviewWipeRestart()
		{
			_suppressLoadScene = true;
			BeginWipeIn(new ShowTransition { Scene = SceneId.Battle });
		}
	}
}


