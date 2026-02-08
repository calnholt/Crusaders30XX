using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("DeckV2 List")]
	public class DeckV2DeckListSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _headingFont = FontSingleton.TitleFont;
		private readonly SpriteFont _contentFont = FontSingleton.ContentFont;
		private CursorStateEvent _cursorEvent;
		private Texture2D _pixel;
		private readonly List<Entity> _tooltipEntities = new();
		private const string TooltipEntityPrefix = "DeckV2Tooltip_";

		[DebugEditable(DisplayName = "Tooltip Scale", Step = 0.05f, Min = 0.1f, Max = 1.0f)]
		public float TooltipScale { get; set; } = 0.85f;

		[DebugEditable(DisplayName = "Right Panel Width", Step = 4, Min = 200, Max = 600)]
		public int RightPanelWidth { get; set; } = 380;

		[DebugEditable(DisplayName = "Panel BG R", Step = 1, Min = 0, Max = 255)]
		public int PanelBgR { get; set; } = 26;

		[DebugEditable(DisplayName = "Panel BG G", Step = 1, Min = 0, Max = 255)]
		public int PanelBgG { get; set; } = 26;

		[DebugEditable(DisplayName = "Panel BG B", Step = 1, Min = 0, Max = 255)]
		public int PanelBgB { get; set; } = 26;

		[DebugEditable(DisplayName = "Row Height", Step = 2, Min = 20, Max = 60)]
		public int RowHeight { get; set; } = 36;

		[DebugEditable(DisplayName = "Row Pad X", Step = 1, Min = 0, Max = 30)]
		public int RowPadX { get; set; } = 14;

		[DebugEditable(DisplayName = "Row BG R", Step = 1, Min = 0, Max = 255)]
		public int RowBgR { get; set; } = 42;

		[DebugEditable(DisplayName = "Row BG G", Step = 1, Min = 0, Max = 255)]
		public int RowBgG { get; set; } = 42;

		[DebugEditable(DisplayName = "Row BG B", Step = 1, Min = 0, Max = 255)]
		public int RowBgB { get; set; } = 42;

		[DebugEditable(DisplayName = "Row Border Left Width", Step = 1, Min = 0, Max = 6)]
		public int RowBorderLeftWidth { get; set; } = 3;

		[DebugEditable(DisplayName = "Group Gap", Step = 1, Min = 0, Max = 16)]
		public int GroupGap { get; set; } = 13;

		[DebugEditable(DisplayName = "Instance Gap", Step = 1, Min = 0, Max = 8)]
		public int InstanceGap { get; set; } = 2;

		[DebugEditable(DisplayName = "Name Scale", Step = 0.01f, Min = 0.05f, Max = 0.3f)]
		public float NameScale { get; set; } = 0.13f;

		[DebugEditable(DisplayName = "Type Scale", Step = 0.01f, Min = 0.05f, Max = 0.2f)]
		public float TypeScale { get; set; } = 0.08f;

		[DebugEditable(DisplayName = "Pip Radius", Step = 1, Min = 2, Max = 10)]
		public int PipRadius { get; set; } = 10;

		[DebugEditable(DisplayName = "Pip Gap", Step = 1, Min = 0, Max = 10)]
		public int PipGap { get; set; } = 8;

		[DebugEditable(DisplayName = "Pip To Name Gap", Step = 1, Min = 0, Max = 20)]
		public int PipToNameGap { get; set; } = 4;

		[DebugEditable(DisplayName = "Hover Slide X", Step = 1, Min = 0, Max = 20)]
		public float HoverSlideX { get; set; } = 4f;

		[DebugEditable(DisplayName = "Remove Anim Duration", Step = 0.05f, Min = 0.05f, Max = 1.0f)]
		public float RemoveAnimDuration { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Title Text Scale", Step = 0.01f, Min = 0.05f, Max = 0.4f)]
		public float TitleTextScale { get; set; } = 0.18f;

		[DebugEditable(DisplayName = "Content Pad", Step = 2, Min = 0, Max = 60)]
		public int ContentPad { get; set; } = 20;

		[DebugEditable(DisplayName = "Scrollbar Width", Step = 1, Min = 2, Max = 12)]
		public int ScrollbarWidth { get; set; } = 6;

		[DebugEditable(DisplayName = "Scroll Speed", Step = 100, Min = 500, Max = 5000)]
		public float ScrollSpeed { get; set; } = 2200f;

		[DebugEditable(DisplayName = "Mouse Scroll Step", Step = 10, Min = 10, Max = 300)]
		public int MouseScrollStep { get; set; } = 60;

		public CustomizationV2HeaderSystem HeaderSystem { get; set; }
		public DeckV2StatsBarSystem StatsBarSystem { get; set; }

		public DeckV2DeckListSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			EventManager.Subscribe<CursorStateEvent>(e => _cursorEvent = e);
		}

		public override void SetActive(bool active)
		{
			base.SetActive(active);
			if (!active) HideTooltipEntities();
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) { HideTooltipEntities(); return; }

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Deck) { HideTooltipEntities(); return; }
			if (StateSingleton.IsActive) { HideTooltipEntities(); return; }

			var deck = EntityManager.GetEntitiesWithComponent<CustomizationV2DeckState>().FirstOrDefault()?.GetComponent<CustomizationV2DeckState>();
			if (deck == null) { HideTooltipEntities(); return; }

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			// Update remove animations
			var expired = new List<string>();
			foreach (var kvp in deck.RemoveSlideTimers)
			{
				deck.RemoveSlideTimers[kvp.Key] = kvp.Value - dt;
				if (deck.RemoveSlideTimers[kvp.Key] <= 0) expired.Add(kvp.Key);
			}
			foreach (var key in expired) deck.RemoveSlideTimers.Remove(key);

			// Compute max scroll for clamping
			int headerH0 = (HeaderSystem?.HeaderHeight ?? 56) + (StatsBarSystem?.BarHeight ?? 50);
			var sorted0 = GetSortedGroupedDeck(deck);
			int contentH = headerH0 + ContentPad + (int)(_headingFont.MeasureString("A").Y * TitleTextScale) + ContentPad;
			string prevG = null;
			foreach (var item in sorted0)
			{
				string gk = item.name.ToLowerInvariant();
				if (prevG != null && prevG != gk) contentH += GroupGap;
				else if (prevG == gk) contentH += InstanceGap;
				prevG = gk;
				contentH += RowHeight;
			}
			int visibleH = Game1.VirtualHeight - headerH0;
			int maxScroll = Math.Max(0, contentH - Game1.VirtualHeight);

			// Scroll via CursorStateEvent
			int panelX0 = Game1.VirtualWidth - RightPanelWidth;
			var panelRect0 = new Rectangle(panelX0, 0, RightPanelWidth, Game1.VirtualHeight);
			if (_cursorEvent != null && panelRect0.Contains(new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y))))
			{
				if (_cursorEvent.ScrollDelta != 0f)
				{
					deck.DeckListScroll -= (int)Math.Round(_cursorEvent.ScrollDelta) * MouseScrollStep;
					deck.DeckListScroll = Math.Clamp(deck.DeckListScroll, 0, maxScroll);
				}
				if (_cursorEvent.ScrollStickY != 0f)
				{
					deck.DeckListScroll = Math.Max(0, deck.DeckListScroll - (int)Math.Round(_cursorEvent.ScrollStickY * ScrollSpeed * dt));
					deck.DeckListScroll = Math.Clamp(deck.DeckListScroll, 0, maxScroll);
				}
			}

			// Sync tooltip entities with current deck rows
			UpdateTooltipEntities(deck, sorted0, headerH0);

			// Click to remove card
			if (_cursorEvent == null || !_cursorEvent.IsAPressedEdge) return;
			var click = new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y));

			int headerH = (HeaderSystem?.HeaderHeight ?? 56) + (StatsBarSystem?.BarHeight ?? 50);
			int panelStartX = Game1.VirtualWidth - RightPanelWidth;
			if (click.X < panelStartX) return;

			var sorted = GetSortedGroupedDeck(deck);
			int drawY = headerH + ContentPad + (int)(_headingFont.MeasureString("A").Y * TitleTextScale) + ContentPad;

			string prevGroup = null;
			foreach (var item in sorted)
			{
				string groupKey = item.name.ToLowerInvariant();
				if (prevGroup != null && prevGroup != groupKey) drawY += GroupGap;
				else if (prevGroup == groupKey) drawY += InstanceGap;
				prevGroup = groupKey;

				int rowY = drawY - deck.DeckListScroll;
				var rowRect = new Rectangle(panelStartX + ContentPad, rowY, RightPanelWidth - ContentPad * 2, RowHeight);
				if (rowRect.Contains(click))
				{
					deck.RemoveSlideTimers[item.key] = RemoveAnimDuration;
					EventManager.Publish(new RemoveCardFromLoadoutRequested { CardKey = item.key, Index = null });
					break;
				}
				drawY += RowHeight;
			}
		}

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

			int headerH = (HeaderSystem?.HeaderHeight ?? 56) + (StatsBarSystem?.BarHeight ?? 50);
			int panelX = Game1.VirtualWidth - RightPanelWidth;
			int vh = Game1.VirtualHeight;

			// Panel background
			_spriteBatch.Draw(_pixel, new Rectangle(panelX, headerH, RightPanelWidth, vh - headerH), new Color(PanelBgR, PanelBgG, PanelBgB));
			// Left border
			_spriteBatch.Draw(_pixel, new Rectangle(panelX, headerH, 1, vh - headerH), new Color(51, 51, 51));

			// Measure title for layout
			string title = "CURRENT DECK";
			var titleSize = _headingFont.MeasureString(title) * TitleTextScale;
			int titleAreaHeight = ContentPad + (int)Math.Ceiling(titleSize.Y) + ContentPad;

			// Draw card rows
			var sorted = GetSortedGroupedDeck(deck);
			int drawY = headerH + titleAreaHeight;

			// Determine hover position
			bool isHovering = _cursorEvent != null && !_cursorEvent.IsAPressedEdge;
			var hoverPoint = isHovering ? new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y)) : Point.Zero;

			string prevGroup = null;
			var borderColor = new Color(160, 0, 0);
			var rowBg = new Color(RowBgR, RowBgG, RowBgB);

			foreach (var item in sorted)
			{
				string groupKey = item.name.ToLowerInvariant();
				if (prevGroup != null && prevGroup != groupKey) drawY += GroupGap;
				else if (prevGroup == groupKey) drawY += InstanceGap;
				prevGroup = groupKey;

				int rowY = drawY - deck.DeckListScroll;

				// Skip off-screen (use title area bottom as top boundary)
				if (rowY + RowHeight < headerH + titleAreaHeight || rowY > vh)
				{
					drawY += RowHeight;
					continue;
				}

				int rowX = panelX + ContentPad;
				int rowW = RightPanelWidth - ContentPad * 2;
				var rowRect = new Rectangle(rowX, rowY, rowW, RowHeight);

				// Hover effect
				float slideX = 0;
				if (isHovering && rowRect.Contains(hoverPoint))
				{
					slideX = -HoverSlideX;
				}

				// Remove animation
				if (deck.RemoveSlideTimers.TryGetValue(item.key, out float removeTimer) && removeTimer > 0)
				{
					float progress = 1f - (removeTimer / RemoveAnimDuration);
					slideX = -10f * progress;
				}

				// Row background
				_spriteBatch.Draw(_pixel, new Rectangle(rowX + (int)slideX, rowY, rowW, RowHeight), rowBg);

				// Left color border
				var colorBorderColor = GetCardColorValue(item.color);
				_spriteBatch.Draw(_pixel, new Rectangle(rowX + (int)slideX, rowY, RowBorderLeftWidth, RowHeight), colorBorderColor);

				// Cost pips
				float pipX = rowX + RowBorderLeftWidth + RowPadX + slideX;
				float pipCenterY = rowY + RowHeight / 2f;

				if (item.card != null && item.card.Cost != null)
				{
					foreach (var cost in item.card.Cost)
					{
						bool isAny = string.Equals(cost?.Trim(), "any", StringComparison.OrdinalIgnoreCase);
						if (isAny)
						{
							var anyTex = PrimitiveTextureFactory.GetAnyCostPipTexture(_graphicsDevice, PipRadius);
							_spriteBatch.Draw(anyTex, new Rectangle((int)pipX, (int)(pipCenterY - PipRadius), PipRadius * 2, PipRadius * 2), Color.White);
						}
						else
						{
							var pipColor = GetCostPipColor(cost);
							var circle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, PipRadius);
							_spriteBatch.Draw(circle, new Rectangle((int)pipX, (int)(pipCenterY - PipRadius), PipRadius * 2, PipRadius * 2), pipColor);
						}
						pipX += PipRadius * 2 + PipGap;
					}
				}

				// Card name
				float nameX = pipX + PipToNameGap;
				string displayName = item.name;
				var nameSize = _contentFont.MeasureString(displayName) * NameScale;
				float nameY = pipCenterY - nameSize.Y / 2f;
				_spriteBatch.DrawString(_contentFont, displayName, new Vector2(nameX, nameY), Color.White, 0f, Vector2.Zero, NameScale, SpriteEffects.None, 0f);

				// Card type label on the right
				string typeLabel = item.card?.Type.ToString().ToUpper() ?? "";
				var typeSize = _contentFont.MeasureString(typeLabel) * TypeScale;
				float typeX = rowX + rowW - RowPadX - typeSize.X + slideX;
				float typeY = pipCenterY - typeSize.Y / 2f;
				_spriteBatch.DrawString(_contentFont, typeLabel, new Vector2(typeX, typeY), new Color(102, 102, 102), 0f, Vector2.Zero, TypeScale, SpriteEffects.None, 0f);

				drawY += RowHeight;
			}

			// Title area background (drawn after cards to cover any that scrolled up)
			_spriteBatch.Draw(_pixel, new Rectangle(panelX, headerH, RightPanelWidth, titleAreaHeight), new Color(PanelBgR, PanelBgG, PanelBgB));
			// Left border over title area
			_spriteBatch.Draw(_pixel, new Rectangle(panelX, headerH, 1, titleAreaHeight), new Color(51, 51, 51));

			// Section title: "CURRENT DECK"
			float titleX = panelX + ContentPad;
			float titleY = headerH + ContentPad;
			_spriteBatch.DrawString(_headingFont, title, new Vector2(titleX, titleY), Color.White, 0f, Vector2.Zero, TitleTextScale, SpriteEffects.None, 0f);

			// Decorative line
			float lineX = titleX + titleSize.X + 8;
			float lineY2 = titleY + titleSize.Y / 2f;
			int lineW = panelX + RightPanelWidth - (int)lineX - ContentPad;
			if (lineW > 0)
			{
				_spriteBatch.Draw(_pixel, new Rectangle((int)lineX, (int)lineY2, lineW, 1), new Color(51, 51, 51));
			}

			// Scrollbar
			int scrollAreaTop = headerH + titleAreaHeight;
			int contentHeight = drawY - scrollAreaTop;
			int visibleHeight = vh - scrollAreaTop;
			if (contentHeight > visibleHeight)
			{
				int maxScroll = contentHeight - visibleHeight;
				if (deck.DeckListScroll > maxScroll) deck.DeckListScroll = maxScroll;
				float thumbRatio = (float)visibleHeight / contentHeight;
				int thumbH = Math.Max(20, (int)(visibleHeight * thumbRatio));
				float scrollFraction = maxScroll > 0 ? (float)deck.DeckListScroll / maxScroll : 0;
				int thumbY = scrollAreaTop + (int)((visibleHeight - thumbH) * scrollFraction);
				int scrollX = panelX + RightPanelWidth - ScrollbarWidth;

				_spriteBatch.Draw(_pixel, new Rectangle(scrollX, scrollAreaTop, ScrollbarWidth, visibleHeight), new Color(10, 10, 10));
				_spriteBatch.Draw(_pixel, new Rectangle(scrollX, thumbY, ScrollbarWidth, thumbH), new Color(51, 51, 51));
			}
		}

		private void UpdateTooltipEntities(
			CustomizationV2DeckState deck,
			List<(string key, string id, CardData.CardColor color, string name, CardBase card)> sorted,
			int headerH)
		{
			int titleAreaHeight = ContentPad + (int)(_headingFont.MeasureString("A").Y * TitleTextScale) + ContentPad;
			int panelX = Game1.VirtualWidth - RightPanelWidth;
			int rowX = panelX + ContentPad;
			int rowW = RightPanelWidth - ContentPad * 2;
			int vh = Game1.VirtualHeight;
			int visibleTop = headerH + titleAreaHeight;

			// Ensure correct number of tooltip entities
			while (_tooltipEntities.Count < sorted.Count)
			{
				int idx = _tooltipEntities.Count;
				var e = EntityManager.CreateEntity(TooltipEntityPrefix + idx);
				EntityManager.AddComponent(e, new Transform { ZOrder = 50000 });
				EntityManager.AddComponent(e, new UIElement
				{
					IsInteractable = true,
					TooltipType = TooltipType.Card,
					TooltipPosition = TooltipPosition.Left,
					IsPreventDefaultClick = true
				});
				EntityManager.AddComponent(e, new CardTooltip());
				EntityManager.AddComponent(e, new CardData());
				_tooltipEntities.Add(e);
			}
			while (_tooltipEntities.Count > sorted.Count)
			{
				var last = _tooltipEntities[_tooltipEntities.Count - 1];
				EntityManager.DestroyEntity(last.Id);
				_tooltipEntities.RemoveAt(_tooltipEntities.Count - 1);
			}

			// Update each tooltip entity's bounds and card data
			int drawY = headerH + titleAreaHeight;
			string prevGroup = null;
			for (int i = 0; i < sorted.Count; i++)
			{
				var item = sorted[i];
				string groupKey = item.name.ToLowerInvariant();
				if (prevGroup != null && prevGroup != groupKey) drawY += GroupGap;
				else if (prevGroup == groupKey) drawY += InstanceGap;
				prevGroup = groupKey;

				int rowY = drawY - deck.DeckListScroll;
				bool visible = rowY + RowHeight >= visibleTop && rowY < vh;

				var e = _tooltipEntities[i];
				var ui = e.GetComponent<UIElement>();
				ui.Bounds = new Rectangle(rowX, rowY, rowW, RowHeight);
				ui.IsInteractable = visible;

				var ct = e.GetComponent<CardTooltip>();
				ct.CardId = item.id;
				ct.TooltipScale = TooltipScale;

				var cd = e.GetComponent<CardData>();
				cd.Color = item.color;

				drawY += RowHeight;
			}
		}

		private void HideTooltipEntities()
		{
			if (_tooltipEntities.Count == 0) return;
			foreach (var e in _tooltipEntities)
				EntityManager.DestroyEntity(e.Id);
			_tooltipEntities.Clear();
		}

		private List<(string key, string id, CardData.CardColor color, string name, CardBase card)> GetSortedGroupedDeck(CustomizationV2DeckState deck)
		{
			var result = new List<(string key, string id, CardData.CardColor color, string name, CardBase card)>();
			if (deck?.DeckCardKeys == null) return result;

			foreach (var entry in deck.DeckCardKeys)
			{
				string id = entry;
				var color = CardData.CardColor.White;
				int sep = entry.IndexOf('|');
				if (sep >= 0)
				{
					id = entry.Substring(0, sep);
					color = ParseColor(entry.Substring(sep + 1));
				}
				var card = CardFactory.Create(id);
				if (card == null) continue;
				string name = card.Name ?? card.CardId;
				result.Add((entry, id, color, name, card));
			}

			result = result
				.OrderBy(t => t.name.ToLowerInvariant())
				.ThenBy(t => ColorOrder(t.color))
				.ToList();
			return result;
		}

		private static int ColorOrder(CardData.CardColor c) => c switch
		{
			CardData.CardColor.White => 0,
			CardData.CardColor.Red => 1,
			CardData.CardColor.Black => 2,
			_ => 3
		};

		private static CardData.CardColor ParseColor(string color) => (color?.Trim().ToLowerInvariant()) switch
		{
			"red" => CardData.CardColor.Red,
			"black" => CardData.CardColor.Black,
			_ => CardData.CardColor.White
		};

		private static Color GetCardColorValue(CardData.CardColor c) => c switch
		{
			CardData.CardColor.White => Color.White,
			CardData.CardColor.Red => Color.DarkRed,
			CardData.CardColor.Black => Color.Black,
			_ => new Color(128, 128, 128)
		};

		private static Color GetCostPipColor(string cost) => (cost?.ToLowerInvariant()) switch
		{
			"white" => Color.White,
			"red" => Color.DarkRed,
			"black" => Color.Black,
			_ => new Color(224, 224, 224)
		};
	}
}
