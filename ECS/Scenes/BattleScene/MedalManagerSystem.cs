using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Listens to battle phase changes and triggers equipped medals when their triggers match.
	/// Initially supports StartOfBattle heal for Medal of Saint Luke.

	/// </summary>
	public class MedalManagerSystem : Core.System
	{
		[DebugEditable(DisplayName = "Activation Delay (s)", Step = 0.05f, Min = 0f, Max = 2f)]
		public float ActivationDelaySeconds { get; set; } = 0.3f;
		public MedalManagerSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<MedalActivateEvent>(OnMedalActivate);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnMedalActivate(MedalActivateEvent e)
		{
			EventQueueBridge.EnqueueTriggerAction(() =>
			{
				var medal = e.MedalEntity.GetComponent<EquippedMedal>().Medal;
				EventManager.Publish(new MedalTriggered { MedalEntity = e.MedalEntity, MedalId = medal.Id });
				medal.Activate();
			}, ActivationDelaySeconds);
		}

	}
}



