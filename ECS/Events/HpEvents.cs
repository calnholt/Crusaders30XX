using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Events
{
    /// <summary>
    /// Adjust HP by Delta (negative to lose HP, positive to heal).
    /// If Target is null, applies to the first Player entity with an HP component.
    /// </summary>
    public class ModifyHpEvent
    {
        public Entity Target { get; set; }
        public int Delta { get; set; }
    }

    /// <summary>
    /// Set current HP to a specified value (clamped to [0, Max]).
    /// If Target is null, applies to the first Player entity with an HP component.
    /// </summary>
    public class SetHpEvent
    {
        public Entity Target { get; set; }
        public int Value { get; set; }
    }
}


