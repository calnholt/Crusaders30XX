using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for handling combat mechanics
    /// </summary>
    public class CombatSystem : Core.System
    {
        public CombatSystem(EntityManager entityManager) : base(entityManager) { }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Player>()
                .Concat(EntityManager.GetEntitiesWithComponent<Enemy>());
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var player = entity.GetComponent<Player>();
            var enemy = entity.GetComponent<Enemy>();
            
            if (player != null)
            {
                UpdatePlayer(player);
            }
            else if (enemy != null)
            {
                UpdateEnemy(enemy);
            }
        }
        
        private void UpdatePlayer(Player player)
        {
            // Reset energy at start of turn
            // This would typically be called by a turn management system
        }
        
        private void UpdateEnemy(Enemy enemy)
        {
            // Handle enemy AI and actions
            // This would be expanded based on your game's AI system
        }
        
        /// <summary>
        /// Applies damage to a target, considering block
        /// </summary>
        public void ApplyDamage(Entity target, int damage)
        {
            var player = target.GetComponent<Player>();
            var enemy = target.GetComponent<Enemy>();
            
            if (player != null)
            {
                ApplyDamageToPlayer(player, damage);
            }
            else if (enemy != null)
            {
                ApplyDamageToEnemy(enemy, damage);
            }
        }
        
        private void ApplyDamageToPlayer(Player player, int damage)
        {
            if (player.Block > 0)
            {
                if (player.Block >= damage)
                {
                    player.Block -= damage;
                    damage = 0;
                }
                else
                {
                    damage -= player.Block;
                    player.Block = 0;
                }
            }
            
            player.CurrentHealth -= damage;
            if (player.CurrentHealth < 0)
                player.CurrentHealth = 0;
        }
        
        private void ApplyDamageToEnemy(Enemy enemy, int damage)
        {
            if (enemy.Block > 0)
            {
                if (enemy.Block >= damage)
                {
                    enemy.Block -= damage;
                    damage = 0;
                }
                else
                {
                    damage -= enemy.Block;
                    enemy.Block = 0;
                }
            }
            
            enemy.CurrentHealth -= damage;
            if (enemy.CurrentHealth < 0)
                enemy.CurrentHealth = 0;
        }
        
        /// <summary>
        /// Adds block to a target
        /// </summary>
        public void AddBlock(Entity target, int block)
        {
            var player = target.GetComponent<Player>();
            var enemy = target.GetComponent<Enemy>();
            
            if (player != null)
            {
                player.Block += block;
            }
            else if (enemy != null)
            {
                enemy.Block += block;
            }
        }
    }
} 