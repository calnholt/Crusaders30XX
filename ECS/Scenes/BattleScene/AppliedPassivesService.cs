using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Systems
{
    internal static class AppliedPassivesService
    {

      public static int GetGalvanizeBonus(int preGalvanizeDamage)
      {
        if (preGalvanizeDamage <= 0) return 0;
        return (int)Math.Ceiling(preGalvanizeDamage * TooltipTextService.GalvanizeBonusFraction);
      }

      public static int GetGuardAbsorption(Entity target, int rawAttackDamage)
      {
        if (rawAttackDamage <= 0 || target == null) return 0;
        var passives = target.GetComponent<AppliedPassives>()?.Passives;
        if (passives == null) return 0;
        if (!passives.TryGetValue(AppliedPassiveType.Guard, out int guardStacks) || guardStacks <= 0) return 0;
        return Math.Min(guardStacks, rawAttackDamage);
      }

      public static int GetPreviewAttackDamage(ModifyHpRequestEvent e, int rawDamage, bool ReadOnly = true)
      {
        e.Delta = -rawDamage;
        if (e.DamageType == ModifyTypeEnum.Attack && rawDamage > 0)
        {
          int guardAbsorbed = GetGuardAbsorption(e.Target, rawDamage);
          if (guardAbsorbed > 0)
          {
            e.Delta += guardAbsorbed;
            if (e.Delta >= 0) return 0;
          }
        }

        int passiveDelta = GetPassiveDelta(e, ReadOnly);
        int newDelta = e.Delta + passiveDelta;
        if (e.DamageType == ModifyTypeEnum.Attack && newDelta > 0) return 0;
        return Math.Max(0, -newDelta);
      }

      public static int GetPassiveDelta(ModifyHpRequestEvent e, bool ReadOnly = false)
      {
        if (e.DamageType == ModifyTypeEnum.Heal)
        {
          return 0;
        }
        var delta = 0;
        var isEnemy = e.Source.HasComponent<Enemy>();
        var sourcePassives = e.Source.GetComponent<AppliedPassives>().Passives;
        var targetPassives = e.Target.GetComponent<AppliedPassives>().Passives;
        if (targetPassives.ContainsKey(AppliedPassiveType.Armor) && e.DamageType == ModifyTypeEnum.Attack)
        {
          targetPassives.TryGetValue(AppliedPassiveType.Armor, out var amount);
          delta -= amount;
        }
        if (targetPassives.ContainsKey(AppliedPassiveType.Wounded))
        {
          targetPassives.TryGetValue(AppliedPassiveType.Wounded, out var amount);
          delta += amount;
        }
        var flatSourceBonus = 0;
        if (sourcePassives.ContainsKey(AppliedPassiveType.Power) && e.DamageType == ModifyTypeEnum.Attack && !isEnemy)
        {
          sourcePassives.TryGetValue(AppliedPassiveType.Power, out var amount);
          delta += amount;
          flatSourceBonus += amount;
        }
        if (sourcePassives.ContainsKey(AppliedPassiveType.Might) && e.DamageType == ModifyTypeEnum.Attack && !isEnemy)
        {
          sourcePassives.TryGetValue(AppliedPassiveType.Might, out var amount);
          delta += amount;
          flatSourceBonus += amount;
        }
        if (e.DamageType == ModifyTypeEnum.Attack && !isEnemy)
        {
          var attackCard = e.AttackCard?.GetComponent<CardData>();
          bool isWeaponAttack = attackCard?.Card?.IsWeapon == true;
          if (!isWeaponAttack && sourcePassives.TryGetValue(AppliedPassiveType.Aggression, out var aggression) && aggression > 0)
          {
            delta += aggression;
            flatSourceBonus += aggression;
            if (!ReadOnly)
            {
              EventManager.Publish(new RemovePassive { Owner = e.Source, Type = AppliedPassiveType.Aggression });
            }
          }
          if (!isWeaponAttack && sourcePassives.TryGetValue(AppliedPassiveType.Galvanize, out var galvanize) && galvanize > 0)
          {
            int rawAttackDamage = Math.Abs(e.Delta);
            int preGalvanizeDamage = rawAttackDamage + flatSourceBonus;
            delta += GetGalvanizeBonus(preGalvanizeDamage);
            if (!ReadOnly)
            {
              EventManager.Publish(new RemovePassive { Owner = e.Source, Type = AppliedPassiveType.Galvanize });
            }
          }
          if (isWeaponAttack && sourcePassives.TryGetValue(AppliedPassiveType.Sharpen, out var sharpen) && sharpen > 0)
          {
            delta += sharpen;
            if (!ReadOnly)
            {
              EventManager.Publish(new RemovePassive { Owner = e.Source, Type = AppliedPassiveType.Sharpen });
            }
          }
        }
        return -delta;
      }
    }
}
