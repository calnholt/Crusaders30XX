using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Cards;

namespace Crusaders30XX.ECS.Systems
{
    internal static class ConditionalDamageCardService
    {
        public static int Resolve(EntityManager entityManager, Entity CardEntity)
        {
            var cardId = CardEntity.GetComponent<CardData>().CardId;
            CardDefinitionCache.TryGet(cardId, out CardDefinition def);
            var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var enemy = entityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            var courage = player.GetComponent<Courage>().Amount;
            var battleStateInfo = player.GetComponent<BattleStateInfo>();
            switch (cardId)
            {
                case "seize":
                {
                    battleStateInfo.PhaseTracking.TryGetValue(TrackingTypeEnum.CourageLost.ToString(), out var courageLost);
                    return courageLost > 0 ? def.valuesParse[0] : 0;
                }
                case "vindicate":
                {
                    return courage >= def.valuesParse[0] ? def.valuesParse[1] : 0;
                }
                default:
                    return 0;
            }
        }
    }
}