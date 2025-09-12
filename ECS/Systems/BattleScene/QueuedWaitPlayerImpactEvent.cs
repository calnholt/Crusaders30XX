using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Queued event that waits until the player's attack animation signals impact.
	/// </summary>
	public class QueuedWaitPlayerImpactEvent : EventQueue.IQueuedEvent
	{
		public string Name { get; }
		public object Payload { get; }
		public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

		private System.Action<PlayerAttackImpactNow> _handler;

		public QueuedWaitPlayerImpactEvent()
		{
			Name = "Rule.WaitPlayerImpact";
			Payload = null;
		}

		public void StartResolving()
		{
			State = EventQueue.EventState.Waiting;
			_handler = OnImpact;
			EventManager.Subscribe(_handler);
		}

		public void Update(float deltaSeconds) { }

		private void OnImpact(PlayerAttackImpactNow _)
		{
			EventManager.Unsubscribe(_handler);
			State = EventQueue.EventState.Complete;
		}
	}
}


