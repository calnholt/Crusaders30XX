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
		var swordSingleCopyPool = StartingDeckGeneratorService.GetSwordSingleCopyStarterCardPool();
		var daggerSingleCopyPool = StartingDeckGeneratorService.GetDaggerSingleCopyStarterCardPool();

		Assert.NotEmpty(swordPool);
		Assert.NotEmpty(daggerPool);
		Assert.Contains("fervor", swordPool);
		Assert.Contains("sacrifice", daggerPool);
		foreach (var cardId in swordPool
			.Concat(daggerPool)
			.Concat(swordSingleCopyPool)
			.Concat(daggerSingleCopyPool))
		{
			Assert.NotNull(CardFactory.Create(cardId));
		}
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
	[InlineData(RunDifficulty.Easy, 16)]
	[InlineData(RunDifficulty.Normal, 18)]
	[InlineData(RunDifficulty.Hard, 20)]
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
