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
            var passives = player.GetComponent<AppliedPassives>().Passives;

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
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -def.damage, DamageType = ModifyTypeEnum.Attack });
                    BlockValueService.ApplyDelta(card, values[i++]);
                    break;
                }
                case "burn":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = target, Type = AppliedPassiveType.Burn, Delta = +values[i++] });
                    if (courage >= values[i++]) 
                    {
                        EventManager.Publish(new ModifyActionPointsEvent { Delta = values[i++] });
                        EventManager.Publish(new ModifyCourageEvent { Delta = -values[i++] });
                    }
                    break;
                }
                case "courageous":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = +values[i++] });
                    EventManager.Publish(new DebugCommandEvent { Command = "EndTurn" });
                    break;
                }
                case "divine_protection":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = target, Type = AppliedPassiveType.Aegis, Delta = +values[i++] });
                    break;
                }
                case "dowse_with_holy_water":
                {
                    var delta = courage >= values[1] ? values[2] : values[0];
                    EventManager.Publish(new ApplyPassiveEvent { Target = target, Type = AppliedPassiveType.Aggression, Delta = delta });
                    break;
                }
                case "fury":
                {
                    player.GetComponent<AppliedPassives>().Passives.TryGetValue(AppliedPassiveType.Aggression, out var amount);
                    if (amount > 0)
                    {
                        EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aggression, Delta = amount });
                    }
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
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -def.damage, DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                case "increase_faith":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Power, Delta = values[i++] });
                    break;
                }
                case "kunai":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -def.damage, DamageType = ModifyTypeEnum.Attack });
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
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -def.damage, DamageType = ModifyTypeEnum.Attack });
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aegis, Delta = +values[i++] });
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Penance, Delta = +values[i++] });
                    break;
                }
                case "pouch_of_kunai":
                {
                    var count = Random.Shared.Next(values[0], values[1] + 1);
                    for (int j = 0; j < count; j++)
                    {
                        var kunai = EntityFactory.CreateCardFromDefinition(entityManager, $"kunai", CardData.CardColor.White, false, j + 1);
                        var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                        EventManager.Publish(new CardMoveRequested { Card = kunai, Deck = deckEntity, Destination = CardZoneType.Hand, Reason = "PouchOfKunai" });
                    }
                    break;
                }
                case "ravage":
                {
                    for (int j = 0; j < values[0]; j++)
                    {
                        EventManager.Publish(new MillCardEvent { });
                    }
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -def.damage, DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                case "reconciled":
                {
                    passives.TryGetValue(AppliedPassiveType.Penance, out var penance);
                    var extraDamage = penance == 0 ? values[0] : 0;
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -(def.damage + extraDamage), DamageType = ModifyTypeEnum.Attack });
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
                    var damage = ConditionalDamageCardService.Resolve(entityManager, card) + def.damage;
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -damage, DamageType = ModifyTypeEnum.Attack });
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
                    EventManager.Publish(new ModifyTemperanceEvent { Delta = 1 });
                    break;
                }
                case "stab":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[i++] });
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -def.damage, DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                case "strike":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -def.damage, DamageType = ModifyTypeEnum.Attack });
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
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -def.damage, DamageType = ModifyTypeEnum.Attack });
                    EventManager.Publish(new ModifyTemperanceEvent { Delta = values[i++] });
                    break;
                }
                case "vindicate":
                {
                    var damage = ConditionalDamageCardService.Resolve(entityManager, card) + def.damage;
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -damage, DamageType = ModifyTypeEnum.Attack });
                    EventManager.Publish(new SetCourageEvent { Amount = 0 });
                    break;
                }
                case "whirlwind":
                {
                    var time = 0.5f;
                    StateSingleton.PreventClicking = true;
                    var numOfHits = values[0];
                    TimerScheduler.Schedule(time * numOfHits, () => {
                        StateSingleton.PreventClicking = false;
                    });
                    for (int j = 0; j < numOfHits; j++)
                    {
                        TimerScheduler.Schedule(time + (j * time), () => {
                            EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -def.damage, DamageType = ModifyTypeEnum.Attack });
                        });
                    }
                    break;
                }
                // weapons
                case "hammer":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -def.damage, DamageType = ModifyTypeEnum.Attack });
                    EventManager.Publish(new ModifyCourageEvent { Delta = +values[i++] });
                    break;
                }
                case "sword":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[i++] });
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -def.damage, DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                default:
                    Console.WriteLine($"[CardPlayService] No effect for id={cardId}");
                    break;
            }
        }
    }
}


