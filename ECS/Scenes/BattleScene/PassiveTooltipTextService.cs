using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Utils;

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
          return $"At the start of your turn, gain {stacks} slow. Lasts for the rest of the quest.";
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
        case AppliedPassiveType.Fear:
          return $"Attacks have a {stacks * 10}% chance to become ambush attacks this quest.";
        case AppliedPassiveType.Siphon:
          return $"For each point of courage this enemy removes from you, it heals {stacks * 3} HP.";
        case AppliedPassiveType.Thorns:
          return $"You gain {stacks} bleed whenever you attack this enemy.";
        case AppliedPassiveType.Bleed:
          return $"While you have bleed, lose 1 HP at the start of your turn then remove one bleed. Lasts for the rest of the quest.";
        default:
          return StringUtils.ToSentenceCase(type.ToString());
      }
    }
  }
}


