using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Crusaders30XX.ECS.Data.Loadouts
{
	public static class LoadoutRepository
	{
		public static Dictionary<string, LoadoutDefinition> LoadFromFolder(string folderAbsPath)
		{
			var map = new Dictionary<string, LoadoutDefinition>();
			if (!Directory.Exists(folderAbsPath)) return map;
			var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			foreach (var file in Directory.GetFiles(folderAbsPath, "*.json"))
			{
				try
				{
					var json = File.ReadAllText(file);
					var def = JsonSerializer.Deserialize<LoadoutDefinition>(json, opts);
					if (def?.id != null) map[def.id] = def;
				}
				catch (System.Exception ex)
				{
					System.Console.WriteLine($"[LoadoutRepository] Failed to parse {file}: {ex.Message}");
				}
			}
			return map;
		}
	}
}



