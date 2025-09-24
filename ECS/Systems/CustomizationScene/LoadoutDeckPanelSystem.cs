using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Cards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Loadout Deck Panel")]
    public class LoadoutDeckPanelSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private MouseState _prevMouse;

        [DebugEditable(DisplayName = "Right Panel Width", Step = 4, Min = 100, Max = 2000)]
        public int PanelWidth { get; set; } = 980;
        [DebugEditable(DisplayName = "Row Gap", Step = 1, Min = 0, Max = 64)]
        public int RowGap { get; set; } = 18;
        [DebugEditable(DisplayName = "Top Margin", Step = 2, Min = 0, Max = 200)]
        public int TopMargin { get; set; } = 24;
        [DebugEditable(DisplayName = "Grid Columns", Step = 1, Min = 1, Max = 6)]
        public int Columns { get; set; } = 3;
        [DebugEditable(DisplayName = "Card Scale", Step = 0.05f, Min = 0.1f, Max = 1.0f)]
        public float CardScale { get; set; } = 0.55f;

        public LoadoutDeckPanelSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _font = font;
            _prevMouse = Mouse.GetState();
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var scene = entity.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null) return;
            var mouse = Mouse.GetState();
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            int vw = _graphicsDevice.Viewport.Width;
            int cardW = GetCvs().CardWidth;
            int cardH = GetCvs().CardHeight;

            int panelX = vw - PanelWidth;
            int panelY = 0;
            int panelH = _graphicsDevice.Viewport.Height;
            int colW = (int)(cardW * CardScale) + 20;
            int col = System.Math.Max(1, Columns);

            if (new Rectangle(panelX, panelY, PanelWidth, panelH).Contains(mouse.Position))
            {
                int delta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
                st.RightScroll = System.Math.Max(0, st.RightScroll - delta / 2);
            }

            // Layout for click detection only (drawing handled in Draw)
            int idx = 0;
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
                if (!CardDefinitionCache.TryGet(id, out var def) || def == null) { idx++; continue; }
                if (def.isWeapon) { idx++; continue; }

                int r = idx / col;
                int c = idx % col;
                int x = panelX + c * colW + (colW / 2);
                int y = panelY + TopMargin + r * ((int)(cardH * CardScale) + RowGap) + (int)(cardH * CardScale / 2) - st.RightScroll;

                var rect = new Rectangle(x - (int)(cardW * CardScale / 2), y - (int)(cardH * CardScale / 2), (int)(cardW * CardScale), (int)(cardH * CardScale));
                if (click && rect.Contains(mouse.Position))
                {
                    st.WorkingCardIds.RemoveAt(idx);
                    break;
                }
                idx++;
            }
            _prevMouse = Mouse.GetState();
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null) return;
            int vw = _graphicsDevice.Viewport.Width;
            int cardW = GetCvs().CardWidth;
            int cardH = GetCvs().CardHeight;
            int panelX = vw - PanelWidth;
            int panelY = 0;
            int colW = (int)(cardW * CardScale) + 20;
            int col = System.Math.Max(1, Columns);

            int idx = 0;
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
                if (!CardDefinitionCache.TryGet(id, out var def) || def == null) { idx++; continue; }
                if (def.isWeapon) { idx++; continue; }
                var card = EnsureTempCard(def, color);

                int r = idx / col;
                int c = idx % col;
                int x = panelX + c * colW + (colW / 2);
                int y = panelY + TopMargin + r * ((int)(cardH * CardScale) + RowGap) + (int)(cardH * CardScale / 2) - st.RightScroll;
                EventManager.Publish(new CardRenderScaledEvent { Card = card, Position = new Vector2(x, y), Scale = CardScale });
                idx++;
            }
        }

        private Entity EnsureTempCard(CardDefinition def, CardData.CardColor color)
        {
            string key = $"Deck_{def.id}_{color}";
            var e = EntityManager.GetEntity(key);
            if (e != null) return e;
            e = EntityManager.CreateEntity(key);
            string name = def.name ?? def.id;
            int block = def.block + (color == CardData.CardColor.Black ? 1 : 0);
            var cardData = new CardData
            {
                Name = name,
                Description = def.text,
                Cost = 0,
                Type = CardData.CardType.Attack,
                Rarity = CardData.CardRarity.Common,
                ImagePath = string.Empty,
                Color = color,
                BlockValue = block
            };
            cardData.CostArray = new System.Collections.Generic.List<CardData.CostType>();
            if (def.cost != null)
            {
                foreach (var c in def.cost)
                {
                    var ct = ParseCostType(c);
                    if (ct != CardData.CostType.NoCost) cardData.CostArray.Add(ct);
                }
            }
            var firstSpecific = cardData.CostArray.FirstOrDefault(x => x == CardData.CostType.Red || x == CardData.CostType.White || x == CardData.CostType.Black);
            if (firstSpecific != CardData.CostType.NoCost) cardData.CardCostType = firstSpecific;
            else if (cardData.CostArray.Any(x => x == CardData.CostType.Any)) cardData.CardCostType = CardData.CostType.Any;
            else cardData.CardCostType = CardData.CostType.NoCost;

            EntityManager.AddComponent(e, cardData);
            EntityManager.AddComponent(e, new Transform { Position = Vector2.Zero, Scale = Vector2.One });
            EntityManager.AddComponent(e, new Sprite { TexturePath = string.Empty, IsVisible = true });
            EntityManager.AddComponent(e, new UIElement { Bounds = new Rectangle(0, 0, GetCvs().CardWidth, GetCvs().CardHeight), IsInteractable = true });
            return e;
        }

        private CardVisualSettings GetCvs()
        {
            return EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>();
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

        private CardData.CostType ParseCostType(string cost)
        {
            if (string.IsNullOrEmpty(cost)) return CardData.CostType.NoCost;
            switch (cost.Trim().ToLowerInvariant())
            {
                case "red": return CardData.CostType.Red;
                case "white": return CardData.CostType.White;
                case "black": return CardData.CostType.Black;
                case "any": return CardData.CostType.Any;
                default: return CardData.CostType.NoCost;
            }
        }
    }
}


