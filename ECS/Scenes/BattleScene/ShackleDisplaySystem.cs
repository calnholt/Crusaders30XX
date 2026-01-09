using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Draws the shackles.png texture over cards that have the Shackle component.
	/// Fades in and out slowly.
	/// </summary>
	[DebugTab("Shackle Display")]
	public class ShackleDisplaySystem : Core.System
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly GraphicsDevice _graphicsDevice;
		private Texture2D _shackleTexture;
		private CardVisualSettings _settings;

	[DebugEditable(DisplayName = "Min Alpha", Step = 5, Min = 0, Max = 255)]
	public int MinAlpha { get; set; } = 40;

	[DebugEditable(DisplayName = "Max Alpha", Step = 5, Min = 0, Max = 255)]
	public int MaxAlpha { get; set; } = 110;

	[DebugEditable(DisplayName = "Fade Speed (Hz)", Step = 0.1f, Min = 0.1f, Max = 5.0f)]
	public float FadeSpeed { get; set; } = 0.2f;

	[DebugEditable(DisplayName = "Shackle Scale", Step = 0.05f, Min = 0.1f, Max = 2.0f)]
	public float ShackleScale { get; set; } = 0.95f;

		[DebugEditable(DisplayName = "Shackle Offset X", Step = 1f, Min = -100f, Max = 100f)]
		public float ShackleOffsetX { get; set; } = 0f;

		[DebugEditable(DisplayName = "Shackle Offset Y", Step = 1f, Min = -100f, Max = 100f)]
		public float ShackleOffsetY { get; set; } = 0f;

		private float _elapsedTime = 0f;

		public ShackleDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D shackleTexture) 
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_shackleTexture = shackleTexture;
			
			// Subscribe to CardRenderEvent to draw shackle overlay right after each card is drawn
			EventManager.Subscribe<CardRenderEvent>(evt => FrameProfiler.Measure("ShackleDisplaySystem.OnCardRenderEvent", () => OnCardRenderEvent(evt)));
			// Subscribe to CardRenderScaledEvent for cards in modals/overlays
			EventManager.Subscribe<CardRenderScaledEvent>(evt => FrameProfiler.Measure("ShackleDisplaySystem.OnCardRenderScaledEvent", () => OnCardRenderScaledEvent(evt)));
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Shackle>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// Track time for fade animation
			if (entity == null) return;
			_elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
		}

		public void Draw()
		{
		}

		private Rectangle ComputeCardBounds(Vector2 position)
		{
			_settings ??= EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
			int cw = _settings?.CardWidth ?? 250;
			int ch = _settings?.CardHeight ?? 350;
			int offsetYExtra = _settings?.CardOffsetYExtra ?? (int)Math.Round((_settings?.UIScale ?? 1f) * 25);
			return new Rectangle(
				(int)position.X - cw / 2,
				(int)position.Y - (ch / 2 + offsetYExtra),
				cw,
				ch
			);
		}

		private void OnCardRenderEvent(CardRenderEvent evt)
		{
			if (!ShouldRenderShackles(evt.Card)) return;

			var transform = evt.Card.GetComponent<Transform>();
			var ui = evt.Card.GetComponent<UIElement>();
			if (transform == null || ui == null) return;

			// Compute bounds exactly like CardHighlightSystem for consistent alignment
			var bounds = ComputeCardBounds(transform.Position);
			var center = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

			// Apply offset
			center.X += ShackleOffsetX;
			center.Y += ShackleOffsetY;

			// Render with card bounds dimensions
			DrawShackleOverlay(center, bounds.Width, bounds.Height, 1f, transform.Rotation);
		}

		private void OnCardRenderScaledEvent(CardRenderScaledEvent evt)
		{
			if (!ShouldRenderShackles(evt.Card)) return;

			var transform = evt.Card.GetComponent<Transform>();
			if (transform == null) return;

			// Get card dimensions and settings
			_settings ??= EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
			int cardWidth = _settings?.CardWidth ?? 250;
			int cardHeight = _settings?.CardHeight ?? 350;
			int cardOffsetYExtra = _settings?.CardOffsetYExtra ?? 0;

			// For scaled rendering, CardDisplaySystem applies CardOffsetYExtra which shifts the card up.
			// The actual visual center of the card is at position.Y - (CardOffsetYExtra * scale)
			var center = evt.Position;
			int offsetY = (int)Math.Round(cardOffsetYExtra * evt.Scale);
			center.Y -= offsetY;

			// Apply shackle-specific offsets (scaled by event scale)
			center.X += ShackleOffsetX * evt.Scale;
			center.Y += ShackleOffsetY * evt.Scale;

			// Render with scaled card dimensions
			DrawShackleOverlay(center, cardWidth, cardHeight, evt.Scale, transform.Rotation);
		}

		private bool ShouldRenderShackles(Entity card)
		{
			return card != null 
				&& card.GetComponent<Shackle>() != null 
				&& _shackleTexture != null;
		}

		private void DrawShackleOverlay(Vector2 center, float cardWidth, float cardHeight, float scale, float rotation)
		{
			// Calculate final alpha using a sine wave for slow fade
			float t = _elapsedTime * FadeSpeed * MathHelper.TwoPi;
			float sine = (float)Math.Sin(t);
			float lerpFactor = (sine + 1f) / 2f; // 0..1
			
			float minA = MinAlpha / 255f;
			float maxA = MaxAlpha / 255f;
			float finalAlpha = MathHelper.Lerp(minA, maxA, lerpFactor);
			finalAlpha = MathHelper.Clamp(finalAlpha, 0f, 1f);

			// Calculate final scale based on card size and scale factor
			// Stretching to fit over card as requested
			float scaleX = (cardWidth / (float)_shackleTexture.Width) * ShackleScale * scale;
			float scaleY = (cardHeight / (float)_shackleTexture.Height) * ShackleScale * scale;
			var spriteScale = new Vector2(scaleX, scaleY);

			// Draw the shackle texture centered on the card
			_spriteBatch.Draw(
				_shackleTexture,
				center,
				null,
				Color.White * finalAlpha,
				rotation,
				new Vector2(_shackleTexture.Width / 2f, _shackleTexture.Height / 2f),
				spriteScale,
				SpriteEffects.None,
				0f
			);
		}
	}
}
