using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Events
{
    /// <summary>
    /// Generic tracking event to increment counters in BattleStateInfo.
    /// Carries a TrackingTypeEnum and a signed delta to apply.
    /// </summary>
    public class TrackingEvent
    {
        public string Type { get; set; }
        public int Delta { get; set; } = 1;
    }
}


