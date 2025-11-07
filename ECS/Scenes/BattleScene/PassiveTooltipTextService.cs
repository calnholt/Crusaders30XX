using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Utils;
using Crusaders30XX.ECS.Data.Cards;

namespace Crusaders30XX.ECS.Systems
{
  internal static class PassiveTooltipTextService
  {
    public static string GetText(AppliedPassiveType type, bool isPlayer, int stacks)
    {
      switch (type)
      {
        case AppliedPassiveType.Burn:
          return $"At the start of {(isPlayer ? "your" : "the enemy's")} turn, {(isPlayer ? "you take" : "it takes")} {stacks} damage this battle.";
        case AppliedPassiveType.DowseWithHolyWater:
          CardDefinitionCache.TryGet("dowse_with_holy_water", out var def);
          return $"Your next attack this turn deals +{def.valuesParse[0] * stacks} damage.";
        case AppliedPassiveType.Slow:
          return $"Ambush attacks are {stacks} second{(stacks == 1 ? "" : "s")} faster this battle.";
        case AppliedPassiveType.Aegis:
          return $"Prevents the next {stacks} damage from any source.";
        case AppliedPassiveType.Stun:
          return $"Skips the next {stacks} attack{(stacks > 1 ? "s" : "")}.";
        case AppliedPassiveType.Armor:
          return $"Takes {stacks} less damage from attacks this battle.";
        case AppliedPassiveType.Wounded:
          return $"Takes {stacks} more damage from all sources this battle.";
        case AppliedPassiveType.Webbing:
          return $"At the start of your turn, gain {stacks} slow this battle.";
        case AppliedPassiveType.Inferno:
          return $"At the start of your turn, gain {stacks} burn {(stacks == 1 ? "" : "s")} this battle.";
        case AppliedPassiveType.Penance:
          return $"Lose {stacks} max HP for the rest of the quest.";
        case AppliedPassiveType.Aggression:
          return $"The next attack this turn gains {stacks} damage.";
        case AppliedPassiveType.Stealth:
          return "You cannot see the number of attacks this monster plans.";
        case AppliedPassiveType.Power:
          return $"Your attacks deal +{stacks} damage this battle.";
        case AppliedPassiveType.Poison:
          return $"Every 60 seconds, lose 1 HP.";
        case AppliedPassiveType.Shield:
          return $"Prevent all damage from the first source each turn.";
        default:
          return StringUtils.ToSentenceCase(type.ToString());
      }
    }
  }
}


