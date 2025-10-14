using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Equipment;

namespace Crusaders30XX.ECS.Systems
{
	public class UIElementEventDelegateSystem : Core.System
	{
        public UIElementEventDelegateSystem(EntityManager entityManager)
			: base(entityManager)
		{
		}

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{

			return EntityManager.GetEntitiesWithComponent<UIElement>();
		}

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var clickedUIComponent = GetRelevantEntities().Select(x => x.GetComponent<UIElement>()).Where(x => x.IsClicked && x.IsHovered).FirstOrDefault();
            var _entity = clickedUIComponent.Owner;
            if (clickedUIComponent == null)
            {
                return;
            }
            switch(clickedUIComponent.EventType)
            {
                case UIElementEventType.UnassignCardAsBlock:
                {
                    EventManager.Publish(new UnassignCardAsBlockRequested { CardEntity = _entity });
                    break;
                }
                case UIElementEventType.AssignEquipmentAsBlock:
                {
                    EventManager.Publish(new AssignEquipmentAsBlockRequested { EquipmentEntity = _entity });
                    break;
                }
                case UIElementEventType.ActivateEquipment:
                {
                    EventManager.Publish(new ActivateEquipmentRequested { EquipmentEntity = _entity });
                    break;
                }
            }
        }

    }
}