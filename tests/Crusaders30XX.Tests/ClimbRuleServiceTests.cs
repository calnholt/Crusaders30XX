using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class ClimbRuleServiceTests
{
	[Fact]
	public void Initial_state_has_time_resources_and_required_slots()
	{
		var state = ClimbRuleService.CreateInitialState(123, TestLoadout());
		int eventPoolCount = EventFactory.GetAllEvents().Count;

		Assert.Equal(0, state.time);
		Assert.Equal(1, state.resources.red);
		Assert.Equal(1, state.resources.white);
		Assert.Equal(1, state.resources.black);
		Assert.Equal(ClimbRuleService.ShopSlotCount, state.shopSlots.Count);
		Assert.Equal(ClimbRuleService.EncounterSlotCount, state.encounterSlots.Count);
		Assert.Equal(Math.Min(eventPoolCount, ClimbRuleService.EventSlotCount), state.eventSlots.Count);
		Assert.All(state.shopSlots.Where(s => s.kind != ClimbShopSlotKinds.Empty), slot => Assert.InRange(slot.timeCost, 1, 3));
		Assert.All(state.encounterSlots, slot =>
		{
			Assert.InRange(slot.timeCost, 1, 3);
			Assert.Equal(state.time, slot.generatedAtTime);
			Assert.InRange(slot.duration, ClimbRuleService.EncounterMinDuration, ClimbRuleService.EncounterMaxDuration);
			Assert.Contains(slot.enemyId, ClimbRuleService.GetClimbEncounterEnemyPool(), StringComparer.OrdinalIgnoreCase);
		});
	}

	[Fact]
	public void Encounter_expiration_uses_rolled_duration_boundary()
	{
		var slot = new ClimbEncounterSlotSave
		{
			id = "encounter",
			enemyId = "skeleton",
			generatedAtTime = 4,
			duration = 3,
			timeCost = 1,
		};

		Assert.False(ClimbRuleService.IsEncounterExpired(slot, 6));
		Assert.True(ClimbRuleService.IsEncounterExpired(slot, 7));

		slot.isFinal = true;
		Assert.False(ClimbRuleService.IsEncounterExpired(slot, ClimbRuleService.MaxTime));
	}

	[Fact]
	public void Replenish_encounters_rerolls_expired_normal_slots()
	{
		var state = new ClimbSaveState
		{
			time = 5,
			encounterSlots = new List<ClimbEncounterSlotSave>
			{
				new()
				{
					id = "encounter_a",
					enemyId = "skeleton",
					generatedAtTime = 0,
					duration = 2,
					timeCost = 1,
					rewardResources = new ClimbResourceSave { red = 1, white = 0, black = 0 },
				},
				new()
				{
					id = "encounter_b",
					enemyId = "demon",
					generatedAtTime = 5,
					duration = 5,
					timeCost = 1,
					rewardResources = new ClimbResourceSave { red = 0, white = 1, black = 0 },
				},
				new()
				{
					id = "encounter_c",
					enemyId = "cactus",
					generatedAtTime = 5,
					duration = 5,
					timeCost = 1,
					rewardResources = new ClimbResourceSave { red = 0, white = 0, black = 1 },
				},
			},
		};

		Assert.True(ClimbRuleService.ReplenishEncounterSlots(state, 123));

		var rerolled = state.encounterSlots.Single(slot => slot.id == "encounter_a");
		Assert.Equal(state.time, rerolled.generatedAtTime);
		Assert.InRange(rerolled.duration, ClimbRuleService.EncounterMinDuration, ClimbRuleService.EncounterMaxDuration);
		Assert.False(ClimbRuleService.IsEncounterExpired(rerolled, state.time));
	}

	[Fact]
	public void Event_slots_assign_distinct_hidden_events_with_valid_windows()
	{
		var state = ClimbRuleService.CreateInitialState(123, TestLoadout());

		Assert.Equal(state.eventSlots.Count, state.eventSlots.Select(s => s.eventTypeId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
		foreach (var slot in state.eventSlots)
		{
			Assert.False(slot.seen);
			Assert.InRange(slot.visibleStartTime - slot.generatedAtTime, 3, 8);
			Assert.InRange(slot.visibleEndTime - slot.visibleStartTime, 3, 6);
			Assert.InRange(slot.timeCost, 1, 2);
		}
	}

	[Fact]
	public void Time_clamps_at_final_encounter_boundary()
	{
		var state = ClimbRuleService.CreateInitialState(123, TestLoadout());

		int applied = ClimbRuleService.ApplyTime(state, 99);

		Assert.Equal(32, ClimbRuleService.MaxTime);
		Assert.Equal(ClimbRuleService.MaxTime, state.time);
		Assert.Equal(ClimbRuleService.MaxTime, applied);
		Assert.True(ClimbRuleService.HasPendingFinalEncounter(state));
	}

	[Fact]
	public void Shop_refresh_triggers_when_time_crosses_eight_time_boundaries()
	{
		Assert.False(ClimbRuleService.ShouldRefreshShopAtTime(0, 7));
		Assert.True(ClimbRuleService.ShouldRefreshShopAtTime(7, 8));
		Assert.True(ClimbRuleService.ShouldRefreshShopAtTime(7, 10));
		Assert.False(ClimbRuleService.ShouldRefreshShopAtTime(8, 9));
		Assert.True(ClimbRuleService.ShouldRefreshShopAtTime(15, 16));
		Assert.True(ClimbRuleService.ShouldRefreshShopAtTime(23, 24));
		Assert.False(ClimbRuleService.ShouldRefreshShopAtTime(24, 31));
		Assert.False(ClimbRuleService.ShouldRefreshShopAtTime(30, ClimbRuleService.MaxTime));
		Assert.False(ClimbRuleService.ShouldRefreshShopAtTime(32, 40));
	}

	[Fact]
	public void Event_visibility_marks_seen_and_expiration_completes_visible_events()
	{
		var state = new ClimbSaveState
		{
			time = 5,
			eventSlots = new List<ClimbEventSlotSave>
			{
				new()
				{
					id = "event",
					eventTypeId = "fountain",
					visibleStartTime = 5,
					visibleEndTime = 7
				}
			}
		};

		ClimbRuleService.MarkFirstVisibleEventsSeen(state);
		Assert.True(state.eventSlots[0].seen);
		Assert.Contains("fountain", state.shownEventTypeIds);

		state.time = 8;
		ClimbRuleService.ExpireEvents(state);
		Assert.True(state.eventSlots[0].isCompleted);
	}

	[Fact]
	public void Replenish_event_slots_stops_at_pool_exhaustion()
	{
		var state = new ClimbSaveState
		{
			time = 0,
			eventSlots = new List<ClimbEventSlotSave>(),
			shownEventTypeIds = EventFactory.GetAllEvents().Keys.ToList(),
		};

		ClimbRuleService.ReplenishEventSlots(state, 123);

		Assert.Empty(state.eventSlots);
	}

	[Fact]
	public void Resource_math_spends_only_when_affordable_and_adds_rewards()
	{
		var resources = new ClimbResourceSave { red = 1, white = 1, black = 0 };

		Assert.False(ClimbRuleService.TrySpend(resources, new ClimbResourceSave { red = 2, white = 0, black = 0 }));
		Assert.True(ClimbRuleService.TrySpend(resources, new ClimbResourceSave { red = 1, white = 0, black = 0 }));
		Assert.Equal(0, resources.red);

		ClimbRuleService.AddResources(resources, new ClimbResourceSave { red = 2, white = 0, black = 3 });
		Assert.Equal(2, resources.red);
		Assert.Equal(1, resources.white);
		Assert.Equal(3, resources.black);
	}

	[Fact]
	public void Refresh_shop_excludes_medals_and_equipment_once_shown()
	{
		var state = ClimbRuleService.CreateInitialState(123, TestLoadout());
		var firstMedal = state.shopSlots.FirstOrDefault(s => s.kind == ClimbShopSlotKinds.Medal)?.itemId;
		var firstEquipment = state.shopSlots.FirstOrDefault(s => s.kind == ClimbShopSlotKinds.Equipment)?.itemId;

		state.time = 8;
		ClimbRuleService.RefreshShopSlots(state, 123, TestLoadout());

		if (!string.IsNullOrWhiteSpace(firstMedal))
		{
			Assert.DoesNotContain(state.shopSlots, s => string.Equals(s.itemId, firstMedal, StringComparison.OrdinalIgnoreCase));
		}
		if (!string.IsNullOrWhiteSpace(firstEquipment))
		{
			Assert.DoesNotContain(state.shopSlots, s => string.Equals(s.itemId, firstEquipment, StringComparison.OrdinalIgnoreCase));
		}
	}

	[Fact]
	public void Shop_costs_use_item_specific_resource_pip_ranges()
	{
		var state = ClimbRuleService.CreateInitialState(123, TestLoadout());

		var medal = state.shopSlots.SingleOrDefault(slot => slot.kind == ClimbShopSlotKinds.Medal);
		var equipment = state.shopSlots.SingleOrDefault(slot => slot.kind == ClimbShopSlotKinds.Equipment);
		var upgrade = state.shopSlots.SingleOrDefault(slot => slot.kind == ClimbShopSlotKinds.Upgrade);
		var replacement = state.shopSlots.SingleOrDefault(slot => slot.kind == ClimbShopSlotKinds.Replacement);

		if (medal != null) Assert.InRange(ResourcePips(medal.cost), 4, 6);
		if (equipment != null) Assert.InRange(ResourcePips(equipment.cost), 6, 8);
		if (upgrade != null) Assert.InRange(ResourcePips(upgrade.cost), 1, 2);
		if (replacement != null) Assert.InRange(ResourcePips(replacement.cost), 1, 2);
	}

	[Fact]
	public void Climb_encounter_pool_excludes_banned_and_image_less_enemies()
	{
		var pool = ClimbRuleService.GetClimbEncounterEnemyPool();

		Assert.NotEmpty(pool);
		Assert.DoesNotContain("gleeber", pool);
		Assert.DoesNotContain("sand_corpse", pool);
		Assert.DoesNotContain("training_demon", pool);
		foreach (string enemyId in pool)
		{
			var enemy = EnemyFactory.Create(enemyId);
			Assert.NotNull(enemy);
			Assert.False(enemy.IsBoss);
			Assert.False(enemy.IsTutorialOnly);
			Assert.True(EnemyPortraitContent.HasPortrait(enemyId));
		}
	}

	private static LoadoutDefinition TestLoadout()
	{
		return new LoadoutDefinition
		{
			id = RunDeckService.PrimaryLoadoutId,
			cardIds = new List<string> { "smite|White", "fervor|Red", "reckoning|Black" },
			weaponId = "sword",
			medalIds = new List<string>(),
		};
	}

	private static int ResourcePips(ClimbResourceSave resources)
	{
		return Math.Max(0, resources?.red ?? 0)
			+ Math.Max(0, resources?.white ?? 0)
			+ Math.Max(0, resources?.black ?? 0);
	}
}
