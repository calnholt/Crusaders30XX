using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Displays the back button in the top-right corner of the Achievement scene.
    /// </summary>
    [DebugTab("Achievement Back Button")]
    public class AchievementBackButtonDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font = FontSingleton.ContentFont;

        private Entity _buttonEntity;
        private Texture2D _buttonTexture;
        private int _cachedW, _cachedH, _cachedR;

        [DebugEditable(DisplayName = "Button Width", Step = 5, Min = 60, Max = 300)]
        public int ButtonWidth { get; set; } = 140;

        [DebugEditable(DisplayName = "Button Height", Step = 5, Min = 30, Max = 100)]
        public int ButtonHeight { get; set; } = 50;

        [DebugEditable(DisplayName = "Margin Right", Step = 5, Min = 10, Max = 100)]
        public int MarginRight { get; set; } = 30;

        [DebugEditable(DisplayName = "Margin Top", Step = 5, Min = 10, Max = 100)]
        public int MarginTop { get; set; } = 30;

        [DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 25)]
        public int CornerRadius { get; set; } = 10;

        [DebugEditable(DisplayName = "Text Scale", Step = 0.01f, Min = 0.1f, Max = 0.5f)]
        public float TextScale { get; set; } = 0.2f;

        private readonly Color _normalColor = new Color(50, 50, 50);
        private readonly Color _hoverColor = new Color(80, 30, 30);
        private readonly Color _textColor = Color.White;

        public AchievementBackButtonDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;

            EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
        }

        private void OnLoadScene(LoadSceneEvent evt)
        {
            if (evt.Scene == SceneId.Achievement)
            {
                EnsureButtonEntity();
            }
        }

        private void EnsureButtonEntity()
        {
            if (_buttonEntity != null && EntityManager.GetEntity(_buttonEntity.Name) != null)
                return;

            _buttonEntity = EntityManager.CreateEntity("AchievementBackButton");
            
            var rect = GetButtonRect();
            EntityManager.AddComponent(_buttonEntity, new Transform
            {
                Position = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f),
                ZOrder = 200
            });
            EntityManager.AddComponent(_buttonEntity, new UIElement
            {
                Bounds = rect,
                IsInteractable = true,
                TooltipType = TooltipType.None
            });
            EntityManager.AddComponent(_buttonEntity, new AchievementBackButton());
            EntityManager.AddComponent(_buttonEntity, new OwnedByScene { Scene = SceneId.Achievement });
        }

        private Rectangle GetButtonRect()
        {
            int x = Game1.VirtualWidth - ButtonWidth - MarginRight;
            int y = MarginTop;
            return new Rectangle(x, y, ButtonWidth, ButtonHeight);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var scene = entity.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Achievement) return;

            EnsureButtonEntity();

            // Update button bounds in case of resize
            var ui = _buttonEntity?.GetComponent<UIElement>();
            if (ui != null)
            {
                ui.Bounds = GetButtonRect();
            }

            // Check for click
            if (ui != null && ui.IsClicked)
            {
                EventManager.Publish(new ShowTransition { Scene = SceneId.Location });
            }
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Achievement) return;

            if (_buttonEntity == null) return;

            var ui = _buttonEntity.GetComponent<UIElement>();
            if (ui == null) return;

            var rect = ui.Bounds;

            // Ensure texture
            if (_buttonTexture == null || _cachedW != rect.Width || _cachedH != rect.Height || _cachedR != CornerRadius)
            {
                _buttonTexture?.Dispose();
                _buttonTexture = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, rect.Width, rect.Height, CornerRadius);
                _cachedW = rect.Width;
                _cachedH = rect.Height;
                _cachedR = CornerRadius;
            }

            // Draw button background
            var bgColor = ui.IsHovered ? _hoverColor : _normalColor;
            _spriteBatch.Draw(_buttonTexture, rect, bgColor);

            // Draw text
            if (_font != null)
            {
                string text = "Back";
                var textSize = _font.MeasureString(text) * TextScale;
                var textPos = new Vector2(
                    rect.X + (rect.Width - textSize.X) / 2f,
                    rect.Y + (rect.Height - textSize.Y) / 2f
                );
                _spriteBatch.DrawString(_font, text, textPos, _textColor, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
            }
        }
    }
}
