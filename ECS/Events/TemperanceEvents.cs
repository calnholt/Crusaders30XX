using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Events
{
    /// <summary>
    /// Increase or decrease the player's Temperance by Delta.
    /// Triggers ability checks after modification.
    /// </summary>
    public class ModifyTemperanceEvent
    {
        public int Delta { get; set; } = 0;
    }
    public class SetTemperanceEvent
    {
        public int Amount { get; set; } = 0;
    }
}


