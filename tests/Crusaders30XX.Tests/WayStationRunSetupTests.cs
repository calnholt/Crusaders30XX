using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Singletons;
using Xunit;

namespace Crusaders30XX.Tests;

public class WayStationRunSetupTests
{
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
		var world = new World();
		var deckEntity = world.CreateEntity("Deck");
		var deck = new Deck();
		world.AddComponent(deckEntity, deck);
		for (int i = 0; i < 20; i++)
		{
			deck.Cards.Add(world.CreateEntity($"DeckCard_{i}"));
		}

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
}
