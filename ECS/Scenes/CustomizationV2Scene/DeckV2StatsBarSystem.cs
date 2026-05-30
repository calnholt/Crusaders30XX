using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("DeckV2 Stats")]
	public class DeckV2StatsBarSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _headingFont = FontSingleton.TitleFont;
		private readonly SpriteFont _contentFont = FontSingleton.ContentFont;
		private Texture2D _pixel;

		[DebugEditable(DisplayName = "Bar Height", Step = 2, Min = 24, Max = 80)]
		public int BarHeight { get; set; } = 50;

		[DebugEditable(DisplayName = "Bar BG R", Step = 1, Min = 0, Max = 255)]
		public int BarBgR { get; set; } = 26;

		[DebugEditable(DisplayName = "Bar BG G", Step = 1, Min = 0, Max = 255)]
		public int BarBgG { get; set; } = 26;

		[DebugEditable(DisplayName = "Bar BG B", Step = 1, Min = 0, Max = 255)]
		public int BarBgB { get; set; } = 26;

		[DebugEditable(DisplayName = "Count Scale", Step = 0.01f, Min = 0.05f, Max = 0.5f)]
		public float CountScale { get; set; } = 0.22f;

		[DebugEditable(DisplayName = "Denom Scale", Step = 0.01f, Min = 0.05f, Max = 0.5f)]
		public float DenomScale { get; set; } = 0.16f;

		[DebugEditable(DisplayName = "Pip Radius", Step = 1, Min = 2, Max = 20)]
		public int PipRadius { get; set; } = 7;

		[DebugEditable(DisplayName = "Pip Gap", Step = 1, Min = 0, Max = 20)]
		public int PipGap { get; set; } = 6;

		[DebugEditable(DisplayName = "Pip Count Gap", Step = 1, Min = 0, Max = 20)]
		public int PipCountGap { get; set; } = 4;

		[DebugEditable(DisplayName = "Section Gap", Step = 2, Min = 4, Max = 60)]
		public int SectionGap { get; set; } = 28;

		[DebugEditable(DisplayName = "Pad X", Step = 2, Min = 0, Max = 60)]
		public int PadX { get; set; } = 28;

		[DebugEditable(DisplayName = "Pip Count Scale", Step = 0.01f, Min = 0.05f, Max = 0.3f)]
		public float PipCountScale { get; set; } = 0.12f;

		[DebugEditable(DisplayName = "Header Offset Y", Step = 1, Min = 0, Max = 20)]
		public int HeaderOffsetY { get; set; } = 0;

		public CustomizationV2HeaderSystem HeaderSystem { get; set; }

		public DeckV2StatsBarSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Deck) return;

			var deck = EntityManager.GetEntitiesWithComponent<CustomizationV2DeckState>().FirstOrDefault()?.GetComponent<CustomizationV2DeckState>();
			if (deck == null) return;

			if (_pixel == null)
			{
				_pixel = new Texture2D(_graphicsDevice, 1, 1);
				_pixel.SetData(new[] { Color.White });
			}

			int headerH = HeaderSystem?.HeaderHeight ?? 56;
			int barY = headerH + HeaderOffsetY;
			int vw = Game1.VirtualWidth;

			// Bar background
			_spriteBatch.Draw(_pixel, new Rectangle(0, barY, vw, BarHeight), new Color(BarBgR, BarBgG, BarBgB));
			// Bottom border
			_spriteBatch.Draw(_pixel, new Rectangle(0, barY + BarHeight - 1, vw, 1), new Color(51, 51, 51));

			int count = deck.DeckCardKeys?.Count ?? 0;

			string countStr = count.ToString();
			string denomStr = " cards";
			var countSize = _headingFont.MeasureString(countStr) * CountScale;
			var denomSize = _headingFont.MeasureString(denomStr) * DenomScale;

			float textY = barY + (BarHeight - countSize.Y) / 2f;
			float denomY = barY + (BarHeight - denomSize.Y) / 2f;
			var countColor = Color.White;
			_spriteBatch.DrawString(_headingFont, countStr, new Vector2(PadX, textY), countColor, 0f, Vector2.Zero, CountScale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_headingFont, denomStr, new Vector2(PadX + countSize.X, denomY), new Color(85, 85, 85), 0f, Vector2.Zero, DenomScale, SpriteEffects.None, 0f);

			// Color pip counts
			int whiteCount = 0, redCount = 0, blackCount = 0;
			if (deck.DeckCardKeys != null)
			{
				foreach (var key in deck.DeckCardKeys)
				{
					int sep = key.IndexOf('|');
					if (sep < 0) continue;
					var colorStr = key.Substring(sep + 1);
					switch (colorStr)
					{
						case "White": whiteCount++; break;
						case "Red": redCount++; break;
						case "Black": blackCount++; break;
					}
				}
			}

			float pipX = PadX + countSize.X + denomSize.X + SectionGap;
			float pipY = barY + BarHeight / 2f;

			DrawColorPip(pipX, pipY, new Color(224, 224, 224), whiteCount);
			pipX += PipRadius * 2 + PipCountGap + _contentFont.MeasureString(whiteCount.ToString()).X * PipCountScale + SectionGap;

			DrawColorPip(pipX, pipY, new Color(160, 0, 0), redCount);
			pipX += PipRadius * 2 + PipCountGap + _contentFont.MeasureString(redCount.ToString()).X * PipCountScale + SectionGap;

			DrawColorPip(pipX, pipY, new Color(51, 51, 51), blackCount);
		}

		private void DrawColorPip(float x, float y, Color pipColor, int count)
		{
			var circle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, PipRadius);
			int d = PipRadius * 2;
			_spriteBatch.Draw(circle, new Rectangle((int)x, (int)(y - PipRadius), d, d), pipColor);

			string countStr = count.ToString();
			var countSize = _contentFont.MeasureString(countStr) * PipCountScale;
			float textX = x + d + PipCountGap;
			float textY = y - countSize.Y / 2f;
			_spriteBatch.DrawString(_contentFont, countStr, new Vector2(textX, textY), new Color(200, 200, 200), 0f, Vector2.Zero, PipCountScale, SpriteEffects.None, 0f);
		}
	}
}
