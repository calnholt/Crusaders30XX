using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Cards;
using System;

namespace Crusaders30XX.ECS.Systems
{
    internal static class CardPlayService
    {
        public static void Resolve(EntityManager entityManager, string cardId, string cardName)
        {
            var enemy = entityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var battleStateInfo = player.GetComponent<BattleStateInfo>();
            int courage = player?.GetComponent<Courage>()?.Amount ?? 0;

            System.Console.WriteLine($"[CardPlayService] Resolving card id={cardId} name={cardName}");
            CardDefinitionCache.TryGet(cardId, out CardDefinition def);
            var values = def.valuesParse;

            switch (cardId)
            {
                case "anoint_the_sick":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = player, Delta = +values[0], DamageType = ModifyTypeEnum.Heal });
                    break;
                }
                case "burn":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Burn, Delta = +values[0] });
                    if (courage >= values[1]) 
                    {
                        EventManager.Publish(new ModifyActionPointsEvent { Delta = values[2] });
                        EventManager.Publish(new ModifyCourageEvent { Delta = -values[3] });
                    }
                    break;
                }
                case "courageous":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = +values[0] });
                    // brief delay so cards in hand are updated
                    // TODO: cards can still be played because animation prolongs
                    TransitionStateSingleton.IsActive = true;
                    TimerScheduler.Schedule(.01f, () => {
                        TransitionStateSingleton.IsActive = false;
                        EventManager.Publish(new DebugCommandEvent { Command = "EndTurn" });
                    });
                    break;
                }
                case "divine_protection":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aegis, Delta = +values[0] });
                    break;
                }
                case "dowse_with_holy_water":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.DowseWithHolyWater, Delta = 1 });
                    break;
                }
                case "inspiration":
                {
                    EventManager.Publish(new RequestDrawCardsEvent { Count = values[0] });
                    EventManager.Publish(new ModifyTemperanceEvent { Delta = values[1] });
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = player, Delta = -values[2], DamageType = ModifyTypeEnum.Effect });
                    break;
                }
                case "seize":
                {
                    battleStateInfo.PhaseTracking.TryGetValue(TrackingTypeEnum.CourageLost, out var courageLost);
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = enemy, Delta = -(courageLost > 0 ? values[1] : values[0]), DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                case "stab":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[0] });
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = enemy, Delta = -values[1], DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                case "strike":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = enemy, Delta = -values[0], DamageType = ModifyTypeEnum.Attack });
                    if (Random.Shared.Next(0, 100) <= values[1])
                    {
                        EventManager.Publish(new ModifyActionPointsEvent { Delta = values[2] });
                    }
                    break;
                }
                case "stun":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Stun, Delta = 1 });
                    break;
                }
                case "vindicate":
                {
                    int damage = values[0] + (courage * values[1]);
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = enemy, Delta = -damage, DamageType = ModifyTypeEnum.Attack });
                    EventManager.Publish(new SetCourageEvent { Amount = 0 });
                    break;
                }
                // weapons
                case "sword":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[0] });
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = enemy, Delta = -values[1], DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                default:
                    System.Console.WriteLine($"[CardPlayService] No effect for id={cardId}");
                    break;
            }
        }
    }
}


