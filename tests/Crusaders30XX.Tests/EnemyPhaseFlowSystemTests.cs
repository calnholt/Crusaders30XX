using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Dialog;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Systems;
using Xunit;

namespace Crusaders30XX.Tests;

public class EnemyPhaseFlowSystemTests
{
	[Fact]
	public void Correlated_dialogue_controls_intermediate_resets_and_final_defeat()
	{
		EventManager.Clear();
		EventQueue.Clear();
		TimerScheduler.Clear();
		DialogDefinitionCache.Reload();

		try
		{
			var world = BuildWorld(out var phaseState, out var enemy, out var definition);
			_ = new EnemyPhaseFlowSystem(world.EntityManager);

			DialogueSequenceRequested request = null;
			int resetCount = 0;
			int defeatPresentationCount = 0;
			int enemyKilledCount = 0;
			int rewardCount = 0;
			int startBattleCount = 0;
			EventManager.Subscribe<DialogueSequenceRequested>(evt => request = evt);
			EventManager.Subscribe<EnemyPhaseResetEvent>(_ => resetCount++);
			EventManager.Subscribe<BeginDefeatPresentationEvent>(_ => defeatPresentationCount++);
			EventManager.Subscribe<EnemyKilledEvent>(_ => enemyKilledCount++);
			EventManager.Subscribe<ShowQuestRewardOverlay>(_ => rewardCount++);
			EventManager.Subscribe<StartBattleRequested>(_ => startBattleCount++);

			EventManager.Publish(new EnemyPhaseLethalEvent { Enemy = enemy });

			Assert.Equal("phase_1_end", request.SegmentId);
			Assert.Equal(1, definition.CurrentPhase);
			Assert.True(phaseState.DefeatPresentationActive);

			EventManager.Publish(new DialogueSequenceCompleted
			{
				DefinitionId = request.DefinitionId,
				SegmentId = request.SegmentId,
				RequestId = Guid.NewGuid(),
			});
			Assert.Equal(1, definition.CurrentPhase);

			Complete(request);
			Assert.Equal(2, definition.CurrentPhase);
			Assert.Equal(1, resetCount);
			Assert.False(phaseState.DefeatPresentationActive);
			Assert.Equal(9, phaseState.TurnNumber);

			request = null;
			EventManager.Publish(new EnemyPhaseLethalEvent { Enemy = enemy });
			Assert.Equal("phase_2_end", request.SegmentId);
			Complete(request);
			Assert.Equal(3, definition.CurrentPhase);
			Assert.Equal(2, resetCount);

			request = null;
			EventManager.Publish(new EnemyPhaseLethalEvent { Enemy = enemy });
			Assert.Equal("victory", request.SegmentId);
			Complete(request);

			Assert.Equal(0, defeatPresentationCount);
			Assert.True(phaseState.DefeatPresentationActive);
			TimerScheduler.Update(0.11f);
			Assert.Equal(1, defeatPresentationCount);
			Assert.Equal(0, enemyKilledCount);
			Assert.Equal(0, rewardCount);
			Assert.Equal(0, startBattleCount);
		}
		finally
		{
			EventManager.Clear();
			EventQueue.Clear();
			TimerScheduler.Clear();
		}
	}

	private static void Complete(DialogueSequenceRequested request)
	{
		EventManager.Publish(new DialogueSequenceCompleted
		{
			DefinitionId = request.DefinitionId,
			SegmentId = request.SegmentId,
			RequestId = request.RequestId,
		});
	}

	private static World BuildWorld(
		out PhaseState phaseState,
		out Entity enemy,
		out FallenShepherd definition)
	{
		var world = new World();
		var phaseEntity = world.CreateEntity("PhaseState");
		phaseState = new PhaseState
		{
			Main = MainPhase.PlayerTurn,
			Sub = SubPhase.Action,
			TurnNumber = 9,
		};
		world.AddComponent(phaseEntity, phaseState);

		var player = world.CreateEntity("Player");
		world.AddComponent(player, new Player());
		world.AddComponent(player, new HP { Max = 25, Current = 25 });
		world.AddComponent(player, new AppliedPassives());
		world.AddComponent(player, new ActionPoints());

		var deckEntity = world.CreateEntity("Deck");
		var deck = new Deck();
		world.AddComponent(deckEntity, deck);
		var card = world.CreateEntity("Card");
		world.AddComponent(card, new CardData());
		deck.Cards.Add(card);
		deck.DrawPile.Add(card);

		definition = new FallenShepherd
		{
			MaxHealth = 30,
			CurrentHealth = 0,
		};
		enemy = world.CreateEntity("Enemy");
		world.AddComponent(enemy, new Enemy
		{
			Id = definition.Id,
			Name = definition.Name,
			MaxHealth = 30,
			CurrentHealth = 0,
			EnemyBase = definition,
		});
		world.AddComponent(enemy, new HP { Max = 30, Current = 0 });
		world.AddComponent(enemy, new EnemyArsenal());
		world.AddComponent(enemy, new AttackIntent());
		world.AddComponent(enemy, new NextTurnAttackIntent());
		world.AddComponent(enemy, new AppliedPassives());
		return world;
	}
}
