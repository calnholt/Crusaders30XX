using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class EnemyFactoryTests
{
	[Fact]
	public void Fallen_shepherd_is_registered_as_a_boss()
	{
		var enemy = EnemyFactory.Create("fallen_shepherd", EnemyDifficulty.Hard);
		var allEnemies = EnemyFactory.GetAllEnemies(EnemyDifficulty.Hard);

		var shepherd = Assert.IsType<FallenShepherd>(enemy);
		Assert.Equal(EnemyDifficulty.Hard, shepherd.Difficulty);
		Assert.True(shepherd.IsBoss);
		Assert.IsType<FallenShepherd>(allEnemies["fallen_shepherd"]);
	}

	[Fact]
	public void Normal_run_map_pool_contains_only_registered_non_boss_enemies()
	{
		var pool = EnemyPortraitContent.GetRunMapEnemyPool();

		Assert.NotEmpty(pool);
		Assert.DoesNotContain("fallen_shepherd", pool);
		foreach (string enemyId in pool)
		{
			var enemy = EnemyFactory.Create(enemyId);
			Assert.NotNull(enemy);
			Assert.False(enemy.IsBoss, $"{enemyId} is marked as a boss");
			Assert.True(EnemyPortraitContent.HasPortrait(enemyId));
		}
	}

	[Fact]
	public void Fallen_shepherd_spawns_with_phase_one_arsenal()
	{
		var world = CreateWorldWithDeck();

		var enemyEntity = EntityFactory.CreateEnemyFromId(
			world,
			"fallen_shepherd",
			world.EntityManager,
			EnemyDifficulty.Medium);

		var enemy = enemyEntity.GetComponent<Enemy>();
		var arsenal = enemyEntity.GetComponent<EnemyArsenal>();
		Assert.Equal("fallen_shepherd", enemy.Id);
		Assert.IsType<FallenShepherd>(enemy.EnemyBase);
		Assert.Equal(EnemyDifficulty.Medium, enemy.EnemyBase.Difficulty);
		Assert.Equal(new[] { "fallen_shepherd_phase_1" }, arsenal.AttackIds);
	}

	[Fact]
	public void Unknown_enemy_id_throws_a_descriptive_exception()
	{
		var world = new World();

		var exception = Assert.Throws<InvalidOperationException>(() =>
			EntityFactory.CreateEnemyFromId(world, "missing_enemy", world.EntityManager));

		Assert.Contains("missing_enemy", exception.Message, StringComparison.Ordinal);
	}

	private static World CreateWorldWithDeck()
	{
		var world = new World();
		var deckEntity = world.CreateEntity("Deck");
		var deck = new Deck();
		world.AddComponent(deckEntity, deck);
		for (int i = 0; i < 20; i++)
		{
			deck.Cards.Add(world.CreateEntity($"DeckCard_{i}"));
		}
		return world;
	}
}
