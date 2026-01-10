using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Achievements
{
    /// <summary>
    /// Example achievement: Play 50 cards.
    /// Demonstrates counter-based tracking for card plays.
    /// </summary>
    public class ExampleCardPlayerAchievement : AchievementBase
    {
        private const int RequiredPlays = 50;

        public ExampleCardPlayerAchievement()
        {
            Id = "example_card_player";
            Name = "Card Player";
            Description = "Play 50 cards";
            Row = 2;
            Column = 0;
            StartsVisible = true; // Starter achievement
            TargetValue = RequiredPlays;
            Points = 15;
        }

        public override void RegisterListeners()
        {
            // CardMoved event fires when cards move between zones
            EventManager.Subscribe<CardMoved>(OnCardMoved);
        }

        public override void UnregisterListeners()
        {
            EventManager.Unsubscribe<CardMoved>(OnCardMoved);
        }

        private void OnCardMoved(CardMoved evt)
        {
            // Track cards moving from Hand to DiscardPile (played cards)
            if (evt.From == CardZoneType.Hand && evt.To == CardZoneType.DiscardPile)
            {
                IncrementProgress();
            }
        }

        protected override void EvaluateCompletion()
        {
            if (GetProgress() >= RequiredPlays)
            {
                Complete();
            }
        }
    }

    /// <summary>
    /// Example achievement: Play 10 red cards.
    /// Demonstrates counter-based tracking with card color filtering.
    /// </summary>
    public class ExampleRedCardMasterAchievement : AchievementBase
    {
        private const int RequiredPlays = 10;

        public ExampleRedCardMasterAchievement()
        {
            Id = "example_red_card_master";
            Name = "Red Card Master";
            Description = "Play 10 red cards";
            Row = 2;
            Column = 1;
            StartsVisible = false; // Revealed when adjacent achievement is completed
            TargetValue = RequiredPlays;
            Points = 20;
        }

        public override void RegisterListeners()
        {
            EventManager.Subscribe<CardMoved>(OnCardMoved);
        }

        public override void UnregisterListeners()
        {
            EventManager.Unsubscribe<CardMoved>(OnCardMoved);
        }

        private void OnCardMoved(CardMoved evt)
        {
            // Track cards moving from Hand to DiscardPile (played cards)
            if (evt.From != CardZoneType.Hand || evt.To != CardZoneType.DiscardPile) return;

            // Check if it's a red card
            var cardData = evt.Card?.GetComponent<CardData>();
            if (cardData == null || cardData.Color != CardData.CardColor.Red) return;

            IncrementProgress();
        }

        protected override void EvaluateCompletion()
        {
            if (GetProgress() >= RequiredPlays)
            {
                Complete();
            }
        }
    }
}
