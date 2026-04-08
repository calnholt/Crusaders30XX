using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Poison")]
	public class PoisonSystem : Core.System
	{
		[DebugEditable(DisplayName = "Seconds Per Tick", Step = 1f, Min = 1f, Max = 600f)]
		public float SecondsPerTick { get; set; } = 60f;

		private float _timerRemaining;
		private bool _wasPoisonedLastFrame;
		private bool _paused = false;

		public PoisonSystem(EntityManager entityManager) : base(entityManager)
		{
			_timerRemaining = SecondsPerTick;

			EventManager.Subscribe<UpdatePassive>(OnUpdatePassive);
			EventManager.Subscribe<PassiveTriggered>(_ => { /* no-op; kept for potential future feedback hooks */ });
			EventManager.Subscribe<TutorialStartedEvent>(OnTutorialStarted);
			EventManager.Subscribe<TutorialCompletedEvent>(OnTutorialCompleted);
		}

		private void OnTutorialStarted(TutorialStartedEvent e)
		{
			LoggingService.Append("PoisonSystem.OnTutorialStarted", new System.Text.Json.Nodes.JsonObject
			{
				["eventType"] = "TutorialStarted"
			});
			_paused = true;
		}

		private void OnTutorialCompleted(TutorialCompletedEvent e)
		{
			LoggingService.Append("PoisonSystem.OnTutorialCompleted", new System.Text.Json.Nodes.JsonObject
			{
				["eventType"] = "TutorialCompleted"
			});
			_paused = false;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			// Run once per frame by anchoring to scene state
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (_paused) return;
			var player = EntityManager.GetEntity("Player");
			if (player == null) return;
			bool isPoisoned = IsPoisoned(player);

			if (!isPoisoned)
			{
				// Reset the timer when poison is not active
				_timerRemaining = SecondsPerTick;
				_wasPoisonedLastFrame = false;
				RemoveMeterComponent(player);
				return;
			}

			// Start full timer when poison becomes active
			if (!_wasPoisonedLastFrame)
			{
				_timerRemaining = SecondsPerTick;
				_wasPoisonedLastFrame = true;
			}

			// Update the PassiveMeterComponent on the tooltip entity
			UpdateMeterComponent(player);

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (dt <= 0f) return;
			_timerRemaining -= dt;
			if (_timerRemaining <= 0f)
			{
				// Apply -1 HP and reset timer
				EventManager.Publish(new ModifyHpRequestEvent
				{
					Source = player,
					Target = player,
					Delta = -1,
					DamageType = ModifyTypeEnum.Effect
				});
				EventManager.Publish(new PassiveTriggered { Owner = player, Type = AppliedPassiveType.Poison });
				EventManager.Publish(new PoisonDamageEvent { DurationSec = .5f });
				_timerRemaining = SecondsPerTick;
			}
		}

		private bool IsPoisoned(Entity player)
		{
			var ap = player?.GetComponent<AppliedPassives>();
			if (ap == null || ap.Passives == null) return false;
			return ap.Passives.TryGetValue(AppliedPassiveType.Poison, out var stacks) && stacks > 0;
		}

		private void OnUpdatePassive(UpdatePassive e)
		{
			LoggingService.Append("PoisonSystem.OnUpdatePassive", new System.Text.Json.Nodes.JsonObject
			{
				["passiveType"] = e.Type.ToString(),
				["ownerId"] = e.Owner?.Id ?? -1
			});
			if (e == null || e.Owner == null) return;
			var player = EntityManager.GetEntity("Player");
			if (player == null || e.Owner.Id != player.Id) return;
			if (e.Type != AppliedPassiveType.Poison) return;
			// If poison cleared, reset timer; if applied, start fresh countdown
			if (!IsPoisoned(player))
			{
				_timerRemaining = SecondsPerTick;
				_wasPoisonedLastFrame = false;
			}
		}

		private void UpdateMeterComponent(Entity player)
		{
			var anchorName = $"UI_PassiveTooltip_{player.Id}_{AppliedPassiveType.Poison}";
			var anchor = EntityManager.GetEntity(anchorName);
			if (anchor == null) return;

			var meter = anchor.GetComponent<PassiveMeterComponent>();
			if (meter == null)
			{
				meter = new PassiveMeterComponent
				{
					CurrentValue = _timerRemaining,
					MaxValue = SecondsPerTick,
					Direction = PassiveMeterDirection.Countdown,
					IsActive = true
				};
				EntityManager.AddComponent(anchor, meter);
			}
			else
			{
				meter.CurrentValue = _timerRemaining;
				meter.MaxValue = SecondsPerTick;
				meter.Direction = PassiveMeterDirection.Countdown;
				meter.IsActive = true;
			}
		}

		private void RemoveMeterComponent(Entity player)
		{
			var anchorName = $"UI_PassiveTooltip_{player.Id}_{AppliedPassiveType.Poison}";
			var anchor = EntityManager.GetEntity(anchorName);
			if (anchor != null && anchor.HasComponent<PassiveMeterComponent>())
			{
				EntityManager.RemoveComponent<PassiveMeterComponent>(anchor);
			}
		}
	}
}
