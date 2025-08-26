using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// After an enemy attack completes, remove the resolved planned attack and
	/// transition to Block if another planned attack remains, otherwise to Action.
	/// </summary>
	public class QueuedAdvanceToNextPlannedAttackEvent : EventQueue.IQueuedEvent
	{
		public string Name { get; }
		public object Payload { get; }
		public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

		private readonly EntityManager _entityManager;
		private readonly string _contextId;

		public QueuedAdvanceToNextPlannedAttackEvent(EntityManager entityManager, string contextId)
		{
			_entityManager = entityManager;
			_contextId = contextId;
			Name = "Rule.AdvanceToNextPlannedAttackIfAny";
			Payload = contextId;
		}

		public void StartResolving()
		{
			// Remove the resolved planned attack by context id
			var enemy = _entityManager.GetEntitiesWithComponent<AttackIntent>()
				.FirstOrDefault(en => en.GetComponent<AttackIntent>().Planned.Any(pa => pa.ContextId == _contextId));
			if (enemy != null)
			{
				var intent = enemy.GetComponent<AttackIntent>();
				if (intent != null)
				{
					int idx = intent.Planned.FindIndex(p => p.ContextId == _contextId);
					if (idx >= 0) intent.Planned.RemoveAt(idx);
				}
			}

			// If any planned attacks remain on any enemy, go back to Block for re-assignment; otherwise go to Action
			bool hasNext = _entityManager.GetEntitiesWithComponent<AttackIntent>()
				.Any(en =>
				{
					var i = en.GetComponent<AttackIntent>();
					return i != null && i.Planned != null && i.Planned.Count > 0;
				});
			if (hasNext)
			{
				EventManager.Publish(new ChangeBattlePhaseEvent { Next = BattlePhase.Block });
			}
			else
			{
				EventManager.Publish(new ChangeBattlePhaseEvent { Next = BattlePhase.Action });
			}

			State = EventQueue.EventState.Complete;
		}

		public void Update(float deltaSeconds) { }
	}
}



