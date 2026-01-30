using System.Collections.Generic;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Objects.EnemyAttacks;

namespace Crusaders30XX.ECS.Factories
{
    /// <summary>
    /// Factory for creating EnemyAttackBase instances from attack IDs
    /// </summary>
    public static class EnemyAttackFactory
    {
        /// <summary>
        /// Creates an EnemyAttackBase instance from an attack ID string
        /// </summary>
        /// <param name="attackId">The attack ID (e.g., "bone_strike")</param>
        /// <returns>The corresponding EnemyAttackBase instance, or null if not found</returns>
        public static EnemyAttackBase Create(string attackId)
        {
            return attackId switch
            {
                // Ogre attacks
                "pummel_into_submission" => new PummelIntoSubmission(),
                "tree_stomp" => new TreeStomp(),
                "slam_trunk" => new SlamTrunk(),
                "fake_out" => new FakeOut(),
                "thud" => new Thud(),
                // Skeleton attacks
                "bone_strike" => new BoneStrike(),
                "sweep" => new Sweep(),
                "calcify" => new Calcify(),
                "skull_crusher" => new SkullCrusher(),
                // SkeletalArcher attacks
                "piercing_shot" => new PiercingShot(),
                "pinning_arrow" => new PinningArrow(),
                "quick_shot" => new QuickShot(),
                "snipe" => new Snipe(),
                // Ninja attacks
                "slice" => new Slice(),
                "dice" => new Dice(),
                "dusk_flick" => new DuskFlick(),
                "cloaked_reaver" => new CloakedReaver(),
                "silencing_stab" => new SilencingStab(),
                "sharpen_blade" => new SharpenBlade(),
                "shadow_step" => new ShadowStep(),
                "nightveil_guillotine" => new NightveilGuillotine(),
                // Demon attacks
                "razor_maw" => new RazorMaw(),
                "scorching_claw" => new ScorchingClaw(),
                "infernal_execution" => new InfernalExecution(),
                // Gleeber attacks
                "pounce" => new Pounce(),
                // SandCorpse attacks
                "sand_blast" => new SandBlast(),
                "sand_storm" => new SandStorm(),
                // SandGolem attacks
                "sand_pound" => new SandPound(),
                "sand_slam" => new SandSlam(),
                // Spider attacks
                "suffocating_silk" => new SuffocatingSilk(),
                "mandible_breaker" => new MandibleBreaker(),
                "rafterfall_ambush" => new RafterfallAmbush(),
                "eight_limbs_of_death" => new EightLimbsOfDeath(),
                "fang_feint" => new FangFeint(),
                // Mummy attacks
                "entomb" => new Entomb(),
                "mummify" => new Mummify(),
                // Succubus attacks
                "velvet_fangs" => new VelvetFangs(),
                "soul_siphon" => new SoulSiphon(),
                "enthralling_gaze" => new EnthrallingGaze(),
                "crushing_adoration" => new CrushingAdoration(),
                "teasing_nip" => new TeasingNip(),
                // Cactus attacks
                "prickly_burst" => new PricklyBurst(),
                // DustWuurm attacks
                "dust_storm" => new DustStorm(),
                // Sorcerer attacks
                "strange_force" => new StrangeForce(),
                // IceDemon attacks
                "icy_blade" => new IcyBlade(),
                "frozen_claw" => new FrozenClaw(),
                "frost_eater" => new FrostEater(),
                // GlacialGuardian attacks
                "glacial_strike" => new GlacialStrike(),
                "glacial_blast" => new GlacialBlast(),
                // CinderboltDemon attacks
                "cinderbolt" => new Cinderbolt(),
                "insidious_bolt" => new InsidiousBolt(),
                // Berserker attacks
                "rage" => new Rage(),
                // Shadow attacks
                "shadow_strike" => new ShadowStrike(),
                "dissipating_darkness" => new EncroachingDarkness(),
                "snuff_out_the_light" => new SnuffOutTheLight(),
                "night_fall" => new NightFall(),
                "from_the_shadows" => new FromTheShadows(),
                // EarthDemon attacks
                "tremor_strike" => new TremorStrike(),
                "stone_barrage" => new StoneBarrage(),
                "earthen_wall" => new EarthenWall(),
                // Generic attacks
                "have_no_mercy" => new HaveNoMercy(),
                // Medusa attacks
                "gaze" => new Gaze(),
                "stone_stare" => new StoneStare(),
                "basilisk_glare" => new BasiliskGlare(),
                "serpent_strike" => new SerpentStrike(),
                _ => null
            };
        }

        /// <summary>
        /// Returns a dictionary of all available enemy attacks, keyed by attack ID
        /// </summary>
        /// <returns>A dictionary mapping attack IDs to EnemyAttackBase instances</returns>
        public static Dictionary<string, EnemyAttackBase> GetAllAttacks()
        {
            return new Dictionary<string, EnemyAttackBase>
            {
                // Ogre attacks
                { "pummel_into_submission", new PummelIntoSubmission() },
                { "tree_stomp", new TreeStomp() },
                { "slam_trunk", new SlamTrunk() },
                { "fake_out", new FakeOut() },
                { "thud", new Thud() },
                // Skeleton attacks
                { "bone_strike", new BoneStrike() },
                { "sweep", new Sweep() },
                { "calcify", new Calcify() },
                { "skull_crusher", new SkullCrusher() },
                // SkeletalArcher attacks
                { "piercing_shot", new PiercingShot() },
                { "pinning_arrow", new PinningArrow() },
                { "quick_shot", new QuickShot() },
                { "snipe", new Snipe() },
                // Ninja attacks
                { "slice", new Slice() },
                { "dice", new Dice() },
                { "dusk_flick", new DuskFlick() },
                { "cloaked_reaver", new CloakedReaver() },
                { "silencing_stab", new SilencingStab() },
                { "sharpen_blade", new SharpenBlade() },
                { "shadow_step", new ShadowStep() },
                { "nightveil_guillotine", new NightveilGuillotine() },
                // Demon attacks
                { "razor_maw", new RazorMaw() },
                { "scorching_claw", new ScorchingClaw() },
                { "infernal_execution", new InfernalExecution() },
                // Gleeber attacks
                { "pounce", new Pounce() },
                // SandCorpse attacks
                { "sand_blast", new SandBlast() },
                { "sand_storm", new SandStorm() },
                // SandGolem attacks
                { "sand_pound", new SandPound() },
                { "sand_slam", new SandSlam() },
                // Spider attacks
                { "suffocating_silk", new SuffocatingSilk() },
                { "mandible_breaker", new MandibleBreaker() },
                { "rafterfall_ambush", new RafterfallAmbush() },
                { "eight_limbs_of_death", new EightLimbsOfDeath() },
                { "fang_feint", new FangFeint() },
                // Mummy attacks
                { "entomb", new Entomb() },
                { "mummify", new Mummify() },
                // Succubus attacks
                { "velvet_fangs", new VelvetFangs() },
                { "soul_siphon", new SoulSiphon() },
                { "enthralling_gaze", new EnthrallingGaze() },
                { "crushing_adoration", new CrushingAdoration() },
                { "teasing_nip", new TeasingNip() },
                // Cactus attacks
                { "prickly_burst", new PricklyBurst() },
                // DustWuurm attacks
                { "dust_storm", new DustStorm() },
                // Sorcerer attacks
                { "strange_force", new StrangeForce() },
                // IceDemon attacks
                { "icy_blade", new IcyBlade() },
                { "frozen_claw", new FrozenClaw() },
                { "frost_eater", new FrostEater() },
                // GlacialGuardian attacks
                { "glacial_strike", new GlacialStrike() },
                { "glacial_blast", new GlacialBlast() },
                // CinderboltDemon attacks
                { "cinderbolt", new Cinderbolt() },
                { "insidious_bolt", new InsidiousBolt() },
                // Berserker attacks
                { "rage", new Rage() },
                // Shadow attacks
                { "shadow_strike", new ShadowStrike() },
                { "dissipating_darkness", new EncroachingDarkness() },
                { "snuff_out_the_light", new SnuffOutTheLight() },
                { "night_fall", new NightFall() },
                { "from_the_shadows", new FromTheShadows() },
                // EarthDemon attacks
                { "tremor_strike", new TremorStrike() },
                { "stone_barrage", new StoneBarrage() },
                { "earthen_wall", new EarthenWall() },
                // Generic attacks
                { "have_no_mercy", new HaveNoMercy() },
                // Medusa attacks
                { "gaze", new Gaze() },
                { "stone_stare", new StoneStare() },
                { "basilisk_glare", new BasiliskGlare() },
                { "serpent_strike", new SerpentStrike() },
            };
        }
    }
}

