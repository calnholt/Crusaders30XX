using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Battle Transition")] 
	public class BattleTransitionDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _pixel;

		// Animation state
		private enum Phase { Idle, WipeIn, Hold, WipeOut }
		private Phase _phase = Phase.Idle;
		private float _t = 0f; // time within current phase
		private bool _suppressNextStartBattleRequest = false; // one-shot debug preview flag

		[DebugEditable(DisplayName = "Wipe Duration (s)", Step = 0.05f, Min = 0.05f, Max = 3f)]
		public float WipeDurationSeconds { get; set; } = 0.65f;
		[DebugEditable(DisplayName = "Hold Black (s)", Step = 0.05f, Min = 0f, Max = 2f)]
		public float HoldSeconds { get; set; } = 0f;
		[DebugEditable(DisplayName = "Angle Degrees", Step = 1f, Min = -90f, Max = 90f)]
		public float AngleDegrees { get; set; } = 40f; // diagonal like Star Wars
		[DebugEditable(DisplayName = "Color Alpha", Step = 5, Min = 0, Max = 255)]
		public int Alpha { get; set; } = 255;

		public BattleTransitionDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
			EventManager.Subscribe<BattleWon>(_ => BeginWipeIn());
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
			_t += dt;
			switch (_phase)
			{
				case Phase.WipeIn:
					if (_t >= WipeDurationSeconds)
					{
						// Fully black now; start hold and request next battle
						_phase = Phase.Hold; _t = 0f;
						if (!_suppressNextStartBattleRequest)
						{
							EventManager.Publish(new StartBattleRequested { });
						}
						else
						{
							// reset the one-shot flag after a preview run
							_suppressNextStartBattleRequest = false;
						}
					}
					break;
				case Phase.Hold:
					if (_t >= HoldSeconds)
					{
						_phase = Phase.WipeOut; _t = 0f;
					}
					break;
				case Phase.WipeOut:
					if (_t >= WipeDurationSeconds)
					{
						_phase = Phase.Idle; _t = 0f;
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
		}

		private void BeginWipeIn()
		{
			_phase = Phase.WipeIn;
			_t = 0f;
		}

		[DebugAction("Preview Wipe (no restart)")]
		private void Debug_PreviewWipe()
		{
			_suppressNextStartBattleRequest = true;
			BeginWipeIn();
		}
	}
}


