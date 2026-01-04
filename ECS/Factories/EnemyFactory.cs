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
        public static EnemyBase Create(string enemyId)
        {
            return enemyId switch
            {
                "demon" => new Demon(),
                "gleeber" => new Gleeber(),
                "mummy" => new Mummy(),
                "ninja" => new Ninja(),
                "ogre" => new Ogre(),
                "sand_corpse" => new SandCorpse(),
                "sand_golem" => new SandGolem(),
                "skeleton" => new Skeleton(),
                "spider" => new Spider(),
                "succubus" => new Succubus(),
                "cactus" => new Cactus(),
                "dust_wuurm" => new DustWuurm(),
                "sorcerer" => new Sorcerer(),
                "ice_demon" => new IceDemon(),
                "glacial_guardian" => new GlacialGuardian(),
                _ => null
            };
        }

        /// <summary>
        /// Returns a dictionary of all available enemies, keyed by enemy ID
        /// </summary>
        /// <returns>A dictionary mapping enemy IDs to EnemyBase instances</returns>
        public static Dictionary<string, EnemyBase> GetAllEnemies()
        {
            return new Dictionary<string, EnemyBase>
            {
                { "demon", new Demon() },
                { "gleeber", new Gleeber() },
                { "mummy", new Mummy() },
                { "ninja", new Ninja() },
                { "ogre", new Ogre() },
                { "sand_corpse", new SandCorpse() },
                { "sand_golem", new SandGolem() },
                { "skeleton", new Skeleton() },
                { "spider", new Spider() },
                { "succubus", new Succubus() },
                { "cactus", new Cactus() },
                { "dust_wuurm", new DustWuurm() },
                { "sorcerer", new Sorcerer() },
                { "ice_demon", new IceDemon() },
                { "glacial_guardian", new GlacialGuardian() }
            };
        }
    }
}

