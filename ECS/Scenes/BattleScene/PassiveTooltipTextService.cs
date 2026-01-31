using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Utils;

namespace Crusaders30XX.ECS.Systems
{
  internal static class PassiveTooltipTextService
  {
    public static readonly int FrostbiteThreshold = 3;
    public static readonly int FrostbiteDamage = 3;

    public static string GetText(AppliedPassiveType type, bool isPlayer, int stacks)
    {
      var text = GetTooltip(type, isPlayer, stacks);
      var suffix = " (Quest)";
      if (AppliedPassivesManagementSystem.GetTurnPassives().Contains(type))
      {
        suffix = " (Turn)";
      }
      else if (AppliedPassivesManagementSystem.GetBattlePassives().Contains(type))
      {
        suffix = " (Battle)";
      }
      return $"{text}{suffix}";
    }
    public static string GetTooltip(AppliedPassiveType type, bool isPlayer, int stacks)
    {
      switch (type)
      {
        case AppliedPassiveType.Burn:
          return $"At the start of {(isPlayer ? "your" : "the enemy's")} turn, {(isPlayer ? "you take" : "it takes")} {stacks} damage.";
        case AppliedPassiveType.Slow:
          return $"Ambush attacks are {stacks} second{(stacks == 1 ? "" : "s")} faster.";
        case AppliedPassiveType.Aegis:
          return $"Prevents the next {stacks} damage from any source.";
        case AppliedPassiveType.Stun:
          return $"Skips the next {stacks} attack{(stacks > 1 ? "s" : "")}.";
        case AppliedPassiveType.Armor:
          return $"Takes {stacks} less damage from attacks.";
        case AppliedPassiveType.Wounded:
          return $"Takes {stacks} more damage from all sources.";
        case AppliedPassiveType.Webbing:
          return $"At the start of your turn, gain {stacks} slow.";
        case AppliedPassiveType.Inferno:
          return $"At the start of your turn, gain {stacks} burn {(stacks == 1 ? "" : "s")}.";
        case AppliedPassiveType.Scar:
          return $"Lose {stacks} max HP.";
        case AppliedPassiveType.Penance:
          return $"Your attacks deal {stacks} less damage. At the start of the next battle, these are converted to scars.";
        case AppliedPassiveType.Aggression:
          return $"The next attack this turn gains {stacks} damage.";
        case AppliedPassiveType.Stealth:
          return "You cannot see the number of attacks this monster plans.";
        case AppliedPassiveType.Power:
          return $"{(isPlayer ? "Your" : "The enemy's")} attacks deal +{stacks} damage.";
        case AppliedPassiveType.Poison:
          return $"Every 60 seconds, lose 1 HP.";
        case AppliedPassiveType.Shield:
          return $"Prevent all damage from the first source each turn.";
        case AppliedPassiveType.Fear:
          return $"Attacks have a {stacks * 10}% chance to become ambush attacks.";
        case AppliedPassiveType.Siphon:
          return $"For each point of courage this enemy removes from you, it heals {stacks * Succubus.SiphonMultiplier} HP.";
        case AppliedPassiveType.Thorns:
          return $"You gain {stacks} bleed whenever you attack this enemy.";
        case AppliedPassiveType.Bleed:
          return $"While you have bleed, lose 1 HP at the start of your turn then remove one bleed.";
        case AppliedPassiveType.Rage:
          return $"{(isPlayer ? "You" : "The enemy")} gain{(isPlayer ? "" : "s")} {stacks} power at the start of the {(isPlayer ? "action phase" : "block phase")}.";
        case AppliedPassiveType.Intellect:
          return $"Your max hand size and the number of cards you draw at the start of the block phase is increased by {stacks}.";
        case AppliedPassiveType.Intimidated:
          return $"At the start of the block phase, {stacks} {(stacks == 1 ? "card" : "cards")} from your hand {(stacks == 1 ? "is" : "are")} intimidated.";
        case AppliedPassiveType.MindFog:
          return $"At the end of your action phase, discard all cards in your hand.";
        case AppliedPassiveType.Channel:
          return $"Increases the potency of attacks.";
        case AppliedPassiveType.Frostbite:
          return $"When you have {FrostbiteThreshold} stacks of frostbite, take {FrostbiteDamage} damage and lose {FrostbiteThreshold} frostbite.";
        case AppliedPassiveType.Frozen:
          return $"When you play a frozen card, gain 1 frostbite and there's a 50% chance it's exhausted. Remove frozen by blocking with it.";
        case AppliedPassiveType.SubZero:
          return $"At the start of the enemy turn, freeze one card from your hand.";
        case AppliedPassiveType.Windchill:
          return $"Whenever you block with a frozen card, gain 1 penance.";
        case AppliedPassiveType.Enflamed:
          return $"If you have 4+ courage at the end of the action phase, take {stacks} damage.";
        case AppliedPassiveType.Shackled:
          return $"At the start of the block phase, shackle 2 cards from your hand. Remove 1 shackled stacks by blocking with them.";
        case AppliedPassiveType.Anathema:
          return $"When you pledge a card, the enemy loses {stacks} damage.";
        case AppliedPassiveType.Silenced:
          return $"You cannot play pledged cards. Remove 1 silenced at the end of your action phase.";
        case AppliedPassiveType.Sealed:
          return $"Sealed cards cannot be played or pledged, but can block. Cracks accumulate: +1 per block, +1 per card played. At 3 cracks, seal is broken.";
        default:
          return StringUtils.ToSentenceCase(type.ToString());
      }
    }
  }
}


