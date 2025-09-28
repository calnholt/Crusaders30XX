using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Cards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Factories;
using System;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Card Library Panel")]
    public class CardLibraryPanelSystem : Core.System
    {
        private readonly World _world;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private MouseState _prevMouse;
        private bool _isInitialized = false;
		private readonly Dictionary<string, int> _cardEntityIds = new Dictionary<string, int>();

        [DebugEditable(DisplayName = "Left Panel Width", Step = 4, Min = 100, Max = 2000)]
        public int PanelWidth { get; set; } = 640;
        [DebugEditable(DisplayName = "Row Gap", Step = 1, Min = 0, Max = 64)]
        public int RowGap { get; set; } = 18;
        [DebugEditable(DisplayName = "Top Margin", Step = 2, Min = 0, Max = 200)]
        public int TopMargin { get; set; } = 32;
        [DebugEditable(DisplayName = "Grid Columns", Step = 1, Min = 1, Max = 6)]
        public int Columns { get; set; } = 3;
        [DebugEditable(DisplayName = "Card Scale", Step = 0.05f, Min = 0.1f, Max = 1.0f)]
        public float CardScale { get; set; } = 0.75f;

        [DebugEditable(DisplayName = "Header Height", Step = 2, Min = 0, Max = 200)]
        public int HeaderHeight { get; set; } = 82;
        [DebugEditable(DisplayName = "Header Text Scale", Step = 0.01f, Min = 0.1f, Max = 2.0f)]
        public float HeaderTextScale { get; set; } = 0.35f;
        [DebugEditable(DisplayName = "Header Pad X", Step = 1, Min = 0, Max = 200)]
        public int HeaderPadX { get; set; } = 12;
        [DebugEditable(DisplayName = "Header Pad Y", Step = 1, Min = 0, Max = 200)]
        public int HeaderPadY { get; set; } = 6;

        public CardLibraryPanelSystem(EntityManager em, World world, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(em)
        {
            _world = world;
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _font = font;
            _prevMouse = Mouse.GetState();
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            EventManager.Subscribe<ShowTransition>(_ => OnLoadScene());
        }

		private void OnLoadScene()
		{
			if (_cardEntityIds.Count == 0) return;
			Console.WriteLine("[CardLibraryPanelSystem] Clearing cached library card entities");
			foreach (var entityId in _cardEntityIds.Values)
			{
				EntityManager.DestroyEntity(entityId);
			}
			_cardEntityIds.Clear();
			_isInitialized = false;
		}

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var scene = entity.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Deck) return;
            var mouse = Mouse.GetState();
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            int vw = _graphicsDevice.Viewport.Width;
            int cardW = GetCvs().CardWidth;
            int cardH = GetCvs().CardHeight;

            int panelX = 0;
            int panelY = 0;
            int panelH = _graphicsDevice.Viewport.Height;
            int colW = (int)(cardW * CardScale) + 20;
            int col = System.Math.Max(1, Columns);

            // Mouse wheel scroll when cursor is in left panel
            if (new Rectangle(panelX, panelY, PanelWidth, panelH).Contains(mouse.Position))
            {
                int delta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
                st.LeftScroll = System.Math.Max(0, st.LeftScroll - delta / 2);
            }

            // Build flat list of cards (all definitions * 3 colors). Only handle input here.
            var defs = CardDefinitionCache.GetAll().Values
                .Where(d => !d.isWeapon)
                .OrderBy(d => ((d.name ?? d.id) ?? string.Empty).ToLowerInvariant())
                .ToList();
            // We'll layout by color order within each definition sequence: White, Red, Black
            CardData.CardColor[] colorOrder = new[] { CardData.CardColor.White, CardData.CardColor.Red, CardData.CardColor.Black };
            // Build set of ids currently in working loadout to filter from library
            var inDeckSet = new System.Collections.Generic.HashSet<string>(st.WorkingCardIds);

            // Clamp scroll to content height
            int totalItems = 0;
            foreach (var def in defs)
            {
                foreach (var color in colorOrder)
                {
                    string key = (def.id ?? def.name).ToLowerInvariant() + "|" + color.ToString();
                    if (!inDeckSet.Contains(key)) totalItems++;
                }
            }
            int rows = System.Math.Max(0, (totalItems + col - 1) / col);
            int cardScaledH = (int)(cardH * CardScale);
            int gapsTotal = rows > 0 ? (rows - 1) * RowGap : 0;
            int contentHeight = HeaderHeight + TopMargin + rows * cardScaledH + gapsTotal;
            int maxScroll = System.Math.Max(0, contentHeight - panelH);
            if (st.LeftScroll > maxScroll) st.LeftScroll = maxScroll;

            int idx = 0;
            foreach (var def in defs)
            {
                foreach (var color in colorOrder)
                {
                    string entry = (def.id ?? def.name).ToLowerInvariant() + "|" + color.ToString();
                    if (inDeckSet.Contains(entry)) continue; // skip those already in deck

                    int r = idx / col;
                    int c = idx % col;
                    int x = panelX + c * colW + (colW / 2);
                    int y = panelY + HeaderHeight + TopMargin + r * ((int)(cardH * CardScale) + RowGap) + (int)(cardH * CardScale / 2) - st.LeftScroll;

                    // Click to add to working deck via event
                    var rect = new Rectangle(x - (int)(cardW * CardScale / 2), y - (int)(cardH * CardScale / 2), (int)(cardW * CardScale), (int)(cardH * CardScale));
                    if (click && rect.Contains(mouse.Position))
                    {
                        EventManager.Publish(new AddCardToLoadoutRequested { CardKey = entry });
                    }
                    idx++;
                }
            }
            _prevMouse = mouse;
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Deck) return;
            int cardW = GetCvs().CardWidth;
            int cardH = GetCvs().CardHeight;
            int panelX = 0;
            int panelY = 0;
            int panelH = _graphicsDevice.Viewport.Height;
            int colW = (int)(cardW * CardScale) + 20;
            int col = System.Math.Max(1, Columns);

            // Background
            var bgRect = new Rectangle(panelX, panelY, PanelWidth, panelH);
            _spriteBatch.Draw(_pixel, bgRect, new Color(0, 0, 0, 160));

            var defs = CardDefinitionCache.GetAll().Values
                .Where(d => !d.isWeapon)
                .OrderBy(d => ((d.name ?? d.id) ?? string.Empty).ToLowerInvariant())
                .ToList();
            CardData.CardColor[] colorOrder2 = new[] { CardData.CardColor.White, CardData.CardColor.Red, CardData.CardColor.Black };
            var inDeckSet = new HashSet<string>(st.WorkingCardIds);
            int idx = 0;
            foreach (var def in defs)
            {
                foreach (var color in colorOrder2)
                {
                    string entry = (def.id ?? def.name).ToLowerInvariant() + "|" + color.ToString();
                    if (inDeckSet.Contains(entry)) continue;

                    int r = idx / col;
                    int c = idx % col;
                    int x = panelX + c * colW + (colW / 2);
                    int y = panelY + HeaderHeight + TopMargin + r * ((int)(cardH * CardScale) + RowGap) + (int)(cardH * CardScale / 2) - st.LeftScroll;
                    var tempCard = EnsureTempCard(def, color);
                    if (tempCard != null)
                    {
                        EventManager.Publish(new CardRenderScaledEvent { Card = tempCard, Position = new Vector2(x, y), Scale = CardScale });
                    }
                    idx++;
                }
            }

            // Header drawn last so it overlays scrolled content
            var headerRect = new Rectangle(panelX, panelY, PanelWidth, HeaderHeight);
            _spriteBatch.Draw(_pixel, headerRect, new Color(30, 30, 30, 220));
            if (_font != null)
            {
                string header = "Available";
                var pos = new Vector2(panelX + HeaderPadX, panelY + HeaderPadY);
                _spriteBatch.DrawString(_font, header, pos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, HeaderTextScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, header, pos, Color.White, 0f, Vector2.Zero, HeaderTextScale, SpriteEffects.None, 0f);
            }
        }

		private Entity EnsureTempCard(CardDefinition def, CardData.CardColor color)
		{
            if (TransitionStateSingleton.IsActive) return null;
			string key = $"{def.id}|{color}";
			if (_cardEntityIds.TryGetValue(key, out var existingId))
			{
				var existing = EntityManager.GetEntity(existingId);
				if (existing != null) return existing;
				// Stale id, remove and recreate
				_cardEntityIds.Remove(key);
			}

			var created = EntityFactory.CreateCardFromDefinition(_world, def.id, color);
			if (created != null)
			{
				_cardEntityIds[key] = created.Id;
			}
			return created;
		}

        private CardVisualSettings GetCvs()
        {
            return EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>();
        }
    }
}


