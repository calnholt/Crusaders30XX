using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using Xunit;

namespace Crusaders30XX.Tests;

public class PlayerHudHealthDisplaySystemTests
{
	[Fact]
	public void Normal_health_preserves_fraction_and_percentages()
	{
		var state = PlayerHudHealthRendering.BuildRenderState(18, 20, 0.75f, 3, 0.20f);

		Assert.Equal(18, state.Current);
		Assert.Equal(20, state.Max);
		Assert.Equal("18/20", state.FractionText);
		Assert.Equal(0.9f, state.TargetPercent, 3);
		Assert.Equal(0.75f, state.DisplayedPercent, 3);
		Assert.Equal(3, state.IncomingDamage);
		Assert.False(state.IsLowHealth);
	}

	[Fact]
	public void Zero_health_has_empty_fill_and_remains_visible_as_fraction()
	{
		var state = PlayerHudHealthRendering.BuildRenderState(0, 20, 0f, 10, 0.20f);

		Assert.Equal("0/20", state.FractionText);
		Assert.Equal(0f, state.TargetPercent);
		Assert.Equal(0, state.IncomingDamage);
		Assert.True(state.IsLowHealth);
	}

	[Fact]
	public void Over_max_health_clamps_fill_without_clamping_fraction()
	{
		var state = PlayerHudHealthRendering.BuildRenderState(30, 20, 1.4f, 0, 0.20f);

		Assert.Equal("30/20", state.FractionText);
		Assert.Equal(1f, state.TargetPercent);
		Assert.Equal(1f, state.DisplayedPercent);
		Assert.False(state.IsLowHealth);
	}

	[Fact]
	public void Low_health_uses_threshold_and_flash_alpha_controls()
	{
		var state = PlayerHudHealthRendering.BuildRenderState(4, 20, 0.2f, 0, 0.20f);
		float alpha = PlayerHudHealthRendering.CalculatePulseAlpha(0f, 2f, 0.35f, 1f);

		Assert.True(state.IsLowHealth);
		Assert.Equal(0.675f, alpha, 3);
	}

	[Fact]
	public void Lethal_damage_preview_equals_current_health()
	{
		var entityManager = new EntityManager();
		AddActiveAttack(entityManager, "lethal", 8);

		Assert.Equal(8, PlayerHudHealthRendering.CalculateTotalIncomingDamage(entityManager, 8));
	}

	[Fact]
	public void Overkill_damage_preview_is_clamped_to_current_health()
	{
		var entityManager = new EntityManager();
		AddActiveAttack(entityManager, "overkill", 50);

		Assert.Equal(8, PlayerHudHealthRendering.CalculateTotalIncomingDamage(entityManager, 8));
	}

	[Fact]
	public void Multiple_enemies_sum_only_their_active_attacks()
	{
		var entityManager = new EntityManager();
		AddAttackIntent(entityManager, "enemy-a", "active-a", "future-a");
		AddAttackIntent(entityManager, "enemy-b", "active-b");
		AddProgress(entityManager, "active-a", 3);
		AddProgress(entityManager, "active-b", 5);
		AddProgress(entityManager, "future-a", 20);
		AddProgress(entityManager, "orphan", 20);

		Assert.Equal(8, PlayerHudHealthRendering.CalculateTotalIncomingDamage(entityManager, 20));
	}

	[Fact]
	public void Track_bounds_use_layout_owned_health_geometry()
	{
		var anchor = new PlayerHudAnchor
		{
			HealthPaddingLeft = 14,
			HealthPaddingRight = 18,
			HealthPaddingVertical = 8,
			HealthTrackHeight = 26,
			ContentGap = 8,
		};

		var track = PlayerHudHealthRendering.CalculateTrackBounds(
			new Rectangle(100, 200, 500, 44),
			anchor,
			20f);

		Assert.Equal(new Rectangle(142, 209, 440, 26), track);
	}

	[Fact]
	public void Legacy_hp_renderer_excludes_only_player_entities()
	{
		var player = new Entity(1);
		player.AddComponent(new Player());
		player.AddComponent(new HP());
		var enemy = new Entity(2);
		enemy.AddComponent(new Enemy());
		enemy.AddComponent(new HP());
		var temporary = new Entity(3);
		temporary.AddComponent(new HP());

		Assert.False(HPDisplaySystem.ShouldRenderLegacyHp(player));
		Assert.True(HPDisplaySystem.ShouldRenderLegacyHp(enemy));
		Assert.True(HPDisplaySystem.ShouldRenderLegacyHp(temporary));
	}

	private static void AddActiveAttack(EntityManager entityManager, string contextId, int damage)
	{
		AddAttackIntent(entityManager, $"enemy-{contextId}", contextId);
		AddProgress(entityManager, contextId, damage);
	}

	private static void AddAttackIntent(
		EntityManager entityManager,
		string entityName,
		params string[] contextIds)
	{
		var enemy = entityManager.CreateEntity(entityName);
		var intent = new AttackIntent();
		foreach (string contextId in contextIds)
		{
			intent.Planned.Add(new PlannedAttack { ContextId = contextId });
		}
		entityManager.AddComponent(enemy, intent);
	}

	private static void AddProgress(EntityManager entityManager, string contextId, int damage)
	{
		var progressEntity = entityManager.CreateEntity($"progress-{contextId}");
		entityManager.AddComponent(progressEntity, new EnemyAttackProgress
		{
			ContextId = contextId,
			ActualDamage = damage,
		});
	}
}
