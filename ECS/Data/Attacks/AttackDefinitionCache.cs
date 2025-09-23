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
				string root = FindProjectRootContaining("Crusaders30XX.csproj");
				if (string.IsNullOrEmpty(root)) return;
				string path = Path.Combine(root, "ECS", "Data", "Attacks", "generic_enemy_attacks.json");
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
			string root = FindProjectRootContaining("Crusaders30XX.csproj");
			_folderPath = string.IsNullOrEmpty(root) ? string.Empty : Path.Combine(root, "ECS", "Data", "Enemies");
			return _folderPath;
		}

		private static string FindProjectRootContaining(string filename)
		{
			try
			{
				var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
				for (int i = 0; i < 6 && dir != null; i++)
				{
					var candidate = Path.Combine(dir.FullName, filename);
					if (File.Exists(candidate)) return dir.FullName;
					dir = dir.Parent;
				}
			}
			catch { }
			return null;
		}
	}
}



