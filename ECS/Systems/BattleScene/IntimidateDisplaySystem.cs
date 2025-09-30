using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Draws "INTIMIDATED!" diagonally over cards that have the Intimidated component.
	/// </summary>
	[DebugTab("Intimidate Display")]
	public class IntimidateDisplaySystem : Core.System
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private readonly GraphicsDevice _graphicsDevice;
		private Texture2D _pixel;
		private readonly System.Collections.Generic.Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
		private CardVisualSettings _settings;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.1f, Max = 2.0f)]
		public float TextScale { get; set; } = 0.3f;

		[DebugEditable(DisplayName = "Rotation (Degrees)", Step = 1f, Min = -180f, Max = 180f)]
		public float RotationDegrees { get; set; } = -55f;

		[DebugEditable(DisplayName = "Overlay Alpha", Step = 5, Min = 0, Max = 255)]
		public int OverlayAlpha { get; set; } = 155;

		[DebugEditable(DisplayName = "Overlay Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int OverlayCornerRadius { get; set; } = 12;

		[DebugEditable(DisplayName = "Shadow Red", Step = 5, Min = 0, Max = 255)]
		public int ShadowRed { get; set; } = 0;

		[DebugEditable(DisplayName = "Shadow Green", Step = 5, Min = 0, Max = 255)]
		public int ShadowGreen { get; set; } = 0;

		[DebugEditable(DisplayName = "Shadow Blue", Step = 5, Min = 0, Max = 255)]
		public int ShadowBlue { get; set; } = 0;

		[DebugEditable(DisplayName = "Shadow Offset X", Step = 0.5f, Min = -10f, Max = 10f)]
		public float ShadowOffsetX { get; set; } = 5f;

		[DebugEditable(DisplayName = "Shadow Offset Y", Step = 0.5f, Min = -10f, Max = 10f)]
		public float ShadowOffsetY { get; set; } = 5f;

		public string IntimidateText { get; set; } = "INTIMIDATED!";

		public IntimidateDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font) 
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_font = font;
			
			// Create a single pixel texture for backgrounds/overlays
			_pixel = new Texture2D(_graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Intimidated>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// No per-entity update logic needed; drawing is handled separately
		}

	public void Draw()
	{
		var intimidatedCards = GetRelevantEntities().ToList();
		if (intimidatedCards.Count == 0) return;

		foreach (var card in intimidatedCards)
		{
			var ui = card.GetComponent<UIElement>();
			var transform = card.GetComponent<Transform>();
			
			if (ui == null || transform == null) continue;

			// Compute bounds exactly like CardHighlightSystem for consistent alignment
			var bounds = ComputeCardBounds(transform.Position);
			var center = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

			// Measure text size (unscaled) so the origin remains centered regardless of TextScale
			var textSizeUnscaled = _font.MeasureString(IntimidateText);

			// Get or create rounded rect texture for overlay (match the card's visual radius)
			_settings ??= EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
			int baseRadius = OverlayCornerRadius > 0 ? OverlayCornerRadius : (_settings?.CardCornerRadius ?? 12);
			var overlayKey = (bounds.Width, bounds.Height, baseRadius);
			if (!_roundedRectCache.TryGetValue(overlayKey, out var roundedRect))
			{
				roundedRect = RoundedRectTextureFactory.CreateRoundedRect(
					_graphicsDevice,
					bounds.Width,
					bounds.Height,
					baseRadius
				);
				_roundedRectCache[overlayKey] = roundedRect;
			}

			// Draw semi-transparent dark overlay with rounded corners
			// Use the card's rotation to match the card orientation
			var overlayColor = new Color(0, 0, 0, OverlayAlpha);
			_spriteBatch.Draw(
				roundedRect,
				center,
				null,
				overlayColor,
				transform.Rotation,
				new Vector2(bounds.Width / 2f, bounds.Height / 2f),
				1f,
				SpriteEffects.None,
				0f
			);

			// Calculate rotation (combine card rotation with the intimidate rotation)
			float rotation = transform.Rotation + MathHelper.ToRadians(RotationDegrees);

			// Text position (centered on card)
			var textOrigin = textSizeUnscaled / 2f;

			// Draw shadow/bold layer first
			var shadowColor = new Color(ShadowRed, ShadowGreen, ShadowBlue);
			_spriteBatch.DrawString(
				_font,
				IntimidateText,
				center + new Vector2(ShadowOffsetX, ShadowOffsetY),
				shadowColor,
				rotation,
				textOrigin,
				TextScale,
				SpriteEffects.None,
				0f
			);

			// Draw text with main color
			_spriteBatch.DrawString(
				_font,
				IntimidateText,
				center,
				Color.DarkRed,
				rotation,
				textOrigin,
				TextScale,
				SpriteEffects.None,
				0f
			);
		}
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
	}
}
