using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Equipment;
using System;

namespace Crusaders30XX.ECS.Systems
{
	internal static class UIElementEventDelegateService
	{
        public static void HandleEvent(UIElementEventType type, Entity entity)
        {
            switch(type)
            {
                case UIElementEventType.ConfirmBlocks:
                {
                    EventManager.Publish(new DebugCommandEvent { Command = "ConfirmEnemyAttack" });
                    break;
                }
                case UIElementEventType.UnassignCardAsBlock:
                {
                    EventManager.Publish(new UnassignCardAsBlockRequested { CardEntity = entity });
                    break;
                }
                case UIElementEventType.ActivateEquipment:
                {
                    EventManager.Publish(new ActivateEquipmentRequested { EquipmentEntity = entity });
                    break;
                }
                case UIElementEventType.CardListModalClose:
                {
                    EventManager.Publish(new CloseCardListModalEvent { });
                    break;
                }
                default:
                {
                    Console.WriteLine($"UIElementEventDelegateSystem: clicked unknown event type {type} on entity {entity.Id}");
                    break;
                }
            }
        }
    }
}