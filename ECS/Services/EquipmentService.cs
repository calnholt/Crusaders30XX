using System.Collections.Generic;
using Crusaders30XX.ECS.Objects.Equipment;

namespace Crusaders30XX.ECS.Services
{
	public static class EquipmentService
	{
		public static string GetTooltipText(EquipmentBase equipment, EquipmentTooltipType type = EquipmentTooltipType.Battle)
		{
			if (equipment != null)
				{
					var parts = new List<string>();
					parts.Add(equipment.Text);
					string abilities = string.Join("\n", parts);
          string blockAndUses = equipment.Block > 0 ? $"Block: {equipment.Block} (uses: {equipment.Uses})" : string.Empty;
          if (type == EquipmentTooltipType.Shop) {
            return abilities + "\n\n" + blockAndUses;
          }
					return type == EquipmentTooltipType.Battle ? (equipment.Name + "\n\n" + abilities) : abilities;
				}
			return string.Empty;
		}
	}


  public enum EquipmentTooltipType
  {
    Battle,
    Shop,
  }
}