using System;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Displays a transient, fading message at screen center when a card cannot be played.
	/// </summary>
	[DebugTab("Cant Play Message")] 
	public class CantPlayCardMessageSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.ContentFont;

		[DebugEditable(DisplayName = "Duration (s)", Step = 0.1f, Min = 0.2f, Max = 10f)]
		public float DurationSec { get; set; } = 1.0f;

		[DebugEditable(DisplayName = "Fade In (s)", Step = 0.05f, Min = 0.0f, Max = 2.0f)]
		public float FadeInSec { get; set; } = 0.15f;

		[DebugEditable(DisplayName = "Fade Out (s)", Step = 0.05f, Min = 0.0f, Max = 2.0f)]
		public float FadeOutSec { get; set; } = 0.1f;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.3f, Max = 3.0f)]
		public float TextScale { get; set; } = 0.25f;

		[DebugEditable(DisplayName = "Offset X", Step = 1, Min = -2000, Max = 2000)]
		public int OffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Offset Y", Step = 1, Min = -2000, Max = 2000)]
		public int OffsetY { get; set; } = -110;

		// Blip animation controls
		[DebugEditable(DisplayName = "Blip In Start Scale", Step = 0.05f, Min = 0f, Max = 1f)]
		public float BlipInStartScale { get; set; } = 0.4f;

		[DebugEditable(DisplayName = "Blip Out End Scale", Step = 0.05f, Min = 0f, Max = 1f)]
		public float BlipOutEndScale { get; set; } = 0f;

		[DebugEditable(DisplayName = "Blip Overshoot", Step = 0.01f, Min = 0f, Max = 0.6f)]
		public float BlipOvershoot { get; set; } = 0.12f;

		[DebugEditable(DisplayName = "Lock Snap Threshold", Step = 0.005f, Min = 0f, Max = 0.2f)]
		public float LockSnapThreshold { get; set; } = 0.095f;

		private string _activeMessage = string.Empty;
		private float _elapsed = 0f;
		private bool _isActive = false;

		public CantPlayCardMessageSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			EventManager.Subscribe<CantPlayCardMessage>(OnCantPlay);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (!_isActive) return;
			_elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (_elapsed >= DurationSec) { _isActive = false; _activeMessage = string.Empty; }
		}

		private void OnCantPlay(CantPlayCardMessage evt)
		{
			if (evt == null || string.IsNullOrWhiteSpace(evt.Message)) return;
			_activeMessage = evt.Message;
			_elapsed = 0f;
			_isActive = true;
		}

		public void Draw()
		{
			if (!_isActive || string.IsNullOrEmpty(_activeMessage)) return;
			int w = Game1.VirtualWidth;
			int h = Game1.VirtualHeight;
			// Compute alpha with fade-in/out (opacity)
			float t = MathHelper.Clamp(_elapsed, 0f, DurationSec);
			float alpha = 1f;
			if (t < FadeInSec) alpha = MathHelper.Clamp(t / Math.Max(0.0001f, FadeInSec), 0f, 1f);
			else if (DurationSec - t < FadeOutSec) alpha = MathHelper.Clamp((DurationSec - t) / Math.Max(0.0001f, FadeOutSec), 0f, 1f);

			// "Blip" scale animation: start at 0 -> overshoot -> settle to 1; then shrink to 0
			float fullScale = TextScale;
			float sMul = 1f;
			if (t < FadeInSec)
			{
				float u = MathHelper.Clamp(t / Math.Max(0.0001f, FadeInSec), 0f, 1f);
				float overshoot = 1.70158f + BlipOvershoot * 2f;
				// EaseOutBack from 0..1, then map to [BlipInStartScale .. 1+BlipOvershoot]
				float outBack = EaseOutBack(u, overshoot);
				sMul = MathHelper.Lerp(BlipInStartScale, 1f + BlipOvershoot, outBack);
			}
			else if (DurationSec - t < FadeOutSec)
			{
				float u = MathHelper.Clamp(1f - (DurationSec - t) / Math.Max(0.0001f, FadeOutSec), 0f, 1f); // 0 -> 1 during fade-out
				float overshoot = 1.70158f + BlipOvershoot * 2f;
				float inBack = EaseInBack(u, overshoot);
				sMul = MathHelper.Lerp(1f, MathHelper.Clamp(BlipOutEndScale, 0f, 1f), inBack);
			}
			else
			{
				// Mid section: lock scale exactly to 1 to avoid jitter
				sMul = 1f;
			}
			// Snap near 1 to avoid tiny jitter
			if (Math.Abs(sMul - 1f) < LockSnapThreshold) sMul = 1f;
			fullScale *= sMul;

			// Keep text white; only alpha animates
			var color = new Color(Color.White, alpha);

			// Center using the dynamic scale
			var unscaled = _font.MeasureString(_activeMessage);
			var size = unscaled * fullScale;
			var pos = new Vector2((w - size.X) / 2f + OffsetX, (h - size.Y) / 2f + OffsetY);
			_spriteBatch.DrawString(_font, _activeMessage, pos, color, 0f, Vector2.Zero, fullScale, SpriteEffects.None, 0f);
		}

		private static float EaseOutBack(float x, float s)
		{
			float c3 = s + 1f;
			float xm1 = x - 1f;
			return 1f + c3 * xm1 * xm1 * xm1 + s * xm1 * xm1;
		}

		private static float EaseInBack(float x, float s)
		{
			float c3 = s + 1f;
			return c3 * x * x * x - s * x * x;
		}
	}
}


