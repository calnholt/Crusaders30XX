using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Draws the seal.png texture over cards that have the Sealed component.
	/// Also displays the crack count (X/3) on the seal.
	/// </summary>
	[DebugTab("Seal Display")]
	public class SealDisplaySystem : Core.System
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly GraphicsDevice _graphicsDevice;
		private Texture2D _sealTexture;
		private CardVisualSettings _settings;

		[DebugEditable(DisplayName = "Seal Alpha", Step = 5, Min = 0, Max = 255)]
		public int SealAlpha { get; set; } = 255;

		[DebugEditable(DisplayName = "Seal Scale", Step = 0.05f, Min = 0.1f, Max = 2.0f)]
		public float SealScale { get; set; } = 0.75f;

		[DebugEditable(DisplayName = "Seal Offset X", Step = 1f, Min = -100f, Max = 100f)]
		public float SealOffsetX { get; set; } = 0f;

		[DebugEditable(DisplayName = "Seal Offset Y", Step = 1f, Min = -100f, Max = 100f)]
		public float SealOffsetY { get; set; } = 0f;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.01f, Min = 0.05f, Max = 3.0f)]
		public float TextScale { get; set; } = 0.15f;

		[DebugEditable(DisplayName = "Text Offset Y", Step = 1f, Min = -100f, Max = 100f)]
		public float TextOffsetY { get; set; } = -82f;

		public SealDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D sealTexture)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_sealTexture = sealTexture;
			EventManager.Subscribe<CardRenderEvent>(evt => FrameProfiler.Measure("SealDisplaySystem.OnCardRenderEvent", () => OnCardRenderEvent(evt)));
			EventManager.Subscribe<CardRenderScaledEvent>(evt => FrameProfiler.Measure("SealDisplaySystem.OnCardRenderScaledEvent", () => OnCardRenderScaledEvent(evt)));
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Sealed>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// No per-frame update needed
		}

		public void Draw()
		{
			// Drawing is handled via event subscriptions
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
			if (!ShouldRenderSeal(evt.Card)) return;

			var transform = evt.Card.GetComponent<Transform>();
			var ui = evt.Card.GetComponent<UIElement>();
			if (transform == null || ui == null) return;

			var bounds = ComputeCardBounds(transform.Position);
			var center = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

			center.X += SealOffsetX;
			center.Y += SealOffsetY;

			var sealedComp = evt.Card.GetComponent<Sealed>();
			DrawSealOverlay(center, bounds.Width, bounds.Height, 1f, sealedComp?.Cracks ?? 0);
		}

		private void OnCardRenderScaledEvent(CardRenderScaledEvent evt)
		{
			if (!ShouldRenderSeal(evt.Card)) return;

			var transform = evt.Card.GetComponent<Transform>();
			if (transform == null) return;

			_settings ??= EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
			int cardWidth = _settings?.CardWidth ?? 250;
			int cardHeight = _settings?.CardHeight ?? 350;
			int cardOffsetYExtra = _settings?.CardOffsetYExtra ?? 0;

			var center = evt.Position;
			int offsetY = (int)Math.Round(cardOffsetYExtra * evt.Scale);
			center.Y -= offsetY;

			center.X += SealOffsetX * evt.Scale;
			center.Y += SealOffsetY * evt.Scale;

			var sealedComp = evt.Card.GetComponent<Sealed>();
			DrawSealOverlay(center, cardWidth, cardHeight, evt.Scale, sealedComp?.Cracks ?? 0);
		}

		private bool ShouldRenderSeal(Entity card)
		{
			return card != null
				&& card.GetComponent<Sealed>() != null
				&& _sealTexture != null;
		}

		private void DrawSealOverlay(Vector2 center, float cardWidth, float cardHeight, float scale, int cracks)
		{
			float finalAlpha = SealAlpha / 255f;
			finalAlpha = MathHelper.Clamp(finalAlpha, 0f, 1f);

			// Use uniform scale (no stretching) - fit to card width
			float uniformScale = (cardWidth / (float)_sealTexture.Width) * SealScale * scale;

			// Draw the seal texture centered on the card with uniform scale
			_spriteBatch.Draw(
				_sealTexture,
				center,
				null,
				Color.White * finalAlpha,
				0,
				new Vector2(_sealTexture.Width / 2f, _sealTexture.Height / 2f),
				uniformScale,
				SpriteEffects.None,
				0f
			);

			// Draw crack count text at bottom right of card
			DrawCrackCount(center, cardWidth, cardHeight, scale, cracks);
		}

		private void DrawCrackCount(Vector2 center, float cardWidth, float cardHeight, float scale, int cracks)
		{
			var font = FontSingleton.ContentFont;
			if (font == null) return;

			string text = $"{cracks}/3";
			var textSize = font.MeasureString(text);
			float textScaleFinal = TextScale * scale;

			// Position text at bottom right of the card
			float padding = 8f * scale;
			var textPos = new Vector2(
				center.X + (cardWidth * scale / 2f) - (textSize.X * textScaleFinal) - padding,
				center.Y + (cardHeight * scale / 2f) - (textSize.Y * textScaleFinal) - padding + TextOffsetY * scale
			);

			// Color #00ffbf (cyan/teal)
			var textColor = new Color(0x00, 0xff, 0xbf);

			// Draw text with shadow for readability
			var shadowOffset = new Vector2(2 * scale, 2 * scale);
			_spriteBatch.DrawString(font, text, textPos + shadowOffset, Color.Black * 0.7f, 0f, Vector2.Zero, textScaleFinal, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(font, text, textPos, textColor, 0f, Vector2.Zero, textScaleFinal, SpriteEffects.None, 0f);
		}
	}
}
