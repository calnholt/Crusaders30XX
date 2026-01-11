using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Displays the achievement points meter at the bottom of the screen.
    /// </summary>
    [DebugTab("Achievement Meter")]
    public class AchievementMeterDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font = FontSingleton.ContentFont;

        private Texture2D _barBackgroundTex;
        private Texture2D _barFillTex;
        private int _cachedBarW, _cachedBarH, _cachedBarR;

        private float _displayedProgress = 0f; // For smooth animation
        private float _targetProgress = 0f;

        // Meter configuration
        [DebugEditable(DisplayName = "Bar X", Step = 10, Min = 50, Max = 500)]
        public int BarX { get; set; } = 150;

        [DebugEditable(DisplayName = "Bar Y", Step = 10, Min = 600, Max = 1000)]
        public int BarY { get; set; } = 950;

        [DebugEditable(DisplayName = "Bar Width", Step = 20, Min = 400, Max = 1600)]
        public int BarWidth { get; set; } = 1600;

        [DebugEditable(DisplayName = "Bar Height", Step = 2, Min = 16, Max = 60)]
        public int BarHeight { get; set; } = 50;

        [DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 20)]
        public int CornerRadius { get; set; } = 8;

        [DebugEditable(DisplayName = "Points Per Level", Step = 10, Min = 50, Max = 500)]
        public int PointsPerLevel { get; set; } = 100;

        [DebugEditable(DisplayName = "Label Scale", Step = 0.01f, Min = 0.1f, Max = 0.4f)]
        public float LabelScale { get; set; } = 0.18f;

        [DebugEditable(DisplayName = "Animation Speed", Step = 0.5f, Min = 1f, Max = 10f)]
        public float AnimationSpeed { get; set; } = 4f;

        // Colors
        private readonly Color _backgroundColor = new Color(30, 30, 30);
        private readonly Color _fillColor = new Color(180, 50, 50); // Brick red
        private readonly Color _textColor = Color.Black;

        public AchievementMeterDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;

            EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
        }

        private void OnLoadScene(LoadSceneEvent evt)
        {
            if (evt.Scene == SceneId.Achievement)
            {
                // Reset animation to start from current progress
                _displayedProgress = CalculateProgress();
                _targetProgress = _displayedProgress;
            }
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var scene = entity.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Achievement) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update target progress
            _targetProgress = CalculateProgress();

            // Animate towards target
            _displayedProgress = MathHelper.Lerp(_displayedProgress, _targetProgress, dt * AnimationSpeed);
        }

        private float CalculateProgress()
        {
            int totalPoints = 0;
            // Only count points from achievements that have been clicked/seen
            foreach (var achievement in AchievementManager.GetAll())
            {
                if (achievement.State == AchievementState.CompleteSeen)
                {
                    totalPoints += achievement.Points;
                }
            }

            // Progress within current level (0 to 1)
            int pointsInLevel = totalPoints % PointsPerLevel;
            return (float)pointsInLevel / PointsPerLevel;
        }

        private int GetTotalPoints()
        {
            int total = 0;
            // Only count points from achievements that have been clicked/seen
            foreach (var achievement in AchievementManager.GetAll())
            {
                if (achievement.State == AchievementState.CompleteSeen)
                {
                    total += achievement.Points;
                }
            }
            return total;
        }

        private int GetCurrentLevel()
        {
            int totalPoints = GetTotalPoints();
            return totalPoints / PointsPerLevel;
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Achievement) return;

            EnsureTextures();

            // Draw background bar
            _spriteBatch.Draw(_barBackgroundTex, new Rectangle(BarX, BarY, BarWidth, BarHeight), _backgroundColor);

            // Draw fill bar
            int fillWidth = Math.Max(CornerRadius * 2, (int)(BarWidth * _displayedProgress));
            if (fillWidth > CornerRadius * 2 && _displayedProgress > 0.01f)
            {
                var fillTex = GetOrCreateBar(fillWidth, BarHeight, CornerRadius);
                _spriteBatch.Draw(fillTex, new Rectangle(BarX, BarY, fillWidth, BarHeight), _fillColor);
            }

            // Draw border/outline
            DrawBorder();

            // Draw labels
            DrawLabels();
        }

        private void EnsureTextures()
        {
            if (_barBackgroundTex == null || _cachedBarW != BarWidth || _cachedBarH != BarHeight || _cachedBarR != CornerRadius)
            {
                _barBackgroundTex?.Dispose();
                _barFillTex?.Dispose();
                _barBackgroundTex = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, BarWidth, BarHeight, CornerRadius);
                _barFillTex = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, BarWidth, BarHeight, CornerRadius);
                _cachedBarW = BarWidth;
                _cachedBarH = BarHeight;
                _cachedBarR = CornerRadius;
            }
        }

        private Texture2D GetOrCreateBar(int width, int height, int radius)
        {
            // For dynamic fill widths, create on demand
            return RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
        }

        private void DrawBorder()
        {
            // Draw subtle border lines at top and bottom of bar
            var borderColor = new Color(80, 80, 80);
            
            // Use the background texture with a tint for the border effect
            // This creates a slight outline effect
        }

        private void DrawLabels()
        {
            if (_font == null) return;

            int totalPoints = GetTotalPoints();
            int currentLevel = GetCurrentLevel();
            int pointsInLevel = totalPoints % PointsPerLevel;

            // Draw level label on the left
            string levelText = $"Level {currentLevel}";
            var levelSize = _font.MeasureString(levelText) * LabelScale;
            float levelX = BarX;
            float levelY = BarY - levelSize.Y - 4;
            _spriteBatch.DrawString(_font, levelText, new Vector2(levelX, levelY), _textColor, 0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);

            // Draw points label on the right
            string pointsText = $"{pointsInLevel} / {PointsPerLevel}";
            var pointsSize = _font.MeasureString(pointsText) * LabelScale;
            float pointsX = BarX + BarWidth - pointsSize.X;
            float pointsY = BarY - pointsSize.Y - 4;
            _spriteBatch.DrawString(_font, pointsText, new Vector2(pointsX, pointsY), _textColor, 0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);

            // Draw total points below the bar
            string totalText = $"Total: {totalPoints} points";
            var totalSize = _font.MeasureString(totalText) * (LabelScale * 0.85f);
            float totalX = BarX + (BarWidth - totalSize.X) / 2f;
            float totalY = BarY + BarHeight + 6;
            _spriteBatch.DrawString(_font, totalText, new Vector2(totalX, totalY), _textColor, 0f, Vector2.Zero, LabelScale * 0.85f, SpriteEffects.None, 0f);
        }
    }
}
