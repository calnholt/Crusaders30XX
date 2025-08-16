using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Crusaders30XX.ECS.Data.Attacks
{
	public static class AttackRepository
	{
		public static Dictionary<string, AttackDefinition> LoadFromFolder(string folderAbsPath)
		{
			var map = new Dictionary<string, AttackDefinition>();
			if (!Directory.Exists(folderAbsPath)) return map;
			var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			foreach (var file in Directory.GetFiles(folderAbsPath, "*.json"))
			{
				try
				{
					var json = File.ReadAllText(file);
					var def = JsonSerializer.Deserialize<AttackDefinition>(json, opts);
					if (def?.id != null) map[def.id] = def;
				}
				catch (System.Exception ex)
				{
					System.Console.WriteLine($"[AttackRepository] Failed to parse {file}: {ex.Message}");
				}
			}
			return map;
		}
	}
}


