using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Objects.EnemyAttacks;

namespace Crusaders30XX.ECS.Objects.Enemies;

public class FallenShepherd : EnemyBase
{
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
        return CurrentPhase switch
        {
            2 => ["fallen_shepherd_phase_2"],
            3 => ["fallen_shepherd_phase_3"],
            _ => ["fallen_shepherd_phase_1"],
        };
    }
}

public class FallenShepherdPhase1 : EnemyAttackBase
{
    public FallenShepherdPhase1()
    {
        Id = "fallen_shepherd_phase_1";
        Name = "Phase 1";
        Damage = 9;
    }
}

public class FallenShepherdPhase2 : EnemyAttackBase
{
    public FallenShepherdPhase2()
    {
        Id = "fallen_shepherd_phase_2";
        Name = "Phase 2";
        Damage = 9;
    }
}

public class FallenShepherdPhase3 : EnemyAttackBase
{
    public FallenShepherdPhase3()
    {
        Id = "fallen_shepherd_phase_3";
        Name = "Phase 3";
        Damage = 9;
    }
}
