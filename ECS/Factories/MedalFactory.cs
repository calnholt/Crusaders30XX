using System.Collections.Generic;
using Crusaders30XX.ECS.Objects.Medals;

namespace Crusaders30XX.ECS.Factories
{
    /// <summary>
    /// Factory for creating MedalBase instances from medal IDs
    /// </summary>
    public static class MedalFactory
    {
        /// <summary>
        /// Creates a MedalBase instance from a medal ID string
        /// </summary>
        /// <param name="medalId">The medal ID (e.g., "st_luke")</param>
        /// <returns>The corresponding MedalBase instance, or null if not found</returns>
        public static MedalBase Create(string medalId)
        {
            return medalId switch
            {
                "st_luke" => new StLuke(),
                "st_michael" => new StMichael(),
                _ => null
            };
        }

        /// <summary>
        /// Returns a dictionary of all available medals, keyed by medal ID
        /// </summary>
        /// <returns>A dictionary mapping medal IDs to MedalBase instances</returns>
        public static Dictionary<string, MedalBase> GetAllMedals()
        {
            return new Dictionary<string, MedalBase>
            {
                { "st_luke", new StLuke() },
                { "st_michael", new StMichael() },
            };
        }
    }
}

