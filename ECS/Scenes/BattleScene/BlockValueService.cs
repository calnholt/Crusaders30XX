using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Cards;
using System;

namespace Crusaders30XX.ECS.Systems
{
    internal static class BlockValueService
    {
        public static void Apply(Entity card, int delta)
        {
          var modifiedBlock = card.GetComponent<ModifiedBlock>();
          if (modifiedBlock == null)
          {
              modifiedBlock = new ModifiedBlock { Owner = card, Delta = delta };
              card.AddComponent(modifiedBlock);
          }
          else
          {
              modifiedBlock.Delta += delta;
          }
        }

        public static int GetBlockValue(Entity card)
        {
          var modifiedBlock = card.GetComponent<ModifiedBlock>();
          var cd = card.GetComponent<CardData>();
          CardDefinitionCache.TryGet(cd.CardId, out var def);
          var block = def.block + (cd.Color == CardData.CardColor.Black ? 1 : 0);
          if (modifiedBlock == null)
          {
              return block;
          }
          return modifiedBlock.Delta + block;
        }
    }
}