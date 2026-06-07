using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using Xunit;

namespace Crusaders30XX.Tests;

public class PlayerHudIntegrationTests : IDisposable
{
	public PlayerHudIntegrationTests() => EventManager.Clear();
	public void Dispose() => EventManager.Clear();

	[Fact]
	public void Player_passives_anchor_below_hud_while_non_player_uses_hp_anchor()
	{
		var entityManager = BuildHud(out var player);
		var system = new AppliedPassivesDisplaySystem(entityManager, null, null);
		var playerTransform = player.GetComponent<Transform>();
		var hud = entityManager.GetEntitiesWithComponent<PlayerHudAnchor>()
			.Single()
			.GetComponent<PlayerHudAnchor>();

		Point playerAnchor = system.ResolvePassiveAnchor(player, playerTransform, true);

		Assert.Equal(hud.Bounds.Center.X, playerAnchor.X);
		Assert.Equal(hud.Bounds.Bottom + system.OffsetY, playerAnchor.Y);

		var enemy = entityManager.CreateEntity("Enemy");
		var enemyTransform = new Transform { Position = new Vector2(900, 300) };
		entityManager.AddComponent(enemy, enemyTransform);
		entityManager.AddComponent(enemy, new HPBarAnchor
		{
			Rect = new Rectangle(800, 350, 200, 20),
		});

		Point enemyAnchor = system.ResolvePassiveAnchor(enemy, enemyTransform, false);

		Assert.Equal(900, enemyAnchor.X);
		Assert.Equal(385, enemyAnchor.Y);
	}

	[Fact]
	public void Tribulation_anchor_uses_courage_region_right_edge()
	{
		var entityManager = BuildHud(out var player);
		var system = new QuestTribulationDisplaySystem(entityManager, null, null, null);
		var courage = entityManager.GetEntitiesWithComponent<PlayerHudRegion>()
			.Single(entity => entity.GetComponent<PlayerHudRegion>().Type == PlayerHudRegionType.Courage)
			.GetComponent<PlayerHudRegion>();

		Vector2? anchor = system.ResolveCourageAnchor(player);

		Assert.Equal(new Vector2(courage.Bounds.Right, courage.Bounds.Center.Y), anchor);
	}

	[Fact]
	public void Battle_exit_and_reentry_never_duplicates_hud_regions()
	{
		var entityManager = BuildHud(out _);
		var layout = new PlayerHudLayoutSystem(entityManager);
		Assert.Equal(6, entityManager.GetEntitiesWithComponent<PlayerHudRegion>().Count());

		SetScene(entityManager, SceneId.Location);
		layout.Update(new GameTime());
		Assert.Empty(entityManager.GetEntitiesWithComponent<PlayerHudRegion>());

		SetScene(entityManager, SceneId.Battle);
		layout.Update(new GameTime());
		layout.Update(new GameTime());

		Assert.Equal(6, entityManager.GetEntitiesWithComponent<PlayerHudRegion>().Count());
		Assert.Equal(
			6,
			entityManager.GetEntitiesWithComponent<PlayerHudRegion>()
				.Select(entity => entity.GetComponent<PlayerHudRegion>().Type)
				.Distinct()
				.Count());
	}

	private static EntityManager BuildHud(out Entity player)
	{
		var entityManager = new EntityManager();
		var scene = entityManager.CreateEntity("Scene");
		entityManager.AddComponent(scene, new SceneState { Current = SceneId.Battle });
		player = entityManager.CreateEntity("Player");
		entityManager.AddComponent(player, new Player());
		entityManager.AddComponent(player, new Transform { Position = new Vector2(500, 300) });
		entityManager.AddComponent(player, new PortraitInfo
		{
			TextureWidth = 200,
			TextureHeight = 400,
			BaseScale = 0.5f,
		});
		new PlayerHudLayoutSystem(entityManager).Update(new GameTime());
		return entityManager;
	}

	private static void SetScene(EntityManager entityManager, SceneId scene)
	{
		entityManager.GetEntitiesWithComponent<SceneState>()
			.Single()
			.GetComponent<SceneState>()
			.Current = scene;
	}
}
