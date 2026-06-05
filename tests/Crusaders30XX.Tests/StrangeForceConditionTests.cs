using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Systems;
using Xunit;

namespace Crusaders30XX.Tests;

public class StrangeForceConditionTests
{
    [Fact]
    public void Condition_requires_two_distinct_blocking_colors()
    {
        var entityManager = new EntityManager();
        var attack = new StrangeForce();
        var progress = new EnemyAttackProgress { PlayedRed = 1, PlayedWhite = 0 };

        Assert.False(ConditionService.Evaluate(attack.ConditionType, entityManager, progress));

        progress.PlayedWhite = 1;

        Assert.True(ConditionService.Evaluate(attack.ConditionType, entityManager, progress));

        progress.PlayedRed = 2;
        progress.PlayedWhite = 0;

        Assert.False(ConditionService.Evaluate(attack.ConditionType, entityManager, progress));
    }
}
