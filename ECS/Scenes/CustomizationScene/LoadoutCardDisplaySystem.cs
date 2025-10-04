using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Cards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework.Input;
using System;

namespace Crusaders30XX.ECS.Systems
{
    public class LoadoutCardDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly World _world;
        private readonly LoadoutDeckPanelSystem _deckPanel;
        private readonly Dictionary<string, int> _createdCardIds = new();
        private MouseState _prevMouse;
        public LoadoutCardDisplaySystem(EntityManager em, World world, GraphicsDevice gd, SpriteBatch sb, LoadoutDeckPanelSystem deckPanel) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _world = world;
            _deckPanel = deckPanel;
            _prevMouse = Mouse.GetState();
            EventManager.Subscribe<ShowTransition>(_ => ClearCards());
            EventManager.Subscribe<SetCustomizationTab>(_ => ClearCards());
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            if (TransitionStateSingleton.IsActive) return;
            var scene = entity.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Deck) return;

            var mouse = Mouse.GetState();
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

            int vw = _graphicsDevice.Viewport.Width;
            int cardW = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardWidth;
            int cardH = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardHeight;

            int panelX = vw - _deckPanel.PanelWidth;
            int panelY = 0;
            int colW = (int)(cardW * _deckPanel.CardScale) + 20;
            int col = Math.Max(1, _deckPanel.Columns);

            var sorted = GetSortedWorkingEntries(st);

            int totalItems = sorted.Count;
            int rows = Math.Max(0, (totalItems + col - 1) / col);
            int cardScaledH = (int)(cardH * _deckPanel.CardScale);
            int gapsTotal = rows > 0 ? (rows - 1) * _deckPanel.RowGap : 0;
            int contentHeight = _deckPanel.HeaderHeight + _deckPanel.TopMargin + rows * cardScaledH + gapsTotal;
            int maxScroll = Math.Max(0, contentHeight - _graphicsDevice.Viewport.Height);
            if (st.RightScroll > maxScroll) st.RightScroll = maxScroll;

            int idx = 0;
            foreach (var view in sorted)
            {
                int r = idx / col;
                int c = idx % col;
                int x = panelX + c * colW + (colW / 2);
                int y = panelY + _deckPanel.HeaderHeight + _deckPanel.TopMargin + r * ((int)(cardH * _deckPanel.CardScale) + _deckPanel.RowGap) + (int)(cardH * _deckPanel.CardScale / 2) - st.RightScroll;
                var rect = new Rectangle(x - (int)(cardW * _deckPanel.CardScale / 2), y - (int)(cardH * _deckPanel.CardScale / 2), (int)(cardW * _deckPanel.CardScale), (int)(cardH * _deckPanel.CardScale));
                if (click && rect.Contains(mouse.Position))
                {
                    EventManager.Publish(new RemoveCardFromLoadoutRequested { CardKey = view.key, Index = null });
                    break;
                }
                idx++;
            }
            _prevMouse = mouse;
        }

        public void Draw()
        {
            if (TransitionStateSingleton.IsActive) return;
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Deck) return;

            int vw = _graphicsDevice.Viewport.Width;
            int cardW = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardWidth;
            int cardH = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardHeight;
            int panelX = vw - _deckPanel.PanelWidth;
            int panelY = 0;
            int colW = (int)(cardW * _deckPanel.CardScale) + 20;
            int col = System.Math.Max(1, _deckPanel.Columns);

            var sorted = GetSortedWorkingEntries(st);
            int idx = 0;
            foreach (var view in sorted)
            {
                if (!CardDefinitionCache.TryGet(view.id, out var def) || def == null || def.isWeapon) { idx++; continue; }
                var card = EnsureTempCard(def, view.color);
                int r = idx / col;
                int c = idx % col;
                int x = panelX + c * colW + (colW / 2);
                int y = panelY + _deckPanel.HeaderHeight + _deckPanel.TopMargin + r * ((int)(cardH * _deckPanel.CardScale) + _deckPanel.RowGap) + (int)(cardH * _deckPanel.CardScale / 2) - st.RightScroll;
                if (card != null)
                {
                    ECS.Core.EventManager.Publish(new CardRenderScaledEvent { Card = card, Position = new Vector2(x, y), Scale = _deckPanel.CardScale });
                }
                idx++;
            }
        }

        private Entity EnsureTempCard(CardDefinition def, CardData.CardColor color)
        {
            string name = def.name ?? def.id;
            string keyName = $"Card_{name}_{color}";
            var existing = EntityManager.GetEntity(keyName);
            if (existing != null) return existing;
            var created = Crusaders30XX.ECS.Factories.EntityFactory.CreateCardFromDefinition(EntityManager, def.id, color);
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

        private List<(string key, string id, CardData.CardColor color, string name)> GetSortedWorkingEntries(CustomizationState st)
        {
            var result = new List<(string key, string id, CardData.CardColor color, string name)>();
            foreach (var entry in st.WorkingCardIds)
            {
                string id = entry;
                var color = CardData.CardColor.White;
                int sep = entry.IndexOf('|');
                if (sep >= 0)
                {
                    id = entry.Substring(0, sep);
                    var colorKey = entry.Substring(sep + 1);
                    color = ParseColor(colorKey);
                }
                if (!CardDefinitionCache.TryGet(id, out var def) || def == null) continue;
                if (def.isWeapon) continue;
                string name = (def.name ?? def.id) ?? string.Empty;
                result.Add((entry, id, color, name));
            }
            int ColorOrder(CardData.CardColor c)
            {
                switch (c)
                {
                    case CardData.CardColor.White: return 0;
                    case CardData.CardColor.Red: return 1;
                    case CardData.CardColor.Black: return 2;
                    default: return 3;
                }
            }
            result = result
                .OrderBy(t => t.name.ToLowerInvariant())
                .ThenBy(t => ColorOrder(t.color))
                .ToList();
            return result;
        }

        private CardData.CardColor ParseColor(string color)
        {
            if (string.IsNullOrEmpty(color)) return CardData.CardColor.White;
            switch (color.Trim().ToLowerInvariant())
            {
                case "red": return CardData.CardColor.Red;
                case "black": return CardData.CardColor.Black;
                case "white":
                default: return CardData.CardColor.White;
            }
        }
    }
}


