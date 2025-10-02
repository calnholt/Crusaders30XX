using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for rendering sprites and UI elements
    /// </summary>
    public class RenderingSystem : Core.System
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Dictionary<string, Texture2D> _textureCache = new();
        
        public RenderingSystem(EntityManager entityManager, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice) 
            : base(entityManager)
        {
            _spriteBatch = spriteBatch;
            _graphicsDevice = graphicsDevice;
        }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Sprite>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Rendering is typically done in a separate Draw method
            // This Update method could handle animation updates
            var animation = entity.GetComponent<Animation>();
            if (animation != null && animation.IsPlaying)
            {
                UpdateAnimation(animation, gameTime);
            }
        }
        
        private void UpdateAnimation(Animation animation, GameTime gameTime)
        {
            animation.CurrentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            if (animation.CurrentTime >= animation.Duration)
            {
                if (animation.IsLooping)
                {
                    animation.CurrentTime = 0f;
                }
                else
                {
                    animation.IsPlaying = false;
                }
            }
        }
        
        /// <summary>
        /// Draws all entities with sprite components
        /// </summary>
        public void Draw()
        {
            var entities = GetRelevantEntities().OrderBy(e => 
            {
                var transform = e.GetComponent<Transform>();
                return transform?.ZOrder ?? 0;
            });
            
            foreach (var entity in entities)
            {
                DrawEntity(entity);
            }
        }
        
        private void DrawEntity(Entity entity)
        {
            var sprite = entity.GetComponent<Sprite>();
            var transform = entity.GetComponent<Transform>();
            
            if (sprite == null || !sprite.IsVisible) return;
            
            var texture = GetTexture(sprite.TexturePath);
            if (texture == null) return;
            
            var position = transform?.Position ?? Vector2.Zero;
            var scale = transform?.Scale ?? Vector2.One;
            var rotation = transform?.Rotation ?? 0f;
            var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            
            _spriteBatch.Draw(
                texture,
                position,
                sprite.SourceRectangle,
                sprite.Tint,
                rotation,
                origin,
                scale,
                SpriteEffects.None,
                0f
            );
        }
        
        private Texture2D GetTexture(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            
            if (!_textureCache.TryGetValue(path, out var texture))
            {
                // In a real implementation, you'd load the texture from content
                // For now, we'll return null
                return null;
            }
            
            return texture;
        }
    }
} 