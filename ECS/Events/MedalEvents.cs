using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Events
{
    /// <summary>
    /// Request to add a medal to the working loadout in customization.
    /// </summary>
    public class AddMedalToLoadoutRequested
    {
        public string MedalId { get; set; }
    }

    /// <summary>
    /// Request to remove a medal from the working loadout in customization.
    /// If Index is provided, it disambiguates duplicates by position.
    /// </summary>
    public class RemoveMedalFromLoadoutRequested
    {
        public string MedalId { get; set; }
        public int? Index { get; set; }
    }

    /// <summary>
    /// Rendering event for customization-medal entries.
    /// </summary>
    public class MedalRenderEvent
    {
        public string MedalId { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsEquipped { get; set; }
        public float NameScale { get; set; }
        public float TextScale { get; set; }
    }
}




