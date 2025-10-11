using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Crusaders30XX.ECS.Data.Save
{
	public static class SaveRepository
	{
		public static Dictionary<string, int> Load(string fileAbsPath)
		{
			var map = new Dictionary<string, int>();
			if (string.IsNullOrEmpty(fileAbsPath) || !File.Exists(fileAbsPath)) return map;
			try
			{
				var json = File.ReadAllText(fileAbsPath);
				var node = JsonNode.Parse(json) as JsonObject;
				if (node == null) return map;
				foreach (var kv in node)
				{
					var key = kv.Key;
					try
					{
						int value = kv.Value?.GetValue<int>() ?? 0;
						map[key] = value;
					}
					catch { }
				}
			}
			catch (System.Exception ex)
			{
				System.Console.WriteLine($"[SaveRepository] Failed to parse {fileAbsPath}: {ex.Message}");
			}
			return map;
		}
	}
}


