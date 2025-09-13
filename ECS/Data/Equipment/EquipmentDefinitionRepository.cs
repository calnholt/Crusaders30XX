using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Crusaders30XX.ECS.Data.Equipment
{
	public static class EquipmentDefinitionRepository
	{
		public static Dictionary<string, EquipmentDefinition> LoadFromFolder(string folderAbsPath)
		{
			var map = new Dictionary<string, EquipmentDefinition>();
			if (!Directory.Exists(folderAbsPath)) return map;
			var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			foreach (var file in Directory.GetFiles(folderAbsPath, "*.json"))
			{
				try
				{
					var json = File.ReadAllText(file);
					var def = JsonSerializer.Deserialize<EquipmentDefinition>(json, opts);
					// Ensure abilities know their owning equipment id
					if (def?.abilities != null)
					{
						foreach (var ability in def.abilities)
						{
							if (ability != null && string.IsNullOrEmpty(ability.equipmentId))
							{
								ability.equipmentId = def.id;
							}
						}
					}
					if (def?.id != null) map[def.id] = def;
				}
				catch (System.Exception ex)
				{
					System.Console.WriteLine($"[EquipmentDefinitionRepository] Failed to parse {file}: {ex.Message}");
				}
			}
			return map;
		}
	}
}


