using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

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
					// Detect whether this file is a single AttackDefinition or an Enemy file with an 'attacks' array
					var root = JsonNode.Parse(json);
					if (root == null) continue;
					if (root["attacks"] is JsonArray attacksArray)
					{
						foreach (var attackNode in attacksArray)
						{
							if (attackNode == null) continue;
							var def = attackNode.Deserialize<AttackDefinition>(opts);
							if (def?.id != null) map[def.id] = def;
						}
					}
					else
					{
						var def = JsonSerializer.Deserialize<AttackDefinition>(json, opts);
						if (def?.id != null) map[def.id] = def;
					}
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


