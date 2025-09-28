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

    /// <summary>
    /// Emitted when the player's Temperance ability activates.
    /// Used to drive player-focused VFX.
    /// </summary>
    public class TriggerTemperance
    {
        public Entity Owner { get; set; }
        public string AbilityId { get; set; }
    }

    // Rendering event for customization-temperance entries
    public class TemperanceAbilityRenderEvent
    {
        public string AbilityId { get; set; }
        public Microsoft.Xna.Framework.Rectangle Bounds { get; set; }
        public bool IsEquipped { get; set; }
        public float NameScale { get; set; }
        public float TextScale { get; set; }
    }
}


