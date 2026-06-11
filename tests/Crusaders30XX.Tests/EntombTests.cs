using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Xunit;

namespace Crusaders30XX.Tests;

public class EntombTests : IDisposable
{
    public EntombTests()
    {
        EventManager.Clear();
    }

    public void Dispose()
    {
        EventManager.Clear();
    }

    [Fact]
    public void Uses_damage_threshold_to_apply_brittle_to_top_draw_pile_card()
    {
        var attack = new Entomb();
        ApplyCardApplicationEvent publishedEvent = null;
        EventManager.Subscribe<ApplyCardApplicationEvent>(evt => publishedEvent = evt);

        attack.OnDamageThresholdMet(new EntityManager());

        Assert.Equal(10, attack.Damage);
        Assert.Equal(3, attack.MinimumDamageToTriggerEffect);
        Assert.Equal(ConditionType.None, attack.ConditionType);
        Assert.Null(attack.OnAttackHit);
        Assert.Equal(
            "If this attack deals 3 or more damage - Apply brittle to the top card of your draw pile.",
            attack.Text);
        Assert.NotNull(publishedEvent);
        Assert.Equal(1, publishedEvent.Amount);
        Assert.Equal(CardApplicationType.Brittle, publishedEvent.Type);
        Assert.Equal(CardApplicationTarget.TopXCards, publishedEvent.Target);
    }
}
