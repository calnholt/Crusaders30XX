using System.Collections.Generic;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Objects.EnemyAttacks;

namespace Crusaders30XX.ECS.Factories
{
    /// <summary>
    /// Factory for creating EnemyBase instances from enemy IDs
    /// </summary>
    public static class EnemyFactory
    {
        /// <summary>
        /// Creates an EnemyBase instance from an enemy ID string
        /// </summary>
        /// <param name="enemyId">The enemy ID (e.g., "demon")</param>
        /// <returns>The corresponding EnemyBase instance, or null if not found</returns>
        public static EnemyBase Create(string enemyId, EnemyDifficulty difficulty = EnemyDifficulty.Easy)
        {
            return enemyId switch
            {
                "demon" => new Demon(difficulty),
                "gleeber" => new Gleeber(difficulty),
                "mummy" => new Mummy(difficulty),
                "ninja" => new Ninja(difficulty),
                "ogre" => new Ogre(difficulty),
                "sand_corpse" => new SandCorpse(difficulty),
                "sand_golem" => new SandGolem(difficulty),
                "skeleton" => new Skeleton(difficulty),
                "skeletal_archer" => new SkeletalArcher(difficulty),
                "spider" => new Spider(difficulty),
                "succubus" => new Succubus(difficulty),
                "cactus" => new Cactus(difficulty),
                "dust_wuurm" => new DustWuurm(difficulty),
                "sorcerer" => new Sorcerer(difficulty),
                "ice_demon" => new IceDemon(difficulty),
                "glacial_guardian" => new GlacialGuardian(difficulty),
                "cinderbolt_demon" => new CinderboltDemon(difficulty),
                "fire_skeleton" => new FireSkeleton(difficulty),
                "berserker" => new Berserker(difficulty),
                "shadow" => new Shadow(difficulty),
                "earth_demon" => new EarthDemon(difficulty),
                _ => null
            };
        }

        /// <summary>
        /// Returns a dictionary of all available enemies, keyed by enemy ID
        /// </summary>
        /// <returns>A dictionary mapping enemy IDs to EnemyBase instances</returns>
        public static Dictionary<string, EnemyBase> GetAllEnemies(EnemyDifficulty difficulty = EnemyDifficulty.Easy)
        {
            return new Dictionary<string, EnemyBase>
            {
                { "demon", new Demon(difficulty) },
                { "gleeber", new Gleeber(difficulty) },
                { "mummy", new Mummy(difficulty) },
                { "ninja", new Ninja(difficulty) },
                { "ogre", new Ogre(difficulty) },
                { "sand_corpse", new SandCorpse(difficulty) },
                { "sand_golem", new SandGolem(difficulty) },
                { "skeleton", new Skeleton(difficulty) },
                { "skeletal_archer", new SkeletalArcher(difficulty) },
                { "spider", new Spider(difficulty) },
                { "succubus", new Succubus(difficulty) },
                { "cactus", new Cactus(difficulty) },
                { "dust_wuurm", new DustWuurm(difficulty) },
                { "sorcerer", new Sorcerer(difficulty) },
                { "ice_demon", new IceDemon(difficulty) },
                { "glacial_guardian", new GlacialGuardian(difficulty) },
                { "cinderbolt_demon", new CinderboltDemon(difficulty) },
                { "fire_skeleton", new FireSkeleton(difficulty) },
                { "berserker", new Berserker(difficulty) },
                { "shadow", new Shadow(difficulty) },
                { "earth_demon", new EarthDemon(difficulty) },
            };
        }
    }
}

