using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Events
{
    /// <summary>
    /// Request to update the selected equipment for a given customization slot/tab.
    /// </summary>
    public class UpdateEquipmentLoadoutRequested
    {
        public CustomizationTabType Slot { get; set; }
        public string EquipmentId { get; set; }
    }

    /// <summary>
    /// Rendering event for customization-equipment entries (left list and right equipped panel).
    /// </summary>
    public class EquipmentRenderEvent
    {
        public string EquipmentId { get; set; }
        public Microsoft.Xna.Framework.Rectangle Bounds { get; set; }
        public bool IsEquipped { get; set; }
        public float NameScale { get; set; }
        public float TextScale { get; set; }
    }
}

 

