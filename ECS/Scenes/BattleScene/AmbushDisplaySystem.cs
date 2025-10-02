using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Shows an "Ambush!" intro animation and a timed meter for ambush attacks.
	/// Auto-confirms the enemy attack when the timer expires by publishing DebugCommandEvent("ConfirmEnemyAttack").
	/// </summary>
	[DebugTab("Ambush Display")]
	public class AmbushDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private readonly Texture2D _pixel;

		private string _lastContextId = null;
		private float _introElapsed = 0f;
		private readonly System.Random _rand = new System.Random();

		// Debug-editable tuning
		[DebugEditable(DisplayName = "Intro Drop Duration (s)", Step = 0.05f, Min = 0.05f, Max = 3f)]
		public float IntroDropDurationSeconds { get; set; } = 0.3f;

		[DebugEditable(DisplayName = "Intro Hold Duration (s)", Step = 0.05f, Min = 0f, Max = 3f)]
		public float IntroHoldDurationSeconds { get; set; } = 0.3f;

		[DebugEditable(DisplayName = "Intro Shrink Duration (s)", Step = 0.05f, Min = 0.05f, Max = 3f)]
		public float IntroShrinkDurationSeconds { get; set; } = 0.1f;

		[DebugEditable(DisplayName = "Text Scale Base", Step = 0.05f, Min = 0.2f, Max = 2f)]
		public float TextScaleBase { get; set; } = 0.6f;

		[DebugEditable(DisplayName = "Text Scale Overshoot", Step = 0.05f, Min = 0f, Max = 1f)]
		public float TextScaleOvershoot { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Text Start Y %", Step = 1, Min = -50, Max = 50)]
		public int TextStartYPercent { get; set; } = -15;

		[DebugEditable(DisplayName = "Text End Y %", Step = 1, Min = 0, Max = 80)]
		public int TextEndYPercent { get; set; } = 30;

		[DebugEditable(DisplayName = "Intro Shake Amplitude (px)", Step = 1, Min = 0, Max = 30)]
		public int IntroShakeAmplitudePx { get; set; } = 1;


		[DebugEditable(DisplayName = "Meter Width", Step = 5, Min = 100, Max = 800)]
		public int MeterWidth { get; set; } = 500;

		[DebugEditable(DisplayName = "Meter Height", Step = 1, Min = 10, Max = 80)]
		public int MeterHeight { get; set; } = 24;

		[DebugEditable(DisplayName = "Meter Skew", Step = 1, Min = 0, Max = 80)]
		public int MeterSkew { get; set; } = 29;

		[DebugEditable(DisplayName = "Meter Y %", Step = 1, Min = 0, Max = 90)]
		public int MeterYPercent { get; set; } = 55;

		[DebugEditable(DisplayName = "Meter BG Alpha", Step = 5, Min = 0, Max = 255)]
		public int MeterBgAlpha { get; set; } = 220;

		[DebugEditable(DisplayName = "Meter Fill Alpha", Step = 5, Min = 0, Max = 255)]
		public int MeterFillAlpha { get; set; } = 240;

		[DebugEditable(DisplayName = "Default Timer (s)", Step = 1, Min = 1, Max = 60)]
		public int DefaultTimerSeconds { get; set; } = 15;

		public AmbushDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_font = font;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });

			EventManager.Subscribe<ChangeBattlePhaseEvent>(evt =>
			{
				// Reset intro when leaving block/attack phases
				if (evt.Current != SubPhase.Block && evt.Current != SubPhase.EnemyAttack)
				{
					var st = GetOrCreateAmbushState();
					st.IsActive = false;
					st.IntroActive = false;
					st.TimerRemainingSeconds = 0f;
					st.FiredAutoConfirm = false;
					_introElapsed = 0f;
					_lastContextId = null;
				}
			});
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<AttackIntent>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>()?.Sub ?? SubPhase.Action;
			var intent = entity.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned.Count == 0)
			{
				Deactivate();
				return;
			}
			var pa = intent.Planned[0];
			var st = GetOrCreateAmbushState();

			// Detect context changes. Only commit activation during Block so ambush triggers at phase entry.
			if (_lastContextId != pa.ContextId)
			{
				_introElapsed = 0f;
				st.FiredAutoConfirm = false;
				if (pa.IsAmbush && phase == SubPhase.Block)
				{
					_lastContextId = pa.ContextId;
					st.IsActive = true;
					st.IntroActive = true;
					st.ContextId = pa.ContextId;
					st.TimerDurationSeconds = System.Math.Max(1f, DefaultTimerSeconds - GetSlowStacks());
					st.TimerRemainingSeconds = st.TimerDurationSeconds;
				}
				else
				{
					// Wait for Block to activate; keep ambush inactive for now
					Deactivate();
				}
			}
			// Fallback: if we are in Block and haven't activated for this context yet, activate now
			else if (pa.IsAmbush && phase == SubPhase.Block && (!st.IsActive || st.ContextId != pa.ContextId))
			{
				_lastContextId = pa.ContextId;
				_introElapsed = 0f;
				st.FiredAutoConfirm = false;
				st.IsActive = true;
				st.IntroActive = true;
				st.ContextId = pa.ContextId;
				st.TimerDurationSeconds = System.Math.Max(1f, DefaultTimerSeconds - GetSlowStacks());
				st.TimerRemainingSeconds = st.TimerDurationSeconds;
			}

			// Maintain state while in Block
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (st.IsActive && phase == SubPhase.Block)
			{
				_introElapsed += dt;
				float dropDurChk = System.Math.Max(0.0001f, IntroDropDurationSeconds);
				float holdDurChk = System.Math.Max(0f, IntroHoldDurationSeconds);
				float shrinkDurChk = System.Math.Max(0.0001f, IntroShrinkDurationSeconds);
				float totalChk = dropDurChk + holdDurChk + shrinkDurChk;
				if (st.IntroActive && _introElapsed >= totalChk)
				{
					st.IntroActive = false;
				}
				if (!st.IntroActive)
				{
					st.TimerRemainingSeconds = System.Math.Max(0f, st.TimerRemainingSeconds - dt);
					if (st.TimerRemainingSeconds <= 0f && !st.FiredAutoConfirm)
					{
						st.FiredAutoConfirm = true;
						EventManager.Publish(new DebugCommandEvent { Command = "ConfirmEnemyAttack" });
					}
				}
			}
			else if (phase != SubPhase.Block)
			{
				// Hide during non-block phases
				st.IntroActive = false;
			}
		}

		public void Draw()
		{
			var st = EntityManager.GetEntitiesWithComponent<AmbushState>().FirstOrDefault()?.GetComponent<AmbushState>();
			if (st == null || !st.IsActive || _font == null) return;
			var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>()?.Sub ?? SubPhase.Action;
			if (phase != SubPhase.Block) return;

			int vx = _graphicsDevice.Viewport.Width;
			int vy = _graphicsDevice.Viewport.Height;

			if (st.IntroActive)
			{
				// Big red "Ambush!" with drop-in + shake + overshoot scale
				string text = "Ambush!";
				float dropDur = System.Math.Max(0.0001f, IntroDropDurationSeconds);
				float holdDur = System.Math.Max(0f, IntroHoldDurationSeconds);
				float shrinkDur = System.Math.Max(0.0001f, IntroShrinkDurationSeconds);
				float total = dropDur + holdDur + shrinkDur;
				float t = MathHelper.Clamp(_introElapsed / total, 0f, 1f);
				float tSecs = _introElapsed;
				float scale;
				float easeDrop = 1f - (float)System.Math.Pow(1f - MathHelper.Clamp(tSecs / dropDur, 0f, 1f), 3);
				float appearScale = TextScaleBase + TextScaleOvershoot * (1f - (float)System.Math.Cos(easeDrop * System.MathF.PI));
				if (tSecs <= dropDur)
				{
					scale = appearScale;
				}
				else if (tSecs <= dropDur + holdDur)
				{
					scale = appearScale;
				}
				else
				{
					float stp = MathHelper.Clamp((tSecs - dropDur - holdDur) / shrinkDur, 0f, 1f);
					float shrinkEase = 1f - (float)System.Math.Pow(1f - stp, 2);
					scale = MathHelper.Lerp(appearScale, 0f, shrinkEase);
				}
				// Drop from top
				float yStart = vy * (TextStartYPercent / 100f);
				float yEnd = vy * (TextEndYPercent / 100f);
				float yPos;
				if (tSecs <= dropDur)
				{
					yPos = MathHelper.Lerp(yStart, yEnd, easeDrop);
				}
				else
				{
					yPos = yEnd;
				}
				int shakeAmp = IntroShakeAmplitudePx;
				int sx = _rand.Next(-shakeAmp, shakeAmp + 1);
				int sy = _rand.Next(-shakeAmp, shakeAmp + 1);
				var size = _font.MeasureString(text) * scale;
				var pos = new Vector2(vx / 2f - size.X / 2f + sx, yPos - size.Y / 2f + sy);
				// Shadow
				_spriteBatch.DrawString(_font, text, pos + new Vector2(2, 2), new Color(0, 0, 0, 200), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
				// Text
				_spriteBatch.DrawString(_font, text, pos, Color.OrangeRed, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			}

			// Timer meter (trapezoid/parallelogram-like), shown at top center
			int meterWidth = System.Math.Max(10, MeterWidth);
			int meterHeight = System.Math.Max(4, MeterHeight);
			int skew = System.Math.Max(0, MeterSkew); // pixels of top-edge inward skew
			var rect = new Rectangle(vx / 2 - meterWidth / 2, (int)(vy * (MeterYPercent / 100f)), meterWidth, meterHeight);
			float ratio = st.TimerDurationSeconds <= 0.0001f ? 0f : MathHelper.Clamp(st.TimerRemainingSeconds / st.TimerDurationSeconds, 0f, 1f);

			DrawTrapezoid(rect, skew, new Color(10, 10, 10, System.Math.Clamp(MeterBgAlpha, 0, 255))); // background
			var fillRect = rect;
			DrawTrapezoidFill(fillRect, skew, ratio, new Color(180, 30, 30, System.Math.Clamp(MeterFillAlpha, 0, 255)));
			DrawTrapezoidBorder(rect, skew, Color.Black, 2);
		}

		[DebugAction("Reset Ambush Intro")]
		public void Debug_ResetAmbushIntro()
		{
			var st = EntityManager.GetEntitiesWithComponent<AmbushState>().FirstOrDefault()?.GetComponent<AmbushState>();
			if (st != null && st.IsActive)
			{
				st.IntroActive = true;
				_introElapsed = 0f;
			}
		}

		[DebugAction("Auto Confirm Now")]
		public void Debug_AutoConfirmNow()
		{
			EventManager.Publish(new DebugCommandEvent { Command = "ConfirmEnemyAttack" });
		}

		private void Deactivate()
		{
			var st = GetOrCreateAmbushState();
			st.IsActive = false;
			st.IntroActive = false;
			st.TimerRemainingSeconds = 0f;
			st.FiredAutoConfirm = false;
			_introElapsed = 0f;
		}

		private AmbushState GetOrCreateAmbushState()
		{
			var e = EntityManager.GetEntitiesWithComponent<AmbushState>().FirstOrDefault();
			if (e == null)
			{
				e = EntityManager.CreateEntity("AmbushStateEntity");
				EntityManager.AddComponent(e, new AmbushState { IsActive = false, IntroActive = false, TimerDurationSeconds = 20f, TimerRemainingSeconds = 0f });
			}
			return e.GetComponent<AmbushState>();
		}

		private void DrawTrapezoid(Rectangle bounds, int skew, Color color)
		{
			for (int y = 0; y < bounds.Height; y++)
			{
				float t = 1f - (y / (float)System.Math.Max(1, bounds.Height - 1));
				int offset = (int)System.Math.Round(skew * t);
				int x = bounds.X + offset;
				int w = System.Math.Max(0, bounds.Width - offset * 2);
				_spriteBatch.Draw(_pixel, new Rectangle(x, bounds.Y + y, w, 1), color);
			}
		}

		private void DrawTrapezoidFill(Rectangle bounds, int skew, float ratio, Color color)
		{
			for (int y = 0; y < bounds.Height; y++)
			{
				float t = 1f - (y / (float)System.Math.Max(1, bounds.Height - 1));
				int offset = (int)System.Math.Round(skew * t);
				int x = bounds.X + offset;
				int w = System.Math.Max(0, bounds.Width - offset * 2);
				int fw = (int)System.Math.Round(w * ratio);
				if (fw > 0)
				{
					// Right-justify fill so it depletes right-to-left
					int xFill = x + (w - fw);
					_spriteBatch.Draw(_pixel, new Rectangle(xFill, bounds.Y + y, fw, 1), color);
				}
			}
		}

		private void DrawTrapezoidBorder(Rectangle bounds, int skew, Color color, int thickness)
		{
			thickness = System.Math.Max(1, thickness);
			for (int y = 0; y < bounds.Height; y++)
			{
				float t = 1f - (y / (float)System.Math.Max(1, bounds.Height - 1));
				int offset = (int)System.Math.Round(skew * t);
				int x = bounds.X + offset;
				int w = System.Math.Max(0, bounds.Width - offset * 2);
				int yy = bounds.Y + y;
				if (y < thickness || y >= bounds.Height - thickness)
				{
					// Top/bottom horizontal edges
					_spriteBatch.Draw(_pixel, new Rectangle(x, yy, w, 1), color);
				}
				else
				{
					// Left vertical edge
					_spriteBatch.Draw(_pixel, new Rectangle(x, yy, thickness, 1), color);
					// Right vertical edge
					_spriteBatch.Draw(_pixel, new Rectangle(x + System.Math.Max(0, w - thickness), yy, thickness, 1), color);
				}
			}
		}

		private int GetSlowStacks()
		{
			var ap = EntityManager.GetEntitiesWithComponent<AppliedPassives>().FirstOrDefault()?.GetComponent<AppliedPassives>();
			if (ap == null) return 0;
			return ap.Passives.TryGetValue(AppliedPassiveType.Slow, out int stacks) ? stacks : 0;
		}
	}
}


