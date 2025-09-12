using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using System;

namespace Crusaders30XX.ECS.Systems
{
    internal static class EvaluateAdditionalCostService
    {
        public static bool CanPay(EntityManager entityManager, string cardId)
        {
            switch (cardId)
            {
                case "stab":
                {
                    var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                    int courage = player?.GetComponent<Courage>()?.Amount ?? 0;
                    if (courage < 2)
                    {
                        EventManager.Publish(new CantPlayCardMessage { Message = "Requires 2 courage!" });
                        return false;
                    }
                    return true;
                }
                case "sword":
                {
                    var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                    var courageCmp = player?.GetComponent<Courage>();
                    int courage = courageCmp?.Amount ?? 0;
                    if (courage < 3)
                    {
                        EventManager.Publish(new CantPlayCardMessage { Message = "Requires 3 courage!" });
                        return false;
                    }
                    return true;
                }
                default:
                    return true;
            }
        }
    }
}


