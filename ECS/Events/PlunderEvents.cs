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

    /// <summary>
    /// Event to trigger the plunder snatch animation.
    /// Published after card selection, before zone mutation.
    /// </summary>
    public class PlunderSnatchAnimationRequested
    {
        public Entity Card { get; set; }
        public Microsoft.Xna.Framework.Vector2 StartPos { get; set; }
        public Microsoft.Xna.Framework.Vector2 TargetPos { get; set; }
        public int DamageThreshold { get; set; }
    }

    /// <summary>
    /// Event published when the plunder snatch animation completes.
    /// </summary>
    public class PlunderSnatchAnimationCompleted
    {
        public Entity Card { get; set; }
        public int DamageThreshold { get; set; }
    }
}
