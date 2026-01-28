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
    /// Displays achievement description panel on the right side when hovering over grid items.
    /// </summary>
    [DebugTab("Achievement Description")]
    public class AchievementDescriptionDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _titleFont = FontSingleton.TitleFont;
        private readonly SpriteFont _contentFont = FontSingleton.ContentFont;

        private string _currentHoveredId = string.Empty;
        private float _slideProgress = 0f;
        private Texture2D _panelTexture;
        private int _cachedPanelW, _cachedPanelH, _cachedPanelR;

        // Panel configuration
        [DebugEditable(DisplayName = "Panel X", Step = 10, Min = 500, Max = 1800)]
        public int PanelX { get; set; } = 1280;

        [DebugEditable(DisplayName = "Panel Y", Step = 10, Min = 100, Max = 800)]
        public int PanelY { get; set; } = 300;

        [DebugEditable(DisplayName = "Panel Width", Step = 10, Min = 200, Max = 800)]
        public int PanelWidth { get; set; } = 530;

        [DebugEditable(DisplayName = "Panel Height", Step = 10, Min = 200, Max = 600)]
        public int PanelHeight { get; set; } = 400;

        [DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 30)]
        public int CornerRadius { get; set; } = 12;

        [DebugEditable(DisplayName = "Padding", Step = 2, Min = 8, Max = 50)]
        public int Padding { get; set; } = 24;

        [DebugEditable(DisplayName = "Title Scale", Step = 0.01f, Min = 0.1f, Max = 1f)]
        public float TitleScale { get; set; } = 0.3f;

        [DebugEditable(DisplayName = "Description Scale", Step = 0.01f, Min = 0.1f, Max = 0.5f)]
        public float DescriptionScale { get; set; } = 0.18f;

        [DebugEditable(DisplayName = "Progress Scale", Step = 0.01f, Min = 0.1f, Max = 0.5f)]
        public float ProgressScale { get; set; } = 0.2f;

        [DebugEditable(DisplayName = "Slide Speed", Step = 1f, Min = 2f, Max = 20f)]
        public float SlideSpeed { get; set; } = 10f;

        [DebugEditable(DisplayName = "Slide Offset", Step = 10, Min = 20, Max = 200)]
        public int SlideOffset { get; set; } = 50;

        [DebugEditable(DisplayName = "Title Margin", Step = 1, Min = 0, Max = 100)]
        public int TitleMargin { get; set; } = 16;

        [DebugEditable(DisplayName = "Description Margin", Step = 1, Min = 0, Max = 100)]
        public int DescriptionMargin { get; set; } = 24;

        [DebugEditable(DisplayName = "Progress Margin", Step = 1, Min = 0, Max = 100)]
        public int ProgressMargin { get; set; } = 8;

        [DebugEditable(DisplayName = "Status Margin", Step = 1, Min = 0, Max = 100)]
        public int StatusMargin { get; set; } = 45;

        public AchievementDescriptionDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;

            EventManager.Subscribe<AchievementGridItemHovered>(OnGridItemHovered);
            EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
        }

        private void OnLoadScene(LoadSceneEvent evt)
        {
            if (evt.Scene == SceneId.Achievement)
            {
                _currentHoveredId = string.Empty;
                _slideProgress = 0f;
            }
        }

        private void OnGridItemHovered(AchievementGridItemHovered evt)
        {
            if (_currentHoveredId != evt.AchievementId)
            {
                _currentHoveredId = evt.AchievementId;
                // Reset slide when changing to new achievement
                if (!string.IsNullOrEmpty(_currentHoveredId))
                {
                    _slideProgress = 0f;
                }
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

            // Animate slide
            if (!string.IsNullOrEmpty(_currentHoveredId))
            {
                _slideProgress = MathHelper.Lerp(_slideProgress, 1f, dt * SlideSpeed);
            }
            else
            {
                _slideProgress = MathHelper.Lerp(_slideProgress, 0f, dt * SlideSpeed);
            }
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Achievement) return;

            // Only draw if we have something hovered or are sliding out
            if (_slideProgress < 0.01f && string.IsNullOrEmpty(_currentHoveredId))
                return;

            var achievement = !string.IsNullOrEmpty(_currentHoveredId)
                ? AchievementManager.GetAchievement(_currentHoveredId)
                : null;

            if (achievement == null && _slideProgress < 0.01f)
                return;

            // Calculate slide offset
            float slideOffset = (1f - _slideProgress) * SlideOffset;
            int panelX = PanelX + (int)slideOffset;
            float alpha = _slideProgress;

            // Draw panel background
            DrawPanel(panelX, PanelY, alpha);

            // Draw content if we have an achievement
            if (achievement != null)
            {
                DrawContent(achievement, panelX, PanelY, alpha);
            }
        }

        private void DrawPanel(int x, int y, float alpha)
        {
            // Ensure panel texture
            if (_panelTexture == null || _cachedPanelW != PanelWidth || _cachedPanelH != PanelHeight || _cachedPanelR != CornerRadius)
            {
                _panelTexture?.Dispose();
                _panelTexture = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, PanelWidth, PanelHeight, CornerRadius);
                _cachedPanelW = PanelWidth;
                _cachedPanelH = PanelHeight;
                _cachedPanelR = CornerRadius;
            }

            // Draw background
            var bgColor = new Color(0, 0, 0, (int)(220 * alpha));
            _spriteBatch.Draw(_panelTexture, new Rectangle(x, y, PanelWidth, PanelHeight), bgColor);

            // Draw border
            var borderColor = new Color(139, 0, 0, (int)(255 * alpha)); // Dark red
            var borderTex = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, PanelWidth - 4, PanelHeight - 4, CornerRadius - 2);
            // Just draw a subtle inner shadow effect by drawing slightly smaller rect
        }

        private void DrawContent(AchievementBase achievement, int panelX, int panelY, float alpha)
        {
            int contentX = panelX + Padding;
            int contentY = panelY + Padding;
            int contentWidth = PanelWidth - Padding * 2;

            var textColor = Color.White * alpha;
            var dimColor = new Color(180, 180, 180) * alpha;

            // Draw title (achievement name)
            if (_titleFont != null && !string.IsNullOrEmpty(achievement.Name))
            {
                var titleSize = _titleFont.MeasureString(achievement.Name) * TitleScale;
                _spriteBatch.DrawString(
                    _titleFont,
                    achievement.Name,
                    new Vector2(contentX, contentY),
                    textColor,
                    0f,
                    Vector2.Zero,
                    TitleScale,
                    SpriteEffects.None,
                    0f
                );
                contentY += (int)titleSize.Y + TitleMargin;
            }

            // Draw description
            if (_contentFont != null && !string.IsNullOrEmpty(achievement.Description))
            {
                string wrappedDesc = WrapText(achievement.Description, contentWidth, DescriptionScale);
                var descSize = _contentFont.MeasureString(wrappedDesc) * DescriptionScale;
                _spriteBatch.DrawString(
                    _contentFont,
                    wrappedDesc,
                    new Vector2(contentX, contentY),
                    dimColor,
                    0f,
                    Vector2.Zero,
                    DescriptionScale,
                    SpriteEffects.None,
                    0f
                );
                contentY += (int)descSize.Y + DescriptionMargin;
            }

            // Draw progress if applicable
            if (achievement.TargetValue > 0 && _contentFont != null)
            {
                int currentProgress = GetAchievementProgress(achievement);
                string progressText = $"Progress: {currentProgress} / {achievement.TargetValue}";
                
                // Draw progress bar background
                int barWidth = contentWidth;
                int barHeight = 20;
                var barBg = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, barWidth, barHeight, 4);
                _spriteBatch.Draw(barBg, new Rectangle(contentX, contentY, barWidth, barHeight), new Color(40, 40, 40) * alpha);

                // Draw progress fill
                float progressPct = Math.Min(1f, (float)currentProgress / achievement.TargetValue);
                int fillWidth = Math.Max(8, (int)(barWidth * progressPct));
                if (fillWidth > 8)
                {
                    var barFill = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, fillWidth, barHeight, 4);
                    var fillColor = achievement.IsCompleted ? Color.White : new Color(139, 0, 0);
                    _spriteBatch.Draw(barFill, new Rectangle(contentX, contentY, fillWidth, barHeight), fillColor * alpha);
                }

                contentY += barHeight + ProgressMargin;

                // Draw progress text
                _spriteBatch.DrawString(
                    _contentFont,
                    progressText,
                    new Vector2(contentX, contentY),
                    dimColor,
                    0f,
                    Vector2.Zero,
                    ProgressScale,
                    SpriteEffects.None,
                    0f
                );
                contentY += StatusMargin;
            }

            // Draw completion status
            if (_contentFont != null)
            {
                string statusText = achievement.State switch
                {
                    AchievementState.CompleteSeen => "Completed!",
                    AchievementState.CompleteUnseen => "Completed!",
                    AchievementState.Visible => "",
                    _ => "Hidden"
                };
                var statusColor = achievement.IsCompleted ? Color.Gold : dimColor;
                statusColor *= alpha;

                _spriteBatch.DrawString(
                    _contentFont,
                    statusText,
                    new Vector2(contentX, contentY),
                    statusColor,
                    0f,
                    Vector2.Zero,
                    ProgressScale,
                    SpriteEffects.None,
                    0f
                );

                // Draw points value (anchored to bottom-right)
                string pointsText = $"+{achievement.Points} Achievement Points";
                float pointsScale = ProgressScale * 0.9f;
                var pointsSize = _contentFont.MeasureString(pointsText) * pointsScale;
                Vector2 pointsPos = new Vector2(
                    panelX + PanelWidth - Padding - pointsSize.X,
                    panelY + PanelHeight - Padding - pointsSize.Y
                );

                _spriteBatch.DrawString(
                    _contentFont,
                    pointsText,
                    pointsPos,
                    new Color(255, 215, 0) * alpha, // Gold
                    0f,
                    Vector2.Zero,
                    pointsScale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        private int GetAchievementProgress(AchievementBase achievement)
        {
            // Use reflection to access protected Progress property
            var progressProperty = typeof(AchievementBase)
                .GetProperty("Progress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var progress = progressProperty?.GetValue(achievement) as AchievementProgress;
            return progress?.CurrentValue ?? 0;
        }

        private string WrapText(string text, int maxWidth, float scale)
        {
            if (_contentFont == null || string.IsNullOrEmpty(text))
                return text;

            var words = text.Split(' ');
            var lines = new List<string>();
            var currentLine = "";

            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                var testSize = _contentFont.MeasureString(testLine) * scale;

                if (testSize.X > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine);

            return string.Join("\n", lines);
        }
    }
}
