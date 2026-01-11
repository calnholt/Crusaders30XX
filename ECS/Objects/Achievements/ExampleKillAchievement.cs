using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Achievements
{
    /// <summary>
    /// Example achievement: Kill 10 enemies (any type).
    /// Demonstrates counter-based tracking with persistent progress.
    /// </summary>
    public class ExampleKillAchievement : AchievementBase
    {
        private const int RequiredKills = 10;

        public ExampleKillAchievement()
        {
            Id = "example_kill_10";
            Name = "Slayer";
            Description = "Defeat 10 enemies";
            Row = 0;
            Column = 0;
            StartsVisible = true; // This is a starter achievement
            TargetValue = RequiredKills;
            Points = 10;
        }

        public override void RegisterListeners()
        {
            EventManager.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        public override void UnregisterListeners()
        {
            EventManager.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            // Only care about damage to enemies
            IncrementProgress();
        }

        protected override void EvaluateCompletion()
        {
            if (GetProgress() >= RequiredKills)
            {
                Complete();
            }
        }
    }

    /// <summary>
    /// Example achievement: Kill 5 Skeletons.
    /// Demonstrates counter-based tracking with enemy type filtering.
    /// </summary>
    public class ExampleSkeletonSlayerAchievement : AchievementBase
    {
        private const int RequiredKills = 5;

        public ExampleSkeletonSlayerAchievement()
        {
            Id = "example_skeleton_slayer";
            Name = "Skeleton Slayer";
            Description = "Defeat 5 Skeletons";
            Row = 0;
            Column = 1; // Adjacent to the starter achievement
            StartsVisible = false; // Revealed when adjacent achievement is completed
            TargetValue = RequiredKills;
            Points = 15;
        }

        public override void RegisterListeners()
        {
            EventManager.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        public override void UnregisterListeners()
        {
            EventManager.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            // Only care about damage to enemies
            if (evt.Enemy == null || !evt.Enemy.HasComponent<Enemy>()) return;
            var enemyBase = evt.Enemy.GetComponent<Enemy>().EnemyBase;
            if (enemyBase == null || enemyBase.Id != "skeleton") return;
            IncrementProgress();
        }

        protected override void EvaluateCompletion()
        {
            if (GetProgress() >= RequiredKills)
            {
                Complete();
            }
        }
    }
}
