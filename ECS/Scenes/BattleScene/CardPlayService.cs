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
        public static void Resolve(EntityManager entityManager, string cardId, string cardName, Entity card)
        {
            var enemy = entityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var battleStateInfo = player.GetComponent<BattleStateInfo>();
            int courage = player?.GetComponent<Courage>()?.Amount ?? 0;

            System.Console.WriteLine($"[CardPlayService] Resolving card id={cardId} name={cardName}");
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
                    BlockValueService.Apply(card, values[i++]);
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
                case "inspiration":
                {
                    EventManager.Publish(new RequestDrawCardsEvent { Count = values[i++] });
                    EventManager.Publish(new ModifyTemperanceEvent { Delta = values[i++] });
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Effect });
                    break;
                }
                case "seize":
                {
                    battleStateInfo.PhaseTracking.TryGetValue(TrackingTypeEnum.CourageLost, out var courageLost);
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -(courageLost > 0 ? values[1] : values[0]), DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                // case "shroud_of_turin":
                // {
                //     // Duplicate the selected card and put the duplicate into discard. Then destroy the Shroud card entity.
                //     var pending = card.GetComponent<ShroudPendingSelection>();
                //     var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                //     if (pending != null && pending.Selected != null && deckEntity != null)
                //     {
                //         // Clone by creating a new entity with the same CardData and base components
                //         var copy = entityManager.CreateEntity($"CopyOf_{pending.Selected.Name}");
                //         var srcCd = pending.Selected.GetComponent<CardData>();
                //         var srcSprite = pending.Selected.GetComponent<Sprite>();
                //         if (srcCd != null)
                //         {
                //             entityManager.AddComponent(copy, new CardData { CardId = srcCd.CardId, Color = srcCd.Color });
                //         }
                //         entityManager.AddComponent(copy, new Transform { Position = Microsoft.Xna.Framework.Vector2.Zero, Scale = Microsoft.Xna.Framework.Vector2.One });
                //         entityManager.AddComponent(copy, new Sprite { TexturePath = srcSprite?.TexturePath ?? string.Empty, IsVisible = true });
                //         entityManager.AddComponent(copy, new UIElement { Bounds = new Microsoft.Xna.Framework.Rectangle(0,0,250,350), IsInteractable = false });
                //         EventManager.Publish(new CardMoveRequested { Card = copy, Deck = deckEntity, Destination = CardZoneType.DiscardPile, Reason = "ShroudCopy" });
                //         // Cleanup selection marker
                //         entityManager.RemoveComponent<ShroudPendingSelection>(card);
                //     }
                //     break;
                // }
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
                    Console.WriteLine($"[CardPlayService] Strike chance: {chance}");
                    if (Random.Shared.Next(0, 100) <= chance)
                    {
                        Console.WriteLine($"[CardPlayService] Strike gained {values[i]} courage");
                        EventManager.Publish(new ModifyActionPointsEvent { Delta = values[i++] });
                    }
                    break;
                }
                case "stun":
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = target, Type = AppliedPassiveType.Stun, Delta = 1 });
                    break;
                }
                case "vindicate":
                {
                    int damage = values[i++] + (courage * values[i++]);
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -damage, DamageType = ModifyTypeEnum.Attack });
                    EventManager.Publish(new SetCourageEvent { Amount = 0 });
                    break;
                }
                // weapons
                case "hammer":
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Attack });
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[i++] });
                    break;
                }
                case "sword":
                {
                    EventManager.Publish(new ModifyCourageEvent { Delta = -values[i++] });
                    EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = target, Delta = -values[i++], DamageType = ModifyTypeEnum.Attack });
                    break;
                }
                default:
                    System.Console.WriteLine($"[CardPlayService] No effect for id={cardId}");
                    break;
            }
        }
    }
}


