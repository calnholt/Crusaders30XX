using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Cards;

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
            CardDefinitionCache.TryGet(cardId, out CardDefinition def);
            var values = def.valuesParse;

            switch (cardId)
            {
                case "anoint_the_sick":
                {
                    EventManager.Publish(new ModifyHpEvent { Target = player, Delta = values[0] });
                    break;
                }
                case "burn":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Owner = enemy, Type = AppliedPassiveType.Burn, Delta = +values[0] });
                    break;
                }
                case "courageous":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = +values[0] });
                    EventManager.Publish(new DebugCommandEvent { Command = "EndTurn" });
                    break;
                }
                case "dowse_with_holy_water":
                {
                    // TODO: implement buffing attacks - may need to create AttackRequestEvent that initiates the actual ModifyHpEvent
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = +values[0] });
                    break;
                }
                case "inspiration":
                {
                    EventManager.Publish(new RequestDrawCardsEvent { Count = values[0] });
                    EventManager.Publish(new ModifyTemperanceEvent { Delta = values[1] });
                    break;
                }
                case "seize":
                {
                    // TODO: implement turn tracking dictionary component
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[0] });
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -values[1] });
                    break;
                }
                case "stab":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[0] });
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -values[1] });
                    break;
                }
                case "strike":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[0] });
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -values[1] });
                    break;
                }
                case "stun":
                {
                    EventManager.Publish(new ApplyStun { Delta = +1 });
                    break;
                }
                case "vindicate":
                {
                    int damage = values[0] + courage * 2;
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -damage });
                    EventManager.Publish(new SetCourageEvent { Amount = 0 });
                    break;
                }
                // weapons
                case "sword":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[0] });
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -values[1] });
                    break;
                }
                default:
                    System.Console.WriteLine($"[CardPlayService] No effect for id={cardId}");
                    break;
            }
        }
    }
}


