using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Cards;
using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Factories;
using System.Reflection;

namespace Crusaders30XX.ECS.Systems
{
    internal static class CardPlayService
    {
        public static void Resolve(EntityManager entityManager, string cardId, string cardName, Entity card, List<Entity> paymentCards)
        {
            var enemy = entityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var battleStateInfo = player.GetComponent<BattleStateInfo>();
            int courage = player?.GetComponent<Courage>()?.Amount ?? 0;

            Console.WriteLine($"[CardPlayService] Resolving card id={cardId} name={cardName}");
            CardDefinitionCache.TryGet(cardId, out CardDefinition def);
            var target = def.target == "Player" ? player : enemy;
            var values = def.valuesParse;
            var i = 0;
            switch (cardId)
            {
                case "anoint_the_sick":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = +values[i++], DamageType = ModifyTypeEnum.Heal });
                    break;
                }
                case "bulwark":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Attack });
                    BlockValueService.ApplyDelta(card, values[i++]);
                    break;
                }
                case "burn":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = target, Type = AppliedPassiveType.Burn, Delta = +values[i++] });
                    if (courage >= values[1]) 
                    {
                        EventManager.Publish(new ModifyActionPointsEvent { Delta = values[i++] });
                        EventManager.Publish(new ModifyCourageEvent { Delta = -values[i++] });
                    }
                    break;
                }
                case "courageous":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = +values[i++] });
                    // brief delay so cards in hand are updated
                    // TODO: cards can still be played because animation prolongs
                    TransitionStateSingleton.IsActive = true;
                    EventManager.Publish(new DebugCommandEvent { Command = "EndTurn" });
                    TimerScheduler.Schedule(1f, () => {
                        TransitionStateSingleton.IsActive = false;
                    });
                    break;
                }
                case "divine_protection":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = target, Type = AppliedPassiveType.Aegis, Delta = +values[i++] });
                    break;
                }
                case "dowse_with_holy_water":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = target, Type = AppliedPassiveType.DowseWithHolyWater, Delta = 1 });
                    break;
                }
                case "heavens_glory":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = target, Type = AppliedPassiveType.Inferno, Delta = values[i++] });
                    break;
                }
                case "impale":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[i++] });
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                case "kunai":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Attack });
                    var chance = values[i++];
                    var random = Random.Shared.Next(0, 100);
                    Console.WriteLine($"[CardPlayService] Kunai random: {random}");
                    if (random <= chance)
                    {
                        EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Wounded, Delta = +1 });
                    }
                    break;
                }
                case "narrow_gate":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Attack });
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aegis, Delta = +values[i++] });
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Penance, Delta = +values[i++] });
                    break;
                }
                case "pouch_of_kunai":
                {
                    var count = Random.Shared.Next(values[0], values[1] + 1);
                    for (int j = 0; j < count; j++)
                    {
                        var kunai = EntityFactory.CreateCardFromDefinition(entityManager, "kunai", CardData.CardColor.White, false);
                        var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                        EventManager.Publish(new CardMoveRequested { Card = kunai, Deck = deckEntity, Destination = CardZoneType.Hand, Reason = "PouchOfKunai" });
                    }
                    break;
                }
                case "sacrifice":
                {
                    EventManager.Publish(new RequestDrawCardsEvent { Count = values[i++] });
                    EventManager.Publish(new ModifyTemperanceEvent { Delta = values[i++] });
                    EventManager.Publish(new ApplyPassiveEvent { Target = target, Type = AppliedPassiveType.Penance, Delta = values[i++] });
                    break;
                }
                case "seize":
                {
                    battleStateInfo.PhaseTracking.TryGetValue(TrackingTypeEnum.CourageLost.ToString(), out var courageLost);
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -(courageLost > 0 ? values[1] : values[0]), DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                case "shield_of_faith":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = target, Type = AppliedPassiveType.Aegis, Delta = +values[i++] });
                    break;
                }
                case "shroud_of_turin":
                {
                    var paymentCard = paymentCards.FirstOrDefault();
                    var cardDataCopy = paymentCard.GetComponent<CardData>();
                    CardDefinitionCache.TryGet(cardDataCopy.CardId, out CardDefinition cardToCopyDef);
                    var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                    var copy = EntityFactory.CreateCardFromDefinition(entityManager, cardToCopyDef.id, cardDataCopy.Color, false);
                    // TODO: need to have better cloning mechanism
                    if (paymentCard.HasComponent<ModifiedBlock>())
                    {
                        var modifiedBlock = paymentCard.GetComponent<ModifiedBlock>(); 
                        Console.WriteLine($"[CardPlayService] Shroud of Turin copying modified block: {modifiedBlock.Delta}");
                        BlockValueService.ApplyDelta(copy, modifiedBlock.Delta);
                    }
                    EventManager.Publish(new CardMoveRequested { Card = copy, Deck = deckEntity, Destination = CardZoneType.Hand, Reason = "ShroudCopy" });
                    break;
                }
                case "stab":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[i++] });
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                case "strike":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Attack });
                    var chance = values[i++];
                    var random = Random.Shared.Next(0, 100);
                    Console.WriteLine($"[CardPlayService] Strike random: {random}");
                    if (random <= chance)
                    {
                        Console.WriteLine($"[CardPlayService] Strike gained {values[i]} courage");
                        EventManager.Publish(new ModifyCourageEvent { Delta = values[i++] });
                    }
                    break;
                }
                case "stun":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = target, Type = AppliedPassiveType.Stun, Delta = 1 });
                    break;
                }
                case "tempest":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Attack });
                    EventManager.Publish(new ModifyTemperanceEvent { Delta = values[i++] });
                    break;
                }
                case "vindicate":
                {
                    var damage = courage >= values[1] ? values[2] : values[0];
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -damage, DamageType = ModifyTypeEnum.Attack });
                    EventManager.Publish(new SetCourageEvent { Amount = 0 });
                    break;
                }
                // weapons
                case "hammer":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Attack });
                    EventManager.Publish(new ModifyCourageEvent { Delta = +values[i++] });
                    break;
                }
                case "sword":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[i++] });
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                default:
                    Console.WriteLine($"[CardPlayService] No effect for id={cardId}");
                    break;
            }
        }
    }
}


