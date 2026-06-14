using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Services
{
	public static class TestFightSetupService
	{
		public static void PrepareWorld(World world)
		{
			if (world == null || !TestFightRuntime.IsActive) return;

			ApplyRunDifficulty(TestFightRuntime.Options.Difficulty);

			var deckEntity = world.EntityManager.GetEntity("Deck");
			if (deckEntity == null)
			{
				deckEntity = world.CreateEntity("Deck");
				world.AddComponent(deckEntity, new Deck());
				world.AddComponent(deckEntity, new DontDestroyOnLoad());
			}

			var player = world.EntityManager.GetEntity("Player");
			if (player == null)
			{
				player = EntityFactory.CreatePlayer(world, loadoutOverride: BuildLoadout());
			}

			player.GetComponent<Player>().DeckEntity = deckEntity;
			var hp = player.GetComponent<HP>();
			if (hp != null)
			{
				hp.Max = WayStationRunSetupSingleton.PlayerMaxHp;
				hp.Current = hp.Max;
			}

			var queuedEntity = world.EntityManager.GetEntity("QueuedEvents");
			if (queuedEntity == null)
			{
				queuedEntity = world.CreateEntity("QueuedEvents");
				world.AddComponent(queuedEntity, new DontDestroyOnLoad());
				world.AddComponent(queuedEntity, new QueuedEvents());
			}

			var queued = queuedEntity.GetComponent<QueuedEvents>();
			queued.Events.Clear();
			queued.Events.Add(new QueuedEvent
			{
				EventId = TestFightRuntime.Options.EnemyId,
				EventType = QueuedEventType.Enemy,
				Difficulty = EnemyDifficulty.Easy,
			});
			queued.CurrentIndex = -1;
			queued.LocationId = string.Empty;
			queued.QuestIndex = 0;
		}

		public static void RegenerateDeck(EntityManager entityManager)
		{
			if (entityManager == null || !TestFightRuntime.IsActive) return;

			var deckEntity = entityManager.GetEntity("Deck");
			var deck = deckEntity?.GetComponent<Deck>();
			if (deck == null)
			{
				throw new InvalidOperationException("Cannot start test fight: test deck is missing.");
			}

			ResetPlayerState(entityManager);
			DestroyExistingCards(entityManager, deck);

			int seed = TestFightRuntime.BeginBattle();
			var generatedKeys = StartingDeckGeneratorService.Generate(
				GetStarterPool(TestFightRuntime.Options.WeaponId),
				seed,
				GetSingleCopyPool(TestFightRuntime.Options.WeaponId));

			for (int i = 0; i < generatedKeys.Count; i++)
			{
				if (!RunDeckService.TryParseCardKey(generatedKeys[i], out var cardId, out var color))
				{
					continue;
				}

				var card = EntityFactory.CreateCardFromDefinition(
					entityManager,
					cardId,
					color,
					index: i);
				if (card == null) continue;

				var cardDefinition = card.GetComponent<CardData>()?.Card;
				if (cardDefinition != null)
				{
					cardDefinition.IsStarter = true;
				}
				deck.Cards.Add(card);
			}

			if (deck.Cards.Count == 0)
			{
				throw new InvalidOperationException("Cannot start test fight: generated deck is empty.");
			}
		}

		public static void ApplyEnemyHpDelta(Entity enemyEntity)
		{
			if (!TestFightRuntime.IsActive || enemyEntity == null) return;

			var enemy = enemyEntity.GetComponent<Enemy>();
			var hp = enemyEntity.GetComponent<HP>();
			var definition = enemy?.EnemyBase;
			if (enemy == null || hp == null || definition == null) return;

			int baselineMax = hp.Max;
			int adjustedMax = TestFightRuntime.ApplyHpDelta(baselineMax);
			int adjustedCurrent = Math.Clamp(hp.Current + TestFightRuntime.HpDelta, 1, adjustedMax);

			hp.Max = adjustedMax;
			hp.Current = adjustedCurrent;
			enemy.MaxHealth = adjustedMax;
			enemy.CurrentHealth = adjustedCurrent;
			definition.MaxHealth = adjustedMax;
			definition.CurrentHealth = adjustedCurrent;
		}

		public static void ResetEncounterQueue(EntityManager entityManager)
		{
			var queued = entityManager?.GetEntity("QueuedEvents")?.GetComponent<QueuedEvents>();
			if (queued != null)
			{
				queued.CurrentIndex = -1;
			}
		}

		private static void DestroyExistingCards(EntityManager entityManager, Deck deck)
		{
			var player = entityManager.GetEntity("Player");
			var equippedWeapon = player?.GetComponent<EquippedWeapon>();
			if (equippedWeapon?.SpawnedEntity != null)
			{
				entityManager.DestroyEntity(equippedWeapon.SpawnedEntity.Id);
				equippedWeapon.SpawnedEntity = null;
			}

			var cards = new HashSet<Entity>(deck.Cards);
			cards.UnionWith(deck.DrawPile);
			cards.UnionWith(deck.DiscardPile);
			cards.UnionWith(deck.ExhaustPile);
			cards.UnionWith(deck.Hand);
			foreach (var card in cards.Where(card => card != null).ToList())
			{
				entityManager.DestroyEntity(card.Id);
			}

			deck.Cards.Clear();
			deck.DrawPile.Clear();
			deck.DiscardPile.Clear();
			deck.ExhaustPile.Clear();
			deck.Hand.Clear();
		}

		private static void ResetPlayerState(EntityManager entityManager)
		{
			var player = entityManager.GetEntity("Player");
			if (player == null) return;

			var passives = player.GetComponent<AppliedPassives>()?.Passives;
			if (passives != null)
			{
				foreach (var passive in passives.Keys.ToList())
				{
					EventManager.Publish(new RemovePassive
					{
						Owner = player,
						Type = passive,
					});
				}
			}

			var battleState = player.GetComponent<BattleStateInfo>();
			battleState?.BattleTracking?.Clear();
			battleState?.TurnTracking?.Clear();
			battleState?.PhaseTracking?.Clear();

			EventManager.Publish(new SetTemperanceEvent { Amount = 0 });

			var hp = player.GetComponent<HP>();
			if (hp != null)
			{
				hp.Max = WayStationRunSetupSingleton.PlayerMaxHp;
				hp.Current = hp.Max;
			}
		}

		private static LoadoutDefinition BuildLoadout()
		{
			return new LoadoutDefinition
			{
				id = "test_fight",
				name = "Test Fight",
				weaponId = TestFightRuntime.Options.WeaponId,
				temperanceId = GetTemperanceId(TestFightRuntime.Options.WeaponId),
				chestId = string.Empty,
				legsId = string.Empty,
				armsId = string.Empty,
				headId = string.Empty,
				medalIds = new List<string>(),
			};
		}

		private static IReadOnlyList<string> GetStarterPool(string weaponId)
		{
			return weaponId switch
			{
				"sword" => StartingDeckGeneratorService.GetSwordStarterCardPool(),
				"dagger" => StartingDeckGeneratorService.GetDaggerStarterCardPool(),
				"hammer" => StartingDeckGeneratorService.GetHammerStarterCardPool(),
				_ => throw new InvalidOperationException($"Unsupported test-fight weapon '{weaponId}'."),
			};
		}

		private static IReadOnlyList<string> GetSingleCopyPool(string weaponId)
		{
			return weaponId switch
			{
				"sword" => StartingDeckGeneratorService.GetSwordSingleCopyStarterCardPool(),
				"dagger" => StartingDeckGeneratorService.GetDaggerSingleCopyStarterCardPool(),
				"hammer" => StartingDeckGeneratorService.GetHammerSingleCopyStarterCardPool(),
				_ => throw new InvalidOperationException($"Unsupported test-fight weapon '{weaponId}'."),
			};
		}

		private static string GetTemperanceId(string weaponId)
		{
			return StartingDeckGeneratorService.GetDefaultTemperanceId(weaponId);
		}

		private static void ApplyRunDifficulty(RunDifficulty difficulty)
		{
			WayStationRunSetupSingleton.SelectedDifficulty = difficulty;
			WayStationRunSetupSingleton.SelectedWeapon = TestFightRuntime.Options.WeaponId switch
			{
				"dagger" => StartingWeapon.Dagger,
				"hammer" => StartingWeapon.Hammer,
				_ => StartingWeapon.Sword,
			};
		}
	}
}
