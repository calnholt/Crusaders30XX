using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Medals;
using Xunit;

namespace Crusaders30XX.Tests;

public class MedalCounterTests
{
	[Fact]
	public void StBenedict_resets_counter_after_six_pledges()
	{
		EventManager.Clear();
		try
		{
			var entityManager = new EntityManager();
			var medal = new StBenedict();
			medal.Initialize(entityManager, entityManager.CreateEntity("Medal"));

			for (int i = 0; i < 5; i++)
			{
				EventManager.Publish(new PledgeAddedEvent { Card = entityManager.CreateEntity($"Card_{i}") });
			}

			Assert.Equal(5, medal.CurrentCount);

			EventManager.Publish(new PledgeAddedEvent { Card = entityManager.CreateEntity("Card_5") });

			Assert.Equal(0, medal.CurrentCount);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void StPaulMiki_resets_and_triggers_once_per_battle()
	{
		EventManager.Clear();
		try
		{
			var entityManager = new EntityManager();
			var medal = new StPaulMiki();
			medal.Initialize(entityManager, entityManager.CreateEntity("Medal"));

			EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.StartBattle });
			Assert.Equal(1, medal.CurrentCount);

			var blackCard = entityManager.CreateEntity("BlackCard");
			entityManager.AddComponent(blackCard, new CardData { Color = CardData.CardColor.Black });

			EventManager.Publish(new CardBlockedEvent { Card = blackCard });
			Assert.Equal(0, medal.CurrentCount);

			EventManager.Publish(new CardBlockedEvent { Card = blackCard });
			Assert.Equal(0, medal.CurrentCount);

			EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.StartBattle });
			Assert.Equal(1, medal.CurrentCount);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void StSimonOfCyrene_decrements_remaining_battles_on_start_battle()
	{
		EventManager.Clear();
		try
		{
			var entityManager = new EntityManager();
			var medal = new StSimonOfCyrene();
			medal.Initialize(entityManager, entityManager.CreateEntity("Medal"));
			medal.OnAcquire();

			Assert.Equal(3, medal.CurrentCount);

			for (int expectedRemaining = 2; expectedRemaining >= 0; expectedRemaining--)
			{
				EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.StartBattle });
				medal.Activate();
				Assert.Equal(expectedRemaining, medal.CurrentCount);
			}
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void StHomobonus_grants_climb_resources_after_three_encounters()
	{
		EventManager.Clear();
		try
		{
			SaveCache.DeleteSaveFilesIfPresent();
			SaveCache.StartNewRun();
			var climb = SaveCache.GetClimbState();
			climb.resources = new ClimbResourceSave { red = 1, white = 1, black = 1 };
			climb.pendingEncounterReward = new ClimbEncounterRewardSave
			{
				resources = new ClimbResourceSave { red = 0, white = 0, black = 0 },
			};
			SaveCache.SaveClimbState(climb);

			var entityManager = new EntityManager();
			var medal = new StHomobonus();
			medal.Initialize(entityManager, entityManager.CreateEntity("Medal"));

			for (int i = 0; i < 2; i++)
			{
				EventManager.Publish(new ShowQuestRewardOverlay { IsEncounterReward = true });
			}

			Assert.Equal(2, medal.CurrentCount);
			Assert.Equal(1, SaveCache.GetClimbState().resources.red);
			Assert.Equal(1, SaveCache.GetClimbState().resources.white);
			Assert.Equal(1, SaveCache.GetClimbState().resources.black);

			var thirdEncounterReward = new ShowQuestRewardOverlay
			{
				IsEncounterReward = true,
				ClimbResources = new ClimbResourceSave { red = 1, white = 0, black = 0 },
			};
			EventManager.Publish(thirdEncounterReward);

			Assert.Equal(0, medal.CurrentCount);
			Assert.Equal(2, SaveCache.GetClimbState().resources.red);
			Assert.Equal(2, SaveCache.GetClimbState().resources.white);
			Assert.Equal(2, SaveCache.GetClimbState().resources.black);
			Assert.Equal(2, thirdEncounterReward.ClimbResources.red);
			Assert.Equal(1, thirdEncounterReward.ClimbResources.white);
			Assert.Equal(1, thirdEncounterReward.ClimbResources.black);
			Assert.Equal(1, SaveCache.GetClimbState().pendingEncounterReward.resources.red);
			Assert.Equal(1, SaveCache.GetClimbState().pendingEncounterReward.resources.white);
			Assert.Equal(1, SaveCache.GetClimbState().pendingEncounterReward.resources.black);

			EventManager.Publish(new ShowQuestRewardOverlay { IsEncounterReward = false });
			Assert.Equal(0, medal.CurrentCount);
		}
		finally
		{
			EventManager.Clear();
			SaveCache.DeleteSaveFilesIfPresent();
		}
	}

	[Fact]
	public void StJerome_grants_courage_when_player_gains_aggression()
	{
		EventManager.Clear();
		try
		{
			var entityManager = new EntityManager();
			var player = entityManager.CreateEntity("Player");
			entityManager.AddComponent(player, new Player());
			entityManager.AddComponent(player, new Courage());

			var medal = new StJerome();
			medal.Initialize(entityManager, entityManager.CreateEntity("Medal"));

			var activateCount = 0;
			EventManager.Subscribe<MedalActivateEvent>(_ => activateCount++);

			EventManager.Publish(new ApplyPassiveEvent
			{
				Target = player,
				Type = AppliedPassiveType.Aggression,
				Delta = 3
			});
			Assert.Equal(1, activateCount);

			var enemy = entityManager.CreateEntity("Enemy");
			EventManager.Publish(new ApplyPassiveEvent
			{
				Target = enemy,
				Type = AppliedPassiveType.Aggression,
				Delta = 1
			});
			Assert.Equal(1, activateCount);

			EventManager.Publish(new ApplyPassiveEvent
			{
				Target = player,
				Type = AppliedPassiveType.Aggression,
				Delta = -1
			});
			Assert.Equal(1, activateCount);

			ModifyCourageRequestEvent courageEvent = null;
			EventManager.Subscribe<ModifyCourageRequestEvent>(evt => courageEvent = evt);
			medal.Activate();

			Assert.NotNull(courageEvent);
			Assert.Equal(ModifyCourageType.Gain, courageEvent.Type);
			Assert.Equal(1, courageEvent.Delta);
			Assert.Equal("st_jerome", courageEvent.Reason);

			EventManager.Publish(new ApplyPassiveEvent
			{
				Target = player,
				Type = AppliedPassiveType.Aggression,
				Delta = 2
			});
			Assert.Equal(2, activateCount);
		}
		finally
		{
			EventManager.Clear();
		}
	}
}
