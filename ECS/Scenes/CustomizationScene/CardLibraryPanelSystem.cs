using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        private CursorStateEvent _cursorEvent;
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
        public float CardScale { get; set; } = 0.70f;

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
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            EventManager.Subscribe<CursorStateEvent>(e => _cursorEvent = e);
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
            if (st == null || _cursorEvent == null) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            int panelH = Game1.VirtualHeight;
            var panelRect = new Rectangle(0, 0, PanelWidth, panelH);
            var cursorPt = new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y));

            if (panelRect.Contains(cursorPt))
            {
                if (_cursorEvent.ScrollDelta != 0f)
                {
                    st.LeftScroll = Math.Max(0, st.LeftScroll - (int)Math.Round(_cursorEvent.ScrollDelta * 60));
                }
                if (_cursorEvent.ScrollStickY != 0f)
                {
                    st.LeftScroll = Math.Max(0, st.LeftScroll - (int)Math.Round(_cursorEvent.ScrollStickY * 1200f * dt));
                }
            }
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


