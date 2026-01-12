using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Achievements
{
    /// <summary>
    /// Play 100 red cards.
    /// </summary>
    public class RedCardApprentice : AchievementBase
    {
        private const int RequiredPlays = 100;

        public RedCardApprentice()
        {
            Id = "red_card_apprentice";
            Name = "Red Card Apprentice";
            Description = $"Play {RequiredPlays} red cards";
            Row = 2;
            Column = 1;
            StartsVisible = false;
            TargetValue = RequiredPlays;
            Points = 20;
        }

        public override void RegisterListeners()
        {
            EventManager.Subscribe<CardPlayedEvent>(OnCardPlayedEvent);
        }

        public override void UnregisterListeners()
        {
            EventManager.Unsubscribe<CardPlayedEvent>(OnCardPlayedEvent);
        }

        private void OnCardPlayedEvent(CardPlayedEvent evt)
        {
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
