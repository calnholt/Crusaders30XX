using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using System.Linq;
using Xunit;

namespace Crusaders30XX.Tests;

public class EnemyDefeatFlowSystemTests
{
	[Fact]
	public void Real_defeat_completion_keeps_portrait_suppressed()
	{
		EventManager.Clear();
		EventQueue.Clear();

		try
		{
			var world = BuildWorld(out var phaseState, out var enemy);
			_ = new EnemyDefeatFlowSystem(world.EntityManager, content: null);

			int enemyKilledCount = 0;
			int questRewardCount = 0;
			EventManager.Subscribe<EnemyKilledEvent>(_ => enemyKilledCount++);
			EventManager.Subscribe<ShowQuestRewardOverlay>(_ => questRewardCount++);

			EventManager.Publish(new BeginDefeatPresentationEvent { Enemy = enemy, IsPreview = false });

			Assert.True(enemy.HasComponent<SuppressPortraitRender>());
			Assert.False(phaseState.DefeatPresentationActive);
			Assert.Equal(1, enemyKilledCount);
			Assert.Equal(1, questRewardCount);
		}
		finally
		{
			EventManager.Clear();
			EventQueue.Clear();
		}
	}

	[Fact]
	public void Preview_defeat_completion_restores_portrait()
	{
		EventManager.Clear();
		EventQueue.Clear();

		try
		{
			var world = BuildWorld(out var phaseState, out var enemy);
			_ = new EnemyDefeatFlowSystem(world.EntityManager, content: null);

			EventManager.Publish(new BeginDefeatPresentationEvent { Enemy = enemy, IsPreview = true });

			Assert.False(enemy.HasComponent<SuppressPortraitRender>());
			Assert.False(phaseState.DefeatPresentationActive);
		}
		finally
		{
			EventManager.Clear();
			EventQueue.Clear();
		}
	}

	[Fact]
	public void Final_climb_encounter_victory_uses_run_victory_transition_without_reward()
	{
		EventManager.Clear();
		EventQueue.Clear();

		try
		{
			var world = BuildWorld(out var phaseState, out var enemy);
			var queued = world.EntityManager.GetEntity("QueuedEvents").GetComponent<QueuedEvents>();
			queued.IsClimbEncounter = true;
			queued.ClimbEncounterSlotId = "final";
			queued.Events[0].EventId = "fallen_shepherd";
			var enemyComponent = enemy.GetComponent<Enemy>();
			enemyComponent.Id = "fallen_shepherd";
			enemyComponent.Name = "Fallen Shepherd";
			enemyComponent.EnemyBase = EnemyFactory.Create("fallen_shepherd", EnemyDifficulty.Hard);
			_ = new EnemyDefeatFlowSystem(world.EntityManager, content: null);

			ShowTransition transition = null;
			int rewardCount = 0;
			EventManager.Subscribe<ShowTransition>(evt => transition = evt);
			EventManager.Subscribe<ShowQuestRewardOverlay>(_ => rewardCount++);

			EventManager.Publish(new BeginDefeatPresentationEvent { Enemy = enemy, IsPreview = false });

			Assert.False(phaseState.DefeatPresentationActive);
			Assert.Equal(0, rewardCount);
			Assert.NotNull(transition);
			Assert.Equal(SceneId.WayStation, transition.Scene);
			Assert.True(transition.EndRunOnLoad);
		}
		finally
		{
			EventManager.Clear();
			EventQueue.Clear();
		}
	}

	[Fact]
	public void Final_time_climb_encounter_reward_dismisses_to_battle()
	{
		EventManager.Clear();
		EventQueue.Clear();

		try
		{
			SaveCache.DeleteSaveFilesIfPresent();
			SaveCache.StartNewRun();
			var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
			loadout.cards = new System.Collections.Generic.List<LoadoutCardEntry>
			{
				new() { entryId = "test_entry_0", cardKey = "smite|White", isStarter = true },
			};
			loadout.weaponId = "sword";
			loadout.medalIds = new System.Collections.Generic.List<string>();
			SaveCache.SaveLoadout(loadout);

			var climb = SaveCache.GetClimbState();
			climb.time = ClimbRuleService.MaxTime - 1;
			climb.encounterSlots[0].timeCost = 1;
			SaveCache.SaveClimbState(climb);

			var world = new World();
			var queuedEntity = world.CreateEntity("QueuedEvents");
			var queued = new QueuedEvents
			{
				Events =
				{
					new QueuedEvent
					{
						EventId = "skeleton",
						EventType = QueuedEventType.Enemy,
						Difficulty = EnemyDifficulty.Easy,
					},
				},
				CurrentIndex = 0,
				IsClimbEncounter = true,
				ClimbEncounterSlotId = "encounter_0",
				LocationId = "climb",
			};
			world.AddComponent(queuedEntity, queued);

			var phaseEntity = world.CreateEntity("PhaseState");
			world.AddComponent(phaseEntity, new PhaseState
			{
				Main = MainPhase.PlayerTurn,
				Sub = SubPhase.Action,
				TurnNumber = 1,
			});

			var enemy = world.CreateEntity("Enemy");
			world.AddComponent(enemy, new Enemy { Id = "skeleton", Name = "Skeleton" });
			world.AddComponent(enemy, new Transform());

			_ = new EnemyDefeatFlowSystem(world.EntityManager, content: null);

			ShowQuestRewardOverlay reward = null;
			EventManager.Subscribe<ShowQuestRewardOverlay>(evt => reward = evt);

			EventManager.Publish(new BeginDefeatPresentationEvent { Enemy = enemy, IsPreview = false });

			Assert.NotNull(reward);
			Assert.True(reward.IsEncounterReward);
			Assert.Equal(SceneId.Battle, reward.DismissScene);
		}
		finally
		{
			EventManager.Clear();
			EventQueue.Clear();
		}
	}

	private static World BuildWorld(out PhaseState phaseState, out Entity enemy)
	{
		var world = new World();
		var phaseEntity = world.CreateEntity("PhaseState");
		phaseState = new PhaseState
		{
			Main = MainPhase.PlayerTurn,
			Sub = SubPhase.Action,
			TurnNumber = 1,
		};
		world.AddComponent(phaseEntity, phaseState);

		var queuedEntity = world.CreateEntity("QueuedEvents");
		var queued = new QueuedEvents
		{
			Events = { new QueuedEvent { EventId = "skeleton" } },
			CurrentIndex = 0,
		};
		world.AddComponent(queuedEntity, queued);

		enemy = world.CreateEntity("Enemy");
		world.AddComponent(enemy, new Enemy { Id = "skeleton", Name = "Skeleton" });
		world.AddComponent(enemy, new Transform());
		return world;
	}
}
