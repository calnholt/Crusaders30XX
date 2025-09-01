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
			var intent = enemy.GetComponent<AttackIntent>();
			if (intent != null)
			{
				int idx = intent.Planned.FindIndex(p => p.ContextId == _contextId);
				if (idx >= 0) intent.Planned.RemoveAt(idx);
			}

			// Defer phase advancement decision to PhaseCoordinator
			var hasNext = intent != null && intent.Planned != null && intent.Planned.Count > 0;
			EventManager.Publish(new ProceedToNextPhase());
			if (!hasNext) {
				EventManager.Publish(new ProceedToNextPhase());
				EventManager.Publish(new ProceedToNextPhase());
			}

			State = EventQueue.EventState.Complete;
		}

		public void Update(float deltaSeconds) { }
	}
}



