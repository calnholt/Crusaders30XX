using System.IO;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Objects.Equipment;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;

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
			EnsureRunMap();
			if (_save?.runMapNodes == null) return defaultValue;
			int count = 0;
			foreach (var node in _save.runMapNodes)
			{
				if (node != null && node.isCompleted) count++;
			}
			return count;
		}

		public static void EnsureRunMap()
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				if (_save.runMapNodes != null && _save.runMapNodes.Count > 0)
				{
					if (SanitizeRunMapEnemyIds())
					{
						Persist();
					}
					return;
				}
				var (seed, nodes) = LocationMapGeneratorService.Generate();
				_save.runMapSeed = seed;
				_save.runMapNodes = nodes;
				if (string.IsNullOrEmpty(_save.lastLocation) && nodes.Count > 0)
				{
					_save.lastLocation = nodes[0].id;
				}
				Persist();
			}
		}

		public static IReadOnlyList<RunMapNode> GetRunMapNodes()
		{
			EnsureLoaded();
			EnsureRunMap();
			return _save?.runMapNodes ?? new List<RunMapNode>();
		}

		public static int RunMapNodeCount => LocationMapConstants.NodeCount;

		public static string GetStartNodeId()
		{
			EnsureRunMap();
			if (_save?.runMapNodes == null || _save.runMapNodes.Count == 0) return "run_0";
			return _save.runMapNodes[0].id;
		}

		public static bool TryGetRunNode(string nodeId, out RunMapNode node, out int index)
		{
			node = null;
			index = -1;
			EnsureRunMap();
			if (_save?.runMapNodes == null || string.IsNullOrEmpty(nodeId)) return false;
			for (int i = 0; i < _save.runMapNodes.Count; i++)
			{
				var n = _save.runMapNodes[i];
				if (n != null && string.Equals(n.id, nodeId, System.StringComparison.OrdinalIgnoreCase))
				{
					node = n;
					index = i;
					return true;
				}
			}
			return false;
		}

		public static bool TryGetRunNode(int index, out RunMapNode node)
		{
			node = null;
			EnsureRunMap();
			if (_save?.runMapNodes == null || index < 0 || index >= _save.runMapNodes.Count) return false;
			node = _save.runMapNodes[index];
			return node != null;
		}

		public static void SetRunNodeRevealed(string nodeId, bool revealed = true)
		{
			if (!TryGetRunNode(nodeId, out var node, out _)) return;
			lock (_lock)
			{
				node.isRevealed = revealed;
				Persist();
			}
		}

		public static void SetRunNodeCompleted(string nodeId, bool completed = true)
		{
			if (!TryGetRunNode(nodeId, out var node, out _)) return;
			lock (_lock)
			{
				node.isCompleted = completed;
				Persist();
			}
		}

		private static bool SanitizeRunMapEnemyIds()
		{
			if (_save?.runMapNodes == null || _save.runMapNodes.Count == 0) return false;
			var pool = EnemyPortraitContent.GetRunMapEnemyPool().ToList();
			if (pool.Count == 0) return false;
			var rng = new System.Random(_save.runMapSeed != 0 ? _save.runMapSeed : 1);
			bool changed = false;
			foreach (var node in _save.runMapNodes)
			{
				if (node == null || string.IsNullOrEmpty(node.enemyId)) continue;
				if (EnemyPortraitContent.HasPortrait(node.enemyId)) continue;
				node.enemyId = pool[rng.Next(pool.Count)];
				changed = true;
			}
			return changed;
		}

		public static void RevealRunNodeChildren(string completedNodeId)
		{
			if (!TryGetRunNode(completedNodeId, out var node, out _)) return;
			if (node.childIndices == null || node.childIndices.Count == 0) return;
			lock (_lock)
			{
				foreach (int childIndex in node.childIndices)
				{
					if (childIndex >= 0 && childIndex < _save.runMapNodes.Count)
					{
						_save.runMapNodes[childIndex].isRevealed = true;
					}
				}
				Persist();
			}
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
			EnsureRunMap();
			if (string.IsNullOrEmpty(questId)) return false;
			return TryGetRunNode(questId, out var node, out _) && node.isCompleted;
		}

		public static void SetQuestCompleted(string locationId, string questId, bool completed)
		{
			SetRunNodeCompleted(questId, completed);
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
						if (_save.version < SaveFile.CURRENT_VERSION)
						{
							System.Console.WriteLine($"[SaveCache] Version mismatch (found {_save.version}, expected {SaveFile.CURRENT_VERSION}). Resetting save file.");
							_save = CreateDefaultSave();
							Persist();
						}
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
							if (_save.version < SaveFile.CURRENT_VERSION)
							{
								System.Console.WriteLine($"[SaveCache] Legacy version mismatch (found {_save.version}, expected {SaveFile.CURRENT_VERSION}). Resetting save file.");
								_save = CreateDefaultSave();
							}
							Persist();
						}
						else
						{
							// Create default save file for new users
							_save = CreateDefaultSave();
							Persist();
						}
					}
					EnsureRunMap();
				}
			}
		}

		private static SaveFile CreateDefaultSave()
		{
			var (seed, nodes) = LocationMapGeneratorService.Generate();
			var save = new SaveFile
			{
				version = SaveFile.CURRENT_VERSION,
				gold = 4,
				runMapSeed = seed,
				runMapNodes = nodes,
				collection = new List<string>
				{
					// Starter cards everyone has
					"anoint_the_sick",
					"smite",
					"fervor",
					"courageous",
					"reckoning",
					"absolution",
					"increase_faith",
					"litany_of_wrath",
					"exaltation",
					"seize",
					"shield_of_faith",
					"stab",
					"tempest",
					"sword",
					"razor_storm",
					"hold_the_line",

					// Temperance abilities
					"angelic_aura",
					"fling_fling",
					"radiance",
				},
				items = new List<SaveItem>(),
				lastLocation = nodes.Count > 0 ? nodes[0].id : "run_0",
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
							"smite|Red",
							"smite|White",
							"fervor|Red",
							"fervor|White",
							"courageous|Black",
							"courageous|White",
							"reckoning|Black",
							"reckoning|Red",
							"razor_storm|Red",
							"razor_storm|Black",
							"absolution|Red",
							"absolution|White",
							"increase_faith|Red",
							"increase_faith|White",
							"litany_of_wrath|White",
							"litany_of_wrath|Red",
							"exaltation|Black",
							"exaltation|White",
							"seize|Red",
							"seize|White",
							"stab|White", 
							"stab|Red",
							"hold_the_line|White", 
							"hold_the_line|Red",
							"shield_of_faith|Red",
							"shield_of_faith|Black",
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
			return save;
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

		public static void AddToCollectionIfMissing(string itemId)
		{
			if (string.IsNullOrWhiteSpace(itemId)) return;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				if (_save.collection == null) _save.collection = new List<string>();
				if (!_save.collection.Contains(itemId))
				{
					_save.collection.Add(itemId);
					Persist();
				}
			}
		}

		public static bool AddCardToLoadout(string loadoutId, string cardKey)
		{
			if (string.IsNullOrWhiteSpace(loadoutId) || string.IsNullOrWhiteSpace(cardKey)) return false;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				if (_save.loadouts == null) _save.loadouts = new List<LoadoutDefinition>();

				var loadout = _save.loadouts.FirstOrDefault(l => l.id == loadoutId);
				if (loadout == null)
				{
					loadout = new LoadoutDefinition { id = loadoutId, name = loadoutId };
					_save.loadouts.Add(loadout);
				}
				if (loadout.cardIds == null) loadout.cardIds = new List<string>();
				loadout.cardIds.Add(cardKey);
				Persist();
				return true;
			}
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

		private const int PointsToMaster = 50;

		public static CardMastery GetMasteryData(string cardId)
		{
			if (string.IsNullOrEmpty(cardId)) return null;
			EnsureLoaded();
			if (_save == null || _save.cardMastery == null) return null;
			_save.cardMastery.TryGetValue(cardId, out var mastery);
			return mastery;
		}

		public static void AddMasteryPoints(string cardId, int points)
		{
			if (string.IsNullOrEmpty(cardId) || points <= 0) return;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				if (_save.cardMastery == null) _save.cardMastery = new Dictionary<string, CardMastery>();

				if (!_save.cardMastery.TryGetValue(cardId, out var mastery))
				{
					mastery = new CardMastery { cardId = cardId, level = 0, points = 0 };
					_save.cardMastery[cardId] = mastery;
				}

				int oldLevel = mastery.level;
				mastery.points += points;

				// Check for level up
				while (mastery.points >= PointsToMaster)
				{
					mastery.points -= PointsToMaster;
					mastery.level++;
				}

				Persist();

				// Publish event if leveled up
				if (mastery.level > oldLevel)
				{
					Crusaders30XX.ECS.Core.EventManager.Publish(new Crusaders30XX.ECS.Events.CardMasteredEvent
					{
						CardId = cardId,
						Level = mastery.level
					});
				}
			}
		}

		/// <summary>
		/// Persist achievement data to disk.
		/// Called by AchievementManager when progress is updated.
		/// </summary>
		public static void PersistAchievements()
		{
			lock (_lock)
			{
				Persist();
			}
		}
	}
}
