using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Crusaders30XX.ECS.Data.Medals
{
	public static class MedalDefinitionRepository
	{
		public static Dictionary<string, MedalDefinition> LoadFromFolder(string folderAbsPath)
		{
			var map = new Dictionary<string, MedalDefinition>();
			if (!Directory.Exists(folderAbsPath)) return map;
			var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			foreach (var file in Directory.GetFiles(folderAbsPath, "*.json"))
			{
				try
				{
					var json = File.ReadAllText(file);
					var def = JsonSerializer.Deserialize<MedalDefinition>(json, opts);
					if (def?.id != null) map[def.id] = def;
				}
				catch (System.Exception ex)
				{
					System.Console.WriteLine($"[MedalDefinitionRepository] Failed to parse {file}: {ex.Message}");
				}
			}
			return map;
		}
	}
}



