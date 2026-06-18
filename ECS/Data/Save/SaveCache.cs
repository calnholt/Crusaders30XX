using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Objects.Equipment;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Tutorials;
using Crusaders30XX.Diagnostics;

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

		public static void ConfigurePrimaryRunSetup(
			string weaponId,
			string temperanceId)
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = CreateDefaultRunSave();
				EnsureRunMap();
				EnsurePrimaryLoadout(_save);

				var resolvedWeaponId = string.IsNullOrWhiteSpace(weaponId) ? "sword" : weaponId;
				var loadout = StartingDeckGeneratorService.BuildStartingLoadout(
					resolvedWeaponId,
					_save.runMapSeed,
					"loadout_1");

				_save.starterCardKeys = new List<string>(loadout.cardIds);
				_save.tradedCardKeys = new List<string>();
				var savedLoadout = _save.loadouts[0];
				savedLoadout.cardIds = new List<string>(loadout.cardIds);
				savedLoadout.weaponId = resolvedWeaponId;
				savedLoadout.temperanceId = string.IsNullOrWhiteSpace(temperanceId)
					? loadout.temperanceId
					: temperanceId;
				if (string.IsNullOrWhiteSpace(savedLoadout.name)) savedLoadout.name = "Deck";
				if (string.IsNullOrWhiteSpace(savedLoadout.id)) savedLoadout.id = "loadout_1";
				savedLoadout.chestId ??= string.Empty;
				savedLoadout.legsId ??= string.Empty;
				savedLoadout.armsId ??= string.Empty;
				savedLoadout.headId ??= string.Empty;
				savedLoadout.medalIds ??= new List<string>();

				_save.climb = ClimbRuleService.CreateInitialState(_save.runMapSeed, savedLoadout);
				Persist();
			}
		}

		public static HashSet<string> GetOwnedCardIds()
		{
			EnsureLoaded();
			var loadout = GetLoadout("loadout_1");
			if (loadout?.cardIds == null || loadout.cardIds.Count == 0)
			{
				return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			}
			var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var key in loadout.cardIds)
			{
				string baseId = DeckRules.ParseBaseCardId(key);
				if (!string.IsNullOrEmpty(baseId)) ids.Add(baseId);
			}
			return ids;
		}

		public static bool IsCardOwned(string cardId)
		{
			if (string.IsNullOrWhiteSpace(cardId)) return false;
			return GetOwnedCardIds().Contains(cardId);
		}

		public static bool IsItemOwned(string itemId, ForSaleItemType itemType)
		{
			if (string.IsNullOrWhiteSpace(itemId)) return false;
			EnsureLoaded();
			var loadout = GetLoadout("loadout_1");
			if (loadout == null) return false;

			switch (itemType)
			{
				case ForSaleItemType.Card:
					return IsCardOwned(itemId);
				case ForSaleItemType.Weapon:
					return string.Equals(loadout.weaponId, itemId, StringComparison.OrdinalIgnoreCase);
				case ForSaleItemType.Medal:
					return loadout.medalIds != null && loadout.medalIds.Any(m => string.Equals(m, itemId, StringComparison.OrdinalIgnoreCase));
				case ForSaleItemType.Equipment:
					return string.Equals(loadout.headId, itemId, StringComparison.OrdinalIgnoreCase)
						|| string.Equals(loadout.chestId, itemId, StringComparison.OrdinalIgnoreCase)
						|| string.Equals(loadout.armsId, itemId, StringComparison.OrdinalIgnoreCase)
						|| string.Equals(loadout.legsId, itemId, StringComparison.OrdinalIgnoreCase);
				default:
					return false;
			}
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
				if (!_save.isRunActive) return;
				if (_save.runMapNodes != null && _save.runMapNodes.Count > 0)
				{
					if (SanitizeRunMapEnemyIds())
					{
						Persist();
					}
					return;
				}
				var (seed, nodes, shops, treasures, events) = GenerateRunMapForSave();
				_save.runMapSeed = seed;
				_save.runMapNodes = nodes;
				_save.runMapShops = shops;
				_save.runMapTreasures = treasures;
				_save.runMapEvents = events;
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
			if (_save?.isRunActive != true) return new List<RunMapNode>();
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
				if (node == null) continue;
				if (!string.IsNullOrEmpty(node.enemyId) && !EnemyPortraitContent.HasPortrait(node.enemyId))
				{
					node.enemyId = pool[rng.Next(pool.Count)];
					changed = true;
				}

				if (node.battleEnemyIds == null || node.battleEnemyIds.Count == 0) continue;
				for (int i = 0; i < node.battleEnemyIds.Count; i++)
				{
					string id = node.battleEnemyIds[i];
					if (string.IsNullOrEmpty(id) || EnemyPortraitContent.HasPortrait(id)) continue;
					node.battleEnemyIds[i] = pool[rng.Next(pool.Count)];
					changed = true;
				}
			}
			return changed;
		}

		public static bool TryRevealRunNode(string nodeId)
		{
			if (!TryGetRunNode(nodeId, out var node, out _)) return false;
			if (node.isRevealed) return false;
			lock (_lock)
			{
				node.isRevealed = true;
				Persist();
			}
			return true;
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

		public static bool IsStarterCardKey(string cardKey)
		{
			if (string.IsNullOrWhiteSpace(cardKey)) return false;
			EnsureLoaded();
			var keys = _save?.starterCardKeys;
			if (keys == null || keys.Count == 0) return false;
			return keys.Any(k => string.Equals(k, cardKey, StringComparison.OrdinalIgnoreCase));
		}

		public static void RemoveStarterCardKey(string cardKey)
		{
			if (string.IsNullOrWhiteSpace(cardKey)) return;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save?.starterCardKeys == null) return;
				int idx = _save.starterCardKeys.FindIndex(k =>
					string.Equals(k, cardKey, StringComparison.OrdinalIgnoreCase));
				if (idx < 0) return;
				_save.starterCardKeys.RemoveAt(idx);
				Persist();
			}
		}

		public static List<string> GetTradedCardKeys()
		{
			EnsureLoaded();
			lock (_lock)
			{
				return CloneStringList(_save?.tradedCardKeys);
			}
		}

		public static void MarkTradedCardKey(string cardKey)
		{
			if (string.IsNullOrWhiteSpace(cardKey)) return;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				_save.tradedCardKeys ??= new List<string>();
				_save.tradedCardKeys.Add(cardKey);
				Persist();
			}
		}

		public static bool ReplaceTrackedTradedCardKey(string oldCardKey, string newCardKey)
		{
			if (string.IsNullOrWhiteSpace(oldCardKey) || string.IsNullOrWhiteSpace(newCardKey)) return false;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				_save.tradedCardKeys ??= new List<string>();
				int idx = _save.tradedCardKeys.FindIndex(k =>
					string.Equals(k, oldCardKey, StringComparison.OrdinalIgnoreCase));
				if (idx < 0) return false;
				_save.tradedCardKeys[idx] = newCardKey;
				Persist();
				return true;
			}
		}

		public static void RemoveTrackedTradedCardKey(string cardKey)
		{
			if (string.IsNullOrWhiteSpace(cardKey)) return;
			EnsureLoaded();
			lock (_lock)
			{
				if (!RemoveTrackedTradedCardKeyLocked(cardKey)) return;
				Persist();
			}
		}

		private static bool RemoveTrackedTradedCardKeyLocked(string cardKey)
		{
			if (string.IsNullOrWhiteSpace(cardKey) || _save?.tradedCardKeys == null) return false;
			int idx = _save.tradedCardKeys.FindIndex(k =>
				string.Equals(k, cardKey, StringComparison.OrdinalIgnoreCase));
			if (idx < 0) return false;
			_save.tradedCardKeys.RemoveAt(idx);
			return true;
		}

		public static void Reload()
		{
			lock (_lock)
			{
				var path = ResolveFilePath();
				_save = SaveRepository.Load(path);
				ApplyVersionPolicy(persist: true);
			}
		}

		public static bool IsQuestCompleted(string locationId, string questId)
		{
			EnsureRunMap();
			if (string.IsNullOrEmpty(questId)) return false;
			return TryGetRunNode(questId, out var node, out _) && node.isCompleted;
		}

		public static bool IsStartQuestCompleted()
		{
			return IsQuestCompleted(null, GetStartNodeId());
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

		public static void SetPendingBattleNode(string nodeId)
		{
			if (string.IsNullOrEmpty(nodeId)) return;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				_save.pendingBattleNodeId = nodeId;
				Persist();
			}
		}

		public static void ClearPendingBattle()
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) return;
				if (string.IsNullOrEmpty(_save.pendingBattleNodeId)) return;
				_save.pendingBattleNodeId = string.Empty;
				Persist();
			}
		}

		public static bool TryGetResumableBattleNode(out string nodeId)
		{
			nodeId = string.Empty;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) return false;
				nodeId = _save.pendingBattleNodeId ?? string.Empty;
			}

			if (string.IsNullOrEmpty(nodeId)) return false;
			if (!TryGetRunNode(nodeId, out var node, out _))
			{
				ClearPendingBattle();
				return false;
			}

			if (!node.isRevealed || node.isCompleted || node.ResolveBattleEnemyIds().Count == 0)
			{
				ClearPendingBattle();
				return false;
			}

			return true;
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
						ApplyVersionPolicy(persist: true);
					}
					else
					{
						// Optional migration: if no LocalApplicationData save exists yet,
						// but a legacy project save file does, load that and persist it
						// to the new location so existing progress is preserved.
						string legacyPath = ResolveLegacyFilePath();
						if (!string.IsNullOrEmpty(legacyPath) && File.Exists(legacyPath))
						{
							_save = SaveRepository.Load(legacyPath) ?? CreateInactiveSavePreservingMeta(null);
							ApplyVersionPolicy(persist: true);
						}
						else
						{
							// New profiles start inactive; WayStation Depart creates the first active run.
							_save = CreateInactiveSavePreservingMeta(null);
							Persist();
						}
					}
					EnsureRunMap();
				}
			}
		}

		public static void StartNewRun()
		{
			EnsureLoaded();
			lock (_lock)
			{
				_save = CreateFreshRunPreservingMeta(_save);
				_save.isRunActive = true;
				Persist();
				CardUsageTelemetryRuntime.StartNewRun(_save.runMapSeed);
			}
		}

		public static bool IsRunActive()
		{
			EnsureLoaded();
			return _save?.isRunActive == true;
		}

		public static void MarkRunInactive()
		{
			EnsureLoaded();
			lock (_lock)
			{
				_save = CreateInactiveSavePreservingMeta(_save);
				Persist();
			}
		}

		/// <summary>
		/// Removes on-disk save files (primary and legacy locations) and clears the in-memory cache.
		/// The next <see cref="EnsureLoaded"/> creates a fresh default save.
		/// </summary>
		public static void DeleteSaveFilesIfPresent()
		{
			lock (_lock)
			{
				_save = null;
				bool deletedAny = false;
				foreach (string path in EnumerateSaveFilePaths())
				{
					if (!File.Exists(path)) continue;
					File.Delete(path);
					System.Console.WriteLine($"[SaveCache] Deleted save file: {path}");
					deletedAny = true;
				}
				if (!deletedAny)
				{
					System.Console.WriteLine("[SaveCache] No save file found to delete.");
				}
			}
		}

		/// <summary>
		/// Any save whose version is not CURRENT_VERSION is discarded and replaced with a new default save.
		/// No migration between versions.
		/// </summary>
		private static void ApplyVersionPolicy(bool persist)
		{
			if (_save == null || _save.version != SaveFile.CURRENT_VERSION)
			{
				int found = _save?.version ?? 0;
				System.Console.WriteLine($"[SaveCache] Save version {found} != {SaveFile.CURRENT_VERSION}; creating a new save file.");
				_save = CreateInactiveSavePreservingMeta(null);
				if (persist) Persist();
			}
		}

		private static SaveFile CreateFreshRunPreservingMeta(SaveFile prior)
		{
			var mastery = prior?.cardMastery;
			var achievements = prior?.achievements;
			var seenTutorials = prior?.seenTutorials;
			var save = CreateDefaultRunSave();
			save.cardMastery = mastery ?? new Dictionary<string, CardMastery>();
			save.achievements = achievements ?? new Dictionary<string, AchievementProgress>();
			save.seenTutorials = seenTutorials ?? new List<string>();
			save.guidedTutorialCompleted = prior?.guidedTutorialCompleted == true;
			return save;
		}

		private static SaveFile CreateInactiveSavePreservingMeta(SaveFile prior)
		{
			return new SaveFile
			{
				version = SaveFile.CURRENT_VERSION,
				isRunActive = false,
				gold = 0,
				runMapSeed = 0,
				runMapNodes = new List<RunMapNode>(),
				runMapShops = new List<RunMapShop>(),
				runMapTreasures = new List<RunMapTreasure>(),
				runMapEvents = new List<RunMapEvent>(),
				items = new List<SaveItem>(),
				lastLocation = string.Empty,
				pendingBattleNodeId = string.Empty,
				loadouts = new List<LoadoutDefinition>(),
				runLongPassives = new Dictionary<string, int>(),
				runCardRestrictions = new Dictionary<string, List<string>>(),
				starterCardKeys = new List<string>(),
				tradedCardKeys = new List<string>(),
				pendingDeckRewardOffer = null,
				climb = new ClimbSaveState(),
				cardMastery = prior?.cardMastery ?? new Dictionary<string, CardMastery>(),
				achievements = prior?.achievements ?? new Dictionary<string, AchievementProgress>(),
				seenTutorials = prior?.seenTutorials ?? new List<string>(),
				guidedTutorialCompleted = prior?.guidedTutorialCompleted == true,
			};
		}

		private static SaveFile CreateDefaultRunSave()
		{
			var (seed, nodes, shops, treasures, events) = GenerateRunMapForSave();
			var startingDeck = StartingDeckGeneratorService.Generate(
				StartingDeckGeneratorService.DefaultStarterCardPool,
				seed);
			var save = new SaveFile
			{
				version = SaveFile.CURRENT_VERSION,
				isRunActive = true,
				gold = 4,
				runMapSeed = seed,
				runMapNodes = nodes,
				runMapShops = shops,
				runMapTreasures = treasures,
				runMapEvents = events,
				items = new List<SaveItem>(),
				lastLocation = nodes.Count > 0 ? nodes[0].id : "run_0",
				pendingBattleNodeId = string.Empty,
				pendingDeckRewardOffer = null,
				starterCardKeys = new List<string>(startingDeck),
				tradedCardKeys = new List<string>(),
				loadouts = new List<LoadoutDefinition>
				{
					new LoadoutDefinition
					{
						id = "loadout_1",
						name = "Deck",
						cardIds = startingDeck,
						weaponId = "sword",
						temperanceId = "angelic_aura",
						chestId = "",
						legsId = "",
						armsId = "",
						headId = "",
						medalIds = new List<string>()
					}
				}
			};
			save.climb = ClimbRuleService.CreateInitialState(seed, save.loadouts[0]);
			return save;
		}

		private static void EnsurePrimaryLoadout(SaveFile save)
		{
			if (save.loadouts == null) save.loadouts = new List<LoadoutDefinition>();
			var loadout = save.loadouts.FirstOrDefault(l => l.id == "loadout_1");
			if (loadout == null)
			{
				loadout = new LoadoutDefinition { id = "loadout_1", name = "Deck", medalIds = new List<string>() };
				save.loadouts.Add(loadout);
			}
			if (loadout.cardIds == null) loadout.cardIds = new List<string>();
			if (loadout.medalIds == null) loadout.medalIds = new List<string>();
		}

		public static string GetSaveDirectory()
		{
			string path = ResolveFilePath();
			if (string.IsNullOrEmpty(path)) return string.Empty;
			return Path.GetDirectoryName(path);
		}

		private static (int seed, List<RunMapNode> nodes, List<RunMapShop> shops, List<RunMapTreasure> treasures, List<RunMapEvent> events) GenerateRunMapForSave()
		{
			var (seed, nodes) = LocationMapGeneratorService.Generate();
			var shops = RunMapShopGeneratorService.Generate(seed, nodes);
			var treasures = RunMapTreasureGeneratorService.Generate(seed, nodes, shops);
			var events = RunMapEventGeneratorService.Generate(seed, nodes, shops, treasures);
#if DEBUG
			RunMapGeneratorLog.Append(LocationMapGeneratorService.ComputeSpreadMetrics(seed, nodes));
#endif
			return (seed, nodes, shops, treasures, events);
		}

		public static IReadOnlyList<RunMapShop> GetRunMapShops()
		{
			EnsureLoaded();
			EnsureRunMap();
			return _save?.runMapShops ?? new List<RunMapShop>();
		}

		public static ClimbSaveState GetClimbState()
		{
			EnsureLoaded();
			EnsureClimbState();
			lock (_lock)
			{
				return CloneClimbState(_save?.climb);
			}
		}

		public static void SaveClimbState(ClimbSaveState state)
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				_save.climb = CloneClimbState(state) ?? ClimbRuleService.CreateInitialState(_save.runMapSeed, GetLoadout("loadout_1"));
				Persist();
			}
		}

		public static void EnsureClimbState()
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				if (!_save.isRunActive) return;
				if (_save.climb != null
					&& _save.climb.shopSlots != null
					&& _save.climb.shopSlots.Count == ClimbRuleService.ShopSlotCount
					&& _save.climb.encounterSlots != null
					&& _save.climb.encounterSlots.Count == ClimbRuleService.EncounterSlotCount)
				{
					return;
				}

				EnsurePrimaryLoadout(_save);
				_save.climb = ClimbRuleService.CreateInitialState(_save.runMapSeed, _save.loadouts[0]);
				Persist();
			}
		}

		public static bool TryGetRunShop(string shopId, out RunMapShop shop, out int index)
		{
			shop = null;
			index = -1;
			EnsureRunMap();
			return RunMapShopService.TryGetShop(shopId, _save?.runMapShops, out shop, out index);
		}

		public static IReadOnlyList<RunMapTreasure> GetRunMapTreasures()
		{
			EnsureLoaded();
			EnsureRunMap();
			return _save?.runMapTreasures ?? new List<RunMapTreasure>();
		}

		public static bool TryGetRunTreasure(string treasureId, out RunMapTreasure treasure, out int index)
		{
			treasure = null;
			index = -1;
			EnsureRunMap();
			return RunMapTreasureService.TryGetTreasure(treasureId, _save?.runMapTreasures, out treasure, out index);
		}

		public static IReadOnlyList<RunMapEvent> GetRunMapEvents()
		{
			EnsureLoaded();
			EnsureRunMap();
			return _save?.runMapEvents ?? new List<RunMapEvent>();
		}

		public static bool TryGetRunEvent(string eventId, out RunMapEvent mapEvent, out int index)
		{
			mapEvent = null;
			index = -1;
			EnsureRunMap();
			return RunMapEventService.TryGetEvent(eventId, _save?.runMapEvents, out mapEvent, out index);
		}

		public static bool TryCompleteRunMapEvent(string eventId)
		{
			if (string.IsNullOrWhiteSpace(eventId)) return false;

			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) return false;
				if (!TryGetRunEvent(eventId, out var mapEvent, out _)) return false;
				if (mapEvent == null || mapEvent.isCompleted) return false;

				mapEvent.isCompleted = true;
				Persist();
				return true;
			}
		}

		public static bool TryClaimRunMapTreasure(
			string treasureId,
			EntityManager entityManager,
			out int rewardGold,
			out string rewardMedalId,
			out string rewardEquipmentId)
		{
			rewardGold = 0;
			rewardMedalId = string.Empty;
			rewardEquipmentId = string.Empty;
			if (string.IsNullOrWhiteSpace(treasureId)) return false;

			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) return false;
				if (!TryGetRunTreasure(treasureId, out var treasure, out int index)) return false;
				if (treasure == null || treasure.isClaimed) return false;

				var rng = new Random((_save.runMapSeed ^ 0x71EA5A71) + index);
				rewardGold = System.Math.Max(0, treasure.rewardGold);

				EnsurePrimaryLoadout(_save);
				var loadout = _save.loadouts[0];

				if (treasure.grantsEquipmentReward)
				{
					rewardEquipmentId = RunMapEquipmentPoolService.PickRandomEquipment(
						rng,
						loadout,
						_save.runMapShops,
						excludeShopOffers: true);
					RunMapEquipmentPoolService.ApplyEquipmentToLoadout(loadout, rewardEquipmentId);
				}
				else
				{
					rewardMedalId = RunMapTreasureMedalPoolService.PickRandomMedal(rng, entityManager);
					if (loadout.medalIds == null) loadout.medalIds = new List<string>();
					loadout.medalIds.Add(rewardMedalId);
				}

				treasure.isClaimed = true;
				AddGold(rewardGold);
				Persist();
				return true;
			}
		}

		public static bool TryPurchaseRunMapShopItem(string shopId, int slotIndex, out int newGold)
		{
			newGold = 0;
			if (string.IsNullOrWhiteSpace(shopId) || slotIndex < 0) return false;

			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) return false;
				if (!TryGetRunShop(shopId, out var shop, out _) || shop?.items == null) return false;
				if (slotIndex >= shop.items.Count) return false;

				var item = shop.items[slotIndex];
				if (item == null || item.isPurchased) return false;
				if (string.IsNullOrWhiteSpace(item.cardId)) return false;
				if (!item.IsMedal && !item.IsEquipment && string.IsNullOrWhiteSpace(item.color)) return false;

				int price = System.Math.Max(0, item.price);
				if (_save.gold < price) return false;

				EnsurePrimaryLoadout(_save);
				var loadout = _save.loadouts[0];
				if (loadout.cardIds == null) loadout.cardIds = new List<string>();
				if (loadout.medalIds == null) loadout.medalIds = new List<string>();

				if (item.IsEquipment && IsItemOwned(item.cardId, ForSaleItemType.Equipment))
				{
					return false;
				}

				_save.gold = System.Math.Max(0, _save.gold - price);
				item.isPurchased = true;
				if (item.IsMedal)
				{
					loadout.medalIds.Add(item.cardId);
				}
				else if (item.IsEquipment)
				{
					RunMapEquipmentPoolService.ApplyEquipmentToLoadout(loadout, item.cardId);
				}
				else
				{
					string cardKey = $"{item.cardId}|{item.color}";
					loadout.cardIds.Add(cardKey);
					Persist();
					EventManager.Publish(new LoadoutCardAdded { LoadoutId = loadout.id, CardKey = cardKey });
					newGold = _save.gold;
					return true;
				}

				Persist();
				newGold = _save.gold;
				return true;
			}
		}

		private static IEnumerable<string> EnumerateSaveFilePaths()
		{
			string primary = ResolvePrimarySaveFilePath(ensureSaveDirectory: false);
			if (!string.IsNullOrEmpty(primary)) yield return primary;
			string legacy = ResolveLegacyFilePath();
			if (!string.IsNullOrEmpty(legacy)) yield return legacy;
		}

		private static string ResolveFilePath()
		{
			string path = ResolvePrimarySaveFilePath(ensureSaveDirectory: true);
			if (!string.IsNullOrEmpty(path)) _filePath = path;
			return path;
		}

		private static string ResolvePrimarySaveFilePath(bool ensureSaveDirectory)
		{
			if (!string.IsNullOrEmpty(_filePath)) return _filePath;
			try
			{
				var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
				if (string.IsNullOrEmpty(appData)) return string.Empty;
				var saveDir = Path.Combine(appData, "Crusaders30XX");
				if (ensureSaveDirectory && !Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);
				return Path.Combine(saveDir, "save_file.json");
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
				EventManager.Publish(new LoadoutCardAdded { LoadoutId = loadoutId, CardKey = cardKey });
				return true;
			}
		}

		public static bool RemoveCardFromLoadout(string loadoutId, string cardKey, bool publishChange = true)
		{
			if (string.IsNullOrWhiteSpace(loadoutId) || string.IsNullOrWhiteSpace(cardKey)) return false;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save?.loadouts == null) return false;
				var loadout = _save.loadouts.FirstOrDefault(l => l.id == loadoutId);
				if (loadout?.cardIds == null) return false;

				int idx = loadout.cardIds.FindIndex(k =>
					string.Equals(k, cardKey, StringComparison.OrdinalIgnoreCase));
				if (idx < 0) return false;

				loadout.cardIds.RemoveAt(idx);
				if (!loadout.cardIds.Any(k =>
					string.Equals(k, cardKey, StringComparison.OrdinalIgnoreCase)))
				{
					_save.runCardRestrictions?.Remove(cardKey);
				}
				RemoveTrackedTradedCardKeyLocked(cardKey);
				Persist();
				if (publishChange)
				{
					EventManager.Publish(new LoadoutCardRemoved { LoadoutId = loadoutId, CardKey = cardKey });
				}
				return true;
			}
		}

		public static bool ReplaceCardInLoadoutAtIndex(
			string loadoutId,
			int index,
			string expectedOldCardKey,
			string newCardKey,
			bool publishChange = true)
		{
			if (string.IsNullOrWhiteSpace(loadoutId) || index < 0 || string.IsNullOrWhiteSpace(newCardKey)) return false;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save?.loadouts == null) return false;
				var loadout = _save.loadouts.FirstOrDefault(l => l.id == loadoutId);
				if (loadout?.cardIds == null || index >= loadout.cardIds.Count) return false;

				string oldKey = loadout.cardIds[index];
				if (!string.IsNullOrWhiteSpace(expectedOldCardKey)
					&& !string.Equals(oldKey, expectedOldCardKey, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}

				loadout.cardIds[index] = newCardKey;
				if (!loadout.cardIds.Any(k => string.Equals(k, oldKey, StringComparison.OrdinalIgnoreCase)))
				{
					_save.runCardRestrictions?.Remove(oldKey);
				}
				Persist();
				if (publishChange)
				{
					EventManager.Publish(new LoadoutCardRemoved { LoadoutId = loadoutId, CardKey = oldKey });
					EventManager.Publish(new LoadoutCardAdded { LoadoutId = loadoutId, CardKey = newCardKey });
				}
				return true;
			}
		}

		public static DeckRewardOfferSave GetPendingDeckRewardOffer()
		{
			EnsureLoaded();
			lock (_lock)
			{
				return CloneDeckRewardOffer(_save?.pendingDeckRewardOffer);
			}
		}

		public static void SetPendingDeckRewardOffer(DeckRewardOfferSave offer)
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				_save.pendingDeckRewardOffer = CloneDeckRewardOffer(offer);
				Persist();
			}
		}

		public static void ClearPendingDeckRewardOffer()
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null || _save.pendingDeckRewardOffer == null) return;
				_save.pendingDeckRewardOffer = null;
				Persist();
			}
		}

		private static DeckRewardOfferSave CloneDeckRewardOffer(DeckRewardOfferSave offer)
		{
			if (offer == null) return null;
			var clone = new DeckRewardOfferSave
			{
				rewardGold = offer.rewardGold,
				options = new List<DeckRewardOfferOptionSave>()
			};
			if (offer.options == null) return clone;
			foreach (var option in offer.options)
			{
				if (option == null) continue;
				clone.options.Add(new DeckRewardOfferOptionSave
				{
					kind = option.kind ?? string.Empty,
					loadoutIndex = option.loadoutIndex,
					outgoingCardKey = option.outgoingCardKey ?? string.Empty,
					incomingCardKey = option.incomingCardKey ?? string.Empty,
					upgradedCardKey = option.upgradedCardKey ?? string.Empty,
				});
			}
			return clone;
		}

		public static bool TrySpendGoldAndAddToCollection(string itemId, int price, ForSaleItemType itemType, out int newGold)
		{
			newGold = 0;
			if (price < 0) price = 0;

			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) return false;
				if (_save.gold < price) return false;

				EnsurePrimaryLoadout(_save);
				var loadout = _save.loadouts[0];

				if (!string.IsNullOrWhiteSpace(itemId) && IsItemOwned(itemId, itemType))
				{
					return false;
				}

				string shopCardKey = null;
				if (itemType == ForSaleItemType.Card && !string.IsNullOrWhiteSpace(itemId))
				{
					shopCardKey = PickShopCardKey(itemId, loadout.cardIds);
					if (string.IsNullOrEmpty(shopCardKey)) return false;
				}

				_save.gold = System.Math.Max(0, _save.gold - price);

				if (!string.IsNullOrWhiteSpace(itemId))
				{
					if (itemType == ForSaleItemType.Card)
					{
						loadout.cardIds.Add(shopCardKey);
					}
					else if (itemType == ForSaleItemType.Weapon)
					{
						loadout.weaponId = itemId;
					}
					else if (itemType == ForSaleItemType.Medal)
					{
						loadout.medalIds.Add(itemId);
					}
					else if (itemType == ForSaleItemType.Equipment)
					{
						EquipmentBase equipment = EquipmentFactory.Create(itemId);
						switch (equipment.Slot)
						{
							case EquipmentSlot.Chest:
								loadout.chestId = itemId;
								break;
							case EquipmentSlot.Legs:
								loadout.legsId = itemId;
								break;
							case EquipmentSlot.Arms:
								loadout.armsId = itemId;
								break;
							case EquipmentSlot.Head:
								loadout.headId = itemId;
								break;
						}
					}
				}

				Persist();
				if (!string.IsNullOrEmpty(shopCardKey))
				{
					EventManager.Publish(new LoadoutCardAdded { LoadoutId = loadout.id, CardKey = shopCardKey });
				}
				newGold = _save.gold;
				return true;
			}
		}

		private static string PickShopCardKey(string cardId, List<string> deckKeys)
		{
			if (DeckRules.CountCardIdInDeck(deckKeys, cardId) >= DeckRules.MaxCopiesPerCardId) return null;
			var deckKeySet = new HashSet<string>(deckKeys ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			string[] colors = { "Red", "White", "Black" };
			var eligible = new List<string>();
			foreach (var color in colors)
			{
				string key = $"{cardId}|{color}";
				if (!deckKeySet.Contains(key)) eligible.Add(key);
			}
			if (eligible.Count == 0) return null;
			return eligible[System.Random.Shared.Next(eligible.Count)];
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

		public static bool IsGuidedTutorialCompleted()
		{
			EnsureLoaded();
			return _save?.guidedTutorialCompleted == true;
		}

		public static void CompleteGuidedTutorial()
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = CreateInactiveSavePreservingMeta(null);
				_save.guidedTutorialCompleted = true;
				_save.seenTutorials ??= new List<string>();
				foreach (string key in GuidedTutorialDefinitions.CoveredTutorialKeys.Append(GuidedTutorialDefinitions.CompletionTutorialKey))
				{
					if (!_save.seenTutorials.Contains(key))
					{
						_save.seenTutorials.Add(key);
					}
				}
				Persist();
			}
		}

		private const int PointsToMaster = 50;

		public static CardMastery GetMasteryData(string cardId)
		{
			if (TestFightRuntime.IsActive) return null;
			if (string.IsNullOrEmpty(cardId)) return null;
			EnsureLoaded();
			if (_save == null || _save.cardMastery == null) return null;
			_save.cardMastery.TryGetValue(cardId, out var mastery);
			return mastery;
		}

		public static void AddMasteryPoints(string cardId, int points)
		{
			if (TestFightRuntime.IsActive) return;
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

		public static void ClearRunScopedState()
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				_save.runLongPassives = new Dictionary<string, int>();
				_save.runCardRestrictions = new Dictionary<string, List<string>>();
				Persist();
			}
		}

		public static IReadOnlyDictionary<string, int> GetRunLongPassivesSnapshot()
		{
			EnsureLoaded();
			lock (_lock)
			{
				if (_save?.runLongPassives == null) return new Dictionary<string, int>();
				return new Dictionary<string, int>(_save.runLongPassives);
			}
		}

		public static void SetRunLongPassive(string passiveTypeName, int stacks)
		{
			if (string.IsNullOrWhiteSpace(passiveTypeName)) return;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				if (_save.runLongPassives == null) _save.runLongPassives = new Dictionary<string, int>();
				if (stacks <= 0) _save.runLongPassives.Remove(passiveTypeName);
				else _save.runLongPassives[passiveTypeName] = stacks;
				Persist();
			}
		}

		public static List<string> GetRunCardRestrictions(string cardKey)
		{
			if (string.IsNullOrWhiteSpace(cardKey)) return new List<string>();
			EnsureLoaded();
			lock (_lock)
			{
				if (_save?.runCardRestrictions == null) return new List<string>();
				if (!_save.runCardRestrictions.TryGetValue(cardKey, out var list) || list == null) return new List<string>();
				return new List<string>(list);
			}
		}

		public static void AddRunCardRestriction(string cardKey, string restrictionName)
		{
			if (string.IsNullOrWhiteSpace(cardKey) || string.IsNullOrWhiteSpace(restrictionName)) return;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				if (_save.runCardRestrictions == null) _save.runCardRestrictions = new Dictionary<string, List<string>>();
				if (!_save.runCardRestrictions.TryGetValue(cardKey, out var list) || list == null)
				{
					list = new List<string>();
					_save.runCardRestrictions[cardKey] = list;
				}
				if (!list.Contains(restrictionName, StringComparer.OrdinalIgnoreCase))
				{
					list.Add(restrictionName);
				}
				Persist();
			}
		}

		public static void RemoveRunCardRestriction(string cardKey, string restrictionName)
		{
			if (string.IsNullOrWhiteSpace(cardKey) || string.IsNullOrWhiteSpace(restrictionName)) return;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save?.runCardRestrictions == null) return;
				if (!_save.runCardRestrictions.TryGetValue(cardKey, out var list) || list == null) return;
				list.RemoveAll(r => string.Equals(r, restrictionName, StringComparison.OrdinalIgnoreCase));
				if (list.Count == 0) _save.runCardRestrictions.Remove(cardKey);
				Persist();
			}
		}

		public static void SetRunCardRestrictionsForCard(string cardKey, List<string> restrictionNames)
		{
			if (string.IsNullOrWhiteSpace(cardKey)) return;
			EnsureLoaded();
			lock (_lock)
			{
				if (_save == null) _save = new SaveFile();
				if (_save.runCardRestrictions == null) _save.runCardRestrictions = new Dictionary<string, List<string>>();
				if (restrictionNames == null || restrictionNames.Count == 0)
				{
					_save.runCardRestrictions.Remove(cardKey);
				}
				else
				{
					_save.runCardRestrictions[cardKey] = new List<string>(restrictionNames);
				}
				Persist();
			}
		}

		private static ClimbSaveState CloneClimbState(ClimbSaveState state)
		{
			if (state == null) return null;
			return new ClimbSaveState
			{
				time = ClimbRuleService.ClampTime(state.time),
				resources = CloneClimbResources(state.resources),
				shopSlots = CloneClimbShopSlots(state.shopSlots),
				encounterSlots = CloneClimbEncounterSlots(state.encounterSlots),
				eventSlots = CloneClimbEventSlots(state.eventSlots),
				shownMedalIds = CloneStringList(state.shownMedalIds),
				shownEquipmentIds = CloneStringList(state.shownEquipmentIds),
				shownEventTypeIds = CloneStringList(state.shownEventTypeIds),
				nextEventSlotId = Math.Max(0, state.nextEventSlotId),
				pendingReplacementOffer = CloneClimbReplacementOffer(state.pendingReplacementOffer),
				pendingEncounterReward = CloneClimbEncounterReward(state.pendingEncounterReward),
				pendingEvent = CloneClimbPendingEvent(state.pendingEvent),
			};
		}

		private static ClimbResourceSave CloneClimbResources(ClimbResourceSave resources)
		{
			return new ClimbResourceSave
			{
				red = Math.Max(0, resources?.red ?? 0),
				white = Math.Max(0, resources?.white ?? 0),
				black = Math.Max(0, resources?.black ?? 0),
			};
		}

		private static List<ClimbShopSlotSave> CloneClimbShopSlots(List<ClimbShopSlotSave> slots)
		{
			var clone = new List<ClimbShopSlotSave>();
			if (slots == null) return clone;
			foreach (var slot in slots)
			{
				if (slot == null) continue;
				clone.Add(new ClimbShopSlotSave
				{
					id = slot.id ?? string.Empty,
					kind = slot.kind ?? ClimbShopSlotKinds.Empty,
					itemId = slot.itemId ?? string.Empty,
					cardKey = slot.cardKey ?? string.Empty,
					deckIndex = slot.deckIndex,
					cost = CloneClimbResources(slot.cost),
					timeCost = Math.Max(0, slot.timeCost),
					isSold = slot.isSold,
					generatedAtTime = ClimbRuleService.ClampTime(slot.generatedAtTime),
				});
			}
			return clone;
		}

		private static List<ClimbEncounterSlotSave> CloneClimbEncounterSlots(List<ClimbEncounterSlotSave> slots)
		{
			var clone = new List<ClimbEncounterSlotSave>();
			if (slots == null) return clone;
			foreach (var slot in slots)
			{
				if (slot == null) continue;
				clone.Add(new ClimbEncounterSlotSave
				{
					id = slot.id ?? string.Empty,
					enemyId = slot.enemyId ?? string.Empty,
					generatedAtTime = ClimbRuleService.ClampTime(slot.generatedAtTime),
					duration = Math.Max(0, slot.duration),
					timeCost = Math.Max(0, slot.timeCost),
					rewardResources = CloneClimbResources(slot.rewardResources),
					hasDeckReward = slot.hasDeckReward,
					isCompleted = slot.isCompleted,
					isFinal = slot.isFinal,
				});
			}
			return clone;
		}

		private static List<ClimbEventSlotSave> CloneClimbEventSlots(List<ClimbEventSlotSave> slots)
		{
			var clone = new List<ClimbEventSlotSave>();
			if (slots == null) return clone;
			foreach (var slot in slots)
			{
				if (slot == null) continue;
				clone.Add(new ClimbEventSlotSave
				{
					id = slot.id ?? string.Empty,
					eventTypeId = slot.eventTypeId ?? string.Empty,
					generatedAtTime = ClimbRuleService.ClampTime(slot.generatedAtTime),
					visibleStartTime = ClimbRuleService.ClampTime(slot.visibleStartTime),
					visibleEndTime = ClimbRuleService.ClampTime(slot.visibleEndTime),
					timeCost = Math.Max(0, slot.timeCost),
					seen = slot.seen,
					isCompleted = slot.isCompleted,
				});
			}
			return clone;
		}

		private static ClimbReplacementOfferSave CloneClimbReplacementOffer(ClimbReplacementOfferSave offer)
		{
			if (offer == null) return null;
			return new ClimbReplacementOfferSave
			{
				shopSlotIndex = offer.shopSlotIndex,
				incomingCardKey = offer.incomingCardKey ?? string.Empty,
				cost = CloneClimbResources(offer.cost),
			};
		}

		private static ClimbEncounterRewardSave CloneClimbEncounterReward(ClimbEncounterRewardSave reward)
		{
			if (reward == null) return null;
			return new ClimbEncounterRewardSave
			{
				encounterSlotId = reward.encounterSlotId ?? string.Empty,
				resources = CloneClimbResources(reward.resources),
				deckRewardOffer = CloneDeckRewardOffer(reward.deckRewardOffer),
				pendingFinalEncounter = reward.pendingFinalEncounter,
			};
		}

		private static ClimbPendingEventSave CloneClimbPendingEvent(ClimbPendingEventSave pending)
		{
			if (pending == null) return null;
			return new ClimbPendingEventSave
			{
				eventSlotId = pending.eventSlotId ?? string.Empty,
				eventTypeId = pending.eventTypeId ?? string.Empty,
			};
		}

		private static List<string> CloneStringList(List<string> list)
		{
			return list == null
				? new List<string>()
				: list.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
	}
}
