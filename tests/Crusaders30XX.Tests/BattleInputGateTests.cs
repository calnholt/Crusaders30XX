using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Xunit;

namespace Crusaders30XX.Tests;

public class BattleInputGateTests
{
	[Fact]
	public void IsBattleInputFrozen_returns_true_when_defeat_presentation_active()
	{
		var entityManager = new EntityManager();
		var phaseEntity = entityManager.CreateEntity("PhaseState");
		entityManager.AddComponent(phaseEntity, new PhaseState { DefeatPresentationActive = true });

		try
		{
			Assert.True(BattleInputGate.IsBattleInputFrozen(entityManager));
		}
		finally
		{
			StateSingleton.IsActive = false;
		}
	}

	[Fact]
	public void IsBattleInputFrozen_returns_true_when_transition_active()
	{
		var entityManager = new EntityManager();
		var phaseEntity = entityManager.CreateEntity("PhaseState");
		entityManager.AddComponent(phaseEntity, new PhaseState { DefeatPresentationActive = false });

		StateSingleton.IsActive = true;
		try
		{
			Assert.True(BattleInputGate.IsBattleInputFrozen(entityManager));
		}
		finally
		{
			StateSingleton.IsActive = false;
		}
	}

	[Fact]
	public void IsBattleInputFrozen_returns_false_when_neither_gate_is_active()
	{
		var entityManager = new EntityManager();
		var phaseEntity = entityManager.CreateEntity("PhaseState");
		entityManager.AddComponent(phaseEntity, new PhaseState { DefeatPresentationActive = false });

		try
		{
			Assert.False(BattleInputGate.IsBattleInputFrozen(entityManager));
		}
		finally
		{
			StateSingleton.IsActive = false;
		}
	}

	[Fact]
	public void IsBattleInputFrozen_returns_true_when_enemy_hp_is_zero()
	{
		var entityManager = new EntityManager();
		var phaseEntity = entityManager.CreateEntity("PhaseState");
		entityManager.AddComponent(phaseEntity, new PhaseState { DefeatPresentationActive = false });
		var enemy = entityManager.CreateEntity("Enemy");
		entityManager.AddComponent(enemy, new Enemy());
		entityManager.AddComponent(enemy, new HP { Max = 30, Current = 0 });

		try
		{
			Assert.True(BattleInputGate.IsBattleInputFrozen(entityManager));
		}
		finally
		{
			StateSingleton.IsActive = false;
		}
	}
}
