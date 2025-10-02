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
	/// Draws the frost.png texture over cards that have the Frozen component.
	/// </summary>
	[DebugTab("Frozen Display")]
	public class FrozenCardDisplaySystem : Core.System
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly GraphicsDevice _graphicsDevice;
		private Texture2D _frostTexture;
		private CardVisualSettings _settings;

		[DebugEditable(DisplayName = "Frost Alpha", Step = 5, Min = 0, Max = 255)]
		public int FrostAlpha { get; set; } = 255;

		[DebugEditable(DisplayName = "Frost Scale", Step = 0.05f, Min = 0.1f, Max = 2.0f)]
		public float FrostScale { get; set; } = 1.0f;

		[DebugEditable(DisplayName = "Frost Offset X", Step = 1f, Min = -100f, Max = 100f)]
		public float FrostOffsetX { get; set; } = 0f;

		[DebugEditable(DisplayName = "Frost Offset Y", Step = 1f, Min = -100f, Max = 100f)]
		public float FrostOffsetY { get; set; } = 0f;

		private float _elapsedTime = 0f;

	public FrozenCardDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D frostTexture) 
		: base(entityManager)
	{
		_graphicsDevice = graphicsDevice;
		_spriteBatch = spriteBatch;
		_frostTexture = frostTexture;
		// Subscribe to CardRenderEvent to draw frost overlay right after each card is drawn
		EventManager.Subscribe<CardRenderEvent>(evt => FrameProfiler.Measure("FrozenCardDisplaySystem.OnCardRenderEvent", () => OnCardRenderEvent(evt)));
		// Subscribe to CardRenderScaledEvent for cards in modals/overlays
		EventManager.Subscribe<CardRenderScaledEvent>(evt => FrameProfiler.Measure("FrozenCardDisplaySystem.OnCardRenderScaledEvent", () => OnCardRenderScaledEvent(evt)));
	}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Frozen>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// Track time for pulse animation
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
		if (!ShouldRenderFrost(evt.Card)) return;

		var transform = evt.Card.GetComponent<Transform>();
		var ui = evt.Card.GetComponent<UIElement>();
		if (transform == null || ui == null) return;

		// Compute bounds exactly like CardHighlightSystem for consistent alignment
		var bounds = ComputeCardBounds(transform.Position);
		var center = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

		// Apply offset
		center.X += FrostOffsetX;
		center.Y += FrostOffsetY;

		// Render with card bounds dimensions
		DrawFrostOverlay(center, bounds.Width, bounds.Height, 1f);
	}

	private void OnCardRenderScaledEvent(CardRenderScaledEvent evt)
	{
		if (!ShouldRenderFrost(evt.Card)) return;

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

		// Apply frost-specific offsets (scaled by event scale)
		center.X += FrostOffsetX * evt.Scale;
		center.Y += FrostOffsetY * evt.Scale;

		// Render with scaled card dimensions
		DrawFrostOverlay(center, cardWidth, cardHeight, evt.Scale);
	}

	private bool ShouldRenderFrost(Entity card)
	{
		return card != null 
			&& card.GetComponent<Frozen>() != null 
			&& _frostTexture != null;
	}

	private void DrawFrostOverlay(Vector2 center, float cardWidth, float cardHeight, float scale)
	{
		// Calculate final alpha
		float finalAlpha = FrostAlpha / 255f;
		finalAlpha = MathHelper.Clamp(finalAlpha, 0f, 1f);

		// Calculate final scale based on card size and scale factor
		float scaleX = (cardWidth / (float)_frostTexture.Width) * FrostScale * scale;
		float scaleY = (cardHeight / (float)_frostTexture.Height) * FrostScale * scale;
		var spriteScale = new Vector2(scaleX, scaleY);

		// Draw the frost texture centered on the card
		_spriteBatch.Draw(
			_frostTexture,
			center,
			null,
			Color.White * finalAlpha,
			0,
			new Vector2(_frostTexture.Width / 2f, _frostTexture.Height / 2f),
			spriteScale,
			SpriteEffects.None,
			0f
		);
	}
	}
}


