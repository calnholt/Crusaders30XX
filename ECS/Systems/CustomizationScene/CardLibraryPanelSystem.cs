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
    [DebugTab("Card Library Panel")]
    public class CardLibraryPanelSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private MouseState _prevMouse;

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

        public CardLibraryPanelSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _font = font;
            _prevMouse = Mouse.GetState();
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
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
            var defs = CardDefinitionCache.GetAll().Values.Where(d => !d.isWeapon).ToList();

            // Clamp scroll to content height
            int totalItems = defs.Count * 3;
            int rows = System.Math.Max(0, (totalItems + col - 1) / col);
            int cardScaledH = (int)(cardH * CardScale);
            int gapsTotal = rows > 0 ? (rows - 1) * RowGap : 0;
            int contentHeight = HeaderHeight + TopMargin + rows * cardScaledH + gapsTotal;
            int maxScroll = System.Math.Max(0, contentHeight - panelH);
            if (st.LeftScroll > maxScroll) st.LeftScroll = maxScroll;

            int idx = 0;
            foreach (var def in defs)
            {
                foreach (var color in new[] { CardData.CardColor.White, CardData.CardColor.Red, CardData.CardColor.Black })
                {
                    int r = idx / col;
                    int c = idx % col;
                    int x = panelX + c * colW + (colW / 2);
                    int y = panelY + HeaderHeight + TopMargin + r * ((int)(cardH * CardScale) + RowGap) + (int)(cardH * CardScale / 2) - st.LeftScroll;

                    // Click to add to working deck
                    var rect = new Rectangle(x - (int)(cardW * CardScale / 2), y - (int)(cardH * CardScale / 2), (int)(cardW * CardScale), (int)(cardH * CardScale));
                    if (click && rect.Contains(mouse.Position))
                    {
                        string entry = (def.id ?? def.name).ToLowerInvariant() + "|" + color.ToString();
                        st.WorkingCardIds.Add(entry);
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
            if (st == null) return;
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

            var defs = CardDefinitionCache.GetAll().Values.Where(d => !d.isWeapon).ToList();
            int idx = 0;
            foreach (var def in defs)
            {
                foreach (var color in new[] { CardData.CardColor.White, CardData.CardColor.Red, CardData.CardColor.Black })
                {
                    int r = idx / col;
                    int c = idx % col;
                    int x = panelX + c * colW + (colW / 2);
                    int y = panelY + HeaderHeight + TopMargin + r * ((int)(cardH * CardScale) + RowGap) + (int)(cardH * CardScale / 2) - st.LeftScroll;
                    var tempCard = EnsureTempCard(def, color);
                    EventManager.Publish(new CardRenderScaledEvent { Card = tempCard, Position = new Vector2(x, y), Scale = CardScale });
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
            string key = $"Lib_{def.id}_{color}";
            var e = EntityManager.GetEntity(key);
            if (e != null) return e;
            // Use shared factory to ensure consistent card creation
            var worldShim = new Crusaders30XX.ECS.Core.World();
            // worldShim not used to manage systems; we only need entity structure
            // Reuse this EntityManager by assigning created entity here
            var created = Crusaders30XX.ECS.Factories.EntityFactory.CreateCardFromDefinition(worldShim, def.id, color);
            if (created == null) return null;
            // Migrate created components into this EntityManager under a new entity named key
            var final = EntityManager.CreateEntity(key);
            var cd = created.GetComponent<CardData>();
            var tr = created.GetComponent<Transform>();
            var sp = created.GetComponent<Sprite>();
            var ui = created.GetComponent<UIElement>();
            EntityManager.AddComponent(final, cd);
            EntityManager.AddComponent(final, tr);
            EntityManager.AddComponent(final, sp);
            EntityManager.AddComponent(final, ui);
            return final;
        }

        private CardVisualSettings GetCvs()
        {
            return EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>();
        }
    }
}


