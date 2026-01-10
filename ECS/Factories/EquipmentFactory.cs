using System.Collections.Generic;
using Crusaders30XX.ECS.Objects.Equipment;

namespace Crusaders30XX.ECS.Factories
{
    /// <summary>
    /// Factory for creating EquipmentBase instances from equipment IDs
    /// </summary>
    public static class EquipmentFactory
    {
        /// <summary>
        /// Creates an EquipmentBase instance from an equipment ID string
        /// </summary>
        /// <param name="equipmentId">The equipment ID (e.g., "pierced_heart_place")</param>
        /// <returns>The corresponding EquipmentBase instance, or null if not found</returns>
        public static EquipmentBase Create(string equipmentId)
        {
            return equipmentId switch
            {
                "helm_of_seeing" => new HelmOfSeeing(),
                "knightly_chest" => new KnightlyChest(),
                "knightly_grieves" => new KnightlyGrieves(),
                "knightly_gauntlets" => new KnightlyGauntlets(),
                "knightly_helm" => new KnightlyHelm(),
                "pierced_heart_plate" => new PiercedHeartPlate(),
                "purging_bracers" => new PurgingBracers(),
                _ => null
            };
        }

        /// <summary>
        /// Returns a dictionary of all available equipment, keyed by equipment ID
        /// </summary>
        /// <returns>A dictionary mapping equipment IDs to EquipmentBase instances</returns>
        public static Dictionary<string, EquipmentBase> GetAllEquipment()
        {
            return new Dictionary<string, EquipmentBase>
            {
                { "helm_of_seeing", new HelmOfSeeing() },
                { "knightly_chest", new KnightlyChest() },
                { "knightly_grieves", new KnightlyGrieves() },
                { "knightly_gauntlets", new KnightlyGauntlets() },
                { "knightly_helm", new KnightlyHelm() },
                { "pierced_heart_plate", new PiercedHeartPlate() },
                { "purging_bracers", new PurgingBracers() },
            };
        }
    }
}

