using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Events
{
    /// <summary>
    /// Event to trigger plunder during preblock phase.
    /// Published by AppliedPassivesManagementSystem when enemy has Plunder passive.
    /// </summary>
    public class PlunderTriggerEvent
    {
        public Entity Enemy { get; set; }
    }

    /// <summary>
    /// Event published when a card is plundered from the player's deck.
    /// </summary>
    public class PlunderCardEvent
    {
        public Entity Card { get; set; }
        public int DamageThreshold { get; set; }
    }

    /// <summary>
    /// Event published when the player rescues a plundered card by dealing enough damage.
    /// </summary>
    public class PlunderRescueEvent
    {
        public Entity Card { get; set; }
    }
}
