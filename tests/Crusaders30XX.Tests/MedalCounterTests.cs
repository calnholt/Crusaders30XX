using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Medals;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class MedalCounterTests
{
	[Fact]
	public void StBenedict_resets_counter_after_three_pledges()
	{
		EventManager.Clear();
		try
		{
			var entityManager = new EntityManager();
			var medal = new StBenedict();
			medal.Initialize(entityManager, entityManager.CreateEntity("Medal"));

			for (int i = 0; i < 2; i++)
			{
				EventManager.Publish(new PledgeAddedEvent { Card = entityManager.CreateEntity($"Card_{i}") });
			}

			Assert.Equal(2, medal.CurrentCount);

			EventManager.Publish(new PledgeAddedEvent { Card = entityManager.CreateEntity("Card_2") });

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
	public void StSimonOfCyrene_applies_anathema_on_start_battle()
	{
		EventManager.Clear();
		try
		{
			var entityManager = new EntityManager();
			var enemy = entityManager.CreateEntity("Enemy");
			entityManager.AddComponent(enemy, new AppliedPassives());
			var medal = new StSimonOfCyrene();
			medal.Initialize(entityManager, entityManager.CreateEntity("Medal"));

			var activateCount = 0;
			EventManager.Subscribe<MedalActivateEvent>(_ => activateCount++);
			var applied = new List<ApplyPassiveEvent>();
			EventManager.Subscribe<ApplyPassiveEvent>(evt => applied.Add(evt));

			EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.StartBattle });
			medal.Activate();

			Assert.Equal(1, activateCount);
			Assert.Single(applied);
			Assert.Same(enemy, applied[0].Target);
			Assert.Equal(AppliedPassiveType.Anathema, applied[0].Type);
			Assert.Equal(1, applied[0].Delta);
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
	public void StPeter_unsubscribes_on_dispose()
	{
		EventManager.Clear();
		try
		{
			var entityManager = new EntityManager();
			var medalEntity = entityManager.CreateEntity("Medal");
			var medal = new StPeter();
			medal.Initialize(entityManager, medalEntity);
			entityManager.AddComponent(medalEntity, new EquippedMedal { Medal = medal });

			var activateCount = 0;
			EventManager.Subscribe<MedalActivateEvent>(_ => activateCount++);

			var blackCard = entityManager.CreateEntity("BlackCard");
			entityManager.AddComponent(blackCard, new CardData { Color = CardData.CardColor.Black });

			EventManager.Publish(new CardBlockedEvent { Card = blackCard });
			EventManager.Publish(new CardBlockedEvent { Card = blackCard });
			EventManager.Publish(new CardBlockedEvent { Card = blackCard });
			Assert.Equal(1, activateCount);

			medal.Dispose();

			EventManager.Publish(new CardBlockedEvent { Card = blackCard });
			EventManager.Publish(new CardBlockedEvent { Card = blackCard });
			EventManager.Publish(new CardBlockedEvent { Card = blackCard });
			Assert.Equal(1, activateCount);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void StPeter_does_not_trigger_after_run_end()
	{
		EventManager.Clear();
		try
		{
			SaveCache.DeleteSaveFilesIfPresent();
			SaveCache.StartNewRun();

			var entityManager = new EntityManager();
			var player = entityManager.CreateEntity("Player");
			entityManager.AddComponent(player, new Player());

			RunMedalService.AcquireAndEquip(entityManager, "st_peter");

			var activateCount = 0;
			EventManager.Subscribe<MedalActivateEvent>(_ => activateCount++);

			RunLifecycleService.EndCurrentRun(entityManager);

			var blackCard = entityManager.CreateEntity("BlackCard");
			entityManager.AddComponent(blackCard, new CardData { Color = CardData.CardColor.Black });

			for (int i = 0; i < 3; i++)
			{
				EventManager.Publish(new CardBlockedEvent { Card = blackCard });
			}

			Assert.Equal(0, activateCount);
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
