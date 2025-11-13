using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Cards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Factories;
using System;
using Crusaders30XX.ECS.Data.Save;

namespace Crusaders30XX.ECS.Systems
{
    public class AvailableCardDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly CardLibraryPanelSystem _libraryPanel;
        private MouseState _prevMouse;
        private readonly World _world;
        private readonly Dictionary<string, int> _createdCardIds = new();
        private CursorStateEvent _cursorEvent;

        public AvailableCardDisplaySystem(EntityManager em, World world, GraphicsDevice gd, SpriteBatch sb, CardLibraryPanelSystem libraryPanel) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _world = world;
            _libraryPanel = libraryPanel;
            _prevMouse = Mouse.GetState();
            EventManager.Subscribe<ShowTransition>(_ => ClearCards());
            EventManager.Subscribe<SetCustomizationTab>(_ => ClearCards());
            EventManager.Subscribe<CursorStateEvent>(e => _cursorEvent = e);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
          if (StateSingleton.IsActive) return;
            var scene = entity.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Deck) return;

            var mouse = Mouse.GetState();
            var prevMouse = mouse; // consume elsewhere if needed

            int cardW = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardWidth;
            int cardH = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardHeight;

            int panelX = 0;
            int panelY = 0;
            int panelH = _graphicsDevice.Viewport.Height;
            int colW = (int)(cardW * _libraryPanel.CardScale) + 20;
            int col = Math.Max(1, _libraryPanel.Columns);

            // Gamepad right-stick scroll when hovering over the left panel
            var gp = GamePad.GetState(PlayerIndex.One);
            if (gp.IsConnected && _cursorEvent != null)
            {
                var panelRect = new Rectangle(panelX, panelY, _libraryPanel.PanelWidth, panelH);
                var p = new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y));
                float y = gp.ThumbSticks.Right.Y; // up positive
                const float Deadzone = 0.2f;
                const float Speed = 2200f; // px/s
                if (panelRect.Contains(p) && MathF.Abs(y) > Deadzone)
                {
                    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                    st.LeftScroll = Math.Max(0, st.LeftScroll - (int)Math.Round(y * Speed * dt));
                }
            }

            var collection = SaveCache.GetCollectionSet();
            var defs = CardDefinitionCache.GetAll().Values
                .Where(d => !d.isWeapon)
                .Where(d => d.canAddToLoadout)
                .Where(d => collection.Contains(d.id))
                .OrderBy(d => ((d.name ?? d.id) ?? string.Empty).ToLowerInvariant())
                .ToList();
            CardData.CardColor[] colorOrder = new[] { CardData.CardColor.White, CardData.CardColor.Red, CardData.CardColor.Black };
            var inDeckSet = new HashSet<string>(st.WorkingCardIds);

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
            int rows = Math.Max(0, (totalItems + col - 1) / col);
            int cardScaledH = (int)(cardH * _libraryPanel.CardScale);
            int gapsTotal = rows > 0 ? (rows - 1) * _libraryPanel.RowGap : 0;
            int contentHeight = _libraryPanel.HeaderHeight + _libraryPanel.TopMargin + rows * cardScaledH + gapsTotal;
            int maxScroll = Math.Max(0, contentHeight - panelH);
            if (st.LeftScroll > maxScroll) st.LeftScroll = maxScroll;

            // Handle clicks to add (mouse or controller A edge)
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            bool gpEdge = _cursorEvent != null && _cursorEvent.IsAPressedEdge;
            Point gpPoint = gpEdge ? new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y)) : Point.Zero;

            int idx = 0;
            foreach (var def in defs)
            {
                foreach (var color in colorOrder)
                {
                    string entry = (def.id ?? def.name).ToLowerInvariant() + "|" + color.ToString();
                    if (inDeckSet.Contains(entry)) continue;
                    int r = idx / col;
                    int c = idx % col;
                    int x = panelX + c * colW + (colW / 2);
                    int y = panelY + _libraryPanel.HeaderHeight + _libraryPanel.TopMargin + r * ((int)(cardH * _libraryPanel.CardScale) + _libraryPanel.RowGap) + (int)(cardH * _libraryPanel.CardScale / 2) - st.LeftScroll;
                    var rect = new Rectangle(x - (int)(cardW * _libraryPanel.CardScale / 2), y - (int)(cardH * _libraryPanel.CardScale / 2), (int)(cardW * _libraryPanel.CardScale), (int)(cardH * _libraryPanel.CardScale));
                    if ((click && rect.Contains(mouse.Position)) || (gpEdge && rect.Contains(gpPoint)))
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
            if (StateSingleton.IsActive) return;
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Deck) return;

            int cardW = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardWidth;
            int cardH = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardHeight;
            int panelX = 0;
            int panelY = 0;
            int colW = (int)(cardW * _libraryPanel.CardScale) + 20;
            int col = Math.Max(1, _libraryPanel.Columns);

            var collection = SaveCache.GetCollectionSet();
            var defs = CardDefinitionCache.GetAll().Values
                .Where(d => !d.isWeapon)
                .Where(d => d.canAddToLoadout)
                .Where(d => collection.Contains(d.id))
                .OrderBy(d => ((d.name ?? d.id) ?? string.Empty).ToLowerInvariant())
                .ToList();
            CardData.CardColor[] colorOrder = new[] { CardData.CardColor.White, CardData.CardColor.Red, CardData.CardColor.Black };
            var inDeckSet = new HashSet<string>(st.WorkingCardIds);
            int idx = 0;
            foreach (var def in defs)
            {
                foreach (var color in colorOrder)
                {
                    string entry = (def.id ?? def.name).ToLowerInvariant() + "|" + color.ToString();
                    if (inDeckSet.Contains(entry)) continue;
                    int r = idx / col;
                    int c = idx % col;
                    int x = panelX + c * colW + (colW / 2);
                    int y = panelY + _libraryPanel.HeaderHeight + _libraryPanel.TopMargin + r * ((int)(cardH * _libraryPanel.CardScale) + _libraryPanel.RowGap) + (int)(cardH * _libraryPanel.CardScale / 2) - st.LeftScroll;
                    // Use CardRenderScaledEvent from CardDisplaySystem
                    var tempCard = EnsureTempCard(def, color);
                    if (tempCard != null)
                    {
                        EventManager.Publish(new CardRenderScaledEvent { Card = tempCard, Position = new Vector2(x, y), Scale = _libraryPanel.CardScale });
                    }
                    idx++;
                }
            }
        }

        private Entity EnsureTempCard(CardDefinition def, CardData.CardColor color)
        {
            string name = def.name ?? def.id;
            string keyName = $"Card_{name}_{color}_0";
            var existing = EntityManager.GetEntity(keyName);
            if (existing != null) return existing;
            var created = EntityFactory.CreateCardFromDefinition(EntityManager, def.id, color);
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


