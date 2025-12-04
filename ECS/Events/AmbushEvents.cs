using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Events
{
    /// <summary>
    /// Fired when an ambush attack's timer expires, just before auto-confirming the enemy attack.
    /// Carries the active ambush context so listeners can react (e.g., auto-assign blocks).
    /// </summary>
    public class AmbushTimerExpired
    {
        public string ContextId { get; set; }
    }
}



