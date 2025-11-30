using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    internal static class AppliedPassivesService
    {
      public static int GetPassiveDelta(ModifyHpRequestEvent e, bool ReadOnly = false)
      {
        if (e.DamageType == ModifyTypeEnum.Heal)
        {
          return 0;
        }
        var delta = 0;
        var sourcePassives = e.Source.GetComponent<AppliedPassives>().Passives;
        var targetPassives = e.Target.GetComponent<AppliedPassives>().Passives;
        // var phaseState = entityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>();
        Console.WriteLine($"[AppliedPassivesService] GetPassiveDelta - passives: {sourcePassives.Count}");
        if (sourcePassives.ContainsKey(AppliedPassiveType.Aggression) && e.DamageType == ModifyTypeEnum.Attack)
        {
          Console.WriteLine($"[AppliedPassivesService] applying Aggression");
          sourcePassives.TryGetValue(AppliedPassiveType.Aggression, out var amount);
          delta += amount;
          if (!ReadOnly)
          {
            EventManager.Publish(new RemovePassive { Owner = e.Source, Type = AppliedPassiveType.Aggression });
          }
        }
        if (targetPassives.ContainsKey(AppliedPassiveType.Armor) && e.DamageType == ModifyTypeEnum.Attack)
        {
          Console.WriteLine($"[AppliedPassivesService] applying Armor");
          targetPassives.TryGetValue(AppliedPassiveType.Armor, out var amount);
          delta -= amount;
        }
        if (targetPassives.ContainsKey(AppliedPassiveType.Wounded))
        {
          Console.WriteLine($"[AppliedPassivesService] applying Wounded");
          targetPassives.TryGetValue(AppliedPassiveType.Wounded, out var amount);
          delta += amount;
        }
        if (sourcePassives.ContainsKey(AppliedPassiveType.Power))
        {
          Console.WriteLine($"[AppliedPassivesService] applying Power");
          sourcePassives.TryGetValue(AppliedPassiveType.Power, out var amount);
          delta += amount;
        }
        Console.WriteLine($"[AppliedPassivesService] GetPassiveDelta - delta: {delta}");
        return -delta;
      }
    }
}