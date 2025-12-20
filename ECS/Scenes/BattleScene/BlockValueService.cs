using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using System;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Systems
{
    internal static class BlockValueService
    {
        public static void ApplyDelta(Entity card, int delta, string reason)
        {
          var modifiedBlock = card.GetComponent<ModifiedBlock>();
          modifiedBlock.Modifications.Add(new Modification { Delta = delta, Reason = reason });
        }

        public static void RemoveModification(Entity card, string reason)
        {
          var modifiedBlock = card.GetComponent<ModifiedBlock>();
          modifiedBlock.Modifications.RemoveAll(m => m.Reason == reason);
        }

        public static int GetTotalBlockValue(Entity card)
        {
          var modifiedBlock = card.GetComponent<ModifiedBlock>();
          if (modifiedBlock == null)
          {
            return GetBaseBlockValue(card);
          }
          var baseBlock = GetBaseBlockValue(card);
          return modifiedBlock.Modifications.Sum(m => m.Delta) + baseBlock;
        }

        public static int GetBaseBlockValue(Entity card)
        {
            var cd = card.GetComponent<CardData>();
            return cd.Card.Block;
        }
    }
}