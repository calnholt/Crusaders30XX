using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Cards;

namespace Crusaders30XX.ECS.Services
{
	public static class ClimbRuleService
	{
		public const int MaxTime = 32;
		public const int ShopSlotCount = 4;
		public const int EncounterSlotCount = 3;
		public const int EventSlotCount = 3;
		public const int ShopRefreshInterval = 8;
		public const int EncounterMinDuration = 2;
		public const int EncounterMaxDuration = 5;
		private const int RngSalt = unchecked((int)0xC11A1B00);

		private static readonly HashSet<string> BannedClimbEncounterEnemyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"gleeber",
			"sand_corpse",
			"training_demon",
		};

		private static readonly CardData.CardColor[] ResourceColors =
		{
			CardData.CardColor.Red,
			CardData.CardColor.White,
			CardData.CardColor.Black
		};

		public static ClimbSaveState CreateInitialState(int seed, LoadoutDefinition loadout = null)
		{
			var state = new ClimbSaveState
			{
				time = 0,
				resources = new ClimbResourceSave { red = 1, white = 1, black = 1 },
				shopSlots = new List<ClimbShopSlotSave>(),
				encounterSlots = new List<ClimbEncounterSlotSave>(),
				eventSlots = new List<ClimbEventSlotSave>(),
				shownMedalIds = new List<string>(),
				shownEquipmentIds = new List<string>(),
				shownEventTypeIds = new List<string>(),
				nextEventSlotId = 0,
			};

			RefreshShopSlots(state, seed, loadout);
			RefreshEncounterSlots(state, seed);
			ReplenishEventSlots(state, seed);
			return state;
		}

		public static int ClampTime(int time) => Math.Clamp(time, 0, MaxTime);

		public static int ApplyTime(ClimbSaveState state, int delta)
		{
			if (state == null) return 0;
			int before = ClampTime(state.time);
			int after = ClampTime(before + Math.Max(0, delta));
			state.time = after;
			return after - before;
		}

		public static bool ShouldRefreshShopAtTime(int previousTime, int newTime)
		{
			previousTime = ClampTime(previousTime);
			newTime = ClampTime(newTime);
			if (newTime <= previousTime) return false;
			int nextBoundary = ((previousTime / ShopRefreshInterval) + 1) * ShopRefreshInterval;
			return nextBoundary < MaxTime && newTime >= nextBoundary;
		}

		public static bool HasPendingFinalEncounter(ClimbSaveState state)
		{
			if (state?.pendingEncounterReward?.pendingFinalEncounter == true) return true;
			return ClampTime(state?.time ?? 0) >= MaxTime;
		}

		public static bool IsEventVisible(ClimbEventSlotSave slot, int time)
		{
			if (slot == null || slot.isCompleted || string.IsNullOrWhiteSpace(slot.eventTypeId)) return false;
			int clamped = ClampTime(time);
			return clamped >= slot.visibleStartTime && clamped <= slot.visibleEndTime;
		}

		public static bool IsEventExpired(ClimbEventSlotSave slot, int time)
		{
			if (slot == null || slot.isCompleted || string.IsNullOrWhiteSpace(slot.eventTypeId)) return false;
			return ClampTime(time) > slot.visibleEndTime;
		}

		public static bool IsEncounterExpired(ClimbEncounterSlotSave slot, int time)
		{
			if (slot == null || slot.isCompleted || slot.isFinal || string.IsNullOrWhiteSpace(slot.enemyId)) return false;
			if (slot.duration <= 0) return true;
			return ClampTime(time) >= ClampTime(slot.generatedAtTime + slot.duration);
		}

		public static void ExpireEvents(ClimbSaveState state)
		{
			if (state?.eventSlots == null) return;
			foreach (var slot in state.eventSlots)
			{
				if (IsPendingEventSlot(state, slot)) continue;
				if (!IsEventExpired(slot, state.time)) continue;
				if (!string.IsNullOrWhiteSpace(slot.eventTypeId))
				{
					AddDistinct(state.shownEventTypeIds, slot.eventTypeId);
				}
				slot.isCompleted = true;
			}
		}

		public static void MarkFirstVisibleEventsSeen(ClimbSaveState state)
		{
			if (state?.eventSlots == null) return;
			foreach (var slot in state.eventSlots)
			{
				if (slot == null || slot.seen || !IsEventVisible(slot, state.time)) continue;
				slot.seen = true;
				AddDistinct(state.shownEventTypeIds, slot.eventTypeId);
			}
		}

		public static void UpdateEventSlots(ClimbSaveState state, int seed)
		{
			if (state == null) return;
			ExpireEvents(state);
			MarkFirstVisibleEventsSeen(state);
			ReplenishEventSlots(state, seed);
		}

		public static bool CanAfford(ClimbResourceSave available, ClimbResourceSave cost)
		{
			available ??= new ClimbResourceSave();
			cost ??= new ClimbResourceSave { red = 0, white = 0, black = 0 };
			return available.red >= Math.Max(0, cost.red)
				&& available.white >= Math.Max(0, cost.white)
				&& available.black >= Math.Max(0, cost.black);
		}

		public static bool TrySpend(ClimbResourceSave available, ClimbResourceSave cost)
		{
			if (!CanAfford(available, cost)) return false;
			available.red -= Math.Max(0, cost?.red ?? 0);
			available.white -= Math.Max(0, cost?.white ?? 0);
			available.black -= Math.Max(0, cost?.black ?? 0);
			return true;
		}

		public static void AddResources(ClimbResourceSave target, ClimbResourceSave reward)
		{
			if (target == null || reward == null) return;
			target.red = Math.Max(0, target.red + Math.Max(0, reward.red));
			target.white = Math.Max(0, target.white + Math.Max(0, reward.white));
			target.black = Math.Max(0, target.black + Math.Max(0, reward.black));
		}

		public static ClimbResourceSave GenerateCost(Random rng, int min = 1, int max = 2)
		{
			rng ??= Random.Shared;
			var cost = new ClimbResourceSave { red = 0, white = 0, black = 0 };
			int pips = rng.Next(Math.Max(1, min), Math.Max(min, max) + 1);
			for (int i = 0; i < pips; i++)
			{
				AddColor(cost, ResourceColors[rng.Next(ResourceColors.Length)], 1);
			}
			return cost;
		}

		public static ClimbResourceSave GenerateReward(Random rng, int min = 1, int max = 3)
		{
			return GenerateCost(rng, min, max);
		}

		public static void RefreshShopSlots(ClimbSaveState state, int seed, LoadoutDefinition loadout)
		{
			if (state == null) return;
			state.shopSlots = new List<ClimbShopSlotSave>(ShopSlotCount);
			var rng = CreateRng(seed, state.time, 11);
			var kinds = new List<string>
			{
				ClimbShopSlotKinds.Medal,
				ClimbShopSlotKinds.Equipment,
				ClimbShopSlotKinds.Upgrade,
				ClimbShopSlotKinds.Replacement,
			}.OrderBy(_ => rng.Next()).ToList();

			for (int i = 0; i < ShopSlotCount; i++)
			{
				var slot = RollShopSlot(state, loadout, rng, i, kinds[i]);
				state.shopSlots.Add(slot);
			}
		}

		public static void RefreshEncounterSlots(ClimbSaveState state, int seed)
		{
			if (state == null) return;
			state.encounterSlots = new List<ClimbEncounterSlotSave>(EncounterSlotCount);
			var rng = CreateRng(seed, state.time, 23);
			for (int i = 0; i < EncounterSlotCount; i++)
			{
				state.encounterSlots.Add(RollEncounterSlot(state, rng, $"encounter_{i}"));
			}
		}

		public static bool ReplenishEncounterSlots(ClimbSaveState state, int seed)
		{
			if (state == null) return false;
			state.encounterSlots ??= new List<ClimbEncounterSlotSave>();

			if (ClampTime(state.time) >= MaxTime) return false;

			var rng = CreateRng(seed, state.time, 23 + state.encounterSlots.Count);
			bool changed = false;
			while (state.encounterSlots.Count < EncounterSlotCount)
			{
				state.encounterSlots.Add(RollEncounterSlot(state, rng, $"encounter_{state.encounterSlots.Count}"));
				changed = true;
			}

			for (int i = 0; i < state.encounterSlots.Count; i++)
			{
				var slot = state.encounterSlots[i];
				if (slot != null
					&& !slot.isCompleted
					&& !slot.isFinal
					&& IsValidClimbEncounterEnemy(slot.enemyId)
					&& !IsEncounterExpired(slot, state.time)
					&& slot.duration >= EncounterMinDuration
					&& slot.duration <= EncounterMaxDuration
					&& slot.timeCost >= 1
					&& slot.timeCost <= 3)
				{
					continue;
				}

				string slotId = string.IsNullOrWhiteSpace(slot?.id) ? $"encounter_{i}" : slot.id;
				state.encounterSlots[i] = RollEncounterSlot(state, rng, slotId, slot?.enemyId);
				changed = true;
			}

			if (state.encounterSlots.Count > EncounterSlotCount)
			{
				state.encounterSlots = state.encounterSlots.Take(EncounterSlotCount).ToList();
				changed = true;
			}

			return changed;
		}

		public static int AdvanceTimeAndUpdateSlots(ClimbSaveState state, int seed, LoadoutDefinition loadout, int timeCost)
		{
			if (state == null) return 0;
			int previousTime = state.time;
			int applied = ApplyTime(state, timeCost);
			if (ShouldRefreshShopAtTime(previousTime, state.time))
			{
				RefreshShopSlots(state, seed, loadout);
			}
			UpdateEventSlots(state, seed);
			ReplenishEncounterSlots(state, seed);
			return applied;
		}

		public static IReadOnlyList<string> GetClimbEncounterEnemyPool()
		{
			return EnemyPortraitContent.GetRunMapEnemyPool()
				.Where(IsValidClimbEncounterEnemy)
				.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public static void RefreshEventSlots(ClimbSaveState state, int seed)
		{
			if (state == null) return;
			state.eventSlots = new List<ClimbEventSlotSave>(EventSlotCount);
			ReplenishEventSlots(state, seed);
		}

		public static void ReplenishEventSlots(ClimbSaveState state, int seed)
		{
			if (state == null) return;
			state.eventSlots ??= new List<ClimbEventSlotSave>();
			state.shownEventTypeIds ??= new List<string>();

			state.eventSlots = state.eventSlots
				.Where(slot => slot != null && !slot.isCompleted && !string.IsNullOrWhiteSpace(slot.eventTypeId))
				.Take(EventSlotCount)
				.ToList();

			if (state.eventSlots.Count >= EventSlotCount) return;
			if (ClampTime(state.time) + 6 > MaxTime) return;

			var activeEventIds = new HashSet<string>(
				state.eventSlots.Select(slot => slot.eventTypeId),
				StringComparer.OrdinalIgnoreCase);
			var shownEventIds = new HashSet<string>(state.shownEventTypeIds, StringComparer.OrdinalIgnoreCase);
			var rng = CreateRng(seed, state.time, 37 + Math.Max(0, state.nextEventSlotId));
			var pool = EventFactory.GetAllEvents().Keys
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Where(id => !shownEventIds.Contains(id))
				.Where(id => !activeEventIds.Contains(id))
				.OrderBy(_ => rng.Next())
				.ToList();

			while (state.eventSlots.Count < EventSlotCount && pool.Count > 0)
			{
				string eventType = pool[0];
				pool.RemoveAt(0);
				int maxStartOffset = Math.Min(8, MaxTime - ClampTime(state.time) - 3);
				if (maxStartOffset < 3) break;

				int start = ClampTime(state.time + rng.Next(3, maxStartOffset + 1));
				int maxDuration = Math.Min(6, MaxTime - start);
				if (maxDuration < 3) break;
				int duration = rng.Next(3, maxDuration + 1);
				int end = ClampTime(start + duration);
				state.eventSlots.Add(new ClimbEventSlotSave
				{
					id = $"climb_event_{state.nextEventSlotId++}",
					eventTypeId = eventType,
					generatedAtTime = state.time,
					visibleStartTime = start,
					visibleEndTime = end,
					timeCost = rng.Next(1, 3),
				});
			}
		}

		public static string RollReplacementIncomingCardKey(Random rng, LoadoutDefinition loadout)
		{
			rng ??= Random.Shared;
			var deckIds = new HashSet<string>(
				(loadout?.cardIds ?? new List<string>()).Select(DeckRules.ParseBaseCardId),
				StringComparer.OrdinalIgnoreCase);
			var pool = CardFactory.GetAllCards().Values
				.Where(card => card != null && card.CanAddToLoadout && !card.IsWeapon && !card.IsToken)
				.Where(card => !string.IsNullOrWhiteSpace(card.CardId))
				.Where(card => !deckIds.Contains(card.CardId) || card.Rarity != Rarity.Starter)
				.Select(card => card.CardId)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(_ => rng.Next())
				.ToList();
			if (pool.Count == 0) return string.Empty;
			var color = ResourceColors[rng.Next(ResourceColors.Length)];
			return RunDeckService.BuildCardKey(pool[0], color);
		}

		private static ClimbShopSlotSave RollShopSlot(
			ClimbSaveState state,
			LoadoutDefinition loadout,
			Random rng,
			int slotIndex,
			string kind)
		{
			var slot = new ClimbShopSlotSave
			{
				id = $"shop_{slotIndex}",
				kind = kind,
				timeCost = rng.Next(1, 4),
				generatedAtTime = state.time,
			};
			slot.cost = GenerateShopCost(rng, kind, slot.timeCost);

			if (kind == ClimbShopSlotKinds.Medal)
			{
				slot.itemId = PickUnshown(MedalFactory.GetAllMedals().Keys, state.shownMedalIds, rng);
				if (string.IsNullOrWhiteSpace(slot.itemId)) slot.kind = ClimbShopSlotKinds.Empty;
				else AddDistinct(state.shownMedalIds, slot.itemId);
			}
			else if (kind == ClimbShopSlotKinds.Equipment)
			{
				var allEquipment = EquipmentFactory.GetAllEquipment().Keys;
				slot.itemId = PickUnshown(allEquipment, state.shownEquipmentIds, rng);
				if (string.IsNullOrWhiteSpace(slot.itemId)) slot.kind = ClimbShopSlotKinds.Empty;
				else AddDistinct(state.shownEquipmentIds, slot.itemId);
			}
			else if (kind == ClimbShopSlotKinds.Upgrade)
			{
				var upgrade = PickUpgradeableDeckIndex(loadout, rng);
				if (upgrade.index < 0) slot.kind = ClimbShopSlotKinds.Empty;
				else
				{
					slot.deckIndex = upgrade.index;
					slot.cardKey = RunDeckService.BuildUpgradedCardKey(upgrade.cardKey);
				}
			}
			else if (kind == ClimbShopSlotKinds.Replacement)
			{
				slot.cardKey = RollReplacementIncomingCardKey(rng, loadout);
				if (string.IsNullOrWhiteSpace(slot.cardKey)) slot.kind = ClimbShopSlotKinds.Empty;
			}

			if (string.Equals(slot.kind, ClimbShopSlotKinds.Empty, StringComparison.OrdinalIgnoreCase))
			{
				slot.timeCost = 0;
				slot.cost = new ClimbResourceSave { red = 0, white = 0, black = 0 };
			}

			return slot;
		}

		private static ClimbEncounterSlotSave RollEncounterSlot(
			ClimbSaveState state,
			Random rng,
			string slotId,
			string excludedEnemyId = "")
		{
			return new ClimbEncounterSlotSave
			{
				id = slotId ?? string.Empty,
				enemyId = RollClimbEncounterEnemyId(rng, excludedEnemyId),
				generatedAtTime = ClampTime(state?.time ?? 0),
				duration = rng.Next(EncounterMinDuration, EncounterMaxDuration + 1),
				timeCost = rng.Next(1, 4),
				rewardResources = GenerateReward(rng, 1, 3),
				hasDeckReward = true,
				isFinal = false,
				isCompleted = false,
			};
		}

		internal static ClimbResourceSave GenerateShopCostForTests(string kind, int timeCost)
		{
			return GenerateShopCost(new Random(1), kind, timeCost);
		}

		private static ClimbResourceSave GenerateShopCost(Random rng, string kind, int timeCost)
		{
			rng ??= Random.Shared;
			var dominant = CardData.CardColor.Red;
			int baseline = 3;
			int reductionPerExtraTime = 1;

			if (string.Equals(kind, ClimbShopSlotKinds.Medal, StringComparison.OrdinalIgnoreCase))
			{
				dominant = CardData.CardColor.White;
				baseline = 6;
				reductionPerExtraTime = 2;
			}
			else if (string.Equals(kind, ClimbShopSlotKinds.Equipment, StringComparison.OrdinalIgnoreCase))
			{
				dominant = CardData.CardColor.Black;
				baseline = 8;
				reductionPerExtraTime = 2;
			}

			int total = Math.Max(0, baseline - Math.Max(0, timeCost - 1) * reductionPerExtraTime);
			return GenerateDominantCost(rng, total, dominant);
		}

		private static ClimbResourceSave GenerateDominantCost(Random rng, int total, CardData.CardColor dominant)
		{
			var cost = new ClimbResourceSave { red = 0, white = 0, black = 0 };
			if (total <= 0) return cost;

			int dominantPips = 1;
			while (dominantPips < total && dominantPips <= (int)Math.Ceiling((total - dominantPips) / 2f))
			{
				dominantPips++;
			}

			AddColor(cost, dominant, dominantPips);

			var offColors = ResourceColors
				.Where(color => color != dominant)
				.OrderBy(_ => rng.Next())
				.ToArray();
			int remaining = total - dominantPips;
			AddColor(cost, offColors[0], remaining / 2 + remaining % 2);
			AddColor(cost, offColors[1], remaining / 2);
			return cost;
		}

		private static string RollClimbEncounterEnemyId(Random rng, string excludedEnemyId)
		{
			rng ??= Random.Shared;
			var pool = GetClimbEncounterEnemyPool().ToList();
			if (pool.Count == 0) return "skeleton";
			if (pool.Count > 1 && !string.IsNullOrWhiteSpace(excludedEnemyId))
			{
				pool = pool
					.Where(id => !string.Equals(id, excludedEnemyId, StringComparison.OrdinalIgnoreCase))
					.ToList();
			}
			return pool[rng.Next(pool.Count)];
		}

		private static bool IsValidClimbEncounterEnemy(string enemyId)
		{
			if (string.IsNullOrWhiteSpace(enemyId)) return false;
			if (BannedClimbEncounterEnemyIds.Contains(enemyId)) return false;
			var enemy = EnemyFactory.Create(enemyId);
			return enemy != null
				&& !enemy.IsBoss
				&& !enemy.IsTutorialOnly
				&& EnemyPortraitContent.HasPortrait(enemyId);
		}

		private static (int index, string cardKey) PickUpgradeableDeckIndex(LoadoutDefinition loadout, Random rng)
		{
			var eligible = new List<(int index, string cardKey)>();
			var cards = loadout?.cardIds;
			if (cards == null) return (-1, string.Empty);
			for (int i = 0; i < cards.Count; i++)
			{
				if (!RunDeckService.TryParseCardKey(cards[i], out var cardId, out _, out bool isUpgraded)) continue;
				if (isUpgraded) continue;
				var card = CardFactory.Create(cardId);
				if (card == null || card.IsWeapon || card.IsToken || !card.CanAddToLoadout) continue;
				eligible.Add((i, cards[i]));
			}

			if (eligible.Count == 0) return (-1, string.Empty);
			return eligible[rng.Next(eligible.Count)];
		}

		private static string PickUnshown(IEnumerable<string> allIds, List<string> shownIds, Random rng)
		{
			var shown = new HashSet<string>(shownIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			var pool = allIds
				.Where(id => !string.IsNullOrWhiteSpace(id) && !shown.Contains(id))
				.OrderBy(_ => rng.Next())
				.ToList();
			return pool.Count == 0 ? string.Empty : pool[0];
		}

		private static void AddColor(ClimbResourceSave resources, CardData.CardColor color, int amount)
		{
			switch (color)
			{
				case CardData.CardColor.Red:
					resources.red += amount;
					break;
				case CardData.CardColor.White:
					resources.white += amount;
					break;
				case CardData.CardColor.Black:
					resources.black += amount;
					break;
			}
		}

		private static void AddDistinct(List<string> list, string value)
		{
			if (list == null || string.IsNullOrWhiteSpace(value)) return;
			if (!list.Contains(value, StringComparer.OrdinalIgnoreCase)) list.Add(value);
		}

		private static bool IsPendingEventSlot(ClimbSaveState state, ClimbEventSlotSave slot)
		{
			if (state?.pendingEvent == null || slot == null) return false;
			return string.Equals(state.pendingEvent.eventSlotId, slot.id, StringComparison.OrdinalIgnoreCase);
		}

		private static Random CreateRng(int seed, int time, int salt)
		{
			int value = seed == 0 ? 1 : seed;
			return new Random(value ^ RngSalt ^ (time * 397) ^ salt);
		}
	}
}
