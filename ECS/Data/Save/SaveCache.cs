using System.Collections.Generic;
using System.IO;

namespace Crusaders30XX.ECS.Data.Save
{
	public static class SaveCache
	{
		private static Dictionary<string, int> _save;
		private static string _filePath;
		private static readonly object _lock = new object();

		public static Dictionary<string, int> GetAll()
		{
			EnsureLoaded();
			return _save;
		}

		public static int GetValueOrDefault(string key, int defaultValue = 0)
		{
			EnsureLoaded();
			return _save != null && _save.TryGetValue(key, out var v) ? v : defaultValue;
		}

		public static void Reload()
		{
			lock (_lock)
			{
				var path = ResolveFilePath();
				_save = SaveRepository.Load(path);
			}
		}

		private static void EnsureLoaded()
		{
			if (_save != null) return;
			lock (_lock)
			{
				if (_save == null)
				{
					var path = ResolveFilePath();
					_save = SaveRepository.Load(path);
				}
			}
		}

		private static string ResolveFilePath()
		{
			if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath)) return _filePath;
			string root = FindProjectRootContaining("Crusaders30XX.csproj");
			_filePath = string.IsNullOrEmpty(root) ? string.Empty : Path.Combine(root, "ECS", "Data", "save_file.json");
			return _filePath;
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


