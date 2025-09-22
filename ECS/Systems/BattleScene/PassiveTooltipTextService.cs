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
          return $"At the start of {(isPlayer ? "your" : "the enemy's")} turn, {(isPlayer ? "you take" : "it takes")} {stacks} damage.";
        case AppliedPassiveType.DowseWithHolyWater:
          CardDefinitionCache.TryGet("dowse_with_holy_water", out var def);
          return $"Your next attack this turn deals +{def.valuesParse[0] * stacks} damage.";
        case AppliedPassiveType.Slow:
          return $"Ambush attacks are {stacks} second{(stacks == 1 ? "" : "s")} faster.";
        case AppliedPassiveType.Aegis:
          return $"Prevents the next {stacks} damage from any source.";
        case AppliedPassiveType.Stun:
          return $"Skips the next {stacks} attack{(stacks > 1 ? "s" : "")}.";
        default:
          return StringUtils.ToSentenceCase(type.ToString());
      }
    }
  }
}


