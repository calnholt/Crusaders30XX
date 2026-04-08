using System.Text.Json.Nodes;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Systems
{
	public class QueuedWaitBuffComplete : EventQueue.IQueuedEvent
	{
		public string Name { get; }
		public object Payload { get; }
		public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

		private readonly bool _targetIsPlayer;
		private System.Action<BuffAnimationComplete> _handler;

		public QueuedWaitBuffComplete(bool targetIsPlayer)
		{
			_targetIsPlayer = targetIsPlayer;
			Name = "Rule.WaitBuffComplete";
			Payload = targetIsPlayer;
		}

		public void StartResolving()
		{
			State = EventQueue.EventState.Waiting;
			_handler = OnComplete;
			EventManager.Subscribe(_handler);
		}

		public void Update(float deltaSeconds) { }

		private void OnComplete(BuffAnimationComplete e)
		{
			LoggingService.Append("QueuedWaitBuffComplete.OnComplete", new JsonObject {
				{ "TargetIsPlayer", e?.TargetIsPlayer }
			});
			if (e == null || e.TargetIsPlayer != _targetIsPlayer) return;
			EventManager.Unsubscribe(_handler);
			State = EventQueue.EventState.Complete;
		}
	}
}


