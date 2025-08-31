using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Crusaders30XX.ECS.Data.Enemies
{
	public static class EnemyRepository
	{
		public static Dictionary<string, EnemyDefinition> LoadFromFolder(string folderAbsPath)
		{
			var map = new Dictionary<string, EnemyDefinition>();
			if (!Directory.Exists(folderAbsPath)) return map;
			var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			foreach (var file in Directory.GetFiles(folderAbsPath, "*.json"))
			{
				try
				{
					var json = File.ReadAllText(file);
					var root = JsonNode.Parse(json);
					if (root == null) continue;
					// Only consider files that have an 'id' and either enemy fields or 'attacks'
					var id = root["id"]?.GetValue<string>();
					var name = root["name"]?.GetValue<string>();
					var hpNode = root["hp"];
					if (string.IsNullOrEmpty(id)) continue;
					var def = new EnemyDefinition
					{
						id = id,
						name = name ?? id,
						hp = hpNode != null ? hpNode.GetValue<int>() : 1,
						attackIds = new List<string>()
					};
					// attacks: either an array of objects with 'id' or 'attackIds' array of strings
					if (root["attacks"] is JsonArray attacksArray)
					{
						foreach (var attackNode in attacksArray)
						{
							var attackId = attackNode?["id"]?.GetValue<string>();
							if (!string.IsNullOrEmpty(attackId)) def.attackIds.Add(attackId);
						}
					}
					else if (root["attackIds"] is JsonArray idsArray)
					{
						foreach (var idNode in idsArray)
						{
							var aid = idNode?.GetValue<string>();
							if (!string.IsNullOrEmpty(aid)) def.attackIds.Add(aid);
						}
					}
					map[def.id] = def;
				}
				catch (System.Exception ex)
				{
					System.Console.WriteLine($"[EnemyRepository] Failed to parse {file}: {ex.Message}");
				}
			}
			return map;
		}
	}
}



