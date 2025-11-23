using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Cards;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    internal static class BlockCardResolveService
    {
      public static void Resolve(Entity card)
      {
        var cardId = card.GetComponent<CardData>().CardId;
        CardDefinitionCache.TryGet(cardId, out var def);
        switch (cardId)
        {
          case "stalwart":
            EventManager.Publish(new ModifyCourageEvent { Delta = -def.valuesParse[0] });
            break;
        }
      }
    }
}