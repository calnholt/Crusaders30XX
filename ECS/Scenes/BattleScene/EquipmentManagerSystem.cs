using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	public class EquipmentManagerSystem : Core.System
	{
		public EquipmentManagerSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<EquipmentActivateEvent>(OnEquipmentActivate);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnEquipmentActivate(EquipmentActivateEvent e)
		{
			if (e.EquipmentEntity == null) return;
			var equipment = e.EquipmentEntity.GetComponent<EquippedEquipment>();
			if (equipment == null) return;
			if (!equipment.Equipment.HasUses || !equipment.Equipment.CanActivate()) return;
			equipment.Equipment.Activate();
			EventManager.Publish(new EquipmentAbilityTriggered { Equipment = e.EquipmentEntity, EquipmentId = equipment.Equipment.Id });
		}

	}
}


