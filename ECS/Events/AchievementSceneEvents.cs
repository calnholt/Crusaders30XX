namespace Crusaders30XX.ECS.Events
{
    /// <summary>
    /// Published when a grid item is hovered in the Achievement scene.
    /// Used to update the description panel.
    /// </summary>
    public class AchievementGridItemHovered
    {
        /// <summary>
        /// The achievement ID being hovered, or empty if unhovered.
        /// </summary>
        public string AchievementId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Published when a completion animation finishes for an achievement.
    /// </summary>
    public class AchievementAnimationComplete
    {
        /// <summary>
        /// The achievement ID that finished animating.
        /// </summary>
        public string AchievementId { get; set; } = string.Empty;

        /// <summary>
        /// Type of animation that completed.
        /// </summary>
        public AchievementAnimationType AnimationType { get; set; } = AchievementAnimationType.Completion;
    }

    /// <summary>
    /// Type of achievement animation.
    /// </summary>
    public enum AchievementAnimationType
    {
        /// <summary>
        /// Animation for a newly completed achievement.
        /// </summary>
        Completion,

        /// <summary>
        /// Animation for revealing a hidden achievement.
        /// </summary>
        Reveal
    }

    /// <summary>
    /// Published when all completion and reveal animations are done.
    /// </summary>
    public class AchievementAnimationsComplete { }
}
