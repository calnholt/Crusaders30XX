using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework.Input;
using System;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Cards;

namespace Crusaders30XX.ECS.Systems
{
    public class LoadoutCardDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly World _world;
        private readonly LoadoutDeckPanelSystem _deckPanel;
        private readonly Dictionary<string, int> _createdCardIds = new();
        private CursorStateEvent _cursorEvent;

        public LoadoutCardDisplaySystem(EntityManager em, World world, GraphicsDevice gd, SpriteBatch sb, LoadoutDeckPanelSystem deckPanel) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _world = world;
            _deckPanel = deckPanel;
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

            int vw = Game1.VirtualWidth;
            int cardW = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardWidth;
            int cardH = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardHeight;

            int panelX = vw - _deckPanel.PanelWidth;
            int panelY = 0;
            int colW = (int)(cardW * _deckPanel.CardScale) + 20;
            int col = Math.Max(1, _deckPanel.Columns);

            // Gamepad right-stick scroll when hovering over the right panel
            var gp = GamePad.GetState(PlayerIndex.One);
            if (gp.IsConnected && _cursorEvent != null)
            {
                int panelH = Game1.VirtualHeight;
                var panelRect = new Rectangle(panelX, panelY, _deckPanel.PanelWidth, panelH);
                var p = new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y));
                float y = gp.ThumbSticks.Right.Y; // up positive
                const float Deadzone = 0.2f;
                const float Speed = 2200f; // px/s
                if (panelRect.Contains(p) && MathF.Abs(y) > Deadzone)
                {
                    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                    st.RightScroll = Math.Max(0, st.RightScroll - (int)Math.Round(y * Speed * dt));
                }
            }

            var sorted = GetSortedWorkingEntries(st);

            int totalItems = sorted.Count;
            int rows = Math.Max(0, (totalItems + col - 1) / col);
            int cardScaledH = (int)(cardH * _deckPanel.CardScale);
            int gapsTotal = rows > 0 ? (rows - 1) * _deckPanel.RowGap : 0;
            int contentHeight = _deckPanel.HeaderHeight + _deckPanel.TopMargin + rows * cardScaledH + gapsTotal;
            int maxScroll = Math.Max(0, contentHeight - Game1.VirtualHeight);
            if (st.RightScroll > maxScroll) st.RightScroll = maxScroll;

            // Unified click detection via CursorStateEvent for both mouse and gamepad
            bool click = _cursorEvent != null && _cursorEvent.IsAPressedEdge;
            var clickPoint = click ? new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y)) : Point.Zero;

            int idx = 0;
            foreach (var view in sorted)
            {
                int r = idx / col;
                int c = idx % col;
                int x = panelX + c * colW + (colW / 2);
                int y = panelY + _deckPanel.HeaderHeight + _deckPanel.TopMargin + r * ((int)(cardH * _deckPanel.CardScale) + _deckPanel.RowGap) + (int)(cardH * _deckPanel.CardScale / 2) - st.RightScroll;
                var rect = new Rectangle(x - (int)(cardW * _deckPanel.CardScale / 2), y - (int)(cardH * _deckPanel.CardScale / 2), (int)(cardW * _deckPanel.CardScale), (int)(cardH * _deckPanel.CardScale));
                if (click && rect.Contains(clickPoint))
                {
                    EventManager.Publish(new RemoveCardFromLoadoutRequested { CardKey = view.key, Index = null });
                    break;
                }
                idx++;
            }
        }

        public void Draw()
        {
            if (StateSingleton.IsActive) return;
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Deck) return;

            int vw = Game1.VirtualWidth;
            int cardW = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardWidth;
            int cardH = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardHeight;
            int panelX = vw - _deckPanel.PanelWidth;
            int panelY = 0;
            int colW = (int)(cardW * _deckPanel.CardScale) + 20;
            int col = Math.Max(1, _deckPanel.Columns);

            var sorted = GetSortedWorkingEntries(st);
            int idx = 0;
            foreach (var view in sorted)
            {
                var card = CardFactory.Create(view.id);
                if (card == null || card.IsWeapon) { idx++; continue; }
                var tempCard = EnsureTempCard(card, view.color);
                int r = idx / col;
                int c = idx % col;
                int x = panelX + c * colW + (colW / 2);
                int y = panelY + _deckPanel.HeaderHeight + _deckPanel.TopMargin + r * ((int)(cardH * _deckPanel.CardScale) + _deckPanel.RowGap) + (int)(cardH * _deckPanel.CardScale / 2) - st.RightScroll;
                if (card != null)
                {
                    EventManager.Publish(new CardRenderScaledEvent { Card = tempCard, Position = new Vector2(x, y), Scale = _deckPanel.CardScale });
                }
                idx++;
            }
        }

        private Entity EnsureTempCard(CardBase card, CardData.CardColor color)
        {
            string name = card.Name ?? card.CardId;
            string keyName = $"Card_{name}_{color}_0";
            var existing = EntityManager.GetEntity(keyName);
            if (existing != null) return existing;
            var created = Factories.EntityFactory.CreateCardFromDefinition(EntityManager, card.CardId, color);
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
                var card = CardFactory.Create(id);
                if (card == null || card.IsWeapon) continue;
                if (!card.CanAddToLoadout) continue;
                if (card.IsWeapon) continue;
                if (!card.CanAddToLoadout) continue;
                string name = card.Name ?? card.CardId;
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


