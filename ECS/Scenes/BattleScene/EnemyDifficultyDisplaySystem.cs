using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Objects.Enemies;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Enemy Difficulty")]
    public class EnemyDifficultyDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly Dictionary<int, Entity> _displayEntities = new();

        [DebugEditable(DisplayName = "Chevron Width", Step = 1, Min = 2, Max = 100)]
        public float ChevronWidth { get; set; } = 21f;

        [DebugEditable(DisplayName = "Chevron Height", Step = 1, Min = 2, Max = 100)]
        public float ChevronHeight { get; set; } = 11f;

        [DebugEditable(DisplayName = "Chevron Thickness", Step = 0.5f, Min = 1, Max = 20)]
        public float ChevronThickness { get; set; } = 2.5f;

        [DebugEditable(DisplayName = "Chevron Gap", Step = 1, Min = -20, Max = 50)]
        public float ChevronGap { get; set; } = -3f;

        [DebugEditable(DisplayName = "Offset X", Step = 1, Min = -500, Max = 500)]
        public float OffsetX { get; set; } = 12f;

        [DebugEditable(DisplayName = "Offset Y", Step = 1, Min = -500, Max = 500)]
        public float OffsetY { get; set; } = 0f;

        [DebugEditable(DisplayName = "Scale", Step = 0.1f, Min = 0.1f, Max = 5f)]
        public float ChevronScale { get; set; } = 1f;

        [DebugEditable(DisplayName = "Color R", Step = 1, Min = 0, Max = 255)]
        public int ColorR { get; set; } = 0;

        [DebugEditable(DisplayName = "Color G", Step = 1, Min = 0, Max = 255)]
        public int ColorG { get; set; } = 0;

        [DebugEditable(DisplayName = "Color B", Step = 1, Min = 0, Max = 255)]
        public int ColorB { get; set; } = 0;

        [DebugEditable(DisplayName = "Alpha", Step = 0.05f, Min = 0, Max = 1f)]
        public float Alpha { get; set; } = 1f;

        public EnemyDifficultyDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Enemy>()
                .Where(e => e.HasComponent<HPBarAnchor>());
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Logic handled in Draw
        }

        public void Draw()
        {
            // Cleanup stale display entities
            var currentEnemyIds = GetRelevantEntities().Select(e => e.Id).ToHashSet();
            var staleIds = _displayEntities.Keys.Where(id => !currentEnemyIds.Contains(id)).ToList();
            foreach (var id in staleIds)
            {
                if (_displayEntities.TryGetValue(id, out var de))
                {
                    EntityManager.DestroyEntity(de.Id);
                }
                _displayEntities.Remove(id);
            }

            foreach (var enemyEntity in GetRelevantEntities())
            {
                var enemy = enemyEntity.GetComponent<Enemy>();
                var anchor = enemyEntity.GetComponent<HPBarAnchor>();
                
                if (enemy?.EnemyBase == null || anchor == null) continue;

                // Find or create the display entity
                if (!_displayEntities.TryGetValue(enemyEntity.Id, out var displayEntity) || EntityManager.GetEntity(displayEntity.Id) == null)
                {
                    displayEntity = EntityManager.CreateEntity($"DifficultyDisplay_{enemyEntity.Id}");
                    EntityManager.AddComponent(displayEntity, new Transform());
                    EntityManager.AddComponent(displayEntity, new UIElement { IsInteractable = false, TooltipPosition = TooltipPosition.Above });
                    EntityManager.AddComponent(displayEntity, new DifficultyDisplayMarker { EnemyEntity = enemyEntity });
                    _displayEntities[enemyEntity.Id] = displayEntity;
                }

                var transform = displayEntity.GetComponent<Transform>();
                var ui = displayEntity.GetComponent<UIElement>();

                int difficultyCount = 1;
                string tooltip = "Acolyte";
                switch (enemy.EnemyBase.Difficulty)
                {
                    case EnemyDifficulty.Easy: difficultyCount = 1; tooltip = "Acolyte"; break;
                    case EnemyDifficulty.Medium: difficultyCount = 2; tooltip = "Fiend"; break;
                    case EnemyDifficulty.Hard: difficultyCount = 3; tooltip = "Overlord"; break;
                }

                float scaledHeight = ChevronHeight * ChevronScale;
                float totalStackHeight = (difficultyCount * scaledHeight) + ((difficultyCount - 1) * ChevronGap * ChevronScale);
                float totalStackWidth = ChevronWidth * ChevronScale;

                // Position to the right of the HP bar
                float startX = anchor.Rect.Right + OffsetX;
                float centerY = anchor.Rect.Center.Y + OffsetY;
                float topY = centerY - (totalStackHeight / 2f);

                // Sync Transform and UIElement
                transform.Position = new Vector2(startX, topY);
                ui.Bounds = new Rectangle((int)startX, (int)topY, (int)totalStackWidth, (int)totalStackHeight + 10);
                ui.Tooltip = tooltip;

                Texture2D chevronMask = PrimitiveTextureFactory.GetAntialiasedChevronMask(
                    _graphicsDevice,
                    ChevronWidth,
                    ChevronHeight,
                    ChevronThickness,
                    difficultyCount,
                    ChevronGap
                );

                if (chevronMask == null) continue;

                Color tint = new Color(ColorR, ColorG, ColorB) * Alpha;
                Vector2 scale = new Vector2(ChevronScale);
                
                _spriteBatch.Draw(chevronMask, transform.Position, null, tint, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
