using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Cards;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    internal static class AppliedPassivesService
    {
      public static int GetPassiveDelta(ModifyHpEvent e)
      {
        var additional = 0;
        var passives = e.Source.GetComponent<AppliedPassives>().Passives;
        // var phaseState = entityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>();
        Console.WriteLine($"[AppliedPassivesService] GetPassiveDelta - passives: {passives.Count}");
        if (passives.ContainsKey(AppliedPassiveType.DowseWithHolyWater) && e.DamageType == ModifyTypeEnum.Attack)
        {
          Console.WriteLine($"[AppliedPassivesService] GetPassiveDelta - DowseWithHolyWater");
          passives.TryGetValue(AppliedPassiveType.DowseWithHolyWater, out var amount);
          CardDefinitionCache.TryGet("dowse_with_holy_water", out var def);
          additional += def.valuesParse[0] * amount;
          EventManager.Publish(new RemovePassive { Owner = e.Source, Type = AppliedPassiveType.DowseWithHolyWater });
        }
        Console.WriteLine($"[AppliedPassivesService] GetPassiveDelta - additional: {additional}");
        return -additional;
      }
    }
}