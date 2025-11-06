using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Data.Cards;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Deck Info Display")]
	public class DeckInfoDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.ContentFont;
		private readonly Texture2D _pixel;

		[DebugEditable(DisplayName = "Panel Padding X", Step = 1, Min = 0, Max = 200)]
		public int PadX { get; set; } = 16;
		[DebugEditable(DisplayName = "Panel Padding Y", Step = 1, Min = 0, Max = 200)]
		public int PadY { get; set; } = 10;
		[DebugEditable(DisplayName = "Text Scale", Step = 0.01f, Min = 0.1f, Max = 2.0f)]
		public float TextScale { get; set; } = 0.16f;
		[DebugEditable(DisplayName = "Background Alpha", Step = 5, Min = 0, Max = 255)]
		public int BgAlpha { get; set; } = 225;

		public DeckInfoDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_pixel = new Texture2D(_graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// purely drawn in Draw()
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Customization) return;
			var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
			if (st == null) return;

			int vw = _graphicsDevice.Viewport.Width;
			int vh = _graphicsDevice.Viewport.Height;

			// Tally counts
			int total = 0, white = 0, red = 0, black = 0;
			foreach (var entry in st.WorkingCardIds)
			{
				var color = ParseColorFromKey(entry);
				total++;
				switch (color)
				{
					case CardData.CardColor.White: white++; break;
					case CardData.CardColor.Red: red++; break;
					case CardData.CardColor.Black: black++; break;
				}
			}

			// Compose lines
			string sizeLine = $"Deck Size: {total}/{Data.Loadouts.DeckRules.RequiredDeckSize}";
			string whiteLine = $"# of White: {white}";
			string redLine = $"# of Red: {red}";
			string blackLine = $"# of Black: {black}";

			// Build over-limit warnings (3+ copies of same name ignoring color)
			var idToName = CardDefinitionCache.GetAll()
				.ToDictionary(kv => (kv.Key ?? string.Empty).ToLowerInvariant(),
							  kv => ((kv.Value?.name ?? kv.Value?.id) ?? string.Empty).Trim());
			var nameCounts = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
			foreach (var entry in st.WorkingCardIds)
			{
				var baseId = ((entry?.Split('|')[0]) ?? string.Empty).ToLowerInvariant();
				var displayName = idToName.TryGetValue(baseId, out var n) ? n : baseId;
				nameCounts[displayName] = (nameCounts.TryGetValue(displayName, out var c) ? c : 0) + 1;
			}
			var overLimit = nameCounts.Where(kv => kv.Value > 2)
				.OrderBy(kv => kv.Key, System.StringComparer.OrdinalIgnoreCase)
				.ToList();
			string warnHeader = overLimit.Count > 0 ? "You can't have three copies of the same card:" : string.Empty;
			var warnLines = new System.Collections.Generic.List<string>();
			if (overLimit.Count > 0)
			{
				foreach (var kv in overLimit)
				{
					warnLines.Add($"- {kv.Key} x{kv.Value}");
				}
			}

			// Measure to center panel
			var s1 = _font.MeasureString(sizeLine) * TextScale;
			var s2 = _font.MeasureString(whiteLine) * TextScale;
			var s3 = _font.MeasureString(redLine) * TextScale;
			var s4 = _font.MeasureString(blackLine) * TextScale;
			int lineGap = 6;
			int contentW = (int)System.Math.Ceiling(System.Math.Max(System.Math.Max(s1.X, s2.X), System.Math.Max(s3.X, s4.X)));
			int contentH = (int)System.Math.Ceiling(s1.Y + s2.Y + s3.Y + s4.Y + lineGap * 3);

			// Measure warnings if present
			float warnHeaderW = 0f, warnHeaderH = 0f;
			float warnBlockW = 0f, warnBlockH = 0f;
			if (overLimit.Count > 0)
			{
				var wh = _font.MeasureString(warnHeader) * TextScale;
				warnHeaderW = wh.X; warnHeaderH = wh.Y;
				foreach (var wl in warnLines)
				{
					var s = _font.MeasureString(wl) * TextScale;
					warnBlockW = System.Math.Max(warnBlockW, s.X);
					warnBlockH += s.Y;
				}
				// Include gaps: one gap before header, gaps between header->first line and between lines
				int warnGaps = 1 /*before header*/ + 1 /*after header*/ + System.Math.Max(0, warnLines.Count - 1);
				contentW = (int)System.Math.Ceiling(System.Math.Max(contentW, System.Math.Max(warnHeaderW, warnBlockW)));
				contentH += (int)System.Math.Ceiling(warnHeaderH + warnBlockH + warnGaps * lineGap);
			}
			int panelW = contentW + PadX * 2;
			int panelH = contentH + PadY * 2;
			int panelX = vw / 2 - panelW / 2;
			int panelY = vh / 2 - panelH / 2;

			// Background
			_spriteBatch.Draw(_pixel, new Rectangle(panelX, panelY, panelW, panelH), new Color(0, 0, 0, System.Math.Clamp(BgAlpha, 0, 255)));

			// Lines
			float y = panelY + PadY;
			var sizeColor = (total == Data.Loadouts.DeckRules.RequiredDeckSize) ? Color.White : Color.Red;
			_spriteBatch.DrawString(_font, sizeLine, new Vector2(panelX + PadX, y), sizeColor, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
			y += s1.Y + lineGap;
			_spriteBatch.DrawString(_font, whiteLine, new Vector2(panelX + PadX, y), Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
			y += s2.Y + lineGap;
			_spriteBatch.DrawString(_font, redLine, new Vector2(panelX + PadX, y), Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
			y += s3.Y + lineGap;
			_spriteBatch.DrawString(_font, blackLine, new Vector2(panelX + PadX, y), Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
			y += s4.Y;

			// Draw warnings if present
			if (overLimit.Count > 0)
			{
				y += lineGap; // gap before header
				_spriteBatch.DrawString(_font, warnHeader, new Vector2(panelX + PadX, y), Color.Red, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
				y += (_font.MeasureString(warnHeader).Y * TextScale) + lineGap;
				foreach (var wl in warnLines)
				{
					_spriteBatch.DrawString(_font, wl, new Vector2(panelX + PadX, y), Color.Red, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
					y += (_font.MeasureString(wl).Y * TextScale) + lineGap;
				}
			}
		}

		private CardData.CardColor ParseColorFromKey(string key)
		{
			if (string.IsNullOrEmpty(key)) return CardData.CardColor.White;
			int sep = key.IndexOf('|');
			if (sep < 0) return CardData.CardColor.White;
			string colorKey = key.Substring(sep + 1);
			switch (colorKey.Trim().ToLowerInvariant())
			{
				case "red": return CardData.CardColor.Red;
				case "black": return CardData.CardColor.Black;
				case "white":
				default: return CardData.CardColor.White;
			}
		}
	}
}


