using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class ClimbEventServiceTests
{
	[Fact]
	public void Update_lifecycle_marks_first_visible_event_seen()
	{
		PrepareRunWithEvent(new ClimbEventSlotSave
		{
			id = "event_a",
			eventTypeId = "icebound_tithe",
			generatedAtTime = 0,
			visibleStartTime = 3,
			visibleEndTime = 6,
			timeCost = 1,
		}, time: 3);

		Assert.True(ClimbEventService.UpdateLifecycle());

		var climb = SaveCache.GetClimbState();
		var slot = climb.eventSlots.Single(s => s.id == "event_a");
		Assert.True(slot.seen);
		Assert.Contains("icebound_tithe", climb.shownEventTypeIds);
	}

	[Fact]
	public void Update_lifecycle_expires_visible_events_without_repeating_them()
	{
		PrepareRunWithEvent(new ClimbEventSlotSave
		{
			id = "event_a",
			eventTypeId = "icebound_tithe",
			generatedAtTime = 0,
			visibleStartTime = 3,
			visibleEndTime = 6,
			timeCost = 1,
		}, time: 7);

		Assert.True(ClimbEventService.UpdateLifecycle());

		var climb = SaveCache.GetClimbState();
		Assert.Contains("icebound_tithe", climb.shownEventTypeIds);
		Assert.DoesNotContain(climb.eventSlots, s => s.eventTypeId == "icebound_tithe");
	}

	[Fact]
	public void Launch_active_event_advances_time_sets_pending_event_and_publishes_modal_event()
	{
		EventManager.Clear();
		try
		{
			PrepareRunWithEvent(new ClimbEventSlotSave
			{
				id = "event_a",
				eventTypeId = "icebound_tithe",
				generatedAtTime = 0,
				visibleStartTime = 3,
				visibleEndTime = 8,
				timeCost = 2,
			}, time: 3);
			var world = new World();
			ShowNarrativeEventOverlay published = null;
			EventManager.Subscribe<ShowNarrativeEventOverlay>(evt => published = evt);

			Assert.True(ClimbEventService.TryLaunchEvent(world.EntityManager, "event_a"));

			var climb = SaveCache.GetClimbState();
			Assert.Equal(5, climb.time);
			Assert.Contains("icebound_tithe", climb.shownEventTypeIds);
			Assert.NotNull(climb.pendingEvent);
			Assert.Equal("event_a", climb.pendingEvent.eventSlotId);
			Assert.Equal("icebound_tithe", climb.pendingEvent.eventTypeId);
			Assert.Contains(climb.eventSlots, s => s.id == "event_a" && !s.isCompleted);
			Assert.NotNull(published);
			Assert.Equal("icebound_tithe", published.EventTypeId);
			Assert.Equal(string.Empty, published.RunMapEventId);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Pending_event_completes_only_after_modal_choice_resolves()
	{
		EventManager.Clear();
		try
		{
			PrepareRunWithEvent(new ClimbEventSlotSave
			{
				id = "event_a",
				eventTypeId = "icebound_tithe",
				generatedAtTime = 0,
				visibleStartTime = 3,
				visibleEndTime = 8,
				timeCost = 2,
			}, time: 3);
			var world = new World();

			Assert.True(ClimbEventService.TryLaunchEvent(world.EntityManager, "event_a"));
			Assert.True(ClimbEventService.TryCompletePendingEvent(world.EntityManager, "icebound_tithe"));

			var climb = SaveCache.GetClimbState();
			Assert.Null(climb.pendingEvent);
			Assert.DoesNotContain(climb.eventSlots, s => s.id == "event_a");
			Assert.Contains("icebound_tithe", climb.shownEventTypeIds);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Pending_event_is_not_expired_if_modal_is_interrupted()
	{
		EventManager.Clear();
		try
		{
			PrepareRunWithEvent(new ClimbEventSlotSave
			{
				id = "event_a",
				eventTypeId = "icebound_tithe",
				generatedAtTime = 0,
				visibleStartTime = 3,
				visibleEndTime = 8,
				timeCost = 2,
			}, time: 7);
			var world = new World();

			Assert.True(ClimbEventService.TryLaunchEvent(world.EntityManager, "event_a"));
			ClimbEventService.UpdateLifecycle();

			var climb = SaveCache.GetClimbState();
			Assert.Equal(9, climb.time);
			Assert.NotNull(climb.pendingEvent);
			Assert.Contains(climb.eventSlots, s => s.id == "event_a" && !s.isCompleted);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Completing_event_that_caps_time_queues_final_encounter()
	{
		EventManager.Clear();
		try
		{
			PrepareRunWithEvent(new ClimbEventSlotSave
			{
				id = "event_a",
				eventTypeId = "icebound_tithe",
				generatedAtTime = 24,
				visibleStartTime = 30,
				visibleEndTime = ClimbRuleService.MaxTime,
				timeCost = 2,
			}, time: ClimbRuleService.MaxTime - 1);
			var world = new World();
			ShowTransition transition = null;
			EventManager.Subscribe<ShowTransition>(evt => transition = evt);

			Assert.True(ClimbEventService.TryLaunchEvent(world.EntityManager, "event_a"));
			Assert.True(ClimbEventService.TryCompletePendingEvent(world.EntityManager, "icebound_tithe"));

			var climb = SaveCache.GetClimbState();
			var queued = world.EntityManager.GetEntitiesWithComponent<QueuedEvents>().Single().GetComponent<QueuedEvents>();
			Assert.Equal(ClimbRuleService.MaxTime, climb.time);
			Assert.DoesNotContain(climb.encounterSlots, slot => slot.isFinal);
			Assert.True(queued.IsClimbEncounter);
			Assert.Equal("final", queued.ClimbEncounterSlotId);
			Assert.Single(queued.Events);
			Assert.Equal("fallen_shepherd", queued.Events[0].EventId);
			var pending = world.EntityManager.GetEntitiesWithComponent<QueuedEvents>().Single().GetComponent<PendingQuestDialog>();
			Assert.NotNull(pending);
			Assert.Equal("fallen_shepherd", pending.DialogId);
			Assert.Equal("intro", pending.SegmentId);
			Assert.NotNull(transition);
			Assert.Equal(SceneId.Battle, transition.Scene);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Launch_rejects_hidden_event_without_advancing_time()
	{
		EventManager.Clear();
		try
		{
			PrepareRunWithEvent(new ClimbEventSlotSave
			{
				id = "event_a",
				eventTypeId = "icebound_tithe",
				generatedAtTime = 0,
				visibleStartTime = 3,
				visibleEndTime = 8,
				timeCost = 2,
			}, time: 2);
			var world = new World();
			ShowNarrativeEventOverlay published = null;
			EventManager.Subscribe<ShowNarrativeEventOverlay>(evt => published = evt);

			Assert.False(ClimbEventService.TryLaunchEvent(world.EntityManager, "event_a"));

			var climb = SaveCache.GetClimbState();
			Assert.Equal(2, climb.time);
			Assert.Null(published);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	private static void PrepareRunWithEvent(ClimbEventSlotSave slot, int time)
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		loadout.cardIds = new List<string> { "smite|White", "fervor|Red", "reckoning|Black" };
		loadout.weaponId = "sword";
		loadout.medalIds = new List<string>();
		SaveCache.SaveLoadout(loadout);

		var climb = SaveCache.GetClimbState();
		climb.time = time;
		climb.eventSlots = new List<ClimbEventSlotSave> { slot };
		climb.shownEventTypeIds = new List<string>();
		climb.nextEventSlotId = 1;
		SaveCache.SaveClimbState(climb);
	}
}
