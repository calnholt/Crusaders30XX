using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Singletons;
using Xunit;

namespace Crusaders30XX.Tests;

public class WayStationRunSetupTests
{
	[Fact]
	public void Depart_starts_new_run_on_climb_without_queueing_battle()
	{
		EventManager.Clear();
		try
		{
			SaveCache.DeleteSaveFilesIfPresent();
			var world = new World();
			ShowTransition transition = null;
			EventManager.Subscribe<ShowTransition>(evt => transition = evt);

			WayStationRunSetupService.Depart(world);

			Assert.True(SaveCache.IsRunActive());
			Assert.NotNull(transition);
			Assert.Equal(SceneId.Climb, transition.Scene);
			Assert.Empty(world.EntityManager.GetEntitiesWithComponent<QueuedEvents>());
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Weapon_starter_pools_only_reference_known_cards()
	{
		var swordPool = StartingDeckGeneratorService.GetSwordStarterCardPool();
		var daggerPool = StartingDeckGeneratorService.GetDaggerStarterCardPool();
		var hammerPool = StartingDeckGeneratorService.GetHammerStarterCardPool();
		var swordSingleCopyPool = StartingDeckGeneratorService.GetSwordSingleCopyStarterCardPool();
		var daggerSingleCopyPool = StartingDeckGeneratorService.GetDaggerSingleCopyStarterCardPool();
		var hammerSingleCopyPool = StartingDeckGeneratorService.GetHammerSingleCopyStarterCardPool();

		Assert.Equal(9, swordPool.Count);
		Assert.Equal(9, daggerPool.Count);
		Assert.Equal(9, hammerPool.Count);
		Assert.Contains("fervor", swordPool);
		Assert.Contains("seize", daggerPool);
		Assert.Contains("mantlet", hammerPool);
		Assert.DoesNotContain("exaltation", swordPool);
		Assert.DoesNotContain("razor_storm", daggerPool);
		Assert.DoesNotContain("unburdened_strike", hammerPool);
		foreach (var cardId in swordPool
			.Concat(daggerPool)
			.Concat(hammerPool)
			.Concat(swordSingleCopyPool)
			.Concat(daggerSingleCopyPool)
			.Concat(hammerSingleCopyPool))
		{
			Assert.NotNull(CardFactory.Create(cardId));
		}
	}

	[Fact]
	public void All_weapon_main_pools_include_shared_weapon_run_starter_cards()
	{
		foreach (var sharedCardId in StartingDeckGeneratorService.SharedWeaponRunStarterCardPool)
		{
			Assert.Contains(sharedCardId, StartingDeckGeneratorService.GetSwordStarterCardPool());
			Assert.Contains(sharedCardId, StartingDeckGeneratorService.GetDaggerStarterCardPool());
			Assert.Contains(sharedCardId, StartingDeckGeneratorService.GetHammerStarterCardPool());
		}
	}

	[Fact]
	public void Weapon_main_pools_do_not_overlap_single_copy_pools()
	{
		Assert.Empty(StartingDeckGeneratorService.GetSwordStarterCardPool()
			.Intersect(StartingDeckGeneratorService.GetSwordSingleCopyStarterCardPool(), System.StringComparer.OrdinalIgnoreCase));
		Assert.Empty(StartingDeckGeneratorService.GetDaggerStarterCardPool()
			.Intersect(StartingDeckGeneratorService.GetDaggerSingleCopyStarterCardPool(), System.StringComparer.OrdinalIgnoreCase));
		Assert.Empty(StartingDeckGeneratorService.GetHammerStarterCardPool()
			.Intersect(StartingDeckGeneratorService.GetHammerSingleCopyStarterCardPool(), System.StringComparer.OrdinalIgnoreCase));
	}

	[Theory]
	[InlineData(RunDifficulty.Easy, 25, 0.8f)]
	[InlineData(RunDifficulty.Normal, 22, 0.9f)]
	[InlineData(RunDifficulty.Hard, 20, 1.0f)]
	public void Difficulty_maps_to_player_hp_and_enemy_health_modifier(
		RunDifficulty difficulty,
		int expectedPlayerMaxHp,
		float expectedEnemyHealthModifier)
	{
		WayStationRunSetupSingleton.SelectedDifficulty = difficulty;

		Assert.Equal(expectedPlayerMaxHp, WayStationRunSetupSingleton.PlayerMaxHp);
		Assert.Equal(expectedEnemyHealthModifier, WayStationRunSetupSingleton.EnemyHealthModifier);
	}

	[Theory]
	[InlineData(RunDifficulty.Easy, 21)]
	[InlineData(RunDifficulty.Normal, 23)]
	[InlineData(RunDifficulty.Hard, 26)]
	public void Enemy_factory_scales_health_for_selected_run_difficulty(
		RunDifficulty difficulty,
		int expectedEnemyHp)
	{
		var world = PrepareWorldWithLoadout(Enumerable.Range(0, 20).Select(_ => "smite|White").ToList());

		WayStationRunSetupSingleton.SelectedDifficulty = difficulty;

		var enemyEntity = EntityFactory.CreateEnemyFromId(world, "skeleton", world.EntityManager);
		var enemy = enemyEntity.GetComponent<Enemy>();
		var hp = enemyEntity.GetComponent<HP>();

		Assert.Equal(expectedEnemyHp, enemy.MaxHealth);
		Assert.Equal(expectedEnemyHp, enemy.CurrentHealth);
		Assert.Equal(expectedEnemyHp, enemy.EnemyBase.MaxHealth);
		Assert.Equal(expectedEnemyHp, enemy.EnemyBase.CurrentHealth);
		Assert.Equal(expectedEnemyHp, hp.Max);
		Assert.Equal(expectedEnemyHp, hp.Current);
	}

	[Fact]
	public void Enemy_factory_adds_upgraded_card_weight_to_health()
	{
		var world = PrepareWorldWithLoadout(new List<string>
		{
			"smite|White|Upgraded",
			"fervor|Red",
			"reckoning|Black",
			"strike|Red",
		});
		WayStationRunSetupSingleton.SelectedDifficulty = RunDifficulty.Hard;

		var enemyEntity = EntityFactory.CreateEnemyFromId(world, "skeleton", world.EntityManager);

		Assert.Equal(6, enemyEntity.GetComponent<Enemy>().MaxHealth);
	}

	[Fact]
	public void Enemy_factory_adds_traded_card_weight_to_health()
	{
		var world = PrepareWorldWithLoadout(new List<string>
		{
			"smite|White",
			"fervor|Red",
			"reckoning|Black",
			"strike|Red",
		}, tradedKeys: new[] { "strike|Red" });
		WayStationRunSetupSingleton.SelectedDifficulty = RunDifficulty.Hard;

		var enemyEntity = EntityFactory.CreateEnemyFromId(world, "skeleton", world.EntityManager);

		Assert.Equal(6, enemyEntity.GetComponent<Enemy>().MaxHealth);
	}

	[Fact]
	public void Enemy_factory_counts_traded_upgraded_card_in_both_bonus_buckets()
	{
		var world = PrepareWorldWithLoadout(new List<string>
		{
			"smite|White",
			"fervor|Red",
			"reckoning|Black",
			"strike|Red|Upgraded",
		}, tradedKeys: new[] { "strike|Red|Upgraded" });
		WayStationRunSetupSingleton.SelectedDifficulty = RunDifficulty.Hard;

		var enemyEntity = EntityFactory.CreateEnemyFromId(world, "skeleton", world.EntityManager);

		Assert.Equal(7, enemyEntity.GetComponent<Enemy>().MaxHealth);
	}

	[Fact]
	public void Enemy_factory_applies_st_clare_to_base_card_count_before_bonuses()
	{
		var world = PrepareWorldWithLoadout(new List<string>
		{
			"smite|White|Upgraded",
			"fervor|Red",
			"reckoning|Black",
			"strike|Red",
		}, tradedKeys: new[] { "strike|Red" });
		WayStationRunSetupSingleton.SelectedDifficulty = RunDifficulty.Hard;
		RunPlayerService.EnsureRunPlayer(world);
		RunMedalService.AcquireAndEquip(world.EntityManager, "st_clare");

		var enemyEntity = EntityFactory.CreateEnemyFromId(world, "skeleton", world.EntityManager);

		Assert.Equal(2, enemyEntity.GetComponent<Enemy>().MaxHealth);
	}

	private static World PrepareWorldWithLoadout(IReadOnlyList<string> cardKeys, IReadOnlyList<string> tradedKeys = null)
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		loadout.cardIds = cardKeys.ToList();
		loadout.weaponId = "sword";
		loadout.medalIds = new List<string>();
		SaveCache.SaveLoadout(loadout);
		foreach (var tradedKey in tradedKeys ?? Array.Empty<string>())
		{
			SaveCache.MarkTradedCardKey(tradedKey);
		}

		var world = new World();
		var deckEntity = world.CreateEntity("Deck");
		var deck = new Deck();
		world.AddComponent(deckEntity, deck);
		for (int i = 0; i < cardKeys.Count; i++)
		{
			deck.Cards.Add(world.CreateEntity($"DeckCard_{i}"));
		}

		return world;
	}
}
