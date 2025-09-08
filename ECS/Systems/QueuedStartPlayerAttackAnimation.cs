using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Queued event to begin the player's attack animation.
	/// </summary>
	public class QueuedStartPlayerAttackAnimation : EventQueue.IQueuedEvent
	{
		public string Name { get; }
		public object Payload { get; }
		public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

		public QueuedStartPlayerAttackAnimation()
		{
			Name = "Rule.StartPlayerAttackAnimation";
			Payload = null;
		}

		public void StartResolving()
		{
			EventManager.Publish(new StartPlayerAttackAnimation());
			State = EventQueue.EventState.Complete;
		}

		public void Update(float deltaSeconds) { }
	}
}


