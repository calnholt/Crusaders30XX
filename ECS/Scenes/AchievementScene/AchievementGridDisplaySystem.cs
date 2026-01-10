using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Displays the achievement grid (15x10) with fog-of-war discovery.
    /// </summary>
    [DebugTab("Achievement Grid")]
    public class AchievementGridDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly Dictionary<string, Texture2D> _roundedRectCache = new();
        private readonly Dictionary<string, Entity> _gridEntities = new();
        private bool _gridCreated = false;

        // Animation state
        private enum AnimationPhase { Idle, PlayingCompletions, PlayingReveals, Complete }
        private AnimationPhase _phase = AnimationPhase.Idle;
        private float _animationTime = 0f;
        private int _animationIndex = 0;
        private List<AchievementBase> _completionQueue = new();
        private List<AchievementBase> _revealQueue = new();

        // Grid configuration
        public const int GRID_COLUMNS = 15;
        public const int GRID_ROWS = 10;

        [DebugEditable(DisplayName = "Cell Size", Step = 2, Min = 20, Max = 100)]
        public int CellSize { get; set; } = 54;

        [DebugEditable(DisplayName = "Cell Gap", Step = 1, Min = 0, Max = 20)]
        public int CellGap { get; set; } = 16;

        [DebugEditable(DisplayName = "Grid Offset X", Step = 10, Min = 0, Max = 500)]
        public int GridOffsetX { get; set; } = 170;

        [DebugEditable(DisplayName = "Grid Offset Y", Step = 10, Min = 0, Max = 500)]
        public int GridOffsetY { get; set; } = 150;

        [DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 20)]
        public int CornerRadius { get; set; } = 6;

        [DebugEditable(DisplayName = "Hover Scale", Step = 0.05f, Min = 1f, Max = 2f)]
        public float HoverScale { get; set; } = 1.15f;

        [DebugEditable(DisplayName = "Scale Lerp Speed", Step = 0.5f, Min = 1f, Max = 30f)]
        public float ScaleLerpSpeed { get; set; } = 12f;

        [DebugEditable(DisplayName = "Completion Anim Duration", Step = 0.1f, Min = 0.1f, Max = 2f)]
        public float CompletionAnimDuration { get; set; } = 0.5f;

        [DebugEditable(DisplayName = "Reveal Anim Duration", Step = 0.1f, Min = 0.1f, Max = 2f)]
        public float RevealAnimDuration { get; set; } = 0.3f;

        [DebugEditable(DisplayName = "Animation Delay", Step = 0.05f, Min = 0f, Max = 1f)]
        public float AnimationDelay { get; set; } = 0.15f;

        // Colors
        private readonly Color _completedColor = Color.White;
        private readonly Color _visibleColor = new Color(139, 0, 0); // Dark Red
        private readonly Color _hiddenColor = Color.Black;

        public AchievementGridDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;

            EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
        }

        private void OnLoadScene(LoadSceneEvent evt)
        {
            if (evt.Scene != SceneId.Achievement) return;

            // Reset animation state
            _phase = AnimationPhase.Idle;
            _animationTime = 0f;
            _animationIndex = 0;
            _completionQueue.Clear();
            _revealQueue.Clear();
            _gridCreated = false;

            // Queue up unseen completions for animation
            var unseen = AchievementManager.GetCompleteUnseen().ToList();
            if (unseen.Count > 0)
            {
                _completionQueue = unseen;
                _phase = AnimationPhase.PlayingCompletions;
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

            // Create grid entities if needed
            if (!_gridCreated)
            {
                CreateGridEntities();
                _gridCreated = true;
            }

            // Update animations
            UpdateAnimations(dt);

            // Update hover states and scaling
            UpdateHoverStates(dt);
        }

        private void CreateGridEntities()
        {
            // Clear existing
            foreach (var kv in _gridEntities)
            {
                if (kv.Value != null)
                    EntityManager.DestroyEntity(kv.Value.Id);
            }
            _gridEntities.Clear();

            // Create grid cells
            for (int row = 0; row < GRID_ROWS; row++)
            {
                for (int col = 0; col < GRID_COLUMNS; col++)
                {
                    string key = $"grid_{row}_{col}";
                    var ent = EntityManager.CreateEntity($"AchievementGrid_{row}_{col}");

                    // Find achievement at this position
                    var achievement = AchievementManager.GetAll()
                        .FirstOrDefault(a => a.Row == row && a.Column == col);

                    var rect = GetCellRect(row, col);
                    var pos = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

                    EntityManager.AddComponent(ent, new Transform { Position = pos, BasePosition = pos, ZOrder = 100 });
                    EntityManager.AddComponent(ent, new UIElement
                    {
                        Bounds = rect,
                        IsInteractable = achievement != null && achievement.State != AchievementState.Hidden,
                        TooltipType = TooltipType.None
                    });
                    var parallax = ParallaxLayer.GetUIParallaxLayer();
                    if (achievement != null && achievement.State == AchievementState.Visible)
                    {
                        parallax.MultiplierX = 0.02f; 
                        parallax.MultiplierY = 0.02f; 
                    }
                    if (achievement != null && (achievement.State == AchievementState.CompleteSeen || achievement.State == AchievementState.CompleteUnseen))
                    {
                        parallax.MultiplierX = 0.015f; 
                        parallax.MultiplierY = 0.015f; 
                    }
                    EntityManager.AddComponent(ent, parallax);
                    EntityManager.AddComponent(ent, new AchievementGridItem
                    {
                        AchievementId = achievement?.Id ?? string.Empty,
                        Row = row,
                        Column = col,
                        CurrentScale = 1f,
                        TargetScale = 1f,
                        Alpha = 1f
                    });
                    EntityManager.AddComponent(ent, new OwnedByScene { Scene = SceneId.Achievement });

                    _gridEntities[key] = ent;
                }
            }
        }

        private Rectangle GetCellRect(int row, int col)
        {
            int x = GridOffsetX + col * (CellSize + CellGap);
            int y = GridOffsetY + row * (CellSize + CellGap);
            return new Rectangle(x, y, CellSize, CellSize);
        }

        private void UpdateAnimations(float dt)
        {
            switch (_phase)
            {
                case AnimationPhase.PlayingCompletions:
                    UpdateCompletionAnimations(dt);
                    break;
                case AnimationPhase.PlayingReveals:
                    UpdateRevealAnimations(dt);
                    break;
            }
        }

        private void UpdateCompletionAnimations(float dt)
        {
            if (_animationIndex >= _completionQueue.Count)
            {
                // Done with completions, check for reveals
                CollectRevealQueue();
                if (_revealQueue.Count > 0)
                {
                    _phase = AnimationPhase.PlayingReveals;
                    _animationIndex = 0;
                    _animationTime = 0f;
                }
                else
                {
                    _phase = AnimationPhase.Complete;
                }
                return;
            }

            _animationTime += dt;

            // Current achievement being animated
            var achievement = _completionQueue[_animationIndex];
            var gridItem = FindGridItem(achievement.Row, achievement.Column);

            if (gridItem != null)
            {
                // Pulse animation: scale up then back down
                float progress = _animationTime / CompletionAnimDuration;
                if (progress < 0.5f)
                {
                    // Scale up
                    gridItem.CurrentScale = 1f + (HoverScale - 1f) * 2f * progress;
                }
                else
                {
                    // Scale down
                    gridItem.CurrentScale = HoverScale - (HoverScale - 1f) * 2f * (progress - 0.5f);
                }
                gridItem.IsAnimatingCompletion = true;
            }

            // Move to next after duration + delay
            if (_animationTime >= CompletionAnimDuration + AnimationDelay)
            {
                if (gridItem != null)
                {
                    gridItem.CurrentScale = 1f;
                    gridItem.IsAnimatingCompletion = false;
                }

                // Mark as seen
                achievement.MarkAsSeen();

                _animationIndex++;
                _animationTime = 0f;
            }
        }

        private void CollectRevealQueue()
        {
            _revealQueue.Clear();
            // Find achievements that were just revealed (adjacent to completed ones)
            // They should be in Visible state but we check for any that need reveal animation
            foreach (var achievement in AchievementManager.GetAll())
            {
                if (achievement.State == AchievementState.Visible)
                {
                    var gridItem = FindGridItem(achievement.Row, achievement.Column);
                    if (gridItem != null && gridItem.Alpha < 1f)
                    {
                        _revealQueue.Add(achievement);
                    }
                }
            }
        }

        private void UpdateRevealAnimations(float dt)
        {
            if (_animationIndex >= _revealQueue.Count)
            {
                _phase = AnimationPhase.Complete;
                return;
            }

            _animationTime += dt;

            var achievement = _revealQueue[_animationIndex];
            var gridItem = FindGridItem(achievement.Row, achievement.Column);

            if (gridItem != null)
            {
                float progress = Math.Min(1f, _animationTime / RevealAnimDuration);
                gridItem.Alpha = progress;
                gridItem.IsAnimatingReveal = progress < 1f;
            }

            if (_animationTime >= RevealAnimDuration + AnimationDelay)
            {
                if (gridItem != null)
                {
                    gridItem.Alpha = 1f;
                    gridItem.IsAnimatingReveal = false;
                }
                _animationIndex++;
                _animationTime = 0f;
            }
        }

        private AchievementGridItem FindGridItem(int row, int col)
        {
            string key = $"grid_{row}_{col}";
            if (_gridEntities.TryGetValue(key, out var ent) && ent != null)
            {
                return ent.GetComponent<AchievementGridItem>();
            }
            return null;
        }

        private void UpdateHoverStates(float dt)
        {
            string hoveredId = string.Empty;

            foreach (var kv in _gridEntities)
            {
                var ent = kv.Value;
                if (ent == null) continue;

                var ui = ent.GetComponent<UIElement>();
                var gridItem = ent.GetComponent<AchievementGridItem>();
                var transform = ent.GetComponent<Transform>();

                if (ui == null || gridItem == null || transform == null) continue;

                // Skip if animating
                if (gridItem.IsAnimatingCompletion || gridItem.IsAnimatingReveal) continue;

                // Get achievement state
                var achievement = !string.IsNullOrEmpty(gridItem.AchievementId)
                    ? AchievementManager.GetAchievement(gridItem.AchievementId)
                    : null;

                // Update interactability based on state
                bool isInteractable = achievement != null && achievement.State != AchievementState.Hidden;
                ui.IsInteractable = isInteractable;

                // Handle hover
                if (ui.IsHovered && isInteractable)
                {
                    gridItem.TargetScale = HoverScale;
                    hoveredId = gridItem.AchievementId;
                }
                else
                {
                    gridItem.TargetScale = 1f;
                }

                // Lerp scale
                gridItem.CurrentScale = MathHelper.Lerp(gridItem.CurrentScale, gridItem.TargetScale, dt * ScaleLerpSpeed);

                // Update BasePosition in case grid settings changed
                var baseRect = GetCellRect(gridItem.Row, gridItem.Column);
                transform.BasePosition = new Vector2(baseRect.X + baseRect.Width / 2f, baseRect.Y + baseRect.Height / 2f);

                // Update bounds based on scale and current parallax-adjusted position
                int scaledSize = (int)(CellSize * gridItem.CurrentScale);
                ui.Bounds = new Rectangle(
                    (int)Math.Round(transform.Position.X - scaledSize / 2f),
                    (int)Math.Round(transform.Position.Y - scaledSize / 2f),
                    scaledSize,
                    scaledSize);
            }

            // Publish hover event
            EventManager.Publish(new AchievementGridItemHovered { AchievementId = hoveredId });
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Achievement) return;

            // Draw all grid cells
            foreach (var kv in _gridEntities)
            {
                var ent = kv.Value;
                if (ent == null) continue;

                var gridItem = ent.GetComponent<AchievementGridItem>();
                var ui = ent.GetComponent<UIElement>();

                if (gridItem == null || ui == null) continue;

                DrawGridCell(gridItem, ui);
            }
        }

        private void DrawGridCell(AchievementGridItem gridItem, UIElement ui)
        {
            var achievement = !string.IsNullOrEmpty(gridItem.AchievementId)
                ? AchievementManager.GetAchievement(gridItem.AchievementId)
                : null;

            // Determine color based on state
            Color cellColor;
            if (achievement == null)
            {
                cellColor = _hiddenColor;
            }
            else
            {
                switch (achievement.State)
                {
                    case AchievementState.CompleteSeen:
                    case AchievementState.CompleteUnseen:
                        cellColor = _completedColor;
                        break;
                    case AchievementState.Visible:
                        cellColor = _visibleColor;
                        break;
                    case AchievementState.Hidden:
                    default:
                        cellColor = _hiddenColor;
                        break;
                }
            }

            // Apply alpha for reveal animation
            cellColor = cellColor * gridItem.Alpha;

            // Get scaled texture
            int size = (int)(CellSize * gridItem.CurrentScale);
            var tex = GetOrCreateRoundedRect(size, size, (int)(CornerRadius * gridItem.CurrentScale));

            // Draw centered on bounds
            var drawRect = ui.Bounds;
            _spriteBatch.Draw(tex, drawRect, cellColor);

            // Draw hover highlight
            if (ui.IsHovered && achievement != null && achievement.State != AchievementState.Hidden)
            {
                var highlightColor = new Color(255, 255, 255, 60);
                _spriteBatch.Draw(tex, drawRect, highlightColor);
            }
        }

        private Texture2D GetOrCreateRoundedRect(int width, int height, int radius)
        {
            string key = $"{width}x{height}r{radius}";
            if (_roundedRectCache.TryGetValue(key, out var tex) && tex != null)
                return tex;

            tex = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
            _roundedRectCache[key] = tex;
            return tex;
        }
    }
}
