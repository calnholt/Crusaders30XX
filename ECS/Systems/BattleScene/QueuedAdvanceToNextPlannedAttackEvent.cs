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

			var hasNext = intent != null && intent.Planned != null && intent.Planned.Count > 0;
			if (hasNext) {
				EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
					"Rule.ChangePhase.PreBlock",
					new ChangeBattlePhaseEvent { Current = SubPhase.PreBlock }
				));
				EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
					"Rule.ChangePhase.Block",
					new ChangeBattlePhaseEvent { Current = SubPhase.Block }
				));
			}
			else {
				EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
					"Rule.ChangePhase.EnemyEnd",
					new ChangeBattlePhaseEvent { Current = SubPhase.EnemyEnd }
				));
				EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
					"Rule.ChangePhase.PlayerStart",
					new ChangeBattlePhaseEvent { Current = SubPhase.PlayerStart }
				));
				EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
					"Rule.ChangePhase.Action",
					new ChangeBattlePhaseEvent { Current = SubPhase.Action }
				));
			}

			State = EventQueue.EventState.Complete;
		}

		public void Update(float deltaSeconds) { }
	}
}



