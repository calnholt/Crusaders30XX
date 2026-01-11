using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Achievements
{
    /// <summary>
    /// Example achievement: Win your first battle.
    /// Demonstrates ephemeral condition tracking (no persistent counter needed).
    /// </summary>
    public class ExampleFirstVictoryAchievement : AchievementBase
    {
        public ExampleFirstVictoryAchievement()
        {
            Id = "example_first_victory";
            Name = "First Victory";
            Description = "Win your first battle";
            Row = 1;
            Column = 0;
            StartsVisible = false; // Starter achievement
            Points = 10;
        }

        public override void RegisterListeners()
        {
            // ShowQuestRewardOverlay is published when a quest/battle is won
            EventManager.Subscribe<ShowQuestRewardOverlay>(OnQuestComplete);
        }

        public override void UnregisterListeners()
        {
            EventManager.Unsubscribe<ShowQuestRewardOverlay>(OnQuestComplete);
        }

        private void OnQuestComplete(ShowQuestRewardOverlay evt)
        {
            // Battle/quest won - complete the achievement immediately
            Complete();
        }

        protected override void EvaluateCompletion()
        {
            // Not needed - we complete directly in the event handler
        }
    }

    /// <summary>
    /// Example achievement: Win 10 battles.
    /// Demonstrates counter-based tracking for battle wins.
    /// </summary>
    public class ExampleVeteranAchievement : AchievementBase
    {
        private const int RequiredWins = 10;

        public ExampleVeteranAchievement()
        {
            Id = "example_veteran";
            Name = "Veteran";
            Description = "Win 10 battles";
            Row = 1;
            Column = 1;
            StartsVisible = false; // Revealed when adjacent achievement is completed
            TargetValue = RequiredWins;
            Points = 20;
        }

        public override void RegisterListeners()
        {
            EventManager.Subscribe<ShowQuestRewardOverlay>(OnQuestComplete);
        }

        public override void UnregisterListeners()
        {
            EventManager.Unsubscribe<ShowQuestRewardOverlay>(OnQuestComplete);
        }

        private void OnQuestComplete(ShowQuestRewardOverlay evt)
        {
            IncrementProgress();
        }

        protected override void EvaluateCompletion()
        {
            if (GetProgress() >= RequiredWins)
            {
                Complete();
            }
        }
    }
}
