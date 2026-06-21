using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class FallenShepherd : EnemyBase
{
    private static readonly List<string> Phase1SmallAttacks =
    [
        "fallen_shepherd_crooks_scar",
        "fallen_shepherd_break_faith",
        "fallen_shepherd_bloodletting",
        "fallen_shepherd_cow_the_flock",
    ];

    private static readonly List<string> Phase2SmallAttacks =
    [
        "fallen_shepherd_shepherds_vigil",
        "fallen_shepherd_hush",
        "fallen_shepherd_crooks_scar",
        "fallen_shepherd_cow_the_flock",
    ];

    private static readonly List<string> Phase3Attacks =
    [
        "fallen_shepherd_purge_the_heretic",
        "fallen_shepherd_fear_the_shepherd",
        "fallen_shepherd_final_sermon",
        "fallen_shepherd_phase_3",
    ];

    public FallenShepherd(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
    {
        Id = "fallen_shepherd";
        Name = "Fallen Shepherd";
        HealthPerCard = 1.43f;
        IsBoss = true;
        Phases = 3;
    }

    public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
    {
        bool isHeavyTurn = turnNumber <= 1 || turnNumber % 2 == 1;

        return CurrentPhase switch
        {
            2 when isHeavyTurn => ["fallen_shepherd_phase_2"],
            2 => ArrayUtils.TakeRandomWithoutReplacement(Phase2SmallAttacks, 3),
            3 => ArrayUtils.TakeRandomWithoutReplacement(Phase3Attacks, 1),
            _ when isHeavyTurn => ["fallen_shepherd_phase_1"],
            _ => ArrayUtils.TakeRandomWithoutReplacement(Phase1SmallAttacks, 3),
        };
    }
}

public class FallenShepherdPhase1 : EnemyAttackBase
{
    private const int BlockerRequirement = 1;

    public FallenShepherdPhase1()
    {
        Id = "fallen_shepherd_phase_1";
        Name = "Cast Out";
        Damage = 9;
        ConditionType = ConditionType.MustBeBlockedByAtLeast1Card;
        Text = $"{EnemyAttackTextHelper.GetText(EnemyAttackTextType.MustBeBlockedByAtLeast, BlockerRequirement)}\n\nEach card used to block this attack becomes colorless.";

        OnAttackReveal = _ =>
        {
            EventManager.Publish(new MustBeBlockedEvent
            {
                Threshold = BlockerRequirement,
                Type = MustBeBlockedSystem.MustBeBlockedByType.AtLeast,
            });
        };

        OnBlockProcessed = (entityManager, card) =>
        {
            FallenShepherdCardRestrictionHelper.Apply<Colorless>(entityManager, card);
        };
    }
}

public class FallenShepherdCrooksScar : EnemyAttackBase
{
    private const int ScarAmount = 1;

    public FallenShepherdCrooksScar()
    {
        Id = "fallen_shepherd_crooks_scar";
        Name = "Crook's Scar";
        Damage = 3;
        ConditionType = ConditionType.OnHit;
        Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Scar, ScarAmount, ConditionType);

        OnAttackHit = entityManager =>
        {
            EventManager.Publish(new ApplyPassiveEvent
            {
                Target = entityManager.GetEntity("Player"),
                Type = AppliedPassiveType.Scar,
                Delta = ScarAmount,
            });
        };
    }
}

public class FallenShepherdBreakFaith : EnemyAttackBase
{
    public FallenShepherdBreakFaith()
    {
        Id = "fallen_shepherd_break_faith";
        Name = "Break Faith";
        Damage = 3;
        Text = "Each card used to block this attack becomes brittle.";

        OnBlockProcessed = (entityManager, card) =>
        {
            FallenShepherdCardRestrictionHelper.Apply<Brittle>(entityManager, card);
        };
    }
}

public class FallenShepherdBloodletting : EnemyAttackBase
{
    private const int BleedAmount = 3;

    public FallenShepherdBloodletting()
    {
        Id = "fallen_shepherd_bloodletting";
        Name = "Bloodletting";
        Damage = 3;
        ConditionType = ConditionType.OnHit;
        Text = $"On hit - Gain {BleedAmount} bleed.";

        OnAttackHit = entityManager =>
        {
            EventManager.Publish(new ApplyPassiveEvent
            {
                Target = entityManager.GetEntity("Player"),
                Type = AppliedPassiveType.Bleed,
                Delta = BleedAmount,
            });
        };
    }
}

public class FallenShepherdCowTheFlock : EnemyAttackBase
{
    private const int IntimidateAmount = 1;

    public FallenShepherdCowTheFlock()
    {
        Id = "fallen_shepherd_cow_the_flock";
        Name = "Cow the Flock";
        Damage = 3;
        Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Intimidate, IntimidateAmount);

        OnAttackReveal = _ =>
        {
            EventManager.Publish(new IntimidateEvent { Amount = IntimidateAmount });
        };
    }
}

public class FallenShepherdPhase2 : EnemyAttackBase
{
    private const int ShackledAmount = 2;

    public FallenShepherdPhase2()
    {
        Id = "fallen_shepherd_phase_2";
        Name = "Binding Sermon";
        Damage = 10;
        BlockRequiredToPreventEffect = Random.Shared.Next(0, 100) <= 50 ? 6 : 7;
        Text = EnemyAttackTextHelper.GetBlockThresholdText(Damage - BlockRequiredToPreventEffect.Value, $"Gain {ShackledAmount} shackled.");

        OnDamageThresholdMet = entityManager =>
        {
            EventManager.Publish(new ApplyPassiveEvent
            {
                Target = entityManager.GetEntity("Player"),
                Type = AppliedPassiveType.Shackled,
                Delta = ShackledAmount,
            });
        };
    }
}

public class FallenShepherdShepherdsVigil : EnemyAttackBase
{
    private const int GuardAmount = 3;

    public FallenShepherdShepherdsVigil()
    {
        Id = "fallen_shepherd_shepherds_vigil";
        Name = "Shepherd's Vigil";
        Damage = 3;
        ConditionType = ConditionType.OnHit;
        Text = EnemyAttackTextHelper.GetText(EnemyAttackTextType.Guard, GuardAmount, ConditionType);

        OnAttackHit = entityManager =>
        {
            EventManager.Publish(new ApplyPassiveEvent
            {
                Target = entityManager.GetEntity("Enemy"),
                Type = AppliedPassiveType.Guard,
                Delta = GuardAmount,
            });
        };
    }
}

public class FallenShepherdHush : EnemyAttackBase
{
    private const int SilencedAmount = 1;

    public FallenShepherdHush()
    {
        Id = "fallen_shepherd_hush";
        Name = "Hush";
        Damage = 3;
        ConditionType = ConditionType.OnHit;
        Text = $"On hit - Gain {SilencedAmount} silenced.";

        OnAttackHit = entityManager =>
        {
            EventManager.Publish(new ApplyPassiveEvent
            {
                Target = entityManager.GetEntity("Player"),
                Type = AppliedPassiveType.Silenced,
                Delta = SilencedAmount,
            });
        };
    }
}

public class FallenShepherdPurgeTheHeretic : EnemyAttackBase
{
    private const int BurnAmount = 1;

    public FallenShepherdPurgeTheHeretic()
    {
        Id = "fallen_shepherd_purge_the_heretic";
        Name = "Purge the Heretic";
        Damage = 8;
        Text = $"On reveal - Gain {BurnAmount} burn.";

        OnAttackReveal = entityManager =>
        {
            EventManager.Publish(new ApplyPassiveEvent
            {
                Target = entityManager.GetEntity("Player"),
                Type = AppliedPassiveType.Burn,
                Delta = BurnAmount,
            });
        };
    }
}

public class FallenShepherdFearTheShepherd : EnemyAttackBase
{
    private const int FearAmount = 1;

    public FallenShepherdFearTheShepherd()
    {
        Id = "fallen_shepherd_fear_the_shepherd";
        Name = "Fear the Shepherd";
        Damage = 9;
        Text = $"On reveal - Gain {FearAmount} fear.";

        OnAttackReveal = entityManager =>
        {
            EventManager.Publish(new ApplyPassiveEvent
            {
                Target = entityManager.GetEntity("Player"),
                Type = AppliedPassiveType.Fear,
                Delta = FearAmount,
            });
        };
    }
}

public class FallenShepherdFinalSermon : EnemyAttackBase
{
    private const int SilencedAmount = 1;

    public FallenShepherdFinalSermon()
    {
        Id = "fallen_shepherd_final_sermon";
        Name = "Final Sermon";
        Damage = 9;
        Text = $"On reveal - Gain {SilencedAmount} silenced.";

        OnAttackReveal = entityManager =>
        {
            EventManager.Publish(new ApplyPassiveEvent
            {
                Target = entityManager.GetEntity("Player"),
                Type = AppliedPassiveType.Silenced,
                Delta = SilencedAmount,
            });
        };
    }
}

public class FallenShepherdPhase3 : EnemyAttackBase
{
    private string _contextId = string.Empty;

    public FallenShepherdPhase3()
    {
        Id = "fallen_shepherd_phase_3";
        Name = "Have No Mercy";
        Damage = 9;
        BlockRequiredToPreventEffect = Random.Shared.Next(0, 100) <= 50 ? 3 : 4;
        Text = EnemyAttackTextHelper.GetBlockThresholdText(Damage - BlockRequiredToPreventEffect.Value, "Discard the selected card from your hand.");

        OnAttackReveal = entityManager =>
        {
            _contextId = GetComponentHelper.GetContextId(entityManager) ?? string.Empty;
            EventManager.Publish(new MarkedForSpecificDiscardEvent
            {
                Amount = 1,
                ContextId = _contextId,
            });

            var markedCard = entityManager.GetEntitiesWithComponent<MarkedForSpecificDiscard>()
                .FirstOrDefault(card => card.GetComponent<MarkedForSpecificDiscard>()?.ContextId == _contextId);
            string cardName = markedCard?.GetComponent<CardData>()?.Card?.Name;
            if (!string.IsNullOrWhiteSpace(cardName))
            {
                Text = EnemyAttackTextHelper.GetBlockThresholdText(Damage - BlockRequiredToPreventEffect.Value, $"Discard {cardName} from your hand.");
            }
        };

        OnDamageThresholdMet = _ =>
        {
            EventManager.Publish(new DiscardMarkedForSpecificDiscardEvent { ContextId = _contextId });
        };
    }
}

internal static class FallenShepherdCardRestrictionHelper
{
    public static void Apply<T>(EntityManager entityManager, Entity card)
        where T : class, IComponent, new()
    {
        if (card?.GetComponent<CardData>() == null || card.GetComponent<AssignedBlockCard>()?.IsEquipment == true)
        {
            return;
        }

        if (!card.HasComponent<T>())
        {
            entityManager.AddComponent(card, new T());
        }

        RunScopedStateService.SyncCardRestrictionsFromComponents(card);
    }
}
