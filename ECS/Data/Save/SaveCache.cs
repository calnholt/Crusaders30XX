using System.IO;
using System.Linq;
using System.Collections.Generic;

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
			var loc = _save.locations?.FirstOrDefault(l => l != null && l.id == locationId);
			if (loc == null || loc.events == null) return defaultValue;
			return loc.events.Count(e => e != null && e.completed);
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
			if (_save == null || string.IsNullOrEmpty(locationId) || string.IsNullOrEmpty(questId)) return false;
			var loc = _save.locations?.FirstOrDefault(l => l != null && l.id == locationId);
			var q = loc?.events?.FirstOrDefault(e => e != null && e.id == questId);
			return q?.completed ?? false;
		}

		public static void SetQuestCompleted(string locationId, string questId, bool completed)
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				if (_save.locations == null) _save.locations = new System.Collections.Generic.List<SaveLocation>();
				var loc = _save.locations.FirstOrDefault(l => l != null && l.id == locationId);
				if (loc == null)
				{
					loc = new SaveLocation { id = locationId, events = new System.Collections.Generic.List<SaveQuest>() };
					_save.locations.Add(loc);
				}
				if (loc.events == null) loc.events = new System.Collections.Generic.List<SaveQuest>();
				var quest = loc.events.FirstOrDefault(e => e != null && e.id == questId);
				if (quest == null)
				{
					quest = new SaveQuest { id = questId, completed = completed };
					loc.events.Add(quest);
				}
				else
				{
					quest.completed = completed;
				}
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


