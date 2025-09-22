using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Events
{
    /// <summary>
    /// Apply a delta to a passive on an entity. Creates the passive if needed.
    /// If the resulting stacks are <= 0, removes the passive from the dictionary.
    /// </summary>
    public class ApplyPassiveEvent
    {
        public Entity Owner { get; set; }
        public AppliedPassiveType Type { get; set; }
        public int Delta { get; set; }
    }

    /// <summary>
    /// Remove all stacks/instances of a passive type from the specified entity.
    /// </summary>
    public class RemovePassive
    {
        public Entity Owner { get; set; }
        public AppliedPassiveType Type { get; set; }
    }

    /// <summary>
    /// Emitted when a passive effect actually triggers (e.g., Burn deals damage).
    /// Used by UI to play feedback animations.
    /// </summary>
    public class PassiveTriggered
    {
        public Entity Owner { get; set; }
        public AppliedPassiveType Type { get; set; }
    }
    /// <summary>
    /// Update the stacks of a passive on an entity.
    /// </summary>
    public class UpdatePassive
    {
        public Entity Owner { get; set; }
        public AppliedPassiveType Type { get; set; }
        public int Delta { get; set; }
    }
}


