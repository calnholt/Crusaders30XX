using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Objects.EnemyAttacks;

namespace Crusaders30XX.ECS.Factories
{
    /// <summary>
    /// Factory for creating EnemyBase instances from enemy IDs
    /// </summary>
    public static class EnemyFactory
    {
        private static readonly IReadOnlyDictionary<string, Func<EnemyDifficulty, EnemyBase>> EnemyConstructors =
            new Dictionary<string, Func<EnemyDifficulty, EnemyBase>>(StringComparer.OrdinalIgnoreCase)
            {
                { "demon", difficulty => new Demon(difficulty) },
                { "gleeber", difficulty => new Gleeber(difficulty) },
                { "mummy", difficulty => new Mummy(difficulty) },
                { "ninja", difficulty => new Ninja(difficulty) },
                { "ogre", difficulty => new Ogre(difficulty) },
                { "sand_corpse", difficulty => new SandCorpse(difficulty) },
                { "sand_golem", difficulty => new SandGolem(difficulty) },
                { "skeleton", difficulty => new Skeleton(difficulty) },
                { "skeletal_archer", difficulty => new SkeletalArcher(difficulty) },
                { "spider", difficulty => new Spider(difficulty) },
                { "succubus", difficulty => new Succubus(difficulty) },
                { "cactus", difficulty => new Cactus(difficulty) },
                { "dust_wuurm", difficulty => new DustWuurm(difficulty) },
                { "sorcerer", difficulty => new Sorcerer(difficulty) },
                { "ice_demon", difficulty => new IceDemon(difficulty) },
                { "glacial_guardian", difficulty => new GlacialGuardian(difficulty) },
                { "cinderbolt_demon", difficulty => new CinderboltDemon(difficulty) },
                { "fire_skeleton", difficulty => new FireSkeleton(difficulty) },
                { "berserker", difficulty => new Berserker(difficulty) },
                { "shadow", difficulty => new Shadow(difficulty) },
                { "earth_demon", difficulty => new EarthDemon(difficulty) },
                { "medusa", difficulty => new Medusa(difficulty) },
                { "wyvern", difficulty => new Wyvern(difficulty) },
                { "blood_martyr", difficulty => new BloodMartyr(difficulty) },
                { "sniper", difficulty => new Sniper(difficulty) },
                { "fallen_shepherd", difficulty => new FallenShepherd(difficulty) },
            };

        /// <summary>
        /// Creates an EnemyBase instance from an enemy ID string
        /// </summary>
        /// <param name="enemyId">The enemy ID (e.g., "demon")</param>
        /// <returns>The corresponding EnemyBase instance, or null if not found</returns>
        public static EnemyBase Create(string enemyId, EnemyDifficulty difficulty = EnemyDifficulty.Easy)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return null;
            return EnemyConstructors.TryGetValue(enemyId, out var create)
                ? create(difficulty)
                : null;
        }

        public static bool IsRegistered(string enemyId)
        {
            return !string.IsNullOrWhiteSpace(enemyId) && EnemyConstructors.ContainsKey(enemyId);
        }

        /// <summary>
        /// Returns a dictionary of all available enemies, keyed by enemy ID
        /// </summary>
        /// <returns>A dictionary mapping enemy IDs to EnemyBase instances</returns>
        public static Dictionary<string, EnemyBase> GetAllEnemies(EnemyDifficulty difficulty = EnemyDifficulty.Easy)
        {
            return EnemyConstructors.ToDictionary(
                entry => entry.Key,
                entry => entry.Value(difficulty),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
