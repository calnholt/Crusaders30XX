using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	public class QueuedStartBuffAnimation : EventQueue.IQueuedEvent
	{
		public string Name { get; }
		public object Payload { get; }
		public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

		private readonly bool _targetIsPlayer;

		public QueuedStartBuffAnimation(bool targetIsPlayer)
		{
			_targetIsPlayer = targetIsPlayer;
			Name = "Rule.StartBuffAnimation";
			Payload = targetIsPlayer;
		}

		public void StartResolving()
		{
			EventManager.Publish(new StartBuffAnimation { TargetIsPlayer = _targetIsPlayer });
			State = EventQueue.EventState.Complete;
		}

		public void Update(float deltaSeconds) { }
	}
}


