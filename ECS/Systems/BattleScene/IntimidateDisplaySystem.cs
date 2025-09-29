using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Draws "INTIMIDATED!" diagonally over cards that have the Intimidated component.
	/// </summary>
	public class IntimidateDisplaySystem : Core.System
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private readonly GraphicsDevice _graphicsDevice;
		private Texture2D _pixel;

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

				// Get card bounds and center
				var bounds = ui.Bounds;
				var center = new Vector2(
					bounds.X + bounds.Width / 2f,
					bounds.Y + bounds.Height / 2f
				);

				// Text to display
				string text = "INTIMIDATED!";
				float textScale = 0.5f; // Adjust size as needed
				
				// Measure text size
				var textSize = _font.MeasureString(text) * textScale;

				// Draw semi-transparent dark overlay first for better text visibility
				var overlayColor = new Color(0, 0, 0, 180); // Dark semi-transparent
				_spriteBatch.Draw(
					_pixel,
					bounds,
					overlayColor
				);

				// Calculate rotation (45 degrees in radians)
				float rotation = MathHelper.ToRadians(-45f);

				// Text position (centered on card)
				var textOrigin = textSize / 2f;

				// Draw text with red color and diagonal rotation
				_spriteBatch.DrawString(
					_font,
					text,
					center,
					Color.Red,
					rotation,
					textOrigin,
					textScale,
					SpriteEffects.None,
					0f
				);

				// Draw a second layer with slight offset for "bold" effect
				_spriteBatch.DrawString(
					_font,
					text,
					center + new Vector2(1, 1),
					new Color(139, 0, 0), // Dark red for shadow/bold effect
					rotation,
					textOrigin,
					textScale,
					SpriteEffects.None,
					0f
				);
			}
		}
	}
}
