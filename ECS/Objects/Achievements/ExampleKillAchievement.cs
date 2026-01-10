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
            // Listen for HP modifications to detect enemy deaths
            EventManager.Subscribe<ModifyHpEvent>(OnModifyHp);
        }

        public override void UnregisterListeners()
        {
            EventManager.Unsubscribe<ModifyHpEvent>(OnModifyHp);
        }

        private void OnModifyHp(ModifyHpEvent evt)
        {
            // Only care about damage to enemies
            if (evt.Target == null || !evt.Target.HasComponent<Enemy>()) return;
            if (evt.Delta >= 0) return; // Not damage

            // Check if enemy died
            var hp = evt.Target.GetComponent<HP>();
            if (hp == null || hp.Current > 0) return;

            // Enemy died! Increment progress
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
            EventManager.Subscribe<ModifyHpEvent>(OnModifyHp);
        }

        public override void UnregisterListeners()
        {
            EventManager.Unsubscribe<ModifyHpEvent>(OnModifyHp);
        }

        private void OnModifyHp(ModifyHpEvent evt)
        {
            // Only care about damage to enemies
            if (evt.Target == null || !evt.Target.HasComponent<Enemy>()) return;
            if (evt.Delta >= 0) return; // Not damage

            // Check if it's a skeleton
            var enemy = evt.Target.GetComponent<Enemy>();
            if (enemy == null || enemy.Id != "skeleton") return;

            // Check if enemy died
            var hp = evt.Target.GetComponent<HP>();
            if (hp == null || hp.Current > 0) return;

            // Skeleton died! Increment progress
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
