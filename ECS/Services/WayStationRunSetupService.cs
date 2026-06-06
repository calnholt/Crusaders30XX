using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Services
{
	public static class WayStationRunSetupService
	{
		public static void Depart(World world)
		{
			if (world == null) return;

			RunDeckService.DestroyRunDeck(world.EntityManager);
			RunPlayerService.DestroyRunPlayer(world.EntityManager);
			SaveCache.StartNewRun();
			SaveCache.ConfigurePrimaryRunSetup(
				WayStationRunSetupSingleton.WeaponId,
				GetSelectedTemperanceId(),
				GetSelectedStarterPool());

			PrepareRunEntitiesForBattle(world);

			if (!SaveCache.IsStartQuestCompleted())
			{
				BeginStartQuestBattle(world);
			}
			else
			{
				EventManager.Publish(new ShowTransition { Scene = SceneId.Location, SkipHold = true });
			}
		}

		public static void BeginStartQuestBattle(World world)
		{
			if (world == null) return;
			if (SaveCache.IsStartQuestCompleted()) return;

			BeginBattleFromNodeId(world, SaveCache.GetStartNodeId());
		}

		public static void BeginBattleFromNodeId(World world, string nodeId)
		{
			if (world == null || string.IsNullOrEmpty(nodeId)) return;

			PrepareRunEntitiesForBattle(world);

			var tempPoi = world.EntityManager.CreateEntity("TempQuestBattleTrigger");
			world.EntityManager.AddComponent(tempPoi, new PointOfInterest { Id = nodeId });
			EventManager.Publish(new QuestSelectRequested { Entity = tempPoi });
			world.EntityManager.DestroyEntity(tempPoi.Id);
		}

		private static void PrepareRunEntitiesForBattle(World world)
		{
			var deckEntity = RunDeckService.EnsureRunDeck(world.EntityManager);
			var player = RunPlayerService.EnsureRunPlayer(world);
			var playerComponent = player?.GetComponent<Player>();
			if (playerComponent != null)
			{
				playerComponent.DeckEntity = deckEntity;
			}
			ApplySelectedPlayerHp(player);
		}

		public static void ApplySelectedPlayerHp(Entity player)
		{
			var hp = player?.GetComponent<HP>();
			if (hp == null) return;

			hp.Max = WayStationRunSetupSingleton.PlayerMaxHp;
			hp.Current = hp.Max;
		}

		private static System.Collections.Generic.IReadOnlyList<string> GetSelectedStarterPool()
		{
			return WayStationRunSetupSingleton.SelectedWeapon switch
			{
				StartingWeapon.Sword => StartingDeckGeneratorService.GetSwordStarterCardPool(),
				StartingWeapon.Dagger => StartingDeckGeneratorService.GetDaggerStarterCardPool(),
				_ => StartingDeckGeneratorService.GetSwordStarterCardPool()
			};
		}

		private static string GetSelectedTemperanceId()
		{
			return WayStationRunSetupSingleton.SelectedWeapon switch
			{
				StartingWeapon.Sword => "angelic_aura",
				StartingWeapon.Dagger => "measured_breath",
				_ => "angelic_aura"
			};
		}
	}
}
