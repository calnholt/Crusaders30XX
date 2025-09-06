using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;

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
                        System.Console.WriteLine("[AdditionalCost] Cannot play 'stab': requires at least 2 Courage");
                        return false;
                    }
                    return true;
                }
                case "hammer":
                {
                    var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                    var courageCmp = player?.GetComponent<Courage>();
                    int courage = courageCmp?.Amount ?? 0;
                    if (courage < 3)
                    {
                        System.Console.WriteLine("[AdditionalCost] Cannot play 'hammer': requires at least 3 Courage to lose");
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


