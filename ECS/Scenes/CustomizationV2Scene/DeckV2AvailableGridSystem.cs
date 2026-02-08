using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("DeckV2 Grid")]
	public class DeckV2AvailableGridSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly World _world;
		private readonly SpriteFont _headingFont = FontSingleton.TitleFont;
		private readonly Dictionary<string, int> _createdCardIds = new();
		private CursorStateEvent _cursorEvent;
		private Texture2D _pixel;
		private int? _lastWheel;

		// Caches
		private List<(string key, CardBase card, CardData.CardColor color)> _cachedEntries;
		private bool _entriesDirty = true;
		private CardVisualSettings _cachedCardVisualSettings;

		[DebugEditable(DisplayName = "Columns", Step = 1, Min = 1, Max = 8)]
		public int Columns { get; set; } = 6;

		[DebugEditable(DisplayName = "Card Scale", Step = 0.01f, Min = 0.2f, Max = 1.0f)]
		public float CardScale { get; set; } = 0.86f;

		[DebugEditable(DisplayName = "Grid Top Margin", Step = 2, Min = 0, Max = 200)]
		public int GridTopMargin { get; set; } = 48;

		[DebugEditable(DisplayName = "Row Gap", Step = 1, Min = 0, Max = 40)]
		public int RowGap { get; set; } = 38;

		[DebugEditable(DisplayName = "Col Gap", Step = 1, Min = 0, Max = 40)]
		public int ColGap { get; set; } = 38;

		[DebugEditable(DisplayName = "Grid Pad X", Step = 2, Min = 0, Max = 60)]
		public int GridPadX { get; set; } = 20;

		[DebugEditable(DisplayName = "Right Panel Width", Step = 4, Min = 200, Max = 600)]
		public int RightPanelWidth { get; set; } = 380;

		[DebugEditable(DisplayName = "Title Text Scale", Step = 0.01f, Min = 0.05f, Max = 0.4f)]
		public float TitleTextScale { get; set; } = 0.18f;

		[DebugEditable(DisplayName = "Title Pad X", Step = 2, Min = 0, Max = 60)]
		public int TitlePadX { get; set; } = 20;

		[DebugEditable(DisplayName = "Title Pad Y", Step = 2, Min = 0, Max = 60)]
		public int TitlePadY { get; set; } = 20;

		[DebugEditable(DisplayName = "Scroll Speed", Step = 100, Min = 500, Max = 5000)]
		public float ScrollSpeed { get; set; } = 2200f;

		[DebugEditable(DisplayName = "Mouse Scroll Step", Step = 10, Min = 10, Max = 300)]
		public int MouseScrollStep { get; set; } = 60;

		[DebugEditable(DisplayName = "Scrollbar Width", Step = 1, Min = 2, Max = 12)]
		public int ScrollbarWidth { get; set; } = 6;

		public CustomizationV2HeaderSystem HeaderSystem { get; set; }
		public DeckV2StatsBarSystem StatsBarSystem { get; set; }

		public DeckV2AvailableGridSystem(EntityManager em, World world, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_world = world;
			EventManager.Subscribe<ShowTransition>(_ => { ClearCards(); InvalidateCaches(); });
			EventManager.Subscribe<SwitchCustomizationV2Tab>(_ => { ClearCards(); InvalidateCaches(); });
			EventManager.Subscribe<CursorStateEvent>(e => _cursorEvent = e);
			EventManager.Subscribe<DeckV2CardAdded>(_ => InvalidateCaches());
			EventManager.Subscribe<DeckV2CardRemoved>(_ => InvalidateCaches());
			EventManager.Subscribe<DeckV2DeckChanged>(_ => InvalidateCaches());
			EventManager.Subscribe<DeleteCachesEvent>(_ =>
			{
				_cachedEntries = null;
				_cachedCardVisualSettings = null;
				_entriesDirty = true;
			});
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Deck) return;
			if (StateSingleton.IsActive) return;

			var deck = EntityManager.GetEntitiesWithComponent<CustomizationV2DeckState>().FirstOrDefault()?.GetComponent<CustomizationV2DeckState>();
			if (deck == null) return;

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			// Compute max scroll for clamping
			var cvs0 = GetCardVisualSettings();
			int scaledH0 = (int)(cvs0.CardHeight * CardScale);
			int col0 = Math.Max(1, Columns);
			var entries0 = GetAvailableEntries();
			int headerH0 = (HeaderSystem?.HeaderHeight ?? 56) + (StatsBarSystem?.BarHeight ?? 50);
			int gridTopY0 = headerH0 + TitlePadY + (int)(_headingFont.MeasureString("A").Y * TitleTextScale) + GridTopMargin;
			int totalRows0 = Math.Max(0, (entries0.Count + col0 - 1) / col0);
			int contentHeight0 = gridTopY0 + totalRows0 * (scaledH0 + RowGap);
			int maxScroll = Math.Max(0, contentHeight0 - Game1.VirtualHeight);

			// Scroll via mouse wheel
			var mouse = Mouse.GetState();
			int panelW0 = Game1.VirtualWidth - RightPanelWidth;
			var gridRect = new Rectangle(0, headerH0, panelW0, Game1.VirtualHeight - headerH0);
			if (_cursorEvent != null && gridRect.Contains(new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y))))
			{
				int wheelValue = mouse.ScrollWheelValue;
				if (_lastWheel.HasValue)
				{
					int diff = wheelValue - _lastWheel.Value;
					if (diff != 0)
					{
						deck.AvailableScroll -= Math.Sign(diff) * MouseScrollStep;
						deck.AvailableScroll = Math.Clamp(deck.AvailableScroll, 0, maxScroll);
					}
				}
				_lastWheel = wheelValue;
			}
			else
			{
				_lastWheel = mouse.ScrollWheelValue;
			}

			// Gamepad scroll
			var gp = GamePad.GetState(PlayerIndex.One);
			if (gp.IsConnected && _cursorEvent != null)
			{
				float y = gp.ThumbSticks.Right.Y;
				const float Deadzone = 0.2f;
				if (MathF.Abs(y) > Deadzone)
				{
					deck.AvailableScroll = Math.Max(0, deck.AvailableScroll - (int)Math.Round(y * ScrollSpeed * dt));
					deck.AvailableScroll = Math.Clamp(deck.AvailableScroll, 0, maxScroll);
				}
			}

			// Click to add card
			if (_cursorEvent == null || !_cursorEvent.IsAPressedEdge) return;
			var click = new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y));

			var cvs = GetCardVisualSettings();
			int cardW = cvs.CardWidth;
			int cardH = cvs.CardHeight;
			int panelW = Game1.VirtualWidth - RightPanelWidth;

			var entries = GetAvailableEntries();
			int col = Math.Max(1, Columns);
			int scaledW = (int)(cardW * CardScale);
			int scaledH = (int)(cardH * CardScale);
			int colW = scaledW + ColGap;
			int headerH = (HeaderSystem?.HeaderHeight ?? 56) + (StatsBarSystem?.BarHeight ?? 50);

			int idx = 0;
			foreach (var entry in entries)
			{
				int r = idx / col;
				int c = idx % col;
				int x = GridPadX + c * colW + scaledW / 2;
				int y = headerH + TitlePadY + (int)(_headingFont.MeasureString("A").Y * TitleTextScale) + GridTopMargin + r * (scaledH + RowGap) + scaledH / 2 - deck.AvailableScroll;

				var rect = new Rectangle(x - scaledW / 2, y - scaledH / 2, scaledW, scaledH);
				if (rect.X < panelW && rect.Contains(click))
				{
					EventManager.Publish(new AddCardToLoadoutRequested { CardKey = entry.key });
					break;
				}
				idx++;
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

			var cvs = GetCardVisualSettings();
			int cardW = cvs.CardWidth;
			int cardH = cvs.CardHeight;
			int panelW = Game1.VirtualWidth - RightPanelWidth;
			int headerH = (HeaderSystem?.HeaderHeight ?? 56) + (StatsBarSystem?.BarHeight ?? 50);

			// Section title: "AVAILABLE CARDS"
			string title = "AVAILABLE CARDS";
			var titleSize = _headingFont.MeasureString(title) * TitleTextScale;
			float titleX = GridPadX + TitlePadX;
			float titleY = headerH + TitlePadY;
			_spriteBatch.DrawString(_headingFont, title, new Vector2(titleX, titleY), Color.White, 0f, Vector2.Zero, TitleTextScale, SpriteEffects.None, 0f);

			// Decorative line after title
			float lineX = titleX + titleSize.X + 8;
			float lineY = titleY + titleSize.Y / 2f;
			int lineW = panelW - (int)lineX - GridPadX;
			if (lineW > 0)
			{
				_spriteBatch.Draw(_pixel, new Rectangle((int)lineX, (int)lineY, lineW, 1), new Color(51, 51, 51));
			}

			// Card grid
			int col = Math.Max(1, Columns);
			int scaledW = (int)(cardW * CardScale);
			int scaledH = (int)(cardH * CardScale);
			int colW = scaledW + ColGap;
			int gridTopY = headerH + TitlePadY + (int)titleSize.Y + GridTopMargin;

			// Clip rect for scrolling — set scissor to mask cards outside the grid area
			var clipRect = new Rectangle(0, headerH, panelW, Game1.VirtualHeight - headerH);
			var previousScissor = _graphicsDevice.ScissorRectangle;
			_graphicsDevice.ScissorRectangle = clipRect;

			var entries = GetAvailableEntries();
			int idx = 0;
			foreach (var entry in entries)
			{
				int r = idx / col;
				int cIdx = idx % col;
				int x = GridPadX + cIdx * colW + scaledW / 2;
				int y = gridTopY + r * (scaledH + RowGap) + scaledH / 2 - deck.AvailableScroll;

				// Skip if off-screen
				if (y + scaledH / 2 < headerH || y - scaledH / 2 > Game1.VirtualHeight)
				{
					idx++;
					continue;
				}

				// Render card
				var tempCard = EnsureTempCard(entry.card, entry.color);
				if (tempCard != null)
				{
					EventManager.Publish(new CardRenderScaledEvent { Card = tempCard, Position = new Vector2(x, y), Scale = CardScale, ClipRect = clipRect });
				}

				idx++;
			}

			// Restore scissor rectangle
			_graphicsDevice.ScissorRectangle = previousScissor;

			// Clamp scroll (also clamped in Update, but guard against stale values after resize)
			int totalRows = Math.Max(0, (entries.Count + col - 1) / col);
			int contentHeight = gridTopY + totalRows * (scaledH + RowGap);
			int maxScrollDraw = Math.Max(0, contentHeight - Game1.VirtualHeight);
			if (deck.AvailableScroll > maxScrollDraw) deck.AvailableScroll = maxScrollDraw;

			// Scrollbar
			int visibleHeight = Game1.VirtualHeight - headerH;
			if (contentHeight > visibleHeight)
			{
				int maxScroll = contentHeight - visibleHeight;
				float thumbRatio = (float)visibleHeight / contentHeight;
				int thumbH = Math.Max(20, (int)(visibleHeight * thumbRatio));
				float scrollFraction = maxScroll > 0 ? (float)deck.AvailableScroll / maxScroll : 0;
				int thumbY = headerH + (int)((visibleHeight - thumbH) * scrollFraction);
				int scrollX = panelW - ScrollbarWidth;

				_spriteBatch.Draw(_pixel, new Rectangle(scrollX, headerH, ScrollbarWidth, visibleHeight), new Color(10, 10, 10));
				_spriteBatch.Draw(_pixel, new Rectangle(scrollX, thumbY, ScrollbarWidth, thumbH), new Color(51, 51, 51));
			}
		}

		private List<(string key, CardBase card, CardData.CardColor color)> GetAvailableEntries()
		{
			if (!_entriesDirty && _cachedEntries != null) return _cachedEntries;

			var deck = EntityManager.GetEntitiesWithComponent<CustomizationV2DeckState>().FirstOrDefault()?.GetComponent<CustomizationV2DeckState>();
			var inDeckSet = deck?.DeckCardKeys != null ? new HashSet<string>(deck.DeckCardKeys) : new HashSet<string>();

			var collection = SaveCache.GetCollectionSet();
			var defs = CardFactory.GetAllCards().Values
				.Where(d => !d.IsWeapon)
				.Where(d => d.CanAddToLoadout)
				.Where(d => collection.Contains(d.CardId))
				.OrderBy(d => ((d.Name ?? d.CardId) ?? string.Empty).ToLowerInvariant())
				.ToList();

			var result = new List<(string key, CardBase card, CardData.CardColor color)>();
			CardData.CardColor[] colorOrder = { CardData.CardColor.White, CardData.CardColor.Red, CardData.CardColor.Black };
			foreach (var def in defs)
			{
				foreach (var color in colorOrder)
				{
					string key = (def.CardId ?? def.Name).ToLowerInvariant() + "|" + color;
					if (inDeckSet.Contains(key)) continue;
					result.Add((key, def, color));
				}
			}
			_cachedEntries = result;
			_entriesDirty = false;
			return result;
		}

		private CardVisualSettings GetCardVisualSettings()
		{
			if (_cachedCardVisualSettings != null) return _cachedCardVisualSettings;
			_cachedCardVisualSettings = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>();
			return _cachedCardVisualSettings;
		}

		private void InvalidateCaches()
		{
			_entriesDirty = true;
		}

		private Entity EnsureTempCard(CardBase card, CardData.CardColor color)
		{
			string name = card.Name ?? card.CardId;
			string keyName = $"CV2_Card_{name}_{color}";
			if (_createdCardIds.TryGetValue(keyName, out int existingId))
			{
				var existing = EntityManager.GetEntity(existingId);
				if (existing != null) return existing;
			}
			var created = EntityFactory.CreateCardFromDefinition(EntityManager, card.CardId, color);
			if (created != null)
			{
				_createdCardIds[keyName] = created.Id;
			}
			return created;
		}

		private void ClearCards()
		{
			foreach (var id in _createdCardIds.Values)
			{
				EntityManager.DestroyEntity(id);
			}
			_createdCardIds.Clear();
		}
	}
}
