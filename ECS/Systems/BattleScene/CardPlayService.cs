using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    internal static class CardPlayService
    {
        public static void Resolve(EntityManager entityManager, string cardId, string cardName)
        {
            var enemy = entityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            int courage = player?.GetComponent<Courage>()?.Amount ?? 0;

            System.Console.WriteLine($"[CardPlayService] Resolving card id={cardId} name={cardName}");

            switch (cardId)
            {
                case "strike":
                {
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -10 });
                    System.Console.WriteLine("[CardPlayService] Applied strike -10");
                    break;
                }
                case "strike_2":
                {
                    int dmg = courage >= 2 ? 12 : 8;
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_3":
                {
                    int dmg = courage >= 3 ? 13 : 7;
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_4":
                {
                    int dmg = courage >= 4 ? 14 : 6;
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_5":
                {
                    int dmg = courage >= 5 ? 15 : 5;
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_6":
                {
                    int dmg = courage >= 6 ? 16 : 4;
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_7":
                {
                    int dmg = courage >= 7 ? 17 : 3;
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_8":
                {
                    int dmg = courage >= 8 ? 18 : 2;
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_9":
                {
                    int dmg = courage >= 9 ? 19 : 1;
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_10":
                {
                    int dmg = courage >= 10 ? 20 : 0;
                    if (dmg > 0) EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "stab":
                {
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -8 });
                    EventManager.Publish(new ModifyCourageEvent { Delta = -2 });
                    break;
                }
                case "stun":
                {
                    EventManager.Publish(new ApplyStun { Delta = +1 });
                    break;
                }
                case "courageous":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = +3 });
                    break;
                }
                case "inspiration":
                {
                    EventManager.Publish(new RequestDrawCardsEvent { Count = 2 });
                    EventManager.Publish(new ModifyTemperanceEvent { Delta = 1 });
                    break;
                }
                case "anoint_the_sick":
                {
                    EventManager.Publish(new ModifyHpEvent { Target = player, Delta = 5 });
                    break;
                }
                // weapons
                case "sword":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -3 });
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -30 });
                    break;
                }
                default:
                    System.Console.WriteLine($"[CardPlayService] No effect for id={cardId}");
                    break;
            }
        }
    }
}


