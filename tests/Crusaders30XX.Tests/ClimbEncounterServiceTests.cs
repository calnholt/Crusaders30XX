using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Xunit;

namespace Crusaders30XX.Tests;

public class ClimbEncounterServiceTests
{
	[Fact]
	public void Queue_encounter_creates_exactly_one_existing_queued_encounter()
	{
		EventManager.Clear();
		try
		{
			PrepareRunWithEncounter(timeCost: 4);
			var world = new World();
			int battleTransitions = 0;
			EventManager.Subscribe<ShowTransition>(evt =>
			{
				if (evt.Scene == SceneId.Battle) battleTransitions++;
			});

			Assert.True(ClimbEncounterService.TryQueueEncounter(world.EntityManager, "encounter_a"));

			var queued = world.EntityManager.GetEntitiesWithComponent<QueuedEvents>().Single().GetComponent<QueuedEvents>();
			Assert.True(queued.IsClimbEncounter);
			Assert.Equal("encounter_a", queued.ClimbEncounterSlotId);
			Assert.Single(queued.Events);
			Assert.Equal("skeleton", queued.Events[0].EventId);
			Assert.Equal(1, battleTransitions);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Complete_queued_encounter_advances_time_grants_resources_and_persists_reward()
	{
		EventManager.Clear();
		try
		{
			PrepareRunWithEncounter(timeCost: 5);
			var world = new World();
			Assert.True(ClimbEncounterService.TryQueueEncounter(world.EntityManager, "encounter_a"));
			world.EntityManager.GetEntitiesWithComponent<QueuedEvents>().Single().GetComponent<QueuedEvents>().CurrentIndex = 0;

			var result = ClimbEncounterService.CompleteQueuedEncounter(world.EntityManager);

			var climb = SaveCache.GetClimbState();
			Assert.True(result.Completed);
			Assert.Equal(5, climb.time);
			Assert.Equal(2, climb.resources.red);
			Assert.Equal(1, climb.resources.white);
			Assert.True(climb.encounterSlots.Single(s => s.id == "encounter_a").isCompleted);
			Assert.NotNull(climb.pendingEncounterReward);
			Assert.Equal("encounter_a", climb.pendingEncounterReward.encounterSlotId);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Complete_resources_only_encounter_persists_resources_without_deck_offer()
	{
		EventManager.Clear();
		try
		{
			PrepareRunWithEncounter(timeCost: 5);
			var world = new World();
			Assert.True(ClimbEncounterService.TryQueueEncounter(world.EntityManager, "encounter_a"));
			world.EntityManager.GetEntitiesWithComponent<QueuedEvents>().Single().GetComponent<QueuedEvents>().CurrentIndex = 0;

			var result = ClimbEncounterService.CompleteQueuedEncounter(world.EntityManager);
			var climb = SaveCache.GetClimbState();

			Assert.True(result.Completed);
			Assert.Null(result.DeckRewardOffer);
			Assert.Null(SaveCache.GetPendingDeckRewardOffer());
			Assert.NotNull(climb.pendingEncounterReward);
			Assert.Null(climb.pendingEncounterReward.deckRewardOffer);
			Assert.Equal(1, climb.pendingEncounterReward.resources.red);
			Assert.Equal(0, climb.pendingEncounterReward.resources.white);
			Assert.Equal(0, climb.pendingEncounterReward.resources.black);
			Assert.True(RewardModalDisplaySystem.HasClimbResourceReward(climb.pendingEncounterReward.resources));
			Assert.Equal("+1 RED", RewardModalDisplaySystem.BuildClimbResourceRewardText(climb.pendingEncounterReward.resources));
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Resolve_pending_reward_clears_save_and_queues_final_encounter_when_time_is_capped()
	{
		EventManager.Clear();
		try
		{
			PrepareRunWithEncounter(timeCost: 1);
			var climb = SaveCache.GetClimbState();
			climb.time = ClimbRuleService.MaxTime - 1;
			climb.encounterSlots.Single(s => s.id == "encounter_a").rewardResources = new ClimbResourceSave { red = 0, white = 1, black = 1 };
			SaveCache.SaveClimbState(climb);

			var world = new World();
			Assert.True(ClimbEncounterService.TryQueueEncounter(world.EntityManager, "encounter_a"));
			world.EntityManager.GetEntitiesWithComponent<QueuedEvents>().Single().GetComponent<QueuedEvents>().CurrentIndex = 0;
			var result = ClimbEncounterService.CompleteQueuedEncounter(world.EntityManager);
			Assert.True(result.PendingFinalEncounter);

			ShowTransition transition = null;
			EventManager.Subscribe<ShowTransition>(evt => transition = evt);

			Assert.True(ClimbEncounterService.ResolvePendingEncounterReward(world.EntityManager));

			var after = SaveCache.GetClimbState();
			var queued = world.EntityManager.GetEntitiesWithComponent<QueuedEvents>().Single().GetComponent<QueuedEvents>();
			Assert.Null(after.pendingEncounterReward);
			Assert.True(queued.IsClimbEncounter);
			Assert.Equal("final", queued.ClimbEncounterSlotId);
			Assert.Single(queued.Events);
			Assert.Equal("fallen_shepherd", queued.Events[0].EventId);
			Assert.NotNull(transition);
			Assert.Equal(SceneId.Battle, transition.Scene);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Completing_last_available_ordinary_encounter_replenishes_slots_before_final_time()
	{
		EventManager.Clear();
		try
		{
			PrepareRunWithEncounter(timeCost: 1);
			var world = new World();

			CompleteEncounter(world, "encounter_a");
			CompleteEncounter(world, "encounter_b");
			CompleteEncounter(world, "encounter_c");

			var climb = SaveCache.GetClimbState();
			Assert.Equal(ClimbRuleService.EncounterSlotCount, climb.encounterSlots.Count);
			Assert.All(climb.encounterSlots, slot =>
			{
				Assert.False(slot.isCompleted);
				Assert.False(slot.isFinal);
				Assert.False(string.IsNullOrWhiteSpace(slot.enemyId));
			});
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Replenishing_encounters_at_max_time_rolls_final_slots()
	{
		var climb = new ClimbSaveState
		{
			time = ClimbRuleService.MaxTime,
			encounterSlots = new List<ClimbEncounterSlotSave>
			{
				new() { id = "encounter_a", enemyId = "skeleton", isCompleted = true },
				new() { id = "encounter_b", enemyId = "gleeber", isCompleted = true },
				new() { id = "encounter_c", enemyId = "skeleton", isCompleted = true },
			}
		};

		Assert.True(ClimbRuleService.ReplenishEncounterSlots(climb, 123));

		Assert.Equal(ClimbRuleService.EncounterSlotCount, climb.encounterSlots.Count);
		Assert.All(climb.encounterSlots, slot =>
		{
			Assert.True(slot.isFinal);
			Assert.False(slot.isCompleted);
			Assert.Equal("fallen_shepherd", slot.enemyId);
			Assert.Equal(0, slot.timeCost);
			Assert.False(slot.hasDeckReward);
		});
	}

	private static void PrepareRunWithEncounter(int timeCost)
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		loadout.cardIds = new List<string> { "smite|White", "fervor|Red", "reckoning|Black" };
		loadout.weaponId = "sword";
		loadout.medalIds = new List<string>();
		SaveCache.SaveLoadout(loadout);

		var climb = SaveCache.GetClimbState();
		climb.time = 0;
		climb.resources = new ClimbResourceSave { red = 1, white = 1, black = 1 };
		climb.encounterSlots = new List<ClimbEncounterSlotSave>
		{
			new()
			{
				id = "encounter_a",
				enemyId = "skeleton",
				timeCost = timeCost,
				rewardResources = new ClimbResourceSave { red = 1, white = 0, black = 0 },
				hasDeckReward = false,
			},
			new() { id = "encounter_b", enemyId = "gleeber", timeCost = 3, hasDeckReward = false },
			new() { id = "encounter_c", enemyId = "skeleton", timeCost = 3, hasDeckReward = false },
		};
		SaveCache.SaveClimbState(climb);
	}

	private static void CompleteEncounter(World world, string encounterSlotId)
	{
		Assert.True(ClimbEncounterService.TryQueueEncounter(world.EntityManager, encounterSlotId));
		world.EntityManager.GetEntitiesWithComponent<QueuedEvents>().Single().GetComponent<QueuedEvents>().CurrentIndex = 0;
		var result = ClimbEncounterService.CompleteQueuedEncounter(world.EntityManager);
		Assert.True(result.Completed);
	}
}
