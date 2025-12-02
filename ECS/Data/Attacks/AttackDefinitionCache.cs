using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Crusaders30XX.ECS.Data.Attacks
{
	public static class AttackDefinitionCache
	{
		private static Dictionary<string, AttackDefinition> _cache;
		private static string _folderPath;
		private static readonly object _lock = new object();

		public static Dictionary<string, AttackDefinition> GetAll()
		{
			EnsureLoaded();
			return _cache;
		}

		public static bool TryGet(string id, out AttackDefinition def)
		{
			EnsureLoaded();
			return _cache.TryGetValue(id, out def);
		}

		public static void Reload()
		{
			lock (_lock)
			{
				var folder = ResolveFolderPath();
				_cache = AttackRepository.LoadFromFolder(folder);
				LoadGenericAttacksIntoCache();
			}
		}

		private static void EnsureLoaded()
		{
			if (_cache != null) return;
			lock (_lock)
			{
				if (_cache == null)
				{
					var folder = ResolveFolderPath();
					_cache = AttackRepository.LoadFromFolder(folder);
					LoadGenericAttacksIntoCache();
				}
			}
		}

		private static void LoadGenericAttacksIntoCache()
		{
			try
			{
				string path = Path.Combine(AppContext.BaseDirectory, "Content", "Data", "Enemies", "generic_enemy_attacks.json");
				if (!File.Exists(path)) return;
				var json = File.ReadAllText(path);
				var node = JsonNode.Parse(json);
				if (node == null) return;
				if (node["attacks"] is JsonArray arr)
				{
					var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
					foreach (var attackNode in arr)
					{
						if (attackNode == null) continue;
						var def = attackNode.Deserialize<AttackDefinition>(opts);
						if (def?.id != null) _cache[def.id] = def;
					}
				}
			}
			catch { }
		}

		private static string ResolveFolderPath()
		{
			if (!string.IsNullOrEmpty(_folderPath) && Directory.Exists(_folderPath)) return _folderPath;
			_folderPath = Path.Combine(AppContext.BaseDirectory, "Content", "Data", "Enemies");
			return _folderPath;
		}
	}
}



