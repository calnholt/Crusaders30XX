using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Crusaders30XX.ECS.Data.Save
{
	public static class SaveRepository
	{
		public static SaveFile Load(string fileAbsPath)
		{
			var result = new SaveFile();
			if (string.IsNullOrEmpty(fileAbsPath) || !File.Exists(fileAbsPath)) return result;
			try
			{
				var json = File.ReadAllText(fileAbsPath);
				var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var data = JsonSerializer.Deserialize<SaveFile>(json, opts) ?? new SaveFile();
				// Ensure non-null collections
				if (data.completedQuests == null) data.completedQuests = new System.Collections.Generic.List<string>();
				if (data.collection == null) data.collection = new System.Collections.Generic.List<string>();
				if (data.items == null) data.items = new System.Collections.Generic.List<SaveItem>();
				result = data;
			}
			catch (System.Exception ex)
			{
				System.Console.WriteLine($"[SaveRepository] Failed to parse {fileAbsPath}: {ex.Message}");
			}
			return result;
		}

		public static void Save(string fileAbsPath, SaveFile data)
		{
			if (string.IsNullOrEmpty(fileAbsPath) || data == null) return;
			try
			{
				var dir = Path.GetDirectoryName(fileAbsPath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
				var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(fileAbsPath, json);
			}
			catch (System.Exception ex)
			{
				System.Console.WriteLine($"[SaveRepository] Failed to write {fileAbsPath}: {ex.Message}");
			}
		}
	}
}
