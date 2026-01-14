using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Cards;

namespace Crusaders30XX.ECS.Objects.Achievements
{
    /// <summary>
    /// Block with a relic.
    /// </summary>
    public class YouChosePoorly : AchievementBase
    {
        public YouChosePoorly()
        {
            Id = "you_chose_poorly";
            Name = "You Chose Poorly";
            Description = "Block with a relic";
            Row = 5;
            Column = 1;
            StartsVisible = false;
            Points = 5;
        }

        public override void RegisterListeners()
        {
            EventManager.Subscribe<CardBlockedEvent>(OnCardBlocked);
        }

        public override void UnregisterListeners()
        {
            EventManager.Unsubscribe<CardBlockedEvent>(OnCardBlocked);
        }

        private void OnCardBlocked(CardBlockedEvent evt)
        {
            // Check if the card that blocked is a relic
            if (evt?.Card == null) return;

            var cardData = evt.Card.GetComponent<CardData>();
            if (cardData?.Card == null) return;

            // Check if it's a relic card
            if (cardData.Card.Type == CardType.Relic)
            {
                Complete();
            }
        }

        protected override void EvaluateCompletion()
        {
            // Completion is checked directly in the event handler
        }
    }
}
