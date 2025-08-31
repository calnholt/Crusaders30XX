using System.Collections.Generic;
using System.IO;

namespace Crusaders30XX.ECS.Data.Enemies
{
	public static class EnemyDefinitionCache
	{
		private static Dictionary<string, EnemyDefinition> _cache;
		private static string _folderPath;
		private static readonly object _lock = new object();

		public static Dictionary<string, EnemyDefinition> GetAll()
		{
			EnsureLoaded();
			return _cache;
		}

		public static bool TryGet(string id, out EnemyDefinition def)
		{
			EnsureLoaded();
			return _cache.TryGetValue(id, out def);
		}

		public static void Reload()
		{
			lock (_lock)
			{
				var folder = ResolveFolderPath();
				_cache = EnemyRepository.LoadFromFolder(folder);
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
					_cache = EnemyRepository.LoadFromFolder(folder);
				}
			}
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



