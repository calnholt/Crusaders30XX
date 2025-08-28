using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Displays the player's Stored Block as a square icon (black fill, white outline) with centered white text.
	/// Positioned to the right of Temperance by default.
	/// </summary>
	[DebugTab("Stored Block Display")]
	public class StoredBlockDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private Texture2D _pixel;

		// Debug fields
		[DebugEditable(DisplayName = "Square Width", Step = 1, Min = 8, Max = 512)]
		public int SquareWidth { get; set; } = 44;

		[DebugEditable(DisplayName = "Square Height", Step = 1, Min = 8, Max = 512)]
		public int SquareHeight { get; set; } = 52;

		[DebugEditable(DisplayName = "Outline Thickness", Step = 1, Min = 1, Max = 32)]
		public int OutlineThickness { get; set; } = 2;

		[DebugEditable(DisplayName = "Anchor Offset X", Step = 2, Min = -2000, Max = 2000)]
		public int AnchorOffsetX { get; set; } = 124;

		[DebugEditable(DisplayName = "Anchor Offset Y", Step = 2, Min = -2000, Max = 2000)]
		public int AnchorOffsetY { get; set; } = 230;

		[DebugEditable(DisplayName = "Text Scale Divisor", Step = 1, Min = 1, Max = 200)]
		public int TextScaleDivisor { get; set; } = 54;

		[DebugEditable(DisplayName = "Text Offset X", Step = 1, Min = -500, Max = 500)]
		public int TextOffsetX { get; set; } = 1;

		[DebugEditable(DisplayName = "Text Offset Y", Step = 1, Min = -500, Max = 500)]
		public int TextOffsetY { get; set; } = 0;

		public StoredBlockDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_font = font;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Player>().Where(e => e.HasComponent<StoredBlock>());
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var player = GetRelevantEntities().FirstOrDefault();
			if (player == null) return;
			var stored = player.GetComponent<StoredBlock>();
			if (stored == null) return;

			var anchor = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (anchor == null) return;
			var t = anchor.GetComponent<Transform>();
			var info = anchor.GetComponent<PortraitInfo>();
			if (t == null || info == null || _font == null) return;

			int w = Math.Max(8, SquareWidth);
			int h = Math.Max(8, SquareHeight);
			int outline = Math.Max(1, OutlineThickness);

			// Position to the right of temperance by default
			var center = new Vector2(t.Position.X + AnchorOffsetX, t.Position.Y + AnchorOffsetY);

			// Draw outer white rectangle and inner black rectangle for border effect
			int x = (int)Math.Round(center.X - w / 2f);
			int y = (int)Math.Round(center.Y - h / 2f);
			var outerRect = new Rectangle(x, y, w, h);
			var innerRect = new Rectangle(x + outline, y + outline, Math.Max(1, w - outline * 2), Math.Max(1, h - outline * 2));
			_spriteBatch.Draw(_pixel, outerRect, Color.White);
			_spriteBatch.Draw(_pixel, innerRect, Color.Black);

			// Text
			string text = Math.Max(0, stored.Amount).ToString();
			float textScale = Math.Min(1.0f, Math.Min(w, h) / Math.Max(1f, (float)TextScaleDivisor));
			var size = _font.MeasureString(text) * textScale;
			var pos = new Vector2(center.X - size.X / 2f + TextOffsetX, center.Y - size.Y / 2f + TextOffsetY);
			_spriteBatch.DrawString(_font, text, pos, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

			// Update hoverable UI element bounds over the stored block square for tooltip (entity pre-created in factory)
			var hover = EntityManager.GetEntitiesWithComponent<StoredBlockTooltipAnchor>().FirstOrDefault();
			var hitRect = outerRect; // use the exact outer rect as hit area
			var ui = hover.GetComponent<UIElement>();
			if (ui != null) ui.Bounds = hitRect;
			var ht = hover.GetComponent<Transform>();
			if (ht != null) ht.Position = new Vector2(hitRect.X, hitRect.Y);
		}


	}
}


