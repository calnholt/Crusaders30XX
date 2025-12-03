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
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Card Library Panel")]
    public class CardLibraryPanelSystem : Core.System
    {
        private readonly World _world;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font = FontSingleton.TitleFont;
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

        public CardLibraryPanelSystem(EntityManager em, World world, GraphicsDevice gd, SpriteBatch sb) : base(em)
        {
            _world = world;
            _graphicsDevice = gd;
            _spriteBatch = sb;
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
            if (st == null) return;
            var mouse = Mouse.GetState();
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            int vw = Game1.VirtualWidth;
            int cardW = GetCvs().CardWidth;
            int cardH = GetCvs().CardHeight;

            int panelX = 0;
            int panelY = 0;
            int panelH = Game1.VirtualHeight;
            int colW = (int)(cardW * CardScale) + 20;
            int col = Math.Max(1, Columns);

            // Mouse wheel scroll when cursor is in left panel (convert mouse to virtual space)
            var dest = Game1.RenderDestination;
            float scaleX = (float)dest.Width / Game1.VirtualWidth;
            float scaleY = (float)dest.Height / Game1.VirtualHeight;
            if (scaleX <= 0.001f) scaleX = 1f;
            if (scaleY <= 0.001f) scaleY = 1f;
            var virtPoint = new Point(
                (int)System.Math.Round((mouse.Position.X - dest.X) / scaleX),
                (int)System.Math.Round((mouse.Position.Y - dest.Y) / scaleY)
            );

            // Mouse wheel scroll when cursor is in left panel (virtual coords)
            if (new Rectangle(panelX, panelY, PanelWidth, panelH).Contains(virtPoint))
            {
                int delta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
                st.LeftScroll = Math.Max(0, st.LeftScroll - delta / 2);
            }

            // Content layout and clicks moved to AvailableCardDisplaySystem
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
            int panelH = Game1.VirtualHeight;
            int colW = (int)(cardW * CardScale) + 20;
            int col = Math.Max(1, Columns);

            // Background
            var bgRect = new Rectangle(panelX, panelY, PanelWidth, panelH);
            _spriteBatch.Draw(_pixel, bgRect, new Color(0, 0, 0, 160));

            // Only draw panel background and header; card content is drawn elsewhere

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

        private CardVisualSettings GetCvs()
        {
            return EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>();
        }
    }
}


