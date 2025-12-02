using System.IO;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Loadouts;

namespace Crusaders30XX.ECS.Data.Save
{
	public static class SaveCache
	{
		private static SaveFile _save;
		private static string _filePath;
		private static readonly object _lock = new object();

		public static SaveFile GetAll()
		{
			EnsureLoaded();
			return _save;
		}

		public static LoadoutDefinition GetLoadout(string id)
		{
			EnsureLoaded();
			if (_save == null || _save.loadouts == null) return null;
			return _save.loadouts.FirstOrDefault(l => l.id == id);
		}

		public static List<LoadoutDefinition> GetAllLoadouts()
		{
			EnsureLoaded();
			if (_save == null || _save.loadouts == null) return new List<LoadoutDefinition>();
			return _save.loadouts;
		}

		public static void SaveLoadout(LoadoutDefinition def)
		{
			if (def == null || string.IsNullOrEmpty(def.id)) return;
			lock (_lock)
			{
				EnsureLoaded();
				if (_save == null) _save = new SaveFile();
				if (_save.loadouts == null) _save.loadouts = new List<LoadoutDefinition>();
				
				var existing = _save.loadouts.FirstOrDefault(l => l.id == def.id);
				if (existing != null)
				{
					_save.loadouts.Remove(existing);
				}
				_save.loadouts.Add(def);
				Persist();
			}
		}

		public static HashSet<string> GetCollectionSet()
		{
			EnsureLoaded();
			if (_save == null || _save.collection == null || _save.collection.Count == 0)
			{
				return new HashSet<string>();
			}
			return new HashSet<string>(_save.collection);
		}

		public static int GetValueOrDefault(string locationId, int defaultValue = 0)
		{
			EnsureLoaded();
			if (_save == null || string.IsNullOrEmpty(locationId)) return defaultValue;
			
			if (!LocationDefinitionCache.TryGet(locationId, out var def) || def == null) return defaultValue;

			if (_save.completedQuests == null) return 0;
			
			int count = 0;
			if (def.pointsOfInterest != null)
			{
				foreach (var poi in def.pointsOfInterest)
				{
					if (poi != null && !string.IsNullOrEmpty(poi.id) && _save.completedQuests.Contains(poi.id))
					{
						count++;
					}
				}
			}
			return count;
		}

		public static int GetGold()
		{
			EnsureLoaded();
			return _save?.gold ?? 0;
		}

		public static void AddGold(int amount)
		{
			EnsureLoaded();
			if (amount <= 0) return;
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				long newValue = (_save.gold) + (long)amount;
				_save.gold = newValue < 0 ? 0 : (int)System.Math.Min(int.MaxValue, newValue);
				Persist();
			}
		}

		public static void Reload()
		{
			lock (_lock)
			{
				var path = ResolveFilePath();
				_save = SaveRepository.Load(path);
			}
		}

		public static bool IsQuestCompleted(string locationId, string questId)
		{
			EnsureLoaded();
			if (_save == null || string.IsNullOrEmpty(questId)) return false;
			return _save.completedQuests != null && _save.completedQuests.Contains(questId);
		}

		public static void SetQuestCompleted(string locationId, string questId, bool completed)
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				if (_save.completedQuests == null) _save.completedQuests = new List<string>();

				if (completed)
				{
					if (!_save.completedQuests.Contains(questId))
					{
						_save.completedQuests.Add(questId);
					}
				}
				else
				{
					if (_save.completedQuests.Contains(questId))
					{
						_save.completedQuests.Remove(questId);
					}
				}
				Persist();
			}
		}

		public static string GetLastLocation()
		{
			EnsureLoaded();
			return _save?.lastLocation ?? string.Empty;
		}

		public static void SetLastLocation(string locationId)
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				_save.lastLocation = locationId ?? string.Empty;
				Persist();
			}
		}

		private static void Persist()
		{
			try
			{
				var path = ResolveFilePath();
				SaveRepository.Save(path, _save);
			}
			catch { }
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
			if (!string.IsNullOrEmpty(_filePath)) return _filePath;
			var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			var saveDir = Path.Combine(appData, "Crusaders30XX");
			Directory.CreateDirectory(saveDir);
			_filePath = Path.Combine(saveDir, "save_file.json");
			return _filePath;
		}
	}
}
