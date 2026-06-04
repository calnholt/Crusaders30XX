using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Systems;
using Xunit;

namespace Crusaders30XX.Tests;

public class InfernalExecutionTests
{
    [Fact]
    public void Condition_requires_at_least_two_blocking_cards()
    {
        var entityManager = new EntityManager();
        var attack = new InfernalExecution();
        var progress = new EnemyAttackProgress { PlayedCards = 1 };

        Assert.False(ConditionService.Evaluate(attack.ConditionType, entityManager, progress));

        progress.PlayedCards = 2;

        Assert.True(ConditionService.Evaluate(attack.ConditionType, entityManager, progress));
    }
}
