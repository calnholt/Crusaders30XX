using System.Collections.Generic;
using System.IO;

namespace Crusaders30XX.ECS.Data.Equipment
{
	public static class EquipmentDefinitionCache
	{
		private static Dictionary<string, EquipmentDefinition> _cache;
		private static string _folderPath;
		private static readonly object _lock = new object();

		public static bool TryGet(string id, out EquipmentDefinition def)
		{
			EnsureLoaded();
			return _cache.TryGetValue(id, out def);
		}

		public static Dictionary<string, EquipmentDefinition> GetAll()
		{
			EnsureLoaded();
			return _cache;
		}

		public static void Reload()
		{
			lock (_lock)
			{
				var folder = ResolveFolderPath();
				_cache = EquipmentDefinitionRepository.LoadFromFolder(folder);
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
					_cache = EquipmentDefinitionRepository.LoadFromFolder(folder);
				}
			}
		}

		private static string ResolveFolderPath()
		{
			if (!string.IsNullOrEmpty(_folderPath) && Directory.Exists(_folderPath)) return _folderPath;
			_folderPath = Path.Combine(System.AppContext.BaseDirectory, "Content", "Data", "Equipment");
			return _folderPath;
		}
	}
}


