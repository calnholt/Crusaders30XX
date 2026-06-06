using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;
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
