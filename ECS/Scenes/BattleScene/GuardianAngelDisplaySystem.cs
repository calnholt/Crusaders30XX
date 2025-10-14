using System.Linq;
using System.Collections.Generic;
using System;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Guardian Angel")]
    public class GuardianAngelDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
        private Texture2D _angelTexture;
        private Texture2D _pixel;
        private SpriteFont _font;

        private float _t;
		private float _rot; // smoothed rotation follower (radians)
		private Vector2 _pos;
		private Vector2 _prevPos;
		private float _spawnAccumulator;
		private static readonly Random _rand = new Random();
		private struct DustParticle
		{
			public Vector2 Position;
			public Vector2 Velocity;
			public float Age;
			public float Lifetime;
			public float Size;
			public float FlickerPeriod;
			public float FlickerOffset;
		}
		private readonly List<DustParticle> _dust = new List<DustParticle>();

		// Speech bubble state
		private bool _bubbleActive;
		private float _bubbleElapsed;
		private string _bubbleText = "";
		private Texture2D _bubbleTexture;
		private int _bubbleTexW;
		private int _bubbleTexH;

        // Placement relative to player
        [DebugEditable(DisplayName = "Offset X", Step = 5, Min = -2000, Max = 2000)]
		public int OffsetX { get; set; } = 215;
        [DebugEditable(DisplayName = "Offset Y", Step = 5, Min = -2000, Max = 2000)]
        public int OffsetY { get; set; } = -135;

        // Motion settings
        [DebugEditable(DisplayName = "Base Radius X", Step = 5, Min = 0, Max = 2000)]
		public int RadiusX { get; set; } = 70;
        [DebugEditable(DisplayName = "Base Radius Y", Step = 5, Min = 0, Max = 2000)]
		public int RadiusY { get; set; } = 25;
        [DebugEditable(DisplayName = "Figure Eight Mix", Step = 0.01f, Min = 0f, Max = 1f)]
		public float FigureEightMix { get; set; } = 0.73f; // 0: ellipse, 1: classic 8
        [DebugEditable(DisplayName = "Angular Speed", Step = 0.05f, Min = 0.05f, Max = 10f)]
		public float AngularSpeed { get; set; } = 1.15f; // radians per second multiplier
        [DebugEditable(DisplayName = "Vertical Bob Amplitude", Step = 1, Min = 0, Max = 1000)]
        public int VerticalBob { get; set; } = 7;
        [DebugEditable(DisplayName = "Vertical Bob Speed", Step = 0.05f, Min = 0.05f, Max = 10f)]
        public float VerticalBobSpeed { get; set; } = 1.15f;

        // Appearance
        [DebugEditable(DisplayName = "Scale", Step = 0.01f, Min = 0.05f, Max = 4f)]
        public float Scale { get; set; } = 0.08f;
        [DebugEditable(DisplayName = "Alpha", Step = 0.05f, Min = 0f, Max = 1f)]
		public float Alpha { get; set; } = 1f;
		[DebugEditable(DisplayName = "Rotation Magnitude", Step = 0.01f, Min = 0f, Max = 1f)]
		public float RotationMagnitude { get; set; } = 0f;
		[DebugEditable(DisplayName = "Rotation Follow", Step = 0.01f, Min = 0.01f, Max = 1f)]
		public float RotationFollow { get; set; } = 0.33f;

		// Sparkle dust trail settings
		[DebugEditable(DisplayName = "Dust Spawn Rate (per sec)", Step = 1, Min = 0, Max = 200)]
		public int DustSpawnRate { get; set; } = 14;
		[DebugEditable(DisplayName = "Dust Speed Min", Step = 1, Min = 0, Max = 2000)]
		public int DustSpeedMin { get; set; } = 22;
		[DebugEditable(DisplayName = "Dust Speed Max", Step = 1, Min = 0, Max = 2000)]
		public int DustSpeedMax { get; set; } = 43;
		[DebugEditable(DisplayName = "Dust Align To Motion")]
		public bool DustAlignToMotion { get; set; } = true;
		[DebugEditable(DisplayName = "Dust Cone Half-Angle (deg)", Step = 1, Min = 0, Max = 180)]
		public int DustConeHalfAngleDeg { get; set; } = 35;
		[DebugEditable(DisplayName = "Dust Angle Min (deg)", Step = 1, Min = -180, Max = 180)]
		public int DustAngleMinDeg { get; set; } = 180; // around downward
		[DebugEditable(DisplayName = "Dust Angle Max (deg)", Step = 1, Min = -180, Max = 180)]
		public int DustAngleMaxDeg { get; set; } = 160;
		[DebugEditable(DisplayName = "Dust Lifetime Min (s)", Step = 0.01f, Min = 0.01f, Max = 5f)]
		public float DustLifetimeMin { get; set; } = 0.6f;
		[DebugEditable(DisplayName = "Dust Lifetime Max (s)", Step = 0.01f, Min = 0.01f, Max = 5f)]
		public float DustLifetimeMax { get; set; } = 1.38f;
		[DebugEditable(DisplayName = "Dust Size Min", Step = 0.01f, Min = 0.01f, Max = 3f)]
		public float DustSizeMin { get; set; } = 0.9f;
		[DebugEditable(DisplayName = "Dust Size Max", Step = 0.01f, Min = 0.01f, Max = 3f)]
		public float DustSizeMax { get; set; } = 3f;
		[DebugEditable(DisplayName = "Flicker Period Min (s)", Step = 0.01f, Min = 0.01f, Max = 2f)]
		public float FlickerPeriodMin { get; set; } = 0.09f;
		[DebugEditable(DisplayName = "Flicker Period Max (s)", Step = 0.01f, Min = 0.01f, Max = 2f)]
		public float FlickerPeriodMax { get; set; } = 0.62f;

		// Bubble settings
		[DebugEditable(DisplayName = "Bubble Text Scale", Step = 0.01f, Min = 0.01f, Max = 3f)]
		public float BubbleTextScale { get; set; } = 0.14f;
		[DebugEditable(DisplayName = "Bubble Padding X", Step = 1, Min = 0, Max = 200)]
		public int BubblePadX { get; set; } = 8;
		[DebugEditable(DisplayName = "Bubble Padding Y", Step = 1, Min = 0, Max = 200)]
		public int BubblePadY { get; set; } = 8;
		[DebugEditable(DisplayName = "Bubble Corner Radius", Step = 1, Min = 0, Max = 60)]
		public int BubbleCornerRadius { get; set; } = 12;
		[DebugEditable(DisplayName = "Bubble Max Width (px)", Step = 10, Min = 50, Max = 2000)]
		public int BubbleMaxWidth { get; set; } = 260;
		[DebugEditable(DisplayName = "Bubble Offset X", Step = 1, Min = -1000, Max = 1000)]
		public int BubbleOffsetX { get; set; } = 0;
		[DebugEditable(DisplayName = "Bubble Offset Y", Step = 1, Min = -1000, Max = 1000)]
		public int BubbleOffsetY { get; set; } = 0;
		[DebugEditable(DisplayName = "Bubble Duration (s)", Step = 0.05f, Min = 0.05f, Max = 10f)]
		public float BubbleDuration { get; set; } = 3f;
		[DebugEditable(DisplayName = "Bubble BG Alpha", Step = 0.05f, Min = 0f, Max = 1f)]
		public float BubbleBgAlpha { get; set; } = 0.82f;
		[DebugEditable(DisplayName = "Bubble Fade In (s)", Step = 0.01f, Min = 0f, Max = 2f)]
		public float BubbleFadeInSeconds { get; set; } = 0.15f;
		[DebugEditable(DisplayName = "Bubble Fade Out (s)", Step = 0.01f, Min = 0f, Max = 2f)]
		public float BubbleFadeOutSeconds { get; set; } = 0.25f;
		[DebugEditable(DisplayName = "Bubble Text", Step = 1)]
		public string DefaultStartBattleText { get; set; } = "You don't scare us!";

        public GuardianAngelDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
			_content = content;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
            // Load shared UI font
            _font = _content.Load<SpriteFont>("NewRocker");
            // Listen for phase changes to show speech bubbles
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
            EventManager.Subscribe<TriggerTemperance>(OnTriggerTemperance);
            EventManager.Subscribe<LoadSceneEvent>(OnLoadSceneEvent);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_t += dt;

			// Compute current guardian position for spawning dust in Update
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var pt = player?.GetComponent<Transform>();
			if (pt != null)
			{
				var oldPos = _pos;
				Vector2 baseRight = pt.Position + new Vector2(OffsetX, OffsetY);
				float ang = _t * AngularSpeed;
				float xEllipse = MathF.Cos(ang);
				float yEllipse = MathF.Sin(ang);
				float xEight = MathF.Sin(ang);
				float yEight = MathF.Sin(2f * ang);
				float x = xEllipse * (1f - FigureEightMix) + xEight * FigureEightMix;
				float y = yEllipse * (1f - FigureEightMix) + yEight * FigureEightMix;
				Vector2 motion = new Vector2(x * RadiusX, y * RadiusY);
				float bob = MathF.Sin(_t * VerticalBobSpeed) * VerticalBob;
				motion.Y += bob;
				_pos = baseRight + motion;
				_prevPos = oldPos;
			}

			// Spawn dust based on accumulator
			_spawnAccumulator += DustSpawnRate * dt;
			while (_spawnAccumulator >= 1f)
			{
				_spawnAccumulator -= 1f;
				SpawnDust(_pos);
			}

			// Update dust particles
			for (int i = _dust.Count - 1; i >= 0; i--)
			{
				var p = _dust[i];
				p.Age += dt;
				if (p.Age >= p.Lifetime)
				{
					_dust.RemoveAt(i);
					continue;
				}
				p.Position += p.Velocity * dt;
				// Slight gravity to emphasize fall
				p.Velocity.Y += 220f * dt;
				_dust[i] = p;
			}

			// Update bubble timer
			if (_bubbleActive)
			{
				_bubbleElapsed += dt;
				if (_bubbleElapsed >= BubbleDuration)
				{
					_bubbleActive = false;
				}
			}
            base.Update(gameTime);
        }

		private void OnChangeBattlePhase(ChangeBattlePhaseEvent e)
		{
			TimerScheduler.Schedule(0.5f, () => {
				if (e.Current == SubPhase.StartBattle)
				{
					ShowBubble(GuardianAngelMessageService.GetMessage(GuardianMessageType.StartOfBattle), BubbleDuration);
				}
				else if (e.Current == SubPhase.Action)
				{
					ShowBubble(GuardianAngelMessageService.GetMessage(GuardianMessageType.ActionPhase), BubbleDuration);
				}
			});
		}

		private bool SkipMessage()
		{
			return Random.Shared.Next(0, 100) > 0;
		}

		private void OnTriggerTemperance(TriggerTemperance e)
		{
			ShowBubble(GuardianAngelMessageService.GetMessage(GuardianMessageType.Temperance), BubbleDuration);
		}

		private void OnLoadSceneEvent(LoadSceneEvent @event)
		{
			_bubbleActive = false;
		}

		private void ShowBubble(string text, float duration)
		{
			_bubbleText = text ?? string.Empty;
			_bubbleElapsed = 0f;
			_bubbleActive = true;
			// Reset cached texture so it can rebuild to fit text
			_bubbleTexW = 0; _bubbleTexH = 0;
		}

		private void SpawnDust(Vector2 origin)
		{
			if (_pixel == null) return;
			float speed = Lerp(DustSpeedMin, DustSpeedMax, (float)_rand.NextDouble());
			Vector2 dir;
			if (DustAlignToMotion)
			{
				Vector2 vel = _pos - _prevPos;
				if (vel.LengthSquared() < 0.0001f) vel = new Vector2(-1f, 0f);
				float baseRad = MathF.Atan2(vel.Y, vel.X) + MathF.PI; // spray backward
				float halfRad = MathHelper.ToRadians(DustConeHalfAngleDeg);
				float offset = Lerp(-halfRad, halfRad, (float)_rand.NextDouble());
				float rad = baseRad + offset;
				dir = new Vector2(MathF.Cos(rad), MathF.Sin(rad));
			}
			else
			{
				float angleDeg = Lerp(DustAngleMinDeg, DustAngleMaxDeg, (float)_rand.NextDouble());
				float rad = MathHelper.ToRadians(angleDeg);
				dir = new Vector2(MathF.Cos(rad), MathF.Sin(rad));
			}
			float lifetime = Lerp(DustLifetimeMin, DustLifetimeMax, (float)_rand.NextDouble());
			float size = Lerp(DustSizeMin, DustSizeMax, (float)_rand.NextDouble());
			float flicker = Lerp(FlickerPeriodMin, FlickerPeriodMax, (float)_rand.NextDouble());
			_dust.Add(new DustParticle
			{
				Position = origin,
				Velocity = dir * speed,
				Age = 0f,
				Lifetime = lifetime,
				Size = size,
				FlickerPeriod = flicker,
				FlickerOffset = (float)_rand.NextDouble() * flicker
			});
		}

		private static float Lerp(float a, float b, float t) => a + (b - a) * t;

		private static string WrapText(SpriteFont font, string text, int maxWidthPx, float scale)
		{
			if (font == null || string.IsNullOrEmpty(text) || maxWidthPx <= 0) return text ?? string.Empty;
			string[] words = text.Replace("\r", "").Split(' ');
			string line = string.Empty;
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			float scaledMax = maxWidthPx / Math.Max(0.0001f, scale);
			for (int i = 0; i < words.Length; i++)
			{
				string w = words[i];
				string testLine = string.IsNullOrEmpty(line) ? w : line + " " + w;
				if (font.MeasureString(testLine).X <= scaledMax)
				{
					line = testLine;
				}
				else
				{
					if (line.Length > 0)
					{
						sb.AppendLine(line);
						line = w;
					}
					else
					{
						// single word longer than max, hard-break
						int charCount = w.Length;
						int start = 0;
						while (start < charCount)
						{
							int len = 1;
							while (start + len <= charCount && font.MeasureString(w.Substring(start, len)).X <= scaledMax) len++;
							len = Math.Max(1, len - 1);
							sb.AppendLine(w.Substring(start, len));
							start += len;
						}
						line = string.Empty;
					}
				}
			}
			if (!string.IsNullOrEmpty(line)) sb.Append(line);
			return sb.ToString();
		}

        public void Draw()
        {
			if (_angelTexture == null)
            {
                // Content pipeline builds guardian_angel.png without extension
                _angelTexture = _content.Load<Texture2D>("guardian_angel");
                if (_angelTexture == null) return;
            }

            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var pt = player?.GetComponent<Transform>();
            if (pt == null) return;

            Vector2 baseRight = pt.Position + new Vector2(OffsetX, OffsetY);

            float ang = _t * AngularSpeed;

			// Blend between an ellipse and a figure eight (Lissajous-like):
            // ellipse: (cos t, sin t)
            // figure-eight: (sin t, sin 2t)
            float xEllipse = MathF.Cos(ang);
            float yEllipse = MathF.Sin(ang);
            float xEight = MathF.Sin(ang);
            float yEight = MathF.Sin(2f * ang);
            float x = xEllipse * (1f - FigureEightMix) + xEight * FigureEightMix;
            float y = yEllipse * (1f - FigureEightMix) + yEight * FigureEightMix;

            // Apply radii
            Vector2 motion = new Vector2(x * RadiusX, y * RadiusY);

			// Add a gentle vertical bob on top for smoothness
            float bob = MathF.Sin(_t * VerticalBobSpeed) * VerticalBob;
            motion.Y += bob;

			Vector2 pos = baseRight + motion;
			_pos = pos;

			var origin = new Vector2(_angelTexture.Width / 2f, _angelTexture.Height / 2f);
			// No rotation
			float rotation = 0f;

            var color = Color.White * Alpha;
			// Draw dust behind the angel
			for (int i = 0; i < _dust.Count; i++)
			{
				var p = _dust[i];
				float lifeT = MathF.Min(1f, p.Age / MathF.Max(0.0001f, p.Lifetime));
				float alpha = (1f - lifeT) * (0.6f + 0.4f * MathF.Sin(((p.Age + p.FlickerOffset) / MathF.Max(0.0001f, p.FlickerPeriod)) * MathF.Tau));
				var c = new Color(255, 245, 230) * MathHelper.Clamp(alpha, 0f, 1f);
				float s = p.Size;
				_spriteBatch.Draw(_pixel, p.Position, null, c, 0f, Vector2.Zero, new Vector2(s, s), SpriteEffects.None, 0f);
			}

			// Draw speech bubble (follows angel)
			if (_bubbleActive && _font != null)
			{
				string wrapped = WrapText(_font, _bubbleText, BubbleMaxWidth, BubbleTextScale);
				var size = _font.MeasureString(wrapped) * BubbleTextScale;
				int bw = (int)Math.Ceiling(size.X) + BubblePadX * 2;
				int bh = (int)Math.Ceiling(size.Y) + BubblePadY * 2;
				if (bw <= 0) bw = 1; if (bh <= 0) bh = 1;
				if (_bubbleTexture == null || bw != _bubbleTexW || bh != _bubbleTexH)
				{
					_bubbleTexture?.Dispose();
					_bubbleTexture = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, bw, bh, BubbleCornerRadius);
					_bubbleTexW = bw; _bubbleTexH = bh;
				}
				// Anchor bubble bottom-left to sprite top-right (plus offsets)
				Vector2 spriteTopRight = pos + new Vector2(_angelTexture.Width * 0.5f * Scale, -_angelTexture.Height * 0.5f * Scale);
				Vector2 bubbleBottomLeft = spriteTopRight + new Vector2(BubbleOffsetX, BubbleOffsetY);
				Vector2 bubbleTopLeft = bubbleBottomLeft - new Vector2(0, bh);
				// Compute fade alpha based on elapsed time
				float aIn = BubbleFadeInSeconds <= 0f ? 1f : MathHelper.Clamp(_bubbleElapsed / BubbleFadeInSeconds, 0f, 1f);
				float aOut = 1f;
				if (BubbleFadeOutSeconds > 0f)
				{
					float tRemain = Math.Max(0f, BubbleDuration - _bubbleElapsed);
					aOut = MathHelper.Clamp(tRemain / BubbleFadeOutSeconds, 0f, 1f);
				}
				float a = MathHelper.Clamp(aIn * aOut, 0f, 1f);
				Color bg = Color.White * (BubbleBgAlpha * a);
				_spriteBatch.Draw(_bubbleTexture, bubbleTopLeft, bg);
				// Text with wrapping
				Vector2 textPos = bubbleTopLeft + new Vector2(BubblePadX, BubblePadY);
				var textColor = new Color(0, 0, 0) * a;
				_spriteBatch.DrawString(_font, wrapped, textPos, textColor, 0f, Vector2.Zero, BubbleTextScale, SpriteEffects.None, 0f);
			}

			// Draw angel
			_spriteBatch.Draw(_angelTexture, pos, null, color, rotation, origin, Scale, SpriteEffects.None, 0f);
        }
    }
}


