using System.IO;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Objects.Equipment;
using Crusaders30XX.ECS.Factories;

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

		private static void Persist()
		{
			try
			{
				var path = ResolveFilePath();
				if (string.IsNullOrEmpty(path)) return;
				SaveRepository.Save(path, _save);
			}
			catch { }
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

		private static void EnsureLoaded()
		{
			if (_save != null) return;
			lock (_lock)
			{
				if (_save == null)
				{
					var path = ResolveFilePath();
					if (!string.IsNullOrEmpty(path) && File.Exists(path))
					{
						_save = SaveRepository.Load(path);
					}
					else
					{
						// Optional migration: if no LocalApplicationData save exists yet,
						// but a legacy project save file does, load that and persist it
						// to the new location so existing progress is preserved.
						string legacyPath = ResolveLegacyFilePath();
						if (!string.IsNullOrEmpty(legacyPath) && File.Exists(legacyPath))
						{
							_save = SaveRepository.Load(legacyPath) ?? CreateDefaultSave();
							Persist();
						}
						else
						{
							// Create default save file for new users
							_save = CreateDefaultSave();
							Persist();
						}
					}
				}
			}
		}

		private static SaveFile CreateDefaultSave()
		{
			return new SaveFile
			{
				gold = 0,
				completedQuests = new List<string>(),
				collection = new List<string>
				{
					// Starter cards everyone has
					"anoint_the_sick",
					"burn",
					"carve",
					"courageous",
					"deus_vult",
					"dowse_with_holy_water",
					"increase_faith",
					"reap",
					"serpent_crush",
					"seize",
					"stab",
					"strike", 
					"stun",
					"tempest",
					"sword",
					"razor_storm",
				},
				items = new List<SaveItem>(),
				lastLocation = "desert_1",
				loadouts = new List<LoadoutDefinition>
				{
					new LoadoutDefinition
					{
						id = "loadout_1",
						name = "Deck",
						cardIds = new List<string>
						{
							"anoint_the_sick|Black",
							"anoint_the_sick|White",
							"burn|Red",
							"burn|White",
							"carve|Red",
							"carve|White",
							"courageous|Black",
							"courageous|White",
							"deus_vult|Black",
							"deus_vult|Red",
							"razor_storm|Red",
							"razor_storm|Black",
							"dowse_with_holy_water|Red",
							"dowse_with_holy_water|White",
							"increase_faith|Red",
							"increase_faith|White",
							"reap|White",
							"reap|Red",
							"serpent_crush|Black",
							"serpent_crush|White",
							"seize|Red",
							"seize|White",
							"stab|White", 
							"stab|Red",
							"strike|White", 
							"strike|Red",
							"stun|Red",
							"stun|Black",
							"tempest|Black",
							"tempest|White",
						},
						weaponId = "sword",
						temperanceId = "angelic_aura",
						chestId = "",
						legsId = "",
						armsId = "",
						headId = "",
						medalIds = new List<string>
						{

						}
					}
				}
			};
		}

		private static string ResolveFilePath()
		{
			if (!string.IsNullOrEmpty(_filePath)) return _filePath;
			try
			{
				var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
				if (string.IsNullOrEmpty(appData)) return string.Empty;
				var saveDir = Path.Combine(appData, "Crusaders30XX");
				if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);
				_filePath = Path.Combine(saveDir, "save_file.json");
				return _filePath;
			}
			catch
			{
				return string.Empty;
			}
		}

		/// <summary>
		/// Legacy project-relative save path used by older builds.
		/// Kept for one-time migration into the LocalApplicationData location.
		/// </summary>
		private static string ResolveLegacyFilePath()
		{
			try
			{
				string root = FindProjectRootContaining("Crusaders30XX.csproj");
				if (string.IsNullOrEmpty(root)) return string.Empty;
				return Path.Combine(root, "ECS", "Data", "save_file.json");
			}
			catch
			{
				return string.Empty;
			}
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

		public static bool TrySpendGoldAndAddToCollection(string itemId, int price, ForSaleItemType itemType, out int newGold)
		{
			newGold = 0;
			if (price < 0) price = 0;

			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null)
				{
					return false;
				}

				if (_save.gold < price)
				{
					return false;
				}

				_save.gold = System.Math.Max(0, _save.gold - price);
				if (_save.collection == null)
				{
					_save.collection = new System.Collections.Generic.List<string>();
				}

				if (!string.IsNullOrWhiteSpace(itemId) && !_save.collection.Contains(itemId))
				{
					_save.collection.Add(itemId);
					if (itemType == ForSaleItemType.Medal && _save.loadouts[0].medalIds.Count < 3)
					{
						_save.loadouts[0].medalIds.Add(itemId);
					}
					if (itemType == ForSaleItemType.Equipment)
					{
						EquipmentBase equipment = EquipmentFactory.Create(itemId);
						switch (equipment.Slot)
						{
							case EquipmentSlot.Chest:
								if (string.IsNullOrEmpty(_save.loadouts[0].chestId))
								{
									_save.loadouts[0].chestId = itemId;
								}
								break;
							case EquipmentSlot.Legs:
								if (string.IsNullOrEmpty(_save.loadouts[0].legsId))
								{
									_save.loadouts[0].legsId = itemId;
								}
								break;
							case EquipmentSlot.Arms:
								if (string.IsNullOrEmpty(_save.loadouts[0].armsId))
								{
									_save.loadouts[0].armsId = itemId;
								}
								break;
							case EquipmentSlot.Head:
								if (string.IsNullOrEmpty(_save.loadouts[0].headId))
								{
									_save.loadouts[0].headId = itemId;
								}
								break;
						}
					}
				}

				Persist();
				newGold = _save.gold;
				return true;
			}
		}

		public static bool HasSeenTutorial(string key)
		{
			if (string.IsNullOrEmpty(key)) return false;
			EnsureLoaded();
			if (_save == null || _save.seenTutorials == null) return false;
			return _save.seenTutorials.Contains(key);
		}

		public static void MarkTutorialSeen(string key)
		{
			if (string.IsNullOrEmpty(key)) return;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				if (_save.seenTutorials == null) _save.seenTutorials = new List<string>();
				if (!_save.seenTutorials.Contains(key))
				{
					_save.seenTutorials.Add(key);
					Persist();
				}
			}
		}
	}
}
