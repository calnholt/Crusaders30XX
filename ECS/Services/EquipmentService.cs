using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Equipment;

namespace Crusaders30XX.ECS.Services
{
	public static class EquipmentService
	{
		public static string GetTooltipText(string equipmentId, EquipmentTooltipType type = EquipmentTooltipType.Battle)
		{
			if (EquipmentDefinitionCache.TryGet(equipmentId, out var def) && def != null)
				{
					var parts = new List<string>();
					if (def.abilities != null)
					{
						foreach (var a in def.abilities)
						{
							string text = string.Empty;
							if (!string.IsNullOrWhiteSpace(a.text)) {
								if (a.type == "Activate") {
									text += $"Activate ({(a.isFreeAction ? "free action" : "1AP")}): ";
								}
							}
							text += a.text;
							if (a.requiresUseOnActivate)
							{
								text += " Lose one use.";
							}
							if (a.destroyOnActivate)
							{
								text += " Destroy this.";
							}
							parts.Add(text);
						}
					}
					string abilities = string.Join("\n", parts);
          string blockAndUses = def.block > 0 ? $"Block: {def.block} (uses: {def.blockUses})" : string.Empty;
          if (type == EquipmentTooltipType.Shop) {
            return abilities + "\n\n" + blockAndUses;
          }
					return type == EquipmentTooltipType.Battle ? (def.name + "\n\n" + abilities) : abilities;
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