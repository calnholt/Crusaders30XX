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
}


